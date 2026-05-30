using UnityEngine;

public sealed class SquareView : MonoBehaviour
{
    public BoardSquare Square { get; private set; }

    public void Initialize(BoardSquare square)
    {
        Square = square;
        gameObject.name = $"Square {square.ToAlgebraic()}";
    }
}
