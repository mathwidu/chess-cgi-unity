# Adversário offline no desktop

O incremento adiciona partidas contra Stockfish 18 no desktop, mantendo o modo
local de dois jogadores. O jogador escolhe lado e dificuldade, vê o estado de
pensamento e pode recuperar uma falha sem perder a posição. O protocolo segue a
[documentação oficial UCI](https://official-stockfish.github.io/docs/stockfish-wiki/UCI-Protocol-and-Stockfish-Commands.html).

## Preparar e jogar

1. Use Unity `6000.3.16f1` e abra `game` desta branch/worktree.
2. Execute `python3 tools/setup_stockfish.py` na raiz. O script seleciona o
   artefato oficial do sistema atual, confere SHA-256 e conserva licença, fonte
   e manifesto em `.local/stockfish/`, ignorado no Git e fora de Assets.
3. Abra `Assets/Scenes/Main.unity`, entre em Play, escolha modo, lado e
   dificuldade. Clique nas opções para alternar e em **Iniciar partida**.
4. **Nova partida** ou `N` reinicia com a mesma configuração. **Menu** permite
   trocar configuração ou voltar a dois jogadores. A partida funciona offline
   depois de preparar o motor.

Para um motor externo, `CHESS_STOCKFISH_PATH` pode apontar para seu executável.
A resolução busca essa variável, `Application.persistentDataPath/Engines`, o
motor local do checkout (Editor) e, por último, PATH. Não há download durante
uma partida nem fallback silencioso para jogadas aleatórias.

Uma build macOS local usa o motor em `Application.persistentDataPath/Engines`.
Neste projeto, no macOS:

```sh
python3 tools/setup_stockfish.py --player-directory "$HOME/Library/Application Support/Faculdade CGI/Xadrez CGI"
```

O setup recusa substituir um motor diferente já presente. A restauração é
remover os três arquivos criados em `Engines` (`stockfish`, `Copying.txt` e
`manifest.json`); o checkout mantém sua cópia local. Nenhuma configuração global
ou pacote do sistema é instalado.

## Arquitetura e contratos de integração

- `ChessMove` representa origem, destino e promoção, independentemente da cena.
- `ChessRulesAdapter.GetSnapshot()` fornece FEN, lado, revisão monotônica e
  histórico desde a posição inicial. O motor recebe o histórico para poder
  considerar repetições; as regras existentes continuam decidindo o resultado.
- `IMoveChooser` recebe valores imutáveis e devolve uma candidata assíncrona.
  Não conhece `GameObject`, mouse, HUD ou OpenXR.
- `ComputerTurnCoordinator` aplica prazo, cancelamento e descarte por revisão
  e lado. Seu resultado é consumido no Update da Unity, na thread principal.
- `ChessGameController` aceita entradas humanas e automáticas no mesmo fluxo de
  regras, animação, sincronização e histórico. A seleção é bloqueada no turno
  automático, inclusive quando o motor falha.
- `StockfishUciMoveChooser` mantém um processo por partida, drena saída/erro e
  realiza toda E/S em worker. Usa `uci`, `isready`, `ucinewgame`, `position`,
  `go`, `stop` e `quit`. Cancelamento encerra somente o processo filho conhecido;
  após 250 ms sem saída normal, esse filho é finalizado.

Os perfis iniciais usam Skill Level 0/6/14, tempo de busca 150/500/1200 ms e
prazo total 6000/6500/7200 ms. Threads=1, Hash=16 MiB, Ponder=false e
UCI_LimitStrength=false. São perfis iniciais, sem equivalência de Elo validada.

## Trabalho paralelo com VR

A branch de IA parte de `origin/main` (`9d59fa7`), sem depender dos pacotes XR.
A combinação foi preparada separadamente na branch `codex/ai-vr-integration`.
A ponta usada como referência é `origin/vr/07-tooling-fixes` (`9ac1c3a`), fim
da sequência de PRs #5–#11. O histórico desses PRs deve ser integrado na ordem
que a frente de VR já estabeleceu.

O merge C# e dos pacotes foi automático. Nove documentos exigiram conciliação
entre a documentação da IA e da frente VR; a resolução está na branch de
integração. A mesma branch adapta os harnesses antigos ao bloqueio do menu,
ao centro do botão de iniciar e à espera da animação por tempo real.

Preservar no merge:

- A guarda `!XRRig.IsHeadsetPresent` em `UpdateCameraForTurn`: a IA escolhe o
  lado humano para a câmera desktop, mas nunca deve girar o headset.
- A configuração world-space, o `XRUIInputModule` e o raycaster XR no HUD. Os
  botões novos são uGUI e reutilizam essa infraestrutura.
- As chamadas de `VrSelectionBridge` a `SelectPiece`, `SelectDestination` e
  `CancelSelection`; o bloqueio por participante já está no controlador comum.
- As alterações de câmera, prefabs, mãos e pacotes da frente VR.

Uma simulação de merge e compilação combinada são evidência de integração de
código. Elas não comprovam conforto, rastreamento, controles nem desempenho
em headset. Windows PC-VR depende também de build e executável Windows. Quest
standalone exige um adaptador Android próprio e teste em hardware; a fábrica
retorna uma falha recuperável nas plataformas ainda sem adaptador.

## Verificar

`tools/test_unity.sh` executa EditMode e PlayMode e exige relatórios XML com
resultado aprovado. A execução real do Stockfish é identificada pela categoria
`StockfishIntegration`; sem motor local, esses testes são explicitamente
ignorados. Não conte testes ignorados como integração aprovada.

Evidências locais de 2026-09-21, Unity 6000.3.16f1, macOS ARM64:

| Verificação | Resultado |
| --- | --- |
| EditMode, incluindo Stockfish real e falhas de processo | 36/36 aprovados, nenhum ignorado |
| PlayMode, incluindo a cena Main e seus botões | 8/8 aprovados, nenhum ignorado |
| Build macOS | Succeeded, 0 erros e 1 aviso |
| Compilação da combinação com a ponta VR | Concluída sem erros de C# |
| PlayMode na combinação com os pacotes VR, sem headset | 8/8 aprovados |

O executável foi aberto e o menu foi inspecionado. O teste automatizado da cena
real comprova início com pretas, jogada real do motor, resposta ao humano,
perspectiva fixa e retorno ao modo local. Não equivale a teste em headset.

O SHA-256 do executável macOS usado foi
`bc0cac905ecdf2147fe22055c733bcd999b1e3f7c399fbaf7fb9055786563590`.
A build é `Builds/macOS/XadrezCGI.app`. Os relatórios e logs ficam em `TestResults/`. Cobertura: parsing, FEN/histórico,
revisão, promoção e subpromoção, roque, en passant, mate, empate, prazo,
cancelamento, resposta antiga, erro de processo, preservação do modo local,
reinício durante animação e interação pela tela inicial real.

## Distribuição e próximos alvos

Este incremento não coloca executáveis em Assets nem na build gerada. A build
local requer o setup acima. Um pacote para terceiros precisa incluir os avisos,
a licença e o código-fonte correspondente ao binário, conforme as
[instruções oficiais do Stockfish](https://official-stockfish.github.io/docs/stockfish-wiki/Developers.html#terms-of-use).
Esse pacote continua pendente de decisão e validação próprias.

Permanecem pendentes a calibração com jogadores, o teste Windows/PC-VR, o
adaptador Quest ARM64/IL2CPP e as medições no headset. O executável macOS ARM64
não pode ser reutilizado em Android ARM64.
