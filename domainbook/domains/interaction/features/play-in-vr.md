---
id: play-in-vr
name: Jogar em VR
status: draft
owners: [mathwidu, RafaelAugustScherer]
terms: [modo-vr, óculos-vr, controle-de-movimento, raio-de-seleção]
---

## Story

Como jogador com um headset de VR
Quero sentar diante do tabuleiro de xadrez em realidade virtual e escolher peças com as mãos
Para que o jogo que já tenho se torne uma partida imersiva em vez de uma de tela

Isto é um plano, não um trabalho construído. É um estudo de viabilidade e uma
rota de conversão de alto nível para levar a build atual de desktop ao modo VR
no HTC Vive (conectado a um PC) e no Meta Quest 3 (standalone, além de PC via
Link). Permanece no contexto de interação porque a mudança é sobretudo sobre
como o jogador percebe e controla o jogo — os [óculos VR](../glossary.md)
comandam a visão e o [controle de movimento](../glossary.md) substitui o mouse
— com trabalho de suporte no pipeline de renderização da apresentação. Quando
a rota for combinada e o framework de XR estiver escolhido, essa escolha
ganha um registro de decisão e as regras abaixo se tornam os critérios de
aceite para construí-la.

### Estudo de viabilidade

#### Veredito

Viável, e um bom encaixe. Os dois headsets alvo são alcançados por um único
caminho de código — o plugin OpenXR da Unity — e o projeto atual já está na
pilha que o ferramental de XR da Unity espera: Unity 6.3 (6000.3), Universal
Render Pipeline (URP) 17.3 e o novo Input System (1.19), sobre o qual o XR
Interaction Toolkit é construído. O jogo é estacionário e em escala de mesa,
então evita o problema mais difícil de VR — o conforto de locomoção. O grosso
do esforço é reencanar a entrada e a câmera, além de um trabalho de
desempenho standalone para o Quest.

#### Os dois headsets, um único caminho

- **OpenXR é o alvo unificado.** O OpenXR Plugin (`com.unity.xr.openxr`)
  permite que uma única build controle muitos runtimes. O HTC Vive é
  alcançado no PC como um runtime OpenXR (SteamVR); o Meta Quest 3 é
  alcançado como um runtime OpenXR tanto standalone no próprio dispositivo
  quanto via Link no PC. Os controles são selecionados em tempo de execução
  a partir da lista **Enabled Interaction Profiles**, então a mesma build
  pode listar o **HTC Vive Controller Profile** e o **Meta Quest Touch
  (Oculus Touch) Controller Profile** e usar o dispositivo presente.
- **O Quest 3 é um alvo de build separado.** É um dispositivo Android: uma
  build standalone do Quest é um app Android/ARM64, enquanto o Vive é um app
  standalone Windows. São duas builds de jogador a partir de um único
  projeto — uma build Windows PC-VR e uma build Android Quest — que diferem
  nas configurações de plataforma, não no código do jogo.

#### O que o projeto atual já nos dá

- **Colliders nas peças e casas.** `InputController` já faz raycast com
  `Physics.Raycast` contra os colliders de `PieceView` e `SquareView`. Esses
  mesmos colliders são o que um raio de controle ou um grab interactor
  atinge, então os alvos selecionáveis já existem.
- **A cena é construída em código em tempo de execução.** `BoardView`,
  `PieceFactory` e `GameHud` constroem o tabuleiro, as peças e a interface
  em scripts. Uma cena construída em código é mais fácil de reancorar e
  reescalar para VR do que uma montada manualmente.
- **Modelos customizados carregam via glTFast**, e URP + Shader Graph +
  shaders embutidos já suportam renderização estéreo single-pass instanced,
  então os modelos de peça renderizam corretamente nos dois olhos sem
  trabalho de shader.
- **O Input System já está em uso.** O XRI lê entrada por meio de ações do
  Input System; o projeto não precisa migrar da entrada legada primeiro.

