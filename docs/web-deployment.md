# Publicação do protótipo Web

Preparação local em 2026-10-23. Domínio escolhido pelo usuário:
`chess.matheusduarte.dev.br`. Esta documentação descreve a receita; não é
evidência de publicação. Nenhum push, release, imagem ou serviço remoto foi
criado nesta etapa. O plano com SHA e hash do pacote fica em
`.local/web-release/PLAN.md`, depois do commit e empacotamento.

## Destino e isolamento

O protótipo fica em `codex/unity-web-prototype`, no worktree
`/Users/mathwidu/.codex/worktrees/chess-web-prototype/chess-cgi-unity`.
O checkout original e suas alterações de desktop/Quest não participam do deploy.
Não mesclar a branch inteira do protótipo em `main` para publicar esta versão.

Destino observado via inspeção limitada: alias SSH `vps-pessoal-nova`, usuário
`mathwidu`, host `srv1397065`, IPv4 `187.77.47.96`, Docker Swarm e Traefik 2.11.
O serviço novo será `chess-web_web`, na rede existente `network_swarm_public`.
Não há banco, volume persistente, secret novo, porta publicada no host nem
mudança no serviço compartilhado do Traefik. NGINX entrega arquivos estáticos;
o jogo e o Stockfish executam no navegador do visitante.

A imagem Linux amd64 usa NGINX oficial sem root, fixado por digest no Dockerfile.
Limites do serviço: 128 MiB e 0,50 CPU; reserva de 16 MiB e 0,05 CPU; raiz somente
leitura, capabilities removidas e `/tmp` de 16 MiB. Esses limites pertencem ao
servidor de arquivos e não representam consumo de memória do jogo no celular.

## Fluxo de release

1. Revisar e fazer commit local do protótipo, ferramentas e documentação.
2. Empacotar o build Unity já verificado, com checkout limpo e SHA completo:

   ```bash
   revision="$(git rev-parse HEAD)"
   python3 tools/web_release.py pack --build-dir Builds/Web --output-dir .local/web-release --revision "$revision"
   python3 tools/web_release.py verify --context .local/web-release/context --revision "$revision"
   ```

3. Validar localmente o contêiner, o pacote, a IA e os workflows. Registrar SHA,
   hash SHA256 do TAR, receita, destino, efeito e retorno no plano revisável.
4. Obter autorização explícita desse plano antes de qualquer alteração externa,
   conforme a skill pessoal `publicar-vps-pessoal`.
5. Publicar a branch do protótipo. No primeiro uso, incorporar somente
   `.github/workflows/web-publish.yml` em `main`, por uma branch/PR independente
   criada sobre o `main` atual. O GitHub exige o workflow manual na branch padrão.
   Não integrar código Unity dessa branch de bootstrap.
6. Criar release de prévia no commit aprovado, tag `web-<SHA12>`, com
   `chess-web-<SHA12>.tar.gz`. Não substituir assets de uma release existente.
7. Disparar manualmente `web-publish.yml` com `source_revision` completo e
   `bundle_sha256` aprovado. Ele testa as ferramentas e o Stockfish real, baixa
   o TAR, verifica cada hash e receita, constrói a imagem amd64 e executa o smoke
   HTTP nessa mesma imagem antes do push ao GHCR. Push comum não faz deploy.
8. Tornar público somente o pacote do jogo
   `ghcr.io/mathwidu/chess-cgi-unity-web`, se necessário, dentro do plano aprovado.
   Pacotes novos no GHCR podem iniciar privados. Confirmar pull na VPS antes
   de modificar o serviço. Não copiar a chave SSH do Mac para o Actions nem
   criar PAT como atalho. A publicação usa `GITHUB_TOKEN` temporário do job.
9. Capturar o digest produzido pelo job e fixá-lo na aplicação pelo Mac:

   ```bash
   python3 tools/deploy_web.py plan --image ghcr.io/mathwidu/chess-cgi-unity-web@sha256:DIGEST --revision SHA_COMPLETO
   python3 tools/deploy_web.py apply --image ghcr.io/mathwidu/chess-cgi-unity-web@sha256:DIGEST --revision SHA_COMPLETO
   ```

10. Conferir imagem efetiva, health e réplicas da VPS. Registrar run, SHA,
    digest e diretório remoto. Depois do DNS, conferir HTTPS e a partida pública.

### O que os hashes comprovam

O TAR contém somente a lista permitida de 18 arquivos públicos (17 do build e
`release.json`), duas receitas do contêiner e o manifesto. Inclui avisos,
licenças e fonte correspondente do Stockfish. A extração rejeita links, caminhos
escapando do destino, duplicatas, membros extras e tamanhos excessivos. Cada
arquivo é conferido por SHA256; as receitas também devem coincidir com o checkout.

`source_revision` identifica o checkout limpo selecionado no empacotamento e a
receita revisada. Este primeiro build foi compilado e testado localmente antes
do commit; não contém um carimbo de proveniência gerado pelo compilador. O elo
entre código e binário é atestação da sessão de build, não prova criptográfica.
O CI não recompila Unity. O hash do TAR identifica exatamente os bytes aprovados;
o digest identifica a imagem publicada. Não reutilizar um build antigo depois
de mudar código: recompilar e repetir a verificação funcional.

