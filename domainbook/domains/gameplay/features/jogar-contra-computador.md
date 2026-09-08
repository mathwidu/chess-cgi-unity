---
id: jogar-contra-computador
name: Jogar contra um adversário controlado pelo computador
status: draft
owners: [mathwidu]
terms: [adversário-controlado-pelo-computador, nível-de-dificuldade, estado-de-pensamento, motor-de-xadrez]
---

## Story

Como jogador sem um segundo participante disponível
Quero disputar uma partida completa contra um adversário controlado pelo computador
Para escolher um nível de desafio adequado no desktop e em realidade virtual

### 1. Resumo executivo

Esta funcionalidade encontra-se em fase de planejamento. O jogo ainda não possui
adversário automático, integração com motor de xadrez, configuração de
dificuldade ou empacotamento de IA para qualquer plataforma.

A proposta é integrar o Stockfish 18 como [motor de xadrez](../glossary.md)
offline. O motor receberá uma fotografia da posição, calculará uma jogada e
devolverá uma candidata ao jogo. As regras locais continuarão responsáveis por
validar e aplicar cada movimento.

Não está previsto o treinamento de um novo modelo. O Stockfish já combina busca
de xadrez com uma rede NNUE pré-treinada. O trabalho do projeto consiste em
integrar, controlar, limitar, testar e distribuir o motor de forma compatível com
a experiência de jogo e com os alvos de realidade virtual.

O alvo final é Meta Quest 3 standalone, sem conexão com um computador. A
implementação será validada progressivamente no Editor e em macOS, depois em
Windows PC-VR/Quest Link e, por último, em Android ARM64 no headset.

### 2. Estado atual do projeto

#### 2.1 Fluxo de jogo existente

O fluxo entregue para a disciplina é uma partida local para dois jogadores:

```text
InputController
    -> ChessGameController
        -> ChessRulesAdapter / Chess.NET
            -> animação, tabuleiro, histórico, HUD e câmera
```

- `InputController` converte mouse e teclado em seleção de peças e casas.
- `ChessGameController` mantém o fluxo da partida, a seleção, a animação, o
  histórico, o status e a troca de perspectiva.
- `ChessRulesAdapter` encapsula Chess.NET, mantém a posição e valida movimentos,
  xeque, xeque-mate, empate, roque, en passant e promoção.
- `BoardView`, `PieceView` e `GameHud` apresentam o resultado da jogada.
- O controlador já bloqueia entrada durante animação e promoção, mas não possui
  um estado específico para o turno de um participante automático.

#### 2.2 Capacidades reutilizáveis

| Capacidade existente | Uso na funcionalidade proposta |
| --- | --- |
| Regras protegidas por `ChessRulesAdapter` | Validar toda jogada devolvida pelo motor |
| Conversão FEN disponível em Chess.NET | Gerar uma fotografia serializável da posição |
| Um único fluxo visual de jogada | Aplicar movimentos humanos e automáticos pela mesma animação |
| Bloqueio de entrada | Impedir jogadas humanas durante o turno do computador |
| HUD e histórico existentes | Exibir pensamento, erro, jogada e término da partida |
| Unity Input System e plano de OpenXR | Manter a escolha de jogada independente do dispositivo de entrada |

#### 2.3 Lacunas identificadas

| Lacuna | Consequência atual |
| --- | --- |
| Não existe representação de jogada independente da cena | A jogada humana começa em `PieceView` e não pode ser produzida por outro participante |
| Não existe fotografia imutável da posição | Um motor não pode consultar a partida sem conhecer o objeto vivo de regras |
| Não existe interface de escolha de jogada | Processo UCI, plug-in Android e testes ficariam acoplados ao controlador |
| Não existe coordenador de turno automático | Cancelamento, timeout e respostas atrasadas não possuem tratamento |
| Não existem testes EditMode ou PlayMode ativos | Uma refatoração do controlador pode quebrar o modo atual sem detecção automática |
| Não existem artefatos ou regras de empacotamento do motor | Nenhuma build distribui ou inicia o Stockfish |
| Não existe configuração de modo, lado ou dificuldade | O HUD só inicia partidas humano contra humano |
| Não existem estados de erro do motor | Falha de processo ou timeout não possui resposta definida |

