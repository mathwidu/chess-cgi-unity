using NUnit.Framework;
using UnityEngine;

public class TeamOutfitApplierTests
{
    [Test]
    public void ApplyTo_RecolorsOnlySemanticOutfitMaterials()
    {
        GameObject character = new GameObject("Character");
        GameObject shirt = GameObject.CreatePrimitive(PrimitiveType.Cube);
        GameObject face = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Material outfit = new Material(Shader.Find("Standard"));
        Material skin = new Material(Shader.Find("Standard"));

        try
        {
            shirt.name = "Shirt";
            shirt.transform.SetParent(character.transform, false);
            outfit.name = "TeamOutfitPrimary";
            outfit.color = Color.red;
            shirt.GetComponent<Renderer>().sharedMaterial = outfit;

            face.name = "Face";
            face.transform.SetParent(character.transform, false);
            skin.name = "SkinMaterial";
            skin.color = new Color(0.8f, 0.55f, 0.42f);
            face.GetComponent<Renderer>().sharedMaterial = skin;

            int changed = TeamOutfitApplier.ApplyTo(character.transform, ChessSide.White);

            Assert.AreEqual(1, changed);
            Color outfitColor = shirt.GetComponent<Renderer>().sharedMaterial.color;
            Color skinColor = face.GetComponent<Renderer>().sharedMaterial.color;

            Assert.Greater(outfitColor.r, 0.75f);
            Assert.Greater(outfitColor.g, 0.75f);
            Assert.AreEqual(0.8f, skinColor.r, 0.001f);
            Assert.AreEqual(0.55f, skinColor.g, 0.001f);
            Assert.AreEqual(0.42f, skinColor.b, 0.001f);
        }
        finally
        {
            Object.DestroyImmediate(character);
            Object.DestroyImmediate(outfit);
            Object.DestroyImmediate(skin);
        }
    }

    [Test]
    public void ApplyTo_UsesDarkOutfitForBlackSide()
    {
        GameObject character = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Material outfit = new Material(Shader.Find("Standard"));

        try
        {
            character.name = "TeamOutfitTorso";
            outfit.name = "NeutralMaterial";
            outfit.color = Color.white;
            character.GetComponent<Renderer>().sharedMaterial = outfit;

            int changed = TeamOutfitApplier.ApplyTo(character.transform, ChessSide.Black);

            Assert.AreEqual(1, changed);
            Color outfitColor = character.GetComponent<Renderer>().sharedMaterial.color;
            Assert.Less(outfitColor.r, 0.2f);
            Assert.Less(outfitColor.g, 0.2f);
            Assert.Less(outfitColor.b, 0.2f);
        }
        finally
        {
            Object.DestroyImmediate(character);
            Object.DestroyImmediate(outfit);
        }
    }

    [Test]
    public void ApplyToOrCreateAccent_TintsSingleMaterialWithoutAddingRuntimeGeometry()
    {
        GameObject character = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Material neutral = new Material(Shader.Find("Standard"));

        try
        {
            character.name = "SingleMaterialCharacter";
            neutral.name = "SingleNeutralMaterial";
            character.GetComponent<Renderer>().sharedMaterial = neutral;

            int changed = TeamOutfitApplier.ApplyToOrCreateAccent(character.transform, ChessSide.Black);

            Color outfitColor = character.GetComponent<Renderer>().sharedMaterial.color;
            Assert.AreEqual(1, changed);
            Assert.IsNull(character.transform.Find("TeamOutfitPrimary_RuntimeUniform"));
            Assert.AreEqual(1, character.GetComponentsInChildren<Renderer>(true).Length);
            Assert.Less(outfitColor.r, 0.55f);
            Assert.Less(outfitColor.g, 0.55f);
            Assert.Less(outfitColor.b, 0.55f);
            Assert.Greater(outfitColor.r, 0.18f);
        }
        finally
        {
            Object.DestroyImmediate(character);
            Object.DestroyImmediate(neutral);
        }
    }
}
