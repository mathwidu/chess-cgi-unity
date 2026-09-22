---
name: Xadrez CGI
description: Interface Unity do xadrez da turma, com professores 3D, cenário de estudo e identidade Feevale.
colors:
  overlay: "#08251D"
  panel: "#0A3924F8"
  panel-strong: "#042B1B"
  preview-surface: "#18422B"
  text: "#FFFFFF"
  muted-text: "#CAE4D3"
  accent: "#FFDD00"
  action-hover: "#FFEB6C"
  action-pressed: "#DDBE00"
  neutral-button: "#0B4B2B"
  neutral-hover: "#18663D"
  neutral-pressed: "#054123"
  menu-panel: "#0A3024F6"
  menu-border: "#2B5642"
  choice: "#13392B"
  choice-selected: "#17402F"
  choice-border: "#315645"
  play-border: "#FFE74D"
  play-depth: "#AF9100"
typography:
  display:
    fontFamily: Lato
    fontSize: "92px"
    fontWeight: 900
  headline:
    fontFamily: Lato
    fontSize: "50px"
    fontWeight: 700
  help-title:
    fontFamily: Lato
    fontSize: "42px"
    fontWeight: 700
  title:
    fontFamily: Lato
    fontSize: "30px"
    fontWeight: 700
  body:
    fontFamily: Lato
    fontSize: "24px"
    fontWeight: 400
  label:
    fontFamily: Lato
    fontSize: "22px"
    fontWeight: 400
  control:
    fontFamily: Lato
    fontSize: "22px"
    fontWeight: 400
  control-selected:
    fontFamily: Lato
    fontSize: "22px"
    fontWeight: 700
  caption:
    fontFamily: Lato
    fontSize: "23px"
    fontWeight: 700
  supporting:
    fontFamily: Lato
    fontSize: "18px"
    fontWeight: 400
  button:
    fontFamily: Lato
    fontSize: "20px"
    fontWeight: 700
  play:
    fontFamily: Lato
    fontSize: "38px"
    fontWeight: 900
rounded:
  choice: "7px"
  action: "10px"
  menu-panel: "12px"
spacing:
  choice-gap: "2px"
  focus-offset: "4px"
  choice-inset: "8px"
  action-inset: "16px"
  panel-inset: "24px"
  modal-inset: "36px"
  menu-inset: "42px"
  help-inset: "48px"
components:
  button-primary:
    backgroundColor: "{colors.accent}"
    textColor: "{colors.panel-strong}"
    typography: "{typography.button}"
  button-primary-hover:
    backgroundColor: "{colors.action-hover}"
  button-primary-pressed:
    backgroundColor: "{colors.action-pressed}"
  button-secondary:
    backgroundColor: "{colors.neutral-button}"
    textColor: "{colors.text}"
    typography: "{typography.button}"
  button-secondary-hover:
    backgroundColor: "{colors.neutral-hover}"
  button-secondary-pressed:
    backgroundColor: "{colors.neutral-pressed}"
  choice:
    backgroundColor: "{colors.choice}"
    textColor: "{colors.muted-text}"
    typography: "{typography.control}"
    rounded: "{rounded.choice}"
    height: "64px"
  choice-selected:
    backgroundColor: "{colors.choice-selected}"
    textColor: "{colors.text}"
    typography: "{typography.control-selected}"
    rounded: "{rounded.choice}"
    height: "64px"
  play:
    backgroundColor: "{colors.accent}"
    textColor: "{colors.panel-strong}"
    typography: "{typography.play}"
    rounded: "{rounded.action}"
    width: "558px"
    height: "70px"
  menu-panel:
    backgroundColor: "{colors.menu-panel}"
    textColor: "{colors.text}"
    rounded: "{rounded.menu-panel}"
    width: "642px"
    height: "684px"
  hud-panel:
    backgroundColor: "{colors.panel}"
    textColor: "{colors.text}"
---

# Design System: Xadrez CGI

## Overview

**Creative North Star: "Palco da turma"**

Os personagens próprios apresentam o xadrez da turma. A identidade Feevale, os professores 3D e o cenário de estudo compõem um ambiente acolhedor de partida, com verde profundo, texto claro e amarelo pontual. A assinatura institucional permanece separada do título do trabalho acadêmico, sem apresentar o jogo como produto oficial da universidade.

