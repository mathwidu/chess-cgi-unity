---
id: redimensionar-o-tabuleiro-no-vr
name: Redimensionar o tabuleiro no VR
status: ready
owners: [mathwidu]
terms: [tamanho-do-tabuleiro-no-vr, mesa, destaque]
decisions: [presentation/ADR-0002]
---

## Story

Como jogador de óculos VR
Quero ver tabuleiro e peças maiores ou menores sobre a mesa
Para escolher um tamanho confortável sem mudar a sala ou a partida

## Rule: O tamanho muda junto, dentro de uma faixa limitada

```gherkin
Example: Aumentar ou diminuir durante o gesto
  Given um gesto válido nas duas hastes laterais
  When o jogador modifica o tamanho
  Then moldura, casas, peças, colliders e destaques mudam juntos no mesmo quadro
  And o tamanho fica entre 75% e 150% do original
  And a partida mantém suas casas, peças, vez e regras

Example: Um valor inválido não altera o tabuleiro
  Given um tamanho válido
  When o ajuste recebe um valor não finito
  Then a pose e a escala do tabuleiro continuam válidas
```

O tamanho escolhido vale durante a sessão e sobrevive a novas partidas e
reconexões do headset. Uma nova sessão começa em 100%. No desktop continua
valendo a escala original, e as hastes ficam ocultas.

## Rule: A base continua apoiada na mesa

```gherkin
Example: Aumentar sobre a mesa regulada
  Given a mesa foi elevada ou abaixada
  When o tabuleiro aumenta ou diminui
  Then a parte inferior da moldura permanece no tampo
  And sala, tampo, pés e câmera mantêm tamanho e pose
  And regular a altura depois conserva o tamanho escolhido
```

`BoardView` mede os limites locais da moldura e compensa o crescimento
vertical para manter sua base apoiada. `ScenePolish` usa a pose de referência
sem o multiplicador pessoal nem a compensação de altura; a sala e a reflexão
continuam em metros. As peças capturadas e o indicador de turno acompanham o
tabuleiro. O retorno de uma peça a sua casa interpola no espaço do pai, para
acompanhar ajustes de tamanho ou altura sem terminar em uma posição antiga.

As hastes metálicas ficam nos cantos laterais da borda voltada ao jogador,
fora das casas e das faixas de peças capturadas. As pegas conservam diâmetro,
altura sobre o tampo e distância frontal durante o gesto; conexões metálicas
telescópicas acompanham a moldura. Assim, crescer o tabuleiro não empurra as
mãos para frente nem exige mudar a altura da pega. O gesto é descrito em
[Ajustar o tabuleiro com duas mãos](../../interaction/features/ajustar-o-tabuleiro-com-duas-maos.md).

## Rule: Redimensionar preserva a física de uma peça solta

```gherkin
Example: A peça arremessada volta à casa após o atraso mesmo com outro tamanho
  Given uma peça foi solta em um destino inválido no VR
  When o jogador redimensiona o tabuleiro enquanto ela está sob a física
  Then nenhuma jogada é registrada
  And o arremesso continua disponível
  And depois de 3 segundos a peça retorna à casa lógica no tamanho atual
  And a peça volta à orientação vertical
```

## Open Questions

A faixa inicial de tamanho e o alcance das hastes precisam de avaliação em
headset real; a validação local com dispositivos simulados não mede conforto.
