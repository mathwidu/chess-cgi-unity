using NUnit.Framework;
using UnityEngine;

public class PieceFactoryTests
{
    [Test]
    public void CreatePiece_WhenCustomPawnPrefabIsConfigured_UsesCustomVisual()
    {
        GameObject rig = new GameObject("Piece Factory Test Rig");
        GameObject pawnPrefab = GameObject.CreatePrimitive(PrimitiveType.Cube);

        try
        {
            PieceFactory factory = rig.AddComponent<PieceFactory>();
            factory.ConfigureCustomPrefab(ChessPieceKind.Pawn, pawnPrefab);

            PieceView piece = factory.CreatePiece(
                new VisualPieceState(BoardSquare.FromAlgebraic("a2"), ChessSide.White, ChessPieceKind.Pawn),
                Vector3.zero,
                rig.transform);

            Assert.IsNotNull(piece.transform.Find("TeamBase"));
            Assert.IsNotNull(piece.transform.Find("CustomVisual"));
            Assert.IsNull(piece.transform.Find("Head"));
            Assert.AreEqual(ChessPieceKind.Pawn, piece.Kind);
            Assert.AreEqual(ChessSide.White, piece.Side);
        }
        finally
        {
            Object.DestroyImmediate(rig);
            Object.DestroyImmediate(pawnPrefab);
        }
    }

    [Test]
    public void CreatePiece_WhenNoCustomPrefabExists_UsesClassicPrimitiveShape()
    {
        GameObject rig = new GameObject("Piece Factory Test Rig");

        try
        {
            PieceFactory factory = rig.AddComponent<PieceFactory>();

            PieceView piece = factory.CreatePiece(
                new VisualPieceState(BoardSquare.FromAlgebraic("a7"), ChessSide.Black, ChessPieceKind.Pawn),
                Vector3.zero,
                rig.transform);

            Assert.IsNull(piece.transform.Find("CustomVisual"));
            Assert.IsNotNull(piece.transform.Find("Head"));
            Assert.AreEqual(ChessPieceKind.Pawn, piece.Kind);
            Assert.AreEqual(ChessSide.Black, piece.Side);
        }
        finally
        {
            Object.DestroyImmediate(rig);
        }
    }

    [Test]
    public void CreatePiece_CustomVisualIsNormalizedToBoardPieceHeight()
    {
        GameObject rig = new GameObject("Piece Factory Test Rig");
        GameObject pawnPrefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pawnPrefab.transform.localScale = new Vector3(1f, 3f, 1f);

        try
        {
            PieceFactory factory = rig.AddComponent<PieceFactory>();
            factory.ConfigureCustomPrefab(ChessPieceKind.Pawn, pawnPrefab);

            PieceView piece = factory.CreatePiece(
                new VisualPieceState(BoardSquare.FromAlgebraic("b2"), ChessSide.White, ChessPieceKind.Pawn),
                Vector3.zero,
                rig.transform);

            Transform customVisual = piece.transform.Find("CustomVisual");
            Renderer renderer = customVisual.GetComponentInChildren<Renderer>();

            Assert.AreEqual(1.15f, renderer.bounds.size.y, 0.02f);
            Assert.GreaterOrEqual(renderer.bounds.min.y, 0.13f);
        }
        finally
        {
            Object.DestroyImmediate(rig);
            Object.DestroyImmediate(pawnPrefab);
        }
    }

