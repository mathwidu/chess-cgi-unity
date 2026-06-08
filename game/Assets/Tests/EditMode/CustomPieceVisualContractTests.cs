using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class CustomPieceVisualContractTests
{
    [TestCase("Pawn_Mathwidu_v3b")]
    [TestCase("Rook_Alex")]
    [TestCase("Knight_Gustavo")]
    [TestCase("Bishop_Rafael")]
    [TestCase("Queen_Marta")]
    [TestCase("King_Ricardo_Carioca")]
    public void CustomPrefab_ExistsAndHasRenderer(string prefabName)
    {
        GameObject prefab = LoadPrefab(prefabName);

        Assert.Greater(prefab.GetComponentsInChildren<Renderer>(true).Length, 0, prefabName);
    }

    [Test]
    public void ActivePawnPrefab_HasSemanticTeamOutfitCandidate()
    {
        GameObject prefab = LoadPrefab("Pawn_Mathwidu_v3b");

        Assert.IsTrue(HasSemanticOutfit(prefab), "Pawn_Mathwidu_v3b must keep the first approved TeamOutfit contract.");
    }

    [Test]
    public void PawnMathwiduSideVariants_ExistAndHaveDifferentAuthoredMaterials()
    {
        GameObject whitePrefab = LoadPrefab("Pawn_Mathwidu_White");
        GameObject blackPrefab = LoadPrefab("Pawn_Mathwidu_Black");

        Assert.Greater(whitePrefab.GetComponentsInChildren<Renderer>(true).Length, 0);
        Assert.Greater(blackPrefab.GetComponentsInChildren<Renderer>(true).Length, 0);

        Assert.IsTrue(HasSideAuthoredMaterial(whitePrefab.transform, "White"));
        Assert.IsTrue(HasSideAuthoredMaterial(blackPrefab.transform, "Black"));
        Assert.IsTrue(HasSideAuthoredTexture(whitePrefab.transform, "White"));
        Assert.IsTrue(HasSideAuthoredTexture(blackPrefab.transform, "Black"));
        Assert.IsTrue(HasNamedChild(whitePrefab.transform, "WeaponSocket"));
        Assert.IsTrue(HasNamedChild(blackPrefab.transform, "WeaponSocket"));
        Assert.IsTrue(HasNamedChild(whitePrefab.transform, "RightHandSocket"));
        Assert.IsTrue(HasNamedChild(blackPrefab.transform, "RightHandSocket"));
    }

    [TestCase(ChessPieceKind.Pawn, "Pawn_Mathwidu")]
    [TestCase(ChessPieceKind.Rook, "Rook_Alex")]
    [TestCase(ChessPieceKind.Knight, "Knight_Gustavo")]
    [TestCase(ChessPieceKind.Bishop, "Bishop_Rafael")]
    [TestCase(ChessPieceKind.Queen, "Queen_Marta")]
    [TestCase(ChessPieceKind.King, "King_Ricardo_Carioca")]
    public void SideSpecificCustomPrefabs_ExistAndHaveAuthoredTeamTextures(ChessPieceKind kind, string characterId)
    {
        GameObject whitePrefab = LoadPrefab($"{characterId}_White");
        GameObject blackPrefab = LoadPrefab($"{characterId}_Black");

        Assert.Greater(whitePrefab.GetComponentsInChildren<Renderer>(true).Length, 0, characterId);
        Assert.Greater(blackPrefab.GetComponentsInChildren<Renderer>(true).Length, 0, characterId);

        Assert.IsTrue(HasSideAuthoredMaterial(whitePrefab.transform, characterId, "White"), characterId);
        Assert.IsTrue(HasSideAuthoredMaterial(blackPrefab.transform, characterId, "Black"), characterId);
        Assert.IsTrue(HasSideAuthoredTexture(whitePrefab.transform, characterId, "White"), characterId);
        Assert.IsTrue(HasSideAuthoredTexture(blackPrefab.transform, characterId, "Black"), characterId);
        Assert.IsTrue(HasNamedChild(whitePrefab.transform, "WeaponSocket"), characterId);
        Assert.IsTrue(HasNamedChild(blackPrefab.transform, "WeaponSocket"), characterId);
    }

    [TestCase(ChessPieceKind.Pawn, "Pawn_Mathwidu")]
    [TestCase(ChessPieceKind.Rook, "Rook_Alex")]
    [TestCase(ChessPieceKind.Knight, "Knight_Gustavo")]
    [TestCase(ChessPieceKind.Bishop, "Bishop_Rafael")]
    [TestCase(ChessPieceKind.Queen, "Queen_Marta")]
    [TestCase(ChessPieceKind.King, "King_Ricardo_Carioca")]
    public void PieceFactory_UsesSideSpecificCustomPrefabsForEveryPiece(ChessPieceKind kind, string characterId)
    {
        GameObject rig = new GameObject("All Side Variants Probe");
        try
        {
            PieceFactory factory = rig.AddComponent<PieceFactory>();
            factory.ConfigureCustomPrefab(kind, ChessSide.White, LoadPrefab($"{characterId}_White"));
            factory.ConfigureCustomPrefab(kind, ChessSide.Black, LoadPrefab($"{characterId}_Black"));

            PieceView whitePiece = factory.CreatePiece(
                new VisualPieceState(BoardSquare.FromAlgebraic("a2"), ChessSide.White, kind),
                Vector3.zero,
                rig.transform);
            PieceView blackPiece = factory.CreatePiece(
                new VisualPieceState(BoardSquare.FromAlgebraic("a7"), ChessSide.Black, kind),
                Vector3.right * 2f,
                rig.transform);

            Assert.IsTrue(HasSideAuthoredMaterial(whitePiece.VisualRoot, characterId, "White"), characterId);
            Assert.IsTrue(HasSideAuthoredMaterial(blackPiece.VisualRoot, characterId, "Black"), characterId);
            Assert.IsNull(whitePiece.VisualRoot.Find("TeamOutfitPrimary_RuntimeUniform"), characterId);
            Assert.IsNull(blackPiece.VisualRoot.Find("TeamOutfitPrimary_RuntimeUniform"), characterId);
        }
        finally
        {
            Object.DestroyImmediate(rig);
        }
    }

    [Test]
    public void PieceFactory_UsesPawnMathwiduSideVariantsWhenConfigured()
    {
        GameObject rig = new GameObject("Pawn Variant Probe");
        try
        {
            PieceFactory factory = rig.AddComponent<PieceFactory>();
            factory.ConfigureCustomPrefab(ChessPieceKind.Pawn, LoadPrefab("Pawn_Mathwidu_v3b"));
            factory.ConfigureCustomPrefab(ChessPieceKind.Pawn, ChessSide.White, LoadPrefab("Pawn_Mathwidu_White"));
            factory.ConfigureCustomPrefab(ChessPieceKind.Pawn, ChessSide.Black, LoadPrefab("Pawn_Mathwidu_Black"));

            PieceView whitePiece = factory.CreatePiece(
                new VisualPieceState(BoardSquare.FromAlgebraic("a2"), ChessSide.White, ChessPieceKind.Pawn),
                Vector3.zero,
                rig.transform);
            PieceView blackPiece = factory.CreatePiece(
                new VisualPieceState(BoardSquare.FromAlgebraic("a7"), ChessSide.Black, ChessPieceKind.Pawn),
                Vector3.right * 2f,
                rig.transform);

            Assert.IsTrue(HasSideAuthoredMaterial(whitePiece.VisualRoot, "White"));
            Assert.IsTrue(HasSideAuthoredMaterial(blackPiece.VisualRoot, "Black"));
            Assert.IsNull(whitePiece.VisualRoot.Find("TeamOutfitPrimary_RuntimeUniform"));
            Assert.IsNull(blackPiece.VisualRoot.Find("TeamOutfitPrimary_RuntimeUniform"));
        }
        finally
        {
            Object.DestroyImmediate(rig);
        }
    }

    [TestCase(ChessPieceKind.Pawn, "Pawn_Mathwidu_v3b")]
    [TestCase(ChessPieceKind.Rook, "Rook_Alex")]
    [TestCase(ChessPieceKind.Knight, "Knight_Gustavo")]
    [TestCase(ChessPieceKind.Bishop, "Bishop_Rafael")]
    [TestCase(ChessPieceKind.Queen, "Queen_Marta")]
    [TestCase(ChessPieceKind.King, "King_Ricardo_Carioca")]
    public void PieceFactory_CustomPieceDoesNotGenerateTeamBase(ChessPieceKind kind, string prefabName)
    {
        GameObject rig = new GameObject("Base Free Probe");
        try
        {
            PieceFactory factory = rig.AddComponent<PieceFactory>();
            factory.ConfigureCustomPrefab(kind, LoadPrefab(prefabName));

            PieceView piece = factory.CreatePiece(
                new VisualPieceState(BoardSquare.FromAlgebraic("a2"), ChessSide.White, kind),
                Vector3.zero,
                rig.transform);

            Assert.IsNull(piece.transform.Find("TeamBase"), prefabName);
            Assert.IsNotNull(piece.transform.Find("CustomVisual"), prefabName);
            Assert.IsNotNull(piece.VisualRoot, prefabName);
            Assert.IsNotNull(piece.GetComponent<Collider>(), prefabName);
            Assert.IsNotNull(piece.GetComponentInChildren<CharacterAnimationDriver>(true), prefabName);
            Assert.IsNotNull(piece.GetComponentInChildren<CharacterVisualContract>(true), prefabName);
            Assert.IsNotNull(piece.GetComponentInChildren<ModularCharacterRig>(true), prefabName);
        }
        finally
        {
            Object.DestroyImmediate(rig);
        }
    }

    [TestCase(ChessPieceKind.Pawn, "Pawn_Mathwidu_v3b")]
    [TestCase(ChessPieceKind.Rook, "Rook_Alex")]
    [TestCase(ChessPieceKind.Knight, "Knight_Gustavo")]
    [TestCase(ChessPieceKind.Bishop, "Bishop_Rafael")]
    [TestCase(ChessPieceKind.Queen, "Queen_Marta")]
    [TestCase(ChessPieceKind.King, "King_Ricardo_Carioca")]
    public void PieceFactory_TeamOutfitOrTintRecolorsForBothSides(ChessPieceKind kind, string prefabName)
    {
        GameObject rig = new GameObject("Team Outfit Runtime Probe");
        try
        {
            PieceFactory factory = rig.AddComponent<PieceFactory>();
            GameObject prefab = LoadPrefab(prefabName);
            bool hasSemanticOutfit = HasSemanticOutfit(prefab);
            factory.ConfigureCustomPrefab(kind, prefab);

            PieceView whitePiece = factory.CreatePiece(
                new VisualPieceState(BoardSquare.FromAlgebraic("a2"), ChessSide.White, kind),
                Vector3.zero,
                rig.transform);
            PieceView blackPiece = factory.CreatePiece(
                new VisualPieceState(BoardSquare.FromAlgebraic("a7"), ChessSide.Black, kind),
                Vector3.right * 2f,
                rig.transform);

            Color white = FindTeamReadableColor(whitePiece.transform);
            Color black = FindTeamReadableColor(blackPiece.transform);

            Assert.Greater(white.r, 0.75f, prefabName);
            Assert.Greater(white.g, 0.75f, prefabName);
            Assert.Less(black.r, hasSemanticOutfit ? 0.2f : 0.55f, prefabName);
            Assert.Less(black.g, hasSemanticOutfit ? 0.2f : 0.55f, prefabName);
            Assert.Less(black.b, hasSemanticOutfit ? 0.2f : 0.55f, prefabName);
            Assert.Greater(black.r, hasSemanticOutfit ? 0.01f : 0.18f, prefabName);
        }
        finally
        {
            Object.DestroyImmediate(rig);
        }
    }

    [TestCase(ChessPieceKind.Pawn, "Pawn_Mathwidu_v3b")]
    [TestCase(ChessPieceKind.Rook, "Rook_Alex")]
    [TestCase(ChessPieceKind.Knight, "Knight_Gustavo")]
    [TestCase(ChessPieceKind.Bishop, "Bishop_Rafael")]
    [TestCase(ChessPieceKind.Queen, "Queen_Marta")]
    [TestCase(ChessPieceKind.King, "King_Ricardo_Carioca")]
    public void PieceFactory_CustomPiecesDoNotCreateRuntimeUniformPanels(ChessPieceKind kind, string prefabName)
    {
        GameObject rig = new GameObject("Runtime Uniform Regression Probe");
        try
        {
            PieceFactory factory = rig.AddComponent<PieceFactory>();
            factory.ConfigureCustomPrefab(kind, LoadPrefab(prefabName));

            PieceView piece = factory.CreatePiece(
                new VisualPieceState(BoardSquare.FromAlgebraic("a2"), ChessSide.White, kind),
                Vector3.zero,
                rig.transform);

            Assert.IsNull(piece.VisualRoot.Find("TeamOutfitPrimary_RuntimeUniform"), prefabName);
        }
        finally
        {
            Object.DestroyImmediate(rig);
        }
    }

    public static IReadOnlyDictionary<ChessPieceKind, string> ActivePrefabs => new Dictionary<ChessPieceKind, string>
    {
        { ChessPieceKind.Pawn, "Pawn_Mathwidu_v3b" },
        { ChessPieceKind.Rook, "Rook_Alex" },
        { ChessPieceKind.Knight, "Knight_Gustavo" },
        { ChessPieceKind.Bishop, "Bishop_Rafael" },
        { ChessPieceKind.Queen, "Queen_Marta" },
        { ChessPieceKind.King, "King_Ricardo_Carioca" }
    };

    private static GameObject LoadPrefab(string prefabName)
    {
        GameObject prefab = Resources.Load<GameObject>($"CustomPieces/{prefabName}");
        Assert.IsNotNull(prefab, prefabName);
        return prefab;
    }

    private static bool HasSemanticOutfit(GameObject prefab)
    {
        foreach (Renderer renderer in prefab.GetComponentsInChildren<Renderer>(true))
        {
            if (HasSemanticOutfitToken(renderer.name))
            {
                return true;
            }

            foreach (Material material in renderer.sharedMaterials)
            {
                if (material != null && HasSemanticOutfitToken(material.name))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static Color FindTeamReadableColor(Transform root)
    {
        Color firstMaterialColor = Color.clear;
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            foreach (Material material in renderer.sharedMaterials)
            {
                if (material != null && firstMaterialColor == Color.clear)
                {
                    firstMaterialColor = material.color;
                }

                if (material != null && (HasSemanticOutfitToken(renderer.name) || HasSemanticOutfitToken(material.name)))
                {
                    return material.color;
                }
            }
        }

        return firstMaterialColor;
    }

    private static bool HasSideAuthoredMaterial(Transform root, string side)
    {
        return HasSideAuthoredMaterial(root, "Pawn_Mathwidu", side);
    }

    private static bool HasSideAuthoredMaterial(Transform root, string characterId, string side)
    {
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            foreach (Material material in renderer.sharedMaterials)
            {
                if (material != null && material.name.Contains($"{characterId}_{side}"))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool HasSideAuthoredTexture(Transform root, string side)
    {
        return HasSideAuthoredTexture(root, "Pawn_Mathwidu", side);
    }

    private static bool HasSideAuthoredTexture(Transform root, string characterId, string side)
    {
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            foreach (Material material in renderer.sharedMaterials)
            {
                if (material == null)
                {
                    continue;
                }

                Texture texture = material.mainTexture;
                if (texture != null && texture.name.Contains($"{characterId}_{side}_UniformTexture"))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool HasSemanticOutfitToken(string value)
    {
        return !string.IsNullOrEmpty(value) &&
            (value.Contains("TeamOutfit") || value.Contains("TeamClothes") || value.Contains("TeamUniform"));
    }

    private static bool HasNamedChild(Transform root, string childName)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == childName)
            {
                return true;
            }
        }

        return false;
    }
}
