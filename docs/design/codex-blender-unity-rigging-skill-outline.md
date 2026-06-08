# Codex Blender Unity Rigging Skill Outline

Purpose: create a local reusable workflow for stylized chess characters.

## Inputs

- character name
- chess piece kind
- identity details
- required outfit materials
- required movement style
- reference asset paths

## Outputs

- Blender scene
- GLB/FBX
- Unity prefab
- preview renders
- audit manifest
- EditMode tests

## Workflow

1. Copy reference images into a private local folder.
2. Generate or choose a clean full-body concept.
3. Build or clean the Blender model without paid generation by default.
4. Keep shoes, feet, legs, torso, head, hair, clothing, glasses and props separable.
5. Add A-pose or neutral stance before rigging.
6. Create named bones or modular control transforms.
7. Export to Unity as a new asset, never overwriting the approved prefab.
8. Add `CharacterVisualContract`, `CharacterAnimationDriver` and movement metadata.
9. Validate with automated tests and a manual board/sidebar pass.

## Safety

- no paid generation by default
- no internet code execution by default
- never overwrite approved assets without backup
- keep the stable delivery tag untouched
- stop if a generated character looks worse than the approved current prefab

## Quality Target

The practical target is `premium stylized indie`: recognizable likeness, clean readable silhouette, complete feet/shoes, separated team outfit surfaces, and enough rig structure for walk/idle/capture clips.
