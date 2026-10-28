using System.Collections.Generic;
using Unity.XR.CompositionLayers;
using Unity.XR.CompositionLayers.Extensions;
using Unity.XR.CompositionLayers.Layers;
using Unity.XR.CompositionLayers.Services;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.XR.OpenXR.CompositionLayers;

/// <summary>
/// Captures the existing world-space HUD at a fixed resolution. Input still uses
/// the original Canvas and eye camera; no proxy interactors or duplicate controls.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(1000)]
public sealed class QuestHudComposition : MonoBehaviour
{
    public const int CaptureLayer = 25;
    public const int TextureWidth = 2048;
    public const int TextureHeight = 1152;
    private const float CaptureInterval = 1f / 36f;

    private Canvas canvas;
    private Camera eye;
    private Camera captureCamera;
    private QuestHudCaptureCamera captureState;
    private RenderTexture captureTexture;
    private RenderTexture layerTexture;
    private Material maskMaterial;
    private Material resolveMaterial;
    private GameObject resources;
    private Transform surface;
    private CompositionLayer composition;
    private QuadLayerData quad;
    private LayerData sceneLayerData;
    private BlendType previousSceneBlend;
    private bool eyeIncludedCaptureLayer;
    private bool failed;
    private bool native;
    private bool rendered;
    private float nextCapture;
    private readonly Dictionary<GameObject, int> originalLayers = new Dictionary<GameObject, int>();
    private readonly List<XRDisplaySubsystem> displays = new List<XRDisplaySubsystem>();
    private readonly UniversalRenderPipeline.SingleCameraRequest captureRequest = new UniversalRenderPipeline.SingleCameraRequest();

    public bool IsActive => resources != null;
    public Camera CaptureCamera => captureCamera;
    public RenderTexture CaptureTexture => captureTexture;
    public RenderTexture LayerTexture => layerTexture;

    public void Configure(Canvas target)
    {
        if (canvas != target || target.renderMode != RenderMode.WorldSpace)
            Release();
        canvas = target;
        if (IsActive) IsolateGraphics();
        nextCapture = 0f;
    }

    private void LateUpdate()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!IsActive && !failed && canvas != null && canvas.renderMode == RenderMode.WorldSpace &&
            XRRig.EyeCamera != null && CompositionLayerManager.Instance?.LayerProvider is OpenXRLayerProvider)
            Activate(XRRig.EyeCamera, true);