#### O que precisa mudar (o trabalho de verdade)

- **A câmera deixa de ser nossa para mover.** Em VR, o head-mounted display
  comanda a câmera a cada quadro por meio de um Tracked Pose Driver sob um
  rig XR Origin. A lógica de órbita, zoom e giro-para-o-jogador-a-jogar do
  `CameraController` deixa de valer para a câmera do olho — não é possível
  mover uma câmera que o headset possui. A ideia de
  [perspectiva](../glossary.md) precisa passar de "apontar a câmera para o
  lado a jogar" para "reancorar ou girar o rig do tabuleiro", ou ser
  abandonada em favor de o jogador girar fisicamente a cabeça.
- **A seleção por mouse é substituída por interactors.** O
  raycast-de-tela-no-clique-esquerdo do `InputController` vira um interactor
  XRI: apontar um [raio de seleção](../glossary.md) para uma peça e puxar o
  gatilho, ou esticar a mão e agarrá-la diretamente. É uma reescrita do
  caminho de entrada, não um ajuste.
- **O HUD precisa sair da tela.** `GameHud` constrói um Canvas overlay em
  espaço de tela (título, turno, status, histórico de jogadas, pedido de
  promoção, tela inicial e o preview da peça selecionada). O XRI só
  consegue comandar um Canvas em **world-space**, então o HUD vira um
  painel colocado na cena, apontado com o controle. O preview em
  render-texture da peça selecionada continua funcionando; só sua entrada
  de apontador passa a vir do controle.
- **O Quest tem um orçamento de quadro apertado.** Uma GPU móvel standalone
  renderizando dois olhos a 72–120 Hz tem muito menos folga do que um PC.
  Pós-processamento e overdraw que são de graça no desktop não são no
  Quest. A cena é pequena, o que ajuda, mas um trabalho de desempenho é
  obrigatório, não opcional.

#### Esforço e risco

- **Esforço:** médio. Nenhum gameplay ou regra nova — `ChessGameController`
  e `ChessRulesAdapter` ficam intocados. O trabalho se concentra neste
  contexto (entrada, câmera) e na apresentação (configurações de
  renderização, HUD em world-space).
- **Principais riscos:** (1) refazer a perspectiva por turno para que uma
  ideia de tela compartilhada ainda faça sentido para uma pessoa em um
  headset; (2) desempenho standalone no Quest; (3) dois alvos de build para
  manter configurados e validados. Nenhum é bloqueador.

### Plano de conversão (alto nível)

1. **Adicionar os pacotes de XR e ligar o OpenXR.** Pelo Package Manager,
   adicionar o XR Plugin Management (`com.unity.xr.management`), o OpenXR
   Plugin (`com.unity.xr.openxr`) e o XR Interaction Toolkit
   (`com.unity.xr.interaction.toolkit`, que traz o XR Core Utilities
   junto). Deixar o editor resolver as versões que verifica para 6000.3 —
   no momento em que isto foi escrito, isso é XRI 3.3.2, OpenXR Plugin na
   linha 1.16.x e XR Core Utilities na linha 2.5.x. Em **Project Settings →
   XR Plug-in Management**, habilitar **OpenXR** tanto na aba
   Windows/Standalone (para o Vive) quanto na aba Android (para o Quest).

2. **Definir os perfis de interação e as configurações de plataforma.**
   - **Ambos:** em **XR Plug-in Management → OpenXR**, adicionar o **HTC
     Vive Controller Profile** e o **Meta Quest Touch (Oculus Touch)
     Controller Profile** a Enabled Interaction Profiles.
   - **Quest 3:** trocar para o **Meta Quest build platform/profile**
     (Unity 6.1+), que instala o pacote **Unity OpenXR: Meta**
     (`com.unity.xr.meta-openxr`) e habilita o **Meta Quest feature
     group**. Confirmar os padrões que ele define: Graphics API
     **Vulkan**, Scripting Backend **IL2CPP**, Target Architecture
     **ARM64**, Android mínimo **API 29**, alvo **API 32**, Stereo
     Rendering **Instancing**.
   - **Vive / PC-VR:** alvo de build Windows standalone; o runtime OpenXR
     ativo é o SteamVR. Sem restrições móveis, então há folga de
     desempenho.
   - Rodar **XR Plug-in Management → Project Validation** em cada aba de
     plataforma e resolver todo item sinalizado.

