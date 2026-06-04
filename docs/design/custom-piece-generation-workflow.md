# Fluxo de geracao de pecas 3D personalizadas

Este fluxo existe para evitar que novas pecas personalizadas caiam no visual de prototipo com primitivas. O padrao de qualidade esperado e o mesmo dos modelos atuais do peao Mathwidu e do bispo Rafael: personagem organico, corpo completo, materiais coerentes e leitura boa dentro do tabuleiro.

## Objetivo

- Criar uma peca/personagem por tipo de peca, mantendo o xadrez jogavel e legivel.
- Gerar modelos com Unity AI/Tripo ou ferramenta equivalente, nao com primitivas manuais.
- Gastar creditos apenas depois de existir um prompt revisado e um checklist de aceite claro.
- Integrar cada modelo como prefab substituivel no `PieceFactory`.

## Estrutura de arquivos

Referencias privadas ficam somente no computador local e nao entram no git:

```text
Assets/Art/PrivateReferences/<Piece>_<Name>/
```

Prefabs aprovados entram no projeto:

```text
Assets/Resources/CustomPieces/<Piece>_<Name>.prefab
Assets/Resources/CustomPieces/<Piece>_<Name>_Assets/
```

Exemplos:

```text
Assets/Resources/CustomPieces/Pawn_Mathwidu_Redhead_v2.prefab
Assets/Resources/CustomPieces/Rook_Alex.prefab
Assets/Resources/CustomPieces/Knight_Gustavo.prefab
Assets/Resources/CustomPieces/Bishop_Rafael.prefab
Assets/Resources/CustomPieces/Queen_Marta.prefab
Assets/Resources/CustomPieces/King_Ricardo_Carioca.prefab
```

## Checklist antes de gerar

1. Salvar 2 a 5 fotos de referencia em `Assets/Art/PrivateReferences/<Piece>_<Name>/`.
2. Se alguma foto vier deitada/lateral, criar uma copia corrigida com sufixo `_upright` e usar essa copia como referencia principal.
3. Escolher quais caracteristicas precisam sobreviver no modelo.
4. Criar primeiro uma concept image frontal, corpo inteiro, com margem e fundo simples.
5. Remover o fundo da concept image e confirmar que o PNG tem alpha real.
6. Gerar o mesh 3D a partir da concept transparente, nao diretamente de fotos com ambiente.
7. Conferir se os prompts proibem fundo, celular, espelho, cadeira, cortes de corpo e estilo de primitivas.
8. Gerar poucas variacoes por rodada, avaliar, e so entao gastar mais creditos.

## Prompt base

Use este formato como base para novas pecas:

```text
Stylized high-quality 3D full-body character for a chess <piece> piece, based on the provided reference photos. The character should be a respectful likeness, not a photorealistic scan. Upright neutral standing pose, centered at origin, facing forward, feet visible, clean game-ready mesh, coherent PBR materials, readable face, expressive but subtle personality.

Key identity details: <hair>, <glasses/accessories>, <clothes>, <distinctive colors>, <body/age/personality cues>.

Chess role cue: add a subtle <piece-specific cue> that reads as <piece>, integrated into the outfit or base, not a costume.

Style target: same quality level as a polished stylized mobile/indie 3D character, organic mesh, smooth proportions, detailed clothing, no blocky primitive shapes.

Avoid: phone, mirror selfie pose, background room, chair, cropped body, oversized props, exaggerated fantasy armor, cartoon mascot style, blocky cylinders/cubes, text, logos, watermarks.
```

## Fluxo recomendado

O caminho que gerou melhor resultado para Marta foi:

1. Importar fotos privadas e corrigir orientacao.
2. Gerar uma concept image limpa com `gpt-image-1-5`.
3. Rejeitar concepts com cabeca grande, corte de corpo ou pose errada.
4. Remover o fundo da concept aprovada com `RemoveImageBackground`.
5. Confirmar localmente que o arquivo final e PNG RGBA com alpha.
6. Gerar o prefab 3D com `model3d-tripo-p1`.
7. Conectar o prefab no `PieceFactory`.
8. Validar no tabuleiro e no HUD.

Regra pratica: fotos reais ajudam na identidade, mas uma concept frontal limpa ajuda muito mais a gerar uma malha 3D boa.

