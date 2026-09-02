---
id: presentation
name: Apresentação
classification:
  domain: supporting-domain
  business-model: engagement-creator
  evolution: custom-built
owners: [mathwidu]
code:
  - game/Assets/Scripts/View/**
  - game/Assets/Scripts/UI/**
relationships:
  - with: gameplay
    type: customer-supplier
    direction: downstream
    patterns: [CF]
---

## Purpose

Mostrar a partida. Este contexto constrói o tabuleiro 3D, as peças, a sala
ao redor delas e o HUD em Canvas, e mantém tudo isso em sincronia com o
estado que o gameplay relata. É o que um jogador realmente olha e lê.

## Domain Roles

- Contexto de view: `BoardView` constrói as casas e a moldura,
  `PieceFactory` constrói um objeto por peça, e ambos são reconstruídos a
  partir de uma lista nova de peças a cada jogada, em vez de sofrer
  mutação no lugar. No modo VR, ambos também adicionam um XR Simple
  Interactable e um `VrSelectionBridge` junto do collider já existente,
  para que o ray interactor da interação consiga selecionar o mesmo
  objeto que o raycast de desktop já atinge.
- Contexto de apresentação: `GameHud` constrói toda a interface em código
  — as linhas de turno e status, o histórico de jogadas, o pedido de
  promoção, a tela inicial e o painel que examina a peça selecionada em um
  pequeno preview 3D.
- Contexto de identidade: cada tipo de peça tem um modelo de personagem
  customizado; quando um modelo está ausente, uma peça é montada a partir
  de primitivas para que o tabuleiro nunca fique vazio
  (`presentation/ADR-0001`).

## Inbound Communication

| Message            | Collaborator | Type    |
| ------------------ | ------------ | ------- |
| `PiecesChanged`    | gameplay     | Event   |
| `HighlightSquares` | gameplay     | Command |
| `StatusChanged`    | gameplay     | Event   |
| `ShowSelectedPiece`| interaction  | Command |

## Outbound Communication

| Message           | Collaborator | Type    |
| ----------------- | ------------ | ------- |
| `NewGame`         | gameplay     | Command |
| `ChoosePromotion` | gameplay     | Command |
| `CancelSelection` | gameplay     | Command |

## Business Decisions

- O tabuleiro, as peças, a cenografia da sala e o HUD inteiro são gerados
  em código em tempo de execução; a cena guarda quase nada, então uma
  reconstrução é a forma de atualizar, não uma edição.
- Uma peça é um modelo customizado por tipo com uma alternativa primitiva,
  então um prefab ausente degrada para uma forma reconhecível em vez de um
  buraco (`presentation/ADR-0001`).
- A interface fala português com o jogador; o código que a constrói fala o
  vocabulário compartilhado em inglês.
- A peça selecionada é mostrada em seu próprio preview renderizado para que
  um jogador consiga ler quem é o personagem sem precisar procurá-lo no
  tabuleiro.

## Assumptions

- A lista de peças do gameplay é a verdade completa sobre uma posição; a
  view não guarda nenhum estado que o gameplay não tenha.
- Reconstruir o tabuleiro e as peças a cada jogada é barato o suficiente
  nessa escala para preferir isso a rastrear e animar diferenças.
- Um modelo customizado pode ter qualquer altura, então ele é escalado
  para caber em um tamanho alvo em vez de ser confiado como já vindo no
  tamanho correto.

## Verification Metrics

- Quadros em que uma peça mostrada diverge da posição relatada pelo
  gameplay — deve ficar em zero, porque a view é reconstruída a partir
  dessa lista.
- Tipos de peça sem modelo customizado nem alternativa primitiva sendo
  renderizada — nunca deve acontecer; a alternativa cobre todo tipo.

## Open Questions

Nenhuma.
