---
status: proposed
date: 2026-10-28
authored-by: agent
---

# Otimizar o código Web para a primeira abertura

## Context and Problem Statement

Após a correção de nitidez e do indicador de carregamento, o usuário informou
que a imagem ficou melhor no iPhone 13, mas a primeira abertura levou cerca de
20 segundos. As seguintes foram mais rápidas, sem duração informada. Isso
é compatível com reaproveitamento de cache; não separa download, compilação
WebAssembly e preparação da cena no Safari.

O Editor deste worktree usava `CodeOptimization=BuildTimes`. Essa preferência
fica em `Library/EditorUserBuildSettings.asset` e não era fixada pela receita.
O Unity 6000.3 recomenda otimização de tamanho para builds Web mobile, com LTO
como opção de distribuição. Um pacote menor é uma hipótese de melhora da
primeira abertura, não uma garantia de tempo no aparelho.

## Considered Options

- Continuar herdando a preferência local de compilação rápida do Editor.
- Fixar `DiskSizeLTO` e medir o artefato com o mesmo conteúdo e perfil visual.
- Reduzir modelos ou texturas antes de medir o efeito do código compilado.

## Decision Outcome

O experimento local fixa `UnityEditor.WebGL.UserBuildSettings.codeOptimization`
em `DiskSizeLTO` dentro de `ChessCgiWebBuild.Prepare`. A referência à extensão
Web é protegida por `UNITY_WEBGL`, mantendo a compilação do código Editor em
outros targets. O build já exige WebGL ativo antes de chamar `Prepare`.

A versão do Unity, managed stripping, memória, assets, qualidade, resolução e
Stockfish permanecem com os valores da candidata anterior. A cópia anterior
continua disponível para comparação e retorno. A preparação e o teste usam
somente o worktree Web; o checkout original desktop/Quest não é operado.

### Consequences

LTO pode aumentar o tempo de geração e alterar o desempenho do código. A
comparação deve registrar bytes brutos, gzip/Brotli, opções efetivas, integridade
dos assets e uma partida real com IA e retomada antes de substituir a prévia.
Tempo no Chromium do Mac não estabelece tempo nem fluidez no Safari do iPhone.
O aceite desta opção depende desse experimento e do reteste físico.

O primeiro build concluiu com `DiskSizeLTO` registrado e opções efetivas
`-flto -Oz` na compilação e `-Os -g0` na ligação. O Wasm passou de
44.351.063 para 33.856.047 bytes descomprimidos (23,7% menos); em gzip,
de 11.126.101 para 10.072.116 bytes (9,5% menos). Os 3.224 pares de caminho e
tamanho do relatório de assets ficaram iguais. O arquivo de dados ganhou
1.170 bytes em gzip, portanto a redução do download total é bem menor que a
redução do código descomprimido. A duração no aparelho ainda precisa ser medida.

Os 787 arquivos versionados sob `game/` foram conferidos após restaurar as três
serializações incidentais do Editor: somente a mudança intencional da receita
permaneceu. O checkout original manteve seu HEAD e os 252 arquivos existentes.
O pacote será validado e a verificação real registrada também na documentação
do piloto do site, sem apresentar esta compilação como aceite físico.

Evidência local: `.local/first-load-optimization/`. A política de compilação
fica versionada; a preferência anterior do Editor e os arquivos incidentais
ficam copiados em `before/` para restauração com o Editor fechado. Nenhuma
publicação ou alteração da VPS decorre desta decisão.

Referência: [Unity 6000.3 — Web build settings](https://docs.unity.com/en-us/engine/6000.3/manual/platform-specific/webgl/building-distribution/web-build-settings).
