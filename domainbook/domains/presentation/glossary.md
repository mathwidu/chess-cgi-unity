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
tipo ou quando o modo desempenho está ligado, para que o tabuleiro nunca
fique sem uma peça.

- **Aliases:** Primitive fallback
- **Status:** validated

## Modo desempenho

Opção do menu que troca as peças personalizadas pelas peças clássicas para
aliviar a GPU. Ligá-la faz a fábrica construir peças clássicas mesmo quando
há um prefab de peça personalizada; desligá-la volta às peças personalizadas.
A escolha é persistida e vale desde a primeira montagem das peças.

- **Aliases:** Performance mode
- **Status:** validated

## Destaque

Um marcador que o tabuleiro coloca em cada destino legal da peça
selecionada.

- **Aliases:** Highlight
- **Status:** validated

## Marca do último lance

As duas casas do último lance que pousou (origem e destino), tingidas de
amarelo suave sobre a cor da própria casa. Só o lance mais recente fica
marcado; uma nova partida limpa as marcas.

- **Aliases:** Last move highlight, MarkLastMove
- **Status:** validated

## Rei em xeque

A casa do rei do lado a jogar tinge de vermelho enquanto ele está em xeque,
inclusive no xeque-mate. O vermelho prevalece sobre a marca do último lance e
some quando o xeque é respondido.

- **Aliases:** Check highlight, MarkCheck
- **Status:** validated

## Sons do tabuleiro

Sons curtos para o que acontece no tabuleiro: um toque de madeira para cada
lance, um toque duplo para a captura, um carrilhão para o xeque, um sinal
suave quando a IA devolve a vez e uma frase no fim da partida (vitória,
derrota ou empate). Sintetizados em código, saem do tabuleiro em 3D no VR.

- **Aliases:** Board sounds, BoardSounds
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

## Mesa

A mesa de madeira sobre a qual o tabuleiro fica: tampo, friso, saia, quatro
pés e ponteiras metálicas, modelada em metros do VR e escalada para o desktop.
Uma placa de controle no tampo, à esquerda do jogador, regula a altura: a
mesa sobe ou desce com o tabuleiro em cima, entre um limite mínimo e um
máximo, e a escolha é persistida. A câmera nunca acompanha a mesa.

- **Aliases:** Table, TableView, Altura da mesa, Table height
- **Status:** validated

## Indicador de turno

Um sinal discreto, junto do tabuleiro, de quem joga agora. É uma luz fina que
corre uma vez pela borda do tabuleiro do lado a jogar, amarela quando a vez é
do jogador e clara e neutra quando a IA pensa. Uma etiqueta curta no tampo
("Sua vez", "Vez das brancas", "IA pensando...") aparece e some sozinha. Fica
oculto no menu e no fim da partida.

- **Aliases:** Turn indicator, TurnIndicatorView, Luz de turno
- **Status:** validated

## Peças capturadas

As peças já tomadas, em miniatura sobre uma faixa de feltro ao lado do
tabuleiro, à direita de quem está sentado. A metade perto do jogador guarda o
que ele capturou, e a metade de lá guarda o que o adversário capturou. As
peças mais valiosas vêm primeiro. Um "+N" marca o lado à frente na
[vantagem de material](../gameplay/glossary.md).

- **Aliases:** Captured pieces, CapturedPiecesView, Peças comidas
- **Status:** validated

## Tela de resultado

O diálogo do HUD que anuncia o [resultado da partida](../gameplay/glossary.md)
quando ela termina: quem venceu, ou o motivo do empate, a quantidade de lances
e o lance final. Oferece jogar de novo com a mesma configuração, ver o
tabuleiro final ou voltar ao menu. Ocultá-la para ver o tabuleiro não
reinicia nada; o botão Resultado da barra de ações a traz de volta.

- **Aliases:** Result dialog, GameOverPanel, Tela de vitória
- **Status:** validated
