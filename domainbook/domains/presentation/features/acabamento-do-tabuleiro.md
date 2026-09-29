---
id: acabamento-do-tabuleiro
name: Acabamento do tabuleiro
status: ready
owners: [mathwidu]
terms: [peça-personalizada, peça-clássica, destaque]
---

## Story

Como jogador no desktop ou VR
Quero reconhecer casas e coordenadas em um tabuleiro de madeira bem acabado
Para localizar minhas jogadas e perceber o jogo apoiado sobre a mesa

## Rule: A moldura é decoração; as 64 casas mantêm a interação

`BoardView` carrega `Resources/Environment/ChessBoardFrame` dentro de
`BoardFrame/BoardBase`. A moldura tem quinas suavizadas, filete discreto e
32 coordenadas em geometria: A–H nas duas bordas de jogadores, 1–8 nas
laterais, orientadas para brancas e pretas. O asset não tem colisores, casas
ou peças duplicadas. As 64 casas, o raycast, os destaques e a altura das peças
continuam sob o `BoardView` existente.
As cores seguem a orientação padrão: A1 escura, H1 clara e cada rainha
inicia sobre uma casa da cor do seu lado.
As casas recebem sombras das peças, mas não projetam sombra: a moldura
sólida já define a silhueta do tabuleiro. Isso evita auto-sombra das lâminas
finas na escala pequena do VR.

A área de jogo mede 45 cm no VR. A moldura mede 50,76 cm e tem 26,1 mm de
espessura. O fundo chega a -0,54 unidade local; o tampo central fica em
0,7557 m, com a mesma âncora de tabuleiro em 0,78 m. O desktop acompanha a
escala do tabuleiro. Na ausência do prefab, a base primitiva mantém o fundo
na mesma altura. O modo desempenho continua trocando somente as peças.
O enquadramento inicial do desktop usa campo de visão de 50 graus para
deixar as coordenadas acima da barra de ações em 16:9. O jogador conserva
o zoom e a órbita; o campo de visão do headset continua sob o sistema XR.

## Rule: As madeiras mantêm a leitura das peças

Casas claras em maple e escuras em nogueira usam cor, normal e rugosidade
com acabamento acetinado; a moldura tem nogueira mais escura. A pequena
variação dos veios não substitui o contraste entre casas. As coordenadas
claras e opacas ficam fora da área jogável, sem reflexos ou transparências.

`build_chess_board.py` preserva um fonte Blender editável e exporta somente
a decoração e amostras de materiais. `ChessBoardImport.Build` remove essas
amostras ao criar o prefab e atualiza os materiais Board_Light/Board_Dark
preservando seus GUIDs e referências da Main. O importador ativa mipmaps e
anisotropia 8×. `ChessBoardImport.BuildTabletop` reimporta também o laboratório.

## Rule: O acabamento é conferido nas duas escalas

Testes de Play Mode verificam o contato da base com a mesa em desktop e
escala VR, coordenadas alinhadas às casas, ausência de colisores decorativos
e raycasts sobre as quatro fileiras vazias. O capturador da Main mantém
evidências anteriores e salva a revisão em `tabletop-polish-20260926/unity`.

## Open Questions

Capturas VR usam HMD simulado. Conforto estéreo, desempenho e legibilidade
em headset físico continuam dependentes da avaliação no hardware alvo.
