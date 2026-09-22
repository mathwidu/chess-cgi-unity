using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed partial class GameHud
{
    private sealed class MenuChoice
    {
        public UnityEngine.UI.Button Button;
        public UnityEngine.UI.Text Title;
        public UnityEngine.UI.Image Marker;
        public GameObject FocusRing;
        public MenuSurface Surface;
    }

    private sealed class MenuTextSize
    {
        public UnityEngine.UI.Text Label;
        public int FontSize;
        public float Height;
        public float MinimumPixels;
    }

    private RectTransform menuContent;
    private RectTransform computerOptions;
    private RectTransform localOptions;
    private UnityEngine.UI.Text menuSummary;
    private readonly List<MenuChoice> modeChoices = new List<MenuChoice>();
    private readonly List<MenuChoice> sideChoices = new List<MenuChoice>();
    private readonly List<MenuChoice> difficultyChoices = new List<MenuChoice>();
    private GameObject focusBeforeHelp;
    private MenuCastPreview menuCast;
    private CanvasGroup menuControls;
    private CanvasGroup matchControls;
    private UnityEngine.UI.Button menuPlayButton;
    private UnityEngine.UI.Button menuHelpButton;
    private RectTransform focusScope;
    private UnityEngine.UI.Text castName;
    private UnityEngine.UI.Text castRole;
    private readonly List<MenuTextSize> menuTypography = new List<MenuTextSize>();
    private Canvas menuCanvas;
    private RectTransform castRect;
    private UnityEngine.UI.Text otherCastName;
    private RectTransform whiteShadow;
    private RectTransform blackShadow;
    private RectTransform whiteCaptionShade;
    private RectTransform blackCaptionShade;
    private GameObject playFocusRing;
    private const float MenuWidth = 1672f;
    private const float MenuHeight = 941f;

    private void BuildStartMenu()
    {
        modeChoices.Clear();
        sideChoices.Clear();
        difficultyChoices.Clear();
        menuTypography.Clear();
        menuCanvas = GetComponent<Canvas>();
        startOverlay = CreatePanel("StartOverlay", hudRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Vector2.zero, overlayColor);
        menuContent = CreateRect("MenuContent", startOverlay, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, new Vector2(MenuWidth, MenuHeight));
        menuControls = menuContent.gameObject.AddComponent<CanvasGroup>();
        var backdrop = CreateRawImage("MenuStudyBackground", menuContent, Vector2.zero, new Vector2(MenuWidth, MenuHeight), Color.white);
        backdrop.texture = Resources.Load<Texture2D>("UI/MenuStudyBackground");
        backdrop.raycastTarget = false;
        whiteShadow = GroundShadow("WhiteContactShadow");
        blackShadow = GroundShadow("BlackContactShadow");
        whiteCaptionShade = CaptionShade("WhiteCaptionShade");
        blackCaptionShade = CaptionShade("BlackCaptionShade");

        var castImage = CreateRawImage("MenuCast", menuContent, new Vector2(225, -194), new Vector2(645, 588), Color.white);
        castRect = castImage.rectTransform;
        castImage.raycastTarget = false;
        menuCast = castImage.gameObject.AddComponent<MenuCastPreview>();
        menuCast.Configure(castImage, chosenSide);

        var title = MenuLabel("StartTitle", menuContent, "Xadrez", 92, textColor, 205, 71, 332, 110, true);
        Font display = Resources.Load<Font>("UI/Lato-Black");
        if (display != null) title.font = display;
        MenuLabel("ProjectName", menuContent, "CGI", 42, accentColor, 534, 103, 94, 55, true);
        MenuLabel("CastCaption", menuContent, "A turma no tabuleiro.", 28, mutedTextColor, 207, 153, 610, 40);

        const float panelX = 965, panelWidth = 642, inset = 42, innerWidth = 558;
        var logo = Resources.Load<Texture2D>("UI/FeevaleLogo");
        if (logo != null)
        {
            const float width = 327;
            // Original PNG: 950x369; visible artwork bounds (52,79)-(895,290).
            // Center the artwork, including its emblem, on the panel's axis.
            float artworkCenter = 473.5f / 950f;
            var logoImage = CreateRawImage("FeevaleSignature", menuContent,
                new Vector2(panelX + panelWidth * 0.5f - width * artworkCenter, -31),
                new Vector2(width, width * logo.height / logo.width), Color.white);
            logoImage.texture = logo;
            logoImage.raycastTarget = false;
        }

        castName = MenuLabel("CastName", menuContent, "", 23, textColor, 0, 0, 330, 32, true);
        castRole = MenuLabel("CastRole", menuContent, "", 18, mutedTextColor, 0, 0, 330, 28);
        otherCastName = MenuLabel("OtherCastName", menuContent, "", 20, textColor, 0, 0, 300, 30);
        castName.alignment = castRole.alignment = otherCastName.alignment = TextAnchor.UpperCenter;
        foreach (var label in new[] {castName, castRole, otherCastName})
        {
            var shadow = label.gameObject.AddComponent<UnityEngine.UI.Shadow>();
            shadow.effectColor = new Color(0.015f, 0.025f, 0.018f, 0.85f);
            shadow.effectDistance = new Vector2(1, -2);
        }
        MenuRule(menuContent, 73, 885, 1367);
        MenuLabel("ProjectCredit", menuContent, "Projeto de Computação Gráfica I", 16, mutedTextColor, 73, 896, 760, 26);

        RectTransform controls = MenuRect("StartCard", menuContent, panelX, 172, panelWidth, 684);
        Surface(controls, new Color32(10, 48, 36, 246), new Color32(43, 86, 66, 255), 12, 2);
        MenuLabel("MenuTitle", controls, "Nova partida", 50, textColor, inset, 46, innerWidth, 72, true);
        MenuLabel("ModeHeading", controls, "Modo de jogo", 22, mutedTextColor, inset, 119, innerWidth, 30);
        RectTransform modes = ChoiceRow("ModeOptions", controls, inset, 157, innerWidth, 64);
        modeChoices.Add(CreateMenuChoice("ComputerModeButton", modes, "Contra IA", 278, 64, () => ChooseMode(true)));
        modeChoices.Add(CreateMenuChoice("LocalModeButton", modes, "Dois jogadores", 278, 64, () => ChooseMode(false)));

        computerOptions = MenuRect("ComputerOptions", controls, inset, 247, innerWidth, 228);
        MenuLabel("SideHeading", computerOptions, "Você joga com", 22, mutedTextColor, 0, 0, innerWidth, 30);
        RectTransform sides = ChoiceRow("SideOptions", computerOptions, 0, 37, innerWidth, 64);
        sideChoices.Add(CreateMenuChoice("WhiteSideButton", sides, "Brancas", 278, 64, () => ChooseSide(ChessSide.White)));
        sideChoices.Add(CreateMenuChoice("BlackSideButton", sides, "Pretas", 278, 64, () => ChooseSide(ChessSide.Black)));
        MenuLabel("DifficultyHeading", computerOptions, "Dificuldade da IA", 22, mutedTextColor, 0, 128, innerWidth, 30);
        RectTransform levels = ChoiceRow("DifficultyOptions", computerOptions, 0, 164, innerWidth, 64);
        difficultyChoices.Add(CreateMenuChoice("BeginnerDifficultyButton", levels, "Iniciante", 166, 64, () => ChooseDifficulty(ComputerDifficulty.Beginner)));
        difficultyChoices.Add(CreateMenuChoice("IntermediateDifficultyButton", levels, "Intermediário", 228, 64, () => ChooseDifficulty(ComputerDifficulty.Intermediate)));
        difficultyChoices.Add(CreateMenuChoice("HardDifficultyButton", levels, "Difícil", 160, 64, () => ChooseDifficulty(ComputerDifficulty.Hard)));

        localOptions = MenuRect("LocalOptions", controls, inset, 270, innerWidth, 205);
        MenuLabel("LocalTitle", localOptions, "O tabuleiro é dos dois.", 30, textColor, 0, 0, innerWidth, 48, true);
        MenuLabel("LocalDescription", localOptions, "As brancas começam. Revezem os lances; a câmera acompanha cada turno.", 24, mutedTextColor, 0, 62, innerWidth, 120);
        menuSummary = MenuLabel("MatchSummary", controls, "", 18, mutedTextColor, inset, 481, innerWidth, 48);
        RectTransform buttonDepth = MenuRect("PlayButtonDepth", controls, inset, 540, innerWidth, 68);
        Surface(buttonDepth, new Color32(175, 145, 0, 255), Color.clear, 10, 0);
        menuPlayButton = MenuButton("StartPlayButton", controls, "Jogar", inset, 535, innerWidth, 70, actionColor, StartGame);
        var playSurface = StyleMenuButton(menuPlayButton, actionColor, new Color32(255, 231, 77, 255), 10, 2);
        playSurface.BottomShade = 0.98f;
        var playFocus = MenuRect("FocusRing", menuPlayButton.transform, -4, -4, innerWidth + 8, 78);
        Surface(playFocus, Color.clear, mutedTextColor, 13, 2);
        playFocusRing = playFocus.gameObject;
        var playLabel = menuPlayButton.GetComponentInChildren<UnityEngine.UI.Text>();
        playLabel.fontSize = 38;
        if (display != null) playLabel.font = display;
        menuHelpButton = MenuButton("StartHowToPlayButton", controls, "Como jogar", inset, 620, innerWidth, 48, Color.clear, ToggleHowToPlay);
        menuHelpButton.GetComponentInChildren<UnityEngine.UI.Text>().fontSize = 22;
        ConfigureMenuNavigation();
        foreach (var label in menuContent.GetComponentsInChildren<UnityEngine.UI.Text>(true))
        {
            bool buttonLabel = label.transform.parent.GetComponent<UnityEngine.UI.Button>() != null;
            menuTypography.Add(new MenuTextSize { Label = label, FontSize = label.fontSize,
                Height = label.rectTransform.rect.height,
                MinimumPixels = buttonLabel ? 16 : label.fontSize >= 28 ? 18 : label.fontSize <= 16 ? 12 : 14 });
        }
    }

    private MenuSurface Surface(RectTransform rect, Color fill, Color border, float radius, float borderWidth)
    {
        var surface = rect.gameObject.AddComponent<MenuSurface>();
        surface.color = fill;
        surface.BorderColor = border;
        surface.BorderWidth = borderWidth;
        surface.Radius = radius;
        surface.raycastTarget = false;
        return surface;
    }

    private MenuSurface StyleMenuButton(UnityEngine.UI.Button button, Color fill, Color border, float radius, float borderWidth)
    {
        var image = button.GetComponent<UnityEngine.UI.Image>();
        // Keep the existing root Image as the hit target for desktop and XR rays.
        // uGUI allows only one Graphic per GameObject, so the chrome is a child.
        image.color = Color.clear;
        var chrome = CreateRect("ButtonSurface", button.transform, Vector2.zero, Vector2.one,
            Vector2.one * 0.5f, Vector2.zero, Vector2.zero);
        chrome.SetAsFirstSibling();
        var surface = Surface(chrome, fill, border, radius, borderWidth);
        button.targetGraphic = surface;
        button.colors = new UnityEngine.UI.ColorBlock {
            normalColor = Color.white, selectedColor = Color.white,
            highlightedColor = new Color(0.91f, 1f, 0.94f), pressedColor = new Color(0.72f, 0.82f, 0.75f),
            disabledColor = new Color(0.45f, 0.5f, 0.45f), colorMultiplier = 1, fadeDuration = 0.12f
        };
        return surface;
    }

    private RectTransform GroundShadow(string name)
    {
        RectTransform rect = MenuRect(name, menuContent, 0, 0, 240, 55);
        var shadow = rect.gameObject.AddComponent<MenuGroundShadow>();
        shadow.color = new Color(0.015f, 0.025f, 0.017f, 0.68f);
        shadow.raycastTarget = false;
        return rect;
    }

    private RectTransform CaptionShade(string name)
    {
        RectTransform rect = GroundShadow(name);
        rect.sizeDelta = new Vector2(540, 124);
        var shade = rect.GetComponent<MenuGroundShadow>();
        shade.SolidCore = 0.75f;
        shade.color = new Color(0.01f, 0.025f, 0.018f, 0.8f);
        return rect;
    }

    private void LateUpdate()
    {
        if (menuCast == null || !showStartScreen) return;
        PositionCastLabels();
    }

    private void PositionCastLabels()
    {
        Vector2 whiteBase = CastBase(ChessSide.White);
        Vector2 blackBase = CastBase(ChessSide.Black);
        PositionUnderBase(whiteShadow, whiteBase, -23);
        PositionUnderBase(blackShadow, blackBase, -23);
        PositionUnderBase(whiteCaptionShade, whiteBase, 0);
        PositionUnderBase(blackCaptionShade, blackBase, 0);
        bool showWhite = chooseComputer && chosenSide == ChessSide.White;
        PositionUnderBase(castName.rectTransform, showWhite ? whiteBase : blackBase, 22);
        PositionUnderBase(castRole.rectTransform, showWhite ? whiteBase : blackBase, 55);
        PositionUnderBase(otherCastName.rectTransform, showWhite ? blackBase : whiteBase, 22);
    }

    private Vector2 CastBase(ChessSide side)
    {
        Vector2 point = menuCast.BaseViewport(side);
        return new Vector2(castRect.anchoredPosition.x + point.x * castRect.rect.width,
            -castRect.anchoredPosition.y + (1f - point.y) * castRect.rect.height);
    }

    private static void PositionUnderBase(RectTransform rect, Vector2 point, float offset)
    {
        rect.anchoredPosition = new Vector2(point.x - rect.rect.width * 0.5f, -point.y - offset);
    }

    private void ConfigureMenuNavigation()
    {
        var firstSide = sideChoices[0].Button;
        var lastLevel = difficultyChoices[2].Button;
        MenuNavigation(modeChoices[0].Button, null, modeChoices[1].Button, null, chooseComputer ? firstSide : menuPlayButton);
        MenuNavigation(modeChoices[1].Button, modeChoices[0].Button, chooseComputer ? firstSide : menuPlayButton, null, chooseComputer ? firstSide : menuPlayButton);
        MenuNavigation(firstSide, modeChoices[1].Button, sideChoices[1].Button, modeChoices[0].Button, difficultyChoices[0].Button);
        MenuNavigation(sideChoices[1].Button, firstSide, difficultyChoices[0].Button, modeChoices[1].Button, difficultyChoices[0].Button);
        for (int i = 0; i < difficultyChoices.Count; i++)
            MenuNavigation(difficultyChoices[i].Button, i == 0 ? sideChoices[1].Button : difficultyChoices[i-1].Button,
                i == 2 ? menuPlayButton : difficultyChoices[i+1].Button, firstSide, menuPlayButton);
        MenuNavigation(menuPlayButton, chooseComputer ? lastLevel : modeChoices[1].Button, null, chooseComputer ? lastLevel : modeChoices[1].Button, menuHelpButton);
        MenuNavigation(menuHelpButton, menuPlayButton, null, menuPlayButton, null);
    }

    private static void MenuNavigation(UnityEngine.UI.Button button, UnityEngine.UI.Selectable left,
        UnityEngine.UI.Selectable right, UnityEngine.UI.Selectable up, UnityEngine.UI.Selectable down)
    {
        button.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.Explicit,
            selectOnLeft = left, selectOnRight = right, selectOnUp = up, selectOnDown = down };
    }

    private void RefreshNavigationFocus()
    {
        EventSystem events = EventSystem.current;
        if (events == null) return;
        RectTransform scope = showHowToPlay ? howToPlayPanel :
            computerErrorPanel.gameObject.activeSelf ? computerErrorPanel :
            promotionPanel.gameObject.activeSelf ? promotionPanel :
            showStartScreen ? menuContent : matchInterface;
        GameObject current = events.currentSelectedGameObject;
        var currentButton = current != null ? current.GetComponent<UnityEngine.UI.Button>() : null;
        if (focusScope == scope && currentButton != null && current.activeInHierarchy &&
            current.transform.IsChildOf(scope) && currentButton.IsInteractable()) return;

        focusScope = scope;
        foreach (var button in scope.GetComponentsInChildren<UnityEngine.UI.Button>())
        {
            if (!button.IsInteractable()) continue;
            events.firstSelectedGameObject = button.gameObject;
            events.SetSelectedGameObject(button.gameObject);
            return;
        }
    }

    private void ChooseMode(bool computer) { chooseComputer = computer; ConfigureMenuNavigation(); RefreshInterface(); }
    private void ChooseSide(ChessSide side) { chosenSide = side; RefreshInterface(); }
    private void ChooseDifficulty(ComputerDifficulty difficulty) { chosenDifficulty = difficulty; RefreshInterface(); }

    private void RefreshStartMenu()
    {
        if (menuContent == null) return;
        menuControls.interactable = !showHowToPlay;
        if (matchControls != null)
            matchControls.interactable = !showHowToPlay && (gameController == null || (!gameController.IsAwaitingPromotion && !gameController.HasComputerError));
        // Fit the same logical surface in the desktop Game view and the VR world-space Canvas.
        Vector2 available = startOverlay.rect.size;
        float scale = Mathf.Min((available.x - 2f) / MenuWidth, (available.y - 2f) / MenuHeight);
        menuContent.localScale = Vector3.one * Mathf.Max(0.1f, scale);
        float effectiveScale = menuContent.localScale.x * (menuCanvas.renderMode == RenderMode.WorldSpace ? 1f : menuCanvas.scaleFactor);
        foreach (MenuTextSize type in menuTypography)
        {
            int fontSize = Mathf.Max(type.FontSize, Mathf.CeilToInt(type.MinimumPixels / Mathf.Max(0.4f, effectiveScale)));
            if (type.Label.fontSize != fontSize) type.Label.fontSize = fontSize;
            float height = Mathf.Max(type.Height, fontSize * 1.25f);
            if (!Mathf.Approximately(type.Label.rectTransform.rect.height, height))
                type.Label.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        }
        for (int i = 0; i < modeChoices.Count; i++) SetChoice(modeChoices[i], i == (chooseComputer ? 0 : 1));
        for (int i = 0; i < sideChoices.Count; i++) SetChoice(sideChoices[i], i == (chosenSide == ChessSide.White ? 0 : 1));
        for (int i = 0; i < difficultyChoices.Count; i++) SetChoice(difficultyChoices[i], i == (int)chosenDifficulty);
        playFocusRing.SetActive(EventSystem.current != null && EventSystem.current.currentSelectedGameObject == menuPlayButton.gameObject);
        SetActive(computerOptions, chooseComputer);
        SetActive(localOptions, !chooseComputer);
        if (menuCast != null) menuCast.SetSide(chosenSide, !chooseComputer);
        castName.text = chosenSide == ChessSide.White && chooseComputer ? "Professora Marta" : "Professor Ricardo";
        otherCastName.text = chosenSide == ChessSide.White && chooseComputer ? "Professor Ricardo" : "Professora Marta";
        castRole.text = !chooseComputer ? "Duas pessoas. Uma partida." : chosenSide == ChessSide.White ? "Rainha · Você joga de brancas" : "Rei · Você joga de pretas";
        menuSummary.text = !chooseComputer ? "Uma partida local para jogar a dois." :
            chosenDifficulty == ComputerDifficulty.Beginner ? "Para praticar os primeiros movimentos." :
            chosenDifficulty == ComputerDifficulty.Intermediate ? "Mais atenção a cada jogada." :
            "Um desafio maior, com menos erros do adversário.";
        PositionCastLabels();
    }

    private MenuChoice CreateMenuChoice(string name, Transform parent, string title, float width, float height, UnityEngine.Events.UnityAction action)
    {
        UnityEngine.UI.Button button = MenuButton(name, parent, "", 0, 0, width, height, Color.clear, action);
        var layout = button.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
        layout.preferredWidth = width;
        layout.preferredHeight = height;
        var surface = StyleMenuButton(button, new Color32(19, 57, 43, 255), new Color32(49, 86, 69, 255), 7, 1.5f);
        var titleLabel = MenuLabel("Title", button.transform, title, 22, textColor, 8, 6, width - 16, height - 12, true);
        titleLabel.alignment = TextAnchor.MiddleCenter;
        var marker = MenuRect("SelectedMarker", button.transform, 18, (height - 30) * 0.5f, 30, 30).gameObject.AddComponent<UnityEngine.UI.Image>();
        marker.sprite = Resources.Load<Sprite>("UI/SelectionCheck");
        marker.color = accentColor;
        marker.raycastTarget = false;
        RectTransform focus = MenuRect("FocusRing", button.transform, -4, -4, width + 8, height + 8);
        Surface(focus, Color.clear, mutedTextColor, 10, 2);
        return new MenuChoice { Button = button, Title = titleLabel, Marker = marker, Surface = surface, FocusRing = focus.gameObject };
    }

    private void SetChoice(MenuChoice choice, bool selected)
    {
        choice.Marker.enabled = selected;
        choice.FocusRing.SetActive(EventSystem.current != null && EventSystem.current.currentSelectedGameObject == choice.Button.gameObject);
        choice.Title.color = selected ? textColor : mutedTextColor;
        choice.Title.font = selected ? GetHudBoldFont() : GetHudFont();
        RectTransform rect = (RectTransform)choice.Button.transform;
        float left = selected ? 51 : 8;
        choice.Title.rectTransform.anchoredPosition = new Vector2(left, -6);
        choice.Title.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Max(1, rect.rect.width - left - 8));
        choice.Surface.color = selected ? new Color32(23, 64, 47, 255) : new Color32(19, 57, 43, 255);
        choice.Surface.SetBorder(selected ? accentColor : new Color32(49, 86, 69, 255));
    }

    private RectTransform ChoiceRow(string name, Transform parent, float x, float y, float width, float height)
    {
        RectTransform row = MenuRect(name, parent, x, y, width, height);
        var layout = row.gameObject.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
        layout.spacing = 2;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;
        return row;
    }

    private RectTransform MenuRect(string name, Transform parent, float x, float y, float width, float height)
    {
        return CreateRect(name, parent, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(x, -y), new Vector2(width, height));
    }

    private UnityEngine.UI.Text MenuLabel(string name, Transform parent, string value, int size, Color color, float x, float y, float width, float height, bool bold = false)
    {
        return CreateText(name, parent, value, size, bold ? FontStyle.Bold : FontStyle.Normal, color, TextAnchor.UpperLeft, new Vector2(x, -y), new Vector2(width, height));
    }

    private UnityEngine.UI.Button MenuButton(string name, Transform parent, string label, float x, float y, float width, float height, Color color, UnityEngine.Events.UnityAction action)
    {
        var button = CreateButton(name, parent, label, Vector2.zero, new Vector2(width, height), color, action);
        var rect = (RectTransform)button.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(x, -y);
        return button;
    }

    private void MenuRule(Transform parent, float x, float y, float width)
    {
        var line = MenuRect("Divider", parent, x, y, width, 1).gameObject.AddComponent<UnityEngine.UI.Image>();
        line.color = new Color(0.7f, 0.86f, 0.74f, 0.28f);
        line.raycastTarget = false;
    }

    private void BuildHelpDialog()
    {
        howToPlayPanel = CreatePanel("HowToPlayPanel", hudRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Vector2.zero, new Color(0.01f, 0.02f, 0.025f, 0.92f));
        RectTransform card = CreatePanel("HelpCard", howToPlayPanel, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, new Vector2(860, 620), panelColor);
        MenuLabel("HelpTitle", card, "Como jogar", 42, textColor, 48, 55, 764, 56, true);
        MenuLabel("HelpText", card, "01   Escolha uma peça do seu lado.\n02   Selecione uma casa destacada para mover.\n03   Proteja seu rei e busque o xeque-mate.", 24, textColor, 48, 184, 764, 138);
        MenuRule(card, 48, 344, 764);
        MenuLabel("CameraHelp", card, "CÂMERA", 15, accentColor, 48, 376, 300, 28, true);
        MenuLabel("CameraShortcuts", card, "Q / E para girar\nScroll para aproximar", 22, mutedTextColor, 48, 417, 356, 68);
        MenuLabel("ActionHelp", card, "PARTIDA", 15, accentColor, 450, 376, 330, 28, true);
        MenuLabel("ActionShortcuts", card, "Esc cancela a seleção\nN inicia uma nova partida", 22, mutedTextColor, 450, 417, 364, 68);
        MenuButton("CloseHelpButton", card, "Entendi", 48, 524, 764, 58, actionColor, ToggleHowToPlay);
    }
}
