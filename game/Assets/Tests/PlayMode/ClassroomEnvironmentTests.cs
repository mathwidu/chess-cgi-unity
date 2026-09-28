#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

public sealed class ClassroomEnvironmentTests
{
    private BoardView board;
    private Transform room;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/Main.unity", new LoadSceneParameters(LoadSceneMode.Single));
        yield return null;
        yield return null;
        board = Object.FindFirstObjectByType<BoardView>();
        room = GameObject.Find("FeevaleComputerLab")?.transform;
        Assert.That(room, Is.Not.Null, "Main must load the actual environment prefab.");
    }

    private Transform Marker(string name) => room.GetComponentsInChildren<Transform>().Single(t => t.name == name);

    private void AssertTableContact()
    {
        Renderer[] boardBase = board.BoardFrameRoot.Find("BoardBase").GetComponentsInChildren<Renderer>();
        Assert.That(Marker("ChessTableSurface").position.y,
            Is.EqualTo(boardBase.Min(r => r.bounds.min.y)).Within(.0002f), "The board must rest on the table, without floating or intersecting.");
        Assert.That(Vector3.Distance(Marker("BoardAnchor").position, board.transform.position), Is.LessThan(.0002f));
    }

    [UnityTest]
    public IEnumerator MainLoadsLabWithoutDuplicatingPiecesOrBlockingBoardInput()
    {
        AssertTableContact();
        Assert.That(board.Pieces.Count, Is.EqualTo(32));
        Assert.That(room.GetComponentsInChildren<PieceView>().Length, Is.Zero);
        Assert.That(room.GetComponentsInChildren<Collider>().Length, Is.Zero);
        Assert.That(room.GetComponentsInChildren<Renderer>().Length, Is.LessThanOrEqualTo(48));
        Assert.That(Marker("RightWindows").localPosition.y, Is.GreaterThan(1f));
        var tabletop = room.GetComponentsInChildren<Renderer>().Single(r => r.name == "Furniture_NaturalAshDesk");
        Material surface = tabletop.sharedMaterial;
        Assert.That(surface.GetTexture("baseColorTexture"), Is.Not.Null, "The tabletop must use its baked wood colour.");
        Assert.That(surface.GetTexture("normalTexture"), Is.Not.Null, "The normal map must survive the glTF import.");
        Assert.That(surface.GetTexture("metallicRoughnessTexture"), Is.Not.Null, "The matte finish must survive the glTF import.");
        Vector3 target = board.GetWorldPosition(BoardSquare.FromAlgebraic("e4"));
        Assert.That(Physics.Raycast(target + Vector3.up * 5, Vector3.down, out RaycastHit hit, 10), Is.True);
        Assert.That(hit.collider.GetComponent<SquareView>(), Is.Not.Null);
        yield return null;
    }

    [UnityTest]
    public IEnumerator RoomTracksVrMetresAndReturnsToDesktopWithoutRebuilding()
    {
        int roomId = room.GetInstanceID();
        Vector3 oldPosition = board.transform.position, oldScale = board.transform.localScale;
        try
        {
            board.transform.SetPositionAndRotation(new Vector3(0, .78f, 0), Quaternion.identity);
            board.transform.localScale = Vector3.one * .045f;
            yield return null;
            AssertTableContact();
            Assert.That(room.lossyScale.x, Is.EqualTo(1).Within(.0001f));
            Assert.That(Marker("SeatedEyeWhite").position.y, Is.EqualTo(1.2f).Within(.0001f));
            Assert.That(Marker("SeatedEyeBlack").position.z, Is.EqualTo(.6f).Within(.0001f));
            board.transform.position = oldPosition;
            board.transform.localScale = oldScale;
            yield return null;
            AssertTableContact();
            Assert.That(room.GetInstanceID(), Is.EqualTo(roomId));
        }
        finally
        {
            board.transform.position = oldPosition;
            board.transform.localScale = oldScale;
        }
    }

    [UnityTest]
    public IEnumerator MenuStudioLightCannotRelightTheClassroom()
    {
        Camera camera = Camera.main;
        camera.transform.position = room.TransformPoint(new Vector3(0, 1.2f, -.6f));
        camera.transform.LookAt(room.TransformPoint(new Vector3(0, 1.4f, 4)));
        Light studio = GameObject.Find("StudioKey").GetComponent<Light>();
        bool studioEnabled = studio.enabled;
        var target = new RenderTexture(320, 180, 24);
        target.Create();
        var pixels = new Texture2D(320, 180, TextureFormat.RGB24, false);
        RenderTexture previous = RenderTexture.active;
        try
        {
            Color32[] Capture()
            {
                RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
                RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, 320, 180), 0, 0); pixels.Apply();
                return pixels.GetPixels32();
            }
            Color32[] before = Capture();
            studio.enabled = false;
            Color32[] after = Capture();
            int changed = before.Zip(after, (a, b) => Mathf.Abs(a.r-b.r)>2 || Mathf.Abs(a.g-b.g)>2 || Mathf.Abs(a.b-b.b)>2).Count(v => v);
            Assert.That(changed, Is.LessThan(8), "The menu studio relights the classroom: " + changed + " pixels.");
        }
        finally
        {
            studio.enabled = studioEnabled; RenderTexture.active = previous;
            target.Release(); Object.DestroyImmediate(target); Object.DestroyImmediate(pixels);
        }
        yield return null;
    }

    [UnityTest]
    public IEnumerator ReapplyingPolishLeavesOneRoomAndPreservesTheBoard()
    {
        Object.FindFirstObjectByType<ScenePolish>().ApplyPolish();
        yield return null;
        yield return null;
        Assert.That(Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Count(t => t.name == "FeevaleComputerLab"), Is.EqualTo(1));
        Assert.That(board.Pieces.Count, Is.EqualTo(32));
        Assert.That(Object.FindObjectsByType<ReflectionProbe>(FindObjectsSortMode.None).Count(p => p.name == "Room Reflection"), Is.EqualTo(1));
    }

    [UnityTest]
    public IEnumerator CoordinatesMatchTheInteractiveSquaresOnBothSides()
    {
        string MaterialAt(string square) => board.Squares.Single(s => s.Square.Equals(BoardSquare.FromAlgebraic(square)))
            .GetComponent<Renderer>().sharedMaterial.name;
        Assert.That(MaterialAt("a1"), Is.EqualTo("Board_Dark"));
        Assert.That(MaterialAt("h1"), Is.EqualTo("Board_Light"));
        Assert.That(MaterialAt("d1"), Is.EqualTo("Board_Light"), "White queen starts on a light square.");
        Assert.That(MaterialAt("d8"), Is.EqualTo("Board_Dark"), "Black queen starts on a dark square.");
        Transform[] markers = board.BoardFrameRoot.GetComponentsInChildren<Transform>();
        Assert.That(markers.Count(t => t.name.StartsWith("Coordinate_")), Is.EqualTo(32));
        for (int i = 0; i < 8; i++)
        {
            foreach (string side in new[] { "White", "Black" })
            {
                Transform file = markers.Single(t => t.name == "Coordinate_" + side + "File_" + (char)('A' + i));
                Transform rank = markers.Single(t => t.name == "Coordinate_" + side + "Rank_" + (i + 1));
                Vector3 filePosition = board.transform.InverseTransformPoint(file.position);
                Vector3 rankPosition = board.transform.InverseTransformPoint(rank.position);
                Assert.That(filePosition.x, Is.EqualTo((i - 3.5f) * board.SquareSize).Within(.001f));
                Assert.That(rankPosition.z, Is.EqualTo((i - 3.5f) * board.SquareSize).Within(.001f));
                Assert.That(Mathf.Sign(filePosition.z), Is.EqualTo(side == "White" ? -1 : 1));
                Assert.That(Mathf.Sign(rankPosition.x), Is.EqualTo(side == "White" ? -1 : 1));
            }
        }
        Assert.That(board.BoardFrameRoot.GetComponentsInChildren<Collider>().Length, Is.Zero);
        // Exercise actual raycasts across all empty ranks, not just a decorative count.
        for (int rank = 3; rank <= 6; rank++)
            for (int file = 0; file < 8; file++)
            {
                var square = new BoardSquare(file, rank);
                Vector3 point = board.GetWorldPosition(square);
                Assert.That(Physics.Raycast(point + Vector3.up * 3, Vector3.down, out RaycastHit hit, 4), Is.True);
                Assert.That(hit.collider.GetComponent<SquareView>().Square, Is.EqualTo(square));
            }
        yield return null;
    }

    [UnityTest]
    public IEnumerator ReflectionContainsOnlyTheRoomAndDoesNotRefreshEveryFrame()
    {
        ReflectionProbe probe = Object.FindObjectsByType<ReflectionProbe>(FindObjectsSortMode.None).Single(p => p.name == "Room Reflection");
        Assert.That(probe.refreshMode, Is.EqualTo(ReflectionProbeRefreshMode.ViaScripting));
        Assert.That(probe.resolution, Is.LessThanOrEqualTo(128));
        Assert.That(probe.cullingMask, Is.EqualTo(1 << 2));
        Assert.That(room.GetComponentsInChildren<Renderer>().All(r => (probe.cullingMask & (1 << r.gameObject.layer)) != 0), Is.True);
        Assert.That(board.Pieces.All(p => (probe.cullingMask & (1 << p.gameObject.layer)) == 0), Is.True);
        yield return null;
    }

    [UnityTest]
    public IEnumerator DesktopDefaultViewKeepsCoordinatesAboveTheActionBar()
    {
        Camera camera = Camera.main;
        camera.aspect = 16f / 9f;
        var view = camera.GetComponent<CameraController>();
        foreach (ChessSide side in new[] { ChessSide.White, ChessSide.Black })
        {
            view.SetPerspective(side, true);
            foreach (Transform marker in board.BoardFrameRoot.GetComponentsInChildren<Transform>().Where(t => t.name.StartsWith("Coordinate_")))
            {
                Vector3 point = camera.WorldToViewportPoint(marker.position);
                Assert.That(point.z, Is.GreaterThan(0));
                Assert.That(point.x, Is.InRange(.025f, .975f), side + ": " + marker.name);
                Assert.That(point.y, Is.InRange(.11f, .89f), side + ": " + marker.name);
            }
        }
        yield return null;
    }
}
#endif
