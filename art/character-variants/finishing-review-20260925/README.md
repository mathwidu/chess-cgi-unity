# Acabamento dos personagens e nova descoberta visual

Rodada local de 25/09/2026, sobre a direção 02 já aprovada, na branch
`codex/character-variants`. A base da main permanece `81bea194`, com VR.
O pedido autorizou os ganhos de acabamento e luz e, em seguida, uma descoberta
para o tabuleiro sobre uma mesa dentro de uma sala de aula da Feevale.

[Abrir a comparação visual](index.html). `before/` preserva as 33 capturas da
rodada anterior; `after/` guarda as 33 capturas desta rodada, sem depender de
futuras substituições em `production/unity`.

## Implementado

- **Roupa:** a máscara do peão branco preserva a cargo junto à cintura, onde a
  camiseta invadia a calça. A torre inclui as faces da gola e distingue o punho
  direito do jeans, mesmo onde se sobrepõem em altura. A correção anterior do
  moletom do Gustavo é preservada.
- **Marca:** PNGs transparentes oficiais de 950 × 369, sem placa ou retângulo
  costurado. A aplicação aproxima-se da superfície e usa alfa contínuo para
  preservar as letras pequenas. Seis aplicações, só nas costas; materiais
  próprios para branco/preto. Os 12 closes de costas foram inspecionados.
- **Mãos dos acessórios:** comprimentos e raios variados, com pontas afiladas,
  mantendo o contato no cabo. A modelagem da pele ainda é simplificada.
- **Base:** perfil circular com 96 segmentos, lateral suave e topo plano.
  Diâmetro, posição dos símbolos e referências dos prefabs preservados.
- **Luz:** o preview usava uma direcional de intensidade 1,6, que alcançava a
  cena e superava a luz principal de 1,2 na seleção do URP. Passou a uma luz
  pontual local, restrita à camada do preview, com câmera renderizada sob demanda.
  A key/fill e a luz ambiente foram equilibradas; sombras suaves habilitadas
  somente no perfil PC, conservando resolução de sombras e perfil Mobile.

## Evidência e regressões

O teste `PreviewLightDoesNotChangeTheBoardRender` carrega a Main, inicia uma
partida e compara o render com a luz do preview ligada/desligada. Antes da
correção, **28.991 pixels** mudavam além da tolerância. Depois, o teste passa,
junto dos demais testes de apresentação e aparência: **32/32 PlayMode**.

As novas amostras reproduziram a falha na cintura da cargo, na gola e no punho
direito da torre. Os resultados anteriores estão em `contours-before.json` e
`cuff-before.json`. A auditoria final aprova **22/22 casos de tecido**. A de
UV encontra zero sobreposições nos seis corpos; a de geometria aprova os
contatos testados dos bastões, a cabeça original e a cobertura das marcas e
dos 12 símbolos. Esses resultados não equivalem a acabamento artístico final.

O inventário atual tem **1.345.592 triângulos** para as 32 peças, antes dos
passes adicionais. Não é medição de desempenho. Os sete GLBs originais, os
GUIDs dos seis prefabs usados e os 26 arquivos preexistentes de animação
continuam iguais às cópias verificadas. O registro da rodada está em
[verification.json](verification.json).

## Percurso reavaliado

| Etapa | Resultado observado | Próxima melhoria |
| --- | --- | --- |
| 1 — Menu | Boa hierarquia e professores inteiros | Explicar a troca para peças clássicas no modo desempenho e aproximar a cenografia do ambiente de jogo |
| 2 — Partida | Roupas com mais volume; sombra de contato presente | Mesa, borda do tabuleiro e contexto da sala ainda simples |
| 3 — Seleção | Nome, tipo, casa e preview ajudam | Painel cobre casas à direita; zoom compete com a base da miniatura |
| 4 — Movimento | e2–e4, turno e histórico legíveis | Avaliar orientação da câmera em movimento e conforto no headset |
| 5 — Vista superior | Símbolos completos e lados distintos | Coordenadas ausentes; símbolos finos e destinos dependentes de cor |
| 6 — Elenco | Identidade preservada e marca integrada | Refinamento localizado de anatomia, bordas, cabelo e acessórios originais |

