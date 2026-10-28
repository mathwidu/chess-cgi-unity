#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public sealed class QuestCharacterDetailIntegrationTests
{
    private ChessGameController game;
    private GameHud hud;
    private PieceView piece;
    private MeshFilter filter;
    private Mesh high, low;
    private QuestCharacterDetail detail;
    private Camera farCamera;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/Main.unity", new LoadSceneParameters(LoadSceneMode.Single));
        yield return null;
        yield return null;
        game = Object.FindFirstObjectByType<ChessGameController>();
        hud = Object.FindFirstObjectByType<GameHud>();
        PieceFactory factory = Object.FindFirstObjectByType<PieceFactory>();
        factory.UsePrimitivePieces = false;
        factory.UseQuestCharacterGeometry = false;
        Click("LocalModeButton");
        Click("StartPlayButton");
        piece = Object.FindFirstObjectByType<BoardView>().Pieces.First(p => p.Square.Equals(BoardSquare.FromAlgebraic("f2")));
        Transform visual = piece.transform.Find("CustomVisual");
        filter = visual.GetComponentInChildren<MeshFilter>();
        high = filter.sharedMesh;
        low = Object.Instantiate(high);
        low.name = "Synthetic table integration mesh";
        detail = visual.gameObject.AddComponent<QuestCharacterDetail>();
        Assert.That(detail.Configure(new[] { filter }, new[] { high }, new[] { low }), Is.True);
        farCamera = new GameObject("Far detail integration camera").AddComponent<Camera>();
        farCamera.enabled = false;
        farCamera.transform.position = piece.transform.position + Vector3.back * 100f;
        detail.RefreshForCamera(farCamera);
        Assert.That(filter.sharedMesh, Is.SameAs(low));
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Scene previous = SceneManager.GetActiveScene();
        SceneManager.SetActiveScene(SceneManager.CreateScene("Empty after detail integration test"));
        yield return SceneManager.UnloadSceneAsync(previous);
        Object.DestroyImmediate(low);
    }

    [UnityTest]
    public IEnumerator HudPreviewLocksHighDetailOnItsOwnFilters()
    {
        int rendererCount = piece.GetComponentsInChildren<Renderer>(true).Length;
        int colliderCount = piece.GetComponentsInChildren<Collider>(true).Length;
        game.SelectPiece(piece);
        hud.RefreshInterface();
        var clone = GameObject.Find("SelectedPiecePreviewClone");
        var cloneDetail = clone.GetComponentInChildren<QuestCharacterDetail>(true);
        var cloneFilter = clone.GetComponentsInChildren<MeshFilter>(true).First(f => f.name == filter.name);
        Assert.That(cloneFilter.sharedMesh, Is.SameAs(high));
        Assert.That(cloneDetail.enabled, Is.False);
        Assert.That(filter.sharedMesh, Is.SameAs(low), "Opening the preview must not alter the board piece.");
        Assert.That(detail.enabled, Is.True);
        Assert.That(piece.GetComponentsInChildren<Renderer>(true).Length, Is.EqualTo(rendererCount));
        Assert.That(piece.GetComponentsInChildren<Collider>(true).Length, Is.EqualTo(colliderCount));
        for (int frame = 0; frame < 3; frame++)
        {
            cloneDetail.RefreshForCamera(farCamera);
            yield return null;
            Assert.That(cloneFilter.sharedMesh, Is.SameAs(high));
        }
    }

    [UnityTest]
    public IEnumerator HoverAndGrabKeepOutlineMeshesCoherentWithoutRecursivelyAddingShells()
    {
        var manager = Object.FindFirstObjectByType<XRInteractionManager>();
        if (manager == null) manager = new GameObject("Detail test interaction manager").AddComponent<XRInteractionManager>();
        Rigidbody body = piece.GetComponent<Rigidbody>();
        if (body == null) body = piece.gameObject.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        XRGrabInteractable grab = piece.GetComponent<XRGrabInteractable>();
        if (grab == null) grab = piece.gameObject.AddComponent<XRGrabInteractable>();
        // Configure may precede an interactable in an editor fixture; rebind before observing hover.
        Assert.That(detail.Configure(new[] { filter }, new[] { high }, new[] { low }), Is.True);
        var highlight = piece.GetComponent<PieceGrabHighlight>();
        if (highlight == null) highlight = piece.gameObject.AddComponent<PieceGrabHighlight>();
        var hand = new GameObject("Detail test hover interactor");
        hand.transform.position = Vector3.one * 100f;
        hand.AddComponent<SphereCollider>().isTrigger = true;
        var interactor = hand.AddComponent<XRDirectInteractor>();
        int originalFilters = piece.GetComponentsInChildren<MeshFilter>(true).Length;
        for (int cycle = 0; cycle < 3; cycle++)
        {
            detail.RefreshForCamera(farCamera);
            manager.HoverEnter((IXRHoverInteractor)interactor, (IXRHoverInteractable)grab);
            Assert.That(highlight.IsLit, Is.True);
            Assert.That(filter.sharedMesh, Is.SameAs(high));
            AssertShells(piece.gameObject, originalFilters);
            manager.HoverExit((IXRHoverInteractor)interactor, (IXRHoverInteractable)grab);
            detail.RefreshForCamera(farCamera);
            Assert.That(filter.sharedMesh, Is.SameAs(low));
            AssertShells(piece.gameObject, originalFilters);
        }
        manager.SelectEnter((IXRSelectInteractor)interactor, (IXRSelectInteractable)grab);
        detail.RefreshForCamera(farCamera);
        Assert.That(filter.sharedMesh, Is.SameAs(high));
        manager.SelectExit((IXRSelectInteractor)interactor, (IXRSelectInteractable)grab);
        detail.RefreshForCamera(farCamera);
        var clone = Object.Instantiate(piece.gameObject, piece.transform.parent);
        var cloneGrab = clone.GetComponent<XRGrabInteractable>();
        manager.HoverEnter((IXRHoverInteractor)interactor, (IXRHoverInteractable)cloneGrab);
        AssertShells(clone, originalFilters);
        Assert.That(clone.GetComponentsInChildren<MeshRenderer>(true).Length,
            Is.EqualTo(piece.GetComponentsInChildren<MeshRenderer>(true).Length));
        manager.HoverExit((IXRHoverInteractor)interactor, (IXRHoverInteractable)cloneGrab);
        yield return null;
    }

    private static void AssertShells(GameObject root, int expected)
    {
        MeshFilter[] shells = root.GetComponentsInChildren<MeshFilter>(true).Where(f => f.name == "GrabOutline").ToArray();
        Assert.That(shells.Length, Is.EqualTo(expected), "One shell per authored part, never shells of shells.");
        foreach (MeshFilter shell in shells)
            Assert.That(shell.sharedMesh, Is.SameAs(shell.transform.parent.GetComponent<MeshFilter>().sharedMesh));
    }

    private static void Click(string name) => GameObject.Find(name).GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
}
#endif
