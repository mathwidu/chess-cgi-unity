#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Unity.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Hands.ProviderImplementation;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;

public sealed class HandContactSkinTests
{
    private GameObject root;
    private XRSimulatedHMD headset;
    private XRHandSubsystem subsystem;

    [SetUp]
    public void SetUp()
    {
        headset = InputSystem.AddDevice<XRSimulatedHMD>();
        root = new GameObject("Visible palm contact replay");
        root.transform.position = new Vector3(48f, 8f, 0f);
        root.AddComponent<XRInteractionManager>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(root);
        if (subsystem != null)
        {
            subsystem.Stop();
            subsystem.Destroy();
        }
        if (headset != null && headset.added) InputSystem.RemoveDevice(headset);
    }

    [UnityTest]
    public IEnumerator ARealPieceRestsAboveTheVisibleLeftPalm() => CheckPalm(true);

    [UnityTest]
    public IEnumerator ARealPieceRestsAboveTheVisibleRightPalm() => CheckPalm(false);

    [UnityTest]
    public IEnumerator ALeftPalmRisingBeforePhysicsDoesNotEngulfTheSupportedPiece() => CheckPalm(true, .005f);

    [UnityTest]
    public IEnumerator ARightPalmRisingBeforePhysicsDoesNotEngulfTheSupportedPiece() => CheckPalm(false, .005f);

    [UnityTest]
    public IEnumerator ALeftPalmLoweringBeforePhysicsDoesNotPullTheSupportedPiece() => CheckPalm(true, -.005f);

    [UnityTest]
    public IEnumerator ARightPalmLoweringBeforePhysicsDoesNotPullTheSupportedPiece() => CheckPalm(false, -.005f);

    [UnityTest]
    public IEnumerator PalmSupportCorrectionStillKeepsTheFingerOutsideAButtonFace() => CheckPalm(true, -.01f, true);

    [UnityTest]
    public IEnumerator ARealPieceRestsOnTheLeftPalmNearTheThumb() => CheckPalm(true, 0f, false, .018f);

    [UnityTest]
    public IEnumerator ARealPieceRestsOnTheRightPalmNearTheThumb() => CheckPalm(false, 0f, false, .018f);

    [UnityTest]
    public IEnumerator RotatingTheLeftPalmBeforePhysicsDoesNotEngulfThePiece() => CheckPalm(true, 0f, false, .018f, 10f);

    [UnityTest]
    public IEnumerator RotatingTheRightPalmBeforePhysicsDoesNotEngulfThePiece() => CheckPalm(false, 0f, false, .018f, 10f);

    [UnityTest]
    public IEnumerator RotatingSupportKeepsTheVisibleFingerOutsideAButton() => CheckPalm(true, 0f, true, .018f, 10f);

