using System;
using UnityEngine;

/// <summary>Chooses shared team materials on the same authored character mesh.</summary>
public sealed class CustomPieceAppearance : MonoBehaviour
{
    [Serializable]
    public sealed class MaterialBinding
    {
        public Renderer Renderer;
        public Material[] White;
        public Material[] Black;
    }

    [SerializeField] private MaterialBinding[] bindings = Array.Empty<MaterialBinding>();
    [SerializeField] private ChessSide side;

    public ChessSide Side => side;

    public void Configure(MaterialBinding[] materialBindings)
    {
        bindings = materialBindings ?? Array.Empty<MaterialBinding>();
        ApplySide(ChessSide.White);
    }

    public void ApplySide(ChessSide value)
    {
        side = value;
        foreach (MaterialBinding binding in bindings)
        {
            if (binding.Renderer != null)
            {
                // Assign references, never tint a shared asset or allocate per-piece materials.
                binding.Renderer.sharedMaterials = value == ChessSide.White ? binding.White : binding.Black;
            }
        }
    }
}
