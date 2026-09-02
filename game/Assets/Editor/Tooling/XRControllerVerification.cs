using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[InitializeOnLoad]
public static class XRControllerVerification
{
    private const string ArmedKey = "ChessCgiXrControllerCheckArmed";
    private const string DoneKey = "ChessCgiXrControllerCheckDone";
    private const string ExitCodeKey = "ChessCgiXrControllerCheckExitCode";
    private const string MainScenePath = "Assets/Scenes/Main.unity";
    private const int RigSettleFrames = 30;
    private const int InputBlockedTimeoutSimFrames = 300;

    private enum Stage
    {
        WaitForRig,
        PressForPiece,
        HoldForPiece,
        ReleaseForPiece,
        VerifyPieceSelected,
        PressForSquare,
        HoldForSquare,
        ReleaseForSquare,
        WaitForMove,
        VerifyMove,
    }

    private const int HoldSimFrames = 5;

    private static Stage stage;
    private static int frameCount;
    private static int stageStartFrame;
    private static int holdStartSimFrame;
    private static NearFarInteractor interactor;
    private static ChessGameController gameController;
    private static BoardView boardView;
    private static PieceView targetPiece;
    private static BoardSquare originSquare;
    private static BoardSquare destinationSquare;
    private static readonly XRVerificationResult result = new XRVerificationResult();

    static XRControllerVerification()
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

    [MenuItem("Chess CGI/VR/Run XR Controller Simulator Check")]
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

                if (!TryBeginSelectPiece())
                {
                    FailAndStop("Could not find the built right controller / a piece to aim at.");
                    return;
                }

                Advance(Stage.PressForPiece);
                return;

            case Stage.PressForPiece:
                holdStartSimFrame = Time.frameCount;
                Advance(Stage.HoldForPiece);
                return;

            case Stage.HoldForPiece:
                HoldManualSelect();
                if (Time.frameCount - holdStartSimFrame < HoldSimFrames)
                {
                    return;
                }

                XRSimpleInteractable targetInteractable = targetPiece.GetComponent<XRSimpleInteractable>();
                UnityEngine.XR.Interaction.Toolkit.XRInteractionManager manager =
                    interactor.interactionManager;
                UnityEngine.XR.Interaction.Toolkit.XRInteractionManager[] allManagers =
                    Object.FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.XRInteractionManager>(FindObjectsSortMode.None);
                Debug.Log("CHESS_CGI_XR_CONTROLLER_CHECK " +
                    $"managerCount={allManagers.Length} " +
                    $"managerActive={(manager != null && manager.gameObject.activeInHierarchy)} " +
                    $"managerEnabled={(manager != null && manager.enabled)} " +
                    $"interactorEnabled={interactor.enabled} interactorActive={interactor.gameObject.activeInHierarchy} " +
                    $"interactableEnabled={(targetInteractable != null && targetInteractable.enabled)}");
                Debug.Log("CHESS_CGI_XR_CONTROLLER_CHECK " +
                    $"isSelectActive={interactor.isSelectActive} " +
                    $"logicalSelectActive={interactor.logicalSelectState.active} " +
                    $"selectReadIsPerformed={interactor.selectInput.ReadIsPerformed()} " +
                    $"selectActionTrigger={interactor.selectActionTrigger} " +
                    $"managerFound={manager != null} " +
                    $"canSelect={(manager != null && targetInteractable != null && manager.CanSelect(interactor, targetInteractable))} " +
                    $"isSelectPossible={(manager != null && targetInteractable != null && manager.IsSelectPossible(interactor, targetInteractable))} " +
                    $"interactableIsSelectableBy={(targetInteractable != null && ((UnityEngine.XR.Interaction.Toolkit.Interactables.IXRSelectInteractable)targetInteractable).IsSelectableBy(interactor))} " +
                    $"interactorCanSelect={(targetInteractable != null && ((UnityEngine.XR.Interaction.Toolkit.Interactors.IXRSelectInteractor)interactor).CanSelect(targetInteractable))}");

