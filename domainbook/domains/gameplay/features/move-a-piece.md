---
id: move-a-piece
name: Mover uma peça
status: ready
owners: [mathwidu]
terms: [jogada, destino-legal, captura, turno, xeque, xeque-mate, empate]
decisions: [gameplay/ADR-0001]
---

## Story

Como jogador diante do tabuleiro
Quero escolher uma das minhas peças e jogá-la em uma casa legal
Para que a partida avance sob as regras reais do xadrez

## Rule: Somente o lado a jogar pode iniciar uma jogada, e somente para uma casa legal

```gherkin
Example: Uma peça do lado a jogar é selecionada e suas casas legais são oferecidas
  Given é a vez das Brancas
  When Brancas seleciona uma peça branca
  Then essa peça é destacada
  And toda casa que as regras permitem é destacada como destino legal

Example: Uma peça do outro lado não inicia uma jogada
  Given é a vez das Brancas
  When Brancas seleciona uma peça preta
  Then nenhuma peça é selecionada
  And nenhum destino é destacado

Example: Uma casa que não é destino legal é recusada
  Given uma peça branca está selecionada com seus destinos legais mostrados
  When Brancas escolhe uma casa que não está entre eles
  Then a peça não se move
  And o status informa que a jogada é inválida
```

## Rule: Uma jogada realizada atualiza o tabuleiro e passa o turno

```gherkin
Example: Uma jogada silenciosa passa o turno
  Given é a vez das Brancas e uma peça branca está selecionada
  When Brancas a joga para uma casa legal vazia
  Then a peça é mostrada na nova casa
  And passa a ser a vez das Pretas

Example: Uma captura remove a peça adversária
  Given um destino legal contém uma peça preta
  When Brancas joga a captura
  Then a peça preta desaparece do tabuleiro
  And a jogada é escrita no histórico com um "x"
```

## Rule: A jogada que encerra a partida é relatada como tal

```gherkin
Example: Xeque-mate encerra a partida a favor do outro lado
  Given uma jogada que deixa o adversário em xeque-mate
  When ela é jogada
  Then o status informa que o lado que deu mate venceu
  And nenhuma outra jogada é aceita

Example: Uma jogada de empate encerra a partida sem vencedor
  Given uma jogada que deixa a posição empatada ou afogada
  When ela é jogada
  Then o status informa que a partida é um empate

Example: Uma jogada de xeque nomeia o xeque e continua
  Given uma jogada que deixa o adversário em xeque, mas não em xeque-mate
  When ela é jogada
  Then o status informa xeque
  And é a vez do adversário
```

## Open Questions

Nenhuma.
