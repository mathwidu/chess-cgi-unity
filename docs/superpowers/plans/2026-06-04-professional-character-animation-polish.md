# Professional Character Animation Polish Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Evolve the stable playable chess delivery into a more professional 3D character chess game with polished custom pieces, a richer selected-piece sidebar, procedural walking movement, and capture animation architecture ready for character-specific attacks.

**Architecture:** Keep chess rules isolated in `ChessRulesAdapter`; all polish lives in the presentation layer. Add small focused runtime classes for character metadata, selected-piece preview control, movement animation, capture resolution, capture animation styles, and visual QA. Preserve the stable tag `entrega-v1-estavel` as the fallback delivery while implementing improvements on `feature/animated-pieces-and-sidebar`.

**Tech Stack:** Unity 6.3 LTS (`6000.3.16f1`), C#, UGUI Canvas, Unity Input System, Unity Test Framework EditMode/PlayMode, URP materials/lights, existing `ChessDotNet` rules adapter.

---

## Baseline And Safety

Current stable checkpoint:

```bash
git show --stat --oneline entrega-v1-estavel
```

Expected: tag points to commit `a962034 feat: stable playable custom chess delivery`.

Work branch:

```bash
git branch --show-current
```

Expected: `feature/animated-pieces-and-sidebar`.

Do not change the stable tag. Every task below should be committed separately. If a task goes wrong, return to the last good commit on this branch; if the whole improvement branch becomes risky, deliver `entrega-v1-estavel`.

Test command used throughout the plan:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -quit \
  -projectPath game \
  -runTests \
  -testPlatform EditMode \
  -testResults TestResults/editmode-professional-polish.xml \
  -logFile Logs/editmode-professional-polish.log
```

Expected: Unity exits with code `0`; the XML shows all EditMode tests passing. If Unity says another instance has the project open, close the Unity Editor and retry.

Final build command:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -quit \
  -projectPath game \
  -executeMethod ChessCgiBuild.BuildMacOS \
  -logFile Logs/build-macos-professional-polish.log
```

Expected: `Builds/macOS/XadrezCGI.app` is rebuilt without errors.

## File Structure Map

Create these runtime files:

- `game/Assets/Scripts/Domain/CharacterProfile.cs`: immutable character metadata model.
- `game/Assets/Scripts/Domain/CharacterProfileCatalog.cs`: maps `ChessPieceKind` to metadata shown in HUD.
- `game/Assets/Scripts/UI/SelectedPiecePreviewController.cs`: owns selected-piece RenderTexture preview, orbit rotation, zoom, lighting, clone fitting.
- `game/Assets/Scripts/UI/SelectedPiecePreviewInput.cs`: UGUI drag/scroll adapter for rotating and zooming the preview.
- `game/Assets/Scripts/View/PieceVisualQuality.cs`: helper methods for bounds, renderer count, materials, scale, and display quality checks.
- `game/Assets/Scripts/View/PieceMotionSettings.cs`: movement/capture timing values.
- `game/Assets/Scripts/View/PieceMotionController.cs`: orchestrates walk, lunge, reaction, capture vanish, and final sync callback.
- `game/Assets/Scripts/View/CaptureResolver.cs`: determines which visual piece is being captured before board sync.
- `game/Assets/Scripts/View/CaptureAnimationStyle.cs`: data model for per-piece capture personality.
- `game/Assets/Scripts/View/CaptureAnimationLibrary.cs`: style lookup by attacking `ChessPieceKind`.
- `game/Assets/Scripts/View/ImpactEffect.cs`: lightweight visual impact flash/shake particle root.

Modify these runtime files:

- `game/Assets/Scripts/UI/GameHud.cs`: stop owning low-level preview code directly; consume metadata catalog and preview controller.
- `game/Assets/Scripts/View/PieceView.cs`: expose visual root, facing helpers, procedural walk pose, hit reaction helpers.
- `game/Assets/Scripts/View/PieceFactory.cs`: tag custom visual roots consistently and apply a polished visual wrapper.
- `game/Assets/Scripts/View/BoardView.cs`: expose `FindPieceAt(BoardSquare square)` and `TryGetPieceAt`.
- `game/Assets/Scripts/Controllers/ChessGameController.cs`: call motion/capture orchestration instead of a simple arc move.
- `game/Assets/Scripts/Controllers/CameraController.cs`: expose a small shake method for capture feedback.
- `game/Assets/Scripts/View/ScenePolish.cs`: improve environment material, props, and light consistency after animation integration.
- `README.md`: update feature list, controls, test commands, and delivery notes.
- `docs/design/capture-animation-roadmap.md`: update from future idea to implemented architecture.

Create/modify tests:

- `game/Assets/Tests/EditMode/CharacterProfileCatalogTests.cs`
- `game/Assets/Tests/EditMode/SelectedPiecePreviewControllerTests.cs`
- `game/Assets/Tests/EditMode/SelectedPiecePreviewInputTests.cs`
- `game/Assets/Tests/EditMode/PieceVisualQualityTests.cs`
- `game/Assets/Tests/EditMode/PieceMotionControllerTests.cs`
- `game/Assets/Tests/EditMode/CaptureResolverTests.cs`
- `game/Assets/Tests/EditMode/CaptureAnimationLibraryTests.cs`
- `game/Assets/Tests/EditMode/ImpactEffectTests.cs`
- Modify existing `GameHudTests.cs`, `PieceFactoryTests.cs`, `PieceViewTests.cs`, `ChessGameControllerTests.cs`, `ScenePolishTests.cs`, `CameraControllerTests.cs`.

If PlayMode coverage is added, create:

- `game/Assets/Tests/PlayMode/ChessCgi.PlayModeTests.asmdef`
- `game/Assets/Tests/PlayMode/MovementAndCaptureFlowTests.cs`

## Implementation Tasks

### Task 1: Protect The Stable Delivery And Document The Improvement Branch

**Goal:** Make the stable fallback explicit before feature work continues.

**Files:**
- Modify: `README.md`
- Create: `docs/design/professional-polish-scope.md`

- [ ] **Step 1: Verify branch and tag**

Run:

```bash
git branch --show-current
git tag --list "entrega-v1-estavel" -n
git show --oneline --no-patch entrega-v1-estavel
```

Expected:

```text
feature/animated-pieces-and-sidebar
entrega-v1-estavel Versao estavel entregavel do Xadrez CGI
a962034 feat: stable playable custom chess delivery
```

- [ ] **Step 2: Write scope doc**

Create `docs/design/professional-polish-scope.md` with this exact structure:

```markdown
# Professional Polish Scope

## Stable Fallback

The stable delivery is the git tag `entrega-v1-estavel`. It contains a playable local 3D chess game with all custom characters connected.

## Improvement Branch

All professional polish work happens on `feature/animated-pieces-and-sidebar`.

## Target Improvements

- Sidebar shows full character information and an interactive 3D preview.
- Custom pieces receive a visual QA pass for scale, materials, orientation, readability, and consistency.
- Movement changes from simple arc movement to procedural walking/stepping.
- Captures receive a generic dramatic animation, then per-piece personality styles.
- The architecture stays safe: chess rules remain isolated from visuals.

## Deferred Unless Time Allows

- Online multiplayer.
- AI opponent.
- Fully rigged humanoid animation clips for every character.
- Character-specific attack clips for every possible attacker/captured pair.
```

- [ ] **Step 3: Add README checkpoint note**

Add a short section after `## Build jogavel`:

```markdown
## Versao estavel

A versao entregavel atual esta marcada com a tag `entrega-v1-estavel`.
As melhorias de animacao, preview 3D e polimento visual devem ser feitas em branches separadas para manter a entrega segura.
```

- [ ] **Step 4: Run docs-only sanity check**

Run:

```bash
git diff --check
```

Expected: no output.

- [ ] **Step 5: Commit**

Run:

```bash
git add README.md docs/design/professional-polish-scope.md
git commit -m "docs: scope professional polish phase"
```

Expected: one commit containing only docs.

### Task 2: Add Character Metadata Catalog

**Goal:** Replace hard-coded display names in `GameHud` with reusable character metadata.

**Files:**
- Create: `game/Assets/Scripts/Domain/CharacterProfile.cs`
- Create: `game/Assets/Scripts/Domain/CharacterProfileCatalog.cs`
- Create: `game/Assets/Tests/EditMode/CharacterProfileCatalogTests.cs`
- Modify: `game/Assets/Scripts/UI/GameHud.cs`

- [ ] **Step 1: Write failing catalog tests**

Create `game/Assets/Tests/EditMode/CharacterProfileCatalogTests.cs`:

```csharp
using NUnit.Framework;

public class CharacterProfileCatalogTests
{
    [Test]
    public void GetProfile_ReturnsKnownCharacterForEachChessKind()
    {
        Assert.AreEqual("Mathwidu", CharacterProfileCatalog.GetProfile(ChessPieceKind.Pawn).DisplayName);
        Assert.AreEqual("Alex", CharacterProfileCatalog.GetProfile(ChessPieceKind.Rook).DisplayName);
        Assert.AreEqual("Gustavo", CharacterProfileCatalog.GetProfile(ChessPieceKind.Knight).DisplayName);
        Assert.AreEqual("Rafael", CharacterProfileCatalog.GetProfile(ChessPieceKind.Bishop).DisplayName);
        Assert.AreEqual("Marta", CharacterProfileCatalog.GetProfile(ChessPieceKind.Queen).DisplayName);
        Assert.AreEqual("Ricardo Carioca", CharacterProfileCatalog.GetProfile(ChessPieceKind.King).DisplayName);
    }

    [Test]
    public void GetProfile_UsesSafePrivateDataDefaults()
    {
        CharacterProfile pawn = CharacterProfileCatalog.GetProfile(ChessPieceKind.Pawn);
        CharacterProfile queen = CharacterProfileCatalog.GetProfile(ChessPieceKind.Queen);

        Assert.AreEqual("Aluno", pawn.Category);
        Assert.AreEqual("Professor", queen.Category);
        Assert.AreEqual("Matricula nao informada", pawn.Registration);
        Assert.AreEqual("Professor", queen.Registration);
    }

    [Test]
    public void GetProfile_ContainsSidebarReadyText()
    {
        CharacterProfile rook = CharacterProfileCatalog.GetProfile(ChessPieceKind.Rook);

        Assert.AreEqual("Torre", rook.PieceName);
        StringAssert.Contains("torre", rook.Description.ToLowerInvariant());
        Assert.IsFalse(string.IsNullOrWhiteSpace(rook.FullName));
    }
}
```

