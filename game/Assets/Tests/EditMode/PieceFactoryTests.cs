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

            Assert.IsNull(piece.transform.Find("TeamBase"));
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
    public void CreatePiece_WhenSideSpecificPawnPrefabsAreConfigured_UsesMatchingSideVisual()
    {
        GameObject rig = new GameObject("Piece Factory Test Rig");
        GameObject whitePrefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
        GameObject blackPrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Material whiteAuthoredMaterial = new Material(Shader.Find("Standard"));
        Material blackAuthoredMaterial = new Material(Shader.Find("Standard"));

        try
        {
            whiteAuthoredMaterial.name = "Pawn_Mathwidu_White_Authored";
            blackAuthoredMaterial.name = "Pawn_Mathwidu_Black_Authored";
            whitePrefab.GetComponent<Renderer>().sharedMaterial = whiteAuthoredMaterial;
            blackPrefab.GetComponent<Renderer>().sharedMaterial = blackAuthoredMaterial;

            PieceFactory factory = rig.AddComponent<PieceFactory>();
            factory.ConfigureCustomPrefab(ChessPieceKind.Pawn, ChessSide.White, whitePrefab);
            factory.ConfigureCustomPrefab(ChessPieceKind.Pawn, ChessSide.Black, blackPrefab);

            PieceView whitePiece = factory.CreatePiece(
                new VisualPieceState(BoardSquare.FromAlgebraic("a2"), ChessSide.White, ChessPieceKind.Pawn),
                Vector3.zero,
                rig.transform);
            PieceView blackPiece = factory.CreatePiece(
                new VisualPieceState(BoardSquare.FromAlgebraic("a7"), ChessSide.Black, ChessPieceKind.Pawn),
                Vector3.right,
                rig.transform);

            Assert.AreEqual(
                "Pawn_Mathwidu_White_Authored",
                whitePiece.VisualRoot.GetComponentInChildren<Renderer>().sharedMaterial.name);
            Assert.AreEqual(
                "Pawn_Mathwidu_Black_Authored",
                blackPiece.VisualRoot.GetComponentInChildren<Renderer>().sharedMaterial.name);
        }
        finally
        {
            Object.DestroyImmediate(rig);
            Object.DestroyImmediate(whitePrefab);
            Object.DestroyImmediate(blackPrefab);
            Object.DestroyImmediate(whiteAuthoredMaterial);
            Object.DestroyImmediate(blackAuthoredMaterial);
        }
    }

    [Test]
    public void CreatePiece_WhenSideSpecificPrefabHasSingleMaterial_DoesNotApplyFallbackTint()
    {
        GameObject rig = new GameObject("Piece Factory Test Rig");
        GameObject blackPrefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Material authoredMaterial = new Material(Shader.Find("Standard"));

        try
        {
            authoredMaterial.name = "SingleNeutralMaterial";
            authoredMaterial.color = Color.gray;
            blackPrefab.GetComponent<Renderer>().sharedMaterial = authoredMaterial;

            PieceFactory factory = rig.AddComponent<PieceFactory>();
            factory.ConfigureCustomPrefab(ChessPieceKind.Pawn, ChessSide.Black, blackPrefab);

            PieceView blackPiece = factory.CreatePiece(
                new VisualPieceState(BoardSquare.FromAlgebraic("a7"), ChessSide.Black, ChessPieceKind.Pawn),
                Vector3.zero,
                rig.transform);

            Material material = blackPiece.VisualRoot.GetComponentInChildren<Renderer>().sharedMaterial;

            Assert.IsFalse(material.name.Contains("ReadableTint"));
            Assert.AreEqual(Color.gray.r, material.color.r, 0.001f);
            Assert.AreEqual(Color.gray.g, material.color.g, 0.001f);
            Assert.AreEqual(Color.gray.b, material.color.b, 0.001f);
        }
        finally
        {
            Object.DestroyImmediate(rig);
            Object.DestroyImmediate(blackPrefab);
            Object.DestroyImmediate(authoredMaterial);
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
            Assert.GreaterOrEqual(renderer.bounds.min.y, 0.01f);
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
            Assert.GreaterOrEqual(renderer.bounds.min.y, 0.01f);
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

            Assert.AreEqual(0f, whitePiece.transform.eulerAngles.y, 0.01f);
            Assert.AreEqual(0f, whitePiece.transform.Find("CustomVisual").localEulerAngles.y, 0.01f);
            Assert.AreEqual(180f, blackPiece.transform.eulerAngles.y, 0.01f);
            Assert.AreEqual(0f, blackPiece.transform.Find("CustomVisual").localEulerAngles.y, 0.01f);

            blackPiece.FaceTowards(new Vector3(0f, 0f, -2f));

            Assert.Greater(Vector3.Dot(blackPiece.VisualRoot.forward, Vector3.back), 0.95f);
        }
        finally
        {
            Object.DestroyImmediate(rig);
            Object.DestroyImmediate(pawnPrefab);
        }
    }

    [Test]
    public void CreatePiece_CustomVisualHasNamedVisualRootWithoutTeamBase()
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

            Assert.IsNull(piece.transform.Find("TeamBase"));
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

    [Test]
    public void CreatePiece_CustomVisualHasAnimationContract()
    {
        GameObject rig = new GameObject("Piece Factory Test Rig");
        GameObject prefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
        try
        {
            PieceFactory factory = rig.AddComponent<PieceFactory>();
            factory.ConfigureCustomPrefab(ChessPieceKind.Queen, prefab);

            PieceView piece = factory.CreatePiece(
                new VisualPieceState(BoardSquare.FromAlgebraic("d1"), ChessSide.White, ChessPieceKind.Queen),
                Vector3.zero,
                rig.transform);

            CharacterVisualContract contract = piece.VisualRoot.GetComponent<CharacterVisualContract>();

            Assert.IsNotNull(contract);
            Assert.AreEqual(ChessPieceKind.Queen, contract.PieceKind);
            Assert.AreEqual(CharacterRigStatus.StaticMesh, contract.RigStatus);
            Assert.IsNotNull(contract.EffectsSocket);
            Assert.IsNotNull(contract.HitSocket);
            Assert.IsNotNull(contract.GroundSocket);
        }
        finally
        {
            Object.DestroyImmediate(rig);
            Object.DestroyImmediate(prefab);
        }
    }

    [Test]
    public void CreatePiece_CustomVisualHasModularRigExtensionPoint()
    {
        GameObject rig = new GameObject("Piece Factory Test Rig");
        GameObject prefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
        try
        {
            PieceFactory factory = rig.AddComponent<PieceFactory>();
            factory.ConfigureCustomPrefab(ChessPieceKind.Pawn, prefab);

            PieceView piece = factory.CreatePiece(
                new VisualPieceState(BoardSquare.FromAlgebraic("e2"), ChessSide.White, ChessPieceKind.Pawn),
                Vector3.zero,
                rig.transform);

            ModularCharacterRig modularRig = piece.VisualRoot.GetComponent<ModularCharacterRig>();

            Assert.IsNotNull(modularRig);
        }
        finally
        {
            Object.DestroyImmediate(rig);
            Object.DestroyImmediate(prefab);
        }
    }

    [Test]
    public void CreatePiece_CustomVisualAppliesSemanticTeamOutfit()
    {
        GameObject rig = new GameObject("Piece Factory Test Rig");
        GameObject prefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Material outfitMaterial = new Material(Shader.Find("Standard"));
        outfitMaterial.name = "TeamOutfitPrimary";
        outfitMaterial.color = Color.red;

        try
        {
            prefab.GetComponent<Renderer>().sharedMaterial = outfitMaterial;
            PieceFactory factory = rig.AddComponent<PieceFactory>();
            factory.ConfigureCustomPrefab(ChessPieceKind.Pawn, prefab);

            PieceView whitePiece = factory.CreatePiece(
                new VisualPieceState(BoardSquare.FromAlgebraic("a2"), ChessSide.White, ChessPieceKind.Pawn),
                Vector3.zero,
                rig.transform);
            PieceView blackPiece = factory.CreatePiece(
                new VisualPieceState(BoardSquare.FromAlgebraic("a7"), ChessSide.Black, ChessPieceKind.Pawn),
                Vector3.right,
                rig.transform);

            Color whiteColor = whitePiece.VisualRoot.GetComponentInChildren<Renderer>().sharedMaterial.color;
            Color blackColor = blackPiece.VisualRoot.GetComponentInChildren<Renderer>().sharedMaterial.color;

            Assert.Greater(whiteColor.r, 0.75f);
            Assert.Greater(whiteColor.g, 0.75f);
            Assert.Less(blackColor.r, 0.2f);
            Assert.Less(blackColor.g, 0.2f);
        }
        finally
        {
            Object.DestroyImmediate(rig);
            Object.DestroyImmediate(prefab);
            Object.DestroyImmediate(outfitMaterial);
        }
    }
}
