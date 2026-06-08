# Blender + Codex integration security plan

Date: 2026-06-06

## Objective

Improve the character production pipeline for the Unity chess project using Codex + Blender + Unity, with no required paid tools beyond the current Codex subscription, while avoiding unsafe installation of third-party skills, MCP servers, or arbitrary code bridges.

The target is a professional stylized asset pipeline, not AAA photorealism. The practical goal is:

- repeatable character generation from local definitions;
- visible Blender inspection and iteration;
- GLB export into Unity;
- rig/animation-ready naming contracts;
- enough documentation to continue without chat context.

## Current baseline

Already available locally:

- Blender 5.1.2 installed at `/Applications/Blender.app` and `/opt/homebrew/bin/blender`;
- project-local Blender generation script at `tools/blender/generate_character.py`;
- first generated proof asset at `game/Assets/Art/GeneratedCharacters/MathwiduPawn/MathwiduPawn.glb`;
- Unity runtime fallback for modular walking through `ModularCharacterRig`;
- stable Unity delivery checkpoint preserved separately through git tag `entrega-v1-estavel`.

The generated pawn proves the local script pipeline works, but the visual quality is still below the target. The next improvement should be pipeline quality and Blender feedback, not replacing all characters yet.

## Candidate tools reviewed

### Option A: project-local Blender CLI scripts

Status: approved now.

Cost: free.

Security profile: lowest risk.

Workflow:

```bash
/opt/homebrew/bin/blender --background --python tools/blender/generate_character.py -- \
  --definition tools/blender/definitions/mathwidu_pawn.json \
  --output game/Assets/Art/GeneratedCharacters/MathwiduPawn/MathwiduPawn.glb \
  --preview game/Assets/Art/GeneratedCharacters/MathwiduPawn/MathwiduPawn_preview.png
```

Why this remains the foundation:

- all code is versioned in our repo;
- output is reproducible;
- no global agent extension is required;
- no external service is required;
- review and tests can run before Unity import.

### Option B: Flue Blender skill / bridge

Status: promising, but not approved for install yet.

Cost: appears free/open source; installation still requires explicit approval.

Security profile: medium risk.

Audit notes:

- `sfkislev/flue@blender` is a skill for controlling Blender through a local shell-to-Blender bridge, not an MCP server.
- The Blender bridge installs an in-process Blender addon exposing a tokenized localhost evaluation endpoint.
- The addon runs Python through `eval`/`exec` inside Blender.
- It uses a random local token and binds to `127.0.0.1`, which is good, but arbitrary Blender Python is still powerful.
- The package includes adapters for multiple apps, so installation scope is broader than only our game.

Use only if:

- we pin the exact version or commit;
- we review the files again immediately before installing;
- we install only after user approval;
- we run it first on a disposable `.blend`;
- we do not let it save/export/delete files unless a task explicitly needs that.

### Option C: Blender MCP

Status: useful for live inspection, but not approved for install yet.

Cost: Blender MCP itself appears free/open source. Some optional integrations are not free or may require accounts/API keys.

Security profile: medium/high risk.

Audit notes:

- The popular `ahujasid/blender-mcp` project exposes MCP tools for Blender scene inspection, screenshots, object editing, and arbitrary Python execution.
- The server depends on `mcp`, `supabase`, and `tomli`.
- It has telemetry support. Telemetry must be disabled before use.
- The Blender addon opens a socket server, defaulting to port `9876`.
- It can integrate with Poly Haven, Sketchfab, Hyper3D Rodin, and Hunyuan3D. These should remain disabled for our no-cost local workflow unless explicitly approved.
- The tool `execute_blender_code` / `execute_code` can run arbitrary Python in Blender.

Use only if:

- telemetry is disabled with `DISABLE_TELEMETRY=true`;
- Poly Haven, Sketchfab, Hyper3D, and Hunyuan are off by default;
- no API keys are entered;
- the server is started only for the current work session;
- any MCP-generated code is reviewed before execution if it touches files, sockets, subprocesses, or external URLs;
- exports still happen through our project-local headless Blender scripts when possible.

