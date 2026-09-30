using NUnit.Framework;
using UnityEngine;
#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Attachment;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Casters;
#endif

public sealed class BoardScaleGestureTests
{
    private const float BaseFrameWidth = 11.28f * .045f;

    [Test]
    public void MovingGripsApartAndTogetherChangesSizeInBothDirections()
    {
        var gesture = new BoardScaleHandles.Gesture();
        Vector3 left = Vector3.left * .33f;
        Vector3 right = Vector3.right * .33f;
        Assert.That(gesture.Begin(left, right, Vector3.right, BaseFrameWidth), Is.True);
        left += Vector3.left * .025f;
        right += Vector3.right * .025f;
        Assert.That(gesture.Step(left, right, 1f, out float grown), Is.True);
        Assert.That(grown, Is.EqualTo(1f + .05f / BaseFrameWidth).Within(.00001f));
        left += Vector3.right * .04f;
        right += Vector3.left * .04f;
        Assert.That(gesture.Step(left, right, grown, out float shrunk), Is.True);
        Assert.That(shrunk, Is.EqualTo(1f - .03f / BaseFrameWidth).Within(.00001f));
    }

    [Test]
    public void MovingBothHandsTogetherNeverTranslatesOrScalesTheBoard()
    {
        var gesture = new BoardScaleHandles.Gesture();
        Vector3 left = Vector3.left * .33f;
        Vector3 right = Vector3.right * .33f;
        gesture.Begin(left, right, Vector3.right, BaseFrameWidth);
        Vector3 movement = new Vector3(.025f, .04f, -.02f);
        Assert.That(gesture.Step(left + movement, right + movement, 1f, out float size), Is.True);
        Assert.That(size, Is.EqualTo(1f).Within(.00001f));
    }

    [Test]
    public void SeparatingHandsVerticallyDoesNotChangeHorizontalSize()
    {
        var gesture = new BoardScaleHandles.Gesture();
        Vector3 left = Vector3.left * .33f;
        Vector3 right = Vector3.right * .33f;
        gesture.Begin(left, right, Vector3.right, BaseFrameWidth);
        Assert.That(gesture.Step(left + Vector3.up * .06f, right + Vector3.down * .06f, 1f, out float size), Is.True);
        Assert.That(size, Is.EqualTo(1f).Within(.00001f));
    }

    [Test]
    public void ReversingUpperLimitOvershootImmediatelyShrinks()
    {
        var gesture = new BoardScaleHandles.Gesture();
        Vector3 left = Vector3.left * .33f;
        Vector3 right = Vector3.right * .33f;
        gesture.Begin(left, right, Vector3.right, BaseFrameWidth);
        left += Vector3.left * .08f;
        right += Vector3.right * .08f;
        gesture.Step(left, right, 1.49f, out float maximum);
        Assert.That(maximum, Is.EqualTo(BoardView.MaximumVrSize));
        left += Vector3.left * .08f;
        right += Vector3.right * .08f;
        gesture.Step(left, right, maximum, out maximum);
        Assert.That(maximum, Is.EqualTo(BoardView.MaximumVrSize));
        left += Vector3.right * .005f;
        right += Vector3.left * .005f;
        gesture.Step(left, right, maximum, out float reversed);
        Assert.That(reversed, Is.EqualTo(maximum - .01f / BaseFrameWidth).Within(.00001f),
            "Overshoot does not create a dead zone when the hands reverse.");
    }

    [Test]
    public void ReversingLowerLimitOvershootImmediatelyGrows()
    {
        var gesture = new BoardScaleHandles.Gesture();
        Vector3 left = Vector3.left * .4f;
        Vector3 right = Vector3.right * .4f;
        gesture.Begin(left, right, Vector3.right, BaseFrameWidth);
        left += Vector3.right * .08f;
        right += Vector3.left * .08f;
        gesture.Step(left, right, .76f, out float minimum);
        Assert.That(minimum, Is.EqualTo(BoardView.MinimumVrSize));
        left += Vector3.right * .08f;
        right += Vector3.left * .08f;
        gesture.Step(left, right, minimum, out minimum);
        left += Vector3.left * .005f;
        right += Vector3.right * .005f;
        gesture.Step(left, right, minimum, out float reversed);
        Assert.That(reversed, Is.EqualTo(minimum + .01f / BaseFrameWidth).Within(.00001f));
    }

