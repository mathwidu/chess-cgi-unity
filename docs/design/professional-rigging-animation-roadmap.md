# Roadmap profissional de rigging e animacao

Data: 2026-06-05

## Objetivo

Sair do estado atual, em que os personagens personalizados sao modelos 3D bonitos porem majoritariamente estaticos, para uma versao mais profissional em que:

- a aba lateral mostra o personagem inteiro, com zoom e rotacao;
- personagens personalizados nao dependem de uma base visivel para parecerem pecas;
- movimentos usam animacoes reais quando o prefab tiver rig;
- capturas podem virar pequenas vinhetas no estilo "xadrez vivo";
- o jogo continua tendo fallback seguro para modelos sem rig.

## Estado atual

O jogo ja tem uma versao estavel entregavel marcada em git como `entrega-v1-estavel`. As melhorias atuais acontecem em `feature/animated-pieces-and-sidebar`.

Os personagens atuais estao conectados como prefabs em `Assets/Resources/CustomPieces/`:

| Peca | Personagem | Estado visual | Estado de animacao |
| --- | --- | --- | --- |
| Peao | Mathwidu | Integrado | movimento procedural |
| Torre | Alex | Integrado | movimento procedural |
| Cavalo | Gustavo | Integrado | movimento procedural |
| Bispo | Rafael | Integrado | movimento procedural |
| Rainha | Marta | Integrado | movimento procedural |
| Rei | Ricardo Carioca | Integrado | movimento procedural |

O movimento procedural atual move o objeto inteiro. Ele pode fazer arco, salto, inclinacao e impacto, mas nao mexe pe, joelho, cavalo, braco ou expressao de forma natural. Para isso, precisamos de um `Animator` com rig e clips.

## Custo e ferramentas pesquisadas

Valores conferidos em 2026-06-05. Conferir novamente antes de pagar, porque planos de IA mudam com frequencia.

| Ferramenta | Uso no fluxo | Custo observado | Observacao |
| --- | --- | --- | --- |
| Blender | Limpeza de malha, separacao de props, rig manual, export FBX/GLB | Gratuito/open source | Melhor ferramenta base para nao depender so de creditos |
| Adobe Mixamo | Auto-rig humanoide e biblioteca de animacoes | Gratuito com Adobe ID | So funciona bem para humanoides bipedes em pose neutra |
| Reallusion AccuRIG | Auto-rig humanoide alternativo | Gratuito | Pode lidar melhor com alguns modelos e exportar para Unity/Blender |
| Unity AI | Agente/MCP/geracao dentro da Unity | Trial 14 dias/1000 creditos; depois US$ 10/mes por 1000 creditos | Bom para acelerar setup, mas nao deve ser o motor principal do fluxo |
| Tripo Studio | Geracao/regeneracao 3D e possivel rig/retarget via API | Free com creditos; Pro em torno de US$ 19,90/mes ou US$ 11,94/mes anual | API cita US$ 1 = 100 creditos; rig e retarget consomem creditos |
| Meshy | Geracao 3D, remesh, rigging/animation em planos | Free; Pro em torno de US$ 20/mes; Studio US$ 60/seat/mes | Free tem limites/licenca; pago da ativos privados/customer owned |
| Rokoko Studio | Captura/retarget de mocap e export FBX | Starter US$ 0; Basic em torno de US$ 10/mes anual | Bom se quisermos capturar movimentos por video/texto |
| DeepMotion Animate 3D | Video para mocap FBX/BVH/GLB | Freemium; Starter US$ 9/mes anual; Professional US$ 39/mes anual | Freemium e pessoal/nao comercial; pago serve melhor para uso final |

Links de referencia:

- Blender: https://www.blender.org/
- Mixamo FAQ: https://helpx.adobe.com/creative-cloud/faq/mixamo-faq.html
- AccuRIG: https://www.reallusion.com/auto-rig/accurig/
- Unity AI: https://unity.com/products e https://docs.unity.com/ai/credits/credits-about
- Unity Humanoid Retargeting: https://docs.unity.cn/6000.1/Documentation/Manual/Retargeting.html
- Tripo Pricing: https://www.tripo3d.ai/pricing e https://docs.tripo3d.ai/get-started/pricing.html
- Meshy Pricing: https://help.meshy.ai/en/articles/12062933-meshy-pricing-plans-free-pro-studio-enterprise
- Rokoko Pricing: https://www.rokoko.com/pricing
- DeepMotion Pricing: https://www.deepmotion.com/pricing-animate3d

