# Personagens — direção 02 aplicada

Implementação local, revisada em 25/09/2026, na branch `codex/character-variants`, sobre
a `main` `81bea194`, que já inclui a portabilidade para VR. O usuário aprovou
a segunda direção e autorizou sua aplicação ao elenco completo.

## Resultado

- [Rodada atual: acabamento, iluminação e descoberta da sala VR](../finishing-review-20260925/index.html).

- [Painel com comparação e closes individuais](../direction-v2/index.html).
- [Elenco no Unity](unity/cast-front.png), [costas](unity/cast-back.png) e
  [vista superior](unity/cast-top.png).
- [Tabuleiro completo](unity/board.png) e [tabuleiro de cima](unity/board-top.png).
- [Menu](unity/menu.png), [preview branco](unity/selected-white.png) e
  [preview preto](unity/selected-black.png).
- [Fonte editável no Blender](characters.blend), [render de revisão](front.png)
  e [inventário de geometria e fontes](report.json).

São seis personagens, cada um com versões branca e preta. As duas versões
usam as mesmas malhas, UVs, rostos e acessórios; o material define o lado.
Os símbolos clássicos ficam na superfície horizontal da base, repetidos
na frente e atrás e afastados dos pés, torre e montaria. A assinatura oficial
Feevale aparece **somente nas costas**, com letras adequadas ao fundo.
A frente mantém o tecido, as estampas e os acessórios de cada pessoa.

| Personagem | Acabamento e identificação |
| --- | --- |
| Matheus / peão | Camiseta, cargo e tênis preservados; mão refeita para segurar uma espada curta, com guarda, cabo e lâmina modelados |
| Alex / torre | Camisa xadrez recolorida preservando o padrão e jeans originais; continua sentado na torre |
| Gustavo / cavalo | Moletom nas duas cores, óculos e montaria preservados; sem adereço adicional na cabeça |
| Rafael / bispo | Moletom, fones e alça; báculo lateral e mão com preensão |
| Marta / rainha | Cardigan, lenço azul, saia estampada, óculos e pequena tiara; marca nas costas do cardigan |
| Ricardo / rei | Cabeça original completa, incluindo sua pequena coroa; moletom inteiramente branco/preto, Feevale nas costas e cetro afastado do antebraço |

O acabamento inclui suavização da geometria original, manutenção das texturas
de identidade, variação do tecido na recoloração e materiais próprios para
metal, couro e bases. Roupa e marca usam malhas, UVs e texturas separados
também no Unity. Os deslocamentos continuam usando o `PieceView` existente;
não foi criado um rig de animação.

## Regressões corrigidas

- **Cabeça do Ricardo:** a remoção da coroa por um plano horizontal também
  cortava cabelo e cabeça. O corte foi retirado, preservando a malha original.
- **Marca:** a projeção na textura do corpo perdia letras em ilhas pequenas
  de UV e atravessava dobras. Cada aplicação agora acompanha a superfície
  da roupa e ocupa o PNG transparente oficial de 950 × 369, compartilhada pelo elenco.
- **Contato:** os bastões deixaram de acompanhar o eixo do antebraço. A pose
  foi ajustada e as mãos refeitas ao redor do cabo; os restos da mão antiga
  foram removidos. Cabo, guarda e lâmina do peão agora se encontram sem lacuna.
- **Base e símbolos:** base em camadas, contorno fino e símbolos clássicos
  completos, com recortes internos corretos. Torre e cavalo ficam dentro da
  borda e livres da geometria central na vista superior.
- **Roupa desfocada/manchada:** bakes de cor de 4096 × 4096, normal de
  2048 × 2048 e filtragem trilinear com anisotropia 8. A máscara de pele passou
  a exigir luminosidade mínima: ruído quase preto do JPEG original estava
  sendo preservado indevidamente nas roupas brancas.
- **Cores antigas e tecido aparentemente rasgado:** máscaras espaciais distinguem
  roupa, pele, fones e lenço. A barra do Ricardo acompanha a cor do moletom;
  ombros da Marta e cargo preta não mantêm trechos da cor anterior. O lenço
  conserva também seus tons claros. Um novo mapa `ProductionUV`, com ilhas
  relaxadas sobre a superfície, separação de regiões sobrepostas e margem de
  bake de 48 pixels, evita que pele/cabelo vazem nas
  roupas ao reduzir a textura à distância. `SourceUV` continua no Blender
  para ler as texturas originais; o GLB exporta apenas `ProductionUV`.
- **Marca só nas costas:** a aplicação frontal foi removida dos seis personagens.
  As letras antigas do moletom são limpas tanto na cor quanto no relevo,
  evitando uma assinatura residual no peito.
- **Dimensões dos acessórios:** transformações são aplicadas à malha antes
  da exportação. Isso evita que a caixa de um bastão inclinado seja novamente
  rotacionada ao medir o prefab e superestime seu tamanho no tabuleiro.
