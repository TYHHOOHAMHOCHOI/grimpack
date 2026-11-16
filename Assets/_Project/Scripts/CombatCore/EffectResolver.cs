using System;
using System.Collections.Generic;

namespace Project05.CombatCore
{
    internal sealed class QueuedEffect
    {
        public UnitRef ActorRef;
        public int Depth;
        public long? ParentEffectSeq;
        public long EffectOrdinal;
        public CombatOrigin Origin;
        public EffectDef Effect;
        public int DamageFlatBonus;
        public int DamagePercentPermille;
        public int StatusAmountBonus;
        public int BoundHeroInstanceId;
    }

    /// <summary>
    /// Deterministic FIFO effect resolver with D=8 reaction depth cap, N=256 effect cap,
    /// canonical target ordering, and once-per-root-action reaction listener suppression.
    /// </summary>
    internal static class EffectResolver
    {
        public const int MaxReactionDepth = 8;
        public const int MaxEffectsPerAction = 256;

        public static bool EnqueueHeroItem(
            CombatLedgerState ledger,
            MutableUnit owner,
            MutableHeroItem item,
            long rootActionId,
            int depth,
            long? parentEffectSeq,
            int nextReadyAtAfterFire,
            ref long nextEffectOrdinal,
            Queue<QueuedEffect> queue,
            HashSet<long> firedReactionsInRoot,
            out long? lastDownedOrTriggerSeq)
        {
            lastDownedOrTriggerSeq = null;

            long itemFiredOrdinal = nextEffectOrdinal++;
            if (!ledger.RecordItemFired(
                owner,
                item,
                owner.ActionOrdinal,
                nextReadyAtAfterFire,
                rootActionId,
                depth,
                parentEffectSeq,
                itemFiredOrdinal,
                out CombatEventRecord itemFiredEvent))
            {
                return false;
            }

            var origin = itemFiredEvent.Origin;
            if (item.Effects != null)
            {
                for (int i = 0; i < item.Effects.Count; i++)
                {
                    EffectDef eff = item.Effects[i];
                    queue.Enqueue(new QueuedEffect
                    {
                        ActorRef = owner.Ref,
                        Depth = depth,
                        ParentEffectSeq = itemFiredEvent.Seq,
                        EffectOrdinal = nextEffectOrdinal++,
                        Origin = origin,
                        Effect = eff,
                        DamageFlatBonus = item.DamageFlatBonus,
                        DamagePercentPermille = item.DamagePercentPermille,
                        StatusAmountBonus = eff.Kind == EffectKind.ApplyStatus ? item.GetStatusBonus(eff.Status) : 0,
                        BoundHeroInstanceId = -1
                    });
                }
            }

            // Evaluate reaction listeners on ItemFired itself.
            return DispatchReactionsForEvent(
                ledger,
                itemFiredEvent,
                rootActionId,
                ref nextEffectOrdinal,
                queue,
                firedReactionsInRoot);
        }

        public static bool EnqueueEnemyMove(
            CombatLedgerState ledger,
            MutableUnit enemy,
            BoundIntentEntry intent,
            long rootActionId,
            long actionStartedSeq,
            ref long nextEffectOrdinal,
            Queue<QueuedEffect> queue)
        {
            if (intent == null || intent.Glyph == MoveGlyphKind.Windup || intent.HitCount <= 0 || intent.ScaledBaseDamage <= 0)
            {
                return true;
            }

            TargetKind targetKind;
            switch (intent.TargetMode)
            {
                case MoveTargetMode.AllHeroes:
                    targetKind = TargetKind.AllHeroes;
                    break;
                case MoveTargetMode.RandomHero:
                case MoveTargetMode.SameAsMove:
                    targetKind = TargetKind.BoundHero;
                    break;
                default:
                    return true;
            }

            var origin = new CombatOrigin(
                OriginKind.EnemyMove,
                -1,
                0L,
                string.Empty,
                new GridCell(0, 0),
                enemy.Ref.InstanceId,
                enemy.DefinitionId,
                intent.MoveId,
                ledger.AllocateOccurrenceId(),
                Array.Empty<AppliedModifierRef>());

            var syntheticEffect = new EffectDef(
                EffectKind.Damage,
                targetKind,
                intent.ScaledBaseDamage,
                string.Empty,
                0,
                0,
                0);

            for (int hit = 0; hit < intent.HitCount; hit++)
            {
                queue.Enqueue(new QueuedEffect
                {
                    ActorRef = enemy.Ref,
                    Depth = 0,
                    ParentEffectSeq = actionStartedSeq,
                    EffectOrdinal = nextEffectOrdinal++,
                    Origin = origin,
                    Effect = syntheticEffect,
                    DamageFlatBonus = 0,
                    DamagePercentPermille = 0,
                    StatusAmountBonus = 0,
                    BoundHeroInstanceId = intent.BoundHeroInstanceId
                });
            }

            return true;
        }

