using System.Collections.Generic;
using ChessDotNet;
using ChessDotNet.Pieces;

public sealed class ChessRulesAdapter
{
    private ChessGame game = new ChessGame();
    private readonly List<string> moves = new List<string>();
    private string initialFen;
    private long revision;

    public ChessRulesAdapter(string fen = null)
    {
        Reset(fen);
    }

    public ChessSide CurrentTurn => ToSide(game.WhoseTurn);

    public void Reset(string fen = null)
    {
        game = fen == null ? new ChessGame() : new ChessGame(fen);
        initialFen = game.GetFen();
        moves.Clear();
        revision++;
    }

    public PositionSnapshot GetSnapshot() => new PositionSnapshot(game.GetFen(), CurrentTurn,
        revision, initialFen, string.Join(" ", moves));

    public MoveResult TryMove(ChessMove move) => TryMove(move.From, move.To, move.Promotion);

    public List<VisualPieceState> GetPieces()
    {
        List<VisualPieceState> pieces = new List<VisualPieceState>();

        for (int rank = 1; rank <= 8; rank++)
        {
            for (int fileIndex = 0; fileIndex < 8; fileIndex++)
            {
                BoardSquare square = new BoardSquare(fileIndex, rank);
                Piece piece = game.GetPieceAt(ToPosition(square));

                if (piece == null)
                {
                    continue;
                }

                pieces.Add(new VisualPieceState(square, ToSide(piece.Owner), ToKind(piece)));
            }
        }

        return pieces;
    }

    public List<BoardSquare> GetLegalDestinations(BoardSquare from)
    {
        IReadOnlyCollection<Move> moves = game.GetValidMoves(ToPosition(from));
        List<BoardSquare> destinations = new List<BoardSquare>();

        foreach (Move move in moves)
        {
            destinations.Add(FromPosition(move.NewPosition));
        }

        return destinations;
    }

    public MoveResult TryMove(BoardSquare from, BoardSquare to, char? promotion)
    {
        if (from.Rank < 1 || to.Rank < 1 || from.Equals(to) ||
            (promotion.HasValue && "QRBN".IndexOf(char.ToUpperInvariant(promotion.Value)) < 0))
            return MoveResult.Failed(from, to, "Movimento invalido.");
        Move move = new Move(ToPosition(from), ToPosition(to), game.WhoseTurn, promotion);

        if (!game.IsValidMove(move))
        {
            return MoveResult.Failed(from, to, "Movimento invalido.");
        }

        // En passant takes a pawn that is not on the destination, which was empty before the move.
        bool destinationWasEmpty = game.GetPieceAt(ToPosition(to)) == null;
        Piece capturedPiece;
        MoveType moveType = game.MakeMove(move, true, out capturedPiece);
        VisualPieceState? captured = null;
        if (capturedPiece != null)
        {
            BoardSquare capturedSquare = destinationWasEmpty ? new BoardSquare(to.FileIndex, from.Rank) : to;
            captured = new VisualPieceState(capturedSquare, ToSide(capturedPiece.Owner), ToKind(capturedPiece));
        }

        moves.Add(new ChessMove(from, to, promotion).ToUci());
        revision++;
        bool isCheck = game.IsInCheck(game.WhoseTurn);
        bool isCheckmate = game.IsCheckmated(game.WhoseTurn);
        bool isStalemate = game.IsStalemated(game.WhoseTurn);
        bool isDraw = game.IsDraw() || isStalemate;
        MatchOutcome outcome = ToOutcome(isCheckmate, isStalemate, isDraw);

        return new MoveResult(
            true,
            from,
            to,
            capturedPiece != null,
            isCheck,
            isCheckmate,
            isDraw,
            BuildMessage(moveType, isCheck, outcome),
            outcome,
            captured);
    }

    public BoardSquare? FindKing(ChessSide side)
    {
        foreach (VisualPieceState piece in GetPieces())
        {
            if (piece.Kind == ChessPieceKind.King && piece.Side == side)
            {
                return piece.Square;
            }
        }

        return null;
    }

    // Material on the board, White minus Black; promotions count as the piece they became.
    public int GetMaterialBalance()
    {
        int balance = 0;
        foreach (VisualPieceState piece in GetPieces())
        {
            int value = ChessPieceValue.Of(piece.Kind);
            balance += piece.Side == ChessSide.White ? value : -value;
        }

        return balance;
    }

    public VisualPieceState? GetPieceAt(BoardSquare square)
    {
        Piece piece = game.GetPieceAt(ToPosition(square));
        if (piece == null)
        {
            return null;
        }

        return new VisualPieceState(square, ToSide(piece.Owner), ToKind(piece));
    }

    private MatchOutcome ToOutcome(bool isCheckmate, bool isStalemate, bool isDraw)
    {
        if (isCheckmate)
        {
            return MatchOutcome.Checkmate;
        }

        if (isStalemate)
        {
            return MatchOutcome.Stalemate;
        }

        if (!isDraw)
        {
            return MatchOutcome.InProgress;
        }

        return game.IsInsufficientMaterial() ? MatchOutcome.InsufficientMaterial : MatchOutcome.Draw;
    }

    private static string BuildMessage(MoveType moveType, bool isCheck, MatchOutcome outcome)
    {
        switch (outcome)
        {
            case MatchOutcome.Checkmate:
                return "Xeque-mate.";
            case MatchOutcome.Stalemate:
                return "Empate por afogamento.";
            case MatchOutcome.InsufficientMaterial:
                return "Empate por material insuficiente.";
            case MatchOutcome.Draw:
                return "Empate.";
        }

        if (isCheck)
        {
            return "Xeque.";
        }

        return moveType.ToString();
    }

    private static Position ToPosition(BoardSquare square)
    {
        return new Position((File)square.FileIndex, square.Rank);
    }

    private static BoardSquare FromPosition(Position position)
    {
        return new BoardSquare((int)position.File, position.Rank);
    }

    private static ChessSide ToSide(Player player)
    {
        return player == Player.White ? ChessSide.White : ChessSide.Black;
    }

    private static ChessPieceKind ToKind(Piece piece)
    {
        if (piece is Pawn)
        {
            return ChessPieceKind.Pawn;
        }

        if (piece is Rook)
        {
            return ChessPieceKind.Rook;
        }

        if (piece is Knight)
        {
            return ChessPieceKind.Knight;
        }

        if (piece is Bishop)
        {
            return ChessPieceKind.Bishop;
        }

        if (piece is Queen)
        {
            return ChessPieceKind.Queen;
        }

        return ChessPieceKind.King;
    }
}
