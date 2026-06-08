# Sprint de creditos para personagens profissionais

Data: 2026-06-05

## Objetivo

Usar creditos do Unity AI de forma controlada para substituir os personagens atuais por modelos mais profissionais, rigaveis e preparados para animacoes reais.

O objetivo nao e apenas gerar modelos mais bonitos. O objetivo e gerar personagens que possam:

- aparecer inteiros e bem enquadrados na aba lateral;
- ficar sem base circular visivel;
- receber rig humanoide;
- andar com clips reais;
- atacar e reagir em capturas no futuro;
- manter uma estetica consistente de "xadrez universitario" no jogo.

## Regra principal

Nao gerar os seis personagens de uma vez.

O primeiro sprint usa creditos apenas para uma prova vertical:

1. gerar um novo Peao Mathwidu em pose rigavel;
2. importar no Unity;
3. confirmar escala, textura, silhueta e enquadramento;
4. testar se o modelo e bom para rig/Animator;
5. so depois repetir para os outros personagens.

## Limite de gasto recomendado

Os valores abaixo sao uma politica do projeto, nao uma garantia da Unity. O custo real deve ser conferido no painel de creditos antes e depois de cada rodada.

| Etapa | Limite recomendado | Criterio de parada |
| --- | ---: | --- |
| Prova vertical do Peao | 40 creditos | parar quando houver 1 candidato bom |
| Lote humanoide simples: Peao, Bispo, Rainha, Rei | 220 creditos | parar se 2 personagens seguidos sairem ruins |
| Props especiais: Torre e Cavalo | 160 creditos | gerar humanoide e prop separados |
| Animacoes iniciais | 120 creditos | so depois de 1 rig valido |
| Reserva para retries | 60 creditos | nao usar sem revisar resultados |

Total recomendado para este ciclo: ate 600 creditos.

Se a conta tiver 1000 creditos do trial, deixar pelo menos 350-400 creditos de margem para ajustes, bugs e animacoes futuras.

## O que cada modelo precisa entregar

Todo personagem gerado deve obedecer aos seguintes criterios:

- modelo 3D full body;
- estilo semi-realista/stylized, coerente com Unity game asset;
- proporcoes humanas naturais;
- pose neutra A-pose ou T-pose;
- olhando para frente;
- maos visiveis e separadas do corpo;
- pernas separadas;
- sem base circular, pedestal ou tabuleiro;
- sem arma integrada, exceto quando for gerado como prop separado;
- cabelo, oculos, roupa e acessorios como geometria legivel;
- textura limpa e sem borrado forte;
- malha sem partes fundidas estranhas;
- exportavel como prefab/GLB/FBX;
- pronto para receber rig humanoide.

## Fluxo manual na Unity

Como o Codex desta sessao ainda nao recebeu o tool direto `Unity_AssetGeneration_GenerateAsset`, o gasto de creditos deve ser feito manualmente na Unity por enquanto.

Passo a passo:

1. Abrir o projeto `chess-cgi-unity/game` no Unity.
2. Ir em `AI` ou `Generate New`, conforme aparecer no menu da sua versao.
3. Escolher geracao de asset 3D/modelo, se disponivel.
4. Usar uma unica referencia por rodada, preferindo foto frontal do rosto/corpo.
5. Colar o prompt do personagem.
6. Gerar no maximo 2 ou 3 variacoes.
7. Antes de aceitar/importar todas, revisar:
   - rosto reconhecivel o suficiente;
   - corpo em pose neutra;
   - sem base;
   - sem deformacao;
   - sem props grudados no corpo;
   - textura legivel no zoom.
8. Salvar apenas o melhor candidato em `Assets/Resources/CustomPieces/<Piece>_<Name>_Professional_Assets/`.
9. Registrar o gasto aproximado na tabela deste documento.

## Prompt base

Usar sempre esta estrutura e substituir os detalhes do personagem:

