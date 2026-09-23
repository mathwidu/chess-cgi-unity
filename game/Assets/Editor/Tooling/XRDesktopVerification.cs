using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class XRDesktopVerification
{
    private const string ArmedKey = "ChessCgiDesktopCheckArmed";
    private const string DoneKey = "ChessCgiDesktopCheckDone";
    private const string ExitCodeKey = "ChessCgiDesktopCheckExitCode";
    private const string MainScenePath = "Assets/Scenes/Main.unity";
    private const int FramesToRun = 30;

    private static int frameCount;
    private static readonly XRVerificationResult result = new XRVerificationResult();

    static XRDesktopVerification()
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

    [MenuItem("Chess CGI/VR/Run Desktop Simulator Check")]
    public static void RunSimulatorCheck()
    {
        XRSimulatorSetup.SetAutomaticInstantiate(false);
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
        bool headsetPresent = XRRig.IsHeadsetPresent;
        Debug.Log($"CHESS_CGI_DESKTOP_CHECK headsetPresent={headsetPresent} originBuilt={XRRig.Origin != null}");
        result.Check(!headsetPresent, "no headset should be present in the desktop check");
        result.Check(XRRig.Origin == null, "the VR rig should not be built in desktop mode");

        CheckBoardTransform();
        CheckCameraFramesBoard();
        CheckDesktopSelectionWired();

        result.LogSummary("CHESS_CGI_DESKTOP_CHECK");
        SessionState.SetInt(ExitCodeKey, result.Passed ? 0 : 1);
        SessionState.SetBool(ArmedKey, false);
        SessionState.SetBool(DoneKey, true);
        EditorApplication.isPlaying = false;
        EditorApplication.update += WaitForEditModeThenExit;
    }

    private static void CheckBoardTransform()
    {
        BoardView boardView = Object.FindFirstObjectByType<BoardView>();
        if (boardView == null)
        {
            Debug.Log("CHESS_CGI_DESKTOP_CHECK boardFound=false");
            result.Check(false, "the BoardView should be found");
            return;
        }

        Vector3 scale = boardView.transform.lossyScale;
        Vector3 position = boardView.transform.position;
        bool scaleIsOne = Vector3.Distance(scale, Vector3.one) < 0.01f;
        bool atOrigin = position.magnitude < 0.01f;

        Debug.Log($"CHESS_CGI_DESKTOP_CHECK boardScale={scale.ToString("F3")} boardPosition={position.ToString("F3")}");
        result.Check(scaleIsOne, "the desktop board should render at world scale 1");
        result.Check(atOrigin, "the desktop board should sit at the world origin");
    }

    private static void CheckCameraFramesBoard()
    {
        Camera mainCamera = Camera.main;
        BoardView boardView = Object.FindFirstObjectByType<BoardView>();
        if (mainCamera == null || boardView == null)
        {
            Debug.Log($"CHESS_CGI_DESKTOP_CHECK mainCameraFound={mainCamera != null} boardFound={boardView != null}");
            result.Check(mainCamera != null, "the Main Camera should be found");
            return;
        }

        result.Check(mainCamera.enabled, "the Main Camera should be enabled in desktop mode");

        Renderer[] squareRenderers = boardView.Squares.Select(s => s.GetComponent<Renderer>()).Where(r => r != null).ToArray();
        if (squareRenderers.Length == 0)
        {
            Debug.Log("CHESS_CGI_DESKTOP_CHECK squareRenderers=0");
            result.Check(false, "the board should have square renderers to frame");
            return;
        }

        Bounds boardBounds = squareRenderers[0].bounds;
        for (int i = 1; i < squareRenderers.Length; i++)
        {
            boardBounds.Encapsulate(squareRenderers[i].bounds);
        }

        Plane[] frustum = GeometryUtility.CalculateFrustumPlanes(mainCamera);
        bool boardInFrustum = GeometryUtility.TestPlanesAABB(frustum, boardBounds);

        Vector3 centerViewport = mainCamera.WorldToViewportPoint(boardBounds.center);
        bool centerInFront = centerViewport.z > 0f;
        bool centerFramed = centerViewport.x > 0.2f && centerViewport.x < 0.8f && centerViewport.y > 0.2f && centerViewport.y < 0.8f;

        Vector3 minViewport = mainCamera.WorldToViewportPoint(boardBounds.min);
        Vector3 maxViewport = mainCamera.WorldToViewportPoint(boardBounds.max);
        float onScreenSpan = Vector2.Distance(new Vector2(minViewport.x, minViewport.y), new Vector2(maxViewport.x, maxViewport.y));

        Debug.Log("CHESS_CGI_DESKTOP_CHECK " +
            $"boardInFrustum={boardInFrustum} centerViewport={centerViewport.ToString("F2")} onScreenSpan={onScreenSpan:F2}");

        result.Check(boardInFrustum, "the board should be inside the Main Camera frustum");
        result.Check(centerInFront, "the board center should be in front of the Main Camera");
        result.Check(centerFramed, "the board center should be near the middle of the desktop view");
        result.Check(onScreenSpan > 0.3f, "the board should fill a meaningful part of the desktop view, not a distant speck");
    }

    private static void CheckDesktopSelectionWired()
    {
        InputController inputController = Object.FindFirstObjectByType<InputController>();
        Camera mainCamera = Camera.main;
        BoardView boardView = Object.FindFirstObjectByType<BoardView>();

        Camera raycastCamera = null;
        if (inputController != null)
        {
            FieldInfo field = typeof(InputController).GetField("raycastCamera", BindingFlags.NonPublic | BindingFlags.Instance);
            raycastCamera = field != null ? field.GetValue(inputController) as Camera : null;
        }

        Debug.Log($"CHESS_CGI_DESKTOP_CHECK inputControllerFound={inputController != null} raycastCameraName={(raycastCamera != null ? raycastCamera.name : "none")}");
        result.Check(inputController != null, "the InputController should be found");
        result.Check(raycastCamera != null && raycastCamera == mainCamera, "desktop selection should raycast from the Main Camera");

        if (mainCamera == null || boardView == null)
        {
            return;
        }

        PieceView pawn = boardView.Pieces.FirstOrDefault(p => p.Square.ToAlgebraic() == "a2");
        if (pawn == null)
        {
            Debug.Log("CHESS_CGI_DESKTOP_CHECK pawnFound=false");
            result.Check(false, "the a2 pawn should exist to test the desktop selection raycast");
            return;
        }

        Vector3 aimPoint = pawn.transform.position + new Vector3(0f, 0.7f, 0f);
        Vector3 screenPoint = mainCamera.WorldToScreenPoint(aimPoint);
        Ray ray = mainCamera.ScreenPointToRay(screenPoint);
        bool hitSomething = Physics.Raycast(ray, out RaycastHit hit, 200f);
        PieceView hitPiece = hitSomething ? hit.collider.GetComponentInParent<PieceView>() : null;

        Debug.Log("CHESS_CGI_DESKTOP_CHECK " +
            $"raycastHit={hitSomething} hitCollider={(hitSomething ? hit.collider.name : "none")} " +
            $"hitPieceSquare={(hitPiece != null ? hitPiece.Square.ToAlgebraic() : "none")}");

        result.Check(hitSomething, "a desktop raycast from the Main Camera should hit the scaled-1 board");
        result.Check(hitPiece != null && hitPiece.Square.ToAlgebraic() == "a2", "the desktop raycast should resolve to the a2 pawn");
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
