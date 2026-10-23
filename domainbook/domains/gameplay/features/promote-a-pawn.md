---
id: promote-a-pawn
name: Promover um peão
status: ready
owners: [mathwidu]
terms: [promoção, jogada, destino-legal]
---

## Story

Como jogador avançando um peão
Quero escolher em que ele se transforma ao chegar à última linha
Para que chegar ao final do tabuleiro seja a promoção que as regras prometem

## Rule: Um peão que chega à última linha pausa para uma escolha antes de se mover

```gherkin
Example: Uma jogada de promoção espera pelo tipo em que a peça vai se transformar
  Given um peão branco com destino legal na linha 8
  When Brancas escolhe essa casa
  Then o peão ainda não se move
  And o status pede a escolha da promoção

Example: O tipo escolhido é o que ocupa a casa
  Given uma jogada de peão está esperando a escolha da promoção
  When Brancas escolhe a dama
  Then a jogada é realizada
  And uma dama branca fica na casa final

Example: Uma jogada de peão que não promove não pergunta nada
  Given um peão branco com destino legal que não está na linha 8
  When Brancas o joga
  Then a jogada acontece de imediato, sem pedido de promoção
```

## Open Questions

Nenhuma.
