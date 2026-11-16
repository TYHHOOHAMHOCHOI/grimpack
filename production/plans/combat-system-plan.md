# Grimpack Combat System Implementation Plan & API Contracts

> **Status**: Approved Architecture Plan (1-Hero Playable Vertical Loop + Full Engine-Free `CombatCore`)
> **Owner**: `@architect`
> **Unity Version**: `6000.6.3f1` (URP 2D, 640×360 native pixel canvas)
> **Scope**: Engine-free deterministic `Project05.CombatCore`, updated item/hero/monster JSON catalogs, interactive 1-hero `Combat.unity` workbench/picker/playback UI, Next-Stage Monster Count (`1..6`) test system, and EditMode verification suite.

---

## 1. Goals & User Requirements Mapping

| Requirement | Concrete Deliverable | Owner(s) |
|---|---|---|
| **GDD-compliant deterministic simulation** | `Project05.CombatCore` (`noEngineReferences: true`), integer-only math, named RNG streams, atomic StepModel, causal EventLog, FIFO EffectResolver (`D=8`, `N=256`), TurnOrderScheduler (`T=1000`), ItemTriggerAndCooldown, EnemyIntentSelector (`H=12`), CombatRunner (`200`-action cap). | `@sim-core` |
| **Temporary 1-hero mode ("tạm thời chỉ dùng 1 nhân vật thôi")** | `CombatCore` supports `1..3` heroes (`HeroCount = 1` in active setup). `CombatHudView` activates Slot 0 (`JoinNumeral = 1`, `char_tank_*` sprites) and hides Slots 1–2 (`Heroes[1] = null`, `Heroes[2] = null`). | `@sim-core`, `@combat-ui` |
| **Suitable items & data ("Thêm các item cho tướng phù hợp")** | Keep all 23 existing items in `items_mvp.json` intact (icons/plates preserved) and add solo-viable Tank/Crusader items (`ShieldGain`, `Heal`, `AllEnemies` cleave, damage-taken `Reaction`, self-sufficient Seal+Detonate combos) with matching icons, plus `heroes_mvp.json` & `monsters_mvp.json`. | `@items-data` |
| **Equipment picker & backpack ("chọn trang bị lấy ra cho nhân vật")** | Interactive Equipment Picker (catalog drawer) + Hero 4×4 Grid + Stash (`8×2`) with drag-and-drop, `R` key / right-click rotation (`0..3`), removal/return, hover popups (`ItemPopupController`), live adjacency synergy bridges, and live Turn Order Forecast (`OrderStripView`). | `@combat-ui` |
| **Next-stage monster count test selector ("Hệ thống test chọn số lượng quái cho màn sau")** | In-scene `1..6` monster count selector (available in Planning ActionStrip and Post-Combat Result panel) + Stage/Seed controls that spawn `1..6` enemies on transition to the next stage while preserving the hero's backpack arrangement and restoring full HP + initial shield. | `@combat-ui` |
| **Verification** | Comprehensive EditMode tests for `CombatCore` determinism, GDD acceptance criteria, zero `UnityEngine` refs, JSON catalog integrity, and `CombatScene` integration. | `@verifier` |

---

## 2. Exclusive File Ownership Matrix

| Agent | Exclusive Files / Directories | Must NOT Edit |
|---|---|---|
| `@architect` | `production/plans/combat-system-plan.md` | Any `.cs`, `.json`, `.py`, `.unity` |
| `@sim-core` | `Assets/_Project/Scripts/CombatCore/` (including `Project05.CombatCore.asmdef`) | `UI/`, `Editor/`, `Data/`, `tools/`, `Tests/`, `CombatScene/` |
| `@items-data` | `tools/art/item_catalog.py`, `tools/art/build_items.py` (if generating new icons), `Assets/_Project/Data/items_mvp.json`, `Assets/_Project/Data/heroes_mvp.json`, `Assets/_Project/Data/monsters_mvp.json`, `design/content/mvp-item-catalog.md`, `Assets/_Project/Scripts/Items/ItemRecords.cs`, `Assets/_Project/Scripts/Items/ItemTooltipText.cs` (for `ShieldGain`/`Heal` popup formatting) | `CombatCore/`, `CombatScene/`, `UI/`, `Editor/`, `Tests/` |
| `@combat-ui` | `Assets/_Project/Scripts/CombatScene/`, `Assets/_Project/Scripts/Project05.Runtime.asmdef`, `Assets/_Project/Scripts/UI/Hud/CombatHudDemo.cs`, `Assets/_Project/Scripts/Editor/CombatSceneBuilder.cs` | `CombatCore/`, `Data/*.json`, `tools/art/`, `Tests/` |
| `@verifier` | `Assets/_Project/Tests/EditMode/CombatCoreTests.cs`, `Assets/_Project/Tests/EditMode/CombatSceneIntegrationTests.cs`, `Assets/_Project/Tests/EditMode/Project05.EditModeTests.asmdef` | Any runtime, data, or editor production files |

---

## 3. GDD Mapping & Adaptation Notes

### 3.1 Engine-Free & Determinism Constraints (All GDDs)
- `Assets/_Project/Scripts/CombatCore/Project05.CombatCore.asmdef`:
  ```json
  {
    "name": "Project05.CombatCore",
    "rootNamespace": "Project05.CombatCore",
    "references": [],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": true
  }
  ```
- Strictly banned inside `CombatCore`: `UnityEngine`, `Unity.*`, `float`, `double`, `System.Random`, `DateTime`, `string.GetHashCode`, `Dictionary`/`HashSet` iteration order for gameplay logic, and non-readonly mutable `static` fields (Unity 6000.6 skips domain reload by default).
- Pure-C# JSON loading: Since `UnityEngine.JsonUtility` is unavailable in `CombatCore`, `@sim-core` provides an internal pure-C# recursive-descent JSON reader (`SimpleJsonParser`) inside `CombatCore` used by `GameCatalog.LoadFromJson(...)`.

