using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

// Batch-only visual evidence from the real Main scene. Does not build a player.
[InitializeOnLoad]
public static class MenuReviewCapture
{
    private const string ActiveKey = "ChessCGI.MenuReviewCapture";
    private const int SettleFrames = 45;
    private const double SettleSeconds = 1.25;

    private sealed class CaptureCase
    {
        public readonly string Name;
        public readonly Vector2Int Size;
        public readonly Action Prepare;
        public readonly string ExpectedFocus;

        public CaptureCase(string name, int width, int height, Action prepare = null, string expectedFocus = null)
        {
            Name = name;
            Size = new Vector2Int(width, height);
            Prepare = prepare;
            ExpectedFocus = expectedFocus;
        }
    }

    // Cases form a journey: later states continue the same session.
    private static readonly CaptureCase[] Cases =
    {
        new CaptureCase("desktop", 1672, 941, () => Click("BlackSideButton", "HardDifficultyButton")),
        new CaptureCase("laptop", 1280, 800),
        new CaptureCase("compact", 1024, 768),
        new CaptureCase("wide", 2560, 1080),
        new CaptureCase("editor", 1223, 704),
        new CaptureCase("white-beginner", 1920, 1080, () => Click("WhiteSideButton", "BeginnerDifficultyButton")),
        new CaptureCase("local", 1920, 1080, () => Click("LocalModeButton")),
        new CaptureCase("help", 1920, 1080, () => Click("StartHowToPlayButton"), "CloseHelpButton"),
        new CaptureCase("match", 1920, 1080, () => Click("CloseHelpButton", "StartPlayButton")),
        new CaptureCase("selection", 1920, 1080, PrepareSelection),
        new CaptureCase("promotion", 1920, 1080, PreparePromotion, "PromoteQueenButton"),
        new CaptureCase("error", 1920, 1080, PrepareEngineFailure, "RetryComputerButton"),
        new CaptureCase("intermediate-compact", 1024, 768,
            () => Click("ComputerMenuButton", "ComputerModeButton", "IntermediateDifficultyButton")),
        new CaptureCase("play-focus", 1672, 941,
            () => EventSystem.current.SetSelectedGameObject(GameObject.Find("StartPlayButton")))
    };

    private static int caseIndex;
    private static int frameCount;
    private static double captureReadyAt;
    private static RenderTexture target;

    static MenuReviewCapture()
    {
        EditorApplication.update += Tick;
    }

    public static void Run()
    {
        if (!Application.isBatchMode)
        {
            throw new InvalidOperationException("Capture requires a separate batch Editor.");
        }

        EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");
        SessionState.SetBool(ActiveKey, true);
        EditorApplication.isPlaying = true;
    }

    private static void Tick()
    {
        if (!SessionState.GetBool(ActiveKey, false) || !EditorApplication.isPlaying || EditorApplication.isCompiling)
        {
            return;
        }

        try
        {
            GameHud hud = UnityEngine.Object.FindFirstObjectByType<GameHud>();
            if (hud == null || ++frameCount < SettleFrames)
            {
                return;
            }

            CaptureCase current = Cases[caseIndex];
            Camera camera = Camera.main;
            if (target == null)
            {
                current.Prepare?.Invoke();
                PrepareRenderTarget(current.Size, camera, hud.GetComponent<Canvas>());
                // Unscaled animation time must elapse even when batch mode runs frames very fast.
                captureReadyAt = Time.realtimeSinceStartupAsDouble + SettleSeconds;
                frameCount = 0;
                return;
            }

            if (Time.realtimeSinceStartupAsDouble < captureReadyAt)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            hud.RefreshInterface();
            Canvas.ForceUpdateCanvases();
            VerifyFocus(current);
            SaveFrame(current.Name, camera);
            ReleaseRenderTarget(camera);
            frameCount = 0;
            Debug.Log("UI_CAPTURE_OK " + current.Name);

            if (++caseIndex == Cases.Length)
            {
                Exit(0);
            }
        }
        catch (Exception error)
        {
            Debug.LogException(error);
            Exit(1);
        }
    }

    private static void PrepareRenderTarget(Vector2Int size, Camera camera, Canvas canvas)
    {
        target = new RenderTexture(size.x, size.y, 24, RenderTextureFormat.ARGB32);
        target.Create();
        camera.targetTexture = target;
        // Evidence only: scene settings are never saved by this batch runner.
        camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;
        canvas.planeDistance = 1f;
    }

    private static void VerifyFocus(CaptureCase current)
    {
        if (current.ExpectedFocus != null && EventSystem.current.currentSelectedGameObject?.name != current.ExpectedFocus)
        {
            throw new InvalidOperationException("Unexpected modal focus; expected " + current.ExpectedFocus);
        }
    }

    private static void SaveFrame(string name, Camera camera)
    {
        string directory = Path.GetFullPath(Path.Combine(Application.dataPath, "../../.impeccable/review"));
        Directory.CreateDirectory(directory);
        RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
        RenderTexture previous = RenderTexture.active;
        var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
        try
        {
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            image.Apply();
            File.WriteAllBytes(Path.Combine(directory, name + ".png"), image.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = previous;
            UnityEngine.Object.DestroyImmediate(image);
        }
    }

    private static void ReleaseRenderTarget(Camera camera)
    {
        camera.targetTexture = null;
        target.Release();
        UnityEngine.Object.DestroyImmediate(target);
        target = null;
    }

    private static void PrepareSelection()
    {
        var controller = UnityEngine.Object.FindFirstObjectByType<ChessGameController>();
        var board = UnityEngine.Object.FindFirstObjectByType<BoardView>();
        controller.SelectPiece(board.Pieces.First(piece => piece.Square.Equals(new BoardSquare(3, 1))));
    }

    private static void PreparePromotion()
    {
        var controller = UnityEngine.Object.FindFirstObjectByType<ChessGameController>();
        var board = UnityEngine.Object.FindFirstObjectByType<BoardView>();
        controller.StartLocalGame();
        // Deliberately test-only: avoid adding mutable rule access to the runtime controller.
        var field = typeof(ChessGameController).GetField("rules", BindingFlags.NonPublic | BindingFlags.Instance);
        var rules = (ChessRulesAdapter)field.GetValue(controller);
        rules.Reset("7k/P7/8/8/8/8/8/7K w - - 0 1");
        board.SyncPieces(rules.GetPieces(), UnityEngine.Object.FindFirstObjectByType<PieceFactory>());
        controller.SelectPiece(board.Pieces.First(piece => piece.Square.Equals(new BoardSquare(0, 7))));
        controller.SelectDestination(new BoardSquare(0, 8));
    }

    private static void PrepareEngineFailure()
    {
        var controller = UnityEngine.Object.FindFirstObjectByType<ChessGameController>();
        controller.SetMoveChooserFactory(() => throw new InvalidOperationException("Visual review: unavailable engine fixture"));
        controller.StartComputerGame(ChessSide.Black, ComputerDifficulty.Beginner);
    }

    private static void Click(params string[] names)
    {
        foreach (string name in names)
        {
            GameObject.Find(name).GetComponent<Button>().onClick.Invoke();
        }
    }

    private static void Exit(int code)
    {
        SessionState.SetBool(ActiveKey, false);
        EditorApplication.Exit(code);
    }
}
