using System.Collections.Generic;
using UnityEngine;

public sealed class BoardView : MonoBehaviour
{
    public const float MinimumVrSize = 0.75f;
    public const float MaximumVrSize = 1.5f;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    // Tints blended into the square's own color, so light and dark squares stay readable.
    private static readonly Color LastMoveTint = new Color32(255, 221, 0, 255);
    private const float LastMoveWeight = 0.35f;
    private static readonly Color CheckTint = new Color(0.92f, 0.12f, 0.08f);
    private const float CheckWeight = 0.65f;
    private const string CaptureAuraMaterialPath = "Materials/CaptureAuraMaterial";
    private const string CheckAuraMaterialPath = "Materials/CheckAuraMaterial";
    private const float AuraSizeMultiplier = 1.5f;

    [SerializeField] private float squareSize = 1.25f;
    [SerializeField] private float pieceBaseHeight = 0.08f;
    [Header("Desktop")]
    [SerializeField] private Vector3 desktopBoardPosition = Vector3.zero;
    [SerializeField] private Vector3 desktopBoardScale = Vector3.one;
    [Header("VR")]
    [SerializeField] private Vector3 vrBoardPosition = new Vector3(0f, 0.78f, 0f);
    [SerializeField] private Vector3 vrBoardScale = new Vector3(0.045f, 0.045f, 0.045f);
    [SerializeField] private Transform boardFrameRoot;
    [SerializeField] private Transform squaresRoot;
    [SerializeField] private Transform piecesRoot;
    [SerializeField] private Transform highlightsRoot;
    [SerializeField] private Material lightSquareMaterial;
    [SerializeField] private Material darkSquareMaterial;
    [SerializeField] private Material highlightMaterial;

    private readonly List<SquareView> squares = new List<SquareView>();
    private readonly List<PieceView> pieces = new List<PieceView>();
    private float surfaceOffset;
    private float vrSize = 1f;
    private float appliedVrSize = 1f;
    private Vector3 sizeLift;
    private bool configuredForHeadset;
    private bool boardBuilt;
    private Bounds frameLocalBounds = new Bounds(new Vector3(0f, -0.265f, 0f), new Vector3(11.28f, 0.55f, 11.28f));
    private TurnIndicatorView turnIndicator;
    private CapturedPiecesView capturedPieces;
    private BoardSounds sounds;
    private BoardSquare? lastMoveFrom;
    private BoardSquare? lastMoveTo;
    private BoardSquare? checkSquare;
    private Transform checkAuraRoot;
    private PieceView checkAuraTarget;

    public float SquareSize => squareSize;
    public float PieceBaseHeight => pieceBaseHeight;
    public IReadOnlyList<SquareView> Squares => squares;
    public IReadOnlyList<PieceView> Pieces => pieces;
    public int HighlightCount => highlightsRoot == null ? 0 : highlightsRoot.childCount;
    public Transform BoardFrameRoot => boardFrameRoot;
    public float SurfaceOffset => surfaceOffset;
    public float VrSize => vrSize;
    public float AppliedVrSize => appliedVrSize;
    public Vector3 VrBaseScale => vrBoardScale;
    public Bounds FrameLocalBounds => frameLocalBounds;
    // A personal board size must never change the room's metre scale or floor position.
    public Vector3 RoomReferenceScale => transform.lossyScale / appliedVrSize;
    public Vector3 RoomReferencePosition => transform.position -
        (transform.parent != null
            ? transform.parent.TransformVector(Vector3.up * surfaceOffset + sizeLift)
            : Vector3.up * surfaceOffset + sizeLift);
    public TurnIndicatorView TurnIndicator => turnIndicator;
    public CapturedPiecesView CapturedPieces => capturedPieces;
    public BoardSounds Sounds => sounds;
    public BoardSquare? LastMoveFrom => lastMoveFrom;
    public BoardSquare? LastMoveTo => lastMoveTo;
    public BoardSquare? CheckSquare => checkSquare;

