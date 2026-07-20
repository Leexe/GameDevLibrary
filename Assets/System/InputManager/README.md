# Input Manager v1.3

## v1.4
- Renamed `VisualNovelActionMap` to `DialogueActionMap`.
- Added new input action bindings for `Zoom` and `Interact`.

## v1.3
- Replaced `UnityEvent` declarations with C# `event Action` delegates for all input events, removing the `UnityEngine.Events` dependency.
- Introduced a reusable `ActionBinding` system that encapsulates `performed`/`canceled` subscriptions, replacing individual `InputAction` fields and named handler methods.
- Consolidated action setup into `BindAction()` helper with automatic null-checking and warning logs for missing actions.
- Updated all subscribers (`MyCharacterController`, `DialogueController`, `BacklogController`, `ShootingSystem`) from `.AddListener()`/`.RemoveListener()` to `+=`/`-=` syntax.
- Added explicit `if (InputManager.Instance)` null guards in `OnDisable` across subscribers, replacing the `?.` null-conditional pattern which is incompatible with C# event operators.
- Fixed a bug in `ShootingSystem.OnDisable` where `OnChangeGun` was incorrectly calling `AddListener` instead of `RemoveListener`.

## v1.2
- Modernized to an event-driven architecture using `.performed` and `.canceled` callbacks for all discrete inputs, guaranteeing no dropped input frames.
- Replaced continuous polling `WasPressedThisFrame()` in `Update()` with proper event subscriptions.
- Grouped continuous polling strictly to `UpdateContinuousInputs()` (e.g. Movement vector reading).
- Refactored event subscriptions to use dedicated methods instead of inline lambdas to prevent garbage collection allocations.
- Re-implemented `UnsubscribeEvents` in `OnDisable` for proper cleanup to prevent memory leaks.
- Fixed action fetching to strictly use the assigned `InputActions` asset rather than the global `InputSystem.actions`.
- Added null-conditional safety checks across enable/disable map methods.

## v1.1
- Replaced callback-based movement handling with a polling-based `UpdateMovementVector` helper.
- Removed the Visual Novel action map and its enable/disable methods.
- Simplified action map enable/disable by calling `FindActionMap()` directly instead of caching action map references.
- Replaced `#region` blocks with section comment headers for better readability.
- Added XML documentation comments to all helper methods.
- Removed `OnDestroy` cleanup since input callbacks are no longer used.
