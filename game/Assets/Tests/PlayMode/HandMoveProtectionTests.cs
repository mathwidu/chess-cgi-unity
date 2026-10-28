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
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;

public sealed class HandMoveProtectionTests
{
    private XRSimulatedHMD headset;
    private float previousCaptureDeltaTime;
    private GameObject replay;
    private XRInteractionManager manager;
    private XRDirectInteractor grip;
    private XRPhysicalHand hand;
    private XRHandContactBody contacts;
    private ChessGameController game;
    private PieceView held;
    private PieceView neighbour;
    private ThrowablePiece throwable;
    private Vector3 offset;
    private Vector3 start;
    private BoardView board;
    private XRDirectInteractor otherGrip;
    private XRHandContactBody otherContacts;
    private Vector3 otherOffset;
    private readonly Dictionary<XRHandJointID, Vector3> joints = new Dictionary<XRHandJointID, Vector3>();

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
        game = Object.FindFirstObjectByType<ChessGameController>();
        game.StartLocalGame();
        board = Object.FindFirstObjectByType<BoardView>();
        manager = Object.FindFirstObjectByType<XRInteractionManager>();
        held = board.Pieces.First(piece => piece.Square.Equals(BoardSquare.FromAlgebraic("e2")));
        neighbour = board.Pieces.First(piece => piece.Square.Equals(BoardSquare.FromAlgebraic("e7")));
        throwable = held.GetComponent<ThrowablePiece>();
        replay = new GameObject("Protected move replay");
        start = new Vector3(48f, 8f, 0f);
        replay.transform.position = start;
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.transform.SetParent(replay.transform, false);
        ground.transform.localPosition = Vector3.down * .05f;
        ground.transform.localScale = new Vector3(2f, .1f, 2f);
        Place(neighbour, start);
        Place(held, start + Vector3.left * .14f + Vector3.up * .06f);
        var gripObject = new GameObject("Hand selection");
        gripObject.transform.SetParent(replay.transform, false);
        SphereCollider trigger = gripObject.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = .01f;
        grip = gripObject.AddComponent<XRDirectInteractor>();
        grip.interactionManager = manager;
        var model = new GameObject("Hand visual");
        model.transform.SetParent(gripObject.transform, false);
        BuildJoints();
        MoveHand(new Vector3(-.14f, .025f, 0f));
        hand = model.AddComponent<XRPhysicalHand>();
        hand.Configure(model.transform, replay.transform, replay.transform, ReadJoint);
        contacts = replay.GetComponentInChildren<XRHandContactBody>();
        yield return Steps(8);
        Assert.That(contacts.IsContactEnabled, Is.True);
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Time.captureDeltaTime = previousCaptureDeltaTime;
        if (throwable != null && throwable.isSelected)
            manager.SelectExit((IXRSelectInteractor)grip, throwable);
        if (otherGrip != null)
            foreach (var selected in otherGrip.interactablesSelected.ToArray())
                manager.SelectExit((IXRSelectInteractor)otherGrip, selected);
        if (headset != null && headset.added) InputSystem.RemoveDevice(headset);
        Scene previous = SceneManager.GetActiveScene();
        SceneManager.SetActiveScene(SceneManager.CreateScene("Empty after protected move replay"));
        yield return SceneManager.UnloadSceneAsync(previous);
    }

    [UnityTest]
    public IEnumerator AnOccupiedHandDoesNotSweepNeighbouringPieces()
    {
        // Isolate the hand: this particular held shape already ignores pieces.
        held.GetComponent<Collider>().excludeLayers |= 1 << PieceView.PhysicsLayer;
        yield return Grab();
        Vector3 initial = neighbour.GetComponent<Rigidbody>().position;
        yield return Sweep(.003f, 50);
        Assert.That(Vector3.Distance(neighbour.GetComponent<Rigidbody>().position, initial), Is.LessThan(.002f),
            "Selecting a piece must protect the other pieces from the occupied hand.");
        Assert.That(game.MoveHistory.Count, Is.Zero);
        Assert.That(grip.IsSelecting(throwable), Is.True);
    }

    [UnityTest]
    public IEnumerator TheHeldPieceDoesNotPushNeighboursAndRegainsCollisionsAfterRelease()
    {
        hand.enabled = false; // Isolate the held piece from all hand contacts.
        MoveHand(new Vector3(-.14f, 0f, 0f));
        Place(held, start + Vector3.left * .14f);
        yield return Grab();
        Vector3 initial = neighbour.GetComponent<Rigidbody>().position;
        yield return Sweep(.003f, 50);
        Assert.That(Vector3.Distance(neighbour.GetComponent<Rigidbody>().position, initial), Is.LessThan(.002f),
            "Transporting a selected piece through the row must not bowl over its neighbours.");
        manager.SelectExit((IXRSelectInteractor)grip, throwable);
        yield return Steps(3);
        Rigidbody free = held.GetComponent<Rigidbody>();
        free.position = start + Vector3.left * .065f + Vector3.up * .01f;
        free.rotation = Quaternion.identity;
        free.useGravity = false; // Isolate piece contacts from floor friction.
        free.linearVelocity = Vector3.zero;
        free.angularVelocity = Vector3.zero;
        yield return Steps(4);
        TestContext.WriteLine($"before impact selected={throwable.isSelected}; kine={free.isKinematic}; mask={held.GetComponent<Collider>().excludeLayers.value}; piece={free.position:F4}; neighbour={neighbour.GetComponent<Rigidbody>().position:F4}; collider={held.GetComponent<Collider>().bounds}");
        free.linearVelocity = Vector3.right * .6f;
        yield return Steps(14);
        TestContext.WriteLine($"after impact mask={held.GetComponent<Collider>().excludeLayers.value}; piece={free.position:F4}; v={free.linearVelocity:F4}; neighbour={neighbour.GetComponent<Rigidbody>().position:F4}; nv={neighbour.GetComponent<Rigidbody>().linearVelocity:F4}");
        Assert.That(free.position.x, Is.LessThan(start.x - .01f),
            "After release, the free piece must hit the neighbour instead of passing through the row.");
        Assert.That(free.linearVelocity.x, Is.LessThan(.1f),
            "The real collision must stop or rebound its incoming motion; the neighbour may resist through floor friction.");
        Assert.That(game.MoveHistory.Count, Is.Zero);
    }

    [UnityTest]
    public IEnumerator OpeningInsideANeighbourWaitsForSeparationAndTheOpenHandCanPushAgain()
    {
        held.GetComponent<Collider>().excludeLayers |= 1 << PieceView.PhysicsLayer;
        yield return Grab();
        yield return Sweep(.003f, 47);
        Vector3 initial = neighbour.GetComponent<Rigidbody>().position;
        manager.SelectExit((IXRSelectInteractor)grip, throwable);
        yield return Steps(5);
        Assert.That(Vector3.Distance(neighbour.GetComponent<Rigidbody>().position, initial), Is.LessThan(.002f),
            "Opening the pinch while the hand intersects a neighbour cannot reactivate an explosive overlap.");
        Assert.That(neighbour.GetComponent<Rigidbody>().linearVelocity.magnitude, Is.LessThan(.04f));
        // Put the released piece out of this brush replay, then withdraw and return.
        Place(held, start + Vector3.up * .3f + Vector3.left * .5f);
        yield return Sweep(-.003f, 47);
        yield return Steps(4);
        yield return Sweep(.003f, 50);
        Assert.That(Vector3.Distance(neighbour.GetComponent<Rigidbody>().position, initial), Is.GreaterThan(.015f),
            "The open hand must become physical again after it clears the released overlap.");
        Assert.That(game.MoveHistory.Count, Is.Zero);
    }

    [UnityTest]
    public IEnumerator TheOtherPalmCanSupportThePieceAfterTheLastPinchOpens()
    {
        yield return Grab();
        CreateOtherHand(new Vector3(.3f, .14f, 0f));
        yield return Steps(8);
        Vector3 otherPalm = start + otherOffset;
        grip.transform.position += otherPalm + Vector3.up * .06f - held.transform.position;
        yield return new WaitForSeconds(.3f);
        manager.SelectExit((IXRSelectInteractor)grip, throwable);
        yield return Steps(45);
        Assert.That(grip.hasSelection, Is.False);
        Assert.That(throwable.IsSupportedByHand, Is.True);
        Assert.That(held.GetComponent<Rigidbody>().position.y, Is.GreaterThan(otherPalm.y + .01f),
            "After release, the other palm must catch the actual piece instead of staying disabled.");
    }

    [UnityTest]
    public IEnumerator TheFreeHandCannotSweepTheRowWhileTheOtherHandHoldsAPiece()
    {
        CreateOtherHand(new Vector3(-.14f, .025f, 0f));
        yield return Steps(8);
        yield return Grab();
        Vector3 initial = neighbour.GetComponent<Rigidbody>().position;
        for (int i = 0; i < 50; i++)
        {
            otherOffset += Vector3.right * .003f;
            yield return new WaitForFixedUpdate();
            yield return null;
        }
        Assert.That(Vector3.Distance(neighbour.GetComponent<Rigidbody>().position, initial), Is.LessThan(.002f),
            "A selection protects the row from both hands, including the hand without a local selection.");
        Assert.That(grip.IsSelecting(throwable), Is.True);
        Assert.That(game.MoveHistory.Count, Is.Zero);
    }

    [UnityTest]
    public IEnumerator ReleasingOneOfTwoHeldPiecesKeepsProtectionUntilTheLastOneIsRemoved()
    {
        CreateOtherHand(new Vector3(.4f, .1f, 0f), true);
        yield return Steps(8);
        yield return Grab();
        PieceView second = board.Pieces.First(piece => piece.Square.Equals(BoardSquare.FromAlgebraic("f2")));
        Place(second, start + otherOffset + Vector3.up * .05f);
        ThrowablePiece secondGrab = second.GetComponent<ThrowablePiece>();
        otherGrip.transform.position = second.GetComponent<Collider>().bounds.center;
        manager.SelectEnter((IXRSelectInteractor)otherGrip, secondGrab);
        yield return Steps(4);
        Assert.That(otherGrip.IsSelecting(secondGrab), Is.True);
        manager.SelectExit((IXRSelectInteractor)grip, throwable);
        yield return Steps(4);
        Assert.That(contacts.IsContactEnabled, Is.False,
            "The hand that opened must stay neutral while the second piece is still selected.");
        second.gameObject.SetActive(false);
        yield return Steps(4);
        Assert.That(otherGrip.hasSelection, Is.False);
        Assert.That(contacts.IsContactEnabled, Is.True,
            "Disabling or resetting the last held object must not leave the other hand permanently blocked.");
        second.gameObject.SetActive(true);
        yield return Steps(3);
        otherGrip.transform.position = second.GetComponent<Collider>().bounds.center;
        manager.SelectEnter((IXRSelectInteractor)otherGrip, secondGrab);
        yield return Steps(3);
        Assert.That(otherGrip.IsSelecting(secondGrab), Is.True);
        Assert.That(contacts.IsContactEnabled, Is.False,
            "A re-enabled object grabbed again must enter shared protection again.");
        manager.SelectExit((IXRSelectInteractor)otherGrip, secondGrab);
        yield return Steps(3);
        Assert.That(contacts.IsContactEnabled, Is.True);
    }

    [UnityTest]
    public IEnumerator ReleasingATallEnlargedPieceCannotRestoreContactsThroughItsTip()
    {
        hand.enabled = false;
        held = board.Pieces.First(piece => piece.Square.Equals(BoardSquare.FromAlgebraic("e1")));
        throwable = held.GetComponent<ThrowablePiece>();
        held.transform.localScale *= 1.5f;
        Place(held, start + Vector3.up * .15f);
        yield return Grab();
        yield return new WaitForSeconds(.3f);
        Collider shape = held.GetComponent<Collider>();
        Rigidbody body = held.GetComponent<Rigidbody>();
        float height = shape.bounds.max.y - held.transform.position.y;
        Place(neighbour, body.position + Vector3.up * (height - .003f));
        Rigidbody other = neighbour.GetComponent<Rigidbody>();
        other.useGravity = false;
        other.WakeUp();
        Physics.SyncTransforms();
        Assert.That(Physics.ComputePenetration(shape, body.position, body.rotation, neighbour.GetComponent<Collider>(),
            other.position, other.rotation, out _, out float depth), Is.True);
        Assert.That(depth, Is.GreaterThan(.001f), "Exercise an actual overlap at the tip of the enlarged king.");
        Vector3 initial = other.position;
        manager.SelectExit((IXRSelectInteractor)grip, throwable);
        body.useGravity = false;
        yield return Steps(4);
        Assert.That(Vector3.Distance(other.position, initial), Is.LessThan(.002f),
            "Clearance must cover the whole tall collider, including the end far from its base pivot.");
        Assert.That(other.linearVelocity.magnitude, Is.LessThan(.04f));
        body.position += Vector3.left * .2f;
        yield return Steps(4);
        Assert.That(shape.excludeLayers.value & (1 << PieceView.PhysicsLayer), Is.Zero,
            "The released tall piece must regain piece contacts once its entire shape is clear.");
    }

    [UnityTest]
    public IEnumerator OpeningOverItsOwnPalmRestoresSupportWhileAnOverlappingThumbWaits()
    {
        yield return Grab();
        MoveHand(offset + Vector3.up * .1f);
        yield return Steps(8);
        XRHandJointID[] thumb = { XRHandJointID.ThumbMetacarpal, XRHandJointID.ThumbProximal,
            XRHandJointID.ThumbDistal, XRHandJointID.ThumbTip };
        // Move an already armed thumb through the held piece while contacts are
        // suppressed. The palm itself remains below, with room to catch it.
        for (int i = 0; i < 10; i++)
        {
            foreach (XRHandJointID id in thumb) joints[id] += new Vector3(.002f, .004f, 0f);
            yield return new WaitForFixedUpdate();
            yield return null;
        }
        yield return new WaitForSeconds(.3f);
        Collider pieceShape = held.GetComponent<Collider>();
        Assert.That(contacts.GetComponentsInChildren<CapsuleCollider>().Any(shape => shape.enabled &&
            Physics.ComputePenetration(shape, shape.transform.position, shape.transform.rotation, pieceShape,
                held.GetComponent<Rigidbody>().position, held.GetComponent<Rigidbody>().rotation, out _, out _)), Is.True,
            "The release must really contain an overlapping tracked finger, not just an unobstructed palm.");
        manager.SelectExit((IXRSelectInteractor)grip, throwable);
        yield return Steps(35);
        Assert.That(throwable.IsSupportedByHand, Is.True,
            "A thumb still overlapping the dropped piece cannot keep the clear palm disabled.");
        Assert.That(held.GetComponent<Rigidbody>().position.y, Is.GreaterThan(start.y + offset.y + .01f));
        Assert.That(game.MoveHistory.Count, Is.Zero);
    }

    private IEnumerator Grab()
    {
        grip.transform.position = held.GetComponent<Collider>().bounds.center;
        manager.SelectEnter((IXRSelectInteractor)grip, throwable);
        yield return Steps(8);
        Assert.That(grip.IsSelecting(throwable), Is.True, "Acquire the real factory piece through XRI selection.");
    }

    private void CreateOtherHand(Vector3 initial, bool selectable = false)
    {
        var otherRoot = new GameObject("Other physical hand");
        otherRoot.transform.SetParent(replay.transform, false);
        if (selectable)
        {
            SphereCollider trigger = otherRoot.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = .01f;
            otherGrip = otherRoot.AddComponent<XRDirectInteractor>();
            otherGrip.interactionManager = manager;
        }
        var model = new GameObject("Other hand visual");
        model.transform.SetParent(otherRoot.transform, false);
        otherOffset = initial;
        bool ReadOther(XRHandJointID id, out Pose pose)
        {
            pose = default;
            if (!joints.TryGetValue(id, out Vector3 point)) return false;
            pose = new Pose(start + otherOffset + point, Quaternion.identity);
            return true;
        }
        XRPhysicalHand freeHand = model.AddComponent<XRPhysicalHand>();
        freeHand.Configure(model.transform, otherRoot.transform, replay.transform, ReadOther);
        otherContacts = otherRoot.GetComponentInChildren<XRHandContactBody>();
    }

    private IEnumerator Sweep(float step, int count)
    {
        for (int i = 0; i < count; i++)
        {
            MoveHand(offset + Vector3.right * step);
            yield return new WaitForFixedUpdate();
            yield return null;
        }
    }

    private void MoveHand(Vector3 value)
    {
        Vector3 delta = value - offset;
        offset = value;
        grip.transform.position += delta;
    }

    private static void Place(PieceView piece, Vector3 position)
    {
        piece.transform.position = position;
        piece.transform.rotation = Quaternion.identity;
        Rigidbody body = piece.GetComponent<Rigidbody>();
        body.position = position;
        body.rotation = Quaternion.identity;
        piece.GetComponent<ThrowablePiece>().SetRest(piece.transform.localPosition);
    }

    private bool ReadJoint(XRHandJointID id, out Pose pose)
    {
        pose = default;
        if (!joints.TryGetValue(id, out Vector3 point)) return false;
        pose = new Pose(start + offset + point, Quaternion.identity);
        return true;
    }

    private void BuildJoints()
    {
        joints.Clear();
        joints[XRHandJointID.Wrist] = Vector3.back * .04f;
        joints[XRHandJointID.Palm] = Vector3.zero;
        Finger(XRHandJointID.IndexProximal, XRHandJointID.IndexIntermediate, XRHandJointID.IndexDistal, XRHandJointID.IndexTip, -.026f, .105f);
        Finger(XRHandJointID.MiddleProximal, XRHandJointID.MiddleIntermediate, XRHandJointID.MiddleDistal, XRHandJointID.MiddleTip, 0f, .115f);
        Finger(XRHandJointID.RingProximal, XRHandJointID.RingIntermediate, XRHandJointID.RingDistal, XRHandJointID.RingTip, .021f, .105f);
        Finger(XRHandJointID.LittleProximal, XRHandJointID.LittleIntermediate, XRHandJointID.LittleDistal, XRHandJointID.LittleTip, .039f, .09f);
        joints[XRHandJointID.ThumbMetacarpal] = new Vector3(-.025f, 0f, -.015f);
        joints[XRHandJointID.ThumbProximal] = new Vector3(-.04f, 0f, 0f);
        joints[XRHandJointID.ThumbDistal] = new Vector3(-.048f, 0f, .02f);
        joints[XRHandJointID.ThumbTip] = new Vector3(-.055f, 0f, .04f);
    }

    private void Finger(XRHandJointID proximal, XRHandJointID intermediate, XRHandJointID distal, XRHandJointID tip, float x, float length)
    {
        joints[proximal] = new Vector3(x, 0f, .04f);
        joints[intermediate] = new Vector3(x, 0f, Mathf.Lerp(.04f, length, .4f));
        joints[distal] = new Vector3(x, 0f, Mathf.Lerp(.04f, length, .75f));
        joints[tip] = new Vector3(x, 0f, length);
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
