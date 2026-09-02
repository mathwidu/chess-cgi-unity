using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[InitializeOnLoad]
public static class XRCameraVerification
{
    private const string ArmedKey = "ChessCgiXrCameraCheckArmed";
    private const string DoneKey = "ChessCgiXrCameraCheckDone";
    private const string ExitCodeKey = "ChessCgiXrCameraCheckExitCode";
    private const string MainScenePath = "Assets/Scenes/Main.unity";
    private const string ApplyOrbitAndZoomMethodName = "ApplyOrbitAndZoom";
    private const int RigSettleFrames = 30;
    private const int OrbitStepCount = 20;
    private const int HoldSimFrames = 5;
    private const int InputBlockedTimeoutSimFrames = 300;
    private const float VrMinDistance = 2.5f;
    private const float VrMaxDistance = 6f;

    private enum Stage
    {
        WaitForRig,
        VerifyOrbitAndZoom,
        PressForPiece,
        ReleaseForPiece,
        PressForSquare,
        ReleaseForSquare,
        WaitForMove,
        VerifyTurnDidNotMoveCamera,
    }

    private static Stage stage;
    private static int frameCount;
    private static int holdStartSimFrame;
    private static CameraController cameraController;
    private static MethodInfo applyOrbitAndZoomMethod;
    private static NearFarInteractor interactor;
    private static ChessGameController gameController;
    private static BoardView boardView;
    private static PieceView targetPiece;
    private static BoardSquare destinationSquare;
    private static Vector3 preMovePosition;
    private static Quaternion preMoveRotation;
    private static readonly XRVerificationResult result = new XRVerificationResult();

    static XRCameraVerification()
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

    [MenuItem("Chess CGI/VR/Run XR Camera Simulator Check")]
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

                if (!TryBeginVerification())
                {
                    FailAndStop("Could not find the XR Origin, the CameraController, or the ApplyOrbitAndZoom method to drive.");
                    return;
                }

                Advance(Stage.VerifyOrbitAndZoom);
                return;

            case Stage.VerifyOrbitAndZoom:
                ReportOrbitAndZoomResult();

                if (!TryBeginSelectPiece())
                {
                    FailAndStop("Could not find the right controller or the a2 pawn to aim at.");
                    return;
                }

                preMovePosition = XRRig.Origin.position;
                preMoveRotation = XRRig.Origin.rotation;
                Advance(Stage.PressForPiece);
                return;

            case Stage.PressForPiece:
                HoldManualSelect();
                if (frameCount - holdStartSimFrame < HoldSimFrames)
                {
                    return;
                }

                interactor.selectInput.manualPerformed = false;
                Advance(Stage.ReleaseForPiece);
                return;

            case Stage.ReleaseForPiece:
                if (!TryBeginSelectSquare())
                {
                    FailAndStop("No legal destination square was highlighted to aim at.");
                    return;
                }

                Advance(Stage.PressForSquare);
                return;

            case Stage.PressForSquare:
                HoldManualSelect();
                if (frameCount - holdStartSimFrame < HoldSimFrames)
                {
                    return;
                }

                interactor.selectInput.manualPerformed = false;
                Advance(Stage.ReleaseForSquare);
                return;

            case Stage.ReleaseForSquare:
                Advance(Stage.WaitForMove);
                return;

            case Stage.WaitForMove:
                if (gameController.IsInputBlocked && frameCount - holdStartSimFrame < InputBlockedTimeoutSimFrames)
                {
                    return;
                }

                Advance(Stage.VerifyTurnDidNotMoveCamera);
                return;

