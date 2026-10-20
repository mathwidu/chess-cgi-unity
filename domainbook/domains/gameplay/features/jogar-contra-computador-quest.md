---
id: jogar-contra-computador-quest
name: Jogar contra o computador offline no Quest
status: ready
owners: [mathwidu]
terms: [jogada, turno, adversário-controlado-pelo-computador, nível-de-dificuldade, fotografia-da-posição]
---

## Story

Como jogador no Quest 3 standalone
Quero escolher lado e dificuldade para jogar contra a IA sem internet
Para usar o mesmo jogo e cenário sem depender de um computador conectado

## Rule: motor e regras têm responsabilidades distintas

```gherkin
Example: responder a uma jogada humana
  Given que o APK contém o motor offline e suas redes de avaliação
  And o jogador iniciou Contra IA
  When chega o turno do computador
  Then o motor recebe a fotografia da posição e o histórico
  And a candidata só é aplicada se corresponder ao turno e à revisão atuais
  And as regras locais precisam aceitar a jogada
```

## Rule: cancelamento preserva a partida e a interação

```gherkin
Example: suspender enquanto o computador pensa
  Given uma busca em andamento
  When o aplicativo é suspenso, reiniciado ou retorna ao menu
  Then a busca perde validade e o processo da sessão é encerrado
  And a thread de interação não aguarda o encerramento
  And uma retomada pode iniciar uma nova sessão

Example: motor ausente, encerrado ou resposta malformada
  Given uma busca que falha
  When o controlador recebe o erro
  Then nenhuma jogada de contingência é aplicada
  And o jogador pode tentar novamente ou voltar ao menu
```

### Limites da entrega

O candidato usa Stockfish 18 ARM64 PIE por processo UCI no Android e preserva
os perfis existentes. O preparo é reproduzível e inclui GPL/fonte correspondente.
Os testes do adaptador e do transporte no Mac não comprovam execução nem
desempenho no Horizon OS. Esta funcionalidade permanece em andamento até a
partida offline e o ciclo de vida serem verificados no aparelho.

Veja a [decisão do transporte](../decisions/0002-usar-processo-uci-local-no-candidato-android.md) e
o [guia de implementação e evidências](../../../../docs/quest3-offline-ai.md).

## Open Questions

- Confirmar execução do motor sob o UID do aplicativo no Horizon OS instalado.
- Medir custo de inicialização, memória, CPU e quadros durante uma partida VR.
