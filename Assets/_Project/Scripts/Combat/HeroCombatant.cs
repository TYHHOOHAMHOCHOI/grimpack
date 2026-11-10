using System.Collections.Generic;
using UnityEngine;
using Project05.Core;
using Project05.Data;
using Project05.Backpack;

namespace Project05.Combat
{
    /// <summary>
    /// Represents a party member (Hero) with their own personal BackpackGrid and role position.
    /// </summary>
    public class HeroCombatant : Combatant
    {
        public HeroDefinitionSO Definition { get; private set; }
        public PartyPosition Position { get; set; }
        public BackpackGrid PersonalBackpack { get; private set; }
        public BackpackSynergyReport LatestSynergyReport { get; private set; } = new BackpackSynergyReport();

        private int _runBuffBonusSpeed;
        private int _runBuffBonusMaxHp;
        public int PermanentBonusMaxHp { get; set; }

        public override bool IsHero => true;

        public HeroCombatant(HeroDefinitionSO definition, PartyPosition position, bool populateStartingItems = true)
        {
            Definition = definition;
            DisplayName = definition.HeroName;
            Position = position;
            MaxHP = definition.MaxHP;
            CurrentHP = MaxHP;
            BaseSpeed = definition.BaseSpeed;

            PersonalBackpack = new BackpackGrid(
                $"Balo: {definition.HeroName}",
                definition.BackpackWidth,
                definition.BackpackHeight,
                definition.InitialUnlockedCols,
                definition.InitialUnlockedRows);

            if (populateStartingItems && definition.StartingItemIds != null)
            {
                for (int i = 0; i < definition.StartingItemIds.Count; i++)
                {
                    ItemDefinitionSO itemDef = DefaultGameCatalog.GetItemById(definition.StartingItemIds[i]);
                    if (itemDef != null)
                    {
                        PersonalBackpack.TryAutoPlaceItem(new ItemData(itemDef));
                    }
                }
            }

            RecalculateSynergies(null);
        }

        public void RecalculateSynergies(IReadOnlyList<BuffDefinitionSO> activeRunBuffs)
        {
            SynergyEvaluator.EvaluateInto(PersonalBackpack, LatestSynergyReport, activeRunBuffs);

            _runBuffBonusSpeed = 0;
            _runBuffBonusMaxHp = 0;
            if (activeRunBuffs != null)
            {
                for (int i = 0; i < activeRunBuffs.Count; i++)
                {
                    if (activeRunBuffs[i] == null) continue;
                    _runBuffBonusSpeed += activeRunBuffs[i].BonusSpeed;
                    _runBuffBonusMaxHp += activeRunBuffs[i].BonusMaxHP;
                }
            }

            int newMaxHp = Mathf.Max(1, Definition.MaxHP + PermanentBonusMaxHp + LatestSynergyReport.TotalPassiveBonusMaxHp + _runBuffBonusMaxHp);
            if (newMaxHp > MaxHP)
            {
                int diff = newMaxHp - MaxHP;
                MaxHP = newMaxHp;
                CurrentHP += diff;
            }
            else
            {
                MaxHP = newMaxHp;
                if (CurrentHP > MaxHP) CurrentHP = MaxHP;
            }
        }

        public override int GetEffectiveSpeed()
        {
            return Mathf.Max(1, BaseSpeed + BonusCombatSpeed + LatestSynergyReport.TotalPassiveBonusSpeed + _runBuffBonusSpeed);
        }
    }
}
