using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[InitializeOnLoad]
public static class XRHandVerification
{
    private const string ArmedKey = "ChessCgiXrHandCheckArmed";
    private const string DoneKey = "ChessCgiXrHandCheckDone";
    private const string ExitCodeKey = "ChessCgiXrHandCheckExitCode";
    private const string MainScenePath = "Assets/Scenes/Main.unity";
    private const int FramesToRun = 30;

    private static int frameCount;
    private static readonly XRVerificationResult result = new XRVerificationResult();

    static XRHandVerification()
    {
        if (SessionState.GetBool(DoneKey, false) && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            SessionState.SetBool(DoneKey, false);
            EditorApplication.Exit(SessionState.GetInt(ExitCodeKey, 1));
            return;
        }

        if (SessionState.GetBool(ArmedKey, false))
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }
    }

    [MenuItem("Chess CGI/VR/Run XR Hand Simulator Check")]
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
        GameObject originObject = GameObject.Find("XR Origin (Vive)");
        Transform cameraOffset = originObject != null ? originObject.transform.Find("Camera Offset") : null;

        GameObject leftHand = cameraOffset != null ? cameraOffset.Find("LeftHandInteractor")?.gameObject : null;
        GameObject rightHand = cameraOffset != null ? cameraOffset.Find("RightHandInteractor")?.gameObject : null;
        GameObject leftController = cameraOffset != null ? cameraOffset.Find("Left Controller")?.gameObject : null;
        GameObject rightController = cameraOffset != null ? cameraOffset.Find("Right Controller")?.gameObject : null;
        XRInputModalityManager modalityManager = cameraOffset != null ? cameraOffset.GetComponent<XRInputModalityManager>() : null;

        NearFarInteractor leftHandNearFar = leftHand != null ? leftHand.GetComponentInChildren<NearFarInteractor>(true) : null;
        XRPokeInteractor leftHandPoke = leftHand != null ? leftHand.GetComponentInChildren<XRPokeInteractor>(true) : null;

        Debug.Log("CHESS_CGI_XR_HAND_CHECK " +
            $"leftHandFound={leftHand != null} rightHandFound={rightHand != null} " +
            $"leftHandNearFarFound={leftHandNearFar != null} leftHandPokeFound={leftHandPoke != null} " +
            $"modalityManagerFound={modalityManager != null} " +
            $"currentInputMode={XRInputModalityManager.currentInputMode.Value}");

        result.Check(leftHand != null, "LeftHandInteractor should be built under Camera Offset");
        result.Check(rightHand != null, "RightHandInteractor should be built under Camera Offset");
        result.Check(leftHandNearFar != null, "the left hand interactor should include a NearFarInteractor");
        result.Check(leftHandPoke != null, "the left hand interactor should include an XRPokeInteractor");
        result.Check(modalityManager != null, "an XRInputModalityManager should be present on Camera Offset");
        result.Check(modalityManager != null && modalityManager.leftHand == leftHand, "XRInputModalityManager.leftHand should reference the built left hand interactor");
        result.Check(modalityManager != null && modalityManager.rightHand == rightHand, "XRInputModalityManager.rightHand should reference the built right hand interactor");
        result.Check(modalityManager != null && modalityManager.leftController == leftController, "XRInputModalityManager.leftController should reference the built left controller");
        result.Check(modalityManager != null && modalityManager.rightController == rightController, "XRInputModalityManager.rightController should reference the built right controller");

        result.LogSummary("CHESS_CGI_XR_HAND_CHECK");
        SessionState.SetInt(ExitCodeKey, result.Passed ? 0 : 1);
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
        EditorApplication.Exit(SessionState.GetInt(ExitCodeKey, 1));
    }
}
