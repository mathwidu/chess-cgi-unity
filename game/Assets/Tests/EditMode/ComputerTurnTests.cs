using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

public class ComputerTurnTests
{
    private sealed class DeferredChooser : IMoveChooser
    {
        public readonly TaskCompletionSource<ChessMove> Reply = new TaskCompletionSource<ChessMove>();
        public CancellationToken Token;
        public Task<ChessMove> ChooseMoveAsync(PositionSnapshot position, MoveSearchSettings settings, CancellationToken token)
        { Token = token; return Reply.Task; }
        public void Dispose() { }
    }

    [Test]
    public async Task TimeoutPreservesPositionAndCancelsEvenAnUncooperativeProvider()
    {
        var provider = new DeferredChooser();
        var position = new ChessRulesAdapter().GetSnapshot();
        using (var coordinator = new ComputerTurnCoordinator(provider))
        {
            coordinator.Begin(position, new MoveSearchSettings(0, 1, 25));
            await Task.Delay(100);
            Assert.That(coordinator.TryTakeResult(position, out var result), Is.True);
            Assert.That(result.Success, Is.False);
            Assert.That(provider.Token.IsCancellationRequested, Is.True);
            provider.Reply.SetResult(ChessRulesTests.Move("e2e4"));
            Assert.That(coordinator.TryTakeResult(position, out _), Is.False);
        }
    }

    [Test]
    public async Task AResponseFromBeforeResetIsDiscarded()
    {
        var provider = new DeferredChooser();
        var rules = new ChessRulesAdapter();
        using (var coordinator = new ComputerTurnCoordinator(provider))
        {
            coordinator.Begin(rules.GetSnapshot(), MoveSearchSettings.ForDifficulty(ComputerDifficulty.Beginner));
            rules.Reset();
            provider.Reply.SetResult(ChessRulesTests.Move("e2e4"));
            await Task.Delay(30);
            Assert.That(coordinator.TryTakeResult(rules.GetSnapshot(), out _), Is.False);
            Assert.That(coordinator.IsThinking, Is.False);
        }
    }

    [Test]
    public async Task CancellationDiscardsALateFailure()
    {
        var provider = new DeferredChooser();
        var position = new ChessRulesAdapter().GetSnapshot();
        using (var coordinator = new ComputerTurnCoordinator(provider))
        {
            coordinator.Begin(position, MoveSearchSettings.ForDifficulty(ComputerDifficulty.Hard));
            coordinator.Cancel();
            provider.Reply.SetException(new InvalidOperationException("Late failure"));
            await Task.Delay(30);
            Assert.That(coordinator.IsThinking, Is.False);
            Assert.That(coordinator.TryTakeResult(position, out _), Is.False);
        }
    }

    [Test]
    public async Task ProviderFailureIsRecoverable()
    {
        var provider = new DeferredChooser();
        var position = new ChessRulesAdapter().GetSnapshot();
        using (var coordinator = new ComputerTurnCoordinator(provider))
        {
            coordinator.Begin(position, MoveSearchSettings.ForDifficulty(ComputerDifficulty.Beginner));
            provider.Reply.SetException(new InvalidOperationException("Engine stopped"));
            await Task.Delay(30);
            Assert.That(coordinator.TryTakeResult(position, out var result), Is.True);
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Does.Contain("Engine stopped"));
        }
    }

    [Test]
    public async Task MissingExecutableFailsWithoutHanging()
    {
        using (var provider = new StockfishUciMoveChooser("/missing/stockfish"))
        {
            try
            {
                await provider.ChooseMoveAsync(new ChessRulesAdapter().GetSnapshot(),
                    MoveSearchSettings.ForDifficulty(ComputerDifficulty.Beginner), CancellationToken.None);
                Assert.Fail("Missing engine should fail.");
            }
            catch (System.IO.FileNotFoundException) { }
        }
    }
}
