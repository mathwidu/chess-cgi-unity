# Publicação integrada em cicochess.dev/jogar/

Preparação local de 28/10/2026. A receita antiga para o subdomínio chess foi
substituída por uma release coordenada com `mathwidu/cicochess-site`. O Unity
continua sendo a única fonte do jogo. Este documento não autoriza publicação.

## Responsabilidades

- `chess-cgi-unity`: fonte Unity, build Web, pacote auditável e imagem Nginx
  `ghcr.io/mathwidu/chess-cgi-unity-web`.
- `cicochess-site`: launcher `/jogar/`, imagem Astro, registro dos dois digests,
  executor e workflows de produção.
- Stack existente `cicochess-site`: serviço `web` mantém o site/feedback;
  novo serviço `game` entrega somente `/jogar/runtime/` no mesmo domínio.
- Não há outro repositório Unity, novo DNS, banco ou volume do jogo.
  Os saves continuam locais por origem/navegador, sem sincronização entre aparelhos.

O runtime usa Nginx sem root, raiz somente leitura, 128 MiB, 0,50 CPU e tmpfs de
16 MiB. O Traefik existente encaminha `/jogar/runtime/` com prioridade 200,
sem remover prefixo ou aplicar a compressão do site. TLS termina no Traefik.

## Compilação e pacote local

Usar sempre o caminho ABSOLUTO do projeto com Unity CLI: há dois checkouts
registrados com o mesmo nome `game`. Não executar duas instâncias no worktree.

```bash
unity run "$PWD/game" --timeout 1800 -- -buildTarget WebGL -executeMethod ChessCgiWebBuild.Build -logFile "$PWD/.local/release-prep/build.log"
```

Os fontes precisam estar revisados, commitados e limpos para empacotar. Arquivos
incidentais do Editor são comparados com cópia anterior, nunca restaurados em
massa sobre mudanças do usuário. O checkout original Quest não é usado.

```bash
revision="$(git rev-parse HEAD)"
python3 tools/web_release.py pack --build-dir Builds/Web --output-dir .local/release-candidate --revision "$revision"
python3 tools/web_release.py verify --context .local/release-candidate/context --revision "$revision"
```

O pacote schema 2 preserva fontes/licença Stockfish, inclui receitas verificadas
e um inventário estrito. O build_id compromete bytes servidos e MIME/codificação.
Brotli qualidade 9 descomprime o gzip original antes de comprimir; cada arquivo
passa por roundtrip e comparação byte a byte. Node 22/zlib, sem dependências novas.
O manifesto público do launcher fica em `/jogar/runtime/manifest.json`; metadados
em `/jogar/runtime/release.json`, ambos no-store. Assets da versão são imutáveis.

Para a release seguinte, importar somente o runtime ativo do contexto aceito:

```bash
python3 tools/web_release.py pack --build-dir Builds/Web --output-dir .local/release-next --revision "$revision" --previous-context CONTEXTO_ACEITO --previous-sha256 HASH_DO_MANIFEST_PRIVADO
```

Esse checksum identifica `CONTEXTO_ACEITO/manifest.json`, não o manifesto público.
Cada pacote guarda o runtime atual e no máximo um anterior. O executor do site
recusa upgrade que elimine o build atualmente publicado. Contextos standalone
schema 1 não são aceitos como anterior; a primeira publicação integrada não tem
runtime anterior na VPS. Nunca substituir bytes sob uma URL já publicada.

## Verificar e publicar somente após autorização

1. Testar pacote, imagem amd64, site+runtime via proxy local, IA e retomada;
   preservar evidência visual e conferir em dispositivo físico.
2. Registrar SHA Unity, SHA site, checksum do TAR, receita, domínio, serviços,
   atualização do executor, rollback e limitações no plano final.
3. Com aprovação, disponibilizar o workflow manual na branch padrão do Unity
   sem mesclar indiscriminadamente todo o trabalho Quest/Web. Publicar a revisão
   aprovada e seu TAR na release `web-<SHA12>`, sem substituir assets existentes.
4. Executar `web-publish.yml` com SHA/checksum exatos. O CI verifica pacote,
   receitas, testes e o mesmo container antes de publicar a imagem no GHCR.
   Capturar digest e build_id; não tornar o pacote público por conveniência.
5. Conferir acesso da credencial dedicada de leitura à imagem privada do jogo.
   No site, publicar a revisão aprovada e executar o fluxo coordenado com
   `game_source_sha`, `game_image_digest`, `game_build_id` e SHA do site.
6. O executor do site disponibiliza/verifica o runtime antes de atualizar o
   launcher e registra ambos. Nenhuma etapa instala Unity na VPS.

`tools/deploy_web.py` agora aceita apenas `plan`; `apply` foi retirado, assim
como o executor antigo e o ativador de DNS. O deploy pertence ao site.

## Retorno e limites

O registro da release vincula site e runtime. Antes de mudar serviços, o
executor guarda a operação pendente com baseline e alvo. Falha parcial pode
ser recuperada por rollback explicitamente autorizado à baseline conhecida.
No retorno à primeira versão do site (`61fcf35`), só o serviço game gerenciado
é removido; o site e o volume de feedbacks são preservados. Rollback não reverte
nem apaga localStorage. Uma aba da versão retirada pode precisar recarregar.

Hash do pacote identifica os bytes aprovados, digest identifica a imagem;
a associação entre fonte e compilação requer registro da sessão de build.
CI de publicação verifica o build fornecido, mas não recompila Unity.
Testes locais não comprovam HTTPS público, acesso GHCR na VPS ou aceite mobile.
Logs Unity podem conter autenticação temporária: manter locais e nunca anexar
seu conteúdo integral a Git/Actions/relatórios.
