# Prova vertical gratuita de rig: peao Mathwidu

Data: 2026-06-05

## Objetivo

Transformar primeiro o peao Mathwidu em um personagem animavel, usando ferramentas gratuitas antes de gastar creditos. Esta prova vertical deve validar o fluxo completo antes de repetir para os outros personagens.

## Estado do asset atual

Fonte atual:

```text
game/Assets/Resources/CustomPieces/Pawn_Mathwidu_Redhead_v2_Assets/selected.glb
```

Evidencia tecnica do GLB:

- vertices: 7584
- indices: 14595
- meshes: 1
- materials: 1
- skins: 0
- animations: 0

Conclusao: o modelo atual e uma malha estatica. Para ter pes andando de verdade, precisamos passar por auto-rig externo ou regenerar uma versao A-pose/T-pose.

## Pacote gratuito gerado

O pacote local para upload em ferramentas gratuitas foi gerado em:

```text
RiggingExports/Pawn_Mathwidu_Redhead_v2/MixamoInput/Pawn_Mathwidu_Redhead_v2_mixamo_input.zip
```

Conteudo do ZIP:

```text
Pawn_Mathwidu_Redhead_v2.obj
Pawn_Mathwidu_Redhead_v2.mtl
Color_a2fab25c-4f7a-4c46-97de-bfe11ecd7664.jpg
ORM_a2fab25c-4f7a-4c46-97de-bfe11ecd7664.jpg
NormalGL_a2fab25c-4f7a-4c46-97de-bfe11ecd7664.jpg
```

O export e gerado pelo script:

```bash
python3 tools/rigging/export_glb_to_mixamo_obj.py \
  game/Assets/Resources/CustomPieces/Pawn_Mathwidu_Redhead_v2_Assets/selected.glb \
  RiggingExports/Pawn_Mathwidu_Redhead_v2/MixamoInput \
  --name Pawn_Mathwidu_Redhead_v2
```

## Tentativa 1: Mixamo gratuito

1. Abrir https://www.mixamo.com/.
2. Entrar com uma conta Adobe.
3. Clicar em `Upload Character`.
4. Fazer upload deste arquivo:

```text
/Users/mathwidu/projetos/faculdade/CGI/unity/chess-cgi-unity/RiggingExports/Pawn_Mathwidu_Redhead_v2/MixamoInput/Pawn_Mathwidu_Redhead_v2_mixamo_input.zip
```

5. Se o Mixamo aceitar o modelo, posicionar os marcadores:
   - chin: queixo;
   - wrists: pulsos;
   - elbows: cotovelos;
   - knees: joelhos;
   - groin: centro do quadril.
6. Escolher skeleton LOD padrao/standard.
7. Conferir a pre-visualizacao do rig.
8. Baixar primeiro um FBX parado:
   - Format: `FBX for Unity`;
   - Skin: `With Skin`;
   - Frames per Second: `30`;
   - Keyframe Reduction: `None`.
9. Salvar como:

```text
game/Assets/Resources/CustomPieces/Pawn_Mathwidu_Rigged_Assets/Pawn_Mathwidu_Rigged.fbx
```

## Animacoes gratuitas para baixar depois do rig

Depois que o personagem rigado estiver aceito:

1. `Idle`: animacao parada.
2. `Walking`: caminhada curta para movimento entre casas.
3. `Stab` ou `Punch`: placeholder do ataque do peao.
4. `Hit Reaction`: placeholder para ser atingido.

Baixar cada uma como `FBX for Unity`. Para animacoes que usam o mesmo personagem, pode usar `Without Skin` depois que o FBX principal com skin ja estiver no projeto.

Sugestao de nomes:

```text
game/Assets/Resources/CustomPieces/Pawn_Mathwidu_Rigged_Assets/Pawn_Mathwidu_Idle.fbx
game/Assets/Resources/CustomPieces/Pawn_Mathwidu_Rigged_Assets/Pawn_Mathwidu_Walk.fbx
game/Assets/Resources/CustomPieces/Pawn_Mathwidu_Rigged_Assets/Pawn_Mathwidu_Attack.fbx
game/Assets/Resources/CustomPieces/Pawn_Mathwidu_Rigged_Assets/Pawn_Mathwidu_Hit.fbx
```

## Tentativa 2: AccuRIG gratuito

Se o Mixamo recusar o ZIP ou gerar um rig ruim:

1. Instalar/abrir AccuRIG.
2. Importar `Pawn_Mathwidu_Redhead_v2.obj` da pasta `RiggingExports/Pawn_Mathwidu_Redhead_v2/MixamoInput/`.
3. Usar auto-rig humanoide.
4. Exportar FBX para Unity.
5. Salvar no mesmo destino do FBX principal:

```text
game/Assets/Resources/CustomPieces/Pawn_Mathwidu_Rigged_Assets/Pawn_Mathwidu_Rigged.fbx
```

## Tentativa 3: gastar o minimo possivel

So entrar aqui se Mixamo e AccuRIG falharem.

Regra de gasto minimo:

1. Regenerar apenas o peao Mathwidu.
2. Nao gerar os seis personagens.
3. Pedir explicitamente A-pose/T-pose, corpo inteiro, frente limpa e sem base.
4. Gerar uma unica concept se necessario.
5. Gerar uma unica malha 3D a partir da concept aprovada.
6. Testar rig gratuito novamente antes de gastar mais.

Prompt base para regeneracao economica:

```text
Stylized full-body 3D character of Mathwidu as a chess pawn, adult proportions, red curly hair, light skin, short beard, casual light shirt and beige pants, clean A-pose with arms slightly away from body, feet visible, facing forward, centered at origin, game-ready mesh, no base, no phone, no mirror, no background, no oversized props, suitable for humanoid auto-rigging in Mixamo.
```

## Integracao no Unity depois do FBX

Quando o FBX rigado estiver em `Assets`:

1. Selecionar `Pawn_Mathwidu_Rigged.fbx`.
2. Aba `Rig`:
   - Animation Type: `Humanoid`;
   - Avatar Definition: `Create From This Model`;
   - Apply.
3. Confirmar que o Avatar fica valido.
4. Criar prefab:

```text
game/Assets/Resources/CustomPieces/Pawn_Mathwidu_Rigged.prefab
```

5. O prefab precisa ter:
   - `Animator`;
   - `CharacterAnimationDriver`;
   - `CharacterVisualContract`;
   - sockets `EffectsSocket`, `HitSocket`, `GroundSocket`;
   - nenhuma base circular visivel.
6. Trocar somente o peao no `PieceFactory` para validar a prova vertical.

## Criterio de aceite

- o peao fica inteiro no tabuleiro;
- o peao fica inteiro na sidebar;
- o FBX tem Avatar humanoide valido;
- caminhada move pes/pernas, nao so o objeto inteiro;
- ao terminar a jogada, o root fica exatamente no centro da casa;
- se o Animator falhar, o jogo ainda consegue usar fallback procedural.
