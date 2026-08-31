---
id: render-the-board
name: Renderizar o tabuleiro e as peças
status: ready
owners: [mathwidu]
terms: [peça-personalizada, peça-clássica, destaque]
decisions: [presentation/ADR-0001]
---

## Story

Como jogador assistindo a partida
Quero o tabuleiro e cada peça desenhados e sempre atualizados
Para que o que eu vejo seja sempre a posição em que a partida está

## Rule: O tabuleiro e as peças são construídos a partir do relato do gameplay sobre a posição

```gherkin
Example: Uma nova partida constrói o tabuleiro completo e as peças iniciais
  Given uma nova partida começou
  When o tabuleiro é construído
  Then existem 64 casas no padrão claro-e-escuro
  And toda peça que o gameplay relata está em sua casa

Example: As peças são reconstruídas para corresponder após uma jogada
  Given uma jogada foi realizada
  When o tabuleiro sincroniza com a nova posição
  Then as peças mostradas correspondem exatamente à lista do gameplay
  And nada permanece em uma casa que o gameplay deixou vazia
```

## Rule: Um tipo de peça sem modelo customizado ainda mostra uma peça

```gherkin
Example: Um modelo customizado é usado quando há um definido para o tipo
  Given um modelo customizado está definido para o bispo
  When um bispo é desenhado
  Then ele usa esse modelo, escalado para caber no tabuleiro

Example: Um modelo ausente recai para uma forma primitiva
  Given nenhum modelo customizado está definido para a torre
  When uma torre é desenhada
  Then ela é construída a partir de formas primitivas na cor do lado
  And a casa nunca fica vazia
```

## Rule: As casas legais da peça selecionada são marcadas

```gherkin
Example: Destaques marcam os destinos legais
  Given uma peça está selecionada com três destinos legais
  When o tabuleiro os destaca
  Then três marcadores de destaque ficam nessas casas
  And limpar a seleção os remove
```

## Open Questions

Nenhuma.
