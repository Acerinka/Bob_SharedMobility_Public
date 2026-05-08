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
