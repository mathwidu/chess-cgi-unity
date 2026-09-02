# Glossário de interação

As palavras que o contexto de interação usa para a entrada e a câmera. Um
termo é um heading H2 com sua definição logo abaixo.

## Seleção

A peça que um jogador escolheu para jogar. Escolher uma peça mostra seus
destinos legais; escolher um deles, ou pressionar `Esc`, encerra a seleção.

- **Aliases:** Selection, Peça selecionada
- **Status:** validated

## Perspectiva

O lado para o qual a câmera principal está voltada. Ela acompanha o turno,
girando para o jogador a jogar, de modo que cada um lê o tabuleiro a partir
do seu próprio lado.

- **Aliases:** Perspective, Câmera por turno, Per-turn camera
- **Status:** validated

## Órbita

Girar a câmera principal ao redor do tabuleiro com `Q` e `E`,
independentemente de quem está jogando.

- **Aliases:** Orbit, Girar câmera
- **Status:** validated

## Modo VR

O jogo renderizado para um headset e controlado com controles de movimento em
vez de monitor, mouse e teclado. Um alvo de conversão proposto para HTC Vive e
Meta Quest 3, ainda não construído.

- **Aliases:** VR mode, Immersive mode
- **Status:** draft

## Óculos VR

O display montado na cabeça que o jogador usa no modo VR. Sua pose rastreada
comanda a câmera, então a visão acompanha para onde o jogador olha, em vez de
girar para o lado a jogar.

- **Aliases:** Headset, HMD
- **Status:** draft

## Controle de movimento

Um controle de mão rastreado que o jogador segura no modo VR, usado para
apontar e escolher peças no lugar do mouse.

- **Aliases:** Motion controller, Controle, Hand controller
- **Status:** draft

## Raio de seleção

Um raio que um controle de movimento projeta, e o jogador aponta para uma
peça ou casa para selecioná-la, substituindo o raycast de tela do mouse.

- **Aliases:** Ray interactor, Pointer ray
- **Status:** draft
