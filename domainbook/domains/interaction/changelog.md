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
  headset está presente, já que o modo VR é de assento único.
  `CameraController` mantém a órbita (Q/E) e o zoom por scroll disponíveis
  em VR, mas passam a agir sobre o XR Origin do rig em vez da câmera do
  olho, dentro de uma faixa de distância própria para a escala de VR; veja
  [play-in-vr](features/play-in-vr.md). O modo desktop permanece
  inalterado.
- Pacote `com.unity.xr.hands` e a feature OpenXR **Hand Tracking Subsystem**
  habilitados para Standalone. `XRRig` constrói um interactor de mão para
  cada lado, extraído das amostras oficiais do XR Interaction Toolkit
  (Starter Assets + Hands Interaction Demo), e um `XRInputModalityManager`
  alterna automaticamente entre esses interactors e os de controle
  existentes conforme o que o runtime relata como presente; veja
  [play-in-vr](features/play-in-vr.md) e
  [rastreamento de mãos](glossary.md).

### Changed

- O assento de VR foi reposicionado para uma vista de mesa: `XRRig` senta o
  jogador logo à frente e acima de um tabuleiro em escala de mesa, em vez de
  colocá-lo dentro de um tabuleiro em escala de sala, e `CameraController` teve
  as faixas de distância e o alvo de órbita/zoom reajustados para essa escala.
  O modo desktop permanece inalterado. Veja
  [ADR-0002](decisions/0002-ver-o-tabuleiro-como-uma-mesa-a-partir-de-um-assento.md).

### Fixed

- O [raio de seleção](glossary.md) dos controles voltou a ser desenhado. Ele
  passou a usar um `CurveVisualController` (com `LineRenderer`) em vez de um
  `XRInteractorLineVisual`, que exigia um componente `ILineRenderable` que o
  Near-Far Interactor não fornece e enchia o console com erros de
  "Missing ILineRenderable".
