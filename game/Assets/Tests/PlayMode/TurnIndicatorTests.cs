using System;
using System.Collections;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class TurnIndicatorTests
{
    private GameObject root;
    private ChessGameController controller;
    private BoardView board;

    private sealed class DeferredChooser : IMoveChooser
    {
        public readonly TaskCompletionSource<ChessMove> Reply = new TaskCompletionSource<ChessMove>();
        public Task<ChessMove> ChooseMoveAsync(PositionSnapshot p, MoveSearchSettings s, CancellationToken token) => Reply.Task;
        public void Dispose() { }
    }

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        root = new GameObject("Turn indicator test");
        board = root.AddComponent<BoardView>();
        var factory = root.AddComponent<PieceFactory>();
        controller = root.AddComponent<ChessGameController>();
        controller.Configure(board, factory, null);
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        UnityEngine.Object.Destroy(root);
        yield return null;
    }

    private TurnIndicatorView Indicator => board.TurnIndicator;

    private void HumanMove(string from, string to)
    {
        controller.SelectPiece(board.Pieces.First(p => p.Square.Equals(BoardSquare.FromAlgebraic(from))));
        controller.SelectDestination(BoardSquare.FromAlgebraic(to));
    }

    private static IEnumerator WaitFor(Func<bool> condition, float seconds = 5f)
    {
        float deadline = Time.realtimeSinceStartup + seconds;
        while (!condition() && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(condition(), Is.True, "The turn indicator did not reach its expected state.");
    }

    [UnityTest]
    public IEnumerator LocalGameLightsTheEdgeOfTheSideToMove()
    {
        controller.StartLocalGame();
        yield return null;
        Assert.That(Indicator.IsShown, Is.True);
        Assert.That(Indicator.LitSide, Is.EqualTo(ChessSide.White));
        Assert.That(Indicator.LabelText, Is.EqualTo("Vez das brancas"));
        Transform light = Indicator.transform.Find("TurnLight");
        Assert.That(light.localPosition.z, Is.LessThan(-board.SquareSize * 4f), "The light sits on the rim, outside the squares.");

        HumanMove("e2", "e4");
        yield return WaitFor(() => !controller.IsInputBlocked);
        yield return null;
        Assert.That(Indicator.LitSide, Is.EqualTo(ChessSide.Black));
        Assert.That(Indicator.LabelText, Is.EqualTo("Vez das pretas"));
        Assert.That(light.localPosition.z, Is.GreaterThan(board.SquareSize * 4f));

        controller.ReturnToMenu();
        yield return null;
        Assert.That(Indicator.IsShown, Is.False, "No turn cue while the menu is open.");
    }

    [UnityTest]
    public IEnumerator AgainstTheComputerYourTurnIsHighlightedAndItsTurnIsMuted()
    {
        var chooser = new DeferredChooser();
        controller.SetMoveChooserFactory(() => chooser);
        controller.StartComputerGame(ChessSide.Black, ComputerDifficulty.Beginner);
        yield return null;
        Assert.That(Indicator.LitSide, Is.EqualTo(ChessSide.White));
        Assert.That(Indicator.LitForPlayer, Is.False);
        Assert.That(Indicator.LabelText, Is.EqualTo("IA pensando..."));

        ChessMove.TryParseUci("e2e4", out ChessMove reply);
        chooser.Reply.SetResult(reply);
        yield return WaitFor(() => controller.MoveHistory.Count == 1 && !controller.IsInputBlocked);
        yield return null;
        Assert.That(Indicator.LitSide, Is.EqualTo(ChessSide.Black));
        Assert.That(Indicator.LitForPlayer, Is.True);
        Assert.That(Indicator.LabelText, Is.EqualTo("Sua vez"));
        yield return WaitFor(() => Indicator.LabelAlpha > 0.99f, 1f);
        // The label fades after a moment; the light on the rim stays.
        yield return WaitFor(() => Indicator.LabelAlpha < 0.01f, 4.5f);
        Assert.That(Indicator.IsShown, Is.True);
    }

    [UnityTest]
    public IEnumerator TheCueDisappearsWhenTheMatchEnds()
    {
        controller.StartLocalGame();
        foreach (string[] move in new[] { new[] { "f2", "f3" }, new[] { "e7", "e5" }, new[] { "g2", "g4" }, new[] { "d8", "h4" } })
        {
            yield return WaitFor(() => !controller.IsInputBlocked);
            HumanMove(move[0], move[1]);
        }

        yield return WaitFor(() => controller.IsGameOver && !controller.IsInputBlocked);
        yield return null;
        Assert.That(Indicator.IsShown, Is.False);
    }
}
