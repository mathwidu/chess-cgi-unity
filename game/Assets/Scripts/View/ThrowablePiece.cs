using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public sealed class ThrowablePiece : XRGrabInteractable
{
    private const float ReturnDelaySeconds = 3f;
    private const float ReturnSeconds = 0.25f;

    private Rigidbody body;
    private Vector3 restPosition;
    private Quaternion restRotation;
    private Coroutine recovery;
    private bool gripped;

    public void SetRest(Vector3 localPosition)
    {
        restPosition = localPosition;
        restRotation = transform.localRotation;
    }

    public void Freeze()
    {
        CancelRecovery();
        HoldStill();
    }

    protected override void Awake()
    {
        base.Awake();
        body = GetComponent<Rigidbody>();
    }

    protected override void SetupRigidbodyGrab(Rigidbody rigidbody)
    {
        if (!gripped)
        {
            gripped = true;
            CancelRecovery();
            rigidbody.isKinematic = false;
            rigidbody.useGravity = true;
        }

        base.SetupRigidbodyGrab(rigidbody);
    }

    protected override void SetupRigidbodyDrop(Rigidbody rigidbody)
    {
        gripped = false;
        base.SetupRigidbodyDrop(rigidbody);
    }

    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        if (!isSelected)
        {
            CancelRecovery();
            recovery = StartCoroutine(ReturnAfterDelay());
        }

        base.OnSelectExited(args);
    }

    private void CancelRecovery()
    {
        if (recovery != null)
        {
            StopCoroutine(recovery);
            recovery = null;
        }
    }

    private void HoldStill()
    {
        if (!body.isKinematic)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }

        body.useGravity = false;
        body.isKinematic = true;
    }

    private IEnumerator ReturnAfterDelay()
    {
        yield return new WaitForSeconds(ReturnDelaySeconds);

        HoldStill();
        Vector3 startPosition = transform.localPosition;
        Quaternion startRotation = transform.localRotation;
        for (float t = 0f; t < 1f; t += Time.deltaTime / ReturnSeconds)
        {
            float eased = Mathf.SmoothStep(0f, 1f, t);
            transform.localPosition = Vector3.Lerp(startPosition, restPosition, eased);
            transform.localRotation = Quaternion.Slerp(startRotation, restRotation, eased);
            yield return null;
        }

        transform.localPosition = restPosition;
        transform.localRotation = restRotation;
        recovery = null;
    }
}
