using System.Collections.Generic;
using UnityEngine;

public sealed class BoardView : MonoBehaviour
{
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

    public float SquareSize => squareSize;
    public float PieceBaseHeight => pieceBaseHeight;
    public IReadOnlyList<SquareView> Squares => squares;
    public IReadOnlyList<PieceView> Pieces => pieces;
    public int HighlightCount => highlightsRoot == null ? 0 : highlightsRoot.childCount;
    public Transform BoardFrameRoot => boardFrameRoot;

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

        BuildBoardFrame();

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
                renderer.sharedMaterial = (fileIndex + rank) % 2 == 0 ? darkSquareMaterial : lightSquareMaterial;

                SquareView squareView = squareObject.AddComponent<SquareView>();
                squareView.Initialize(square);
                squares.Add(squareView);
            }
        }
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
    }

    public void HighlightSquares(IEnumerable<BoardSquare> highlightedSquares)
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
    }

    public void ClearHighlights()
    {
        EnsureRoots();
        ClearChildren(highlightsRoot);
    }

    private void ConfigureBoardTransformForMode()
    {
        if (XRRig.IsHeadsetPresent)
        {
            transform.localPosition = vrBoardPosition + Vector3.up * surfaceOffset;
            transform.localScale = vrBoardScale;
        }
        else
        {
            transform.localPosition = desktopBoardPosition + Vector3.up * surfaceOffset;
            transform.localScale = desktopBoardScale;
        }
    }

    private void EnsureRoots()
    {
        boardFrameRoot = EnsureChildRoot(boardFrameRoot, "BoardFrame");
        squaresRoot = EnsureChildRoot(squaresRoot, "Squares");
        piecesRoot = EnsureChildRoot(piecesRoot, "Pieces");
        highlightsRoot = EnsureChildRoot(highlightsRoot, "Highlights");
    }

    private void BuildBoardFrame()
    {
        float boardWidth = squareSize * 8f;
        Material rimMaterial = darkSquareMaterial != null ? darkSquareMaterial : lightSquareMaterial;
        Material baseMaterial = lightSquareMaterial != null ? lightSquareMaterial : darkSquareMaterial;

        GameObject baseObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ConfigureDecorativePart(
            baseObject,
            "BoardBase",
            new Vector3(0f, -0.08f, 0f),
            new Vector3(boardWidth + 0.5f, 0.12f, boardWidth + 0.5f),
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