## Prompt para Marta, Rainha

Referencias locais preparadas para esta rodada:

```text
Assets/Art/PrivateReferences/Queen_Marta/ref_01_face_scarf_upright.jpeg
Assets/Art/PrivateReferences/Queen_Marta/ref_02_profile_scarf_upright.jpeg
Assets/Art/PrivateReferences/Queen_Marta/ref_03_full_body_sitting_upright.jpeg
```

Concept aprovada para gerar o mesh:

```text
Assets/Art/PrivateReferences/Queen_Marta/marta_queen_concept_front_v3.png
```

```text
Stylized high-quality 3D full-body character for a chess queen piece, based on the provided reference photos. The character should be an elderly female professor named Marta, with a respectful likeness rather than a photorealistic scan. Upright neutral standing pose, centered at origin, facing forward, feet visible, clean game-ready mesh, coherent PBR materials, readable face, warm academic expression.

Key identity details: light white-blonde hair pulled back, large black rectangular glasses, cream knit sweater, blue-and-white patterned scarf with visible floral/ornamental motifs, patterned long skirt, gentle professor presence.

Chess role cue: add a subtle elegant queen cue, such as a small refined tiara, brooch, or crown-like scarf pin. It should read as queen but stay tasteful and academic.

Style target: same quality level as the current custom pawn and bishop models in the game, polished stylized indie 3D character, organic mesh, natural proportions, detailed clothing and scarf texture, not made from primitive shapes.

Avoid: phone, mirror selfie pose, seated pose, background room, chair, cropped body, oversized crown, fantasy armor, mascot/cartoon toy style, blocky cylinders/cubes, text, logos, watermarks.
```

### Prompt de concept aprovado

```text
Create a clean full-body front-view character concept image for a stylized 3D chess queen model. The entire character must fit inside the square image with generous margin: top of hair, full skirt, legs, and shoes all visible, no cropping anywhere. Use the provided photos only as identity and clothing reference. Subject: elderly female professor named Marta, light white-blonde hair pulled back, large black rectangular glasses, cream knit sweater, blue-and-white ornamental floral scarf, patterned long skirt, gentle academic expression. Pose: upright neutral standing pose, arms relaxed, feet visible, centered, facing directly forward. IMPORTANT proportions: natural adult human proportions, realistic head size, head-to-body ratio around 1:7, not chibi, not bobblehead, not toy mascot. Style: polished stylized indie game 3D character concept, organic natural proportions, readable face, detailed scarf fabric and clothing folds, suitable to convert into a 3D mesh. Add only a small tasteful queen cue such as a tiny tiara pin or brooch, not a visible costume crown. Plain pure white or transparent background, no room, no chair, no phone, no mirror, no text, no logo, no oversized crown, no blocky primitive shapes.
```

### Prompt de mesh aprovado

```text
Convert the provided transparent full-body character concept into a high-quality game-ready 3D mesh. Preserve the elderly female professor identity: light white-blonde hair pulled back, large black rectangular glasses, cream knit sweater, blue-and-white ornamental scarf, patterned long skirt, black shoes, gentle academic expression. Keep natural adult proportions, not chibi, not bobblehead. The model must stand upright in a neutral pose, centered at origin, facing forward, feet flat, complete body, coherent PBR materials, readable face, detailed scarf and skirt textures. Preserve the small queen cue tastefully, but avoid oversized crown or fantasy armor. No base, no background, no text, no phone, no chair, no primitive blocky shapes.
```

## Lote Alex, Gustavo e Ricardo

Este lote confirmou que props complexos funcionam melhor quando aparecem na concept antes do mesh.

### Alex, Torre

Referencias:

```text
Assets/Art/PrivateReferences/Rook_Alex/ref_01_front_upright.jpeg
Assets/Art/PrivateReferences/Rook_Alex/ref_02_profile_upright.jpeg
```

Concept aprovada:

```text
Assets/Art/PrivateReferences/Rook_Alex/alex_rook_concept_front_v1.png
```

Resumo do prompt aprovado: Alex sentado em uma pequena torre/castelo estilizado, com cabelo claro curto, blusao escuro com linhas verdes, jeans e torre com ameias visiveis. A torre deve parecer peca de xadrez, nao cadeira.