## Decisao recomendada

Nao tentar rigar todos os personagens de uma vez.

O caminho mais seguro e profissional e fazer uma prova vertical com apenas uma peca primeiro:

1. Peao Mathwidu, porque e humanoide em pe e e a peca mais repetida.
2. Rig humanoide com Mixamo ou AccuRIG.
3. Importacao no Unity como Humanoid Avatar.
4. Animator com `Idle`, `Walk`, `Attack`, `Hit`, `Captured`.
5. `PieceMotionController` usa Animator quando existir e fallback procedural quando nao existir.
6. Depois de validado, replicar o fluxo para Bispo, Rainha e Rei.
7. Torre e Cavalo entram depois porque tem props especiais.

Essa abordagem evita gastar creditos em 6 personagens antes de saber se o pipeline fecha dentro do projeto.

## Codex + Blender + Unity no-cost rig route

Esta e a rota preferencial antes de qualquer gasto:

1. Construir ou limpar o personagem no Blender.
2. Manter pernas, pes, torso, cabeca, cabelo, roupa, oculos e props separaveis.
3. Posicionar o corpo em A-pose ou postura neutra animavel.
4. Criar ossos nomeados ou controles modulares para quadril, coluna, bracos, maos, pernas e pes.
5. Exportar GLB/FBX para Unity sem sobrescrever o asset aprovado.
6. Validar o prefab com `CharacterVisualContract`.
7. Animar primeiro com movimento procedural legivel.
8. Trocar gradualmente por clips de Animator quando o rig estiver confiavel.

Esse caminho usa Codex para escrever scripts, auditar hierarquias, gerar testes, operar Blender/Unity via ferramentas locais e documentar repeticao do fluxo. O objetivo e reduzir trabalho manual sem depender de creditos pagos.

## AAA reality check

AAA significa qualidade de producao comparavel a grandes estudios profissionais: escultura dedicada, retopologia, UVs, textura, materiais, rigging, animacao, iluminacao, revisao de arte e muitas iteracoes de direcao visual.

Codex pode automatizar partes desse pipeline, mas um fluxo local sem custo externo deve mirar primeiro em `premium stylized indie`: personagens bonitos, coerentes, reconheciveis, animaveis e bem integrados no jogo, sem prometer realismo de estúdio AAA em uma unica rodada.

## Contrato tecnico dos prefabs animaveis

Todo personagem profissional deve seguir este contrato:

```text
<Piece>_<Name>.prefab
  Root
    VisualRoot
      RiggedCharacterMesh
      Animator
    PropRoot optional
    EffectsSocket
    HitSocket
    GroundSocket
```

Regras:

- `Root` fica na casa do tabuleiro e termina exatamente na posicao final da jogada.
- `VisualRoot` pode animar internamente, mas nao decide regra de xadrez.
- `Animator` e opcional no curto prazo, obrigatorio nos personagens polidos.
- `PropRoot` guarda torre, cavalo, coroa, espada, adaga ou efeitos.
- collider/base fisica pode existir invisivel, mas a base circular visivel deve sair dos personagens personalizados.
- cada prefab animavel deve ter `CharacterVisualContract` com nome, tipo de rig, clips disponiveis e sockets.

## Fase 1: corrigir e validar a aba lateral

Status: implementada nesta rodada.

Objetivo: fazer o preview enquadrar qualquer modelo atual sem cortar cabeca, pernas ou props.

Tarefas atomicas:

- [x] Trocar RenderTexture do preview para proporcao quadrada.
- [x] Aumentar a area vertical da aba lateral.
- [x] Calcular bounds do clone renderizado antes de posicionar camera.
- [x] Ajustar distancia da camera a partir de FOV, aspect ratio e altura/largura do modelo.
- [x] Manter rotacao e zoom interativos.
- [x] Adicionar teste de enquadramento para personagem alto.
- [x] Adicionar teste de altura minima do preview no HUD.

