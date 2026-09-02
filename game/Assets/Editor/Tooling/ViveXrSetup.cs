using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEditor.XR.OpenXR;
using UnityEngine;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR.Features.Interactions;

public static class ViveXrSetup
{
    private const string OpenXrLoaderTypeName = "UnityEngine.XR.OpenXR.OpenXRLoader";

    [MenuItem("Chess CGI/VR/Configure OpenXR for Vive (Standalone)")]
    public static void ConfigureVive()
    {
        BuildTargetGroup standalone = BuildTargetGroup.Standalone;

        XRGeneralSettings generalSettings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(standalone);
        if (generalSettings == null)
        {
            throw new System.InvalidOperationException("No XRGeneralSettings for Standalone. Expected Assets/XR/XRGeneralSettingsPerBuildTarget.asset to already exist.");
        }

        if (generalSettings.AssignedSettings == null)
        {
            generalSettings.AssignedSettings = ScriptableObject.CreateInstance<XRManagerSettings>();
            EditorUtility.SetDirty(generalSettings);
        }

        XRManagerSettings manager = generalSettings.AssignedSettings;
        bool assigned = XRPackageMetadataStore.AssignLoader(manager, OpenXrLoaderTypeName, standalone);
        if (!assigned)
        {
            throw new System.InvalidOperationException("XRPackageMetadataStore.AssignLoader failed for OpenXRLoader on Standalone.");
        }

        manager.automaticLoading = true;
        manager.automaticRunning = true;
        EditorUtility.SetDirty(manager);

        OpenXRSettings openXrSettings = OpenXRSettings.GetSettingsForBuildTargetGroup(standalone);
        if (openXrSettings == null)
        {
            throw new System.InvalidOperationException("No OpenXRSettings for Standalone.");
        }

        HTCViveControllerProfile vive = openXrSettings.GetFeature<HTCViveControllerProfile>();
        if (vive == null)
        {
            throw new System.InvalidOperationException("HTCViveControllerProfile feature not found in OpenXRSettings for Standalone.");
        }
        vive.enabled = true;

        openXrSettings.renderMode = OpenXRSettings.RenderMode.SinglePassInstanced;
        EditorUtility.SetDirty(openXrSettings);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"CHESS_CGI_VIVE_SETUP loaderAssigned={assigned} automaticLoading={manager.automaticLoading} viveEnabled={vive.enabled} renderMode={openXrSettings.renderMode}");

        RunValidation();
    }

    [MenuItem("Chess CGI/VR/Run OpenXR Project Validation (Standalone)")]
    public static void RunValidation()
    {
        BuildTargetGroup standalone = BuildTargetGroup.Standalone;
        List<OpenXRFeature.ValidationRule> issues = new List<OpenXRFeature.ValidationRule>();
        OpenXRProjectValidation.GetCurrentValidationIssues(issues, standalone);

        if (issues.Count == 0)
        {
            Debug.Log("CHESS_CGI_VIVE_VALIDATION result=clean issues=0");
            return;
        }

        int errorCount = issues.Count(i => i.error);
        foreach (OpenXRFeature.ValidationRule issue in issues)
        {
            string level = issue.error ? "ERROR" : "WARNING";
            Debug.Log($"CHESS_CGI_VIVE_VALIDATION_ISSUE level={level} message=\"{issue.message}\"");
        }

        Debug.Log($"CHESS_CGI_VIVE_VALIDATION result={(errorCount > 0 ? "errors" : "warnings-only")} issues={issues.Count} errors={errorCount}");
    }
}
