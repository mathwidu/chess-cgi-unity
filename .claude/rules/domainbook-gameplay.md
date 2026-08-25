---
paths:
  - "game/Assets/Scripts/Rules/**"
  - "game/Assets/Scripts/Domain/**"
  - "game/Assets/Scripts/Controllers/ChessGameController.cs"
---

# Gameplay

Code here is claimed by the gameplay domain. Changing it means updating `domainbook/domains/gameplay/` in the same commit, or waiving the commit with a "Skip-Docs: <reason>" trailer. Any file under that folder clears the check: the canvas, the glossary, the changelog, a feature, a decision, or a debt record.

Before you name anything here, call `explain_terms` with the words you are about to use — this context has its own, and they win over the book's. `domainbook/domains/gameplay/index.md` holds its canvas. Without MCP, its words are in `domainbook/domains/gameplay/glossary.md`.
