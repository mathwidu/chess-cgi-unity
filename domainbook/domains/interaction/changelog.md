# Changelog

O que mudou no contexto de interação, lançamento mais recente primeiro, no
formato [Keep a Changelog](https://keepachangelog.com/en/1.1.0/): um H2 por
lançamento, escrito como "## [1.2.0] - 2026-06-30" com " [YANKED]" ao final
se o lançamento foi retirado, contendo Added, Changed, Deprecated, Removed,
Fixed ou Security como H3s, cada um deles uma lista de itens.

## [Unreleased]

### Added

- Pacotes de XR (XR Plugin Management, OpenXR Plugin, XR Interaction Toolkit)
  adicionados ao projeto para a fase de conversão para VR do HTC Vive; veja
  [ADR-0001](decisions/0001-use-openxr-and-the-xr-interaction-toolkit-for-vr-mode.md).
- `XRRig` monta em código um XR Origin (VR) em tempo de execução quando um
  headset está presente, de modo que a câmera do olho rastreia a pose do
  headset em vez do `CameraController` de desktop; veja
  [play-in-vr](features/play-in-vr.md). O modo desktop permanece inalterado
  quando nenhum headset está presente.
- `XRRig` monta um Near-Far Interactor com um raio visível em cada controle de
  movimento, vinculado ao gatilho para selecionar. Um novo componente
  `VrSelectionBridge` transforma o evento de seleção de um XR Simple
  Interactable numa peça ou casa nas mesmas chamadas
  `ChessGameController.SelectPiece` / `SelectSquare` que o caminho do mouse de
  desktop faz; veja [play-in-vr](features/play-in-vr.md).
