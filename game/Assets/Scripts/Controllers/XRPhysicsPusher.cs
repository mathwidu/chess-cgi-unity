using System;
using UnityEngine;

// A world-space proxy follows a tracked contact point without inheriting its
// Transform motion. Only the physical table-button caps can receive its push.
[RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
public sealed class XRPhysicsPusher : MonoBehaviour
{
    public const int PhysicsLayer = 28;
    private const int SettleSteps = 3;
    private const float MaxPoseStep = .12f;
    private const float MaxSettleStep = .01f;
    private const float RearmClearance = .002f;

    private Transform poseSource;
    private Transform trackingOrigin;
    private Func<bool> isPoseTracked;
    private Rigidbody body;
    private SphereCollider touchCollider;
    private bool hasPose;
    private bool hasOriginPose;
    private Vector3 previousPosition;
    private Quaternion previousRotation;
    private Vector3 previousOriginPosition;
    private Quaternion previousOriginRotation;
    private int settledSteps;

    public Rigidbody Body => body;
    public SphereCollider TouchCollider => touchCollider;
    public bool IsContactEnabled => touchCollider != null && touchCollider.enabled;
    public bool IsArmed => IsContactEnabled;

    public void Configure(Transform source, Func<bool> tracked, Transform origin = null, float radius = .012f)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (tracked == null) throw new ArgumentNullException(nameof(tracked));
        InitializePhysics();
        poseSource = source;
        isPoseTracked = tracked;
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

    // The rig also calls this before a recenter, seat change or keyboard orbit.
    // No swept collision is allowed across a discontinuity in the tracked pose.
    public void RequireRearm()
    {
        if (touchCollider != null) touchCollider.enabled = false;
        if (body != null) body.detectCollisions = false;
        hasPose = false;
        hasOriginPose = false;
        settledSteps = 0;
    }

    private bool PoseIsAvailable()
    {
        return poseSource != null && poseSource.gameObject.activeInHierarchy &&
            isPoseTracked != null && isPoseTracked();
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
        if (!PoseIsAvailable() || OriginMoved() ||
            !Finite(poseSource.position, poseSource.rotation) ||
            PoseJumped(poseSource.position, poseSource.rotation))
            RequireRearm();
    }

    private void FixedUpdate()
    {
        if (!PoseIsAvailable())
        {
            RequireRearm();
            return;
        }

        Vector3 position = poseSource.position;
        Quaternion rotation = poseSource.rotation;
        if (!Finite(position, rotation))
        {
            RequireRearm();
            return;
        }

        if (OriginMoved() || PoseJumped(position, rotation)) RequireRearm();

        if (!touchCollider.enabled)
        {
            // Teleport the disabled proxy, never MovePosition it from its stale
            // pose. Reacquiring inside a cap stays disarmed until it leaves.
            body.position = position;
            body.rotation = rotation;
            bool stable = !hasPose || (position - previousPosition).sqrMagnitude <= MaxSettleStep * MaxSettleStep;
            bool outside = !Physics.CheckSphere(position, touchCollider.radius + RearmClearance,
                1 << PhysicalTableButton.PhysicsLayer, QueryTriggerInteraction.Ignore);
            settledSteps = stable && outside ? settledSteps + 1 : 0;
            if (settledSteps >= SettleSteps)
            {
                body.detectCollisions = true;
                touchCollider.enabled = true;
            }
        }
        else
        {
            body.MovePosition(position);
            body.MoveRotation(rotation);
        }

        previousPosition = position;
        previousRotation = rotation;
        hasPose = true;
        if (trackingOrigin != null)
        {
            previousOriginPosition = trackingOrigin.position;
            previousOriginRotation = trackingOrigin.rotation;
            hasOriginPose = true;
        }
    }

    private static bool Finite(Vector3 position, Quaternion rotation)
    {
        return Finite(position.x) && Finite(position.y) && Finite(position.z) &&
            Finite(rotation.x) && Finite(rotation.y) && Finite(rotation.z) && Finite(rotation.w);
    }

    private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
}
