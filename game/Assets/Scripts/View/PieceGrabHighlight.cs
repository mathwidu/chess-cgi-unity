using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public sealed class PieceGrabHighlight : MonoBehaviour
{
    private const string OutlineMaterialPath = "XR/GrabOutlineMaterial";
    private const float OutlineWidthPerPieceHeight = 0.03f;
    private const string CubeMeshName = "Cube";
    private const string ShellName = "GrabOutline";
    private static readonly int OutlineWidthId = Shader.PropertyToID("_OutlineWidth");
    private static readonly int RadialBlendId = Shader.PropertyToID("_RadialBlend");

    [Serializable]
    private sealed class Shell
    {
        public MeshFilter Source;
        public MeshFilter Filter;
        public MeshRenderer Renderer;
    }

    // Runtime-created bindings must survive Instantiate with references remapped
    // to the clone, rather than treating its existing shells as new source meshes.
    [SerializeField, HideInInspector] private List<Shell> shells = new List<Shell>();
    private ChessGameController gameController;
    private PieceView pieceView;
    private XRGrabInteractable interactable;
    private bool lit;
    private QuestCharacterDetail detail;
    private bool detailSubscribed;

    public bool IsLit => lit;

    private void Awake()
    {
        gameController = FindFirstObjectByType<ChessGameController>();
        pieceView = GetComponent<PieceView>();
        interactable = GetComponent<XRGrabInteractable>();
        BindDetail();
    }

    private void OnEnable()
    {
        BindDetail();
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
        // A cloned component starts with lit=false but may inherit visible shells.
        foreach (Shell shell in shells)
            if (shell.Filter != null) shell.Filter.gameObject.SetActive(false);
        if (detail != null && detailSubscribed) detail.MeshesChanged -= SynchronizeShells;
        detailSubscribed = false;
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
        BindDetail();
        if (detail != null) detail.RefreshForCamera(null);
        SetLit(interactable.isHovered && !interactable.isSelected && gameController != null && gameController.CanGrabPiece(pieceView));
    }

    private void BindDetail()
    {
        if (detail == null) detail = GetComponentInChildren<QuestCharacterDetail>(true);
        if (detail != null && !detailSubscribed && isActiveAndEnabled)
        {
            detail.MeshesChanged += SynchronizeShells;
            detailSubscribed = true;
        }
    }

    private void SynchronizeShells()
    {
        foreach (Shell shell in shells)
        {
            if (shell.Source == null || shell.Filter == null) continue;
            shell.Filter.sharedMesh = shell.Source.sharedMesh;
            shell.Renderer.localBounds = shell.Source.GetComponent<MeshRenderer>().localBounds;
        }
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

        foreach (Shell shell in shells)
        {
            if (shell.Filter != null) shell.Filter.gameObject.SetActive(lit);
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
            MeshRenderer sourceRenderer = source.GetComponent<MeshRenderer>();
            if (source.name == ShellName || mesh == null || sourceRenderer == null)
            {
                continue;
            }

            GameObject shell = new GameObject(ShellName);
            shell.layer = source.gameObject.layer;
            shell.transform.SetParent(source.transform, false);
            MeshFilter shellFilter = shell.AddComponent<MeshFilter>();
            shellFilter.sharedMesh = mesh;

            MeshRenderer renderer = shell.AddComponent<MeshRenderer>();
            Material[] materials = new Material[mesh.subMeshCount];
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i] = outline;
            }

            renderer.sharedMaterials = materials;
            renderer.localBounds = sourceRenderer.localBounds;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            MaterialPropertyBlock properties = new MaterialPropertyBlock();
            properties.SetFloat(OutlineWidthId, width);
            properties.SetFloat(RadialBlendId, mesh.name == CubeMeshName ? 1f : 0f);
            renderer.SetPropertyBlock(properties);
            shells.Add(new Shell { Source = source, Filter = shellFilter, Renderer = renderer });
        }
    }
}
