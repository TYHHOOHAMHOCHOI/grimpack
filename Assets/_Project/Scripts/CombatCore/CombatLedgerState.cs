using System;
using System.Collections.Generic;

namespace Project05.CombatCore
{
    internal sealed class MutableStatusLot
    {
        public string StatusId;
        public long OriginalAmount;
        public long RemainingAmount;
        public long ConsumedAmount;
        public long ExpiredAmount;
        public bool IsBaseline;
        public long SourceRefId;
        public int ExpiryActionOrdinal;
    }

    internal sealed class MutableHeroItem
    {
        public long InstanceId;
        public string DefinitionId;
        public bool IsInnate;
        public GridCell AnchorCell;
        public TriggerDef Trigger;
        public IReadOnlyList<EffectDef> Effects;
        public int EffectiveCooldown;
        public int NextReadyAt;
        public IReadOnlyList<AppliedModifierRef> AppliedModifiers;
        public int DamageFlatBonus;
        public int DamagePercentPermille;
        public Dictionary<string, int> StatusAmountBonuses;

        public int GetStatusBonus(string statusId)
        {
            if (StatusAmountBonuses != null &&
                !string.IsNullOrEmpty(statusId) &&
                StatusAmountBonuses.TryGetValue(statusId, out int bonus))
            {
                return bonus;
            }
            return 0;
        }
    }

    internal sealed class MutableUnit
    {
        public UnitRef Ref;
        public string DefinitionId;
        public int Hp;
        public int MaxHp;
        public int Shield;
        public int Gauge;
        public int EffectiveSpeed;
        public int ActionOrdinal;

        public List<MutableHeroItem> Items;
        public int IntentIndex;
        public IReadOnlyList<BoundIntentEntry> IntentSequence;
        public List<MutableStatusLot> StatusLots = new List<MutableStatusLot>();

        public bool IsAlive => Hp > 0;

        public long GetStatusTotal(string statusId)
        {
            long sum = 0;
            for (int i = 0; i < StatusLots.Count; i++)
            {
                MutableStatusLot lot = StatusLots[i];
                if (lot.RemainingAmount > 0 && string.Equals(lot.StatusId, statusId, StringComparison.Ordinal))
                {
                    sum += lot.RemainingAmount;
                }
            }
            return sum;
        }

        public bool HasStatus(string statusId) => GetStatusTotal(statusId) > 0;

        public IReadOnlyList<ActiveStatusView> BuildStatusViews()
        {
            if (StatusLots.Count == 0)
            {
                return Array.Empty<ActiveStatusView>();
            }

            var ids = new List<string>();
            for (int i = 0; i < StatusLots.Count; i++)
            {
                MutableStatusLot lot = StatusLots[i];
                if (lot.RemainingAmount <= 0)
                {
                    continue;
                }
                bool seen = false;
                for (int j = 0; j < ids.Count; j++)
                {
                    if (string.Equals(ids[j], lot.StatusId, StringComparison.Ordinal))
                    {
                        seen = true;
                        break;
                    }
                }
                if (!seen)
                {
                    ids.Add(lot.StatusId);
                }
            }

            if (ids.Count == 0)
            {
                return Array.Empty<ActiveStatusView>();
            }

            ids.Sort(StringComparer.Ordinal);
            var views = new ActiveStatusView[ids.Count];
            for (int i = 0; i < ids.Count; i++)
            {
                views[i] = new ActiveStatusView(ids[i], GetStatusTotal(ids[i]));
            }
            return views;
        }
    }

    /// <summary>
    /// Mutable simulation ledger with atomic step commits, causal event log, and per-seq snapshots.
    /// </summary>
    internal sealed class CombatLedgerState
    {
        public const int RulesVersion = 1;
        public const int TraceRecordCeiling = 20000;
        public const int DefaultStatusDurationActions = 2;
        public const int DefaultDetonatePerUnit = 2;
        public const string AnStatusId = "an";
        public const string StunStatusId = "stun";

        private readonly int _encounterId;
        private readonly List<MutableUnit> _heroes = new List<MutableUnit>();
        private readonly List<MutableUnit> _enemies = new List<MutableUnit>();
        private readonly List<SchedulerUnitState> _schedulerUnits = new List<SchedulerUnitState>();
        private readonly List<CombatEventRecord> _events = new List<CombatEventRecord>();
        private readonly List<CombatStateSnapshot> _snapshots = new List<CombatStateSnapshot>();

        private long _nextSeq = 1L;
        private long _nextOccurrenceId = 1L;
        private int _completedActions;
        private CombatStateSnapshot _baseline;

        public IReadOnlyList<MutableUnit> Heroes => _heroes;
        public IReadOnlyList<MutableUnit> Enemies => _enemies;
        public IReadOnlyList<CombatEventRecord> Events => _events;
        public int CompletedActions => _completedActions;
        public bool CeilingReached { get; private set; }
        public string CeilingDiagnostic { get; private set; } = string.Empty;

