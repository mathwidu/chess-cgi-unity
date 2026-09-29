# Laboratório de informática inspirado na Feevale

Direção escolhida pelo usuário em 25/09/2026: um laboratório com computadores,
como as salas que frequenta no curso. O estudo anterior em
`../classroom-discovery-20260925/` continua preservado como comparação.

## Referências reais consultadas

Fonte: [infraestrutura do Mestrado Profissional em Indústria Criativa da Feevale](https://www.feevale.br/pos-graduacao/stricto-sensu/mestrado--profissional-em-industria-criativa/infraestrutura).
As três fotografias foram abertas e inspecionadas visualmente no navegador.

- [Laboratório de Projetos de TI](https://www.feevale.br/Comum/midias/cc66f7a5-cd9c-4416-ad2f-4396075d1c48/1200x580/Lab-de-Projetos-de-TI.jpg):
  bancadas claras em fileiras, monitores e gabinetes pretos, teclados, persianas
  verticais, quadro e piso amadeirado. É a referência visual principal.
- [Laboratório de Redes](https://www.feevale.br/Comum/midias/c194f139-57e8-4031-a958-e40f7b19dc67/1200x580/Lab-de-redes.jpg):
  cadeiras escuras com rodízios, mesas claras, quadro, projetor e persianas.
  A foto mostra notebooks; usamos desktops conforme a referência anterior.
- [Laboratório de Computação Gráfica](https://www.feevale.br/Comum/midias/d345f105-572b-4aee-a20f-2f9216cf3808/1200x580/Laborat%C3%B3rio-de-Computa%C3%A7%C3%A3o-Gr%C3%A1fica.jpg):
  confirma outra configuração com desktops, mas tem cadeiras vermelhas e paredes
  lilases. Essa paleta não foi misturada à direção principal.

São fotos publicadas pela instituição; não comprovam o estado atual de uma sala
específica. A planta, medidas, quantidade de postos e mesa de xadrez são uma
adaptação para o jogo, não um levantamento arquitetônico.

## Modelo e integração

- Fonte editável: `FeevaleComputerLab.blend`, em metros, eixo Z para cima.
- Gerador: `../build_feevale_lab.py`; a madeira é um material procedural do
  Blender convertido em textura albedo de 1024 px para o glTF.
- Exportação: `game/Assets/Art/Environment/FeevaleLab/FeevaleComputerLab.glb`.
- Prefab: `game/Assets/Resources/Environment/FeevaleComputerLab.prefab`.
- Importador: `FeevaleLabImport.Build` no Editor instalado do projeto.
- `ScenePolish` carrega o prefab na Main e acompanha o transform do `BoardView`.
  O ambiente usa a mesma relação de escala do tabuleiro no desktop e em VR.
- Mesa: 1,30 × 0,90 m, superfície em 0,7737 m; a base do tabuleiro encosta
  nessa superfície. Centro do tabuleiro em 0,78 m, área jogável de 45 cm.
- Sala: 6,8 × 9,4 × 3,3 m, 12 computadores e 14 cadeiras, corredor central
  livre e ambos os assentos previstos pelo `XRRig` preservados.
- Somente cenário no GLB. Tabuleiro, 32 peças, regras, materiais dos personagens
  e respectivos prefabs continuam sendo os existentes no jogo.
- Geometria combinada por material: 26 renderers, 101.639 triângulos.
  Não há luzes adicionais nem colisores que capturem o raycast das casas.
  Paredes e teto não bloqueiam a luz direcional usada no jogo.
- HUD VR aproximado: painel de 2,304 × 1,296 m em (0; 1,6; 1,35), espelhado
  para pretas. A posição anterior em Z=4 escondia botões atrás dos monitores.
  A tela desktop não muda. Controles sem rastreamento são ocultados somente
  nas capturas do simulador, para não mostrar mãos presas na origem.
- Luzes do estúdio do menu ligadas somente durante o render dos professores:
  a comparação antes da correção encontrou 51.123 pixels da sala alterados.
- Bias da luz principal em escala VR: profundidade 0,4 / normal 0,5. Comparação
  com sombras desligadas e valores 0,4 / 1 / 2 identificou a origem das faixas
  no tampo. Foi usado o menor valor ensaiado que removeu o artefato. Desktop
  mantém o bias do pipeline. Evidências em `.local/feevale-lab-20260925/shadow-probe/`.

## Verificação

44/44 testes PlayMode aprovados, incluindo o teste de iluminação do estúdio,
contato mesa/tabuleiro nos dois tamanhos, raycast de casa livre, reaplicação
sem duplicação e partida real contra Stockfish no desktop. Os 170 arquivos
de personagens e estudos de animação conferem com os hashes anteriores.

As capturas em `unity/` são da Main em Play Mode. O caminho VR usa um
`XRSimulatedHMD` antes de carregar a cena, acionando o tabuleiro em metros,
`XRRig` e HUD World Space reais. Algumas vistas ocultam o HUD ou mudam a
direção da câmera para inspecionar a sala. Saída sem pós-processamento para
comparação visual, conforme o fluxo de revisão dos personagens.

Isso não é validação em headset. Conforto, alcance, leitura estereoscópica,
sombras e frame time no dispositivo alvo continuam pendentes. O perfil Mobile
e os assets de pós-processamento não foram alterados.

Os logs e o backup do código anterior ficam em `.local/feevale-lab-20260925/`.
`preserved.json` registra hashes de 170 arquivos dos personagens e dos estudos
de animação. Não é necessário regenerar os personagens para importar a sala.

## Próxima descoberta visual

Depois de avaliar esta sala no headset: acabamento do tabuleiro (madeira,
bordas, coordenadas), leitura das bases a 45 cm, posicionamento do HUD dentro
da sala, iluminação indireta pré-calculada e orçamento de detalhes por distância.
Essas melhorias precisam preservar a prioridade visual da partida e ser
medidas no dispositivo; não estão declaradas como prontas nesta entrega.
