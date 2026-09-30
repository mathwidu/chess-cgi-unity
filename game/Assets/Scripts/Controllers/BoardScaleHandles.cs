using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.XR;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// The two anchored grips are selectable, not movable. Only their horizontal
// separation changes BoardView's size; the board remains fixed to the table.
public sealed class BoardScaleHandles : MonoBehaviour
{
    public const int PhysicsLayer = 26;
    private const float GripOffset = .075f;
    private const float SelectDistance = .085f;
    private const float HoldDistance = .12f;
    private readonly Dictionary<NearFarInteractor, GripSource> sources = new Dictionary<NearFarInteractor, GripSource>(4);
    private readonly List<XRInputSubsystem> trackingSubsystems = new List<XRInputSubsystem>(4);
    private readonly string[] percentages = new string[76];
    private BoardView board;
    private ChessGameController game;
    private GameObject visualRoot;
    private Material steel;
    private Transform leftRail;
    private Transform rightRail;
    private Transform leftMount;
    private Transform rightMount;
    private RectTransform label;
    private Text percentage;
    private Text instruction;
    private XRHandTrackingEvents leftHandEvents;
    private XRHandTrackingEvents rightHandEvents;
    private Transform observedOrigin;
    private Vector3 originPosition;
    private Quaternion originRotation;
    private Vector3 boardPosition;
    private Quaternion boardRotation;
    private Vector3 layoutScale;
    private Bounds layoutBounds;
    private bool layoutAsBlack;
    private bool focused = true;
    private bool paused;
    private bool requiresRelease;
    private ReleaseLatch releaseFirst;
    private ReleaseLatch releaseSecond;
    private Gesture gesture;

    public BoardScaleHandle LeftHandle { get; private set; }
    public BoardScaleHandle RightHandle { get; private set; }
    public bool IsResizing => gesture.IsActive;
    public bool HandlesVisible => visualRoot != null && visualRoot.activeSelf;

    public static BoardScaleHandles Build(BoardView boardView)
    {
        BoardScaleHandles existing = boardView.GetComponentInChildren<BoardScaleHandles>(true);
        if (existing != null)
        {
            existing.Interrupt(true);
            existing.RefreshLayout();
            return existing;
        }

        var root = new GameObject("BoardScaleHandles");
        root.transform.SetParent(boardView.transform, false);
        BoardScaleHandles handles = root.AddComponent<BoardScaleHandles>();
        handles.Initialize(boardView);
        return handles;
    }

    private void Initialize(BoardView boardView)
    {
        board = boardView;
        game = FindFirstObjectByType<ChessGameController>();
        focused = Application.isFocused;
        steel = ScenePolish.CreateMaterial("Runtime_BoardScale_Steel", new Color(.58f, .63f, .67f), .85f, .6f);
        visualRoot = new GameObject("Scale grips (VR)");
        visualRoot.transform.SetParent(transform, false);
        visualRoot.SetActive(false);
        LeftHandle = BuildGrip("Left scale grip", -1f);
        RightHandle = BuildGrip("Right scale grip", 1f);
        BuildLabel();
        for (int i = 0; i < percentages.Length; i++) percentages[i] = (i + 75).ToString() + "%";
        boardPosition = board.transform.position;
        boardRotation = board.transform.rotation;
        RefreshLayout();
        RefreshVisibility();
    }

    private BoardScaleHandle BuildGrip(string name, float side)
    {
        var gripObject = new GameObject(name);
        gripObject.layer = PhysicsLayer;
        gripObject.transform.SetParent(visualRoot.transform, false);
        var collider = gripObject.AddComponent<CapsuleCollider>();
        collider.direction = 0;
        collider.height = .11f;
        collider.radius = .012f;
        Renderer grip = MetalCylinder(gripObject.transform, "Steel grip", Vector3.zero, .02f, .1f);
        MetalCylinder(gripObject.transform, "Frame connection", Vector3.left * side * .062f, .012f, .026f);
        Transform rail = MetalCylinder(gripObject.transform, "Telescoping side connection", Vector3.zero, .012f, .002f).transform;
        rail.localRotation = Quaternion.Euler(90f, 0f, 0f);
        Transform mount = MetalCylinder(gripObject.transform, "Frame mount", Vector3.zero, .018f, .022f).transform;
        if (side < 0f) { leftRail = rail; leftMount = mount; }
        else { rightRail = rail; rightMount = mount; }
        MetalCylinder(gripObject.transform, "Inner collar", Vector3.left * side * .05f, .028f, .014f);
        MetalCylinder(gripObject.transform, "Outer collar", Vector3.right * side * .051f, .024f, .006f);
        BoardScaleHandle handle = gripObject.AddComponent<BoardScaleHandle>();
        handle.Configure(this, collider, grip);
        return handle;
    }

