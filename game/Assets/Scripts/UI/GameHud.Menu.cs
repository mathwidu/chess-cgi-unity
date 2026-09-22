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

    private void BuildStartMenu()
    {
        modeChoices.Clear();
        sideChoices.Clear();
        difficultyChoices.Clear();
        menuTypography.Clear();
        menuCanvas = GetComponent<Canvas>();
        startOverlay = CreatePanel("StartOverlay", hudRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Vector2.zero, overlayColor);
        menuContent = CreateRect("MenuContent", startOverlay, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, new Vector2(1600f, 960f));
        menuControls = menuContent.gameObject.AddComponent<CanvasGroup>();

        var castImage = CreateRawImage("MenuCast", menuContent, new Vector2(24, -106), new Vector2(970, 740), Color.white);
        castImage.raycastTarget = false;
        menuCast = castImage.gameObject.AddComponent<MenuCastPreview>();
        menuCast.Configure(castImage, chosenSide);

        var title = MenuLabel("StartTitle", menuContent, "Xadrez", 46, textColor, 64, 32, 220, 66, true);
        Font display = Resources.Load<Font>("UI/Lato-Black");
        if (display != null) title.font = display;
        MenuLabel("ProjectName", menuContent, "CGI", 22, accentColor, 228, 55, 78, 32, true);
        MenuLabel("CastCaption", menuContent, "A turma no tabuleiro.", 19, mutedTextColor, 65, 103, 470, 30);

        var logo = Resources.Load<Texture2D>("UI/FeevaleLogo");
        if (logo != null)
        {
            float width = 280;
            var logoImage = CreateRawImage("FeevaleSignature", menuContent, new Vector2(1264, -22), new Vector2(width, width * logo.height / logo.width), Color.white);
            logoImage.texture = logo;
            logoImage.raycastTarget = false;
        }

        castName = MenuLabel("CastName", menuContent, "", 30, textColor, 82, 808, 810, 44, true);
        castRole = MenuLabel("CastRole", menuContent, "", 19, mutedTextColor, 84, 856, 810, 30);
        MenuRule(menuContent, 64, 929, 1472);
        MenuLabel("ProjectCredit", menuContent, "Projeto de Computação Gráfica I", 16, mutedTextColor, 64, 945, 760, 26);

        RectTransform controls = MenuRect("StartCard", menuContent, 1050, 220, 486, 660);
        MenuLabel("MenuTitle", controls, "Nova partida", 42, textColor, 0, 0, 486, 60, true);
        MenuLabel("ModeHeading", controls, "Modo de jogo", 19, mutedTextColor, 0, 96, 486, 28);
        RectTransform modes = ChoiceRow("ModeOptions", controls, 0, 132, 486, 58);
        modes.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>().spacing = 12;
        modeChoices.Add(CreateMenuChoice("ComputerModeButton", modes, "Contra IA", 224, 58, () => ChooseMode(true)));
        modeChoices.Add(CreateMenuChoice("LocalModeButton", modes, "Dois jogadores", 250, 58, () => ChooseMode(false)));

        computerOptions = MenuRect("ComputerOptions", controls, 0, 220, 486, 236);
        MenuLabel("SideHeading", computerOptions, "Você joga com", 19, mutedTextColor, 0, 0, 486, 28);
        RectTransform sides = ChoiceRow("SideOptions", computerOptions, 0, 36, 486, 64);
        sides.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>().spacing = 12;
        sideChoices.Add(CreateMenuChoice("WhiteSideButton", sides, "Brancas", 237, 64, () => ChooseSide(ChessSide.White)));
        sideChoices.Add(CreateMenuChoice("BlackSideButton", sides, "Pretas", 237, 64, () => ChooseSide(ChessSide.Black)));
        MenuLabel("DifficultyHeading", computerOptions, "Dificuldade da IA", 19, mutedTextColor, 0, 132, 486, 28);
        RectTransform levels = ChoiceRow("DifficultyOptions", computerOptions, 0, 168, 486, 58);
        levels.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>().spacing = 8;
        difficultyChoices.Add(CreateMenuChoice("BeginnerDifficultyButton", levels, "Iniciante", 140, 58, () => ChooseDifficulty(ComputerDifficulty.Beginner)));
        difficultyChoices.Add(CreateMenuChoice("IntermediateDifficultyButton", levels, "Intermediário", 210, 58, () => ChooseDifficulty(ComputerDifficulty.Intermediate)));
        difficultyChoices.Add(CreateMenuChoice("HardDifficultyButton", levels, "Difícil", 120, 58, () => ChooseDifficulty(ComputerDifficulty.Hard)));

        localOptions = MenuRect("LocalOptions", controls, 0, 238, 486, 218);
        MenuLabel("LocalTitle", localOptions, "O tabuleiro é dos dois.", 28, textColor, 0, 0, 486, 48, true);
        MenuLabel("LocalDescription", localOptions, "As brancas começam. Revezem os lances; a câmera acompanha cada turno.", 22, mutedTextColor, 0, 64, 470, 132);

        menuSummary = MenuLabel("MatchSummary", controls, "", 18, mutedTextColor, 0, 486, 486, 30);
        menuPlayButton = MenuButton("StartPlayButton", controls, "Jogar", 0, 540, 486, 72, actionColor, StartGame);
        menuPlayButton.GetComponentInChildren<UnityEngine.UI.Text>().fontSize = 27;
        menuHelpButton = MenuButton("StartHowToPlayButton", controls, "Como jogar", 0, 626, 486, 44, Color.clear, ToggleHowToPlay);
        ConfigureMenuNavigation();
        foreach (var label in menuContent.GetComponentsInChildren<UnityEngine.UI.Text>(true))
        {
            bool buttonLabel = label.transform.parent.GetComponent<UnityEngine.UI.Button>() != null;
            menuTypography.Add(new MenuTextSize { Label = label, FontSize = label.fontSize,
                Height = label.rectTransform.rect.height,
                MinimumPixels = buttonLabel ? 16 : label.fontSize >= 28 ? 18 : label.fontSize <= 16 ? 12 : 14 });
        }
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
        float scale = Mathf.Min(1f, (available.x - 80f) / 1600f, (available.y - 64f) / 960f);
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
        SetActive(computerOptions, chooseComputer);
        SetActive(localOptions, !chooseComputer);
        if (menuCast != null) menuCast.SetSide(chosenSide, !chooseComputer);
        castName.text = !chooseComputer ? "Marta e Ricardo" : chosenSide == ChessSide.White ? "Professora Marta" : "Professor Ricardo";
        castRole.text = !chooseComputer ? "Duas pessoas. Uma partida." : chosenSide == ChessSide.White ? "Rainha  /  Você joga de brancas" : "Rei  /  Você joga de pretas";
        menuSummary.text = chooseComputer
            ? "Contra IA   /   " + DifficultyName(chosenDifficulty) + "   /   " + SideName(chosenSide)
            : "Partida local   /   Dois jogadores   /   Brancas começam";
    }

    private MenuChoice CreateMenuChoice(string name, Transform parent, string title, float width, float height, UnityEngine.Events.UnityAction action)
    {
        UnityEngine.UI.Button button = MenuButton(name, parent, "", 0, 0, width, height, Color.clear, action);
        var layout = button.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
        layout.preferredWidth = width;
        layout.preferredHeight = height;
        var titleLabel = MenuLabel("Title", button.transform, title, 22, textColor, 12, 6, width - 24, height - 12, true);
        titleLabel.alignment = TextAnchor.MiddleLeft;
        var marker = MenuRect("SelectedMarker", button.transform, 14, height - 3, width - 28, 2).gameObject.AddComponent<UnityEngine.UI.Image>();
        marker.color = accentColor;
        marker.raycastTarget = false;
        RectTransform focus = MenuRect("FocusRing", button.transform, 0, 0, width, height);
        foreach (Rect edge in new[] { new Rect(0, 0, width, 2), new Rect(0, height - 2, width, 2), new Rect(0, 0, 2, height), new Rect(width - 2, 0, 2, height) })
        {
            var line = MenuRect("Edge", focus, edge.x, edge.y, edge.width, edge.height).gameObject.AddComponent<UnityEngine.UI.Image>();
            line.color = mutedTextColor;
            line.raycastTarget = false;
        }
        return new MenuChoice { Button = button, Title = titleLabel, Marker = marker, FocusRing = focus.gameObject };
    }

    private void SetChoice(MenuChoice choice, bool selected)
    {
        choice.Marker.enabled = selected;
        choice.FocusRing.SetActive(EventSystem.current != null && EventSystem.current.currentSelectedGameObject == choice.Button.gameObject);
        choice.Title.color = selected ? textColor : mutedTextColor;
        var colors = choice.Button.colors;
        Color normal = selected ? new Color32(19, 64, 49, 255) : Color.clear;
        Color hover = selected ? new Color32(24, 80, 58, 255) : new Color32(15, 49, 38, 255);
        if (colors.normalColor != normal || colors.selectedColor != normal || colors.highlightedColor != hover)
        {
            colors.normalColor = normal;
            colors.selectedColor = normal;
            colors.highlightedColor = hover;
            choice.Button.colors = colors;
        }
    }

    private RectTransform ChoiceRow(string name, Transform parent, float x, float y, float width, float height)
    {
        RectTransform row = MenuRect(name, parent, x, y, width, height);
        var layout = row.gameObject.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
        layout.spacing = 24;
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
