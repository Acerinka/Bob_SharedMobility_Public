# Engineering Standards

This Unity project treats runtime code as product code, not scene-local prototype glue.

## Runtime Boundaries

- `Assets/_Project/Source/Core` owns cross-feature infrastructure such as input, logging, camera lookup, pointer routing, and high-level directors.
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
- Scene pointer support is bootstrapped by `ScenePointerRouting`, which ensures an `EventSystem`, UI input module, and camera physics raycasters.

## Logging

- Runtime scripts should use `ProjectLog`.
- Informational logs are compiled for Editor and development builds only.
- Warnings and errors should describe actionable configuration issues.

## UI Controllers

- Managers/directors coordinate state. They should not perform low-level pointer raycasts.
- Buttons and clickable world objects should own their own pointer event handlers.
- UI panels should use `CanvasGroup` consistently: `alpha`, `blocksRaycasts`, and `interactable` must move together.

## Scene Configuration

- Scene references should be serialized explicitly where possible.
- Runtime fallback discovery is acceptable only in core bootstrap/resolver classes.
- Debug utilities must be disabled in release behavior or guarded by `UNITY_EDITOR || DEVELOPMENT_BUILD`.
