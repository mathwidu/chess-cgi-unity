using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.XR.CoreUtils.Editor;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEditor.XR.OpenXR;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Hands.OpenXR;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR.Features.Interactions;
using UnityEngine.XR.OpenXR.Features.MetaQuestSupport;

public static class ChessCgiQuestBuild
{
    private const BuildTargetGroup TargetGroup = BuildTargetGroup.Android;
    private const string MainScene = "Assets/Scenes/Main.unity";
    private const string ApplicationIdentifier = "com.mathwidu.chesscgi";
    private const string OpenXrLoaderTypeName = "UnityEngine.XR.OpenXR.OpenXRLoader";
    private const string MockRuntimeFeatureId = "com.unity.openxr.feature.mockruntime";

    // Deliberately pin the first development APK to installed API 35 rather than Auto.
    // This is a local development target, not a store-submission policy declaration.
    private const AndroidSdkVersions TargetSdk = AndroidSdkVersions.AndroidApiLevel35;

    [MenuItem("Chess CGI/Build/Quest 3/Prepare Android")]
    public static void Prepare()
    {
        RequireAndroidEditor();

        XRGeneralSettingsPerBuildTarget settings = GetXrSettingsContainer();
        if (!settings.HasSettingsForBuildTarget(TargetGroup))
            settings.CreateDefaultSettingsForBuildTarget(TargetGroup);

        XRGeneralSettings general = settings.SettingsForBuildTarget(TargetGroup);
        XRGeneralSettings standalone = settings.SettingsForBuildTarget(BuildTargetGroup.Standalone);
        if (general == standalone)
            throw new BuildFailedException("Android and Standalone share XRGeneralSettings. Separate them before preparing Quest.");

        if (!settings.HasManagerSettingsForBuildTarget(TargetGroup))
            settings.CreateDefaultManagerSettingsForBuildTarget(TargetGroup);

        XRManagerSettings manager = general.Manager;
        if (standalone != null && manager == standalone.Manager)
            throw new BuildFailedException("Android and Standalone share XRManagerSettings. Separate them before preparing Quest.");

        // Assign only to Android; do not replace the loader list or alter Standalone.
        if (!manager.activeLoaders.Any(loader => loader is OpenXRLoader) &&
            !XRPackageMetadataStore.AssignLoader(manager, OpenXrLoaderTypeName, TargetGroup))
            throw new BuildFailedException("Could not assign the OpenXR loader to Android.");

        general.InitManagerOnStart = true;
        manager.automaticLoading = true;
        manager.automaticRunning = true;
        EditorUtility.SetDirty(general);
        EditorUtility.SetDirty(manager);
        EditorUtility.SetDirty(settings);

        // The public package API creates missing Android settings and feature assets.
        FeatureHelpers.RefreshFeatures(TargetGroup);
        OpenXRSettings openXr = GetOpenXrSettings();
        SetFeatureEnabled<MetaQuestFeature>(openXr, true);
        SetFeatureEnabled<FoveatedRenderingFeature>(openXr, true);
        openXr.foveatedRenderingApi = OpenXRSettings.BackendFovationApi.SRPFoveation;
        // Meta Quest Support copies its setting into OpenXR during validation.
        // The package exposes this choice only through a serialized Editor field.
        var questSettings = new SerializedObject(openXr.GetFeature<MetaQuestFeature>());
        SerializedProperty foveationApi = questSettings.FindProperty("m_foveatedRenderingApi");
        if (foveationApi == null)
            throw new BuildFailedException("Meta Quest Support no longer exposes its serialized foveation API; review the installed OpenXR package.");
        foveationApi.intValue = (int)OpenXRSettings.BackendFovationApi.SRPFoveation;
        questSettings.ApplyModifiedPropertiesWithoutUndo();
        SetFeatureEnabled<OculusTouchControllerProfile>(openXr, true);
        SetFeatureEnabled<MetaQuestTouchPlusControllerProfile>(openXr, true);
        SetFeatureEnabled<HandTracking>(openXr, true);
        SetFeatureEnabled<MetaHandTrackingAim>(openXr, true);
        SetFeatureEnabled<HandInteractionProfile>(openXr, true);
        // MockRuntime lives in an assembly with autoReferenced=false; address the
        // optional test feature by its public feature ID, without a test dependency.
        OpenXRFeature mockRuntime = FeatureHelpers.GetFeatureWithIdForBuildTarget(TargetGroup, MockRuntimeFeatureId);
        if (mockRuntime != null)
        {
            mockRuntime.enabled = false;
            EditorUtility.SetDirty(mockRuntime);
        }
        openXr.renderMode = OpenXRSettings.RenderMode.SinglePassInstanced;
        EditorUtility.SetDirty(openXr);
        // Recompute the custom loader selection after disabling any existing Mock Runtime.
        FeatureHelpers.RefreshFeatures(TargetGroup);

        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, ApplicationIdentifier);
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
        PlayerSettings.Android.targetSdkVersion = TargetSdk;
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan });
        EditorUserBuildSettings.buildAppBundle = false;
        EditorUserBuildSettings.exportAsGoogleAndroidProject = false;

        AssetDatabase.SaveAssets();
        Validate();
        Debug.Log("CHESS_CGI_QUEST_PREPARE result=prepared target=Android architecture=ARM64 backend=IL2CPP minSdk=29 targetSdk=35 graphics=Vulkan");
    }

    [MenuItem("Chess CGI/Build/Quest 3/Validate Android")]
    public static void Validate()
    {
        RequireAndroidEditor();
        var errors = new List<string>();
        Check(AssetDatabase.LoadAssetAtPath<SceneAsset>(MainScene) != null, $"Main scene is missing: {MainScene}", errors);
        Check(PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android) == ApplicationIdentifier,
            $"Android application identifier must be {ApplicationIdentifier}.", errors);
        Check(PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android) == ScriptingImplementation.IL2CPP,
            "Android scripting backend must be IL2CPP.", errors);
        Check(PlayerSettings.Android.targetArchitectures == AndroidArchitecture.ARM64, "Android architecture must be ARM64 only.", errors);
        Check(PlayerSettings.Android.minSdkVersion == AndroidSdkVersions.AndroidApiLevel29, "Android minimum SDK must be API 29.", errors);
        Check(PlayerSettings.Android.targetSdkVersion == TargetSdk, "Android target SDK must be explicitly API 35.", errors);
        Check(!PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.Android) &&
              PlayerSettings.GetGraphicsAPIs(BuildTarget.Android).SequenceEqual(new[] { GraphicsDeviceType.Vulkan }),
            "Android graphics API must be explicitly Vulkan only.", errors);
        Check(!EditorUserBuildSettings.buildAppBundle && !EditorUserBuildSettings.exportAsGoogleAndroidProject,
            "Android output must be an APK, without Gradle project export.", errors);

        XRGeneralSettings general = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(TargetGroup);
        XRManagerSettings manager = general != null ? general.Manager : null;
        Check(general != null && general.InitManagerOnStart && manager != null &&
              manager.automaticLoading && manager.automaticRunning,
            "Android XR initialization, loading and running must be automatic.", errors);
        bool hasOpenXrLoader = manager != null && manager.activeLoaders.Any(loader => loader is OpenXRLoader);
        Check(hasOpenXrLoader, "Android must have the OpenXR loader assigned.", errors);
        Check(manager != null && manager.activeLoaders.FirstOrDefault() is OpenXRLoader,
            "OpenXR must be the first Android XR loader. Review any existing provider order explicitly.", errors);

        OpenXRSettings openXr = OpenXRSettings.GetSettingsForBuildTargetGroup(TargetGroup);
        Check(openXr != null, "Android OpenXR settings are missing. Run Prepare first.", errors);
        if (openXr != null)
        {
            Check(openXr.renderMode == OpenXRSettings.RenderMode.SinglePassInstanced,
                "Android OpenXR render mode must be Single Pass Instanced / Multi-view.", errors);
            CheckFeatureEnabled<MetaQuestFeature>(openXr, errors);
            CheckFeatureEnabled<FoveatedRenderingFeature>(openXr, errors);
            Check(openXr.foveatedRenderingApi == OpenXRSettings.BackendFovationApi.SRPFoveation,
                "Android OpenXR must use SRP Foveation for render comparisons.", errors);
            CheckFeatureEnabled<OculusTouchControllerProfile>(openXr, errors);
            CheckFeatureEnabled<MetaQuestTouchPlusControllerProfile>(openXr, errors);
            CheckFeatureEnabled<HandTracking>(openXr, errors);
            CheckFeatureEnabled<MetaHandTrackingAim>(openXr, errors);
            CheckFeatureEnabled<HandInteractionProfile>(openXr, errors);
            OpenXRFeature mockRuntime = FeatureHelpers.GetFeatureWithIdForBuildTarget(TargetGroup, MockRuntimeFeatureId);
            Check(mockRuntime == null || !mockRuntime.enabled,
                "Mock Runtime must be disabled for Android.", errors);
        }

        var loggedIssues = new HashSet<string>();
        if (hasOpenXrLoader && openXr != null)
        {
            // Evaluate current features directly: CoreUtils may have registered rules
            // before Prepare created the Android feature assets in this Editor session.
            var openXrIssues = new List<OpenXRFeature.ValidationRule>();
            OpenXRProjectValidation.GetCurrentValidationIssues(openXrIssues, TargetGroup);
            foreach (OpenXRFeature.ValidationRule issue in openXrIssues)
                RecordValidationIssue(issue.message, issue.error, "OpenXR", loggedIssues, errors);
        }

        // CoreUtils also holds XRI/XR Hands rules. Its query is internal in 2.6.0;
        // use the same narrow reflection boundary as XRProjectValidationCheck.
        MethodInfo getIssues = typeof(BuildValidator).GetMethod("GetCurrentValidationIssues",
            BindingFlags.NonPublic | BindingFlags.Static, null,
            new[] { typeof(HashSet<BuildValidationRule>), typeof(BuildTargetGroup) }, null);
        if (getIssues == null)
            throw new BuildFailedException("Cannot query Android XR project validation: BuildValidator.GetCurrentValidationIssues was not found.");
        var xrIssues = new HashSet<BuildValidationRule>();
        getIssues.Invoke(null, new object[] { xrIssues, TargetGroup });
        foreach (BuildValidationRule issue in xrIssues)
            RecordValidationIssue(issue.Message, issue.Error, issue.Category, loggedIssues, errors);

        ChessCgiStockfishAndroidBuild.ValidatePayload();
        Debug.Log("CHESS_CGI_QUEST_DEVICE_CHECK Offline AI is packaged for Android; launch, lifecycle and XR performance still require a headset session.");
        foreach (string error in errors)
            Debug.LogError($"CHESS_CGI_QUEST_VALIDATION_ERROR {error}");
        Debug.Log($"CHESS_CGI_QUEST_VALIDATION result={(errors.Count == 0 ? "passed" : "failed")} target=Android errors={errors.Count} xrIssues={loggedIssues.Count}");
        if (errors.Count > 0)
            throw new BuildFailedException($"Quest validation failed with {errors.Count} blocking issue(s). See CHESS_CGI_QUEST_VALIDATION_ERROR in the Editor log.");
    }

    [MenuItem("Chess CGI/Build/Quest 3/Build Development APK")]
    public static void Build()
    {
        BuildApk(true);
    }

    [MenuItem("Chess CGI/Build/Quest 3/Build Demo APK")]
    public static void BuildDemo()
    {
        BuildApk(false);
    }

    private static void BuildApk(bool development)
    {
        Validate();
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string repositoryRoot = Directory.GetParent(projectRoot).FullName;
        string outputDirectory = Path.Combine(repositoryRoot, "Builds", "Quest3");
        string outputPath = Path.Combine(outputDirectory,
            development ? "XadrezCGI-Quest3-dev.apk" : "XadrezCGI-Quest3-demo.apk");
        Directory.CreateDirectory(outputDirectory);

        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { MainScene },
            locationPathName = outputPath,
            target = BuildTarget.Android,
            targetGroup = TargetGroup,
            options = development ? BuildOptions.Development : BuildOptions.None
        });
        BuildSummary summary = report.summary;
        Debug.Log($"CHESS_CGI_QUEST_BUILD_RESULT result={summary.result} path={outputPath} size={summary.totalSize} warnings={summary.totalWarnings} errors={summary.totalErrors}");
        if (summary.result != BuildResult.Succeeded)
            throw new BuildFailedException($"Quest APK build failed with result {summary.result} and {summary.totalErrors} errors.");
    }

    private static void RequireAndroidEditor()
    {
        if (!BuildPipeline.IsBuildTargetSupported(TargetGroup, BuildTarget.Android))
            throw new BuildFailedException("Android Build Support is missing for this Unity Editor. Install its Android module, SDK/NDK and OpenJDK before preparing Quest.");
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            throw new BuildFailedException("Android must already be the active build target. Select Android in Build Profiles, or launch batch mode with -buildTarget Android before -executeMethod ChessCgiQuestBuild.Prepare (or Validate/Build).");
        if (EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer)
            throw new BuildFailedException("Quest preparation, validation and build must run outside Play mode and outside another build.");
    }

    private static XRGeneralSettingsPerBuildTarget GetXrSettingsContainer()
    {
        if (!EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out XRGeneralSettingsPerBuildTarget settings) || settings == null)
            throw new BuildFailedException("The existing XRGeneralSettingsPerBuildTarget config object is missing. Restore/register the project's XR settings before preparing Quest; Standalone settings will not be replaced.");
        return settings;
    }

    private static OpenXRSettings GetOpenXrSettings()
    {
        OpenXRSettings settings = OpenXRSettings.GetSettingsForBuildTargetGroup(TargetGroup);
        if (settings == null)
            throw new BuildFailedException("Could not create Android OpenXR settings. Check the Android module and resolved OpenXR package.");
        return settings;
    }

    private static void SetFeatureEnabled<T>(OpenXRSettings settings, bool enabled) where T : OpenXRFeature
    {
        T feature = settings.GetFeature<T>();
        if (feature == null)
            throw new BuildFailedException($"Android OpenXR feature {typeof(T).Name} is unavailable in the installed packages.");
        feature.enabled = enabled;
        EditorUtility.SetDirty(feature);
    }

    private static void CheckFeatureEnabled<T>(OpenXRSettings settings, List<string> errors) where T : OpenXRFeature
    {
        T feature = settings.GetFeature<T>();
        Check(feature != null && feature.enabled, $"Android OpenXR feature {typeof(T).Name} must be enabled.", errors);
    }

    private static void Check(bool passed, string message, List<string> errors)
    {
        if (!passed)
            errors.Add(message);
    }

    private static void RecordValidationIssue(string message, bool blocking, string category,
        HashSet<string> loggedIssues, List<string> errors)
    {
        if (!loggedIssues.Add($"{blocking}:{message}"))
            return;
        string detail = $"category=\"{category}\" message=\"{message}\"";
        if (blocking)
            errors.Add(detail);
        else
            Debug.LogWarning($"CHESS_CGI_QUEST_VALIDATION_WARNING {detail}");
    }
}
