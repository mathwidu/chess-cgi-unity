using System;
using UnityEngine;

// A world-space proxy follows a tracked contact point without inheriting its
// Transform motion. Only the physical table-button caps can receive its push.
[RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
[DefaultExecutionOrder(100)]
public sealed class XRPhysicsPusher : MonoBehaviour
{
    public delegate bool PoseReader(out Pose pose);

    public const int PhysicsLayer = 28;
    private const int SettleSteps = 3;
    private const float MaxPoseStep = .12f;
    private const float MaxSettleStep = .01f;
    private const float RearmClearance = .002f;

    private Transform poseSource;
    private Transform trackingOrigin;
    private Func<bool> isPoseTracked;
    private PoseReader readPose;
    private Rigidbody body;
    private SphereCollider touchCollider;
    private bool hasPose;
    private bool hasOriginPose;
    private Vector3 previousPosition;
    private Quaternion previousRotation;
    private Vector3 previousOriginPosition;
    private Quaternion previousOriginRotation;
    private int settledSteps;
    private readonly Collider[] nearbyCaps = new Collider[8];
    private readonly RaycastHit[] sweptCaps = new RaycastHit[8];
    private PhysicalTableButton contactButton;
    private PhysicalTableButton quarantineButton;
    private Vector3 buttonPosition;
    private Quaternion buttonRotation;
    private Vector3 buttonScale;
    private bool focused = true;
    private bool paused;

    internal Func<bool> RearmAllowed;
    internal event Action<bool> RearmRequired;

    public Rigidbody Body => body;
    public SphereCollider TouchCollider => touchCollider;
    public bool IsContactEnabled => touchCollider != null && touchCollider.enabled;
    public bool IsArmed => IsContactEnabled;
    public PhysicalTableButton ContactButton => contactButton;
    public Pose RawPose { get; private set; }
    public Pose TargetPose { get; private set; }

    public void Configure(Transform source, Func<bool> tracked, Transform origin = null, float radius = .012f)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (tracked == null) throw new ArgumentNullException(nameof(tracked));
        InitializePhysics();
        poseSource = source;
        isPoseTracked = tracked;
        readPose = ReadTransformPose;
        trackingOrigin = origin;
        touchCollider.radius = Mathf.Max(.001f, radius);
        RequireRearm();
    }

    // A raw XR joint/model-local pose must remain independent of any visual
    // correction. The Transform overload remains useful for capture fixtures.
    public void Configure(PoseReader source, Transform origin = null, float radius = .012f)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        InitializePhysics();
        poseSource = null;
        isPoseTracked = null;
        readPose = source;
        trackingOrigin = origin;
        touchCollider.radius = Mathf.Max(.001f, radius);
        RequireRearm();
    }

    private void Awake()
    {
        InitializePhysics();
        RequireRearm();
    }

    private void InitializePhysics()
    {
        if (body != null && touchCollider != null) return;
        gameObject.layer = PhysicsLayer;
        body = GetComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        touchCollider = GetComponent<SphereCollider>();
        touchCollider.isTrigger = false;
        touchCollider.radius = .012f;
        touchCollider.contactOffset = .0005f;
        touchCollider.includeLayers = 1 << PhysicalTableButton.PhysicsLayer;
        touchCollider.excludeLayers = ~(1 << PhysicalTableButton.PhysicsLayer);
        RequireRearm();
    }

    private void OnEnable() => RequireRearm();
    private void OnDisable() => RequireRearm();
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

    // The rig also calls this before a recenter, seat change or keyboard orbit.
    // No swept collision is allowed across a discontinuity in the tracked pose.
    public void RequireRearm()
    {
        RequireRearm(false);
    }

    private void RequireRearm(bool retainVisualContact)
    {
        bool notifyHand = IsArmed || contactButton != null;
        if (contactButton != null) quarantineButton = contactButton;
        if (touchCollider != null) touchCollider.enabled = false;
        if (body != null) body.detectCollisions = false;
        hasPose = false;
        hasOriginPose = false;
        settledSteps = 0;
        if (!retainVisualContact) contactButton = null;
        if (notifyHand) RearmRequired?.Invoke(retainVisualContact);
    }

    internal void DisarmForHand(bool retainVisualContact) => RequireRearm(retainVisualContact);

    private bool ReadTransformPose(out Pose pose)
    {
        pose = default;
        if (poseSource == null || !poseSource.gameObject.activeInHierarchy ||
            isPoseTracked == null || !isPoseTracked()) return false;
        pose = new Pose(poseSource.position, poseSource.rotation);
        return true;
    }

    public bool TryReadPose(out Pose pose)
    {
        pose = default;
        return isActiveAndEnabled && focused && !paused && Time.timeScale > 0f &&
            readPose != null && readPose(out pose) && Finite(pose.position, pose.rotation);
    }

    private bool OriginMoved()
    {
        return trackingOrigin != null && hasOriginPose &&
            ((trackingOrigin.position - previousOriginPosition).sqrMagnitude > .00000001f ||
             Quaternion.Angle(trackingOrigin.rotation, previousOriginRotation) > .01f);
    }

    private bool PoseJumped(Vector3 position, Quaternion rotation)
    {
        return hasPose && ((position - previousPosition).sqrMagnitude > MaxPoseStep * MaxPoseStep ||
            Quaternion.Angle(rotation, previousRotation) > 90f);
    }

    private void LateUpdate()
    {
        // Disable before the next physics step even when tracking disappears
        // between FixedUpdates or a modality manager hides this source.
        if (!TryReadPose(out Pose pose) || OriginMoved() || PoseJumped(pose.position, pose.rotation))
            RequireRearm();
        else if (contactButton != null && ButtonMoved())
        {
            RequireRearm(true);
            RememberButtonPose();
        }
    }

    private void FixedUpdate()
    {
        if (!TryReadPose(out Pose pose))
        {
            RequireRearm();
            return;
        }

        Vector3 position = pose.position;
        Quaternion rotation = pose.rotation;
        RawPose = pose;

        if (OriginMoved() || PoseJumped(position, rotation)) RequireRearm();

        if (contactButton != null && (!ButtonIsAvailable(contactButton) ||
            !FaceSupport(contactButton, position, touchCollider.radius, out _)))
            contactButton = null;
        if (contactButton != null && ButtonMoved())
            RequireRearm(true); // A height step cannot sweep an armed hand into the relocated cap.

        if (contactButton == null) FindContactButton(position);
        RememberButtonPose();

        if (!touchCollider.enabled)
        {
            // Teleport the disabled proxy, never MovePosition it from its stale
            // pose. Reacquiring inside a cap stays disarmed until it leaves.
            body.position = position;
            body.rotation = rotation;
            bool stable = !hasPose || (position - previousPosition).sqrMagnitude <= MaxSettleStep * MaxSettleStep;
            bool outside = !Physics.CheckSphere(position, touchCollider.radius + RearmClearance,
                1 << PhysicalTableButton.PhysicsLayer, QueryTriggerInteraction.Ignore);
            // An overpress stays quarantined even after the raw hand has passed
            // beyond the collider. It must withdraw or leave the side of the cap.
            if (contactButton != null && FaceSupport(contactButton, position, touchCollider.radius, out float support))
                outside &= Vector3.Dot(position - contactButton.RestFaceCenter, contactButton.PressDirection) + support < -RearmClearance;
            if (quarantineButton != null && ButtonIsAvailable(quarantineButton) &&
                FaceSupport(quarantineButton, position, touchCollider.radius, out float quarantineSupport))
                outside &= Vector3.Dot(position - quarantineButton.RestFaceCenter, quarantineButton.PressDirection) + quarantineSupport < -RearmClearance;
            if (outside) quarantineButton = null;
            outside &= RearmAllowed == null || RearmAllowed();
            settledSteps = stable && outside ? settledSteps + 1 : 0;
            if (settledSteps >= SettleSteps)
            {
                body.detectCollisions = true;
                touchCollider.enabled = true;
            }
        }
        else
        {
            position = ConstrainTarget(position);
            body.MovePosition(position);
            body.MoveRotation(rotation);
        }

        TargetPose = new Pose(position, rotation);

        previousPosition = pose.position;
        previousRotation = rotation;
        hasPose = true;
        if (trackingOrigin != null)
        {
            previousOriginPosition = trackingOrigin.position;
            previousOriginRotation = trackingOrigin.rotation;
            hasOriginPose = true;
        }
    }

    internal bool IsClearForRearm(Pose pose)
    {
        Vector3 position = pose.position;
        if (Physics.CheckSphere(position, touchCollider.radius + RearmClearance,
            1 << PhysicalTableButton.PhysicsLayer, QueryTriggerInteraction.Ignore)) return false;
        PhysicalTableButton known = quarantineButton != null ? quarantineButton : contactButton;
        if (known == null || !ButtonIsAvailable(known) || !FaceSupport(known, position, touchCollider.radius, out float support)) return true;
        return Vector3.Dot(position - known.RestFaceCenter, known.PressDirection) + support < -RearmClearance;
    }

    private static bool ButtonIsAvailable(PhysicalTableButton button)
    {
        return button.isActiveAndEnabled && button.Body != null && button.Body.gameObject.activeInHierarchy;
    }

    private bool ButtonMoved()
    {
        Transform mount = contactButton.transform;
        return (mount.position - buttonPosition).sqrMagnitude > .000004f ||
            Quaternion.Angle(mount.rotation, buttonRotation) > .5f ||
            (mount.lossyScale - buttonScale).sqrMagnitude > .000001f;
    }

    private void RememberButtonPose()
    {
        if (contactButton == null) return;
        Transform mount = contactButton.transform;
        buttonPosition = mount.position;
        buttonRotation = mount.rotation;
        buttonScale = mount.lossyScale;
    }

    private void FindContactButton(Vector3 position)
    {
        bool armedAtStart = touchCollider.enabled;
        Vector3 travel = position - previousPosition;
        float distance = travel.magnitude;
        if (hasPose && touchCollider.enabled && distance > .00001f)
        {
            int count = Physics.SphereCastNonAlloc(previousPosition, touchCollider.radius, travel / distance,
                sweptCaps, distance, 1 << PhysicalTableButton.PhysicsLayer, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                if (TryAcquire(sweptCaps[i].collider, position, true)) return;
                if (armedAtStart && !touchCollider.enabled) return;
            }
        }

        int overlaps = Physics.OverlapSphereNonAlloc(position, touchCollider.radius + .015f, nearbyCaps,
            1 << PhysicalTableButton.PhysicsLayer, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < overlaps; i++)
        {
            if (TryAcquire(nearbyCaps[i], position, false)) return;
            if (armedAtStart && !touchCollider.enabled) return;
        }
    }

    private bool TryAcquire(Collider cap, Vector3 position, bool swept)
    {
        PhysicalTableButton button = cap.GetComponentInParent<PhysicalTableButton>();
        if (button == null || !ButtonIsAvailable(button) ||
            !FaceSupport(button, position, touchCollider.radius, out float support)) return false;

        Vector3 normal = button.PressDirection;
        float depth = Vector3.Dot(position - button.RestFaceCenter, normal);
        float stroke = PhysicalTableButton.Stroke * Mathf.Abs(button.transform.lossyScale.z);
        float previousDepth = hasPose ? Vector3.Dot(previousPosition - button.RestFaceCenter, normal) : depth;
        // Accept a front approach or a disarmed hand overlapping the cap for
        // visual blocking. A hand coming from behind cannot pull/push the cap.
        if (touchCollider.enabled && previousDepth + support > stroke + RearmClearance)
        {
            quarantineButton = button;
            RequireRearm();
            return false;
        }
        if (!swept && (depth < -support - .015f || depth > stroke + support)) return false;
        contactButton = button;
        RememberButtonPose();
        return true;
    }

    private Vector3 ConstrainTarget(Vector3 position)
    {
        if (contactButton == null || !FaceSupport(contactButton, position, touchCollider.radius, out float support))
            return position;
        Vector3 direction = contactButton.PressDirection;
        float stroke = PhysicalTableButton.Stroke * Mathf.Abs(contactButton.transform.lossyScale.z);
        float depth = Vector3.Dot(position - contactButton.RestFaceCenter, direction);
        float maximum = stroke - support;
        return position - direction * Mathf.Max(0f, depth - maximum);
    }

    // The renderer follows the actual cap face, while the physical target may
    // advance as far as the cap's full stroke to supply the force to its spring.
    public bool TryGetVisualOffset(out Vector3 offset)
    {
        offset = Vector3.zero;
        if (contactButton == null || !ButtonIsAvailable(contactButton) || OriginMoved() ||
            !TryReadPose(out Pose pose) || PoseJumped(pose.position, pose.rotation) ||
            !FaceSupport(contactButton, pose.position, touchCollider.radius, out float support)) return false;
        Vector3 direction = contactButton.PressDirection;
        float penetration = Vector3.Dot(pose.position - contactButton.FaceCenter, direction) + support;
        if (penetration <= 0f) return false;
        offset = -direction * penetration;
        return true;
    }

    private static bool FaceSupport(PhysicalTableButton button, Vector3 position, float radius, out float support)
    {
        Vector3 direction = button.PressDirection;
        Vector3 delta = position - button.RestFaceCenter;
        Vector3 radial = delta - direction * Vector3.Dot(delta, direction);
        Vector3 scale = button.transform.lossyScale;
        float capRadius = button.Diameter * .5f * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
        float beyondEdge = Mathf.Max(0f, radial.magnitude - capRadius);
        support = beyondEdge < radius ? Mathf.Sqrt(radius * radius - beyondEdge * beyondEdge) : 0f;
        return support > .00001f;
    }

    private static bool Finite(Vector3 position, Quaternion rotation)
    {
        return Finite(position.x) && Finite(position.y) && Finite(position.z) &&
            Finite(rotation.x) && Finite(rotation.y) && Finite(rotation.z) && Finite(rotation.w);
    }

    private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
}
