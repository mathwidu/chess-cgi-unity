using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class PerformanceModeVerification
{
    private const string ArmedKey = "ChessCgiPerformanceCheckArmed";
    private const string DoneKey = "ChessCgiPerformanceCheckDone";
    private const string ExitCodeKey = "ChessCgiPerformanceCheckExitCode";
    private const string MainScenePath = "Assets/Scenes/Main.unity";
    private const int FramesToRun = 30;

    private static int frameCount;
    private static readonly XRVerificationResult result = new XRVerificationResult();

    static PerformanceModeVerification()
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

    [MenuItem("Chess CGI/VR/Run Performance Mode Check")]
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
        ChessGameController controller = Object.FindFirstObjectByType<ChessGameController>();
        BoardView boardView = Object.FindFirstObjectByType<BoardView>();
        if (controller == null || boardView == null)
        {
            Debug.Log($"CHESS_CGI_PERFORMANCE_CHECK controllerFound={controller != null} boardFound={boardView != null}");
            result.Check(controller != null, "the ChessGameController should be found");
            result.Check(boardView != null, "the BoardView should be found");
            Finish();
            return;
        }

        controller.SetPerformanceMode(false);
        int customChildrenOff = boardView.Pieces.Count(p => HasChild(p.transform, "CustomVisual"));
        int primitiveChildrenOff = boardView.Pieces.Count(p => HasChild(p.transform, "Base") && HasChild(p.transform, "Stem"));
        Debug.Log($"CHESS_CGI_PERFORMANCE_CHECK modeOff pieces={boardView.Pieces.Count} customVisual={customChildrenOff} primitive={primitiveChildrenOff}");
        result.Check(!controller.PerformanceMode, "performance mode should report off after SetPerformanceMode(false)");
        result.Check(customChildrenOff == boardView.Pieces.Count && boardView.Pieces.Count > 0, "every piece should build the custom visual path when performance mode is off");
        result.Check(primitiveChildrenOff == 0, "no piece should build the primitive path when performance mode is off");

        controller.SetPerformanceMode(true);
        int customChildrenOn = boardView.Pieces.Count(p => HasChild(p.transform, "CustomVisual"));
        int primitiveChildrenOn = boardView.Pieces.Count(p => HasChild(p.transform, "Base") && HasChild(p.transform, "Stem"));
        Debug.Log($"CHESS_CGI_PERFORMANCE_CHECK modeOn pieces={boardView.Pieces.Count} customVisual={customChildrenOn} primitive={primitiveChildrenOn}");
        result.Check(controller.PerformanceMode, "performance mode should report on after SetPerformanceMode(true)");
        result.Check(primitiveChildrenOn == boardView.Pieces.Count && boardView.Pieces.Count > 0, "every piece should build the primitive path when performance mode is on");
        result.Check(customChildrenOn == 0, "no piece should build the custom visual path when performance mode is on");
        result.Check(customChildrenOff > 0 && customChildrenOn == 0, "toggling performance mode on should rebuild the pieces without custom visuals");

        controller.SetPerformanceMode(false);
        Finish();
    }

    private static bool HasChild(Transform parent, string childName)
    {
        return parent.Find(childName) != null;
    }

    private static void Finish()
    {
        result.LogSummary("CHESS_CGI_PERFORMANCE_CHECK");
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
