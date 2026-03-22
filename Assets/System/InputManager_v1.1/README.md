# Input Manager v1.1

## v1.1
- Replaced callback-based movement handling with a polling-based `UpdateMovementVector` helper.
- Removed the Visual Novel action map and its enable/disable methods.
- Simplified action map enable/disable by calling `FindActionMap()` directly instead of caching action map references.
- Replaced `#region` blocks with section comment headers for better readability.
- Added XML documentation comments to all helper methods.
- Removed `OnDestroy` cleanup since input callbacks are no longer used.
