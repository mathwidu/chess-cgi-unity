using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class XRTableButtonVerification
{
    private const string ArmedKey = "ChessCgiXrTableButtonCheckArmed";
    private const string DoneKey = "ChessCgiXrTableButtonCheckDone";
    private const string ExitCodeKey = "ChessCgiXrTableButtonCheckExitCode";
    private const string MainScenePath = "Assets/Scenes/Main.unity";
    private const int FramesBeforeStart = 30;
    private const float ApproachDistance = 0.03f;
    private const float PushStep = 0.0015f;
    private const int PushSteps = 40;

    private static readonly XRVerificationResult result = new XRVerificationResult();
    private static readonly Queue<(float delay, Action run)> steps = new Queue<(float, Action)>();

    private static int frameCount;
    private static float nextStepTime;
    private static TableView table;
    private static Transform source;
    private static int stepBefore;

    static XRTableButtonVerification()
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

    [MenuItem("Chess CGI/VR/Run XR Table Button Simulator Check")]
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
        table = UnityEngine.Object.FindFirstObjectByType<TableView>();
        PhysicalTableButton[] buttons = UnityEngine.Object.FindObjectsByType<PhysicalTableButton>(FindObjectsSortMode.None);
        PhysicalTableButton raise = buttons.FirstOrDefault(b => b.FaceGraphic.PointsUp);
        PhysicalTableButton lower = buttons.FirstOrDefault(b => !b.FaceGraphic.PointsUp);
        Transform room = GameObject.Find("FeevaleComputerLab")?.transform;

        result.Check(table != null && raise != null && lower != null, "the table and both physical buttons should exist");
        result.Check(room != null && room.GetComponentsInChildren<MeshCollider>().Length > 0, "the VR room should have colliders for this check to mean anything");
        if (table == null || raise == null || lower == null)
        {
            return;
        }

        table.SetHeightStep(0);
        source = new GameObject("Table button check fingertip").transform;
        source.position = raise.RestFaceCenter - raise.PressDirection * ApproachDistance;
        var contact = new GameObject("Table button check contact");
        contact.AddComponent<XRPhysicsPusher>().Configure(source, () => true);

        steps.Enqueue((0.5f, () => { }));
        steps.Enqueue((0.1f, () =>
        {
            result.Check(raise.PressFraction < 0.05f, $"the raise button should rest un-pressed, pressed {raise.PressFraction:F2}");
            result.Check(lower.PressFraction < 0.05f, $"the lower button should rest un-pressed, pressed {lower.PressFraction:F2}");
        }));

        QueuePress(raise, "raise", 1);
        QueuePress(lower, "lower", -1);

        steps.Enqueue((0.1f, () =>
        {
            var timer = Stopwatch.StartNew();
            table.SetHeightStep(1);
            table.SetHeightStep(0);
            UnityEngine.Debug.Log($"CHESS_CGI_XR_TABLE_BUTTON_CHECK two height steps took {timer.ElapsedMilliseconds} ms");
        }));
    }

    private static void QueuePress(PhysicalTableButton button, string name, int direction)
    {
        steps.Enqueue((0.1f, () =>
        {
            stepBefore = table.HeightStep;
            source.position = button.RestFaceCenter - button.PressDirection * ApproachDistance;
        }));
        steps.Enqueue((0.3f, () => { }));
        for (int i = 0; i < PushSteps; i++)
        {
            steps.Enqueue((0.03f, () =>
            {
                if (table.HeightStep == stepBefore)
                {
                    source.position += button.PressDirection * PushStep;
                }
            }));
        }

        steps.Enqueue((0.2f, () =>
        {
            result.Check(table.HeightStep == stepBefore + direction, $"pushing the {name} button should move the table one step, step {stepBefore} -> {table.HeightStep}");
            source.position = button.RestFaceCenter - button.PressDirection * ApproachDistance;
        }));
        steps.Enqueue((0.8f, () => { }));
        steps.Enqueue((0.1f, () =>
        {
            result.Check(button.PressFraction < 0.15f, $"the {name} button should spring back after the push, pressed {button.PressFraction:F2}");
        }));
    }

    private static void ReportAndStop()
    {
        result.LogSummary("CHESS_CGI_XR_TABLE_BUTTON_CHECK");
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
        XRSimulatorSetup.SetAutomaticInstantiate(false);
        EditorApplication.Exit(SessionState.GetInt(ExitCodeKey, 1));
    }
}
