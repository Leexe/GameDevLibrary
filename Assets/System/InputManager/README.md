# Input Manager v1.2

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
