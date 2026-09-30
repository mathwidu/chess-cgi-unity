using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Hands.ProviderImplementation;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Attachment;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Casters;

public sealed class XRPhysicalHandTests
{
    private GameObject root;
    private Transform origin;
    private Transform rawController;
    private GameObject model;
    private XRPhysicalHand hand;
    private bool tracked;
    private int presses;
    private XRHandSubsystem replaySubsystem;

    [SetUp]
    public void SetUp()
    {
        presses = 0;
        root = new GameObject("Physical hand fixture");
        root.transform.position = new Vector3(48f, 8f, 0f);
        origin = new GameObject("Raw origin").transform;
        origin.SetParent(root.transform, false);
        rawController = new GameObject("Raw controller pose").transform;
        rawController.SetParent(origin, false);
        GameObject prefab = Resources.Load<GameObject>("XR/LeftControllerHand");
        Assert.That(prefab, Is.Not.Null, "Use the actual visible hand model and its imported bones.");
        model = Object.Instantiate(prefab, rawController, false);
        tracked = true;
        hand = model.AddComponent<XRPhysicalHand>();
        hand.ConfigureController(model.transform, root.transform, origin, () => tracked, true);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(root);
        if (replaySubsystem != null)
        {
            replaySubsystem.Stop();
            replaySubsystem.Destroy();
            replaySubsystem = null;
        }
    }

    [Test]
    public void PalmAndEveryFingertipHaveIndependentButtonOnlyContacts()
    {
        Assert.That(hand.Contacts.Count, Is.EqualTo(18));
        Assert.That(FindContact("Palm Contact").TouchCollider.radius, Is.GreaterThan(.02f));
        foreach (string name in new[] { "Thumb", "Index", "Middle", "Ring", "Little" })
            Assert.That(FindContact(name + " tip Contact"), Is.Not.Null);
        foreach (XRPhysicsPusher contact in hand.Contacts)
        {
            Assert.That(contact.transform.IsChildOf(rawController), Is.False,
                "Tracked Transforms cannot teleport rigidbodies through the scene hierarchy.");
            Assert.That(contact.TouchCollider.excludeLayers.value, Is.EqualTo(~(1 << PhysicalTableButton.PhysicsLayer)));
            Assert.That(contact.Body.isKinematic, Is.True);
        }
        Assert.That(hand.TryGetRawJointPose(XRHandJointID.Palm, out _), Is.True);
    }

    [UnityTest]
    public IEnumerator PalmPressBlocksTheActualModelWithoutChangingTrackingOrFeedingBack()
    {
        PhysicalTableButton button = CreateButtonInFrontOfPalm();
        XRPhysicsPusher palm = FindContact("Palm Contact");
        yield return FixedSteps(8);
        Assert.That(palm.IsArmed, Is.True);
        Vector3 initialController = rawController.position;
        for (int i = 0; i < 24; i++)
        {
            rawController.position += button.PressDirection * .003f;
            yield return new WaitForFixedUpdate();
        }
        yield return FixedSteps(6);
        yield return null;
        Assert.That(palm.ContactButton, Is.SameAs(button));
        Assert.That(button.PressFraction, Is.GreaterThan(.7f));
        Assert.That(presses, Is.EqualTo(1));
        Assert.That(hand.VisualOffset.magnitude, Is.GreaterThan(.005f), "An open palm receives a visible stop.");
        Assert.That(Vector3.Distance(rawController.position, initialController + button.PressDirection * .072f), Is.LessThan(.0001f));
        Assert.That(hand.TryGetRawJointPose(XRHandJointID.Palm, out Pose rawPalm), Is.True);
        Assert.That(Vector3.Distance(palm.RawPose.position, rawPalm.position), Is.LessThan(.0001f));
        Assert.That(Vector3.Dot(palm.TargetPose.position - button.RestFaceCenter, button.PressDirection) + palm.TouchCollider.radius,
            Is.LessThanOrEqualTo(PhysicalTableButton.Stroke + .0001f));
        Transform palmBone = FindBone("L_Palm");
        Assert.That(Vector3.Distance(palmBone.position, rawPalm.position + hand.VisualOffset), Is.LessThan(.0001f));
        Assert.That(Vector3.Dot(palmBone.position - button.FaceGraphic.transform.position, button.PressDirection) + palm.TouchCollider.radius,
            Is.LessThanOrEqualTo(.0005f), "The palm skin contact cannot pass through the visible cap face.");
        Vector3 rawBeforeHold = rawPalm.position;
        yield return FixedSteps(12);
        yield return null;
        hand.TryGetRawJointPose(XRHandJointID.Palm, out rawPalm);
        Assert.That(Vector3.Distance(rawPalm.position, rawBeforeHold), Is.LessThan(.0001f),
            "Repeated visual corrections cannot drift the next physical pose.");

        rawController.position += button.transform.right * .09f;
        yield return FixedSteps(3);
        yield return null;
        Assert.That(hand.VisualOffset.magnitude, Is.LessThan(.0001f), "Lateral withdrawal releases the rendered hand.");
    }

