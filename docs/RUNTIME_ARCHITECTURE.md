# Runtime Architecture

The scene is organized around a small set of production-facing roots:

- `Project_Runtime`: runtime profile and diagnostics controls.
- `AppNavigationService`: current app screen, dock panel, and sub-panel state.
- `Main Camera` / `UI Camera`: render stack and world-space canvas event camera.
- `System_OnboardingFlow`: cold-start onboarding flow and startup visibility.
- `Canvas_Intro`: onboarding UI surface.
- `System_MainApp`: main HMI surface and feature modules.
- `System_VideoSources`: video emitters and media playback sources.
- `Bob_Actor`: animated companion rig.

## Startup Contract

`OnboardingFlowManager` owns the cold-start state. In `Awake`, it hides the main app and non-current onboarding panels before the first rendered frame. In `Start`, it restarts the guided flow after dependent components have initialized.

The serialized scene should match the same contract: the main app root starts inactive, the intro canvas starts active, and only the welcome panel is visible.

## Input Contract

UI controls are handled by the Unity `EventSystem` and `GraphicRaycaster`.

World-space collider controls, such as the map and liquid menu items, are routed through `SceneWorldPointerRouter`. This keeps UI priority explicit and avoids camera-level `PhysicsRaycaster` components competing with UI.

## Diagnostics Contract

Development shortcuts are centralized under `Project_Runtime` through `RuntimeDiagnosticsHub`.

The profile switch is the outer control point:

- `Development`: enables selected shortcuts and debug aids.
- `Production`: disables shortcut backdoors, debug WAV saving, and gamepad logging.

The `Backdoor Registry` list in `RuntimeDiagnosticsHub` is the Inspector-facing audit point for every non-product shortcut. Add new diagnostic entrances there before wiring feature scripts to them.

Feature scripts should not invent their own always-on debug backdoors. Add new diagnostics to `RuntimeDiagnosticsHub` so they can be audited and disabled together.

### Backdoor Location

In the Unity Hierarchy, select `Project_Runtime`. In the Inspector, open:

- `RuntimeDiagnosticsHub > Backdoor Registry`: all shortcut/backdoor entries, owners, enabled state, keyboard shortcut, and gamepad shortcut.
- `RuntimeDiagnosticsHub > Voice Command Diagnostics`: text used by the voice command injection backdoor.
- component context menu `Backdoor/Inject Voice Command`: sends the diagnostic voice command without microphone/API access.
- component context menu `Backdoor/Log Registry`: prints every registered backdoor to the Console.

Voice diagnostics are split into three explicit entries:

- `Voice input shortcut`: real microphone recording shortcut, default hold `F` / gamepad South in Development.
- `Voice command injection`: direct text command injection, independent of microphone and OpenAI API key.
- `Voice debug WAV capture`: optional WAV file dump for audio debugging.

## Navigation Direction

`AppNavigationService` is the runtime owner for app-level navigation. Dock buttons and Bob-triggered shortcuts can still use their existing scene references, but they route through the service when available.

New screens should be represented by `AppScreenId` and registered explicitly on `Project_Runtime`. New generic UI buttons should prefer `AppNavigationButton` over direct scene method bindings. Feature scripts may still own internal screen behavior, but they should not decide global app navigation.

Modal and overlay screens should use `AppNavigationService.OpenModal(...)` / `CloseTopModal()` / `CloseAllModals()`. A visible blocking modal is allowed to suppress world collider interaction globally; normal dock panels rely on UI raycast priority and should only block the map where the panel actually covers it.

Navigation code lives under `Assets/_Project/Source/Core/Navigation`.

See `docs/UI_NAVIGATION_MODEL.md` for the full app-shell and screen-stack model.
