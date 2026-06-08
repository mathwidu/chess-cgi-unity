# Base-Free Team Outfit Walk Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remove visible bases from custom pieces, introduce safe team outfit recoloring, and polish the pawn walk into a more grounded procedural movement.

**Architecture:** Keep chess rules untouched. Change only visual/runtime presentation classes: `PieceFactory`, new `TeamOutfitApplier`, `PieceMotionSettings`, `PieceView`, `ModularCharacterRig`, and focused EditMode tests.

**Tech Stack:** Unity 6.3 LTS, C# MonoBehaviour runtime scripts, NUnit EditMode tests, existing Blender-generated GLB character pipeline.

---

### Task 1: Lock The Base-Free Custom Piece Contract

**Files:**
- Modify: `game/Assets/Tests/EditMode/PieceFactoryTests.cs`
- Modify: `game/Assets/Scripts/View/PieceFactory.cs`

- [ ] **Step 1: Write the failing test**

Change the custom pawn test to assert that custom pieces do not create `TeamBase`:

```csharp
Assert.IsNull(piece.transform.Find("TeamBase"));
Assert.IsNotNull(piece.transform.Find("CustomVisual"));
```

Rename `CreatePiece_CustomVisualHasNamedVisualRootAndTeamBase` to `CreatePiece_CustomVisualHasNamedVisualRootWithoutTeamBase`, and assert:

```csharp
Assert.IsNull(piece.transform.Find("TeamBase"));
Assert.IsNotNull(piece.transform.Find("CustomVisual"));
Assert.IsNotNull(piece.VisualRoot);
Assert.AreEqual("CustomVisual", piece.VisualRoot.name);
```

- [ ] **Step 2: Verify RED**

Run a Unity MCP probe or EditMode tests. Expected failure: custom pieces still create `TeamBase`.

- [ ] **Step 3: Implement minimal code**

In `PieceFactory.BuildCustomShape`, remove the call:

```csharp
AddCylinder(parent, "TeamBase", new Vector3(0f, 0.06f, 0f), new Vector3(0.74f, 0.12f, 0.74f), sideMaterial);
```

Keep the classic primitive fallback untouched.

- [ ] **Step 4: Verify GREEN**

Run the same Unity MCP probe or EditMode tests. Expected result: no `TeamBase`, `CustomVisual` remains present.

### Task 2: Add Safe Team Outfit Recoloring

**Files:**
- Create: `game/Assets/Scripts/View/TeamOutfitApplier.cs`
- Create: `game/Assets/Scripts/View/TeamOutfitApplier.cs.meta`
- Create: `game/Assets/Tests/EditMode/TeamOutfitApplierTests.cs`
- Create: `game/Assets/Tests/EditMode/TeamOutfitApplierTests.cs.meta`
- Modify: `game/Assets/Scripts/View/PieceFactory.cs`
- Modify: `docs/design/custom-piece-generation-workflow.md`

- [ ] **Step 1: Write failing tests**

Create tests proving:

```csharp
TeamOutfitApplier.ApplyTo(character.transform, ChessSide.White);
```

recolors a material named `TeamOutfitPrimary` to a light color, while a material named `SkinMaterial` remains unchanged.

Also test black side:

```csharp
TeamOutfitApplier.ApplyTo(character.transform, ChessSide.Black);
```

should recolor `TeamOutfitPrimary` to a dark color.

- [ ] **Step 2: Verify RED**

Run the TeamOutfit tests. Expected failure: `TeamOutfitApplier` does not exist.

- [ ] **Step 3: Implement minimal code**

Create `TeamOutfitApplier` with:

```csharp
public static int ApplyTo(Transform root, ChessSide side)
```

It scans renderers and only replaces materials when material name or renderer name contains one of:

```text
TeamOutfit
TeamClothes
TeamUniform
```

It must clone materials before changing color so source assets are not destructively edited.

- [ ] **Step 4: Wire PieceFactory**

After `ConfigureCustomVisualExtensions(visual, kind)`, call:

```csharp
TeamOutfitApplier.ApplyTo(visual.transform, side);
```

- [ ] **Step 5: Update docs**

Add a "Team outfit semantic materials" section to `docs/design/custom-piece-generation-workflow.md` requiring future Blender/Codex generated characters to expose semantic outfit materials.

- [ ] **Step 6: Verify GREEN**

Run TeamOutfit tests and PieceFactory probes.

### Task 3: Make Pawn Walk Slower And More Grounded

