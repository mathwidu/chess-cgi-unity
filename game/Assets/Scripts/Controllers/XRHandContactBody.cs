using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// A compound kinematic body follows raw joints. Dynamic pieces respond to its
// contacts without changing tracking, parenting pieces, or selecting a chess move.
[RequireComponent(typeof(Rigidbody))]
[DefaultExecutionOrder(100)]
public sealed class XRHandContactBody : MonoBehaviour
{
    private const int SettleSteps = 3;
    private const float MaxPoseStep = .12f;
    private const float MaxSettleStep = .01f;
    private const int PieceMask = 1 << PieceView.PhysicsLayer;

    private static readonly XRHandJointID[,] Segments =
    {
        { XRHandJointID.ThumbMetacarpal, XRHandJointID.ThumbProximal },
        { XRHandJointID.ThumbProximal, XRHandJointID.ThumbDistal },
        { XRHandJointID.ThumbDistal, XRHandJointID.ThumbTip },
        { XRHandJointID.IndexProximal, XRHandJointID.IndexIntermediate },
        { XRHandJointID.IndexIntermediate, XRHandJointID.IndexDistal },
        { XRHandJointID.IndexDistal, XRHandJointID.IndexTip },
        { XRHandJointID.MiddleProximal, XRHandJointID.MiddleIntermediate },
        { XRHandJointID.MiddleIntermediate, XRHandJointID.MiddleDistal },
        { XRHandJointID.MiddleDistal, XRHandJointID.MiddleTip },
        { XRHandJointID.RingProximal, XRHandJointID.RingIntermediate },
        { XRHandJointID.RingIntermediate, XRHandJointID.RingDistal },
        { XRHandJointID.RingDistal, XRHandJointID.RingTip },
        { XRHandJointID.LittleProximal, XRHandJointID.LittleIntermediate },
        { XRHandJointID.LittleIntermediate, XRHandJointID.LittleDistal },
        { XRHandJointID.LittleDistal, XRHandJointID.LittleTip },
    };

    private readonly Pose[] joints = new Pose[(int)XRHandJointID.EndMarker];
    private readonly bool[] jointValid = new bool[(int)XRHandJointID.EndMarker];
    private readonly CapsuleCollider[] fingers = new CapsuleCollider[Segments.GetLength(0)];
    private readonly bool[] segmentValid = new bool[Segments.GetLength(0)];
    private readonly int[] segmentSettleSteps = new int[Segments.GetLength(0)];
    private readonly Collider[] nearbyPieces = new Collider[64];
    private readonly List<DeferredContact> deferredContacts = new List<DeferredContact>(32);
    private readonly List<ThrowablePiece> supportedPieces = new List<ThrowablePiece>(4);
    private readonly struct DeferredContact
    {
        public readonly Collider Hand;
        public readonly Collider Piece;
        public DeferredContact(Collider hand, Collider piece) { Hand = hand; Piece = piece; }
    }
    private XRPhysicalHand hand;
    private Transform trackingOrigin;
    private Rigidbody body;
    private BoxCollider palm;
    private PhysicsMaterial surface;
    private Pose previousPose;
    private Pose previousOrigin;
    private bool hasPose;
    private bool focused = true;
    private bool paused;
    private int settledSteps;
    private XRBaseInteractor selector;
    private bool occupied;

    private bool IsTrackingArmed => body != null && body.detectCollisions && palm != null && palm.enabled;
    public bool IsContactEnabled => IsTrackingArmed && !occupied;
    public uint ContactRevision { get; private set; }

