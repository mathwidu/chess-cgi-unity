# Unity MCP Credit-Conscious Workflow Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to apply this workflow while implementing the chess MVP. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Use the Unity MCP connection to accelerate the chess project while keeping Unity AI credit usage low and predictable.

**Architecture:** Treat MCP as an editor automation and verification layer first, not as an asset generator. Code, scene setup, validation, screenshots, and console checks use local MCP tools; credit-consuming Unity AI Assistant or asset generation features require an explicit checkpoint.

**Tech Stack:** Unity 6.3 LTS, Unity AI Assistant package with MCP Server, Codex MCP tools, local C# scripts, Unity Editor console, Git.

---

## Credit Policy

Current Unity documentation says Unity Credits can be consumed when using Unity AI features such as sending Assistant messages and generating images, 3D models, sounds, textures, videos, skyboxes, and other generated assets. Unity also says credit rates are examples only, can vary by task complexity and model, and should be monitored in the Unity Dashboard.

Project rule:

1. Use `Unity_RunCommand`, `Unity_GetConsoleLogs`, camera capture, and scene capture freely for local automation and verification.
2. Do not use `Unity_AssetGeneration_GenerateAsset` unless the user explicitly approves the exact asset, estimated purpose, and fallback.
3. Avoid Unity's in-editor AI Assistant chat for routine work; use Codex chat and local repo edits instead.
4. Prefer primitives, procedural meshes, free/manual assets, and code-generated prefabs for the MVP.
5. Review credit usage before any asset-generation sprint.

Official references checked on 2026-05-30:

- `https://docs.unity.com/en-us/ai/credits/credits-about`
- `https://support.unity.com/hc/en-us/articles/48060149523476-Getting-started-with-Unity-AI-open-beta-user-guide`
- `https://support.unity.com/hc/en-us/articles/48478191684628-How-to-cancel-Unity-AI-Unity-AI-Trial-subscription`

## MCP Tool Tiers

### Tier 0: Always Allowed

Use these whenever they help development or learning:

- `Unity_RunCommand`: inspect scene, create GameObjects, assign components, set camera/lights, create materials, save scenes, run editor helpers.
- `Unity_GetConsoleLogs`: check compile errors, runtime errors, warnings, import issues.
- Shell/git tools: edit scripts, run status, commit work, inspect project files.

### Tier 1: Use When Useful

Use these for visual validation, because they are computationally heavier but not asset-generation prompts:

- `Unity_Camera_Capture`: confirm Game camera framing.
- `Unity_SceneView_CaptureMultiAngleSceneView`: validate board layout, piece placement, camera/lights.

### Tier 2: Approval Required

Only use with explicit user approval:

- `Unity_AssetGeneration_GenerateAsset`
- `Unity_AssetGeneration_GetModels`
- Unity Assistant agent prompts inside the Editor
- Any flow that starts a paid trial, buys credits, or sends many Assistant messages

## Daily Working Loop

- [ ] **Step 1: Start state check**

Run:

```text
Use Unity_GetConsoleLogs with maxEntries 20.
Use git status --short -uall.
```

Expected: no Unity errors. If there are errors, fix them before adding features.

- [ ] **Step 2: Plan a small slice**

Pick one slice that creates visible progress in 30-90 minutes:

```text
Examples:
- rules adapter smoke tests
- generated board and square coordinates
- primitive pieces on board
- selection and legal move highlights
- camera controls
- HUD messages
```

Expected: one slice has a clear test or visual acceptance check.

- [ ] **Step 3: Implement mostly outside Unity**

Prefer editing C# files with Codex/apply_patch. Use Unity only to compile/import and verify.

Expected: scripts remain readable and versioned in Git.

- [ ] **Step 4: Use MCP for editor automation**

Use `Unity_RunCommand` for repetitive Unity work:

```text
- create empty scene roots: GameManager, Board, Pieces, Highlights
- assign scripts/components
- create materials
- position camera/lights
- save Main.unity
- run small editor inspections
```

Expected: Unity scene changes are reproducible and logged in chat.

- [ ] **Step 5: Verify**

Use:

```text
Unity_GetConsoleLogs
Unity_SceneView_CaptureMultiAngleSceneView when layout changed
Unity_Camera_Capture when camera or UI changed
Unity Test Runner manually or via Editor command when tests changed
```

Expected: no console errors; tests pass; screenshot/capture confirms the visible result.

- [ ] **Step 6: Commit**

Commit each completed slice:

```bash
git add <changed files>
git commit -m "<type>: <short result>"
```

Expected: rollback is easy if a later Unity operation breaks something.

## Chess MVP Roadmap With MCP Usage

### Phase 1: Rules And Data

Use mostly local code edits and EditMode tests.

- Build `ChessRulesAdapter`.
- Test legal moves, illegal moves, turn changes, check, mate, promotion, castling, and en passant.
- Use MCP only to check compile errors and Unity test discovery.

Credit use: zero expected.

### Phase 2: Board Scene

Use MCP heavily through `Unity_RunCommand`.

- Create scene roots.
- Create 64 board squares as primitives.
- Create materials for light/dark squares and highlights.
- Place camera and lights.
- Save `Assets/Scenes/Main.unity`.
- Capture multi-angle scene view.

Credit use: zero expected.

### Phase 3: Classic Pieces

Start with procedural primitive pieces.

- Create `PieceFactory` that builds distinguishable pawn, rook, knight, bishop, queen, and king using cylinders, spheres, capsules, cubes, and simple composition.
- Use MCP to instantiate and inspect piece prefabs or scene objects.
- Use capture tools to check readability from game camera.

Credit use: zero expected.

### Phase 4: Interaction

Use local scripts plus MCP validation.

- Add raycast selection.
- Highlight legal destinations.
- Animate movement and capture.
- Block input during animation.
- Confirm console stays clean after Play tests.

Credit use: zero expected.

### Phase 5: Game Completion

Use local code and Unity UI.

- Add HUD for turn, check, checkmate, draw, and restart.
- Add promotion UI.
- Add camera controls.
- Capture Game camera for visual review.

Credit use: zero expected.

### Phase 6: Polish

Use manual/procedural polish first.

- Improve materials.
- Add board bevels or borders.
- Add simple piece hover/selection animation.
- Add report screenshots.
- Record demo video.

Credit use: zero expected unless approved.

### Phase 7: Optional AI-Generated Assets

Only after the MVP is playable.

- Choose one high-impact asset category.
- Prefer one small experiment: one board texture, one background image, or one piece concept.
- Define an upper credit budget before starting.
- Save before generation and commit before/after separately.

Credit use: approval required.

## Approval Checklist For Any Credit-Spending Action

Before using Unity asset generation or in-editor Assistant:

- [ ] What exact asset or task are we asking Unity AI to do?
- [ ] Why is code/manual/procedural work not enough?
- [ ] What is the fallback if the generated result is bad?
- [ ] What is the maximum number of attempts?
- [ ] Has the user approved this specific action in chat?

Default maximum: one generation attempt per approved experiment.

## Recommended Defaults For This Project

- Build the MVP with zero Unity AI credits.
- Use Codex plus MCP for scene automation, not Unity Assistant prompts.
- Use primitive/procedural pieces for the professor's baseline grading.
- Spend credits only if the core game is done and we want cosmetic upgrades.
- Keep Unity AI package installed because it provides the MCP bridge, but avoid its generative tools by default.

