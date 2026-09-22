#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class MenuPresentationTests
{
    private GameHud hud;
    private ChessGameController controller;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/Main.unity", new LoadSceneParameters(LoadSceneMode.Single));
        yield return null;
        yield return null;
        hud = Object.FindFirstObjectByType<GameHud>();
        controller = Object.FindFirstObjectByType<ChessGameController>();
        Canvas.ForceUpdateCanvases();
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Scene previous = SceneManager.GetActiveScene();
        SceneManager.SetActiveScene(SceneManager.CreateScene("Empty after menu test"));
        yield return SceneManager.UnloadSceneAsync(previous);
    }

    [UnityTest]
    public IEnumerator DifficultyChoicesStartTheSelectedLevelAndKeepItOnReturn()
    {
        string[] names = { "BeginnerDifficultyButton", "IntermediateDifficultyButton", "HardDifficultyButton" };
        for (int i = 0; i < names.Length; i++)
        {
            yield return Click(names[i]);
            yield return Click("WhiteSideButton");
            yield return Click("StartPlayButton");
            Assert.That(controller.Difficulty, Is.EqualTo((ComputerDifficulty)i));
            Assert.That(controller.HumanSide, Is.EqualTo(ChessSide.White));
            Assert.That(controller.IsAgainstComputer, Is.True);
            Assert.That(controller.IsInputBlocked, Is.False);
            yield return Click("MenuButton");
            Assert.That(GameObject.Find(names[i]).transform.Find("SelectedMarker").GetComponent<Image>().enabled, Is.True);
            Assert.That(controller.IsMenuOpen, Is.True);
        }
        yield return Click("BlackSideButton");
        yield return Click("LocalModeButton");
        Assert.That(GameObject.Find("DifficultyOptions"), Is.Null);
        Assert.That(GameObject.Find("SideOptions"), Is.Null);
        yield return Click("StartPlayButton");
        Assert.That(controller.IsAgainstComputer, Is.False);
        Assert.That(controller.CurrentTurn, Is.EqualTo(ChessSide.White));
        yield return Click("MenuButton");
        yield return Click("ComputerModeButton");
        Assert.That(GameObject.Find("BlackSideButton").transform.Find("SelectedMarker").GetComponent<Image>().enabled, Is.True);
        yield return null;
    }

    [UnityTest]
    public IEnumerator KeyboardCanConfigureAndStartALocalGame()
    {
        EventSystem events = EventSystem.current;
        Assert.That(events.currentSelectedGameObject.name, Is.EqualTo("ComputerModeButton"));
        var move = new AxisEventData(events) { moveDir = MoveDirection.Right, moveVector = Vector2.right };
        ExecuteEvents.Execute(events.currentSelectedGameObject, move, ExecuteEvents.moveHandler);
        Assert.That(events.currentSelectedGameObject.name, Is.EqualTo("LocalModeButton"));
        ExecuteEvents.Execute(events.currentSelectedGameObject, new BaseEventData(events), ExecuteEvents.submitHandler);
        yield return null;
        move = new AxisEventData(events) { moveDir = MoveDirection.Down, moveVector = Vector2.down };
        ExecuteEvents.Execute(events.currentSelectedGameObject, move, ExecuteEvents.moveHandler);
        Assert.That(events.currentSelectedGameObject.name, Is.EqualTo("StartPlayButton"));
        ExecuteEvents.Execute(events.currentSelectedGameObject, new BaseEventData(events), ExecuteEvents.submitHandler);
        yield return null;
        Assert.That(controller.IsMenuOpen, Is.False);
        Assert.That(controller.IsAgainstComputer, Is.False);
        Assert.That(events.currentSelectedGameObject.name, Is.EqualTo("NewGameButton"));
        yield return Click("MenuButton");
        Assert.That(events.currentSelectedGameObject.name, Is.EqualTo("ComputerModeButton"));
    }

    [UnityTest]
    public IEnumerator HelpBlocksUnderlyingButtonsAndReturnsToTheConfiguration()
    {
        yield return Click("HardDifficultyButton");
        yield return Click("StartHowToPlayButton");
        Assert.That(GameObject.Find("StartPlayButton").GetComponent<Button>().IsInteractable(), Is.False);
        Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("CloseHelpButton"));
        yield return Click("CloseHelpButton");
        Assert.That(GameObject.Find("StartPlayButton").GetComponent<Button>().IsInteractable(), Is.True);
        Assert.That(GameObject.Find("HardDifficultyButton").transform.Find("SelectedMarker").GetComponent<Image>().enabled, Is.True);
        yield return Click("StartPlayButton");
        yield return Click("HowToPlayButton");
        Assert.That(GameObject.Find("NewGameButton").GetComponent<Button>().IsInteractable(), Is.False);
        yield return Click("CloseHelpButton");
        Assert.That(GameObject.Find("NewGameButton").GetComponent<Button>().IsInteractable(), Is.True);
        yield return null;
    }

    [UnityTest]
    public IEnumerator MenuFitsDesktopAndWorldCanvasSizesWithoutClippedLabels()
    {
        var canvas = hud.GetComponent<Canvas>();
        var scaler = hud.GetComponent<CanvasScaler>();
        scaler.enabled = false;
        canvas.renderMode = RenderMode.WorldSpace;
        var root = (RectTransform)canvas.transform;
        foreach (Vector2 size in new[] { new Vector2(1920, 1080), new Vector2(1280, 800), new Vector2(1024, 768), new Vector2(2560, 1080) })
        {
            root.sizeDelta = size;
            Canvas.ForceUpdateCanvases();
            hud.RefreshInterface();
            Canvas.ForceUpdateCanvases();
            var content = (RectTransform)GameObject.Find("MenuContent").transform;
            var corners = new Vector3[4];
            content.GetWorldCorners(corners);
            foreach (Vector3 corner in corners)
            {
                Vector2 local = root.InverseTransformPoint(corner);
                Assert.That(root.rect.Contains(local), Is.True, $"Menu outside canvas {size}: {local}");
            }
            foreach (var label in content.GetComponentsInChildren<Text>())
            {
                if (string.IsNullOrEmpty(label.text) || !label.enabled) continue;
                Assert.That(label.preferredHeight, Is.LessThanOrEqualTo(label.rectTransform.rect.height + 1), $"Clipped label {label.name}: {label.text}");
            }
        }
        yield return null;
    }

    private IEnumerator Click(string name)
    {
        Canvas.ForceUpdateCanvases();
        var target = GameObject.Find(name);
        Assert.That(target, Is.Not.Null, name);
        Assert.That(target.GetComponent<Button>().IsInteractable(), Is.True, name);
        var rect = (RectTransform)target.transform;
        var data = new PointerEventData(EventSystem.current)
        {
            position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center)),
            button = PointerEventData.InputButton.Left
        };
        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(data, hits);
        Assert.That(hits.Count, Is.GreaterThan(0), name);
        Assert.That(hits[0].gameObject, Is.EqualTo(target), $"Blocked click on {name}");
        ExecuteEvents.Execute(target, data, ExecuteEvents.pointerClickHandler);
        // Newly enabled Graphics enter the Canvas raycast registry on the next frame.
        yield return null;
    }
}
#endif
