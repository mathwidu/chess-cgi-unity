#if UNITY_EDITOR_OSX
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class EngineBenchmarkBuild
{
    private const string MainScene = "Assets/Scenes/Main.unity";
    private const string ProductName = "XadrezCGI";

    [MenuItem("Chess CGI/Build/Engine benchmark (macOS Mono)")]
    public static void BuildMacMono() => BuildMac(ScriptingImplementation.Mono2x, "mono");

    [MenuItem("Chess CGI/Build/Engine benchmark (macOS IL2CPP)")]
    public static void BuildMacIl2cpp() => BuildMac(ScriptingImplementation.IL2CPP, "il2cpp");

    private static void BuildMac(ScriptingImplementation backend, string name)
    {
        string repositoryRoot = Directory.GetParent(Directory.GetParent(Application.dataPath).FullName).FullName;
        string outputPath = Path.Combine(repositoryRoot, "Builds", "EngineBenchmark", name, $"{ProductName}.app");
        if (Directory.Exists(outputPath))
        {
            Directory.Delete(outputPath, true);
        }

        ScriptingImplementation previousBackend = PlayerSettings.GetScriptingBackend(NamedBuildTarget.Standalone);
        OSArchitecture previousArchitecture = UnityEditor.OSXStandalone.UserBuildSettings.architecture;
        try
        {
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, backend);
            UnityEditor.OSXStandalone.UserBuildSettings.architecture = OSArchitecture.ARM64;
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { MainScene },
                locationPathName = outputPath,
                target = BuildTarget.StandaloneOSX,
                targetGroup = BuildTargetGroup.Standalone,
                extraScriptingDefines = new[] { "CHESS_ENGINE_BENCHMARK" },
                options = BuildOptions.None
            });
            BuildSummary summary = report.summary;
            Debug.Log($"ENGINE_BENCHMARK_BUILD result={summary.result} path={outputPath} errors={summary.totalErrors}");
            if (summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException($"Engine benchmark build failed with result {summary.result}.");
            }
        }
        finally
        {
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, previousBackend);
            UnityEditor.OSXStandalone.UserBuildSettings.architecture = previousArchitecture;
        }
    }
}
#endif