    public void Configure(XRPhysicalHand source, Transform origin)
    {
        hand = source;
        trackingOrigin = origin;
        gameObject.layer = XRPhysicsPusher.PhysicsLayer;
        body = GetComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        surface = new PhysicsMaterial("Hand piece contact")
        {
            staticFriction = .85f,
            dynamicFriction = .75f,
            bounciness = 0f,
            frictionCombine = PhysicsMaterialCombine.Maximum,
            bounceCombine = PhysicsMaterialCombine.Minimum,
        };
        palm = gameObject.AddComponent<BoxCollider>();
        ConfigureCollider(palm);
        for (int i = 0; i < fingers.Length; i++)
        {
            var segment = new GameObject(Segments[i, 0] + " piece contact");
            segment.layer = XRPhysicsPusher.PhysicsLayer;
            segment.transform.SetParent(transform, false);
            fingers[i] = segment.AddComponent<CapsuleCollider>();
            fingers[i].direction = 2;
            ConfigureCollider(fingers[i]);
        }
        RequireRearm();
        RefreshOccupation();
    }

    public void BindSelection(XRBaseInteractor interactor)
    {
        UnbindSelection();
        selector = interactor;
        if (selector == null) return;
        selector.selectEntered.AddListener(SelectionChanged);
        selector.selectExited.AddListener(SelectionChanged);
        RefreshOccupation();
    }

    private void SelectionChanged(BaseInteractionEventArgs _) => RefreshOccupation();

    private void RefreshOccupation()
    {
        if (palm == null) return;
        // A grab protects the board from both hands, independently of which
        // interactor owns it. The final XRI release restores physical contacts.
        bool next = ThrowablePiece.AnyPieceHeld;
        if (next == occupied) return;
        ContactRevision++;
        WakeNearbyPieces();
        occupied = next;
        if (!occupied && hasPose) DeferOverlappingContacts(previousPose);
        palm.excludeLayers = occupied ? ~0 : ~PieceMask;
        foreach (CapsuleCollider finger in fingers) finger.excludeLayers = occupied ? ~0 : ~PieceMask;
    }

    // Reopening a pinch is different from recovering tracking. Restore clear
    // shapes immediately; only overlapping pairs wait for separation.
    private void DeferOverlappingContacts(Pose pose)
    {
        int count = Physics.OverlapSphereNonAlloc(pose.position, .25f, nearbyPieces, PieceMask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            Collider piece = nearbyPieces[i];
            if (piece == null || piece.attachedRigidbody == null) continue;
            DeferIfOverlapping(palm, pose, piece);
            foreach (CapsuleCollider finger in fingers) DeferIfOverlapping(finger, pose, piece);
        }
    }

    private void DeferIfOverlapping(Collider shape, Pose pose, Collider piece)
    {
        if (!shape.enabled || Physics.GetIgnoreCollision(shape, piece) || !OverlapsPiece(shape, pose, piece)) return;
        Physics.IgnoreCollision(shape, piece, true);
        deferredContacts.Add(new DeferredContact(shape, piece));
    }

    private bool OverlapsPiece(Collider shape, Pose pose, Collider piece)
    {
        Vector3 local = shape == palm ? Vector3.zero : shape.transform.localPosition;
        Quaternion rotation = shape == palm ? Quaternion.identity : shape.transform.localRotation;
        Rigidbody pieceBody = piece.attachedRigidbody;
        Vector3 piecePosition = pieceBody != null ? pieceBody.position : piece.transform.position;
        Quaternion pieceRotation = pieceBody != null ? pieceBody.rotation : piece.transform.rotation;
        return Physics.ComputePenetration(shape, pose.position + pose.rotation * local, pose.rotation * rotation,
            piece, piecePosition, pieceRotation, out _, out _);
    }

    private void RestoreSeparatedContacts(Pose pose)
    {
        for (int i = deferredContacts.Count - 1; i >= 0; i--)
        {
            DeferredContact pair = deferredContacts[i];
            if (pair.Hand != null && pair.Piece != null && OverlapsPiece(pair.Hand, pose, pair.Piece)) continue;
            if (pair.Hand != null && pair.Piece != null) Physics.IgnoreCollision(pair.Hand, pair.Piece, false);
            deferredContacts.RemoveAt(i);
        }
    }

