using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class ChessGameController : MonoBehaviour
{
    [SerializeField] private BoardView boardView;
    [SerializeField] private PieceFactory pieceFactory;
    [SerializeField] private GameHud hud;
    [SerializeField] private CameraController cameraController;
    [SerializeField] private PieceMotionController motionController;
    [SerializeField] private float moveDuration = 0.28f;

    private readonly ChessRulesAdapter rules = new ChessRulesAdapter();
    private readonly List<BoardSquare> legalDestinations = new List<BoardSquare>();
    private readonly List<string> moveHistory = new List<string>();

    private PieceView selectedPiece;
    private bool inputBlocked;
    private bool gameOver;
    private bool awaitingPromotion;
    private BoardSquare pendingPromotionTo;

    public PieceView SelectedPiece => selectedPiece;
    public bool IsInputBlocked => inputBlocked || awaitingPromotion;
    public bool IsAwaitingPromotion => awaitingPromotion;
    public ChessSide CurrentTurn => rules.CurrentTurn;
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

        if (hud == null)
        {
            hud = Object.FindFirstObjectByType<GameHud>();
        }

        if (cameraController == null)
        {
            cameraController = Object.FindFirstObjectByType<CameraController>();
        }

        if (motionController == null)
        {
            motionController = Object.FindFirstObjectByType<PieceMotionController>();
        }

        if (hud != null)
        {
            hud.Configure(this);
        }
    }

    private void Start()
    {
        StartLocalGame();
    }

    public void StartLocalGame()
    {
        NewGame();
    }

    public void NewGame()
    {
        rules.Reset();
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
        if (!awaitingPromotion || selectedPiece == null)
        {
            return;
        }

        awaitingPromotion = false;
        ExecuteSelectedMove(pendingPromotionTo, char.ToUpperInvariant(promotion));
    }

    public void CancelSelection()
    {
        awaitingPromotion = false;
        ClearSelection();
        SetStatusForTurn();
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
        PieceView movingPiece = selectedPiece;
        BoardSquare origin = selectedPiece.Square;
        PieceView capturedPiece = CaptureResolver.Resolve(boardView, movingPiece, destination);
        MoveResult moveResult = rules.TryMove(origin, destination, promotion);

        if (!moveResult.Success)
        {
            StatusMessage = moveResult.Message;
            return;
        }

        string moveNotation = BuildMoveNotation(movingPiece, origin, destination, moveResult, promotion);
        moveHistory.Add(moveNotation);
        ClearSelection();

        if (Application.isPlaying && moveDuration > 0f)
        {
            StartCoroutine(AnimateMoveThenSync(movingPiece, capturedPiece, destination, moveResult));
        }
        else
        {
            boardView.SyncPieces(rules.GetPieces(), pieceFactory);
            ApplyMoveResult(moveResult);
        }
    }

    private IEnumerator AnimateMoveThenSync(PieceView movingPiece, PieceView capturedPiece, BoardSquare destination, MoveResult moveResult)
    {
        inputBlocked = true;
        Vector3 targetPosition = boardView.GetPieceWorldPosition(destination);
        if (motionController != null && moveResult.IsCapture)
        {
            yield return motionController.PlayCapture(movingPiece, capturedPiece, targetPosition);
        }
        else if (motionController != null)
        {
            yield return motionController.MovePiece(movingPiece, targetPosition);
        }
        else
        {
            yield return movingPiece.MoveTo(targetPosition, moveDuration);
        }

        if (moveResult.IsCapture && cameraController != null)
        {
            cameraController.Shake(0.07f, 0.16f);
        }

        boardView.SyncPieces(rules.GetPieces(), pieceFactory);
        inputBlocked = false;
        ApplyMoveResult(moveResult);
    }

    private void ApplyMoveResult(MoveResult moveResult)
    {
        if (moveResult.IsCheckmate)
        {
            gameOver = true;
            ChessSide winner = CurrentTurn == ChessSide.White ? ChessSide.Black : ChessSide.White;
            StatusMessage = $"Xeque-mate. {SideName(winner)} vencem.";
            UpdateCameraForTurn(false);
            return;
        }

        if (moveResult.IsDraw)
        {
            gameOver = true;
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
        StatusMessage = $"Turno: {SideName(CurrentTurn)}";
    }

    private void UpdateCameraForTurn(bool instant)
    {
        if (cameraController != null)
        {
            cameraController.SetPerspective(CurrentTurn, instant);
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
