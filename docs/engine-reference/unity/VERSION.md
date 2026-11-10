# Unity Engine — Version Reference

| Field | Value |
|-------|-------|
| **Engine Version** | Unity 6.6 (6000.6.3f1) |
| **Previously pinned** | Unity 6.3 LTS (6000.3), pinned 2026-02-13. The 6.3 LTS content in this directory is kept; see the `6.3 LTS → 6.6` sections. |
| **Installed at pin time** | 6000.6.3f1 — matches the pin (probed 2026-11-09: the editor at `C:/Program Files/Unity/Hub/Editor/6000.6.3f1/` answered `-version`; `ProjectSettings/ProjectVersion.txt` says 6000.6.3f1) |
| **Release Date** | August 31, 2026 (6000.6.0f1; https://unity.com/releases/editor/whats-new/6000.6.0). 6000.6.3f1 shipped Sep 24, 2026. |
| **Latest editor on the pinned line** | 6000.6.8f1 (Nov 3, 2026), the latest confirmed; the project's 6000.6.3f1 is 5 patch releases behind. 6000.6.9f1 and 6000.6.10f1 returned 404 (not proof they do not exist). |
| **Latest editor on the previous pin (6.3 LTS)** | 6000.3.26f1 (Oct 8, 2026) |
| **Project Pinned** | 2026-11-09 |
| **Last Docs Verified** | 2026-11-09 (6.4 to 6.6 changes, 6.6 editor versions, package versions); 2026-10-17 (release timeline of 6.4 to 6.7); 2026-02-13 (rest of the directory: 6.3 LTS content; `modules/` and `plugins/` NOT re-verified for 6.6) |
| **LLM Knowledge Cutoff** | May 2025 |

## Knowledge Gap Warning

The LLM's training data likely covers Unity up to ~2022 LTS (2022.3). The entire
Unity 6 release series (formerly Unity 2023 Tech Stream) introduced significant
changes that the model does NOT know about. Always cross-reference this directory
before suggesting Unity API calls.

## Installed-Version Gap Warning

The warning above is one-directional — it covers the **model** knowing less than
this pin. The reverse gap is real and `/setup-engine` §3 creates it deliberately
("pin the newer one and upgrade later"): this reference can sit **ahead of the
installed editor**, and an agent citing it correctly then emits APIs that do not
compile locally. **Check `Installed at pin time` above before trusting a
version-qualified claim** — `NOT DETERMINED` means the gap is unknown, not absent.

## Post-Cutoff Version Timeline

| Version | Release | Risk Level | Key Theme |
|---------|---------|------------|-----------|
| 6.2 | Aug 2025 | MEDIUM | Supported Update release: Unity AI (beta) in the Editor, Mesh LOD, World Space UI for UI Toolkit, Graph Toolkit, Android XR hand mesh |
| 6.3 LTS | Dec 2025 | HIGH | First LTS since 6.0 (two years of support): Platform Toolkit, Nintendo Switch 2 support, Android XR, desktop screen reader support, Shader Build Settings |
| 6.4 | Mar 2026 | HIGH | URP Compatibility Mode removed for custom render passes (Render Graph replaces it), `Object.Destroy` now calls `OnDisable` on all descendants, `InstanceID` deprecated in favour of `EntityId`, ECS packages become Core packages. Source: https://docs.unity.com/en-us/engine/6000.6/manual/upgrade-guides/upgrade-guide-unity64 |
| 6.5 | Jun 2026 | HIGH | `EntityId` replaces `InstanceID` (int-based APIs no longer compile), obsolete GameObject/Component shortcuts such as `.rigidbody2D` removed, `LowLevelPhysics2D` renamed `PhysicsCore2D`, Built-In Render Pipeline deprecated, Editor.log project-local by default, `Graphics.RenderSprite` for 2D. Source: https://docs.unity.com/en-us/engine/6000.6/manual/upgrade-guides/upgrade-guide-unity65 |
| 6.6 | Sep 2026 | HIGH | New projects skip domain reload on entering Play Mode, dynamic batching removed, legacy UXML Factory/Traits removed, `UNITY_64` and `DEVELOPMENT_BUILD` deprecated, Content Directories, Cinemachine 3 becomes a core package. Final supported release of the Unity 6 family. Source: https://docs.unity.com/en-us/engine/6000.6/manual/upgrade-guides/upgrade-guide-unity66 |

## Release Context: 6.3 LTS to 6.7

**The pin moved to Unity 6.6 (6000.6.3f1) on 2026-11-09**, because the project
was already created on, and has installed, 6000.6.3f1. **6.6 is not an LTS:** it
is supported until 6.7 ships (see the table below), while 6.3 LTS is supported
until December 2027. Re-pin when 6.7 LTS ships. The `6.3 LTS → 6.6` sections of
`breaking-changes.md`, `deprecated-apis.md` and `current-best-practices.md` carry
the API changes verified on 2026-11-09. The 6.7 APIs, and the `modules/` and
`plugins/` files (still written for 6.3 LTS), are unconfirmed for 6.6: check them
against Unity's docs for 6.6 before relying on them. 6.0 LTS support ends in
October 2026.

| Version | Release | Status (2026-10-17) | Key Theme |
|---------|---------|---------------------|-----------|
| 6.4 | Mar 2026 | Reached end of life | ECS (Entities, Collections, Mathematics, Entities Graphics) ships as Core packages, Project Auditor built into the Editor, DirectStorage on Windows, `EntityId` replaces `InstanceID`, URP Compatibility Mode removed in favor of Render Graph |
| 6.5 | Jun 2026 | Reached end of life | Entity unification (`InstanceID` to `EntityId`), Editor Lifecycle API toward a CoreCLR Editor, 2D (custom lights and shadows, `RenderSprite` API, Physics Core 2D), on-tile post-processing for mobile, Built-In Render Pipeline deprecated |
| 6.6 | Sep 2026 | Supported until 6.7 ships | Final supported release of the Unity 6 family: Play Mode skips domain reloads by default for new projects, Content Directories, Build Analysis window, WebGPU as a production Web option |
| 6.7 | Beta | Not released | 6000.7.0b1 on Sep 17, 2026, latest 6000.7.0b5 on Oct 15, 2026; Unity's 6.6 post calls it the 6.7 LTS |

Dates and status, verified 2026-10-17: 6.4 (announced Mar 17, 2026; editor
6000.4.0f1 Mar 18) https://discussions.unity.com/t/unity-6-4-is-now-available/1713245
and https://docs.unity.com/en-us/engine/6000.6/manual/whats-new/unity64; 6.5
(Jun 15, 2026) https://discussions.unity.com/t/unity-6-5-is-now-available/1723176
and https://docs.unity.com/en-us/engine/6000.5/manual/whats-new/unity65; 6.6
(editor 6000.6.0f1 Aug 31, announced Sep 1, 2026, latest 6000.6.6f1 Oct 15)
https://discussions.unity.com/t/unity-6-6-is-now-available/1735357. The 6.4 and
6.5 threads carry the banner "This supported release has reached end of life"
(6.4 points to 6.5 or above, 6.5 to 6.6 or above); the 6.6 post says it is
supported "until the next release - in this case 6.7 - arrives" and that it
"brings you one step closer to the 6.7 LTS". Editor versions and release dates
come from Unity's release API
https://services.api.unity.com/unity/editor/release/v1/releases (6.3 LTS first
editor 6000.3.0f1 on Dec 3, 2025; latest 6000.3.26f1 on Oct 8, 2026). Upgrading
the pin is a decision for when 6.7 LTS ships and `/setup-engine` re-pins. Until
then a new project would start on 6.3 LTS; this project is already on 6.6.

