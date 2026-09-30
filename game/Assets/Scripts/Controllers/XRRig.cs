using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Interaction.Toolkit.Attachment;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Casters;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;

public sealed class XRRig : MonoBehaviour
{
    private const float EyeHeight = 1.2f;
    private const float OrbitSpeed = 80f;
    private const float ZoomSpeed = 6f;
    private const float MinBoardDistance = 0.35f;
    private const float MaxBoardDistance = 1.2f;
    private const float GrabRadius = 0.06f;
    private static readonly Vector3 GrabPointOffset = new Vector3(0f, -0.02f, 0f);
    private static readonly Vector3 SeatPosition = new Vector3(0f, 0f, -0.6f);
    private static readonly Vector3 BoardTarget = new Vector3(0f, 0.78f, 0f);
    public static readonly Vector3 SeatEyePosition = SeatPosition + Vector3.up * EyeHeight;
    public static Camera EyeCamera { get; private set; }
    public static Transform Origin { get; private set; }
    public static bool SeatedAsBlack { get; private set; }

    public static Vector3 SeatAwarePoint(Vector3 point)
    {
        return SeatedAsBlack ? new Vector3(-point.x, point.y, -point.z) : point;
    }

    private InputController inputController;
    private ChessGameController gameController;
    private Camera desktopCamera;
    private Camera eyeCamera;
    private bool rigBuilt;
    private readonly List<XRPhysicsPusher> physicsPushers = new List<XRPhysicsPusher>();
    private readonly List<XRPhysicalHand> physicalHands = new List<XRPhysicalHand>();
    private readonly List<XRInputSubsystem> trackingSubsystems = new List<XRInputSubsystem>();

