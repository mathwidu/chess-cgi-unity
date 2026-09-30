using System.Collections;
using UnityEngine;

public sealed class PieceView : MonoBehaviour
{
    public const int PhysicsLayer = 6;

    private const float SelectedScaleMultiplier = 1.07f;
    private const float MoveArcHeight = 0.18f;

    private Vector3 baseScale;
    private Quaternion restRotation;

    public BoardSquare Square { get; private set; }
    public ChessSide Side { get; private set; }
    public ChessPieceKind Kind { get; private set; }

    public void Initialize(VisualPieceState state)
    {
        Square = state.Square;
        Side = state.Side;
        Kind = state.Kind;
        baseScale = transform.localScale;
        restRotation = transform.localRotation;
        gameObject.name = $"{Side} {Kind} {Square.ToAlgebraic()}";
    }

    public void SetSquare(BoardSquare square)
    {
        Square = square;
        gameObject.name = $"{Side} {Kind} {Square.ToAlgebraic()}";
    }

    public void SetSelected(bool selected)
    {
        transform.localScale = selected ? baseScale * SelectedScaleMultiplier : baseScale;
    }

    public IEnumerator MoveTo(Vector3 target, float duration)
    {
        // Resolve movement in board space while its size or table height changes.
        Quaternion startRotation = transform.localRotation;
        Transform reference = transform.parent;
        Vector3 start = reference != null ? reference.InverseTransformPoint(transform.position) : transform.position;
        Vector3 end = reference != null ? reference.InverseTransformPoint(target) : target;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = Mathf.SmoothStep(0f, 1f, t);
            Vector3 arc = Vector3.up * (Mathf.Sin(t * Mathf.PI) * MoveArcHeight);
            Vector3 point = Vector3.Lerp(start, end, eased);
            transform.position = (reference != null ? reference.TransformPoint(point) : point) + arc;
            transform.localRotation = Quaternion.Slerp(startRotation, restRotation, eased);
            yield return null;
        }

        transform.position = reference != null ? reference.TransformPoint(end) : end;
        transform.localRotation = restRotation;
    }
}
