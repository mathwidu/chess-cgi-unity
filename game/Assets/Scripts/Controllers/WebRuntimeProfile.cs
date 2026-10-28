using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Runtime adjustments are applied to a copy, never to the desktop/Quest render assets.
public sealed class WebRuntimeProfile : MonoBehaviour
{
    private UniversalRenderPipelineAsset pipeline;
    private RenderPipelineAsset previousPipeline;
    private ChessGameController controller;
    private BoardView board;
    private double nextSample;
    private double sampleStart;
    private int frames;
    private bool diagnostics;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void ChessWebPublishState(string json);
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap() => new GameObject("Web Runtime").AddComponent<WebRuntimeProfile>();
#endif

    private void Awake()
    {
        controller = FindFirstObjectByType<ChessGameController>();
        board = FindFirstObjectByType<BoardView>();
        previousPipeline = QualitySettings.renderPipeline;
        var source = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (source != null)
        {
            pipeline = Instantiate(source);
            pipeline.name = "Web runtime profile";
            pipeline.renderScale = 1;
            QualitySettings.renderPipeline = pipeline;
        }
        Application.targetFrameRate = Application.isMobilePlatform ? 30 : 60;
        QualitySettings.vSyncCount = 0;
        diagnostics = Application.absoluteURL.Contains("qa=1");
        nextSample = sampleStart = Time.realtimeSinceStartupAsDouble;
    }

    private void Update()
    {
        frames++;
        double now = Time.realtimeSinceStartupAsDouble;
        if (now < nextSample) return;
        double elapsed = now - sampleStart;
        float fps = elapsed > .1 ? (float)(frames / elapsed) : 0;
        sampleStart = now;
        frames = 0;
        nextSample = now + 1;
        RecordFrameSample(fps, elapsed, Application.isFocused);
    }

    private void RecordFrameSample(float fps, double elapsed, bool focused)
    {
        // FPS alone does not identify GPU pressure. Keep character detail stable;
        // the player's existing performance mode remains an explicit choice.
        if (diagnostics && controller != null && board != null) Publish(fps, elapsed, focused);
    }

    public void OnWebVisibility(string hidden)
    {
        if (controller != null) controller.SendMessage("OnApplicationPause", hidden == "1", SendMessageOptions.DontRequireReceiver);
        sampleStart = Time.realtimeSinceStartupAsDouble;
        frames = 0;
    }

    private void Publish(float fps, double elapsed, bool focused)
    {
        var state = new State
        {
            menu = controller.IsMenuOpen, thinking = controller.IsComputerThinking,
            error = controller.HasComputerError, blocked = controller.IsInputBlocked,
            promotion = controller.IsAwaitingPromotion, classic = controller.PerformanceMode,
            moves = controller.MoveHistory.Count, status = controller.StatusMessage,
            selected = controller.SelectedPiece == null ? "" : controller.SelectedPiece.Square.ToAlgebraic(),
            turn = controller.CurrentTurn.ToString(), fps = fps, sampleSeconds = (float)elapsed, focused = focused,
            renderScale = pipeline == null ? 1 : pipeline.renderScale,
            width = Screen.width, height = Screen.height, pieces = board.Pieces.Count,
            heapBytes = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong()
        };
        Camera camera = Camera.main;
        if (camera != null)
        {
            for (int rank = 1; rank <= 8; rank++)
            for (int file = 0; file < 8; file++)
            {
                var square = new BoardSquare(file, rank);
                Vector3 position = camera.WorldToScreenPoint(board.GetWorldPosition(square));
                state.cells.Add(new Point { name = square.ToAlgebraic(), x = position.x / Screen.width, y = 1 - position.y / Screen.height });
            }
            foreach (PieceView piece in board.Pieces)
            {
                Vector3 position = camera.WorldToScreenPoint(piece.transform.position + Vector3.up * .5f);
                state.targets.Add(new Point { name = piece.Square.ToAlgebraic(), x = position.x / Screen.width, y = 1 - position.y / Screen.height });
            }
        }
        foreach (var button in FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None))
        {
            if (!button.isActiveAndEnabled || !button.interactable) continue;
            var rect = button.transform as RectTransform;
            if (rect == null) continue;
            Vector2 center = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
            var label = button.GetComponentInChildren<UnityEngine.UI.Text>();
            state.buttons.Add(new Point { name = button.name, label = label == null ? "" : label.text, x = center.x / Screen.width, y = 1 - center.y / Screen.height });
        }
#if UNITY_WEBGL && !UNITY_EDITOR
        ChessWebPublishState(JsonUtility.ToJson(state));
#endif
    }

    private void OnDestroy()
    {
        if (QualitySettings.renderPipeline == pipeline) QualitySettings.renderPipeline = previousPipeline;
        if (pipeline != null) Destroy(pipeline);
    }

    [Serializable] private sealed class Point { public string name, label; public float x, y; }
    [Serializable] private sealed class State
    {
        public bool menu, thinking, error, blocked, promotion, classic, focused;
        public int moves, width, height, pieces;
        public string status, selected, turn;
        public float fps, renderScale, sampleSeconds;
        public long heapBytes;
        public List<Point> cells = new List<Point>();
        public List<Point> targets = new List<Point>();
        public List<Point> buttons = new List<Point>();
    }
}
