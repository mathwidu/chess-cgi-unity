# Xadrez 3D CGI - Especificacao de Design

Data: 2026-05-29

## Contexto

O trabalho de Computacao Grafica I exige uma aplicacao 2D ou 3D feita em Processing ou Unity, com cena grafica funcional, multiplos objetos, organizacao espacial, transformacoes geometricas dinamicas, animacao, interatividade, codigo-fonte, instrucoes ou executavel, relatorio tecnico-critico de 2 a 4 paginas e video demonstrativo de ate 3 minutos.

O projeto sera desenvolvido em Unity como um jogo de xadrez 3D local para dois jogadores, priorizando regras completas e apresentacao grafica clara. WebGL, IA e pecas personalizadas ficam como extensoes posteriores, sem bloquear o MVP.

## Visao Do Jogo

O jogo sera um xadrez 3D local em que dois jogadores alternam turnos no mesmo computador. O jogador seleciona uma peca com o mouse, visualiza as casas legais destacadas e escolhe uma casa de destino. O sistema aplica as regras oficiais do xadrez por meio de uma biblioteca C#, anima a movimentacao ou captura e atualiza o estado da partida.

O jogo mostrara turno atual, xeque, xeque-mate, empate e promocao. A cena tera tabuleiro 8x8, 32 pecas classicas, camera, luzes, materiais, destaques visuais e uma interface minima.

## Objetivos De CGI

- Demonstrar multiplos objetos graficos: casas, pecas, luzes, camera, destaques e UI.
- Demonstrar organizacao espacial: mapeamento entre casas de xadrez e coordenadas 3D.
- Demonstrar transformacoes geometricas: translacao animada das pecas, rotacao de camera/pecas e escala para selecao/destaque.
- Demonstrar animacao: movimento de pecas, captura, feedback de selecao e camera.
- Demonstrar interatividade: selecao e movimento por mouse, controles de camera por teclado/scroll e reinicio de partida.
- Demonstrar modelagem: pecas classicas feitas com primitivas, meshes simples ou prefabs proprios.
- Explicar conceitos tecnicos no relatorio: GameObjects, componentes, Transform, raycast, interpolacao por tempo, colisao/selecionabilidade, camera, iluminacao e separacao entre estado logico e visual.

## Escopo Do MVP

- Projeto Unity na pasta `chess-cgi-unity`.
- Jogo local completo para dois jogadores.
- Regras completas via biblioteca/motor de xadrez em C#.
- Tabuleiro 3D classico, apresentavel e legivel.
- Pecas classicas simples e distinguiveis.
- Selecao por mouse e destaque de movimentos legais.
- Movimento animado, captura, promocao, xeque, xeque-mate e empate.
- Camera com rotacao, zoom e reset.
- HUD minimo para turno, mensagens e promocao.
- README com versao da Unity, como abrir, como executar e controles.
- Relatorio tecnico-critico em PDF.
- Video demonstrativo de ate 3 minutos.

## Fora Do MVP

- Build WebGL jogavel no navegador.
- Build Windows e macOS.
- IA adversaria.
- Pecas personalizadas como colegas ou professor.
- Multiplayer online entre computadores diferentes.

Esses itens podem ser adicionados depois que o MVP, o relatorio e o video estiverem seguros.

## Arquitetura

### 1. Motor de regras

Camada responsavel apenas pelo estado logico do xadrez: posicao inicial, movimentos legais, turnos, capturas, xeque, xeque-mate, empate, promocao, roque e en passant. Essa camada nao conhece GameObjects, camera, materiais ou animacoes.

### 2. Estado visual do tabuleiro

Camada responsavel por mapear casas logicas para posicoes 3D da Unity. Ela cria ou organiza casas, pecas e destaques visuais, recebendo o estado logico do motor e sincronizando a cena.

### 3. Interacao e fluxo de jogo

Camada responsavel por raycast do mouse, selecao de pecas, destaque de movimentos validos, execucao de jogadas, bloqueio de input durante animacoes, promocao, mensagens e fim de jogo.

### 4. Apresentacao 3D e UI

Camada responsavel por camera, luz, materiais, animacoes e HUD. A camera deve permitir orbitar ou alternar a visao do tabuleiro, sem interferir na regra do jogo.