## DNS: única configuração do usuário após o deploy

Criar no provedor de `matheusduarte.dev.br`:

| Campo | Valor |
| --- | --- |
| Tipo | `A` |
| Nome | `chess` |
| Destino | `187.77.47.96` |
| TTL | `300` ou automático |
| Proxy/CDN, se existir | Somente DNS durante a ativação |

Não criar um registro AAAA para esse nome: IPv6 não faz parte desta receita.
Na inspeção inicial, A e AAAA retornaram NXDOMAIN. A criação/alteração de DNS
continua exclusivamente com o usuário.

O deploy inicia um processo limitado a 72 horas que consulta o DNS a cada
minuto. Ele só ativa o resolver de certificado da rota do jogo se todos os
endereços resolvidos forem o IPv4 acima e a imagem/revisão ainda forem as suas.
O lock de deploy impede que uma atualização provisória interrompa o ativador
de uma versão que venha a ser restaurada por rollback.

Ao confirmar DNS, ele adiciona a label `letsencryptresolver` somente ao serviço
do jogo, salva o estado de retorno e testa HTTPS com confiança de certificado
normal: `/healthz`, `/release.json` e cabeçalhos de WASM/data/Stockfish.
Há uma única ativação e até seis minutos de verificação. Nenhum DNS é alterado.
Isso usa o resolver HTTP-01 já existente; não reinicia/configura o Traefik.

Estado e log ficam no diretório informado pelo deploy:
`~/.local/share/chess-web/releases/<SHA12>-<DIGEST12>/activation.json` e
`activation.log`. Estados possíveis: `waiting_dns`, `dns_timeout`,
`waiting_https`, `https_verified`, `https_failed` e `stopped`.
O processo não sobrevive a reinício da VPS. Se expirado, parado ou com HTTPS
falho, inspecionar a causa antes de reativá-lo; não repetir ordens ACME às cegas.
Certificado válido e smoke técnico não substituem uma partida no navegador.

## Falha e retorno

Antes da mutação, o aplicador confirma host/usuário, manager, rede, propriedade
do serviço, colisões de domínio, arquitetura e label de revisão da imagem.
O lock cobre inventário, baseline de retorno, mudança e decisão de saúde.
Um serviço preexistente sem `current-stack.json` faz a ferramenta parar.

Se a imagem não ficar saudável em três minutos, a primeira instalação remove
somente a stack `chess-web` e verifica a ausência do serviço. Em atualização,
restaura `previous-stack.json` e verifica a imagem anterior saudável. Se o
retorno também falhar, reporta falha de convergência e exige inspeção limitada.
Não há prune, remoção de dados ou alteração de outras stacks.

Para retorno manual de uma instalação saudável, após autorização específica:

```bash
# Primeira instalação: remove somente o jogo. O ativador encerrará ao detectar ausência.
docker stack rm chess-web
# Atualização: usar o previous-stack.json preservado naquela release.
docker stack deploy --resolve-image never --with-registry-auth --compose-file CAMINHO/previous-stack.json chess-web
```

Não executar esses exemplos sem confirmar o diretório/revisão e a autorização.
Após retorno manual, conferir imagem/saúde e sincronizar `current-stack.json`
com a stack restaurada antes de uma nova atualização.

## Evidência local e limites

- Pacote inicial Unity: 36.670.918 bytes; IA JS/WASM sob demanda: 7.316.081 bytes.
- NGINX amd64 exercitado com os limites acima e raiz somente leitura.
  Smoke HTTP verifica bytes, MIME, gzip, cache, ranges, licenças e 404 reais.
- Partida no navegador servida pelo contêiner: 32 peças, lance humano e resposta
  da IA, `moves=2`, turno White, sem erro de runtime nem erro no console.
  Captura local: `Builds/WebQA/container-deployment.jpg`.
- Testes do empacotamento/extrator, guards de deploy e locks são locais;
  Docker/SSH dos testes de rollback são simulados. Nenhuma falha foi provocada
  em serviço da VPS.
- `actionlint` e `docker stack config` passaram localmente. O resultado do
  GitHub Actions e do deploy deve ser registrado após execução autorizada.
- Android/iPhone físicos ainda precisam de avaliação. A emulação já realizada
  verifica fluxo/toque/layout; não prova memória, temperatura ou FPS nesses aparelhos.

Referências oficiais: [Unity e NGINX](https://docs.unity3d.com/6000.3/Documentation/Manual/web-server-config-nginx.html),
[NGINX sem root](https://github.com/nginx/docker-nginx-unprivileged),
[workflows manuais](https://docs.github.com/en/actions/how-tos/manage-workflow-runs/manually-run-a-workflow),
[GHCR](https://docs.github.com/en/packages/working-with-a-github-packages-registry/working-with-the-container-registry),
[Docker stack deploy](https://docs.docker.com/reference/cli/docker/stack/deploy/),
[Traefik 2.11 ACME](https://doc.traefik.io/traefik/v2.11/https/acme/).
