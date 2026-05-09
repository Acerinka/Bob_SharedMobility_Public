# Project Industrialization Audit

This document tracks remaining prototype debt in the Bob Shared Mobility Unity project.

## Current Runtime Shape

- `Project_Runtime` is the intended control point for diagnostics, navigation, and runtime profile.
- `AppNavigationService` owns app-level screen and modal state.
- `SceneWorldPointerRouter` owns world collider pointer routing and UI priority.
- `BobInteractionDirector` owns Bob target commands and motion sequencing.
- `MapViewController` owns map view state and map surface transitions.
- `MapFragmentVisibilityPresenter` owns state-owned map fragment visibility and fragment transition tweens.
- `RuntimeDiagnosticsHub` owns development backdoors and profile gating.

## What Is Improved

- Project assets and source are under `Assets/_Project`.
- Main app navigation has a route table and screen IDs.
- UI/world pointer priority is explicit instead of relying on competing camera raycasters.
- Backdoors are profile-gated by `RuntimeDiagnosticsHub`.
- Map transitions now support retargeting so fast `7 -> 8` input can continue from the current visual state.
- Bob target IDs now have a central code-level contract in `BobTargetIds`.
- Backdoor logging now includes the concrete Bob target shortcut registry, not only the high-level backdoor category.
- `RuntimeDiagnosticsHub` now exposes a one-click runtime wiring validation pass for backdoor entries, core references, and Bob targets.
- `RuntimeDiagnosticsHub` now exposes a one-click runtime state log for navigation, map, and Bob command state.
- `BobInteractionDirector` now exposes an Inspector runtime snapshot and Inspector-tunable command scheduling safeguards.
- Bob command phases and command request results are now explicit enums in `BobCommandTypes` instead of implicit string/boolean outcomes.
- `BobController` has been split into focused partial files for actions, flight/avoidance, and runtime simulation helpers.
- Map fragment visibility, alpha restoration, and fragment scale tweens are separated from `MapViewController`.
- `MapViewController`, `BobInteractionDirector`, `AppNavigationService`, and `RuntimeDiagnosticsHub` are now split by runtime responsibility instead of growing as monolithic scripts.
- `OnboardingFlowManager` is now split into lifecycle/click entry points, flow coroutines, and presentation/audio helpers.
- Full-map Bob commands now execute through a code-level map runtime command path instead of depending on the scene UnityEvent as the source of truth.
- Bob feature-return callbacks now use strict interaction-token validation so stale callbacks from canceled interactions cannot move Bob.
- Map debug shortcuts no longer bypass Bob command scheduling while a Bob command is already in progress.
- Map surface ownership now cancels legacy `LiquidMenuItem` delayed auto-expands so old Small/Medium/Full child callbacks cannot revive stale map surfaces.
- Code-owned `Mapfull` starts the full-map transition immediately while Bob's remote gesture plays in parallel; the remote delay no longer leaves a stale mini/half-map overlap window.
- The project industrialization validator now checks large scripts, asset prefixes, large media, raw Bob target literals, scene-wide discovery, and direct runtime input reads.
- Dock scene bindings now land on compatibility request wrappers; direct dock panel state mutation is restricted to internal `Apply...` methods used by the navigation service/manager.

## Remaining Industrialization Debt

### P0: Runtime State Ownership

`BobInteractionDirector` and `MapViewController` still coordinate through scene references and UnityEvents. This works, but the command contract is implicit. The next production-grade step is a dedicated command layer:

- `BobCommandRouter`: receives voice, debug, and UI target requests.
- `BobMotionStateMachine`: owns Bob motion phases and command cancellation policy.
- `MapStateMachine`: owns map view requests, retargeting, and transition completion.

### P1: Scene Configuration Debt

The main scene still contains large serialized object lists for map visibility and app panels. These lists are functional but hard to audit. They should migrate into authored configuration assets:

- map state config as a `ScriptableObject`;
- navigation screen registry as the route table source of truth;
- onboarding page sequence as a `ScriptableObject`.

### P1: Medium Script Ownership Risk

No project-owned script currently exceeds the 450-line warning threshold. The next risk is not raw file size, but responsibility drift inside medium-size classes:

- `OnboardingFlowManager`: startup contract, dialogue, page flow, Bob placement.
- `SceneWorldPointerRouter`: pointer prioritization, route filtering, hover state, click dispatch.
- `MapFragmentVisibilityPresenter`: state-owned fragment visibility, fragment alpha/scale restoration, transition tween creation.
- `VoiceCommandRecognizer`: recording, transport, command parsing, and injected diagnostics.

These should be split only after behavior is stable and a clear owner boundary exists, otherwise the refactor will hide bugs.

### P2: Prototype Visual Debt

Some app surfaces are still image-heavy and route entries may be `PrototypeImage`. This is acceptable for migration, but production-ready pages should be componentized UI with explicit buttons, panels, and lifecycle hooks.

Current asset scan:

- 43 PNG files exceed 8 MB: 19 in `Art/Textures/Onboarding`, 13 in `Art/Textures/VehicleControls`, and 11 in `Art/Textures/References`.
- `Assets/_Project/Media/Video/VID_BobCompressed.mp4` has been re-encoded as H.264 Constrained Baseline, with no B-frames and no timecode/data track. Verify once in Unity Play Mode after import.
- Prefix rules are clean for production assets; `Documentation` assets are intentionally excluded from prefix enforcement.

See `docs/MEDIA_ASSET_GOVERNANCE.md` for the executable cleanup policy and Editor menu tools.

### P2: Naming And Text Debt

There are still legacy scene object names and some mojibake comments in older scripts. They do not currently break runtime behavior, but they slow down maintenance and should be cleaned as a separate mechanical pass.

## Industrialization Requirements Backlog

These requirements convert the audit into executable project work. Every future cleanup should map to one of these items instead of becoming another local patch.

### R1: Map Runtime Layering

`MapViewController` must become a thin facade around map state. It should own only the active map surface, transition request policy, collider sizing, and runtime debug snapshot.

- Map surface ownership remains in `MapViewController`.
- State-owned Home/Work, distance, route-card, and companion fragments move into a dedicated presenter.
- Map state configuration should migrate from scene lists into a `ScriptableObject` once behavior is stable.
- Transition timing should be authored through one profile-style configuration, not scattered public fields.

First correction started: `MapFragmentVisibilityPresenter` now owns map fragment caching, alpha restoration, and fragment visibility tweens.

### R2: Bob Command Architecture

`BobInteractionDirector` must stop being the single place for input shortcuts, command scheduling, motion policy, remote events, and map cleanup.

- `BobCommandRouter` receives debug, voice, UI, and future HMI commands.
- `BobMotionStateMachine` owns accepted, queued, interrupted, settling, and rejected command phases.
- Target execution should consume stable `BobTargetIds`, not scene-name assumptions.
- Any command that displaces panels must define what it opens, what it restores, and what it cancels.
- Runtime-critical commands such as `Mapfull` must be code-owned; scene UnityEvents may remain as authoring hooks, but not as the only business path.

### R3: App Navigation Architecture

App pages should be routed through a navigation contract rather than direct GameObject toggles.

- Each page has an `AppScreenId`.
- Each page has a lifecycle: preparing, opening, active, closing, inactive.
- Overlays, modals, and map expansion must declare priority so lower surfaces cannot steal input.
- PNG-backed prototype pages must be marked as prototype and replaced by componentized UI over time.

### R4: Runtime Diagnostics And Backdoors

Backdoors are allowed only as first-class diagnostics, not hidden logic paths.

- All backdoors live under `RuntimeDiagnosticsHub`.
- Each entry declares shortcut, target command, profile gating, and runtime validation.
- Voice input diagnostics must be visible beside keyboard and UI triggers.
- Adding a new backdoor requires updating the validation pass.

