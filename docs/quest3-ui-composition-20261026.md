# Interface independente da resolução do cenário

Solicitação: tornar menu e painéis mais legíveis sem aumentar o cenário de
65%, preservando a seleção por pinça e a profundidade das mãos/peças.

Candidato corrigido local `0.1.4-ui.2 (4)`. O versionCode permanece 4 nesta comparação
para permitir restaurar diretamente os APKs aceitos com `adb install -r`,
sem desinstalar o aplicativo nem apagar preferências. A identificação completa
continua no rodapé, com a data de compilação.

## Candidato instalado

**Atualização corrigida instalada:** `0.1.4-ui.2 (4)`, em 26/10/2026 às
21:04:53, no mesmo pacote, com dados preservados. Rodapé:
**v0.1.4-ui.2 (4) · 26/10/2026 21:02:28 Brasília (UTC-3)**.
SHA-256 local e instalado conferidos:
`cd2c1279b7129e9b9aaaec31a7f9e5bda647c28213f37c90d9928b705fec0794`.
Os registros confirmaram ativação nativa e captura 2048×1152 com câmera Game
e escala recebida 0,65, sem fallback nem erro de aplicação. A primeira coleta
teve `SF=0` e nenhum alvo de projeção válido, enquanto o headset estava fora
de uso; esses valores não comprovam resolução real nem desempenho. O reteste
de piscadas, nitidez e pinça foi solicitado ao usuário.

Primeiro candidato `0.1.4-ui.1` instalado por `adb install -r` em 26/10/2026 às 20:49, no mesmo
pacote `com.mathwidu.chesscgi`, com os dados preservados. O SHA-256 do APK
instalado foi conferido: `b874548c15c5a090129cda2cbf6fc684a72b3b551c06cb618d79b7d9dd1b4e2d`.
O rodapé desta compilação é **v0.1.4-ui.1 (4) · 26/10/2026 20:43:09 Brasília (UTC-3)**.

A primeira abertura encontrou o Quest em repouso: o processo iniciou sem
erros registrados, mas a cena e o compositor ainda não haviam iniciado.
Após acordar, `native_underlay` e captura nativa 2048×1152 foram confirmados.
O usuário aprovou a nitidez do menu, mas relatou glitches piscando; este primeiro
candidato foi rejeitado para entrega. O log contínuo mostrou `SF=0.65/1.00`
alternando e alvo XR 1680×1760 apesar do perfil 0,65. A revisão dos fontes do
URP confirmou que a câmera Preview escreve escala global 1, mesmo com XR
desabilitado nela. A correção deve manter a captura nítida sem essa alternância.

## Implementação

- `com.unity.xr.compositionlayers` 2.6.1, com o provider do OpenXR 1.16.1
  já instalado; a feature é habilitada apenas para Android.
- `QuestHudComposition` captura o Canvas original em 2048 × 1152,
  no máximo 36 vezes por segundo. O compositor mantém a reprojeção da camada
  ligada à pose do headset; a entrada continua sendo processada no fluxo XR
  existente. A frequência da captura limita a atualização visual do HUD.
- A câmera de captura é separada, ortográfica, sem HDR, sombras, pós-processamento
  ou XR. A câmera é do tipo `Game`, e `QuestHudCaptureFeature` ajusta somente
  seu alvo em `OnCameraPreCull`, depois que o URP aplica a escala do cenário
  aos displays XR. Mantém RenderGraph habilitado; o modo de compatibilidade
  é recusado. O cenário mantém MSAA 4× e foveation 0,5.
- A interface usa uma camada quad atrás da projeção principal. Uma superfície
  sem collider reserva sua cobertura no framebuffer, respeitando profundidade.
  A textura de cobertura e a textura de cor resolvida mantêm transparência dos
  painéis; o quad acompanha o plano e as dimensões do Canvas.
