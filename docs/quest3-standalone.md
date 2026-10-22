# Quest 3 standalone

## Direção acordada em 17/10/2026

O jogo deve ser instalado e executado diretamente no Meta Quest 3. A primeira
etapa preserva a sala virtual Feevale, os personagens, as regras e a interação
de mesa já existentes. Posteriormente, o jogador poderá escolher também um
modo de realidade mista: tabuleiro no ambiente real, visível por passthrough.
Esse segundo modo é uma extensão planejada; não substitui o cenário virtual.

Base desta frente: `a2e1cd0bb04f8af57600354e037a6ad00e625fe6`, ponta de
`origin/feat/board-model-plus` conferida no GitHub em 17/10/2026, incluindo as
melhorias de peças soltas, roque e mira do HUD. A branch de trabalho é
`codex/quest3-standalone-offline-ai`, criada a partir da preparação local em
`codex/quest3-standalone`. Os estudos locais de animação foram preservados.

## Preparação e limites

- Unity `6000.3.16f1`, URP `17.3.0`, OpenXR `1.16.1`, XR Interaction Toolkit
  `3.3.2` e XR Hands `1.9.0`; manter as versões declaradas no projeto.
- O mesmo projeto produz binários diferentes para desktop e Android. O loader
  OpenXR e os recursos do Quest precisam ser configurados na plataforma Android.
- Em 21/10/2026 o usuário confirmou o modo de desenvolvedor liberado. O Quest 3
  foi reconhecido pelo ADB como autorizado, recebeu o APK e abriu o jogo em VR.
  A primeira sessão revelou mãos duplicadas; a validação física está em andamento.
- **Contra IA tem adaptação Android desde 19/10/2026.** O candidato inclui
  Stockfish 18 ARM64 e usa processo Java/UCI. O processo do motor foi observado
  como filho do jogo no Quest; partida offline e ciclo de pausa/reinício ainda
  precisam ser validados. Veja [implementação e evidências](quest3-offline-ai.md).
- A verificação da interface, conforto, escala, iluminação e desempenho no Quest
  exige o aparelho. Simulação no Editor e build bem-sucedido não substituem isso.

O APK de 17/10 exigia selecionar **Dois jogadores**. O candidato atual também
inclui **Contra IA**, com lado e dificuldade, para o teste offline no aparelho.
O modo local continua disponível.

## Gerar o APK no Mac

Instale **Android Build Support**, **Android SDK & NDK Tools** e **OpenJDK** no
mesmo Editor. No ambiente utilizado nesta frente, o CLI existente está em
`~/.unity/bin/unity`. O comando para obter os módulos compatíveis é:

```bash
unity install-modules --editor-version 6000.3.16f1 --architecture arm64 \
  --module android --childModules
```

Com o Editor fechado e o terminal na raiz do repositório:

```bash
python3 tools/setup_stockfish_android.py
bash tools/build_quest3.sh prepare
bash tools/build_quest3.sh validate
bash tools/build_quest3.sh build
# APK de demonstração com o perfil salvo:
bash tools/build_quest3.sh demo
```

`prepare` configura Android pelas APIs do Editor e valida o resultado;
`validate` confere o estado e as regras OpenXR; `build` valida e gera a build
de desenvolvimento com a configuração salva pelo primeiro comando. `demo`
gera `Builds/Quest3/XadrezCGI-Quest3-demo.apk` com `BuildOptions.None`, no mesmo
identificador de aplicativo, substituindo a instalação anterior quando instalado.
O wrapper inicia o Editor com `-buildTarget Android` antes de executar o método.
Os menus equivalentes ficam em **Chess CGI > Build > Quest 3**. Para usar o menu,
selecione antes Android em **File > Build Profiles**.

