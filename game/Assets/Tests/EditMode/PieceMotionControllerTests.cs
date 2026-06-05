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
}
