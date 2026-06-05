public static class CaptureResolver
{
    public static PieceView Resolve(BoardView boardView, PieceView attacker, BoardSquare destination)
    {
        if (boardView == null || attacker == null)
        {
            return null;
        }

        PieceView destinationPiece = boardView.FindPieceAt(destination);
        if (destinationPiece != null && destinationPiece.Side != attacker.Side)
        {
            return destinationPiece;
        }

        return null;
    }
}