O resultado esperado é `Builds/Quest3/XadrezCGI-Quest3-dev.apk`, acompanhado da
fonte e licença do motor. Os logs de cada
execução ficam em `.local/quest3/`. O wrapper confere o resultado registrado
pelo Unity e calcula SHA-256 do APK. Não instala no headset e não publica em loja.
As configurações de Android ficam salvas no projeto; para voltar a executar no
desktop pelo Editor, selecione novamente a plataforma desktop em Build Profiles.

Configuração da primeira build: Android ARM64, IL2CPP, Vulkan, API mínima 29,
API alvo 35, renderização Single Pass Instanced, OpenXR automático e suporte
Meta Quest. Controles Touch/Touch Plus, rastreamento de mãos e Meta Hand
Tracking Aim reutilizam a entrada existente. O Mock Runtime não pertence ao APK.
O identificador do aplicativo é `com.mathwidu.chesscgi`.

O perfil de qualidade Mobile é o padrão Android. Na primeira sessão usava
escala de renderização 0,8 e MSAA desligado. O candidato de 21/10 usa escala
1,0 e inicialmente MSAA 2×, com melhora de nitidez confirmada pelo usuário.
O candidato de alta definição usa MSAA 4×, escala 1,25, HDR desligado e textos
do HUD com densidade 2×, preservando sala e personagens. O usuário aprovou
essa definição e confirmou a aparência das luminárias, cores e reflexos, mas
relatou atraso ao olhar para o tabuleiro. O perfil de demonstração aprovado
passa a ser também a base de trabalho: escala 1,0, MSAA 2×, HDR desligado,
preenchimento por vértice,
sombras em tempo real desligadas e foveation 0,5. Mantém modelos e texturas.
Em 22/10 o usuário confirmou qualidade visual suficiente e boa fluidez, além
de um lance manual e retorno ao menu no headset.

## Próximas entregas e critérios

| Entrega | Evidência necessária |
| --- | --- |
| Preparação Android | Módulos disponíveis, compilação e validação OpenXR sem erros bloqueantes |
| APK de diagnóstico | Build Android ARM64 concluída; manifesto e bibliotecas conferidos |
| Primeira sessão no Quest | Instalação autorizada, abertura em VR, cenário visível, tracking e áudio |
| Partida local | Agarrar/soltar, lance válido/inválido, captura, roque, promoção, fim e reinício |
| Mesa e HUD | Controles e mãos, botões de altura, escala com duas mãos, recentralização e menus |
| Adversário offline Android | Adaptador compatível, jogada legal, cancelamento, pausa e reinício no aparelho |
| Desempenho e conforto | Taxa real do runtime, CPU/GPU, memória e estabilidade durante uma partida |
| Realidade mista opcional | Escolha entre sala virtual e passthrough, posicionamento e escala do tabuleiro |

A integração da IA preserva `IMoveChooser`, `ComputerTurnCoordinator` e a
validação das jogadas por `ChessRulesAdapter`. O transporte Android e seu
empacotamento possuem verificações locais; funcionamento offline e desempenho
no Quest continuam pendentes. O sucesso do Stockfish desktop não valida o aparelho.

Na futura realidade mista, preservar o mesmo tabuleiro e regras e controlar
explicitamente a visibilidade/colisão da sala virtual. Ainda precisam ser
definidos posicionamento na mesa real, reposicionamento, persistência e retorno
à sala virtual. Não adicionar SDKs, permissões de cena ou implementação de
passthrough antes de definir essa etapa.

## Demonstração instalada em 22/10/2026

O usuário pediu uma versão instalada para mostrar na faculdade e passou a
priorizar fluidez, aceitando algum custo visual. Nas comparações anteriores,
olhar para os lados era fluido, enquanto o tabuleiro com os personagens
apresentava atraso. Após os ajustes abaixo, aprovou a qualidade visual e o
funcionamento e solicitou usar essa mesma versão como base de trabalho.

