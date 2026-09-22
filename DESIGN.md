---
name: Xadrez CGI
description: Interface Unity do xadrez com personagens da turma e identidade Feevale.
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
  choice-selected: "#134031"
  choice-selected-hover: "#18503A"
  choice-hover: "#0F3126"
typography:
  display:
    fontFamily: Lato
    fontSize: "46px"
    fontWeight: 900
  headline:
    fontFamily: Lato
    fontSize: "42px"
    fontWeight: 700
  title:
    fontFamily: Lato
    fontSize: "30px"
    fontWeight: 700
  body:
    fontFamily: Lato
    fontSize: "22px"
    fontWeight: 400
  label:
    fontFamily: Lato
    fontSize: "19px"
    fontWeight: 400
  control:
    fontFamily: Lato
    fontSize: "22px"
    fontWeight: 700
  button:
    fontFamily: Lato
    fontSize: "20px"
    fontWeight: 700
  play:
    fontFamily: Lato
    fontSize: "27px"
    fontWeight: 700
spacing:
  difficulty-gap: "8px"
  choice-gap: "12px"
  action-inset: "16px"
  panel-inset: "24px"
  modal-inset: "36px"
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
  choice-selected:
    backgroundColor: "{colors.choice-selected}"
    textColor: "{colors.text}"
    typography: "{typography.control}"
  play:
    backgroundColor: "{colors.accent}"
    textColor: "{colors.panel-strong}"
    typography: "{typography.play}"
    width: "486px"
    height: "72px"
  hud-panel:
    backgroundColor: "{colors.panel}"
    textColor: "{colors.text}"
---

# Design System: Xadrez CGI

## Overview

**Creative North Star: "Palco da turma"**

Os personagens próprios apresentam o xadrez da turma. A direção escolhida pelo usuário combina a identidade Feevale com o elenco 3D do projeto; a assinatura institucional permanece separada do título do trabalho acadêmico.

Este documento registra o código observado no commit `444133e`: `GameHud.cs`, `GameHud.Menu.cs`, `GameHud.Match.cs` e `MenuCastPreview.cs`. A composição atual usa campo verde profundo, tipografia clara, ação amarela e controles à direita. O tom “sóbrio” continua sendo hipótese de implementação, não preferência aprovada nem aceite visual final.

**Key Characteristics:**
- Personagens existentes do projeto como elemento visual principal.
- Marca Feevale original e proporcional, sem apresentar o jogo como produto oficial.
- Cor e tipografia separam ação, informação e estado.
- Interface Unity uGUI nativa, compartilhando contratos com o Canvas world-space.

## Colors

O verde organiza o campo e os painéis; branco e verde claro sustentam a leitura; amarelo identifica ações e seleção.

### Primary
- **Amarelo de ação** (`accent`): botão principal, marcadores de seleção e informações destacadas. Os estados de interação usam `action-hover` e `action-pressed`.

### Neutral
- **Verde de fundo** (`overlay`): campo contínuo do menu.
- **Verdes de painel** (`panel`, `panel-strong`, `preview-surface`): HUD, painel da peça selecionada e fundo de seu preview.
- **Texto claro** (`text`, `muted-text`): informação principal e rótulos de apoio.
- **Verdes de controle** (`neutral-button`, `neutral-hover`, `neutral-pressed`): ações secundárias.
- **Verdes de escolha** (`choice-selected`, `choice-selected-hover`, `choice-hover`): estado persistente e passagem do ponteiro nas opções do menu.

**The Foco separado Rule.** Nas opções do menu, foco usa contorno claro; seleção usa fundo verde e sublinhado amarelo. Um estado não substitui o outro.

## Typography

Lato Regular, Bold e Black são arquivos incorporados ao projeto. Lato é a alternativa redistribuível adotada; Avenir é referência institucional, não fonte incorporada.

Os tokens representam os tamanhos nominais de `Text.fontSize`, em unidades lógicas do Canvas; a notação `px` permite transportar o registro, sem prometer tamanho físico ou angular em VR. `display` identifica o título Xadrez, `headline` os títulos de menu e ajuda, `title` o professor, `body` descrições, `label` rótulos, `control` escolhas e `button` ações comuns. `play` é a variação da ação Jogar. Não há espaçamento entre letras ou altura de linha personalizados registrados no código.

