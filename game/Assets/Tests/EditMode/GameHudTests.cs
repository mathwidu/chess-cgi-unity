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
            Assert.GreaterOrEqual(selectedPanel.GetComponent<RectTransform>().sizeDelta.x, 360f);
            Assert.GreaterOrEqual(selectedPanel.GetComponent<RectTransform>().sizeDelta.y, 520f);
            Assert.GreaterOrEqual(preview.GetComponent<RectTransform>().sizeDelta.y, 300f);
            RenderTexture previewTexture = preview.GetComponent<RawImage>().texture as RenderTexture;
            Assert.IsNotNull(previewTexture);
            Assert.GreaterOrEqual(previewTexture.width, 768);
            Assert.GreaterOrEqual(previewTexture.height, 640);
            Assert.IsTrue(preview.GetComponent<RawImage>().raycastTarget);
            Assert.IsNotNull(preview.GetComponent<SelectedPiecePreviewInput>());
            Assert.IsNotNull(selectedPanel.Find("PreviewZoomInButton"));
            Assert.IsNotNull(selectedPanel.Find("PreviewZoomOutButton"));
            Assert.IsNotNull(selectedPanel.Find("PreviewZoomInButton").GetComponent<Button>());
            Assert.IsNotNull(selectedPanel.Find("PreviewZoomOutButton").GetComponent<Button>());
            StringAssert.Contains("Matheus Duarte", FindText(hud.transform, "HudRoot/SelectedPiecePanel/SelectedPieceNameText").text);
            StringAssert.Contains("Peao", FindText(hud.transform, "HudRoot/SelectedPiecePanel/SelectedPieceKindText").text);
            StringAssert.Contains("e2", FindText(hud.transform, "HudRoot/SelectedPiecePanel/SelectedPieceSquareText").text);
            Text profileText = FindText(hud.transform, "HudRoot/SelectedPiecePanel/SelectedPieceProfileText");
            StringAssert.Contains("Nome: Matheus Duarte", profileText.text);
            StringAssert.Contains("Categoria: Criador do jogo", profileText.text);
            StringAssert.Contains("Registro: Matricula 0276899", profileText.text);
            StringAssert.Contains("criador do jogo", FindText(hud.transform, "HudRoot/SelectedPiecePanel/SelectedPieceDescriptionText").text);
        }
        finally
        {
            Object.DestroyImmediate(rig);
        }
    }

    [Test]
    public void SelectedPiecePreviewInput_RotatesModelAndZoomsCamera()
    {
        GameObject rig = CreatePlayableRig(out BoardView boardView, out ChessGameController controller, out GameHud hud);
        try
        {
            controller.NewGame();
            PieceView pawn = boardView.Pieces.First(piece => piece.Square.Equals(BoardSquare.FromAlgebraic("e2")));
            controller.SelectPiece(pawn);
            hud.RefreshInterface();

            Transform preview = hud.transform.Find("HudRoot/SelectedPiecePanel/SelectedPiecePreview");
            SelectedPiecePreviewInput input = preview.GetComponent<SelectedPiecePreviewInput>();
            Assert.IsNotNull(input);
            Assert.IsTrue(input.HasInteractivePreview);

            Quaternion beforeRotation = input.TargetRotationForTests;
            float beforeDistance = input.CameraDistanceForTests;
            Assert.Greater(beforeDistance, 1.35f);
            Assert.Less(beforeDistance, 5.4f);

            input.RotatePreview(45f);
            Button zoomInButton = hud.transform.Find("HudRoot/SelectedPiecePanel/PreviewZoomInButton").GetComponent<Button>();
            Button zoomOutButton = hud.transform.Find("HudRoot/SelectedPiecePanel/PreviewZoomOutButton").GetComponent<Button>();
            zoomInButton.onClick.Invoke();

            Assert.AreNotEqual(beforeRotation.eulerAngles.y, input.TargetRotationForTests.eulerAngles.y);
            Assert.Less(input.CameraDistanceForTests, beforeDistance);

            float zoomedInDistance = input.CameraDistanceForTests;
            zoomOutButton.onClick.Invoke();

            Assert.Greater(input.CameraDistanceForTests, zoomedInDistance);
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

            AssertSelectedPieceHud(boardView, controller, hud, ChessPieceKind.Rook, "a1", "Alex Fenner", "Torre", "Matricula 0403240");
            AssertSelectedPieceHud(boardView, controller, hud, ChessPieceKind.Knight, "b1", "Gustavo Cornalewski", "Cavalo", "Matricula 0407923");
            AssertSelectedPieceHud(boardView, controller, hud, ChessPieceKind.Bishop, "c1", "Rafael Scharer", "Bispo", "Matricula 040603");
            AssertSelectedPieceHud(boardView, controller, hud, ChessPieceKind.Queen, "d1", "MARTA ROSECLER BEZ", "Rainha", "Professora de Ciencias da Computacao - Universidade Feevale");
            AssertSelectedPieceHud(boardView, controller, hud, ChessPieceKind.King, "e1", "RICARDO FERREIRA DE OLIVEIRA", "Rei", "Professor de Ciencias da Computacao - Universidade Feevale");
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
        string expectedKindName,
        string expectedRegistry)
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
        StringAssert.Contains($"Nome: {expectedModelName}", FindText(hud.transform, "HudRoot/SelectedPiecePanel/SelectedPieceProfileText").text);
        StringAssert.Contains(expectedRegistry, FindText(hud.transform, "HudRoot/SelectedPiecePanel/SelectedPieceProfileText").text);
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
