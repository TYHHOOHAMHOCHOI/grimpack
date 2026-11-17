using System;
using System.Collections.Generic;

namespace Project05.CombatCore
{
    /// <summary>
    /// A single predicted future action in the turn-order forecast.
    /// </summary>
    public sealed class TurnForecastEntry
    {
        public UnitRef Unit { get; }
        public int DeltaTime { get; }
        public int GlobalIndex { get; }
        public int OwnerActionOrdinal { get; }
        public MoveGlyphKind EnemyIntentGlyph { get; }

        public TurnForecastEntry(
            UnitRef unit,
            int deltaTime,
            int globalIndex,
            int ownerActionOrdinal,
            MoveGlyphKind enemyIntentGlyph)
        {
            Unit = unit;
            DeltaTime = deltaTime;
            GlobalIndex = globalIndex;
            OwnerActionOrdinal = ownerActionOrdinal;
            EnemyIntentGlyph = enemyIntentGlyph;
        }
    }

    /// <summary>
    /// A pre-rolled enemy move and bound hero target in an enemy's intent sequence.
    /// </summary>
    public sealed class BoundIntentEntry
    {
        public int EntryIndex { get; }
        public string MoveId { get; }
        public MoveGlyphKind Glyph { get; }
        public DeliveryKind Delivery { get; }
        public int HitCount { get; }
        public int ScaledBaseDamage { get; }
        public MoveTargetMode TargetMode { get; }
        public int BoundHeroInstanceId { get; }

        public BoundIntentEntry(
            int entryIndex,
            string moveId,
            MoveGlyphKind glyph,
            DeliveryKind delivery,
            int hitCount,
            int scaledBaseDamage,
            MoveTargetMode targetMode,
            int boundHeroInstanceId)
        {
            EntryIndex = entryIndex;
            MoveId = moveId ?? string.Empty;
            Glyph = glyph;
            Delivery = delivery;
            HitCount = hitCount;
            ScaledBaseDamage = scaledBaseDamage;
            TargetMode = targetMode;
            BoundHeroInstanceId = boundHeroInstanceId;
        }
    }

    /// <summary>
    /// Pre-rolled encounter data for a single enemy spawned into an encounter.
    /// </summary>
    public sealed class BoundEnemyState
    {
        public int EnemyInstanceId { get; }
        public int Slot { get; }
        public string DefinitionId { get; }
        public int ScaledMaxHp { get; }
        public int EffectiveSpeed { get; }
        public IReadOnlyList<BoundIntentEntry> Sequence { get; }

        public BoundEnemyState(
            int enemyInstanceId,
            int slot,
            string definitionId,
            int scaledMaxHp,
            int effectiveSpeed,
            IReadOnlyList<BoundIntentEntry> sequence)
        {
            EnemyInstanceId = enemyInstanceId;
            Slot = slot;
            DefinitionId = definitionId ?? string.Empty;
            ScaledMaxHp = scaledMaxHp;
            EffectiveSpeed = effectiveSpeed;
            Sequence = sequence ?? Array.Empty<BoundIntentEntry>();
        }
    }

    /// <summary>
    /// Baseline setup for a party hero entering combat.
    /// </summary>
    public sealed class HeroBaselineInput
    {
        public int Slot { get; }
        public HeroDef Definition { get; }
        public int PermanentHpBonus { get; }
        public ArrangementGridView FrozenGrid { get; }
        public GridSynergyView FrozenSynergy { get; }

        /// <summary>Signed max-HP term from an active curse (Frailty, or a Pinned curse's HP rider); 0 if none (System 19 Formula 3).</summary>
        public int CurseHpDelta { get; }

        /// <summary>Signed speed term from an active curse (Sluggish, or a Pinned curse's speed rider), added after the item sum; 0 if none.</summary>
        public int CurseSpeedDelta { get; }

        /// <summary>Damage the hero takes at the start of each of its own turns from a Wither curse; 0 if none (System 19 Formula 4).</summary>
        public int WitherPerTurn { get; }