O menu compensa a redução de escala: controles buscam 16 pixels efetivos; rótulos comuns, 14; títulos nominais a partir de 28, 18; créditos de tamanho nominal até 16, 12. A compensação usa um piso de escala de 0,4, portanto esses valores não garantem leitura em qualquer viewport arbitrariamente pequeno. A caixa de texto cresce até pelo menos 1,25 vezes o tamanho calculado. O ajuste pertence ao menu, não a todo o HUD.

## Layout

O Canvas desktop usa referência 1920 × 1080 com `ScaleWithScreenSize` e equilíbrio entre largura e altura de 0,5. O menu ocupa uma superfície lógica central de 1600 × 960, reduzida continuamente para caber na área disponível, com reservas de 80 na largura e 64 na altura. Não existem breakpoints declarados.

A superfície atual reúne palco à esquerda e coluna de configuração de largura 486 à direita. Modo, lado, dificuldade, resumo e Jogar seguem a ordem da interação. No modo local, a explicação da partida substitui lado e dificuldade. Esta composição pertence ao menu; não impõe a mesma coluna a novas superfícies.

O HUD da partida ancora título no alto esquerdo, turno e histórico no alto direito, preview da peça selecionada abaixo e ações na base esquerda. Os espaçamentos nomeados no frontmatter descrevem os intervalos e recuos efetivamente reutilizados.

## Elevation & Depth

Painéis usam camadas tonais, sem sombras uGUI. `CreatePanel` acrescenta `Outline` discreto quando a opacidade supera 0,75: cor Unity `(0.45, 0.57, 0.56, 0.15)` e distância `(1, -1)`. Esse contorno não é uma sombra elevada.

O palco produz profundidade real com câmera ortográfica e luzes de estúdio. A troca de lado altera escala, profundidade e luminosidade do personagem; a interpolação exponencial usa fator 9 por segundo e tempo não escalado. Botões usam transição de cor de 0,12 segundo.

## Shapes

Painéis e controles são retângulos de `Image`, sem sprite de cantos arredondados ou escala de raios. Divisórias usam linha de uma unidade lógica; as opções selecionadas têm marcador inferior de duas unidades. O foco das opções forma um contorno de duas unidades nas quatro bordas.

## Components

### Buttons

Botões principais têm fundo amarelo, texto verde escuro e rótulo centralizado. Secundários usam verde e texto branco. O foco de botões comuns usa a cor de hover definida no `ColorBlock`; o contorno separado é específico das opções do menu. Jogar ocupa toda a largura da coluna. Como jogar no menu tem fundo transparente em repouso.

### Escolhas de modo, lado e dificuldade

Rótulos alinhados à esquerda, fundo transparente quando inativos e estado selecionado persistente. O hover tem cores próprias para opções selecionadas e não selecionadas. A navegação explícita por teclado acompanha o modo ativo. Ajuda e diálogos deslocam o escopo de foco e bloqueiam a interação com os controles de fundo.

### Painéis e diálogos

HUD e diálogos usam os mesmos verdes e texto claro. Ajuda, promoção e falha da IA aparecem sobre uma camada escura; os cartões centrais têm dimensões próprias do conteúdo, sem sistema de elevação adicional.

### Palco dos professores

O menu instancia Marta com base branca e Ricardo com base preta. Na partida contra IA, o lado escolhido avança, cresce e recebe maior luminosidade; no modo local, os dois recebem ênfase igual. O nome e a função abaixo do palco acompanham a escolha. Os modelos e seus materiais próprios são preservados.

### Assinatura Feevale

O PNG original mantém 950 × 369, importação sem compressão e NPOT Scale None. O `RawImage` calcula a altura pela proporção da textura. Proveniência e licença das fontes estão em `game/Assets/Resources/UI/ORIGINS.md`.

## Do's and Don'ts

### Do:
- **Do** preservar os personagens próprios e a relação visual com a Feevale.
- **Do** manter a proporção original da assinatura institucional.
- **Do** distinguir foco de seleção nas escolhas do menu.
- **Do** verificar leitura e enquadramento no Canvas e viewport de destino.

### Don't:
- **Don't** substituir a identidade do projeto por uma interface genérica.
- **Don't** restaurar no menu os três modelos ou o antigo tabuleiro com borda amarela rejeitados pelo usuário.
- **Don't** tratar medidas lógicas do Canvas como comprovação de legibilidade angular em VR.
- **Don't** apresentar uma hipótese de tom visual ou uma revisão técnica como aprovação estética do usuário.