                Advance(Stage.ReleaseForPiece);
                return;

            case Stage.ReleaseForPiece:
                interactor.selectInput.manualPerformed = false;
                holdStartSimFrame = Time.frameCount;
                Advance(Stage.VerifyPieceSelected);
                return;

            case Stage.VerifyPieceSelected:
                if (Time.frameCount - holdStartSimFrame < HoldSimFrames)
                {
                    return;
                }

                ReportPieceSelection();

                if (!TryBeginSelectSquare())
                {
                    FailAndStop("No legal destination square was highlighted to aim at.");
                    return;
                }

                Advance(Stage.PressForSquare);
                return;

            case Stage.PressForSquare:
                holdStartSimFrame = Time.frameCount;
                Advance(Stage.HoldForSquare);
                return;

            case Stage.HoldForSquare:
                HoldManualSelect();
                if (Time.frameCount - holdStartSimFrame < HoldSimFrames)
                {
                    return;
                }

                List<IXRInteractable> squareValidTargets = new List<IXRInteractable>();
                interactor.GetValidTargets(squareValidTargets);
                Debug.Log("CHESS_CGI_XR_CONTROLLER_CHECK " +
                    $"beforeSquareRelease hasHover={interactor.hasHover} hoveredCount={interactor.interactablesHovered.Count} " +
                    $"validTargetsCount={squareValidTargets.Count} " +
                    $"validTarget0={(squareValidTargets.Count > 0 ? (squareValidTargets[0] as Object)?.name : "none")}");

                Advance(Stage.ReleaseForSquare);
                return;

            case Stage.ReleaseForSquare:
                interactor.selectInput.manualPerformed = false;
                holdStartSimFrame = Time.frameCount;
                Advance(Stage.WaitForMove);
                return;

            case Stage.WaitForMove:
                if (gameController.IsInputBlocked && Time.frameCount - holdStartSimFrame < InputBlockedTimeoutSimFrames)
                {
                    return;
                }

                Advance(Stage.VerifyMove);
                return;

