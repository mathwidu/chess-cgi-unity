using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CustomPieceSideVariantImportRunner
{
    private const string MainScenePath = "Assets/Scenes/Main.unity";

    private readonly struct VariantSpec
    {
        public VariantSpec(string characterId, string whiteProperty, string blackProperty)
        {
            CharacterId = characterId;
            WhiteProperty = whiteProperty;
            BlackProperty = blackProperty;
        }

        public string CharacterId { get; }

        public string WhiteProperty { get; }

        public string BlackProperty { get; }
    }

    private static readonly IReadOnlyList<VariantSpec> Variants = new[]
    {
        new VariantSpec("Pawn_Mathwidu", "whitePawnPrefab", "blackPawnPrefab"),
        new VariantSpec("Rook_Alex", "whiteRookPrefab", "blackRookPrefab"),
        new VariantSpec("Knight_Gustavo", "whiteKnightPrefab", "blackKnightPrefab"),
        new VariantSpec("Bishop_Rafael", "whiteBishopPrefab", "blackBishopPrefab"),
        new VariantSpec("Queen_Marta", "whiteQueenPrefab", "blackQueenPrefab"),
        new VariantSpec("King_Ricardo_Carioca", "whiteKingPrefab", "blackKingPrefab")
    };

    public static void ImportAndWireAllSideVariants()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Dictionary<string, GameObject> importedPrefabs = new Dictionary<string, GameObject>();
        foreach (VariantSpec variant in Variants)
        {
            importedPrefabs[$"{variant.CharacterId}_White"] = ImportSideVariant(variant.CharacterId, "White");
            importedPrefabs[$"{variant.CharacterId}_Black"] = ImportSideVariant(variant.CharacterId, "Black");
        }

        UnityEngine.SceneManagement.Scene scene = EditorSceneManager.OpenScene(MainScenePath);
        PieceFactory factory = Object.FindFirstObjectByType<PieceFactory>();
        if (factory == null)
        {
            throw new IOException("PieceFactory not found in Main scene.");
        }

        SerializedObject serializedFactory = new SerializedObject(factory);
        foreach (VariantSpec variant in Variants)
        {
            serializedFactory.FindProperty(variant.WhiteProperty).objectReferenceValue = importedPrefabs[$"{variant.CharacterId}_White"];
            serializedFactory.FindProperty(variant.BlackProperty).objectReferenceValue = importedPrefabs[$"{variant.CharacterId}_Black"];
        }

        serializedFactory.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(factory);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
    }

    private static GameObject ImportSideVariant(string characterId, string side)
    {
        string candidateModelPath = $"Assets/Art/CharacterCandidates/{characterId}/side_variants/{side}/{characterId}_{side}.glb";
        string importedFolderPath = $"Assets/Resources/CustomPieces/{characterId}_{side}_Assets";
        string importedModelPath = $"{importedFolderPath}/selected.glb";
        string importedPrefabPath = $"Assets/Resources/CustomPieces/{characterId}_{side}.prefab";

        EnsureFolder("Assets/Resources", "CustomPieces");
        EnsureFolder("Assets/Resources/CustomPieces", $"{characterId}_{side}_Assets");

        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string candidateModelFile = Path.Combine(projectRoot, candidateModelPath);
        string importedFolder = Path.Combine(projectRoot, importedFolderPath);
        string importedModelFile = Path.Combine(projectRoot, importedModelPath);

        if (!File.Exists(candidateModelFile))
        {
            throw new FileNotFoundException("Side variant candidate model not found.", candidateModelPath);
        }

        Directory.CreateDirectory(importedFolder);
        File.Copy(candidateModelFile, importedModelFile, true);
        AssetDatabase.ImportAsset(importedModelPath, ImportAssetOptions.ForceSynchronousImport);
        GameObject importedModel = AssetDatabase.LoadAssetAtPath<GameObject>(importedModelPath);
        if (importedModel == null)
        {
            throw new IOException($"Could not load imported model at {importedModelPath}");
        }

        GameObject instance = Object.Instantiate(importedModel);
        if (instance == null)
        {
            throw new IOException($"Could not instantiate imported model at {importedModelPath}");
        }

        instance.name = $"{characterId}_{side}";
        PrefabUtility.SaveAsPrefabAsset(instance, importedPrefabPath);
        Object.DestroyImmediate(instance);

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(importedPrefabPath);
        if (prefab == null)
        {
            throw new IOException($"Could not load created prefab at {importedPrefabPath}");
        }

        return prefab;
    }

    private static void EnsureFolder(string parent, string folder)
    {
        string path = $"{parent}/{folder}";
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, folder);
        }
    }
}
