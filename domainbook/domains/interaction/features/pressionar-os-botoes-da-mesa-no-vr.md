---
id: pressionar-os-botoes-da-mesa-no-vr
name: Pressionar os botões da mesa no VR
status: ready
owners: [mathwidu]
terms: [modo-vr, controle-de-movimento, rastreamento-de-mãos]
---

## Story

Como jogador no modo VR
Quero encostar e apertar os botões da mesa com o controle ou o indicador
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
  Given o rastreamento de mãos ativo com pose válida do indicador
  When o jogador encosta e empurra com a ponta do indicador
  Then o contato físico acompanha a ponta rastreada
  And o botão afunda e retorna por mola quando solto
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
```

`XRRig` cria os `XRPhysicsPusher` fora da hierarquia dos transforms rastreados.
Cada proxy tem um `Rigidbody` cinemático e uma esfera que avança por
`MovePosition` em `FixedUpdate`. Os controles têm contato no ponto de pega e
no indicador do modelo; mãos usam o `IndexTip` do `XRHandSkeletonDriver` e
a validade da pose reportada pelo `XRHandSubsystem`.

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
