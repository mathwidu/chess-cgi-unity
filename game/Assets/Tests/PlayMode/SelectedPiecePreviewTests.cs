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

public sealed class SelectedPiecePreviewTests
{
    private ChessGameController game;
    private BoardView board;
    private Mouse mouse;
    private InputSettings originalSettings, testSettings;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        originalSettings = InputSystem.settings;
        testSettings = Object.Instantiate(originalSettings);
        InputSystem.settings = testSettings;
        testSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        testSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/Main.unity", new LoadSceneParameters(LoadSceneMode.Single));
        yield return null;
        yield return null;
        game = Object.FindFirstObjectByType<ChessGameController>();
        board = Object.FindFirstObjectByType<BoardView>();
        Object.FindFirstObjectByType<PieceFactory>().UsePrimitivePieces = false;
        Click("LocalModeButton");
        Click("StartPlayButton");
        Object.FindFirstObjectByType<CameraController>().SetPerspective(ChessSide.White, true);
        game.SelectPiece(board.Pieces.First(p => p.Square.Equals(BoardSquare.FromAlgebraic("f2"))));
        mouse = InputSystem.AddDevice<Mouse>();
        yield return null;
        Canvas.ForceUpdateCanvases();
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        if (mouse != null) InputSystem.RemoveDevice(mouse);
        InputSystem.settings = originalSettings;
        Object.DestroyImmediate(testSettings);
        Scene previous = SceneManager.GetActiveScene();
        SceneManager.SetActiveScene(SceneManager.CreateScene("Empty after preview test"));
        yield return SceneManager.UnloadSceneAsync(previous);
    }

    [UnityTest]
    public IEnumerator VisibleControlsRotateZoomAndRestoreOnlyThePreview()
    {
        var input = Preview;
        Transform clone = GameObject.Find("SelectedPiecePreviewClone").transform;
        Camera previewCamera = GameObject.Find("SelectedPiecePreviewCamera").GetComponent<Camera>();
        Vector3 clonePosition = clone.position, cameraPosition = previewCamera.transform.position;
        Quaternion cloneRotation = clone.rotation, boardRotation = Camera.main.transform.rotation;
        Vector3 piecePosition = game.SelectedPiece.transform.position, boardCameraPosition = Camera.main.transform.position;
        Click("PreviewRotateRightButton");
        Click("PreviewZoomInButton");
        yield return null;
        Assert.That(Quaternion.Angle(cloneRotation, clone.rotation), Is.EqualTo(30f).Within(.1f));
        Assert.That(input.ZoomPercent, Is.EqualTo(120));
        Assert.That(GameObject.Find("PreviewZoomText").GetComponent<UnityEngine.UI.Text>().text, Is.EqualTo("120%"));
        Click("PreviewResetButton");
        yield return null;
        Assert.That(Vector3.Distance(clone.position, clonePosition), Is.LessThan(.001f));
        Assert.That(Quaternion.Angle(clone.rotation, cloneRotation), Is.LessThan(.01f));
        Assert.That(Vector3.Distance(previewCamera.transform.position, cameraPosition), Is.LessThan(.001f));
        Assert.That(input.ZoomPercent, Is.EqualTo(100));
        Assert.That(game.SelectedPiece.transform.position, Is.EqualTo(piecePosition));
        Assert.That(Camera.main.transform.position, Is.EqualTo(boardCameraPosition));
        Assert.That(Quaternion.Angle(Camera.main.transform.rotation, boardRotation), Is.LessThan(.01f));
        Assert.That(game.MoveHistory.Count, Is.Zero);
    }

    [UnityTest]
    public IEnumerator MouseDragAndWheelStayInsideThePreview()
    {
        var rect = Preview.GetComponent<RectTransform>();
        Vector2 centre = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
        Quaternion original = GameObject.Find("SelectedPiecePreviewClone").transform.rotation;
        Camera previewCamera = GameObject.Find("SelectedPiecePreviewCamera").GetComponent<Camera>();
        Quaternion previewRotation = previewCamera.transform.rotation, boardRotation = Camera.main.transform.rotation;
        Vector3 boardPosition = Camera.main.transform.position;
        PieceView piece = game.SelectedPiece;
        InputSystem.QueueStateEvent(mouse, new MouseState { position = centre });
        yield return null;
        yield return null;
        InputSystem.QueueStateEvent(mouse, new MouseState { position = centre }.WithButton(MouseButton.Left));
        yield return null;
        Vector2 delta = new Vector2(60, 30);
        InputSystem.QueueStateEvent(mouse, new MouseState { position = centre + delta, delta = delta }.WithButton(MouseButton.Left));
        yield return null;
        yield return null;
        Assert.That(Preview.IsDragging, Is.True);
        Assert.That(Quaternion.Angle(original, GameObject.Find("SelectedPiecePreviewClone").transform.rotation), Is.GreaterThan(15));
        Assert.That(Quaternion.Angle(previewRotation, previewCamera.transform.rotation), Is.GreaterThan(5));
        InputSystem.QueueStateEvent(mouse, new MouseState { position = centre + delta });
        yield return null;
        Assert.That(Preview.IsDragging, Is.False);
        InputSystem.QueueStateEvent(mouse, new MouseState { position = centre, scroll = new Vector2(0, 120) });
        yield return null;
        yield return null;
        Assert.That(Preview.ZoomPercent, Is.GreaterThan(100));
        Assert.That(game.SelectedPiece, Is.SameAs(piece));
        Assert.That(game.MoveHistory.Count, Is.Zero);
        Assert.That(Camera.main.transform.position, Is.EqualTo(boardPosition));
        Assert.That(Quaternion.Angle(Camera.main.transform.rotation, boardRotation), Is.LessThan(.01f));
        // A secondary click on the preview must not enter room-look mode.
        InputSystem.QueueStateEvent(mouse, new MouseState { position = centre }.WithButton(MouseButton.Right));
        yield return null;
        Assert.That(Object.FindFirstObjectByType<CameraController>().IsLookingAround, Is.False);
    }

    [UnityTest]
    public IEnumerator MaximumZoomLetsThePlayerDragTheHeadBackIntoView()
    {
        game.SelectPiece(board.Pieces.First(p => p.Kind == ChessPieceKind.King && p.Side == ChessSide.White));
        yield return null;
        Preview.ZoomPreview(1000);
        yield return null;
        var clone = GameObject.Find("SelectedPiecePreviewClone").transform;
        var camera = GameObject.Find("SelectedPiecePreviewCamera").GetComponent<Camera>();
        var renderers = clone.GetComponentsInChildren<Renderer>();
        Bounds bounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers) bounds.Encapsulate(renderer.bounds);
        Vector3 head = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
        float before = camera.WorldToViewportPoint(head).y;
        Quaternion rotation = clone.rotation;
        Vector3 boardPosition = Camera.main.transform.position;
        Quaternion boardRotation = Camera.main.transform.rotation;
        int zoom = Preview.ZoomPercent;
        var rect = Preview.GetComponent<RectTransform>();
        Vector2 start = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center + Vector2.up * rect.rect.height * .25f));
        Vector2 end = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center
            + new Vector2(rect.rect.width * .12f, -rect.rect.height * .17f)));
        InputSystem.QueueStateEvent(mouse, new MouseState { position = start });
        yield return null;
        yield return null;
        InputSystem.QueueStateEvent(mouse, new MouseState { position = start }.WithButton(MouseButton.Right));
        yield return null;
        InputSystem.QueueStateEvent(mouse, new MouseState { position = end, delta = end - start }.WithButton(MouseButton.Right));
        yield return null;
        yield return null;
        InputSystem.QueueStateEvent(mouse, new MouseState { position = end });
        yield return null;
        float after = camera.WorldToViewportPoint(head).y;
        TestContext.WriteLine($"Head viewport Y at {zoom}%: before={before:F3}; after dragging down={after:F3}");
        Assert.That(after, Is.LessThan(before - .2f), "Dragging must reposition the enlarged character instead of leaving the head clipped.");
        Assert.That(after, Is.InRange(.1f, .97f), "The top of the head must be recoverable without reducing zoom.");
        Assert.That(camera.WorldToViewportPoint(head).x, Is.GreaterThan(.58f), "Horizontal dragging must also move the character.");
        Assert.That(Preview.ZoomPercent, Is.EqualTo(zoom));
        Assert.That(Quaternion.Angle(rotation, clone.rotation), Is.LessThan(.01f));
        Assert.That(Camera.main.transform.position, Is.EqualTo(boardPosition));
        Assert.That(Quaternion.Angle(Camera.main.transform.rotation, boardRotation), Is.LessThan(.01f));
        Assert.That(Object.FindFirstObjectByType<CameraController>().IsLookingAround, Is.False);
        Assert.That(game.MoveHistory.Count, Is.Zero);
    }

    [UnityTest]
    public IEnumerator VisibleMoveButtonsPreserveZoomAndResetRestoresFraming()
    {
        var camera = GameObject.Find("SelectedPiecePreviewCamera").GetComponent<Camera>();
        var clone = GameObject.Find("SelectedPiecePreviewClone").transform;
        Vector3 originalCamera = camera.transform.position, originalClone = clone.position;
        Quaternion originalRotation = clone.rotation;
        Preview.ZoomPreview(1000);
        yield return null;
        int zoom = Preview.ZoomPercent;
        Vector3 closeCamera = camera.transform.position;
        Click("PreviewMoveDownButton");
        yield return null;
        yield return null;
        Assert.That(Vector3.Dot(camera.transform.position - closeCamera, camera.transform.up), Is.GreaterThan(.05f));
        Assert.That(Preview.ZoomPercent, Is.EqualTo(zoom));
        Assert.That(clone.position, Is.EqualTo(originalClone));
        Assert.That(clone.rotation, Is.EqualTo(originalRotation));
        Click("PreviewMoveUpButton");
        yield return null;
        Assert.That(Vector3.Distance(camera.transform.position, closeCamera), Is.LessThan(.001f));
        Preview.PanPreview(new Vector2(.2f, -.25f));
        // Rotation must still use the character's original pivot after panning.
        for (int i = 0; i < 4; i++) Preview.RotatePreview(90);
        Assert.That(Vector3.Distance(clone.position, originalClone), Is.LessThan(.001f));
        Click("PreviewResetButton");
        yield return null;
        Assert.That(Vector3.Distance(camera.transform.position, originalCamera), Is.LessThan(.001f));
        Assert.That(Vector3.Distance(clone.position, originalClone), Is.LessThan(.001f));
        Assert.That(Quaternion.Angle(clone.rotation, originalRotation), Is.LessThan(.01f));
        Assert.That(Preview.ZoomPercent, Is.EqualTo(100));
        Assert.That(game.MoveHistory.Count, Is.Zero);
    }

    [UnityTest]
    public IEnumerator ZoomLimitsDisableOnlyTheMatchingButtonAndResetRecovers()
    {
        Preview.ZoomPreview(1000);
        yield return null;
        Assert.That(Preview.ZoomPercent, Is.InRange(180, 183));
        Assert.That(Button("PreviewZoomInButton").interactable, Is.False);
        Assert.That(Button("PreviewZoomOutButton").interactable, Is.True);
        Preview.ZoomPreview(-1000);
        yield return null;
        Assert.That(Preview.ZoomPercent, Is.EqualTo(67));
        Assert.That(Button("PreviewZoomOutButton").interactable, Is.False);
        Assert.That(Button("PreviewZoomInButton").interactable, Is.True);
        Click("PreviewResetButton");
        yield return null;
        Assert.That(Button("PreviewZoomInButton").interactable && Button("PreviewZoomOutButton").interactable, Is.True);
        Assert.That(Preview.ZoomPercent, Is.EqualTo(100));
    }

    [UnityTest]
    public IEnumerator EveryCharacterFitsAtRestoredAspectForBothTeams()
    {
        var image = GameObject.Find("SelectedPiecePreview").GetComponent<UnityEngine.UI.RawImage>();
        var camera = GameObject.Find("SelectedPiecePreviewCamera").GetComponent<Camera>();
        Assert.That(camera.aspect, Is.EqualTo(image.rectTransform.rect.width / image.rectTransform.rect.height).Within(.001f));
        Assert.That((float)image.texture.width / image.texture.height, Is.EqualTo(camera.aspect).Within(.001f));
        foreach (ChessSide side in new[] { ChessSide.White, ChessSide.Black })
        {
            foreach (PieceView piece in board.Pieces.Where(p => p.Side == side).GroupBy(p => p.Kind).Select(g => g.First()).ToArray())
            {
                game.SelectPiece(piece);
                yield return null;
                Assert.That(Preview.ZoomPercent, Is.EqualTo(100));
                var renderers = GameObject.Find("SelectedPiecePreviewClone").GetComponentsInChildren<Renderer>();
                Bounds bounds = renderers[0].bounds;
                foreach (Renderer renderer in renderers) bounds.Encapsulate(renderer.bounds);
                for (int corner = 0; corner < 8; corner++)
                {
                    Vector3 sign = new Vector3((corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1);
                    Vector3 viewport = camera.WorldToViewportPoint(bounds.center + Vector3.Scale(bounds.extents, sign));
                    Assert.That(viewport.x, Is.InRange(.01f, .99f), piece.Kind + " horizontal framing");
                    Assert.That(viewport.y, Is.InRange(.01f, .99f), piece.Kind + " vertical framing");
                    Assert.That(viewport.z, Is.GreaterThan(camera.nearClipPlane));
                }
                Preview.RotatePreview(90);
                Preview.ZoomPreview(1);
            }
            if (side == ChessSide.White)
            {
                game.SelectPiece(board.Pieces.First(p => p.Square.Equals(BoardSquare.FromAlgebraic("e2"))));
                game.SelectDestination(BoardSquare.FromAlgebraic("e4"));
                yield return new WaitForSeconds(.8f);
            }
        }
    }

    [UnityTest]
    public IEnumerator MaximumZoomKeepsTheTopVisibleForEveryCharacterAndTeam()
    {
        var camera = GameObject.Find("SelectedPiecePreviewCamera").GetComponent<Camera>();
        foreach (ChessSide side in new[] { ChessSide.White, ChessSide.Black })
        {
            foreach (PieceView piece in board.Pieces.Where(p => p.Side == side).GroupBy(p => p.Kind).Select(g => g.First()).ToArray())
            {
                game.SelectPiece(piece);
                yield return null;
                Assert.That(Preview.ZoomPercent, Is.EqualTo(100));
                Preview.ZoomPreview(1000);
                yield return null;
                foreach (Renderer renderer in GameObject.Find("SelectedPiecePreviewClone").GetComponentsInChildren<Renderer>())
                {
                    Bounds bounds = renderer.bounds;
                    for (int corner = 0; corner < 4; corner++)
                    {
                        Vector3 top = new Vector3((corner & 1) == 0 ? bounds.min.x : bounds.max.x, bounds.max.y,
                            (corner & 2) == 0 ? bounds.min.z : bounds.max.z);
                        Vector3 viewport = camera.WorldToViewportPoint(top);
                        Assert.That(viewport.y, Is.LessThanOrEqualTo(.95f), $"{side} {piece.Kind}: head or accessory clips at maximum zoom");
                        Assert.That(viewport.z, Is.GreaterThan(camera.nearClipPlane));
                    }
                }
                Preview.PanPreview(new Vector2(.1f, -.2f));
                Preview.RotatePreview(90);
            }
            if (side == ChessSide.White)
            {
                game.SelectPiece(board.Pieces.First(p => p.Square.Equals(BoardSquare.FromAlgebraic("e2"))));
                game.SelectDestination(BoardSquare.FromAlgebraic("e4"));
                yield return new WaitForSeconds(.8f);
            }
        }
    }

    [UnityTest]
    public IEnumerator RebuildingTheHudKeepsSelectionAndCreatesWorkingControls()
    {
        PieceView piece = game.SelectedPiece;
        Preview.RotatePreview(90);
        Object.FindFirstObjectByType<GameHud>().RebuildInterface();
        yield return null;
        Assert.That(game.SelectedPiece, Is.SameAs(piece));
        Assert.That(Preview.HasInteractivePreview, Is.True);
        Click("PreviewZoomInButton");
        yield return null;
        Assert.That(Preview.ZoomPercent, Is.EqualTo(120));
        game.CancelSelection();
        yield return null;
        Assert.That(GameObject.Find("SelectedPiecePanel"), Is.Null);
        Assert.That(GameObject.Find("SelectedPiecePreviewClone"), Is.Null);
    }

    [UnityTest]
    public IEnumerator WorldSpacePanelKeepsVisibleRotationZoomAndResetControls()
    {
        InputDevice headset = InputSystem.AddDevice("XRHMD");
        try
        {
            var hud = Object.FindFirstObjectByType<GameHud>();
            hud.RebuildInterface();
            yield return null;
            Assert.That(hud.GetComponent<Canvas>().renderMode, Is.EqualTo(RenderMode.WorldSpace));
            Assert.That(GameObject.Find("PreviewGestureHint").GetComponent<UnityEngine.UI.Text>().text, Does.Contain("botões"));
            Quaternion original = GameObject.Find("SelectedPiecePreviewClone").transform.rotation;
            Click("PreviewRotateLeftButton");
            Click("PreviewZoomInButton");
            yield return null;
            Assert.That(Quaternion.Angle(original, GameObject.Find("SelectedPiecePreviewClone").transform.rotation), Is.GreaterThan(25));
            Assert.That(Preview.ZoomPercent, Is.EqualTo(120));
            Camera previewCamera = GameObject.Find("SelectedPiecePreviewCamera").GetComponent<Camera>();
            Vector3 positionBeforePan = previewCamera.transform.position;
            Click("PreviewMoveDownButton");
            yield return null;
            Assert.That(Vector3.Dot(previewCamera.transform.position - positionBeforePan, previewCamera.transform.up), Is.GreaterThan(.05f));
            Assert.That(Preview.ZoomPercent, Is.EqualTo(120));
            Click("PreviewResetButton");
            yield return null;
            Assert.That(Preview.ZoomPercent, Is.EqualTo(100));
            Assert.That(Quaternion.Angle(original, GameObject.Find("SelectedPiecePreviewClone").transform.rotation), Is.LessThan(.01f));
            Assert.That(game.MoveHistory.Count, Is.Zero);
        }
        finally { InputSystem.RemoveDevice(headset); }
    }

    private static SelectedPiecePreviewInput Preview => Object.FindFirstObjectByType<SelectedPiecePreviewInput>();
    private static UnityEngine.UI.Button Button(string name) => GameObject.Find(name).GetComponent<UnityEngine.UI.Button>();
    private static void Click(string name) => Button(name).onClick.Invoke();
}
#endif