#### 2.4 Ambiente técnico verificado

| Item | Estado verificado | Interpretação |
| --- | --- | --- |
| Unity | Projeto fixado em `6000.3.16f1`; Editor e licença ativos | Base local disponível para implementação |
| macOS | Ambiente de desenvolvimento ARM64 | Permite validar gameplay, UCI e build macOS |
| Stockfish | Versão 18, executável Mach-O ARM64, handshake UCI concluído | Dependência local disponível; integração Unity ainda não implementada |
| Android Build Support | Não instalado no Editor atual | Necessário antes da fase Quest |
| Windows Build Support | Não instalado no Editor atual | Necessário para gerar a build PC-VR neste ambiente |
| Quest 3 | Integração do motor ainda não testada no hardware | Plataforma standalone permanece não comprovada |

### 3. Objetivo e definição de sucesso

A funcionalidade será considerada concluída somente quando:

- uma partida humano contra computador puder ser iniciada e finalizada;
- o computador controlar exatamente um lado e jogar somente no próprio turno;
- toda jogada automática for validada pelas mesmas regras usadas no modo local;
- o modo humano contra humano permanecer disponível;
- os [níveis de dificuldade](../glossary.md) produzirem desafios distintos;
- o [estado de pensamento](../glossary.md) não bloquear renderização, interface
  ou rastreamento do headset;
- reinício, saída da cena, timeout e falha do motor não corromperem a partida;
- cada plataforma usar um artefato compatível e verificável;
- a build standalone executar offline no Quest dentro do orçamento de desempenho;
- as obrigações de distribuição das dependências forem atendidas.

### 4. Escopo

| Incluído | Não incluído |
| --- | --- |
| Modo humano contra computador | Treinamento de rede neural |
| Escolha de lado e dificuldade | Unity ML-Agents |
| Stockfish offline | LLM, API paga ou serviço em nuvem |
| Interface assíncrona de escolha de jogada | Multiplayer em rede |
| Testes com adversário programado | Reescrita das regras de xadrez |
| Integração UCI em macOS e Windows | Conversão completa da interação para VR |
| Experimento e integração no Quest | Atualização geral de pacotes sem necessidade comprovada |
| Empacotamento, licença e observabilidade | Inclusão de binários no PR documental |

O multiplayer local existente permanece parte do produto, mas não será alterado
por esta iniciativa além da preservação de seu comportamento.

### 5. Solução técnica proposta

#### 5.1 Motor recomendado

O Stockfish 18 é a opção recomendada para o primeiro incremento pelos seguintes
motivos:

- funciona offline;
- implementa o protocolo UCI;
- possui controles de força e tempo de busca;
- oferece builds e processo de compilação para desktop e Android;
- já contém a avaliação NNUE necessária;
- evita criar e treinar um motor próprio.

A recomendação ainda depende da aprovação das obrigações da GPLv3. A escolha só
se torna uma decisão aceita após registro explícito da equipe.

#### 5.2 Princípio arquitetural

O motor escolhe uma jogada; ele não controla a partida. O estado vivo e a
legalidade permanecem no gameplay local.

```text
Entrada humana ───────────────┐
                              ├─> fluxo único de aplicação -> regras locais
Adversário / IMoveChooser ────┘                │
                                               └-> animação, tabuleiro, HUD e histórico
```

O ponto de variação será uma interface pequena:

```csharp
public readonly struct ChessMove
{
    public BoardSquare From { get; }
    public BoardSquare To { get; }
    public char? Promotion { get; }
}

public interface IMoveChooser
{
    Task<ChessMove> ChooseMoveAsync(
        PositionSnapshot position,
        MoveSearchSettings settings,
        CancellationToken cancellationToken);
}
```

