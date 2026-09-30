using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;

// Contact poses are raw tracking/model-local data. Only the rendered skeleton
// receives the contact offset; aiming, near casting and attach poses stay raw.
[DefaultExecutionOrder(50)]
public sealed class XRPhysicalHand : MonoBehaviour
{
    public delegate bool JointPoseReader(XRHandJointID joint, out Pose pose);

    private readonly struct ContactDefinition
    {
        public readonly string Name;
        public readonly XRHandJointID First;
        public readonly XRHandJointID Second;
        public readonly float Radius;
        public ContactDefinition(string name, XRHandJointID first, float radius, XRHandJointID second = XRHandJointID.Invalid)
        {
            Name = name;
            First = first;
            Second = second;
            Radius = radius;
        }
    }

    private static readonly ContactDefinition[] Anatomy =
    {
        new ContactDefinition("Palm", XRHandJointID.Palm, .022f),
        new ContactDefinition("Palm heel", XRHandJointID.Wrist, .018f, XRHandJointID.Palm),
        new ContactDefinition("Palm index", XRHandJointID.Palm, .018f, XRHandJointID.IndexProximal),
        new ContactDefinition("Palm little", XRHandJointID.Palm, .018f, XRHandJointID.LittleProximal),
        new ContactDefinition("Index knuckle", XRHandJointID.IndexProximal, .013f),
        new ContactDefinition("Middle knuckle", XRHandJointID.MiddleProximal, .013f),
        new ContactDefinition("Ring knuckle", XRHandJointID.RingProximal, .013f),
        new ContactDefinition("Little knuckle", XRHandJointID.LittleProximal, .011f),
        new ContactDefinition("Thumb tip", XRHandJointID.ThumbTip, .010f),
        // The imported fingertip extends about 2 mm past this joint; 8 mm
        // support leaves a small contact margin instead of a floating fingertip.
        new ContactDefinition("Index tip", XRHandJointID.IndexTip, .008f),
        new ContactDefinition("Middle tip", XRHandJointID.MiddleTip, .011f),
        new ContactDefinition("Ring tip", XRHandJointID.RingTip, .010f),
        new ContactDefinition("Little tip", XRHandJointID.LittleTip, .009f),
        new ContactDefinition("Thumb pad", XRHandJointID.ThumbDistal, .009f, XRHandJointID.ThumbTip),
        new ContactDefinition("Index pad", XRHandJointID.IndexDistal, .009f, XRHandJointID.IndexTip),
        new ContactDefinition("Middle pad", XRHandJointID.MiddleDistal, .009f, XRHandJointID.MiddleTip),
        new ContactDefinition("Ring pad", XRHandJointID.RingDistal, .009f, XRHandJointID.RingTip),
        new ContactDefinition("Little pad", XRHandJointID.LittleDistal, .008f, XRHandJointID.LittleTip),
    };

    private sealed class ContactPose
    {
        private readonly XRPhysicalHand owner;
        private readonly ContactDefinition definition;
        public ContactPose(XRPhysicalHand owner, ContactDefinition definition)
        {
            this.owner = owner;
            this.definition = definition;
        }
        public bool Read(out Pose pose)
        {
            pose = default;
            if (owner == null || !owner.isActiveAndEnabled || owner.readJoint == null ||
                !owner.readJoint(definition.First, out pose)) return false;
            if (definition.Second == XRHandJointID.Invalid) return true;
            if (!owner.readJoint(definition.Second, out Pose second)) return false;
            pose.position = (pose.position + second.position) * .5f;
            return true;
        }
    }

    private readonly List<XRPhysicsPusher> contacts = new List<XRPhysicsPusher>(Anatomy.Length);
    private readonly Transform[] controllerBones = new Transform[(int)XRHandJointID.EndMarker];
    private Transform visualRoot;
    private Vector3 baseLocalPosition;
    private Quaternion baseLocalRotation;
    private Vector3 baseLocalScale;
    private Func<bool> controllerTracked;
    private JointPoseReader readJoint;
    private XRHandSkeletonDriver skeleton;
    private GameObject handInteractor;
    private Transform trackingSpace;
    private bool configured;
    private bool handQuarantined = true;
    private bool resettingContacts;
    private int clearSteps;
    private Pose rawSkeletonRoot;
    private bool hasSkeletonRoot;

    public IReadOnlyList<XRPhysicsPusher> Contacts => contacts;
    public Vector3 VisualOffset { get; private set; }

