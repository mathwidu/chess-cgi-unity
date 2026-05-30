# Xadrez 3D CGI

Projeto de Computacao Grafica I desenvolvido em Unity 6.3 LTS.

## Estado atual

- Jogo 3D local para duas pessoas alternando turnos.
- Regras completas delegadas para `ChessDotNet`: movimentos legais, xeque, xeque-mate, empate, roque, en passant e promocao.
- Tabuleiro e pecas classicas montados com primitivas 3D.
- Camera alterna automaticamente para o lado do jogador da vez.
- HUD mostra estado da partida, escolha de promocao, historico recente e nova partida.

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
A suite atual cobre dominio, regras especiais de xadrez, montagem do tabuleiro, fluxo do controller, promocao e camera por turno.

Resultado esperado nesta fase: `17` testes passando.

## Build jogavel

Para gerar uma versao jogavel fora do Editor:

1. Abra `File > Build Profiles`.
2. Selecione o alvo desejado, como macOS ou WebGL.
3. Garanta que `Assets/Scenes/Main.unity` esteja na lista de cenas.
4. Clique em `Build`.

## Entregaveis

- Codigo-fonte Unity em `game/`.
- Relatorio em `docs/report/`.
- Video demonstrativo em `docs/video/`.
