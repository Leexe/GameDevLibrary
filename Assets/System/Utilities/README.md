# Utilities v1.1

## v1.1
- Added `PositionTweenType` (`None`, `Shake`, `Smooth`) to `SwayingObject` to support smooth sine-wave position motion in addition to procedural position shake.
- Added `_activateOnStart` toggle to `SwayingObject` for controlling automated start behavior.
- Added `DynamicLight.cs` utility script for light flickering and intensity animation.

## v1.0
- Added `SwayingObject.cs` component for procedural 3D object position shake, pendulum rotation, and scale pulsing using `PrimeTween`.
- Added `Singleton.cs` (base singleton classes: `MonoSingleton`, `PersistentMonoSingleton`).
- Added `DontDestroyOnLoad.cs`, `FMODAsyncLoader.cs`, and `SODictionaryUtility.cs`.