    // This overload is also the explicit seam for pose-replay tests. A reader
    // returns world poses and must never read a visually corrected Transform.
    public void Configure(Transform modelRoot, Transform physicsParent, Transform origin, JointPoseReader rawJointPose)
    {
        if (modelRoot == null) throw new ArgumentNullException(nameof(modelRoot));
        if (physicsParent == null) throw new ArgumentNullException(nameof(physicsParent));
        if (rawJointPose == null) throw new ArgumentNullException(nameof(rawJointPose));
        if (configured) throw new InvalidOperationException("A physical hand is configured once.");
        visualRoot = modelRoot;
        baseLocalPosition = visualRoot.localPosition;
        baseLocalRotation = visualRoot.localRotation;
        baseLocalScale = visualRoot.localScale;
        readJoint = rawJointPose;
        configured = true;
        foreach (ContactDefinition definition in Anatomy)
        {
            var contactObject = new GameObject(modelRoot.name + " " + definition.Name + " Contact");
            contactObject.transform.SetParent(physicsParent, false);
            XRPhysicsPusher pusher = contactObject.AddComponent<XRPhysicsPusher>();
            pusher.Configure(new ContactPose(this, definition).Read, origin, definition.Radius);
            pusher.RearmAllowed = HandCanRearm;
            pusher.RearmRequired += ContactRearmRequired;
            contacts.Add(pusher);
        }
    }

    public void ConfigureController(Transform modelRoot, Transform physicsParent, Transform origin, Func<bool> tracked, bool leftHand)
    {
        if (modelRoot == null) throw new ArgumentNullException(nameof(modelRoot));
        if (tracked == null) throw new ArgumentNullException(nameof(tracked));
        controllerTracked = tracked;
        string prefix = leftHand ? "L_" : "R_";
        foreach (Transform bone in modelRoot.GetComponentsInChildren<Transform>(true))
        {
            for (int i = (int)XRHandJointID.Wrist; i < (int)XRHandJointID.EndMarker; i++)
                if (bone.name == prefix + (i == (int)XRHandJointID.Wrist ? "Wrist" : ((XRHandJointID)i).ToString())) controllerBones[i] = bone;
        }
        Configure(modelRoot, physicsParent, origin, ReadControllerJoint);
    }

    public void ConfigureTrackedHand(XRHandSkeletonDriver driver, GameObject interactor,
        Transform originSpace, Transform physicsParent, Transform origin)
    {
        if (driver == null || driver.handTrackingEvents == null || driver.rootTransform == null)
            throw new ArgumentException("A tracked hand requires its skeleton and tracking events.", nameof(driver));
        if (interactor == null) throw new ArgumentNullException(nameof(interactor));
        if (originSpace == null) throw new ArgumentNullException(nameof(originSpace));
        skeleton = driver;
        handInteractor = interactor;
        trackingSpace = originSpace;
        if (driver.handTrackingEvents.handIsTracked)
        {
            rawSkeletonRoot = driver.handTrackingEvents.rootPose;
            hasSkeletonRoot = true;
        }
        Configure(driver.transform, physicsParent, origin, ReadTrackedJoint);
        SubscribeHandEvents();
    }

    public bool TryGetRawJointPose(XRHandJointID joint, out Pose pose)
    {
        pose = default;
        return isActiveAndEnabled && readJoint != null && readJoint(joint, out pose);
    }

    private bool ReadControllerJoint(XRHandJointID joint, out Pose pose)
    {
        pose = default;
        int index = (int)joint;
        if (visualRoot == null || !visualRoot.gameObject.activeInHierarchy || controllerTracked == null ||
            !controllerTracked() || index < 0 || index >= controllerBones.Length || controllerBones[index] == null) return false;
        Transform bone = controllerBones[index];
        // InverseTransformPoint cancels the visual root's current offset. The
        // saved base matrix reapplies its uncorrected offset under the raw TPD.
        Matrix4x4 parentMatrix = visualRoot.parent != null ? visualRoot.parent.localToWorldMatrix : Matrix4x4.identity;
        Matrix4x4 rawModel = parentMatrix * Matrix4x4.TRS(baseLocalPosition, baseLocalRotation, baseLocalScale);
        Quaternion parentRotation = visualRoot.parent != null ? visualRoot.parent.rotation : Quaternion.identity;
        pose = new Pose(rawModel.MultiplyPoint3x4(visualRoot.InverseTransformPoint(bone.position)),
            parentRotation * baseLocalRotation * Quaternion.Inverse(visualRoot.rotation) * bone.rotation);
        return true;
    }

    private bool ReadTrackedJoint(XRHandJointID joint, out Pose pose)
    {
        pose = default;
        if ((int)joint < (int)XRHandJointID.Wrist || (int)joint >= (int)XRHandJointID.EndMarker) return false;
        if (skeleton == null || !skeleton.isActiveAndEnabled || handInteractor == null ||
            !handInteractor.activeInHierarchy || trackingSpace == null) return false;
        XRHandTrackingEvents events = skeleton.handTrackingEvents;
        if (events == null || !events.isActiveAndEnabled || !events.handIsTracked || events.subsystem == null) return false;
        XRHand hand = events.handedness == Handedness.Left ? events.subsystem.leftHand : events.subsystem.rightHand;
        if (!hand.isTracked || !hand.GetJoint(joint).TryGetPose(out Pose localPose)) return false;
        pose = new Pose(trackingSpace.TransformPoint(localPose.position), trackingSpace.rotation * localPose.rotation);
        return true;
    }