#endif
        if (!IsActive) return;
        if (native && !(CompositionLayerManager.Instance?.LayerProvider is OpenXRLayerProvider))
        {
            Release();
            return;
        }
        if (canvas == null || canvas.renderMode != RenderMode.WorldSpace || eye == null)
        {
            Release();
            return;
        }
        SynchronizePose();
        if (Time.unscaledTime >= nextCapture)
        {
            try { Capture(); }
            catch (System.Exception exception)
            {
                failed = true;
                Debug.LogWarning($"CHESS_CGI_QUEST_UI fallback=canvas reason={exception.GetType().Name} detail={exception.Message}");
                Release();
            }
        }
    }

    // The non-native path exercises the same capture and raycast geometry in PlayMode.
    // Runtime activation requires the real OpenXR provider above.
    private void Activate(Camera eyeCamera, bool useNativeLayer)
    {
        if (IsActive || canvas == null || canvas.renderMode != RenderMode.WorldSpace) return;
        if (useNativeLayer && CompositionLayerManager.mainCameraCache != eyeCamera)
        {
            failed = true;
            Debug.LogWarning("CHESS_CGI_QUEST_UI fallback=canvas reason=composition_camera_mismatch");
            return;
        }
        Shader maskShader = Resources.Load<Shader>("UI/QuestHudMask");
        Shader resolveShader = Resources.Load<Shader>("UI/QuestHudResolve");
        if (maskShader == null || resolveShader == null || !maskShader.isSupported || !resolveShader.isSupported)
        {
            failed = true;
            Debug.LogWarning("CHESS_CGI_QUEST_UI fallback=canvas reason=shader_unavailable");
            return;
        }

        eye = eyeCamera;
        native = useNativeLayer;
        eyeIncludedCaptureLayer = (eye.cullingMask & (1 << CaptureLayer)) != 0;
        resources = new GameObject("Quest HUD capture resources");
        resources.transform.SetParent(transform, false);
        captureTexture = CreateTexture("Quest HUD coverage", 24);
        layerTexture = CreateTexture("Quest HUD compositor", 0);
        maskMaterial = new Material(maskShader) { name = "Quest HUD depth mask" };
        maskMaterial.mainTexture = captureTexture;
        resolveMaterial = new Material(resolveShader) { name = "Quest HUD straight color" };

        var cameraObject = new GameObject("Quest HUD capture camera");
        cameraObject.transform.SetParent(resources.transform, false);
        captureCamera = cameraObject.AddComponent<Camera>();
        captureCamera.enabled = false;
        // Game cameras preserve URP's global XR scale. The renderer feature
        // overrides only this target after that global setting has been applied.
        captureCamera.cameraType = CameraType.Game;
        captureState = cameraObject.AddComponent<QuestHudCaptureCamera>();
        captureCamera.cullingMask = 1 << CaptureLayer;
        captureCamera.orthographic = true;
        captureCamera.nearClipPlane = .01f;
        captureCamera.farClipPlane = 2f;
        captureCamera.clearFlags = CameraClearFlags.SolidColor;
        captureCamera.backgroundColor = Color.clear;
        captureCamera.allowHDR = false;
        captureCamera.allowMSAA = false;
        captureCamera.allowDynamicResolution = false;
        captureCamera.useOcclusionCulling = false;
        captureCamera.targetTexture = captureTexture;
        var data = cameraObject.AddComponent<UniversalAdditionalCameraData>();
        data.allowXRRendering = false;
        data.renderPostProcessing = false;
        data.renderShadows = false;
        data.requiresColorOption = CameraOverrideOption.Off;
        data.requiresDepthOption = CameraOverrideOption.Off;
        captureRequest.destination = captureTexture;

        GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Quad);
        plane.name = "Quest HUD depth surface";
        plane.transform.SetParent(resources.transform, false);
        // Never let the visual mask intercept an existing button raycast.
        Collider collider = plane.GetComponent<Collider>();
        collider.enabled = false;
        Destroy(collider);
        surface = plane.transform;
        var renderer = plane.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = maskMaterial;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.enabled = native;

        if (native)
        {
            composition = plane.AddComponent<CompositionLayer>();
            quad = new QuadLayerData { ApplyTransformScale = true, Size = Vector2.one, BlendType = BlendType.Premultiply };
            composition.ChangeLayerDataType(quad);
            composition.Order = -1;
            var textures = plane.AddComponent<TexturesExtension>();
            textures.LeftTexture = layerTexture;
            textures.RightTexture = layerTexture;
            textures.IsDynamic = true;
            textures.CropToAspect = false;
            textures.MipMapMode = TexturesExtension.MipMapModeEnum.None;
            sceneLayerData = CompositionLayerManager.Instance.DefaultSceneCompositionLayer.LayerData;
            previousSceneBlend = sceneLayerData.BlendTypeDirectly;
            sceneLayerData.BlendType = BlendType.Premultiply;
        }

        IsolateGraphics();
        SynchronizePose();
        Debug.Log($"CHESS_CGI_QUEST_UI mode={(native ? "native_underlay" : "capture_test")} texture={TextureWidth}x{TextureHeight} refreshMax=36 input=original_canvas layer={CaptureLayer}");
    }

    private static RenderTexture CreateTexture(string name, int depth)
    {
        var texture = new RenderTexture(TextureWidth, TextureHeight, depth, RenderTextureFormat.ARGB32)
        {
            name = name, antiAliasing = 1, useMipMap = false, autoGenerateMips = false,
            useDynamicScale = false, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp
        };
        texture.Create();
        return texture;
    }

    private void IsolateGraphics()
    {
        if (!originalLayers.ContainsKey(canvas.gameObject))
            originalLayers.Add(canvas.gameObject, canvas.gameObject.layer);
        canvas.gameObject.layer = CaptureLayer;
        foreach (Graphic graphic in canvas.GetComponentsInChildren<Graphic>(true))
        {
            GameObject target = graphic.gameObject;
            if (!originalLayers.ContainsKey(target)) originalLayers.Add(target, target.layer);
            target.layer = CaptureLayer;
        }
        eye.cullingMask &= ~(1 << CaptureLayer);
        // Do not change canvas.worldCamera, XRUIInputModule.uiCamera, Graphic.enabled
        // or interaction masks: the original raycast targets remain in world space.
    }

    private void SynchronizePose()
    {
        var rect = (RectTransform)canvas.transform;
        Vector3 centre = rect.TransformPoint(rect.rect.center);
        Vector2 size = Vector2.Scale(rect.rect.size, rect.lossyScale);
        surface.SetPositionAndRotation(centre, rect.rotation);
        surface.localScale = new Vector3(rect.rect.width, rect.rect.height, 1f);
        captureCamera.transform.SetPositionAndRotation(centre - rect.forward, rect.rotation);
        captureCamera.orthographicSize = Mathf.Abs(size.y) * .5f;
        captureCamera.aspect = Mathf.Abs(size.x / size.y);
    }

    private void Capture()
    {
        Canvas.ForceUpdateCanvases();
        captureState.TargetPrepared = false;
        RenderPipeline.SubmitRenderRequest(captureCamera, captureRequest);
        if (!captureState.TargetPrepared)
            throw new System.InvalidOperationException("HUD capture requires QuestHudCaptureFeature and RenderGraph");
        if (native)
        {
            SubsystemManager.GetSubsystems(displays);
            foreach (XRDisplaySubsystem display in displays)
                if (display.running && Mathf.Abs(display.scaleOfAllRenderTargets - captureState.SceneRenderScale) > .001f)
                    throw new System.InvalidOperationException("HUD capture changed the XR display render scale");
        }
        // uGUI renders premultiplied color. The underlay carries straight opaque
        // color; its coverage goes into the eye buffer with depth testing instead.
        Graphics.Blit(captureTexture, layerTexture, resolveMaterial);
        nextCapture = Time.unscaledTime + CaptureInterval;
        if (!rendered)
        {
            rendered = true;
            Debug.Log($"CHESS_CGI_QUEST_UI captured={captureTexture.width}x{captureTexture.height} camera={captureCamera.cameraType} sceneScale={captureState.SceneRenderScale:F2} native={native}");
        }
    }

    private void OnDisable() => Release();
    private void OnDestroy() => Release();

    private void Release()
    {
        if (!IsActive) return;
        resources.SetActive(false);
        foreach (KeyValuePair<GameObject, int> entry in originalLayers)
            if (entry.Key != null && entry.Key.layer == CaptureLayer) entry.Key.layer = entry.Value;
        originalLayers.Clear();
        if (eye != null && eyeIncludedCaptureLayer) eye.cullingMask |= 1 << CaptureLayer;
        if (sceneLayerData != null) sceneLayerData.BlendType = previousSceneBlend;
        if (captureCamera != null) captureCamera.targetTexture = null;
        Destroy(resources);
        Destroy(maskMaterial);
        Destroy(resolveMaterial);
        captureTexture.Release();
        layerTexture.Release();
        Destroy(captureTexture);
        Destroy(layerTexture);
        resources = null;
        captureCamera = null;
        captureState = null;
        captureTexture = null;
        layerTexture = null;
        composition = null;
        quad = null;
        sceneLayerData = null;
        rendered = false;
    }
}