## Unity 6 Releases Before the Cutoff

These two predate the May 2025 cutoff, so they are not post-cutoff. The Knowledge
Gap Warning above still applies to them: treat their APIs as unconfirmed until
checked against this directory.

| Version | Release | Risk Level | Key Theme |
|---------|---------|------------|-----------|
| 6.0 LTS | Oct 2024 | HIGH | Unity 6 launch: GPU Resident Drawer, Render Graph for URP, multiplayer workflows, mobile web, new Update release type |
| 6.1 | Apr 2025 | MEDIUM | First Supported Update release: URP Deferred+ path, Variable Rate Shading, Project Auditor, WebGPU for Unity Web, foldable/large-screen Android |

Release dates verified 2026-10-17 against Unity's announcements: 6.0
(Oct 17, 2024) https://unity.com/blog/introducing-unity-6-launch, 6.1
(Apr 24, 2025) https://unity.com/blog/unity-6-1-is-now-available, 6.2
(Aug 12, 2025) https://discussions.unity.com/t/6-2-is-now-available/1677060,
6.3 LTS (Dec 4, 2025) https://unity.com/blog/unity-6-3-lts-is-now-available.
Support windows: https://unity.com/releases/unity-6/support (6.0 LTS through
October 2026, 6.3 LTS until December 2027).

