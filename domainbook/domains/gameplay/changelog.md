# Changelog

O que mudou no contexto de gameplay, lançamento mais recente primeiro, no
formato [Keep a Changelog](https://keepachangelog.com/en/1.1.0/): um H2 por
lançamento, escrito como "## [1.2.0] - 2026-06-30" com " [YANKED]" ao final
se o lançamento foi retirado, contendo Added, Changed, Deprecated, Removed,
Fixed ou Security como H3s, cada um deles uma lista de itens.

## [Unreleased]

### Added

- `ChessGameController` expõe `LastMove` e `CheckedKing` (a casa do rei que o
  último lance deixou em xeque) e dispara `MoveApplied` uma vez por lance,
  depois que ele pousa e o estado da partida (turno, xeque, resultado) foi
  atualizado. `ReleasePiece` passa a devolver se a peça solta foi aceita
  (lance feito ou seleção desfeita na própria casa) ou recusada.
  `ChessRulesAdapter.FindKing` localiza o rei de um lado. Nenhuma regra de
  xadrez mudou.

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
- [Capturas e vantagem de material](glossary.md):
  - `ChessRulesAdapter` informa em `MoveResult.Captured` a peça tomada, com a
    casa onde ela estava; no en passant, é a casa do peão ao lado.
  - `ChessRulesAdapter.GetMaterialBalance` calcula a diferença de material no
    tabuleiro, com os valores de `ChessPieceValue`.
  - `ChessGameController` guarda `CapturedPieces` em ordem e mantém
    `MaterialBalance` atualizado a cada lance; os dois zeram numa nova
    partida. `IsAnimatingMove` informa quando o lance ainda está se movendo.
  - Nenhuma regra de xadrez mudou.
- [Resultado da partida](glossary.md): `ChessRulesAdapter` classifica a
  jogada que encerra a partida em `MatchOutcome` (xeque-mate, afogamento,
  material insuficiente ou outro empate) e o devolve em `MoveResult.Outcome`.
  `ChessGameController` expõe `Outcome` e `Winner` (o lado que deu o
  xeque-mate; nulo no empate), e `IsGameOver` passa a derivar de `Outcome`.
  Uma nova partida volta a `InProgress` sem vencedor. O status do empate nomeia
  o motivo ("Empate por afogamento.", "Empate por material insuficiente.").
  Nenhuma regra de xadrez mudou.

### Changed

- Organização da IA para revisão: dificuldade e resultado em arquivos próprios; inicialização UCI, configuração e leitura da resposta em rotinas nomeadas; observação de tarefas tardias usa `Task`. Contratos, perfis e cancelamento preservados.
- A busca do executável do Stockfish passa a olhar primeiro a pasta do jogo (onde está o `XadrezCGI.exe`), antes de `persistentDataPath/Engines` e do PATH; assim uma build de Windows roda com o `stockfish.exe` ao lado do jogo, sem instalação.

### Fixed

- Reinício durante a animação não aplica o resultado da partida anterior.
