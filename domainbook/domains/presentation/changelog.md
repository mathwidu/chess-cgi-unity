# Changelog

O que mudou no contexto de apresentação, lançamento mais recente primeiro, no
formato [Keep a Changelog](https://keepachangelog.com/en/1.1.0/): um H2 por
lançamento, escrito como "## [1.2.0] - 2026-06-30" com " [YANKED]" ao final
se o lançamento foi retirado, contendo Added, Changed, Deprecated, Removed,
Fixed ou Security como H3s, cada um deles uma lista de itens.

## [Unreleased]

### Added

- `BoardView` and `PieceFactory` add an XR Simple Interactable and a
  `VrSelectionBridge` to each square and piece when a headset is present, so
  interaction's ray interactor can select them the same way the desktop
  raycast does. No change in desktop mode.
