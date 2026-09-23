# Changelog

O que mudou no contexto de apresentação, lançamento mais recente primeiro, no
formato [Keep a Changelog](https://keepachangelog.com/en/1.1.0/): um H2 por
lançamento, escrito como "## [1.2.0] - 2026-06-30" com " [YANKED]" ao final
se o lançamento foi retirado, contendo Added, Changed, Deprecated, Removed,
Fixed ou Security como H3s, cada um deles uma lista de itens.

## [Unreleased]

### Added

- Tela inicial com modo, lado e dificuldade; estado de pensamento, recuperação de falha e retorno ao menu.
- Nova partida mantém as opções escolhidas.

### Changed

- Construção visual do menu separada de escolhas, foco e ajuste ao Canvas. HUD e palco divididos em etapas nomeadas; cenários de captura do Editor declarados junto de suas dimensões e foco esperado. Composição aprovada preservada.

- Menu com escolhas explícitas de modo, lado e três dificuldades, estado selecionado e resumo antes da partida.
- HUD, promoção, recuperação de falha e ajuda com tipografia ampliada e identidade visual comum.
- Ajuda bloqueia os botões de fundo; a configuração é preservada ao fechar e ao retornar ao menu.
- A composição do menu se ajusta às dimensões do Canvas desktop ou world-space; o modo local oculta as opções exclusivas da IA.
- Direção Palco da turma: cores Feevale, assinatura original, tipografia Lato e elenco real em preview 3D isolado; trocar lado move suavemente o palco, sem alterar a partida.
- Navegação por teclado inicia com foco visível e transfere o foco para ajuda, promoção e recuperação de erro.
- O palco 3D do preview da peça selecionada não herda a escala do Canvas, mantendo o enquadramento em Canvas de tela ou world-space.
- Redistribuição do menu: assinaturas no cabeçalho, elenco central e faixa inferior com modo, lado, dificuldade e ação de jogar; navegação explícita acompanha os grupos horizontais.
- O menu destaca Marta (brancas) ou Ricardo (pretas) conforme o lado selecionado; no modo local, ambos recebem o mesmo destaque. O palco usa iluminação própria e os dois modelos existentes.
- Corrigida a proporção da assinatura Feevale desativando o redimensionamento NPOT na importação.
- Foco de navegação usa contorno claro, separado do valor escolhido; textos pequenos preservam leitura nas dimensões menores do Game view.

- Menu Mesa de partida, aprovado por imagem em 22/09/2026: cenário com tabuleiro, professores 3D à esquerda e painel de configuração à direita. A logo considera os limites visíveis do PNG para centralizar sobre o painel. Seleção usa contorno e confirmação; uma descrição explica a dificuldade. Nomes acompanham as bases sobre sombra suave. Interação uGUI e Canvas world-space preservados.
- Jogar recebe um contorno de foco independente da seleção; Intermediário selecionado cabe em uma linha na janela de 1024×768.
