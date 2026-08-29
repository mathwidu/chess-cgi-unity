using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR;

public sealed class XRRig : MonoBehaviour
{
    private const float EyeHeight = 1.2f;
    private static readonly Vector3 SeatPosition = new Vector3(0f, 0f, -3.4f);

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

        GameObject offsetObject = new GameObject("Camera Offset");
        offsetObject.transform.SetParent(originObject.transform, false);

        GameObject cameraObject = new GameObject("Eye Camera");
        cameraObject.transform.SetParent(offsetObject.transform, false);

        eyeCamera = cameraObject.AddComponent<Camera>();
        eyeCamera.nearClipPlane = 0.1f;
        eyeCamera.farClipPlane = 100f;
        cameraObject.AddComponent<AudioListener>();

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

        rigBuilt = true;
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
