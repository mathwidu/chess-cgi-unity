---
status: accepted
date: 2026-05-30
---

# Modelos de peça personalizados com alternativa primitiva

## Context and Problem Statement

As peças são personagens customizados modelados pela turma, um por tipo, e
parte do propósito do projeto é mostrá-los. Mas um modelo customizado pode
estar ausente, não atribuído, ou ainda em produção, e um tabuleiro com um
buraco onde deveria haver uma peça é ao mesmo tempo errado para jogar e
constrangedor para demonstrar. O jogo precisa mostrar os personagens
customizados quando existem e continuar jogável quando não existem.

## Decision Drivers

- Os personagens customizados são um destaque do projeto e devem ser
  mostrados.
- O tabuleiro precisa sempre ter um conjunto completo e reconhecível de
  peças para jogar.
- Trabalho em andamento em um modelo não deve bloquear a execução do jogo
  inteiro.

## Considered Options

- Exigir todo modelo customizado antes de o jogo poder rodar.
- Mostrar apenas os modelos customizados, e deixar um tipo em branco quando
  seu modelo estiver ausente.
- Mostrar o modelo customizado por tipo, e construir uma peça a partir de
  formas primitivas quando nenhum modelo estiver definido.

## Decision Outcome

Opção escolhida: "modelo customizado por tipo, com uma alternativa
primitiva". `PieceFactory` usa o prefab atribuído a um tipo e o escala para
caber; quando um tipo não tem prefab, ela monta uma peça a partir de formas
primitivas — uma base, uma haste e uma cabeça que varia por tipo — na cor do
lado. Todo tipo, portanto, é renderizado, qualquer que seja o que esteja
atribuído.

### Consequences

- Bom, porque os personagens customizados aparecem assim que estiverem
  prontos, e o tabuleiro fica completo mesmo quando não estão.
- Bom, porque um modelo pode ser adicionado ou trocado um tipo de cada vez
  sem quebrar o jogo.
- Ruim, porque existem dois caminhos de renderização para peças e ambos
  precisam continuar funcionando.
- Ruim, porque as formas de reserva são simples, então um tabuleiro
  misturando modelos customizados e formas de reserva parece desigual até
  que todo modelo esteja pronto.
