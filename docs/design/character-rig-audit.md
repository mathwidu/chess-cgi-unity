# Auditoria inicial de rig dos personagens

Data: 2026-06-05

## Objetivo

Registrar, antes de gastar creditos, quais personagens personalizados podem seguir para rigging, quais precisam de limpeza no Blender e quais provavelmente precisam ser regenerados em pose mais propria para animacao.

## Evidencia automatizada atual

Leitura dos arquivos `selected.glb` em `Assets/Resources/CustomPieces/*_Assets/`:

| Prefab | GLB | Nodes | Meshes | Materials | Skins | Animations |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| Pawn_Mathwidu_Redhead_v2 | Pawn_Mathwidu_Redhead_v2_Assets/selected.glb | 2 | 1 | 1 | 0 | 0 |
| Rook_Alex | Rook_Alex_Assets/selected.glb | 2 | 1 | 1 | 0 | 0 |
| Knight_Gustavo | Knight_Gustavo_Assets/selected.glb | 2 | 1 | 1 | 0 | 0 |
| Bishop_Rafael | Bishop_Rafael_Assets/selected.glb | 2 | 1 | 1 | 0 | 0 |
| Queen_Marta | Queen_Marta_Assets/selected.glb | 2 | 1 | 1 | 0 | 0 |
| King_Ricardo_Carioca | King_Ricardo_Carioca_Assets/selected.glb | 2 | 1 | 1 | 0 | 0 |

Conclusao: todos os personagens atuais sao malhas estaticas sem `skin`, sem `skeleton` e sem clips de animacao no GLB. A Unity consegue mover o objeto inteiro, mas nao consegue mover pes, joelhos, bracos, cavalo ou roupas com animacao real sem uma etapa externa de rigging/regeneracao.

## Criterios de classificacao

- `Rigavel com limpeza`: parece humanoide e pode ser testado primeiro em Blender + Mixamo/AccuRIG, mas ainda precisa de conferencia visual de malha/topologia.
- `Regenerar ou separar prop`: o modelo mistura personagem e prop/pose de forma que pode prejudicar auto-rig. Deve ir para uma versao nova em pose neutra ou para separacao manual de partes.
- `Regenerar`: modelo nao e adequado para rig direto sem retrabalho alto.

## Pawn_Mathwidu_Redhead_v2

- Peca: Peao.
- Personagem: Mathwidu.
- Arquivo: `Assets/Resources/CustomPieces/Pawn_Mathwidu_Redhead_v2.prefab`.
- GLB fonte: `Assets/Resources/CustomPieces/Pawn_Mathwidu_Redhead_v2_Assets/selected.glb`.
- Evidencia tecnica: 1 mesh, 1 material, 0 skins, 0 animations, sem Animator no prefab.
- Leitura visual atual: humanoide em pe, melhor candidato para prova vertical.
- Classificacao inicial: Rigavel com limpeza.
- Decisao antes de gastar creditos: testar no Mixamo e no AccuRIG primeiro; se ambos falharem, regenerar Mathwidu em A-pose/T-pose com corpo inteiro e sem base.
- Pacote gratuito preparado: `RiggingExports/Pawn_Mathwidu_Redhead_v2/MixamoInput/Pawn_Mathwidu_Redhead_v2_mixamo_input.zip`.
- Fluxo detalhado: `docs/design/pawn-rigging-free-pipeline.md`.
- Decisao v3: preserve current visual and attempt rig cleanup. Os previews `preview_front.png`, `preview_three_quarter.png` e `preview_board_scale.png` confirmaram que o visual antigo ainda e o baseline aprovado, enquanto o `MathwiduPawnV2` procedural fica rejeitado como experimento.
- Validacao pendente: abrir no Blender, conferir orientacao frontal, malha dos bracos/pernas, separacao de sapatos/cabelo e deformacao basica.

## Rook_Alex

- Peca: Torre.
- Personagem: Alex.
- Arquivo: `Assets/Resources/CustomPieces/Rook_Alex.prefab`.
- GLB fonte: `Assets/Resources/CustomPieces/Rook_Alex_Assets/selected.glb`.
- Evidencia tecnica: 1 mesh, 1 material, 0 skins, 0 animations, sem Animator no prefab.
- Leitura visual atual: personagem sentado em torre pequena; prop e corpo provavelmente estao fundidos em uma unica malha.
- Classificacao inicial: Regenerar ou separar prop.
- Decisao antes de gastar creditos: nao rigar como primeira prova. Depois do peao, avaliar se vale separar Alex da torre no Blender ou regenerar Alex sentado com torre como prop separado.
- Validacao pendente: abrir no Blender e confirmar se a torre pode virar `PropRoot` sem quebrar o corpo.

## Knight_Gustavo

