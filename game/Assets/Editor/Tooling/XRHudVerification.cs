using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.XR;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;

[InitializeOnLoad]
public static class XRHudVerification
{
    private const string ArmedKey = "ChessCgiXrHudCheckArmed";
    private const string DoneKey = "ChessCgiXrHudCheckDone";
    private const string ExitCodeKey = "ChessCgiXrHudCheckExitCode";
    private const string MainScenePath = "Assets/Scenes/Main.unity";
    private const int RigSettleFrames = 30;
    private const int HoldSimFrames = 5;

    private enum Stage
    {
        WaitForRig,
        PressButton,
        HoldButton,
        ReleaseButton,
        VerifyClick,
    }

    private static Stage stage;
    private static int frameCount;
    private static int holdStartSimFrame;
    private static NearFarInteractor interactor;
    private static GameObject startPlayButton;
    private static GameObject startOverlay;
    private static readonly XRVerificationResult result = new XRVerificationResult();

    static XRHudVerification()
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

    [MenuItem("Chess CGI/VR/Run XR Hud Simulator Check")]
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
        stage = Stage.WaitForRig;
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

        switch (stage)
        {
            case Stage.WaitForRig:
                if (frameCount < RigSettleFrames)
                {
                    return;
                }

                if (!TryBeginPressStartButton())
                {
                    FailAndStop("Could not find the world-space HUD canvas, its start button, or the right controller.");
                    return;
                }

                stage = Stage.PressButton;
                return;

            case Stage.PressButton:
                holdStartSimFrame = Time.frameCount;
                stage = Stage.HoldButton;
                return;

            case Stage.HoldButton:
                interactor.uiPressInput.manualPerformed = true;
                interactor.uiPressInput.manualFramePerformed = Time.frameCount;
                if (Time.frameCount - holdStartSimFrame < HoldSimFrames)
                {
                    return;
                }

                LogUiModel("duringHold");
                stage = Stage.ReleaseButton;
                return;

            case Stage.ReleaseButton:
                interactor.uiPressInput.manualPerformed = false;
                holdStartSimFrame = Time.frameCount;
                stage = Stage.VerifyClick;
                return;

            case Stage.VerifyClick:
                if (Time.frameCount - holdStartSimFrame < HoldSimFrames)
                {
                    return;
                }

                ReportAndStop();
                return;
        }
    }

    private static bool TryBeginPressStartButton()
    {
        GameHud hud = Object.FindFirstObjectByType<GameHud>();
        Canvas canvas = hud != null ? hud.GetComponent<Canvas>() : null;
        EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>();
        GameObject controllerObject = GameObject.Find("Right Controller");
        interactor = controllerObject != null ? controllerObject.GetComponent<NearFarInteractor>() : null;

        Transform buttonTransform = hud != null
            ? hud.transform.Find("HudRoot/StartOverlay/StartCard/StartPlayButton")
            : null;
        startPlayButton = buttonTransform != null ? buttonTransform.gameObject : null;
        Transform overlayTransform = hud != null ? hud.transform.Find("HudRoot/StartOverlay") : null;
        startOverlay = overlayTransform != null ? overlayTransform.gameObject : null;

        bool trackedRaycasterFound = hud != null && hud.GetComponent<TrackedDeviceGraphicRaycaster>() != null;
        bool legacyRaycasterFound = hud != null && hud.GetComponent<GraphicRaycaster>() != null;
        bool xrInputModuleFound = eventSystem != null && eventSystem.GetComponent<XRUIInputModule>() != null;

        Debug.Log("CHESS_CGI_XR_HUD_CHECK " +
            $"canvasFound={canvas != null} renderMode={(canvas != null ? canvas.renderMode.ToString() : "n/a")} " +
            $"trackedRaycasterFound={trackedRaycasterFound} " +
            $"legacyRaycasterFound={legacyRaycasterFound} " +
            $"eventSystemFound={eventSystem != null} " +
            $"xrInputModuleFound={xrInputModuleFound} " +
            $"buttonFound={startPlayButton != null} controllerFound={controllerObject != null} interactorFound={interactor != null}");

        result.Check(canvas != null, "the HUD canvas should be found");
        result.Check(canvas != null && canvas.renderMode == RenderMode.WorldSpace, "the HUD canvas should render in world space");
        result.Check(trackedRaycasterFound, "the HUD canvas should have a TrackedDeviceGraphicRaycaster");
        result.Check(!legacyRaycasterFound, "the HUD canvas should not have a legacy GraphicRaycaster");
        result.Check(eventSystem != null, "an EventSystem should be found");
        result.Check(xrInputModuleFound, "the EventSystem should have an XRUIInputModule");
        result.Check(startPlayButton != null, "the start play button should be found");
        result.Check(controllerObject != null, "the right controller should be found");
        result.Check(interactor != null, "the right controller's NearFarInteractor should be found");

        if (canvas == null || startPlayButton == null || overlayTransform == null || interactor == null)
        {
            return false;
        }

        interactor.uiPressInput.inputSourceMode =
            UnityEngine.XR.Interaction.Toolkit.Inputs.Readers.XRInputButtonReader.InputSourceMode.ManualValue;
        interactor.enableUIInteraction = true;

        AimControllerAt(startPlayButton.transform.position);
        LogUiModel("afterAim");
        return true;
    }

    private static void LogUiModel(string when)
    {
        bool gotModel = interactor.TryGetUIModel(out TrackedDeviceModel model);
        GameObject raycastTarget = model.currentRaycast.isValid ? model.currentRaycast.gameObject : null;
        Debug.Log("CHESS_CGI_XR_HUD_CHECK " +
            $"uiModel[{when}] gotModel={gotModel} enableUIInteraction={interactor.enableUIInteraction} " +
            $"raycastValid={model.currentRaycast.isValid} raycastTarget={(raycastTarget != null ? raycastTarget.name : "none")} " +
            $"select={model.select} position={model.position.ToString("F2")} " +
            $"controllerPos={GameObject.Find("Right Controller")?.transform.position.ToString("F2")}");

        if (when == "duringHold")
        {
            result.Check(model.currentRaycast.isValid, "the UI raycast should be valid while aiming at the start button");
            result.Check(raycastTarget != null && raycastTarget.name == "StartPlayButton",
                $"the UI raycast should hit StartPlayButton, hit {(raycastTarget != null ? raycastTarget.name : "none")} instead");
        }
    }

    private static void ReportAndStop()
    {
        LogUiModel("afterRelease");
        bool startOverlayHiddenAfterClick = startOverlay != null && !startOverlay.activeSelf;
        Debug.Log("CHESS_CGI_XR_HUD_CHECK " +
            $"startOverlayHiddenAfterClick={startOverlayHiddenAfterClick}");
        result.Check(startOverlayHiddenAfterClick, "the start overlay should hide after the button click completes");

        result.LogSummary("CHESS_CGI_XR_HUD_CHECK");
        SessionState.SetInt(ExitCodeKey, result.Passed ? 0 : 1);
        EditorApplication.update -= Tick;
        SessionState.SetBool(ArmedKey, false);
        SessionState.SetBool(DoneKey, true);
        EditorApplication.isPlaying = false;
        EditorApplication.update += WaitForEditModeThenExit;
    }

    private static void AimControllerAt(Vector3 worldTarget)
    {
        GameObject controllerObject = GameObject.Find("Right Controller");
        if (controllerObject == null)
        {
            return;
        }

        TrackedPoseDriver poseDriver = controllerObject.GetComponent<TrackedPoseDriver>();
        if (poseDriver != null)
        {
            poseDriver.enabled = false;
        }

        Vector3 aimOrigin = XRRig.SeatEyePosition + new Vector3(0.25f, -0.2f, 0.1f);
        controllerObject.transform.SetPositionAndRotation(
            aimOrigin,
            Quaternion.LookRotation((worldTarget - aimOrigin).normalized, Vector3.up));
    }

    private static void FailAndStop(string reason)
    {
        Debug.LogError($"CHESS_CGI_XR_HUD_CHECK FAILED reason=\"{reason}\"");
        SessionState.SetInt(ExitCodeKey, 1);
        EditorApplication.update -= Tick;
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
