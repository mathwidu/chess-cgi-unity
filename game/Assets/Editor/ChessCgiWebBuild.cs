using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// A separate Web target. Native render settings are restored after the build.
public static class ChessCgiWebBuild
{
    private const string PipelinePath = "Assets/Settings/Web_RPAsset.asset";
    private const string MainScene = "Assets/Scenes/Main.unity";
    private static string NativeBackupDirectory => Path.GetFullPath(Path.Combine(Application.dataPath, "../../.local/unity-web-native-settings"));

    [MenuItem("Chess CGI/Build/Web prototype")]
    public static void Build()
    {
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
            throw new BuildFailedException("Install Web Build Support for Unity 6000.3.16f1 first.");
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
            throw new BuildFailedException("Launch with -buildTarget WebGL before invoking ChessCgiWebBuild.Build.");
        string repository = Directory.GetParent(Application.dataPath).Parent.FullName;
        string engine = Path.Combine(repository, ".local/stockfish-web/engine");
        if (!File.Exists(Path.Combine(engine, "stockfish-18-lite-single.wasm")))
            throw new BuildFailedException("Run python3 tools/setup_stockfish_web.py first.");

        using (var nativeSettings = new NativeRenderSettingsSnapshot())
        {
            Prepare();
            QualitySettings.SetQualityLevel(0, false);
            QualitySettings.renderPipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            string output = Path.Combine(repository, "Builds/Web");
            Directory.CreateDirectory(output);
            BuildReport report;
            using (ChessWebAssetOptimizer.Stage())
            {
                report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { MainScene }, locationPathName = output,
                    target = BuildTarget.WebGL, targetGroup = BuildTargetGroup.WebGL,
                    options = BuildOptions.DetailedBuildReport
                });
            }
            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException($"Web build: {report.summary.result}, {report.summary.totalErrors} errors.");
            CopyDirectory(engine, Path.Combine(output, "stockfish"));
            WriteAssetReport(report);
            Debug.Log($"CHESS_CGI_WEB_BUILD result={report.summary.result} bytes={report.summary.totalSize} warnings={report.summary.totalWarnings} path={output}");
        }
    }

    // URP's shader preprocessor saves prefilter flags even on other configured
    // pipelines. Restore both the in-memory quality slots and their original files.
    private sealed class NativeRenderSettingsSnapshot : IDisposable
    {
        private readonly Dictionary<string, byte[]> files = new Dictionary<string, byte[]>();
        private readonly string[] pipelineGuids;
        private readonly int qualityLevel;

        public NativeRenderSettingsSnapshot()
        {
            if (Directory.Exists(NativeBackupDirectory))
                throw new BuildFailedException("An interrupted Web build left native settings backups. Restore them with the Editor closed before building: " + NativeBackupDirectory);
            RequireSaved(QualitySettings.GetQualitySettings(), "QualitySettings");
            qualityLevel = QualitySettings.GetQualityLevel();
            var settings = new SerializedObject(QualitySettings.GetQualitySettings());
            SerializedProperty levels = settings.FindProperty("m_QualitySettings");
            pipelineGuids = new string[levels.arraySize];
            for (int i = 0; i < levels.arraySize; i++)
            {
                UnityEngine.Object pipeline = levels.GetArrayElementAtIndex(i).FindPropertyRelative("customRenderPipeline").objectReferenceValue;
                pipelineGuids[i] = pipeline == null ? "" : AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(pipeline));
            }
            files.Add("ProjectSettings/QualitySettings.asset", File.ReadAllBytes("ProjectSettings/QualitySettings.asset"));
            foreach (string guid in AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset", new[] { "Assets" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path == PipelinePath) continue;
                RequireSaved(AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(path), path);
                files.Add(path, File.ReadAllBytes(path));
            }
            // Keep recovery copies even if the Editor is terminated before finally runs.
            foreach (var file in files)
            {
                string backup = Path.Combine(NativeBackupDirectory, file.Key);
                Directory.CreateDirectory(Path.GetDirectoryName(backup));
                File.WriteAllBytes(backup, file.Value);
            }
            File.WriteAllLines(Path.Combine(NativeBackupDirectory, "files.txt"), files.Keys);
        }

        private static void RequireSaved(UnityEngine.Object asset, string label)
        {
            if (asset != null && EditorUtility.IsDirty(asset))
                throw new BuildFailedException("Save the existing native render settings before building Web: " + label);
        }

        public void Dispose()
        {
            var settings = new SerializedObject(QualitySettings.GetQualitySettings());
            SerializedProperty levels = settings.FindProperty("m_QualitySettings");
            if (levels.arraySize != pipelineGuids.Length)
                throw new BuildFailedException("Quality levels changed during the Web build; inspect native render settings.");
            for (int i = 0; i < pipelineGuids.Length; i++)
                levels.GetArrayElementAtIndex(i).FindPropertyRelative("customRenderPipeline").objectReferenceValue =
                    string.IsNullOrEmpty(pipelineGuids[i]) ? null :
                    AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(AssetDatabase.GUIDToAssetPath(pipelineGuids[i]));
            settings.ApplyModifiedPropertiesWithoutUndo();
            QualitySettings.SetQualityLevel(qualityLevel, false);
            AssetDatabase.SaveAssets();
            foreach (var file in files) File.WriteAllBytes(file.Key, file.Value);
            foreach (var file in files)
                if (file.Key.StartsWith("Assets/", StringComparison.Ordinal))
                    AssetDatabase.ImportAsset(file.Key, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            EditorUtility.ClearDirty(QualitySettings.GetQualitySettings());
            foreach (var file in files)
                if (!File.ReadAllBytes(file.Key).SequenceEqual(file.Value))
                    throw new BuildFailedException("Native render settings were not restored: " + file.Key);
            Directory.Delete(NativeBackupDirectory, true);
            Debug.Log("CHESS_WEB_NATIVE_RENDER_SETTINGS_RESTORED files=" + files.Count);
        }
    }

    public static void VerifyNativeSettingsRoundTrip()
    {
        using (var snapshot = new NativeRenderSettingsSnapshot())
        {
            QualitySettings.SetQualityLevel(0, false);
            QualitySettings.renderPipeline = null;
            AssetDatabase.SaveAssets();
        }
        Debug.Log("CHESS_WEB_NATIVE_SETTINGS_ROUND_TRIP_PASSED");
    }

    public static void AuditLastBuild() => WriteAssetReport(BuildReport.GetLatestReport());

    private static void WriteAssetReport(BuildReport report)
    {
        if (report == null) throw new BuildFailedException("No build report is available.");
        var totals = new Dictionary<string, ulong>();
        foreach (var packed in report.packedAssets)
        foreach (var entry in packed.contents)
        {
            string path = entry.sourceAssetPath ?? "(generated)";
            totals.TryGetValue(path, out ulong bytes);
            totals[path] = bytes + entry.packedSize;
        }
        var entries = new List<AssetSize>();
        foreach (var pair in totals) entries.Add(new AssetSize { path = pair.Key, bytes = pair.Value });
        entries.Sort((a, b) => b.bytes.CompareTo(a.bytes));
        string repository = Directory.GetParent(Application.dataPath).Parent.FullName;
        File.WriteAllText(Path.Combine(repository, "Builds/web-build-assets.json"),
            JsonUtility.ToJson(new AssetSizes { entries = entries.ToArray() }, true));
    }

    [Serializable] private sealed class AssetSize { public string path; public ulong bytes; }
    [Serializable] private sealed class AssetSizes { public AssetSize[] entries; }

    public static void Prepare()
    {
        var web = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
        if (web == null)
        {
            web = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset"));
            web.name = "Web_RPAsset";
            AssetDatabase.CreateAsset(web, PipelinePath);
        }
        web.supportsHDR = false;
        web.msaaSampleCount = 2;
        web.renderScale = 1;
        web.shadowDistance = 18;
        web.shadowCascadeCount = 1;
        web.mainLightShadowmapResolution = 1024;
        var webSettings = new SerializedObject(web);
        webSettings.FindProperty("m_SoftShadowsSupported").boolValue = false;
        webSettings.ApplyModifiedPropertiesWithoutUndo();
        web.maxAdditionalLightsCount = 2;
        web.supportsCameraDepthTexture = false;
        web.supportsCameraOpaqueTexture = false;
        EditorUtility.SetDirty(web);

        PlayerSettings.WebGL.template = "PROJECT:ChessWeb";
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = false;
        PlayerSettings.WebGL.dataCaching = true;
        PlayerSettings.WebGL.initialMemorySize = 128;
        PlayerSettings.WebGL.maximumMemorySize = 768;
        PlayerSettings.WebGL.memoryGrowthMode = WebGLMemoryGrowthMode.Geometric;
        PlayerSettings.WebGL.threadsSupport = false;
        PlayerSettings.WebGL.wasm2023 = false;
#if UNITY_WEBGL
        // Pin the shipping policy instead of inheriting the Editor's BuildTimes default.
        UnityEditor.WebGL.UserBuildSettings.codeOptimization = UnityEditor.WebGL.WasmCodeOptimization.DiskSizeLTO;
        Debug.Log("CHESS_WEB_CODE_OPTIMIZATION " + UnityEditor.WebGL.UserBuildSettings.codeOptimization);
#endif
        PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.WebGL, ManagedStrippingLevel.Medium);
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.WebGL, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.WebGL, new[] { GraphicsDeviceType.OpenGLES3 });
        EditorUserBuildSettings.webGLBuildSubtarget = WebGLTextureSubtarget.ETC2;

        // Only Web import overrides; original files and other platform overrides stay intact.
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Art", "Assets/Resources/UI" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!(AssetImporter.GetAtPath(path) is TextureImporter importer)) continue;
            var settings = importer.GetPlatformTextureSettings("WebGL");
            int size = path.Contains("MenuStudyBackground") ? 2048 : 1024;
            if (settings.overridden && settings.maxTextureSize == size && settings.format == TextureImporterFormat.Automatic) continue;
            settings.name = "WebGL";
            settings.overridden = true;
            settings.maxTextureSize = size;
            settings.format = TextureImporterFormat.Automatic;
            settings.textureCompression = TextureImporterCompression.Compressed;
            importer.SetPlatformTextureSettings(settings);
            importer.SaveAndReimport();
        }
        AssetDatabase.SaveAssets();
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (string path in Directory.GetFiles(source))
            File.Copy(path, Path.Combine(destination, Path.GetFileName(path)), true);
        foreach (string path in Directory.GetDirectories(source))
            CopyDirectory(path, Path.Combine(destination, Path.GetFileName(path)));
    }
}
