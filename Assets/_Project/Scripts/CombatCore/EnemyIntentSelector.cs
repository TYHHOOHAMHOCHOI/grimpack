using System;
using System.Collections.Generic;

namespace Project05.CombatCore
{
    /// <summary>
    /// Pre-rolls binding 12-action enemy intent sequences using the RNG "intents" substream.
    /// Guarantees atomic preparation and 2 RNG calls per bound entry (move + target).
    /// </summary>
    public static class EnemyIntentSelector
    {
        public const int IntentHorizon = 12;
        public const int HeavyGap = 2;
        public const int RepeatLimit = 2;

        internal static bool TryBindAll(
            EncounterSetup setup,
            IReadOnlyList<HeroBaselineInput> sortedHeroes,
            IReadOnlyList<EnemySpawnInput> sortedEnemies,
            SeededRng rng,
            out IReadOnlyList<BoundEnemyState> boundEnemies,
            out string error)
        {
            boundEnemies = Array.Empty<BoundEnemyState>();
            error = string.Empty;

            if (rng == null)
            {
                error = "SeededRng is required to bind enemy intents.";
                return false;
            }
            if (sortedHeroes == null || sortedHeroes.Count == 0)
            {
                error = "At least one hero is required to bind enemy intents.";
                return false;
            }
            if (sortedEnemies == null || sortedEnemies.Count == 0)
            {
                error = "At least one enemy is required to bind enemy intents.";
                return false;
            }

            RngStream originalIntents = rng.GetStream("intents");
            RngStream workingIntents = originalIntents.Clone();

            var result = new BoundEnemyState[sortedEnemies.Count];

            for (int eIdx = 0; eIdx < sortedEnemies.Count; eIdx++)
            {
                EnemySpawnInput spawn = sortedEnemies[eIdx];
                if (spawn == null || spawn.Definition == null)
                {
                    error = "Enemy spawn at index " + eIdx + " has a null definition.";
                    return false;
                }
                if (spawn.HpPermille <= 0)
                {
                    error = "Enemy '" + spawn.Definition.Id + "' HpPermille must be positive.";
                    return false;
                }
                if (spawn.DamagePermille <= 0)
                {
                    error = "Enemy '" + spawn.Definition.Id + "' DamagePermille must be positive.";
                    return false;
                }
                if (spawn.Definition.Moves == null || spawn.Definition.Moves.Count == 0)
                {
                    error = "Enemy '" + spawn.Definition.Id + "' has no moves defined.";
                    return false;
                }

                int enemyInstanceId = 101 + spawn.Slot;
                int scaledMaxHp = ScaleStat(spawn.Definition.BaseMaxHp, spawn.HpPermille, minimum: 1);
                int effectiveSpeed = TurnOrderScheduler.EffectiveSpeed(spawn.Definition.BaseSpeed, 0, 0);

                BoundIntentEntry[] sequence = BuildEnemySequence(
                    spawn.Definition,
                    spawn.DamagePermille,
                    sortedHeroes,
                    workingIntents,
                    out error);

                if (sequence == null)
                {
                    return false;
                }

                result[eIdx] = new BoundEnemyState(
                    enemyInstanceId,
                    spawn.Slot,
                    spawn.Definition.Id,
                    scaledMaxHp,
                    effectiveSpeed,
                    sequence);
            }

            // Atomic commit only after every enemy succeeds.
            originalIntents.CopyFrom(workingIntents);
            boundEnemies = result;
            return true;
        }