Run only this test:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -quit -projectPath game \
  -runTests -testPlatform EditMode \
  -testResults TestResults/editmode-character-profile-fail.xml \
  -logFile Logs/editmode-character-profile-fail.log \
  -testFilter CharacterProfileCatalogTests
```

Expected: compile fails because `CharacterProfile` and `CharacterProfileCatalog` do not exist.

- [ ] **Step 2: Implement metadata model**

Create `game/Assets/Scripts/Domain/CharacterProfile.cs`:

```csharp
public readonly struct CharacterProfile
{
    public ChessPieceKind Kind { get; }
    public string PieceName { get; }
    public string DisplayName { get; }
    public string FullName { get; }
    public string Category { get; }
    public string Registration { get; }
    public string Description { get; }

    public CharacterProfile(
        ChessPieceKind kind,
        string pieceName,
        string displayName,
        string fullName,
        string category,
        string registration,
        string description)
    {
        Kind = kind;
        PieceName = pieceName;
        DisplayName = displayName;
        FullName = fullName;
        Category = category;
        Registration = registration;
        Description = description;
    }
}
```

- [ ] **Step 3: Implement catalog**

Create `game/Assets/Scripts/Domain/CharacterProfileCatalog.cs`:

```csharp
public static class CharacterProfileCatalog
{
    private const string UnknownRegistration = "Matricula nao informada";

    public static CharacterProfile GetProfile(ChessPieceKind kind)
    {
        switch (kind)
        {
            case ChessPieceKind.Pawn:
                return new CharacterProfile(kind, "Peao", "Mathwidu", "Mathwidu", "Aluno", UnknownRegistration, "Peao personalizado ruivo, usado como base visual do exercito.");
            case ChessPieceKind.Rook:
                return new CharacterProfile(kind, "Torre", "Alex", "Alex", "Aluno", UnknownRegistration, "Aluno representado sentado em uma pequena torre de xadrez.");
            case ChessPieceKind.Knight:
                return new CharacterProfile(kind, "Cavalo", "Gustavo", "Gustavo", "Aluno", UnknownRegistration, "Aluno representado em um cavalo pequeno de xadrez.");
            case ChessPieceKind.Bishop:
                return new CharacterProfile(kind, "Bispo", "Rafael", "Rafael", "Aluno", UnknownRegistration, "Aluno usado como bispo personalizado da turma.");
            case ChessPieceKind.Queen:
                return new CharacterProfile(kind, "Rainha", "Marta", "Professora Marta", "Professor", "Professor", "Professora representada como rainha, com scarf azul e branco.");
            case ChessPieceKind.King:
                return new CharacterProfile(kind, "Rei", "Ricardo Carioca", "Professor Ricardo Carioca", "Professor", "Professor", "Professor representado como rei, com moletom azul e postura de docente.");
            default:
                return new CharacterProfile(kind, "Peca", "Peca classica", "Peca classica", "Sistema", "Nao aplicavel", "Peca classica de fallback.");
        }
    }
}
```

Important: do not invent registration numbers. Replace `Matricula nao informada` only after the user confirms each value and confirms that it can be committed.

- [ ] **Step 4: Update GameHud name lookup**

Replace `GetPieceModelName(PieceView piece)` in `game/Assets/Scripts/UI/GameHud.cs` with:

```csharp
private static string GetPieceModelName(PieceView piece)
{
    return CharacterProfileCatalog.GetProfile(piece.Kind).DisplayName;
}
```

- [ ] **Step 5: Run catalog tests**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -quit -projectPath game \
  -runTests -testPlatform EditMode \
  -testResults TestResults/editmode-character-profile.xml \
  -logFile Logs/editmode-character-profile.log \
  -testFilter CharacterProfileCatalogTests
```

Expected: all `CharacterProfileCatalogTests` pass.

- [ ] **Step 6: Run full EditMode suite**

Run the standard EditMode command from the baseline section.

Expected: all tests pass.

- [ ] **Step 7: Commit**

Run:

```bash
git add game/Assets/Scripts/Domain game/Assets/Scripts/UI/GameHud.cs game/Assets/Tests/EditMode/CharacterProfileCatalogTests.cs
git commit -m "feat: add character profile catalog"
```

### Task 3: Upgrade Selected Piece Sidebar Information

**Goal:** The selected-piece panel should show full profile details, not only name/kind/square.

**Files:**
- Modify: `game/Assets/Scripts/UI/GameHud.cs`
- Modify: `game/Assets/Tests/EditMode/GameHudTests.cs`

- [ ] **Step 1: Write failing HUD metadata test**

Add this test to `GameHudTests.cs`:

```csharp
[Test]
public void RefreshInterface_ShowsCharacterProfileMetadata()
{
    GameObject rig = CreatePlayableRig(out BoardView boardView, out ChessGameController controller, out GameHud hud);
    try
    {
        controller.NewGame();
        PieceView queen = boardView.Pieces.First(piece =>
            piece.Kind == ChessPieceKind.Queen &&
            piece.Side == ChessSide.White &&
            piece.Square.Equals(BoardSquare.FromAlgebraic("d1")));

        controller.SelectPiece(queen);
        hud.RefreshInterface();

        StringAssert.Contains("Professora Marta", FindText(hud.transform, "HudRoot/SelectedPiecePanel/SelectedPieceFullNameText").text);
        StringAssert.Contains("Professor", FindText(hud.transform, "HudRoot/SelectedPiecePanel/SelectedPieceRoleText").text);
        StringAssert.Contains("Professor", FindText(hud.transform, "HudRoot/SelectedPiecePanel/SelectedPieceRegistrationText").text);
        StringAssert.Contains("scarf", FindText(hud.transform, "HudRoot/SelectedPiecePanel/SelectedPieceDescriptionText").text.ToLowerInvariant());
    }
    finally
    {
        Object.DestroyImmediate(rig);
    }
}
```

Run:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -quit -projectPath game \
  -runTests -testPlatform EditMode \
  -testResults TestResults/editmode-sidebar-metadata-fail.xml \
  -logFile Logs/editmode-sidebar-metadata-fail.log \
  -testFilter GameHudTests.RefreshInterface_ShowsCharacterProfileMetadata
```

Expected: test fails because the new text fields do not exist.

- [ ] **Step 2: Add new HUD text fields**

In `GameHud.cs`, add private fields:

```csharp
private Text selectedPieceFullNameText;
private Text selectedPieceRoleText;
private Text selectedPieceRegistrationText;
private Text selectedPieceDescriptionText;
```

- [ ] **Step 3: Resize selected panel**

Change the `SelectedPiecePanel` creation from:

```csharp
new Vector2(304f, 348f)
```

to:

```csharp
new Vector2(360f, 520f)
```

Change the selected preview size from `new Vector2(272f, 174f)` to `new Vector2(328f, 230f)`.

- [ ] **Step 4: Create metadata text fields**

Below `selectedPieceSideText` creation, add:

```csharp
selectedPieceFullNameText = CreateText("SelectedPieceFullNameText", selectedPiecePanel, "-", 13, FontStyle.Bold, textColor, TextAnchor.UpperLeft, new Vector2(16f, -346f), new Vector2(328f, 22f));
selectedPieceRoleText = CreateText("SelectedPieceRoleText", selectedPiecePanel, "-", 13, FontStyle.Normal, mutedTextColor, TextAnchor.UpperLeft, new Vector2(16f, -372f), new Vector2(328f, 22f));
selectedPieceRegistrationText = CreateText("SelectedPieceRegistrationText", selectedPiecePanel, "-", 13, FontStyle.Normal, mutedTextColor, TextAnchor.UpperLeft, new Vector2(16f, -398f), new Vector2(328f, 22f));
selectedPieceDescriptionText = CreateText("SelectedPieceDescriptionText", selectedPiecePanel, "-", 12, FontStyle.Normal, textColor, TextAnchor.UpperLeft, new Vector2(16f, -430f), new Vector2(328f, 70f));
```

Also adjust the existing name/kind/square/side fields so they start after the larger preview:

```csharp
selectedPieceNameText = CreateText("SelectedPieceNameText", selectedPiecePanel, "-", 22, FontStyle.Bold, textColor, TextAnchor.UpperLeft, new Vector2(16f, -286f), new Vector2(328f, 30f));
selectedPieceKindText = CreateText("SelectedPieceKindText", selectedPiecePanel, "-", 15, FontStyle.Bold, accentColor, TextAnchor.UpperLeft, new Vector2(16f, -318f), new Vector2(328f, 22f));
selectedPieceSquareText = CreateText("SelectedPieceSquareText", selectedPiecePanel, "-", 13, FontStyle.Normal, mutedTextColor, TextAnchor.UpperLeft, new Vector2(16f, -346f), new Vector2(156f, 22f));
selectedPieceSideText = CreateText("SelectedPieceSideText", selectedPiecePanel, "-", 13, FontStyle.Normal, mutedTextColor, TextAnchor.UpperRight, new Vector2(188f, -346f), new Vector2(156f, 22f));
```

If fields overlap, keep the larger panel and move profile fields down by 24 px increments until no text overlaps at 1920x1080.

- [ ] **Step 5: Populate metadata in RefreshSelectedPiecePanel**

Inside `RefreshSelectedPiecePanel`, after `bool hasSelection`, add:

```csharp
CharacterProfile profile = CharacterProfileCatalog.GetProfile(selectedPiece.Kind);
```

Use `profile`:

```csharp
selectedPieceNameText.text = profile.DisplayName;
selectedPieceKindText.text = $"{profile.PieceName} {SideAdjective(selectedPiece.Side)}";
selectedPieceFullNameText.text = $"Nome: {profile.FullName}";
selectedPieceRoleText.text = $"Categoria: {profile.Category}";
selectedPieceRegistrationText.text = $"Registro: {profile.Registration}";
selectedPieceDescriptionText.text = profile.Description;
```

- [ ] **Step 6: Run HUD tests**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -quit -projectPath game \
  -runTests -testPlatform EditMode \
  -testResults TestResults/editmode-sidebar-metadata.xml \
  -logFile Logs/editmode-sidebar-metadata.log \
  -testFilter GameHudTests
```

Expected: all `GameHudTests` pass.

- [ ] **Step 7: Manual visual check in Unity**

Open Unity, run `Assets/Scenes/Main.unity`, click the white queen on `d1`.

Expected:

