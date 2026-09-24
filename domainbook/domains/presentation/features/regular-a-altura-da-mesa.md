---
id: regular-a-altura-da-mesa
name: Regular a altura da mesa
status: ready
owners: [mathwidu]
terms: [mesa]
decisions: [presentation/ADR-0002]
---

## Story

Como jogador sentado diante do tabuleiro, de óculos VR ou no desktop
Quero subir ou descer a mesa com o tabuleiro em cima
Para ver melhor as peças e jogar numa altura confortável

## Rule: Os botões da mesa movem só a mesa e o tabuleiro

```gherkin
Example: Subir a mesa
  Given uma partida em andamento com a mesa na altura padrão
  When o jogador aciona Subir na placa da mesa
  Then o tampo sobe um passo e o tabuleiro sobe junto, com todas as peças
  And os pés continuam no chão, e as pernas crescem
  And a câmera não se move
  And a placa mostra a nova altura em centímetros e um nível a mais aceso

Example: Descer a mesa
  Given a mesa está acima do limite mínimo
  When o jogador aciona Descer
  Then a mesa e o tabuleiro descem um passo
```

## Rule: A altura tem limites e é lembrada

```gherkin
Example: Chegar ao limite máximo
  Given a mesa está um passo abaixo do máximo
  When o jogador aciona Subir
  Then a mesa chega ao máximo
  And o botão Subir fica indisponível, e Descer continua disponível

Example: A altura sobrevive a uma nova partida e a uma nova sessão
  Given o jogador regulou a mesa
  When ele inicia uma nova partida, ou fecha e abre o jogo
  Then o tabuleiro é montado sobre a mesma altura
```

## Rule: A placa fica ao alcance do jogador

```gherkin
Example: A placa acompanha o lado de quem joga
  Given o jogador joga de brancas
  Then a placa fica no tampo, à esquerda do tabuleiro, inclinada para ele
  When o jogador passa a jogar de pretas — o assento VR gira contra a IA
    ou a câmera desktop vira para o turno das pretas
  Then a placa passa para a esquerda desse jogador

Example: A placa responde ao mouse e ao raio do controle
  Given o modo desktop
  Then os botões respondem ao clique do mouse
  Given o modo VR
  Then os botões respondem ao raio do controle com o gatilho
```

No VR, o passo é de 3 cm, entre 65 e 95 cm de altura do tampo, e a altura
padrão é 77 cm. No desktop, a mesma sala aparece escalada
([ADR-0002](../decisions/0002-modelar-a-sala-em-metros-do-vr-e-escalar-para-o-desktop.md))
e o passo é de 0,25 unidade do tabuleiro, para o tabuleiro não sair do
enquadramento. A escolha fica em `PlayerPrefs` (`ChessCgi.TableHeightStep`).

## Open Questions

O conforto dos limites e do passo ainda precisa ser validado em headset, assim
como o toque direto na placa com o rastreamento de mãos.
