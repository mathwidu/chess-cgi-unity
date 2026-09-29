using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Captures the real Main HUD without changing scene assets or player preferences.
[InitializeOnLoad]
public static class PiecePanelReviewCapture
{
    private const string ActiveKey = "ChessCGI.PiecePanelCapture";
    private const string ZoomReviewKey = "ChessCGI.PiecePanelZoomReview";
    private static readonly string[] Squares = { "f2", "f2", "e1", "b1" };
    private static readonly string[] ControlNames = { "pawn-controls", "pawn-rotated", "king-controls", "knight-controls" };
    private static readonly string[] ZoomNames = { "king-max-zoom", "king-repositioned", "king-restored", "knight-max-zoom" };
    private static bool ZoomReview => SessionState.GetBool(ZoomReviewKey, false);
    private static string[] Names => ZoomReview ? ZoomNames : ControlNames;
    private static int index, frames;
    private static double readyAt;
    private static RenderTexture target;

    static PiecePanelReviewCapture() => EditorApplication.update += Tick;

    public static void Run() => Start(false);
    public static void RunZoomReview() => Start(true);

    private static void Start(bool zoomReview)
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Use batch mode for captures.");
        SessionState.SetBool(ZoomReviewKey, zoomReview);
        EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");
        SessionState.SetBool(ActiveKey, true);
        EditorApplication.isPlaying = true;
    }

    private static void Tick()
    {
        if (!SessionState.GetBool(ActiveKey, false) || !EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        try
        {
            if (++frames < 35) return;
            var hud = UnityEngine.Object.FindFirstObjectByType<GameHud>();
            var game = UnityEngine.Object.FindFirstObjectByType<ChessGameController>();
            if (hud == null || game == null || Camera.main == null) return;
            if (target == null)
            {
                UnityEngine.Object.FindFirstObjectByType<PieceFactory>().UsePrimitivePieces = false;
                GameObject.Find("LocalModeButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
                GameObject.Find("StartPlayButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
                UnityEngine.Object.FindFirstObjectByType<CameraController>().SetPerspective(ChessSide.White, true);
                target = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
                target.Create();
                Camera.main.targetTexture = target;
                var canvas = hud.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = Camera.main;
                canvas.planeDistance = 1;
                Select(game);
                return;
            }
            if (Time.realtimeSinceStartupAsDouble < readyAt) return;
            hud.RefreshInterface();
            Canvas.ForceUpdateCanvases();
            RenderPipeline.SubmitRenderRequest(Camera.main, new RenderPipeline.StandardRequest { destination = target });
            RenderTexture previous = RenderTexture.active;
            var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            try
            {
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                image.Apply();
                string folder = ZoomReview ? "preview-pan-20260925" : "preview-controls-20260925";
                string directory = Path.GetFullPath(Path.Combine(Application.dataPath, "../../art/character-variants/" + folder));
                Directory.CreateDirectory(directory);
                File.WriteAllBytes(Path.Combine(directory, Names[index] + ".png"), image.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                UnityEngine.Object.DestroyImmediate(image);
            }
            if (++index == Names.Length) { Exit(0); return; }
            Select(game);
        }
        catch (Exception e) { Debug.LogException(e); Exit(1); }
    }

    private static void Select(ChessGameController game)
    {
        if (ZoomReview)
        {
            var board = UnityEngine.Object.FindFirstObjectByType<BoardView>();
            if (index == 0 || index == 3)
            {
                string square = index == 0 ? "e1" : "b1";
                game.SelectPiece(board.Pieces.First(p => p.Square.Equals(BoardSquare.FromAlgebraic(square))));
                UnityEngine.Object.FindFirstObjectByType<GameHud>().RefreshInterface();
                UnityEngine.Object.FindFirstObjectByType<SelectedPiecePreviewInput>().ZoomPreview(1000);
            }
            else if (index == 1)
                UnityEngine.Object.FindFirstObjectByType<SelectedPiecePreviewInput>().PanPreview(new Vector2(.12f, -.28f));
            else
                UnityEngine.Object.FindFirstObjectByType<SelectedPiecePreviewInput>().ResetView();
        }
        else if (index == 1)
        {
            var preview = UnityEngine.Object.FindFirstObjectByType<SelectedPiecePreviewInput>();
            preview.RotatePreview(140);
            preview.ZoomPreview(1);
        }
        else
        {
            var board = UnityEngine.Object.FindFirstObjectByType<BoardView>();
            game.SelectPiece(board.Pieces.First(p => p.Square.Equals(BoardSquare.FromAlgebraic(Squares[index]))));
        }
        frames = 0;
        readyAt = Time.realtimeSinceStartupAsDouble + .7;
    }

    private static void Exit(int code)
    {
        SessionState.SetBool(ActiveKey, false);
        EditorApplication.isPlaying = false;
        EditorApplication.Exit(code);
    }
}
