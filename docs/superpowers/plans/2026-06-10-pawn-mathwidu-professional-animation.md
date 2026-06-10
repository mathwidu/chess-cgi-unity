# Pawn Mathwidu Professional Animation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Promote the current Mathwidu pawn from partial rig/procedural movement to a professional animated vertical slice with real Blender-authored rig/animation clips, Unity Animator integration, grounded walking, and a first theatrical capture.

**Architecture:** Keep chess rules and board state independent from animation. The board moves the `PieceView` root between squares; the character rig animates inside `VisualRoot`. If the professional Animator path fails, the current procedural walk remains the fallback so the playable delivery is not lost.

**Tech Stack:** Unity 6.3 LTS `6000.3.16f1`, Blender local Python pipeline, C# MonoBehaviour scripts, Unity Animator/AnimationClip assets, NUnit EditMode/PlayMode tests, Git branch `feature/animated-pieces-and-sidebar`.

---

## Current Evidence

- `Pawn_Mathwidu_v3b.prefab`, `Pawn_Mathwidu_White.prefab`, and `Pawn_Mathwidu_Black.prefab` already contain `SkinnedMeshRenderer`, an armature hierarchy, and an `Animator` component.
- The current `Animator` components have no `Avatar` and no `RuntimeAnimatorController` assigned.
- The visible walking quality currently comes from `PieceView.MoveWithWalk()` plus `ModularCharacterRig.ApplyWalk()`, not from real authored clips.
- There are no `.anim` or `.controller` files currently in `game/Assets`.
- The plan therefore does not regenerate the pawn from zero. It upgrades the current pawn into a fully animated vertical slice.

## Non-Goals

- Do not rig every character in this plan.
- Do not merge into `main`.
- Do not spend Unity AI credits.
- Do not overwrite the stable delivery branch or tag.
- Do not remove the procedural fallback.
- Do not use personal reference photos in git.

## Safety Rules

- Work only on `feature/animated-pieces-and-sidebar`.
- Commit after each stable task group.
- Keep `main`, `stable/entrega-v1-estavel`, and tag `entrega-v1-estavel` untouched.
- If the pawn cannot reach an acceptable rigged state within the available time, keep the current procedural pawn and document the blocker.
- A feature is not complete until the game remains playable after `Play`, piece movement, capture, turn change, and new game.

## Target Pawn Contract

Final pawn prefab names:

```text
Assets/Resources/CustomPieces/Pawn_Mathwidu_Animated_White.prefab
Assets/Resources/CustomPieces/Pawn_Mathwidu_Animated_Black.prefab
```

Temporary review assets:

```text
Assets/Art/CharacterCandidates/Pawn_Mathwidu/professional_animation/
```

Required hierarchy:

```text
Pawn_Mathwidu_Animated_<Side>
  VisualRoot
    Armature
      Hips
      Spine
      Chest
      Neck
      Head
      UpperArm.L / Forearm.L / Hand.L
      UpperArm.R / Forearm.R / Hand.R
      Thigh.L / Shin.L / Foot.L / Toe.L
      Thigh.R / Shin.R / Foot.R / Toe.R
    Meshes
      Body
      Hair
      Shirt
      Pants
      Shoes
      FaceDetails
    Sockets
      GroundSocket
      HitSocket
      EffectsSocket
      RightHandSocket
      LeftHandSocket
      WeaponSocket
      CastSocket
  Animator
  CharacterAnimationDriver
  CharacterVisualContract
  ModularCharacterRig
```

Required clips:

| Clip | Target duration | Loop | Purpose |
| --- | ---: | --- | --- |
| `Pawn_Idle_Breathing` | 2.0s | yes | subtle idle body motion |
| `Pawn_Walk_Grounded` | 1.2s | yes | real foot/arm walk cycle |
| `Pawn_Move_Start` | 0.2s | no | small anticipation before movement |
| `Pawn_Move_Stop` | 0.2s | no | settle at destination |
| `Pawn_Attack_DaggerLunge` | 0.55s | no | pawn capture strike |
| `Pawn_Hit_Recoil` | 0.35s | no | reaction when hit |
| `Pawn_Captured_Fall` | 0.55s | no | defeated/captured reaction |
| `Pawn_Selected_Pose` | 1.5s | yes | subtle sidebar/selected pose, optional for gameplay |

