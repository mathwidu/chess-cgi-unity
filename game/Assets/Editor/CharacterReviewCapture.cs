using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

// Captures imported assets and the actual Main scene; never saves scene or player preferences.
[InitializeOnLoad]
public static class CharacterReviewCapture
{
    private const string ActiveKey = "ChessCGI.CharacterReviewCapture";
    private const int GalleryLayer = 28;
    private static readonly string[] Names = {
        "menu", "board", "selected-white", "move", "selected-black", "board-top", "cast-front", "cast-top",
        "detail-pawn", "detail-rook", "detail-knight", "detail-bishop", "detail-queen", "detail-king",
        "back-pawn", "back-rook", "back-knight", "back-bishop", "back-queen", "back-king", "cast-back",
        "detail-black-pawn", "detail-black-rook", "detail-black-knight", "detail-black-bishop", "detail-black-queen", "detail-black-king",
        "back-black-pawn", "back-black-rook", "back-black-knight", "back-black-bishop", "back-black-queen", "back-black-king"
    };
    private static readonly string[] Prefabs = { "Pawn_Mathwidu_Redhead_v2", "Rook_Alex", "Knight_Gustavo", "Bishop_Rafael", "Queen_Marta", "King_Ricardo_Carioca" };
    private static int index;
    private static int frames;
    private static double readyAt;
    private static Camera camera;
    private static RenderTexture target;
    private static GameObject gallery;

    static CharacterReviewCapture() => EditorApplication.update += Tick;

