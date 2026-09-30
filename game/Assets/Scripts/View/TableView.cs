using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

// The table under the board, modelled in VR meters. The desktop shows the same room scaled
// up around its board (BoardView.FitVrRoomToMode), so one model serves both modes.
// Its height panel moves only the table and the board on it; the camera never follows.
public sealed class TableView : MonoBehaviour
{
    // Top surface under the board frame's base; BoardView places the VR board at 0.78 m.
    public const float DefaultHeight = 0.7557f;

    private const string HeightStepKey = "ChessCgi.TableHeightStep";
    private const float TopSize = 0.9f;
    private const float TopThickness = 0.036f;
    private const float RevealThickness = 0.01f;
    private const float ApronHeight = 0.075f;
    private const float ApronThickness = 0.02f;
    private const float LegInset = 0.375f;
    private const float LegSize = 0.045f;
    private const float FootHeight = 0.018f;

    private const float LabDeskWidth = 1.3f;
    private const float LabDeskDepth = 0.9f;
    private const float LabDeskThickness = 0.028f;
    private const float LabDeskMargin = 0.01f;
    private const float LabDeskLegSplit = 0.2f;
    private const float LabDeskReach = 1f;
    private static readonly string[] LabDeskMarkers = { "ChessTableSurface", "BoardAnchor" };

    // An edge-mounted control on the player's left, entirely below the tabletop.
    private static readonly Vector3 PanelPosition = new Vector3(-0.30f, -0.018f, -TopSize * 0.5f - 0.008f);
    private const float PanelTilt = 25f;
    private const float PanelWidth = 0.27f;
    private const float PanelHeight = 0.185f;
    private const float PanelPixelsPerMeter = 2000f;
    private const float ButtonDiameter = 180f; // 9 cm in the metre-scale VR room.

    private static readonly Color PanelColor = new Color32(21, 27, 34, 255);
    private static readonly Color PanelBorderColor = new Color32(70, 79, 92, 255);
    private static readonly Color RaiseColor = new Color32(232, 49, 58, 255);
    private static readonly Color LowerColor = new Color32(36, 118, 229, 255);
    private static readonly Color ChoiceBorderColor = new Color32(62, 72, 84, 255);
    private static readonly Color MutedTextColor = new Color32(213, 221, 231, 255);
    private static readonly Color AccentColor = new Color32(255, 221, 0, 255);

    [SerializeField] private int minStep = -4;
    [SerializeField] private int maxStep = 6;
    [SerializeField] private float vrStepMeters = 0.03f;
    // 0.25 board units on the desktop, where the room is scaled up around the board.
    [SerializeField] private float desktopStepMeters = 0.01125f;

    private readonly List<Transform> legs = new List<Transform>();
    private readonly List<Image> levelTicks = new List<Image>();
    private readonly List<LabDeskMesh> labDeskMeshes = new List<LabDeskMesh>();
    private readonly List<LabDeskMarker> labDeskMarkers = new List<LabDeskMarker>();
    private Transform labRoom;
    private Transform top;
    private Transform panel;
    private Canvas panelCanvas;
    private Button raiseButton;
    private Button lowerButton;
    private readonly List<PhysicalTableButton> physicalButtons = new List<PhysicalTableButton>();
    private Text heightText;
    private Material frameMaterial;
    private BoardView board;
    private CameraController cameraController;
    private bool headset;
    private bool panelOnBlackSide;

    public int HeightStep { get; private set; }
    public int MinStep => minStep;
    public int MaxStep => maxStep;
    public float Height => DefaultHeight + HeightStep * (headset ? vrStepMeters : desktopStepMeters);
    public float BoardOffset => (Height - DefaultHeight) * transform.lossyScale.y;
    public bool CanRaise => HeightStep < maxStep;
    public bool CanLower => HeightStep > minStep;

