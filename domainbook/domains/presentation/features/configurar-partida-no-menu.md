---
id: configurar-partida-no-menu
name: Configurar a partida no menu
status: ready
owners: [mathwidu]
terms: [hud]
---

## Story

Como jogador
Quero comparar os modos e as dificuldades antes de começar
Para que eu inicie uma partida com a configuração que escolhi

## Rule: As escolhas da partida são explícitas

```gherkin
Example: Escolher dificuldade e lado contra a IA
  Given o menu está aberto no modo Contra IA
  When o jogador escolhe Difícil e Pretas
  Then ambas as opções recebem contorno amarelo e marcador de confirmação
  And uma descrição explica a dificuldade escolhida
  When o jogador inicia a partida
  Then o controlador recebe o lado e a dificuldade escolhidos
  And a IA faz o primeiro lance

Example: Escolher dois jogadores
  Given o jogador configurou uma partida contra IA
  When ele escolhe Dois jogadores
  Then lado e dificuldade da IA ficam ocultos
  And o menu informa que as brancas começam
  When ele volta a Contra IA
  Then as escolhas anteriores continuam selecionadas
```

## Rule: A ajuda preserva a configuração e concentra a interação

```gherkin
Example: Consultar as instruções
  Given o jogador escolheu uma dificuldade no menu
  When ele abre Como jogar
  Then os botões por trás da ajuda ficam indisponíveis
  And o foco de navegação vai para o botão de fechar a ajuda
  When ele fecha a ajuda
  Then a configuração escolhida é preservada
  And os botões do menu voltam a responder
```

## Rule: O menu cabe no Canvas e a partida mantém sua identificação

```gherkin
Example: Redimensionar o Game view
  Given o menu está aberto
  When a área do Canvas muda
  Then a composição se ajusta sem cortar controles ou rótulos

Example: Retornar ao menu
  Given uma partida contra IA está em andamento
  Then o HUD identifica a dificuldade e o lado humano
  When o jogador volta ao menu
  Then suas escolhas permanecem selecionadas durante a sessão
```

## Rule: A navegação por teclado acompanha o contexto

```gherkin
Example: Iniciar sem usar o mouse
  Given a cena Main acabou de entrar em Play
  Then o primeiro controle do menu recebe foco
  When o jogador usa as setas e Enter para escolher Dois jogadores e Jogar
  Then a partida local começa
  And o foco passa para as ações da partida

Example: Uma escolha obrigatória interrompe as ações comuns
  Given a partida pede promoção ou apresenta falha da IA
  Then o foco passa para o primeiro botão desse diálogo
  And as ações comuns da partida ficam indisponíveis
```

## Rule: O palco apresenta os dois lados sem alterar a partida

```gherkin
Example: Escolher o lado humano
  Given o menu está aberto contra IA
  When o jogador escolhe Brancas
  Then a professora Marta aparece maior e em primeiro plano
  When o jogador escolhe Pretas
  Then o professor Ricardo aparece maior e em primeiro plano
  And a configuração e o nome visível correspondem ao lado escolhido
  And cada nome acompanha a base do professor correspondente
  When o jogador escolhe Dois jogadores
  Then os dois professores recebem o mesmo destaque
```

## Rule: A composição mantém identidade e alinhamento

```gherkin
Example: Apresentar a marca sobre as escolhas
  Given o menu mostra o painel de configuração à direita
  Then a parte visível da logo Feevale fica centralizada no eixo desse painel
  And a textura mantém sua proporção original
  And o cenário ilustrado não bloqueia os controles

Example: Separar seleção e foco
  Given uma opção de cada grupo está selecionada
  Then somente ela tem o marcador amarelo de confirmação
  When o foco do teclado muda para outra opção
  Then o contorno claro indica foco sem mudar a seleção
```

A composição aprovada é `.impeccable/mocks/approved/mesa-de-partida.png`.
O fundo cenográfico é um asset separado; texto e controles são uGUI nativos.
Os professores continuam sendo peças personalizadas 3D renderizadas durante
as transições de lado. `MenuSurface` desenha os controles na resolução do
Canvas; `MenuGroundShadow` acompanha as bases, sem afetar a partida.

## Open Questions

A legibilidade e o conforto no headset ainda exigem validação em hardware.