    private IEnumerator CheckPalm(bool left, float beforeRenderOffset = 0f, bool checkButton = false,
        float contactShift = 0f, float beforeRenderAngle = 0f)
    {
        var pose = new GameObject("Raw controller pose").transform;
        pose.SetParent(root.transform, false);
        GameObject model = Object.Instantiate(Resources.Load<GameObject>(left ? "XR/LeftHandVisual" : "XR/RightHandVisual"), root.transform, false);
        XRHandSkeletonDriver skeleton = model.GetComponentInChildren<XRHandSkeletonDriver>();
        var descriptors = new List<XRHandSubsystemDescriptor>();
        SubsystemManager.GetSubsystemDescriptors(descriptors);
        const string id = "ChessCgi.PalmSurfaceReplay";
        XRHandSubsystemDescriptor descriptor = descriptors.Find(candidate => candidate.id == id);
        if (descriptor == null)
        {
            XRHandSubsystemDescriptor.Register(new XRHandSubsystemDescriptor.Cinfo
            {
                id = id, providerType = typeof(PalmReplayProvider), subsystemTypeOverride = typeof(PalmReplaySubsystem),
            });
            descriptors.Clear();
            SubsystemManager.GetSubsystemDescriptors(descriptors);
            descriptor = descriptors.Find(candidate => candidate.id == id);
        }
        subsystem = descriptor.Create();
        PalmReplayProvider replay = ((PalmReplaySubsystem)subsystem).Replay;
        replay.Left = left;
        foreach (JointToTransformReference joint in skeleton.jointTransformReferences)
            replay.Joints[joint.xrHandJointID.ToIndex()] = new Pose(root.transform.InverseTransformPoint(joint.jointTransform.position),
                Quaternion.Inverse(root.transform.rotation) * joint.jointTransform.rotation);
        Pose initialWrist = replay.Joints[XRHandJointID.Wrist.ToIndex()];
        Vector3 initialMiddle = replay.Joints[XRHandJointID.MiddleProximal.ToIndex()].position;
        Vector3 across = replay.Joints[XRHandJointID.LittleProximal.ToIndex()].position - replay.Joints[XRHandJointID.IndexProximal.ToIndex()].position;
        Vector3 normal = Vector3.Cross(initialMiddle - initialWrist.position, across).normalized * (left ? -1f : 1f);
        Quaternion turn = Quaternion.FromToRotation(normal, Vector3.up);
        for (int i = 0; i < replay.Joints.Length; i++)
            replay.Joints[i] = new Pose(turn * (replay.Joints[i].position - initialWrist.position) + Vector3.back * .04f,
                turn * replay.Joints[i].rotation);
        subsystem.Start();
        XRPhysicalHand hand = model.AddComponent<XRPhysicalHand>();
        hand.ConfigureTrackedHand(skeleton, pose.gameObject, root.transform, root.transform, root.transform);
        yield return null;
        Assert.That(skeleton.handTrackingEvents.subsystem, Is.SameAs(subsystem));
        UpdateReplay();
        Assert.That(hand.TryGetRawJointPose(XRHandJointID.Wrist, out Pose wrist), Is.True);
        hand.TryGetRawJointPose(XRHandJointID.MiddleProximal, out Pose middle);
        yield return Steps(8);
        Assert.That(root.GetComponentInChildren<XRHandContactBody>().IsContactEnabled, Is.True, "The replayed physical palm must really be armed.");
        foreach (JointToTransformReference joint in skeleton.jointTransformReferences)
            if (joint.xrHandJointID == XRHandJointID.Wrist || joint.xrHandJointID == XRHandJointID.MiddleProximal ||
                joint.xrHandJointID == XRHandJointID.IndexProximal || joint.xrHandJointID == XRHandJointID.LittleProximal)
            {
                Assert.That(hand.TryGetRawJointPose(joint.xrHandJointID, out Pose raw), Is.True);
                Assert.That(Vector3.Distance(joint.jointTransform.position, raw.position + hand.VisualOffset), Is.LessThan(.0005f),
                    "Rendered joints must match the provider, not an unrelated editor bind pose.");
            }
        hand.TryGetRawJointPose(XRHandJointID.Wrist, out wrist);
        hand.TryGetRawJointPose(XRHandJointID.MiddleProximal, out middle);
        Vector3 center = (wrist.position + middle.position) * .5f;
        hand.TryGetRawJointPose(XRHandJointID.ThumbMetacarpal, out Pose thumb);
        Vector3 side = Vector3.ProjectOnPlane(thumb.position - center, Vector3.up).normalized;
        Vector3 sampleCenter = center + side * contactShift;
        CaptureSurface(center, left ? "left" : "right", Vector3.up);
        CaptureSurface(center, left ? "left-reverse" : "right-reverse", Vector3.down);

        float skinTop = float.NegativeInfinity;
        int samples = 0;
        foreach (SkinnedMeshRenderer renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            var mesh = new Mesh();
            renderer.BakeMesh(mesh);
            foreach (Vector3 vertex in mesh.vertices)
            {
                Vector3 world = renderer.transform.TransformPoint(vertex);
                if (new Vector2(world.x - sampleCenter.x, world.z - sampleCenter.z).sqrMagnitude > .015f * .015f) continue;
                samples++;
                skinTop = Mathf.Max(skinTop, world.y);
            }
            Object.Destroy(mesh);
        }
        Assert.That(samples, Is.GreaterThan(5), "Measure the actual rendered skin around the palm, not only the joint plane.");
        var pieces = new GameObject("Real VR piece scale").transform;
        pieces.SetParent(root.transform, false);
        pieces.localScale = Vector3.one * .05625f;
        PieceFactory factory = root.AddComponent<PieceFactory>();
        factory.UsePrimitivePieces = true;
        if (beforeRenderOffset != 0f || contactShift != 0f)
        {
            GameObject prefab = Resources.Load<GameObject>("CustomPieces/Pawn_Mathwidu_Redhead_v2");
            Assert.That(prefab, Is.Not.Null, "Use the pawn configured in Main.");
            factory.ConfigureCustomPrefab(ChessPieceKind.Pawn, prefab);
            factory.UsePrimitivePieces = false;
        }
        PieceView piece = factory.CreatePiece(new VisualPieceState(BoardSquare.FromAlgebraic("e2"), ChessSide.White, ChessPieceKind.Pawn),
            sampleCenter + Vector3.up * .07f, pieces);
        Rigidbody body = piece.GetComponent<Rigidbody>();
        Assert.That(body, Is.Not.Null, "Use the real factory's VR piece and rolling collider.");
        body.isKinematic = false;
        body.useGravity = true;
        body.WakeUp();
        yield return Steps(45);
        float sole = piece.GetComponent<Collider>().bounds.min.y;
        Assert.That(sole, Is.GreaterThanOrEqualTo(skinTop - .0015f),
            $"The piece's base ({sole:F4}) must not sink into the visible palm ({skinTop:F4}); samples={samples}; center={center:F4}; piece={piece.transform.position:F4}; tilt={Vector3.Angle(piece.transform.up, Vector3.up):F1}; palm={root.GetComponentInChildren<XRHandContactBody>().GetComponent<BoxCollider>().bounds}.");
        Assert.That(sole - skinTop, Is.LessThan(.008f), "The piece must rest on the skin rather than float above it.");
        Assert.That(body.linearVelocity.magnitude, Is.LessThan(.1f));
        if (contactShift != 0f)
        {
            float baseline = VisiblePieceBottom(piece) - VisiblePalmTop(model, sampleCenter);
            float actualGap = VisibleMeshBottom(piece) - VisiblePalmTop(model, sampleCenter);
            TestContext.WriteLine($"Off-center gap bounds={baseline:F5}, actual mesh={actualGap:F5}; side={side:F3}; tilt={Vector3.Angle(piece.transform.up, Vector3.up):F2}");
            Assert.That(baseline, Is.GreaterThanOrEqualTo(-.0015f), "The static thumb-side support must work before testing temporal rotation.");
            if (beforeRenderAngle == 0f) Assert.That(actualGap, Is.GreaterThanOrEqualTo(-.00005f),
                "At rest the actual visible base must remain above the skin, including the small static penetration previously tolerated.");
        }
        PhysicalTableButton button = null;
        if (checkButton)
        {
            hand.TryGetRawJointPose(XRHandJointID.IndexTip, out Pose tip);
            var mount = new GameObject("Button face beside supported piece");
            mount.transform.SetParent(root.transform, false);
            mount.transform.rotation = Quaternion.LookRotation(Vector3.up, Vector3.forward);
            mount.transform.position = tip.position + Vector3.up * (.003f + PhysicalTableButton.CapDepth * .5f);
            button = mount.AddComponent<PhysicalTableButton>();
            button.Configure(.04f, Color.blue, false, false, () => { });
            button.Body.constraints = RigidbodyConstraints.FreezeAll;
            yield return Steps(8);
            Assert.That(hand.Contacts.Any(contact => contact.ContactButton == button), Is.True,
                "The actual hand must establish the button-face contact while supporting the piece.");
        }
        if (beforeRenderOffset != 0f) yield return CheckRenderPhase(model, hand, piece, beforeRenderOffset, button);
        if (beforeRenderAngle != 0f) CheckRotationPhase(model, hand, piece, center, sampleCenter, beforeRenderAngle, button);
    }

