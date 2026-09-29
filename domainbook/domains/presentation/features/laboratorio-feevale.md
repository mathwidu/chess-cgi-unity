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

O jogador encontra o tabuleiro sobre uma mesa dentro de um laboratório da
Feevale. A versão atual usa o vídeo da sala real enviado pelo usuário e uma
foto do tampo com uma régua de 30 cm. As fotos institucionais da sala 102 do
prédio Verde orientaram as versões anteriores. A gravação fornecida passa a
ser a referência principal de composição, materiais e condição de luz.

A régua fornece uma referência conhecida de 0,30 m no plano do tampo, mas
não estabelece dimensões completas da mesa ou da sala: parte das bordas está
oculta ou fora da foto. O envelope de 6,8 × 9,4 × 3,3 m e a disposição dos
móveis são estimados e adaptados ao jogo, não um levantamento métrico.

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
31 renderers com materiais agrupados, dentro do limite existente de 48.
Teto e paredes não projetam sombras que bloqueiem a iluminação direcional. A iluminação do preview continua isolada.
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
não requer sombras adicionais em tempo real. Três conjuntos de janelas altas têm
caixilhos claros subdivididos, bandeiras superiores e faixas ocre. O vidro
escuro opaco usa a reflexão do ambiente e segue a condição noturna do vídeo,
sem uma segunda cena externa nem ordenação de transparências.

A luz principal representa o teto, com inclinação de 68 graus, intensidade
0,72, cor branca levemente fria e sombra 0,64. O preenchimento oposto usa
0,95. A luz de sombra é explicitamente a principal, mesmo com preenchimento
mais intenso. As 18 luminárias lineares têm uma única lâmpada e seguem o comprimento
da sala, paralelas às paredes de persianas/janelas, conforme correção do usuário.
São geometria emissiva; não criam 18 luzes.
As duas luzes direcionais recebem dois preenchimentos de cone largo junto ao
teto, voltados para baixo e sem sombras adicionais. A intensidade em metros é
2,1; alcance acompanha a escala da sala, e intensidade acompanha seu
quadrado para compensar a atenuação por distância no desktop.
Um reflection probe de 128 px por face registra somente o laboratório, na
camada Ignore Raycast, após a pose estabilizar. A atualização distribui o
trabalho por frames e ocorre na inicialização, reconstrução ou troca da pose
da sala, nunca continuamente. Peças, HUD e estúdio do preview ficam fora do
reflexo. Não há novos passes de sombra por luz adicional ou dependência de
pós-processamento. O custo e o conforto devem ser medidos no headset alvo.

O botão **Olhar ao redor** e os atalhos
ficam no HUD de desktop; a versão VR continua com o painel no espaço.

## Rule: A composição prioriza os elementos reconhecíveis da gravação

A parede do fundo é azul e tem quadro branco e porta clara; a parede da
projeção é cinza e tem um segundo quadro. Vigas aparentes, eletrocalhas,
suportes metálicos e luminárias de tubos reconstroem o ritmo do teto. O
ar-condicionado largo fica junto às janelas e o projetor permanece suspenso.
As persianas cinza têm pesos e correntes inferiores. Abaixo delas ficam
armários brancos com nichos abertos, portas coloridas e três equipamentos
genéricos de laboratório.

Quatro grupos reúnem 16 mesas individuais. Os conjuntos são girados em 90 graus:
alunos e notebooks ficam voltados às persianas ou janelas, com o quadro de
projeção ao lado, conforme correção confirmada pelo usuário. Há 17 notebooks,
incluindo o da mesa do professor, e 19 cadeiras contando os dois jogadores. A quantidade é
uma adaptação: o vídeo não demonstra a contagem completa. As mesas usam
laminado claro, bordas finas, passa-cabos pretos e pés metálicos em T. Os
notebooks têm variações abertas/fechadas e conteúdo abstrato nas telas.
A mesa central fica livre, com as âncoras anteriores do tabuleiro e jogador.

Os arquivos pessoais da referência permanecem em armazenamento local,
fora do asset e dos diretórios destinados ao Git. Pessoas e conteúdo das
telas da gravação não são reproduzidos no cenário.

O fonte Blender, mapas e capturas desta versão estão em
`art/character-variants/feevale-room-v3-20260928/`. O gerador é
`art/character-variants/build_feevale_lab.py`; `FeevaleLabImport.Build` atualiza
o prefab preservando os GUIDs. `RunReferenceDesktop` e `RunReferenceVr` de
`FeevaleLabReviewCapture` salvam a nova evidência sem sobrescrever as anteriores.
A sala anterior permanece em `tabletop-polish-20260926/lab-source/`; o
acabamento do tabuleiro dessa rodada continua ativo. Veja
[acabamento-do-tabuleiro](acabamento-do-tabuleiro.md).

Verificação proporcional: contato entre mesa e base nos dois tamanhos, 32 peças
sem duplicação, seleção livre de colisores decorativos, reaplicação sem duplicar
a sala, revisão de menu e jogada no desktop e inicialização com HMD simulado.
Capturas com HMD simulado não comprovam ergonomia ou desempenho no headset.

## Open Questions

Alcance, conforto, legibilidade em estéreo e desempenho precisam de validação
no headset alvo. As dimensões e a disposição atual da sala 102 não foram medidas.
