using UnityEngine;

public sealed class GameHud : MonoBehaviour
{
    [SerializeField] private ChessGameController gameController;

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

        GUILayout.BeginArea(new Rect(16f, 16f, 280f, 170f), GUI.skin.box);
        GUILayout.Label(gameController.StatusMessage);

        if (gameController.IsAwaitingPromotion)
        {
            GUILayout.Label("Promocao");
            GUILayout.BeginHorizontal();
            PromotionButton("Q", 'Q');
            PromotionButton("R", 'R');
            PromotionButton("B", 'B');
            PromotionButton("N", 'N');
            GUILayout.EndHorizontal();
        }

        if (GUILayout.Button("Nova partida (N)"))
        {
            gameController.NewGame();
        }

        GUILayout.EndArea();
    }

    private void PromotionButton(string label, char piece)
    {
        if (GUILayout.Button(label))
        {
            gameController.ChoosePromotion(piece);
        }
    }
}
