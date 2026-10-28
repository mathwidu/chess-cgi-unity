#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.XR;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public sealed class HandPlacementFeelTests
{
    private XRSimulatedHMD headset;
    private MetaAimHand aim;
    private MetaAimHand previousLeftAim;
    private NearFarInteractor near;
    private BoardView board;
    private ChessGameController game;
    private PieceView pawn;
    private ThrowablePiece grab;
    private Quaternion upright;
    private float previousCaptureDeltaTime;
    private HandPlacementPoseReplay poseReplay;
    private readonly Dictionary<XRHandJointID, Vector3> supportJoints = new Dictionary<XRHandJointID, Vector3>();
    private readonly BoardSquare destination = BoardSquare.FromAlgebraic("e3");

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        previousCaptureDeltaTime = Time.captureDeltaTime;
        // Replay at the headset's 72 Hz. With an uncapped batch-mode frame rate,
        // a pose step written after Dynamic can otherwise reach XRI in a frame
        // with a much smaller delta, inventing an exaggerated throw velocity.
        Time.captureDeltaTime = 1f / 72f;
        headset = InputSystem.AddDevice<XRSimulatedHMD>();
        EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/Main.unity", new LoadSceneParameters(LoadSceneMode.Single));
        yield return null;
        yield return null;
        if (Object.FindFirstObjectByType<XRRig>() == null)
            new GameObject("Natural placement replay bootstrap").AddComponent<XRRig>();
        yield return null;
        yield return null;

        previousLeftAim = MetaAimHand.left;
        aim = MetaAimHand.CreateHand(InputDeviceCharacteristics.Left);
        Assert.That(aim, Is.Not.Null);
        MetaAimHand.left = aim;
        Transform offset = XRRig.Origin.Find("Camera Offset");
        offset.GetComponent<XRInputModalityManager>().enabled = false;
        foreach (string name in new[] { "LeftHandInteractor", "RightHandInteractor", "Left Controller", "Right Controller" })
            offset.Find(name).gameObject.SetActive(false);
        GameObject hand = offset.Find("LeftHandInteractor").gameObject;
        foreach (TrackedPoseDriver driver in hand.GetComponentsInChildren<TrackedPoseDriver>(true))
            driver.enabled = false;
        foreach (XRPokeInteractor poke in hand.GetComponentsInChildren<XRPokeInteractor>(true))
            poke.enabled = false;
        near = hand.GetComponentInChildren<NearFarInteractor>(true);
        poseReplay = hand.AddComponent<HandPlacementPoseReplay>();
        poseReplay.Source = near.interactionAttachController.transformToFollow;
        near.enableFarCasting = false;
        near.enableUIInteraction = false;
        game = Object.FindFirstObjectByType<ChessGameController>();
        board = Object.FindFirstObjectByType<BoardView>();
        game.StartLocalGame();
        yield return null;
        pawn = board.Pieces.First(piece => piece.Square.Equals(BoardSquare.FromAlgebraic("e2")));
        grab = pawn.GetComponent<ThrowablePiece>();
        upright = pawn.transform.rotation;
        near.nearInteractionCaster.castOrigin.position = grab.colliders[0].bounds.center;
        QueuePinch(0f);
        hand.SetActive(true);
        yield return PhysicsSteps(3);
        QueuePinch(.95f);
        yield return PhysicsSteps(3);
        Assert.That(near.IsSelecting(grab), Is.True, "Acquire through MetaAim and the original near input before testing placement.");
        yield return new WaitForSeconds(.2f);
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Time.captureDeltaTime = previousCaptureDeltaTime;
        if (aim != null && aim.added) InputSystem.RemoveDevice(aim);
        if (MetaAimHand.left == aim) MetaAimHand.left = previousLeftAim;
        if (headset != null && headset.added) InputSystem.RemoveDevice(headset);
        Scene previous = SceneManager.GetActiveScene();
        SceneManager.SetActiveScene(SceneManager.CreateScene("Empty after natural placement test"));
        yield return SceneManager.UnloadSceneAsync(previous);
    }

    [UnityTest]
    public IEnumerator AHandPlacementSettlesWithoutAVisualArcAndRecordsOneMove()
    {
        yield return MoveNativeGripToBoardSquare(destination);
        HandPlacementLandingRenderProbe render = CreateLandingRenderProbe(destination);
        QueuePinch(.1f);
        yield return PhysicsSteps(3);
        Assert.That(grab.isSelected, Is.False, "Release through the actual near-hand input.");
        yield return new WaitForSeconds(.7f);

        Assert.That(game.MoveHistory.Count, Is.EqualTo(1));
        Assert.That(pawn.Square, Is.EqualTo(destination));
        AssertLandingWithoutArc(render);
    }

    [UnityTest]
    public IEnumerator AHandCaptureSettlesWithoutAVisualArcAndRecordsOneMove() =>
        CaptureFromHeight(0f);

    [UnityTest]
    public IEnumerator OpeningAboveAnOccupiedGreenSquareStillCapturesAndRecordsOneMove() =>
        CaptureFromHeight(.3f);

    private IEnumerator CaptureFromHeight(float height)
    {
        QueuePinch(.1f);
        yield return PhysicsSteps(3);
        yield return PlayFixtureMove("e2", "e4");
        yield return PlayFixtureMove("d7", "d5");
        yield return AcquireNativePiece("e4");
        BoardSquare capture = BoardSquare.FromAlgebraic("d5");
        int previousMoves = game.MoveHistory.Count;
        int previousCaptures = game.CapturedPieces.Count;
        yield return MoveNativeGripToBoardSquare(capture);
        if (height > 0f)
        {
            yield return MoveNativePinch(board.transform.up * height, .7f);
            yield return new WaitForSeconds(.08f);
        }
        HandPlacementLandingRenderProbe render = CreateLandingRenderProbe(capture);
        QueuePinch(.1f);
        yield return PhysicsSteps(3);
        Assert.That(grab.isSelected, Is.False);
        yield return new WaitForSeconds(1.5f);

        Assert.That(game.MoveHistory.Count, Is.EqualTo(previousMoves + 1), "A physical capture records exactly one chess move.");
        Assert.That(game.CapturedPieces.Count, Is.EqualTo(previousCaptures + 1));
        Assert.That(pawn.Square, Is.EqualTo(capture));
        AssertLandingWithoutArc(render);
    }

    [UnityTest]
    public IEnumerator AHandPromotionStaysOnItsSquareWhileWaitingForTheChoiceAndCommitsOnce() =>
        PlaceAndPromote(.4f);

    [UnityTest]
    public IEnumerator ChoosingPromotionImmediatelyDoesNotRestartTheOldPlacementAnimation() =>
        PlaceAndPromote(0f);

    [UnityTest]
    public IEnumerator CancelingPhysicalPromotionReturnsThePawnWithoutMakingAMove() =>
        PlaceAndPromote(0f, true);

    private IEnumerator PlaceAndPromote(float waitBeforeChoice, bool cancel = false)
    {
        QueuePinch(.1f);
        yield return PhysicsSteps(3);
        foreach (string move in new[] { "a2a4", "h7h5", "a4a5", "h5h4", "a5a6", "h4h3", "a6b7", "h3g2" })
            yield return PlayFixtureMove(move.Substring(0, 2), move.Substring(2, 2));
        yield return AcquireNativePiece("b7");
        BoardSquare promotion = BoardSquare.FromAlgebraic("a8");
        int previousMoves = game.MoveHistory.Count;
        int previousCaptures = game.CapturedPieces.Count;
        yield return MoveNativeGripToBoardSquare(promotion);
        HandPlacementLandingRenderProbe render = CreateLandingRenderProbe(promotion);
        QueuePinch(.1f);
        yield return PhysicsSteps(3);
        Assert.That(grab.isSelected, Is.False);
        if (waitBeforeChoice > 0f) yield return new WaitForSeconds(waitBeforeChoice);

        Assert.That(game.IsAwaitingPromotion, Is.True, "The real physical release must ask for the promotion choice.");
        Assert.That(game.MoveHistory.Count, Is.EqualTo(previousMoves), "The pending choice cannot create a duplicate or premature move.");
        if (cancel)
        {
            game.CancelSelection();
            yield return new WaitForSeconds(.5f);
            Assert.That(game.IsAwaitingPromotion, Is.False);
            Assert.That(game.SelectedPiece, Is.Null);
            Assert.That(game.MoveHistory.Count, Is.EqualTo(previousMoves));
            Assert.That(game.CapturedPieces.Count, Is.EqualTo(previousCaptures));
            BoardSquare origin = BoardSquare.FromAlgebraic("b7");
            Assert.That(pawn.Square, Is.EqualTo(origin));
            Assert.That(Vector3.Distance(pawn.transform.position, board.GetPieceWorldPosition(origin)), Is.LessThan(.003f),
                "Canceling the uncommitted promotion must restore the real pawn to its logical square.");
            Assert.That(pawn.GetComponent<Rigidbody>().isKinematic, Is.False,
                "The returned pawn must support hand contact again after canceling the choice.");
            yield break;
        }
        game.ChoosePromotion('Q');
        yield return new WaitForSeconds(.7f);
        Assert.That(game.MoveHistory.Count, Is.EqualTo(previousMoves + 1));
        Assert.That(game.CapturedPieces.Count, Is.EqualTo(previousCaptures + 1));
        PieceView promoted = board.Pieces.First(piece => piece.Square.Equals(promotion));
        Assert.That(promoted.Kind, Is.EqualTo(ChessPieceKind.Queen));
        Assert.That(game.IsAwaitingPromotion, Is.False);
        AssertLandingWithoutArc(render);
    }

    [UnityTest]
    public IEnumerator NativeQueenGrabPreservesPhysicalWorldScaleDuringSelectEntered()
    {
        yield return PrepareBlackQueenAtNativeGrip();
        Vector3 restingScale = pawn.transform.lossyScale;
        Vector3 localGrip = pawn.transform.InverseTransformPoint(poseReplay.Source.position);
        Vector3 callbackScale = Vector3.zero;
        float callbackGripError = float.PositiveInfinity;
        bool entered = false;
        grab.selectEntered.AddListener(_ =>
        {
            entered = true;
            callbackScale = pawn.transform.lossyScale;
            callbackGripError = Vector3.Distance(pawn.transform.TransformPoint(localGrip), poseReplay.Source.position);
        });

        QueuePinch(.95f);
        yield return PhysicsSteps(3);
        Assert.That(near.IsSelecting(grab), Is.True, "Acquire the real queen through native near selection.");
        Assert.That(entered, Is.True);
        TestContext.WriteLine($"Queen native grab: restingScale={restingScale:F5}; selectEnteredScale={callbackScale:F5}; stableScale={pawn.transform.lossyScale:F5}; targetScale={grab.GetTargetLocalScale():F5}; callbackGripError={callbackGripError:F5}");
        Assert.That(callbackScale.x / restingScale.x, Is.InRange(.99f, 1.08f),
            "A selection highlight may enlarge the queen slightly, but XRI reparenting must never turn a board-scaled piece into a metre-scale collider.");
        Assert.That(callbackGripError, Is.LessThan(.004f), "Selecting must preserve the actual point between the fingers, including inside selectEntered.");
    }

    [UnityTest]
    public IEnumerator NativeQueenGripFollowsTheHandToAnotherSquareWithoutRootFeedback()
    {
        yield return PrepareBlackQueenAtNativeGrip();
        BoardSquare targetSquare = BoardSquare.FromAlgebraic("d7");
        Vector3 originalRoot = pawn.transform.position;
        Vector3 restingScale = pawn.transform.lossyScale;
        Vector3 initialPinch = poseReplay.Source.position;
        Vector3 localGrip = pawn.transform.InverseTransformPoint(initialPinch);
        Vector3 displacement = board.GetPieceWorldPosition(targetSquare) - board.GetPieceWorldPosition(pawn.Square);
        QueuePinch(.95f);
        yield return PhysicsSteps(3);
        Assert.That(near.IsSelecting(grab), Is.True);
        yield return new WaitForSeconds(.2f);

        // These commands are fixed from the pre-grab anatomical point. Never
        // compensate input from the queen's current root as SetHeldPose does.
        yield return MoveNativePinch(board.transform.up * .05f, .4f);
        Quaternion originalInputRotation = poseReplay.Source.rotation;
        poseReplay.Source.rotation = Quaternion.AngleAxis(25f, board.transform.forward) * originalInputRotation;
        yield return PhysicsSteps(3);
        Assert.That(Vector3.Distance(pawn.transform.TransformPoint(localGrip), poseReplay.Source.position), Is.LessThan(.004f),
            "Tilting must rotate the queen around the physical point between the fingers, not detach its base from that point.");
        poseReplay.Source.rotation = originalInputRotation;
        yield return PhysicsSteps(3);
        yield return MoveNativePinch(displacement, .45f);
        yield return MoveNativePinch(-board.transform.up * .05f, .4f);
        yield return new WaitForSeconds(.08f);

        float gripError = Vector3.Distance(pawn.transform.TransformPoint(localGrip), poseReplay.Source.position);
        TestContext.WriteLine($"Queen hand-only replay: inputDelta={poseReplay.Source.position - initialPinch:F5}; rootDelta={pawn.transform.position - originalRoot:F5}; gripError={gripError:F5}; worldScale={pawn.transform.lossyScale:F5}");
        Assert.That(gripError, Is.LessThan(.004f), "The same physical grip point must remain between the fingers throughout a hand-only move.");
        Assert.That(pawn.transform.lossyScale.x / restingScale.x, Is.InRange(.99f, 1.08f));
        Assert.That(Vector3.Distance(pawn.transform.position, originalRoot + displacement), Is.LessThan(.004f),
            "The queen's base must actually follow the commanded hand displacement rather than remain at d8.");
        Assert.That(board.TryGetSquareAt(pawn.transform.position, out BoardSquare actual), Is.True);
        Assert.That(actual, Is.EqualTo(targetSquare));
        Assert.That(game.MoveHistory.Count, Is.EqualTo(3), "Moving a held queen must not score before the fingers open.");

        QueuePinch(.1f);
        yield return PhysicsSteps(3);
        Assert.That(grab.isSelected, Is.False);
        yield return new WaitForSeconds(.65f);
        Assert.That(game.MoveHistory.Count, Is.EqualTo(4), "A legal hand-only transfer must commit where the queen's physical base was placed.");
        Assert.That(pawn.Square, Is.EqualTo(targetSquare));
    }

    [UnityTest]
    public IEnumerator AStationaryTiltedPieceWithItsBaseNearTheBoardCanBePlaced()
    {
        Vector3 target = board.GetPieceWorldPosition(destination);
        SetHeldPose(target + board.transform.up * .03f,
            Quaternion.AngleAxis(60f, board.transform.forward) * upright);
        yield return new WaitForSeconds(grab.throwSmoothingDuration + .08f);
        AssertOverDestination();
        float rootGap = Vector3.Dot(pawn.transform.position - target, board.transform.up);
        float baseGap = LowestBaseHeight(target);
        Assert.That(rootGap, Is.GreaterThan(.025f), "Exercise the current root-only rejection band.");
        Assert.That(baseGap, Is.InRange(0f, .02f), "The actual base is already close enough to the target plane.");

        QueuePinch(.45f);
        yield return PhysicsSteps(3);
        Assert.That(grab.isSelected, Is.False, "The fingers must really open before evaluating placement.");
        yield return new WaitForSeconds(.65f);
        AssertOneMove("A tilted root must not veto a stationary base already close to a legal square.");
    }

    [UnityTest]
    public IEnumerator LoweringGentlyAndOpeningNearTheBoardPlacesWithoutAQuarterSecondPause()
    {
        Vector3 end = board.GetPieceWorldPosition(destination) + board.transform.up * .008f;
        Vector3 start = end + board.transform.up * .09f;
        SetHeldPose(start, upright);
        yield return new WaitForSeconds(grab.throwSmoothingDuration + .08f);
        yield return MoveHeld(start, end, .3f);
        Assert.That(poseReplay.MaximumStepSpeed, Is.InRange(.28f, .32f), "Replay a measured gentle hand descent, not a pose teleport.");
        AssertOverDestination();
        QueuePinch(.45f);
        yield return PhysicsSteps(3);
        Assert.That(grab.HasReleaseVelocity, Is.True);
        Assert.That(Vector3.Dot(grab.ReleaseVelocity, board.transform.up), Is.LessThan(-.35f),
            "The real XRI estimate must contain the gentle descent amplified by its throw gain.");
        Assert.That(grab.IsSupportedByHand, Is.False);
        yield return new WaitForSeconds(.65f);
        AssertOneMove("A gentle downward placement must not require waiting for the full throw-history window.");
    }

    [UnityTest]
    public IEnumerator StoppingBrieflyAtALegalSquarePlacesDespiteOlderThrowHistory()
    {
        Vector3 end = board.GetPieceWorldPosition(destination) + board.transform.up * .008f;
        Vector3 start = end - board.transform.right * .154f;
        SetHeldPose(start, upright);
        yield return new WaitForSeconds(grab.throwSmoothingDuration + .08f);
        yield return MoveHeld(start, end, .22f);
        Assert.That(poseReplay.MaximumStepSpeed, Is.InRange(.68f, .72f), "Measure the input trajectory before testing old throw history.");
        Vector3 stopped = pawn.transform.position;
        yield return new WaitForSeconds(.04f);
        Assert.That(Vector3.Distance(pawn.transform.position, stopped), Is.LessThan(.003f), "The held piece must already have stopped.");
        AssertOverDestination();
        QueuePinch(.45f);
        yield return PhysicsSteps(3);
        Assert.That(grab.ReleaseVelocity.magnitude, Is.GreaterThan(.35f), "Exercise a remaining old-motion estimate rather than a fresh toss.");
        yield return new WaitForSeconds(.65f);
        AssertOneMove("Briefly stopping and opening at the destination should be enough to place the piece.");
    }

    [UnityTest]
    public IEnumerator OpeningThreeCentimetersAboveALegalSquareWhileLoweringWaitsForTheBoardAndPlaces()
    {
        Vector3 target = board.GetPieceWorldPosition(destination);
        Vector3 end = target + board.transform.up * .03f;
        Vector3 start = end + board.transform.up * .06f;
        SetHeldPose(start, upright);
        yield return new WaitForSeconds(grab.throwSmoothingDuration + .08f);
        yield return MoveHeld(start, end, .3f);
        Assert.That(poseReplay.MaximumStepSpeed, Is.InRange(.18f, .22f));
        AssertOverDestination();
        Assert.That(LowestBaseHeight(target), Is.InRange(.028f, .032f), "Open a small real gap above the legal destination.");
        HandPlacementContactProbe contacts = pawn.gameObject.AddComponent<HandPlacementContactProbe>();

        QueuePinch(.1f);
        yield return PhysicsSteps(1);
        Assert.That(grab.isSelected, Is.False, "The native pinch must release before any assisted move.");
        Assert.That(game.MoveHistory.Count, Is.Zero, "A nearby release must still reach the board instead of scoring while airborne.");
        yield return new WaitForSeconds(.7f);
        Assert.That(contacts.SawBoardSupport, Is.True, "The released factory piece must really land on the board.");
        AssertOneMove("Opening 3 cm above a legal square during a gentle descent should settle into a placement.");
    }

    [UnityTest]
    public IEnumerator OpeningFiveCentimetersAboveTheGreenSquareCommitsOnlyAfterLanding() =>
        ReleaseAboveLegalSquare(.05f);

    [UnityTest]
    public IEnumerator OpeningNineCentimetersAboveTheGreenSquareCommitsOnlyAfterLanding() =>
        ReleaseAboveLegalSquare(.09f);

    [UnityTest]
    public IEnumerator OpeningTwentyCentimetersAboveTheGreenSquareCommitsOnlyAfterLanding() =>
        ReleaseAboveLegalSquare(.20f);

    [UnityTest]
    public IEnumerator OpeningFiftyCentimetersAboveTheGreenSquareCommitsOnlyAfterLanding() =>
        ReleaseAboveLegalSquare(.50f);

    [UnityTest]
    public IEnumerator OpeningOneMeterTwentyAboveTheGreenSquareCommitsOnlyAfterLanding() =>
        ReleaseAboveLegalSquare(1.20f);

    [UnityTest]
    public IEnumerator OpeningWithASmallSidewaysHandMotionStillPlacesOnTheGreenSquare()
    {
        Vector3 squareCenter = board.GetPieceWorldPosition(destination);
        float squareWidth = Vector3.Distance(squareCenter, board.GetPieceWorldPosition(BoardSquare.FromAlgebraic("f3")));
        Vector3 target = squareCenter + board.transform.right * (squareWidth * .5f - .015f) + board.transform.up * .08f;
        Vector3 start = target - board.transform.right * .05f;
        SetHeldPose(start, upright);
        yield return new WaitForSeconds(grab.throwSmoothingDuration + .08f);
        yield return MoveHeld(start, target, .25f);
        AssertOverDestination();
        Assert.That(board.GetComponent<BoardPlacementPreview>().IsLegal, Is.True);
        HandPlacementContactProbe contacts = pawn.gameObject.AddComponent<HandPlacementContactProbe>();
        Vector3 releasedPosition = Vector3.zero;
        grab.selectExited.AddListener(_ => releasedPosition = pawn.transform.position);
        poseReplay.MoveAt(board.transform.right * .2f);
        QueuePinch(.1f);
        for (int frame = 0; frame < 12 && near.IsSelecting(grab); frame++) yield return null;
        poseReplay.Stop();
        yield return PhysicsSteps(1);
        Assert.That(grab.isSelected, Is.False);
        Assert.That(board.TryGetSquareAt(releasedPosition, out BoardSquare releasedOver), Is.True);
        Assert.That(releasedOver, Is.EqualTo(destination), "The fingers really open over the green square.");
        Assert.That(game.MoveHistory.Count, Is.Zero, "A gentle opening still waits for physical board contact.");
        yield return new WaitForSeconds(1.2f);
        Assert.That(contacts.SawBoardSupport, Is.True);
        AssertOneMove("Small sideways motion during opening must not slide the intended placement into a refused square.");
    }

    private IEnumerator ReleaseAboveLegalSquare(float gap)
    {
        Vector3 target = board.GetPieceWorldPosition(destination);
        Vector3 delta = target + board.transform.up * gap - pawn.transform.position;
        yield return MoveNativePinch(board.transform.up * .15f, .6f);
        yield return MoveNativePinch(delta - board.transform.up * .15f, .7f);
        yield return new WaitForSeconds(.08f);
        AssertOverDestination();
        Assert.That(LowestBaseHeight(target), Is.InRange(gap - .003f, gap + .003f));
        Assert.That(board.GetComponent<BoardPlacementPreview>().IsLegal, Is.True,
            "Reproduce releasing a real held piece above the green legal target.");
        HandPlacementContactProbe contacts = pawn.gameObject.AddComponent<HandPlacementContactProbe>();

        QueuePinch(.1f);
        yield return PhysicsSteps(1);
        Assert.That(grab.isSelected, Is.False);
        Assert.That(game.MoveHistory.Count, Is.Zero, "Opening must not score an airborne piece.");
        yield return new WaitForSeconds(1.5f);
        Assert.That(contacts.SawBoardSupport, Is.True, "The released piece actually lands on the legal square.");
        AssertOneMove("A gentle release above the green square must count when it settles in that legal destination.");
    }

    [UnityTest]
    public IEnumerator ABriefFingertipSupportAfterOpeningCanClearAndSettleIntoTheLegalSquare() =>
        ReleaseAfterFingertipSupport(.06f);

    [UnityTest]
    public IEnumerator WithdrawingTheOpeningFingerForAThirdOfASecondStillAllowsThePieceToLand() =>
        ReleaseAfterFingertipSupport(.32f);

    private IEnumerator ReleaseAfterFingertipSupport(float supportSeconds)
    {
        Vector3 target = board.GetPieceWorldPosition(destination);
        SetHeldPose(target + board.transform.up * .008f, upright);
        yield return new WaitForSeconds(grab.throwSmoothingDuration + .08f);
        XRHandContactBody hand = CreateSupportHand(target, true);
        yield return PhysicsSteps(8);
        Assert.That(hand.GetComponent<Rigidbody>().detectCollisions, Is.True, "Arm tracking in clear space before release.");
        Assert.That(hand.GetComponent<BoxCollider>().enabled, Is.True,
            "Tracking may be armed even while the global held-piece rule suppresses contacts.");
        AssertOverDestination();
        HandPlacementContactProbe contacts = pawn.gameObject.AddComponent<HandPlacementContactProbe>();

        QueuePinch(.1f);
        for (int step = 0; step < 8 && !contacts.SawFingerSupport; step++) yield return PhysicsSteps(1);
        Assert.That(grab.isSelected, Is.False);
        Assert.That(contacts.SawFingerSupport, Is.True, "Exercise a real supporting finger contact, not an assumed side touch.");
        Assert.That(hand.IsContactEnabled, Is.True, "Opening must restore the clear hand contacts before testing their support veto.");
        Assert.That(grab.IsSupportedByHand, Is.True);
        Assert.That(game.MoveHistory.Count, Is.Zero, "Do not confirm while the fingers still support the released piece.");
        while (Time.time < contacts.FirstFingerSupportAt + supportSeconds) yield return null;
        // Keep every joint valid while the opening finger withdraws gradually.
        // The palm remains beside the piece and MetaAim remains tracked.
        for (int step = 0; step < 3; step++)
        {
            foreach (XRHandJointID joint in new[] { XRHandJointID.IndexIntermediate, XRHandJointID.IndexDistal, XRHandJointID.IndexTip })
                supportJoints[joint] += Vector3.right * .008f;
            yield return PhysicsSteps(1);
        }
        yield return new WaitForSeconds(.7f);
        Assert.That(contacts.SawBoardSupport, Is.True);
        Assert.That(board.TryGetSquareAt(pawn.transform.position, out BoardSquare landed), Is.True);
        Assert.That(landed, Is.EqualTo(destination), "The actual released piece must still land over the same legal square.");
        AssertOneMove("A brief finger contact during opening must not permanently reject a piece that then reaches its legal square.");
    }

    [UnityTest]
    public IEnumerator RemainingOnAnOpenPalmOverALegalSquareStillDoesNotPlace() =>
        RemainOnPalmAboveLegalSquare(0f);

    [UnityTest]
    public IEnumerator RemainingOnAPalmHighAboveTheGreenSquareCannotLeaveAMovePending() =>
        RemainOnPalmAboveLegalSquare(.5f);

    private IEnumerator RemainOnPalmAboveLegalSquare(float height)
    {
        Vector3 target = board.GetPieceWorldPosition(destination) + board.transform.up * height;
        SetHeldPose(target + board.transform.up * .014f, upright);
        yield return new WaitForSeconds(grab.throwSmoothingDuration + .08f);
        XRHandContactBody hand = CreateSupportHand(target, false);
        yield return PhysicsSteps(8);
        Assert.That(hand.GetComponent<Rigidbody>().detectCollisions, Is.True);
        Assert.That(hand.GetComponent<BoxCollider>().enabled, Is.True);
        AssertOverDestination();
        HandPlacementContactProbe contacts = pawn.gameObject.AddComponent<HandPlacementContactProbe>();

        QueuePinch(.1f);
        yield return new WaitForSeconds(1.5f);
        Assert.That(grab.isSelected, Is.False);
        Assert.That(contacts.SawPalmSupport, Is.True, "The released piece must actually remain supported by the palm.");
        Assert.That(grab.IsSupportedByHand, Is.True);
        Assert.That(game.MoveHistory.Count, Is.Zero, "A legal X/Z projection while carried on a palm is not a board placement.");
        Assert.That(game.SelectedPiece, Is.Null, "Keeping a piece on the palm must not leave a move pending indefinitely.");
        Assert.That(game.CurrentTurn, Is.EqualTo(ChessSide.White));
        Assert.That(pawn.Square, Is.EqualTo(BoardSquare.FromAlgebraic("e2")));
    }

    [UnityTest]
    public IEnumerator DisablingTheBridgeDuringObservationCancelsItsSelectionWithoutAMove()
    {
        SetHeldPose(board.GetPieceWorldPosition(destination) + board.transform.up * .03f, upright);
        yield return new WaitForSeconds(grab.throwSmoothingDuration + .08f);
        AssertOverDestination();
        VrSelectionBridge bridge = pawn.GetComponent<VrSelectionBridge>();
        bool sawNativeRelease = false;
        grab.selectExited.AddListener(_ =>
        {
            sawNativeRelease = true;
            bridge.enabled = false;
        });

        QueuePinch(.1f);
        yield return PhysicsSteps(2);
        Assert.That(sawNativeRelease, Is.True);
        Assert.That(grab.isSelected, Is.False);
        Assert.That(bridge.enabled, Is.False);
        Assert.That(game.SelectedPiece, Is.Null, "Disabling an observer must clear only its still-pending chess selection.");
        Assert.That(board.HighlightCount, Is.Zero);
        yield return new WaitForSeconds(.4f);
        Assert.That(game.MoveHistory.Count, Is.Zero, "A disabled Bridge cannot later confirm a physical landing.");
    }

    [UnityTest]
    public IEnumerator DisablingOnlyTheThrowableBeforeDetachCannotLeaveObservationWaitingForItsCache()
    {
        SetHeldPose(board.GetPieceWorldPosition(destination) + board.transform.up * .03f, upright);
        yield return new WaitForSeconds(grab.throwSmoothingDuration + .08f);
        AssertOverDestination();
        bool disabledBeforeDetach = false;
        grab.selectExited.AddListener(_ =>
        {
            // The native event is before XRI LateUpdate, not a manual detach.
            disabledBeforeDetach = !grab.HasReleaseVelocity;
            grab.enabled = false;
        });

        QueuePinch(.1f);
        yield return PhysicsSteps(2);
        Assert.That(disabledBeforeDetach, Is.True, "Disable the actual interactable before its first detach/cache.");
        Assert.That(grab.isActiveAndEnabled, Is.False);
        Assert.That(pawn.GetComponent<VrSelectionBridge>().isActiveAndEnabled, Is.True);
        Assert.That(grab.HasReleaseVelocity, Is.False, "The disabled interactable must really be absent from LateUpdate processing.");
        yield return new WaitForSeconds(.4f);
        Assert.That(game.MoveHistory.Count, Is.Zero);
        Assert.That(game.SelectedPiece, Is.Null, "A missing detach cache must not keep the chess selection beyond the observation deadline.");
        Assert.That(board.HighlightCount, Is.Zero);
    }

    [UnityTest]
    public IEnumerator LosingTrackingAfterOpeningButBeforeLandingCancelsTheCandidate()
    {
        Vector3 target = board.GetPieceWorldPosition(destination);
        SetHeldPose(target + board.transform.up * .03f, upright);
        yield return new WaitForSeconds(grab.throwSmoothingDuration + .08f);
        AssertOverDestination();
        QueuePinch(.1f);
        for (int frame = 0; frame < 8 && near.IsSelecting(grab); frame++) yield return null;
        Assert.That(grab.isSelected, Is.False);
        Assert.That(game.MoveHistory.Count, Is.Zero);
        Assert.That(game.SelectedPiece, Is.SameAs(pawn), "The legal candidate must really still be under observation.");
        Assert.That(LowestBaseHeight(target), Is.GreaterThan(.02f), "Invalidate tracking before the released piece reaches the board.");

        QueuePinch(.1f, false);
        yield return PhysicsSteps(3);
        Assert.That(aim.isTracked.isPressed, Is.False);
        Assert.That(game.SelectedPiece, Is.Null);
        yield return new WaitForSeconds(.7f);
        Assert.That(game.MoveHistory.Count, Is.Zero, "A physical landing after tracking loss cannot confirm the old candidate.");
        Assert.That(game.CurrentTurn, Is.EqualTo(ChessSide.White));
    }

    [UnityTest]
    public IEnumerator PinchingAgainBeforeLandingCancelsTheOldCandidateWithoutAHiddenMove()
    {
        Vector3 target = board.GetPieceWorldPosition(destination);
        SetHeldPose(target + board.transform.up * .03f, upright);
        yield return new WaitForSeconds(grab.throwSmoothingDuration + .08f);
        AssertOverDestination();
        QueuePinch(.1f);
        for (int frame = 0; frame < 8 && near.IsSelecting(grab); frame++) yield return null;
        Assert.That(grab.isSelected, Is.False);
        Assert.That(game.MoveHistory.Count, Is.Zero);
        Assert.That(game.SelectedPiece, Is.SameAs(pawn));
        Assert.That(LowestBaseHeight(target), Is.GreaterThan(.02f), "Re-grab during the airborne observation window.");

        QueuePinch(.95f);
        yield return PhysicsSteps(2);
        Assert.That(near.IsSelecting(grab), Is.True, "Acquire again through the actual pinch and near caster.");
        // Cancel the new gesture by moving outside the board. Height alone
        // no longer separates a deliberate release from a placement.
        Vector3 outside = target + board.transform.right * 1.5f + board.transform.up * .2f;
        SetHeldPose(outside, upright);
        yield return new WaitForSeconds(.35f);
        Assert.That(game.MoveHistory.Count, Is.Zero, "The canceled observation cannot commit while the piece is held again.");
        QueuePinch(.1f);
        yield return new WaitForSeconds(.7f);
        Assert.That(grab.isSelected, Is.False);
        Assert.That(game.MoveHistory.Count, Is.Zero, "A new off-board release must not revive the first candidate.");
        Assert.That(game.CurrentTurn, Is.EqualTo(ChessSide.White));
    }

    [UnityTest]
    public IEnumerator PausingBothInputAndGameDuringObservationClearsSelectionAndCannotCommitAfterResume()
    {
        Vector3 target = board.GetPieceWorldPosition(destination);
        SetHeldPose(target + board.transform.up * .03f, upright);
        yield return new WaitForSeconds(grab.throwSmoothingDuration + .08f);
        AssertOverDestination();
        QueuePinch(.1f);
        for (int frame = 0; frame < 8 && near.IsSelecting(grab); frame++) yield return null;
        Assert.That(grab.isSelected, Is.False);
        Assert.That(game.MoveHistory.Count, Is.Zero);
        Assert.That(game.SelectedPiece, Is.SameAs(pawn));
        Assert.That(LowestBaseHeight(target), Is.GreaterThan(.02f));
        XRHandGrabInput input = near.GetComponentInParent<XRHandGrabInput>();
        Assert.That(input, Is.Not.Null);

        // A real application pause reaches both components. Pausing only the
        // input would miss ChessGameController's suspended cancellation guard.
        input.SendMessage("OnApplicationPause", true, SendMessageOptions.RequireReceiver);
        game.SendMessage("OnApplicationPause", true, SendMessageOptions.RequireReceiver);
        VrSelectionBridge bridge = pawn.GetComponent<VrSelectionBridge>();
        bridge.SendMessage("OnApplicationPause", true, SendMessageOptions.RequireReceiver);
        PieceView selectedImmediatelyAfterPause = game.SelectedPiece;
        yield return new WaitForSeconds(.35f);
        PieceView selectedWhilePaused = game.SelectedPiece;
        int movesWhilePaused = game.MoveHistory.Count;
        input.SendMessage("OnApplicationPause", false, SendMessageOptions.RequireReceiver);
        game.SendMessage("OnApplicationPause", false, SendMessageOptions.RequireReceiver);
        bridge.SendMessage("OnApplicationPause", false, SendMessageOptions.RequireReceiver);
        yield return new WaitForSeconds(.4f);
        Assert.That(movesWhilePaused, Is.Zero);
        Assert.That(selectedImmediatelyAfterPause, Is.Null, "Pause must cancel synchronously, before the player can suspend its next frame.");
        Assert.That(selectedWhilePaused, Is.Null, "The pending chess selection must clear even while the game is suspended.");
        Assert.That(game.MoveHistory.Count, Is.Zero, "Resuming cannot confirm a landing from the canceled pre-pause observation.");
        Assert.That(game.SelectedPiece, Is.Null);
        Assert.That(board.HighlightCount, Is.Zero);
        Assert.That(game.CurrentTurn, Is.EqualTo(ChessSide.White));
    }

    [UnityTest]
    public IEnumerator TossingUpAboveALegalSquareStillDoesNotPlaceWhenItLands()
    {
        Vector3 target = board.GetPieceWorldPosition(destination);
        SetHeldPose(target + board.transform.up * .2f, upright);
        yield return new WaitForSeconds(grab.throwSmoothingDuration + .08f);
        AssertOverDestination();
        HandPlacementContactProbe contacts = pawn.gameObject.AddComponent<HandPlacementContactProbe>();
        yield return MoveNativePinch(board.transform.up * .09f, .1f);
        poseReplay.MoveAt(board.transform.up * .9f);
        QueuePinch(.1f);
        for (int frame = 0; frame < 12 && near.IsSelecting(grab); frame++) yield return null;
        poseReplay.Stop();
        yield return PhysicsSteps(2);
        Assert.That(grab.isSelected, Is.False);
        Assert.That(Vector3.Dot(grab.ReleaseVelocity, board.transform.up), Is.GreaterThan(.35f),
            "Exercise a real upward throw, rather than a stationary release at a high position.");
        yield return new WaitForSeconds(1.5f);
        Assert.That(contacts.SawBoardSupport, Is.True, "The tossed piece must actually return to the board.");
        Assert.That(game.MoveHistory.Count, Is.Zero, "A throw must not become a move when it later lands on the board.");
        Assert.That(game.SelectedPiece, Is.Null);
        Assert.That(game.CurrentTurn, Is.EqualTo(ChessSide.White));
    }

    [UnityTest]
    public IEnumerator MovingSidewaysThroughALegalSquareAndOpeningStillCountsAsAToss()
    {
        Vector3 end = board.GetPieceWorldPosition(destination) + board.transform.up * .008f;
        Vector3 start = end - board.transform.right * .154f;
        SetHeldPose(start, upright);
        yield return new WaitForSeconds(grab.throwSmoothingDuration + .08f);
        yield return MoveHeld(start, end, .22f);
        AssertOverDestination();
        Vector3 releasedPosition = Vector3.zero;
        int releases = 0;
        grab.selectExited.AddListener(_ => { releasedPosition = pawn.transform.position; releases++; });
        poseReplay.MoveAt(board.transform.right * .7f);
        QueuePinch(.45f);
        // Keep moving the input pose until XRI processes the opening. A stopped
        // render frame must not accidentally turn this into the brief-pause case.
        for (int frame = 0; frame < 12 && near.IsSelecting(grab); frame++)
        {
            yield return null;
        }
        poseReplay.Stop();
        yield return PhysicsSteps(3);
        Assert.That(releases, Is.EqualTo(1));
        Assert.That(board.TryGetSquareAt(releasedPosition, out BoardSquare releasedOver), Is.True);
        Assert.That(releasedOver, Is.EqualTo(destination), "The moving release must still happen over the legal square.");
        Assert.That(grab.ReleaseVelocity.magnitude, Is.GreaterThan(.35f));
        yield return new WaitForSeconds(.65f);
        Assert.That(game.MoveHistory.Count, Is.Zero, "A moving lateral toss is not a deliberate placement.");
        Assert.That(game.CurrentTurn, Is.EqualTo(ChessSide.White));
    }

    private HandPlacementLandingRenderProbe CreateLandingRenderProbe(BoardSquare square)
    {
        var probe = new GameObject("Physical placement render probe").AddComponent<HandPlacementLandingRenderProbe>();
        probe.Configure(game, pawn, board, square, game.MoveHistory.Count);
        return probe;
    }

    private static void AssertLandingWithoutArc(HandPlacementLandingRenderProbe probe)
    {
        TestContext.WriteLine($"Physical landing render: accepted={probe.AcceptanceSeen}; samples={probe.Samples}; beforeRenderSamples={probe.BeforeRenderSamples}; maxHeight={probe.MaximumHeight:F5}m");
        Assert.That(probe.AcceptanceSeen, Is.True, "Observe actual acceptance through move history or the pending promotion choice.");
        Assert.That(probe.Samples, Is.GreaterThan(0));
        Assert.That(probe.MaximumHeight, Is.LessThanOrEqualTo(.02f),
            "A piece physically placed on its target must settle there, without replaying the desktop move arc.");
    }

    private IEnumerator AcquireNativePiece(string square)
    {
        pawn = board.Pieces.First(piece => piece.Square.Equals(BoardSquare.FromAlgebraic(square)));
        grab = pawn.GetComponent<ThrowablePiece>();
        upright = pawn.transform.rotation;
        poseReplay.Source.position = grab.colliders[0].bounds.center + board.transform.right * .003f;
        poseReplay.Source.rotation = Quaternion.identity;
        QueuePinch(.1f);
        yield return PhysicsSteps(3);
        QueuePinch(.95f);
        yield return PhysicsSteps(3);
        Assert.That(near.IsSelecting(grab), Is.True, "Acquire the actual current-turn piece through the native near interactor.");
        yield return new WaitForSeconds(.2f);
    }

    private IEnumerator MoveNativeGripToBoardSquare(BoardSquare square)
    {
        // Calculate one prescribed input path from the initial pose. No piece
        // transform or Rigidbody pose is written to reach the destination.
        Vector3 start = pawn.transform.position;
        Vector3 target = board.GetPieceWorldPosition(square) + board.transform.up * .002f;
        Vector3 delta = target - start;
        float vertical = Vector3.Dot(delta, board.transform.up);
        yield return MoveNativePinch(board.transform.up * .06f, .5f);
        yield return MoveNativePinch(delta - board.transform.up * vertical, .7f);
        yield return MoveNativePinch(board.transform.up * (vertical - .06f), .5f);
        yield return new WaitForSeconds(.08f);
        Assert.That(board.TryGetSquareAt(pawn.transform.position, out BoardSquare actual), Is.True);
        Assert.That(actual, Is.EqualTo(square));
        Assert.That(LowestBaseHeight(board.GetPieceWorldPosition(square)), Is.InRange(-.005f, .008f),
            "Release only after placing the physical base near the destination surface.");
    }

    private IEnumerator PrepareBlackQueenAtNativeGrip()
    {
        // Release the setup pawn at its own square, then create the same legal
        // black-queen opportunity seen on the device using public chess input.
        QueuePinch(.1f);
        yield return PhysicsSteps(3);
        Assert.That(grab.isSelected, Is.False);
        Assert.That(game.MoveHistory.Count, Is.Zero);
        yield return PlayFixtureMove("e2", "e4");
        yield return PlayFixtureMove("d7", "d5");
        yield return PlayFixtureMove("g1", "f3");
        Assert.That(game.CurrentTurn, Is.EqualTo(ChessSide.Black));
        pawn = board.Pieces.First(piece => piece.Square.Equals(BoardSquare.FromAlgebraic("d8")));
        grab = pawn.GetComponent<ThrowablePiece>();
        upright = pawn.transform.rotation;
        Assert.That(pawn.Kind, Is.EqualTo(ChessPieceKind.Queen));
        Assert.That(near.nearInteractionCaster.castOrigin, Is.SameAs(poseReplay.Source),
            "Use the prefab's actual pinch point for both near targeting and attachment.");
        // Inside the physical queen, around mid-height, not a point computed
        // later from a moved root or an already-created dynamic attachment.
        poseReplay.Source.position = grab.colliders[0].bounds.center + board.transform.right * .003f;
        poseReplay.Source.rotation = Quaternion.identity;
        QueuePinch(.1f);
        yield return PhysicsSteps(3);
        Assert.That(grab.isSelected, Is.False);
        Assert.That(game.CanGrabPiece(pawn), Is.True);
    }

    private IEnumerator PlayFixtureMove(string from, string to)
    {
        PieceView moving = board.Pieces.First(piece => piece.Square.Equals(BoardSquare.FromAlgebraic(from)));
        int count = game.MoveHistory.Count;
        game.SelectPiece(moving);
        game.SelectDestination(BoardSquare.FromAlgebraic(to));
        while (game.IsInputBlocked) yield return null;
        yield return null;
        Assert.That(game.MoveHistory.Count, Is.EqualTo(count + 1), $"Prepare legal position with {from}-{to}.");
    }

    private IEnumerator MoveNativePinch(Vector3 displacement, float seconds)
    {
        poseReplay.MoveBy(displacement, seconds);
        while (poseReplay.IsMoving) yield return null;
        yield return null;
        Assert.That(near.IsSelecting(grab), Is.True, "The real near interactor must keep holding during the prescribed input movement.");
    }

    private void QueuePinch(float value, bool tracked = true)
    {
        aim.UpdateHand(tracked, MetaAimFlags.Computed | MetaAimFlags.Valid, Pose.identity, value, 0f, 0f, 0f);
        InputSystem.Update();
    }

    private XRHandContactBody CreateSupportHand(Vector3 plane, bool fingertipOnly)
    {
        var root = new GameObject("Raw hand contact during native placement");
        root.transform.SetPositionAndRotation(plane, board.transform.rotation);
        var model = new GameObject("Contact replay visual");
        model.transform.SetParent(root.transform, false);
        float x = fingertipOnly ? .07f : 0f;
        float y = fingertipOnly ? .001f : -.005f;
        // Keep the auxiliary palm ahead of the occupied pawn row: at x=70 mm,
        // z=0 its volume overlaps f2 and correctly cannot arm. The fingertip
        // itself stays at the same near-board point in e3.
        float z = fingertipOnly ? .04f : 0f;
        supportJoints.Clear();
        supportJoints[XRHandJointID.Wrist] = new Vector3(x, y, z - .04f);
        supportJoints[XRHandJointID.Palm] = new Vector3(x, y, z);
        supportJoints[XRHandJointID.IndexProximal] = new Vector3(x - .026f, y, z + .04f);
        supportJoints[XRHandJointID.MiddleProximal] = new Vector3(x, y, z + .04f);
        supportJoints[XRHandJointID.LittleProximal] = new Vector3(x + .039f, y, z + .04f);
        if (fingertipOnly)
        {
            supportJoints[XRHandJointID.IndexIntermediate] = new Vector3(.036f, y, .055f);
            supportJoints[XRHandJointID.IndexDistal] = new Vector3(.025f, y, .025f);
            supportJoints[XRHandJointID.IndexTip] = new Vector3(.01f, y, 0f);
        }
        bool ReadJoint(XRHandJointID id, out Pose pose)
        {
            pose = default;
            if (!supportJoints.TryGetValue(id, out Vector3 point)) return false;
            pose = new Pose(root.transform.TransformPoint(point), root.transform.rotation);
            return true;
        }
        XRPhysicalHand physical = model.AddComponent<XRPhysicalHand>();
        physical.Configure(model.transform, root.transform, root.transform, ReadJoint);
        return root.GetComponentInChildren<XRHandContactBody>();
    }

    private void SetHeldPose(Vector3 position, Quaternion rotation)
    {
        Transform pose = near.interactionAttachController.transformToFollow;
        Quaternion delta = rotation * Quaternion.Inverse(pawn.transform.rotation);
        pose.position = position + delta * (pose.position - pawn.transform.position);
        pose.rotation = delta * pose.rotation;
    }

    private IEnumerator MoveHeld(Vector3 start, Vector3 end, float seconds)
    {
        // Drive the input before XRI Dynamic, from a fixed input origin. Do not
        // feed back the rendered piece pose into the next source sample.
        poseReplay.MoveBy(end - start, seconds);
        while (poseReplay.IsMoving) yield return null;
        yield return null;
    }

    private float LowestBaseHeight(Vector3 plane)
    {
        MeshCollider collider = pawn.GetComponent<MeshCollider>();
        Vector3[] vertices = collider.sharedMesh.vertices;
        float bottom = vertices.Min(vertex => vertex.y);
        return vertices.Where(vertex => Mathf.Abs(vertex.y - bottom) < .0001f)
            .Min(vertex => Vector3.Dot(collider.transform.TransformPoint(vertex) - plane, board.transform.up));
    }

    private void AssertOverDestination()
    {
        Assert.That(board.TryGetSquareAt(pawn.transform.position, out BoardSquare square), Is.True);
        Assert.That(square, Is.EqualTo(destination));
        Assert.That(near.IsSelecting(grab), Is.True);
        Assert.That(game.SelectedPiece, Is.SameAs(pawn));
    }

    private void AssertOneMove(string message)
    {
        Assert.That(game.MoveHistory.Count, Is.EqualTo(1), message + $" release={grab.ReleaseVelocity:F3}; status={game.StatusMessage}");
        Assert.That(game.CurrentTurn, Is.EqualTo(ChessSide.Black));
        Assert.That(pawn.Square, Is.EqualTo(destination));
    }

    private static IEnumerator PhysicsSteps(int count)
    {
        Physics.SyncTransforms();
        for (int i = 0; i < count; i++)
        {
            yield return new WaitForFixedUpdate();
            yield return null;
        }
    }
}