    public void Build()
    {
        headset = XRRig.IsHeadsetPresent;
        Material topMaterial = ScenePolish.CreateMaterial("Runtime_Table_Top", new Color(0.34f, 0.2f, 0.11f), 0f, 0.32f);
        frameMaterial = ScenePolish.CreateMaterial("Runtime_Table_Frame", new Color(0.22f, 0.13f, 0.075f), 0f, 0.28f);
        Material footMaterial = ScenePolish.CreateMaterial("Runtime_Table_Feet", new Color(0.74f, 0.6f, 0.36f), 0.85f, 0.62f);

        top = new GameObject("Top").transform;
        top.SetParent(transform, false);
        // The top keeps its collider so it blocks the VR ray from reaching the HUD behind it.
        ScenePolish.CreateCube(top, "Tabletop", new Vector3(0f, -TopThickness * 0.5f, 0f),
            new Vector3(TopSize, TopThickness, TopSize), topMaterial, true);
        // A recessed band under the top reads as a shadow line, so the top looks thin and crisp.
        ScenePolish.CreateCube(top, "EdgeReveal", new Vector3(0f, -TopThickness - RevealThickness * 0.5f, 0f),
            new Vector3(TopSize - 0.04f, RevealThickness, TopSize - 0.04f), frameMaterial, false);

        float apronY = -TopThickness - RevealThickness - ApronHeight * 0.5f;
        float apronOffset = LegInset + (LegSize - ApronThickness) * 0.5f;
        float apronLength = LegInset * 2f;
        ScenePolish.CreateCube(top, "ApronFront", new Vector3(0f, apronY, -apronOffset),
            new Vector3(apronLength, ApronHeight, ApronThickness), frameMaterial, false);
        ScenePolish.CreateCube(top, "ApronBack", new Vector3(0f, apronY, apronOffset),
            new Vector3(apronLength, ApronHeight, ApronThickness), frameMaterial, false);
        ScenePolish.CreateCube(top, "ApronLeft", new Vector3(-apronOffset, apronY, 0f),
            new Vector3(ApronThickness, ApronHeight, apronLength), frameMaterial, false);
        ScenePolish.CreateCube(top, "ApronRight", new Vector3(apronOffset, apronY, 0f),
            new Vector3(ApronThickness, ApronHeight, apronLength), frameMaterial, false);

        legs.Clear();
        for (int i = 0; i < 4; i++)
        {
            float x = i % 2 == 0 ? -LegInset : LegInset;
            float z = i < 2 ? -LegInset : LegInset;
            legs.Add(ScenePolish.CreateCube(transform, "Leg", new Vector3(x, 0f, z), Vector3.one, frameMaterial, false).transform);
            ScenePolish.CreateCube(transform, "Foot", new Vector3(x, FootHeight * 0.5f, z),
                new Vector3(LegSize + 0.008f, FootHeight, LegSize + 0.008f), footMaterial, false);
        }

        ApplyHeight();
    }

    public void BuildOnLabDesk(Transform room)
    {
        headset = XRRig.IsHeadsetPresent;
        labRoom = room;
        frameMaterial = ScenePolish.CreateMaterial("Runtime_Table_Frame", new Color(0.22f, 0.13f, 0.075f), 0f, 0.28f);

        top = new GameObject("Top").transform;
        top.SetParent(transform, false);
        var tabletop = new GameObject("Tabletop");
        tabletop.transform.SetParent(top, false);
        tabletop.transform.localPosition = new Vector3(0f, -LabDeskThickness * 0.5f, 0f);
        tabletop.AddComponent<BoxCollider>().size = new Vector3(LabDeskWidth, LabDeskThickness, LabDeskDepth);

        labDeskMeshes.Clear();
        MeshFilter[] roomMeshes = Application.isPlaying ? room.GetComponentsInChildren<MeshFilter>(true) : new MeshFilter[0];
        foreach (MeshFilter filter in roomMeshes)
        {
            Mesh shared = filter.sharedMesh;
            if (shared == null || !shared.isReadable)
            {
                continue;
            }

            Matrix4x4 meshToRoom = room.worldToLocalMatrix * filter.transform.localToWorldMatrix;
            Vector3[] rest = shared.vertices;
            var moving = new List<int>();
            for (int i = 0; i < rest.Length; i++)
            {
                if (IsAboveLabDeskLegSplit(meshToRoom.MultiplyPoint3x4(rest[i])))
                {
                    moving.Add(i);
                }
            }

            if (moving.Count > 0)
            {
                labDeskMeshes.Add(new LabDeskMesh(filter.mesh, rest, moving.ToArray(), meshToRoom.inverse.MultiplyVector(Vector3.up)));
            }
        }

        labDeskMarkers.Clear();
        foreach (Transform part in room.GetComponentsInChildren<Transform>(true))
        {
            if (System.Array.IndexOf(LabDeskMarkers, part.name) >= 0)
            {
                labDeskMarkers.Add(new LabDeskMarker(part, room.InverseTransformPoint(part.position)));
            }
        }

        ApplyHeight();
        if (headset)
        {
            AddRoomColliders(room);
        }
    }

