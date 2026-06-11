# Learning Log - Unity

## Marco 1: Projeto e Editor

- Scene: janela onde a cena 3D e editada.
- Game: janela que mostra a camera do jogo.
- Hierarchy: lista de GameObjects da cena.
- Inspector: painel de componentes do GameObject selecionado.
- Project: arquivos do projeto dentro de `Assets`.
- Transform: componente que controla posicao, rotacao e escala.

## Marco 2: Bibliotecas externas

- `Assets/Plugins` permite colocar DLLs que scripts C# da Unity podem referenciar.
- A biblioteca `ChessDotNet` guarda a regra do xadrez fora da apresentacao 3D.
- Separar regra e visual reduz retrabalho quando a cena, modelos ou WebGL mudarem.
- Arquivos `.asmdef` definem assemblies. O Test Runner so encontra testes quando eles estao numa assembly que referencia NUnit e os runners da Unity.

## Marco 3: Fluxo de jogo

- `ChessGameController` coordena selecao, turno, movimento, promocao, historico e mensagens.
- `BoardView` cuida da representacao visual do tabuleiro e sincroniza as pecas depois de cada jogada.
- `GameHud` usa Canvas/UGUI para a tela inicial, historico, status, promocao e aba lateral da peca selecionada.
- A aba lateral usa uma camera separada com `RenderTexture` para mostrar o modelo 3D selecionado sem mexer na cena principal.
- `SelectedPiecePreviewInput` permite girar o preview com drag e controlar o zoom com scroll ou botoes.
- Testes EditMode ajudam a validar regras e fluxos sem precisar jogar partidas completas manualmente.
- Para promocao, o controller pausa a jogada em `IsAwaitingPromotion` ate o jogador escolher rainha, torre, bispo ou cavalo.
