# UI Page Authoring Kit

This is the production path for adding or migrating HMI pages.

## Production Shape

Every app page should have these parts:

- `AppScreenId`: one stable enum value for navigation, analytics, diagnostics, and route lookup.
- `AppNavigationRouteTable`: one route entry describing layer, route kind, prefab, presentation mode, and production status.
- `AppScreenController`: one root controller for visibility, raycast blocking, and lifecycle.
- `AppScreenLifecycleController`: serialized hooks for page enter, exit, pause, and resume.
- `AppNavigationButton`: buttons request navigation through the service instead of calling another feature controller directly.
- `CanvasGroupPresenter`: all show/hide behavior keeps alpha, interactable, and raycast blocking synchronized.

## Assets

- Route table: `Assets/_Project/Settings/Navigation/AppNavigationRouteTable.asset`
- Screen template: `Assets/_Project/Prefabs/UI/Screens/PF_AppScreen_Template.prefab`
- Validator: `Tools/Bob Shared Mobility/Validate UI Page Architecture`

## New Page Checklist

1. Add a clear value to `AppScreenId`.
2. Duplicate `PF_AppScreen_Template`.
3. Set the duplicated root `AppScreenController.ScreenId`.
4. Add the route to `AppNavigationRouteTable.asset`.
5. Bind the screen in `AppNavigationService` or assign the prefab on the route.
6. Use `AppNavigationButton` for external navigation into the page.
7. Keep feature-local button methods inside the active page only.
8. Run `Tools/Bob Shared Mobility/Validate UI Page Architecture`.

## PNG Migration Rule

Prototype images are allowed only when the route is marked `PrototypeImage` or `Hybrid`.

Before a page can be considered production-ready:

- text should be real TMP/UI text, not baked into a screenshot;
- buttons should be real `Button` or pointer-handler controls;
- controls should have component state, disabled state, and focus/hover behavior where relevant;
- navigation should be routed through `AppNavigationService`;
- no covered foreground UI should allow pointer events to leak into map/world colliders.

## Lifecycle Semantics

- `Enter`: the screen becomes visible.
- `Exit`: the screen is closed.
- `Pause`: a blocking modal/overlay covers the current screen.
- `Resume`: the blocking modal/overlay closes and the screen becomes active again.

Use lifecycle hooks for starting page-specific animations, pausing timers, stopping polling, and refreshing visible state.

## Prefab Factory

`AppNavigationService` can instantiate a route prefab when a requested screen is not already present in the scene.

For this to work:

- the route must assign `screenPrefab`;
- the prefab root must contain `AppScreenController`;
- the prefab `AppScreenController.ScreenId` must match the route `screenId`;
- optional `dynamicScreenRoot` on `AppNavigationService` can define where runtime-created pages are parented.

## Current Migration Status

The project is now in a hybrid state:

- dock pages are explicitly registered in `AppNavigationService`;
- core map/world pointer routing is centralized;
- backdoors live in `RuntimeDiagnosticsHub`;
- onboarding still contains sprite-driven prototype pages and is intentionally reported by the validator;
- new production pages should start from the template and route table instead of scene-local wiring.
