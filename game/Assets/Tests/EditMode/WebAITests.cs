using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;

public class WebAITests
{
    private sealed class Bridge : IWebStockfishBridge
    {
        public readonly Queue<string> Messages = new Queue<string>();
        public int RequestId;
        public int Canceled;
        public bool Disposed;
        public string Fen;
        public string Moves;
        public void Begin(int id, string fen, string moves, int skill, int moveTime)
        { RequestId = id; Fen = fen; Moves = moves; }
        public string TakeMessage() => Messages.Count == 0 ? null : Messages.Dequeue();
        public void Cancel() { Canceled++; }
        public void Dispose() { Disposed = true; }
        public void Reply(string type, string value = "", int id = 0)
        { Messages.Enqueue("{\"requestId\":" + (id == 0 ? RequestId : id) + ",\"type\":\"" + type + "\",\"value\":\"" + value + "\"}"); }
    }

    private static readonly MoveSearchSettings Limits = new MoveSearchSettings(6, 500, 6500);

    [Test]
    public void PlayerLoopDeliversAMoveWithoutManagedTimers()
    {
        var bridge = new Bridge();
        var rules = new ChessRulesAdapter();
        long originalRevision = rules.GetSnapshot().Revision;
        double now = 0;
        using (var coordinator = new ComputerTurnCoordinator(new WebStockfishMoveChooser(bridge, () => now)))
        {
            coordinator.Begin(rules.GetSnapshot(), Limits);
            Assert.That(coordinator.IsThinking, Is.True);
            Assert.That(coordinator.TryTakeResult(rules.GetSnapshot(), out _), Is.False);
            bridge.Reply("searching");
            bridge.Reply("bestmove", "e2e4");
            Assert.That(coordinator.TryTakeResult(rules.GetSnapshot(), out var result), Is.True);
            Assert.That(result.Success, Is.True);
            Assert.That(result.Move.ToUci(), Is.EqualTo("e2e4"));
            Assert.That(rules.GetSnapshot().Revision, Is.EqualTo(originalRevision), "The engine must not mutate the game.");
            Assert.That(coordinator.IsThinking, Is.False);
        }
        Assert.That(bridge.Disposed, Is.True);
    }

    [Test]
    public void LoadingHasItsOwnDeadlineBeforeTheSearchBudget()
    {
        var bridge = new Bridge();
        double now = 0;
        using (var chooser = new WebStockfishMoveChooser(bridge, () => now))
        {
            var task = chooser.ChooseMoveAsync(new ChessRulesAdapter().GetSnapshot(), Limits, CancellationToken.None);
            now = 59;
            chooser.Poll();
            Assert.That(task.IsCompleted, Is.False);
            bridge.Reply("searching");
            chooser.Poll();
            now = 61;
            chooser.Poll();
            Assert.That(task.IsCompleted, Is.False, "Startup must not consume the move deadline.");
            now = 65.5;
            chooser.Poll();
            Assert.That(task.IsFaulted, Is.True);
            Assert.Throws<TimeoutException>(() => task.GetAwaiter().GetResult());
            Assert.That(bridge.Canceled, Is.EqualTo(1));
        }
    }

