#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unity.Collections;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Hands.ProviderImplementation;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Casters;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;
using UnityEngine.InputSystem.XR;

public sealed class XRInputModalityTests
{
    private XRSimulatedHMD headset;
    private XRSimulatedController leftController;
    private XRSimulatedController rightController;
    private ReplaySubsystem hands;
    private XRInputModalityManager manager;
    private GameObject leftVisual;
    private GameObject rightVisual;

    private IEnumerator LoadRig(bool controllersTracked, bool handsTracked)
    {
        var descriptors = new List<XRHandSubsystemDescriptor>();
        SubsystemManager.GetSubsystemDescriptors(descriptors);
        const string id = "ChessCgi.ModalityReplay";
        var descriptor = descriptors.Find(item => item.id == id);
        if (descriptor == null)
        {
            XRHandSubsystemDescriptor.Register(new XRHandSubsystemDescriptor.Cinfo
            {
                id = id, providerType = typeof(ReplayProvider), subsystemTypeOverride = typeof(ReplaySubsystem),
            });
            descriptors.Clear();
            SubsystemManager.GetSubsystemDescriptors(descriptors);
            descriptor = descriptors.Find(item => item.id == id);
        }
        hands = (ReplaySubsystem)descriptor.Create();
        hands.Start();
        SetHandsTracked(handsTracked);
        headset = InputSystem.AddDevice<XRSimulatedHMD>();
        if (controllersTracked) AddControllers();
        EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/Main.unity", new LoadSceneParameters(LoadSceneMode.Single));
        yield return null;
        if (Object.FindFirstObjectByType<XRRig>() == null)
            new GameObject("Modality test bootstrap").AddComponent<XRRig>();
        yield return null;
        yield return null;
        manager = Object.FindFirstObjectByType<XRInputModalityManager>();
        Assert.That(manager, Is.Not.Null);
        var transforms = XRRig.Origin.GetComponentsInChildren<Transform>(true);
        leftVisual = transforms.Single(item => item.name == "LeftHandVisual").gameObject;
        rightVisual = transforms.Single(item => item.name == "RightHandVisual").gameObject;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        RemoveControllers();
        if (headset != null && headset.added) InputSystem.RemoveDevice(headset);
        Scene previous = SceneManager.GetActiveScene();
        SceneManager.SetActiveScene(SceneManager.CreateScene("Empty after modality test"));
        yield return SceneManager.UnloadSceneAsync(previous);
        if (hands != null) { hands.Stop(); hands.Destroy(); }
    }

    [UnityTest]
    public IEnumerator ControllersTrackedBeforeStartupHideBothTrackedHandVisuals()
    {
        yield return LoadRig(true, false);
        AssertMode(false, true);
    }

    [UnityTest]
    public IEnumerator HandsTrackedBeforeStartupHideBothControllerModels()
    {
        yield return LoadRig(false, true);
        AssertMode(true, false);
    }

    [UnityTest]
    public IEnumerator TrackingTransitionsKeepVisualsAndContactsInTheirActiveMode()
    {
        yield return LoadRig(false, false);
        AssertMode(false, false);
        SetHandsTracked(true);
        yield return null;
        AssertMode(true, false);
        for (int i = 0; i < 8; i++)
        {
            SetHandsTracked(true);
            yield return new WaitForFixedUpdate();
        }
        foreach (GameObject visual in new[] { leftVisual, rightVisual })
            Assert.That(visual.GetComponent<XRPhysicalHand>().Contacts[0].IsArmed, Is.True,
                "The tracked palm must be armed before switching away from hands");
        AddControllers();
        yield return null;
        yield return null;
        AssertMode(false, true);
        SetHandsTracked(false);
        RemoveControllers();
        yield return null;
        AssertMode(false, false);
        SetHandsTracked(true);
        yield return null;
        AssertMode(true, false);
    }