        /// <summary>The curse that the terms above come from, for the trace origin; empty if none.</summary>
        public string CurseId { get; }

        public HeroBaselineInput(
            int slot,
            HeroDef definition,
            int permanentHpBonus,
            ArrangementGridView frozenGrid,
            GridSynergyView frozenSynergy = null,
            int curseHpDelta = 0,
            int curseSpeedDelta = 0,
            int witherPerTurn = 0,
            string curseId = null)
        {
            Slot = slot;
            Definition = definition;
            PermanentHpBonus = permanentHpBonus;
            FrozenGrid = frozenGrid;
            FrozenSynergy = frozenSynergy;
            CurseHpDelta = curseHpDelta;
            CurseSpeedDelta = curseSpeedDelta;
            WitherPerTurn = witherPerTurn < 0 ? 0 : witherPerTurn;
            CurseId = curseId ?? string.Empty;
        }
    }

    /// <summary>
    /// The one place a hero's max HP and effective speed are computed (System 15 HeroStats with System 19's curse terms),
    /// so the planning preview, the forecast and the fight always agree.
    /// </summary>
    public static class HeroStatMath
    {
        /// <summary>maxHP = max(1, h + B_perm + H + C_hp).</summary>
        public static int MaxHp(HeroDef def, int permanentHpBonus, int itemHpBonus, int curseHpDelta)
        {
            long raw = (long)(def != null ? def.BaseMaxHp : 1) + permanentHpBonus + itemHpBonus + curseHpDelta;
            if (raw < 1L) return 1;
            return raw > int.MaxValue ? int.MaxValue : (int)raw;
        }

        /// <summary>Effective speed with the curse term added after the item sum, so the curse always bites.</summary>
        public static int Speed(HeroDef def, int itemSpeedBonus, int curseSpeedDelta) =>
            TurnOrderScheduler.EffectiveSpeed(def != null ? def.BaseSpeed : 1, itemSpeedBonus + curseSpeedDelta, 0);

        /// <summary>Max HP of a hero baseline as the fight will use it.</summary>
        public static int MaxHp(HeroBaselineInput input, GameCatalog catalog) =>
            MaxHp(input.Definition, input.PermanentHpBonus, TurnOrderScheduler.ComputeHeroHpBonus(input.FrozenGrid, catalog), input.CurseHpDelta);

        /// <summary>Effective speed of a hero baseline as the fight will use it.</summary>
        public static int Speed(HeroBaselineInput input, GameCatalog catalog) =>
            Speed(input.Definition, TurnOrderScheduler.ComputeHeroSpeedBonus(input.FrozenGrid, catalog), input.CurseSpeedDelta);
    }

    /// <summary>
    /// Spawn parameters for an enemy entering an encounter.
    /// </summary>
    public sealed class EnemySpawnInput
    {
        public int Slot { get; }
        public MonsterDef Definition { get; }
        public int HpPermille { get; }
        public int DamagePermille { get; }

        public EnemySpawnInput(
            int slot,
            MonsterDef definition,
            int hpPermille = 1000,
            int damagePermille = 1000)
        {
            Slot = slot;
            Definition = definition;
            HpPermille = hpPermille;
            DamagePermille = damagePermille;
        }
    }

    /// <summary>
    /// Full encounter setup provided to <see cref="CombatRunner.TryPrepare"/>.
    /// </summary>
    public sealed class EncounterSetup
    {
        public int EncounterId { get; }
        public int StageNumber { get; }
        public IReadOnlyList<HeroBaselineInput> Heroes { get; }
        public IReadOnlyList<EnemySpawnInput> Enemies { get; }

        public EncounterSetup(
            int encounterId,
            int stageNumber,
            IReadOnlyList<HeroBaselineInput> heroes,
            IReadOnlyList<EnemySpawnInput> enemies)
        {
            EncounterId = encounterId;
            StageNumber = stageNumber;
            Heroes = heroes ?? Array.Empty<HeroBaselineInput>();
            Enemies = enemies ?? Array.Empty<EnemySpawnInput>();
        }
    }

