using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class CustomPieceCoverageTests
{
    [Test]
    public void Resources_AllCurrentCustomPrefabsExistAndHaveRenderer()
    {
        foreach (GameObject prefab in LoadCustomPrefabs().Values)
        {
            Assert.IsNotNull(prefab);
            Assert.Greater(prefab.GetComponentsInChildren<Renderer>(true).Length, 0, prefab.name);
        }
    }

    [Test]
    public void InitialBoard_UsesCustomVisualForEveryPiece()
    {
        GameObject rig = new GameObject("Custom Piece Coverage Test");
        try
        {
            BoardView board = rig.AddComponent<BoardView>();
            PieceFactory factory = rig.AddComponent<PieceFactory>();
            foreach (KeyValuePair<ChessPieceKind, GameObject> entry in LoadCustomPrefabs())
            {
                factory.ConfigureCustomPrefab(entry.Key, entry.Value);
            }

            ChessRulesAdapter rules = new ChessRulesAdapter();
            board.SyncPieces(rules.GetPieces(), factory);

            Assert.AreEqual(32, board.Pieces.Count);
            Assert.AreEqual(16, Count(board.Pieces, ChessPieceKind.Pawn));
            Assert.AreEqual(4, Count(board.Pieces, ChessPieceKind.Rook));
            Assert.AreEqual(4, Count(board.Pieces, ChessPieceKind.Knight));
            Assert.AreEqual(4, Count(board.Pieces, ChessPieceKind.Bishop));
            Assert.AreEqual(2, Count(board.Pieces, ChessPieceKind.Queen));
            Assert.AreEqual(2, Count(board.Pieces, ChessPieceKind.King));

            foreach (PieceView piece in board.Pieces)
            {
                Assert.IsNotNull(piece.VisualRoot, piece.name);
                Assert.AreEqual("CustomVisual", piece.VisualRoot.name, piece.name);
                Assert.IsNotNull(piece.VisualRoot.GetComponent<CharacterAnimationDriver>(), piece.name);
            }
        }
        finally
        {
            Object.DestroyImmediate(rig);
        }
    }

    private static Dictionary<ChessPieceKind, GameObject> LoadCustomPrefabs()
    {
        return new Dictionary<ChessPieceKind, GameObject>
        {
            { ChessPieceKind.Pawn, Load("Pawn_Mathwidu_v3b") },
            { ChessPieceKind.Rook, Load("Rook_Alex") },
            { ChessPieceKind.Knight, Load("Knight_Gustavo") },
            { ChessPieceKind.Bishop, Load("Bishop_Rafael") },
            { ChessPieceKind.Queen, Load("Queen_Marta") },
            { ChessPieceKind.King, Load("King_Ricardo_Carioca") }
        };
    }

    private static GameObject Load(string prefabName)
    {
        GameObject prefab = Resources.Load<GameObject>($"CustomPieces/{prefabName}");
        Assert.IsNotNull(prefab, prefabName);
        return prefab;
    }

    private static int Count(IReadOnlyList<PieceView> pieces, ChessPieceKind kind)
    {
        int count = 0;
        foreach (PieceView piece in pieces)
        {
            if (piece.Kind == kind)
            {
                count++;
            }
        }

        return count;
    }
}
