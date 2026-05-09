# Engineering Standards

This Unity project treats runtime code as product code, not scene-local prototype glue.

## Runtime Boundaries

- `Assets/_Project/Source/Core` owns cross-feature infrastructure such as input, logging, camera lookup, pointer routing, and high-level directors.
- `Assets/_Project/Source/Core/Navigation` owns app-shell navigation, screen IDs, modal commands, and shared panel presentation helpers.
- `Assets/_Project/Source/Modules/*` owns domain behavior for one feature area.
- `Assets/_Project/Source/UI` owns reusable UI surfaces and dialogs.
- Feature scripts should call core services instead of reading global Unity state directly.

## Input

- Product input goes through `ProjectInput`.
- Gamepad button mapping goes through `GamepadButtonReader`.
- Feature scripts should express intent such as submit, cancel, restart, or voice hold instead of directly reading `Gamepad.current`, `Keyboard.current`, or `Input.GetKeyDown`.
- Pointer/clickable objects should use `EventSystem` handlers such as `IPointerClickHandler`, `IPointerDownHandler`, and `IPointerUpHandler`.

## Cameras And Pointer Routing

- Only the rendering camera named `Main Camera` should use the `MainCamera` tag.
- The `UI Camera` is selected by name through `SceneCameraProvider`, not through `Camera.main`.
- `ScenePointerRouting` owns the `EventSystem` and UI input module only.
- World-space collider input is routed by `SceneWorldPointerRouter`; do not add broad camera `PhysicsRaycaster` components as a shortcut.

## Logging

- Runtime scripts should use `ProjectLog`.
- Informational logs are compiled for Editor and development builds only.
- Warnings and errors should describe actionable configuration issues.

## UI Controllers

- Managers/directors coordinate state. They should not perform low-level pointer raycasts.
- Buttons and clickable world objects should own their own pointer event handlers.
- UI panels should use `CanvasGroup` consistently: `alpha`, `blocksRaycasts`, and `interactable` must move together.
- App-level navigation goes through `AppNavigationService` and `AppScreenId`.
- Every new app page needs a route in `Assets/_Project/Settings/Navigation/AppNavigationRouteTable.asset`.
- New page roots should start from `Assets/_Project/Prefabs/UI/Screens/PF_AppScreen_Template.prefab`.
- New generic navigation buttons should use `AppNavigationButton` with an explicit `AppNavigationCommand`; keep direct Button-to-method scene bindings only for feature-local actions inside the active screen.
- Panel show/hide animation should use `CanvasGroupPresenter` unless a feature has a stronger reason to own custom presentation.
- App screen registration must be explicit in `Project_Runtime`; runtime discovery is reserved for bootstrap fallback, not primary scene configuration.
- Modal and overlay screens should define whether they block world input through `AppScreenController.BlocksWorldInputWhenVisible`.
- App-level navigation should continue converging on the app-shell model in `docs/UI_NAVIGATION_MODEL.md`; avoid adding more direct cross-feature button wiring.

## Scene Configuration

- The main scene should expose `Project_Runtime` as the top-level runtime profile and diagnostics control point.
- Scene references should be serialized explicitly where possible.
- Runtime fallback discovery is acceptable only in core bootstrap/resolver classes.
- Debug utilities must be controlled by `RuntimeDiagnosticsHub`, disabled in release behavior, or guarded by `UNITY_EDITOR || DEVELOPMENT_BUILD`.
- Voice debugging must expose both real input shortcuts and no-microphone command injection through `RuntimeDiagnosticsHub`.

## Page Production Gate

- Run `Tools/Bob Shared Mobility/Validate UI Page Architecture` before handing off a new page.
- Run `Tools/Bob Shared Mobility/Validate Project Industrialization` before handing off broad runtime, asset, or scene-architecture changes.
- Run `Tools/Bob Shared Mobility/Assets/Log Large Media Backlog` before media-heavy UI handoff, and use `Apply Texture Import Policy` for known large texture folders.
- `PrototypeImage` routes are acceptable during migration, but they cannot be marked production-ready.
- Baked PNG screens must be converted into componentized UI before final production status.
- Direct button calls into `DockPanelController`, `DockNavigationManager`, or `MapViewController` are migration debt unless the button is strictly feature-local inside the active page.

## Bob Motion

- Bob target movement is owned by `BobInteractionDirector`; feature scripts should request targets instead of starting transform tweens on Bob directly.
- Only one Bob flight sequence may be active at a time. New target commands during flight should be queued or explicitly cancelled through the director.
- Do not call `bob.transform.DOKill()` from feature code. Use Bob controller/director APIs so `lastVanishedPos`, flying state, blend shapes, and idle hover stay coherent.
