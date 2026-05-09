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

Code references to serialized Bob target IDs should go through `BobTargetIds`; keep the scene strings serialized for authoring, but do not duplicate raw ID literals across runtime code.

Bob target commands should go through `BobInteractionDirector.RequestTarget(...)` when callers need an explicit command result. `GoToTarget(...)` remains a compatibility wrapper for legacy boolean callers. Command phases and command results are defined in `BobCommandTypes`. Debug shortcuts may use special diagnostic toggles only while no Bob command is in progress; otherwise they must fall back to the same command scheduling path as voice and UI requests. Immediate command execution and reset clear stale queued targets before new command ownership begins.

Direct and remote Bob targets both run the same preflight cleanup before they occupy the workspace. Keep `BobInteractionDirector.resetSceneBeforeRemoteTriggers` enabled for remote targets such as full-screen map so dock panels like volume are closed before the map expands. Runtime-critical map commands such as `Mapfull` are executed by code against `MapViewController.ActiveInstance`; scene UnityEvents can remain as authoring hooks, but they are not the only business path. `Mapfull` starts the map transition immediately and plays Bob's remote gesture in parallel; the remote delay only holds command ownership long enough for the gesture to read correctly. Keep `interruptActiveInteractionForRuntimeMapCommands` enabled so a `7 -> 8` or `4 -> 8` sequence cancels the older Bob/icon interaction before it can fire a half-map or dock-panel completion callback.

Product map target commands are idempotent. Repeating `Mapfull` while the full map is requested or transitioning must not restart Bob or reset workspace state. Repeating `Map` while the medium map is requested or transitioning follows the same rule. Development debug shortcuts keep their back/rollback ergonomics only after the map state is fully settled: stable `8` toggles full back toward medium, and stable `7` toggles medium back toward small.

Bob's home anchor is owned by `BobController.HomeWorldPosition`. Interruptions may preserve the current visual position for a follow-up direct flight, but remote workspace transitions must not rewrite the home anchor. Use `RestoreHomeAnchor(...)` when a remote transition should leave Bob in his original idle location. Feature-return callbacks must release Bob with the interaction token issued by `BobInteractionDirector`; stale tokens from canceled feature callbacks are ignored.

Remote workspace transitions must call `BobController.PrepareForRemoteInteractionAtHome()` before playing a remote gesture. This is required because direct target interactions may have hidden Bob with `ArriveAndVanish`; remote gestures cannot assume the Bob GameObject is active.

### Backdoor Location

In the Unity Hierarchy, select `Project_Runtime`. In the Inspector, open:

- `RuntimeDiagnosticsHub > Backdoor Registry`: all shortcut/backdoor entries, owners, enabled state, keyboard shortcut, and gamepad shortcut.
- `RuntimeDiagnosticsHub > Voice Command Diagnostics`: text used by the voice command injection backdoor.
- component context menu `Backdoor/Inject Voice Command`: sends the diagnostic voice command without microphone/API access.
- component context menu `Backdoor/Log Registry`: prints every registered backdoor to the Console.
- component context menu `Diagnostics/Validate Runtime Wiring`: validates the backdoor registry, core scene references, and Bob target registry in one pass.
- component context menu `Diagnostics/Log Runtime State`: prints the current navigation state, map state, and Bob command state in one pass.
- `BobInteractionDirector > Diagnostics/Validate Registered Targets`: checks Bob target IDs, direct target references, remote events, and duplicate shortcut keys.
- `BobInteractionDirector > Runtime Snapshot (Read Only)`: live command phase, last command result, active target, queued target, interaction token, Bob active state, and motion flags.

Voice diagnostics are split into three explicit entries:

- `Voice input shortcut`: real microphone recording shortcut, default hold `F` / gamepad South in Development.
- `Voice command injection`: direct text command injection, independent of microphone and OpenAI API key.
- `Voice debug WAV capture`: optional WAV file dump for audio debugging.

## Navigation Direction

`AppNavigationService` is the runtime owner for app-level navigation. Dock buttons and Bob-triggered shortcuts can still use their existing scene references, but they route through the service when available.

New screens should be represented by `AppScreenId` and registered explicitly on `Project_Runtime`. New generic UI buttons should prefer `AppNavigationButton` over direct scene method bindings. Feature scripts may still own internal screen behavior, but they should not decide global app navigation.

Modal and overlay screens should use `AppNavigationService.OpenModal(...)` / `CloseTopModal()` / `CloseAllModals()`. A visible blocking modal is allowed to suppress world collider interaction globally; normal dock panels rely on UI raycast priority and should only block the map where the panel actually covers it.

Dock panel commands have separate open and toggle contracts. Use `AppNavigationService.OpenDockPanel(...)` when code needs to ensure a panel is open. Use `AppNavigationService.ToggleDockPanel(...)` for bottom dock buttons and Bob dock shortcuts so the first press opens the panel and the second matching press closes it back to the shell state. The fallback `DockNavigationManager` follows the same split: `SwitchToApp(...)` is ensure-open, and `ToggleApp(...)` is the only same-target close path.

Scene-persistent button calls to `DockPanelController.OpenSpecificLevel3`, `BackToLevel2`, `OpenLevel2Menu`, or `CloseEntireApp` are compatibility request wrappers. They must route back through `AppNavigationService` / `DockNavigationManager`; only the internal `Apply...` methods are allowed to mutate dock panel state directly. This keeps older scene bindings working while preventing scene UnityEvents from bypassing the navigation contract.

Navigation code lives under `Assets/_Project/Source/Core/Navigation`.

See `docs/UI_NAVIGATION_MODEL.md` for the full app-shell and screen-stack model. See `docs/PROJECT_INDUSTRIALIZATION_AUDIT.md` for the current debt register and refactor priorities.

Media-heavy UI and video playback assets are governed by `docs/MEDIA_ASSET_GOVERNANCE.md`. Use its Editor tools before treating baked PNG screens or video playback as production-ready.

## Map Surface Contract

`MapViewController` owns the map visual state machine. `currentState` is the requested state, while `SettledState` is the last completed state. The Inspector runtime snapshot also exposes the visible map surface and queued state so transition bugs can be audited without guessing from the rendered frame.

Only one map surface should be visible during a state change. Keep `enforceExclusiveSurfaceOwnership` enabled unless a deliberate transition design calls for overlapping surfaces. This prevents the mini map LiquidIcon animation from continuing while medium or full map surfaces are opening.

State-owned map fragments such as Home/Work, route cards, and turn-distance labels are animated by `MapFragmentVisibilityPresenter` through `MapViewController`, not toggled ad hoc by their own scripts. Tune them from `System_MainApp > Item_Map_System > MapViewController > Visibility Timing`: keep `syncConfigVisibilityWithTransition` and `animateConfigVisibility` enabled, then adjust `configVisibilityTransitionDelay`, `configVisibilityFadeDuration`, and `configHiddenScaleMultiplier` to align fragment motion with `popDuration` / `retractDuration`.

The legacy `LiquidMenuItem` hierarchy may still be present on map surfaces for authored shape data, but map surface runtime ownership belongs to `MapViewController`. During every surface switch, `MapViewController` cancels LiquidMenu delayed auto-expands and child tweens so old Small/Medium/Full callbacks cannot reopen a stale surface.