### 3.2 Effect Vocabulary Extension for 1-Hero Mode
The spike GDD (`effect-resolution-model.md`) defines `Damage`, `ApplyStatus`, and `ConsumeDetonate`, while listing `ShieldGain` and `Heal` as deferred until coordinated. For the 1-hero playable loop, a solo hero fighting `1..6` monsters needs defensive/sustain tools, so **we explicitly adopt `ShieldGain` and `Heal`** under the exact same System 3 / System 36 / System 4 atomic mutation contract:
1. `Damage`: Base `B` (`1..100,000`), flat bucket `F`, percent bucket `P` (permille, clamped `>= -1000`), `Raw = clamp(B + F, 0, 10^12)`, `Scaled = (Raw * (1000 + P) + 500) / 1000`, `r = clamp(Scaled, 0, 2,147,483,647)`. Stages `Shield` loss first (`s = min(q, r)`), then `HP` loss (`l = min(h, r - s)`), emitting separate `Damage` records per changed field sharing `OccurrenceId`. `r == 0` emits no mutation event.
2. `ApplyStatus`: Positive `amount` (`+ StatusAmount` synergy sum for `"an"`). Emits `StatusApplied` with expiration boundary `target.ActionOrdinal + 2` (2 completed actions of the target, not counting the action in progress).
3. `ConsumeDetonate`: Consumes up to `units` of `status` (`"an"`) oldest-first (baseline contributions first by ID, then `StatusApplied` by ascending `Seq`). Computes `B = min(c * perUnit, 10^12)`, applies `DamageFlat`/`DamagePercent` modifiers, and atomically publishes `StatusConsumed` followed by `DetonationDamage` steps. If `c == 0`, emits `DetonationEmpty(no_available_units)`.
4. `ShieldGain`: Adds positive `amount` (`1..100,000`) to target's shield (capped safely within `int.MaxValue`). Emits `ShieldGained` with `ShieldBefore`, `ShieldAfter`, `ResolvedAmount`, causal `Origin`, and resolution stamps. No-op if target is downed (`EffectSkipped(target_downed)`) or shield unchanged.
5. `Heal`: Restores positive `amount` (`1..100,000`) HP to living target up to `MaxHp` (`actual = min(amount, MaxHp - Hp)`). Emits `Healed` with `HpBefore`, `HpAfter`, `ResolvedAmount`, causal `Origin`, and resolution stamps. Downed target emits `EffectSkipped(target_downed)`; full-HP target (`actual == 0`) emits no mutation event.

### 3.3 Target Resolution Rules
- **Hero Item Targets** (late-bound when effect is taken from FIFO queue, in Canonical Order `(side, slot)`):
  - `FirstEnemy`: lowest-slot living enemy (`HP > 0`).
  - `AllEnemies`: all living enemies in ascending slot order (`0..5`).
  - `Self`: the item's owner hero (if living).
  - `AllHeroes`: all living heroes in ascending party slot order (`0..2`).
  - `LowestHpHero`: living hero with lowest current `HP` (tie-break by lowest party slot).
- **Enemy Move Targets** (bound pre-combat in `PreparedEncounter`, never retargeted):
  - `RandomHero` / `SameAsMove`: single bound hero instance ID (`BoundHero`). If downed at resolution time, emit `EffectSkipped(target_downed)` with no retarget.
  - `AllHeroes`: all living heroes in ascending party slot order (`0..2`). If none living, emit `EffectSkipped(target_downed)`.
  - `Windup` (`HitCount == 0`): empty effect list; still counts as a completed actor action and advances `n` and `e`.

### 3.4 1-Hero Party Mapping (`Party` & `CombatHudView`)
- `Party` supports `1..3` heroes (`hero_crusader` in Slot `0`, `JoinNumeral = 1`, `HeroInstanceId = 0`).
- `CombatHudData.Heroes`: array length 3 where `Heroes[0]` is populated from `HeroStateView` (`Slot 0`) and `Heroes[1] = null`, `Heroes[2] = null`.
- `CombatHudView.Apply(data)` already checks `i < data.Heroes.Length && data.Heroes[i] != null` and sets `heroes[1]` and `heroes[2]` inactive automatically.
- `EnemySlotData` (`0..5`): populated for present enemies `0..enemyCount-1`, `Present = false` for unused slots. `CombatHudView.LayoutEnemies` places slots `0..2` in Line 1 (`x = 384`) and `3..5` in Line 2 (`x = 524`), each line `<= 300 px` pitch budget (`Swarmer` pitch `82`, `Brute` pitch `100` — up to 6 Brutes fit `3 * 100 = 300 <= 300`).

---

## 4. Exact C# Contracts for `Project05.CombatCore`

All public types below live in `namespace Project05.CombatCore`.

### 4.1 Enums & Core Value Types
```csharp
public enum TierKind { White = 0, Green = 1, Blue = 2, Purple = 3, Gold = 4, Red = 5 }
public enum TriggerKind { Passive = 0, Cooldown = 1, Reaction = 2, CombatStart = 3 }
public enum EffectKind { Damage = 0, ApplyStatus = 1, ConsumeDetonate = 2, ShieldGain = 3, Heal = 4 }
public enum TargetKind { FirstEnemy = 0, AllEnemies = 1, Self = 2, AllHeroes = 3, LowestHpHero = 4, BoundHero = 5 }
public enum SynergyKind { DamageFlat = 0, DamagePercent = 1, CooldownReduction = 2, StatusAmount = 3 }
public enum ReactionEventKind { None = 0, ItemFired = 1, StatusApplied = 2, StatusConsumed = 3, Damage = 4, UnitDowned = 5 }
public enum ReactionSideFilter { Any = 0, Hero = 1, Enemy = 2, OwnHero = 3, OtherHero = 4 }

public enum MonsterClassKind { Regular = 0, Elite = 1, Boss = 2 }
public enum MonsterSizeKind { Small = 0, Medium = 1, Large = 2, Boss = 3 }
public enum MoveGlyphKind { Attack = 0, Windup = 1 }
public enum MoveTargetMode { None = 0, RandomHero = 1, AllHeroes = 2, SameAsMove = 3 }
public enum DeliveryKind { Swing = 0, Throw = 1 }

public enum ContainerKind { Hero0 = 0, Hero1 = 1, Hero2 = 2, Stash = 100, Tray = 200 }
public enum GridOpCode
{
    Ok = 0, NoEffect = 1, NothingToUndo = 2, NoPlacement = 3, Frozen = 4,
    ContractViolation = 5, UnknownContainer = 6, UnknownItem = 7,
    InvalidOrientation = 8, RotationDisabled = 9, OutOfBounds = 10,
    LockedCell = 11, Occupied = 12, BufferTooSmall = 13
}

public enum UnitSide { Hero = 0, Enemy = 1 }
public enum CombatOutcome { Victory = 0, Defeat = 1, Draw = 2 }
public enum CombatRunStatus { Completed = 0, TechnicalStop = 1 }

public enum CombatEventKind
{
    IntentsBound = 0,
    ActionStarted = 1,
    ActionSkipped = 2,
    ItemFired = 3,
    StatusApplied = 4,
    StatusConsumed = 5,
    StatusExpired = 6,
    Damage = 7,
    DetonationDamage = 8,
    ShieldGained = 9,
    Healed = 10,
    UnitDowned = 11,
    ChainLimit = 12,
    ListenerSuppressed = 13,
    EffectSkipped = 14,
    DetonationEmpty = 15,
    CombatEnded = 16
}

public enum OriginKind { None = 0, Item = 1, EnemyMove = 2, StatusExpiry = 3, EncounterSetup = 4 }

public readonly struct GridCell
{
    public readonly int X;
    public readonly int Y;
    public GridCell(int x, int y) { X = x; Y = y; }
}

public readonly struct UnitRef
{
    public readonly UnitSide Side;
    public readonly int Slot;
    public readonly int InstanceId;
    public UnitRef(UnitSide side, int slot, int instanceId) { Side = side; Slot = slot; InstanceId = instanceId; }
}
```

