using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

public class MadChessMoveChooserTests
{
    [TestCase(ComputerDifficulty.Beginner)]
    [TestCase(ComputerDifficulty.Intermediate)]
    [TestCase(ComputerDifficulty.Hard)]
    public async Task EachDifficultyPlaysALegalMoveWithinItsMoveTime(ComputerDifficulty difficulty)
    {
        MoveSearchSettings settings = MoveSearchSettings.ForDifficulty(difficulty);
        using (var engine = new MadChessMoveChooser())
        using (var deadline = new CancellationTokenSource(settings.TimeoutMilliseconds))
        {
            var rules = new ChessRulesAdapter();
            var clock = Stopwatch.StartNew();
            ChessMove move = await engine.ChooseMoveAsync(rules.GetSnapshot(), settings, deadline.Token);
            Assert.That(clock.ElapsedMilliseconds, Is.LessThan(settings.MoveTimeMilliseconds + 500));
            Assert.That(rules.TryMove(move).Success, Is.True);
        }
    }

    [Test]
    public async Task BeginnerKeepsItsMoveTimeThroughoutAGame()
    {
        MoveSearchSettings settings = MoveSearchSettings.ForDifficulty(ComputerDifficulty.Beginner);
        using (var engine = new MadChessMoveChooser())
        using (var deadline = new CancellationTokenSource(30000))
        {
            var rules = new ChessRulesAdapter();
            const int plies = 20;
            var clock = Stopwatch.StartNew();
            for (int ply = 0; ply < plies; ply++)
            {
                ChessMove move = await engine.ChooseMoveAsync(rules.GetSnapshot(), settings, deadline.Token);
                MoveResult result = rules.TryMove(move);
                Assert.That(result.Success, Is.True);
                if (result.IsCheckmate || result.IsDraw) rules.Reset();
            }
            Assert.That(clock.ElapsedMilliseconds / plies, Is.LessThan(settings.MoveTimeMilliseconds * 2));
        }
    }

    [Test]
    public async Task ReplaysCastlingAndEnPassantHistoryAndKeepsPlaying()
    {
        using (var engine = new MadChessMoveChooser())
        using (var deadline = new CancellationTokenSource(10000))
        {
            var rules = new ChessRulesAdapter();
            foreach (string move in new[] { "e2e4", "g8f6", "e4e5", "d7d5", "e5d6", "e7d6", "g1f3", "f8e7", "f1e2", "e8g8", "e1g1" })
                Assert.That(rules.TryMove(ChessRulesTests.Move(move)).Success, Is.True, move);
            for (int ply = 0; ply < 6; ply++)
            {
                ChessMove reply = await engine.ChooseMoveAsync(rules.GetSnapshot(), new MoveSearchSettings(0, 30, 4000, 1200), deadline.Token);
                Assert.That(rules.TryMove(reply).Success, Is.True);
            }
        }
    }

    [Test]
    public async Task StrongestLimitFindsMateInOne()
    {
        using (var engine = new MadChessMoveChooser())
        using (var deadline = new CancellationTokenSource(5000))
        {
            var rules = new ChessRulesAdapter();
            foreach (string move in new[] { "f2f3", "e7e5", "g2g4" }) rules.TryMove(ChessRulesTests.Move(move));
            ChessMove mate = await engine.ChooseMoveAsync(rules.GetSnapshot(), new MoveSearchSettings(0, 300, 4000, 2600), deadline.Token);
            Assert.That(rules.TryMove(mate).IsCheckmate, Is.True);
        }
    }

    [Test]
    public async Task PromotionIsValidatedByLocalRules()
    {
        using (var engine = new MadChessMoveChooser())
        using (var deadline = new CancellationTokenSource(5000))
        {
            var rules = new ChessRulesAdapter("8/7P/8/8/8/2k5/1r6/K7 w - - 0 1");
            ChessMove move = await engine.ChooseMoveAsync(rules.GetSnapshot(), new MoveSearchSettings(0, 150, 4000), deadline.Token);
            Assert.That(move.Promotion.HasValue, Is.True);
            Assert.That(rules.TryMove(move).Success, Is.True);
        }
    }

    [Test]
    public async Task CancellationStopsTheSearchAndTheEngineKeepsWorking()
    {
        using (var engine = new MadChessMoveChooser())
        {
            var clock = Stopwatch.StartNew();
            using (var cancellation = new CancellationTokenSource(150))
            {
                try
                {
                    await engine.ChooseMoveAsync(new ChessRulesAdapter().GetSnapshot(),
                        new MoveSearchSettings(0, 10000, 12000, 2600), cancellation.Token);
                    Assert.Fail("The long search should have been canceled.");
                }
                catch (System.OperationCanceledException) { }
            }
            Assert.That(clock.ElapsedMilliseconds, Is.LessThan(1000));
            using (var deadline = new CancellationTokenSource(5000))
            {
                var rules = new ChessRulesAdapter();
                ChessMove move = await engine.ChooseMoveAsync(rules.GetSnapshot(), new MoveSearchSettings(0, 30, 4000), deadline.Token);
                Assert.That(rules.TryMove(move).Success, Is.True);
            }
        }
    }

    [Test]
    public void DisposeStopsARunningSearch()
    {
        var engine = new MadChessMoveChooser();
        Task<ChessMove> search = engine.ChooseMoveAsync(new ChessRulesAdapter().GetSnapshot(),
            new MoveSearchSettings(0, 10000, 12000, 2600), CancellationToken.None);
        Thread.Sleep(100);
        engine.Dispose();
        Assert.That(() => search.Wait(1000), Throws.InstanceOf<System.AggregateException>()
            .With.InnerException.InstanceOf<System.OperationCanceledException>());
    }
}