            case Stage.VerifyTurnDidNotMoveCamera:
                ReportTurnCameraResult();
                EditorApplication.update -= Tick;
                SessionState.SetBool(ArmedKey, false);
                SessionState.SetBool(DoneKey, true);
                EditorApplication.isPlaying = false;
                EditorApplication.update += WaitForEditModeThenExit;
                return;
        }
    }

    private static void Advance(Stage next)
    {
        stage = next;
        holdStartSimFrame = frameCount;
    }

    private static bool TryBeginVerification()
    {
        cameraController = Object.FindFirstObjectByType<CameraController>();
        gameController = Object.FindFirstObjectByType<ChessGameController>();
        boardView = Object.FindFirstObjectByType<BoardView>();
        applyOrbitAndZoomMethod = typeof(CameraController).GetMethod(
            ApplyOrbitAndZoomMethodName, BindingFlags.NonPublic | BindingFlags.Instance);

        Debug.Log("CHESS_CGI_XR_CAMERA_CHECK " +
            $"headsetPresent={XRRig.IsHeadsetPresent} originFound={XRRig.Origin != null} " +
            $"cameraControllerFound={cameraController != null} applyOrbitAndZoomMethodFound={applyOrbitAndZoomMethod != null}");

        result.Check(XRRig.IsHeadsetPresent, "headset should be present in the simulator check");
        result.Check(XRRig.Origin != null, "XR Origin should be found");
        result.Check(cameraController != null, "CameraController should be found");
        result.Check(applyOrbitAndZoomMethod != null, "CameraController.ApplyOrbitAndZoom should be found");

        return XRRig.Origin != null && cameraController != null && gameController != null &&
            boardView != null && applyOrbitAndZoomMethod != null;
    }

    private static void ReportOrbitAndZoomResult()
    {
        Vector3 baselinePosition = XRRig.Origin.position;
        Quaternion baselineRotation = XRRig.Origin.rotation;

        for (int i = 0; i < OrbitStepCount; i++)
        {
            applyOrbitAndZoomMethod.Invoke(cameraController, new object[] { XRRig.Origin, -1f, 0f, VrMinDistance, VrMaxDistance });
        }

        Vector3 orbitedPosition = XRRig.Origin.position;
        float orbitPositionDelta = Vector3.Distance(baselinePosition, orbitedPosition);
        float orbitRotationDelta = Quaternion.Angle(baselineRotation, XRRig.Origin.rotation);

        Vector3 target = new Vector3(0f, 0f, 0.35f);
        float distanceBeforeZoom = Vector3.Distance(orbitedPosition, target);
        applyOrbitAndZoomMethod.Invoke(cameraController, new object[] { XRRig.Origin, 0f, 0.1f, VrMinDistance, VrMaxDistance });
        float distanceAfterZoom = Vector3.Distance(XRRig.Origin.position, target);

        Debug.Log("CHESS_CGI_XR_CAMERA_CHECK " +
            $"orbitPositionDelta={orbitPositionDelta:F2} orbitRotationDelta={orbitRotationDelta:F2} " +
            $"distanceBeforeZoom={distanceBeforeZoom:F2} distanceAfterZoom={distanceAfterZoom:F2}");

        result.Check(orbitPositionDelta > 0.05f, "manual orbit should move the XR Origin");
        result.Check(orbitRotationDelta > 0.5f, "manual orbit should rotate the XR Origin");
        result.Check(distanceBeforeZoom >= VrMinDistance && distanceBeforeZoom <= VrMaxDistance,
            $"distanceBeforeZoom={distanceBeforeZoom:F2} should be within the VR zoom bounds [{VrMinDistance}, {VrMaxDistance}]");
        result.Check(distanceAfterZoom >= VrMinDistance && distanceAfterZoom <= VrMaxDistance,
            $"distanceAfterZoom={distanceAfterZoom:F2} should be within the VR zoom bounds [{VrMinDistance}, {VrMaxDistance}]");
        result.Check(distanceAfterZoom < distanceBeforeZoom, "zooming in should move the XR Origin closer to the board");
    }

    private static void HoldManualSelect()
    {
        interactor.selectInput.manualPerformed = true;
        interactor.selectInput.manualFramePerformed = Time.frameCount;
    }

    private static bool TryBeginSelectPiece()
    {
        GameObject controllerObject = GameObject.Find("Right Controller");
        interactor = controllerObject != null ? controllerObject.GetComponent<NearFarInteractor>() : null;
        if (interactor == null)
        {
            return false;
        }

        interactor.selectInput.inputSourceMode = UnityEngine.XR.Interaction.Toolkit.Inputs.Readers.XRInputButtonReader.InputSourceMode.ManualValue;
        interactor.selectActionTrigger = UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInputInteractor.InputTriggerType.State;

        targetPiece = boardView.Pieces.FirstOrDefault(p => p.Square.ToAlgebraic() == "a2");
        if (targetPiece == null)
        {
            return false;
        }

        AimControllerAt(targetPiece.transform.position);
        return true;
    }

    private static bool TryBeginSelectSquare()
    {
        Transform highlightsRoot = boardView.transform.Find("Highlights");
        if (highlightsRoot == null || highlightsRoot.childCount == 0)
        {
            return false;
        }

        string algebraic = highlightsRoot.GetChild(0).name.Replace("Highlight ", string.Empty);
        destinationSquare = BoardSquare.FromAlgebraic(algebraic);
        SquareView destinationView = boardView.Squares.FirstOrDefault(s => s.Square.Equals(destinationSquare));
        if (destinationView == null)
        {
            return false;
        }

        AimControllerAt(destinationView.transform.position);
        return true;
    }

    private static void ReportTurnCameraResult()
    {
        PieceView movedPiece = boardView.Pieces.FirstOrDefault(p => p.Square.Equals(destinationSquare));
        Vector3 postMovePosition = XRRig.Origin.position;
        Quaternion postMoveRotation = XRRig.Origin.rotation;
        bool originMovedByTurn = Vector3.Distance(preMovePosition, postMovePosition) > 0.01f;
        bool originRotatedByTurn = Quaternion.Angle(preMoveRotation, postMoveRotation) > 0.5f;

        Debug.Log("CHESS_CGI_XR_CAMERA_CHECK " +
            $"turnFlipped={gameController.CurrentTurn} pieceMoved={movedPiece != null} " +
            $"originMovedByTurn={originMovedByTurn} " +
            $"originRotatedByTurn={originRotatedByTurn} " +
            $"currentPerspective={cameraController.CurrentPerspective}");

        result.Check(gameController.CurrentTurn == ChessSide.Black, "the turn should pass to Black after the move");
        result.Check(movedPiece != null, "the pawn should be on the destination square after the move");
        result.Check(!originMovedByTurn, "the XR Origin should not move on a turn-only change");
        result.Check(!originRotatedByTurn, "the XR Origin should not rotate on a turn-only change");
        result.Check(cameraController.CurrentPerspective == ChessSide.White, "the retired per-turn flip should leave CurrentPerspective at White");

        result.LogSummary("CHESS_CGI_XR_CAMERA_CHECK");
        SessionState.SetInt(ExitCodeKey, result.Passed ? 0 : 1);
    }

    private static void AimControllerAt(Vector3 worldTarget)
    {
        GameObject controllerObject = GameObject.Find("Right Controller");
        if (controllerObject == null)
        {
            return;
        }

        UnityEngine.InputSystem.XR.TrackedPoseDriver poseDriver =
            controllerObject.GetComponent<UnityEngine.InputSystem.XR.TrackedPoseDriver>();
        if (poseDriver != null)
        {
            poseDriver.enabled = false;
        }

        Vector3 aimOrigin = worldTarget + new Vector3(0f, 3f, 0f);
        controllerObject.transform.SetPositionAndRotation(
            aimOrigin,
            Quaternion.LookRotation((worldTarget - aimOrigin).normalized, Vector3.forward));
    }

    private static void FailAndStop(string reason)
    {
        Debug.LogError($"CHESS_CGI_XR_CAMERA_CHECK FAILED reason=\"{reason}\"");
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
