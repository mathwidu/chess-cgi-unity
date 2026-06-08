# Roadmap De Animacoes De Captura

Data: 2026-05-30

## Objetivo

Planejar uma evolucao futura para capturas com mais personalidade, inspirada na ideia de pecas vivas que se enfrentam, sem bloquear a entrega atual nem depender de todos os modelos personalizados prontos.

## Referencias

- `Wizard's Chess`, de Harry Potter: referencia de fantasia fisica, em que as pecas parecem ter presenca propria no tabuleiro e a captura vira um pequeno evento dramatico.
- `Battle Chess`: referencia de sistema, porque cada captura pode ser tratada como uma vinheta curta entre atacante e capturado.
- `Lewis Chessmen`: referencia visual historica para formas compactas, expressivas e legiveis mesmo em tamanho pequeno.

Links de pesquisa:

- https://harrypotter.fandom.com/wiki/Wizard%27s_Chess
- https://www.wbstudiotour.co.uk/search/chess/
- https://en.wikipedia.org/wiki/Battle_Chess
- https://en.wikipedia.org/wiki/Lewis_chessmen

## Direcao Criativa

A captura deve parecer teatral e estilizada, nao violenta demais. A leitura ideal e: a peca atacante toma iniciativa, a peca capturada reage, existe impacto visual curto, e o tabuleiro volta rapido para o fluxo normal da partida.

O jogo deve continuar sendo xadrez. As animacoes nao podem atrasar muito a jogada, confundir a casa final ou esconder informacoes importantes do HUD.

## Fases

### Pre-fase: Movimento Base-Free

Status: em andamento.

Antes de investir em capturas cinematograficas, as pecas personalizadas precisam se mover bem sem `TeamBase`. A leitura de time passa a vir do figurino branco/preto e de highlights discretos. Esta fase estabiliza:

- peao com grounded walk, menos deslizamento e menos salto vertical;
- torre preparada para heavy hop;
- cavalo preparado para arcing L jump, como um salto de obstaculo;
- bispo preparado para ritual stride;
- rainha preparada para confident walk;
- rei preparado para authoritative short steps.

Essa pre-fase e requisito para capturas melhores porque um ataque bonito em cima de uma peca que desliza ainda vai parecer artificial.

### Fase 1: Captura Generica Leve

Status: implementada na camada procedural atual.

Funciona com qualquer modelo, inclusive pecas classicas:

- atacante faz um pequeno avanco ou salto na direcao da peca capturada;
- peca capturada treme, diminui escala e desaparece;
- impacto com flash curto, poeira simples ou particulas pequenas;
- camera shake muito leve, apenas no momento da captura;
- duracao alvo: 0.35s a 0.55s.

Essa fase e a melhor primeira evolucao porque nao exige rig humanoide nem animacoes personalizadas.

### Fase 2: Captura Por Tipo De Peca

Status: implementada com estilos procedurais por tipo de peca.

Mantem o mesmo sistema, mas adiciona variacao por peca:

- peao: empurrao curto;
- torre: pancada reta e pesada;
- bispo: golpe diagonal;
- cavalo: salto;
- rainha: movimento mais elegante e dominante;
- rei: efeito contido, porque o rei quase nunca captura em cenas decisivas.

As animacoes continuam genericas, mas a identidade do xadrez aparece mais.

### Fase 3: Capturas Com Personagens Personalizados

Status: planejada para a camada de rig/clipes opcionais.

Quando os modelos definitivos estiverem escolhidos:

- usar um rig simples ou humanoide se o asset suportar;
- criar uma animacao base por personagem, nao por cada combinacao possivel;
- reaproveitar a mesma vinheta com pequenas variacoes de timing, escala e particula;
- evitar depender de expressoes faciais, porque no tamanho do tabuleiro elas aparecem pouco.

### Fase 3.1: Capture Style Contracts

Status: implementada como esqueleto tecnico.

Cada tipo de peca agora tem um contrato nomeado de captura. Esses nomes ainda nao sao clips finais; eles sao alvos estaveis para o fluxo Blender/Unity quando os personagens rigados estiverem prontos.

