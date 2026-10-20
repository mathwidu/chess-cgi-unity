using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

// Android uses a packaged executable through Java, not System.Diagnostics.Process
// (process creation is unsupported by IL2CPP). No Unity objects enter the search.
public sealed class AndroidStockfishMoveChooser : IMoveChooser
{
    private readonly IStockfishSessionFactory factory;
    private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
    private readonly SemaphoreSlim gate = new SemaphoreSlim(1, 1);
    private IStockfishSession session;
    private int disposed;

    public AndroidStockfishMoveChooser(IStockfishSessionFactory factory)
    {
        this.factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    public async Task<ChessMove> ChooseMoveAsync(PositionSnapshot position, MoveSearchSettings settings,
        CancellationToken cancellationToken)
    {
        if (Volatile.Read(ref disposed) != 0)
            throw new ObjectDisposedException(nameof(AndroidStockfishMoveChooser));

        using (var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, lifetime.Token))
        {
            await gate.WaitAsync(linked.Token).ConfigureAwait(false);
            try
            {
                return await Task.Run(() => Search(position, settings, linked.Token), linked.Token)
                    .ConfigureAwait(false);
            }
            finally { gate.Release(); }
        }
    }

    private ChessMove Search(PositionSnapshot position, MoveSearchSettings settings, CancellationToken token)
    {
        try
        {
            token.ThrowIfCancellationRequested();
            if (session == null)
            {
                session = factory.Open();
                token.ThrowIfCancellationRequested();
                session.Send("uci");
                ReadUntil(line => line == "uciok", token);
                session.Send("setoption name Threads value 1");
                session.Send("setoption name Hash value 16");
                session.Send("setoption name Ponder value false");
                session.Send("setoption name UCI_LimitStrength value false");
                session.Send("ucinewgame");
            }

            session.Send("setoption name Skill Level value " + settings.SkillLevel);
            session.Send("isready");
            ReadUntil(line => line == "readyok", token);
            session.Send("position fen " + position.InitialFen +
                (string.IsNullOrEmpty(position.Moves) ? "" : " moves " + position.Moves));
            session.Send("go movetime " + settings.MoveTimeMilliseconds);
            string answer = ReadUntil(line => line.StartsWith("bestmove ", StringComparison.Ordinal), token);
            string[] fields = answer.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (fields.Length < 2 || !ChessMove.TryParseUci(fields[1], out ChessMove move))
                throw new InvalidDataException("O motor devolveu uma jogada malformada ou sem movimento.");
            token.ThrowIfCancellationRequested();
            return move;
        }
        catch
        {
            CloseSession();
            token.ThrowIfCancellationRequested();
            throw;
        }
    }

    private string ReadUntil(Func<string, bool> predicate, CancellationToken token)
    {
        while (true)
        {
            token.ThrowIfCancellationRequested();
            string line = session.ReadLine(50);
            if (line != null && predicate(line)) return line;
        }
    }

    private void CloseSession()
    {
        IStockfishSession old = session;
        session = null;
        // Cleanup must not replace the original protocol/cancellation error.
        try { old?.Dispose(); }
        catch (Exception exception) { System.Diagnostics.Trace.TraceWarning(exception.ToString()); }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0) return;
        lifetime.Cancel();
        // The worker observes cancellation at most one bounded read later. Shutdown
        // and JNI cleanup are serialized after it, never on Unity's main thread.
        _ = Task.Run(CloseAsync);
    }

    private async Task CloseAsync()
    {
        await gate.WaitAsync().ConfigureAwait(false);
        try
        {
            CloseSession();
            factory.Dispose();
        }
        catch (Exception exception) { System.Diagnostics.Trace.TraceWarning(exception.ToString()); }
        finally { gate.Release(); }
        // A canceled waiter can still observe these objects; do not dispose them here.
    }
}