Animator state names must match the clip names above.

---

## File Map

### Blender / Pipeline

- Create: `tools/blender/create_mathwidu_pawn_animation_pack.py`
  - Opens or imports the approved pawn source.
  - Ensures rig naming, sockets, feet, shoes, and animation actions.
  - Exports review GLB/FBX files.
- Create: `tools/blender/tests/test_mathwidu_pawn_animation_pack.py`
  - Validates expected output files, action names, socket names, and manifest fields.
- Modify: `tools/blender/definitions/mathwidu_pawn_v2.json`
  - Add animation contract fields and output paths.
- Create: `game/Assets/Art/CharacterCandidates/Pawn_Mathwidu/professional_animation/character_animation_manifest.json`
  - Tracks source, clips, durations, export status, and manual review decisions.

### Unity Runtime

- Modify: `game/Assets/Scripts/View/CharacterAnimationDriver.cs`
  - Add named methods for idle, move, attack, hit, captured.
  - Add controller/clip checks and safe fallback.
- Modify: `game/Assets/Scripts/View/PieceMotionController.cs`
  - Use `CharacterAnimationDriver` for animated movement when it has a valid controller.
  - Preserve procedural movement fallback.
  - Guarantee movement completion callback behavior.
- Modify: `game/Assets/Scripts/View/PieceView.cs`
  - Add a narrow integration point for animated root movement without duplicating chess logic.
  - Keep `MoveWithWalk()` as fallback.
- Modify: `game/Assets/Scripts/View/CharacterVisualContract.cs`
  - Record animation readiness and sockets for the pawn.
- Modify: `game/Assets/Scripts/View/PieceFactory.cs`
  - Prefer animated pawn prefabs only after validation passes.

### Unity Assets

- Create: `game/Assets/Animations/Characters/Pawn_Mathwidu/`
- Create: `game/Assets/Animations/Characters/Pawn_Mathwidu/Pawn_Mathwidu.controller`
- Create/import clips under `game/Assets/Animations/Characters/Pawn_Mathwidu/`
- Create: `game/Assets/Resources/CustomPieces/Pawn_Mathwidu_Animated_White.prefab`
- Create: `game/Assets/Resources/CustomPieces/Pawn_Mathwidu_Animated_Black.prefab`

### Tests

- Modify: `game/Assets/Tests/EditMode/CharacterAnimationDriverTests.cs`
- Modify: `game/Assets/Tests/EditMode/PieceMotionControllerTests.cs`
- Modify: `game/Assets/Tests/EditMode/CustomPieceVisualContractTests.cs`
- Create: `game/Assets/Tests/EditMode/PawnAnimationContractTests.cs`
- Create or extend PlayMode smoke test for one pawn move and one pawn capture.

---

## Task 1: Safety Snapshot

**Files:** none, git only.

- [ ] **Step 1: Confirm branch**

Run:

```bash
git status --short --branch
git branch --show-current
```

Expected:

```text
## feature/animated-pieces-and-sidebar...origin/feature/animated-pieces-and-sidebar
feature/animated-pieces-and-sidebar
```

- [ ] **Step 2: Confirm stable fallback exists**

Run:

```bash
git tag --list 'entrega-*'
git branch --list 'stable/entrega-v1-estavel'
```

Expected:

```text
entrega-v1-estavel
entrega-v2-polida
  stable/entrega-v1-estavel
```

- [ ] **Step 3: Create local safety tag for this animation attempt**

Run:

```bash
git tag -a before-pawn-professional-animation -m "Before professional pawn animation attempt"
```

