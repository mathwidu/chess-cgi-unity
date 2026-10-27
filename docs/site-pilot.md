# Piloto local CiCoChess em /jogar/

## Fonte e integração

O jogo continua em uma única fonte Unity 6000.3.16f1. A branch
`codex/unity-web-prototype` e seu worktree isolam este piloto do checkout Quest
que está em desenvolvimento. Não existe outro repositório com uma cópia do jogo.
`cicochess-site` cuida da página `/jogar/`, preservando estilos e mídia do colega;
Unity fornece apenas artefatos compilados e o contrato de sessão.

`tools/serve_site_pilot.py` serve o build em URLs por conteúdo e encaminha o
restante para o servidor Astro local. Ele só escuta em `127.0.0.1`, verifica os
hashes antes de servir, recusa caminho fora do inventário e distingue MIME,
gzip, erros, HEAD e revalidação. Não é um servidor de produção.

## Reproduzir

No worktree Web, com o engine preparado conforme `docs/web-prototype.md`:

```bash
mkdir -p .local/pilot
unity test ./game --mode EditMode --output "$PWD/.local/pilot/editmode.xml" --timeout 1200 -- -logFile "$PWD/.local/pilot/editmode.log"
unity test ./game --mode PlayMode --output "$PWD/.local/pilot/playmode.xml" --timeout 1200 -- -logFile "$PWD/.local/pilot/playmode.log"
unity run ./game --timeout 1800 -- -buildTarget WebGL -executeMethod ChessCgiWebBuild.Build -logFile "$PWD/.local/pilot/build.log"
python3 -m unittest tools.tests.test_site_pilot
node --test tools/tests/stockfish_web_bridge.test.cjs tools/tests/session_web_bridge.test.cjs
python3 tools/serve_site_pilot.py --site http://127.0.0.1:4321 --port 8090
```

Não executar dois processos Unity sobre o mesmo projeto. O build faz staging
recuperável dos assets Web e restaura as configurações nativas ao terminar.
Os logs locais do Unity podem conter dados de autenticação temporária e não
devem entrar no Git ou ser compartilhados integralmente.
O servidor Astro deve estar rodando em `127.0.0.1:4321`, conforme
`cicochess-site/docs/JOGO-LOCAL.md`. Abrir `http://127.0.0.1:8090/jogar/`.
Reiniciar o proxy após recompilar: um build alterado não deve reutilizar a URL
imutável anterior. Não copiar `Builds/Web` para o Git do site.

## Sessão

A chave `cicochess.match.v1` guarda um único slot, limitado a 64 KiB/2.048 lances.
O JSON contém versão, modo, lado humano, dificuldade e lances UCI. Uma posição
FEN sozinha perderia o histórico de repetição e as capturas; por isso a retomada
reproduz os lances em um adapter novo antes de alterar o controller ativo.
Veja `domainbook/domains/gameplay/features/retomar-partida-no-navegador.md`.

O bootstrap existe só em Web fora do Editor. Abrir o menu não grava um save.
O evento de persistência ocorre após aceitar o lance e antes da animação;
recarregar nesse intervalo retoma a posição lógica já aceita. A retomada limpa
a busca IA anterior, reconstrói histórico/capturas e atualiza o HUD sem repetir
os eventos/animações históricos. Storage bloqueado/quota não interrompem jogo.
Salvar é local por origem e não sincroniza aparelhos. Usar uma aba por vez.

## Desempenho sem alteração visual

O piloto preserva meshes, materiais, texturas, iluminação, animações e o perfil
Web existente. Não foi introduzida outra redução de qualidade.

1. Unity não é solicitado ao visitar o site ou a entrada `/jogar/`; o carregamento
   começa no clique de nova partida/continuação.
2. Stockfish só é carregado quando uma partida pede IA, usando Worker separado.
3. O identificador é o hash do inventário compilado. Assets dessa versão aceitam
   cache imutável; o manifesto usa `no-store`. Unity Data Caching permanece ativo.
4. Compressão deve ser comparada sobre os bytes originais, sem recomprimir gzip.
   Brotli não muda qualidade visual; seu ganho e o custo de carregamento precisam
   de medição, além de validação do servidor sobre HTTPS antes da publicação.

