using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

[Category("StockfishIntegration")]
public class StockfishIntegrationTests
{
    private string path;

    [SetUp]
    public void FindEngine()
    {
        path = ComputerOpponentFactory.FindExecutable();
        if (!File.Exists(path)) Assert.Ignore("Run tools/setup_stockfish.py or set CHESS_STOCKFISH_PATH for engine integration tests.");
    }

    [Test]
    public async Task RealEngineFindsMateAndKeepsASessionAcrossTurns()
    {
        using (var engine = new StockfishUciMoveChooser(path))
        using (var deadline = new CancellationTokenSource(10000))
        {
            var rules = new ChessRulesAdapter();
            // Exercise the same persistent process in two different positions.
            ChessMove opening = await engine.ChooseMoveAsync(rules.GetSnapshot(), new MoveSearchSettings(20, 50, 5000), deadline.Token);
            Assert.That(rules.TryMove(opening).Success, Is.True);
            rules.Reset();
            foreach (string move in new[] { "f2f3", "e7e5", "g2g4" }) rules.TryMove(ChessRulesTests.Move(move));
            ChessMove mate = await engine.ChooseMoveAsync(rules.GetSnapshot(), new MoveSearchSettings(20, 150, 5000), deadline.Token);
            Assert.That(rules.TryMove(mate).IsCheckmate, Is.True);
        }
    }

    [Test]
    public async Task CancellationStopsARealSearchAndAnotherSessionCanStart()
    {
        using (var engine = new StockfishUciMoveChooser(path))
        {
            using (var cancellation = new CancellationTokenSource(150))
            {
                try
                {
                    await engine.ChooseMoveAsync(new ChessRulesAdapter().GetSnapshot(),
                        new MoveSearchSettings(20, 10000, 12000), cancellation.Token);
                    Assert.Fail("The long search should have been canceled.");
                }
                catch (System.OperationCanceledException) { }
            }
            using (var deadline = new CancellationTokenSource(5000))
            {
                var rules = new ChessRulesAdapter();
                ChessMove move = await engine.ChooseMoveAsync(rules.GetSnapshot(), new MoveSearchSettings(0, 30, 4000), deadline.Token);
                Assert.That(rules.TryMove(move).Success, Is.True);
            }
        }
    }

    [Test]
    public async Task RealEnginePromotionIsValidatedByLocalRules()
    {
        using (var engine = new StockfishUciMoveChooser(path))
        using (var deadline = new CancellationTokenSource(5000))
        {
            // The king has no legal move, so all legal candidates are promotions.
            var rules = new ChessRulesAdapter("8/7P/8/8/8/2k5/1r6/K7 w - - 0 1");
            ChessMove move = await engine.ChooseMoveAsync(rules.GetSnapshot(), new MoveSearchSettings(20, 150, 4000), deadline.Token);
            Assert.That(move.Promotion.HasValue, Is.True);
            Assert.That(rules.TryMove(move).Success, Is.True);
        }
    }
}
