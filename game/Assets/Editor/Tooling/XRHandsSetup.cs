using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine;
using UnityEngine.XR.Hands.OpenXR;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;

public static class XRHandsSetup
{
    private const string PackageName = "com.unity.xr.interaction.toolkit";
    private const string BaseRigSampleDisplayName = "Starter Assets";
    private const string HandsSampleDisplayName = "Hands Interaction Demo";
    private const string RigPrefabName = "XR Origin Hands (XR Rig)";
    private const string LeftHandPrefabPath = "Assets/Resources/XR/LeftHandInteractor.prefab";
    private const string RightHandPrefabPath = "Assets/Resources/XR/RightHandInteractor.prefab";
    private static readonly string[] MissingVisualNameFragments = { "Quest Visual", "Android XR Visual" };

    [MenuItem("Chess CGI/VR/Import XR Hands Sample And Extract Interactors")]
    public static void ImportAndExtractHandInteractors()
    {
        UnityEditor.PackageManager.PackageInfo packageInfo = UnityEditor.PackageManager.PackageInfo.FindForPackageName(PackageName);
        if (packageInfo == null)
        {
            throw new System.InvalidOperationException($"Package {PackageName} is not resolved in this project.");
        }

        Sample[] samples = Sample.FindByPackage(PackageName, packageInfo.version).ToArray();

        // The Hands Interaction Demo rig is a Prefab Variant of Starter Assets' base
        // XR Origin (XR Rig) prefab, so that sample must also be present for it to import cleanly.
        ImportSample(samples, BaseRigSampleDisplayName);
        ImportSample(samples, HandsSampleDisplayName);
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

        GameObject rigPrefab = FindPrefabByName(RigPrefabName);
        if (rigPrefab == null)
        {
            throw new System.InvalidOperationException($"\"{RigPrefabName}\" prefab was not found after importing the samples.");
        }

        Transform cameraOffset = rigPrefab.transform.Find("Camera Offset");
        ExtractHandInteractor(cameraOffset, "Left Hand", LeftHandPrefabPath);
        ExtractHandInteractor(cameraOffset, "Right Hand", RightHandPrefabPath);

        AssetDatabase.SaveAssets();
        Debug.Log($"CHESS_CGI_XR_HANDS_SETUP extracted leftHand={LeftHandPrefabPath} rightHand={RightHandPrefabPath}");
    }

    [MenuItem("Chess CGI/VR/Enable OpenXR Hand Tracking Feature")]
    public static void EnableHandTrackingFeature()
    {
        FeatureHelpers.RefreshFeatures(BuildTargetGroup.Standalone);
        OpenXRFeature feature = FeatureHelpers.GetFeatureWithIdForBuildTarget(BuildTargetGroup.Standalone, HandTracking.featureId2);
        if (feature == null)
        {
            throw new System.InvalidOperationException(
                $"Could not find the Hand Tracking Subsystem feature ({HandTracking.featureId2}) for Standalone. Is com.unity.xr.hands installed?");
        }

        feature.enabled = true;
        EditorUtility.SetDirty(feature);
        AssetDatabase.SaveAssets();

        Debug.Log($"CHESS_CGI_XR_HANDS_SETUP handTrackingFeatureEnabled={feature.enabled}");
    }

    private static void ImportSample(Sample[] samples, string displayName)
    {
        Sample sample = samples.FirstOrDefault(s => s.displayName == displayName);
        if (sample.displayName == null)
        {
            throw new System.InvalidOperationException($"Sample \"{displayName}\" was not found for {PackageName}.");
        }

        if (!sample.isImported)
        {
            sample.Import(Sample.ImportOptions.OverridePreviousImports);
        }
    }

    private static GameObject FindPrefabByName(string name)
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Samples" });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileNameWithoutExtension(path) == name)
            {
                return AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
        }

        return null;
    }

    private static void ExtractHandInteractor(Transform cameraOffset, string childName, string destinationPath)
    {
        Transform source = cameraOffset != null ? cameraOffset.Find(childName) : null;
        if (source == null)
        {
            throw new System.InvalidOperationException($"Could not find \"{childName}\" under \"{cameraOffset?.name}\".");
        }

        GameObject instance = Object.Instantiate(source.gameObject);
        instance.name = childName;

        for (int i = instance.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = instance.transform.GetChild(i);
            if (MissingVisualNameFragments.Any(fragment => child.name.Contains(fragment)))
            {
                Object.DestroyImmediate(child.gameObject);
            }
        }

        string directory = System.IO.Path.GetDirectoryName(destinationPath);
        if (!AssetDatabase.IsValidFolder(directory))
        {
            System.IO.Directory.CreateDirectory(directory);
        }

        PrefabUtility.SaveAsPrefabAsset(instance, destinationPath);
        Object.DestroyImmediate(instance);
    }
}