A documentação oficial de [compressão e headers do Unity 6.3](https://docs.unity3d.com/6000.3/Documentation/Manual/webgl-deploying.html)
orienta o contrato de entrega. Os limites finais devem considerar primeiro
download, cache aquecido, inicialização da IA, tempo até o tabuleiro, memória
e fluidez no aparelho alvo. FPS do Mac/emulação não comprova celular físico.

## Evidências desta rodada

Em 26/10/2026 (execução avançou para 27/10 UTC):

- EditMode: **100/100 passaram**, incluindo 26 casos novos de serialização/replay.
- PlayMode com alvo desktop: **159/160 passaram**, incluindo os 10 casos novos de
  sessão/controller. A falha `MenuStudioLightCannotRelightTheClassroom` registrou
  8.841 pixels alterados. É o mesmo sintoma arquivado em `TestResults/web-playmode.xml`
  de 23/10; o reteste daquele dia passou. Causa da intermitência não confirmada,
  e a suíte completa não é declarada verde.
- A primeira execução PlayMode com alvo Web passou 153/160: além dessa falha,
  seis casos esperavam textura desktop de 2.048 px, enquanto o perfil Web existente
  importa 1.024 px. Esses seis passaram ao executar no alvo desktop apropriado.
- Ponte de armazenamento: **15/15 Node**; ponte Stockfish: **7/7 Node**, incluindo
  respostas do WASM real; servidor de integração: **6/6 Python**.
- Site: **65 passaram / 17 skips condicionais** na suíte desktop/mobile;
  após o ajuste de conflito entre abas, **16/16 testes do launcher** passaram.
  `astro check`: zero erros, avisos ou hints.
- Revisão independente: guard de conflito entre abas revisado; sem novos achados
  concretos no escopo. Proteção otimista, não uma transação entre abas simultâneas.
- Os 252 arquivos previamente alterados no checkout original Unity foram
  comparados por SHA-256 e ficaram idênticos, assim como o HEAD original.

Build Web concluído em 26/10/2026, com os assets-fonte restaurados e três
configurações nativas restauradas pelo build. Serializações incidentais do
Editor em OpenXR, Web_RPAsset e UnityConnectSettings foram guardadas em
`.local/pilot/` e devolvidas aos bytes anteriores. Identificador do runtime:
`c396972b0ca978aaacbe42d0e4d8e0914fe2e3a628f0d23e15ce8d222b2403f2`.

### Navegador real no Mac, 26–27/10

- Entrada e CTA do site; Unity só inicia após o clique. Layout observado em
  1280×720 e 390×844. O visual do jogo permaneceu no perfil Web existente.
- Partida local: `e2-e4`, `d7-d5`, `e4xd5`; após reload, os três lances, a vez
  das pretas, 31 peças e todas as casas ocupadas coincidiram. Histórico e
  expositor de captura conferidos visualmente.
- Contra IA iniciante: `e2-e4`, resposta real `e7-e6`; retomada manteve os dois
  lances e a vez das brancas, sem uma resposta adicional. Outro lance no
  viewport estreito recebeu nova resposta da IA.
- Tela cheia e saída voltaram corretamente ao site. O CTA reabriu `/jogar/`
  com o save preservado. Uma segunda aba iniciou um save novo; a aba antiga
  recusou gravação e mostrou aviso. Sair de ambas preservou o save mais recente.
- Nenhum erro JavaScript reportado na inicialização observada. Unity emitiu
  um aviso de depreciação de sincronização de `persistentDataPath`; a sessão
  usa `localStorage`, e seu ciclo foi validado separadamente.
- Nesta rodada, interação estreita foi por mouse. O IAB não oferece
  `Input.dispatchTouchEvent`; a tentativa foi encerrada e a emulação foi
  restaurada. Os testes Unity de toque passaram, mas isso não substitui QA
  de gesto/dispositivo físico. Não foi medido FPS de celular.

As capturas e comparações estão em `cicochess-site/.local/pilot/`:
`jogar-entrada-desktop.png`, `jogar-entrada-mobile.png`,
`jogar-retomada-historico.png`, `jogar-partida-mobile.png`,
`jogar-conflito-abas.png`, `resume-comparison.json`, `resume-ai-comparison.json`.

### Download e próxima otimização

| Arquivos necessários | Servidos pelo piloto | Brotli qualidade 9 (comparação) |
| --- | ---: | ---: |
| Unity inicial | 36.670.139 B | 32.770.917 B |
| IA, sob demanda | 7.316.081 B | 5.482.843 B |
| Total da primeira partida com IA | 43.986.220 B | 38.253.760 B |

O relatório `.local/pilot/compression.json` foi produzido por:

```bash
node tools/measure_web_transfer.cjs Builds/Web .local/pilot/compression.json
```

É uma comparação de compressão sem perda: cada resultado foi descomprimido e
comparado byte a byte ao original. Não altera nem serve novos arquivos. O ganho
potencial total é **13,03%**; não é uma redução já aplicada ao piloto. O arquivo
de fontes do Stockfish para distribuição/licença não entra na transferência de
uma partida, pois não é solicitado pelo runtime.

No retorno com cache, o registro CDP `network-warm.json` mostrou loader,
framework e WASM sem transferência de corpo; o `.data.gz` foi revalidado com
304 e reutilizado pelo cache Unity. O primeiro registro CDP foi truncado e
não é usado para alegar bytes ou tempo de rede: os números da tabela são a
soma dos arquivos necessários, sem headers HTTP. Não há benchmark de banda
limitada ou aparelho físico nesta entrega.

O próximo passo de otimização é testar Brotli sobre HTTPS e incorporar o
empacotamento compatível à release. Depois, medir a inicialização/carregamento
da sala e dos personagens e avaliar entrega em etapas ou compartilhamento de
meshes/texturas. O orçamento anterior de ~15 MB ainda não foi atingido;
reduzir qualidade dos personagens não foi usado para contornar essa meta.

Graphify do worktree Unity foi atualizado. O índice do site foi recusado por
conter referência a fonte ausente/de outro checkout; a integração foi conferida
nos fontes atuais, sem tratar aquele grafo como evidência.

Não houve publicação, push, GHCR, mudança de VPS ou DNS.
