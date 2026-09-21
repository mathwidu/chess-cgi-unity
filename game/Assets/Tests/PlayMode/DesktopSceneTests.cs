#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class DesktopSceneTests
{
    [UnityTest]
    [Category("StockfishIntegration")]
    public IEnumerator MainSceneMenuStartsARealComputerGameAndKeepsHumanPerspective()
    {
        if (!File.Exists(ComputerOpponentFactory.FindExecutable())) Assert.Ignore("Local Stockfish required.");
        EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/Main.unity", new LoadSceneParameters(LoadSceneMode.Single));
        yield return null;
        yield return null;
        var controller = Object.FindFirstObjectByType<ChessGameController>();
        var board = Object.FindFirstObjectByType<BoardView>();
        Assert.That(controller.IsMenuOpen, Is.True);
        GameObject.Find("SideChoiceButton").GetComponent<Button>().onClick.Invoke();
        GameObject.Find("StartPlayButton").GetComponent<Button>().onClick.Invoke();
        Assert.That(controller.IsAgainstComputer, Is.True);
        Assert.That(controller.HumanSide, Is.EqualTo(ChessSide.Black));
        float deadline = Time.realtimeSinceStartup + 10;
        while ((controller.MoveHistory.Count != 1 || controller.IsInputBlocked) && Time.realtimeSinceStartup < deadline)
            yield return null;
        Assert.That(controller.MoveHistory.Count, Is.EqualTo(1));
        Assert.That(controller.HasComputerError, Is.False);
        Assert.That(Object.FindFirstObjectByType<CameraController>().CurrentPerspective, Is.EqualTo(ChessSide.Black));
        controller.SelectPiece(board.Pieces.First(p => p.Square.Equals(BoardSquare.FromAlgebraic("e7"))));
        controller.SelectDestination(BoardSquare.FromAlgebraic("e5"));
        deadline = Time.realtimeSinceStartup + 10;
        while ((controller.MoveHistory.Count != 3 || controller.IsInputBlocked) && Time.realtimeSinceStartup < deadline)
            yield return null;
        Assert.That(controller.MoveHistory.Count, Is.EqualTo(3));
        Assert.That(controller.CurrentTurn, Is.EqualTo(ChessSide.Black));
        GameObject.Find("MenuButton").GetComponent<Button>().onClick.Invoke();
        Assert.That(controller.IsMenuOpen, Is.True);
        GameObject.Find("ModeChoiceButton").GetComponent<Button>().onClick.Invoke();
        GameObject.Find("StartPlayButton").GetComponent<Button>().onClick.Invoke();
        Assert.That(controller.IsAgainstComputer, Is.False);
        Assert.That(controller.MoveHistory.Count, Is.Zero);
        Scene previous = SceneManager.GetActiveScene();
        SceneManager.SetActiveScene(SceneManager.CreateScene("Empty after desktop test"));
        yield return SceneManager.UnloadSceneAsync(previous);
    }
}
#endif
