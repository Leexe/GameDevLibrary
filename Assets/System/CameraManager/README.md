# Camera Manager v1.2

## v1.2
- Added screen shake support via `CinemachineBasicMultiChannelPerlin` with `StartContinuousShake()`, `TriggerShake()`, and `StopShake()` methods.
- Added configurable screen shake parameters (`_defaultShakeAmplitude`, `_defaultShakeFrequency`, `_shakeResetDuration`).

## v1.1
- Added `FocusOn(Transform target)` and `ClearFocus()` methods to allow the camera to lock onto targets using Cinemachine pan/tilt adjustments.
- Integrated `PrimeTween` for smooth focus transitions.
- Added support for tracking `CinemachineCamera`, `CinemachinePanTilt`, and `CinemachineInputAxisController`.