3. **Configurar o URP para estéreo.** Definir o **Render Mode** do OpenXR
   como **Single Pass Instanced** para cada provedor (recai para
   multi-pass onde não é suportado). Manter o MSAA no asset do URP para
   qualidade de borda, e permanecer dentro do conjunto de pós-processamento
   suportado em XR — Bloom, Depth of Field, Tonemapping e ajustes de cor
   funcionam em XR, enquanto Lens Distortion, Spatial-Temporal
   Post-Processing, câmera física e multi-display não. Considerar
   renderização fixed-foveated (o caminho Forward+ do URP 17 suporta) como
   uma alavanca de desempenho no Quest.

4. **Substituir a câmera por um rig XR Origin.** Trocar a câmera principal
   única por um **XR Origin (VR)**: um Camera Offset segurando a Main
   Camera, com um **Tracked Pose Driver** vinculando a pose do olho ao
   headset. Para um jogo de mesa sentado, usar o modo de tracking-origin
   **Device** e definir a altura do olho com **Camera Y Offset**, e ligar
   um controle de **recentralização** (`XRInputSubsystem.TryRecenter`) para
   que o jogador possa reposicionar o tabuleiro à sua frente. Apontar a
   referência de câmera existente do raycast/interactor para essa nova
   câmera do olho.

5. **Substituir a seleção por mouse pela interação com o controle.** Pôr
   um **Near-Far Interactor** em cada controle (ele unifica os casos
   near/direct e far/ray que antes precisavam de dois componentes) e
   adicionar as **Default Input Actions** do XRI. Dar a cada peça um
   interactable — um **XR Grab Interactable** para alcançar-e-agarrar, ou
   um **XR Simple Interactable** para apontar-e-selecionar — reaproveitando
   os colliders que `PieceView`/`SquareView` já carregam. Ligar o evento de
   seleção do interactor às chamadas existentes de
   `ChessGameController.SelectPiece` / `SelectSquare`, para que a camada de
   regras veja os mesmos comandos que vê hoje. Manter o destaque dos
   destinos legais; ele já marca casas na cena.

6. **Mover o HUD para o world space.** Definir o render mode do Canvas do
   HUD como **World Space** e posicioná-lo como um painel na cena (por
   exemplo, ao lado ou acima do tabuleiro). Adicionar um **Tracked Device
   Graphic Raycaster** ao Canvas e trocar o Standalone Input Module do
   EventSystem pelo **XR UI Input Module**, para que o raio do controle
   acione botões e o pedido de promoção. O preview em render-texture da
   peça selecionada permanece; apenas sua entrada de arrastar/rolar passa
   do mouse para o controle.

7. **Refazer a perspectiva por turno para VR.** Decidir o que "a visão
   fica voltada para o lado a jogar" significa para um único jogador em um
   headset. Provavelmente o rig do tabuleiro gira 180° entre os turnos, ou
   as peças/rótulos se reorientam, em vez de a câmera se mover. Aposentar a
   órbita/zoom/giro do `CameraController` na câmera do olho; o que
   sobreviver disso atua sobre o XR Origin ou o tabuleiro, não sobre o HMD.

8. **Validar, compilar e testar no dispositivo.** Rodar novamente a
   validação do projeto, compilar o player Windows PC-VR e o player
   Android Quest, e testar cada um em seu hardware. Fazer um trabalho de
   desempenho no Quest — manter a taxa de atualização alvo, observar
   draw calls e overdraw, e apoiar-se em single-pass instanced e
   renderização foveada.

