# Sala, materiais e câmera de PC — 25/09/2026

Atualização local integrada à cena Main. Layout inspirado em referências reais;
não é uma reprodução medida da sala atual da universidade.

## Referência identificada

A [página oficial da Feevale](https://www.feevale.br/pos-graduacao/stricto-sensu/mestrado--profissional-em-industria-criativa/infraestrutura)
identifica o **Laboratório de Redes como sala 102 do prédio Verde, Câmpus II**.
Sua [fotografia](https://www.feevale.br/Comum/midias/c194f139-57e8-4031-a958-e40f7b19dc67/1200x580/Lab-de-redes.jpg),
consultada visualmente em 25/09/2026, mostra tampos amadeirados claros, notebooks,
cadeiras escuras, persianas, nichos coloridos baixos, quadro e projetor.
A foto do [Laboratório de Projetos de TI](https://www.feevale.br/Comum/midias/cc66f7a5-cd9c-4416-ad2f-4396075d1c48/1200x580/Lab-de-Projetos-de-TI.jpg)
(sala 206 segundo a mesma página) permanece referência complementar para desktops.
As fotos são referências externas; não foram incorporadas aos assets do jogo.

As três janelas da direita, a distribuição, as medidas e a mesa central são
adaptações para a partida. A fotografia não confirma seu alinhamento exato
nem o estado atual completo da sala 102.

## O que mudou

- Tampos de madeira clara com mapas compartilhados de cor, normal e rugosidade
  em 2048 × 2048. A mesa do xadrez usa o mesmo acabamento fosco das bancadas.
- Pintura, cadeiras e persianas com mapas de superfície em 1024 × 1024,
  UVs em escala física, mipmaps e anisotropia 8× no importador do ambiente.
- Três conjuntos de janelas com caixilhos, fechos, bandeiras superiores e
  peitoris. Um mapa opaco de vidro fosco dá variação suave de céu/verde;
  não há transparências ordenadas nem uma segunda cena externa.
- Nichos sob as persianas com detalhes em azul esverdeado, rosa e espaços
  neutros, inspirados no mobiliário da foto da sala 102.
- Oclusão de contato em cor de vértice: 20 raios por amostra, alcance de 55 cm,
  aplicada somente à cópia de exportação. O modelo Blender continua editável.
- Luz principal 1,0, preenchimento direcional 0,3, ambiente levemente quente,
  teto com menor emissão e dois preenchimentos largos voltados para baixo,
  sem sombras extras. Alcance e intensidade compensam a escala desktop/VR.
- **Olhar ao redor** no desktop: botão do HUD nivela a visão do assento e
  amplia o campo de visão. Botão direito + movimento do mouse gira a visão;
  soltar libera o cursor. R, Esc ou Voltar ao tabuleiro retorna ao lado atual.
  A troca de turno não interrompe a observação. Q/E e zoom continuam na vista
  do tabuleiro. O controle desktop não dirige a câmera de headset.

## Fontes e reprodução

- Gerador: `../build_feevale_lab.py`.
- Fonte editável: `FeevaleComputerLab.blend`, com imagens empacotadas.
- Exportação: `game/Assets/Art/Environment/FeevaleLab/FeevaleComputerLab.glb`.
- Importador do Editor: `FeevaleLabImport.Build` / menu **Chess CGI → Import Feevale Computer Lab**.
- Prefab: `Resources/Environment/FeevaleComputerLab`.
- Revisão: `index.html`; imagens em `unity/` são renders reais da Main.
- Métricas: `model-report.json`; verificação consolidada em `verification.json`.

O ambiente possui 31 renderers agrupados por material, 118.407 triângulos e
15 imagens embutidas no GLB. Mantém 12 computadores, 14 cadeiras e nenhuma
peça de xadrez ou colisor de decoração no asset. A mesa permanece a 0,7737 m,
com centro do tabuleiro a 0,78 m. A sala mede 6,8 × 9,4 × 3,3 m.

## Verificação e limites

**50/50 testes PlayMode aprovados**, sem ignorados, e 14 capturas da Main
em 1920 × 1080. Conferidos mapas importados, mesa apoiada, 32 peças,
seleção, câmera, menu e IA offline. Os 175 arquivos protegidos (personagens,
animações, Main, XR e versões/configuração) mantêm os hashes do início da etapa.
Domainbook validado e Graphify atualizado.

Conferência interativa adicional no Editor: aberta uma partida de dois jogadores,
acionado **Olhar ao redor** no HUD e confirmado o retorno ao tabuleiro com **R**.
A partida ficou aberta na vista do tabuleiro para revisão do usuário.

Os testes de câmera usam uma cópia temporária de InputSettings para o mouse
simulado receber eventos em batch, sem alterar o foco/configuração do usuário.
Capturas VR usam XRSimulatedHMD antes da Main e a inicialização real do rig,
com render monoscópico no Editor. As vistas de ambiente ocultam o HUD e os
controles sem rastreamento apenas durante as capturas.

Desempenho, alcance, conforto e leitura em estéreo no headset físico ainda
precisam de avaliação. O tabuleiro mantém seus materiais anteriores e pode
receber a próxima etapa de acabamento após esta revisão.

## Recuperação

O estado anterior dos arquivos alterados e os hashes dos personagens estão
em `.local/feevale-room-v2-20260925/before/` e `preserved.json` na raiz do repo.
A primeira sala e suas capturas permanecem em `../feevale-lab-20260925/`.
Para voltar apenas o ambiente, restaure o gerador, GLB, prefab e ScenePolish
desse backup com a Unity fechada, preservando os arquivos .meta; reabra o
Editor para reimportar. Os modelos/fontes dos personagens não foram substituídos.
