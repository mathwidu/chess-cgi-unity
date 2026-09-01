# Briefing para reuniao com professor - Xadrez CGI

## Estado verificado

- Repositorio: `https://github.com/mathwidu/chess-cgi-unity`
- Caminho local: `/Users/mathwidu/projetos/faculdade/chess-cgi-unity`
- Branch: `main`
- Commit atual: `3b86e08` (`3b86e08505ff2e9f58da990ebef4237667f3f83d`)
- Ultimo commit: `chore: remove delivery scratch wording`
- Data do ultimo commit: `2026-06-11 15:30:29 -0300`
- Estado do Git apos clone: limpo
- Tamanho local aproximado: `13M`
- Arquivos versionados: `160`
- Scripts C#: `16`, total aproximado `2477` linhas
- Prefabs customizados: `7`
- Modelos GLB customizados: `7`

Observacao: neste Mac a Unity `6000.3.16f1` nao esta instalada em `/Applications/Unity/Hub/Editor/6000.3.16f1`, entao eu nao rodei build/Play local agora. A estrutura e o README foram validados por inspecao do repo.

## Resumo de 30 segundos

O projeto e um xadrez 3D local para duas pessoas feito em Unity 6.3 LTS. O foco grafico esta no tabuleiro 3D interativo, personagens customizados para as pecas, camera dinamica por turno, iluminacao/cenario de sala de aula, HUD em Canvas e preview 3D interativo da peca selecionada. As regras completas do xadrez sao delegadas a biblioteca `ChessDotNet`, enquanto o projeto implementa a camada visual, interacao, sincronizacao entre estado logico e cena 3D, animacoes e fluxo de jogo.

## Como abrir

1. Clonar o repositorio.
2. Abrir Unity Hub.
3. Instalar/usar Unity `6000.3.16f1`.
4. Adicionar como projeto a pasta `game`, nao a raiz do repo.
5. Abrir a cena `Assets/Scenes/Main.unity`.
6. Clicar em `Play`.

Para build macOS:

1. Abrir `Assets/Scenes/Main.unity`.
2. Usar menu `Chess CGI > Build > macOS`.
3. Saida esperada: `Builds/macOS/XadrezCGI.app`.

## Funcionalidades prontas

- Partida local 2 jogadores, brancas e pretas alternando turnos.
- Movimentos legais de xadrez via `ChessDotNet`.
- Xeque, xeque-mate, empate, roque, en passant e promocao.
- Selecao por mouse via raycast.
- Destaque de casas legais.
- Movimento animado das pecas.
- Camera troca perspectiva automaticamente conforme o turno.
- Controles `Q`/`E` para orbitar camera e scroll para zoom.
- `Esc` cancela selecao e `N` reinicia.
- HUD com tela inicial, instrucoes, status, historico, painel de promocao.
- Preview 3D interativo da peca selecionada com drag e zoom.
- Pecas classicas de fallback caso algum prefab customizado falhe.
- Sala/cenario academico gerado com luzes, mesa, paredes, quadro e objetos.
- Build macOS automatizado por menu de editor.

## Arquitetura tecnica

- `ChessGameController`: orquestra partida, selecao, jogadas, estado, historico e camera.
- `ChessRulesAdapter`: encapsula `ChessDotNet` e converte entre regra logica e estado visual.
- `BoardView`: gera tabuleiro 8x8, coordenadas, frame, highlights e sincroniza pecas.
- `PieceFactory`: cria pecas; usa prefab customizado quando existe e primitivas como fallback.
- `PieceView`: guarda estado visual da peca e anima movimento com arco suave.
- `InputController`: le teclado/mouse com Unity Input System e usa raycast para escolher peca/casa.
- `CameraController`: orbita, zoom e perspectiva por turno.
- `GameHud`: cria HUD em Canvas, historico, promocao e preview 3D.
- `SelectedPiecePreviewInput`: rotacao e zoom do preview.
- `ScenePolish`: aplica iluminacao, camera e cenario de sala de aula.
- `ChessCgiBuild`: menu de build macOS.

## Pontos de computacao grafica para defender

- Modelagem/representacao 3D das pecas: GLB importados como prefabs.
- Geometria procedural: tabuleiro, highlights, bases e fallback das pecas com primitivas.
- Transformacoes: posicionamento por coordenadas, escala, rotacao por lado, movimento interpolado.
- Camera: perspectiva alternada por turno, orbita, zoom e transicao suave.
- Iluminacao: key/fill/rim lights, sombras suaves e ambiente Trilight.
- Materiais: casas claras/escuras, highlight, pecas brancas/pretas, materiais de cenario.
- RenderTexture: preview 3D da peca selecionada renderizado dentro do HUD.
- Pipeline: URP (`com.unity.render-pipelines.universal` 17.3.0).

