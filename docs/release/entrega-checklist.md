# Checklist de entrega

Data alvo: 11/06/2026.

## Itens obrigatorios

- [x] Codigo-fonte completo no repositorio Git.
- [x] Projeto Unity separado em `game/`.
- [x] Cena principal em `game/Assets/Scenes/Main.unity`.
- [x] Instrucoes de abertura no `README.md`.
- [x] Relatorio tecnico em `docs/report/relatorio-tecnico.md`.
- [x] PDF do relatorio em `docs/report/relatorio-tecnico.pdf`.
- [x] Roteiro para video demonstrativo em `docs/video/roteiro-video.md`.
- [ ] Video demonstrativo final de ate 3 minutos gravado e anexado/linkado.

## Validacao recomendada antes de entregar

- [ ] Abrir o projeto pela pasta `game` no Unity Hub.
- [ ] Abrir `Assets/Scenes/Main.unity`.
- [ ] Clicar em `Play` e iniciar uma partida.
- [ ] Selecionar uma peca e conferir aba lateral com preview 3D.
- [ ] Fazer uma jogada das brancas e conferir virada de camera para as pretas.
- [ ] Fazer uma jogada das pretas e conferir historico.
- [x] Rodar EditMode tests pela Unity via MCP.
- [x] Confirmar `35` testes passando.

Ultima validacao automatizada: `35` testes EditMode passando em 11/06/2026.

## Observacoes para o professor

- O repositorio nao versiona `Library/`, `Builds/`, caches locais, referencias privadas ou fotos pessoais.
- A build macOS pode ser gerada pelo menu `Chess CGI > Build > macOS`.
- A entrega por Git deve usar a branch `main` ou `stable/entrega-v1-estavel`, conforme definido no momento final da publicacao.