    [Test]
    public void EndingAndRestartingUsesNewHandPositionsWithoutASizeJump()
    {
        var gesture = new BoardScaleHandles.Gesture();
        gesture.Begin(Vector3.left * .33f, Vector3.right * .33f, Vector3.right, BaseFrameWidth);
        gesture.End();
        Assert.That(gesture.Step(Vector3.left, Vector3.right, 1.2f, out _), Is.False);
        Vector3 left = new Vector3(-.43f, .2f, .1f);
        Vector3 right = new Vector3(.43f, .2f, .1f);
        gesture.Begin(left, right, Vector3.right, BaseFrameWidth);
        gesture.Step(left, right, 1.2f, out float size);
        Assert.That(size, Is.EqualTo(1.2f));
    }

    [Test]
    public void SuddenHandPoseJumpStopsRatherThanResizing()
    {
        var gesture = new BoardScaleHandles.Gesture();
        Vector3 left = Vector3.left * .33f;
        Vector3 right = Vector3.right * .33f;
        gesture.Begin(left, right, Vector3.right, BaseFrameWidth);
        Assert.That(gesture.Step(left + Vector3.left * .2f, right, 1f, out float size), Is.False);
        Assert.That(size, Is.EqualTo(1f));
        Assert.That(gesture.IsActive, Is.False);
    }

    [Test]
    public void InvalidPoseWidthOrCrossedHandsCannotStartOrContinue()
    {
        var gesture = new BoardScaleHandles.Gesture();
        Vector3 left = Vector3.left * .33f;
        Vector3 right = Vector3.right * .33f;
        Assert.That(gesture.Begin(left, right, Vector3.right, float.NaN), Is.False);
        Assert.That(gesture.Begin(right, left, Vector3.right, BaseFrameWidth), Is.False);
        Assert.That(gesture.Begin(left, right, Vector3.right, BaseFrameWidth), Is.True);
        Assert.That(gesture.Step(new Vector3(float.NaN, 0f, 0f), right, 1f, out float size), Is.False);
        Assert.That(size, Is.EqualTo(1f));
    }

    [Test]
    public void ProjectionFollowsBoardAxisRatherThanWorldX()
    {
        var gesture = new BoardScaleHandles.Gesture();
        Vector3 axis = Quaternion.Euler(0f, 90f, 0f) * Vector3.right;
        Vector3 left = -axis * .33f;
        Vector3 right = axis * .33f;
        gesture.Begin(left, right, axis, BaseFrameWidth);
        gesture.Step(left - axis * .02f, right + axis * .02f, 1f, out float size);
        Assert.That(size, Is.EqualTo(1f + .04f / BaseFrameWidth).Within(.00001f));
    }

#if UNITY_EDITOR
    private GameObject inputFixture;
    private XRSimulatedHMD headset;
    private XRSimulatedController leftController;
    private XRSimulatedController rightController;
    private readonly List<InputAction> fixtureActions = new List<InputAction>();
    private Scene fixtureScene;

