using System.Collections.Generic;

namespace Project05.Items
{
    /// <summary>An item placed in one hero grid: its record and the grid cell of its origin.</summary>
    public readonly struct PlacedItem
    {
        public readonly ItemRecord Item;
        public readonly int X;
        public readonly int Y;

        public PlacedItem(ItemRecord item, int x, int y)
        {
            Item = item;
            X = x;
            Y = y;
        }
    }

    /// <summary>One modifier a neighbour gives to an item (adjacency-and-synergy.md rules 3 to 7).</summary>
    public readonly struct SynergyModifier
    {
        public readonly int SourceIndex;
        public readonly ItemRecord Source;
        public readonly string Kind;
        public readonly int Amount;
        public readonly string StatusId;

        public SynergyModifier(int sourceIndex, ItemRecord source, string kind, int amount, string statusId)
        {
            SourceIndex = sourceIndex;
            Source = source;
            Kind = kind;
            Amount = amount;
            StatusId = statusId;
        }
    }

    /// <summary>
    /// Pure synergy evaluation for one hero grid: who is adjacent to whom and which rules make a real link.
    /// Integer arithmetic only, no engine state, no randomness, so the same arrangement always gives the same view.
    /// </summary>
    public static class ItemSynergy
    {
        /// <summary>
        /// Computes, for each placed item, the modifiers it receives. Items are adjacent when a cell of one shares an
        /// edge with a cell of the other. A rule links only when it can change something on the target.
        /// </summary>
        /// <param name="placed">The items of one hero grid.</param>
        /// <returns>One list of modifiers per placed item, in the same order, sorted by source then rule index.</returns>
        public static List<SynergyModifier>[] Evaluate(IReadOnlyList<PlacedItem> placed)
        {
            var result = new List<SynergyModifier>[placed.Count];
            for (int i = 0; i < placed.Count; i++) result[i] = new List<SynergyModifier>();

            for (int s = 0; s < placed.Count; s++)
            {
                SynergyRuleRecord[] rules = placed[s].Item.synergy;
                for (int r = 0; r < rules.Length; r++)
                {
                    for (int t = 0; t < placed.Count; t++)
                    {
                        if (t == s || !Adjacent(placed[s], placed[t])) continue;
                        if (!Targets(rules[r], placed[t].Item) || !Applicable(rules[r], placed[t].Item)) continue;
                        result[t].Add(new SynergyModifier(s, placed[s].Item, rules[r].kind, rules[r].amount, rules[r].statusId));
                    }
                }
            }

            return result;
        }

        /// <summary>True when the two placed items share at least one cell edge.</summary>
        public static bool Adjacent(PlacedItem a, PlacedItem b)
        {
            foreach (CellRecord ca in a.Item.cells)
            {
                foreach (CellRecord cb in b.Item.cells)
                {
                    int dx = System.Math.Abs(a.X + ca.x - (b.X + cb.x));
                    int dy = System.Math.Abs(a.Y + ca.y - (b.Y + cb.y));
                    if (dx + dy == 1) return true;
                }
            }

            return false;
        }

        private static bool Targets(SynergyRuleRecord rule, ItemRecord target)
        {
            if (rule.targetTags.Length == 0) return true;
            foreach (string tag in rule.targetTags) if (target.HasTag(tag)) return true;
            return false;
        }

        private static bool Applicable(SynergyRuleRecord rule, ItemRecord target)
        {
            switch (rule.kind)
            {
                case "DamageFlat":
                case "DamagePercent": return target.DealsDamage;
                case "CooldownReduction": return target.HasCooldown;
                case "StatusAmount": return target.AppliesStatus(rule.statusId);
                default: return false;
            }
        }

        /// <summary>Damage after modifiers: Raw = max(0, base + flat), result = (Raw x (1000 + percent) + 500) / 1000 (effect-resolution-model.md).</summary>
        public static int EffectiveDamage(int baseDamage, int flat, int percentPermille)
        {
            long raw = System.Math.Max(0, baseDamage + flat);
            long p = System.Math.Max(-1000, percentPermille);
            return (int)((raw * (1000 + p) + 500) / 1000);
        }

        /// <summary>Cooldown after reduction: clamp(N - C, 1, 99) (item-trigger-and-cooldown.md rule 7).</summary>
        public static int EffectiveCooldown(int n, int reduction)
        {
            int v = n - reduction;
            return v < 1 ? 1 : v > 99 ? 99 : v;
        }

        /// <summary>Sums the modifiers of one kind.</summary>
        public static int Sum(IReadOnlyList<SynergyModifier> modifiers, string kind, string statusId = null)
        {
            int total = 0;
            foreach (SynergyModifier m in modifiers)
            {
                if (m.Kind == kind && (statusId == null || m.StatusId == statusId)) total += m.Amount;
            }

            return total;
        }
    }
}
