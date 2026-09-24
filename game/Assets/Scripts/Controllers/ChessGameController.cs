using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class ChessGameController : MonoBehaviour
{
    [SerializeField] private BoardView boardView;
    [SerializeField] private PieceFactory pieceFactory;
    [SerializeField] private GameHud hud;
    [SerializeField] private CameraController cameraController;
    [SerializeField] private float moveDuration = 0.28f;

    private const float ReturnDuration = 0.15f;
    private const string PerformanceModeKey = "ChessCgi.PerformanceMode";

    private readonly ChessRulesAdapter rules = new ChessRulesAdapter();
    private readonly List<BoardSquare> legalDestinations = new List<BoardSquare>();
    private readonly List<string> moveHistory = new List<string>();

    private PieceView selectedPiece;
    private bool inputBlocked;
    private bool gameOver;
    private bool awaitingPromotion;
    private BoardSquare pendingPromotionTo;
    private bool againstComputer;
    private bool matchStarted;
    private bool suspended;
    private MoveResult? animatingMove;
    private ComputerTurnCoordinator computerTurn;
    private System.Func<IMoveChooser> moveChooserFactory = ComputerOpponentFactory.Create;

    public bool IsAgainstComputer => againstComputer;
    public ChessSide HumanSide { get; private set; } = ChessSide.White;
    public ComputerDifficulty Difficulty { get; private set; } = ComputerDifficulty.Beginner;
    public bool IsComputerTurn => againstComputer && CurrentTurn != HumanSide && !gameOver;
    public bool IsComputerThinking => computerTurn != null && computerTurn.IsThinking;
    public bool HasComputerError { get; private set; }
    public bool IsGameOver => gameOver;
    public bool IsMenuOpen => !matchStarted;

    public PieceView SelectedPiece => selectedPiece;
    public bool IsInputBlocked => !isActiveAndEnabled || !matchStarted || suspended || inputBlocked || awaitingPromotion || IsComputerTurn;
    public bool IsAwaitingPromotion => awaitingPromotion;
    public ChessSide CurrentTurn => rules.CurrentTurn;
    public bool PerformanceMode => pieceFactory != null && pieceFactory.UsePrimitivePieces;
    public IReadOnlyList<string> MoveHistory => moveHistory;
    public string StatusMessage { get; private set; } = "Turno: Brancas";

    public void Configure(BoardView board, PieceFactory factory, GameHud gameHud, CameraController camera = null)
    {
        boardView = board;
        pieceFactory = factory;
        hud = gameHud;
        cameraController = camera;

        if (hud != null)
        {
            hud.Configure(this);
        }
    }

    private void Awake()
    {
        if (boardView == null)
        {
            boardView = Object.FindFirstObjectByType<BoardView>();
        }

        if (pieceFactory == null)
        {
            pieceFactory = Object.FindFirstObjectByType<PieceFactory>();
        }

        if (pieceFactory != null)
        {
            pieceFactory.UsePrimitivePieces = PlayerPrefs.GetInt(PerformanceModeKey, 1) == 1;
        }

        if (hud == null)
        {
            hud = Object.FindFirstObjectByType<GameHud>();
        }

        if (cameraController == null)
        {
            cameraController = Object.FindFirstObjectByType<CameraController>();
        }

        if (hud != null)
        {
            hud.Configure(this);
        }
    }

    private void Start()
    {
        if (!matchStarted)
        {
            StartLocalGame();
            if (hud != null)
            {
                ReturnToMenu();
            }
        }
    }

    private void Update()
    {
        if (!matchStarted || suspended || !IsComputerTurn || inputBlocked || HasComputerError)
        {
            return;
        }
        if (computerTurn == null || !computerTurn.IsThinking)
        {
            BeginComputerTurn();
        }
        if (computerTurn == null || !computerTurn.TryTakeResult(rules.GetSnapshot(), out ComputerTurnResult result))
        {
            return;
        }
        if (!result.Success)
        {
            FailComputerTurn(result.Error);
            return;
        }
        if (!ExecuteMove(result.Move))
        {
            FailComputerTurn("A IA devolveu uma jogada ilegal. Tente novamente.");
        }
    }

    // Injection point for another platform adapter and deterministic gameplay tests.
    public void SetMoveChooserFactory(System.Func<IMoveChooser> factory)
    {
        StopComputerTurn();
        moveChooserFactory = factory ?? throw new System.ArgumentNullException(nameof(factory));
    }

    public void StartLocalGame()
    {
        againstComputer = false;
        NewGame();
    }

    public void StartComputerGame(ChessSide humanSide, ComputerDifficulty difficulty)
    {
        HumanSide = humanSide;
        Difficulty = difficulty;
        againstComputer = true;
        NewGame();
    }

    public void NewGame()
    {
        StopAllCoroutines();
        StopComputerTurn();
        rules.Reset();
        animatingMove = null;
        matchStarted = true;
        gameOver = false;
        inputBlocked = false;
        awaitingPromotion = false;
        selectedPiece = null;
        legalDestinations.Clear();
        moveHistory.Clear();

        boardView.BuildBoard();
        boardView.SyncPieces(rules.GetPieces(), pieceFactory);
        SetStatusForTurn();
        UpdateCameraForTurn(true);
    }

    public void SetPerformanceMode(bool enabled)
    {
        if (pieceFactory == null)
        {
            return;
        }

        pieceFactory.UsePrimitivePieces = enabled;
        PlayerPrefs.SetInt(PerformanceModeKey, enabled ? 1 : 0);

        if (boardView != null && boardView.Pieces.Count > 0)
        {
            ClearSelection();
            boardView.SyncPieces(rules.GetPieces(), pieceFactory);
        }
    }

    public void ReturnToMenu()
    {
        StopAllCoroutines();
        StopComputerTurn();
        animatingMove = null;
        matchStarted = false;
        inputBlocked = false;
        awaitingPromotion = false;
        ClearSelection();
        boardView.SyncPieces(rules.GetPieces(), pieceFactory);
    }

    public void RetryComputerTurn()
    {
        if (!IsComputerTurn || !HasComputerError)
        {
            return;
        }
        StopComputerTurn();
        SetStatusForTurn();
    }

    private void BeginComputerTurn()
    {
        try
        {
            if (computerTurn == null)
            {
                computerTurn = new ComputerTurnCoordinator(moveChooserFactory());
            }
            computerTurn.Begin(rules.GetSnapshot(), MoveSearchSettings.ForDifficulty(Difficulty));
            StatusMessage = "IA pensando...";
        }
        catch (System.Exception exception)
        {
            FailComputerTurn(exception.Message);
        }
    }

    private void FailComputerTurn(string detail)
    {
        StopComputerTurn();
        HasComputerError = true;
        StatusMessage = "IA indisponivel. Tente novamente ou volte ao menu.";
        Debug.LogWarning("CHESS_AI_FAILURE " + detail);
    }

    private void StopComputerTurn()
    {
        computerTurn?.Dispose();
        computerTurn = null;
        HasComputerError = false;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        StopComputerTurn();
        inputBlocked = false;
        if (matchStarted && boardView != null && pieceFactory != null)
        {
            ClearSelection();
            awaitingPromotion = false;
            boardView.SyncPieces(rules.GetPieces(), pieceFactory);
            if (animatingMove.HasValue)
            {
                ApplyMoveResult(animatingMove.Value);
            }
            animatingMove = null;
        }
    }

    private void OnEnable()
    {
        if (matchStarted && !gameOver)
        {
            SetStatusForTurn();
        }
    }

    private void OnApplicationPause(bool paused)
    {
        suspended = paused;
        if (paused)
        {
            StopComputerTurn();
        }
        else if (matchStarted && !gameOver)
        {
            SetStatusForTurn();
        }
    }

    private void OnDestroy() => StopComputerTurn();

    public void SelectPiece(PieceView piece)
    {
        if (piece == null || IsInputBlocked || gameOver)
        {
            return;
        }

        if (piece.Side != CurrentTurn)
        {
            if (selectedPiece != null && legalDestinations.Contains(piece.Square))
            {
                SelectDestination(piece.Square);
            }
            else
            {
                StatusMessage = $"Turno: {SideName(CurrentTurn)}";
            }

            return;
        }

        SelectOwnPiece(piece);
    }

    public bool CanGrabPiece(PieceView piece)
    {
        return piece != null && !IsInputBlocked && !gameOver && piece.Side == CurrentTurn;
    }

    public void GrabPiece(PieceView piece)
    {
        if (CanGrabPiece(piece))
        {
            SelectOwnPiece(piece);
        }
    }

    public void ReleasePiece(PieceView piece, Vector3 worldPosition)
    {
        if (piece == null)
        {
            return;
        }

        if (piece != selectedPiece)
        {
            ReturnToSquare(piece);
            return;
        }

        bool onBoard = boardView.TryGetSquareAt(worldPosition, out BoardSquare destination);
        if (onBoard && destination.Equals(piece.Square))
        {
            CancelSelection();
            ReturnToSquare(piece);
            return;
        }

        if (!onBoard || !legalDestinations.Contains(destination))
        {
            ClearSelection();
            StatusMessage = "Movimento invalido.";
            ReturnToSquare(piece);
            return;
        }

        SelectDestination(destination);
        if (awaitingPromotion)
        {
            piece.StartCoroutine(piece.MoveTo(boardView.GetPieceWorldPosition(destination), ReturnDuration));
        }
    }

    public void SelectSquare(SquareView square)
    {
        if (square == null)
        {
            return;
        }

        SelectDestination(square.Square);
    }

    public void SelectDestination(BoardSquare destination)
    {
        if (selectedPiece == null || IsInputBlocked || gameOver)
        {
            return;
        }

        if (!legalDestinations.Contains(destination))
        {
            StatusMessage = "Movimento invalido.";
            return;
        }

        if (RequiresPromotion(selectedPiece, destination))
        {
            pendingPromotionTo = destination;
            awaitingPromotion = true;
            StatusMessage = "Escolha a promocao.";
            return;
        }

        ExecuteSelectedMove(destination, null);
    }

    public void ChoosePromotion(char promotion)
    {
        if (!awaitingPromotion || selectedPiece == null || "QRBN".IndexOf(char.ToUpperInvariant(promotion)) < 0)
        {
            return;
        }

        awaitingPromotion = false;
        ExecuteSelectedMove(pendingPromotionTo, char.ToUpperInvariant(promotion));
    }

    public void CancelSelection()
    {
        if (!isActiveAndEnabled || !matchStarted || suspended || inputBlocked || IsComputerTurn || gameOver)
        {
            return;
        }
        awaitingPromotion = false;
        ClearSelection();
        SetStatusForTurn();
    }

    private void ReturnToSquare(PieceView piece)
    {
        piece.StartCoroutine(piece.MoveTo(boardView.GetPieceWorldPosition(piece.Square), ReturnDuration));
    }

    private void SelectOwnPiece(PieceView piece)
    {
        ClearSelection();
        selectedPiece = piece;
        selectedPiece.SetSelected(true);
        legalDestinations.Clear();
        legalDestinations.AddRange(rules.GetLegalDestinations(piece.Square));
        boardView.HighlightSquares(legalDestinations);

        if (legalDestinations.Count == 0)
        {
            StatusMessage = "Sem movimentos legais.";
        }
        else
        {
            StatusMessage = $"{SideName(CurrentTurn)}: escolha o destino.";
        }
    }

    private void ExecuteSelectedMove(BoardSquare destination, char? promotion)
    {
        ExecuteMove(new ChessMove(selectedPiece.Square, destination, promotion));
    }

    private bool ExecuteMove(ChessMove move)
    {
        PieceView movingPiece = null;
        foreach (PieceView piece in boardView.Pieces)
        {
            if (piece.Square.Equals(move.From))
            {
                movingPiece = piece;
                break;
            }
        }
        if (movingPiece == null)
        {
            return false;
        }
        MoveResult moveResult = rules.TryMove(move);

        if (!moveResult.Success)
        {
            StatusMessage = moveResult.Message;
            return false;
        }

        string moveNotation = BuildMoveNotation(movingPiece, move.From, move.To, moveResult, move.Promotion);
        moveHistory.Add(moveNotation);
        ClearSelection();

        if (Application.isPlaying && moveDuration > 0f)
        {
            animatingMove = moveResult;
            StartCoroutine(AnimateMoveThenSync(movingPiece, move.To, moveResult));
        }
        else
        {
            boardView.SyncPieces(rules.GetPieces(), pieceFactory);
            ApplyMoveResult(moveResult);
        }
        return true;
    }

    private IEnumerator AnimateMoveThenSync(PieceView movingPiece, BoardSquare destination, MoveResult moveResult)
    {
        inputBlocked = true;
        yield return movingPiece.MoveTo(boardView.GetPieceWorldPosition(destination), moveDuration);
        boardView.SyncPieces(rules.GetPieces(), pieceFactory);
        inputBlocked = false;
        animatingMove = null;
        ApplyMoveResult(moveResult);
    }

    private void ApplyMoveResult(MoveResult moveResult)
    {
        // The provider survives human turns, but each completed search is consumed once.
        if (moveResult.IsCheckmate)
        {
            gameOver = true;
            StopComputerTurn();
            ChessSide winner = CurrentTurn == ChessSide.White ? ChessSide.Black : ChessSide.White;
            StatusMessage = $"Xeque-mate. {SideName(winner)} vencem.";
            UpdateCameraForTurn(false);
            return;
        }

        if (moveResult.IsDraw)
        {
            gameOver = true;
            StopComputerTurn();
            StatusMessage = "Empate.";
            UpdateCameraForTurn(false);
            return;
        }

        if (moveResult.IsCheck)
        {
            StatusMessage = $"Xeque. Turno: {SideName(CurrentTurn)}";
            UpdateCameraForTurn(false);
            return;
        }

        SetStatusForTurn();
        UpdateCameraForTurn(false);
    }

    private void ClearSelection()
    {
        if (selectedPiece != null)
        {
            selectedPiece.SetSelected(false);
        }

        selectedPiece = null;
        legalDestinations.Clear();
        boardView.ClearHighlights();
    }

    private void SetStatusForTurn()
    {
        StatusMessage = IsComputerTurn ? "IA pensando..." : $"Turno: {SideName(CurrentTurn)}";
    }

    private void UpdateCameraForTurn(bool instant)
    {
        if (cameraController != null && !XRRig.IsHeadsetPresent)
        {
            cameraController.SetPerspective(againstComputer ? HumanSide : CurrentTurn, instant);
        }
    }

    private static bool RequiresPromotion(PieceView piece, BoardSquare destination)
    {
        return piece.Kind == ChessPieceKind.Pawn &&
            ((piece.Side == ChessSide.White && destination.Rank == 8) ||
             (piece.Side == ChessSide.Black && destination.Rank == 1));
    }

    private static string BuildMoveNotation(
        PieceView movingPiece,
        BoardSquare origin,
        BoardSquare destination,
        MoveResult moveResult,
        char? promotion)
    {
        string separator = moveResult.IsCapture ? "x" : "-";
        string promotionSuffix = promotion.HasValue ? $"={char.ToUpperInvariant(promotion.Value)}" : string.Empty;
        string stateSuffix = moveResult.IsCheckmate ? "#" : moveResult.IsCheck ? "+" : string.Empty;
        return $"{SideName(movingPiece.Side)}: {origin.ToAlgebraic()}{separator}{destination.ToAlgebraic()}{promotionSuffix}{stateSuffix}";
    }

    private static string SideName(ChessSide side)
    {
        return side == ChessSide.White ? "Brancas" : "Pretas";
    }
}