        public CombatLedgerState(
            GameCatalog catalog,
            EncounterSetup setup,
            IReadOnlyList<BoundEnemyState> boundEnemies)
        {
            _encounterId = setup.EncounterId;

            var sortedHeroes = new List<HeroBaselineInput>(setup.Heroes);
            sortedHeroes.Sort((a, b) => a.Slot.CompareTo(b.Slot));

            for (int i = 0; i < sortedHeroes.Count; i++)
            {
                HeroBaselineInput input = sortedHeroes[i];
                HeroDef def = input.Definition;
                ArrangementGridView grid = input.FrozenGrid;
                GridSynergyView synergy = input.FrozenSynergy;
                if (synergy == null && grid != null)
                {
                    synergy = AdjacencySynergyEvaluator.Evaluate(grid, catalog);
                }

                int hpBonus = input.PermanentHpBonus + TurnOrderScheduler.ComputeHeroHpBonus(grid, catalog);
                int maxHp = Math.Max(1, def.BaseMaxHp + hpBonus);
                int speedBonus = TurnOrderScheduler.ComputeHeroSpeedBonus(grid, catalog);
                int effSpeed = TurnOrderScheduler.EffectiveSpeed(def.BaseSpeed, speedBonus, 0);

                var heroUnit = new MutableUnit
                {
                    Ref = new UnitRef(UnitSide.Hero, input.Slot, input.Slot + 1),
                    DefinitionId = def.Id,
                    Hp = maxHp,
                    MaxHp = maxHp,
                    Shield = Math.Max(0, def.InitialShield),
                    Gauge = 0,
                    EffectiveSpeed = effSpeed,
                    ActionOrdinal = 0,
                    Items = BuildHeroItems(input.Slot, def, grid, synergy, catalog)
                };

                _heroes.Add(heroUnit);
                _schedulerUnits.Add(new SchedulerUnitState
                {
                    Unit = heroUnit.Ref,
                    Gauge = 0,
                    EffectiveSpeed = effSpeed,
                    ActionOrdinal = 0,
                    IntentIndex = 0,
                    IntentSequence = Array.Empty<BoundIntentEntry>(),
                    IsAlive = heroUnit.IsAlive
                });
            }

            var sortedBoundEnemies = new List<BoundEnemyState>(boundEnemies);
            sortedBoundEnemies.Sort((a, b) => a.Slot.CompareTo(b.Slot));

            for (int i = 0; i < sortedBoundEnemies.Count; i++)
            {
                BoundEnemyState bound = sortedBoundEnemies[i];
                var enemyUnit = new MutableUnit
                {
                    Ref = new UnitRef(UnitSide.Enemy, bound.Slot, bound.EnemyInstanceId),
                    DefinitionId = bound.DefinitionId,
                    Hp = bound.ScaledMaxHp,
                    MaxHp = bound.ScaledMaxHp,
                    Shield = 0,
                    Gauge = 0,
                    EffectiveSpeed = bound.EffectiveSpeed,
                    ActionOrdinal = 0,
                    IntentIndex = 0,
                    IntentSequence = bound.Sequence
                };

                _enemies.Add(enemyUnit);
                _schedulerUnits.Add(new SchedulerUnitState
                {
                    Unit = enemyUnit.Ref,
                    Gauge = 0,
                    EffectiveSpeed = bound.EffectiveSpeed,
                    ActionOrdinal = 0,
                    IntentIndex = 0,
                    IntentSequence = bound.Sequence,
                    IsAlive = enemyUnit.IsAlive
                });
            }

            _baseline = CaptureSnapshot(0L);
            _snapshots.Add(_baseline);
        }

        private static List<MutableHeroItem> BuildHeroItems(
            int heroSlot,
            HeroDef heroDef,
            ArrangementGridView grid,
            GridSynergyView synergy,
            GameCatalog catalog)
        {
            var items = new List<MutableHeroItem>();

            if (heroDef.InnateItem != null)
            {
                TriggerDef t = heroDef.InnateItem.Trigger;
                int effCd = (t.Kind == TriggerKind.Cooldown || t.Kind == TriggerKind.Reaction)
                    ? ClampCooldown(t.N)
                    : 0;
                int nextReady = t.Kind == TriggerKind.Cooldown ? 1 + t.S : 0;

                items.Add(new MutableHeroItem
                {
                    InstanceId = -1L - heroSlot,
                    DefinitionId = heroDef.InnateItem.Id,
                    IsInnate = true,
                    AnchorCell = new GridCell(0, -1),
                    Trigger = t,
                    Effects = heroDef.InnateItem.Effects,
                    EffectiveCooldown = effCd,
                    NextReadyAt = nextReady,
                    AppliedModifiers = Array.Empty<AppliedModifierRef>(),
                    DamageFlatBonus = 0,
                    DamagePercentPermille = 0,
                    StatusAmountBonuses = null
                });
            }

            if (grid != null && grid.Items != null)
            {
                var placed = new List<PlacedItemView>(grid.Items);
                placed.Sort((a, b) =>
                {
                    int cy = a.AnchorCell.Y.CompareTo(b.AnchorCell.Y);
                    return cy != 0 ? cy : a.AnchorCell.X.CompareTo(b.AnchorCell.X);
                });

                for (int i = 0; i < placed.Count; i++)
                {
                    PlacedItemView p = placed[i];
                    ItemDef def = catalog.FindItem(p.DefinitionId);
                    if (def == null)
                    {
                        continue;
                    }

                    ItemSynergySummary summary = synergy != null ? synergy.FindForItem(p.InstanceId) : null;

                    int cdRed = summary != null ? summary.CooldownReductionTotal : 0;
                    int dmgFlat = summary != null ? summary.DamageFlatTotal : 0;
                    int dmgPct = summary != null ? summary.DamagePercentTotal : 0;
                    Dictionary<string, int> statusBonuses = null;
                    var appliedRefs = new List<AppliedModifierRef>();

                    if (summary != null && summary.IncomingLinks != null)
                    {
                        for (int l = 0; l < summary.IncomingLinks.Count; l++)
                        {
                            SynergyLink link = summary.IncomingLinks[l];
                            appliedRefs.Add(new AppliedModifierRef(
                                link.SourceInstanceId,
                                link.SourceDefId,
                                link.RuleIndex,
                                link.Kind,
                                link.Amount));

                            if (link.Kind == SynergyKind.StatusAmount && !string.IsNullOrEmpty(link.StatusId))
                            {
                                if (statusBonuses == null)
                                {
                                    statusBonuses = new Dictionary<string, int>(StringComparer.Ordinal);
                                }
                                statusBonuses.TryGetValue(link.StatusId, out int cur);
                                statusBonuses[link.StatusId] = cur + link.Amount;
                            }
                        }
                    }

                    TriggerDef t = def.Trigger;
                    int effCd = (t.Kind == TriggerKind.Cooldown || t.Kind == TriggerKind.Reaction)
                        ? ClampCooldown(t.N - cdRed)
                        : 0;
                    int nextReady = t.Kind == TriggerKind.Cooldown ? 1 + t.S : 0;

                    items.Add(new MutableHeroItem
                    {
                        InstanceId = p.InstanceId,
                        DefinitionId = def.Id,
                        IsInnate = false,
                        AnchorCell = p.AnchorCell,
                        Trigger = t,
                        Effects = def.Effects,
                        EffectiveCooldown = effCd,
                        NextReadyAt = nextReady,
                        AppliedModifiers = appliedRefs.ToArray(),
                        DamageFlatBonus = dmgFlat,
                        DamagePercentPermille = dmgPct,
                        StatusAmountBonuses = statusBonuses
                    });
                }
            }

            return items;
        }

