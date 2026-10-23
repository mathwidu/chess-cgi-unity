using UnityEngine;

// Web presentation reuses the match/menu state and existing controls. The native
// desktop and world-space builders keep their original composition.
public sealed partial class GameHud
{
    private bool IsWebPresentation => WebTouchInput.Enabled && !XRRig.IsHeadsetPresent;
    private bool WebCompactMenu => IsWebPresentation;
    private bool webHistoryOpen, webPreviewOpen;
    private RectTransform webHistoryOverlay, webPreviewOverlay, webHeader, webActions;
    private UnityEngine.UI.Button webPreviewButton;
    private UnityEngine.UI.ScrollRect webMenuScroll;
    private Vector2 webLastSize, webMenuLastSize;

    private void ConfigureWebSafeArea()
    {
        if (!IsWebPresentation || hudRoot == null) return;
        Vector2 viewport = WebTouchInput.ViewportSize;
        float scale = WebTouchInput.UiScale;
        Vector2 logicalSize = viewport / scale;
        var scaler = GetComponent<UnityEngine.UI.CanvasScaler>();
        scaler.referenceResolution = logicalSize;
        scaler.matchWidthOrHeight = 0;
        Rect safe = WebTouchInput.SafeArea;
        if (safe.width <= 0 || safe.height <= 0) safe = new Rect(Vector2.zero, viewport);
        hudRoot.anchorMin = hudRoot.anchorMax = hudRoot.pivot = Vector2.zero;
        hudRoot.anchoredPosition = safe.position / scale;
        hudRoot.sizeDelta = safe.size / scale;
    }

    private void BuildWebMatchInterface()
    {
        webLastSize = webMenuLastSize = Vector2.zero;
        webMenuScroll = null;
        matchInterface = CreateRect("MatchInterface", hudRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Vector2.zero);
        matchControls = matchInterface.gameObject.AddComponent<CanvasGroup>();
        webHeader = CreatePanel("TurnPanel", matchInterface, new Vector2(.5f, 1), new Vector2(.5f, 1),
            new Vector2(.5f, 1), new Vector2(0, -8), new Vector2(336, 104), panelColor);
        matchSummaryText = MenuLabel("MatchDetails", webHeader, "", 12, mutedTextColor, 12, 8, 312, 18);
        turnText = MenuLabel("TurnText", webHeader, "Brancas jogam", 23, accentColor, 12, 27, 312, 31, true);
        statusText = MenuLabel("StatusText", webHeader, "", 15, textColor, 12, 62, 312, 38);

        webActions = CreatePanel("ActionBar", matchInterface, new Vector2(.5f, 0), new Vector2(.5f, 0),
            new Vector2(.5f, 0), new Vector2(0, 8), new Vector2(336, 104), panelColor);
        desktopView = Object.FindFirstObjectByType<CameraController>();
        MenuButton("CameraLeftButton", webActions, "Girar ←", 0, 0, 60, 44, neutralButtonColor,
            () => desktopView?.ApplyTouchGesture(-20f, 0));
        MenuButton("CameraRightButton", webActions, "Girar →", 0, 0, 60, 44, neutralButtonColor,
            () => desktopView?.ApplyTouchGesture(20f, 0));
        MenuButton("CameraZoomOutButton", webActions, "− Zoom", 0, 0, 60, 44, neutralButtonColor,
            () => desktopView?.ApplyTouchGesture(0, -.08f));
        MenuButton("CameraZoomInButton", webActions, "+ Zoom", 0, 0, 60, 44, neutralButtonColor,
            () => desktopView?.ApplyTouchGesture(0, .08f));
        MenuButton("CameraResetButton", webActions, "Voltar", 0, 0, 60, 44, neutralButtonColor,
            () => desktopView?.ReturnToBoard());
        MenuButton("NewGameButton", webActions, "Nova", 0, 0, 60, 44, actionColor, RestartGame);
        cancelButtonText = MenuButton("CancelButton", webActions, "Cancelar", 0, 0, 60, 44, neutralButtonColor, CancelSelection)
            .GetComponentInChildren<UnityEngine.UI.Text>();
        MenuButton("WebHistoryButton", webActions, "Lances", 0, 0, 60, 44, neutralButtonColor,
            () => { webHistoryOpen = true; webPreviewOpen = false; RefreshInterface(); });
        webPreviewButton = MenuButton("WebPreviewButton", webActions, "Peça", 0, 0, 60, 44, neutralButtonColor,
            () => { webPreviewOpen = true; webHistoryOpen = false; RefreshInterface(); });
        MenuButton("MenuButton", webActions, "Menu", 0, 0, 60, 44, neutralButtonColor, ShowMenu);

        webHistoryOverlay = ModalOverlay("WebHistoryOverlay");
        RectTransform history = ModalCard("MoveHistoryPanel", webHistoryOverlay, 336, 320);
        MenuLabel("MoveHistoryTitle", history, "ÚLTIMOS LANCES", 18, accentColor, 20, 20, 296, 28, true);
        moveHistoryText = MenuLabel("MoveHistoryText", history, "", 18, textColor, 20, 66, 296, 178);
        MenuButton("CloseHistoryButton", history, "Fechar", 20, 256, 296, 44, actionColor,
            () => { webHistoryOpen = false; RefreshInterface(); });

        BuildSelectedPieceDetails();
        selectedPiecePanel.Find("PreviewGestureHint").GetComponent<UnityEngine.UI.Text>().text = "Arraste para girar";
        webPreviewOverlay = ModalOverlay("WebPreviewOverlay");
        selectedPiecePanel.SetParent(webPreviewOverlay, false);
        MenuButton("ClosePreviewButton", selectedPiecePanel, "Fechar", 0, 0, 100, 44, actionColor,
            () => { webPreviewOpen = false; RefreshInterface(); });
        BuildPromotionDialog();
        BuildComputerErrorDialog();
        BuildGameOverDialog();
    }

