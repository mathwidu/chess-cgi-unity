public readonly struct MoveResult
{
    public bool Success { get; }
    public BoardSquare From { get; }
    public BoardSquare To { get; }
    public bool IsCapture { get; }
    public bool IsCheck { get; }
    public bool IsCheckmate { get; }
    public bool IsDraw { get; }
    public string Message { get; }
    public MatchOutcome Outcome { get; }
    // The piece taken by this move, on the square it stood (differs from To for en passant).
    public VisualPieceState? Captured { get; }

    public MoveResult(
        bool success,
        BoardSquare from,
        BoardSquare to,
        bool isCapture,
        bool isCheck,
        bool isCheckmate,
        bool isDraw,
        string message,
        MatchOutcome outcome = MatchOutcome.InProgress,
        VisualPieceState? captured = null)
    {
        Success = success;
        From = from;
        To = to;
        IsCapture = isCapture;
        IsCheck = isCheck;
        IsCheckmate = isCheckmate;
        IsDraw = isDraw;
        Message = message;
        Outcome = outcome;
        Captured = captured;
    }

    public static MoveResult Failed(BoardSquare from, BoardSquare to, string message)
    {
        return new MoveResult(false, from, to, false, false, false, false, message);
    }
}
