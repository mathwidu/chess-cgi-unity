---
status: accepted
date: 2026-05-30
---

# Custom piece models with a primitive fallback

## Context and Problem Statement

The pieces are custom characters modelled on the class, one per kind, and part of
the point of the project is to show them. But a custom model can be missing,
unassigned, or still being made, and a board with a hole where a piece should be
is both wrong to play on and embarrassing to demo. The game needs to show the
custom characters when they exist and stay playable when they do not.

## Decision Drivers

- The custom characters are a headline of the project and should be shown.
- The board must always have a full, recognisable set of pieces to play.
- Work in progress on one model should not block running the whole game.

## Considered Options

- Require every custom model before the game can run.
- Show only the custom models, and leave a kind blank when its model is missing.
- Show the custom model per kind, and build a piece from primitive shapes when
  no model is set.

## Decision Outcome

Chosen option: "custom model per kind, with a primitive fallback". `PieceFactory`
uses the assigned prefab for a kind and scales it to fit; when a kind has no
prefab, it assembles a piece from primitive shapes — a base, a stem, and a head
that varies by kind — in the side's colour. Every kind therefore renders,
whatever is assigned.

### Consequences

- Good, because the custom characters show whenever they are ready, and the
  board is complete even when they are not.
- Good, because a model can be added or swapped one kind at a time without
  breaking the game.
- Bad, because two rendering paths exist for pieces and both have to keep working.
- Bad, because the fallback shapes are plain, so a board mixing custom models and
  fallbacks looks uneven until every model is in.