- Peca: Cavalo.
- Personagem: Gustavo.
- Arquivo: `Assets/Resources/CustomPieces/Knight_Gustavo.prefab`.
- GLB fonte: `Assets/Resources/CustomPieces/Knight_Gustavo_Assets/selected.glb`.
- Evidencia tecnica: 1 mesh, 1 material, 0 skins, 0 animations, sem Animator no prefab.
- Leitura visual atual: personagem sentado em cavalo pequeno; cavalo e personagem precisam de comportamentos diferentes.
- Classificacao inicial: Regenerar ou separar prop.
- Decisao antes de gastar creditos: nao rigar como humanoide simples. O caminho profissional e separar Gustavo do cavalo ou regenerar com cavalo como prop/mount controlado por animacao propria.
- Validacao pendente: abrir no Blender e avaliar se cavalo, pernas e torso estao fundidos demais para skin weights aceitaveis.

## Bishop_Rafael

- Peca: Bispo.
- Personagem: Rafael.
- Arquivo: `Assets/Resources/CustomPieces/Bishop_Rafael.prefab`.
- GLB fonte: `Assets/Resources/CustomPieces/Bishop_Rafael_Assets/selected.glb`.
- Evidencia tecnica: 1 mesh, 1 material, 0 skins, 0 animations, sem Animator no prefab.
- Leitura visual atual: humanoide em pe, com boa chance de auto-rig depois do peao.
- Classificacao inicial: Rigavel com limpeza.
- Decisao antes de gastar creditos: testar apenas depois do fluxo do peao estar aprovado; se deformar mal, regenerar em pose neutra com bracos afastados do corpo.
- Validacao pendente: abrir no Blender e conferir bracos, casaco e possiveis partes coladas ao corpo.

## Queen_Marta

- Peca: Rainha.
- Personagem: Marta.
- Arquivo: `Assets/Resources/CustomPieces/Queen_Marta.prefab`.
- GLB fonte: `Assets/Resources/CustomPieces/Queen_Marta_Assets/selected.glb`.
- Evidencia tecnica: 1 mesh, 1 material, 0 skins, 0 animations, sem Animator no prefab.
- Leitura visual atual: humanoide em pe, mas saia longa e cachecol podem deformar mal em rig humanoide automatico.
- Classificacao inicial: Rigavel com limpeza.
- Decisao antes de gastar creditos: testar depois do peao e do bispo. Se a saia/cachecol deformarem mal, manter Marta com animacao mais sutil ou regenerar com roupa mais amigavel para rig.
- Validacao pendente: abrir no Blender e verificar se saia, cachecol e oculos conseguem se manter estaveis durante caminhada curta.

## King_Ricardo_Carioca

- Peca: Rei.
- Personagem: Ricardo Carioca.
- Arquivo: `Assets/Resources/CustomPieces/King_Ricardo_Carioca.prefab`.
- GLB fonte: `Assets/Resources/CustomPieces/King_Ricardo_Carioca_Assets/selected.glb`.
- Evidencia tecnica: 1 mesh, 1 material, 0 skins, 0 animations, sem Animator no prefab.
- Leitura visual atual: humanoide em pe, provavel bom candidato depois de Mathwidu/Rafael.
- Classificacao inicial: Rigavel com limpeza.
- Decisao antes de gastar creditos: testar auto-rig gratuito antes de regenerar; preservar moletom azul e coroa discreta.
- Validacao pendente: abrir no Blender e conferir oculos, coroa, bracos e proporcao do corpo.

## Ordem recomendada de prova

1. `Pawn_Mathwidu_Redhead_v2`: prova vertical de humanoide em pe.
2. `Bishop_Rafael`: confirma se o fluxo funciona em outro humanoide.
3. `King_Ricardo_Carioca`: professor humanoide em pe, risco medio.
4. `Queen_Marta`: testar com cuidado por causa de saia/cachecol.
5. `Rook_Alex`: tratar torre como prop separado ou regenerar.
6. `Knight_Gustavo`: tratar cavalo como prop/rig separado ou regenerar.

## Proximo passo tecnico

Criar a prova vertical do peao rigado:

1. Exportar/abrir `Pawn_Mathwidu_Redhead_v2_Assets/selected.glb` no Blender.
2. Conferir se o modelo esta de frente, completo e sem base visivel acoplada.
3. Testar upload/auto-rig no Mixamo.
4. Se Mixamo falhar, testar AccuRIG.
5. Se ambos falharem, regenerar o peao em A-pose/T-pose antes de gastar com os outros.

## Overnight Audit 2026-06-07

Credit spend policy: blocked without explicit user confirmation.

The automated audit lives in `tools/character_pipeline/audit_custom_pieces.py`.
It checks the six active custom prefabs for prefab presence, selected GLB presence,
`TeamBase` tokens, `TeamOutfit` semantic tokens, and `Animator` tokens.

The active runtime contract test lives in
`game/Assets/Tests/EditMode/CustomPieceVisualContractTests.cs`. It verifies that
all six active custom prefabs exist, have renderers, are instantiated through
`PieceFactory` without runtime `TeamBase`, and receive the required visual
extension components. The stricter semantic outfit check is currently locked on
`Pawn_Mathwidu_v3b`, because it is the first character already rebuilt with a
real `TeamOutfitPrimary` surface. The remaining characters should receive
non-destructive semantic clothing overlays before the next full-team outfit gate.
