---
id: play-in-vr
name: Play in VR
status: draft
owners: [mathwidu, RafaelAugustScherer]
terms: [vr-mode, headset, motion-controller, ray-interactor]
---

## Story

As a player with a VR headset
I want to sit at the chess board in virtual reality and pick pieces with my hands
So that the game I already have becomes an immersive match instead of a screen one

This is a plan, not built work. It is a feasibility study and a high-level
conversion route for taking the current desktop build to VR mode on HTC Vive
(PC-tethered) and Meta Quest 3 (standalone, plus PC over Link). It stays in the
interaction context because the change is mostly about how the player perceives
and controls the game — the [headset](../glossary.md) drives the view and the
[motion controller](../glossary.md) replaces the mouse — with supporting work in
presentation's render pipeline. When the route is agreed and the XR framework is
chosen, that choice earns a decision record and the rules below become the
acceptance criteria for building it.

### Feasibility study

#### Verdict

Feasible, and a good fit. Both target headsets are reachable through one code
path — Unity's OpenXR plugin — and the current project is already on the stack
Unity's XR tooling expects: Unity 6.3 (6000.3), Universal Render Pipeline
(URP) 17.3, and the new Input System (1.19), which the XR Interaction Toolkit
builds on. The game is stationary and table-scale, so it avoids the hardest VR
problem — locomotion comfort. The bulk of the effort is re-plumbing input and
the camera, plus a standalone-performance pass for the Quest.

#### The two headsets, one path

- **OpenXR is the unified target.** The OpenXR Plugin (`com.unity.xr.openxr`)
  lets one build drive many runtimes. HTC Vive is reached on the PC as an OpenXR
  runtime (SteamVR); Meta Quest 3 is reached as an OpenXR runtime both standalone
  on-device and over Link on the PC. Controllers are selected at runtime from the
  **Enabled Interaction Profiles** list, so the same build can list the **HTC Vive
  Controller Profile** and the **Meta Quest Touch (Oculus Touch) Controller
  Profile** and use whichever device is present.
- **Quest 3 is a separate build target.** It is an Android device: a standalone
  Quest build is an Android/ARM64 app, while Vive is a Windows standalone app.
  These are two player builds from one project — a Windows PC-VR build and an
  Android Quest build — differing in platform settings, not in game code.

#### What the current project already gives us

- **Colliders on pieces and squares.** `InputController` already raycasts
  `Physics.Raycast` against `PieceView` and `SquareView` colliders. The same
  colliders are what a controller ray or a grab interactor hits, so the pickable
  targets exist.
- **The scene is built in code at runtime.** `BoardView`, `PieceFactory`, and
  `GameHud` construct the board, pieces, and UI in scripts. A code-built scene is
  easier to re-anchor and re-scale for VR than a hand-laid one.
- **Custom models load through glTFast**, and URP + Shader Graph + built-in
  shaders already support single-pass instanced stereo rendering, so the piece
  models render correctly in both eyes without shader work.
- **Input System is already in.** XRI reads input through Input System actions;
  the project does not need to migrate off legacy input first.

#### What must change (the real work)

- **The camera stops being ours to move.** In VR the head-mounted display drives
  the camera every frame through a Tracked Pose Driver under an XR Origin rig.
  `CameraController`'s orbit, zoom, and swing-to-face-the-player-on-move logic no
  longer applies to the eye camera — you cannot move a camera the headset owns.
  The [perspective](../glossary.md) idea has to move from "point the camera at the
  side on move" to "re-anchor or turn the board rig", or be dropped in favour of
  the player physically turning their head.
- **Mouse picking is replaced by interactors.** `InputController`'s
  screen-raycast-on-left-click becomes an XRI interactor: point a
  [ray interactor](../glossary.md) at a piece and pull the trigger, or reach out
  and grab it directly. This is a rewrite of the input path, not a tweak.
- **The HUD must leave the screen.** `GameHud` builds a screen-space overlay
  Canvas (title, turn, status, move history, promotion prompt, start screen, and
  the selected-piece preview). XRI can only drive a **world-space** Canvas, so the
  HUD becomes a panel placed in the scene, pointed at with the controller. The
  render-texture selected-piece preview still works; only its pointer input moves
  to the controller.
- **The Quest has a tight frame budget.** A standalone mobile GPU rendering two
  eyes at 72–120 Hz is far less headroom than a PC. Post-processing and overdraw
  that are free on desktop are not on Quest. The scene is small, which helps, but
  a performance pass is required, not optional.

#### Effort and risk

- **Effort:** medium. No new gameplay or rules — `ChessGameController` and
  `ChessRulesAdapter` are untouched. The work concentrates in this context
  (input, camera) and in presentation (render settings, world-space HUD).
