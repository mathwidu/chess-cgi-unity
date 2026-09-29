# Sala Feevale — referência real, versão 3

Iteração de 28/09/2026, organizada em 29/09 para a branch `codex/visual-feevale`.
A geometria é
orientada pelo vídeo de 72,7 s fornecido pelo usuário e pela foto da mesa com
uma régua de 30 cm. O objetivo é aproximar o cenário dos elementos observados,
preservando a posição de jogo em PC e VR. A integração com a `main` será uma
etapa posterior, em conjunto com as alterações da equipe.

[Comparação e capturas](index.html) · [Modelo](lab-source/model-report.json) ·
[Verificação](verification.json)

## Mudanças

- Parede azul com quadro e porta clara, parede cinza da projeção e janelas
  altas com caixilhos claros e faixas ocre/alaranjadas.
- Teto com vigas, eletrocalhas e 18 luminárias simples suspensas; projetor e
  ar-condicionado largo junto às janelas. As lâmpadas são únicas e seguem
  o comprimento da sala, paralelas às paredes das janelas e persianas,
  conforme a correção do usuário.
- Persianas cinza com pesos/correntes; armários brancos, nichos e portas
  coloridas; três equipamentos genéricos de laboratório.
- Conjuntos girados em 90° conforme correção confirmada pelo usuário: alunos
  voltados para persianas ou janelas, com o quadro de projeção ao lado.
- Quatro grupos de quatro mesas, 17 notebooks e 19 cadeiras contando os
  jogadores e a mesa do professor. Telas usam formas abstratas.
- Tampos de madeira clara com borda fina, passa-cabos pretos e pernas em T;
  mapas de cor, normal e rugosidade compartilhados, com 2048 px nos tampos.
- Luz interna branca de teto e vidro escuro, seguindo o horário da gravação.
  As luminárias são emissivas: não há uma luz dinâmica para cada tubo.

## Fidelidade e adaptação

O intervalo graduado de 0 a 30 cm é a única dimensão conhecida na referência.
A foto tem perspectiva e bordas ocultas; não permite deduzir todas as medidas
do tampo, a altura da mesa ou as dimensões da sala. O envelope de
6,8 × 9,4 × 3,3 m permanece estimado. Quantidade de mesas, distribuição e
área central são uma adaptação para o jogo.

O tampo do tabuleiro permanece a 0,7557 m; sua âncora fica em 0,78 m.
Os marcadores dos assentos permanecem a 1,20 m de altura e 0,60 m do centro.
A sala continua acompanhando a escala e a pose do tabuleiro, sem colisores
que interceptem a seleção das casas. Personagens, regras, HUD, promoção,
fim de partida e controles de câmera permanecem sob os componentes existentes.

O vídeo, a foto e os quadros extraídos ficam em `.local/`/Downloads. Não são
texturas do jogo nem arquivos desta entrega. Não foram reproduzidos colegas
como NPCs, rostos ou conteúdo real das telas.

## Abrir no jogo

Abrir `game/Assets/Scenes/Main.unity` no Unity 6000.3.16f1 e usar
**Chess CGI → Jogar no Editor**. Desmarcar **Modo desempenho** para ver os
personagens personalizados. Usar **Olhar ao redor** e arraste direito para
examinar a sala; **R** volta ao tabuleiro.

O GLB e o prefab ficam nos caminhos anteriores. Não é necessário rodar Blender
para jogar. O importador preserva o GUID do prefab e a rotação de integração.

## Reprodução

Com o Editor fechado, na raiz do repositório:

```sh
/opt/homebrew/bin/blender -b --python art/character-variants/build_feevale_lab.py

"/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -quit -projectPath "$PWD/game" \
  -executeMethod FeevaleLabImport.Build \
  -logFile "$PWD/.local/feevale-room-v3-20260928/import.log"

bash tools/test_unity.sh
```

`lab-source/FeevaleComputerLab.blend` é o fonte editável, com imagens empacotadas.
O gerador combina uma cópia de exportação por material e aplica oclusão de
contato em cor de vértice. Não altera a cena Unity nem os personagens.

Capturas de revisão, uma execução de cada vez, sem `-quit` porque a rotina
assíncrona encerra o Editor após salvar todas as imagens:

```sh
"/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -projectPath "$PWD/game" \
  -executeMethod FeevaleLabReviewCapture.RunReferenceDesktop \
  -logFile "$PWD/.local/feevale-room-v3-20260928/desktop-capture.log"

"/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -projectPath "$PWD/game" \
  -executeMethod FeevaleLabReviewCapture.RunReferenceVr \
  -logFile "$PWD/.local/feevale-room-v3-20260928/vr-capture.log"
```

As vistas `vr-*` usam HMD simulado e câmera de revisão, incluindo ângulos fora
do assento para inspecionar o cenário. Capturas e testes automatizados não
comprovam ergonomia, legibilidade em estéreo ou desempenho no headset físico.
Resultados executados nesta rodada, registrados em `verification.json`.
Esse arquivo preserva a verificação local anterior à publicação; a data e
os campos `local_changes_only` e `published_this_iteration` retratam aquele
momento, não o estado posterior da branch remota:

- 36 testes EditMode e 62 PlayMode aprovados, sem falhas ou ignorados.
- 18 capturas de 1920 × 1080: seis do PC e 12 pelo caminho com HMD simulado,
  incluindo a câmera superior de revisão.
- Importação confirmada: 31 renderers, 187.352 triângulos e tampo a 0,7557 m.
- Comparação SHA-256: todos os `.meta` originais preservados; personagens,
  tabuleiro, regras, HUD, rig VR e o ensaio AnimationStudies sem alterações.
- Revisão visual identificou e corrigiu a sobreposição entre borda e laminado,
  a orientação das mesas e a direção/duplicidade das lâmpadas.

O ambiente anterior tinha 118.407 triângulos e 31 renderers. O novo mantém
31 renderers, com mais geometria nos grupos de mesas e no teto. Isso não
estabelece um resultado de desempenho: não houve medição de FPS no headset.

## Recuperação e continuidade

A fonte anterior está em `../tabletop-polish-20260926/lab-source/`. Antes da
edição, foram copiados o gerador, o GLB, o prefab, `ScenePolish` e a rotina de
captura para `.local/feevale-room-v3-20260928/before/`, com manifesto SHA-256
dos assets. O arquivo `RESTORE.md` local descreve a restauração seletiva.
O ensaio `Assets/AnimationStudies` permanece separado e preservado.

Depois da avaliação desta versão, os próximos refinamentos possíveis são:
medidas completas e posição exata dos móveis; acabamento de vidros e metais;
pequenos objetos de uso; e ajuste de desempenho/legibilidade no headset alvo.
Essas etapas precisam ser combinadas com o usuário antes de ampliar o escopo.
