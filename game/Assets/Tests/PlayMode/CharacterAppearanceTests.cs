#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class CharacterAppearanceTests
{
    private static readonly string[] Prefabs =
    {
        "Pawn_Mathwidu_Redhead_v2", "Rook_Alex", "Knight_Gustavo", "Bishop_Rafael", "Queen_Marta", "King_Ricardo_Carioca"
    };
    private GameObject stage;
    private PieceFactory factory;

    [SetUp]
    public void SetUp()
    {
        stage = new GameObject("Character test stage");
        factory = stage.AddComponent<PieceFactory>();
        for (int i = 0; i < Prefabs.Length; i++)
            factory.ConfigureCustomPrefab((ChessPieceKind)i, Resources.Load<GameObject>("CustomPieces/" + Prefabs[i]));
    }

    [TearDown]
    public void TearDown() => Object.DestroyImmediate(stage);

    [Test]
    public void EveryCharacterFitsItsSquareAtDesktopAndVrTableScale(
        [Values] ChessPieceKind kind, [Values(1f, 0.045f)] float tableScale)
    {
        stage.transform.localScale = Vector3.one * tableScale;
        stage.transform.position = new Vector3(2, 0.8f, -3);
        foreach (ChessSide side in new[] { ChessSide.White, ChessSide.Black })
        {
            PieceView piece = factory.CreatePiece(new VisualPieceState(new BoardSquare(0, 1), side, kind), stage.transform.position, stage.transform);
            var appearance = piece.GetComponentInChildren<CustomPieceAppearance>();
            Assert.That(appearance, Is.Not.Null, kind.ToString());
            Assert.That(appearance.Side, Is.EqualTo(side));
            Assert.That(piece.GetComponent<Collider>(), Is.Not.Null);
            Assert.That(piece.transform.Find("TeamBase"), Is.Null, "The authored base must not be duplicated.");
            Bounds bounds = LocalBounds(piece.transform);
            Assert.That(bounds.min.y, Is.EqualTo(0).Within(0.006f), "Feet/base float above the board.");
            Assert.That(bounds.min.x, Is.GreaterThanOrEqualTo(-0.625f));
            Assert.That(bounds.max.x, Is.LessThanOrEqualTo(0.625f));
            Assert.That(bounds.min.z, Is.GreaterThanOrEqualTo(-0.625f));
            Assert.That(bounds.max.z, Is.LessThanOrEqualTo(0.625f));
            float expectedHeight = kind == ChessPieceKind.Pawn ? 1.15f : kind == ChessPieceKind.Queen || kind == ChessPieceKind.King ? 1.43f : 1.31f;
            Assert.That(bounds.size.y, Is.EqualTo(expectedHeight).Within(0.01f));
            piece.SetSelected(true);
            bounds = LocalBounds(stage.transform);
            Assert.That(bounds.size.x, Is.LessThanOrEqualTo(1.25f));
            Assert.That(bounds.size.z, Is.LessThanOrEqualTo(1.25f));
        }
    }

    [Test]
    public void TeamSwapSharesGeometryAndDoesNotRecolorOtherPiecesOrPrefab([Values] ChessPieceKind kind)
    {
        GameObject prefab = Resources.Load<GameObject>("CustomPieces/" + Prefabs[(int)kind]);
        var before = prefab.GetComponentsInChildren<Renderer>().Select(r => r.sharedMaterials).ToArray();
        var white = factory.CreatePiece(new VisualPieceState(default, ChessSide.White, kind), Vector3.zero, stage.transform);
        var black = factory.CreatePiece(new VisualPieceState(default, ChessSide.Black, kind), Vector3.right, stage.transform);
        var whiteMeshes = white.GetComponentsInChildren<MeshFilter>();
        var blackMeshes = black.GetComponentsInChildren<MeshFilter>();
        Assert.That(whiteMeshes.Select(m => m.sharedMesh), Is.EqualTo(blackMeshes.Select(m => m.sharedMesh)));
        Renderer whiteBody = white.GetComponentsInChildren<Renderer>().Single(r => r.sharedMaterial.name == "Body_White");
        Renderer blackBody = black.GetComponentsInChildren<Renderer>().Single(r => r.sharedMaterial.name == "Body_Black");
        Assert.That(whiteBody.sharedMaterial.GetTexture("baseColorTexture"), Is.Not.SameAs(blackBody.sharedMaterial.GetTexture("baseColorTexture")));
        Assert.That(prefab.GetComponentsInChildren<Renderer>().Select(r => r.sharedMaterials).ToArray(), Is.EqualTo(before));
        Assert.That(whiteBody.sharedMaterial.name, Is.EqualTo("Body_White"));

        GameObject previewClone = Object.Instantiate(black.gameObject, stage.transform);
        Assert.That(previewClone.GetComponentInChildren<CustomPieceAppearance>().Side, Is.EqualTo(ChessSide.Black));
        Assert.That(previewClone.GetComponentsInChildren<Renderer>().Select(r => r.sharedMaterials),
            Is.EqualTo(black.GetComponentsInChildren<Renderer>().Select(r => r.sharedMaterials)));
    }

    [Test]
    public void ClothingAndBrandingKeepIndependentTexturesForBothSides([Values] ChessPieceKind kind)
    {
        foreach (ChessSide side in new[] { ChessSide.White, ChessSide.Black })
        {
            var piece = factory.CreatePiece(new VisualPieceState(default, side, kind), Vector3.zero, stage.transform);
            var materials = piece.GetComponentsInChildren<Renderer>().SelectMany(r => r.sharedMaterials).ToArray();
            var body = materials.Single(m => m.name == "Body_" + side);
            var brand = materials.Single(m => m.name == "FeevalePatch_" + side);
            var bodyMap = (Texture2D)body.GetTexture("baseColorTexture");
            var brandMap = (Texture2D)brand.GetTexture("baseColorTexture");
            Assert.That(brandMap, Is.Not.SameAs(bodyMap), "A small body UV island must not determine logo resolution.");
            Assert.That(brandMap.width, Is.GreaterThanOrEqualTo(1024));
            Assert.That(bodyMap.width, Is.GreaterThanOrEqualTo(2048));
            Assert.That(bodyMap.anisoLevel, Is.GreaterThanOrEqualTo(8));
            Assert.That(bodyMap.filterMode, Is.EqualTo(FilterMode.Trilinear));
            Assert.That(body.GetTexture("normalTexture"), Is.Not.Null);
        }
    }

    [Test]
    public void PerformanceModeAndLegacyCustomPrefabKeepTheirFallbacks()
    {
        factory.UsePrimitivePieces = true;
        PieceView primitive = factory.CreatePiece(new VisualPieceState(default, ChessSide.White, ChessPieceKind.Pawn), Vector3.zero, stage.transform);
        Assert.That(primitive.GetComponentInChildren<CustomPieceAppearance>(), Is.Null);
        Assert.That(primitive.transform.Find("Head"), Is.Not.Null);
        factory.UsePrimitivePieces = false;
        var legacy = GameObject.CreatePrimitive(PrimitiveType.Cube);
        legacy.transform.SetParent(stage.transform);
        factory.ConfigureCustomPrefab(ChessPieceKind.Rook, legacy);
        PieceView piece = factory.CreatePiece(new VisualPieceState(default, ChessSide.Black, ChessPieceKind.Rook), Vector3.zero, stage.transform);
        Assert.That(piece.transform.Find("TeamBase"), Is.Not.Null);
        Assert.That(piece.transform.Find("CustomVisual").GetComponent<Renderer>().bounds.size.y, Is.EqualTo(1.31f).Within(0.01f));
    }

    [UnityTest]
    public IEnumerator MainSceneUsesVariantsInMenuBoardAndSelectedPreview()
    {
        EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/Main.unity", new LoadSceneParameters(LoadSceneMode.Single));
        yield return null;
        yield return null;
        var controller = Object.FindFirstObjectByType<ChessGameController>();
        var board = Object.FindFirstObjectByType<BoardView>();
        Assert.That(GameObject.Find("WhiteProfessor").GetComponentInChildren<CustomPieceAppearance>().Side, Is.EqualTo(ChessSide.White));
        Assert.That(GameObject.Find("BlackProfessor").GetComponentInChildren<CustomPieceAppearance>().Side, Is.EqualTo(ChessSide.Black));
        // Set the factory directly so the verification does not change the user's saved preference.
        Object.FindFirstObjectByType<PieceFactory>().UsePrimitivePieces = false;
        controller.StartLocalGame();
        yield return null;
        Assert.That(board.Pieces.Count, Is.EqualTo(32));
        foreach (PieceView piece in board.Pieces)
            Assert.That(piece.GetComponentInChildren<CustomPieceAppearance>().Side, Is.EqualTo(piece.Side));
        controller.SelectPiece(board.Pieces.First(p => p.Square.Equals(BoardSquare.FromAlgebraic("e2"))));
        yield return null;
        var clone = GameObject.Find("SelectedPiecePreviewClone");
        Assert.That(clone, Is.Not.Null);
        Assert.That(clone.GetComponentInChildren<CustomPieceAppearance>().Side, Is.EqualTo(ChessSide.White));
        Vector3 whitePreviewFacing = clone.GetComponentInChildren<CustomPieceAppearance>().transform.forward;
        controller.SelectDestination(BoardSquare.FromAlgebraic("e4"));
        float deadline = Time.realtimeSinceStartup + 5;
        while (controller.IsInputBlocked && Time.realtimeSinceStartup < deadline) yield return null;
        controller.SelectPiece(board.Pieces.First(p => p.Square.Equals(BoardSquare.FromAlgebraic("e7"))));
        yield return null;
        Assert.That(GameObject.Find("SelectedPiecePreviewClone").GetComponentInChildren<CustomPieceAppearance>().Side, Is.EqualTo(ChessSide.Black));
        Vector3 blackPreviewFacing = GameObject.Find("SelectedPiecePreviewClone").GetComponentInChildren<CustomPieceAppearance>().transform.forward;
        Assert.That(Vector3.Dot(whitePreviewFacing, blackPreviewFacing), Is.GreaterThan(.999f), "Preview must face the camera for both teams.");
        Scene previous = SceneManager.GetActiveScene();
        SceneManager.SetActiveScene(SceneManager.CreateScene("Empty after character test"));
        yield return SceneManager.UnloadSceneAsync(previous);
    }

    private static Bounds LocalBounds(Transform root)
    {
        var bounds = new Bounds();
        bool first = true;
        foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>())
        {
            Bounds mesh = filter.sharedMesh.bounds;
            for (int i = 0; i < 8; i++)
            {
                Vector3 sign = new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1);
                Vector3 point = root.InverseTransformPoint(filter.transform.TransformPoint(mesh.center + Vector3.Scale(mesh.extents, sign)));
                if (first) { bounds = new Bounds(point, Vector3.zero); first = false; }
                else bounds.Encapsulate(point);
            }
        }
        return bounds;
    }
}
#endif