## O que ainda merece trabalho nos modelos

- **Matheus:** gola, manga/pele, barra da calça junto ao tênis e anatomia da mão.
- **Alex:** mãos entrelaçadas e contorno traseiro da camisa contra o jeans.
- **Gustavo:** contato das mãos com a montaria, seu relevo e pedestal original.
- **Rafael:** planos sob os fones, pele da mão e resíduo à frente da base original.
- **Marta:** punhos, barra, cor do lenço atrás, cabelo e contato aparente dos sapatos.
- **Ricardo:** contorno da coroa original, cabelo/rosto e anatomia da mão do cetro.

A ronda atual não cria detalhe facial ausente nas texturas originais. Os
closes continuam mostrando os limites da fonte e onde modelagem/pintura manual
produziria ganho maior que aumentar novamente a resolução.

## Descoberta da sala em VR

O [estudo de sala](../classroom-discovery-20260925/README.md) contém dois renders
e uma fonte Blender editável, usando os 32 personagens. Mesa de laminado claro,
pernas metálicas, cadeiras verdes, quadro e janelas formam uma proposta inspirada
na Feevale. Não é réplica de uma sala levantada nem uma cena Unity pronta.

A ordem proposta é mesa/tabuleiro, seleção e leitura, ambiente e medição da
iluminação/custo no headset. As dimensões do jogo e as propostas estão separadas
no registro do estudo. Foi identificado que `ScenePolish` recria somente piso
e mesa, removendo a cenografia serializada; isso precisa ser tratado na
implementação futura da sala.

## Condições e limites da verificação

Unity 6000.3.16f1 com URP; 1920 × 1080 para o percurso e 1100 × 1400 para os
closes. A galeria usa luz própria. O capturador suprime pós-processamento em
ambas as revisões para não aplicar tonemapping ao HUD convertido para câmera.
As câmeras e enquadramentos são iguais, mas os materiais e a luz mudaram.

`lighting/` conserva um ensaio adicional com a Main e pós-processamento ativo,
antes/depois do isolamento da luz. Nesse ensaio o HUD também recebe tonemapping;
ele serve à avaliação da cena 3D, não à fidelidade da interface. O ensaio precede
o último ajuste do punho do Alex e da transparência da marca. O perfil de
pós-processamento da Main permanece intacto.

Não foram medidos FPS, frame time, alcance, conforto, acessibilidade completa
ou reconhecimento dos tipos por jogadores no headset. O ambiente do estudo usa
render offline e não comprova desempenho em tempo real. OpenXR segue sem runtime
de headset disponível neste Mac. Nenhuma publicação ou merge foi realizado.

## Reprodução e recuperação

Gerar: `blender -b --python art/character-variants/build_characters.py`.
Auditar: abrir `production/characters.blend` em batch com `--python-exit-code 1`
e executar `audit_characters.py`, `audit_fabrics.py` e `audit_uvs.py`, cada um com
`-- --report <arquivo>`. Capturar: Unity batch, projeto `game`, método
`CharacterReviewCapture.Run`. Testar: PlayMode, filtro
`CharacterAppearanceTests;MenuPresentationTests`.

Logs e XML desta rodada estão em `.local/character-production/` com os prefixos
`finishing-` e `tests-finishing`. A cópia recuperável anterior às mudanças fica
em `.local/character-production/finishing-20260925/`, com caminhos espelhados e
`preserved.json`. Para restaurar esta rodada, com o Editor fechado, devolver
somente esses arquivos aos caminhos correspondentes e reimportar; não usar
reset global, pois o checkout já continha alterações e estudos de animação.
