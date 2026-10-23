#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class WebLayoutTests
{
    private GameHud hud;
    private ChessGameController game;
    private const string PerformanceKey = "ChessCgi.PerformanceMode";
    private bool hadPerformancePreference;
    private int previousPerformancePreference;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        hadPerformancePreference = PlayerPrefs.HasKey(PerformanceKey);
        previousPerformancePreference = PlayerPrefs.GetInt(PerformanceKey);
        WebTouchInput.EditorPreviewEnabled = true;
        WebTouchInput.EditorViewportSize = new Vector2(360, 640);
        WebTouchInput.EditorSafeArea = new Rect(0, 20, 360, 600);
        EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/Main.unity", new LoadSceneParameters(LoadSceneMode.Single));
        yield return null;
        yield return null;
        hud = Object.FindFirstObjectByType<GameHud>();
        game = Object.FindFirstObjectByType<ChessGameController>();
        game.SetPerformanceMode(false);
        yield return null;
        Assert.That(game.PerformanceMode, Is.False, "Web layout must be verified with the real characters.");
        Canvas.ForceUpdateCanvases();
        hud.RefreshInterface();
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        WebTouchInput.EditorPreviewEnabled = false;
        WebTouchInput.EditorViewportSize = Vector2.zero;
        WebTouchInput.EditorSafeArea = Rect.zero;
        var previous = SceneManager.GetActiveScene();
        SceneManager.SetActiveScene(SceneManager.CreateScene("Empty after Web layout test"));
        yield return SceneManager.UnloadSceneAsync(previous);
        if (hadPerformancePreference) PlayerPrefs.SetInt(PerformanceKey, previousPerformancePreference);
        else PlayerPrefs.DeleteKey(PerformanceKey);
    }

    [UnityTest]
    public IEnumerator PortraitLandscapeAndTabletKeepControlsReadableAndDialogsInsideSafeArea()
    {
        foreach (Vector2 size in new[] { new Vector2(360, 640), new Vector2(640, 360), new Vector2(768, 1024), new Vector2(1024, 768) })
        {
            WebTouchInput.EditorViewportSize = size;
            WebTouchInput.EditorSafeArea = new Rect(0, 16, size.x, size.y - 32);
            hud.RefreshInterface();
            Canvas.ForceUpdateCanvases();
            hud.RefreshInterface();
            Canvas.ForceUpdateCanvases();
            var scroll = hud.GetComponentInChildren<UnityEngine.UI.ScrollRect>();
            Assert.That(scroll, Is.Not.Null);
            Assert.That(scroll.viewport.GetComponent<UnityEngine.UI.RectMask2D>(), Is.Not.Null);
            Assert.That(scroll.content.rect.width, Is.LessThanOrEqualTo(scroll.viewport.rect.width));
            foreach (var button in scroll.content.GetComponentsInChildren<UnityEngine.UI.Button>())
            {
                Assert.That(button.GetComponent<RectTransform>().rect.height, Is.GreaterThanOrEqualTo(44), button.name);
                foreach (var label in button.GetComponentsInChildren<UnityEngine.UI.Text>())
                    Assert.That(label.preferredHeight, Is.LessThanOrEqualTo(label.rectTransform.rect.height + 1), label.text);
            }
            AssertInsideRoot("PromotionCard");
            AssertInsideRoot("ComputerErrorCard");
            AssertInsideRoot("GameOverCard");
            AssertInsideRoot("HelpCard");
            Object.FindFirstObjectByType<CameraController>().SetPerspective(ChessSide.White, true);
            AssertBoardFitsFreeViewport();
            yield return null;
        }
    }

    [UnityTest]
    public IEnumerator HistoryAndCharacterPreviewOpenOnDemandAndReleaseTheCloneWhenClosed()
    {
        Assert.That(GameObject.Find("SelectedPiecePreviewCamera"), Is.Null, "Web must not allocate its high-resolution preview at startup.");
        Click("LocalModeButton");
        Click("StartPlayButton");
        yield return null;
        Assert.That(GameObject.Find("SelectedPiecePreviewCamera"), Is.Null);
        PieceView piece = Object.FindFirstObjectByType<BoardView>().Pieces.First(p => p.Square.Equals(BoardSquare.FromAlgebraic("e2")));
        game.SelectPiece(piece);
        hud.RefreshInterface();
        Assert.That(GameObject.Find("SelectedPiecePanel"), Is.Null);
        Assert.That(GameObject.Find("SelectedPiecePreviewClone"), Is.Null);
        Click("WebPreviewButton");
        yield return null;
        Assert.That(GameObject.Find("SelectedPiecePreviewClone"), Is.Not.Null);
        AssertInsideRoot("SelectedPiecePanel");
        Click("ClosePreviewButton");
        yield return null;
        Assert.That(GameObject.Find("SelectedPiecePreviewClone"), Is.Null);
        Assert.That(game.SelectedPiece, Is.SameAs(piece));
        Click("WebHistoryButton");
        Assert.That(GameObject.Find("MoveHistoryPanel"), Is.Not.Null);
        Assert.That(GameObject.Find("NewGameButton").GetComponent<UnityEngine.UI.Button>().IsInteractable(), Is.False);
        Click("CloseHistoryButton");
        Assert.That(GameObject.Find("NewGameButton").GetComponent<UnityEngine.UI.Button>().IsInteractable(), Is.True);
        Assert.That(game.MoveHistory.Count, Is.Zero);
    }

    private void AssertInsideRoot(string name)
    {
        RectTransform root = hud.GetComponentsInChildren<RectTransform>(true).Single(r => r.name == "HudRoot");
        RectTransform target = hud.GetComponentsInChildren<RectTransform>(true).Single(r => r.name == name);
        var corners = new Vector3[4];
        target.GetWorldCorners(corners);
        foreach (Vector3 corner in corners)
        {
            Vector2 local = root.InverseTransformPoint(corner);
            Assert.That(local.x, Is.InRange(root.rect.xMin - 1, root.rect.xMax + 1), name);
            Assert.That(local.y, Is.InRange(root.rect.yMin - 1, root.rect.yMax + 1), name);
        }
    }

    private static void AssertBoardFitsFreeViewport()
    {
        var board = Object.FindFirstObjectByType<BoardView>();
        Rect free = WebTouchInput.BoardViewport;
        void Check(Vector3 world)
        {
            Vector3 point = Camera.main.WorldToViewportPoint(world);
            Assert.That(point.z, Is.GreaterThan(0));
            Assert.That(point.x, Is.InRange(free.xMin, free.xMax), "Board/character is behind a side control or outside the viewport.");
            Assert.That(point.y, Is.InRange(free.yMin, free.yMax), "Board/character is behind the top/bottom HUD.");
        }
        for (int i = 0; i < 8; i++)
        {
            Vector3 sign = new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1);
            Check(board.transform.TransformPoint(board.FrameLocalBounds.center + Vector3.Scale(board.FrameLocalBounds.extents, sign)));
        }
        foreach (PieceView piece in board.Pieces)
        {
            Assert.That(piece.GetComponentInChildren<CustomPieceAppearance>(), Is.Not.Null, "Camera test must frame custom characters.");
            foreach (Renderer renderer in piece.GetComponentsInChildren<Renderer>())
            for (int i = 0; i < 8; i++)
            {
                Vector3 sign = new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1);
                Check(renderer.bounds.center + Vector3.Scale(renderer.bounds.extents, sign));
            }
        }
    }

    private static void Click(string name)
    {
        var button = GameObject.Find(name).GetComponent<UnityEngine.UI.Button>();
        Assert.That(button.IsInteractable(), Is.True, name);
        button.onClick.Invoke();
    }
}
#endif
