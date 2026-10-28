#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;

public sealed class QuestCharacterGeometryTests
{
    private static readonly string[] Prefabs =
    {
        "Pawn_Mathwidu_Redhead_v2", "Rook_Alex", "Knight_Gustavo", "Bishop_Rafael", "Queen_Marta", "King_Ricardo_Carioca"
    };
    private GameObject stage;
    private PieceFactory factory;
    private XRSimulatedHMD headset;

    [SetUp]
    public void SetUp()
    {
        headset = InputSystem.AddDevice<XRSimulatedHMD>();
        stage = new GameObject("Quest geometry integration");
        stage.AddComponent<XRInteractionManager>();
        factory = stage.AddComponent<PieceFactory>();
        for (int i = 0; i < Prefabs.Length; i++)
            factory.ConfigureCustomPrefab((ChessPieceKind)i, Resources.Load<GameObject>("CustomPieces/" + Prefabs[i]));
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(stage);
        if (headset != null && headset.added) InputSystem.RemoveDevice(headset);
    }

    [Test]
    public void ReducedCharacterPreservesFitMaterialsAndPhysics([Values] ChessPieceKind kind, [Values] ChessSide side)
    {
        var prefab = Resources.Load<GameObject>("CustomPieces/" + Prefabs[(int)kind]);
        var prefabMeshes = prefab.GetComponentsInChildren<MeshFilter>().Select(f => f.sharedMesh).ToArray();
        var prefabMaterials = prefab.GetComponentsInChildren<Renderer>().Select(r => r.sharedMaterials).ToArray();
        factory.UseQuestCharacterGeometry = false;
        var original = Create(kind, side);
        factory.UseQuestCharacterGeometry = true;
        var optimized = Create(kind, side);
        CheckGeometry(original.gameObject, optimized.gameObject);
        Assert.That(optimized.GetComponent<MeshCollider>().sharedMesh,
            Is.SameAs(original.GetComponent<MeshCollider>().sharedMesh), "Physical shape must remain identical.");
        Assert.That(optimized.GetComponent<Rigidbody>().mass, Is.EqualTo(original.GetComponent<Rigidbody>().mass));
        Assert.That(optimized.GetComponent<ThrowablePiece>(), Is.Not.Null);
        Assert.That(optimized.GetComponent<VrSelectionBridge>(), Is.Not.Null);
        Assert.That(optimized.GetComponentInChildren<CustomPieceAppearance>().Side, Is.EqualTo(side));
        Assert.That(prefab.GetComponentsInChildren<MeshFilter>().Select(f => f.sharedMesh), Is.EqualTo(prefabMeshes));
        Assert.That(prefab.GetComponentsInChildren<Renderer>().Select(r => r.sharedMaterials), Is.EqualTo(prefabMaterials));
    }

    [Test]
    public void CapturedPiecesUseTheSameReducedGeometryAndKeepTheirCollider([Values] ChessPieceKind kind)
    {
        factory.UseQuestCharacterGeometry = false;
        var original = factory.CreateDisplayPiece(ChessSide.Black, kind, stage.transform);
        factory.UseQuestCharacterGeometry = true;
        var reduced = factory.CreateDisplayPiece(ChessSide.Black, kind, stage.transform);
        CheckGeometry(original, reduced);
        Assert.That(reduced.GetComponent<MeshCollider>().sharedMesh, Is.SameAs(original.GetComponent<MeshCollider>().sharedMesh));
        Assert.That(reduced.GetComponent<PieceView>(), Is.Null);
    }

    [Test]
    public void BothTeamsShareReducedMeshesAndPrimitiveModeStillWorks()
    {
        factory.UseQuestCharacterGeometry = true;
        var white = Create(ChessPieceKind.Pawn, ChessSide.White);
        var black = Create(ChessPieceKind.Pawn, ChessSide.Black);
        Assert.That(Filters(white.gameObject).Select(f => f.sharedMesh), Is.EqualTo(Filters(black.gameObject).Select(f => f.sharedMesh)));
        factory.UsePrimitivePieces = true;
        var primitive = Create(ChessPieceKind.Pawn, ChessSide.White);
        Assert.That(primitive.transform.Find("CustomVisual"), Is.Null);
        Assert.That(primitive.GetComponent<ThrowablePiece>(), Is.Not.Null);
    }

