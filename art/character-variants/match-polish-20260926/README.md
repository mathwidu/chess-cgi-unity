# Interface da partida — experimento revertido

Estas capturas registram a proposta de HUD e indicadores avaliada em
26/09/2026 e depois revertida a pedido do usuário para evitar sobreposição
com o trabalho dos colegas. Não representam a interface ativa desta branch.

A restauração recuperou `GameHud.Match.cs`, `GameHud.cs`, `BoardView.cs` e
`ChessGameController.cs` ao estado anterior à experiência. Os arquivos
`GameHud.MatchState.cs`, `BoardView.Indicators.cs`, `MatchPresentationTests.cs`
e `MatchVisualReviewCapture.cs`, com seus metadados, foram retirados do Unity.

As melhorias anteriores dos personagens, cenário, tabuleiro, câmera e
preview da peça permanecem. Veja o [índice atual](../README.md) e o
[guia de integração](../../../docs/visual-feevale.md).
