# Overnight Character Animation Polish Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Move the current stable custom chess game toward a more professional state with base-free characters, semantic team outfits, richer per-piece movement, a stronger selected-piece sidebar, and an animation/capture pipeline ready for future rigged assets.

**Architecture:** Keep chess rules isolated. Add contract checks and presentation helpers around `PieceFactory`, `PieceView`, `TeamOutfitApplier`, `CharacterProfileCatalog`, `SelectedPiecePreviewController`, and future animation libraries. Prefer no-cost local Unity/Blender/Codex automation before any paid generation.

**Tech Stack:** Unity 6.3 LTS, C# MonoBehaviour runtime, NUnit EditMode tests, Unity MCP probes, Blender local scripts/MCP, Python unittest for Blender tooling.

---

## Execution Policy

Before implementing this plan, keep these rules active:

- Do not spend Unity AI, Tripo, Meshy, or other paid credits.
- Do not delete old character assets.
- Do not change chess rules unless a test proves a visual layer requires a small adapter.
- Keep the fallback tag `entrega-v1-estavel` untouched.
- Prefer small commits after each task group if the user allows committing.
- If a task needs a paid generation step, stop and report instead of improvising.

Recommended execution order for an overnight run:

1. Task 1: safety checkpoint and baseline gates.
2. Task 2: automated custom piece audit.
3. Task 3: semantic team outfit contract for all six piece types.
4. Task 4: base-free selection and invisible support.
5. Task 5: per-piece movement styles.
6. Task 6: selected-piece sidebar polish.
7. Task 7: capture animation architecture skeleton.
8. Task 8: validation, screenshots, and final report.
9. Task 9: future rig pipeline documentation only, no unattended paid generation.

---

## File Map

Existing files likely to modify:

- `game/Assets/Scripts/View/PieceFactory.cs`: custom prefab instantiation, visual extensions, outfit application.
- `game/Assets/Scripts/View/TeamOutfitApplier.cs`: semantic outfit recoloring.
- `game/Assets/Scripts/View/PieceMotionSettings.cs`: movement parameters.
- `game/Assets/Scripts/View/PieceView.cs`: root movement coroutine and walk pose evaluation.
- `game/Assets/Scripts/View/ModularCharacterRig.cs`: procedural limb animation when bindable parts exist.
- `game/Assets/Scripts/View/CharacterAnimationDriver.cs`: future Animator bridge.
- `game/Assets/Scripts/View/CharacterVisualContract.cs`: rig/animation/socket metadata.
- `game/Assets/Scripts/UI/GameHud.cs`: sidebar layout and selected-piece text.
- `game/Assets/Scripts/UI/SelectedPiecePreviewController.cs`: 3D preview fit, camera, zoom, rotation.
- `game/Assets/Scripts/Domain/CharacterProfile.cs`: selected-piece metadata model.
- `game/Assets/Scripts/Domain/CharacterProfileCatalog.cs`: metadata for Mathwidu, Alex, Gustavo, Rafael, Marta, Ricardo.
- `docs/design/custom-piece-generation-workflow.md`: production workflow.
- `docs/design/capture-animation-roadmap.md`: capture roadmap.
- `docs/design/professional-rigging-animation-roadmap.md`: rigging roadmap.

New files likely to create:

- `game/Assets/Scripts/View/PieceMovementStyle.cs`
- `game/Assets/Scripts/View/PieceMovementStyleLibrary.cs`
- `game/Assets/Scripts/View/CaptureAnimationController.cs`
- `game/Assets/Scripts/View/CaptureAnimationStyle.cs`
- `game/Assets/Scripts/View/CaptureAnimationStyleLibrary.cs`
- `game/Assets/Tests/EditMode/CustomPieceVisualContractTests.cs`
- `game/Assets/Tests/EditMode/PieceMovementStyleLibraryTests.cs`
- `game/Assets/Tests/EditMode/CaptureAnimationStyleLibraryTests.cs`
- `tools/character_pipeline/audit_custom_pieces.py`
- `docs/design/overnight-character-polish-report.md`

---

## Task 1: Safety Checkpoint And Baseline Gates

**Files:**
- Read: `.git/`
- Read: `game/Assets/Scenes/Main.unity`
- Read: `game/Assets/Resources/CustomPieces/`
- Modify only if needed: `docs/design/overnight-character-polish-report.md`

- [ ] **Step 1: Verify fallback tag exists**

Run:

```bash
git rev-parse entrega-v1-estavel
```

Expected: prints a commit hash.

- [ ] **Step 2: Capture current status**

Run:

```bash
git status --short
git diff --stat
```

Expected: output may be dirty. Save a short summary in the final report. Do not clean unrelated files.

- [ ] **Step 3: Run whitespace gate**

Run:

```bash
git diff --check
```

Expected: no output, exit code 0.

- [ ] **Step 4: Run current Python character tests**

Run:

```bash
python3 -m unittest tools.blender.tests.test_mathwidu_v3b_candidate -v
```

Expected: all tests pass.

- [ ] **Step 5: Probe Unity console**

Use Unity MCP `Unity_GetConsoleLogs` with `logTypes="error,warning"`.

Expected:

```text
errorCount: 0
warningCount: 0
```

- [ ] **Step 6: Stop rule**

If any baseline gate fails, stop and fix only the failure that blocks compilation/testing. Do not proceed into polish work with a broken baseline.

---

## Task 2: Automated Custom Piece Audit

**Files:**
- Create: `tools/character_pipeline/audit_custom_pieces.py`
- Create: `tools/character_pipeline/__init__.py`
- Create: `tools/character_pipeline/tests/test_audit_custom_pieces.py`
- Create/Modify: `docs/design/character-rig-audit.md`
- Create: `game/Assets/Tests/EditMode/CustomPieceVisualContractTests.cs`

- [ ] **Step 1: Write Python test for expected audit keys**

Create `tools/character_pipeline/tests/test_audit_custom_pieces.py` with:

```python
import json
import unittest
from pathlib import Path

from tools.character_pipeline.audit_custom_pieces import build_audit


class CustomPieceAuditTests(unittest.TestCase):
    def test_audit_lists_all_six_current_custom_prefabs(self):
        root = Path(__file__).resolve().parents[3]
        audit = build_audit(root)
        names = {entry["prefab"] for entry in audit["pieces"]}
        self.assertEqual(
            {
                "Pawn_Mathwidu_v3b",
                "Rook_Alex",
                "Knight_Gustavo",
                "Bishop_Rafael",
                "Queen_Marta",
                "King_Ricardo_Carioca",
            },
            names,
        )

    def test_audit_marks_credit_spend_as_blocked(self):
        root = Path(__file__).resolve().parents[3]
        audit = build_audit(root)
        self.assertEqual("blocked_without_user_confirmation", audit["creditSpendPolicy"])
```

- [ ] **Step 2: Run test to verify RED**

Run:

```bash
python3 -m unittest tools.character_pipeline.tests.test_audit_custom_pieces -v
```

Expected: fail because `tools.character_pipeline.audit_custom_pieces` does not exist.

- [ ] **Step 3: Implement audit builder**

Create `tools/character_pipeline/audit_custom_pieces.py` with:

```python
from __future__ import annotations

import json
from pathlib import Path


CUSTOM_PREFABS = [
    ("Pawn_Mathwidu_v3b", "Pawn", "Mathwidu"),
    ("Rook_Alex", "Rook", "Alex"),
    ("Knight_Gustavo", "Knight", "Gustavo"),
    ("Bishop_Rafael", "Bishop", "Rafael"),
    ("Queen_Marta", "Queen", "Marta"),
    ("King_Ricardo_Carioca", "King", "Ricardo Carioca"),
]


def build_audit(repo_root: Path) -> dict:
    resources = repo_root / "game" / "Assets" / "Resources" / "CustomPieces"
    pieces = []
    for prefab, kind, person in CUSTOM_PREFABS:
        prefab_path = resources / f"{prefab}.prefab"
        asset_dir = resources / f"{prefab}_Assets"
        glb_path = asset_dir / "selected.glb"
        text = prefab_path.read_text(errors="ignore") if prefab_path.exists() else ""
        pieces.append(
            {
                "prefab": prefab,
                "kind": kind,
                "person": person,
                "prefabExists": prefab_path.exists(),
                "selectedGlbExists": glb_path.exists(),
                "hasTeamBaseToken": "TeamBase" in text,
                "hasTeamOutfitToken": "TeamOutfit" in text or "TeamClothes" in text or "TeamUniform" in text,
                "hasAnimatorToken": "Animator" in text,
            }
        )

    return {
        "creditSpendPolicy": "blocked_without_user_confirmation",
        "pieces": pieces,
    }


def main() -> int:
    repo_root = Path(__file__).resolve().parents[2]
    print(json.dumps(build_audit(repo_root), indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
```

- [ ] **Step 4: Run Python audit tests**

Run:

```bash
python3 -m unittest tools.character_pipeline.tests.test_audit_custom_pieces -v
```

Expected: pass.

- [ ] **Step 5: Write Unity contract test**

Create `game/Assets/Tests/EditMode/CustomPieceVisualContractTests.cs` with:

```csharp
using NUnit.Framework;
using UnityEngine;

public class CustomPieceVisualContractTests
{
    [TestCase("Pawn_Mathwidu_v3b")]
    [TestCase("Rook_Alex")]
    [TestCase("Knight_Gustavo")]
    [TestCase("Bishop_Rafael")]
    [TestCase("Queen_Marta")]
    [TestCase("King_Ricardo_Carioca")]
    public void CustomPrefab_ExistsAndHasRenderer(string prefabName)
    {
        GameObject prefab = Resources.Load<GameObject>($"CustomPieces/{prefabName}");
        Assert.IsNotNull(prefab, prefabName);
        Assert.Greater(prefab.GetComponentsInChildren<Renderer>(true).Length, 0, prefabName);
    }
}
```