    private Renderer MetalCylinder(Transform parent, string name, Vector3 position, float diameter, float length)
    {
        GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cylinder.name = name;
        cylinder.layer = PhysicsLayer;
        cylinder.transform.SetParent(parent, false);
        cylinder.transform.localPosition = position;
        cylinder.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        cylinder.transform.localScale = new Vector3(diameter, length * .5f, diameter);
        Collider collider = cylinder.GetComponent<Collider>();
        collider.enabled = false;
        if (Application.isPlaying) Destroy(collider);
        else DestroyImmediate(collider);
        Renderer renderer = cylinder.GetComponent<Renderer>();
        renderer.sharedMaterial = steel;
        return renderer;
    }

    private void BuildLabel()
    {
        var canvasObject = new GameObject("Scale instruction", typeof(RectTransform), typeof(Canvas));
        label = (RectTransform)canvasObject.transform;
        label.SetParent(visualRoot.transform, false);
        label.sizeDelta = new Vector2(800f, 104f);
        canvasObject.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        percentage = LabelText("Scale percentage", font, new Vector2(0f, 23f), 32);
        instruction = LabelText("Two-hand instruction", font, new Vector2(0f, -21f), 24);
        percentage.color = new Color32(255, 221, 0, 255);
        instruction.text = "Segure as duas hastes";
    }

    private Text LabelText(string name, Font font, Vector2 position, int fontSize)
    {
        var textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
        var rect = (RectTransform)textObject.transform;
        rect.SetParent(label, false);
        rect.sizeDelta = new Vector2(800f, 48f);
        rect.anchoredPosition = position;
        Text text = textObject.GetComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(.92f, .94f, .95f);
        text.raycastTarget = false;
        return text;
    }

    private void Update()
    {
        if (board == null) return;
        RefreshVisibility();
        ObserveOrigin();
    }

    private void LateUpdate()
    {
        if (board == null) return;
        ObserveOrigin();
        if (requiresRelease && (releaseFirst == null || releaseFirst.IsReleased(this)) &&
            (releaseSecond == null || releaseSecond.IsReleased(this)))
        {
            requiresRelease = false;
            releaseFirst = releaseSecond = null;
        }

        bool moved = (board.transform.position - boardPosition).sqrMagnitude > .000001f ||
            Quaternion.Angle(board.transform.rotation, boardRotation) > .1f || layoutAsBlack != XRRig.SeatedAsBlack;
        if (moved || Blocked()) Interrupt(true);

        NearFarInteractor left = LeftHandle.Interactor;
        NearFarInteractor right = RightHandle.Interactor;
        if (!requiresRelease && !Blocked() && left != null && right != null)
        {
            Vector3 leftPoint = default;
            Vector3 rightPoint = default;
            InteractorHandedness leftIdentity = InteractorHandedness.None;
            InteractorHandedness rightIdentity = InteractorHandedness.None;
            // XRI can publish Region.None for the frame in which selection starts.
            // Physical proximity and the select filter already prove a near grab.
            bool valid = left != right && left.selectionRegion.Value != NearFarInteractor.Region.Far &&
                right.selectionRegion.Value != NearFarInteractor.Region.Far &&
                TryPose(left, out leftPoint, out leftIdentity) &&
                TryPose(right, out rightPoint, out rightIdentity) &&
                leftIdentity != rightIdentity &&
                LeftHandle.IsNear(leftPoint, HoldDistance) && RightHandle.IsNear(rightPoint, HoldDistance);
            if (!valid) Interrupt(true);
            else if (!gesture.IsActive)
                gesture.Begin(leftPoint, rightPoint, board.transform.right,
                    board.FrameLocalBounds.size.x * board.VrBaseScale.x);
            else if (gesture.Step(leftPoint, rightPoint, board.VrSize, out float size))
                board.SetVrSize(size);
            else Interrupt(true);
        }
        else gesture.End();

        RefreshLayout();
        RefreshFeedback();
        boardPosition = board.transform.position;
        boardRotation = board.transform.rotation;
    }

    private void RefreshVisibility()
    {
        bool visible = XRRig.IsHeadsetPresent && game != null && !game.IsMenuOpen;
        if (visualRoot.activeSelf == visible) return;
        if (!visible) Interrupt(true);
        visualRoot.SetActive(visible);
    }

