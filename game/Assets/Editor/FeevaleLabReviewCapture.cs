using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit.UI;

/// <summary>Actual Main captures, including the simulated headset startup path.</summary>
[InitializeOnLoad]
public static class FeevaleLabReviewCapture
{
    private const string ActiveKey = "ChessCGI.LabCapture";
    private const string VrKey = "ChessCGI.LabCapture.VR";
    private const string ReferenceKey = "ChessCGI.LabCapture.ReferenceRoom";
    private const string ControlsKey = "ChessCGI.LabCapture.TableControls";
    private const string HeightKey = "ChessCgi.TableHeightStep";
    private static readonly string[] ControlsNames = { "match", "controls", "mount", "pressed-controls", "raised-controls", "access" };
    private static readonly string[] DesktopNames = { "desktop-menu", "desktop-match", "desktop-move", "desktop-look-room", "desktop-look-right", "desktop-return-board" };
    private static readonly string[] VrNames = { "vr-menu", "vr-play", "vr-table", "vr-overview", "vr-room", "vr-black-side", "vr-right-windows", "vr-table-detail" };
    private static readonly string[] ReferenceVrNames = { "vr-menu", "vr-play", "vr-table", "vr-overview", "vr-room", "vr-black-side", "vr-right-windows", "vr-table-detail", "vr-blue-wall", "vr-blinds-storage", "vr-ceiling", "room-layout" };
    private static int index, frames;
    private static bool loadedVr;
    private static RenderTexture target;
    private static Camera camera;
    private static double readyAt;
    private static TableButtonPhysicsReview physicalReview;

    static FeevaleLabReviewCapture()
    {
        EditorApplication.update += Tick;
        EditorApplication.quitting += RestoreControlsHeight;
    }

    public static void RunDesktop()
    {
        FeevaleLabImport.Build();
        Start(false);
    }

    public static void RunVr() => Start(true);

    // Review existing imports without regenerating prefabs or replacing older evidence.
    public static void RunTabletopDesktop() => StartTabletop(false);
    public static void RunTabletopVr() => StartTabletop(true);

    public static void RunReferenceDesktop() => StartReference(false);
    public static void RunReferenceVr() => StartReference(true);

    public static void RunTableControlsDesktop() => StartTableControls(false);
    public static void RunTableControlsVr() => StartTableControls(true);

