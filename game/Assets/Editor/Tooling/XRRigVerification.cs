using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;

[InitializeOnLoad]
public static class XRRigVerification
{
    private const string ArmedKey = "ChessCgiXrRigCheckArmed";
    private const string DoneKey = "ChessCgiXrRigCheckDone";
    private const string MainScenePath = "Assets/Scenes/Main.unity";
    private const int FramesToRun = 30;

    private static int frameCount;

    static XRRigVerification()
    {
        if (SessionState.GetBool(DoneKey, false) && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            SessionState.SetBool(DoneKey, false);
            EditorApplication.Exit(0);
            return;
        }

        if (SessionState.GetBool(ArmedKey, false))
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }
    }

    [MenuItem("Chess CGI/VR/Run XR Rig Simulator Check")]
    public static void RunSimulatorCheck()
    {
        EditorSceneManager.OpenScene(MainScenePath);
        SessionState.SetBool(ArmedKey, true);
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        EditorApplication.isPlaying = true;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(ArmedKey, false))
        {
            return;
        }

        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        frameCount = 0;
        EditorApplication.update += Tick;
    }

    private static void Tick()
    {
        if (!EditorApplication.isPlaying)
        {
            EditorApplication.update -= Tick;
            return;
        }

        frameCount++;
        if (frameCount < FramesToRun)
        {
            return;
        }

        EditorApplication.update -= Tick;
        ReportAndStop();
    }

    private static void ReportAndStop()
    {
        XRHMD hmd = InputSystem.GetDevice<XRHMD>();
        bool headsetPresent = XRRig.IsHeadsetPresent;
        GameObject originObject = GameObject.Find("XR Origin (Vive)");

        Debug.Log($"CHESS_CGI_XR_RIG_CHECK headsetPresent={headsetPresent} hmdDevice={(hmd != null ? hmd.displayName : "none")} originBuilt={originObject != null}");

        if (originObject != null)
        {
            Transform eyeCameraTransform = originObject.transform.Find("Camera Offset/Eye Camera");
            Camera eyeCamera = eyeCameraTransform != null ? eyeCameraTransform.GetComponent<Camera>() : null;
            Unity.XR.CoreUtils.XROrigin origin = originObject.GetComponent<Unity.XR.CoreUtils.XROrigin>();

            Debug.Log($"CHESS_CGI_XR_RIG_CHECK eyeCameraFound={eyeCamera != null} " +
                $"trackedPoseDriverFound={(eyeCameraTransform != null && eyeCameraTransform.GetComponent<TrackedPoseDriver>() != null)} " +
                $"originCameraAssigned={(origin != null && origin.Camera == eyeCamera)} " +
                $"trackingOriginMode={(origin != null ? origin.RequestedTrackingOriginMode.ToString() : "n/a")} " +
                $"cameraYOffset={(origin != null ? origin.CameraYOffset.ToString("0.00") : "n/a")}");
        }

        GameObject desktopCameraObject = GameObject.Find("Main Camera");
        if (desktopCameraObject != null)
        {
            Camera desktopCamera = desktopCameraObject.GetComponent<Camera>();
            AudioListener desktopListener = desktopCameraObject.GetComponent<AudioListener>();
            Debug.Log($"CHESS_CGI_XR_RIG_CHECK desktopCameraEnabled={(desktopCamera != null && desktopCamera.enabled)} " +
                $"desktopListenerEnabled={(desktopListener != null && desktopListener.enabled)}");
        }

        InputController inputController = Object.FindFirstObjectByType<InputController>();
        if (inputController != null)
        {
            FieldInfo field = typeof(InputController).GetField("raycastCamera", BindingFlags.NonPublic | BindingFlags.Instance);
            Camera raycastCamera = field != null ? field.GetValue(inputController) as Camera : null;
            Debug.Log($"CHESS_CGI_XR_RIG_CHECK raycastCameraName={(raycastCamera != null ? raycastCamera.name : "none")}");
        }

        SessionState.SetBool(ArmedKey, false);
        SessionState.SetBool(DoneKey, true);
        EditorApplication.isPlaying = false;
        EditorApplication.update += WaitForEditModeThenExit;
    }

    private static void WaitForEditModeThenExit()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        EditorApplication.update -= WaitForEditModeThenExit;
        SessionState.SetBool(DoneKey, false);
        EditorApplication.Exit(0);
    }
}