If the tag already exists, do not recreate it. Run:

```bash
git rev-parse before-pawn-professional-animation
```

- [ ] **Step 4: Commit any uncommitted plan-only changes before asset work**

Run:

```bash
git status --short
```

If only this plan file is modified, commit it:

```bash
git add docs/superpowers/plans/2026-06-10-pawn-mathwidu-professional-animation.md
git commit -m "docs: plan professional pawn animation"
```

---

## Task 2: Pawn Rig Audit Gate

**Files:**
- Create: `tools/character_pipeline/audit_pawn_animation_contract.py`
- Create: `tools/character_pipeline/tests/test_audit_pawn_animation_contract.py`
- Read: `game/Assets/Resources/CustomPieces/Pawn_Mathwidu_v3b.prefab`
- Read: `game/Assets/Resources/CustomPieces/Pawn_Mathwidu_White.prefab`
- Read: `game/Assets/Resources/CustomPieces/Pawn_Mathwidu_Black.prefab`

- [ ] **Step 1: Write the audit test**

The test must assert that the current pawn prefabs have:

```text
SkinnedMeshRenderer: present
Animator: present
Armature: present
m_Avatar: missing at baseline
m_Controller: missing at baseline
```

Run:

```bash
python3 -m unittest tools.character_pipeline.tests.test_audit_pawn_animation_contract -v
```

Expected before implementation:

```text
FAILED
```

- [ ] **Step 2: Implement the audit script**

Script output must be JSON with this shape:

```json
{
  "prefabs": [
    {
      "name": "Pawn_Mathwidu_v3b",
      "hasSkinnedMeshRenderer": true,
      "hasAnimator": true,
      "hasArmatureToken": true,
      "hasAvatar": false,
      "hasController": false
    }
  ]
}
```

- [ ] **Step 3: Run audit test**

Run:

```bash
python3 -m unittest tools.character_pipeline.tests.test_audit_pawn_animation_contract -v
```

Expected:

```text
OK
```

- [ ] **Step 4: Commit**

Run:

```bash
git add tools/character_pipeline/audit_pawn_animation_contract.py tools/character_pipeline/tests/test_audit_pawn_animation_contract.py
git commit -m "test: audit pawn animation contract"
```

---

## Task 3: Blender Animation Pack Script

**Files:**
- Create: `tools/blender/create_mathwidu_pawn_animation_pack.py`
- Create: `tools/blender/tests/test_mathwidu_pawn_animation_pack.py`
- Modify: `tools/blender/definitions/mathwidu_pawn_v2.json`

- [ ] **Step 1: Write failing Python tests for expected outputs**

The test must verify:

```text
professional_animation/Pawn_Mathwidu_Animated.glb exists
professional_animation/Pawn_Mathwidu_Animated.fbx exists if FBX export is available
professional_animation/character_animation_manifest.json exists
manifest lists all required clips
manifest lists all required sockets
manifest declares visibleBaseRemoved = true
manifest declares shoesComplete = true
```

Run:

```bash
python3 -m unittest tools.blender.tests.test_mathwidu_pawn_animation_pack -v
```

Expected:

```text
FAILED
```

- [ ] **Step 2: Implement manifest generation first**

Create `character_animation_manifest.json` with:

```json
{
  "character": "Pawn_Mathwidu",
  "piece": "Pawn",
  "status": "animation_pack_generated",
  "source": "Pawn_Mathwidu_v3b",
  "visibleBaseRemoved": true,
  "shoesComplete": true,
  "clips": [
    "Pawn_Idle_Breathing",
    "Pawn_Walk_Grounded",
    "Pawn_Move_Start",
    "Pawn_Move_Stop",
    "Pawn_Attack_DaggerLunge",
    "Pawn_Hit_Recoil",
    "Pawn_Captured_Fall",
    "Pawn_Selected_Pose"
  ],
  "sockets": [
    "GroundSocket",
    "HitSocket",
    "EffectsSocket",
    "RightHandSocket",
    "LeftHandSocket",
    "WeaponSocket",
    "CastSocket"
  ]
}
```