## Componentes Principais

- `ChessGameController`: orquestra partida, estado, selecao, jogadas, fim de jogo e mensagens.
- `BoardView`: cria o tabuleiro 8x8, converte casas logicas para posicoes 3D e gerencia destaques.
- `PieceView`: representa uma peca na cena e executa animacoes de movimento, captura, selecao e escala.
- `PieceFactory`: cria pecas classicas a partir de prefabs, primitivas ou meshes simples.
- `InputController`: le mouse/teclado, faz raycast e envia intencoes ao controlador de jogo.
- `CameraController`: controla rotacao, zoom, reset e alternancia de perspectiva.
- `GameHud`: mostra turno, xeque, fim de jogo e opcoes de promocao.
- `ChessRulesAdapter`: encapsula a biblioteca de regras para evitar acoplamento entre Unity e a API concreta da biblioteca.

## Organizacao Da Cena Unity

- `GameManager`: objeto vazio com controllers principais.
- `Board`: raiz do tabuleiro e casas.
- `Pieces`: raiz das pecas.
- `Highlights`: raiz dos marcadores de casas.
- `Main Camera`: camera em perspectiva.
- `Directional Light`: luz principal da cena.
- `Canvas`: UI minima.

## Fluxo De Jogo

1. A partida inicia na posicao padrao do xadrez.
2. O `BoardView` monta o tabuleiro e o `PieceFactory` cria as 32 pecas.
3. O HUD mostra o turno das brancas.
4. O jogador clica em uma peca propria.
5. A peca selecionada recebe destaque visual.
6. O jogo consulta o motor de regras e destaca apenas casas de destino legais.
7. O jogador clica em uma casa destacada.
8. O input fica bloqueado enquanto a peca se move com animacao de translacao.
9. Se houver captura, a peca capturada recebe feedback curto e e removida ou ocultada.
10. O estado logico e atualizado no motor de regras.
11. A cena sincroniza com o estado logico.
12. O turno alterna para o outro jogador.
13. Se houver xeque, o HUD e o rei recebem destaque.
14. Se houver promocao, o HUD abre escolha de rainha, torre, bispo ou cavalo.
15. Se houver xeque-mate ou empate, o HUD mostra fim de jogo e permite reiniciar.

## Controles

- Mouse: selecionar peca e casa de destino.
- `Q` / `E`: girar camera ao redor do tabuleiro.
- Scroll: aproximar ou afastar camera.
- `R`: resetar camera.
- `N`: iniciar nova partida.
- `Esc`: cancelar selecao.

## Marcos De Entrega

1. Ambiente e esqueleto: Unity Hub, Unity 6 LTS, Git, `.gitignore`, README inicial e estrutura de pastas.
2. Tabuleiro e pecas: cena 3D com tabuleiro, materiais, camera, luz e pecas posicionadas.
3. Motor de regras: biblioteca C#, `ChessRulesAdapter`, posicao inicial e consulta de movimentos legais.
4. Interacao jogavel: selecao, destaques, movimento, captura e alternancia de turnos.
5. Regras especiais e estados: roque, en passant, promocao, xeque, xeque-mate e empate.
6. Polimento CGI: animacoes, camera, feedback visual, materiais e apresentacao.
7. Entregaveis: README, relatorio PDF, video demonstrativo e builds opcionais.

## Priorizacao

Nao pode falhar: jogo local completo, regras, interacao, relatorio e video.

Extras: WebGL, IA, pecas personalizadas, builds multiplataforma e multiplayer online.

## Criterios De Aceite

- O projeto abre na versao documentada da Unity.
- O tabuleiro 3D e as pecas aparecem corretamente na cena.
- Dois jogadores conseguem concluir uma partida local alternando turnos.
- O jogo impede movimentos ilegais.
- O jogo trata captura, promocao, roque, en passant, xeque, xeque-mate e empate.
- Ha animacao perceptivel de movimento e feedback visual de selecao.
- Ha controles de camera e reinicio.
- O README explica como executar.
- O relatorio conecta a implementacao aos conceitos de Computacao Grafica.
- O video demonstra os requisitos principais em ate 3 minutos.