- **Enquadramento do menu:** a câmera acomoda as bases completas dos professores
  quando o destaque muda entre branco, preto e partida local.

Os símbolos usam contornos da fonte Noto Sans Symbols 2, com fonte e licença
OFL preservadas em [fonts](../fonts/). Os arquivos oficiais da Feevale e sua
procedência estão em [brand](../direction-v2/brand/provenance.json).

## Integração

Os seis prefabs em `game/Assets/Resources/CustomPieces` foram atualizados pelo
Editor, preservando seus GUIDs. As referências da Main continuam válidas.
Os modelos novos ficam em `game/Assets/Art/Characters/Direction02`; os GLBs
originais em `*_Assets/selected.glb` permanecem intactos.

`CustomPieceAppearance` troca referências de materiais compartilhados sem
alterar os assets ou criar materiais por peça. `PieceFactory` aplica o lado
e usa a base integrada; prefabs antigos mantêm a base procedural. O menu
aplica branco à Marta e preto ao Ricardo antes de clonar os materiais usados
na transição. O preview herda a aparência e corrige a orientação das pretas
para mostrar a frente inicialmente.

Para ver os personagens durante uma partida, desmarcar **Modo desempenho**
no menu. A alternativa primitiva e a preferência existente foram preservadas.

## Verificação e limites

- Unity 6000.3.16f1: importação e compilação executadas; capturas feitas pelo
  URP com os prefabs realmente usados na Main.
- 32 testes PlayMode de personagens e apresentação do menu aprovados:
  seis tipos nos dois lados, geometria compartilhada, isolamento dos materiais,
  clones do preview, encaixe na casa, seleção ampliada e escala local 1 / 0,045,
  além do modo desempenho, do fallback de prefabs antigos e da independência
  das texturas da roupa e da marca nos seis personagens. O enquadramento
  completo dos professores é verificado nos três estados de destaque do menu.
- Auditoria geométrica reproduz as falhas no [modelo anterior](geometry-audit-before.json)
  e passa no [modelo corrigido](geometry-audit.json): zero interseções dos
  bastões com o corpo, 12 símbolos inteiros e visíveis de cima, seis aplicações
  da marca completas, exclusivamente nas costas, e sobre a roupa, sem corte da cabeça do Ricardo.
- A luz do preview fica isolada da partida; um teste de comparação de pixels
  reproduz a interferência anterior e passa após a correção. A auditoria de
  tecido inclui 22 casos, com cintura da cargo e gola/punho da torre.
- As bases usam laterais suaves e topo plano. A marca usa transparência
  contínua sem retângulo; dedos dos acessórios têm proporções variadas.
- A [auditoria de UV](uv-audit.json) rejeita sobreposições entre superfícies
  no mapa de produção. O [diagnóstico anterior](uv-audit-before.json) registra
  sobreposições de braços, torso e dobras; a geração agora abre as regiões
  afetadas e verifica novamente antes de exportar.
- 33 capturas no Unity: menu, tabuleiro, movimento, previews, elenco de frente,
  de costas e de cima, mais frente/costas de cada personagem nos dois lados.
- A [auditoria de tecido](fabric-audit.json) acompanha o [resultado anterior](fabric-audit-before.json):
  amostra as regiões relatadas, o vazamento de cor à distância, o tecido abaixo
  dos fones e a preservação da mão junto à calça do peão e dos antebraços da torre. As capturas completam
  essa verificação, incluindo as versões pretas no painel.
- GLBs originais, GUIDs dos prefabs e estudos de animação preexistentes
  conferidos por hashes/referências.

O conjunto completo possui **1.345.592 triângulos** antes dos passes
de sombra/contorno; por personagem são 36.780–52.452, em 6–8 renderers com
materiais compartilhados (230 no tabuleiro completo). Esses números são
inventário de geometria, não medição de FPS. A suavização tem custo e o modo
desempenho continua disponível. No desktop, cor usa BC7 e normal BC5; no
Android, as texturas do corpo ficam limitadas a 2K com ASTC 6×6. A marca
mantém seu atlas independente sem compressão. O perfil PC habilita sombras suaves de qualidade média, sem aumentar sua resolução;
o perfil Mobile e os pacotes XR permanecem inalterados.
Não foram medidos desempenho, agarrar/soltar ou reconhecimento em headset;
o Mac informa `XR_ERROR_RUNTIME_UNAVAILABLE`. A escala de VR foi testada em
PlayMode, sem representar um teste com dispositivo. Não houve build de player.

O aumento do bake evita perdas no acabamento novo, mas não cria detalhe
facial ausente nas texturas originais de 2K. Textura facial, cabelo e algumas
dobras ainda têm o limite do material de origem. O conceito ilustrado continua
como referência de acabamento; esta entrega não demonstra reconhecimento
com jogadores nem equivalência visual completa com a imagem gerada.

