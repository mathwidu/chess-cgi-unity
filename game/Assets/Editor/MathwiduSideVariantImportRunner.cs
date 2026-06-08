using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MathwiduSideVariantImportRunner
{
    private const string MainScenePath = "Assets/Scenes/Main.unity";

    public static void ImportAndWirePawnSideVariants()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        GameObject whitePrefab = ImportSideVariant("White");
        GameObject blackPrefab = ImportSideVariant("Black");

        UnityEngine.SceneManagement.Scene scene = EditorSceneManager.OpenScene(MainScenePath);
        PieceFactory factory = Object.FindFirstObjectByType<PieceFactory>();
        if (factory == null)
        {
            throw new IOException("PieceFactory not found in Main scene.");
        }

        SerializedObject serializedFactory = new SerializedObject(factory);
        serializedFactory.FindProperty("whitePawnPrefab").objectReferenceValue = whitePrefab;
        serializedFactory.FindProperty("blackPawnPrefab").objectReferenceValue = blackPrefab;
        serializedFactory.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(factory);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
    }

    private static GameObject ImportSideVariant(string side)
    {
        string candidateModelPath = $"Assets/Art/CharacterCandidates/Pawn_Mathwidu/side_variants/{side}/Pawn_Mathwidu_{side}.glb";
        string importedFolderPath = $"Assets/Resources/CustomPieces/Pawn_Mathwidu_{side}_Assets";
        string importedModelPath = $"{importedFolderPath}/selected.glb";
        string importedPrefabPath = $"Assets/Resources/CustomPieces/Pawn_Mathwidu_{side}.prefab";

        EnsureFolder("Assets/Resources", "CustomPieces");
        EnsureFolder("Assets/Resources/CustomPieces", $"Pawn_Mathwidu_{side}_Assets");

        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string candidateModelFile = Path.Combine(projectRoot, candidateModelPath);
        string importedFolder = Path.Combine(projectRoot, importedFolderPath);
        string importedModelFile = Path.Combine(projectRoot, importedModelPath);

        if (!File.Exists(candidateModelFile))
        {
            throw new FileNotFoundException("Mathwidu side variant candidate model not found.", candidateModelPath);
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

        instance.name = $"Pawn_Mathwidu_{side}";
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
