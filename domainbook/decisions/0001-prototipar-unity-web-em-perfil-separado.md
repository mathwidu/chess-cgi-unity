---
status: accepted
date: 2026-10-23
authored-by: agent
---

# Prototipar Unity Web em perfil separado

## Context and Problem Statement

O objetivo é levar a partida com peças personalizadas e adversário controlado
pelo computador ao navegador, inclusive com toque, preservando desktop e Quest.
Reaproveitamos as regras e a fotografia da posição. O transporte de IA web,
layout responsivo e renderização recebem adaptações específicas da plataforma.

## Considered Options

- Compartilhar a configuração de build com desktop e Quest.
- Isolar o protótipo Unity Web em outro worktree e usar um perfil por plataforma.
- Recriar o jogo em outro runtime web antes de medir o protótipo Unity.

## Decision Outcome

O protótipo parte de `30bcbfe` em outro worktree. Os assets fonte ficam intactos;
o build aplica overrides WebGL de textura, pipeline próprio e orçamento de
pixels. A partida começa com peças personalizadas; peças clássicas continuam
como opção explícita, em preferência separada no navegador.

Unity Web é a primeira hipótese, não uma decisão de manter o runtime a qualquer
custo. O critério é uma partida real com IA e toque em aparelhos modestos,
medindo bytes, tempo até jogar, memória e fluidez. Os resultados e limitações
ficam em `docs/web-prototype.md`. Não há publicação de serviço nem alteração
da versão corrente do jogo nesta etapa.

### Consequences

O pacote inicial medido passou de 145,94 MB para 36,67 MB após remover legados
do build e comprimir as texturas extraídas do ambiente; a IA adiciona 7,32 MB
quando acionada. O build prepara essas cópias temporariamente, mantém journal e
backups recuperáveis, e restaura os fontes ao terminar. O perfil nativo e os
assets protegidos tiveram restauração conferida por hash. O orçamento de 15 MB
continua aberto; ainda não há aceite para aparelhos modestos.

Partida com IA, toque e layouts retrato/paisagem foram exercitados no Mac,
incluindo o ramo de runtime mobile via user agent Android simulado. A emulação
confirma o comportamento observado, sem estabelecer fluidez, memória ou
temperatura em Android e iPhone físicos.
