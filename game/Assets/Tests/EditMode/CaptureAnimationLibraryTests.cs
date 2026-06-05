using NUnit.Framework;

public class CaptureAnimationLibraryTests
{
    [Test]
    public void GetStyle_ReturnsDistinctMovementForMajorPieces()
    {
        CaptureAnimationStyle pawn = CaptureAnimationLibrary.GetStyle(ChessPieceKind.Pawn);
        CaptureAnimationStyle rook = CaptureAnimationLibrary.GetStyle(ChessPieceKind.Rook);
        CaptureAnimationStyle queen = CaptureAnimationLibrary.GetStyle(ChessPieceKind.Queen);

        Assert.AreEqual("Empurrao curto", pawn.DisplayName);
        Assert.AreEqual("Avanco pesado", rook.DisplayName);
        Assert.AreEqual("Golpe dominante", queen.DisplayName);
        Assert.Greater(rook.LungeDistance, pawn.LungeDistance);
        Assert.Greater(queen.ImpactScale, pawn.ImpactScale);
    }

    [Test]
    public void GetStyle_AllPieceKindsHaveSafeDurations()
    {
        foreach (ChessPieceKind kind in System.Enum.GetValues(typeof(ChessPieceKind)))
        {
            CaptureAnimationStyle style = CaptureAnimationLibrary.GetStyle(kind);
            Assert.Greater(style.Duration, 0.2f);
            Assert.Less(style.Duration, 0.9f);
        }
    }
}
