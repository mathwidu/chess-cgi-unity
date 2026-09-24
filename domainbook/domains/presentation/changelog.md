# Changelog

O que mudou no contexto de apresentação, lançamento mais recente primeiro, no
formato [Keep a Changelog](https://keepachangelog.com/en/1.1.0/): um H2 por
lançamento, escrito como "## [1.2.0] - 2026-06-30" com " [YANKED]" ao final
se o lançamento foi retirado, contendo Added, Changed, Deprecated, Removed,
Fixed ou Security como H3s, cada um deles uma lista de itens.

## [Unreleased]

### Added

- [Mesa](glossary.md) de verdade sob o tabuleiro, no lugar do cubo, com altura
  regulável ([feature](features/regular-a-altura-da-mesa.md),
  [ADR-0002](decisions/0002-modelar-a-sala-em-metros-do-vr-e-escalar-para-o-desktop.md)).
  - `TableView` monta um tampo de nogueira com friso, a saia, quatro pés e
    ponteiras metálicas.
  - Uma placa inclinada no tampo, à esquerda do jogador, tem Subir e Descer,
    um indicador de nível e a altura em centímetros.
  - A mesa sobe ou desce um passo com o tabuleiro em cima: os pés ficam no
    chão, as pernas crescem e a câmera não se move. Há limite mínimo e
    máximo, e a escolha fica em `PlayerPrefs`.
  - A placa acompanha o lado de quem joga e responde ao mouse e ao raio do
    controle.
  - `BoardView.SetSurfaceOffset` move o tabuleiro com a mesa, e
    `BoardView.FitVrRoomToMode` escala a sala, modelada em metros do VR, para
    o desktop.
  - O harness `XRDesktopVerification` passou a considerar a altura atual da
    mesa.

- [Tela de resultado](glossary.md) ao fim da partida
  ([feature](features/mostrar-o-resultado-da-partida.md)): um diálogo modal no
  estilo dos diálogos de promoção e de falha da IA, que aparece 0,8 s depois do
  fim da partida para o lance final pousar no tabuleiro. Mostra o tipo de
  resultado, quem venceu ("Você venceu!", "A IA venceu", "Brancas vencem",
  "Pretas vencem" ou "Empate"), uma mensagem, a quantidade de lances e o lance
  final, com uma faixa amarela quando o jogador vence. Oferece Jogar novamente
  (mesma configuração), Ver tabuleiro e Voltar ao menu, recebe o foco e
  bloqueia a barra de ações enquanto está aberta. Com a tela oculta, o painel
  de turno mostra o resultado e o botão Cancelar vira Resultado para reabri-la.
  `MenuReviewCapture` ganhou o estado `game-over`.

- Em VR, a peça ao alcance da mão (a que o gatilho do indicador agarraria)
  ganha um contorno laranja: `PieceFactory` acrescenta um `PieceGrabHighlight`
  a cada peça quando um headset está presente. Ele mostra, sobre cada malha da
  peça, uma cópia com as faces voltadas para dentro, um pouco inflada, num
  material sem iluminação (`Resources/XR/GrabOutlineMaterial`, shader
  `ChessCgi/GrabOutline`), o que custa quase nada em GPUs modestas e vale
  para as peças personalizadas e para as clássicas do modo desempenho. O
  contorno só aparece quando `ChessGameController.CanGrabPiece` permite e
  some ao agarrar a peça ou tirar a mão do alcance. Sem mudança no modo
  desktop. Não é o [destaque](glossary.md) de destino legal.
- Modo desempenho: `PieceFactory` ganha uma flag `usePrimitivePieces` que faz
  `CreatePiece` construir peças clássicas mesmo quando há um prefab de peça
  personalizada definido, um ganho em GPUs modestas. O `GameHud` acrescenta uma
  caixa de seleção "Modo desempenho" ao menu inicial — presente no desktop e no
  VR, já que o menu é o mesmo — que reflete o estado atual e chama
  `ChessGameController.SetPerformanceMode`. O padrão é ligado; o jogador pode
  desmarcá-lo no menu para voltar às peças personalizadas.
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

### Changed

- Em VR, as peças ganham um XR Grab Interactable (com Rigidbody kinematic) e
  ficam na layer `PieceView.PhysicsLayer` (nomeada "ChessPieces" no
  TagManager), que o raio distante dos controles ignora; as casas deixaram de
  receber um XR Simple Interactable. `BoardView.TryGetSquareAt` traduz uma
  posição no mundo em casa do tabuleiro, para saber onde uma peça foi solta.
  Sem mudança no modo desktop; veja
  [play-in-vr](../interaction/features/play-in-vr.md).
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

- Construção visual do menu separada de escolhas, foco e ajuste ao Canvas. HUD e palco divididos em etapas nomeadas; cenários de captura do Editor declarados junto de suas dimensões e foco esperado. Composição aprovada preservada.

- Menu com escolhas explícitas de modo, lado e três dificuldades, estado selecionado e resumo antes da partida.
- HUD, promoção, recuperação de falha e ajuda com tipografia ampliada e identidade visual comum.
- Ajuda bloqueia os botões de fundo; a configuração é preservada ao fechar e ao retornar ao menu.
- A composição do menu se ajusta às dimensões do Canvas desktop ou world-space; o modo local oculta as opções exclusivas da IA.
- Direção Palco da turma: cores Feevale, assinatura original, tipografia Lato e elenco real em preview 3D isolado; trocar lado move suavemente o palco, sem alterar a partida.
- Navegação por teclado inicia com foco visível e transfere o foco para ajuda, promoção e recuperação de erro.
- O palco 3D do preview da peça selecionada não herda a escala do Canvas, mantendo o enquadramento em Canvas de tela ou world-space.
- Redistribuição do menu: assinaturas no cabeçalho, elenco central e faixa inferior com modo, lado, dificuldade e ação de jogar; navegação explícita acompanha os grupos horizontais.
- O menu destaca Marta (brancas) ou Ricardo (pretas) conforme o lado selecionado; no modo local, ambos recebem o mesmo destaque. O palco usa iluminação própria e os dois modelos existentes.
- Corrigida a proporção da assinatura Feevale desativando o redimensionamento NPOT na importação.
- Foco de navegação usa contorno claro, separado do valor escolhido; textos pequenos preservam leitura nas dimensões menores do Game view.

- Menu Mesa de partida, aprovado por imagem em 22/09/2026: cenário com tabuleiro, professores 3D à esquerda e painel de configuração à direita. A logo considera os limites visíveis do PNG para centralizar sobre o painel. Seleção usa contorno e confirmação; uma descrição explica a dificuldade. Nomes acompanham as bases sobre sombra suave. Interação uGUI e Canvas world-space preservados.
- Jogar recebe um contorno de foco independente da seleção; Intermediário selecionado cabe em uma linha na janela de 1024×768.
- A caixa de seleção "Modo desempenho" passou para a linha inferior do painel do menu Mesa de partida, ao lado de "Como jogar", com o mesmo estilo de seleção e foco por teclado das demais escolhas; continua no desktop e no VR.
- O painel do HUD em world-space acompanha o assento de VR: quando o jogador joga de pretas contra a IA, o painel passa para o lado oposto do tabuleiro e continua de frente para ele.

### Fixed

- Raio do VR grudando no HUD do fundo:
  - O `TrackedDeviceGraphicRaycaster` do HUD passa a checar oclusão 3D em
    todas as camadas.
  - Em VR, só os controles do HUD da partida recebem o raio. Os painéis
    decorativos e o preview da peça deixam de ser alvo.
  - O tampo da mesa ganha um collider para barrar o raio.
  - O painel VR do HUD sobe de 1,4 para 1,75 m, com a borda de baixo no chão,
    para a barra de ações não ficar atrás da mesa.
  - `XRHudVerification` confere essa configuração e, de ponta a ponta, que um
    collider entre a mão e o HUD impede o raio de chegar a Nova partida. Ao
    sair, ele também devolve a configuração do XR Simulator.
- A sala era montada no `Awake`, e o XR Simulator só liga o headset depois
  disso. Assim, a mesa ficava na escala do desktop dentro do VR simulado.
  Agora o `ScenePolish` remonta a sala quando o modo muda.

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
