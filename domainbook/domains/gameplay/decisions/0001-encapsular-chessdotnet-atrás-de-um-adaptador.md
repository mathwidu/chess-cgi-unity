---
status: accepted
date: 2026-05-30
---

# Encapsular ChessDotNet atrás de um adaptador

## Context and Problem Statement

O jogo precisa de regras de xadrez corretas — jogadas legais, xeque,
xeque-mate, roque, en passant, promoção e empates. Escrever e testar isso do
zero é um trabalho grande, e não é sobre isso que trata a disciplina, que é
computação gráfica. Ao mesmo tempo, uma biblioteca de regras de terceiros traz
seus próprios tipos (`ChessGame`, `Piece`, `Position`, `Player`), e deixar
esses tipos se espalharem pelo tabuleiro, pelas peças, pela entrada e pelo HUD
prenderia cada parte do jogo a uma única biblioteca.

## Decision Drivers

- As regras precisam ser corretas sem gastar o tempo do projeto com um motor
  de regras.
- O código de gráficos e interação deve falar o vocabulário do próprio jogo,
  não o de uma biblioteca.
- Trocar ou atualizar a biblioteca de regras deve tocar um único arquivo, não
  o código todo.

## Considered Options

- Escrever as regras de xadrez à mão, nos próprios tipos do jogo.
- Usar a biblioteca `ChessDotNet` diretamente onde quer que as regras sejam
  necessárias.
- Usar `ChessDotNet` por meio de um único adaptador que expõe os próprios
  tipos do jogo.

## Decision Outcome

Opção escolhida: "Usar `ChessDotNet` por meio de um único adaptador".
`ChessRulesAdapter` é o único código que referencia a biblioteca; ele traduz
entre `ChessGame`/`Piece`/`Position`/`Player` da biblioteca e os próprios
`BoardSquare`, `ChessSide`, `ChessPieceKind`, `VisualPieceState` e
`MoveResult` do jogo. Todo o resto — o controlador, as views, a entrada — vê
apenas os tipos do jogo.

### Consequences

- Bom, porque as regras ficam corretas de graça e o projeto gasta seu esforço
  em gráficos.
- Bom, porque o resto do código é independente da biblioteca; ela poderia ser
  substituída editando um único adaptador.
- Bom, porque o vocabulário do jogo é consistente, o que também é o que o
  livro documenta.
- Ruim, porque o adaptador precisa ser mantido em sincronia com a API da
  biblioteca, e um bug da biblioteca é herdado em vez de corrigível no local.
