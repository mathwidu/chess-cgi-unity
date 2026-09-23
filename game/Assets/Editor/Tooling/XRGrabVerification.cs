using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Casters;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;

[InitializeOnLoad]
public static class XRGrabVerification
{
    private const string ArmedKey = "ChessCgiXrGrabCheckArmed";
    private const string DoneKey = "ChessCgiXrGrabCheckDone";
    private const string ExitCodeKey = "ChessCgiXrGrabCheckExitCode";
    private const string MainScenePath = "Assets/Scenes/Main.unity";
    private const int FramesBeforeStart = 30;
    private const float MoveWait = 0.8f;

    private static readonly XRVerificationResult result = new XRVerificationResult();
    private static readonly Queue<(float delay, Action run)> steps = new Queue<(float, Action)>();

    private static int frameCount;
    private static float nextStepTime;
    private static ChessGameController game;
    private static BoardView board;
    private static NearFarInteractor leftInteractor;

    static XRGrabVerification()
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

    [MenuItem("Chess CGI/VR/Run XR Grab Simulator Check")]
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
        if (frameCount < FramesBeforeStart)
        {
            return;
        }

        if (frameCount == FramesBeforeStart)
        {
            StartChecks();
            nextStepTime = Time.realtimeSinceStartup;
        }

        if (steps.Count == 0)
        {
            EditorApplication.update -= Tick;
            ReportAndStop();
            return;
        }

        if (Time.realtimeSinceStartup < nextStepTime)
        {
            return;
        }

