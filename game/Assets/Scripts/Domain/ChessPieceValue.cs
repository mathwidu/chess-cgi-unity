// Conventional material values, used to tell which side is ahead in material.
public static class ChessPieceValue
{
    public static int Of(ChessPieceKind kind)
    {
        switch (kind)
        {
            case ChessPieceKind.Pawn:
                return 1;
            case ChessPieceKind.Knight:
            case ChessPieceKind.Bishop:
                return 3;
            case ChessPieceKind.Rook:
                return 5;
            case ChessPieceKind.Queen:
                return 9;
            default:
                return 0;
        }
    }
}