    public void Configure(
        Transform squaresParent,
        Transform piecesParent,
        Transform highlightsParent,
        Material lightMaterial,
        Material darkMaterial,
        Material legalMoveMaterial)
    {
        squaresRoot = squaresParent;
        piecesRoot = piecesParent;
        highlightsRoot = highlightsParent;
        lightSquareMaterial = lightMaterial;
        darkSquareMaterial = darkMaterial;
        highlightMaterial = legalMoveMaterial;
    }

    // Raises or lowers the board with the table under it; world units of the current mode.
    public void SetSurfaceOffset(float worldOffset)
    {
        surfaceOffset = worldOffset;
        ConfigureBoardTransformForMode();
    }

    public bool SetVrSize(float factor)
    {
        if (!XRRig.IsHeadsetPresent || float.IsNaN(factor) || float.IsInfinity(factor)) return false;
        float next = Mathf.Clamp(factor, MinimumVrSize, MaximumVrSize);
        if (Mathf.Approximately(next, vrSize)) return false;
        vrSize = next;
        ConfigureBoardTransformForMode();
        // Near selection and drops must see the new square and piece colliders immediately.
        Physics.SyncTransforms();
        return true;
    }

    private void Update()
    {
        // A simulator or runtime can become available after the scene has already built.
        if (boardBuilt && configuredForHeadset != XRRig.IsHeadsetPresent)
            ConfigureBoardTransformForMode();
    }

    // The room is modelled in VR meters around the VR board. On the desktop the same room is
    // scaled and moved so it sits under the desktop board exactly as it does in VR.
    public void FitVrRoomToMode(Transform room)
    {
        if (XRRig.IsHeadsetPresent)
        {
            room.localPosition = Vector3.zero;
            room.localScale = Vector3.one;
            return;
        }

        float scale = desktopBoardScale.y / vrBoardScale.y;
        room.localScale = Vector3.one * scale;
        room.localPosition = desktopBoardPosition - vrBoardPosition * scale;
    }

    public Vector3 GetWorldPosition(BoardSquare square)
    {
        return transform.TransformPoint(LocalSquarePosition(square, 0f));
    }

    public Vector3 GetPieceWorldPosition(BoardSquare square)
    {
        return transform.TransformPoint(LocalSquarePosition(square, pieceBaseHeight));
    }

    public bool TryGetSquareAt(Vector3 worldPosition, out BoardSquare square)
    {
        Vector3 local = transform.InverseTransformPoint(worldPosition);
        int fileIndex = Mathf.RoundToInt(local.x / squareSize + 3.5f);
        int rank = Mathf.RoundToInt(local.z / squareSize + 4.5f);

        if (fileIndex < 0 || fileIndex > 7 || rank < 1 || rank > 8)
        {
            square = default;
            return false;
        }

        square = new BoardSquare(fileIndex, rank);
        return true;
    }

    private Vector3 LocalSquarePosition(BoardSquare square, float localY)
    {
        float x = (square.FileIndex - 3.5f) * squareSize;
        float z = (square.Rank - 4.5f) * squareSize;
        return new Vector3(x, localY, z);
    }

    public void BuildBoard()
    {
        ConfigureBoardTransformForMode();
        EnsureRoots();
        ClearChildren(boardFrameRoot);
        ClearChildren(squaresRoot);
        ClearChildren(highlightsRoot);
        squares.Clear();
        lastMoveFrom = null;
        lastMoveTo = null;
        checkSquare = null;
        RefreshCheckAura();

        BuildBoardFrame();
        MeasureFrameBounds();
        ConfigureBoardTransformForMode();
        EnsureTurnIndicator();
        EnsureCapturedPieces();
        EnsureSounds();

        for (int rank = 1; rank <= 8; rank++)
        {
            for (int fileIndex = 0; fileIndex < 8; fileIndex++)
            {
                BoardSquare square = new BoardSquare(fileIndex, rank);
                GameObject squareObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                squareObject.transform.SetParent(squaresRoot);
                squareObject.transform.position = GetWorldPosition(square);
                squareObject.transform.localRotation = Quaternion.identity;
                squareObject.transform.localScale = new Vector3(squareSize, 0.08f, squareSize);

                Renderer renderer = squareObject.GetComponent<Renderer>();
                // The solid frame casts the board silhouette. Coplanar veneer
                // tiles only receive piece shadows, avoiding self-shadow bands in VR.
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                // Standard orientation: a1 is dark and h1 is light.
                renderer.sharedMaterial = (fileIndex + rank) % 2 == 1 ? darkSquareMaterial : lightSquareMaterial;

                SquareView squareView = squareObject.AddComponent<SquareView>();
                squareView.Initialize(square);
                squares.Add(squareView);
            }
        }
        boardBuilt = true;
        BoardScaleHandles.Build(this);
    }

