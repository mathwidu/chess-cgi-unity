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
- O modo de desenvolvedor do aparelho ainda depende da conta proprietária,
  conforme confirmado pelo usuário. Isso não impede importar, compilar, validar
  configurações ou gerar um APK no Mac; impede esta rodada de teste no headset.
- **Contra IA tem adaptação Android desde 19/10/2026.** O candidato inclui
  Stockfish 18 ARM64 e usa processo Java/UCI. A execução no headset ainda
  precisa ser validada; veja [implementação e evidências](quest3-offline-ai.md).
- A verificação da interface, conforto, escala, iluminação e desempenho no Quest
  exige o aparelho. Simulação no Editor e build bem-sucedido não substituem isso.

O APK de 17/10 exigia selecionar **Dois jogadores**. O candidato atual também
inclui **Contra IA**, com lado e dificuldade, para o primeiro teste offline
quando o dispositivo estiver liberado. O modo local continua disponível.

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
```

`prepare` configura Android pelas APIs do Editor e valida o resultado;
`validate` confere o estado e as regras OpenXR; `build` valida e gera a build
de desenvolvimento com a configuração salva pelo primeiro comando.
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

O perfil de qualidade Mobile já existente é o padrão Android. Esta etapa não
reduz os personagens a peças clássicas, não redesenha a sala e não comprova que
o perfil existente atinge a taxa de atualização necessária. Ajustes visuais e
de desempenho devem partir das medições no aparelho.

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
