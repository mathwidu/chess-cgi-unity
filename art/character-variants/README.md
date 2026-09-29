# Personagens, laboratório e tabuleiro

Índice consolidado em 28/09/2026 para a branch `codex/visual-feevale`, criada
sobre `81bea194e6b4a24adfaf3c24b235709660829c8e`. A base já contém IA e VR.
O estado integrado está em `game/`; os diretórios datados abaixo preservam
fontes, referências e evidências das etapas de criação.

## Resultado integrado

| Frente | Documentação e evidências | Fonte editável |
| --- | --- | --- |
| Seis personagens, variantes brancas/pretas, acessórios e marca nas costas | [Produção](production/README.md), [acabamento](finishing-review-20260925/README.md) | [characters.blend](production/characters.blend) |
| Sala real Feevale, mesas, materiais e iluminação | [Sala v3](feevale-room-v3-20260928/README.md), [comparação](feevale-room-v3-20260928/index.html) | [Sala](feevale-room-v3-20260928/lab-source/FeevaleComputerLab.blend) |
| Acabamento do tabuleiro | [Tabuleiro e evidências](tabletop-polish-20260926/README.md) | [Tabuleiro](tabletop-polish-20260926/board-source/ChessBoard.blend) |
| Olhar ao redor no PC | [Regras da interação](../../domainbook/domains/interaction/features/olhar-ao-redor-no-desktop.md) | `game/Assets/Scripts/Controllers/CameraController.cs` |
| Girar, ampliar e reposicionar a peça no painel | [Controles](preview-controls-20260925/README.md), [enquadramento e reposicionamento](preview-pan-20260925/README.md) | `game/Assets/Scripts/UI/SelectedPiecePreviewInput.cs` e `GameHud.cs` |

Para abrir o jogo, revisar os controles e integrar o trabalho com as outras
branches, consulte o [guia da entrega](../../docs/visual-feevale.md).
Os assets exportados e seus `.meta` estão versionados; não é necessário
executar Blender ou reconstruir os prefabs para jogar.

## Histórico e limites

- [Estudo 01](study-01.md) e `review/`: direção rejeitada, preservada para comparação.
- [Direção 02](direction-v2/README.md): referências e conceito aprovado;
  imagens conceituais não são capturas do jogo.
- `classroom-discovery-20260925/`: proposta inicial da sala. O laboratório
  integrado está nas fontes de `feevale-room-v3-20260928/`.
- `feevale-lab-20260925/` e `feevale-room-v2-20260925/`: versões anteriores
  do laboratório, com referências reais e evolução dos materiais.
- `visual-review-20260925/`, `finishing-review-20260925/` e `production/`:
  auditorias e capturas de etapas específicas; as imagens não são atualizadas
  automaticamente quando os assets mudam.
- [match-polish-20260926](match-polish-20260926/README.md): experimento de
  interface e indicadores **revertido**, sem código ativo nesta entrega.

Os arquivos `verification.json` datados registram o resultado da respectiva
rodada. Referências a `.local/` são logs e backups locais, não dependências do
jogo. A [verificação de consolidação](../verification-20260928.json) registra
os testes executados para esta branch. Headset físico, conforto e desempenho
no dispositivo continuam pendentes.

## Autoria e reprodução

- `build_characters.py` e `uv_layout.py`: exportações de personagens em
  `game/Assets/Art/Characters/Direction02/`; auditorias `audit_*.py`.
- `build_feevale_lab.py`: exportação da sala em `game/Assets/Art/Environment/`
  e fonte em `feevale-room-v3-20260928/lab-source/`.
- `build_chess_board.py`: tabuleiro no mesmo diretório de assets, com fonte
  em `tabletop-polish-20260926/board-source/`.
- `generate_study.py` e `build_classroom_study.py`: geradores históricos,
  sem necessidade de execução para usar o resultado aprovado.
- `fonts/`: fonte dos símbolos clássicos e sua licença.
- `direction-v2/brand/`: marca Feevale e registro de procedência.

Regerar sobrescreve os arquivos de saída; preserve ajustes manuais antes de
executar os geradores. Os procedimentos por etapa estão nos READMEs acima.