A composição aprovada **Mesa de partida** atualiza a expressão do menu: tabuleiro cenográfico, livros e planta acolhem as peças personalizadas, enquanto uma superfície verde concentra a configuração. A imagem aprovada é referência de composição; o cenário raster, os prefabs 3D e os controles Unity são camadas independentes. Essa composição pertence ao menu e não impõe um cenário ilustrado a todas as telas.

Registro mesclado em 22/09/2026 a partir de `GameHud.cs`, `GameHud.Menu.cs`, `GameHud.Match.cs`, `MenuCastPreview.cs`, `MenuSurface.cs` e `MenuGroundShadow.cs`; referência visual em `.impeccable/mocks/approved/mesa-de-partida.png`. Os tokens descrevem a implementação atual. Aprovação da direção e inspeção de capturas não equivalem a aceite funcional ou a validação em dispositivo VR.

**Key Characteristics:**
- Professores existentes do projeto, renderizados em 3D, como presença principal.
- Cenário de estudo independente dos textos, da marca e dos controles.
- Marca Feevale original, proporcional e alinhada pelo conteúdo visível.
- Seleção persistente com cor, peso e ícone; foco com contorno próprio.
- Interface Unity uGUI nativa, com medidas lógicas e contratos de Canvas world-space.

## Colors

Verdes profundos dão continuidade ao cenário e aos painéis; branco e verde claro sustentam a leitura; amarelo identifica a ação principal e as escolhas confirmadas.

### Primary
- **Amarelo de ação** (`accent`): Jogar, bordas de seleção e marcador de confirmação. `play-border` e `play-depth` formam a borda iluminada e a pequena base do botão. Os botões comuns do HUD usam `action-hover` e `action-pressed`.

### Neutral
- **Verde de fundo** (`overlay`): campo exterior que completa a área não ocupada pela composição.
- **Verde de configuração** (`menu-panel`, `menu-border`): superfície quase opaca sobre o cenário, com contorno discreto.
- **Verdes de escolha** (`choice`, `choice-selected`, `choice-border`): repouso, seleção persistente e borda não selecionada.
- **Verdes do HUD** (`panel`, `panel-strong`, `preview-surface`): informação de partida e preview da peça selecionada.
- **Texto claro** (`text`, `muted-text`): informação principal, legenda e apoio; a seleção também passa o texto de apoio para branco.
- **Verdes de ação auxiliar** (`neutral-button`, `neutral-hover`, `neutral-pressed`): botões compartilhados pelo HUD e pelos diálogos.

**The Foco separado Rule.** Seleção usa fundo, borda amarela, texto em negrito e ícone de confirmação. Foco usa contorno claro exterior, inclusive sobre uma escolha não selecionada. Um estado não substitui o outro.

## Typography

**Display Font:** Lato Black incorporada ao projeto.
**Body Font:** Lato Regular; Lato Bold para títulos, escolhas selecionadas e ações comuns.

Lato é a alternativa redistribuível adotada; Avenir permanece referência institucional, sem ter sido incorporada ou renomeada. As fontes vêm de Google Fonts, com SIL Open Font License 1.1 em `game/Assets/Resources/UI/OFL.txt` e proveniência em `ORIGINS.md`.

Os valores do frontmatter representam `Text.fontSize` em unidades lógicas do Canvas. A notação `px` permite transportar o registro, sem prometer pixels físicos ou tamanho angular em VR. Não há tracking ou altura de linha personalizados. O fallback de emergência para fontes internas da Unity recupera recursos ausentes; não é uma alternativa visual aprovada.

### Hierarchy
- **Display:** título Xadrez em Black; a sigla CGI permanece uma marca secundária amarela.
- **Headline / Help title:** Nova partida e Como jogar, respectivamente.
- **Title / Body:** título e explicação do modo local; instruções de ajuda.
- **Label / Control:** rótulos de grupos e opções; seleção aplica `control-selected` sem mudar a escala nominal.
- **Caption / Supporting:** nome do professor em evidência e função/resumo. O outro professor usa Lato Regular em 20 unidades.
- **Button / Play:** ações comuns e ação principal do menu; Jogar usa Black para sustentar sua prioridade.

**The Leitura antes da escala Rule.** O menu compensa a redução do Canvas: controles buscam 16 pixels efetivos; rótulos comuns, 14; textos nominais a partir de 28, 18; créditos nominais até 16, 12. A compensação usa piso de escala 0,4 e altura mínima de caixa de 1,25 vezes o tamanho calculado. Isso não garante leitura em viewports arbitrariamente pequenos e não é uma regra aplicada a todo o HUD.

