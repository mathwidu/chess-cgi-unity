# Changelog

O que mudou no contexto de apresentação, lançamento mais recente primeiro, no
formato [Keep a Changelog](https://keepachangelog.com/en/1.1.0/): um H2 por
lançamento, escrito como "## [1.2.0] - 2026-06-30" com " [YANKED]" ao final
se o lançamento foi retirado, contendo Added, Changed, Deprecated, Removed,
Fixed ou Security como H3s, cada um deles uma lista de itens.

## [Unreleased]

### Added

- [Marca do último lance](glossary.md) e [rei em xeque](glossary.md)
  ([feature](features/sentir-cada-lance.md)): `BoardView.MarkLastMove` tinge
  de amarelo suave as casas de origem e destino do último lance, e
  `BoardView.MarkCheck` tinge de vermelho a casa do rei em xeque. A tinta é
  misturada à cor da própria casa com um `MaterialPropertyBlock`, sem
  materiais novos. O controlador marca quando o lance pousa, e as marcas
  somem numa nova partida.
- [Sons do tabuleiro](glossary.md): `BoardSounds`, montado pelo `BoardView`,
  toca um som quando cada lance pousa, avisado pelo evento
  `ChessGameController.MoveApplied`. São toque (lance), toque duplo (captura),
  carrilhão (xeque), sinal suave (a IA devolveu a vez) e frases de vitória,
  derrota ou empate. Os clipes são sintetizados em código no primeiro uso,
  sem arquivos de áudio nem licenças. No VR o som é 3D e sai do tabuleiro; no
  desktop é 2D.

- [Peças capturadas](glossary.md) ao lado do tabuleiro
  ([feature](features/ver-as-pecas-capturadas.md)):
  - `CapturedPiecesView`, montado pelo `BoardView` em unidades locais do
    tabuleiro, põe miniaturas das peças tomadas sobre duas faixas de feltro à
    direita de quem está sentado.
  - Suas capturas ficam na metade perto de você, e as do adversário na de lá,
    em 3 × 5 lugares, com as mais valiosas primeiro.
  - A peça capturada voa da casa até o lugar dela quando o lance termina.
  - Um "+N" amarelo marca quem está à frente em material.
  - As miniaturas vêm de `PieceFactory.CreateDisplayPiece`: sem collider,
    `PieceView` ou interação de VR, e trocam para peças clássicas no modo
    desempenho.
  - O conjunto troca de lado com o jogador e sobe e desce com a mesa.

- [Indicador de turno](glossary.md)
  ([feature](features/ver-de-quem-e-a-vez.md)):
  - `TurnIndicatorView`, montado pelo `BoardView` em unidades locais do
    tabuleiro, acende uma luz fina na borda do lado a jogar. A luz corre uma
    vez do centro para fora, sem animação contínua, e é amarela na vez do
    jogador e clara e neutra na vez da IA.
  - Uma etiqueta no tampo ("Sua vez", "Vez das brancas", "Vez das pretas",
    "IA pensando...") aparece e some após cerca de 3 s; a da IA fica enquanto
    ela pensa. A etiqueta fica de pé para quem está sentado e não tem
    raycaster, então não pega o raio.
  - O indicador some no menu e no fim da partida.
  - No desktop, onde o HUD já nomeia o turno, a luz é mais fina.

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

- Acabamento do tabuleiro e mesa: moldura de nogueira com quinas suaves,
  coordenadas dos dois lados, casas com mapas de madeira e tampo com veios
  mais discretos. Luz lateral acompanha as janelas e o reflexo da sala é
  calculado na inicialização. A altura das casas e peças permanece igual;
  tampo rebaixado mantém contato com a base mais espessa. Veja
  [acabamento-do-tabuleiro](features/acabamento-do-tabuleiro.md).

- Controles explícitos no preview da peça: giro para os dois lados, zoom com
  percentual e limites, restauração e instrução de arraste. O arraste vertical
  muda o ângulo de observação; a interação fica restrita ao preview. Imagem e
  câmera passam a usar a mesma proporção, e os botões ficam fora do personagem.
  Nome, tipo e casa ficam no cabeçalho; os dados do perfil não repetem o nome.
  Veja [preview-the-selected-piece](features/preview-the-selected-piece.md).

- Sala inspirada no Laboratório de Redes (102, prédio Verde): tampos de
  madeira clara, mapas de normal/rugosidade, pintura e tecidos com textura,
  janelas à direita, nichos coloridos e oclusão de contato na exportação.
  Ajuste da luz principal e preenchimento; HUD e ajuda explicam como olhar
  ao redor no PC. Veja [laboratorio-feevale](features/laboratorio-feevale.md).

- Direção 02 aplicada aos seis personagens: roupas brancas/pretas com a mesma
  geometria, texturas de identidade preservadas, logo Feevale nas costas,
  espada curta para o peão, báculo para o bispo e cetro para o rei. Bases com
  símbolos clássicos voltados para cima substituem os grandes chapéus do
  estudo descartado. Prefabs preservam os GUIDs e os GLBs originais permanecem
  disponíveis. `CustomPieceAppearance` seleciona materiais compartilhados no
  tabuleiro e no menu; o preview herda a aparência e mostra a frente nos dois
  lados. Há fonte Blender, capturas Unity e testes de escala desktop/VR;
  validação e desempenho em headset continuam pendentes. Veja
  [identificar-pecas-personalizadas](features/identificar-pecas-personalizadas.md).

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

- No VR, a peça agarrada inclina junto com o controle: o `XRGrabInteractable`
  das peças passa a seguir a rotação (`trackRotation`), mantendo a pose do
  momento em que foi pega. Ao soltar, `PieceView.MoveTo` endireita a peça
  enquanto ela desliza para a casa, no lance aceito, na jogada recusada e na
  promoção. O `XRGrabVerification` inclina o controle 40° com a peça na mão e
  confere que ela volta de pé.

- Junção do laboratório Feevale com a [mesa](glossary.md) regulável
  ([feature](features/regular-a-altura-da-mesa.md)): no laboratório, a placa
  de altura sobe e desce a mesa de xadrez do próprio laboratório (tampo sobe,
  pés em T ficam no chão), e a sala deixa de subir junto com o tabuleiro. A
  altura padrão passa a 75,57 cm, a base da nova moldura; as faixas das
  [peças capturadas](glossary.md) e a etiqueta do
  [indicador de turno](glossary.md) descem para esse tampo, e a luz do
  indicador vai para a borda externa da moldura, fora das coordenadas. O HUD
  do VR fica na posição do laboratório, (0; 1,6; 1,35).

- A etiqueta do [indicador de turno](glossary.md) aparece só no VR. No
  desktop, o painel de turno já nomeia a vez, e a etiqueta chegava a aparecer
  grande na borda de baixo em telas 16:10.

- Laboratório reconstruído a partir do vídeo e da foto do tampo fornecidos
  pelo usuário: paredes azul/cinza, janelas com faixas ocre, vigas e luminárias
  suspensas, armários sob as persianas e mesas agrupadas com notebooks.
  Iluminação passa a representar a condição interna noturna da gravação.
  O tabuleiro mantém contato com o tampo e as âncoras anteriores em PC/VR;
  dimensões completas e quantidade de móveis continuam estimadas. Veja
  [laboratorio-feevale](features/laboratorio-feevale.md).

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

- Zoom máximo do preview mantém margem acima da cabeça e dos acessórios.
  O jogador pode reposicionar a imagem com arraste direito/do meio ou com
  os botões Mover ↑/↓, mantendo zoom e orientação. Restaurar também desfaz
  o deslocamento, e o ajuste não interfere no tabuleiro nem na câmera da sala.
  Veja [preview-the-selected-piece](features/preview-the-selected-piece.md).

- Revisão dos personagens da direção 02: cabeça original do Ricardo restaurada,
  contatos dos bastões e da espada ajustados e aplicações Feevale separadas da
  textura do corpo. Símbolos clássicos completos passam a ocupar áreas livres
  da base; torre e cavalo preservam sua geometria inferior. Texturas recebem
  maior resolução de bake, relevo de tecido e importação consistente. Corrigida
  a classificação de ruído escuro da roupa como pele, que produzia manchas nas
  variantes brancas. O menu enquadra as bases completas dos dois professores.
  Auditoria geométrica e closes no Unity complementam os
  testes de escala e materiais; avaliação em headset permanece pendente.

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

- Revisão dos tecidos: corrigidas regiões de cor antiga na barra do rei,
  no cardigan da rainha, na calça do peão e ao redor dos fones do bispo.
  Novo UV de produção, com separação de regiões sobrepostas, evita que braços
  recebam textura da camisa e reduz vazamento entre cores na filtragem à
  distância; o UV de origem é preservado no Blender. Feevale passa a aparecer
  somente nas costas; letras antigas também são limpas do relevo. Painel
  e capturas passam a incluir closes brancos e pretos dos seis personagens.
- Revisão específica do cavalo: removido o corte horizontal que deixava os
  punhos e a metade inferior do moletom pretos. A máscara acompanha a cintura
  sentada e inclui a borda do capuz, preservando pele e montaria. A auditoria
  agora amostra essas quatro regiões e compara mãos/montaria com a origem.
  A avaliação visual de menu, partida, seleção e elenco está registrada em
  `art/character-variants/visual-review-20260925/`; as propostas gerais ainda
  não foram aplicadas. O diagnóstico separa a falha de cor da sombra na calça.

- Rodada de acabamento: corrigidas a cintura da cargo do peão branco e a
  gola/punho da torre; dedos dos acessórios com proporções variadas; bases com
  lateral suave e topo plano; PNG Feevale transparente somente nas costas.
  O preview passou a usar luz pontual e camada própria, evitando iluminar o
  tabuleiro e substituir a luz principal do URP. Key/fill equilibradas e sombras
  suaves no perfil PC; Mobile preservado. Verificação: 32 testes PlayMode,
  22 casos de tecido e auditorias de UV/geometria aprovados, com 33 capturas.
  Comparação e avaliação atual: `art/character-variants/finishing-review-20260925/`.
- Descoberta seguinte orientada a VR: estudo Blender de sala inspirada na Feevale,
  com tabuleiro de 45 cm sobre uma mesa, carteiras, janelas e quadro. Registro em
  `art/character-variants/classroom-discovery-20260925/`; proposta de composição,
  ainda fora da Main. Próximas prioridades: mesa/tabuleiro, seleção sem cobrir
  casas, leitura dos símbolos, ambiente e medição em headset.
- Laboratório de informática: evolução da sala a partir das fotos oficiais de
  Projetos de TI e Redes da Feevale, com bancadas claras, desktops, cadeiras
  escuras, persianas e piso amadeirado. Prefab carregado na Main pelo
  `ScenePolish`, alinhado à posição e escala do tabuleiro, com mesa central e
  circulação para os dois lados. Fonte Blender preservado; geometria agrupada
  em 26 renderers, sem colisores decorativos. Detalhes, limites e evidências em
  `features/laboratorio-feevale.md` e `art/character-variants/feevale-lab-20260925/`.
  O HUD VR foi aproximado e reduzido para permanecer à frente das bancadas,
  sem monitores cobrindo as opções do menu; o desktop mantém seu layout.
  Luzes do estúdio dos professores isoladas no tempo de render, para não
  iluminar a sala. Bias da luz principal ajustado somente na escala VR após
  comparação visual identificar faixas de auto-sombra no tampo.
