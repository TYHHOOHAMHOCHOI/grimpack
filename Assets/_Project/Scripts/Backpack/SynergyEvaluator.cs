using System;
using System.Collections.Generic;
using UnityEngine;
using Project05.Core;
using Project05.Data;

namespace Project05.Backpack
{
    public struct SynergyLink
    {
        public ItemData SourceItem;
        public ItemData TargetItem;
        public GridCoord StarCell;
        public string Summary;
    }

    public class EvaluatedItemStats
    {
        public ItemData Item;
        public int TotalPhysicalDamage;
        public int TotalMagicDamage;
        public int TotalShield;
        public int TotalHeal;
        public int TotalPoison;
        public int TotalBleed;
        public int TotalStun;
        public int EffectiveCooldownTurns;
        public int SynergyLinksReceived;
    }

    public class BackpackSynergyReport
    {
        public readonly Dictionary<ItemData, EvaluatedItemStats> ItemStats = new Dictionary<ItemData, EvaluatedItemStats>();
        public readonly List<SynergyLink> ActiveLinks = new List<SynergyLink>();
        public int TotalPassiveBonusSpeed;
        public int TotalPassiveBonusMaxHp;

        public void Clear()
        {
            ItemStats.Clear();
            ActiveLinks.Clear();
            TotalPassiveBonusSpeed = 0;
            TotalPassiveBonusMaxHp = 0;
        }
    }

    /// <summary>
    /// Evaluates directional Star cell synergies and orthogonal adjacency bonuses across a BackpackGrid.
    /// </summary>
    public static class SynergyEvaluator
    {
        private static readonly List<GridCoord> TempStarCells = new List<GridCoord>(16);
        private static readonly HashSet<(ItemData, ItemData)> ProcessedPairs = new HashSet<(ItemData, ItemData)>();

        public static BackpackSynergyReport Evaluate(BackpackGrid grid, IReadOnlyList<BuffDefinitionSO> activeRunBuffs = null)
        {
            BackpackSynergyReport report = new BackpackSynergyReport();
            EvaluateInto(grid, report, activeRunBuffs);
            return report;
        }