    private bool Blocked()
    {
        if (!isActiveAndEnabled || !board.isActiveAndEnabled || !XRRig.IsHeadsetPresent || !focused || paused || Time.timeScale <= 0f || game == null ||
            !game.isActiveAndEnabled || game.IsMenuOpen || game.IsInputBlocked || game.IsAnimatingMove || game.SelectedPiece != null)
            return true;
        for (int i = 0; i < board.Pieces.Count; i++)
        {
            PieceView piece = board.Pieces[i];
            if (piece != null && piece.TryGetComponent(out XRGrabInteractable grabbed) && grabbed.isSelected) return true;
        }
        return false;
    }

    internal bool CanSelect(BoardScaleHandle handle, IXRSelectInteractor candidate)
    {
        if (requiresRelease || Blocked() || !(candidate is NearFarInteractor near) ||
            !near.enableNearCasting || near.selectionRegion.Value == NearFarInteractor.Region.Far)
            return false;
        if (!TryPose(near, out Vector3 position, out InteractorHandedness identity)) return false;
        BoardScaleHandle other = handle == LeftHandle ? RightHandle : LeftHandle;
        if (other != null && other.Interactor != null &&
            (!TryPose(other.Interactor, out _, out InteractorHandedness otherIdentity) || otherIdentity == identity))
            return false;
        return handle.IsNear(position, handle.Interactor == near ? HoldDistance : SelectDistance);
    }

    internal void ReleasedHandle(BoardScaleHandle handle, IXRSelectInteractor selector, bool forced)
    {
        gesture.End();
        if (!(selector is NearFarInteractor near)) return;
        ButtonControl button = sources.TryGetValue(near, out GripSource grip) ? grip.SelectionButton : null;
        if (button == null) button = near.selectInput.inputActionPerformed?.activeControl as ButtonControl;
        // ManagerSelectExit from a vetoed filter is not necessarily isCanceled.
        // Retain a pressed physical input before the handle clears its selector.
        if (forced || near.isSelectActive || (button != null && button.isPressed)) Latch(handle, near);
    }

    private void Latch(BoardScaleHandle handle, NearFarInteractor selector)
    {
        ReleaseLatch previous = handle == LeftHandle ? releaseFirst : releaseSecond;
        if (previous == null || previous.Interactor != selector)
        {
            if (!sources.TryGetValue(selector, out GripSource source))
            {
                source = new GripSource(selector);
                sources.Add(selector, source);
            }
            var latch = new ReleaseLatch(selector, source);
            if (handle == LeftHandle) releaseFirst = latch;
            else releaseSecond = latch;
        }
        requiresRelease = true;
    }

    private bool TryPose(NearFarInteractor interactor, out Vector3 position, out InteractorHandedness identity)
    {
        if (!sources.TryGetValue(interactor, out GripSource source))
        {
            source = new GripSource(interactor);
            sources.Add(interactor, source);
        }
        return source.Read(this, out position, out identity);
    }

    private void ObserveOrigin()
    {
        Transform origin = XRRig.Origin;
        if (observedOrigin != origin)
        {
            Interrupt(true);
            UnsubscribeTracking();
            observedOrigin = origin;
            leftHandEvents = rightHandEvents = null;
            sources.Clear();
            if (origin != null)
            {
                // Inventory only when a rig appears, not on every tracking update.
                XRHandTrackingEvents[] events = origin.GetComponentsInChildren<XRHandTrackingEvents>(true);
                for (int i = 0; i < events.Length; i++)
                    if (events[i].handedness == Handedness.Left) leftHandEvents = events[i];
                    else if (events[i].handedness == Handedness.Right) rightHandEvents = events[i];
                SubsystemManager.GetSubsystems(trackingSubsystems);
                for (int i = 0; i < trackingSubsystems.Count; i++)
                    trackingSubsystems[i].trackingOriginUpdated += TrackingOriginUpdated;
            }
        }
        else if (origin != null && ((origin.position - originPosition).sqrMagnitude > .00000001f ||
            Quaternion.Angle(origin.rotation, originRotation) > .01f)) Interrupt(true);
        if (origin == null) return;
        originPosition = origin.position;
        originRotation = origin.rotation;
    }

    private void TrackingOriginUpdated(XRInputSubsystem subsystem) => Interrupt(true);

    private void Interrupt(bool release)
    {
        gesture.End();
        if (!release || LeftHandle == null || RightHandle == null) return;
        if (LeftHandle.Interactor != null || RightHandle.Interactor != null)
        {
            if (LeftHandle.Interactor != null) Latch(LeftHandle, LeftHandle.Interactor);
            if (RightHandle.Interactor != null) Latch(RightHandle, RightHandle.Interactor);
        }
    }

