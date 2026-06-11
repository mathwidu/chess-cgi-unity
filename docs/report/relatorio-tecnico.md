# Relatorio tecnico - Xadrez CGI

Aluno: Matheus Duarte
Disciplina: Computacao Grafica I
Projeto: Xadrez CGI
Tecnologia principal: Unity 6.3 LTS, C# e URP

## 1. Descricao e objetivo

O projeto Xadrez CGI e um jogo de xadrez 3D local desenvolvido em Unity para a disciplina de Computacao Grafica I. O objetivo foi construir uma aplicacao interativa que reunisse conceitos de modelagem, organizacao espacial, transformacoes geometricas, animacao, camera, interface e regras de jogo.

A proposta escolhida foi um xadrez tematico de faculdade. O tabuleiro fica em um ambiente simples de sala/aula, com mesa, paredes, quadro e elementos de apoio. As pecas combinam formas classicas de xadrez com personagens personalizados baseados em colegas e professores da turma. O jogo pode ser utilizado por duas pessoas no mesmo computador, alternando turnos entre brancas e pretas.

Do ponto de vista de Computacao Grafica, o projeto busca mostrar uma cena 3D funcional com multiplos objetos organizados no espaco, transformacoes dinamicas, camera interativa, movimentacao das pecas e um HUD que ajuda o jogador a entender o estado da partida. Do ponto de vista de jogo, o objetivo foi entregar uma versao jogavel e estavel, sem depender de validacao manual das regras de xadrez a cada partida.

## 2. Tecnologias utilizadas

O projeto foi desenvolvido em Unity 6.3 LTS, usando C# para a implementacao da logica de jogo, controle de cena, criacao visual das pecas e interface. A renderizacao usa o pipeline padrao do projeto Unity criado para 3D, com materiais simples e luzes ajustadas para manter bom desempenho no Editor.

A biblioteca `ChessDotNet` foi usada para validar as regras de xadrez. Essa decisao reduziu o risco de erro em regras especiais como roque, en passant, promocao, xeque, xeque-mate e empate. Assim, o codigo do projeto concentra-se na adaptacao entre a regra abstrata do xadrez e a apresentacao 3D no Unity.

A interface foi implementada com Canvas/UGUI. O HUD apresenta tela inicial, instrucoes, status da partida, historico de jogadas, painel de promocao e uma aba lateral para a peca selecionada. Essa aba usa uma camera propria e uma `RenderTexture` para exibir um preview 3D independente da cena principal, permitindo girar e aproximar o modelo selecionado.

## 3. Modelagem e organizacao da cena

A cena principal esta em `Assets/Scenes/Main.unity`. A organizacao visual e controlada principalmente por scripts. O `BoardView` cria e sincroniza a representacao do tabuleiro, das casas e das pecas. O `PieceFactory` cria a aparencia das pecas, usando prefabs personalizados quando existem e formas classicas em primitivas como fallback.

O tabuleiro e organizado como uma matriz 8x8. Cada casa possui uma coordenada logica, como `e2`, e uma posicao correspondente no mundo 3D. Essa conversao permite que a regra de xadrez trabalhe com notacao de tabuleiro, enquanto a parte grafica posiciona objetos em coordenadas tridimensionais.

Os personagens personalizados foram colocados em `Assets/Resources/CustomPieces`, permitindo carregamento em runtime. Cada tipo de peca possui um personagem principal:

- Peao: Matheus Duarte, criador do jogo.
- Bispo: Rafael Scharer.
- Cavalo: Gustavo Cornalewski.
- Torre: Alex Fenner.
- Rainha: MARTA ROSECLER BEZ.
- Rei: RICARDO FERREIRA DE OLIVEIRA.

A ambientacao foi mantida simples para evitar excesso de poluicao visual. O foco principal permanece no tabuleiro, nas pecas e nas interacoes do jogador.

## 4. Transformacoes geometricas