public sealed class HandPlacementLandingRenderProbe : MonoBehaviour
{
    private ChessGameController game;
    private PieceView piece;
    private BoardView board;
    private BoardSquare destination;
    private int initialMoves;

    public bool AcceptanceSeen { get; private set; }
    public int Samples { get; private set; }
    public int BeforeRenderSamples { get; private set; }
    public float MaximumHeight { get; private set; } = float.NegativeInfinity;

    public void Configure(ChessGameController controller, PieceView moving, BoardView view, BoardSquare square, int moveCount)
    {
        game = controller;
        piece = moving;
        board = view;
        destination = square;
        initialMoves = moveCount;
    }

    private void OnEnable() => Application.onBeforeRender += BeforeRender;
    private void OnDisable() => Application.onBeforeRender -= BeforeRender;
    private void LateUpdate() => Observe(false);
    private void BeforeRender() => Observe(true);

    private void Observe(bool beforeRender)
    {
        if (game == null || piece == null || board == null) return;
        AcceptanceSeen |= game.MoveHistory.Count > initialMoves || game.IsAwaitingPromotion;
        if (!AcceptanceSeen) return;
        Samples++;
        if (beforeRender) BeforeRenderSamples++;
        MaximumHeight = Mathf.Max(MaximumHeight,
            Vector3.Dot(piece.transform.position - board.GetPieceWorldPosition(destination), board.transform.up));
    }
}

