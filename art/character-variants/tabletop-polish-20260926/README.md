# Tabuleiro, mesa e iluminação — 26/09/2026

Primeira etapa da avaliação visual integrada ao projeto Unity em 26/09,
originalmente na branch `codex/character-variants`. Consolidada com as demais
melhorias em `codex/visual-feevale`; veja o [guia da entrega](../../../docs/visual-feevale.md).
Esta etapa preservou os personagens. A Main carrega os componentes existentes.

## Resultado

- Moldura de nogueira com quinas suavizadas, filete discreto e fundo escuro.
  Área jogável de 45 cm, largura externa de 50,76 cm e espessura de 26,1 mm no VR.
- Casas em maple e nogueira com mapas de cor, normal e rugosidade de 1024 px;
  mipmaps e anisotropia 8×. Materiais mantêm os GUIDs originais da Main.
- 32 coordenadas em geometria, orientadas para os dois jogadores, combinadas
  em um renderer. A1 escura, H1 clara; rainhas iniciam em casas de suas cores.
- Tampo da mesa central em 0,7557 m, 18 mm abaixo da versão anterior para
  apoiar a moldura. Casas, peças, assentos e âncora do tabuleiro mantêm as
  posições anteriores. Tampos com madeira em escala mais fina, menor contraste
  e mapas de cor, normal e rugosidade de 2048 px.
- Luz principal lateral pelas janelas, preenchimento mais suave e ambiente
  menos uniforme. Bias específico na escala VR evita faixas de auto-sombra.
  Casas e laminado dos tampos recebem sombras; a moldura, bordas e estruturas
  projetam as silhuetas. As lâminas finas não duplicam esses casters.
- Um reflexo da sala em 128 px por face, distribuído por frames na
  inicialização e atualizado apenas ao reconstruir ou mover a sala.
  Peças, HUD e estúdio do preview são excluídos dessa captura.
- Campo de visão inicial do desktop em 50 graus para deixar coordenadas
  acima da barra de ações em 16:9. Zoom, órbita e olhar ao redor permanecem.

Os seis personagens, suas variantes e a interface foram preservados. Os
197 arquivos protegidos de personagens, cenas, configurações, UI e controllers
mantêm os hashes anteriores. A sala continua com 31 renderers; a moldura
acrescenta quatro renderers e nenhum colisor. O modo desempenho continua
trocando as peças, sem alterar as regras de interação do tabuleiro.

## Verificação executada

- Blender 5.2.1: geração e exportação dos dois GLBs concluídas.
- Unity 6000.3.16f1 / URP 17.3.0: importação e compilação concluídas.
- **62 testes Play Mode passaram**, sem falhas ou testes ignorados, na versão
  final. Incluem contato entre mesa e base nas duas escalas, alinhamento e
  cores das coordenadas, raycasts nas casas, enquadramento desktop para os
  dois lados, isolamento do reflexo, preview e controles existentes.
- 14 capturas da Main: seis desktop e oito no caminho de HMD simulado.
  O fluxo desktop inicia partida, completa e2–e4, troca perspectiva, olha
  para a sala e retorna ao tabuleiro.
- Comparação interativa em `index.html`: detalhe, posição do jogador e sala.
  As três imagens anteriores foram copiadas da avaliação de 26/09, sem edição.

As capturas estão em `unity/`. A evidência de antes está em `before/`.
Resultados e logs: `.local/tabletop-polish-20260926/playmode-final.xml`,
`playmode-final.log`, `vr-capture-final.log` e `pc-capture-final.log` na raiz
do repositório. `verification.json` registra o resumo final.

## Limites

VR usa XRSimulatedHMD, visão monoscópica e captura sem pós-processamento.
Não foi medido desempenho, conforto estéreo ou legibilidade em headset físico.
O custo inicial do reflexo deve ser medido no hardware alvo. As capturas de
ambiente ocultam HUD e controles sem rastreamento; a imagem `vr-play` mantém
o HUD real. O desktop converte o Canvas para Screen Space Camera apenas
durante a captura; no jogo permanece Screen Space Overlay.

Esta etapa não reorganiza o HUD, não acrescenta detalhes de uso da sala e
não revisa novamente as malhas e texturas dos personagens. Essas frentes
permanecem na avaliação visual anterior. O laboratório continua inspirado
na Feevale, sem pretensão de ser uma réplica medida da sala 102.

## Fontes e reprodução

- `board-source/ChessBoard.blend`: fonte editável da moldura e coordenadas.
- `lab-source/FeevaleComputerLab.blend`: fonte editável da sala e mesa.
- Geradores: `art/character-variants/build_chess_board.py` e
  `art/character-variants/build_feevale_lab.py`.
- Importação: menu **Chess CGI / Import Wooden Board** e
  **Chess CGI / Import Feevale Computer Lab**, ou método
  `ChessBoardImport.BuildTabletop` em batch.
- Capturas: `FeevaleLabReviewCapture.RunTabletopDesktop` e
  `FeevaleLabReviewCapture.RunTabletopVr`. São métodos assíncronos de batch;
  o próprio capturador encerra o Editor, portanto não passar `-quit`.

O backup dos 13 arquivos existentes alterados está em
`.local/tabletop-polish-20260926/before/`, com os caminhos relativos originais.
Para recuperar essa etapa, feche o Editor e copie somente os arquivos desejados
de volta aos mesmos caminhos. Não usar reset geral: há alterações anteriores
do usuário e de outras etapas nesta branch. Os fontes e as capturas anteriores
do laboratório permanecem em `feevale-room-v2-20260925/`.

Referências técnicas consultadas: [iluminação na Unity 6.3](https://docs.unity3d.com/6000.3/Documentation/Manual/lighting-configuration-workflow.html),
[reflection probes em URP](https://docs.unity3d.com/6000.3/Documentation/Manual/urp/lighting/reflection-probes-introduction.html)
e [atualização por RenderProbe](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/ReflectionProbe.RenderProbe.html).