Validacao:

- selecionar Marta deve mostrar corpo inteiro;
- selecionar Ricardo, Gustavo, Alex, Rafael e Mathwidu deve manter o personagem legivel;
- zoom do mouse ainda deve funcionar;
- rotacao por drag ainda deve funcionar.

## Fase 2: inventario tecnico dos modelos atuais

Objetivo: descobrir quais modelos atuais podem ser rigados e quais precisam ser regenerados.

Tarefas atomicas:

- [x] Criar `docs/design/character-rig-audit.md`.
- [x] Para cada prefab, registrar dados tecnicos disponiveis no projeto: GLB, mesh, material, skins, animations e ausencia de Animator no prefab.
- [ ] Abrir cada GLB/FBX no Blender e conferir se a malha esta limpa.
- [x] Classificar cada personagem em uma categoria inicial:
  - `Rigavel agora`: humanoide em pe, malha limpa, pose neutra.
  - `Rigavel com limpeza`: precisa separar prop/base/cabelo/oculos.
  - `Regenerar`: pose muito complexa, partes fundidas, malha ruim ou proporcao inadequada.
- [ ] Registrar screenshots de frente/lado para cada asset.

Testes:

- `CustomPieceCoverageTests` continua garantindo 32/32 pecas com visual customizado.
- Novo `CharacterRigAuditTests` garante que todos os personagens tem uma entrada no inventario.

Aceite:

- nenhum personagem segue para rig sem uma decisao documentada.

## Fase 3: prova vertical com peao rigado

Objetivo: provar que o fluxo completo funciona em uma unica peca antes de gastar com todas.

Tarefas atomicas:

- [ ] Exportar `Pawn_Mathwidu` atual para Blender.
- [x] Preparar pacote gratuito OBJ/MTL/texturas para Mixamo/AccuRIG.
- [x] Documentar o fluxo gratuito em `docs/design/pawn-rigging-free-pipeline.md`.
- [ ] Testar rig no Mixamo.
- [ ] Testar rig no AccuRIG se Mixamo falhar.
- [ ] Se ambos falharem, regenerar uma versao A-pose/T-pose do peao.
- [ ] Importar FBX rigado no Unity.
- [ ] Configurar importacao como `Humanoid`.
- [ ] Confirmar Avatar valido no Inspector.
- [ ] Baixar/importar clips `Idle`, `Walk`, `Attack`, `Hit`.
- [ ] Criar `Pawn_Mathwidu_Rigged.prefab`.
- [ ] Criar Animator Controller `CharacterHumanoid.controller`.
- [ ] Adicionar `CharacterVisualContract` ao prefab.
- [ ] Trocar somente o peao para usar a versao rigada.

Testes:

- `CharacterVisualContractTests` valida que o peao rigado tem Animator e clips minimos.
- `PieceMotionControllerTests` valida que, quando existe Animator, a jogada chama animacao de movimento.
- `PieceMotionControllerTests` valida fallback procedural quando nao existe Animator.
- `ChessGameControllerTests` garante que a regra de xadrez nao depende do tempo da animacao.

Aceite manual:

- peao anda ate a casa com pe mexendo;
- peao termina exatamente no centro da casa;
- input fica bloqueado durante a animacao;
- camera troca de turno no tempo correto;
- sidebar mostra o peao inteiro.

## Fase 4: arquitetura de animacoes reais

Objetivo: preparar o codigo para Animator sem quebrar os modelos antigos.

Tarefas atomicas:

- [x] Criar `CharacterVisualContract.cs`.
- [ ] Criar `CharacterAnimationDriver.cs`.
- [ ] Expor metodos `PlayIdle`, `PlayMove`, `PlayAttack`, `PlayHit`, `PlayCaptured`.
- [ ] Atualizar `PieceMotionController` para escolher:
  - Animator real se existir;
  - procedural se nao existir;
  - instantaneo em testes quando solicitado.
