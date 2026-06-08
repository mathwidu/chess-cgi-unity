using NUnit.Framework;

public class PieceMovementStyleLibraryTests
{
    [TestCase(ChessPieceKind.Pawn, "GroundedWalk")]
    [TestCase(ChessPieceKind.Rook, "HeavyHop")]
    [TestCase(ChessPieceKind.Knight, "ArcingLJump")]
    [TestCase(ChessPieceKind.Bishop, "RitualStride")]
    [TestCase(ChessPieceKind.Queen, "ConfidentWalk")]
    [TestCase(ChessPieceKind.King, "AuthoritativeSteps")]
    public void GetStyle_ReturnsExpectedStyleName(ChessPieceKind kind, string expectedName)
    {
        PieceMovementStyle style = PieceMovementStyleLibrary.GetStyle(kind);

        Assert.AreEqual(expectedName, style.Name);
    }

    [Test]
    public void AllStyles_FinishExactlyAtDestination()
    {
        foreach (ChessPieceKind kind in System.Enum.GetValues(typeof(ChessPieceKind)))
        {
            PieceMovementStyle style = PieceMovementStyleLibrary.GetStyle(kind);

            Assert.AreEqual(1f, style.RootProgressAt(1f), 0.001f, kind.ToString());
        }
    }

    [Test]
    public void AllStyles_AreSlowEnoughToReadCharacterMotion()
    {
        foreach (ChessPieceKind kind in System.Enum.GetValues(typeof(ChessPieceKind)))
        {
            PieceMovementStyle style = PieceMovementStyleLibrary.GetStyle(kind);

            Assert.GreaterOrEqual(style.Duration, 1.35f, kind.ToString());
            Assert.LessOrEqual(style.Duration, 1.75f, kind.ToString());
        }
    }

    [Test]
    public void AllStyles_HaveReadableBodyMotion()
    {
        foreach (ChessPieceKind kind in System.Enum.GetValues(typeof(ChessPieceKind)))
        {
            PieceMovementStyle style = PieceMovementStyleLibrary.GetStyle(kind);

            Assert.Greater(style.StepHeight + style.HopHeight + style.BodySway, 0.05f, kind.ToString());
            Assert.Greater(style.LeanAngle, 2f, kind.ToString());
        }
    }

    [Test]
    public void Knight_UsesHigherArcThanPawn()
    {
        PieceMovementStyle pawn = PieceMovementStyleLibrary.GetStyle(ChessPieceKind.Pawn);
        PieceMovementStyle knight = PieceMovementStyleLibrary.GetStyle(ChessPieceKind.Knight);

        Assert.Greater(knight.HopHeight, pawn.HopHeight);
        Assert.AreEqual("ArcingLJump", knight.Name);
    }

    [Test]
    public void Rook_UsesHeavyHopWithLowStride()
    {
        PieceMovementStyle rook = PieceMovementStyleLibrary.GetStyle(ChessPieceKind.Rook);

        Assert.AreEqual("HeavyHop", rook.Name);
        Assert.Less(rook.StrideCycles, 1f);
        Assert.Greater(rook.HopHeight, 0.1f);
        Assert.GreaterOrEqual(rook.Duration, 1.5f);
    }
}
