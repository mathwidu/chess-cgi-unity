---
id: gameplay
name: Gameplay
classification:
  domain: core-domain
  business-model: engagement-creator
  evolution: custom-built
owners: [mathwidu]
code:
  - game/Assets/Scripts/Rules/**
  - game/Assets/Scripts/Domain/**
  - game/Assets/Scripts/Controllers/ChessGameController.cs
---

## Purpose

Rodar uma partida legal de xadrez entre dois jogadores compartilhando uma
tela. Este contexto é dono da partida: de quem é o turno, quais jogadas são
legais e se uma jogada termina em xeque, xeque-mate ou empate. É a razão de
existir do produto; tudo o que é visual está a seu serviço.

## Domain Roles

- Contexto de execução: `ChessGameController` conduz a partida — seleciona
  uma peça, pede os destinos legais, joga uma jogada e transforma o
  resultado em uma linha de status e um sinal para a câmera.
- Contexto de modelo: os value types de `Domain/` (`BoardSquare`,
  `ChessSide`, `ChessPieceKind`, `VisualPieceState`, `MoveResult`) são as
  palavras que o resto do jogo fala, então nenhuma view ou input handler
  toca a biblioteca de regras.
- Contexto anticorrupção: `ChessRulesAdapter` é o único código que sabe que a
  biblioteca `ChessDotNet` existe; ele traduz entre os tipos da biblioteca e
  os próprios tipos deste contexto.

## Inbound Communication

| Message              | Collaborator | Type    |
| --------------------- | ------------ | ------- |
| `SelectPiece`        | interaction  | Command |
| `SelectDestination`  | interaction  | Command |
| `ChoosePromotion`    | presentation | Command |
| `NewGame`            | interaction, presentation | Command |
| `GetLegalDestinations` | interaction | Query   |

## Outbound Communication

| Message             | Collaborator | Type    |
| ------------------- | ------------ | ------- |
| `PiecesChanged`     | presentation | Event   |
| `HighlightSquares`  | presentation | Command |
| `TurnChanged`       | interaction  | Event   |
| `StatusChanged`     | presentation | Event   |

## Business Decisions

- Legalidade, xeque, xeque-mate e empates vêm da biblioteca `ChessDotNet`,
  acessada somente por `ChessRulesAdapter` (`gameplay/ADR-0001`).
- O tabuleiro é 8×8 com um índice de coluna de 0–7 e uma linha de 1–8; um
  `BoardSquare` recusa qualquer coordenada fora desse intervalo em vez de
  ajustá-la ao limite.
- Uma partida roda por vez e é local: dois jogadores se revezam na mesma
  máquina, Brancas depois Pretas, sem adversário de IA e sem rede.
- Uma jogada só é oferecida se as regras a devolverem como legal; o
  controlador nunca inventa um destino que a biblioteca não permitiu.
- Um peão que chega à última linha pausa o turno para uma escolha de
  promoção antes de a jogada ser confirmada.

## Assumptions

- Os dois jogadores cooperam e compartilham a entrada; não há bloqueio por
  lado.
- A biblioteca de regras está correta sobre o xadrez; este contexto não a
  reverifica.
- Uma jogada ou é totalmente bem-sucedida ou deixa a partida intocada — não
  existe jogada parcial para desfazer.

## Verification Metrics

- Jogadas que o controlador oferece e que a biblioteca de regras rejeitaria
  — deve ficar em zero, porque o conjunto oferecido é construído a partir da
  própria resposta da biblioteca.
- Turnos em que o status relatado (xeque, xeque-mate, empate ou turno comum)
  não corresponde à visão da biblioteca sobre a posição.

## Open Questions

Nenhuma.
