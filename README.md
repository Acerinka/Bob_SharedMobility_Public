# Bob Shared Mobility

Unity prototype for a shared-mobility in-cabin assistant experience. The project centers on Bob, an animated companion that guides onboarding, dock/menu navigation, map interactions, vehicle controls, voice commands, map presentation, media controls, and lane-assist scenario prompts.

The current `main` branch is the hardened delivery branch. It includes the Bob interaction scheduling fixes, route-table navigation migration, media encoding cleanup, project governance tooling, and regression documentation needed for handoff review.

## Stack

- Unity `2022.3.53f1`
- Universal Render Pipeline `14.0.11`
- Input System `1.11.2`
- TextMesh Pro `3.0.7`
- DOTween runtime package included in project assets

## Open The Project

1. Clone the repository.
2. Open the root folder in Unity Hub with Unity `2022.3.53f1`.
3. Open `Assets/_Project/Scenes/BobSharedMobility.unity`.
4. Press Play and run the checklist in `docs/DELIVERY_REGRESSION_CHECKLIST.md`.

The base prototype runs without external API keys. Real microphone transcription is optional and requires a local OpenAI API key; see `docs/SETUP_AND_SECRETS.md`.

## Project Layout

Project-owned content lives under `Assets/_Project`:

- `Scenes`: production scenes.
- `Source`: runtime scripts and editor governance tools.
- `Source/Core`: Bob orchestration, onboarding, diagnostics, pointer routing, and project input wrappers.
- `Source/Core/Navigation`: route-table navigation, screen IDs, modal commands, dock buttons, and shared panel presentation.
- `Prefabs`: reusable scene objects and local models.
- `Art/Textures`: UI, onboarding, navigation, reference, and vehicle-control textures.
- `Art/Materials`: Bob, icon, and liquid materials.
- `Art/Shaders/ShaderGraphs`: authored Shader Graph assets.
- `Media/Audio/Voice`: voice-over clips.
- `Media/Video`: prototype video assets.
- `Settings/Rendering`: URP and profile assets.
- `Settings/Navigation`: app route tables and navigation configuration assets.
- `Documentation`: local project and Unity template documentation assets.

External and vendor content stays outside `_Project`, including `Assets/Plugins`, `Assets/TextMesh Pro`, `Assets/Oculus`, and `Assets/Resources/DOTweenSettings.asset`.

## Delivery Status

This branch has been prepared for review as `v1.1` quality:

- Bob runtime commands are centralized through request, scheduling, motion, and diagnostics paths.
- Map and Mapfull debug commands support interruption without losing Bob's visible state.
- Dock-level panels such as Volume can toggle closed on repeated activation.
- Navigation state is routed through project-owned services and route-table configuration.
- Scene persistent-call bindings have compatibility wrappers while the project continues migrating to route tables and services.
- The main video asset has been re-encoded to Unity-friendly H.264 Constrained Baseline settings.
- Project governance checks and regression docs are present in the repository.

## Documentation

- `docs/DELIVERY_REGRESSION_CHECKLIST.md`: acceptance and regression checklist.
- `docs/PROJECT_INDUSTRIALIZATION_AUDIT.md`: industrialization audit and remaining improvement register.
- `docs/SETUP_AND_SECRETS.md`: local setup, API keys, microphone, and GitHub secret guidance.
- `docs/MEDIA_ASSET_GOVERNANCE.md`: media import and encoding policy.
- `docs/RUNTIME_ARCHITECTURE.md`: runtime architecture overview.
- `docs/UI_NAVIGATION_MODEL.md`: route-table navigation model.
- `docs/PAGE_AUTHORING_KIT.md`: UI page authoring conventions.
- `docs/ENGINEERING_STANDARDS.md`: coding and project standards.
- `docs/ASSET_NAMING.md`: asset naming and folder rules.

## Governance

Before submitting changes:

1. Run Unity and validate the main scene.
2. Run the manual checklist in `docs/DELIVERY_REGRESSION_CHECKLIST.md`.
3. Keep C# scripts focused and below the project line-count guardrail unless there is a documented exception.
4. Preserve Unity `.meta` files.
5. Keep project assets under `Assets/_Project` and follow the prefix rules in `docs/ASSET_NAMING.md`.
6. Re-encode any large media assets according to `docs/MEDIA_ASSET_GOVERNANCE.md`.
7. Do not commit local API keys, Wit/Meta server tokens, Unity license files, or personal debug WAV output.

The GitHub Actions workflow in `.github/workflows/static-governance.yml` enforces static delivery checks that do not require a Unity license.

## License

MIT. See `LICENSE`.
