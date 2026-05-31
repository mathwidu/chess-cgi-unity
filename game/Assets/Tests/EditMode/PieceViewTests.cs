using NUnit.Framework;
using UnityEngine;

public class PieceViewTests
{
    [Test]
    public void SetSelected_UsesSubtleScaleMultiplierAndRestoresBaseScale()
    {
        GameObject pieceObject = new GameObject("Piece View Test");
        try
        {
            PieceView piece = pieceObject.AddComponent<PieceView>();
            pieceObject.transform.localScale = new Vector3(2f, 2f, 2f);
            piece.Initialize(new VisualPieceState(BoardSquare.FromAlgebraic("e2"), ChessSide.White, ChessPieceKind.Pawn));

            piece.SetSelected(true);

            AssertVector(new Vector3(2.14f, 2.14f, 2.14f), pieceObject.transform.localScale);

            piece.SetSelected(false);

            AssertVector(new Vector3(2f, 2f, 2f), pieceObject.transform.localScale);
        }
        finally
        {
            Object.DestroyImmediate(pieceObject);
        }
    }

    [Test]
    public void MoveTo_WithZeroDurationPlacesPieceExactlyAtTarget()
    {
        GameObject pieceObject = new GameObject("Piece View Test");
        try
        {
            PieceView piece = pieceObject.AddComponent<PieceView>();
            Vector3 target = new Vector3(2f, 0.08f, 3f);

            while (piece.MoveTo(target, 0f).MoveNext())
            {
            }

            AssertVector(target, pieceObject.transform.position);
        }
        finally
        {
            Object.DestroyImmediate(pieceObject);
        }
    }

    private static void AssertVector(Vector3 expected, Vector3 actual)
    {
        Assert.AreEqual(expected.x, actual.x, 0.001f);
        Assert.AreEqual(expected.y, actual.y, 0.001f);
        Assert.AreEqual(expected.z, actual.z, 0.001f);
    }
}
