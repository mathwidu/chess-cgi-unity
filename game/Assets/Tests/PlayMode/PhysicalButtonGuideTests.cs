using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class PhysicalButtonGuideTests
{
    private GameObject fixture;
    private PhysicalTableButton button;
    private Transform source;
    private XRPhysicsPusher hand;
    private int presses;

    [SetUp]
    public void SetUp()
    {
        presses = 0;
        fixture = new GameObject("Physical button guide test");
        fixture.transform.SetPositionAndRotation(new Vector3(30f, 2f, 0f), Quaternion.Euler(25f, 180f, 0f));
        var mount = new GameObject("Button mount");
        mount.transform.SetParent(fixture.transform, false);
        button = mount.AddComponent<PhysicalTableButton>();
        button.Configure(.09f, Color.red, true, false, () => presses++);
        source = new GameObject("Raw palm pose").transform;
        source.SetParent(fixture.transform, false);
        source.localPosition = Vector3.back * .1f;
        var contact = new GameObject("Physical palm");
        contact.transform.SetParent(fixture.transform, false);
        hand = contact.AddComponent<XRPhysicsPusher>();
        hand.Configure(source, () => true, fixture.transform, .025f);
        Physics.SyncTransforms();
    }

    [TearDown]
    public void TearDown() => Object.DestroyImmediate(fixture);

    private IEnumerator Step(Vector3 pose)
    {
        source.localPosition = pose;
        yield return new WaitForFixedUpdate();
        yield return null;
    }

    private void AssertInGuide()
    {
        Vector3 local = button.transform.InverseTransformPoint(button.Body.position);
        Assert.That(Mathf.Abs(local.x), Is.LessThan(.0002f));
        Assert.That(Mathf.Abs(local.y), Is.LessThan(.0002f));
        Assert.That(local.z, Is.InRange(-.0002f, PhysicalTableButton.Stroke + .0002f));
        Assert.That(Quaternion.Angle(button.Body.rotation, button.transform.rotation), Is.LessThan(.1f));
        Vector3 rendered = button.transform.InverseTransformPoint(button.Body.transform.position);
        Assert.That(Mathf.Abs(rendered.x), Is.LessThan(.0005f), "The interpolated visible cap stays in the guide too.");
        Assert.That(Mathf.Abs(rendered.y), Is.LessThan(.0005f));
        Assert.That(rendered.z, Is.InRange(-.0005f, PhysicalTableButton.Stroke + .0005f));
    }

    [UnityTest]
    public IEnumerator DeepPalmAndSidewaysMotionCannotRemoveOrTwistTheCap()
    {
        for (int i = 0; i < 6; i++) yield return Step(Vector3.back * .1f);
        Assert.That(hand.IsArmed, Is.True);
        for (int i = 0; i < 65; i++)
        {
            float depth = -.1f + i * .003f;
            yield return Step(new Vector3(Mathf.Sin(i * .15f) * .018f, .01f, depth));
            AssertInGuide();
        }
        Assert.That(presses, Is.EqualTo(1), "A deep tracked palm push is one press, never repeated steps.");
        float maximumCentre = PhysicalTableButton.Stroke - PhysicalTableButton.CapDepth * .5f - hand.TouchCollider.radius;
        float physicalDepth = button.transform.InverseTransformPoint(hand.Body.position).z;
        Assert.That(physicalDepth, Is.LessThanOrEqualTo(maximumCentre + .002f),
            "The physical hand stops at the end of the button stroke even when its raw pose continues through it.");
    }

    [UnityTest]
    public IEnumerator WithdrawingAfterADeepPushRestoresTheSpringAndAllowsAnotherPress()
    {
        for (int i = 0; i < 6; i++) yield return Step(Vector3.back * .1f);
        for (int i = 0; i < 60; i++) yield return Step(Vector3.forward * (-.1f + i * .003f));
        Assert.That(presses, Is.EqualTo(1));
        for (int i = 0; i < 60; i++) yield return Step(Vector3.forward * (.08f - i * .003f));
        for (int i = 0; i < 30; i++) yield return Step(Vector3.back * .1f);
        Assert.That(button.PressFraction, Is.LessThan(.04f));
        for (int i = 0; i < 60; i++) yield return Step(Vector3.forward * (-.1f + i * .003f));
        Assert.That(presses, Is.EqualTo(2));
        AssertInGuide();
    }

    [UnityTest]
    public IEnumerator BothFacesOfTheTableControlKeepTheirGuideWhenTheMountMoves()
    {
        for (int i = 0; i < 6; i++) yield return Step(Vector3.back * .1f);
        for (int i = 0; i < 45; i++) yield return Step(Vector3.forward * (-.1f + i * .002f));
        foreach (float angle in new[] { 180f, 0f, 180f })
        {
            button.PrepareMountMove();
            fixture.transform.SetPositionAndRotation(fixture.transform.position + Vector3.up * .03f,
                Quaternion.Euler(25f, angle, 0f));
            button.FinishMountMove();
            hand.RequireRearm();
            yield return null;
            AssertInGuide();
        }
        Assert.That(presses, Is.LessThanOrEqualTo(1), "Moving the mount cannot create another physical command.");
    }
}
