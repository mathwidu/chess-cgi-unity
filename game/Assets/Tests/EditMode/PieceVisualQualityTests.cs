using NUnit.Framework;
using UnityEngine;

public class PieceVisualQualityTests
{
    [Test]
    public void Evaluate_CustomPrefabWithRendererPassesBasicBounds()
    {
        GameObject prefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
        try
        {
            PieceVisualQuality.Report report = PieceVisualQuality.Evaluate(prefab);

            Assert.IsTrue(report.HasRenderer);
            Assert.AreEqual(1, report.RendererCount);
            Assert.Greater(report.Bounds.size.y, 0f);
        }
        finally
        {
            Object.DestroyImmediate(prefab);
        }
    }

    [Test]
    public void Evaluate_EmptyPrefabReportsNoRenderer()
    {
        GameObject prefab = new GameObject("Empty Custom Piece");
        try
        {
            PieceVisualQuality.Report report = PieceVisualQuality.Evaluate(prefab);

            Assert.IsFalse(report.HasRenderer);
            Assert.AreEqual(0, report.RendererCount);
        }
        finally
        {
            Object.DestroyImmediate(prefab);
        }
    }
}