    private void OnEnable()
    {
        Application.onBeforeRender += BeforeRender;
        if (configured)
        {
            SubscribeHandEvents();
            RequireRearm();
        }
    }

    private void OnDisable()
    {
        Application.onBeforeRender -= BeforeRender;
        UnsubscribeHandEvents();
        RequireRearm();
    }

    private void SubscribeHandEvents()
    {
        if (skeleton == null || skeleton.handTrackingEvents == null) return;
        // Subscribe after the skeleton driver, so its raw root update cannot
        // replace the visual block in the same Dynamic/BeforeRender callback.
        skeleton.handTrackingEvents.poseUpdated.RemoveListener(HandPoseUpdated);
        skeleton.handTrackingEvents.poseUpdated.AddListener(HandPoseUpdated);
        skeleton.handTrackingEvents.trackingLost.RemoveListener(RequireRearm);
        skeleton.handTrackingEvents.trackingLost.AddListener(RequireRearm);
    }

    private void UnsubscribeHandEvents()
    {
        if (skeleton == null || skeleton.handTrackingEvents == null) return;
        skeleton.handTrackingEvents.poseUpdated.RemoveListener(HandPoseUpdated);
        skeleton.handTrackingEvents.trackingLost.RemoveListener(RequireRearm);
    }

    private void HandPoseUpdated(Pose pose)
    {
        rawSkeletonRoot = pose;
        hasSkeletonRoot = true;
        UpdateVisual();
    }
    private void LateUpdate() => UpdateVisual();

    private bool HandCanRearm() => !handQuarantined;

    private void FixedUpdate()
    {
        if (!configured || !handQuarantined) return;
        bool clear = true;
        bool trackedSample = false;
        for (int i = 0; i < contacts.Count; i++)
        {
            if (contacts[i] == null || !contacts[i].TryReadPose(out Pose pose)) continue;
            trackedSample = true;
            if (!contacts[i].IsClearForRearm(pose)) clear = false;
        }
        clearSteps = clear && trackedSample ? clearSteps + 1 : 0;
        if (clearSteps >= 3) handQuarantined = false;
    }

    [BeforeRenderOrder(10000)]
    private void BeforeRender() => UpdateVisual();

    private void UpdateVisual()
    {
        if (!configured || visualRoot == null) return;
        Vector3 offset = Vector3.zero;
        // Button faces on this panel are parallel. The greatest correction also
        // keeps the other contact samples outside their actual cap faces.
        for (int i = 0; i < contacts.Count; i++)
            if (contacts[i] != null && contacts[i].TryGetVisualOffset(out Vector3 candidate) &&
                candidate.sqrMagnitude > offset.sqrMagnitude) offset = candidate;
        ApplyVisualOffset(offset);
    }

    private void ApplyVisualOffset(Vector3 offset)
    {
        VisualOffset = offset;
        if (skeleton != null && skeleton.rootTransform != null)
        {
            if (offset.sqrMagnitude > 0f) skeleton.ApplyRootPoseOffset(offset);
            else skeleton.ResetRootPoseOffset();
            if (hasSkeletonRoot)
            {
                Transform parent = skeleton.rootTransform.parent;
                Vector3 localOffset = parent != null ? parent.InverseTransformVector(offset) : offset;
                skeleton.rootTransform.localPosition = rawSkeletonRoot.position + localOffset;
            }
        }
        else if (visualRoot != null)
        {
            Transform parent = visualRoot.parent;
            visualRoot.localPosition = baseLocalPosition + (parent != null ? parent.InverseTransformVector(offset) : offset);
        }
    }

    public void RequireRearm()
    {
        ContactRearmRequired(false);
    }

    private void ContactRearmRequired(bool retainVisualContact)
    {
        if (resettingContacts) return;
        handQuarantined = true;
        clearSteps = 0;
        resettingContacts = true;
        try
        {
            for (int i = 0; i < contacts.Count; i++)
                if (contacts[i] != null) contacts[i].DisarmForHand(retainVisualContact);
        }
        finally { resettingContacts = false; }
        if (!retainVisualContact) ApplyVisualOffset(Vector3.zero);
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus) RequireRearm();
    }

    private void OnApplicationPause(bool isPaused)
    {
        if (isPaused) RequireRearm();
    }

    private void OnDestroy()
    {
        UnsubscribeHandEvents();
        for (int i = 0; i < contacts.Count; i++)
            if (contacts[i] != null)
            {
                contacts[i].RearmRequired -= ContactRearmRequired;
                Destroy(contacts[i].gameObject);
            }
    }
}
