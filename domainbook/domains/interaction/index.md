---
id: interaction
name: Interação
classification:
  domain: supporting-domain
  business-model: engagement-creator
  evolution: custom-built
owners: [mathwidu]
code:
  - game/Assets/Scripts/Controllers/InputController.cs
  - game/Assets/Scripts/Controllers/CameraController.cs
relationships:
  - with: gameplay
    type: customer-supplier
    direction: downstream
    patterns: [CF]
  - with: presentation
    type: customer-supplier
    direction: downstream
    patterns: [CF]
---

## Purpose

Transformar o que um jogador faz — um clique, uma tecla, a roda do mouse —
em comandos para a partida, e mover a câmera para que o tabuleiro seja lido
do lado do jogador ativo.

## Domain Roles

- Contexto de entrada: `InputController` lê o mouse e o teclado, faz raycast
  de um clique na cena, decide se uma peça ou uma casa foi atingida, e envia
  o comando correspondente ao gameplay.
- Contexto de câmera: `CameraController` orbita e dá zoom na visão
  principal, e a gira para ficar voltada para o jogador a jogar quando o
  turno muda.

## Inbound Communication

| Message         | Collaborator | Type    |
| --------------- | ------------ | ------- |
| `PrimaryClick`  | player       | Command |
| `CancelKey`     | player       | Command |
| `NewGameKey`    | player       | Command |
| `OrbitKey`      | player       | Command |
| `ZoomWheel`     | player       | Command |
| `TurnChanged`   | gameplay     | Event   |

## Outbound Communication

| Message             | Collaborator | Type    |
| ------------------- | ------------ | ------- |
| `SelectPiece`       | gameplay     | Command |
| `SelectDestination` | gameplay     | Command |
| `CancelSelection`   | gameplay     | Command |
| `NewGame`           | gameplay     | Command |

## Business Decisions

- Um clique é resolvido fazendo raycast na cena 3D e lendo a view de
  apresentação que ele atinge — uma `PieceView` significa selecionar
  aquela peça, uma `SquareView` significa mover para lá — então a escolha
  segue o que o jogador vê.
- A câmera se reorienta para o lado do jogador a jogar, porque dois
  jogadores compartilham uma tela e cada um deve ler o tabuleiro a partir
  do seu próprio lado.
- As teclas são fixas e poucas: `Esc` cancela, `N` inicia uma nova partida,
  `Q` e `E` orbitam, a roda dá zoom — sem camada de entrada remapeável.

## Assumptions

- Exatamente uma câmera principal faz a seleção, e sua visão corresponde ao
  que o jogador vê.
- O jogador usa mouse e teclado; não há caminho de toque ou gamepad.
- Os colliders da apresentação nas peças e casas são o que um raio pode
  atingir; a entrada os lê em vez de tentar adivinhar uma posição do
  tabuleiro a partir de coordenadas de tela.

## Verification Metrics

- Cliques que atingem uma peça ou casa mas chegam ao gameplay como o
  comando errado, ou como nenhum — deve ficar em zero.
- Mudanças de turno em que a câmera não termina voltada para o jogador a
  jogar.

## Open Questions

Nenhuma.
