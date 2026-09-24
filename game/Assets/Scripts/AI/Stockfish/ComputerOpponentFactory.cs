using System;
using System.IO;
using UnityEngine;

public static class ComputerOpponentFactory
{
    public static IMoveChooser Create()
    {
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX
        return new StockfishUciMoveChooser(FindExecutable());
#else
        throw new PlatformNotSupportedException("O adversario de IA ainda nao esta disponivel nesta plataforma.");
#endif
    }

    public static string FindExecutable()
    {
        string configured = Environment.GetEnvironmentVariable("CHESS_STOCKFISH_PATH");
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured;
        }
        string executable = Application.platform == RuntimePlatform.WindowsPlayer ||
            Application.platform == RuntimePlatform.WindowsEditor ? "stockfish.exe" : "stockfish";
        string besideGame = Path.GetFullPath(Path.Combine(Application.dataPath, "..", executable));
        if (File.Exists(besideGame))
        {
            return besideGame;
        }
        // Local machine setup is outside Assets and cannot leak into a different platform's build.
        string installed = Path.Combine(Application.persistentDataPath, "Engines", executable);
        if (File.Exists(installed))
        {
            return installed;
        }
#if UNITY_EDITOR
        string local = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", ".local", "stockfish", executable));
        if (File.Exists(local))
        {
            return local;
        }
#endif
        foreach (string directory in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator))
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                continue;
            }
            string candidate = Path.Combine(directory, executable);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }
        return installed;
    }
}
