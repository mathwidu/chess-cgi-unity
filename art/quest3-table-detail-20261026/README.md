# Peças personalizadas vistas sobre o tabuleiro — Quest 3

Derivação visual para distância, criada com Blender 5.2.1 LTS a partir dos
seis `Assets/Art/Characters/Direction02/<Kind>/Model.glb`. Os originais e
as malhas de inspeção em `quest3-optimization-20261026` foram preservados.

| Peça | Triângulos Table | Quantidade inicial |
| --- | ---: | ---: |
| Peão | 4.664 | 16 |
| Torre | 4.138 | 4 |
| Cavalo | 4.531 | 4 |
| Bispo | 5.175 | 4 |
| Rainha | 4.907 | 2 |
| Rei | 5.453 | 2 |

Total do tabuleiro em Table: **150.720**, contra **389.424** nas malhas de
inspeção e **1.345.592** nos originais. A troca no jogo mantém as mesmas
MeshFilters, materiais, renderers, limites visíveis e colliders. Perto da
câmera, sob hover ou durante a pega, a peça usa a malha de inspeção. O
preview da peça selecionada mantém permanentemente esse detalhe maior.

## Derivação e limites

Comando reproduzível, que recusa sobrescrever a saída existente:

```sh
/Applications/Blender.app/Contents/MacOS/Blender --factory-startup --background \
  --offline-mode --disable-autoexec --python-exit-code 1 \
  --python tools/optimize_quest_characters.py -- --profile table \
  --output /caminho/novo Pawn Rook Knight Bishop Queen King
```

A importação une vértices compatíveis separados por seams. Decimate preserva
8% da topologia do corpo, 25% das mãos, 20% do emblema, 5% da decoração da
base e 10% dos demais acessórios. A superfície principal da base e partes
com menos de 200 triângulos permanecem inteiras. Validação remove faces
duplicadas antes da transferência das normais autorais. A exportação
preserva nomes, coordenadas, UVs e slots; materiais efetivos vêm do prefab
original no Unity. Tangentes exportadas são normalizadas e ortogonalizadas.

Cada pasta contém fonte `.blend` com materiais, `Table.glb` de geometria,
relatório com hash da origem/saída e erro de superfície amostrado, além de
vistas equivalentes original/Table. São preservadas as identidades, cores,
emblemas e acessórios. Table reduz detalhes de rosto e roupa e torna
algumas silhuetas mais angulares; não é a malha usada para inspeção próxima.

Os seis `.blend` foram reabertos em processo separado e os GLBs exatos
reimportados. Contagem de partes/triângulos, UVs, dependências, coordenadas,
normais e tangentes foram verificados. Comparações de frente, costas e
material neutro foram examinadas:

- [Frente, original e Table](comparison-front.png)
- [Costas, original e Table](comparison-back.png)
- [Material neutro, original e Table](comparison-neutral.png)

Evidências: `.local/quest3/performance-detail-20261026/`. Esta verificação
de assets não comprova FPS nem aceitação visual no Quest; a medição e a
interação do APK são registradas em `docs/quest3-performance-20261026.md`.