### 4.2 Static Catalog Definitions (`GameCatalog`)
```csharp
public sealed class TriggerDef
{
    public TriggerKind Kind { get; }
    public int N { get; }
    public int S { get; }
    public ReactionEventKind EventKind { get; }
    public string EventStatus { get; }
    public ReactionSideFilter EventSide { get; }
}

public sealed class EffectDef
{
    public EffectKind Kind { get; }
    public TargetKind Target { get; }
    public int Base { get; }
    public string Status { get; }
    public int Amount { get; }
    public int Units { get; }
    public int PerUnit { get; }
}

public sealed class SynergyRuleDef
{
    public IReadOnlyList<string> TargetTags { get; }
    public SynergyKind Kind { get; }
    public int Amount { get; }
    public string StatusId { get; }
}

public sealed class ItemDef
{
    public string Id { get; }
    public string Art { get; }
    public TierKind Tier { get; }
    public string Footprint { get; }
    public IReadOnlyList<GridCell> Cells { get; }
    public IReadOnlyList<string> Tags { get; }
    public TriggerDef Trigger { get; }
    public IReadOnlyList<EffectDef> Effects { get; }
    public int HpBonus { get; }
    public int SpeedBonus { get; }
    public IReadOnlyList<SynergyRuleDef> Synergy { get; }
    public string NameVi { get; }
    public string NameEn { get; }
    public string FlavorVi { get; }
    public string FlavorEn { get; }
}

public sealed class BackpackProfileDef
{
    public int Width { get; }          // default 4, max 6
    public int Height { get; }         // default 4, max 6
    public int StartWidth { get; }     // default 4
    public int StartHeight { get; }    // default 4
    public IReadOnlyList<GridCell> UnlockSequence { get; }
}

public sealed class InnateItemDef
{
    public string Id { get; }
    public TriggerDef Trigger { get; }
    public IReadOnlyList<EffectDef> Effects { get; }
    public string NameVi { get; }
    public string NameEn { get; }
    public string DescVi { get; }
    public string DescEn { get; }
}

public sealed class HeroDef
{
    public string Id { get; }
    public string Art { get; }          // "tank" -> maps to char_tank_*
    public string NameVi { get; }
    public string NameEn { get; }
    public string RoleVi { get; }
    public string RoleEn { get; }
    public int BaseMaxHp { get; }       // 40..300
    public int BaseSpeed { get; }       // 5..60
    public int InitialShield { get; }   // 0..30
    public BackpackProfileDef BackpackProfile { get; }
    public IReadOnlyList<string> StartingItems { get; }
    public string SignatureItemId { get; }
    public InnateItemDef InnateItem { get; } // optional (null allowed if hero has none, or authored)
}

public sealed class MonsterMoveDef
{
    public string Id { get; }
    public string NameVi { get; }
    public string NameEn { get; }
    public MoveGlyphKind Glyph { get; }
    public DeliveryKind Delivery { get; }
    public int HitCount { get; }            // 0..8 (0 = windup)
    public int BaseDamage { get; }          // 0 for windup, 1..100000 for attack
    public MoveTargetMode TargetMode { get; }
    public string SameAsMoveId { get; }
    public int Weight { get; }              // 0..100
    public bool Heavy { get; }
    public string LinkedNextMoveId { get; }
}

public sealed class BossPhaseDef
{
    public int StartIndex { get; }
    public int EndIndexInclusive { get; }   // -1 = open-ended
    public IReadOnlyList<string> AllowedMoveIds { get; }
}

public sealed class MonsterDef
{
    public string Id { get; }
    public string Visual { get; }           // "swarmer" or "brute"
    public int BodyHeight { get; }          // 26 (swarmer) or 44 (brute)
    public string NameVi { get; }
    public string NameEn { get; }
    public MonsterClassKind Classification { get; }
    public MonsterSizeKind SizeClass { get; }
    public int BaseMaxHp { get; }           // 1..100000
    public int BaseSpeed { get; }           // 5..60
    public string OpenerMoveId { get; }
    public IReadOnlyList<MonsterMoveDef> Moves { get; }
    public IReadOnlyList<BossPhaseDef> BossPhases { get; }
}

public sealed class GameCatalog
{
    public IReadOnlyList<ItemDef> Items { get; }     // sorted or authored + ordinal lookup
    public IReadOnlyList<HeroDef> Heroes { get; }
    public IReadOnlyList<MonsterDef> Monsters { get; }

    public ItemDef FindItem(string id);
    public HeroDef FindHero(string id);
    public MonsterDef FindMonster(string id);

    public static GameCatalog LoadFromJson(
        string itemsJson,
        string heroesJson,
        string monstersJson,
        out IReadOnlyList<string> validationErrors);
}
```

