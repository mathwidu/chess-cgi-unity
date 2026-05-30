using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class ChessGameController : MonoBehaviour
{
    [SerializeField] private BoardView boardView;
    [SerializeField] private PieceFactory pieceFactory;
    [SerializeField] private GameHud hud;
    [SerializeField] private float moveDuration = 0.28f;

    private readonly ChessRulesAdapter rules = new ChessRulesAdapter();
    private readonly List<BoardSquare> legalDestinations = new List<BoardSquare>();

    private PieceView selectedPiece;
    private bool inputBlocked;
    private bool gameOver;
    private bool awaitingPromotion;
    private BoardSquare pendingPromotionTo;

    public PieceView SelectedPiece => selectedPiece;
    public bool IsInputBlocked => inputBlocked || awaitingPromotion;
    public bool IsAwaitingPromotion => awaitingPromotion;
    public ChessSide CurrentTurn => rules.CurrentTurn;
    public string StatusMessage { get; private set; } = "Turno: Brancas";

    public void Configure(BoardView board, PieceFactory factory, GameHud gameHud)
    {
        boardView = board;
        pieceFactory = factory;
        hud = gameHud;

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

        if (hud != null)
        {
            hud.Configure(this);
        }
    }

    private void Start()
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

        boardView.BuildBoard();
        boardView.SyncPieces(rules.GetPieces(), pieceFactory);
        SetStatusForTurn();
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
        MoveResult moveResult = rules.TryMove(origin, destination, promotion);

        if (!moveResult.Success)
        {
            StatusMessage = moveResult.Message;
            return;
        }

        ClearSelection();

        if (Application.isPlaying && moveDuration > 0f)
        {
            StartCoroutine(AnimateMoveThenSync(movingPiece, destination, moveResult));
        }
        else
        {
            boardView.SyncPieces(rules.GetPieces(), pieceFactory);
            ApplyMoveResult(moveResult);
        }
    }

    private IEnumerator AnimateMoveThenSync(PieceView movingPiece, BoardSquare destination, MoveResult moveResult)
    {
        inputBlocked = true;
        yield return movingPiece.MoveTo(boardView.GetPieceWorldPosition(destination), moveDuration);
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
            return;
        }

        if (moveResult.IsDraw)
        {
            gameOver = true;
            StatusMessage = "Empate.";
            return;
        }

        if (moveResult.IsCheck)
        {
            StatusMessage = $"Xeque. Turno: {SideName(CurrentTurn)}";
            return;
        }

        SetStatusForTurn();
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

    private static bool RequiresPromotion(PieceView piece, BoardSquare destination)
    {
        return piece.Kind == ChessPieceKind.Pawn &&
            ((piece.Side == ChessSide.White && destination.Rank == 8) ||
             (piece.Side == ChessSide.Black && destination.Rank == 1));
    }

    private static string SideName(ChessSide side)
    {
        return side == ChessSide.White ? "Brancas" : "Pretas";
    }
}
