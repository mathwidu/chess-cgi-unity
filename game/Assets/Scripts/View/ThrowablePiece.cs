using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public sealed class ThrowablePiece : XRGrabInteractable
{
    private static readonly HashSet<ThrowablePiece> heldPieces = new HashSet<ThrowablePiece>();
    public static bool AnyPieceHeld => heldPieces.Count > 0;
    public static event Action HeldPiecesChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetHeldPieces()
    {
        heldPieces.Clear();
        HeldPiecesChanged = null;
    }

    private void SetHeld(bool held)
    {
        bool wasHolding = AnyPieceHeld;
        if (held) heldPieces.Add(this);
        else heldPieces.Remove(this);
        if (wasHolding != AnyPieceHeld) HeldPiecesChanged?.Invoke();
    }

    private readonly struct HandSupport
    {
        public readonly XRHandContactBody Hand;
        public readonly uint Revision;
        public readonly bool SupportsWeight;
        public readonly Collider PieceShape;
        public HandSupport(XRHandContactBody hand, bool supportsWeight, Collider pieceShape)
        {
            Hand = hand;
            Revision = hand.ContactRevision;
            SupportsWeight = supportsWeight;
            PieceShape = pieceShape;
        }
    }

    private const float ReturnDelaySeconds = 3f;
    private const float ReturnSeconds = 0.25f;
    private const float CenterOfMassScale = 0.6f;

    private Rigidbody body;
    private Vector3 restPosition;
    private Quaternion restRotation;
    private Coroutine recovery;
    private bool gripped;
    private bool loose;
    private bool motionLocked;
    private bool hasRest;
    private Collider[] pieceShapes;
    private int[] savedExclusions;
    private bool piecesProtected;
    private bool observingPlacement;
    private readonly Collider[] nearbyPieces = new Collider[64];
    private readonly Dictionary<Collider, HandSupport> supportingHands = new Dictionary<Collider, HandSupport>();

    public bool IsPhysicallyLoose => loose && !motionLocked;
    public bool IsSupportedByHand => HasHandContact(true);
    public bool HasSupportFrom(XRHandContactBody hand) => !isSelected && !motionLocked && HasHandContact(true, hand);
    public Vector3 ReleaseVelocity { get; private set; }
    public bool HasReleaseVelocity { get; private set; }

    internal void SetPlacementObservation(bool observing)
    {
        observingPlacement = observing;
        // Keep the held-piece protection until the intended landing resolves.
        // A capture released from above must pass through its target instead
        // of stacking on it and losing the green-square placement.
        if (observing) ProtectFromOtherPieces();
    }

    public void SetRest(Vector3 localPosition)
    {
        restPosition = localPosition;
        restRotation = transform.localRotation;
        hasRest = true;
        CancelRecovery();
        RestUnderPhysics();
    }

    public void Freeze()
    {
        CancelRecovery();
        loose = false;
        motionLocked = true;
        HoldStill();
    }

    public void RefreshRestingPhysics()
    {
        if (!hasRest || loose || motionLocked || isSelected) return;
        body.position = transform.parent != null ? transform.parent.TransformPoint(restPosition) : restPosition;
        body.rotation = transform.parent != null ? transform.parent.rotation * restRotation : restRotation;
        RestUnderPhysics();
    }

    protected override void Awake()
    {
        base.Awake();
        body = GetComponent<Rigidbody>();
        body.ResetCenterOfMass();
        body.centerOfMass *= CenterOfMassScale;
        pieceShapes = GetComponents<Collider>();
        savedExclusions = new int[pieceShapes.Length];
    }

    protected override void SetupRigidbodyGrab(Rigidbody rigidbody)
    {
        if (!gripped)
        {
            gripped = true;
            CancelRecovery();
            loose = true;
            motionLocked = false;
            HasReleaseVelocity = false;
            ProtectFromOtherPieces();
            SetHeld(true);
            rigidbody.isKinematic = false;
            rigidbody.useGravity = true;
        }

        base.SetupRigidbodyGrab(rigidbody);
    }

    protected override void SetupRigidbodyDrop(Rigidbody rigidbody)
    {
        gripped = false;
        SetHeld(false);
        base.SetupRigidbodyDrop(rigidbody);
    }

    protected override void Detach()
    {
        base.Detach();
        // Preserve intent before the next contact/gravity step changes velocity.
        ReleaseVelocity = body.linearVelocity;
        HasReleaseVelocity = true;
    }

    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        if (!isSelected && gameObject.activeInHierarchy)
        {
            CancelRecovery();
            BeginLoose();
        }

        base.OnSelectExited(args);
    }

    private void CancelRecovery()
    {
        supportingHands.Clear();
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

    private void RestUnderPhysics()
    {
        loose = false;
        motionLocked = false;
        body.isKinematic = false;
        body.useGravity = true;
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        body.Sleep();
    }

    private void BeginLoose()
    {
        if (!hasRest || gripped || motionLocked) return;
        loose = true;
        if (recovery == null) recovery = StartCoroutine(ReturnAfterDelay());
    }

    private void FixedUpdate()
    {
        RestorePieceContactsWhenClear();
        if (!hasRest || gripped || motionLocked || loose) return;
        // Touch and impacts change physical pose only. Their recovery never
        // enters chess selection, changes Square, or advances the turn.
        Vector3 home = transform.parent != null ? transform.parent.TransformPoint(restPosition) : restPosition;
        if ((body.position - home).sqrMagnitude > .002f * .002f ||
            Quaternion.Angle(transform.localRotation, restRotation) > 2f)
            BeginLoose();
    }

    private bool HasHandContact(bool requireSupport, XRHandContactBody onlyHand = null)
    {
        foreach (KeyValuePair<Collider, HandSupport> contact in supportingHands)
            if ((!requireSupport || contact.Value.SupportsWeight) && contact.Value.Hand != null &&
                (onlyHand == null || contact.Value.Hand == onlyHand) &&
                contact.Value.Revision == contact.Value.Hand.ContactRevision &&
                contact.Value.Hand.CanContact(contact.Key, contact.Value.PieceShape))
                return true;
        return false;
    }

    private IEnumerator ReturnAfterDelay()
    {
        float unsupportedSeconds = 0f;
        while (unsupportedSeconds < ReturnDelaySeconds)
        {
            // Sleeping rigidbodies don't receive OnCollisionStay, so retain a
            // support until it exits or its tracked hand is no longer active.
            unsupportedSeconds = HasHandContact(false) ? 0f : unsupportedSeconds + Time.deltaTime;
            yield return null;
        }

        motionLocked = true;
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
        body.position = transform.position;
        body.rotation = transform.rotation;
        recovery = null;
        supportingHands.Clear();
        RestUnderPhysics();
    }

    private void OnCollisionEnter(Collision collision) => UpdateHandSupport(collision);
    private void OnCollisionStay(Collision collision)
    {
        base.OnCollisionStay();
        UpdateHandSupport(collision);
    }

    private void UpdateHandSupport(Collision collision)
    {
        if (collision.rigidbody == null || !collision.rigidbody.TryGetComponent(out XRHandContactBody hand)) return;
        bool supportsWeight = false;
        for (int i = 0; i < collision.contactCount; i++)
            if (Vector3.Dot(collision.GetContact(i).normal, Vector3.up) > .35f)
            {
                supportsWeight = true;
                break;
            }
        Collider ownShape = collision.contactCount > 0 ? collision.GetContact(0).thisCollider : pieceShapes[0];
        supportingHands[collision.collider] = new HandSupport(hand, supportsWeight, ownShape);
        if (supportsWeight) hand.RecordSupport(this);
        BeginLoose();
    }

    private void OnCollisionExit(Collision collision)
    {
        supportingHands.Remove(collision.collider);
    }

    private void ProtectFromOtherPieces()
    {
        if (piecesProtected) return;
        piecesProtected = true;
        for (int i = 0; i < pieceShapes.Length; i++)
        {
            savedExclusions[i] = pieceShapes[i].excludeLayers;
            pieceShapes[i].excludeLayers |= 1 << PieceView.PhysicsLayer;
        }
    }

    private void RestorePieceContactsWhenClear()
    {
        if (!piecesProtected || gripped || isSelected || motionLocked || observingPlacement) return;
        float radius = .025f;
        foreach (Collider shape in pieceShapes)
            radius = Mathf.Max(radius, Vector3.Distance(shape.bounds.center, transform.position) + shape.bounds.extents.magnitude + .025f);
        int count = Physics.OverlapSphereNonAlloc(body.position, radius, nearbyPieces,
            1 << PieceView.PhysicsLayer, QueryTriggerInteraction.Ignore);
        if (count == nearbyPieces.Length) return;
        for (int i = 0; i < count; i++)
        {
            Collider other = nearbyPieces[i];
            if (other.attachedRigidbody == body) continue;
            Rigidbody otherBody = other.attachedRigidbody;
            Vector3 position = otherBody != null ? otherBody.position : other.transform.position;
            Quaternion rotation = otherBody != null ? otherBody.rotation : other.transform.rotation;
            foreach (Collider shape in pieceShapes)
                if (Physics.ComputePenetration(shape, body.position, body.rotation, other, position, rotation, out _, out _)) return;
        }
        RestorePieceContacts();
    }

    private void RestorePieceContacts()
    {
        if (!piecesProtected) return;
        for (int i = 0; i < pieceShapes.Length; i++)
            if (pieceShapes[i] != null) pieceShapes[i].excludeLayers = savedExclusions[i];
        piecesProtected = false;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        gripped = false;
        observingPlacement = false;
        SetHeld(false);
        RestorePieceContacts();
    }
}
