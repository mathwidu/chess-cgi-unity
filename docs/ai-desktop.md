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
A resolução busca essa variável, o executável ao lado do jogo (na pasta do
`XadrezCGI.exe`), `Application.persistentDataPath/Engines`, o motor local do
checkout (Editor) e, por último, PATH. Não há download durante
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
para **Brancas / Pretas** e **Iniciante / Intermediário / Difícil**. A borda amarela e o
ícone de confirmação identificam a opção selecionada; um contorno claro identifica
o foco de navegação sem mudar o valor da opção, inclusive em Jogar. Uma descrição
explica o nível escolhido antes de iniciar. No modo local, as opções exclusivas da IA saem da tela.
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

`GameHud.Menu.Layout.cs` constrói o menu; `GameHud.Menu.cs` mantém escolhas,
foco e ajuste ao Canvas. `GameHud.Match.cs` constrói o HUD e os diálogos da partida. O ciclo de vida do Canvas e a integração XR permanecem em `GameHud.cs`.
O menu segue a prévia **Mesa de partida**, aprovada pelo usuário por imagem:
cenário com tabuleiro, livros e planta, professores 3D à esquerda e painel
à direita. A marca Feevale é centralizada pelo conteúdo visível do PNG no
mesmo eixo do painel. Os controles têm bordas arredondadas, confirmação
amarela e foco claro separado; uma descrição explica o nível da IA.
Marta representa as brancas e Ricardo, as pretas. O lado escolhido avança e
recebe mais luz; seus nomes acompanham as bases. No modo local, ambos têm
a mesma ênfase. As texturas e modelos existentes continuam preservados.
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
O utilitário exporta `.impeccable/review/*.png` em cinco dimensões, incluindo
a referência 1672×941, e nos estados IA com pretas/difícil, brancas/iniciante,
local, ajuda, partida, seleção, promoção e erro, além de Intermediário
selecionado em 1024×768 e Jogar com foco pelo teclado,
e sai sem gerar build. Promoção e erro usam condições determinísticas de teste.
Para preservar as cores da UI overlay, a captura desativa o pós-processamento
na câmera de evidência. O tabuleiro nessas imagens fica sem os efeitos de cor
da câmera; a configuração da cena e do jogo não é salva ou alterada.

## Arquitetura e contratos de integração

### Mapa de leitura do código

| Responsabilidade | Fonte principal |
| --- | --- |
| Jogada e fotografia imutável da posição | `Domain/ChessMove.cs`, `Domain/PositionSnapshot.cs` |
| Contrato de escolha de jogada | `AI/IMoveChooser.cs` |
| Dificuldade e limites de busca | `AI/ComputerDifficulty.cs`, `AI/MoveSearchSettings.cs` |
| Prazo, cancelamento e descarte de resultado antigo | `AI/ComputerTurnCoordinator.cs`, `AI/ComputerTurnResult.cs` |
| Localização do executável e protocolo UCI | `AI/Stockfish/ComputerOpponentFactory.cs`, `AI/Stockfish/StockfishUciMoveChooser.cs` |
| Aplicação da jogada humana ou automática na cena | `Controllers/ChessGameController.cs` |
| Montagem visual do menu e ajuda | `UI/GameHud.Menu.Layout.cs` |
| Escolhas, navegação e ajuste do menu ao Canvas | `UI/GameHud.Menu.cs` |
| HUD da partida e diálogos de promoção/erro | `UI/GameHud.Match.cs` |
| Professores isolados em RenderTexture | `UI/MenuCastPreview.cs` |
| Geometria de controles e sombras | `UI/MenuSurface.cs`, `UI/MenuGroundShadow.cs` |

Os caminhos acima partem de `game/Assets/Scripts/`. A construção da interface
segue blocos nomeados, mantendo a hierarquia e as medidas da composição aprovada.
A inicialização UCI, configuração da busca e leitura de `bestmove` ficam em
rotinas separadas; a sincronização e o ciclo de vida do processo continuam no
mesmo adaptador. Tipos públicos têm seus próprios arquivos. Métodos assíncronos
retornam `Task`, inclusive a observação intencional de falhas tardias.

`game/Assets/Editor/MenuReviewCapture.cs` reúne nome, dimensão, preparação e
foco esperado de cada captura em um único registro. Os casos formam uma jornada
ordenada; preparação de promoção e falha ficam restritas ao utilitário do Editor.
Não há acesso mutável adicional às regras na interface pública do jogo.

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

