using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class CapturedPiecesTests
{
    private GameObject root;
    private ChessGameController controller;
    private BoardView board;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        root = new GameObject("Captured pieces test");
        board = root.AddComponent<BoardView>();
        var factory = root.AddComponent<PieceFactory>();
        controller = root.AddComponent<ChessGameController>();
        controller.Configure(board, factory, null);
        controller.StartLocalGame();
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        UnityEngine.Object.Destroy(root);
        yield return null;
    }

    private CapturedPiecesView View => board.CapturedPieces;

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
        // Let the view catch up with the landed move and finish the capture flight.
        yield return new WaitForSecondsRealtime(0.6f);
    }

    private static IEnumerator WaitFor(Func<bool> condition)
    {
        float deadline = Time.realtimeSinceStartup + 5f;
        while (!condition() && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(condition(), Is.True, "The match did not reach its expected state.");
    }

    [UnityTest]
    public IEnumerator ACaptureShowsTheTakenPieceBesideTheBoard()
    {
        Assert.That(View.DisplayedCount, Is.Zero);
        yield return Play("e2e4", "d7d5", "e4d5");

        Assert.That(controller.CapturedPieces.Count, Is.EqualTo(1));
        Assert.That(controller.CapturedPieces[0].Kind, Is.EqualTo(ChessPieceKind.Pawn));
        Assert.That(controller.CapturedPieces[0].Side, Is.EqualTo(ChessSide.Black));
        Assert.That(controller.MaterialBalance, Is.EqualTo(1));
        Assert.That(View.DisplayedCount, Is.EqualTo(1));
        Assert.That(View.NearCount, Is.EqualTo(1), "White sits on the near side and keeps what it took.");
        Assert.That(View.BalanceText, Is.EqualTo("+1"));
        Assert.That(board.Pieces.Count, Is.EqualTo(31));

        Transform shown = View.transform.Find("Pieces").GetChild(0);
        Assert.That(shown.GetComponentInChildren<PieceView>(), Is.Null, "A captured piece is only a look-alike.");
        Assert.That(shown.GetComponentsInChildren<Collider>().Any(c => c.enabled), Is.False, "It never catches a click or the VR hand.");
        Assert.That(Mathf.Abs(shown.localPosition.x), Is.GreaterThan(board.SquareSize * 4f), "It stands beside the board, not on it.");
    }

    [UnityTest]
    public IEnumerator EachSideKeepsItsOwnCapturesAndEvenMaterialHidesTheScore()
    {
        yield return Play("e2e4", "d7d5", "e4d5", "d8d5");

        Assert.That(View.NearCount, Is.EqualTo(1));
        Assert.That(View.FarCount, Is.EqualTo(1));
        Assert.That(controller.MaterialBalance, Is.Zero);
        Assert.That(View.BalanceText, Is.Empty);
    }

    [UnityTest]
    public IEnumerator ANewGameClearsTheCaptures()
    {
        yield return Play("e2e4", "d7d5", "e4d5");
        controller.NewGame();
        yield return null;
        Assert.That(controller.CapturedPieces, Is.Empty);
        Assert.That(View.DisplayedCount, Is.Zero);
        Assert.That(View.BalanceText, Is.Empty);
    }
}