    private static void StartTableControls(bool vr)
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Use the batch Editor; no scene is saved.");
        SessionState.SetBool(ControlsKey, true);
        SessionState.SetBool(ControlsKey + ".HadHeight", PlayerPrefs.HasKey(HeightKey));
        SessionState.SetInt(ControlsKey + ".Height", PlayerPrefs.GetInt(HeightKey, 0));
        PlayerPrefs.DeleteKey(HeightKey);
        SessionState.SetString("ChessCGI.LabCapture.Output", "../../.local/table-physical-buttons-20260929");
        Start(vr);
    }

    private static void StartReference(bool vr)
    {
        SessionState.SetBool(ReferenceKey, true);
        SessionState.SetString("ChessCGI.LabCapture.Output", "../../art/character-variants/feevale-room-v3-20260928/unity");
        Start(vr);
    }

    private static void StartTabletop(bool vr)
    {
        string folder = Environment.GetCommandLineArgs().Contains("-reviewDisableReflection") ? "diagnostic-no-reflection" : "unity";
        if (Environment.GetCommandLineArgs().Contains("-reviewDisableShadows")) folder = "diagnostic-no-shadows";
        SessionState.SetString("ChessCGI.LabCapture.Output", "../../art/character-variants/tabletop-polish-20260926/" + folder);
        Start(vr);
    }

    private static void Start(bool vr)
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Use the batch Editor; no scene is saved.");
        if (vr) EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        else EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");
        SessionState.SetBool(VrKey, vr);
        SessionState.SetBool(ActiveKey, true);
        EditorApplication.isPlaying = true;
    }

    private static void Tick()
    {
        if (!SessionState.GetBool(ActiveKey, false) || !EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        bool vr = SessionState.GetBool(VrKey, false);
        try
        {
            if (++frames < 35) return;
            if (vr && !loadedVr)
            {
                InputSystem.AddDevice<XRSimulatedHMD>();
                SceneManager.LoadScene("Main");
                loadedVr = true; frames = 0;
                return;
            }
            var hud = UnityEngine.Object.FindFirstObjectByType<GameHud>();
            if (hud == null) return;
            if (vr && UnityEngine.Object.FindFirstObjectByType<XRRig>() == null)
            {
                new GameObject("Review XR Bootstrap").AddComponent<XRRig>();
                frames = 0;
                return;
            }
            camera = vr ? XRRig.EyeCamera : Camera.main;
            if (camera == null) return;
            bool controls = SessionState.GetBool(ControlsKey, false);
            string[] names = controls ? ControlsNames : vr ? (SessionState.GetBool(ReferenceKey, false) ? ReferenceVrNames : VrNames) : DesktopNames;
            string name = (controls ? (vr ? "vr-" : "desktop-") : "") + names[index];
            if (target == null)
            {
                Prepare(hud, vr);
                target = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
                target.Create(); camera.targetTexture = target;
                camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
                if (!vr)
                {
                    Canvas canvas = hud.GetComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
                }
                readyAt = Time.realtimeSinceStartupAsDouble + 1.0;
                frames = 0;
                return;
            }
            if (Time.realtimeSinceStartupAsDouble < readyAt) return;
            if (controls && index == 3)
            {
                if (physicalReview == null) throw new InvalidOperationException("Physics review was not started.");
                if (!physicalReview.Finished) return;
                if (physicalReview.Error != null) throw physicalReview.Error;
            }
            if (!controls && !vr && index == 2 && !UnityEngine.Object.FindFirstObjectByType<BoardView>().Pieces
                    .Any(p => p.Square.Equals(BoardSquare.FromAlgebraic("e4"))))
                throw new InvalidOperationException("Review move e2-e4 did not complete.");
            Canvas.ForceUpdateCanvases(); hud.RefreshInterface(); Canvas.ForceUpdateCanvases();
            if (controls && vr && index == 1) VerifyTableControlsRequireContact();
            RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
            RenderTexture previous = RenderTexture.active;
            var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            try
            {
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); image.Apply();
                string directory = Path.GetFullPath(Path.Combine(Application.dataPath, SessionState.GetString(
                    "ChessCGI.LabCapture.Output", "../../art/character-variants/feevale-room-v2-20260925/unity")));
                Directory.CreateDirectory(directory);
                File.WriteAllBytes(Path.Combine(directory, name + ".png"), image.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                UnityEngine.Object.DestroyImmediate(image);
                camera.targetTexture = null; target.Release(); UnityEngine.Object.DestroyImmediate(target); target = null;
            }
            Debug.Log("LAB_CAPTURE_OK " + name);
            if (controls && index == 3) UnityEngine.Object.Destroy(physicalReview.gameObject);
            frames = 0;
            int count = controls && vr ? 5 : names.Length;
            if (++index == count) Exit(0);
        }
        catch (Exception error) { Debug.LogException(error); Exit(1); }
    }

    private static void Prepare(GameHud hud, bool vr)
    {
        if (SessionState.GetBool(ControlsKey, false))
        {
            PrepareTableControls(hud, vr);
            return;
        }
        if (Environment.GetCommandLineArgs().Contains("-reviewDisableReflection"))
            foreach (ReflectionProbe probe in UnityEngine.Object.FindObjectsByType<ReflectionProbe>(FindObjectsSortMode.None))
                probe.intensity = 0;
        if (Environment.GetCommandLineArgs().Contains("-reviewDisableShadows"))
            foreach (Light light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                light.shadows = LightShadows.None;
        var controller = UnityEngine.Object.FindFirstObjectByType<ChessGameController>();
        var board = UnityEngine.Object.FindFirstObjectByType<BoardView>();
        if (index == 1)
        {
            UnityEngine.Object.FindFirstObjectByType<PieceFactory>().UsePrimitivePieces = false;
            GameObject.Find("LocalModeButton").GetComponent<Button>().onClick.Invoke();
            GameObject.Find("StartPlayButton").GetComponent<Button>().onClick.Invoke();
        }
        if (!vr)
        {
            var view = UnityEngine.Object.FindFirstObjectByType<CameraController>();
            if (index == 2)
            {
                controller.SelectPiece(board.Pieces.First(p => p.Square.Equals(BoardSquare.FromAlgebraic("e2"))));
                controller.SelectDestination(BoardSquare.FromAlgebraic("e4"));
            }
            if (index == 3)
            {
                GameObject.Find("RoomViewButton").GetComponent<Button>().onClick.Invoke();
                if (!view.IsLookingAround) throw new InvalidOperationException("Desktop room button did not activate.");
            }
            if (index == 4)
                camera.transform.rotation = Quaternion.Euler(-2, 95, 0);
            if (index == 5)
            {
                GameObject.Find("RoomViewButton").GetComponent<Button>().onClick.Invoke();
                if (view.IsLookingAround) throw new InvalidOperationException("Desktop return did not restore the board.");
            }
            return;
        }
        if (Mathf.Abs(board.transform.lossyScale.x - .045f) > .0001f)
            throw new InvalidOperationException("The real simulated-headset startup path did not use VR board scale.");
        if (hud.GetComponent<Canvas>().renderMode != RenderMode.WorldSpace)
            throw new InvalidOperationException("Simulated VR must use the actual world-space HUD.");
        var pose = camera.GetComponent<TrackedPoseDriver>();
        if (pose != null) pose.enabled = false;
        // No controllers are tracked by this HMD-only capture. Hide their
        // untracked origin poses in the evidence; runtime prefabs are untouched.
        var modality = UnityEngine.Object.FindFirstObjectByType<XRInputModalityManager>();
        if (modality != null) modality.enabled = false;
        foreach (string name in new[] { "Left Controller", "Right Controller", "LeftHandVisual", "RightHandVisual", "LeftHandInteractor", "RightHandInteractor" })
        {
            GameObject untracked = GameObject.Find(name);
            if (untracked != null) untracked.SetActive(false);
        }
        camera.stereoTargetEye = StereoTargetEyeMask.None;
        camera.fieldOfView = 72;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.36f, .34f, .31f);
        Vector3 position = XRRig.SeatEyePosition;
        Vector3 look = new Vector3(0, .82f, 0);
        if (index == 0) look = new Vector3(0, 1.6f, 1.35f);
        if (index >= 2) hud.GetComponent<Canvas>().enabled = false;
        switch (index)
        {
            case 3: position = new Vector3(2.75f, 1.85f, -2.5f); look = new Vector3(0, 1.15f, 1.1f); break;
            case 4: look = new Vector3(0, 1.42f, 4.8f); break;
            case 5: position = new Vector3(0, 1.2f, .6f); look = new Vector3(0, 1.1f, -4.6f); break;
            case 6: look = new Vector3(3.4f, 1.85f, .1f); break;
            case 7: position = new Vector3(.57f, 1.06f, -.43f); look = new Vector3(.05f, .80f, 0); camera.fieldOfView = 65; break;
            case 8: position = new Vector3(2.85f, 1.65f, .85f); look = new Vector3(-.20f, 1.40f, -4.5f); break;
            case 9: position = new Vector3(-1.65f, 1.50f, -3.75f); look = new Vector3(-3.15f, 1.40f, 2.8f); break;
            case 10: position = new Vector3(-2.35f, 1.55f, -3.55f); look = new Vector3(2.65f, 2.45f, 3.75f); break;
            case 11: position = new Vector3(0, 2.78f, .1f); look = new Vector3(0, 0, .1f); camera.orthographic = true; camera.orthographicSize = 5.05f; break;
        }
        Quaternion rotation = index == 11 ? Quaternion.Euler(90, 0, 0) : Quaternion.LookRotation(look - position);
        camera.transform.SetPositionAndRotation(position, rotation);
    }

    private static void PrepareTableControls(GameHud hud, bool vr)
    {
        if (index == 0)
        {
            GameObject.Find("LocalModeButton").GetComponent<Button>().onClick.Invoke();
            GameObject.Find("StartPlayButton").GetComponent<Button>().onClick.Invoke();
        }
        if (vr)
        {
            var board = UnityEngine.Object.FindFirstObjectByType<BoardView>();
            if (Mathf.Abs(board.transform.lossyScale.x - .045f) > .0001f)
                throw new InvalidOperationException("Table controls review must use the actual simulated VR startup.");
            var pose = camera.GetComponent<TrackedPoseDriver>();
            if (pose != null) pose.enabled = false;
            var modality = UnityEngine.Object.FindFirstObjectByType<XRInputModalityManager>();
            if (modality != null) modality.enabled = false;
            foreach (string name in new[] { "Left Controller", "Right Controller", "LeftHandVisual", "RightHandVisual", "LeftHandInteractor", "RightHandInteractor" })
            {
                GameObject untracked = GameObject.Find(name);
                if (untracked != null) untracked.SetActive(false);
            }
            camera.stereoTargetEye = StereoTargetEyeMask.None;
            if (index == 0)
                camera.transform.SetPositionAndRotation(XRRig.SeatEyePosition, Quaternion.LookRotation(new Vector3(0, .78f, 0) - XRRig.SeatEyePosition));
        }
        var view = UnityEngine.Object.FindFirstObjectByType<CameraController>();
        if (view != null) view.enabled = false;
        if (index == 0) return;
        var table = UnityEngine.Object.FindFirstObjectByType<TableView>();
        if (index == 5)
        {
            table.SetHeightStep(0);
            hud.GetComponent<Canvas>().enabled = true;
            if (view != null) view.enabled = true;
            GameObject.Find("TableControlsButton").GetComponent<Button>().onClick.Invoke();
            return;
        }
        hud.GetComponent<Canvas>().enabled = false;
        if (index == 4) table.SetHeightStep(table.MaxStep);
        if (index == 3)
        {
            table.SetHeightStep(0);
            physicalReview = new GameObject("Physical table-button review").AddComponent<TableButtonPhysicsReview>();
            physicalReview.Begin(table, vr);
        }
        var rect = GameObject.Find("HeightPanelCanvas").GetComponent<RectTransform>();
        Vector3 center = rect.TransformPoint(rect.rect.center);
        Vector3 face = -rect.forward;
        float unit = table.transform.lossyScale.x;
        Vector3 position = center + face * (.48f * unit);
        if (index == 2 || index == 3) position += rect.right * (.25f * unit) + Vector3.up * (.12f * unit);
        camera.fieldOfView = index == 2 || index == 3 ? 48 : 38;
        camera.transform.SetPositionAndRotation(position, Quaternion.LookRotation(center - position));
    }

    private static void VerifyTableControlsRequireContact()
    {
        var table = UnityEngine.Object.FindFirstObjectByType<TableView>();
        var raycaster = GameObject.Find("HeightPanelCanvas").GetComponent<TrackedDeviceGraphicRaycaster>();
        int step = table.HeightStep;
        Vector3 origin = XRRig.SeatAwarePoint(new Vector3(-.3f, .95f, -.62f));
        foreach (string name in new[] { "TableRaiseButton", "TableLowerButton" })
        {
            var button = GameObject.Find(name).GetComponent<Button>();
            if (button.enabled || button.targetGraphic.raycastTarget)
                throw new InvalidOperationException("VR table buttons must be physical contact targets.");
            var rect = (RectTransform)button.transform;
            Vector3 center = rect.TransformPoint(rect.rect.center);
            var pointer = new TrackedDeviceEventData(EventSystem.current)
            {
                button = PointerEventData.InputButton.Left,
                layerMask = ~0,
                rayPoints = new List<Vector3> { origin, center + (center - origin).normalized * .2f }
            };
            var hits = new List<RaycastResult>();
            raycaster.Raycast(pointer, hits);
            if (hits.Any(hit => hit.gameObject == button.gameObject))
                throw new InvalidOperationException("A ray must not activate the physical cap.");
            button.onClick.Invoke();
            if (table.HeightStep != step)
                throw new InvalidOperationException("VR activation requires physical travel, not a UI click.");
        }
        foreach (string name in new[] { "LeftHandVisual Index Contact", "RightHandVisual Index Contact",
            "Left Controller Contact", "Right Controller Contact", "Left Controller Index Contact", "Right Controller Index Contact" })
            if (GameObject.Find(name)?.GetComponent<XRPhysicsPusher>() == null)
                throw new InvalidOperationException("VR rig did not create its physics contact: " + name);
        Debug.Log("TABLE_CONTROLS_VR_CONTACT_TARGETS_PASSED");
    }

    private static void Exit(int code)
    {
        RestoreControlsHeight();
        SessionState.SetBool(ActiveKey, false);
        SessionState.EraseBool(ReferenceKey);
        SessionState.EraseString("ChessCGI.LabCapture.Output");
        EditorApplication.isPlaying = false;
        EditorApplication.Exit(code);
    }

    private static void RestoreControlsHeight()
    {
        if (SessionState.GetBool(ControlsKey, false))
        {
            if (SessionState.GetBool(ControlsKey + ".HadHeight", false))
                PlayerPrefs.SetInt(HeightKey, SessionState.GetInt(ControlsKey + ".Height", 0));
            else PlayerPrefs.DeleteKey(HeightKey);
            SessionState.EraseBool(ControlsKey);
            SessionState.EraseBool(ControlsKey + ".HadHeight");
            SessionState.EraseInt(ControlsKey + ".Height");
        }
    }
}
