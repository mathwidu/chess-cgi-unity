using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class GameHudTests
{
    [Test]
    public void RebuildInterface_CreatesCanvasHudRoots()
    {
        GameObject hudObject = new GameObject("HUD Test Canvas");
        try
        {
            GameHud hud = hudObject.AddComponent<GameHud>();

            hud.RebuildInterface();

            Assert.IsNotNull(hudObject.GetComponent<Canvas>());
            Assert.IsNotNull(hudObject.GetComponent<CanvasScaler>());
            Assert.IsNotNull(hudObject.GetComponent<GraphicRaycaster>());
            Assert.IsNotNull(hudObject.transform.Find("HudRoot/TopBar"));
            Assert.IsNotNull(hudObject.transform.Find("HudRoot/TopBar/BrandPanel"));
            Assert.IsNotNull(hudObject.transform.Find("HudRoot/TopBar/TurnPanel"));
            Assert.IsNotNull(hudObject.transform.Find("HudRoot/MoveHistoryPanel"));
            Assert.IsNotNull(hudObject.transform.Find("HudRoot/ActionBar"));
            Assert.IsNotNull(hudObject.transform.Find("HudRoot/PromotionPanel"));
            Assert.IsNotNull(hudObject.transform.Find("HudRoot/StartOverlay"));
        }
        finally
        {
            Object.DestroyImmediate(hudObject);
        }
    }

    [Test]
    public void RefreshInterface_UpdatesTurnAndMoveHistoryText()
    {
        GameObject rig = CreatePlayableRig(out BoardView boardView, out ChessGameController controller, out GameHud hud);
        try
        {
            controller.NewGame();
            hud.RefreshInterface();

            Text turnText = FindText(hud.transform, "HudRoot/TopBar/TurnPanel/TurnText");
            Assert.AreEqual("Brancas jogam", turnText.text);

            Move(controller, boardView, "e2", "e4");
            hud.RefreshInterface();

            Assert.AreEqual("Pretas jogam", turnText.text);
            Text historyText = FindText(hud.transform, "HudRoot/MoveHistoryPanel/MoveHistoryText");
            StringAssert.Contains("1. Brancas: e2-e4", historyText.text);
        }
        finally
        {
            Object.DestroyImmediate(rig);
        }
    }

    private static GameObject CreatePlayableRig(
        out BoardView boardView,
        out ChessGameController controller,
        out GameHud hud)
    {
        GameObject rig = new GameObject("HUD Controller Test Rig");
        boardView = rig.AddComponent<BoardView>();
        PieceFactory pieceFactory = rig.AddComponent<PieceFactory>();
        CameraController cameraController = rig.AddComponent<CameraController>();
        controller = rig.AddComponent<ChessGameController>();

        GameObject hudObject = new GameObject("Canvas");
        hudObject.transform.SetParent(rig.transform);
        hud = hudObject.AddComponent<GameHud>();

        Transform squaresRoot = CreateChild(rig.transform, "Squares");
        Transform piecesRoot = CreateChild(rig.transform, "Pieces");
        Transform highlightsRoot = CreateChild(rig.transform, "Highlights");

        boardView.Configure(squaresRoot, piecesRoot, highlightsRoot, null, null, null);
        controller.Configure(boardView, pieceFactory, hud, cameraController);
        hud.RebuildInterface();
        return rig;
    }

    private static void Move(ChessGameController controller, BoardView boardView, string from, string to)
    {
        PieceView piece = boardView.Pieces.First(piece => piece.Square.Equals(BoardSquare.FromAlgebraic(from)));
        controller.SelectPiece(piece);
        controller.SelectDestination(BoardSquare.FromAlgebraic(to));
    }

    private static Text FindText(Transform root, string path)
    {
        Transform target = root.Find(path);
        Assert.IsNotNull(target, path);
        Text text = target.GetComponent<Text>();
        Assert.IsNotNull(text, path);
        return text;
    }

    private static Transform CreateChild(Transform parent, string name)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent);
        return child.transform;
    }
}