- [ ] Criar parametro de duracao maxima por animacao.
- [ ] Garantir callback de fim de movimento mesmo se clip falhar.
- [ ] Adicionar logs de warning quando um clip esperado faltar.

Testes:

- movimento com Animator chama trigger correto;
- movimento sem Animator usa fallback;
- callback e chamado exatamente uma vez;
- captura espera impacto antes de destruir visual;
- timeout evita travar partida.

## Fase 5: remocao das bases visiveis

Objetivo: tirar a base circular aparente dos personagens sem perder clique, sombra e legibilidade.

Tarefas atomicas:

- [ ] Criar collider invisivel padrao por peca.
- [ ] Mover highlight/selection para efeito no quadrado ou aro fino no chao.
- [ ] Remover ou esconder bases visiveis dos prefabs personalizados.
- [ ] Manter base nas pecas classicas, se necessario para leitura.
- [ ] Ajustar raycast para selecionar pelo collider invisivel.

Testes:

- clicar no personagem ainda seleciona a peca;
- clicar no quadrado ainda mostra movimentos legais;
- todos os personalizados ficam sem base visivel;
- capturas e movimentos ainda terminam na casa correta.

Aceite manual:

- personagens parecem estar sobre o tabuleiro, nao em pedestais;
- highlight substitui a funcao visual da base;
- sombras continuam coerentes.

## Fase 6: replicacao por personagem

Objetivo: transformar todos os personagens em assets profissionais animaveis.

Ordem recomendada:

1. Mathwidu / Peao: humanoide em pe, melhor prova.
2. Rafael / Bispo: humanoide em pe, ataque de oracao/laser depois.
3. Marta / Rainha: humanoide em pe, roupa/cachecol exigem cuidado em skin weights.
4. Ricardo Carioca / Rei: humanoide em pe, moletom e oculos.
5. Alex / Torre: separar Alex sentado da torre; torre vira prop/mount.
6. Gustavo / Cavalo: separar Gustavo do cavalo; cavalo precisa rig proprio ou animacao procedural dedicada.

Tarefas atomicas por personagem:

- [ ] Confirmar se o modelo atual e rigavel.
- [ ] Se nao for, gerar concept A-pose/T-pose limpa.
- [ ] Gerar ou corrigir mesh.
- [ ] Remover base visivel e props fundidos indevidos.
- [ ] Rigar humanoide.
- [ ] Importar no Unity como Humanoid.
- [ ] Configurar materiais e escala.
- [ ] Criar prefab `*_Rigged`.
- [ ] Conectar no `PieceFactory`.
- [ ] Validar no tabuleiro.
- [ ] Validar na sidebar.

Testes por personagem:

- prefab existe e carrega por `Resources`;
- prefab tem Renderer;
- prefab tem `CharacterVisualContract`;
- prefab tem Animator se classificado como rigado;
- tamanho normalizado cabe no tabuleiro;
- preview lateral enquadra inteiro.

## Fase 7: movimentos especiais por tipo de peca

Objetivo: cada tipo de peca se mover com personalidade.

Movimentos de deslocamento:

- Peao: caminhada curta com passos pequenos.
- Torre: torre/plataforma avanca pesada, com Alex reagindo sentado.
- Cavalo: cavalo pequeno salta em "L"; Gustavo acompanha como cavaleiro.
- Bispo: caminhada diagonal com gesto de mao.
- Rainha: passo elegante/decidido.
- Rei: passo curto, solene.

Tarefas atomicas:

- [ ] Criar `MoveAnimationStyle` por tipo de peca.
- [ ] Mapear tipo de peca para clip ou fallback procedural.
- [ ] Adicionar arco especial para cavalo.
- [ ] Adicionar peso/impacto sutil para torre.
- [ ] Ajustar duracao para nao atrasar demais a partida.

Testes:

- cada tipo retorna estilo proprio;
- movimento de cavalo preserva destino em L;
- movimento nao altera regras;
- duracao maxima por jogada fica abaixo do limite definido.

## Fase 8: capturas no estilo xadrez vivo

