using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Runs for menu, wrapper and ordinary Unity Android builds.
public sealed class ChessCgiStockfishAndroidBuild : IPreprocessBuildWithReport, IPostGenerateGradleAndroidProject
{
    public const string EngineAsset = "Assets/Plugins/Android/Stockfish/arm64-v8a/libstockfish.so";
    private const string Notices = "Assets/StreamingAssets/ThirdParty/Stockfish/";
    private const string AndroidNamespace = "http://schemas.android.com/apk/res/android";
    private const string GradleMarker = "// Chess CGI: packaged offline Stockfish executable";
    public int callbackOrder => 1000;

    [Serializable]
    private sealed class EngineManifest
    {
        public string engine;
        public string binary_sha256;
        public string source;
        public string license;
    }

    public static void ValidatePayload()
    {
        if (!File.Exists(EngineAsset) || !File.Exists(Notices + "manifest.json"))
            throw new BuildFailedException("Prepare the offline Android engine first: python3 tools/setup_stockfish_android.py");
        EngineManifest manifest = JsonUtility.FromJson<EngineManifest>(File.ReadAllText(Notices + "manifest.json"));
        if (manifest == null || manifest.engine != "Stockfish 18" || string.IsNullOrEmpty(manifest.source) ||
            string.IsNullOrEmpty(manifest.license) || string.IsNullOrEmpty(manifest.binary_sha256))
            throw new BuildFailedException("The Android Stockfish provenance manifest is incomplete.");
        using (var stream = File.OpenRead(EngineAsset))
        using (var hash = SHA256.Create())
        {
            string actual = BitConverter.ToString(hash.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
            if (!string.Equals(actual, manifest.binary_sha256, StringComparison.Ordinal))
                throw new BuildFailedException("Android Stockfish checksum does not match its prepared manifest.");
            stream.Position = 0;
            var header = new byte[20];
            if (stream.Read(header, 0, header.Length) != header.Length || header[0] != 0x7f ||
                header[1] != 'E' || header[2] != 'L' || header[3] != 'F' || header[4] != 2 || header[5] != 1 ||
                header[16] != 3 || header[17] != 0 || header[18] != 183 || header[19] != 0)
                throw new BuildFailedException("Android Stockfish must be a little-endian ELF64 AArch64 PIE executable.");
        }
        var importer = AssetImporter.GetAtPath(EngineAsset) as PluginImporter;
        if (importer == null || importer.GetCompatibleWithAnyPlatform() || importer.GetCompatibleWithEditor() ||
            !importer.GetCompatibleWithPlatform(BuildTarget.Android) ||
            importer.GetPlatformData(BuildTarget.Android, "CPU") != "ARM64" || importer.isPreloaded)
            throw new BuildFailedException("Stockfish must be imported for Android ARM64 only, without native preloading.");
        if (!File.Exists(Notices + "Copying.txt"))
            throw new BuildFailedException("The Stockfish GPL license is missing from StreamingAssets.");
        Debug.Log("CHESS_CGI_ANDROID_STOCKFISH payload=verified engine=18 abi=arm64-v8a transport=JavaProcessBuilder");
    }

    public void OnPreprocessBuild(BuildReport report)
    {
        if (report.summary.platform == BuildTarget.Android) ValidatePayload();
    }

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        string manifestPath = Path.Combine(path, "src", "main", "AndroidManifest.xml");
        var document = new XmlDocument { PreserveWhitespace = true };
        document.Load(manifestPath);
        XmlElement application = document.SelectSingleNode("/manifest/application") as XmlElement;
        if (application == null) throw new BuildFailedException("Android manifest has no application element.");
        application.SetAttribute("extractNativeLibs", AndroidNamespace, "true");
        document.Save(manifestPath);

        string rules = "-keep class com.mathwidu.chesscgi.StockfishProcess { public *; }\n";
        File.WriteAllText(Path.Combine(path, "stockfish-consumer-rules.pro"), rules, new UTF8Encoding(false));
        ConfigureGradle(Path.Combine(path, "build.gradle"), true);
        ConfigureGradle(Path.Combine(Directory.GetParent(path).FullName, "launcher", "build.gradle"), false);
    }

    private static void ConfigureGradle(string path, bool library)
    {
        if (!File.Exists(path)) throw new BuildFailedException("Missing Android Gradle project: " + path);
        string gradle = File.ReadAllText(path);
        if (gradle.Contains(GradleMarker)) return;
        // Keep the PIE bytes intact: it is executed via ProcessBuilder, never dlopen'ed.
        string block = "\n" + GradleMarker + "\nandroid {\n" +
            "    packagingOptions { jniLibs {\n" +
            "        useLegacyPackaging true\n" +
            "        keepDebugSymbols += ['**/libstockfish.so']\n" +
            "    } }\n" +
            (library ? "    defaultConfig { consumerProguardFiles 'stockfish-consumer-rules.pro' }\n" : "") +
            "}\n";
        File.WriteAllText(path, gradle + block, new UTF8Encoding(false));
    }
}
