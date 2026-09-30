#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Casters;

public sealed class BoardResizeTests
{
    private const string HeightKey = "ChessCgi.TableHeightStep";
    private XRSimulatedHMD headset;
    private BoardView board;
    private TableView table;
    private ChessGameController game;
    private Transform room;
    private bool hadHeight;
    private int savedHeight;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        hadHeight = PlayerPrefs.HasKey(HeightKey);
        savedHeight = PlayerPrefs.GetInt(HeightKey, 0);
        PlayerPrefs.DeleteKey(HeightKey);
        headset = InputSystem.AddDevice<XRSimulatedHMD>();
        EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/Main.unity", new LoadSceneParameters(LoadSceneMode.Single));
        yield return null;
        yield return null;
        board = Object.FindFirstObjectByType<BoardView>();
        table = Object.FindFirstObjectByType<TableView>();
        game = Object.FindFirstObjectByType<ChessGameController>();
        room = GameObject.Find("FeevaleComputerLab").transform;
        game.StartLocalGame();
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        if (headset != null && headset.added) InputSystem.RemoveDevice(headset);
        if (hadHeight) PlayerPrefs.SetInt(HeightKey, savedHeight);
        else PlayerPrefs.DeleteKey(HeightKey);
        Scene previous = SceneManager.GetActiveScene();
        SceneManager.SetActiveScene(SceneManager.CreateScene("Empty after board resize test"));
        yield return SceneManager.UnloadSceneAsync(previous);
    }

    private float FrameBottom() => board.BoardFrameRoot.GetComponentsInChildren<Renderer>().Min(r => r.bounds.min.y);
    private void AssertContact() => Assert.That(FrameBottom(), Is.EqualTo(table.Height).Within(.0002f),
        "Changing the personal size must keep the bottom of the frame on the tabletop.");

    [UnityTest]
    public IEnumerator ResizeChangesBoardPiecesAndCollidersInTheSameFrame()
    {
        PieceView piece = board.Pieces.First(p => p.Square.Equals(BoardSquare.FromAlgebraic("a1")));
        float pieceWidth = piece.GetComponent<Collider>().bounds.size.x;
        float frameWidth = board.BoardFrameRoot.GetComponentsInChildren<Renderer>().Max(r => r.bounds.max.x)
            - board.BoardFrameRoot.GetComponentsInChildren<Renderer>().Min(r => r.bounds.min.x);
        Assert.That(board.SetVrSize(1.4f), Is.True);
        Assert.That(piece.GetComponent<Collider>().bounds.size.x, Is.EqualTo(pieceWidth * 1.4f).Within(.0002f));
        float newWidth = board.BoardFrameRoot.GetComponentsInChildren<Renderer>().Max(r => r.bounds.max.x)
            - board.BoardFrameRoot.GetComponentsInChildren<Renderer>().Min(r => r.bounds.min.x);
        Assert.That(newWidth, Is.EqualTo(frameWidth * 1.4f).Within(.0002f));
        foreach (PieceView placed in board.Pieces)
            Assert.That(Vector3.Distance(placed.transform.position, board.GetPieceWorldPosition(placed.Square)), Is.LessThan(.0002f));
        AssertContact();
        yield return null;
    }

    [UnityTest]
    public IEnumerator ResizingLeavesRoomTableCameraAndBoardOrientationUnchanged()
    {
        Matrix4x4 roomPose = room.localToWorldMatrix;
        Vector3 tablePosition = table.transform.position, tableScale = table.transform.lossyScale;
        Camera camera = XRRig.EyeCamera != null ? XRRig.EyeCamera : Camera.main;
        Vector3 eyePosition = camera.transform.position;
        Quaternion eyeRotation = camera.transform.rotation, boardRotation = board.transform.rotation;
        foreach (float size in new[] { 1.5f, .75f, 1f })
        {
            board.SetVrSize(size);
            yield return null;
            Assert.That(Vector3.Distance(room.position, roomPose.GetColumn(3)), Is.LessThan(.0002f));
            Assert.That(Vector3.Distance(room.lossyScale, Vector3.one), Is.LessThan(.0002f));
            Assert.That(Vector3.Distance(table.transform.position, tablePosition), Is.LessThan(.0002f));
            Assert.That(Vector3.Distance(table.transform.lossyScale, tableScale), Is.LessThan(.0002f));
            Assert.That(Vector3.Distance(camera.transform.position, eyePosition), Is.LessThan(.0002f));
            Assert.That(Quaternion.Angle(camera.transform.rotation, eyeRotation), Is.LessThan(.001f));
            Assert.That(Quaternion.Angle(board.transform.rotation, boardRotation), Is.LessThan(.001f));
            AssertContact();
        }
    }

    [UnityTest]
    public IEnumerator HeightAndNewGamesPreserveSizeAndContact()
    {
        board.SetVrSize(1.35f);
        foreach (int step in new[] { table.MinStep, table.MaxStep, 0 })
        {
            table.SetHeightStep(step);
            yield return null;
            Assert.That(board.VrSize, Is.EqualTo(1.35f));
            AssertContact();
        }
        game.NewGame();
        yield return null;
        Assert.That(board.VrSize, Is.EqualTo(1.35f));
        Assert.That(board.Pieces.Count, Is.EqualTo(32));
        Assert.That(board.Squares.Count, Is.EqualTo(64));
        AssertContact();
    }

    [UnityTest]
    public IEnumerator LimitsAndNonFiniteInputCannotCollapseOrExplodeTheBoard()
    {
        board.SetVrSize(float.MaxValue);
        Assert.That(board.VrSize, Is.EqualTo(BoardView.MaximumVrSize));
        board.SetVrSize(-float.MaxValue);
        Assert.That(board.VrSize, Is.EqualTo(BoardView.MinimumVrSize));
        Vector3 validPosition = board.transform.position, validScale = board.transform.localScale;
        foreach (float invalid in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity })
        {
            Assert.That(board.SetVrSize(invalid), Is.False);
            Assert.That(board.transform.position, Is.EqualTo(validPosition));
            Assert.That(board.transform.localScale, Is.EqualTo(validScale));
        }
        AssertContact();
        yield return null;
    }

    [UnityTest]
    public IEnumerator EverySquareAndMoveStillResolveAfterEnlargingAndShrinking()
    {
        foreach (float size in new[] { .75f, 1.5f })
        {
            board.SetVrSize(size);
            foreach (SquareView tile in board.Squares)
            {
                Assert.That(board.TryGetSquareAt(board.GetWorldPosition(tile.Square), out BoardSquare found), Is.True);
                Assert.That(found, Is.EqualTo(tile.Square));
            }
            Vector3 target = board.GetWorldPosition(BoardSquare.FromAlgebraic("c4"));
            Assert.That(Physics.Raycast(target + Vector3.up, Vector3.down, out RaycastHit hit, 2), Is.True);
            Assert.That(hit.collider.GetComponent<SquareView>().Square, Is.EqualTo(BoardSquare.FromAlgebraic("c4")));
        }
        PieceView pawn = board.Pieces.First(p => p.Square.Equals(BoardSquare.FromAlgebraic("e2")));
        game.GrabPiece(pawn);
        Assert.That(board.HighlightCount, Is.EqualTo(2));
        Assert.That(game.ReleasePiece(pawn, board.GetPieceWorldPosition(BoardSquare.FromAlgebraic("e4")), out bool moveStarted), Is.True);
        Assert.That(moveStarted, Is.True);
        yield return new WaitForSeconds(.6f);
        Assert.That(game.CurrentTurn, Is.EqualTo(ChessSide.Black));
        Assert.That(game.MoveHistory.Count, Is.EqualTo(1));
        Assert.That(board.Pieces.Any(p => p.Square.Equals(BoardSquare.FromAlgebraic("e4"))), Is.True);
    }

    [UnityTest]
    public IEnumerator AnInvalidVrDropCanBeThrownAndReturnsToItsSquareAfterResizing()
    {
        if (Object.FindFirstObjectByType<XRRig>() == null)
            new GameObject("Throwable resize replay bootstrap").AddComponent<XRRig>();
        yield return null;
        yield return null;
        Assert.That(XRRig.Origin, Is.Not.Null);

        // Keep the input pose under the replay's control. The separate prefab
        // test verifies that the real tracked hand reaches the same piece layer.
        var replay = new GameObject("Controlled near-grab replay");
        replay.SetActive(false);
        replay.transform.SetParent(XRRig.Origin, false);
        var caster = replay.AddComponent<SphereInteractionCaster>();
        caster.castOrigin = replay.transform;
        caster.castRadius = .06f;
        caster.physicsLayerMask = 1 << PieceView.PhysicsLayer;
        var far = replay.AddComponent<CurveInteractionCaster>();
        var attachment = replay.AddComponent<UnityEngine.XR.Interaction.Toolkit.Attachment.InteractionAttachController>();
        attachment.transformToFollow = replay.transform;
        var near = replay.AddComponent<NearFarInteractor>();
        near.interactionManager = Object.FindFirstObjectByType<UnityEngine.XR.Interaction.Toolkit.XRInteractionManager>();
        near.nearInteractionCaster = caster;
        near.farInteractionCaster = far;
        near.interactionAttachController = attachment;
        near.enableFarCasting = false;
        near.enableUIInteraction = false;
        near.selectInput = new XRInputButtonReader("Controlled drop replay")
        {
            inputSourceMode = XRInputButtonReader.InputSourceMode.ManualValue,
        };
        replay.SetActive(true);
        PieceView pawn = board.Pieces.First(p => p.Square.Equals(BoardSquare.FromAlgebraic("e2")));
        var throwable = pawn.GetComponent<ThrowablePiece>();
        Rigidbody body = pawn.GetComponent<Rigidbody>();
        Quaternion upright = pawn.transform.localRotation;
        near.nearInteractionCaster.castOrigin.position = pawn.transform.position;
        Physics.SyncTransforms();
        yield return null;
        yield return null;
        near.selectInput.QueueManualState(true, 1f);
        yield return null;
        yield return null;
        yield return null;
        Assert.That(throwable.isSelected, Is.True,
            $"The controlled near caster must select e2; selected={game.SelectedPiece?.name}, hover={near.hasHover}, select={near.isSelectActive}, pose={near.transform.position}, piece={pawn.transform.position}.");
        Assert.That(game.SelectedPiece, Is.SameAs(pawn));

        near.transform.position += board.GetPieceWorldPosition(BoardSquare.FromAlgebraic("e5")) - pawn.transform.position;
        near.transform.rotation = Quaternion.Euler(25f, 20f, 10f);
        yield return new WaitForSeconds(.3f);
        Assert.That(Vector3.Distance(pawn.transform.position, board.GetPieceWorldPosition(pawn.Square)), Is.GreaterThan(.02f),
            "The replayed attachment pose must actually move the held piece before release.");
        near.selectInput.QueueManualState(false, 0f);
        yield return null;
        yield return null;
        yield return null;
        Assert.That(game.SelectedPiece, Is.Null);
        Assert.That(body.isKinematic, Is.False, "An illegal VR drop must retain the colleagues' throwable physics.");
        Assert.That(Vector3.Distance(pawn.transform.position, board.GetPieceWorldPosition(pawn.Square)), Is.GreaterThan(.02f),
            "The dropped piece must not snap straight back to its square.");
        Assert.That(pawn.Square, Is.EqualTo(BoardSquare.FromAlgebraic("e2")));
        Assert.That(game.MoveHistory.Count, Is.Zero);

        board.SetVrSize(1.4f);
        yield return new WaitForSeconds(3.05f);
        Assert.That(body.isKinematic, Is.True, "The delayed return must freeze the thrown rigidbody.");
        board.SetVrSize(.9f);
        yield return new WaitForSeconds(.5f);
        Assert.That(Vector3.Distance(pawn.transform.position, board.GetPieceWorldPosition(pawn.Square)), Is.LessThan(.0002f));
        Assert.That(Quaternion.Angle(pawn.transform.localRotation, upright), Is.LessThan(.01f));
        Assert.That(game.MoveHistory.Count, Is.Zero);
        Assert.That(game.CurrentTurn, Is.EqualTo(ChessSide.White));
    }

    [UnityTest]
    public IEnumerator InterruptedVrMoveResumesOnceWithoutLosingSizeOrTurn()
    {
        board.SetVrSize(1.3f);
        PieceView pawn = board.Pieces.First(p => p.Square.Equals(BoardSquare.FromAlgebraic("e2")));
        int notifications = 0;
        game.MoveApplied += _ => notifications++;
        game.SelectPiece(pawn);
        game.SelectDestination(BoardSquare.FromAlgebraic("e4"));
        Assert.That(game.IsAnimatingMove, Is.True);
        game.enabled = false;
        Assert.That(notifications, Is.Zero, "Scene teardown must not publish an interrupted move.");
        yield return null;
        game.enabled = true;
        Assert.That(board.Pieces.Any(p => p.Square.Equals(BoardSquare.FromAlgebraic("e4"))), Is.True);
        Assert.That(game.CurrentTurn, Is.EqualTo(ChessSide.Black));
        Assert.That(game.IsInputBlocked, Is.False);
        Assert.That(board.VrSize, Is.EqualTo(1.3f));
        Assert.That(notifications, Is.EqualTo(1));
        game.enabled = false;
        game.enabled = true;
        yield return new WaitForSeconds(.4f);
        Assert.That(notifications, Is.EqualTo(1), "Reactivation and the cancelled animation cannot duplicate the move.");
        Assert.That(game.MoveHistory.Count, Is.EqualTo(1));
        AssertContact();
    }

    [UnityTest]
    public IEnumerator ActualTrackedHandPrefabNearCasterCanSelectTheGamesPieceLayer()
    {
        if (Object.FindFirstObjectByType<XRRig>() == null)
            new GameObject("Tracked hand near-cast test bootstrap").AddComponent<XRRig>();
        yield return null;
        yield return null;
        Assert.That(XRRig.Origin, Is.Not.Null);
        Transform offset = XRRig.Origin.Find("Camera Offset");
        offset.GetComponent<XRInputModalityManager>().enabled = false;
        GameObject handRoot = offset.Find("LeftHandInteractor").gameObject;
        foreach (UnityEngine.InputSystem.XR.TrackedPoseDriver driver in handRoot.GetComponentsInChildren<UnityEngine.InputSystem.XR.TrackedPoseDriver>(true))
            driver.enabled = false;
        handRoot.SetActive(true);
        NearFarInteractor near = handRoot.GetComponentInChildren<NearFarInteractor>(true);
        near.enableFarCasting = false;
        near.enableUIInteraction = false;
        near.selectInput = new XRInputButtonReader("Replay hand pinch")
        {
            inputSourceMode = XRInputButtonReader.InputSourceMode.ManualValue,
        };
        Assert.That(((SphereInteractionCaster)near.nearInteractionCaster).physicsLayerMask.value & (1 << PieceView.PhysicsLayer), Is.Not.Zero);
        PieceView pawn = board.Pieces.First(p => p.Square.Equals(BoardSquare.FromAlgebraic("e2")));
        near.nearInteractionCaster.castOrigin.position = pawn.transform.position;
        Physics.SyncTransforms();
        yield return null;
        yield return null;
        near.selectInput.QueueManualState(true, 1f);
        yield return null;
        yield return null;
        yield return null;
        Assert.That(pawn.GetComponent<XRGrabInteractable>().isSelected, Is.True,
            "The actual hand prefab, configured by the rig, must reach layer 6 through its real near caster.");
        Assert.That(game.SelectedPiece, Is.SameAs(pawn));
        Assert.That(board.HighlightCount, Is.EqualTo(2));
        near.selectInput.QueueManualState(false, 0f);
        yield return null;
        yield return null;
        Assert.That(game.SelectedPiece, Is.Null);
    }

    [UnityTest]
    public IEnumerator DestroyingAnActiveVrGameDoesNotRecreatePiecesOrTheInteractionManager()
    {
        PieceView[] existingPieces = board.Pieces.ToArray();
        foreach (UnityEngine.XR.Interaction.Toolkit.XRInteractionManager manager in
            Object.FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.XRInteractionManager>(FindObjectsSortMode.None))
            Object.DestroyImmediate(manager);
        Object.Destroy(game);
        yield return null;
        Assert.That(Object.FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.XRInteractionManager>(FindObjectsSortMode.None),
            Is.Empty, "Tearing down a game cannot create a replacement XR manager.");
        Assert.That(board.Pieces.ToArray(), Is.EqualTo(existingPieces),
            "Destruction must not rebuild the piece hierarchy inside OnDisable.");
    }

    [UnityTest]
    public IEnumerator DesktopKeepsItsOriginalScaleAndVrSizeIsSessionOnly()
    {
        board.SetVrSize(1.3f);
        InputSystem.RemoveDevice(headset);
        yield return null;
        yield return null;
        Assert.That(board.AppliedVrSize, Is.EqualTo(1f));
        Assert.That(board.transform.localScale, Is.EqualTo(Vector3.one));
        Assert.That(board.SetVrSize(1.5f), Is.False);
        headset = InputSystem.AddDevice<XRSimulatedHMD>();
        yield return null;
        yield return null;
        Assert.That(board.AppliedVrSize, Is.EqualTo(1.3f));
        AssertContact();
        EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/Main.unity", new LoadSceneParameters(LoadSceneMode.Single));
        yield return null;
        yield return null;
        board = Object.FindFirstObjectByType<BoardView>();
        table = Object.FindFirstObjectByType<TableView>();
        Assert.That(board.VrSize, Is.EqualTo(1f), "A freshly constructed session does not persist the personal size.");
        AssertContact();
    }
}
#endif
