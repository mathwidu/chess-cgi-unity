# Geometria derivada para o Quest 3

Fontes preservados: `game/Assets/Art/Characters/Direction02/<tipo>/Model.glb`.
Derivação pelo Blender **5.2.1 LTS**, com `tools/optimize_quest_characters.py`.
Cada pasta contém o `.blend` editável com materiais, vistas equivalentes
`before.png` / `after.png`, `report.json` com hashes e métricas e a exportação
`Geometry.glb`. Essa exportação contém malhas e materiais marcadores, sem
duplicar as texturas. O Unity usa somente as malhas, mantendo os materiais
compartilhados dos prefabs originais, inclusive as variantes de cada time.

| Tipo | Original | Derivação | Quantidade inicial |
| --- | ---: | ---: | ---: |
| Peão | 39.424 | 11.752 | 16 |
| Torre | 36.780 | 11.010 | 4 |
| Cavalo | 44.708 | 12.255 | 4 |
| Bispo | 45.610 | 13.262 | 4 |
| Rainha | 52.452 | 13.456 | 2 |
| Rei | 50.756 | 14.186 | 2 |
| Tabuleiro completo | **1.345.592** | **389.424** | **32** |

Contagens de triângulos dos personagens, sem confundir com draw calls ou
triângulos extras desenhados por destaques. A redução é de **71,06%**.
Não há troca de personagem por distância: a derivação é usada no Android.

O modificador Decimate usa razão 0,28 no corpo; 0,50 nas mãos e no emblema;
0,16 nos símbolos sobre a base; 0,25 nos acessórios restantes. A base e
partes com menos de 200 triângulos ficam integrais. Normais autorais são
transferidas por interpolação da superfície; UVs são preservadas. Isso
mantém melhor o sombreamento do que recalcular normais na malha reduzida.
O exportador também normaliza e ortogonaliza as tangentes contra as normais
finais; três tangentes nulas em triângulos finos recebem uma base válida.
Esse saneamento muda somente o XYZ das tangentes, preservando geometria,
UVs, normais e handedness. A validação percorreu os seis GLBs finais.
A comparação ampliada ainda mostra diferenças de silhueta e de pequenos
detalhes: a aparência no headset deve ser conferida separadamente.

Os seis `.blend` foram reabertos em outro processo Blender, e os seis GLBs
exatos foram reimportados, conferindo partes, triângulos, UVs e dependências
de textura. Relatórios locais em `.local/quest3/performance-20261026/`:
`blender-final-verified.json` e `tangents-verified.json`.

Integração: cópias em `game/Assets/Resources/QuestCharacterGeometry/`, com
GUIDs próprios gerados pelo Editor. `PieceFactory` mede e monta o collider
original antes de aplicar a derivação. `QuestCharacterGeometry` troca apenas
`sharedMesh`, conserva materiais e limites dos renderers e valida todas as
partes antes de trocar qualquer uma. Exportação ausente ou incompatível
preserva o personagem original. Desktop e peças clássicas mantêm seu fluxo.

Para regenerar, escolha um destino novo ou preserve uma cópia desta pasta
antes de substituí-la: o script recusa sobrescrever uma pasta de tipo já
existente. Nunca sobrescreva os GLBs originais Direction02.