As transformacoes geometricas aparecem em diferentes partes do projeto:

- Translacao: as pecas mudam de casa quando o jogador realiza uma jogada. A posicao final e calculada a partir da coordenada da casa no tabuleiro.
- Rotacao: a camera gira para acompanhar o lado do jogador da vez, e o preview da aba lateral pode ser girado pelo usuario.
- Escala: os modelos personalizados sao ajustados para caberem nas casas do tabuleiro e tambem no preview lateral.

Essas transformacoes sao aplicadas por componentes `Transform` do Unity. O uso de coordenadas logicas separadas das coordenadas 3D facilita a manutencao, pois a regra de xadrez nao depende da posicao visual dos objetos.

## 5. Animacao e interatividade

A movimentacao das pecas usa interpolacao entre a posicao inicial e a posicao final, criando uma animacao simples de deslocamento. A camera tambem muda de enquadramento quando o turno alterna entre brancas e pretas, reforcando a ideia de dois jogadores locais.

A interatividade principal acontece por mouse e teclado. O jogador seleciona uma peca, visualiza destinos validos e escolhe a casa de destino. O teclado permite cancelar selecao, iniciar nova partida, girar camera e resetar enquadramento. O scroll controla zoom da camera principal.

A aba lateral aumenta a interatividade com os modelos. Quando uma peca e selecionada, o jogador consegue ver informacoes do personagem e manipular o preview 3D com drag, scroll e botoes de zoom. Isso ajuda a valorizar os personagens criados para o trabalho.

## 6. Logica de jogo e arquitetura

A arquitetura separa regra, controle e visual. O `ChessRulesAdapter` integra a biblioteca `ChessDotNet` e transforma as jogadas do projeto em chamadas para a biblioteca. O `ChessGameController` coordena selecao, turno, promocao, historico e mensagens do HUD. O `BoardView` fica responsavel por refletir o estado do jogo na cena 3D.

Essa divisao foi importante para criar testes automatizados e reduzir dependencia de testes manuais. Os testes EditMode cobrem conversao de casas, regras especiais, fluxo do controller, criacao de tabuleiro, camera, HUD e prefabs customizados.

## 7. Desafios encontrados

Um dos principais desafios foi equilibrar ambicao visual com estabilidade. A ideia inicial evoluiu para personagens personalizados e varias tentativas de animacao mais complexa, mas a entrega precisava continuar jogavel. Por isso, foi preservada uma branch estavel e as animacoes mais arriscadas foram deixadas como melhoria futura.

Outro desafio foi a qualidade dos modelos personalizados. Como o projeto envolve pessoas reais da turma, os modelos precisavam ser reconheciveis, mas sem exigir realismo profissional. A solucao foi adotar uma leitura estilizada de jogo, com prefabs personalizados e uma documentacao de fluxo para novas pecas.

Tambem houve desafio em testar xadrez completo manualmente. Para resolver isso, a regra foi delegada a uma biblioteca consolidada e o projeto recebeu testes automatizados no Test Runner da Unity.

## 8. Conclusao e melhorias futuras

O projeto entrega um xadrez 3D funcional, com regras completas, cena tematica, personagens personalizados, HUD interativo e testes automatizados. Ele atende aos objetivos principais de uma aplicacao grafica interativa: ha multiplos objetos 3D, transformacoes dinamicas, animacao por interpolacao, entrada por usuario e organizacao espacial em cena.

Como melhorias futuras, o projeto pode evoluir para animacoes mais ricas de deslocamento e captura, com personagens rigados caminhando, cavalo saltando, torre impactando a casa e efeitos especificos por tipo de peca. Outras melhorias planejadas incluem IA para jogar contra o computador, modo online, build WebGL, polimento dos modelos e uma interface ainda mais profissional.

Mesmo com essas possibilidades futuras, a versao atual ja e uma base estavel e entregavel para demonstrar os conceitos de Computacao Grafica I dentro de um jogo completo e jogavel.
