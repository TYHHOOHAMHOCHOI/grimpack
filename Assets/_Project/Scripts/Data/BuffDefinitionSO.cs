using UnityEngine;
using Project05.Core;

namespace Project05.Data
{
    [CreateAssetMenu(fileName = "NewBuffDefinition", menuName = "Project05/Data/Buff Definition")]
    public class BuffDefinitionSO : ScriptableObject
    {
        public string BuffId;
        public string BuffName;
        [TextArea(2, 3)] public string Description;
        public CardTier Tier;

        [Header("Immediate & Persistent Run Modifiers")]
        public int BonusMaxHP;
        public int BonusSpeed;
        public int BonusWeaponDamage;
        public int BonusMagicDamage;
        public int BonusStartingShield;
        public int BonusPoisonPerTrigger;
        public int BonusBleedPerTrigger;
        public int BonusHealPerTrigger;
        public int CooldownReductionTurns;
        public int UnlockExtraBackpackSlots;
        public int ImmediateGoldGain;
        public int ImmediateHealPercent;

        public static BuffDefinitionSO CreateRuntime(
            string id,
            string buffName,
            string description,
            CardTier tier,
            int maxHp = 0,
            int speed = 0,
            int weaponDmg = 0,
            int magicDmg = 0,
            int startShield = 0,
            int poison = 0,
            int bleed = 0,
            int healBonus = 0,
            int cdReduction = 0,
            int unlockSlots = 0,
            int goldGain = 0,
            int healPct = 0)
        {
            BuffDefinitionSO def = CreateInstance<BuffDefinitionSO>();
            def.name = id;
            def.BuffId = id;
            def.BuffName = buffName;
            def.Description = description;
            def.Tier = tier;
            def.BonusMaxHP = maxHp;
            def.BonusSpeed = speed;
            def.BonusWeaponDamage = weaponDmg;
            def.BonusMagicDamage = magicDmg;
            def.BonusStartingShield = startShield;
            def.BonusPoisonPerTrigger = poison;
            def.BonusBleedPerTrigger = bleed;
            def.BonusHealPerTrigger = healBonus;
            def.CooldownReductionTurns = cdReduction;
            def.UnlockExtraBackpackSlots = unlockSlots;
            def.ImmediateGoldGain = goldGain;
            def.ImmediateHealPercent = healPct;
            return def;
        }
    }
}