### 4.3 Seeded RNG (`SeededRng`)
```csharp
public sealed class RngStreamSnapshot
{
    public string Name { get; }
    public ulong State { get; }
    public ulong Increment { get; }
}

public sealed class RngSnapshot
{
    public ulong Seed { get; }
    public int Version { get; }
    public IReadOnlyList<RngStreamSnapshot> Streams { get; }
}

public sealed class RngStream
{
    public string Name { get; }
    public uint NextUInt32();
    public int NextBounded(int bound);               // 1 <= bound
    public int NextWeighted(IReadOnlyList<int> weights);
    public RngStream Clone();
}

public sealed class SeededRng
{
    public const int CurrentVersion = 1;
    public static readonly string[] StreamNames = { "doors", "drops", "intents" };

    public SeededRng(ulong seed);
    public ulong Seed { get; }
    public RngStream GetStream(string name);         // throws ArgumentException on unknown stream
    public RngSnapshot CaptureSnapshot();
    public bool TryRestore(RngSnapshot snapshot, out string error);
    public SeededRng Clone();
}
```
- Stream derivation: ASCII bytes of `streamName` mixed with `seed` via SplitMix64 (no `string.GetHashCode()`), initializing PCG-XSH-RR `(state, inc)` with `inc = (mixed | 1UL)`.

### 4.4 Grid Backpack & Adjacency Synergy
```csharp
public interface IItemIdSource
{
    long Next();
}

public sealed class SequentialItemIdSource : IItemIdSource
{
    public SequentialItemIdSource(long startAt = 1);
    public long Current { get; }
    public long Next();
}

public sealed class PlacedItemView
{
    public long InstanceId { get; }
    public string DefinitionId { get; }
    public ContainerKind Container { get; }
    public int OriginX { get; }
    public int OriginY { get; }
    public int Orientation { get; }
    public GridCell AnchorCell { get; }
    public int ReadingIndex { get; }
    public IReadOnlyList<GridCell> OccupiedCells { get; }
}

public sealed class TrayItemView
{
    public long InstanceId { get; }
    public string DefinitionId { get; }
    public int TrayEntryNumber { get; }
}

public sealed class ArrangementGridView
{
    public ContainerKind Container { get; }
    public int Width { get; }
    public int Height { get; }
    public bool IsOpen(int x, int y);
    public IReadOnlyList<PlacedItemView> Items { get; }
}

public sealed class ArrangementView
{
    public bool IsFrozen { get; }
    public IReadOnlyList<ArrangementGridView> HeroGrids { get; }
    public ArrangementGridView Stash { get; }
    public IReadOnlyList<TrayItemView> Tray { get; }
}

public sealed class GridPreviewResult
{
    public GridOpCode Code { get; }
    public IReadOnlyList<GridCell> OccupiedCells { get; }
    public IReadOnlyList<long> BlockingItemIds { get; }
    public int ProjectedReadingIndex { get; }
}

public sealed class GridOpResult
{
    public GridOpCode Code { get; }
    public bool IsOk => Code == GridOpCode.Ok;
    public int CandidatesTried { get; }
    public int CellsUnlocked { get; }
}

public sealed class BackpackArrangement
{
    public BackpackArrangement(
        GameCatalog catalog,
        int heroCount,
        IReadOnlyList<BackpackProfileDef> heroProfiles,
        IItemIdSource idSource,
        int stashWidth = 8,
        int stashHeight = 2,
        bool rotationEnabled = true);

    public bool IsFrozen { get; }
    public int UndoDepth { get; }

    public GridOpResult CreateInTray(string definitionId, out long instanceId);
    public GridOpResult Place(long instanceId, ContainerKind container, int originX, int originY, int orientation);
    public GridOpResult AutoPlace(long instanceId, ContainerKind container);
    public GridOpResult RemoveToTray(long instanceId);
    public GridOpResult DestroyTrayItem(long instanceId);
    public GridOpResult UnlockHeroCells(int heroSlot, int count);
    public GridOpResult Undo();
    public GridPreviewResult PreviewPlacement(string definitionId, long existingInstanceId, ContainerKind container, int originX, int originY, int orientation);
    public IReadOnlyList<long> Freeze();
    public GridOpResult Thaw();
    public ArrangementView CaptureView();
    public string Snapshot();
    public GridOpResult Restore(string snapshotText);

    public static IReadOnlyList<GridCell> RotateFootprint(IReadOnlyList<GridCell> baseCells, int orientation);
    public static GridCell ComputeAnchor(IReadOnlyList<GridCell> occupiedCells);
}
```
> Note on Stash dimensions: `CombatSceneBuilder.cs` renders an `8×2` (16-cell, `160×40 px`) StashBed at `x=7, y=152`. Defaulting `stashWidth = 8, stashHeight = 2` (or configurable `5×5` in tests) lets `BackpackArrangement` match both GDD `5×5` test fixtures AND the exact `8×2` scene geometry seamlessly!

```csharp
public sealed class SynergyLink
{
    public long SourceInstanceId { get; }
    public string SourceDefId { get; }
    public GridCell SourceAnchor { get; }
    public int RuleIndex { get; }
    public long TargetInstanceId { get; }
    public SynergyKind Kind { get; }
    public int Amount { get; }
    public string StatusId { get; }
    public GridCell SeamSourceCell { get; }
    public GridCell SeamTargetCell { get; }
}

public sealed class ItemSynergySummary
{
    public long TargetInstanceId { get; }
    public int DamageFlatTotal { get; }
    public int DamagePercentTotal { get; }
    public int CooldownReductionTotal { get; }
    public int GetStatusAmountBonus(string statusId);
    public IReadOnlyList<SynergyLink> IncomingLinks { get; }
}

public sealed class GridSynergyView
{
    public IReadOnlyList<ItemSynergySummary> Items { get; }
    public IReadOnlyList<SynergyLink> AllLinks { get; }
    public ItemSynergySummary FindForItem(long instanceId);
}

public sealed class SynergyPreviewResult
{
    public GridOpCode Code { get; }
    public GridSynergyView ProjectedView { get; }
    public IReadOnlyList<SynergyLink> GainedLinks { get; }
    public IReadOnlyList<SynergyLink> LostLinks { get; }
}

public static class AdjacencySynergyEvaluator
{
    public static GridSynergyView Evaluate(ArrangementGridView heroGrid, GameCatalog catalog);
    public static SynergyPreviewResult PreviewPlacement(
        ArrangementGridView heroGrid,
        ItemDef item,
        long existingInstanceId,
        int originX,
        int originY,
        int orientation,
        GameCatalog catalog);
}
```

