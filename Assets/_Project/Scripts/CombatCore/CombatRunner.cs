using System;
using System.Collections.Generic;

namespace Project05.CombatCore
{
    /// <summary>
    /// Deterministic engine-free encounter preparation and execution entry point.
    /// </summary>
    public static class CombatRunner
    {
        public const int MaxActorActions = 200;
        public const int MaxReactionDepth = EffectResolver.MaxReactionDepth;
        public const int MaxEffectsPerAction = EffectResolver.MaxEffectsPerAction;
        public const int GaugeThreshold = TurnOrderScheduler.GaugeThreshold;
        public const int IntentHorizon = EnemyIntentSelector.IntentHorizon;

        public static bool TryPrepare(
            GameCatalog catalog,
            EncounterSetup setup,
            SeededRng rng,
            out PreparedEncounter prepared,
            out string error)
        {
            prepared = null;
            error = string.Empty;

            if (catalog == null)
            {
                error = "GameCatalog is null.";
                return false;
            }
            if (setup == null)
            {
                error = "EncounterSetup is null.";
                return false;
            }
            if (rng == null)
            {
                error = "SeededRng is null.";
                return false;
            }
            if (setup.Heroes == null || setup.Heroes.Count < 1 || setup.Heroes.Count > 3)
            {
                error = "Encounter must contain between 1 and 3 heroes.";
                return false;
            }
            if (setup.Enemies == null || setup.Enemies.Count < 1 || setup.Enemies.Count > 6)
            {
                error = "Encounter must contain between 1 and 6 enemies.";
                return false;
            }

            var sortedHeroes = new List<HeroBaselineInput>(setup.Heroes);
            sortedHeroes.Sort((a, b) => (a != null ? a.Slot : -1).CompareTo(b != null ? b.Slot : -1));

            for (int i = 0; i < sortedHeroes.Count; i++)
            {
                HeroBaselineInput h = sortedHeroes[i];
                if (h == null || h.Definition == null)
                {
                    error = "Hero baseline at index " + i + " is null or missing Definition.";
                    return false;
                }
                if (h.Slot < 0 || h.Slot >= 3)
                {
                    error = "Hero slot " + h.Slot + " is out of range [0..2].";
                    return false;
                }
                if (i > 0 && sortedHeroes[i - 1].Slot == h.Slot)
                {
                    error = "Duplicate hero slot " + h.Slot + ".";
                    return false;
                }
                if (h.FrozenGrid != null && h.FrozenGrid.Items != null)
                {
                    for (int p = 0; p < h.FrozenGrid.Items.Count; p++)
                    {
                        string defId = h.FrozenGrid.Items[p].DefinitionId;
                        if (catalog.FindItem(defId) == null)
                        {
                            error = "Hero slot " + h.Slot + " grid references unknown item '" + defId + "'.";
                            return false;
                        }
                    }
                }
            }

            var sortedEnemies = new List<EnemySpawnInput>(setup.Enemies);
            sortedEnemies.Sort((a, b) => (a != null ? a.Slot : -1).CompareTo(b != null ? b.Slot : -1));

            for (int i = 0; i < sortedEnemies.Count; i++)
            {
                EnemySpawnInput e = sortedEnemies[i];
                if (e == null || e.Definition == null)
                {
                    error = "Enemy spawn at index " + i + " is null or missing Definition.";
                    return false;
                }
                if (e.Slot < 0 || e.Slot >= 6)
                {
                    error = "Enemy slot " + e.Slot + " is out of range [0..5].";
                    return false;
                }
                if (i > 0 && sortedEnemies[i - 1].Slot == e.Slot)
                {
                    error = "Duplicate enemy slot " + e.Slot + ".";
                    return false;
                }
            }

            if (!EnemyIntentSelector.TryBindAll(
                setup,
                sortedHeroes,
                sortedEnemies,
                rng,
                out IReadOnlyList<BoundEnemyState> boundEnemies,
                out error))
            {
                return false;
            }

            var ledger = new CombatLedgerState(catalog, setup, boundEnemies);
            CombatStateSnapshot baseline = ledger.CaptureCurrentSnapshot();

            prepared = new PreparedEncounter(
                setup,
                baseline.Heroes,
                boundEnemies,
                baseline.Forecast);
            return true;
        }