A versão de demonstração foi instalada com `Success`, no mesmo aplicativo
**Xadrez CGI** (`com.mathwidu.chesscgi`). Para abrir no headset, procurar na
**Biblioteca**, em **Fontes desconhecidas / Unknown Sources**. A execução é
standalone: o cabo e o Mac não são necessários. O aplicativo ocupa o
armazenamento interno e permanece instalado após desconectar ou reiniciar.

- APK: `Builds/Quest3/XadrezCGI-Quest3-demo.apk`, 265.377.481 bytes.
- SHA-256: `bc286bc0892d7717b6c9282cacdb590dab34af45c21f6594055f571b9362a737`.
  O hash do `base.apk` instalado foi conferido e corresponde ao arquivo local.
- Build `BuildOptions.None`, sem `DEBUGGABLE`; manifesto, assinatura,
  alinhamento, bibliotecas ARM64 e motor/fonte verificados.
- Abertura normal com categoria `LAUNCHER`, sem extras de diagnóstico,
  retornou `Status: ok`.
- Perfil salvo: escala 1,0, MSAA 2×, HDR desligado, distância de sombras zero,
  quatro luzes adicionais calculadas por vértice. Modelos e texturas originais
  preservados; sombras em tempo real desligadas neste perfil.
- `QuestRenderProfile` aplica foveation 0,5 no APK normal e tenta novamente
  após pausa. Aguarda pipeline, display e capacidade disponíveis. O diagnóstico
  com extras continua exclusivo do APK de desenvolvimento.
- Testes do helper: 12/12 EditMode em `TestResults/Quest3Render/DemoProfile.xml`.
  A revisão de release identificou a possibilidade de consultar a capacidade
  cedo demais; a guarda foi corrigida e recompilada antes de instalar.
- Log da build final: `.local/quest3/demo-20261022-164745-4515.log`.
  Evidências da instalação e execução em
  `.local/quest3-device/2026-10-22-render-comparison/demo-release/`.

O usuário confirmou que consegue pegar uma peça, fazer um lance e retornar ao
menu. Aprovou a qualidade visual e relatou que a versão está rodando bem;
essa confirmação define o checkpoint de trabalho e demonstração na branch
`codex/quest3-standalone-offline-ai`. Os valores salvos de Mobile e o padrão
de `QuestRenderProfile` são os mesmos usados no APK aprovado; abrir normalmente
não exige parâmetros do ADB.

A coleta numérica final não foi concluída e o Quest deixou de aparecer no USB
durante a leitura posterior. O aceite acima é do usuário no aparelho, sem
afirmação de 72 FPS sustentados. Partida prolongada, retomada e ciclo completo
da IA sem rede continuam com seus próprios critérios de validação.

## Comparação de nitidez em 22/10/2026

O usuário confirmou aparência boa das luminárias, cores e reflexos com HDR
desligado, mas a definição e o serrilhado ainda não atingiram o resultado
esperado. Com o Quest acordado, o log acumulou 131 amostras; nas dez últimas,
a mediana foi 28/72 FPS, `App=33,605 ms` e 45,5 quadros repetidos por segundo.
Esse é o ponto de partida para comparar resolução e custo de GPU.

`QuestRenderDiagnostics` pertence somente ao Editor e aos APKs Android de
desenvolvimento; sua inicialização automática ocorre somente nesse APK.
Ele lê extras do Intent de abertura e cria uma instância temporária do
pipeline. A escala e o MSAA dessa instância não alteram o asset salvo. Valores
inválidos são rejeitados. Nenhuma opção é persistida nas preferências ou nas
propriedades globais do Quest. O lançamento normal mantém os valores do projeto.

Exemplo de comparação pelo ADB, com o aparelho autorizado:

```bash
adb shell am force-stop com.mathwidu.chesscgi
adb shell am start -W -n com.mathwidu.chesscgi/com.unity3d.player.UnityPlayerGameActivity \
  --ef chesscgi.qa.renderScale 1.25 --ei chesscgi.qa.msaa 4 \
  --ef chesscgi.qa.foveation 0
```

