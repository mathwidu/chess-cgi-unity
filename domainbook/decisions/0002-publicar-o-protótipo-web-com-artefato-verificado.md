---
status: proposed
date: 2026-10-23
authored-by: agent
---

# Publicar o protótipo Web com artefato verificado

## Context and Problem Statement

O usuário escolheu `chess.matheusduarte.dev.br` e solicitou a publicação na VPS
pessoal, cuidando apenas do DNS. O protótipo permanece em branch/worktree
separados, preservando o jogo desktop/Quest e suas alterações existentes.

## Considered Options

- Copiar arquivos soltos diretamente para um servidor HTTP.
- Recompilar Unity em cada execução do CI, com configuração/licenciamento próprios.
- Verificar o build local, publicar imagem imutável e usar a infraestrutura da VPS.

## Decision Outcome

Receita preparada localmente; publicação depende de aprovação explícita.

O build Unity testado localmente é empacotado com hashes e receitas fixas.
GitHub Actions verifica esse pacote, exercita a IA e o servidor HTTP e publica
uma imagem amd64 no GHCR. O Mac aplica o digest exato por SSH no serviço novo
`chess-web_web`, sem levar a chave pessoal para o CI. A etapa inicial inclui
somente o workflow manual no `main`; o código do protótipo não é mesclado.

O NGINX entrega arquivos gzip com os tipos exigidos pelo runtime e revalidação
de cache, pois os nomes Unity são estáveis. Licenças e fonte do Stockfish
acompanham o mesmo artefato. O jogo e a IA executam no dispositivo do visitante.

Um ativador limitado a 72 horas espera o registro A criado pelo usuário antes
de solicitar certificado pela infraestrutura Traefik existente. Locks de
release protegem a decisão de saúde/retorno e a ativação do certificado.

### Consequences

O hash do pacote autentica bytes; o vínculo entre build Unity e código desta
primeira release é atestado pela sessão local, pois o compilador não produziu
um carimbo de commit. CI verde e saúde técnica não representam aceite em
Android/iPhone físicos. Receita, limites, verificação e retorno estão em
`docs/web-deployment.md`.