- [ ] **Step 6: Probe Unity test class by MCP**

Use `Unity_RunCommand` to call each assertion or run EditMode tests. Expected: all six prefabs exist and have renderers.

- [ ] **Step 7: Update audit document**

Append to `docs/design/character-rig-audit.md`:

```markdown
## Overnight Audit 2026-06-07

Credit spend policy: blocked without explicit user confirmation.

The automated audit lives in `tools/character_pipeline/audit_custom_pieces.py`.
It checks the six active custom prefabs for prefab presence, selected GLB presence,
TeamBase tokens, TeamOutfit semantic tokens, and Animator tokens.
```

---

## Task 3: Semantic Team Outfit Contract For All Custom Pieces

**Files:**
- Modify: `game/Assets/Scripts/View/TeamOutfitApplier.cs`
- Modify: `game/Assets/Tests/EditMode/TeamOutfitApplierTests.cs`
- Modify: `game/Assets/Tests/EditMode/CustomPieceVisualContractTests.cs`
- Modify: `docs/design/custom-piece-generation-workflow.md`
- Potential asset updates: `game/Assets/Resources/CustomPieces/*`

- [ ] **Step 1: Add test that all active prefabs have semantic outfit candidates or documented exceptions**

Append to `CustomPieceVisualContractTests.cs`:

```csharp
[TestCase("Pawn_Mathwidu_v3b")]
[TestCase("Rook_Alex")]
[TestCase("Knight_Gustavo")]
[TestCase("Bishop_Rafael")]
[TestCase("Queen_Marta")]
[TestCase("King_Ricardo_Carioca")]
public void CustomPrefab_HasSemanticTeamOutfitOrDocumentedStaticException(string prefabName)
{
    GameObject prefab = Resources.Load<GameObject>($"CustomPieces/{prefabName}");
    Assert.IsNotNull(prefab, prefabName);

    Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
    bool hasSemanticOutfit = false;
    foreach (Renderer renderer in renderers)
    {
        if (renderer.name.Contains("TeamOutfit") || renderer.name.Contains("TeamClothes") || renderer.name.Contains("TeamUniform"))
        {
            hasSemanticOutfit = true;
            break;
        }

        foreach (Material material in renderer.sharedMaterials)
        {
            if (material != null &&
                (material.name.Contains("TeamOutfit") ||
                 material.name.Contains("TeamClothes") ||
                 material.name.Contains("TeamUniform")))
            {
                hasSemanticOutfit = true;
                break;
            }
        }
    }

    Assert.IsTrue(hasSemanticOutfit, $"{prefabName} needs a semantic team outfit material or overlay.");
}
```

- [ ] **Step 2: Verify RED**

Run focused Unity MCP assertions. Expected: likely fail for characters that still lack `TeamOutfit` material.

- [ ] **Step 3: Implement no-cost overlay strategy**

If the original GLB has no separable material, add a small overlay mesh in Blender or Unity with semantic material:

```text
TeamOutfitPrimary
```

Rules:

- Overlay must cover clothing/torso only.
- Overlay must not cover face, hair, glasses, scarf, horse, tower, or crown.
- Overlay must be subtle enough not to destroy identity.
- If a character has complex clothing, prefer a small torso badge/shirt layer over recoloring the entire mesh.

- [ ] **Step 4: Reimport or update prefab safely**

For each prefab updated:

```text
game/Assets/Resources/CustomPieces/<PrefabName>.prefab
game/Assets/Resources/CustomPieces/<PrefabName>_Assets/selected.glb
```

Keep older assets in place unless the new file is clearly the active `selected.glb`.

- [ ] **Step 5: Verify white/black recolor with runtime probe**

Use Unity MCP to instantiate each custom piece twice through `PieceFactory`, one white and one black. Expected:

```text
No TeamBase generated: True
White outfit light: True
Black outfit dark: True
```

- [ ] **Step 6: Update workflow docs**

In `docs/design/custom-piece-generation-workflow.md`, add a note:

```markdown
During overnight polish, every active character must either expose a real clothing
material with a `TeamOutfit*` name or receive a small non-destructive overlay
that can be recolored by team.
```

---

## Task 4: Base-Free Selection And Invisible Support

**Files:**
- Modify: `game/Assets/Scripts/View/PieceFactory.cs`
- Modify: `game/Assets/Tests/EditMode/PieceFactoryTests.cs`
- Modify: `game/Assets/Tests/EditMode/CustomPieceVisualContractTests.cs`
- Potential create: `game/Assets/Scripts/View/PieceSelectionCollider.cs`
- Potential create: `game/Assets/Tests/EditMode/PieceSelectionColliderTests.cs`

