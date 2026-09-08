---
id: turn-the-camera
name: Girar a câmera
status: ready
owners: [mathwidu]
terms: [perspectiva, órbita]
---

## Story

Como um dos dois jogadores compartilhando uma tela
Quero que a câmera fique voltada para o meu lado quando for a minha vez
Para que eu leia o tabuleiro a partir do meu próprio lado sem mudar de lugar

## Rule: A câmera fica voltada para o jogador a jogar quando o turno muda

```gherkin
Example: A visão gira para o lado a jogar
  Given passa a ser a vez das Pretas
  When a mudança de turno é aplicada
  Then a câmera termina voltada para o tabuleiro a partir do lado das Pretas

Example: Uma nova partida define a visão de imediato para o primeiro jogador
  Given uma nova partida começa com as Brancas a jogar
  When o tabuleiro é montado
  Then a câmera já está voltada para o lado das Brancas, sem transição
```

## Rule: O jogador pode orbitar e dar zoom sem mudar de quem é o turno

```gherkin
Example: Q e E orbitam a câmera ao redor do tabuleiro
  Given o jogo está em andamento
  When o jogador segura Q ou E
  Then a câmera gira ao redor do tabuleiro
  And o turno não muda

Example: A roda dá zoom entre um limite próximo e um distante
  Given o jogo está em andamento
  When o jogador rola a roda
  Then a câmera se move para mais perto ou mais longe, travada entre seus limites
```

## Open Questions

Nenhuma.
