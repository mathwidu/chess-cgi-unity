# Codex Blender Pawn Vertical Slice Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the first local Codex + Blender + Unity character pipeline slice by generating a modular Mathwidu pawn asset without paid services.

**Architecture:** Blender runs headless from a repo script and reads a versioned character definition. The generated GLB uses a stable modular hierarchy that Unity can import and later animate by transforms. Unity integration remains optional until the generated asset is visually accepted.

**Tech Stack:** Blender 5.1.2, Python/bpy, Unity 6.3 LTS, C#, GLB, repo-local JSON definitions.

---

## File Structure

- `tools/blender/definitions/mathwidu_pawn.json`: source-of-truth data for the first generated character.
- `tools/blender/generate_character.py`: Blender Python generator that creates a modular stylized character and exports GLB.
- `game/Assets/Art/GeneratedCharacters/MathwiduPawn/MathwiduPawn.glb`: generated output imported by Unity.
- `docs/superpowers/specs/2026-06-06-codex-blender-unity-character-pipeline-design.md`: approved architecture spec.
- `docs/superpowers/plans/2026-06-06-codex-blender-pawn-vertical-slice.md`: this executable plan.

## Task 1: Install And Verify Blender

- [x] **Step 1: Install Blender**

Run:

```bash
/opt/homebrew/bin/brew install --cask blender
```

Expected: Homebrew installs `/Applications/Blender.app` and links `/opt/homebrew/bin/blender`.

- [x] **Step 2: Verify Blender version**

Run:

```bash
/opt/homebrew/bin/blender --version | /usr/bin/head -5
```

Expected: output starts with `Blender 5.1.2`.

## Task 2: Define The First Character

- [x] **Step 1: Create the pawn definition**

Create `tools/blender/definitions/mathwidu_pawn.json` with identity, colors, proportions, outfit, and output naming for the Mathwidu pawn.

- [x] **Step 2: Validate the definition**

Run:

```bash
/usr/bin/python3 -m json.tool tools/blender/definitions/mathwidu_pawn.json >/tmp/mathwidu_pawn.json
```

Expected: command exits with code 0.

## Task 3: Generate A Modular GLB In Blender

- [x] **Step 1: Create Blender generator**

Create `tools/blender/generate_character.py` with CLI arguments:

```bash
--definition tools/blender/definitions/mathwidu_pawn.json
--output game/Assets/Art/GeneratedCharacters/MathwiduPawn/MathwiduPawn.glb
```

- [x] **Step 2: Run Blender headless**

Run:

```bash
/opt/homebrew/bin/blender --background --python tools/blender/generate_character.py -- \
  --definition tools/blender/definitions/mathwidu_pawn.json \
  --output game/Assets/Art/GeneratedCharacters/MathwiduPawn/MathwiduPawn.glb
```

Expected: output includes `Exported modular character` and creates the GLB.

- [x] **Step 3: Check output file exists**

Run:

```bash
/bin/ls -lh game/Assets/Art/GeneratedCharacters/MathwiduPawn/MathwiduPawn.glb
```

Expected: file exists and is larger than 10 KB.

## Task 4: Runtime Modular Walk Driver

- [x] **Step 1: Add modular rig component**

Create `game/Assets/Scripts/View/ModularCharacterRig.cs` to auto-bind generated part names and animate arms, legs, feet, torso, and head by transform.

- [x] **Step 2: Add modular rig tests**

Create `game/Assets/Tests/EditMode/ModularCharacterRigTests.cs` to verify auto-bind, walk pose, and reset pose.

- [x] **Step 3: Connect PieceView movement to modular rig**

Modify `game/Assets/Scripts/View/PieceView.cs` so `MoveWithWalk` calls `ModularCharacterRig.ApplyWalk` when a visual has a modular rig and resets it after movement.

## Task 5: Import Smoke Validation

- [ ] **Step 1: Trigger Unity import**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -quit \
  -projectPath game \
  -logFile Logs/unity-import-generated-pawn.log
```

Expected: Unity exits with code 0.

- [ ] **Step 2: Check compile/import logs**

Run:

```bash
/usr/bin/grep -E "error CS|Compilation failed|Scripts have compiler errors|Asset import failed|Exception" Logs/unity-import-generated-pawn.log
```

Expected: no output.

Current status: blocked while the Unity Editor already has this project open. Batchmode reported `Multiple Unity instances cannot open the same project`. Run this after closing the Editor or use the open Editor's refresh/import flow.

## Task 6: Unity Prefab Integration

This task starts only after manual visual acceptance of the generated pawn GLB.

- [ ] **Step 1: Create an editor importer command**

Create a Unity editor script that loads `MathwiduPawn.glb`, wraps it in a prefab, adds `CharacterVisualContract`, and saves `Assets/Resources/CustomPieces/Pawn_Mathwidu_Modular.prefab`.

- [ ] **Step 2: Connect the modular pawn**

Update the scene or factory configuration to use `Pawn_Mathwidu_Modular.prefab` for pawns only.

- [ ] **Step 3: Validate in Game view**

Run Unity, select a pawn, and confirm sidebar and board scale.

## Self-Review

- The plan covers the approved Blender + Unity pipeline proof, not the full six-character rollout.
- There are no external paid tools in the implementation path.
- The generated GLB is reviewed before replacing the playable pawn prefab.
- The stable `entrega-v1-estavel` fallback remains untouched.
