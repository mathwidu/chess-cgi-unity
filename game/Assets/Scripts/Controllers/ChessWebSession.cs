using System;
using System.Collections;
using UnityEngine;

// Storage is injected in tests; desktop/Quest never touch browser storage.
public interface IChessWebSessionStorage
{
    bool Read(out string json, out bool hasSave, out string reason);
    bool Write(string json, out string reason);
    void Notify(string eventName, bool ok, bool hasSave, string reason);
}

public sealed class ChessWebSession : MonoBehaviour
{
    public const string ObjectName = "Web Session";
    private ChessGameController controller;
    private IChessWebSessionStorage storage;
    private bool hasSave;

#if UNITY_WEBGL && !UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindFirstObjectByType<ChessWebSession>() == null)
            new GameObject(ObjectName).AddComponent<ChessWebSession>();
    }
#endif

    private IEnumerator Start()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        // Controller.Start opens the standard menu without authoring a save.
        // Wait for that initialization before telling the launcher it may resume.
        yield return null;
        if (storage == null)
            Initialize(FindFirstObjectByType<ChessGameController>(), new BrowserStorage());
#else
        yield break;
#endif
    }

    public void Initialize(ChessGameController game, IChessWebSessionStorage sessionStorage)
    {
        if (sessionStorage == null) throw new ArgumentNullException(nameof(sessionStorage));
        if (controller != null) controller.SessionChanged -= SaveNow;
        controller = game;
        storage = sessionStorage;
        gameObject.name = ObjectName;
        if (controller != null) controller.SessionChanged += SaveNow;
        bool readable = Read(out string json, out string reason);
        if (readable && hasSave) ChessMatchSession.TryReplay(json, out _, out reason);
        // Readiness means the controller can receive commands. A corrupt save
        // or denied storage must still let the launcher offer a new game.
        if (controller == null) reason = "scene-unavailable";
        Notify("cicochess:session-ready", controller != null, reason);
    }

    // SendMessage entry points deliberately take no serialized user payload.
    public void SaveNow()
    {
        if (storage == null) return;
        if (controller == null) { Notify("cicochess:save", false, "scene-unavailable"); return; }
        if (!controller.TrySaveSession(out string json, out string reason))
        { Notify("cicochess:save", false, reason); return; }
        bool ok;
        try { ok = storage.Write(json, out reason); }
        catch (Exception) { ok = false; reason = "storage-unavailable"; }
        if (ok) hasSave = true;
        Notify("cicochess:save", ok, reason);
    }

    public void ResumeSavedGame()
    {
        if (storage == null) return;
        bool ok = Read(out string json, out string reason);
        if (ok && !hasSave) { ok = false; reason = "no-save"; }
        if (ok && controller == null) { ok = false; reason = "scene-unavailable"; }
        if (ok) ok = controller.TryResumeSession(json, out reason);
        // Never rewrite, remove or migrate a rejected save automatically.
        Notify("cicochess:resume", ok, reason);
    }

    private bool Read(out string json, out string reason)
    {
        try { return storage.Read(out json, out hasSave, out reason); }
        catch (Exception) { json = null; reason = "storage-unavailable"; return false; }
    }

    private void Notify(string name, bool ok, string reason)
    {
        // Storage and notification failures cannot interrupt an accepted move.
        try { storage.Notify(name, ok, hasSave, reason ?? ""); }
        catch (Exception) { Debug.LogWarning("CHESS_SESSION_NOTIFICATION_UNAVAILABLE"); }
    }

    private void OnDestroy()
    {
        if (controller != null) controller.SessionChanged -= SaveNow;
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    private sealed class BrowserStorage : IChessWebSessionStorage
    {
        [Serializable]
        private sealed class ReadResult
        {
            public bool ok;
            public bool hasSave;
            public string reason;
            public string value;
        }

        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern string ChessSession_Read(string key);
        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern int ChessSession_Write(string key, string value);
        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern void ChessSession_Notify(string eventName, int ok, int hasSave, string reason);

        public bool Read(out string json, out bool exists, out string reason)
        {
            ReadResult result = JsonUtility.FromJson<ReadResult>(ChessSession_Read(ChessMatchSession.StorageKey));
            json = result.value;
            exists = result.hasSave;
            reason = result.reason;
            return result.ok;
        }
        public bool Write(string json, out string reason)
        {
            int result = ChessSession_Write(ChessMatchSession.StorageKey, json);
            reason = result == 0 ? "" : result == 1 ? "storage-full" :
                result == 3 ? "save-conflict" : "storage-unavailable";
            return result == 0;
        }
        public void Notify(string name, bool ok, bool exists, string reason) =>
            ChessSession_Notify(name, ok ? 1 : 0, exists ? 1 : 0, reason);
    }
#endif
}
