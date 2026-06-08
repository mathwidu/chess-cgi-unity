# Overnight Character Polish Report

Date: 2026-06-07

## Summary

This pass moved the current stable delivery toward the planned professional character pipeline without spending paid credits. The stable fallback tag `entrega-v1-estavel` remains untouched, while the active branch keeps the new polish work.

Implemented:

- automated custom piece audit for the six active custom prefabs;
- Unity visual contract checks for custom pieces;
- semantic team outfit recoloring without runtime uniform geometry;
- base-free custom character instantiation with invisible support/click collider;
- per-piece movement styles for pawn, rook, knight, bishop, queen and king;
- selected-piece sidebar with larger preview and profile metadata;
- capture style contracts for future rich attack clips.

## Changed Files

Main areas touched:

- `game/Assets/Scripts/View/`
- `game/Assets/Scripts/UI/`
- `game/Assets/Scripts/Domain/`
- `game/Assets/Tests/EditMode/`
- `tools/character_pipeline/`
- `docs/design/`

The working tree was already dirty before this run. This pass did not clean or revert unrelated files.

## Validation

Passed:

- `git rev-parse entrega-v1-estavel`
- `git diff --check`
- `python3 -m unittest tools.blender.tests.test_mathwidu_v3b_candidate -v`
- `python3 -m unittest tools.character_pipeline.tests.test_audit_custom_pieces -v`
- Unity MCP compile/refresh command
- Unity MCP focused EditMode method assertions for:
  - `CustomPieceCoverageTests`
  - `CustomPieceVisualContractTests`
  - `TeamOutfitApplierTests`
  - `PieceMovementStyleLibraryTests`
  - `PieceViewTests`
  - `SelectedPiecePreviewControllerTests`
  - `CharacterProfileCatalogTests`
  - `GameHudTests`
  - `CaptureAnimationLibraryTests`
  - `CaptureAnimationStyleLibraryTests`
- Unity console check: 0 errors, 0 warnings.

## Manual QA Needed

Open `Assets/Scenes/Main.unity`, press Play, and check:

1. Select each piece kind on the board.
2. Confirm the sidebar shows the full model, not cropped at the head or feet.
3. Move pawn, rook, knight, bishop, queen and king at least once.
4. Confirm movement styles read differently.
5. Make one capture if possible and confirm the attacker ends exactly on the destination square.
6. Confirm black/white team clothing or accent remains readable after turn camera rotation.

## Stop Rules Triggered

None.

No Unity AI, Tripo, Meshy or other paid generation was used in this pass.

## Next Recommended Phase

The next professional step is a true base-free rig vertical slice:

1. Pick `Pawn_Mathwidu_v3b` as the first target.
2. Remove visible pedestal dependency completely.
3. Keep feet and shoes visible.
4. Create or repair an animatable rig in Blender.
5. Import as a new prefab, not overwriting the approved current prefab.
6. Connect real `Idle`, `Walk`, `Hit` and `Capture` clips through `CharacterAnimationDriver`.
7. Keep procedural motion as fallback for all other pieces.

## Follow-up Visual Fixes

After manual QA, this pass was refined with:

- slower movement style durations to make walk/hop arcs easier to read;
- non-magenta capture impact colors;
- URP-compatible runtime material creation for generated visual effects;
- runtime uniform panels were reverted after Play Mode QA because they created rectangular artifacts;
- single-material custom prefabs now receive only a non-geometric side-readability material tint;
- black custom pieces now keep neutral child visual rotation and rotate the root, so movement faces the travel direction;
- Bishop pedestal removal stays documented as an asset-pipeline task instead of a runtime mesh sanitizer.
