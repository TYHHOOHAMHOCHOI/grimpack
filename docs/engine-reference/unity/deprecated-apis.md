# Unity 6.3 LTS — Deprecated APIs

**Last verified:** 2026-02-13

Quick lookup table for deprecated APIs and their replacements.
Format: **Don't use X** → **Use Y instead**

---

## Input

| Deprecated | Replacement | Notes |
|------------|-------------|-------|
| `Input.GetKey()` | `Keyboard.current[Key.X].isPressed` | New Input System |
| `Input.GetKeyDown()` | `Keyboard.current[Key.X].wasPressedThisFrame` | New Input System |
| `Input.GetMouseButton()` | `Mouse.current.leftButton.isPressed` | New Input System |
| `Input.GetAxis()` | `InputAction` callbacks | New Input System |
| `Input.mousePosition` | `Mouse.current.position.ReadValue()` | New Input System |

**Migration:** Install `com.unity.inputsystem` package.

---

## UI

| Deprecated | Replacement | Notes |
|------------|-------------|-------|
| `Canvas` (UGUI) | `UIDocument` (UI Toolkit) | UI Toolkit is now production-ready |
| `Text` component | `TextMeshPro` or UI Toolkit `Label` | Better rendering, fewer draw calls |
| `Image` component | UI Toolkit `VisualElement` with background | More flexible styling |

**Migration:** UGUI still works, but UI Toolkit is recommended for new projects.

---

## DOTS/Entities

| Deprecated | Replacement | Notes |
|------------|-------------|-------|
| `ComponentSystem` | `ISystem` (unmanaged) | Entities 1.0+ complete rewrite |
| `JobComponentSystem` | `ISystem` with `IJobEntity` | Burst-compatible |
| `GameObjectEntity` | Pure ECS workflow | No GameObject conversion |
| `EntityManager.CreateEntity()` (old signature) | `EntityManager.CreateEntity(EntityArchetype)` | Explicit archetype |
| `ComponentDataFromEntity<T>` | `ComponentLookup<T>` | Entities 1.0+ rename |

**Migration:** See Entities package migration guide. Major refactor required.

---

## Rendering

| Deprecated | Replacement | Notes |
|------------|-------------|-------|
| `CommandBuffer.DrawMesh()` | RenderGraph API | URP/HDRP render passes |
| `OnPreRender()` / `OnPostRender()` | `RenderPipelineManager` callbacks | SRP compatibility |
| `Camera.SetReplacementShader()` | Custom render pass | Not supported in SRP |

---

## Physics

| Deprecated | Replacement | Notes |
|------------|-------------|-------|
| `Physics.RaycastAll()` | `Physics.RaycastNonAlloc()` | Avoid GC allocations |
| `Rigidbody.velocity` (direct write) | `Rigidbody.AddForce()` | Better physics stability |

---

## Asset Loading

| Deprecated | Replacement | Notes |
|------------|-------------|-------|
| `Resources.Load()` | Addressables | Better memory control, async loading |
| Synchronous asset loading | `Addressables.LoadAssetAsync()` | Non-blocking |

---

## Animation

| Deprecated | Replacement | Notes |
|------------|-------------|-------|
| Legacy Animation component | Animator Controller | Mecanim system |
| `Animation.Play()` | `Animator.Play()` | State machine control |

---

## Particles

| Deprecated | Replacement | Notes |
|------------|-------------|-------|
| Legacy Particle System | Visual Effect Graph | GPU-accelerated, more performant |

---

## Scripting

| Deprecated | Replacement | Notes |
|------------|-------------|-------|
| `WWW` class | `UnityWebRequest` | Modern async networking |
| `Application.LoadLevel()` | `SceneManager.LoadScene()` | Scene management |

---

## Platform-Specific

### WebGL
| Deprecated | Replacement | Notes |
|------------|-------------|-------|
| WebGL 1.0 | WebGL 2.0 or WebGPU | Unity 6+ defaults to WebGPU |

---

## Quick Migration Patterns

### Input Example
```csharp
// ❌ Deprecated
if (Input.GetKeyDown(KeyCode.Space)) {
    Jump();
}

// ✅ New Input System
using UnityEngine.InputSystem;
if (Keyboard.current.spaceKey.wasPressedThisFrame) {
    Jump();
}
```