- [ ] **Step 3: Implement Blender import/export shell**

The script must accept:

```bash
--source game/Assets/Art/CharacterCandidates/Pawn_Mathwidu/v3b/Pawn_Mathwidu_v3b.glb
--output game/Assets/Art/CharacterCandidates/Pawn_Mathwidu/professional_animation
```

It must fail with a clear message if Blender cannot import the source.

- [ ] **Step 4: Implement socket creation**

Required empty objects:

```text
GroundSocket
HitSocket
EffectsSocket
RightHandSocket
LeftHandSocket
WeaponSocket
CastSocket
```

Socket locations should match the Unity `CharacterVisualContract` defaults unless Blender provides better bone-based placement.

- [ ] **Step 5: Implement animation actions**

Create Blender actions with exact names:

```text
Pawn_Idle_Breathing
Pawn_Walk_Grounded
Pawn_Move_Start
Pawn_Move_Stop
Pawn_Attack_DaggerLunge
Pawn_Hit_Recoil
Pawn_Captured_Fall
Pawn_Selected_Pose
```

The first version may use keyed bone transforms if the armature is valid. If armature binding is not usable, the script must mark the manifest:

```json
"status": "blocked_armature_not_animatable"
```

and stop before exporting a misleading asset.

- [ ] **Step 6: Run Blender generation**

Run with the local Blender binary. On macOS:

```bash
/Applications/Blender.app/Contents/MacOS/Blender --background --python tools/blender/create_mathwidu_pawn_animation_pack.py -- --source game/Assets/Art/CharacterCandidates/Pawn_Mathwidu/v3b/Pawn_Mathwidu_v3b.glb --output game/Assets/Art/CharacterCandidates/Pawn_Mathwidu/professional_animation
```

On Windows, use the installed `blender.exe` path.

- [ ] **Step 7: Run tests**

Run:

```bash
python3 -m unittest tools.blender.tests.test_mathwidu_pawn_animation_pack -v
```

Expected:

```text
OK
```

- [ ] **Step 8: Commit**

Run:

```bash
git add tools/blender/create_mathwidu_pawn_animation_pack.py tools/blender/tests/test_mathwidu_pawn_animation_pack.py tools/blender/definitions/mathwidu_pawn_v2.json game/Assets/Art/CharacterCandidates/Pawn_Mathwidu/professional_animation
git commit -m "feat: generate professional pawn animation pack"
```

---

## Task 4: Manual Blender Review Gate

**Files:**
- Review: `game/Assets/Art/CharacterCandidates/Pawn_Mathwidu/professional_animation/`
- Update: `game/Assets/Art/CharacterCandidates/Pawn_Mathwidu/professional_animation/character_animation_manifest.json`
- Update: `docs/design/character-rig-audit.md`

- [ ] **Step 1: Open generated asset in Blender**

Checklist:

```text
head recognizable as Mathwidu
ginger/red hair visible
white shoes complete
no visible chess base
feet not clipped
arms not fused to torso
legs separate enough for walking
clothes readable from board camera
white/black outfit surfaces still recolorable
```

- [ ] **Step 2: Scrub each action**

Review:

```text
Pawn_Idle_Breathing
Pawn_Walk_Grounded
Pawn_Move_Start
Pawn_Move_Stop
Pawn_Attack_DaggerLunge
Pawn_Hit_Recoil
Pawn_Captured_Fall
Pawn_Selected_Pose
```

Reject criteria:

```text
feet rotate through floor
arms twist unnaturally
head detaches or warps
shirt/pants collapse
walk looks worse than current procedural walk
```

- [ ] **Step 3: Update manifest review status**

Accepted:

```json
"manualReview": "approved_for_unity_import"
```

Rejected:

```json
"manualReview": "rejected",
"rejectionReason": "specific visible problem"
```

