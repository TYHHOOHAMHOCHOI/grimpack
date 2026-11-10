# Unity 6.3 LTS — Breaking Changes

**Last verified:** 2026-02-13

This document tracks breaking API changes and behavioral differences between Unity 2022 LTS
(likely in model training) and Unity 6.3 LTS (current version). Organized by risk level.

## HIGH RISK — Will Break Existing Code

### Entities/DOTS API Complete Overhaul
**Versions:** Entities 1.0+ (Unity 6.0+)

```csharp
// ❌ OLD (pre-Unity 6, GameObjectEntity pattern)
public class HealthComponent : ComponentData {
    public float Value;
}

// ✅ NEW (Unity 6+, IComponentData)
public struct HealthComponent : IComponentData {
    public float Value;
}

// ❌ OLD: ComponentSystem
public class DamageSystem : ComponentSystem { }

// ✅ NEW: ISystem (unmanaged, Burst-compatible)
public partial struct DamageSystem : ISystem {
    public void OnCreate(ref SystemState state) { }
    public void OnUpdate(ref SystemState state) { }
}
```

**Migration:** Follow Unity's ECS migration guide. Major architectural changes required.

---

### Input System — Legacy Input Deprecated
**Versions:** Unity 6.0+

```csharp
// ❌ OLD: Input class (deprecated)
if (Input.GetKeyDown(KeyCode.Space)) { }

// ✅ NEW: Input System package
using UnityEngine.InputSystem;
if (Keyboard.current.spaceKey.wasPressedThisFrame) { }
```

**Migration:** Install Input System package, replace all `Input.*` calls with new API.

---

### URP/HDRP Renderer Feature API Changes
**Versions:** Unity 6.0+

```csharp
// ❌ OLD: ScriptableRenderPass.Execute signature
public override void Execute(ScriptableRenderContext context, ref RenderingData data)

// ✅ NEW: Uses RenderGraph API
public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
```

**Migration:** Update custom render passes to use RenderGraph API.

---

## MEDIUM RISK — Behavioral Changes

### Addressables — Asset Loading Returns
**Versions:** Unity 6.2+

Asset loading failures now throw exceptions by default instead of returning null.
Add proper exception handling or use `TryLoad` variants.

```csharp
// ❌ OLD: Silent null on failure
var handle = Addressables.LoadAssetAsync<Sprite>("key");
var sprite = handle.Result; // null if failed

// ✅ NEW: Throws on failure, use try/catch or TryLoad
try {
    var handle = Addressables.LoadAssetAsync<Sprite>("key");
    var sprite = await handle.Task;
} catch (Exception e) {
    Debug.LogError($"Failed to load: {e}");
}
```

---

### Physics — Default Solver Iterations Changed
**Versions:** Unity 6.0+

Default solver iterations increased for better stability.
Check `Physics.defaultSolverIterations` if you rely on old behavior.

---

## LOW RISK — Deprecations (Still Functional)

### UGUI (Legacy UI)
**Status:** Deprecated but supported
**Replacement:** UI Toolkit

UGUI still works but UI Toolkit is recommended for new projects.

---

### Legacy Particle System
**Status:** Deprecated
**Replacement:** Visual Effect Graph (VFX Graph)

---

### Old Animation System
**Status:** Deprecated
**Replacement:** Animator Controller (Mecanim)

---

## Platform-Specific Breaking Changes

### WebGL
- **Unity 6.0+**: WebGPU is now the default (WebGL 2.0 fallback available)
- Update shaders for WebGPU compatibility

### Android
- **Unity 6.0+**: Minimum API level raised to 24 (Android 7.0)

### iOS
- **Unity 6.0+**: Minimum deployment target raised to iOS 13

---

## Migration Checklist

When upgrading from 2022 LTS to Unity 6.3 LTS:

- [ ] Audit all DOTS/ECS code (complete rewrite likely needed)
- [ ] Replace `Input` class with Input System package
- [ ] Update custom render passes to RenderGraph API
- [ ] Add exception handling to Addressables calls
- [ ] Test physics behavior (solver iterations changed)
- [ ] Consider migrating UGUI to UI Toolkit for new UI
- [ ] Update WebGL shaders for WebGPU
- [ ] Verify minimum platform versions (Android/iOS)

