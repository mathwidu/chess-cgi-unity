using System.Collections.Generic;
using UnityEngine;

public sealed class GameHud : MonoBehaviour
{
    [SerializeField] private ChessGameController gameController;
    [SerializeField] private int visibleMoveCount = 6;

    public void Configure(ChessGameController controller)
    {
        gameController = controller;
    }

    private void Awake()
    {
        if (gameController == null)
        {
            gameController = Object.FindFirstObjectByType<ChessGameController>();
        }
    }

    private void OnGUI()
    {
        if (gameController == null)
        {
            return;
        }

        GUILayout.BeginArea(new Rect(16f, 16f, 320f, 300f), GUI.skin.window);
        GUILayout.Label("Xadrez CGI");
        GUILayout.Label(gameController.StatusMessage);

        if (gameController.IsAwaitingPromotion)
        {
            GUILayout.Label("Promocao");
            GUILayout.BeginHorizontal();
            PromotionButton("Rainha", 'Q');
            PromotionButton("Torre", 'R');
            PromotionButton("Bispo", 'B');
            PromotionButton("Cavalo", 'N');
            GUILayout.EndHorizontal();
        }

        DrawMoveHistory(gameController.MoveHistory);

        if (GUILayout.Button("Nova partida"))
        {
            gameController.NewGame();
        }

        GUILayout.EndArea();
    }

    private void DrawMoveHistory(IReadOnlyList<string> moveHistory)
    {
        if (moveHistory.Count == 0)
        {
            return;
        }

        GUILayout.Space(8f);
        GUILayout.Label("Jogadas");

        int startIndex = Mathf.Max(0, moveHistory.Count - visibleMoveCount);
        for (int i = startIndex; i < moveHistory.Count; i++)
        {
            GUILayout.Label($"{i + 1}. {moveHistory[i]}");
        }
    }

    private void PromotionButton(string label, char piece)
    {
        if (GUILayout.Button(label))
        {
            gameController.ChoosePromotion(piece);
        }
    }
}