    [Test]
    public void CreatePiece_CustomBishopVisualIsTallerThanCustomPawn()
    {
        GameObject rig = new GameObject("Piece Factory Test Rig");
        GameObject bishopPrefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bishopPrefab.transform.localScale = new Vector3(1f, 3f, 1f);

        try
        {
            PieceFactory factory = rig.AddComponent<PieceFactory>();
            factory.ConfigureCustomPrefab(ChessPieceKind.Bishop, bishopPrefab);

            PieceView piece = factory.CreatePiece(
                new VisualPieceState(BoardSquare.FromAlgebraic("c1"), ChessSide.White, ChessPieceKind.Bishop),
                Vector3.zero,
                rig.transform);

            Transform customVisual = piece.transform.Find("CustomVisual");
            Renderer renderer = customVisual.GetComponentInChildren<Renderer>();

            Assert.AreEqual(1.31f, renderer.bounds.size.y, 0.02f);
            Assert.GreaterOrEqual(renderer.bounds.min.y, 0.13f);
        }
        finally
        {
            Object.DestroyImmediate(rig);
            Object.DestroyImmediate(bishopPrefab);
        }
    }

    [Test]
    public void CreatePiece_CustomVisualFacesForwardTowardOpponent()
    {
        GameObject rig = new GameObject("Piece Factory Test Rig");
        GameObject pawnPrefab = GameObject.CreatePrimitive(PrimitiveType.Cube);

        try
        {
            PieceFactory factory = rig.AddComponent<PieceFactory>();
            factory.ConfigureCustomPrefab(ChessPieceKind.Pawn, pawnPrefab);

            PieceView whitePiece = factory.CreatePiece(
                new VisualPieceState(BoardSquare.FromAlgebraic("c2"), ChessSide.White, ChessPieceKind.Pawn),
                Vector3.zero,
                rig.transform);
            PieceView blackPiece = factory.CreatePiece(
                new VisualPieceState(BoardSquare.FromAlgebraic("c7"), ChessSide.Black, ChessPieceKind.Pawn),
                Vector3.zero,
                rig.transform);

            Assert.AreEqual(0f, whitePiece.transform.Find("CustomVisual").localEulerAngles.y, 0.01f);
            Assert.AreEqual(180f, blackPiece.transform.Find("CustomVisual").localEulerAngles.y, 0.01f);
        }
        finally
        {
            Object.DestroyImmediate(rig);
            Object.DestroyImmediate(pawnPrefab);
        }
    }

    [Test]
    public void CreatePiece_CustomVisualHasNamedVisualRootAndTeamBase()
    {
        GameObject rig = new GameObject("Piece Factory Test Rig");
        GameObject prefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
        try
        {
            PieceFactory factory = rig.AddComponent<PieceFactory>();
            factory.ConfigureCustomPrefab(ChessPieceKind.Rook, prefab);

            PieceView piece = factory.CreatePiece(
                new VisualPieceState(BoardSquare.FromAlgebraic("a1"), ChessSide.White, ChessPieceKind.Rook),
                Vector3.zero,
                rig.transform);

            Assert.IsNotNull(piece.transform.Find("TeamBase"));
            Assert.IsNotNull(piece.transform.Find("CustomVisual"));
            Assert.IsNotNull(piece.VisualRoot);
            Assert.AreEqual("CustomVisual", piece.VisualRoot.name);
        }
        finally
        {
            Object.DestroyImmediate(rig);
            Object.DestroyImmediate(prefab);
        }
    }

    [Test]
    public void CreatePiece_CustomVisualHasAnimationDriverExtensionPoint()
    {
        GameObject rig = new GameObject("Piece Factory Test Rig");
        GameObject prefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
        try
        {
            PieceFactory factory = rig.AddComponent<PieceFactory>();
            factory.ConfigureCustomPrefab(ChessPieceKind.King, prefab);

            PieceView piece = factory.CreatePiece(
                new VisualPieceState(BoardSquare.FromAlgebraic("e1"), ChessSide.White, ChessPieceKind.King),
                Vector3.zero,
                rig.transform);

            CharacterAnimationDriver driver = piece.VisualRoot.GetComponent<CharacterAnimationDriver>();

            Assert.IsNotNull(driver);
            Assert.IsFalse(driver.HasAnimator);
        }
        finally
        {
            Object.DestroyImmediate(rig);
            Object.DestroyImmediate(prefab);
        }
    }
}
