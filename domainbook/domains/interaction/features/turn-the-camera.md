---
id: turn-the-camera
name: Turn the camera
status: ready
owners: [mathwidu]
terms: [perspective, orbit]
---

## Story

As one of two players sharing a screen
I want the camera to face my side when it is my move
So that I read the board from my own end without moving my seat

## Rule: The camera faces the player on move when the turn changes

```gherkin
Example: The view swings to the side on move
  Given it becomes Black's turn
  When the turn change is applied
  Then the camera ends facing the board from Black's end

Example: A new game sets the view instantly for the first player
  Given a new game starts with White to move
  When the board is set up
  Then the camera is already facing White's end, without a transition
```

## Rule: The player can orbit and zoom without changing whose turn it is

```gherkin
Example: Q and E orbit the camera around the board
  Given the game is in play
  When the player holds Q or E
  Then the camera rotates around the board
  And the turn does not change

Example: The wheel zooms between a near and far limit
  Given the game is in play
  When the player scrolls the wheel
  Then the camera moves nearer or further, clamped between its limits
```

## Open Questions

None.