        public static CombatResult RunEncounter(
            GameCatalog catalog,
            EncounterSetup setup,
            SeededRng rng)
        {
            if (!TryPrepare(catalog, setup, rng, out PreparedEncounter prepared, out string error))
            {
                return new CombatResult(CombatRunStatus.TechnicalStop, null, error, null);
            }

            return RunEncounter(catalog, prepared);
        }

        public static CombatResult RunEncounter(
            GameCatalog catalog,
            PreparedEncounter prepared)
        {
            if (catalog == null)
            {
                return new CombatResult(CombatRunStatus.TechnicalStop, null, "GameCatalog is null.", null);
            }
            if (prepared == null || prepared.Setup == null || prepared.BoundEnemies == null)
            {
                return new CombatResult(CombatRunStatus.TechnicalStop, null, "PreparedEncounter is invalid.", null);
            }

            var ledger = new CombatLedgerState(catalog, prepared.Setup, prepared.BoundEnemies);

            if (!ledger.RecordIntentsBound())
            {
                return BuildTechnicalStop(ledger);
            }

            // Pre-fight CombatStart phase (one pre-fight root action per hero with CombatStart items, a = 0, outside 200-action cap).
            for (int h = 0; h < ledger.Heroes.Count; h++)
            {
                MutableUnit hero = ledger.Heroes[h];
                if (!hero.IsAlive || hero.Items == null)
                {
                    continue;
                }

                var combatStartItems = new List<MutableHeroItem>();
                for (int i = 0; i < hero.Items.Count; i++)
                {
                    if (hero.Items[i].Trigger.Kind == TriggerKind.CombatStart)
                    {
                        combatStartItems.Add(hero.Items[i]);
                    }
                }

                if (combatStartItems.Count == 0)
                {
                    continue;
                }

                var combatStartOrigin = new CombatOrigin(
                    OriginKind.EncounterSetup,
                    hero.Ref.Slot,
                    0L,
                    string.Empty,
                    new GridCell(0, 0),
                    -1,
                    string.Empty,
                    string.Empty,
                    0L,
                    Array.Empty<AppliedModifierRef>());

                if (!ledger.RecordActionStarted(
                    hero,
                    rootActionId: 0L,
                    deltaTime: 0,
                    gaugeBefore: hero.Gauge,
                    gaugeAfter: hero.Gauge,
                    origin: combatStartOrigin,
                    incrementActionOrdinal: false,
                    reasonCode: "combat_start"))
                {
                    return BuildTechnicalStop(ledger);
                }

                long startSeq = ledger.Events[ledger.Events.Count - 1].Seq;
                var queue = new Queue<QueuedEffect>();
                var firedReactions = new HashSet<long>();
                long nextEffectOrdinal = 1L;

                for (int i = 0; i < combatStartItems.Count; i++)
                {
                    if (!EffectResolver.EnqueueHeroItem(
                        ledger,
                        hero,
                        combatStartItems[i],
                        rootActionId: 0L,
                        depth: 0,
                        parentEffectSeq: startSeq,
                        nextReadyAtAfterFire: 0,
                        ref nextEffectOrdinal,
                        queue,
                        firedReactions,
                        out _))
                    {
                        return BuildTechnicalStop(ledger);
                    }
                }

                if (!EffectResolver.DrainQueue(
                    ledger,
                    rootActionId: 0L,
                    queue,
                    firedReactions,
                    ref nextEffectOrdinal,
                    out long? terminalCauseSeq))
                {
                    return BuildTechnicalStop(ledger);
                }

                if (ledger.TryEvaluateOutcome(false, out CombatOutcome preOutcome, out string preReason))
                {
                    if (!ledger.RecordCombatEnded(0L, terminalCauseSeq, preOutcome, preReason))
                    {
                        return BuildTechnicalStop(ledger);
                    }
                    return new CombatResult(
                        CombatRunStatus.Completed,
                        preOutcome,
                        string.Empty,
                        ledger.BuildTrace());
                }
            }

            // Check degenerate initial outcome if CombatStart wasn't triggered.
            if (ledger.TryEvaluateOutcome(false, out CombatOutcome initOutcome, out string initReason))
            {
                if (!ledger.RecordCombatEnded(0L, null, initOutcome, initReason))
                {
                    return BuildTechnicalStop(ledger);
                }
                return new CombatResult(CombatRunStatus.Completed, initOutcome, string.Empty, ledger.BuildTrace());
            }

            // Main turn-order loop up to MaxActorActions = 200.
            while (ledger.CompletedActions < MaxActorActions)
            {
                if (!ledger.TryAdvanceToNextActor(
                    out MutableUnit actor,
                    out int deltaTime,
                    out int gaugeBefore,
                    out int gaugeAfter))
                {
                    CombatOutcome fallbackOutcome = ledger.CountAliveHeroes() > 0 && ledger.CountAliveEnemies() == 0
                        ? CombatOutcome.Victory
                        : (ledger.CountAliveHeroes() == 0 && ledger.CountAliveEnemies() > 0
                            ? CombatOutcome.Defeat
                            : CombatOutcome.Draw);
                    if (!ledger.RecordCombatEnded(ledger.CompletedActions, null, fallbackOutcome, "no_eligible_actor"))
                    {
                        return BuildTechnicalStop(ledger);
                    }
                    return new CombatResult(
                        CombatRunStatus.Completed,
                        fallbackOutcome,
                        string.Empty,
                        ledger.BuildTrace());
                }

                long rootActionId = ledger.CompletedActions + 1L;
                bool isStunned = actor.HasStatus(CombatLedgerState.StunStatusId);

                if (isStunned)
                {
                    if (!ledger.RecordActionSkipped(
                        actor,
                        rootActionId,
                        deltaTime,
                        gaugeBefore,
                        gaugeAfter,
                        "stunned"))
                    {
                        return BuildTechnicalStop(ledger);
                    }

                    if (!ledger.ExpireDueStatuses(actor, rootActionId))
                    {
                        return BuildTechnicalStop(ledger);
                    }

                    ledger.IncrementCompletedActions();

                    bool limitReached = ledger.CompletedActions >= MaxActorActions;
                    if (ledger.TryEvaluateOutcome(limitReached, out CombatOutcome skipOutcome, out string skipReason))
                    {
                        if (!ledger.RecordCombatEnded(rootActionId, null, skipOutcome, skipReason))
                        {
                            return BuildTechnicalStop(ledger);
                        }
                        return new CombatResult(
                            CombatRunStatus.Completed,
                            skipOutcome,
                            string.Empty,
                            ledger.BuildTrace());
                    }

                    continue;
                }

                CombatOrigin actionOrigin = CombatOrigin.System;
                BoundIntentEntry chosenIntent = null;
                List<MutableHeroItem> readyHeroItems = null;

                if (actor.Ref.Side == UnitSide.Hero)
                {
                    readyHeroItems = new List<MutableHeroItem>();
                    int nextActionOrdinal = actor.ActionOrdinal + 1;
                    if (actor.Items != null)
                    {
                        for (int i = 0; i < actor.Items.Count; i++)
                        {
                            MutableHeroItem item = actor.Items[i];
                            if (item.Trigger.Kind == TriggerKind.Cooldown && nextActionOrdinal >= item.NextReadyAt)
                            {
                                readyHeroItems.Add(item);
                            }
                        }
                    }

                    if (readyHeroItems.Count == 0)
                    {
                        if (!ledger.RecordActionSkipped(
                            actor,
                            rootActionId,
                            deltaTime,
                            gaugeBefore,
                            gaugeAfter,
                            "no_ready_item"))
                        {
                            return BuildTechnicalStop(ledger);
                        }

                        if (!ledger.ExpireDueStatuses(actor, rootActionId))
                        {
                            return BuildTechnicalStop(ledger);
                        }

                        ledger.IncrementCompletedActions();

                        bool noItemLimitReached = ledger.CompletedActions >= MaxActorActions;
                        if (ledger.TryEvaluateOutcome(noItemLimitReached, out CombatOutcome noItemOutcome, out string noItemReason))
                        {
                            if (!ledger.RecordCombatEnded(rootActionId, null, noItemOutcome, noItemReason))
                            {
                                return BuildTechnicalStop(ledger);
                            }
                            return new CombatResult(
                                CombatRunStatus.Completed,
                                noItemOutcome,
                                string.Empty,
                                ledger.BuildTrace());
                        }

                        continue;
                    }
                }
                else if (actor.Ref.Side == UnitSide.Enemy &&
                    actor.IntentSequence != null &&
                    actor.IntentSequence.Count > 0)
                {
                    int seqIdx = actor.IntentIndex % actor.IntentSequence.Count;
                    chosenIntent = actor.IntentSequence[seqIdx];
                    actionOrigin = new CombatOrigin(
                        OriginKind.EnemyMove,
                        -1,
                        0L,
                        string.Empty,
                        new GridCell(0, 0),
                        actor.Ref.InstanceId,
                        actor.DefinitionId,
                        chosenIntent.MoveId,
                        0L,
                        Array.Empty<AppliedModifierRef>());
                }

                if (!ledger.RecordActionStarted(
                    actor,
                    rootActionId,
                    deltaTime,
                    gaugeBefore,
                    gaugeAfter,
                    actionOrigin))
                {
                    return BuildTechnicalStop(ledger);
                }

                long actionStartedSeq = ledger.Events[ledger.Events.Count - 1].Seq;
                var queue = new Queue<QueuedEffect>();
                var firedReactions = new HashSet<long>();
                long nextEffectOrdinal = 1L;

                if (actor.Ref.Side == UnitSide.Hero && readyHeroItems != null)
                {
                    for (int i = 0; i < readyHeroItems.Count; i++)
                    {
                        MutableHeroItem item = readyHeroItems[i];
                        int nextReadyAt = actor.ActionOrdinal + item.EffectiveCooldown;
                        if (!EffectResolver.EnqueueHeroItem(
                            ledger,
                            actor,
                            item,
                            rootActionId,
                            depth: 0,
                            parentEffectSeq: actionStartedSeq,
                            nextReadyAtAfterFire: nextReadyAt,
                            ref nextEffectOrdinal,
                            queue,
                            firedReactions,
                            out _))
                        {
                            return BuildTechnicalStop(ledger);
                        }
                    }
                }
                else if (actor.Ref.Side == UnitSide.Enemy && chosenIntent != null)
                {
                    actor.IntentIndex = (actor.IntentIndex + 1) % actor.IntentSequence.Count;
                    if (!EffectResolver.EnqueueEnemyMove(
                        ledger,
                        actor,
                        chosenIntent,
                        rootActionId,
                        actionStartedSeq,
                        ref nextEffectOrdinal,
                        queue))
                    {
                        return BuildTechnicalStop(ledger);
                    }
                }

                if (!EffectResolver.DrainQueue(
                    ledger,
                    rootActionId,
                    queue,
                    firedReactions,
                    ref nextEffectOrdinal,
                    out long? terminalCauseSeq))
                {
                    return BuildTechnicalStop(ledger);
                }

                if (!ledger.ExpireDueStatuses(actor, rootActionId))
                {
                    return BuildTechnicalStop(ledger);
                }

                ledger.IncrementCompletedActions();

                bool capReached = ledger.CompletedActions >= MaxActorActions;
                if (ledger.TryEvaluateOutcome(capReached, out CombatOutcome outcome, out string reasonCode))
                {
                    if (!ledger.RecordCombatEnded(rootActionId, terminalCauseSeq, outcome, reasonCode))
                    {
                        return BuildTechnicalStop(ledger);
                    }
                    return new CombatResult(
                        CombatRunStatus.Completed,
                        outcome,
                        string.Empty,
                        ledger.BuildTrace());
                }
            }

            // Defensive fallback if loop exits via cap.
            ledger.TryEvaluateOutcome(true, out CombatOutcome endOutcome, out string endReason);
            if (!ledger.RecordCombatEnded(ledger.CompletedActions, null, endOutcome, endReason))
            {
                return BuildTechnicalStop(ledger);
            }
            return new CombatResult(
                CombatRunStatus.Completed,
                endOutcome,
                string.Empty,
                ledger.BuildTrace());
        }

        private static CombatResult BuildTechnicalStop(CombatLedgerState ledger)
        {
            return new CombatResult(
                CombatRunStatus.TechnicalStop,
                null,
                ledger.CeilingDiagnostic,
                ledger.BuildTrace());
        }
    }
}
