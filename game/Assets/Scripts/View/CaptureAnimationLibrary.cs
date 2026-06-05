using UnityEngine;

public static class CaptureAnimationLibrary
{
    public static CaptureAnimationStyle GetStyle(ChessPieceKind kind)
    {
        switch (kind)
        {
            case ChessPieceKind.Pawn:
                return new CaptureAnimationStyle("Empurrao curto", 0.38f, 0.62f, 0.85f, new Color(1f, 0.78f, 0.35f));
            case ChessPieceKind.Rook:
                return new CaptureAnimationStyle("Avanco pesado", 0.52f, 0.82f, 1.15f, new Color(0.75f, 0.86f, 1f));
            case ChessPieceKind.Knight:
                return new CaptureAnimationStyle("Salto de cavalo", 0.48f, 0.76f, 1f, new Color(0.9f, 0.72f, 1f));
            case ChessPieceKind.Bishop:
                return new CaptureAnimationStyle("Corte diagonal", 0.44f, 0.7f, 0.95f, new Color(0.8f, 1f, 0.82f));
            case ChessPieceKind.Queen:
                return new CaptureAnimationStyle("Golpe dominante", 0.58f, 0.86f, 1.25f, new Color(1f, 0.68f, 0.95f));
            case ChessPieceKind.King:
                return new CaptureAnimationStyle("Comando real", 0.46f, 0.66f, 1.05f, new Color(1f, 0.92f, 0.45f));
            default:
                return new CaptureAnimationStyle("Captura", 0.45f, 0.72f, 1f, Color.yellow);
        }
    }
}