    private void CheckRotationPhase(GameObject model, XRPhysicalHand hand, PieceView piece, Vector3 center,
        Vector3 sampleCenter, float angle, PhysicalTableButton button)
    {
        PalmReplayProvider replay = ((PalmReplaySubsystem)subsystem).Replay;
        XRHandContactBody contacts = root.GetComponentInChildren<XRHandContactBody>();
        Rigidbody palm = contacts.GetComponent<Rigidbody>();
        Rigidbody body = piece.GetComponent<Rigidbody>();
        Assert.That(piece.GetComponent<ThrowablePiece>().IsSupportedByHand, Is.True);
        hand.TryGetRawJointPose(XRHandJointID.Wrist, out Pose originalWrist);
        Pose originalSource = replay.Joints[XRHandJointID.Wrist.ToIndex()];
        Vector3 palmPosition = palm.position, piecePosition = body.position;
        Quaternion palmRotation = palm.rotation, pieceRotation = body.rotation;
        float baseline = VisiblePieceBottom(piece) - VisiblePalmTop(model, sampleCenter);
        replay.RotationPivot = root.transform.InverseTransformPoint(center);
        Vector3 axis = Vector3.Cross((sampleCenter - center).normalized, Vector3.up);
        replay.BeforeRenderRotation = Quaternion.AngleAxis(angle, root.transform.InverseTransformDirection(axis));
        // Rotate only the provider's render sample, around a stationary palm
        // center. No simulation or body transform changes between these samples.
        subsystem.TryUpdateHands(XRHandSubsystem.UpdateType.BeforeRender);
        hand.TryGetRawJointPose(XRHandJointID.Wrist, out Pose lateWrist);
        hand.TryGetRawJointPose(XRHandJointID.MiddleProximal, out Pose lateMiddle);
        Vector3 expectedWrist = root.transform.TransformPoint(replay.RotationPivot +
            replay.BeforeRenderRotation * (originalSource.position - replay.RotationPivot));
        float lateGap = VisiblePieceBottom(piece) - VisiblePalmTop(model, sampleCenter);
        Transform drawnWrist = model.GetComponentInChildren<XRHandSkeletonDriver>().jointTransformReferences
            .First(joint => joint.xrHandJointID == XRHandJointID.Wrist).jointTransform;
        Vector3 firstDrawn = drawnWrist.position;
        Quaternion firstDrawnRotation = drawnWrist.rotation;
        subsystem.TryUpdateHands(XRHandSubsystem.UpdateType.BeforeRender);
        float repeatedPositionError = Vector3.Distance(drawnWrist.position, firstDrawn);
        float repeatedRotationError = Quaternion.Angle(drawnWrist.rotation, firstDrawnRotation);
        float fingerDepth = 0f;
        if (button != null)
        {
            Transform tip = model.GetComponentInChildren<XRHandSkeletonDriver>().jointTransformReferences
                .First(joint => joint.xrHandJointID == XRHandJointID.IndexTip).jointTransform;
            fingerDepth = Vector3.Dot(tip.position - button.FaceCenter, button.PressDirection) + .008f;
        }
        replay.BeforeRenderRotation = Quaternion.identity;
        subsystem.TryUpdateHands(XRHandSubsystem.UpdateType.BeforeRender);
        float restoredGap = VisiblePieceBottom(piece) - VisiblePalmTop(model, sampleCenter);
        TestContext.WriteLine($"Render-only rotation={angle:F1}; baseline={baseline:F5}, late={lateGap:F5}, restored={restoredGap:F5}; buttonDepth={fingerDepth:F5}");
        Assert.That(Vector3.Distance(lateWrist.position, expectedWrist), Is.LessThan(.0001f), "Latest tracking must remain raw.");
        Assert.That(Vector3.Distance((lateWrist.position + lateMiddle.position) * .5f, center), Is.LessThan(.0001f));
        Assert.That(Vector3.Distance(palm.position, palmPosition), Is.LessThan(.0001f));
        Assert.That(Vector3.Distance(body.position, piecePosition), Is.LessThan(.0001f));
        Assert.That(Quaternion.Angle(palm.rotation, palmRotation), Is.LessThan(.01f));
        Assert.That(Quaternion.Angle(body.rotation, pieceRotation), Is.LessThan(.01f));
        Assert.That(replay.Joints[XRHandJointID.Wrist.ToIndex()], Is.EqualTo(originalSource));
        Assert.That(repeatedPositionError, Is.LessThan(.0001f), "Repeated render callbacks cannot feed the correction back.");
        Assert.That(repeatedRotationError, Is.LessThan(.01f));
        Assert.That(restoredGap, Is.EqualTo(baseline).Within(.0005f));
        Assert.That(lateGap, Is.GreaterThanOrEqualTo(baseline - .0015f), "A rotating render sample must not engulf the physically supported piece.");
        if (button != null) Assert.That(fingerDepth, Is.LessThanOrEqualTo(.0005f), "The actual drawn fingertip must respect the button after rotation.");
    }

