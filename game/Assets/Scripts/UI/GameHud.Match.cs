using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class GameHud
{
    // Let the final move land on the board before the result covers it.
    private const float ResultRevealDelay = 0.8f;

    private RectTransform matchInterface;
    private Text matchSummaryText;
    private Text cancelButtonText;
    private RectTransform gameOverPanel;
    private Image gameOverAccent;
    private Text gameOverKicker;
    private Text gameOverTitle;
    private Text gameOverMessage;
    private Text gameOverDetails;
    private bool resultDismissed;
    private float resultRevealAt = -1f;

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
        BuildGameOverDialog();
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
        cancelButtonText = MenuButton("CancelButton", actions, "Cancelar", 222, 16, 148, 50, neutralButtonColor, CancelSelection).GetComponentInChildren<Text>();
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

    private void BuildGameOverDialog()
    {
        gameOverPanel = ModalOverlay("GameOverPanel");
        RectTransform result = ModalCard("GameOverCard", gameOverPanel, 800, 372);
        gameOverAccent = MenuRect("GameOverAccent", result, 0, 0, 800, 8).gameObject.AddComponent<Image>();
        gameOverAccent.raycastTarget = false;
        gameOverKicker = MenuLabel("GameOverKicker", result, "", 16, accentColor, 36, 40, 728, 24, true);
        gameOverTitle = MenuLabel("GameOverTitle", result, "", 50, textColor, 36, 70, 728, 66, true);
        gameOverMessage = MenuLabel("GameOverMessage", result, "", 22, mutedTextColor, 36, 146, 728, 60);
        MenuRule(result, 36, 222, 728);
        gameOverDetails = MenuLabel("GameOverDetails", result, "", 18, mutedTextColor, 36, 240, 728, 28);
        MenuButton("PlayAgainButton", result, "Jogar novamente", 36, 290, 280, 56, actionColor, RestartGame);
        MenuButton("ReviewBoardButton", result, "Ver tabuleiro", 326, 290, 214, 56, neutralButtonColor, HideResult);
        MenuButton("GameOverMenuButton", result, "Voltar ao menu", 550, 290, 214, 56, neutralButtonColor, ShowMenu);
    }

    // The result follows the controller; the player may hide it to study the final position.
    private void RefreshGameOverDialog()
    {
        bool finished = gameController != null && gameController.IsGameOver && !showStartScreen;
        if (!finished)
        {
            resultDismissed = false;
            resultRevealAt = -1f;
            SetActive(gameOverPanel, false);
            return;
        }

        if (resultRevealAt < 0f)
        {
            resultRevealAt = Time.unscaledTime + ResultRevealDelay;
        }

        SetActive(gameOverPanel, !resultDismissed && Time.unscaledTime >= resultRevealAt);
        bool playerWon = gameController.Outcome == MatchOutcome.Checkmate &&
            (!gameController.IsAgainstComputer || gameController.Winner == gameController.HumanSide);
        gameOverAccent.color = playerWon ? accentColor : mutedTextColor;
        gameOverKicker.color = playerWon ? accentColor : mutedTextColor;
        gameOverKicker.text = OutcomeName(gameController.Outcome);
        gameOverTitle.text = ResultHeadline();
        gameOverMessage.text = ResultMessage();
        gameOverDetails.text = ResultDetails(gameController.MoveHistory);
    }

    private bool IsResultShown => gameOverPanel != null && gameOverPanel.gameObject.activeSelf;

    private void HideResult()
    {
        resultDismissed = true;
        RefreshInterface();
    }

    private void ShowResult()
    {
        resultDismissed = false;
        RefreshInterface();
    }

    private string ResultHeadline()
    {
        if (gameController.Outcome != MatchOutcome.Checkmate || !gameController.Winner.HasValue)
        {
            return "Empate";
        }

        ChessSide winner = gameController.Winner.Value;
        if (gameController.IsAgainstComputer)
        {
            return winner == gameController.HumanSide ? "Você venceu!" : "A IA venceu";
        }

        return $"{SideName(winner)} vencem";
    }

    private string ResultMessage()
    {
        switch (gameController.Outcome)
        {
            case MatchOutcome.Checkmate:
                ChessSide winner = gameController.Winner.GetValueOrDefault();
                if (!gameController.IsAgainstComputer)
                {
                    return $"As {SideName(winner).ToLowerInvariant()} deram xeque-mate. Boa partida!";
                }

                return winner == gameController.HumanSide
                    ? $"Seu xeque-mate derrotou a IA no nível {DifficultyName(gameController.Difficulty)}."
                    : "A IA encontrou o xeque-mate. Jogue de novo ou escolha outra dificuldade no menu.";
            case MatchOutcome.Stalemate:
                return "O lado a jogar não está em xeque, mas não tem nenhum lance legal.";
            case MatchOutcome.InsufficientMaterial:
                return "Não restam peças suficientes para nenhum lado dar xeque-mate.";
            default:
                return "As regras reconhecem esta posição como empate.";
        }
    }

    private static string ResultDetails(IReadOnlyList<string> moveHistory)
    {
        if (moveHistory.Count == 0)
        {
            return string.Empty;
        }

        string finalMove = moveHistory[moveHistory.Count - 1].Replace(": ", " ");
        string count = moveHistory.Count == 1 ? "1 lance" : $"{moveHistory.Count} lances";
        return $"{count}  /  Lance final: {finalMove}";
    }

    private static string OutcomeName(MatchOutcome outcome)
    {
        switch (outcome)
        {
            case MatchOutcome.Checkmate:
                return "XEQUE-MATE";
            case MatchOutcome.Stalemate:
                return "EMPATE POR AFOGAMENTO";
            case MatchOutcome.InsufficientMaterial:
                return "EMPATE POR MATERIAL INSUFICIENTE";
            default:
                return "EMPATE";
        }
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
