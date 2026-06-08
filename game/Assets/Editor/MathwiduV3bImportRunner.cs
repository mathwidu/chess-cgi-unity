using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MathwiduV3bImportRunner
{
    private const string CandidateModelPath = "Assets/Art/CharacterCandidates/Pawn_Mathwidu/v3b/Pawn_Mathwidu_v3b.glb";
    private const string ImportedFolderPath = "Assets/Resources/CustomPieces/Pawn_Mathwidu_v3b_Assets";
    private const string ImportedModelPath = ImportedFolderPath + "/selected.glb";
    private const string ImportedPrefabPath = "Assets/Resources/CustomPieces/Pawn_Mathwidu_v3b.prefab";
    private const string MainScenePath = "Assets/Scenes/Main.unity";

    public static void ImportAndWirePawn()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        EnsureFolder("Assets/Resources", "CustomPieces");
        EnsureFolder("Assets/Resources/CustomPieces", "Pawn_Mathwidu_v3b_Assets");

        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string candidateModelFile = Path.Combine(projectRoot, CandidateModelPath);
        string importedFolder = Path.Combine(projectRoot, ImportedFolderPath);
        string importedModelFile = Path.Combine(projectRoot, ImportedModelPath);

        if (!File.Exists(candidateModelFile))
        {
            throw new FileNotFoundException("Mathwidu v3b candidate model not found.", CandidateModelPath);
        }

        Directory.CreateDirectory(importedFolder);
        File.Copy(candidateModelFile, importedModelFile, true);
        AssetDatabase.ImportAsset(ImportedModelPath, ImportAssetOptions.ForceSynchronousImport);
        GameObject importedModel = AssetDatabase.LoadAssetAtPath<GameObject>(ImportedModelPath);
        if (importedModel == null)
        {
            throw new IOException($"Could not load imported model at {ImportedModelPath}");
        }

        GameObject instance = Object.Instantiate(importedModel);
        if (instance == null)
        {
            throw new IOException($"Could not instantiate imported model at {ImportedModelPath}");
        }

        instance.name = "Pawn_Mathwidu_v3b";
        PrefabUtility.SaveAsPrefabAsset(instance, ImportedPrefabPath);
        Object.DestroyImmediate(instance);

        GameObject pawnPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ImportedPrefabPath);
        if (pawnPrefab == null)
        {
            throw new IOException($"Could not load created prefab at {ImportedPrefabPath}");
        }

        UnityEngine.SceneManagement.Scene scene = EditorSceneManager.OpenScene(MainScenePath);
        PieceFactory factory = Object.FindFirstObjectByType<PieceFactory>();
        if (factory == null)
        {
            throw new IOException("PieceFactory not found in Main scene.");
        }

        SerializedObject serializedFactory = new SerializedObject(factory);
        serializedFactory.FindProperty("pawnPrefab").objectReferenceValue = pawnPrefab;
        serializedFactory.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(factory);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
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