---

**Sources:**
- https://docs.unity3d.com/6000.0/Documentation/Manual/upgrade-guides.html
- https://docs.unity3d.com/Packages/com.unity.entities@1.3/manual/upgrade-guide.html


---

## 6.3 LTS → 6.6 (6000.3 → 6000.6)

**Last verified:** 2026-11-09 (this section only; the sections above describe 6.3 LTS and were not re-verified).

Sources, all read on 2026-11-09:
- https://docs.unity.com/en-us/engine/6000.6/manual/upgrade-guides/upgrade-guide-unity64
- https://docs.unity.com/en-us/engine/6000.6/manual/upgrade-guides/upgrade-guide-unity65
- https://docs.unity.com/en-us/engine/6000.6/manual/upgrade-guides/upgrade-guide-unity66
- https://docs.unity.com/en-us/engine/6000.6/manual/whats-new/unity64 (and `unity65` under 6000.5, `unity66` under 6000.6)
- https://unity.com/releases/editor/whats-new/6000.6.3f1

### 6.4

- **URP Compatibility Mode removed for custom render passes**, and the `URP_COMPATIBILITY_MODE` scripting define is removed. Use Render Graph.
- `IVolume` is removed; use `VolumeGizmoDrawer`. SRP Core's dependency on `com.unity.modules.physics` is now soft; disabling the physics module breaks local volumes.
- `LightShadowCasterMode` values are renamed (`NonLightmappedOnly` → `ShadowMask`, `Everything` → `DistanceShadowMask`); scripts are auto-updated.
- **`Object.Destroy` now calls `OnDisable` on components of all descendant GameObjects**, as `DestroyImmediate` does. Before, only direct children. Remove any manual workaround to avoid duplicate calls.
- The Asset Database reimports dependents only when a dependency's import artifact changes. `ScriptedImporter` and `AssetPostprocessor` code may need `DependsOnSourceAsset` or `DependsOnArtifact`.
- PVRTC texture targeting is removed.
- `InstanceID` is deprecated in favour of `EntityId` (see 6.5).
- Project Auditor is built into the Editor; some analyzer types became internal.

### 6.5

