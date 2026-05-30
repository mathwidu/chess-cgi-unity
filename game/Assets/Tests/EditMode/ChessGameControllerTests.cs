using System.Linq;
using NUnit.Framework;
using UnityEngine;

public class ChessGameControllerTests
{
    [Test]
    public void SelectPiece_ForCurrentTurnHighlightsLegalDestinations()
    {
        GameObject rig = new GameObject("Controller Test Rig");
        try
        {
            BoardView boardView = rig.AddComponent<BoardView>();
            PieceFactory pieceFactory = rig.AddComponent<PieceFactory>();
            ChessGameController controller = rig.AddComponent<ChessGameController>();

            Transform squaresRoot = CreateChild(rig.transform, "Squares");
            Transform piecesRoot = CreateChild(rig.transform, "Pieces");
            Transform highlightsRoot = CreateChild(rig.transform, "Highlights");

            boardView.Configure(squaresRoot, piecesRoot, highlightsRoot, null, null, null);
            controller.Configure(boardView, pieceFactory, null);
            controller.NewGame();

            PieceView pawn = boardView.Pieces.First(piece => piece.Square.Equals(BoardSquare.FromAlgebraic("e2")));

            controller.SelectPiece(pawn);

            Assert.AreSame(pawn, controller.SelectedPiece);
            Assert.AreEqual(2, boardView.HighlightCount);
        }
        finally
        {
            Object.DestroyImmediate(rig);
        }
    }

    [Test]
    public void MoveToLegalDestination_AlternatesTurnAndUpdatesCameraPerspective()
    {
        GameObject rig = new GameObject("Controller Test Rig");
        try
        {
            BoardView boardView = rig.AddComponent<BoardView>();
            PieceFactory pieceFactory = rig.AddComponent<PieceFactory>();
            CameraController cameraController = rig.AddComponent<CameraController>();
            ChessGameController controller = rig.AddComponent<ChessGameController>();

            Transform squaresRoot = CreateChild(rig.transform, "Squares");
            Transform piecesRoot = CreateChild(rig.transform, "Pieces");
            Transform highlightsRoot = CreateChild(rig.transform, "Highlights");

            boardView.Configure(squaresRoot, piecesRoot, highlightsRoot, null, null, null);
            controller.Configure(boardView, pieceFactory, null, cameraController);
            controller.NewGame();

            PieceView pawn = boardView.Pieces.First(piece => piece.Square.Equals(BoardSquare.FromAlgebraic("e2")));

            controller.SelectPiece(pawn);
            controller.SelectDestination(BoardSquare.FromAlgebraic("e4"));

            Assert.AreEqual(ChessSide.Black, controller.CurrentTurn);
            Assert.AreEqual(ChessSide.Black, cameraController.CurrentPerspective);
        }
        finally
        {
            Object.DestroyImmediate(rig);
        }
    }

    [Test]
    public void MoveToLegalDestination_RecordsMoveHistoryAndClearsItOnNewGame()
    {
        GameObject rig = CreatePlayableRig(out BoardView boardView, out ChessGameController controller, out _);
        try
        {
            Move(controller, boardView, "e2", "e4");
            Move(controller, boardView, "e7", "e5");

            CollectionAssert.AreEqual(
                new[] { "Brancas: e2-e4", "Pretas: e7-e5" },
                controller.MoveHistory);

            controller.NewGame();

            Assert.AreEqual(0, controller.MoveHistory.Count);
        }
        finally
        {
            Object.DestroyImmediate(rig);
        }
    }

    [Test]
    public void ChoosePromotion_FromPendingPromotion_ReplacesPieceAndRecordsMove()
    {
        GameObject rig = CreatePlayableRig(out BoardView boardView, out ChessGameController controller, out _);
        try
        {
            Move(controller, boardView, "a2", "a4");
            Move(controller, boardView, "h7", "h5");
            Move(controller, boardView, "a4", "a5");
            Move(controller, boardView, "h5", "h4");
            Move(controller, boardView, "a5", "a6");
            Move(controller, boardView, "h4", "h3");
            Move(controller, boardView, "a6", "b7");
            Move(controller, boardView, "h3", "g2");

            BeginMove(controller, boardView, "b7", "a8");

            Assert.IsTrue(controller.IsAwaitingPromotion);
            Assert.AreEqual("Escolha a promocao.", controller.StatusMessage);

            controller.ChoosePromotion('Q');

            PieceView promotedPiece = boardView.Pieces.First(piece => piece.Square.Equals(BoardSquare.FromAlgebraic("a8")));
            Assert.IsFalse(controller.IsAwaitingPromotion);
            Assert.AreEqual(ChessPieceKind.Queen, promotedPiece.Kind);
            CollectionAssert.Contains(controller.MoveHistory.ToArray(), "Brancas: b7xa8=Q");
        }
        finally
        {
            Object.DestroyImmediate(rig);
        }
    }

    private static GameObject CreatePlayableRig(
        out BoardView boardView,
        out ChessGameController controller,
        out CameraController cameraController)
    {
        GameObject rig = new GameObject("Controller Test Rig");
        boardView = rig.AddComponent<BoardView>();
        PieceFactory pieceFactory = rig.AddComponent<PieceFactory>();
        cameraController = rig.AddComponent<CameraController>();
        controller = rig.AddComponent<ChessGameController>();

        Transform squaresRoot = CreateChild(rig.transform, "Squares");
        Transform piecesRoot = CreateChild(rig.transform, "Pieces");
        Transform highlightsRoot = CreateChild(rig.transform, "Highlights");

        boardView.Configure(squaresRoot, piecesRoot, highlightsRoot, null, null, null);
        controller.Configure(boardView, pieceFactory, null, cameraController);
        controller.NewGame();
        return rig;
    }

    private static void Move(ChessGameController controller, BoardView boardView, string from, string to)
    {
        BeginMove(controller, boardView, from, to);
        Assert.IsFalse(controller.IsAwaitingPromotion, $"{from}-{to} unexpectedly needs promotion.");
    }

    private static void BeginMove(ChessGameController controller, BoardView boardView, string from, string to)
    {
        PieceView piece = boardView.Pieces.First(piece => piece.Square.Equals(BoardSquare.FromAlgebraic(from)));
        controller.SelectPiece(piece);
        controller.SelectDestination(BoardSquare.FromAlgebraic(to));
    }

    private static Transform CreateChild(Transform parent, string name)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent);
        return child.transform;
    }
}
