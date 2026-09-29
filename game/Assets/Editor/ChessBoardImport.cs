using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Imports decorative geometry while preserving the interactive board and material GUIDs.</summary>
public static class ChessBoardImport
{
    private const string ModelPath = "Assets/Art/Environment/ChessBoard/ChessBoard.glb";
    private const string PrefabPath = "Assets/Resources/Environment/ChessBoardFrame.prefab";

    [MenuItem("Chess CGI/Import Wooden Board")]
    public static void Build()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var importer = AssetImporter.GetAtPath(ModelPath);
        if (importer == null) throw new FileNotFoundException("Build the Blender board first.", ModelPath);
        var settings = new SerializedObject(importer);
        settings.FindProperty("importSettings.generateMipMaps").boolValue = true;
        settings.FindProperty("importSettings.anisotropicFilterLevel").intValue = 8;
        settings.FindProperty("importSettings.defaultMinFilterMode").intValue = 9987;
        settings.ApplyModifiedPropertiesWithoutUndo();
        importer.SaveAndReimport();
        Material[] materials = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<Material>().ToArray();
        UpdateSquareMaterial("Board_Light", materials.Single(m => m.name == "Board_Maple"));
        UpdateSquareMaterial("Board_Dark", materials.Single(m => m.name == "Board_Walnut"));
        var root = new GameObject("ChessBoardFrame");
        try
        {
            var geometry = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath), root.transform, false);
            geometry.name = "AuthoredFrame";
            // Matches the room importer: Blender -Y becomes Unity -Z, white's seat.
            geometry.transform.localRotation = Quaternion.Euler(0, 180, 0);
            foreach (Transform child in geometry.GetComponentsInChildren<Transform>())
                if (child.name.StartsWith("MaterialSample_", StringComparison.Ordinal))
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>())
            {
                renderer.shadowCastingMode = renderer.name == "Board_CoordinateIvory" ? ShadowCastingMode.Off : ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }
            if (root.GetComponentsInChildren<Collider>().Length != 0)
                throw new InvalidOperationException("The decorative frame must not intercept moves.");
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            AssetDatabase.SaveAssets();
            Debug.Log("CHESS_BOARD_IMPORTED coordinates=32 interactiveSquares=0");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    public static void BuildTabletop()
    {
        Build();
        FeevaleLabImport.Build();
    }

    private static void UpdateSquareMaterial(string name, Material source)
    {
        Material target = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/" + name + ".mat");
        if (target == null) throw new InvalidOperationException("Missing serialized board material: " + name);
        target.shader = source.shader;
        target.CopyPropertiesFromMaterial(source);
        target.enableInstancing = true;
        EditorUtility.SetDirty(target);
    }
}