- Sidebar opens.
- Preview remains visible.
- Text shows `Marta`, `Professora Marta`, `Professor`, `Registro: Professor`, and a short description.
- Text does not overlap the move history panel at 1920x1080.

- [ ] **Step 8: Commit**

Run:

```bash
git add game/Assets/Scripts/UI/GameHud.cs game/Assets/Tests/EditMode/GameHudTests.cs
git commit -m "feat: enrich selected piece sidebar"
```

### Task 4: Extract Interactive Selected Piece Preview Controller

**Goal:** The preview should show the whole model and allow rotation/zoom without bloating `GameHud`.

**Files:**
- Create: `game/Assets/Scripts/UI/SelectedPiecePreviewController.cs`
- Create: `game/Assets/Scripts/UI/SelectedPiecePreviewInput.cs`
- Create: `game/Assets/Tests/EditMode/SelectedPiecePreviewControllerTests.cs`
- Create: `game/Assets/Tests/EditMode/SelectedPiecePreviewInputTests.cs`
- Modify: `game/Assets/Scripts/UI/GameHud.cs`

- [ ] **Step 1: Write failing preview controller tests**

Create `SelectedPiecePreviewControllerTests.cs`:

```csharp
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class SelectedPiecePreviewControllerTests
{
    [Test]
    public void ShowPiece_CreatesPreviewTextureAndClone()
    {
        GameObject owner = new GameObject("Preview Owner");
        GameObject source = GameObject.CreatePrimitive(PrimitiveType.Cube);
        try
        {
            RawImage image = owner.AddComponent<RawImage>();
            SelectedPiecePreviewController preview = owner.AddComponent<SelectedPiecePreviewController>();
            PieceView piece = source.AddComponent<PieceView>();
            piece.Initialize(new VisualPieceState(BoardSquare.FromAlgebraic("e2"), ChessSide.White, ChessPieceKind.Pawn));

            preview.Configure(image);
            preview.ShowPiece(piece);

            Assert.IsNotNull(image.texture);
            Assert.IsTrue(preview.HasPreview);
            Assert.Greater(preview.CurrentZoom, 0f);
        }
        finally
        {
            Object.DestroyImmediate(owner);
            Object.DestroyImmediate(source);
        }
    }

    [Test]
    public void RotateAndZoom_AreClampedToReadableValues()
    {
        GameObject owner = new GameObject("Preview Owner");
        try
        {
            SelectedPiecePreviewController preview = owner.AddComponent<SelectedPiecePreviewController>();

            preview.Rotate(45f);
            preview.Zoom(-100f);

            Assert.AreEqual(45f, preview.CurrentYaw, 0.001f);
            Assert.GreaterOrEqual(preview.CurrentZoom, 1.6f);

            preview.Zoom(100f);

            Assert.LessOrEqual(preview.CurrentZoom, 4.8f);
        }
        finally
        {
            Object.DestroyImmediate(owner);
        }
    }
}
```

Create `SelectedPiecePreviewInputTests.cs`:

```csharp
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;

public class SelectedPiecePreviewInputTests
{
    [Test]
    public void Configure_SendsDragAndScrollToPreviewController()
    {
        GameObject owner = new GameObject("Preview Input Test");
        try
        {
            SelectedPiecePreviewController controller = owner.AddComponent<SelectedPiecePreviewController>();
            SelectedPiecePreviewInput input = owner.AddComponent<SelectedPiecePreviewInput>();
            input.Configure(controller);

            PointerEventData drag = new PointerEventData(EventSystem.current) { delta = new Vector2(20f, 0f) };
            PointerEventData scroll = new PointerEventData(EventSystem.current) { scrollDelta = new Vector2(0f, 1f) };

            input.OnDrag(drag);
            input.OnScroll(scroll);

            Assert.Greater(controller.CurrentYaw, 0f);
            Assert.Greater(controller.CurrentZoom, 0f);
        }
        finally
        {
            Object.DestroyImmediate(owner);
        }
    }
}
```

Expected before implementation: compile fails because the new classes do not exist.

- [ ] **Step 2: Implement `SelectedPiecePreviewController`**

Create a MonoBehaviour with this public API:

```csharp
public sealed class SelectedPiecePreviewController : MonoBehaviour
{
    public bool HasPreview { get; private set; }
    public float CurrentYaw { get; private set; }
    public float CurrentZoom { get; private set; }

    public void Configure(RawImage targetImage);
    public void ShowPiece(PieceView selectedPiece);
    public void Clear();
    public void Rotate(float deltaYaw);
    public void Zoom(float deltaZoom);
    public void ResetView();
}
```

Implementation rules:

- Create one `RenderTexture(768, 512, 24)` with antiAliasing `4`.
- Create an offscreen stage at `(96, 96, 96)`.
- Clone the selected piece under the stage.
- Disable `PieceView` and all `Collider` components on the clone.
- Fit the full renderer bounds into view; the whole body must be visible.
- Clamp zoom to `1.6f` minimum and `4.8f` maximum.
- Default yaw should be `180f`, so the preview faces the player.
- `Clear()` must destroy the clone and release references.
- `OnDestroy()` must release the RenderTexture.

- [ ] **Step 3: Implement `SelectedPiecePreviewInput`**

Create a MonoBehaviour implementing:

```csharp
using UnityEngine;
using UnityEngine.EventSystems;

public sealed class SelectedPiecePreviewInput : MonoBehaviour, IDragHandler, IScrollHandler
{
    [SerializeField] private float dragSensitivity = 0.45f;
    [SerializeField] private float scrollSensitivity = 0.35f;

    private SelectedPiecePreviewController previewController;

    public void Configure(SelectedPiecePreviewController controller)
    {
        previewController = controller;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (previewController != null)
        {
            previewController.Rotate(eventData.delta.x * dragSensitivity);
        }
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (previewController != null)
        {
            previewController.Zoom(eventData.scrollDelta.y * scrollSensitivity);
        }
    }
}
```

- [ ] **Step 4: Move preview code out of `GameHud`**

In `GameHud.cs`:

- Remove fields: `selectedPiecePreviewTexture`, `selectedPiecePreviewCamera`, `selectedPiecePreviewLight`, `selectedPiecePreviewStage`, `selectedPiecePreviewClone`, `previewedPiece`.
- Add field: `private SelectedPiecePreviewController selectedPiecePreviewController;`
- When creating `SelectedPiecePreview`, add:

```csharp
selectedPiecePreviewImage.raycastTarget = true;
selectedPiecePreviewController = selectedPiecePreviewImage.gameObject.AddComponent<SelectedPiecePreviewController>();
selectedPiecePreviewController.Configure(selectedPiecePreviewImage);
SelectedPiecePreviewInput previewInput = selectedPiecePreviewImage.gameObject.AddComponent<SelectedPiecePreviewInput>();
previewInput.Configure(selectedPiecePreviewController);
```

- Replace `BuildSelectedPiecePreview(selectedPiece)` with:

```csharp
selectedPiecePreviewController.ShowPiece(selectedPiece);
```

- Replace `ClearSelectedPiecePreviewClone()` with:

```csharp
selectedPiecePreviewController.Clear();
```

- Remove old private methods that only handled preview clone/camera fitting.

- [ ] **Step 5: Run preview tests**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -quit -projectPath game \
  -runTests -testPlatform EditMode \
  -testResults TestResults/editmode-preview-controller.xml \
  -logFile Logs/editmode-preview-controller.log \
  -testFilter SelectedPiecePreview
```

Expected: preview controller/input tests pass.

- [ ] **Step 6: Run HUD tests**

Run `GameHudTests`. Expected: all pass.

- [ ] **Step 7: Manual visual check**

In Play Mode:

- Select any piece.
- Drag over the preview.
- Scroll over the preview.
- The preview rotates and zooms; the game board piece does not move.

- [ ] **Step 8: Commit**

Run:

```bash
git add game/Assets/Scripts/UI game/Assets/Tests/EditMode
git commit -m "feat: add interactive selected piece preview"
```

### Task 5: Add Custom Piece Visual Quality Audit

**Goal:** Give every generated character a measurable quality gate: bounds, renderer count, material count, scale, orientation, and full-body visibility.

**Files:**
- Create: `game/Assets/Scripts/View/PieceVisualQuality.cs`
- Create: `game/Assets/Tests/EditMode/PieceVisualQualityTests.cs`
- Modify: `docs/design/custom-piece-generation-workflow.md`

- [ ] **Step 1: Write failing visual quality tests**

Create `PieceVisualQualityTests.cs`:

```csharp
using NUnit.Framework;
using UnityEngine;

public class PieceVisualQualityTests
{
    [Test]
    public void Evaluate_CustomPrefabWithRendererPassesBasicBounds()
    {
        GameObject prefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
        try
        {
            PieceVisualQuality.Report report = PieceVisualQuality.Evaluate(prefab);

            Assert.IsTrue(report.HasRenderer);
            Assert.AreEqual(1, report.RendererCount);
            Assert.Greater(report.Bounds.size.y, 0f);
        }
        finally
        {
            Object.DestroyImmediate(prefab);
        }
    }

    [Test]
    public void Evaluate_EmptyPrefabReportsNoRenderer()
    {
        GameObject prefab = new GameObject("Empty Custom Piece");
        try
        {
            PieceVisualQuality.Report report = PieceVisualQuality.Evaluate(prefab);

            Assert.IsFalse(report.HasRenderer);
            Assert.AreEqual(0, report.RendererCount);
        }
        finally
        {
            Object.DestroyImmediate(prefab);
        }
    }
}
```

- [ ] **Step 2: Implement `PieceVisualQuality`**

Create:

```csharp
using UnityEngine;

public static class PieceVisualQuality
{
    public readonly struct Report
    {
        public bool HasRenderer { get; }
        public int RendererCount { get; }
        public int MaterialSlotCount { get; }
        public Bounds Bounds { get; }
        public bool IsReadableOnBoard { get; }

        public Report(bool hasRenderer, int rendererCount, int materialSlotCount, Bounds bounds, bool isReadableOnBoard)
        {
            HasRenderer = hasRenderer;
            RendererCount = rendererCount;
            MaterialSlotCount = materialSlotCount;
            Bounds = bounds;
            IsReadableOnBoard = isReadableOnBoard;
        }
    }

    public static Report Evaluate(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            return new Report(false, 0, 0, new Bounds(root.transform.position, Vector3.zero), false);
        }

