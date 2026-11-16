using System;
using System.Collections.Generic;

namespace Project05.CombatCore
{
    internal sealed class SchedulerUnitState
    {
        public UnitRef Unit;
        public int Gauge;
        public int EffectiveSpeed;
        public int ActionOrdinal;
        public int IntentIndex;
        public IReadOnlyList<BoundIntentEntry> IntentSequence;
        public bool IsAlive;

        public SchedulerUnitState Clone()
        {
            return new SchedulerUnitState
            {
                Unit = Unit,
                Gauge = Gauge,
                EffectiveSpeed = EffectiveSpeed,
                ActionOrdinal = ActionOrdinal,
                IntentIndex = IntentIndex,
                IntentSequence = IntentSequence,
                IsAlive = IsAlive
            };
        }
    }

    /// <summary>
    /// Pure integer turn-order gauge scheduler and forecast engine (T=1000, V_max=200, G_max=1199).
    /// </summary>
    public static class TurnOrderScheduler
    {
        public const int GaugeThreshold = 1000;
        public const int MaxEffectiveSpeed = 200;
        public const int MaxGauge = 1199;

        public static int EffectiveSpeed(int baseSpeed, int flatBonus, int percentPermille = 0)
        {
            long raw = (long)baseSpeed + flatBonus;
            if (raw <= 0)
            {
                return 1;
            }

            int clampedPercent = percentPermille < -1000 ? -1000 : percentPermille;
            long scaled = (raw * (1000L + clampedPercent) + 500L) / 1000L;
            if (scaled < 1L)
            {
                return 1;
            }
            if (scaled > MaxEffectiveSpeed)
            {
                return MaxEffectiveSpeed;
            }
            return (int)scaled;
        }

        internal static int ComputeHeroSpeedBonus(ArrangementGridView grid, GameCatalog catalog)
        {
            if (grid == null || catalog == null || grid.Items == null)
            {
                return 0;
            }

            int bonus = 0;
            for (int i = 0; i < grid.Items.Count; i++)
            {
                ItemDef def = catalog.FindItem(grid.Items[i].DefinitionId);
                if (def != null)
                {
                    bonus += def.SpeedBonus;
                }
            }
            return bonus;
        }

        internal static int ComputeHeroHpBonus(ArrangementGridView grid, GameCatalog catalog)
        {
            if (grid == null || catalog == null || grid.Items == null)
            {
                return 0;
            }

            int bonus = 0;
            for (int i = 0; i < grid.Items.Count; i++)
            {
                ItemDef def = catalog.FindItem(grid.Items[i].DefinitionId);
                if (def != null)
                {
                    bonus += def.HpBonus;
                }
            }
            return bonus;
        }

        public static IReadOnlyList<TurnForecastEntry> ForecastPlanning(
            IReadOnlyList<HeroBaselineInput> heroes,
            IReadOnlyList<BoundEnemyState> boundEnemies,
            GameCatalog catalog,
            int count = 6)
        {
            if (count <= 0)
            {
                return Array.Empty<TurnForecastEntry>();
            }

            var units = new List<SchedulerUnitState>();

            if (heroes != null)
            {
                var sortedHeroes = new List<HeroBaselineInput>(heroes);
                sortedHeroes.Sort((a, b) => a.Slot.CompareTo(b.Slot));
                for (int i = 0; i < sortedHeroes.Count; i++)
                {
                    HeroBaselineInput h = sortedHeroes[i];
                    if (h == null || h.Definition == null)
                    {
                        continue;
                    }

                    int speedBonus = ComputeHeroSpeedBonus(h.FrozenGrid, catalog);
                    int effSpeed = EffectiveSpeed(h.Definition.BaseSpeed, speedBonus, 0);
                    int hpBonus = h.PermanentHpBonus + ComputeHeroHpBonus(h.FrozenGrid, catalog);
                    int maxHp = Math.Max(1, h.Definition.BaseMaxHp + hpBonus);

                    units.Add(new SchedulerUnitState
                    {
                        Unit = new UnitRef(UnitSide.Hero, h.Slot, h.Slot + 1),
                        Gauge = 0,
                        EffectiveSpeed = effSpeed,
                        ActionOrdinal = 0,
                        IntentIndex = 0,
                        IntentSequence = Array.Empty<BoundIntentEntry>(),
                        IsAlive = maxHp > 0
                    });
                }
            }

            if (boundEnemies != null)
            {
                var sortedEnemies = new List<BoundEnemyState>(boundEnemies);
                sortedEnemies.Sort((a, b) => a.Slot.CompareTo(b.Slot));
                for (int i = 0; i < sortedEnemies.Count; i++)
                {
                    BoundEnemyState e = sortedEnemies[i];
                    if (e == null)
                    {
                        continue;
                    }

                    units.Add(new SchedulerUnitState
                    {
                        Unit = new UnitRef(UnitSide.Enemy, e.Slot, e.EnemyInstanceId),
                        Gauge = 0,
                        EffectiveSpeed = Math.Max(1, Math.Min(MaxEffectiveSpeed, e.EffectiveSpeed)),
                        ActionOrdinal = 0,
                        IntentIndex = 0,
                        IntentSequence = e.Sequence ?? Array.Empty<BoundIntentEntry>(),
                        IsAlive = e.ScaledMaxHp > 0
                    });
                }
            }

            return ForecastFromStates(units, count);
        }

