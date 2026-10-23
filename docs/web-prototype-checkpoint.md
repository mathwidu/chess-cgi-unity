# Checkpoint: protótipo Unity Web disponível localmente

Pausa e retomada solicitadas pelo usuário em 23/10/2026. O build otimizado foi
concluído e verificado no navegador após a retomada. Este arquivo mantém o
registro da pausa e o estado final para continuidade.

## Isolamento

- Worktree: `/Users/mathwidu/.codex/worktrees/chess-web-prototype/chess-cgi-unity`.
- Branch: `codex/unity-web-prototype`, base `30bcbfe`; o commit de release será
  registrado no plano local de publicação.
- O checkout original e suas alterações de VR/animação permanecem separados.
- Unity 6000.3.16f1 com Web Build Support instalado. Não houve push ou deploy.

## Concluído e verificado

- Personagens, IA Stockfish em Worker/WASM, seleção por toque, zoom por pinça,
  HUD responsivo, histórico e ficha da peça sob demanda implementados.
- Primeiro build Web executou em IAB e Chrome. Lance humano `e2-e4`, IA real
  `e7-e5`; sem erros no console dos cenários verificados.
- Chrome com toque emulado: jogar, selecionar, mover, histórico, ficha,
  pinça, restaurar câmera e rolagem de menu. Viewports 360×640 e 640×360;
  desktop 1280×720 também inspecionado. Isso não é teste físico de celular.
- Unity: EditMode 74/74; seleção PlayMode 40/42 inicialmente, regressão de
  preview desktop corrigida e reteste dos dois casos + layout Web passou 4/4.
  Um teste visual de iluminação foi intermitente; causa não estabelecida.
- Ponte IA Node 6/6, incluindo WASM real; setup Python 5/5.
- Otimizador temporário de assets implementado. `VerifyStageRoundTrip` passou
  no Editor com 22 texturas ETC2/1K, excluindo legados de Resources e restaurando
  fontes/GUIDs. Log: `Builds/web-assets-roundtrip.log`.

## Registro histórico da pausa

- Compilação otimizada interrompida a pedido do usuário; log
  `Builds/web-build-optimized.log`. Não usar seu conteúdo como build final validado.
- Recuperação dos assets executada com
  `ChessWebAssetOptimizer.RestoreInterruptedStage`; processo terminou com sucesso.
- Perfis nativos, QualitySettings e migrações automáticas de OpenXR/UnityConnect
  restaurados no worktree. Os nove arquivos protegidos conferem com os hashes
  anteriores em `Builds/web-preservation-before.json`.
- Sem pasta de estágio nem journal residual. Servidor local e abas temporárias
  encerrados. Agentes auxiliares concluídos; nenhuma continuação agendada.
- Logs locais tiveram o argumento temporário `-accessToken` removido.

## Concluído após a retomada

- O snapshot nativo agora recusa QualitySettings/pipelines com mudanças não
  salvas, conserva backups em `.local/unity-web-native-settings` e bloqueia
  outro build se uma interrupção deixar recuperação pendente.
- `VerifyNativeSettingsRoundTrip` e o build completo passaram. Os nove hashes
  protegidos conferiram após a Unity encerrar, sem pasta/journal residual.
- Build final: `Builds/web-build-final.log`, resultado `Succeeded`.
  Download inicial **36.670.918 bytes**, ante 145.935.065 (redução de 74,87%).
  IA JS/WASM adiciona 7.316.081 bytes sob demanda. Inventários:
  `Builds/web-download-final.json` e `Builds/web-build-assets.json`.
- Partida/IA novamente exercitadas no IAB. Chrome com user agent Android e
  toque emulados verificou lance, IA, histórico, ficha da peça, pinça sem
  lance acidental, câmera, rotação e rolagem do menu. O ramo mobile iniciou
  em escala 0,85 e limite de 30 FPS. Console sem erros nos cenários executados.
- Comparação visual da sala/tabuleiro/personagens com o baseline. Capturas
  finais desktop, retrato e paisagem em `Builds/WebQA/`.
- Documentação e domainbook atualizados; migrações automáticas não relacionadas
  de OpenXR/UnityConnect restauradas e whitespace dos metadados normalizado.
- Servidor disponível somente em `http://127.0.0.1:8090/` enquanto o processo
  local permanecer ativo. Nenhum deploy ou push foi executado.

## Próximas validações

O orçamento de aproximadamente 15 MB permanece aberto. Medir custo de malhas
e fundo do menu antes de decidir LOD ou carregamento separado. Testar Android
e iPhone físicos, incluindo rede limitada, partida prolongada, aquecimento,
memória, troca de aplicativo, área segura e toque. A emulação no Mac não
comprova esses critérios. A receita de hospedagem pública foi preparada em
`docs/web-deployment.md`; publicação externa ainda depende do plano autorizado.

## Comandos úteis

Sempre executar na raiz deste worktree, sem usar o checkout original:

```bash
/Users/mathwidu/.unity/bin/unity run ./game --timeout 1800 -- -buildTarget WebGL -executeMethod ChessCgiWebBuild.Build -logFile /Users/mathwidu/.codex/worktrees/chess-web-prototype/chess-cgi-unity/Builds/web-build-optimized.log
python3 tools/serve_web.py
"$HOME/.local/bin/graphify-project" ensure --root .
```

O preparo Web já foi executado. Para um checkout limpo no Mac, seguir a etapa
de `Prepare` em `docs/web-prototype.md` antes da primeira troca de plataforma.
Não imprimir cabeçalhos de logs Unity: a CLI pode incluir um access token
temporário nos argumentos registrados. Os logs não são destinados ao Git.
