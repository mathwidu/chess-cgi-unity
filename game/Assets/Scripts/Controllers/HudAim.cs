using UnityEngine;

public static class HudAim
{
    public static bool TryGetPanelPoint(Ray ray, RectTransform panel, float margin, float maxDistance, int blockingMask, out Vector3 point)
    {
        point = default;
        var plane = new Plane(panel.forward, panel.position);
        if (!plane.Raycast(ray, out float distance) || distance > maxDistance)
        {
            return false;
        }

        Vector3 hit = ray.GetPoint(distance);
        Vector3 local = panel.InverseTransformPoint(hit);
        Rect area = panel.rect;
        if (local.x < area.xMin - margin || local.x > area.xMax + margin || local.y < area.yMin - margin || local.y > area.yMax + margin)
        {
            return false;
        }

        if (Physics.Raycast(ray, distance, blockingMask, QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        point = hit;
        return true;
    }
}
