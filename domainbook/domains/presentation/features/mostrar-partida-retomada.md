---
id: mostrar-partida-retomada
name: Mostrar a partida retomada no HUD
status: ready
owners: [mathwidu]
terms: [hud, marca-do-último-lance, rei-em-xeque, peças-capturadas, tela-de-resultado]
---

## Story

Como jogador que retomou uma partida salva
Quero ver o tabuleiro e o HUD correspondente à posição restaurada
Para continuar jogando ou examinar o resultado sem iniciar outra partida

## Rule: a retomada válida fecha a tela inicial sem iniciar uma partida nova

```gherkin
Example: continuar a última partida
  Given que o HUD está na tela inicial
  When o controlador termina uma retomada validada
  Then ShowResumedMatch fecha a tela inicial e os painéis transitórios
  And o HUD lê turno, histórico, capturas e resultado do controlador restaurado
  And um resultado anteriormente dispensado pode aparecer novamente
  And as miniaturas de capturas são reconstruídas sem animação mesmo com a mesma contagem e material de outra partida
  And nenhuma ação de nova partida é executada
  And o desenho e os controles existentes permanecem os mesmos
```

### Verificação

`ChessWebSessionTests.TerminalMoveSavedDuringAnimationRestoresResultWinnerAndHud`
cobre a retirada da tela inicial e o resultado restaurado; passou na execução
Unity de 26/10/2026. `RepeatedResumeWithSameCountAndMaterialRefreshesCapturedDisplayWithoutFlight`
cobre a invalidação explícita das miniaturas, sem reutilizar o cache de outra
partida nem animar capturas históricas e também passou. No navegador real, o
piloto restaurou uma captura, seus três lances e a vez das pretas, sem menu
encobrindo a partida. O viewport 390×844 manteve tabuleiro e controles acessíveis.
Dispositivo móvel físico permanece uma verificação separada.

## Open Questions

- Confirmar tela de resultado e fechamento dos painéis transitórios no Web Player.
