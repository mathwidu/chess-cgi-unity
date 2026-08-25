---
id: preview-the-selected-piece
name: Preview the selected piece
status: ready
owners: [mathwidu]
terms: [selected-piece-preview, custom-piece]
decisions: [presentation/ADR-0001]
---

## Story

As a player who picked a piece
I want to see that piece on its own and turn it around
So that I can read which character it is and look at the model

## Rule: Selecting a piece shows it in its own preview

```gherkin
Example: The panel appears with the selected piece
  Given no piece is selected and the panel is hidden
  When a piece is selected
  Then the selected-piece panel appears
  And it shows that piece rendered on its own, with its name and square

Example: Clearing the selection hides the panel
  Given a piece is selected and the panel is shown
  When the selection is cleared
  Then the panel is hidden
  And the preview piece is torn down
```

## Rule: The preview can be turned and zoomed without touching the board

```gherkin
Example: Dragging turns the preview only
  Given the selected-piece preview is shown
  When the player drags across it
  Then the preview piece rotates
  And no piece on the board moves

Example: The zoom buttons and the wheel change the preview distance
  Given the selected-piece preview is shown
  When the player uses the "+" or "-" button, or scrolls over the preview
  Then the preview camera moves nearer or further
  And the board camera is unchanged
```

## Open Questions

None.
