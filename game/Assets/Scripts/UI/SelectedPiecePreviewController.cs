using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public sealed class SelectedPiecePreviewController : MonoBehaviour
{
    private const float MinZoom = 1.6f;
    private const float MaxZoom = 4.8f;
    private const float DefaultZoom = 3f;
    private const float DefaultYaw = 180f;

    private readonly Color previewSurfaceColor = new Color(0.12f, 0.13f, 0.13f, 1f);

    private RawImage targetImage;
    private RenderTexture previewTexture;
    private Camera previewCamera;
    private Light previewLight;
    private Transform previewStage;
    private GameObject previewClone;
    private PieceView previewedPiece;

    public bool HasPreview => previewClone != null;

    public float CurrentYaw { get; private set; } = DefaultYaw;

    public float CurrentZoom { get; private set; } = DefaultZoom;

    public void Configure(RawImage image)
    {
        targetImage = image;
        EnsureResources();

        if (targetImage != null)
        {
            targetImage.texture = previewTexture;
        }
    }

    public void ShowPiece(PieceView selectedPiece)
    {
        if (selectedPiece == null)
        {
            Clear();
            return;
        }

        EnsureResources();

        if (previewedPiece == selectedPiece && previewClone != null)
        {
            RenderIfPossible();
            return;
        }

        ClearClone();

        previewClone = Object.Instantiate(selectedPiece.gameObject, previewStage);
        previewClone.name = "SelectedPiecePreviewClone";
        previewClone.transform.localPosition = Vector3.zero;
        previewClone.transform.localRotation = Quaternion.Euler(0f, CurrentYaw, 0f);
        previewClone.transform.localScale = Vector3.one;
        previewedPiece = selectedPiece;

        DisablePreviewInteractionComponents(previewClone);
        FitPreviewClone(previewClone.transform);
        ApplyView();
    }

    public void Clear()
    {
        ClearClone();
        previewedPiece = null;
    }

    public void Rotate(float deltaYaw)
    {
        CurrentYaw = Mathf.Repeat(CurrentYaw + deltaYaw, 360f);
        ApplyView();
    }

    public void Zoom(float deltaZoom)
    {
        CurrentZoom = Mathf.Clamp(CurrentZoom - deltaZoom, MinZoom, MaxZoom);
        ApplyView();
    }

    public void ResetView()
    {
        CurrentYaw = DefaultYaw;
        CurrentZoom = DefaultZoom;
        ApplyView();
    }

    private void OnDestroy()
    {
        ClearClone();

        if (previewTexture != null)
        {
            previewTexture.Release();
            DestroyUnityObject(previewTexture);
            previewTexture = null;
        }
    }

    private void EnsureResources()
    {
        if (previewTexture == null)
        {
            previewTexture = new RenderTexture(768, 512, 24)
            {
                name = "SelectedPiecePreviewTexture",
                antiAliasing = 4,
                useMipMap = false
            };
            previewTexture.Create();
        }

        if (previewStage == null)
        {
            GameObject stageObject = new GameObject("SelectedPiecePreviewStage");
            stageObject.transform.SetParent(transform, false);
            stageObject.transform.position = new Vector3(96f, 96f, 96f);
            previewStage = stageObject.transform;
        }

        if (previewCamera == null)
        {
            GameObject cameraObject = new GameObject("SelectedPiecePreviewCamera");
            cameraObject.transform.SetParent(previewStage, false);
            previewCamera = cameraObject.AddComponent<Camera>();
            previewCamera.clearFlags = CameraClearFlags.SolidColor;
            previewCamera.backgroundColor = previewSurfaceColor;
            previewCamera.fieldOfView = 24f;
            previewCamera.nearClipPlane = 0.03f;
            previewCamera.farClipPlane = 12f;
            previewCamera.targetTexture = previewTexture;
        }

        if (previewLight == null)
        {
            GameObject lightObject = new GameObject("SelectedPiecePreviewLight");
            lightObject.transform.SetParent(previewStage, false);
            previewLight = lightObject.AddComponent<Light>();
            previewLight.type = LightType.Directional;
            previewLight.intensity = 1.7f;
            previewLight.color = new Color(1f, 0.95f, 0.86f, 1f);
        }

        previewLight.transform.localRotation = Quaternion.Euler(38f, -28f, 0f);
        ApplyCameraPose();
    }

    private void ApplyView()
    {
        if (previewClone != null)
        {
            previewClone.transform.localRotation = Quaternion.Euler(0f, CurrentYaw, 0f);
        }

        ApplyCameraPose();
        RenderIfPossible();
    }

    private void ApplyCameraPose()
    {
        if (previewCamera == null || previewStage == null)
        {
            return;
        }

        previewCamera.transform.localPosition = new Vector3(0f, 0.92f, -CurrentZoom);
        previewCamera.transform.LookAt(previewStage.position + new Vector3(0f, 0.82f, 0f));
    }

    private void RenderIfPossible()
    {
        if (previewCamera != null &&
            previewTexture != null &&
            previewTexture.IsCreated() &&
            SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null)
        {
            previewCamera.Render();
        }
    }

    private void FitPreviewClone(Transform clone)
    {
        Renderer[] renderers = clone.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            clone.localPosition = new Vector3(0f, 0.08f, 0f);
            return;
        }

        Bounds bounds = CalculateBounds(renderers);
        if (bounds.size.y > 0.001f)
        {
            float targetHeight = 1.7f;
            float scale = targetHeight / bounds.size.y;
            clone.localScale *= scale;
        }

        bounds = CalculateBounds(renderers);
        Vector3 targetCenter = previewStage.position + new Vector3(0f, 0.82f, 0f);
        clone.position += targetCenter - bounds.center;

        bounds = CalculateBounds(renderers);
        float targetFloor = previewStage.position.y + 0.06f;
        clone.position += Vector3.up * (targetFloor - bounds.min.y);
    }

    private void ClearClone()
    {
        if (previewClone != null)
        {
            DestroyUnityObject(previewClone);
            previewClone = null;
        }
    }

    private static void DisablePreviewInteractionComponents(GameObject clone)
    {
        PieceView clonePieceView = clone.GetComponent<PieceView>();
        if (clonePieceView != null)
        {
            clonePieceView.enabled = false;
        }

        Collider[] colliders = clone.GetComponentsInChildren<Collider>();
        foreach (Collider collider in colliders)
        {
            collider.enabled = false;
        }
    }

    private static Bounds CalculateBounds(Renderer[] renderers)
    {
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return bounds;
    }

    private static void DestroyUnityObject(Object target)
    {
        if (Application.isPlaying)
        {
            Object.Destroy(target);
        }
        else
        {
            Object.DestroyImmediate(target);
        }
    }
}
