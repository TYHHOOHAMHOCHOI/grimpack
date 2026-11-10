using System;
using UnityEngine;
using Project05.Core;
using Project05.Backpack;

namespace Project05.Data
{
    [CreateAssetMenu(fileName = "NewItemDefinition", menuName = "Project05/Data/Item Definition")]
    public class ItemDefinitionSO : ScriptableObject
    {
        public string ItemId;
        public string DisplayName;
        [TextArea(2, 4)] public string Description;
        public ItemCategory Category;
        public CardTier RarityTier;
        public int GoldCost = 15;
        public string SpriteName;

        [Header("Shape & Synergy Stars")]
        public ItemShape Shape = new ItemShape();

        [Header("Turn Cooldown (0 = Passive Only, 1 = Every Turn, 2 = Every 2 Turns...)")]
        public int CooldownTurns = 1;

        [Header("Base Trigger Effects")]
        public int BasePhysicalDamage;
        public int BaseMagicDamage;
        public int BaseShield;
        public int BaseHeal;
        public int BasePoison;
        public int BaseBleed;
        public int BaseStun;
        public bool TargetAllEnemies;
        public bool HealLowestAlly;

        [Header("Passive Stat Modifiers (While in Hero Backpack)")]
        public int PassiveBonusSpeed;
        public int PassiveBonusMaxHp;

        [Header("Directional Star Synergy Effects (Applied to Items on Star Cells)")]
        public ItemCategory SynergyTargetCategory = ItemCategory.Weapon;
        public bool SynergyTargetsAnyCategory;
        public int StarBonusDamage;
        public int StarBonusShield;
        public int StarBonusHeal;
        public int StarBonusPoison;
        public int StarBonusBleed;
        public int StarCooldownReduction;
        public int StarBonusSpeed;

        public static ItemDefinitionSO CreateRuntime(
            string id,
            string displayName,
            string description,
            ItemCategory category,
            CardTier tier,
            int goldCost,
            string spriteName,
            ItemShape shape,
            int cooldownTurns,
            int physDmg = 0,
            int magicDmg = 0,
            int shield = 0,
            int heal = 0,
            int poison = 0,
            int bleed = 0,
            int stun = 0,
            int passiveSpeed = 0,
            ItemCategory synergyTargetCat = ItemCategory.Weapon,
            bool synergyAny = false,
            int starDmg = 0,
            int starShield = 0,
            int starHeal = 0,
            int starPoison = 0,
            int starBleed = 0,
            int starCdReduction = 0,
            bool targetAll = false,
            bool healLowestAlly = false)
        {
            ItemDefinitionSO def = CreateInstance<ItemDefinitionSO>();
            def.name = id;
            def.ItemId = id;
            def.DisplayName = displayName;
            def.Description = description;
            def.Category = category;
            def.RarityTier = tier;
            def.GoldCost = goldCost;
            def.SpriteName = spriteName;
            def.Shape = shape ?? ItemShape.Create1x1();
            def.CooldownTurns = cooldownTurns;
            def.BasePhysicalDamage = physDmg;
            def.BaseMagicDamage = magicDmg;
            def.BaseShield = shield;
            def.BaseHeal = heal;
            def.BasePoison = poison;
            def.BaseBleed = bleed;
            def.BaseStun = stun;
            def.PassiveBonusSpeed = passiveSpeed;
            def.SynergyTargetCategory = synergyTargetCat;
            def.SynergyTargetsAnyCategory = synergyAny;
            def.StarBonusDamage = starDmg;
            def.StarBonusShield = starShield;
            def.StarBonusHeal = starHeal;
            def.StarBonusPoison = starPoison;
            def.StarBonusBleed = starBleed;
            def.StarCooldownReduction = starCdReduction;
            def.TargetAllEnemies = targetAll;
            def.HealLowestAlly = healLowestAlly;
            return def;
        }
    }
}