    public void SyncPieces(IEnumerable<VisualPieceState> states, PieceFactory factory)
    {
        EnsureRoots();
        ClearChildren(piecesRoot);
        pieces.Clear();

        foreach (VisualPieceState state in states)
        {
            PieceView piece = factory.CreatePiece(state, GetPieceWorldPosition(state.Square), piecesRoot);
            pieces.Add(piece);
        }

        RefreshCheckAura();
    }

    public void HighlightSquares(IEnumerable<BoardSquare> highlightedSquares, IEnumerable<BoardSquare> capturableSquares)
    {
        EnsureRoots();
        ClearChildren(highlightsRoot);

        foreach (BoardSquare square in highlightedSquares)
        {
            GameObject highlight = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            highlight.name = $"Highlight {square.ToAlgebraic()}";
            highlight.transform.SetParent(highlightsRoot);
            highlight.transform.position = transform.TransformPoint(LocalSquarePosition(square, 0.095f));
            highlight.transform.localRotation = Quaternion.identity;
            highlight.transform.localScale = new Vector3(squareSize * 0.28f, 0.014f, squareSize * 0.28f);

            Collider collider = highlight.GetComponent<Collider>();
            if (Application.isPlaying)
            {
                Object.Destroy(collider);
            }
            else
            {
                Object.DestroyImmediate(collider);
            }
            if (highlightMaterial != null)
            {
                highlight.GetComponent<Renderer>().sharedMaterial = highlightMaterial;
            }
        }

        foreach (BoardSquare square in capturableSquares)
        {
            AddCaptureAura(square);
        }
    }

    private void AddCaptureAura(BoardSquare square)
    {
        PieceView target = FindPieceAt(square);
        if (target != null)
        {
            CreateAura(target, CaptureAuraMaterialPath, $"CaptureAura {square.ToAlgebraic()}", highlightsRoot);
        }
    }

    private PieceView FindPieceAt(BoardSquare square)
    {
        foreach (PieceView piece in pieces)
        {
            if (piece.Square.Equals(square))
            {
                return piece;
            }
        }

        return null;
    }