## Layout

O Canvas desktop usa referência 1920 × 1080, `ScaleWithScreenSize` e equilíbrio entre largura e altura de 0,5. Dentro dele, a composição aprovada ocupa uma superfície lógica de **1672 × 941**, centralizada e reduzida uniformemente para caber na área disponível, com reserva total de duas unidades por eixo. A escala mínima é 0,1. Não existem breakpoints, recorte do cenário para preencher a tela ou rearranjo em coluna única; proporções diferentes deixam campo verde exterior.

O menu posiciona os professores à esquerda e a configuração à direita. A superfície de configuração começa em `(965, 172)`, tem recuo lateral `menu-inset` e largura interna 558. Modo, lado, dificuldade, resumo, Jogar e Como jogar seguem a sequência de interação. No modo local, a explicação da partida substitui lado e dificuldade. As linhas usam `HorizontalLayoutGroup`, intervalo `choice-gap` e altura comum; Intermediário recebe mais largura para conservar o nome completo.

O enquadramento 3D ocupa 645 × 588 unidades. Legendas e sombras acompanham a projeção das bases dos professores, em vez de posições fixas na ilustração. O centro visível da assinatura Feevale acompanha o eixo da superfície de configuração, compensando a margem transparente do PNG.

O HUD da partida conserva sua distribuição funcional: título no alto esquerdo, turno e histórico no alto direito, preview da peça selecionada abaixo e ações na base esquerda. Os recuos do HUD, diálogos e ajuda permanecem tokens próprios; a composição do menu não os substitui.

## Elevation & Depth

O menu combina perspectiva do cenário, volume real dos professores e superfícies uGUI de relevo discreto. A profundidade serve à relação entre personagem, base e tabuleiro, e à prioridade de Jogar. O HUD e os diálogos conservam camadas tonais e contornos, sem herdar o relevo do menu.

### Shadow Vocabulary
- **Contato das bases:** elipses de 240 × 55, cor Unity `(0.015, 0.025, 0.017, 0.68)`, com queda de alpha `(1 − r²)³`. A posição segue cada base projetada.
- **Apoio de legenda:** elipses de 540 × 124, cor `(0.01, 0.025, 0.018, 0.8)`, núcleo sólido até 75% do raio e transição suave até a borda. Mantêm a leitura sobre casas claras e escuras.
- **Recorte de letra:** `UnityEngine.UI.Shadow`, cor `(0.015, 0.025, 0.018, 0.85)` e distância `(1, −2)` nas legendas dos professores. É apoio de contraste, não elevação de painéis.
- **Volume de superfície:** `MenuSurface` modula RGB verticalmente de 95% a 100%; Jogar usa 98% a 100% e uma base inferior própria. Não é uma sombra dura deslocada para decorar cartões.
- **Contorno do HUD:** `CreatePanel` acrescenta `Outline` quando alpha supera 0,75, com cor `(0.45, 0.57, 0.56, 0.15)` e distância `(1, −1)`.

A troca de lado altera escala, profundidade e luminosidade dos professores com interpolação exponencial de fator 9 por segundo e tempo não escalado. A seleção vai de 0 para brancas a 1 para pretas; o modo local usa 0,5 e dá ênfase igual aos dois. O estúdio renderiza ao inicializar ou durante a transição, sem animação decorativa contínua. Botões usam transição de cor de 0,12 segundo.

**The Contraste acompanha o personagem Rule.** Ao deslocar ou redimensionar um professor, reposicionar junto sua sombra de contato e o apoio escuro da legenda; verificar a leitura sobre ambas as cores do tabuleiro cenográfico.

## Shapes

O menu usa cantos suavemente arredondados gerados por `MenuSurface`, com os raios `choice`, `action` e `menu-panel`. Não depende de um cartão inteiro rasterizado. A borda das opções tem 1,5 unidade; a do painel e de Jogar, duas. O foco é exterior, afastado por `focus-offset`, com borda de duas unidades e raio 10 nas escolhas e 13 em Jogar.

O marcador de seleção é um círculo com confirmação, um PNG proveniente de Bootstrap Icons; não é um caractere de fonte. Painéis e botões comuns do HUD continuam retangulares. A divisória inferior é uma linha sutil de uma unidade; a nova seleção do menu não usa o antigo sublinhado.

