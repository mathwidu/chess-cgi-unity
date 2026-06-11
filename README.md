# Xadrez CGI

Projeto de Computacao Grafica I desenvolvido em Unity 6.3 LTS. O jogo e um xadrez 3D local para duas pessoas, com regras completas, tabuleiro interativo, camera por turno, HUD em Canvas e personagens personalizados da turma.

## Estado da entrega

- Branch principal de entrega: `main`.
- Branch estavel preservada: `stable/entrega-v1-estavel`.
- Cena principal: `game/Assets/Scenes/Main.unity`.
- Versao da Unity: `6000.3.16f1`, registrada em `game/ProjectSettings/ProjectVersion.txt`.
- Regras de xadrez: `ChessDotNet`, em `game/Assets/Plugins/ChessDotNet.dll`.
- Testes automatizados: EditMode no Test Runner da Unity.
- Relatorio tecnico: `docs/report/relatorio-tecnico.pdf`.
- Roteiro de gravacao do video: `docs/video/roteiro-video.md`.

## Como abrir pelo Git

1. Clone o repositorio.
2. Abra o Unity Hub.
3. Instale a Unity 6.3 LTS `6000.3.16f1`, se ainda nao estiver instalada.
4. No Unity Hub, clique em `Add` ou `Add project from disk`.
5. Selecione a pasta `game` dentro deste repositorio.
6. Abra o projeto.
7. Abra a cena `Assets/Scenes/Main.unity`, caso ela nao abra automaticamente.
8. Clique em `Play`.

O professor nao precisa abrir a pasta raiz do repositorio como projeto Unity. A pasta correta do projeto e `game`.

## Continuar no Windows

O handoff para outro computador esta documentado em:

- `CODEX_WINDOWS_HANDOFF.md`: ponto de partida para o Codex no Windows.
- `docs/setup/windows-environment-setup.md`: instalacao de Unity, Blender, Codex, Unity MCP e Blender MCP.
- `docs/setup/repository-delivery-flow.md`: branches, tags, GitHub, fallback de entrega e fluxo de push.

## Como jogar

- Mouse: selecionar uma peca e depois a casa de destino.
- `Esc`: cancelar selecao.
- `N`: nova partida.
- `Q` / `E`: girar camera.
- Scroll: zoom da camera principal.
- Aba lateral: aparece ao selecionar uma peca, mostra o modelo 3D, permite girar com drag, aproximar/afastar com scroll e usar botoes `+` e `-`.

O jogo alterna automaticamente entre brancas e pretas. Quando o turno muda, a camera vira para o lado do jogador atual.

## Funcionalidades implementadas

- Xadrez 3D local para duas pessoas alternando turnos.
- Regras completas por biblioteca: movimentos legais, xeque, xeque-mate, empate, roque, en passant e promocao.
- Tabuleiro 8x8 com coordenadas logicas e objetos 3D.
- Pecas classicas em primitivas como fallback.
- Personagens personalizados por tipo de peca.
- Interatividade por mouse e teclado.
- Movimento animado das pecas no tabuleiro.
- Camera dinamica por turno.
- HUD em Canvas com tela inicial, instrucoes, historico, status, promocao e painel da peca selecionada.
- Preview 3D interativo da peca selecionada.
- Testes EditMode cobrindo dominio, regras, controller, board view, camera, HUD, prefabs e polimento de cena.

## Pecas personalizadas

| Peca | Personagem | Registro | Prefab |
| --- | --- | --- | --- |
| Peao | Matheus Duarte | Matricula 0276899, criador do jogo | `game/Assets/Resources/CustomPieces/Pawn_Mathwidu_Redhead_v2.prefab` |
| Bispo | Rafael Scharer | Matricula 040603 | `game/Assets/Resources/CustomPieces/Bishop_Rafael.prefab` |
| Cavalo | Gustavo Cornalewski | Matricula 0407923 | `game/Assets/Resources/CustomPieces/Knight_Gustavo.prefab` |
| Torre | Alex Fenner | Matricula 0403240 | `game/Assets/Resources/CustomPieces/Rook_Alex.prefab` |
| Rainha | MARTA ROSECLER BEZ | Professora de Ciencias da Computacao - Universidade Feevale | `game/Assets/Resources/CustomPieces/Queen_Marta.prefab` |
| Rei | RICARDO FERREIRA DE OLIVEIRA | Professor de Ciencias da Computacao - Universidade Feevale | `game/Assets/Resources/CustomPieces/King_Ricardo_Carioca.prefab` |

As fotos usadas como referencia ficam apenas localmente e nao entram no Git. A pasta de referencias privadas esta ignorada por `.gitignore`.

## Estrutura do projeto

```text
.
├── README.md
├── docs/
│   ├── design/        # decisoes tecnicas, fluxo de personagens e roadmap
│   ├── release/       # checklist de entrega
│   ├── report/        # relatorio tecnico em Markdown e PDF
│   ├── setup/         # handoff de ambiente e repositorio
│   └── video/         # roteiro do video demonstrativo
└── game/
    ├── Assets/
    │   ├── Plugins/   # ChessDotNet
    │   ├── Resources/ # prefabs carregados em runtime
    │   ├── Scenes/    # Main.unity
    │   ├── Scripts/   # codigo C#
    │   └── Tests/     # testes EditMode
    ├── Packages/
    └── ProjectSettings/
```

## Testes automatizados

No Unity:

1. Abra `Window > General > Test Runner`.
2. Selecione `EditMode`.
3. Clique em `Run All`.

Resultado esperado: `35` testes passando.

Observacao: se a Unity estiver aberta no projeto, um teste via batchmode externo pode falhar porque a Unity nao permite duas instancias abrindo o mesmo projeto ao mesmo tempo. Nesse caso, rode pelo Test Runner do Editor ou feche o Editor antes de executar em batchmode.

## Build jogavel

Para gerar uma versao jogavel fora do Editor:

1. Abra a cena `Assets/Scenes/Main.unity`.
2. No menu superior, clique em `Chess CGI > Build > macOS`.
3. A build sera gerada em `Builds/macOS/XadrezCGI.app`.

Tambem e possivel usar `File > Build Profiles`, desde que `Assets/Scenes/Main.unity` esteja na lista de cenas.

As pastas `Build/` e `Builds/` sao ignoradas no Git para manter o repositorio leve. Para entrega por Git, o codigo-fonte e os assets em `game/` sao suficientes para abrir e rodar no Editor.

## Versoes estaveis

- `main`: versao jogavel estavel com pacote final de entrega.
- `stable/entrega-v1-estavel`: snapshot preservado da entrega segura.
- `feature/animated-pieces-and-sidebar`: branch de melhorias com side variants, animacoes e pipeline Blender/Codex em evolucao.

## Documentacao complementar

- Checklist de entrega: `docs/release/entrega-checklist.md`.
- Plano inicial: `docs/superpowers/plans/2026-05-29-chess-cgi-unity.md`.
- Fluxo de uso do Unity MCP: `docs/superpowers/plans/2026-05-30-unity-mcp-credit-conscious-workflow.md`.
- Polimento visual e UI: `docs/superpowers/plans/2026-05-30-visual-polish-ui-college-theme.md`.
- Arquitetura e design: `docs/design/chess-cgi-design.md`.
- Fluxo de personagens customizados: `docs/design/custom-piece-generation-workflow.md`.
- Roadmap de animacoes futuras: `docs/design/capture-animation-roadmap.md`.
