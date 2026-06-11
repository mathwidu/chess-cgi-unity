# Xadrez CGI

Projeto de Computacao Grafica I desenvolvido em Unity 6.3 LTS. O jogo e um xadrez 3D local para duas pessoas, com regras completas, tabuleiro interativo, camera por turno, HUD em Canvas e personagens personalizados inspirados na turma.

## Como Abrir

1. Clone este repositorio.
2. Abra o Unity Hub.
3. Instale a Unity 6.3 LTS `6000.3.16f1`, se ainda nao estiver instalada.
4. No Unity Hub, clique em `Add` ou `Add project from disk`.
5. Selecione a pasta `game` dentro deste repositorio.
6. Abra o projeto.
7. Abra a cena `Assets/Scenes/Main.unity`, caso ela nao abra automaticamente.
8. Clique em `Play`.

A pasta correta para abrir como projeto Unity e `game`.

## Como Jogar

- Mouse: selecionar uma peca e depois a casa de destino.
- `Esc`: cancelar selecao.
- `N`: iniciar nova partida.
- `Q` / `E`: girar a camera.
- Scroll: aproximar ou afastar a camera principal.
- Aba lateral: ao selecionar uma peca, mostra o modelo 3D, permite girar com drag, aproximar/afastar com scroll e usar os botoes `+` e `-`.

O jogo alterna automaticamente entre brancas e pretas. Quando o turno muda, a camera vira para o lado do jogador atual.

## Funcionalidades

- Xadrez 3D local para duas pessoas alternando turnos.
- Regras completas por biblioteca: movimentos legais, xeque, xeque-mate, empate, roque, en passant e promocao.
- Tabuleiro 8x8 com coordenadas logicas e objetos 3D.
- Pecas classicas como fallback visual.
- Personagens personalizados por tipo de peca.
- Interatividade por mouse e teclado.
- Movimento animado das pecas no tabuleiro.
- Camera dinamica por turno.
- HUD com tela inicial, instrucoes, historico, status, promocao e painel da peca selecionada.
- Preview 3D interativo da peca selecionada.

## Pecas Personalizadas

| Peca | Personagem | Registro | Prefab |
| --- | --- | --- | --- |
| Peao | Matheus Duarte | Matricula 0276899, criador do jogo | `game/Assets/Resources/CustomPieces/Pawn_Mathwidu_Redhead_v2.prefab` |
| Bispo | Rafael Scharer | Matricula 040603 | `game/Assets/Resources/CustomPieces/Bishop_Rafael.prefab` |
| Cavalo | Gustavo Cornalewski | Matricula 0407923 | `game/Assets/Resources/CustomPieces/Knight_Gustavo.prefab` |
| Torre | Alex Fenner | Matricula 0403240 | `game/Assets/Resources/CustomPieces/Rook_Alex.prefab` |
| Rainha | Marta Rosecler Bez | Professora de Ciencias da Computacao - Universidade Feevale | `game/Assets/Resources/CustomPieces/Queen_Marta.prefab` |
| Rei | Ricardo Ferreira de Oliveira | Professor de Ciencias da Computacao - Universidade Feevale | `game/Assets/Resources/CustomPieces/King_Ricardo_Carioca.prefab` |

As fotos usadas como referencia ficam apenas localmente e nao entram no Git. Os personagens foram criados com apoio de ferramentas de IA generativa para modelagem 3D a partir de referencias visuais autorizadas, depois importados e configurados como prefabs no Unity.

## Estrutura

```text
.
├── README.md
├── RELATORIO_TECNICO.md
├── RELATORIO_TECNICO.pdf
└── game/
    ├── Assets/
    │   ├── Plugins/   # ChessDotNet
    │   ├── Resources/ # prefabs carregados em runtime
    │   ├── Scenes/    # Main.unity
    │   └── Scripts/   # codigo C#
    ├── Packages/
    └── ProjectSettings/
```

## Build Jogavel

Para gerar uma versao jogavel fora do Editor:

1. Abra a cena `Assets/Scenes/Main.unity`.
2. No menu superior, clique em `Chess CGI > Build > macOS`.
3. A build sera gerada em `Builds/macOS/XadrezCGI.app`.

Tambem e possivel usar `File > Build Profiles`, desde que `Assets/Scenes/Main.unity` esteja na lista de cenas.

As pastas `Build/` e `Builds/` sao ignoradas no Git para manter o repositorio leve. Para entrega por Git, o codigo-fonte e os assets em `game/` sao suficientes para abrir e rodar no Editor.
