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

    [Test]
    public void EvaluateWalkPose_StartMiddleEndKeepsBoardDestinationStable()
    {
        Vector3 start = new Vector3(0f, 0.08f, 0f);
        Vector3 target = new Vector3(2f, 0.08f, 0f);
        PieceMotionSettings settings = PieceMotionSettings.Default;

        PieceView.WalkPose startPose = PieceView.EvaluateWalkPose(start, target, 0f, settings);
        PieceView.WalkPose middlePose = PieceView.EvaluateWalkPose(start, target, 0.5f, settings);
        PieceView.WalkPose endPose = PieceView.EvaluateWalkPose(start, target, 1f, settings);

        AssertVector(start, startPose.RootPosition);
        Assert.AreEqual(1f, middlePose.RootPosition.x, 0.01f);
        Assert.GreaterOrEqual(middlePose.VisualOffset.y, 0f);
        Assert.LessOrEqual(middlePose.VisualOffset.y, 0.055f);
        AssertVector(target, endPose.RootPosition);
        AssertVector(Vector3.zero, endPose.VisualOffset);
    }

    [Test]
    public void EvaluateWalkPose_UsesSubtleSideSwayDuringStride()
    {
        Vector3 start = new Vector3(0f, 0.08f, 0f);
        Vector3 target = new Vector3(2f, 0.08f, 0f);
        PieceMotionSettings settings = PieceMotionSettings.Default;

        PieceView.WalkPose quarterPose = PieceView.EvaluateWalkPose(start, target, 0.25f, settings);

        Assert.Greater(Mathf.Abs(quarterPose.VisualOffset.x), 0.001f);
        Assert.Less(Mathf.Abs(quarterPose.VisualOffset.x), 0.04f);
    }

    [Test]
    public void EvaluateWalkPose_KnightStyleUsesReadableArcWithoutChangingDestination()
    {
        Vector3 start = new Vector3(0f, 0.08f, 0f);
        Vector3 target = new Vector3(2f, 0.08f, 1f);
        PieceMotionSettings settings = PieceMotionSettings.Default;
        PieceMovementStyle knight = PieceMovementStyleLibrary.GetStyle(ChessPieceKind.Knight);

        PieceView.WalkPose middlePose = PieceView.EvaluateWalkPose(start, target, 0.5f, settings, knight);
        PieceView.WalkPose endPose = PieceView.EvaluateWalkPose(start, target, 1f, settings, knight);

        Assert.Greater(middlePose.VisualOffset.y, 0.2f);
        AssertVector(target, endPose.RootPosition);
        AssertVector(Vector3.zero, endPose.VisualOffset);
    }

    private static void AssertVector(Vector3 expected, Vector3 actual)
    {
        Assert.AreEqual(expected.x, actual.x, 0.001f);
        Assert.AreEqual(expected.y, actual.y, 0.001f);
        Assert.AreEqual(expected.z, actual.z, 0.001f);
    }
}
