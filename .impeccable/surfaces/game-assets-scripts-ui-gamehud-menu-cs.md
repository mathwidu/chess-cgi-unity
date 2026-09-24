---
version: 1
slug: "game-assets-scripts-ui-gamehud-menu-cs"
primary_target: "game/Assets/Scripts/UI/GameHud.Menu.cs"
related_targets: ["game/Assets/Scripts/UI/GameHud.Menu.Layout.cs", "game/Assets/Scripts/UI/GameHud.Match.cs", "game/Assets/Scripts/UI/MenuCastPreview.cs"]
---

# Menu inicial do Xadrez CGI

A composição vigente é **Mesa de partida**, escolhida por imagem em 22/09/2026.
O contrato único está em `../menu-contract.md`; a referência aprovada, em
`../mocks/approved/mesa-de-partida.png`. `DESIGN.md` e `../design.json` registram
as medidas e estados da implementação nativa Unity.

`GameHud.Menu.Layout.cs` constrói os elementos. `GameHud.Menu.cs` mantém escolhas,
foco e ajuste ao Canvas; `GameHud.Match.cs` constrói o HUD e os diálogos;
`MenuCastPreview.cs` isola os professores 3D da cena de partida.

A organização do código não autoriza redesenho. Preservar a composição aprovada,
a marca original centralizada, os modelos reais e a integração uGUI/world-space.
