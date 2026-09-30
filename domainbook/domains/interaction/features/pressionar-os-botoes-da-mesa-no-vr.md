---
id: pressionar-os-botoes-da-mesa-no-vr
name: Pressionar os botões da mesa no VR
status: ready
owners: [mathwidu]
terms: [modo-vr, controle-de-movimento, rastreamento-de-mãos]
---

## Story

Como jogador no modo VR
Quero encostar e apertar os botões da mesa com o controle, a palma ou um dedo
Para regular a altura da mesa por contato físico

## Rule: O contato empurra a capa do botão

```gherkin
Example: Apertar com um controle de movimento
  Given o controle rastreado ao alcance da placa da mesa
  When o jogador encosta e empurra a capa vermelha ou azul
  Then o contato cinemático do controle empurra o corpo dinâmico da capa
  And atingir o curso de acionamento move a mesa uma vez
  And não é necessário apertar o gatilho

Example: Apertar com uma mão rastreada
  Given o rastreamento de mãos ativo com poses válidas da palma e dos dedos
  When o jogador encosta e empurra com a palma ou um dedo
  Then o contato físico acompanha as articulações rastreadas
  And o botão afunda e retorna por mola quando solto

Example: A mão encontra a face visível
  Given uma mão empurrando a capa
  When o rastreamento avança além da face desenhada
  Then a mão visível continua apoiada na capa, inclusive durante sua interpolação
  And seu material opaco oculta os objetos atrás dela
  And a pose original continua fornecendo a força ao mecanismo
  And o ponto de pega e a mira mantêm a pose original, sem realimentação do deslocamento visual
```

## Rule: Só poses válidas fornecem contato

```gherkin
Example: Perda e retorno do rastreamento
  Given um contato físico ativo
  When o runtime perde o rastreamento ou desativa a modalidade
  Then o contato deixa de colidir
  When o rastreamento retorna com a ponta dentro de uma capa
  Then o contato permanece desarmado
  When a ponta sai da capa e permanece estável por três passos da física
  Then o contato volta a funcionar

Example: Recentrar, trocar assento ou saltar de pose
  When a origem do rastreamento muda ou a pose salta bruscamente
  Then o contato se reposiciona sem varrer o caminho com colisões
  And exige estabilidade fora da capa antes de voltar a colidir

Example: Um passo de altura reposiciona a placa sob uma mão
  Given a palma e os dedos perto da capa durante a pressão
  When um acionamento muda a altura da mesa e da placa
  Then todos os contatos dessa mão ficam desarmados juntos
  And a mão conserva seu apoio visual até ser retirada
  And a pressão não repete o comando na nova posição
  And rearmar exige afastar todos os contatos rastreados dessa mão
```

`XRRig` cria um `XRPhysicalHand` por modelo de mão. Cada um coordena 18
`XRPhysicsPusher` na palma, laterais, nós, pontas e segmentos dos cinco dedos.
Os proxies ficam fora da hierarquia rastreada, com `Rigidbody` cinemático e
esfera movida por `MovePosition` em `FixedUpdate`. O alvo físico respeita o fim
do curso da capa. Se um contato exige rearmar, a mão inteira fica em quarentena
até que suas amostras válidas estejam estáveis fora das capas.

O modelo dos controles reconstrói poses de ossos sem o deslocamento visual;
mãos articuladas usam as poses do `XRHandSubsystem` no espaço de origem e sua
validade. A correção da mão desenhada segue a face interpolada da capa em
`LateUpdate` e antes de renderizar. No `XRHandSkeletonDriver`, ela usa o offset
da raiz e restaura a última pose original ao perder rastreamento. O rig mantém
a mira e o ponto de pega independentes dessa correção.

O alcance próximo dos dois tipos de entrada consulta apenas peças e hastes,
com raio de 6 cm. As casas e o cenário não ocupam o buffer de detecção antes
dos alvos agarráveis; o raio distante continua reservado ao HUD.

As capas usam a layer `TableButtons` (27), e os proxies usam `XRPhysicalHands`
(28). A exclusão no próprio collider restringe o contato dos proxies às capas,
preservando as peças e o ambiente. Os casts de interação e o bloqueio do raio
do HUD excluem os proxies; tampo, peças e ambiente continuam bloqueando o raio.
Os botões da mesa não são alvos do raio no VR. As regras de curso, retorno,
limites e altura estão em
[Regular a altura da mesa](../../presentation/features/regular-a-altura-da-mesa.md).

## Open Questions

O alcance e a sensação de pressão precisam ser conferidos em headset real.
Os testes locais verificam colisões, retorno e rastreamento simulado no motor
de física, sem medir a precisão do rastreamento ou o conforto no dispositivo.
