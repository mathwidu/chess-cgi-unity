# Correção do zoom e enquadramento — 25/09/2026

A aproximação preserva uma margem acima da cabeça e dos acessórios. O limite
continua em aproximadamente 182%; o recorte inferior é esperado no zoom.
O botão direito (ou do meio) arrastado sobre a imagem desloca o personagem
na direção do ponteiro. Os botões **Mover ↑/↓** ajustam a altura, inclusive
no painel VR. **Restaurar** recupera giro, posição e distância de 100%.

O deslocamento manual é independente do ajuste automático do zoom e do
ponto de giro. Não altera a peça do tabuleiro, a seleção ou a câmera da sala.
Os controles continuam fora da imagem, que mantém 368 × 220 unidades e a
RenderTexture de 1104 × 660.

## Reprodução e resultado

Antes da correção, dois testes falharam: a cabeça saía do quadro no zoom
máximo e o arraste direito não a trazia de volta. No Ricardo, o topo da
cabeça tinha Y de viewport 1,086 antes e depois do arraste. Após a correção,
ficou em 0,896 ao aproximar e em 0,468 após arrastar para baixo, sem reduzir
os 182%. A repetição confirmou a falha original antes da implementação.

Passaram **59/59 testes PlayMode**, incluindo nove do painel. A projeção do
topo dos seis personagens dos dois times foi conferida no zoom máximo.
Eventos reais do Input System exercitaram arraste horizontal/vertical,
zoom e isolamento da câmera da sala. Botões, restauração, troca de peça e
Canvas World Space com XRHMD simulado também foram verificados.

Quatro capturas reais da Main foram inspecionadas: Ricardo em 182%, Ricardo
reposicionado, Ricardo restaurado e Gustavo em 182%. O capturador usa Screen
Space Camera para incluir o HUD; o desktop mantém Screen Space Overlay.
Headset físico permanece pendente. Detalhes em `verification.json`.

Os hashes de 185 arquivos protegidos permaneceram iguais, incluindo modelos,
texturas, cena, ambiente, controles do tabuleiro e configurações XR. Domainbook
validado. Backups deste ajuste estão em `.local/preview-pan-20260925/before/`.

## Reexecutar

Feche o Editor deste projeto antes de executar os comandos:

```bash
'/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity' \
  -batchmode -projectPath "$PWD/game" \
  -runTests -testPlatform PlayMode -testFilter SelectedPiecePreviewTests \
  -testResults "$PWD/.local/preview-pan-20260925/recheck.xml" \
  -logFile "$PWD/.local/preview-pan-20260925/recheck.log"

'/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity' \
  -batchmode -projectPath "$PWD/game" \
  -executeMethod PiecePanelReviewCapture.RunZoomReview \
  -logFile "$PWD/.local/preview-pan-20260925/capture.log"
```
