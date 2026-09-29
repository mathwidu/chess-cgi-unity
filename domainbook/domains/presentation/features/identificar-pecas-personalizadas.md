---
id: identificar-pecas-personalizadas
name: Identificar as peças personalizadas de cima
status: draft
owners: [mathwidu]
terms: [peça-personalizada, preview-da-peça-selecionada]
---

## Story

Como jogador olhando o tabuleiro de cima ou por uma vista inclinada
Quero distinguir o lado e o tipo de cada personagem
Para reconhecer a posição sem precisar selecionar cada peça para ler seu nome

## Rule: Roupa e acessórios preservam a identidade de cada pessoa

O personagem é o mesmo nas brancas e nas pretas. A roupa principal muda para
branca ou preta; rosto, pele, cabelo, óculos e particularidades do vestuário
preservam a identidade da pessoa. A identificação do tipo deve funcionar
também pela parte superior.

No retorno de 24/09/2026, o usuário rejeitou a descaracterização por grandes
chapéus e materiais lisos. Pediu referências de outros jogos de xadrez,
melhor acabamento de personagens e roupas, logo da Feevale aparente em todos
e uma espadinha na mão do peão.

## Rule: A direção aprovada permanece integrada ao jogo

O [estudo 01](../../../../art/character-variants/study-01.md) contém 12 variações
e uma composição de 32 peças no Blender. Fica preservado como comparação,
mas sua direção de acessórios e recoloração foi rejeitada.

A [direção 02](../../../../art/character-variants/direction-v2/README.md) foi
aprovada e [aplicada aos seis personagens](../../../../art/character-variants/production/README.md).
Roupa e base definem o lado; rosto, cabelo e particularidades da pessoa são
compartilhados. Acessórios proporcionais e símbolos clássicos na superfície
horizontal da base complementam a identificação do tipo. A marca oficial tem
aplicações próprias para fundos claros/escuros, somente nas costas, conforme
o retorno mais recente do usuário em 25/09/2026.

A revisão de 25/09/2026 restaura a cabeça completa do Ricardo, inclusive a
pequena coroa do modelo original. Mãos e antebraços acomodam os bastões sem
atravessar as mangas; o cabo da espada continua através da guarda. A remoção
das mãos antigas preserva as calças e elimina fragmentos desconectados.

A Feevale usa malhas de aplicação sobre a roupa e um atlas próprio, separado
dos UVs do corpo. O peito fica livre em todo o elenco; a marca antiga dos
moletoms é retirada da cor e do relevo. O importador troca também a textura
da marca ao aplicar o lado.
As bases têm bordas arredondadas, filetes metálicos e símbolos clássicos em
relevo nas diagonais, fora da projeção dos pés e da montaria. Torre e cavalo
preservam a geometria inferior original.

As cores do corpo são geradas em 4K; normais em 2K incluem relevo fino de tecido,
sem aplicá-lo à pele. A importação usa filtragem trilinear e anisotropia 8.
O perfil Android limita os mapas do corpo a 2K. As fontes originais de 2K
continuam limitando o detalhe facial: um bake maior não reconstrói informação
que não existia. A regra de preservação de pele inclui luminosidade mínima,
evitando preservar ruído quase preto da roupa como se fosse pele. A separação
espacial também distingue a mão do peão da calça, fones do moletom e lenço do
cardigan. A barra do Ricardo recebe a mesma recoloração do restante do moletom.
O Blender preserva `SourceUV` para a leitura das fontes e cria `ProductionUV`
com espaço entre ilhas e sem sobreposições entre superfícies. A geração
relaxa os mapas, separa regiões com conflito e executa a checagem antes de
exportar; só `ProductionUV` segue para o GLB. Isso reduz vazamentos
de pele/cabelo nas roupas em níveis menores da textura.

`CustomPieceAppearance` guarda as referências dos materiais dos dois lados.
`PieceFactory` aplica o lado e acomoda a base integrada na altura da casa, sem
adicionar outra base. O fallback de prefabs antigos e o modo desempenho
continuam disponíveis. `MenuCastPreview` aplica as variantes aos professores
antes da animação de destaque; o preview clona a aparência da peça e apresenta
sua frente também nas pretas. O enquadramento do menu comporta a base completa
dos dois professores, inclusive durante a alternância do destaque.

