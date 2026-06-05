# Xadrez 3D CGI

Projeto de Computacao Grafica I desenvolvido em Unity 6.3 LTS.

## Estado atual

- Jogo 3D local para duas pessoas alternando turnos.
- Regras completas delegadas para `ChessDotNet`: movimentos legais, xeque, xeque-mate, empate, roque, en passant e promocao.
- Tabuleiro com moldura, mesa e ambientacao simples de sala/aula.
- Pecas classicas montadas com primitivas 3D quando nao ha personagem customizado.
- Peao, torre, cavalo, bispo, rainha e rei personalizados gerados como prefabs 3D e integrados ao tabuleiro.
- Camera alterna automaticamente para o lado do jogador da vez, com enquadramento mais proximo.
- HUD mostra tela inicial, instrucoes, estado da partida, escolha de promocao, historico recente, nova partida e detalhe da peca selecionada com preview 3D.

## Como abrir

1. Instale Unity Hub.
2. Instale Unity 6.3 LTS `6000.3.16f1`.
3. No Unity Hub, clique em `Add` e selecione a pasta `game`.
4. Abra a cena `Assets/Scenes/Main.unity`.
5. Clique em `Play`.

## Controles

- Mouse: selecionar peca e casa de destino.
- `Q` / `E`: girar camera.
- Scroll: zoom.
- `R`: resetar camera.
- `N`: nova partida.
- `Esc`: cancelar selecao.

## Testes automatizados

No Unity, abra `Window > General > Test Runner`, selecione `EditMode` e clique em `Run All`.
A suite atual cobre dominio, regras especiais de xadrez, montagem do tabuleiro, fluxo do controller, promocao, camera por turno e suporte a pecas personalizadas.

Resultado esperado nesta fase: `34` testes passando.

## Tema visual

O jogo usa uma ambientacao sutil de faculdade/CGI: tabuleiro sobre mesa, fundo de sala, quadro e props simples feitos com primitivas. A ideia e valorizar os personagens da turma sem deixar o visual pesado ou poluido.

Regra de assets: antes de regenerar um personagem, tente melhorar iluminacao, camera, escala e materiais. Os meshes gerados devem ser recriados apenas quando a silhueta ou identidade estiverem erradas.

## Pecas personalizadas

O jogo suporta prefabs customizados por tipo de peca no `PieceFactory`. Se um prefab estiver configurado, ele e usado como visual da peca; se nao houver prefab, o jogo volta para a forma classica criada com primitivas.

Escalacao atual:

| Peca | Personagem | Prefab |
| --- | --- | --- |
| Peao | Mathwidu | `Assets/Resources/CustomPieces/Pawn_Mathwidu_Redhead_v2.prefab` |
| Torre | Alex | `Assets/Resources/CustomPieces/Rook_Alex.prefab` |
| Cavalo | Gustavo | `Assets/Resources/CustomPieces/Knight_Gustavo.prefab` |
| Bispo | Rafael | `Assets/Resources/CustomPieces/Bishop_Rafael.prefab` |
| Rainha | Marta | `Assets/Resources/CustomPieces/Queen_Marta.prefab` |
| Rei | Ricardo Carioca | `Assets/Resources/CustomPieces/King_Ricardo_Carioca.prefab` |

O fluxo de producao para novos personagens esta documentado em `docs/design/custom-piece-generation-workflow.md`.

As fotos usadas como referencia ficam apenas localmente em `Assets/Art/PrivateReferences/` e sao ignoradas pelo git.

## Build jogavel

Para gerar uma versao jogavel fora do Editor:

1. Abra a cena `Assets/Scenes/Main.unity`.
2. No menu superior, clique em `Chess CGI > Build > macOS`.
3. A build sera gerada em `Builds/macOS/XadrezCGI.app`.

Tambem e possivel usar `File > Build Profiles`, desde que `Assets/Scenes/Main.unity` esteja na lista de cenas.

## Versao estavel

A versao entregavel atual esta marcada com a tag `entrega-v1-estavel`.
As melhorias de animacao, preview 3D e polimento visual devem ser feitas em branches separadas para manter a entrega segura.

## Entregaveis

- Codigo-fonte Unity em `game/`.
- Relatorio em `docs/report/`.
- Video demonstrativo em `docs/video/`.