    [UnityTest]
    public IEnumerator PointingIndexPressStopsTheRealSkinAtTheRenderedFaceAndWithdrawsOnce()
    {
        ControllerHandPose pose = model.AddComponent<ControllerHandPose>();
        pose.Configure(null, true);
        Transform proximal = FindBone("L_IndexProximal");
        Transform intermediate = FindBone("L_IndexIntermediate");
        Transform distal = FindBone("L_IndexDistal");
        Quaternion proximalRest = proximal.localRotation;
        Quaternion intermediateRest = intermediate.localRotation;
        Quaternion distalRest = distal.localRotation;
        pose.SetPinch(1f);
        proximal.localRotation = proximalRest;
        intermediate.localRotation = intermediateRest;
        distal.localRotation = distalRest;
        pose.enabled = false;
        foreach (Animator animator in model.GetComponentsInChildren<Animator>(true)) animator.enabled = false;

        Assert.That(hand.TryGetRawJointPose(XRHandJointID.IndexTip, out Pose rawTip), Is.True);
        Assert.That(hand.TryGetRawJointPose(XRHandJointID.IndexDistal, out Pose rawDistal), Is.True);
        Vector3 direction = (rawTip.position - rawDistal.position).normalized;
        Assert.That(direction.sqrMagnitude, Is.GreaterThan(.99f));
        var mount = new GameObject("Pointing index button");
        mount.transform.SetParent(root.transform, false);
        mount.transform.rotation = Quaternion.LookRotation(direction,
            Mathf.Abs(Vector3.Dot(direction, Vector3.up)) > .9f ? Vector3.forward : Vector3.up);
        mount.transform.position = rawTip.position + direction * (.10f + PhysicalTableButton.CapDepth * .5f);
        PhysicalTableButton button = mount.AddComponent<PhysicalTableButton>();
        button.Configure(.09f, Color.blue, false, false, () => presses++);
        Physics.SyncTransforms();
        XRPhysicsPusher fingertip = FindContact("Index tip Contact");
        yield return FixedSteps(8);
        yield return null;
        Assert.That(fingertip.IsArmed, Is.True);

        for (int i = 0; i < 70; i++)
        {
            rawController.position += direction * .003f;
            yield return new WaitForFixedUpdate();
            yield return null;
        }
        for (int i = 0; i < 10; i++)
        {
            yield return new WaitForFixedUpdate();
            yield return null;
        }
        Assert.That(presses, Is.EqualTo(1), "A sustained pointing finger produces one physical activation.");
        Assert.That(fingertip.ContactButton, Is.SameAs(button));
        Assert.That(button.PressFraction, Is.GreaterThan(.7f));
        Assert.That(hand.VisualOffset.magnitude, Is.GreaterThan(.02f),
            "The visible finger stops while the raw controller continues into the cap.");

        SkinnedMeshRenderer skin = model.GetComponentInChildren<SkinnedMeshRenderer>();
        Assert.That(skin, Is.Not.Null, "Measure the imported hand skin, rather than its bone or proxy radius.");
        var baked = new Mesh();
        try
        {
            skin.BakeMesh(baked);
            Vector3 faceCenter = button.FaceGraphic.transform.position;
            Vector3 pressDirection = button.PressDirection;
            float centralRadiusSquared = Mathf.Pow(button.Diameter * .48f, 2f);
            float maximumSignedDistance = float.NegativeInfinity;
            int centralVertices = 0;
            foreach (Vector3 vertex in baked.vertices)
            {
                Vector3 delta = skin.transform.TransformPoint(vertex) - faceCenter;
                float signedDistance = Vector3.Dot(delta, pressDirection);
                Vector3 radial = delta - pressDirection * signedDistance;
                if (radial.sqrMagnitude > centralRadiusSquared) continue;
                maximumSignedDistance = Mathf.Max(maximumSignedDistance, signedDistance);
                centralVertices++;
            }
            Assert.That(centralVertices, Is.GreaterThan(0), "The pointing skin overlaps the cap's central disk.");
            Assert.That(maximumSignedDistance, Is.InRange(-.008f, .004f),
                "The rendered fingertip must stay close to the drawn face without penetrating it.");
        }
        finally
        {
            Object.DestroyImmediate(baked);
        }

        for (int i = 0; i < 70; i++)
        {
            rawController.position -= direction * .003f;
            yield return new WaitForFixedUpdate();
            yield return null;
        }
        yield return FixedSteps(10);
        yield return null;
        Assert.That(presses, Is.EqualTo(1), "Withdrawing the finger cannot activate the cap again.");
        Assert.That(hand.VisualOffset.magnitude, Is.LessThan(.0001f));
        Assert.That(fingertip.IsArmed, Is.True, "The withdrawn finger is ready for a fresh physical press.");
    }