```text
Create a high quality stylized semi-realistic 3D game character for a Unity chess game set in a university classroom.

The character must be a full-body humanoid miniature chess character, standing upright in a clean A-pose, facing forward, with arms slightly away from the body, hands visible, legs separated, and neutral feet placement.

Do not add a chess base, pedestal, board, weapon, background, text, logo, or environment.

The mesh should be clean and suitable for humanoid rigging and animation in Unity. Keep clothing, hair, glasses, and accessories readable as geometry or clean textures. Avoid fused limbs, crossed arms, seated poses, extreme facial expression, oversized head, bulky body, or melted details.

Style: polished stylized game asset, readable from an isometric chess camera, consistent with a premium but simple university-themed chess game.

Character details:
<DETAILS>
```

## Prompts por personagem

### Peao Mathwidu

```text
Create a high quality stylized semi-realistic 3D game character for a Unity chess game set in a university classroom.

The character must be a full-body humanoid miniature chess character, standing upright in a clean A-pose, facing forward, with arms slightly away from the body, hands visible, legs separated, and neutral feet placement.

Do not add a chess base, pedestal, board, weapon, background, text, logo, or environment.

The mesh should be clean and suitable for humanoid rigging and animation in Unity. Keep clothing, hair, glasses, and accessories readable as geometry or clean textures. Avoid fused limbs, crossed arms, seated poses, extreme facial expression, oversized head, bulky body, or melted details.

Style: polished stylized game asset, readable from an isometric chess camera, consistent with a premium but simple university-themed chess game.

Character details:
young adult man, fair skin, ginger/red curly short hair, light ginger beard and mustache, friendly confident look, average athletic body, light gray t-shirt, beige cargo pants, white sneakers, subtle playful redhead identity, university student pawn character.
```

### Bispo Rafael

```text
Create a high quality stylized semi-realistic 3D game character for a Unity chess game set in a university classroom.

The character must be a full-body humanoid miniature chess character, standing upright in a clean A-pose, facing forward, with arms slightly away from the body, hands visible, legs separated, and neutral feet placement.

Do not add a chess base, pedestal, board, weapon, background, text, logo, or environment.

The mesh should be clean and suitable for humanoid rigging and animation in Unity. Avoid fused limbs, crossed arms, seated poses, extreme facial expression, oversized head, bulky body, or melted details.

Style: polished stylized game asset, readable from an isometric chess camera, consistent with a premium but simple university-themed chess game.

Character details:
young adult man named Rafael, fair skin, brown hair, expressive eyebrows, slim face, calm focused expression, dark casual hoodie or dark shirt, subtle bishop identity through elegant diagonal sash or small academic stole, no religious symbol, no weapon.
```

### Rainha Marta

```text
Create a high quality stylized semi-realistic 3D game character for a Unity chess game set in a university classroom.

The character must be a full-body humanoid miniature chess character, standing upright in a clean A-pose, facing forward, with arms slightly away from the body, hands visible, legs separated, and neutral feet placement.

Do not add a chess base, pedestal, board, weapon, background, text, logo, or environment.

The mesh should be clean and suitable for humanoid rigging and animation in Unity. Avoid fused limbs, crossed arms, seated poses, extreme facial expression, oversized head, bulky body, or melted details.

Style: polished stylized game asset, readable from an isometric chess camera, consistent with a premium but simple university-themed chess game.

Character details:
older woman professor named Marta, light skin, short white or silver hair, black glasses, warm teacher expression, white sweater, elegant blue and white patterned scarf, long patterned skirt, subtle queen identity through a small refined crown-shaped brooch or delicate crown accessory, dignified academic look.
```

### Rei Ricardo Carioca

```text
Create a high quality stylized semi-realistic 3D game character for a Unity chess game set in a university classroom.

The character must be a full-body humanoid miniature chess character, standing upright in a clean A-pose, facing forward, with arms slightly away from the body, hands visible, legs separated, and neutral feet placement.

Do not add a chess base, pedestal, board, weapon, background, text, logo, or environment.

The mesh should be clean and suitable for humanoid rigging and animation in Unity. Avoid fused limbs, crossed arms, seated poses, extreme facial expression, oversized head, bulky body, or melted details.

Style: polished stylized game asset, readable from an isometric chess camera, consistent with a premium but simple university-themed chess game.

Character details:
older male professor named Ricardo Carioca, light skin, short gray hair, glasses, calm serious teacher expression, blue university hoodie, academic but approachable look, subtle king identity through a small tasteful crown accessory or royal collar detail, no oversized fantasy armor.
```

