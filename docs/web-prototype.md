# Protótipo Unity Web

Alvo: navegador desktop e mobile, com peças personalizadas, adversário local
e controles por toque. Unity **6000.3.16f1**; WebGL 2, WebAssembly sem threads
gerenciadas, renderização URP. A base deste protótipo é o commit `30bcbfe`.
O checkout original e suas mudanças de VR continuam separados.

## Preparar e executar

1. Instalar **Web Build Support** para a versão exata da Unity (sem atualizar o Editor).
2. Na raiz deste checkout, executar `python3 tools/setup_stockfish_web.py`.
3. Executar:

```bash
unity run ./game --timeout 420 -- -buildTarget OSXUniversal -executeMethod ChessCgiWebBuild.Prepare -logFile ../Builds/web-prepare.log
unity run ./game --timeout 1800 -- -buildTarget WebGL -executeMethod ChessCgiWebBuild.Build -logFile ../Builds/web-build.log
python3 tools/serve_web.py
```

O primeiro comando é a preparação no Mac: grava os overrides antes da troca
de plataforma, evitando converter as texturas originais em 4K para Web.

Abrir <http://127.0.0.1:8090/>. O servidor envia `Content-Encoding: gzip`
e o MIME correto para WASM; servir os arquivos com headers incorretos não
constitui uma falha do jogo. O motor em `stockfish/` é carregado sob demanda,
sem CDN ou API externa. Não existe multiplayer online neste protótipo.

Para QA, `?qa=1` mostra um painel de diagnóstico com estado da partida,
coordenadas de interação e amostras de FPS. O painel não controla as regras.
`heapBytes` é memória alocada reportada pela Unity, não RAM total da aba/GPU.

## Preservação e perfis

- Desktop e Quest mantêm seus defaults e assets. Web inicia com personagens
  e usa uma chave de preferência de peças clássicas própria.
- `ChessCgiWebBuild` cria `Web_RPAsset` a partir de uma cópia do pipeline Mobile.
  Captura os pipelines por GUID e os arquivos de qualidade/URP antes do build;
  restaura os slots em memória e os bytes depois dos preprocessadores da Unity.
  Exige que esses ajustes estejam salvos e mantém cópias de recuperação locais
  até confirmar a restauração. O roundtrip dos perfis passou no Editor, com
  comparação dos nove arquivos protegidos após o processo terminar.
- Texturas recebem override **WebGL** de 1K (fundo do menu 2K). Originais,
  overrides Standalone e Android permanecem. ETC2 é o primeiro alvo de compressão.
- [O estágio de assets](web-assets.md) exclui legados de `Resources` e substitui
  temporariamente sala/tabuleiro por cópias com texturas comprimidas. Os fontes
  retornam no `Dispose`; um journal permite recuperar uma interrupção do Editor.
- O canvas limita a densidade de pixels a 1,25; o perfil em memória reduz a
  resolução 3D, até 0,65, após amostras sustentadas abaixo de 26 FPS.
- Os previews Web usam render targets menores (menu 645×588; peça 736×440),
  MSAA 2× e abertura do detalhe da peça sob demanda.
- O template limita o canvas à área segura informada pelo CSS do navegador;
  o HUD também respeita a área informada pela Unity.
- Ocultar a página cancela a busca da IA e suspende a partida; retornar permite
  retomar a vez. Recarregar/fechar a página ainda não recupera a partida.

## Critérios e evidência

A cobertura será avaliada por capacidade, sem presumir um modelo específico:
Android de entrada/intermediário com Chrome, iPhone com Safari e computadores
com vídeo integrado. A matriz física deve incluir um Android com pouca memória,
um iPhone de geração anterior e um notebook integrado; nenhuma dessas classes
recebe aprovação de desempenho apenas por emulação no Mac. Verificar primeira
abertura em rede limitada, uma partida prolongada, alternância de aplicativo,
rotação da tela e recuperação de falha da IA.

