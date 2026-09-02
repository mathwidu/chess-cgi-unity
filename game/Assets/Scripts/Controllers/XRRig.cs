using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Attachment;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Casters;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;

public sealed class XRRig : MonoBehaviour
{
    private const float EyeHeight = 1.2f;
    private static readonly Vector3 SeatPosition = new Vector3(0f, 0f, -3.4f);
    public static readonly Vector3 SeatEyePosition = SeatPosition + Vector3.up * EyeHeight;
    public static Camera EyeCamera { get; private set; }
    public static Transform Origin { get; private set; }

    private InputController inputController;
    private ChessGameController gameController;
    private Camera desktopCamera;
    private Camera eyeCamera;
    private bool rigBuilt;

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

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
        {
            Recenter();
        }
    }

    private void BuildRig()
    {
        GameObject originObject = new GameObject("XR Origin (Vive)");
        originObject.transform.SetPositionAndRotation(SeatPosition, Quaternion.identity);
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
        origin.CameraYOffset = EyeHeight;

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

        GameObject leftController = BuildController(offsetObject.transform, "Left Controller", "LeftHand");
        GameObject rightController = BuildController(offsetObject.transform, "Right Controller", "RightHand");
        GameObject leftHand = BuildHandInteractor(offsetObject.transform, "LeftHandInteractor");
        GameObject rightHand = BuildHandInteractor(offsetObject.transform, "RightHandInteractor");
        BuildHandVisual(offsetObject.transform, "LeftHandVisual");
        BuildHandVisual(offsetObject.transform, "RightHandVisual");

        XRInputModalityManager modalityManager = offsetObject.AddComponent<XRInputModalityManager>();
        modalityManager.leftController = leftController;
        modalityManager.rightController = rightController;
        modalityManager.leftHand = leftHand;
        modalityManager.rightHand = rightHand;

        rigBuilt = true;
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

        TrackedPoseDriver aimPoseDriver = instance.transform.Find("Aim Pose")?.GetComponent<TrackedPoseDriver>();
        aimPoseDriver?.positionInput.action?.actionMap?.asset?.Enable();

        return instance;
    }

    private static void BuildHandVisual(Transform parent, string resourceName)
    {
        GameObject prefab = Resources.Load<GameObject>($"XR/{resourceName}");
        if (prefab == null)
        {
            Debug.LogWarning($"XRRig could not find Resources/XR/{resourceName}; hand visuals will be unavailable.");
            return;
        }

        GameObject instance = Object.Instantiate(prefab, parent);
        instance.name = resourceName;
    }

    private static GameObject BuildController(Transform parent, string name, string hand)
    {
        GameObject controllerObject = new GameObject(name);
        controllerObject.SetActive(false);
        controllerObject.transform.SetParent(parent, false);

        TrackedPoseDriver poseDriver = controllerObject.AddComponent<TrackedPoseDriver>();
        poseDriver.positionInput = new InputActionProperty(new InputAction(
            $"XR {hand} Position", InputActionType.Value, $"<XRController>{{{hand}}}/pointerPosition", expectedControlType: "Vector3"));
        poseDriver.rotationInput = new InputActionProperty(new InputAction(
            $"XR {hand} Rotation", InputActionType.Value, $"<XRController>{{{hand}}}/pointerRotation", expectedControlType: "Quaternion"));

        SphereInteractionCaster nearCaster = controllerObject.AddComponent<SphereInteractionCaster>();
        CurveInteractionCaster farCaster = controllerObject.AddComponent<CurveInteractionCaster>();
        InteractionAttachController attachController = controllerObject.AddComponent<InteractionAttachController>();

        LineRenderer lineRenderer = controllerObject.AddComponent<LineRenderer>();
        lineRenderer.material = CreateRayMaterial();
        controllerObject.AddComponent<XRInteractorLineVisual>();

        NearFarInteractor interactor = controllerObject.AddComponent<NearFarInteractor>();
        interactor.nearInteractionCaster = nearCaster;
        interactor.farInteractionCaster = farCaster;
        interactor.interactionAttachController = attachController;
        interactor.enableNearCasting = false;

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

        controllerObject.SetActive(true);
        return controllerObject;
    }

    private static Material CreateRayMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        return new Material(shader) { color = Color.cyan };
    }

    private static void Recenter()
    {
        var inputSubsystems = new List<XRInputSubsystem>();
        SubsystemManager.GetSubsystems(inputSubsystems);
        for (int i = 0; i < inputSubsystems.Count; i++)
        {
            inputSubsystems[i].TryRecenter();
        }
    }
}
