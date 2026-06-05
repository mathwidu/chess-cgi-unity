using System.Collections;
using UnityEngine;

public sealed class PieceView : MonoBehaviour
{
    private const float SelectedScaleMultiplier = 1.07f;
    private const float MoveArcHeight = 0.18f;

    private Vector3 baseScale;

    public BoardSquare Square { get; private set; }
    public ChessSide Side { get; private set; }
    public ChessPieceKind Kind { get; private set; }
    public Transform VisualRoot { get; private set; }

    public readonly struct WalkPose
    {
        public WalkPose(Vector3 rootPosition, Vector3 visualOffset, Quaternion visualRotation)
        {
            RootPosition = rootPosition;
            VisualOffset = visualOffset;
            VisualRotation = visualRotation;
        }

        public Vector3 RootPosition { get; }

        public Vector3 VisualOffset { get; }

        public Quaternion VisualRotation { get; }
    }

    public void Initialize(VisualPieceState state)
    {
        Square = state.Square;
        Side = state.Side;
        Kind = state.Kind;
        baseScale = transform.localScale;
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

    public void SetVisualRoot(Transform visualRoot)
    {
        VisualRoot = visualRoot;
    }

    public IEnumerator MoveTo(Vector3 target, float duration)
    {
        Vector3 start = transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = Mathf.SmoothStep(0f, 1f, t);
            Vector3 arc = Vector3.up * (Mathf.Sin(t * Mathf.PI) * MoveArcHeight);
            transform.position = Vector3.Lerp(start, target, eased) + arc;
            yield return null;
        }

        transform.position = target;
    }

    public static WalkPose EvaluateWalkPose(Vector3 start, Vector3 target, float normalizedTime, PieceMotionSettings settings)
    {
        float t = Mathf.Clamp01(normalizedTime);
        float eased = Mathf.SmoothStep(0f, 1f, t);
        Vector3 rootPosition = Vector3.Lerp(start, target, eased);
        float step = Mathf.Sin(t * Mathf.PI);
        Vector3 visualOffset = Vector3.up * Mathf.Abs(step) * settings.StepHeight;
        float lean = Mathf.Sin(t * Mathf.PI * 2f) * settings.LeanAngle;
        Quaternion visualRotation = Quaternion.Euler(lean, 0f, 0f);

        if (t >= 1f)
        {
            visualOffset = Vector3.zero;
            visualRotation = Quaternion.identity;
        }

        return new WalkPose(rootPosition, visualOffset, visualRotation);
    }
}
