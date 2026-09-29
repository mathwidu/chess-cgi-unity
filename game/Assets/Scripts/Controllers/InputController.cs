using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public sealed class InputController : MonoBehaviour
{
    [SerializeField] private ChessGameController gameController;
    [SerializeField] private Camera raycastCamera;
    private CameraController desktopCameraController;

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

        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            HandlePrimaryClick(mouse.position.ReadValue());
        }
    }

    private void HandlePrimaryClick(Vector2 screenPosition)
    {
        if (raycastCamera == null || (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()))
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
}
