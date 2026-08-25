---
id: promote-a-pawn
name: Promote a pawn
status: ready
owners: [mathwidu]
terms: [promotion, move, legal-destination]
---

## Story

As a player advancing a pawn
I want to choose what it becomes when it reaches the far rank
So that reaching the end of the board is the promotion the rules promise

## Rule: A pawn reaching the far rank pauses for a choice before it moves

```gherkin
Example: A promoting move waits for the piece to promote to
  Given a white pawn with a legal destination on rank 8
  When White chooses that square
  Then the pawn does not move yet
  And the status asks for the promotion choice

Example: The chosen kind is what lands on the square
  Given a pawn move is waiting for a promotion choice
  When White chooses the queen
  Then the move is played
  And a white queen stands on the far square

Example: A non-promoting pawn move does not ask
  Given a white pawn with a legal destination that is not on rank 8
  When White plays it
  Then the move happens at once with no promotion prompt
```

## Open Questions

None.