    [UnityTest]
    public IEnumerator HandMenuRaysStartAtTheirAimPoseAndFollowEachHand()
    {
        yield return LoadRig(false, true);
        foreach (GameObject hand in new[] { manager.leftHand, manager.rightHand })
        {
            CurveVisualController visual = AimHandAtMenu(hand);
            Transform aim = visual.curveInteractionDataProvider.curveOrigin;
            var line = visual.GetComponent<LineRenderer>();
            visual.curveStartOffset = 0f;
            visual.SendMessage("OnBeforeRenderLineVisual");
            AssertRayStartsAtAim(line, aim);

            aim.position += new Vector3(.12f, .08f, 0f);
            visual.SendMessage("OnBeforeRenderLineVisual");
            AssertRayStartsAtAim(line, aim);

            Camera eye = XRRig.EyeCamera;
            eye.GetComponent<TrackedPoseDriver>().enabled = false;
            eye.transform.position += new Vector3(.1f, 0f, 0f);
            visual.SendMessage("OnBeforeRenderLineVisual");
            AssertRayStartsAtAim(line, aim);
        }
    }

    [UnityTest]
    public IEnumerator DisablingFarInteractionAlsoHidesTheHandMenuRay()
    {
        yield return LoadRig(false, true);
        foreach (GameObject hand in new[] { manager.leftHand, manager.rightHand })
        {
            CurveVisualController visual = AimHandAtMenu(hand);
            Assert.That(visual.curveInteractionDataProvider.isActive, Is.True);
            hand.GetComponentInChildren<NearFarInteractor>(true).enableFarCasting = false;
            visual.SendMessage("OnBeforeRenderLineVisual");
            Assert.That(visual.GetComponent<LineRenderer>().enabled, Is.False,
                "A hand that cannot point at UI must not show a selection ray");
        }
    }

    private static CurveVisualController AimHandAtMenu(GameObject hand)
    {
        foreach (TrackedPoseDriver driver in hand.GetComponentsInChildren<TrackedPoseDriver>(true))
            driver.enabled = false;
        NearFarInteractor interactor = hand.GetComponentInChildren<NearFarInteractor>(true);
        var far = (CurveInteractionCaster)interactor.farInteractionCaster;
        far.enableStabilization = false;
        interactor.enableFarCasting = true;
        var panel = Object.FindFirstObjectByType<GameHud>().GetComponent<RectTransform>();
        Vector3 target = panel.TransformPoint(panel.rect.center);
        far.castOrigin.position = target - panel.forward * .8f + panel.right * (hand.name.StartsWith("Left") ? -.3f : .3f);
        far.castOrigin.rotation = Quaternion.LookRotation(target - far.castOrigin.position, panel.up);
        Physics.SyncTransforms();
        return hand.GetComponentInChildren<CurveVisualController>(true);
    }

    private static void AssertRayStartsAtAim(LineRenderer line, Transform aim)
    {
        Assert.That(line.enabled, Is.True, "The fixture must aim at the real menu");
        Assert.That(line.positionCount, Is.GreaterThan(1));
        Vector3 start = line.useWorldSpace ? line.GetPosition(0) : line.transform.TransformPoint(line.GetPosition(0));
        Assert.That(Vector3.Distance(start, aim.position), Is.LessThan(.002f),
            "The visible ray must start at the same tracked origin used for menu selection");
    }

