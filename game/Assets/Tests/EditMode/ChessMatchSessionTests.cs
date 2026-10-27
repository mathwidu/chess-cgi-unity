using System.Linq;
using NUnit.Framework;

public class ChessMatchSessionTests
{
    private const string Empty = "{\"version\":1,\"mode\":\"local\",\"humanSide\":\"white\",\"difficulty\":\"beginner\",\"moves\":[]}";

    private static ChessRulesAdapter Play(params string[] moves)
    {
        var rules = new ChessRulesAdapter();
        foreach (string uci in moves)
        {
            Assert.That(ChessMove.TryParseUci(uci, out ChessMove move), Is.True);
            Assert.That(rules.TryMove(move).Success, Is.True, uci);
        }
        return rules;
    }

    private static ChessMatchSession.Replay RoundTrip(ChessRulesAdapter rules)
    {
        Assert.That(ChessMatchSession.TrySerialize(rules.GetSnapshot(), true, ChessSide.Black,
            ComputerDifficulty.Hard, out string json, out string reason), Is.True, reason);
        Assert.That(ChessMatchSession.TryReplay(json, out var replay, out reason), Is.True, reason);
        Assert.That(replay.Rules.GetSnapshot().Fen, Is.EqualTo(rules.GetSnapshot().Fen));
        Assert.That(replay.Rules.GetSnapshot().Moves, Is.EqualTo(rules.GetSnapshot().Moves));
        Assert.That(replay.AgainstComputer, Is.True);
        Assert.That(replay.HumanSide, Is.EqualTo(ChessSide.Black));
        Assert.That(replay.Difficulty, Is.EqualTo(ComputerDifficulty.Hard));
        return replay;
    }

    [Test]
    public void ReplayRetainsOrderedCapturesAndMaterial()
    {
        var replay = RoundTrip(Play("e2e4", "d7d5", "e4d5", "d8d5"));
        var captures = replay.Moves.Where(m => m.Result.Captured.HasValue).Select(m => m.Result.Captured.Value).ToArray();
        Assert.That(captures.Length, Is.EqualTo(2));
        Assert.That(captures[0].Side, Is.EqualTo(ChessSide.Black));
        Assert.That(captures[1].Side, Is.EqualTo(ChessSide.White));
        Assert.That(captures.All(p => p.Square.Equals(BoardSquare.FromAlgebraic("d5"))), Is.True);
        Assert.That(replay.Rules.GetMaterialBalance(), Is.Zero);
    }

    [Test]
    public void ReplayRestoresCastlingAndTheRook()
    {
        var before = RoundTrip(Play("e2e4", "e7e5", "g1f3", "b8c6", "f1c4", "g8f6"));
        ChessMove.TryParseUci("e1g1", out var castle);
        Assert.That(before.Rules.TryMove(castle).Success, Is.True);
        var after = RoundTrip(before.Rules);
        Assert.That(after.Rules.GetPieceAt(BoardSquare.FromAlgebraic("g1")).Value.Kind, Is.EqualTo(ChessPieceKind.King));
        Assert.That(after.Rules.GetPieceAt(BoardSquare.FromAlgebraic("f1")).Value.Kind, Is.EqualTo(ChessPieceKind.Rook));
        Assert.That(after.Moves.Last().Result.Captured, Is.Null);
    }

    [Test]
    public void PendingEnPassantSurvivesReloadAndRecordsTheCorrectSquare()
    {
        var replay = RoundTrip(Play("e2e4", "a7a6", "e4e5", "d7d5"));
        ChessMove.TryParseUci("e5d6", out var move);
        MoveResult result = replay.Rules.TryMove(move);
        Assert.That(result.Success, Is.True);
        Assert.That(result.Captured.Value.Square, Is.EqualTo(BoardSquare.FromAlgebraic("d5")));
        Assert.That(RoundTrip(replay.Rules).Rules.GetPieceAt(BoardSquare.FromAlgebraic("d5")), Is.Null);
    }

    [TestCase("q", ChessPieceKind.Queen)]
    [TestCase("r", ChessPieceKind.Rook)]
    [TestCase("b", ChessPieceKind.Bishop)]
    [TestCase("n", ChessPieceKind.Knight)]
    public void PromotionIsReplayedFromTheStandardInitialPosition(string promotion, ChessPieceKind kind)
    {
        var replay = RoundTrip(Play("a2a4", "h7h5", "a4a5", "h5h4", "a5a6", "h4h3", "a6b7", "h3g2", "b7a8" + promotion));
        Assert.That(replay.Rules.GetPieceAt(BoardSquare.FromAlgebraic("a8")).Value.Kind, Is.EqualTo(kind));
        Assert.That(replay.Moves.Last().Move.Promotion, Is.EqualTo(char.ToUpperInvariant(promotion[0])));
    }

    [Test]
    public void RepetitionHistoryAndItsSubsequentRuleResultSurviveReload()
    {
        var direct = Play("g1f3", "g8f6", "f3g1", "f6g8");
        var replay = RoundTrip(direct);
        foreach (string uci in new[] { "g1f3", "g8f6", "f3g1", "f6g8" })
        {
            ChessMove.TryParseUci(uci, out var move);
            MoveResult expected = direct.TryMove(move);
            MoveResult actual = replay.Rules.TryMove(move);
            Assert.That(actual.Success, Is.EqualTo(expected.Success));
            Assert.That(actual.Outcome, Is.EqualTo(expected.Outcome));
        }
        Assert.That(replay.Rules.GetSnapshot().Moves, Is.EqualTo("g1f3 g8f6 f3g1 f6g8 g1f3 g8f6 f3g1 f6g8"));
        RoundTrip(replay.Rules);
    }

