using System.Collections.Generic;
using UnityEngine;

public sealed class GameHud : MonoBehaviour
{
    [SerializeField] private ChessGameController gameController;
    [SerializeField] private int visibleMoveCount = 6;

    private bool showStartScreen = true;
    private bool showHowToPlay;
    private GUIStyle titleStyle;
    private GUIStyle subtitleStyle;
    private GUIStyle statusStyle;
    private GUIStyle labelStyle;
    private GUIStyle smallLabelStyle;
    private GUIStyle panelStyle;
    private GUIStyle buttonStyle;
    private GUIStyle badgeStyle;

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

        EnsureStyles();
        DrawTopBar();
        DrawMoveHistoryPanel(gameController.MoveHistory);
        DrawActionBar();

        if (gameController.IsAwaitingPromotion)
        {
            DrawPromotionPanel();
        }

        if (showStartScreen)
        {
            DrawStartScreen();
        }
    }

    private void DrawTopBar()
    {
        GUILayout.BeginArea(new Rect(16f, 14f, Screen.width - 32f, 76f), panelStyle);
        GUILayout.BeginHorizontal();
        GUILayout.BeginVertical(GUILayout.Width(260f));
        GUILayout.Label("Xadrez CGI", titleStyle);
        GUILayout.Label("Computacao Grafica I", smallLabelStyle);
        GUILayout.EndVertical();

        GUILayout.FlexibleSpace();
        GUILayout.BeginVertical(GUILayout.Width(360f));
        GUILayout.Label(gameController.CurrentTurn == ChessSide.White ? "Turno: Brancas" : "Turno: Pretas", badgeStyle);
        GUILayout.Label(gameController.StatusMessage, statusStyle);
        GUILayout.EndVertical();
        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }

    private void DrawMoveHistoryPanel(IReadOnlyList<string> moveHistory)
    {
        Rect rect = new Rect(Screen.width - 290f, 106f, 274f, 260f);
        GUILayout.BeginArea(rect, panelStyle);
        GUILayout.Label("Historico", subtitleStyle);

        if (moveHistory.Count == 0)
        {
            GUILayout.Label("Nenhuma jogada ainda.", smallLabelStyle);
        }
        else
        {
            int startIndex = Mathf.Max(0, moveHistory.Count - visibleMoveCount);
            for (int i = startIndex; i < moveHistory.Count; i++)
            {
                GUILayout.Label($"{i + 1}. {moveHistory[i]}", labelStyle);
            }
        }

        GUILayout.EndArea();
    }

    private void DrawActionBar()
    {
        GUILayout.BeginArea(new Rect(16f, Screen.height - 82f, 470f, 66f), panelStyle);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Nova partida", buttonStyle, GUILayout.Width(145f), GUILayout.Height(42f)))
        {
            showStartScreen = false;
            gameController.StartLocalGame();
        }

        if (GUILayout.Button("Cancelar", buttonStyle, GUILayout.Width(125f), GUILayout.Height(42f)))
        {
            gameController.CancelSelection();
        }

        if (GUILayout.Button("Como jogar", buttonStyle, GUILayout.Width(145f), GUILayout.Height(42f)))
        {
            showHowToPlay = !showHowToPlay;
        }
        GUILayout.EndHorizontal();
        GUILayout.EndArea();

        if (showHowToPlay)
        {
            DrawHowToPlayPanel();
        }
    }

    private void DrawPromotionPanel()
    {
        GUILayout.BeginArea(CenteredRect(440f, 130f), panelStyle);
        GUILayout.Label("Promocao", subtitleStyle);
        GUILayout.Label("Escolha a nova peca do peao.", labelStyle);
        GUILayout.BeginHorizontal();
        PromotionButton("Rainha", 'Q');
        PromotionButton("Torre", 'R');
        PromotionButton("Bispo", 'B');
        PromotionButton("Cavalo", 'N');
        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }

    private void DrawStartScreen()
    {
        Color previousColor = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.52f);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = previousColor;

        GUILayout.BeginArea(CenteredRect(520f, showHowToPlay ? 430f : 310f), panelStyle);
        GUILayout.Label("Xadrez CGI", titleStyle);
        GUILayout.Label("Xadrez 3D local com personagens da turma.", statusStyle);
        GUILayout.Space(16f);

        if (GUILayout.Button("Jogar", buttonStyle, GUILayout.Height(48f)))
        {
            showStartScreen = false;
            showHowToPlay = false;
            gameController.StartLocalGame();
        }

        if (GUILayout.Button(showHowToPlay ? "Ocultar como jogar" : "Como jogar", buttonStyle, GUILayout.Height(42f)))
        {
            showHowToPlay = !showHowToPlay;
        }

        if (showHowToPlay)
        {
            GUILayout.Space(12f);
            DrawHowToPlayContent();
        }

        GUILayout.EndArea();
    }

    private void DrawHowToPlayPanel()
    {
        GUILayout.BeginArea(new Rect(16f, 98f, 360f, 210f), panelStyle);
        DrawHowToPlayContent();
        GUILayout.EndArea();
    }

    private void DrawHowToPlayContent()
    {
        GUILayout.Label("Como jogar", subtitleStyle);
        GUILayout.Label("Clique em uma peca do turno atual.", labelStyle);
        GUILayout.Label("Clique em uma casa destacada para mover.", labelStyle);
        GUILayout.Label("Q/E giram a camera. Scroll aproxima.", labelStyle);
        GUILayout.Label("Esc cancela selecao. N reinicia.", labelStyle);
    }

    private void PromotionButton(string label, char piece)
    {
        if (GUILayout.Button(label, buttonStyle, GUILayout.Height(38f)))
        {
            gameController.ChoosePromotion(piece);
        }
    }

    private void EnsureStyles()
    {
        if (titleStyle != null)
        {
            return;
        }

        panelStyle = new GUIStyle(GUI.skin.window)
        {
            padding = new RectOffset(16, 16, 14, 14)
        };

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 28,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.97f, 0.94f, 0.86f) }
        };

        subtitleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.95f, 0.9f, 0.78f) }
        };

        statusStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 15,
            normal = { textColor = new Color(0.95f, 0.95f, 0.9f) }
        };

        labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            normal = { textColor = new Color(0.9f, 0.9f, 0.84f) }
        };

        smallLabelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            normal = { textColor = new Color(0.75f, 0.76f, 0.72f) }
        };

        badgeStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 17,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleRight,
            normal = { textColor = new Color(0.97f, 0.84f, 0.52f) }
        };

        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold
        };
    }

    private static Rect CenteredRect(float width, float height)
    {
        return new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
    }
}