        Bounds bounds = renderers[0].bounds;
        int materialSlots = 0;
        foreach (Renderer renderer in renderers)
        {
            bounds.Encapsulate(renderer.bounds);
            materialSlots += renderer.sharedMaterials.Length;
        }

        bool readable = bounds.size.y >= 0.7f && bounds.size.y <= 2.2f && bounds.size.x <= 2.2f && bounds.size.z <= 2.2f;
        return new Report(true, renderers.Length, materialSlots, bounds, readable);
    }
}
```

- [ ] **Step 3: Add workflow checklist**

In `custom-piece-generation-workflow.md`, add a section:

```markdown
## Checklist de polimento dentro do Unity

Antes de aceitar um novo prefab personalizado:

- O prefab tem pelo menos um Renderer.
- O modelo fica inteiro na sidebar com zoom padrao.
- A altura normalizada no tabuleiro fica entre 1.15 e 1.45 unidades.
- A peca olha para o adversario: brancas para frente, pretas rotacionadas 180 graus.
- A base nao cobre pernas ou props importantes.
- Materiais nao estouram em branco puro nem somem em preto puro.
- O personagem continua legivel na camera de jogo padrao.
```

- [ ] **Step 4: Run tests**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -quit -projectPath game \
  -runTests -testPlatform EditMode \
  -testResults TestResults/editmode-piece-visual-quality.xml \
  -logFile Logs/editmode-piece-visual-quality.log \
  -testFilter PieceVisualQualityTests
```

Expected: pass.

- [ ] **Step 5: Commit**

Run:

```bash
git add game/Assets/Scripts/View/PieceVisualQuality.cs game/Assets/Tests/EditMode/PieceVisualQualityTests.cs docs/design/custom-piece-generation-workflow.md
git commit -m "test: add custom piece visual quality audit"
```

### Task 6: Polish Piece Factory Visual Presentation

**Goal:** Make all custom characters read as deliberate chess pieces: consistent base, renderer shadows, selection scale, and fit.

**Files:**
- Modify: `game/Assets/Scripts/View/PieceFactory.cs`
- Modify: `game/Assets/Tests/EditMode/PieceFactoryTests.cs`

- [ ] **Step 1: Add tests for custom visual wrapper**

Add to `PieceFactoryTests.cs`:

```csharp
[Test]
public void CreatePiece_CustomVisualHasNamedVisualRootAndTeamBase()
{
    GameObject rig = new GameObject("Piece Factory Test Rig");
    GameObject prefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
    try
    {
        PieceFactory factory = rig.AddComponent<PieceFactory>();
        factory.ConfigureCustomPrefab(ChessPieceKind.Rook, prefab);

        PieceView piece = factory.CreatePiece(
            new VisualPieceState(BoardSquare.FromAlgebraic("a1"), ChessSide.White, ChessPieceKind.Rook),
            Vector3.zero,
            rig.transform);

        Assert.IsNotNull(piece.transform.Find("TeamBase"));
        Assert.IsNotNull(piece.transform.Find("CustomVisual"));
        Assert.IsNotNull(piece.VisualRoot);
        Assert.AreEqual("CustomVisual", piece.VisualRoot.name);
    }
    finally
    {
        Object.DestroyImmediate(rig);
        Object.DestroyImmediate(prefab);
    }
}
```

Expected failure: `PieceView.VisualRoot` does not exist.

- [ ] **Step 2: Expose visual root in `PieceView`**

Modify `PieceView.cs`:

```csharp
public Transform VisualRoot { get; private set; }

public void SetVisualRoot(Transform visualRoot)
{
    VisualRoot = visualRoot;
}
```

- [ ] **Step 3: Set visual root in `PieceFactory`**

After creating a custom visual:

```csharp
pieceView.SetVisualRoot(root.transform.Find("CustomVisual"));
```

Because `pieceView` is currently initialized after `BuildCustomShape`, change `BuildCustomShape` to return `Transform` instead of `bool`, or call:

```csharp
Transform customVisual = root.transform.Find("CustomVisual");
pieceView.SetVisualRoot(customVisual);
```

after `BuildCustomShape`.

For primitive fallback, set `VisualRoot` to the root transform:

```csharp
pieceView.SetVisualRoot(root.transform);
```

- [ ] **Step 4: Apply renderer quality settings**

Add helper in `PieceFactory`:

```csharp
private static void ConfigureRenderers(Transform root)
{
    Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
    foreach (Renderer renderer in renderers)
    {
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        renderer.receiveShadows = true;
    }
}
```

Call it after custom or primitive shape construction.

- [ ] **Step 5: Run PieceFactory tests**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -quit -projectPath game \
  -runTests -testPlatform EditMode \
  -testResults TestResults/editmode-piece-factory-polish.xml \
  -logFile Logs/editmode-piece-factory-polish.log \
  -testFilter PieceFactoryTests
```

Expected: pass.

- [ ] **Step 6: Manual visual check**

In Play Mode, confirm:

- Bases have consistent white/black team color.
- Custom characters stand above the base.
- Shadows are visible but not hiding faces.
- Black-side custom pieces face white-side pieces.

- [ ] **Step 7: Commit**

Run:

```bash
git add game/Assets/Scripts/View/PieceFactory.cs game/Assets/Scripts/View/PieceView.cs game/Assets/Tests/EditMode/PieceFactoryTests.cs
git commit -m "feat: polish custom piece visual wrapper"
```

### Task 7: Add Movement Animation Settings And Pure Pose Evaluation

**Goal:** Prepare movement animation without changing game flow yet.

**Files:**
- Create: `game/Assets/Scripts/View/PieceMotionSettings.cs`
- Modify: `game/Assets/Scripts/View/PieceView.cs`
- Modify: `game/Assets/Tests/EditMode/PieceViewTests.cs`

- [ ] **Step 1: Add tests for procedural pose evaluation**

Add to `PieceViewTests.cs`:

```csharp
[Test]
public void EvaluateWalkPose_StartMiddleEndKeepsBoardDestinationStable()
{
    Vector3 start = new Vector3(0f, 0.08f, 0f);
    Vector3 target = new Vector3(2f, 0.08f, 0f);
    PieceMotionSettings settings = PieceMotionSettings.Default;

    PieceView.WalkPose startPose = PieceView.EvaluateWalkPose(start, target, 0f, settings);
    PieceView.WalkPose middlePose = PieceView.EvaluateWalkPose(start, target, 0.5f, settings);
    PieceView.WalkPose endPose = PieceView.EvaluateWalkPose(start, target, 1f, settings);

    AssertVector(start, startPose.RootPosition);
    Assert.AreEqual(1f, middlePose.RootPosition.x, 0.01f);
    Assert.Greater(middlePose.VisualOffset.y, 0f);
    AssertVector(target, endPose.RootPosition);
    Assert.AreEqual(Vector3.zero, endPose.VisualOffset);
}
```

Expected: compile fails because `PieceMotionSettings` and `WalkPose` do not exist.

- [ ] **Step 2: Create settings**

Create `PieceMotionSettings.cs`:

```csharp
using UnityEngine;

[System.Serializable]
public readonly struct PieceMotionSettings
{
    public float WalkDuration { get; }
    public float StepHeight { get; }
    public float LeanAngle { get; }
    public float CaptureDuration { get; }

    public static PieceMotionSettings Default => new PieceMotionSettings(0.55f, 0.08f, 4.5f, 0.45f);

    public PieceMotionSettings(float walkDuration, float stepHeight, float leanAngle, float captureDuration)
    {
        WalkDuration = walkDuration;
        StepHeight = stepHeight;
        LeanAngle = leanAngle;
        CaptureDuration = captureDuration;
    }
}
```

- [ ] **Step 3: Add pure pose evaluation to `PieceView`**

Add nested struct:

```csharp
public readonly struct WalkPose
{
    public Vector3 RootPosition { get; }
    public Vector3 VisualOffset { get; }
    public Quaternion VisualRotation { get; }

    public WalkPose(Vector3 rootPosition, Vector3 visualOffset, Quaternion visualRotation)
    {
        RootPosition = rootPosition;
        VisualOffset = visualOffset;
        VisualRotation = visualRotation;
    }
}
```

Add method:

```csharp
public static WalkPose EvaluateWalkPose(Vector3 start, Vector3 target, float normalizedTime, PieceMotionSettings settings)
{
    float t = Mathf.Clamp01(normalizedTime);
    float eased = Mathf.SmoothStep(0f, 1f, t);
    Vector3 rootPosition = Vector3.Lerp(start, target, eased);
    float step = Mathf.Sin(t * Mathf.PI * 4f);
    Vector3 visualOffset = Vector3.up * Mathf.Abs(step) * settings.StepHeight;
    float lean = Mathf.Sin(t * Mathf.PI * 2f) * settings.LeanAngle;
    Quaternion visualRotation = Quaternion.Euler(lean, 0f, 0f);

    if (t >= 1f)
    {
        visualOffset = Vector3.zero;
        visualRotation = Quaternion.identity;
    }

    return new WalkPose(rootPosition, visualOffset, visualRotation);
}
```

- [ ] **Step 4: Run tests**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -quit -projectPath game \
  -runTests -testPlatform EditMode \
  -testResults TestResults/editmode-walk-pose.xml \
  -logFile Logs/editmode-walk-pose.log \
  -testFilter PieceViewTests
```

Expected: pass.

- [ ] **Step 5: Commit**

Run:

```bash
git add game/Assets/Scripts/View/PieceMotionSettings.cs game/Assets/Scripts/View/PieceView.cs game/Assets/Tests/EditMode/PieceViewTests.cs
git commit -m "feat: add procedural walk pose evaluation"
```

### Task 8: Implement Procedural Walk Movement

**Goal:** When a piece moves, it should travel with a walking/stepping feel instead of a floating arc.

**Files:**
- Modify: `game/Assets/Scripts/View/PieceView.cs`
- Create: `game/Assets/Scripts/View/PieceMotionController.cs`
- Create: `game/Assets/Tests/EditMode/PieceMotionControllerTests.cs`
- Modify: `game/Assets/Scripts/Controllers/ChessGameController.cs`

- [ ] **Step 1: Add movement controller tests**

Create `PieceMotionControllerTests.cs`:

```csharp
using NUnit.Framework;
using UnityEngine;

public class PieceMotionControllerTests
{
    [Test]
    public void CreateDefaultSettings_UsesReadableDurations()
    {
        PieceMotionSettings settings = PieceMotionSettings.Default;

        Assert.Greater(settings.WalkDuration, 0.35f);
        Assert.Less(settings.WalkDuration, 0.9f);
        Assert.Greater(settings.StepHeight, 0.02f);
        Assert.Less(settings.StepHeight, 0.18f);
    }

    [Test]
    public void MoveInstantlyForTests_PlacesPieceAtTarget()
    {
        GameObject owner = new GameObject("Motion Test");
        GameObject pieceObject = new GameObject("Piece");
        try
        {
            PieceView piece = pieceObject.AddComponent<PieceView>();
            PieceMotionController motion = owner.AddComponent<PieceMotionController>();
            Vector3 target = new Vector3(3f, 0.08f, 2f);

            motion.MoveInstant(piece, target);

            Assert.AreEqual(target.x, piece.transform.position.x, 0.001f);
            Assert.AreEqual(target.y, piece.transform.position.y, 0.001f);
            Assert.AreEqual(target.z, piece.transform.position.z, 0.001f);
        }
        finally
        {
            Object.DestroyImmediate(owner);
            Object.DestroyImmediate(pieceObject);
        }
    }
}
```

- [ ] **Step 2: Add `MoveWithWalk` to `PieceView`**

Replace old `MoveTo` body with a new wrapper that calls a new method:

```csharp
public IEnumerator MoveWithWalk(Vector3 target, PieceMotionSettings settings)
{
    Vector3 start = transform.position;
    float duration = Mathf.Max(0.001f, settings.WalkDuration);
    float elapsed = 0f;

    FaceTowards(target);

    while (elapsed < duration)
    {
        elapsed += Time.deltaTime;
        float normalized = Mathf.Clamp01(elapsed / duration);
        WalkPose pose = EvaluateWalkPose(start, target, normalized, settings);
        transform.position = pose.RootPosition;
        if (VisualRoot != null && VisualRoot != transform)
        {
            VisualRoot.localPosition += pose.VisualOffset;
            VisualRoot.localRotation = pose.VisualRotation;
        }
        yield return null;
    }

    transform.position = target;
    if (VisualRoot != null && VisualRoot != transform)
    {
        VisualRoot.localPosition = Vector3.zero;
        VisualRoot.localRotation = Quaternion.identity;
    }
}
```

Add:

```csharp
public void FaceTowards(Vector3 worldTarget)
{
    Vector3 direction = worldTarget - transform.position;
    direction.y = 0f;
    if (direction.sqrMagnitude > 0.001f)
    {
        transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
    }
}
```

Keep `MoveTo(Vector3 target, float duration)` as compatibility wrapper for existing tests:

```csharp
public IEnumerator MoveTo(Vector3 target, float duration)
{
    return MoveWithWalk(target, new PieceMotionSettings(duration, PieceMotionSettings.Default.StepHeight, PieceMotionSettings.Default.LeanAngle, PieceMotionSettings.Default.CaptureDuration));
}
```

- [ ] **Step 3: Implement `PieceMotionController`**

Create:

```csharp
using System.Collections;
using UnityEngine;

public sealed class PieceMotionController : MonoBehaviour
{
    [SerializeField] private float walkDuration = 0.55f;
    [SerializeField] private float stepHeight = 0.08f;
    [SerializeField] private float leanAngle = 4.5f;
    [SerializeField] private float captureDuration = 0.45f;

    public PieceMotionSettings Settings => new PieceMotionSettings(walkDuration, stepHeight, leanAngle, captureDuration);

    public IEnumerator MovePiece(PieceView piece, Vector3 target)
    {
        if (piece == null)
        {
            yield break;
        }

        yield return piece.MoveWithWalk(target, Settings);
    }

    public void MoveInstant(PieceView piece, Vector3 target)
    {
        if (piece != null)
        {
            piece.transform.position = target;
        }
    }
}
```

- [ ] **Step 4: Wire controller in `ChessGameController`**

Add field:

```csharp
[SerializeField] private PieceMotionController motionController;
```

Find it in `Awake()`:

```csharp
if (motionController == null)
{
    motionController = Object.FindFirstObjectByType<PieceMotionController>();
}
```

In `AnimateMoveThenSync`, replace:

```csharp
yield return movingPiece.MoveTo(boardView.GetPieceWorldPosition(destination), moveDuration);
```

with:

```csharp
Vector3 targetPosition = boardView.GetPieceWorldPosition(destination);
if (motionController != null)
{
    yield return motionController.MovePiece(movingPiece, targetPosition);
}
else
{
    yield return movingPiece.MoveTo(targetPosition, moveDuration);
}
```

- [ ] **Step 5: Add `PieceMotionController` to scene**

In Unity or via MCP, add `PieceMotionController` to `GameManager` in `Assets/Scenes/Main.unity`.

Expected: `ChessGameController.motionController` references the new component or can find it by type.

- [ ] **Step 6: Run tests**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -quit -projectPath game \
  -runTests -testPlatform EditMode \
  -testResults TestResults/editmode-piece-motion.xml \
  -logFile Logs/editmode-piece-motion.log \
  -testFilter PieceMotionControllerTests
```

Then run full EditMode.

- [ ] **Step 7: Manual animation check**

In Play Mode:

- Move `e2` to `e4`.
- The pawn should move through space with a stepping/bobbing feel.
- Input should be blocked while the piece moves.
- Camera should rotate only after move resolution.

- [ ] **Step 8: Commit**

Run:

```bash
git add game/Assets/Scripts/View game/Assets/Scripts/Controllers/ChessGameController.cs game/Assets/Tests/EditMode/PieceMotionControllerTests.cs game/Assets/Scenes/Main.unity
git commit -m "feat: animate piece movement with procedural walk"
```

### Task 9: Resolve Captured Piece Before Board Sync

**Goal:** Capture animations need access to the captured visual before `BoardView.SyncPieces` destroys/recreates pieces.

**Files:**
- Create: `game/Assets/Scripts/View/CaptureResolver.cs`
- Create: `game/Assets/Tests/EditMode/CaptureResolverTests.cs`
- Modify: `game/Assets/Scripts/View/BoardView.cs`

- [ ] **Step 1: Add board lookup methods**

In `BoardView.cs`, add:

```csharp
public PieceView FindPieceAt(BoardSquare square)
{
    foreach (PieceView piece in pieces)
    {
        if (piece.Square.Equals(square))
        {
            return piece;
        }
    }

    return null;
}

public bool TryGetPieceAt(BoardSquare square, out PieceView piece)
{
    piece = FindPieceAt(square);
    return piece != null;
}
```

- [ ] **Step 2: Write capture resolver tests**

Create `CaptureResolverTests.cs`:

```csharp
using NUnit.Framework;
using UnityEngine;

public class CaptureResolverTests
{
    [Test]
    public void Resolve_NormalCaptureReturnsPieceOnDestination()
    {
        GameObject rig = new GameObject("Capture Resolver Test");
        try
        {
            BoardView board = rig.AddComponent<BoardView>();
            Transform piecesRoot = new GameObject("Pieces").transform;
            piecesRoot.SetParent(rig.transform);
            board.Configure(new GameObject("Squares").transform, piecesRoot, new GameObject("Highlights").transform, null, null, null);

            PieceView attacker = CreatePiece(piecesRoot, "e4", ChessSide.White, ChessPieceKind.Pawn);
            PieceView captured = CreatePiece(piecesRoot, "d5", ChessSide.Black, ChessPieceKind.Pawn);

            PieceView resolved = CaptureResolver.Resolve(board, attacker, BoardSquare.FromAlgebraic("d5"));

            Assert.AreSame(captured, resolved);
        }
        finally
        {
            Object.DestroyImmediate(rig);
        }
    }

    [Test]
    public void Resolve_EmptyDestinationReturnsNull()
    {
        GameObject rig = new GameObject("Capture Resolver Test");
        try
        {
            BoardView board = rig.AddComponent<BoardView>();
            Transform piecesRoot = new GameObject("Pieces").transform;
            piecesRoot.SetParent(rig.transform);
            board.Configure(new GameObject("Squares").transform, piecesRoot, new GameObject("Highlights").transform, null, null, null);

            PieceView attacker = CreatePiece(piecesRoot, "e4", ChessSide.White, ChessPieceKind.Pawn);

            PieceView resolved = CaptureResolver.Resolve(board, attacker, BoardSquare.FromAlgebraic("e5"));

            Assert.IsNull(resolved);
        }
        finally
        {
            Object.DestroyImmediate(rig);
        }
    }

    private static PieceView CreatePiece(Transform parent, string square, ChessSide side, ChessPieceKind kind)
    {
        GameObject pieceObject = new GameObject($"{side} {kind} {square}");
        pieceObject.transform.SetParent(parent);
        PieceView piece = pieceObject.AddComponent<PieceView>();
        piece.Initialize(new VisualPieceState(BoardSquare.FromAlgebraic(square), side, kind));
        return piece;
    }
}
```

- [ ] **Step 3: Implement resolver**

Create `CaptureResolver.cs`:

```csharp
public static class CaptureResolver
{
    public static PieceView Resolve(BoardView boardView, PieceView attacker, BoardSquare destination)
    {
        if (boardView == null || attacker == null)
        {
            return null;
        }

        PieceView destinationPiece = boardView.FindPieceAt(destination);
        if (destinationPiece != null && destinationPiece.Side != attacker.Side)
        {
            return destinationPiece;
        }

        return null;
    }
}
```

Note: en passant animation is explicitly deferred until after generic captures. En passant remains rules-correct because board sync still comes from `ChessRulesAdapter`.

- [ ] **Step 4: Run tests**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -quit -projectPath game \
  -runTests -testPlatform EditMode \
  -testResults TestResults/editmode-capture-resolver.xml \
  -logFile Logs/editmode-capture-resolver.log \
  -testFilter CaptureResolverTests
```

Expected: pass.

- [ ] **Step 5: Commit**

Run:

```bash
git add game/Assets/Scripts/View/BoardView.cs game/Assets/Scripts/View/CaptureResolver.cs game/Assets/Tests/EditMode/CaptureResolverTests.cs
git commit -m "feat: resolve captured piece before visual sync"
```

### Task 10: Add Generic Capture Animation

**Goal:** Captures should feel like a small event: attacker advances, captured piece reacts, impact effect appears, then board sync happens.

