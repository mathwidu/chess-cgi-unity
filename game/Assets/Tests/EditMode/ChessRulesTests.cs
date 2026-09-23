using NUnit.Framework;

public class ChessRulesTests
{
    internal static ChessMove Move(string uci)
    {
        Assert.That(ChessMove.TryParseUci(uci, out ChessMove move), Is.True);
        return move;
    }

    [TestCase("e2e4")]
    [TestCase("a7a8q")]
    [TestCase("a7b8n")]
    [TestCase("e1g1")]
    public void MoveRoundTrips(string uci) => Assert.That(Move(uci).ToUci(), Is.EqualTo(uci));

    [TestCase(null)] [TestCase("")] [TestCase("0000")] [TestCase("(none)")]
    [TestCase("a1a1")] [TestCase("i2e4")] [TestCase("e2e9")] [TestCase("a7a8k")]
    [TestCase("e2e4\nquit")] [TestCase("e2e4qq")]
    public void InvalidMoveIsRejected(string uci) => Assert.That(ChessMove.TryParseUci(uci, out _), Is.False);

    [Test]
    public void InvalidMoveLeavesPositionAndRevisionUntouched()
    {
        var rules = new ChessRulesAdapter();
        PositionSnapshot before = rules.GetSnapshot();
        Assert.That(rules.TryMove(Move("e2e5")).Success, Is.False);
        Assert.That(rules.TryMove(default(ChessMove)).Success, Is.False);
        Assert.That(rules.GetSnapshot().Fen, Is.EqualTo(before.Fen));
        Assert.That(rules.GetSnapshot().Revision, Is.EqualTo(before.Revision));
    }

    [Test]
    public void SnapshotIsImmutableAndRetainsHistoryAcrossMoves()
    {
        var rules = new ChessRulesAdapter();
        var before = rules.GetSnapshot();
        Assert.That(rules.TryMove(Move("e2e4")).Success, Is.True);
        Assert.That(rules.TryMove(Move("e7e5")).Success, Is.True);
        Assert.That(before.Moves, Is.Empty);
        Assert.That(rules.GetSnapshot().Moves, Is.EqualTo("e2e4 e7e5"));
        long movedRevision = rules.GetSnapshot().Revision;
        rules.Reset();
        Assert.That(rules.GetSnapshot().Fen, Is.EqualTo(before.Fen));
        Assert.That(rules.GetSnapshot().Revision, Is.GreaterThan(movedRevision));
    }

    [Test]
    public void CastlingMovesBothPieces()
    {
        var rules = new ChessRulesAdapter("r3k2r/8/8/8/8/8/8/R3K2R w KQkq - 0 1");
        Assert.That(rules.TryMove(Move("e1g1")).Success, Is.True);
        Assert.That(rules.GetPieceAt(BoardSquare.FromAlgebraic("f1")).Value.Kind, Is.EqualTo(ChessPieceKind.Rook));
        Assert.That(rules.GetPieceAt(BoardSquare.FromAlgebraic("g1")).Value.Kind, Is.EqualTo(ChessPieceKind.King));
    }

    [Test]
    public void EnPassantRemovesTheCapturedPawn()
    {
        var rules = new ChessRulesAdapter();
        foreach (string move in new[] { "e2e4", "a7a6", "e4e5", "d7d5" })
            Assert.That(rules.TryMove(Move(move)).Success, Is.True);
        MoveResult result = rules.TryMove(Move("e5d6"));
        Assert.That(result.Success && result.IsCapture, Is.True);
        Assert.That(rules.GetPieceAt(BoardSquare.FromAlgebraic("d5")), Is.Null);
    }

    [TestCase('q', ChessPieceKind.Queen)]
    [TestCase('r', ChessPieceKind.Rook)]
    [TestCase('b', ChessPieceKind.Bishop)]
    [TestCase('n', ChessPieceKind.Knight)]
    public void UnderpromotionIsApplied(char promotion, ChessPieceKind kind)
    {
        var rules = new ChessRulesAdapter("7k/P7/8/8/8/8/8/7K w - - 0 1");
        Assert.That(rules.TryMove(Move("a7a8" + promotion)).Success, Is.True);
        Assert.That(rules.GetPieceAt(BoardSquare.FromAlgebraic("a8")).Value.Kind, Is.EqualTo(kind));
    }

    [Test]
    public void CheckmateEndsThePosition()
    {
        var rules = new ChessRulesAdapter();
        foreach (string move in new[] { "f2f3", "e7e5", "g2g4" }) rules.TryMove(Move(move));
        Assert.That(rules.TryMove(Move("d8h4")).IsCheckmate, Is.True);
    }

    [Test]
    public void StalemateIsReported()
    {
        var rules = new ChessRulesAdapter("7k/5K2/8/6Q1/8/8/8/8 w - - 0 1");
        Assert.That(rules.TryMove(Move("g5g6")).IsDraw, Is.True);
    }
}