    public bool CanContact(Collider handShape, Collider pieceShape) => IsContactEnabled &&
        handShape != null && handShape.enabled && pieceShape != null && !Physics.GetIgnoreCollision(handShape, pieceShape);

    public void RecordSupport(ThrowablePiece piece)
    {
        if (piece != null && !supportedPieces.Contains(piece)) supportedPieces.Add(piece);
    }

    public bool TryGetSupportVisualPose(out Pose raw, out Pose drawn)
    {
        raw = drawn = default;
        bool supported = false;
        for (int i = supportedPieces.Count - 1; i >= 0; i--)
        {
            ThrowablePiece piece = supportedPieces[i];
            if (piece == null || !piece.HasSupportFrom(this)) supportedPieces.RemoveAt(i);
            else supported = true;
        }
        if (!supported || !hasPose || !IsContactEnabled || !ReadPalm(out raw) || Discontinuous(raw)) return false;
        // Match the interpolated piece's visual phase for both translation and
        // rotation. This changes the drawing only, never raw joints or bodies.
        Vector3 offset = Vector3.ClampMagnitude(transform.position - raw.position, .02f);
        Quaternion rotation = transform.rotation * Quaternion.Inverse(raw.rotation);
        rotation = Quaternion.RotateTowards(Quaternion.identity, rotation, 10f);
        drawn = new Pose(raw.position + offset, rotation * raw.rotation);
        return true;
    }

    private void UnbindSelection()
    {
        if (selector == null) return;
        selector.selectEntered.RemoveListener(SelectionChanged);
        selector.selectExited.RemoveListener(SelectionChanged);
        selector = null;
    }

    private void ConfigureCollider(Collider shape)
    {
        shape.sharedMaterial = surface;
        shape.contactOffset = .0005f;
        shape.includeLayers = PieceMask;
        shape.excludeLayers = ~PieceMask;
        shape.enabled = false;
    }

    public void RequireRearm()
    {
        ContactRevision++;
        if (IsTrackingArmed) WakeNearbyPieces();
        if (body != null) body.detectCollisions = false;
        if (palm != null) palm.enabled = false;
        for (int i = 0; i < fingers.Length; i++)
        {
            if (fingers[i] != null) fingers[i].enabled = false;
            segmentValid[i] = false;
            segmentSettleSteps[i] = 0;
        }
        hasPose = false;
        settledSteps = 0;
    }

    private bool ReadPalm(out Pose pose)
    {
        pose = default;
        if (!focused || paused || Time.timeScale <= 0f || hand == null ||
            !ReadJoint(XRHandJointID.Wrist) || !ReadJoint(XRHandJointID.MiddleProximal) ||
            !ReadJoint(XRHandJointID.IndexProximal) || !ReadJoint(XRHandJointID.LittleProximal)) return false;
        Vector3 wrist = joints[(int)XRHandJointID.Wrist].position;
        Vector3 middle = joints[(int)XRHandJointID.MiddleProximal].position;
        Vector3 across = joints[(int)XRHandJointID.LittleProximal].position - joints[(int)XRHandJointID.IndexProximal].position;
        Vector3 forward = middle - wrist;
        Vector3 normal = Vector3.Cross(forward, across);
        if (forward.sqrMagnitude < .0004f || normal.sqrMagnitude < .0000001f) return false;
        // Geometry gives a stable palm frame on either hand without assuming a
        // provider-specific axis for the OpenXR Palm joint's rotation.
        pose = new Pose((wrist + middle) * .5f, Quaternion.LookRotation(forward, normal));
        return true;
    }

    private bool ReadJoint(XRHandJointID id)
    {
        int index = (int)id;
        bool valid = hand.TryGetRawJointPose(id, out Pose pose) && Finite(pose.position);
        jointValid[index] = valid;
        joints[index] = pose;
        return valid;
    }