### Option D: third-party Blender MCP workflow skill

Status: useful as reading material, not enough by itself.

Cost: free to inspect/install as a skill, but install still requires approval.

Security profile: low/medium risk.

Audit notes:

- `vladmdgolam/agent-skills@blender-mcp` is mainly workflow documentation for using Blender MCP tools.
- It contains good GLB/export validation guidance, especially around material export and `gltf-transform`.
- It assumes an MCP server already exists; installing this skill alone does not create the Blender connection.
- Some guidance targets web/Three.js rather than Unity, so we should adapt it instead of copying it blindly.

## Security rules

No install rule:

- Do not install skills, Python packages, Blender addons, or MCP servers without explicit user approval in the current session.

No blind execution rule:

- Do not run code copied from a third-party skill/MCP output unless it has been reviewed for file writes, network calls, subprocesses, destructive Blender ops, hidden persistence, and token/API-key handling.

No secret rule:

- Do not enter API keys into Blender addons for this project unless a future task explicitly requires a paid/external service.

No telemetry rule:

- Any MCP/bridge with telemetry must be launched with telemetry disabled when supported.

No global-first rule:

- Prefer project-local scripts and project-local config over global installation. If a global install is unavoidable, document exact package/version/commit.

No production-scene-first rule:

- Test a new bridge against a disposable `.blend` before touching the Unity chess assets.

## Recommended no-cost path

### Phase 1: keep Blender CLI as the source of truth

Improve `tools/blender/generate_character.py` until the Mathwidu pawn looks good in preview renders:

- better anatomy proportions;
- more deliberate stylized face;
- cleaner hair/beard geometry;
- readable clothing layers;
- richer material setup;
- predictable full-body camera previews;
- explicit rig sockets and named movable limbs.

Acceptance:

- preview PNG is visually acceptable before Unity import;
- GLB imports without errors;
- Unity prefab can use the same character contract;
- generated hierarchy names remain stable.

### Phase 2: add a local Blender inspection bridge

After Phase 1 produces a better model, test one bridge:

Preferred test order:

1. Flue Blender bridge if we want a thinner shell contract.
2. Blender MCP if we want richer live scene screenshots and MCP tool ergonomics.

Acceptance:

- Codex can inspect scene hierarchy;
- Codex can capture a Blender viewport screenshot;
- Codex can run a tiny harmless script such as printing object names;
- no external integrations are enabled;
- telemetry is disabled;
- no project files are modified until explicitly requested.

### Phase 3: create our own local skill

Create a local skill only after the workflow is good enough to repeat. Proposed name:

`chess-blender-character-pipeline`

The skill should include:

- character definition schema;
- approved Blender generation commands;
- visual quality checklist;
- rig naming contract;
- export/import validation;
- Unity prefab integration steps;
- forbidden operations and security rules.

### Phase 4: professional animation readiness

Once characters are clean:

- replace per-character bases with character-owned scale/contact helpers;
- keep chess base/piece identity as optional separate child object;
- add armature or modular animation contract;
- create idle/walk/jump/capture-hit placeholder clips;
- verify animations in Blender before Unity import;
- add Unity tests for animation routing and prefab contracts.

## Blocked or deferred paths

Do not prioritize now:

- Hyper3D/Rodin/Hunyuan paid generation;
- Sketchfab asset imports requiring tokens;
- marketplace assets with uncertain licenses;
- full photorealistic AAA character pipeline;
- unreviewed global MCP installation.

## Immediate next tasks

1. Improve the local Blender generator quality for `MathwiduPawn`.
2. Add a screenshot/preview comparison workflow so we can judge the model before Unity.
3. Audit and, only after approval, test either Flue or Blender MCP on a disposable Blender scene.
4. If the bridge is safe and useful, adapt its patterns into our own local skill instead of depending on third-party instructions long-term.
