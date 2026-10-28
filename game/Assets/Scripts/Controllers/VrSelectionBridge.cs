using System.Collections;
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
    private const float MaximumPlacementSpeed = .35f;
    private const float PlacementHeightTolerance = .025f;
    // Height alone does not distinguish a gentle placement from a throw.
    // Observe the physical landing after an intentional release, allowing
    // flight time plus time for the opening fingers to withdraw. Never keep
    // a move pending until the loose piece's automatic three-second return.
    private const float HandPlacementObservationSeconds = .60f;
    private const float MaximumHandPlacementObservationSeconds = 2f;
    private const float BoardContactHeightTolerance = .005f;
    private const float RecentHandMotionSeconds = .03f;
    private const int MotionSampleCount = 16;

    private ChessGameController gameController;
    private PieceView pieceView;
    private ThrowablePiece throwable;
    private XRGrabInteractable interactable;
    private BoardView board;
    private XRSelectFilterDelegate grabFilter;
    private Coroutine pendingDrop;
    private Transform handMotionSource;
    private readonly Vector3[] handPositions = new Vector3[MotionSampleCount];
    private readonly float[] handTimes = new float[MotionSampleCount];
    private int handSampleCount;
    private int nextHandSample;
    private Transform baseGeometry;
    private Vector3[] basePoints;
    private Rigidbody body;
    private float lastBoardSupportAt = float.NegativeInfinity;
    private bool pendingHandVelocity;
    private Vector3 assistedVelocity;
    private XRHandGrabInput assistedHand;
    private BoardSquare assistedDestination;

    private void Awake()
    {
        gameController = FindFirstObjectByType<ChessGameController>();
        board = FindFirstObjectByType<BoardView>();
        pieceView = GetComponent<PieceView>();
        throwable = GetComponent<ThrowablePiece>();
        interactable = GetComponent<XRGrabInteractable>();
        body = GetComponent<Rigidbody>();
        CacheBasePoints();
        grabFilter = new XRSelectFilterDelegate((interactor, grabbed) =>
            interactor.IsSelecting(grabbed) || (gameController != null && gameController.CanGrabPiece(pieceView)));
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
        AbandonPendingDrop();
        handMotionSource = null;
        if (interactable == null)
        {
            return;
        }

        interactable.selectFilters.Remove(grabFilter);
        interactable.selectEntered.RemoveListener(OnSelectEntered);
        interactable.selectExited.RemoveListener(OnSelectExited);
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
        {
            AbandonPendingDrop();
        }
    }

    private void OnApplicationFocus(bool focused)
    {
        if (!focused)
        {
            AbandonPendingDrop();
        }
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        CancelPendingDrop();
        lastBoardSupportAt = float.NegativeInfinity;
        handSampleCount = nextHandSample = 0;
        handMotionSource = HandInput(args.interactorObject) != null
            ? args.interactorObject.GetAttachTransform(interactable) : null;
        RecordHandPosition();
        if (gameController != null)
        {
            gameController.GrabPiece(pieceView);
            Vibrate(args.interactorObject, GrabAmplitude, GrabSeconds);
        }
    }

    private void LateUpdate()
    {
        if (interactable != null && interactable.isSelected) RecordHandPosition();
        // XRInteractionManager's earlier LateUpdate has now detached and cached
        // the original XRI velocity. Correct only a deliberate placement,
        // before an old throw-history sample can slide it into another square.
        if (!pendingHandVelocity || throwable == null || !throwable.HasReleaseVelocity) return;
        pendingHandVelocity = false;
        if (interactable.isSelected || body == null || assistedHand == null || !assistedHand.CanCommitRelease ||
            gameController == null || gameController.SelectedPiece != pieceView || throwable.IsSupportedByHand ||
            !board.TryGetSquareAt(body.position, out BoardSquare current) || !current.Equals(assistedDestination) ||
            !gameController.CanStartMoveWithDrop(pieceView, body.position)) return;
        body.linearVelocity = assistedVelocity;
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        CancelPendingDrop();
        if (gameController == null || gameController.SelectedPiece != pieceView)
        {
            return;
        }
        XRHandGrabInput handInput = HandInput(args.interactorObject);
        bool intentional = !args.isCanceled && (handInput == null || handInput.CanCommitRelease);
        Vector3 releasePosition = transform.position;
        bool overBoard = TryGetBoardHeight(releasePosition, handInput != null, false, out float releaseHeight);
        bool inPlacementRegion = overBoard && !float.IsNaN(releaseHeight) && !float.IsInfinity(releaseHeight) &&
            releaseHeight >= -PlacementHeightTolerance &&
            (handInput != null || releaseHeight <= PlacementHeightTolerance);
        RecordHandPosition();
        bool hasHandVelocity = TryGetRecentHandVelocity(out Vector3 handVelocity);
        handMotionSource = null;
        if (!intentional || !inPlacementRegion)
        {
            gameController.CancelSelection();
            return;
        }
        lastBoardSupportAt = float.NegativeInfinity;
        // Same-square and illegal releases cannot become moves. Clear their
        // highlights now, without waiting for a physics tick on a fast render loop.
        if (!gameController.CanStartMoveWithDrop(pieceView, releasePosition))
        {
            bool accepted = gameController.ReleasePiece(pieceView, releasePosition, out _);
            Vibrate(args.interactorObject, accepted ? DropAmplitude : RefusedAmplitude, accepted ? DropSeconds : RefusedSeconds);
            return;
        }
        if (handInput != null && hasHandVelocity && handVelocity.sqrMagnitude <= MaximumPlacementSpeed * MaximumPlacementSpeed)
        {
            throwable.SetPlacementObservation(true);
            pendingHandVelocity = true;
            assistedVelocity = handVelocity;
            assistedHand = handInput;
            board.TryGetSquareAt(releasePosition, out assistedDestination);
        }
        pendingDrop = StartCoroutine(EvaluateDrop(args.interactorObject, releasePosition, handInput,
            hasHandVelocity, handVelocity, releaseHeight));
    }

    private void CancelPendingDrop()
    {
        if (pendingDrop != null) StopCoroutine(pendingDrop);
        CompleteObservation();
    }

    private void CompleteObservation()
    {
        pendingDrop = null;
        pendingHandVelocity = false;
        assistedHand = null;
        if (throwable != null) throwable.SetPlacementObservation(false);
    }

    private void AbandonPendingDrop()
    {
        bool wasObserving = pendingDrop != null;
        CancelPendingDrop();
        if (wasObserving) ClearOwnedSelection();
    }

    private void ClearOwnedSelection()
    {
        if (interactable != null && !interactable.isSelected && gameController != null &&
            gameController.SelectedPiece == pieceView) gameController.CancelSelection();
    }

    private IEnumerator EvaluateDrop(IXRSelectInteractor source, Vector3 releasePosition, XRHandGrabInput handInput,
        bool hasHandVelocity, Vector3 handVelocity, float releaseHeight)
    {
        bool trackedHand = handInput != null;
        float deadline = Time.time + (trackedHand ? HandObservationDuration(releaseHeight) : HandPlacementObservationSeconds);
        // XRI applies throw velocity in LateUpdate, after SelectExited. Wait for
        // that detach before interpreting the released object's physical motion.
        yield return null;
        while (throwable != null && throwable.isActiveAndEnabled && !throwable.HasReleaseVelocity &&
            !interactable.isSelected && Time.time <= deadline) yield return null;
        if (throwable == null || !throwable.isActiveAndEnabled || !throwable.HasReleaseVelocity)
        {
            CompleteObservation();
            ClearOwnedSelection();
            yield break;
        }
        Vector3 releaseVelocity = throwable.ReleaseVelocity;
        if (trackedHand)
        {
            Vector3 placementVelocity = hasHandVelocity ? handVelocity : releaseVelocity;
            bool slow = placementVelocity.sqrMagnitude <= MaximumPlacementSpeed * MaximumPlacementSpeed;
            throwable.SetPlacementObservation(slow);
            board.TryGetSquareAt(releasePosition, out BoardSquare destination);
            while (slow && Time.time <= deadline)
            {
                if (interactable.isSelected || gameController == null || gameController.SelectedPiece != pieceView)
                {
                    CompleteObservation();
                    yield break;
                }
                if (handInput == null || !handInput.CanCommitRelease || throwable == null || !throwable.isActiveAndEnabled)
                {
                    break;
                }
                Vector3 position = body != null ? body.position : transform.position;
                if (!board.TryGetSquareAt(position, out BoardSquare current) || !current.Equals(destination) ||
                    !gameController.CanStartMoveWithDrop(pieceView, position))
                {
                    break;
                }
                // A falling piece or a finger still leaving it is observed,
                // not accepted in the air. A palm that keeps carrying it times out.
                bool reachedBoard = Time.fixedTime - lastBoardSupportAt <= Time.fixedDeltaTime * 1.5f &&
                    TryGetBoardHeight(position, true, true, out float height) && Mathf.Abs(height) <= BoardContactHeightTolerance;
                if (reachedBoard && !throwable.IsSupportedByHand)
                {
                    CompleteObservation();
                    CommitDrop(source, position);
                    yield break;
                }
                yield return new WaitForFixedUpdate();
            }
            CompleteObservation();
            ClearOwnedSelection();
            yield break;
        }

        // Controllers keep their original release estimate and one-tick rule.
        yield return new WaitForFixedUpdate();
        CompleteObservation();
        if (interactable.isSelected || gameController == null || gameController.SelectedPiece != pieceView)
        {
            yield break;
        }
        bool placement = releaseVelocity.sqrMagnitude <= MaximumPlacementSpeed * MaximumPlacementSpeed && !throwable.IsSupportedByHand;
        if (!placement)
        {
            gameController.CancelSelection();
            yield break;
        }
        CommitDrop(source, releasePosition);
    }

    private void CommitDrop(IXRSelectInteractor source, Vector3 position)
    {
        bool accepted = gameController.ReleasePiece(pieceView, position, out bool moveStarted);
        if (moveStarted) throwable.Freeze();
        Vibrate(source, accepted ? DropAmplitude : RefusedAmplitude, accepted ? DropSeconds : RefusedSeconds);
    }

    private float HandObservationDuration(float releaseHeight)
    {
        float gravityTowardBoard = Vector3.Dot(Physics.gravity, -board.transform.up);
        float fallSeconds = gravityTowardBoard > .01f
            ? Mathf.Sqrt(2f * Mathf.Max(0f, releaseHeight) / gravityTowardBoard)
            : MaximumHandPlacementObservationSeconds;
        return Mathf.Min(MaximumHandPlacementObservationSeconds, HandPlacementObservationSeconds + fallSeconds);
    }

    private bool TryGetBoardHeight(Vector3 position, bool usePieceBase, bool physicsPose, out float height)
    {
        height = 0f;
        if (board == null || !board.TryGetSquareAt(position, out BoardSquare square)) return false;
        Vector3 plane = board.GetPieceWorldPosition(square);
        height = Vector3.Dot(position - plane, board.transform.up);
        if (usePieceBase && baseGeometry != null && basePoints != null && basePoints.Length > 0)
        {
            height = float.PositiveInfinity;
            foreach (Vector3 point in basePoints)
            {
                Vector3 world = physicsPose && body != null
                    ? body.position + body.rotation * Vector3.Scale(point, baseGeometry.lossyScale)
                    : baseGeometry.TransformPoint(point);
                height = Mathf.Min(height, Vector3.Dot(world - plane, board.transform.up));
            }
        }
        return true;
    }

    private void OnCollisionEnter(Collision collision) => ObserveBoardSupport(collision);
    private void OnCollisionStay(Collision collision) => ObserveBoardSupport(collision);

    private void ObserveBoardSupport(Collision collision)
    {
        if (board == null || interactable.isSelected || collision.collider.GetComponentInParent<PieceView>() != null ||
            collision.collider.GetComponentInParent<BoardView>() != board) return;
        for (int i = 0; i < collision.contactCount; i++)
            if (Vector3.Dot(collision.GetContact(i).normal, board.transform.up) > .35f)
            {
                lastBoardSupportAt = Time.fixedTime;
                return;
            }
    }

    private void CacheBasePoints()
    {
        if (TryGetComponent(out MeshCollider meshCollider) && meshCollider.sharedMesh != null)
        {
            baseGeometry = meshCollider.transform;
            Vector3[] vertices = meshCollider.sharedMesh.vertices;
            float bottom = float.PositiveInfinity;
            foreach (Vector3 vertex in vertices) bottom = Mathf.Min(bottom, vertex.y);
            var points = new System.Collections.Generic.List<Vector3>();
            foreach (Vector3 vertex in vertices)
                if (Mathf.Abs(vertex.y - bottom) < .0001f) points.Add(vertex);
            basePoints = points.ToArray();
        }
        else if (TryGetComponent(out BoxCollider box))
        {
            baseGeometry = box.transform;
            Vector3 half = box.size * .5f;
            basePoints = new[]
            {
                box.center + new Vector3(-half.x, -half.y, -half.z),
                box.center + new Vector3(-half.x, -half.y, half.z),
                box.center + new Vector3(half.x, -half.y, -half.z),
                box.center + new Vector3(half.x, -half.y, half.z),
            };
        }
    }

    private void RecordHandPosition()
    {
        if (handMotionSource == null) return;
        // The release event can arrive in the same frame as a previous sample.
        int previous = (nextHandSample + MotionSampleCount - 1) % MotionSampleCount;
        if (handSampleCount > 0 && handTimes[previous] == Time.time)
        {
            handPositions[previous] = handMotionSource.position;
            return;
        }
        handPositions[nextHandSample] = handMotionSource.position;
        handTimes[nextHandSample] = Time.time;
        nextHandSample = (nextHandSample + 1) % MotionSampleCount;
        handSampleCount = Mathf.Min(handSampleCount + 1, MotionSampleCount);
    }

    private bool TryGetRecentHandVelocity(out Vector3 velocity)
    {
        velocity = Vector3.zero;
        if (handSampleCount < 2) return false;
        int latest = (nextHandSample + MotionSampleCount - 1) % MotionSampleCount;
        int earlier = latest;
        for (int age = 1; age < handSampleCount; age++)
        {
            earlier = (latest + MotionSampleCount - age) % MotionSampleCount;
            if (handTimes[latest] - handTimes[earlier] >= RecentHandMotionSeconds) break;
        }
        float seconds = handTimes[latest] - handTimes[earlier];
        if (seconds < .01f) return false;
        velocity = (handPositions[latest] - handPositions[earlier]) / seconds;
        return true;
    }

    private static XRHandGrabInput HandInput(IXRSelectInteractor interactor) =>
        (interactor as Component)?.GetComponentInParent<XRHandGrabInput>();

    private static void Vibrate(IXRInteractor interactor, float amplitude, float seconds)
    {
        if (interactor is XRBaseInputInteractor inputInteractor)
        {
            inputInteractor.SendHapticImpulse(amplitude, seconds);
        }
    }
}
