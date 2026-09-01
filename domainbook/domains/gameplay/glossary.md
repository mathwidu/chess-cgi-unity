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

## Adversário controlado pelo computador

Participante não humano que controla exatamente um lado de uma partida local e
fornece uma jogada quando esse lado possui o turno. A implementação proposta usa
um motor de xadrez externo, mas sempre devolve a jogada candidata às regras
locais antes que a partida seja alterada.

- **Aliases:** Adversário de IA, Computer opponent, Computer player
- **Status:** draft

## Nível de dificuldade

Configuração apresentada ao jogador que limita a força e o orçamento de tempo
do adversário controlado pelo computador sem modificar quais movimentos são
legais no xadrez.

- **Aliases:** Dificuldade, Perfil de dificuldade, Difficulty level
- **Status:** draft

## Estado de pensamento

Intervalo entre o adversário receber uma fotografia imutável da posição e sua
jogada candidata ser aceita ou a busca falhar. A entrada humana de jogada fica
bloqueada nesse estado, mas renderização, interface e rastreamento do headset
continuam funcionando.

- **Aliases:** IA pensando, Turno do computador, Thinking state
- **Status:** draft

## Motor de xadrez

Programa que recebe uma posição e configurações de busca e devolve uma jogada
candidata. Ele não controla nem altera diretamente a partida viva; as regras
locais continuam sendo a autoridade final.

- **Aliases:** Chess engine, Motor de IA
- **Status:** draft