public sealed class HandPlacementContactProbe : MonoBehaviour
{
    public bool SawBoardSupport { get; private set; }
    public bool SawFingerSupport { get; private set; }
    public bool SawPalmSupport { get; private set; }
    public float FirstFingerSupportAt { get; private set; }

    private void OnCollisionEnter(Collision collision) => Observe(collision);
    private void OnCollisionStay(Collision collision) => Observe(collision);

    private void Observe(Collision collision)
    {
        bool supports = false;
        for (int i = 0; i < collision.contactCount; i++)
            supports |= Vector3.Dot(collision.GetContact(i).normal, Vector3.up) > .35f;
        if (!supports) return;
        if (collision.collider.GetComponentInParent<BoardView>() != null &&
            collision.collider.GetComponentInParent<PieceView>() == null)
            SawBoardSupport = true;
        if (collision.rigidbody == null || !collision.rigidbody.TryGetComponent(out XRHandContactBody _)) return;
        if (collision.collider is CapsuleCollider)
        {
            if (!SawFingerSupport) FirstFingerSupportAt = Time.time;
            SawFingerSupport = true;
        }
        if (collision.collider is BoxCollider) SawPalmSupport = true;
    }
}

[DefaultExecutionOrder(-30000)]
public sealed class HandPlacementPoseReplay : MonoBehaviour
{
    public Transform Source { get; set; }
    public bool IsMoving { get; private set; }
    public float MaximumStepSpeed { get; private set; }
    private Vector3 start;
    private Vector3 displacement;
    private Vector3 velocity;
    private float started;
    private float duration;

    public void MoveBy(Vector3 delta, float seconds)
    {
        start = Source.position;
        displacement = delta;
        started = Time.time;
        duration = seconds;
        velocity = Vector3.zero;
        MaximumStepSpeed = 0f;
        IsMoving = true;
    }

    public void MoveAt(Vector3 metersPerSecond)
    {
        velocity = metersPerSecond;
        duration = 0f;
        IsMoving = true;
    }

    public void Stop() => IsMoving = false;

    private void Update()
    {
        if (!IsMoving || Source == null) return;
        Vector3 previous = Source.position;
        if (duration > 0f)
        {
            float t = Mathf.Clamp01((Time.time - started) / duration);
            Source.position = start + displacement * t;
            IsMoving = t < 1f;
        }
        else Source.position += velocity * Time.deltaTime;
        if (Time.deltaTime > 0f)
            MaximumStepSpeed = Mathf.Max(MaximumStepSpeed, Vector3.Distance(previous, Source.position) / Time.deltaTime);
    }
}
#endif
