using UnityEngine;

public sealed class TeamOutfitApplier : MonoBehaviour
{
    private static readonly Color WhiteOutfit = new Color(0.92f, 0.9f, 0.84f, 1f);
    private static readonly Color BlackOutfit = new Color(0.06f, 0.07f, 0.08f, 1f);
    private static readonly Color WhiteFallbackTint = new Color(0.96f, 0.95f, 0.9f, 1f);
    private static readonly Color BlackFallbackTint = new Color(0.34f, 0.36f, 0.38f, 1f);
    private static readonly string[] SemanticOutfitTokens =
    {
        "TeamOutfit",
        "TeamClothes",
        "TeamUniform"
    };
    private static readonly string[] FallbackTintSkipTokens =
    {
        "Skin",
        "Face",
        "Hair",
        "Eye",
        "Glass",
        "Glasses",
        "Beard",
        "Mouth",
        "Teeth"
    };

    public int Apply(ChessSide side)
    {
        return ApplyTo(transform, side);
    }

    public static int ApplyTo(Transform root, ChessSide side)
    {
        if (root == null)
        {
            return 0;
        }

        int changedCount = 0;
        Color targetColor = side == ChessSide.White ? WhiteOutfit : BlackOutfit;
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer renderer in renderers)
        {
            Material[] materials = renderer.sharedMaterials;
            bool rendererChanged = false;
            for (int i = 0; i < materials.Length; i++)
            {
                Material material = materials[i];
                if (material == null || !IsSemanticOutfit(renderer, material))
                {
                    continue;
                }

                Material recolored = new Material(material)
                {
                    name = $"{material.name}_{side}Outfit"
                };
                RuntimeMaterialFactory.ApplyColor(recolored, targetColor);
                materials[i] = recolored;
                changedCount++;
                rendererChanged = true;
            }

            if (rendererChanged)
            {
                renderer.sharedMaterials = materials;
            }
        }

        return changedCount;
    }

    public static int ApplyToOrCreateAccent(Transform root, ChessSide side)
    {
        int changedCount = ApplyTo(root, side);
        if (changedCount > 0 || root == null)
        {
            return changedCount;
        }

        return ApplyFallbackTint(root, side);
    }

    private static int ApplyFallbackTint(Transform root, ChessSide side)
    {
        int changedCount = 0;
        Color targetColor = side == ChessSide.White ? WhiteFallbackTint : BlackFallbackTint;
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer renderer in renderers)
        {
            Material[] materials = renderer.sharedMaterials;
            bool rendererChanged = false;
            for (int i = 0; i < materials.Length; i++)
            {
                Material material = materials[i];
                if (material == null || ShouldSkipFallbackTint(renderer, material))
                {
                    continue;
                }

                Material recolored = new Material(material)
                {
                    name = $"{material.name}_{side}ReadableTint"
                };
                RuntimeMaterialFactory.ApplyColor(recolored, targetColor);
                materials[i] = recolored;
                changedCount++;
                rendererChanged = true;
            }

            if (rendererChanged)
            {
                renderer.sharedMaterials = materials;
            }
        }

        return changedCount;
    }

    private static bool IsSemanticOutfit(Renderer renderer, Material material)
    {
        return ContainsAnyToken(renderer.name) || ContainsAnyToken(material.name);
    }

    private static bool ShouldSkipFallbackTint(Renderer renderer, Material material)
    {
        return ContainsAnyToken(renderer.name, FallbackTintSkipTokens) ||
            ContainsAnyToken(material.name, FallbackTintSkipTokens);
    }

    private static bool ContainsAnyToken(string value)
    {
        return ContainsAnyToken(value, SemanticOutfitTokens);
    }

    private static bool ContainsAnyToken(string value, string[] tokens)
    {
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        foreach (string token in tokens)
        {
            if (value.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }
}
