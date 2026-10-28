# Otimização no Quest 3 — 26/10/2026

## Comparação no aparelho

As duas primeiras coletas usam o mesmo APK **0.1.1 (2)**, escala **0,90**.
Todas mantêm MSAA **4×**, foveation **0,5**, HDR e sombras desligados. O
usuário olhou para o tabuleiro em Dois jogadores; a segunda coleta usou
a opção existente Modo desempenho.

| Apresentação | Amostras sem overlay | Mediana FPS | Mediana App | GPU | Frames antigos |
| --- | ---: | ---: | ---: | ---: | ---: |
| 32 personagens originais | 19 | 34 | 27,21 ms | 96% | 42 |
| Peças clássicas | 18 | 72,5 | 10,77 ms | 85% | 0 |
| Personagens reduzidos, 0.1.2 / escala 0,90 | 14 | 53,5 | 16,83 ms | 98% | 19 |
| Personagens reduzidos, 0.1.2 / escala 0,80 | 19 | 56 | 16,40 ms | 98% | 17 |
| Detalhe por distância, 0.1.3 / escala 0,80 | 30 | 65,5 | 14,06 ms | 98% | 8 |

São amostras VrApi de aproximadamente um segundo, coletadas por 30 segundos.
Somente `LCnt=1` entra no resumo para excluir o overlay do sistema. Não há
garantia de enquadramento idêntico: a cabeça e as mãos se moveram. O salto
com as peças clássicas aponta custo importante na apresentação dos
personagens; sozinho não separa geometria de materiais e outros passes.
A coleta inicial em repouso foi retida como sem amostra válida.

Durante o ensaio, a seleção pelas mãos parou de responder. O log foi salvo
antes de reiniciar o mesmo APK. O usuário confirmou recuperação após a
reabertura e abertura dos dedos. Não foi isolada uma causa; a otimização de
renderização não deve ser apresentada como correção comprovada desse caso.

Evidências locais: `.local/quest3/performance-20261026/`, especialmente
`baseline-board-awake.json`, `primitive-board.json`, respectivos logs,
`baseline-board.png` e `input-unresponsive-before-restart.log`.

## Candidato 0.1.2 (3) com personagens

Instalado e iniciado às 16h53 (São Paulo), com hash remoto
`c145a6650a1c77a363217aeee99725651ef62dd73acc2ad19f5b03554aff8c09`
igual ao APK inspecionado. Menu: `v0.1.2 (3) · 26/10/2026 16:48:10 Brasília (UTC-3)`.
Sem erro gerenciado/fatal na captura de abertura. O headset estava em
repouso; essa abertura não mede FPS nem comprova conforto.

- Derivação das seis peças personalizadas: **389.424 triângulos** no
  tabuleiro completo, contra 1.345.592. Materiais/texturas de ambos os times,
  partes e bases mantidos. Fontes Blender e comparação em
  [`art/quest3-optimization-20261026/`](../art/quest3-optimization-20261026/README.md).
- A fábrica calcula escala e collider com o modelo original e só depois
  troca a malha visível. Bounds dos renderers permanecem os originais.
  Peças capturadas também usam a derivação; peças clássicas e desktop não.
- O preview da peça selecionada passa a reutilizar a imagem enquanto não
  houver mudança. Zoom, giro, deslocamento e troca da seleção atualizam a
  textura. O clone tem física e interação desativadas e mantém a textura
  de 1104×660 com MSAA 4×.
- Perfil inicial do candidato permanece igual ao da comparação, para medir
  o efeito das otimizações antes de aumentar a resolução.

A primeira medição do candidato ficou em **53,5 FPS** de mediana (51–73),
com GPU em 98%. Melhorou frente aos 34 FPS, mas **não passou** a meta de
72 Hz. O usuário confirmou que a partida estava aberta durante a coleta e
que voltou ao menu depois; a captura PNG posterior mostra esse menu. O
conforto das mãos e a aceitação visual continuam sendo gates físicos.

