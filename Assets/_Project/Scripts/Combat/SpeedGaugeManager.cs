using System.Collections.Generic;
using UnityEngine;

namespace Project05.Combat
{
    /// <summary>
    /// Controls the Turn-Based Initiative / Speed Gauge system.
    /// Each living combatant fills their ActionGauge (0..100) based on their EffectiveSpeed.
    /// Whoever reaches >= 100 first takes their turn immediately.
    /// </summary>
    public class SpeedGaugeManager
    {
        public const float TurnThreshold = 100f;

        private readonly List<Combatant> _allCombatants = new List<Combatant>(8);

        public void GatherLivingCombatants(
            IReadOnlyList<HeroCombatant> heroes,
            IReadOnlyList<MonsterCombatant> monsters)
        {
            _allCombatants.Clear();
            if (heroes != null)
            {
                for (int i = 0; i < heroes.Count; i++)
                {
                    if (heroes[i] != null && heroes[i].IsAlive)
                    {
                        _allCombatants.Add(heroes[i]);
                    }
                }
            }

            if (monsters != null)
            {
                for (int i = 0; i < monsters.Count; i++)
                {
                    if (monsters[i] != null && monsters[i].IsAlive)
                    {
                        _allCombatants.Add(monsters[i]);
                    }
                }
            }
        }

        /// <summary>
        /// Ticks gauges smoothly over time. If one or more combatants reach >= 100, returns the highest-priority ready combatant.
        /// </summary>
        public Combatant TickGauges(
            IReadOnlyList<HeroCombatant> heroes,
            IReadOnlyList<MonsterCombatant> monsters,
            float deltaTime,
            float gaugeSpeedScale = 2.2f)
        {
            GatherLivingCombatants(heroes, monsters);
            if (_allCombatants.Count == 0) return null;

            Combatant alreadyReady = FindBestReadyCombatant();
            if (alreadyReady != null) return alreadyReady;

            if (deltaTime <= 0f) return null;

            for (int i = 0; i < _allCombatants.Count; i++)
            {
                Combatant c = _allCombatants[i];
                c.ActionGauge += c.GetEffectiveSpeed() * deltaTime * gaugeSpeedScale;
            }

            return FindBestReadyCombatant();
        }

        /// <summary>
        /// Advances gauges deterministically to the exact moment the next combatant reaches TurnThreshold (100).
        /// Ideal for instant step-by-step turns and automated unit tests.
        /// </summary>
        public Combatant AdvanceToNextTurnInstant(
            IReadOnlyList<HeroCombatant> heroes,
            IReadOnlyList<MonsterCombatant> monsters)
        {
            GatherLivingCombatants(heroes, monsters);
            if (_allCombatants.Count == 0) return null;

            Combatant alreadyReady = FindBestReadyCombatant();
            if (alreadyReady != null) return alreadyReady;

            float minTimeNeeded = float.MaxValue;
            for (int i = 0; i < _allCombatants.Count; i++)
            {
                Combatant c = _allCombatants[i];
                int spd = Mathf.Max(1, c.GetEffectiveSpeed());
                float remaining = Mathf.Max(0f, TurnThreshold - c.ActionGauge);
                float t = remaining / spd;
                if (t < minTimeNeeded)
                {
                    minTimeNeeded = t;
                }
            }

            if (minTimeNeeded < float.MaxValue && minTimeNeeded > 0f)
            {
                for (int i = 0; i < _allCombatants.Count; i++)
                {
                    Combatant c = _allCombatants[i];
                    c.ActionGauge += c.GetEffectiveSpeed() * minTimeNeeded;
                }
            }

            // Ensure at least one reaches threshold despite floating point rounding
            Combatant ready = FindBestReadyCombatant();
            if (ready == null && _allCombatants.Count > 0)
            {
                Combatant highest = _allCombatants[0];
                for (int i = 1; i < _allCombatants.Count; i++)
                {
                    if (_allCombatants[i].ActionGauge > highest.ActionGauge)
                    {
                        highest = _allCombatants[i];
                    }
                }
                highest.ActionGauge = TurnThreshold;
                ready = highest;
            }

            return ready;
        }

        public static void ConsumeTurnGauge(Combatant actingUnit)
        {
            if (actingUnit == null) return;
            actingUnit.ActionGauge = Mathf.Max(0f, actingUnit.ActionGauge - TurnThreshold);
        }

        private Combatant FindBestReadyCombatant()
        {
            Combatant best = null;
            for (int i = 0; i < _allCombatants.Count; i++)
            {
                Combatant c = _allCombatants[i];
                if (c.ActionGauge + 0.001f < TurnThreshold) continue;

                if (best == null)
                {
                    best = c;
                }
                else if (c.ActionGauge > best.ActionGauge + 0.01f)
                {
                    best = c;
                }
                else if (Mathf.Abs(c.ActionGauge - best.ActionGauge) <= 0.01f)
                {
                    // Tie-breaker: higher EffectiveSpeed, then Hero before Monster
                    int spdC = c.GetEffectiveSpeed();
                    int spdBest = best.GetEffectiveSpeed();
                    if (spdC > spdBest || (spdC == spdBest && c.IsHero && !best.IsHero))
                    {
                        best = c;
                    }
                }
            }

            return best;
        }
    }
}