    private static bool Finite(Vector3 value) =>
        float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);

    private bool Discontinuous(Pose pose) => hasPose &&
        ((pose.position - previousPose.position).sqrMagnitude > MaxPoseStep * MaxPoseStep ||
         Quaternion.Angle(pose.rotation, previousPose.rotation) > 90f ||
         (trackingOrigin != null && ((trackingOrigin.position - previousOrigin.position).sqrMagnitude > .00000001f ||
         Quaternion.Angle(trackingOrigin.rotation, previousOrigin.rotation) > .01f)));

    private void FixedUpdate()
    {
        if (palm == null) return;
        if (!ReadPalm(out Pose pose))
        {
            RequireRearm();
            return;
        }
        if (Discontinuous(pose)) RequireRearm();
        bool stable = hasPose && (pose.position - previousPose.position).sqrMagnitude < MaxSettleStep * MaxSettleStep &&
            Quaternion.Angle(pose.rotation, previousPose.rotation) < 8f;
        bool armed = IsTrackingArmed;
        if (armed)
        {
            body.MovePosition(pose.position);
            body.MoveRotation(pose.rotation);
        }
        else
        {
            // Teleport only while disabled, never sweep from an old tracking pose.
            body.position = pose.position;
            body.rotation = pose.rotation;
        }
        UpdateShapes(pose, armed);
        if (!occupied) RestoreSeparatedContacts(pose);
        previousPose = pose;
        if (trackingOrigin != null) previousOrigin = new Pose(trackingOrigin.position, trackingOrigin.rotation);
        hasPose = true;
        if (armed) return;
        settledSteps = stable && ClearOfLoosePieces(pose) ? settledSteps + 1 : 0;
        if (settledSteps < SettleSteps) return;
        body.detectCollisions = true;
        palm.enabled = true;
        for (int i = 0; i < fingers.Length; i++)
            fingers[i].enabled = segmentValid[i] && segmentSettleSteps[i] >= SettleSteps;
    }

    private void UpdateShapes(Pose pose, bool armed)
    {
        float width = Vector3.Distance(joints[(int)XRHandJointID.IndexProximal].position,
            joints[(int)XRHandJointID.LittleProximal].position) + .012f;
        float length = Vector3.Distance(joints[(int)XRHandJointID.Wrist].position,
            joints[(int)XRHandJointID.MiddleProximal].position) + .015f;
        // Half a millimeter per face clears the measured ~0.2 mm skin overlap
        // at rest, without a visible floating gap or runtime mesh calibration.
        palm.size = new Vector3(Mathf.Clamp(width, .045f, .09f), .029f, Mathf.Clamp(length, .055f, .12f));
        Quaternion inverse = Quaternion.Inverse(pose.rotation);
        for (int i = (int)XRHandJointID.ThumbMetacarpal; i < (int)XRHandJointID.EndMarker; i++)
            ReadJoint((XRHandJointID)i);
        for (int i = 0; i < fingers.Length; i++)
        {
            CapsuleCollider shape = fingers[i];
            bool valid = SegmentTracked(i);
            Vector3 first = valid ? inverse * (joints[(int)Segments[i, 0]].position - pose.position) : Vector3.zero;
            Vector3 second = valid ? inverse * (joints[(int)Segments[i, 1]].position - pose.position) : Vector3.zero;
            Vector3 direction = second - first;
            valid &= first.sqrMagnitude < .0625f && second.sqrMagnitude < .0625f && direction.sqrMagnitude < .0081f;
            if (!valid)
            {
                if (shape.enabled)
                {
                    ContactRevision++;
                    WakeNearbyPieces();
                }
                shape.enabled = false;
                segmentValid[i] = false;
                segmentSettleSteps[i] = 0;
                continue;
            }
            Vector3 center = (first + second) * .5f;
            Quaternion rotation = direction.sqrMagnitude > .000001f
                ? Quaternion.FromToRotation(Vector3.forward, direction) : Quaternion.identity;
            float step = (center - shape.transform.localPosition).sqrMagnitude;
            float turn = Quaternion.Angle(rotation, shape.transform.localRotation);
            bool stable = segmentValid[i] && step < MaxSettleStep * MaxSettleStep && turn < 8f;
            bool jumped = segmentValid[i] && (step > MaxPoseStep * MaxPoseStep || turn > 90f);
            if (shape.enabled && jumped)
            {
                ContactRevision++;
                WakeNearbyPieces();
                shape.enabled = false;
            }
            segmentSettleSteps[i] = stable ? Mathf.Min(SettleSteps, segmentSettleSteps[i] + 1) : 0;
            segmentValid[i] = true;
            shape.transform.localPosition = center;
            shape.transform.localRotation = rotation;
            shape.radius = Mathf.Clamp(width * .09f, .005f, .008f);
            shape.height = direction.magnitude + shape.radius * 2f;
            if (!armed) shape.enabled = false;
            else if (!shape.enabled && segmentSettleSteps[i] >= SettleSteps && ClearFinger(i)) shape.enabled = true;
        }
    }

    private bool SegmentTracked(int index) =>
        jointValid[(int)Segments[index, 0]] && jointValid[(int)Segments[index, 1]];

    private bool ClearOfLoosePieces(Pose pose)
    {
        // Query the proposed world geometry directly. Disabled/interpolated
        // collider Transforms must not substitute an older physics pose here.
        int count = Physics.OverlapBoxNonAlloc(pose.position, palm.size * .5f, nearbyPieces,
            pose.rotation, PieceMask, QueryTriggerInteraction.Ignore);
        if (HasLoosePiece(count)) return false;
        for (int i = 0; i < fingers.Length; i++)
            if (segmentValid[i] && !ClearFinger(i)) return false;
        return true;
    }

    private bool ClearFinger(int index)
    {
        int count = Physics.OverlapCapsuleNonAlloc(joints[(int)Segments[index, 0]].position,
            joints[(int)Segments[index, 1]].position, fingers[index].radius, nearbyPieces,
            PieceMask, QueryTriggerInteraction.Ignore);
        return !HasLoosePiece(count);
    }

    private bool HasLoosePiece(int count)
    {
        if (count == nearbyPieces.Length) return true;
        for (int i = 0; i < count; i++)
        {
            Collider other = nearbyPieces[i];
            // A tracked hand must regain clearance before contacting a dynamic
            // piece, including a sleeping piece still on its logical square.
            if (other.attachedRigidbody != null && !other.attachedRigidbody.isKinematic) return true;
        }
        return false;
    }

    private void LateUpdate()
    {
        if (palm != null && (!ReadPalm(out Pose pose) || Discontinuous(pose))) RequireRearm();
    }

    private void OnEnable()
    {
        ThrowablePiece.HeldPiecesChanged += RefreshOccupation;
        RefreshOccupation();
    }

    private void OnDisable()
    {
        ThrowablePiece.HeldPiecesChanged -= RefreshOccupation;
        RequireRearm();
    }

    private void WakeNearbyPieces()
    {
        // Removing a kinematic support doesn't wake sleeping dynamic bodies.
        // This bounded query runs only when removing contacts, and also covers
        // compound contacts without depending on per-collider exit ordering.
        int count = Physics.OverlapSphereNonAlloc(body.position, .18f, nearbyPieces, PieceMask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            Rigidbody piece = nearbyPieces[i].attachedRigidbody;
            if (piece != null && !piece.isKinematic) piece.WakeUp();
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        focused = hasFocus;
        if (!hasFocus) RequireRearm();
    }
    private void OnApplicationPause(bool isPaused)
    {
        paused = isPaused;
        if (isPaused) RequireRearm();
    }
    private void OnDestroy()
    {
        UnbindSelection();
        ThrowablePiece.HeldPiecesChanged -= RefreshOccupation;
        foreach (DeferredContact pair in deferredContacts)
            if (pair.Hand != null && pair.Piece != null) Physics.IgnoreCollision(pair.Hand, pair.Piece, false);
        if (surface != null) Destroy(surface);
    }
}
