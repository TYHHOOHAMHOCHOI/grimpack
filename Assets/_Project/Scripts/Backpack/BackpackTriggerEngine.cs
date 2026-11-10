using System.Collections.Generic;
using UnityEngine;
using Project05.Core;
using Project05.Data;
using Project05.Combat;

namespace Project05.Backpack
{
    public struct ItemTriggerRecord
    {
        public ItemData Item;
        public int DamageDealt;
        public int ShieldGained;
        public int HealGained;
        public int PoisonApplied;
        public int BleedApplied;
        public int StunApplied;
    }

    public class BackpackTurnResult
    {
        public HeroCombatant ActingHero;
        public readonly List<ItemTriggerRecord> TriggeredItems = new List<ItemTriggerRecord>(8);
        public readonly List<ItemData> CooldownTickedItems = new List<ItemData>(8);
        public int TotalDamageDealt;
        public int TotalShieldGained;
        public int TotalHealGained;
        public int BleedSelfDamageTaken;

        public void Clear()
        {
            ActingHero = null;
            TriggeredItems.Clear();
            CooldownTickedItems.Clear();
            TotalDamageDealt = 0;
            TotalShieldGained = 0;
            TotalHealGained = 0;
            BleedSelfDamageTaken = 0;
        }
    }

    /// <summary>
    /// Auto-triggers all ready items in a Hero's personal BackpackGrid when that Hero's turn arrives.
    /// Manages turn-based item cooldowns and applies SynergyEvaluator bonuses + Run Buffs.
    /// </summary>
    public static class BackpackTriggerEngine
    {
        public static void ResetCooldownsForCombatStart(BackpackGrid grid)
        {
            if (grid == null) return;
            IReadOnlyList<ItemData> items = grid.PlacedItems;
            for (int i = 0; i < items.Count; i++)
            {
                items[i].CurrentCooldown = 0;
                items[i].TriggerCountThisCombat = 0;
            }
        }

        public static BackpackTurnResult TriggerHeroBackpack(
            HeroCombatant hero,
            IReadOnlyList<HeroCombatant> party,
            IReadOnlyList<MonsterCombatant> enemies,
            IReadOnlyList<BuffDefinitionSO> activeRunBuffs = null)
        {
            BackpackTurnResult result = new BackpackTurnResult();
            TriggerHeroBackpackInto(hero, party, enemies, activeRunBuffs, result);
            return result;
        }