    [UnityTest]
    public IEnumerator NativeNearSelectionStartsThenGrowsShrinksAndRearmsAfterFocusLoss()
    {
        if (XRRig.IsHeadsetPresent) Assert.Ignore("This fixture requires an isolated editor without an existing headset.");
        EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/Main.unity", new LoadSceneParameters(LoadSceneMode.Single));
        fixtureScene = SceneManager.GetActiveScene();
        yield return null;
        InputSystem.RegisterLayout<XRSimulatedHMD>();
        InputSystem.RegisterLayout<XRSimulatedController>();
        headset = InputSystem.AddDevice<XRSimulatedHMD>();
        leftController = InputSystem.AddDevice<XRSimulatedController>();
        rightController = InputSystem.AddDevice<XRSimulatedController>();
        InputSystem.SetDeviceUsage(leftController, UnityEngine.InputSystem.CommonUsages.LeftHand);
        InputSystem.SetDeviceUsage(rightController, UnityEngine.InputSystem.CommonUsages.RightHand);
        InputSystem.QueueStateEvent(headset, new XRSimulatedHMDState
        {
            isTracked = true, trackingState = 3,
            deviceRotation = Quaternion.identity, centerEyeRotation = Quaternion.identity,
            leftEyeRotation = Quaternion.identity, rightEyeRotation = Quaternion.identity,
        });
        yield return Frames(3);

        ChessGameController game = Object.FindFirstObjectByType<ChessGameController>();
        BoardView board = Object.FindFirstObjectByType<BoardView>();
        game.StartLocalGame();
        board.SetVrSize(1f);
        BoardScaleHandles handles = BoardScaleHandles.Build(board);
        handles.SendMessage("OnApplicationFocus", true);
        yield return Frames(3);
        Assert.That(handles.HandlesVisible, Is.True);

        inputFixture = new GameObject("Native scale gesture inputs");
        XRInteractionManager manager = Object.FindFirstObjectByType<XRInteractionManager>();
        if (manager == null) manager = inputFixture.AddComponent<XRInteractionManager>();
        NearFarInteractor leftInteractor = CreateNativeInteractor("LeftHand", manager);
        NearFarInteractor rightInteractor = CreateNativeInteractor("RightHand", manager);
        Vector3 leftPoint = handles.LeftHandle.Grip.position;
        Vector3 rightPoint = handles.RightHandle.Grip.position;
        float fixedGripZ = leftPoint.z;
        QueueController(leftController, leftPoint, false);
        QueueController(rightController, rightPoint, false);
        Physics.SyncTransforms();
        Vector3 physicalGripSize = handles.LeftHandle.GripCollider.bounds.size;
        yield return Frames(3);

        QueueController(leftController, leftPoint, true);
        yield return Frames(2);
        Assert.That(handles.LeftHandle.Interactor, Is.SameAs(leftInteractor));
        Assert.That(handles.RightHandle.Interactor, Is.Null);
        Assert.That(handles.IsResizing, Is.False, "A single physical hand cannot start scaling.");
        QueueController(rightController, rightPoint, true);
        yield return Frames(2);
        Assert.That(handles.RightHandle.Interactor, Is.SameAs(rightInteractor));
        Assert.That(handles.IsResizing, Is.True,
            "Region.None during selectEntered must not invalidate the newly selected pair.");

        for (int i = 0; i < 8; i++)
        {
            leftPoint -= board.transform.right * .003f;
            rightPoint += board.transform.right * .003f;
            QueueController(leftController, leftPoint, true);
            QueueController(rightController, rightPoint, true);
            yield return null;
        }
        float grown = board.VrSize;
        Assert.That(grown, Is.GreaterThan(1.03f));
        for (int i = 0; i < 8; i++)
        {
            leftPoint += board.transform.right * .004f;
            rightPoint -= board.transform.right * .004f;
            QueueController(leftController, leftPoint, true);
            QueueController(rightController, rightPoint, true);
            yield return null;
        }
        Assert.That(board.VrSize, Is.LessThan(grown));
        Assert.That(board.VrSize, Is.LessThan(1f));

        for (int i = 0; i < 22; i++)
        {
            leftPoint -= board.transform.right * .008f;
            rightPoint += board.transform.right * .008f;
            QueueController(leftController, leftPoint, true);
            QueueController(rightController, rightPoint, true);
            yield return null;
        }
        yield return Frames(2);
        Assert.That(board.VrSize, Is.EqualTo(BoardView.MaximumVrSize).Within(.00001f));
        Assert.That(handles.LeftHandle.Grip.position.z, Is.EqualTo(fixedGripZ).Within(.00001f),
            "A horizontal gesture keeps the grip under the hand even at 150%.");
        Assert.That(Vector3.Distance(handles.LeftHandle.GripCollider.bounds.size, physicalGripSize), Is.LessThan(.0001f));
        leftPoint += board.transform.right * .005f;
        rightPoint -= board.transform.right * .005f;
        QueueController(leftController, leftPoint, true);
        QueueController(rightController, rightPoint, true);
        yield return Frames(2);
        Assert.That(board.VrSize, Is.LessThan(BoardView.MaximumVrSize), "Reversing upper overshoot responds immediately.");
        for (int i = 0; i < 37; i++)
        {
            leftPoint += board.transform.right * .006f;
            rightPoint -= board.transform.right * .006f;
            QueueController(leftController, leftPoint, true);
            QueueController(rightController, rightPoint, true);
            yield return null;
        }
        yield return Frames(2);
        Assert.That(board.VrSize, Is.EqualTo(BoardView.MinimumVrSize).Within(.00001f));
        Assert.That(handles.LeftHandle.Grip.position.z, Is.EqualTo(fixedGripZ).Within(.00001f));
        leftPoint -= board.transform.right * .005f;
        rightPoint += board.transform.right * .005f;
        QueueController(leftController, leftPoint, true);
        QueueController(rightController, rightPoint, true);
        yield return Frames(2);
        Assert.That(board.VrSize, Is.GreaterThan(BoardView.MinimumVrSize), "Reversing lower overshoot responds immediately.");

        float beforeTrackingLoss = board.VrSize;
        QueueController(leftController, leftPoint, true, false);
        yield return Frames(2);
        Assert.That(handles.IsResizing, Is.False);
        QueueController(leftController, leftPoint, true);
        yield return Frames(3);
        Assert.That(handles.IsResizing, Is.False,
            "Tracking reacquisition with the trigger held cannot automatically restart the gesture.");
        Assert.That(board.VrSize, Is.EqualTo(beforeTrackingLoss).Within(.00001f));
        QueueController(leftController, leftPoint, false);
        QueueController(rightController, rightPoint, false);
        yield return Frames(2);
        leftPoint = handles.LeftHandle.Grip.position;
        rightPoint = handles.RightHandle.Grip.position;
        QueueController(leftController, leftPoint, true);
        QueueController(rightController, rightPoint, true);
        yield return Frames(3);
        Assert.That(handles.IsResizing, Is.True);

        float beforeFocusLoss = board.VrSize;
        handles.SendMessage("OnApplicationFocus", false);
        leftPoint -= board.transform.right * .02f;
        rightPoint += board.transform.right * .02f;
        QueueController(leftController, leftPoint, true);
        QueueController(rightController, rightPoint, true);
        yield return Frames(2);
        handles.SendMessage("OnApplicationFocus", true);
        yield return Frames(2);
        Assert.That(handles.IsResizing, Is.False, "Focus recovery still requires releasing the old physical inputs.");
        Assert.That(board.VrSize, Is.EqualTo(beforeFocusLoss).Within(.00001f));
        QueueController(leftController, leftPoint, false);
        QueueController(rightController, rightPoint, false);
        yield return Frames(2);
        leftPoint = handles.LeftHandle.Grip.position;
        rightPoint = handles.RightHandle.Grip.position;
        QueueController(leftController, leftPoint, true);
        QueueController(rightController, rightPoint, true);
        yield return Frames(3);
        Assert.That(handles.IsResizing, Is.True);
        Assert.That(board.VrSize, Is.EqualTo(beforeFocusLoss).Within(.00001f), "Regrabbing creates a fresh baseline.");

        if (XRRig.Origin != null)
        {
            XRRig.Origin.position += Vector3.right * .01f;
            yield return Frames(2);
            Assert.That(handles.IsResizing, Is.False, "A recentered origin interrupts a still-held gesture.");
            Assert.That(board.VrSize, Is.EqualTo(beforeFocusLoss).Within(.00001f));
        }
    }