        public static bool DrainQueue(
            CombatLedgerState ledger,
            long rootActionId,
            Queue<QueuedEffect> queue,
            HashSet<long> firedReactionsInRoot,
            ref long nextEffectOrdinal,
            out long? lastTerminalCauseSeq)
        {
            lastTerminalCauseSeq = null;
            int processedCount = 0;

            while (queue.Count > 0)
            {
                if (processedCount >= MaxEffectsPerAction)
                {
                    QueuedEffect blocked = queue.Peek();
                    ledger.RecordChainLimit(
                        blocked.ActorRef,
                        blocked.Origin,
                        rootActionId,
                        blocked.Depth,
                        blocked.ParentEffectSeq,
                        blocked.EffectOrdinal,
                        "effect_cap");
                    break;
                }

                QueuedEffect item = queue.Dequeue();
                processedCount += 1;

                if (item.Depth > MaxReactionDepth)
                {
                    ledger.RecordChainLimit(
                        item.ActorRef,
                        item.Origin,
                        rootActionId,
                        item.Depth,
                        item.ParentEffectSeq,
                        item.EffectOrdinal,
                        "reaction_depth");
                    break;
                }

                List<MutableUnit> targets = ResolveTargets(ledger, item);
                if (targets.Count == 0)
                {
                    if (!ledger.RecordEffectSkipped(
                        item.ActorRef,
                        UnitRef.None,
                        item.Origin,
                        rootActionId,
                        item.Depth,
                        item.ParentEffectSeq,
                        item.EffectOrdinal,
                        "no_valid_target"))
                    {
                        return false;
                    }
                    continue;
                }

                for (int t = 0; t < targets.Count; t++)
                {
                    MutableUnit target = targets[t];
                    if (!ExecuteEffectOnTarget(
                        ledger,
                        item,
                        target,
                        rootActionId,
                        ref nextEffectOrdinal,
                        queue,
                        firedReactionsInRoot,
                        ref lastTerminalCauseSeq))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static List<MutableUnit> ResolveTargets(CombatLedgerState ledger, QueuedEffect queued)
        {
            var targets = new List<MutableUnit>();
            switch (queued.Effect.Target)
            {
                case TargetKind.FirstEnemy:
                {
                    MutableUnit first = ledger.FindFirstAliveEnemy();
                    if (first != null) targets.Add(first);
                    break;
                }
                case TargetKind.AllEnemies:
                {
                    for (int i = 0; i < ledger.Enemies.Count; i++)
                    {
                        if (ledger.Enemies[i].IsAlive) targets.Add(ledger.Enemies[i]);
                    }
                    break;
                }
                case TargetKind.Self:
                {
                    MutableUnit self = ledger.FindUnit(queued.ActorRef);
                    if (self != null) targets.Add(self);
                    break;
                }
                case TargetKind.AllHeroes:
                {
                    for (int i = 0; i < ledger.Heroes.Count; i++)
                    {
                        if (ledger.Heroes[i].IsAlive) targets.Add(ledger.Heroes[i]);
                    }
                    break;
                }
                case TargetKind.LowestHpHero:
                {
                    MutableUnit lowest = ledger.FindLowestHpAliveHero();
                    if (lowest != null) targets.Add(lowest);
                    break;
                }
                case TargetKind.BoundHero:
                {
                    MutableUnit bound = ledger.FindHeroByInstanceId(queued.BoundHeroInstanceId);
                    if (bound != null) targets.Add(bound);
                    break;
                }
            }
            return targets;
        }

        private static bool ExecuteEffectOnTarget(
            CombatLedgerState ledger,
            QueuedEffect queued,
            MutableUnit target,
            long rootActionId,
            ref long nextEffectOrdinal,
            Queue<QueuedEffect> queue,
            HashSet<long> firedReactionsInRoot,
            ref long? lastTerminalCauseSeq)
        {
            switch (queued.Effect.Kind)
            {
                case EffectKind.Damage:
                {
                    int resolvedDamage = CombatLedgerState.ComputeResolvedDamage(
                        queued.Effect.Base,
                        queued.DamageFlatBonus,
                        queued.DamagePercentPermille);

                    if (!ledger.ApplyDamage(
                        queued.ActorRef,
                        target,
                        resolvedDamage,
                        queued.Origin,
                        rootActionId,
                        queued.Depth,
                        queued.ParentEffectSeq,
                        queued.EffectOrdinal,
                        isDetonation: false,
                        out CombatEventRecord dmgEv,
                        out CombatEventRecord downEv))
                    {
                        return false;
                    }

                    if (dmgEv != null)
                    {
                        if (!DispatchReactionsForEvent(ledger, dmgEv, rootActionId, ref nextEffectOrdinal, queue, firedReactionsInRoot))
                        {
                            return false;
                        }
                    }
                    if (downEv != null)
                    {
                        lastTerminalCauseSeq = downEv.Seq;
                        if (!DispatchReactionsForEvent(ledger, downEv, rootActionId, ref nextEffectOrdinal, queue, firedReactionsInRoot))
                        {
                            return false;
                        }
                    }
                    return true;
                }
                case EffectKind.ApplyStatus:
                {
                    int amount = queued.Effect.Amount + queued.StatusAmountBonus;
                    if (!ledger.ApplyStatus(
                        queued.ActorRef,
                        target,
                        queued.Effect.Status,
                        amount,
                        queued.Origin,
                        rootActionId,
                        queued.Depth,
                        queued.ParentEffectSeq,
                        queued.EffectOrdinal,
                        out CombatEventRecord statusEv))
                    {
                        return false;
                    }

                    if (statusEv != null)
                    {
                        if (!DispatchReactionsForEvent(ledger, statusEv, rootActionId, ref nextEffectOrdinal, queue, firedReactionsInRoot))
                        {
                            return false;
                        }
                    }
                    return true;
                }
                case EffectKind.ConsumeDetonate:
                {
                    if (!ledger.ConsumeAndDetonate(
                        queued.ActorRef,
                        target,
                        queued.Effect.Status,
                        queued.Effect.Units,
                        queued.Effect.PerUnit,
                        queued.DamageFlatBonus,
                        queued.DamagePercentPermille,
                        queued.Origin,
                        rootActionId,
                        queued.Depth,
                        queued.ParentEffectSeq,
                        queued.EffectOrdinal,
                        out CombatEventRecord consumedEv,
                        out CombatEventRecord detDmgEv,
                        out CombatEventRecord downEv))
                    {
                        return false;
                    }

                    if (consumedEv != null)
                    {
                        if (!DispatchReactionsForEvent(ledger, consumedEv, rootActionId, ref nextEffectOrdinal, queue, firedReactionsInRoot))
                        {
                            return false;
                        }
                    }
                    if (detDmgEv != null)
                    {
                        if (!DispatchReactionsForEvent(ledger, detDmgEv, rootActionId, ref nextEffectOrdinal, queue, firedReactionsInRoot))
                        {
                            return false;
                        }
                    }
                    if (downEv != null)
                    {
                        lastTerminalCauseSeq = downEv.Seq;
                        if (!DispatchReactionsForEvent(ledger, downEv, rootActionId, ref nextEffectOrdinal, queue, firedReactionsInRoot))
                        {
                            return false;
                        }
                    }
                    return true;
                }
                case EffectKind.ShieldGain:
                {
                    return ledger.ApplyShieldGain(
                        queued.ActorRef,
                        target,
                        queued.Effect.Amount,
                        queued.Origin,
                        rootActionId,
                        queued.Depth,
                        queued.ParentEffectSeq,
                        queued.EffectOrdinal);
                }
                case EffectKind.Heal:
                {
                    return ledger.ApplyHeal(
                        queued.ActorRef,
                        target,
                        queued.Effect.Amount,
                        queued.Origin,
                        rootActionId,
                        queued.Depth,
                        queued.ParentEffectSeq,
                        queued.EffectOrdinal);
                }
                default:
                    return true;
            }
        }

        private static bool DispatchReactionsForEvent(
            CombatLedgerState ledger,
            CombatEventRecord sourceEvent,
            long rootActionId,
            ref long nextEffectOrdinal,
            Queue<QueuedEffect> queue,
            HashSet<long> firedReactionsInRoot)
        {
            if (sourceEvent == null)
            {
                return true;
            }

            // Walk heroes in slot order, and items in canonical row-by-row order.
            for (int h = 0; h < ledger.Heroes.Count; h++)
            {
                MutableUnit hero = ledger.Heroes[h];
                if (!hero.IsAlive || hero.Items == null)
                {
                    continue;
                }

                for (int i = 0; i < hero.Items.Count; i++)
                {
                    MutableHeroItem item = hero.Items[i];
                    if (item.Trigger.Kind != TriggerKind.Reaction)
                    {
                        continue;
                    }

                    if (!MatchesReactionTrigger(hero, item, sourceEvent))
                    {
                        continue;
                    }

                    // Once-per-root-action suppression must run before cooldown check so a second match in the same root emits ListenerSuppressed.
                    if (firedReactionsInRoot.Contains(item.InstanceId))
                    {
                        if (!ledger.RecordListenerSuppressed(
                            hero.Ref,
                            item,
                            rootActionId,
                            sourceEvent.Depth + 1,
                            sourceEvent.Seq,
                            nextEffectOrdinal++,
                            sourceEvent.Seq))
                        {
                            return false;
                        }
                        continue;
                    }

                    // Check cooldown readiness: ready when hero.ActionOrdinal >= item.NextReadyAt.
                    if (hero.ActionOrdinal < item.NextReadyAt)
                    {
                        continue;
                    }

                    int nextDepth = sourceEvent.Depth + 1;
                    if (nextDepth > MaxReactionDepth)
                    {
                        var limitOrigin = new CombatOrigin(
                            OriginKind.Item,
                            hero.Ref.Slot,
                            item.InstanceId,
                            item.DefinitionId,
                            item.AnchorCell,
                            -1,
                            string.Empty,
                            string.Empty,
                            0L,
                            item.AppliedModifiers);

                        if (!ledger.RecordChainLimit(
                            hero.Ref,
                            limitOrigin,
                            rootActionId,
                            nextDepth,
                            sourceEvent.Seq,
                            nextEffectOrdinal++,
                            "reaction_depth"))
                        {
                            return false;
                        }
                        continue;
                    }

                    firedReactionsInRoot.Add(item.InstanceId);
                    int nextReadyAt = hero.ActionOrdinal + item.EffectiveCooldown;

                    if (!EnqueueHeroItem(
                        ledger,
                        hero,
                        item,
                        rootActionId,
                        nextDepth,
                        sourceEvent.Seq,
                        nextReadyAt,
                        ref nextEffectOrdinal,
                        queue,
                        firedReactionsInRoot,
                        out _))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool MatchesReactionTrigger(
            MutableUnit listenerOwner,
            MutableHeroItem item,
            CombatEventRecord ev)
        {
            TriggerDef t = item.Trigger;

            switch (t.EventKind)
            {
                case ReactionEventKind.ItemFired:
                    if (ev.Kind != CombatEventKind.ItemFired) return false;
                    if (ev.Origin != null && ev.Origin.ItemInstanceId == item.InstanceId) return false;
                    break;
                case ReactionEventKind.StatusApplied:
                    if (ev.Kind != CombatEventKind.StatusApplied) return false;
                    if (!string.IsNullOrEmpty(t.EventStatus) &&
                        !string.Equals(t.EventStatus, ev.StatusId, StringComparison.Ordinal))
                    {
                        return false;
                    }
                    break;
                case ReactionEventKind.StatusConsumed:
                    if (ev.Kind != CombatEventKind.StatusConsumed) return false;
                    if (!string.IsNullOrEmpty(t.EventStatus) &&
                        !string.Equals(t.EventStatus, ev.StatusId, StringComparison.Ordinal))
                    {
                        return false;
                    }
                    break;
                case ReactionEventKind.Damage:
                    if (ev.Kind != CombatEventKind.Damage && ev.Kind != CombatEventKind.DetonationDamage)
                    {
                        return false;
                    }
                    break;
                case ReactionEventKind.UnitDowned:
                    if (ev.Kind != CombatEventKind.UnitDowned) return false;
                    break;
                default:
                    return false;
            }

            UnitRef subject = ev.Kind == CombatEventKind.ItemFired ? ev.Actor : ev.Target;
            switch (t.EventSide)
            {
                case ReactionSideFilter.Any:
                    return true;
                case ReactionSideFilter.Hero:
                    return subject.IsValid && subject.Side == UnitSide.Hero;
                case ReactionSideFilter.Enemy:
                    return subject.IsValid && subject.Side == UnitSide.Enemy;
                case ReactionSideFilter.OwnHero:
                    return subject.IsValid && subject == listenerOwner.Ref;
                case ReactionSideFilter.OtherHero:
                    return subject.IsValid && subject.Side == UnitSide.Hero && subject != listenerOwner.Ref;
                default:
                    return false;
            }
        }
    }
}
