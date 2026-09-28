# Revisão visual — 25/09/2026

Escopo: corrigir a roupa do Gustavo e avaliar a apresentação dos seis personagens,
o menu, a partida local, a seleção e a vista superior. Evidências recapturadas no
Unity 6000.3.16f1 nesta revisão, usando os prefabs da Main. As propostas gerais
abaixo são avaliação, ainda não uma reformulação aplicada ao jogo.

## Percurso observado

1. **Menu — boa hierarquia, acabamento desigual.** A marca, o botão Jogar e os
   estados selecionados são claros. A cena de fundo parece mais acabada que os
   modelos em primeiro plano. A opção Modo desempenho não explica que substitui
   os personagens por peças clássicas. Evidência: `before/menu.png`.
2. **Partida — precisa de revisão de luz e composição.** Roupas/bases brancas
   perdem dobras e contraste; há grande vazio acima do tabuleiro e um fundo cinza
   sem relação com o ambiente do menu. Evidência: `before/board.png`.
3. **Seleção — identificação útil, sobreposição problemática.** Nome, tipo e casa
   ajudam a reconhecer a peça, mas o painel cobre casas e personagens à direita;
   o preview também parece apertado junto aos botões de zoom. Dados biográficos
   recebem espaço que poderia priorizar a jogada. Evidência: `before/selected-white.png`.
4. **Movimento — turno e histórico claros, orientação a validar.** O registro
   e2–e4 e o novo turno são visíveis. A câmera troca de lado; conforto da transição
   exige observar movimento/VR, não pode ser concluído pela imagem parada.
   Evidência: `before/move.png`.
5. **Vista superior — diferenciação de lados funciona, símbolos pequenos.** Os
   contornos clássicos permanecem visíveis, mas ocupam poucos pixels, especialmente
   rei/rainha/bispo. Não há coordenadas na borda para relacionar o histórico às
   casas. Evidência: `before/board-top.png`.
6. **Elenco — identidade preservada, acabamento ainda irregular.** A montaria,
   torre, fones, lenço, cabelo e acessórios distinguem os personagens de perto.
   Bordas de roupa, dedos, bases e materiais não têm o mesmo nível de acabamento.
   Evidências: `before/cast-front.png` e closes individuais.

## Achados por personagem

| Personagem | O que manter | Próxima melhoria visual |
| --- | --- | --- |
| Matheus / peão | Cabelo ruivo, cargo, camiseta e espada curta | Acertar a borda entre camiseta e calça; dedos da mão da espada ainda parecem cilindros regulares; separar tecido, pele e metal no acabamento |
| Alex / torre | Pose sentado, xadrez da roupa e torre | Limpar contorno da gola e mangas; dedos entrelaçados e encontro com a torre; reduzir aparência brilhante/plástica da camisa |
| Gustavo / cavalo | Óculos, cabelo, moletom e montaria | Corrigir faixa preta no moletom/capuz; revisar sombras duras na calça; depois refinar mãos, rédeas/contato e acabamento da montaria |
| Rafael / bispo | Fones, alça e báculo | Limpar pequenos planos triangulares sob os fones; refinar a mão e eliminar o volume irregular da antiga base sob os pés |
| Marta / rainha | Lenço azul, óculos, saia estampada e pequena tiara | Refinar mãos, espessura da saia e separação de cabelo/rosto; controlar detalhe fino que se perde na distância do tabuleiro |
| Ricardo / rei | Cabeça completa, pequena coroa, cruz e cetro | Melhorar contorno da coroa, cabelo/rosto e naturalidade da mão; tecido menos uniforme no close, sem adicionar ruído |

## Diagnóstico do Gustavo

A recoloração terminava em Z=1,0, cortando punhos e costas no meio da roupa.
A seleção de faces também excluía a borda superior do capuz. O teste de textura
foi ampliado com regiões pequenas que evitam pele e montaria; antes da correção,
a região do punho e a faixa inferior estavam 100% escuras.

