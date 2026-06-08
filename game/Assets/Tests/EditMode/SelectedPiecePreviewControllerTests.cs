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
    public void ShowPiece_DefaultCameraFitsTallCharacterHeight()
    {
        GameObject owner = new GameObject("Preview Owner");
        GameObject source = GameObject.CreatePrimitive(PrimitiveType.Cube);
        try
        {
            source.transform.localScale = new Vector3(0.5f, 3.5f, 0.5f);

            RawImage image = owner.AddComponent<RawImage>();
            SelectedPiecePreviewController preview = owner.AddComponent<SelectedPiecePreviewController>();
            PieceView piece = source.AddComponent<PieceView>();
            piece.Initialize(new VisualPieceState(BoardSquare.FromAlgebraic("d1"), ChessSide.White, ChessPieceKind.Queen));

            preview.Configure(image);
            preview.ShowPiece(piece);

            const float expectedFittedCharacterHeight = 1.55f;
            const float previewCameraFieldOfView = 24f;
            const float expectedSafetyMargin = 1.24f;
            float visibleVerticalSpan = 2f * preview.CurrentZoom * Mathf.Tan(previewCameraFieldOfView * 0.5f * Mathf.Deg2Rad);

            Assert.GreaterOrEqual(visibleVerticalSpan, expectedFittedCharacterHeight * expectedSafetyMargin);
        }
        finally
        {
            Object.DestroyImmediate(owner);
            Object.DestroyImmediate(source);
        }
    }

    [Test]
    public void ShowPiece_DefaultCameraFitsWideAndTallCharacter()
    {
        GameObject owner = new GameObject("Preview Owner");
        GameObject source = GameObject.CreatePrimitive(PrimitiveType.Cube);
        try
        {
            source.transform.localScale = new Vector3(1.6f, 3.2f, 0.8f);
            RawImage image = owner.AddComponent<RawImage>();
            SelectedPiecePreviewController preview = owner.AddComponent<SelectedPiecePreviewController>();
            PieceView piece = source.AddComponent<PieceView>();
            piece.Initialize(new VisualPieceState(BoardSquare.FromAlgebraic("d1"), ChessSide.White, ChessPieceKind.Queen));

            preview.Configure(image);
            preview.ShowPiece(piece);

            Assert.GreaterOrEqual(preview.CurrentZoom, 1.6f);
            Assert.LessOrEqual(preview.CurrentZoom, 8.5f);
            Assert.IsTrue(preview.HasPreview);
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
            Assert.LessOrEqual(preview.CurrentZoom, 8.5f);

            preview.Zoom(100f);

            Assert.GreaterOrEqual(preview.CurrentZoom, 1.6f);
        }
        finally
        {
            Object.DestroyImmediate(owner);
        }
    }
}
