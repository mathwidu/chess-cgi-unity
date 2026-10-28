using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Hands.Gestures;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// Whole-hand grasp belongs only to the scale grips. The original pinch reader
// still owns piece release validity and feeds the HUD directly.
[DisallowMultipleComponent]
[DefaultExecutionOrder(XRInteractionUpdateOrder.k_XRInputDeviceButtonReader + 2)]
public sealed class BoardScaleHandInput : MonoBehaviour, IXRSelectFilter, IXRInputButtonReader
{
    private const float PressThreshold = .75f;
    private const float ReleaseThreshold = .55f;
    private XRHandGrabInput pinch;
    private NearFarInteractor interactor;
    private XRHandTrackingEvents tracking;
    private Transform trackingSpace;
    private BoardScaleHandles handles;
    private BoardScaleHandle grip;
    private Vector3 palmPosition;
    private Vector3 gripOffset;
    private float strength;
    private bool valid;
    private bool closed;
    private bool needsOpen = true;
    private bool focused = true;
    private bool paused;
    private bool performed;
    private int performedFrame = -1;
    private int completedFrame = -1;

    public bool canProcess => isActiveAndEnabled;
    internal bool IsControllingGrip => grip != null;
    internal bool IsEndingNaturally { get; private set; }
    internal bool IsReleased => valid && strength <= ReleaseThreshold && pinch != null && !pinch.ReadIsPerformed();

    public void Configure(XRHandGrabInput pinchInput, NearFarInteractor target)
    {
        Cancel();
        if (interactor != null) interactor.selectFilters.Remove(this);
        pinch = pinchInput;
        interactor = target;
        trackingSpace = transform.parent;
        tracking = null;
        if (isActiveAndEnabled && interactor != null) interactor.selectFilters.Add(this);
    }

    public bool Process(IXRSelectInteractor candidate, IXRSelectInteractable interactable)
        => grip == null || ReferenceEquals(interactable, grip.Interactable);

    internal bool Controls(BoardScaleHandle handle) => grip == handle;

    internal bool TryGetGripPose(out Vector3 position, out InteractorHandedness identity)
    {
        position = palmPosition + gripOffset;
        identity = interactor != null ? interactor.handedness : InteractorHandedness.None;
        return grip != null && valid && focused && !paused && isActiveAndEnabled &&
            interactor != null && interactor.isActiveAndEnabled &&
            identity != InteractorHandedness.None && BoardScaleHandles.Gesture.Finite(position);
    }

    private void OnEnable()
    {
        Cancel();
        if (interactor != null) interactor.selectFilters.Add(this);
    }

    private void OnDisable()
    {
        Cancel();
        if (interactor != null) interactor.selectFilters.Remove(this);
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        focused = hasFocus;
        Cancel();
    }

    private void OnApplicationPause(bool isPaused)
    {
        paused = isPaused;
        Cancel();
    }

    private void Update()
    {
        if (!focused || paused || Time.timeScale <= 0f || interactor == null ||
            !interactor.isActiveAndEnabled)
        {
            Cancel();
            SetPerformed(false);
            return;
        }
        bool pinching = pinch != null && pinch.ReadIsPerformed();
        if (!interactor.enableNearCasting || !ReadHand())
        {
            Cancel();
            SetPerformed(pinching);
            return;
        }

        if (needsOpen)
        {
            if (strength <= ReleaseThreshold) needsOpen = false;
            SetPerformed(pinching);
            return;
        }
        closed = closed ? strength > ReleaseThreshold : strength >= PressThreshold;

        if (grip != null)
        {
            // A held grip may continue with either the original pinch or the
            // whole-hand grasp, but interruption always requires opening again.
            if (!interactor.IsSelecting(grip.Interactable) || !grip.Process(interactor, grip.Interactable)) Cancel();
            else if (!closed && (pinch == null || !pinch.ReadIsPerformed())) EndGrip(true);
            SetPerformed(pinching || grip != null);
            return;
        }
        SetPerformed(pinching);
        if (!closed) return;

        if (interactor.hasSelection)
        {
            // Taking over an already pinched grip preserves its current anchor.
            // A piece selection is never converted into whole-hand selection.
            if (interactor.interactablesSelected.Count != 1 ||
                !(interactor.interactablesSelected[0] is XRSimpleInteractable selected) ||
                !selected.TryGetComponent(out BoardScaleHandle selectedGrip)) return;
            TryBeginGrip(selectedGrip, true);
            return;
        }
        if (handles == null) handles = FindFirstObjectByType<BoardScaleHandles>();
        if (handles == null) return;
        BoardScaleHandle first = handles.LeftHandle;
        BoardScaleHandle second = handles.RightHandle;
        if (first == null || second == null) return;
        if ((second.Grip.position - palmPosition).sqrMagnitude < (first.Grip.position - palmPosition).sqrMagnitude)
        {
            first = handles.RightHandle;
            second = handles.LeftHandle;
        }
        if (!TryBeginGrip(first, false)) TryBeginGrip(second, false);
    }

