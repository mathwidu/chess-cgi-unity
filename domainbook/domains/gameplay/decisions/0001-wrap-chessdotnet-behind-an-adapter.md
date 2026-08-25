---
status: accepted
date: 2026-05-30
---

# Wrap ChessDotNet behind an adapter

## Context and Problem Statement

The game needs correct chess rules — legal moves, check, checkmate, castling, en
passant, promotion, and draws. Writing and testing those from scratch is a large
job that is not what this coursework is about, which is computer graphics. At the
same time, a third-party rules library brings its own types (`ChessGame`,
`Piece`, `Position`, `Player`), and letting those spread through the board, the
pieces, the input, and the HUD would tie every part of the game to one library.

## Decision Drivers

- Rules must be correct without spending the project's time on a rules engine.
- The graphics and interaction code should speak the game's own words, not a
  library's.
- Swapping or upgrading the rules library should touch one file, not the whole
  codebase.

## Considered Options

- Hand-write the chess rules in the game's own types.
- Use the `ChessDotNet` library directly wherever rules are needed.
- Use `ChessDotNet` through a single adapter that exposes the game's own types.

## Decision Outcome

Chosen option: "Use `ChessDotNet` through a single adapter". `ChessRulesAdapter`
is the only code that references the library; it translates between the library's
`ChessGame`/`Piece`/`Position`/`Player` and the game's own `BoardSquare`,
`ChessSide`, `ChessPieceKind`, `VisualPieceState`, and `MoveResult`. Everything
else — the controller, the views, the input — sees only the game's types.

### Consequences

- Good, because rules are correct for free and the project spends its effort on
  graphics.
- Good, because the rest of the code is independent of the library; the library
  could be replaced by editing one adapter.
- Good, because the game's vocabulary is consistent, which is also what the book
  documents.
- Bad, because the adapter must be kept in step with the library's API, and a
  library bug is inherited rather than fixable in place.
