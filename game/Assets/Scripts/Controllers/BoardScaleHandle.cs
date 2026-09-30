using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[RequireComponent(typeof(XRSimpleInteractable))]
public sealed class BoardScaleHandle : MonoBehaviour, IXRSelectFilter
{
    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private MaterialPropertyBlock highlight;
    private BoardScaleHandles owner;
    private Renderer gripRenderer;
    private bool paired;
    private bool listening;
    private int shownState = -1;

    public XRSimpleInteractable Interactable { get; private set; }
    public CapsuleCollider GripCollider { get; private set; }
    public Transform Grip => transform;
    public NearFarInteractor Interactor { get; private set; }
    public bool canProcess => isActiveAndEnabled;

    internal void Configure(BoardScaleHandles handles, CapsuleCollider collider, Renderer renderer)
    {
        // This is also called while the VR-only hierarchy is inactive. Native
        // Unity objects must be created here on the main thread, not in a field initializer.
        highlight = new MaterialPropertyBlock();
        owner = handles;
        gripRenderer = renderer;
        GripCollider = collider;
        Interactable = GetComponent<XRSimpleInteractable>();
        Interactable.selectMode = InteractableSelectMode.Single;
        Interactable.colliders.Clear();
        Interactable.colliders.Add(collider);
        if (isActiveAndEnabled) Listen();
    }

    public bool Process(IXRSelectInteractor interactor, IXRSelectInteractable interactable)
        => owner != null && owner.CanSelect(this, interactor);

    internal bool IsNear(Vector3 position, float maximumDistance)
        => GripCollider != null && GripCollider.enabled &&
           (GripCollider.ClosestPoint(position) - position).sqrMagnitude <= maximumDistance * maximumDistance;

    private void OnEnable()
    {
        if (Interactable != null) Listen();
    }

    private void Listen()
    {
        if (listening) return;
        listening = true;
        Interactable.selectFilters.Add(this);
        Interactable.selectEntered.AddListener(Selected);
        Interactable.selectExited.AddListener(Deselected);
        Interactable.hoverEntered.AddListener(HoverEntered);
        Interactable.hoverExited.AddListener(HoverExited);
        RefreshHighlight();
    }

    private void OnDisable()
    {
        if (listening && Interactable != null)
        {
            Interactable.selectFilters.Remove(this);
            Interactable.selectEntered.RemoveListener(Selected);
            Interactable.selectExited.RemoveListener(Deselected);
            Interactable.hoverEntered.RemoveListener(HoverEntered);
            Interactable.hoverExited.RemoveListener(HoverExited);
        }
        if (owner != null && Interactor != null) owner.ReleasedHandle(this, Interactor, true);
        listening = false;
        Interactor = null;
        paired = false;
    }

    private void Selected(SelectEnterEventArgs args)
    {
        Interactor = args.interactorObject as NearFarInteractor;
        if (Interactor != null) Interactor.SendHapticImpulse(.18f, .035f);
        RefreshHighlight();
    }

    private void Deselected(SelectExitEventArgs args)
    {
        if (owner != null) owner.ReleasedHandle(this, args.interactorObject, args.interactorObject.isSelectActive);
        Interactor = null;
        paired = false;
        RefreshHighlight();
    }

    private void HoverEntered(HoverEnterEventArgs args) => RefreshHighlight();
    private void HoverExited(HoverExitEventArgs args) => RefreshHighlight();

    internal void SetPaired(bool value)
    {
        paired = value;
        RefreshHighlight();
    }

    private void RefreshHighlight()
    {
        if (gripRenderer == null || Interactable == null) return;
        int state = paired ? 3 : Interactor != null ? 2 : Interactable.isHovered ? 1 : 0;
        if (state == shownState) return;
        shownState = state;
        Color color = state == 3 ? new Color(.85f, .88f, .38f) : state == 2 ? new Color(.88f, .72f, .35f) :
            state == 1 ? new Color(.78f, .82f, .76f) : new Color(.58f, .63f, .67f);
        highlight.SetColor(BaseColor, color);
        highlight.SetColor(ColorId, color);
        gripRenderer.SetPropertyBlock(highlight);
    }
}