    private void RefreshLayout()
    {
        if (board == null || LeftHandle == null) return;
        Bounds bounds = board.FrameLocalBounds;
        Vector3 scale = board.transform.lossyScale;
        if (bounds.size.x <= 0f || scale.x <= 0f || scale.y <= 0f || scale.z <= 0f) return;
        bool asBlack = XRRig.SeatedAsBlack;
        if (layoutScale == scale && layoutBounds == bounds && layoutAsBlack == asBlack) return;
        layoutScale = scale;
        layoutBounds = bounds;
        layoutAsBlack = asBlack;
        Vector3 physicalScale = new Vector3(1f / scale.x, 1f / scale.y, 1f / scale.z);
        float front = asBlack ? bounds.max.z - .36f : bounds.min.z + .36f;
        // Keep the hand's grip at one physical Z throughout the gesture. The
        // fixed front position also clears the captured trays at maximum size.
        float fixedFront = front * board.VrBaseScale.z * BoardView.MaximumVrSize;
        float z = fixedFront / scale.z;
        float extension = front * scale.z - fixedFront;
        float y = bounds.min.y + .025f / scale.y;
        LeftHandle.transform.localPosition = new Vector3(bounds.min.x - GripOffset / scale.x, y, z);
        RightHandle.transform.localPosition = new Vector3(bounds.max.x + GripOffset / scale.x, y, z);
        LeftHandle.transform.localScale = RightHandle.transform.localScale = physicalScale;
        SetConnection(leftRail, leftMount, .062f, extension);
        SetConnection(rightRail, rightMount, -.062f, extension);
        // Keep the central front edge clear for TurnIndicatorView's turn label.
        label.localPosition = new Vector3(LeftHandle.transform.localPosition.x, bounds.min.y + .002f / scale.y,
            z + (asBlack ? .035f : -.035f) / scale.z);
        label.localRotation = Quaternion.Euler(90f, asBlack ? 180f : 0f, 0f);
        label.localScale = physicalScale * .0005f;
    }

    private static void SetConnection(Transform rail, Transform mount, float x, float extension)
    {
        rail.localPosition = new Vector3(x, 0f, extension * .5f);
        rail.localScale = new Vector3(.012f, Mathf.Max(.002f, Mathf.Abs(extension)) * .5f, .012f);
        mount.localPosition = new Vector3(x, 0f, extension);
    }

    private void RefreshFeedback()
    {
        int index = Mathf.Clamp(Mathf.RoundToInt(board.VrSize * 100f) - 75, 0, percentages.Length - 1);
        percentage.text = percentages[index];
        instruction.text = requiresRelease ? "Solte as hastes para continuar" :
            Blocked() ? "Aguarde para ajustar" : gesture.IsActive ? "Afaste ou aproxime as mãos" : "Segure as duas hastes";
        LeftHandle.SetPaired(gesture.IsActive);
        RightHandle.SetPaired(gesture.IsActive);
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        focused = hasFocus;
        if (!hasFocus) Interrupt(true);
    }

    private void OnApplicationPause(bool isPaused)
    {
        paused = isPaused;
        if (isPaused) Interrupt(true);
    }

    private void OnDisable() => Interrupt(true);

    private void UnsubscribeTracking()
    {
        for (int i = 0; i < trackingSubsystems.Count; i++)
            trackingSubsystems[i].trackingOriginUpdated -= TrackingOriginUpdated;
        trackingSubsystems.Clear();
    }

    private void OnDestroy()
    {
        UnsubscribeTracking();
        if (steel != null) Destroy(steel);
    }

    private sealed class GripSource
    {
        private readonly NearFarInteractor interactor;
        private readonly Transform point;
        private readonly TrackedPoseDriver driver;
        public ButtonControl SelectionButton { get; private set; }

        public GripSource(NearFarInteractor selected)
        {
            interactor = selected;
            point = selected.nearInteractionCaster?.castOrigin ?? selected.transform;
            driver = point.GetComponentInParent<TrackedPoseDriver>();
        }

