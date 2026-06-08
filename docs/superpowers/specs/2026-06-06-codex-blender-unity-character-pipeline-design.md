# Codex + Blender + Unity Character Pipeline Design

Data: 2026-06-06

## Decisao

O projeto vai seguir uma esteira de personagens baseada em Codex Pro, Blender gratuito e Unity. O objetivo e nao depender de Unity AI, Mixamo, Meshy, Tripo ou assets pagos para criar personagens animaveis.

Essa esteira substitui a expectativa de "gerar personagens por prompt" por uma abordagem mais controlada:

- Codex cria e mantem scripts;
- Blender gera, organiza, rigga e exporta os personagens;
- Unity importa, cria prefabs, valida escala e executa o jogo;
- o jogo usa fallback seguro quando um personagem ainda nao estiver no novo padrao.

## Objetivo visual

O alvo nao e realismo AAA fotorealista. O alvo e "premium stylized":

- personagens reconheciveis por silhueta, cabelo, roupa e acessorios;
- visual consistente entre todos os colegas/professores;
- leitura clara no tabuleiro isometrico;
- modelos sem base circular visivel;
- personagens preparados para animacoes de movimento e captura;
- qualidade superior aos GLBs atuais em consistencia e controle.

## Arquitetura

### 1. Character Definition

Cada personagem tera uma definicao versionada, inicialmente em JSON ou YAML, com:

- nome real;
- peca de xadrez;
- categoria: aluno ou professor;
- dados exibidos na sidebar;
- proporcoes;
- cabelo;
- rosto simplificado;
- roupas;
- cores principais;
- acessorios;
- props especiais;
- estilo de movimento;
- estilo de captura.

Exemplo conceitual:

```json
{
  "id": "mathwidu_pawn",
  "displayName": "Mathwidu",
  "fullName": "Mathwidu",
  "piece": "Pawn",
  "category": "Aluno",
  "hair": "short curly ginger",
  "outfit": {
    "shirt": "light gray t-shirt",
    "pants": "beige cargo pants",
    "shoes": "white sneakers"
  },
  "animationProfile": "student_walk"
}
```

### 2. Blender Generator

O Blender sera executado por scripts Python criados pelo Codex. Esses scripts devem:

- criar corpo estilizado por primitivas e malhas simples;
- separar partes animaveis: corpo, cabeca, bracos, antebracos, maos, pernas, pes;
- criar cabelo e acessorios como objetos separados;
- criar materiais PBR simples;
- criar uma hierarquia previsivel;
- exportar o asset como GLB ou FBX;
- manter escala consistente para o tabuleiro.

Primeira meta: personagem modular com partes separadas.

Meta posterior: armature Blender mais completa para retarget/animacoes humanoides.

### 3. Rig Modular

O primeiro rig sera transform-based, nao skin-based.

Hierarquia esperada:

```text
CharacterRoot
  BodyRoot
    Torso
    Neck
    Head
      Hair
      Glasses optional
    LeftArmRoot
      UpperArm
      Forearm
      Hand
    RightArmRoot
      UpperArm
      Forearm
      Hand
    LeftLegRoot
      Thigh
      Shin
      Foot
    RightLegRoot
      Thigh
      Shin
      Foot
  PropRoot optional
  EffectsSocket
  HitSocket
  GroundSocket
```

Esse rig permite animacoes visiveis e controladas sem depender de skin weights perfeitos.

### 4. Unity Importer

O Unity tera ferramentas de Editor para:

- importar o GLB/FBX gerado;
- criar prefab em `Assets/Resources/CustomPieces/`;
- adicionar `CharacterVisualContract`;
- adicionar driver de animacao modular;
- configurar escala;
- configurar materiais;
- validar bounds;
- conectar no `PieceFactory`;
- gerar screenshot de validacao quando possivel.

### 5. Runtime Animation

O jogo devera escolher a animacao disponivel por prioridade:

1. Animator/rig completo, quando existir;
2. rig modular por transforms;
3. movimento procedural atual;
4. movimento instantaneo em testes.

Perfis iniciais:

- Peao: passos curtos, bracos leves;
- Bispo: caminhada diagonal com gesto de mao;
- Rainha: passo elegante;
- Rei: passo curto e pesado;
- Torre: deslocamento pesado com prop;
- Cavalo: salto em L com mount/procedural horse.

## Prova vertical

O primeiro personagem sera o Peao Mathwidu.

Escopo da prova:

- gerar personagem modular novo;
- cabelo ruivo/cacheado;
- camiseta cinza;
- calca cargo bege;
- tenis branco;
- sem base visivel;
- importar no Unity;
- trocar apenas o prefab do peao;
- animar caminhada com pernas e pes;
- garantir sidebar enquadrando corpo inteiro;
- manter fallback para todos os outros personagens.

## Testes e validacao

### Testes automatizados

- definicao do peao existe e e valida;
- gerador produz arquivo de saida esperado;
- prefab carrega via `Resources`;
- prefab tem `CharacterVisualContract`;
- prefab tem driver modular;
- bounds cabem no tabuleiro;
- movimento chama o driver modular quando disponivel;
- fallback procedural continua funcionando.

### Validacao manual

- personagem parece estar sobre o tabuleiro, nao em pedestal;
- pes se movem durante a caminhada;
- destino termina exatamente no centro da casa;
- sidebar mostra corpo inteiro;
- visual e consistente com o jogo;
- nenhuma regra de xadrez e alterada.

## Riscos

- Blender pode nao estar instalado no Mac.
- Geracao procedural pode ficar simples demais na primeira versao.
- Personagens organicos muito detalhados exigem escultura manual ou pipeline mais avancado.
- Rig modular nao substitui deformacao AAA, mas entrega controle e consistencia.
- Criar todos os seis personagens exige iteracao visual.

## Nao objetivos

Nesta fase nao faremos:

- fotorealismo;
- face rig detalhado;
- simulacao de roupa;
- cabelo fisico;
- mocap;
- compra de assets;
- assinatura de ferramenta 3D paga.

## Plano de evolucao

1. Instalar ou localizar Blender.
2. Criar estrutura `tools/blender/`.
3. Criar definicao do Peao Mathwidu.
4. Criar script gerador Blender minimo.
5. Exportar primeiro GLB.
6. Importar e criar prefab Unity.
7. Criar driver modular de caminhada.
8. Validar no jogo.
9. Repetir para Rafael, Marta, Ricardo, Alex e Gustavo.
10. Evoluir capturas especiais por tipo de peca.

## Criterio de sucesso

A arquitetura sera considerada validada quando o peao novo:

- for criado por script local;
- entrar no Unity sem ferramenta paga;
- substituir o peao atual;
- andar com pernas/pes animados;
- aparecer inteiro na sidebar;
- nao quebrar testes nem regras de xadrez.
