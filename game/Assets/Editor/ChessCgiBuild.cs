using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class ChessCgiBuild
{
    private const string MainScene = "Assets/Scenes/Main.unity";
    private const string ProductName = "XadrezCGI";

    [MenuItem("Chess CGI/Build/macOS")]
    public static void BuildMacOS()
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string repositoryRoot = Directory.GetParent(projectRoot).FullName;
        string outputDirectory = Path.Combine(repositoryRoot, "Builds", "macOS");
        string outputPath = Path.Combine(outputDirectory, $"{ProductName}.app");

        Directory.CreateDirectory(outputDirectory);
        if (Directory.Exists(outputPath))
        {
            Directory.Delete(outputPath, true);
        }

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = new[] { MainScene },
            locationPathName = outputPath,
            target = BuildTarget.StandaloneOSX,
            targetGroup = BuildTargetGroup.Standalone,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;
        Debug.Log($"CHESS_CGI_BUILD_RESULT result={summary.result} path={outputPath} size={summary.totalSize} warnings={summary.totalWarnings} errors={summary.totalErrors}");

        if (summary.result != BuildResult.Succeeded)
        {
            throw new InvalidOperationException($"macOS build failed with result {summary.result} and {summary.totalErrors} errors.");
        }
    }
}
