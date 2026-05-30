using System.Linq;
using NUnit.Framework;

public class ChessRulesAdapterTests
{
    [Test]
    public void NewGame_StartsWithWhiteToMoveAndThirtyTwoPieces()
    {
        ChessRulesAdapter rules = new ChessRulesAdapter();

        Assert.AreEqual(ChessSide.White, rules.CurrentTurn);
        Assert.AreEqual(32, rules.GetPieces().Count);
    }

    [Test]
    public void LegalMoves_ForWhitePawnAtE2_IncludeE3AndE4()
    {
        ChessRulesAdapter rules = new ChessRulesAdapter();

        string[] moves = rules.GetLegalDestinations(BoardSquare.FromAlgebraic("e2"))
            .Select(square => square.ToAlgebraic())
            .ToArray();

        CollectionAssert.Contains(moves, "e3");
        CollectionAssert.Contains(moves, "e4");
    }

    [Test]
    public void TryMove_E2ToE4_AlternatesTurn()
    {
        ChessRulesAdapter rules = new ChessRulesAdapter();

        MoveResult result = rules.TryMove(BoardSquare.FromAlgebraic("e2"), BoardSquare.FromAlgebraic("e4"), null);

        Assert.IsTrue(result.Success);
        Assert.AreEqual(ChessSide.Black, rules.CurrentTurn);
        Assert.AreEqual("e4", result.To.ToAlgebraic());
    }
}
