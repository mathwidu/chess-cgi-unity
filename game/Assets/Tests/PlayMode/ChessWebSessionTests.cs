using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class ChessWebSessionTests
{
    private const string Empty = "{\"version\":1,\"mode\":\"local\",\"humanSide\":\"white\",\"difficulty\":\"beginner\",\"moves\":[]}";
    private GameObject root;
    private BoardView board;
    private GameHud hud;
    private ChessGameController controller;
    private ChessWebSession session;
    private MemoryStorage storage;

    private sealed class Notice
    {
        public string Name, Reason;
        public bool Ok, HasSave;
    }
    private sealed class MemoryStorage : IChessWebSessionStorage
    {
        public string Json = Empty.Replace("[]", "[\"e2e4\"]");
        public int Writes;
        public bool ReadBlocked, WriteBlocked, ThrowOnWrite;
        public readonly List<Notice> Events = new List<Notice>();
        public bool Read(out string json, out bool exists, out string reason)
        {
            json = ReadBlocked ? null : Json;
            exists = !ReadBlocked && Json != null;
            reason = ReadBlocked ? "storage-unavailable" : "";
            return !ReadBlocked;
        }
        public bool Write(string value, out string reason)
        {
            Writes++;
            if (ThrowOnWrite) throw new InvalidOperationException("Storage is blocked in this test.");
            reason = WriteBlocked ? "storage-full" : "";
            if (WriteBlocked) return false;
            Json = value;
            return true;
        }
        public void Notify(string name, bool ok, bool hasSave, string reason) =>
            Events.Add(new Notice { Name = name, Ok = ok, HasSave = hasSave, Reason = reason });
    }
    private sealed class DeferredChooser : IPlayerLoopMoveChooser
    {
        public readonly TaskCompletionSource<ChessMove> Reply = new TaskCompletionSource<ChessMove>();
        public int Calls;
        public bool Disposed;
        public CancellationToken Token;
        public Task<ChessMove> ChooseMoveAsync(PositionSnapshot position, MoveSearchSettings settings, CancellationToken token)
        { Calls++; Token = token; return Reply.Task; }
        public void Poll() { }
        public void Dispose() { Disposed = true; }
    }

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        root = new GameObject("Session test");
        board = root.AddComponent<BoardView>();
        var factory = root.AddComponent<PieceFactory>();
        // Keep the real boot/menu branch, without building an unrelated UI tree.
        hud = root.AddComponent<GameHud>();
        hud.enabled = false;
        controller = root.AddComponent<ChessGameController>();
        controller.Configure(board, factory, hud);
        session = root.AddComponent<ChessWebSession>();
        storage = new MemoryStorage();
        session.Initialize(controller, storage);
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    { UnityEngine.Object.Destroy(root); yield return null; }

    private void Move(string from, string to)
    {
        controller.SelectPiece(board.Pieces.First(p => p.Square.Equals(BoardSquare.FromAlgebraic(from))));
        controller.SelectDestination(BoardSquare.FromAlgebraic(to));
    }

    private IEnumerator WaitFor(Func<bool> condition)
    {
        float deadline = Time.realtimeSinceStartup + 5f;
        while (!condition() && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(condition(), Is.True, "The expected restored gameplay state was not reached.");
    }

    [UnityTest]
    public IEnumerator BootAndSaveNowPreserveThePreviousSaveUntilTheUserStarts()
    {
        string before = storage.Json;
        Assert.That(controller.IsMenuOpen, Is.True);
        Assert.That(controller.HasPersistableSession, Is.False);
        session.SaveNow();
        Assert.That(storage.Writes, Is.Zero);
        Assert.That(storage.Json, Is.EqualTo(before));
        Assert.That(storage.Events.Last().Reason, Is.EqualTo("no-active-session"));
        controller.StartLocalGame();
        Assert.That(storage.Writes, Is.EqualTo(1));
        Assert.That(ChessMatchSession.TryReplay(storage.Json, out var replay, out _), Is.True);
        Assert.That(replay.Moves, Is.Empty);
        yield return null;
    }

    [UnityTest]
    public IEnumerator AcceptedMoveIsSavedBeforeAnimationAndResumeDoesNotReemitIt()
    {
        controller.StartLocalGame();
        int landed = 0;
        controller.MoveApplied += _ => landed++;
        Move("e2", "e4");
        Assert.That(controller.IsAnimatingMove, Is.True);
        Assert.That(landed, Is.Zero);
        Assert.That(storage.Writes, Is.EqualTo(2));
        Assert.That(ChessMatchSession.TryReplay(storage.Json, out var replay, out _), Is.True);
        Assert.That(replay.Rules.GetSnapshot().Moves, Is.EqualTo("e2e4"));
        session.ResumeSavedGame();
        Assert.That(controller.IsAnimatingMove, Is.False);
        Assert.That(controller.MoveHistory, Is.EqualTo(new[] { "Brancas: e2-e4" }));
        Assert.That(board.Pieces.Any(p => p.Square.Equals(BoardSquare.FromAlgebraic("e4"))), Is.True);
        Assert.That(controller.LastMove.Value.To.ToAlgebraic(), Is.EqualTo("e4"));
        Assert.That(landed, Is.Zero);
        Assert.That(storage.Writes, Is.EqualTo(2), "Resume must not rewrite the original save.");
        yield return new WaitForSeconds(0.35f);
        Assert.That(landed, Is.Zero, "The old animation must have been cancelled.");
    }

    [UnityTest]
    public IEnumerator TerminalMoveSavedDuringAnimationRestoresResultWinnerAndHud()
    {
        typeof(GameHud).GetField("resultDismissed", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(hud, true);
        string beforeMate = Empty.Replace("[]", "[\"f2f3\",\"e7e5\",\"g2g4\"]");
        Assert.That(controller.TryResumeSession(beforeMate, out _), Is.True);
        Move("d8", "h4");
        Assert.That(controller.IsAnimatingMove, Is.True);
        Assert.That(ChessMatchSession.TryReplay(storage.Json, out var replay, out _), Is.True);
        Assert.That(replay.Outcome, Is.EqualTo(MatchOutcome.Checkmate));
        session.ResumeSavedGame();
        Assert.That(controller.Outcome, Is.EqualTo(MatchOutcome.Checkmate));
        Assert.That(controller.Winner, Is.EqualTo(ChessSide.Black));
        Assert.That(controller.MoveHistory.Last(), Does.EndWith("#"));
        Assert.That(controller.CheckedKing, Is.EqualTo(BoardSquare.FromAlgebraic("e1")));
        Assert.That(controller.IsComputerTurn, Is.False);
        Assert.That((bool)typeof(GameHud).GetField("showStartScreen", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(hud), Is.False,
            "A resumed board must not remain covered by the initial HUD menu.");
        Assert.That((bool)typeof(GameHud).GetField("resultDismissed", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(hud), Is.False,
            "Dismissing an earlier result must not hide the restored match result.");
        yield return null;
    }

    [UnityTest]
    public IEnumerator ResumeRebuildsCapturesMaterialAndOrderedHistory()
    {
        storage.Json = Empty.Replace("[]", "[\"e2e4\",\"d7d5\",\"e4d5\"]");
        session.ResumeSavedGame();
        Assert.That(controller.MoveHistory, Is.EqualTo(new[] { "Brancas: e2-e4", "Pretas: d7-d5", "Brancas: e4xd5" }));
        Assert.That(controller.CapturedPieces.Count, Is.EqualTo(1));
        Assert.That(controller.CapturedPieces[0].Square, Is.EqualTo(BoardSquare.FromAlgebraic("d5")));
        Assert.That(controller.MaterialBalance, Is.EqualTo(1));
        Assert.That(controller.CurrentTurn, Is.EqualTo(ChessSide.Black));
        Assert.That(controller.IsInputBlocked, Is.False);
        yield return null;
    }

    [UnityTest]
    public IEnumerator CorruptSaveNeverMutatesLiveBoardOrReplacesStoredBytes()
    {
        controller.StartLocalGame();
        Move("e2", "e4");
        yield return WaitFor(() => !controller.IsInputBlocked);
        string[] before = controller.MoveHistory.ToArray();
        storage.Json = Empty.Replace("[]", "[\"d2d4\",\"e7e5\",\"e1e8\"]");
        string invalid = storage.Json;
        int writes = storage.Writes;
        session.ResumeSavedGame();
        Assert.That(storage.Events.Last().Name, Is.EqualTo("cicochess:resume"));
        Assert.That(storage.Events.Last().Ok, Is.False);
        Assert.That(controller.MoveHistory, Is.EqualTo(before));
        Assert.That(board.Pieces.Any(p => p.Square.Equals(BoardSquare.FromAlgebraic("e4"))), Is.True);
        Assert.That(storage.Json, Is.EqualTo(invalid));
        Assert.That(storage.Writes, Is.EqualTo(writes));
        yield return null;
    }

    [UnityTest]
    public IEnumerator RepeatedResumeWithSameCountAndMaterialRefreshesCapturedDisplayWithoutFlight()
    {
        Assert.That(controller.TryResumeSession(Empty.Replace("[]", "[\"e2e4\",\"d7d5\",\"e4d5\"]"), out _), Is.True);
        yield return null;
        CapturedPiecesView view = board.CapturedPieces;
        Assert.That(view.DisplayedCount, Is.EqualTo(1));
        int firstObject = view.transform.Find("Pieces").GetChild(0).gameObject.GetInstanceID();
        Assert.That(controller.TryResumeSession(Empty.Replace("[]", "[\"d2d4\",\"e7e5\",\"d4e5\"]"), out _), Is.True);
        Assert.That(controller.CapturedPieces[0].Square.ToAlgebraic(), Is.EqualTo("e5"));
        Assert.That(controller.MaterialBalance, Is.EqualTo(1));
        yield return null;
        Transform shown = view.transform.Find("Pieces").GetChild(0);
        Assert.That(shown.gameObject.GetInstanceID(), Is.Not.EqualTo(firstObject));
        Vector3 position = shown.localPosition;
        yield return new WaitForSeconds(0.1f);
        Assert.That(shown.localPosition, Is.EqualTo(position), "A replayed capture must already be on the tray, without a flight animation.");
    }

    [UnityTest]
    public IEnumerator ReadyAllowsANewGameWithCorruptSaveOrUnavailableStorage()
    {
        storage.Json = "broken";
        session.Initialize(controller, storage);
        Assert.That(storage.Events.Last().Name, Is.EqualTo("cicochess:session-ready"));
        Assert.That(storage.Events.Last().Ok, Is.True);
        Assert.That(storage.Events.Last().HasSave, Is.True);
        Assert.That(storage.Events.Last().Reason, Is.EqualTo("invalid-save"));
        Assert.That(storage.Writes, Is.Zero);
        storage.ReadBlocked = true;
        storage.ThrowOnWrite = true;
        session.Initialize(controller, storage);
        Assert.That(storage.Events.Last().Ok, Is.True);
        Assert.That(storage.Events.Last().Reason, Is.EqualTo("storage-unavailable"));
        controller.StartLocalGame();
        Move("e2", "e4");
        Assert.That(controller.MoveHistory.Count, Is.EqualTo(1));
        Assert.That(storage.Events.Last().Ok, Is.False);
        Assert.That(storage.Events.Last().Reason, Is.EqualTo("storage-unavailable"));
        yield return WaitFor(() => !controller.IsInputBlocked);
    }

    [UnityTest]
    public IEnumerator QuotaFailureKeepsTheLastConsistentSaveAndTheGamePlayable()
    {
        controller.StartLocalGame();
        string previous = storage.Json;
        storage.WriteBlocked = true;
        Move("e2", "e4");
        Assert.That(storage.Json, Is.EqualTo(previous));
        Assert.That(storage.Events.Last().Reason, Is.EqualTo("storage-full"));
        Assert.That(storage.Events.Last().HasSave, Is.True);
        yield return WaitFor(() => !controller.IsInputBlocked);
        Move("e7", "e5");
        Assert.That(controller.MoveHistory.Count, Is.EqualTo(2));
        Assert.That(storage.Json, Is.EqualTo(previous));
    }

    [UnityTest]
    public IEnumerator ResumeCancelsOldSearchAndStartsExactlyOneForTheRestoredComputerTurn()
    {
        var old = new DeferredChooser();
        var fresh = new DeferredChooser();
        var choosers = new Queue<IMoveChooser>(new IMoveChooser[] { old, fresh });
        controller.SetMoveChooserFactory(() => choosers.Dequeue());
        controller.StartComputerGame(ChessSide.Black, ComputerDifficulty.Beginner);
        yield return null;
        Assert.That(old.Calls, Is.EqualTo(1));
        storage.Json = Empty.Replace("\"local\"", "\"computer\"").Replace("\"beginner\"", "\"hard\"")
            .Replace("[]", "[\"e2e4\"]");
        session.ResumeSavedGame();
        Assert.That(old.Token.IsCancellationRequested, Is.True);
        Assert.That(old.Disposed, Is.True);
        Assert.That(controller.HumanSide, Is.EqualTo(ChessSide.White));
        Assert.That(controller.Difficulty, Is.EqualTo(ComputerDifficulty.Hard));
        yield return null;
        yield return null;
        Assert.That(fresh.Calls, Is.EqualTo(1));
        ChessMove.TryParseUci("d2d4", out var stale);
        old.Reply.SetResult(stale);
        ChessMove.TryParseUci("e7e5", out var expected);
        fresh.Reply.SetResult(expected);
        yield return WaitFor(() => controller.MoveHistory.Count == 2 && !controller.IsInputBlocked);
        Assert.That(controller.MoveHistory, Is.EqualTo(new[] { "Brancas: e2-e4", "Pretas: e7-e5" }));
        Assert.That(fresh.Calls, Is.EqualTo(1));
    }

    [UnityTest]
    public IEnumerator InvalidResumeDoesNotCancelTheCurrentComputerSearch()
    {
        var chooser = new DeferredChooser();
        controller.SetMoveChooserFactory(() => chooser);
        controller.StartComputerGame(ChessSide.Black, ComputerDifficulty.Beginner);
        yield return null;
        Assert.That(controller.TryResumeSession(Empty.Replace("[]", "[\"e2e5\"]"), out _), Is.False);
        Assert.That(chooser.Token.IsCancellationRequested, Is.False);
        Assert.That(chooser.Disposed, Is.False);
        Assert.That(controller.IsComputerThinking, Is.True);
        Assert.That(controller.MoveHistory, Is.Empty);
        yield return null;
    }
}
