# Changelog

O que mudou no contexto de interação, lançamento mais recente primeiro, no
formato [Keep a Changelog](https://keepachangelog.com/en/1.1.0/): um H2 por
lançamento, escrito como "## [1.2.0] - 2026-06-30" com " [YANKED]" ao final
se o lançamento foi retirado, contendo Added, Changed, Deprecated, Removed,
Fixed ou Security como H3s, cada um deles uma lista de itens.

## [Unreleased]

### Added

- Atualização 0.1.1: solturas lentas sobre a casa verde não são recusadas
  apenas pela altura. A observação inclui o tempo estimado de queda, com
  prazo máximo de 2 s, mantendo pouso real, mesma casa, abertura deliberada
  e cancelamentos. A proteção entre peças continua durante essa tentativa
  para uma captura não empilhar a peça sobre a adversária.

- Hastes de ajuste aceitam mão fechada pelos quatro dedos, além da pinça,
  com pose da palma independente do sinal Meta Aim e do polegar. O sinal
  adicional só seleciona a haste próxima; peças e HUD mantêm o leitor de
  pinça original. Saída do menu, pausa, foco, rastreamento e modalidade
  cancelam sem soltura duplicada. Os nove replays das hastes passaram na
  rodada conjunta de 51 testes; o gesto físico no Quest aguarda o reteste.

- Candidato de entrega de 26/10: solturas lentas com mãos até 10 cm acima de
  um destino legal observam o pouso por até 600 ms. Replays de 5/9 cm e de
  retirada de dedo em 320 ms reproduziram a recusa anterior. O lance ainda
  exige contato real, mesma casa legal e ausência de apoio da mão; arremesso,
  perda de rastreamento, foco e pausa continuam cancelando a tentativa.

- No reteste de 23/10 do candidato `4751f26e…`, o usuário confirmou acesso ao
  menu por pinça e destino/colocação do peão funcionando. A captura parcial
  também reteve clique em Jogar e dois lances iniciados. As sondas temporárias
  foram arquivadas e removidas, preservando a lógica funcional. Tremor e
  fluidez permanecem fora do aceite deste teste. A suíte completa da versão
  normal passou 218/218 PlayMode e 64/64 EditMode após a limpeza.

- A pinça do menu passa a usar o mesmo sinal da pega (0,75/0,55), com
  cancelamento do ponteiro quando há perda de rastreamento, foco, pausa ou
  troca de modalidade. O replay do menu real falhou nas duas mãos com força
  0,85 e passou após a correção. A rodada conjunta passou 64/64 casos,
  incluindo começo de partida pelo menu, interrupções no mesmo frame,
  colocação, apoio na palma e proteção das duas mãos. Reteste no Quest pendente.

- Destino de colocação mostra a casa sob a base da peça segurada, indicando
  legalidade ou retorno à origem. Os replays de colocação, captura e promoção
  reproduziram o arco visual de 18 cm após uma soltura aceita. O caminho
  físico passa a concluir com encaixe curto; um novo movimento cancela a
  animação anterior da mesma peça. A escala da raiz física permanece estável
  durante a pega. O desvio intermitente observado no Quest ainda exige a
  captura de aquisição e poses imediatamente anteriores à soltura.

- No teste físico de `7ff45c0…`, o usuário confirmou proteção das duas mãos
  e apoio sem afundamento visível. A colocação ainda recusa o lance após
  soltar, e há tremor inclusive no ar longe das peças. A revisão permanece
  parcialmente aceita; captura dirigida no aparelho deve separar as causas.

- Reteste do candidato `7765154…`: a soltura funciona, mas o usuário pediu
  proteger também a mão livre e relatou recusa de colocação e pequeno
  afundamento na palma. Qualquer pega física agora protege as duas mãos até
  terminar a última. Colocação lenta observa por até 250 ms o pouso real na
  mesma casa legal, com assistência de velocidade restrita a essa intenção.
  O apoio visual corrige translação e rotação; a palma ganha 0,5 mm por face.
  Aceite no aparelho permanece necessário.

- Revisão da pega no Quest em 23/10: leitura exclusiva da pinça evita que o
  gesto de agarrar de outro provider mantenha a peça presa. A mão ocupada e
  sua peça deixam de derrubar vizinhas, com restauração após separação.
  Colocação com mãos usa a base real e movimento recente, preservando
  arremessos, casas legais e controles. Apoio visual acompanha a translação
  física interpolada; restrição dos botões é resolvida após essa correção.
  Reteste físico permanece necessário.

- [Agarrar e apoiar peças com as mãos](features/agarrar-e-apoiar-pecas-com-as-maos.md):
  pinça iniciada antes de alcançar a peça passa a adquirir o alvo próximo;
  a primeira mão conserva a posse até abrir. Palma e dedos têm um corpo
  composto para sustentar peças soltas, com rearme após perda de rastreamento
  e sem transformar o simples contato em lance. A sensação no Quest ainda
  exige validação física desta nova interação. A revisão de 23/10 acrescenta
  pinça analógica com histerese, abertura obrigatória após perda de foco/pausa
  ou rastreamento, contato que derruba peças sem lance e distinção entre
  arremessar no ar e colocar devagar sobre uma casa.

- Aceite físico da base Quest em 22/10/2026: o usuário confirmou pegar uma
  peça, efetuar um lance e voltar ao menu normalmente na versão de demonstração.

- [Raio de seleção](glossary.md) ocioso: com a mão vazia e apontando para o
  painel do HUD, o raio aparece mais escuro até tocar um botão, quando volta
  ao brilho normal. Tabuleiro, peças e mesa barram a mira, e segurar ou
  tocar uma peça esconde o raio ocioso.

- [Ajustar o tabuleiro com duas mãos](features/ajustar-o-tabuleiro-com-duas-maos.md):
  duas hastes metálicas de proximidade controlam o tamanho continuamente;
  uma mão sozinha, entrada do mesmo lado e o raio do HUD não iniciam o gesto.
  Rastreamento inválido, pausa, recentralização e estados ocupados da partida
  interrompem o ajuste e exigem novas agarradas.

- [Pressionar os botões da mesa no VR](features/pressionar-os-botoes-da-mesa-no-vr.md):
  contatos cinemáticos no controle e no indicador empurram as capas dinâmicas,
  sem gatilho. Perda de rastreamento e reposicionamentos desarmam o contato
  até que ele esteja estável fora do botão. Os proxies não empurram peças nem
  bloqueiam os raios de interação do HUD.

- Vibração no [agarrar e soltar](glossary.md): o `XRRig` põe um
  `HapticImpulsePlayer` em cada controle, ligado direto à saída de vibração
  do OpenXR (`<XRController>{mão}/{Haptic}`), porque o rig é montado em
  código sem asset de input. O `VrSelectionBridge` vibra de leve ao agarrar
  (0,3 × 40 ms) e ao soltar num lance aceito (0,5 × 70 ms), e forte e longo
  quando a jogada é recusada (0,85 × 200 ms). O `XRGrabVerification` confere
  essa ligação nos dois controles.

- [Olhar ao redor no desktop](features/olhar-ao-redor-no-desktop.md): botão
  direito gira a visão no assento; R/Esc ou o botão do HUD volta ao tabuleiro.
  O cursor é liberado ao soltar, perder foco ou sair do componente. Observar
  a sala não provoca seleção acidental nem é interrompido pela troca de turno.

- Pacotes de XR (XR Plugin Management, OpenXR Plugin, XR Interaction Toolkit)
  resolvidos no projeto para a fase de conversão para VR no HTC Vive; veja
  [ADR-0001](decisions/0001-usar-openxr-e-o-xr-interaction-toolkit-para-o-modo-vr.md).
- `XRRig` constrói um XR Origin (VR) em código, em tempo de execução, quando
  um headset está presente, para que a câmera do olho rastreie a pose do
  headset em vez do `CameraController` de desktop; veja
  [play-in-vr](features/play-in-vr.md). O modo desktop permanece inalterado
  quando nenhum headset está presente.
- `XRRig` constrói um Near-Far Interactor com um raio visível em cada
  controle de movimento, vinculado ao gatilho para selecionar. Um novo
  componente `VrSelectionBridge` transforma o evento de seleção de um XR
  Simple Interactable em uma peça ou casa nas mesmas chamadas
  `ChessGameController.SelectPiece` / `SelectSquare` que o caminho de mouse
  de desktop faz; veja [play-in-vr](features/play-in-vr.md).
- `ChessGameController` aposenta o giro de câmera por turno quando um
  headset está presente, já que o modo VR é de assento único. A órbita
  (Q/E) e o zoom por scroll continuam disponíveis em VR, agora de posse do
  `XRRig`: agem sobre o XR Origin do rig, ao redor do tabuleiro e dentro de
  uma faixa de distância própria da escala de VR. O `CameraController` passa
  a ser exclusivo do desktop; veja [play-in-vr](features/play-in-vr.md).
- Pacote `com.unity.xr.hands` e a feature OpenXR **Hand Tracking Subsystem**
  habilitados para Standalone. `XRRig` constrói um interactor de mão para
  cada lado, extraído das amostras oficiais do XR Interaction Toolkit
  (Starter Assets + Hands Interaction Demo), e um `XRInputModalityManager`
  alterna automaticamente entre esses interactors e os de controle
  existentes conforme o que o runtime relata como presente; veja
  [play-in-vr](features/play-in-vr.md) e
  [rastreamento de mãos](glossary.md).
- O **Oculus Touch Controller Profile** foi habilitado para Standalone ao lado
  do HTC Vive Controller Profile, então uma mesma build detecta e usa o
  dispositivo presente automaticamente — Vive pelo runtime OpenXR do SteamVR,
  Oculus Rift pelo runtime da Oculus ou pelo SteamVR — sem caminho de código
  por fabricante; veja
  [ADR-0001](decisions/0001-usar-openxr-e-o-xr-interaction-toolkit-para-o-modo-vr.md).
  O Rift usa os [controles de movimento](glossary.md) Touch (raio de seleção e
  gatilho), não rastreamento de mãos articulado, que a Rift não possui.
- Cada [controle de movimento](glossary.md) carrega um modelo de mão
  (`Resources/XR/LeftControllerHand` / `RightControllerHand`) que segue a
  pose do controle, para que o jogador veja as mãos mesmo sem rastreamento
  de mãos articulado, como na Rift com os Touch. Os prefabs são gerados por
  **Chess CGI → VR → Create Controller Hands And Ray Material**.
- No Oculus Rift, o jogador [agarra e solta](glossary.md) as peças com a mão em
  vez de apontar um raio. Segurar o grip do [controle de movimento](glossary.md)
  perto de uma peça da vez a agarra e mostra seus destinos legais; soltá-la
  sobre um destino legal faz a jogada, e soltá-la em qualquer outro lugar
  conta como jogada inválida e a peça volta à casa de origem. O
  `VrSelectionBridge` agora liga o evento de agarrar e soltar de um XR Grab
  Interactable a `ChessGameController.GrabPiece` / `ReleasePiece`; veja
  [play-in-vr](features/play-in-vr.md).
- Contra a IA jogando de pretas, o assento de VR gira 180° em torno do tabuleiro, para o jogador ver a partida do lado das pretas; ao voltar ao menu ou jogar a dois, volta ao lado das brancas. O `XRRig` acompanha o estado do controlador e preserva a órbita e o zoom já feitos.

### Changed

- [Agarrar e soltar](glossary.md): soltar uma peça fora de um destino legal
  não a devolve mais à casa na hora. O `VrSelectionBridge` congela a peça só
  quando a solta começa um lance; nos outros casos ela fica solta, sob a
  física, e volta depois de 3 segundos.

- O raio dos controles e das mãos no VR deixou de ser magnético: o
  `CurveInteractionCaster` usa `Raycast` em vez do `ConeCast` padrão do XRI
  (3° nos controles, 6° nos prefabs de mão), e o `CurveVisualController` não
  encaixa mais a ponta da linha no alvo selecionado nem em snap volumes. O raio
  só acerta o que está exatamente na mira e a linha fica reta. O
  `XRGrabVerification` confere isso nos dois controles e nas duas mãos.

- Os [botões da mesa](features/pressionar-os-botoes-da-mesa-no-vr.md) recebem
  contatos da palma e dos cinco dedos. A mão desenhada apoia na face interpolada
  da capa sem deslocar a mira ou o ponto de pega. Perda de rastreamento e
  mudanças de altura desarmam a mão inteira até sua retirada.

- O assento de VR foi reposicionado para uma vista de mesa: `XRRig` senta o
  jogador logo à frente e acima de um tabuleiro em escala de mesa, em vez de
  colocá-lo dentro de um tabuleiro em escala de sala. O modo desktop permanece
  inalterado. Veja
  [ADR-0002](decisions/0002-ver-o-tabuleiro-como-uma-mesa-a-partir-de-um-assento.md).
- Os modos VR e desktop ficaram independentes na câmera e nos controles. O
  `CameraController` não age mais em VR (retorna cedo quando um headset está
  presente) e perdeu os campos de distância de VR, que migraram junto com a
  órbita/zoom para o `XRRig`. Voltou aos valores originais de terceira pessoa,
  para o tabuleiro em escala 1 na origem. A seleção de desktop faz raycast a
  partir da Main Camera e a de VR a partir da Eye Camera, cada modo com a sua
  câmera, de modo que mexer em um modo não afete o outro.

- O [raio de seleção](glossary.md) deixou de escolher peças e casas: os
  controles selecionam com o grip (não mais o gatilho) e só o alcance próximo
  agarra peças. O raio distante ficou só para o HUD, e só é desenhado quando
  aponta para ele; o gatilho continua clicando nos botões do HUD. As casas do
  tabuleiro deixaram de ser interactables, e os interactors de mão seguem a
  mesma regra. Removida a verificação `XRControllerVerification`, que testava
  o fluxo antigo de apontar e puxar o gatilho; o `XRGrabVerification` a
  substitui.
- Contra a IA, a câmera desktop permanece na perspectiva do lado humano.
- O controlador central bloqueia seleções humanas no turno do computador, independentemente de mouse ou raio VR.
- Agarrar uma peça com o [controle de movimento](glossary.md) passou do grip para o gatilho do indicador, o mesmo botão do clique de UI (o raio distante só age sobre o HUD, então os dois usos não se cruzam). Enquanto segura uma peça, o modelo de mão do controle fecha na pose de pinça, e reabre ao soltar; o `ControllerHandPose` faz essa transição sobre os ossos da mão.

### Fixed

- O [raio de seleção](glossary.md) das mãos usa a mesma origem rastreada da
  seleção do menu. O ponto auxiliar de pinça dos prefabs não recebia eventos
  e deixava a linha nascer perto da cabeça; o rig remove seu override visual.
  A linha também respeita a desativação do farcasting durante gestos de toque.
  Testes PlayMode reproduziram e cobrem origem, movimento independente da
  cabeça e ocultação nas duas mãos. O usuário confirmou origem e seleção
  por pinça funcionando no Quest 3 em 21/10/2026.

- Duplicação de mãos na primeira sessão do Quest 3: o `XRRig` configura o
  seletor de entrada antes de ativá-lo, e cada visual rastreado pertence ao
  grupo de seu interactor. Alternar para controles oculta o visual da mão e
  desarma seus contatos físicos. Testes PlayMode cobrem abertura com entrada
  já rastreada e transições. O usuário confirmou um único par nos dois modos
  no Quest 3 após reinstalar o APK em 21/10/2026.

- O alcance próximo das mãos rastreadas inclui a camada das peças do jogo.
  Os prefabs só consultavam a camada padrão, deixando as peças fora da detecção;
  o rig agora consulta só peças e hastes nos dois lados, com raio de 6 cm,
  evitando que casas e cenário ocupem o buffer antes dos alvos agarráveis.

- O [raio de seleção](glossary.md) não gruda mais no HUD do fundo em VR.
  Antes, o raio atravessava o tabuleiro e a mesa e pegava os painéis do HUD
  a 4 m; às vezes acionava Nova partida ou Menu ao mirar numa peça. Agora só
  os botões do HUD recebem o raio, e tabuleiro, peças e mesa o barram. A
  correção está no HUD e na mesa (ver o changelog de
  [apresentação](../presentation/changelog.md)).

- O [raio de seleção](glossary.md) dos controles voltou a ser desenhado. Ele
  passou a usar um `CurveVisualController` (com `LineRenderer`) em vez de um
  `XRInteractorLineVisual`, que exigia um componente `ILineRenderable` que o
  Near-Far Interactor não fornece e enchia o console com erros de
  "Missing ILineRenderable".
- Na build de Windows, os [controles de movimento](glossary.md) não apareciam
  e não interagiam com nada (nem raio, nem menus). O `XRRig` buscava o shader
  do [raio de seleção](glossary.md) com `Shader.Find`, mas nenhum material
  usava "Universal Render Pipeline/Unlit", então a build o removia;
  `new Material(null)` lançava uma exceção no meio da construção do rig, e o
  rig tentava de novo a cada frame. O raio agora usa o material
  `Resources/XR/ControllerRayMaterial`, que entra na build, e o `XRRig`
  marca o rig como construído antes de montá-lo, para que uma falha não o
  reconstrua a cada frame.
- Cliques sobre a interface não selecionam peças por trás do HUD; o atalho N fica inativo no menu.

- O contato da ponta do indicador foi calibrado no modelo importado para reduzir o afastamento visual durante a pressão, mantendo a proteção contra atravessar a capa.
