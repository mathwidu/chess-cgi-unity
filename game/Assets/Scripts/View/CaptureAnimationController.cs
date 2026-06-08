using System.Collections;
using UnityEngine;

public sealed class CaptureAnimationController : MonoBehaviour
{
    public IEnumerator PlayCapture(PieceView attacker, PieceView captured)
    {
        if (attacker == null || captured == null)
        {
            yield break;
        }

        CaptureAnimationStyle style = CaptureAnimationStyleLibrary.GetStyle(attacker.Kind);
        CharacterAnimationDriver driver = attacker.GetComponentInChildren<CharacterAnimationDriver>();
        if (driver != null)
        {
            driver.TryPlayCapture(style);
        }

        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, style.Duration);
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime > 0f ? Time.deltaTime : duration;
            yield return null;
        }
    }
}
