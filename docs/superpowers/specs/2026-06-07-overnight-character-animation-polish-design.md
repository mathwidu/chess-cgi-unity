# Overnight Character Animation Polish Design

Data: 2026-06-07

## Objetivo

Levar o jogo de uma versao estavel e entregavel para uma versao mais profissional, com personagens personalizados sem base visivel, roupa clara/escura por time, sidebar mais informativa, movimentos mais caracteristicos por peca e base tecnica preparada para animacoes de captura.

Esta fase deve ser segura para rodar durante a noite. Ela nao deve gastar creditos de Unity AI, Tripo, Meshy ou outro gerador pago sem confirmacao explicita do usuario.

## Contexto atual

- Projeto real: `/Users/mathwidu/projetos/faculdade/CGI/unity/chess-cgi-unity`.
- Fallback de entrega preservado: tag `entrega-v1-estavel`.
- Cena principal: `game/Assets/Scenes/Main.unity`.
- Prefabs atuais: `game/Assets/Resources/CustomPieces/`.
- O peao `Pawn_Mathwidu_v3b` ja esta em direcao base-free, com camiseta `TeamOutfitPrimary` e tenis.
- `PieceFactory` ja suporta custom visuals, aplica `TeamOutfitApplier` e adiciona `CharacterAnimationDriver`, `ModularCharacterRig` e `CharacterVisualContract`.
- O movimento atual tem caminhada procedural, mas ainda nao e animacao humanoide real com pe plantado.

## Principios

1. Regras de xadrez nao mudam nesta fase.
2. O jogador deve sempre conseguir entender time, peca, casa e turno.
3. Nenhum asset aprovado deve ser destruido ou sobrescrito sem copia/fallback.
4. Tudo que for feito durante a noite precisa ter teste automatizado ou probe Unity MCP.
5. Creditos pagos ou geracao externa ficam bloqueados por stop rule.
6. O plano deve gerar melhorias visiveis mesmo se a trilha de rig profissional ainda nao fechar.

## Abordagens consideradas

### Abordagem A: rigar todos os personagens primeiro

Vantagem: caminho mais proximo de animacao real com pes, ataques e reacoes.

Risco: pode travar a noite inteira em Blender/rigging, especialmente em Alex e Gustavo, porque torre/cavalo podem estar fundidos com o corpo. Tambem e onde haveria maior tentacao de gastar creditos.

### Abordagem B: polimento visual e procedural primeiro

Vantagem: entrega melhorias no jogo inteiro sem custo, preserva jogabilidade, melhora aparencia e cria base para capturas. Funciona mesmo com modelos static mesh.

Risco: os pes ainda nao terao contato AAA se o modelo nao tiver rig real; a animacao melhora, mas nao vira mocap.

### Abordagem C: pipeline hibrido Codex + Blender + Unity

Vantagem: cria um caminho profissional e reaproveitavel: personagens com contrato, semantic materials, previews, importacao Unity e testes. Pode evoluir para rig real sem depender de ferramentas pagas.

Risco: exige disciplina de validacao por personagem e pode precisar de varias rodadas de Blender.

## Decisao

Seguir com uma combinacao de B + C.

Durante a noite, executar primeiro as melhorias que sao seguras e sem custo:

- inventario/audit dos seis personagens;
- contrato de roupa por time para todos;
- retirada/ocultacao de bases visiveis;
- movimentos procedurais por tipo de peca;
- sidebar premium com dados e preview inteiro;
- arquitetura de captura preparada, sem tentar finalizar as vinhetas cinematicas.

A trilha AAA/rig real fica planejada e documentada, mas so entra como trabalho separado depois dos checkpoints acima.

## Escopo da execucao noturna

### Dentro do escopo

- Melhorar assets existentes sem gastar creditos.
- Criar scripts locais de audit/preview quando necessario.
- Criar testes EditMode para contratos visuais.
- Criar movimento procedural por tipo de peca.
- Melhorar a sidebar/preview/metadata.
- Preparar contratos para captura e rig futuro.
- Rodar gates automatizados e salvar evidencias.

### Fora do escopo

