---
status: accepted
date: 2026-08-29
---

# Use OpenXR and the XR Interaction Toolkit for VR mode

## Context and Problem Statement

[VR mode](../glossary.md) needs an XR framework: something that drives the
[headset](../glossary.md) pose onto the camera, reads [motion controller](../glossary.md)
input, and turns a pointed [ray interactor](../glossary.md) into a select
command. The two headsets on the roadmap — HTC Vive (PC-tethered) and Meta
Quest 3 (standalone, plus PC over Link) — are reached by different vendor
SDKs, but the game should not fork into two input paths to support them.

## Decision Drivers

- HTC Vive, the first target, has no vendor all-in-one SDK — it is only
  reachable through OpenXR (as a SteamVR OpenXR runtime).
- One code path should serve both target headsets rather than one per vendor.
- The project is already on the stack Unity's XR tooling expects: Unity 6.3
  (6000.3), URP 17.3, and the Input System (1.19), which the XR Interaction
  Toolkit builds on.

## Considered Options

- Unity's OpenXR Plugin + XR Interaction Toolkit (XRI).
- Meta's all-in-one SDK (Meta XR / Oculus Integration).

## Decision Outcome

Chosen option: "Unity's OpenXR Plugin + XR Interaction Toolkit". Meta's
all-in-one SDK is Quest-only and cannot reach the Vive at all, which the Vive
phase needs regardless of what is chosen for Quest later. OpenXR is the one
path both headsets share: Vive through SteamVR, Quest 3 both standalone and
over Link, each selected at runtime from the OpenXR **Enabled Interaction
Profiles** list. XRI sits on top of OpenXR and reads input through Input
System actions, which the project already uses.

### Consequences

- Good, because the Vive phase and a later Quest phase build on the same
  input and rig code — no per-vendor branch.
- Good, because OpenXR is the only way to reach the Vive, so this also settles
  the framework question for Quest without a separate decision later.
- Bad, because OpenXR's Quest support (via the `com.unity.xr.meta-openxr`
  package) needs its own project settings pass when that phase starts; this
  record does not cover it.
