using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public sealed class VrSelectionBridge : MonoBehaviour
{
    private ChessGameController gameController;
    private PieceView pieceView;
    private XRGrabInteractable interactable;
    private XRSelectFilterDelegate grabFilter;

    private void Awake()
    {
        gameController = FindFirstObjectByType<ChessGameController>();
        pieceView = GetComponent<PieceView>();
        interactable = GetComponent<XRGrabInteractable>();
        grabFilter = new XRSelectFilterDelegate((interactor, grabbed) => gameController != null && gameController.CanGrabPiece(pieceView));
    }

    private void OnEnable()
    {
        if (interactable == null)
        {
            return;
        }

        interactable.selectFilters.Add(grabFilter);
        interactable.selectEntered.AddListener(OnSelectEntered);
        interactable.selectExited.AddListener(OnSelectExited);
    }

    private void OnDisable()
    {
        if (interactable == null)
        {
            return;
        }

        interactable.selectFilters.Remove(grabFilter);
        interactable.selectEntered.RemoveListener(OnSelectEntered);
        interactable.selectExited.RemoveListener(OnSelectExited);
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        if (gameController != null)
        {
            gameController.GrabPiece(pieceView);
        }
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        if (gameController != null)
        {
            gameController.ReleasePiece(pieceView, transform.position);
        }
    }
}