**Files:**
- Modify: `game/Assets/Scripts/View/PieceMotionController.cs`
- Modify: `game/Assets/Scripts/View/PieceView.cs`
- Modify: `game/Assets/Scripts/Controllers/ChessGameController.cs`
- Create: `game/Assets/Scripts/View/ImpactEffect.cs`
- Create: `game/Assets/Tests/EditMode/ImpactEffectTests.cs`
- Modify: `game/Assets/Tests/EditMode/PieceMotionControllerTests.cs`

- [ ] **Step 1: Add tests for impact effect**

Create `ImpactEffectTests.cs`:

```csharp
using NUnit.Framework;
using UnityEngine;

public class ImpactEffectTests
{
    [Test]
    public void CreateImpact_CreatesShortLivedVisualRoot()
    {
        GameObject root = ImpactEffect.CreateImpact(Vector3.one, Color.yellow);
        try
        {
            Assert.AreEqual("ImpactEffect", root.name);
            Assert.IsNotNull(root.GetComponentInChildren<Renderer>());
            Assert.AreEqual(Vector3.one, root.transform.position);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }
}
```

- [ ] **Step 2: Implement `ImpactEffect`**

Create:

```csharp
using UnityEngine;

public static class ImpactEffect
{
    public static GameObject CreateImpact(Vector3 position, Color color)
    {
        GameObject root = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        root.name = "ImpactEffect";
        root.transform.position = position;
        root.transform.localScale = new Vector3(0.28f, 0.28f, 0.28f);

        Renderer renderer = root.GetComponent<Renderer>();
        Material material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        material.color = color;
        renderer.sharedMaterial = material;

        Collider collider = root.GetComponent<Collider>();
        if (Application.isPlaying)
        {
            Object.Destroy(collider);
            Object.Destroy(root, 0.35f);
        }
        else
        {
            Object.DestroyImmediate(collider);
        }

        return root;
    }
}
```

- [ ] **Step 3: Add hit reaction to `PieceView`**

Add:

```csharp
public void ApplyHitReaction(float intensity)
{
    float clamped = Mathf.Clamp01(intensity);
    transform.localScale = baseScale * Mathf.Lerp(1f, 0.82f, clamped);
}

public void RestoreBaseScale()
{
    transform.localScale = baseScale;
}
```

- [ ] **Step 4: Add capture coroutine to `PieceMotionController`**

Add:

```csharp
public IEnumerator PlayCapture(PieceView attacker, PieceView captured, Vector3 destination)
{
    if (attacker == null)
    {
        yield break;
    }

    if (captured == null)
    {
        yield return MovePiece(attacker, destination);
        yield break;
    }

    Vector3 attackerStart = attacker.transform.position;
    Vector3 lungeTarget = Vector3.Lerp(attackerStart, captured.transform.position, 0.72f);
    float elapsed = 0f;
    float duration = Mathf.Max(0.01f, Settings.CaptureDuration);

    attacker.FaceTowards(captured.transform.position);

    while (elapsed < duration)
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / duration);
        attacker.transform.position = Vector3.Lerp(attackerStart, lungeTarget, Mathf.SmoothStep(0f, 1f, t));
        captured.ApplyHitReaction(t);
        yield return null;
    }

    ImpactEffect.CreateImpact(captured.transform.position + Vector3.up * 0.65f, Color.yellow);
    captured.gameObject.SetActive(false);
    yield return MovePiece(attacker, destination);
}
```

- [ ] **Step 5: Integrate in `ChessGameController`**

In `ExecuteSelectedMove`, before `rules.TryMove`, cache:

```csharp
PieceView capturedPiece = CaptureResolver.Resolve(boardView, movingPiece, destination);
```

Pass `capturedPiece` into animation coroutine:

```csharp
StartCoroutine(AnimateMoveThenSync(movingPiece, capturedPiece, destination, moveResult));
```

Change coroutine signature:

```csharp
private IEnumerator AnimateMoveThenSync(PieceView movingPiece, PieceView capturedPiece, BoardSquare destination, MoveResult moveResult)
```

Inside:

```csharp
Vector3 targetPosition = boardView.GetPieceWorldPosition(destination);
if (motionController != null && moveResult.IsCapture)
{
    yield return motionController.PlayCapture(movingPiece, capturedPiece, targetPosition);
}
else if (motionController != null)
{
    yield return motionController.MovePiece(movingPiece, targetPosition);
}
else
{
    yield return movingPiece.MoveTo(targetPosition, moveDuration);
}
```

- [ ] **Step 6: Run tests**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -quit -projectPath game \
  -runTests -testPlatform EditMode \
  -testResults TestResults/editmode-generic-capture.xml \
  -logFile Logs/editmode-generic-capture.log \
  -testFilter "ImpactEffectTests;PieceMotionControllerTests;ChessGameControllerTests"
```

Expected: pass.

- [ ] **Step 7: Manual capture check**

In Play Mode, run moves:

```text
e2-e4
d7-d5
e4xd5
```

Expected:

- White pawn approaches black pawn.
- Black pawn visibly reacts.
- Impact effect appears.
- Captured piece disappears.
- Board sync places the white pawn on `d5`.
- Turn changes to black after animation.

- [ ] **Step 8: Commit**

Run:

```bash
git add game/Assets/Scripts/View game/Assets/Scripts/Controllers/ChessGameController.cs game/Assets/Tests/EditMode
git commit -m "feat: add generic capture animation"
```

### Task 11: Add Per-Piece Capture Styles

**Goal:** Captures should vary by piece type, creating the foundation for Harry Potter / Battle Chess inspired personality.

**Files:**
- Create: `game/Assets/Scripts/View/CaptureAnimationStyle.cs`
- Create: `game/Assets/Scripts/View/CaptureAnimationLibrary.cs`
- Create: `game/Assets/Tests/EditMode/CaptureAnimationLibraryTests.cs`
- Modify: `game/Assets/Scripts/View/PieceMotionController.cs`
- Modify: `docs/design/capture-animation-roadmap.md`

- [ ] **Step 1: Write style tests**

Create:

```csharp
using NUnit.Framework;

public class CaptureAnimationLibraryTests
{
    [Test]
    public void GetStyle_ReturnsDistinctMovementForMajorPieces()
    {
        CaptureAnimationStyle pawn = CaptureAnimationLibrary.GetStyle(ChessPieceKind.Pawn);
        CaptureAnimationStyle rook = CaptureAnimationLibrary.GetStyle(ChessPieceKind.Rook);
        CaptureAnimationStyle queen = CaptureAnimationLibrary.GetStyle(ChessPieceKind.Queen);

        Assert.AreEqual("Empurrao curto", pawn.DisplayName);
        Assert.AreEqual("Avanco pesado", rook.DisplayName);
        Assert.AreEqual("Golpe dominante", queen.DisplayName);
        Assert.Greater(rook.LungeDistance, pawn.LungeDistance);
        Assert.Greater(queen.ImpactScale, pawn.ImpactScale);
    }

    [Test]
    public void GetStyle_AllPieceKindsHaveSafeDurations()
    {
        foreach (ChessPieceKind kind in System.Enum.GetValues(typeof(ChessPieceKind)))
        {
            CaptureAnimationStyle style = CaptureAnimationLibrary.GetStyle(kind);
            Assert.Greater(style.Duration, 0.2f);
            Assert.Less(style.Duration, 0.9f);
        }
    }
}
```

- [ ] **Step 2: Implement style model**

Create:

```csharp
using UnityEngine;

public readonly struct CaptureAnimationStyle
{
    public string DisplayName { get; }
    public float Duration { get; }
    public float LungeDistance { get; }
    public float ImpactScale { get; }
    public Color ImpactColor { get; }

    public CaptureAnimationStyle(string displayName, float duration, float lungeDistance, float impactScale, Color impactColor)
    {
        DisplayName = displayName;
        Duration = duration;
        LungeDistance = lungeDistance;
        ImpactScale = impactScale;
        ImpactColor = impactColor;
    }
}
```

- [ ] **Step 3: Implement style library**

Create:

```csharp
using UnityEngine;

public static class CaptureAnimationLibrary
{
    public static CaptureAnimationStyle GetStyle(ChessPieceKind kind)
    {
        switch (kind)
        {
            case ChessPieceKind.Pawn:
                return new CaptureAnimationStyle("Empurrao curto", 0.38f, 0.62f, 0.85f, new Color(1f, 0.78f, 0.35f));
            case ChessPieceKind.Rook:
                return new CaptureAnimationStyle("Avanco pesado", 0.52f, 0.82f, 1.15f, new Color(0.75f, 0.86f, 1f));
            case ChessPieceKind.Knight:
                return new CaptureAnimationStyle("Salto de cavalo", 0.48f, 0.76f, 1.0f, new Color(0.9f, 0.72f, 1f));
            case ChessPieceKind.Bishop:
                return new CaptureAnimationStyle("Corte diagonal", 0.44f, 0.7f, 0.95f, new Color(0.8f, 1f, 0.82f));
            case ChessPieceKind.Queen:
                return new CaptureAnimationStyle("Golpe dominante", 0.58f, 0.86f, 1.25f, new Color(1f, 0.68f, 0.95f));
            case ChessPieceKind.King:
                return new CaptureAnimationStyle("Comando real", 0.46f, 0.66f, 1.05f, new Color(1f, 0.92f, 0.45f));
            default:
                return new CaptureAnimationStyle("Captura", 0.45f, 0.72f, 1f, Color.yellow);
        }
    }
}
```

- [ ] **Step 4: Apply style in `PieceMotionController.PlayCapture`**

At start of `PlayCapture`, add:

```csharp
CaptureAnimationStyle style = CaptureAnimationLibrary.GetStyle(attacker.Kind);
float duration = style.Duration;
Vector3 lungeTarget = Vector3.Lerp(attackerStart, captured.transform.position, style.LungeDistance);
```

Replace `ImpactEffect.CreateImpact(..., Color.yellow)` with:

```csharp
GameObject impact = ImpactEffect.CreateImpact(captured.transform.position + Vector3.up * 0.65f, style.ImpactColor);
impact.transform.localScale *= style.ImpactScale;
```

- [ ] **Step 5: Update roadmap**

Change `docs/design/capture-animation-roadmap.md` to mark:

- Fase 1 as implemented when Task 10 is complete.
- Fase 2 as implemented when Task 11 is complete.
- Fase 3 as future rig/clip layer.

- [ ] **Step 6: Run tests**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -quit -projectPath game \
  -runTests -testPlatform EditMode \
  -testResults TestResults/editmode-capture-styles.xml \
  -logFile Logs/editmode-capture-styles.log \
  -testFilter CaptureAnimationLibraryTests
```

