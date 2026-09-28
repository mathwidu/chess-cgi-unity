# Personagens da turma — direção 02

> Atualização de 25/09/2026: na implementação atual, a Feevale aparece apenas
> nas costas, conforme o retorno do usuário. Os ajustes de tecidos, UVs e as
> capturas dos dois lados estão no [registro de produção](../production/README.md).
> As propostas abaixo registram a direção visual que originou a implementação.

Pesquisa e conceito visual de 24/09/2026, na branch `codex/character-variants`,
sobre a `main` `81bea19`, que já integra IA, menu e VR.

**Estado: direção aprovada e aplicada aos seis personagens no Unity.**
O usuário aprovou o segundo conceito e autorizou sua aplicação ao elenco.
Veja [modelos reais, capturas e verificação](../production/README.md). Esta
página preserva a pesquisa e os critérios que orientaram o acabamento.

- [Painel visual com referências e comparações](index.html)
- [Referências e interpretação](references.md)
- [Conceito de peão e rei, brancas e pretas](concepts/pawn-king.png)
- [Elenco original renderizado a partir dos GLBs](original-cast.png)

## O que precisa mudar

O mesmo personagem deve continuar reconhecível nos dois lados: mesmo rosto,
cabelo, óculos, corpo e roupa característica. O tecido principal passa a
branco ou preto, preservando costuras, pregas, estampas e pequenos detalhes de
identidade. O usuário pediu uma logo Feevale visível em todos e uma espadinha
na mão do peão. Os grandes chapéus do estudo 01 não serão adotados como solução
do conjunto.

O gerador anterior removeu a ligação da textura com a cor do material e
aplicou um valor uniforme em faces selecionadas por posição/cor. Isso apagou
informação importante do tecido. A revisão deve partir das texturas originais,
com máscaras por região de roupa e uma camada independente para a assinatura.
As máscaras aproximadas do estudo 01 não são uma segmentação final de vestuário.

## Direção para os seis personagens

| Peça / pessoa | Identidade a preservar | Pista proposta para a peça | Aplicação da Feevale |
| --- | --- | --- | --- |
| Peão / Matheus | Cabelo e barba ruivos, camiseta, calça cargo e tênis | Espadinha curta na mão, cabeça livre; conjunto mais simples | Assinatura no peito; aplicação complementar nas costas |
| Torre / Alex | Cabelo, camisa xadrez e postura sentada | Valorizar a torre onde já se senta, deixando o parapeito quadrado visível; dispensar chapéu de ameias | Patch de tecido no peito, sem apagar o xadrez; marca nas costas |
| Cavalo / Gustavo | Óculos, moletom, postura e montaria | Melhorar a cabeça e o pescoço do cavalo existente; dispensar um segundo cavalo sobre a cabeça | Peito do moletom e costas abaixo do capuz |
| Bispo / Rafael | Moletom, fones no pescoço e alça transversal | Testar um báculo fino ao lado do corpo, com ponta visível além do ombro; cabeça livre | Área do peito livre da alça e dos fones; costas |
| Rainha / Marta | Óculos, cabelo, cardigan, lenço azul e saia estampada | Silhueta da saia e pequena tiara próxima da original; remover a grande coroa do estudo 01 | Patch no cardigan, fora do lenço; costas sem ocultar a estampa |
| Rei / Ricardo | Óculos, cabelo grisalho, moletom Feevale e detalhes azuis | No conceito, cetro fino com pequena cruz; cabeça livre. Comparar sua leitura com a rainha | Assinatura maior no centro do moletom, como no original; costas abaixo do capuz |

A cabeça do báculo, o cetro e a tiara ainda precisam de comparação de silhueta:
acessórios pequenos podem desaparecer de longe. Não é necessário acrescentar
uma arma a cada personagem. A torre e a montaria já fornecem pistas que devem
ser aproveitadas.

## Leitura de cima

Na vista inclinada, usar contorno, postura e um acessório principal. Perto de
90 graus, rostos, estampas no peito e acessórios verticais podem ficar ocultos.
Por isso a proposta inclui o símbolo clássico da peça aplicado **na superfície
horizontal superior da base**, repetido na frente e atrás, fora da área dos
pés. Não é uma placa vertical com o nome do personagem.

Os símbolos devem ter a mesma forma nas brancas e nas pretas, contraste com a
base e distinção clara entre rei/rainha e bispo/peão. Ainda precisam de espaço
real no modelo: o conceito 2D não comprova que o encaixe funcionará no tabuleiro.
A cor do tecido e da base informa o lado; o símbolo informa o tipo. O contorno
laranja de interação VR existente continua com função própria.

## Logo oficial e roupas

