using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class XRPhysicsPusherTests
{
    private GameObject root;
    private Transform origin;
    private Transform source;
    private XRPhysicsPusher pusher;
    private bool tracked;
    private int presses;

    [SetUp]
    public void SetUp()
    {
        // Isolate the contact fixture from the board and any scene bootstraps.
        root = new GameObject("XR contact test");
        root.transform.position = new Vector3(40f, 8f, 0f);
        origin = new GameObject("Tracking origin").transform;
        origin.SetParent(root.transform, false);
        source = new GameObject("Tracked fingertip").transform;
        source.SetParent(origin, false);
        source.localPosition = Vector3.back * .06f;
        var contact = new GameObject("Physical contact");
        contact.transform.SetParent(root.transform, false);
        tracked = true;
        pusher = contact.AddComponent<XRPhysicsPusher>();
        pusher.Configure(source, () => tracked, origin);
    }

    [TearDown]
    public void TearDown() => Object.DestroyImmediate(root);

    [Test]
    public void StartsDisarmedAndCanOnlyContactButtonCaps()
    {
        Assert.That(pusher.IsContactEnabled, Is.False);
        Assert.That(pusher.Body.isKinematic, Is.True);
        Assert.That(pusher.TouchCollider.isTrigger, Is.False);
        Assert.That(pusher.gameObject.layer, Is.EqualTo(XRPhysicsPusher.PhysicsLayer));
        Assert.That(pusher.TouchCollider.excludeLayers.value, Is.EqualTo(~(1 << PhysicalTableButton.PhysicsLayer)));
        Assert.That(pusher.TouchCollider.contactOffset, Is.LessThan(.001f));
    }

    [UnityTest]
    public IEnumerator TrackedMotionPushesButtonLayerWithoutASelectAction()
    {
        Rigidbody cap = CreateBody("Button layer", Vector3.zero, PhysicalTableButton.PhysicsLayer);
        Rigidbody other = CreateBody("Unrelated object", Vector3.back * .034f, 0);
        Vector3 unrelatedPosition = other.position;
        yield return FixedSteps(4);
        Assert.That(pusher.IsContactEnabled, Is.True);

        // This path passes through the unrelated body before it reaches the cap.
        // The only input is a tracked Transform; no trigger, ray or UI event.
        for (int i = 0; i < 14; i++)
        {
            source.localPosition += Vector3.forward * .004f;
            yield return new WaitForFixedUpdate();
        }

        Assert.That(cap.position.z, Is.GreaterThan(root.transform.position.z + .005f),
            "The kinematic contact pushes a dynamic cap on layer 27.");
        Assert.That(Vector3.Distance(other.position, unrelatedPosition), Is.LessThan(.0001f),
            "The proxy never pushes objects outside the cap layer.");
    }

    [UnityTest]
    public IEnumerator TrackingLossAndReacquisitionInsideACapRequireLeavingIt()
    {
        CreateStaticCap();
        yield return FixedSteps(4);
        Assert.That(pusher.IsContactEnabled, Is.True);
        tracked = false;
        yield return new WaitForFixedUpdate();
        Assert.That(pusher.IsContactEnabled, Is.False);

        source.localPosition = Vector3.zero;
        tracked = true;
        yield return FixedSteps(5);
        Assert.That(pusher.IsContactEnabled, Is.False, "A returning hand cannot spawn inside a cap.");
        Assert.That(Vector3.Distance(pusher.Body.position, source.position), Is.LessThan(.0001f));

        source.localPosition = Vector3.back * .06f;
        yield return FixedSteps(5);
        Assert.That(pusher.IsContactEnabled, Is.True, "Leaving and settling restores contact.");
    }

    [UnityTest]
    public IEnumerator InactiveSourceImmediatelyLosesPhysicalContact()
    {
        yield return FixedSteps(4);
        Assert.That(pusher.IsContactEnabled, Is.True);
        source.gameObject.SetActive(false);
        yield return new WaitForFixedUpdate();
        Assert.That(pusher.IsContactEnabled, Is.False);
        Assert.That(pusher.Body.detectCollisions, Is.False);
    }

    [UnityTest]
    public IEnumerator OriginChangeDisarmsEvenWhenThePoseMovesLessThanTeleportThreshold()
    {
        CreateStaticCap();
        yield return FixedSteps(4);
        Assert.That(pusher.IsContactEnabled, Is.True);
        origin.position += Vector3.forward * .06f;
        yield return FixedSteps(5);
        Assert.That(pusher.IsContactEnabled, Is.False,
            "A recentered or changed seat cannot materialize its fingertip inside a cap.");
    }

    [UnityTest]
    public IEnumerator PoseTeleportAcrossACapDoesNotPushAlongItsPath()
    {
        Rigidbody cap = CreateBody("Button layer", Vector3.zero, PhysicalTableButton.PhysicsLayer);
        Vector3 initialPosition = cap.position;
        yield return FixedSteps(4);
        Assert.That(pusher.IsContactEnabled, Is.True);
        source.localPosition = Vector3.forward * .2f;
        yield return new WaitForFixedUpdate();
        Assert.That(pusher.IsContactEnabled, Is.False);
        yield return FixedSteps(4);
        Assert.That(Vector3.Distance(cap.position, initialPosition), Is.LessThan(.0001f),
            "The disabled body relocates directly without a swept press.");
        Assert.That(pusher.IsContactEnabled, Is.True);
    }

    [UnityTest]
    public IEnumerator FullOverpressStaysWithinCapTravelAndCanEscapeSideways()
    {
        PhysicalTableButton button = CreatePhysicalButton();
        source.position = SourceAtDepth(button, -.03f);
        yield return FixedSteps(4);
        for (int i = 1; i <= 35; i++)
        {
            source.position = SourceAtDepth(button, Mathf.Lerp(-.03f, .075f, i / 35f));
            yield return new WaitForFixedUpdate();
        }
        yield return FixedSteps(8);

        Assert.That(pusher.IsArmed, Is.True);
        Assert.That(pusher.ContactButton, Is.SameAs(button));
        float targetDepth = Vector3.Dot(pusher.TargetPose.position - button.RestFaceCenter, button.PressDirection);
        Assert.That(targetDepth + pusher.TouchCollider.radius, Is.LessThanOrEqualTo(PhysicalTableButton.Stroke + .0001f),
            "The target cannot drive the cap past its machined stop, even when raw tracking passes through it.");
        Assert.That(button.PressFraction, Is.GreaterThan(.7f));
        Assert.That(button.PressDepth, Is.InRange(0f, PhysicalTableButton.Stroke + .0001f));
        Assert.That(presses, Is.EqualTo(1), "Holding a deep raw pose must not repeatedly actuate.");
        Assert.That(pusher.TryGetVisualOffset(out Vector3 offset), Is.True);
        Assert.That(Vector3.Dot(source.position + offset - button.FaceGraphic.transform.position, button.PressDirection) + pusher.TouchCollider.radius,
            Is.EqualTo(0f).Within(.0001f), "Visual contact uses the actual moving face, with no visible gap or penetration.");
        Assert.That(Vector3.Distance(source.position, pusher.RawPose.position), Is.LessThan(.0001f));

        source.position += button.transform.right * .09f;
        yield return FixedSteps(3);
        Assert.That(pusher.ContactButton, Is.Null);
        Assert.That(pusher.TryGetVisualOffset(out _), Is.False);
        Assert.That(Vector3.Distance(pusher.Body.position, source.position), Is.LessThan(.0001f),
            "The hand can leave laterally instead of sticking to an infinite plane.");
    }

    [UnityTest]
    public IEnumerator TrackingFocusPauseAndMountMovementRequireWithdrawalAfterOverpress()
    {
        PhysicalTableButton button = CreatePhysicalButton();
        source.position = SourceAtDepth(button, -.03f);
        yield return FixedSteps(4);
        for (int i = 1; i <= 25; i++)
        {
            source.position = SourceAtDepth(button, Mathf.Lerp(-.03f, .065f, i / 25f));
            yield return new WaitForFixedUpdate();
        }
        Assert.That(pusher.IsArmed, Is.True);
        tracked = false;
        yield return new WaitForFixedUpdate();
        tracked = true;
        yield return FixedSteps(5);
        Assert.That(pusher.IsArmed, Is.False, "Loss of tracking retains a quarantine beyond the collider's back face.");
        source.position = SourceAtDepth(button, -.03f);
        yield return FixedSteps(5);
        Assert.That(pusher.IsArmed, Is.True);

        foreach (string interruption in new[] { "OnApplicationFocus", "OnApplicationPause" })
        {
            for (int i = 1; i <= 25; i++)
            {
                source.position = SourceAtDepth(button, Mathf.Lerp(-.03f, .035f, i / 25f));
                yield return new WaitForFixedUpdate();
            }
            pusher.SendMessage(interruption, interruption == "OnApplicationPause");
            yield return null;
            Assert.That(pusher.IsArmed, Is.False);
            pusher.SendMessage(interruption, interruption == "OnApplicationFocus");
            yield return FixedSteps(5);
            Assert.That(pusher.IsArmed, Is.False, "Recovery while held still requires leaving the cap.");
            source.position = SourceAtDepth(button, -.03f);
            yield return FixedSteps(5);
            Assert.That(pusher.IsArmed, Is.True);
        }

        for (int i = 1; i <= 20; i++)
        {
            source.position = SourceAtDepth(button, Mathf.Lerp(-.03f, .025f, i / 20f));
            yield return new WaitForFixedUpdate();
        }
        button.PrepareMountMove();
        button.transform.position += button.transform.up * .01f;
        button.FinishMountMove();
        yield return FixedSteps(5);
        Assert.That(pusher.IsArmed, Is.False, "A relocated table panel cannot sweep into an armed hand.");
        Assert.That(pusher.TryGetVisualOffset(out _), Is.True, "The quarantined hand remains visually outside the relocated cap.");
    }

    [UnityTest]
    public IEnumerator ApproachingFromBehindCannotActuateTheButton()
    {
        PhysicalTableButton button = CreatePhysicalButton();
        source.position = SourceAtDepth(button, .07f);
        yield return FixedSteps(4);
        for (int i = 1; i <= 30; i++)
        {
            source.position = SourceAtDepth(button, Mathf.Lerp(.07f, -.03f, i / 30f));
            yield return new WaitForFixedUpdate();
        }
        yield return FixedSteps(5);
        Assert.That(presses, Is.Zero, "Contacts are quarantined at the back instead of pulling the cap toward the hand.");
        Assert.That(button.PressDepth, Is.LessThan(.001f));
    }

    private PhysicalTableButton CreatePhysicalButton()
    {
        var mount = new GameObject("Guided physical cap");
        mount.transform.SetParent(root.transform, false);
        PhysicalTableButton button = mount.AddComponent<PhysicalTableButton>();
        presses = 0;
        button.Configure(.07f, Color.blue, false, false, () => presses++);
        Physics.SyncTransforms();
        return button;
    }

    private Vector3 SourceAtDepth(PhysicalTableButton button, float depth)
    {
        return button.RestFaceCenter + button.PressDirection * (depth - pusher.TouchCollider.radius);
    }

    private void CreateStaticCap()
    {
        var cap = new GameObject("Occupied cap");
        cap.transform.SetParent(root.transform, false);
        cap.layer = PhysicalTableButton.PhysicsLayer;
        cap.AddComponent<BoxCollider>().size = new Vector3(.05f, .05f, .015f);
        Physics.SyncTransforms();
    }

    private Rigidbody CreateBody(string name, Vector3 localPosition, int layer)
    {
        var target = new GameObject(name);
        target.transform.SetParent(root.transform, false);
        target.transform.localPosition = localPosition;
        target.layer = layer;
        var collider = target.AddComponent<BoxCollider>();
        collider.size = new Vector3(.03f, .03f, .015f);
        collider.contactOffset = .0005f;
        Rigidbody body = target.AddComponent<Rigidbody>();
        body.useGravity = false;
        body.constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
        Physics.SyncTransforms();
        return body;
    }

    private static IEnumerator FixedSteps(int count)
    {
        for (int i = 0; i < count; i++) yield return new WaitForFixedUpdate();
    }
}