Then run full EditMode.

- [ ] **Step 7: Manual capture sample**

Test these captures:

```text
Pawn: e2-e4, d7-d5, e4xd5
Knight: g1-f3, b8-c6, f3xe5 after opening a valid route
Queen: use a short test position through controller tests or manual free play if available
```

Expected: capture impact style differs by attacking piece type.

- [ ] **Step 8: Commit**

Run:

```bash
git add game/Assets/Scripts/View docs/design/capture-animation-roadmap.md game/Assets/Tests/EditMode/CaptureAnimationLibraryTests.cs
git commit -m "feat: add per-piece capture animation styles"
```

### Task 12: Add Camera Shake For Captures

**Goal:** Add a restrained camera response to impact without harming board readability.

**Files:**
- Modify: `game/Assets/Scripts/Controllers/CameraController.cs`
- Modify: `game/Assets/Scripts/Controllers/ChessGameController.cs`
- Modify: `game/Assets/Tests/EditMode/CameraControllerTests.cs`

- [ ] **Step 1: Add camera shake tests**

Add to `CameraControllerTests.cs`:

```csharp
[Test]
public void Shake_DoesNotChangePerspectiveSide()
{
    GameObject cameraObject = new GameObject("Camera Test");
    try
    {
        CameraController camera = cameraObject.AddComponent<CameraController>();
        camera.SetPerspective(ChessSide.Black, true);

        camera.Shake(0.08f, 0.1f);

        Assert.AreEqual(ChessSide.Black, camera.CurrentPerspective);
    }
    finally
    {
        Object.DestroyImmediate(cameraObject);
    }
}
```

- [ ] **Step 2: Implement shake API**

In `CameraController.cs`, add:

```csharp
private Coroutine shakeTransition;

public void Shake(float amplitude, float duration)
{
    if (!Application.isPlaying || amplitude <= 0f || duration <= 0f)
    {
        return;
    }

    if (shakeTransition != null)
    {
        StopCoroutine(shakeTransition);
    }

    shakeTransition = StartCoroutine(ShakeRoutine(amplitude, duration));
}
```

Add coroutine:

```csharp
private IEnumerator ShakeRoutine(float amplitude, float duration)
{
    Vector3 basePosition = transform.position;
    float elapsed = 0f;

    while (elapsed < duration)
    {
        elapsed += Time.deltaTime;
        float fade = 1f - Mathf.Clamp01(elapsed / duration);
        Vector3 offset = new Vector3(Mathf.Sin(elapsed * 80f), Mathf.Cos(elapsed * 63f), 0f) * amplitude * fade;
        transform.position = basePosition + offset;
        yield return null;
    }

    transform.position = basePosition;
    shakeTransition = null;
}
```

- [ ] **Step 3: Trigger shake after capture**

In `ChessGameController.AnimateMoveThenSync`, after capture animation but before sync:

```csharp
if (moveResult.IsCapture && cameraController != null)
{
    cameraController.Shake(0.07f, 0.16f);
}
```

- [ ] **Step 4: Run tests**

Run `CameraControllerTests`, then full EditMode.

- [ ] **Step 5: Manual check**

Capture a piece. Expected: a small readable shake, no nausea, no camera clipping, no loss of board orientation.

- [ ] **Step 6: Commit**

Run:

```bash
git add game/Assets/Scripts/Controllers/CameraController.cs game/Assets/Scripts/Controllers/ChessGameController.cs game/Assets/Tests/EditMode/CameraControllerTests.cs
git commit -m "feat: add subtle capture camera shake"
```

### Task 13: Prepare Optional Rigged Animation Layer

**Goal:** Make the system ready for future humanoid clips without requiring all current generated models to be rigged.

**Files:**
- Create: `game/Assets/Scripts/View/CharacterAnimationDriver.cs`
- Create: `game/Assets/Tests/EditMode/CharacterAnimationDriverTests.cs`
- Modify: `game/Assets/Scripts/View/PieceFactory.cs`
- Modify: `docs/design/capture-animation-roadmap.md`

- [ ] **Step 1: Write driver tests**

Create:

```csharp
using NUnit.Framework;
using UnityEngine;

public class CharacterAnimationDriverTests
{
    [Test]
    public void TryPlay_WhenNoAnimatorExists_ReturnsFalse()
    {
        GameObject character = new GameObject("Character");
        try
        {
            CharacterAnimationDriver driver = character.AddComponent<CharacterAnimationDriver>();

            Assert.IsFalse(driver.TryPlay("Walk"));
        }
        finally
        {
            Object.DestroyImmediate(character);
        }
    }

    [Test]
    public void Configure_SetsAnimatorWhenPresent()
    {
        GameObject character = new GameObject("Character");
        try
        {
            Animator animator = character.AddComponent<Animator>();
            CharacterAnimationDriver driver = character.AddComponent<CharacterAnimationDriver>();

            driver.Configure(animator);

            Assert.IsTrue(driver.HasAnimator);
        }
        finally
        {
            Object.DestroyImmediate(character);
        }
    }
}
```

- [ ] **Step 2: Implement driver**

Create:

```csharp
using UnityEngine;

public sealed class CharacterAnimationDriver : MonoBehaviour
{
    [SerializeField] private Animator animator;

    public bool HasAnimator => animator != null;

    public void Configure(Animator targetAnimator)
    {
        animator = targetAnimator;
    }

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    public bool TryPlay(string stateName)
    {
        if (animator == null || string.IsNullOrEmpty(stateName))
        {
            return false;
        }

        animator.Play(stateName);
        return true;
    }
}
```

- [ ] **Step 3: Attach driver to custom visuals**

In `PieceFactory.BuildCustomShape`, after instantiating `visual`, add:

```csharp
CharacterAnimationDriver driver = visual.GetComponent<CharacterAnimationDriver>();
if (driver == null)
{
    driver = visual.AddComponent<CharacterAnimationDriver>();
}
driver.Configure(visual.GetComponentInChildren<Animator>());
```

This does not require current prefabs to have Animator. It only creates the extension point.

- [ ] **Step 4: Document future rig workflow**

Update `capture-animation-roadmap.md`:

```markdown
## Camada opcional de rig

Os modelos atuais podem funcionar sem rig usando movimento procedural. Se um prefab futuro tiver `Animator`, o `CharacterAnimationDriver` pode tocar estados como `Walk`, `Attack`, `Hit` e `Idle`. O fallback procedural continua obrigatorio para qualquer modelo sem rig.
```

- [ ] **Step 5: Run tests**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -quit -projectPath game \
  -runTests -testPlatform EditMode \
  -testResults TestResults/editmode-animation-driver.xml \
  -logFile Logs/editmode-animation-driver.log \
  -testFilter CharacterAnimationDriverTests
```

Then run `PieceFactoryTests`.

- [ ] **Step 6: Commit**

Run:

```bash
git add game/Assets/Scripts/View/CharacterAnimationDriver.cs game/Assets/Scripts/View/PieceFactory.cs game/Assets/Tests/EditMode/CharacterAnimationDriverTests.cs docs/design/capture-animation-roadmap.md
git commit -m "feat: prepare optional rigged character animation layer"
```

### Task 14: Professional Scene And Lighting Pass

**Goal:** Improve overall presentation without adding heavy effects or breaking camera paths.

**Files:**
- Modify: `game/Assets/Scripts/View/ScenePolish.cs`
- Modify: `game/Assets/Tests/EditMode/ScenePolishTests.cs`
- Modify: `game/Assets/Scenes/Main.unity`

- [ ] **Step 1: Add scene polish tests for presentation props**

Add to `ScenePolishTests.cs`:

```csharp
[Test]
public void ApplyPolish_CreatesClassroomDetailsWithoutBlockingBoard()
{
    GameObject rig = new GameObject("Scene Polish Test Rig");
    try
    {
        ScenePolish polish = rig.AddComponent<ScenePolish>();

        polish.ApplyPolish();

        Transform collegeTheme = rig.transform.Find("CollegeTheme");
        Assert.IsNotNull(collegeTheme.Find("DeskTrim"));
        Assert.IsNotNull(collegeTheme.Find("MarkerTrayNorth"));
        Assert.IsNotNull(collegeTheme.Find("MarkerTraySouth"));
        Assert.IsNotNull(collegeTheme.Find("SmallClock"));
        Assert.AreEqual(0, collegeTheme.GetComponentsInChildren<Collider>().Length);
    }
    finally
    {
        Object.DestroyImmediate(rig);
    }
}
```

- [ ] **Step 2: Add tasteful classroom details**

In `ScenePolish.BuildCollegeTheme`, add:

```csharp
CreateCube(collegeTheme, "DeskTrim", new Vector3(0f, -0.02f, 0f), new Vector3(13.55f, 0.05f, 13.55f), darkMaterial, false);
CreateCube(collegeTheme, "MarkerTrayNorth", new Vector3(0f, 2.42f, 12.6f), new Vector3(3.8f, 0.06f, 0.08f), darkMaterial, false);
CreateCube(collegeTheme, "MarkerTraySouth", new Vector3(0f, 2.42f, -12.6f), new Vector3(3.8f, 0.06f, 0.08f), darkMaterial, false);
CreateCube(collegeTheme, "SmallClock", new Vector3(-5.4f, 3.92f, 12.62f), new Vector3(0.46f, 0.46f, 0.06f), boardMaterial, false);
```

- [ ] **Step 3: Tune lighting**

Keep camera-safe lighting. Target:

- Key Light intensity between `1.2` and `1.7`.
- Fill Light range at least `12`.
- Rim Light intensity below `75`.
- Ambient ground color not pure black.

Do not add bloom or heavy post-processing before movement/capture is stable.

- [ ] **Step 4: Run scene polish tests**

Run `ScenePolishTests`, then full EditMode.

- [ ] **Step 5: Apply scene polish in Unity**

Open `Assets/Scenes/Main.unity`, select `Scene Polish`, run/apply if needed, save scene.

Expected: scene remains a classroom-like contained environment; no wall clips through turn cameras.

- [ ] **Step 6: Commit**

Run:

```bash
git add game/Assets/Scripts/View/ScenePolish.cs game/Assets/Tests/EditMode/ScenePolishTests.cs game/Assets/Scenes/Main.unity
git commit -m "feat: polish classroom scene presentation"
```

### Task 15: Manual Character Model Polish Pass

**Goal:** Inspect and tune every current custom prefab for final readability.

**Files:**
- Modify as needed:
  - `game/Assets/Resources/CustomPieces/Pawn_Mathwidu_Redhead_v2.prefab`
  - `game/Assets/Resources/CustomPieces/Rook_Alex.prefab`
  - `game/Assets/Resources/CustomPieces/Knight_Gustavo.prefab`
  - `game/Assets/Resources/CustomPieces/Bishop_Rafael.prefab`
  - `game/Assets/Resources/CustomPieces/Queen_Marta.prefab`
  - `game/Assets/Resources/CustomPieces/King_Ricardo_Carioca.prefab`
- Modify: `docs/design/custom-piece-generation-workflow.md`

- [ ] **Step 1: Create visual checklist table**

Add to `custom-piece-generation-workflow.md`:

```markdown
## Checklist final dos personagens atuais