    private void AddRoomColliders(Transform room)
    {
        Physics.IgnoreLayerCollision(PhysicalTableButton.PhysicsLayer, room.gameObject.layer);
        foreach (MeshFilter filter in room.GetComponentsInChildren<MeshFilter>(true))
        {
            Mesh mesh = filter.sharedMesh;
            if (mesh == null || !mesh.isReadable)
            {
                continue;
            }

            MeshCollider collider = filter.gameObject.AddComponent<MeshCollider>();
            collider.sharedMesh = mesh;
            foreach (LabDeskMesh desk in labDeskMeshes)
            {
                if (desk.Mesh == mesh)
                {
                    desk.Collider = collider;
                }
            }
        }
    }

    private static bool IsAboveLabDeskLegSplit(Vector3 roomPoint)
    {
        return Mathf.Abs(roomPoint.x) <= LabDeskWidth * 0.5f + LabDeskMargin
            && Mathf.Abs(roomPoint.z) <= LabDeskDepth * 0.5f + LabDeskMargin
            && roomPoint.y > LabDeskLegSplit
            && roomPoint.y < LabDeskReach;
    }

    public void Raise()
    {
        SetHeightStep(HeightStep + 1);
    }

    public void Lower()
    {
        SetHeightStep(HeightStep - 1);
    }

    public void FocusControls()
    {
        if (headset || panelCanvas == null || cameraController == null) return;
        var rect = (RectTransform)panelCanvas.transform;
        Vector3 center = rect.TransformPoint(rect.rect.center);
        Vector3 position = center - rect.forward * (0.48f * transform.lossyScale.x);
        cameraController.LookAt(position, center);
    }

    public void SetHeightStep(int step)
    {
        HeightStep = Mathf.Clamp(step, minStep, maxStep);
        PlayerPrefs.SetInt(HeightStepKey, HeightStep);
        ApplyHeight();
    }

    private void Start()
    {
        // A table left over from a saved scene was never built; ScenePolish replaces it.
        if (top == null)
        {
            return;
        }

        board = Object.FindFirstObjectByType<BoardView>();
        cameraController = Object.FindFirstObjectByType<CameraController>();
        HeightStep = Mathf.Clamp(PlayerPrefs.GetInt(HeightStepKey, 0), minStep, maxStep);
        BuildPanel();
        ApplyHeight();
    }

    private void Update()
    {
        if (panelCanvas == null)
        {
            return;
        }

        if (headset && panelCanvas.worldCamera == null)
        {
            panelCanvas.worldCamera = XRRig.EyeCamera;
        }

        bool onBlackSide = PlayerOnBlackSide();
        if (onBlackSide != panelOnBlackSide)
        {
            PlacePanel(onBlackSide);
        }
    }

    private void ApplyHeight()
    {
        if (top == null)
        {
            return;
        }

        foreach (PhysicalTableButton button in physicalButtons) button.PrepareMountMove();
        top.localPosition = Vector3.up * Height;
        float lift = Height - DefaultHeight;
        foreach (LabDeskMesh desk in labDeskMeshes)
        {
            Vector3[] vertices = (Vector3[])desk.Rest.Clone();
            foreach (int index in desk.Moving)
            {
                vertices[index] += desk.Up * lift;
            }

            desk.Mesh.vertices = vertices;
            desk.Mesh.RecalculateBounds();
            if (desk.Collider != null)
            {
                desk.Collider.sharedMesh = null;
                desk.Collider.sharedMesh = desk.Mesh;
            }
        }

        foreach (LabDeskMarker marker in labDeskMarkers)
        {
            marker.Transform.position = labRoom.TransformPoint(marker.Rest + Vector3.up * lift);
        }

        // Adjustable legs: the feet stay on the floor and the legs reach the underside of the top.
        float legHeight = Height - TopThickness - FootHeight;
        foreach (Transform leg in legs)
        {
            leg.localScale = new Vector3(LegSize, legHeight, LegSize);
            leg.localPosition = new Vector3(leg.localPosition.x, FootHeight + legHeight * 0.5f, leg.localPosition.z);
        }

        if (board != null)
        {
            board.SetSurfaceOffset(BoardOffset);
        }

        foreach (PhysicalTableButton button in physicalButtons) button.FinishMountMove();
        RefreshPanel();
    }

