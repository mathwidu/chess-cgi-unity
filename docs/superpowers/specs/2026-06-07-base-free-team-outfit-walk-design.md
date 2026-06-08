# Base-Free Team Outfit Walk Design

## Goal

Move the chess game away from "custom character glued to a chess base" and toward a premium stylized character chess game where identity comes from character design, team outfit colors, and piece-specific motion.

## Approved Direction

All custom pieces should stop using the visible `TeamBase` pedestal. White and black teams should be readable through character outfit colors, not through bases. White pieces use the same character silhouette with light/white clothing. Black pieces use the same silhouette with black/dark clothing. Face, hair, skin, glasses, shoes, and signature accessories stay recognizable.

The immediate playable slice is:

- remove generated bases from custom pieces;
- add a reusable team outfit contract for semantic outfit materials;
- polish pawn walking so it feels less like sliding and more like a small character stepping across the board;
- document the asset rules so future Blender/Unity/Codex character generation produces semantic, recolorable, riggable characters.

## Scope For This Slice

This slice does not try to produce final cinematic captures for every piece. Capture ideas remain in the roadmap, but the next implementation should make movement quality credible first. The pawn is the vertical slice because it is the most common piece and already has the best rig candidate.

## Architecture

Chess rules remain isolated in `ChessRulesAdapter`. Visual polish remains in the presentation/runtime layer:

- `PieceFactory` creates visual pieces and no longer adds a visible custom pedestal.
- `TeamOutfitApplier` applies white/black team colors to semantic outfit materials or semantic outfit renderers first. If a generated prefab has no separable outfit material, it may apply a temporary non-geometric material tint for team readability.
- `PieceMotionSettings` carries richer walk tuning values.
- `PieceView` evaluates a smoother walk pose with body bob and sway.
- `ModularCharacterRig` remains the procedural rig bridge for generated characters and gets smoother walk timing.
- The Blender character-generation docs require semantic material names for future assets.

## Team Outfit Contract

Runtime recoloring should prefer opt-in semantic materials. Current generated GLBs can have skin, hair, clothes, and face baked into one material, so tinting them is not a final-quality asset solution. However, for playability, single-material prefabs may receive a temporary whole-material side tint as long as the code creates no runtime geometry and keeps the Blender/material-separation path documented as the professional fix.

Recognized semantic names:

- `TeamOutfitPrimary`
- `TeamOutfitSecondary`
- `TeamClothes`
- `TeamUniform`
- renderer names containing `TeamOutfit`

Non-outfit materials are left untouched when semantic outfit materials exist. If no semantic material exists, the fallback tint may affect the whole single-material generated texture; that is acceptable only as a temporary readability bridge, not as the final character pipeline.

## Movement Design

The current movement has a useful foundation but reads as slide-plus-bob. The improved pawn movement should:

- start and stop more gradually;
- take more than one step over a square;
- reduce the big vertical hop;
- add small side sway and torso motion;
- keep feet moving through the rig instead of relying only on root motion;
- reset cleanly at the destination.

The first version is still procedural and not AAA animation. It is a professional-friendly bridge: good enough for a coursework game, testable, and ready to be replaced later by authored animation clips.

## Future Piece Motion Profiles

After the pawn movement is acceptable, each piece gets its own motion profile:

- Pawn: short grounded walk.
- Bishop: elegant diagonal glide or ritual stride.
- Rook: heavy hop or block-like thump.
- Knight: arcing L-shaped jump with horse-like landing.
- Queen: confident smooth walk.
- King: shorter authoritative steps.

## Future Capture Animation Roadmap

Capture animations remain a separate phase after movement polish:

- Pawn uses a small dagger strike.
- Knight jumps and neighs before impact.
- Rook drops or slams onto the captured piece.
- Bishop uses a prayer or laser-like diagonal strike.
- Queen uses a sword strike that splits the target.
- King uses an open-hand shove or commanding hit.

Each capture should be built as a visual layer above the legal move system. The rules engine only decides that a capture happened; animation code decides how it looks.

## Acceptance Criteria

- Custom pieces spawned by `PieceFactory` do not contain `TeamBase`.
- Classic fallback pieces still render normally when no custom prefab exists.
- White and black custom pieces can receive different team outfit colors through semantic outfit materials or a temporary non-geometric fallback tint.
- Existing generated assets with non-semantic materials are not destructively recolored; any fallback tint is applied on duplicated runtime materials.
- Pawn walk settings are slower and more grounded than the previous slide-hop.
- Runtime-created pawn visuals still receive `CharacterAnimationDriver`, `ModularCharacterRig`, and `CharacterVisualContract`.
- The design is documented clearly enough to guide future Codex plus Blender character generation.

## Testing

Automated tests should cover:

- custom pieces no longer create `TeamBase`;
- custom pieces still expose `CustomVisual`;
- semantic outfit materials are recolored for white and black sides;
- single-material generated prefabs can receive a readable fallback tint without runtime geometry;
- walk settings have slower readable duration and lower hop;
- walk pose keeps exact start and destination positions;
- runtime custom visual still receives animation extension components.

Manual Unity validation should cover:

- open `Assets/Scenes/Main.unity`;
- press Play;
- confirm custom characters stand directly on the board without visible bases;
- move a pawn and inspect whether the walk looks less rushed and less floaty;
- select white and black pieces and verify the team read is still understandable.