Há capturas reais do Unity de menu, tabuleiro completo, vistas superiores e
preview dos dois lados. Testes PlayMode cobrem os seis tipos nas escalas 1 e
0,045, encaixe na casa inclusive na seleção, compartilhamento de malhas e
isolamento dos materiais. A aceitação de reconhecimento por jogadores e a
medição/validação em headset continuam pendentes; por isso a feature permanece
`draft`. Os arquivos de animação preexistentes e GLBs originais foram preservados.

Além dos testes de integração, `audit_characters.py` verifica a silhueta original
da cabeça do rei, interseções dos bastões, fragmentos das mãos antigas, cobertura
e afastamento das aplicações Feevale, símbolos dentro da base e sua visibilidade
vertical. A amostragem da roupa branca do cavalo cobre a regressão de pixels
escuros. `audit_fabrics.py` verifica as regiões de tecido relatadas e a mão
próxima à calça, incluindo filtragem à distância. Há closes de frente e costas
dos seis personagens nos dois lados. Esses testes não substituem
a avaliação visual de acabamento nem a validação em headset.

A revisão de 25/09 ampliou a amostragem do cavalo para punho, cintura frontal,
costas e borda do capuz. O limite horizontal da recoloração foi substituído
por um contorno que acompanha a pose sentada. Mãos e montaria são comparadas
com as cores da fonte nos dois lados. A mancha triangular na calça foi isolada
como sombra em um ensaio do Unity, não como ausência de triângulos; o ajuste
de iluminação foi aplicado na rodada seguinte ao perfil PC, com sombras
suaves e menor contraste. A luz direcional do preview foi substituída por uma
luz pontual isolada: ela afetava o tabuleiro e a escolha da luz principal no URP.
Um teste na Main compara os pixels com o preview iluminado e apagado. O perfil
Mobile permanece inalterado até a medição em headset.
O relatório em `art/character-variants/visual-review-20260925/` também registra
bordas de roupa, acabamento de mãos e bases, perda de detalhe nas brancas e
casas encobertas pelo preview. A feature continua em avaliação.

A rodada de acabamento amplia a máscara da gola/punho da torre e preserva a
cargo sob a camiseta do peão. Bases passam a ter topo plano e laterais suaves;
os dedos dos acessórios ganham comprimentos/raios variados. A Feevale usa o
PNG oficial transparente, sem placa ou borda, somente nas costas. São 32 testes
PlayMode e 22 casos de tecido aprovados, além das auditorias de UV/geometria.
A [comparação da rodada](../../../../art/character-variants/finishing-review-20260925/README.md)
registra os limites artísticos remanescentes e a descoberta inicial da sala de
aula com tabuleiro sobre uma mesa. Naquela rodada, a sala era um estudo Blender.
O cenário posteriormente integrado à Main está documentado em
[laboratorio-feevale](laboratorio-feevale.md); a avaliação em headset permanece pendente.

## Rule: A leitura de lado e tipo é conferida no jogo

- Cada tipo é reconhecível em conjunto, em casas claras e escuras, nos dois
  lados, pela vista inclinada e pela vista superior.
- Rei/rainha e bispo/cavalo têm contornos superiores diferentes.
- A cor da roupa varia pelo lado sem modificar a cor da pele ou do cabelo.
- Costuras, estampas, óculos, fones e outras particularidades do vestuário
  continuam reconhecíveis; uma cor uniforme não apaga a identidade da pessoa.
- A assinatura oficial da Feevale fica visível apenas nas costas de cada personagem, com
  contraste nos dois lados e aplicação preservada ao trocar a cor do tecido.
- A espadinha do peão tem contato correto com a mão, não atravessa o corpo e
  permanece dentro da casa nos estados de repouso e movimento.
- Acessórios preservam a face e não invadem a casa vizinha.
- Pistas complementares na base ficam voltadas para cima e não são ocultas
  pelos pés; reconhecimento também é conferido pelas costas.
- Promoção e preview exibem o mesmo personagem e a mesma variação da peça.
- A alternativa primitiva e o modo desempenho continuam disponíveis.
- A interação de mouse e a captura em VR continuam corretas, com avaliação
  de legibilidade e desempenho no ambiente de execução antes da aceitação.

## Open Questions

A legibilidade e o desempenho em headset físico permanecem pendentes.
