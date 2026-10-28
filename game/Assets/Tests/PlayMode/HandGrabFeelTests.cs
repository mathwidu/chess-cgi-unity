#if UNITY_EDITOR
using System.Collections;
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
using UnityEngine.XR.OpenXR.Features.Interactions;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public sealed class HandGrabFeelTests
{
    private XRSimulatedHMD headset;
    private MetaAimHand aim;
    private MetaAimHand previousLeftAim;
    private GameObject hand;
    private NearFarInteractor near;
    private ChessGameController game;
    private BoardView board;
    private PieceView pawn;
    private XRGrabInteractable grab;
    private HandInteractionProfile.HandInteraction alternateHand;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        headset = InputSystem.AddDevice<XRSimulatedHMD>();
        EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/Main.unity", new LoadSceneParameters(LoadSceneMode.Single));
        yield return null;
        yield return null;
        if (Object.FindFirstObjectByType<XRRig>() == null)
            new GameObject("Analog hand grab replay bootstrap").AddComponent<XRRig>();
        yield return null;
        yield return null;

        previousLeftAim = MetaAimHand.left;
        aim = MetaAimHand.CreateHand(InputDeviceCharacteristics.Left);
        Assert.That(aim, Is.Not.Null);
        MetaAimHand.left = aim;
        Assert.That(aim.usages, Does.Contain(UnityEngine.InputSystem.CommonUsages.LeftHand));

        Transform offset = XRRig.Origin.Find("Camera Offset");
        offset.GetComponent<XRInputModalityManager>().enabled = false;
        foreach (string name in new[] { "LeftHandInteractor", "RightHandInteractor", "Left Controller", "Right Controller" })
            offset.Find(name).gameObject.SetActive(false);
        hand = offset.Find("LeftHandInteractor").gameObject;
        foreach (TrackedPoseDriver driver in hand.GetComponentsInChildren<TrackedPoseDriver>(true))
            driver.enabled = false;
        foreach (XRPokeInteractor poke in hand.GetComponentsInChildren<XRPokeInteractor>(true))
            poke.enabled = false;
        near = hand.GetComponentInChildren<NearFarInteractor>(true);
        near.enableFarCasting = false;
        near.enableUIInteraction = false;

        game = Object.FindFirstObjectByType<ChessGameController>();
        game.StartLocalGame();
        yield return null;
        board = Object.FindFirstObjectByType<BoardView>();
        pawn = board.Pieces.First(piece => piece.Square.Equals(BoardSquare.FromAlgebraic("e2")));
        grab = pawn.GetComponent<XRGrabInteractable>();
        near.nearInteractionCaster.castOrigin.position = grab.colliders[0].bounds.center;
        QueuePinch(0f, true);
        hand.SetActive(true);
        yield return PhysicsSteps(3);
        Assert.That(near.interactablesHovered, Does.Contain(grab));
        Assert.That(near.selectInput.ReadValue(), Is.EqualTo(0f).Within(.001f));
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        if (alternateHand != null && alternateHand.added) InputSystem.RemoveDevice(alternateHand);
        if (aim != null && aim.added) InputSystem.RemoveDevice(aim);
        if (MetaAimHand.left == aim) MetaAimHand.left = previousLeftAim;
        if (headset != null && headset.added) InputSystem.RemoveDevice(headset);
        Scene previous = SceneManager.GetActiveScene();
        SceneManager.SetActiveScene(SceneManager.CreateScene("Empty after analog hand grab test"));
        yield return SceneManager.UnloadSceneAsync(previous);
    }

    [UnityTest]
    public IEnumerator ARelaxedPinchAcquiresMaintainsAndReleasesThroughTheRealNearInput()
    {
        int acquired = 0;
        int released = 0;
        grab.selectEntered.AddListener(_ => acquired++);
        grab.selectExited.AddListener(_ => released++);

        // Feed the device upstream of the original input actions and reader.
        // Replacing near.selectInput with a performed bool would hide its thresholds.
        QueuePinch(.85f, true);
        yield return PhysicsSteps(3);
        Assert.That(near.selectInput.ReadValue(), Is.EqualTo(.85f).Within(.001f), "The native input chain must receive the analog pinch.");
        Assert.That(near.IsSelecting(grab), Is.True, "A relaxed .85 pinch at the piece must acquire it.");
        Assert.That(game.SelectedPiece, Is.SameAs(pawn));
        Assert.That(near.uiPressInput.ReadIsPerformed(), Is.True, "The same deliberate pinch must be usable for the menu and near grabs.");

        QueuePinch(.65f, true);
        yield return PhysicsSteps(4);
        Assert.That(near.IsSelecting(grab), Is.True, "Relaxing a held pinch to .65 must not drop the piece.");
        Assert.That(acquired, Is.EqualTo(1));
        Assert.That(released, Is.Zero);

        QueuePinch(.45f, true);
        yield return PhysicsSteps(4);
        Assert.That(grab.isSelected, Is.False, "Opening to .45 must release the piece.");
        Assert.That(game.SelectedPiece, Is.Null);
        Assert.That(released, Is.EqualTo(1));
        yield return PhysicsSteps(3);
        Assert.That(released, Is.EqualTo(1), "Holding the fingers open must not repeatedly release the piece.");
        Assert.That(game.MoveHistory.Count, Is.Zero);
    }

    [UnityTest]
    public IEnumerator TrackingLossReleasesAndAStillClosedHandMustOpenBeforeRearming()
    {
        QueuePinch(.95f, true);
        yield return PhysicsSteps(3);
        Assert.That(near.IsSelecting(grab), Is.True, "A strong valid pinch must acquire before testing tracking loss.");

        BoardSquare destination = BoardSquare.FromAlgebraic("e3");
        near.nearInteractionCaster.castOrigin.position += board.GetPieceWorldPosition(destination) - pawn.transform.position;
        yield return PhysicsSteps(12);
        Assert.That(board.TryGetSquareAt(pawn.transform.position, out BoardSquare over), Is.True);
        Assert.That(over, Is.EqualTo(destination), "The tracked hand must really hold the piece over a legal destination.");

        // Preserve the stale strong value while invalidating tracking explicitly.
        QueuePinch(.95f, false);
        yield return PhysicsSteps(3);
        Assert.That(aim.isTracked.isPressed, Is.False);
        Assert.That(grab.isSelected, Is.False, "Invalid tracking must release even if the last pinch value remains high.");
        yield return new WaitForSeconds(.6f);
        Assert.That(game.MoveHistory.Count, Is.Zero, "Tracking loss over e3 must not commit a chess move after the deferred drop.");
        Assert.That(game.CurrentTurn, Is.EqualTo(ChessSide.White));

        QueuePinch(.95f, true);
        yield return PhysicsSteps(3);
        Assert.That(near.hasSelection, Is.False, "Restoring tracking with closed fingers cannot silently re-grab.");

        QueuePinch(.45f, true);
        yield return PhysicsSteps(3);
        Assert.That(near.hasSelection, Is.False, "Opening only rearms the hand.");
        near.nearInteractionCaster.castOrigin.position = grab.colliders[0].bounds.center;
        QueuePinch(.85f, true);
        yield return PhysicsSteps(3);
        Assert.That(near.IsSelecting(grab), Is.True, "A new pinch after opening must acquire again.");
    }

    [UnityTest]
    public IEnumerator LosingApplicationFocusReleasesAndRequiresOpeningAfterReturn()
        => InterruptAndRearm("OnApplicationFocus", false, true);

    [UnityTest]
    public IEnumerator PausingApplicationReleasesAndRequiresOpeningAfterReturn()
        => InterruptAndRearm("OnApplicationPause", true, false);

    [UnityTest]
    public IEnumerator AnOpenOrOnlyPartlyClosedTrackedHandCannotStartAGrab()
    {
        foreach (float strength in new[] { 0f, .45f, .65f, 0f })
        {
            QueuePinch(strength, true);
            yield return PhysicsSteps(3);
            Assert.That(near.hasSelection, Is.False, $"A hand at pinch strength {strength} must only hover.");
            Assert.That(game.SelectedPiece, Is.Null);
            Assert.That(game.MoveHistory.Count, Is.Zero);
        }
    }

    [UnityTest]
    public IEnumerator OpeningThePinchReleasesEvenWhenAnotherProviderStillReportsAClosedGrasp()
    {
        QueuePinch(.95f, true);
        yield return PhysicsSteps(3);
        Assert.That(near.IsSelecting(grab), Is.True);
        AddClosedGraspProvider();
        QueuePinch(.1f, true);
        yield return PhysicsSteps(3);
        AssertClosedGraspAndOpenPinch();
        Assert.That(grab.isSelected, Is.False, "Opening the actual thumb/index pinch must release even while graspValue stays high.");
        yield return PhysicsSteps(4);
        Assert.That(near.hasSelection, Is.False, "An unchanged closed-grasp channel must not re-grab the open pinch.");
        Assert.That(game.MoveHistory.Count, Is.Zero);
    }

    [UnityTest]
    public IEnumerator AClosedGraspFromAnotherProviderCannotAcquireAnOpenPinch()
    {
        AddClosedGraspProvider();
        QueuePinch(.1f, true);
        yield return PhysicsSteps(4);
        AssertClosedGraspAndOpenPinch();
        Assert.That(near.hasSelection, Is.False, "The near pinch must not acquire just because the other fingers remain curled.");
        Assert.That(game.SelectedPiece, Is.Null);
        Assert.That(game.MoveHistory.Count, Is.Zero);
    }

    private void AddClosedGraspProvider()
    {
        InputSystem.RegisterLayout<HandInteractionProfile.HandInteraction>("HandInteraction");
        alternateHand = InputSystem.AddDevice<HandInteractionProfile.HandInteraction>();
        InputSystem.SetDeviceUsage(alternateHand, UnityEngine.InputSystem.CommonUsages.LeftHand);
        InputSystem.QueueDeltaStateEvent(alternateHand.graspValue, .95f);
        InputSystem.QueueDeltaStateEvent(alternateHand.pinchValue, .1f);
        InputSystem.Update();
    }

    private void AssertClosedGraspAndOpenPinch()
    {
        Assert.That(aim.pinchStrengthIndex.ReadValue(), Is.EqualTo(.1f).Within(.001f));
        Assert.That(alternateHand.graspValue.ReadValue(), Is.EqualTo(.95f).Within(.001f));
        // Read the same original prefab reader, before XRRig wraps it. Its
        // action intentionally merges grasp and pinch; the near pinch cannot.
        NearFarInteractor prefabNear = Resources.Load<GameObject>("XR/LeftHandInteractor")
            .GetComponentInChildren<NearFarInteractor>(true);
        Assert.That(prefabNear.selectInput.ReadValue(), Is.EqualTo(.95f).Within(.001f),
            "Exercise an actually conflicting action value from two native devices.");
    }

    private void QueuePinch(float strength, bool tracked)
    {
        aim.UpdateHand(tracked, MetaAimFlags.Computed | MetaAimFlags.Valid,
            Pose.identity, strength, 0f, 0f, 0f);
        InputSystem.Update();
    }

    private IEnumerator InterruptAndRearm(string message, bool interrupted, bool resumed)
    {
        QueuePinch(.95f, true);
        yield return PhysicsSteps(3);
        Assert.That(near.IsSelecting(grab), Is.True, "Acquire through the native input before interrupting the application.");
        XRHandGrabInput input = hand.GetComponent<XRHandGrabInput>();
        Assert.That(input, Is.Not.Null);
        // Unity lifecycle messages are the external boundary; MetaAim stays tracked and pinched.
        input.SendMessage(message, interrupted, SendMessageOptions.DontRequireReceiver);
        yield return PhysicsSteps(3);
        Assert.That(aim.isTracked.isPressed, Is.True);
        Assert.That(aim.pinchStrengthIndex.ReadValue(), Is.EqualTo(.95f).Within(.001f));
        Assert.That(grab.isSelected, Is.False, message + " must release even if MetaAim can still run in the background.");

        input.SendMessage(message, resumed, SendMessageOptions.DontRequireReceiver);
        QueuePinch(.95f, true);
        yield return PhysicsSteps(3);
        Assert.That(near.hasSelection, Is.False, "Returning with closed fingers must not re-grab.");
        QueuePinch(.45f, true);
        yield return PhysicsSteps(3);
        near.nearInteractionCaster.castOrigin.position = grab.colliders[0].bounds.center;
        QueuePinch(.85f, true);
        yield return PhysicsSteps(3);
        Assert.That(near.IsSelecting(grab), Is.True, "Opening and pinching again must restore intentional grabbing.");
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
#endif
