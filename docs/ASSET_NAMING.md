# Asset Naming

All project-owned Unity assets should live under `Assets/_Project`. Third-party packages, Unity template assets, generated package content, and vendor settings should remain outside `_Project`.

## Directory Rules

- `Source`: C# runtime code.
- `Scenes`: Unity scenes.
- `Prefabs`: reusable scene objects and local models.
- `Art/Textures`: imported raster textures and sprites.
- `Art/Materials`: material assets.
- `Art/Shaders/ShaderGraphs`: shader graph source assets.
- `Art/RenderTextures`: render texture assets.
- `Media/Audio/Voice`: voice-over audio.
- `Media/Video`: video files.
- `Settings/Rendering`: URP profiles and render pipeline settings.
- `Documentation`: project documentation assets and archived Unity template readme content.

## File Prefixes

- `PF_`: prefab.
- `MODEL_`: model import.
- `TEX_`: texture or sprite.
- `MAT_`: material.
- `SG_`: Shader Graph.
- `RT_`: render texture.
- `VO_`: voice-over audio.
- `VID_`: video.
- `REF_`: design/reference image.

## Style

- Use ASCII file names.
- Use PascalCase after the prefix: `TEX_Onboarding_Homepage01.png`.
- Use two-digit numbers for ordered assets: `Step01`, `Homepage12`, `Alt01`.
- Avoid spaces, punctuation-only distinctions, raw export names, sentence names, and temporary names like `test`, `new`, `copy`, or `final2`.
- Preserve Unity `.meta` files when moving or renaming assets.