- [ ] **Step 1: Add test that custom runtime pieces have no TeamBase**

Add to `CustomPieceVisualContractTests.cs`:

```csharp
[TestCase(ChessPieceKind.Pawn, "Pawn_Mathwidu_v3b")]
[TestCase(ChessPieceKind.Rook, "Rook_Alex")]
[TestCase(ChessPieceKind.Knight, "Knight_Gustavo")]
[TestCase(ChessPieceKind.Bishop, "Bishop_Rafael")]
[TestCase(ChessPieceKind.Queen, "Queen_Marta")]
[TestCase(ChessPieceKind.King, "King_Ricardo_Carioca")]
public void PieceFactory_CustomPieceDoesNotGenerateTeamBase(ChessPieceKind kind, string prefabName)
{
    GameObject rig = new GameObject("Base Free Probe");
    try
    {
        PieceFactory factory = rig.AddComponent<PieceFactory>();
        factory.ConfigureCustomPrefab(kind, Resources.Load<GameObject>($"CustomPieces/{prefabName}"));

        PieceView piece = factory.CreatePiece(
            new VisualPieceState(BoardSquare.FromAlgebraic("a2"), ChessSide.White, kind),
            Vector3.zero,
            rig.transform);

        Assert.IsNull(piece.transform.Find("TeamBase"), prefabName);
        Assert.IsNotNull(piece.GetComponent<Collider>(), prefabName);
    }
    finally
    {
        Object.DestroyImmediate(rig);
    }
}
```

- [ ] **Step 2: Verify current result**

Run focused Unity MCP assertions. Expected: pass for runtime `TeamBase`; collider should exist.

- [ ] **Step 3: Add invisible support if click area is too weak**

If manual or automated probes show selection is hard, create `PieceSelectionCollider.cs`:

```csharp
using UnityEngine;

public sealed class PieceSelectionCollider : MonoBehaviour
{
    [SerializeField] private float radius = 0.36f;
    [SerializeField] private float height = 1.35f;

    public void EnsureCollider()
    {
        CapsuleCollider capsule = GetComponent<CapsuleCollider>();
        if (capsule == null)
        {
            capsule = gameObject.AddComponent<CapsuleCollider>();
        }

        capsule.radius = radius;
        capsule.height = height;
        capsule.center = new Vector3(0f, height * 0.5f, 0f);
        capsule.isTrigger = false;
    }
}
```

- [ ] **Step 4: Wire only if needed**

If `PieceSelectionCollider` is created, call `EnsureCollider()` from `PieceFactory.CreatePiece` after `AddCollider(root)`.

- [ ] **Step 5: Manual acceptance**

In Play Mode:

- select a white pawn without visible base;
- select a black pawn after camera rotation;
- confirm legal move highlights still appear;
- confirm clicking a character does not require clicking exactly on the feet.

---

## Task 5: Per-Piece Movement Style Library

**Files:**
- Create: `game/Assets/Scripts/View/PieceMovementStyle.cs`
- Create: `game/Assets/Scripts/View/PieceMovementStyleLibrary.cs`
- Modify: `game/Assets/Scripts/View/PieceView.cs`
- Modify: `game/Assets/Scripts/View/PieceMotionSettings.cs`
- Create: `game/Assets/Tests/EditMode/PieceMovementStyleLibraryTests.cs`
- Modify: `game/Assets/Tests/EditMode/PieceViewTests.cs`
- Modify: `docs/design/capture-animation-roadmap.md`

- [ ] **Step 1: Write failing tests for style lookup**

Create `PieceMovementStyleLibraryTests.cs`:

```csharp
using NUnit.Framework;

public class PieceMovementStyleLibraryTests
{
    [TestCase(ChessPieceKind.Pawn, "GroundedWalk")]
    [TestCase(ChessPieceKind.Rook, "HeavyHop")]
    [TestCase(ChessPieceKind.Knight, "ArcingLJump")]
    [TestCase(ChessPieceKind.Bishop, "RitualStride")]
    [TestCase(ChessPieceKind.Queen, "ConfidentWalk")]
    [TestCase(ChessPieceKind.King, "AuthoritativeSteps")]
    public void GetStyle_ReturnsExpectedStyleName(ChessPieceKind kind, string expectedName)
    {
        PieceMovementStyle style = PieceMovementStyleLibrary.GetStyle(kind);
        Assert.AreEqual(expectedName, style.Name);
    }

    [Test]
    public void AllStyles_FinishExactlyAtDestination()
    {
        foreach (ChessPieceKind kind in System.Enum.GetValues(typeof(ChessPieceKind)))
        {
            PieceMovementStyle style = PieceMovementStyleLibrary.GetStyle(kind);
            Assert.AreEqual(1f, style.RootProgressAt(1f), 0.001f, kind.ToString());
        }
    }
}
```