    [UnityTest]
    public IEnumerator ContactDoesNotChangeTheNearPoseOrCancelASelectedPiece()
    {
        XRInteractionManager manager = root.AddComponent<XRInteractionManager>();
        NearFarInteractor interactor = CreateInteractor(manager);
        GameObject piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
        piece.name = "Raw near-grab target";
        piece.transform.SetParent(root.transform, false);
        piece.transform.position = rawController.position;
        piece.transform.localScale = Vector3.one * .025f;
        piece.layer = PieceView.PhysicsLayer;
        XRGrabInteractable grab = piece.AddComponent<XRGrabInteractable>();
        grab.interactionManager = manager;
        grab.GetComponent<Rigidbody>().useGravity = false;
        interactor.selectInput.QueueManualState(true, 1f);
        yield return null;
        yield return null;
        yield return null;
        Assert.That(grab.isSelected, Is.True, "The manager selects a real nearby grab target.");
        Assert.That(interactor.hasSelection, Is.True);

        PhysicalTableButton button = CreateButtonInFrontOfPalm();
        yield return FixedSteps(8);
        for (int i = 0; i < 24; i++)
        {
            rawController.position += button.PressDirection * .003f;
            yield return new WaitForFixedUpdate();
        }
        yield return null;
        Assert.That(hand.VisualOffset.magnitude, Is.GreaterThan(.005f));
        Assert.That(interactor.hasSelection, Is.True);
        Assert.That(grab.isSelected, Is.True, "Button contact never mutates the XR selection lifecycle.");
        Assert.That(((SphereInteractionCaster)interactor.nearInteractionCaster).castOrigin.position, Is.EqualTo(rawController.position));
        Assert.That(Vector3.Distance(grab.transform.position, rawController.position), Is.LessThan(.015f),
            "A grabbed piece continues following the raw near pose while only the hand visual is corrected.");
    }

