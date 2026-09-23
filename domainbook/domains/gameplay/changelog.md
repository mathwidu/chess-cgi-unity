# Changelog

O que mudou no contexto de gameplay, lançamento mais recente primeiro, no
formato [Keep a Changelog](https://keepachangelog.com/en/1.1.0/): um H2 por
lançamento, escrito como "## [1.2.0] - 2026-06-30" com " [YANKED]" ao final
se o lançamento foi retirado, contendo Added, Changed, Deprecated, Removed,
Fixed ou Security como H3s, cada um deles uma lista de itens.

## [Unreleased]

### Added

- `ChessGameController.SetPerformanceMode` liga ou desliga o modo desempenho:
  ajusta a fábrica de peças e, com uma partida em andamento, refaz as peças na
  hora com `BoardView.SyncPieces` para trocar peças personalizadas por peças
  clássicas sem reiniciar o jogo. A escolha é lida na inicialização e persistida
  com `PlayerPrefs`; o padrão é ligado, e o jogador pode desmarcá-lo no menu
  para voltar às peças personalizadas.
