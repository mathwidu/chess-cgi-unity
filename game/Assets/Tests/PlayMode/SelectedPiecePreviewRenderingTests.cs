#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class SelectedPiecePreviewRenderingTests
{
    private ChessGameController game;
    private BoardView board;
    private GameHud hud;
    private int previewRenders;
    private InputDevice headset;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        previewRenders = 0;
        RenderPipelineManager.beginCameraRendering += CountPreviewRender;
        EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/Main.unity", new LoadSceneParameters(LoadSceneMode.Single));
        yield return null;
        yield return null;
        game = Object.FindFirstObjectByType<ChessGameController>();
        board = Object.FindFirstObjectByType<BoardView>();
        hud = Object.FindFirstObjectByType<GameHud>();
        Click("LocalModeButton");
        Click("StartPlayButton");
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        RenderPipelineManager.beginCameraRendering -= CountPreviewRender;
        if (headset != null) InputSystem.RemoveDevice(headset);
        Scene previous = SceneManager.GetActiveScene();
        SceneManager.SetActiveScene(SceneManager.CreateScene("Empty after preview rendering test"));
        yield return SceneManager.UnloadSceneAsync(previous);
    }

    [UnityTest]
    public IEnumerator InitialImageIsRenderedOnceAndStaticFramesReuseItsFullQualityTexture()
    {
        Assert.That(previewRenders, Is.Zero, "No selection needs no preview render.");
        Select("f2");
        Assert.That(previewRenders, Is.EqualTo(1), "A selected piece must have an initial image.");
        var texture = (RenderTexture)GameObject.Find("SelectedPiecePreview").GetComponent<UnityEngine.UI.RawImage>().texture;
        Assert.That(texture.width, Is.EqualTo(1104));
        Assert.That(texture.height, Is.EqualTo(660));
        Assert.That(texture.antiAliasing, Is.EqualTo(4));
        for (int frame = 0; frame < 4; frame++)
        {
            hud.RefreshInterface();
            yield return null;
        }
        Assert.That(previewRenders, Is.EqualTo(1), "An unchanged preview must reuse the rendered image.");
    }

    [UnityTest]
    public IEnumerator ZoomRotationPanAndResetUpdateTheImageOnceThenSettle()
    {
        Select("f2");
        foreach (string control in new[] { "PreviewZoomInButton", "PreviewRotateRightButton", "PreviewMoveDownButton", "PreviewResetButton" })
        {
            int before = previewRenders;
            Click(control);
            hud.RefreshInterface();
            Assert.That(previewRenders, Is.EqualTo(before + 1), control + " must update the image.");
            yield return null;
            hud.RefreshInterface();
            yield return null;
            Assert.That(previewRenders, Is.EqualTo(before + 1), control + " must not cause continuous rendering.");
        }
    }

    [UnityTest]
    public IEnumerator SelectionChangesCancellationAndHudRecreationNeverReuseTheWrongImage()
    {
        Select("f2");
        GameObject first = GameObject.Find("SelectedPiecePreviewClone");
        int before = previewRenders;
        Select("g1");
        Assert.That(previewRenders, Is.EqualTo(before + 1));
        Assert.That(first.activeInHierarchy, Is.False, "The first replacement image cannot include the old piece.");
        // The old clone is destroyed at the end of the frame; inspect the new one afterwards.
        yield return null;
        Assert.That(GameObject.Find("SelectedPiecePreviewClone"), Is.Not.SameAs(first));
        before = previewRenders;
        game.CancelSelection();
        hud.RefreshInterface();
        yield return null;
        Assert.That(GameObject.Find("SelectedPiecePreviewClone"), Is.Null);
        Assert.That(previewRenders, Is.EqualTo(before), "Cancellation must not render the hidden panel.");
        Select("f2");
        Assert.That(previewRenders, Is.EqualTo(before + 1), "Returning to a piece needs a fresh image.");
        before = previewRenders;
        hud.RebuildInterface();
        Assert.That(previewRenders, Is.EqualTo(before + 1), "Recreated UI must receive a rendered image.");
        yield return null;
        Assert.That(previewRenders, Is.EqualTo(before + 1));
    }

    [UnityTest]
    public IEnumerator CameraLightingModelAndLostTextureRefreshTheImage()
    {
        Select("f2");
        var camera = GameObject.Find("SelectedPiecePreviewCamera").GetComponent<Camera>();
        var light = GameObject.Find("SelectedPiecePreviewLight").GetComponent<Light>();
        var part = GameObject.Find("SelectedPiecePreviewClone").GetComponentInChildren<Renderer>().transform;
        int before = previewRenders;
        camera.fieldOfView -= 3f;
        hud.RefreshInterface();
        Assert.That(previewRenders, Is.EqualTo(++before), "A projection change needs a new image.");
        light.intensity *= .75f;
        hud.RefreshInterface();
        Assert.That(previewRenders, Is.EqualTo(++before), "Preview lighting affects the image.");
        part.localScale *= 1.05f;
        hud.RefreshInterface();
        Assert.That(previewRenders, Is.EqualTo(++before), "A visible child transform affects the image.");
        camera.targetTexture.Release();
        hud.RefreshInterface();
        Assert.That(camera.targetTexture.IsCreated(), Is.True);
        Assert.That(previewRenders, Is.EqualTo(++before), "A lost render target needs a fresh image.");
        yield return null;
        Assert.That(previewRenders, Is.EqualTo(before));
    }

    [UnityTest]
    public IEnumerator WorldSpacePreviewAlsoReusesTheImageUntilAControlChangesTheView()
    {
        headset = InputSystem.AddDevice("XRHMD");
        hud.RebuildInterface();
        Select("f2");
        Assert.That(hud.GetComponent<Canvas>().renderMode, Is.EqualTo(RenderMode.WorldSpace));
        int before = previewRenders;
        for (int frame = 0; frame < 3; frame++) yield return null;
        Assert.That(previewRenders, Is.EqualTo(before));
        Click("PreviewRotateLeftButton");
        hud.RefreshInterface();
        Assert.That(previewRenders, Is.EqualTo(before + 1));
    }

    [UnityTest]
    public IEnumerator PhysicalPieceCloneStaysStillWithoutChangingTheOriginalPhysicsOrSelection()
    {
        PieceView source = board.Pieces.First(p => p.Square.Equals(BoardSquare.FromAlgebraic("f2")));
        var sourceBody = source.gameObject.AddComponent<Rigidbody>();
        sourceBody.useGravity = true;
        sourceBody.interpolation = RigidbodyInterpolation.Interpolate;
        var throwable = source.gameObject.AddComponent<ThrowablePiece>();
        throwable.SetRest(source.transform.localPosition);
        source.gameObject.AddComponent<VrSelectionBridge>();
        source.gameObject.AddComponent<PieceGrabHighlight>();
        Vector3 sourcePosition = source.transform.position;
        Quaternion sourceRotation = source.transform.rotation;
        bool sourceHeld = ThrowablePiece.AnyPieceHeld;
        Select("f2");
        var clone = GameObject.Find("SelectedPiecePreviewClone");
        var cloneBody = clone.GetComponent<Rigidbody>();
        Assert.That(cloneBody.isKinematic, Is.True, "The presentation clone must not fall under gravity.");
        Assert.That(cloneBody.useGravity, Is.False);
        Assert.That(cloneBody.interpolation, Is.EqualTo(RigidbodyInterpolation.None));
        Assert.That(clone.GetComponent<ThrowablePiece>().enabled, Is.False, "A preview cannot register as an interactable.");
        Assert.That(clone.GetComponent<VrSelectionBridge>().enabled, Is.False);
        Assert.That(clone.GetComponent<PieceGrabHighlight>().enabled, Is.False);
        Click("PreviewRotateRightButton");
        hud.RefreshInterface();
        Vector3 clonePosition = clone.transform.position;
        Quaternion cloneRotation = clone.transform.rotation;
        int before = previewRenders;
        for (int frame = 0; frame < 3; frame++) yield return new WaitForFixedUpdate();
        Assert.That(Vector3.Distance(clone.transform.position, clonePosition), Is.LessThan(.0001f));
        Assert.That(Quaternion.Angle(clone.transform.rotation, cloneRotation), Is.LessThan(.01f));
        Assert.That(previewRenders, Is.EqualTo(before));
        Assert.That(sourceBody.isKinematic, Is.False);
        Assert.That(sourceBody.useGravity, Is.True);
        Assert.That(sourceBody.interpolation, Is.EqualTo(RigidbodyInterpolation.Interpolate));
        Assert.That(throwable.enabled, Is.True);
        Assert.That(source.GetComponent<VrSelectionBridge>().enabled, Is.True);
        Assert.That(source.GetComponent<PieceGrabHighlight>().enabled, Is.True);
        Assert.That(Vector3.Distance(source.transform.position, sourcePosition), Is.LessThan(.001f));
        Assert.That(Quaternion.Angle(source.transform.rotation, sourceRotation), Is.LessThan(.01f));
        Assert.That(game.SelectedPiece, Is.SameAs(source));
        Assert.That(ThrowablePiece.AnyPieceHeld, Is.EqualTo(sourceHeld));
        Assert.That(game.MoveHistory.Count, Is.Zero);
    }

    private void Select(string square)
    {
        game.SelectPiece(board.Pieces.First(p => p.Square.Equals(BoardSquare.FromAlgebraic(square))));
        hud.RefreshInterface();
    }

    private void CountPreviewRender(ScriptableRenderContext context, Camera camera)
    {
        if (camera.name == "SelectedPiecePreviewCamera") previewRenders++;
    }

    private static void Click(string name) => GameObject.Find(name).GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
}
#endif