    [Test]
    public void TableAssetsSwapOnTheSamePartsAndRestoreInspection([Values] ChessPieceKind kind)
    {
        factory.UseQuestCharacterGeometry = true;
        PieceView piece = Create(kind, ChessSide.White);
        var detail = piece.GetComponentInChildren<QuestCharacterDetail>();
        Assert.That(detail, Is.Not.Null, "The complete Table asset must be compatible: " + kind);
        Assert.That(detail.enabled, Is.True, "An incompatible Table must not silently pass the integration test: " + kind);
        var filters = Filters(piece.gameObject);
        var geometry = Resources.Load<GameObject>($"QuestCharacterGeometry/{kind}/Geometry")
            .GetComponentsInChildren<MeshFilter>(true).ToDictionary(f => f.name, f => f.sharedMesh);
        var table = Resources.Load<GameObject>($"QuestCharacterGeometry/{kind}/Table")
            .GetComponentsInChildren<MeshFilter>(true).ToDictionary(f => f.name, f => f.sharedMesh);
        var bounds = filters.ToDictionary(f => f.name, f => f.GetComponent<Renderer>().localBounds);
        var materials = filters.ToDictionary(f => f.name, f => f.GetComponent<Renderer>().sharedMaterials);
        Collider[] colliders = piece.GetComponentsInChildren<Collider>(true);
        Mesh colliderMesh = piece.GetComponent<MeshCollider>().sharedMesh;
        int rendererCount = piece.GetComponentsInChildren<Renderer>(true).Length;
        long highTriangles = filters.Sum(f => Triangles(f.sharedMesh));
        var camera = new GameObject("Table geometry test camera").AddComponent<Camera>();
        camera.transform.SetParent(stage.transform);
        camera.enabled = false;
        camera.transform.position = Vector3.back * 100f;
        detail.RefreshForCamera(camera);
        Assert.That(filters.Sum(f => Triangles(f.sharedMesh)), Is.LessThan(highTriangles), kind + " needs a lighter table mesh.");
        foreach (MeshFilter filter in filters)
            Assert.That(filter.sharedMesh, Is.SameAs(table[filter.name]));
        Bounds totalBounds = filters[0].GetComponent<Renderer>().bounds;
        foreach (MeshFilter filter in filters) totalBounds.Encapsulate(filter.GetComponent<Renderer>().bounds);
        camera.transform.position = totalBounds.center + Vector3.back * .01f;
        detail.RefreshForCamera(camera);
        foreach (MeshFilter filter in filters)
        {
            Assert.That(filter.sharedMesh, Is.SameAs(geometry[filter.name]));
            Assert.That(filter.GetComponent<Renderer>().localBounds, Is.EqualTo(bounds[filter.name]));
            Assert.That(filter.GetComponent<Renderer>().sharedMaterials, Is.EqualTo(materials[filter.name]));
        }
        Assert.That(piece.GetComponentsInChildren<Renderer>(true).Length, Is.EqualTo(rendererCount));
        Assert.That(piece.GetComponentsInChildren<Collider>(true), Is.EqualTo(colliders));
        Assert.That(piece.GetComponent<MeshCollider>().sharedMesh, Is.SameAs(colliderMesh));
    }

    private PieceView Create(ChessPieceKind kind, ChessSide side) => factory.CreatePiece(
        new VisualPieceState(BoardSquare.FromAlgebraic("e2"), side, kind), Vector3.zero, stage.transform);

    private static MeshFilter[] Filters(GameObject piece) => piece.transform.Find("CustomVisual").GetComponentsInChildren<MeshFilter>();

    private static void CheckGeometry(GameObject original, GameObject reduced)
    {
        var source = Filters(original).ToDictionary(f => f.name);
        var optimized = Filters(reduced).ToDictionary(f => f.name);
        Assert.That(optimized.Keys, Is.EquivalentTo(source.Keys), "No accessory may disappear.");
        long before = 0, after = 0;
        foreach (var pair in source)
        {
            MeshFilter a = pair.Value, b = optimized[pair.Key];
            Assert.That(b.sharedMesh, Is.Not.SameAs(a.sharedMesh), pair.Key);
            before += Triangles(a.sharedMesh);
            after += Triangles(b.sharedMesh);
            Assert.That(b.GetComponent<Renderer>().sharedMaterials, Is.EqualTo(a.GetComponent<Renderer>().sharedMaterials));
            Assert.That(Vector3.Distance(b.GetComponent<Renderer>().bounds.center, a.GetComponent<Renderer>().bounds.center), Is.LessThan(.00001f));
            Assert.That(Vector3.Distance(b.GetComponent<Renderer>().bounds.size, a.GetComponent<Renderer>().bounds.size), Is.LessThan(.00001f));
            Assert.That(Vector3.Distance(b.transform.position, a.transform.position), Is.LessThan(.00001f));
            Assert.That(Vector3.Distance(b.transform.lossyScale, a.transform.lossyScale), Is.LessThan(.00001f));
            Assert.That(Quaternion.Angle(a.transform.rotation, b.transform.rotation), Is.LessThan(.001f));
            Assert.That(Vector3.Distance(b.sharedMesh.bounds.center, a.sharedMesh.bounds.center), Is.LessThan(.015f), "Export axes/centre changed.");
            Assert.That(Vector3.Distance(b.sharedMesh.bounds.size, a.sharedMesh.bounds.size), Is.LessThan(.025f), "Export scale changed.");
            Assert.That(b.sharedMesh.normals.Length, Is.EqualTo(b.sharedMesh.vertexCount));
            if (a.sharedMesh.uv.Length > 0) Assert.That(b.sharedMesh.uv.Length, Is.EqualTo(b.sharedMesh.vertexCount));
        }
        Assert.That(after, Is.LessThan(before * .35), "At least 65% fewer triangles per character.");
    }

    private static long Triangles(Mesh mesh)
    {
        long count = 0;
        for (int i = 0; i < mesh.subMeshCount; i++) count += mesh.GetIndexCount(i) / 3;
        return count;
    }
}
#endif
