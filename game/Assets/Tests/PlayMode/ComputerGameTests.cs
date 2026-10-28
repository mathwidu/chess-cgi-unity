using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class ComputerGameTests
{
    private GameObject root;
    private ChessGameController controller;
    private BoardView board;

    private sealed class ScriptedMoveChooser : IMoveChooser
    {
        private readonly Queue<string> moves;
        public int Calls;
        public ScriptedMoveChooser(params string[] moves) { this.moves = new Queue<string>(moves); }
        public Task<ChessMove> ChooseMoveAsync(PositionSnapshot p, MoveSearchSettings s, CancellationToken token)
        {
            Calls++;
            if (!ChessMove.TryParseUci(moves.Dequeue(), out var move)) throw new InvalidOperationException("Bad test move");
            return Task.FromResult(move);
        }
        public void Dispose() { }
    }

    private sealed class DeferredChooser : IMoveChooser
    {
        public readonly TaskCompletionSource<ChessMove> Reply = new TaskCompletionSource<ChessMove>();
        public Task<ChessMove> ChooseMoveAsync(PositionSnapshot p, MoveSearchSettings s, CancellationToken token) => Reply.Task;
        public void Dispose() { }
    }

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        root = new GameObject("AI test");
        board = root.AddComponent<BoardView>();
        var factory = root.AddComponent<PieceFactory>();
        controller = root.AddComponent<ChessGameController>();
        controller.Configure(board, factory, null);
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        UnityEngine.Object.Destroy(root);
        yield return null;
    }

    private void HumanMove(string from, string to)
    {
        controller.SelectPiece(board.Pieces.First(p => p.Square.Equals(BoardSquare.FromAlgebraic(from))));
        controller.SelectDestination(BoardSquare.FromAlgebraic(to));
    }

    private IEnumerator WaitFor(Func<bool> condition)
    {
        float deadline = Time.realtimeSinceStartup + 5f;
        while (!condition() && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(condition(), Is.True, "Gameplay did not reach its expected state.");
    }

    [UnityTest]
    public IEnumerator DisablingAndSuspendingBlockHumanInputAndResumeSafely()
    {
        controller.enabled = false;
        HumanMove("e2", "e4");
        Assert.That(controller.MoveHistory.Count, Is.Zero);
        controller.enabled = true;
        controller.SendMessage("OnApplicationPause", true);
        HumanMove("e2", "e4");
        Assert.That(controller.MoveHistory.Count, Is.Zero);
        controller.SendMessage("OnApplicationPause", false);
        HumanMove("e2", "e4");
        yield return WaitFor(() => !controller.IsInputBlocked);
        Assert.That(controller.MoveHistory.Count, Is.EqualTo(1));
    }

    [UnityTest]
    public IEnumerator LocalGameStillAlternatesAndRestartCancelsAnimation()
    {
        HumanMove("e2", "e4");
        Assert.That(controller.IsInputBlocked, Is.True);
        controller.NewGame();
        yield return new WaitForSeconds(0.4f);
        Assert.That(controller.MoveHistory.Count, Is.Zero);
        Assert.That(controller.CurrentTurn, Is.EqualTo(ChessSide.White));
        HumanMove("d2", "d4");
        yield return WaitFor(() => !controller.IsInputBlocked);
        HumanMove("d7", "d5");
        yield return WaitFor(() => !controller.IsInputBlocked);
        Assert.That(controller.MoveHistory.Count, Is.EqualTo(2));
    }

    [UnityTest]
    public IEnumerator ComputerMovesOnlyOnItsTurnAndCanFinishWithCheckmate()
    {
        var chooser = new ScriptedMoveChooser("e7e5", "d8h4");
        controller.SetMoveChooserFactory(() => chooser);
        controller.StartComputerGame(ChessSide.White, ComputerDifficulty.Beginner);
        HumanMove("f2", "f3");
        yield return WaitFor(() => controller.MoveHistory.Count == 2 && !controller.IsInputBlocked);
        HumanMove("g2", "g4");
        yield return WaitFor(() => controller.IsGameOver);
        Assert.That(controller.MoveHistory.Count, Is.EqualTo(4));
        Assert.That(controller.StatusMessage, Does.StartWith("Xeque-mate"));
        Assert.That(controller.Outcome, Is.EqualTo(MatchOutcome.Checkmate));
        Assert.That(controller.Winner, Is.EqualTo(ChessSide.Black));
        Assert.That(chooser.Calls, Is.EqualTo(2));
        HumanMove("e2", "e4");
        Assert.That(controller.MoveHistory.Count, Is.EqualTo(4), "No move is accepted after checkmate.");
    }

    [UnityTest]
    public IEnumerator HumanCheckmateNamesTheHumanAsWinnerAndNewGameClearsIt()
    {
        var chooser = new ScriptedMoveChooser("f7f6", "g7g5", "f7f6");
        controller.SetMoveChooserFactory(() => chooser);
        controller.StartComputerGame(ChessSide.White, ComputerDifficulty.Hard);
        HumanMove("e2", "e4");
        yield return WaitFor(() => controller.MoveHistory.Count == 2 && !controller.IsInputBlocked);
        HumanMove("d2", "d4");
        yield return WaitFor(() => controller.MoveHistory.Count == 4 && !controller.IsInputBlocked);
        HumanMove("d1", "h5");
        yield return WaitFor(() => controller.IsGameOver && !controller.IsInputBlocked);
        Assert.That(controller.Outcome, Is.EqualTo(MatchOutcome.Checkmate));
        Assert.That(controller.Winner, Is.EqualTo(ChessSide.White));
        Assert.That(controller.IsComputerTurn, Is.False);
        Assert.That(controller.MoveHistory.Last(), Does.EndWith("#"));
        controller.NewGame();
        Assert.That(controller.Outcome, Is.EqualTo(MatchOutcome.InProgress));
        Assert.That(controller.Winner, Is.Null);
        Assert.That(controller.IsGameOver, Is.False);
    }

    [UnityTest]
    public IEnumerator ChoosingBlackMakesComputerOpenAndBlocksHumanSelection()
    {
        var chooser = new DeferredChooser();
        controller.SetMoveChooserFactory(() => chooser);
        controller.StartComputerGame(ChessSide.Black, ComputerDifficulty.Beginner);
        yield return null;
        Assert.That(controller.IsComputerThinking, Is.True);
        HumanMove("e2", "e4");
        Assert.That(controller.MoveHistory.Count, Is.Zero);
        Assert.That(controller.SelectedPiece, Is.Null);
        ChessMove.TryParseUci("d2d4", out var reply);
        chooser.Reply.SetResult(reply);
        yield return WaitFor(() => controller.CurrentTurn == ChessSide.Black && !controller.IsInputBlocked);
        Assert.That(controller.MoveHistory.Count, Is.EqualTo(1));
    }

    [UnityTest]
    public IEnumerator RestartDiscardsAComputerResponseFromTheOldMatch()
    {
        var chooser = new DeferredChooser();
        controller.SetMoveChooserFactory(() => chooser);
        controller.StartComputerGame(ChessSide.Black, ComputerDifficulty.Beginner);
        yield return null;
        controller.StartLocalGame();
        ChessMove.TryParseUci("e2e4", out var reply);
        chooser.Reply.SetResult(reply);
        yield return new WaitForSeconds(0.4f);
        Assert.That(controller.MoveHistory.Count, Is.Zero);
        Assert.That(controller.IsInputBlocked, Is.False);
    }

    [UnityTest]
    public IEnumerator IllegalComputerMoveLeavesTheBoardIntactAndRetryRecovers()
    {
        int attempt = 0;
        controller.SetMoveChooserFactory(() => new ScriptedMoveChooser(attempt++ == 0 ? "e2e5" : "e2e4"));
        controller.StartComputerGame(ChessSide.Black, ComputerDifficulty.Beginner);
        yield return WaitFor(() => controller.HasComputerError);
        Assert.That(controller.MoveHistory.Count, Is.Zero);
        Assert.That(board.Pieces.Any(p => p.Square.Equals(BoardSquare.FromAlgebraic("e2"))), Is.True);
        controller.RetryComputerTurn();
        yield return WaitFor(() => controller.MoveHistory.Count == 1 && !controller.IsInputBlocked);
        Assert.That(controller.HasComputerError, Is.False);
    }

    [UnityTest]
    public IEnumerator MenuCancelsThinkingAndLocalModeRemainsPlayable()
    {
        var chooser = new DeferredChooser();
        controller.SetMoveChooserFactory(() => chooser);
        controller.StartComputerGame(ChessSide.Black, ComputerDifficulty.Beginner);
        yield return null;
        controller.ReturnToMenu();
        ChessMove.TryParseUci("e2e4", out var move);
        chooser.Reply.SetResult(move);
        yield return null;
        Assert.That(controller.MoveHistory.Count, Is.Zero);
        Assert.That(controller.IsMenuOpen && controller.IsInputBlocked, Is.True);
        controller.StartLocalGame();
        HumanMove("e2", "e4");
        yield return WaitFor(() => !controller.IsInputBlocked);
        Assert.That(controller.CurrentTurn, Is.EqualTo(ChessSide.Black));
    }
}
