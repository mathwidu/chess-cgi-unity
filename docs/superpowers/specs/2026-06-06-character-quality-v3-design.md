# Character Quality V3 Design

Data: 2026-06-06

## Decisao

A tentativa `MathwiduPawnV2` validou uma coisa tecnica importante: Codex consegue gerar um GLB local com hierarquia animavel. Mas a tentativa falhou no criterio principal do projeto: qualidade visual e semelhanca com a pessoa.

A nova decisao e inverter a ordem da pipeline:

1. Aprovar primeiro um personagem bonito no Blender.
2. So depois preparar rig, animacao e importacao no Unity.
3. Nunca substituir uma peca jogavel por um modelo que ainda nao venceu o gate visual.

O gerador procedural baseado em primitivas simples fica rebaixado para experimento tecnico. Ele nao e mais o caminho principal para personagens finais.

## Objetivo

Criar personagens 3D estilizados, reconheciveis e preparados para animacao, sem cair no visual de prototipo. O alvo e um personagem cartoon premium de jogo, com leitura boa no tabuleiro, sem necessidade de realismo fotografico.

Para o peao Mathwidu, o personagem aprovado precisa preservar:

- cabelo ruivo/cacheado curto;
- barba e bigode ruivos claros;
- pele clara;
- corpo adulto com proporcao natural estilizada;
- camiseta clara/cinza;
- calca bege/cargo;
- tenis branco;
- personalidade casual/confiante;
- corpo completo, pes visiveis, maos visiveis;
- pose neutra ou A-pose amigavel para rig.

## Principios

### Qualidade antes de rig

Um personagem ruim com rig nao serve. Um personagem bom sem rig ainda pode ser usado como fallback entregavel.

### Preview antes de Unity

Nenhum personagem v3 entra no `PieceFactory` antes de:

- render frontal aprovado;
- render 3/4 aprovado;
- render isometrico de tabuleiro aprovado;
- checklist de semelhanca aprovado;
- checklist de proporcao aprovado.

### Blender como bancada visual

O Blender sera usado para inspecionar e polir o modelo antes do Unity:

- escala;
- orientacao;
- silhueta;
- materiais;
- cabelo;
- roupa;
- separacao de props;
- possibilidade de rig.

### Unity como integracao final

O Unity nao sera usado para decidir se o personagem e bonito. O Unity valida:

- escala no tabuleiro;
- prefab;
- sidebar;
- materiais em runtime;
- contrato de animacao;
- fallback quando nao houver rig.

## Arquitetura V3

### 1. Candidate Asset Folder

Cada tentativa visual fica em uma pasta de candidatos:

```text
game/Assets/Art/CharacterCandidates/<Piece>_<Name>/<version>/
```

Exemplo:

```text
game/Assets/Art/CharacterCandidates/Pawn_Mathwidu/v3a/
```

Essa pasta pode conter:

- arquivo Blender `.blend`;
- GLB/FBX candidato;
- screenshots de preview;
- notas de revisao;
- manifesto de qualidade.

### 2. Quality Manifest

Cada candidato tera um manifesto:

```text
character_quality_manifest.json
```

Campos minimos:

- `candidateId`;
- `personName`;
- `pieceKind`;
- `visualStatus`;
- `rigStatus`;
- `approvedForUnity`;
- `identityChecklist`;
- `technicalChecklist`;
- `previewImages`.

### 3. Blender Review Script

Um script local gera previews consistentes:

```text
tools/blender/render_character_review.py
```

Saidas obrigatorias:

```text
preview_front.png
preview_three_quarter.png
preview_board_scale.png
```

### 4. Unity Import Gate

O Unity so importa candidatos com:

```json
"approvedForUnity": true
```

Se `approvedForUnity` for falso, o importador deve falhar com mensagem clara.

### 5. Rig Strategy

O rig vem depois do aceite visual:

1. Tentar rig humanoide quando o corpo for bipedal e limpo.
2. Separar props como `PropRoot`.
3. Usar `CharacterVisualContract` para declarar `StaticMesh`, `RigCandidate`, `RiggedHumanoid` ou `RiggedProp`.
4. Manter fallback procedural ate uma peca ter clips reais aprovados.

## Candidato Inicial

O primeiro candidato v3 sera:

```text
Pawn_Mathwidu/v3a
```

Ele deve ser produzido com foco visual, nao com primitivas. Existem tres caminhos aceitaveis:

1. Reaproveitar o `Pawn_Mathwidu_Redhead_v2` atual e preparar para rig;
2. Criar concept frontal limpa e gerar um novo mesh organico;
3. Modelar uma base cartoon em Blender com malhas mais organicas antes do rig.

Recomendacao: comecar pelo caminho 1 para preservar a qualidade que ja agradou, enquanto medimos se ele pode virar rig. Se o modelo atual nao for rigavel com qualidade, seguir para o caminho 2.

## Criterios de aceite

### Visual

- O personagem parece melhor ou igual ao `Pawn_Mathwidu_Redhead_v2` atual.
- O personagem lembra o Mathwidu pelas pistas visuais principais.
- O rosto e legivel em preview frontal.
- A silhueta funciona de longe no tabuleiro.
- Nao parece feito de cilindros/cubos/esferas.

### Tecnico

- O arquivo abre no Blender sem erros.
- A escala e previsivel.
- O personagem olha para frente.
- Pes e maos existem.
- Se houver base, ela e removivel ou separada.
- Props nao ficam fundidos ao corpo quando isso atrapalhar rig.

### Unity

- O prefab nao substitui a versao estavel sem aprovacao.
- A sidebar mostra o corpo inteiro.
- O `PieceFactory` so aponta para candidato aprovado.
- O fallback entregavel continua disponivel.

## Nao objetivos

Nesta fase nao faremos:

- todos os seis personagens v3;
- captura estilo xadrez bruxo;
- rig facial;
- simulacao de roupa;
- compra de assets;
- dependencia obrigatoria de ferramenta paga.

## Resultado esperado

Ao final da fase v3a, teremos uma resposta clara:

1. O peao atual pode ser polido/rigado sem perder qualidade;
2. ou precisamos regenerar o peao com concept limpa;
3. ou precisamos aceitar um caminho hibrido: modelo bonito estatico agora, rig/animacao depois.
