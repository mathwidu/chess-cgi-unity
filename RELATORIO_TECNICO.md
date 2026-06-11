# Relatório Técnico - Xadrez CGI

**Disciplina:** Computação Gráfica
**Projeto:** Xadrez 3D interativo em Unity
**Aluno:** Matheus Duarte
**Matrícula:** 0276899
**Data:** 11/06/2026

## 1. Objetivo

O objetivo do projeto foi desenvolver uma aplicação 3D interativa utilizando conceitos de Computação Gráfica em um jogo de xadrez completo. O projeto busca demonstrar criação de cena, modelagem e composição de objetos 3D, interação com usuário, transformações, câmera, iluminação, materiais, animações simples e organização de assets dentro da Unity.

O jogo foi planejado como um xadrez local para duas pessoas, alternando os turnos entre brancas e pretas. Além das peças clássicas, o projeto recebeu personagens personalizados inspirados em colegas e professores da turma, trazendo uma identidade visual ligada ao contexto acadêmico.

## 2. Tecnologias Utilizadas

O projeto foi desenvolvido com Unity 6.3 LTS, utilizando C# para a lógica de jogo e organização da cena. Para as regras formais do xadrez foi usada a biblioteca ChessDotNet, permitindo validar movimentos legais, xeque, xeque-mate, empate, roque, en passant e promoção.

Os principais recursos usados foram:

- Unity 6.3 LTS `6000.3.16f1`;
- C# para scripts de controle, interação, interface e movimentação;
- ChessDotNet para validação das regras de xadrez;
- Universal Render Pipeline para renderização;
- Prefabs e materiais para organizar tabuleiro, peças e personagens;
- Modelos 3D personalizados importados para o Unity.

## 3. Estrutura do Jogo

A cena principal está em `game/Assets/Scenes/Main.unity`. O tabuleiro é formado por uma matriz 8x8 de casas 3D, cada uma associada a uma coordenada lógica do xadrez. As peças são instanciadas pelo sistema de jogo e posicionadas sobre o tabuleiro conforme o estado da partida.

O jogador seleciona uma peça com o mouse e depois escolhe uma casa de destino. O movimento só é executado quando a biblioteca de regras confirma que a jogada é válida. Após cada jogada, o turno é alternado e a câmera muda para o lado do jogador atual, facilitando a experiência de duas pessoas jogando no mesmo computador.

A interface apresenta:

- tela inicial;
- botões de nova partida, cancelar seleção e ajuda;
- indicador de turno;
- histórico de jogadas;
- tela de promoção de peão;
- painel lateral da peça selecionada.

O painel lateral permite visualizar o personagem selecionado mais de perto, girar o modelo e controlar o zoom para observar detalhes do asset durante a partida.

## 4. Modelagem e Personagens

O projeto usa peças clássicas como fallback visual e personagens personalizados para cada tipo principal de peça. Os personagens foram criados com apoio de ferramentas de IA generativa para modelagem 3D a partir de referências visuais autorizadas, depois importados para a Unity e configurados como prefabs.

A distribuição dos personagens ficou:

| Peça | Personagem | Registro |
| --- | --- | --- |
| Peão | Matheus Duarte | Matrícula 0276899, criador do jogo |
| Bispo | Rafael Scharer | Matrícula 040603 |
| Cavalo | Gustavo Cornalewski | Matrícula 0407923 |
| Torre | Alex Fenner | Matrícula 0403240 |
| Rainha | Marta Rosecler Bez | Professora de Ciências da Computação - Universidade Feevale |
| Rei | Ricardo Ferreira de Oliveira | Professor de Ciências da Computação - Universidade Feevale |

As imagens de referência foram usadas apenas como apoio visual para criar modelos estilizados. Elas não fazem parte do repositório entregue. Dentro do jogo, os modelos foram ajustados para ficarem legíveis a partir da câmera isométrica e funcionarem dentro da escala do tabuleiro.

## 5. Conceitos de Computação Gráfica Aplicados

O projeto aplica conceitos importantes da disciplina:

- **Cena 3D:** composição de tabuleiro, peças, ambiente e câmera.
- **Modelagem por objetos:** uso de primitivas, prefabs, meshes importadas e materiais.
- **Transformações geométricas:** translação, rotação e escala para posicionamento das peças, câmera e preview lateral.
- **Interação:** seleção por mouse, comandos por teclado, zoom e rotação.
- **Câmera:** troca dinâmica de perspectiva conforme o turno.
- **Iluminação e materiais:** ambiente temático simples, materiais distintos para tabuleiro, peças e personagens.
- **Animação:** movimento interpolado das peças entre casas e transições de câmera.
- **Interface gráfica:** HUD em Canvas integrado a uma cena 3D.

## 6. Desafios e Soluções

Um dos principais desafios foi integrar as regras completas do xadrez com uma representação visual 3D. Para evitar inconsistências, a lógica das regras ficou separada da parte visual. Assim, a Unity representa a cena e a interação, enquanto a biblioteca de xadrez valida a partida.

Outro desafio foi a escala dos personagens personalizados. Como alguns modelos possuem proporções diferentes, foi necessário ajustar prefabs, posicionamento e preview lateral para manter uma leitura clara no tabuleiro.

Também houve atenção especial para a experiência de dois jogadores locais. A câmera por turno foi implementada para que tanto brancas quanto pretas possam jogar com uma perspectiva adequada.

## 7. Como Executar

Para abrir o projeto:

1. Clonar o repositório.
2. Abrir o Unity Hub.
3. Adicionar a pasta `game` como projeto Unity.
4. Usar a Unity 6.3 LTS `6000.3.16f1`.
5. Abrir a cena `Assets/Scenes/Main.unity`.
6. Pressionar `Play`.

Para gerar uma build no macOS:

1. Abrir `Assets/Scenes/Main.unity`.
2. Usar o menu `Chess CGI > Build > macOS`.
3. A build será criada em `Builds/macOS/XadrezCGI.app`.

## 8. Conclusão

O resultado final é um jogo de xadrez 3D funcional, com regras completas, interface jogável, câmera dinâmica e personagens personalizados. O projeto demonstra a aplicação prática de conceitos de Computação Gráfica em uma experiência interativa, ao mesmo tempo em que conecta o conteúdo técnico da disciplina com uma temática ligada à turma e aos professores.

Como melhorias futuras, seria possível evoluir as animações dos personagens, criar movimentos de captura mais elaborados, melhorar ainda mais a diferenciação visual entre os lados e adicionar uma inteligência artificial para jogar contra o usuário.