- **Main risks:** (1) reworking the per-turn perspective so a shared-screen idea
  still makes sense for one person in a headset; (2) Quest standalone performance;
  (3) two build targets to keep configured and validated. None are blockers.

### Conversion plan (high level)

1. **Add the XR packages and turn on OpenXR.** Through the Package Manager, add
   XR Plugin Management (`com.unity.xr.management`), the OpenXR Plugin
   (`com.unity.xr.openxr`), and the XR Interaction Toolkit
   (`com.unity.xr.interaction.toolkit`, which pulls in XR Core Utilities). Let the
   editor resolve the versions it verifies for 6000.3 — at the time of writing
   that is XRI 3.3.2, OpenXR Plugin on the 1.16.x line, and XR Core Utilities on
   the 2.5.x line. In **Project Settings → XR Plug-in Management**, enable
   **OpenXR** on both the Windows/Standalone tab (for Vive) and the Android tab
   (for Quest).

2. **Set the interaction profiles and platform settings.**
   - **Both:** under **XR Plug-in Management → OpenXR**, add the **HTC Vive
     Controller Profile** and the **Meta Quest Touch (Oculus Touch) Controller
     Profile** to Enabled Interaction Profiles.
   - **Quest 3:** switch to the **Meta Quest build platform/profile** (Unity 6.1+),
     which installs the **Unity OpenXR: Meta** package
     (`com.unity.xr.meta-openxr`) and enables the **Meta Quest feature group**.
     Confirm the defaults it sets: Graphics API **Vulkan**, Scripting Backend
     **IL2CPP**, Target Architecture **ARM64**, minimum Android **API 29**, target
     **API 32**, Stereo Rendering **Instancing**.
   - **Vive / PC-VR:** build target Windows standalone; the active OpenXR runtime
     is SteamVR. No mobile constraints, so it has performance headroom.
   - Run **XR Plug-in Management → Project Validation** on each platform tab and
     clear every flagged item.

3. **Set URP up for stereo.** Set the OpenXR **Render Mode** to **Single Pass
   Instanced** for each provider (it falls back to multi-pass where unsupported).
   Keep MSAA on the URP asset for edge quality, and stay within the XR-supported
   post-processing set — Bloom, Depth of Field, Tonemapping and colour adjustments
   work in XR, while Lens Distortion, Spatial-Temporal Post-Processing, physical
   camera, and multi-display do not. Consider fixed-foveated rendering (URP 17's
   Forward+ path supports it) as a Quest performance lever.

4. **Replace the camera with an XR Origin rig.** Swap the single main camera for
   an **XR Origin (VR)**: a Camera Offset holding the Main Camera, with a **Tracked
   Pose Driver** binding the eye pose to the headset. For a seated table game, use
   **Device** tracking-origin mode and set the eye height with **Camera Y Offset**,
   and wire a **recenter** control (`XRInputSubsystem.TryRecenter`) so the player
   can reset the board in front of their seat. Point the existing raycast/interactor
   camera reference at this new eye camera.

5. **Replace mouse picking with controller interaction.** Put a **Near-Far
   Interactor** on each controller (it unifies the near/direct and far/ray cases
   that used to need two components) and add the XRI **Default Input Actions**.
   Give each piece an interactable — an **XR Grab Interactable** for reach-and-grab,
   or an **XR Simple Interactable** for point-and-select — reusing the colliders
   `PieceView`/`SquareView` already carry. Bridge the interactor's select event to
   the existing `ChessGameController.SelectPiece` / `SelectSquare` calls, so the
   rules layer sees the same commands it does today. Keep the legal-destinations
   highlight; it already marks squares in the scene.

6. **Move the HUD to world space.** Set the HUD Canvas render mode to **World
   Space** and place it as a panel in the scene (for example beside or above the
   board). Add a **Tracked Device Graphic Raycaster** to the Canvas and swap the
   EventSystem's Standalone Input Module for the **XR UI Input Module**, so the
   controller ray drives buttons and the promotion prompt. The selected-piece
   render-texture preview stays; repoint its drag/scroll input from the mouse to
   the controller.

7. **Rework the per-turn perspective for VR.** Decide what "the view faces the
   side on move" means for one player in a headset (see Open Questions). Likely the
   board rig turns 180° between turns, or the pieces/labels reorient, rather than
   the camera moving. Retire `CameraController`'s orbit/zoom/swing on the eye
   camera; any of it that survives acts on the XR Origin or the board, not the HMD.

8. **Validate, build, and test on device.** Re-run project validation, build the
   Windows PC-VR player and the Android Quest player, and test each on its
   hardware. Do a Quest performance pass — hold the target refresh, watch draw
   calls and overdraw, and lean on single-pass instanced and foveated rendering.

