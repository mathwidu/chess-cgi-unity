---
id: sentir-cada-lance
name: Sentir cada lance
status: ready
owners: [mathwidu]
terms: [marca-do-último-lance, rei-em-xeque, sons-do-tabuleiro]
---

## Story

Como jogador, principalmente de óculos VR e contra a IA
Quero ver, ouvir e sentir cada lance e cada xeque
Para acompanhar a partida sem procurar no HUD o que aconteceu

## Rule: O tabuleiro marca o último lance e o rei em xeque

```gherkin
Example: A IA joga e o lance fica marcado
  Given uma partida contra a IA
  When a peça da IA pousa no destino
  Then as casas de origem e de destino ficam tingidas de amarelo suave
  And a marca do lance anterior some

Example: Um xeque tinge o rei de vermelho
  Given um lance deixa o rei adversário em xeque
  Then a casa desse rei fica vermelha
  When o xeque é respondido
  Then o vermelho some

Example: Nova partida
  Given há casas marcadas
  When o jogador começa uma nova partida
  Then nenhuma casa fica marcada
```

## Rule: Cada lance tem um som quando pousa

```gherkin
Example: Lance e captura
  Given uma partida em andamento
  When um lance pousa sem captura
  Then toca um toque de madeira
  When um lance pousa com captura
  Then toca um toque duplo

Example: Xeque, vez e fim de partida
  Given um lance deixa o rei em xeque
  Then logo depois do toque soa um carrilhão
  Given a IA acabou de jogar e a vez é do jogador
  Then soa um sinal suave de vez
  Given o lance termina a partida
  Then soa uma frase de vitória, de derrota contra a IA ou de empate
```

## Rule: No VR, o controle vibra ao agarrar e soltar

```gherkin
Example: Agarrar e jogar
  Given o jogador agarra uma peça da vez
  Then o controle dá um pulso curto e leve
  When ele a solta num destino legal
  Then o controle dá outro pulso curto

Example: Jogada recusada
  When o jogador solta a peça fora dos destinos legais
  Then o controle vibra mais forte e por mais tempo
  And a peça volta à casa de origem
```

As marcas misturam uma tinta à cor da própria casa, então funcionam em casas
claras e escuras. Os sons são sintetizados em código e, no VR, saem do
tabuleiro em 3D. A vibração usa a saída de vibração do OpenXR de cada controle.

## Open Questions

- O volume e o timbre dos sons, e a força da vibração, ainda precisam ser
  ajustados em headset.
- Não há opção de silenciar os sons no menu.