- **`InstanceID` → `EntityId`.** Int-based APIs produce compile errors (for example `GetInstanceID()` → `GetEntityId()`). `EntityId` is 8 bytes; downcasting to `int` loses version bits. `EntityId.ToString()` strings were 32-bit in 6.2 to 6.4 and are 64-bit `ulong` in 6.5, so do not `int.Parse` them. Do not use `GetHashCode()` as a dictionary key. Check any `StructLayout.Explicit` or unsafe reinterpretation involving `EntityId`.
- **Obsolete GameObject/Component shortcuts removed**: `.rigidbody`, `.rigidbody2D`, `.camera`, `.light`, `.renderer`, `.collider`, `.collider2d` and others, plus `AddComponent(string)`. Use `GetComponent<T>()` and `AddComponent<T>()`.
- **`LowLevelPhysics2D` is renamed `PhysicsCore2D`**; the namespace changes from `UnityEngine.LowLevelPhysics2D` to `Unity.U2D.Physics`; the settings asset is renamed Physics Core Settings 2D. Projects from 6.3/6.4 are migrated automatically. The guide gives a `#if UNITY_6000_5_OR_NEWER` pattern for code that must compile on both.
- **The uGUI dependency was removed from Render Pipeline Core.** If a project relied on uGUI indirectly through Core, URP or HDRP, it must add `com.unity.ugui` explicitly. (This project's `Packages/manifest.json` lists `com.unity.ugui` directly.)
- The legacy Render Graph compiler is removed (projects on the native render pass Render Graph API are unaffected).
- **Editor.log is project-local by default** (`ProjectName/Logs/Editor.log`). Update CI scripts that read it, or pass `-logFile <path>`; `-useGlobalLog` restores the old behaviour.
- Unity now ships `System.Text.Json`, `System.Collections.Immutable`, `System.Reflection.Metadata` and `System.Runtime.CompilerServices.Unsafe`; Unity's copy is used and user copies are dropped.
- Built-In Render Pipeline is deprecated (supported through the 6.7 LTS lifecycle); dynamic batching is deprecated (removed in 6.6).
- Removed or changed `ModelImporter` APIs (see the upgrade guide for the list).

### 6.6

- **Dynamic batching is removed** (obsolete).
- Legacy Rendering Debugger APIs no longer compile; use `ISerializedDebugDisplaySettings`.
- Render Pipeline Core no longer depends on Terrain; add `com.unity.modules.terrain` if the project needs it.
- **UI Toolkit removals**: `DefaultEventSystem.LegacyInputProcessor` is removed, and `UIToolkitInputConfiguration.SetRuntimeInputBackend` calls fail to compile (delete them). Legacy UXML Factory/Traits are removed; use `UxmlElement` and `UxmlAttribute`.
- **`UNITY_64` and `DEVELOPMENT_BUILD` are deprecated.** They still work in 6.6 with analyzer warnings `UAC0008` and `UAC0009`, and become compile errors in Unity 6.8. Replacements named by the guide: `IntPtr.Size == 8`, `UNITY_ENABLE_CHECKS`, `UNITY_INCLUDE_INSTRUMENTATION`, `Debug.isDebugBuild`. A new Managed Code Variant Player setting controls diagnostics (default Release defines no diagnostic symbols). `BuildOptions.ForceEnableAssertions` is obsolete (use `PlayerSettings.SetManagedCodeVariant(…, ManagedCodeVariant.Checked)`). `CoreBuildData.developmentBuild` is obsolete and always returns `false`.
- **Play Mode domain reload.** The what's-new page says new projects default to "Reload Scene only", and the 6.6 manual pages say the Editor does not reload code on entering Play mode by default. With domain reload off, static variables and static event subscribers persist between Play sessions. Reset them with `[OnEnteringPlayMode]`, `[OnExitingPlayMode]` or `[AutoStaticsCleanup]` (types using `AutoStaticsCleanup` must be `partial` and cannot have static constructors; the auto-cleanup analyzer is off by default). Pages: https://docs.unity.com/en-us/engine/6000.6/manual/programming-environment/code-reload-serialization/code-reload/code-reloading-editor/domain-reloading.md
- Cinemachine 3 is a core package; Cinemachine 2 projects are auto-upgraded, with a different API and data format.
- YAML: word wrapping is disabled and "Reduce Version Control Noise" is removed, so expect unrelated YAML diffs.
- `com.unity.test-framework.performance` is now a core package. The Progressive CPU Lightmapper is deprecated.
- New projects: `Library/Artifacts` is renamed `DataStore`; Mesh Read/Write has a new requirement (see the what's-new page).

### Known issues listed in the 6000.6.3f1 release notes

Vulkan swapchain crash (UUM-153744); deadlock on exiting Play Mode with a shader destroyed during async compile (UUM-152966); `Resources.UnloadUnusedAssets()` much slower in Player builds (UUM-149540); Windows Editor Play Mode framerate drops with a high mouse polling rate (UUM-142550). Fixed in that release: the 2D Renderer no longer creates a depth and stencil buffer in Players when Disable Depth and Stencil Buffers is enabled. Source: https://unity.com/releases/editor/whats-new/6000.6.3f1

### NOT SOURCEABLE

- NOT SOURCEABLE — how an existing (non-new) project treats its Enter Play Mode settings after upgrading to 6.6, not stated at the 6.6 what's-new or code-reload pages.
- NOT SOURCEABLE — Input System, Test Framework or UGUI breaking changes in 6.4 and 6.6, not stated at the 6.4 or 6.6 upgrade guides. The 6.5 guide and what's-new page list no Input System breaking changes.
- NOT SOURCEABLE — Physics 2D / Physics Core 2D changes in 6.4 and 6.6, and `EntityId` changes in 6.6, not stated at the 6.6 what's-new page.
- NOT SOURCEABLE — Render Graph or `RenderSprite` breaking changes in 6.6, not stated at the 6.6 upgrade guide.
- NOT SOURCEABLE — whether the legacy Input Manager is removed or disabled by default in 6.4 to 6.6; only search snippets say it is deprecated, no page read states it.
- NOT SOURCEABLE — the full 6000.6.0f1 release notes: the page is about 593,000 characters and only the first 100,000 were read.
- NOT SOURCEABLE — CoreCLR details for 6.4 to 6.6 beyond one-line mentions.