- Comprar creditos ou assinatura.
- Usar Unity AI `GenerateMesh`, `RigMesh` ou `GenerateHumanoidAnimation` sem nova aprovacao.
- Fazer um rig AAA completo para todos os personagens.
- Criar combinacoes de captura entre todos os pares de pecas.
- Refatorar regras de xadrez.
- Recriar todo o jogo ou trocar de arquitetura.

## Contrato visual alvo

Todo personagem personalizado deve ter:

- `CustomVisual` como raiz visual.
- Nenhuma `TeamBase` gerada pelo runtime.
- Pelo menos um material ou renderer com nome contendo `TeamOutfit`, `TeamClothes` ou `TeamUniform`.
- Materiais de pele, cabelo, barba, oculos e acessorios sem esses nomes.
- Renderer suficiente para leitura no tabuleiro.
- `CharacterVisualContract` presente.
- `CharacterAnimationDriver` presente.
- `ModularCharacterRig` presente, mesmo que `CanAnimateWalk` seja falso nos modelos estaticos.

## Contrato de identidade por personagem

| Peca | Personagem | Identidade minima | Time |
| --- | --- | --- | --- |
| Peao | Mathwidu | ruivo, barba leve, camiseta, calca clara, tenis branco | camiseta clara/preta |
| Torre | Alex | blusao escuro com linhas verdes, sentado em torre pequena | roupa/torso claro/preto sem perder linhas |
| Cavalo | Gustavo | oculos, moletom preto, cavalo pequeno | roupa/torso claro/preto, cavalo preservado |
| Bispo | Rafael | rosto jovem, cabelo castanho, visual de bispo/colega | roupa clara/preta |
| Rainha | Marta | professora, oculos, cabelo claro, cachecol azul/branco | roupa clara/preta, cachecol preservado |
| Rei | Ricardo Carioca | professor, oculos, moletom azul Feevale | roupa clara/preta, identidade do moletom preservada |

## Movimento alvo por peca

| Peca | Movimento normal | Preparacao para captura |
| --- | --- | --- |
| Peao | grounded walk curto, com pouco salto | lunge curto/adaga futuramente |
| Torre | heavy hop em blocos, peso vertical | queda/impacto pesado |
| Cavalo | arcing L jump, como salto de obstaculo | relincho + salto no ataque |
| Bispo | ritual diagonal stride, leve elevacao | rajada/oracao/laser |
| Rainha | confident glide/walk, dominante | golpe elegante com espada/energia |
| Rei | authoritative short steps | golpe de mao aberta |

## Sidebar alvo

A sidebar deve:

- Mostrar o personagem inteiro no preview.
- Permitir rotacao e zoom.
- Mostrar nome curto, nome completo, peca, time, casa, categoria e registro.
- Usar metadados de `CharacterProfileCatalog`.
- Evitar corte de texto.
- Ficar legivel em 1920x1080 e em janela menor.

## Arquitetura de animacao

Nesta fase, a animacao visual fica em duas camadas:

1. `PieceView` decide a interpolacao raiz ate a casa final.
2. Uma biblioteca de estilos decide offset, rotacao, duracao e flavor visual por `ChessPieceKind`.

O futuro `Animator` real deve entrar por `CharacterAnimationDriver`, mas o fallback procedural continua obrigatorio.

## Stop rules

Parar e reportar se:

- Qualquer comando tentar gastar credito ou iniciar geracao paga.
- Unity ficar sem compilar apos tres tentativas de correcao.
- Um prefab aprovado precisar ser sobrescrito sem backup.
- A cena principal perder os seis prefabs customizados.
- O Test Runner/EditMode ficar bloqueado por erro de infraestrutura nao relacionado.
- Blender MCP ou Blender local ficar indisponivel e a proxima tarefa depender dele.
- A mudanca ameacar a tag de fallback `entrega-v1-estavel`.

## Criterios de aceite

- `git diff --check` passa.
- Testes Python/Blender relevantes passam.
- Probes Unity MCP passam para contratos de custom pieces.
- Console Unity termina com 0 errors.
- Todos os seis tipos de peca continuam usando `CustomVisual`.
- As pecas customizadas nao recebem `TeamBase`.
- Todo tipo de peca tem movimento diferenciado documentado/testado.
- Sidebar continua mostrando preview interativo.
- Plano/roadmap ficam atualizados para a proxima fase de captura.

