using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public sealed class PieceGrabHighlight : MonoBehaviour
{
    private const string OutlineMaterialPath = "XR/GrabOutlineMaterial";
    private const float OutlineWidthPerPieceHeight = 0.03f;
    private const string CubeMeshName = "Cube";
    private static readonly int OutlineWidthId = Shader.PropertyToID("_OutlineWidth");
    private static readonly int RadialBlendId = Shader.PropertyToID("_RadialBlend");

    private readonly List<GameObject> shells = new List<GameObject>();
    private ChessGameController gameController;
    private PieceView pieceView;
    private XRGrabInteractable interactable;
    private bool lit;

    public bool IsLit => lit;

    private void Awake()
    {
        gameController = FindFirstObjectByType<ChessGameController>();
        pieceView = GetComponent<PieceView>();
        interactable = GetComponent<XRGrabInteractable>();
    }

    private void OnEnable()
    {
        if (interactable == null)
        {
            return;
        }

        interactable.hoverEntered.AddListener(OnInteractionChanged);
        interactable.hoverExited.AddListener(OnInteractionChanged);
        interactable.selectEntered.AddListener(OnInteractionChanged);
        interactable.selectExited.AddListener(OnInteractionChanged);
    }

    private void OnDisable()
    {
        SetLit(false);
        if (interactable == null)
        {
            return;
        }

        interactable.hoverEntered.RemoveListener(OnInteractionChanged);
        interactable.hoverExited.RemoveListener(OnInteractionChanged);
        interactable.selectEntered.RemoveListener(OnInteractionChanged);
        interactable.selectExited.RemoveListener(OnInteractionChanged);
    }

    private void Update()
    {
        if (interactable != null && (lit || interactable.isHovered))
        {
            Refresh();
        }
    }

    private void OnInteractionChanged(BaseInteractionEventArgs args)
    {
        Refresh();
    }

    private void Refresh()
    {
        SetLit(interactable.isHovered && !interactable.isSelected && gameController != null && gameController.CanGrabPiece(pieceView));
    }

    private void SetLit(bool value)
    {
        if (value == lit)
        {
            return;
        }

        lit = value;
        if (lit && shells.Count == 0)
        {
            BuildShells();
        }

        foreach (GameObject shell in shells)
        {
            shell.SetActive(lit);
        }
    }

    private void BuildShells()
    {
        Material outline = Resources.Load<Material>(OutlineMaterialPath);
        if (outline == null)
        {
            return;
        }

        float width = OutlineWidthPerPieceHeight * transform.lossyScale.y;

        foreach (MeshFilter source in GetComponentsInChildren<MeshFilter>())
        {
            Mesh mesh = source.sharedMesh;
            if (mesh == null || source.GetComponent<MeshRenderer>() == null)
            {
                continue;
            }

            GameObject shell = new GameObject("GrabOutline");
            shell.layer = source.gameObject.layer;
            shell.transform.SetParent(source.transform, false);
            shell.AddComponent<MeshFilter>().sharedMesh = mesh;

            MeshRenderer renderer = shell.AddComponent<MeshRenderer>();
            Material[] materials = new Material[mesh.subMeshCount];
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i] = outline;
            }

            renderer.sharedMaterials = materials;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            MaterialPropertyBlock properties = new MaterialPropertyBlock();
            properties.SetFloat(OutlineWidthId, width);
            properties.SetFloat(RadialBlendId, mesh.name == CubeMeshName ? 1f : 0f);
            renderer.SetPropertyBlock(properties);
            shells.Add(shell);
        }
    }
}
