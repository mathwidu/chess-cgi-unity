using UnityEngine;
using UnityEngine.UI;

public sealed partial class GameHud
{
    private RectTransform matchInterface;
    private Text matchSummaryText;
    private RectTransform roomViewPanel;
    private UnityEngine.UI.Text roomViewButtonText;
    private UnityEngine.UI.Text roomViewHint;
    private CameraController desktopView;
    private UnityEngine.UI.Text previewZoomText;
    private UnityEngine.UI.Button previewZoomInButton, previewZoomOutButton;

    private void BuildMatchInterface()
    {
        matchInterface = CreateRect("MatchInterface", hudRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Vector2.zero);
        matchControls = matchInterface.gameObject.AddComponent<CanvasGroup>();
        BuildMatchHeader();
        BuildTurnAndHistory();
        BuildSelectedPieceDetails();
        BuildMatchActions();
        BuildRoomViewControls();
        BuildPromotionDialog();
        BuildComputerErrorDialog();
    }

    private void BuildMatchHeader()
    {
        RectTransform brand = CreatePanel("BrandPanel", matchInterface, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -24), new Vector2(650, 94), panelColor);
        MenuLabel("TitleText", brand, "XADREZ / CGI", 27, textColor, 24, 16, 596, 34, true);
        matchSummaryText = MenuLabel("MatchDetails", brand, "", 16, accentColor, 24, 60, 602, 26);
    }

    private void BuildTurnAndHistory()
    {
        RectTransform turn = CreatePanel("TurnPanel", matchInterface, Vector2.one, Vector2.one, Vector2.one, new Vector2(-24, -24), new Vector2(408, 108), panelColor);
        turnText = MenuLabel("TurnText", turn, "Brancas jogam", 26, accentColor, 24, 16, 360, 35, true);
        statusText = MenuLabel("StatusText", turn, "Escolha uma peça para mover.", 18, textColor, 24, 58, 360, 46);
        RectTransform history = CreatePanel("MoveHistoryPanel", matchInterface, Vector2.one, Vector2.one, Vector2.one, new Vector2(-24, -148), new Vector2(408, 240), panelColor);
        MenuLabel("MoveHistoryTitle", history, "ÚLTIMOS LANCES", 15, mutedTextColor, 24, 20, 360, 24, true);
        MenuRule(history, 24, 56, 360);
        moveHistoryText = MenuLabel("MoveHistoryText", history, "Seu primeiro lance começa a história.", 19, textColor, 24, 76, 360, 158);
    }

    private void BuildSelectedPieceDetails()
    {
        selectedPiecePanel = CreatePanel("SelectedPiecePanel", matchInterface, Vector2.one, Vector2.one, Vector2.one, new Vector2(-24, -404), new Vector2(408, 534), panelStrongColor);
        selectedPieceNameText = MenuLabel("SelectedPieceNameText", selectedPiecePanel, "", 23, textColor, 20, 16, 368, 54, true);
        selectedPieceKindText = MenuLabel("SelectedPieceKindText", selectedPiecePanel, "", 18, accentColor, 20, 72, 236, 26, true);
        selectedPieceSquareText = MenuLabel("SelectedPieceSquareText", selectedPiecePanel, "", 16, mutedTextColor, 268, 74, 120, 24);
        selectedPieceSquareText.alignment = TextAnchor.UpperRight;
        selectedPieceSideText = null; // The team is already part of the piece label.
        selectedPiecePreviewImage = CreateRawImage("SelectedPiecePreview", selectedPiecePanel, new Vector2(20, -104), new Vector2(368, 220), Color.white);
        selectedPiecePreviewInput = selectedPiecePreviewImage.gameObject.AddComponent<SelectedPiecePreviewInput>();
        var hint = MenuLabel("PreviewGestureHint", selectedPiecePanel,
            XRRig.IsHeadsetPresent ? "Use os botões para ajustar a vista" : "Esquerdo: girar · Direito: mover",
            14, mutedTextColor, 20, 332, 296, 24);
        hint.alignment = TextAnchor.MiddleLeft;
        previewZoomText = MenuLabel("PreviewZoomText", selectedPiecePanel, "100%", 17, textColor, 328, 332, 60, 24, true);
        previewZoomText.alignment = TextAnchor.MiddleRight;
        PreviewButton("PreviewRotateLeftButton", "← Girar", 20, 368, 82, () => selectedPiecePreviewInput.RotatePreview(30f));
        PreviewButton("PreviewRotateRightButton", "Girar →", 110, 368, 82, () => selectedPiecePreviewInput.RotatePreview(-30f));
        PreviewButton("PreviewMoveUpButton", "Mover ↑", 200, 368, 90, () => selectedPiecePreviewInput.PanPreview(Vector2.up * .12f));
        PreviewButton("PreviewMoveDownButton", "Mover ↓", 298, 368, 90, () => selectedPiecePreviewInput.PanPreview(Vector2.down * .12f));
        previewZoomOutButton = PreviewButton("PreviewZoomOutButton", "− Afastar", 20, 420, 112, ZoomSelectedPiecePreviewOut);
        PreviewButton("PreviewResetButton", "Restaurar", 148, 420, 112, () => selectedPiecePreviewInput.ResetView());
        previewZoomInButton = PreviewButton("PreviewZoomInButton", "+ Zoom", 276, 420, 112, ZoomSelectedPiecePreviewIn);
        MenuRule(selectedPiecePanel, 20, 474, 368);
        selectedPieceProfileText = MenuLabel("SelectedPieceProfileText", selectedPiecePanel, "", 14, mutedTextColor, 20, 486, 368, 40);
        selectedPieceDescriptionText = null;
        EnsureSelectedPiecePreviewResources();
        selectedPiecePreviewImage.texture = selectedPiecePreviewTexture;
        selectedPiecePreviewInput.Configure(null, selectedPiecePreviewCamera);
    }

    private UnityEngine.UI.Button PreviewButton(string name, string label, float x, float y, float width, UnityEngine.Events.UnityAction action)
    {
        var button = MenuButton(name, selectedPiecePanel, label, x, y, width, 42, neutralButtonColor, action);
        button.GetComponentInChildren<UnityEngine.UI.Text>().fontSize = 16;
        return button;
    }

    private void BuildMatchActions()
    {
        RectTransform actions = CreatePanel("ActionBar", matchInterface, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(24, 24), new Vector2(708, 82), panelColor);
        MenuButton("NewGameButton", actions, "Nova partida", 16, 16, 194, 50, actionColor, RestartGame);
        MenuButton("CancelButton", actions, "Cancelar", 222, 16, 148, 50, neutralButtonColor, CancelSelection);
        howToPlayButtonText = MenuButton("HowToPlayButton", actions, "Como jogar", 382, 16, 166, 50, neutralButtonColor, ToggleHowToPlay).GetComponentInChildren<Text>();
        MenuButton("MenuButton", actions, "Menu", 560, 16, 132, 50, neutralButtonColor, ShowMenu);
    }

    private void BuildRoomViewControls()
    {
        if (XRRig.IsHeadsetPresent) return;
        desktopView = Object.FindFirstObjectByType<CameraController>();
        roomViewPanel = CreatePanel("RoomViewPanel", matchInterface, new Vector2(1, 0), new Vector2(1, 0),
            new Vector2(1, 0), new Vector2(-24, 24), new Vector2(408, 100), panelColor);
        roomViewButtonText = MenuButton("RoomViewButton", roomViewPanel, "Olhar ao redor", 16, 12, 376, 42,
            neutralButtonColor, () =>
            {
                if (desktopView == null) return;
                if (desktopView.IsLookingAround) desktopView.ReturnToBoard();
                else desktopView.LookAround();
            }).GetComponentInChildren<UnityEngine.UI.Text>();
        roomViewHint = MenuLabel("RoomViewHint", roomViewPanel, "", 15, mutedTextColor, 16, 64, 376, 22);
    }

    private void RefreshRoomViewControls()
    {
        if (roomViewPanel == null) return;
        SetActive(roomViewPanel, !XRRig.IsHeadsetPresent);
        bool looking = desktopView != null && desktopView.IsLookingAround;
        roomViewButtonText.text = looking ? "Voltar ao tabuleiro  ·  R" : "Olhar ao redor";
        roomViewHint.text = looking ? "Arraste com o botão direito para olhar" : "Q/E: girar  ·  Scroll: zoom  ·  R: voltar";
    }

    private void BuildPromotionDialog()
    {
        promotionPanel = ModalOverlay("PromotionPanel");
        RectTransform promotion = ModalCard("PromotionCard", promotionPanel, 760, 244);
        MenuLabel("PromotionTitle", promotion, "Promova seu peão", 34, textColor, 36, 32, 688, 44, true);
        MenuLabel("PromotionHelp", promotion, "Escolha a peça que vai continuar a partida.", 21, mutedTextColor, 36, 92, 688, 36);
        MenuButton("PromoteQueenButton", promotion, "Rainha", 36, 152, 163, 56, actionColor, () => ChoosePromotion('Q'));
        MenuButton("PromoteRookButton", promotion, "Torre", 211, 152, 163, 56, neutralButtonColor, () => ChoosePromotion('R'));
        MenuButton("PromoteBishopButton", promotion, "Bispo", 386, 152, 163, 56, neutralButtonColor, () => ChoosePromotion('B'));
        MenuButton("PromoteKnightButton", promotion, "Cavalo", 561, 152, 163, 56, neutralButtonColor, () => ChoosePromotion('N'));
    }

    private void BuildComputerErrorDialog()
    {
        computerErrorPanel = ModalOverlay("ComputerErrorPanel");
        RectTransform error = ModalCard("ComputerErrorCard", computerErrorPanel, 800, 266);
        MenuLabel("ComputerErrorTitle", error, "A IA não conseguiu jogar", 32, textColor, 36, 34, 728, 46, true);
        MenuLabel("ComputerErrorHelp", error, "Sua partida foi preservada. Tente novamente\nou volte ao menu para começar outra partida.", 22, mutedTextColor, 36, 100, 728, 64);
        MenuButton("RetryComputerButton", error, "Tentar novamente", 36, 186, 358, 54, actionColor, () => gameController.RetryComputerTurn());
        MenuButton("ComputerMenuButton", error, "Voltar ao menu", 410, 186, 354, 54, neutralButtonColor, ShowMenu);
    }

    private RectTransform ModalOverlay(string name)
    {
        return CreatePanel(name, hudRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Vector2.zero, new Color(0.01f, 0.02f, 0.025f, 0.88f));
    }

    private RectTransform ModalCard(string name, Transform parent, float width, float height)
    {
        return CreatePanel(name, parent, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, new Vector2(width, height), panelColor);
    }
}
