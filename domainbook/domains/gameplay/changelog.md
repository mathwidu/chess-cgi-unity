# Changelog

O que mudou no contexto de gameplay, lançamento mais recente primeiro, no
formato [Keep a Changelog](https://keepachangelog.com/en/1.1.0/): um H2 por
lançamento, escrito como "## [1.2.0] - 2026-06-30" com " [YANKED]" ao final
se o lançamento foi retirado, contendo Added, Changed, Deprecated, Removed,
Fixed ou Security como H3s, cada um deles uma lista de itens.

## [Unreleased]

### Added

- `ChessGameController` ganha `CanGrabPiece`, `GrabPiece` e `ReleasePiece`
  para o [agarrar e soltar](../interaction/glossary.md) do VR. Agarrar uma
  peça do lado do turno a seleciona; soltar sobre um destino legal faz a
  jogada pelo mesmo `SelectDestination` do clique, e soltar em qualquer outro
  lugar (fora dos destinos legais ou do tabuleiro) informa "Movimento
  invalido.", limpa a seleção e devolve a peça à casa de origem em vez de
  manter a peça selecionada como no clique. Soltar na própria casa apenas
  desfaz a seleção. Nenhuma regra de xadrez mudou.
- `ChessGameController.SetPerformanceMode` liga ou desliga o modo desempenho:
  ajusta a fábrica de peças e, com uma partida em andamento, refaz as peças na
  hora com `BoardView.SyncPieces` para trocar peças personalizadas por peças
  clássicas sem reiniciar o jogo. A escolha é lida na inicialização e persistida
  com `PlayerPrefs`; o padrão é ligado, e o jogador pode desmarcá-lo no menu
  para voltar às peças personalizadas.
- Adversário offline no desktop, com escolha de lado, três dificuldades e validação local de toda candidata.
- Cancelamento e timeout de busca, descarte de respostas antigas e recuperação por nova tentativa ou menu.

### Changed

- Organização da IA para revisão: dificuldade e resultado em arquivos próprios; inicialização UCI, configuração e leitura da resposta em rotinas nomeadas; observação de tarefas tardias usa `Task`. Contratos, perfis e cancelamento preservados.
- A busca do executável do Stockfish passa a olhar primeiro a pasta do jogo (onde está o `XadrezCGI.exe`), antes de `persistentDataPath/Engines` e do PATH; assim uma build de Windows roda com o `stockfish.exe` ao lado do jogo, sem instalação.

### Fixed

- Reinício durante a animação não aplica o resultado da partida anterior.