### R5: Scene Configuration Governance

The main scene should be treated as wiring, not business logic.

- Large serialized lists move into authored config assets.
- Scene object names must describe role and layer, not temporary design labels.
- Runtime-only debug state must appear in read-only Inspector snapshots.
- New authored config must have validation or a one-click diagnostic check.

### R6: Definition Of Done For Refactors

Each industrialization pass is only done when it preserves behavior and reduces ownership ambiguity.

- The owner class has fewer responsibilities than before.
- Public Inspector fields either remain user-tunable or move to a named config asset.
- Existing scene references keep working.
- `git diff --check` passes.
- Unity compile/play validation is required when the editor is available.

### R7: Runtime Bugfix Chain Requirements

Interaction bugs must be fixed by auditing the full runtime chain, not by tuning isolated durations.

- [x] Every command path starts at a named entry point: UI, world pointer, voice, or diagnostic backdoor.
- [x] Debug shortcuts must not bypass `BobInteractionDirector` scheduling while Bob is flying, waiting for a feature return, or running a remote delay.
- [x] Feature return callbacks must carry an interaction token; stale tokens from canceled interactions must be ignored.
- [x] Reset and immediate command execution must clear stale queued targets before new command ownership begins.
- [x] Map surface changes own and cancel previous map icon callbacks before a new map surface becomes active.
- [x] Runtime-critical map commands must run through code-owned command handlers, with scene UnityEvents treated as optional authoring hooks.
- [x] Map surface changes must cancel legacy LiquidMenu delayed auto-expands before the old surface is hidden.
- [x] Code-owned remote map commands must not leave a remote-delay window where stale surfaces keep animating.
- [x] Dock navigation has separate ensure-open and toggle contracts; `SwitchToApp` opens, while `ToggleApp` is the only fallback path that closes an already-open dock app.
- [x] Every bugfix should update either this audit, `RUNTIME_ARCHITECTURE.md`, or a purpose-built diagnostic so the rule is discoverable later.

### R8: Final Bugfix Pass Checklist

This checklist is the current final-pass status after the repeated `7/8`, `4/8`, and interrupted Bob motion fixes.

- [x] `7 -> 8` during Bob motion routes through `BobInteractionDirector` instead of direct debug bypass.
- [x] Stale Bob feature callbacks cannot return Bob after a newer command has taken ownership.
- [x] Full-map commands close dock panels such as Volume before expanding the map.
- [x] Full-map commands preserve Bob's home anchor instead of rewriting it after interruption.
- [x] Home/Work and other map fragments are owned by map state config instead of scattered object toggles.
- [x] Map transition and fragment visibility timing are Inspector tunable on `MapViewController`.
- [x] Runtime diagnostics can log Bob, map, and navigation state together.
- [x] Runtime validation now warns if exclusive map surface ownership is broken.
- [x] Legacy LiquidMenu auto-expand timers are cancellable and are canceled by map surface transitions.
- [x] Full-map runtime commands interrupt active Bob/icon interactions instead of allowing the half-map icon event to complete first.
- [x] Repeated product `Mapfull` / `Map` target commands are idempotent during active transitions, while stable debug shortcuts preserve `7/8` rollback behavior.
- [x] Remote map commands now explicitly reactivate Bob at his home anchor before the remote gesture, covering the `7 -> 8 -> 8` hidden-Bob regression.
- [x] Bottom dock targets now distinguish open vs toggle: bottom buttons and Bob dock shortcuts close the active matching panel on the second press.

### R9: Whole-Project Industrialization Requirements

This is the current whole-project backlog after auditing file management, scripts, scene wiring, runtime diagnostics, and large assets.

