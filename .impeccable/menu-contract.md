# Menu inicial — Mesa de partida

Escopo: menu e HUD uGUI, desktop primeiro, Canvas comum ao VR. Experience na apresentação dos professores; Operate nas escolhas da partida. A imagem `mocks/approved/mesa-de-partida.png` (1672 × 941) foi escolhida explicitamente pelo usuário em 22/09/2026 e substitui as direções anteriores.

## Direction contract

THESIS: os professores que dão identidade às peças apresentam o jogo em uma mesa de estudo.

OWN-WORLD: verdes Feevale, tabuleiro, livros e planta; personagens 3D reais, título branco e ação amarela. Marca institucional original, proporcional e separada do título acadêmico.

STORY: reconhecer o projeto da turma, escolher adversário, lado e dificuldade e iniciar a partida.

FIRST VIEWPORT: Xadrez CGI no alto à esquerda; Marta e Ricardo sobre o tabuleiro cenográfico. Configuração em painel à direita, com a parte visível da assinatura Feevale centralizada no mesmo eixo. Seleção tem borda e confirmação amarelas; foco tem contorno claro próprio. Jogar é a ação dominante.

FORM: composição Mesa de partida aprovada por imagem. Preservar seus alinhamentos, distribuição, hierarquia e camadas. No jogo, manter os prefabs 3D e a logo originais em vez das versões ilustradas da referência; usar Lato incorporada como alternativa redistribuível, sem afirmar que é Avenir.

FINISH: referências vigentes em `../DESIGN.md` e `design.json`; revisão em `review/study-review.md` e limites de validação em `review/study-verdict.md` e `../docs/ai-desktop.md`.

## Comportamento que deve ser preservado

- Marta representa brancas, Ricardo representa pretas. Escolher o lado ajusta posição, escala e luz; nomes acompanham as bases. No modo local, ambos têm a mesma ênfase.
- Fundo cenográfico é uma imagem separada; marca, personagens, textos e controles são camadas independentes.
- Configuração, ajuda, promoção e erro mantêm seleção e navegação por teclado. Os controles continuam uGUI para o Canvas world-space.
- O palco de menu não participa da partida e restaura a iluminação externa após cada render.

Estudos anteriores ficam em `direction-notes.md`, `direction-options.json` e nos relatórios de revisão iniciais. São histórico, não instruções para substituir a composição aprovada.
