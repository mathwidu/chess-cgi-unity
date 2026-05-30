using NUnit.Framework;

public class BoardSquareTests
{
    [Test]
    public void FromAlgebraic_ParsesFileAndRank()
    {
        BoardSquare square = BoardSquare.FromAlgebraic("e4");

        Assert.AreEqual(4, square.FileIndex);
        Assert.AreEqual(4, square.Rank);
        Assert.AreEqual("e4", square.ToAlgebraic());
    }

    [Test]
    public void FromAlgebraic_RejectsInvalidSquare()
    {
        Assert.Throws<System.ArgumentException>(() => BoardSquare.FromAlgebraic("i9"));
    }
}
