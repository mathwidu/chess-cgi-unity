using System.Collections.Generic;
using UnityEngine;

public sealed class BoardView : MonoBehaviour
{
    [SerializeField] private float squareSize = 1.25f;
    [SerializeField] private float pieceBaseHeight = 0.08f;
    [SerializeField] private Transform squaresRoot;
    [SerializeField] private Transform piecesRoot;
    [SerializeField] private Transform highlightsRoot;
    [SerializeField] private Material lightSquareMaterial;
    [SerializeField] private Material darkSquareMaterial;
    [SerializeField] private Material highlightMaterial;

    private readonly List<SquareView> squares = new List<SquareView>();
    private readonly List<PieceView> pieces = new List<PieceView>();

    public float SquareSize => squareSize;
    public float PieceBaseHeight => pieceBaseHeight;
    public IReadOnlyList<SquareView> Squares => squares;
    public IReadOnlyList<PieceView> Pieces => pieces;
    public int HighlightCount => highlightsRoot == null ? 0 : highlightsRoot.childCount;

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

    public Vector3 GetWorldPosition(BoardSquare square)
    {
        float x = (square.FileIndex - 3.5f) * squareSize;
        float z = (square.Rank - 4.5f) * squareSize;
        return transform.TransformPoint(new Vector3(x, 0f, z));
    }

    public Vector3 GetPieceWorldPosition(BoardSquare square)
    {
        Vector3 position = GetWorldPosition(square);
        position.y += pieceBaseHeight;
        return position;
    }

    public void BuildBoard()
    {
        EnsureRoots();
        ClearChildren(squaresRoot);
        ClearChildren(highlightsRoot);
        squares.Clear();

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
            highlight.transform.position = GetWorldPosition(square) + new Vector3(0f, 0.08f, 0f);
            highlight.transform.localRotation = Quaternion.identity;
            highlight.transform.localScale = new Vector3(squareSize * 0.35f, 0.02f, squareSize * 0.35f);

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

    private void EnsureRoots()
    {
        squaresRoot = EnsureChildRoot(squaresRoot, "Squares");
        piecesRoot = EnsureChildRoot(piecesRoot, "Pieces");
        highlightsRoot = EnsureChildRoot(highlightsRoot, "Highlights");
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