### 4.5 Turn Order, Enemy Intent, Event Log, Snapshots & CombatRunner
```csharp
public sealed class TurnForecastEntry
{
    public UnitRef Unit { get; }
    public int DeltaTime { get; }
    public int GlobalIndex { get; }      // 1-based action index across all units
    public int OwnerActionOrdinal { get; }
    public MoveGlyphKind EnemyIntentGlyph { get; }
}

public sealed class BoundIntentEntry
{
    public int EntryIndex { get; }
    public string MoveId { get; }
    public MoveGlyphKind Glyph { get; }
    public DeliveryKind Delivery { get; }
    public int HitCount { get; }
    public int ScaledBaseDamage { get; }
    public MoveTargetMode TargetMode { get; }
    public int BoundHeroInstanceId { get; } // -1 if AllHeroes or targetless Windup
}

public sealed class BoundEnemyState
{
    public int EnemyInstanceId { get; }
    public int Slot { get; }
    public string DefinitionId { get; }
    public int ScaledMaxHp { get; }
    public int EffectiveSpeed { get; }
    public IReadOnlyList<BoundIntentEntry> Sequence { get; }
}

public sealed class HeroBaselineInput
{
    public int Slot { get; }
    public HeroDef Definition { get; }
    public int PermanentHpBonus { get; }
    public ArrangementGridView FrozenGrid { get; }
    public GridSynergyView FrozenSynergy { get; }
}

public sealed class EnemySpawnInput
{
    public int Slot { get; }                // 0..5
    public MonsterDef Definition { get; }
    public int HpPermille { get; }          // default 1000 (range 500..10000)
    public int DamagePermille { get; }      // default 1000 (range 500..5000)
}

public sealed class EncounterSetup
{
    public int EncounterId { get; }
    public int StageNumber { get; }
    public IReadOnlyList<HeroBaselineInput> Heroes { get; }   // 1..3
    public IReadOnlyList<EnemySpawnInput> Enemies { get; }    // 1..6
}

public sealed class AppliedModifierRef
{
    public long SourceItemInstanceId { get; }
    public string SourceItemDefId { get; }
    public int RuleIndex { get; }
    public SynergyKind Kind { get; }
    public int Amount { get; }
}

public sealed class CombatOrigin
{
    public OriginKind Kind { get; }
    public int HeroSlot { get; }
    public long ItemInstanceId { get; }
    public string ItemDefId { get; }
    public GridCell AnchorCell { get; }
    public int EnemyInstanceId { get; }
    public string MonsterDefId { get; }
    public string MoveId { get; }
    public long ActionOccurrenceId { get; }
    public IReadOnlyList<AppliedModifierRef> AppliedModifiers { get; }
}

public sealed class StatusSourceContribution
{
    public bool IsBaseline { get; }
    public long SourceRefId { get; }         // Event Seq or baseline contribution ID
    public int AmountConsumed { get; }
}

public sealed class CombatEventRecord
{
    public long Seq { get; }
    public CombatEventKind Kind { get; }
    public long RootActionId { get; }
    public int Depth { get; }
    public long? ParentEffectSeq { get; }
    public long EffectOrdinal { get; }
    public CombatOrigin Origin { get; }
    public UnitRef Actor { get; }
    public UnitRef Target { get; }
    public TriggerKind? TriggerKind { get; }
    public int ActionOrdinal { get; }
    public int NextReadyAt { get; }
    public int DeltaTime { get; }
    public int GaugeBefore { get; }
    public int GaugeAfter { get; }
    public string StatusId { get; }
    public long StatusAmount { get; }
    public int ExpiryActionOrdinal { get; }
    public long OccurrenceId { get; }
    public IReadOnlyList<StatusSourceContribution> ConsumedSources { get; }
    public int ResolvedAmount { get; }
    public int ShieldBefore { get; }
    public int ShieldAfter { get; }
    public int HpBefore { get; }
    public int HpAfter { get; }
    public string ReasonCode { get; }
    public CombatOutcome? Outcome { get; }
}

public sealed class ActiveStatusView
{
    public string StatusId { get; }
    public long TotalAmount { get; }
}

public sealed class ItemCooldownView
{
    public long InstanceId { get; }
    public string DefinitionId { get; }
    public TriggerKind TriggerKind { get; }
    public int EffectiveCooldown { get; }
    public int NextReadyAt { get; }
    public int ActionsRemaining { get; }    // max(1, NextReadyAt - OwnerActionOrdinal) or 0 if ready
    public bool IsReady { get; }
}

public sealed class HeroStateView
{
    public int Slot { get; }
    public int InstanceId { get; }
    public string DefinitionId { get; }
    public int Hp { get; }
    public int MaxHp { get; }
    public int Shield { get; }
    public int Gauge { get; }
    public int EffectiveSpeed { get; }
    public int ActionOrdinal { get; }
    public bool IsAlive { get; }
    public IReadOnlyList<ActiveStatusView> Statuses { get; }
    public IReadOnlyList<ItemCooldownView> ItemCooldowns { get; }
}

public sealed class EnemyStateView
{
    public int Slot { get; }
    public int InstanceId { get; }
    public string DefinitionId { get; }
    public int Hp { get; }
    public int MaxHp { get; }
    public int Shield { get; }
    public int Gauge { get; }
    public int EffectiveSpeed { get; }
    public int ActionOrdinal { get; }
    public int IntentIndex { get; }
    public BoundIntentEntry CurrentIntent { get; }
    public bool IsAlive { get; }
    public IReadOnlyList<ActiveStatusView> Statuses { get; }
}

public sealed class CombatStateSnapshot
{
    public long EventSeq { get; }
    public int CompletedActions { get; }
    public IReadOnlyList<HeroStateView> Heroes { get; }
    public IReadOnlyList<EnemyStateView> Enemies { get; }
    public IReadOnlyList<TurnForecastEntry> Forecast { get; }
}

public sealed class CombatTrace
{
    public int EncounterId { get; }
    public int RulesVersion { get; }
    public CombatStateSnapshot Baseline { get; }
    public IReadOnlyList<CombatEventRecord> Events { get; }
    public IReadOnlyList<CombatStateSnapshot> SnapshotsBySeq { get; } // index 0 = Baseline, index seq = post-event view
}

public sealed class PreparedEncounter
{
    public EncounterSetup Setup { get; }
    public IReadOnlyList<HeroStateView> InitialHeroes { get; }
    public IReadOnlyList<BoundEnemyState> BoundEnemies { get; }
    public IReadOnlyList<TurnForecastEntry> InitialForecast { get; }
}

public sealed class CombatResult
{
    public CombatRunStatus Status { get; }
    public CombatOutcome? Outcome { get; }
    public string StopDiagnostic { get; }
    public CombatTrace Trace { get; }
}

public static class CombatRunner
{
    public const int MaxActorActions = 200;
    public const int MaxReactionDepth = 8;
    public const int MaxEffectsPerAction = 256;
    public const int GaugeThreshold = 1000;
    public const int IntentHorizon = 12;

    public static bool TryPrepare(
        GameCatalog catalog,
        EncounterSetup setup,
        SeededRng rng,
        out PreparedEncounter prepared,
        out string error);

    public static CombatResult RunEncounter(
        GameCatalog catalog,
        PreparedEncounter prepared);

    // Convenience: Prepare + RunEncounter in one call
    public static CombatResult RunEncounter(
        GameCatalog catalog,
        EncounterSetup setup,
        SeededRng rng);
}
```
> **Pure Planning Forecast Helper:** `@combat-ui` also needs the live Turn Order Forecast (`Forecast(6)`) **while the player rearranges items before clicking Fight** without mutating `rng` or `PreparedEncounter`!
> Let's expose a static pure helper on `TurnOrderScheduler`:
> ```csharp
> public static class TurnOrderScheduler
> {
>     public static int EffectiveSpeed(int baseSpeed, int flatBonus, int percentPermille = 0);
>     public static IReadOnlyList<TurnForecastEntry> ForecastPlanning(
>         IReadOnlyList<HeroBaselineInput> heroes,
>         IReadOnlyList<BoundEnemyState> boundEnemies,
>         GameCatalog catalog,
>         int count = 6);
> }
> ```
> Because `PreparedEncounter` already holds `BoundEnemies` (bound once when entering the room), calling `TurnOrderScheduler.ForecastPlanning(...)` after any drag/drop/rotate recomputes the Hero's `EffectiveSpeed` (base + passive `speedBonus` in grid) and updates `OrderStripView` immediately with zero RNG draws!

