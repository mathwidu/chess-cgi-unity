---
id: olhar-ao-redor-no-desktop
name: Olhar ao redor no desktop
status: ready
owners: [mathwidu]
terms: [perspectiva, órbita, olhar-ao-redor]
---

## Story

Como jogador de PC
Quero observar a sala a partir do meu assento e voltar ao tabuleiro
Para reconhecer o ambiente sem perder o controle da partida

## Rule: O jogador gira a visão no assento e pode voltar ao tabuleiro

Durante a partida, o jogador pode olhar a sala a partir do assento atual,
independentemente da órbita do tabuleiro. O botão **Olhar ao redor** nivela a
visão e amplia o campo de visão de 42° para 65°. Segurar o botão direito fora
do HUD e mover o mouse gira a cabeça: 360° na horizontal, limitado entre
75° para cima e 80° para baixo, sem inclinação lateral nem deslocamento.

Soltar o botão devolve o cursor e mantém a direção escolhida. **R**, **Esc**
ou **Voltar ao tabuleiro** restaura a perspectiva do lado atual e o campo de
visão anterior. Uma troca de turno atualiza esse lado, mas não interrompe a
observação. Iniciar outra partida ou abrir o menu restaura a vista do jogo.
Q/E e scroll continuam como órbita e zoom na vista do tabuleiro.

## Rule: Olhar ao redor preserva a seleção e libera o cursor

`CameraController` cuida da pose e libera o cursor ao perder foco, ser
desativado ou detectar um headset. O HUD não inicia arraste de câmera;
`InputController` bloqueia a seleção enquanto o botão direito estiver
pressionado, incluindo o quadro em que o arraste termina, sem depender da
ordem de Update dos componentes.

Os controles aparecem somente no desktop. O caminho de pose e movimento
do `XRRig` permanece separado. Testes de PlayMode exercitam a entrada pelo
mouse, retorno, troca de lado, foco, interação com HUD e presença de headset.
Capturas do Editor não substituem validação física de VR.

## Open Questions

Conforto e desempenho no headset físico continuam pendentes.
