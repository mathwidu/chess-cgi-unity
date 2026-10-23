using NUnit.Framework;
using UnityEngine;

public sealed class WebTouchGestureTests
{
    private WebTouchGesture gesture;
    [SetUp] public void SetUp() => gesture = new WebTouchGesture();
    private WebTouchPoint Finger(int id, float x, bool released = false, bool ui = false)
        => new WebTouchPoint(id, new Vector2(x, 100), released, false, ui);

    [Test]
    public void TapIsEmittedOnlyAtReleaseAndOnlyOnce()
    {
        gesture.Update(new[] { Finger(1, 100) }, 0, 360);
        Assert.That(gesture.HasTap, Is.False);
        gesture.Update(new[] { Finger(1, 103, true) }, .1f, 360);
        Assert.That(gesture.HasTap, Is.True);
        Assert.That(gesture.TapPosition.x, Is.EqualTo(103));
        gesture.Update(new WebTouchPoint[0], .2f, 360);
        Assert.That(gesture.HasTap, Is.False);
    }

    [Test]
    public void DragReturningToTheStartDoesNotBecomeATap()
    {
        gesture.Update(new[] { Finger(1, 100) }, 0, 360);
        gesture.Update(new[] { Finger(1, 140) }, .1f, 360);
        gesture.Update(new[] { Finger(1, 100, true) }, .2f, 360);
        Assert.That(gesture.HasTap, Is.False);
    }

    [TestCase(true)]
    [TestCase(false)]
    public void TouchStartingOnOrCrossingUiNeverSelectsTheBoard(bool startsOverUi)
    {
        gesture.Update(new[] { Finger(1, 100, ui: startsOverUi) }, 0, 360);
        gesture.Update(new[] { Finger(1, 101, ui: true) }, .1f, 360);
        gesture.Update(new[] { Finger(1, 102, true) }, .2f, 360);
        Assert.That(gesture.HasTap, Is.False);
    }

    [Test]
    public void PinchAndOrbitCannotSelectAfterOneFingerRemains()
    {
        gesture.Update(new[] { Finger(1, 100) }, 0, 360);
        gesture.Update(new[] { Finger(1, 100), Finger(2, 200) }, .05f, 360);
        gesture.Update(new[] { Finger(1, 100), Finger(2, 240) }, .1f, 360);
        Assert.That(gesture.PinchDelta, Is.GreaterThan(0));
        Assert.That(gesture.OrbitDelta, Is.GreaterThan(0));
        gesture.Update(new[] { Finger(1, 100), Finger(2, 240, true) }, .15f, 360);
        gesture.Update(new[] { Finger(1, 100, true) }, .2f, 360);
        Assert.That(gesture.HasTap, Is.False);
        gesture.Update(new[] { Finger(3, 100) }, .3f, 360);
        gesture.Update(new[] { Finger(3, 100, true) }, .4f, 360);
        Assert.That(gesture.HasTap, Is.True, "A fresh gesture must recover after all fingers lift.");
    }

    [Test]
    public void OneFingerOverUiBlocksTheWholeCameraGesture()
    {
        gesture.Update(new[] { Finger(1, 100), Finger(2, 200, ui: true) }, 0, 360);
        gesture.Update(new[] { Finger(1, 80), Finger(2, 260) }, .1f, 360);
        Assert.That(gesture.OrbitDelta, Is.Zero);
        Assert.That(gesture.PinchDelta, Is.Zero);
    }

    [Test]
    public void CancelledTouchAndLongPressAreNotTaps()
    {
        gesture.Update(new[] { Finger(1, 100) }, 0, 360);
        gesture.Update(new[] { new WebTouchPoint(1, new Vector2(100, 100), true, true) }, .1f, 360);
        Assert.That(gesture.HasTap, Is.False);
        gesture.Update(new[] { Finger(2, 100) }, 1, 360);
        gesture.Update(new[] { Finger(2, 100, true) }, 2, 360);
        Assert.That(gesture.HasTap, Is.False);
    }
}

#if UNITY_EDITOR
public sealed class WebTouchBoardTests
{
    private UnityEngine.InputSystem.Touchscreen touchscreen;
    private UnityEngine.InputSystem.Mouse mouse;
    private UnityEngine.InputSystem.InputSettings originalSettings, testSettings;
    private ChessGameController game;

