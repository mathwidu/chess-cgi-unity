using UnityEngine;
using UnityEngine.UI;

public sealed partial class GameHud
{
    private RectTransform matchInterface;
    private Text matchSummaryText;

    private void BuildMatchInterface()
    {
        matchInterface = CreateRect("MatchInterface", hudRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Vector2.zero);
        matchControls = matchInterface.gameObject.AddComponent<CanvasGroup>();
        BuildMatchHeader();
        BuildTurnAndHistory();
        BuildSelectedPieceDetails();
        BuildMatchActions();
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
        selectedPiecePreviewImage = CreateRawImage("SelectedPiecePreview", selectedPiecePanel, new Vector2(24, -24), new Vector2(360, 232), Color.white);
        selectedPiecePreviewInput = selectedPiecePreviewImage.gameObject.AddComponent<SelectedPiecePreviewInput>();
        MenuButton("PreviewZoomOutButton", selectedPiecePanel, "−", 274, 206, 48, 40, neutralButtonColor, ZoomSelectedPiecePreviewOut);
        MenuButton("PreviewZoomInButton", selectedPiecePanel, "+", 328, 206, 48, 40, actionColor, ZoomSelectedPiecePreviewIn);
        selectedPieceNameText = MenuLabel("SelectedPieceNameText", selectedPiecePanel, "", 24, textColor, 24, 270, 360, 58, true);
        selectedPieceKindText = MenuLabel("SelectedPieceKindText", selectedPiecePanel, "", 19, accentColor, 24, 330, 360, 28, true);
        selectedPieceSquareText = MenuLabel("SelectedPieceSquareText", selectedPiecePanel, "", 17, mutedTextColor, 24, 368, 170, 26);
        selectedPieceSideText = MenuLabel("SelectedPieceSideText", selectedPiecePanel, "", 17, mutedTextColor, 210, 368, 174, 26);
        selectedPieceProfileText = MenuLabel("SelectedPieceProfileText", selectedPiecePanel, "", 17, textColor, 24, 406, 360, 116);
        // The name, role and record already identify the person; keep the redundant long bio out of the compact HUD.
        selectedPieceDescriptionText = null;
        EnsureSelectedPiecePreviewResources();
        selectedPiecePreviewImage.texture = selectedPiecePreviewTexture;
        selectedPiecePreviewInput.Configure(null, selectedPiecePreviewCamera);
    }

    private void BuildMatchActions()
    {
        RectTransform actions = CreatePanel("ActionBar", matchInterface, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(24, 24), new Vector2(708, 82), panelColor);
        MenuButton("NewGameButton", actions, "Nova partida", 16, 16, 194, 50, actionColor, RestartGame);
        MenuButton("CancelButton", actions, "Cancelar", 222, 16, 148, 50, neutralButtonColor, CancelSelection);
        howToPlayButtonText = MenuButton("HowToPlayButton", actions, "Como jogar", 382, 16, 166, 50, neutralButtonColor, ToggleHowToPlay).GetComponentInChildren<Text>();
        MenuButton("MenuButton", actions, "Menu", 560, 16, 132, 50, neutralButtonColor, ShowMenu);
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