    public static void Run()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Use a separate batch Editor for captures.");
        CharacterVariantImport.Build();
        EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");
        SessionState.SetBool(ActiveKey, true);
        EditorApplication.isPlaying = true;
    }

    private static void Tick()
    {
        if (!SessionState.GetBool(ActiveKey, false) || !EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        try
        {
            if (++frames < 45) return;
            var hud = UnityEngine.Object.FindFirstObjectByType<GameHud>();
            if (hud == null) return;
            if (target == null)
            {
                camera = Camera.main;
                Prepare(hud);
                bool detail = Names[index].StartsWith("detail-") || Names[index].StartsWith("back-");
                target = new RenderTexture(detail ? 1100 : 1920, detail ? 1400 : 1080, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
                target.Create();
                camera.targetTexture = target;
                // Review captures use neutral output so the HUD is not tone-mapped
                // when converted from ScreenSpaceOverlay to a render texture.
                // Main's in-game post-processing assets remain unchanged.
                camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
                Canvas canvas = hud.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1;
                readyAt = Time.realtimeSinceStartupAsDouble + 1.5;
                frames = 0;
                return;
            }
            if (Time.realtimeSinceStartupAsDouble < readyAt) return;
            Canvas.ForceUpdateCanvases();
            hud.RefreshInterface();
            Canvas.ForceUpdateCanvases();
            RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
            RenderTexture previous = RenderTexture.active;
            var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            try
            {
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                image.Apply();
                if (index >= 1 && index <= 4)
                {
                    Color corner = image.GetPixel(32, target.height - 32);
                    if (corner.g < corner.r * 1.3f)
                        throw new InvalidOperationException("Main capture is missing the green HUD panel: " + corner);
                }
                string directory = Path.GetFullPath(Path.Combine(Application.dataPath, "../../art/character-variants/production/unity"));
                Directory.CreateDirectory(directory);
                File.WriteAllBytes(Path.Combine(directory, Names[index] + ".png"), image.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                UnityEngine.Object.DestroyImmediate(image);
                camera.targetTexture = null;
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                target = null;
            }
            Debug.Log("CHARACTER_CAPTURE_OK " + Names[index]);
            frames = 0;
            if (++index == Names.Length) Exit(0);
        }
        catch (Exception error)
        {
            Debug.LogException(error);
            Exit(1);
        }
    }

    private static void Prepare(GameHud hud)
    {
        var controller = UnityEngine.Object.FindFirstObjectByType<ChessGameController>();
        var board = UnityEngine.Object.FindFirstObjectByType<BoardView>();
        if (index == 20)
        {
            foreach (Transform child in gallery.transform) child.gameObject.SetActive(true);
            camera.cullingMask = 1 << GalleryLayer;
            camera.orthographicSize = 2.35f;
            camera.transform.position = gallery.transform.position + new Vector3(0, 6.5f, 10);
            camera.transform.LookAt(gallery.transform.position + new Vector3(0, .6f, 1.15f));
            return;
        }
        if (index >= 8)
        {
            bool black = index >= 21;
            int column = (index - (black ? 21 : 8)) % 6;
            bool back = index >= (black ? 27 : 14);
            float rowZ = black ? 2.3f : 0;
            Vector3 center = gallery.transform.position + new Vector3((column - 2.5f) * 1.16f, .72f, rowZ);
            foreach (Transform child in gallery.transform)
                if (child.GetComponentInChildren<CustomPieceAppearance>(true) != null)
                    child.gameObject.SetActive(Mathf.Abs(child.localPosition.x - (column - 2.5f) * 1.16f) < .01f && Mathf.Abs(child.localPosition.z - rowZ) < .1f);
            camera.cullingMask = 1 << GalleryLayer;
            camera.orthographicSize = .83f;
            camera.transform.position = center + new Vector3(back ? -.5f : -1.2f, .8f, back ? 4 : -4);
            camera.transform.LookAt(center);
            return;
        }
        switch (index)
        {
            case 1:
                UnityEngine.Object.FindFirstObjectByType<PieceFactory>().UsePrimitivePieces = false;
                GameObject.Find("LocalModeButton").GetComponent<Button>().onClick.Invoke();
                GameObject.Find("StartPlayButton").GetComponent<Button>().onClick.Invoke();
                break;
            case 2:
                controller.SelectPiece(board.Pieces.First(p => p.Square.Equals(BoardSquare.FromAlgebraic("e2"))));
                break;
            case 3:
                controller.SelectDestination(BoardSquare.FromAlgebraic("e4"));
                break;
            case 4:
                controller.SelectPiece(board.Pieces.First(p => p.Square.Equals(BoardSquare.FromAlgebraic("e7"))));
                break;
            case 5:
                controller.StartLocalGame();
                UnityEngine.Object.FindFirstObjectByType<CameraController>().enabled = false;
                hud.GetComponent<Canvas>().enabled = false;
                camera.orthographic = true;
                camera.orthographicSize = 5.9f;
                camera.transform.position = new Vector3(0, 15, 0);
                camera.transform.rotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
                break;
            case 6:
                BuildGallery();
                camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
                camera.cullingMask = 1 << GalleryLayer;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.085f, .10f, .12f);
                camera.orthographic = true;
                camera.orthographicSize = 2.35f;
                camera.transform.position = gallery.transform.position + new Vector3(0, 6.5f, -8);
                camera.transform.LookAt(gallery.transform.position + new Vector3(0, .6f, 1.15f));
                break;
            case 7:
                camera.cullingMask = 1 << GalleryLayer;
                camera.orthographicSize = 2.30f;
                camera.transform.position = gallery.transform.position + new Vector3(0, 10, 1.15f);
                camera.transform.rotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
                break;
        }
    }

    private static void BuildGallery()
    {
        foreach (Light light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None)) light.enabled = false;
        gallery = new GameObject("Imported character review");
        gallery.transform.position = new Vector3(80, 0, 0);
        for (int column = 0; column < Prefabs.Length; column++)
        for (int row = 0; row < 2; row++)
        {
            var prefab = Resources.Load<GameObject>("CustomPieces/" + Prefabs[column]);
            var instance = UnityEngine.Object.Instantiate(prefab, gallery.transform);
            instance.transform.localPosition = new Vector3((column - 2.5f) * 1.16f, 0, row * 2.3f);
            instance.transform.localRotation = Quaternion.Euler(0, 180, 0);
            instance.GetComponentInChildren<CustomPieceAppearance>().ApplySide((ChessSide)row);
        }
        var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.transform.SetParent(gallery.transform, false);
        floor.transform.localPosition = new Vector3(0, -.004f, 1);
        floor.transform.localScale = Vector3.one * 4;
        floor.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(.19f, .205f, .22f) };
        foreach (Transform child in gallery.GetComponentsInChildren<Transform>()) child.gameObject.layer = GalleryLayer;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(.45f, .47f, .51f);
        AddLight("ReviewKey", new Vector3(45, -35, 0), 1.2f, new Color(1, .975f, .93f));
        AddLight("ReviewFill", new Vector3(25, 120, 0), .5f, new Color(.88f, .93f, 1));
    }

    private static void AddLight(string name, Vector3 angle, float intensity, Color color)
    {
        var light = new GameObject(name).AddComponent<Light>();
        light.transform.SetParent(gallery.transform, false);
        light.transform.localRotation = Quaternion.Euler(angle);
        light.type = LightType.Directional;
        light.cullingMask = 1 << GalleryLayer;
        light.intensity = intensity;
        light.color = color;
        light.shadows = name == "ReviewKey" ? LightShadows.Soft : LightShadows.None;
        light.shadowStrength = .55f;
    }

    private static void Exit(int code)
    {
        SessionState.EraseBool(ActiveKey);
        EditorApplication.isPlaying = false;
        EditorApplication.Exit(code);
    }
}
