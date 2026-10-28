---
id: ver-de-quem-e-a-vez
name: Ver de quem é a vez
status: ready
owners: [mathwidu]
terms: [indicador-de-turno]
---

## Story

Como jogador sentado diante do tabuleiro, principalmente de óculos VR, onde o
HUD fica longe
Quero perceber de relance que é a minha vez
Para jogar sem procurar o painel de turno e sem que nada cubra o tabuleiro

## Rule: A borda do lado a jogar acende, sem cobrir casas

```gherkin
Example: Numa partida local, a vez passa de um lado ao outro
  Given uma partida entre dois jogadores acabou de começar
  Then uma luz amarela fina corre pela borda do tabuleiro do lado das brancas
  And a etiqueta "Vez das brancas" aparece no tampo, à frente dessa borda
  When as brancas jogam
  Then a luz passa para a borda das pretas com a etiqueta "Vez das pretas"

Example: Contra a IA, só a vez do jogador tem destaque
  Given uma partida contra a IA
  When é a vez da IA
  Then a luz fica na borda da IA, clara e neutra, com "IA pensando..."
  When é a vez do jogador
  Then a luz fica na borda do jogador, em amarelo, com "Sua vez"
```

## Rule: O sinal é discreto e não disputa atenção

```gherkin
Example: A etiqueta some sozinha
  Given a etiqueta "Sua vez" apareceu
  When passam cerca de 3 segundos
  Then a etiqueta some e a luz continua na borda

Example: Nada anima em loop
  Given o turno mudou
  Then a luz corre uma única vez do centro da borda para fora e para

Example: Fora da partida não há sinal de turno
  Given o menu está aberto ou a partida terminou
  Then nem a luz nem a etiqueta aparecem
```

A luz e a etiqueta são montadas pelo `BoardView` em unidades locais do
tabuleiro. Por isso sobem e descem com a mesa e têm a mesma proporção no
desktop e no VR. A etiqueta fica de pé para quem está sentado: o assento VR,
ou a câmera desktop. Ela não tem raycaster e não pega o raio. A etiqueta
aparece só no VR; no desktop, o painel de turno do HUD já nomeia a vez, e só a
luz aparece, mais fina.

## Open Questions

A visibilidade da luz e o tamanho da etiqueta ainda precisam ser validados em
headset.