    [UnityEngine.TestTools.UnitySetUp]
    public System.Collections.IEnumerator SetUp()
    {
        WebTouchInput.EditorPreviewEnabled = true;
        WebTouchInput.EditorViewportSize = Vector2.zero;
        originalSettings = UnityEngine.InputSystem.InputSystem.settings;
        testSettings = Object.Instantiate(originalSettings);
        UnityEngine.InputSystem.InputSystem.settings = testSettings;
        testSettings.backgroundBehavior = UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
        testSettings.editorInputBehaviorInPlayMode = UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/Main.unity",
            new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
        yield return null;
        yield return null;
        game = Object.FindFirstObjectByType<ChessGameController>();
        GameObject.Find("LocalModeButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
        GameObject.Find("StartPlayButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
        Object.FindFirstObjectByType<CameraController>().SetPerspective(ChessSide.White, true);
        touchscreen = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Touchscreen>();
        mouse = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Mouse>();
        yield return null;
    }

    [UnityEngine.TestTools.UnityTearDown]
    public System.Collections.IEnumerator TearDown()
    {
        UnityEngine.InputSystem.InputSystem.RemoveDevice(touchscreen);
        UnityEngine.InputSystem.InputSystem.RemoveDevice(mouse);
        UnityEngine.InputSystem.InputSystem.settings = originalSettings;
        Object.DestroyImmediate(testSettings);
        WebTouchInput.EditorPreviewEnabled = false;
        WebTouchInput.EditorViewportSize = Vector2.zero;
        WebTouchInput.EditorSafeArea = Rect.zero;
        var previous = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        UnityEngine.SceneManagement.SceneManager.SetActiveScene(UnityEngine.SceneManagement.SceneManager.CreateScene("Empty after Web touch test"));
        yield return UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(previous);
    }

    [UnityEngine.TestTools.UnityTest]
    public System.Collections.IEnumerator TapSelectsOnReleaseAndEmulatedMouseIsSuppressed()
    {
        PieceView piece = null;
        foreach (PieceView candidate in Object.FindFirstObjectByType<BoardView>().Pieces)
            if (candidate.Square.Equals(BoardSquare.FromAlgebraic("e2"))) piece = candidate;
        Assert.That(piece, Is.Not.Null);
        Vector2 position = Camera.main.WorldToScreenPoint(piece.GetComponent<Collider>().bounds.center);
        Assert.That(WebTouchInput.IsOverUi(position), Is.False, "Test piece must be in the board area.");
        QueueTouch(1, position, UnityEngine.InputSystem.TouchPhase.Began);
        yield return null;
        Assert.That(game.SelectedPiece, Is.Null);
        QueueTouch(1, position, UnityEngine.InputSystem.TouchPhase.Ended);
        yield return null;
        Assert.That(game.SelectedPiece, Is.SameAs(piece));
        game.CancelSelection();
        var state = new UnityEngine.InputSystem.LowLevel.MouseState { position = position }
            .WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Left);
        UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse, state);
        yield return null;
        Assert.That(game.SelectedPiece, Is.Null, "Mouse emulation must not duplicate a touch.");
    }

    [UnityEngine.TestTools.UnityTest]
    public System.Collections.IEnumerator UiTouchAndDragDoNotSelectBoardPieces()
    {
        var button = GameObject.Find("WebHistoryButton").GetComponent<RectTransform>();
        Vector2 uiPosition = RectTransformUtility.WorldToScreenPoint(null, button.TransformPoint(button.rect.center));
        Assert.That(WebTouchInput.IsOverUi(uiPosition), Is.True);
        QueueTouch(2, uiPosition, UnityEngine.InputSystem.TouchPhase.Began);
        yield return null;
        QueueTouch(2, uiPosition + Vector2.up * 50, UnityEngine.InputSystem.TouchPhase.Moved);
        yield return null;
        QueueTouch(2, uiPosition + Vector2.up * 50, UnityEngine.InputSystem.TouchPhase.Ended);
        yield return null;
        Assert.That(game.SelectedPiece, Is.Null);
        Assert.That(game.MoveHistory.Count, Is.Zero);
    }

    private void QueueTouch(int id, Vector2 position, UnityEngine.InputSystem.TouchPhase phase)
    {
        UnityEngine.InputSystem.InputSystem.QueueStateEvent(touchscreen,
            new UnityEngine.InputSystem.LowLevel.TouchState { touchId = id, position = position, phase = phase });
    }
}
#endif
