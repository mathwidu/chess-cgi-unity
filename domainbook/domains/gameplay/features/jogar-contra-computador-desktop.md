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
  And o motor interrompe a busca em andamento

Example: falha recuperável
  Given que o motor falhou ou o prazo expirou
  When a busca falha
  Then a posição é preservada e a entrada humana continua bloqueada nesse turno
  And posso tentar novamente com uma sessão nova ou voltar ao menu
```

### Limites da entrega

O motor é o MadChess v3.4 embutido no jogo ([decisão 0002](../decisions/0002-embutir-o-madchess-como-motor-de-xadrez.md)),
com hash de 16 MiB e uma busca por vez. Cada perfil usa um
[Elo de força limitada](../glossary.md) e um tempo por jogada: Iniciante
600/150 ms, Intermediário 1200/500 ms, Difícil 2000/1200 ms. Os prazos totais
continuam 6000/6500/7200 ms. O Elo segue a escala do autor do motor; a
calibração com jogadores continua aberta.

O adaptador UCI do Stockfish continua no repositório, com suas validações, mas
a fábrica não o usa mais. Windows, PC-VR e Quest exigem validações próprias.

## Open Questions

- Calibrar dificuldade com jogadores.
- Incluir a licença MIT do MadChess nos créditos antes de entregar o aplicativo a terceiros.
- Medir no Quest real o frame time, a CPU e as coletas de lixo do motor embutido.
