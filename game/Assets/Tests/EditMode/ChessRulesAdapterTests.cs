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

    [Test]
    public void TryMove_WhiteKingsideCastle_MovesKingAndRook()
    {
        ChessRulesAdapter rules = new ChessRulesAdapter();
        Move(rules, "e2", "e4");
        Move(rules, "e7", "e5");
        Move(rules, "g1", "f3");
        Move(rules, "b8", "c6");
        Move(rules, "f1", "c4");
        Move(rules, "g8", "f6");

        MoveResult result = Move(rules, "e1", "g1");

        Assert.IsTrue(result.Success);
        AssertPiece(rules, "g1", ChessSide.White, ChessPieceKind.King);
        AssertPiece(rules, "f1", ChessSide.White, ChessPieceKind.Rook);
        Assert.IsFalse(rules.GetPieceAt(BoardSquare.FromAlgebraic("e1")).HasValue);
        Assert.IsFalse(rules.GetPieceAt(BoardSquare.FromAlgebraic("h1")).HasValue);
        Assert.AreEqual(ChessSide.Black, rules.CurrentTurn);
    }

    [Test]
    public void TryMove_EnPassantCapture_RemovesPassedPawn()
    {
        ChessRulesAdapter rules = new ChessRulesAdapter();
        Move(rules, "e2", "e4");
        Move(rules, "a7", "a6");
        Move(rules, "e4", "e5");
        Move(rules, "d7", "d5");

        MoveResult result = Move(rules, "e5", "d6");

        Assert.IsTrue(result.IsCapture);
        AssertPiece(rules, "d6", ChessSide.White, ChessPieceKind.Pawn);
        Assert.IsFalse(rules.GetPieceAt(BoardSquare.FromAlgebraic("d5")).HasValue);
        Assert.IsFalse(rules.GetPieceAt(BoardSquare.FromAlgebraic("e5")).HasValue);
        Assert.AreEqual(ChessSide.Black, rules.CurrentTurn);
    }

    [Test]
    public void TryMove_PawnPromotesToQueen_ReplacesPawnWithQueen()
    {
        ChessRulesAdapter rules = new ChessRulesAdapter();
        Move(rules, "a2", "a4");
        Move(rules, "h7", "h5");
        Move(rules, "a4", "a5");
        Move(rules, "h5", "h4");
        Move(rules, "a5", "a6");
        Move(rules, "h4", "h3");
        Move(rules, "a6", "b7");
        Move(rules, "h3", "g2");

        MoveResult result = Move(rules, "b7", "a8", 'Q');

        Assert.IsTrue(result.IsCapture);
        AssertPiece(rules, "a8", ChessSide.White, ChessPieceKind.Queen);
        Assert.IsFalse(rules.GetPieceAt(BoardSquare.FromAlgebraic("b7")).HasValue);
        Assert.AreEqual(ChessSide.Black, rules.CurrentTurn);
    }

    [Test]
    public void TryMove_FoolsMate_ReportsCheckmate()
    {
        ChessRulesAdapter rules = new ChessRulesAdapter();
        Move(rules, "f2", "f3");
        Move(rules, "e7", "e5");
        Move(rules, "g2", "g4");

        MoveResult result = Move(rules, "d8", "h4");

        Assert.IsTrue(result.IsCheck);
        Assert.IsTrue(result.IsCheckmate);
        Assert.AreEqual("Xeque-mate.", result.Message);
        Assert.AreEqual(ChessSide.White, rules.CurrentTurn);
    }

    [Test]
    public void TryMove_WhenKingIsInCheck_RejectsMoveThatDoesNotResolveCheck()
    {
        ChessRulesAdapter rules = new ChessRulesAdapter();
        Move(rules, "e2", "e4");
        Move(rules, "e7", "e5");
        Move(rules, "g1", "f3");
        Move(rules, "d7", "d6");
        MoveResult check = Move(rules, "f1", "b5");

        MoveResult ignoredCheck = rules.TryMove(BoardSquare.FromAlgebraic("a7"), BoardSquare.FromAlgebraic("a6"), null);

        Assert.IsTrue(check.IsCheck);
        Assert.IsFalse(ignoredCheck.Success);
        Assert.AreEqual(ChessSide.Black, rules.CurrentTurn);
    }

    private static MoveResult Move(ChessRulesAdapter rules, string from, string to, char? promotion = null)
    {
        MoveResult result = rules.TryMove(BoardSquare.FromAlgebraic(from), BoardSquare.FromAlgebraic(to), promotion);
        Assert.IsTrue(result.Success, $"Expected {from}-{to} to be legal. Message: {result.Message}");
        return result;
    }

    private static void AssertPiece(ChessRulesAdapter rules, string square, ChessSide side, ChessPieceKind kind)
    {
        VisualPieceState? piece = rules.GetPieceAt(BoardSquare.FromAlgebraic(square));
        Assert.IsTrue(piece.HasValue, $"Expected a piece at {square}.");
        Assert.AreEqual(side, piece.Value.Side);
        Assert.AreEqual(kind, piece.Value.Kind);
    }
}
