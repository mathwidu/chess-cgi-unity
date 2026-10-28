using System.Collections.Generic;
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
        CheckUnblockedRay,
        CheckBlockedRay,
    }

    private static Stage stage;
    private static int frameCount;
    private static int holdStartSimFrame;
    private static NearFarInteractor interactor;
    private static GameObject startPlayButton;
    private static GameObject startOverlay;
    private static GameObject newGameButton;
    private static GameObject rayBlocker;
    private static int stageStartFrame;
    // Above the board, so only the test blocker can stand between the hand and the HUD.
    private static readonly Vector3 HighAimOrigin = new Vector3(0.25f, 1.3f, -0.5f);
    private static readonly XRVerificationResult result = new XRVerificationResult();

    static XRHudVerification()
    {
        if (SessionState.GetBool(DoneKey, false) && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            SessionState.SetBool(DoneKey, false);
            XRSimulatorSetup.SetAutomaticInstantiate(false);
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
        XRSimulatorSetup.SetAutomaticInstantiate(true);
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

                VerifyStartClick();
                BeginRayGuardCheck();
                return;

            case Stage.CheckUnblockedRay:
                if (Time.frameCount - stageStartFrame < HoldSimFrames)
                {
                    return;
                }

                CheckRayHitsNewGame(true);
                // A collider between the hand and the HUD must stop the ray, like the board and table do.
                rayBlocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rayBlocker.name = "HudRayBlocker";
                rayBlocker.transform.position = Vector3.Lerp(HighAimOrigin, GetRectWorldCenter((RectTransform)newGameButton.transform), 0.3f);
                rayBlocker.transform.localScale = Vector3.one * 0.3f;
                // Batch frames can outrun the physics step; register the moved collider now.
                Physics.SyncTransforms();
                stageStartFrame = Time.frameCount;
                stage = Stage.CheckBlockedRay;
                return;

            case Stage.CheckBlockedRay:
                if (Time.frameCount - stageStartFrame < HoldSimFrames)
                {
                    return;
                }

                CheckRayHitsNewGame(false);
                Object.Destroy(rayBlocker);
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
            ? hud.transform.Find("HudRoot/StartOverlay/MenuContent/StartCard/StartPlayButton")
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

        CheckCanvasPlacement(canvas);

        if (canvas == null || startPlayButton == null || overlayTransform == null || interactor == null)
        {
            return false;
        }

        interactor.uiPressInput.inputSourceMode =
            UnityEngine.XR.Interaction.Toolkit.Inputs.Readers.XRInputButtonReader.InputSourceMode.ManualValue;
        interactor.enableUIInteraction = true;

        AimControllerAt(GetRectWorldCenter(startPlayButton.GetComponent<RectTransform>()));
        LogUiModel("afterAim");
        return true;
    }

    private static Vector3 GetRectWorldCenter(RectTransform rect)
    {
        Vector3[] corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        return (corners[0] + corners[2]) * 0.5f;
    }

    private static void CheckCanvasPlacement(Canvas canvas)
    {
        Camera eyeCamera = XRRig.EyeCamera;
        if (canvas == null || eyeCamera == null)
        {
            Debug.Log($"CHESS_CGI_XR_HUD_CHECK canvasPlacement canvasFound={canvas != null} eyeCameraFound={eyeCamera != null}");
            result.Check(eyeCamera != null, "the eye camera should be found for the HUD placement check");
            return;
        }

        Transform eye = eyeCamera.transform;
        Vector3 canvasCenter = canvas.transform.position;
        Vector3 localToEye = eye.InverseTransformPoint(canvasCenter);
        Vector3 dirToCanvas = (canvasCenter - eye.position).normalized;
        float inFrontDot = Vector3.Dot(dirToCanvas, eye.forward);
        float facingDot = Vector3.Dot(canvas.transform.forward, eye.forward);
        float horizontalOffset = localToEye.z > 0.001f ? Mathf.Abs(localToEye.x) / localToEye.z : Mathf.Infinity;

        Debug.Log("CHESS_CGI_XR_HUD_CHECK " +
            $"canvasPlacement localToEye={localToEye.ToString("F2")} inFrontDot={inFrontDot:F2} " +
            $"facingDot={facingDot:F2} horizontalOffset={horizontalOffset:F2}");

        result.Check(localToEye.z > 0f, "the HUD canvas should sit in front of the eye camera");
        result.Check(inFrontDot > 0.5f, "the HUD canvas should be within the eye camera's forward view");
        result.Check(horizontalOffset < 0.5f, "the HUD canvas should be centered, not off to the side");
        result.Check(facingDot > 0.5f, "the HUD canvas should face the player so its text is readable, not mirrored");
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

    private static void VerifyStartClick()
    {
        LogUiModel("afterRelease");
        bool startOverlayHiddenAfterClick = startOverlay != null && !startOverlay.activeSelf;
        Debug.Log("CHESS_CGI_XR_HUD_CHECK " +
            $"startOverlayHiddenAfterClick={startOverlayHiddenAfterClick}");
        result.Check(startOverlayHiddenAfterClick, "the start overlay should hide after the button click completes");
    }

    // The HUD hangs behind the board: decorative panels must not catch the ray, and board,
    // pieces and table must block it before it reaches a HUD button.
    private static void BeginRayGuardCheck()
    {
        GameHud hud = Object.FindFirstObjectByType<GameHud>();
        TrackedDeviceGraphicRaycaster raycaster = hud.GetComponent<TrackedDeviceGraphicRaycaster>();
        Transform turnPanel = hud.transform.Find("HudRoot/MatchInterface/TurnPanel");
        Transform tabletop = Object.FindFirstObjectByType<TableView>()?.transform.Find("Top/Tabletop");
        newGameButton = GameObject.Find("NewGameButton");
        bool occludes = raycaster != null && raycaster.checkFor3DOcclusion && raycaster.blockingMask.value == ~0;
        bool panelIgnoresRay = turnPanel != null && !turnPanel.GetComponent<Image>().raycastTarget;
        bool buttonTakesRay = newGameButton != null && newGameButton.GetComponent<Image>().raycastTarget;
        bool tableBlocks = tabletop != null && tabletop.GetComponent<Collider>() != null;
        Debug.Log("CHESS_CGI_XR_HUD_CHECK rayGuard " +
            $"occlusion={occludes} panelIgnoresRay={panelIgnoresRay} buttonTakesRay={buttonTakesRay} tableBlocks={tableBlocks}");
        result.Check(occludes, "the HUD raycaster should check 3D occlusion against every layer");
        result.Check(panelIgnoresRay, "decorative HUD panels should not catch the VR ray");
        result.Check(buttonTakesRay, "HUD buttons should still take the VR ray");
        result.Check(tableBlocks, "the tabletop should have a collider that blocks the ray");

        if (newGameButton == null)
        {
            ReportAndStop();
            return;
        }

        AimControllerAt(GetRectWorldCenter((RectTransform)newGameButton.transform), HighAimOrigin);
        stageStartFrame = Time.frameCount;
        stage = Stage.CheckUnblockedRay;
    }

    private static void CheckRayHitsNewGame(bool expectHit)
    {
        // Cast straight through the HUD raycaster: the UI module only recasts a device that moved,
        // so a still, scripted controller would report a stale result.
        TrackedDeviceGraphicRaycaster raycaster = Object.FindFirstObjectByType<GameHud>().GetComponent<TrackedDeviceGraphicRaycaster>();
        Vector3 target = GetRectWorldCenter((RectTransform)newGameButton.transform);
        var eventData = new TrackedDeviceEventData(EventSystem.current)
        {
            rayPoints = new List<Vector3> { HighAimOrigin, HighAimOrigin + (target - HighAimOrigin) * 1.5f },
            layerMask = ~0
        };
        var hits = new List<RaycastResult>();
        raycaster.Raycast(eventData, hits);
        GameObject hit = hits.Count > 0 ? hits[0].gameObject : null;
        bool hitsButton = hits.Exists(result => result.gameObject == newGameButton);
        Debug.Log($"CHESS_CGI_XR_HUD_CHECK rayToNewGame blocker={!expectHit} hit={(hit != null ? hit.name : "none")}");
        result.Check(hitsButton == expectHit, expectHit
            ? "an unobstructed ray should reach the Nova partida button"
            : "a collider in front of the HUD should stop the ray before the Nova partida button");
    }

    private static void ReportAndStop()
    {
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
        AimControllerAt(worldTarget, XRRig.SeatEyePosition + new Vector3(0.25f, -0.2f, 0.1f));
    }

    private static void AimControllerAt(Vector3 worldTarget, Vector3 aimOrigin)
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
        // A batch run exits here without a domain reload; restore the simulator setting now.
        XRSimulatorSetup.SetAutomaticInstantiate(false);
        EditorApplication.Exit(SessionState.GetInt(ExitCodeKey, 1));
    }
}
