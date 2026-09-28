# Estudo 01 — histórico, não usado no jogo

Estudo visual de 24/09/2026, sobre a `main` `81bea19` (IA, menu e VR
integrados). **São modelos de estudo no Blender, ainda fora do jogo.**
Os GLBs, prefabs, materiais, cena e controles do Unity permanecem preservados.

## Revisão da direção — 24/09/2026

**O estudo 01 abaixo foi rejeitado como direção de acabamento.** Os chapéus
grandes descaracterizaram os personagens e a cor lisa apagou estampas e a
assinatura do moletom de Ricardo. Os arquivos em `review/` ficam preservados
para comparação; não são a proposta atual nem assets de produção.

A [direção 02](direction-v2/README.md) reúne referências de jogos de xadrez,
preservação do vestuário, aplicação da logo oficial da Feevale em todos os
personagens e espadinha para o peão. Veja o
[painel visual](direction-v2/index.html) e o
[conceito do peão e do Ricardo](direction-v2/concepts/pawn-king.png).

O novo conceito é uma imagem gerada para orientar acabamento e acessórios;
**não representa malhas 3D refeitas ou uma captura do Unity**. Os GLBs do jogo
continuam originais. Os símbolos na parte superior da base são uma proposta
complementar para testar a leitura de cima, sem depender de chapéus.

## Requisitos mantidos

- O mesmo personagem representa cada tipo de peça nos dois lados.
- Brancas usam roupa branca; pretas usam roupa preta. Rosto, pele e cabelo
  continuam sendo a identidade do personagem.
- O tipo precisa ser reconhecível pelo topo e pela vista inclinada do
  tabuleiro. Acessórios, silhueta e símbolos clássicos discretos podem
  colaborar; nomes na base não resolvem sozinhos essa necessidade.

## Estudo 01 — histórico para comparação

| Peça | Personagem | Forma no topo |
| --- | --- | --- |
| Peão | Matheus | Cabeça livre, sem acessório alto; forma mais simples do conjunto |
| Torre | Alex | Aro quadrado com ameias |
| Cavalo | Gustavo | Perfil de cavalo em relevo sobre um elmo aberto |
| Bispo | Rafael | Mitra alongada com duas metades separadas |
| Rainha | Marta | Coroa aberta de cinco pontas, com extremidades arredondadas |
| Rei | Ricardo | Cruz larga em relevo sobre a coroa, visível de cima |

As roupas são materiais neutros de estudo. A segmentação por faces é uma
primeira aproximação: limites junto de mãos, gola e punhos precisam de
acabamento. Estampas, logotipos e padrões das roupas não são a versão final.
Os acessórios são volumes de comparação, ainda sujeitos a ajuste de tamanho,
caimento e estilo. As coroas antigas são parcialmente retiradas somente das
cópias usadas pelo estudo.

## Arquivos

- [Fonte editável e empacotada](review/character-variants-study.blend): duas
  cenas, `01 · Comparativo dos personagens` e `02 · Leitura no tabuleiro`.
  A primeira contém as 12 variações. A segunda mostra 32 peças sobre 64 casas.
- [Vista inclinada](review/angle-45.png), [vista superior](review/top.png),
  [vista mais baixa](review/front.png) e [tabuleiro completo](review/board.png).
  No comparativo, as colunas são peão, torre, cavalo, bispo, rainha e rei;
  pretas atrás, brancas à frente.
- [Relatório de origem](review/report.json): personagens, contagem de faces e
  hashes SHA-256 dos seis GLBs de origem.
- [Gerador do estudo](generate_study.py): modela os acessórios e atribui
  materiais somente a cópias importadas em memória.

Reproduzir com o Blender 5.2.1 LTS já instalado:

```sh
blender --background --python art/character-variants/generate_study.py
```

O comando recria os arquivos dentro de `review/`. Para preservar ajustes
manuais, salvar uma cópia do `.blend` com outro nome antes de regenerar.

## Verificação do estudo 01

O `.blend` foi reaberto no Blender 5.2.1 LTS. A verificação encontrou as duas
cenas, as 12 variações e as 32 peças do tabuleiro; confirmou que cada par
branca/preta tem a mesma geometria e atribuição de faces, com materiais de
roupa diferentes. Todas as texturas de arquivo estão incorporadas. Os seis
GLBs de origem continuam com os mesmos hashes. Registro:
[validation.json](review/validation.json).

Os renders comparam as formas pelos ângulos necessários, inclusive no
tabuleiro cheio. Isso não comprova reconhecimento por jogadores nem qualidade
em um headset. O estudo usa Blender/Cycles, não a renderização URP do jogo.

O roteiro anterior de integração, ainda não executado, era:

1. Avaliar a leitura das seis formas no tabuleiro sem consultar uma legenda,
   especialmente cavalo/bispo e rainha/rei.
2. Refinar os acessórios escolhidos e as fronteiras dos materiais das roupas,
   preservando detalhes de identidade que devam permanecer nas estampas.
3. Preparar materiais e malhas compartilhados, exportar por personagem e
   ligar a variação à cor do lado em `PieceFactory` e ao preview.
4. Conferir promoção, modo desempenho, seleção por mouse, captura/soltura em
   VR, contorno de seleção, enquadramento e escala do tabuleiro. Medir o custo
   em URP e avaliar a leitura no headset disponível.

Não há novos prefabs de produção ou validação de VR nesta entrega do estudo.
O próximo refinamento deve seguir a direção 02, e não reproduzir os acessórios
ou a recoloração lisa do gerador histórico.
