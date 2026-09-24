using UnityEngine;
using UnityEngine.UI;

// A quiet cue for whose turn it is: a thin light sweeps once along the board rim of the side
// to move, and a short label on the table names the turn, then fades. It lives in board-local
// units, so it rides with the board on the table in both modes and never covers a square.
public sealed class TurnIndicatorView : MonoBehaviour
{
    private const float SweepSeconds = 0.35f;
    private const float LabelFadeInSeconds = 0.25f;
    private const float LabelHoldSeconds = 2.5f;
    private const float LabelFadeOutSeconds = 0.6f;
    private const float StripHeight = 0.02f;
    // The desktop HUD already names the turn, so the light is only a thin accent there.
    private const float VrStripDepth = 0.12f;
    private const float DesktopStripDepth = 0.05f;
    // BoardView's rim tops out at 0.025 and reaches 0.425 beyond the squares; its base
    // bottom, where the table surface is, sits at -0.14.
    private const float RimTop = 0.025f;
    private const float RimWidth = 0.425f;
    private const float TableSurface = -0.14f;

    private static readonly Color AccentColor = new Color32(255, 221, 0, 255);
    private static readonly Color MutedColor = new Color32(202, 228, 211, 255);

    private ChessGameController game;
    private CameraController cameraController;
    private Transform strip;
    private Material stripMaterial;
    private RectTransform label;
    private CanvasGroup labelGroup;
    private Text labelText;
    private float stripLength;
    private float stripDepth;
    private float stripOffset;
    private float labelOffset;
    private string shownState;
    private float changedAt;
    private bool labelStays;

    public bool IsShown => strip != null && strip.gameObject.activeSelf;
    public ChessSide LitSide { get; private set; }
    public bool LitForPlayer { get; private set; }
    public string LabelText => labelText != null ? labelText.text : string.Empty;
    public float LabelAlpha => labelGroup != null ? labelGroup.alpha : 0f;

    public void Build(float squareSize)
    {
        float halfBoard = squareSize * 4f;
        stripLength = squareSize * 4.8f;
        stripDepth = XRRig.IsHeadsetPresent ? VrStripDepth : DesktopStripDepth;
        stripOffset = halfBoard + RimWidth * 0.5f;
        labelOffset = halfBoard + RimWidth + 0.55f;

        strip = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
        strip.name = "TurnLight";
        strip.SetParent(transform, false);
        DestroyUnityObject(strip.GetComponent<Collider>());
        // An unlit copy of the bundled ray material: stays legible under any light and is
        // kept in player builds, unlike a shader looked up by name.
        Material template = Resources.Load<Material>("XR/ControllerRayMaterial");
        stripMaterial = template != null ? new Material(template) : new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        MeshRenderer renderer = strip.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = stripMaterial;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        BuildLabel();
        Hide();
    }

    private void BuildLabel()
    {
        var canvasObject = new GameObject("TurnLabel", typeof(RectTransform));
        label = (RectTransform)canvasObject.transform;
        label.SetParent(transform, false);
        label.sizeDelta = new Vector2(480f, 90f);
        label.localScale = Vector3.one * 0.005f;
        // No raycaster: the label is read, never pointed at, so it cannot catch the VR ray.
        canvasObject.AddComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        labelGroup = canvasObject.AddComponent<CanvasGroup>();
        labelGroup.interactable = false;
        labelGroup.blocksRaycasts = false;

        var textRect = new GameObject("Text", typeof(RectTransform)).GetComponent<RectTransform>();
        textRect.SetParent(label, false);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        labelText = textRect.gameObject.AddComponent<Text>();
        Font font = Resources.Load<Font>("UI/Lato-Bold");
        labelText.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        labelText.fontSize = 56;
        labelText.alignment = TextAnchor.MiddleCenter;
        labelText.horizontalOverflow = HorizontalWrapMode.Overflow;
        labelText.raycastTarget = false;
    }

    private void Update()
    {
        if (strip == null)
        {
            return;
        }

        if (game == null)
        {
            game = Object.FindFirstObjectByType<ChessGameController>();
            cameraController = Object.FindFirstObjectByType<CameraController>();
            if (game == null)
            {
                return;
            }
        }

        if (!game.isActiveAndEnabled || game.IsMenuOpen || game.IsGameOver)
        {
            Hide();
            return;
        }

        ChessSide side = game.CurrentTurn;
        bool computerToMove = game.IsAgainstComputer && side != game.HumanSide;
        string text = !game.IsAgainstComputer ? (side == ChessSide.White ? "Vez das brancas" : "Vez das pretas")
            : !computerToMove ? "Sua vez"
            : game.HasComputerError ? string.Empty : "IA pensando...";
        string state = side + "|" + text;
        if (state != shownState)
        {
            Show(side, !computerToMove, text, computerToMove);
            shownState = state;
            changedAt = Time.unscaledTime;
        }

        OrientLabel();
        Animate(Time.unscaledTime - changedAt);
    }

    private void Show(ChessSide side, bool forPlayer, string text, bool labelStaysWhileThinking)
    {
        LitSide = side;
        LitForPlayer = forPlayer;
        labelStays = labelStaysWhileThinking;
        float direction = side == ChessSide.White ? -1f : 1f;
        Color color = forPlayer ? AccentColor : MutedColor;
        strip.localPosition = new Vector3(0f, RimTop + StripHeight * 0.5f, direction * stripOffset);
        stripMaterial.color = color;
        label.localPosition = new Vector3(0f, TableSurface + 0.02f, direction * labelOffset);
        labelText.text = text;
        labelText.color = color;
        strip.gameObject.SetActive(true);
        label.gameObject.SetActive(text.Length > 0);
    }

    private void Animate(float elapsed)
    {
        // One sweep from the middle of the edge outward; no looping animation during play.
        float sweep = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / SweepSeconds));
        strip.localScale = new Vector3(Mathf.Max(0.001f, stripLength * sweep), StripHeight, stripDepth);

        float fadeIn = Mathf.Clamp01(elapsed / LabelFadeInSeconds);
        float fadeOut = labelStays ? 0f : Mathf.Clamp01((elapsed - LabelFadeInSeconds - LabelHoldSeconds) / LabelFadeOutSeconds);
        labelGroup.alpha = fadeIn * (1f - fadeOut);
    }

    // Lying on the table, the label reads upright from wherever the player sits.
    private void OrientLabel()
    {
        bool viewerOnBlackSide = XRRig.IsHeadsetPresent
            ? XRRig.SeatedAsBlack
            : cameraController != null && cameraController.CurrentPerspective == ChessSide.Black;
        label.localRotation = Quaternion.Euler(0f, viewerOnBlackSide ? 180f : 0f, 0f) * Quaternion.Euler(90f, 0f, 0f);
    }

    private void Hide()
    {
        shownState = null;
        strip.gameObject.SetActive(false);
        label.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        DestroyUnityObject(stripMaterial);
    }

    private static void DestroyUnityObject(Object target)
    {
        if (target == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Object.Destroy(target);
        }
        else
        {
            Object.DestroyImmediate(target);
        }
    }
}
