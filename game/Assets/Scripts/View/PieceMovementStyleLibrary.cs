public static class PieceMovementStyleLibrary
{
    public static PieceMovementStyle GetStyle(ChessPieceKind kind)
    {
        switch (kind)
        {
            case ChessPieceKind.Rook:
                return new PieceMovementStyle("HeavyHop", 1.55f, 0.018f, 0.006f, 0.62f, 0.2f, 5f);
            case ChessPieceKind.Knight:
                return new PieceMovementStyle("ArcingLJump", 1.65f, 0.018f, 0.01f, 0.82f, 0.36f, 8.5f);
            case ChessPieceKind.Bishop:
                return new PieceMovementStyle("RitualStride", 1.46f, 0.034f, 0.015f, 1.05f, 0.055f, 4f);
            case ChessPieceKind.Queen:
                return new PieceMovementStyle("ConfidentWalk", 1.42f, 0.03f, 0.013f, 1f, 0.045f, 2.8f);
            case ChessPieceKind.King:
                return new PieceMovementStyle("AuthoritativeSteps", 1.52f, 0.028f, 0.012f, 0.88f, 0.04f, 2.5f);
            default:
                return new PieceMovementStyle("GroundedWalk", 1.38f, 0.052f, 0.02f, 1.35f, 0.008f, 3.6f);
        }
    }
}