    private NearFarInteractor CreateNativeInteractor(string hand, XRInteractionManager manager)
    {
        var source = new GameObject(hand + " native scale input");
        source.SetActive(false);
        source.transform.SetParent(inputFixture.transform, false);
        var driver = source.AddComponent<TrackedPoseDriver>();
        driver.positionInput = new InputActionProperty(FixtureAction(hand + " position", InputActionType.Value,
            $"<XRSimulatedController>{{{hand}}}/devicePosition", "Vector3"));
        driver.rotationInput = new InputActionProperty(FixtureAction(hand + " rotation", InputActionType.Value,
            $"<XRSimulatedController>{{{hand}}}/deviceRotation", "Quaternion"));
        driver.trackingStateInput = new InputActionProperty(FixtureAction(hand + " tracking", InputActionType.Value,
            $"<XRSimulatedController>{{{hand}}}/trackingState", "Integer"));
        var near = source.AddComponent<SphereInteractionCaster>();
        near.castOrigin = source.transform;
        near.castRadius = .06f;
        near.physicsLayerMask = 1 << BoardScaleHandles.PhysicsLayer;
        var far = source.AddComponent<CurveInteractionCaster>();
        far.raycastMask = ~(1 << BoardScaleHandles.PhysicsLayer);
        var attach = source.AddComponent<InteractionAttachController>();
        var interactor = source.AddComponent<NearFarInteractor>();
        interactor.interactionManager = manager;
        interactor.nearInteractionCaster = near;
        interactor.farInteractionCaster = far;
        interactor.interactionAttachController = attach;
        interactor.enableFarCasting = false;
        interactor.selectInput = new XRInputButtonReader("Native scale select")
        {
            inputSourceMode = XRInputButtonReader.InputSourceMode.InputAction,
            inputActionPerformed = FixtureAction(hand + " select", InputActionType.Button,
                $"<XRSimulatedController>{{{hand}}}/triggerButton", "Button"),
        };
        source.SetActive(true);
        return interactor;
    }

