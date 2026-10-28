# Quest 3 standalone

## Estado dos fontes em 28/10/2026

A branch de trabalho é `codex/quest3-standalone-offline-ai`. Ela contém o
candidato **v0.1.4-ui.2**, com cenário a 65% e HUD independente; a aceitação
física desse candidato permanece pendente. A estável de exposição e o
perfil de 65% aprovado têm tags próprias. Consulte o
[registro de integração e recuperação](quest3-git-checkpoints-20261028.md)
para os commits, APKs e diferenças de aceite. As seções abaixo preservam o
histórico dos testes e das instalações.

## Perfil de trabalho aprovado — escala 0,65

Por solicitação do usuário, em 26/10/2026 às 19h46 foi instalada uma
comparação com escala **0,65**, mantendo MSAA **4×**, foveation 0,5 e o
mesmo código e geometria da versão estável. Rodapé:
**v0.1.3 (4) · 26/10/2026 19:44:54 Brasília (UTC-3)**. A hora identifica
esta compilação; a versão/código Android continuam iguais para permitir
restaurar o APK aprovado com `adb install -r`, sem limpar dados.

Hash do APK instalado conferido:
`4d96b00eef2223983f2ce470c5b3a93fa87321c29b926b22f06194559961ab95`.
São cerca de 34% menos pixels que em 0,80. Isso não comprova ganho
proporcional de FPS. O usuário confirmou **fluidez e imagem aceitável**;
0,65 permanece como perfil de trabalho. As duas tentativas de
medição foram interrompidas e não comprovam FPS sustentado. Cópia e recuperação em
`Builds/Quest3/comparison-20261026-scale065/`. A estável abaixo permanece
preservada, e `quest3-stable.json` continua apontando para o APK aceito.

## Versão estável para entrega — 0.1.3 (4)

O usuário aprovou esta compilação e a escolheu para a exposição em
26/10/2026. Ela foi instalada como **Xadrez CGI**, pacote
`com.mathwidu.chesscgi`, e está preservada para restauração. Rodapé:
**v0.1.3 (4) · 26/10/2026 17:39:37 Brasília (UTC-3)**. SHA-256 do APK aceito:
`541bf61c8f198f504778035a0d0c4b62be4690709c6030a387bace9aed08532e`.

Mantém escala 0,80, MSAA 4× e foveation 0,5. As peças personalizadas usam
menos geometria à distância e recuperam detalhe perto da câmera ou na
pega. O usuário aprovou aparência no tabuleiro e de perto, movimento e
jogabilidade. A medição de 30 s com os 32 personagens deu **65,5 FPS**
(63–68), abaixo da meta técnica de 72 Hz; esse limite permanece registrado.
O aceite do usuário não transforma essa medição em 72 FPS sustentados.

Passaram **278/278 PlayMode e 70/70 EditMode**, com os mesmos fontes C# do
APK. A instalação foi conferida novamente por versão, código e hash, sem
substituir ou reiniciar o binário aceito. APK e checkpoint dos fontes:
`Builds/Quest3/stable-20261026-v0.1.3/`. Identificação em
[`quest3-stable.json`](quest3-stable.json); roteiro de uso e recuperação em
[`quest3-entrega-20261026.md`](quest3-entrega-20261026.md).

## Comparação anterior — 0.1.2, escala 0,80

A comparação de resolução 0,80 foi instalada às 16h58. O rodapé era
**v0.1.2 (3) · 26/10/2026 16:57:16 Brasília (UTC-3)**; SHA-256
`18c8da7b4b0ef9ef0816f85717c60c86a50f0b387f637efbc2d946486ca033c5`.
Mantém MSAA 4×, foveation 0,5 e os personagens otimizados. A hora distingue
esta comparação do build 0,90 abaixo. A medição deu **56 FPS** de mediana,
ainda abaixo de 72 Hz. O usuário aprovou a definição e confirmou menu e
colocação de peão com ambas as mãos. Cópia em
`Builds/Quest3/comparison-20261026-scale080/`.

## Atualização 0.1.2 — custo dos personagens, escala 0,90

Instalada e iniciada no Quest em 26/10/2026 às 16h53 (São Paulo), no mesmo
pacote `com.mathwidu.chesscgi`. O arquivo instalado tem SHA-256
`c145a6650a1c77a363217aeee99725651ef62dd73acc2ad19f5b03554aff8c09`,
igual ao APK inspecionado. Manifesto: versão **0.1.2 / código 3**; uma
instalação do jogo. O rodapé esperado é
**v0.1.2 (3) · 26/10/2026 16:48:10 Brasília (UTC-3)**, conferido no recurso
empacotado. A captura de abertura não registrou erro gerenciado/fatal.

Os personagens Android usam 389.424 triângulos no conjunto inicial, 71,06%
a menos. Materiais, texturas dos dois times, bases, ajuste e colliders foram
preservados. O preview da peça reutiliza a imagem parada e seu clone fica
sem física/interação. MSAA 4×, escala 0,90 e foveation 0,5 permanecem iguais
para a comparação. Não houve nova mudança de regras ou gestos nesta rodada.

Passaram **261/261 PlayMode e 70/70 EditMode** com os mesmos fontes C# do
APK, além de **19/19** testes de integração após o saneamento final das
tangentes. Os seis assets importados também estão empacotados no APK.
Detalhes, medições anteriores e limites em
[otimização de desempenho](quest3-performance-20261026.md).

Esta compilação mediu 53,5 FPS de mediana, abaixo da meta de 72.
A comparação de resolução menor acima mediu 56 FPS.
A versão 0.1.1 permanece preservada em `Builds/Quest3/delivery-20261026-v0.1.1/`.

## Atualização 0.1.1 — identificação e colocação

Instalada e iniciada no Quest em 26/10/2026 às 16h17 (São Paulo), substituindo
o mesmo pacote `com.mathwidu.chesscgi`. O manifesto do aparelho confirmou
versão 0.1.1 / código Android 2; o SHA-256 remoto é igual ao APK verificado:
`9146a734f8f0090ddd3b48356e8f1d3ff2842b47593410e78a0396016c4a127f`.
A captura de inicialização não registrou erro gerenciado/fatal. Há uma
instalação do jogo; `com.matheus.quest.devcheck` é o diagnóstico antigo.

