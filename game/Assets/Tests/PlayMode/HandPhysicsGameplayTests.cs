#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public sealed class HandPhysicsGameplayTests
{
    private XRSimulatedHMD headset;
    private float previousCaptureDeltaTime;
    private BoardView board;
    private ChessGameController game;
    private XRInteractionManager manager;
    private XRDirectInteractor grabber;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        previousCaptureDeltaTime = Time.captureDeltaTime;
        // XRI estimates throw velocity from render samples. An uncapped batch
        // frame must not compress a headset replay into sub-millisecond steps.
        Time.captureDeltaTime = 1f / 72f;
        headset = InputSystem.AddDevice<XRSimulatedHMD>();
        EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/Main.unity", new LoadSceneParameters(LoadSceneMode.Single));
        yield return null;
        yield return null;
        board = Object.FindFirstObjectByType<BoardView>();
        game = Object.FindFirstObjectByType<ChessGameController>();
        manager = Object.FindFirstObjectByType<XRInteractionManager>();
        game.StartLocalGame();
        var handObject = new GameObject("Deliberate grip replay");
        handObject.AddComponent<SphereCollider>().isTrigger = true;
        grabber = handObject.AddComponent<XRDirectInteractor>();
        grabber.interactionManager = manager;
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Time.captureDeltaTime = previousCaptureDeltaTime;
        if (headset != null && headset.added) InputSystem.RemoveDevice(headset);
        Scene previous = SceneManager.GetActiveScene();
        SceneManager.SetActiveScene(SceneManager.CreateScene("Empty after physical chess replay"));
        yield return SceneManager.UnloadSceneAsync(previous);
    }

    [UnityTest]
    public IEnumerator PlacementFrameShowsTheHeldPiecesSquareAndClearsAfterRelease()
    {
        PieceView piece = Piece("e2");
        ThrowablePiece grab = piece.GetComponent<ThrowablePiece>();
        grabber.transform.position = grab.colliders[0].bounds.center;
        manager.SelectEnter((IXRSelectInteractor)grabber, grab);
        yield return Steps(12);
        var preview = board.GetComponent<BoardPlacementPreview>();
        Assert.That(preview, Is.Not.Null);
        Assert.That(preview.Target, Is.EqualTo(BoardSquare.FromAlgebraic("e2")));
        Assert.That(preview.IsLegal, Is.False, "The origin must not promise a new move.");

        foreach (string square in new[] { "e3", "f3" })
        {
            Vector3 target = board.GetPieceWorldPosition(BoardSquare.FromAlgebraic(square)) + Vector3.up * .06f;
            grabber.transform.position += target - piece.transform.position;
            yield return Steps(12);
            Assert.That(preview.Target, Is.EqualTo(BoardSquare.FromAlgebraic(square)),
                "The visible target must agree with the held base, including an illegal square.");
            Assert.That(preview.IsLegal, Is.EqualTo(square == "e3"));
            Assert.That(game.MoveHistory.Count, Is.Zero, "Showing a legal target cannot register a move.");
        }

        manager.SelectExit((IXRSelectInteractor)grabber, grab);
        yield return Steps(2);
        Assert.That(preview.Target, Is.Null);
        LineRenderer frame = preview.GetComponentInChildren<LineRenderer>();
        Assert.That(frame.enabled, Is.False);
        Assert.That(frame.GetComponent<Collider>(), Is.Null, "The guide cannot obstruct a hand or a drop.");
        game.NewGame();
        yield return null;
        Assert.That(board.GetComponents<BoardPlacementPreview>().Length, Is.EqualTo(1));
        Assert.That(preview.Target, Is.Null);
    }

    [UnityTest]
    public IEnumerator ReleasingInTheAirAboveALegalSquareDoesNotMakeAChessMove()
    {
        PieceView piece = Piece("e2");
        ThrowablePiece grab = piece.GetComponent<ThrowablePiece>();
        grabber.transform.position = piece.transform.position;
        manager.SelectEnter((IXRSelectInteractor)grabber, grab);
        yield return Steps(12);
        Vector3 target = board.GetPieceWorldPosition(BoardSquare.FromAlgebraic("e3")) + Vector3.up * .2f;
        grabber.transform.position += target - piece.transform.position;
        yield return Steps(12);
        Assert.That(Vector3.Distance(piece.transform.position, target), Is.LessThan(.01f));
        manager.SelectExit((IXRSelectInteractor)grabber, grab);
        yield return Steps(4);
        Assert.That(game.MoveHistory.Count, Is.Zero, "A release 20 cm above a legal square is a physical toss, not a deliberate board placement.");
        Assert.That(game.CurrentTurn, Is.EqualTo(ChessSide.White));
        Assert.That(piece.Square, Is.EqualTo(BoardSquare.FromAlgebraic("e2")));
        Assert.That(piece.GetComponent<Rigidbody>().isKinematic, Is.False);
        Assert.That(game.SelectedPiece, Is.Null);
    }

    [UnityTest]
    public IEnumerator AnOpenHandPushesRealPiecesAndTheirNeighboursWithoutSelectingOrMovingInChess()
    {
        PieceView first = Piece("e2");
        PieceView neighbour = Piece("e7");
        Vector3 start = new Vector3(48f, 8f, 0f);
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.transform.position = start - Vector3.up * .05f;
        ground.transform.localScale = new Vector3(2f, .1f, 2f);
        Place(first, start);
        Place(neighbour, start + Vector3.right * .045f);
        var raw = new GameObject("Open physical hand replay");
        Vector3 offset = start + new Vector3(-.14f, .025f, 0f);
        Dictionary<XRHandJointID, Vector3> joints = OpenHandJoints();
        bool Read(XRHandJointID id, out Pose pose)
        {
            pose = default;
            if (!joints.TryGetValue(id, out Vector3 point)) return false;
            pose = new Pose(offset + point, Quaternion.identity);
            return true;
        }
        XRPhysicalHand hand = raw.AddComponent<XRPhysicalHand>();
        hand.Configure(raw.transform, raw.transform, raw.transform, Read);
        yield return Steps(8);
        for (int i = 0; i < 50; i++)
        {
            offset += Vector3.right * .003f;
            yield return new WaitForFixedUpdate();
        }
        yield return Steps(6);
        Assert.That(Vector3.Distance(first.transform.position, start), Is.GreaterThan(.015f), "The hand must push the first actual board piece.");
        Assert.That(Vector3.Distance(neighbour.transform.position, start + Vector3.right * .045f), Is.GreaterThan(.01f),
            "The neighbouring opponent piece must respond to the first piece's impact.");
        Assert.That(first.Square, Is.EqualTo(BoardSquare.FromAlgebraic("e2")));
        Assert.That(neighbour.Square, Is.EqualTo(BoardSquare.FromAlgebraic("e7")));
        Assert.That(game.MoveHistory.Count, Is.Zero);
        Assert.That(game.CurrentTurn, Is.EqualTo(ChessSide.White));
        Assert.That(game.SelectedPiece, Is.Null, "Touch is independent of chess selection.");
    }

    [UnityTest]
    public IEnumerator DeliberatePlacementAtBoardHeightStillMakesExactlyOneLegalMove()
    {
        PieceView piece = Piece("e2");
        ThrowablePiece grab = piece.GetComponent<ThrowablePiece>();
        grabber.transform.position = piece.transform.position;
        manager.SelectEnter((IXRSelectInteractor)grabber, grab);
        yield return Steps(12);
        Vector3 target = board.GetPieceWorldPosition(BoardSquare.FromAlgebraic("e3"));
        Vector3 handStart = grabber.transform.position;
        Vector3 handEnd = handStart + target - piece.transform.position;
        for (float t = 0f; t < .4f; t += Time.deltaTime)
        {
            grabber.transform.position = Vector3.Lerp(handStart, handEnd, t / .4f);
            yield return null;
        }
        grabber.transform.position = handEnd;
        // A deliberate placement is slow and settled. A teleport followed by
        // 12 physics ticks can still be inside XRI's .25 s throw history.
        yield return new WaitForSeconds(grab.throwSmoothingDuration + .05f);
        Assert.That(grab.isSelected, Is.True);
        Assert.That(game.SelectedPiece, Is.SameAs(piece));
        Vector3 releasePosition = piece.transform.position;
        manager.SelectExit((IXRSelectInteractor)grabber, grab);
        yield return new WaitForSeconds(.65f);
        Assert.That(game.MoveHistory.Count, Is.EqualTo(1), $"release={releasePosition:F4}; target={target:F4}; ended={piece.transform.position:F4}; status={game.StatusMessage}; selected={game.SelectedPiece}; throw={grab.ReleaseVelocity:F3}; v={piece.GetComponent<Rigidbody>().linearVelocity}");
        Assert.That(game.CurrentTurn, Is.EqualTo(ChessSide.Black));
        Assert.That(board.Pieces.Any(p => p.Square.Equals(BoardSquare.FromAlgebraic("e3")) && p.Side == ChessSide.White), Is.True);
    }

    [UnityTest]
    public IEnumerator AnotherLegalMoveDoesNotReplaceAnUnrelatedPhysicallyLoosePiece()
    {
        PieceView loosePiece = Piece("b8");
        Rigidbody body = loosePiece.GetComponent<Rigidbody>();
        Vector3 away = new Vector3(48f, 8f, 0f);
        body.position = away;
        loosePiece.transform.position = away;
        body.WakeUp();
        yield return Steps(2);
        game.SelectPiece(Piece("e2"));
        game.SelectDestination(BoardSquare.FromAlgebraic("e4"));
        yield return new WaitForSeconds(.65f);
        Assert.That(game.MoveHistory.Count, Is.EqualTo(1));
        Assert.That(loosePiece != null, Is.True, "Completing a chess move must not destroy the other hand's loose piece.");
        Assert.That(board.Pieces, Does.Contain(loosePiece));
        Assert.That(loosePiece.Square, Is.EqualTo(BoardSquare.FromAlgebraic("b8")));
        Assert.That(Vector3.Distance(loosePiece.transform.position, board.GetPieceWorldPosition(loosePiece.Square)), Is.GreaterThan(1f),
            "An unrelated move cannot reset another piece's physical interaction.");
    }

    [UnityTest]
    public IEnumerator AFastLateralReleaseAtBoardHeightIsATossRatherThanAPlacement()
    {
        PieceView piece = Piece("e2");
        ThrowablePiece grab = piece.GetComponent<ThrowablePiece>();
        grabber.transform.position = piece.transform.position;
        manager.SelectEnter((IXRSelectInteractor)grabber, grab);
        yield return Steps(12);
        Vector3 target = board.GetPieceWorldPosition(BoardSquare.FromAlgebraic("e3"));
        grabber.transform.position += target - Vector3.right * .1f - piece.transform.position;
        yield return Steps(12);
        float movementStartedAt = Time.time;
        for (int i = 0; i < 10; i++)
        {
            // 10 mm per 72 Hz frame is a measured .72 m/s toss. Mixing a
            // physics wait with a render wait made the old replay much slower.
            grabber.transform.position += Vector3.right * .01f;
            yield return null;
        }
        Assert.That(.1f / (Time.time - movementStartedAt), Is.InRange(.65f, .8f), "Exercise a fast input, not an unmeasured frame-count gesture.");
        Assert.That(Vector3.Distance(piece.transform.position, target), Is.LessThan(.015f));
        manager.SelectExit((IXRSelectInteractor)grabber, grab);
        yield return Steps(4);
        Assert.That(grab.ReleaseVelocity.magnitude, Is.GreaterThan(.35f), "Replay a real lateral toss through XRI's throw estimator.");
        Assert.That(game.MoveHistory.Count, Is.Zero, "Tossing sideways across a legal square must not register a chess placement.");
    }

    [UnityTest]
    public IEnumerator AHandKeepsItsPieceWhileAnotherPieceCompletesALegalMove()
    {
        PieceView held = Piece("e2");
        ThrowablePiece grab = held.GetComponent<ThrowablePiece>();
        grabber.transform.position = held.transform.position;
        manager.SelectEnter((IXRSelectInteractor)grabber, grab);
        yield return Steps(12);
        grabber.transform.position += Vector3.up * .15f;
        yield return Steps(12);
        Assert.That(grab.isSelected, Is.True);
        game.SelectPiece(Piece("g2"));
        game.SelectDestination(BoardSquare.FromAlgebraic("g3"));
        yield return Steps(2);
        Assert.That(grab.isSelected, Is.True, "The move animation cannot force the other hand to drop its piece.");
        yield return new WaitForSeconds(.55f);
        Assert.That(game.CurrentTurn, Is.EqualTo(ChessSide.Black));
        Assert.That(grab.isSelected, Is.True, "The turn change cannot take a physical piece out of its holder's hand.");
        Assert.That(board.Pieces, Does.Contain(held));
        manager.SelectExit((IXRSelectInteractor)grabber, grab);
        yield return Steps(4);
        Assert.That(grab.isSelected, Is.False);
        Assert.That(game.MoveHistory.Count, Is.EqualTo(1), "Dropping the retained physical piece is not another move.");
    }

    [UnityTest]
    public IEnumerator RestartRemovesAHeldPieceEvenWhileXriHasUnparentedIt()
    {
        PieceView held = Piece("e2");
        ThrowablePiece grab = held.GetComponent<ThrowablePiece>();
        grabber.transform.position = held.transform.position;
        manager.SelectEnter((IXRSelectInteractor)grabber, grab);
        yield return Steps(12);
        grabber.transform.position += Vector3.up * .15f;
        yield return Steps(12);
        Assert.That(grab.isSelected, Is.True);
        Assert.That(held.transform.parent, Is.Null, "Exercise XRI's real unparenting during a grab.");
        game.NewGame();
        yield return Steps(3);
        Assert.That(held == null, Is.True, "A reset must remove old physical pieces by identity, including those outside Pieces root.");
        Assert.That(grabber.hasSelection, Is.False);
        Assert.That(board.Pieces.Count, Is.EqualTo(32));
        Assert.That(game.MoveHistory.Count, Is.Zero);
    }

    [UnityTest]
    public IEnumerator UntouchedPiecesStayAtRestBeforeAndAfterResizingTheBoard()
    {
        yield return new WaitForSeconds(.5f);
        foreach (PieceView piece in board.Pieces)
            Assert.That(Vector3.Distance(piece.transform.position, board.GetPieceWorldPosition(piece.Square)), Is.LessThan(.001f), piece.name);
        board.SetVrSize(1.4f);
        yield return new WaitForSeconds(.5f);
        foreach (PieceView piece in board.Pieces)
        {
            Assert.That(Vector3.Distance(piece.transform.position, board.GetPieceWorldPosition(piece.Square)), Is.LessThan(.001f), piece.name);
            Assert.That(piece.GetComponent<Rigidbody>().IsSleeping(), Is.True, "Untouched resting pieces must not consume continuous simulation.");
        }
        Assert.That(game.MoveHistory.Count, Is.Zero);
    }

    private PieceView Piece(string square) => board.Pieces.First(p => p.Square.Equals(BoardSquare.FromAlgebraic(square)));

    private static void Place(PieceView piece, Vector3 position)
    {
        piece.transform.SetPositionAndRotation(position, Quaternion.identity);
        Rigidbody body = piece.GetComponent<Rigidbody>();
        body.position = position;
        body.rotation = Quaternion.identity;
        piece.GetComponent<ThrowablePiece>().SetRest(piece.transform.localPosition);
    }

    private static Dictionary<XRHandJointID, Vector3> OpenHandJoints()
    {
        var joints = new Dictionary<XRHandJointID, Vector3>
        {
            [XRHandJointID.Wrist] = new Vector3(0f, 0f, -.04f),
            [XRHandJointID.Palm] = Vector3.zero,
            [XRHandJointID.ThumbMetacarpal] = new Vector3(-.025f, 0f, -.015f),
            [XRHandJointID.ThumbProximal] = new Vector3(-.04f, 0f, 0f),
            [XRHandJointID.ThumbDistal] = new Vector3(-.048f, 0f, .02f),
            [XRHandJointID.ThumbTip] = new Vector3(-.055f, 0f, .04f),
        };
        void Finger(XRHandJointID proximal, XRHandJointID intermediate, XRHandJointID distal, XRHandJointID tip, float x, float length)
        {
            joints[proximal] = new Vector3(x, 0f, .04f);
            joints[intermediate] = new Vector3(x, 0f, Mathf.Lerp(.04f, length, .4f));
            joints[distal] = new Vector3(x, 0f, Mathf.Lerp(.04f, length, .75f));
            joints[tip] = new Vector3(x, 0f, length);
        }
        Finger(XRHandJointID.IndexProximal, XRHandJointID.IndexIntermediate, XRHandJointID.IndexDistal, XRHandJointID.IndexTip, -.026f, .105f);
        Finger(XRHandJointID.MiddleProximal, XRHandJointID.MiddleIntermediate, XRHandJointID.MiddleDistal, XRHandJointID.MiddleTip, 0f, .115f);
        Finger(XRHandJointID.RingProximal, XRHandJointID.RingIntermediate, XRHandJointID.RingDistal, XRHandJointID.RingTip, .021f, .105f);
        Finger(XRHandJointID.LittleProximal, XRHandJointID.LittleIntermediate, XRHandJointID.LittleDistal, XRHandJointID.LittleTip, .039f, .09f);
        return joints;
    }

    private static IEnumerator Steps(int count)
    {
        Physics.SyncTransforms();
        for (int i = 0; i < count; i++)
        {
            yield return new WaitForFixedUpdate();
            yield return null;
        }
    }
}
#endif
