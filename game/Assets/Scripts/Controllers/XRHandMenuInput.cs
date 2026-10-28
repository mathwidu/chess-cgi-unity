using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[DisallowMultipleComponent]
[DefaultExecutionOrder(XRInteractionUpdateOrder.k_XRInputDeviceButtonReader + 1)]
public sealed class XRHandMenuInput : MonoBehaviour
{
    private XRHandGrabInput pinch;
    private NearFarInteractor interactor;
    private bool restoreUi;

    public void Configure(XRHandGrabInput input, NearFarInteractor target)
    {
        pinch = input;
        interactor = target;
        var reader = new XRInputButtonReader("Hand menu pinch")
        {
            inputSourceMode = XRInputButtonReader.InputSourceMode.ObjectReference,
        };
        reader.SetObjectReference(pinch);
        interactor.uiPressInput = reader;
        if (isActiveAndEnabled) CancelPendingPress();
    }

    private void OnEnable()
    {
        restoreUi = false;
        CancelPendingPress();
    }

    private void Update()
    {
        if (pinch == null || !pinch.CanCommitRelease)
        {
            CancelPendingPress();
            return;
        }

        if (restoreUi && interactor != null && interactor.isActiveAndEnabled)
        {
            restoreUi = false;
            interactor.enableUIInteraction = true;
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus) CancelPendingPress();
    }

    private void OnApplicationPause(bool isPaused)
    {
        if (isPaused) CancelPendingPress();
    }

    private void OnDisable()
    {
        CancelPendingPress();
        // The inactive hand must retain its UI configuration for the next
        // modality switch. Its interactor cannot register while inactive.
        if (restoreUi && interactor != null) interactor.enableUIInteraction = true;
        restoreUi = false;
    }

    private void CancelPendingPress()
    {
        if (interactor == null || !interactor.enableUIInteraction) return;
        restoreUi = true;
        // Resetting a pressed input to false would look like a click release.
        // Unregister the pointer first so XRI cancels its pending UI press.
        interactor.enableUIInteraction = false;
    }
}
