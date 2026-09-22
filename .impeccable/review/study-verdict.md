# Mesa de partida — entrega nativa Unity

Referência: `../mocks/approved/mesa-de-partida.png`, escolhida pelo usuário por imagem. Implementação em `GameHud.Menu.cs`, com cenário separado, controles uGUI e professores 3D reais. Sem build de player.

## Verificações

- PlayMode desktop: 13/13; integração VR: 13/13; nenhum ignorado. XML `MenuStudyPlayMode.xml` em TestResults de cada checkout.
- GUI: aberto o checkout desktop correto, comando Chess CGI → Jogar no Editor executado; alternância Brancas/Iniciante para Pretas/Difícil confirmada visualmente na aba Game. Editor deixado em Play no menu.
- Captura nativa: 14 estados/tamanhos da cena Main; arquivos regeneráveis em `.impeccable/review/`, via `MenuReviewCapture.Run`.
- XR HUD: raio do controle simulado atinge Jogar, clica e fecha o menu; exit 0/PASSED. Erros preexistentes do runtime ausente, visual do raio e indexador registrados em `docs/ai-desktop.md`. Hardware pendente.
- Finish review independente: F1 contraste, F2 Intermediário no compacto e F3 foco de Jogar resolvidos. Ver `study-review.md`.
- Domainbook válido; diff sem whitespace inválido; índice Graphify atualizado (código/links; enriquecimento semântico pendente).

## Fidelidade e processo

Spec de 12 regiões, duas imagens e três regiões tipográficas medidas. Os gates de spec, plates e hero passaram; comparador final `match`, 0.8854. Esse número não certifica qualidade nem identidade pixel a pixel. Diferenças deliberadas: prefabs reais, marca original, Lato incorporada, descrição por dificuldade e foco acessível.

O engine local do Impeccable exige `mobile.png` de 390px no gate responsive. O alvo autorizado é Unity desktop/VR; não existe página mobile nesta entrega. O gate web permanece aberto, sem captura fictícia e sem forçar o encerramento. A cobertura nativa foi concluída em cinco dimensões e Canvas world-space. O próprio SKILL orienta verificar as classes de dispositivo entregues em plataformas nativas.

Revisão e documentação executadas por subagentes novos com os roteiros locais de substituição do Impeccable (`reference/degraded/finish-reviewer.md` e `documenter.md`), não por agentes nomeados indisponíveis. O revisor emitiu `ship` para os três achados; DESIGN.md e design.json registram a implementação final, sem canonizar pendências de hardware.