#### 5.3 Responsabilidades propostas

| Componente | Responsabilidade | Não deve conhecer |
| --- | --- | --- |
| `ChessMove` | Representar origem, destino e promoção | Cena, HUD, Stockfish |
| `PositionSnapshot` | Transportar FEN, lado a jogar e revisão da partida | Objetos mutáveis de Chess.NET |
| `MoveSearchSettings` | Transportar força, tempo e limites de recurso | Processo ou plug-in específico |
| `IMoveChooser` | Solicitar uma jogada de forma assíncrona | Regras visuais e estado da cena |
| `ComputerTurnCoordinator` | Controlar pensamento, timeout, cancelamento e revisão | Detalhes internos do motor |
| `ChessRulesAdapter` | Gerar a fotografia e validar a candidata | UI e empacotamento |
| `ChessGameController` | Orquestrar participantes e o fluxo visual comum | Comandos UCI e ABI da plataforma |

Essa separação concentra a complexidade do motor atrás de `IMoveChooser`. O
controlador recebe o mesmo tipo de jogada independentemente de processo,
plug-in nativo ou implementação de teste.

#### 5.4 Adaptadores previstos

| Adaptador | Finalidade |
| --- | --- |
| `ScriptedMoveChooser` | Testes determinísticos de turno, cancelamento e falhas |
| `StockfishUciMoveChooser` | Editor, macOS e Windows PC-VR |
| Adaptador Quest | Implementação definida após experimento Android no hardware |

#### 5.5 Ciclo de vida UCI

Uma sessão do Stockfish permanecerá ativa durante a partida:

1. iniciar o executável correspondente à plataforma;
2. redirecionar entrada, saída e erro;
3. enviar `uci` e aguardar `uciok` dentro do timeout;
4. aplicar limites de recurso e dificuldade;
5. enviar `isready` e aguardar `readyok`;
6. enviar `ucinewgame` no início de cada partida;
7. enviar `position fen <FEN>` e `go movetime <milissegundos>` em cada turno;
8. interpretar `bestmove`, inclusive promoções;
9. conferir revisão, turno e legalidade antes da aplicação;
10. enviar `stop` em cancelamentos e `quit` no encerramento;
11. finalizar somente o processo filho conhecido se o encerramento normal falhar.

Saída de análise intermediária poderá ser consumida para evitar bloqueio de
buffer, mas não alterará objetos Unity. Apenas `bestmove` produz uma candidata.

### 6. Estratégia de dificuldade

A interface apresentará perfis nomeados em vez de parâmetros internos do motor.
A configuração inicial proposta é:

| Perfil | Experiência pretendida | Configuração a calibrar |
| --- | --- | --- |
| Iniciante | Cometer erros perceptíveis e responder rapidamente | `Skill Level` baixo e busca curta |
| Intermediário | Desafiar jogadores casuais sem dominar a partida | Força limitada e tempo moderado |
| Difícil | Priorizar qualidade dentro do orçamento do dispositivo | Força elevada e tempo máximo aprovado |

O Stockfish 18 verificado oferece `Skill Level` de 0 a 20 e
`UCI_LimitStrength` com `UCI_Elo` de 1320 a 3190. Quando `UCI_LimitStrength`
está ativo, `UCI_Elo` tem precedência. Cada perfil usará uma única estratégia
documentada para evitar combinações ambíguas.

O baseline de recursos será `Threads=1`, hash pequeno e `Ponder=false`. Os
valores finais serão definidos por calibração, e não apenas por equivalência
teórica de Elo.

### 7. Estratégia por plataforma

#### 7.1 Matriz de execução

