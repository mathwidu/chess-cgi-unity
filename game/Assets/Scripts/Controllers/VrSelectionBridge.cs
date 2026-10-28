using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public sealed class VrSelectionBridge : MonoBehaviour
{
    // Short pulses so the hand feels the grab and the drop; a refused drop buzzes longer.
    private const float GrabAmplitude = 0.3f;
    private const float GrabSeconds = 0.04f;
    private const float DropAmplitude = 0.5f;
    private const float DropSeconds = 0.07f;
    private const float RefusedAmplitude = 0.85f;
    private const float RefusedSeconds = 0.2f;

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
            Vibrate(args.interactorObject, GrabAmplitude, GrabSeconds);
        }
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        if (gameController != null)
        {
            bool accepted = gameController.ReleasePiece(pieceView, transform.position);
            Vibrate(args.interactorObject, accepted ? DropAmplitude : RefusedAmplitude, accepted ? DropSeconds : RefusedSeconds);
        }
    }

    private static void Vibrate(IXRInteractor interactor, float amplitude, float seconds)
    {
        if (interactor is XRBaseInputInteractor inputInteractor)
        {
            inputInteractor.SendHapticImpulse(amplitude, seconds);
        }
    }
}
