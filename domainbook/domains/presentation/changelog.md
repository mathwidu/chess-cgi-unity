# Changelog

O que mudou no contexto de apresentação, lançamento mais recente primeiro, no
formato [Keep a Changelog](https://keepachangelog.com/en/1.1.0/): um H2 por
lançamento, escrito como "## [1.2.0] - 2026-06-30" com " [YANKED]" ao final
se o lançamento foi retirado, contendo Added, Changed, Deprecated, Removed,
Fixed ou Security como H3s, cada um deles uma lista de itens.

## [Unreleased]

### Added

- Modo desempenho: `PieceFactory` ganha uma flag `usePrimitivePieces` que faz
  `CreatePiece` construir peças clássicas mesmo quando há um prefab de peça
  personalizada definido, um ganho em GPUs modestas. O `GameHud` acrescenta uma
  caixa de seleção "Modo desempenho" ao menu inicial — presente no desktop e no
  VR, já que o menu é o mesmo — que reflete o estado atual e chama
  `ChessGameController.SetPerformanceMode`. O padrão é desligado.
- `BoardView` e `PieceFactory` adicionam um XR Simple Interactable e um
  `VrSelectionBridge` a cada casa e peça quando um headset está presente,
  para que o ray interactor da interação consiga selecioná-los da mesma
  forma que o raycast de desktop faz. Sem mudança no modo desktop.
- `GameHud` põe seu Canvas em world-space quando um headset está presente,
  como um painel na cena com um Tracked Device Graphic Raycaster e o XR UI
  Input Module, em vez do Canvas Screen Space Overlay e o Graphic Raycaster
  do desktop; veja [play-in-vr](../interaction/features/play-in-vr.md). O
  modo desktop permanece inalterado quando nenhum headset está presente.

### Changed

- A cena ao redor foi reduzida a mesa e chão. `ScenePolish` deixou de montar a
  sala de aula de faculdade e as duas luzes de ponto, e passou a construir uma
  plataforma de mesa com um equipamento de duas luzes direcionais (uma chave
  com sombras e uma de preenchimento sem sombras).
- O tabuleiro virou uma unidade única que escala e move como um todo.
  `BoardView` posiciona casas, peças e destaques em espaço local e os converte
  com `Transform.TransformPoint`, de modo que o transform do root do tabuleiro
  reja toda a montagem — a base para o jogador reposicionar e redimensionar o
  tabuleiro no futuro. Veja
  [ADR-0002](../interaction/decisions/0002-ver-o-tabuleiro-como-uma-mesa-a-partir-de-um-assento.md).
- Perfil de render reduzido para caber numa GPU modesta (ex.: GTX 1050 Ti):
  sem textura de profundidade nem de opacos, HDR e SSAO desligados, sombras
  mais curtas com menos cascatas e sem soft shadows, e bloom em filtragem
  padrão.
- O transform do tabuleiro passa a ser aplicado por modo em tempo de
  execução, na montagem: `BoardView` põe o root do tabuleiro em escala 1 na
  origem no desktop e em escala de mesa elevada em VR, com campos serializados
  próprios de cada modo. Assim o valor salvo na cena de um modo não quebra o
  outro.

### Fixed

- O tabuleiro de desktop voltou a aparecer emoldurado pela câmera. Ele havia
  virado um ponto minúsculo e descentralizado porque a cena guardava a escala
  e a posição de mesa do modo VR no root compartilhado do tabuleiro; agora
  `BoardView` aplica os valores de cada modo em tempo de execução, então o
  desktop volta ao tabuleiro em escala 1 na origem.
- O menu (HUD em world-space) de VR deixou de aparecer espelhado e só visível
  ao olhar para a direita. `GameHud` centraliza o painel à frente do jogador
  sentado e o gira para ficar de frente para ele, com o texto legível, em vez
  de posicioná-lo deslocado à direita e voltado ao contrário.
- As peças não se empilham mais no centro do tabuleiro. `PieceFactory` fixa a
  escala local do root da peça em `Vector3.one` e ajusta o visual custom em
  espaço local — altura e base calculadas a partir do `lossyScale` do pai —,
  em vez de deixá-lo herdar a escala de mundo do tabuleiro; assim cada peça
  assenta na sua casa na escala de mesa.
- A mesa da cenografia deixou de aparecer como um cubo pequeno flutuando no
  centro do tabuleiro no modo desktop. `ScenePolish` passa a posicionar e
  dimensionar a mesa por modo: no desktop ela vira uma plataforma larga logo
  abaixo do tabuleiro em escala 1, e em VR mantém o bloco em escala de mesa sob
  o tabuleiro reduzido.
