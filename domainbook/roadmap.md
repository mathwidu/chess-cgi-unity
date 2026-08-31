---
id: chess-cgi-unity
milestones:
  - { id: playable-delivery, name: Entrega jogável do xadrez personalizado, status: done }
  - { id: vr-conversion, name: Conversão para VR no HTC Vive e no Meta Quest 3, status: planned }
  
---

# roteiro do chess-cgi-unity

## Marcos

### Entrega jogável do xadrez personalizado

A build entregue para a disciplina: um xadrez 3D local para dois jogadores em
Unity 6.3, com regras completas, um tabuleiro e HUD construídos em código em
tempo de execução, um modelo de personagem customizado para cada tipo de
peça, controle por mouse e teclado, e uma câmera que gira para o jogador cujo
turno é o atual.

### Conversão para VR no HTC Vive e no Meta Quest 3

Levar o jogo para a realidade virtual: o jogador usa um headset e escolhe as
peças com controles de movimento em vez de monitor, mouse e teclado, visando
o HTC Vive (conectado a um PC) e o Meta Quest 3 (standalone). O primeiro
passo é um estudo de viabilidade e um plano de conversão de alto nível — veja
a funcionalidade de interação
[Jogar em VR](domains/interaction/features/play-in-vr.md). A construção vem
depois que o plano for revisado e a escolha do framework de XR estiver
definida em um registro de decisão.