    [UnityTest]
    public IEnumerator TrackingAndOriginResetClearTheVisualAndQuarantineEveryContact()
    {
        PhysicalTableButton button = CreateButtonInFrontOfPalm();
        yield return FixedSteps(8);
        for (int i = 0; i < 24; i++)
        {
            rawController.position += button.PressDirection * .003f;
            yield return new WaitForFixedUpdate();
        }
        yield return null;
        Assert.That(hand.VisualOffset.magnitude, Is.GreaterThan(.005f));
        tracked = false;
        yield return null;
        yield return null;
        Assert.That(hand.VisualOffset.magnitude, Is.LessThan(.0001f));
        foreach (XRPhysicsPusher contact in hand.Contacts) Assert.That(contact.IsArmed, Is.False);
        tracked = true;
        yield return FixedSteps(5);
        Assert.That(FindContact("Palm Contact").IsArmed, Is.False);
        origin.position += Vector3.up * .01f;
        yield return new WaitForFixedUpdate();
        Assert.That(FindContact("Palm Contact").IsArmed, Is.False);
        hand.RequireRearm();
        Assert.That(hand.VisualOffset, Is.EqualTo(Vector3.zero));
    }

    [UnityTest]
    public IEnumerator AHeightStepQuarantinesTheWholeHandUntilEveryContactWithdraws()
    {
        PhysicalTableButton button = CreateButtonInFrontOfPalm();
        yield return FixedSteps(8);
        // Use the same guided mechanism and relocation contract as TableView.
        // The callback moves the whole mount while palm/knuckles are advancing.
        int lastPresses = 0;
        for (int i = 0; i < 35; i++)
        {
            rawController.position += button.PressDirection * .003f;
            yield return new WaitForFixedUpdate();
            if (presses == lastPresses) continue;
            lastPresses = presses;
            button.PrepareMountMove();
            button.transform.position += Vector3.up * .03f;
            button.FinishMountMove();
        }
        yield return FixedSteps(10);
        Assert.That(presses, Is.EqualTo(1), "Samples of one held hand cannot activate the newly relocated cap again.");
        foreach (XRPhysicsPusher contact in hand.Contacts)
            Assert.That(contact.IsArmed, Is.False, "Rearming is shared across all anatomical samples.");

        rawController.position += button.transform.right * .2f;
        yield return FixedSteps(10);
        foreach (XRPhysicsPusher contact in hand.Contacts)
            Assert.That(contact.IsArmed, Is.True, "Leaving the panel with the whole hand permits a fresh press.");
    }