        public bool Read(BoardScaleHandles owner, out Vector3 position, out InteractorHandedness identity)
        {
            position = default;
            identity = InteractorHandedness.None;
            if (interactor == null || !interactor.isActiveAndEnabled || point == null ||
                !point.gameObject.activeInHierarchy || driver == null || !driver.isActiveAndEnabled) return false;
            var device = driver.positionInput.action?.activeControl?.device as TrackedDevice;
            const int poseTracked = (int)(InputTrackingState.Position | InputTrackingState.Rotation);
            if (device == null || !device.added || !device.isTracked.isPressed ||
                (device.trackingState.ReadValue() & poseTracked) != poseTracked) return false;
            if (SelectionButton == null || SelectionButton.device != device)
            {
                InputAction select = interactor.selectInput.inputActionPerformed;
                if (select != null)
                    for (int i = 0; i < select.controls.Count; i++)
                        if (select.controls[i] is ButtonControl button && button.device == device)
                        {
                            SelectionButton = button;
                            break;
                        }
            }
            for (int i = 0; i < device.usages.Count; i++)
            {
                if (device.usages[i] == UnityEngine.InputSystem.CommonUsages.LeftHand) identity = InteractorHandedness.Left;
                if (device.usages[i] == UnityEngine.InputSystem.CommonUsages.RightHand) identity = InteractorHandedness.Right;
            }
            if (identity == InteractorHandedness.None) return false;
            if (!(device is XRController))
            {
                XRHandTrackingEvents events = identity == InteractorHandedness.Left ? owner.leftHandEvents : owner.rightHandEvents;
                if (events == null || !events.isActiveAndEnabled || !events.handIsTracked) return false;
                XRHand hand = events.handedness == Handedness.Left ? events.subsystem.leftHand : events.subsystem.rightHand;
                if (!hand.GetJoint(XRHandJointID.IndexTip).TryGetPose(out _) ||
                    !hand.GetJoint(XRHandJointID.ThumbTip).TryGetPose(out _)) return false;
            }
            position = point.position;
            return Gesture.Finite(position);
        }
    }

    private sealed class ReleaseLatch
    {
        public readonly NearFarInteractor Interactor;
        private readonly GripSource source;
        private readonly ButtonControl physicalButton;

        public ReleaseLatch(NearFarInteractor interactor, GripSource gripSource)
        {
            Interactor = interactor;
            source = gripSource;
            physicalButton = gripSource.SelectionButton ??
                interactor.selectInput.inputActionPerformed?.activeControl as ButtonControl;
        }

        public bool IsReleased(BoardScaleHandles owner)
        {
            // Device state stays readable when an action/interactor is disabled.
            // Do not mistake a modality switch for releasing a held controller.
            if (physicalButton != null && physicalButton.device is XRController)
                return !physicalButton.device.added || !physicalButton.isPressed;
            // Hand pinch readers may be disabled with the hand. Wait for current
            // tracking before trusting a non-performed value from those readers.
            if (!source.Read(owner, out _, out _)) return false;
            return Interactor != null && !Interactor.selectInput.ReadIsPerformed();
        }
    }

    // Incremental world-space span avoids scale feedback from grip offsets and
    // rebases at both clamps, so reversing an overshoot has no dead zone.
    public struct Gesture
    {
        private Vector3 previousLeft;
        private Vector3 previousRight;
        private Vector3 axis;
        private float baseWidth;
        public bool IsActive { get; private set; }

        public bool Begin(Vector3 left, Vector3 right, Vector3 boardRight, float frameBaseWidth)
        {
            End();
            if (!Finite(left) || !Finite(right) || !Finite(boardRight) || !Finite(frameBaseWidth) ||
                frameBaseWidth <= .01f || boardRight.sqrMagnitude < .5f) return false;
            axis = boardRight.normalized;
            if (Vector3.Dot(right - left, axis) < .1f) return false;
            previousLeft = left;
            previousRight = right;
            baseWidth = frameBaseWidth;
            IsActive = true;
            return true;
        }

        public bool Step(Vector3 left, Vector3 right, float currentSize, out float nextSize)
        {
            nextSize = currentSize;
            if (!IsActive || !Finite(left) || !Finite(right) || !Finite(currentSize) ||
                (left - previousLeft).sqrMagnitude > .12f * .12f ||
                (right - previousRight).sqrMagnitude > .12f * .12f || Vector3.Dot(right - left, axis) < .1f)
            {
                End();
                return false;
            }
            float delta = Vector3.Dot((right - left) - (previousRight - previousLeft), axis);
            nextSize = Mathf.Clamp(currentSize + delta / baseWidth, BoardView.MinimumVrSize, BoardView.MaximumVrSize);
            previousLeft = left;
            previousRight = right;
            return true;
        }

        public void End() => IsActive = false;
        internal static bool Finite(Vector3 value) => Finite(value.x) && Finite(value.y) && Finite(value.z);
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