            case Stage.VerifyMove:
                ReportMoveResult();
                result.LogSummary("CHESS_CGI_XR_CONTROLLER_CHECK");
                SessionState.SetInt(ExitCodeKey, result.Passed ? 0 : 1);
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
        stageStartFrame = frameCount;
    }

    private static void HoldManualSelect()
    {
        interactor.selectInput.manualPerformed = true;
        interactor.selectInput.manualFramePerformed = Time.frameCount;
    }

    private static void UseLevelTriggeredSelectForThisHarness(NearFarInteractor targetInteractor)
    {
        targetInteractor.selectActionTrigger = UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInputInteractor.InputTriggerType.State;
    }

    private static bool TryBeginSelectPiece()
    {
        GameObject controllerObject = GameObject.Find("Right Controller");
        interactor = controllerObject != null ? controllerObject.GetComponent<NearFarInteractor>() : null;
        gameController = Object.FindFirstObjectByType<ChessGameController>();
        boardView = Object.FindFirstObjectByType<BoardView>();

        Debug.Log("CHESS_CGI_XR_CONTROLLER_CHECK " +
            $"controllerFound={controllerObject != null} " +
            $"interactorFound={interactor != null} " +
            $"nearCasterAssigned={(interactor != null && interactor.nearInteractionCaster != null)} " +
            $"farCasterAssigned={(interactor != null && interactor.farInteractionCaster != null)} " +
            $"attachControllerAssigned={(interactor != null && interactor.interactionAttachController != null)} " +
            $"enableNearCasting={(interactor != null && interactor.enableNearCasting)} " +
            $"enableFarCasting={(interactor != null && interactor.enableFarCasting)} " +
            $"selectInputSourceMode={(interactor != null ? interactor.selectInput.inputSourceMode.ToString() : "n/a")} " +
            $"lineVisualFound={(controllerObject != null && controllerObject.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals.XRInteractorLineVisual>() != null)}");

        result.Check(interactor != null, "the right controller's NearFarInteractor should be found");

        if (interactor == null || gameController == null || boardView == null)
        {
            return false;
        }

        interactor.selectInput.inputSourceMode = UnityEngine.XR.Interaction.Toolkit.Inputs.Readers.XRInputButtonReader.InputSourceMode.ManualValue;
        UseLevelTriggeredSelectForThisHarness(interactor);

        targetPiece = boardView.Pieces.FirstOrDefault(p => p.Square.ToAlgebraic() == "a2");
        if (targetPiece == null)
        {
            return false;
        }

        XRSimpleInteractable interactable = targetPiece.GetComponent<XRSimpleInteractable>();
        VrSelectionBridge bridge = targetPiece.GetComponent<VrSelectionBridge>();
        Debug.Log("CHESS_CGI_XR_CONTROLLER_CHECK " +
            $"pieceInteractableFound={interactable != null} pieceBridgeFound={bridge != null}");
        result.Check(interactable != null, "the a2 pawn should have an XRSimpleInteractable");
        result.Check(bridge != null, "the a2 pawn should have a VrSelectionBridge");

        if (interactable != null)
        {
            interactable.selectEntered.AddListener(args => Debug.Log("CHESS_CGI_XR_CONTROLLER_CHECK selectEnteredEventFired " +
                $"currentTurn={gameController.CurrentTurn} selectedPieceAtEvent={(gameController.SelectedPiece != null ? gameController.SelectedPiece.name : "null")} " +
                $"statusAtEvent=\"{gameController.StatusMessage}\""));
            interactable.hoverEntered.AddListener(args => Debug.Log("CHESS_CGI_XR_CONTROLLER_CHECK hoverEnteredEventFired"));
        }

        originSquare = targetPiece.Square;
        AimControllerAt(targetPiece.transform.position);
        return true;
    }

    private static void ReportPieceSelection()
    {
        GameObject controllerObject = GameObject.Find("Right Controller");
        result.Check(gameController.SelectedPiece == targetPiece, "aiming and selecting the a2 pawn should select it in the game controller");
        Debug.Log("CHESS_CGI_XR_CONTROLLER_CHECK " +
            $"pieceSelected={(gameController.SelectedPiece == targetPiece)} " +
            $"highlightCount={boardView.HighlightCount} " +
            $"hasHover={interactor.hasHover} hoveredCount={interactor.interactablesHovered.Count} " +
            $"hasSelection={interactor.hasSelection} selectedCount={interactor.interactablesSelected.Count} " +
            $"controllerPos={(controllerObject != null ? controllerObject.transform.position.ToString("F2") : "n/a")} " +
            $"targetPiecePos={targetPiece.transform.position.ToString("F2")}");

        for (int i = 0; i < interactor.interactablesHovered.Count; i++)
        {
            IXRHoverInteractable hovered = interactor.interactablesHovered[i];
            Object hoveredObject = hovered as Object;
            bool isTarget = hoveredObject == targetPiece.GetComponent<XRSimpleInteractable>();
            Debug.Log($"CHESS_CGI_XR_CONTROLLER_CHECK hoveredTarget[{i}]={(hoveredObject != null ? hoveredObject.name : "null")} isOurPiece={isTarget}");
        }

        List<IXRInteractable> liveValidTargets = new List<IXRInteractable>();
        interactor.GetValidTargets(liveValidTargets);
        Debug.Log($"CHESS_CGI_XR_CONTROLLER_CHECK liveValidTargetsCount={liveValidTargets.Count}");
        for (int i = 0; i < liveValidTargets.Count; i++)
        {
            Object targetObject = liveValidTargets[i] as Object;
            Debug.Log($"CHESS_CGI_XR_CONTROLLER_CHECK liveValidTarget[{i}]={(targetObject != null ? targetObject.name : "null")}");
        }
    }

    private static bool TryBeginSelectSquare()
    {
        Transform highlightsRoot = boardView.transform.Find("Highlights");
        if (highlightsRoot == null || highlightsRoot.childCount == 0)
        {
            return false;
        }

        string highlightName = highlightsRoot.GetChild(0).name;
        string algebraic = highlightName.Replace("Highlight ", string.Empty);
        destinationSquare = BoardSquare.FromAlgebraic(algebraic);

        SquareView destinationView = boardView.Squares.FirstOrDefault(s => s.Square.Equals(destinationSquare));
        if (destinationView == null)
        {
            return false;
        }

        XRSimpleInteractable interactable = destinationView.GetComponent<XRSimpleInteractable>();
        VrSelectionBridge destinationBridge = destinationView.GetComponent<VrSelectionBridge>();
        Debug.Log("CHESS_CGI_XR_CONTROLLER_CHECK " +
            $"destinationSquare={algebraic} destinationInteractableFound={interactable != null} " +
            $"destinationBridgeFound={destinationBridge != null}");
        result.Check(interactable != null, $"the highlighted destination square {algebraic} should have an XRSimpleInteractable");
        result.Check(destinationBridge != null, $"the highlighted destination square {algebraic} should have a VrSelectionBridge");

        if (interactable != null)
        {
            interactable.selectEntered.AddListener(args => Debug.Log("CHESS_CGI_XR_CONTROLLER_CHECK squareSelectEnteredEventFired " +
                $"selectedPieceAtEvent={(gameController.SelectedPiece != null ? gameController.SelectedPiece.name : "null")} " +
                $"isInputBlockedAtEvent={gameController.IsInputBlocked} " +
                $"statusAtEvent=\"{gameController.StatusMessage}\""));
            interactable.hoverEntered.AddListener(args => Debug.Log("CHESS_CGI_XR_CONTROLLER_CHECK squareHoverEnteredEventFired"));
            interactable.selectExited.AddListener(args => Debug.Log("CHESS_CGI_XR_CONTROLLER_CHECK squareSelectExitedEventFired " +
                $"selectedPieceAtEvent={(gameController.SelectedPiece != null ? gameController.SelectedPiece.name : "null")} " +
                $"statusAtEvent=\"{gameController.StatusMessage}\""));
        }

        if (targetPiece != null)
        {
            XRSimpleInteractable pieceInteractable = targetPiece.GetComponent<XRSimpleInteractable>();
            if (pieceInteractable != null)
            {
                pieceInteractable.selectEntered.AddListener(args => Debug.Log("CHESS_CGI_XR_CONTROLLER_CHECK pieceSelectEnteredEventFiredAgain " +
                    $"selectedPieceAtEvent={(gameController.SelectedPiece != null ? gameController.SelectedPiece.name : "null")} " +
                    $"statusAtEvent=\"{gameController.StatusMessage}\""));
            }
        }

        AimControllerAt(destinationView.transform.position);
        return true;
    }

    private static void ReportMoveResult()
    {
        PieceView movedPiece = boardView.Pieces.FirstOrDefault(p => p.Square.Equals(destinationSquare));
        bool originCleared = boardView.Pieces.All(p => !p.Square.Equals(originSquare));
        bool pieceOnDestination = movedPiece != null && movedPiece.Kind == ChessPieceKind.Pawn;

        Debug.Log("CHESS_CGI_XR_CONTROLLER_CHECK " +
            $"selectionClearedAfterMove={(gameController.SelectedPiece == null)} " +
            $"pieceOnDestination={pieceOnDestination} " +
            $"originSquareCleared={originCleared} " +
            $"statusMessage=\"{gameController.StatusMessage}\"");

        result.Check(gameController.SelectedPiece == null, "selection should clear after the move completes");
        result.Check(pieceOnDestination, "the pawn should be on the destination square after the move");
        result.Check(originCleared, "the origin square should be empty after the move");
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
        Debug.LogError($"CHESS_CGI_XR_CONTROLLER_CHECK FAILED reason=\"{reason}\"");
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
