using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using UnityEngine;

public static class XRSimulatorSetup
{
    private const string PackageName = "com.unity.xr.interaction.toolkit";
    private const string SampleDisplayName = "XR Interaction Simulator";
    private const string SettingsAssetPath = "Assets/XRI/Settings/Resources/XRDeviceSimulatorSettings.asset";

    [MenuItem("Chess CGI/VR/Import XR Interaction Simulator Sample")]
    public static void ImportSimulatorSample()
    {
        UnityEditor.PackageManager.PackageInfo packageInfo = UnityEditor.PackageManager.PackageInfo.FindForPackageName(PackageName);
        if (packageInfo == null)
        {
            throw new System.InvalidOperationException($"Package {PackageName} is not resolved in this project.");
        }

        Sample sample = Sample.FindByPackage(PackageName, packageInfo.version)
            .FirstOrDefault(s => s.displayName == SampleDisplayName);
        if (sample.displayName == null)
        {
            throw new System.InvalidOperationException($"Sample \"{SampleDisplayName}\" was not found for {PackageName}@{packageInfo.version}.");
        }

        bool imported = sample.isImported || sample.Import(Sample.ImportOptions.OverridePreviousImports);
        AssetDatabase.Refresh();

        string prefabPath = FindSimulatorPrefabPath();
        if (prefabPath == null)
        {
            throw new System.InvalidOperationException("XR Interaction Simulator prefab was not found after importing the sample.");
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        ScriptableObject settings = AssetDatabase.LoadAssetAtPath<ScriptableObject>(SettingsAssetPath);
        if (settings == null)
        {
            throw new System.InvalidOperationException($"Could not load {SettingsAssetPath}.");
        }

        SerializedObject serializedSettings = new SerializedObject(settings);
        serializedSettings.FindProperty("m_SimulatorPrefab").objectReferenceValue = prefab;
        serializedSettings.FindProperty("m_AutomaticallyInstantiateSimulatorPrefab").boolValue = true;
        serializedSettings.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();

        Debug.Log($"CHESS_CGI_XR_SIMULATOR_SETUP imported={imported} prefabPath={prefabPath} autoInstantiate=True");
    }

    private static string FindSimulatorPrefabPath()
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Samples" });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = Path.GetFileNameWithoutExtension(path);
            if (fileName == "XR Interaction Simulator" || fileName == "XR Device Simulator")
            {
                return path;
            }
        }

        return null;
    }
}
