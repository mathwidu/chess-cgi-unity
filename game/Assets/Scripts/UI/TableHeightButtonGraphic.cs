using UnityEngine;

// The moving cap's circular face and arrow; also used as the desktop-only hit area.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class TableHeightButtonGraphic : UnityEngine.UI.MaskableGraphic
{
    private const int Segments = 48;
    public bool PointsUp = true;

    protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper mesh)
    {
        mesh.Clear();
        Rect rect = GetPixelAdjustedRect();
        float radius = Mathf.Min(rect.width, rect.height) * 0.5f;
        if (radius <= 0f) return;

        AddDisc(mesh, rect.center, radius, new Color(color.r * .45f, color.g * .45f, color.b * .45f, color.a));
        AddDisc(mesh, rect.center, radius * .94f, color);

        float direction = PointsUp ? 1f : -1f;
        Color arrowColor = new Color(1f, 1f, 1f, color.a);
        int stem = mesh.currentVertCount;
        AddArrowVertex(mesh, rect.center, radius, direction, -.11f, -.42f, arrowColor);
        AddArrowVertex(mesh, rect.center, radius, direction, -.11f, .07f, arrowColor);
        AddArrowVertex(mesh, rect.center, radius, direction, .11f, .07f, arrowColor);
        AddArrowVertex(mesh, rect.center, radius, direction, .11f, -.42f, arrowColor);
        mesh.AddTriangle(stem, stem + 1, stem + 2);
        mesh.AddTriangle(stem, stem + 2, stem + 3);
        int head = mesh.currentVertCount;
        AddArrowVertex(mesh, rect.center, radius, direction, -.36f, .05f, arrowColor);
        AddArrowVertex(mesh, rect.center, radius, direction, 0f, .46f, arrowColor);
        AddArrowVertex(mesh, rect.center, radius, direction, .36f, .05f, arrowColor);
        mesh.AddTriangle(head, head + 1, head + 2);
    }

    public override bool Raycast(Vector2 screenPoint, Camera eventCamera)
    {
        if (!base.Raycast(screenPoint, eventCamera)
            || !RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, eventCamera, out Vector2 localPoint))
            return false;
        Rect rect = rectTransform.rect;
        float radius = Mathf.Min(rect.width, rect.height) * 0.5f;
        return (localPoint - rect.center).sqrMagnitude <= radius * radius;
    }

    private static void AddDisc(UnityEngine.UI.VertexHelper mesh, Vector2 center, float radius, Color fill)
    {
        int start = mesh.currentVertCount;
        mesh.AddVert(center, fill, Vector2.zero);
        for (int i = 0; i < Segments; i++)
        {
            float angle = i * 2f * Mathf.PI / Segments;
            mesh.AddVert(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius, fill, Vector2.zero);
        }
        for (int i = 0; i < Segments; i++)
            mesh.AddTriangle(start, start + 1 + i, start + 1 + (i + 1) % Segments);
    }

    private static void AddArrowVertex(UnityEngine.UI.VertexHelper mesh, Vector2 center, float radius,
        float direction, float x, float y, Color fill)
    {
        mesh.AddVert(center + new Vector2(x, y * direction) * radius, fill, Vector2.zero);
    }
}