    public static bool IsHeadsetPresent =>
        XRSettings.isDeviceActive || InputSystem.GetDevice<XRHMD>() != null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        new GameObject("XR Rig Bootstrap").AddComponent<XRRig>();
    }

    private void Awake()
    {
        inputController = FindFirstObjectByType<InputController>();
        gameController = FindFirstObjectByType<ChessGameController>();
        desktopCamera = Camera.main;
    }

    private void Update()
    {
        if (!rigBuilt)
        {
            if (!IsHeadsetPresent)
            {
                return;
            }

            BuildRig();
            return;
        }

        if (Origin == null)
        {
            return;
        }

        UpdateSeatSide();

        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;

        if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
        {
            Recenter();
        }

        float orbitDirection = 0f;
        if (keyboard != null && keyboard.qKey.isPressed)
        {
            orbitDirection -= 1f;
        }

        if (keyboard != null && keyboard.eKey.isPressed)
        {
            orbitDirection += 1f;
        }

        float scrollDelta = mouse == null ? 0f : mouse.scroll.ReadValue().y * 0.01f;

        ApplyOrbitAndZoom(Origin, orbitDirection, scrollDelta);
    }

    private void UpdateSeatSide()
    {
        if (gameController == null)
        {
            gameController = FindFirstObjectByType<ChessGameController>();
            return;
        }

        bool asBlack = gameController.IsAgainstComputer && !gameController.IsMenuOpen && gameController.HumanSide == ChessSide.Black;
        if (asBlack == SeatedAsBlack)
        {
            return;
        }

        SeatedAsBlack = asBlack;
        RearmPhysicalContacts();
        Origin.RotateAround(BoardTarget, Vector3.up, 180f);
    }

    private void ApplyOrbitAndZoom(Transform subject, float orbitDirection, float scrollDelta)
    {
        if (Mathf.Abs(orbitDirection) > 0f)
        {
            RearmPhysicalContacts();
            subject.RotateAround(BoardTarget, Vector3.up, orbitDirection * OrbitSpeed * Time.deltaTime);
            subject.rotation = Quaternion.LookRotation(BoardTarget - subject.position, Vector3.up);
        }

        if (Mathf.Abs(scrollDelta) > 0.01f)
        {
            RearmPhysicalContacts();
            Vector3 direction = (subject.position - BoardTarget).normalized;
            float distance = Vector3.Distance(subject.position, BoardTarget);
            distance = Mathf.Clamp(distance - scrollDelta * ZoomSpeed, MinBoardDistance, MaxBoardDistance);
            subject.position = BoardTarget + direction * distance;
            subject.rotation = Quaternion.LookRotation(BoardTarget - subject.position, Vector3.up);
        }
    }

    private void BuildRig()
    {
        rigBuilt = true;
        SeatedAsBlack = false;
        bool usingSimulator = InputSystem.GetDevice<XRHMD>() is XRSimulatedHMD;

        GameObject originObject = new GameObject("XR Origin (VR)");
        Vector3 originPosition = usingSimulator ? SeatEyePosition : SeatPosition;
        Quaternion originRotation = usingSimulator
            ? Quaternion.LookRotation((BoardTarget - SeatEyePosition).normalized, Vector3.up)
            : Quaternion.identity;
        originObject.transform.SetPositionAndRotation(originPosition, originRotation);
        Origin = originObject.transform;

        GameObject offsetObject = new GameObject("Camera Offset");
        offsetObject.transform.SetParent(originObject.transform, false);

        GameObject cameraObject = new GameObject("Eye Camera");
        cameraObject.transform.SetParent(offsetObject.transform, false);

        eyeCamera = cameraObject.AddComponent<Camera>();
        eyeCamera.nearClipPlane = 0.1f;
        eyeCamera.farClipPlane = 100f;
        cameraObject.AddComponent<AudioListener>();
        EyeCamera = eyeCamera;

        TrackedPoseDriver poseDriver = cameraObject.AddComponent<TrackedPoseDriver>();
        poseDriver.positionInput = new InputActionProperty(new InputAction(
            "XR HMD Position", InputActionType.Value, "<XRHMD>/centerEyePosition", expectedControlType: "Vector3"));
        poseDriver.rotationInput = new InputActionProperty(new InputAction(
            "XR HMD Rotation", InputActionType.Value, "<XRHMD>/centerEyeRotation", expectedControlType: "Quaternion"));

        XROrigin origin = originObject.AddComponent<XROrigin>();
        origin.Origin = originObject;
        origin.Camera = eyeCamera;
        origin.CameraFloorOffsetObject = offsetObject;
        origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Device;
        origin.CameraYOffset = usingSimulator ? 0f : EyeHeight;

        if (desktopCamera != null)
        {
            desktopCamera.enabled = false;
            AudioListener desktopListener = desktopCamera.GetComponent<AudioListener>();
            if (desktopListener != null)
            {
                desktopListener.enabled = false;
            }
        }

        if (inputController != null)
        {
            inputController.Configure(gameController, eyeCamera);
        }

        GameObject leftController = BuildController(offsetObject.transform, "Left Controller", "LeftHand", "LeftControllerHand");
        GameObject rightController = BuildController(offsetObject.transform, "Right Controller", "RightHand", "RightControllerHand");
        GameObject leftHand = BuildHandInteractor(offsetObject.transform, "LeftHandInteractor");
        GameObject rightHand = BuildHandInteractor(offsetObject.transform, "RightHandInteractor");
        GameObject leftHandVisual = BuildHandVisual(offsetObject.transform, "LeftHandVisual");
        GameObject rightHandVisual = BuildHandVisual(offsetObject.transform, "RightHandVisual");
        BuildTrackedHandPusher(leftHandVisual, leftHand);
        BuildTrackedHandPusher(rightHandVisual, rightHand);

        XRInputModalityManager modalityManager = offsetObject.AddComponent<XRInputModalityManager>();
        modalityManager.leftController = leftController;
        modalityManager.rightController = rightController;
        modalityManager.leftHand = leftHand;
        modalityManager.rightHand = rightHand;

        SubsystemManager.GetSubsystems(trackingSubsystems);
        foreach (XRInputSubsystem subsystem in trackingSubsystems)
            subsystem.trackingOriginUpdated += TrackingOriginUpdated;
    }

    private static GameObject BuildHandInteractor(Transform parent, string resourceName)
    {
        GameObject prefab = Resources.Load<GameObject>($"XR/{resourceName}");
        if (prefab == null)
        {
            Debug.LogWarning($"XRRig could not find Resources/XR/{resourceName}; hand tracking will be unavailable.");
            return null;
        }

        GameObject instance = Object.Instantiate(prefab, parent);
        instance.name = resourceName;
        RestrictRayToUi(instance);

        TrackedPoseDriver aimPoseDriver = instance.transform.Find("Aim Pose")?.GetComponent<TrackedPoseDriver>();
        aimPoseDriver?.positionInput.action?.actionMap?.asset?.Enable();

        return instance;
    }

    private static void RestrictRayToUi(GameObject interactorObject)
    {
        // Contact proxies must never become near-grab targets or block UI rays.
        foreach (SphereInteractionCaster caster in interactorObject.GetComponentsInChildren<SphereInteractionCaster>(true))
        {
            // The caster has a small fixed hit buffer. Board tiles and scenery
            // must not fill it before actual grab targets can be considered.
            caster.physicsLayerMask = (1 << PieceView.PhysicsLayer) | (1 << BoardScaleHandles.PhysicsLayer);
            caster.castRadius = GrabRadius;
        }
        foreach (CurveInteractionCaster caster in interactorObject.GetComponentsInChildren<CurveInteractionCaster>(true))
            caster.raycastMask &= ~((1 << PieceView.PhysicsLayer) | (1 << XRPhysicsPusher.PhysicsLayer) | (1 << BoardScaleHandles.PhysicsLayer));

        NearFarInteractor interactor = interactorObject.GetComponentInChildren<NearFarInteractor>(true);
        if (interactor == null)
        {
            return;
        }

        CurveInteractionCaster farCaster = interactor.farInteractionCaster as CurveInteractionCaster;
        if (farCaster != null)
        {
            farCaster.hitDetectionType = CurveInteractionCaster.HitDetectionType.Raycast;
        }

        CurveVisualController curveVisual = interactorObject.GetComponentInChildren<CurveVisualController>(true);
        if (curveVisual != null)
        {
            curveVisual.curveInteractionDataProvider = new UiOnlyCurveData(interactor);
            KeepRayStraight(curveVisual);
        }
    }

    private static void KeepRayStraight(CurveVisualController curveVisual)
    {
        curveVisual.snapToSelectedAttachIfAvailable = false;
        curveVisual.snapToSnapVolumeIfAvailable = false;
    }

    private static GameObject BuildHandVisual(Transform parent, string resourceName)
    {
        GameObject prefab = Resources.Load<GameObject>($"XR/{resourceName}");
        if (prefab == null)
        {
            Debug.LogWarning($"XRRig could not find Resources/XR/{resourceName}; hand visuals will be unavailable.");
            return null;
        }

        GameObject instance = Object.Instantiate(prefab, parent);
        instance.name = resourceName;
        return instance;
    }

    private GameObject BuildController(Transform parent, string name, string hand, string handModelName)
    {
        GameObject controllerObject = new GameObject(name);
        controllerObject.SetActive(false);
        controllerObject.transform.SetParent(parent, false);

        TrackedPoseDriver poseDriver = controllerObject.AddComponent<TrackedPoseDriver>();
        var positionAction = new InputAction(
            $"XR {hand} Position", InputActionType.Value, $"<XRController>{{{hand}}}/pointerPosition", expectedControlType: "Vector3");
        positionAction.AddBinding($"<XRSimulatedController>{{{hand}}}/devicePosition");
        poseDriver.positionInput = new InputActionProperty(positionAction);
        var rotationAction = new InputAction(
            $"XR {hand} Rotation", InputActionType.Value, $"<XRController>{{{hand}}}/pointerRotation", expectedControlType: "Quaternion");
        rotationAction.AddBinding($"<XRSimulatedController>{{{hand}}}/deviceRotation");
        poseDriver.rotationInput = new InputActionProperty(rotationAction);
        poseDriver.trackingStateInput = new InputActionProperty(new InputAction(
            $"XR {hand} Tracking State", InputActionType.Value, $"<XRController>{{{hand}}}/trackingState", expectedControlType: "Integer"));

        GameObject grabPoint = new GameObject("Grab Point");
        grabPoint.transform.SetParent(controllerObject.transform, false);
        grabPoint.transform.localPosition = GrabPointOffset;

        SphereInteractionCaster nearCaster = controllerObject.AddComponent<SphereInteractionCaster>();
        nearCaster.castOrigin = grabPoint.transform;
        nearCaster.castRadius = GrabRadius;
        nearCaster.physicsLayerMask = (1 << PieceView.PhysicsLayer) | (1 << BoardScaleHandles.PhysicsLayer);
        CurveInteractionCaster farCaster = controllerObject.AddComponent<CurveInteractionCaster>();
        farCaster.raycastMask = ~((1 << PieceView.PhysicsLayer) | (1 << XRPhysicsPusher.PhysicsLayer) | (1 << BoardScaleHandles.PhysicsLayer));
        farCaster.hitDetectionType = CurveInteractionCaster.HitDetectionType.Raycast;
        InteractionAttachController attachController = controllerObject.AddComponent<InteractionAttachController>();

        LineRenderer lineRenderer = controllerObject.AddComponent<LineRenderer>();
        lineRenderer.sharedMaterial = Resources.Load<Material>("XR/ControllerRayMaterial");
        lineRenderer.widthMultiplier = 0.01f;

        NearFarInteractor interactor = controllerObject.AddComponent<NearFarInteractor>();
        interactor.nearInteractionCaster = nearCaster;
        interactor.farInteractionCaster = farCaster;
        interactor.interactionAttachController = attachController;
        interactor.enableNearCasting = true;

        CurveVisualController curveVisual = controllerObject.AddComponent<CurveVisualController>();
        curveVisual.lineRenderer = lineRenderer;
        curveVisual.curveInteractionDataProvider = new UiOnlyCurveData(interactor);
        KeepRayStraight(curveVisual);

        XRInputButtonReader selectInput = new XRInputButtonReader("Select")
        {
            inputSourceMode = XRInputButtonReader.InputSourceMode.InputAction,
            inputActionPerformed = new InputAction(
                $"XR {hand} Select", InputActionType.Button, $"<XRController>{{{hand}}}/triggerButton"),
        };
        interactor.selectInput = selectInput;

        XRInputButtonReader uiPressInput = new XRInputButtonReader("UI Press")
        {
            inputSourceMode = XRInputButtonReader.InputSourceMode.InputAction,
            inputActionPerformed = new InputAction(
                $"XR {hand} UI Press", InputActionType.Button, $"<XRController>{{{hand}}}/triggerButton"),
        };
        interactor.uiPressInput = uiPressInput;

        // The rig is built in code with no input action asset, so bind the controller's
        // OpenXR haptic output directly; the interactor finds this player when it vibrates.
        HapticImpulsePlayer haptics = controllerObject.AddComponent<HapticImpulsePlayer>();
        haptics.hapticOutput = new XRInputHapticImpulseProvider($"XR {hand} Haptic", inputSourceMode: XRInputHapticImpulseProvider.InputSourceMode.InputAction)
        {
            inputAction = new InputAction($"XR {hand} Haptic", InputActionType.PassThrough, $"<XRController>{{{hand}}}/{{Haptic}}"),
        };

        GameObject handModel = BuildHandVisual(controllerObject.transform, handModelName);
        if (handModel != null)
        {
            handModel.AddComponent<ControllerHandPose>().Configure(interactor, hand == "LeftHand");
            XRPhysicalHand physicalHand = handModel.AddComponent<XRPhysicalHand>();
            physicalHand.ConfigureController(handModel.transform, transform, Origin,
                () => ControllerIsTracked(controllerObject, poseDriver), hand == "LeftHand");
            RegisterPhysicalHand(physicalHand);
        }
        else
            BuildPhysicsPusher(name + " Contact", grabPoint.transform,
                () => ControllerIsTracked(controllerObject, poseDriver), .022f);

        controllerObject.SetActive(true);
        return controllerObject;
    }

    private static bool ControllerIsTracked(GameObject controller, TrackedPoseDriver driver)
    {
        var device = driver.positionInput.action?.activeControl?.device as UnityEngine.InputSystem.TrackedDevice;
        const int poseTracked = (int)(InputTrackingState.Position | InputTrackingState.Rotation);
        return controller.activeInHierarchy && driver.isActiveAndEnabled && device != null && device.added &&
            device.isTracked.isPressed && (device.trackingState.ReadValue() & poseTracked) == poseTracked;
    }

    private void BuildTrackedHandPusher(GameObject visual, GameObject handInteractor)
    {
        if (visual == null || handInteractor == null) return;
        XRHandSkeletonDriver skeleton = visual.GetComponentInChildren<XRHandSkeletonDriver>(true);
        if (skeleton == null || skeleton.handTrackingEvents == null || skeleton.rootTransform == null) return;
        XRPhysicalHand physicalHand = visual.AddComponent<XRPhysicalHand>();
        physicalHand.ConfigureTrackedHand(skeleton, handInteractor, visual.transform.parent, transform, Origin);
        RegisterPhysicalHand(physicalHand);
    }

    private void RegisterPhysicalHand(XRPhysicalHand hand)
    {
        physicalHands.Add(hand);
        for (int i = 0; i < hand.Contacts.Count; i++) physicsPushers.Add(hand.Contacts[i]);
    }

    private void BuildPhysicsPusher(string name, Transform source, System.Func<bool> tracked, float radius)
    {
        // Keep rigidbodies outside the tracked hierarchy. Origin/bone transforms
        // can jump at recentering; only FixedUpdate moves an armed contact proxy.
        var contact = new GameObject(name);
        contact.transform.SetParent(transform, false);
        XRPhysicsPusher pusher = contact.AddComponent<XRPhysicsPusher>();
        pusher.Configure(source, tracked, Origin, radius);
        physicsPushers.Add(pusher);
    }

    private void RearmPhysicalContacts()
    {
        foreach (XRPhysicalHand hand in physicalHands)
            if (hand != null) hand.RequireRearm();
        foreach (XRPhysicsPusher pusher in physicsPushers)
            if (pusher != null) pusher.RequireRearm();
    }

    private void TrackingOriginUpdated(XRInputSubsystem subsystem) => RearmPhysicalContacts();

    private void OnDestroy()
    {
        foreach (XRInputSubsystem subsystem in trackingSubsystems)
            subsystem.trackingOriginUpdated -= TrackingOriginUpdated;
    }

    private void Recenter()
    {
        RearmPhysicalContacts();
        var inputSubsystems = new List<XRInputSubsystem>();
        SubsystemManager.GetSubsystems(inputSubsystems);
        for (int i = 0; i < inputSubsystems.Count; i++)
        {
            inputSubsystems[i].TryRecenter();
        }
    }
}
