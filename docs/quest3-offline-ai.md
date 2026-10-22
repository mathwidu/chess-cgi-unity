# IA offline no Quest 3

## Estado em 19/10/2026

A build Android passa a usar Stockfish 18 incluído no APK, com suas duas redes
NNUE incorporadas. O jogo não baixa o motor e não consulta serviços durante a
partida. A preparação no Mac pode baixar dependências uma vez; depois o cache
verificado permite reconstrução offline.

O adaptador preserva `IMoveChooser`, `ComputerTurnCoordinator`, a fotografia da
posição e a validação de toda candidata em `ChessRulesAdapter`. Desktop mantém
o transporte anterior. Cenário, personagens, HUD e modo local não foram alterados.
A realidade mista permanece uma opção futura, mantendo também a sala virtual.

**Atualização de 21/10/2026:** com o modo de desenvolvedor liberado, o APK abriu
no Quest 3 e `libstockfish.so` foi observado como processo filho do jogo.
A inicialização pelo aplicativo foi comprovada; jogada legal sem rede,
cancelamento, pausa/reinício e orçamento de CPU/GPU ainda precisam de validação.
Veja o [registro da sessão física](quest3-standalone.md#primeira-sessão-física-em-21102026).

## Implementação

- Android IL2CPP não implementa a criação de processos de
  `System.Diagnostics.Process`. `ComputerOpponentFactory` seleciona
  `AndroidStockfishMoveChooser` somente no player Android; o Editor continua
  usando `StockfishUciMoveChooser`.
- O executável ELF64/AArch64 PIE recebe o nome de empacotamento
  `libstockfish.so`. Android o extrai para `ApplicationInfo.nativeLibraryDir`;
  `StockfishProcess` o inicia com `ProcessBuilder`, sem shell, cópia para pasta
  gravável, `chmod` ou carregamento como biblioteca compartilhada.
- O build exige Android ARM64, impede pré-carregamento do executável, confere
  hash/proveniência/licença e conserva seus bytes durante o Gradle. O hook
  define extração, `useLegacyPackaging` e regra de preservação da classe Java.
- Classe e contexto Java são resolvidos na thread Unity. As operações de busca
  e limpeza ocorrem em workers anexados à JVM, com desligamento ao terminar cada
  chamada. O processo persiste entre lances da mesma partida.
- O protocolo usa filas limitadas e threads Java para stdin/stdout; stderr é
  drenado junto de stdout. Apenas linhas de telemetria `info` são descartadas.
  Saturação, EOF, arquivo ausente e jogada malformada viram erros recuperáveis.
- A leitura tem polling de 50 ms. Cancelamento, timeout e descarte do chooser
  encerram a sessão no worker; pausa/reinício/menu não aguardam o processo na
  thread principal. Mesmo um stdin bloqueado não impede o cancelamento.
- Inicialização mantém `Threads=1`, `Hash=16`, `Ponder=false` e
  `UCI_LimitStrength=false`. São os mesmos perfis desktop: Iniciante 0/150 ms,
  Intermediário 6/500 ms, Difícil 14/1200 ms. Os nomes não representam Elo.
  Prazos totais de 6000/6500/7200 ms incluem a inicialização.

## Preparar e gerar

Com Unity fechado, na raiz do repositório:

```bash
python3 tools/setup_stockfish_android.py
python3 tools/setup_stockfish_android.py --verify-only --offline
bash tools/build_quest3.sh prepare
bash tools/build_quest3.sh build
```

O setup usa o NDK r27c do Unity 6000.3.16f1 já instalado. A versão `sf_18` e os
hashes das entradas são fixos. O pacote ARMv8 oficial continha um executável
`ET_EXEC`; o script recompila sua fonte original como PIE com alinhamento de
16 KiB. As duas redes padrão são verificadas byte a byte dentro do binário.

O wrapper verifica o payload sem rede antes de abrir Unity. Depois da build,
reúne o APK, licença, manifesto e bundle da fonte correspondente em
`Builds/Quest3/`, e executa `tools/verify_quest3_apk.py`. Esse verificador confere
assinatura, alinhamento ZIP, manifesto, classe Java no DEX, ABI, hash do motor
empacotado e fonte correspondente.

O binário grande é regenerável e ignorado pelo Git; seus metadados, receita,
testes e avisos ficam versionáveis. O [guia do pacote](stockfish-android-package.md)
detalha entradas, flags, reprodução e arquivos de evidência.

Ao entregar o APK a outra pessoa, mantenha junto os arquivos
`stockfish-18-android-corresponding-source.tar.gz`, `Stockfish-Copying.txt` e
`stockfish-manifest.json`. A licença GPL do Stockfish foi preservada; a licença
do restante do projeto não foi alterada.

## Verificação desta rodada

- Pacote nativo: 8 testes Python aprovados, preparo offline repetível e duas
  compilações com SHA-256 idêntico. Executável PIE de 113.750.848 bytes,
  dependências somente `libc.so`, `libdl.so`, `libm.so`; os quatro segmentos
  `LOAD` têm alinhamento de 16 KiB. Isso não mede o kernel do headset.
- SHA-256 do motor:
  `60d57aad661e3fe2f9c7731ae63d4c9687455adfdc863e99dbff0ce3ba5b82bc`.
- SHA-256 do bundle da fonte:
  `5162e27b2f8387d8049456e58266e696c1331d4a283aa91b4773137720746ca6`.
- O helper Java real foi compilado e executado na JVM do Mac com Stockfish
  macOS: handshake, reutilização, abertura, mate, promoção, polling, três
  cancelamentos/reaberturas, EOF, comando inválido, arquivo ausente, stdin
  bloqueado e ausência de processos filhos residuais. Log em
  `.local/quest3-ai/java-smoke.log`. Isso valida o transporte, não o executável Android.
- **52/52 EditMode aprovados**, incluindo seis novos casos do adaptador:
  protocolo/histórico, cancelamento e resposta tardia, descarte durante handshake,
  EOF, jogada malformada e cancelamento de busca enfileirada.
  A primeira execução travou no harness de asserções assíncronas; o harness foi
  corrigido para aguardar tarefas sem bloquear o contexto do Unity.
- **133/133 PlayMode aprovados na execução completa final**. A primeira suíte
  terminou 132/133: a comparação de imagem
  `ClassroomEnvironmentTests.MenuStudioLightCannotRelightTheClassroom` falhou.
  Ela passou isoladamente e na segunda suíte completa, sem alteração no código
  visual ou no teste. A causa da ocorrência intermitente não foi comprovada;
  preservar os logs `PlayMode.log`, `Lighting-isolated.log` e
  `PlayMode-repeat.log` em `TestResults/Quest3AI/`.
- Build Android IL2CPP concluída com **zero erros** e quatro avisos. APK de
  desenvolvimento em `Builds/Quest3/XadrezCGI-Quest3-dev.apk`, 302.433.691 bytes
  (aproximadamente 302 MB). SHA-256:
  `a42adbc506533995a3c606ba336894412930328e2d48108c16e9290147c857be`.
  Assinatura, alinhamento ZIP, manifesto Quest, ABI ARM64 exclusiva, extração
  do executável, classe Java no DEX e hashes do motor/avisos/fonte passaram na
  inspeção automática. Resultado em `.local/quest3-ai/apk-inspection.json`;
  log do build em `.local/quest3/build-20261019-095851-88770.log`.
  O APK anterior foi preservado em
  `.local/quest3-ai/baseline/XadrezCGI-Quest3-without-ai.apk`.
- Revisão independente encontrou risco de stdin bloqueado impedir cancelamento;
  a fila de escrita e o teste correspondente foram acrescentados e revisados.
  Nenhuma execução no headset ocorreu.

## Primeira validação no aparelho

1. Com modo de desenvolvedor e instalação liberados, iniciar o APK e selecionar
   **Contra IA**, Brancas, Iniciante. Desligar a conexão de rede e realizar um
   lance; conferir resposta legal e ausência de bloqueio do tracking/HUD.
2. Iniciar com Pretas e verificar a primeira jogada da IA. Repetir as três
   dificuldades, promoção e fim de partida.
3. Durante uma busca, voltar ao menu, reiniciar e suspender/retomar. Confirmar
   posição preservada quando aplicável, sessão nova e nenhuma jogada atrasada.
4. Registrar versão do Horizon OS, logs e comportamento do processo sob o UID
   do aplicativo. Executar o binário somente por `adb shell` não valida essa permissão.
5. Medir CPU/GPU, memória e estabilidade de quadros com IA pensando e parada,
   usando a sala e os personagens. Ajustar limites somente a partir dessas medições.

## Fontes técnicas

- [Unity 6.3: plug-ins nativos Android](https://docs.unity3d.com/6000.3/Documentation/Manual/AndroidNativePlugins.html).
- [Unity 6.3: AndroidJavaObject e threads](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/AndroidJavaObject.html).
- [Android: restrição de execução em diretórios graváveis](https://developer.android.com/about/versions/10/behavior-changes-10#execute-permission).
- [Android: extração de bibliotecas nativas](https://developer.android.com/guide/topics/manifest/application-element#extractNativeLibs).
- [Android: páginas de 16 KB](https://developer.android.com/guide/practices/page-sizes).
- [AOSP Android 15: permissão de execução de arquivos instalados](https://android.googlesource.com/platform/system/sepolicy/+/refs/heads/android15-release/private/app.te#437).
- [Stockfish: integração e licença](https://official-stockfish.github.io/docs/stockfish-wiki/Developers.html).
