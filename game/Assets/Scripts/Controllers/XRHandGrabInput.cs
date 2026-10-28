using System;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[DisallowMultipleComponent]
[DefaultExecutionOrder(XRInteractionUpdateOrder.k_XRInputDeviceButtonReader)]
public sealed class XRHandGrabInput : MonoBehaviour, IXRInputButtonReader
{
    private const float PressThreshold = .75f;
    private const float ReleaseThreshold = .55f;
    private const int TrackedPose = (int)(InputTrackingState.Position | InputTrackingState.Rotation);

    private XRInputButtonReader source;
    private InteractorHandedness handedness;
    private bool needsOpen = true;
    private bool performed;
    private bool valid;
    private bool focused = true;
    private bool paused;
    private float strength;
    private int performedFrame = -1;
    private int completedFrame = -1;

    public bool CanCommitRelease => isActiveAndEnabled && valid && HasTrackedPose() && focused && !paused;

    public void Configure(XRInputButtonReader rawInput, InteractorHandedness hand)
    {
        source = rawInput ?? throw new ArgumentNullException(nameof(rawInput));
        handedness = hand;
        ResetReader();
        if (isActiveAndEnabled) source.EnableDirectActionIfModeUsed();
    }

    private void OnEnable()
    {
        ResetReader();
        source?.EnableDirectActionIfModeUsed();
    }

    private void OnDisable()
    {
        ResetReader();
        source?.DisableDirectActionIfModeUsed();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        focused = hasFocus;
        ResetReader();
    }

    private void OnApplicationPause(bool isPaused)
    {
        paused = isPaused;
        ResetReader();
    }

    private void Update()
    {
        if (!focused || paused || source == null || !HasTrackedPose())
        {
            ResetReader();
            return;
        }

        // Select Value also merges HandInteraction.graspValue. The other fingers
        // can remain curled after the thumb/index pinch opens, so that aggregate
        // must not keep a near pinch held or acquire an open hand.
        float value = AimHand().pinchStrengthIndex.ReadValue();
        if (float.IsNaN(value) || float.IsInfinity(value))
        {
            ResetReader();
            return;
        }

        valid = true;
        strength = Mathf.Clamp01(value);
        if (needsOpen)
        {
            // A hand returning already pinched must not acquire a nearby piece by itself.
            if (strength <= ReleaseThreshold) needsOpen = false;
            SetPerformed(false);
            return;
        }

        SetPerformed(performed ? strength > ReleaseThreshold : strength >= PressThreshold);
    }

    private bool HasTrackedPose()
    {
        MetaAimHand device = AimHand();
        return device != null && device.added && device.isTracked.isPressed &&
            (device.trackingState.ReadValue() & TrackedPose) == TrackedPose;
    }

    private MetaAimHand AimHand() => handedness == InteractorHandedness.Left ? MetaAimHand.left :
        handedness == InteractorHandedness.Right ? MetaAimHand.right : null;

    private void ResetReader()
    {
        valid = false;
        strength = 0f;
        needsOpen = true;
        SetPerformed(false);
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
    public float ReadValue() => strength;
    public bool TryReadValue(out float value)
    {
        value = strength;
        return valid;
    }
}