Os PNGs de [brand/](brand/) foram extraídos sem alteração do pacote oficial
disponível no [site da Feevale](https://www.feevale.br/institucional/bem-vindo/marketing/manual-da-marca):

- `feevale-color-dark-type.png`: símbolo colorido e letras cinzas para tecido claro.
- `feevale-color-white-type.png`: símbolo colorido e letras brancas para tecido escuro.
- [Proveniência e hashes](brand/provenance.json).

Preservar a assinatura completa e sua proporção. Usar área lisa de tecido ou
patch para que pregas, xadrez, lenço, fones ou alças não destruam a leitura.
Separar tecido, detalhe e marca no arquivo fonte; a exportação poderá reuni-los
em uma textura para limitar o custo. Uma troca da cor do lado nunca deve
recolorir a pele, cabelo ou a marca.

A aplicação complementar nas costas atende também à visão do próprio lado do
tabuleiro. A legibilidade do texto no peito a 90 graus não é um objetivo
realista: a marca e a identificação do tipo de peça têm funções distintas.

## O que significa melhorar a qualidade

1. **Modelagem:** limpar mãos, dedos, contato com objetos, óculos e contorno do
   cabelo. Construir gola, punhos, bolso e costuras coerentes com cada roupa;
   evitar tecido fundido na mão ou acessório atravessando o corpo.
2. **Materiais:** recuperar estampas e variação de tecido, controlar brilho e
   rugosidade de algodão, jeans, couro e metal. Não adicionar detalhe fino que
   vira ruído na escala real da peça.
3. **Variações:** mesma geometria e UV para o par de lados, com regiões de
   tecido e marca bem definidas. Preservar as particularidades de cada pessoa.
4. **Escala de jogo:** examinar o tabuleiro com 32 peças e peças adjacentes,
   além do close. Sombras ou iluminação de estúdio não substituem esse teste.

Para o peão, refazer a mão para segurar o cabo: não atravessar o gesto de
positivo do GLB atual com uma haste. A lâmina deve ter tamanho próximo ao
antebraço, guardar distância do corpo e permanecer dentro da casa durante
repouso, seleção, deslocamento e captura. A ilustração sugere a posição; não
valida contatos, colisões ou animação.

## Produção aplicada

Matheus e Ricardo serviram de pilotos para o contato com acessórios, contraste
da marca e variações do tecido. O processo foi aplicado aos seis personagens,
com fonte Blender, exportações, materiais compartilhados e prefabs preservando
os GUIDs. Menu, tabuleiro e preview foram conferidos no Unity; a medição no
hardware alvo de VR continua pendente.

As alterações concretas, contagens de geometria, comandos e limitações estão
no [registro de produção](../production/README.md). Os estudos de animação
preexistentes foram preservados.

## Critérios para aceitar o lote

- Reconhecer os personagens lado a lado com os originais, sem consultar nomes.
- Distinguir lado e tipo nas casas claras e escuras, em visão frontal, inclinada,
  a 60–75 graus e a 90 graus, incluindo costas e tabuleiro cheio.
- Logo presente nos seis personagens, com contraste nos dois lados e sem
  interferência de alças, capuzes e acessórios.
- Espada segura pela mão e dentro da casa, sem interseções nos estados do jogo.
- Materiais consistentes no menu, preview da peça selecionada e promoção.
- Modo desempenho, seleção por mouse, agarrar/soltar e contorno em VR corretos.
- Custo medido no Unity/URP e leitura observada no headset; ausência de teste
  com jogadores ou headset deve continuar explícita.

## Proveniência do conceito e limites

`concepts/pawn-king.png` foi criado e refinado com o **imagegen integrado** a
partir do render original e das assinaturas oficiais. Os prompts completos
estão em [pawn-king.prompt.txt](concepts/pawn-king.prompt.txt) e
[pawn-king-refine.prompt.txt](concepts/pawn-king-refine.prompt.txt).
O [primeiro rascunho](concepts/pawn-king-draft.png) tinha placas verticais;
a revisão mudou os símbolos para cima e corrigiu a disposição da marca no peão.

É uma ilustração de direção de arte. Rostos e logotipos podem apresentar
variações geradas; ela não deve substituir a geometria original nem servir
como textura pronta. As miniaturas superiores também são ilustrações, não
câmeras sincronizadas de um mesmo modelo. Na produção, usar os PNGs oficiais
e comprovar identidade, contatos e dimensões nas malhas reais.

**O conceito é uma referência ilustrada. A implementação posterior está em
[production/](../production/README.md), com os prefabs e scripts atualizados,
GLBs originais preservados e capturas reais do Unity. Sem teste em headset,
build de player, commit ou publicação nesta entrega.**