    /// <summary>
    /// Provenance reference to a single adjacency synergy modifier applied to an effect.
    /// </summary>
    public sealed class AppliedModifierRef
    {
        public long SourceItemInstanceId { get; }
        public string SourceItemDefId { get; }
        public int RuleIndex { get; }
        public SynergyKind Kind { get; }
        public int Amount { get; }

        public AppliedModifierRef(
            long sourceItemInstanceId,
            string sourceItemDefId,
            int ruleIndex,
            SynergyKind kind,
            int amount)
        {
            SourceItemInstanceId = sourceItemInstanceId;
            SourceItemDefId = sourceItemDefId ?? string.Empty;
            RuleIndex = ruleIndex;
            Kind = kind;
            Amount = amount;
        }
    }

    /// <summary>
    /// Causal origin metadata for a combat event or queued effect.
    /// </summary>
    public sealed class CombatOrigin
    {
        public static readonly CombatOrigin System = new CombatOrigin(
            OriginKind.None,
            -1,
            0L,
            string.Empty,
            new GridCell(0, 0),
            -1,
            string.Empty,
            string.Empty,
            0L,
            Array.Empty<AppliedModifierRef>());

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

        public CombatOrigin(
            OriginKind kind,
            int heroSlot,
            long itemInstanceId,
            string itemDefId,
            GridCell anchorCell,
            int enemyInstanceId,
            string monsterDefId,
            string moveId,
            long actionOccurrenceId,
            IReadOnlyList<AppliedModifierRef> appliedModifiers)
        {
            Kind = kind;
            HeroSlot = heroSlot;
            ItemInstanceId = itemInstanceId;
            ItemDefId = itemDefId ?? string.Empty;
            AnchorCell = anchorCell;
            EnemyInstanceId = enemyInstanceId;
            MonsterDefId = monsterDefId ?? string.Empty;
            MoveId = moveId ?? string.Empty;
            ActionOccurrenceId = actionOccurrenceId;
            AppliedModifiers = appliedModifiers ?? Array.Empty<AppliedModifierRef>();
        }

        public CombatOrigin WithAppliedModifiers(IReadOnlyList<AppliedModifierRef> modifiers)
        {
            return new CombatOrigin(
                Kind,
                HeroSlot,
                ItemInstanceId,
                ItemDefId,
                AnchorCell,
                EnemyInstanceId,
                MonsterDefId,
                MoveId,
                ActionOccurrenceId,
                modifiers);
        }
    }

    /// <summary>
    /// Provenance breakdown of a single status contribution consumed during detonation.
    /// </summary>
    public sealed class StatusSourceContribution
    {
        public bool IsBaseline { get; }
        public long SourceRefId { get; }
        public int AmountConsumed { get; }

        public StatusSourceContribution(bool isBaseline, long sourceRefId, int amountConsumed)
        {
            IsBaseline = isBaseline;
            SourceRefId = sourceRefId;
            AmountConsumed = amountConsumed;
        }
    }

    /// <summary>
    /// Immutable typed event record stored in the append-only encounter trace.
    /// </summary>
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

