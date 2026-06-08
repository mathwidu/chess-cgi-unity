# Character Quality V3 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the failed primitive-procedural character path with a visual-first Blender review pipeline that keeps the current good pawn active until a better, rig-ready candidate is approved.

**Architecture:** Character candidates live outside `Resources/CustomPieces` until approved. Blender generates consistent review renders and a manifest records visual/technical acceptance. Unity import refuses unapproved candidates, preserving the stable playable game.

**Tech Stack:** Unity 6.3 LTS, Blender 5.1.2, Python/bpy, C# Editor scripts, JSON manifests, existing `CharacterVisualContract`, existing `PieceFactory`.

---

## File Structure

- Create `game/Assets/Art/CharacterCandidates/Pawn_Mathwidu/v3a/character_quality_manifest.json`: source of truth for the first visual-first candidate.
- Create `tools/blender/render_character_review.py`: Blender script to render front, three-quarter, and board-scale previews for any candidate model.
- Create `tools/blender/tests/test_character_quality_manifest.py`: Python tests for manifest gates.
- Create `game/Assets/Editor/CharacterCandidateImporter.cs`: Unity Editor importer that refuses candidates without `approvedForUnity: true`.
- Modify `docs/design/custom-piece-generation-workflow.md`: add the v3 rule that primitive procedural characters cannot become final assets.
- Keep `game/Assets/Resources/CustomPieces/Pawn_Mathwidu_Redhead_v2.prefab`: active pawn until a candidate is approved.

## Task 1: Quarantine The Failed Procedural Path

**Files:**
- Modify: `docs/design/custom-piece-generation-workflow.md`
- Verify: `game/Assets/Scenes/Main.unity`

- [ ] **Step 1: Confirm the active pawn is the previous approved prefab**

Run in Unity MCP:

```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        PieceFactory factory = Object.FindFirstObjectByType<PieceFactory>();
        if (factory == null)
        {
            result.LogError("No PieceFactory found.");
            return;
        }

        SerializedObject serializedFactory = new SerializedObject(factory);
        Object pawn = serializedFactory.FindProperty("pawnPrefab").objectReferenceValue;
        result.Log("Current pawn prefab: {0}", pawn != null ? AssetDatabase.GetAssetPath(pawn) : "<none>");
    }
}
```

Expected: `Assets/Resources/CustomPieces/Pawn_Mathwidu_Redhead_v2.prefab`.

- [ ] **Step 2: Add the v3 rule to the workflow doc**

Add this section to `docs/design/custom-piece-generation-workflow.md`:

```markdown
## Regra V3: qualidade visual antes de rig

A tentativa `MathwiduPawnV2` provou que primitivas modulares conseguem criar uma hierarquia animavel, mas o resultado visual ficou abaixo do alvo e nao deve ser usado como caminho principal.

Nenhum modelo criado por primitivas simples pode substituir um personagem aprovado sem passar por gate visual no Blender. O novo fluxo e:

1. aprovar preview frontal, 3/4 e escala de tabuleiro no Blender;
2. aprovar semelhanca e qualidade visual;
3. marcar `approvedForUnity: true` no manifesto;
4. importar para Unity;
5. so entao conectar no `PieceFactory`.
```

- [ ] **Step 3: Verify no active scene reference points to the rejected modular pawn**

Run:

```bash
rg -n "Pawn_Mathwidu_ModularV2|5d0c42ff3977b4302bf536d080877b2a" game/Assets/Scenes/Main.unity
```

Expected: no output.

- [ ] **Step 4: Commit**

```bash
git add docs/design/custom-piece-generation-workflow.md game/Assets/Scenes/Main.unity
git commit -m "docs: quarantine failed primitive character path"
```

## Task 2: Create The Candidate Manifest Gate

**Files:**
- Create: `game/Assets/Art/CharacterCandidates/Pawn_Mathwidu/v3a/character_quality_manifest.json`
- Create: `tools/blender/tests/test_character_quality_manifest.py`

- [ ] **Step 1: Write the failing manifest test**

Create `tools/blender/tests/test_character_quality_manifest.py`:

```python
import json
import unittest
from pathlib import Path


class CharacterQualityManifestTests(unittest.TestCase):
    def test_mathwidu_v3a_manifest_starts_unapproved_for_unity(self):
        manifest_path = Path("game/Assets/Art/CharacterCandidates/Pawn_Mathwidu/v3a/character_quality_manifest.json")
        self.assertTrue(manifest_path.exists())

        manifest = json.loads(manifest_path.read_text(encoding="utf-8"))

        self.assertEqual("Pawn_Mathwidu_v3a", manifest["candidateId"])
        self.assertEqual("Mathwidu", manifest["personName"])
        self.assertEqual("Pawn", manifest["pieceKind"])
        self.assertEqual("visual_review_pending", manifest["visualStatus"])
        self.assertEqual("rig_review_pending", manifest["rigStatus"])
        self.assertFalse(manifest["approvedForUnity"])
        self.assertFalse(manifest["replacesActivePrefab"])
        self.assertIn("ginger curly short hair", manifest["identityChecklist"])
        self.assertIn("light ginger beard and mustache", manifest["identityChecklist"])
        self.assertIn("not primitive-built", manifest["technicalChecklist"])


if __name__ == "__main__":
    unittest.main()
```

- [ ] **Step 2: Run the test and verify it fails**

Run:

```bash
python3 -m unittest tools.blender.tests.test_character_quality_manifest -v
```

Expected: FAIL because the manifest does not exist.

- [ ] **Step 3: Create the manifest**

Create `game/Assets/Art/CharacterCandidates/Pawn_Mathwidu/v3a/character_quality_manifest.json`:

```json
{
  "candidateId": "Pawn_Mathwidu_v3a",
  "personName": "Mathwidu",
  "pieceKind": "Pawn",
  "sourceAssetPath": "game/Assets/Resources/CustomPieces/Pawn_Mathwidu_Redhead_v2_Assets/selected.glb",
  "activeFallbackPrefabPath": "game/Assets/Resources/CustomPieces/Pawn_Mathwidu_Redhead_v2.prefab",
  "candidateModelPath": "game/Assets/Art/CharacterCandidates/Pawn_Mathwidu/v3a/Pawn_Mathwidu_v3a.glb",
  "visualStatus": "visual_review_pending",
  "rigStatus": "rig_review_pending",
  "approvedForUnity": false,
  "replacesActivePrefab": false,
  "identityChecklist": [
    "ginger curly short hair",
    "light ginger beard and mustache",
    "fair skin",
    "light gray shirt",
    "beige cargo pants",
    "white sneakers",
    "adult stylized proportions"
  ],
  "technicalChecklist": [
    "not primitive-built",
    "full body visible",
    "feet visible",
    "hands visible",
    "front-facing",
    "clean board-scale silhouette",
    "rig candidate only after visual approval"
  ],
  "previewImages": {
    "front": "game/Assets/Art/CharacterCandidates/Pawn_Mathwidu/v3a/preview_front.png",
    "threeQuarter": "game/Assets/Art/CharacterCandidates/Pawn_Mathwidu/v3a/preview_three_quarter.png",
    "boardScale": "game/Assets/Art/CharacterCandidates/Pawn_Mathwidu/v3a/preview_board_scale.png"
  }
}
```

- [ ] **Step 4: Run the test and verify it passes**

Run:

```bash
python3 -m unittest tools.blender.tests.test_character_quality_manifest -v
```

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add tools/blender/tests/test_character_quality_manifest.py game/Assets/Art/CharacterCandidates/Pawn_Mathwidu/v3a/character_quality_manifest.json
git commit -m "test: add visual gate manifest for pawn candidate"
```

## Task 3: Build Blender Review Renders

**Files:**
- Create: `tools/blender/render_character_review.py`
- Output: `game/Assets/Art/CharacterCandidates/Pawn_Mathwidu/v3a/preview_front.png`
- Output: `game/Assets/Art/CharacterCandidates/Pawn_Mathwidu/v3a/preview_three_quarter.png`
- Output: `game/Assets/Art/CharacterCandidates/Pawn_Mathwidu/v3a/preview_board_scale.png`

- [ ] **Step 1: Create the review renderer script**

Create `tools/blender/render_character_review.py`:

```python
#!/usr/bin/env python3
import argparse
import json
import os
import sys

import bpy
from mathutils import Vector


def parse_args():
    argv = sys.argv
    argv = argv[argv.index("--") + 1:] if "--" in argv else []
    parser = argparse.ArgumentParser()
    parser.add_argument("--manifest", required=True)
    return parser.parse_args(argv)


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()