---

## 5. JSON Data Schemas (`Assets/_Project/Data/`)

### 5.1 `items_mvp.json` (Existing 23 items + New 1-Hero Items)
- Existing 23 items remain untouched in `id`, `art`, `tier`, `footprint`, `cells`, `tags`, `trigger`, `effects`, `hpBonus`, `speedBonus`, `synergy`, `nameVi`, `nameEn`, `flavorVi`, `flavorEn`.
- **Supported Trigger / Effect Extensions** (confirmed with `@sim-core`):
  - `effects[].kind`: `"Damage"`, `"ApplyStatus"`, `"ConsumeDetonate"`, `"ShieldGain"`, `"Heal"`.
  - `effects[].target`: `"FirstEnemy"`, `"AllEnemies"`, `"Self"`, `"AllHeroes"`, `"LowestHpHero"` (default `"FirstEnemy"` if omitted on `Damage`/`ApplyStatus`/`ConsumeDetonate`; `"Self"` valid on `ShieldGain`/`Heal`).
  - `trigger.kind`: `"Passive"`, `"Cooldown"`, `"Reaction"`, `"CombatStart"`.
  - `trigger.eventKind`: `"ItemFired"`, `"StatusApplied"`, `"StatusConsumed"`, `"Damage"`, `"UnitDowned"`.
  - `trigger.eventSide`: `"any"`, `"hero"`, `"enemy"`, `"self"` / `"own_hero"`, `"other_hero"` (case-insensitive; `"self"` and `"own_hero"` both map to `ReactionSideFilter.OwnHero`).
  - `synergy[].kind`: strictly keep the 4 GDD kinds (`"DamageFlat"`, `"DamagePercent"`, `"CooldownReduction"`, `"StatusAmount"`).
- **New Hero Items (`@items-data`)**: Keep all 23 existing items and add ~6–10 solo-viable Tank/Crusader items (e.g. `ShieldGain`, `Heal`, `AllEnemies` cleave, `AllEnemies` Seal/Detonate, `Damage`-taken `Reaction`) with matching `item_<art>_d_<fp>.png` / `_c_<fp>.png` icons generated via `tools/art/build_items.py` so zero sprite paths break.
- Update `ItemRecords.cs` / `ItemTooltipText.cs` to format `"ShieldGain"`, `"Heal"`, `"Self"`, `"AllEnemies"` on `ConsumeDetonate`, and `"Reaction(Damage, self)"`.

### 5.2 `heroes_mvp.json` (New)
Canonical schema (note: `GameCatalog` should also accept `baseHp` alias for `baseMaxHp`, `backpack` alias for `backpackProfile`, and `{ "itemId", "isSignature" }` objects in `startingItems` if present):
```json
{
  "heroes": [
    {
      "id": "hero_crusader",
      "art": "tank",
      "nameVi": "Kỵ Sĩ Hắc Quang",
      "nameEn": "Duskblade Crusader",
      "roleVi": "Đỡ Đòn / Khắc Ấn",
      "roleEn": "Tank / Seal Bruiser",
      "baseMaxHp": 110,
      "baseSpeed": 22,
      "initialShield": 8,
      "backpackProfile": {
        "width": 4,
        "height": 4,
        "startWidth": 4,
        "startHeight": 4,
        "unlockSequence": []
      },
      "startingItems": [
        "item_iron_sword",
        "item_spiked_buckler",
        "item_seal_ink",
        "item_whetstone"
      ],
      "signatureItemId": "item_spiked_buckler",
      "innateItem": {
        "id": "innate_crusader_vow",
        "nameVi": "Lời Thề Thiết Giáp",
        "nameEn": "Ironclad Vow",
        "descVi": "Đầu trận: +6 Giáp, áp 1 Ấn lên mọi kẻ địch.",
        "descEn": "Combat start: +6 Shield, apply 1 Seal to all enemies.",
        "tags": ["relic", "shield"],
        "trigger": { "kind": "CombatStart", "n": 0, "s": 0, "eventKind": "", "eventStatus": "", "eventSide": "" },
        "effects": [
          { "kind": "ShieldGain", "target": "Self", "base": 0, "status": "", "amount": 6, "units": 0, "perUnit": 0 },
          { "kind": "ApplyStatus", "target": "AllEnemies", "base": 0, "status": "an", "amount": 1, "units": 0, "perUnit": 0 }
        ]
      }
    }
  ]
}
```
Additional heroes (`hero_alchemist`, `hero_ranger`, `hero_cleric`) may also be authored in `heroes_mvp.json` for future 3-hero mode; `CombatSceneController` loads `hero_crusader` (`art: "tank"`) as active Slot 0 in 1-hero mode.

