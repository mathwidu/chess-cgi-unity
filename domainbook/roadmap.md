---
id: chess-cgi-unity
milestones:
  - { id: playable-delivery, name: Entrega jogável do xadrez personalizado, status: done }
  - { id: vr-conversion, name: Conversão para VR no HTC Vive e no Meta Quest 3, status: planned }
  - { id: adversario-computador, name: Adversário controlado pelo computador do desktop ao Quest standalone, status: planned }
  
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

### Adversário controlado pelo computador do desktop ao Quest standalone

Adicionar um adversário offline com níveis de dificuldade nomeados. O primeiro
passo comprova o contrato comum de gameplay e a integração UCI com Stockfish no
Editor e em uma build macOS. Depois, o mesmo contrato segue para Windows PC-VR e
Quest Link e, por fim, para uma build Android ARM64 executada sem PC no Meta
Quest 3. A integração standalone só é escolhida depois de um experimento no
hardware — consulte a funcionalidade de gameplay
[Jogar contra um adversário controlado pelo computador](domains/gameplay/features/jogar-contra-computador.md).
