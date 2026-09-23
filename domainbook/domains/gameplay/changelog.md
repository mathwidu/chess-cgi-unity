# Changelog

O que mudou no contexto de gameplay, lançamento mais recente primeiro, no
formato [Keep a Changelog](https://keepachangelog.com/en/1.1.0/): um H2 por
lançamento, escrito como "## [1.2.0] - 2026-06-30" com " [YANKED]" ao final
se o lançamento foi retirado, contendo Added, Changed, Deprecated, Removed,
Fixed ou Security como H3s, cada um deles uma lista de itens.

## [Unreleased]

### Added

- Adversário offline no desktop, com escolha de lado, três dificuldades e validação local de toda candidata.
- Cancelamento e timeout de busca, descarte de respostas antigas e recuperação por nova tentativa ou menu.

### Changed

- Organização da IA para revisão: dificuldade e resultado em arquivos próprios; inicialização UCI, configuração e leitura da resposta em rotinas nomeadas; observação de tarefas tardias usa `Task`. Contratos, perfis e cancelamento preservados.

### Fixed

- Reinício durante a animação não aplica o resultado da partida anterior.
