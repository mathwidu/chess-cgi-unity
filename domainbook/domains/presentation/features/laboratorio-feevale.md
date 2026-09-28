---
id: laboratorio-feevale
name: Laboratório de informática Feevale
status: ready
owners: [mathwidu]
terms: [peça-personalizada, hud, modo-desempenho]
---

## Story

Como jogador de xadrez em PC ou VR
Quero encontrar o tabuleiro sobre uma mesa dentro de um laboratório da Feevale
Para perceber a escala e reconhecer o ambiente do meu curso

## Rule: O laboratório usa referências reais e acompanha a escala do tabuleiro

O jogador encontra o tabuleiro sobre uma mesa dentro de um laboratório inspirado
em fotografias oficiais da Feevale. A direção acompanha a experiência do usuário
com laboratórios de informática no curso. A segunda etapa usa a foto oficial
do Laboratório de Redes, identificado pela Feevale como sala 102 do prédio
Verde. O layout e as medidas são adaptados; não é uma réplica medida da sala.

`ScenePolish` carrega `Resources/Environment/FeevaleComputerLab` na Main. O modelo
é autorado em metros e acompanha posição, rotação e escala reais de `BoardView`.
Isso mantém a mesa encostada na base do tabuleiro tanto no desktop ampliado
quanto no modo VR. A relação de referência é 0,045 m por unidade do tabuleiro,
com centro em 0,78 m e tampo em 0,7557 m. O tampo central foi rebaixado 18 mm
para apoiar a moldura de madeira de 26,1 mm, mantendo casas, peças e assentos
nas posições anteriores. A atualização acompanha mudanças de
modo sem precisar reconstruir a geometria do ambiente.

O prefab contém somente o cenário. As peças personalizadas, peças clássicas,
HUD e regras permanecem sob seus componentes atuais. O cenário não tem
colisores de decoração e não interfere no raycast das casas. O asset usa
31 renderers com materiais agrupados; teto e paredes não projetam sombras que
bloqueiem a iluminação direcional. A iluminação do preview continua isolada.
O laminado fino dos tampos recebe sombras, mas sua borda e estrutura projetam
a sombra da mesa. Evitar o laminado como caster reduz as marcas de auto-sombra
sem remover as sombras dos objetos colocados sobre ele.

As luzes do estúdio dos professores ficam ligadas somente durante o render
síncrono do preview. Uma comparação da sala com a luz do estúdio desligada
encontrou 51.123 pixels alterados antes da correção; uma máscara de camada
sozinha não isolava essa luz direcional no URP.

A luz principal usa bias de profundidade 1,0 e normal 0,5 quando o tabuleiro
está na escala pequena, eliminando faixas de auto-sombra sobre a mesa. No
desktop ela volta aos valores do pipeline. Não há alteração dos assets de
qualidade PC/Mobile nesta etapa; `ChessCgi.Runtime` referencia o runtime URP
para configurar somente essa luz, sem modificar o asset compartilhado.

O HUD World Space ocupa 2,304 × 1,296 m, centrado em (0; 1,6; 1,35), voltado
ao usuário sentado. Ele permanece à frente das bancadas, acima da mesa e do
encosto da cadeira oposta. `SeatAwarePoint` espelha sua posição ao jogar de
pretas. A configuração anterior, a 4 m do centro, ficava escondida atrás dos
monitores. O layout Screen Space do desktop não muda.

## Rule: Os materiais mantêm detalhe perto da mesa

Os tampos agora têm madeira clara com mapas de cor, normal e rugosidade em
2048 px. Pintura, tecido das cadeiras e persianas usam mapas compartilhados
de 1024 px. Mipmaps e filtragem anisotrópica 8× preservam a leitura em ângulos
rasantes. A cópia de exportação recebe oclusão de contato em cor de vértice;
não requer sombras adicionais em tempo real. Três conjuntos de janelas com
caixilhos, bandeiras superiores e peitoris detalham a direita. O vidro fosco
usa um mapa opaco de luz externa, evitando custo e ordenação de transparências.
Nichos coloridos sob as persianas remetem aos detalhes da foto da sala 102.

A luz principal vem da direita, alinhada com as janelas, com intensidade 1,15
e sombra 0,76; o preenchimento oposto usa 0,22. O ambiente equatorial é
levemente quente, com menos luz uniforme para separar os materiais.
As duas luzes direcionais recebem dois preenchimentos de cone largo junto ao
teto, voltados para baixo e sem sombras adicionais. Alcance acompanha a escala da sala, e intensidade
acompanha seu quadrado para compensar a atenuação por distância no desktop.
Um reflection probe de 128 px por face registra somente o laboratório, na
camada Ignore Raycast, após a pose estabilizar. A atualização distribui o
trabalho por frames e ocorre na inicialização, reconstrução ou troca da pose
da sala, nunca continuamente. Peças, HUD e estúdio do preview ficam fora do
reflexo. Não há novos passes de sombra por luz adicional ou dependência de
pós-processamento. O custo e o conforto devem ser medidos no headset alvo.

O botão **Olhar ao redor** e os atalhos
ficam no HUD de desktop; a versão VR continua com o painel no espaço.

O acabamento de setembro de 2026 está em
`art/character-variants/tabletop-polish-20260926/`, com fontes e materiais
regeneráveis. Os veios dos tampos têm menor contraste e escala mais fina;
as superfícies continuam com mapas de cor, normal e rugosidade em 2048 px.
Veja [acabamento-do-tabuleiro](acabamento-do-tabuleiro.md).

O fonte Blender, as referências, as medidas propostas e as capturas da Main
estão em `art/character-variants/feevale-room-v2-20260925/`; a primeira etapa
permanece em `feevale-lab-20260925/`. O gerador é
`art/character-variants/build_feevale_lab.py` e o importador do Editor é
`FeevaleLabImport.Build`. Reimportar o cenário não altera os personagens.

Verificação proporcional: contato entre mesa e base nos dois tamanhos, 32 peças
sem duplicação, seleção livre de colisores decorativos, reaplicação sem duplicar
a sala, revisão de menu e jogada no desktop e inicialização com HMD simulado.
Capturas com HMD simulado não comprovam ergonomia ou desempenho no headset.

## Open Questions

Alcance, conforto, legibilidade em estéreo e desempenho precisam de validação
no headset alvo. As dimensões e a disposição atual da sala 102 não foram medidas.
