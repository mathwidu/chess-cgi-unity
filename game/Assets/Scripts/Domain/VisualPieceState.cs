public readonly struct VisualPieceState
{
    public BoardSquare Square { get; }
    public ChessSide Side { get; }
    public ChessPieceKind Kind { get; }

    public VisualPieceState(BoardSquare square, ChessSide side, ChessPieceKind kind)
    {
        Square = square;
        Side = side;
        Kind = kind;
    }
}