O rodapé do menu mostra **v0.1.1 (2) · 26/10/2026 16:11:30 Brasília (UTC-3)**.
O recurso com esses dados foi localizado e conferido dentro do APK. O callback
`ChessCgiBuildIdentityBuild` grava um recurso JSON novo em cada build e
confere sua importação; o menu não usa o relógio do dia em que o jogador abre
o app. A versão exibida precisa concordar com `Application.version`.

Solturas lentas com mãos não são recusadas apenas por estarem acima dos
antigos 10 cm. A espera inclui 600 ms mais o tempo estimado de queda, no
máximo 2 s. Confirmação exige pouso na mesma casa legal, base próxima ao
plano e ausência de apoio da mão. A proteção entre peças acompanha a
observação para uma captura alta não empilhar sobre a adversária. A
velocidade lateral e o perfil visual permanecem como na versão anterior.

Reprodução: solturas a 20/50/120 cm e captura a 30 cm falharam antes das
correções. A suíte final passou **236/236 PlayMode e 70/70 EditMode**, com
zero skips. Os replays de abertura com movimento lateral passaram sem
exigir alteração de velocidade. A revisão independente terminou sem
achados pendentes. A sensação no headset continua sendo uma conferência
distinta dos testes, do build e da instalação.

O perfil continua em MSAA 4× e escala 0,90. Na rodada posterior, este APK
mediu 34 FPS de mediana com personagens e 72,5 com peças clássicas; ver
[condições da comparação](quest3-performance-20261026.md). Cópia recuperável, licença/fonte do Stockfish e identificação
dos fontes testados: `Builds/Quest3/delivery-20261026-v0.1.1/`. O candidato
anterior permanece em `Builds/Quest3/delivery-20261026/`. A atualização usa
código Android maior; seguir o [roteiro de recuperação](quest3-entrega-20261026.md)
para preservar os dados se for necessário voltar à lógica anterior.

O ensaio exploratório a 2 m acima do tabuleiro continuou quicando até o
prazo de observação e depois saiu da casa. Sua asserção provisória de
colocação obrigatória foi retirada: essa não é uma promessa de aceitar
quedas arbitrárias. Os relatórios com falha e a trajetória foram preservados
em `.local/quest3/menu-version-20261026/`; as guardas de pouso e cancelamento
foram mantidas.

## Candidato anterior para a entrega de 26/10/2026

APK gerado, inspecionado, instalado e iniciado no Quest às 14h07 (horário de
São Paulo), com hash remoto conferido:
`7d6f644021f577a70d7912b154c5720ff584d3fe1b1237db3964d0e9a964cd7e`.
230/230 PlayMode e 64/64 EditMode passaram com os mesmos fontes do build.
A primeira rodada EditMode falhou uma vez no arquivo PID do motor simulado
silencioso do Mac; a reexecução completa passou sem alterar os fontes.
Nenhum erro gerenciado/fatal apareceu na captura de inicialização. O headset
não estava em uso nessa coleta: ela não produziu amostra do display XR nem
comprova samples efetivos, conforto ou FPS. Veja o
[roteiro de conferência da entrega](quest3-entrega-20261026.md).

O candidato mantém a sala e os personagens, com MSAA 4× e escala de renderização
0,90 (19% menos pixels que 1,00). A consulta `cmd gpu vkjson` no Quest conectado
informou `framebufferColorSampleCounts`, `framebufferDepthSampleCounts` e
`framebufferStencilSampleCounts` iguais a 7, ou 1/2/4×: 8× não está disponível
nesse framebuffer. HDR desligado, sombras a distância zero e foveation 0,5
continuam. `QuestRenderProfile` registra perfil, capacidade e swapchain sem
confundir o valor configurado com samples realmente relatados pelo runtime.
Esse ajuste não comprova sozinho fluidez nem elimina todo tipo de serrilhado.

Abrir a pinça devagar até 10 cm acima da casa verde agora permite observar
o pouso por até 600 ms. O lance continua exigindo contato real com o tabuleiro,
mesma casa legal e ausência de apoio na mão. A reprodução de 5/9 cm e do dedo
que demora 320 ms para sair falhava com a margem anterior de 4 cm/250 ms.
Arremessos, apoio persistente na palma e interrupções não viram jogadas.

As hastes laterais também aceitam fechar os quatro dedos da mão sobre cada
uma delas. Uma mão só segura; duas mãos afastando/aproximando ajustam o
tamanho. Abrir solta, e interrupções exigem abrir antes de agarrar de novo.
O menu e as peças continuam usando a pinça. O replay de troca de modalidade
reproduziu uma soltura duplicada na implementação inicial; o candidato usa
o ciclo normal de seleção do XRI, com saída apenas do par ainda selecionado.