### Gustavo, Cavalo

Referencias:

```text
Assets/Art/PrivateReferences/Knight_Gustavo/ref_01_front_upright.jpeg
Assets/Art/PrivateReferences/Knight_Gustavo/ref_02_profile_upright.jpeg
```

Concept aprovada:

```text
Assets/Art/PrivateReferences/Knight_Gustavo/gustavo_knight_concept_front_v2.png
```

Resumo do prompt aprovado: Gustavo sentado em um cavalo pequeno de xadrez, com oculos pretos, cabelo claro cacheado, moletom preto, relogio no pulso e proporcoes adultas. A v1 ficou mais infantil/chibi, entao a v2 reforcou proporcao adulta e cavalo como peca compacta.

### Ricardo Carioca, Rei

Referencias:

```text
Assets/Art/PrivateReferences/King_Ricardo_Carioca/ref_01_front_upright.jpeg
Assets/Art/PrivateReferences/King_Ricardo_Carioca/ref_02_profile_upright.jpeg
```

Concept aprovada:

```text
Assets/Art/PrivateReferences/King_Ricardo_Carioca/ricardo_king_concept_front_v2.png
```

Resumo do prompt aprovado: professor Ricardo Carioca em pe como Rei, com cabelo grisalho, oculos, moletom azul, calca escura, coroa pequena e postura calma de professor. A v1 foi rejeitada porque trouxe artefatos de interface no topo da imagem; a v2 proibiu explicitamente paineis, screenshots, UI e caracteres.

## Checklist de aceite visual

Aceitar o modelo somente se:

- O corpo inteiro existe e esta em pose neutra.
- A peca olha para frente no tabuleiro.
- O rosto e legivel em camera de jogo.
- As marcas visuais principais aparecem: cabelo claro, oculos pretos, blusa clara, cachecol azul e branco.
- O modelo parece uma malha organica, nao uma montagem de cilindros/cubos.
- O personagem nao vem com fundo, cadeira, celular, espelho ou pose de foto.
- A silhueta funciona em tamanho pequeno no tabuleiro.
- O detalhe de Rainha e discreto e nao rouba a identidade da professora.

Rejeitar e regenerar se:

- O rosto ficar muito generico.
- O modelo vier sentado, cortado ou com pose de selfie.
- A roupa/cachecol sumirem.
- A coroa ficar grande demais.
- O resultado parecer pior que o peao e o bispo atuais.

## Integracao no Unity

1. Salvar o prefab aprovado em `Assets/Resources/CustomPieces/Queen_Marta.prefab`.
2. Abrir `Assets/Scenes/Main.unity`.
3. Selecionar `GameManager`.
4. No componente `PieceFactory`, preencher `Queen Prefab` com `Queen_Marta.prefab`.
5. Conferir a orientacao:
   - peca branca deve olhar para o lado adversario;
   - peca preta deve ser rotacionada automaticamente pelo `PieceFactory`.
6. Conferir escala no tabuleiro e no preview do HUD.
7. Atualizar `README.md` movendo Marta de planejada para escalacao atual.

## Validacao

Rodar a suite de EditMode no Test Runner ou via CLI:

```bash
"/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity" \
  -runTests \
  -batchmode \
  -projectPath "$(pwd)/game" \
  -testPlatform EditMode \
  -testResults /tmp/chess-cgi-editmode.xml \
  -logFile /tmp/chess-cgi-editmode.log
```

Validar manualmente:

1. Entrar em Play Mode.
2. Selecionar a Rainha branca em `d1`.
3. Conferir se o HUD mostra Marta e se o preview 3D enquadra bem.
4. Fazer uma jogada branca e conferir a camera virando para as pretas.
5. Selecionar uma Rainha preta quando existir no tabuleiro e conferir orientacao.

## Controle de custos

- Primeira rodada: gerar no maximo 2 ou 3 variacoes.
- Nao gastar nova rodada para problemas corrigiveis por escala, material, iluminacao ou camera.
- Regenerar apenas se silhueta, pose, identidade ou qualidade base estiverem ruins.
- Guardar o prompt final aprovado neste documento ou em um novo arquivo por personagem.