### Asset Loading Example
```csharp
// ❌ Deprecated
var prefab = Resources.Load<GameObject>("Enemies/Goblin");

// ✅ Addressables
var handle = Addressables.LoadAssetAsync<GameObject>("Enemies/Goblin");
await handle.Task;
var prefab = handle.Result;
```

### UI Example
```csharp
// ❌ Deprecated (UGUI)
GetComponent<Text>().text = "Score: 100";

// ✅ TextMeshPro
GetComponent<TextMeshProUGUI>().text = "Score: 100";

// ✅ UI Toolkit
rootVisualElement.Q<Label>("score-label").text = "Score: 100";
```

---

**Sources:**
- https://docs.unity3d.com/6000.0/Documentation/Manual/deprecated-features.html
- https://docs.unity3d.com/Packages/com.unity.inputsystem@1.11/manual/Migration.html


---

## 6.3 LTS → 6.6 (6000.3 → 6000.6)

**Last verified:** 2026-11-09 (this section only; the sections above describe 6.3 LTS and were not re-verified).

Sources: the 6.4, 6.5 and 6.6 upgrade guides at https://docs.unity.com/en-us/engine/6000.6/manual/upgrade-guides/upgrade-guide-unity64 (and `…unity65`, `…unity66`). Full context for each row is in `breaking-changes.md`.

| Don't use | Use instead | Since |
|-----------|-------------|-------|
| `InstanceID`, `GetInstanceID()` (int-based) | `EntityId`, `GetEntityId()` (8 bytes; do not cast to `int`, do not `int.Parse` its string, do not use `GetHashCode()` as a dictionary key) | deprecated 6.4, int APIs fail to compile in 6.5 |
| `.rigidbody`, `.rigidbody2D`, `.camera`, `.light`, `.renderer`, `.collider`, `.collider2d` shortcuts | `GetComponent<T>()` | removed 6.5 |
| `AddComponent(string)` | `AddComponent<T>()` | removed 6.5 |
| `UnityEngine.LowLevelPhysics2D` | `PhysicsCore2D` in namespace `Unity.U2D.Physics` (auto-migrated; guard with `#if UNITY_6000_5_OR_NEWER` for dual-version code) | renamed 6.5 |
| URP Compatibility Mode, `URP_COMPATIBILITY_MODE` define (custom render passes) | Render Graph | removed 6.4 |
| Legacy Render Graph compiler | The native render pass Render Graph API | removed 6.5 |
| `IVolume` | `VolumeGizmoDrawer` | removed 6.4 |
| Legacy Rendering Debugger APIs | `ISerializedDebugDisplaySettings` | no longer compile in 6.6 |
| UXML Factory/Traits (legacy) | `UxmlElement` and `UxmlAttribute` | removed 6.6 |
| `DefaultEventSystem.LegacyInputProcessor`, `UIToolkitInputConfiguration.SetRuntimeInputBackend` | Delete the calls (the guide gives no replacement call) | removed 6.6 |
| `UNITY_64` | `IntPtr.Size == 8` | deprecated 6.6 (warning `UAC0008`), compile error in 6.8 |
| `DEVELOPMENT_BUILD` | `UNITY_ENABLE_CHECKS`, `UNITY_INCLUDE_INSTRUMENTATION` or `Debug.isDebugBuild` (the guide lists these as replacements; which one fits which use is NOT SOURCEABLE here) | deprecated 6.6 (warning `UAC0009`), compile error in 6.8 |
| `BuildOptions.ForceEnableAssertions` | `PlayerSettings.SetManagedCodeVariant(…, ManagedCodeVariant.Checked)` | obsolete 6.6 |
| `CoreBuildData.developmentBuild` | No replacement named (always returns `false`) | obsolete 6.6 |
| Dynamic batching | NOT SOURCEABLE — no replacement named at the 6.5 or 6.6 upgrade guides | deprecated 6.5, removed 6.6 |
| Built-In Render Pipeline | Deprecated, supported through the 6.7 LTS lifecycle; NOT SOURCEABLE — no replacement named at the page read | deprecated 6.5 |
| Shader Graph Reflection Probe node | NOT SOURCEABLE — no replacement named | deprecated 6.5 |
| Progressive CPU Lightmapper | NOT SOURCEABLE — no replacement named | deprecated 6.6 |
| Cinemachine 2 API | Cinemachine 3 (core package; auto-upgrade changes the API and data format) | 6.6 |
