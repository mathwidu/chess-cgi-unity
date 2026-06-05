using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;

public class SelectedPiecePreviewInputTests
{
    [Test]
    public void Configure_SendsDragAndScrollToPreviewController()
    {
        GameObject owner = new GameObject("Preview Input Test");
        GameObject eventSystemObject = new GameObject("EventSystem");
        try
        {
            eventSystemObject.AddComponent<EventSystem>();
            SelectedPiecePreviewController controller = owner.AddComponent<SelectedPiecePreviewController>();
            SelectedPiecePreviewInput input = owner.AddComponent<SelectedPiecePreviewInput>();
            input.Configure(controller);

            PointerEventData drag = new PointerEventData(EventSystem.current) { delta = new Vector2(20f, 0f) };
            PointerEventData scroll = new PointerEventData(EventSystem.current) { scrollDelta = new Vector2(0f, 1f) };

            float initialYaw = controller.CurrentYaw;
            float initialZoom = controller.CurrentZoom;

            input.OnDrag(drag);
            input.OnScroll(scroll);

            Assert.Greater(controller.CurrentYaw, initialYaw);
            Assert.Less(controller.CurrentZoom, initialZoom);
        }
        finally
        {
            Object.DestroyImmediate(owner);
            Object.DestroyImmediate(eventSystemObject);
        }
    }
}