## Reprodução

Executar a partir da raiz do repositório, com o Editor deste projeto fechado:

```sh
/opt/homebrew/bin/blender --background --python-exit-code 1 --python art/character-variants/build_characters.py -- --quick
/opt/homebrew/bin/blender --background art/character-variants/production/characters.blend --python-exit-code 1 --python art/character-variants/audit_characters.py -- --report art/character-variants/production/geometry-audit.json
/opt/homebrew/bin/blender --background art/character-variants/production/characters.blend --python-exit-code 1 --python art/character-variants/audit_uvs.py -- --report art/character-variants/production/uv-audit.json
/opt/homebrew/bin/blender --background art/character-variants/production/characters.blend --python-exit-code 1 --python art/character-variants/audit_fabrics.py -- --report art/character-variants/production/fabric-audit.json
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath "$PWD/game" -executeMethod CharacterReviewCapture.Run -logFile "$PWD/.local/character-production/capture.log"
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath "$PWD/game" -runTests -testPlatform PlayMode -testFilter 'CharacterAppearanceTests;MenuPresentationTests' -testResults "$PWD/.local/character-production/tests.xml" -logFile "$PWD/.local/character-production/tests.log"
```

`--quick` muda somente a resolução/amostras do render de revisão; os assets
de jogo mantêm a mesma qualidade. O importador também está no menu do Editor:
`Chess CGI > Import Direction 02 Characters`. A captura não salva cenas nem
altera a preferência de desempenho. Usar somente `--kinds Pawn,King` gera um
piloto parcial; a regeneração final deve incluir os seis personagens.

O `.blend` referencia os PNGs gerados por caminhos relativos em
`game/Assets/Art/Characters/Direction02`. Manter essa estrutura ao copiar o
projeto; os arquivos de textura não devem ser duplicados dentro do `.blend`.

## Recuperação

Os prefabs anteriores estão no commit `81bea194` e na cópia local
`.git/codex-backups/character-production-20260924T235959Z/`, acompanhada de
`record.json` com SHA-256. Restaurar somente o prefab desejado, mantendo seu
`.meta`, permite voltar ao original; o factory continua aceitando esse formato.
Não restaurar toda a árvore de trabalho, pois contém outros estudos do usuário.
Os 26 arquivos preexistentes de `AnimationStudies` não fazem parte desta alteração.

A primeira implementação da direção 02, anterior às correções desta revisão,
está em `.local/character-production/revision-20260924-234859/`, com gerador,
fonte Blender, assets e prefabs. Para recuperar essa candidata, fechar o Editor,
copiar de volta somente esses caminhos correspondentes e reimportar no Unity.
Os arquivos retirados do cabelo artificial ficam em
`.local/character-production/obsolete-hair/`. Essas cópias são locais; a versão
original recuperável pelo Git continua sendo `81bea194`.

A revisão imediatamente anterior aos ajustes de tecido e marca apenas nas costas
está em `.local/character-production/fabric-revision-20260925-004503/`, com
`scope.json`, geradores, Blender, assets, prefabs e capturas. A restauração usa
os mesmos caminhos correspondentes, com o Editor fechado; não alcança os estudos
de animação nem modifica os GLBs originais.

## Revisão específica do Gustavo e avaliação geral — 25/09

O moletom do cavalo ainda conservava uma faixa preta: a seleção de faces
começava acima da cintura e terminava antes do capuz, e a máscara de cor
interrompia a recoloração em um plano horizontal. A seleção foi ampliada e
o contorno passou a acompanhar a cintura sentada, os punhos e a borda do
capuz. Pele e montaria permanecem protegidas, com comparação nas duas variantes.

O teste de regressão em [gustavo-audit-before.json](gustavo-audit-before.json)
reproduz quatro regiões escuras no modelo anterior. A auditoria de tecidos
agora inclui essas regiões e a preservação das mãos/montaria. A sombra
triangular da calça foi isolada no Unity desligando apenas as sombras; o
ajuste artístico da iluminação continua proposto na avaliação geral.

A [revisão visual](../visual-review-20260925/index.html) reúne a comparação do
Gustavo, novas capturas da Main e prioridades para os seis personagens, menu,
tabuleiro e seleção. Ela diferencia correções aplicadas de propostas. Os
closes não representam uma aprovação de todo o acabamento nem um teste em VR.

A cópia imediatamente anterior desta revisão fica em
`.local/character-production/gustavo-review-20260925/`, com `scope.json` e os
mesmos caminhos relativos. Para restaurar, fechar o Editor e copiar somente
os caminhos listados, preservando alterações posteriores e arquivos `.meta`.
Os originais e estudos de animação não fazem parte da substituição.
