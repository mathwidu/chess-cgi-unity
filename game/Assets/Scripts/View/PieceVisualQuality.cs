using UnityEngine;

public static class PieceVisualQuality
{
    public readonly struct Report
    {
        public Report(bool hasRenderer, int rendererCount, int materialSlotCount, Bounds bounds, bool isReadableOnBoard)
        {
            HasRenderer = hasRenderer;
            RendererCount = rendererCount;
            MaterialSlotCount = materialSlotCount;
            Bounds = bounds;
            IsReadableOnBoard = isReadableOnBoard;
        }

        public bool HasRenderer { get; }

        public int RendererCount { get; }

        public int MaterialSlotCount { get; }

        public Bounds Bounds { get; }

        public bool IsReadableOnBoard { get; }
    }

    public static Report Evaluate(GameObject root)
    {
        if (root == null)
        {
            return new Report(false, 0, 0, new Bounds(Vector3.zero, Vector3.zero), false);
        }

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            return new Report(false, 0, 0, new Bounds(root.transform.position, Vector3.zero), false);
        }

        Bounds bounds = renderers[0].bounds;
        int materialSlots = 0;
        foreach (Renderer renderer in renderers)
        {
            bounds.Encapsulate(renderer.bounds);
            materialSlots += renderer.sharedMaterials.Length;
        }

        bool readable =
            bounds.size.y >= 0.7f &&
            bounds.size.y <= 2.2f &&
            bounds.size.x <= 2.2f &&
            bounds.size.z <= 2.2f;

        return new Report(true, renderers.Length, materialSlots, bounds, readable);
    }
}