        public static void EvaluateInto(
            BackpackGrid grid,
            BackpackSynergyReport report,
            IReadOnlyList<BuffDefinitionSO> activeRunBuffs = null)
        {
            if (report == null) throw new ArgumentNullException(nameof(report));
            report.Clear();
            if (grid == null) return;

            int runBonusWeaponDmg = 0;
            int runBonusMagicDmg = 0;
            int runBonusPoison = 0;
            int runBonusBleed = 0;
            int runBonusHeal = 0;
            int runCdReduction = 0;

            if (activeRunBuffs != null)
            {
                for (int i = 0; i < activeRunBuffs.Count; i++)
                {
                    BuffDefinitionSO buff = activeRunBuffs[i];
                    if (buff == null) continue;
                    runBonusWeaponDmg += buff.BonusWeaponDamage;
                    runBonusMagicDmg += buff.BonusMagicDamage;
                    runBonusPoison += buff.BonusPoisonPerTrigger;
                    runBonusBleed += buff.BonusBleedPerTrigger;
                    runBonusHeal += buff.BonusHealPerTrigger;
                    runCdReduction += buff.CooldownReductionTurns;
                }
            }

            IReadOnlyList<ItemData> items = grid.PlacedItems;

            // Step 1: Initialize base stats for each placed item
            for (int i = 0; i < items.Count; i++)
            {
                ItemData item = items[i];
                ItemDefinitionSO def = item.Definition;

                report.TotalPassiveBonusSpeed += def.PassiveBonusSpeed;
                report.TotalPassiveBonusMaxHp += def.PassiveBonusMaxHp;

                bool isWeaponOrPhys = def.Category == ItemCategory.Weapon || def.BasePhysicalDamage > 0;
                int basePhys = def.BasePhysicalDamage > 0 ? def.BasePhysicalDamage + runBonusWeaponDmg : 0;
                int baseMagic = def.BaseMagicDamage > 0 ? def.BaseMagicDamage + runBonusMagicDmg : 0;
                int basePoison = def.BasePoison > 0 ? def.BasePoison + runBonusPoison : 0;
                int baseBleed = (def.BaseBleed > 0 || isWeaponOrPhys) ? def.BaseBleed + runBonusBleed : 0;
                int baseHeal = def.BaseHeal > 0 ? def.BaseHeal + runBonusHeal : 0;
                int baseCd = def.CooldownTurns > 0 ? Mathf.Max(1, def.CooldownTurns - runCdReduction) : 0;

                report.ItemStats[item] = new EvaluatedItemStats
                {
                    Item = item,
                    TotalPhysicalDamage = basePhys,
                    TotalMagicDamage = baseMagic,
                    TotalShield = def.BaseShield,
                    TotalHeal = baseHeal,
                    TotalPoison = basePoison,
                    TotalBleed = baseBleed,
                    TotalStun = def.BaseStun,
                    EffectiveCooldownTurns = baseCd,
                    SynergyLinksReceived = 0
                };
            }

            // Step 2: Evaluate directional Star cell synergies between items
            ProcessedPairs.Clear();

            for (int i = 0; i < items.Count; i++)
            {
                ItemData source = items[i];
                ItemDefinitionSO srcDef = source.Definition;

                source.GetSynergyStarGridCells(source.AnchorX, source.AnchorY, source.RotationSteps, TempStarCells);

                for (int s = 0; s < TempStarCells.Count; s++)
                {
                    GridCoord starCoord = TempStarCells[s];
                    ItemData target = grid.GetItemAt(starCoord.X, starCoord.Y);
                    if (target == null || target == source) continue;

                    // Only apply a source item's star bonus once per target item
                    if (ProcessedPairs.Contains((source, target))) continue;

                    ItemDefinitionSO tgtDef = target.Definition;
                    bool categoryMatches = srcDef.SynergyTargetsAnyCategory || tgtDef.Category == srcDef.SynergyTargetCategory;
                    if (!categoryMatches) continue;

                    ProcessedPairs.Add((source, target));

                    if (!report.ItemStats.TryGetValue(target, out EvaluatedItemStats targetStats)) continue;

                    bool targetIsOffensive = tgtDef.Category == ItemCategory.Weapon ||
                                             tgtDef.BasePhysicalDamage > 0 ||
                                             tgtDef.BaseMagicDamage > 0;
                    bool targetCanInflictStatus = targetIsOffensive ||
                                                  tgtDef.BasePoison > 0 ||
                                                  tgtDef.BaseBleed > 0;

                    bool appliedAny = false;
                    if (srcDef.StarBonusDamage > 0 && targetIsOffensive)
                    {
                        if (tgtDef.BaseMagicDamage > 0 && tgtDef.BasePhysicalDamage == 0)
                        {
                            targetStats.TotalMagicDamage += srcDef.StarBonusDamage;
                        }
                        else
                        {
                            targetStats.TotalPhysicalDamage += srcDef.StarBonusDamage;
                        }
                        appliedAny = true;
                    }

                    if (srcDef.StarBonusShield > 0 && (tgtDef.BaseShield > 0 || tgtDef.Category == ItemCategory.Armor))
                    {
                        targetStats.TotalShield += srcDef.StarBonusShield;
                        appliedAny = true;
                    }

                    if (srcDef.StarBonusHeal > 0 && tgtDef.BaseHeal > 0)
                    {
                        targetStats.TotalHeal += srcDef.StarBonusHeal;
                        appliedAny = true;
                    }

                    if (srcDef.StarBonusPoison > 0 && targetCanInflictStatus)
                    {
                        bool hadNoPoisonBefore = targetStats.TotalPoison == 0 && tgtDef.BasePoison == 0;
                        targetStats.TotalPoison += srcDef.StarBonusPoison;
                        if (hadNoPoisonBefore && runBonusPoison > 0)
                        {
                            targetStats.TotalPoison += runBonusPoison;
                        }
                        appliedAny = true;
                    }

                    if (srcDef.StarBonusBleed > 0 && targetCanInflictStatus)
                    {
                        targetStats.TotalBleed += srcDef.StarBonusBleed;
                        appliedAny = true;
                    }

                    if (srcDef.StarCooldownReduction > 0 && targetStats.EffectiveCooldownTurns > 1)
                    {
                        targetStats.EffectiveCooldownTurns = Mathf.Max(1, targetStats.EffectiveCooldownTurns - srcDef.StarCooldownReduction);
                        appliedAny = true;
                    }

                    if (srcDef.StarBonusSpeed > 0)
                    {
                        report.TotalPassiveBonusSpeed += srcDef.StarBonusSpeed;
                        appliedAny = true;
                    }

                    if (appliedAny)
                    {
                        targetStats.SynergyLinksReceived++;
                        report.ActiveLinks.Add(new SynergyLink
                        {
                            SourceItem = source,
                            TargetItem = target,
                            StarCell = starCoord,
                            Summary = $"{srcDef.DisplayName} ★ -> {tgtDef.DisplayName}"
                        });
                    }
                }
            }
        }
    }
}
