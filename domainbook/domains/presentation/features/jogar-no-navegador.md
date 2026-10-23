---
id: jogar-no-navegador
name: Apresentar a partida no navegador
status: draft
owners: [mathwidu]
terms: [hud, peça-personalizada, preview-da-peça-selecionada, tela-de-resultado]
---

## Story

Como jogador no navegador
Quero ler e operar a partida em retrato, paisagem e tablet
Para jogar com os personagens sem esconder o tabuleiro atrás dos painéis.

## Rule: O HUD reserva uma região para o tabuleiro

```gherkin
Example: Jogar em retrato
  Given um navegador de aproximadamente 360 por 640
  Then turno e status aparecem no topo
  And ações e botões de câmera aparecem embaixo
  And o tabuleiro e os personagens são enquadrados na faixa livre entre as barras

Example: Girar para paisagem curta
  Given a altura lógica disponível é menor que 500 e a vista é paisagem
  Then informações ficam à esquerda e controles à direita
  And a região central fica livre para o tabuleiro

Example: Usar área segura
  Given a tela informa uma safe area
  Then controles e diálogos ficam dentro dessa área
  And o reenquadramento considera essa mesma área
```

## Rule: Informações adicionais ficam disponíveis por demanda

```gherkin
Example: Inspecionar a peça
  Given uma peça está selecionada
  Then a seleção não cria nem renderiza o preview de alta resolução automaticamente
  When o jogador abre Peça
  Then o preview mostra o personagem com giro, deslocamento e zoom
  When fecha o painel
  Then o clone é liberado e a câmera de preview deixa de renderizar
  And a seleção da partida é preservada

Example: Ver os últimos lances
  When o jogador abre Lances
  Then o histórico aparece em diálogo
  And as ações da partida ficam indisponíveis até fechar
```

## Rule: Menu e escolhas obrigatórias continuam legíveis

```gherkin
Example: Configurar em uma tela estreita
  Then o menu reorganiza personagens e opções em uma coluna
  And o conteúdo excedente pode ser rolado verticalmente
  And os botões têm pelo menos 44 unidades lógicas de altura

Example: Promover ou terminar a partida
  Then a promoção, falha da IA e tela de resultado cabem na área segura
  And cada escolha continua sendo um botão uGUI
```

`GameHud.Web.cs` aplica essa composição apenas quando `WebTouchInput.Enabled`
está ativo e não há headset. Os builders desktop e world-space permanecem no
fluxo existente. A composição Web usa largura lógica 360 em retrato e até 720
unidades de altura em paisagem. O menu usa ScrollRect com viewport mascarada,
conteúdo de altura explícita e os mesmos botões, escolhas e personagens 3D.
No Web, o preview usa uma textura 736 × 440 somente após abrir Peça; a textura é
reutilizada após a primeira abertura e liberada quando o HUD é destruído.
O menu usa 645 × 588 e ambos os previews usam MSAA 2×. Os tamanhos e MSAA
das versões desktop e Quest permanecem iguais aos anteriores.

Verificação local:

No Editor, antes de carregar `Main`, definir
`WebTouchInput.EditorPreviewEnabled = true`. Para testes de geometria sem mudar
o Game view, definir também `EditorViewportSize` e, opcionalmente,
`EditorSafeArea`. Zerar esses dois valores e desativar o override no teardown.
O override é transitório, existe apenas no Editor e não muda o modo desempenho
nem os assets de renderização.

`WebLayoutTests` verifica 360 × 640, 640 × 360, 768 × 1024 e 1024 × 768,
diálogos na área segura, fontes de botões, preview por demanda e bounds reais
projetados na região livre do HUD. O principal executa a suíte PlayMode e a
revisão visual no build Web; esses testes não substituem a validação móvel real.

## Open Questions

Validar legibilidade física, safe areas reportadas pelo navegador e desempenho
no aparelho. O primeiro enquadramento busca mostrar tudo; o zoom manual pode
ampliar além desse limite, e Voltar restaura a vista completa.