        private static BoundIntentEntry[] BuildEnemySequence(
            MonsterDef def,
            int damagePermille,
            IReadOnlyList<HeroBaselineInput> sortedHeroes,
            RngStream intentsStream,
            out string error)
        {
            error = string.Empty;
            var entries = new BoundIntentEntry[IntentHorizon];
            var chosenMoves = new MonsterMoveDef[IntentHorizon];

            for (int i = 0; i < IntentHorizon; i++)
            {
                MonsterMoveDef move = SelectMoveForIndex(def, i, chosenMoves, intentsStream, out error);
                if (move == null)
                {
                    return null;
                }
                chosenMoves[i] = move;

                // 2nd RNG call per entry: target draw on "intents" stream.
                int targetRoll = intentsStream.NextBounded(sortedHeroes.Count);
                int boundHeroInstanceId = ResolveBoundHeroId(move, targetRoll, i, chosenMoves, entries, sortedHeroes);

                int scaledBaseDamage = move.BaseDamage <= 0
                    ? 0
                    : ScaleStat(move.BaseDamage, damagePermille, minimum: 1);

                entries[i] = new BoundIntentEntry(
                    i,
                    move.Id,
                    move.Glyph,
                    move.Delivery,
                    move.HitCount,
                    scaledBaseDamage,
                    move.TargetMode,
                    boundHeroInstanceId);
            }

            return entries;
        }

        private static MonsterMoveDef SelectMoveForIndex(
            MonsterDef def,
            int index,
            MonsterMoveDef[] priorMoves,
            RngStream intentsStream,
            out string error)
        {
            error = string.Empty;

            // Forced opener on index 0 if specified.
            if (index == 0 && !string.IsNullOrEmpty(def.OpenerMoveId))
            {
                MonsterMoveDef opener = FindMoveById(def, def.OpenerMoveId);
                if (opener == null)
                {
                    error = "Monster '" + def.Id + "' openerMoveId '" + def.OpenerMoveId + "' not found.";
                    return null;
                }
                // Consume 1 RNG word via NextWeighted([1]) to preserve exact 2-call-per-entry stride.
                intentsStream.NextBounded(1);
                return opener;
            }

            // Forced linkedNext follow-up if prior move specifies one.
            if (index > 0 && priorMoves[index - 1] != null && !string.IsNullOrEmpty(priorMoves[index - 1].LinkedNextMoveId))
            {
                MonsterMoveDef linked = FindMoveById(def, priorMoves[index - 1].LinkedNextMoveId);
                if (linked == null)
                {
                    error = "Monster '" + def.Id + "' linkedNextMoveId '" + priorMoves[index - 1].LinkedNextMoveId + "' not found.";
                    return null;
                }
                intentsStream.NextBounded(1);
                return linked;
            }

            // Phase pool filtering (if boss phases apply to this index).
            List<MonsterMoveDef> phasePool = GetPhasePool(def, index);
            if (phasePool.Count == 0)
            {
                error = "Monster '" + def.Id + "' has no eligible moves at intent index " + index + ".";
                return null;
            }

            // Strict: satisfy both repeat limit and heavy gap.
            List<MonsterMoveDef> candidates = FilterCandidates(phasePool, index, priorMoves, enforceRepeatLimit: true, enforceHeavyGap: true);

            // Relaxation 1: relax heavy gap, keep repeat limit.
            if (candidates.Count == 0)
            {
                candidates = FilterCandidates(phasePool, index, priorMoves, enforceRepeatLimit: true, enforceHeavyGap: false);
            }

            // Relaxation 2: relax repeat limit as well (full phase pool with positive weights).
            if (candidates.Count == 0)
            {
                candidates = FilterCandidates(phasePool, index, priorMoves, enforceRepeatLimit: false, enforceHeavyGap: false);
            }

            if (candidates.Count == 0)
            {
                // Last-resort fallback if every move in phase had weight 0.
                int fallbackIdx = intentsStream.NextBounded(phasePool.Count);
                return phasePool[fallbackIdx];
            }

            var weights = new int[candidates.Count];
            for (int i = 0; i < candidates.Count; i++)
            {
                weights[i] = candidates[i].Weight;
            }

            int chosenIdx = intentsStream.NextWeighted(weights);
            return candidates[chosenIdx];
        }

