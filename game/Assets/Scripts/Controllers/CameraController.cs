using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class CameraController : MonoBehaviour
{
    [Header("Desktop")]
    [SerializeField] private float orbitSpeed = 80f;
    [SerializeField] private float zoomSpeed = 6f;
    [SerializeField] private float minDistance = 7f;
    [SerializeField] private float maxDistance = 15f;
    [SerializeField] private float turnPerspectiveDistance = 11.2f;
    [SerializeField] private float turnPerspectiveHeight = 8.4f;
    [SerializeField] private float transitionSpeed = 5f;
    [SerializeField] private Vector3 target = new Vector3(0f, 0f, 0.35f);

    private Coroutine perspectiveTransition;

    public ChessSide CurrentPerspective { get; private set; } = ChessSide.White;

    private void Update()
    {
        if (XRRig.IsHeadsetPresent)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;

        float orbitDirection = 0f;
        if (keyboard != null && keyboard.qKey.isPressed)
        {
            orbitDirection -= 1f;
        }

        if (keyboard != null && keyboard.eKey.isPressed)
        {
            orbitDirection += 1f;
        }

        float scrollDelta = mouse == null ? 0f : mouse.scroll.ReadValue().y * 0.01f;

        ApplyOrbitAndZoom(orbitDirection, scrollDelta);

        if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
        {
            SetPerspective(CurrentPerspective, true);
        }
    }

    private void ApplyOrbitAndZoom(float orbitDirection, float scrollDelta)
    {
        if (Mathf.Abs(orbitDirection) > 0f)
        {
            transform.RotateAround(target, Vector3.up, orbitDirection * orbitSpeed * Time.deltaTime);
            transform.rotation = Quaternion.LookRotation(target - transform.position, Vector3.up);
        }

        if (Mathf.Abs(scrollDelta) > 0.01f)
        {
            Vector3 direction = (transform.position - target).normalized;
            float distance = Vector3.Distance(transform.position, target);
            distance = Mathf.Clamp(distance - scrollDelta * zoomSpeed, minDistance, maxDistance);
            transform.position = target + direction * distance;
            transform.rotation = Quaternion.LookRotation(target - transform.position, Vector3.up);
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

    private Vector3 GetPerspectivePosition(ChessSide side)
    {
        float z = side == ChessSide.White ? -turnPerspectiveDistance : turnPerspectiveDistance;
        return new Vector3(0f, turnPerspectiveHeight, z);
    }
}
