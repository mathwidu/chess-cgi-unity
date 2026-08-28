---
id: chess-cgi-unity
milestones:
  - { id: playable-delivery, name: Playable custom chess delivery, status: done }
  - { id: vr-conversion, name: VR conversion for HTC Vive and Meta Quest 3, status: planned }
  - { id: adversario-computador, name: Adversário controlado pelo computador do desktop ao Quest standalone, status: planned }
  
---

# chess-cgi-unity roadmap

## Milestones

### Playable custom chess delivery

The delivered coursework build: a local two-player 3D chess in Unity 6.3 with
full rules, a board and HUD built in code at runtime, a custom character model
for every piece kind, mouse-and-keyboard control, and a camera that turns to
the player whose move it is.

### VR conversion for HTC Vive and Meta Quest 3

Bring the game to virtual reality: the player wears a headset and picks pieces
with motion controllers instead of a monitor, mouse, and keyboard, targeting HTC
Vive (PC-tethered) and Meta Quest 3 (standalone). The first step is a feasibility
study and a high-level conversion plan — see the interaction feature
[Play in VR](domains/interaction/features/play-in-vr.md). Building it comes after
the plan is reviewed and the XR framework choice is settled in a decision record.

### Adversário controlado pelo computador do desktop ao Quest standalone

Adicionar um adversário offline com níveis de dificuldade nomeados. O primeiro
passo comprova o contrato comum de gameplay e a integração UCI com Stockfish no
Editor e em uma build macOS. Depois, o mesmo contrato segue para Windows PC-VR e
Quest Link e, por fim, para uma build Android ARM64 executada sem PC no Meta
Quest 3. A integração standalone só é escolhida depois de um experimento no
hardware — consulte a funcionalidade de gameplay
[Jogar contra um adversário controlado pelo computador](domains/gameplay/features/jogar-contra-computador.md).
