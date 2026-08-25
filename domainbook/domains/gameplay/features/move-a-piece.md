---
id: move-a-piece
name: Move a piece
status: ready
owners: [mathwidu]
terms: [move, legal-destination, capture, turn, check, checkmate, draw]
decisions: [gameplay/ADR-0001]
---

## Story

As a player at the board
I want to pick one of my pieces and play it to a legal square
So that the match advances under the real rules of chess

## Rule: Only the side on move may start a move, and only to a legal square

```gherkin
Example: A piece of the side on move is selected and its legal squares are offered
  Given it is White's turn
  When White selects a white piece
  Then that piece is highlighted
  And every square the rules allow it is highlighted as a legal destination

Example: A piece of the other side does not start a move
  Given it is White's turn
  When White selects a black piece
  Then no piece is selected
  And no destination is highlighted

Example: A square that is not a legal destination is refused
  Given a white piece is selected with its legal destinations shown
  When White chooses a square that is not among them
  Then the piece does not move
  And the status reads that the move was invalid
```

## Rule: A played move updates the board and passes the turn

```gherkin
Example: A quiet move passes the turn
  Given it is White's turn and a white piece is selected
  When White plays it to an empty legal square
  Then the piece is shown on the new square
  And it becomes Black's turn

Example: A capture removes the opponent piece
  Given a legal destination holds a black piece
  When White plays the capture
  Then the black piece is gone from the board
  And the move is written to the history with an "x"
```

## Rule: The move that ends the game is reported as such

```gherkin
Example: Checkmate ends the game for the other side
  Given a move that leaves the opponent checkmated
  When it is played
  Then the status says the mating side wins
  And no further move is accepted

Example: A drawing move ends the game with no winner
  Given a move that leaves the position drawn or stalemated
  When it is played
  Then the status says the game is a draw

Example: A checking move names the check and continues
  Given a move that leaves the opponent in check but not mated
  When it is played
  Then the status says check
  And it is the opponent's turn
```

## Open Questions

None.
