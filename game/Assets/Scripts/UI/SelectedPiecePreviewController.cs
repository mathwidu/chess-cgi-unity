using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public sealed class SelectedPiecePreviewController : MonoBehaviour
{
    private const float MinZoom = 1.6f;
    private const float MaxZoom = 8.5f;
    private const float DefaultZoom = 3f;
    private const float DefaultYaw = 180f;
    private const float TargetPreviewHeight = 1.55f;
    private const float FitSafetyMargin = 1.28f;

    private readonly Color previewSurfaceColor = new Color(0.12f, 0.13f, 0.13f, 1f);

    private RawImage targetImage;
    private RenderTexture previewTexture;
    private Camera previewCamera;
    private Light previewLight;
    private Transform previewStage;
    private GameObject previewClone;
    private PieceView previewedPiece;
    private Vector3 previewFocusPoint;
    private bool hasPreviewFocusPoint;

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
        Bounds fittedBounds = FitPreviewClone(previewClone.transform);
        ConfigureCameraForBounds(fittedBounds);
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
            previewTexture = new RenderTexture(768, 768, 24)
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
            previewFocusPoint = GetDefaultFocusPoint();
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
            previewCamera.farClipPlane = 24f;
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

        Vector3 focusPoint = hasPreviewFocusPoint ? previewFocusPoint : GetDefaultFocusPoint();
        previewCamera.transform.position = focusPoint + new Vector3(0f, 0f, -CurrentZoom);
        previewCamera.transform.LookAt(focusPoint);
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

    private Bounds FitPreviewClone(Transform clone)
    {
        Renderer[] renderers = clone.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            clone.localPosition = new Vector3(0f, 0.08f, 0f);
            return new Bounds(previewStage.position + new Vector3(0f, 0.58f, 0f), new Vector3(0.6f, 1f, 0.6f));
        }

        Bounds bounds = CalculateBounds(renderers);
        if (bounds.size.y > 0.001f)
        {
            float scale = TargetPreviewHeight / bounds.size.y;
            clone.localScale *= scale;
        }

        bounds = CalculateBounds(renderers);
        Vector3 targetCenter = previewStage.position + new Vector3(0f, 0.82f, 0f);
        clone.position += targetCenter - bounds.center;

        bounds = CalculateBounds(renderers);
        float targetFloor = previewStage.position.y + 0.06f;
        clone.position += Vector3.up * (targetFloor - bounds.min.y);
        bounds = CalculateBounds(renderers);
        return bounds;
    }

    private void ConfigureCameraForBounds(Bounds bounds)
    {
        if (previewCamera == null || previewTexture == null)
        {
            CurrentZoom = DefaultZoom;
            hasPreviewFocusPoint = false;
            return;
        }

        previewFocusPoint = bounds.center;
        hasPreviewFocusPoint = true;

        float aspect = previewTexture.height > 0 ? (float)previewTexture.width / previewTexture.height : 1f;
        float fitDistance = CalculateFitDistance(bounds, previewCamera.fieldOfView, aspect, FitSafetyMargin);
        CurrentZoom = Mathf.Clamp(fitDistance, MinZoom, MaxZoom);
    }

    private Vector3 GetDefaultFocusPoint()
    {
        return previewStage != null ? previewStage.position + new Vector3(0f, 0.82f, 0f) : new Vector3(0f, 0.82f, 0f);
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

    private static float CalculateFitDistance(Bounds bounds, float verticalFieldOfView, float aspect, float safetyMargin)
    {
        float verticalHalfAngle = Mathf.Max(1f, verticalFieldOfView) * 0.5f * Mathf.Deg2Rad;
        float horizontalHalfAngle = Mathf.Atan(Mathf.Tan(verticalHalfAngle) * Mathf.Max(0.1f, aspect));
        float verticalDistance = bounds.extents.y / Mathf.Tan(verticalHalfAngle);
        float horizontalDistance = Mathf.Max(bounds.extents.x, bounds.extents.z) / Mathf.Tan(horizontalHalfAngle);
        return Mathf.Max(verticalDistance, horizontalDistance, MinZoom) * Mathf.Max(1f, safetyMargin);
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
