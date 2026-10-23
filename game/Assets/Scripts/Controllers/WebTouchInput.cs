using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Shared Web-only gate. Editor previews are explicit, transient and never persisted.
public static class WebTouchInput
{
#if UNITY_EDITOR
    public static bool EditorPreviewEnabled { get; set; }
    public static Vector2 EditorViewportSize { get; set; }
    public static Rect EditorSafeArea { get; set; }
#endif
    public static bool Enabled
    {
        get
        {
#if UNITY_EDITOR
            if (EditorPreviewEnabled) return true;
#endif
            return Application.platform == RuntimePlatform.WebGLPlayer;
        }
    }

    public static Vector2 ViewportSize
    {
        get
        {
#if UNITY_EDITOR
            if (EditorPreviewEnabled && EditorViewportSize.x > 0 && EditorViewportSize.y > 0)
                return EditorViewportSize;
#endif
            return new Vector2(Mathf.Max(1, Screen.width), Mathf.Max(1, Screen.height));
        }
    }

    public static Rect SafeArea
    {
        get
        {
#if UNITY_EDITOR
            if (EditorPreviewEnabled && EditorViewportSize.x > 0)
                return EditorSafeArea.width > 0 ? EditorSafeArea : new Rect(Vector2.zero, ViewportSize);
#endif
            Rect safe = Screen.safeArea;
            return safe.width > 0 && safe.height > 0 ? safe : new Rect(Vector2.zero, ViewportSize);
        }
    }

    public static float UiScale
    {
        get
        {
            Vector2 viewport = ViewportSize;
            return Mathf.Max(.1f, viewport.x < viewport.y ? viewport.x / 360f
                : viewport.y / (viewport.y < 600f ? 360f : Mathf.Min(720f, viewport.y)));
        }
    }

    public static bool ShortLandscape => SafeArea.height / UiScale < 500 && SafeArea.width > SafeArea.height;

    // The camera fits its real model bounds in this free region, while still
    // rendering the room across the whole screen behind the HUD.
    public static Rect BoardViewport
    {
        get
        {
            Vector2 viewport = ViewportSize;
            Rect safe = SafeArea;
            float scale = UiScale;
            float side = ShortLandscape ? 160f : 8f;
            float vertical = ShortLandscape ? 8f : 120f;
            return new Rect((safe.x + side * scale) / viewport.x,
                (safe.y + vertical * scale) / viewport.y,
                Mathf.Max(1, safe.width - 2 * side * scale) / viewport.x,
                Mathf.Max(1, safe.height - 2 * vertical * scale) / viewport.y);
        }
    }

    // Raycast at the actual position, without depending on EventSystem update order,
    // mouse pointer IDs or a stale IsPointerOverGameObject result for another finger.
    public static bool IsOverUi(Vector2 position)
    {
        EventSystem events = EventSystem.current;
        if (events == null) return false;
        var data = new PointerEventData(events) { position = position };
        var hits = new List<RaycastResult>();
        events.RaycastAll(data, hits);
        foreach (RaycastResult hit in hits)
            if (hit.module is GraphicRaycaster) return true;
        return false;
    }
}

public readonly struct WebTouchPoint
{
    public readonly int Id;
    public readonly Vector2 Position;
    public readonly bool Released, Cancelled, OverUi;

    public WebTouchPoint(int id, Vector2 position, bool released = false, bool cancelled = false, bool overUi = false)
    {
        Id = id;
        Position = position;
        Released = released;
        Cancelled = cancelled;
        OverUi = overUi;
    }
}

// One gesture owns all fingers until they are lifted. A pinch can never become a tap
// when only one finger remains. This also makes mouse emulation safe to ignore.
public sealed class WebTouchGesture
{
    private int primaryId = -1;
    private Vector2 startPosition, previousCentre;
    private float startTime, previousSpan;
    private bool invalidTap, uiGesture, multiple, cameraTracking;
    public bool HasTap { get; private set; }
    public Vector2 TapPosition { get; private set; }
    public float OrbitDelta { get; private set; }
    public float PinchDelta { get; private set; }

    public void Update(IReadOnlyList<WebTouchPoint> points, float time, float shortSide)
    {
        HasTap = false;
        OrbitDelta = PinchDelta = 0;
        if (points.Count == 0) { Reset(); return; }
        float dragThreshold = Mathf.Max(12f, shortSide * .025f);
        if (primaryId < 0)
        {
            primaryId = points[0].Id;
            startPosition = points[0].Position;
            startTime = time;
            invalidTap = points[0].Released || points[0].Cancelled;
        }
        if (points.Count > 1) multiple = invalidTap = true;
        int active = 0;
        WebTouchPoint first = default, second = default;
        foreach (WebTouchPoint point in points)
        {
            uiGesture |= point.OverUi;
            invalidTap |= point.Cancelled;
            if (point.Id == primaryId)
            {
                invalidTap |= (point.Position - startPosition).sqrMagnitude > dragThreshold * dragThreshold;
                if (point.Released && !point.Cancelled && !invalidTap && !uiGesture && !multiple && time - startTime <= .55f)
                {
                    HasTap = true;
                    TapPosition = point.Position;
                }
            }
            if (point.Released || point.Cancelled) continue;
            if (active == 0) first = point;
            if (active == 1) second = point;
            active++;
        }
        // Another finger may cross UI after the primary sample was inspected.
        if (uiGesture || multiple) HasTap = false;
        if (active == 2 && !uiGesture)
        {
            Vector2 centre = (first.Position + second.Position) * .5f;
            float span = Vector2.Distance(first.Position, second.Position);
            if (cameraTracking)
            {
                OrbitDelta = (centre.x - previousCentre.x) / Mathf.Max(1, shortSide) * 180f;
                PinchDelta = (span - previousSpan) / Mathf.Max(1, shortSide);
            }
            previousCentre = centre;
            previousSpan = span;
            cameraTracking = true;
        }
        else cameraTracking = false;
        if (active == 0) Reset();
    }

    public void Reset()
    {
        primaryId = -1;
        invalidTap = uiGesture = multiple = cameraTracking = false;
    }
}