O comando `bash tools/build_quest3.sh delivery` gera um APK normal, sem modo
de desenvolvimento, em `Builds/Quest3/XadrezCGI-Quest3-delivery-candidate.apk`.
Esse caminho preserva `XadrezCGI-Quest3-work.apk` e a cópia de 23/10 em
`Builds/Quest3/working-20261023/` para recuperação. Os dados desta rodada ficam
em `.local/quest3/delivery-20261026/`. O candidato não é um aceite físico:
o usuário fará a conferência no Quest à tarde, antes de levar à faculdade.

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
# APK normal de trabalho, em arquivo separado:
bash tools/build_quest3.sh work
```

`prepare` configura Android pelas APIs do Editor e valida o resultado;
`validate` confere o estado e as regras OpenXR; `build` valida e gera a build
de desenvolvimento com a configuração salva pelo primeiro comando. `demo`
gera `Builds/Quest3/XadrezCGI-Quest3-demo.apk` com `BuildOptions.None`, no mesmo
identificador de aplicativo, substituindo a instalação anterior quando instalado.
O wrapper inicia o Editor com `-buildTarget Android` antes de executar o método.
`work` usa o mesmo perfil normal e grava `Builds/Quest3/XadrezCGI-Quest3-work.apk`,
preservando o arquivo de demonstração. O menu correspondente é **Build Working APK**.
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

## Interação das mãos preparada em 22/10/2026

O usuário relatou dificuldade em agarrar de perto com a pinça e pediu contato
físico, incluindo apoiar uma peça na palma aberta. A implementação está em
[Agarrar e apoiar peças com as mãos](../domainbook/domains/interaction/features/agarrar-e-apoiar-pecas-com-as-maos.md).
O alcance continua em 6 cm; a pinça iniciada antes da aproximação pode adquirir
a peça ao chegar. Mão aberta não seleciona e duas pinças não disputam a posse.
O alvo mais próximo é preservado para não atrair uma vizinha ao tentar pegar
uma peça proibida pela vez.

Palma e dedos sustentam peças soltas por gravidade e atrito. O retorno à
origem aguarda três segundos sem apoio da mão. Contatos compostos, perda e
retorno de rastreamento, inclusive de um único joint, têm proteção contra
apoio obsoleto e reativação dentro da peça. As peças ancoradas mantêm o
comportamento anterior até serem agarradas.

- Regressão PlayMode: **62/62** aprovados em
  `TestResults/Quest3Hands/hand-interaction-final.xml`, incluindo pinça,
  controles, transferência, palma, dedos, botões, modalidades, HUD e escala.
- Importação do novo teste de alvo: **1/1** em
  `TestResults/Quest3Hands/hand-target-import.xml`; GUIDs dos novos assets
  verificados como válidos e únicos. `domainbook validate` aprovado.
- Candidato: `Builds/Quest3/XadrezCGI-Quest3-hands-candidate.apk`,
  **265.405.745 bytes**, SHA-256
  `c7971e24c2b8edad4d9f2cb71fc2f786bd746d85b861ff2297b3ed9598a05e77`.
- Build de release com `BuildOptions.None`, resultado `Succeeded`, zero erros;
  manifesto, assinatura, alinhamento, bibliotecas e motor/fonte verificados.
  Log: `.local/quest3/demo-20261022-204948-71382.log`.
- Mesmo perfil gráfico aprovado: escala 1,0, MSAA 2×, HDR desligado, luzes
  adicionais por vértice, sombras em tempo real desligadas e foveation 0,5.
- A demonstração aprovada continua em `Builds/Quest3/XadrezCGI-Quest3-demo.apk`
  e tem cópia em `Builds/Quest3/stable-20261022/`, com o hash original acima.

Na conferência de 22/10, `adb devices -l` não mostrou o Quest e o candidato
não pôde ser instalado naquela rodada. A instalação ocorreu em 23/10,
conforme a sessão abaixo. Os testes do Editor não comprovam sensação dos
gestos ou desempenho no aparelho.

Após conectar e autorizar o Quest, instalar o candidato com `adb install -r`
e abrir **Xadrez CGI** em **Biblioteca > Fontes desconhecidas**. O pacote é o
mesmo, portanto a instalação substitui a versão anterior no headset. Para
restaurar a demonstração aprovada, usar `adb install -r` no APK da pasta
`stable-20261022`, sem desinstalar ou apagar dados.

Na validação física, iniciar Dois jogadores e conferir: pinça antes e depois
de aproximar de uma peça; levantar uma peça e soltá-la sobre a outra palma
aberta ao lado do tabuleiro; mantê-la por mais de cinco segundos; transportar,
inclinar e agarrar novamente; fazer um lance e usar o menu. Conferir ambas as
mãos, troca para controles e fluidez olhando os 32 personagens. A sensação
de contato e a folga dos colliders poderão exigir calibração no aparelho.

## Sessão das mãos no aparelho em 23/10/2026

O Quest voltou a aparecer autorizado no ADB. O candidato de mãos foi
instalado com `Success`; o hash do `base.apk` instalado foi conferido e
corresponde a `c7971e24c2b8edad4d9f2cb71fc2f786bd746d85b861ff2297b3ed9598a05e77`.
Antes da atualização, o APK instalado correspondia à demonstração aprovada
(`bc286bc…`), que continua preservada para restauração.

- Abertura normal pelo launcher retornou `Status: ok`, com início `COLD`.
- Captura do aparelho mostra sala, tabuleiro e modelos de mão nos dois olhos.
- O runtime registrou `foveationRequested=0.50 foveationReported=0.50`.
- Não foram encontradas exceções gerenciadas ou falhas fatais nas leituras
  inicial e de interação do processo. Isso não substitui a validação física.
- Evidências locais: `.local/quest3-device/2026-10-23-hands/`, incluindo
  `session.json`, instalação, hashes, abertura, logs e `startup.png`.

O usuário confirmou melhora da pinça, mas ainda considerou pegar e largar
difíceis. A peça permaneceu na palma, porém penetrou parcialmente na mão
visível. Esta primeira interação ainda não foi aceita. O comportamento
escolhido para a próxima revisão é permitir empurrar e derrubar peças pelo
toque, sem registrar lance; arremessar e pegar novamente deve ser fácil.
Rastreamento, botões, escala, menus, IA offline, fluidez e retomada continuam
com critérios de aceite próprios.

## Revisão do contato físico em 23/10/2026

O retorno no aparelho orientou uma segunda revisão de mãos. O perfil de
renderização da demonstração permanece igual. A interação passa a ter:

- Pinça próxima analógica: inicia em 0,75 e só larga ao abrir até 0,55;
  o leitor de clique do HUD permanece separado. Foco perdido, pausa e
  rastreamento inválido cancelam e exigem abrir a mão antes de nova pega.
- Peças dinâmicas adormecidas em repouso, com massa de 50 g e centro de massa
  baixo desde a criação. Mãos e outras peças podem empurrar e derrubar sem
  alterar casa lógica, turno ou histórico.
- Palma de 28 mm e dedos compostos, preservando joints crus e o rearme com
  espaço livre. A espessura anterior de 12 mm deixava a base dentro da skin.
- Recuperação após três segundos sem contato válido da mão, inclusive
  lateral; apoio na palma e novo agarrar adiam a recuperação.
- Soltura separada de colocação: captura da posição original e da velocidade
  XRI antes de gravidade/colisões. No ar, em arremesso rápido ou ao perder
  tracking/foco/pausa, a seleção termina sem lance. Colocar devagar junto
  da superfície ainda passa pelas mesmas regras de xadrez.
- Sincronização após lance reaproveita peças não afetadas, preservando uma
  peça solta durante outro lance. Reinício e mudança de modelos refazem o
  conjunto; ajuste de altura/tamanho mantém o repouso das demais peças.

Os casos de pinça/tracking, foco/pausa, arremesso lateral, soltura no ar,
continuidade e repouso após escala foram reproduzidos antes de suas correções.
A regressão geométrica usa provider Left/Right, SkeletonDriver ativo,
`ConfigureTrackedHand`, skin renderizada e peça real da fábrica. Confere
joints desenhados contra joints crus e mede o apoio nas duas mãos; imagens
locais ficam em `TestResults/Quest3Hands/palm-surface-*.png`. Não há skinning
na CPU para calibrar a mão em runtime. Essa pose controlada não substitui
rastreamento real, sensação de pega, lançamento, conforto ou custo no Quest.

Verificação desta revisão: suíte completa PlayMode **165/165**, incluindo
IA, captura, roque, HUD, botões, controles, ajuste do tabuleiro e mãos. Após
identificar que XRI desparenta objetos agarrados, a reconstrução passou a
remover também essas identidades; a regressão afetada passou **34/34**,
incluindo reinício segurando peça e continuidade durante troca de turno.
A suíte EditMode também passou **64/64** após a última alteração de código.
A primeira execução ampla teve duas regressões de tempo da seleção,
corrigidas mantendo limpeza imediata de solturas que não podem gerar lance,
e a intermitência visual do teste de luz do estúdio já registrada em
21/10. O teste visual passou isolado e na repetição completa. Relatórios
locais em `TestResults/Quest3Hands/`.

### Segundo candidato físico, testado no aparelho

- `Builds/Quest3/XadrezCGI-Quest3-hands-physics-candidate.apk`, 265.415.709 bytes.
- SHA-256 `ff3b2da18001439b1d94664114488caac8ed337ff3b28c0cd80cb9d56204bbd5`.
- Build normal concluído com zero erros e quatro avisos; assinatura,
  alinhamento, ARM64, payload offline e fonte correspondente verificados.
  Leitor novo e corpo de contato constam no metadata; diagnósticos e testes
  estão ausentes do pacote. Os fontes permaneceram iguais durante o build.
- Instalado com `Success`; hash do APK no aparelho igual ao candidato.
  Abertura normal `LAUNCHER` retornou `Status: ok`, `COLD`.
- Runtime confirmou `foveationRequested=0.50 foveationReported=0.50`.
  A leitura inicial do processo não mostrou erros fatais ou exceções
  gerenciadas. A captura mostra sala, tabuleiro e um par de mãos por olho.
- Amostras iniciais, com enquadramento variando durante interação, ficaram
  perto de 45–48/72 FPS, GPU 98–99%. Não são comparação controlada com a
  demonstração aprovada nem comprovam fluidez sustentada. Não se alterou o
  perfil gráfico para esta revisão de mãos.
- A demonstração aprovada `bc286bc…` permanece no APK padrão e em
  `Builds/Quest3/stable-20261022/`; esse candidato substituiu a instalação anterior.
- Evidências locais: `.local/quest3-device/2026-10-23-hands-physics/` e
  `.local/quest3/hands-physics-build.json`. O usuário confirmou que o
  empurrão sem lance funciona. Relatou ainda dificuldade de pegar/largar,
  pequeno afundamento na palma e peça presa mesmo abrindo a pinça; pediu
  proteção das vizinhas enquanto outra peça está selecionada.

## Revisão da soltura e proteção das vizinhas em 23/10/2026

A pinça passa a ler exclusivamente `MetaAimHand.pinchStrengthIndex`. Os dois
providers nativos no replay confirmaram que `Select Value` agregava também
`HandInteraction.graspValue`: com indicador e polegar abertos a 0,10, o grasp
em 0,95 mantinha a pega e podia adquirir uma peça. A mudança preserva limites
0,75/0,55, foco, pausa, tracking, rearme e entrada separada do HUD.

A mão ocupada e sua peça suspendem colisões com as vizinhas. A mão livre
mantém o toque e o apoio. Na abertura, cada par ainda sobreposto aguarda
separação; a palma livre volta sem exigir que todos os dedos saiam. A peça
recupera suas colisões quando todo seu volume estiver livre, inclusive no
topo de modelos altos e ampliados. Sobreposição na soltura não deve causar
impulso de reativação.

A colocação com mãos usa o anel inferior do collider e a velocidade recente
da entrada (cerca de 30 ms), com limites mantidos de 2,5 cm e 0,35 m/s.
Isso aceita inclinação com base perto do tabuleiro, descida suave e parada
breve, sem exigir limpar os 0,25 s do histórico amplificado de arremesso.
Legalidade, casa X/Z, veto de apoio na mão e velocidade física lançada seguem
separados. Os controles conservam sua política anterior.

O replay temporal mediu cerca de 5,25 mm de penetração visual após avançar
apenas o `BeforeRender` em 5 mm, sem movimento ou penetração física. Durante
apoio válido, uma compensação visual de translação de até 2 cm acompanha o
corpo interpolado. As poses cruas e físicas permanecem iguais. A face dos
botões é resolvida depois dessa candidata: um RED adicional reproduziu
3,7 mm de passagem pelo botão com a composição anterior. A palma mantém
28 mm, sem mudar frequência física ou qualidade gráfica. A compensação atual
não resolve a diferença de rotação entre as fases; esse gesto exige QA próprio.

Relatórios em `TestResults/Quest3Hands/`:

- `placement-protection-red.xml`: 6 falhas reproduzidas em 8 casos; depois
  a fixture de movimento foi temporizada a 72 Hz para eliminar velocidades
  artificiais de frames variáveis.
- `protection-temporal-red.xml`: confirmou recusa de colocação natural e
  penetração temporal nas duas mãos.
- `pinch-mixed-red.xml`: reproduziu as duas falhas de pinça com providers
  MetaAim e HandInteraction simultâneos.
- `pinch-green-button-red.xml`: pinça e colocação verdes, composição do
  botão ainda vermelha antes da correção.
- `hands-placement-full-EditMode.xml` e `hands-placement-full-PlayMode.xml`:
  **64/64 EditMode e 184/184 PlayMode** na suíte completa após as correções.
- `hands-placement-green.xml`: **53/53** regressões afetadas aprovadas,
  incluindo mão livre com a outra ocupada, soltura na própria palma com dedo
  sobreposto, peça alta, botões e aquisição nativa.

Os replays não comprovam sensação ou desempenho no Quest. O aceite físico
final deve conferir soltar sem desmanchar todos os dedos, não derrubar
vizinhas durante a pega, apoio visível, colocação e fluidez.

### Novo candidato instalado para reteste

- APK `Builds/Quest3/XadrezCGI-Quest3-hands-placement-candidate.apk`,
  **265.437.601 bytes**, SHA-256
  `7765154be25f6f805da8f83a32a6d4006737f265b0778fb98bed01860b74c138`.
- Build de release normal, `Succeeded`, zero erros e quatro avisos.
  Assinatura, alinhamento de 16 KiB, manifesto, bibliotecas ARM64,
  motor offline e fonte correspondente verificados. Metadata contém os
  componentes das mãos e perfil; testes e diagnósticos ficaram fora do APK.
- Os fontes de produção permaneceram iguais durante a compilação.
  Log `.local/quest3/demo-20261023-134655-89600.log`; registro do wrapper
  `.local/quest3/hands-placement-build.json`.
- Instalado com `Success`; o hash do `base.apk` no Quest corresponde ao
  candidato. Início normal `LAUNCHER` retornou `Status: ok`.
- Runtime confirmou `foveationRequested=0.50 foveationReported=0.50`.
  Leitura inicial do processo não encontrou exceções gerenciadas nem erro
  fatal. Isso não prova facilidade dos gestos ou desempenho sustentado.
- Evidências locais em `.local/quest3-device/2026-10-23-hands-placement/`.
  No reteste, o usuário confirmou soltura pela pinça e proteção da mão
  ocupada. Relatou recusa sobre uma casa legal e pequeno afundamento na
  palma; pediu que a mão livre também não mova vizinhas durante uma pega.
  Aceite desta interação é parcial e requer nova revisão.
- A demonstração aprovada continua no arquivo padrão e na pasta
  `Builds/Quest3/stable-20261022/`, com SHA original `bc286bc…`.
  Esta instalação substitui o candidato de mãos anterior no aparelho.
  No Quest, abrir **Biblioteca > Fontes desconhecidas > Xadrez CGI**;
  funciona sem cabo depois de instalado.

## Revisão das duas mãos e do pouso em 23/10/2026

O relato do candidato `7765154…` foi reproduzido por replays separados:
soltar 3 cm acima de uma casa legal era recusado antes do pouso; um dedo
que ainda apoiava brevemente a peça também encerrava a tentativa cedo.
A mão livre deslocava uma vizinha cerca de 6,2 cm enquanto a outra segurava
uma peça. Girar somente a fase `BeforeRender` em 10° piorava o apoio visual
em cerca de 2 mm, apesar das poses físicas permanecerem imóveis. Havia
ainda penetração estática próxima de 0,2 mm na base visual real do peão.

- `ThrowablePiece` mantém o conjunto de pegas físicas. Enquanto qualquer
  uma existir, os contatos das duas mãos com peças ficam suspensos. Soltar
  uma de duas peças não reativa a física; a última soltura ou remoção restaura
  pares livres, mantendo a guarda contra sobreposição.
- `VrSelectionBridge` solta imediatamente e observa por até 250 ms uma
  colocação lenta iniciada com a base a até 4 cm do tabuleiro. Só confirma
  com contato real, base física a até 5 mm do plano, sem apoio de mão e na
  mesma casa legal da saída. Um dedo pode sair durante a espera; continuar
  carregando a peça na palma não confirma lance.
- Só nessa colocação próxima e válida, a velocidade recente de até 0,35 m/s
  é reaplicada após o detach. O histórico de arremesso amplificado do XRI
  permanece registrado, mas não arrasta uma peça cuja mão já parou.
  Soltura alta, arremesso rápido, cancelamento, apoio e nova pega não
  recebem essa assistência. Os controles preservam a regra anterior.
- O desenho da mão usa a transformação rígida entre a palma crua e o corpo
  interpolado, limitada a 2 cm no centro e 10°. Os botões avaliam cada ponto
  já transformado. Poses cruas, corpos físicos, escala e frequência física
  permanecem iguais. A palma passa a 29 mm, acrescentando 0,5 mm por face.
- Pausa e perda de foco cancelam a observação imediatamente. A limpeza da
  seleção pode ocorrer com a partida suspensa; iniciar lances continua
  bloqueado. Desativar a ponte ou não obter o cache de detach também encerra
  a tentativa, sem deixar destaques pendentes.

`shared-protection-landing-rotation-red.xml` e `landing-static-skin-red.xml`
registram as falhas anteriores. `shared-landing-skin-green.xml` passou
**28/28**, incluindo as duas mãos, duas pegas, remoção e reativação da última
peça, pouso real, apoio persistente, arremesso lateral, rotação nas duas
palmas e composição com botão. O RED de lifecycle confirmou três seleções
pendentes entre 13 casos; a pausa foi reforçada para exigir cancelamento
síncrono. A suíte final passou **199/199 PlayMode e 64/64 EditMode** em
`shared-landing-full-PlayMode.xml` e `shared-landing-full-EditMode.xml`, com
os fontes e testes inalterados durante as duas execuções. A revisão
independente dos fontes não encontrou outros bloqueadores. O aceite físico
do novo candidato permanece pendente. O perfil gráfico
aprovado e o APK de demonstração `bc286bc…` continuam preservados.

### Candidato de pouso e proteção das duas mãos instalado

- APK `Builds/Quest3/XadrezCGI-Quest3-hands-shared-landing-candidate.apk`,
  **265.438.053 bytes**, SHA-256
  `7ff45c028f81741691dd4c344b9491d5287774c125fa75c0ca622029970b5aea`.
- Build normal de release, `Succeeded`, zero erros e quatro avisos.
  Assinatura, alinhamento de 16 KiB, manifesto, bibliotecas ARM64, motor
  offline e fonte correspondente conferidos no próprio candidato.
  Metadata contém os componentes de interação e exclui testes e diagnóstico.
- Os fontes de produção permaneceram iguais durante o build. O perfil
  Mobile foi restaurado byte a byte após somente a geração de prefilters;
  os dois APKs da demonstração aprovada mantêm SHA `bc286bc…`.
  Log `.local/quest3/demo-20261023-164746-67726.log`; wrapper e testes em
  `.local/quest3/hands-shared-landing-{build,tests}.json`.
- Instalado com `Success`, substituindo o candidato `7765154…`. O hash do
  `base.apk` instalado corresponde ao novo candidato. Início normal
  `LAUNCHER` retornou `Status: ok`, `LaunchState: COLD`.
- Leitura inicial do processo não encontrou exceções gerenciadas nem erro
  fatal. O aplicativo estava pausado e sem foco nessa captura; confirmação
  do perfil em execução ativa e aceite dos gestos dependem do headset em uso.
  Essa leitura não mede fluidez sustentada.
- Evidências em `.local/quest3-device/2026-10-23-hands-shared-landing/`.
  Roteiro físico enviado para colocação sobre casa destacada, proteção das
  duas mãos durante a pega e apoio visível na palma. O usuário confirmou
  a proteção das duas mãos com retorno do toque após soltar, além de apoio
  bom sem afundamento visível. A colocação ainda solta mas recusa o lance.
  Também relatou mãos muito trêmulas, inclusive no ar longe das peças.
  Aceite desta revisão é parcial e exige investigação dessas duas falhas.
  Abrir no Quest em **Biblioteca > Fontes desconhecidas > Xadrez CGI**;
  o cabo não é necessário para jogar depois de instalado.

O log do mesmo processo registra trechos de 44–45/72 FPS, GPU próxima de
98–99% e quadros repetidos. O enquadramento não foi controlado nessa leitura;
não é uma comparação de performance com a demonstração aprovada. A fonte
nativa reaplica a mesma rotação no caminho completo sem contato, e não foi
encontrado um estabilizador do visual que estivesse sendo ignorado. Isso
mantém tracking, apresentação irregular e correção visual como hipóteses
separadas. Um APK otimizado de diagnóstico foi preparado com probes
temporárias por soltura e comparação por abertura; os limites de colocação
permanecem iguais até obter o motivo real no aparelho.

### Diagnóstico dirigido instalado após o reteste

- APK usado na captura `Builds/Quest3/XadrezCGI-Quest3-hands-diagnostic-visual.apk`,
  **265.478.149 bytes**, SHA-256
  `1779fafcd37e29621c6e13edab65ab3569e12ababa1582f63695d1162579a1a3`.
  Build otimizado com `BuildOptions.None`, zero erros e quatro avisos.
  O símbolo temporário `QUEST_HAND_DIAGNOSTICS` é passado apenas ao build
  via [`extraScriptingDefines`](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/BuildPlayerOptions-extraScriptingDefines.html),
  sem alterar os símbolos salvos do projeto.
- Assinatura, manifesto, alinhamento de 16 KiB, payload ARM64, motor offline
  e fonte correspondente verificados. Instalação retornou `Success`; o hash
  do `base.apk` coincide. Abertura `LAUNCHER` retornou `Status: ok`.
  Os APKs anteriores e o perfil Mobile salvo continuam preservados.
- Cada soltura emite `begin/outcome` com motivo da recusa e medidas de casa,
  base, velocidade do anchor e da mão, tracking, apoio e contato real.
  A observação visual agrega poses recebidas versus desenhadas a cada dois
  segundos. Não há log por callback nem novos limites de colocação.
- Comparações preparadas por abertura: A = visual atual/escala 1,0;
  B = visual nativo/escala 1,0; C = visual atual/escala 0,8.
  MSAA 2× e foveation 0,5 permanecem comuns. B e C ainda não foram executados.
  A primeira geração `1d83d91…` só tinha o registro de soltura ativo; o
  bootstrap do observador visual foi corrigido na geração atual.
- `hand-diagnostic-probes.xml` passou **40/40 PlayMode**. Isso confirma que
  as probes não mudaram os comportamentos exercitados no Editor; não prova
  a causa no Quest. Após o teste do usuário, o caso A reteve 17 solturas:
  12 na casa de origem, duas fora do tabuleiro, uma em destino ilegal e
  dois lances iniciados. As tentativas 1–10 já haviam saído do buffer do
  logcat. Não houve recusa por velocidade ou prazo nas tentativas retidas.
  Os dois destinos legais observados chegaram ao contato e foram aceitos.
- Nas solturas registradas como casa de origem, o ponto cru da pinça e o
  ponto seguido pelo seletor por vezes estavam bem afastados da raiz da
  peça. Isso exige testar o seguimento e a referência da base antes de
  ampliar a tolerância. Os registros não provam sozinhos a causa do desvio.
  O parser local foi corrigido para ler `outcome=motivo`.
- A captura contém 81 relatórios visuais com rastreamento; 67 mantiveram a
  raiz desenhada igual à pose nativa. Na sessão, a mediana de renderização
  foi 37/72 FPS, com GPU em torno de 99%; no enquadramento do jogo houve
  cerca de 1,6 milhão de triângulos e 365 chamadas. Os contadores menores
  do fim da captura correspondem a outro enquadramento. Essa leitura não
  isola a causa do tremor e não mede uma regressão contra a demonstração.
  Colocação e tremor continuam sem aceite.
- Evidências e casos em `.local/quest3-device/2026-10-23-hands-diagnostic/`;
  builds em `.local/quest3/hands-diagnostic{-visual}-build.{json,log}`.
  `diagnosis-plan.json` registra o código temporário que deve ser removido
  depois da captura e antes de concluir o próximo candidato normal.

### Refinamento da colocação manual após os registros

Os registros não sustentaram aumentar o prazo de pouso: as 12 solturas na
origem nem chegaram a essa espera. Um novo replay move a rainha por um
percurso fixo da mão, sem recalcular a entrada a partir da peça, e verifica
o ponto de pega, inclinação e transferência de d8 para d7. Esse transporte
passou antes da correção e não reproduziu o desvio persistente do aparelho.

Foram reproduzidos e corrigidos outros problemas concretos:

- `native-queen-grip-red.xml` mostrou escala física **23,78× maior** dentro
  de `selectEntered`: o XRI já tinha retirado a peça do tabuleiro quando
  `SetSelected` aplicava uma escala em unidades locais antigas. O primeiro
  frame corrigia o tamanho, mas o callback recebia um collider deformado.
  A seleção deixa de redimensionar peças físicas; a interface desktop
  conserva seu destaque. Os 15 casos passaram após a correção.
- `placement-feedback-red.xml` reproduziu o arco de **18 cm** depois de
  colocação, captura e promoção aceitas. Colocação física passa a usar
  encaixe sem arco, em até 100 ms; clique e IA conservam a animação. A
  promoção mantém esse caminho até a escolha; a animação anterior é
  invalidada quando outra começa na mesma peça.
- O mesmo RED reproduziu `ReleasePiece` informando sucesso durante
  suspensão, embora nenhum lance fosse aplicado. O retorno agora acompanha
  o aceite real pelo controlador, incluindo pedido de promoção.
- A revisão independente encontrou cancelamento de promoção que deixava
  o peão no destino sem lance. `placement-cancel-promotion-red.xml`
  confirmou **79,55 mm fora da origem**. Cancelar devolve o peão à casa
  lógica por 150 ms, bloqueia entrada durante o retorno e restaura o corpo
  se a peça continua ativa. Os 28 testes afetados passaram depois do ajuste.

O novo `BoardPlacementPreview` contorna a casa sob a base enquanto a peça
está segurada: verde para destino legal, amarelo para origem e vermelho
para casa ilegal. Usa a mesma projeção de `TryGetSquareAt`, não possui
collider e não escolhe a casa pela mão. Verde indica legalidade da projeção;
os critérios de soltura e contato ainda precisam ser atendidos.

A primeira suíte completa passou **208/208 PlayMode** antes do fechamento
do cancelamento, com hash dos 120 fontes C# constante. A verificação final
inclui também escolha imediata da promoção e contraprova de clique com arco.
O próximo diagnóstico guarda aquisição, últimas poses seguradas em três
fases, saída e resultado em linhas limitadas a 1.700 caracteres. Inclui os
dois attaches, raiz, corpo, rotação e escalas para localizar onde surge o
desvio observado no Quest, sem log por frame. A causa das recusas
intermitentes e do tremor continua sem confirmação física.

A verificação final passou **209/209 PlayMode e 64/64 EditMode**, com os
120 fontes C# constantes. Relatórios em
`TestResults/Quest3Hands/placement-feedback-verified-PlayMode.xml` e
`placement-feedback-verified-EditMode-recheck.xml`; hashes e tentativas em
`.local/quest3/hands-placement-feedback-tests.json`. A rodada anterior de
209 casos expôs dois replays sem frequência de frames controlada; um
estimou 10,12 m/s numa entrada que deveria estar parada. Os dois fixtures
passam a usar 72 Hz, e o arremesso é comandado e medido em 0,72 m/s. Os
17 casos afetados e a suíte completa passaram com essa calibração, sem
alterar os limites do jogo. O primeiro EditMode teve 63/64 por falta do
arquivo de PID do motor simulado no prazo de 250 ms; a repetição completa
com os mesmos fontes passou 64/64. Os relatórios com falha foram preservados.

O usuário preferiu testar apenas a colocação neste candidato. As comparações
de visual nativo e resolução reduzida permanecem adiadas; o perfil gráfico
aprovado foi preservado.

### Candidato com destino visível e encaixe curto instalado

- APK `Builds/Quest3/XadrezCGI-Quest3-hands-placement-feedback-candidate.apk`,
  **265.477.193 bytes**, SHA-256
  `158d99cdbb7039fea12b8bc48c8582b7b34880bd1ecebb291ff260227f79aa01`.
  Build otimizado `Succeeded`, quatro avisos e zero erros, com diagnóstico
  temporário habilitado apenas pelo símbolo passado nesse build.
- Assinatura, alinhamento de 16 KiB, manifesto, bibliotecas ARM64, motor
  offline e fonte correspondente aprovados na inspeção do candidato.
  Os fontes do build coincidem com os verificados na suíte final.
  O perfil Mobile foi restaurado após prefilters; os APKs anteriores,
  inclusive a demonstração aprovada `bc286bc…`, mantêm seus hashes.
- A instalação retornou `Success`. O hash do `base.apk` instalado coincide
  com `158d99cd…`; a abertura normal retornou `Status: ok`, PID `19095`.
  A leitura inicial não mostrou exceções gerenciadas nem erro fatal.
  Isso não confirma ainda o aceite físico da colocação ou da fluidez.
- Evidências em `.local/quest3-device/2026-10-23-hands-placement-feedback/`.
  Coleta contínua limitada a cinco minutos preserva aquisição, últimas
  poses seguradas e resultado antes que o buffer do aparelho descarte
  tentativas. O roteiro pede um peão da vez, uma casa à frente, verificando
  se o contorno fica verde e o lance permanece aceito.
- A recusa intermitente permanece em investigação até esse teste. O
  diagnóstico temporário deve ser removido após a captura e antes de
  concluir a próxima revisão normal de trabalho/demonstração.
- O reteste foi bloqueado no menu: o usuário relatou que fechar os dedos
  não clicava e não conseguiu iniciar a partida. A sessão focada registrou
  as mãos rastreadas, sem tentativa de soltura. Esse APK não recebeu aceite
  de colocação; `session.json` preserva o feedback.

### Pinça do menu: reprodução e correção

O menu usava o `ReleaseThresholdButtonReader` do exemplo XRI com limiares
1,0/0,9. Mesmo com `MetaAim.indexPressed` verdadeiro a 0,85, seu ramo de
pressão exigia valor superior a 0,9. O teste real
`HandMenuInputTests` alimentou o dispositivo MetaAim, confirmou o raycast
no botão `LocalModeButton` e recebeu zero cliques nas duas mãos
(`menu-native-pinch-red.xml`, 0/2).

O clique compartilha agora a leitura exclusiva da pinça usada pela pega,
com limiares 0,75/0,55. `XRHandMenuInput` cancela o registro do ponteiro
quando a entrada fica inválida, antes do processamento da UI, para uma
interrupção não virar clique por soltura. A retomada ainda exige abrir
antes de uma nova pinça. O gatilho dos controles permanece igual.

- Primeira rodada: **25/25** casos de menu, pega, modalidade e HUD.
- Rodada final: **64/64 PlayMode** em
  `TestResults/Quest3Hands/menu-native-pinch-verified.xml`; inclui início pelo
  menu nas duas mãos, histerese, perda de rastreamento/foco, pausa e troca
  de modalidade, inclusive interrupção e retorno sem um frame intermediário,
  além de colocação, apoio e proteção de vizinhas. Hashes congelados em
  `.local/quest3/menu-native-pinch-verified-tests.json`.
- O diagnóstico temporário registra pressão/click e estado do leitor/raio,
  além das poses de aquisição e soltura. O reteste físico precisa confirmar
  primeiro o acesso ao menu e depois a colocação recusada. Resolução,
  materiais e perfil gráfico não foram alterados nesta correção.
- Candidato instalado e iniciado: `Builds/Quest3/XadrezCGI-Quest3-hands-menu-pinch-candidate.apk`,
  **265.486.677 bytes**, SHA-256
  `4751f26e9eaecdde8708f41e9f2c2a4373b5d60979b951125686b8aa669ff998`.
  Build `Succeeded` com quatro avisos e zero erros; assinatura, manifesto,
  alinhamento, ARM64 e motor offline aprovados. O metadata inclui o novo
  leitor de menu e exclui os testes. Os hashes de C# coincidem com a rodada
  verificada. A instalação retornou `Success`, o hash remoto corresponde,
  e a abertura retornou `Status: ok`, PID `30713`, sem exceções gerenciadas
  ou erro fatal na captura inicial. Aceite no headset ainda pendente.
- Evidências em `.local/quest3-device/2026-10-23-hands-menu-pinch/`, com coleta
  contínua limitada a dez minutos. O APK `158d99cd…` e a demonstração aprovada
  `bc286bc…` foram preservados. Este candidato ainda contém diagnóstico
  temporário e não substitui o checkpoint aprovado de demonstração.

### Reteste de menu e colocação confirmado em 23/10/2026

No candidato `4751f26e…`, o usuário confirmou **“Sim, o menu funciona”** e
**“Sim, contorno e colocação funcionam”**. Esse aceite cobre acesso à partida
por pinça e a colocação solicitada de um peão. A captura parcial reteve um
clique em `StartPlayButton` e dois resultados `move-started`, após contato
real com o tabuleiro, sem exceções gerenciadas ou erro fatal. O buffer não
preservou todas as tentativas, portanto esses registros não são uma taxa de
acerto. Evidência em `post-feedback-analysis.json` e `session.json` da sessão.

O tremor das mãos e a fluidez não foram reavaliados nesse aceite; o usuário
priorizou apenas a colocação. A resolução e o perfil gráfico são os mesmos.
Também permanecem os critérios próprios de captura, roque, promoção, arremesso
e retomada prolongada no headset.

Após a captura, as sondas temporárias e o modo de comparação visual foram
arquivados em `.local/quest3/accepted-menu-pinch-diagnostics/` e retirados do
código. A ponte conserva as mesmas decisões de pega/soltura/colocação. O
perfil normal recupera sua inicialização de foveation 0,5; o diagnóstico de
renderização continua disponível somente no APK de desenvolvimento. A revisão
independente confirmou a equivalência do código funcional e preservação dos
APKs aprovados. A versão normal de trabalho usa o arquivo separado `-work.apk`.

A suíte completa após a limpeza passou **218/218 PlayMode e 64/64 EditMode**,
sem mudar fontes durante a execução. Relatórios em
`TestResults/Quest3Hands/hands-work-release-PlayMode.xml` e
`hands-work-release-EditMode.xml`, com hashes congelados em
`.local/quest3/hands-work-release-tests.json`. O wrapper de build é normal,
sem símbolos de diagnóstico temporário e sem alterar o perfil Mobile.

### Versão normal de trabalho instalada em 23/10/2026

- APK: `Builds/Quest3/XadrezCGI-Quest3-work.apk`, **265.440.677 bytes**.
  SHA-256 `e4832a96349ac11c5ebafb4e88038ca3e4de4e161f10460e769ee161888b2e16`.
  Cópia recuperável em `Builds/Quest3/working-20261023/`, com manifesto do
  checkpoint. A demonstração de 22/10 e o candidato aceito `4751f26e…`
  conservam seus arquivos e hashes.
- Build normal `BuildOptions.None`, sem símbolos temporários. Resultado
  `Succeeded`, quatro avisos e zero erros. Assinatura, manifesto, alinhamento
  de 16 KiB, ARM64, licença e fonte do motor offline aprovados.
- O metadata inclui pinça, guarda do menu, contorno de colocação e perfil
  normal; exclui testes, sondas temporárias e diagnóstico de renderização.
  Os fontes do build correspondem à suíte completa aprovada.
- Instalação `Success`; hash do `base.apk` remoto igual ao arquivo local.
  Abertura normal `Status: ok`, PID `14194`, sem exceções gerenciadas ou
  erro fatal na captura inicial. O aplicativo continua identificado como
  **Xadrez CGI**, pacote `com.mathwidu.chesscgi`, e roda sem o cabo.
- Evidências em `.local/quest3-device/2026-10-23-hands-working/` e
  `.local/quest3/hands-work-release-build.json`. O aceite do comportamento
  foi realizado no candidato anterior; a cópia normal após a limpeza ainda
  não foi retestada pelo usuário. A revisão confirmou equivalência funcional,
  e os 218/64 testes passaram após essa limpeza. Tremor e fluidez seguem com
  avaliação própria pendente.

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

- [Unity 6.3: interpolação de corpos físicos](https://docs.unity3d.com/6000.3/Documentation/Manual/rigidbody-interpolation.html).
- [Unity 6.3: exclusão de camadas por collider](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Collider-excludeLayers.html).
- [Unity 6.3: ambiente Android](https://docs.unity3d.com/6000.3/Documentation/Manual/android-sdksetup.html).
- [Unity 6.3: plataforma Meta Quest](https://docs.unity.com/en-us/engine/6000.3/manual/xr/configuring-project-for/meta-quest-develop/build-profile).
- [OpenXR 1.16.1: Meta Quest Support](https://docs.unity3d.com/Packages/com.unity.xr.openxr@1.16/manual/features/metaquest.html).
- [OpenXR: validação do projeto](https://docs.unity3d.com/Packages/com.unity.xr.openxr@1.16/manual/project-validation.html).
- [Meta: preparação de software e dispositivo](https://developers.meta.com/vr/documentation/unity/unity-software-setup/).
