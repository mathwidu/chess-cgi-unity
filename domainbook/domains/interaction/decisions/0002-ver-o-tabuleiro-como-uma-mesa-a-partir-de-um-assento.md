---
status: accepted
date: 2026-09-21
---

# Ver o tabuleiro como uma mesa a partir de um assento

## Context and Problem Statement

O feedback da build no Windows (HTC Vive, GPU GTX 1050 Ti) apontou que, no
[modo VR](../glossary.md), o jogador vive o [tabuleiro](../../gameplay/glossary.md)
por dentro — é como se fizesse parte de um tabuleiro em escala de sala — em vez
de vê-lo como uma mesa à sua frente. O xadrez é jogado sobre uma mesa, então a
vista precisava mudar. Além disso, a intenção declarada é que o jogador possa,
no futuro, mover e redimensionar o tabuleiro, e a mesma GPU modesta do Vive
tem pouca margem para uma cena cara ao redor.

## Decision Drivers

- A vista de dentro de um tabuleiro em escala de sala desorienta; a referência
  natural do jogo é uma mesa vista de um assento.
- O jogador deverá, mais adiante, mover e redimensionar o tabuleiro, então o
  tabuleiro precisa ser uma unidade que escala e se move como um todo.
- A GTX 1050 Ti tem pouca margem: a cena ao redor e o custo de render precisam
  ser baixos para o modo VR não engasgar ao mover a cabeça.

## Considered Options

- Manter a vista imersiva de dentro do tabuleiro em escala de sala.
- Tabuleiro em escala de mesa (~0,45 m) visto de um assento logo à frente.

## Decision Outcome

Opção escolhida: "tabuleiro em escala de mesa visto de um assento". `XRRig`
senta o jogador à frente e acima do tabuleiro; `BoardView` passa a posicionar
casas, peças e destaques em espaço local e convertê-los com
`Transform.TransformPoint`, de modo que o root do tabuleiro seja uma unidade
única que escala e move tudo junto — a base sobre a qual a futura feature de
mover e redimensionar vai se apoiar. A cena ao redor foi reduzida a mesa e
chão, e o perfil de render foi cortado para caber numa GPU modesta.

### Consequences

- Bom, porque a vista passa a ser a de uma mesa de xadrez, que é a referência
  esperada do jogo.
- Bom, porque o tabuleiro vira uma unidade única já pronta para ser movida e
  redimensionada quando essa feature chegar — este registro não a cobre.
- Bom, porque a cena mais enxuta e o perfil de render reduzido aliviam a GPU no
  modo VR.
- Ruim, porque posicionar peças e casas em espaço local obrigou a refazer a
  montagem da peça (do contrário o visual herdava a escala de mundo do
  tabuleiro e as peças se empilhavam no centro) — corrigido, mas é uma
  armadilha a lembrar sempre que a escala do tabuleiro mudar.
