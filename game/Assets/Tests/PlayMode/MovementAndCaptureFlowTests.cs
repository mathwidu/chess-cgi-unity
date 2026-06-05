using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class MovementAndCaptureFlowTests
{
    [UnityTest]
    public IEnumerator LegalMove_CompletesAfterMotionAndChangesTurn()
    {
        GameObject rig = CreatePlayableRig(out BoardView boardView, out ChessGameController controller);
        try
        {
            yield return null;
            PieceView pawn = boardView.FindPieceAt(BoardSquare.FromAlgebraic("e2"));

            controller.SelectPiece(pawn);
            controller.SelectDestination(BoardSquare.FromAlgebraic("e4"));

            yield return new WaitForSeconds(1.2f);

            Assert.AreEqual(ChessSide.Black, controller.CurrentTurn);
            Assert.IsFalse(controller.IsInputBlocked);
            Assert.IsNotNull(boardView.FindPieceAt(BoardSquare.FromAlgebraic("e4")));
        }
        finally
        {
            Object.Destroy(rig);
        }
    }

    [UnityTest]
    public IEnumerator Capture_CompletesAfterImpactAndRemovesCapturedPiece()
    {
        GameObject rig = CreatePlayableRig(out BoardView boardView, out ChessGameController controller);
        try
        {
            yield return null;
            Move(controller, boardView, "e2", "e4");
            yield return new WaitForSeconds(1.2f);
            Move(controller, boardView, "d7", "d5");
            yield return new WaitForSeconds(1.2f);
            Move(controller, boardView, "e4", "d5");
            yield return new WaitForSeconds(1.4f);

            PieceView whitePawn = boardView.FindPieceAt(BoardSquare.FromAlgebraic("d5"));
            Assert.IsNotNull(whitePawn);
            Assert.AreEqual(ChessSide.White, whitePawn.Side);
            Assert.AreEqual(ChessSide.Black, controller.CurrentTurn);
            Assert.IsFalse(controller.IsInputBlocked);
        }
        finally
        {
            Object.Destroy(rig);
        }
    }

    private static void Move(ChessGameController controller, BoardView boardView, string from, string to)
    {
        PieceView piece = boardView.FindPieceAt(BoardSquare.FromAlgebraic(from));
        Assert.IsNotNull(piece, from);
        controller.SelectPiece(piece);
        controller.SelectDestination(BoardSquare.FromAlgebraic(to));
    }

    private static GameObject CreatePlayableRig(out BoardView boardView, out ChessGameController controller)
    {
        GameObject rig = new GameObject("PlayMode Rig");
        boardView = rig.AddComponent<BoardView>();
        PieceFactory factory = rig.AddComponent<PieceFactory>();
        rig.AddComponent<PieceMotionController>();
        controller = rig.AddComponent<ChessGameController>();

        Transform squares = new GameObject("Squares").transform;
        Transform pieces = new GameObject("Pieces").transform;
        Transform highlights = new GameObject("Highlights").transform;
        squares.SetParent(rig.transform);
        pieces.SetParent(rig.transform);
        highlights.SetParent(rig.transform);

        boardView.Configure(squares, pieces, highlights, null, null, null);
        controller.Configure(boardView, factory, null);
        return rig;
    }
}
