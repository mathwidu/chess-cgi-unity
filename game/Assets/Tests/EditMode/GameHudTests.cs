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
            Assert.IsNotNull(hudObject.transform.Find("HudRoot/SelectedPiecePanel"));
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

    [Test]
    public void RefreshInterface_ShowsSelectedPieceDetails()
    {
        GameObject rig = CreatePlayableRig(out BoardView boardView, out ChessGameController controller, out GameHud hud);
        try
        {
            controller.NewGame();
            hud.RefreshInterface();

            Transform selectedPanel = hud.transform.Find("HudRoot/SelectedPiecePanel");
            Assert.IsNotNull(selectedPanel);
            Assert.IsFalse(selectedPanel.gameObject.activeSelf);

            PieceView pawn = boardView.Pieces.First(piece => piece.Square.Equals(BoardSquare.FromAlgebraic("e2")));
            controller.SelectPiece(pawn);
            hud.RefreshInterface();

            Assert.IsTrue(selectedPanel.gameObject.activeSelf);
            Transform preview = selectedPanel.Find("SelectedPiecePreview");
            Assert.IsNotNull(preview);
            Assert.IsNotNull(preview.GetComponent<RawImage>().texture);
            Assert.GreaterOrEqual(preview.GetComponent<RectTransform>().sizeDelta.y, 360f);
            StringAssert.Contains("Mathwidu", FindText(hud.transform, "HudRoot/SelectedPiecePanel/SelectedPieceNameText").text);
            StringAssert.Contains("Peao", FindText(hud.transform, "HudRoot/SelectedPiecePanel/SelectedPieceKindText").text);
            StringAssert.Contains("e2", FindText(hud.transform, "HudRoot/SelectedPiecePanel/SelectedPieceSquareText").text);
        }
        finally
        {
            Object.DestroyImmediate(rig);
        }
    }

    [Test]
    public void RefreshInterface_ShowsCustomBackRankModelNames()
    {
        GameObject rig = CreatePlayableRig(out BoardView boardView, out ChessGameController controller, out GameHud hud);
        try
        {
            controller.NewGame();

            AssertSelectedPieceHud(boardView, controller, hud, ChessPieceKind.Rook, "a1", "Alex", "Torre");
            AssertSelectedPieceHud(boardView, controller, hud, ChessPieceKind.Knight, "b1", "Gustavo", "Cavalo");
            AssertSelectedPieceHud(boardView, controller, hud, ChessPieceKind.Queen, "d1", "Marta", "Rainha");
            AssertSelectedPieceHud(boardView, controller, hud, ChessPieceKind.King, "e1", "Ricardo Carioca", "Rei");
        }
        finally
        {
            Object.DestroyImmediate(rig);
        }
    }

    [Test]
    public void RefreshInterface_ShowsCharacterProfileMetadata()
    {
        GameObject rig = CreatePlayableRig(out BoardView boardView, out ChessGameController controller, out GameHud hud);
        try
        {
            controller.NewGame();
            PieceView queen = boardView.Pieces.First(piece =>
                piece.Kind == ChessPieceKind.Queen &&
                piece.Side == ChessSide.White &&
                piece.Square.Equals(BoardSquare.FromAlgebraic("d1")));

            controller.SelectPiece(queen);
            hud.RefreshInterface();

            StringAssert.Contains("Professora Marta", FindText(hud.transform, "HudRoot/SelectedPiecePanel/SelectedPieceFullNameText").text);
            StringAssert.Contains("Professor", FindText(hud.transform, "HudRoot/SelectedPiecePanel/SelectedPieceRoleText").text);
            StringAssert.Contains("Professor", FindText(hud.transform, "HudRoot/SelectedPiecePanel/SelectedPieceRegistrationText").text);
            StringAssert.Contains("cachecol", FindText(hud.transform, "HudRoot/SelectedPiecePanel/SelectedPieceDescriptionText").text.ToLowerInvariant());
            StringAssert.Contains("Confident walk", FindText(hud.transform, "HudRoot/SelectedPiecePanel/SelectedPieceMovementText").text);
            StringAssert.Contains("Captura futura", FindText(hud.transform, "HudRoot/SelectedPiecePanel/SelectedPieceCaptureText").text);
        }
        finally
        {
            Object.DestroyImmediate(rig);
        }
    }

    private static void AssertSelectedPieceHud(
        BoardView boardView,
        ChessGameController controller,
        GameHud hud,
        ChessPieceKind kind,
        string square,
        string expectedModelName,
        string expectedKindName)
    {
        PieceView piece = boardView.Pieces.First(piece =>
            piece.Kind == kind &&
            piece.Side == ChessSide.White &&
            piece.Square.Equals(BoardSquare.FromAlgebraic(square)));
        controller.SelectPiece(piece);
        hud.RefreshInterface();

        StringAssert.Contains(expectedModelName, FindText(hud.transform, "HudRoot/SelectedPiecePanel/SelectedPieceNameText").text);
        StringAssert.Contains(expectedKindName, FindText(hud.transform, "HudRoot/SelectedPiecePanel/SelectedPieceKindText").text);
        StringAssert.Contains(square, FindText(hud.transform, "HudRoot/SelectedPiecePanel/SelectedPieceSquareText").text);
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
