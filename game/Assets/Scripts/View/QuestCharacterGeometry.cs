using System.Collections.Generic;
using UnityEngine;

/// <summary>Shares reduced Android meshes while retaining the authored materials and physical fit.</summary>
internal static class QuestCharacterGeometry
{
    private static readonly Dictionary<ChessPieceKind, Dictionary<string, Mesh>> meshes = new();
    private static readonly Dictionary<ChessPieceKind, Dictionary<string, Mesh>> tableMeshes = new();
    private static readonly HashSet<ChessPieceKind> warned = new();
    private static readonly HashSet<ChessPieceKind> tableWarned = new();

    public static bool TryApply(ChessPieceKind kind, Transform visual)
    {
        if (!meshes.TryGetValue(kind, out Dictionary<string, Mesh> replacements))
        {
            replacements = Load(kind, "Geometry");
            meshes[kind] = replacements;
        }

        MeshFilter[] filters = visual.GetComponentsInChildren<MeshFilter>(true);
        if (replacements == null || filters.Length != replacements.Count)
            return KeepOriginal(kind);

        // Validate the entire character before touching it; an outdated export must not mix parts.
        foreach (MeshFilter filter in filters)
        {
            if (!replacements.TryGetValue(filter.name, out Mesh mesh) || mesh == null ||
                filter.sharedMesh == null || mesh.subMeshCount != filter.sharedMesh.subMeshCount ||
                filter.GetComponent<MeshRenderer>() == null)
                return KeepOriginal(kind);
        }

        foreach (MeshFilter filter in filters)
        {
            MeshRenderer renderer = filter.GetComponent<MeshRenderer>();
            Bounds authoredBounds = renderer.localBounds;
            filter.sharedMesh = replacements[filter.name];
            // Placement, palm support and culling continue to see the same authored extent.
            renderer.localBounds = authoredBounds;
        }
        ConfigureDistanceDetail(kind, visual, filters);
        return true;
    }

    private static void ConfigureDistanceDetail(ChessPieceKind kind, Transform visual, MeshFilter[] filters)
    {
        if (!tableMeshes.TryGetValue(kind, out Dictionary<string, Mesh> replacements))
        {
            replacements = Load(kind, "Table");
            if (replacements != null) tableMeshes[kind] = replacements;
        }
        if (replacements == null || replacements.Count != filters.Length)
        {
            KeepInspection(kind);
            return;
        }
        var high = new Mesh[filters.Length];
        var low = new Mesh[filters.Length];
        for (int i = 0; i < filters.Length; i++)
        {
            high[i] = filters[i].sharedMesh;
            if (!replacements.TryGetValue(filters[i].name, out low[i]))
            {
                KeepInspection(kind);
                return;
            }
        }
        QuestCharacterDetail detail = visual.GetComponent<QuestCharacterDetail>();
        if (detail == null) detail = visual.gameObject.AddComponent<QuestCharacterDetail>();
        if (!detail.Configure(filters, high, low))
        {
            detail.enabled = false;
            KeepInspection(kind);
        }
    }

    private static Dictionary<string, Mesh> Load(ChessPieceKind kind, string level)
    {
        GameObject asset = Resources.Load<GameObject>($"QuestCharacterGeometry/{kind}/{level}");
        if (asset == null) return null;
        var result = new Dictionary<string, Mesh>();
        foreach (MeshFilter filter in asset.GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter.sharedMesh == null || !result.TryAdd(filter.name, filter.sharedMesh)) return null;
        }
        return result;
    }

    private static void KeepInspection(ChessPieceKind kind)
    {
        if (tableWarned.Add(kind))
            Debug.LogWarning($"Quest table geometry unavailable or incompatible for {kind}; keeping inspection geometry.");
    }

    private static bool KeepOriginal(ChessPieceKind kind)
    {
        if (warned.Add(kind))
            Debug.LogWarning($"Quest geometry unavailable or incompatible for {kind}; keeping the original character.");
        return false;
    }
}