        public static void TriggerHeroBackpackInto(
            HeroCombatant hero,
            IReadOnlyList<HeroCombatant> party,
            IReadOnlyList<MonsterCombatant> enemies,
            IReadOnlyList<BuffDefinitionSO> activeRunBuffs,
            BackpackTurnResult result)
        {
            result.Clear();
            result.ActingHero = hero;
            if (hero == null || !hero.IsAlive || hero.PersonalBackpack == null) return;

            hero.RecalculateSynergies(activeRunBuffs);
            BackpackSynergyReport synergyReport = hero.LatestSynergyReport;
            IReadOnlyList<ItemData> placedItems = hero.PersonalBackpack.PlacedItems;

            bool performedOffensiveAction = false;

            for (int i = 0; i < placedItems.Count; i++)
            {
                ItemData item = placedItems[i];
                if (item == null || item.Definition == null) continue;

                // Passive-only items (CooldownTurns <= 0) never enter active cooldown
                if (item.Definition.CooldownTurns <= 0) continue;

                // If item is currently on cooldown from an earlier turn, decrement by 1 turn and rest
                if (item.CurrentCooldown > 0)
                {
                    item.CurrentCooldown--;
                    result.CooldownTickedItems.Add(item);
                    continue;
                }

                // Item is ready to trigger!
                if (!synergyReport.ItemStats.TryGetValue(item, out EvaluatedItemStats stats))
                {
                    continue;
                }

                ItemTriggerRecord record = new ItemTriggerRecord { Item = item };

                // 1. Shield Gain
                if (stats.TotalShield > 0)
                {
                    hero.AddShield(stats.TotalShield);
                    record.ShieldGained = stats.TotalShield;
                    result.TotalShieldGained += stats.TotalShield;
                }

                // 2. Heal (Self or Lowest HP Ally; +50% emergency potency when target HP < 50%)
                if (stats.TotalHeal > 0)
                {
                    HeroCombatant healTarget = item.Definition.HealLowestAlly
                        ? FindLowestHpLivingAlly(party, hero)
                        : hero;
                    if (healTarget != null && healTarget.IsAlive)
                    {
                        int healAmount = stats.TotalHeal;
                        if (healTarget.CurrentHP * 2 < healTarget.MaxHP)
                        {
                            healAmount = Mathf.RoundToInt(healAmount * 1.5f);
                        }
                        int healed = healTarget.Heal(healAmount);
                        record.HealGained = healed;
                        result.TotalHealGained += healed;
                    }
                }

                // 3. Offensive Effects (Physical / Magic Damage & Statuses)
                int rawAttack = stats.TotalPhysicalDamage + stats.TotalMagicDamage;
                bool hasOffensivePayload = rawAttack > 0 || stats.TotalPoison > 0 || stats.TotalBleed > 0 || stats.TotalStun > 0;

                if (hasOffensivePayload && enemies != null)
                {
                    if (item.Definition.TargetAllEnemies)
                    {
                        for (int e = 0; e < enemies.Count; e++)
                        {
                            MonsterCombatant enemy = enemies[e];
                            if (enemy == null || !enemy.IsAlive) continue;
                            performedOffensiveAction = true;
                            ApplyOffensivePayloadToTarget(enemy, rawAttack, stats, ref record, result);
                        }
                    }
                    else
                    {
                        MonsterCombatant primaryEnemy = FindFirstLivingEnemy(enemies);
                        if (primaryEnemy != null)
                        {
                            performedOffensiveAction = true;
                            ApplyOffensivePayloadToTarget(primaryEnemy, rawAttack, stats, ref record, result);
                        }
                    }
                }

                // Reset cooldown: if EffectiveCooldownTurns == 1, sets CurrentCooldown = 0 (triggers again next turn).
                // If EffectiveCooldownTurns == 2, sets CurrentCooldown = 1 (rests next turn, triggers on the turn after).
                int effectiveCd = Mathf.Max(1, stats.EffectiveCooldownTurns);
                item.CurrentCooldown = effectiveCd - 1;
                item.TriggerCountThisCombat++;

                result.TriggeredItems.Add(record);
            }

            // If the hero performed at least one offensive action while bleeding, trigger Bleed damage
            if (performedOffensiveAction && hero.BleedStacks > 0)
            {
                result.BleedSelfDamageTaken = StatusEffectSystem.ProcessBleedOnAction(hero);
            }
        }

        private static void ApplyOffensivePayloadToTarget(
            MonsterCombatant target,
            int rawAttack,
            EvaluatedItemStats stats,
            ref ItemTriggerRecord record,
            BackpackTurnResult result)
        {
            if (rawAttack > 0)
            {
                int hpLost = StatusEffectSystem.ApplyStandardDamage(target, rawAttack, out int blocked);
                int totalDealt = hpLost + blocked;
                record.DamageDealt += totalDealt;
                result.TotalDamageDealt += totalDealt;
            }

            if (stats.TotalPoison > 0)
            {
                StatusEffectSystem.ApplyStatus(target, StatusType.Poison, stats.TotalPoison);
                record.PoisonApplied += stats.TotalPoison;
            }

            if (stats.TotalBleed > 0)
            {
                StatusEffectSystem.ApplyStatus(target, StatusType.Bleed, stats.TotalBleed);
                record.BleedApplied += stats.TotalBleed;
            }

            if (stats.TotalStun > 0)
            {
                StatusEffectSystem.ApplyStatus(target, StatusType.Stun, stats.TotalStun);
                record.StunApplied += stats.TotalStun;
            }
        }

        private static MonsterCombatant FindFirstLivingEnemy(IReadOnlyList<MonsterCombatant> enemies)
        {
            if (enemies == null) return null;
            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i] != null && enemies[i].IsAlive)
                {
                    return enemies[i];
                }
            }
            return null;
        }

        private static HeroCombatant FindLowestHpLivingAlly(IReadOnlyList<HeroCombatant> party, HeroCombatant fallback)
        {
            if (party == null || party.Count == 0) return fallback;
            HeroCombatant lowest = fallback;
            float lowestRatio = fallback != null && fallback.MaxHP > 0
                ? (float)fallback.CurrentHP / fallback.MaxHP
                : 1f;

            for (int i = 0; i < party.Count; i++)
            {
                HeroCombatant ally = party[i];
                if (ally == null || !ally.IsAlive || ally.MaxHP <= 0) continue;
                float ratio = (float)ally.CurrentHP / ally.MaxHP;
                if (ratio < lowestRatio)
                {
                    lowestRatio = ratio;
                    lowest = ally;
                }
            }

            return lowest ?? fallback;
        }
    }
}
