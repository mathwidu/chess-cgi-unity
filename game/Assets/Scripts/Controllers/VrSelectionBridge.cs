using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public sealed class VrSelectionBridge : MonoBehaviour
{
    private ChessGameController gameController;
    private PieceView pieceView;
    private SquareView squareView;
    private XRSimpleInteractable interactable;

    private void Awake()
    {
        gameController = FindFirstObjectByType<ChessGameController>();
        pieceView = GetComponent<PieceView>();
        squareView = GetComponent<SquareView>();
        interactable = GetComponent<XRSimpleInteractable>();
    }

    private void OnEnable()
    {
        if (interactable != null)
        {
            interactable.selectEntered.AddListener(OnSelectEntered);
        }
    }

    private void OnDisable()
    {
        if (interactable != null)
        {
            interactable.selectEntered.RemoveListener(OnSelectEntered);
        }
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        if (gameController == null)
        {
            return;
        }

        if (pieceView != null)
        {
            gameController.SelectPiece(pieceView);
            return;
        }

        if (squareView != null)
        {
            gameController.SelectSquare(squareView);
        }
    }
}