Cada comparação exige encerrar e reabrir o processo. A escala aceita vai de
0,75 a 1,5, MSAA aceita 1/2/4 e o nível de foveation de 0 a 0,75. As sondas
de iluminação também aceitam `chesscgi.qa.shadowDistance` (float de 0 a 50,
padrão inicial: 50; perfil aprovado: 0) e `chesscgi.qa.additionalLights` (int de 0 a 4,
padrão do perfil: 4). Distância zero isola a rasterização das sombras;
limite zero de luzes adicionais limita sua contribuição por objeto, mantendo
parte do processamento global do URP.
Essas sondas por Intent são diagnósticas e não alteram o perfil salvo.
A sequência
planejada compara 1,0/4×/0 com 1,25/4×/0 antes de experimentar 1,25/4×/0,25.
Assim, primeiro muda a resolução e depois a redução de resolução periférica.
O segundo passo pode degradar as bordas da visão; ganho e aparência precisam
ser medidos. Não pressupor que compensa o déficit atual de GPU.

A preparação Android habilita a feature de foveation do OpenXR já instalado
e seleciona SRP Foveation tanto no OpenXR quanto em Meta Quest Support. A
feature fica disponível para o experimento. No início desta sessão o padrão
era zero; o candidato de demonstração posterior usa 0,5. Não
há SDK adicional nem uso de eye tracking. Logs `CHESS_CGI_QUEST_RENDER`
registram resolução efetiva do destino XR, escala/MSAA solicitados,
capacidade e nível relatado pelo display. O campo `Fov` do runtime deve
confirmar ativação; o getter isolado não comprova ganho nem qualidade.

Contadores de chamadas, mudanças de material, triângulos e vértices são
gravados a cada dois segundos quando disponíveis; ausência é registrada e
não é interpretada como zero. A amostra aguarda pipeline e display XR ativos,
com render pass válido. Seis testes EditMode aprovados em
`TestResults/Quest3Render/Diagnostics.xml` verificam isolamento do perfil e
rejeição de entradas inválidas. A revisão independente não identificou bloqueio
à instalação; Intent, retomada XR e foveation efetiva dependem da sessão física.

O APK diagnóstico foi gerado e instalado em 22/10 com `Success`; a abertura
da versão de referência retornou `Status: ok`. Pacote de 323.671.753 bytes,
SHA-256 `fcaca22556889941ef70f7853fa581799caa6634f6aab050899fcead49859440`.
Manifesto, assinatura, alinhamento, bibliotecas ARM64 e motor/fonte passaram
na verificação. Log `.local/quest3/build-20261022-161700-72878.log`.
Evidências da comparação em `.local/quest3-device/2026-10-22-render-comparison/`.
Os logs confirmam 1680 × 1760 por olho na referência A (escala 1,0) e
2100 × 2200 na B (escala 1,25), ambas com MSAA 4×, HDR desligado e foveation
zero. B registra cerca de 504 chamadas e 2,65 milhões de triângulos nos
contadores do frame; são contadores do render, não inventário de malhas únicas.

O usuário confirmou que **a nitidez de B chegou ao esperado**, mas relatou
travamentos e atraso. Nas dez últimas amostras curtas de B, a mediana foi
15/72 FPS e `App=57,70 ms`, com GPU perto de 98%. A, coletada com o aparelho
acordado após a confirmação de prontidão, teve mediana de 18/72 FPS e
`App=50,255 ms`. A pose variou: esses números diagnosticam o déficit atual e
não comprovam uma comparação controlada nem desempenho sustentado. As
capturas ADB ficaram pretas; a aparência no headset foi confirmada pelo usuário.
A qualidade de B serve de referência visual; o perfil salvo continua em escala
1,0 enquanto a otimização é testada. C manteve escala 1,25 e aplicou foveation
0,25: destino XR confirmado em 2100 × 2200, runtime `Fov=1`, mediana curta de
24/72 FPS. O usuário confirmou nitidez boa, mas ainda com engasgos e atraso.
Amostras de poses diferentes não comprovam ganho controlado nem desempenho
sustentado.

