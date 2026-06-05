using NUnit.Framework;
using UnityEngine;

public class CaptureResolverTests
{
    [Test]
    public void Resolve_NormalCaptureReturnsPieceOnDestination()
    {
        GameObject rig = new GameObject("Capture Resolver Test");
        try
        {
            BoardView board = rig.AddComponent<BoardView>();
            PieceFactory factory = rig.AddComponent<PieceFactory>();
            board.SyncPieces(
                new[]
                {
                    new VisualPieceState(BoardSquare.FromAlgebraic("e4"), ChessSide.White, ChessPieceKind.Pawn),
                    new VisualPieceState(BoardSquare.FromAlgebraic("d5"), ChessSide.Black, ChessPieceKind.Pawn)
                },
                factory);
            PieceView attacker = board.FindPieceAt(BoardSquare.FromAlgebraic("e4"));
            PieceView captured = board.FindPieceAt(BoardSquare.FromAlgebraic("d5"));

            PieceView resolved = CaptureResolver.Resolve(board, attacker, BoardSquare.FromAlgebraic("d5"));

            Assert.AreSame(captured, resolved);
        }
        finally
        {
            Object.DestroyImmediate(rig);
        }
    }

    [Test]
    public void Resolve_EmptyDestinationReturnsNull()
    {
        GameObject rig = new GameObject("Capture Resolver Test");
        try
        {
            BoardView board = rig.AddComponent<BoardView>();
            PieceFactory factory = rig.AddComponent<PieceFactory>();
            board.SyncPieces(
                new[] { new VisualPieceState(BoardSquare.FromAlgebraic("e4"), ChessSide.White, ChessPieceKind.Pawn) },
                factory);
            PieceView attacker = board.FindPieceAt(BoardSquare.FromAlgebraic("e4"));

            PieceView resolved = CaptureResolver.Resolve(board, attacker, BoardSquare.FromAlgebraic("e5"));

            Assert.IsNull(resolved);
        }
        finally
        {
            Object.DestroyImmediate(rig);
        }
    }

    [Test]
    public void FindPieceAt_ReturnsSyncedPieceBySquare()
    {
        GameObject rig = new GameObject("Board Lookup Test");
        try
        {
            BoardView board = rig.AddComponent<BoardView>();
            PieceFactory factory = rig.AddComponent<PieceFactory>();
            board.BuildBoard();
            board.SyncPieces(
                new[] { new VisualPieceState(BoardSquare.FromAlgebraic("c3"), ChessSide.White, ChessPieceKind.Knight) },
                factory);

            Assert.IsTrue(board.TryGetPieceAt(BoardSquare.FromAlgebraic("c3"), out PieceView piece));
            Assert.AreEqual(ChessPieceKind.Knight, piece.Kind);
            Assert.IsNull(board.FindPieceAt(BoardSquare.FromAlgebraic("c4")));
        }
        finally
        {
            Object.DestroyImmediate(rig);
        }
    }
}
