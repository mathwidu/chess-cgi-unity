using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class BoardFeedbackTests
{
    private GameObject root;
    private ChessGameController controller;
    private BoardView board;

    private sealed class ScriptedMoveChooser : IMoveChooser
    {
        private readonly Queue<string> moves;
        public ScriptedMoveChooser(params string[] moves) { this.moves = new Queue<string>(moves); }
        public Task<ChessMove> ChooseMoveAsync(PositionSnapshot p, MoveSearchSettings s, CancellationToken token)
        {
            ChessMove.TryParseUci(moves.Dequeue(), out ChessMove move);
            return Task.FromResult(move);
        }
        public void Dispose() { }
    }

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        root = new GameObject("Board feedback test");
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

    private IEnumerator Play(params string[] moves)
    {
        foreach (string uci in moves)
        {
            yield return WaitFor(() => !controller.IsInputBlocked);
            ChessMove.TryParseUci(uci, out ChessMove move);
            controller.SelectPiece(board.Pieces.First(p => p.Square.Equals(move.From)));
            controller.SelectDestination(move.To);
        }

        yield return WaitFor(() => !controller.IsInputBlocked);
        yield return null;
    }

    private static IEnumerator WaitFor(Func<bool> condition, float seconds = 5f)
    {
        float deadline = Time.realtimeSinceStartup + seconds;
        while (!condition() && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(condition(), Is.True, "The board did not reach its expected state.");
    }

    private Renderer SquareRenderer(string square) =>
        board.Squares.First(s => s.Square.Equals(BoardSquare.FromAlgebraic(square))).GetComponent<Renderer>();

    [UnityTest]
    public IEnumerator TheLastMoveIsTintedOnItsTwoSquares()
    {
        controller.StartLocalGame();
        yield return Play("e2e4");
        Assert.That(board.LastMoveFrom, Is.EqualTo(BoardSquare.FromAlgebraic("e2")));
        Assert.That(board.LastMoveTo, Is.EqualTo(BoardSquare.FromAlgebraic("e4")));
        Assert.That(SquareRenderer("e2").HasPropertyBlock(), Is.True);
        Assert.That(SquareRenderer("e4").HasPropertyBlock(), Is.True);
        Assert.That(SquareRenderer("d4").HasPropertyBlock(), Is.False);

        yield return Play("e7e5");
        Assert.That(SquareRenderer("e2").HasPropertyBlock(), Is.False, "Only the latest move stays marked.");
        Assert.That(SquareRenderer("e7").HasPropertyBlock(), Is.True);

        controller.NewGame();
        Assert.That(board.LastMoveTo, Is.Null);
        Assert.That(board.Squares.Any(s => s.GetComponent<Renderer>().HasPropertyBlock()), Is.False);
    }

    [UnityTest]
    public IEnumerator AKingInCheckGlowsRedUntilTheCheckIsAnswered()
    {
        controller.StartLocalGame();
        yield return Play("e2e4", "f7f6", "d1h5");
        Assert.That(controller.CheckedKing, Is.EqualTo(BoardSquare.FromAlgebraic("e8")));
        Assert.That(board.CheckSquare, Is.EqualTo(BoardSquare.FromAlgebraic("e8")));
        var properties = new MaterialPropertyBlock();
        SquareRenderer("e8").GetPropertyBlock(properties);
        Color tint = properties.GetColor("_BaseColor");
        Assert.That(tint.r, Is.GreaterThan(tint.g + 0.2f), "The king's square turns red.");

        yield return Play("g7g6");
        Assert.That(controller.CheckedKing, Is.Null);
        Assert.That(board.CheckSquare, Is.Null);
    }

    [UnityTest]
    public IEnumerator EachLandedMoveMakesItsSound()
    {
        controller.StartLocalGame();
        yield return Play("e2e4", "d7d5", "e4d5", "e8d7");
        yield return new WaitForSecondsRealtime(0.4f);
        CollectionAssert.AreEqual(new[] { "Move", "Move", "Capture", "Move" }, board.Sounds.Played.ToArray());

        yield return Play("d1g4");
        yield return new WaitForSecondsRealtime(0.4f);
        Assert.That(board.Sounds.Played.Skip(4), Is.EqualTo(new[] { "Move", "Check" }));
    }

    [UnityTest]
    public IEnumerator TheComputerHandingBackTheTurnAndLosingSoundDifferently()
    {
        controller.SetMoveChooserFactory(() => new ScriptedMoveChooser("e7e5", "d8h4"));
        controller.StartComputerGame(ChessSide.White, ComputerDifficulty.Beginner);
        yield return Play("f2f3");
        yield return WaitFor(() => controller.MoveHistory.Count == 2 && !controller.IsInputBlocked);
        yield return new WaitForSecondsRealtime(0.4f);
        Assert.That(board.Sounds.Played.Last(), Is.EqualTo("Turn"), "The computer's reply hands the turn back with a soft cue.");

        yield return Play("g2g4");
        yield return WaitFor(() => controller.IsGameOver && !controller.IsInputBlocked);
        yield return new WaitForSecondsRealtime(0.5f);
        Assert.That(board.Sounds.Played.Last(), Is.EqualTo("Loss"));
    }

    [UnityTest]
    public IEnumerator ReleasingAGrabbedPieceTellsWhetherTheDropWasAccepted()
    {
        controller.StartLocalGame();
        yield return null;
        PieceView pawn = board.Pieces.First(p => p.Square.Equals(BoardSquare.FromAlgebraic("e2")));
        controller.GrabPiece(pawn);
        Assert.That(controller.ReleasePiece(pawn, board.GetPieceWorldPosition(BoardSquare.FromAlgebraic("e5"))), Is.False);
        yield return WaitFor(() => !controller.IsInputBlocked);

        controller.GrabPiece(pawn);
        Assert.That(controller.ReleasePiece(pawn, board.GetPieceWorldPosition(BoardSquare.FromAlgebraic("e4"))), Is.True);
        yield return WaitFor(() => controller.MoveHistory.Count == 1);
    }
}
