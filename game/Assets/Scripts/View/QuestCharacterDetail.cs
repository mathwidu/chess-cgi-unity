using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>Changes only shared meshes, retaining one renderer and the authored bounds per part.</summary>
public sealed class QuestCharacterDetail : MonoBehaviour
{
    public const float InspectionEnterRatio = .45f;
    public const float TableEnterRatio = .32f;
    private const float CheckInterval = .15f;

    // Serialize runtime bindings so Instantiate remaps the filters/renderers into
    // the clone while preserving references to the two shared mesh sets.
    [SerializeField, HideInInspector] private MeshFilter[] filters;
    [SerializeField, HideInInspector] private MeshRenderer[] renderers;
    [SerializeField, HideInInspector] private Mesh[] inspectionMeshes;
    [SerializeField, HideInInspector] private Mesh[] tableMeshes;
    [SerializeField, HideInInspector] private Bounds[] authoredBounds;
    [SerializeField, HideInInspector] private Bounds localBounds;
    [SerializeField, HideInInspector] private bool inspection = true;
    private XRGrabInteractable interactable;
    private float nextCheck;

    public bool IsInspectionDetail => inspection;
    public event Action MeshesChanged;

    private void Awake() => interactable = GetComponentInParent<XRGrabInteractable>();

    private void OnEnable()
    {
        // Spread the checks across frames without changing global random state.
        nextCheck = Time.unscaledTime + (GetInstanceID() & 7) * (CheckInterval / 8f);
    }

    public bool Configure(MeshFilter[] targets, Mesh[] high, Mesh[] low)
    {
        if (!Compatible(targets, high, low)) return false;
        filters = (MeshFilter[])targets.Clone();
        inspectionMeshes = (Mesh[])high.Clone();
        tableMeshes = (Mesh[])low.Clone();
        renderers = new MeshRenderer[filters.Length];
        authoredBounds = new Bounds[filters.Length];
        bool firstPoint = true;
        for (int i = 0; i < filters.Length; i++)
        {
            renderers[i] = filters[i].GetComponent<MeshRenderer>();
            authoredBounds[i] = renderers[i].localBounds;
            Matrix4x4 toLocal = transform.worldToLocalMatrix * renderers[i].localToWorldMatrix;
            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 sign = new Vector3((corner & 1) == 0 ? -1 : 1,
                    (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1);
                Vector3 point = toLocal.MultiplyPoint3x4(authoredBounds[i].center + Vector3.Scale(authoredBounds[i].extents, sign));
                if (firstPoint) { localBounds = new Bounds(point, Vector3.zero); firstPoint = false; }
                else localBounds.Encapsulate(point);
            }
        }
        interactable = GetComponentInParent<XRGrabInteractable>();
        Apply(true);
        enabled = true;
        return true;
    }

    private static bool Compatible(MeshFilter[] targets, Mesh[] high, Mesh[] low)
    {
        if (targets == null || high == null || low == null || targets.Length == 0 ||
            high.Length != targets.Length || low.Length != targets.Length) return false;
        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] == null || targets[i].GetComponent<MeshRenderer>() == null ||
                high[i] == null || low[i] == null || high[i].vertexCount == 0 || low[i].vertexCount == 0 ||
                high[i].subMeshCount != low[i].subMeshCount ||
                (high[i].HasVertexAttribute(VertexAttribute.Normal) && !low[i].HasVertexAttribute(VertexAttribute.Normal)) ||
                (high[i].HasVertexAttribute(VertexAttribute.TexCoord0) && !low[i].HasVertexAttribute(VertexAttribute.TexCoord0))) return false;
            float tolerance = .025f + high[i].bounds.size.magnitude * .05f;
            if (Vector3.Distance(high[i].bounds.center, low[i].bounds.center) > tolerance ||
                Vector3.Distance(high[i].bounds.size, low[i].bounds.size) > tolerance) return false;
        }
        return true;
    }

    private void Update()
    {
        if (Time.unscaledTime < nextCheck) return;
        nextCheck = Time.unscaledTime + CheckInterval;
        Camera camera = XRRig.EyeCamera;
        if (camera == null || !camera.isActiveAndEnabled) camera = Camera.main;
        RefreshForCamera(camera);
    }

    public void RefreshForCamera(Camera camera)
    {
        if (!enabled || filters == null) return;
        if (interactable != null && (interactable.isHovered || interactable.isSelected))
        {
            Apply(true);
            return;
        }
        if (camera == null) return;
        Vector3 scale = transform.lossyScale;
        float diameter = 2f * localBounds.extents.magnitude *
            Mathf.Max(Mathf.Abs(scale.x), Mathf.Max(Mathf.Abs(scale.y), Mathf.Abs(scale.z)));
        float distance = Vector3.Distance(camera.transform.position, transform.TransformPoint(localBounds.center));
        float ratio = diameter / Mathf.Max(.0001f, distance);
        if (ratio >= InspectionEnterRatio) Apply(true);
        else if (ratio <= TableEnterRatio) Apply(false);
    }

    // Explicit inspection wins over hover, camera polling and the cloned source's current LOD.
    public void ForceInspectionDetail()
    {
        Apply(true);
        enabled = false;
    }

    private void Apply(bool high)
    {
        if (filters == null) return;
        Mesh[] meshes = high ? inspectionMeshes : tableMeshes;
        // Check every binding before changing any part of the character.
        for (int i = 0; i < filters.Length; i++)
            if (filters[i] == null || renderers[i] == null || meshes[i] == null) return;
        bool changed = false;
        for (int i = 0; i < filters.Length; i++)
        {
            if (filters[i].sharedMesh == meshes[i]) continue;
            filters[i].sharedMesh = meshes[i];
            renderers[i].localBounds = authoredBounds[i];
            changed = true;
        }
        inspection = high;
        if (changed) MeshesChanged?.Invoke();
    }
}
