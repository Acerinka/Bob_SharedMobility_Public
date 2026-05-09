# Media Asset Governance

This project still contains prototype-era media assets. Treat this document as the operational rulebook for media cleanup.

## Unity Editor Tools

Use these menu items before handing off runtime or UI changes:

- `Tools/Bob Shared Mobility/Assets/Log Large Media Backlog`
- `Tools/Bob Shared Mobility/Assets/Apply Texture Import Policy`
- `Tools/Bob Shared Mobility/Validate Project Industrialization`

The backlog logger reports PNG files over 8 MB and MP4 files over 50 MB. The texture policy tool manages import settings for known high-risk texture folders without moving or renaming assets.

## Texture Import Policy

- `Assets/_Project/Art/Textures/References`: reference-only images, max 1024 px, compressed.
- `Assets/_Project/Art/Textures/Onboarding`: runtime prototype UI images, max 2048 px, high-quality compressed.
- `Assets/_Project/Art/Textures/VehicleControls`: runtime prototype UI images, max 2048 px, high-quality compressed.

These settings reduce imported/runtime texture cost, but they do not shrink the source PNG files stored in Git. The long-term fix is to replace baked full-screen PNGs with componentized UI.

## Current Backlog

- 43 PNG files exceed 8 MB:
  - 19 under `Assets/_Project/Art/Textures/Onboarding`
  - 13 under `Assets/_Project/Art/Textures/VehicleControls`
  - 11 under `Assets/_Project/Art/Textures/References`
- `Assets/_Project/Media/Video/VID_BobCompressed.mp4` was re-encoded from about 92 MB to about 61 MB.

## Video Policy

Unity playback assets should be encoded as H.264 with conservative profile/level settings. For this project, the first replacement candidate is:

```text
Assets/_Project/Media/Video/VID_BobCompressed.mp4
```

The current replacement criteria:

- Keep the existing filename and `.meta` file so scene references survive.
- Use H.264 baseline or another Unity-tested profile that does not produce timestamp skew warnings.
- Keep audio only if the scene actually needs it.
- Verify playback in Unity after replacement.

Current encoded profile:

- codec: H.264 Constrained Baseline
- resolution/frame rate: 1920x1080 at 60 fps
- B-frames: 0
- pixel format: yuv420p
- audio: AAC LC, 48 kHz stereo
- data/timecode track: removed

The file should still be verified once in Unity Play Mode after import so Unity's platform-specific video backend confirms the warning is gone.
