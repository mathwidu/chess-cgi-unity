# Glossário de apresentação

As palavras que o contexto de apresentação usa para o que o jogador vê. Um
termo é um heading H2 com sua definição logo abaixo.

## Peça personalizada

O modelo de personagem mostrado para um tipo de peça, um por tipo, inspirado
na turma. Carregado como um prefab e escalado para caber no tabuleiro.

- **Aliases:** Custom piece, Custom character
- **Status:** validated
- **Example:** o peão é o modelo "Mathwidu ruivo"; o bispo é "Rafael".

## Peça clássica

A peça que uma fábrica constrói a partir de formas primitivas — cilindros,
esferas, cubos — quando nenhum modelo customizado está definido para um
tipo, para que o tabuleiro nunca fique sem uma peça.

- **Aliases:** Primitive fallback
- **Status:** validated

## Destaque

Um marcador que o tabuleiro coloca em cada destino legal da peça
selecionada.

- **Aliases:** Highlight
- **Status:** validated

## Preview da peça selecionada

O painel que mostra a peça selecionada sozinha, renderizada por uma câmera
pequena em uma textura, que o jogador pode orbitar e dar zoom para ler o
personagem.

- **Aliases:** Selected-piece preview, Painel da peça
- **Status:** validated

## HUD

A interface Canvas construída sobre o jogo: as linhas de título e turno, a
mensagem de status, o histórico de jogadas, o pedido de promoção, a tela
inicial e o painel da peça selecionada.

- **Aliases:** Interface, Canvas UI
- **Status:** validated
