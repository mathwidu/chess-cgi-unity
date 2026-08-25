---
id: pick-with-the-mouse
name: Pick with the mouse
status: ready
owners: [mathwidu]
terms: [selection]
---

## Story

As a player using the mouse
I want a click on the board to mean what I see under the cursor
So that selecting a piece and choosing a square feel direct

## Rule: A click resolves to whatever the ray hits in the scene

```gherkin
Example: Clicking a piece selects it
  Given the game is waiting for input
  When the player clicks a piece
  Then that piece is sent to gameplay as the selection

Example: Clicking a square with a piece already selected moves there
  Given a piece is selected
  When the player clicks a square
  Then that square is sent to gameplay as the destination

Example: A click that hits nothing does nothing
  Given the game is waiting for input
  When the player clicks empty space off the board
  Then no selection and no move are sent
```

## Rule: The keys cancel, restart, and never fire mid-animation

```gherkin
Example: Escape cancels the current selection
  Given a piece is selected
  When the player presses Escape
  Then the selection is cleared

Example: N starts a new game
  Given a game in progress
  When the player presses N
  Then a new game begins from the starting position

Example: Input is ignored while a move is animating
  Given a move is playing its animation
  When the player clicks or presses a key
  Then it has no effect until the animation finishes
```

## Open Questions

None.
