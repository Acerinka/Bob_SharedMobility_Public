# UI Navigation Model

This project should converge toward an app-shell navigation model instead of scene-local button wiring.

## Target Pattern

For a Unity HMI or in-vehicle app, the common production shape is:

- `AppShell`: owns the permanent frame, dock, status bar, and root layers.
- `NavigationService`: receives navigation requests and decides which screen is active.
- `ScreenStack`: manages full app screens such as Home, Apps, Settings, Climate, Seats, and Map.
- `ModalLayer`: owns blocking dialogs, onboarding prompts, lane-assist prompts, and confirmations.
- `OverlayLayer`: owns transient toasts, Bob hints, route/lane overlays, and visual feedback.
- `InputRouter`: normalizes pointer, gamepad, voice, and shortcuts before feature code sees them.
- `FeatureModule`: each domain owns its view controller and state, but does not decide global navigation.

## Current Gap

The scene still has direct button-to-method wiring across many pages. That works for a prototype, but it makes these problems likely:

- hidden world objects can react through foreground UI;
- debug shortcuts are scattered across feature scripts;
- onboarding/audio flows decide navigation locally;
- app pages are activated by individual scripts instead of a central stack;
- state cannot be inspected as one clear app navigation state.

## Refactor Direction

The first industrialization pass introduces:

- a serialized `ScreenId` enum for all major app screens;
- `AppScreenController` for future screen roots that are not part of the legacy dock stack;
- an `AppNavigationService` that opens/closes screens through a single API;
- `CanvasGroupPresenter` so panel visibility always moves `alpha`, `blocksRaycasts`, and `interactable` together;
- `AppNavigationButton` for new UI buttons that should request navigation instead of calling feature methods directly;
- a single place to inspect current screen, modal, and active overlay.

The existing Dock panels are now registered as `AppScreenId` entries and still keep their old scene bindings. This is deliberate: the project can keep working while buttons are migrated from direct method calls to navigation commands.

## Current Implementation

- `Project_Runtime` owns `AppNavigationService`.
- `System_DockNavigation` cooperates with `AppNavigationService` instead of being the only global state holder.
- Dock screens are explicitly registered in the `AppNavigationService.Dock Screen Registry`; runtime code builds lookup tables from that registry.
- `DockButtonController` routes user/Bob activation through `AppNavigationService` when it is present.
- `DockPanelController` reports level-2 and level-3 panel changes back to the navigation service.
- App panels expose `screenId` and `navigationLayer` in the Inspector.
- Modal/Overlay screens can be opened through `AppNavigationService.OpenModal(...)`; visible blocking modals prevent world collider input through `SceneWorldPointerRouter`.

## Migration Rule

New navigation should use one of these routes:

- call `AppNavigationService.OpenScreen(...)` from code;
- add `AppNavigationButton` to a UI button and choose a clear `AppNavigationCommand`;
- keep feature-local methods only for logic inside the currently active screen.

Do not add new navigation by relying on GameObject names or runtime discovery. If a new app screen is added, create an `AppScreenId` entry and register the screen/panel explicitly on `Project_Runtime`.

The scene should remain visually authored in Unity, but state transitions should be owned by navigation services rather than by loose GameObject activation calls spread across feature scripts.
