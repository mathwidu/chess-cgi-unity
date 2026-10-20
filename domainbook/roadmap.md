---
id: chess-cgi-unity
milestones:
  - { id: playable-delivery, name: Entrega jogável do xadrez personalizado, status: done }
  - { id: vr-conversion, name: Conversão para VR no HTC Vive e no Meta Quest 3, status: in-progress }
  - { id: adversario-computador, name: Adversário controlado pelo computador do desktop ao Quest standalone, status: in-progress }
  - { id: realidade-mista-opcional, name: Escolher entre sala virtual e ambiente real no Quest, status: planned }
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
o HTC Vive (conectado a um PC) e o Meta Quest 3 (standalone). O rig, o HUD no
mundo, o agarrar/soltar e os ajustes físicos da mesa já têm implementação e
verificações no Editor; veja [Jogar em VR](domains/interaction/features/play-in-vr.md).
A frente atual prepara o build Android preservando a sala virtual e os
personagens. O [registro do Quest](../docs/quest3-standalone.md) distingue
compilação, simulação e o teste no aparelho, ainda dependente do modo de
desenvolvedor. A conversão só termina após validação no hardware.

### Adversário controlado pelo computador do desktop ao Quest standalone

Adicionar um adversário offline com níveis de dificuldade nomeados. O primeiro
passo comprova o contrato comum de gameplay e a integração UCI com Stockfish no
Editor e em uma build macOS. Depois, o mesmo contrato segue para Windows PC-VR e
Quest Link e, por fim, para uma build Android ARM64 executada sem PC no Meta
Quest 3. Em 19/10/2026, o usuário autorizou antecipar a implementação Android
enquanto o aparelho aguarda modo de desenvolvedor. O candidato usa processo
UCI local e preserva os contratos comuns; a validação física segue necessária.
Consulte [IA no Quest](domains/gameplay/features/jogar-contra-computador-quest.md).

### Escolher entre sala virtual e ambiente real no Quest

Depois da primeira versão standalone, acrescentar um modo de realidade mista
com passthrough, mantendo a sala virtual como opção. Ambos devem reutilizar o
tabuleiro, os personagens e as regras. Posicionamento no ambiente real,
persistência e transição entre os modos ainda serão definidos. Direção
confirmada pelo usuário em 17/10/2026; sem implementação ou teste nesta etapa.