## Major Changes from 2022 LTS to Unity 6.3 LTS

### Breaking Changes
- **Entities/DOTS**: Major API overhaul in Entities 1.0+, complete redesign of ECS patterns
- **Input System**: Legacy Input Manager deprecated, new Input System is default
- **Rendering**: URP/HDRP significant upgrades, SRP Batcher improvements
- **Addressables**: Asset management workflow changes
- **Scripting**: C# 9 support, new API patterns

### New Features (Post-Cutoff)
- **DOTS**: Production-ready Entity Component System (Entities 1.3+)
- **Graphics**: Enhanced URP/HDRP pipelines, GPU Resident Drawer
- **Multiplayer**: Netcode for GameObjects improvements
- **UI Toolkit**: Production-ready for runtime UI (replaces UGUI for new projects)
- **Async Asset Loading**: Improved Addressables performance
- **Web**: WebGPU support

### Deprecated Systems
- **Legacy Input Manager**: Use new Input System package
- **Legacy Particle System**: Use Visual Effect Graph
- **UGUI**: Still supported, but UI Toolkit recommended for new projects
- **Old ECS (GameObjectEntity)**: Replaced by modern DOTS/Entities

## Verified Sources

- Official docs: https://docs.unity3d.com/6000.0/Documentation/Manual/index.html
- Unity 6 release: https://unity.com/releases/unity-6
- Unity 6.0 launch: https://unity.com/blog/introducing-unity-6-launch
- Unity 6.1 announcement: https://unity.com/blog/unity-6-1-is-now-available
- Unity 6.2 announcement: https://discussions.unity.com/t/6-2-is-now-available/1677060
- Unity 6.3 LTS announcement: https://unity.com/blog/unity-6-3-lts-is-now-available
- Migration guide: https://docs.unity3d.com/6000.0/Documentation/Manual/upgrade-guides.html
- Unity 6 support: https://unity.com/releases/unity-6/support
- Unity 6.4 announcement: https://discussions.unity.com/t/unity-6-4-is-now-available/1713245
- Unity 6.5 announcement: https://discussions.unity.com/t/unity-6-5-is-now-available/1723176
- Unity 6.6 announcement: https://discussions.unity.com/t/unity-6-6-is-now-available/1735357
- Editor release API: https://services.api.unity.com/unity/editor/release/v1/releases
- Unity 6.4 upgrade guide: https://docs.unity.com/en-us/engine/6000.6/manual/upgrade-guides/upgrade-guide-unity64
- Unity 6.5 upgrade guide: https://docs.unity.com/en-us/engine/6000.6/manual/upgrade-guides/upgrade-guide-unity65
- Unity 6.6 upgrade guide: https://docs.unity.com/en-us/engine/6000.6/manual/upgrade-guides/upgrade-guide-unity66
- Unity 6.6 what's new: https://docs.unity.com/en-us/engine/6000.6/manual/whats-new/unity66
- 6000.6.0f1 release notes: https://unity.com/releases/editor/whats-new/6000.6.0 (only the first 100,000 characters of about 593,000 were read)
- 6000.6.3f1 release notes: https://unity.com/releases/editor/whats-new/6000.6.3f1
- C# API reference: https://docs.unity3d.com/6000.0/Documentation/ScriptReference/index.html
