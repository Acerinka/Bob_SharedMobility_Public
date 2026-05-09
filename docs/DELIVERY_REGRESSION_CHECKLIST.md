# Delivery Regression Checklist

This checklist captures the current delivery hardening pass for Bob Shared Mobility.

## Delivery Blockers Closed

- Worktree changes are intended to be committed as one delivery hardening change set.
- Dock scene bindings now route through `AppNavigationService` / `DockNavigationManager` request contracts.
- Direct dock panel mutation is restricted to `DockPanelController.Apply...` methods used by navigation owners.
- `VID_BobCompressed.mp4` was re-encoded in place while preserving the existing path and `.meta` reference.

## Video Acceptance

Validated with `ffprobe` after replacement:

- `codec_name=h264`
- `profile=Constrained Baseline`
- `has_b_frames=0`
- `pix_fmt=yuv420p`
- `r_frame_rate=60/1`
- `duration=60.900000`
- file size: about 61 MB

Unity Play Mode still needs one import/playback pass to confirm the platform video backend no longer logs the timestamp/profile warning.

## Runtime Regression Paths

Run these paths in Unity Play Mode before handoff:

- `7 -> 8`: medium map to full map during Bob motion; no half-map/minimap overlap.
- `7 -> 8 -> 8`: Bob remains visible/owned correctly after repeated full-map command.
- `4 -> 8`: Volume closes/evades before full-map expansion.
- `Volume -> Volume`: second activation closes the dock panel and returns to shell state.
- `Apps -> Apps`: second activation closes the dock panel and returns to shell state.
- `Map -> Mapfull -> Map`: repeated map commands are idempotent while transitions are active.
- Runtime diagnostics: run `Project_Runtime > RuntimeDiagnosticsHub > Diagnostics/Validate Runtime Wiring`.
- Map diagnostics: run `MapViewController > Diagnostics/Validate Map Runtime State`.

## Static Checks Run

- `git diff --check`
- no project-owned C# script over 450 lines
- no `.cs` file missing a `.meta`
- zero direct input governance violations
- zero scene-wide discovery violations outside allowed bootstrap/diagnostic/editor files
- zero asset prefix violations under `Assets/_Project`
