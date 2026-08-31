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
  - game/Assets/Scripts/Controllers/XRRig.cs
  - game/Assets/Scripts/Controllers/VrSelectionBridge.cs
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
  turno muda. No modo VR, esse giro por turno é aposentado — o modo VR é de
  assento único — mas a órbita e o zoom continuam disponíveis, agindo sobre
  o XR Origin do rig de VR em vez da câmera do olho, dentro de uma faixa de
  distância própria para a escala de VR; veja
  [play-in-vr](features/play-in-vr.md).
- Contexto do rig de VR: `XRRig` constrói um XR Origin em tempo de execução
  quando um headset está presente — a câmera do olho, seu Tracked Pose
  Driver e o controle de recentralização — e reaponta o `InputController`
  para a câmera do olho em vez da câmera de desktop. Também constrói um
  ray interactor em cada controle de movimento, vinculado ao gatilho para
  selecionar.
- Contexto de seleção em VR: `VrSelectionBridge` mapeia o evento de seleção
  de uma peça ou casa, vindo de um XR Simple Interactable, para os mesmos
  comandos `SelectPiece` / `SelectSquare` que o caminho de clique de
  desktop envia, para que o gameplay veja um único vocabulário de entrada
  independentemente do modo.

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
- Mudanças de turno, fora do modo VR, em que a câmera não termina voltada
  para o jogador a jogar.
- Mudanças de turno em modo VR que movem o XR Origin sem que o jogador
  tenha orbitado ou dado zoom — deve ficar em zero, já que o giro por turno
  é aposentado nesse modo.

## Open Questions

Nenhuma.