    private bool TryBeginGrip(BoardScaleHandle candidate, bool alreadyPinched)
    {
        if (candidate == null || candidate.Interactable == null || interactor.interactionManager == null) return false;
        grip = candidate;
        gripOffset = alreadyPinched ?
            (interactor.nearInteractionCaster?.castOrigin ?? interactor.transform).position - palmPosition : Vector3.zero;
        if (!candidate.Process(interactor, candidate.Interactable) ||
            !interactor.interactionManager.IsSelectPossible(interactor, candidate.Interactable))
        {
            grip = null;
            return false;
        }
        // The palm need not share the sample prefab's pinch cast pose. Enter
        // only after both normal XRI filters and physical proximity approve it.
        // Selection then uses the ordinary input/exit lifecycle, never XRI's
        // manual-interaction flag, which survives external select cancellation.
        SetPerformed(true);
        interactor.interactionManager.SelectEnter((IXRSelectInteractor)interactor, (IXRSelectInteractable)candidate.Interactable);
        if (interactor.IsSelecting(candidate.Interactable)) return true;
        grip = null;
        SetPerformed(pinch != null && pinch.ReadIsPerformed());
        return false;
    }

    private bool ReadHand()
    {
        if (tracking == null)
        {
            XRHandTrackingEvents[] events = GetComponentsInChildren<XRHandTrackingEvents>(true);
            Handedness side = interactor.handedness == InteractorHandedness.Left ? Handedness.Left : Handedness.Right;
            for (int i = 0; i < events.Length; i++)
                if (events[i].handedness == side) { tracking = events[i]; break; }
        }
        if (tracking == null || !tracking.isActiveAndEnabled || !tracking.handIsTracked || trackingSpace == null) return false;
        XRHand hand = tracking.handedness == Handedness.Left ? tracking.subsystem.leftHand : tracking.subsystem.rightHand;
        if (!hand.isTracked || !hand.GetJoint(XRHandJointID.Palm).TryGetPose(out Pose palm) ||
            !TryCurl(hand, XRHandFingerID.Index, out float index) ||
            !TryCurl(hand, XRHandFingerID.Middle, out float middle) ||
            !TryCurl(hand, XRHandFingerID.Ring, out float ring) ||
            !TryCurl(hand, XRHandFingerID.Little, out float little)) return false;
        palmPosition = trackingSpace.TransformPoint(palm.position);
        strength = Mathf.Min(Mathf.Min(index, middle), Mathf.Min(ring, little));
        valid = BoardScaleHandles.Gesture.Finite(palmPosition);
        return valid;
    }

    private static bool TryCurl(XRHand hand, XRHandFingerID finger, out float curl)
    {
        // Uncached calculations cannot accidentally reuse another update's
        // finger cache. No thumb pose or Meta Aim pinch validity is needed.
        XRFingerShape shape = hand.CalculateFingerShapeUncached(finger, XRFingerShapeTypes.FullCurl);
        return shape.TryGetFullCurl(out curl) && !float.IsNaN(curl) && !float.IsInfinity(curl);
    }

    private void Cancel()
    {
        valid = closed = false;
        needsOpen = true;
        EndGrip(false);
        SetPerformed(false);
    }

    private void EndGrip(bool natural)
    {
        IsEndingNaturally = natural;
        if (grip != null && interactor != null && interactor.interactionManager != null && interactor.IsSelecting(grip.Interactable))
            interactor.interactionManager.SelectExit((IXRSelectInteractor)interactor, (IXRSelectInteractable)grip.Interactable);
        grip = null;
        gripOffset = Vector3.zero;
        IsEndingNaturally = false;
    }

    private void SetPerformed(bool value)
    {
        if (performed == value) return;
        performed = value;
        if (value) performedFrame = Time.frameCount;
        else completedFrame = Time.frameCount;
    }

    public bool ReadIsPerformed() => performed;
    public bool ReadWasPerformedThisFrame() => performedFrame == Time.frameCount;
    public bool ReadWasCompletedThisFrame() => completedFrame == Time.frameCount;
    public float ReadValue() => !isActiveAndEnabled || !focused || paused ? 0f :
        Mathf.Max(pinch != null ? pinch.ReadValue() : 0f, grip != null ? strength : 0f);

    public bool TryReadValue(out float value)
    {
        value = ReadValue();
        return isActiveAndEnabled && focused && !paused && (valid || (pinch != null && pinch.TryReadValue(out _)));
    }
}
