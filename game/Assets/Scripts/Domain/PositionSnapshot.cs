public readonly struct PositionSnapshot
{
    public string Fen { get; }
    public ChessSide SideToMove { get; }
    public long Revision { get; }
    public string InitialFen { get; }
    public string Moves { get; }

    public PositionSnapshot(string fen, ChessSide sideToMove, long revision, string initialFen, string moves)
    {
        Fen = fen;
        SideToMove = sideToMove;
        Revision = revision;
        InitialFen = initialFen;
        Moves = moves;
    }
}