| Alvo | Artefato do motor | O que a etapa comprova | O que permanece pendente |
| --- | --- | --- | --- |
| Editor e macOS ARM64 | Executável Mach-O para macOS | Arquitetura comum, UCI, dificuldade, falhas e build macOS | Windows, Android e desempenho VR |
| Windows PC-VR / Quest Link | Executável para Windows | Empacotamento no PC, OpenXR e orçamento PC-VR | Execução sem PC |
| Meta Quest 3 standalone | Artefato Android `arm64-v8a` | Execução offline, ciclo de vida e desempenho no headset | Nada; é o portão final |

#### 7.2 Compatibilidade ARM64

macOS ARM64 e Android ARM64 compartilham a família de instruções da CPU, mas não
o formato de executável, a ABI ou o sistema operacional. O executável Mach-O do
macOS não é reutilizável no Quest. O alvo standalone requer código AArch64 para
Android, empacotado de acordo com `arm64-v8a` e validado em uma build IL2CPP.

#### 7.3 Experimento obrigatório no Quest

O experimento deve comparar duas rotas:

| Rota | Vantagem | Risco a comprovar |
| --- | --- | --- |
| Processo UCI separado | Reutiliza o protocolo desktop e mantém separação clara do motor | Permissão de execução, caminho do binário e ciclo de vida no Horizon OS |
| Plug-in nativo Android | Empacotamento nativo suportado pela Unity e controle direto de thread | Ponte C++, estabilidade IL2CPP e implicações de ligação com código GPLv3 |

A decisão será tomada somente após build e medição em um Quest 3 real.

### 8. Impactos da implementação

| Área | Alteração prevista | Impacto |
| --- | --- | --- |
| Gameplay | Novos valores de domínio, fotografia FEN, coordenador de turno e fluxo único de aplicação | Refatoração moderada no `ChessGameController` e no `ChessRulesAdapter` |
| Interação | Entrada humana aceita somente quando o lado humano possui o turno | Reaproveita mouse e futuro OpenXR sem acoplar o motor ao dispositivo |
| Apresentação | Seleção de modo, lado e dificuldade; estados de pensamento e falha | Alteração no HUD de tela e no futuro painel world-space |
| Build | Seleção, cópia, permissão e verificação do artefato por plataforma | Requer automação e testes separados para macOS, Windows e Android |
| Desempenho | Concorrência entre busca do motor e renderização VR | Risco principal no Quest standalone |
| Testes | Inclusão de EditMode, PlayMode e smokes de integração | Pré-requisito para refatorar o controlador com segurança |
| Licenciamento | Distribuição de Stockfish GPLv3 e avisos de Chess.NET MIT | Exige licença, versão, checksum e fonte correspondente |
| Trabalho paralelo de VR | IA e VR alteram controlador e HUD por motivos diferentes | Requer ordem de integração e contratos acordados antes de edições concorrentes |

#### 8.1 Mapa provável de arquivos

| Local | Conteúdo previsto |
| --- | --- |
| `Assets/Scripts/Domain/` | `ChessMove`, `PositionSnapshot`, modo, lado humano e estado da partida |
| `Assets/Scripts/AI/` | `IMoveChooser`, configurações, coordenador e adaptador programado |
| `Assets/Scripts/AI/Stockfish/` | Sessão UCI, comandos, parser e adaptador de processo |
| `Assets/Scripts/Rules/ChessRulesAdapter.cs` | Fotografia FEN e validação de `ChessMove` |
| `Assets/Scripts/Controllers/ChessGameController.cs` | Orquestração dos participantes e aplicação comum |
| `Assets/Scripts/UI/` | Configuração da partida e feedback do motor |
| `Assets/Plugins/Android/` | Plug-in `.so` somente se aprovado pelo experimento |
| `Assets/StreamingAssets/` ou pós-build | Artefatos executáveis somente após comprovação no alvo |
| `Assets/Tests/EditMode/` | Domínio, UCI, timeout, cancelamento e validação |
| `Assets/Tests/PlayMode/` | Fluxo completo da partida e estados visíveis |

Alterações futuras em gameplay, interação ou apresentação deverão atualizar os
respectivos domínios do DomainBook no mesmo commit.

