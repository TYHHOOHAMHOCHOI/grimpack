using UnityEngine;
using Project05.Core;
using Project05.Data;

namespace Project05.Combat
{
    public struct MonsterIntent
    {
        public string MoveName;
        public IntentType IntentType;
        public int TargetHeroIndex;
        public string TargetHeroName;
        public int DamageAmount;
        public int ShieldAmount;
        public StatusType StatusType;
        public int StatusStacks;
        public int SpeedBonus;

        public string GetSummaryText()
        {
            switch (IntentType)
            {
                case IntentType.Attack:
                    return $"⚔ Đánh [{TargetHeroName}]: {DamageAmount} ST";
                case IntentType.HeavyBleedAttack:
                    return $"🩸 Chém [{TargetHeroName}]: {DamageAmount} ST + {StatusStacks} Chảy máu";
                case IntentType.PoisonSpit:
                    return $"☣ Phun Độc [{TargetHeroName}]: {DamageAmount} ST + {StatusStacks} Độc";
                case IntentType.StunCurse:
                    return $"⚡ Gây Choáng [{TargetHeroName}]: {DamageAmount} ST + {StatusStacks} Choáng";
                case IntentType.DefendSelf:
                    return $"🛡 Phòng Thủ: +{ShieldAmount} Giáp";
                case IntentType.EnrageBuff:
                    return $"🔥 Cuồng Nộ: +{ShieldAmount} Giáp & +{SpeedBonus} Tốc độ";
                case IntentType.CleaveAll:
                    return StatusStacks > 0
                        ? $"💥 Quét Toàn Đội: {DamageAmount} ST + {StatusStacks} {StatusEffectSystem.GetStatusVietnameseName(StatusType)}"
                        : $"💥 Quét Toàn Đội: {DamageAmount} ST";
                default:
                    return MoveName;
            }
        }
    }

    public class MonsterCombatant : Combatant
    {
        public MonsterDefinitionSO Definition { get; private set; }
        public float DifficultyMultiplier { get; private set; }
        public MonsterIntent CurrentIntent { get; set; }
        public int MoveCursor { get; set; }

        public override bool IsHero => false;

        public MonsterCombatant(MonsterDefinitionSO definition, float difficultyMultiplier = 1f)
        {
            Definition = definition;
            DisplayName = definition.MonsterName;
            DifficultyMultiplier = Mathf.Max(0.5f, difficultyMultiplier);

            MaxHP = Mathf.Max(10, Mathf.RoundToInt(definition.MaxHP * DifficultyMultiplier));
            CurrentHP = MaxHP;
            BaseSpeed = Mathf.Max(5, Mathf.RoundToInt(definition.BaseSpeed * Mathf.Lerp(1f, DifficultyMultiplier, 0.25f)));
            MoveCursor = 0;
        }
    }
}
