#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class QuestCharacterDetailTests
{
    private GameObject root, clone;
    private Camera camera;
    private MeshFilter filter;
    private Mesh high, low;
    private QuestCharacterDetail detail;

    [SetUp]
    public void SetUp()
    {
        root = new GameObject("Distance detail test");
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.transform.SetParent(root.transform, false);
        filter = part.GetComponent<MeshFilter>();
        high = filter.sharedMesh;
        low = Object.Instantiate(high);
        low.name = "Synthetic table mesh";
        detail = root.AddComponent<QuestCharacterDetail>();
        camera = new GameObject("Detail test camera").AddComponent<Camera>();
        camera.transform.SetParent(root.transform, false);
        camera.enabled = false;
    }

    [TearDown]
    public void TearDown()
    {
        if (clone != null) Object.DestroyImmediate(clone);
        Object.DestroyImmediate(root);
        Object.DestroyImmediate(low);
    }

    [Test]
    public void DistanceChangesBothWaysWithAHysteresisBand()
    {
        Configure();
        ViewFrom(8f);
        Assert.That(filter.sharedMesh, Is.SameAs(low));
        ViewFrom(4.5f);
        Assert.That(filter.sharedMesh, Is.SameAs(low), "The middle band keeps table detail.");
        ViewFrom(3f);
        Assert.That(filter.sharedMesh, Is.SameAs(high));
        ViewFrom(4.5f);
        Assert.That(filter.sharedMesh, Is.SameAs(high), "The same middle band keeps inspection detail.");
        ViewFrom(6f);
        Assert.That(filter.sharedMesh, Is.SameAs(low));
    }

    [Test]
    public void EnlargingTheBoardAlsoEnlargesTheInspectionDistance()
    {
        Configure();
        ViewFrom(6f);
        Assert.That(filter.sharedMesh, Is.SameAs(low));
        root.transform.localScale = Vector3.one * 2f;
        ViewFrom(6f);
        Assert.That(filter.sharedMesh, Is.SameAs(high), "A larger piece occupies more of the view at the same distance.");
        root.transform.localScale = Vector3.one * .5f;
        ViewFrom(6f);
        Assert.That(filter.sharedMesh, Is.SameAs(low));
    }

    [Test]
    public void StableViewsAndMissingCamerasDoNotThrashOrHideThePiece()
    {
        Configure();
        int swaps = 0;
        detail.MeshesChanged += () => swaps++;
        ViewFrom(8f);
        for (int i = 0; i < 100; i++)
        {
            ViewFrom(i % 2 == 0 ? 4.3f : 4.7f);
            detail.RefreshForCamera(null);
        }
        Assert.That(swaps, Is.EqualTo(1));
        Assert.That(filter.sharedMesh, Is.SameAs(low));
        ViewFrom(3f);
        for (int i = 0; i < 100; i++) detail.RefreshForCamera(null);
        Assert.That(swaps, Is.EqualTo(2));
        Assert.That(filter.sharedMesh, Is.SameAs(high));
        Assert.That(filter.GetComponent<MeshRenderer>().enabled, Is.True);
    }

    [Test]
    public void SwapsPreserveBoundsMaterialsTransformsAndTheSameCollider()
    {
        var renderer = filter.GetComponent<MeshRenderer>();
        renderer.localBounds = new Bounds(new Vector3(.01f, .02f, 0), new Vector3(1.1f, 1.2f, 1f));
        Bounds bounds = renderer.localBounds;
        Material[] materials = renderer.sharedMaterials;
        Collider collider = filter.GetComponent<Collider>();
        Vector3 position = filter.transform.localPosition, scale = filter.transform.localScale;
        Quaternion rotation = filter.transform.localRotation;
        Configure();
        for (int i = 0; i < 4; i++)
        {
            ViewFrom(i % 2 == 0 ? 8f : 2f);
            Assert.That(renderer.localBounds, Is.EqualTo(bounds));
            Assert.That(renderer.sharedMaterials, Is.EqualTo(materials));
            Assert.That(filter.GetComponent<Collider>(), Is.SameAs(collider));
            Assert.That(collider.enabled, Is.True);
            Assert.That(filter.transform.localPosition, Is.EqualTo(position));
            Assert.That(filter.transform.localScale, Is.EqualTo(scale));
            Assert.That(filter.transform.localRotation, Is.EqualTo(rotation));
            Assert.That(root.GetComponentsInChildren<MeshRenderer>(true).Length, Is.EqualTo(1));
            Assert.That(root.GetComponentsInChildren<Collider>(true).Length, Is.EqualTo(1));
        }
    }

    [TestCase("missing")]
    [TestCase("submeshes")]
    [TestCase("coordinates")]
    [TestCase("uv")]
    public void AnIncompatibleTableCannotPartiallyReplaceTheCharacter(string problem)
    {
        GameObject second = GameObject.CreatePrimitive(PrimitiveType.Cube);
        second.transform.SetParent(root.transform, false);
        MeshFilter secondFilter = second.GetComponent<MeshFilter>();
        Mesh invalid = low;
        if (problem == "missing") invalid = null;
        if (problem == "submeshes") low.subMeshCount = 2;
        if (problem == "coordinates") low.bounds = new Bounds(Vector3.one * 10f, Vector3.one);
        if (problem == "uv") low.uv = System.Array.Empty<Vector2>();
        Assert.That(detail.Configure(new[] { filter, secondFilter }, new[] { high, high }, new[] { low, invalid }), Is.False);
        ViewFrom(8f);
        Assert.That(filter.sharedMesh, Is.SameAs(high));
        Assert.That(secondFilter.sharedMesh, Is.SameAs(high));
    }

    [UnityTest]
    public IEnumerator ACloneRemapsItsBindingsAndCanLockInspectionWithoutChangingTheSource()
    {
        Configure();
        ViewFrom(8f);
        clone = Object.Instantiate(root);
        QuestCharacterDetail cloneDetail = clone.GetComponent<QuestCharacterDetail>();
        MeshFilter cloneFilter = clone.GetComponentInChildren<MeshFilter>();
        Assert.That(cloneFilter.sharedMesh, Is.SameAs(low));
        cloneDetail.enabled = false;
        cloneDetail.ForceInspectionDetail();
        Assert.That(cloneDetail.enabled, Is.False);
        Assert.That(cloneFilter.sharedMesh, Is.SameAs(high));
        Assert.That(filter.sharedMesh, Is.SameAs(low));
        Assert.That(detail.enabled, Is.True);
        for (int frame = 0; frame < 3; frame++)
        {
            cloneDetail.RefreshForCamera(camera);
            yield return null;
            Assert.That(cloneFilter.sharedMesh, Is.SameAs(high));
        }
        Assert.That(clone.GetComponentsInChildren<MeshRenderer>(true).Length, Is.EqualTo(1));
        Assert.That(clone.GetComponentsInChildren<Collider>(true).Length, Is.EqualTo(1));
    }

    private void Configure() => Assert.That(detail.Configure(new[] { filter }, new[] { high }, new[] { low }), Is.True);

    private void ViewFrom(float distance)
    {
        camera.transform.position = Vector3.back * distance;
        detail.RefreshForCamera(camera);
    }
}
#endif
