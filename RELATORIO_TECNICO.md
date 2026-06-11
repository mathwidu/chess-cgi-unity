# Relatorio Tecnico - Xadrez CGI

**Disciplina:** Computacao Grafica I
**Projeto:** Xadrez 3D interativo em Unity
**Aluno:** Matheus Duarte
**Matricula:** 0276899
**Data:** 11/06/2026

## 1. Objetivo

O objetivo do projeto foi desenvolver uma aplicacao 3D interativa utilizando conceitos de Computacao Grafica em um jogo de xadrez completo. O projeto busca demonstrar criacao de cena, modelagem e composicao de objetos 3D, interacao com usuario, transformacoes, camera, iluminacao, materiais, animacoes simples e organizacao de assets dentro da Unity.

O jogo foi planejado como um xadrez local para duas pessoas, alternando os turnos entre brancas e pretas. Alem das pecas classicas, o projeto recebeu personagens personalizados inspirados em colegas e professores da turma, trazendo uma identidade visual ligada ao contexto academico.

## 2. Tecnologias Utilizadas

O projeto foi desenvolvido com Unity 6.3 LTS, utilizando C# para a logica de jogo e organizacao da cena. Para as regras formais do xadrez foi usada a biblioteca ChessDotNet, permitindo validar movimentos legais, xeque, xeque-mate, empate, roque, en passant e promocao.

Os principais recursos usados foram:

- Unity 6.3 LTS `6000.3.16f1`;
- C# para scripts de controle, interacao, interface e movimentacao;
- ChessDotNet para validacao das regras de xadrez;
- Universal Render Pipeline para renderizacao;
- Prefabs e materiais para organizar tabuleiro, pecas e personagens;
- Modelos 3D personalizados importados para o Unity.

## 3. Estrutura do Jogo

A cena principal esta em `game/Assets/Scenes/Main.unity`. O tabuleiro e formado por uma matriz 8x8 de casas 3D, cada uma associada a uma coordenada logica do xadrez. As pecas sao instanciadas pelo sistema de jogo e posicionadas sobre o tabuleiro conforme o estado da partida.

O jogador seleciona uma peca com o mouse e depois escolhe uma casa de destino. O movimento so e executado quando a biblioteca de regras confirma que a jogada e valida. Apos cada jogada, o turno e alternado e a camera muda para o lado do jogador atual, facilitando a experiencia de duas pessoas jogando no mesmo computador.

A interface apresenta:

- tela inicial;
- botoes de nova partida, cancelar selecao e ajuda;
- indicador de turno;
- historico de jogadas;
- tela de promocao de peao;
- painel lateral da peca selecionada.

O painel lateral permite visualizar o personagem selecionado mais de perto, girar o modelo e controlar o zoom para observar detalhes do asset durante a partida.

## 4. Modelagem e Personagens

O projeto usa pecas classicas como fallback visual e personagens personalizados para cada tipo principal de peca. Os personagens foram criados com apoio de ferramentas de IA generativa para modelagem 3D a partir de referencias visuais autorizadas, depois importados para a Unity e configurados como prefabs.

A distribuicao dos personagens ficou:

| Peca | Personagem | Registro |
| --- | --- | --- |
| Peao | Matheus Duarte | Matricula 0276899, criador do jogo |
| Bispo | Rafael Scharer | Matricula 040603 |
| Cavalo | Gustavo Cornalewski | Matricula 0407923 |
| Torre | Alex Fenner | Matricula 0403240 |
| Rainha | MARTA ROSECLER BEZ | Professora de Ciencias da Computacao - Universidade Feevale |
| Rei | RICARDO FERREIRA DE OLIVEIRA | Professor de Ciencias da Computacao - Universidade Feevale |

As imagens de referencia foram usadas apenas como apoio visual para criar modelos estilizados. Elas nao fazem parte do repositorio entregue. Dentro do jogo, os modelos foram ajustados para ficarem legiveis a partir da camera isometrica e funcionarem dentro da escala do tabuleiro.

## 5. Conceitos de Computacao Grafica Aplicados

O projeto aplica conceitos importantes da disciplina:

- **Cena 3D:** composicao de tabuleiro, pecas, ambiente e camera.
- **Modelagem por objetos:** uso de primitivas, prefabs, meshes importadas e materiais.
- **Transformacoes geometricas:** translacao, rotacao e escala para posicionamento das pecas, camera e preview lateral.
- **Interacao:** selecao por mouse, comandos por teclado, zoom e rotacao.
- **Camera:** troca dinamica de perspectiva conforme o turno.
- **Iluminacao e materiais:** ambiente tematico simples, materiais distintos para tabuleiro, pecas e personagens.
- **Animacao:** movimento interpolado das pecas entre casas e transicoes de camera.
- **Interface grafica:** HUD em Canvas integrado a uma cena 3D.

## 6. Desafios e Solucoes

Um dos principais desafios foi integrar as regras completas do xadrez com uma representacao visual 3D. Para evitar inconsistencias, a logica das regras ficou separada da parte visual. Assim, a Unity representa a cena e a interacao, enquanto a biblioteca de xadrez valida a partida.

Outro desafio foi a escala dos personagens personalizados. Como alguns modelos possuem proporcoes diferentes, foi necessario ajustar prefabs, posicionamento e preview lateral para manter uma leitura clara no tabuleiro.

Tambem houve atencao especial para a experiencia de dois jogadores locais. A camera por turno foi implementada para que tanto brancas quanto pretas possam jogar com uma perspectiva adequada.

## 7. Como Executar

Para abrir o projeto:

1. Clonar o repositorio.
2. Abrir o Unity Hub.
3. Adicionar a pasta `game` como projeto Unity.
4. Usar a Unity 6.3 LTS `6000.3.16f1`.
5. Abrir a cena `Assets/Scenes/Main.unity`.
6. Pressionar `Play`.

Para gerar uma build no macOS:

1. Abrir `Assets/Scenes/Main.unity`.
2. Usar o menu `Chess CGI > Build > macOS`.
3. A build sera criada em `Builds/macOS/XadrezCGI.app`.

## 8. Conclusao

O resultado final e um jogo de xadrez 3D funcional, com regras completas, interface jogavel, camera dinamica e personagens personalizados. O projeto demonstra a aplicacao pratica de conceitos de Computacao Grafica em uma experiencia interativa, ao mesmo tempo em que conecta o conteudo tecnico da disciplina com uma tematica ligada a turma e aos professores.

Como melhorias futuras, seria possivel evoluir as animacoes dos personagens, criar movimentos de captura mais elaborados, melhorar ainda mais a diferenciacao visual entre os lados e adicionar uma inteligencia artificial para jogar contra o usuario.
