using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Imports the metre-scale room without baking in a board or characters.</summary>
public static class FeevaleLabImport
{
    private const string ModelPath = "Assets/Art/Environment/FeevaleLab/FeevaleComputerLab.glb";
    private const string PrefabPath = "Assets/Resources/Environment/FeevaleComputerLab.prefab";

    [MenuItem("Chess CGI/Import Feevale Computer Lab")]
    public static void Build()
    {
        if (!File.Exists(ModelPath)) throw new FileNotFoundException("Build the Blender laboratory first.", ModelPath);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.ImportAsset(ModelPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        // Preserve the wood grain at a grazing tabletop angle, including in VR.
        var settings = new SerializedObject(AssetImporter.GetAtPath(ModelPath));
        settings.FindProperty("importSettings.generateMipMaps").boolValue = true;
        settings.FindProperty("importSettings.anisotropicFilterLevel").intValue = 8;
        settings.FindProperty("importSettings.defaultMinFilterMode").intValue = 9987; // glTF LINEAR_MIPMAP_LINEAR
        if (settings.ApplyModifiedPropertiesWithoutUndo())
            ((AssetImporter)settings.targetObject).SaveAndReimport();
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        if (model == null) throw new InvalidOperationException("The laboratory glTF did not import.");
        var root = new GameObject("FeevaleComputerLab");
        try
        {
            GameObject geometry = UnityEngine.Object.Instantiate(model, root.transform, false);
            geometry.name = "AuthoredRoom";
            // Blender +Y is the front wall. glTFast reflects X into Unity's handedness.
            geometry.transform.localRotation = Quaternion.Euler(0, 180, 0);
            int triangles = 0;
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                // Daylight comes from the existing rig. The ceiling must not block it.
                renderer.shadowCastingMode = renderer.name.StartsWith("Shell_", StringComparison.Ordinal)
                    || renderer.name.StartsWith("Signage_", StringComparison.Ordinal)
                    // The edge band and frame already cast the desk silhouette.
                    // Its thin veneer receives object shadows but must not self-shadow.
                    || renderer.name == "Furniture_NaturalAshDesk"
                    ? ShadowCastingMode.Off : ShadowCastingMode.On;
                renderer.receiveShadows = true;
                renderer.lightProbeUsage = LightProbeUsage.Off;
                foreach (Material material in renderer.sharedMaterials)
                    if (material == null || material.shader == null || material.shader.name.Contains("Error"))
                        throw new InvalidOperationException("Invalid laboratory material on " + renderer.name);
            }
            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>())
                for (int i = 0; i < filter.sharedMesh.subMeshCount; i++)
                    triangles += (int)filter.sharedMesh.GetIndexCount(i) / 3;
            Transform surface = root.GetComponentsInChildren<Transform>().Single(t => t.name == "ChessTableSurface");
            Transform window = root.GetComponentsInChildren<Transform>().Single(t => t.name == "WindowSide");
            if (Mathf.Abs(surface.position.y - .7557f) > .0001f || window.position.x > -3f)
                throw new InvalidOperationException("Laboratory axes or metre scale changed.");
            if (root.GetComponentsInChildren<Collider>().Length != 0)
                throw new InvalidOperationException("Decorative furniture must not intercept board interaction.");
            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"FEEVALE_LAB_IMPORTED renderers={renderers.Length} triangles={triangles} table={surface.position.y}");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }
}
