public readonly struct ComputerTurnResult
{
    public ChessMove Move { get; }
    public string Error { get; }
    public bool Success => Error == null;

    public ComputerTurnResult(ChessMove move, string error)
    {
        Move = move;
        Error = error;
    }
}
