#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class MenuPresentationTests
{
    private GameHud hud;
    private ChessGameController controller;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/Main.unity", new LoadSceneParameters(LoadSceneMode.Single));
        yield return null;
        yield return null;
        hud = Object.FindFirstObjectByType<GameHud>();
        controller = Object.FindFirstObjectByType<ChessGameController>();
        Canvas.ForceUpdateCanvases();
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Scene previous = SceneManager.GetActiveScene();
        SceneManager.SetActiveScene(SceneManager.CreateScene("Empty after menu test"));
        yield return SceneManager.UnloadSceneAsync(previous);
    }

    [UnityTest]
    public IEnumerator DifficultyChoicesStartTheSelectedLevelAndKeepItOnReturn()
    {
        string[] names = { "BeginnerDifficultyButton", "IntermediateDifficultyButton", "HardDifficultyButton" };
        for (int i = 0; i < names.Length; i++)
        {
            yield return Click(names[i]);
            yield return Click("WhiteSideButton");
            yield return Click("StartPlayButton");
            Assert.That(controller.Difficulty, Is.EqualTo((ComputerDifficulty)i));
            Assert.That(controller.HumanSide, Is.EqualTo(ChessSide.White));
            Assert.That(controller.IsAgainstComputer, Is.True);
            Assert.That(controller.IsInputBlocked, Is.False);
            yield return Click("MenuButton");
            Assert.That(GameObject.Find(names[i]).transform.Find("SelectedMarker").GetComponent<Image>().enabled, Is.True);
            Assert.That(controller.IsMenuOpen, Is.True);
        }
        yield return Click("BlackSideButton");
        yield return Click("LocalModeButton");
        Assert.That(GameObject.Find("DifficultyOptions"), Is.Null);
        Assert.That(GameObject.Find("SideOptions"), Is.Null);
        yield return Click("StartPlayButton");
        Assert.That(controller.IsAgainstComputer, Is.False);
        Assert.That(controller.CurrentTurn, Is.EqualTo(ChessSide.White));
        yield return Click("MenuButton");
        yield return Click("ComputerModeButton");
        Assert.That(GameObject.Find("BlackSideButton").transform.Find("SelectedMarker").GetComponent<Image>().enabled, Is.True);
        yield return null;
    }

    [UnityTest]
    public IEnumerator KeyboardCanConfigureAndStartALocalGame()
    {
        EventSystem events = EventSystem.current;
        Assert.That(events.currentSelectedGameObject.name, Is.EqualTo("ComputerModeButton"));
        var move = new AxisEventData(events) { moveDir = MoveDirection.Right, moveVector = Vector2.right };
        ExecuteEvents.Execute(events.currentSelectedGameObject, move, ExecuteEvents.moveHandler);
        Assert.That(events.currentSelectedGameObject.name, Is.EqualTo("LocalModeButton"));
        ExecuteEvents.Execute(events.currentSelectedGameObject, new BaseEventData(events), ExecuteEvents.submitHandler);
        yield return null;
        move = new AxisEventData(events) { moveDir = MoveDirection.Down, moveVector = Vector2.down };
        ExecuteEvents.Execute(events.currentSelectedGameObject, move, ExecuteEvents.moveHandler);
        Assert.That(events.currentSelectedGameObject.name, Is.EqualTo("StartPlayButton"));
        yield return null;
        Assert.That(events.currentSelectedGameObject.transform.Find("FocusRing").gameObject.activeSelf, Is.True);
        ExecuteEvents.Execute(events.currentSelectedGameObject, new BaseEventData(events), ExecuteEvents.submitHandler);
        yield return null;
        Assert.That(controller.IsMenuOpen, Is.False);
        Assert.That(controller.IsAgainstComputer, Is.False);
        Assert.That(events.currentSelectedGameObject.name, Is.EqualTo("NewGameButton"));
        yield return Click("MenuButton");
        Assert.That(events.currentSelectedGameObject.name, Is.EqualTo("ComputerModeButton"));
    }

    [UnityTest]
    public IEnumerator HelpBlocksUnderlyingButtonsAndReturnsToTheConfiguration()
    {
        yield return Click("HardDifficultyButton");
        yield return Click("StartHowToPlayButton");
        Assert.That(GameObject.Find("StartPlayButton").GetComponent<Button>().IsInteractable(), Is.False);
        Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("CloseHelpButton"));
        yield return Click("CloseHelpButton");
        Assert.That(GameObject.Find("StartPlayButton").GetComponent<Button>().IsInteractable(), Is.True);
        Assert.That(GameObject.Find("HardDifficultyButton").transform.Find("SelectedMarker").GetComponent<Image>().enabled, Is.True);
        yield return Click("StartPlayButton");
        yield return Click("HowToPlayButton");
        Assert.That(GameObject.Find("NewGameButton").GetComponent<Button>().IsInteractable(), Is.False);
        yield return Click("CloseHelpButton");
        Assert.That(GameObject.Find("NewGameButton").GetComponent<Button>().IsInteractable(), Is.True);
        yield return null;
    }

    [UnityTest]
    public IEnumerator ProfessorFocusFollowsSideWithoutChangingMatchOrLogoProportions()
    {
        var signature = GameObject.Find("FeevaleSignature").GetComponent<RawImage>();
        float originalAspect = 950f / 369f;
        Assert.That((float)signature.texture.width / signature.texture.height, Is.EqualTo(originalAspect).Within(0.002f), "Logo import changed its original aspect ratio");
        Assert.That(signature.rectTransform.rect.width / signature.rectTransform.rect.height, Is.EqualTo(originalAspect).Within(0.002f));
        var panel = (RectTransform)GameObject.Find("StartCard").transform;
        Vector3 visibleLogoCenter = signature.rectTransform.TransformPoint(new Vector3(
            signature.rectTransform.rect.xMin + signature.rectTransform.rect.width * (473.5f / 950f), 0, 0));
        Vector3 panelCenter = panel.TransformPoint(panel.rect.center);
        Assert.That(hud.transform.InverseTransformPoint(visibleLogoCenter).x,
            Is.EqualTo(hud.transform.InverseTransformPoint(panelCenter).x).Within(1), "Visible logo must be centered above configuration panel");
        foreach (var surface in hud.GetComponentsInChildren<MenuSurface>())
        {
            var mesh = surface.canvasRenderer.GetMesh();
            Assert.That(mesh, Is.Not.Null, "Missing menu mesh: " + surface.name);
            Assert.That(mesh.vertexCount, Is.GreaterThan(0), "Missing rendered menu surface: " + surface.name);
        }
        Assert.That(GameObject.Find("ComputerModeButton").transform.Find("SelectedMarker").GetComponent<Image>().sprite,
            Is.Not.Null, "Selection check must be a bundled runtime icon");
        var white = GameObject.Find("WhiteProfessor").transform;
        var black = GameObject.Find("BlackProfessor").transform;
        Assert.That(white.Find("Menu_Queen_Marta"), Is.Not.Null);
        Assert.That(black.Find("Menu_King_Ricardo_Carioca"), Is.Not.Null);
        Assert.That(white.localScale.x, Is.GreaterThan(black.localScale.x));
        yield return Click("BlackSideButton");
        yield return new WaitForSecondsRealtime(0.6f);
        Assert.That(black.localScale.x, Is.GreaterThan(white.localScale.x));
        Assert.That(black.localPosition.z, Is.LessThan(white.localPosition.z));
        Assert.That(GameObject.Find("CastName").GetComponent<Text>().text, Is.EqualTo("Professor Ricardo"));
        Assert.That(controller.IsMenuOpen, Is.True);
        yield return Click("WhiteSideButton");
        yield return new WaitForSecondsRealtime(0.6f);
        Assert.That(white.localScale.x, Is.GreaterThan(black.localScale.x));
        yield return Click("LocalModeButton");
        yield return new WaitForSecondsRealtime(0.8f);
        Assert.That(white.localScale.x, Is.EqualTo(black.localScale.x).Within(0.002f));
    }

    [UnityTest]
    public IEnumerator MenuFitsDesktopAndWorldCanvasSizesWithoutClippedLabels()
    {
        var canvas = hud.GetComponent<Canvas>();
        var scaler = hud.GetComponent<CanvasScaler>();
        scaler.enabled = false;
        canvas.renderMode = RenderMode.WorldSpace;
        var root = (RectTransform)canvas.transform;
        foreach (Vector2 size in new[] { new Vector2(1920, 1080), new Vector2(1280, 800), new Vector2(1024, 768), new Vector2(2560, 1080) })
        {
            root.sizeDelta = size;
            Canvas.ForceUpdateCanvases();
            hud.RefreshInterface();
            GameObject.Find("IntermediateDifficultyButton").GetComponent<Button>().onClick.Invoke();
            Canvas.ForceUpdateCanvases();
            var content = (RectTransform)GameObject.Find("MenuContent").transform;
            var corners = new Vector3[4];
            content.GetWorldCorners(corners);
            foreach (Vector3 corner in corners)
            {
                Vector2 local = root.InverseTransformPoint(corner);
                Assert.That(root.rect.Contains(local), Is.True, $"Menu outside canvas {size}: {local}");
            }
            foreach (var label in content.GetComponentsInChildren<Text>())
            {
                if (string.IsNullOrEmpty(label.text) || !label.enabled) continue;
                Assert.That(label.preferredHeight, Is.LessThanOrEqualTo(label.rectTransform.rect.height + 1), $"Clipped label {label.name}: {label.text}");
            }
        }
        yield return null;
    }

    [UnityTest]
    public IEnumerator CheckmateShowsTheResultWithRestartReviewAndMenu()
    {
        yield return Click("LocalModeButton");
        yield return Click("StartPlayButton");
        yield return PlayMoves("f2f3", "e7e5", "g2g4", "d8h4");
        Assert.That(GameObject.Find("GameOverPanel"), Is.Null, "The final move must land before the result covers it.");
        yield return WaitForResult();
        Assert.That(Label("GameOverKicker"), Is.EqualTo("XEQUE-MATE"));
        Assert.That(Label("GameOverTitle"), Is.EqualTo("Pretas vencem"));
        Assert.That(Label("GameOverDetails"), Is.EqualTo("4 lances  /  Lance final: Pretas d8-h4#"));
        Assert.That(Label("TurnText"), Is.EqualTo("Pretas vencem"));
        Assert.That(Label("StatusText"), Is.EqualTo("Xeque-mate. Partida encerrada."));
        Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("PlayAgainButton"));
        Assert.That(GameObject.Find("NewGameButton").GetComponent<Button>().IsInteractable(), Is.False);

        yield return Click("ReviewBoardButton");
        Assert.That(GameObject.Find("GameOverPanel"), Is.Null);
        Assert.That(ButtonLabel("CancelButton"), Is.EqualTo("Resultado"));
        yield return Click("CancelButton");
        Assert.That(GameObject.Find("GameOverPanel"), Is.Not.Null);

        yield return Click("PlayAgainButton");
        Assert.That(controller.IsGameOver, Is.False);
        Assert.That(controller.IsAgainstComputer, Is.False);
        Assert.That(controller.MoveHistory.Count, Is.Zero);
        Assert.That(GameObject.Find("GameOverPanel"), Is.Null);
        Assert.That(ButtonLabel("CancelButton"), Is.EqualTo("Cancelar"));

        yield return PlayMoves("f2f3", "e7e5", "g2g4", "d8h4");
        yield return WaitForResult();
        yield return Click("GameOverMenuButton");
        Assert.That(controller.IsMenuOpen, Is.True);
        Assert.That(GameObject.Find("GameOverPanel"), Is.Null);
        Assert.That(GameObject.Find("StartPlayButton"), Is.Not.Null);
    }

    [UnityTest]
    public IEnumerator BeatingTheComputerCongratulatesThePlayer()
    {
        controller.SetMoveChooserFactory(() => new ScriptedMoveChooser("f7f6", "g7g5"));
        yield return Click("ComputerModeButton");
        yield return Click("WhiteSideButton");
        yield return Click("IntermediateDifficultyButton");
        yield return Click("StartPlayButton");
        yield return PlayMoves("e2e4", "d2d4", "d1h5");
        yield return WaitForResult();
        Assert.That(controller.Winner, Is.EqualTo(ChessSide.White));
        Assert.That(Label("GameOverTitle"), Is.EqualTo("Você venceu!"));
        Assert.That(Label("GameOverMessage"), Does.Contain("Intermediário"));
        Assert.That(GameObject.Find("GameOverKicker").GetComponent<Text>().color, Is.EqualTo(GameObject.Find("TurnText").GetComponent<Text>().color));
    }

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

    // Plays through the controller, waiting out animations and computer replies between moves.
    private IEnumerator PlayMoves(params string[] moves)
    {
        var board = Object.FindFirstObjectByType<BoardView>();
        foreach (string uci in moves)
        {
            yield return WaitFor(() => !controller.IsInputBlocked);
            Assert.That(ChessMove.TryParseUci(uci, out ChessMove move), Is.True, uci);
            PieceView piece = null;
            foreach (PieceView candidate in board.Pieces)
                if (candidate.Square.Equals(move.From)) piece = candidate;
            Assert.That(piece, Is.Not.Null, uci);
            controller.SelectPiece(piece);
            controller.SelectDestination(move.To);
        }
        yield return WaitFor(() => !controller.IsInputBlocked);
    }

    private IEnumerator WaitForResult()
    {
        yield return WaitFor(() => GameObject.Find("GameOverPanel") != null);
        // Newly enabled Graphics enter the Canvas raycast registry on the next frame.
        yield return null;
    }

    private static IEnumerator WaitFor(System.Func<bool> condition)
    {
        float deadline = Time.realtimeSinceStartup + 5f;
        while (!condition() && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(condition(), Is.True, "The match did not reach its expected state.");
    }

    private static string Label(string name) => GameObject.Find(name).GetComponent<Text>().text;

    private static string ButtonLabel(string name) => GameObject.Find(name).GetComponentInChildren<Text>().text;

    private IEnumerator Click(string name)
    {
        Canvas.ForceUpdateCanvases();
        var target = GameObject.Find(name);
        Assert.That(target, Is.Not.Null, name);
        Assert.That(target.GetComponent<Button>().IsInteractable(), Is.True, name);
        var rect = (RectTransform)target.transform;
        var data = new PointerEventData(EventSystem.current)
        {
            position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center)),
            button = PointerEventData.InputButton.Left
        };
        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(data, hits);
        Assert.That(hits.Count, Is.GreaterThan(0), name);
        Assert.That(hits[0].gameObject, Is.EqualTo(target), $"Blocked click on {name}");
        ExecuteEvents.Execute(target, data, ExecuteEvents.pointerClickHandler);
        // Newly enabled Graphics enter the Canvas raycast registry on the next frame.
        yield return null;
    }
}
#endif
