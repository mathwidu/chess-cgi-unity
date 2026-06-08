# Side-Specific Character Combat Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add the Unity and Blender-facing structure needed for real white/black character variants and future capture animations.

**Architecture:** `PieceFactory` resolves side-specific prefabs first, generic prefabs second, and classic primitive pieces last. `CharacterVisualContract` owns stable sockets for future weapon, cast, and hit animation effects. Runtime tint remains a temporary fallback only for generic assets that do not yet have side-specific Blender variants.

**Tech Stack:** Unity 6.3 LTS, C#, NUnit EditMode tests, Blender Python pipeline documentation.

---

## Files

- Modify: `game/Assets/Scripts/View/PieceFactory.cs`
- Modify: `game/Assets/Scripts/View/CharacterVisualContract.cs`
- Modify: `game/Assets/Tests/EditMode/PieceFactoryTests.cs`
- Modify: `game/Assets/Tests/EditMode/CharacterVisualContractTests.cs`
- Modify: `docs/design/custom-piece-generation-workflow.md`
- Modify: `docs/design/capture-animation-roadmap.md`
- Create: `docs/superpowers/specs/2026-06-07-side-specific-character-combat-design.md`
- Create: `docs/superpowers/plans/2026-06-07-side-specific-character-combat.md`

## Task 1: Side-Specific Prefab Resolution

**Files:**
- Modify: `game/Assets/Scripts/View/PieceFactory.cs`
- Test: `game/Assets/Tests/EditMode/PieceFactoryTests.cs`

- [ ] **Step 1: Write failing test for side-specific prefab selection**

Add a test that creates a white cube prefab and a black sphere prefab for pawns, configures both, creates one white pawn and one black pawn, and asserts each `CustomVisual` contains the expected renderer object.

- [ ] **Step 2: Run Unity EditMode test and verify RED**

Run the focused `PieceFactoryTests` through Unity Test Runner or Unity MCP. Expected result: compile/test failure because `ConfigureCustomPrefab(ChessPieceKind, ChessSide, GameObject)` does not exist yet.

- [ ] **Step 3: Add serialized side-specific prefab fields**

Add private serialized fields to `PieceFactory` for `whitePawnPrefab`, `blackPawnPrefab`, `whiteRookPrefab`, `blackRookPrefab`, `whiteKnightPrefab`, `blackKnightPrefab`, `whiteBishopPrefab`, `blackBishopPrefab`, `whiteQueenPrefab`, `blackQueenPrefab`, `whiteKingPrefab`, and `blackKingPrefab`.

- [ ] **Step 4: Add side-specific configure method**

Implement `public void ConfigureCustomPrefab(ChessPieceKind kind, ChessSide side, GameObject prefab)` and route each kind/side to the matching field.

- [ ] **Step 5: Change prefab lookup**

Replace `GetCustomPrefab(ChessPieceKind kind)` with `GetCustomPrefab(ChessPieceKind kind, ChessSide side, out bool isSideSpecific)`. The method returns side-specific first; if missing, returns the existing generic field and `isSideSpecific = false`.

- [ ] **Step 6: Run focused tests and verify GREEN**

Run `PieceFactoryTests`. Expected result: side-specific test passes and existing generic fallback tests still pass.

## Task 2: Prevent Runtime Tint On Final Side Variants

**Files:**
- Modify: `game/Assets/Scripts/View/PieceFactory.cs`
- Test: `game/Assets/Tests/EditMode/PieceFactoryTests.cs`

- [ ] **Step 1: Write failing test for no fallback tint on side-specific prefab**

Create a side-specific prefab with a non-semantic material named `SingleNeutralMaterial`, create a black pawn, and assert the material name does not contain `ReadableTint`.

- [ ] **Step 2: Verify RED**

Run the focused test. Expected result: failure because `BuildCustomShape` currently always calls `TeamOutfitApplier.ApplyToOrCreateAccent`.

- [ ] **Step 3: Skip fallback tint for side-specific prefabs**

In `BuildCustomShape`, call `TeamOutfitApplier.ApplyTo(visual.transform, side)` for side-specific prefabs. For generic prefabs, keep `ApplyToOrCreateAccent`.

- [ ] **Step 4: Verify GREEN**

Run `PieceFactoryTests`. Expected result: side-specific prefab keeps its authored material, generic prefabs still receive fallback readability tint.

## Task 3: Combat-Ready Visual Contract

**Files:**
- Modify: `game/Assets/Scripts/View/CharacterVisualContract.cs`
- Test: `game/Assets/Tests/EditMode/CharacterVisualContractTests.cs`

- [ ] **Step 1: Write failing socket test**

Extend the socket test to assert `WeaponSocket`, `RightHandSocket`, `LeftHandSocket`, and `CastSocket` exist after `Configure`.

- [ ] **Step 2: Verify RED**

Run `CharacterVisualContractTests`. Expected result: failure because the new socket properties do not exist.

- [ ] **Step 3: Add socket fields and properties**

Add serialized `Transform` fields and public properties for the four new sockets.

- [ ] **Step 4: Ensure socket creation**

Update `EnsureRequiredSockets` to create:

```csharp
weaponSocket = EnsureSocket(weaponSocket, "WeaponSocket", new Vector3(0.28f, 0.72f, 0.12f));
rightHandSocket = EnsureSocket(rightHandSocket, "RightHandSocket", new Vector3(0.34f, 0.62f, 0.04f));
leftHandSocket = EnsureSocket(leftHandSocket, "LeftHandSocket", new Vector3(-0.34f, 0.62f, 0.04f));
castSocket = EnsureSocket(castSocket, "CastSocket", new Vector3(0f, 0.92f, 0.22f));
```

- [ ] **Step 5: Verify GREEN**

Run `CharacterVisualContractTests` and `PieceFactoryTests`.

## Task 4: Docs And Validation

**Files:**
- Modify: `docs/design/custom-piece-generation-workflow.md`
- Modify: `docs/design/capture-animation-roadmap.md`

- [ ] **Step 1: Update custom piece workflow**

Document that final assets must be Blender-authored white/black variants and that runtime tinting is only a temporary fallback.

- [ ] **Step 2: Update capture roadmap**

Document the socket names and capture concepts so future animation work knows where to attach props/effects.

- [ ] **Step 3: Run static checks**

Run `git diff --check`. Expected result: no whitespace errors.

- [ ] **Step 4: Run available automated tests**

Run Unity EditMode tests through Unity MCP or Unity Test Runner. Expected result: tests related to `PieceFactory`, `CharacterVisualContract`, and `TeamOutfitApplier` pass.

- [ ] **Step 5: Manual Unity check**

Open `Assets/Scenes/Main.unity`, press Play, confirm the game still opens and the existing fallback custom pieces remain visible while side-specific variants are not yet fully authored.

## Implementation Notes

- Do not delete or reset the stable tag `entrega-v1-estavel`.
- Do not remove generic prefab fields yet; the scene currently depends on them.
- Do not rely on runtime tint as the final visual direction.
- Do not create fake runtime clothing panels.
- Keep changes scoped to the variant-resolution and combat-contract layer before regenerating all Blender assets.
