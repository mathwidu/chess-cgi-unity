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
- `GameHud` usa `OnGUI`, uma forma simples de desenhar interface no Editor sem criar ainda uma UI definitiva com Canvas.
- Testes EditMode ajudam a validar regras e fluxos sem precisar jogar partidas completas manualmente.
- Para promocao, o controller pausa a jogada em `IsAwaitingPromotion` ate o jogador escolher rainha, torre, bispo ou cavalo.

## Marco 4: Polimento visual e animacoes

- `PieceMotionController` executa movimento visual sem alterar a regra do xadrez.
- `PieceView.MoveWithWalk` anima uma caminhada procedural simples ate a casa final.
- `CaptureResolver` identifica a peca capturada antes de `BoardView.SyncPieces` recriar o tabuleiro visual.
- `ImpactEffect` e `CaptureAnimationLibrary` criam uma captura curta com estilos por tipo de peca.
- `CharacterAnimationDriver` prepara os modelos para futuros rigs/Animator, mantendo fallback procedural quando o prefab nao tem animacao.
- Testes PlayMode sao uteis quando o comportamento depende de `Start`, corrotinas e tempo real.
- A tag `entrega-v1-estavel` protege a primeira entrega; a tag `entrega-v2-polida` marca a versao com polimento profissional.