| Personagem | Peca | Status | Ajustes finais |
| --- | --- | --- | --- |
| Mathwidu | Peao | Revisar | escala, cabelo ruivo, leitura das costas/frente |
| Alex | Torre | Revisar | torre pequena, pose sentada, contraste do blusao |
| Gustavo | Cavalo | Revisar | cavalo pequeno, oculos, proporcao adulta |
| Rafael | Bispo | Revisar | postura, contraste do casaco, altura |
| Marta | Rainha | Revisar | scarf azul/branco, oculos, coroa discreta |
| Ricardo Carioca | Rei | Revisar | moletom azul, oculos, coroa discreta |
```

- [ ] **Step 2: Inspect every prefab in isolation**

For each prefab:

1. Open prefab.
2. Confirm all renderers are under one clear root.
3. Confirm no unnecessary colliders are inside generated GLB children.
4. Confirm material colors are readable in a neutral light.
5. Confirm no huge hidden background planes exist.
6. Save only if a real adjustment is made.

- [ ] **Step 3: Inspect every piece in game camera**

Run Play Mode and select:

```text
a2 pawn, a1 rook, b1 knight, c1 bishop, d1 queen, e1 king
```

For each:

- It must be recognizable from game camera.
- It must be visible in full in sidebar preview.
- It must not clip through the base.
- It must face opponent correctly.
- It must not look much larger/smaller than pieces of similar rank.

- [ ] **Step 4: Apply small prefab-only corrections**

Allowed corrections:

- Root local scale.
- Root local rotation.
- Child material roughness/smoothness.
- Minor child offsets.
- Disable irrelevant generated helper objects if visible.

Forbidden corrections in this task:

- Regenerate models.
- Replace character identity.
- Add new animation clips.
- Add new gameplay code.

- [ ] **Step 5: Run visual runtime assertion**

Use Unity MCP or a temporary editor command to assert:

```text
Runtime PieceView count = 32
Pawn custom = 16/16
Rook custom = 4/4
Knight custom = 4/4
Bishop custom = 4/4
Queen custom = 2/2
King custom = 2/2
```

Expected: every count matches.

- [ ] **Step 6: Commit**

Run:

```bash
git add game/Assets/Resources/CustomPieces docs/design/custom-piece-generation-workflow.md
git commit -m "art: polish current custom character prefabs"
```

### Task 16: Add PlayMode Smoke Test For Movement And Capture

**Goal:** Catch regressions that only appear in Play Mode/coroutines.

**Files:**
- Create: `game/Assets/Tests/PlayMode/ChessCgi.PlayModeTests.asmdef`
- Create: `game/Assets/Tests/PlayMode/MovementAndCaptureFlowTests.cs`

- [ ] **Step 1: Create PlayMode asmdef**

Create `game/Assets/Tests/PlayMode/ChessCgi.PlayModeTests.asmdef`:

```json
{
    "name": "ChessCgi.PlayModeTests",
    "rootNamespace": "",
    "references": [
        "ChessCgi.Runtime",
        "UnityEngine.TestRunner"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [
        "nunit.framework.dll"
    ],
    "autoReferenced": true,
    "defineConstraints": [
        "UNITY_INCLUDE_TESTS"
    ],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 2: Create smoke test**

Create `MovementAndCaptureFlowTests.cs`:

```csharp
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class MovementAndCaptureFlowTests
{
    [UnityTest]
    public IEnumerator LegalMove_CompletesAfterMotionAndChangesTurn()
    {
        GameObject rig = CreatePlayableRig(out BoardView boardView, out ChessGameController controller);

        PieceView pawn = boardView.Pieces.First(piece => piece.Square.Equals(BoardSquare.FromAlgebraic("e2")));
        controller.SelectPiece(pawn);
        controller.SelectDestination(BoardSquare.FromAlgebraic("e4"));

        yield return new WaitForSeconds(1.2f);

        Assert.AreEqual(ChessSide.Black, controller.CurrentTurn);
        Assert.IsFalse(controller.IsInputBlocked);
        Object.Destroy(rig);
    }

    private static GameObject CreatePlayableRig(out BoardView boardView, out ChessGameController controller)
    {
        GameObject rig = new GameObject("PlayMode Rig");
        boardView = rig.AddComponent<BoardView>();
        PieceFactory factory = rig.AddComponent<PieceFactory>();
        PieceMotionController motion = rig.AddComponent<PieceMotionController>();
        controller = rig.AddComponent<ChessGameController>();

        Transform squares = new GameObject("Squares").transform;
        Transform pieces = new GameObject("Pieces").transform;
        Transform highlights = new GameObject("Highlights").transform;
        squares.SetParent(rig.transform);
        pieces.SetParent(rig.transform);
        highlights.SetParent(rig.transform);

        boardView.Configure(squares, pieces, highlights, null, null, null);
        controller.Configure(boardView, factory, null);
        controller.NewGame();
        return rig;
    }
}
```

If compiler complains that `motion` is unused, remove the local variable assignment and keep `rig.AddComponent<PieceMotionController>();`.

- [ ] **Step 3: Run PlayMode tests**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -quit -projectPath game \
  -runTests -testPlatform PlayMode \
  -testResults TestResults/playmode-motion-capture.xml \
  -logFile Logs/playmode-motion-capture.log
```

Expected: PlayMode tests pass.

- [ ] **Step 4: Commit**

Run:

```bash
git add game/Assets/Tests/PlayMode
git commit -m "test: add playmode movement smoke test"
```

### Task 17: Final Documentation, Build, And Delivery Verification

**Goal:** End with a professional build and clear delivery docs.

**Files:**
- Modify: `README.md`
- Modify: `docs/learning-log.md`
- Modify: `docs/design/capture-animation-roadmap.md`
- Optional create: `docs/design/final-polish-validation.md`

- [ ] **Step 1: Update README feature list**

Ensure README states:

```markdown
- Sidebar com detalhes do personagem selecionado, preview 3D interativo, zoom e rotacao.
- Movimento visual com caminhada procedural ate a casa de destino.
- Capturas com impacto visual curto e estilos diferentes por tipo de peca.
- Personagens personalizados revisados visualmente para leitura no tabuleiro.
```

- [ ] **Step 2: Update controls**

Ensure README states:

```markdown
Preview 3D da peca:
- Arraste dentro do preview para girar o modelo.
- Use scroll no preview para aproximar ou afastar.
```

- [ ] **Step 3: Run all EditMode tests**

Run baseline EditMode command.

Expected: pass.

- [ ] **Step 4: Run PlayMode tests**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -quit -projectPath game \
  -runTests -testPlatform PlayMode \
  -testResults TestResults/playmode-final.xml \
  -logFile Logs/playmode-final.log
```

Expected: pass.

- [ ] **Step 5: Generate final macOS build**

Run final build command from baseline section.

Expected: `Builds/macOS/XadrezCGI.app` exists and launches.

- [ ] **Step 6: Manual acceptance checklist**

Open `Builds/macOS/XadrezCGI.app` and verify:

- Start screen appears.
- New game starts.
- All custom characters appear.
- Selecting a piece opens sidebar.
- Sidebar text is readable.
- Sidebar preview shows full model.
- Drag/scroll preview works.
- Legal move walks to target square.
- Capture has impact/reaction.
- Turn changes after animation.
- Camera does not clip through walls.
- Promotion UI still works.
- New game resets board and sidebar.

- [ ] **Step 7: Save validation doc**

Create `docs/design/final-polish-validation.md`:

```markdown
# Final Polish Validation

Date: 2026-06-04

## Automated Tests

- EditMode: passing.
- PlayMode: passing.
- macOS build: generated.

## Manual Checks

- All custom characters visible.
- Sidebar preview rotates and zooms.
- Movement animation completes before turn switch.
- Capture animation shows impact and removes captured piece.
- Camera stays readable on both turns.
- Stable fallback tag remains `entrega-v1-estavel`.
```

- [ ] **Step 8: Commit and tag**

Run:

```bash
git add README.md docs
git commit -m "docs: validate professional polish delivery"
git tag -a entrega-v2-polida -m "Versao polida com animacoes e sidebar premium"
```

Expected: new tag exists:

```bash
git tag --list "entrega-v*" -n
```

## Risk Controls

- If a movement task breaks legal move flow, do not modify `ChessRulesAdapter`; fix the visual/controller integration.
- If generated models do not have `Animator`, use procedural movement. Do not block the project on rigging.
- If capture animation creates confusing timing, reduce durations before adding new effects.
- If sidebar preview causes performance problems, lower RenderTexture to `512x384` and render only when selection, yaw, or zoom changes.
- If PlayMode tests are flaky in batchmode, keep EditMode tests mandatory and use manual PlayMode checklist before build.

## Self-Review

- Spec coverage: The plan covers stable fallback, metadata, sidebar information, interactive preview, custom piece quality, movement, captures, per-piece styles, camera feedback, rig-ready architecture, scene polish, manual model polish, PlayMode smoke tests, docs, build, and final tag.
- Placeholder scan: No `TBD`, `TODO`, or unassigned future fields are required to complete the plan. Registration numbers intentionally use `Matricula nao informada` until the user supplies real values with permission.
- Type consistency: New types are named consistently across tasks: `CharacterProfile`, `CharacterProfileCatalog`, `SelectedPiecePreviewController`, `SelectedPiecePreviewInput`, `PieceVisualQuality`, `PieceMotionSettings`, `PieceMotionController`, `CaptureResolver`, `CaptureAnimationStyle`, `CaptureAnimationLibrary`, `ImpactEffect`, and `CharacterAnimationDriver`.
- Delivery safety: `entrega-v1-estavel` remains untouched; `entrega-v2-polida` is created only after final tests/build/manual validation.
