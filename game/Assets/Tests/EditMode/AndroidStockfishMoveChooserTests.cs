using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

public class AndroidStockfishMoveChooserTests
{
    private sealed class Session : IStockfishSession
    {
        private readonly ConcurrentQueue<string> output = new ConcurrentQueue<string>();
        private readonly AutoResetEvent available = new AutoResetEvent(false);
        public readonly List<string> Commands = new List<string>();
        public readonly TaskCompletionSource<bool> SearchStarted = new TaskCompletionSource<bool>();
        public readonly TaskCompletionSource<bool> HandshakeStarted = new TaskCompletionSource<bool>();
        public string Answer = "bestmove e2e4";
        public bool Handshake = true;
        public bool EofOnSearch;
        public volatile bool Closed;
        private bool eof;
        public void Reply(string line) { output.Enqueue(line); available.Set(); }
        public void Send(string command)
        {
            Commands.Add(command);
            if (command == "uci")
            {
                if (Handshake) Reply("uciok");
                HandshakeStarted.TrySetResult(true);
            }
            if (command == "isready") Reply("readyok");
            if (command.StartsWith("go "))
            {
                if (EofOnSearch) eof = true;
                else if (Answer != null) Reply(Answer);
                SearchStarted.TrySetResult(true);
            }
        }
        public string ReadLine(int timeoutMilliseconds)
        {
            if (eof) throw new IOException("engine EOF");
            if (output.TryDequeue(out string line)) return line;
            available.WaitOne(timeoutMilliseconds);
            return output.TryDequeue(out line) ? line : null;
        }
        public void Dispose() { Closed = true; available.Set(); }
    }

    private sealed class Factory : IStockfishSessionFactory
    {
        private readonly Queue<Session> sessions;
        public int OpenCount;
        public readonly TaskCompletionSource<bool> Closed = new TaskCompletionSource<bool>();
        public Factory(params Session[] sessions) { this.sessions = new Queue<Session>(sessions); }
        public IStockfishSession Open() { OpenCount++; return sessions.Dequeue(); }
        public void Dispose() { Closed.TrySetResult(true); }
    }

    private static readonly MoveSearchSettings Limits = new MoveSearchSettings(14, 50, 3000);

    private static async Task FinishWithin(Task task)
    {
        Assert.That(await Task.WhenAny(task, Task.Delay(4000)), Is.SameAs(task), "Worker did not finish.");
        await task;
    }

    private static async Task ExpectFailure<T>(Task task) where T : Exception
    {
        try
        {
            await FinishWithin(task);
            Assert.Fail("Expected " + typeof(T).Name);
        }
        catch (T) { }
    }

    [Test]
    public async Task SessionIsReusedAndReceivesDifficultyAndFullMoveHistory()
    {
        var session = new Session();
        var factory = new Factory(session);
        using (var chooser = new AndroidStockfishMoveChooser(factory))
        using (var deadline = new CancellationTokenSource(4000))
        {
            var rules = new ChessRulesAdapter();
            ChessMove first = await chooser.ChooseMoveAsync(rules.GetSnapshot(), Limits, deadline.Token);
            Assert.That(rules.TryMove(first).Success, Is.True);
            session.Answer = "bestmove e7e5";
            ChessMove second = await chooser.ChooseMoveAsync(rules.GetSnapshot(), Limits, deadline.Token);
            Assert.That(rules.TryMove(second).Success, Is.True);
            Assert.That(factory.OpenCount, Is.EqualTo(1));
            Assert.That(session.Commands, Does.Contain("setoption name Threads value 1"));
            Assert.That(session.Commands, Does.Contain("setoption name Hash value 16"));
            Assert.That(session.Commands, Does.Contain("setoption name Ponder value false"));
            Assert.That(session.Commands, Does.Contain("setoption name UCI_LimitStrength value false"));
            Assert.That(session.Commands, Does.Contain("setoption name Skill Level value 14"));
            Assert.That(session.Commands, Does.Contain("go movetime 50"));
            Assert.That(session.Commands, Has.Some.EndsWith(" moves e2e4"));
        }
        await FinishWithin(factory.Closed.Task);
        Assert.That(session.Closed, Is.True);
    }