- [ ] **Step 2: Verify RED**

Run focused EditMode test. Expected: `PieceMovementStyleLibrary` does not exist.

- [ ] **Step 3: Create `PieceMovementStyle.cs`**

```csharp
using UnityEngine;

public readonly struct PieceMovementStyle
{
    public PieceMovementStyle(
        string name,
        float duration,
        float stepHeight,
        float bodySway,
        float strideCycles,
        float hopHeight,
        float leanAngle)
    {
        Name = name;
        Duration = duration;
        StepHeight = stepHeight;
        BodySway = bodySway;
        StrideCycles = strideCycles;
        HopHeight = hopHeight;
        LeanAngle = leanAngle;
    }

    public string Name { get; }
    public float Duration { get; }
    public float StepHeight { get; }
    public float BodySway { get; }
    public float StrideCycles { get; }
    public float HopHeight { get; }
    public float LeanAngle { get; }

    public float RootProgressAt(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * t * (t * (t * 6f - 15f) + 10f);
    }
}
```

- [ ] **Step 4: Create `PieceMovementStyleLibrary.cs`**

```csharp
public static class PieceMovementStyleLibrary
{
    public static PieceMovementStyle GetStyle(ChessPieceKind kind)
    {
        switch (kind)
        {
            case ChessPieceKind.Rook:
                return new PieceMovementStyle("HeavyHop", 0.74f, 0.02f, 0.006f, 0.7f, 0.16f, 4.5f);
            case ChessPieceKind.Knight:
                return new PieceMovementStyle("ArcingLJump", 0.86f, 0.02f, 0.01f, 0.9f, 0.28f, 8f);
            case ChessPieceKind.Bishop:
                return new PieceMovementStyle("RitualStride", 0.82f, 0.035f, 0.014f, 1.2f, 0.06f, 3.5f);
            case ChessPieceKind.Queen:
                return new PieceMovementStyle("ConfidentWalk", 0.78f, 0.028f, 0.012f, 1.1f, 0.04f, 2.5f);
            case ChessPieceKind.King:
                return new PieceMovementStyle("AuthoritativeSteps", 0.84f, 0.026f, 0.01f, 0.95f, 0.035f, 2.2f);
            default:
                return new PieceMovementStyle("GroundedWalk", 0.82f, 0.045f, 0.018f, 1.55f, 0.02f, 3.2f);
        }
    }
}
```

- [ ] **Step 5: Add test for knight arc and rook hop**

Append:

```csharp
[Test]
public void Knight_UsesHigherArcThanPawn()
{
    PieceMovementStyle pawn = PieceMovementStyleLibrary.GetStyle(ChessPieceKind.Pawn);
    PieceMovementStyle knight = PieceMovementStyleLibrary.GetStyle(ChessPieceKind.Knight);

    Assert.Greater(knight.HopHeight, pawn.HopHeight);
    Assert.AreEqual("ArcingLJump", knight.Name);
}

[Test]
public void Rook_UsesHeavyHopWithLowStride()
{
    PieceMovementStyle rook = PieceMovementStyleLibrary.GetStyle(ChessPieceKind.Rook);

    Assert.AreEqual("HeavyHop", rook.Name);
    Assert.Less(rook.StrideCycles, 1f);
    Assert.Greater(rook.HopHeight, 0.1f);
}
```

- [ ] **Step 6: Wire style into `PieceView.MoveWithWalk`**

At the start of `MoveWithWalk`, derive style:

```csharp
PieceMovementStyle style = PieceMovementStyleLibrary.GetStyle(Kind);
PieceMotionSettings effectiveSettings = new PieceMotionSettings(
    style.Duration,
    style.StepHeight,
    style.LeanAngle,
    settings.CaptureDuration,
    style.StrideCycles,
    style.BodySway,
    settings.TorsoBobHeight);
```

Use `effectiveSettings` for `duration`, `EvaluateWalkPose`, and `modularRig.ApplyWalk`.

- [ ] **Step 7: Extend `EvaluateWalkPose` for hop height**

Add overload if needed:

```csharp
public static WalkPose EvaluateWalkPose(Vector3 start, Vector3 target, float normalizedTime, PieceMotionSettings settings, PieceMovementStyle style)
```

For `Knight` and `Rook`, visual/root vertical offset can use:

```csharp
float hop = Mathf.Sin(Mathf.Clamp01(normalizedTime) * Mathf.PI) * style.HopHeight;
```

Keep final position exact at `t >= 1f`.

- [ ] **Step 8: Verify tests**

Run focused Unity MCP assertions for:

- `PieceMovementStyleLibraryTests.GetStyle_ReturnsExpectedStyleName`
- `PieceMovementStyleLibraryTests.AllStyles_FinishExactlyAtDestination`
- `PieceViewTests.EvaluateWalkPose_StartMiddleEndKeepsBoardDestinationStable`

- [ ] **Step 9: Manual acceptance**