def look_at(obj, target):
    direction = Vector(target) - obj.location
    obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def import_model(path):
    if not os.path.exists(path):
        raise FileNotFoundError(path)
    bpy.ops.import_scene.gltf(filepath=path)
    objects = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    if not objects:
        raise RuntimeError("Imported model has no mesh objects")
    return objects


def calculate_bounds(objects):
    bounds = None
    for obj in objects:
        for corner in obj.bound_box:
            world = obj.matrix_world @ Vector(corner)
            if bounds is None:
                bounds = [world.copy(), world.copy()]
            else:
                bounds[0].x = min(bounds[0].x, world.x)
                bounds[0].y = min(bounds[0].y, world.y)
                bounds[0].z = min(bounds[0].z, world.z)
                bounds[1].x = max(bounds[1].x, world.x)
                bounds[1].y = max(bounds[1].y, world.y)
                bounds[1].z = max(bounds[1].z, world.z)
    return bounds


def setup_lighting():
    key_data = bpy.data.lights.new("ReviewKey", "AREA")
    key = bpy.data.objects.new("ReviewKey", key_data)
    bpy.context.collection.objects.link(key)
    key.location = (-2.8, -4.0, 4.2)
    key.data.energy = 650
    key.data.size = 4.5
    look_at(key, (0, 0, 0.9))

    fill_data = bpy.data.lights.new("ReviewFill", "POINT")
    fill = bpy.data.objects.new("ReviewFill", fill_data)
    bpy.context.collection.objects.link(fill)
    fill.location = (2.2, -1.5, 2.4)
    fill.data.energy = 90


def setup_camera(location, target, lens):
    camera_data = bpy.data.cameras.new("ReviewCamera")
    camera = bpy.data.objects.new("ReviewCamera", camera_data)
    bpy.context.collection.objects.link(camera)
    camera.location = location
    camera.data.lens = lens
    look_at(camera, target)
    bpy.context.scene.camera = camera
    return camera


def render(path):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    bpy.context.scene.render.engine = "BLENDER_EEVEE"
    bpy.context.scene.render.resolution_x = 1200
    bpy.context.scene.render.resolution_y = 1400
    bpy.context.scene.view_settings.view_transform = "Filmic"
    bpy.context.scene.view_settings.look = "Medium High Contrast"
    bpy.context.scene.render.filepath = path
    bpy.ops.render.render(write_still=True)


def main():
    args = parse_args()
    manifest = json.loads(open(args.manifest, "r", encoding="utf-8").read())
    clear_scene()
    objects = import_model(manifest["sourceAssetPath"])
    bounds_min, bounds_max = calculate_bounds(objects)
    center = (bounds_min + bounds_max) * 0.5
    height = max(bounds_max.z - bounds_min.z, 0.1)
    target = (center.x, center.y, bounds_min.z + height * 0.55)

    setup_lighting()
    previews = manifest["previewImages"]

    setup_camera((center.x, center.y - height * 2.8, bounds_min.z + height * 0.55), target, 60)
    render(previews["front"])

    bpy.data.objects.remove(bpy.context.scene.camera, do_unlink=True)
    setup_camera((center.x + height * 1.5, center.y - height * 2.4, bounds_min.z + height * 0.7), target, 58)
    render(previews["threeQuarter"])

    bpy.data.objects.remove(bpy.context.scene.camera, do_unlink=True)
    setup_camera((center.x + height * 2.1, center.y - height * 2.5, bounds_min.z + height * 1.35), target, 42)
    render(previews["boardScale"])


if __name__ == "__main__":
    main()
```

- [ ] **Step 2: Run the renderer**

Run:

```bash
/opt/homebrew/bin/blender --background --python tools/blender/render_character_review.py -- \
  --manifest game/Assets/Art/CharacterCandidates/Pawn_Mathwidu/v3a/character_quality_manifest.json
