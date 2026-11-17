using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project05.Items
{
    /// <summary>One footprint cell, relative to the item origin.</summary>
    [Serializable]
    public sealed class CellRecord
    {
        public int x;
        public int y;
    }

    /// <summary>The single trigger of an item (design/gdd/item-trigger-and-cooldown.md, rule 1).</summary>
    [Serializable]
    public sealed class TriggerRecord
    {
        /// <summary>Passive, Cooldown, Reaction or CombatStart.</summary>
        public string kind = "Passive";

        /// <summary>Cooldown N, in the owner's own actions. Zero for passive and combat-start items.</summary>
        public int n;

        /// <summary>Start delay S.</summary>
        public int s;

        public string eventKind = string.Empty;
        public string eventStatus = string.Empty;
        public string eventSide = string.Empty;
    }

    /// <summary>One authored effect. MVP kinds are Damage, ApplyStatus and ConsumeDetonate (effect-resolution-model.md).</summary>
    [Serializable]
    public sealed class EffectRecord
    {
        public string kind = "Damage";
        public string target = "FirstEnemy";
        public int base_;
        public string status = string.Empty;
        public int amount;
        public int units;
        public int perUnit;
    }

    /// <summary>One synergy rule on a neighbour link (design/gdd/adjacency-and-synergy.md, rule 3).</summary>
    [Serializable]
    public sealed class SynergyRuleRecord
    {
        public string[] targetTags = Array.Empty<string>();

        /// <summary>DamageFlat, DamagePercent (permille), CooldownReduction or StatusAmount.</summary>
        public string kind = "DamageFlat";
        public int amount;
        public string statusId = string.Empty;
    }

    /// <summary>A static item definition, as authored in Assets/_Project/Data/items_mvp.json.</summary>
    [Serializable]
    public sealed class ItemRecord
    {
        public string id = string.Empty;
        public string art = string.Empty;
        public string tier = "White";
        public string footprint = "1x1";
        public CellRecord[] cells = Array.Empty<CellRecord>();
        public string[] tags = Array.Empty<string>();
        public TriggerRecord trigger = new TriggerRecord();
        public EffectRecord[] effects = Array.Empty<EffectRecord>();
        public int hpBonus;
        public int speedBonus;
        public SynergyRuleRecord[] synergy = Array.Empty<SynergyRuleRecord>();
        public string nameVi = string.Empty;
        public string nameEn = string.Empty;
        public string flavorVi = string.Empty;
        public string flavorEn = string.Empty;
        /// <summary>The id of the upgraded (+) version, or empty (item-upgrades.md).</summary>
        public string upgradeId = string.Empty;
        /// <summary>For a + item, the id of its base item; empty for a base item.</summary>
        public string upgradeOf = string.Empty;
        /// <summary>Gold paid when a room is won while the item is in the hero grid.</summary>
        public int goldOnWin;

        /// <summary>True for an upgraded (+) item.</summary>
        public bool IsUpgraded => !string.IsNullOrEmpty(upgradeOf);

        /// <summary>True when the item detonates debuffs.</summary>
        public bool Detonates
        {
            get
            {
                foreach (EffectRecord e in effects) if (e.kind == "DetonateAll") return true;
                return false;
            }
        }

        /// <summary>True when any effect or trigger has a cooldown (Cooldown and Reaction items).</summary>
        public bool HasCooldown => trigger.kind == "Cooldown" || trigger.kind == "Reaction";

        /// <summary>True when the item has at least one Damage or ConsumeDetonate effect.</summary>
        public bool DealsDamage
        {
            get
            {
                foreach (EffectRecord e in effects) if (e.kind == "Damage" || e.kind == "ConsumeDetonate" || e.kind == "DetonateAll") return true;
                return false;
            }
        }

        /// <summary>True when the item has an ApplyStatus effect for the given status.</summary>
        public bool AppliesStatus(string statusId)
        {
            foreach (EffectRecord e in effects) if (e.kind == "ApplyStatus" && (string.IsNullOrEmpty(statusId) || e.status == statusId)) return true;
            return false;
        }

        /// <summary>True when the item carries the tag.</summary>
        public bool HasTag(string tag) => Array.IndexOf(tags, tag) >= 0;
    }

    /// <summary>A fusion recipe as authored in items_mvp.json (design/gdd/item-fusion.md).</summary>
    [Serializable]
    public sealed class RecipeRecord
    {
        public string id = string.Empty;
        public string[] ingredients = Array.Empty<string>();
        public string result = string.Empty;
    }

    [Serializable]
    internal sealed class ItemCatalogFile
    {
        public ItemRecord[] items = Array.Empty<ItemRecord>();
        public RecipeRecord[] recipes = Array.Empty<RecipeRecord>();
    }

    /// <summary>Read-only lookup over the parsed item catalog.</summary>
    public sealed class ItemCatalog
    {
        private readonly Dictionary<string, ItemRecord> _byId = new Dictionary<string, ItemRecord>(StringComparer.Ordinal);

        /// <summary>Every item in authored order.</summary>
        public IReadOnlyList<ItemRecord> All { get; }

        /// <summary>The fusion recipes in authored order (empty for a catalog without them).</summary>
        public IReadOnlyList<RecipeRecord> Recipes { get; }

        private ItemCatalog(ItemRecord[] items, RecipeRecord[] recipes)
        {
            All = items;
            Recipes = recipes ?? Array.Empty<RecipeRecord>();
            foreach (ItemRecord i in items) _byId[i.id] = i;
        }

        /// <summary>The recipe that makes this item, or null.</summary>
        public RecipeRecord RecipeMaking(string resultId)
        {
            foreach (RecipeRecord r in Recipes)
            {
                if (r.result == resultId) return r;
            }

            return null;
        }

        /// <summary>The recipes that consume this base item, in authored order.</summary>
        public List<RecipeRecord> RecipesUsing(string baseId)
        {
            var list = new List<RecipeRecord>();
            foreach (RecipeRecord r in Recipes)
            {
                if (Array.IndexOf(r.ingredients, baseId) >= 0) list.Add(r);
            }

            return list;
        }

        /// <summary>Parses the catalog JSON written by tools/art/item_catalog.py.</summary>
        /// <param name="json">The contents of items_mvp.json.</param>
        public static ItemCatalog Load(string json)
        {
            // JsonUtility cannot read a field named "base" (a C# keyword), so the data file spells it "base" and is
            // renamed here to the field "base_".
            string fixedJson = json.Replace("\"base\":", "\"base_\":");
            ItemCatalogFile file = JsonUtility.FromJson<ItemCatalogFile>(fixedJson);
            return new ItemCatalog(file.items, file.recipes);
        }

        /// <summary>Finds an item by id, or null.</summary>
        public ItemRecord Find(string id) => id != null && _byId.TryGetValue(id, out ItemRecord r) ? r : null;
    }
}
