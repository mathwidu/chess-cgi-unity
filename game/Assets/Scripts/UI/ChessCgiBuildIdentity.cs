using System;
using System.Globalization;
using UnityEngine;

// The build callback writes this data into Resources; runtime never reads the current clock.
[Serializable]
public sealed class ChessCgiBuildIdentity
{
    public const string ResourceName = "ChessCgiBuildIdentity";
    public string versionName;
    public int androidVersionCode;
    public string builtAtUtc;
    public string buildTarget;

    public static string MenuLabel
    {
        get
        {
#if UNITY_EDITOR
            return $"v{Application.version} · Editor";
#else
            TextAsset asset = Resources.Load<TextAsset>(ResourceName);
            ChessCgiBuildIdentity identity = null;
            if (asset != null)
            {
                try { identity = JsonUtility.FromJson<ChessCgiBuildIdentity>(asset.text); }
                catch (ArgumentException) { }
            }
            return FormatMenuLabel(identity, Application.version, Application.platform == RuntimePlatform.Android);
#endif
        }
    }

    public static string FormatMenuLabel(ChessCgiBuildIdentity identity, string applicationVersion, bool androidPlayer)
    {
        string unavailable = $"v{applicationVersion} · data da compilação indisponível";
        if (identity == null || !string.Equals(identity.versionName, applicationVersion, StringComparison.Ordinal) ||
            string.IsNullOrEmpty(identity.buildTarget) ||
            androidPlayer != (identity.buildTarget == "Android") ||
            (androidPlayer && identity.androidVersionCode <= 0) ||
            !DateTimeOffset.TryParseExact(identity.builtAtUtc, "O", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out DateTimeOffset builtAt))
            return unavailable;

        string code = androidPlayer ? $" ({identity.androidVersionCode})" : "";
        string brasiliaTime = builtAt.ToOffset(TimeSpan.FromHours(-3))
            .ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
        return $"v{applicationVersion}{code} · {brasiliaTime} Brasília (UTC-3)";
    }
}