- [x] Add a one-click project industrialization validator under `Tools/Bob Shared Mobility`.
- [x] Keep every project-owned runtime asset under `Assets/_Project`; current top-level non-project folders are vendor/runtime packages such as Oculus, DOTween, TextMesh Pro, and Unity Resources.
- [x] Enforce asset prefixes from `docs/ASSET_NAMING.md` for prefabs, models, textures, materials, shader graphs, render textures, voice audio, and video; current filename prefix scan reports zero violations.
- [x] Review all PNG files over 8 MB and record the production risk; replacement/move work is now tracked as external content authoring in `docs/MEDIA_ASSET_GOVERNANCE.md`.
- [x] Re-encode `VID_BobCompressed.mp4` as a Unity-friendly H.264 Constrained Baseline playback asset while preserving the original path and `.meta` reference.
- [x] Split `BobController` into focused partial files before it becomes another oversized runtime script.
- [x] Split scripts over 450 lines when behavior is stable; completed for `BobController`, `BobInteractionDirector`, `MapViewController`, `AppNavigationService`, `RuntimeDiagnosticsHub`, and `OnboardingFlowManager`.
- [x] Keep scene-wide discovery (`FindObjectOfType`, `FindObjectsOfType`, `Resources.FindObjectsOfTypeAll`) only in bootstrap resolvers, diagnostics, or editor validators; validator allowlist is now explicit rather than whole-folder based.
- [x] Route product input through `ProjectInput` and `GamepadButtonReader`; the validator now warns on direct runtime `Input.Get...`, `Keyboard.current`, or `Gamepad.current` use outside owners.
- [x] Replace remaining raw Bob target literals in runtime code with `BobTargetIds`.
- [x] Replace magic DOTween string IDs with named constants in controllers that own animation state.
- [x] Make delayed callbacks owned and cancellable for Bob/map/dock interaction chains; stale feature-return, delayed icon, dock, map surface, and LiquidMenu callbacks are tokened, killed, or state-owned.
- [x] Audit direct scene button bindings; remaining dock persistent calls are compatibility request wrappers and global dock/shell navigation now routes through `AppNavigationService` / dock contracts.
- [x] Treat the main scene as wiring only for current runtime code; large serialized content-list extraction is deferred to the external authoring backlog after Unity-side asset creation is available.
- [x] Keep every interaction bugfix discoverable in diagnostics, architecture docs, or this audit before marking it complete.

## External Authoring Backlog

The current code industrialization plan is closed. These items remain real project work, but they are not safe to complete from this shell-only pass because they require Unity editor asset authoring, media tooling, or visual replacement decisions.

- Replace baked large PNG prototype screens with componentized UI or smaller sliced assets, starting with `Onboarding`, `VehicleControls`, and reference-only images already listed in `docs/MEDIA_ASSET_GOVERNANCE.md`.
- Extract map state lists, onboarding page sequences, and navigation screen registry data into authored `ScriptableObject` assets once the current scene behavior is stable inside Unity.
- Continue reducing scene persistent calls only when the replacement route can be assigned and validated inside the Unity editor.

## Governance Rules Going Forward

- New global navigation must go through `AppNavigationService`.
- New debug or diagnostic entry points must be registered in `RuntimeDiagnosticsHub`.
- New Bob targets must use `BobTargetIds` when referenced from code.
- New Bob command callers should use `RequestTarget(...)` when they need to distinguish accepted, queued, duplicate, already-open, or invalid requests.
- Run `RuntimeDiagnosticsHub > Diagnostics/Validate Runtime Wiring` after adding or changing backdoors, Bob targets, or voice diagnostics.
- Use `RuntimeDiagnosticsHub > Diagnostics/Log Runtime State` before debugging interaction bugs so navigation, map, and Bob state are captured together.
- Run `Tools/Bob Shared Mobility/Assets/Log Large Media Backlog` and `Apply Texture Import Policy` before media-heavy UI handoff.
- New map mode changes should call `MapViewController.SwitchToState` or a named trigger method, not toggle GameObjects directly.
- New screens must have an `AppScreenId`, a route table entry, and a production status.
