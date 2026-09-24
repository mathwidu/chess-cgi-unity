using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Runtime choices, keyboard focus and responsive state; construction lives in GameHud.Menu.Layout.cs.
public sealed partial class GameHud
{
    private sealed class MenuChoice
    {
        public Button Button;
        public Text Title;
        public Image Marker;
        public GameObject FocusRing;
        public MenuSurface Surface;
    }

    private sealed class MenuTextSize
    {
        public Text Label;
        public int FontSize;
        public float Height;
        public float MinimumPixels;
    }

    private RectTransform menuContent;
    private RectTransform computerOptions;
    private RectTransform localOptions;
    private Text menuSummary;
    private readonly List<MenuChoice> modeChoices = new List<MenuChoice>();
    private readonly List<MenuChoice> sideChoices = new List<MenuChoice>();
    private readonly List<MenuChoice> difficultyChoices = new List<MenuChoice>();
    private GameObject focusBeforeHelp;
    private MenuCastPreview menuCast;
    private CanvasGroup menuControls;
    private CanvasGroup matchControls;
    private Button menuPlayButton;
    private Button menuHelpButton;
    private RectTransform focusScope;
    private Text castName;
    private Text castRole;
    private readonly List<MenuTextSize> menuTypography = new List<MenuTextSize>();
    private Canvas menuCanvas;
    private RectTransform castRect;
    private Text otherCastName;
    private RectTransform whiteShadow;
    private RectTransform blackShadow;
    private RectTransform whiteCaptionShade;
    private RectTransform blackCaptionShade;
    private GameObject playFocusRing;
    private Toggle performanceToggle;
    private GameObject performanceFocusRing;

    private void LateUpdate()
    {
        if (menuCast == null || !showStartScreen)
        {
            return;
        }
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
            MenuNavigation(difficultyChoices[i].Button, i == 0 ? sideChoices[1].Button : difficultyChoices[i - 1].Button,
                i == 2 ? menuPlayButton : difficultyChoices[i + 1].Button, firstSide, menuPlayButton);
        MenuNavigation(menuPlayButton, chooseComputer ? lastLevel : modeChoices[1].Button, null, chooseComputer ? lastLevel : modeChoices[1].Button, menuHelpButton);
        MenuNavigation(menuHelpButton, menuPlayButton, performanceToggle, menuPlayButton, null);
        MenuNavigation(performanceToggle, menuHelpButton, null, menuPlayButton, null);
    }

    private static void MenuNavigation(Selectable button, Selectable left,
        Selectable right, Selectable up, Selectable down)
    {
        button.navigation = new Navigation
        {
            mode = Navigation.Mode.Explicit,
            selectOnLeft = left,
            selectOnRight = right,
            selectOnUp = up,
            selectOnDown = down
        };
    }

    private void RefreshNavigationFocus()
    {
        EventSystem events = EventSystem.current;
        if (events == null)
        {
            return;
        }
        RectTransform scope = showHowToPlay ? howToPlayPanel :
            computerErrorPanel.gameObject.activeSelf ? computerErrorPanel :
            promotionPanel.gameObject.activeSelf ? promotionPanel :
            showStartScreen ? menuContent : matchInterface;
        GameObject current = events.currentSelectedGameObject;
        var currentButton = current != null ? current.GetComponent<Button>() : null;
        if (focusScope == scope && currentButton != null && current.activeInHierarchy &&
            current.transform.IsChildOf(scope) && currentButton.IsInteractable()) return;

        focusScope = scope;
        foreach (var button in scope.GetComponentsInChildren<Button>())
        {
            if (!button.IsInteractable())
            {
                continue;
            }
            events.firstSelectedGameObject = button.gameObject;
            events.SetSelectedGameObject(button.gameObject);
            return;
        }
    }

    private void ChooseMode(bool computer)
    {
        chooseComputer = computer;
        ConfigureMenuNavigation();
        RefreshInterface();
    }

    private void ChooseSide(ChessSide side)
    {
        chosenSide = side;
        RefreshInterface();
    }

    private void ChooseDifficulty(ComputerDifficulty difficulty)
    {
        chosenDifficulty = difficulty;
        RefreshInterface();
    }

