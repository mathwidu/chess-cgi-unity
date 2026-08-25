# Presentation glossary

The words the presentation context uses for what a player sees. A term is an H2
heading with its definition below it.

## Custom piece

The character model shown for a piece kind, one per kind, inspired by the class.
Loaded as a prefab and scaled to fit the board.

- **Aliases:** Peça personalizada, Custom character
- **Status:** validated
- **Example:** the pawn is the "Mathwidu redhead" model; the bishop is "Rafael".

## Primitive fallback

The piece a factory builds from primitive shapes — cylinders, spheres, cubes —
when no custom model is set for a kind, so the board is never missing a piece.

- **Aliases:** Peça clássica
- **Status:** validated

## Highlight

A marker the board places on each legal destination of the selected piece.

- **Aliases:** Destaque
- **Status:** validated

## Selected-piece preview

The panel that shows the selected piece on its own, rendered by a small camera
into a texture, which the player can orbit and zoom to read the character.

- **Aliases:** Preview da peça, Painel da peça
- **Status:** validated

## HUD

The Canvas interface built over the game: the title and turn lines, the status
message, the move history, the promotion prompt, the start screen, and the
selected-piece panel.

- **Aliases:** Interface, Canvas UI
- **Status:** validated
