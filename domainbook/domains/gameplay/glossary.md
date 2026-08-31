# Glossário de gameplay

As palavras que o contexto de gameplay usa para a partida e suas regras. Um
termo é um heading H2 com sua definição logo abaixo.

## Jogada

Uma peça saindo de uma casa para outra, julgada legal pelas regras antes de
ser jogada. Mostrada no histórico como origem, um separador e destino — um
traço para uma jogada silenciosa, um `x` para uma captura.

- **Aliases:** Move, Lance
- **Status:** validated
- **Example:** `e2-e4`; `Brancas: e2-e4` na linha do histórico.

## Destino legal

Uma casa para a qual as regras permitem que a peça selecionada se mova. O
controlador só oferece esse conjunto, e o tabuleiro o destaca.

- **Aliases:** Legal destination
- **Status:** validated

## Captura

Uma jogada que termina em uma casa ocupada por uma peça adversária, removendo-a
do tabuleiro. Escrita com um `x` entre origem e destino.

- **Aliases:** Capture
- **Status:** validated

## Turno

De quem é a vez de jogar, Brancas ou Pretas. Passa para o outro lado após uma
jogada legal, e a câmera gira junto.

- **Aliases:** Turn
- **Status:** validated

## Xeque

Uma posição em que o lado a jogar tem seu rei atacado e precisa respondê-lo.

- **Aliases:** Check
- **Status:** validated

## Xeque-mate

Um xeque que o lado a jogar não consegue escapar. Encerra a partida a favor
do outro lado.

- **Aliases:** Checkmate
- **Status:** validated

## Empate

Uma partida que termina sem vencedor — afogamento ou qualquer posição de
empate que as regras reconheçam.

- **Aliases:** Draw
- **Status:** validated

## Promoção

Um peão que chega à última linha se transformando em outro tipo de peça. A
jogada pausa para o jogador escolher dama, torre, bispo ou cavalo antes de
ser confirmada.

- **Aliases:** Promotion
- **Status:** validated
