---
status: accepted
date: 2026-09-24
---

# Modelar a sala em metros do VR e escalar para o desktop

## Context and Problem Statement

A [mesa](../glossary.md) sob o tabuleiro era um cubo com medidas diferentes
em cada modo: uma placa de 12 × 0,8 × 12 unidades no desktop e um bloco de
0,9 × 0,774 × 0,9 m no VR. Para virar uma mesa de verdade, com tampo, saia,
pés e uma placa de controle que regula a altura, esse modelo teria de ser
desenhado e mantido duas vezes. Além disso, o tabuleiro do desktop tem escala 1
na origem, e o do VR tem escala 0,045 a 0,78 m de altura
([interaction ADR 0002](../../interaction/decisions/0002-ver-o-tabuleiro-como-uma-mesa-a-partir-de-um-assento.md)).
Por isso as medidas de um modo não servem no outro.

## Decision Drivers

- Uma só mesa, igual nos dois modos, com as mesmas proporções em relação ao
  tabuleiro.
- A altura da mesa precisa mover a mesa e o tabuleiro juntos, sem mexer na
  câmera; no VR quem manda na altura dos olhos são os óculos.
- O desktop não pode mudar de enquadramento nem de escala do tabuleiro.

## Considered Options

- Manter duas geometrias, uma por modo, com medidas próprias.
- Modelar a sala (chão e mesa) em metros do VR e, no desktop, escalar e
  deslocar o root da sala para que ela fique sob o tabuleiro do desktop
  exatamente como fica sob o do VR.

## Decision Outcome

Opção escolhida: "modelar a sala em metros do VR". `BoardView.FitVrRoomToMode`
aplica ao root `CollegeTheme` a escala `desktopBoardScale / vrBoardScale`
(cerca de 22) e o deslocamento que leva o ponto do tabuleiro VR ao do
desktop. `TableView` constrói a mesa em metros e move o tampo. O tabuleiro
acompanha por `BoardView.SetSurfaceOffset`, com o deslocamento do tampo
convertido para unidades do modo pela escala do root.

### Consequences

- Bom, porque uma única mesa serve aos dois modos, e ajustes de desenho valem
  para ambos.
- Bom, porque o tabuleiro continua sendo a referência de cada modo: no desktop
  ele segue com escala 1 na origem, e o chão desce para baixo dos pés da mesa.
- Bom, porque a altura é uma coisa só: a mesa sobe e desce com o tabuleiro em
  cima, e a câmera não é tocada.
- Ruim, porque tudo o que entrar na sala precisa ser medido em metros do VR,
  mesmo quando só aparece no desktop. No desktop, esses metros ficam cerca de
  22 vezes maiores.
- Ruim, porque o passo de altura precisa de um valor por modo: 3 cm no VR e
  0,25 unidade no desktop (cerca de 1,1 cm da sala escalada). Um passo de 3 cm
  no desktop tiraria o tabuleiro do enquadramento.