**Files:**
- Modify: `game/Assets/Tests/EditMode/PieceViewTests.cs`
- Modify: `game/Assets/Tests/EditMode/PieceMotionControllerTests.cs`
- Modify: `game/Assets/Tests/EditMode/ModularCharacterRigTests.cs`
- Modify: `game/Assets/Scripts/View/PieceMotionSettings.cs`
- Modify: `game/Assets/Scripts/View/PieceView.cs`
- Modify: `game/Assets/Scripts/View/ModularCharacterRig.cs`

- [ ] **Step 1: Write failing tests**

Add tests asserting:

```csharp
Assert.GreaterOrEqual(PieceMotionSettings.Default.WalkDuration, 0.75f);
Assert.LessOrEqual(PieceMotionSettings.Default.StepHeight, 0.055f);
Assert.Greater(PieceMotionSettings.Default.StrideCycles, 1.2f);
```

Add a pose test proving the mid-walk visual offset is subtle:

```csharp
PieceView.WalkPose middle = PieceView.EvaluateWalkPose(start, target, 0.5f, PieceMotionSettings.Default);
Assert.LessOrEqual(middle.VisualOffset.y, 0.055f);
Assert.AreEqual(1f, middle.RootPosition.x, 0.08f);
```

- [ ] **Step 2: Verify RED**

Run tests. Expected failure: settings still use `0.55f` duration and `0.08f` step height, and `StrideCycles` does not exist.

- [ ] **Step 3: Extend PieceMotionSettings**

Add properties:

```csharp
public float StrideCycles { get; }
public float BodySway { get; }
public float TorsoBobHeight { get; }
```

Keep the existing constructor and add an overload that accepts the new fields. Set `Default` to:

```csharp
new PieceMotionSettings(0.82f, 0.045f, 3.2f, 0.45f, 1.55f, 0.018f, 0.024f)
```

- [ ] **Step 4: Improve PieceView.EvaluateWalkPose**

Use smootherstep for root movement:

```csharp
float eased = t * t * t * (t * (t * 6f - 15f) + 10f);
```

Use stride cycles for body movement:

```csharp
float phase = t * Mathf.PI * 2f * settings.StrideCycles;
float lift = Mathf.Pow(Mathf.Abs(Mathf.Sin(phase)), 1.35f);
Vector3 visualOffset = Vector3.up * lift * settings.StepHeight + Vector3.right * Mathf.Sin(phase) * settings.BodySway;
```

- [ ] **Step 5: Pass settings to ModularCharacterRig**

Add overload:

```csharp
public void ApplyWalk(float normalizedTime, PieceMotionSettings settings)
```

Keep the old overload delegating to default settings. In `PieceView.MoveWithWalk`, call the new overload.

- [ ] **Step 6: Verify GREEN**

Run movement tests. Expected result: all movement tests pass.

### Task 4: Validate In Unity Runtime

**Files:**
- No file edits unless validation exposes a bug.

- [ ] **Step 1: Compile through Unity MCP**

Use `Unity_RunCommand` to compile and create a custom pawn through `PieceFactory`.

Expected logs:

```text
Runtime visual root: CustomVisual
Runtime has ModularCharacterRig: True
Runtime rig can animate walk: True
Runtime has CharacterVisualContract: True
Runtime TeamBase exists: False
```

- [ ] **Step 2: Check console**

Use `Unity_GetConsoleLogs` for errors and warnings. Expected: 0 errors.

- [ ] **Step 3: Manual play check**

Open `Assets/Scenes/Main.unity`, press Play, move a pawn, and check:

- no visible custom base;
- pawn stands on the board;
- pawn movement is slower and less floaty;
- black and white identity still reads from outfit/visual design.

### Task 5: Document Next Animation Phase

**Files:**
- Modify: `docs/design/capture-animation-roadmap.md`

- [ ] **Step 1: Add movement prerequisite**

Add a short section saying capture animations should start after base-free movement and pawn walk polish are stable.

- [ ] **Step 2: Add per-piece movement notes**

Record the target motions:

- Pawn: grounded walk.
- Rook: heavy hop.
- Knight: arcing L jump.
- Bishop: ritual stride.
- Queen: confident walk.
- King: authoritative short steps.

- [ ] **Step 3: Verify docs**

Run:

```bash
rg -n "base-free|grounded walk|arcing L jump|heavy hop" docs/design/capture-animation-roadmap.md docs/superpowers/specs/2026-06-07-base-free-team-outfit-walk-design.md
```

Expected: each phrase appears in the relevant docs.