| Peca | Contrato | Clip futuro |
| --- | --- | --- |
| Peao | `DaggerLunge` | `Capture_Pawn_DaggerLunge` |
| Torre | `TowerCrush` | `Capture_Rook_TowerCrush` |
| Cavalo | `HorseLeap` | `Capture_Knight_HorseLeap` |
| Bispo | `PrayerBeam` | `Capture_Bishop_PrayerBeam` |
| Rainha | `RoyalSlash` | `Capture_Queen_RoyalSlash` |
| Rei | `OpenHandStrike` | `Capture_King_OpenHandStrike` |

O `PieceMotionController` continua usando captura procedural como fallback. Se um prefab futuro tiver `CharacterAnimationDriver` com Animator e clip configurado, o fluxo ja tenta tocar o clip correspondente antes de executar a vinheta procedural.

### Fase 3.2: Side-Specific Character Variants

Status: em implementacao.

Brancas e pretas nao devem depender de uma pintura generica do mesmo boneco. Cada personagem passa a poder ter dois prefabs finais: `<Piece>_<Name>_White` e `<Piece>_<Name>_Black`. O `PieceFactory` usa o prefab especifico do lado quando ele existe e usa o prefab generico apenas como fallback temporario.

Isso permite que as pretas tenham figurino escuro com personalidade propria e as brancas tenham figurino claro, mantendo rosto, cabelo, oculos, postura e props do personagem.

### Fase 3.3: Combat Sockets

Status: contrato tecnico implementado.

Cada `CustomVisual` deve expor os seguintes pontos de ancoragem:

| Socket | Uso futuro |
| --- | --- |
| `EffectsSocket` | particulas gerais e aura curta |
| `HitSocket` | ponto que recebe impacto |
| `GroundSocket` | poeira, aterrissagem e contato com o tabuleiro |
| `WeaponSocket` | arma ou prop principal |
| `RightHandSocket` | mao direita para golpes e segurar props |
| `LeftHandSocket` | mao esquerda para golpes e segurar props |
| `CastSocket` | origem de laser, oracao, magia ou slash |

O Blender pode exportar objetos vazios com esses nomes. Se eles nao existirem, a Unity cria sockets padrao para manter o contrato estavel.

O preset local `tools/blender/definitions/side_variant_combat_preset.json` concentra os nomes dos clips futuros, os sockets obrigatorios e a intencao de figurino branco/preto para que novos personagens sejam gerados com o mesmo contrato.

### Fase 4: Vinhetas Cinematicas Opcionais

So vale entrar se o restante da entrega ja estiver seguro:

- aproximar levemente a camera na captura;
- pausar input durante a vinheta;
- usar camera shake, impacto e dissolucao;
- voltar para a perspectiva do turno seguinte.

Essa fase tem maior risco porque pode quebrar o ritmo do jogo e exigir muito ajuste fino.

## Arquitetura Recomendada

Criar um `CaptureAnimationController` separado do motor de regras. O `ChessGameController` continuaria decidindo apenas que uma captura aconteceu; a camada visual receberia:

```text
attacker PieceView
captured PieceView
origin square
destination square
move result
```

O controlador visual executaria a vinheta e, no fim, chamaria a sincronizacao atual do tabuleiro.

## Camada Opcional De Rig

Os modelos atuais funcionam sem rig usando movimento procedural. Se um prefab futuro tiver `Animator`, o `CharacterAnimationDriver` pode tocar estados como `Walk`, `Attack`, `Hit` e `Idle`. O fallback procedural continua obrigatorio para qualquer modelo sem rig, porque os assets gerados por IA podem variar muito de estrutura.

## Criterios De Aceite

- Capturas continuam respeitando as regras do xadrez.
- A peca atacante termina exatamente na casa de destino.
- A peca capturada some apenas depois do impacto visual.
- O input fica bloqueado durante a animacao.
- A animacao funciona com peca classica e com peca personalizada.
- O efeito visual nao passa de 0.55s na versao padrao.

## Validacao Atual

- `CaptureResolverTests`: valida a resolucao da peca capturada antes da sincronizacao visual.
- `ImpactEffectTests`: valida o efeito curto de impacto.
- `CaptureAnimationLibraryTests`: valida estilos diferentes e duracoes seguras por tipo de peca.
- `PieceMotionControllerTests`: valida movimento instantaneo para testes e captura procedural.
- `MovementAndCaptureFlowTests` em PlayMode: valida jogada legal e captura com corrotinas reais.