    private void AssertMode(bool handMode, bool controllerMode)
    {
        Assert.That(manager.leftHand.activeInHierarchy, Is.EqualTo(handMode), "Left hand input mode");
        Assert.That(manager.rightHand.activeInHierarchy, Is.EqualTo(handMode), "Right hand input mode");
        Assert.That(manager.leftController.activeInHierarchy, Is.EqualTo(controllerMode), "Left controller mode");
        Assert.That(manager.rightController.activeInHierarchy, Is.EqualTo(controllerMode), "Right controller mode");
        Assert.That(leftVisual.activeInHierarchy, Is.EqualTo(handMode), "Left tracked mesh must not survive outside its input mode");
        Assert.That(rightVisual.activeInHierarchy, Is.EqualTo(handMode), "Right tracked mesh must not survive outside its input mode");
        foreach (GameObject visual in new[] { leftVisual, rightVisual })
        {
            Assert.That(visual.GetComponentsInChildren<Renderer>(true).Length, Is.GreaterThan(0));
            if (handMode)
                Assert.That(visual.GetComponentsInChildren<Renderer>().Any(renderer => renderer.enabled), Is.True,
                    "Tracked hands must have a visible mesh when their mode is active");
            if (!handMode)
                foreach (XRPhysicsPusher contact in visual.GetComponent<XRPhysicalHand>().Contacts)
                    Assert.That(contact.IsArmed, Is.False, "Hidden hands must not press table buttons");
        }
        foreach (GameObject controller in new[] { manager.leftController, manager.rightController })
            Assert.That(controller.GetComponentInChildren<ControllerHandPose>(true).gameObject.activeInHierarchy,
                Is.EqualTo(controllerMode), "Controller hand model follows the same switch as its interactor");
    }

    private void AddControllers()
    {
        leftController = InputSystem.AddDevice<XRSimulatedController>();
        rightController = InputSystem.AddDevice<XRSimulatedController>();
        InputSystem.SetDeviceUsage(leftController, CommonUsages.LeftHand);
        InputSystem.SetDeviceUsage(rightController, CommonUsages.RightHand);
        foreach (var controller in new[] { leftController, rightController })
            InputSystem.QueueStateEvent(controller, new XRSimulatedControllerState
            {
                isTracked = true, trackingState = 3, devicePosition = new Vector3(0f, .3f, .2f), deviceRotation = Quaternion.identity,
            });
        InputSystem.Update();
    }

    private void RemoveControllers()
    {
        if (leftController != null && leftController.added) InputSystem.RemoveDevice(leftController);
        if (rightController != null && rightController.added) InputSystem.RemoveDevice(rightController);
        leftController = rightController = null;
    }

    private void SetHandsTracked(bool tracked)
    {
        hands.Replay.Tracked = tracked;
        hands.TryUpdateHands(XRHandSubsystem.UpdateType.Dynamic);
        hands.TryUpdateHands(XRHandSubsystem.UpdateType.BeforeRender);
    }

    public sealed class ReplaySubsystem : XRHandSubsystem
    {
        public ReplayProvider Replay => (ReplayProvider)provider;
    }

    public sealed class ReplayProvider : XRHandSubsystemProvider
    {
        public bool Tracked;
        public override void Start() { }
        public override void Stop() { }
        public override void Destroy() { }
        public override void GetHandLayout(NativeArray<bool> layout)
        {
            for (int i = 0; i < layout.Length; i++) layout[i] = true;
        }
        public override XRHandSubsystem.UpdateSuccessFlags TryUpdateHands(XRHandSubsystem.UpdateType updateType,
            ref Pose leftRoot, NativeArray<XRHandJoint> leftJoints, ref Pose rightRoot, NativeArray<XRHandJoint> rightJoints)
        {
            if (!Tracked) return XRHandSubsystem.UpdateSuccessFlags.None;
            leftRoot = new Pose(new Vector3(-.2f, .3f, .2f), Quaternion.identity);
            rightRoot = new Pose(new Vector3(.2f, .3f, .2f), Quaternion.identity);
            for (int i = 0; i < leftJoints.Length; i++)
            {
                var joint = XRHandJointIDUtility.FromIndex(i);
                leftJoints[i] = XRHandProviderUtility.CreateJoint(Handedness.Left, XRHandJointTrackingState.Pose, joint, leftRoot);
                rightJoints[i] = XRHandProviderUtility.CreateJoint(Handedness.Right, XRHandJointTrackingState.Pose, joint, rightRoot);
            }
            return XRHandSubsystem.UpdateSuccessFlags.All;
        }
    }
}
#endif
