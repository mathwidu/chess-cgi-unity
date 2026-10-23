---
id: jogar-por-toque
name: Jogar por toque no navegador
status: draft
owners: [mathwidu]
terms: [seleção, perspectiva, órbita]
---

## Story

Como jogador no navegador com tela de toque
Quero selecionar uma peça e depois sua casa de destino
Para jogar com os mesmos personagens e regras da partida local.

## Rule: Um tap se confirma ao soltar o dedo

```gherkin
Example: Selecionar e mover
  Given o navegador espera uma jogada
  When o jogador toca e solta sobre uma peça da vez
  Then a peça é enviada ao gameplay como seleção
  When toca e solta sobre uma casa destacada
  Then a casa é enviada ao gameplay como destino

Example: Distinguir um gesto de uma jogada
  Given um dedo está sobre o tabuleiro
  When ele arrasta, permanece pressionado por mais de 550 ms ou recebe cancelamento
  Then soltar esse dedo não seleciona uma peça nem uma casa
  And voltar ao ponto inicial não transforma o arrasto em tap

Example: Interagir com o HUD
  Given um dedo começou sobre o HUD ou passou por ele
  When esse dedo é solto sobre o tabuleiro
  Then o tabuleiro não recebe o tap
```

## Rule: Dois dedos controlam a câmera sem enviar uma jogada

```gherkin
Example: Órbita e aproximação
  Given dois dedos estão fora do HUD
  When os dedos se deslocam horizontalmente juntos
  Then a câmera faz órbita ao redor do tabuleiro
  When os dedos se afastam ou se aproximam
  Then a câmera aproxima ou afasta a vista
  When sobra um dedo e ele é solto
  Then nenhuma jogada é enviada
  And uma nova seleção exige outro tap após soltar todos os dedos

Example: Um dedo está no HUD
  Given pelo menos um dedo do gesto passou sobre o HUD
  Then o gesto não gira nem aproxima a câmera principal
```

`WebTouchGesture` mantém a decisão de tap, arrasto, cancelamento e multitouch;
`InputController` lê `Touchscreen` pelo Input System e usa raycast uGUI na posição
real de cada dedo. O mouse emulado fica suprimido durante o toque e por 350 ms
depois dele. O mouse real continua com a seleção por clique e os atalhos atuais.
Os botões de câmera oferecem órbita, aproximação e retorno sem gesto.

No Web, a perspectiva usa 55 graus de elevação e enquadra os bounds do tabuleiro
e dos personagens dentro da região livre do HUD. Um novo turno, retorno ao
tabuleiro ou mudança de viewport recalcula a distância; aproximação manual pode
ampliar a posição depois disso. O modo VR mantém sua câmera rastreada.

Verificação local:

`WebTouchGestureTests` cobre tap no release, arrasto de ida/volta, UI, cancelamento,
pressionamento longo, pinch/órbita e o dedo remanescente. `WebTouchBoardTests`
exercita eventos reais do Input System e supressão do mouse emulado. A execução
no Unity e a validação física em navegador móvel pertencem à integração.

## Open Questions

O conforto de pinch/órbita e a precisão dos alvos precisam de teste em aparelho.
