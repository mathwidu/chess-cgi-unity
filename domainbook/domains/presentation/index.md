---
id: presentation
name: Presentation
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

Show the match. This context builds the 3D board, the pieces, the room around
them, and the Canvas HUD, and keeps all of it in step with the state gameplay
reports. It is what a player actually looks at and reads.

## Domain Roles

- View context: `BoardView` builds the squares and the frame, `PieceFactory`
  builds one object per piece, and both are rebuilt from a fresh list of pieces
  after every move rather than mutated in place.
- Presentation context: `GameHud` builds the whole interface in code — the turn
  and status lines, the move history, the promotion prompt, the start screen,
  and the panel that inspects the selected piece in a small 3D preview.
- Identity context: each piece kind has a custom character model; when a model
  is missing, a piece is assembled from primitives instead so the board is never
  empty (`presentation/ADR-0001`).

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

- The board, the pieces, the room scenery, and the entire HUD are generated in
  code at runtime; the scene stores almost nothing, so a rebuild is the way to
  refresh, not an edit.
- A piece is a custom model per kind with a primitive fallback, so a missing
  prefab degrades to a recognisable shape instead of a hole
  (`presentation/ADR-0001`).
- The interface speaks Portuguese to the player; the code that builds it speaks
  the shared English vocabulary.
- The selected piece is shown in its own rendered preview so a player can read
  who the character is without hunting for it on the board.

## Assumptions

- Gameplay's list of pieces is the whole truth for a position; the view holds no
  state gameplay does not.
- Rebuilding the board and pieces each move is cheap enough at this scale to
  prefer over tracking and animating differences.
- A custom model may be any height, so it is scaled to fit a target size rather
  than trusted to arrive correctly sized.

## Verification Metrics

- Frames where a shown piece disagrees with gameplay's reported position —
  should stay at zero, because the view is rebuilt from that list.
- Piece kinds with neither a custom model nor a primitive fallback rendering —
  should never happen; the fallback covers every kind.

## Open Questions

None.