Objetivo: transformar capturas em pequenas vinhetas dramaticas sem travar o jogo.

Ideias por atacante:

- Peao: saca uma adaga pequena e golpeia.
- Torre: torre cai/esmaga em cima da peca capturada.
- Cavalo: cavalo relincha, salta e derruba a peca.
- Bispo: rajada de luz ou gesto de oracao que empurra a peca.
- Rainha: espada ou corte elegante quebrando a peca ao meio.
- Rei: tapa/soco de mao aberta, curto e com autoridade.

Tarefas atomicas:

- [ ] Criar `CaptureAnimationClipSet` por tipo.
- [ ] Separar animacao do atacante, reacao do capturado e efeito de impacto.
- [ ] Criar sockets `WeaponSocket`, `HitSocket`, `VfxSocket`.
- [ ] Criar VFX simples por tipo: flash, poeira, slash, luz, esmagamento.
- [ ] Adicionar SFX opcional, com volume baixo.
- [ ] Implementar skip/fallback se clip faltar.
- [ ] Limitar duracao padrao entre 0.7s e 1.4s.

Testes:

- captura chama ataque do tipo certo;
- capturado toca `Hit` ou fallback;
- visual capturado some so depois do impacto;
- atacante termina na casa capturada;
- input fica bloqueado durante a vinheta;
- timeout evita travamento.

## Fase 9: QA final e build

Objetivo: deixar uma versao apresentavel e segura para entregar.

Tarefas atomicas:

- [ ] Rodar suite EditMode completa.
- [ ] Rodar PlayMode de movimento/captura.
- [ ] Jogar roteiro manual com abertura, captura, xeque e cancelamento.
- [ ] Validar todos os personagens na sidebar.
- [ ] Fazer build macOS.
- [ ] Abrir o `.app` gerado.
- [ ] Atualizar README com controles, build e creditos dos personagens.
- [ ] Criar tag de entrega nova se a versao estiver estavel.

Comandos:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -quit \
  -projectPath game \
  -runTests \
  -testPlatform EditMode \
  -testResults TestResults/editmode-rigged-animation.xml \
  -logFile Logs/editmode-rigged-animation.log
```

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -quit \
  -projectPath game \
  -executeMethod ChessCgiBuild.BuildMacOS \
  -logFile Logs/build-macos-rigged-animation.log
```

Aceite:

- nao ha regressao de regra de xadrez;
- todos os modelos aparecem inteiros na sidebar;
- todos os personagens personalizados tem visual consistente;
- movimentos nao quebram ritmo de jogo;
- pelo menos uma peca rigada prova o fluxo profissional;
- fallback procedural continua funcionando para pecas ainda nao rigadas.

## Riscos principais

- Modelos gerados por IA podem nao ter topologia boa para rig.
- Props fundidos no corpo podem deformar durante animacao.
- Cavalo e torre nao sao humanoides, entao Mixamo nao resolve tudo.
- Animacoes longas podem deixar o xadrez cansativo.
- Custos de creditos podem crescer se regenerarmos muitas variacoes.

## Politica de gasto

Antes de gastar creditos ou assinar algo:

1. Fazer auditoria dos modelos atuais.
2. Testar ferramentas gratuitas com o peao.
3. So pagar se o bloqueio for claramente a ferramenta gratuita.
4. Comprar/assinar por um mes, nao anual, durante a fase de descoberta.
5. Medir quantos creditos um personagem consome antes de processar todos.
6. Nunca regenerar os seis personagens na mesma rodada sem aceitar um prototipo.

Depois da decisao de usar creditos, o fluxo operacional com prompts, limite de gasto e criterios de aceite ficou registrado em:

- `docs/design/unity-ai-character-credit-sprint.md`

## Proximo passo recomendado

Depois da correcao da sidebar, o proximo passo tecnico deve ser a Fase 2: criar o inventario de rig dos seis personagens. Esse inventario dira, com evidencia, se vamos:

- rigar os modelos atuais;
- limpar os modelos atuais no Blender;
- regenerar apenas alguns personagens em A-pose/T-pose;
- ou seguir com animacao procedural melhorada em pecas com props especiais.
