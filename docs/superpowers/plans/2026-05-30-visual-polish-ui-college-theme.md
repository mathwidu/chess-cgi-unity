# Visual Polish, UI, and College Theme Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Turn the current playable chess prototype into a polished Unity chess game with premium-simple presentation, a subtle college/classroom theme, clearer UI, and better in-game readability for custom characters.

**Architecture:** Keep chess rules and game state unchanged. Add presentation systems around the existing scene: reusable scene-decor helpers, better lighting/camera/material configuration, and UI screens layered over the current `ChessGameController`. Visual polish must improve the game without blocking board raycasts or requiring expensive generated assets.

**Tech Stack:** Unity 6.3 LTS, URP, C#, Unity UI, Unity Test Runner EditMode, Unity MCP validation, Git.

---

## File Structure

- Modify `game/Assets/Scripts/View/BoardView.cs`: board construction, decorative board base, optional board-side labels.
- Modify `game/Assets/Scripts/View/PieceFactory.cs`: custom-piece presentation height and team-base polish.
- Modify `game/Assets/Scripts/Controllers/CameraController.cs`: camera distance, FOV-friendly framing, and turn perspective.
- Modify `game/Assets/Scripts/Controllers/ChessGameController.cs`: expose game-start state hooks and UI actions when needed.
- Modify `game/Assets/Scripts/UI/GameHud.cs`: improve HUD layout, status display, history display, menu/start-game interactions.
- Create `game/Assets/Scripts/View/ScenePolish.cs`: one component responsible for lighting, table/classroom props, background, and render setup created from primitives.
- Create `game/Assets/Tests/EditMode/ScenePolishTests.cs`: verify polish objects are created with stable names and do not collide with board interaction.
- Modify `game/Assets/Tests/EditMode/BoardViewTests.cs`: verify board decorative geometry is separated from square geometry.
- Modify `game/Assets/Tests/EditMode/CameraControllerTests.cs`: verify camera framing remains symmetric for both sides.
- Modify `game/Assets/Tests/EditMode/ChessGameControllerTests.cs`: verify UI-facing state remains correct after new game, moves, and promotion.
- Modify `game/Assets/Scenes/Main.unity`: save the polished scene with lighting, props, camera, board, custom pieces, and UI wiring.
- Modify `README.md`: document the polished scene, UI flow, controls, and expected test count.

---

## Phase 0: Baseline and Guardrails

### Task 0.1: Capture Baseline Visual State

**Files:**
- No code changes.

- [ ] **Step 1: Capture camera screenshot**

Run through Unity MCP:

```csharp
Camera camera = Camera.main ?? Object.FindFirstObjectByType<Camera>();
result.Log("BASELINE_CAMERA {0} position={1} rotation={2} fov={3}", camera.name, camera.transform.position, camera.transform.rotation.eulerAngles, camera.fieldOfView);
```

Expected: camera exists and captures the current board before polish.

- [ ] **Step 2: Record known visual issues**

Record these in the implementation notes:

```text
- Background is a plain gray/skybox environment.
- Board is flat and has no table/base context.
- Only one directional light is present.
- Custom characters look smaller and less detailed than generated concept renders.
- HUD is functional but not yet presentation-quality.
```

### Task 0.2: Keep Rules Untouched

**Files:**
- Test: `game/Assets/Tests/EditMode/ChessRulesAdapterTests.cs`
- Test: `game/Assets/Tests/EditMode/ChessGameControllerTests.cs`

- [ ] **Step 1: Run current EditMode suite**

Run: Unity Test Runner EditMode all tests.

Expected: all existing tests pass before visual work.

- [ ] **Step 2: Treat any rules regression as blocker**

Expected: visual work must not modify `ChessRulesAdapter.cs` unless a later plan explicitly targets rules.

---

## Phase 1: Premium-Simple Scene Polish

### Task 1.1: Add ScenePolish Component

**Files:**
- Create: `game/Assets/Scripts/View/ScenePolish.cs`
- Test: `game/Assets/Tests/EditMode/ScenePolishTests.cs`

- [ ] **Step 1: Write failing test for stable polish roots**

Create an EditMode test that instantiates `ScenePolish`, calls `ApplyPolish()`, and expects root children named:

```csharp
CollegeTheme
LightingRig
```

Expected before implementation: compile failure because `ScenePolish` does not exist.

- [ ] **Step 2: Implement minimal ScenePolish**

`ScenePolish.ApplyPolish()` must:

```text
- Create or reuse `CollegeTheme`.
- Create or reuse `LightingRig`.
- Avoid duplicate objects if called twice.
- Use generated primitive objects only.
```

- [ ] **Step 3: Verify idempotency**

Call `ApplyPolish()` twice in the test.

Expected: there is still exactly one `CollegeTheme` and one `LightingRig`.

### Task 1.2: Improve Lighting

**Files:**
- Modify: `game/Assets/Scripts/View/ScenePolish.cs`
- Modify scene: `game/Assets/Scenes/Main.unity`

- [ ] **Step 1: Add lighting test**

Extend `ScenePolishTests` to assert `LightingRig` creates:

```text
Key Light
Fill Light
Rim Light
```

Expected: all three lights exist after `ApplyPolish()`.

- [ ] **Step 2: Implement lighting rig**

Use these starting values:

```text
Key Light: Directional, intensity 1.45, warm white, rotation (45, -35, 0), soft shadows.
Fill Light: Point, intensity 95, range 13, cool soft color, position (-5, 5, -4), no harsh shadows.
Rim Light: Point, intensity 60, range 10, cool color, position (4, 4.5, 5), no harsh shadows.
```

- [ ] **Step 3: Validate in Unity**

Run a Unity MCP command to count lights.

Expected: at least three named polish lights exist and no duplicate polish lights exist after saving/reopening.

### Task 1.3: Improve Board Materials and Base

**Files:**
- Modify: `game/Assets/Scripts/View/BoardView.cs`
- Modify: `game/Assets/Materials/Board_Light.mat`
- Modify: `game/Assets/Materials/Board_Dark.mat`
- Modify: `game/Assets/Materials/Board_Highlight.mat`
- Test: `game/Assets/Tests/EditMode/BoardViewTests.cs`

- [ ] **Step 1: Write test for decorative board base**

Test `BuildBoard()` creates a child root named `BoardFrame` and still creates exactly `64` `SquareView` components.

Expected before implementation: fail because `BoardFrame` does not exist.

- [ ] **Step 2: Implement board base**

`BoardView.BuildBoard()` must create:

```text
BoardFrame
BoardFrame/BoardBase
BoardFrame/OuterRim
```

The decorative objects must not have `SquareView`.

- [ ] **Step 3: Verify interaction safety**

Ensure board decorative geometry either has no collider or is positioned below squares enough that square raycasts remain the primary interaction target.

Expected: existing input tests still pass.

### Task 1.4: Improve Camera Framing

**Files:**
- Modify: `game/Assets/Scripts/Controllers/CameraController.cs`
- Test: `game/Assets/Tests/EditMode/CameraControllerTests.cs`

- [ ] **Step 1: Add test for premium camera distance**

Assert white and black perspectives remain symmetric and use the same height.

Expected: positions are mirrored on Z and height remains equal.

- [ ] **Step 2: Tune camera values**

Use these starting values:

```text
turnPerspectiveDistance: 11.2
turnPerspectiveHeight: 8.4
fieldOfView in scene: 42
target: (0, 0, 0.35)
```

Expected: pieces occupy more screen space without hiding the full board.

---

## Phase 2: Subtle College Theme

### Task 2.1: Add Table and Classroom Props

**Files:**
- Modify: `game/Assets/Scripts/View/ScenePolish.cs`
- Test: `game/Assets/Tests/EditMode/ScenePolishTests.cs`

- [ ] **Step 1: Write test for college-theme props**

Assert `CollegeTheme` contains:

```text
Table
BackWall
Whiteboard
Notebook
Books
```

- [ ] **Step 2: Implement props with primitives**

Use simple cubes and planes:

```text
Table: below board, large wood material.
BackWall: behind board, muted wall color.
Whiteboard: behind board, no collider.
Notebook: side prop, no collider.
Books: side prop, no collider.
```

- [ ] **Step 3: Keep board clear**

Props must not overlap board bounds from `x=-5.2..5.2`, `z=-5.2..5.2` except table below the board.

Expected: board remains visually central.

### Task 2.2: Add CGI Identity Without Text Overload

**Files:**
- Modify: `game/Assets/Scripts/View/ScenePolish.cs`
- Modify: `README.md`

- [ ] **Step 1: Add a small whiteboard mark**

Add a small object or UI label named `CGIWhiteboardMark`.

Expected: it is subtle and does not become the main visual focus.

- [ ] **Step 2: Document the theme**

Update README with:

```text
Visual theme: a subtle CGI classroom/table setting, using primitives so the project remains lightweight and editable.
```

---

## Phase 3: UI and HUD Upgrade

