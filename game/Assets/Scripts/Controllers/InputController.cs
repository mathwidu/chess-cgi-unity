using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public sealed class InputController : MonoBehaviour
{
    [SerializeField] private ChessGameController gameController;
    [SerializeField] private Camera raycastCamera;
    private CameraController desktopCameraController;
    private readonly WebTouchGesture touchGesture = new WebTouchGesture();
    private readonly List<WebTouchPoint> touchPoints = new List<WebTouchPoint>(10);
    private float suppressMouseUntil;
    private bool waitForTouchRelease;

    public void Configure(ChessGameController controller, Camera camera)
    {
        gameController = controller;
        raycastCamera = camera;
        desktopCameraController = camera != null ? camera.GetComponent<CameraController>() : null;
    }

    private void Awake()
    {
        if (gameController == null)
        {
            gameController = Object.FindFirstObjectByType<ChessGameController>();
        }

        if (raycastCamera == null)
        {
            raycastCamera = Camera.main;
        }
        desktopCameraController = raycastCamera != null ? raycastCamera.GetComponent<CameraController>() : null;
    }

    private void Update()
    {
        if (WebTouchInput.Enabled && !XRRig.IsHeadsetPresent) HandleTouches();
        if (gameController == null || (desktopCameraController != null && desktopCameraController.BlocksBoardPointer))
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;

        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            gameController.CancelSelection();
        }

        if (keyboard != null && keyboard.nKey.wasPressedThisFrame && !gameController.IsMenuOpen)
        {
            gameController.NewGame();
        }

        if (mouse != null && mouse.leftButton.wasPressedThisFrame && Time.unscaledTime >= suppressMouseUntil)
        {
            HandlePrimaryClick(mouse.position.ReadValue());
        }
    }

    private void HandlePrimaryClick(Vector2 screenPosition)
    {
        if (gameController == null || raycastCamera == null || WebTouchInput.IsOverUi(screenPosition))
        {
            return;
        }

        Ray ray = raycastCamera.ScreenPointToRay(screenPosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, 200f))
        {
            return;
        }

        PieceView piece = hit.collider.GetComponentInParent<PieceView>();
        if (piece != null)
        {
            gameController.SelectPiece(piece);
            return;
        }

        SquareView square = hit.collider.GetComponentInParent<SquareView>();
        if (square != null)
        {
            gameController.SelectSquare(square);
        }
    }

    private void HandleTouches()
    {
        touchPoints.Clear();
        Touchscreen screen = Touchscreen.current;
        if (screen != null)
        {
            foreach (var touch in screen.touches)
            {
                if (!touch.press.isPressed && !touch.press.wasReleasedThisFrame) continue;
                Vector2 position = touch.position.ReadValue();
                touchPoints.Add(new WebTouchPoint(touch.touchId.ReadValue(), position,
                    touch.press.wasReleasedThisFrame, touch.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Canceled,
                    WebTouchInput.IsOverUi(position)));
            }
        }
        if (touchPoints.Count > 0) suppressMouseUntil = Time.unscaledTime + .35f;
        if (waitForTouchRelease)
        {
            if (touchPoints.Count == 0) waitForTouchRelease = false;
            return;
        }
        touchGesture.Update(touchPoints, Time.unscaledTime, Mathf.Min(Screen.width, Screen.height));
        if (desktopCameraController != null)
            desktopCameraController.ApplyTouchGesture(touchGesture.OrbitDelta, touchGesture.PinchDelta);
        if (touchGesture.HasTap && (desktopCameraController == null || !desktopCameraController.BlocksBoardPointer))
            HandlePrimaryClick(touchGesture.TapPosition);
    }

    private void OnApplicationFocus(bool focused)
    {
        if (!focused)
        {
            touchGesture.Reset();
            waitForTouchRelease = true;
        }
    }

    private void OnDisable()
    {
        touchGesture.Reset();
        waitForTouchRelease = true;
    }
}
