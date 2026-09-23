using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

// Desktop transport. Quest can supply another IMoveChooser without changing gameplay.
public sealed class StockfishUciMoveChooser : IMoveChooser
{
    private readonly string executablePath;
    private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
    private readonly SemaphoreSlim gate = new SemaphoreSlim(1, 1);
    private Process process;
    private int disposed;

    public StockfishUciMoveChooser(string executablePath)
    {
        this.executablePath = executablePath;
    }

    public async Task<ChessMove> ChooseMoveAsync(PositionSnapshot position, MoveSearchSettings settings,
        CancellationToken cancellationToken)
    {
        if (Volatile.Read(ref disposed) != 0)
        {
            throw new ObjectDisposedException(nameof(StockfishUciMoveChooser));
        }
        using (var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, lifetime.Token))
        {
            await gate.WaitAsync(linked.Token).ConfigureAwait(false);
            try
            {
                return await Task.Run(() => Search(position, settings, linked.Token), linked.Token).ConfigureAwait(false);
            }
            finally
            {
                gate.Release();
            }
        }
    }

    private ChessMove Search(PositionSnapshot position, MoveSearchSettings settings, CancellationToken token)
    {
        try
        {
            token.ThrowIfCancellationRequested();
            bool fresh = process == null;
            Process session = process ?? StartProcess();
            using (token.Register(() => QueueClose(session)))
            {
                token.ThrowIfCancellationRequested();
                if (fresh)
                {
                    InitializeSession(session, token);
                }
                ConfigureSearch(session, position, settings, token);
                string answer = ReadUntil(session, line => line.StartsWith("bestmove ", StringComparison.Ordinal), token);
                ChessMove move = ParseBestMove(answer);
                token.ThrowIfCancellationRequested();
                return move;
            }
        }
        catch
        {
            Close(Interlocked.Exchange(ref process, null));
            token.ThrowIfCancellationRequested();
            throw;
        }
    }

    private static void InitializeSession(Process session, CancellationToken token)
    {
        Send(session, "uci");
        ReadUntil(session, line => line == "uciok", token);
        Send(session, "setoption name Threads value 1");
        Send(session, "setoption name Hash value 16");
        Send(session, "setoption name Ponder value false");
        Send(session, "setoption name UCI_LimitStrength value false");
        Send(session, "ucinewgame");
    }

    private static void ConfigureSearch(Process session, PositionSnapshot position,
        MoveSearchSettings settings, CancellationToken token)
    {
        Send(session, "setoption name Skill Level value " + settings.SkillLevel);
        Send(session, "isready");
        ReadUntil(session, line => line == "readyok", token);
        // Keep move history so the engine can reason about repetitions.
        Send(session, "position fen " + position.InitialFen +
            (string.IsNullOrEmpty(position.Moves) ? "" : " moves " + position.Moves));
        Send(session, "go movetime " + settings.MoveTimeMilliseconds);
    }

    private static ChessMove ParseBestMove(string answer)
    {
        string[] fields = answer.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (fields.Length < 2 || !ChessMove.TryParseUci(fields[1], out ChessMove move))
            throw new InvalidDataException("O motor devolveu uma jogada malformada ou sem movimento.");
        return move;
    }

    private Process StartProcess()
    {
        if (string.IsNullOrWhiteSpace(executablePath) || !File.Exists(executablePath))
            throw new FileNotFoundException("O motor de xadrez nao esta disponivel neste dispositivo.");
        var session = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = executablePath,
                WorkingDirectory = Path.GetDirectoryName(Path.GetFullPath(executablePath)),
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };
        try
        {
            session.Start();
            // Drain stderr continuously; a full pipe must never stall the engine.
            session.ErrorDataReceived += (_, __) => { };
            session.BeginErrorReadLine();
            process = session;
            return session;
        }
        catch
        {
            session.Dispose();
            throw;
        }
    }

    private static void Send(Process session, string command)
    {
        session.StandardInput.WriteLine(command);
        session.StandardInput.Flush();
    }

    private static string ReadUntil(Process session, Func<string, bool> predicate, CancellationToken token)
    {
        while (true)
        {
            token.ThrowIfCancellationRequested();
            string line = session.StandardOutput.ReadLine();
            if (line == null)
            {
                throw new IOException("O motor de xadrez encerrou inesperadamente.");
            }
            if (predicate(line))
            {
                return line;
            }
        }
    }

    private void QueueClose(Process session)
    {
        if (Interlocked.CompareExchange(ref process, null, session) == session)
            ThreadPool.QueueUserWorkItem(_ => Close(session));
    }

    private static void Close(Process session)
    {
        if (session == null)
        {
            return;
        }
        try
        {
            if (!session.HasExited)
            {
                try
                {
                    Send(session, "stop");
                    Send(session, "quit");
                }
                catch (IOException) { }
                if (!session.WaitForExit(250))
                {
                    session.Kill();
                }
            }
        }
        catch (InvalidOperationException) { }
        catch (System.ComponentModel.Win32Exception) { }
        finally
        {
            session.Dispose();
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0)
        {
            return;
        }
        lifetime.Cancel();
        Process session = Interlocked.Exchange(ref process, null);
        if (session != null)
        {
            ThreadPool.QueueUserWorkItem(_ => Close(session));
        }
        // Pending calls still use the CTS/semaphore; do not dispose them underneath a worker.
    }
}
