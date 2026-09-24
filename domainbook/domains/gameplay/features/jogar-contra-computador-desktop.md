---
id: jogar-contra-computador-desktop
name: Jogar contra o computador no desktop
status: ready
owners: [mathwidu]
terms: [jogada, turno, promoção, adversário-controlado-pelo-computador, nível-de-dificuldade, estado-de-pensamento, fotografia-da-posição]
---

## Story

Como jogador no desktop
Quero escolher meu lado e um adversário offline
Para jogar sozinho usando as mesmas regras, animações e histórico do modo local

## Rule: a configuração determina os participantes

```gherkin
Example: jogar com as pretas
  Given que escolhi Contra IA, Pretas e Iniciante
  When inicio a partida
  Then o computador faz a primeira jogada com as brancas
  And só posso selecionar minhas peças no turno das pretas
  And a câmera desktop permanece na perspectiva das pretas

Example: preservar as opções ao reiniciar
  Given uma partida contra o computador
  When inicio Nova partida ou pressiono N fora do menu
  Then a posição volta ao início com o mesmo lado e dificuldade
  And nenhuma resposta ou animação anterior altera a partida nova
```

## Rule: o motor fornece uma candidata e as regras continuam soberanas

```gherkin
Example: jogada legal do computador
  Given uma resposta para a revisão e o lado atuais
  When as regras locais aceitam a candidata
  Then a jogada usa a mesma animação, histórico e resultado de uma jogada humana
  And promoção, roque, en passant, xeque, mate e empate usam as regras existentes

Example: resposta ilegal ou antiga
  Given uma candidata ilegal ou produzida antes de reiniciar
  When ela chega ao jogo
  Then a posição não é alterada
  And uma resposta antiga é descartada
  And uma resposta ilegal apresenta a opção de tentar novamente ou voltar ao menu
```

## Rule: pensar não bloqueia a aplicação

```gherkin
Example: busca interrompida
  Given que a IA está pensando
  When volto ao menu, desativo o controlador, suspendo a aplicação ou reinicio
  Then a busca é cancelada e sua resposta perde validade
  And o processo filho pertencente à sessão é encerrado

Example: falha recuperável
  Given que falta o executável, o processo encerrou ou o prazo expirou
  When a busca falha
  Then a posição é preservada e a entrada humana continua bloqueada nesse turno
  And posso tentar novamente com uma sessão nova ou voltar ao menu
```

### Limites da entrega

O transporte usa um processo UCI persistente, com Threads=1, Hash=16 MiB e
Ponder=false. Cada perfil usa somente Skill Level, com UCI_LimitStrength=false:
Iniciante 0/150 ms, Intermediário 6/500 ms, Difícil 14/1200 ms. Os prazos totais
são 6000/6500/7200 ms e incluem inicialização. Os nomes não prometem um Elo;
a calibração com jogadores continua aberta.

O executável externo é preparado localmente, com versão, SHA-256, fonte e
licença. Não é incluído em Assets nem automaticamente redistribuído com a build.
Windows, PC-VR e Quest exigem validações próprias. O adaptador Android ainda
não existe. O guia operacional registra a evidência obtida nesta entrega.

## Open Questions

- Calibrar dificuldade com jogadores.
- Aprovar o pacote de redistribuição do motor antes de entregar o aplicativo a terceiros.
- Medir o orçamento de desempenho e escolher o adaptador no Quest real.
