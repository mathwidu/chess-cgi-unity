using UnityEngine;
using UnityEngine.InputSystem;

public sealed class CameraController : MonoBehaviour
{
    [SerializeField] private float orbitSpeed = 80f;
    [SerializeField] private float zoomSpeed = 6f;
    [SerializeField] private float minDistance = 8f;
    [SerializeField] private float maxDistance = 18f;

    private readonly Vector3 target = new Vector3(0f, 0f, 0.25f);
    private Vector3 defaultPosition;
    private Quaternion defaultRotation;

    private void Awake()
    {
        defaultPosition = transform.position;
        defaultRotation = transform.rotation;
    }

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
            transform.position = defaultPosition;
            transform.rotation = defaultRotation;
        }
    }
}