### Task 3.1: Add Start Screen State

**Files:**
- Modify: `game/Assets/Scripts/UI/GameHud.cs`
- Modify: `game/Assets/Scripts/Controllers/ChessGameController.cs`
- Test: `game/Assets/Tests/EditMode/ChessGameControllerTests.cs`

- [ ] **Step 1: Add controller action for starting/resetting**

Expose a public method:

```csharp
public void StartLocalGame()
```

It should call `NewGame()` and allow UI buttons to start a match.

- [ ] **Step 2: Add HUD start panel**

`GameHud` must show:

```text
Title: Xadrez CGI
Primary action: Jogar
Secondary action: Como jogar
```

- [ ] **Step 3: Validate UI actions**

Test that `StartLocalGame()` resets move history and sets the status to `Turno: Brancas`.

### Task 3.2: Improve In-Game HUD

**Files:**
- Modify: `game/Assets/Scripts/UI/GameHud.cs`

- [ ] **Step 1: Add HUD structure**

Create named UI roots:

```text
TopBar
TurnBadge
StatusText
MoveHistoryPanel
ActionBar
PromotionPanel
```

- [ ] **Step 2: Improve text content**

Display:

```text
Turno atual
Status da partida
Historico recente
Nova partida
Cancelar selecao
Promocao
```

- [ ] **Step 3: Validate manually**

Play scene and verify the HUD updates after:

```text
e2-e4
e7-e5
selection cancel
new game
```

---

## Phase 4: Custom Character Readability

### Task 4.1: Improve Custom Piece Presentation

**Files:**
- Modify: `game/Assets/Scripts/View/PieceFactory.cs`
- Test: `game/Assets/Tests/EditMode/PieceFactoryTests.cs`

- [ ] **Step 1: Add tests for custom heights by piece type**

Verify:

```text
Pawn height: 1.15
Bishop/Knight/Rook height: 1.31
Queen/King height: 1.43
```

- [ ] **Step 2: Tune team base**

Use team-base material and a subtle base height so custom character feet do not visually sink into the board.

- [ ] **Step 3: Validate scene**

Unity MCP validation must report:

```text
customPawns=16
customBishops=4
```

### Task 4.2: Avoid Unnecessary Asset Regeneration

**Files:**
- Modify: `README.md`

- [ ] **Step 1: Document asset rule**

Add:

```text
Generated character meshes should only be regenerated when the silhouette or identity is wrong. Lighting, camera, scale, and material polish should be tried first.
```

---

## Phase 5: UX and Game Feel

### Task 5.1: Improve Move Feedback

**Files:**
- Modify: `game/Assets/Scripts/View/PieceView.cs`
- Test: existing controller tests continue passing.

- [ ] **Step 1: Add movement arc**

Modify `MoveTo()` so movement lifts slightly at midpoint:

```text
arcHeight: 0.18
position = lerp(start, target, eased) + Vector3.up * sin(t * PI) * arcHeight
```

- [ ] **Step 2: Validate no final-position drift**

At the end of animation, final position must be exactly target.

### Task 5.2: Improve Selection and Legal-Move Highlight

**Files:**
- Modify: `game/Assets/Scripts/View/PieceView.cs`
- Modify: `game/Assets/Scripts/View/BoardView.cs`

- [ ] **Step 1: Keep selected scale stable**

Selected pieces should scale up mildly without causing overlaps:

```text
selected scale multiplier: 1.07
```

- [ ] **Step 2: Use softer highlight**

Legal move highlights should be flatter, translucent-looking if possible, and visually distinct from board squares.

---

## Phase 6: Delivery Readiness

### Task 6.1: Update Documentation

**Files:**
- Modify: `README.md`
- Modify or create: `docs/report/`

- [ ] **Step 1: Update README**

README must describe:

```text
How to open
How to play
Controls
Automated tests
Custom characters
Visual theme
Build generation
```

### Task 6.2: Final Verification

**Files:**
- No code changes unless verification finds issues.

- [ ] **Step 1: Run EditMode tests**

Expected: all tests pass.

- [ ] **Step 2: Validate scene through Unity MCP**

Expected:

```text
pieces=32
customPawns=16
customBishops=4
named polish roots exist
camera exists
lights exist
```

- [ ] **Step 3: Generate macOS build**

Run the existing build path:

```text
Chess CGI > Build > macOS
```

Expected: `Builds/macOS/XadrezCGI.app`.

- [ ] **Step 4: Commit final delivery changes**

Commit message:

```bash
git commit -m "chore: prepare polished delivery build"
```
