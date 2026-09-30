#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class TableHeightTests
{
    private const string HeightStepKey = "ChessCgi.TableHeightStep";
    private bool hadSavedStep;
    private int savedStep;
    private TableView table;
    private BoardView board;
    private Rigidbody pusher;
    private float pusherRadius;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        // The height is a player preference; never leak a test's height into the Editor.
        hadSavedStep = PlayerPrefs.HasKey(HeightStepKey);
        savedStep = PlayerPrefs.GetInt(HeightStepKey, 0);
        PlayerPrefs.DeleteKey(HeightStepKey);
        EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/Main.unity", new LoadSceneParameters(LoadSceneMode.Single));
        yield return null;
        yield return null;
        table = Object.FindFirstObjectByType<TableView>();
        board = Object.FindFirstObjectByType<BoardView>();
        Press("LocalModeButton");
        Press("StartPlayButton");
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        if (pusher != null) Object.Destroy(pusher.gameObject);
        if (hadSavedStep) PlayerPrefs.SetInt(HeightStepKey, savedStep);
        else PlayerPrefs.DeleteKey(HeightStepKey);
        Scene previous = SceneManager.GetActiveScene();
        SceneManager.SetActiveScene(SceneManager.CreateScene("Empty after table test"));
        yield return SceneManager.UnloadSceneAsync(previous);
    }

    [UnityTest]
    public IEnumerator RaisingMovesTableAndBoardTogetherButNotTheCamera()
    {
        Assert.That(table, Is.Not.Null, "ScenePolish should build the table");
        Assert.That(table.HeightStep, Is.Zero);
        Vector3 cameraPosition = Camera.main.transform.position;
        float boardY = board.transform.position.y;
        Transform piece = board.Pieces[0].transform;
        float pieceY = piece.position.y;
        Transform room = GameObject.Find("FeevaleComputerLab").transform;
        Vector2 deskBefore = ChessDeskHeightRange(room);
        float roomY = room.position.y;
        Transform panel = table.transform.Find("Top/HeightPanel");
        float panelY = panel.position.y;

        Press("TableRaiseButton");
        yield return new WaitForSeconds(.35f);

        float lift = board.transform.position.y - boardY;
        Assert.That(table.HeightStep, Is.EqualTo(1));
        Assert.That(lift, Is.GreaterThan(0f));
        Assert.That(lift, Is.EqualTo(table.BoardOffset).Within(1e-4f));
        Assert.That(piece.position.y - pieceY, Is.EqualTo(lift).Within(1e-4f), "The pieces ride on the board.");
        Assert.That(panel.position.y - panelY, Is.EqualTo(lift).Within(1e-4f), "The edge controls ride on the table.");
        Vector2 deskAfter = ChessDeskHeightRange(room);
        Assert.That(deskAfter.y - deskBefore.y, Is.EqualTo(lift).Within(1e-3f), "The lab desk top stays right under the board.");
        Assert.That(deskAfter.x, Is.EqualTo(deskBefore.x).Within(1e-3f), "The desk grows; its feet stay on the floor.");
        Assert.That(room.position.y, Is.EqualTo(roomY).Within(1e-4f), "Only the desk moves, not the room.");
        Renderer[] boardBase = board.BoardFrameRoot.Find("BoardBase").GetComponentsInChildren<Renderer>();
        Assert.That(GameObject.Find("ChessTableSurface").transform.position.y,
            Is.EqualTo(boardBase.Min(r => r.bounds.min.y)).Within(1e-3f), "The board still rests on the desk.");
        Assert.That(Vector3.Distance(Camera.main.transform.position, cameraPosition), Is.LessThan(1e-4f), "The camera never follows the table.");
        Assert.That(PlayerPrefs.GetInt(HeightStepKey), Is.EqualTo(1));
        Assert.That(GameObject.Find("HeightText").GetComponent<Text>().text, Does.EndWith(" cm"));
    }

    [UnityTest]
    public IEnumerator HeightStopsAtItsLimitsAndSurvivesANewGame()
    {
        table.SetHeightStep(table.MaxStep - 1);
        Press("TableRaiseButton");
        yield return new WaitForSeconds(.65f);
        Assert.That(table.HeightStep, Is.EqualTo(table.MaxStep));
        Assert.That(Button("TableRaiseButton").IsInteractable(), Is.False);
        table.Raise();
        Assert.That(table.HeightStep, Is.EqualTo(table.MaxStep), "Raising past the limit does nothing.");

        table.SetHeightStep(table.MinStep + 1);
        Press("TableLowerButton");
        yield return new WaitForSeconds(.65f);
        Assert.That(table.HeightStep, Is.EqualTo(table.MinStep));
        Assert.That(Button("TableLowerButton").IsInteractable(), Is.False);
        Assert.That(Button("TableRaiseButton").IsInteractable(), Is.True);
        float lowered = table.BoardOffset;
        Assert.That(lowered, Is.LessThan(0f));

        Press("NewGameButton");
        yield return null;
        Assert.That(board.transform.position.y, Is.EqualTo(lowered).Within(1e-4f), "A new board is built on the same table height.");
    }

    [UnityTest]
    public IEnumerator PanelStaysBelowTheTableEdgeAndFacesBothPlayers()
    {
        Transform panel = table.transform.Find("Top/HeightPanel");
        Assert.That(panel, Is.Not.Null);
        Assert.That(panel.position.x, Is.LessThan(0f), "White plays from -z; the panel sits on their left.");
        Assert.That(panel.localPosition.z, Is.LessThan(-.45f), "The controls attach outside the front edge.");
        AssertPanelBelowTabletop();
        table.SetHeightStep(table.MinStep);
        AssertPanelBelowTabletop();
        table.SetHeightStep(table.MaxStep);
        AssertPanelBelowTabletop();
        var camera = Object.FindFirstObjectByType<CameraController>();
        camera.SetPerspective(ChessSide.Black, true);
        yield return null;
        Assert.That(panel.position.x, Is.GreaterThan(0f), "From the black side the panel moves to that player's left.");
        Assert.That(panel.localPosition.z, Is.GreaterThan(.45f));
        AssertPanelBelowTabletop();
        Vector3 face = GameObject.Find("HeightPanelCanvas").transform.forward * -1f;
        Assert.That(face.y, Is.GreaterThan(0.35f), "The edge-mounted face tilts up toward the player.");
        Assert.That(face.z, Is.GreaterThan(0f), "The panel face turns toward the black player.");
    }

    [UnityTest]
    public IEnumerator RoundControlsReceivePointerClicksAndIgnoreTheirCorners()
    {
        Camera camera = Camera.main;
        var view = Object.FindFirstObjectByType<CameraController>();
        Vector3 boardPosition = camera.transform.position;
        Quaternion boardRotation = camera.transform.rotation;
        Press("TableControlsButton");
        yield return null;
        Assert.That(view.IsLookingAround, Is.True);
        Canvas.ForceUpdateCanvases();

        foreach (string name in new[] { "TableRaiseButton", "TableLowerButton" })
        {
            Button button = Button(name);
            var rect = (RectTransform)button.transform;
            Assert.That(rect.parent, Is.EqualTo(Mechanism(name).Body.transform), "The desktop hit face must ride on the physical cap.");
            Vector2 point = camera.WorldToScreenPoint(rect.TransformPoint(rect.rect.center));
            var pointer = new PointerEventData(EventSystem.current) { position = point, button = PointerEventData.InputButton.Left };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            Assert.That(hits.FirstOrDefault().gameObject, Is.EqualTo(button.gameObject), "The focused controls must be visible and receive clicks ahead of the HUD.");
            Vector3 viewport = camera.WorldToViewportPoint(rect.TransformPoint(rect.rect.center));
            Assert.That(viewport.z, Is.GreaterThan(0f));
            Assert.That(viewport.x, Is.InRange(.1f, .9f));
            Assert.That(viewport.y, Is.InRange(.1f, .9f));
            foreach (Vector2 direction in new[] { Vector2.up, Vector2.down, Vector2.left, Vector2.right })
            {
                pointer.position = camera.WorldToScreenPoint(rect.TransformPoint(rect.rect.center + direction * rect.rect.width * .45f));
                hits.Clear();
                EventSystem.current.RaycastAll(pointer, hits);
                Assert.That(hits.FirstOrDefault().gameObject, Is.EqualTo(button.gameObject), "Points near the visible disc's edge must receive clicks.");
            }
            pointer.position = point;
            ExecuteEvents.Execute(hits[0].gameObject, pointer, ExecuteEvents.pointerClickHandler);
            Assert.That(table.HeightStep, Is.EqualTo(name == "TableRaiseButton" ? 0 : 1), "A pointer click requests a physical push, not an immediate height change.");
            yield return new WaitForSeconds(.65f);
            Assert.That(table.HeightStep, Is.EqualTo(name == "TableRaiseButton" ? 1 : 0));

            pointer.position = camera.WorldToScreenPoint(rect.TransformPoint(rect.rect.min + Vector2.one * 5f));
            hits.Clear();
            EventSystem.current.RaycastAll(pointer, hits);
            Assert.That(hits.Any(hit => hit.gameObject == button.gameObject), Is.False, "Empty square corners must not activate a round button.");
        }
        Press("RoomViewButton");
        yield return null;
        Assert.That(view.IsLookingAround, Is.False);
        Assert.That(Vector3.Distance(camera.transform.position, boardPosition), Is.LessThan(.001f));
        Assert.That(Quaternion.Angle(camera.transform.rotation, boardRotation), Is.LessThan(.01f));
    }

    [UnityTest]
    public IEnumerator CollisionPressNeedsTravelAndReleaseBeforeAnotherStep()
    {
        PhysicalTableButton button = Mechanism("TableRaiseButton");
        Assert.That(button.Body.isKinematic, Is.False, "The moving cap must be driven by physics.");
        Assert.That(button.Body.GetComponent<MeshCollider>().convex, Is.True);
        Assert.That(button.Body.GetComponent<ConfigurableJoint>(), Is.Not.Null);
        CreatePusher(button);
        yield return MovePusher(button, -.03f, .003f, 25);
        Assert.That(table.HeightStep, Is.Zero, "A shallow touch must not activate the button.");
        yield return MovePusher(button, .003f, PhysicalTableButton.Stroke * .93f, 25);
        yield return HoldPusher(button, PhysicalTableButton.Stroke * .93f, 20);
        Assert.That(button.IsPressed, Is.True, "The real collider must depress the dynamic cap.");
        Assert.That(table.HeightStep, Is.EqualTo(1));
        Assert.That(button.PressDepth, Is.LessThanOrEqualTo(PhysicalTableButton.Stroke + .001f), "The joint limits the cap's travel.");
        yield return HoldPusher(button, PhysicalTableButton.Stroke * .93f, 40);
        Assert.That(table.HeightStep, Is.EqualTo(1), "Holding down the cap never repeats a command.");

        yield return MovePusher(button, PhysicalTableButton.Stroke * .93f, -.03f, 25);
        yield return HoldPusher(button, -.03f, 40);
        Assert.That(button.PressFraction, Is.LessThan(.04f), "The spring returns the cap when the finger withdraws.");
        yield return MovePusher(button, -.03f, PhysicalTableButton.Stroke * .93f, 35);
        yield return HoldPusher(button, PhysicalTableButton.Stroke * .93f, 10);
        Assert.That(table.HeightStep, Is.EqualTo(2), "A second full press produces exactly one further step.");
    }

    [UnityTest]
    public IEnumerator DisabledCapStillMovesButCannotActivateUntilReleased()
    {
        table.SetHeightStep(table.MaxStep);
        PhysicalTableButton button = Mechanism("TableRaiseButton");
        Assert.That(button.IsAvailable, Is.False);
        CreatePusher(button);
        yield return MovePusher(button, -.03f, PhysicalTableButton.Stroke * .93f, 35);
        yield return HoldPusher(button, PhysicalTableButton.Stroke * .93f, 10);
        Assert.That(button.IsPressed, Is.True);
        Assert.That(table.HeightStep, Is.EqualTo(table.MaxStep));
        table.SetHeightStep(table.MaxStep - 1);
        yield return HoldPusher(button, PhysicalTableButton.Stroke * .93f, 20);
        Assert.That(table.HeightStep, Is.EqualTo(table.MaxStep - 1), "Making a held button available must not count as another press.");
        yield return MovePusher(button, PhysicalTableButton.Stroke * .93f, -.03f, 25);
        yield return HoldPusher(button, -.03f, 35);
        yield return MovePusher(button, -.03f, PhysicalTableButton.Stroke * .93f, 35);
        yield return HoldPusher(button, PhysicalTableButton.Stroke * .93f, 10);
        Assert.That(table.HeightStep, Is.EqualTo(table.MaxStep));
    }

    [UnityTest]
    public IEnumerator PhysicsCapsFollowHeightAndSideWithoutPhantomPresses()
    {
        foreach (int step in new[] { table.MinStep, table.MaxStep, 0 })
        {
            table.SetHeightStep(step);
            for (int i = 0; i < 20; i++) yield return new WaitForFixedUpdate();
            Assert.That(table.HeightStep, Is.EqualTo(step));
            foreach (PhysicalTableButton cap in table.GetComponentsInChildren<PhysicalTableButton>())
            {
                Vector3 local = cap.transform.InverseTransformPoint(cap.Body.position);
                Assert.That(local.magnitude, Is.LessThan(.0006f), "The cap and joint anchor move with the mount, with no accidental depression.");
                Assert.That(Quaternion.Angle(cap.transform.rotation, cap.Body.rotation), Is.LessThan(.5f));
            }
        }
        Object.FindFirstObjectByType<CameraController>().SetPerspective(ChessSide.Black, true);
        yield return null;
        for (int i = 0; i < 20; i++) yield return new WaitForFixedUpdate();
        Assert.That(table.HeightStep, Is.Zero);
        PhysicalTableButton lower = Mechanism("TableLowerButton");
        Assert.That(lower.PressDirection.z, Is.LessThan(0f));
        CreatePusher(lower);
        yield return MovePusher(lower, -.03f, PhysicalTableButton.Stroke * .93f, 35);
        yield return HoldPusher(lower, PhysicalTableButton.Stroke * .93f, 10);
        Assert.That(table.HeightStep, Is.EqualTo(-1), "The blue cap can be physically pressed from the black player's side.");
    }

    private void CreatePusher(PhysicalTableButton button)
    {
        var finger = new GameObject("Test kinematic fingertip");
        pusherRadius = .012f * button.transform.lossyScale.z;
        var collider = finger.AddComponent<SphereCollider>();
        collider.radius = pusherRadius;
        collider.contactOffset = .0005f * button.transform.lossyScale.z;
        collider.excludeLayers = ~(1 << PhysicalTableButton.PhysicsLayer);
        pusher = finger.AddComponent<Rigidbody>();
        pusher.isKinematic = true;
        pusher.useGravity = false;
        pusher.position = PusherPosition(button, -.03f);
    }

    private Vector3 PusherPosition(PhysicalTableButton button, float depth)
    {
        return button.RestFaceCenter + button.PressDirection * (depth * button.transform.lossyScale.z - pusherRadius);
    }

    private IEnumerator MovePusher(PhysicalTableButton button, float from, float to, int steps)
    {
        for (int i = 1; i <= steps; i++)
        {
            pusher.MovePosition(PusherPosition(button, Mathf.Lerp(from, to, i / (float)steps)));
            yield return new WaitForFixedUpdate();
        }
    }

    private IEnumerator HoldPusher(PhysicalTableButton button, float depth, int steps)
    {
        for (int i = 0; i < steps; i++)
        {
            pusher.MovePosition(PusherPosition(button, depth));
            yield return new WaitForFixedUpdate();
        }
    }

    private static PhysicalTableButton Mechanism(string buttonName)
    {
        return GameObject.Find(buttonName + "Mechanism").GetComponent<PhysicalTableButton>();
    }

    private void AssertPanelBelowTabletop()
    {
        var canvas = GameObject.Find("HeightPanelCanvas").GetComponent<RectTransform>();
        var corners = new Vector3[4];
        canvas.GetWorldCorners(corners);
        foreach (Vector3 corner in corners)
            Assert.That(table.transform.InverseTransformPoint(corner).y, Is.LessThan(table.Height), "The entire control face stays below the tabletop.");
    }

    private static Vector2 ChessDeskHeightRange(Transform room)
    {
        var range = new Vector2(float.MaxValue, float.MinValue);
        foreach (MeshFilter filter in room.GetComponentsInChildren<MeshFilter>())
        {
            foreach (Vector3 vertex in filter.sharedMesh.vertices)
            {
                Vector3 roomPoint = room.InverseTransformPoint(filter.transform.TransformPoint(vertex));
                if (Mathf.Abs(roomPoint.x) > 0.66f || Mathf.Abs(roomPoint.z) > 0.46f || roomPoint.y > 1f) continue;
                float y = filter.transform.TransformPoint(vertex).y;
                range = new Vector2(Mathf.Min(range.x, y), Mathf.Max(range.y, y));
            }
        }
        return range;
    }

    private static Button Button(string name)
    {
        GameObject target = GameObject.Find(name);
        Assert.That(target, Is.Not.Null, name);
        return target.GetComponent<Button>();
    }

    private static void Press(string name)
    {
        Button button = Button(name);
        Assert.That(button.IsInteractable(), Is.True, name);
        button.onClick.Invoke();
    }
}
#endif
