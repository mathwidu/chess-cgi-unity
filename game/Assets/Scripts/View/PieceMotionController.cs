using System.Collections;
using UnityEngine;

public sealed class PieceMotionController : MonoBehaviour
{
    [SerializeField] private float walkDuration = 1.12f;
    [SerializeField] private float stepHeight = 0.045f;
    [SerializeField] private float leanAngle = 3.2f;
    [SerializeField] private float captureDuration = 0.45f;

    public PieceMotionSettings Settings => new PieceMotionSettings(walkDuration, stepHeight, leanAngle, captureDuration);

    public IEnumerator MovePiece(PieceView piece, Vector3 target)
    {
        if (piece == null)
        {
            yield break;
        }

        yield return piece.MoveWithWalk(target, Settings);
    }

    public IEnumerator PlayCapture(PieceView attacker, PieceView captured, Vector3 destination)
    {
        if (attacker == null)
        {
            yield break;
        }

        if (captured == null)
        {
            yield return MovePiece(attacker, destination);
            yield break;
        }

        Vector3 attackerStart = attacker.transform.position;
        CaptureAnimationStyle style = CaptureAnimationLibrary.GetStyle(attacker.Kind);
        CharacterAnimationDriver driver = attacker.GetComponentInChildren<CharacterAnimationDriver>();
        if (driver != null)
        {
            driver.TryPlayCapture(style);
        }

        Vector3 lungeTarget = Vector3.Lerp(attackerStart, captured.transform.position, style.LungeDistance);
        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, style.Duration);

        attacker.FaceTowards(captured.transform.position);

        while (elapsed < duration)
        {
            float frameDelta = Time.deltaTime > 0f ? Time.deltaTime : duration;
            elapsed += frameDelta;
            float t = Mathf.Clamp01(elapsed / duration);
            attacker.transform.position = Vector3.Lerp(attackerStart, lungeTarget, Mathf.SmoothStep(0f, 1f, t));
            captured.ApplyHitReaction(t);
            yield return null;
        }

        GameObject impact = ImpactEffect.CreateImpact(captured.transform.position + Vector3.up * 0.65f, style.ImpactColor);
        impact.transform.localScale *= style.ImpactScale;
        captured.gameObject.SetActive(false);
        yield return MovePiece(attacker, destination);
    }

    public void MoveInstant(PieceView piece, Vector3 target)
    {
        if (piece != null)
        {
            piece.transform.position = target;
        }
    }
}
