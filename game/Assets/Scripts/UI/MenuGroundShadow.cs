using UnityEngine;

// Soft contact shadow follows each live figure's projected base on the menu backdrop.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class MenuGroundShadow : UnityEngine.UI.MaskableGraphic
{
    private MenuGroundShadow() { useLegacyMeshGeneration = false; }
    public float SolidCore;

    protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper mesh)
    {
        mesh.Clear();
        Rect rect = GetPixelAdjustedRect();
        const int segments = 48, rings = 12;
        for (int ring = 0; ring <= rings; ring++)
        {
            float radius = (float)ring / rings;
            Color tint = color;
            tint.a *= SolidCore > 0 ? 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(SolidCore, 1f, radius))
                : Mathf.Pow(1f - radius * radius, 3f);
            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                Vector2 position = rect.center + new Vector2(Mathf.Cos(angle) * rect.width, Mathf.Sin(angle) * rect.height) * radius * 0.5f;
                mesh.AddVert(position, tint, Vector2.zero);
                if (ring == 0) continue;
                int current = ring * segments + i, next = ring * segments + (i + 1) % segments;
                mesh.AddTriangle(current - segments, current, next);
                mesh.AddTriangle(current - segments, next, next - segments);
            }
        }
    }
}
