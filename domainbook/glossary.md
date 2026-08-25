# Glossary

The words this book uses across every context, defined once and used the same
way wherever they appear. Code identifiers are English; the player-facing
interface is Portuguese, so a term carries its Portuguese name as an alias where
the HUD shows one.

## Board

The 8×8 grid the game is played on. Built in code at runtime as squares under a
board frame.

- **Aliases:** Tabuleiro
- **Status:** validated

## Square

One cell of the board. Held as a file index of 0–7 and a rank of 1–8, and shown
to a player in algebraic notation from a1 to h8.

- **Aliases:** Casa
- **Status:** validated
- **Example:** e4 is file index 4, rank 4.

## Side

One of the two players, White or Black. Sides alternate turns, White first.

- **Aliases:** Brancas, Pretas
- **Status:** validated

## Piece kind

The type of a chess piece: pawn, rook, knight, bishop, queen, or king. A kind is
separate from the side that owns the piece and from the model that shows it.

- **Aliases:** Peça
- **Status:** validated
