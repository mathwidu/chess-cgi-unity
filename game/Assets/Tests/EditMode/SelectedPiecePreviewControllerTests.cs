using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class SelectedPiecePreviewControllerTests
{
    [Test]
    public void ShowPiece_CreatesPreviewTextureAndClone()
    {
        GameObject owner = new GameObject("Preview Owner");
        GameObject source = GameObject.CreatePrimitive(PrimitiveType.Cube);
        try
        {
            RawImage image = owner.AddComponent<RawImage>();
            SelectedPiecePreviewController preview = owner.AddComponent<SelectedPiecePreviewController>();
            PieceView piece = source.AddComponent<PieceView>();
            piece.Initialize(new VisualPieceState(BoardSquare.FromAlgebraic("e2"), ChessSide.White, ChessPieceKind.Pawn));

            preview.Configure(image);
            preview.ShowPiece(piece);

            Assert.IsNotNull(image.texture);
            Assert.IsTrue(preview.HasPreview);
            Assert.Greater(preview.CurrentZoom, 0f);
        }
        finally
        {
            Object.DestroyImmediate(owner);
            Object.DestroyImmediate(source);
        }
    }

    [Test]
    public void RotateAndZoom_AreClampedToReadableValues()
    {
        GameObject owner = new GameObject("Preview Owner");
        try
        {
            SelectedPiecePreviewController preview = owner.AddComponent<SelectedPiecePreviewController>();
            float initialYaw = preview.CurrentYaw;

            preview.Rotate(45f);
            preview.Zoom(-100f);

            Assert.AreEqual(Mathf.Repeat(initialYaw + 45f, 360f), preview.CurrentYaw, 0.001f);
            Assert.GreaterOrEqual(preview.CurrentZoom, 1.6f);

            preview.Zoom(100f);

            Assert.LessOrEqual(preview.CurrentZoom, 4.8f);
        }
        finally
        {
            Object.DestroyImmediate(owner);
        }
    }
}
