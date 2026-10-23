# Stockfish 18 no protótipo web

O Web Player usa Stockfish.js **18.0.0 lite-single** em um Web Worker local.
O motor fornece somente a jogada candidata; `ComputerTurnCoordinator` descarta
fotografias de outra revisão/turno e `ChessRulesAdapter` continua validando a
legalidade antes de alterar a partida. Os transportes desktop e Android e os
perfis de dificuldade existentes continuam disponíveis pelos seus ramos da factory.

## Preparar e integrar

Na raiz deste worktree:

```bash
python3 tools/setup_stockfish_web.py
```

O script não instala pacotes nem executa hooks npm. Ele baixa os artefatos
oficiais fixados, verifica SHA-256 e prepara `.local/stockfish-web/engine/`:

| Arquivo | Uso |
| --- | --- |
| `stockfish-18-lite-single.js` | Worker UCI (20.670 bytes) |
| `stockfish-18-lite-single.wasm` | Motor e rede de avaliação embutida (7.295.411 bytes) |
| `Copying.txt`, `AUTHORS`, `README.upstream.md` | Licença GPLv3, créditos e instruções originais |
| `stockfish.js-18.0.0-source.tar.gz` | Fonte correspondente ao commit fixado |
| `SOURCE.txt`, `manifest.json` | Proveniência, variante e hashes dos artefatos |

Copiar **a pasta inteira** para `<output-do-Web-Player>/stockfish/` após o build.
Não copiar esses binários para `Assets`/`StreamingAssets`. O bridge resolve a
URL relativa à página (`document.baseURI`), sem CDN, API ou acesso a credenciais.
Servir o diretório do player via HTTP(S), com JS como `text/javascript` e WASM
como `application/wasm`. Esta variante não exige `SharedArrayBuffer`, COOP ou
COEP. `file://` não é um fluxo suportado para testar Worker/WASM.

A seleção da factory é `UNITY_WEBGL && !UNITY_EDITOR`. No Editor, mesmo com
WebGL como alvo, a factory usa o motor desktop. O build Web é necessário para
comprovar o Worker real.

O cache e o destino são verificados novamente ao repetir o preparo. Um arquivo
existente com conteúdo diferente é preservado e causa erro legível, em vez de
ser sobrescrito. `--root <diretório>` permite um preparo isolado em outro local.

## Busca, prazos e ciclo de vida

`WebStockfishMoveChooser` implementa `IMoveChooser` e `IPlayerLoopMoveChooser`.
`TryTakeResult`, já chamado por `ChessGameController.Update`, chama `Poll()` e
consome as tarefas concluídas. Essa rota não usa `Task.Run`, `Task.Delay`,
`System.Diagnostics.Process` nem timer gerenciado.

O relógio padrão é `Time.realtimeSinceStartupAsDouble`, independente de
`timeScale`; os testes injetam `Func<double>`. Cada pedido tem um identificador.
O carregamento/handshake tem prazo de 60 segundos, permitindo baixar e compilar
o WASM no primeiro uso. Quando `readyok` inicia a busca, um evento `searching`
inicia o prazo existente do perfil (6/6,5/7,2 segundos). A busca mantém
Skill Level 0/6/14 e `go movetime` 150/500/1200 ms. A variante lite usa uma rede
menor, portanto esses nomes não representam Elo calibrado nem força equivalente
à versão desktop completa.

O Worker persiste entre jogadas e recebe posição inicial mais histórico UCI.
O cancelamento, o menu, reinício, suspensão e `Dispose` invalidam o pedido e
encerram o Worker. Uma nova sessão cria outro Worker. As funções de callback
comparam a geração do Worker; o C# também ignora identificadores antigos.
Mensagens `info` não entram na fila de resultados. Falha de download/WASM,
erro do Worker, resposta malformada ou prazo excedido falham sem jogada de
contingência, preservando a opção de tentar novamente/voltar ao menu.

Os prazos são conferidos antes de consumir a fila. Ao retomar uma aba que ficou
sem atualizações além do prazo, uma resposta tardia é descartada. Enquanto não
há `Update`, o relógio não é processado; não se cria uma thread para contornar
o ciclo de vida do navegador.

## Verificações e limites

Executado em 2026-10-23 neste worktree:

```bash
python3 tools/setup_stockfish_web.py
python3 -m unittest tools/tests/test_setup_stockfish_web.py -v
node --test tools/tests/stockfish_web_bridge.test.cjs
```

- Preparo oficial concluído, com hashes verificados; sem instalação npm.
- Python: **5/5** casos passaram (pin, cache, preservação e rejeição de download corrompido).
- Node: **6/6** casos passaram (handshake, histórico/dificuldade, reuso, descarte,
  falhas, cancelamento/Dispose e integração com o WASM real).
- O teste real devolveu `d2d4`/`d7d5` em turnos sucessivos pelo protocolo do bridge.
  Ele roda o WASM por stdin/stdout no Node; isso ainda não comprova o Web Worker
  no navegador nem a integração IL2CPP.

`game/Assets/Tests/EditMode/WebAITests.cs` cobre a rota player loop, relógio,
prazos de carregamento/busca, revisão, IDs antigos, respostas inválidas, retry e
Dispose. Sua execução no Unity e o build/Web Player ficam com o principal para
evitar instâncias concorrentes sobre o mesmo projeto. A aceitação ainda exige
partida real em HTTP localhost com ambos os lados, dificuldades, reinício/menu,
erro recuperável e responsividade durante pensamento. Esses resultados devem
ser registrados no relatório do build, sem converter testes Node em aceitação
do navegador.

## Proveniência e referências

- [Registry fixado `stockfish@18.0.0`](https://registry.npmjs.org/stockfish/18.0.0):
  `gitHead=3f5d4d17bee569cbec41d5f6abed23717eb17c01`.
- [README correspondente](https://github.com/nmrugg/stockfish.js/blob/3f5d4d17bee569cbec41d5f6abed23717eb17c01/README.md)
  e [release v18.0.0](https://github.com/nmrugg/stockfish.js/releases/tag/v18.0.0).
- JS SHA-256: `2278005057f381491f1c9bb3e44c9f5920b3a00bef9759e33cc6582769a1f1fe`.
- WASM SHA-256: `a8fbc05ec6920b56d7485826dcb02c5ffd2826bcbf751cf973046f237a9096f1`.
- Fonte SHA-256: `3eff01c34587e0dbb1604f383faa50820dd99d5037184a1eef3577790a110cff`.
- A fonte contém o passo upstream para obter `nn-9067e33176e8.nnue`, que já está
  embutida no WASM distribuído. Não houve recompilação do motor nesta entrega.
- [Limitações .NET do Unity 6.3 Web](https://docs.unity3d.com/6000.3/Documentation/Manual/web-dotnet-api-support.html)
  e [plugins JavaScript](https://docs.unity3d.com/6000.3/Documentation/Manual/web-interacting-browser-js.html).