- [ ] **Step 4: Commit review decision**

Run:

```bash
git add game/Assets/Art/CharacterCandidates/Pawn_Mathwidu/professional_animation/character_animation_manifest.json docs/design/character-rig-audit.md
git commit -m "docs: record pawn animation review"
```

---

## Task 5: Unity Import And Animator Assets

**Files:**
- Create: `game/Assets/Animations/Characters/Pawn_Mathwidu/`
- Create: `game/Assets/Animations/Characters/Pawn_Mathwidu/Pawn_Mathwidu.controller`
- Create/import: `.anim` clips under that folder
- Create: `game/Assets/Resources/CustomPieces/Pawn_Mathwidu_Animated_White.prefab`
- Create: `game/Assets/Resources/CustomPieces/Pawn_Mathwidu_Animated_Black.prefab`

- [ ] **Step 1: Import generated GLB/FBX into Unity**

Use Unity Editor import if available. Import settings:

```text
Rig/Animation Type: Humanoid if avatar validates
Rig/Animation Type: Generic if Humanoid fails but bones animate correctly
Avatar Definition: Create From This Model
Root Motion: disabled for gameplay
Animation import: enabled
Loop Time: true only for Idle, Walk, Selected
```

- [ ] **Step 2: Create Animator Controller**

States:

```text
Idle -> Pawn_Idle_Breathing
Walk -> Pawn_Walk_Grounded
MoveStart -> Pawn_Move_Start
MoveStop -> Pawn_Move_Stop
Attack -> Pawn_Attack_DaggerLunge
Hit -> Pawn_Hit_Recoil
Captured -> Pawn_Captured_Fall
Selected -> Pawn_Selected_Pose
```

Parameters:

```text
Trigger MoveStart
Bool IsMoving
Trigger MoveStop
Trigger Attack
Trigger Hit
Trigger Captured
Bool Selected
Float MoveSpeed
```

- [ ] **Step 3: Create animated white/black prefabs**

Rules:

```text
White prefab uses light/white clothing
Black prefab uses dark/black clothing
No visible circular TeamBase
Root stays at board square origin
VisualRoot has localPosition zero
Animator assigned to Pawn_Mathwidu.controller
CharacterVisualContract rigStatus is RiggedHumanoid or RiggedProp
Sockets present
```

- [ ] **Step 4: Do not switch PieceFactory yet**

The old pawn remains active until tests and manual Play validation pass.

- [ ] **Step 5: Commit imported assets**

Run:

```bash
git add game/Assets/Animations/Characters/Pawn_Mathwidu game/Assets/Resources/CustomPieces/Pawn_Mathwidu_Animated_White.prefab game/Assets/Resources/CustomPieces/Pawn_Mathwidu_Animated_White.prefab.meta game/Assets/Resources/CustomPieces/Pawn_Mathwidu_Animated_Black.prefab game/Assets/Resources/CustomPieces/Pawn_Mathwidu_Animated_Black.prefab.meta
git commit -m "feat: import animated pawn prefabs"
```

---

## Task 6: CharacterAnimationDriver Professional API

**Files:**
- Modify: `game/Assets/Scripts/View/CharacterAnimationDriver.cs`
- Modify: `game/Assets/Tests/EditMode/CharacterAnimationDriverTests.cs`

- [ ] **Step 1: Write failing tests**

Required test behaviors:

```text
HasPlayableAnimator is false when Animator has no controller
TryPlayMove sets IsMoving true and MoveSpeed
TryStopMove sets IsMoving false and fires MoveStop
TryPlayAttack uses Pawn_Attack_DaggerLunge for pawn capture
TryPlayHit fires Hit trigger
TryPlayCaptured fires Captured trigger
All methods return false without playable Animator
```

Run Unity EditMode tests. Expected: failing tests.

- [ ] **Step 2: Implement driver methods**

Public API:

```csharp
public bool HasPlayableAnimator { get; }
public bool TryPlayIdle();
public bool TryStartMove(float moveSpeed);
public bool TryStopMove();
public bool TryPlayAttack(CaptureAnimationStyle style);
public bool TryPlayHit();
public bool TryPlayCaptured();
public bool TrySetSelected(bool selected);
```

Rules:

```text
Never throw if Animator is absent
Return false when controller is absent
Do not own chess movement
Do not depend on current Animator state names except through constants
```

- [ ] **Step 3: Run tests**

Expected: `CharacterAnimationDriverTests` passes.

- [ ] **Step 4: Commit**

Run:

```bash
git add game/Assets/Scripts/View/CharacterAnimationDriver.cs game/Assets/Tests/EditMode/CharacterAnimationDriverTests.cs
git commit -m "feat: add professional character animation driver"
```

---

## Task 7: Animated Movement Integration

**Files:**
- Modify: `game/Assets/Scripts/View/PieceMotionController.cs`
- Modify: `game/Assets/Scripts/View/PieceView.cs`
- Modify: `game/Assets/Tests/EditMode/PieceMotionControllerTests.cs`
- Create: `game/Assets/Tests/EditMode/PawnAnimationContractTests.cs`

- [ ] **Step 1: Write failing movement tests**

Test cases:

```text
MovePiece starts animated move when CharacterAnimationDriver.HasPlayableAnimator is true
MovePiece still moves root to exact target
MovePiece stops animated move at the end
MovePiece uses procedural fallback when Animator controller is missing
MovePiece faces target before movement
MovePiece resets VisualRoot local transform after movement
```

- [ ] **Step 2: Add animated move path**

Rules:

```text
Root movement remains Vector3.Lerp between board squares
Animator clip moves limbs only
Root motion stays disabled
Travel duration uses max(style.Duration, configured pawn walk clip duration)
MoveSpeed parameter scales clip when movement duration changes
Input remains blocked through existing controller flow
```

- [ ] **Step 3: Preserve procedural fallback**

If:

```text
CharacterAnimationDriver missing
Animator missing
Controller missing
Clip missing
Runtime error
```

Then:

```text
Use existing MoveWithWalk()
Log a warning only once per prefab type if practical
Do not break the move
```

- [ ] **Step 4: Run tests**

Run Unity EditMode test runner for:

```text
CharacterAnimationDriverTests
PieceMotionControllerTests
PawnAnimationContractTests
```

- [ ] **Step 5: Commit**

Run:

```bash
git add game/Assets/Scripts/View/PieceMotionController.cs game/Assets/Scripts/View/PieceView.cs game/Assets/Tests/EditMode/PieceMotionControllerTests.cs game/Assets/Tests/EditMode/PawnAnimationContractTests.cs
git commit -m "feat: drive pawn movement with Animator fallback"
```

---

## Task 8: Pawn Capture V1

**Files:**
- Modify: `game/Assets/Scripts/View/CaptureAnimationController.cs`
- Modify: `game/Assets/Scripts/View/PieceMotionController.cs`
- Modify: `game/Assets/Scripts/View/CaptureAnimationLibrary.cs`
- Modify: `game/Assets/Tests/EditMode/PieceMotionControllerTests.cs`
- Modify: `game/Assets/Tests/EditMode/CaptureAnimationLibraryTests.cs`

- [ ] **Step 1: Write failing capture tests**

Test cases:

```text
Pawn capture tries Pawn_Attack_DaggerLunge
Captured pawn tries Hit or Captured before being hidden
Fallback capture still hides captured piece
Attacker still ends exactly on destination
Capture duration never blocks indefinitely
```

- [ ] **Step 2: Implement pawn attack sequence**

Sequence:

```text
attacker faces captured piece
attacker TryPlayAttack(style)
captured TryPlayHit()
short impact effect at captured HitSocket
captured TryPlayCaptured()
captured hidden after impact window
attacker moves to final square
attacker returns to Idle
```

- [ ] **Step 3: Keep capture short**

