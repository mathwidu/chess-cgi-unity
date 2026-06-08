using NUnit.Framework;

public class CaptureAnimationStyleLibraryTests
{
    [TestCase(ChessPieceKind.Pawn, "DaggerLunge", "Capture_Pawn_DaggerLunge")]
    [TestCase(ChessPieceKind.Rook, "TowerCrush", "Capture_Rook_TowerCrush")]
    [TestCase(ChessPieceKind.Knight, "HorseLeap", "Capture_Knight_HorseLeap")]
    [TestCase(ChessPieceKind.Bishop, "PrayerBeam", "Capture_Bishop_PrayerBeam")]
    [TestCase(ChessPieceKind.Queen, "RoyalSlash", "Capture_Queen_RoyalSlash")]
    [TestCase(ChessPieceKind.King, "OpenHandStrike", "Capture_King_OpenHandStrike")]
    public void GetStyle_ReturnsExpectedCaptureContract(ChessPieceKind kind, string expectedName, string expectedClip)
    {
        CaptureAnimationStyle style = CaptureAnimationStyleLibrary.GetStyle(kind);

        Assert.AreEqual(expectedName, style.Name);
        Assert.AreEqual(expectedName, style.ContractName);
        Assert.AreEqual(expectedClip, style.FutureClipName);
    }

    [Test]
    public void AllCaptureStyles_StayShortEnoughForChessFlow()
    {
        foreach (ChessPieceKind kind in System.Enum.GetValues(typeof(ChessPieceKind)))
        {
            CaptureAnimationStyle style = CaptureAnimationStyleLibrary.GetStyle(kind);

            Assert.LessOrEqual(style.Duration, 0.95f, kind.ToString());
            Assert.GreaterOrEqual(style.ImpactAtNormalizedTime, 0.35f, kind.ToString());
            Assert.LessOrEqual(style.ImpactAtNormalizedTime, 0.75f, kind.ToString());
            Assert.IsNotEmpty(style.FutureClipName, kind.ToString());
        }
    }

    [Test]
    public void AllCaptureStyles_AvoidMagentaParticleBugColors()
    {
        foreach (ChessPieceKind kind in System.Enum.GetValues(typeof(ChessPieceKind)))
        {
            UnityEngine.Color color = CaptureAnimationStyleLibrary.GetStyle(kind).ImpactColor;
            bool isMagentaLike = color.r > 0.82f && color.b > 0.72f && color.g < 0.86f;

            Assert.IsFalse(isMagentaLike, kind.ToString());
        }
    }

    [Test]
    public void CompatibilityLibrary_UsesSameSourceOfTruth()
    {
        foreach (ChessPieceKind kind in System.Enum.GetValues(typeof(ChessPieceKind)))
        {
            CaptureAnimationStyle planned = CaptureAnimationStyleLibrary.GetStyle(kind);
            CaptureAnimationStyle live = CaptureAnimationLibrary.GetStyle(kind);

            Assert.AreEqual(planned.Name, live.Name, kind.ToString());
            Assert.AreEqual(planned.FutureClipName, live.FutureClipName, kind.ToString());
            Assert.AreEqual(planned.Duration, live.Duration, 0.001f, kind.ToString());
        }
    }
}
