#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class DesktopRoomViewTests
{
    private CameraController view;
    private ChessGameController game;
    private Mouse mouse;
    private Keyboard keyboard;
    private InputSettings originalInputSettings, testInputSettings;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        originalInputSettings = InputSystem.settings;
        testInputSettings = Object.Instantiate(originalInputSettings);
        InputSystem.settings = testInputSettings;
        testInputSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        testInputSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/Main.unity", new LoadSceneParameters(LoadSceneMode.Single));
        yield return null;
        yield return null;
        game = Object.FindFirstObjectByType<ChessGameController>();
        view = Object.FindFirstObjectByType<CameraController>();
        GameObject.Find("LocalModeButton").GetComponent<Button>().onClick.Invoke();
        GameObject.Find("StartPlayButton").GetComponent<Button>().onClick.Invoke();
        view.SetPerspective(ChessSide.White, true);
        mouse = InputSystem.AddDevice<Mouse>();
        keyboard = InputSystem.AddDevice<Keyboard>();
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        if (view != null) view.ReturnToBoard();
        if (mouse != null) InputSystem.RemoveDevice(mouse);
        if (keyboard != null) InputSystem.RemoveDevice(keyboard);
        InputSystem.settings = originalInputSettings;
        Object.DestroyImmediate(testInputSettings);
        Scene previous = SceneManager.GetActiveScene();
        SceneManager.SetActiveScene(SceneManager.CreateScene("Empty after room view test"));
        yield return SceneManager.UnloadSceneAsync(previous);
    }

    [UnityTest]
    public IEnumerator VisibleButtonLooksAroundAndReturnsToPlayableBoard()
    {
        Vector3 position = view.transform.position;
        Quaternion rotation = view.transform.rotation;
        float fov = Camera.main.fieldOfView;
        var button = GameObject.Find("RoomViewButton").GetComponent<Button>();
        button.onClick.Invoke();
        yield return null;
        Assert.That(view.IsLookingAround, Is.True);
        Assert.That(view.transform.position, Is.EqualTo(position));
        Assert.That(Mathf.Abs(view.transform.forward.y), Is.LessThan(.01f));
        Assert.That(Camera.main.fieldOfView, Is.GreaterThan(fov));
        Assert.That(button.GetComponentInChildren<Text>().text, Does.Contain("Voltar"));
        button.onClick.Invoke();
        yield return null;
        Assert.That(view.IsLookingAround, Is.False);
        Assert.That(Quaternion.Angle(view.transform.rotation, rotation), Is.LessThan(.01f));
        Assert.That(Camera.main.fieldOfView, Is.EqualTo(fov));
        var board = Object.FindFirstObjectByType<BoardView>();
        game.SelectPiece(board.Pieces.Single(p => p.Square.Equals(BoardSquare.FromAlgebraic("e2"))));
        game.SelectDestination(BoardSquare.FromAlgebraic("e4"));
        yield return new WaitForSeconds(.8f);
        Assert.That(game.MoveHistory.Count, Is.EqualTo(1));
    }

    [UnityTest]
    public IEnumerator RightDragRotatesInPlaceWithoutSelectingAndRRestoresView()
    {
        Vector3 position = view.transform.position;
        Quaternion rotation = view.transform.rotation;
        Vector2 centre = Camera.main.pixelRect.center;
        InputSystem.QueueStateEvent(mouse, new MouseState { position = centre }.WithButton(MouseButton.Right).WithButton(MouseButton.Left));
        yield return null;
        Assert.That(view.IsLookingAround, Is.True);
        Assert.That(game.SelectedPiece, Is.Null);
        InputSystem.QueueStateEvent(mouse, new MouseState { position = centre, delta = new Vector2(800, 1300) }.WithButton(MouseButton.Right));
        yield return null;
        Assert.That(Vector3.Distance(view.transform.position, position), Is.LessThan(.001f));
        Assert.That(Quaternion.Angle(rotation, view.transform.rotation), Is.GreaterThan(80));
        Assert.That(Mathf.DeltaAngle(0, view.transform.eulerAngles.x), Is.GreaterThanOrEqualTo(-75.01f));
        InputSystem.QueueStateEvent(mouse, new MouseState { position = centre });
        yield return null;
        yield return null;
        Assert.That(view.BlocksBoardPointer, Is.False);
        Assert.That(Cursor.lockState, Is.Not.EqualTo(CursorLockMode.Locked));
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.R));
        yield return null;
        Assert.That(view.IsLookingAround, Is.False);
        Assert.That(Quaternion.Angle(rotation, view.transform.rotation), Is.LessThan(.01f));
    }

    [UnityTest]
    public IEnumerator TurnChangeDoesNotInterruptLookingButResetUsesTheNewSide()
    {
        view.LookAround();
        Vector3 position = view.transform.position;
        Quaternion rotation = view.transform.rotation;
        view.SetPerspective(ChessSide.Black, false);
        yield return new WaitForSeconds(.3f);
        Assert.That(view.CurrentPerspective, Is.EqualTo(ChessSide.Black));
        Assert.That(view.transform.position, Is.EqualTo(position));
        Assert.That(Quaternion.Angle(view.transform.rotation, rotation), Is.LessThan(.01f));
        view.ReturnToBoard();
        Assert.That(view.transform.position.z, Is.GreaterThan(0));
        Assert.That(view.IsLookingAround, Is.False);
    }

    [UnityTest]
    public IEnumerator RightClickOnHudDoesNotCapturePointerAndFocusLossReleasesIt()
    {
        var rect = GameObject.Find("RoomViewButton").GetComponent<RectTransform>();
        Vector2 point = RectTransformUtility.WorldToScreenPoint(null, rect.position);
        InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
        yield return null;
        yield return null;
        InputSystem.QueueStateEvent(mouse, new MouseState { position = point }.WithButton(MouseButton.Right));
        yield return null;
        Assert.That(view.IsLookingAround, Is.False);
        InputSystem.QueueStateEvent(mouse, new MouseState { position = Camera.main.pixelRect.center });
        yield return null;
        yield return null;
        InputSystem.QueueStateEvent(mouse, new MouseState { position = Camera.main.pixelRect.center }.WithButton(MouseButton.Right));
        yield return null;
        Assert.That(view.IsLookingAround, Is.True);
        view.SendMessage("OnApplicationFocus", false);
        Assert.That(Cursor.lockState, Is.Not.EqualTo(CursorLockMode.Locked));
        view.enabled = false;
        Assert.That(view.IsLookingAround, Is.False);
    }

    [UnityTest]
    public IEnumerator OrbitAndZoomStillWorkAfterReturningFromTheRoom()
    {
        view.LookAround();
        view.ReturnToBoard();
        Vector3 initial = view.transform.position;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E));
        yield return null;
        yield return null;
        Assert.That(Vector3.Distance(view.transform.position, initial), Is.GreaterThan(.01f));
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        InputSystem.QueueStateEvent(mouse, new MouseState { position = Camera.main.pixelRect.center });
        yield return null;
        yield return null;
        float distance = view.transform.position.magnitude;
        InputSystem.QueueStateEvent(mouse, new MouseState { position = Camera.main.pixelRect.center, scroll = new Vector2(0, 120) });
        yield return null;
        yield return null;
        Assert.That(view.transform.position.magnitude, Is.LessThan(distance));
        Assert.That(view.IsLookingAround, Is.False);
    }

    [UnityTest]
    public IEnumerator HeadsetIgnoresDesktopLookAndHidesDesktopControls()
    {
        InputDevice headset = InputSystem.AddDevice("XRHMD");
        try
        {
            Vector3 position = view.transform.position;
            Quaternion rotation = view.transform.rotation;
            view.LookAround();
            view.SetPerspective(ChessSide.Black, true);
            yield return null;
            Assert.That(view.IsLookingAround, Is.False);
            Assert.That(view.transform.position, Is.EqualTo(position));
            Assert.That(Quaternion.Angle(view.transform.rotation, rotation), Is.LessThan(.01f));
            Assert.That(GameObject.Find("RoomViewPanel"), Is.Null);
        }
        finally { InputSystem.RemoveDevice(headset); }
    }
}
#endif
