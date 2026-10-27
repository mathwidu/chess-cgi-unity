---
id: retomar-partida-no-navegador
name: Retomar uma partida salva no navegador
status: ready
owners: [mathwidu]
terms: [partida-salva, jogada, captura, promoção, resultado-da-partida, fotografia-da-posição, adversário-controlado-pelo-computador]
---

## Story

Como jogador no navegador
Quero continuar minha última partida local após sair ou recarregar a página
Para preservar as jogadas e a configuração do adversário sem criar uma conta

## Rule: a partida é salva depois da aceitação lógica da jogada

```gherkin
Example: recarregar durante uma animação
  Given que o jogador iniciou uma partida nova explicitamente
  When as regras aceitam uma jogada legal
  Then o histórico UCI completo é salvo antes da animação dessa jogada
  And retomar coloca a peça diretamente no destino aceito
  And o histórico, as capturas e o resultado incluem essa jogada

Example: abrir o jogo com um save existente
  Given que existe uma partida salva no navegador
  When o controlador inicializa o tabuleiro e retorna ao menu automaticamente
  Then essa inicialização não sobrescreve o save
  And SaveNow não salva um tabuleiro que ainda não foi iniciado pelo jogador
  And iniciar uma partida nova explicitamente substitui o save anterior
```

## Rule: uma retomada é validada por inteiro antes de alterar a partida viva

```gherkin
Example: reconstruir a partida
  Given uma partida salva da versão 1 com configurações reconhecidas
  When cada jogada UCI é repetida a partir da posição inicial padrão
  Then cada jogada precisa ser legal e a promoção precisa estar explícita
  And nenhuma jogada pode ocorrer depois de um resultado terminal
  And roque, en passant e histórico de repetição são reconstruídos pelas regras
  And capturas, material, histórico, xeque e resultado são derivados desse replay
  And a partida viva só é substituída depois da última validação

Example: save corrompido ou de outra versão
  Given um save inválido, incompleto, excessivo ou de versão não suportada
  When o jogador tenta continuar
  Then a partida viva e sua busca do computador permanecem intactas
  And o conteúdo salvo não é apagado, migrado nem reescrito automaticamente
  And uma falha visível é enviada ao launcher
```

## Rule: a busca anterior perde validade e o turno restaurado é executado uma vez

```gherkin
Example: retomar enquanto uma busca antiga está pendente
  Given que o computador está pensando na partida viva
  When outra partida salva válida é retomada
  Then a busca antiga é cancelada e seu provedor é descartado
  And uma resposta antiga não altera o tabuleiro restaurado
  And o ciclo de atualização inicia uma única busca nova se o turno restaurado pertence à IA
  And uma partida terminal restaurada não inicia uma busca
  And jogadas históricas não disparam novamente MoveApplied nem animações
```

## Rule: a falha de armazenamento não impede uma partida nova

```gherkin
Example: armazenamento indisponível ou cheio
  Given que o navegador bloqueia localStorage ou que a quota foi atingida
  When o jogador inicia ou joga uma partida
  Then a partida continua funcionando
  And uma falha de salvamento é enviada ao launcher
  And uma escrita recusada preserva o último valor completo que localStorage aceitou
```

## Rule: uma aba desatualizada preserva o save que outra aba alterou

```gherkin
Example: sair de uma aba antiga após jogar em outra
  Given que a aba A leu uma partida salva
  And a aba B gravou outra partida depois dessa leitura
  When a aba A tenta salvar seu estado antigo, inclusive ao sair ou ocultar
  Then a ponte compara o valor bruto atual com o último valor observado pela aba A
  And uma divergência recusa a escrita com save-conflict sem substituir o save da aba B
  And somente uma nova leitura explícita aceita esse valor como referência
  And gravar um JSON já idêntico ao valor atual é um sucesso sem escrita
```

### Contrato do navegador

Somente o runtime Web cria o GameObject `Web Session`, após `ChessGameController.Start`.
O launcher deve instalar seus listeners antes de `createUnityInstance`, aguardar
a instância e `cicochess:session-ready` e então usar
`instance.SendMessage('Web Session', 'ResumeSavedGame')` ou `SaveNow`.
Entrar sem retomar mantém o menu padrão. Uma partida nova é iniciada pelos
controles existentes do jogo; o bootstrap nunca decide apagar uma partida.

Eventos no `window` principal:

- `cicochess:session-ready`: `{ok,hasSave,reason}`. `ok` confirma o controlador
  disponível para receber comandos, mesmo com save inválido ou storage bloqueado.
  `hasSave` indica existência observada; em falha de leitura, pode ser desconhecida
  e aparecer como `false`. `reason` pode descrever uma falha do save/storage.
- `cicochess:save`: `{ok,hasSave,reason}` após uma tentativa de salvar.
- `cicochess:resume`: `{ok,reason}` após uma tentativa de retomar.

Razões são códigos sem conteúdo da partida: `no-save`, `no-active-session`,
`invalid-save`, `unsupported-version`, `save-too-large`, `invalid-move`,
`illegal-move`, `moves-after-result`, `scene-unavailable`, `invalid-session`,
`storage-full`, `storage-unavailable` e `save-conflict`; sucesso usa uma string vazia.

A chave de `localStorage` é `cicochess.match.v1`, com este schema fechado:

```json
{"version":1,"mode":"computer","humanSide":"white","difficulty":"beginner","moves":["e2e4","e7e5"]}
```

`mode` aceita `local`/`computer`, `humanSide` aceita `white`/`black`, e
`difficulty` aceita `beginner`/`intermediate`/`hard`. A versão 1 implica a
posição inicial padrão; não aceita FENs personalizados. O leitor rejeita
campos ausentes, repetidos ou desconhecidos, JSON truncado e conteúdo extra.
Os limites são 65.536 bytes UTF-8 e 2.048 lances. Além do limite, a partida
continua, mas a última gravação válida é mantida e a falha é comunicada.

### Limites da entrega

O save pertence a esta origem, navegador e perfil; não é sincronizado nem
garantido depois de limpeza dos dados do navegador. A escrita usa `setItem`
síncrono e atômico. Antes de escrever, a ponte relê a chave e compara seu valor
bruto com o último valor lido ou escrito com sucesso nesta instância. Ausência
da chave e string vazia são referências diferentes; falhas de quota e conflitos
não avançam essa referência. A comparação é uma proteção otimista contra abas
desatualizadas, sem uma transação ou lock entre abas simultâneas. A política de
empates continua sendo a de `ChessRulesAdapter`;
esta entrega preserva o histórico que ele e o motor recebem, sem acrescentar
uma nova regra de reivindicação de empate. Preferências visuais, câmera, seleção
transitória e animação não fazem parte da partida salva.

As suites `ChessMatchSessionTests` (26 EditMode) e `ChessWebSessionTests`
(10 PlayMode) passaram em 26/10/2026, cobrindo replay, limites, corrupção,
aceitação antes da animação, boot, HUD, falhas do storage e cancelamento da IA.
A ponte passou 15 testes Node. No build Web real foram confirmadas a retomada
após reload de uma captura e da partida contra IA, sem lance duplicado, além
da recusa de escrita de uma aba antiga sobre uma partida nova de outra aba.
As mensagens de corrupção/quota/storage bloqueado têm testes de contrato no
site e da ponte. Evidências e limites estão em `docs/site-pilot.md`.
Nada foi publicado por esta implementação.

## Open Questions

- Confirmar a retomada e persistência em navegadores de aparelhos móveis físicos.
