---
id: regular-a-altura-da-mesa
name: Regular a altura da mesa
status: ready
owners: [mathwidu]
terms: [mesa]
decisions: [presentation/ADR-0002]
---

## Story

Como jogador sentado diante do tabuleiro, de óculos VR ou no desktop
Quero subir ou descer a mesa com o tabuleiro em cima
Para ver melhor as peças e jogar numa altura confortável

## Rule: Os botões da mesa movem só a mesa e o tabuleiro

```gherkin
Example: Subir a mesa
  Given uma partida em andamento com a mesa na altura padrão
  When o jogador aciona Subir na placa da mesa
  Then o tampo sobe um passo e o tabuleiro sobe junto, com todas as peças
  And os pés continuam no chão, e as pernas crescem
  And a câmera não se move
  And a placa mostra a nova altura em centímetros e um nível a mais aceso

Example: Descer a mesa
  Given a mesa está acima do limite mínimo
  When o jogador aciona Descer
  Then a mesa e o tabuleiro descem um passo
```

## Rule: No laboratório Feevale, a mesa regulável é a mesa do laboratório

```gherkin
Example: Subir a mesa do laboratório
  Given o laboratório Feevale carregado ao redor do tabuleiro
  When o jogador aciona Subir na placa da mesa
  Then o tampo da mesa de xadrez do laboratório sobe um passo com o tabuleiro
  And os pés em T continuam no chão, e as colunas crescem
  And o resto da sala, o piso e as cadeiras não se movem
```

A mesa de xadrez vem fundida às outras mesas nas malhas do laboratório. O
`TableView` não monta mesa própria: ele desloca, só em Play, os vértices da
malha dentro da área de 1,30 × 0,90 m da mesa de xadrez e acima de 20 cm,
e move os marcadores `ChessTableSurface` e `BoardAnchor` junto. A sala
acompanha a pose do tabuleiro descontando esse deslocamento, então fica
parada no chão. Um collider invisível do tamanho do tampo mantém o bloqueio
do raio do VR. A mesa própria do `TableView` só aparece se o prefab do
laboratório faltar.

## Rule: A altura tem limites e é lembrada

```gherkin
Example: Chegar ao limite máximo
  Given a mesa está um passo abaixo do máximo
  When o jogador aciona Subir
  Then a mesa chega ao máximo
  And o botão Subir fica indisponível, e Descer continua disponível

Example: A altura sobrevive a uma nova partida e a uma nova sessão
  Given o jogador regulou a mesa
  When ele inicia uma nova partida, ou fecha e abre o jogo
  Then o tabuleiro é montado sobre a mesma altura
```

## Rule: A placa fica ao alcance do jogador

```gherkin
Example: A placa acompanha o lado de quem joga
  Given o jogador joga de brancas
  Then a placa fica presa à borda frontal da mesa, abaixo do tampo e à esquerda do jogador
  And a face é inclinada para cima, na direção do assento
  When o jogador passa a jogar de pretas, no assento VR ou na câmera desktop
  Then a placa passa para a esquerda desse jogador

Example: Os controles são maiores e distinguem as duas direções
  Given a placa de controle da mesa
  Then Subir é um botão circular vermelho com uma seta branca para cima
  And Descer é um botão circular azul com uma seta branca para baixo
  And cada botão tem 9 cm de diâmetro na escala do VR
  And a placa acompanha o tampo quando a altura muda

Example: Os botões são apertados com física
  Given a placa de controle no VR
  When o jogador encosta a palma, um dedo ou o controle num botão e empurra
  Then a capa circular afunda ao longo de um curso de 12 mm
  And a mesa muda um passo quando o botão atinge 70% desse curso
  And segurar o botão apertado não repete o comando
  When o jogador afasta a mão ou o controle
  Then a mola traz a capa de volta
  And outro comando exige soltar e apertar novamente

Example: Pressão profunda ou lateral conserva o mecanismo
  Given uma mão empurrando a capa além do fim do curso ou para o lado
  Then a capa conserva seu eixo e sua orientação na placa
  And seu deslocamento fica entre o repouso e os 12 mm de curso
  And a mão visível fica apoiada na face física até ser retirada

Example: O clique desktop pressiona o mesmo mecanismo
  Given o modo desktop
  When o jogador clica num botão
  Then uma força pressiona a capa e o curso físico aciona a mesa
  Given o modo VR
  Then encostar e empurrar o botão funciona sem apertar o gatilho
  And o raio do controle não aciona os botões da mesa
  And no desktop só o disco recebe o clique, sem os cantos vazios do seu retângulo

Example: Um botão indisponível continua sendo uma peça física
  Given a mesa no limite máximo
  When o jogador aperta o botão vermelho
  Then a capa afunda, mas a mesa permanece no máximo
  And habilitar o botão enquanto ele está pressionado não aciona outro passo

Example: A placa fica acessível pela vista desktop
  Given uma partida no desktop, com o enquadramento normal do tabuleiro
  When o jogador aciona Altura da mesa no HUD
  Then a câmera enquadra a placa presa à borda, mantendo os botões clicáveis
  When o jogador aciona Voltar ao tabuleiro ou R
  Then a câmera volta à perspectiva do lado atual
```

No VR, o passo é de 3 cm, entre 64 e 94 cm de altura do tampo, e a altura
padrão é 75,57 cm, a base da moldura do tabuleiro. No desktop, a mesma sala aparece escalada
([ADR-0002](../decisions/0002-modelar-a-sala-em-metros-do-vr-e-escalar-para-o-desktop.md))
e o passo é de 0,25 unidade do tabuleiro, para o tabuleiro não sair do
enquadramento. A escolha fica em `PlayerPrefs` (`ChessCgi.TableHeightStep`).

Cada `PhysicalTableButton` tem uma capa cilíndrica com `MeshCollider` convexo e
`Rigidbody` dinâmico. Um `ConfigurableJoint` limita o deslocamento ao eixo de
pressão; a mola e o amortecimento aplicam força em `FixedUpdate`. A guia também
corrige deslocamentos laterais, rotação e excesso de curso, pois um contato
cinemático rastreado pode superar os limites do solver. A seta acompanha a
capa. Ao mover a mesa ou trocar o lado, capa e âncora são reposicionadas juntas,
preservando o curso e evitando um impulso do solver. Os contatos do VR vêm de
[`XRPhysicsPusher`](../../interaction/features/pressionar-os-botoes-da-mesa-no-vr.md).

## Open Questions

O conforto dos limites, do passo e da pressão física ainda precisa ser validado
em headset com controles e com o rastreamento de mãos. Testes de PlayMode e
capturas com headset simulado verificam a física do Unity, sem comprovar esse
conforto no dispositivo.