    private void OnDestroy()
    {
        foreach (LabDeskMesh desk in labDeskMeshes)
        {
            Destroy(desk.Mesh);
        }
    }

    private bool PlayerOnBlackSide()
    {
        if (headset)
        {
            return XRRig.SeatedAsBlack;
        }

        return cameraController != null && cameraController.CurrentPerspective == ChessSide.Black;
    }

    private void PlacePanel(bool onBlackSide)
    {
        foreach (PhysicalTableButton button in physicalButtons) button.PrepareMountMove();
        panelOnBlackSide = onBlackSide;
        panel.localPosition = onBlackSide ? new Vector3(-PanelPosition.x, PanelPosition.y, -PanelPosition.z) : PanelPosition;
        panel.localRotation = Quaternion.Euler(0f, onBlackSide ? 180f : 0f, 0f);
        foreach (PhysicalTableButton button in physicalButtons) button.FinishMountMove();
    }

    private void BuildPanel()
    {
        panel = new GameObject("HeightPanel").transform;
        panel.SetParent(top, false);
        Transform tilt = new GameObject("Tilt").transform;
        tilt.SetParent(panel, false);
        // The upper edge is fixed to the fascia; the lower edge angles out toward the seat.
        tilt.localRotation = Quaternion.Euler(PanelTilt, 0f, 0f);
        Material mountMaterial = ScenePolish.CreateMaterial("Runtime_Table_ControlMount", PanelBorderColor, 0.6f, 0.35f);
        ScenePolish.CreateCube(tilt, "PanelBase", new Vector3(0f, -PanelHeight * 0.5f, 0f),
            new Vector3(PanelWidth + 0.008f, PanelHeight + 0.008f, 0.012f), mountMaterial, false);
        foreach (float x in new[] { -PanelWidth * 0.35f, PanelWidth * 0.35f })
        {
            ScenePolish.CreateCube(panel, "EdgeMount", new Vector3(x, 0f, 0.008f),
                new Vector3(0.018f, 0.026f, 0.026f), mountMaterial, false);
        }

        var canvasObject = new GameObject("HeightPanelCanvas", typeof(RectTransform));
        var canvasRect = (RectTransform)canvasObject.transform;
        canvasRect.SetParent(tilt, false);
        canvasRect.localPosition = new Vector3(0f, 0f, -0.0065f);
        canvasRect.pivot = new Vector2(0.5f, 1f);
        canvasRect.sizeDelta = new Vector2(PanelWidth, PanelHeight) * PanelPixelsPerMeter;
        canvasRect.localScale = Vector3.one / PanelPixelsPerMeter;
        panelCanvas = canvasObject.AddComponent<Canvas>();
        panelCanvas.renderMode = RenderMode.WorldSpace;
        panelCanvas.worldCamera = headset ? XRRig.EyeCamera : Camera.main;
        if (headset)
        {
            var raycaster = canvasObject.AddComponent<TrackedDeviceGraphicRaycaster>();
            raycaster.checkFor3DOcclusion = true;
            raycaster.blockingMask = ~(1 << XRPhysicsPusher.PhysicsLayer);
        }
        else
        {
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        Font bold = Resources.Load<Font>("UI/Lato-Bold");
        float width = canvasRect.sizeDelta.x;
        float height = canvasRect.sizeDelta.y;
        Surface(Rect("Background", canvasRect, 0f, 0f, width, height), PanelColor, PanelBorderColor, 18f, 2f);
        Label("Title", canvasRect, "ALTURA DA MESA", 20, MutedTextColor, bold, 0f, 14f, width, 28f);
        raiseButton = PanelButton("TableRaiseButton", canvasRect, "Subir", true, RaiseColor, 54f, bold, Raise);
        lowerButton = PanelButton("TableLowerButton", canvasRect, "Descer", false, LowerColor, 306f, bold, Lower);
        BuildLevelTicks(canvasRect);
        heightText = Label("HeightText", canvasRect, "", 24, Color.white, bold, 392f, 312f, 116f, 36f);
        PlacePanel(PlayerOnBlackSide());
    }

    private void BuildLevelTicks(Transform parent)
    {
        levelTicks.Clear();
        int count = maxStep - minStep + 1;
        const float gap = 4f;
        float width = (328f - gap * (count - 1)) / count;
        for (int i = 0; i < count; i++)
        {
            Image tick = Rect("LevelTick", parent, 32f + i * (width + gap), 324f, width, 12f).gameObject.AddComponent<Image>();
            tick.raycastTarget = false;
            levelTicks.Add(tick);
        }
    }

    private void RefreshPanel()
    {
        if (raiseButton == null)
        {
            return;
        }

        raiseButton.interactable = CanRaise;
        lowerButton.interactable = CanLower;
        physicalButtons[0].SetAvailable(CanRaise);
        physicalButtons[1].SetAvailable(CanLower);
        for (int i = 0; i < levelTicks.Count; i++)
        {
            levelTicks[i].color = minStep + i <= HeightStep ? AccentColor : ChoiceBorderColor;
        }

        // The model height in centimeters; on the desktop it is the scaled room's equivalent.
        heightText.text = Mathf.RoundToInt(Height * 100f) + " cm";
    }

    private Button PanelButton(string name, Transform parent, string label, bool pointsUp, Color fill,
        float x, Font font, UnityAction action)
    {
        var mount = new GameObject(name + "Mechanism");
        mount.transform.SetParent(parent.parent, false);
        mount.transform.localPosition = new Vector3(
            (x + ButtonDiameter * .5f - PanelWidth * PanelPixelsPerMeter * .5f) / PanelPixelsPerMeter,
            -(58f + ButtonDiameter * .5f) / PanelPixelsPerMeter, -.025f);
        PhysicalTableButton physical = mount.AddComponent<PhysicalTableButton>();
        physical.Configure(ButtonDiameter / PanelPixelsPerMeter, fill, pointsUp, !headset, action);
        physicalButtons.Add(physical);
        // Desktop hits use the visible cap's own moving canvas, so the circular
        // hit area matches the face even when it is depressed or seen at an angle.
        TableHeightButtonGraphic surface = physical.FaceGraphic;
        surface.gameObject.name = name;
        Button button = surface.gameObject.AddComponent<Button>();
        button.targetGraphic = surface;
        button.transition = Selectable.Transition.None;
        button.enabled = !headset;
        // Keep keyboard navigation inside the HUD; this panel is pointed at, not tabbed to.
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        button.onClick.AddListener(physical.PressFromPointer);
        Label("Label", parent, label, 24, Color.white, font, x, 264f, ButtonDiameter, 36f);
        return button;
    }

    private static MenuSurface Surface(RectTransform rect, Color fill, Color border, float radius, float borderWidth)
    {
        MenuSurface surface = rect.gameObject.AddComponent<MenuSurface>();
        surface.color = fill;
        surface.BorderColor = border;
        surface.BorderWidth = borderWidth;
        surface.Radius = radius;
        surface.raycastTarget = false;
        return surface;
    }

    private static Text Label(string name, Transform parent, string value, int size, Color color, Font font,
        float x, float y, float width, float height)
    {
        Text label = Rect(name, parent, x, y, width, height).gameObject.AddComponent<Text>();
        label.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = size;
        label.color = color;
        label.alignment = TextAnchor.MiddleCenter;
        label.alignByGeometry = true;
        label.raycastTarget = false;
        label.text = value;
        return label;
    }

    private static RectTransform Rect(string name, Transform parent, float x, float y, float width, float height)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(x, -y);
        rect.sizeDelta = new Vector2(width, height);
        return rect;
    }

    private sealed class LabDeskMesh
    {
        public readonly Mesh Mesh;
        public readonly Vector3[] Rest;
        public readonly int[] Moving;
        public readonly Vector3 Up;
        public MeshCollider Collider;

        public LabDeskMesh(Mesh mesh, Vector3[] rest, int[] moving, Vector3 up)
        {
            Mesh = mesh;
            Rest = rest;
            Moving = moving;
            Up = up;
        }
    }

    private sealed class LabDeskMarker
    {
        public readonly Transform Transform;
        public readonly Vector3 Rest;

        public LabDeskMarker(Transform transform, Vector3 rest)
        {
            Transform = transform;
            Rest = rest;
        }
    }
}