- `Canvas.worldCamera` e a câmera do `XRUIInputModule` continuam na câmera
  dos olhos. A camada Unity 25 (`QuestHudCapture`) é excluída da câmera dos
  olhos e incluída nas máscaras dos raios das duas mãos. Os botões originais
  continuam ativos e recebem os mesmos eventos.
- A câmera XR recebe a tag `MainCamera`, exigida pelo cálculo de pose do provider.
  Uma câmera incompatível no cache do compositor provoca retorno ao Canvas
  normal, com diagnóstico, em vez de exibir um painel desalinhado.
- Sem provider nativo (desktop/Editor), o HUD original é mantido. Desativação
  libera as texturas e restaura as camadas e o bit de culling apropriado.
- Uma guarda verifica que a feature preparou o alvo e que a escala XR continua
  igual à escala do cenário após cada captura. Incompatibilidade retorna ao
  Canvas original com diagnóstico. O preparo invalida renderers já carregados
  ao adicionar a feature, além de salvar os assets.

## Verificação e limites

Evidências locais em `.local/quest3/ui-layers-20261026/`. Testes de captura
verificam detalhe de um texel em escala 0,65, composição de alpha, atualização
de controles, correspondência dos cantos do Canvas e descarte dos recursos.
Testes de entrada reproduzem pinças das duas mãos pelo modelo XR real.
O Editor não comprova a composição nativa, nitidez percebida, oclusão no Quest
ou custo de GPU. A aprovação visual e de interação continua dependente do teste
físico deste APK.

Suítes completas: **284/284 PlayMode e 70/70 EditMode**, sem skips, com
hashes dos fontes preservados durante a execução. A primeira rodada EditMode
teve um timeout intermitente no teste de falha de processo da IA desktop
(prazo de 1 segundo). O grupo isolado passou 4/4 e a segunda rodada completa
passou 70/70; código de IA e expectativas desse teste permaneceram iguais aos
da versão aceita. As evidências da primeira execução foram mantidas.

Para a correção `ui.2`, a suíte direcionada de captura passou **4/4**, mantendo
contraste de um texel acima de 0,9 e verificando a escala recebida do URP antes
do ajuste local. Suítes finais: **284/284 PlayMode e 70/70 EditMode**, sem skips.
A primeira rodada EditMode teve dois casos de timeout de processo desktop
(limites de 1 segundo e 250 ms), incluindo ausência do arquivo PID antes da
inicialização do processo. O grupo passou 4/4 isoladamente e a suíte completa
passou 70/70 na repetição. Os 11 fontes de IA/teste conferidos são idênticos
ao candidato anterior; nenhuma expectativa ou limite foi relaxado. Evidências em
`.local/quest3/ui-layers-fix-20261026/`; o candidato anterior e seus logs foram
arquivados em `Builds/Quest3/ui-candidate-20261026-v0.1.4-ui.1/`, identificado
como rejeitado por piscadas.

Uma coleta de desempenho deste candidato deve considerar que a interface
acrescenta uma camada nativa: o critério antigo `LCnt=1` não se aplica sem revisão.
Não confundir painel Meta aberto/ausência do usuário com desempenho em partida.

## Restauração preservada

- Perfil de trabalho aceito em 65%:
  `Builds/Quest3/comparison-20261026-scale065/XadrezCGI-Quest3-scale065.apk`,
  SHA-256 `4d96b00eef2223983f2ce470c5b3a93fa87321c29b926b22f06194559961ab95`.
- Estável anterior em 80%:
  `Builds/Quest3/stable-20261026-v0.1.3/XadrezCGI-Quest3-stable.apk`,
  SHA-256 `541bf61c8f198f504778035a0d0c4b62be4690709c6030a387bace9aed08532e`.

`docs/quest3-stable.json` continua apontando para a estável aceita; este candidato
não a substitui automaticamente.

Referência oficial: [XR Composition Layers](https://docs.unity3d.com/Packages/com.unity.xr.compositionlayers@2.6/manual/index.html).
As APIs e limitações foram conferidas também nos fontes e na documentação
distribuída com os pacotes instalados.