    private void RefreshWebPanels()
    {
        if (!IsWebPresentation) return;
        bool selected = gameController != null && gameController.SelectedPiece != null;
        bool mandatory = showStartScreen || showHowToPlay || IsResultShown || (gameController != null &&
            (gameController.IsAwaitingPromotion || gameController.HasComputerError));
        if (!selected || mandatory) webPreviewOpen = false;
        if (mandatory) webHistoryOpen = false;
        SetActive(webPreviewOverlay, webPreviewOpen && selected);
        SetActive(webHistoryOverlay, webHistoryOpen);
        if (webPreviewButton != null) webPreviewButton.interactable = selected;
    }

    private void RefreshWebLayout()
    {
        if (!IsWebPresentation || hudRoot == null || webHeader == null) return;
        ConfigureWebSafeArea();
        Vector2 size = hudRoot.rect.size;
        if (size == webLastSize) return;
        webLastSize = size;
        float width = Mathf.Min(600, size.x - 16);
        webHeader.anchorMin = webHeader.anchorMax = webHeader.pivot = new Vector2(.5f, 1);
        webHeader.anchoredPosition = new Vector2(0, -8);
        webHeader.sizeDelta = new Vector2(width, 104);
        foreach (var text in webHeader.GetComponentsInChildren<UnityEngine.UI.Text>())
            text.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width - 24);
        webActions.anchorMin = webActions.anchorMax = webActions.pivot = new Vector2(.5f, 0);
        webActions.anchoredPosition = new Vector2(0, 8);
        webActions.sizeDelta = new Vector2(width, 104);
        string[] cameraButtons = { "CameraLeftButton", "CameraRightButton", "CameraZoomOutButton", "CameraZoomInButton", "CameraResetButton" };
        string[] actions = { "NewGameButton", "CancelButton", "WebHistoryButton", "WebPreviewButton", "MenuButton" };
        float cell = (width - 24) / 5f;
        for (int i = 0; i < 5; i++)
        {
            WebButton(webActions, cameraButtons[i], 4 + i * (cell + 4), 4, cell, 44, 13);
            WebButton(webActions, actions[i], 4 + i * (cell + 4), 56, cell, 44, 13);
        }
        if (WebTouchInput.ShortLandscape)
        {
            webHeader.anchorMin = webHeader.anchorMax = webHeader.pivot = new Vector2(0, 1);
            webHeader.anchoredPosition = new Vector2(8, -8);
            webHeader.sizeDelta = new Vector2(144, 196);
            WebRect(webHeader, "MatchDetails", 12, 10, 120, 42, 11);
            WebRect(webHeader, "TurnText", 12, 58, 120, 58, 22);
            WebRect(webHeader, "StatusText", 12, 122, 120, 68, 14);
            webActions.anchorMin = webActions.anchorMax = webActions.pivot = new Vector2(1, .5f);
            webActions.anchoredPosition = new Vector2(-8, 0);
            webActions.sizeDelta = new Vector2(144, 248);
            for (int i = 0; i < 5; i++)
            {
                WebButton(webActions, cameraButtons[i], 4, 4 + i * 50, 66, 44, 13);
                WebButton(webActions, actions[i], 74, 4 + i * 50, 66, 44, 13);
            }
        }
        else
        {
            WebRect(webHeader, "MatchDetails", 12, 8, width - 24, 18, 12);
            WebRect(webHeader, "TurnText", 12, 27, width - 24, 31, 23);
            WebRect(webHeader, "StatusText", 12, 62, width - 24, 38, 15);
        }
        LayoutWebDialogs(size);
        LayoutWebPreview(size);
        LayoutWebHelp(size);
    }

    private void FitWebMenu()
    {
        if (webMenuScroll == null)
        {
            var scroll = CreateRect("WebMenuScroll", startOverlay, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Vector2.zero);
            webMenuScroll = scroll.gameObject.AddComponent<UnityEngine.UI.ScrollRect>();
            var viewport = CreatePanel("Viewport", scroll, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Vector2.zero, Color.clear);
            viewport.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
            menuContent.SetParent(viewport, false);
            webMenuScroll.viewport = viewport;
            webMenuScroll.content = menuContent;
            webMenuScroll.horizontal = false;
            webMenuScroll.vertical = true;
            webMenuScroll.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped;
            menuContent.anchorMin = menuContent.anchorMax = menuContent.pivot = new Vector2(.5f, 1);
            menuContent.anchoredPosition = Vector2.zero;
            menuContent.localScale = Vector3.one;
        }
        Vector2 available = hudRoot.rect.size;
        if (webMenuLastSize == available) return;
        webMenuLastSize = available;
        bool portrait = available.x < 580;
        float width = available.x - 16;
        float cardWidth = portrait ? width : Mathf.Min(440, width * .56f);
        float stageWidth = portrait ? width : width - cardWidth - 16;
        float cardY = portrait ? 196 : 8;
        float contentHeight = Mathf.Max(available.y, cardY + 500);
        menuContent.sizeDelta = new Vector2(width, contentHeight);
        WebRect(menuContent, "MenuStudyBackground", 0, 0, width, contentHeight);
        WebRect(menuContent, "StartTitle", 12, 8, stageWidth - 74, 46, 34);
        WebRect(menuContent, "ProjectName", stageWidth - 62, 20, 58, 30, 23);
        WebRect(menuContent, "CastCaption", 12, 53, stageWidth - 24, 24, 16);
        float castHeight = portrait ? 112 : Mathf.Min(310, stageWidth / 1.097f);
        float castWidth = castHeight * 645f / 588f;
        WebRect(menuContent, "MenuCast", (stageWidth - castWidth) * .5f, portrait ? 76 : 100, castWidth, castHeight);
        WebRect(menuContent, "CastName", 8, portrait ? 170 : 110 + castHeight, stageWidth - 16, 22, 14);
        // Keep the professors visible; their compact caption gives the selected side.
        SetActive(castRole.rectTransform, false);
        SetActive(otherCastName.rectTransform, false);
        foreach (var decoration in new[] { whiteShadow, blackShadow, whiteCaptionShade, blackCaptionShade }) SetActive(decoration, false);
        Transform logo = menuContent.Find("FeevaleSignature");
        if (logo != null)
        {
            logo.gameObject.SetActive(!portrait);
            WebRect(menuContent, "FeevaleSignature", 12, 154 + castHeight, Mathf.Min(180, stageWidth - 24), Mathf.Min(180, stageWidth - 24) * 369f / 950f);
        }
        Transform credit = menuContent.Find("ProjectCredit");
        if (credit != null) credit.gameObject.SetActive(false);
        Transform divider = menuContent.Find("Divider");
        if (divider != null) divider.gameObject.SetActive(false);
        RectTransform card = WebRect(menuContent, "StartCard", portrait ? 0 : stageWidth + 16, cardY, cardWidth, 484);
        float inner = cardWidth - 32;
        WebRect(card, "MenuTitle", 16, 12, inner, 34, 26);
        WebRect(card, "ModeHeading", 16, 48, inner, 20, 15);
        WebRect(card, "ModeOptions", 16, 72, inner, 44);
        WebRect(card, "ComputerOptions", 16, 128, inner, 144);
        WebRect(computerOptions, "SideHeading", 0, 0, inner, 20, 15);
        WebRect(computerOptions, "SideOptions", 0, 24, inner, 44);
        WebRect(computerOptions, "DifficultyHeading", 0, 78, inner, 20, 15);
        WebRect(computerOptions, "DifficultyOptions", 0, 102, inner, 44);
        WebRect(card, "LocalOptions", 16, 136, inner, 132);
        WebRect(localOptions, "LocalTitle", 0, 0, inner, 44, 23);
        WebRect(localOptions, "LocalDescription", 0, 50, inner, 76, 17);
        WebRect(card, "MatchSummary", 16, 282, inner, 38, 15);
        WebRect(card, "PlayButtonDepth", 16, 328, inner, 50);
        WebButton(card, "StartPlayButton", 16, 324, inner, 50, 25);
        WebRect(menuPlayButton.transform, "FocusRing", -3, -3, inner + 6, 56);
        WebButton(card, "StartHowToPlayButton", 16, 388, inner, 44, 17);
        RectTransform performance = WebRect(card, "PerformanceModeToggle", 16, 436, inner, 44);
        WebRect(performance, "Box", 0, 3, 30, 30);
        WebRect(performance, "Label", 42, 6, inner - 42, 28, 16);
        WebRect(performance, "FocusRing", -3, -3, inner + 6, 42);
        foreach (var choices in new[] { modeChoices, sideChoices, difficultyChoices })
        foreach (MenuChoice choice in choices)
        {
            var element = choice.Button.GetComponent<UnityEngine.UI.LayoutElement>();
            element.minHeight = element.preferredHeight = 44;
            element.minWidth = 0;
            element.preferredWidth = choices == difficultyChoices ? inner / 3 : inner / 2;
            choice.Title.fontSize = choices == difficultyChoices ? 13 : 15;
            choice.Title.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 36);
            WebRect(choice.Button.transform, "SelectedMarker", 6, 14, 16, 16);
            var focus = (RectTransform)choice.FocusRing.transform;
            focus.anchorMin = Vector2.zero;
            focus.anchorMax = Vector2.one;
            focus.pivot = Vector2.one * .5f;
            focus.anchoredPosition = Vector2.zero;
            focus.sizeDelta = new Vector2(4, 4);
        }
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(menuContent);
    }

    private void LayoutWebDialogs(Vector2 size)
    {
        float width = Mathf.Min(560, size.x - 24), inner = width - 40;
        RectTransform promotion = WebCard(promotionPanel, "PromotionCard", width, 250);
        WebRect(promotion, "PromotionTitle", 20, 18, inner, 38, 26);
        WebRect(promotion, "PromotionHelp", 20, 64, inner, 42, 16);
        float half = (inner - 12) / 2;
        WebButton(promotion, "PromoteQueenButton", 20, 122, half, 44, 18);
        WebButton(promotion, "PromoteRookButton", 32 + half, 122, half, 44, 18);
        WebButton(promotion, "PromoteBishopButton", 20, 184, half, 44, 18);
        WebButton(promotion, "PromoteKnightButton", 32 + half, 184, half, 44, 18);
        RectTransform error = WebCard(computerErrorPanel, "ComputerErrorCard", width, 296);
        WebRect(error, "ComputerErrorTitle", 20, 18, inner, 62, 26);
        WebRect(error, "ComputerErrorHelp", 20, 84, inner, 82, 17);
        WebButton(error, "RetryComputerButton", 20, 180, inner, 44, 18);
        WebButton(error, "ComputerMenuButton", 20, 236, inner, 44, 18);
        bool shortView = size.y < 440;
        RectTransform result = WebCard(gameOverPanel, "GameOverCard", width, shortView ? 308 : 414);
        WebRect(result, "GameOverAccent", 0, 0, width, 6);
        WebRect(result, "GameOverKicker", 20, 20, inner, 24, 14);
        WebRect(result, "GameOverTitle", 20, 50, inner, 42, 29);
        WebRect(result, "GameOverMessage", 20, 100, inner, 76, 17);
        WebRect(result, "Divider", 20, 184, inner, 1);
        WebRect(result, "GameOverDetails", 20, 194, inner, 42, 14);
        WebButton(result, "PlayAgainButton", 20, 244, shortView ? half : inner, 44, 17);
        WebButton(result, "ReviewBoardButton", shortView ? 32 + half : 20, shortView ? 244 : 300, shortView ? half : inner, 44, 17);
        WebButton(result, "GameOverMenuButton", 20, shortView ? 190 : 356, inner, 44, 17);
        if (shortView)
        {
            WebRect(result, "GameOverMessage", 20, 96, inner, 52, 16);
            WebRect(result, "Divider", 20, 154, inner, 1);
            WebRect(result, "GameOverDetails", 20, 164, inner, 26, 13);
        }
        RectTransform history = WebCard(webHistoryOverlay, "MoveHistoryPanel", width, 320);
        WebRect(history, "MoveHistoryTitle", 20, 20, inner, 28, 18);
        WebRect(history, "MoveHistoryText", 20, 66, inner, 178, 18);
        WebButton(history, "CloseHistoryButton", 20, 256, inner, 44, 18);
    }

    private void LayoutWebPreview(Vector2 size)
    {
        bool sideBySide = WebTouchInput.ShortLandscape;
        float width = Mathf.Min(sideBySide ? 640 : 408, size.x - 24), inner = width - 32;
        bool shortView = size.y < 500;
        float height = shortView ? Mathf.Min(size.y - 16, 344) : Mathf.Min(size.y - 24, 558);
        selectedPiecePanel.anchorMin = selectedPiecePanel.anchorMax = selectedPiecePanel.pivot = Vector2.one * .5f;
        selectedPiecePanel.anchoredPosition = Vector2.zero;
        selectedPiecePanel.sizeDelta = new Vector2(width, height);
        WebRect(selectedPiecePanel, "SelectedPieceNameText", 16, 14, inner - 84, 52, 20);
        WebButton(selectedPiecePanel, "ClosePreviewButton", width - 92, 12, 76, 44, 16);
        WebRect(selectedPiecePanel, "SelectedPieceKindText", 16, 70, inner - 104, 26, 17);
        WebRect(selectedPiecePanel, "SelectedPieceSquareText", width - 120, 70, 104, 26, 15);
        float previewHeight = shortView ? Mathf.Max(50, height - 254) : Mathf.Min(inner * 220f / 368f, height - 306);
        float previewWidth = previewHeight * 368f / 220f;
        WebRect(selectedPiecePanel, "SelectedPiecePreview", (width - previewWidth) * .5f, 104, previewWidth, previewHeight);
        float controlsY = 112 + previewHeight;
        WebRect(selectedPiecePanel, "PreviewGestureHint", 16, controlsY, inner - 64, 24, 12);
        WebRect(selectedPiecePanel, "PreviewZoomText", width - 80, controlsY, 64, 24, 15);
        float quarter = (inner - 12) / 4;
        string[] row = { "PreviewRotateLeftButton", "PreviewRotateRightButton", "PreviewMoveUpButton", "PreviewMoveDownButton" };
        for (int i = 0; i < row.Length; i++) WebButton(selectedPiecePanel, row[i], 16 + i * (quarter + 4), controlsY + 28, quarter, 44, 12);
        float third = (inner - 8) / 3;
        WebButton(selectedPiecePanel, "PreviewZoomOutButton", 16, controlsY + 78, third, 44, 13);
        WebButton(selectedPiecePanel, "PreviewResetButton", 20 + third, controlsY + 78, third, 44, 13);
        WebButton(selectedPiecePanel, "PreviewZoomInButton", 24 + 2 * third, controlsY + 78, third, 44, 13);
        WebRect(selectedPiecePanel, "Divider", 16, controlsY + 134, inner, 1);
        WebRect(selectedPiecePanel, "SelectedPieceProfileText", 16, controlsY + 146, inner, 44, 14);
        selectedPieceProfileText.gameObject.SetActive(!shortView);
        selectedPiecePanel.Find("Divider").gameObject.SetActive(!shortView);
        if (sideBySide)
        {
            float imageHeight = Mathf.Min(height - 160, width * .44f * 220f / 368f);
            float imageWidth = imageHeight * 368f / 220f;
            float controlsX = imageWidth + 32, controlsWidth = width - controlsX - 16;
            float half = (controlsWidth - 6) / 2;
            WebRect(selectedPiecePanel, "SelectedPiecePreview", 16, 104, imageWidth, imageHeight);
            WebRect(selectedPiecePanel, "PreviewGestureHint", 16, height - 40, imageWidth, 24, 14);
            WebRect(selectedPiecePanel, "PreviewZoomText", controlsX, 264, controlsWidth, 24, 15);
            WebButton(selectedPiecePanel, "PreviewRotateLeftButton", controlsX, 104, half, 44, 14);
            WebButton(selectedPiecePanel, "PreviewRotateRightButton", controlsX + half + 6, 104, half, 44, 14);
            WebButton(selectedPiecePanel, "PreviewMoveUpButton", controlsX, 154, half, 44, 14);
            WebButton(selectedPiecePanel, "PreviewMoveDownButton", controlsX + half + 6, 154, half, 44, 14);
            float thirdWidth = (controlsWidth - 8) / 3;
            WebButton(selectedPiecePanel, "PreviewZoomOutButton", controlsX, 204, thirdWidth, 44, 13);
            WebButton(selectedPiecePanel, "PreviewResetButton", controlsX + thirdWidth + 4, 204, thirdWidth, 44, 13);
            WebButton(selectedPiecePanel, "PreviewZoomInButton", controlsX + 2 * (thirdWidth + 4), 204, thirdWidth, 44, 13);
        }
    }

    private void BuildWebHelpDialog()
    {
        howToPlayPanel = ModalOverlay("HowToPlayPanel");
        RectTransform card = ModalCard("HelpCard", howToPlayPanel, 336, 460);
        MenuLabel("HelpTitle", card, "Como jogar", 27, textColor, 20, 20, 296, 40, true);
        MenuLabel("HelpText", card,
            "Toque na peça e depois na casa destacada.\n\nDois dedos giram e aproximam a câmera.\n\nPeça abre o personagem: arraste para girar e use os botões para zoom.\n\nLances abre o histórico; Menu troca a partida.",
            17, textColor, 20, 76, 296, 300);
        MenuButton("CloseHelpButton", card, "Entendi", 20, 396, 296, 44, actionColor, ToggleHowToPlay);
    }

    private void LayoutWebHelp(Vector2 size)
    {
        if (howToPlayPanel == null) return;
        float width = Mathf.Min(560, size.x - 24);
        float height = Mathf.Min(460, size.y - 24);
        RectTransform card = WebCard(howToPlayPanel, "HelpCard", width, height);
        WebRect(card, "HelpTitle", 20, 20, width - 40, 40, 27);
        WebRect(card, "HelpText", 20, 72, width - 40, height - 150, size.y < 400 ? 15 : 17);
        WebButton(card, "CloseHelpButton", 20, height - 64, width - 40, 44, 18);
    }

    private static RectTransform WebCard(RectTransform overlay, string name, float width, float height)
    {
        var card = (RectTransform)overlay.Find(name);
        card.sizeDelta = new Vector2(width, height);
        return card;
    }

    private static RectTransform WebRect(Transform parent, string name, float x, float y, float width, float height, int fontSize = 0)
    {
        Transform target = parent.Find(name);
        if (target == null) return null;
        var rect = (RectTransform)target;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(x, -y);
        rect.sizeDelta = new Vector2(width, height);
        var text = target.GetComponent<UnityEngine.UI.Text>();
        if (text != null && fontSize > 0) text.fontSize = fontSize;
        return rect;
    }

    private static void WebButton(Transform parent, string name, float x, float y, float width, float height, int fontSize)
    {
        RectTransform rect = WebRect(parent, name, x, y, width, height);
        if (rect == null) return;
        var label = rect.GetComponentInChildren<UnityEngine.UI.Text>();
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.pivot = Vector2.one * .5f;
        label.rectTransform.anchoredPosition = Vector2.zero;
        label.rectTransform.sizeDelta = new Vector2(-4, -4);
        label.fontSize = fontSize;
    }
}