## Components

### Buttons

**Jogar** é amplo, amarelo e tem volume discreto. Usa a largura interna do painel, rótulo centralizado em Lato Black, borda iluminada e base inferior de cor `play-depth`. O foco possui contorno claro próprio, sem alterar a seleção dos grupos.

Escolhas e Jogar usam `ColorBlock` como multiplicador da superfície: branco no repouso e foco, `(0.91, 1, 0.94)` no hover, `(0.72, 0.82, 0.75)` ao pressionar e `(0.45, 0.5, 0.45)` quando desabilitados. Esses valores não substituem os preenchimentos dos tokens. Os botões comuns do HUD continuam usando os estados absolutos `action-*` e `neutral-*`. Como jogar tem fundo transparente em repouso e rótulo em negrito.

### Escolhas de modo, lado e dificuldade

Controles contíguos, com rótulos centralizados. Seleção combina borda amarela, fundo verde mais claro, Lato Bold e confirmação amarela de 30 × 30. A caixa de texto reserva espaço para o marcador somente quando selecionada. `SelectionCheck.png` deriva de `check-circle-fill` do Bootstrap Icons 1.13.1, sob licença MIT, conforme `ORIGINS.md`.

### Navegação e diálogos

A navegação explícita por teclado acompanha o modo ativo. Ajuda, promoção e falha da IA mudam o escopo de foco e bloqueiam os controles de fundo. O componente visual arredondado é filho do botão; o `Image` original conserva a área de interação dos raios desktop e XR. Isso preserva o contrato uGUI e não constitui evidência de teste de dispositivo VR.

### Painéis e diálogos

A configuração sobrepõe uma superfície verde quase opaca ao cenário; controles continuam elementos nativos independentes. HUD, ajuda, promoção e erro conservam o vocabulário tonal anterior, com dimensões adequadas ao conteúdo e fundo escuro de bloqueio nos diálogos.

### Palco dos professores

O menu instancia `Queen_Marta` com base branca e `King_Ricardo_Carioca` com base preta. Os prefabs e seus materiais próprios permanecem preservados; instâncias de apresentação recebem escala e luz de estúdio. Na partida contra IA, o lado escolhido avança, cresce e ganha luminosidade. No modo local, os dois recebem ênfase igual. Nomes, função e sombras acompanham a projeção das bases.

O `RawImage` recebe uma `RenderTexture` transparente de 1290 × 1176, com antialiasing 4×. O cenário raster separado, `MenuStudyBackground.png`, contém apenas ambiente, tabuleiro, livros e planta. Não contém marca, textos, professores nem controles, e não aparece no tabuleiro da partida. A iluminação do estúdio é isolada durante a renderização e os estados externos são restaurados antes da câmera principal.

### Assinatura Feevale

O PNG original conserva 950 × 369, sem compressão e com NPOT Scale None. O `RawImage` mantém a proporção original. O alinhamento compensa o centro horizontal do conteúdo visível em `473,5 / 950` da largura, incluindo o emblema, para centrar a assinatura sobre o painel. Proveniência da marca, cenário, ícone e fontes está em `game/Assets/Resources/UI/ORIGINS.md`.

## Do's and Don'ts

### Do:
- **Do** preservar as peças personalizadas e a relação visual com a Feevale.
- **Do** manter a proporção original e o alinhamento óptico da assinatura institucional.
- **Do** manter cenário, professores e controles em camadas independentes.
- **Do** distinguir foco de seleção nas escolhas e em Jogar.
- **Do** verificar legendas sobre casas claras e escuras, acompanhando a posição das bases.
- **Do** verificar leitura e enquadramento no Canvas e viewport de destino.

### Don't:
- **Don't** substituir os professores 3D por retratos gerados nem incorporar textos e controles ao cenário raster.
- **Don't** restaurar os três modelos ou o antigo tabuleiro com borda amarela rejeitados pelo usuário; o tabuleiro cenográfico da composição aprovada é uma decisão posterior e distinta.
- **Don't** aplicar a composição ilustrada ou o relevo do menu indiscriminadamente ao HUD da partida.
- **Don't** tratar medidas lógicas do Canvas como comprovação de legibilidade angular em VR.
- **Don't** transformar inconsistências de captura, recursos ausentes ou falhas de contraste em regras do sistema.
- **Don't** apresentar aprovação da direção visual ou revisão técnica como aceite funcional do produto.