    private IEnumerator CheckRenderPhase(GameObject model, XRPhysicalHand hand, PieceView piece, float offsetY, PhysicalTableButton button)
    {
        PalmReplayProvider replay = ((PalmReplaySubsystem)subsystem).Replay;
        XRHandContactBody contacts = root.GetComponentInChildren<XRHandContactBody>();
        BoxCollider palm = contacts.GetComponent<BoxCollider>();
        Rigidbody palmBody = contacts.GetComponent<Rigidbody>();
        Rigidbody body = piece.GetComponent<Rigidbody>();
        MeshCollider shape = piece.GetComponent<MeshCollider>();
        ThrowablePiece throwable = piece.GetComponent<ThrowablePiece>();
        Assert.That(throwable.IsSupportedByHand, Is.True, "Establish actual support before testing visual continuity.");
        hand.TryGetRawJointPose(XRHandJointID.Wrist, out Pose wrist);
        hand.TryGetRawJointPose(XRHandJointID.MiddleProximal, out Pose middle);
        Vector3 center = (wrist.position + middle.position) * .5f;
        float initialGap = VisiblePieceBottom(piece) - VisiblePalmTop(model, center);
        float initialPhysicalGap = PhysicalPieceBottom(shape, body) - PhysicalPalmTop(palm, palmBody);
        Vector3 palmPosition = palmBody.position, piecePosition = body.position;
        Quaternion palmRotation = palmBody.rotation, pieceRotation = body.rotation;
        Pose suppliedWrist = replay.Joints[XRHandJointID.Wrist.ToIndex()];
        replay.BeforeRenderOffset = Vector3.up * offsetY;
        // No yield: only the render pose advances, never a physical timestep.
        subsystem.TryUpdateHands(XRHandSubsystem.UpdateType.BeforeRender);
        Assert.That(hand.TryGetRawJointPose(XRHandJointID.Wrist, out Pose lateWrist), Is.True);
        Vector3 expected = root.transform.TransformPoint(suppliedWrist.position + replay.BeforeRenderOffset);
        float rawError = Vector3.Distance(lateWrist.position, expected);
        float providerMutation = Vector3.Distance(replay.Joints[XRHandJointID.Wrist.ToIndex()].position, suppliedWrist.position);
        float lateGap = VisiblePieceBottom(piece) - VisiblePalmTop(model, center);
        float latePhysicalGap = PhysicalPieceBottom(shape, body) - PhysicalPalmTop(palm, palmBody);
        float fingerDepth = 0f;
        if (button != null)
        {
            hand.TryGetRawJointPose(XRHandJointID.IndexTip, out Pose tip);
            fingerDepth = Vector3.Dot(tip.position + hand.VisualOffset - button.FaceCenter, button.PressDirection) + .008f;
        }
        float palmMotion = Vector3.Distance(palmBody.position, palmPosition), pieceMotion = Vector3.Distance(body.position, piecePosition);
        float palmTurn = Quaternion.Angle(palmBody.rotation, palmRotation), pieceTurn = Quaternion.Angle(body.rotation, pieceRotation);
        bool validSupport = contacts.IsContactEnabled && throwable.IsSupportedByHand;
        replay.DynamicOffset = replay.BeforeRenderOffset;
        yield return Steps(45);
        float settledGap = VisiblePieceBottom(piece) - VisiblePalmTop(model, center);
        hand.TryGetRawJointPose(XRHandJointID.Wrist, out Pose settledWrist);
        TestContext.WriteLine($"offset={offsetY:F4}; visual initial={initialGap:F5}, late={lateGap:F5}, settled={settledGap:F5}; physical initial={initialPhysicalGap:F5}, late={latePhysicalGap:F5}; physics motion palm={palmMotion:F6}, piece={pieceMotion:F6}");
            Assert.That(validSupport, Is.True);
            Assert.That(rawError, Is.LessThan(.0001f), "Visual correction cannot modify raw tracking.");
            Assert.That(Vector3.Distance(settledWrist.position, expected), Is.LessThan(.0001f));
            Assert.That(providerMutation, Is.LessThan(.0001f));
            Assert.That(palmMotion, Is.LessThan(.0001f), "BeforeRender cannot move the physical palm.");
            Assert.That(pieceMotion, Is.LessThan(.0001f), "BeforeRender cannot move the physical piece.");
            Assert.That(palmTurn, Is.LessThan(.01f));
            Assert.That(pieceTurn, Is.LessThan(.01f));
            Assert.That(latePhysicalGap, Is.EqualTo(initialPhysicalGap).Within(.0001f));
            if (button != null) Assert.That(fingerDepth, Is.LessThanOrEqualTo(.0005f),
                "The support offset cannot undo the fingertip's visible stop at a button face.");
            Assert.That(lateGap, Is.GreaterThanOrEqualTo(-.0015f), "The newer visible palm cannot engulf a physically supported piece.");
            Assert.That(settledGap, Is.GreaterThanOrEqualTo(-.0015f));
            if (button == null) Assert.That(settledGap, Is.EqualTo(initialGap).Within(.002f), "The stopped pose must converge without a retained offset.");
    }