    private InputAction FixtureAction(string name, InputActionType type, string binding, string controlType)
    {
        var action = new InputAction(name, type, binding, expectedControlType: controlType);
        fixtureActions.Add(action);
        return action;
    }

    private static void QueueController(XRSimulatedController device, Vector3 position, bool pressed, bool tracked = true)
    {
        var state = new XRSimulatedControllerState
        {
            devicePosition = position, deviceRotation = Quaternion.identity,
            isTracked = tracked, trackingState = tracked ? 3 : 0, trigger = pressed ? 1f : 0f,
        }.WithButton(ControllerButton.TriggerButton, pressed);
        InputSystem.QueueStateEvent(device, state);
    }

    private static IEnumerator Frames(int count)
    {
        for (int i = 0; i < count; i++) yield return null;
    }

    [UnityTearDown]
    public IEnumerator ClearNativeFixture()
    {
        if (inputFixture != null) Object.DestroyImmediate(inputFixture);
        for (int i = 0; i < fixtureActions.Count; i++) fixtureActions[i].Dispose();
        fixtureActions.Clear();
        if (leftController != null && leftController.added) InputSystem.RemoveDevice(leftController);
        if (rightController != null && rightController.added) InputSystem.RemoveDevice(rightController);
        if (headset != null && headset.added) InputSystem.RemoveDevice(headset);
        inputFixture = null;
        leftController = rightController = null;
        headset = null;
        if (fixtureScene.IsValid() && fixtureScene.isLoaded)
        {
            SceneManager.SetActiveScene(SceneManager.CreateScene("Empty after scale gesture input test"));
            yield return SceneManager.UnloadSceneAsync(fixtureScene);
        }
        fixtureScene = default;
    }
#endif
}
