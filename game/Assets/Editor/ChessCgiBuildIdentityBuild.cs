using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Covers the Quest helpers and ordinary Player builds, including incremental builds.
public sealed class ChessCgiBuildIdentityBuild : IPreprocessBuildWithReport
{
    private const string ResourceAsset = "Assets/Resources/ChessCgiBuildIdentity.json";
    public int callbackOrder => 2000;

    public void OnPreprocessBuild(BuildReport report)
    {
        bool android = report.summary.platform == BuildTarget.Android;
        if (android)
            ChessCgiQuestBuild.ValidateBuildVersion();

        var identity = new ChessCgiBuildIdentity
        {
            versionName = PlayerSettings.bundleVersion,
            androidVersionCode = android ? PlayerSettings.Android.bundleVersionCode : 0,
            builtAtUtc = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture),
            buildTarget = report.summary.platform.ToString()
        };
        string json = JsonUtility.ToJson(identity, true);
        File.WriteAllText(ResourceAsset, json + "\n", new UTF8Encoding(false));
        AssetDatabase.ImportAsset(ResourceAsset, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        TextAsset imported = AssetDatabase.LoadAssetAtPath<TextAsset>(ResourceAsset);
        if (imported == null || imported.text.Trim() != json)
            throw new BuildFailedException("Could not import the current build identity. Refusing to package an old timestamp.");

        Debug.Log($"CHESS_CGI_BUILD_IDENTITY target={identity.buildTarget} version={identity.versionName} code={identity.androidVersionCode} builtAtUtc={identity.builtAtUtc}");
    }
}
