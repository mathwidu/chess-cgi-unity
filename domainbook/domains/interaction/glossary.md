# Interaction glossary

The words the interaction context uses for input and the camera. A term is an H2
heading with its definition below it.

## Selection

The piece a player has picked to move. Picking a piece shows its legal
destinations; picking one of them, or pressing `Esc`, ends the selection.

- **Aliases:** Seleção, Peça selecionada
- **Status:** validated

## Perspective

The side the main camera faces. It follows the turn, swinging to the player on
move so each reads the board from their own end.

- **Aliases:** Câmera por turno, Per-turn camera
- **Status:** validated

## Orbit

Rotating the main camera around the board with `Q` and `E`, independently of
whose turn it is.

- **Aliases:** Girar câmera
- **Status:** validated

## VR mode

The game rendered to a headset and controlled with motion controllers instead of
on a monitor with mouse and keyboard. A proposed conversion target for HTC Vive
and Meta Quest 3, not yet built.

- **Aliases:** Modo VR, Immersive mode
- **Status:** draft

## Headset

The head-mounted display the player wears in VR mode. Its tracked pose drives the
camera, so the view follows where the player looks rather than swinging to the
side on move.

- **Aliases:** HMD, Óculos VR
- **Status:** draft

## Motion controller

A tracked hand controller the player holds in VR mode, used to point at and pick
pieces in place of the mouse.

- **Aliases:** Controle, Hand controller
- **Status:** draft

## Ray interactor

A ray a motion controller casts and the player points at a piece or square to
select it, replacing the mouse's screen raycast.

- **Aliases:** Raio de seleção, Pointer ray
- **Status:** draft
