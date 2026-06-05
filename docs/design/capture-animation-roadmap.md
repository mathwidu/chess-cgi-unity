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

## Criterios De Aceite

- Capturas continuam respeitando as regras do xadrez.
- A peca atacante termina exatamente na casa de destino.
- A peca capturada some apenas depois do impacto visual.
- O input fica bloqueado durante a animacao.
- A animacao funciona com peca classica e com peca personalizada.
- O efeito visual nao passa de 0.55s na versao padrao.
