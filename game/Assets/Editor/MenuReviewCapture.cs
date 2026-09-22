using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;

// Batch-only visual evidence from the real Main scene. Does not build a player.
[InitializeOnLoad]
public static class MenuReviewCapture
{
    private const string Active = "ChessCGI.MenuReviewCapture";
    private static int step;
    private static int frame;
    private static RenderTexture target;
    private static string directory;
    private static readonly Vector2Int[] sizes = {
        new Vector2Int(1920,1080), new Vector2Int(1280,800), new Vector2Int(1024,768),
        new Vector2Int(2560,1080), new Vector2Int(1223,704),
        new Vector2Int(1920,1080), new Vector2Int(1920,1080), new Vector2Int(1920,1080), new Vector2Int(1920,1080),
        new Vector2Int(1920,1080), new Vector2Int(1920,1080), new Vector2Int(1920,1080)
    };
    private static readonly string[] names = {"desktop", "laptop", "compact", "wide", "editor", "black-hard", "local", "help", "match", "selection", "promotion", "error"};
    static MenuReviewCapture() { EditorApplication.update += Tick; }
    public static void Run()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Capture requires a separate batch Editor.");
        EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");
        SessionState.SetBool(Active, true);
        EditorApplication.isPlaying = true;
    }
    private static void Tick()
    {
        if (!SessionState.GetBool(Active, false) || !EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        try
        {
            var hud = UnityEngine.Object.FindFirstObjectByType<GameHud>();
            if (hud == null) return;
            if (++frame < 45) return;
            var camera = Camera.main;
            var canvas = hud.GetComponent<Canvas>();
            if (target == null)
            {
                directory = Path.GetFullPath(Path.Combine(Application.dataPath, "../../.impeccable/review"));
                Directory.CreateDirectory(directory);
                if (step == 5) { Click("BlackSideButton"); Click("HardDifficultyButton"); }
                if (step == 6) Click("LocalModeButton");
                if (step == 7) Click("StartHowToPlayButton");
                if (step == 8) { Click("CloseHelpButton"); Click("StartPlayButton"); }
                var controller = UnityEngine.Object.FindFirstObjectByType<ChessGameController>();
                var board = UnityEngine.Object.FindFirstObjectByType<BoardView>();
                if (step == 9) controller.SelectPiece(board.Pieces.First(piece => piece.Square.Equals(new BoardSquare(3,1))));
                if (step == 10)
                {
                    // Deterministic legal promotion fixture, used only for the visual review.
                    controller.StartLocalGame();
                    var rules = (ChessRulesAdapter)typeof(ChessGameController).GetField("rules", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(controller);
                    rules.Reset("7k/P7/8/8/8/8/8/7K w - - 0 1");
                    board.SyncPieces(rules.GetPieces(), UnityEngine.Object.FindFirstObjectByType<PieceFactory>());
                    controller.SelectPiece(board.Pieces.First(piece => piece.Square.Equals(new BoardSquare(0,7))));
                    controller.SelectDestination(new BoardSquare(0,8));
                }
                if (step == 11)
                {
                    controller.SetMoveChooserFactory(() => throw new InvalidOperationException("Visual review: unavailable engine fixture"));
                    controller.StartComputerGame(ChessSide.Black, ComputerDifficulty.Beginner);
                }
                target = new RenderTexture(sizes[step].x, sizes[step].y, 24, RenderTextureFormat.ARGB32);
                target.Create();
                camera.targetTexture = target;
                camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1f;
                frame = 0;
                return;
            }
            Canvas.ForceUpdateCanvases();
            hud.RefreshInterface();
            Canvas.ForceUpdateCanvases();
            string expectedFocus = step == 7 ? "CloseHelpButton" : step == 10 ? "PromoteQueenButton" : step == 11 ? "RetryComputerButton" : null;
            if (expectedFocus != null && UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject?.name != expectedFocus)
                throw new InvalidOperationException("Unexpected modal focus; expected " + expectedFocus);
            RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
            RenderTexture.active = target;
            var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            image.Apply();
            File.WriteAllBytes(Path.Combine(directory, names[step] + ".png"), image.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(image);
            RenderTexture.active = null;
            camera.targetTexture = null;
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
            target = null;
            frame = 0;
            Debug.Log("UI_CAPTURE_OK " + names[step]);
            if (++step == sizes.Length)
            {
                SessionState.SetBool(Active, false);
                EditorApplication.Exit(0);
            }
        }
        catch (Exception error)
        {
            SessionState.SetBool(Active, false);
            Debug.LogException(error);
            EditorApplication.Exit(1);
        }
    }
    private static void Click(string name) { GameObject.Find(name).GetComponent<Button>().onClick.Invoke(); }
}