    private GameObject CreateAura(PieceView target, string materialPath, string auraName, Transform parent)
    {
        Material aura = Resources.Load<Material>(materialPath);
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
        if (aura == null || renderers.Length == 0)
        {
            return null;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        GameObject auraObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        auraObject.name = auraName;
        auraObject.transform.SetParent(parent);
        auraObject.transform.position = bounds.center;
        auraObject.transform.localRotation = Quaternion.identity;
        auraObject.transform.localScale = bounds.size * AuraSizeMultiplier / transform.lossyScale.x;

        Collider collider = auraObject.GetComponent<Collider>();
        if (Application.isPlaying)
        {
            Object.Destroy(collider);
        }
        else
        {
            Object.DestroyImmediate(collider);
        }

        Renderer renderer = auraObject.GetComponent<Renderer>();
        renderer.sharedMaterial = aura;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return auraObject;
    }

    private void RefreshCheckAura()
    {
        checkAuraRoot = EnsureChildRoot(checkAuraRoot, "CheckAura");
        ClearChildren(checkAuraRoot);
        checkAuraTarget = null;

        if (!checkSquare.HasValue)
        {
            return;
        }

        PieceView king = FindPieceAt(checkSquare.Value);
        if (king != null && CreateAura(king, CheckAuraMaterialPath, $"CheckAura {checkSquare.Value.ToAlgebraic()}", checkAuraRoot) != null)
        {
            checkAuraTarget = king;
        }
    }

    private void LateUpdate()
    {
        if (checkAuraTarget == null || checkAuraRoot == null || checkAuraRoot.childCount == 0)
        {
            return;
        }

        Renderer[] renderers = checkAuraTarget.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            return;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        checkAuraRoot.GetChild(0).position = bounds.center;
    }

    public void MarkLastMove(BoardSquare from, BoardSquare to)
    {
        lastMoveFrom = from;
        lastMoveTo = to;
        RefreshSquareTints();
    }

    public void MarkCheck(BoardSquare? king)
    {
        checkSquare = king;
        RefreshSquareTints();
        RefreshCheckAura();
    }

    private void RefreshSquareTints()
    {
        var properties = new MaterialPropertyBlock();
        foreach (SquareView squareView in squares)
        {
            BoardSquare square = squareView.Square;
            Renderer renderer = squareView.GetComponent<Renderer>();
            bool inCheck = checkSquare.HasValue && checkSquare.Value.Equals(square);
            bool lastMove = (lastMoveFrom.HasValue && lastMoveFrom.Value.Equals(square)) ||
                (lastMoveTo.HasValue && lastMoveTo.Value.Equals(square));
            if (!inCheck && !lastMove)
            {
                renderer.SetPropertyBlock(null);
                continue;
            }

            Material material = renderer.sharedMaterial;
            Color baseColor = material != null && material.HasProperty(BaseColorId) ? material.GetColor(BaseColorId) : Color.white;
            // The king in check wins over the last-move tint on the same square.
            Color tinted = inCheck ? Color.Lerp(baseColor, CheckTint, CheckWeight) : Color.Lerp(baseColor, LastMoveTint, LastMoveWeight);
            properties.SetColor(BaseColorId, tinted);
            renderer.SetPropertyBlock(properties);
        }
    }

    public void ClearHighlights()
    {
        // Clearing may be called during teardown; it must never create roots.
        if (highlightsRoot != null) ClearChildren(highlightsRoot);
    }

    private void ConfigureBoardTransformForMode()
    {
        configuredForHeadset = XRRig.IsHeadsetPresent;
        if (configuredForHeadset)
        {
            appliedVrSize = vrSize;
            // Grow around the bottom of the frame, which remains in contact with the tabletop.
            sizeLift = Vector3.up * (-frameLocalBounds.min.y * vrBoardScale.y * (vrSize - 1f));
            transform.localPosition = vrBoardPosition + Vector3.up * surfaceOffset + sizeLift;
            transform.localScale = vrBoardScale * vrSize;
        }
        else
        {
            appliedVrSize = 1f;
            sizeLift = Vector3.zero;
            transform.localPosition = desktopBoardPosition + Vector3.up * surfaceOffset;
            transform.localScale = desktopBoardScale;
        }
    }

    private void MeasureFrameBounds()
    {
        bool hasPoint = false;
        foreach (MeshFilter filter in boardFrameRoot.GetComponentsInChildren<MeshFilter>())
        {
            if (filter.sharedMesh == null) continue;
            Bounds mesh = filter.sharedMesh.bounds;
            Matrix4x4 toBoard = transform.worldToLocalMatrix * filter.transform.localToWorldMatrix;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = mesh.center + Vector3.Scale(mesh.extents,
                    new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                Vector3 point = toBoard.MultiplyPoint3x4(corner);
                if (!hasPoint) { frameLocalBounds = new Bounds(point, Vector3.zero); hasPoint = true; }
                else frameLocalBounds.Encapsulate(point);
            }
        }
    }

    private void EnsureRoots()
    {
        boardFrameRoot = EnsureChildRoot(boardFrameRoot, "BoardFrame");
        squaresRoot = EnsureChildRoot(squaresRoot, "Squares");
        piecesRoot = EnsureChildRoot(piecesRoot, "Pieces");
        highlightsRoot = EnsureChildRoot(highlightsRoot, "Highlights");
    }

    private void EnsureTurnIndicator()
    {
        if (turnIndicator != null)
        {
            return;
        }

        Transform root = EnsureChildRoot(null, "TurnIndicator");
        turnIndicator = root.GetComponent<TurnIndicatorView>();
        if (turnIndicator == null)
        {
            turnIndicator = root.gameObject.AddComponent<TurnIndicatorView>();
            turnIndicator.Build(squareSize);
        }
    }

    private void EnsureCapturedPieces()
    {
        if (capturedPieces != null)
        {
            return;
        }

        Transform root = EnsureChildRoot(null, "CapturedPieces");
        capturedPieces = root.GetComponent<CapturedPiecesView>();
        if (capturedPieces == null)
        {
            capturedPieces = root.gameObject.AddComponent<CapturedPiecesView>();
            capturedPieces.Build(this);
        }
    }

    private void EnsureSounds()
    {
        if (sounds != null)
        {
            return;
        }

        Transform root = EnsureChildRoot(null, "Sounds");
        sounds = root.GetComponent<BoardSounds>();
        if (sounds == null)
        {
            sounds = root.gameObject.AddComponent<BoardSounds>();
        }
    }

    private void BuildBoardFrame()
    {
        float boardWidth = squareSize * 8f;
        GameObject framePrefab = Resources.Load<GameObject>("Environment/ChessBoardFrame");
        if (framePrefab != null)
        {
            GameObject frame = Object.Instantiate(framePrefab, boardFrameRoot, false);
            frame.name = "BoardBase";
            float horizontalScale = squareSize / 1.25f;
            frame.transform.localScale = new Vector3(horizontalScale, 1f, horizontalScale);
            return;
        }

        Material rimMaterial = darkSquareMaterial != null ? darkSquareMaterial : lightSquareMaterial;
        Material baseMaterial = lightSquareMaterial != null ? lightSquareMaterial : darkSquareMaterial;

        GameObject baseObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ConfigureDecorativePart(
            baseObject,
            "BoardBase",
            new Vector3(0f, -0.29f, 0f),
            new Vector3(boardWidth + 1.28f, 0.5f, boardWidth + 1.28f),
            baseMaterial);

        GameObject rimObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ConfigureDecorativePart(
            rimObject,
            "OuterRim",
            new Vector3(0f, -0.015f, 0f),
            new Vector3(boardWidth + 0.85f, 0.08f, boardWidth + 0.85f),
            rimMaterial);
    }

    private void ConfigureDecorativePart(GameObject part, string name, Vector3 localPosition, Vector3 localScale, Material material)
    {
        part.name = name;
        part.transform.SetParent(boardFrameRoot);
        part.transform.localPosition = localPosition;
        part.transform.localRotation = Quaternion.identity;
        part.transform.localScale = localScale;

        if (material != null)
        {
            part.GetComponent<Renderer>().sharedMaterial = material;
        }

        Collider collider = part.GetComponent<Collider>();
        if (Application.isPlaying)
        {
            Object.Destroy(collider);
        }
        else
        {
            Object.DestroyImmediate(collider);
        }
    }

    private Transform EnsureChildRoot(Transform current, string rootName)
    {
        if (current != null)
        {
            return current;
        }

        Transform existing = transform.Find(rootName);
        if (existing != null)
        {
            return existing;
        }

        GameObject root = new GameObject(rootName);
        root.transform.SetParent(transform);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;
        return root.transform;
    }

    private static void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            GameObject child = parent.GetChild(i).gameObject;
            if (Application.isPlaying)
            {
                Object.Destroy(child);
            }
            else
            {
                Object.DestroyImmediate(child);
            }
        }
    }
}