## Rule: O jogador vê e controla a partida de dentro de um headset

Estes são os critérios de aceite para quando este rascunho for construído.

```gherkin
Example: O headset comanda a visão
  Given o modo VR está rodando em um headset conectado
  When o jogador move a cabeça
  Then a câmera do olho segue a pose do headset
  And o giro de câmera por turno não move mais a câmera do olho

Example: Apontar para uma peça e puxar o gatilho a seleciona
  Given é a vez do jogador no modo VR
  When o jogador aponta o raio do controle para uma de suas peças e puxa o gatilho
  Then essa peça é selecionada
  And seus destinos legais são destacados, como em um clique de mouse

Example: A mesma camada de regras recebe os mesmos comandos
  Given uma peça está selecionada no modo VR
  When o jogador aponta para uma casa destacada e puxa o gatilho
  Then a jogada chega às regras pelo comando existente de escolha de destino
  And o resultado corresponde ao da build de desktop
```

**Construído:** os três exemplos acima. `XRRig` constrói um XR Origin (VR) em
tempo de execução quando um headset está presente — um Camera Offset segurando
a câmera do olho, um Tracked Pose Driver vinculado ao dispositivo genérico
`<XRHMD>` para rastrear tanto um headset real quanto o XR Device Simulator, o
modo de tracking-origin Device, e um controle de recentralização sobre
`XRInputSubsystem.TryRecenter`. O giro de câmera por turno é aposentado
quando um headset está presente: `ChessGameController` deixa de chamar
`CameraController.SetPerspective` a cada troca de turno nesse modo, já que o
modo VR é de assento único (contra um futuro oponente de IA, não hot-seat) e
não há um segundo lado para o qual virar a visão. As teclas de órbita (Q/E) e
o zoom por scroll do `CameraController` continuam funcionando em VR, mas
passam a girar e aproximar o XR Origin — `XRRig.Origin` — em vez da câmera do
olho, que só o headset comanda; o jogador pode assim reposicionar seu assento
ao redor do tabuleiro se quiser, dentro de uma faixa de distância própria
para a escala de VR, mais perto do tabuleiro do que a órbita externa do
desktop. O modo de desktop não é afetado quando nenhum headset está
presente.

`XRRig` também constrói um [raio de seleção](../glossary.md) — um Near-Far
Interactor, apenas com projeção far, já que o assento fica a uma distância de
mesa do tabuleiro — em cada [controle de movimento](../glossary.md), rastreado
da mesma forma genérica por meio de `<XRController>{LeftHand}` /
`{RightHand}`, e mostra o raio com um line visual. A seleção é vinculada ao
botão de gatilho, não ao grip binding padrão do XRI, para corresponder a
"puxa o gatilho" no texto da regra acima. `BoardView` e `PieceFactory` dão a
cada casa e peça um XR Simple Interactable quando um headset está presente,
reaproveitando os mesmos colliders que o raycast de desktop do
`InputController` já atinge; um novo componente `VrSelectionBridge` escuta o
evento de seleção desse interactable e chama `ChessGameController.SelectPiece`
/ `SelectSquare` — as mesmas duas chamadas que o caminho de clique de desktop
faz — para que a camada de regras veja comandos idênticos nos dois modos.

## Rule: A interface vive no mundo, não na tela

```gherkin
Example: O HUD é um painel em world-space que o controle pode usar
  Given o modo VR está rodando
  When o jogador aponta o raio do controle para o botão de nova partida no painel do HUD
  Then o botão responde ao controle, não a um mouse
  And nenhum overlay em espaço de tela é mostrado ao headset

Example: A promoção é escolhida com o controle
  Given um peão chega à última linha no modo VR
  When o pedido de promoção aparece em world space
  Then o jogador escolhe dama, torre, bispo ou cavalo com o raio do controle
```

