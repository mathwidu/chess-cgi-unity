#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
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
        Renderer leg = table.transform.Find("Leg").GetComponent<Renderer>();
        float floorContact = leg.bounds.min.y;
        float tabletopY = table.transform.Find("Top/Tabletop").GetComponent<Renderer>().bounds.max.y;

        Press("TableRaiseButton");
        yield return null;

        float lift = board.transform.position.y - boardY;
        Assert.That(table.HeightStep, Is.EqualTo(1));
        Assert.That(lift, Is.GreaterThan(0f));
        Assert.That(lift, Is.EqualTo(table.BoardOffset).Within(1e-4f));
        Assert.That(piece.position.y - pieceY, Is.EqualTo(lift).Within(1e-4f), "The pieces ride on the board.");
        Assert.That(table.transform.Find("Top/Tabletop").GetComponent<Renderer>().bounds.max.y - tabletopY,
            Is.EqualTo(lift).Within(1e-3f), "The tabletop stays right under the board.");
        Assert.That(leg.bounds.min.y, Is.EqualTo(floorContact).Within(1e-3f), "The legs grow; the feet stay on the floor.");
        Assert.That(Vector3.Distance(Camera.main.transform.position, cameraPosition), Is.LessThan(1e-4f), "The camera never follows the table.");
        Assert.That(PlayerPrefs.GetInt(HeightStepKey), Is.EqualTo(1));
        Assert.That(GameObject.Find("HeightText").GetComponent<Text>().text, Does.EndWith(" cm"));
    }

    [UnityTest]
    public IEnumerator HeightStopsAtItsLimitsAndSurvivesANewGame()
    {
        while (table.CanRaise) Press("TableRaiseButton");
        Assert.That(table.HeightStep, Is.EqualTo(table.MaxStep));
        Assert.That(Button("TableRaiseButton").IsInteractable(), Is.False);
        table.Raise();
        Assert.That(table.HeightStep, Is.EqualTo(table.MaxStep), "Raising past the limit does nothing.");

        while (table.CanLower) Press("TableLowerButton");
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
    public IEnumerator PanelFacesThePlayerAndMovesWithTheirSide()
    {
        Transform panel = table.transform.Find("Top/HeightPanel");
        Assert.That(panel, Is.Not.Null);
        Assert.That(panel.position.x, Is.LessThan(0f), "White plays from -z; the panel sits on their left.");
        var camera = Object.FindFirstObjectByType<CameraController>();
        camera.SetPerspective(ChessSide.Black, true);
        yield return null;
        Assert.That(panel.position.x, Is.GreaterThan(0f), "From the black side the panel moves to that player's left.");
        Vector3 face = GameObject.Find("HeightPanelCanvas").transform.forward * -1f;
        Assert.That(face.y, Is.GreaterThan(0.5f), "The panel face looks up at the player.");
        Assert.That(face.z, Is.GreaterThan(0f), "The panel face turns toward the black player.");
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
