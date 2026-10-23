using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

// Only protocol values cross the bridge. Stockfish never changes the live game.
public interface IWebStockfishBridge : IDisposable
{
    void Begin(int requestId, string initialFen, string moves, int skill, int moveTimeMilliseconds);
    string TakeMessage();
    void Cancel();
}

public sealed class WebStockfishMoveChooser : IPlayerLoopMoveChooser
{
    public const int InitializationTimeoutMilliseconds = 60000;
    private readonly IWebStockfishBridge bridge;
    private readonly Func<double> clock;
    private TaskCompletionSource<ChessMove> pending;
    private CancellationTokenRegistration cancellation;
    private MoveSearchSettings settings;
    private int nextRequestId;
    private int requestId;
    private double deadline;
    private bool initializing;
    private bool disposed;

    public WebStockfishMoveChooser() : this(new BrowserBridge(), () => Time.realtimeSinceStartupAsDouble) { }

    // The injected clock measures real seconds, independent of timeScale.
    public WebStockfishMoveChooser(IWebStockfishBridge bridge, Func<double> clock)
    {
        this.bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
        this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public Task<ChessMove> ChooseMoveAsync(PositionSnapshot position, MoveSearchSettings searchSettings,
        CancellationToken token)
    {
        if (disposed) throw new ObjectDisposedException(nameof(WebStockfishMoveChooser));
        if (pending != null) throw new InvalidOperationException("Uma busca da IA web ja esta em andamento.");
        token.ThrowIfCancellationRequested();
        settings = searchSettings;
        requestId = ++nextRequestId;
        if (requestId == 0) requestId = ++nextRequestId;
        var completion = new TaskCompletionSource<ChessMove>();
        pending = completion;
        initializing = true;
        deadline = clock() + InitializationTimeoutMilliseconds / 1000.0;
        try
        {
            bridge.Begin(requestId, position.InitialFen, position.Moves, settings.SkillLevel,
                settings.MoveTimeMilliseconds);
            cancellation = token.Register(CancelPending);
        }
        catch (Exception exception) { Fail(exception); }
        return completion.Task;
    }

    public void Poll()
    {
        if (pending == null || disposed) return;
        try
        {
            // Enforce the deadline before reading a late reply after a suspended tab.
            if (clock() >= deadline)
            {
                Fail(new TimeoutException(initializing
                    ? "O carregamento da IA web demorou demais. Confira a conexao e tente novamente."
                    : "A IA demorou demais. Tente novamente."));
                return;
            }
            // The bridge emits only stage/result/error messages, never UCI info spam.
            for (int count = 0; count < 8 && pending != null; count++)
            {
                string json = bridge.TakeMessage();
                if (string.IsNullOrEmpty(json)) break;
                Message message = JsonUtility.FromJson<Message>(json);
                if (message == null || message.requestId != requestId) continue;
                if (message.type == "searching" && initializing)
                {
                    initializing = false;
                    deadline = clock() + settings.TimeoutMilliseconds / 1000.0;
                }
                else if (message.type == "bestmove")
                {
                    if (!ChessMove.TryParseUci(message.value, out ChessMove move))
                        throw new InvalidDataException("O motor web devolveu uma jogada malformada ou sem movimento.");
                    var completion = TakePending();
                    completion.TrySetResult(move);
                }
                else if (message.type == "error")
                    throw new InvalidOperationException(message.value);
            }
        }
        catch (Exception exception) { Fail(exception); }
    }

    private TaskCompletionSource<ChessMove> TakePending()
    {
        var completion = pending;
        pending = null;
        cancellation.Dispose();
        cancellation = default;
        return completion;
    }

    private void CancelPending()
    {
        if (pending == null) return;
        var completion = TakePending();
        try { bridge.Cancel(); }
        finally { completion.TrySetCanceled(); }
    }

    private void Fail(Exception exception)
    {
        if (pending == null) return;
        var completion = TakePending();
        try { bridge.Cancel(); }
        catch (Exception) { /* Preserve the protocol or deadline error. */ }
        completion.TrySetException(exception);
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        try { CancelPending(); }
        finally { bridge.Dispose(); }
    }

    [Serializable]
    private sealed class Message
    {
        public int requestId;
        public string type;
        public string value;
    }

    private sealed class BrowserBridge : IWebStockfishBridge
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        private int handle = ChessStockfish_Create();
        [DllImport("__Internal")] private static extern int ChessStockfish_Create();
        [DllImport("__Internal")] private static extern void ChessStockfish_Begin(int handle, int requestId,
            string initialFen, string moves, int skill, int moveTimeMilliseconds);
        [DllImport("__Internal")] private static extern string ChessStockfish_TakeMessage(int handle);
        [DllImport("__Internal")] private static extern void ChessStockfish_Cancel(int handle);
        [DllImport("__Internal")] private static extern void ChessStockfish_Dispose(int handle);
        public void Begin(int requestId, string initialFen, string moves, int skill, int moveTimeMilliseconds)
        { ChessStockfish_Begin(handle, requestId, initialFen, moves, skill, moveTimeMilliseconds); }
        public string TakeMessage() => ChessStockfish_TakeMessage(handle);
        public void Cancel() { if (handle != 0) ChessStockfish_Cancel(handle); }
        public void Dispose()
        {
            if (handle == 0) return;
            ChessStockfish_Dispose(handle);
            handle = 0;
        }
#else
        public BrowserBridge() { throw new PlatformNotSupportedException("A IA web exige uma build WebGL."); }
        public void Begin(int requestId, string initialFen, string moves, int skill, int moveTimeMilliseconds) { }
        public string TakeMessage() => null;
        public void Cancel() { }
        public void Dispose() { }
#endif
    }
}
