# Quest 3: integração e recuperação dos fontes

Em 28/10/2026, o trabalho do Quest foi organizado na branch
`codex/quest3-standalone-offline-ai`, preservando os checkpoints aprovados e
integrando os commits remotos `f2e59dc` e `1b1528a`.

## Versões preservadas

| Estado | Referência Git | Identificação no menu | Perfil |
| --- | --- | --- | --- |
| Estável aprovada para exposição | `quest3-stable-v0.1.3-20261026` → `9243e9cdf12c856e24ac9b074139360ad7ef6c8d` | v0.1.3 (4), 26/10/2026 17:39:37 | Cenário 80%, MSAA 4×, foveation 0,5 |
| Perfil de trabalho aprovado posteriormente | `quest3-working-65pct-20261026` → `b89f42d07a8c8882435e4a29168cdbc98a3cdf7e` | v0.1.3 (4), 26/10/2026 19:44:54 | Cenário 65%, MSAA 4×, foveation 0,5 |
| Candidato mais recente de interface | `53187f9` e a integração subsequente na branch Quest | v0.1.4-ui.2 (4), 26/10/2026 21:02:28 | Cenário 65%, HUD independente de 2048 × 1152 |

Os horários são de Brasília (UTC-3). A estável de 80% foi aceita pelo usuário,
com mediana medida de 65,5 FPS, abaixo da meta de 72 Hz. O usuário também
aprovou fluidez e imagem do perfil de 65%; suas coletas interrompidas não
comprovam FPS sustentado. A aceitação física de `ui.2` permanece pendente:
o primeiro candidato de interface, `ui.1`, havia apresentado cintilação.

## Procedência dos fontes

O commit da estável foi reconstruído sobre `30bcbfe` usando o arquivo
`Builds/Quest3/stable-20261026-v0.1.3/source-changes.tar.gz`. Foram conferidos
294 hashes do checkpoint, incluindo fontes, geometria e todos os arquivos
arquivados. SHA-256 do arquivo de fontes:

`ad06f4efe1d7e6fc152bdd2bd2d000a075d6eebb4394fcaf89fae2e1cc58d4b3`

O commit de 65% aplica somente o `source-overlay.tar.gz` arquivado na pasta
`Builds/Quest3/comparison-20261026-scale065/`: configuração do pipeline Mobile
e identificação de build. Os fontes C# e a geometria são os mesmos da estável.
SHA-256 desse arquivo de alterações:

`c7e753b03cce3591e62840bfd9aae8c9b33aee78b47aee3eb2e207bf7b3d38e3`

A recuperação criou commits novos em 28/10, sem substituir os arquivos de
trabalho nem recompilar os APKs aprovados. O JSON de identificação de build
foi mantido para identificar cada snapshot; o callback de build o regenera
antes de empacotar um novo Player.

## Integração dos commits remotos

- `f2e59dc`: inicialização sem raios de controles ou mãos não rastreados.
  Foi preservado o `Camera Offset` inativo até todas as referências estarem
  configuradas, incluindo os visuais de mãos do Quest. O teste remoto foi
  incorporado integralmente.
- `1b1528a`: retorno da peça grava posição e rotação no `Rigidbody`, além do
  transform. Essa correção já estava nos fontes locais. O teste remoto foi
  adaptado ao contrato atual: a peça volta em repouso dinâmico, disponível
  para novo contato físico, sem registrar uma jogada.

Os dois commits mantêm sua autoria e histórico no merge. As mudanças de
runtime locais foram preservadas, incluindo apoio na palma, proteção das
duas mãos durante uma pega, destino de colocação, hastes de ajuste, IA
offline, geometrias e captura de HUD. O estudo independente
`game/Assets/AnimationStudies/A003/` permaneceu local, fora destes commits.

## Recuperação

Para abrir os fontes da versão aceita em outro diretório, sem trocar a
versão de trabalho atual:

```bash
git worktree add --detach ../chess-cgi-quest3-stable quest3-stable-v0.1.3-20261026
```

Para o perfil aprovado de 65%, use a tag
`quest3-working-65pct-20261026` em outro diretório livre. O projeto Unity fica
na subpasta `game` e usa o Editor 6000.3.16f1. Uma nova compilação terá outra
data de build; para reinstalar exatamente o binário aceito, use o APK
arquivado, conforme o [guia de entrega](quest3-entrega-20261026.md).

Os APKs continuam locais em `Builds/Quest3/`, fora do Git:

| APK | SHA-256 |
| --- | --- |
| `stable-20261026-v0.1.3/XadrezCGI-Quest3-stable.apk` | `541bf61c8f198f504778035a0d0c4b62be4690709c6030a387bace9aed08532e` |
| `comparison-20261026-scale065/XadrezCGI-Quest3-scale065.apk` | `4d96b00eef2223983f2ce470c5b3a93fa87321c29b926b22f06194559961ab95` |
| `ui-candidate-20261026-v0.1.4-ui.2/XadrezCGI-Quest3-ui-candidate.apk` | `cd2c1279b7129e9b9aaaec31a7f9e5bda647c28213f37c90d9928b705fec0794` |

As três cópias foram conferidas antes e depois da integração. Há também uma
cópia local do estado anterior, com patch, arquivos, hashes e referências
Git, em `.local/quest3/git-sync-20261028/`.

## Verificação da integração

- Unity 6000.3.16f1: **286/286 PlayMode** e **70/70 EditMode** aprovados,
  sem falhas nem testes ignorados. Os dois testes remotos passaram na suíte
  integrada. Os hashes dos fontes C# permaneceram iguais durante cada execução.
- Revisão independente dos conflitos: inicialização da modalidade e retorno
  físico preservados; nenhuma mudança de runtime necessária na integração.
- Domainbook validado e cobertura documental dos domínios conferida. A limpeza
  de espaços finais em arquivos serializados Unity não alterou GUIDs ou
  valores dos assets. As tags mantêm os bytes dos checkpoints originais.
- Relatórios XML/JSON e logs locais em
  `.local/quest3/git-sync-20261028/integrated-playmode.*` e
  `.local/quest3/git-sync-20261028/integrated-editmode.*`.

Esta integração não gerou nem instalou APK. Os testes de Editor verificam
o comportamento simulado; nitidez, cintilação, interação e FPS do candidato
`ui.2` ainda precisam de aceitação física no Quest.
