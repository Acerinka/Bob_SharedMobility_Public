# Bob Shared Mobility

Unity prototype for a shared-mobility in-cabin assistant experience. The project centers on Bob, an animated companion that guides onboarding, dock/menu navigation, map interactions, vehicle controls, voice commands, and lane-assist scenario prompts.

## Unity Version

- Unity `2022.3.53f1`
- Universal Render Pipeline `14.0.11`
- Input System `1.11.2`
- TextMesh Pro `3.0.7`

## Project Layout

Project-owned content lives under `Assets/_Project`:

- `Scenes`: production scenes.
- `Source`: runtime scripts.
- `Prefabs`: project prefabs and local models.
- `Art/Textures`: UI, onboarding, navigation, reference, and vehicle-control textures.
- `Art/Materials`: Bob, icon, and liquid materials.
- `Art/Shaders/ShaderGraphs`: authored Shader Graph assets.
- `Media/Audio/Voice`: voice-over clips.
- `Media/Video`: prototype video assets.
- `Settings/Rendering`: URP and profile assets.
- `Documentation`: local project and Unity template documentation assets.

External/vendor content stays outside `_Project`, including `Assets/Plugins`, `Assets/TextMesh Pro`, `Assets/Oculus`, and `Assets/Resources/DOTweenSettings.asset`.

## Main Scene

Open:

`Assets/_Project/Scenes/BobSharedMobility.unity`

## Naming Rules

See `docs/ASSET_NAMING.md`.

## Engineering Standards

See `docs/ENGINEERING_STANDARDS.md`.
