---
id: ajustar-o-tabuleiro-com-duas-maos
name: Ajustar o tabuleiro com duas mãos
status: ready
owners: [mathwidu]
terms: [hastes-de-ajuste, modo-vr, agarrar-e-soltar, rastreamento-de-mãos]
decisions: [interaction/ADR-0001]
---

## Story

Como jogador de óculos VR
Quero segurar as duas hastes metálicas laterais e afastar ou aproximar as mãos
Para aumentar ou diminuir o tabuleiro e as peças enquanto vejo o resultado

## Rule: O gesto exige duas mãos perto das hastes

```gherkin
Example: Afastar as mãos aumenta o tabuleiro em tempo real
  Given uma mão segura cada haste com o gatilho ou o gesto de agarrar
  When o jogador afasta as mãos ao longo da largura do tabuleiro
  Then o tabuleiro e as peças aumentam durante o movimento
  And aproximar as mãos diminui o tamanho

Example: Uma mão sozinha não altera o tamanho
  Given somente uma haste está agarrada
  When o jogador puxa essa haste
  Then o tamanho continua igual

Example: Mover as duas mãos juntas não desloca o jogo
  Given ambas as hastes estão agarradas
  When as mãos se deslocam sem alterar sua distância lateral
  Then o tabuleiro não muda de tamanho nem de posição ou orientação
```

As hastes usam seleção de proximidade. O raio do HUD não as seleciona. Duas
entradas do mesmo lado, mesmo que uma seja controle e a outra mão rastreada,
não contam como duas mãos.

## Rule: Soltar ou interromper o rastreamento encerra o gesto

```gherkin
Example: Soltar uma haste mantém o tamanho escolhido
  Given um ajuste em andamento
  When uma das mãos solta a haste
  Then o tamanho atual é mantido
  And uma próxima dupla de agarradas começa a partir desse tamanho

Example: Uma pose inválida não causa um salto de escala
  Given um ajuste em andamento
  When há perda de rastreamento, pausa, perda de foco ou recentralização
  Then o ajuste para sem mudar o tamanho para uma pose inválida
  And o jogador precisa soltar e agarrar novamente para continuar
```

Menu, promoção, jogadas em animação, turno da IA e uma peça selecionada ou
agarrada bloqueiam o ajuste. Um salto brusco de pose ou cruzamento das mãos
também interrompe o gesto. O ajuste não envia comandos de jogada ao gameplay.

## Rule: Os limites permitem voltar imediatamente

```gherkin
Example: Inverter o movimento no limite
  Given o tabuleiro chegou ao tamanho máximo ou mínimo
  When o jogador inverte o movimento das duas mãos
  Then o tamanho volta a variar imediatamente
  And o jogador não precisa desfazer uma distância acumulada além do limite
```

Veja os limites e o contato com a mesa em
[Redimensionar o tabuleiro no VR](../../presentation/features/redimensionar-o-tabuleiro-no-vr.md).

## Open Questions

Conforto, alcance e estabilidade ainda precisam de uma sessão com headset real.
