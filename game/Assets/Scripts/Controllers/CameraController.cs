using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

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
    [SerializeField] private float mouseLookSensitivity = 0.14f;
    [SerializeField] private float roomFieldOfView = 65f;

    private Coroutine perspectiveTransition;
    private Camera desktopCamera;
    private ChessGameController gameController;
    private bool pointerLooking;
    private float lookYaw, lookPitch, boardFieldOfView;
    private CursorLockMode previousCursorLock;
    private bool previousCursorVisible;
    private int suppressedPointerFrame = -1;

    public ChessSide CurrentPerspective { get; private set; } = ChessSide.White;
    public bool IsLookingAround { get; private set; }
    // Also inspect the button itself: InputController can update before this component.
    public bool BlocksBoardPointer => pointerLooking || suppressedPointerFrame == Time.frameCount
        || (Mouse.current != null && Mouse.current.rightButton.isPressed);

    private void Awake()
    {
        desktopCamera = GetComponent<Camera>();
        gameController = Object.FindFirstObjectByType<ChessGameController>();
    }

    private void Update()
    {
        if (XRRig.IsHeadsetPresent)
        {
            EndRoomView();
            return;
        }

        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;

        if (gameController != null && (gameController.IsMenuOpen || gameController.IsAwaitingPromotion))
        {
            if (IsLookingAround) ReturnToBoard();
            return;
        }
        if (keyboard != null && (keyboard.rKey.wasPressedThisFrame
            || (IsLookingAround && keyboard.escapeKey.wasPressedThisFrame)))
        {
            ReturnToBoard();
            return;
        }

        bool overUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        bool beganLook = false;
        if (mouse != null && mouse.rightButton.wasPressedThisFrame && !overUi
            && (desktopCamera == null || desktopCamera.pixelRect.Contains(mouse.position.ReadValue())))
        {
            EnterRoomView(false);
            previousCursorLock = Cursor.lockState;
            previousCursorVisible = Cursor.visible;
            pointerLooking = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            beganLook = true;
        }
        if (pointerLooking)
        {
            if (mouse == null || !mouse.rightButton.isPressed) ReleasePointer();
            else if (!beganLook)
            {
                // Mouse delta is already measured per input update, not per second.
                Vector2 delta = mouse.delta.ReadValue() * mouseLookSensitivity;
                lookYaw = Mathf.Repeat(lookYaw + delta.x, 360f);
                lookPitch = Mathf.Clamp(lookPitch - delta.y, -75f, 80f);
                transform.rotation = Quaternion.Euler(lookPitch, lookYaw, 0f);
            }
        }
        if (IsLookingAround) return;

        float orbitDirection = 0f;
        if (keyboard != null && keyboard.qKey.isPressed)
        {
            orbitDirection -= 1f;
        }

        if (keyboard != null && keyboard.eKey.isPressed)
        {
            orbitDirection += 1f;
        }

        float scrollDelta = mouse == null || overUi ? 0f : mouse.scroll.ReadValue().y * 0.01f;

        ApplyOrbitAndZoom(orbitDirection, scrollDelta);
    }

    public void LookAround()
    {
        if (XRRig.IsHeadsetPresent || !isActiveAndEnabled) return;
        EnterRoomView(true);
    }

    private void EnterRoomView(bool levelView)
    {
        StopPerspectiveTransition();
        if (!IsLookingAround)
        {
            if (desktopCamera != null) boardFieldOfView = desktopCamera.fieldOfView;
            IsLookingAround = true;
        }
        Vector3 angles = transform.eulerAngles;
        lookYaw = angles.y;
        lookPitch = levelView ? 0f : Mathf.DeltaAngle(0, angles.x);
        transform.rotation = Quaternion.Euler(lookPitch, lookYaw, 0);
        if (desktopCamera != null) desktopCamera.fieldOfView = roomFieldOfView;
    }

    public void ReturnToBoard()
    {
        SetPerspective(CurrentPerspective, true);
    }

    private void EndRoomView()
    {
        ReleasePointer();
        if (IsLookingAround && desktopCamera != null) desktopCamera.fieldOfView = boardFieldOfView;
        IsLookingAround = false;
    }

    private void ReleasePointer()
    {
        if (!pointerLooking) return;
        pointerLooking = false;
        suppressedPointerFrame = Time.frameCount;
        Cursor.lockState = previousCursorLock;
        Cursor.visible = previousCursorVisible;
    }

    private void OnApplicationFocus(bool focused)
    {
        if (!focused) ReleasePointer();
    }

    private void OnDisable()
    {
        EndRoomView();
        StopPerspectiveTransition();
    }

    private void StopPerspectiveTransition()
    {
        if (perspectiveTransition == null) return;
        StopCoroutine(perspectiveTransition);
        perspectiveTransition = null;
    }

    private void ApplyOrbitAndZoom(float orbitDirection, float scrollDelta)
    {
        if (Mathf.Abs(orbitDirection) > 0f)
        {
            StopPerspectiveTransition();
            transform.RotateAround(target, Vector3.up, orbitDirection * orbitSpeed * Time.deltaTime);
            transform.rotation = Quaternion.LookRotation(target - transform.position, Vector3.up);
        }

        if (Mathf.Abs(scrollDelta) > 0.01f)
        {
            StopPerspectiveTransition();
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
        if (XRRig.IsHeadsetPresent) return;
        // A completed turn must not turn the user's head while they inspect the room.
        if (IsLookingAround && !instant) return;
        EndRoomView();
        Vector3 targetPosition = GetPerspectivePosition(side);
        Quaternion targetRotation = Quaternion.LookRotation(target - targetPosition, Vector3.up);

        StopPerspectiveTransition();

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
