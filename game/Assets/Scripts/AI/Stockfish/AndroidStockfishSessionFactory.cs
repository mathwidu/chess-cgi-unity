#if UNITY_ANDROID && !UNITY_EDITOR
using System;
using System.IO;
using UnityEngine;

// Construct on Unity's thread so the application class loader resolves our Java
// helper. The cached global reference is then used only by serialized workers.
public sealed class AndroidStockfishSessionFactory : IStockfishSessionFactory
{
    private readonly AndroidJavaClass helper;
    private readonly string executablePath;

    public AndroidStockfishSessionFactory()
    {
        using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
        using (var info = activity.Call<AndroidJavaObject>("getApplicationInfo"))
        {
            executablePath = Path.Combine(info.Get<string>("nativeLibraryDir"), "libstockfish.so");
        }
        if (!File.Exists(executablePath))
            throw new FileNotFoundException("O motor offline nao foi extraido do APK. Reinstale a build do Quest.");
        helper = new AndroidJavaClass("com.mathwidu.chesscgi.StockfishProcess");
    }

    public IStockfishSession Open()
    {
        using (new JniWorkerScope())
            return new Session(helper.CallStatic<AndroidJavaObject>("open", executablePath));
    }

    public void Dispose()
    {
        using (new JniWorkerScope()) helper.Dispose();
    }

    private sealed class Session : IStockfishSession
    {
        private readonly AndroidJavaObject process;
        public Session(AndroidJavaObject process) { this.process = process; }
        public void Send(string command)
        {
            using (new JniWorkerScope()) process.Call("send", command);
        }
        public string ReadLine(int timeoutMilliseconds)
        {
            using (new JniWorkerScope()) return process.Call<string>("readLine", timeoutMilliseconds);
        }
        public void Dispose()
        {
            using (new JniWorkerScope())
            {
                try { process.Call("close"); }
                finally { process.Dispose(); }
            }
        }
    }

    private sealed class JniWorkerScope : IDisposable
    {
        public JniWorkerScope()
        {
            if (AndroidJNI.AttachCurrentThread() != 0)
                throw new InvalidOperationException("Nao foi possivel iniciar a comunicacao com a IA Android.");
        }
        public void Dispose() { AndroidJNI.DetachCurrentThread(); }
    }
}
#endif
