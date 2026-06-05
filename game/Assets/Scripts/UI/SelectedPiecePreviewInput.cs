using UnityEngine;
using UnityEngine.EventSystems;

public sealed class SelectedPiecePreviewInput : MonoBehaviour, IDragHandler, IScrollHandler
{
    [SerializeField] private float dragSensitivity = 0.45f;
    [SerializeField] private float scrollSensitivity = 0.35f;

    private SelectedPiecePreviewController previewController;

    public void Configure(SelectedPiecePreviewController controller)
    {
        previewController = controller;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (previewController != null)
        {
            previewController.Rotate(eventData.delta.x * dragSensitivity);
        }
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (previewController != null)
        {
            previewController.Zoom(eventData.scrollDelta.y * scrollSensitivity);
        }
    }
}
