using UnityEngine;
using UnityEngine.InputSystem;

public sealed class InputController : MonoBehaviour
{
    [SerializeField] private ChessGameController gameController;
    [SerializeField] private Camera raycastCamera;

    public void Configure(ChessGameController controller, Camera camera)
    {
        gameController = controller;
        raycastCamera = camera;
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
    }

    private void Update()
    {
        if (gameController == null)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;

        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            gameController.CancelSelection();
        }

        if (keyboard != null && keyboard.nKey.wasPressedThisFrame)
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
        if (raycastCamera == null)
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
