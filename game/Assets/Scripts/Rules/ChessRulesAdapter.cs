using System.Collections.Generic;
using ChessDotNet;
using ChessDotNet.Pieces;

public sealed class ChessRulesAdapter
{
    private ChessGame game = new ChessGame();

    public ChessSide CurrentTurn => ToSide(game.WhoseTurn);

    public void Reset()
    {
        game = new ChessGame();
    }

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
        Move move = new Move(ToPosition(from), ToPosition(to), game.WhoseTurn, promotion);

        if (!game.IsValidMove(move))
        {
            return MoveResult.Failed(from, to, "Movimento invalido.");
        }

        Piece capturedPiece;
        MoveType moveType = game.MakeMove(move, true, out capturedPiece);
        bool isCheck = game.IsInCheck(game.WhoseTurn);
        bool isCheckmate = game.IsCheckmated(game.WhoseTurn);
        bool isDraw = game.IsDraw() || game.IsStalemated(game.WhoseTurn);

        return new MoveResult(
            true,
            from,
            to,
            capturedPiece != null,
            isCheck,
            isCheckmate,
            isDraw,
            BuildMessage(moveType, isCheck, isCheckmate, isDraw));
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

    private static string BuildMessage(MoveType moveType, bool isCheck, bool isCheckmate, bool isDraw)
    {
        if (isCheckmate)
        {
            return "Xeque-mate.";
        }

        if (isDraw)
        {
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
