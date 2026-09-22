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
   dificuldade. Escolha as opções desejadas e clique em **Jogar**.
   Também é possível usar **Chess CGI → Jogar no Editor** para abrir a cena
   e iniciar o Play pela interface da Unity, sem gerar uma build.
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

## Menu e teste local no Editor

O menu apresenta **Contra IA** e **Dois jogadores**, com escolhas diretas
para **Brancas / Pretas** e **Iniciante / Intermediário / Difícil**. O sublinhado amarelo e o
destaque identificam a opção selecionada; um contorno claro identifica o foco
de navegação sem mudar o valor da opção; o resumo confirma a configuração
antes de iniciar. No modo local, as opções exclusivas da IA saem da tela.
Voltar ao modo IA conserva as escolhas durante a sessão; sair de Play restaura
os valores iniciais (IA, brancas, iniciante).

Roteiro manual, diretamente na aba **Game** da Unity:

- Inicie contra IA de brancas, mova `e2 → e4` e aguarde a resposta.
- Volte ao menu, escolha pretas e outro nível: a IA deve abrir a partida.
- Experimente os três níveis; a indicação do HUD deve corresponder à escolha.
- Use setas e Enter desde a abertura para configurar e iniciar, sem clicar antes.
- Abra e feche **Como jogar**; a configuração deve ser preservada.
- Selecione uma peça para ver o preview, arraste o modelo e teste o zoom.
- Use **Nova partida**: mesmas opções, tabuleiro reiniciado.
- Troque para **Dois jogadores** e confirme a alternância de turno e câmera.
- Redimensione a aba Game: o menu deve permanecer inteiro e legível.

`GameHud.Menu.cs` concentra a configuração e `GameHud.Match.cs` apresenta a
partida. O ciclo de vida do Canvas e a integração XR permanecem em `GameHud.cs`.
O menu usa a direção **Palco da turma**, escolhida pelo usuário: campo verde,
assinaturas no cabeçalho, Marta e Ricardo à esquerda e configuração em
coluna à direita. O lado escolhido destaca o professor correspondente: Marta
representa as brancas e Ricardo, as pretas; no modo local, os dois recebem
o mesmo destaque. A iluminação do palco é independente da partida e a marca
Feevale mantém sua proporção original, sem redimensionamento NPOT.
`MenuCastPreview` mantém esses modelos fora da partida e renderiza apenas na
abertura ou durante a transição de lado; os colliders ficam desativados e
o palco é desativado durante a partida. As luzes direcionais externas são
suspensas somente durante o render síncrono do preview, com restauração em finally. A
composição reaproveita uGUI e o Input System, sem novos pacotes. A assinatura
original da Feevale e as fontes Lato (SIL OFL) têm suas origens registradas em
`game/Assets/Resources/UI/ORIGINS.md`; Lato não é a fonte institucional Avenir.

As decisões visuais ficam em `DESIGN.md`. Para repetir as capturas da cena real,
com esse checkout fechado no Editor, execute Unity em `-batchmode` com
`-projectPath game -executeMethod MenuReviewCapture.Run -logFile TestResults/menu-capture.log`.
O utilitário exporta `.impeccable/review/*.png` em cinco dimensões de desktop e
nos estados IA com pretas/difícil, local, ajuda, partida, seleção, promoção e erro,
e sai sem gerar build. Promoção e erro usam condições determinísticas de teste.
Para preservar as cores da UI overlay, a captura desativa o pós-processamento
na câmera de evidência. O tabuleiro nessas imagens fica sem os efeitos de cor
da câmera; a configuração da cena e do jogo não é salva ou alterada.

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
| PlayMode desktop antes da troca para dois professores | 12/12 aprovados, nenhum ignorado |
| Build macOS anterior à reforma do menu | Succeeded, 0 erros e 1 aviso; não atualizada nesta reforma |
| Compilação da combinação com a ponta VR | Concluída sem erros de C# |
| PlayMode atual na combinação com os pacotes VR, incluindo professores e proporção da marca | 13/13 aprovados, nenhum ignorado |
| Harnesses com XR Interaction Simulator | HUD, seleção por controle e câmera aprovados |

A validação atual é pelo Editor; a build anterior não representa o menu reformulado.
A redistribuição do menu foi verificada em 1920×1080, 1280×800, 1024×768,
2560×1080 e 1223×704. O teste de texto também verifica Canvas world-space. A última revisão foi
capturada e testada na cópia de integração, com fontes de menu, modelos e marca
idênticos à cópia desktop, evitando abrir outro processo sobre os Editors em uso.
Os harnesses XR foram reexecutados após essa redistribuição: o raio do controle
alcançou Jogar e iniciou a partida. O teste automatizado da cena
real comprova início com pretas, jogada real do motor, resposta ao humano,
perspectiva fixa e retorno ao modo local. Não equivale a teste em headset.

Os três harnesses concluíram com exit code 0 e marcadores PASSED na branch de
integração. Os logs estão em `.local/vr-integration/TestResults/`, incluindo
`XRHudVerification.log`, `XRControllerVerification.log` e
`XRCameraVerification.log`. Ainda registram `XR_ERROR_RUNTIME_UNAVAILABLE`
(no Mac sem runtime/headset), `Missing ILineRenderable / Ray Interactor`
(inicialização do visual dos raios) e uma exceção do indexador `UnityEditor.Search`.
Portanto, os resultados aprovam as asserções desses harnesses; não comprovam
uma sessão XR sem erros. A triagem do visual dos raios e a validação em hardware
continuam com a frente VR. A configuração do simulador foi restaurada para
auto-instanciação desativada após a verificação.

O SHA-256 do executável macOS usado foi
`bc0cac905ecdf2147fe22055c733bcd999b1e3f7c399fbaf7fb9055786563590`.
A build anterior é `Builds/macOS/XadrezCGI.app`; para testar a interface atual, use Play no Editor. Os relatórios e logs ficam em `TestResults/`. Cobertura: parsing, FEN/histórico,
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