        internal static IReadOnlyList<TurnForecastEntry> ForecastFromStates(
            IReadOnlyList<SchedulerUnitState> sourceUnits,
            int count)
        {
            if (count <= 0 || sourceUnits == null || sourceUnits.Count == 0)
            {
                return Array.Empty<TurnForecastEntry>();
            }

            var sim = new List<SchedulerUnitState>(sourceUnits.Count);
            for (int i = 0; i < sourceUnits.Count; i++)
            {
                if (sourceUnits[i] != null && sourceUnits[i].IsAlive && sourceUnits[i].EffectiveSpeed > 0)
                {
                    sim.Add(sourceUnits[i].Clone());
                }
            }

            if (sim.Count == 0)
            {
                return Array.Empty<TurnForecastEntry>();
            }

            var result = new List<TurnForecastEntry>(count);
            for (int g = 0; g < count; g++)
            {
                if (!TryAdvanceNextActor(sim, out int winnerIndex, out int deltaTime, out _, out _))
                {
                    break;
                }

                SchedulerUnitState winner = sim[winnerIndex];
                winner.ActionOrdinal += 1;

                MoveGlyphKind glyph = MoveGlyphKind.Attack;
                if (winner.Unit.Side == UnitSide.Enemy && winner.IntentSequence != null && winner.IntentSequence.Count > 0)
                {
                    int seqIdx = winner.IntentIndex % winner.IntentSequence.Count;
                    glyph = winner.IntentSequence[seqIdx].Glyph;
                    winner.IntentIndex += 1;
                }

                result.Add(new TurnForecastEntry(
                    winner.Unit,
                    deltaTime,
                    g,
                    winner.ActionOrdinal,
                    glyph));
            }

            return result.ToArray();
        }

        internal static bool TryAdvanceNextActor(
            IList<SchedulerUnitState> units,
            out int winnerIndex,
            out int deltaTime,
            out int winnerGaugeBefore,
            out int winnerGaugeAfter)
        {
            winnerIndex = -1;
            deltaTime = 0;
            winnerGaugeBefore = 0;
            winnerGaugeAfter = 0;

            if (units == null || units.Count == 0)
            {
                return false;
            }

            int minDelta = int.MaxValue;
            bool foundEligible = false;

            for (int i = 0; i < units.Count; i++)
            {
                SchedulerUnitState u = units[i];
                if (u == null || !u.IsAlive || u.EffectiveSpeed <= 0)
                {
                    continue;
                }

                foundEligible = true;
                int ticksNeeded;
                if (u.Gauge >= GaugeThreshold)
                {
                    ticksNeeded = 0;
                }
                else
                {
                    int deficit = GaugeThreshold - u.Gauge;
                    ticksNeeded = (deficit + u.EffectiveSpeed - 1) / u.EffectiveSpeed;
                }

                if (ticksNeeded < minDelta)
                {
                    minDelta = ticksNeeded;
                }
            }

            if (!foundEligible)
            {
                return false;
            }

            deltaTime = minDelta;

            // Advance all alive eligible units' gauges simultaneously, clamping to MaxGauge.
            int[] oldGauges = new int[units.Count];
            for (int i = 0; i < units.Count; i++)
            {
                SchedulerUnitState u = units[i];
                if (u == null || !u.IsAlive || u.EffectiveSpeed <= 0)
                {
                    continue;
                }

                oldGauges[i] = u.Gauge;
                if (deltaTime > 0)
                {
                    long nextGauge = (long)u.Gauge + (long)deltaTime * u.EffectiveSpeed;
                    u.Gauge = nextGauge > MaxGauge ? MaxGauge : (int)nextGauge;
                }
            }

            // Pick winner among eligible units with Gauge >= GaugeThreshold.
            for (int i = 0; i < units.Count; i++)
            {
                SchedulerUnitState candidate = units[i];
                if (candidate == null || !candidate.IsAlive || candidate.EffectiveSpeed <= 0)
                {
                    continue;
                }
                if (candidate.Gauge < GaugeThreshold)
                {
                    continue;
                }

                if (winnerIndex < 0 || IsHigherPriority(candidate, units[winnerIndex]))
                {
                    winnerIndex = i;
                }
            }

            if (winnerIndex < 0)
            {
                return false;
            }

            SchedulerUnitState winner = units[winnerIndex];
            winnerGaugeBefore = oldGauges[winnerIndex];
            winnerGaugeAfter = winner.Gauge - GaugeThreshold;
            if (winnerGaugeAfter < 0)
            {
                winnerGaugeAfter = 0;
            }
            winner.Gauge = winnerGaugeAfter;
            return true;
        }

        private static bool IsHigherPriority(SchedulerUnitState a, SchedulerUnitState b)
        {
            if (a.Gauge != b.Gauge)
            {
                return a.Gauge > b.Gauge;
            }
            if (a.EffectiveSpeed != b.EffectiveSpeed)
            {
                return a.EffectiveSpeed > b.EffectiveSpeed;
            }
            if (a.Unit.Side != b.Unit.Side)
            {
                return (int)a.Unit.Side < (int)b.Unit.Side;
            }
            return a.Unit.Slot < b.Unit.Slot;
        }
    }
}
