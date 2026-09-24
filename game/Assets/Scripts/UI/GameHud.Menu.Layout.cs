using UnityEngine;
using UnityEngine.UI;

// Construction and styling of the approved menu; runtime choices live in GameHud.Menu.cs.
public sealed partial class GameHud
{
    // Coordinates in the approved composition; scale this surface as a whole for each Canvas.
    private const float MenuWidth = 1672f;
    private const float MenuHeight = 941f;
    private const float MenuPanelX = 965f;
    private const float MenuPanelWidth = 642f;
    private const float MenuInset = 42f;
    private const float MenuInnerWidth = 558f;

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

        BuildMenuStage();
        Font display = Resources.Load<Font>("UI/Lato-Black");
        BuildMenuBranding(display);
        BuildCastCaptions();
        BuildMenuFooter();
        BuildMenuConfiguration(display);
        ConfigureMenuNavigation();
        RegisterMenuTypography();
    }

    private void BuildMenuStage()
    {
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
    }

    private void BuildMenuBranding(Font display)
    {
        var title = MenuLabel("StartTitle", menuContent, "Xadrez", 92, textColor, 205, 71, 332, 110, true);
        if (display != null)
        {
            title.font = display;
        }
        MenuLabel("ProjectName", menuContent, "CGI", 42, accentColor, 534, 103, 94, 55, true);
        MenuLabel("CastCaption", menuContent, "A turma no tabuleiro.", 28, mutedTextColor, 207, 153, 610, 40);

        var logo = Resources.Load<Texture2D>("UI/FeevaleLogo");
        if (logo != null)
        {
            const float width = 327;
            // Original PNG: 950x369; visible artwork bounds (52,79)-(895,290).
            // Center the artwork, including its emblem, on the panel's axis.
            float artworkCenter = 473.5f / 950f;
            var logoImage = CreateRawImage("FeevaleSignature", menuContent,
                new Vector2(MenuPanelX + MenuPanelWidth * 0.5f - width * artworkCenter, -31),
                new Vector2(width, width * logo.height / logo.width), Color.white);
            logoImage.texture = logo;
            logoImage.raycastTarget = false;
        }
    }

    private void BuildCastCaptions()
    {
        castName = MenuLabel("CastName", menuContent, "", 23, textColor, 0, 0, 330, 32, true);
        castRole = MenuLabel("CastRole", menuContent, "", 18, mutedTextColor, 0, 0, 330, 28);
        otherCastName = MenuLabel("OtherCastName", menuContent, "", 20, textColor, 0, 0, 300, 30);
        castName.alignment = castRole.alignment = otherCastName.alignment = TextAnchor.UpperCenter;
        foreach (var label in new[] { castName, castRole, otherCastName })
        {
            var shadow = label.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.015f, 0.025f, 0.018f, 0.85f);
            shadow.effectDistance = new Vector2(1, -2);
        }
    }

    private void BuildMenuFooter()
    {
        MenuRule(menuContent, 73, 885, 1367);
        MenuLabel("ProjectCredit", menuContent, "Projeto de Computação Gráfica I", 16, mutedTextColor, 73, 896, 760, 26);
    }

    private void BuildMenuConfiguration(Font display)
    {
        RectTransform controls = MenuRect("StartCard", menuContent, MenuPanelX, 172, MenuPanelWidth, 684);
        Surface(controls, new Color32(10, 48, 36, 246), new Color32(43, 86, 66, 255), 12, 2);
        MenuLabel("MenuTitle", controls, "Nova partida", 50, textColor, MenuInset, 46, MenuInnerWidth, 72, true);

        BuildGameModeOptions(controls);
        BuildComputerOptions(controls);
        BuildLocalOptions(controls);
        BuildMenuActions(controls, display);
    }

    private void BuildGameModeOptions(RectTransform controls)
    {
        MenuLabel("ModeHeading", controls, "Modo de jogo", 22, mutedTextColor, MenuInset, 119, MenuInnerWidth, 30);
        RectTransform modes = ChoiceRow("ModeOptions", controls, MenuInset, 157, MenuInnerWidth, 64);
        modeChoices.Add(CreateMenuChoice("ComputerModeButton", modes, "Contra IA", 278, 64, () => ChooseMode(true)));
        modeChoices.Add(CreateMenuChoice("LocalModeButton", modes, "Dois jogadores", 278, 64, () => ChooseMode(false)));
    }

    private void BuildComputerOptions(RectTransform controls)
    {
        computerOptions = MenuRect("ComputerOptions", controls, MenuInset, 247, MenuInnerWidth, 228);
        MenuLabel("SideHeading", computerOptions, "Você joga com", 22, mutedTextColor, 0, 0, MenuInnerWidth, 30);
        RectTransform sides = ChoiceRow("SideOptions", computerOptions, 0, 37, MenuInnerWidth, 64);
        sideChoices.Add(CreateMenuChoice("WhiteSideButton", sides, "Brancas", 278, 64, () => ChooseSide(ChessSide.White)));
        sideChoices.Add(CreateMenuChoice("BlackSideButton", sides, "Pretas", 278, 64, () => ChooseSide(ChessSide.Black)));
        MenuLabel("DifficultyHeading", computerOptions, "Dificuldade da IA", 22, mutedTextColor, 0, 128, MenuInnerWidth, 30);
        RectTransform levels = ChoiceRow("DifficultyOptions", computerOptions, 0, 164, MenuInnerWidth, 64);
        difficultyChoices.Add(CreateMenuChoice("BeginnerDifficultyButton", levels, "Iniciante", 166, 64, () => ChooseDifficulty(ComputerDifficulty.Beginner)));
        difficultyChoices.Add(CreateMenuChoice("IntermediateDifficultyButton", levels, "Intermediário", 228, 64, () => ChooseDifficulty(ComputerDifficulty.Intermediate)));
        difficultyChoices.Add(CreateMenuChoice("HardDifficultyButton", levels, "Difícil", 160, 64, () => ChooseDifficulty(ComputerDifficulty.Hard)));
    }

    private void BuildLocalOptions(RectTransform controls)
    {
        localOptions = MenuRect("LocalOptions", controls, MenuInset, 270, MenuInnerWidth, 205);
        MenuLabel("LocalTitle", localOptions, "O tabuleiro é dos dois.", 30, textColor, 0, 0, MenuInnerWidth, 48, true);
        MenuLabel("LocalDescription", localOptions, "As brancas começam. Revezem os lances; a câmera acompanha cada turno.", 24, mutedTextColor, 0, 62, MenuInnerWidth, 120);
    }

    private void BuildMenuActions(RectTransform controls, Font display)
    {
        menuSummary = MenuLabel("MatchSummary", controls, "", 18, mutedTextColor, MenuInset, 481, MenuInnerWidth, 48);
        RectTransform buttonDepth = MenuRect("PlayButtonDepth", controls, MenuInset, 540, MenuInnerWidth, 68);
        Surface(buttonDepth, new Color32(175, 145, 0, 255), Color.clear, 10, 0);
        menuPlayButton = MenuButton("StartPlayButton", controls, "Jogar", MenuInset, 535, MenuInnerWidth, 70, actionColor, StartGame);
        var playSurface = StyleMenuButton(menuPlayButton, actionColor, new Color32(255, 231, 77, 255), 10, 2);
        playSurface.BottomShade = 0.98f;
        var playFocus = MenuRect("FocusRing", menuPlayButton.transform, -4, -4, MenuInnerWidth + 8, 78);
        Surface(playFocus, Color.clear, mutedTextColor, 13, 2);
        playFocusRing = playFocus.gameObject;
        var playLabel = menuPlayButton.GetComponentInChildren<Text>();
        playLabel.fontSize = 38;
        if (display != null)
        {
            playLabel.font = display;
        }
        menuHelpButton = MenuButton("StartHowToPlayButton", controls, "Como jogar", MenuInset, 620, 270, 48, Color.clear, ToggleHowToPlay);
        menuHelpButton.GetComponentInChildren<Text>().fontSize = 22;
        BuildPerformanceToggle(controls);
    }

    private void BuildPerformanceToggle(RectTransform controls)
    {
        RectTransform row = MenuRect("PerformanceModeToggle", controls, MenuInset + 286, 620, 272, 48);
        var hitArea = row.gameObject.AddComponent<Image>();
        hitArea.color = Color.clear;
        RectTransform box = MenuRect("Box", row, 0, 9, 30, 30);
        Surface(box, new Color32(19, 57, 43, 255), new Color32(49, 86, 69, 255), 6, 1.5f);
        var check = MenuRect("Checkmark", box, 0, 0, 30, 30).gameObject.AddComponent<Image>();
        check.sprite = Resources.Load<Sprite>("UI/SelectionCheck");
        check.color = accentColor;
        check.raycastTarget = false;
        var label = MenuLabel("Label", row, "Modo desempenho", 18, mutedTextColor, 42, 11, 230, 28);
        label.raycastTarget = false;
        RectTransform focus = MenuRect("FocusRing", row, -4, -4, 280, 56);
        Surface(focus, Color.clear, mutedTextColor, 10, 2);
        performanceFocusRing = focus.gameObject;

        performanceToggle = row.gameObject.AddComponent<Toggle>();
        performanceToggle.transition = Selectable.Transition.None;
        performanceToggle.targetGraphic = hitArea;
        performanceToggle.graphic = check;
        performanceToggle.isOn = gameController != null && gameController.PerformanceMode;
        performanceToggle.onValueChanged.AddListener(OnPerformanceModeChanged);
    }

    private void RegisterMenuTypography()
    {
        foreach (Text label in menuContent.GetComponentsInChildren<Text>(true))
        {
            bool buttonLabel = label.transform.parent.GetComponent<Button>() != null;
            float minimumPixels = buttonLabel ? 16 : label.fontSize >= 28 ? 18 : label.fontSize <= 16 ? 12 : 14;
            menuTypography.Add(new MenuTextSize
            {
                Label = label,
                FontSize = label.fontSize,
                Height = label.rectTransform.rect.height,
                MinimumPixels = minimumPixels
            });
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

    private MenuSurface StyleMenuButton(Button button, Color fill, Color border, float radius, float borderWidth)
    {
        var image = button.GetComponent<Image>();
        // Keep the existing root Image as the hit target for desktop and XR rays.
        // uGUI allows only one Graphic per GameObject, so the chrome is a child.
        image.color = Color.clear;
        var chrome = CreateRect("ButtonSurface", button.transform, Vector2.zero, Vector2.one,
            Vector2.one * 0.5f, Vector2.zero, Vector2.zero);
        chrome.SetAsFirstSibling();
        var surface = Surface(chrome, fill, border, radius, borderWidth);
        button.targetGraphic = surface;
        button.colors = new ColorBlock {
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

    private MenuChoice CreateMenuChoice(string name, Transform parent, string title, float width, float height, UnityEngine.Events.UnityAction action)
    {
        Button button = MenuButton(name, parent, "", 0, 0, width, height, Color.clear, action);
        var layout = button.gameObject.AddComponent<LayoutElement>();
        layout.preferredWidth = width;
        layout.preferredHeight = height;
        var surface = StyleMenuButton(button, new Color32(19, 57, 43, 255), new Color32(49, 86, 69, 255), 7, 1.5f);
        var titleLabel = MenuLabel("Title", button.transform, title, 22, textColor, 8, 6, width - 16, height - 12, true);
        titleLabel.alignment = TextAnchor.MiddleCenter;
        var marker = MenuRect("SelectedMarker", button.transform, 18, (height - 30) * 0.5f, 30, 30).gameObject.AddComponent<Image>();
        marker.sprite = Resources.Load<Sprite>("UI/SelectionCheck");
        marker.color = accentColor;
        marker.raycastTarget = false;
        RectTransform focus = MenuRect("FocusRing", button.transform, -4, -4, width + 8, height + 8);
        Surface(focus, Color.clear, mutedTextColor, 10, 2);
        return new MenuChoice { Button = button, Title = titleLabel, Marker = marker, Surface = surface, FocusRing = focus.gameObject };
    }

    private RectTransform ChoiceRow(string name, Transform parent, float x, float y, float width, float height)
    {
        RectTransform row = MenuRect(name, parent, x, y, width, height);
        var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
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

    private Text MenuLabel(string name, Transform parent, string value, int size, Color color, float x, float y, float width, float height, bool bold = false)
    {
        return CreateText(name, parent, value, size, bold ? FontStyle.Bold : FontStyle.Normal, color, TextAnchor.UpperLeft, new Vector2(x, -y), new Vector2(width, height));
    }

    private Button MenuButton(string name, Transform parent, string label, float x, float y, float width, float height, Color color, UnityEngine.Events.UnityAction action)
    {
        var button = CreateButton(name, parent, label, Vector2.zero, new Vector2(width, height), color, action);
        var rect = (RectTransform)button.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(x, -y);
        return button;
    }

    private void MenuRule(Transform parent, float x, float y, float width)
    {
        var line = MenuRect("Divider", parent, x, y, width, 1).gameObject.AddComponent<Image>();
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
