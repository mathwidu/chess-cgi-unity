# Side-Specific Character Combat Design

## Goal

Replace generic runtime team tinting with real white-side and black-side character variants, generated and polished in Blender, while preparing every custom chess character for future movement and capture animations.

## Approved Direction

Each custom chess character must have an artistic white version and an artistic black version. The two versions should keep the same identity, face, silhouette, and signature traits, but use side-specific wardrobe and prop design:

- white side: light, white, cream, or classroom-clean outfit language;
- black side: black, charcoal, dark blue, or villain-side outfit language;
- no visible team pedestal/base on human characters;
- no runtime geometry panels to fake uniforms;
- runtime color tint is allowed only as a temporary fallback when a side-specific prefab is missing.

The user explicitly rejected the final direction of "just tinting the same model". The final route is Blender-authored variants.

## Character Set

- Pawn: Mathwidu, student, ginger/red curly hair, future dagger strike.
- Bishop: Rafael, student, future prayer or diagonal laser strike.
- Rook: Alex, student, seated on or integrated with a small tower, future tower slam.
- Knight: Gustavo, student, seated on or integrated with a small horse, future horse jump and neigh.
- Queen: Marta, professor, scarf/teacher identity, future sword or energy strike.
- King: Ricardo Carioca, professor, blue Feevale reference, future open-hand command hit.

## Unity Architecture

`PieceFactory` becomes responsible for selecting the best custom prefab for a piece and side:

1. Try a side-specific custom prefab for `(kind, side)`.
2. If missing, use the existing generic custom prefab for `kind`.
3. If no custom prefab exists, use the classic primitive fallback.

Side-specific prefabs are considered final art assets and should not receive fallback tinting. Generic custom prefabs may still receive semantic outfit recoloring or temporary non-geometric fallback tinting until their Blender variants exist.

## Blender Asset Contract

Each Blender-generated character variant should export with:

- full-body character mesh standing upright and facing forward;
- readable shoes/feet for walk animation;
- no chess base/pedestal for humanoid characters;
- semantic material names when separable: `Skin`, `Hair`, `Eyes`, `Glasses`, `TeamOutfitPrimary`, `TeamOutfitSecondary`, `Shoes`, `Accessory`;
- optional prop placeholders for future combat: `WeaponSocket`, `RightHandSocket`, `LeftHandSocket`, `CastSocket`;
- centered origin at ground contact;
- consistent scale so Unity can fit the visual to the board square.

For pieces with integrated props, the prop can remain part of the model if it is part of the character concept:

- Alex may sit on a small tower;
- Gustavo may sit on a small horse;
- Marta may carry or imply queen authority through scarf/crown-like accent;
- Ricardo may carry king authority through posture or subtle crown/command accent.

## Animation Preparation

The current movement remains procedural, but the asset contract must prepare for authored animation clips later. Every runtime custom visual should expose stable sockets:

- `EffectsSocket`: visual particles and magic/projectile effects.
- `HitSocket`: targetable point for impact reactions.
- `GroundSocket`: ground contact and landing effects.
- `WeaponSocket`: where a future weapon/prop can attach.
- `RightHandSocket`: right-hand attack/hold anchor.
- `LeftHandSocket`: left-hand attack/hold anchor.
- `CastSocket`: spell/prayer/laser origin for bishop/queen effects.

The future capture animation layer should use the legal move result from chess rules but remain visual-only. Rules decide whether capture is legal; view code decides how it looks.

## Capture Concepts

- Pawn: walks in, draws or already holds a small dagger, short strike.
- Bishop: pauses, raises hand or staff, fires prayer/laser-like diagonal effect.
- Rook: tower/body heavy hop or slam onto target.
- Knight: horse-like L jump, neigh/readable anticipation, landing impact.
- Queen: elegant sword or energy slash splitting the captured piece visually.
- King: authoritative open-hand shove or command hit.

## Acceptance Criteria

- `PieceFactory` supports side-specific custom prefab selection without breaking existing generic prefab fallback.
- Side-specific prefabs do not get runtime fallback tint applied by default.
- Existing generic prefabs still work and can use temporary tint only as fallback.
- `CharacterVisualContract` creates all combat-preparation sockets.
- Tests prove white and black prefabs can differ by actual prefab identity.
- Tests prove the generic fallback still works when side-specific assets are missing.
- Tests prove side-specific visuals keep stable combat sockets for future animations.
- Documentation clearly states the professional Blender route and the temporary nature of runtime tinting.

## Out Of Scope For This Slice

- Final high-quality Blender regeneration for all twelve side variants.
- Authored humanoid animation clips.
- Full capture choreography and destruction effects.
- Online play or AI.

The first implementation slice is the Unity-side architecture and docs. After that, Blender can generate/import the actual `White` and `Black` variants one character at a time.
