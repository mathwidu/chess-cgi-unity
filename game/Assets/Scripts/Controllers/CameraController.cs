using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class CameraController : MonoBehaviour
{
    [SerializeField] private float orbitSpeed = 80f;
    [SerializeField] private float zoomSpeed = 6f;
    [SerializeField] private float minDistance = 7f;
    [SerializeField] private float maxDistance = 15f;
    [SerializeField] private float turnPerspectiveDistance = 11.2f;
    [SerializeField] private float turnPerspectiveHeight = 8.4f;
    [SerializeField] private float transitionSpeed = 5f;
    [SerializeField] private Vector3 target = new Vector3(0f, 0f, 0.35f);

    private Coroutine perspectiveTransition;
    private Coroutine shakeTransition;

    public ChessSide CurrentPerspective { get; private set; } = ChessSide.White;

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;

        if (keyboard != null && keyboard.qKey.isPressed)
        {
            transform.RotateAround(target, Vector3.up, -orbitSpeed * Time.deltaTime);
            transform.LookAt(target);
        }

        if (keyboard != null && keyboard.eKey.isPressed)
        {
            transform.RotateAround(target, Vector3.up, orbitSpeed * Time.deltaTime);
            transform.LookAt(target);
        }

        float scroll = mouse == null ? 0f : mouse.scroll.ReadValue().y * 0.01f;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            Vector3 direction = (transform.position - target).normalized;
            float distance = Vector3.Distance(transform.position, target);
            distance = Mathf.Clamp(distance - scroll * zoomSpeed, minDistance, maxDistance);
            transform.position = target + direction * distance;
            transform.LookAt(target);
        }

        if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
        {
            SetPerspective(CurrentPerspective, true);
        }
    }

    public void SetPerspective(ChessSide side, bool instant)
    {
        CurrentPerspective = side;
        Vector3 targetPosition = GetPerspectivePosition(side);
        Quaternion targetRotation = Quaternion.LookRotation(target - targetPosition, Vector3.up);

        if (perspectiveTransition != null)
        {
            StopCoroutine(perspectiveTransition);
            perspectiveTransition = null;
        }

        if (!Application.isPlaying || instant)
        {
            transform.position = targetPosition;
            transform.rotation = targetRotation;
            return;
        }

        perspectiveTransition = StartCoroutine(TransitionTo(targetPosition, targetRotation));
    }

    public void Shake(float amplitude, float duration)
    {
        if (!Application.isPlaying || amplitude <= 0f || duration <= 0f)
        {
            return;
        }

        if (shakeTransition != null)
        {
            StopCoroutine(shakeTransition);
        }

        shakeTransition = StartCoroutine(ShakeRoutine(amplitude, duration));
    }

    private IEnumerator TransitionTo(Vector3 targetPosition, Quaternion targetRotation)
    {
        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;
        float elapsed = 0f;

        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime * transitionSpeed;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed));
            transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
            yield return null;
        }

        transform.position = targetPosition;
        transform.rotation = targetRotation;
        perspectiveTransition = null;
    }

    private IEnumerator ShakeRoutine(float amplitude, float duration)
    {
        Vector3 basePosition = transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float frameDelta = Time.deltaTime > 0f ? Time.deltaTime : duration;
            elapsed += frameDelta;
            float fade = 1f - Mathf.Clamp01(elapsed / duration);
            Vector3 offset = new Vector3(Mathf.Sin(elapsed * 80f), Mathf.Cos(elapsed * 63f), 0f) * amplitude * fade;
            transform.position = basePosition + offset;
            yield return null;
        }

        transform.position = basePosition;
        shakeTransition = null;
    }

    private Vector3 GetPerspectivePosition(ChessSide side)
    {
        float z = side == ChessSide.White ? -turnPerspectiveDistance : turnPerspectiveDistance;
        return new Vector3(0f, turnPerspectiveHeight, z);
    }
}
