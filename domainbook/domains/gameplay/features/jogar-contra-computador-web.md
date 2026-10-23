---
id: jogar-contra-computador-web
name: Jogar contra o computador local no navegador
status: ready
owners: [mathwidu]
terms: [jogada, turno, adversário-controlado-pelo-computador, nível-de-dificuldade, fotografia-da-posição, motor-de-xadrez]
---

## Story

Como jogador no navegador
Quero escolher lado e dificuldade para enfrentar um motor local
Para jogar no Web Player sem um serviço remoto de IA

## Rule: o motor fornece uma candidata e as regras decidem a jogada

```gherkin
Example: responder ao turno humano
  Given que o Web Player tem Stockfish 18 lite single-thread servido junto da página
  And o jogador iniciou Contra IA e escolheu seu lado e dificuldade
  When chega o turno do adversário controlado pelo computador
  Then um Web Worker recebe a fotografia da posição e seu histórico
  And o motor devolve somente uma candidata identificada pelo pedido
  And o turno e a revisão atuais precisam corresponder à fotografia
  And as regras locais precisam aceitar a jogada antes de alterar a partida
```

## Rule: o carregamento tem prazo próprio e a interação continua disponível

```gherkin
Example: primeira busca
  Given que o motor ainda precisa carregar e compilar seu WASM local
  When a busca começa
  Then o carregamento e o handshake têm até 60 segundos
  And o prazo do nível de dificuldade começa quando o motor inicia a busca
  And a verificação de conclusão e prazos ocorre no ciclo de atualização do jogo
  And a busca não bloqueia a thread de renderização com trabalho do motor

Example: motor ausente, prazo excedido ou candidata inválida
  Given que o motor não consegue concluir uma busca válida
  When a falha é observada no ciclo de atualização
  Then a partida não recebe uma jogada de contingência
  And o jogador pode tentar novamente ou voltar ao menu
```

## Rule: respostas de sessões anteriores perdem a validade

```gherkin
Example: reiniciar ou retornar ao menu durante o pensamento
  Given que uma busca está pendente
  When a partida é reiniciada, suspensa, desativada ou retorna ao menu
  Then o pedido é cancelado e seu Worker é encerrado
  And mensagens antigas não completam uma nova busca
  And a próxima sessão pode iniciar outro Worker
```

### Limites da entrega

O protótipo usa artefatos oficiais de `stockfish@18.0.0`, com SHA-256, GPLv3,
créditos e fonte correspondente incluídos no preparo local. A variante lite
single-thread não depende de COOP/COEP e usa rede de avaliação menor que o motor
desktop. Os perfis existentes não são Elo calibrado. O transporte Android e o
desktop continuam em seus caminhos próprios.

Testes Python e do protocolo JavaScript, incluindo respostas do WASM real no
Node, foram executados. Testes EditMode, build IL2CPP e partidas com Worker real
em HTTP localhost precisam de validação integrada pelo principal. O preparo ou
os testes de protocolo não constituem aceitação do Web Player.

Veja [implementação, preparo e evidências](../../../../docs/web-ai.md).

## Open Questions

- Confirmar ambos os lados, dificuldades e ciclo de vida no Web Player integrado.
- Medir carregamento, memória e responsividade nos navegadores/dispositivos alvo.