    [UnityTest]
    public IEnumerator TrackedSkeletonUsesOriginSpaceJointsAndResetsItsOwnVisualOffset()
    {
        Object.DestroyImmediate(model);
        origin.rotation = Quaternion.Euler(0f, 33f, 0f);
        model = Object.Instantiate(Resources.Load<GameObject>("XR/LeftHandVisual"), origin, false);
        XRHandSkeletonDriver skeleton = model.GetComponentInChildren<XRHandSkeletonDriver>();
        Assert.That(skeleton, Is.Not.Null);
        var descriptors = new List<XRHandSubsystemDescriptor>();
        SubsystemManager.GetSubsystemDescriptors(descriptors);
        const string providerId = "ChessCgi.PhysicalHandReplay";
        XRHandSubsystemDescriptor descriptor = descriptors.Find(candidate => candidate.id == providerId);
        if (descriptor == null)
        {
            XRHandSubsystemDescriptor.Register(new XRHandSubsystemDescriptor.Cinfo
            {
                id = providerId, providerType = typeof(HandReplayProvider), subsystemTypeOverride = typeof(HandReplaySubsystem),
            });
            descriptors.Clear();
            SubsystemManager.GetSubsystemDescriptors(descriptors);
            descriptor = descriptors.Find(candidate => candidate.id == providerId);
        }
        replaySubsystem = descriptor.Create();
        HandReplayProvider replay = ((HandReplaySubsystem)replaySubsystem).Replay;
        foreach (JointToTransformReference joint in skeleton.jointTransformReferences)
            replay.Joints[joint.xrHandJointID.ToIndex()] = new Pose(origin.InverseTransformPoint(joint.jointTransform.position),
                Quaternion.Inverse(origin.rotation) * joint.jointTransform.rotation);
        replaySubsystem.Start();
        skeleton.handTrackingEvents.updateType = XRHandTrackingEvents.UpdateTypes.Dynamic | XRHandTrackingEvents.UpdateTypes.BeforeRender;
        hand = model.AddComponent<XRPhysicalHand>();
        hand.ConfigureTrackedHand(skeleton, rawController.gameObject, origin, root.transform, origin);
        yield return null; // TrackingEvents discovers this running, fixture-owned provider.
        Assert.That(skeleton.handTrackingEvents.subsystem, Is.SameAs(replaySubsystem));
        UpdateReplay();
        Assert.That(hand.TryGetRawJointPose(XRHandJointID.Palm, out Pose rawPalm), Is.True);
        Assert.That(Vector3.Distance(rawPalm.position, origin.TransformPoint(replay.Joints[XRHandJointID.Palm.ToIndex()].position)), Is.LessThan(.0001f));
        PhysicalTableButton button = CreateButtonInFrontOfPalm();
        yield return FixedSteps(8);
        for (int i = 0; i < 24; i++)
        {
            replay.Offset += origin.InverseTransformVector(button.PressDirection * .003f);
            UpdateReplay();
            yield return new WaitForFixedUpdate();
        }
        yield return null;
        UpdateReplay();
        Assert.That(hand.VisualOffset.magnitude, Is.GreaterThan(.005f));
        Vector3 rawWrist = origin.TransformPoint(replay.Joints[XRHandJointID.Wrist.ToIndex()].position + replay.Offset);
        Assert.That(Vector3.Distance(skeleton.rootTransform.position, rawWrist + hand.VisualOffset), Is.LessThan(.0005f),
            "The driver offset is applied in the skeleton parent's space; raw joints remain in origin space.");
        hand.TryGetRawJointPose(XRHandJointID.Palm, out rawPalm);
        Assert.That(Vector3.Distance(rawPalm.position, origin.TransformPoint(replay.Joints[XRHandJointID.Palm.ToIndex()].position + replay.Offset)), Is.LessThan(.0001f));
        replay.Tracked = false;
        UpdateReplay();
        Assert.That(hand.VisualOffset, Is.EqualTo(Vector3.zero));
        Assert.That(Vector3.Distance(skeleton.rootTransform.position, rawWrist), Is.LessThan(.0005f),
            "Tracking loss resets the visual driver's last known offset immediately.");
        foreach (XRPhysicsPusher contact in hand.Contacts) Assert.That(contact.IsArmed, Is.False);
        replay.Tracked = true;
        UpdateReplay();
        yield return FixedSteps(8);
        Assert.That(FindContact("Palm Contact").IsArmed, Is.False, "Reacquiring a palm still overpressed cannot trigger another step.");
    }

    private void UpdateReplay()
    {
        replaySubsystem.TryUpdateHands(XRHandSubsystem.UpdateType.Dynamic);
        replaySubsystem.TryUpdateHands(XRHandSubsystem.UpdateType.BeforeRender);
    }

    public sealed class HandReplaySubsystem : XRHandSubsystem
    {
        public HandReplayProvider Replay => (HandReplayProvider)provider;
    }

    public sealed class HandReplayProvider : XRHandSubsystemProvider
    {
        public readonly Pose[] Joints = new Pose[XRHandJointID.EndMarker.ToIndex()];
        public Vector3 Offset;
        public bool Tracked = true;
        public override void Start() { }
        public override void Stop() { }
        public override void Destroy() { }
        public override void GetHandLayout(NativeArray<bool> layout)
        {
            for (int i = 0; i < layout.Length; i++) layout[i] = true;
        }
        public override XRHandSubsystem.UpdateSuccessFlags TryUpdateHands(XRHandSubsystem.UpdateType updateType,
            ref Pose leftRoot, NativeArray<XRHandJoint> leftJoints, ref Pose rightRoot, NativeArray<XRHandJoint> rightJoints)
        {
            if (!Tracked) return XRHandSubsystem.UpdateSuccessFlags.None;
            leftRoot = Joints[XRHandJointID.Wrist.ToIndex()];
            leftRoot.position += Offset;
            for (int i = 0; i < leftJoints.Length; i++)
            {
                Pose pose = Joints[i];
                pose.position += Offset;
                leftJoints[i] = XRHandProviderUtility.CreateJoint(Handedness.Left,
                    XRHandJointTrackingState.Pose, XRHandJointIDUtility.FromIndex(i), pose);
            }
            return XRHandSubsystem.UpdateSuccessFlags.LeftHandRootPose | XRHandSubsystem.UpdateSuccessFlags.LeftHandJoints;
        }
    }

