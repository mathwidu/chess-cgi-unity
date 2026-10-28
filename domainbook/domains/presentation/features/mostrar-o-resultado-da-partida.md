---
id: mostrar-o-resultado-da-partida
name: Mostrar o resultado da partida
status: ready
owners: [mathwidu]
terms: [tela-de-resultado, hud]
---

## Story

Como jogador que acabou de terminar uma partida
Quero ver claramente quem venceu, ou por que empatou
Para que eu decida jogar de novo, estudar o tabuleiro final ou voltar ao menu

## Rule: O fim da partida abre a tela de resultado depois do lance final

```gherkin
Example: Um xeque-mate numa partida local nomeia o lado vencedor
  Given uma partida entre dois jogadores
  When Pretas dão xeque-mate
  Then a peça do lance final termina de se mover no tabuleiro
  And logo depois a tela de resultado aparece com "XEQUE-MATE" e "Pretas vencem"
  And ela informa a quantidade de lances e o lance final
  And o painel de turno mostra "Pretas vencem" e "Xeque-mate. Partida encerrada."

Example: Vencer a IA parabeniza o jogador
  Given uma partida contra a IA no nível Intermediário, jogando de brancas
  When o jogador dá xeque-mate
  Then a tela de resultado mostra "Você venceu!" com destaque amarelo
  And a mensagem cita o nível Intermediário

Example: Perder para a IA não usa o destaque de vitória
  Given uma partida contra a IA
  When a IA dá xeque-mate
  Then a tela de resultado mostra "A IA venceu" em tom neutro
  And sugere jogar de novo ou escolher outra dificuldade

Example: Um empate nomeia o motivo
  Given uma partida em andamento
  When a posição termina afogada
  Then a tela de resultado mostra "EMPATE POR AFOGAMENTO" e "Empate"
```

## Rule: A tela de resultado concentra a interação e oferece os próximos passos

```gherkin
Example: A tela recebe o foco e bloqueia as ações comuns
  Given a tela de resultado está aberta
  Then o foco de navegação está em Jogar novamente
  And os botões da barra de ações ficam indisponíveis

Example: Jogar novamente mantém a configuração
  Given a tela de resultado está aberta numa partida contra a IA
  When o jogador escolhe Jogar novamente
  Then uma nova partida começa com o mesmo modo, lado e dificuldade
  And a tela de resultado some

Example: Ver o tabuleiro final e voltar ao resultado
  Given a tela de resultado está aberta
  When o jogador escolhe Ver tabuleiro
  Then a tela some e o tabuleiro final fica visível, sem aceitar jogadas
  And o botão Cancelar da barra de ações passa a se chamar Resultado
  When o jogador escolhe Resultado
  Then a tela de resultado volta

Example: Voltar ao menu
  Given a tela de resultado está aberta
  When o jogador escolhe Voltar ao menu
  Then o menu abre com as escolhas da sessão preservadas
```

A tela é construída em `GameHud.Match.cs`, ao lado dos diálogos de promoção
e de falha da IA, e lê `ChessGameController.Outcome` e `Winner` a cada
atualização do HUD; ela não guarda o resultado. Por ser uGUI no mesmo Canvas,
funciona no desktop e no Canvas world-space do VR, onde o raio do controle
aciona os botões.

## Open Questions

A legibilidade da tela no headset ainda exige validação em hardware.