```

Expected: creates the three preview PNG files listed in the manifest.

- [ ] **Step 3: Inspect the previews**

Open:

```text
game/Assets/Art/CharacterCandidates/Pawn_Mathwidu/v3a/preview_front.png
game/Assets/Art/CharacterCandidates/Pawn_Mathwidu/v3a/preview_three_quarter.png
game/Assets/Art/CharacterCandidates/Pawn_Mathwidu/v3a/preview_board_scale.png
```

Expected: the current approved pawn is visible and remains visually better than the rejected procedural candidate.

- [ ] **Step 4: Commit**

```bash
git add tools/blender/render_character_review.py game/Assets/Art/CharacterCandidates/Pawn_Mathwidu/v3a
git commit -m "feat: add Blender visual review renders for character candidates"
```

## Task 4: Add Unity Import Refusal For Unapproved Candidates

**Files:**
- Create: `game/Assets/Editor/CharacterCandidateImporter.cs`
- Test: `game/Assets/Tests/EditMode/CharacterCandidateManifestTests.cs`

- [ ] **Step 1: Write the failing Unity test**

Create `game/Assets/Tests/EditMode/CharacterCandidateManifestTests.cs`:

```csharp
using NUnit.Framework;

public class CharacterCandidateManifestTests
{
    [Test]
    public void CanImportIntoResources_ReturnsFalseWhenCandidateIsNotApproved()
    {
        CharacterCandidateManifest manifest = new CharacterCandidateManifest
        {
            ApprovedForUnity = false,
            ReplacesActivePrefab = false
        };

        Assert.IsFalse(manifest.CanImportIntoResources());
    }

    [Test]
    public void CanImportIntoResources_ReturnsTrueOnlyWhenApprovedAndReplacingIsExplicit()
    {
        CharacterCandidateManifest manifest = new CharacterCandidateManifest
        {
            ApprovedForUnity = true,
            ReplacesActivePrefab = true
        };

        Assert.IsTrue(manifest.CanImportIntoResources());
    }
}
```

- [ ] **Step 2: Run the test and verify it fails**

Run:

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -quit \
  -projectPath game \
  -runTests -testPlatform EditMode \
  -testResults TestResults/editmode-character-candidate-manifest.xml \
  -logFile Logs/editmode-character-candidate-manifest.log
```

Expected: FAIL because `CharacterCandidateManifest` does not exist.

- [ ] **Step 3: Implement the manifest type**

Create `game/Assets/Editor/CharacterCandidateImporter.cs`:

```csharp
using System;
using UnityEditor;
using UnityEngine;

[Serializable]
public sealed class CharacterCandidateManifest
{
    public string candidateId;
    public string personName;
    public string pieceKind;
    public string candidateModelPath;
    public bool ApprovedForUnity;
    public bool ReplacesActivePrefab;

    public bool CanImportIntoResources()
    {
        return ApprovedForUnity && ReplacesActivePrefab;
    }
}

public static class CharacterCandidateImporter
{
    public static GameObject ImportApprovedCandidate(CharacterCandidateManifest manifest)
    {
        if (manifest == null)
        {
            throw new ArgumentNullException(nameof(manifest));
        }

        if (!manifest.CanImportIntoResources())
        {
            throw new InvalidOperationException("Character candidate is not approved for Unity import.");
        }

        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(manifest.candidateModelPath);
        if (model == null)
        {
            throw new InvalidOperationException($"Candidate model not found: {manifest.candidateModelPath}");
        }

        return model;
    }
}
```

- [ ] **Step 4: Run the test and verify it passes**