Target total duration:

```text
0.75s to 1.25s
```

Reject if:

```text
move feels sluggish
piece disappears before visible hit
turn change happens before destination is reached
```

- [ ] **Step 4: Commit**

Run:

```bash
git add game/Assets/Scripts/View/CaptureAnimationController.cs game/Assets/Scripts/View/PieceMotionController.cs game/Assets/Scripts/View/CaptureAnimationLibrary.cs game/Assets/Tests/EditMode/PieceMotionControllerTests.cs game/Assets/Tests/EditMode/CaptureAnimationLibraryTests.cs
git commit -m "feat: add pawn theatrical capture animation"
```

---

## Task 9: Switch Active Pawn Prefabs

**Files:**
- Modify: `game/Assets/Scripts/View/PieceFactory.cs`
- Modify: `game/Assets/Scenes/Main.unity` if serialized prefab references are scene-level
- Modify: `game/Assets/Tests/EditMode/CustomPieceVisualContractTests.cs`
- Modify: `game/Assets/Tests/EditMode/CustomPieceCoverageTests.cs`

- [ ] **Step 1: Write failing coverage test**

Test should assert:

```text
White pawns instantiate Pawn_Mathwidu_Animated_White or equivalent active animated prefab
Black pawns instantiate Pawn_Mathwidu_Animated_Black or equivalent active animated prefab
Active pawn prefab has no visible TeamBase token
Active pawn prefab has Animator with controller
Active pawn prefab has CharacterAnimationDriver
Active pawn prefab has CharacterVisualContract
```

- [ ] **Step 2: Switch only pawn**

Do not change rook, knight, bishop, queen, or king in this task.

- [ ] **Step 3: Run coverage tests**

Expected:

```text
CustomPieceCoverageTests pass
CustomPieceVisualContractTests pass
PawnAnimationContractTests pass
```

- [ ] **Step 4: Commit**

Run:

```bash
git add game/Assets/Scripts/View/PieceFactory.cs game/Assets/Scenes/Main.unity game/Assets/Tests/EditMode/CustomPieceVisualContractTests.cs game/Assets/Tests/EditMode/CustomPieceCoverageTests.cs
git commit -m "feat: use animated Mathwidu pawn prefabs"
```

---

## Task 10: Manual Gameplay Review

**Files:**
- Update: `docs/design/character-rig-audit.md`
- Update: `docs/design/overnight-character-polish-report.md` or create a new dated report if clearer

- [ ] **Step 1: Open Unity scene**

Open:

```text
game/Assets/Scenes/Main.unity
```

- [ ] **Step 2: Test white pawn move**

Manual script:

```text
Start game
Move e2 to e4
Observe foot motion
Observe root destination
Observe camera turn change
```

Acceptance:

```text
feet visibly move
body does not slide obviously
pawn faces destination
pawn ends centered on e4
turn changes after movement
no console errors
```

- [ ] **Step 3: Test black pawn move**

Manual script:

```text
Move e7 to e5
Observe dark outfit
Observe orientation
Observe foot motion
```

Acceptance:

```text
black pawn is visually distinct
does not walk backward
does not show white outfit
ends centered on e5
```

- [ ] **Step 4: Test pawn capture**

Manual script:

```text
White: e2-e4
Black: d7-d5
White: e4xd5
```

Acceptance:

```text
attack animation triggers
captured piece reacts before disappearing
attacker ends on d5
move history records capture
game remains playable
```

- [ ] **Step 5: Test sidebar**

Acceptance:

```text
sidebar shows full pawn body
preview can rotate
preview can zoom
animated pawn does not appear clipped
```

- [ ] **Step 6: Document review result**

Record:

```text
approved
approved_with_minor_issues
rejected
```

If rejected, list concrete visual issue and fallback to old pawn.

- [ ] **Step 7: Commit review**

Run:

```bash
git add docs/design/character-rig-audit.md docs/design/overnight-character-polish-report.md
git commit -m "docs: validate animated pawn gameplay"
```