A inspeção das câmeras não encontrou renderização contínua redundante do
elenco no menu: a prévia do elenco é atualizada por mudanças/transições, a
câmera desktop é desativada no VR e a prévia de seleção não renderiza sem
peça selecionada. O próximo experimento mantém C e altera somente a distância
de sombras para zero no clone, restaurando 50 após medir. O perfil Mobile
mantém shadowmap principal 1024, uma cascata, sombras rígidas e até quatro
luzes adicionais por pixel sem sombras. A ampliação do diagnóstico passou
**12/12 EditMode** em `TestResults/Quest3Render/LightingDiagnostics.xml`,
incluindo isolamento dos ajustes de iluminação e rejeição de limites inválidos.
O APK com essas sondas também passou na verificação e foi instalado com
`Success`: 323.675.275 bytes, SHA-256
`4f9ccf30e0bf4ea740ea7bf938db38dc40dce799f74ed46dfb93e7627842f0be`.
Log `.local/quest3/build-20261022-162925-84971.log`. A revisão incremental
não identificou regressão concreta; a medição das sondas está em andamento.

## Primeira sessão física em 21/10/2026

- APK de 19/10 instalado com `adb install -r -t --user 0`; resultado `Success`.
  A abertura da `UnityPlayerGameActivity` retornou `Status: ok`; o OpenXR
  chegou ao estado `FOCUSED`, e o usuário confirmou que o jogo abriu.
- Captura do framebuffer mostra a sala Feevale, tabuleiro, HUD e mãos nos
  dois olhos. O usuário relatou dois pares de mãos. O modo desempenho estava
  exibindo peças clássicas; a aparência dos personagens ainda precisa ser
  conferida no aparelho com essa opção desmarcada.
- Identificado em `XRRig`: visuais rastreados ficavam fora dos grupos do
  `XRInputModalityManager`, e o componente era ativado antes de receber suas
  referências. A correção mantém os visuais sob o interactor correspondente
  e ativa o grupo após configurar as referências, preservando Camera Offset
  como espaço das poses das articulações.
- Três testes PlayMode reproduziram a falha antes da correção e passaram
  depois: controles já presentes na abertura, mãos já presentes na abertura
  e aquisição/perda/troca de entrada. O último confirma um contato armado
  antes da troca e seu desarme quando a mão é ocultada. Resultados em
  `TestResults/Quest3Hands/repro-rig.xml` e `fixed.xml`.
- Suíte completa com a correção: **136/136 PlayMode aprovados** em
  `TestResults/Quest3Hands/PlayMode-repeat.xml`. A primeira execução terminou
  135/136 com falha no teste de comparação de imagem
  `MenuStudioLightCannotRelightTheClassroom`, que passou isoladamente e na
  repetição completa, sem mudança desse teste ou da iluminação. A mesma
  instabilidade já havia ocorrido em 19/10; a causa permanece pendente.
- O motor `libstockfish.so` apareceu como filho do processo Unity no aparelho.
  Isso confirma sua inicialização pelo aplicativo; não comprova ainda uma
  partida sem rede nem as respostas/cancelamentos da IA.
- A telemetria do runtime mostrou trechos a 72/72 FPS e outros em torno de
  47–49/72 FPS. Desempenho sustentado permanece pendente; a correção das mãos
  não resolve por si só esse custo.
- O usuário também relatou imagem de baixa resolução. O log OpenXR confirmou
  swapchains de 1344 × 1408 por olho, diante de 1680 × 1760 recomendados pelo
  runtime; o perfil Mobile usava escala 0,8, sem MSAA. Resolução dinâmica e
  foveation estavam desligadas, e os overrides ADB de tamanho estavam vazios.
  O candidato passa a escala 1,0 e MSAA 2×. A mudança de escala aumenta o
  número de pixels em 56,25%, antes do custo adicional do MSAA; a comparação
  de nitidez e desempenho faz parte do reteste, sem aceite de desempenho ainda.
