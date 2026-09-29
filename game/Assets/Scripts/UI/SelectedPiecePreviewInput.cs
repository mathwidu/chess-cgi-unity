using UnityEngine;
using UnityEngine.EventSystems;

public sealed class SelectedPiecePreviewInput : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
{
    private const float ZoomStep = 1.2f;
    private const float MinimumDistanceRatio = 0.55f;
    private const float MaximumDistanceRatio = 1.5f;

    private Transform previewTarget;
    private Camera previewCamera;
    private Vector3 focusPoint;
    private Vector3 panOffset, zoomFocusOffset;
    private Renderer[] previewRenderers;
    private PointerEventData.InputButton dragButton;
    private Vector3 initialPosition, initialCameraOffset;
    private Quaternion initialRotation;
    private float initialDistance;

    public bool HasInteractivePreview => previewTarget != null && previewCamera != null;
    public bool IsDragging { get; private set; }
    public int ZoomPercent => HasInteractivePreview ? Mathf.RoundToInt(100f * initialDistance / CameraDistance) : 100;
    public bool CanZoomIn => HasInteractivePreview && CameraDistance > initialDistance * MinimumDistanceRatio + 0.001f;
    public bool CanZoomOut => HasInteractivePreview && CameraDistance < initialDistance * MaximumDistanceRatio - 0.001f;
    private Vector3 ViewFocusPoint => focusPoint + panOffset + zoomFocusOffset;
    private float CameraDistance => Vector3.Distance(previewCamera.transform.position, ViewFocusPoint);