        (float delay, Action run) step = steps.Dequeue();
        step.run();
        nextStepTime = Time.realtimeSinceStartup + step.delay;
    }

    private static void StartChecks()
    {
        game = UnityEngine.Object.FindFirstObjectByType<ChessGameController>();
        board = UnityEngine.Object.FindFirstObjectByType<BoardView>();
        Transform offset = GameObject.Find("XR Origin (VR)")?.transform.Find("Camera Offset");
        leftInteractor = offset?.Find("Left Controller")?.GetComponent<NearFarInteractor>();

        result.Check(game != null && board != null && leftInteractor != null, "game, board and left controller interactor should exist");
        if (game == null || board == null || leftInteractor == null)
        {
            return;
        }

        game.StartLocalGame();

        CheckRigAndPieces(offset);

        PieceView whitePawn = FindPiece(4, 2);
        PieceView blackPawn = FindPiece(4, 7);

        steps.Enqueue((0.1f, () =>
        {
            XRGrabInteractable white = whitePawn.GetComponent<XRGrabInteractable>();
            XRGrabInteractable black = blackPawn.GetComponent<XRGrabInteractable>();
            result.Check(((IXRSelectInteractable)white).IsSelectableBy(leftInteractor), "the current side's piece should be grabbable");
            result.Check(!((IXRSelectInteractable)black).IsSelectableBy(leftInteractor), "the other side's piece should not be grabbable");
            white.interactionManager.SelectEnter((IXRSelectInteractor)leftInteractor, (IXRSelectInteractable)white);
        }));
        steps.Enqueue((0.1f, () =>
        {
            result.Check(game.SelectedPiece == whitePawn, "grabbing a piece should select it");
            result.Check(board.HighlightCount == 2, "grabbing e2 should highlight its 2 legal destinations");
            XRGrabInteractable white = whitePawn.GetComponent<XRGrabInteractable>();
            white.interactionManager.SelectExit((IXRSelectInteractor)leftInteractor, (IXRSelectInteractable)white);
        }));
        steps.Enqueue((0.3f, () =>
        {
            result.Check(game.SelectedPiece == null, "releasing on the origin square should deselect the piece");
            result.Check(game.CurrentTurn == ChessSide.White, "releasing on the origin square should not move");
        }));

        steps.Enqueue((MoveWait, () =>
        {
            game.GrabPiece(FindPiece(4, 2));
            game.ReleasePiece(FindPiece(4, 2), board.GetPieceWorldPosition(new BoardSquare(4, 4)));
        }));
        steps.Enqueue((0.1f, () =>
        {
            result.Check(game.CurrentTurn == ChessSide.Black, "releasing e2 pawn on e4 should make the move and pass the turn");
            result.Check(FindPiece(4, 4) != null && FindPiece(4, 2) == null, "the pawn should end on e4");
        }));

        steps.Enqueue((MoveWait, () =>
        {
            game.GrabPiece(FindPiece(4, 7));
            game.ReleasePiece(FindPiece(4, 7), board.GetPieceWorldPosition(new BoardSquare(4, 4)));
            result.Check(game.StatusMessage == "Movimento invalido.", "releasing on an illegal square should report an invalid move");
            result.Check(game.SelectedPiece == null, "an invalid release should clear the selection");
            result.Check(game.CurrentTurn == ChessSide.Black, "an invalid release should not pass the turn");
        }));
        steps.Enqueue((0.4f, () =>
        {
            PieceView returned = FindPiece(4, 7);
            Vector3 expected = board.GetPieceWorldPosition(new BoardSquare(4, 7));
            result.Check(Vector3.Distance(returned.transform.position, expected) < 0.001f, "an invalid release should return the piece to its square");
        }));

        steps.Enqueue((0.1f, () =>
        {
            PieceView piece = FindPiece(3, 7);
            game.GrabPiece(piece);
            game.ReleasePiece(piece, board.GetPieceWorldPosition(new BoardSquare(3, 7)) + new Vector3(50f, 0f, 50f));
            result.Check(game.StatusMessage == "Movimento invalido.", "releasing off the board should report an invalid move");
            result.Check(game.SelectedPiece == null, "releasing off the board should clear the selection");
        }));
    }

    private static void CheckRigAndPieces(Transform offset)
    {
        int grabbable = 0;
        foreach (PieceView piece in board.Pieces)
        {
            XRGrabInteractable grab = piece.GetComponent<XRGrabInteractable>();
            Rigidbody body = piece.GetComponent<Rigidbody>();
            bool ready = grab != null && body != null && body.isKinematic && piece.GetComponent<VrSelectionBridge>() != null;
            grabbable += ready ? 1 : 0;
        }

        result.Check(grabbable == 32, $"all 32 pieces should be grabbable, found {grabbable}");
        result.Check(board.Squares.Count == 64, "the board should have 64 squares");
        foreach (SquareView square in board.Squares)
        {
            result.Check(square.GetComponent<XRBaseInteractable>() == null, "squares should not be XR interactables");
        }

        foreach (string controllerName in new[] { "Left Controller", "Right Controller" })
        {
            Transform controller = offset.Find(controllerName);
            NearFarInteractor interactor = controller.GetComponent<NearFarInteractor>();
            CurveInteractionCaster far = controller.GetComponent<CurveInteractionCaster>();
            CurveVisualController visual = controller.GetComponent<CurveVisualController>();
            string selectPath = interactor.selectInput.inputActionPerformed.bindings[0].path;

            result.Check(interactor.enableNearCasting, $"{controllerName} should cast near for grabbing");
            result.Check(selectPath.EndsWith("gripButton"), $"{controllerName} should select with the grip, got {selectPath}");
            result.Check((far.raycastMask.value & (1 << PieceView.PhysicsLayer)) == 0 && far.raycastMask.value != 0, $"{controllerName} far ray should skip the pieces layer but still reach the HUD");
            result.Check(visual.curveInteractionDataProvider is UiOnlyCurveData, $"{controllerName} ray should only show over the HUD");
        }

        foreach (string handName in new[] { "LeftHandInteractor", "RightHandInteractor" })
        {
            Transform hand = offset.Find(handName);
            CurveVisualController visual = hand == null ? null : hand.GetComponentInChildren<CurveVisualController>(true);
            result.Check(visual == null || visual.curveInteractionDataProvider is UiOnlyCurveData, $"{handName} ray should only show over the HUD");
        }
    }

    private static PieceView FindPiece(int fileIndex, int rank)
    {
        BoardSquare square = new BoardSquare(fileIndex, rank);
        foreach (PieceView piece in board.Pieces)
        {
            if (piece.Square.Equals(square))
            {
                return piece;
            }
        }

        return null;
    }

    private static void ReportAndStop()
    {
        result.LogSummary("CHESS_CGI_XR_GRAB_CHECK");
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