## Rule: The player sees and controls the match from inside a headset

These are the acceptance criteria for when this draft is built.

```gherkin
Example: The headset drives the view
  Given VR mode is running on a connected headset
  When the player moves their head
  Then the eye camera follows the headset pose
  And the per-turn camera swing no longer moves the eye camera

Example: Pointing at a piece and pulling the trigger selects it
  Given it is the player's turn in VR mode
  When the player points the controller ray at one of their pieces and pulls the trigger
  Then that piece is selected
  And its legal destinations are highlighted, as with a mouse click

Example: The same rules layer receives the same commands
  Given a piece is selected in VR mode
  When the player points at a highlighted square and pulls the trigger
  Then the move reaches the rules through the existing select-destination command
  And the outcome matches the desktop build
```

## Rule: The interface lives in the world, not on the screen

```gherkin
Example: The HUD is a world-space panel the controller can use
  Given VR mode is running
  When the player points the controller ray at the new-game button on the HUD panel
  Then the button responds to the controller, not to a mouse
  And no screen-space overlay is shown to the headset

Example: Promotion is chosen with the controller
  Given a pawn reaches the far rank in VR mode
  When the promotion prompt appears in world space
  Then the player picks queen, rook, bishop, or knight with the controller ray
```

## Rule: The same build serves HTC Vive and Meta Quest 3

```gherkin
Example: One build reads whichever controller is present
  Given the build lists both the HTC Vive and Meta Quest Touch interaction profiles
  When the player runs it on a Vive or on a Quest 3
  Then OpenXR binds the controller of the device in use
  And input works without a separate code path per headset

Example: Quest runs standalone within its frame budget
  Given the Android build runs on Meta Quest 3 with single pass instanced rendering
  When a match is played
  Then the app holds its target refresh rate
```

## Open Questions

- **What does the per-turn perspective become in VR?** On a shared screen the
  camera swung to face the player on move. With one person in a headset, does the
  board turn between turns, do the pieces/labels reorient, or does the idea retire
  in favour of the player turning their head? This needs a design call before
  step 7 is built; it is the one open behavioural decision, and it is settled by
  agreeing the intended VR turn experience, not by more research.
- **One seat or hot-seat pass-the-headset?** The desktop build is two players on
  one screen. VR is one headset — is VR mode single-seat (one human, or versus a
  future AI), or do two players pass the headset each turn? This scopes whether the
  two-sided perspective work in step 7 is even needed.
- **Which XR framework is committed to?** The plan assumes Unity's OpenXR + XR
  Interaction Toolkit. That commitment earns a decision record once agreed, with
  the Meta all-in-one SDK considered and rejected as the alternative.

### References

Official Unity documentation the study is grounded in (Unity 6.3 / 6000.3 and the
matching package docs):

- XR overview and project setup — https://docs.unity3d.com/6000.3/Documentation/Manual/XR.html
- XR Interaction Toolkit 3.3.2 for 6000.3 — https://docs.unity3d.com/6000.3/Documentation/Manual/com.unity.xr.interaction.toolkit.html
- Near-Far Interactor — https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@3.3/manual/near-far-interactor.html
- XR Grab Interactable — https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@3.3/manual/xr-grab-interactable.html
- World-space UI setup (Tracked Device Graphic Raycaster, XR UI Input Module) — https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@3.0/manual/ui-setup.html
- XR Origin (rig, Camera Offset, tracking origin modes) — https://docs.unity3d.com/Packages/com.unity.xr.core-utils@2.5/manual/xr-origin.html
- Recenter tracking — https://docs.unity3d.com/ScriptReference/XR.XRInputSubsystem.TryRecenter.html
- HTC Vive Controller Profile — https://docs.unity3d.com/Packages/com.unity.xr.openxr@1.15/manual/features/htcvivecontrollerprofile.html
- Meta Quest support (OpenXR plugin) — https://docs.unity3d.com/Packages/com.unity.xr.openxr@1.16/manual/features/metaquest.html
- Unity OpenXR: Meta, project setup — https://docs.unity3d.com/Packages/com.unity.xr.meta-openxr@2.1/manual/get-started/project-settings.html
- Meta Quest build platform and Player defaults (6.3) — https://docs.unity3d.com/6000.3/Documentation/Manual/xr-meta-quest-build-profile.html
- URP compatibility in XR — https://docs.unity3d.com/6000.3/Documentation/Manual/xr-render-pipeline-compatibility.html
- Single-pass instanced / stereo rendering — https://docs.unity3d.com/6000.3/Documentation/Manual/SinglePassStereoRendering.html
- Foveated rendering (URP 17 Forward+) — https://docs.unity3d.com/6000.3/Documentation/Manual/xr-foveated-rendering.html
