using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public sealed class HandPiecePhysicsTests
{
    private GameObject root;
    private XRPhysicalHand hand;
    private readonly Dictionary<XRHandJointID, Vector3> joints = new Dictionary<XRHandJointID, Vector3>();
    private Vector3 handOffset;
    private bool tracked;

    [SetUp]
    public void SetUp()
    {
        root = new GameObject("Open palm physics replay");
        root.transform.position = new Vector3(48f, 8f, 0f);
        var model = new GameObject("Tracked hand visual");
        model.transform.SetParent(root.transform, false);
        joints.Clear();
        joints[XRHandJointID.Wrist] = new Vector3(0f, 0f, -.04f);
        joints[XRHandJointID.Palm] = Vector3.zero;
        AddFinger(XRHandJointID.IndexProximal, XRHandJointID.IndexIntermediate,
            XRHandJointID.IndexDistal, XRHandJointID.IndexTip, -.026f, .105f);
        AddFinger(XRHandJointID.MiddleProximal, XRHandJointID.MiddleIntermediate,
            XRHandJointID.MiddleDistal, XRHandJointID.MiddleTip, 0f, .115f);
        AddFinger(XRHandJointID.RingProximal, XRHandJointID.RingIntermediate,
            XRHandJointID.RingDistal, XRHandJointID.RingTip, .021f, .105f);
        AddFinger(XRHandJointID.LittleProximal, XRHandJointID.LittleIntermediate,
            XRHandJointID.LittleDistal, XRHandJointID.LittleTip, .039f, .09f);
        joints[XRHandJointID.ThumbMetacarpal] = new Vector3(-.025f, 0f, -.015f);
        joints[XRHandJointID.ThumbProximal] = new Vector3(-.04f, 0f, 0f);
        joints[XRHandJointID.ThumbDistal] = new Vector3(-.048f, 0f, .02f);
        joints[XRHandJointID.ThumbTip] = new Vector3(-.055f, 0f, .04f);
        handOffset = Vector3.zero;
        tracked = true;
        hand = model.AddComponent<XRPhysicalHand>();
        hand.Configure(model.transform, root.transform, root.transform, ReadJoint);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(root);
    }

    [UnityTest]
    public IEnumerator LoosePieceCanRestOnTheOpenPalmUnderGravity()
    {
        yield return FixedSteps(8);
        Rigidbody piece = DropPiece();
        yield return FixedSteps(40);
        AssertRestingOnPalm(piece);
        Assert.That(piece.linearVelocity.magnitude, Is.LessThan(.15f), "A resting piece must not keep bouncing or falling");
    }

    [UnityTest]
    public IEnumerator OpenPalmCarriesARestingPieceDuringGentleMovement()
    {
        yield return FixedSteps(8);
        Rigidbody piece = DropPiece();
        yield return FixedSteps(35);
        AssertRestingOnPalm(piece);
        for (int i = 0; i < 24; i++)
        {
            handOffset += Vector3.right * .001f;
            yield return new WaitForFixedUpdate();
        }
        yield return FixedSteps(8);
        AssertRestingOnPalm(piece);
        Assert.That(piece.position.x - root.transform.position.x, Is.GreaterThan(.012f),
            "The palm must carry the piece through contact and friction, without parenting it");
        Assert.That(piece.transform.parent, Is.SameAs(root.transform));
    }

    [UnityTest]
    public IEnumerator LostTrackingRemovesSupportAndReacquisitionDoesNotLaunchThePiece()
    {
        yield return FixedSteps(8);
        Rigidbody piece = DropPiece();
        yield return FixedSteps(35);
        AssertRestingOnPalm(piece);
        tracked = false;
        yield return FixedSteps(15);
        Assert.That(piece.position.y, Is.LessThan(root.transform.position.y - .05f),
            "A missing hand must not leave an invisible platform behind");
        tracked = true;
        handOffset = Vector3.up * .25f;
        yield return FixedSteps(8);
        Assert.That(piece.linearVelocity.y, Is.LessThan(0f), "Tracking recovery must not impart an upward impulse");
        Assert.That(piece.linearVelocity.magnitude, Is.LessThan(6f));
    }

    [UnityTest]
    public IEnumerator DisablingTheHandWakesAPieceThatWasSleepingOnIt()
    {
        yield return FixedSteps(8);
        Rigidbody piece = DropPiece();
        yield return FixedSteps(40);
        AssertRestingOnPalm(piece);
        piece.Sleep();
        hand.enabled = false;
        yield return FixedSteps(15);
        Assert.That(piece.position.y, Is.LessThan(root.transform.position.y - .05f));
    }

    [UnityTest]
    public IEnumerator ReacquiringInsideALoosePieceWaitsForClearanceInsteadOfLaunchingIt()
    {
        yield return FixedSteps(8);
        Rigidbody piece = DropPiece();
        yield return FixedSteps(40);
        AssertRestingOnPalm(piece);
        tracked = false;
        yield return FixedSteps(15);
        // Replay a recovered tracking pose inside a loose object, where enabling
        // a collider immediately would create an artificial separation impulse.
        piece.position = root.transform.position;
        piece.linearVelocity = Vector3.zero;
        piece.useGravity = false;
        tracked = true;
        yield return FixedSteps(8);
        Assert.That(root.GetComponentInChildren<XRHandContactBody>().IsContactEnabled, Is.False);
        Assert.That(piece.linearVelocity.magnitude, Is.LessThan(.01f));
        piece.position += Vector3.right * .3f;
        yield return FixedSteps(8);
        Assert.That(root.GetComponentInChildren<XRHandContactBody>().IsContactEnabled, Is.True,
            "Clear tracking recovery must not leave the hand permanently disabled");
    }

    [UnityTest]
    public IEnumerator RecoveringAFingerJointInsideAPieceWaitsForClearance()
    {
        yield return FixedSteps(8);
        Vector3 intermediate = joints[XRHandJointID.IndexIntermediate];
        joints.Remove(XRHandJointID.IndexIntermediate);
        yield return FixedSteps(4);
        XRHandContactBody contacts = root.GetComponentInChildren<XRHandContactBody>();
        CapsuleCollider finger = contacts.transform.Find("IndexIntermediate piece contact").GetComponent<CapsuleCollider>();
        Assert.That(contacts.IsContactEnabled, Is.True, "The rest of the tracked hand remains available");
        Assert.That(finger.enabled, Is.False);
        Rigidbody piece = DropPiece();
        piece.transform.localScale = Vector3.one * .008f;
        piece.useGravity = false;
        piece.position = root.transform.position + new Vector3(-.026f, 0f, .075f);
        yield return FixedSteps(3);
        Assert.That(piece.linearVelocity.magnitude, Is.LessThan(.01f), "The setup must clear all still tracked contacts");
        joints[XRHandJointID.IndexIntermediate] = intermediate;
        yield return FixedSteps(5);
        Assert.That(finger.enabled, Is.False, "One returning joint must not activate a capsule through the loose piece");
        Assert.That(piece.linearVelocity.magnitude, Is.LessThan(.02f));
        piece.position += Vector3.up * .1f;
        yield return FixedSteps(8);
        Assert.That(finger.enabled, Is.True, "The finger must become physical again once clear");
    }

    [UnityTest]
    public IEnumerator SupportedThrowableWaitsForTheHandToLeaveBeforeReturningHome()
    {
        var manager = root.AddComponent<XRInteractionManager>();
        var grabberObject = new GameObject("Pinch for recovery replay");
        grabberObject.transform.SetParent(root.transform, false);
        grabberObject.AddComponent<SphereCollider>().isTrigger = true;
        var grabber = grabberObject.AddComponent<XRDirectInteractor>();
        grabber.interactionManager = manager;
        yield return FixedSteps(8);
        Rigidbody piece = DropPiece();
        piece.isKinematic = true;
        var throwable = piece.gameObject.AddComponent<ThrowablePiece>();
        throwable.interactionManager = manager;
        throwable.movementType = XRBaseInteractable.MovementType.Instantaneous;
        throwable.SetRest(Vector3.forward * 2f);
        yield return null;
        manager.SelectEnter((IXRSelectInteractor)grabber, throwable);
        yield return null;
        yield return new WaitForFixedUpdate();
        manager.SelectExit((IXRSelectInteractor)grabber, throwable);
        yield return null;
        yield return new WaitForFixedUpdate();
        piece.position = root.transform.position + Vector3.up * .06f;
        piece.linearVelocity = Vector3.zero;
        piece.angularVelocity = Vector3.zero;
        Assert.That(piece.isKinematic, Is.False);
        yield return new WaitForSeconds(4f);
        AssertRestingOnPalm(piece);
        Assert.That(piece.isKinematic, Is.False, "A supported piece must not freeze and teleport home after three seconds");

        var probe = piece.gameObject.AddComponent<ContactProbe>();
        // Add a lateral thumb contact while the palm still supplies the support.
        Vector3 side = piece.position - root.transform.position + Vector3.right * .015f;
        joints[XRHandJointID.ThumbMetacarpal] = side + Vector3.right * .04f + Vector3.up * .015f;
        joints[XRHandJointID.ThumbProximal] = side + Vector3.right * .04f + Vector3.up * .005f;
        joints[XRHandJointID.ThumbDistal] = side + Vector3.right * .04f - Vector3.up * .005f;
        joints[XRHandJointID.ThumbTip] = side + Vector3.right * .04f - Vector3.up * .015f;
        XRHandJointID[] thumb = { XRHandJointID.ThumbMetacarpal, XRHandJointID.ThumbProximal,
            XRHandJointID.ThumbDistal, XRHandJointID.ThumbTip };
        yield return FixedSteps(8);
        for (int i = 0; i < 20; i++)
        {
            foreach (XRHandJointID id in thumb) joints[id] -= Vector3.right * .002f;
            yield return new WaitForFixedUpdate();
        }
        yield return FixedSteps(5);
        Assert.That(probe.SawPalmAndFinger, Is.True, "The replay must actually create simultaneous compound contacts");
        foreach (XRHandJointID id in thumb) joints[id] += Vector3.right * .08f;
        yield return new WaitForSeconds(3.5f);
        Assert.That(probe.FingerExited, Is.True, "The finger must really leave while the palm remains");
        AssertRestingOnPalm(piece);
        Assert.That(piece.isKinematic, Is.False, "A lateral finger leaving cannot cancel the palm's support");
        tracked = false;
        yield return FixedSteps(15);
        Assert.That(piece.position.y, Is.LessThan(root.transform.position.y - .05f));
        // The old contact cannot survive a disabled/rearmed hand elsewhere.
        tracked = true;
        handOffset = Vector3.right * .4f;
        yield return new WaitForSeconds(3.5f);
        Assert.That(throwable.IsPhysicallyLoose, Is.False, "Recovery restores a resting piece which can respond to the next touch.");
        Assert.That(piece.isKinematic, Is.False);
        Assert.That(piece.IsSleeping(), Is.True);
        Assert.That(Vector3.Distance(piece.transform.localPosition, Vector3.forward * 2f), Is.LessThan(.001f),
            "Once no hand supports it, the existing recovery must still restore the piece");
    }

    public sealed class ContactProbe : MonoBehaviour
    {
        private readonly HashSet<Collider> contacts = new HashSet<Collider>();
        public bool SawPalmAndFinger { get; private set; }
        public bool FingerExited { get; private set; }
        private void OnCollisionEnter(Collision collision) => Record(collision);
        private void OnCollisionStay(Collision collision) => Record(collision);
        private void Record(Collision collision)
        {
            if (collision.rigidbody == null || !collision.rigidbody.TryGetComponent<XRHandContactBody>(out _)) return;
            contacts.Add(collision.collider);
            bool palm = false, finger = false;
            foreach (Collider shape in contacts)
            {
                palm |= shape is BoxCollider;
                finger |= shape is CapsuleCollider;
            }
            SawPalmAndFinger |= palm && finger;
        }
        private void OnCollisionExit(Collision collision)
        {
            FingerExited |= collision.collider is CapsuleCollider;
            contacts.Remove(collision.collider);
        }
    }

    private void AddFinger(XRHandJointID proximal, XRHandJointID intermediate,
        XRHandJointID distal, XRHandJointID tip, float x, float length)
    {
        joints[proximal] = new Vector3(x, 0f, .04f);
        joints[intermediate] = new Vector3(x, 0f, Mathf.Lerp(.04f, length, .4f));
        joints[distal] = new Vector3(x, 0f, Mathf.Lerp(.04f, length, .75f));
        joints[tip] = new Vector3(x, 0f, length);
    }

    private bool ReadJoint(XRHandJointID joint, out Pose pose)
    {
        pose = default;
        if (!tracked || !joints.TryGetValue(joint, out Vector3 local)) return false;
        pose = new Pose(root.transform.position + handOffset + local, Quaternion.identity);
        return true;
    }

    private Rigidbody DropPiece()
    {
        GameObject piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
        piece.name = "Loose piece on palm";
        piece.layer = PieceView.PhysicsLayer;
        piece.transform.SetParent(root.transform, false);
        piece.transform.localPosition = handOffset + Vector3.up * .06f;
        piece.transform.localScale = Vector3.one * .018f;
        Rigidbody body = piece.AddComponent<Rigidbody>();
        body.mass = .05f;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        return body;
    }

    private void AssertRestingOnPalm(Rigidbody piece)
    {
        Vector3 palm = root.transform.position + handOffset;
        Assert.That(piece.position.y - palm.y, Is.InRange(.004f, .04f),
            "The loose piece must remain above the open palm instead of falling through it");
        Vector2 lateral = new Vector2(piece.position.x - palm.x, piece.position.z - palm.z);
        Assert.That(lateral.magnitude, Is.LessThan(.03f));
    }

    private static IEnumerator FixedSteps(int count)
    {
        for (int i = 0; i < count; i++) yield return new WaitForFixedUpdate();
    }
}