### 5.3 `monsters_mvp.json` (New)
Canonical schema matching `enemy-and-intent.md`, `encounter-generation.md`, and `HudSpriteSet.monsters` (`"swarmer"` -> index `0`, `bodyHeight: 26`; `"brute"` -> index `1`, `bodyHeight: 44`). (`GameCatalog` should accept `baseHp` alias for `baseMaxHp`, `linkedNext` alias for `linkedNextMoveId`, and `phases` alias for `bossPhases`):
- Required MVP roster (plus optional `mon_striker` / `mon_boss`):
  - `mon_hunter` (`visual: "swarmer"`, `bodyHeight: 26`, Small, Regular, HP `52`, Speed `20`, Strike `B=7`)
  - `mon_swarmer` (`visual: "swarmer"`, `bodyHeight: 26`, Small, Regular, HP `38`, Speed `26`, Multi-hit `3×4`)
  - `mon_sweeper` (`visual: "brute"`, `bodyHeight: 44`, Medium, Regular, HP `56`, Speed `18`, Sweep `B=5` `AllHeroes`)
  - `mon_brute` (`visual: "brute"`, `bodyHeight: 44`, Large, Regular, HP `88`, Speed `19`, Opener Strike `B=7`, Windup -> Heavy `B=14`)
  - `mon_elite` / `mon_elite_bruiser` (`visual: "brute"`, `bodyHeight: 44`, Large, Elite, HP `140`, Speed `21`, Strike `B=10`, Plain Heavy `B=17`)
- Optional `"encounterPresets"` / `"tierTable"` object may also be included in `monsters_mvp.json` for stage scaling (`1000 + 60*(r-1)`, `1000 + 40*(r-1)`).

---

## 6. Combat.unity Runtime Architecture (`Project05.CombatScene`)

Directory: `Assets/_Project/Scripts/CombatScene/` (inside `Project05.Runtime` assembly, referencing `Project05.CombatCore` in `Project05.Runtime.asmdef`).

### 6.1 Component Breakdown
1. **`CombatSceneController.cs`** (MonoBehaviour attached to `CombatCanvas` / `CombatSceneRoot`):
   - Loads `items_mvp.json`, `heroes_mvp.json`, `monsters_mvp.json` via `TextAsset`s serialized by `CombatSceneBuilder` using `GameCatalog.LoadFromJson(...)`.
   - Initializes `BackpackArrangement` (1 hero, 4×4 Hero0 grid + 8×2 Stash, `SequentialItemIdSource`), places starting items (`hero_crusader` loadout + starter spare items in Stash), and manages `RunSeed` + `CurrentStage` (`1..8+`) + `NextStageMonsterCount` (`1..6`, default `2`).
   - Calls `CombatRunner.TryPrepare(...)` whenever entering a stage (binds intents once from `SeededRng.GetStream("intents")`).
   - State machine: `Planning` -> `Playing` -> `PostCombat` -> `Planning` (next stage).
   - **On "Chiến đấu / Fight"**: checks if Tray is empty (or confirms/destroys Tray items on Freeze), freezes `BackpackArrangement` & evaluates `GridSynergyView`, runs `CombatRunner.RunEncounter(catalog, prepared)`, receives immutable `CombatTrace`, and hands it to `CombatPlaybackController`.
   - **On "Màn tiếp theo / Next Stage"**: thaws `BackpackArrangement`, increments `Stage`, generates `NextStageMonsterCount` (`1..6`) monsters scaled by stage (`m_hp = 1000 + 60*(stage-1)`, `m_dmg = 1000 + 40*(stage-1)`), restores Hero HP/Shield automatically via new `PreparedEncounter`, preserves hero backpack & stash items, updates HUD + Forecast.
   - **On "Đánh lại / Retry"**: thaws `BackpackArrangement`, re-prepares the same stage/seed/monster count so the player can tweak item placement and retry!

2. **`WorkbenchController.cs`** (Interactive 4×4 Grid + 8×2 Stash + Equipment Picker):
   - **Hero 4×4 Grid** (`origin (7, 40)`, cell `20×20`) and **Stash 8×2 Grid** (`origin (7, 152)`, cell `20×20`).
   - **Workbench Mode Toggle Tabs** right above the lower panel (`Kho đồ` | `Chọn đồ`):
     - **Kho đồ (Stash)**: shows the `8×2` stash grid where spare owned items can be dragged into/out of the hero's 4×4 grid.
     - **Chọn đồ (Equipment Picker)**: displays a compact scrollable/paged catalog grid of all items from `items_mvp.json` (showing tier plate + icon + hover popup). Clicking any catalog item spawns a fresh instance via `CreateInTray` and calls `AutoPlace(..., ContainerKind.Hero0)` (or `ContainerKind.Stash` if hero grid is full; or begins a held drag if both are full!). Also provides a **"Cất / Bỏ" (Unequip/Discard)** action or right-click/Trash drop to remove items from the hero grid back to Stash or remove unwanted items.
   - **Drag / Drop / Rotate (`R` key)**:
     - Left-click & drag any item plate in Hero Grid or Stash.
     - While dragging, pressing `R` (via `UnityEngine.InputSystem.Keyboard.current.rKey.wasPressedThisFrame` or right-click) increments `orientation = (orientation + 1) % 4` and updates the ghost footprint & preview highlight!
     - Calls `BackpackArrangement.PreviewPlacement(...)` + `AdjacencySynergyEvaluator.PreviewPlacement(...)` on hovered cell `(ox, oy)`:
       - Valid free placement -> green/Candle cell highlight + preview synergy bridges.
       - Invalid/Occupied -> red/Blood cell highlight.
     - On drop: commits `BackpackArrangement.Place(...)`, refreshes item plate GameObjects (rotating visual footprint cleanly via `RotateFootprint` bounds or `RectTransform` rotation), refreshes `ItemGridContext` + resting synergy bridges + hero preview stats (`MaxHp`, `Shield`, `EffectiveSpeed`) + `TurnOrderScheduler.ForecastPlanning(...)` -> `OrderStripView`.
   - **Popup Integration**: keeps `ItemHoverTarget` + `ItemPopupController` working on Hero Grid items, Stash items, AND Equipment Picker items!

