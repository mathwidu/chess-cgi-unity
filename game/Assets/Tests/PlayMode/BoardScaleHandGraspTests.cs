#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unity.Collections;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Hands.ProviderImplementation;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Casters;

// Replay actual joint geometry and Meta Aim devices through the real rig and XRI.
// No new implementation type is referenced, so the original pinch-only rig can
// compile this suite and fail on the missing whole-hand selection behavior.
public sealed class BoardScaleHandGraspTests
{
    private readonly List<InputAction> actions = new List<InputAction>();
    private readonly GameObject[] hands = new GameObject[2];
    private readonly NearFarInteractor[] interactors = new NearFarInteractor[2];
    private readonly MetaAimHand[] aims = new MetaAimHand[2];
    private readonly MetaAimHand[] previousAims = new MetaAimHand[2];
    private XRSimulatedHMD headset;
    private XRHandSubsystem subsystem;
    private ScaleGraspReplayProvider replay;
    private BoardView board;
    private ChessGameController game;
    private BoardScaleHandles handles;
    private Transform trackingSpace;
    private float previousCaptureDeltaTime;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        previousCaptureDeltaTime = Time.captureDeltaTime;
        Time.captureDeltaTime = 1f / 72f;
        headset = InputSystem.AddDevice<XRSimulatedHMD>();
        previousAims[0] = MetaAimHand.left;
        previousAims[1] = MetaAimHand.right;
        aims[0] = MetaAimHand.CreateHand(InputDeviceCharacteristics.Left);
        aims[1] = MetaAimHand.CreateHand(InputDeviceCharacteristics.Right);
        MetaAimHand.left = aims[0];
        MetaAimHand.right = aims[1];
        CreateSubsystem();
        EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/Main.unity", new LoadSceneParameters(LoadSceneMode.Single));
        yield return Frames(2);
        if (Object.FindFirstObjectByType<XRRig>() == null)
            new GameObject("Whole-hand scale replay bootstrap").AddComponent<XRRig>();
        yield return Frames(2);
        game = Object.FindFirstObjectByType<ChessGameController>();
        board = Object.FindFirstObjectByType<BoardView>();
        game.StartLocalGame();
        board.SetVrSize(1f);
        handles = BoardScaleHandles.Build(board);
        handles.SendMessage("OnApplicationFocus", true);
        trackingSpace = XRRig.Origin.Find("Camera Offset");
        trackingSpace.GetComponent<XRInputModalityManager>().enabled = false;
        foreach (string name in new[] { "LeftHandInteractor", "RightHandInteractor", "Left Controller", "Right Controller" })
            trackingSpace.Find(name).gameObject.SetActive(false);
        for (int i = 0; i < hands.Length; i++)
        {
            string side = i == 0 ? "LeftHand" : "RightHand";
            hands[i] = trackingSpace.Find(side + "Interactor").gameObject;
            foreach (TrackedPoseDriver driver in hands[i].GetComponentsInChildren<TrackedPoseDriver>(true)) driver.enabled = false;
            foreach (XRPokeInteractor poke in hands[i].GetComponentsInChildren<XRPokeInteractor>(true)) poke.enabled = false;
            foreach (XRPhysicalHand physical in hands[i].GetComponentsInChildren<XRPhysicalHand>(true)) physical.enabled = false;
            NearFarInteractor interactor = hands[i].GetComponentInChildren<NearFarInteractor>(true);
            interactors[i] = interactor;
            interactor.enableFarCasting = false;
            interactor.enableUIInteraction = false;
            // The normal pinch-grab pose is not available from a physical OpenXR
            // profile in EditMode. Bind its real pose driver to the replayed aim.
            TrackedPoseDriver pointDriver = interactor.nearInteractionCaster.castOrigin.GetComponent<TrackedPoseDriver>();
            Assert.That(pointDriver, Is.Not.Null);
            pointDriver.positionInput = new InputActionProperty(Action(side + " scale position", "<MetaAimHand>{" + side + "}/devicePosition", "Vector3"));
            pointDriver.rotationInput = new InputActionProperty(Action(side + " scale rotation", "<MetaAimHand>{" + side + "}/deviceRotation", "Quaternion"));
            pointDriver.trackingStateInput = new InputActionProperty(Action(side + " scale tracking", "<MetaAimHand>{" + side + "}/trackingState", "Integer"));
            pointDriver.enabled = true;
            SetHand(i, i == 0 ? handles.LeftHandle.Grip.position : handles.RightHandle.Grip.position, false);
            hands[i].SetActive(true);
        }
        yield return Frames(5);
        Assert.That(handles.HandlesVisible, Is.True);
        Assert.That(interactors[0].uiPressInput.GetObjectReference(), Is.SameAs(hands[0].GetComponent<XRHandGrabInput>()),
            "The HUD must retain the original pinch-only reader.");
    }

    [UnityTest]
    public IEnumerator TwoWholeHandsGraspTheRealHandlesAndOnlyTheirLateralSpanResizes()
    {
        Vector3 left = handles.LeftHandle.Grip.position;
        Vector3 right = handles.RightHandle.Grip.position;
        SetHand(0, left, true);
        yield return Frames(4);
        Assert.That(handles.LeftHandle.Interactor, Is.SameAs(interactors[0]), "A whole hand must grab a nearby handle without an index/thumb pinch.");
        Assert.That(handles.IsResizing, Is.False);
        Assert.That(board.VrSize, Is.EqualTo(1f).Within(.00001f));
        SetHand(1, right, true);
        yield return Frames(4);
        Assert.That(handles.RightHandle.Interactor, Is.SameAs(interactors[1]));
        Assert.That(handles.IsResizing, Is.True);
        for (int i = 0; i < 8; i++)
        {
            left -= board.transform.right * .003f;
            right += board.transform.right * .003f;
            SetHand(0, left, true);
            SetHand(1, right, true);
            yield return Frames(1);
        }
        yield return Frames(2);
        Assert.That(board.VrSize, Is.GreaterThan(1.04f));
        float chosen = board.VrSize;
        SetHand(0, left, false);
        yield return Frames(4);
        Assert.That(handles.LeftHandle.Interactor, Is.Null);
        Assert.That(handles.IsResizing, Is.False);
        Assert.That(board.VrSize, Is.EqualTo(chosen).Within(.00001f));
        Assert.That(game.SelectedPiece, Is.Null);
        Assert.That(game.MoveHistory.Count, Is.Zero);
    }

    [UnityTest]
    public IEnumerator AWholeHandCannotGrabAPieceOrPressTheActualMenu()
    {
        PieceView piece = board.Pieces.First(candidate => candidate.Square.Equals(BoardSquare.FromAlgebraic("e2")));
        Vector3 piecePosition = piece.transform.position;
        SetHand(0, piecePosition + Vector3.up * .02f, true);
        yield return Frames(4);
        Assert.That(piece.GetComponent<XRGrabInteractable>().isSelected, Is.False);
        Assert.That(interactors[0].hasSelection, Is.False);
        Assert.That(game.SelectedPiece, Is.Null);
        Assert.That(interactors[0].selectInput.ReadIsPerformed(), Is.False, "Fist input must never be merged into the pinch reader.");

        game.ReturnToMenu();
        yield return Frames(4);
        var hud = Object.FindFirstObjectByType<GameHud>();
        Button button = hud.GetComponentsInChildren<Button>(true).Single(candidate => candidate.name == "LocalModeButton");
        int clicks = 0;
        button.onClick.AddListener(() => clicks++);
        RectTransform panel = (RectTransform)hud.transform;
        RectTransform rect = (RectTransform)button.transform;
        Vector3 target = rect.TransformPoint(rect.rect.center);
        interactors[0].enableFarCasting = true;
        interactors[0].enableUIInteraction = true;
        interactors[0].enableNearCasting = false;
        var far = (CurveInteractionCaster)interactors[0].farInteractionCaster;
        far.enableStabilization = false;
        far.castOrigin.SetPositionAndRotation(target - panel.forward * .3f, Quaternion.LookRotation(panel.forward, panel.up));
        Canvas.ForceUpdateCanvases();
        yield return Frames(4);
        Assert.That(interactors[0].TryGetUIModel(out var model), Is.True);
        Assert.That(model.currentRaycast.isValid, Is.True, "The actual menu button must be hit before checking fist input.");
        Assert.That(model.currentRaycast.gameObject.GetComponentInParent<Button>(), Is.SameAs(button));
        Assert.That(model.select, Is.False);
        SetHand(0, piecePosition, false);
        yield return Frames(4);
        Assert.That(clicks, Is.Zero);
        Assert.That(game.IsMenuOpen, Is.True);
    }

    [UnityTest]
    public IEnumerator TrackingLossRequiresARealOpeningBeforeWholeHandReacquisition()
        => InterruptGrasp(null);

    [UnityTest]
    public IEnumerator FocusLossRequiresARealOpeningBeforeWholeHandReacquisition()
        => InterruptGrasp("OnApplicationFocus");

    [UnityTest]
    public IEnumerator PauseRequiresARealOpeningBeforeWholeHandReacquisition()
        => InterruptGrasp("OnApplicationPause");

    [UnityTest]
    public IEnumerator SwitchingHandModalityCancelsTheGripUntilTheHandsOpen()
        => InterruptGrasp("Modality");

    [UnityTest]
    public IEnumerator ReturningToTheMenuCancelsBothGripsAndReopeningRearmsTheNextMatch()
    {
        for (int round = 0; round < 2; round++)
        {
            SetHand(0, handles.LeftHandle.Grip.position, true);
            SetHand(1, handles.RightHandle.Grip.position, true);
            yield return Frames(4);
            Assert.That(handles.IsResizing, Is.True, "The repeated gesture must acquire both actual grips.");
            game.ReturnToMenu();
            yield return Frames(4);
            Assert.That(handles.LeftHandle.Interactor, Is.Null);
            Assert.That(handles.RightHandle.Interactor, Is.Null);
            Assert.That(interactors[0].hasSelection || interactors[1].hasSelection, Is.False);
            Assert.That(interactors[0].isPerformingManualInteraction || interactors[1].isPerformingManualInteraction, Is.False);
            game.StartLocalGame();
            yield return Frames(4);
            Assert.That(handles.IsResizing, Is.False, "Closed hands must not regrab after returning from the menu.");
            SetHand(0, handles.LeftHandle.Grip.position, false);
            SetHand(1, handles.RightHandle.Grip.position, false);
            yield return Frames(4);
        }
    }

    [UnityTest]
    public IEnumerator PalmAndFourTrackedFingersCanGraspWithoutPinchAimOrThumbTracking()
    {
        replay.ThumbTracked = false;
        SetHand(0, handles.LeftHandle.Grip.position, true);
        SetHand(1, handles.RightHandle.Grip.position, true);
        for (int i = 0; i < aims.Length; i++)
            aims[i].UpdateHand(false, MetaAimFlags.None, Pose.identity, 0f, 0f, 0f, 0f);
        InputSystem.Update();
        yield return Frames(4);
        Assert.That(handles.IsResizing, Is.True, "A fist uses the tracked palm/fingers even when its pinch pose and thumb are unavailable.");
        Assert.That(interactors[0].uiPressInput.ReadIsPerformed(), Is.False);
        Assert.That(interactors[1].uiPressInput.ReadIsPerformed(), Is.False);
    }

    [UnityTest]
    public IEnumerator OneExtendedFingerDoesNotCountAsAWholeHandGrasp()
    {
        replay.ExtendedFinger[0] = XRHandFingerID.Index;
        SetHand(0, handles.LeftHandle.Grip.position, true);
        yield return Frames(4);
        Assert.That(handles.LeftHandle.Interactor, Is.Null, "Three curled fingers must not acquire a grip.");
        replay.ExtendedFinger[0] = null;
        UpdateHands();
        yield return Frames(4);
        Assert.That(handles.LeftHandle.Interactor, Is.SameAs(interactors[0]));
        replay.ExtendedFinger[0] = XRHandFingerID.Index;
        UpdateHands();
        yield return Frames(4);
        Assert.That(handles.LeftHandle.Interactor, Is.Null, "Opening the grasp must release even with the remaining fingers curled.");
    }

    private IEnumerator InterruptGrasp(string message)
    {
        Vector3 left = handles.LeftHandle.Grip.position;
        Vector3 right = handles.RightHandle.Grip.position;
        SetHand(0, left, true);
        SetHand(1, right, true);
        yield return Frames(4);
        Assert.That(handles.IsResizing, Is.True, "The gesture must be active before testing cancellation.");
        if (message == null) SetHand(0, left, true, false);
        else if (message == "Modality") hands[0].SetActive(false);
        else
        {
            hands[0].BroadcastMessage(message, message == "OnApplicationPause", SendMessageOptions.DontRequireReceiver);
            handles.SendMessage(message, message == "OnApplicationPause");
        }
        yield return Frames(3);
        Assert.That(handles.IsResizing, Is.False);
        float chosen = board.VrSize;
        if (message == null) SetHand(0, left, true);
        else if (message == "Modality") hands[0].SetActive(true);
        else
        {
            hands[0].BroadcastMessage(message, message == "OnApplicationFocus", SendMessageOptions.DontRequireReceiver);
            handles.SendMessage(message, message == "OnApplicationFocus");
        }
        yield return Frames(4);
        Assert.That(handles.IsResizing, Is.False, "A closed hand returning from an interruption must not reacquire.");
        Assert.That(board.VrSize, Is.EqualTo(chosen).Within(.00001f));
        SetHand(0, left, false);
        SetHand(1, right, false);
        yield return Frames(4);
        SetHand(0, handles.LeftHandle.Grip.position, true);
        SetHand(1, handles.RightHandle.Grip.position, true);
        yield return Frames(4);
        Assert.That(handles.IsResizing, Is.True, "Both hands may regrasp after a real opening.");
    }

    private InputAction Action(string name, string binding, string type)
    {
        var action = new InputAction(name, InputActionType.Value, binding, expectedControlType: type);
        actions.Add(action);
        return action;
    }

    private void SetHand(int index, Vector3 world, bool closed, bool tracked = true)
    {
        Vector3 local = trackingSpace.InverseTransformPoint(world);
        replay.Positions[index] = local;
        replay.Closed[index] = closed;
        replay.Tracked[index] = tracked;
        // Deliberately keep index/thumb pinch open for every fist.
        aims[index].UpdateHand(tracked, MetaAimFlags.Computed | MetaAimFlags.Valid, new Pose(local, Quaternion.identity), 0f, 0f, 0f, 0f);
        InputSystem.Update();
        UpdateHands();
        Physics.SyncTransforms();
    }

    private void UpdateHands()
    {
        subsystem.TryUpdateHands(XRHandSubsystem.UpdateType.Dynamic);
        subsystem.TryUpdateHands(XRHandSubsystem.UpdateType.BeforeRender);
    }

    private IEnumerator Frames(int count)
    {
        for (int i = 0; i < count; i++)
        {
            UpdateHands();
            Physics.SyncTransforms();
            yield return null;
        }
    }

    private void CreateSubsystem()
    {
        var descriptors = new List<XRHandSubsystemDescriptor>();
        SubsystemManager.GetSubsystemDescriptors(descriptors);
        const string id = "ChessCgi.ScaleWholeHandReplay";
        XRHandSubsystemDescriptor descriptor = descriptors.Find(candidate => candidate.id == id);
        if (descriptor == null)
        {
            XRHandSubsystemDescriptor.Register(new XRHandSubsystemDescriptor.Cinfo
            {
                id = id, providerType = typeof(ScaleGraspReplayProvider), subsystemTypeOverride = typeof(ScaleGraspReplaySubsystem),
            });
            descriptors.Clear();
            SubsystemManager.GetSubsystemDescriptors(descriptors);
            descriptor = descriptors.Find(candidate => candidate.id == id);
        }
        subsystem = descriptor.Create();
        replay = ((ScaleGraspReplaySubsystem)subsystem).Replay;
        subsystem.Start();
        UpdateHands();
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        if (subsystem != null) { subsystem.Stop(); subsystem.Destroy(); subsystem = null; }
        for (int i = 0; i < aims.Length; i++)
        {
            if (aims[i] != null && aims[i].added) InputSystem.RemoveDevice(aims[i]);
            if (i == 0 && MetaAimHand.left == aims[i]) MetaAimHand.left = previousAims[i];
            if (i == 1 && MetaAimHand.right == aims[i]) MetaAimHand.right = previousAims[i];
        }
        if (headset != null && headset.added) InputSystem.RemoveDevice(headset);
        Scene previous = SceneManager.GetActiveScene();
        SceneManager.SetActiveScene(SceneManager.CreateScene("Empty after whole-hand scale test"));
        yield return SceneManager.UnloadSceneAsync(previous);
        foreach (InputAction action in actions) action.Dispose();
        actions.Clear();
        Time.captureDeltaTime = previousCaptureDeltaTime;
    }

    public sealed class ScaleGraspReplaySubsystem : XRHandSubsystem
    {
        public ScaleGraspReplayProvider Replay => (ScaleGraspReplayProvider)provider;
    }

    public sealed class ScaleGraspReplayProvider : XRHandSubsystemProvider
    {
        public readonly Vector3[] Positions = new Vector3[2];
        public readonly bool[] Closed = new bool[2];
        public readonly bool[] Tracked = { true, true };
        public readonly XRHandFingerID?[] ExtendedFinger = new XRHandFingerID?[2];
        public bool ThumbTracked = true;
        public override void Start() { }
        public override void Stop() { }
        public override void Destroy() { }
        public override void GetHandLayout(NativeArray<bool> layout)
        {
            for (int i = 0; i < layout.Length; i++) layout[i] = true;
        }
        public override XRHandSubsystem.UpdateSuccessFlags TryUpdateHands(XRHandSubsystem.UpdateType type,
            ref Pose leftRoot, NativeArray<XRHandJoint> leftJoints, ref Pose rightRoot, NativeArray<XRHandJoint> rightJoints)
        {
            XRHandSubsystem.UpdateSuccessFlags flags = XRHandSubsystem.UpdateSuccessFlags.None;
            if (Tracked[0])
            {
                leftRoot = new Pose(Positions[0], Quaternion.identity);
                Fill(leftJoints, 0, Handedness.Left);
                flags |= XRHandSubsystem.UpdateSuccessFlags.LeftHandRootPose | XRHandSubsystem.UpdateSuccessFlags.LeftHandJoints;
            }
            if (Tracked[1])
            {
                rightRoot = new Pose(Positions[1], Quaternion.identity);
                Fill(rightJoints, 1, Handedness.Right);
                flags |= XRHandSubsystem.UpdateSuccessFlags.RightHandRootPose | XRHandSubsystem.UpdateSuccessFlags.RightHandJoints;
            }
            return flags;
        }
        private void Fill(NativeArray<XRHandJoint> joints, int hand, Handedness side)
        {
            for (int i = 0; i < joints.Length; i++)
                joints[i] = XRHandProviderUtility.CreateJoint(side,
                    !ThumbTracked && i >= XRHandJointID.ThumbMetacarpal.ToIndex() && i <= XRHandJointID.ThumbTip.ToIndex() ? XRHandJointTrackingState.None : XRHandJointTrackingState.Pose,
                    XRHandJointIDUtility.FromIndex(i), new Pose(Positions[hand], Quaternion.identity));
            foreach (XRHandFingerID finger in new[] { XRHandFingerID.Index, XRHandFingerID.Middle, XRHandFingerID.Ring, XRHandFingerID.Little })
            {
                XRHandJointID first = finger.GetFrontJointID();
                float x = ((int)finger - 2f) * .018f;
                Vector3[] points = Closed[hand] && ExtendedFinger[hand] != finger ? new[]
                {
                    new Vector3(x, 0f, -.04f), new Vector3(x, 0f, 0f), new Vector3(x, -.03f, 0f),
                    new Vector3(x, -.03f, -.025f), new Vector3(x, -.01f, -.025f),
                } : new[]
                {
                    new Vector3(x, 0f, -.04f), new Vector3(x, 0f, 0f), new Vector3(x, 0f, .03f),
                    new Vector3(x, 0f, .055f), new Vector3(x, 0f, .075f),
                };
                for (int j = 0; j < points.Length; j++)
                {
                    XRHandJointID joint = first + j;
                    joints[joint.ToIndex()] = XRHandProviderUtility.CreateJoint(side, XRHandJointTrackingState.Pose,
                        joint, new Pose(Positions[hand] + points[j], Quaternion.identity));
                }
            }
        }
    }
}
#endif