- Evidências locais em `.local/quest3-device/2026-10-21-first-session/`:
  instalação, abertura, logs, memória e `first-screen.png`. A pasta `baseline/`
  preserva o APK anterior e as configurações antes da correção.
- O candidato corrigido foi gerado com zero erros e quatro avisos, passou
  assinatura, manifesto, alinhamento, ABI e hashes do motor/fonte e foi
  instalado com `Success`; a reabertura retornou `Status: ok` e OpenXR
  `FOCUSED`. APK de 323.630.527 bytes, SHA-256
  `ff847684af61ffdc21afe22660d7340f56cd4c47d6d1942210093e35e15f55ce`.
  Log `.local/quest3/build-20261021-201638-67112.log`.
- No candidato, o log do aparelho confirmou swapchains de **1680 × 1760 por
  olho** e `SF=1.00`. A primeira captura e os logs não apresentaram erros
  fatais/gerenciados. Há amostras do menu perto de 72 FPS e outras abaixo do
  alvo; o enquadramento e estado do jogo variaram, portanto isso não constitui
  comparação controlada de desempenho com o APK anterior.
  Evidências em `hands-and-clarity-fix/` sob a pasta da sessão física.
- O usuário confirmou **um único par de mãos com controles e com mãos livres**
  e melhora de nitidez após essa instalação. Solicitou mais nitidez e menos
  serrilhado, e relatou falha no ponteiro do menu com mãos livres. Esses dois
  pontos seguem em investigação; essa confirmação não encerra os testes de
  interação ou desempenho da partida.
- No ponteiro das mãos, o usuário descreveu seleção controlável com a linha
  aparentemente saindo do centro da visão. O ponto de origem visual dos
  prefabs dependia de `PinchPointFollow`, sem referência aos eventos de mão;
  a seleção usava outra origem, o Aim Pose rastreado. Dois testes reproduziram
  o problema: início da linha a 1,211 m da origem do seletor e linha ainda
  visível após desativar a interação à distância. O rig agora remove o override
  de origem visual, e `UiOnlyCurveData` respeita a atividade do interactor.
  **5/5 testes específicos aprovados**, incluindo os três casos anteriores;
  XMLs `TestResults/Quest3Hands/ray-repro.xml` e `ray-fixed.xml`. As poses são
  injetadas na fixture. Após reinstalar, o usuário confirmou no Quest que a
  origem do ponteiro e a seleção dos botões com pinça funcionam.
- A suíte completa após a correção do ponteiro terminou **137/138** em
  `TestResults/Quest3Hands/PlayMode-ray-fixed.xml`, com os cinco testes
  específicos aprovados e a mesma falha de comparação de imagem
  `MenuStudioLightCannotRelightTheClassroom`. O resultado de 136/136 acima
  pertence ao candidato anterior.
- O candidato seguinte mantém escala 1,0 e passa MSAA de 2× para 4× para
  comparar serrilhado. A resolução do framebuffer não aumenta nesta etapa.
  Densidade dos glifos do HUD e minificação das texturas ainda podem limitar
  a nitidez; não foram alteradas junto desse experimento. Foveation permanece
  desligada; não foi acrescentado SDK nem houve troca de modelos.
- O APK com a correção do ponteiro e MSAA 4× foi instalado e aberto em VR,
  SHA-256 `ba7709d5c54b335e5cb16ff5459b1f4f2c1f0b9a5d04183b0b51417d6ca2c9c0`.
  O usuário confirmou ponteiro/pinça corretos e nova melhora de imagem,
  pedindo ainda mais definição. O trecho com menu, tabuleiro e 32 personagens
  visíveis registrou cerca de 26–27/72 FPS, `App` entre 29 e 41 ms e GPU 99%.
  A captura anterior de 2× não incluía o mesmo enquadramento do tabuleiro;
  os dados não isolam o custo do MSAA. Evidências em `hand-ray-msaa4/`.