### 9. Requisitos não funcionais

| ID | Requisito |
| --- | --- |
| RNF-01 | A busca do motor não pode bloquear a thread principal da Unity |
| RNF-02 | O adversário deve funcionar offline durante a partida |
| RNF-03 | Toda busca deve possuir timeout e cancelamento |
| RNF-04 | Respostas de uma revisão antiga da partida devem ser descartadas |
| RNF-05 | Reinício, saída e suspensão devem encerrar ou invalidar a busca ativa |
| RNF-06 | Logs devem distinguir falha de processo, protocolo, timeout, jogada inválida e empacotamento |
| RNF-07 | Somente o artefato correspondente ao alvo pode entrar em cada build |
| RNF-08 | O Quest deve manter o orçamento de quadros, CPU, memória e temperatura definido com a frente de VR |
| RNF-09 | A build distribuída deve identificar versão, arquitetura, checksum e licença do motor |

### 10. Estratégia de testes e evidências

| Camada | Casos obrigatórios | Dependência externa |
| --- | --- | --- |
| EditMode | Conversão de jogada, FEN, revisão, configuração, comandos UCI, parser, timeout e cancelamento | Nenhuma |
| PlayMode | Turno automático, animação, histórico, promoção, xeque-mate, empate, reinício e retorno ao menu | `ScriptedMoveChooser` |
| Integração macOS | Handshake, partida completa, ausência do executável, saída inesperada e encerramento limpo | Stockfish macOS |
| Integração Windows PC-VR | Partida com OpenXR e manutenção do orçamento de quadros | Stockfish Windows e headset |
| Integração Quest | Instalação, partida offline, reinício, suspensão, retomada, falha e encerramento | Artefato Android e Quest 3 |
| Perfil Quest | Latência, frame time, CPU/GPU, memória, temperatura, throttling e bateria | Unity Profiler, MQDH e OVR Metrics Tool |
| Distribuição | Arquitetura correta, checksum, licença e fonte correspondente | Pacote de cada plataforma |

Os testes de gameplay usarão a interface pública de escolha de jogada. Testes
que dependam de detalhes internos do processo UCI não substituirão os testes de
comportamento da partida.

### 11. Plano de implementação

| Fase | Entrega | Dependências | Portão de saída |
| --- | --- | --- | --- |
| 0. Decisões | Aprovação do motor, licença, escopo, dificuldades e orçamento VR | Decisões D-01 a D-09 | Decisões registradas e responsáveis definidos |
| 1. Baseline | Testes das regras e do fluxo humano contra humano | Unity atual reproduzível | Modo existente preservado e testes de base passando |
| 2. Contrato comum | `ChessMove`, fotografia, `IMoveChooser`, coordenador e `ScriptedMoveChooser` | Fase 1 | Partida automática completa em PlayMode |
| 3. Integração macOS | Sessão UCI persistente, FEN, `bestmove`, timeout, logs e encerramento | Fase 2 e Stockfish macOS | Partida completa no Editor e na build macOS |
| 4. Experiência e dificuldade | Configuração, perfis, pensamento, erros e recuperação | Fase 3 | Perfis distintos e falhas sem corrupção de estado |
| 5. Windows PC-VR | Artefato Windows, build OpenXR e teste em headset | Plano de VR e suporte Windows | Partida PC-VR dentro do orçamento aprovado |
| 6. Experimento Quest | Build ARM64/IL2CPP e comparação entre processo e plug-in | Android Build Support e Quest 3 | Evidência técnica e decisão do adaptador |
| 7. Quest produtivo | Adaptador escolhido, matriz de testes e pacote de licença | Fase 6 | Partida offline no Quest com desempenho aprovado |

As fases 1 a 4 formam o incremento funcional de desktop. As fases 5 a 7
promovem a mesma arquitetura para VR e standalone, sem criar uma segunda lógica
de partida.

### 12. Registro de riscos

