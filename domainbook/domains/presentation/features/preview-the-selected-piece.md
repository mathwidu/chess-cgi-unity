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
Quero vê-la sozinha, girá-la e ajustar seu enquadramento
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

## Rule: O preview pode ser girado, deslocado e aproximado sem tocar o tabuleiro

```gherkin
Example: Arrastar gira apenas o preview
  Given o preview da peça selecionada está exibido
  When o jogador arrasta sobre ele com o botão esquerdo
  Then a peça do preview gira no eixo horizontal
  And o movimento vertical muda a altura de observação, com inclinação limitada
  And nenhuma peça do tabuleiro se move

Example: Os botões de zoom e a roda mudam a distância do preview
  Given o preview da peça selecionada está exibido
  When o jogador usa "+ Zoom" ou "− Afastar", ou rola a roda sobre o preview
  Then a câmera do preview se move para mais perto ou mais longe
  And a câmera do tabuleiro permanece inalterada

Example: O zoom preserva o topo do personagem
  Given a vista foi restaurada e o jogador ainda não a deslocou manualmente
  When aproxima até o zoom máximo de aproximadamente 182%
  Then o enquadramento sobe o necessário para manter margem acima da cabeça e dos acessórios
  And a base e os pés podem sair do quadro nessa aproximação

Example: Arrastar com o botão direito reposiciona o personagem
  Given o jogador quer inspecionar um detalhe no zoom máximo
  When arrasta sobre a imagem com o botão direito ou do meio
  Then o personagem acompanha o movimento horizontal e vertical dentro do preview
  And sua orientação e o percentual de zoom não mudam
  And a câmera da sala e a seleção do tabuleiro permanecem inalteradas
  And o ajuste permanece após a atualização do painel

Example: Botões permitem ajustar a altura no desktop e em VR
  Given o preview da peça selecionada está exibido
  When aciona "Mover ↑" ou "Mover ↓"
  Then o personagem se desloca na direção indicada dentro da imagem
  And o zoom permanece no mesmo percentual

Example: Restaurar recupera a vista inicial
  Given o jogador girou, deslocou e aproximou o personagem
  When aciona "Restaurar"
  Then a peça volta à orientação e posição iniciais do preview
  And a câmera volta ao enquadramento de 100% daquela peça

Example: Os controles permanecem visíveis fora da imagem
  Given uma peça está selecionada
  Then há botões de giro para os dois lados, mover para cima e para baixo, Restaurar, afastar e aproximar
  And o percentual de zoom aparece abaixo da imagem
  And a instrução distingue os botões esquerdo para girar e direito para mover no desktop
  And em VR a instrução indica os botões disponíveis no painel

Example: Chegar ao limite desativa o botão correspondente
  Given o zoom está no limite mais próximo ou mais distante
  Then apenas o botão que ultrapassaria esse limite está desativado
  And Restaurar continua disponível
```

## Rule: O personagem tem proporção correta e os dados não se repetem

```gherkin
Example: Selecionar outro personagem restaura seu enquadramento
  Given o jogador alterou a vista da peça anterior
  When seleciona outra peça
  Then o novo personagem aparece inteiro no enquadramento de 100%
  And não herda o deslocamento da peça anterior
  And a proporção da textura renderizada corresponde ao espaço da imagem
  And o nome, o tipo, o lado e a casa aparecem acima da imagem
  And categoria e registro aparecem em um rodapé compacto, sem repetir o nome
```

## Open Questions

Nenhuma.