In Play Mode:

- pawn walks subtly;
- rook moves with heavier hop;
- knight jumps in a more arced motion;
- all pieces end centered on the target square;
- camera turn rotation still happens after move completion.

---

## Task 6: Selected-Piece Sidebar Premium Polish

**Files:**
- Modify: `game/Assets/Scripts/UI/GameHud.cs`
- Modify: `game/Assets/Scripts/UI/SelectedPiecePreviewController.cs`
- Modify: `game/Assets/Scripts/Domain/CharacterProfile.cs`
- Modify: `game/Assets/Scripts/Domain/CharacterProfileCatalog.cs`
- Modify: `game/Assets/Tests/EditMode/GameHudTests.cs`
- Modify: `game/Assets/Tests/EditMode/SelectedPiecePreviewControllerTests.cs`
- Modify: `game/Assets/Tests/EditMode/CharacterProfileCatalogTests.cs`

- [ ] **Step 1: Add profile fields for role and animation notes**

Extend `CharacterProfile` constructor with:

```csharp
string movementStyle,
string captureConcept
```

Add properties:

```csharp
public string MovementStyle { get; }
public string CaptureConcept { get; }
```

- [ ] **Step 2: Update catalog data**

Examples:

```csharp
"Grounded walk",
"Adaga curta em captura futura"
```

For Gustavo:

```csharp
"Arcing L jump",
"Relincho e salto em captura futura"
```

- [ ] **Step 3: Update profile tests**

Add:

```csharp
Assert.AreEqual("Grounded walk", CharacterProfileCatalog.GetProfile(ChessPieceKind.Pawn).MovementStyle);
Assert.IsTrue(CharacterProfileCatalog.GetProfile(ChessPieceKind.Knight).CaptureConcept.Contains("Relincho"));
```

- [ ] **Step 4: Adjust sidebar panel sizing**

In `GameHud.RebuildInterface`, adjust selected panel constants:

```csharp
selectedPiecePanel = CreatePanel(... new Vector2(390f, 700f), panelStrongColor);
selectedPiecePreviewImage = CreateRawImage(... new Vector2(358f, 360f), Color.white);
```

Keep text below preview with enough vertical spacing.

- [ ] **Step 5: Add movement/capture text fields**

Add two `Text` fields:

```csharp
private Text selectedPieceMovementText;
private Text selectedPieceCaptureText;
```

Populate in `RefreshSelectedPiecePanel`:

```csharp
selectedPieceMovementText.text = $"Movimento: {profile.MovementStyle}";
selectedPieceCaptureText.text = $"Captura futura: {profile.CaptureConcept}";
```

- [ ] **Step 6: Improve preview fit safety**

In `SelectedPiecePreviewController`, increase margin if cuts still happen:

```csharp
private const float FitSafetyMargin = 1.28f;
private const float TargetPreviewHeight = 1.55f;
```

Expected visual effect: the whole character appears, with a little empty space around it.

- [ ] **Step 7: Add preview test for wide/tall character**

Append to `SelectedPiecePreviewControllerTests.cs`:

```csharp
[Test]
public void ShowPiece_DefaultCameraFitsWideAndTallCharacter()
{
    GameObject owner = new GameObject("Preview Owner");
    GameObject source = GameObject.CreatePrimitive(PrimitiveType.Cube);
    try
    {
        source.transform.localScale = new Vector3(1.6f, 3.2f, 0.8f);
        RawImage image = owner.AddComponent<RawImage>();
        SelectedPiecePreviewController preview = owner.AddComponent<SelectedPiecePreviewController>();
        PieceView piece = source.AddComponent<PieceView>();
        piece.Initialize(new VisualPieceState(BoardSquare.FromAlgebraic("d1"), ChessSide.White, ChessPieceKind.Queen));

        preview.Configure(image);
        preview.ShowPiece(piece);

        Assert.GreaterOrEqual(preview.CurrentZoom, 1.6f);
        Assert.LessOrEqual(preview.CurrentZoom, 8.5f);
        Assert.IsTrue(preview.HasPreview);
    }
    finally
    {
        Object.DestroyImmediate(owner);
        Object.DestroyImmediate(source);
    }
}
```

- [ ] **Step 8: Verify tests**

Run focused Unity MCP assertions or EditMode suite for:

- `CharacterProfileCatalogTests`
- `SelectedPiecePreviewControllerTests`
- `GameHudTests`

- [ ] **Step 9: Manual acceptance**

Select each type of piece and confirm:

- no full-body crop in preview;
- text is readable;
- no overlapping labels;
- movement/capture info appears;
- zoom and rotation still work.

---

## Task 7: Capture Animation Architecture Skeleton

