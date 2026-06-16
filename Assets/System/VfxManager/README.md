# VFX Manager

A performant, centralized manager for handling Unity Visual Effect Graph (VFX Graph) instances across multiple targets using `GraphicsBuffer`.

## Features
- **GraphicsBuffer Integration**: Uses `GraphicsBuffer` to pass arrays of position and scale data to the VFX Graph, allowing a single VFX Graph instance to spawn particles at multiple dynamic locations simultaneously.
- **Dynamic Target Tracking**: Tracks moving `Transform` targets and updates their positions in the GPU buffer every frame.
- **Auto-Scaling Buffers**: Automatically handles buffer capacity expansion when the number of targets exceeds the current buffer size.
- **Performance Optimized**: Disables `VisualEffect` GameObjects when no targets are active to save performance. Uses a single `VisualEffect` instance per VFX asset/ID instead of instantiating prefabs for every target.
- **PrimeTween Integration**: Supports adding targets with a timed duration, automatically removing them via `PrimeTween.Tween.Delay`.

## How it Works
The `VisualEffectsManager` binds to a `StructuredBuffer` in the VFX Graph. By default, it expects the following properties exposed in your VFX Graph:
- `SpawnPoints` (GraphicsBuffer): The buffer containing position (Vector3) and scale (float) data.
- `SpawnPointsCount` (uint): The current number of active spawn points.

### Buffer Data Structure
```hlsl
struct BufferData
{
    float3 Position; // 12 Bytes
    float Scale;     // 4 Bytes
};
```
Make sure your VFX Graph matches this 16-byte stride structure when reading from the `SpawnPoints` buffer.

## Usage

### Adding a Target
```csharp
// Adds a new target to an existing VFX ID.
VisualEffectsManager.Instance.AddVfxTarget("FireVFX", targetTransform);

// Adds a target and automatically removes it after 2 seconds.
VisualEffectsManager.Instance.AddVfxTarget("FireVFX", targetTransform, 2.0f);

// Adds a target for a new VFX asset, registering it dynamically.
VisualEffectsManager.Instance.AddVfxTarget("IceVFX", targetTransform, myIceVfxAsset);
```

### Removing a Target
```csharp
VisualEffectsManager.Instance.RemoveVfxTarget("FireVFX", targetTransform);
```

## Dependencies
- Unity VFX Graph
- PrimeTween (for timed VFX removal)