    private NearFarInteractor CreateInteractor(XRInteractionManager manager)
    {
        rawController.gameObject.SetActive(false);
        origin.gameObject.SetActive(false);
        var floorOffset = new GameObject("Hand fixture camera offset");
        floorOffset.transform.SetParent(origin, false);
        var cameraObject = new GameObject("Hand fixture camera");
        cameraObject.transform.SetParent(floorOffset.transform, false);
        Camera fixtureCamera = cameraObject.AddComponent<Camera>();
        fixtureCamera.enabled = false;
        XROrigin xrOrigin = origin.gameObject.AddComponent<XROrigin>();
        xrOrigin.Origin = origin.gameObject;
        xrOrigin.CameraFloorOffsetObject = floorOffset;
        xrOrigin.Camera = fixtureCamera;
        xrOrigin.CameraYOffset = 0f;
        origin.gameObject.SetActive(true);
        var near = rawController.gameObject.AddComponent<SphereInteractionCaster>();
        near.castOrigin = rawController;
        near.castRadius = .06f;
        near.physicsLayerMask = ~(1 << XRPhysicsPusher.PhysicsLayer);
        var far = rawController.gameObject.AddComponent<CurveInteractionCaster>();
        var attach = rawController.gameObject.AddComponent<InteractionAttachController>();
        var interactor = rawController.gameObject.AddComponent<NearFarInteractor>();
        interactor.interactionManager = manager;
        interactor.nearInteractionCaster = near;
        interactor.farInteractionCaster = far;
        interactor.interactionAttachController = attach;
        interactor.enableFarCasting = false;
        interactor.selectInput = new XRInputButtonReader("Hand fixture select")
        {
            inputSourceMode = XRInputButtonReader.InputSourceMode.ManualValue,
        };
        rawController.gameObject.SetActive(true);
        return interactor;
    }

    private PhysicalTableButton CreateButtonInFrontOfPalm()
    {
        Assert.That(hand.TryGetRawJointPose(XRHandJointID.Palm, out Pose palm), Is.True);
        var mount = new GameObject("Palm cap fixture");
        mount.transform.SetParent(root.transform, false);
        hand.TryGetRawJointPose(XRHandJointID.IndexProximal, out Pose index);
        hand.TryGetRawJointPose(XRHandJointID.LittleProximal, out Pose little);
        hand.TryGetRawJointPose(XRHandJointID.MiddleProximal, out Pose middle);
        hand.TryGetRawJointPose(XRHandJointID.Wrist, out Pose wrist);
        Vector3 pressDirection = Vector3.Cross(index.position - little.position, middle.position - wrist.position).normalized;
        Assert.That(pressDirection.sqrMagnitude, Is.GreaterThan(.9f));
        mount.transform.rotation = Quaternion.LookRotation(pressDirection, Mathf.Abs(Vector3.Dot(pressDirection, Vector3.up)) < .9f ? Vector3.up : Vector3.forward);
        mount.transform.position = palm.position + pressDirection * (.022f + .025f + PhysicalTableButton.CapDepth * .5f);
        var button = mount.AddComponent<PhysicalTableButton>();
        presses = 0;
        button.Configure(.07f, Color.blue, false, false, () => presses++);
        Physics.SyncTransforms();
        return button;
    }

    private XRPhysicsPusher FindContact(string suffix)
    {
        foreach (XRPhysicsPusher contact in hand.Contacts)
            if (contact.name.EndsWith(suffix)) return contact;
        Assert.Fail("Missing anatomical contact " + suffix);
        return null;
    }

    private Transform FindBone(string name)
    {
        foreach (Transform bone in model.GetComponentsInChildren<Transform>(true))
            if (bone.name == name) return bone;
        Assert.Fail("Missing imported bone " + name);
        return null;
    }

    private static IEnumerator FixedSteps(int count)
    {
        for (int i = 0; i < count; i++) yield return new WaitForFixedUpdate();
    }
}
