---
id: gameplay
name: Gameplay
classification:
  domain: core-domain
  business-model: engagement-creator
  evolution: custom-built
owners: [mathwidu]
code:
  - game/Assets/Scripts/Rules/**
  - game/Assets/Scripts/Domain/**
  - game/Assets/Scripts/Controllers/ChessGameController.cs
---

## Purpose

Run a legal game of chess between two players sharing one screen. This context
owns the match: whose turn it is, which moves are legal, and whether a move ends
in check, checkmate, or a draw. It is the reason the product exists; everything
visual serves it.

## Domain Roles

- Execution context: `ChessGameController` drives the match — it selects a piece,
  asks for legal destinations, plays a move, and turns the result into a status
  line and a camera cue.
- Model context: the `Domain/` value types (`BoardSquare`, `ChessSide`,
  `ChessPieceKind`, `VisualPieceState`, `MoveResult`) are the words the rest of
  the game speaks, so no view or input handler touches the rules library.
- Anticorruption context: `ChessRulesAdapter` is the only code that knows the
  `ChessDotNet` library exists; it translates between the library's types and
  this context's own.

## Inbound Communication

| Message              | Collaborator | Type    |
| -------------------- | ------------ | ------- |
| `SelectPiece`        | interaction  | Command |
| `SelectDestination`  | interaction  | Command |
| `ChoosePromotion`    | presentation | Command |
| `NewGame`            | interaction, presentation | Command |
| `GetLegalDestinations` | interaction | Query   |

## Outbound Communication

| Message             | Collaborator | Type    |
| ------------------- | ------------ | ------- |
| `PiecesChanged`     | presentation | Event   |
| `HighlightSquares`  | presentation | Command |
| `TurnChanged`       | interaction  | Event   |
| `StatusChanged`     | presentation | Event   |

## Business Decisions

- Legality, check, checkmate, and draws come from the `ChessDotNet` library,
  reached only through `ChessRulesAdapter` (`gameplay/ADR-0001`).
- The board is 8×8 with a file index of 0–7 and a rank of 1–8; a `BoardSquare`
  refuses any coordinate outside that range rather than clamping it.
- One game runs at a time and it is local: two players take turns on the same
  machine, White then Black, with no AI opponent and no network.
- A move is only offered if the rules return it as legal; the controller never
  invents a destination the library did not allow.
- A pawn reaching the far rank pauses the turn for a promotion choice before the
  move is committed.

## Assumptions

- Both players are cooperative and share the input; there is no per-side lockout.
- The rules library is correct about chess; this context does not re-check it.
- A move either fully succeeds or leaves the match untouched — there is no
  partial move to unwind.

## Verification Metrics

- Moves the controller offers that the rules library would reject — should stay
  at zero, because the offered set is built from the library's own answer.
- Turns where the reported status (check, checkmate, draw, or plain turn) does
  not match the library's view of the position.

## Open Questions

None.
