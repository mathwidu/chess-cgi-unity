# Assets temporários do protótipo Web

`ChessWebAssetOptimizer.Stage()` prepara assets somente com o alvo WebGL ativo.
O chamador deve limitar seu tempo de vida ao build:

```csharp
BuildReport report;
using (ChessWebAssetOptimizer.Stage())
{
    report = BuildPipeline.BuildPlayer(options);
}
```

O `using` restaura os assets também quando `BuildPlayer` lança uma exceção. Uma
falha durante a preparação aciona a mesma recuperação antes de propagar o erro.
O script não modifica GLB/PNG fonte nem configurações de importação existentes.

## O que entra no build

- Os sete diretórios `Resources/CustomPieces/*_Assets` e o prefab legado
  `Pawn_Mathwidu_Redhead.prefab` saem temporariamente de `Resources`, usando
  `AssetDatabase.MoveAsset`. Os GUIDs são preservados. As seis peças
  personalizadas atuais, incluindo `Pawn_Mathwidu_Redhead_v2`, continuam no lugar.
- Os PNGs embutidos de `ChessBoard.glb` e `FeevaleComputerLab.glb` são extraídos
  sem decodificar/regravar os pixels. São importados como texturas temporárias
  com override WebGL ETC2 RGBA8, qualidade 100 e limite de 1024 por dimensão.
  O estágio preserva sRGB/linear do `graphicsFormat` original, filtro, wrap,
  mipmaps e anisotropia. Usa `TextureImporterType.Default` também para normais:
  o shader glTFast espera os canais RGB originais, sem o empacotamento de normal
  map do Unity. Não há `Blit`, mudança de gama, inversão de canal ou nova UV.
- Malhas e materiais da sala/tabuleiro são clonados em assets nativos. A malha
  mantém geometria, normais, tangentes e UVs; os materiais mantêm shader,
  propriedades e transformações de textura. Só as referências às texturas mudam.
- Cópias regulares e totalmente desempacotadas dos dois prefabs de
  `Resources/Environment` substituem temporariamente os originais nos mesmos
  caminhos carregados por string. Os originais ficam fora de `Resources` e
  retornam com seus próprios GUIDs. As referências das cópias apontam para as
  malhas/materiais temporários, sem depender dos GLB originais.
- `Assets/Materials/Board_Light.mat` e `Board_Dark.mat`, usados pelas casas
  interativas, recebem temporariamente as texturas Web. A escrita usa um asset
  serializado pelo próprio Unity e guarda cópia recuperável com SHA-256; a
  restauração retorna os bytes exatos dos arquivos e confere seus metadados.

Antes de permitir o build, o estágio consulta as dependências da cena `Main` e
de todos os assets ainda em `Resources`. Se algum modelo legado, prefab
estacionado ou GLB original da sala/tabuleiro continuar alcançável, a preparação
falha e restaura o projeto. Esse bloqueio também detecta novas referências que
tornem a exclusão de um asset legado inválida.

## Recuperação

Antes do build, `ChessCgiWebBuild` exige que QualitySettings e os pipelines
nativos estejam salvos. Mantém cópias dos arquivos em
`.local/unity-web-native-settings/`, preserva os slots por GUID e restaura memória
e disco ao terminar. As cópias só são removidas depois de conferir os bytes.
Se o processo for encerrado à força, um novo build recusa sobrescrever esse
backup: com o Editor fechado, compare os caminhos listados em `files.txt` com
seus equivalentes dentro de `game/`, preserve uma cópia de quaisquer mudanças
posteriores e restaure os arquivos do backup. Só então remova a pasta de backup.
Essa recuperação não usa `git restore`, portanto preserva configurações locais
que já estavam salvas antes da compilação.

Os assets temporários ficam em `game/Assets/__ChessWebBuildStage`. O journal e
os snapshots ficam em `.local/unity-web-asset-stage/` na raiz do repositório,
ignorada pelo Git. Não apague nenhuma dessas pastas antes de restaurar.

O journal é gravado antes de cada movimentação/escrita. A recuperação identifica
os assets por GUID, confere o SHA-256 dos snapshots e recusa sobrescrever um
arquivo alterado concorrentemente. Se houver conflito, mantém o journal e os
snapshots para o principal reconciliar. A restauração é repetível após falha
parcial; só remove os temporários quando todas as restaurações terminaram.

Um novo `Stage()` recupera um journal anterior antes de começar. Após matar o
Editor durante um build, restaure no mesmo checkout e projeto `game`:

```text
Chess CGI > Build > Restore interrupted Web asset stage
```

Também é possível chamar
`-executeMethod ChessWebAssetOptimizer.RestoreInterruptedStage` no Editor da
versão do projeto. Use uma única instância do Editor nesse projeto. Uma pasta
de estágio sem journal ou com identidade divergente bloqueia a preparação para
inspeção manual. Em uma interrupção entre criar a pasta e registrar seu GUID,
os fontes ainda não foram movidos; a pasta residual requer essa inspeção.

## Evidência e limites

Em 23/10/2026, a inspeção do pacote instalado `com.unity.cloud.gltfast` 6.14.1
confirmou que o `GltfImporter` cria as texturas embutidas como subassets, sem
`TextureImporter` nem configuração de compressão por plataforma. Portanto,
overrides de PNGs externos não alcançam essas texturas GLB. O estágio usa o
`TextureImporter` apenas nas imagens temporárias extraídas. A API de
[overrides por plataforma](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/TextureImporter.SetPlatformTextureSettings.html)
é do Unity; a semântica das texturas embutidas foi confirmada nos fontes locais
`Editor/Scripts/GltfImporter.cs` e `Runtime/Scripts/GltfImport.cs` do glTFast.

A análise estática validou chunks GLB, intervalos das imagens, nomes únicos e
payloads PNG das 22 imagens. Dezenove já têm 1024 × 1024; apenas as três imagens
`NaturalAshDesk` da sala têm 2048 × 2048 e são reduzidas. Os dois GUIDs de prefab
Environment não têm referências serializadas externas em `Assets`; os pontos
de uso encontrados carregam `Environment/ChessBoardFrame` e
`Environment/FeevaleComputerLab` por `Resources.Load`.

A compilação isolada do script contra os assemblies do Unity 6000.3.16f1 passou.
O teste `VerifyStageRoundTrip` também passou no Editor com alvo WebGL: registrou
`CHESS_WEB_ASSETS_STAGED` (22 texturas), `CHESS_WEB_ASSETS_RESTORED` e
`CHESS_WEB_ASSETS_ROUND_TRIP_PASSED`. As pastas de estágio/journal foram removidas
e não surgiram mudanças nos materiais/prefabs originais no diff.

O build completo de 23/10/2026 também passou com o estágio ativo. O download de
`Web.data.gz` caiu de 134.691.964 para 25.427.852 bytes; o carregamento inicial
total ficou em 36.670.918 bytes. `Builds/web-build-assets.json` contém as cópias
geradas e não contém os legados ou os GLB originais do ambiente. Após a saída
do Editor, os nove hashes de qualidade, pipelines, materiais e prefabs protegidos
conferiram e as pastas de recuperação estavam ausentes. A recuperação do estágio
após interrupção forçada também foi executada com sucesso na pausa da tarefa.

A comparação no navegador entre `Builds/WebQA/baseline-desktop.jpg` e
`final-desktop.jpg` manteve sala, tabuleiro e personagens na vista de jogo
inspecionada. IA e toque foram repetidos no pacote final. Isso não comprova
fidelidade em todos os ângulos nem disponibilidade/custo de ETC2 em aparelhos
físicos; esses limites continuam na matriz de validação do protótipo.

Roteiro de verificação para futuras alterações:

1. Executar `ChessWebAssetOptimizer.VerifyStageRoundTrip` no Editor WebGL e
   observar `CHESS_WEB_ASSETS_STAGED`, `CHESS_WEB_ASSETS_RESTORED` e
   `CHESS_WEB_ASSETS_ROUND_TRIP_PASSED`, sem pasta/journal residual.
2. Construir com o `using` e comparar `Builds/web-build-assets.json` e
   `Builds/Web/Build/*.data.gz` com a base de 134,69 MB comprimidos. O relatório
   deve mostrar os assets gerados e excluir os sete GLB legados e os dois GLB
   originais de ambiente; `packedSize` não é tamanho de download comprimido.
3. Conferir no navegador personagens, sala, madeira, normais e casas do
   tabuleiro, além de IA e toque. Repetir a comparação visual após alterar ETC2
   ou o limite de 1K da mesa. Um build menor não prova fidelidade ou memória de GPU em
   todos os navegadores; a disponibilidade de ETC2 varia por dispositivo.
4. Conferir o diff após sucesso e após um build que falhe: nenhum fonte,
   material original, prefab original ou `.meta` deve mudar por causa do estágio.