    [Test]
    public void TerminalResultIsDerivedFromMovesAndMovesAfterItAreRejected()
    {
        var replay = RoundTrip(Play("f2f3", "e7e5", "g2g4", "d8h4"));
        Assert.That(replay.Outcome, Is.EqualTo(MatchOutcome.Checkmate));
        Assert.That(replay.Rules.CurrentTurn, Is.EqualTo(ChessSide.White));
        string invalid = Empty.Replace("[]", "[\"f2f3\",\"e7e5\",\"g2g4\",\"d8h4\",\"e2e4\"]");
        Assert.That(ChessMatchSession.TryReplay(invalid, out _, out var reason), Is.False);
        Assert.That(reason, Is.EqualTo("moves-after-result"));
    }

    [TestCase("not json")]
    [TestCase("{}")]
    [TestCase("[]")]
    [TestCase("null")]
    public void CorruptOrMissingSchemaIsRejected(string value) =>
        Assert.That(ChessMatchSession.TryReplay(value, out _, out _), Is.False);

    [Test]
    public void RejectsWrongVersionMissingDuplicateUnknownAndTrailingContent()
    {
        Assert.That(ChessMatchSession.TryReplay(Empty.Replace("\"version\":1", "\"version\":2"), out _, out string reason), Is.False);
        Assert.That(reason, Is.EqualTo("unsupported-version"));
        foreach (string invalid in new[] {
            Empty.Replace("\"version\":1,", ""),
            Empty.Replace("\"mode\":\"local\",", ""),
            Empty.Replace("\"humanSide\":\"white\",", ""),
            Empty.Replace("\"difficulty\":\"beginner\",", ""),
            Empty.Replace(",\"moves\":[]", ""),
            Empty.Replace("\"version\":1", "\"version\":1,\"version\":1"),
            Empty.Replace("\"moves\":[]", "\"extra\":1,\"moves\":[]"),
            Empty + " garbage",
            Empty.Replace("\"version\":1", "\"version\":01"),
            Empty.Replace("\"version\":1", "\"version\":1.0"),
            Empty.Replace("[]", "null"), Empty.Replace("[]", "[null]"),
            Empty.Replace("[]", "[\"e2e4\",]"),
            Empty.Replace("\"local\"", "\"local\\x\""),
            Empty.Replace("\"local\"", "\"local\n\"") })
            Assert.That(ChessMatchSession.TryReplay(invalid, out _, out _), Is.False, invalid);
    }

    [TestCase("mode", "local", "remote")]
    [TestCase("humanSide", "white", "purple")]
    [TestCase("difficulty", "beginner", "expert")]
    public void UnknownSettingsAreRejected(string key, string oldValue, string newValue) =>
        Assert.That(ChessMatchSession.TryReplay(Empty.Replace("\"" + key + "\":\"" + oldValue + "\"",
            "\"" + key + "\":\"" + newValue + "\""), out _, out _), Is.False);

    [TestCase("e2e5")]
    [TestCase("e7e5")]
    [TestCase("e2e4q")]
    [TestCase("E2E4")]
    [TestCase("a1a1")]
    [TestCase("e2e4\\n")]
    public void EveryMoveMustBeCanonicalAndLegal(string move) =>
        Assert.That(ChessMatchSession.TryReplay(Empty.Replace("[]", "[\"" + move + "\"]"), out _, out _), Is.False);

    [Test]
    public void WhitespaceAndJsonUnicodeEscapesRemainValidJson()
    {
        Assert.That(ChessMatchSession.TryReplay(" \n" + Empty.Replace("local", "\\u006cocal") + "\t", out var replay, out _), Is.True);
        Assert.That(replay.Moves, Is.Empty);
    }

    [Test]
    public void SizeAndMoveCountAreBoundedBeforeReplay()
    {
        Assert.That(ChessMatchSession.TryReplay(Empty + new string(' ', ChessMatchSession.MaxBytes), out _, out var reason), Is.False);
        Assert.That(reason, Is.EqualTo("save-too-large"));
        Assert.That(ChessMatchSession.TryReplay(Empty + new string('é', ChessMatchSession.MaxBytes / 2), out _, out reason), Is.False);
        Assert.That(reason, Is.EqualTo("save-too-large"));
        string tooManyMoves = Empty.Replace("[]", "[" + string.Join(",", Enumerable.Repeat("\"g1f3\"", ChessMatchSession.MaxMoves + 1)) + "]");
        Assert.That(ChessMatchSession.TryReplay(tooManyMoves, out _, out reason), Is.False);
        Assert.That(reason, Is.EqualTo("invalid-save"), "The bounded reader rejects the count before attempting the illegal repeated moves.");
    }

    [Test]
    public void VersionOneNeverSerializesAnUnspecifiedCustomStartingPosition()
    {
        var rules = new ChessRulesAdapter("7k/P7/8/8/8/8/8/7K w - - 0 1");
        Assert.That(ChessMatchSession.TrySerialize(rules.GetSnapshot(), false, ChessSide.White,
            ComputerDifficulty.Beginner, out _, out _), Is.False);
    }
}