    private static float VisiblePalmTop(GameObject model, Vector3 center)
    {
        float top = float.NegativeInfinity;
        int samples = 0;
        foreach (SkinnedMeshRenderer renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            var mesh = new Mesh();
            renderer.BakeMesh(mesh);
            foreach (Vector3 vertex in mesh.vertices)
            {
                Vector3 world = renderer.transform.TransformPoint(vertex);
                if (new Vector2(world.x - center.x, world.z - center.z).sqrMagnitude > .015f * .015f) continue;
                samples++;
                top = Mathf.Max(top, world.y);
            }
            Object.DestroyImmediate(mesh);
        }
        Assert.That(samples, Is.GreaterThan(5));
        return top;
    }

    private static float VisiblePieceBottom(PieceView piece) => piece.GetComponentsInChildren<Renderer>().Min(renderer => renderer.bounds.min.y);

    private static float VisibleMeshBottom(PieceView piece)
    {
        float bottom = float.PositiveInfinity;
        foreach (MeshFilter filter in piece.GetComponentsInChildren<MeshFilter>())
            if (filter.sharedMesh != null)
                foreach (Vector3 vertex in filter.sharedMesh.vertices)
                    bottom = Mathf.Min(bottom, filter.transform.TransformPoint(vertex).y);
        foreach (SkinnedMeshRenderer skin in piece.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            var mesh = new Mesh();
            skin.BakeMesh(mesh);
            foreach (Vector3 vertex in mesh.vertices) bottom = Mathf.Min(bottom, skin.transform.TransformPoint(vertex).y);
            Object.DestroyImmediate(mesh);
        }
        return bottom;
    }