| ID | Risco | Impacto | Mitigação e portão |
| --- | --- | --- | --- |
| R-01 | Refatoração quebrar o modo local | Alto | Testes de baseline antes da interface de IA |
| R-02 | Resposta atrasada alterar uma nova partida | Alto | Revisão imutável e cancelamento obrigatório |
| R-03 | Processo travar ou produzir saída inválida | Médio | Timeout, parser defensivo e estado recuperável |
| R-04 | Executável desktop ser incluído no alvo errado | Alto | Seleção de artefato e inspeção automatizada da build |
| R-05 | Processo UCI não ser viável no Horizon OS | Alto | Comparação com plug-in nativo no experimento Quest |
| R-06 | Motor consumir o orçamento de CPU e temperatura do VR | Alto | `Threads=1`, hash limitado, sem ponder e perfil em hardware |
| R-07 | Dificuldade não corresponder à experiência esperada | Médio | Calibração com partidas e critérios observáveis |
| R-08 | Distribuição descumprir GPLv3 | Alto | Aprovação da licença antes de incluir binários |
| R-09 | IA e VR produzirem refatorações incompatíveis | Alto | Contratos comuns e ordem de integração aprovados |
| R-10 | Dependência Chess.NET descontinuada limitar evolução | Médio | Manter o adaptador e registrar migração como trabalho separado |

### 13. Decisões já estabelecidas

- Meta Quest 3 standalone é o alvo final.
- Editor/macOS e Windows PC-VR são etapas intermediárias de validação.
- A solução deve funcionar offline.
- Não será treinado um novo modelo.
- Dificuldade será apresentada por perfis compreensíveis.
- As regras locais continuarão sendo a autoridade sobre a partida.
- Movimentos humanos e automáticos compartilharão validação e apresentação.
- A integração definitiva do Quest dependerá de experimento no hardware.

## Rule: o modo contra computador pode ser configurado

```gherkin
Example: iniciar uma partida humano contra computador
  Given que a tela inicial está aberta
  When o jogador escolhe o modo contra computador, um lado e uma dificuldade
  Then uma nova partida começa com essas configurações
  And o modo humano contra humano continua disponível

Example: atribuir exatamente um lado ao computador
  Given que o jogador escolheu as peças brancas
  When chega o turno das peças pretas
  Then a entrada de jogada humana fica bloqueada
  And o adversário recebe a posição das peças pretas
```

## Rule: a jogada automática usa as regras e o fluxo visual existentes

```gherkin
Example: aplicar uma resposta legal
  Given que é o turno do computador
  And o motor devolveu uma candidata para a revisão atual
  When as regras validam a candidata
  Then a peça usa a animação existente
  And histórico, status, turno e término são atualizados normalmente

Example: rejeitar uma resposta inválida
  Given que a resposta é malformada, antiga, fora do turno ou ilegal
  When o coordenador recebe a resposta
  Then a partida permanece inalterada
  And o HUD apresenta uma falha recuperável
```

## Rule: a busca mantém a aplicação responsiva

```gherkin
Example: continuar renderizando durante a busca
  Given que o computador está escolhendo uma jogada
  When a Unity processa quadros, interface e rastreamento
  Then a thread principal permanece responsiva
  And o HUD apresenta o estado de pensamento

Example: cancelar uma busca antiga
  Given que existe uma busca em andamento
  When uma nova partida começa ou a cena é encerrada
  Then a busca antiga é cancelada
  And qualquer resposta atrasada é descartada pela revisão
```

## Rule: a dificuldade altera o desafio sem alterar as regras

```gherkin
Example: aplicar um perfil de dificuldade
  Given que um nível de dificuldade foi selecionado
  When a sessão do motor é configurada
  Then a força e o orçamento de busca daquele perfil são aplicados
  And toda candidata continua sujeita às regras locais

Example: tratar o limite de tempo
  Given que a busca ultrapassou o tempo máximo
  When o timeout é atingido
  Then a partida permanece válida e responsiva
  And o HUD oferece as ações de recuperação aprovadas
```

