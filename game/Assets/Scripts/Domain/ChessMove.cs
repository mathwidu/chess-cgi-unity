using System;

public readonly struct ChessMove
{
    public BoardSquare From { get; }
    public BoardSquare To { get; }
    public char? Promotion { get; }

    public ChessMove(BoardSquare from, BoardSquare to, char? promotion = null)
    {
        if (from.Rank < 1 || to.Rank < 1 || from.Equals(to))
            throw new ArgumentException("A move requires two different valid squares.");
        char? piece = promotion.HasValue ? char.ToUpperInvariant(promotion.Value) : (char?)null;
        if (piece.HasValue && "QRBN".IndexOf(piece.Value) < 0)
            throw new ArgumentException("Promotion must be Q, R, B or N.", nameof(promotion));
        From = from;
        To = to;
        Promotion = piece;
    }

    public static bool TryParseUci(string value, out ChessMove move)
    {
        move = default;
        if (value == null || (value.Length != 4 && value.Length != 5))
        {
            return false;
        }
        try
        {
            move = new ChessMove(BoardSquare.FromAlgebraic(value.Substring(0, 2)),
                BoardSquare.FromAlgebraic(value.Substring(2, 2)), value.Length == 5 ? value[4] : (char?)null);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    public string ToUci() => From.ToAlgebraic() + To.ToAlgebraic() +
        (Promotion.HasValue ? char.ToLowerInvariant(Promotion.Value).ToString() : "");
}