        private static List<MonsterMoveDef> GetPhasePool(MonsterDef def, int index)
        {
            var pool = new List<MonsterMoveDef>(def.Moves.Count);
            BossPhaseDef activePhase = null;
            if (def.BossPhases != null)
            {
                for (int i = 0; i < def.BossPhases.Count; i++)
                {
                    BossPhaseDef p = def.BossPhases[i];
                    if (index >= p.StartIndex && (p.EndIndexInclusive < 0 || index <= p.EndIndexInclusive))
                    {
                        activePhase = p;
                        break;
                    }
                }
            }

            if (activePhase == null || activePhase.AllowedMoveIds == null || activePhase.AllowedMoveIds.Count == 0)
            {
                for (int i = 0; i < def.Moves.Count; i++)
                {
                    pool.Add(def.Moves[i]);
                }
                return pool;
            }

            for (int i = 0; i < def.Moves.Count; i++)
            {
                MonsterMoveDef move = def.Moves[i];
                for (int j = 0; j < activePhase.AllowedMoveIds.Count; j++)
                {
                    if (string.Equals(move.Id, activePhase.AllowedMoveIds[j], StringComparison.Ordinal))
                    {
                        pool.Add(move);
                        break;
                    }
                }
            }
            return pool;
        }

        private static List<MonsterMoveDef> FilterCandidates(
            List<MonsterMoveDef> pool,
            int index,
            MonsterMoveDef[] priorMoves,
            bool enforceRepeatLimit,
            bool enforceHeavyGap)
        {
            var list = new List<MonsterMoveDef>(pool.Count);
            for (int i = 0; i < pool.Count; i++)
            {
                MonsterMoveDef move = pool[i];
                if (move.Weight <= 0)
                {
                    continue;
                }

                if (enforceRepeatLimit && ViolatesRepeatLimit(move, index, priorMoves))
                {
                    continue;
                }

                if (enforceHeavyGap && move.Heavy && ViolatesHeavyGap(index, priorMoves))
                {
                    continue;
                }

                list.Add(move);
            }
            return list;
        }

        private static bool ViolatesRepeatLimit(MonsterMoveDef move, int index, MonsterMoveDef[] priorMoves)
        {
            if (index < RepeatLimit)
            {
                return false;
            }

            for (int k = 1; k <= RepeatLimit; k++)
            {
                MonsterMoveDef prev = priorMoves[index - k];
                if (prev == null || !string.Equals(prev.Id, move.Id, StringComparison.Ordinal))
                {
                    return false;
                }
            }
            return true;
        }

        private static bool ViolatesHeavyGap(int index, MonsterMoveDef[] priorMoves)
        {
            int start = Math.Max(0, index - HeavyGap);
            for (int i = start; i < index; i++)
            {
                if (priorMoves[i] != null && priorMoves[i].Heavy)
                {
                    return true;
                }
            }
            return false;
        }

        private static int ResolveBoundHeroId(
            MonsterMoveDef move,
            int targetRoll,
            int index,
            MonsterMoveDef[] chosenMoves,
            BoundIntentEntry[] entries,
            IReadOnlyList<HeroBaselineInput> sortedHeroes)
        {
            switch (move.TargetMode)
            {
                case MoveTargetMode.None:
                case MoveTargetMode.AllHeroes:
                    return -1;
                case MoveTargetMode.RandomHero:
                    return sortedHeroes[targetRoll].Slot + 1;
                case MoveTargetMode.SameAsMove:
                    if (!string.IsNullOrEmpty(move.SameAsMoveId))
                    {
                        for (int j = index - 1; j >= 0; j--)
                        {
                            if (chosenMoves[j] != null &&
                                string.Equals(chosenMoves[j].Id, move.SameAsMoveId, StringComparison.Ordinal))
                            {
                                return entries[j].BoundHeroInstanceId;
                            }
                        }
                    }
                    return sortedHeroes[targetRoll].Slot + 1;
                default:
                    return -1;
            }
        }

        private static MonsterMoveDef FindMoveById(MonsterDef def, string moveId)
        {
            for (int i = 0; i < def.Moves.Count; i++)
            {
                if (string.Equals(def.Moves[i].Id, moveId, StringComparison.Ordinal))
                {
                    return def.Moves[i];
                }
            }
            return null;
        }

        private static int ScaleStat(int baseValue, int permille, int minimum)
        {
            if (baseValue <= 0)
            {
                return minimum;
            }
            long scaled = ((long)baseValue * permille + 500L) / 1000L;
            if (scaled < minimum)
            {
                return minimum;
            }
            if (scaled > int.MaxValue)
            {
                return int.MaxValue;
            }
            return (int)scaled;
        }
    }
}
