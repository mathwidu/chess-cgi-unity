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
    private RectTransform focusScope;

    private void BuildStartMenu()
    {
        modeChoices.Clear();
        sideChoices.Clear();
        difficultyChoices.Clear();
        startOverlay = CreatePanel("StartOverlay", hudRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Vector2.zero, overlayColor);
        menuContent = CreateRect("MenuContent", startOverlay, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, new Vector2(1600f, 960f));
        menuControls = menuContent.gameObject.AddComponent<CanvasGroup>();

        var castImage = CreateRawImage("MenuCast", menuContent, new Vector2(602, -136), new Vector2(990, 756), Color.white);
        castImage.raycastTarget = false;
        menuCast = castImage.gameObject.AddComponent<MenuCastPreview>();
        menuCast.Configure(castImage, chosenSide);

        var logo = Resources.Load<Texture2D>("UI/FeevaleLogo");
        if (logo != null)
        {
            var logoImage = CreateRawImage("FeevaleSignature", menuContent, new Vector2(1240, -30), new Vector2(304, 304f * logo.height / logo.width), Color.white);
            logoImage.texture = logo;
            logoImage.raycastTarget = false;
        }
        var title = MenuLabel("StartTitle", menuContent, "Xadrez", 140, textColor, 46, 88, 734, 184, true);
        Font display = Resources.Load<Font>("UI/Lato-Black");
        if (display != null) title.font = display;
        MenuLabel("ProjectName", menuContent, "CGI", 46, accentColor, 54, 266, 520, 62, true);
        MenuLabel("CastCaption", menuContent, "A turma no tabuleiro.", 30, textColor, 760, 872, 780, 44, true);
        MenuLabel("ProjectCredit", menuContent, "Projeto de Computação Gráfica I", 18, mutedTextColor, 760, 923, 780, 28);

        RectTransform controls = MenuRect("StartCard", menuContent, 56, 346, 530, 598);
        RectTransform modes = ChoiceRow("ModeOptions", controls, 0, 0, 530, 64);
        modeChoices.Add(CreateMenuChoice("ComputerModeButton", modes, "Contra IA", 253, 64, () => ChooseMode(true)));
        modeChoices.Add(CreateMenuChoice("LocalModeButton", modes, "Dois jogadores", 253, 64, () => ChooseMode(false)));

        computerOptions = MenuRect("ComputerOptions", controls, 0, 100, 530, 330);
        MenuLabel("SideHeading", computerOptions, "Você joga com", 20, mutedTextColor, 0, 0, 530, 30);
        RectTransform sides = ChoiceRow("SideOptions", computerOptions, 0, 42, 530, 56);
        sideChoices.Add(CreateMenuChoice("WhiteSideButton", sides, "Brancas", 253, 56, () => ChooseSide(ChessSide.White)));
        sideChoices.Add(CreateMenuChoice("BlackSideButton", sides, "Pretas", 253, 56, () => ChooseSide(ChessSide.Black)));
        MenuLabel("DifficultyHeading", computerOptions, "Dificuldade da IA", 20, mutedTextColor, 0, 130, 530, 30);
        RectTransform levels = MenuRect("DifficultyOptions", computerOptions, 0, 173, 530, 158);
        var levelLayout = levels.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
        levelLayout.spacing = 7;
        levelLayout.childControlWidth = levelLayout.childControlHeight = true;
        levelLayout.childForceExpandWidth = levelLayout.childForceExpandHeight = false;
        difficultyChoices.Add(CreateMenuChoice("BeginnerDifficultyButton", levels, "Iniciante", 530, 48, () => ChooseDifficulty(ComputerDifficulty.Beginner)));
        difficultyChoices.Add(CreateMenuChoice("IntermediateDifficultyButton", levels, "Intermediário", 530, 48, () => ChooseDifficulty(ComputerDifficulty.Intermediate)));
        difficultyChoices.Add(CreateMenuChoice("HardDifficultyButton", levels, "Difícil", 530, 48, () => ChooseDifficulty(ComputerDifficulty.Hard)));

        localOptions = MenuRect("LocalOptions", controls, 0, 110, 530, 320);
        MenuLabel("LocalTitle", localOptions, "As brancas\ncomeçam.", 44, textColor, 0, 22, 520, 120, true);
        MenuLabel("LocalDescription", localOptions, "Revezem os lances neste dispositivo.\nA câmera acompanha cada turno.", 24, mutedTextColor, 0, 180, 520, 94);

        menuSummary = MenuLabel("MatchSummary", controls, "", 18, mutedTextColor, 0, 449, 530, 30);
        var play = MenuButton("StartPlayButton", controls, "Jogar", 0, 490, 530, 68, actionColor, StartGame);
        play.GetComponentInChildren<UnityEngine.UI.Text>().fontSize = 27;
        MenuButton("StartHowToPlayButton", controls, "Como jogar", 0, 573, 210, 40, Color.clear, ToggleHowToPlay);
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

    private void ChooseMode(bool computer) { chooseComputer = computer; RefreshInterface(); }
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
        for (int i = 0; i < modeChoices.Count; i++) SetChoice(modeChoices[i], i == (chooseComputer ? 0 : 1));
        for (int i = 0; i < sideChoices.Count; i++) SetChoice(sideChoices[i], i == (chosenSide == ChessSide.White ? 0 : 1));
        for (int i = 0; i < difficultyChoices.Count; i++) SetChoice(difficultyChoices[i], i == (int)chosenDifficulty);
        SetActive(computerOptions, chooseComputer);
        SetActive(localOptions, !chooseComputer);
        if (menuCast != null) menuCast.SetSide(chosenSide);
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
        var titleLabel = MenuLabel("Title", button.transform, title, 27, textColor, 14, 6, width - 28, height - 12, true);
        titleLabel.alignment = TextAnchor.MiddleLeft;
        var marker = MenuRect("SelectedMarker", button.transform, 14, height - 3, width - 28, 2).gameObject.AddComponent<UnityEngine.UI.Image>();
        marker.color = accentColor;
        marker.raycastTarget = false;
        return new MenuChoice { Button = button, Title = titleLabel, Marker = marker };
    }

    private void SetChoice(MenuChoice choice, bool selected)
    {
        choice.Marker.enabled = selected;
        choice.Title.color = selected ? textColor : mutedTextColor;
        var colors = choice.Button.colors;
        Color normal = selected ? new Color(1, 1, 1, 0.055f) : Color.clear;
        if (colors.normalColor != normal)
        {
            colors.normalColor = normal;
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
