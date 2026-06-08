using UnityEngine;

public static class CaptureAnimationStyleLibrary
{
    public static CaptureAnimationStyle GetStyle(ChessPieceKind kind)
    {
        switch (kind)
        {
            case ChessPieceKind.Pawn:
                return new CaptureAnimationStyle(
                    "Empurrao curto",
                    "DaggerLunge",
                    0.38f,
                    0.62f,
                    0.85f,
                    new Color(1f, 0.78f, 0.35f),
                    0.5f,
                    "Capture_Pawn_DaggerLunge");
            case ChessPieceKind.Rook:
                return new CaptureAnimationStyle(
                    "Avanco pesado",
                    "TowerCrush",
                    0.52f,
                    0.82f,
                    1.15f,
                    new Color(0.58f, 0.74f, 0.88f),
                    0.62f,
                    "Capture_Rook_TowerCrush");
            case ChessPieceKind.Knight:
                return new CaptureAnimationStyle(
                    "Salto de cavalo",
                    "HorseLeap",
                    0.48f,
                    0.76f,
                    1f,
                    new Color(0.48f, 0.78f, 1f),
                    0.58f,
                    "Capture_Knight_HorseLeap");
            case ChessPieceKind.Bishop:
                return new CaptureAnimationStyle(
                    "Corte diagonal",
                    "PrayerBeam",
                    0.44f,
                    0.7f,
                    0.95f,
                    new Color(0.8f, 1f, 0.82f),
                    0.55f,
                    "Capture_Bishop_PrayerBeam");
            case ChessPieceKind.Queen:
                return new CaptureAnimationStyle(
                    "Golpe dominante",
                    "RoyalSlash",
                    0.58f,
                    0.86f,
                    1.25f,
                    new Color(1f, 0.86f, 0.42f),
                    0.52f,
                    "Capture_Queen_RoyalSlash");
            case ChessPieceKind.King:
                return new CaptureAnimationStyle(
                    "Comando real",
                    "OpenHandStrike",
                    0.46f,
                    0.66f,
                    1.05f,
                    new Color(1f, 0.92f, 0.45f),
                    0.5f,
                    "Capture_King_OpenHandStrike");
            default:
                return new CaptureAnimationStyle(
                    "Captura",
                    "GenericCapture",
                    0.45f,
                    0.72f,
                    1f,
                    Color.yellow,
                    0.5f,
                    "Capture_Generic");
        }
    }
}
