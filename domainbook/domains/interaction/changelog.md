# Changelog

What changed in the interaction context, newest release first, in the
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/) format: one H2 per
release, written "## [1.2.0] - 2026-06-30" with " [YANKED]" appended if the
release was pulled, holding Added, Changed, Deprecated, Removed, Fixed or
Security as H3s, each of them a bullet list.

## [Unreleased]

### Added

- XR packages (XR Plugin Management, OpenXR Plugin, XR Interaction Toolkit)
  resolved into the project for the HTC Vive VR conversion phase; see
  [ADR-0001](decisions/0001-use-openxr-and-the-xr-interaction-toolkit-for-vr-mode.md).
- `XRRig` builds a code-built XR Origin (VR) at runtime when a headset is
  present, so the eye camera tracks the headset pose instead of the desktop
  `CameraController`; see [play-in-vr](features/play-in-vr.md). Desktop mode is
  unchanged when no headset is present.