Run the same Unity EditMode command.

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add game/Assets/Editor/CharacterCandidateImporter.cs game/Assets/Tests/EditMode/CharacterCandidateManifestTests.cs
git commit -m "test: block unapproved character candidate imports"
```

## Task 5: Decide Whether To Rig Or Regenerate Mathwidu

**Files:**
- Modify: `game/Assets/Art/CharacterCandidates/Pawn_Mathwidu/v3a/character_quality_manifest.json`
- Modify: `docs/design/character-rig-audit.md`

- [ ] **Step 1: Open current pawn in Blender review**

Run:

```bash
/opt/homebrew/bin/blender game/Assets/Resources/CustomPieces/Pawn_Mathwidu_Redhead_v2_Assets/selected.glb
```

Expected: Blender opens the current visually approved pawn.

- [ ] **Step 2: Record the visual decision**

If the current pawn still beats the rejected procedural candidate, set:

```json
"visualStatus": "approved_current_visual_baseline"
```

If it is too fused or not full-body enough for rig, keep:

```json
"approvedForUnity": false
```

- [ ] **Step 3: Record the rig decision**

If the mesh has a clean humanoid silhouette, set:

```json
"rigStatus": "rig_candidate"
```

If the mesh cannot be rigged without damaging quality, set:

```json
"rigStatus": "regenerate_required"
```

- [ ] **Step 4: Update rig audit**

In `docs/design/character-rig-audit.md`, update `Pawn_Mathwidu_Redhead_v2` with one of:

```text
Decisao v3: preserve current visual and attempt rig cleanup.
```

or:

```text
Decisao v3: regenerate from clean approved concept because rig cleanup would reduce visual quality.
```

- [ ] **Step 5: Commit**

```bash
git add game/Assets/Art/CharacterCandidates/Pawn_Mathwidu/v3a/character_quality_manifest.json docs/design/character-rig-audit.md
git commit -m "docs: record Mathwidu v3 rig decision"
```

## Task 6: Only If Needed, Generate A Better Concept Before Mesh

**Files:**
- Create: `game/Assets/Art/CharacterCandidates/Pawn_Mathwidu/v3b/concept_prompt.md`
- Create: `game/Assets/Art/CharacterCandidates/Pawn_Mathwidu/v3b/character_quality_manifest.json`

- [ ] **Step 1: Create the concept prompt**

Create `game/Assets/Art/CharacterCandidates/Pawn_Mathwidu/v3b/concept_prompt.md`:

```text
Create a clean full-body front-view concept image for a premium stylized 3D chess pawn character based on Mathwidu. The character must be recognizable by short curly ginger/red hair, light ginger beard and mustache, fair skin, casual light gray t-shirt, beige cargo pants, and white sneakers. Use adult stylized proportions, not chibi, not bobblehead, not toy mascot. Pose: upright neutral A-pose, arms slightly away from body, hands visible, feet visible, centered, facing forward. Style: polished stylized indie game character, organic mesh-friendly shapes, readable face, soft confident expression, suitable for Blender modeling and humanoid rigging. No phone, no mirror, no room background, no base, no pedestal, no text, no logo, no blocky primitive shapes.
```

- [ ] **Step 2: Generate or draw the concept**

Use an approved image generation route or manual drawing route. Save the selected concept as:

```text
game/Assets/Art/CharacterCandidates/Pawn_Mathwidu/v3b/concept_front.png
```

Expected: full body, no crop, readable face, stronger likeness than `MathwiduPawnV2`.

- [ ] **Step 3: Do not generate mesh until concept is approved**

Set manifest:

```json
"visualStatus": "concept_review_pending",
"approvedForUnity": false
```

- [ ] **Step 4: Commit**

```bash
git add game/Assets/Art/CharacterCandidates/Pawn_Mathwidu/v3b
git commit -m "docs: prepare Mathwidu v3b concept gate"
```

## Task 7: Final Verification Gate

**Files:**
- Verify all files touched in previous tasks.

- [ ] **Step 1: Run Python tests**

```bash
python3 -m unittest discover tools/blender/tests -v
```

Expected: PASS.

- [ ] **Step 2: Run Unity EditMode tests**

```bash
/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -quit \
  -projectPath game \
  -runTests -testPlatform EditMode \
  -testResults TestResults/editmode-character-quality-v3.xml \
  -logFile Logs/editmode-character-quality-v3.log
```

Expected: PASS.

- [ ] **Step 3: Verify active pawn fallback**

Run:

```bash
rg -n "pawnPrefab:.*ab939d5e8d2b44ff2b8359b7df081895" game/Assets/Scenes/Main.unity
```

Expected: one match, confirming active pawn is `Pawn_Mathwidu_Redhead_v2`.

- [ ] **Step 4: Verify rejected modular pawn is not active**

Run:

```bash
rg -n "Pawn_Mathwidu_ModularV2|5d0c42ff3977b4302bf536d080877b2a" game/Assets/Scenes/Main.unity
```

Expected: no output.

- [ ] **Step 5: Check whitespace**

```bash
git diff --check
```

Expected: no output.

## Self-Review

- This plan preserves the current good pawn and stable delivery.
- This plan explicitly rejects primitive procedural models as final assets.
- This plan creates a visual approval gate before Unity integration.
- This plan still supports future rigging and animation through existing `CharacterVisualContract`.
- This plan does not require paid tools.
