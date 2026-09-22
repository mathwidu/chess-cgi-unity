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
  Then ambas as opções recebem sublinhado amarelo e destaque de seleção
  And o resumo mostra Contra IA, Difícil e Pretas
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

## Open Questions

A legibilidade e o conforto no headset ainda exigem validação em hardware.
