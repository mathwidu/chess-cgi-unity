---
id: pick-with-the-mouse
name: Escolher com o mouse
status: ready
owners: [mathwidu]
terms: [seleção]
---

## Story

Como jogador usando o mouse
Quero que um clique no tabuleiro signifique o que vejo sob o cursor
Para que selecionar uma peça e escolher uma casa pareçam diretos

## Rule: Um clique se resolve para o que o raio atinge na cena

```gherkin
Example: Clicar em uma peça a seleciona
  Given o jogo está esperando entrada
  When o jogador clica em uma peça
  Then essa peça é enviada ao gameplay como a seleção

Example: Clicar em uma casa com uma peça já selecionada move para lá
  Given uma peça está selecionada
  When o jogador clica em uma casa
  Then essa casa é enviada ao gameplay como o destino

Example: Um clique que não atinge nada não faz nada
  Given o jogo está esperando entrada
  When o jogador clica em espaço vazio fora do tabuleiro
  Then nenhuma seleção e nenhuma jogada são enviadas
```

## Rule: As teclas cancelam, reiniciam e nunca disparam durante a animação

```gherkin
Example: Escape cancela a seleção atual
  Given uma peça está selecionada
  When o jogador pressiona Escape
  Then a seleção é limpa

Example: N inicia uma nova partida
  Given uma partida em andamento
  When o jogador pressiona N
  Then uma nova partida começa a partir da posição inicial

Example: A entrada é ignorada durante a animação de uma jogada
  Given uma jogada está reproduzindo sua animação
  When o jogador clica ou pressiona uma tecla
  Then isso não tem efeito até a animação terminar
```

## Open Questions

Nenhuma.