    public void Configure(Transform target, Camera camera)
    {
        Vector3 centre = target != null ? target.position : Vector3.zero;
        if (target != null)
        {
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
                centre = bounds.center;
            }
        }
        Configure(target, camera, centre);
    }

    public void Configure(Transform target, Camera camera, Vector3 explicitFocusPoint)
    {
        previewTarget = target;
        previewCamera = camera;
        focusPoint = explicitFocusPoint;
        panOffset = zoomFocusOffset = Vector3.zero;
        previewRenderers = target != null ? target.GetComponentsInChildren<Renderer>() : null;
        IsDragging = false;
        if (!HasInteractivePreview) return;
        initialPosition = target.position;
        initialRotation = target.rotation;
        initialCameraOffset = camera.transform.position - focusPoint;
        initialDistance = Mathf.Max(0.01f, initialCameraOffset.magnitude);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!HasInteractivePreview || eventData == null || IsDragging) return;
        dragButton = eventData.button;
        IsDragging = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!IsDragging || eventData == null || eventData.button != dragButton) return;
        var rect = (RectTransform)transform;
        // Work in the preview's coordinates so a drag behaves the same at any Canvas scale, including VR.
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, eventData.position, eventData.pressEventCamera, out Vector2 current)
            || !RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, eventData.position - eventData.delta, eventData.pressEventCamera, out Vector2 previous)) return;
        Vector2 delta = current - previous;
        delta = new Vector2(delta.x / Mathf.Max(1f, rect.rect.width), delta.y / Mathf.Max(1f, rect.rect.height));
        if (dragButton != PointerEventData.InputButton.Left)
        {
            PanPreview(delta);
            return;
        }
        RotatePreview(-delta.x * 360f);
        Vector3 offset = previewCamera.transform.position - ViewFocusPoint;
        float elevation = Mathf.Asin(Mathf.Clamp(offset.normalized.y, -1f, 1f)) * Mathf.Rad2Deg;
        elevation = Mathf.Clamp(elevation + delta.y * 100f, -15f, 65f);
        float radians = elevation * Mathf.Deg2Rad;
        SetView(offset.magnitude, new Vector3(0f, Mathf.Sin(radians), -Mathf.Cos(radians)));
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (eventData == null || eventData.button == dragButton) IsDragging = false;
    }
    private void OnDisable() => IsDragging = false;

    public void OnScroll(PointerEventData eventData)
    {
        if (eventData == null)
        {
            return;
        }

        ZoomPreview(eventData.scrollDelta.y);
    }

    public void RotatePreview(float degrees)
    {
        if (!HasInteractivePreview || Mathf.Approximately(degrees, 0f))
        {
            return;
        }

        previewTarget.RotateAround(focusPoint, Vector3.up, degrees);
        Vector3 offset = previewCamera.transform.position - ViewFocusPoint;
        SetView(offset.magnitude, offset.normalized);
    }

    // Delta is a fraction of the visible image. The character follows the pointer;
    // its orbit pivot stays fixed, independent of the camera's framing offset.
    public void PanPreview(Vector2 delta)
    {
        if (!HasInteractivePreview) return;
        float viewHeight = 2f * CameraDistance * Mathf.Tan(previewCamera.fieldOfView * .5f * Mathf.Deg2Rad);
        Vector3 shift = (previewCamera.transform.right * (delta.x * previewCamera.aspect)
            + previewCamera.transform.up * delta.y) * viewHeight;
        panOffset -= shift;
        previewCamera.transform.position -= shift;
    }

    public void ZoomPreview(float scrollAmount)
    {
        if (!HasInteractivePreview || Mathf.Approximately(scrollAmount, 0f))
        {
            return;
        }

        Vector3 cameraDirection = previewCamera.transform.position - ViewFocusPoint;
        float currentDistance = Mathf.Max(0.01f, cameraDirection.magnitude);
        float targetDistance = Mathf.Clamp(currentDistance / Mathf.Pow(ZoomStep, Mathf.Clamp(scrollAmount, -10f, 10f)),
            initialDistance * MinimumDistanceRatio, initialDistance * MaximumDistanceRatio);

        SetView(targetDistance, cameraDirection.normalized);
    }

    private void SetView(float distance, Vector3 direction)
    {
        Quaternion rotation = Quaternion.LookRotation(-direction, Vector3.up);
        zoomFocusOffset = Vector3.zero;
        if (distance < initialDistance && previewRenderers != null)
        {
            // Dolly toward the upper body only as needed to leave a margin above
            // the head/accessories. Manual panning remains independent of this.
            Vector3 cameraPosition = focusPoint + direction * distance;
            Quaternion inverse = Quaternion.Inverse(rotation);
            float topSlope = .86f * Mathf.Tan(previewCamera.fieldOfView * .5f * Mathf.Deg2Rad);
            float lift = 0f;
            foreach (Renderer renderer in previewRenderers)
            {
                if (renderer == null) continue;
                Bounds bounds = renderer.bounds;
                for (int corner = 0; corner < 8; corner++)
                {
                    Vector3 sign = new Vector3((corner & 1) == 0 ? -1 : 1,
                        (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1);
                    Vector3 point = inverse * (bounds.center + Vector3.Scale(bounds.extents, sign) - cameraPosition);
                    lift = Mathf.Max(lift, point.y - topSlope * point.z);
                }
            }
            zoomFocusOffset = rotation * Vector3.up * lift;
        }
        previewCamera.transform.SetPositionAndRotation(ViewFocusPoint + direction * distance, rotation);
    }

    public void ResetView()
    {
        if (!HasInteractivePreview) return;
        IsDragging = false;
        panOffset = zoomFocusOffset = Vector3.zero;
        previewTarget.SetPositionAndRotation(initialPosition, initialRotation);
        previewCamera.transform.position = focusPoint + initialCameraOffset;
        previewCamera.transform.LookAt(focusPoint);
    }

    public void NormalizeCameraDistance()
    {
        if (!HasInteractivePreview)
        {
            return;
        }

        Vector3 cameraDirection = previewCamera.transform.position - ViewFocusPoint;
        float currentDistance = cameraDirection.magnitude;
        float minimumDistance = initialDistance * MinimumDistanceRatio;
        float maximumDistance = initialDistance * MaximumDistanceRatio;
        if (currentDistance >= minimumDistance && currentDistance <= maximumDistance)
        {
            return;
        }

        Vector3 direction = currentDistance > 0.01f ? cameraDirection.normalized : Vector3.back;
        float targetDistance = Mathf.Clamp(currentDistance, minimumDistance, maximumDistance);
        SetView(targetDistance, direction);
    }
}