### Torre Alex

Para a torre, nao gerar Alex sentado direto no primeiro asset rigavel. Gerar em duas partes:

1. Alex humanoide em pose neutra.
2. Torre pequena como prop separado.

Depois a pose sentada e a montagem acontecem no Unity/Blender, preservando rig e animacao.

```text
Create a high quality stylized semi-realistic 3D game character for a Unity chess game set in a university classroom.

The character must be a full-body humanoid miniature chess character, standing upright in a clean A-pose, facing forward, with arms slightly away from the body, hands visible, legs separated, and neutral feet placement.

Do not add a chess base, pedestal, board, tower, weapon, background, text, logo, or environment.

The mesh should be clean and suitable for humanoid rigging and animation in Unity. Avoid fused limbs, crossed arms, seated poses, extreme facial expression, oversized head, bulky body, or melted details.

Style: polished stylized game asset, readable from an isometric chess camera, consistent with a premium but simple university-themed chess game.

Character details:
young adult man named Alex, fair skin, short blond/light brown hair, slim build, serious neutral expression, dark navy sweater with thin green grid lines, jeans, casual university student look, intended to become the rook character later.
```

Prop separado:

```text
Create a small stylized medieval chess rook tower prop for a Unity chess game, low height, clean cylindrical stone tower shape, readable crenellations, dark neutral material, no character, no base, no board, no environment, game-ready mesh, suitable as a seat or mount prop for a miniature character.
```

### Cavalo Gustavo

Para o cavalo, tambem gerar em duas partes:

1. Gustavo humanoide em pose neutra.
2. Cavalo pequeno como prop/mount separado.

```text
Create a high quality stylized semi-realistic 3D game character for a Unity chess game set in a university classroom.

The character must be a full-body humanoid miniature chess character, standing upright in a clean A-pose, facing forward, with arms slightly away from the body, hands visible, legs separated, and neutral feet placement.

Do not add a chess base, pedestal, board, horse, weapon, background, text, logo, or environment.

The mesh should be clean and suitable for humanoid rigging and animation in Unity. Avoid fused limbs, crossed arms, seated poses, extreme facial expression, oversized head, bulky body, or melted details.

Style: polished stylized game asset, readable from an isometric chess camera, consistent with a premium but simple university-themed chess game.

Character details:
young adult man named Gustavo, fair skin, short light brown hair, black rectangular glasses, black hoodie with small orange gym-style logo, smartwatch, casual university student look, intended to become the knight rider character later.
```

Prop separado:

```text
Create a small stylized toy-like horse mount for a Unity chess knight piece, compact proportions, friendly but strong silhouette, readable horse head and legs, dark neutral material, no rider, no base, no board, no environment, game-ready mesh, suitable for a miniature character to sit on later.
```

## Criterios de aceite antes de gastar no proximo personagem

Depois de cada geracao, aprovar somente se:

- o personagem e reconhecivel pelo cabelo, roupa e silhueta;
- ele esta em pose rigavel;
- nao tem base/pedestal;
- nao esta sentado;
- as maos nao estao coladas no corpo;
- o rosto nao ficou derretido;
- a textura nao ficou borrada demais;
- o modelo nao parece pesado demais para um jogo simples;
- a importacao no Unity mostra Renderer e material corretamente;
- a aba lateral consegue enquadrar o corpo inteiro.

Se falhar em dois itens grandes, descartar e gerar outra variacao.

## Registro de gasto

Preencher durante o sprint:

| Data | Personagem | Tentativa | Custo estimado | Resultado | Decisao |
| --- | --- | ---: | ---: | --- | --- |
| 2026-06-05 | Peao Mathwidu | 1 |  |  |  |

## Proximo passo pratico

1. Confirmar no painel da Unity quantos creditos aparecem disponiveis.
2. Gerar apenas o Peao Mathwidu com o prompt acima.
3. Se sair bom, salvar/importar no projeto.
4. Rodar validacao visual no tabuleiro e sidebar.
5. Decidir se repetimos para Rafael, Marta e Ricardo.