Metas propostas: 30 FPS sustentados em Android de entrada, iPhones de gerações
anteriores e notebook integrado; retrato de 360 px legível; partida com IA sem
travar controles; cancelamento seguro ao reiniciar; carregamento inicial enxuto.
O alvo inicial de aproximadamente 15 MB transferidos é um orçamento a medir,
não uma garantia deste primeiro protótipo. Modelos ainda preservam a geometria
original; uma revisão de LOD depende de medição e comparação visual.

Testes na Unity 6000.3.16f1: 74/74 EditMode. A seleção PlayMode teve 40/42 na
primeira execução; uma regressão de criação do preview desktop foi corrigida.
O reteste dos dois casos e dos dois testes de layout Web com personagens
passou 4/4. O teste visual de iluminação falhou uma vez e passou no reteste;
isso não estabelece a causa da intermitência. Ponte IA: 6/6 testes Node,
incluindo respostas do WASM real; setup: 5/5 testes Python.

O primeiro build executou no navegador: partida com personagens, lance humano
`e2-e4`, resposta real do Stockfish `e7-e5`, sem erro reportado. No Chrome,
eventos de toque emulados validaram seleção, lance, histórico, ficha da peça,
zoom por pinça, retorno da câmera e rolagem do menu. Viewports 360×640 e 640×360
mantiveram tabuleiro e ações acessíveis; 1280×720 também foi inspecionado.
Esses testes rodaram no Mac, inclusive os viewports e toques emulados.

O build otimizado terminou com sucesso em 23/10/2026. A soma dos arquivos do
carregamento inicial caiu de **145.935.065 para 36.670.918 bytes** (74,87% a menos).
Os valores abaixo são MB decimais, de arquivos efetivamente servidos comprimidos,
e não o tamanho descomprimido dos assets ou a memória ocupada:

| Conteúdo | Download |
| --- | ---: |
| Dados e assets (`Web.data.gz`) | 25,43 MB |
| Runtime (`Web.wasm.gz`) | 11,12 MB |
| HTML, loader e framework | 0,12 MB |
| **Inicial** | **36,67 MB** |
| Stockfish JS/WASM, somente quando a IA é acionada | +7,32 MB |

O orçamento proposto de 15 MB ainda não foi atingido. A geometria foi mantida;
malhas da sala, modelos e fundo do menu são candidatos à próxima medição de
custo. A entrega deste protótipo não aprova o desempenho em aparelhos modestos.

No build otimizado, a partida desktop novamente aceitou `e2-e4` e uma resposta
real da IA, manteve os 32 personagens e não registrou erros no console. A
comparação visual com a captura anterior manteve materiais da sala/tabuleiro
e personagens na vista inspecionada. Uma amostra local ficou próxima de 60 FPS.
No Chrome com user agent Android simulado, o perfil iniciou em escala 0,85 e
limite de 30 FPS; o lance por toque recebeu `d7-d5` da IA. Histórico, ficha da
peça, pinça sem lance acidental, retorno da câmera e rotação 360×640/640×360
funcionaram. As amostras de 30 FPS são do Mac com emulação, não de um Android.
A Unity reportou cerca de 98 MB alocados, sem incluir a RAM total do navegador,
memória gráfica ou a memória do Worker Stockfish.

O relatório de assets exclui os legados e os GLB originais do ambiente. Os nove
arquivos protegidos conferiram por SHA-256 após o encerramento da Unity; não
restaram estágio, journal nem backup de recuperação. Tamanhos e capturas locais:
`Builds/web-download-final.json`, `Builds/web-build-assets.json` e
`Builds/WebQA/final-{desktop,mobile-portrait,mobile-landscape}.jpg`.

Emulação não substitui teste físico de toque, aquecimento, área segura e memória
em Android/iPhone. Hospedagem pública e teste físico ainda não foram executados.

Referências de plataforma: [Unity Web .NET](https://docs.unity3d.com/6000.3/Documentation/Manual/web-dotnet-api-support.html),
[headers de compressão e Safari](https://docs.unity3d.com/6000.3/Documentation/Manual/web-server-config-apache.html),
[área segura CSS](https://developer.mozilla.org/en-US/docs/Web/CSS/Reference/Values/env).
