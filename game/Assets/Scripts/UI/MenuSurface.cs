using UnityEngine;

// Resolution-independent uGUI chrome, shared by screen-space and world-space menus.
// The menu artwork and the live professor models are separate from this geometry.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class MenuSurface : UnityEngine.UI.MaskableGraphic
{
    private MenuSurface() { useLegacyMeshGeneration = false; }
    public float Radius = 8f;
    public float BorderWidth = 1.5f;
    public Color BorderColor = new Color32(49, 87, 70, 255);
    public float BottomShade = 0.95f;
    private const int CornerSteps = 8;
    private const int Points = (CornerSteps + 1) * 4;

    public void SetBorder(Color value)
    {
        if (BorderColor == value) return;
        BorderColor = value;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper mesh)
    {
        mesh.Clear();
        Rect rect = GetPixelAdjustedRect();
        if (rect.width <= 0 || rect.height <= 0) return;
        float radius = Mathf.Min(Radius, Mathf.Min(rect.width, rect.height) * 0.5f);
        float border = Mathf.Max(0, BorderWidth);
        Rect inner = new Rect(rect.x + border, rect.y + border, rect.width - border * 2, rect.height - border * 2);
        if (inner.width <= 0 || inner.height <= 0) return;
        mesh.AddVert(inner.center, Shade(inner.center.y, rect), Vector2.zero);
        for (int i = 0; i < Points; i++)
        {
            Vector2 point = Perimeter(inner, Mathf.Max(0, radius - border), i);
            mesh.AddVert(point, Shade(point.y, rect), Vector2.zero);
        }
        for (int i = 0; i < Points; i++) mesh.AddTriangle(0, 1 + i, 1 + (i + 1) % Points);
        if (border <= 0) return;
        int start = mesh.currentVertCount;
        for (int i = 0; i < Points; i++)
        {
            mesh.AddVert(Perimeter(inner, Mathf.Max(0, radius - border), i), BorderColor, Vector2.zero);
            mesh.AddVert(Perimeter(rect, radius, i), BorderColor, Vector2.zero);
        }
        for (int i = 0; i < Points; i++)
        {
            int current = start + i * 2, next = start + ((i + 1) % Points) * 2;
            mesh.AddTriangle(current, current + 1, next);
            mesh.AddTriangle(current + 1, next + 1, next);
        }
    }

    private Color Shade(float y, Rect rect)
    {
        float shade = Mathf.Lerp(BottomShade, 1f, Mathf.InverseLerp(rect.yMin, rect.yMax, y));
        return new Color(color.r * shade, color.g * shade, color.b * shade, color.a);
    }

    private static Vector2 Perimeter(Rect rect, float radius, int index)
    {
        int corner = index / (CornerSteps + 1);
        float angle = (corner * 90f + index % (CornerSteps + 1) * 90f / CornerSteps) * Mathf.Deg2Rad;
        Vector2 center = new Vector2(corner == 0 || corner == 3 ? rect.xMax - radius : rect.xMin + radius,
            corner < 2 ? rect.yMax - radius : rect.yMin + radius);
        return center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
    }
}