    private static float PhysicalPalmTop(BoxCollider shape, Rigidbody body)
    {
        float top = float.NegativeInfinity;
        for (int i = 0; i < 8; i++)
        {
            Vector3 sign = new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1);
            Vector3 point = shape.center + Vector3.Scale(shape.size * .5f, sign);
            top = Mathf.Max(top, (body.position + body.rotation * Vector3.Scale(point, shape.transform.lossyScale)).y);
        }
        return top;
    }

    private static float PhysicalPieceBottom(MeshCollider shape, Rigidbody body) => shape.sharedMesh.vertices.Min(vertex =>
        (body.position + body.rotation * Vector3.Scale(vertex, shape.transform.lossyScale)).y);


    private IEnumerator Steps(int count)
    {
        Physics.SyncTransforms();
        for (int i = 0; i < count; i++)
        {
            UpdateReplay();
            yield return new WaitForFixedUpdate();
            yield return null;
        }
    }

    private void UpdateReplay()
    {
        subsystem.TryUpdateHands(XRHandSubsystem.UpdateType.Dynamic);
        subsystem.TryUpdateHands(XRHandSubsystem.UpdateType.BeforeRender);
    }

    private void CaptureSurface(Vector3 center, string label, Vector3 side)
    {
        var lightObject = new GameObject("Surface inspection light");
        lightObject.transform.SetParent(root.transform, false);
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.5f;
        lightObject.transform.rotation = Quaternion.LookRotation(-side + Vector3.forward * .4f);
        var cameraObject = new GameObject("Surface inspection camera");
        cameraObject.transform.SetParent(root.transform, false);
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.transform.position = center + side * .3f;
        camera.transform.rotation = Quaternion.LookRotation(-side, Vector3.forward);
        camera.orthographic = true;
        camera.orthographicSize = .14f;
        camera.nearClipPlane = .01f;
        camera.farClipPlane = .6f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.15f, .16f, .18f);
        camera.allowHDR = false;
        var target = new RenderTexture(512, 512, 24);
        camera.targetTexture = target;
        camera.Render();
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = target;
        var pixels = new Texture2D(512, 512, TextureFormat.RGB24, false);
        pixels.ReadPixels(new Rect(0, 0, 512, 512), 0, 0);
        pixels.Apply();
        string directory = Path.GetFullPath(Path.Combine(Application.dataPath, "../../TestResults/Quest3Hands"));
        Directory.CreateDirectory(directory);
        File.WriteAllBytes(Path.Combine(directory, "palm-surface-" + label + ".png"), pixels.EncodeToPNG());
        RenderTexture.active = previous;
        camera.targetTexture = null;
        Object.DestroyImmediate(pixels);
        Object.DestroyImmediate(target);
        Object.DestroyImmediate(cameraObject);
        Object.DestroyImmediate(lightObject);
    }

    public sealed class PalmReplaySubsystem : XRHandSubsystem
    {
        public PalmReplayProvider Replay => (PalmReplayProvider)provider;
    }

    public sealed class PalmReplayProvider : XRHandSubsystemProvider
    {
        public readonly Pose[] Joints = new Pose[XRHandJointID.EndMarker.ToIndex()];
        public Vector3 DynamicOffset;
        public Vector3 BeforeRenderOffset;
        public Vector3 RotationPivot;
        public Quaternion BeforeRenderRotation = Quaternion.identity;
        public bool Left;
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
            NativeArray<XRHandJoint> joints = Left ? leftJoints : rightJoints;
            Vector3 offset = updateType == XRHandSubsystem.UpdateType.Dynamic ? DynamicOffset : BeforeRenderOffset;
            Quaternion rotation = updateType == XRHandSubsystem.UpdateType.BeforeRender ? BeforeRenderRotation : Quaternion.identity;
            Pose wrist = Joints[XRHandJointID.Wrist.ToIndex()];
            wrist.position = RotationPivot + rotation * (wrist.position - RotationPivot) + offset;
            wrist.rotation = rotation * wrist.rotation;
            if (Left) leftRoot = wrist;
            else rightRoot = wrist;
            for (int i = 0; i < joints.Length; i++)
            {
                Pose pose = Joints[i];
                pose.position = RotationPivot + rotation * (pose.position - RotationPivot) + offset;
                pose.rotation = rotation * pose.rotation;
                joints[i] = XRHandProviderUtility.CreateJoint(Left ? Handedness.Left : Handedness.Right,
                    XRHandJointTrackingState.Pose, XRHandJointIDUtility.FromIndex(i), pose);
            }
            return Left ? XRHandSubsystem.UpdateSuccessFlags.LeftHandRootPose | XRHandSubsystem.UpdateSuccessFlags.LeftHandJoints :
                XRHandSubsystem.UpdateSuccessFlags.RightHandRootPose | XRHandSubsystem.UpdateSuccessFlags.RightHandJoints;
        }
    }
}
#endif
