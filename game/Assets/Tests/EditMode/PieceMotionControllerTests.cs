using NUnit.Framework;
using UnityEngine;

public class PieceMotionControllerTests
{
    [Test]
    public void CreateDefaultSettings_UsesReadableDurations()
    {
        PieceMotionSettings settings = PieceMotionSettings.Default;

        Assert.Greater(settings.WalkDuration, 0.35f);
        Assert.Less(settings.WalkDuration, 0.9f);
        Assert.Greater(settings.StepHeight, 0.02f);
        Assert.Less(settings.StepHeight, 0.18f);
    }

    [Test]
    public void MoveInstantlyForTests_PlacesPieceAtTarget()
    {
        GameObject owner = new GameObject("Motion Test");
        GameObject pieceObject = new GameObject("Piece");
        try
        {
            PieceView piece = pieceObject.AddComponent<PieceView>();
            PieceMotionController motion = owner.AddComponent<PieceMotionController>();
            Vector3 target = new Vector3(3f, 0.08f, 2f);

            motion.MoveInstant(piece, target);

            Assert.AreEqual(target.x, piece.transform.position.x, 0.001f);
            Assert.AreEqual(target.y, piece.transform.position.y, 0.001f);
            Assert.AreEqual(target.z, piece.transform.position.z, 0.001f);
        }
        finally
        {
            Object.DestroyImmediate(owner);
            Object.DestroyImmediate(pieceObject);
        }
    }

    [Test]
    public void PlayCapture_HidesCapturedPieceAndMovesAttackerToDestination()
    {
        GameObject owner = new GameObject("Motion Test");
        GameObject attackerObject = new GameObject("Attacker");
        GameObject capturedObject = new GameObject("Captured");
        GameObject impact = null;
        try
        {
            PieceMotionController motion = owner.AddComponent<PieceMotionController>();
            PieceView attacker = attackerObject.AddComponent<PieceView>();
            PieceView captured = capturedObject.AddComponent<PieceView>();
            Vector3 destination = new Vector3(2f, 0.08f, 1f);

            attacker.Initialize(new VisualPieceState(BoardSquare.FromAlgebraic("e4"), ChessSide.White, ChessPieceKind.Pawn));
            captured.Initialize(new VisualPieceState(BoardSquare.FromAlgebraic("d5"), ChessSide.Black, ChessPieceKind.Pawn));
            attacker.transform.position = Vector3.zero;
            captured.transform.position = new Vector3(1f, 0.08f, 1f);

            RunCoroutineToEnd(motion.PlayCapture(attacker, captured, destination));

            impact = GameObject.Find("ImpactEffect");
            Assert.IsFalse(captured.gameObject.activeSelf);
            Assert.AreEqual(destination.x, attacker.transform.position.x, 0.001f);
            Assert.AreEqual(destination.y, attacker.transform.position.y, 0.001f);
            Assert.AreEqual(destination.z, attacker.transform.position.z, 0.001f);
            Assert.IsNotNull(impact);
        }
        finally
        {
            if (impact != null)
            {
                Object.DestroyImmediate(impact);
            }

            Object.DestroyImmediate(owner);
            Object.DestroyImmediate(attackerObject);
            Object.DestroyImmediate(capturedObject);
        }
    }

    private static void RunCoroutineToEnd(System.Collections.IEnumerator routine)
    {
        while (routine.MoveNext())
        {
            if (routine.Current is System.Collections.IEnumerator nested)
            {
                RunCoroutineToEnd(nested);
            }
        }
    }
}
