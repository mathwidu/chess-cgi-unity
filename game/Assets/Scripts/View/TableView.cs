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
    // Top surface just under the board frame; BoardView places the VR board at 0.78 m.
    public const float DefaultHeight = 0.774f;

    private const string HeightStepKey = "ChessCgi.TableHeightStep";
    private const float TopSize = 0.9f;
    private const float TopThickness = 0.036f;
    private const float RevealThickness = 0.01f;
    private const float ApronHeight = 0.075f;
    private const float ApronThickness = 0.02f;
    private const float LegInset = 0.375f;
    private const float LegSize = 0.045f;
    private const float FootHeight = 0.018f;

    // A small lectern on the player's left, turned toward the seat and clear of the board.
    private static readonly Vector3 PanelPosition = new Vector3(-0.335f, 0f, 0.06f);
    private const float PanelYaw = -12f;
    private const float PanelTilt = 28f;
    private const float PanelWidth = 0.12f;
    private const float PanelDepth = 0.14f;
    private const float PanelPixelsPerMeter = 2000f;

    private static readonly Color PanelColor = new Color32(4, 43, 27, 255);
    private static readonly Color PanelBorderColor = new Color32(43, 86, 66, 255);
    private static readonly Color ChoiceColor = new Color32(19, 57, 43, 255);
    private static readonly Color ChoiceBorderColor = new Color32(49, 86, 69, 255);
    private static readonly Color MutedTextColor = new Color32(202, 228, 211, 255);
    private static readonly Color AccentColor = new Color32(255, 221, 0, 255);

    [SerializeField] private int minStep = -4;
    [SerializeField] private int maxStep = 6;
    [SerializeField] private float vrStepMeters = 0.03f;
    // 0.25 board units on the desktop, where the room is scaled up around the board.
    [SerializeField] private float desktopStepMeters = 0.01125f;

    private readonly List<Transform> legs = new List<Transform>();
    private readonly List<Image> levelTicks = new List<Image>();
    private Transform top;
    private Transform panel;
    private Canvas panelCanvas;
    private Button raiseButton;
    private Button lowerButton;
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
        ScenePolish.CreateCube(top, "Tabletop", new Vector3(0f, -TopThickness * 0.5f, 0f),
            new Vector3(TopSize, TopThickness, TopSize), topMaterial, false);
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

    public void Raise()
    {
        SetHeightStep(HeightStep + 1);
    }

    public void Lower()
    {
        SetHeightStep(HeightStep - 1);
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

        top.localPosition = Vector3.up * Height;
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

        RefreshPanel();
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
        panelOnBlackSide = onBlackSide;
        panel.localPosition = onBlackSide ? new Vector3(-PanelPosition.x, PanelPosition.y, -PanelPosition.z) : PanelPosition;
        panel.localRotation = Quaternion.Euler(0f, onBlackSide ? 180f + PanelYaw : PanelYaw, 0f);
    }

    private void BuildPanel()
    {
        panel = new GameObject("HeightPanel").transform;
        panel.SetParent(top, false);
        Transform tilt = new GameObject("Tilt").transform;
        tilt.SetParent(panel, false);
        // Hinged at the front edge: the far edge rises so the face looks up at the player.
        tilt.localRotation = Quaternion.Euler(-PanelTilt, 0f, 0f);
        ScenePolish.CreateCube(tilt, "PanelBase", new Vector3(0f, 0.006f, PanelDepth * 0.5f),
            new Vector3(PanelWidth + 0.01f, 0.012f, PanelDepth + 0.01f), frameMaterial, false);
        float rise = PanelDepth * Mathf.Sin(PanelTilt * Mathf.Deg2Rad);
        ScenePolish.CreateCube(panel, "PanelStand", new Vector3(0f, rise * 0.5f, PanelDepth * Mathf.Cos(PanelTilt * Mathf.Deg2Rad) - 0.008f),
            new Vector3(PanelWidth, rise, 0.014f), frameMaterial, false);

        var canvasObject = new GameObject("HeightPanelCanvas", typeof(RectTransform));
        var canvasRect = (RectTransform)canvasObject.transform;
        canvasRect.SetParent(tilt, false);
        canvasRect.localPosition = new Vector3(0f, 0.0125f, PanelDepth * 0.5f);
        canvasRect.localRotation = Quaternion.Euler(90f, 0f, 0f);
        canvasRect.sizeDelta = new Vector2(PanelWidth, PanelDepth) * PanelPixelsPerMeter;
        canvasRect.localScale = Vector3.one / PanelPixelsPerMeter;
        panelCanvas = canvasObject.AddComponent<Canvas>();
        panelCanvas.renderMode = RenderMode.WorldSpace;
        panelCanvas.worldCamera = headset ? XRRig.EyeCamera : Camera.main;
        if (headset)
        {
            canvasObject.AddComponent<TrackedDeviceGraphicRaycaster>();
        }
        else
        {
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        Font bold = Resources.Load<Font>("UI/Lato-Bold");
        Surface(Rect("Background", canvasRect, 0f, 0f, 240f, 280f), PanelColor, PanelBorderColor, 16f, 2f);
        Label("Title", canvasRect, "ALTURA DA MESA", 17, MutedTextColor, bold, 0f, 16f, 240f, 26f);
        raiseButton = PanelButton("TableRaiseButton", canvasRect, "↑  Subir", 52f, bold, Raise);
        BuildLevelTicks(canvasRect);
        heightText = Label("HeightText", canvasRect, "", 22, Color.white, bold, 0f, 148f, 240f, 32f);
        lowerButton = PanelButton("TableLowerButton", canvasRect, "↓  Descer", 196f, bold, Lower);
        PlacePanel(PlayerOnBlackSide());
    }

    private void BuildLevelTicks(Transform parent)
    {
        levelTicks.Clear();
        int count = maxStep - minStep + 1;
        const float gap = 4f;
        float width = (200f - gap * (count - 1)) / count;
        for (int i = 0; i < count; i++)
        {
            Image tick = Rect("LevelTick", parent, 20f + i * (width + gap), 130f, width, 10f).gameObject.AddComponent<Image>();
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
        for (int i = 0; i < levelTicks.Count; i++)
        {
            levelTicks[i].color = minStep + i <= HeightStep ? AccentColor : ChoiceBorderColor;
        }

        // The model height in centimeters; on the desktop it is the scaled room's equivalent.
        heightText.text = Mathf.RoundToInt(Height * 100f) + " cm";
    }

    private static Button PanelButton(string name, Transform parent, string label, float y, Font font, UnityAction action)
    {
        RectTransform rect = Rect(name, parent, 20f, y, 200f, 64f);
        // The clear root Image is the hit target for the mouse and the XR ray.
        rect.gameObject.AddComponent<Image>().color = Color.clear;
        MenuSurface surface = Surface(Rect("ButtonSurface", rect, 0f, 0f, 200f, 64f), ChoiceColor, ChoiceBorderColor, 10f, 1.5f);
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = surface;
        button.colors = new ColorBlock
        {
            normalColor = Color.white,
            selectedColor = Color.white,
            highlightedColor = new Color(0.91f, 1f, 0.94f),
            pressedColor = new Color(0.72f, 0.82f, 0.75f),
            disabledColor = new Color(0.45f, 0.5f, 0.45f),
            colorMultiplier = 1f,
            fadeDuration = 0.12f
        };
        // Keep keyboard navigation inside the HUD; this panel is pointed at, not tabbed to.
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        button.onClick.AddListener(action);
        Label("Label", rect, label, 24, Color.white, font, 0f, 0f, 200f, 64f);
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
}