A comparação seguinte reduz somente a escala de 0,90 para **0,80** (21%
menos pixels), mantendo personagens e MSAA 4×. Compilação identificada por
`v0.1.2 (3) · 26/10/2026 16:57:16 Brasília (UTC-3)`, SHA-256
`18c8da7b4b0ef9ef0816f85717c60c86a50f0b387f637efbc2d946486ca033c5`.
Os fontes C# continuam iguais aos das suítes completas; só o perfil e a
data de compilação mudaram. Evidências em `.local/quest3/performance-scale080-20261026/`.

A coleta de 17h17 ficou em **56 FPS** de mediana (40–59), ainda abaixo de
72 Hz, com GPU em 98%. O usuário aprovou a definição dos textos/personagens
em 0,80 e confirmou menu e colocação de um peão com cada mão. Reduzir 21%
dos pixels trouxe pouco ganho: a geometria é a próxima hipótese a isolar.
Isso não comprova a causa do episódio anterior de perda de seleção.

Verificação local: **261/261 PlayMode e 70/70 EditMode**, sem skips e sem
mudança dos fontes C# durante as suítes. Após o saneamento das tangentes
exportadas, **19/19** casos de integração de geometria passaram nos assets finais.
Os relatórios de geometria comprovam que somente tangentes foram alteradas,
com posições, índices, UVs, normais e materiais preservados.

## Versão 0.1.3: detalhe por distância e aceite para entrega

O candidato **0.1.3 (4)** mantém escala 0,80, MSAA 4× e foveation 0,5.
Adiciona uma segunda malha das seis peças personalizadas: **150.720
triângulos** quando todas estão em detalhe de distância, 61,3% abaixo dos
389.424 da malha de inspeção. Texturas, materiais, partes e bases continuam
os mesmos. As fontes, o processo e os limites visuais estão em
[`art/quest3-table-detail-20261026/`](../art/quest3-table-detail-20261026/README.md).

A troca considera diâmetro aparente da peça/distância da câmera, incluindo
a escala do tabuleiro. Entra em inspeção a partir de 0,45 e retorna ao
detalhe de distância abaixo de 0,32; a faixa intermediária mantém o estado.
Hover e pega usam inspeção. O preview da peça selecionada fixa inspeção e
desliga a adaptação no clone. Os mesmos MeshFilters/renderers mantêm os
limites originais; física e aceitação de lances não mudam. Contornos
acompanham a malha, sem duplicar peças nem adicionar colliders.

Antes do APK, foram conferidos os seis arquivos Blender reabertos, os GLBs
exatos reimportados e a correspondência dos nomes/transformações/UVs,
normais e tangentes. As suítes completas passaram **278/278 PlayMode e
70/70 EditMode**, sem skips e com os fontes congelados. A revisão
independente dos clones, contornos, fallback e preservação da física
terminou sem achados acionáveis.

APK normal instalado às 17h42, rodapé **v0.1.3 (4) · 26/10/2026 17:39:37
Brasília (UTC-3)**; SHA-256
`541bf61c8f198f504778035a0d0c4b62be4690709c6030a387bace9aed08532e`.
A coleta entre 17h43m00s e 17h43m30s teve 30 amostras sem overlay e mediana
de **65,5 FPS** (63–68), GPU 98%, temperatura 34 °C e 8 frames antigos.
Ainda **não passou** o critério de 72 Hz. O usuário aprovou a aparência no
tabuleiro e de perto, relatou movimento fluido e jogo normal, e solicitou
explicitamente fixar **esta versão** como estável para a entrega.

O mesmo APK aceito permanece no Quest e está preservado com os fontes em
`Builds/Quest3/stable-20261026-v0.1.3/`. Nenhuma otimização adicional foi
aplicada depois do aceite. Evidências em
`.local/quest3/performance-detail-20261026/`, incluindo
`adaptive-detail-board.json`, `physical-acceptance.json` e o checkpoint.