---

## Task 11: Automated Validation Gate

**Files:** none unless tests fail and require fixes.

- [ ] **Step 1: Run Python pipeline tests**

Run:

```bash
python3 -m unittest tools.blender.tests.test_character_definition tools.blender.tests.test_character_quality_manifest tools.blender.tests.test_mathwidu_v3b_candidate tools.blender.tests.test_all_piece_side_variants tools.blender.tests.test_mathwidu_pawn_animation_pack tools.character_pipeline.tests.test_audit_custom_pieces tools.character_pipeline.tests.test_audit_pawn_animation_contract -v
```

Expected:

```text
OK
```

- [ ] **Step 2: Run Unity EditMode tests**

Use Unity Test Runner:

```text
Window > General > Test Runner > EditMode > Run All
```

Expected:

```text
All EditMode tests pass
```

- [ ] **Step 3: Run Unity PlayMode tests**

Use Unity Test Runner:

```text
Window > General > Test Runner > PlayMode > Run All
```

Expected:

```text
All PlayMode tests pass
```

- [ ] **Step 4: Run git hygiene checks**

Run:

```bash
git diff --check
git ls-files | rg '(^|/)(Library|Temp|UserSettings|Builds|Logs|TestResults|PrivateReferences|_Recovery|GeneratedAssets)(/|$)' || true
```

Expected:

```text
no output
```

---

## Task 12: Cut Decision

**Files:**
- Update: `README.md` only if the animated pawn becomes part of the recommended demo.
- Update: `docs/setup/repository-delivery-flow.md` only if branch/tag policy changes.

- [ ] **Step 1: If approved, tag the animated pawn milestone**

Run:

```bash
git tag -a pawn-animation-v1 -m "Animated Mathwidu pawn vertical slice"
```

- [ ] **Step 2: Push branch and tag**

Run:

```bash
git push origin feature/animated-pieces-and-sidebar
git push origin pawn-animation-v1
```

- [ ] **Step 3: If rejected, keep fallback**

If manual review rejects the animated pawn:

```bash
git checkout before-pawn-professional-animation -- game/Assets/Scripts/View/PieceFactory.cs game/Assets/Scenes/Main.unity
```

Then commit a docs-only note explaining why the animated pawn remains experimental.

- [ ] **Step 4: Decide next character**

If pawn passes:

```text
Next: Bishop_Rafael, because he is the simplest humanoid standing character after the pawn.
```

If pawn fails:

```text
Next: improve Blender rig generator or accept procedural movement for delivery.
```

---

## Definition Of Done

The pawn vertical slice is complete only when all are true:

- White and black animated pawn prefabs exist.
- Both use side-specific clothing colors without visible bases.
- Both have playable Animator Controller assigned.
- The pawn moves with visible feet/arms/body motion.
- The pawn does not walk backward.
- The pawn ends exactly in the target square.
- Pawn capture plays an attack/reaction sequence before hiding the captured piece.
- Procedural fallback still works for non-animated pieces.
- EditMode tests pass.
- PlayMode smoke tests pass.
- No private references or Unity caches are tracked by git.
- The work remains isolated on `feature/animated-pieces-and-sidebar`.

## Replication Rule For Other Characters

Do not start Rafael, Marta, Ricardo, Alex, or Gustavo until the pawn reaches `approved` or `approved_with_minor_issues`.

After approval:

1. Copy this contract.
2. Replace pawn-specific clips with piece-specific clips.
3. Keep the same socket names.
4. Keep the same Unity driver API.
5. Add one character at a time.
6. Commit after each character.

Recommended order:

```text
1. Bishop_Rafael
2. King_Ricardo_Carioca
3. Queen_Marta
4. Rook_Alex
5. Knight_Gustavo
```

Rook and Knight must use special movement:

- Rook: heavy hop / tower weight, not normal human walking.
- Knight: horse jump arc, Gustavo body follows mount.

