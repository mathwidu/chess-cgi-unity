---
id: ver-as-pecas-capturadas
name: Ver as peças capturadas
status: ready
owners: [mathwidu]
terms: [pecas-capturadas]
---

## Story

Como jogador no meio da partida
Quero ver quais peças já foram capturadas, e por quem
Para avaliar a partida de relance, sem contar peças no tabuleiro

## Rule: Cada captura vai para o lado de quem capturou

```gherkin
Example: As brancas capturam um peão
  Given uma partida em andamento, e o jogador senta do lado das brancas
  When as brancas tomam um peão preto
  Then o lance termina no tabuleiro
  And o peão voa da casa dele até a faixa ao lado do tabuleiro, na metade
    perto do jogador
  And ele fica ali em miniatura, sem poder ser clicado nem agarrado

Example: Cada lado guarda o que tomou
  Given as brancas tomaram um peão
  When as pretas recapturam com a dama
  Then a faixa perto do jogador mostra o peão preto
  And a faixa do lado de lá mostra o peão branco
```

## Rule: O placar de material aparece só quando alguém está à frente

```gherkin
Example: Vantagem das brancas
  Given as brancas tomaram a dama e um peão, e perderam dois peões
  Then um "+8" amarelo aparece no canto do lado das brancas

Example: Material igual
  Given cada lado tomou um peão
  Then nenhum placar aparece

Example: Promoção conta como a peça nova
  Given um peão é promovido a dama sem captura
  Then o placar considera a dama no lugar do peão
```

## Rule: As capturas acompanham a partida e o jogador

```gherkin
Example: Nova partida
  Given há peças capturadas ao lado do tabuleiro
  When o jogador começa uma nova partida
  Then as faixas ficam vazias e o placar some

Example: Mudar de lado
  Given o jogador passa a jogar do lado das pretas
  Then as faixas vão para a direita dele
  And a metade perto dele mostra o que as pretas capturaram
```

As faixas são montadas pelo `BoardView` em unidades locais do tabuleiro, por
isso sobem e descem com a mesa. As miniaturas são da mesma família das peças
do tabuleiro (personagens, ou peças clássicas no modo desempenho) e são
ordenadas por valor.

## Open Questions

- No desktop, a câmera fixa deixa as faixas na borda direita da tela, em parte
  atrás do painel de histórico. Q/E giram a câmera para vê-las. Falta decidir
  se o desktop deve ganhar um resumo das capturas no HUD.
- A leitura das miniaturas ainda precisa ser validada em headset.
