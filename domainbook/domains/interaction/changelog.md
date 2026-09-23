# Changelog

O que mudou no contexto de interação, lançamento mais recente primeiro, no
formato [Keep a Changelog](https://keepachangelog.com/en/1.1.0/): um H2 por
lançamento, escrito como "## [1.2.0] - 2026-06-30" com " [YANKED]" ao final
se o lançamento foi retirado, contendo Added, Changed, Deprecated, Removed,
Fixed ou Security como H3s, cada um deles uma lista de itens.

## [Unreleased]

### Added

- Pacotes de XR (XR Plugin Management, OpenXR Plugin, XR Interaction Toolkit)
  resolvidos no projeto para a fase de conversão para VR no HTC Vive; veja
  [ADR-0001](decisions/0001-usar-openxr-e-o-xr-interaction-toolkit-para-o-modo-vr.md).
- `XRRig` constrói um XR Origin (VR) em código, em tempo de execução, quando
  um headset está presente, para que a câmera do olho rastreie a pose do
  headset em vez do `CameraController` de desktop; veja
  [play-in-vr](features/play-in-vr.md). O modo desktop permanece inalterado
  quando nenhum headset está presente.
- `XRRig` constrói um Near-Far Interactor com um raio visível em cada
  controle de movimento, vinculado ao gatilho para selecionar. Um novo
  componente `VrSelectionBridge` transforma o evento de seleção de um XR
  Simple Interactable em uma peça ou casa nas mesmas chamadas
  `ChessGameController.SelectPiece` / `SelectSquare` que o caminho de mouse
  de desktop faz; veja [play-in-vr](features/play-in-vr.md).
- `ChessGameController` aposenta o giro de câmera por turno quando um
  headset está presente, já que o modo VR é de assento único. A órbita
  (Q/E) e o zoom por scroll continuam disponíveis em VR, agora de posse do
  `XRRig`: agem sobre o XR Origin do rig, ao redor do tabuleiro e dentro de
  uma faixa de distância própria da escala de VR. O `CameraController` passa
  a ser exclusivo do desktop; veja [play-in-vr](features/play-in-vr.md).
- Pacote `com.unity.xr.hands` e a feature OpenXR **Hand Tracking Subsystem**
  habilitados para Standalone. `XRRig` constrói um interactor de mão para
  cada lado, extraído das amostras oficiais do XR Interaction Toolkit
  (Starter Assets + Hands Interaction Demo), e um `XRInputModalityManager`
  alterna automaticamente entre esses interactors e os de controle
  existentes conforme o que o runtime relata como presente; veja
  [play-in-vr](features/play-in-vr.md) e
  [rastreamento de mãos](glossary.md).
- O **Oculus Touch Controller Profile** foi habilitado para Standalone ao lado
  do HTC Vive Controller Profile, então uma mesma build detecta e usa o
  dispositivo presente automaticamente — Vive pelo runtime OpenXR do SteamVR,
  Oculus Rift pelo runtime da Oculus ou pelo SteamVR — sem caminho de código
  por fabricante; veja
  [ADR-0001](decisions/0001-usar-openxr-e-o-xr-interaction-toolkit-para-o-modo-vr.md).
  O Rift usa os [controles de movimento](glossary.md) Touch (raio de seleção e
  gatilho), não rastreamento de mãos articulado, que a Rift não possui.
- Cada [controle de movimento](glossary.md) carrega um modelo de mão
  (`Resources/XR/LeftControllerHand` / `RightControllerHand`) que segue a
  pose do controle, para que o jogador veja as mãos mesmo sem rastreamento
  de mãos articulado, como na Rift com os Touch. Os prefabs são gerados por
  **Chess CGI → VR → Create Controller Hands And Ray Material**.
- No Oculus Rift, o jogador [agarra e solta](glossary.md) as peças com a mão em
  vez de apontar um raio. Segurar o grip do [controle de movimento](glossary.md)
  perto de uma peça da vez a agarra e mostra seus destinos legais; soltá-la
  sobre um destino legal faz a jogada, e soltá-la em qualquer outro lugar
  conta como jogada inválida e a peça volta à casa de origem. O
  `VrSelectionBridge` agora liga o evento de agarrar e soltar de um XR Grab
  Interactable a `ChessGameController.GrabPiece` / `ReleasePiece`; veja
  [play-in-vr](features/play-in-vr.md).

### Changed

- O assento de VR foi reposicionado para uma vista de mesa: `XRRig` senta o
  jogador logo à frente e acima de um tabuleiro em escala de mesa, em vez de
  colocá-lo dentro de um tabuleiro em escala de sala. O modo desktop permanece
  inalterado. Veja
  [ADR-0002](decisions/0002-ver-o-tabuleiro-como-uma-mesa-a-partir-de-um-assento.md).
- Os modos VR e desktop ficaram independentes na câmera e nos controles. O
  `CameraController` não age mais em VR (retorna cedo quando um headset está
  presente) e perdeu os campos de distância de VR, que migraram junto com a
  órbita/zoom para o `XRRig`. Voltou aos valores originais de terceira pessoa,
  para o tabuleiro em escala 1 na origem. A seleção de desktop faz raycast a
  partir da Main Camera e a de VR a partir da Eye Camera, cada modo com a sua
  câmera, de modo que mexer em um modo não afete o outro.

- O [raio de seleção](glossary.md) deixou de escolher peças e casas: os
  controles selecionam com o grip (não mais o gatilho) e só o alcance próximo
  agarra peças. O raio distante ficou só para o HUD, e só é desenhado quando
  aponta para ele; o gatilho continua clicando nos botões do HUD. As casas do
  tabuleiro deixaram de ser interactables, e os interactors de mão seguem a
  mesma regra. Removida a verificação `XRControllerVerification`, que testava
  o fluxo antigo de apontar e puxar o gatilho; o `XRGrabVerification` a
  substitui.
- Contra a IA, a câmera desktop permanece na perspectiva do lado humano.
- O controlador central bloqueia seleções humanas no turno do computador, independentemente de mouse ou raio VR.
- Agarrar uma peça com o [controle de movimento](glossary.md) passou do grip para o gatilho do indicador, o mesmo botão do clique de UI (o raio distante só age sobre o HUD, então os dois usos não se cruzam). Enquanto segura uma peça, o modelo de mão do controle fecha na pose de pinça, e reabre ao soltar; o `ControllerHandPose` faz essa transição sobre os ossos da mão.

### Fixed

- O [raio de seleção](glossary.md) dos controles voltou a ser desenhado. Ele
  passou a usar um `CurveVisualController` (com `LineRenderer`) em vez de um
  `XRInteractorLineVisual`, que exigia um componente `ILineRenderable` que o
  Near-Far Interactor não fornece e enchia o console com erros de
  "Missing ILineRenderable".
- Na build de Windows, os [controles de movimento](glossary.md) não apareciam
  e não interagiam com nada (nem raio, nem menus). O `XRRig` buscava o shader
  do [raio de seleção](glossary.md) com `Shader.Find`, mas nenhum material
  usava "Universal Render Pipeline/Unlit", então a build o removia;
  `new Material(null)` lançava uma exceção no meio da construção do rig, e o
  rig tentava de novo a cada frame. O raio agora usa o material
  `Resources/XR/ControllerRayMaterial`, que entra na build, e o `XRRig`
  marca o rig como construído antes de montá-lo, para que uma falha não o
  reconstrua a cada frame.
- Cliques sobre a interface não selecionam peças por trás do HUD; o atalho N fica inativo no menu.