    [Test]
    public void InitializationTimeoutFailsThroughCoordinatorWithoutThreads()
    {
        var bridge = new Bridge();
        var snapshot = new ChessRulesAdapter().GetSnapshot();
        double now = 0;
        using (var coordinator = new ComputerTurnCoordinator(new WebStockfishMoveChooser(bridge, () => now)))
        {
            coordinator.Begin(snapshot, Limits);
            now = WebStockfishMoveChooser.InitializationTimeoutMilliseconds / 1000.0;
            Assert.That(coordinator.TryTakeResult(snapshot, out var result), Is.True);
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Does.Contain("carregamento"));
            Assert.That(bridge.Canceled, Is.EqualTo(1));
        }
    }

    [Test]
    public void CancellationAndOldRequestIdsCannotCompleteANewSearch()
    {
        var bridge = new Bridge();
        var snapshot = new ChessRulesAdapter().GetSnapshot();
        using (var chooser = new WebStockfishMoveChooser(bridge, () => 0))
        using (var cancellation = new CancellationTokenSource())
        {
            var old = chooser.ChooseMoveAsync(snapshot, Limits, cancellation.Token);
            int oldId = bridge.RequestId;
            cancellation.Cancel();
            Assert.That(old.IsCanceled, Is.True);
            Assert.That(bridge.Canceled, Is.EqualTo(1));
            var current = chooser.ChooseMoveAsync(snapshot, Limits, CancellationToken.None);
            bridge.Reply("bestmove", "d2d4", oldId);
            chooser.Poll();
            Assert.That(current.IsCompleted, Is.False);
            bridge.Reply("searching");
            bridge.Reply("bestmove", "e2e4");
            chooser.Poll();
            Assert.That(current.GetAwaiter().GetResult().ToUci(), Is.EqualTo("e2e4"));
        }
    }

    [Test]
    public void ResetDiscardsACompletedWebMoveByPositionRevision()
    {
        var bridge = new Bridge();
        var rules = new ChessRulesAdapter();
        using (var coordinator = new ComputerTurnCoordinator(new WebStockfishMoveChooser(bridge, () => 0)))
        {
            coordinator.Begin(rules.GetSnapshot(), Limits);
            rules.Reset();
            bridge.Reply("bestmove", "e2e4");
            Assert.That(coordinator.TryTakeResult(rules.GetSnapshot(), out _), Is.False);
            Assert.That(coordinator.IsThinking, Is.False);
        }
    }

    [TestCase("(none)")]
    [TestCase("0000")]
    [TestCase("e2e9")]
    public void InvalidAnswersFailWithoutAFallbackMove(string answer)
    {
        var bridge = new Bridge();
        using (var chooser = new WebStockfishMoveChooser(bridge, () => 0))
        {
            var task = chooser.ChooseMoveAsync(new ChessRulesAdapter().GetSnapshot(), Limits, CancellationToken.None);
            bridge.Reply("bestmove", answer);
            chooser.Poll();
            Assert.Throws<System.IO.InvalidDataException>(() => task.GetAwaiter().GetResult());
            Assert.That(bridge.Canceled, Is.EqualTo(1));
        }
    }

    [Test]
    public void WorkerErrorIsReadableAndTheChooserCanRetry()
    {
        var bridge = new Bridge();
        var snapshot = new ChessRulesAdapter().GetSnapshot();
        using (var chooser = new WebStockfishMoveChooser(bridge, () => 0))
        {
            var first = chooser.ChooseMoveAsync(snapshot, Limits, CancellationToken.None);
            bridge.Reply("error", "Nao foi possivel carregar a IA web.");
            chooser.Poll();
            var error = Assert.Throws<InvalidOperationException>(() => first.GetAwaiter().GetResult());
            Assert.That(error.Message, Does.Contain("carregar"));
            var retry = chooser.ChooseMoveAsync(snapshot, Limits, CancellationToken.None);
            bridge.Reply("bestmove", "e2e4");
            chooser.Poll();
            Assert.That(retry.IsCompleted, Is.True);
        }
    }

    [Test]
    public void DisposeCancelsAndRetainsNoPendingResult()
    {
        var bridge = new Bridge();
        var chooser = new WebStockfishMoveChooser(bridge, () => 0);
        var pending = chooser.ChooseMoveAsync(new ChessRulesAdapter().GetSnapshot(), Limits, CancellationToken.None);
        chooser.Dispose();
        chooser.Dispose();
        Assert.That(pending.IsCanceled, Is.True);
        Assert.That(bridge.Canceled, Is.EqualTo(1));
        Assert.That(bridge.Disposed, Is.True);
        Assert.Throws<ObjectDisposedException>(() => chooser.ChooseMoveAsync(new ChessRulesAdapter().GetSnapshot(), Limits, CancellationToken.None));
    }
}