- Para os textos, `CanvasScaler` em WorldSpace passa a usar
  `dynamicPixelsPerUnit=2`, com o painel lógico em 1920 × 1080. O teste novo
  reproduziu glifos com apenas 1 pixel por unidade e passou após o ajuste,
  verificando também ausência de corte das legendas. **28/28 testes de HUD,
  menu, prévia e modos de entrada aprovados** em
  `TestResults/Quest3Hands/text-fixed-ui.xml`; repro em `text-repro.xml`.
- O candidato seguinte desliga HDR no perfil Mobile, mantendo escala 1,0 e
  MSAA 4×. O código do URP inclui HDR entre as condições para textura
  intermediária e cópia final. A câmera ocular não habilita pós-processamento;
  Bloom, tonemapping e exposição do perfil também não estão ativos. A
  [Unity recomenda desligar HDR em XR móvel](https://docs.unity3d.com/6000.3/Documentation/Manual/xr-untethered-device-optimization.html#hdr).
  Ganho e aparência ainda precisam do reteste. As luminárias têm emissão
  acima de 1 no GLB e merecem comparação nas bordas claras após mudar o
  momento de limitação para a saída LDR.
- O candidato HDR desligado/texto 2× foi gerado, verificado, instalado e aberto
  com `Success`, `Status: ok` e OpenXR `FOCUSED`. APK de 323.635.302 bytes,
  SHA-256 `472fcfaeb1650680db228f2fec55fcb67e784fe4ddc0b9de6effd6a28ade86cf`;
  log `.local/quest3/build-20261021-203842-85879.log`. As primeiras 12 amostras
  ficaram em torno de 27–28/72 FPS. Depois o Quest foi observado em repouso
  (`mWakefulness=Asleep`), sem novas amostras nem framebuffer PNG disponível.
  Esse trecho inicial não fecha a comparação de HDR. O reteste de leitura,
  luminárias e desempenho segue aguardando o usuário. Evidências em
  `hdr-off-text2/` na pasta da sessão; os APKs anteriores foram preservados.
- Inventário estático para orientar o próximo profiling: as 32 peças com
  personagens somam **1.345.592 triângulos disponíveis**, 230 partes/renderers
  e 24 materiais compartilhados; 134 renderers lançam sombras. Os prefabs
  não têm LOD e os materiais URP estão com `Cull Off`. As texturas dos
  personagens já usam mipmaps, trilinear e anisotropia 8, com albedos/normais
  Android limitados a 2048/ASTC 6×6. Essas contagens descrevem os assets e
  instâncias iniciais: enquadramento, descarte, instancing e passes mudam os
  triângulos e chamadas efetivamente processados. Nenhuma redução de malha,
  alteração de culling ou de textura foi aplicada; falta medir seu custo real.

## Evidência inicial de 17/10/2026

O registro abaixo identifica o primeiro APK, anterior à IA Android. O candidato
mais recente e os resultados de 19/10 estão no [guia da IA offline](quest3-offline-ai.md).

- Android Build Support, SDK/NDK e OpenJDK instalados no Editor existente;
  verificação estrutural do CLI retornou `ok: true`. Logs em
  `.local/quest3/android-modules-install.log` e `android-modules-verify.json`.
- Compilação no Editor concluída. A primeira tentativa revelou uma referência
  indevida à assembly de teste do Mock Runtime; o utilitário passou a consultar
  esse recurso pelo identificador público, sem depender dessa assembly.
- **46/46 EditMode e 133/133 PlayMode aprovados**, sem ignorados, em
  `TestResults/Quest3/EditMode.xml` e `TestResults/Quest3/PlayMode.xml`.
  O Stockfish desktop foi preparado pelo script existente com SHA-256 conferido;
  esses testes não exercitam um motor Android.
- Duas execuções de `prepare` concluídas com zero erros bloqueantes. A segunda
  não mudou os 33 arquivos de configuração comparados
  (`.local/quest3/idempotence.json`). Blocos Standalone, pacotes, qualidade,
  cenas de build e os estudos de animação locais foram preservados.
- Dois avisos OpenXR: SSAO e prioridade da leitura de entrada. A regra de SSAO
  percorre todos os pipelines configurados; o recurso está no renderer PC e
  já está desativado (`m_Active: 0`), enquanto Mobile não possui renderer
  features. O aviso não comprova SSAO ativo no Quest. A recomendação opcional
  de latência permanece para a avaliação no aparelho.
- A primeira importação indicou Metal Toolchain ausente no Xcode do Mac.
  O componente oficial `27A266a` foi instalado: `xcrun metal --version`
  respondeu `32023.921`, e a compilação de um shader mínimo produziu
  `.local/quest3/metal-smoke.air`. Isso verifica o compilador, não a aparência
  do jogo. O problema não havia bloqueado `prepare` Android.
- **APK Android gerado com sucesso**, com zero erros no relatório de build:
  `Builds/Quest3/XadrezCGI-Quest3-dev.apk`, 222.621.577 bytes
  (222,6 MB; 212,3 MiB). Log em
  `.local/quest3/build-20261017-214036-10739.log`.
- Inspeção do APK final: apenas bibliotecas `arm64-v8a`, incluindo IL2CPP,
  Unity, OpenXR e XR Hands; pacote `com.mathwidu.chesscgi`, API mínima 29,
  alvo 35, categoria VR, suporte ao Quest 3 (`eureka`) e permissão de mãos.
  Assinatura v2, CRC do ZIP e alinhamento com `zipalign -c -P 16 4` aprovados.
  Evidência em `.local/quest3/apk-inspection.json` e nos arquivos
  `apk-manifest.txt`, `apk-badging.txt`, `apk-signature.txt` e
  `apk-alignment.txt` da mesma pasta. Esses checks verificam o pacote;
  não comprovam funcionamento no dispositivo.
- SHA-256 do APK:
  `f1ff5e641f94805daab5a348ab15d50a1ca348ddd49ec290eb41bf155fc1f3c2`.
- O build recalculou campos privados `m_Prefilter*` do pipeline Mobile.
  São dados de filtragem de shaders gerados pelo pré-processador URP;
  sua cópia pós-build foi guardada em `.local/quest3/`, e o asset foi
  restaurado ao conteúdo original. A qualidade visual configurada foi mantida.
  O cache nativo Android em `game/.utmp/` passou a ser ignorado pelo Git.
- O Unity também serializou os valores padrão de batching do Android
  (estático ligado, dinâmico desligado). Essa entrada foi mantida; os outros
  32 arquivos comparados após `prepare` não mudaram durante o build.
- Nenhuma instalação nem sessão no Quest foi realizada. A liberação do modo
  de desenvolvedor e a validação física continuam pendentes.

## Referências

- [Unity 6.3: ambiente Android](https://docs.unity3d.com/6000.3/Documentation/Manual/android-sdksetup.html).
- [Unity 6.3: plataforma Meta Quest](https://docs.unity.com/en-us/engine/6000.3/manual/xr/configuring-project-for/meta-quest-develop/build-profile).
- [OpenXR 1.16.1: Meta Quest Support](https://docs.unity3d.com/Packages/com.unity.xr.openxr@1.16/manual/features/metaquest.html).
- [OpenXR: validação do projeto](https://docs.unity3d.com/Packages/com.unity.xr.openxr@1.16/manual/project-validation.html).
- [Meta: preparação de software e dispositivo](https://developers.meta.com/vr/documentation/unity/unity-software-setup/).
