# Final Polish Validation

Date: 2026-06-05

## Automated Tests

- EditMode: passing, `61/61`, result file `TestResults/editmode-final-polish.xml`.
- PlayMode: passing, `2/2`, result file `TestResults/playmode-final-polish.xml`.
- macOS build: generated at `Builds/macOS/XadrezCGI.app`, `455M`, Unity build result `Succeeded`, `0` build errors.

## Implemented Checks

- Stable fallback tag remains `entrega-v1-estavel`.
- All six custom character prefabs load from `Resources/CustomPieces`.
- Initial board coverage validates 32/32 pieces with `CustomVisual`.
- Sidebar preview supports selected piece metadata, rotation and zoom.
- Movement animation completes before turn switch.
- Capture animation shows impact and removes captured piece.
- Camera stays turn-aware and includes subtle capture shake.

## Manual Checks

- Open `Builds/macOS/XadrezCGI.app`.
- Start a new game.
- Confirm all custom characters are visible.
- Select one piece of each type and inspect the sidebar preview.
- Drag and scroll the preview.
- Move `e2-e4`, `d7-d5`, then capture `e4xd5`.
- Confirm turn changes after animation and the camera remains readable.
- Test promotion UI with a pawn if time allows.