Um ensaio separado no Unity manteve o mesmo modelo/material e desligou apenas
as sombras. A mancha triangular da calça desapareceu. Desligar o mapa normal
em seguida não produziu alteração comparável: essa mancha é de sombreamento,
enquanto a faixa horizontal continua visível até no material sem iluminação.
Capturas diagnósticas ficam na pasta local `.local/character-production/gustavo-debug`.

## Acabamento compartilhado

As bases em camadas ajudam a identificar o lado, mas o diâmetro e a borda
estriada dominam os personagens no tabuleiro. A segunda base original do
Gustavo/Rafael cria uma sensação de pedestal empilhado. Os símbolos funcionam
melhor nos closes; a leitura deve ser testada no tamanho real de jogo. A Feevale
as costas está legível de perto, porém o retângulo com borda visível parece
uma placa aplicada: integrar o acabamento ao tecido é uma melhoria posterior,
sem retornar a marca à frente. Evidências: closes e `before/cast-back.png`.

## Ordem recomendada

- **Primeiro: contornos de roupa e iluminação da partida.** Corrigir máscaras por
  superfície, conferir frente/costas/laterais e equilibrar luzes sem apagar sombras.
  Aceite: sem cortes arbitrários, pele preservada e dobras visíveis nos dois lados.
- **Depois: tabuleiro e HUD.** Reservar espaço para o painel, enquadrar o tabuleiro,
  dar material à mesa/borda e incluir coordenadas discretas. Aceite: nenhuma casa
  escondida com preview aberto nas resoluções-alvo.
- **Em seguida: mãos, rostos, cabelo e bases.** Trabalho de modelagem e pintura
  dirigido a cada personagem; uma textura maior não recupera detalhe ausente.
  Padronizar rugosidade e acabamento, preservando características pessoais.
- **Por fim: refinamento para distância e VR.** Simplificar a leitura dos símbolos,
  reduzir peso visual dos pedestais, definir níveis de detalhe e medir desempenho
  no dispositivo. Não decidir orçamento de polígonos apenas pelo desktop.

## Limites

Os primeiros estados usam renderização URP da Main. Galeria/closes usam luzes
neutras próprias para inspeção; não representam a luz da partida. O capturador
suprime pós-processamento. São evidências de aparência e estados, não um ensaio
com jogadores. Não foram avaliados leitores de tela, navegação completa por
teclado, daltonismo, todas as resoluções, promoção/fim de partida ou headset VR.
Os riscos de acessibilidade visíveis incluem símbolos pequenos e destinos legais
indicados principalmente por cor; isso exige testes específicos, sem alegação de
conformidade. Originais, estudos de animação e GUIDs devem permanecer preservados.

## Referências técnicas consultadas

O ensaio local sustenta a distinção entre cor e sombra. Para um próximo ajuste
controlado, a documentação da [Unity 6.3 sobre sombras em URP](https://docs.unity3d.com/6000.3/Documentation/Manual/urp/shadows-troubleshooting-urp.html)
descreve os controles de bias por luz e os efeitos de valores excessivos.
A importação observada usa o tipo Normal Map; a referência da [Unity 6.3](https://docs.unity3d.com/6000.3/Documentation/Manual/texture-type-normal-map.html)
foi conferida. Nenhum pacote, configuração XR ou qualidade global foi alterado.

## Resultado da correção de roupa

A auditoria final de tecidos passou nos 18 casos, incluindo os oito do Gustavo.
As regiões de punho, capuz e costas passaram de aproximadamente 100% de amostras
escuras para 0%; a região frontal passou para 0,15%, abaixo do limite de 1,5%
usado para bordas filtradas. Nas amostras de mãos e montaria, nenhuma excedeu
a tolerância de alteração de cor em qualquer lado. Auditorias de UV e geometria
também passaram. Isso valida as regiões especificadas, não certifica todo o
acabamento artístico. O mapa de cores do restante do elenco permaneceu idêntico.

Verificação final: 31/31 testes PlayMode aprovados e 33 capturas finais do Unity.
Originais, GUIDs e estudos de animação conferidos sem alterações. Trabalho local;
sem commit, publicação ou ensaio no headset. O painel mantém o diagnóstico e as
propostas gerais separados da correção aplicada ao Gustavo.
