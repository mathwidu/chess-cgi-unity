using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public sealed class GameHud : MonoBehaviour
{
    [SerializeField] private ChessGameController gameController;
    [SerializeField] private int visibleMoveCount = 6;

    private readonly Color panelColor = new Color(0.075f, 0.07f, 0.06f, 0.88f);
    private readonly Color panelStrongColor = new Color(0.055f, 0.052f, 0.048f, 0.94f);
    private readonly Color overlayColor = new Color(0.02f, 0.018f, 0.016f, 0.66f);
    private readonly Color textColor = new Color(0.94f, 0.91f, 0.84f, 1f);
    private readonly Color mutedTextColor = new Color(0.72f, 0.72f, 0.66f, 1f);
    private readonly Color accentColor = new Color(0.95f, 0.73f, 0.38f, 1f);
    private readonly Color actionColor = new Color(0.2f, 0.32f, 0.38f, 0.96f);
    private readonly Color actionHoverColor = new Color(0.27f, 0.42f, 0.49f, 1f);
    private readonly Color neutralButtonColor = new Color(0.27f, 0.25f, 0.22f, 0.96f);

    private bool showStartScreen = true;
    private bool showHowToPlay;
    private Font hudFont;
    private RectTransform hudRoot;
    private RectTransform startOverlay;
    private RectTransform howToPlayPanel;
    private RectTransform startHowToPlayText;
    private RectTransform promotionPanel;
    private Text turnText;
    private Text statusText;
    private Text moveHistoryText;
    private Text howToPlayButtonText;
    private Text startHowToPlayButtonText;

    public void Configure(ChessGameController controller)
    {
        gameController = controller;
        RefreshInterface();
    }

    private void Awake()
    {
        if (gameController == null)
        {
            gameController = Object.FindFirstObjectByType<ChessGameController>();
        }

        RebuildInterface();
    }

    private void Update()
    {
        RefreshInterface();
    }

    public void RebuildInterface()
    {
        EnsureCanvasInfrastructure();
        ClearExistingRoot();

        hudRoot = CreateRect("HudRoot", transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Vector2.zero);

        RectTransform topBar = CreateRect("TopBar", hudRoot, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(0f, 88f));
        RectTransform brandPanel = CreatePanel("BrandPanel", topBar, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -2f), new Vector2(320f, 74f), panelColor);
        CreateText("TitleText", brandPanel, "Xadrez CGI", 25, FontStyle.Bold, textColor, TextAnchor.UpperLeft, new Vector2(14f, -10f), new Vector2(292f, 34f));
        CreateText("SubtitleText", brandPanel, "Computacao Grafica I", 12, FontStyle.Normal, mutedTextColor, TextAnchor.UpperLeft, new Vector2(14f, -45f), new Vector2(292f, 18f));

        RectTransform turnPanel = CreatePanel("TurnPanel", topBar, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-16f, -2f), new Vector2(304f, 74f), panelColor);
        turnText = CreateText("TurnText", turnPanel, "Brancas jogam", 17, FontStyle.Bold, accentColor, TextAnchor.UpperRight, new Vector2(14f, -10f), new Vector2(276f, 24f));
        statusText = CreateText("StatusText", turnPanel, "Escolha uma peca para mover.", 13, FontStyle.Normal, textColor, TextAnchor.UpperRight, new Vector2(14f, -38f), new Vector2(276f, 26f));

        RectTransform historyPanel = CreatePanel("MoveHistoryPanel", hudRoot, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-16f, -104f), new Vector2(304f, 238f), panelColor);
        CreateText("MoveHistoryTitle", historyPanel, "Historico", 17, FontStyle.Bold, textColor, TextAnchor.UpperLeft, new Vector2(14f, -12f), new Vector2(276f, 24f));
        moveHistoryText = CreateText("MoveHistoryText", historyPanel, "Nenhuma jogada ainda.", 13, FontStyle.Normal, mutedTextColor, TextAnchor.UpperLeft, new Vector2(14f, -42f), new Vector2(276f, 178f));

        RectTransform actionBar = CreatePanel("ActionBar", hudRoot, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(16f, 16f), new Vector2(410f, 58f), panelColor);
        CreateButton("NewGameButton", actionBar, "Nova partida", new Vector2(14f, 12f), new Vector2(122f, 34f), actionColor, StartGame);
        CreateButton("CancelButton", actionBar, "Cancelar", new Vector2(144f, 12f), new Vector2(108f, 34f), neutralButtonColor, CancelSelection);
        howToPlayButtonText = CreateButton("HowToPlayButton", actionBar, "Como jogar", new Vector2(260f, 12f), new Vector2(124f, 34f), neutralButtonColor, ToggleHowToPlay).GetComponentInChildren<Text>();

        howToPlayPanel = CreatePanel("HowToPlayPanel", hudRoot, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -104f), new Vector2(380f, 210f), panelStrongColor);
        CreateText("HowToPlayTitle", howToPlayPanel, "Como jogar", 18, FontStyle.Bold, textColor, TextAnchor.UpperLeft, new Vector2(16f, -14f), new Vector2(348f, 26f));
        CreateText("HowToPlayText", howToPlayPanel, BuildHowToPlayText(), 13, FontStyle.Normal, textColor, TextAnchor.UpperLeft, new Vector2(16f, -48f), new Vector2(348f, 142f));

        promotionPanel = CreatePanel("PromotionPanel", hudRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(500f, 150f), panelStrongColor);
        CreateText("PromotionTitle", promotionPanel, "Promocao", 22, FontStyle.Bold, textColor, TextAnchor.UpperCenter, new Vector2(18f, -14f), new Vector2(464f, 30f));
        CreateText("PromotionHelp", promotionPanel, "Escolha a nova peca do peao.", 13, FontStyle.Normal, mutedTextColor, TextAnchor.UpperCenter, new Vector2(18f, -48f), new Vector2(464f, 22f));
        CreateButton("PromoteQueenButton", promotionPanel, "Rainha", new Vector2(22f, 92f), new Vector2(104f, 34f), actionColor, () => ChoosePromotion('Q'));
        CreateButton("PromoteRookButton", promotionPanel, "Torre", new Vector2(142f, 92f), new Vector2(94f, 34f), neutralButtonColor, () => ChoosePromotion('R'));
        CreateButton("PromoteBishopButton", promotionPanel, "Bispo", new Vector2(252f, 92f), new Vector2(94f, 34f), neutralButtonColor, () => ChoosePromotion('B'));
        CreateButton("PromoteKnightButton", promotionPanel, "Cavalo", new Vector2(362f, 92f), new Vector2(104f, 34f), neutralButtonColor, () => ChoosePromotion('N'));

        startOverlay = CreatePanel("StartOverlay", hudRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Vector2.zero, overlayColor);
        RectTransform startCard = CreatePanel("StartCard", startOverlay, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560f, 390f), panelStrongColor);
        CreateText("StartTitle", startCard, "Xadrez CGI", 34, FontStyle.Bold, textColor, TextAnchor.UpperCenter, new Vector2(24f, -28f), new Vector2(512f, 48f));
        CreateText("StartSubtitle", startCard, "Xadrez 3D local com personagens da turma.", 15, FontStyle.Normal, mutedTextColor, TextAnchor.UpperCenter, new Vector2(42f, -82f), new Vector2(476f, 28f));
        CreateButton("StartPlayButton", startCard, "Jogar", new Vector2(160f, 128f), new Vector2(240f, 42f), actionColor, StartGame);
        startHowToPlayButtonText = CreateButton("StartHowToPlayButton", startCard, "Como jogar", new Vector2(180f, 182f), new Vector2(200f, 36f), neutralButtonColor, ToggleHowToPlay).GetComponentInChildren<Text>();
        startHowToPlayText = CreateText("StartHowToPlayText", startCard, BuildHowToPlayText(), 13, FontStyle.Normal, textColor, TextAnchor.UpperLeft, new Vector2(64f, -236f), new Vector2(432f, 118f)).rectTransform;

        RefreshInterface();
    }

    public void RefreshInterface()
    {
        if (hudRoot == null)
        {
            return;
        }

        bool hasController = gameController != null;
        bool awaitingPromotion = hasController && gameController.IsAwaitingPromotion;

        SetActive(startOverlay, showStartScreen);
        SetActive(howToPlayPanel, showHowToPlay && !showStartScreen);
        SetActive(startHowToPlayText, showHowToPlay && showStartScreen);
        SetActive(promotionPanel, awaitingPromotion);

        if (turnText != null)
        {
            turnText.text = !hasController || gameController.CurrentTurn == ChessSide.White ? "Brancas jogam" : "Pretas jogam";
        }

        if (statusText != null)
        {
            string status = hasController ? CompactStatus(gameController.StatusMessage) : "Escolha uma peca para mover.";
            statusText.text = status;
            statusText.color = status.StartsWith("Movimento invalido") ? new Color(0.95f, 0.45f, 0.36f, 1f) : textColor;
        }

        if (moveHistoryText != null)
        {
            moveHistoryText.text = hasController ? FormatMoveHistory(gameController.MoveHistory) : "Nenhuma jogada ainda.";
        }

        if (howToPlayButtonText != null)
        {
            howToPlayButtonText.text = showHowToPlay && !showStartScreen ? "Ocultar" : "Como jogar";
        }

        if (startHowToPlayButtonText != null)
        {
            startHowToPlayButtonText.text = showHowToPlay ? "Ocultar como jogar" : "Como jogar";
        }
    }

    private void StartGame()
    {
        showStartScreen = false;
        showHowToPlay = false;
        if (gameController != null)
        {
            gameController.StartLocalGame();
        }

        RefreshInterface();
    }

    private void CancelSelection()
    {
        if (gameController != null)
        {
            gameController.CancelSelection();
        }

        RefreshInterface();
    }

    private void ToggleHowToPlay()
    {
        showHowToPlay = !showHowToPlay;
        RefreshInterface();
    }

    private void ChoosePromotion(char piece)
    {
        if (gameController != null)
        {
            gameController.ChoosePromotion(piece);
        }

        RefreshInterface();
    }

    private void EnsureCanvasInfrastructure()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
        }

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        CanvasScaler scaler = GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = gameObject.AddComponent<CanvasScaler>();
        }

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        if (GetComponent<GraphicRaycaster>() == null)
        {
            gameObject.AddComponent<GraphicRaycaster>();
        }

        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.transform.SetParent(transform);
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<InputSystemUIInputModule>();
        }
    }

    private void ClearExistingRoot()
    {
        Transform existing = transform.Find("HudRoot");
        if (existing != null)
        {
            DestroyObject(existing.gameObject);
        }
    }

    private RectTransform CreatePanel(
        string name,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPosition,
        Vector2 sizeDelta,
        Color color)
    {
        RectTransform rect = CreateRect(name, parent, anchorMin, anchorMax, pivot, anchoredPosition, sizeDelta);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        return rect;
    }

    private RectTransform CreateRect(
        string name,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPosition,
        Vector2 sizeDelta)
    {
        GameObject gameObject = new GameObject(name);
        gameObject.transform.SetParent(parent, false);
        RectTransform rect = gameObject.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
        return rect;
    }

    private Text CreateText(
        string name,
        Transform parent,
        string text,
        int fontSize,
        FontStyle fontStyle,
        Color color,
        TextAnchor alignment,
        Vector2 anchoredPosition,
        Vector2 sizeDelta)
    {
        RectTransform rect = CreateRect(name, parent, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), anchoredPosition, sizeDelta);
        Text label = rect.gameObject.AddComponent<Text>();
        label.font = GetHudFont();
        label.fontSize = fontSize;
        label.fontStyle = fontStyle;
        label.color = color;
        label.alignment = alignment;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Truncate;
        label.text = text;
        return label;
    }

    private Button CreateButton(
        string name,
        Transform parent,
        string label,
        Vector2 anchoredPosition,
        Vector2 sizeDelta,
        Color normalColor,
        UnityEngine.Events.UnityAction action)
    {
        RectTransform rect = CreateRect(name, parent, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), anchoredPosition, sizeDelta);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = normalColor;

        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.colors = new ColorBlock
        {
            normalColor = normalColor,
            highlightedColor = normalColor == actionColor ? actionHoverColor : new Color(0.36f, 0.33f, 0.28f, 1f),
            pressedColor = new Color(0.14f, 0.22f, 0.26f, 1f),
            selectedColor = normalColor,
            disabledColor = new Color(0.16f, 0.15f, 0.14f, 0.65f),
            colorMultiplier = 1f,
            fadeDuration = 0.12f
        };
        button.onClick.AddListener(action);

        Text buttonText = CreateText("Label", rect, label, 13, FontStyle.Bold, textColor, TextAnchor.MiddleCenter, Vector2.zero, sizeDelta);
        buttonText.raycastTarget = false;
        return button;
    }

    private string FormatMoveHistory(IReadOnlyList<string> moveHistory)
    {
        if (moveHistory.Count == 0)
        {
            return "Nenhuma jogada ainda.";
        }

        StringBuilder builder = new StringBuilder();
        int startIndex = Mathf.Max(0, moveHistory.Count - visibleMoveCount);
        for (int i = startIndex; i < moveHistory.Count; i++)
        {
            if (builder.Length > 0)
            {
                builder.AppendLine();
            }

            builder.Append(i + 1);
            builder.Append(". ");
            builder.Append(moveHistory[i]);
        }

        return builder.ToString();
    }

    private static string CompactStatus(string status)
    {
        if (status.StartsWith("Turno:"))
        {
            return "Escolha uma peca para mover.";
        }

        return status;
    }

    private static string BuildHowToPlayText()
    {
        return "1. Clique em uma peca do turno atual.\n" +
            "2. Clique em uma casa destacada para mover.\n" +
            "3. Q/E giram a camera, scroll aproxima.\n" +
            "4. Esc cancela selecao, N reinicia.";
    }

    private Font GetHudFont()
    {
        if (hudFont != null)
        {
            return hudFont;
        }

        hudFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (hudFont == null)
        {
            hudFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        return hudFont;
    }

    private static void SetActive(Component component, bool active)
    {
        if (component != null)
        {
            component.gameObject.SetActive(active);
        }
    }

    private static void DestroyObject(Object target)
    {
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