## Pecas customizadas

| Peca | Personagem | Registro |
| --- | --- | --- |
| Peao | Matheus Duarte | Matricula 0276899 |
| Bispo | Rafael Scharer | Matricula 040603 |
| Cavalo | Gustavo Cornalewski | Matricula 0407923 |
| Torre | Alex Fenner | Matricula 0403240 |
| Rainha | Marta Rosecler Bez | Professora de Ciencias da Computacao - Universidade Feevale |
| Rei | Ricardo Ferreira de Oliveira | Professor de Ciencias da Computacao - Universidade Feevale |

Nota para explicar: as fotos de referencia nao entram no Git. O `.gitignore` exclui `game/Assets/Art/PrivateReferences/`. O repo versiona apenas prefabs e GLBs finais.

## Respostas prontas para perguntas provaveis

**Como as regras sao garantidas?**
O projeto usa `ChessDotNet.dll` para movimentos validos, turno, xeque, xeque-mate, empate e estado do tabuleiro. O codigo proprio fica na integracao visual e interativa: pega movimentos validos, destaca casas, executa jogada e sincroniza a cena 3D.

**O que voces implementaram alem da biblioteca de xadrez?**
Toda a experiencia 3D: tabuleiro, camera, input por raycast, animacao de movimento, HUD, preview 3D, pecas customizadas, fallback visual, cenario e build.

**Por que usar biblioteca para regras?**
Porque regras de xadrez sao complexas e propensas a bug. Para uma disciplina de Computacao Grafica, faz sentido concentrar esforco na visualizacao, interacao e pipeline 3D, mantendo a regra confiavel.

**Onde aparece Computacao Grafica?**
Na cena 3D, materiais, iluminacao, camera, transformacoes, modelos GLB, renderizacao em URP, RenderTexture para preview e geracao procedural de elementos do ambiente.

**O jogo tem IA?**
Nao. E local para duas pessoas. IA adversaria pode ser uma evolucao futura, mas nao faz parte do escopo atual.

**O jogo e online/multiplayer em rede?**
Nao. Multiplayer e local por alternancia de turnos no mesmo computador.

**Como trocar personagens?**
Adicionar/alterar prefabs em `Assets/Resources/CustomPieces` e configurar no `PieceFactory`.

## Ideias boas para discutir com o professor

- Adicionar coordenadas visuais no tabuleiro (`a-h`, `1-8`) para demonstrar mapeamento logico/visual.
- Criar area de pecas capturadas.
- Adicionar animacao especial de captura, xeque e promocao.
- Melhorar materiais/shaders das pecas e do tabuleiro.
- Adicionar som/feedback visual para jogada invalida.
- Salvar historico em PGN ou FEN.
- Adicionar modo contra IA simples.
- Adicionar tela de creditos/metodologia com autoria e origem dos assets.
- Gerar build macOS/Windows e registrar evidencia de execucao.
- Criar relatorio curto com arquitetura, imagens, requisitos e limitacoes.

## Limitacoes honestas

- Nao ha IA/adversario automatico.
- Nao ha rede/multiplayer online.
- Sem evidencia de build local nesta maquina agora porque a Unity exata nao esta instalada.
- As regras dependem de `ChessDotNet`; isso e uma escolha tecnica, nao implementacao propria completa do motor de xadrez.
- O HUD e construido por codigo e pode exigir ajustes finos em resolucoes muito diferentes.

## Arquivos principais para mostrar

- `README.md`
- `game/ProjectSettings/ProjectVersion.txt`
- `game/ProjectSettings/EditorBuildSettings.asset`
- `game/Assets/Scenes/Main.unity`
- `game/Assets/Scripts/Controllers/ChessGameController.cs`
- `game/Assets/Scripts/Rules/ChessRulesAdapter.cs`
- `game/Assets/Scripts/View/BoardView.cs`
- `game/Assets/Scripts/View/PieceFactory.cs`
- `game/Assets/Scripts/UI/GameHud.cs`
- `game/Assets/Scripts/View/ScenePolish.cs`
- `game/Assets/Editor/ChessCgiBuild.cs`