    [Test]
    public async Task CancelingSearchClosesItAndCannotLeakLateReplyIntoNextSession()
    {
        var old = new Session { Answer = null };
        var fresh = new Session { Answer = "bestmove d2d4" };
        var factory = new Factory(old, fresh);
        using (var chooser = new AndroidStockfishMoveChooser(factory))
        using (var cancellation = new CancellationTokenSource(4000))
        {
            Task<ChessMove> pending = chooser.ChooseMoveAsync(new ChessRulesAdapter().GetSnapshot(), Limits, cancellation.Token);
            await FinishWithin(old.SearchStarted.Task);
            cancellation.Cancel();
            await ExpectFailure<OperationCanceledException>(pending);
            Assert.That(old.Closed, Is.True);
            old.Reply("bestmove e2e4");
            using (var deadline = new CancellationTokenSource(4000))
            {
                ChessMove next = await chooser.ChooseMoveAsync(new ChessRulesAdapter().GetSnapshot(), Limits, deadline.Token);
                Assert.That(next.From, Is.EqualTo(BoardSquare.FromAlgebraic("d2")));
                Assert.That(factory.OpenCount, Is.EqualTo(2));
            }
        }
        await FinishWithin(factory.Closed.Task);
    }

    [Test]
    public async Task DisposeDoesNotWaitForHandshakeOnCallingThread()
    {
        var session = new Session { Handshake = false };
        var factory = new Factory(session);
        var chooser = new AndroidStockfishMoveChooser(factory);
        Task<ChessMove> pending = chooser.ChooseMoveAsync(new ChessRulesAdapter().GetSnapshot(), Limits, CancellationToken.None);
        await FinishWithin(session.HandshakeStarted.Task);
        var watch = Stopwatch.StartNew();
        chooser.Dispose();
        Assert.That(watch.ElapsedMilliseconds, Is.LessThan(100));
        await ExpectFailure<OperationCanceledException>(pending);
        await FinishWithin(factory.Closed.Task);
        Assert.That(session.Closed, Is.True);
        await ExpectFailure<ObjectDisposedException>(
            chooser.ChooseMoveAsync(new ChessRulesAdapter().GetSnapshot(), Limits, CancellationToken.None));
        chooser.Dispose();
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task MalformedMoveOrEofClosesFailedSessionAndAllowsRetry(bool eof)
    {
        var broken = new Session { EofOnSearch = eof, Answer = "bestmove (none)" };
        var good = new Session();
        var factory = new Factory(broken, good);
        using (var chooser = new AndroidStockfishMoveChooser(factory))
        using (var deadline = new CancellationTokenSource(4000))
        {
            if (eof)
                await ExpectFailure<IOException>(
                    chooser.ChooseMoveAsync(new ChessRulesAdapter().GetSnapshot(), Limits, deadline.Token));
            else
                await ExpectFailure<InvalidDataException>(
                    chooser.ChooseMoveAsync(new ChessRulesAdapter().GetSnapshot(), Limits, deadline.Token));
            Assert.That(broken.Closed, Is.True);
            var rules = new ChessRulesAdapter();
            Assert.That(rules.TryMove(await chooser.ChooseMoveAsync(rules.GetSnapshot(), Limits, deadline.Token)).Success, Is.True);
        }
        await FinishWithin(factory.Closed.Task);
    }

    [Test]
    public async Task CancelingQueuedRequestDoesNotCloseTheActiveSearch()
    {
        var session = new Session { Answer = null };
        var factory = new Factory(session);
        using (var chooser = new AndroidStockfishMoveChooser(factory))
        using (var deadline = new CancellationTokenSource(4000))
        using (var queuedCancellation = new CancellationTokenSource())
        {
            var rules = new ChessRulesAdapter();
            Task<ChessMove> first = chooser.ChooseMoveAsync(rules.GetSnapshot(), Limits, deadline.Token);
            await FinishWithin(session.SearchStarted.Task);
            Task<ChessMove> queued = chooser.ChooseMoveAsync(rules.GetSnapshot(), Limits, queuedCancellation.Token);
            queuedCancellation.Cancel();
            await ExpectFailure<OperationCanceledException>(queued);
            Assert.That(session.Closed, Is.False);
            session.Reply("bestmove e2e4");
            Assert.That(rules.TryMove(await first).Success, Is.True);
            Assert.That(factory.OpenCount, Is.EqualTo(1));
        }
        await FinishWithin(factory.Closed.Task);
    }
}
