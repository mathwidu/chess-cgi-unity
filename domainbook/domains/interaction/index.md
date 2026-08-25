---
id: interaction
name: Interaction
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

Turn what a player does — a click, a key, the scroll wheel — into commands for
the match, and move the camera so the board reads from the active player's side.

## Domain Roles

- Input context: `InputController` reads the mouse and keyboard, raycasts a click
  into the scene, decides whether a piece or a square was hit, and sends the
  matching command to gameplay.
- Camera context: `CameraController` orbits and zooms the main view, and swings
  it to face the player whose move it is when the turn changes.

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

- A click is resolved by raycasting into the 3D scene and reading the
  presentation view it hits — a `PieceView` means select that piece, a
  `SquareView` means move there — so picking follows what the player sees.
- The camera reorients to the side of the player on move, because two players
  share one screen and each should read the board from their own end.
- Keys are fixed and few: `Esc` cancels, `N` starts a new game, `Q` and `E`
  orbit, the wheel zooms — no rebindable input layer.

## Assumptions

- Exactly one main camera does the picking, and its view matches what the player
  sees.
- The player uses a mouse and keyboard; there is no touch or gamepad path.
- Presentation's colliders on pieces and squares are what a ray can hit; input
  reads them rather than guessing a board position from screen coordinates.

## Verification Metrics

- Clicks that hit a piece or square but reach gameplay as the wrong command, or
  as none — should stay at zero.
- Turn changes where the camera does not end facing the player on move.

## Open Questions

None.