    private void RefreshStartMenu()
    {
        if (menuContent == null)
        {
            return;
        }

        menuControls.interactable = !showHowToPlay;
        if (matchControls != null)
        {
            bool matchNeedsChoice = gameController != null &&
                (gameController.IsAwaitingPromotion || gameController.HasComputerError);
            matchControls.interactable = !showHowToPlay && !matchNeedsChoice;
        }

        FitMenuToCanvas();
        RefreshMenuChoices();
        RefreshCastDetails();
        menuSummary.text = chooseComputer
            ? DifficultyDescription(chosenDifficulty)
            : "Uma partida local para jogar a dois.";
        PositionCastLabels();
    }

    private void FitMenuToCanvas()
    {
        // Fit the same logical surface in the desktop Game view and the VR world-space Canvas.
        Vector2 available = startOverlay.rect.size;
        float scale = Mathf.Min((available.x - 2f) / MenuWidth, (available.y - 2f) / MenuHeight);
        menuContent.localScale = Vector3.one * Mathf.Max(0.1f, scale);
        float canvasScale = menuCanvas.renderMode == RenderMode.WorldSpace ? 1f : menuCanvas.scaleFactor;
        float effectiveScale = menuContent.localScale.x * canvasScale;

        foreach (MenuTextSize type in menuTypography)
        {
            int fontSize = Mathf.Max(type.FontSize, Mathf.CeilToInt(type.MinimumPixels / Mathf.Max(0.4f, effectiveScale)));
            if (type.Label.fontSize != fontSize)
            {
                type.Label.fontSize = fontSize;
            }

            float height = Mathf.Max(type.Height, fontSize * 1.25f);
            if (!Mathf.Approximately(type.Label.rectTransform.rect.height, height))
            {
                type.Label.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            }
        }
    }

    private void RefreshMenuChoices()
    {
        SelectMenuChoice(modeChoices, chooseComputer ? 0 : 1);
        SelectMenuChoice(sideChoices, chosenSide == ChessSide.White ? 0 : 1);
        SelectMenuChoice(difficultyChoices, (int)chosenDifficulty);
        playFocusRing.SetActive(IsNavigationFocused(menuPlayButton));
        performanceFocusRing.SetActive(IsNavigationFocused(performanceToggle));
        SetActive(computerOptions, chooseComputer);
        SetActive(localOptions, !chooseComputer);
    }

    private void SelectMenuChoice(List<MenuChoice> choices, int selectedIndex)
    {
        for (int i = 0; i < choices.Count; i++)
        {
            SetChoice(choices[i], i == selectedIndex);
        }
    }

    private void RefreshCastDetails()
    {
        if (menuCast != null)
        {
            menuCast.SetSide(chosenSide, !chooseComputer);
        }

        bool highlightWhite = chooseComputer && chosenSide == ChessSide.White;
        castName.text = highlightWhite ? "Professora Marta" : "Professor Ricardo";
        otherCastName.text = highlightWhite ? "Professor Ricardo" : "Professora Marta";
        castRole.text = !chooseComputer ? "Duas pessoas. Uma partida."
            : highlightWhite ? "Rainha · Você joga de brancas" : "Rei · Você joga de pretas";
    }

    private static string DifficultyDescription(ComputerDifficulty difficulty)
    {
        switch (difficulty)
        {
            case ComputerDifficulty.Beginner:
                return "Para praticar os primeiros movimentos.";
            case ComputerDifficulty.Intermediate:
                return "Mais atenção a cada jogada.";
            default:
                return "Um desafio maior, com menos erros do adversário.";
        }
    }

    private static bool IsNavigationFocused(Selectable button)
    {
        return EventSystem.current != null && EventSystem.current.currentSelectedGameObject == button.gameObject;
    }

    private void SetChoice(MenuChoice choice, bool selected)
    {
        choice.Marker.enabled = selected;
        choice.FocusRing.SetActive(IsNavigationFocused(choice.Button));
        choice.Title.color = selected ? textColor : mutedTextColor;
        choice.Title.font = selected ? GetHudBoldFont() : GetHudFont();
        RectTransform rect = (RectTransform)choice.Button.transform;
        float left = selected ? 51 : 8;
        choice.Title.rectTransform.anchoredPosition = new Vector2(left, -6);
        choice.Title.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Max(1, rect.rect.width - left - 8));
        choice.Surface.color = selected ? new Color32(23, 64, 47, 255) : new Color32(19, 57, 43, 255);
        choice.Surface.SetBorder(selected ? accentColor : new Color32(49, 86, 69, 255));
    }

}
