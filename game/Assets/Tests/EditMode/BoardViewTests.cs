using NUnit.Framework;
using UnityEngine;

public class BoardViewTests
{
    [Test]
    public void GetWorldPosition_MapsCornersAroundOrigin()
    {
        GameObject gameObject = new GameObject("BoardView Test");
        BoardView boardView = gameObject.AddComponent<BoardView>();

        float halfBoard = 3.5f * boardView.SquareSize;

        Assert.AreEqual(new Vector3(-halfBoard, 0f, -halfBoard), boardView.GetWorldPosition(BoardSquare.FromAlgebraic("a1")));
        Assert.AreEqual(new Vector3(halfBoard, 0f, halfBoard), boardView.GetWorldPosition(BoardSquare.FromAlgebraic("h8")));

        Object.DestroyImmediate(gameObject);
    }

    [Test]
    public void GetPieceWorldPosition_UsesPieceBaseHeight()
    {
        GameObject gameObject = new GameObject("BoardView Test");
        BoardView boardView = gameObject.AddComponent<BoardView>();

        Vector3 position = boardView.GetPieceWorldPosition(BoardSquare.FromAlgebraic("e4"));

        Assert.AreEqual(0.08f, position.y);

        Object.DestroyImmediate(gameObject);
    }
}