## Rule: cada plataforma usa um artefato compatível

```gherkin
Example: reutilizar o contrato em macOS e Windows
  Given que o artefato correspondente ao alvo foi empacotado
  When uma partida contra o computador é executada
  Then a mesma fotografia e o mesmo contrato de jogada são usados
  And detalhes do processo não entram no controlador da partida

Example: executar standalone no Quest
  Given que a build Android ARM64 está instalada no Meta Quest 3
  When uma partida completa é jogada sem conexão com um PC
  Then o adversário funciona offline no headset
  And a aplicação mantém o orçamento de desempenho aprovado
```

## Open Questions

As decisões abaixo devem ser respondidas antes da fase indicada:

| ID | Decisão requerida | Alternativas ou informação necessária | Recomendação atual | Bloqueia |
| --- | --- | --- | --- | --- |
| D-01 | Adotar Stockfish 18? | Stockfish ou outro motor UCI compatível | Adotar Stockfish 18 | Fase 3 |
| D-02 | Aceitar as obrigações da GPLv3? | Distribuir licença e fonte correspondente ou escolher outra dependência | Aceitar somente com processo de distribuição definido | Inclusão de qualquer binário |
| D-03 | Quantos níveis serão oferecidos? | Dois, três ou mais perfis | Iniciante, Intermediário e Difícil | Fase 4 |
| D-04 | Qual o tempo máximo por jogada? | Limite único ou limite por dificuldade | Definir por perfil após calibração | Fase 4 |
| D-05 | O jogador poderá escolher o lado? | Lado fixo, escolha manual ou aleatória | Permitir escolha; lado fixo no primeiro incremento é aceitável | UI da fase 4 |
| D-06 | Qual a recuperação para falha do motor? | Tentar novamente, reiniciar, voltar ao menu ou jogada de contingência | Não aplicar jogada automática de contingência sem validação | Fase 4 |
| D-07 | Qual o orçamento de desempenho no Quest? | Taxa de atualização, frame time, CPU, memória e temperatura | Definir em conjunto com a frente de VR | Fases 5 e 6 |
| D-08 | Processo UCI ou plug-in nativo no Quest? | Resultado do experimento de empacotamento, desempenho e licença | Adiar a decisão até a fase 6 | Fase 7 |
| D-09 | Qual a ordem de integração entre IA e VR? | IA primeiro, VR primeiro ou contratos comuns antes do trabalho paralelo | Implementar o contrato comum e o adversário programado antes das alterações concorrentes | Início das fases de código |

### Referências

Fontes oficiais usadas no estudo:

- Código-fonte e licença GPLv3 do Stockfish — https://github.com/official-stockfish/Stockfish
- Integração e distribuição do Stockfish — https://official-stockfish.github.io/docs/stockfish-wiki/Developers.html
- Controles de dificuldade do Stockfish — https://official-stockfish.github.io/docs/stockfish-wiki/Stockfish-FAQ.html
- Avaliação NNUE do Stockfish — https://official-stockfish.github.io/docs/stockfish-wiki/Advanced-topics.html
- Compilação do Stockfish para macOS e Android — https://official-stockfish.github.io/docs/stockfish-wiki/Compiling-from-source.html
- Dependências Android da Unity 6.3 — https://docs.unity3d.com/6000.3/Documentation/Manual/android-install-dependencies.html
- Plug-ins nativos Android na Unity 6.3 — https://docs.unity3d.com/6000.3/Documentation/Manual/AndroidNativePlugins.html
- ABI ARM64 do Android — https://developer.android.com/ndk/guides/abis
- Análise de desempenho Unity no Meta Quest — https://developers.meta.com/horizon/documentation/unity/unity-perf/
- OVR Metrics Tool — https://developers.meta.com/horizon/documentation/unity/ts-ovrmetricstool/
- Código-fonte e licença MIT do Chess.NET — https://github.com/thomas-daniels/Chess.NET
