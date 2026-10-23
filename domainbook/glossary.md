# Glossário

As palavras que este livro usa em todo contexto, definidas uma vez e usadas da
mesma forma onde quer que apareçam. Identificadores de código estão em
inglês; a interface do jogador é em português, então cada termo carrega o
nome em inglês (o identificador de código) como alias.

## Tabuleiro

A grade 8×8 sobre a qual o jogo é disputado. Construída em código em tempo de
execução como casas sob uma moldura de tabuleiro.

- **Aliases:** Board
- **Status:** validated

## Casa

Uma célula do tabuleiro. Guardada como um índice de coluna de 0–7 e uma linha
de 1–8, e mostrada ao jogador em notação algébrica de a1 a h8.

- **Aliases:** Square
- **Status:** validated
- **Example:** e4 é índice de coluna 4, linha 4.

## Lado

Um dos dois jogadores, Brancas ou Pretas. Os lados se alternam a cada turno,
Brancas primeiro.

- **Aliases:** Side, Brancas, Pretas
- **Status:** validated

## Tipo de peça

O tipo de uma peça de xadrez: peão, torre, cavalo, bispo, dama ou rei. Um
tipo é diferente do lado dono da peça e do modelo que a representa.

- **Aliases:** Piece kind, Peça
- **Status:** validated
