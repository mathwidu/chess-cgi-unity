# Gameplay glossary

The words the gameplay context uses for the match and its rules. A term is an H2
heading with its definition below it.

## Move

A piece leaving one square for another, judged legal by the rules before it is
played. Shown in the history as origin, a separator, and destination — a dash
for a quiet move, an `x` for a capture.

- **Aliases:** Jogada, Lance
- **Status:** validated
- **Example:** `e2-e4`; `Brancas: e2-e4` in the history line.

## Legal destination

A square the rules allow the selected piece to move to. The controller only ever
offers this set, and the board highlights it.

- **Aliases:** Destino legal
- **Status:** validated

## Capture

A move that lands on a square holding an opponent piece, removing it from the
board. Written with an `x` between origin and destination.

- **Aliases:** Captura
- **Status:** validated

## Turn

Whose move it is, White or Black. It passes to the other side after a legal
move, and the camera turns with it.

- **Aliases:** Turno
- **Status:** validated

## Check

A position where the side to move has its king attacked and must answer it.

- **Aliases:** Xeque
- **Status:** validated

## Checkmate

A check the side to move cannot escape. It ends the game in favour of the other
side.

- **Aliases:** Xeque-mate
- **Status:** validated

## Draw

A game that ends with neither side winning — stalemate or any drawn position the
rules recognise.

- **Aliases:** Empate
- **Status:** validated

## Promotion

A pawn reaching the far rank becoming another kind. The move pauses for the
player to choose queen, rook, bishop, or knight before it is committed.

- **Aliases:** Promoção
- **Status:** validated
