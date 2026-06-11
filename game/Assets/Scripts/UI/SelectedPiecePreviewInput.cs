using UnityEngine;
using UnityEngine.EventSystems;

public sealed class SelectedPiecePreviewInput : MonoBehaviour, IBeginDragHandler, IDragHandler, IScrollHandler
{
    private const float RotationDegreesPerPixel = -0.42f;
    private const float ZoomStep = 0.28f;
    private const float MinimumDistance = 1.35f;
    private const float DefaultDistance = 3.8f;
    private const float MaximumDistance = 5.4f;

    private Transform previewTarget;
    private Camera previewCamera;
    private Vector3 focusPoint;

    public bool HasInteractivePreview => previewTarget != null && previewCamera != null;

    public void Configure(Transform target, Camera camera)
    {
        previewTarget = target;
        previewCamera = camera;
        RefreshFocusPoint();
        NormalizeCameraDistance();
    }

    public void Configure(Transform target, Camera camera, Vector3 explicitFocusPoint)
    {
        previewTarget = target;
        previewCamera = camera;
        focusPoint = explicitFocusPoint;
        NormalizeCameraDistance();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (previewTarget != null && focusPoint == Vector3.zero)
        {
            RefreshFocusPoint();
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData == null)
        {
            return;
        }

        RotatePreview(eventData.delta.x * RotationDegreesPerPixel);
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (eventData == null)
        {
            return;
        }

        ZoomPreview(eventData.scrollDelta.y);
    }

    private void LateUpdate()
    {
        NormalizeCameraDistance();
    }

    public void RotatePreview(float degrees)
    {
        if (previewTarget == null || Mathf.Approximately(degrees, 0f))
        {
            return;
        }

        previewTarget.Rotate(Vector3.up, degrees, Space.World);
    }

    public void ZoomPreview(float scrollAmount)
    {
        if (!HasInteractivePreview || Mathf.Approximately(scrollAmount, 0f))
        {
            return;
        }

        Vector3 cameraDirection = previewCamera.transform.position - focusPoint;
        float currentDistance = Mathf.Max(0.01f, cameraDirection.magnitude);
        float targetDistance = Mathf.Clamp(currentDistance - scrollAmount * ZoomStep, MinimumDistance, MaximumDistance);

        previewCamera.transform.position = focusPoint + cameraDirection.normalized * targetDistance;
        previewCamera.transform.LookAt(focusPoint);
        previewCamera.Render();
    }

    private void RefreshFocusPoint()
    {
        if (previewTarget == null)
        {
            focusPoint = Vector3.zero;
            return;
        }

        Renderer[] renderers = previewTarget.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            focusPoint = previewTarget.position;
            return;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        focusPoint = bounds.center;
    }

    public void NormalizeCameraDistance()
    {
        if (!HasInteractivePreview)
        {
            return;
        }

        Vector3 cameraDirection = previewCamera.transform.position - focusPoint;
        float currentDistance = cameraDirection.magnitude;
        if (currentDistance >= MinimumDistance && currentDistance <= MaximumDistance)
        {
            return;
        }

        Vector3 direction = currentDistance > 0.01f ? cameraDirection.normalized : Vector3.back;
        float targetDistance = currentDistance < MinimumDistance ? MinimumDistance : DefaultDistance;
        previewCamera.transform.position = focusPoint + direction * targetDistance;
        previewCamera.transform.LookAt(focusPoint);
        previewCamera.Render();
    }
}