Em 22/09/2026, os PRs #14 (`vr/08-validation-fixes`) e #15
(`vr/09-performance-mode`) também estavam abertos. Eles são posteriores à
base de integração registrada acima e não fazem parte da validação desta IA.
As branches #5–#11 receberam novos SHAs; entre `9ac1c3a` e
`vr/07-tooling-fixes@2150b65` a diferença observada foi somente documental.
A integração de #14/#15 precisa conciliar suas novas mudanças de HUD/controladores
separadamente; este PR desktop continua independente da sequência VR.

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

Evidências locais, Unity 6000.3.16f1, macOS ARM64:

| Verificação | Resultado e versão |
| --- | --- |
| EditMode após organização para PR, 22/09/2026 | 36/36 aprovados, nenhum ignorado; `TestResults/EditMode.xml`, incluindo Stockfish real e falhas de processo |
| PlayMode desktop após organização para PR | 13/13 aprovados, nenhum ignorado; `TestResults/PlayMode.xml` |
| EditMode e PlayMode na combinação com VR, após organização | 36/36 e 13/13 aprovados, nenhum ignorado; `.local/vr-integration/TestResults/EditMode.xml` e `PlayMode.xml` |
| Interface da Unity, revisão visual anterior | Cena Main aberta no checkout desktop e Play iniciado pelo menu Chess CGI; troca para Pretas/Difícil confirmada na aba Game |
| Capturas nativas após organização | 14 estados/tamanhos exportados após estabilização da transição; `TestResults/PrCleanupCapture.log` |
| HUD com XR Interaction Simulator, mesma revisão | PASSED, exit 0; raio atingiu StartPlayButton e iniciou a partida |
| Revisão visual independente anterior, roteiro local Impeccable | F1–F3 resolvidos; `.impeccable/review/study-review.md` |
| Build macOS | Anterior à reforma; nenhuma build gerada nesta revisão |

A validação atual usa Play no Editor. As capturas do menu cobrem 1672×941,
1280×800, 1024×768, 2560×1080 e 1223×704, além de estados em 1920×1080.
São renders da cena real, não screenshots da interface do Editor.
O utilitário espera no mínimo 45 frames e 1,25 s após preparar cada estado: a
transição usa tempo real e não deve ser fotografada ainda em andamento. O painel
de configuração foi comparado com as capturas anteriores nas cinco dimensões;
layout, rótulos e seleção foram preservados. O teste de
texto inclui Canvas world-space e Intermediário selecionado; o teste de foco
confirma navegação até Jogar. Também verifica proporção e centralização óptica
da marca, geometria renderizada e alternância dos professores. O teste de
partida comprova início com pretas, jogada real do motor, resposta ao humano,
perspectiva fixa e retorno ao modo local.

Execute testes de processo e capturas em sequência. Uma execução de EditMode
na integração, concorrente com outro Editor capturando a cena, atingiu o prazo
em quatro testes do motor simulado (250/1000 ms). A execução isolada passou
36/36 sem alterar os prazos nem a implementação. Esse resultado registra
sensibilidade da verificação ao ambiente; não demonstra estabilidade sob carga
concorrente nem uma falha das regras do jogo.

A revisão visual anterior ficou limitada às correções levantadas: contraste dos nomes,
Intermediário no compacto e foco de Jogar. O comparador mecânico registra 88,54%
(`match`), sem significar identidade visual ou substituir a inspeção. Os modelos
reais, a marca original e Lato são adaptações deliberadas da referência. A
validação nativa e a limitação do gate web do Impeccable estão em
`.impeccable/review/study-verdict.md`.

O log `.local/vr-integration/TestResults/PrCleanupXRHud.log` registra o clique
por raio e `CHESS_CGI_XR_HUD_CHECK PASSED`. Também registra
`XR_ERROR_RUNTIME_UNAVAILABLE` (Mac sem runtime/headset), `Missing ILineRenderable / Ray Interactor`
na inicialização dos visuais e uma exceção do indexador `UnityEditor.Search`.
As asserções do harness passaram; isso não comprova uma sessão XR sem erros
nem conforto/desempenho no headset. A auto-instanciação do simulador foi
restaurada para desativada após a verificação. Os harnesses de seleção e câmera
passaram na revisão anterior e não foram repetidos nesta refatoração.

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
