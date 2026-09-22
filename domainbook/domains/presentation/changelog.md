# Changelog

O que mudou no contexto de apresentação, lançamento mais recente primeiro, no
formato [Keep a Changelog](https://keepachangelog.com/en/1.1.0/): um H2 por
lançamento, escrito como "## [1.2.0] - 2026-06-30" com " [YANKED]" ao final
se o lançamento foi retirado, contendo Added, Changed, Deprecated, Removed,
Fixed ou Security como H3s, cada um deles uma lista de itens.

## [Unreleased]

### Added

- `BoardView` e `PieceFactory` adicionam um XR Simple Interactable e um
  `VrSelectionBridge` a cada casa e peça quando um headset está presente,
  para que o ray interactor da interação consiga selecioná-los da mesma
  forma que o raycast de desktop faz. Sem mudança no modo desktop.
- `GameHud` põe seu Canvas em world-space quando um headset está presente,
  como um painel na cena com um Tracked Device Graphic Raycaster e o XR UI
  Input Module, em vez do Canvas Screen Space Overlay e o Graphic Raycaster
  do desktop; veja [play-in-vr](../interaction/features/play-in-vr.md). O
  modo desktop permanece inalterado quando nenhum headset está presente.

- Tela inicial com modo, lado e dificuldade; estado de pensamento, recuperação de falha e retorno ao menu.
- Nova partida mantém as opções escolhidas.

- Harness de HUD XR localiza StartPlayButton por identidade entre os botões, sem depender dos contêineres visuais do menu.
### Changed

- Menu com escolhas explícitas de modo, lado e três dificuldades, estado selecionado e resumo antes da partida.
- HUD, promoção, recuperação de falha e ajuda com tipografia ampliada e identidade visual comum.
- Ajuda bloqueia os botões de fundo; a configuração é preservada ao fechar e ao retornar ao menu.
- A composição do menu se ajusta às dimensões do Canvas desktop ou world-space; o modo local oculta as opções exclusivas da IA.
- Direção Palco da turma: cores Feevale, assinatura original, tipografia Lato e elenco real em preview 3D isolado; trocar lado move suavemente o palco, sem alterar a partida.
- Navegação por teclado inicia com foco visível e transfere o foco para ajuda, promoção e recuperação de erro.
- O palco 3D do preview da peça selecionada não herda a escala do Canvas, mantendo o enquadramento em Canvas de tela ou world-space.
- Redistribuição do menu: assinaturas no cabeçalho, elenco central e faixa inferior com modo, lado, dificuldade e ação de jogar; navegação explícita acompanha os grupos horizontais.
