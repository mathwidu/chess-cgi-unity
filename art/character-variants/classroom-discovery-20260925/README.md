# Descoberta visual — uma partida dentro da Feevale

Estudo de composição de 25/09/2026, após a rodada de acabamento dos personagens
e da luz. O pedido é jogar em VR dentro de uma sala de aula da Feevale, com o
tabuleiro sobre uma mesa. Esta sala é **uma proposta inspirada na universidade**,
sem levantamento de uma sala específica. Ainda não está integrada à Main.

## O que é possível avaliar agora

- [Vista sentado](classroom-eye.png): os 32 personagens reais, na escala 0,045
  usada pelo jogo, sobre um tabuleiro de 45 cm e uma mesa com tampo a 77,4 cm.
- [Vista do ambiente](classroom-overview.png): janelas laterais, quadro com a
  marca oficial, carteiras, cadeiras verdes e espaço livre ao redor da mesa.
- [Fonte editável](classroom-study.blend) e [dimensões do estudo](study.json).
  A cena foi construída por `../build_classroom_study.py`, sem alterar a Main.

São renders offline do Blender. Eles avaliam proporção, materiais e composição;
não demonstram aparência no Unity, FPS, conforto ou interação no headset.
O arquivo JSON usa coordenadas do Blender (Z é altura), enquanto o Unity usa Y.

## Situação encontrada no jogo

`ScenePolish.BuildCollegeTheme` remove os objetos sob `CollegeTheme` e recria
somente um piso e uma mesa cúbica. Assim, as paredes e quadros serializados na
Main não definem o ambiente durante a partida. A próxima implementação precisa
revisar essa construção, além de criar os modelos do ambiente.

A iluminação agora preserva melhor o relevo das peças e o preview deixou de
iluminar o tabuleiro inteiro. A cena jogável ainda tem fundo cinza, borda lisa,
mesa sem pernas e nenhum contexto espacial de sala de aula. O painel de seleção
desktop continua cobrindo casas à direita. O posicionamento do HUD em VR precisa
de uma avaliação própria: o código atual o coloca em `(0, 1.4, 4)` com escala
0,0032, separado da mesa; uma captura desktop não valida esse arranjo.

## Direção visual proposta

O tabuleiro é o foco. Mesa de laminado claro, estrutura metálica fosca, piso e
paredes neutros evitam competir com as roupas brancas e pretas. Verde aparece
nas cadeiras e em poucos objetos. A Feevale aparece no quadro e nas costas dos
personagens, mantendo as peças livres de placas frontais.

Luz lateral ampla deve ajudar a ler dobras, mãos e símbolos sem estourar o
branco. A sala pode usar iluminação estática pré-calculada; as peças móveis
precisam de iluminação e sombras compatíveis com sua movimentação. Essa é uma
hipótese de implementação a medir no hardware escolhido.

Na vista sentado, o quadro fica parcialmente fora do enquadramento quando o
jogador olha o tabuleiro. Isso é esperado: a identidade da sala deve funcionar
quando ele movimenta a cabeça, sem forçar a câmera a mirar o quadro. O jogo
precisa continuar legível com a atenção voltada para as casas.

## Medidas: código atual e proposta

| Elemento | Origem | Medida / decisão |
| --- | --- | --- |
| Área de jogo | `BoardView`, escala VR | 45 × 45 cm; casas de 5,625 cm |
| Altura de referência do tabuleiro | `XRRig` | 78 cm |
| Olhos sentado | `XRRig` | 1,20 m de altura, 60 cm atrás do centro |
| Símbolo de tipo | Gerador × escala VR | Cerca de 7,2 mm; precisa de teste de leitura |
| Mesa atual em VR | `ScenePolish` | Bloco de 90 × 90 cm e 77,4 cm de altura |
| Superfície de jogo do estudo | Proposta, sobre a borda | 80,2 cm; precisa alinhar alvo de interação ao integrar |
| Mesa do estudo | Proposta | 1,30 × 0,90 m, tampo de 3,5 cm e pernas separadas |
| Sala | Proposta | 6,8 × 7 m; sem correspondência comprovada com uma sala real |

A borda mais distante da área de jogo fica a 82,5 cm da posição horizontal dos
olhos, antes de considerar inclinação do tronco ou alcance dos braços. Isso não
é uma medida ergonômica de alcance. O teste em headset deve comparar interação
por raio, inclinação e eventual ajuste da posição da mesa, sem presumir que
todas as casas possam ser tocadas confortavelmente.

## Próxima etapa, em ordem

| Prioridade | Mudança proposta | Como avaliar |
| --- | --- | --- |
| 1 — mesa e tabuleiro | Substituir o bloco por tampo/pernas, criar borda chanfrada, materiais foscos e coordenadas discretas | Tabuleiro apoiado sem interseções; coordenadas legíveis; nenhuma reflexão encobre peças ou destinos |
| 2 — seleção e leitura | Reservar espaço ao preview; indicar destino também por forma/contorno; testar espessura dos símbolos | Todas as casas visíveis com seleção aberta; seis tipos reconhecidos sentado, dos dois lados |
| 3 — sala de aula | Paredes, janelas, quadro, carteiras e cadeiras, com densidade visual baixa perto da mesa | Sensação de estar numa sala ao virar a cabeça; objetos não atravessam usuário ou espaço de interação |
| 4 — luz e materiais em VR | Traduzir o estudo para URP com luz estática, probes e sombras proporcionais ao hardware | Roupas claras conservam dobras; pretas conservam volume; comparação em PCVR e standalone conforme alvo |
| 5 — custo e conforto | Medir os 32 personagens e ambiente juntos antes de definir LOD e resolução final | Frame time de CPU/GPU no headset, memória, estabilidade e alcance; sem aceitar só por contagem de triângulos |

## Acabamento dos personagens que permanece aberto

A rodada atual melhora máscaras, preensão dos acessórios, bases e aplicação da
marca. Ainda há oportunidades de modelagem/pintura localizada: dedos e pele;
gola e pequenos planos sob os fones do Rafael; base original sob alguns modelos;
contorno da coroa do Ricardo; cabelo e rosto; separação da saia e das mãos da
Marta. As texturas originais limitam o detalhe facial. Aumentar o bake não cria
informação nova. Esses pontos estão visíveis nos closes da
[revisão de acabamento](../finishing-review-20260925/index.html).

## Referências consultadas e limites

- A [infraestrutura oficial do ICCT](https://www.feevale.br/institucional/infraestrutura/instituto-de-ciencias-criativas-e-tecnologicas)
  situa salas e laboratórios da universidade. Não fornece as medidas deste estudo.
- A [infraestrutura de Design de Interiores](https://www.feevale.br/graduacao/design-de-interiores/infraestrutura)
  descreve espaços de ensino e desenho. Não foi usada como comprovação de uma
  planta, mobiliário ou paleta de uma sala específica.
- A [documentação de otimização de sombras do Unity 6.3](https://docs.unity3d.com/6000.3/Documentation/Manual/shadows-optimization.html)
  explica a troca entre suavidade e custo das sombras. Por isso a alteração desta
  rodada ficou no perfil PC; o perfil Mobile aguarda medição real.

O estudo escolhe a composição para a próxima implementação. Aceitação visual
da sala, reconhecimento dos tipos, acessibilidade e desempenho continuam
dependentes da cena jogável e do headset.