        public CombatEventRecord(
            long seq,
            CombatEventKind kind,
            long rootActionId,
            int depth,
            long? parentEffectSeq,
            long effectOrdinal,
            CombatOrigin origin,
            UnitRef actor,
            UnitRef target,
            TriggerKind? triggerKind,
            int actionOrdinal,
            int nextReadyAt,
            int deltaTime,
            int gaugeBefore,
            int gaugeAfter,
            string statusId,
            long statusAmount,
            int expiryActionOrdinal,
            long occurrenceId,
            IReadOnlyList<StatusSourceContribution> consumedSources,
            int resolvedAmount,
            int shieldBefore,
            int shieldAfter,
            int hpBefore,
            int hpAfter,
            string reasonCode,
            CombatOutcome? outcome)
        {
            Seq = seq;
            Kind = kind;
            RootActionId = rootActionId;
            Depth = depth;
            ParentEffectSeq = parentEffectSeq;
            EffectOrdinal = effectOrdinal;
            Origin = origin ?? CombatOrigin.System;
            Actor = actor;
            Target = target;
            TriggerKind = triggerKind;
            ActionOrdinal = actionOrdinal;
            NextReadyAt = nextReadyAt;
            DeltaTime = deltaTime;
            GaugeBefore = gaugeBefore;
            GaugeAfter = gaugeAfter;
            StatusId = statusId ?? string.Empty;
            StatusAmount = statusAmount;
            ExpiryActionOrdinal = expiryActionOrdinal;
            OccurrenceId = occurrenceId;
            ConsumedSources = consumedSources ?? Array.Empty<StatusSourceContribution>();
            ResolvedAmount = resolvedAmount;
            ShieldBefore = shieldBefore;
            ShieldAfter = shieldAfter;
            HpBefore = hpBefore;
            HpAfter = hpAfter;
            ReasonCode = reasonCode ?? string.Empty;
            Outcome = outcome;
        }
    }

    /// <summary>
    /// Aggregated active status view on a unit at a snapshot.
    /// </summary>
    public sealed class ActiveStatusView
    {
        public string StatusId { get; }
        public long TotalAmount { get; }

        public ActiveStatusView(string statusId, long totalAmount)
        {
            StatusId = statusId ?? string.Empty;
            TotalAmount = totalAmount;
        }
    }

    /// <summary>
    /// Immutable view of a hero item's cooldown state at a snapshot.
    /// </summary>
    public sealed class ItemCooldownView
    {
        public long InstanceId { get; }
        public string DefinitionId { get; }
        public TriggerKind TriggerKind { get; }
        public int EffectiveCooldown { get; }
        public int NextReadyAt { get; }
        public int ActionsRemaining { get; }
        public bool IsReady { get; }

        public ItemCooldownView(
            long instanceId,
            string definitionId,
            TriggerKind triggerKind,
            int effectiveCooldown,
            int nextReadyAt,
            int actionsRemaining,
            bool isReady)
        {
            InstanceId = instanceId;
            DefinitionId = definitionId ?? string.Empty;
            TriggerKind = triggerKind;
            EffectiveCooldown = effectiveCooldown;
            NextReadyAt = nextReadyAt;
            ActionsRemaining = actionsRemaining;
            IsReady = isReady;
        }
    }

    /// <summary>
    /// Immutable snapshot view of a hero unit.
    /// </summary>
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

        public HeroStateView(
            int slot,
            int instanceId,
            string definitionId,
            int hp,
            int maxHp,
            int shield,
            int gauge,
            int effectiveSpeed,
            int actionOrdinal,
            bool isAlive,
            IReadOnlyList<ActiveStatusView> statuses,
            IReadOnlyList<ItemCooldownView> itemCooldowns)
        {
            Slot = slot;
            InstanceId = instanceId;
            DefinitionId = definitionId ?? string.Empty;
            Hp = hp;
            MaxHp = maxHp;
            Shield = shield;
            Gauge = gauge;
            EffectiveSpeed = effectiveSpeed;
            ActionOrdinal = actionOrdinal;
            IsAlive = isAlive;
            Statuses = statuses ?? Array.Empty<ActiveStatusView>();
            ItemCooldowns = itemCooldowns ?? Array.Empty<ItemCooldownView>();
        }
    }

    /// <summary>
    /// Immutable snapshot view of an enemy unit.
    /// </summary>
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

        public EnemyStateView(
            int slot,
            int instanceId,
            string definitionId,
            int hp,
            int maxHp,
            int shield,
            int gauge,
            int effectiveSpeed,
            int actionOrdinal,
            int intentIndex,
            BoundIntentEntry currentIntent,
            bool isAlive,
            IReadOnlyList<ActiveStatusView> statuses)
        {
            Slot = slot;
            InstanceId = instanceId;
            DefinitionId = definitionId ?? string.Empty;
            Hp = hp;
            MaxHp = maxHp;
            Shield = shield;
            Gauge = gauge;
            EffectiveSpeed = effectiveSpeed;
            ActionOrdinal = actionOrdinal;
            IntentIndex = intentIndex;
            CurrentIntent = currentIntent;
            IsAlive = isAlive;
            Statuses = statuses ?? Array.Empty<ActiveStatusView>();
        }
    }

    /// <summary>
    /// Complete immutable snapshot of combat state at a specific event sequence number.
    /// </summary>
    public sealed class CombatStateSnapshot
    {
        public long EventSeq { get; }
        public int CompletedActions { get; }
        public IReadOnlyList<HeroStateView> Heroes { get; }
        public IReadOnlyList<EnemyStateView> Enemies { get; }
        public IReadOnlyList<TurnForecastEntry> Forecast { get; }

        public CombatStateSnapshot(
            long eventSeq,
            int completedActions,
            IReadOnlyList<HeroStateView> heroes,
            IReadOnlyList<EnemyStateView> enemies,
            IReadOnlyList<TurnForecastEntry> forecast)
        {
            EventSeq = eventSeq;
            CompletedActions = completedActions;
            Heroes = heroes ?? Array.Empty<HeroStateView>();
            Enemies = enemies ?? Array.Empty<EnemyStateView>();
            Forecast = forecast ?? Array.Empty<TurnForecastEntry>();
        }
    }

    /// <summary>
    /// Complete deterministic trace of an executed encounter.
    /// </summary>
    public sealed class CombatTrace
    {
        public int EncounterId { get; }
        public int RulesVersion { get; }
        public CombatStateSnapshot Baseline { get; }
        public IReadOnlyList<CombatEventRecord> Events { get; }
        public IReadOnlyList<CombatStateSnapshot> SnapshotsBySeq { get; }

        public CombatTrace(
            int encounterId,
            int rulesVersion,
            CombatStateSnapshot baseline,
            IReadOnlyList<CombatEventRecord> events,
            IReadOnlyList<CombatStateSnapshot> snapshotsBySeq)
        {
            EncounterId = encounterId;
            RulesVersion = rulesVersion;
            Baseline = baseline;
            Events = events ?? Array.Empty<CombatEventRecord>();
            SnapshotsBySeq = snapshotsBySeq ?? Array.Empty<CombatStateSnapshot>();
        }
    }

    /// <summary>
    /// Result of <see cref="CombatRunner.TryPrepare"/> with bound enemy intents and initial forecast.
    /// </summary>
    public sealed class PreparedEncounter
    {
        public EncounterSetup Setup { get; }
        public IReadOnlyList<HeroStateView> InitialHeroes { get; }
        public IReadOnlyList<BoundEnemyState> BoundEnemies { get; }
        public IReadOnlyList<TurnForecastEntry> InitialForecast { get; }

        public PreparedEncounter(
            EncounterSetup setup,
            IReadOnlyList<HeroStateView> initialHeroes,
            IReadOnlyList<BoundEnemyState> boundEnemies,
            IReadOnlyList<TurnForecastEntry> initialForecast)
        {
            Setup = setup;
            InitialHeroes = initialHeroes ?? Array.Empty<HeroStateView>();
            BoundEnemies = boundEnemies ?? Array.Empty<BoundEnemyState>();
            InitialForecast = initialForecast ?? Array.Empty<TurnForecastEntry>();
        }
    }

    /// <summary>
    /// Final result returned by <see cref="CombatRunner.RunEncounter"/>.
    /// </summary>
    public sealed class CombatResult
    {
        public CombatRunStatus Status { get; }
        public CombatOutcome? Outcome { get; }
        public string StopDiagnostic { get; }
        public CombatTrace Trace { get; }

        public CombatResult(
            CombatRunStatus status,
            CombatOutcome? outcome,
            string stopDiagnostic,
            CombatTrace trace)
        {
            Status = status;
            Outcome = outcome;
            StopDiagnostic = stopDiagnostic ?? string.Empty;
            Trace = trace;
        }
    }
}