**Construído:** o primeiro exemplo acima. `GameHud` põe o render mode do seu
Canvas em World Space quando um headset está presente, escalado e
posicionado como um painel ao lado do tabuleiro, com um Tracked Device
Graphic Raycaster no lugar do Graphic Raycaster de tela e o XR UI Input
Module da interação no lugar do Input System UI Input Module do desktop; o
Canvas usa a câmera do olho de `XRRig` como sua world camera, para que o
raycast de UI resolva contra o mesmo ponto de vista que o jogador enxerga.
Como o painel reaproveita o mesmo layout ancorado que o modo desktop já
constrói, todo botão do HUD — incluindo o pedido de promoção do segundo
exemplo — herda a mesma interação por raio sem trabalho por botão; só o
primeiro exemplo tem uma checagem automatizada dedicada até aqui. Verificado
via CLI batchmode Play mode: apontar o raio do controle direito para o botão
"Jogar" da tela inicial e segurar o gatilho aciona o clique e oculta o
overlay, do mesmo raycast world-space até o mesmo evento `onClick` que o
clique de mouse do desktop usa.

## Rule: A mesma build atende HTC Vive e Meta Quest 3

```gherkin
Example: Uma build lê qualquer controle presente
  Given a build lista os perfis de interação do HTC Vive e do Meta Quest Touch
  When o jogador a executa em um Vive ou em um Quest 3
  Then o OpenXR vincula o controle do dispositivo em uso
  And a entrada funciona sem um caminho de código separado por headset

Example: O Quest roda standalone dentro do seu orçamento de quadro
  Given a build Android roda no Meta Quest 3 com renderização single pass instanced
  When uma partida é jogada
  Then o aplicativo mantém sua taxa de atualização alvo
```

## Open Questions

Nenhuma.

### References

Documentação oficial da Unity em que o estudo se baseia (Unity 6.3 / 6000.3 e a
documentação correspondente dos pacotes):

- XR overview and project setup — https://docs.unity3d.com/6000.3/Documentation/Manual/XR.html
- XR Interaction Toolkit 3.3.2 for 6000.3 — https://docs.unity3d.com/6000.3/Documentation/Manual/com.unity.xr.interaction.toolkit.html
- Near-Far Interactor — https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@3.3/manual/near-far-interactor.html
- XR Grab Interactable — https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@3.3/manual/xr-grab-interactable.html
- World-space UI setup (Tracked Device Graphic Raycaster, XR UI Input Module) — https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@3.0/manual/ui-setup.html
- XR Origin (rig, Camera Offset, tracking origin modes) — https://docs.unity3d.com/Packages/com.unity.xr.core-utils@2.5/manual/xr-origin.html
- Recenter tracking — https://docs.unity3d.com/ScriptReference/XR.XRInputSubsystem.TryRecenter.html
- HTC Vive Controller Profile — https://docs.unity3d.com/Packages/com.unity.xr.openxr@1.15/manual/features/htcvivecontrollerprofile.html
- Meta Quest support (OpenXR plugin) — https://docs.unity3d.com/Packages/com.unity.xr.openxr@1.16/manual/features/metaquest.html
- Unity OpenXR: Meta, project setup — https://docs.unity3d.com/Packages/com.unity.xr.meta-openxr@2.1/manual/get-started/project-settings.html
- Meta Quest build platform and Player defaults (6.3) — https://docs.unity3d.com/6000.3/Documentation/Manual/xr-meta-quest-build-profile.html
- URP compatibility in XR — https://docs.unity3d.com/6000.3/Documentation/Manual/xr-render-pipeline-compatibility.html
- Single-pass instanced / stereo rendering — https://docs.unity3d.com/6000.3/Documentation/Manual/SinglePassStereoRendering.html
- Foveated rendering (URP 17 Forward+) — https://docs.unity3d.com/6000.3/Documentation/Manual/xr-foveated-rendering.html
