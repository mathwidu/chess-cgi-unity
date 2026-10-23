---
id: preview-the-selected-piece
name: Ver o preview da peça selecionada
status: ready
owners: [mathwidu]
terms: [preview-da-peça-selecionada, peça-personalizada]
decisions: [presentation/ADR-0001]
---

## Story

Como jogador que escolheu uma peça
Quero vê-la sozinha e girá-la
Para que eu consiga ler qual personagem é e observar o modelo

## Rule: Selecionar uma peça a mostra em seu próprio preview

```gherkin
Example: O painel aparece com a peça selecionada
  Given nenhuma peça está selecionada e o painel está oculto
  When uma peça é selecionada
  Then o painel da peça selecionada aparece
  And ele mostra essa peça renderizada sozinha, com seu nome e sua casa

Example: Limpar a seleção oculta o painel
  Given uma peça está selecionada e o painel está exibido
  When a seleção é limpa
  Then o painel é ocultado
  And a peça do preview é desfeita
```

## Rule: O preview pode ser girado e ter o zoom ajustado sem tocar o tabuleiro

```gherkin
Example: Arrastar gira apenas o preview
  Given o preview da peça selecionada está exibido
  When o jogador arrasta sobre ele
  Then a peça do preview gira
  And nenhuma peça do tabuleiro se move

Example: Os botões de zoom e a roda mudam a distância do preview
  Given o preview da peça selecionada está exibido
  When o jogador usa o botão "+" ou "-", ou rola a roda sobre o preview
  Then a câmera do preview se move para mais perto ou mais longe
  And a câmera do tabuleiro permanece inalterada
```

## Open Questions

Nenhuma.