**Files:**
- Create: `game/Assets/Scripts/View/CaptureAnimationStyle.cs`
- Create: `game/Assets/Scripts/View/CaptureAnimationStyleLibrary.cs`
- Create: `game/Assets/Scripts/View/CaptureAnimationController.cs`
- Create: `game/Assets/Tests/EditMode/CaptureAnimationStyleLibraryTests.cs`
- Modify: `docs/design/capture-animation-roadmap.md`

- [ ] **Step 1: Write tests for capture style ideas**

Create `CaptureAnimationStyleLibraryTests.cs`:

```csharp
using NUnit.Framework;

public class CaptureAnimationStyleLibraryTests
{
    [TestCase(ChessPieceKind.Pawn, "DaggerLunge")]
    [TestCase(ChessPieceKind.Rook, "TowerCrush")]
    [TestCase(ChessPieceKind.Knight, "HorseLeap")]
    [TestCase(ChessPieceKind.Bishop, "PrayerBeam")]
    [TestCase(ChessPieceKind.Queen, "RoyalSlash")]
    [TestCase(ChessPieceKind.King, "OpenHandStrike")]
    public void GetStyle_ReturnsExpectedCaptureStyle(ChessPieceKind kind, string expectedName)
    {
        CaptureAnimationStyle style = CaptureAnimationStyleLibrary.GetStyle(kind);
        Assert.AreEqual(expectedName, style.Name);
    }

    [Test]
    public void AllCaptureStyles_StayShortEnoughForChessFlow()
    {
        foreach (ChessPieceKind kind in System.Enum.GetValues(typeof(ChessPieceKind)))
        {
            CaptureAnimationStyle style = CaptureAnimationStyleLibrary.GetStyle(kind);
            Assert.LessOrEqual(style.Duration, 0.95f, kind.ToString());
        }
    }
}
```

- [ ] **Step 2: Verify RED**

Run focused tests. Expected: capture style types do not exist.

- [ ] **Step 3: Create `CaptureAnimationStyle.cs`**

```csharp
public readonly struct CaptureAnimationStyle
{
    public CaptureAnimationStyle(string name, float duration, float impactAtNormalizedTime, string futureClipName)
    {
        Name = name;
        Duration = duration;
        ImpactAtNormalizedTime = impactAtNormalizedTime;
        FutureClipName = futureClipName;
    }

    public string Name { get; }
    public float Duration { get; }
    public float ImpactAtNormalizedTime { get; }
    public string FutureClipName { get; }
}
```

- [ ] **Step 4: Create `CaptureAnimationStyleLibrary.cs`**

```csharp
public static class CaptureAnimationStyleLibrary
{
    public static CaptureAnimationStyle GetStyle(ChessPieceKind kind)
    {
        switch (kind)
        {
            case ChessPieceKind.Rook:
                return new CaptureAnimationStyle("TowerCrush", 0.82f, 0.62f, "Capture_Rook_TowerCrush");
            case ChessPieceKind.Knight:
                return new CaptureAnimationStyle("HorseLeap", 0.88f, 0.58f, "Capture_Knight_HorseLeap");
            case ChessPieceKind.Bishop:
                return new CaptureAnimationStyle("PrayerBeam", 0.78f, 0.55f, "Capture_Bishop_PrayerBeam");
            case ChessPieceKind.Queen:
                return new CaptureAnimationStyle("RoyalSlash", 0.76f, 0.52f, "Capture_Queen_RoyalSlash");
            case ChessPieceKind.King:
                return new CaptureAnimationStyle("OpenHandStrike", 0.7f, 0.5f, "Capture_King_OpenHandStrike");
            default:
                return new CaptureAnimationStyle("DaggerLunge", 0.64f, 0.5f, "Capture_Pawn_DaggerLunge");
        }
    }
}
```

- [ ] **Step 5: Create skeleton controller without changing gameplay**

Create `CaptureAnimationController.cs`:

```csharp
using System.Collections;
using UnityEngine;

public sealed class CaptureAnimationController : MonoBehaviour
{
    public IEnumerator PlayCapture(PieceView attacker, PieceView captured)
    {
        if (attacker == null || captured == null)
        {
            yield break;
        }

        CaptureAnimationStyle style = CaptureAnimationStyleLibrary.GetStyle(attacker.Kind);
        CharacterAnimationDriver driver = attacker.GetComponentInChildren<CharacterAnimationDriver>();
        if (driver != null)
        {
            driver.TryPlay(style.FutureClipName);
        }

        float elapsed = 0f;
        while (elapsed < style.Duration)
        {
            elapsed += Time.deltaTime > 0f ? Time.deltaTime : style.Duration;
            yield return null;
        }
    }
}
```

Do not wire it into the live capture flow in this task unless existing capture code already has a clean extension point.

- [ ] **Step 6: Verify tests**

Run:

```bash
git diff --check
```

Use Unity MCP focused assertions for `CaptureAnimationStyleLibraryTests`.

- [ ] **Step 7: Update roadmap**

Add a section to `docs/design/capture-animation-roadmap.md`:

```markdown
### Fase 3.1: Capture Style Contracts

Each piece now has a named capture style contract. These names are not final
clips yet; they are stable targets for future Blender/Unity animation work.
```

---

## Task 8: Final Validation And Report

**Files:**
- Create/Modify: `docs/design/overnight-character-polish-report.md`

- [ ] **Step 1: Run whitespace gate**

Run:

```bash
git diff --check
```

Expected: exit code 0.

- [ ] **Step 2: Run Python tests**

Run:

```bash
python3 -m unittest tools.blender.tests.test_mathwidu_v3b_candidate -v
python3 -m unittest tools.character_pipeline.tests.test_audit_custom_pieces -v
```

Expected: all tests pass.

- [ ] **Step 3: Run Unity focused tests by MCP**

Use `Unity_RunCommand` to call focused tests for:

- `CustomPieceCoverageTests.InitialBoard_UsesCustomVisualForEveryPiece`
- `CustomPieceVisualContractTests`
- `TeamOutfitApplierTests`
- `PieceMovementStyleLibraryTests`
- `PieceViewTests`
- `SelectedPiecePreviewControllerTests`
- `CaptureAnimationStyleLibraryTests`

Expected: all focused assertions pass.

- [ ] **Step 4: Check Unity console**

Use `Unity_GetConsoleLogs` for warnings/errors.

Expected:

```text
errorCount: 0
```

Warnings are acceptable only if documented and unrelated to this work.

- [ ] **Step 5: Optional scene capture**

If Unity Scene/Game view capture is stable, capture the scene or camera through MCP and store paths in the report.

- [ ] **Step 6: Write report**

Create `docs/design/overnight-character-polish-report.md` with:

```markdown
# Overnight Character Polish Report

Date: 2026-06-07

## Summary

## Changed Files

## Validation

## Manual QA Needed

## Stop Rules Triggered

## Next Recommended Phase
```

- [ ] **Step 7: Final git status**

Run:

```bash
git status --short
git diff --stat
```

Include the summary in the final chat response.

---

## Task 9: Future Rig Pipeline Documentation

**Files:**
- Modify: `docs/design/professional-rigging-animation-roadmap.md`
- Modify: `docs/design/custom-piece-generation-workflow.md`
- Potential create: `docs/design/codex-blender-unity-rigging-skill-outline.md`

- [ ] **Step 1: Document the no-cost preferred rig route**

Add:

```markdown
## Codex + Blender + Unity no-cost rig route

1. Build or clean character in Blender.
2. Keep limbs, torso, head, hair, clothing, glasses and props separable.
3. Create an A-pose or neutral stance.
4. Add named bones or modular control transforms.
5. Export GLB/FBX into Unity.
6. Validate with `CharacterVisualContract`.
7. Animate procedurally first, then replace with Animator clips when available.
```

- [ ] **Step 2: Document AAA reality check**

Add:

```markdown
AAA quality means production quality comparable to large professional studios:
specialized sculpting, retopology, UVs, textures, rigging, animation, lighting,
and many review passes. Codex can automate parts of this pipeline, but a single
overnight no-cost run should target "premium stylized indie" rather than full AAA.
```

- [ ] **Step 3: Define next skill outline**

Create `docs/design/codex-blender-unity-rigging-skill-outline.md`:

```markdown
# Codex Blender Unity Rigging Skill Outline

Purpose: create a local reusable workflow for stylized chess characters.

Inputs:
- character name
- chess piece kind
- identity details
- required materials
- required movement style

Outputs:
- Blender scene
- GLB/FBX
- Unity prefab
- preview renders
- audit manifest
- tests/probes

Safety:
- no paid generation by default
- no internet code execution by default
- never overwrite approved assets without backup
```

- [ ] **Step 4: Verify docs**

Run:

```bash
rg -n "premium stylized indie|no-cost rig route|Codex Blender Unity Rigging Skill" docs/design
```

Expected: all phrases appear.

---

## Overnight Stop Checklist

Stop the run and report instead of continuing if any of these happen:

- A test failure points to chess rules rather than visual polish.
- A file outside `game/Assets`, `docs`, or `tools` would need changes.
- Unity requests paid generation.
- Blender imports or exports corrupted assets.
- A character becomes visually worse than the current approved prefab.
- The sidebar or movement polish breaks Play Mode.
- More than three consecutive attempts fail on the same blocker.

---

## Manual QA Checklist For The Morning

After overnight execution, the user should check:

- Open `Assets/Scenes/Main.unity`.
- Press Play.
- Start a new game.
- Select each kind of white piece.
- Confirm sidebar shows full model, profile, movement info and capture concept.
- Move pawn, rook, knight, bishop, queen and king at least once.
- Confirm each movement style reads differently.
- Make one capture if board state allows.
- Rotate to black turn and confirm black outfit/identity still reads.
- Confirm no custom piece has a visible generic circular base.

