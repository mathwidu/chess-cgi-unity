# Pacote Stockfish 18 para Android ARM64

O preparo local usa o código do release oficial `sf_18`, sem alteração dos
algoritmos. O executável ARMv8 distribuído nesse release é `ET_EXEC`; por isso o
script recompila a mesma fonte como PIE para execução com `ProcessBuilder`.
O nome `libstockfish.so` permite empacotar e extrair o executável no
`nativeLibraryDir` do aplicativo. O arquivo não é uma biblioteca para `dlopen`.

```bash
python3 tools/setup_stockfish_android.py
python3 tools/setup_stockfish_android.py --verify-only --offline
python3 -m unittest discover -s tools/tests -p 'test_setup_stockfish_android.py' -v
```

O NDK padrão vem do Editor declarado em `game/ProjectSettings/ProjectVersion.txt`.
Neste checkout é Unity `6000.3.16f1` com NDK r27c (`27.2.12479018`). Um NDK dessa
mesma revisão pode ser informado com `--ndk-root`. Não há instalação de
ferramentas, abertura do Unity ou acesso ao headset durante esse preparo.

## Entradas fixadas

- Release: <https://github.com/official-stockfish/Stockfish/tree/sf_18>.
- Pacote: `stockfish-android-armv8.tar`, 115.578.880 bytes, SHA-256
  `e2eca54b0e3189ec7de338133c2b34fa8f5cdec3d2473519b414a5cb6815e768`.
- Executável original, usado somente para identificação: SHA-256
  `cc3b3b74466b6d85cdc0f8cfdad4111c6a44bd0698f4eab96fea7b359a54d3d4`.
- Rede grande: `nn-c288c895ea92.nnue`, SHA-256
  `c288c895ea924429ea9092e3f36b2b3c1f00f2a3a4c759ff7e57e79e3b43e4a7`.
- Rede pequena: `nn-37f18f62d772.nnue`, SHA-256
  `37f18f62d772f3107e1d6aaca3898c130c3c86f2ab63e6555fbbca20635a899d`.

O script valida SHA e tamanho antes de extrair o pacote. A extração aceita
somente arquivos regulares e diretórios sob `stockfish/`, rejeitando travessia
de diretório, links, entradas especiais e caminhos repetidos. Arquivos existentes
com conteúdo divergente provocam falha e permanecem preservados.

## Compilação e reprodução offline

O comando usa o Makefile original com `COMP=ndk ARCH=armv8 OS=Android KERNEL=Linux`,
alvo `aarch64-linux-android29-clang++`, otimização de release e LTO do upstream.
O Makefile fornece `-fPIE -pie` e `-static-libstdc++`; os únicos flags adicionais
do linker são:

```text
-Wl,-z,max-page-size=16384 -Wl,-z,common-page-size=16384 -Wl,--build-id=sha1
```

O ambiente de compilação é controlado, com `SOURCE_DATE_EPOCH=1700000000`,
`LC_ALL=C` e flags de Git vazios. O manifesto registra o hash do compilador,
argumentos, hash da receita, hash do inventário de fontes e de cada rede.
`--rebuild --offline` força uma nova compilação usando somente o cache verificado;
o preparo normal reutiliza uma compilação cujo registro e binário ainda conferem.

O cache fica em `.local/stockfish/android/`. Ele preserva o pacote oficial,
ambas as redes e um bundle determinístico
`stockfish-18-android-corresponding-source.tar.gz` com as fontes completas do
pacote, scripts, redes, GPL, autores, inventário de hashes e `REBUILD.txt`.
Esse arquivo permite reconstruir o executável offline com o NDK correspondente.
O tar usa ordem estável, timestamps zerados e gzip sem data ou nome local.

O bundle de fontes deve acompanhar cada APK entregue a outra pessoa. Mantenha-o
junto do artefato local; o caminho relativo e SHA estão no manifesto do APK.
Ele fica fora de `Assets` para não duplicar as redes no aplicativo. O script
somente prepara os arquivos; a associação desse bundle à entrega do APK cabe
ao fluxo de build/distribuição.

## Arquivos do Unity e verificações

- `game/Assets/Plugins/Android/Stockfish/arm64-v8a/libstockfish.so`: PIE gerado;
  binário local grande, excluído do Git pelo responsável pela integração.
- `.meta` do executável: `PluginImporter` apenas Android/ARM64,
  `isPreloaded: 0`, Any e Editor desabilitados.
- `game/Assets/StreamingAssets/ThirdParty/Stockfish/`: `manifest.json`,
  `Copying.txt`, `AUTHORS` e `SOURCE.txt`, incluídos no APK.

O verificador usa os headers ELF e `llvm-readelf` do NDK: exige ELF64/AArch64,
`ET_DYN`, ponto de entrada, `DF_1_PIE`, `/system/bin/linker64`, todos os segmentos
`LOAD` com alinhamento de pelo menos 16 KiB e congruência de offset/endereço.
`DT_NEEDED` aceita somente bibliotecas do sistema Android (`libc.so`, `libm.so`
e `libdl.so`), evitando dependência de `libc++_shared.so`.

Também confirma que os bytes completos das duas redes padrão estão embutidos,
confere o hash do executável instalado, metadados, avisos e bundle offline.
Os detalhes ELF e o log de compilação ficam em
`.local/stockfish/android/builds/<hash-da-receita>/`.

A licença upstream é `GPL-3.0-or-later`; o texto original está preservado sem
alterações. Preparar o pacote não comprova sua importação pelo Unity,
extração pelo Android, inicialização UCI, jogada de IA ou execução no Quest.
Esses passos pertencem à integração e à validação física do principal.
