using System.Collections;
using UnityEngine;

public sealed class PieceView : MonoBehaviour
{
    private const float SelectedScaleMultiplier = 1.07f;

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

    public void ApplyHitReaction(float intensity)
    {
        float clamped = Mathf.Clamp01(intensity);
        transform.localScale = baseScale * Mathf.Lerp(1f, 0.82f, clamped);
    }

    public void RestoreBaseScale()
    {
        transform.localScale = baseScale;
    }

    public void SetVisualRoot(Transform visualRoot)
    {
        VisualRoot = visualRoot;
    }

    public IEnumerator MoveTo(Vector3 target, float duration)
    {
        return MoveWithWalk(
            target,
            new PieceMotionSettings(
                duration,
                PieceMotionSettings.Default.StepHeight,
                PieceMotionSettings.Default.LeanAngle,
                PieceMotionSettings.Default.CaptureDuration));
    }

    public IEnumerator MoveWithWalk(Vector3 target, PieceMotionSettings settings)
    {
        Vector3 start = transform.position;
        float duration = settings.WalkDuration;
        if (duration <= 0f)
        {
            transform.position = target;
            ResetVisualPose();
            yield break;
        }

        Vector3 visualStartLocalPosition = VisualRoot != null ? VisualRoot.localPosition : Vector3.zero;
        Quaternion visualStartLocalRotation = VisualRoot != null ? VisualRoot.localRotation : Quaternion.identity;
        float elapsed = 0f;

        FaceTowards(target);

        while (elapsed < duration)
        {
            float frameDelta = Time.deltaTime > 0f ? Time.deltaTime : duration;
            elapsed += frameDelta;
            float t = Mathf.Clamp01(elapsed / duration);
            WalkPose pose = EvaluateWalkPose(start, target, t, settings);
            transform.position = pose.RootPosition;
            if (VisualRoot != null && VisualRoot != transform)
            {
                VisualRoot.localPosition = visualStartLocalPosition + pose.VisualOffset;
                VisualRoot.localRotation = visualStartLocalRotation * pose.VisualRotation;
            }

            yield return null;
        }

        transform.position = target;
        if (VisualRoot != null && VisualRoot != transform)
        {
            VisualRoot.localPosition = visualStartLocalPosition;
            VisualRoot.localRotation = visualStartLocalRotation;
        }
    }

    public void FaceTowards(Vector3 worldTarget)
    {
        Vector3 direction = worldTarget - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }
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

    private void ResetVisualPose()
    {
        if (VisualRoot != null && VisualRoot != transform)
        {
            VisualRoot.localPosition = Vector3.zero;
            VisualRoot.localRotation = Quaternion.identity;
        }
    }
}