3. **`NextStageTestPanel.cs`** (Next-Stage Monster Count `1..6` Test System):
   - Docked cleanly in the `ActionStrip` during Planning (`[1] [2] [3] [4] [5] [6]` selector buttons + label `"Quái màn sau: N"` + Stage/Seed readout) AND mirrored in the Post-Combat Result overlay (`"Màn tiếp theo (N quái)"`) so testers can pick `1..6` either before or right after a fight!
   - Also provides an optional **"Áp dụng ngay" (Apply Now)** button in Planning (or re-rolls current planning encounter with `N` monsters) so the tester doesn't even have to fight a dummy encounter first if they immediately want to test `N=6` monsters right on Stage 1, while preserving the primary `"Quái màn sau: 1..6"` next-stage behavior!

4. **`CombatPlaybackController.cs`** & **`CombatHudMapper.cs`**:
   - Reads `CombatTrace.Events[seq]` and `CombatTrace.SnapshotsBySeq[seq]` step by step.
   - Pure `CombatHudMapper.MapSnapshot(PreparedEncounter prepared, CombatStateSnapshot snap, CombatEventRecord currentEvent, ...)` maps:
     - Slot 0 Hero (`Heroes[0]`): HP, MaxHp, Armour (`Shield`), SpeedGauge (`gauge * 18 / 1000` or mapped cleanly to `UnitRowsView`), Statuses (`"an"` -> `StatusKind.Seal`), Pose (`Idle1`/`Hurt1`/`Down2`), `Acting`, `Avatars` (from `ItemFired`), `Readouts` (from `Damage`/`DetonationDamage`/`ShieldGained`/`Healed`).
     - Slots 1–2 (`Heroes[1] = null`, `Heroes[2] = null`) -> hidden.
     - Enemies `0..5`: `Present = i < snap.Enemies.Count`, `Visual = 0` (`swarmer`) or `1` (`brute`), HP, MaxHp, Gauge, Statuses, `Intent`, `Magnitude` (`r` or `r×h`), `TargetJoinNumeral = 1` (or `0` for `AllHeroes`), Pose (`Idle1`/`Act`/`Hurt`/`Die2`), `Readouts`.
     - `OrderStripView`: mapped directly from `snap.Forecast` (horizon 6).
   - `SpeedControlView` (`1x`, `2x`, `4x`, `Skip`): controls step hold duration (`0.45s`, `0.22s`, `0.11s`, or immediate jump to end on `Skip`).
   - Post-Combat Result Panel in Stage right column (`Art Bible 7.5`): shows `CHIẾN THẮNG` / `THẤT BẠI` / `HÒA`, action count, biggest hit, total damage, monster count selector `1..6` for next stage, **"Màn tiếp theo"** and **"Đánh lại"**.

5. **`CombatHudDemo.cs` & `CombatSceneBuilder.cs`**:
   - `CombatHudDemo.cs`: in `OnEnable()`, `if (Application.isPlaying) return;` so EditMode scene previews & `CaptureEvidence` continue to work identically, while Play Mode is driven 100% by `CombatSceneController`.
   - `CombatSceneBuilder.cs`: builds `Combat.unity` with `CombatSceneController`, `WorkbenchController`, `CombatPlaybackController`, `NextStageTestPanel`, `Fight` button, `Kho đồ | Chọn đồ` tabs, `PostCombatPanel`, and serializes `items_mvp.json`, `heroes_mvp.json`, `monsters_mvp.json` `TextAsset`s onto `CombatSceneController`. Also keeps `NameJoin = "1"` and `NameLabel = "Kỵ Sĩ Hắc Quang"`.

---

## 7. Work Breakdown & Sequencing

1. **Phase 1 — Data & Core Contracts** (Parallel: `@sim-core`, `@items-data`):
   - `@items-data`: Update `tools/art/item_catalog.py` (+ `build_items.py` if new icons generated), write `items_mvp.json`, `heroes_mvp.json`, `monsters_mvp.json`, `design/content/mvp-item-catalog.md`, and sync `ItemRecords.cs` / `ItemTooltipText.cs`.
   - `@sim-core`: Implement `Assets/_Project/Scripts/CombatCore/` (`Project05.CombatCore.asmdef`, `SimpleJsonParser`, `GameCatalog`, `SeededRng`, `BackpackArrangement`, `AdjacencySynergyEvaluator`, `TurnOrderScheduler`, `CombatState` / `CombatTrace`, `EffectResolver`, `EnemyIntentSelector`, `CombatRunner`).
2. **Phase 2 — Combat Scene & UI Integration** (`@combat-ui`):
   - Add `Project05.CombatCore` to `Project05.Runtime.asmdef`.
   - Implement `Assets/_Project/Scripts/CombatScene/` (`CombatSceneController`, `WorkbenchController`, `EquipmentPickerView`, `NextStageTestPanel`, `CombatPlaybackController`, `CombatHudMapper`, `PostCombatPanelView`).
   - Update `CombatHudDemo.cs` (Play Mode guard) and `CombatSceneBuilder.cs` (wire all controllers, UI panels, and `TextAsset` JSON references).
3. **Phase 3 — Verification** (`@verifier`):
   - Add `Project05.CombatCore` to `Project05.EditModeTests.asmdef`.
   - Write & run `CombatCoreTests.cs` (GDD acceptance criteria: no `UnityEngine` refs, no mutable static fields, no float/double, determinism parity, 2-source oldest-first `an` detonation, FIFO `D=8`/`N=256`, once-per-root suppression, binding intents, 1..6 enemy slots, 200-action cap, `ShieldGain`/`Heal`) and `CombatSceneIntegrationTests.cs` (catalog loading, 1-hero HUD mapping, picker/drag/rotate/remove, next-stage 1..6 spawning).
