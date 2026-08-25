---
id: render-the-board
name: Render the board and pieces
status: ready
owners: [mathwidu]
terms: [custom-piece, primitive-fallback, highlight]
decisions: [presentation/ADR-0001]
---

## Story

As a player watching the game
I want the board and every piece drawn and kept current
So that what I see is always the position the match is in

## Rule: The board and pieces are built from gameplay's report of the position

```gherkin
Example: A new game builds the full board and starting pieces
  Given a new game has started
  When the board is built
  Then there are 64 squares in the light-and-dark pattern
  And every piece gameplay reports stands on its square

Example: The pieces are rebuilt to match after a move
  Given a move has been played
  When the board syncs to the new position
  Then the shown pieces match gameplay's list exactly
  And nothing lingers on a square gameplay left empty
```

## Rule: A piece kind with no custom model still shows a piece

```gherkin
Example: A custom model is used when one is set for the kind
  Given a custom model is set for the bishop
  When a bishop is drawn
  Then it uses that model, scaled to fit the board

Example: A missing model falls back to a primitive shape
  Given no custom model is set for the rook
  When a rook is drawn
  Then it is built from primitive shapes in the side's colour
  And the square is never left empty
```

## Rule: The selected piece's legal squares are marked

```gherkin
Example: Highlights mark the legal destinations
  Given a piece is selected with three legal destinations
  When the board highlights them
  Then three highlight markers stand on those squares
  And clearing the selection removes them
```

## Open Questions

None.
