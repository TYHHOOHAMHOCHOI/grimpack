using System.Collections.Generic;
using UnityEngine;
using Project05.Core;
using Project05.Data;

namespace Project05.Combat
{
    /// <summary>
    /// Selects and executes pre-announced Monster Intents (Slay the Spire style).
    /// </summary>
    public static class MonsterIntentController
    {
        public static MonsterIntent RollNextIntent(MonsterCombatant monster, IReadOnlyList<HeroCombatant> party)
        {
            if (monster == null || monster.Definition == null || monster.Definition.Moveset == null || monster.Definition.Moveset.Count == 0)
            {
                return new MonsterIntent
                {
                    MoveName = "Tấn Công",
                    IntentType = IntentType.Attack,
                    TargetHeroIndex = 0,
                    TargetHeroName = "Tiền Tuyến",
                    DamageAmount = 10
                };
            }

            List<MonsterMoveDefinition> moves = monster.Definition.Moveset;
            MonsterMoveDefinition move = moves[monster.MoveCursor % moves.Count];
            monster.MoveCursor++;

            int targetIndex = SelectTargetHeroIndex(party, move.TargetsBacklineFirst);
            string targetName = (party != null && targetIndex >= 0 && targetIndex < party.Count && party[targetIndex] != null)
                ? party[targetIndex].DisplayName
                : "Tiền Tuyến";

            int scaledDmg = move.BaseDamage > 0
                ? Mathf.Max(1, Mathf.RoundToInt(move.BaseDamage * monster.DifficultyMultiplier))
                : 0;
            int scaledShield = move.ShieldGain > 0
                ? Mathf.Max(1, Mathf.RoundToInt(move.ShieldGain * monster.DifficultyMultiplier))
                : 0;

            MonsterIntent intent = new MonsterIntent
            {
                MoveName = move.MoveName,
                IntentType = move.IntentType,
                TargetHeroIndex = targetIndex,
                TargetHeroName = targetName,
                DamageAmount = scaledDmg,
                ShieldAmount = scaledShield,
                StatusType = move.StatusToApply,
                StatusStacks = move.StatusStacks,
                SpeedBonus = move.SpeedBonus
            };

            monster.CurrentIntent = intent;
            return intent;
        }

        public static string ExecuteIntent(MonsterCombatant monster, IReadOnlyList<HeroCombatant> party)
        {
            if (monster == null || !monster.IsAlive) return string.Empty;

            MonsterIntent intent = monster.CurrentIntent;
            bool performedOffensive = false;
            string logMessage;

            switch (intent.IntentType)
            {
                case IntentType.DefendSelf:
                    monster.AddShield(intent.ShieldAmount);
                    logMessage = $"{monster.DisplayName} dùng [{intent.MoveName}] tạo +{intent.ShieldAmount} Giáp!";
                    break;

                case IntentType.EnrageBuff:
                    monster.AddShield(intent.ShieldAmount);
                    monster.BonusCombatSpeed += intent.SpeedBonus;
                    logMessage = $"{monster.DisplayName} gầm rú [{intent.MoveName}]: +{intent.ShieldAmount} Giáp & +{intent.SpeedBonus} Tốc độ!";
                    break;

                case IntentType.CleaveAll:
                {
                    performedOffensive = true;
                    int totalDmg = 0;
                    if (party != null)
                    {
                        for (int i = 0; i < party.Count; i++)
                        {
                            HeroCombatant h = party[i];
                            if (h == null || !h.IsAlive) continue;
                            totalDmg += StatusEffectSystem.ApplyStandardDamage(h, intent.DamageAmount, out _);
                            if (intent.StatusStacks > 0)
                            {
                                StatusEffectSystem.ApplyStatus(h, intent.StatusType, intent.StatusStacks);
                            }
                        }
                    }
                    logMessage = $"{monster.DisplayName} tung [{intent.MoveName}] quét toàn đội gây tổng {totalDmg} sát thương HP!";
                    break;
                }

                default:
                {
                    performedOffensive = true;
                    HeroCombatant target = ResolveTargetHero(party, intent.TargetHeroIndex);
                    if (target == null)
                    {
                        logMessage = $"{monster.DisplayName} định tấn công nhưng không còn mục tiêu.";
                        break;
                    }

                    int hpLost = StatusEffectSystem.ApplyStandardDamage(target, intent.DamageAmount, out int blocked);
                    if (intent.StatusStacks > 0)
                    {
                        StatusEffectSystem.ApplyStatus(target, intent.StatusType, intent.StatusStacks);
                    }

                    string statusNote = intent.StatusStacks > 0
                        ? $" & gây +{intent.StatusStacks} {StatusEffectSystem.GetStatusVietnameseName(intent.StatusType)}"
                        : string.Empty;
                    logMessage = $"{monster.DisplayName} dùng [{intent.MoveName}] vào {target.DisplayName}: mất {hpLost} HP (Giáp chặn {blocked}){statusNote}!";
                    break;
                }
            }

            if (performedOffensive && monster.BleedStacks > 0)
            {
                int bleedDmg = StatusEffectSystem.ProcessBleedOnAction(monster);
                logMessage += $" (Mất {bleedDmg} HP do Chảy máu!)";
            }

            return logMessage;
        }

        public static void RefreshIntentTargetIfDead(MonsterCombatant monster, IReadOnlyList<HeroCombatant> party)
        {
            if (monster == null || !monster.IsAlive || party == null || party.Count == 0) return;

            MonsterIntent intent = monster.CurrentIntent;
            if (intent.IntentType == IntentType.DefendSelf ||
                intent.IntentType == IntentType.EnrageBuff ||
                intent.IntentType == IntentType.CleaveAll)
            {
                return;
            }

            int idx = intent.TargetHeroIndex;
            if (idx >= 0 && idx < party.Count && party[idx] != null && party[idx].IsAlive)
            {
                return;
            }

            for (int i = 0; i < party.Count; i++)
            {
                if (party[i] != null && party[i].IsAlive)
                {
                    intent.TargetHeroIndex = i;
                    intent.TargetHeroName = party[i].DisplayName;
                    monster.CurrentIntent = intent;
                    return;
                }
            }
        }

        private static HeroCombatant ResolveTargetHero(IReadOnlyList<HeroCombatant> party, int preferredIndex)
        {
            if (party == null || party.Count == 0) return null;
            if (preferredIndex >= 0 && preferredIndex < party.Count && party[preferredIndex] != null && party[preferredIndex].IsAlive)
            {
                return party[preferredIndex];
            }

            for (int i = 0; i < party.Count; i++)
            {
                if (party[i] != null && party[i].IsAlive) return party[i];
            }
            return null;
        }

        private static int SelectTargetHeroIndex(IReadOnlyList<HeroCombatant> party, bool backlineFirst)
        {
            if (party == null || party.Count == 0) return 0;

            if (backlineFirst)
            {
                for (int i = party.Count - 1; i >= 0; i--)
                {
                    if (party[i] != null && party[i].IsAlive) return i;
                }
            }
            else
            {
                for (int i = 0; i < party.Count; i++)
                {
                    if (party[i] != null && party[i].IsAlive) return i;
                }
            }

            return 0;
        }
    }
}