        private static int ClampCooldown(int raw)
        {
            if (raw < 1) return 1;
            if (raw > 99) return 99;
            return raw;
        }

        public long AllocateOccurrenceId() => _nextOccurrenceId++;

        public int CountAliveHeroes()
        {
            int count = 0;
            for (int i = 0; i < _heroes.Count; i++)
            {
                if (_heroes[i].IsAlive) count++;
            }
            return count;
        }

        public int CountAliveEnemies()
        {
            int count = 0;
            for (int i = 0; i < _enemies.Count; i++)
            {
                if (_enemies[i].IsAlive) count++;
            }
            return count;
        }

        public MutableUnit FindUnit(UnitRef unitRef)
        {
            if (!unitRef.IsValid) return null;

            List<MutableUnit> list = unitRef.Side == UnitSide.Hero ? _heroes : _enemies;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Ref == unitRef)
                {
                    return list[i];
                }
            }
            return null;
        }

        public MutableUnit FindHeroByInstanceId(int heroInstanceId)
        {
            for (int i = 0; i < _heroes.Count; i++)
            {
                if (_heroes[i].Ref.InstanceId == heroInstanceId)
                {
                    return _heroes[i];
                }
            }
            return null;
        }

        public MutableUnit FindFirstAliveEnemy()
        {
            for (int i = 0; i < _enemies.Count; i++)
            {
                if (_enemies[i].IsAlive) return _enemies[i];
            }
            return null;
        }

        public MutableUnit FindLowestHpAliveHero()
        {
            MutableUnit best = null;
            for (int i = 0; i < _heroes.Count; i++)
            {
                MutableUnit h = _heroes[i];
                if (!h.IsAlive) continue;
                if (best == null || h.Hp < best.Hp || (h.Hp == best.Hp && h.Ref.Slot < best.Ref.Slot))
                {
                    best = h;
                }
            }
            return best;
        }

        public bool TryEvaluateOutcome(bool actionLimitReached, out CombatOutcome outcome, out string reasonCode)
        {
            int aliveHeroes = CountAliveHeroes();
            int aliveEnemies = CountAliveEnemies();

            if (aliveHeroes == 0 && aliveEnemies == 0)
            {
                outcome = CombatOutcome.Draw;
                reasonCode = "mutual_elimination";
                return true;
            }
            if (aliveHeroes > 0 && aliveEnemies == 0)
            {
                outcome = CombatOutcome.Victory;
                reasonCode = "all_enemies_downed";
                return true;
            }
            if (aliveHeroes == 0 && aliveEnemies > 0)
            {
                outcome = CombatOutcome.Defeat;
                reasonCode = "all_heroes_downed";
                return true;
            }
            if (actionLimitReached)
            {
                outcome = CombatOutcome.Defeat;
                reasonCode = "action_limit";
                return true;
            }

            outcome = default(CombatOutcome);
            reasonCode = string.Empty;
            return false;
        }

        public void SyncSchedulerStates()
        {
            int idx = 0;
            for (int i = 0; i < _heroes.Count; i++, idx++)
            {
                MutableUnit h = _heroes[i];
                SchedulerUnitState s = _schedulerUnits[idx];
                s.Gauge = h.Gauge;
                s.ActionOrdinal = h.ActionOrdinal;
                s.IsAlive = h.IsAlive;
            }
            for (int i = 0; i < _enemies.Count; i++, idx++)
            {
                MutableUnit e = _enemies[i];
                SchedulerUnitState s = _schedulerUnits[idx];
                s.Gauge = e.Gauge;
                s.ActionOrdinal = e.ActionOrdinal;
                s.IntentIndex = e.IntentIndex;
                s.IsAlive = e.IsAlive;
            }
        }

        public bool TryAdvanceToNextActor(
            out MutableUnit actor,
            out int deltaTime,
            out int gaugeBefore,
            out int gaugeAfter)
        {
            SyncSchedulerStates();
            if (!TurnOrderScheduler.TryAdvanceNextActor(
                _schedulerUnits,
                out int winnerIndex,
                out deltaTime,
                out gaugeBefore,
                out gaugeAfter))
            {
                actor = null;
                return false;
            }

            // Copy updated gauges back to live MutableUnits atomically when caller emits ActionStarted/ActionSkipped.
            int idx = 0;
            for (int i = 0; i < _heroes.Count; i++, idx++)
            {
                _heroes[i].Gauge = _schedulerUnits[idx].Gauge;
            }
            for (int i = 0; i < _enemies.Count; i++, idx++)
            {
                _enemies[i].Gauge = _schedulerUnits[idx].Gauge;
            }

            UnitRef winnerRef = _schedulerUnits[winnerIndex].Unit;
            actor = FindUnit(winnerRef);
            return actor != null;
        }

        public void IncrementCompletedActions()
        {
            _completedActions += 1;
        }

        public bool RecordIntentsBound()
        {
            for (int i = 0; i < _enemies.Count; i++)
            {
                MutableUnit e = _enemies[i];
                if (!AppendEvent(new CombatEventRecord(
                    seq: _nextSeq,
                    kind: CombatEventKind.IntentsBound,
                    rootActionId: 0L,
                    depth: 0,
                    parentEffectSeq: null,
                    effectOrdinal: 0L,
                    origin: new CombatOrigin(
                        OriginKind.EncounterSetup,
                        -1,
                        0L,
                        string.Empty,
                        new GridCell(0, 0),
                        e.Ref.InstanceId,
                        e.DefinitionId,
                        e.IntentSequence.Count > 0 ? e.IntentSequence[0].MoveId : string.Empty,
                        0L,
                        Array.Empty<AppliedModifierRef>()),
                    actor: e.Ref,
                    target: UnitRef.None,
                    triggerKind: null,
                    actionOrdinal: 0,
                    nextReadyAt: 0,
                    deltaTime: 0,
                    gaugeBefore: 0,
                    gaugeAfter: 0,
                    statusId: string.Empty,
                    statusAmount: 0L,
                    expiryActionOrdinal: 0,
                    occurrenceId: 0L,
                    consumedSources: null,
                    resolvedAmount: e.IntentSequence.Count,
                    shieldBefore: 0,
                    shieldAfter: 0,
                    hpBefore: e.Hp,
                    hpAfter: e.Hp,
                    reasonCode: string.Empty,
                    outcome: null)))
                {
                    return false;
                }
            }
            return true;
        }

        public bool RecordActionStarted(
            MutableUnit actor,
            long rootActionId,
            int deltaTime,
            int gaugeBefore,
            int gaugeAfter,
            CombatOrigin origin,
            bool incrementActionOrdinal = true,
            string reasonCode = "")
        {
            if (incrementActionOrdinal)
            {
                actor.ActionOrdinal += 1;
            }
            return AppendEvent(new CombatEventRecord(
                seq: _nextSeq,
                kind: CombatEventKind.ActionStarted,
                rootActionId: rootActionId,
                depth: 0,
                parentEffectSeq: null,
                effectOrdinal: 0L,
                origin: origin,
                actor: actor.Ref,
                target: UnitRef.None,
                triggerKind: null,
                actionOrdinal: actor.ActionOrdinal,
                nextReadyAt: 0,
                deltaTime: deltaTime,
                gaugeBefore: gaugeBefore,
                gaugeAfter: gaugeAfter,
                statusId: string.Empty,
                statusAmount: 0L,
                expiryActionOrdinal: 0,
                occurrenceId: 0L,
                consumedSources: null,
                resolvedAmount: 0,
                shieldBefore: actor.Shield,
                shieldAfter: actor.Shield,
                hpBefore: actor.Hp,
                hpAfter: actor.Hp,
                reasonCode: reasonCode ?? string.Empty,
                outcome: null));
        }

        public bool RecordActionSkipped(
            MutableUnit actor,
            long rootActionId,
            int deltaTime,
            int gaugeBefore,
            int gaugeAfter,
            string reasonCode)
        {
            actor.ActionOrdinal += 1;
            return AppendEvent(new CombatEventRecord(
                seq: _nextSeq,
                kind: CombatEventKind.ActionSkipped,
                rootActionId: rootActionId,
                depth: 0,
                parentEffectSeq: null,
                effectOrdinal: 0L,
                origin: CombatOrigin.System,
                actor: actor.Ref,
                target: UnitRef.None,
                triggerKind: null,
                actionOrdinal: actor.ActionOrdinal,
                nextReadyAt: 0,
                deltaTime: deltaTime,
                gaugeBefore: gaugeBefore,
                gaugeAfter: gaugeAfter,
                statusId: string.Empty,
                statusAmount: 0L,
                expiryActionOrdinal: 0,
                occurrenceId: 0L,
                consumedSources: null,
                resolvedAmount: 0,
                shieldBefore: actor.Shield,
                shieldAfter: actor.Shield,
                hpBefore: actor.Hp,
                hpAfter: actor.Hp,
                reasonCode: reasonCode,
                outcome: null));
        }

        public bool RecordItemFired(
            MutableUnit actor,
            MutableHeroItem item,
            int actionOrdinal,
            int nextReadyAtAfterFire,
            long rootActionId,
            int depth,
            long? parentEffectSeq,
            long effectOrdinal,
            out CombatEventRecord emitted)
        {
            item.NextReadyAt = nextReadyAtAfterFire;
            long occId = AllocateOccurrenceId();
            var origin = new CombatOrigin(
                OriginKind.Item,
                actor.Ref.Slot,
                item.InstanceId,
                item.DefinitionId,
                item.AnchorCell,
                -1,
                string.Empty,
                string.Empty,
                occId,
                item.AppliedModifiers);

            emitted = new CombatEventRecord(
                seq: _nextSeq,
                kind: CombatEventKind.ItemFired,
                rootActionId: rootActionId,
                depth: depth,
                parentEffectSeq: parentEffectSeq,
                effectOrdinal: effectOrdinal,
                origin: origin,
                actor: actor.Ref,
                target: UnitRef.None,
                triggerKind: item.Trigger.Kind,
                actionOrdinal: actionOrdinal,
                nextReadyAt: item.NextReadyAt,
                deltaTime: 0,
                gaugeBefore: actor.Gauge,
                gaugeAfter: actor.Gauge,
                statusId: string.Empty,
                statusAmount: 0L,
                expiryActionOrdinal: 0,
                occurrenceId: occId,
                consumedSources: null,
                resolvedAmount: 0,
                shieldBefore: actor.Shield,
                shieldAfter: actor.Shield,
                hpBefore: actor.Hp,
                hpAfter: actor.Hp,
                reasonCode: string.Empty,
                outcome: null);

            return AppendEvent(emitted);
        }

        public bool ApplyStatus(
            UnitRef actorRef,
            MutableUnit target,
            string statusId,
            int amount,
            CombatOrigin origin,
            long rootActionId,
            int depth,
            long? parentEffectSeq,
            long effectOrdinal,
            out CombatEventRecord emitted)
        {
            emitted = null;
            if (target == null || !target.IsAlive)
            {
                return RecordEffectSkipped(actorRef, target != null ? target.Ref : UnitRef.None, origin, rootActionId, depth, parentEffectSeq, effectOrdinal, "target_dead");
            }
            if (amount <= 0 || string.IsNullOrEmpty(statusId))
            {
                return RecordEffectSkipped(actorRef, target.Ref, origin, rootActionId, depth, parentEffectSeq, effectOrdinal, "zero_amount");
            }

            long seq = _nextSeq;
            int expiryActionOrdinal = target.ActionOrdinal + DefaultStatusDurationActions;
            long occId = AllocateOccurrenceId();

            target.StatusLots.Add(new MutableStatusLot
            {
                StatusId = statusId,
                OriginalAmount = amount,
                RemainingAmount = amount,
                ConsumedAmount = 0L,
                ExpiredAmount = 0L,
                IsBaseline = false,
                SourceRefId = seq,
                ExpiryActionOrdinal = expiryActionOrdinal
            });

            emitted = new CombatEventRecord(
                seq: seq,
                kind: CombatEventKind.StatusApplied,
                rootActionId: rootActionId,
                depth: depth,
                parentEffectSeq: parentEffectSeq,
                effectOrdinal: effectOrdinal,
                origin: origin,
                actor: actorRef,
                target: target.Ref,
                triggerKind: null,
                actionOrdinal: target.ActionOrdinal,
                nextReadyAt: 0,
                deltaTime: 0,
                gaugeBefore: target.Gauge,
                gaugeAfter: target.Gauge,
                statusId: statusId,
                statusAmount: amount,
                expiryActionOrdinal: expiryActionOrdinal,
                occurrenceId: occId,
                consumedSources: null,
                resolvedAmount: amount,
                shieldBefore: target.Shield,
                shieldAfter: target.Shield,
                hpBefore: target.Hp,
                hpAfter: target.Hp,
                reasonCode: string.Empty,
                outcome: null);

            return AppendEvent(emitted);
        }

        public bool ApplyDamage(
            UnitRef actorRef,
            MutableUnit target,
            int resolvedDamage,
            CombatOrigin origin,
            long rootActionId,
            int depth,
            long? parentEffectSeq,
            long effectOrdinal,
            bool isDetonation,
            out CombatEventRecord damageEvent,
            out CombatEventRecord downedEvent)
        {
            damageEvent = null;
            downedEvent = null;

            if (target == null || !target.IsAlive)
            {
                return RecordEffectSkipped(actorRef, target != null ? target.Ref : UnitRef.None, origin, rootActionId, depth, parentEffectSeq, effectOrdinal, "target_dead");
            }

            int clamped = resolvedDamage < 0 ? 0 : resolvedDamage;
            if (clamped == 0)
            {
                return true;
            }
            int shieldBefore = target.Shield;
            int hpBefore = target.Hp;

            int shieldAbsorbed = Math.Min(shieldBefore, clamped);
            int remaining = clamped - shieldAbsorbed;
            int hpLoss = Math.Min(hpBefore, remaining);

            target.Shield = shieldBefore - shieldAbsorbed;
            target.Hp = hpBefore - hpLoss;

            damageEvent = new CombatEventRecord(
                seq: _nextSeq,
                kind: isDetonation ? CombatEventKind.DetonationDamage : CombatEventKind.Damage,
                rootActionId: rootActionId,
                depth: depth,
                parentEffectSeq: parentEffectSeq,
                effectOrdinal: effectOrdinal,
                origin: origin,
                actor: actorRef,
                target: target.Ref,
                triggerKind: null,
                actionOrdinal: 0,
                nextReadyAt: 0,
                deltaTime: 0,
                gaugeBefore: target.Gauge,
                gaugeAfter: target.Gauge,
                statusId: string.Empty,
                statusAmount: 0L,
                expiryActionOrdinal: 0,
                occurrenceId: AllocateOccurrenceId(),
                consumedSources: null,
                resolvedAmount: clamped,
                shieldBefore: shieldBefore,
                shieldAfter: target.Shield,
                hpBefore: hpBefore,
                hpAfter: target.Hp,
                reasonCode: string.Empty,
                outcome: null);

            if (!AppendEvent(damageEvent))
            {
                return false;
            }

            if (hpBefore > 0 && target.Hp == 0)
            {
                downedEvent = new CombatEventRecord(
                    seq: _nextSeq,
                    kind: CombatEventKind.UnitDowned,
                    rootActionId: rootActionId,
                    depth: depth,
                    parentEffectSeq: damageEvent.Seq,
                    effectOrdinal: effectOrdinal,
                    origin: origin,
                    actor: actorRef,
                    target: target.Ref,
                    triggerKind: null,
                    actionOrdinal: target.ActionOrdinal,
                    nextReadyAt: 0,
                    deltaTime: 0,
                    gaugeBefore: target.Gauge,
                    gaugeAfter: target.Gauge,
                    statusId: string.Empty,
                    statusAmount: 0L,
                    expiryActionOrdinal: 0,
                    occurrenceId: 0L,
                    consumedSources: null,
                    resolvedAmount: 0,
                    shieldBefore: target.Shield,
                    shieldAfter: target.Shield,
                    hpBefore: hpBefore,
                    hpAfter: 0,
                    reasonCode: isDetonation ? "detonation_lethal" : "damage_lethal",
                    outcome: null);

                if (!AppendEvent(downedEvent))
                {
                    return false;
                }
            }

            return true;
        }

        public bool ConsumeAndDetonate(
            UnitRef actorRef,
            MutableUnit target,
            string statusId,
            int requestedUnits,
            int perUnit,
            int damageFlatBonus,
            int damagePercentPermille,
            CombatOrigin origin,
            long rootActionId,
            int depth,
            long? parentEffectSeq,
            long effectOrdinal,
            out CombatEventRecord consumedEvent,
            out CombatEventRecord detonationDamageEvent,
            out CombatEventRecord downedEvent)
        {
            consumedEvent = null;
            detonationDamageEvent = null;
            downedEvent = null;

            if (target == null || !target.IsAlive)
            {
                return RecordEffectSkipped(actorRef, target != null ? target.Ref : UnitRef.None, origin, rootActionId, depth, parentEffectSeq, effectOrdinal, "target_dead");
            }

            if (requestedUnits <= 0 || string.IsNullOrEmpty(statusId))
            {
                return RecordDetonationEmpty(actorRef, target, statusId, origin, rootActionId, depth, parentEffectSeq, effectOrdinal);
            }

            long remainingToConsume = requestedUnits;
            long totalConsumed = 0;
            var sources = new List<StatusSourceContribution>();

            for (int i = 0; i < target.StatusLots.Count && remainingToConsume > 0; i++)
            {
                MutableStatusLot lot = target.StatusLots[i];
                if (lot.RemainingAmount <= 0 || !string.Equals(lot.StatusId, statusId, StringComparison.Ordinal))
                {
                    continue;
                }

                long take = lot.RemainingAmount < remainingToConsume ? lot.RemainingAmount : remainingToConsume;
                lot.RemainingAmount -= take;
                lot.ConsumedAmount += take;
                remainingToConsume -= take;
                totalConsumed += take;

                int takeInt = take > int.MaxValue ? int.MaxValue : (int)take;
                sources.Add(new StatusSourceContribution(lot.IsBaseline, lot.SourceRefId, takeInt));
            }

            if (totalConsumed == 0)
            {
                return RecordDetonationEmpty(actorRef, target, statusId, origin, rootActionId, depth, parentEffectSeq, effectOrdinal);
            }

            consumedEvent = new CombatEventRecord(
                seq: _nextSeq,
                kind: CombatEventKind.StatusConsumed,
                rootActionId: rootActionId,
                depth: depth,
                parentEffectSeq: parentEffectSeq,
                effectOrdinal: effectOrdinal,
                origin: origin,
                actor: actorRef,
                target: target.Ref,
                triggerKind: null,
                actionOrdinal: target.ActionOrdinal,
                nextReadyAt: 0,
                deltaTime: 0,
                gaugeBefore: target.Gauge,
                gaugeAfter: target.Gauge,
                statusId: statusId,
                statusAmount: totalConsumed,
                expiryActionOrdinal: 0,
                occurrenceId: AllocateOccurrenceId(),
                consumedSources: sources.ToArray(),
                resolvedAmount: totalConsumed > int.MaxValue ? int.MaxValue : (int)totalConsumed,
                shieldBefore: target.Shield,
                shieldAfter: target.Shield,
                hpBefore: target.Hp,
                hpAfter: target.Hp,
                reasonCode: string.Empty,
                outcome: null);

            if (!AppendEvent(consumedEvent))
            {
                return false;
            }

            int effectivePerUnit = perUnit > 0 ? perUnit : DefaultDetonatePerUnit;
            long baseDetonation = totalConsumed * effectivePerUnit;
            int resolvedDamage = ComputeResolvedDamage(baseDetonation, damageFlatBonus, damagePercentPermille);

            return ApplyDamage(
                actorRef,
                target,
                resolvedDamage,
                origin,
                rootActionId,
                depth,
                consumedEvent.Seq,
                effectOrdinal,
                isDetonation: true,
                out detonationDamageEvent,
                out downedEvent);
        }

        public bool ApplyShieldGain(
            UnitRef actorRef,
            MutableUnit target,
            int amount,
            CombatOrigin origin,
            long rootActionId,
            int depth,
            long? parentEffectSeq,
            long effectOrdinal)
        {
            if (target == null || !target.IsAlive)
            {
                return RecordEffectSkipped(actorRef, target != null ? target.Ref : UnitRef.None, origin, rootActionId, depth, parentEffectSeq, effectOrdinal, "target_dead");
            }
            if (amount <= 0)
            {
                return true;
            }

            int shieldBefore = target.Shield;
            long sum = (long)shieldBefore + amount;
            target.Shield = sum > int.MaxValue ? int.MaxValue : (int)sum;

            return AppendEvent(new CombatEventRecord(
                seq: _nextSeq,
                kind: CombatEventKind.ShieldGained,
                rootActionId: rootActionId,
                depth: depth,
                parentEffectSeq: parentEffectSeq,
                effectOrdinal: effectOrdinal,
                origin: origin,
                actor: actorRef,
                target: target.Ref,
                triggerKind: null,
                actionOrdinal: 0,
                nextReadyAt: 0,
                deltaTime: 0,
                gaugeBefore: target.Gauge,
                gaugeAfter: target.Gauge,
                statusId: string.Empty,
                statusAmount: 0L,
                expiryActionOrdinal: 0,
                occurrenceId: AllocateOccurrenceId(),
                consumedSources: null,
                resolvedAmount: target.Shield - shieldBefore,
                shieldBefore: shieldBefore,
                shieldAfter: target.Shield,
                hpBefore: target.Hp,
                hpAfter: target.Hp,
                reasonCode: string.Empty,
                outcome: null));
        }

        public bool ApplyHeal(
            UnitRef actorRef,
            MutableUnit target,
            int amount,
            CombatOrigin origin,
            long rootActionId,
            int depth,
            long? parentEffectSeq,
            long effectOrdinal)
        {
            if (target == null || !target.IsAlive)
            {
                return RecordEffectSkipped(actorRef, target != null ? target.Ref : UnitRef.None, origin, rootActionId, depth, parentEffectSeq, effectOrdinal, "target_dead");
            }
            if (amount <= 0)
            {
                return true;
            }

            int hpBefore = target.Hp;
            int missing = Math.Max(0, target.MaxHp - hpBefore);
            int healed = Math.Min(missing, amount);
            if (healed <= 0)
            {
                return true;
            }
            target.Hp = hpBefore + healed;

            return AppendEvent(new CombatEventRecord(
                seq: _nextSeq,
                kind: CombatEventKind.Healed,
                rootActionId: rootActionId,
                depth: depth,
                parentEffectSeq: parentEffectSeq,
                effectOrdinal: effectOrdinal,
                origin: origin,
                actor: actorRef,
                target: target.Ref,
                triggerKind: null,
                actionOrdinal: 0,
                nextReadyAt: 0,
                deltaTime: 0,
                gaugeBefore: target.Gauge,
                gaugeAfter: target.Gauge,
                statusId: string.Empty,
                statusAmount: 0L,
                expiryActionOrdinal: 0,
                occurrenceId: AllocateOccurrenceId(),
                consumedSources: null,
                resolvedAmount: healed,
                shieldBefore: target.Shield,
                shieldAfter: target.Shield,
                hpBefore: hpBefore,
                hpAfter: target.Hp,
                reasonCode: string.Empty,
                outcome: null));
        }

        public bool ExpireDueStatuses(MutableUnit actor, long rootActionId)
        {
            if (actor == null || !actor.IsAlive || actor.StatusLots.Count == 0)
            {
                return true;
            }

            // Collect status IDs with due lots in canonical ordinal order.
            var dueStatusIds = new List<string>();
            for (int i = 0; i < actor.StatusLots.Count; i++)
            {
                MutableStatusLot lot = actor.StatusLots[i];
                if (lot.RemainingAmount > 0 && lot.ExpiryActionOrdinal <= actor.ActionOrdinal)
                {
                    bool seen = false;
                    for (int j = 0; j < dueStatusIds.Count; j++)
                    {
                        if (string.Equals(dueStatusIds[j], lot.StatusId, StringComparison.Ordinal))
                        {
                            seen = true;
                            break;
                        }
                    }
                    if (!seen)
                    {
                        dueStatusIds.Add(lot.StatusId);
                    }
                }
            }

            if (dueStatusIds.Count == 0)
            {
                return true;
            }

            dueStatusIds.Sort(StringComparer.Ordinal);

            var expiryOrigin = new CombatOrigin(
                OriginKind.StatusExpiry,
                actor.Ref.Side == UnitSide.Hero ? actor.Ref.Slot : -1,
                0L,
                string.Empty,
                new GridCell(0, 0),
                actor.Ref.Side == UnitSide.Enemy ? actor.Ref.InstanceId : -1,
                actor.DefinitionId,
                string.Empty,
                0L,
                Array.Empty<AppliedModifierRef>());

            for (int s = 0; s < dueStatusIds.Count; s++)
            {
                string statusId = dueStatusIds[s];
                long expiredTotal = 0L;
                var sources = new List<StatusSourceContribution>();

                for (int i = 0; i < actor.StatusLots.Count; i++)
                {
                    MutableStatusLot lot = actor.StatusLots[i];
                    if (lot.RemainingAmount <= 0 ||
                        !string.Equals(lot.StatusId, statusId, StringComparison.Ordinal) ||
                        lot.ExpiryActionOrdinal > actor.ActionOrdinal)
                    {
                        continue;
                    }

                    long amt = lot.RemainingAmount;
                    lot.RemainingAmount = 0L;
                    lot.ExpiredAmount += amt;
                    expiredTotal += amt;
                    sources.Add(new StatusSourceContribution(
                        lot.IsBaseline,
                        lot.SourceRefId,
                        amt > int.MaxValue ? int.MaxValue : (int)amt));
                }

                if (expiredTotal > 0L)
                {
                    if (!AppendEvent(new CombatEventRecord(
                        seq: _nextSeq,
                        kind: CombatEventKind.StatusExpired,
                        rootActionId: rootActionId,
                        depth: 0,
                        parentEffectSeq: null,
                        effectOrdinal: 0L,
                        origin: expiryOrigin,
                        actor: actor.Ref,
                        target: actor.Ref,
                        triggerKind: null,
                        actionOrdinal: actor.ActionOrdinal,
                        nextReadyAt: 0,
                        deltaTime: 0,
                        gaugeBefore: actor.Gauge,
                        gaugeAfter: actor.Gauge,
                        statusId: statusId,
                        statusAmount: expiredTotal,
                        expiryActionOrdinal: actor.ActionOrdinal,
                        occurrenceId: 0L,
                        consumedSources: sources.ToArray(),
                        resolvedAmount: 0,
                        shieldBefore: actor.Shield,
                        shieldAfter: actor.Shield,
                        hpBefore: actor.Hp,
                        hpAfter: actor.Hp,
                        reasonCode: string.Empty,
                        outcome: null)))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public bool RecordChainLimit(
            UnitRef actorRef,
            CombatOrigin origin,
            long rootActionId,
            int depth,
            long? parentEffectSeq,
            long effectOrdinal,
            string reasonCode)
        {
            return AppendEvent(new CombatEventRecord(
                seq: _nextSeq,
                kind: CombatEventKind.ChainLimit,
                rootActionId: rootActionId,
                depth: depth,
                parentEffectSeq: parentEffectSeq,
                effectOrdinal: effectOrdinal,
                origin: origin,
                actor: actorRef,
                target: UnitRef.None,
                triggerKind: null,
                actionOrdinal: 0,
                nextReadyAt: 0,
                deltaTime: 0,
                gaugeBefore: 0,
                gaugeAfter: 0,
                statusId: string.Empty,
                statusAmount: 0L,
                expiryActionOrdinal: 0,
                occurrenceId: 0L,
                consumedSources: null,
                resolvedAmount: 0,
                shieldBefore: 0,
                shieldAfter: 0,
                hpBefore: 0,
                hpAfter: 0,
                reasonCode: reasonCode,
                outcome: null));
        }

        public bool RecordListenerSuppressed(
            UnitRef listenerOwner,
            MutableHeroItem item,
            long rootActionId,
            int depth,
            long? parentEffectSeq,
            long effectOrdinal,
            long triggerEventSeq)
        {
            var origin = new CombatOrigin(
                OriginKind.Item,
                listenerOwner.Slot,
                item.InstanceId,
                item.DefinitionId,
                item.AnchorCell,
                -1,
                string.Empty,
                string.Empty,
                0L,
                item.AppliedModifiers);

            return AppendEvent(new CombatEventRecord(
                seq: _nextSeq,
                kind: CombatEventKind.ListenerSuppressed,
                rootActionId: rootActionId,
                depth: depth,
                parentEffectSeq: parentEffectSeq,
                effectOrdinal: effectOrdinal,
                origin: origin,
                actor: listenerOwner,
                target: UnitRef.None,
                triggerKind: item.Trigger.Kind,
                actionOrdinal: 0,
                nextReadyAt: item.NextReadyAt,
                deltaTime: 0,
                gaugeBefore: 0,
                gaugeAfter: 0,
                statusId: string.Empty,
                statusAmount: 0L,
                expiryActionOrdinal: 0,
                occurrenceId: triggerEventSeq,
                consumedSources: null,
                resolvedAmount: 0,
                shieldBefore: 0,
                shieldAfter: 0,
                hpBefore: 0,
                hpAfter: 0,
                reasonCode: "once_per_root_action",
                outcome: null));
        }

        public bool RecordEffectSkipped(
            UnitRef actorRef,
            UnitRef targetRef,
            CombatOrigin origin,
            long rootActionId,
            int depth,
            long? parentEffectSeq,
            long effectOrdinal,
            string reasonCode)
        {
            return AppendEvent(new CombatEventRecord(
                seq: _nextSeq,
                kind: CombatEventKind.EffectSkipped,
                rootActionId: rootActionId,
                depth: depth,
                parentEffectSeq: parentEffectSeq,
                effectOrdinal: effectOrdinal,
                origin: origin,
                actor: actorRef,
                target: targetRef,
                triggerKind: null,
                actionOrdinal: 0,
                nextReadyAt: 0,
                deltaTime: 0,
                gaugeBefore: 0,
                gaugeAfter: 0,
                statusId: string.Empty,
                statusAmount: 0L,
                expiryActionOrdinal: 0,
                occurrenceId: 0L,
                consumedSources: null,
                resolvedAmount: 0,
                shieldBefore: 0,
                shieldAfter: 0,
                hpBefore: 0,
                hpAfter: 0,
                reasonCode: reasonCode,
                outcome: null));
        }

        private bool RecordDetonationEmpty(
            UnitRef actorRef,
            MutableUnit target,
            string statusId,
            CombatOrigin origin,
            long rootActionId,
            int depth,
            long? parentEffectSeq,
            long effectOrdinal)
        {
            return AppendEvent(new CombatEventRecord(
                seq: _nextSeq,
                kind: CombatEventKind.DetonationEmpty,
                rootActionId: rootActionId,
                depth: depth,
                parentEffectSeq: parentEffectSeq,
                effectOrdinal: effectOrdinal,
                origin: origin,
                actor: actorRef,
                target: target.Ref,
                triggerKind: null,
                actionOrdinal: 0,
                nextReadyAt: 0,
                deltaTime: 0,
                gaugeBefore: target.Gauge,
                gaugeAfter: target.Gauge,
                statusId: statusId,
                statusAmount: 0L,
                expiryActionOrdinal: 0,
                occurrenceId: 0L,
                consumedSources: null,
                resolvedAmount: 0,
                shieldBefore: target.Shield,
                shieldAfter: target.Shield,
                hpBefore: target.Hp,
                hpAfter: target.Hp,
                reasonCode: "no_status_available",
                outcome: null));
        }

        public bool RecordCombatEnded(
            long rootActionId,
            long? terminalCauseSeq,
            CombatOutcome outcome,
            string reasonCode)
        {
            return AppendEvent(new CombatEventRecord(
                seq: _nextSeq,
                kind: CombatEventKind.CombatEnded,
                rootActionId: rootActionId,
                depth: 0,
                parentEffectSeq: terminalCauseSeq,
                effectOrdinal: 0L,
                origin: CombatOrigin.System,
                actor: UnitRef.None,
                target: UnitRef.None,
                triggerKind: null,
                actionOrdinal: _completedActions,
                nextReadyAt: 0,
                deltaTime: 0,
                gaugeBefore: 0,
                gaugeAfter: 0,
                statusId: string.Empty,
                statusAmount: 0L,
                expiryActionOrdinal: 0,
                occurrenceId: 0L,
                consumedSources: null,
                resolvedAmount: 0,
                shieldBefore: 0,
                shieldAfter: 0,
                hpBefore: 0,
                hpAfter: 0,
                reasonCode: reasonCode,
                outcome: outcome));
        }

        public static int ComputeResolvedDamage(long baseDamage, int flatBonus, int percentPermille)
        {
            long raw = baseDamage + flatBonus;
            if (raw < 0L)
            {
                raw = 0L;
            }
            if (raw > 1000000000000L)
            {
                raw = 1000000000000L;
            }

            int clampedPercent = percentPermille < -1000 ? -1000 : percentPermille;
            long scaled = (raw * (1000L + clampedPercent) + 500L) / 1000L;
            if (scaled < 0L)
            {
                return 0;
            }
            if (scaled > int.MaxValue)
            {
                return int.MaxValue;
            }
            return (int)scaled;
        }

        public CombatStateSnapshot CaptureCurrentSnapshot()
        {
            return _snapshots.Count > 0 ? _snapshots[_snapshots.Count - 1] : _baseline;
        }

        public CombatTrace BuildTrace()
        {
            return new CombatTrace(
                _encounterId,
                RulesVersion,
                _baseline,
                _events.ToArray(),
                _snapshots.ToArray());
        }

        private bool AppendEvent(CombatEventRecord record)
        {
            if (_events.Count >= TraceRecordCeiling)
            {
                CeilingReached = true;
                CeilingDiagnostic = "Trace record ceiling (" + TraceRecordCeiling + ") exceeded at seq " + _nextSeq + ".";
                return false;
            }

            _events.Add(record);
            _snapshots.Add(CaptureSnapshot(record.Seq));
            _nextSeq += 1L;
            return true;
        }

        private CombatStateSnapshot CaptureSnapshot(long seq)
        {
            SyncSchedulerStates();
            IReadOnlyList<TurnForecastEntry> forecast = TurnOrderScheduler.ForecastFromStates(_schedulerUnits, 6);

            var heroViews = new HeroStateView[_heroes.Count];
            for (int i = 0; i < _heroes.Count; i++)
            {
                MutableUnit h = _heroes[i];
                var itemViews = new ItemCooldownView[h.Items.Count];
                for (int j = 0; j < h.Items.Count; j++)
                {
                    MutableHeroItem item = h.Items[j];
                    int actionsRemaining = 0;
                    bool isReady = false;
                    if (item.Trigger.Kind == TriggerKind.Cooldown)
                    {
                        // Hero's next action ordinal is h.ActionOrdinal + 1.
                        int nextActionOrd = h.ActionOrdinal + 1;
                        actionsRemaining = item.NextReadyAt > nextActionOrd
                            ? item.NextReadyAt - nextActionOrd
                            : 0;
                        isReady = h.IsAlive && actionsRemaining == 0;
                    }
                    else if (item.Trigger.Kind == TriggerKind.Reaction)
                    {
                        actionsRemaining = item.NextReadyAt > h.ActionOrdinal
                            ? item.NextReadyAt - h.ActionOrdinal
                            : 0;
                        isReady = h.IsAlive && actionsRemaining == 0;
                    }

                    itemViews[j] = new ItemCooldownView(
                        item.InstanceId,
                        item.DefinitionId,
                        item.Trigger.Kind,
                        item.EffectiveCooldown,
                        item.NextReadyAt,
                        actionsRemaining,
                        isReady);
                }

                heroViews[i] = new HeroStateView(
                    h.Ref.Slot,
                    h.Ref.InstanceId,
                    h.DefinitionId,
                    h.Hp,
                    h.MaxHp,
                    h.Shield,
                    h.Gauge,
                    h.EffectiveSpeed,
                    h.ActionOrdinal,
                    h.IsAlive,
                    h.BuildStatusViews(),
                    itemViews);
            }

            var enemyViews = new EnemyStateView[_enemies.Count];
            for (int i = 0; i < _enemies.Count; i++)
            {
                MutableUnit e = _enemies[i];
                BoundIntentEntry currentIntent = null;
                if (e.IntentSequence != null && e.IntentSequence.Count > 0)
                {
                    currentIntent = e.IntentSequence[e.IntentIndex % e.IntentSequence.Count];
                }

                enemyViews[i] = new EnemyStateView(
                    e.Ref.Slot,
                    e.Ref.InstanceId,
                    e.DefinitionId,
                    e.Hp,
                    e.MaxHp,
                    e.Shield,
                    e.Gauge,
                    e.EffectiveSpeed,
                    e.ActionOrdinal,
                    e.IntentIndex,
                    currentIntent,
                    e.IsAlive,
                    e.BuildStatusViews());
            }

            return new CombatStateSnapshot(seq, _completedActions, heroViews, enemyViews, forecast);
        }
    }
}
