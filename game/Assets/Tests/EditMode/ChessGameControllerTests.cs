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

    private static Transform CreateChild(Transform parent, string name)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent);
        return child.transform;
    }
}
