using System.Collections.Generic;
using Project05.UI.Hud;

namespace Project05.Items
{
    /// <summary>One line of popup text, optionally led by a keyword glyph (art bible 7.8, 7.9).</summary>
    public sealed class PopupLine
    {
        public readonly GlyphKind? Glyph;
        public readonly string Text;
        public readonly bool Dim;

        public PopupLine(GlyphKind? glyph, string text, bool dim = false)
        {
            Glyph = glyph;
            Text = text;
            Dim = dim;
        }
    }

    /// <summary>Everything the popup shows for one item, independent of how it is drawn.</summary>
    public sealed class PopupContent
    {
        public string Title = string.Empty;
        public string Subtitle = string.Empty;
        public readonly List<PopupLine> Lines = new List<PopupLine>();
    }

    /// <summary>
    /// Builds the popup text for an item from its definition and, when it sits in a hero grid, from the modifiers
    /// its neighbours give it (design/gdd/hover-popup-and-glossary.md, rule 7). It calculates nothing the owning
    /// systems do not define: effective numbers use the documented formulas and show their base value.
    /// TEMPORARY: strings are Vietnamese literals until the localization system (System 7) exists.
    /// </summary>
    public static class ItemTooltipText
    {
        /// <summary>Tier name in words (never colour alone, art bible 4.4).</summary>
        public static string TierName(string tier)
        {
            switch (tier)
            {
                case "White": return "Trắng";
                case "Green": return "Lục";
                case "Blue": return "Lam";
                case "Purple": return "Tím";
                case "Gold": return "Vàng";
                case "Red": return "Đỏ";
                default: return tier;
            }
        }

        /// <summary>Footprint in words: the shape and the number of cells.</summary>
        public static string ShapeName(ItemRecord item)
        {
            string shape;
            switch (item.footprint)
            {
                case "l": shape = "chữ L"; break;
                case "t": shape = "chữ T"; break;
                case "s": shape = "chữ S"; break;
                default: shape = item.footprint.Replace('x', '×'); break;
            }

            return shape + ", " + item.cells.Length + " ô";
        }

        /// <summary>Tag in words.</summary>
        public static string TagName(string tag)
        {
            switch (tag)
            {
                case "weapon": return "vũ khí";
                case "blade": return "lưỡi sắc";
                case "axe": return "rìu";
                case "affliction": return "gây hại";
                case "alchemy": return "giả kim";
                case "tool": return "dụng cụ";
                case "armour": return "giáp";
                case "boots": return "giày";
                case "ranged": return "tầm xa";
                case "detonator": return "kích nổ";
                case "bell": return "chuông";
                case "instrument": return "nhạc cụ";
                case "charm": return "bùa";
                case "banner": return "cờ";
                case "blunt": return "cùn";
                case "chain": return "xích";
                case "relic": return "di vật";
                case "incense": return "hương";
                case "shield": return "khiên";
                case "heal": return "hồi máu";
                case "potion": return "dược phẩm";
                default: return tag;
            }
        }

        /// <summary>A status kind in words (debuffs-and-detonation.md).</summary>
        public static string StatusName(string statusId)
        {
            switch (statusId)
            {
                case "poison": return "Độc";
                case "burn": return "Thiêu Đốt";
                case "frost": return "Băng Giá";
                case "slow": return "Làm Chậm";
                case "weaken": return "Suy Yếu";
                case "stun": return "Đóng Băng";
                case "": case null: return "debuff";
                default: return statusId;
            }
        }

        /// <summary>The glyph of a status kind.</summary>
        public static GlyphKind StatusGlyph(string statusId)
        {
            switch (statusId)
            {
                case "poison": return GlyphKind.Droplet;
                case "burn": return GlyphKind.Burn;
                case "frost": return GlyphKind.Frost;
                case "slow": return GlyphKind.Slow;
                case "weaken": return GlyphKind.Weaken;
                default: return GlyphKind.Seal;
            }
        }

        private static string Target(string target)
        {
            switch (target)
            {
                case "AllEnemies": return "mọi kẻ địch";
                case "Self": return "bản thân";
                case "AllHeroes": return "cả đội";
                case "LowestHpHero": return "đồng đội ít HP nhất";
                default: return "kẻ địch đầu tiên";
            }
        }

        private static string Signed(int v) => v > 0 ? "+" + v : v.ToString();

        /// <summary>Builds the compact popup for an item.</summary>
        /// <param name="item">The definition.</param>
        /// <param name="placed">True when the item sits in a hero grid. Stash items neither give nor receive synergy.</param>
        /// <param name="received">The modifiers its neighbours give it (empty when not placed).</param>
        /// <summary>
        /// The fusion lines (item-fusion.md Rule 11): a fused item says what it was made from and is "Huyền thoại"; an
        /// ingredient (or its +) says what it can become and with what. Inserted before the tag and flavour lines.
        /// </summary>
        public static void AddRecipeLines(PopupContent c, ItemRecord item, ItemCatalog catalog)
        {
            if (c == null || item == null || catalog == null || catalog.Recipes.Count == 0) return;
            int at = c.Lines.Count;
            while (at > 0 && c.Lines[at - 1].Dim) at--;   // before the dim tag and flavour lines
            var lines = new List<PopupLine>();
            RecipeRecord made = catalog.RecipeMaking(item.id);
            if (made != null)
            {
                c.Subtitle = "Huyền thoại · " + ShapeName(item);
                lines.Add(new PopupLine(GlyphKind.Anvil, "Luyện từ: " + Names(made.ingredients, null, catalog)));
            }

            string baseId = item.IsUpgraded ? item.upgradeOf : item.id;
            List<RecipeRecord> uses = catalog.RecipesUsing(baseId);
            for (int i = 0; i < uses.Count && i < 2; i++)
            {
                ItemRecord result = catalog.Find(uses[i].result);
                string others = Names(uses[i].ingredients, baseId, catalog);
                lines.Add(new PopupLine(GlyphKind.Anvil, "Dùng để luyện: " + (result != null ? result.nameVi : uses[i].result) +
                                                         (others.Length > 0 ? " (cùng " + others + ")" : string.Empty)));
            }

            if (uses.Count > 2) lines.Add(new PopupLine(GlyphKind.Anvil, "… và " + (uses.Count - 2) + " công thức khác"));
            c.Lines.InsertRange(at, lines);
        }

        private static string Names(string[] ids, string skipOnce, ItemCatalog catalog)
        {
            var names = new List<string>();
            bool skipped = false;
            foreach (string id in ids)
            {
                if (!skipped && id == skipOnce)
                {
                    skipped = true;
                    continue;
                }

                ItemRecord r = catalog.Find(id);
                names.Add(r != null ? r.nameVi : id);
            }

            return string.Join(" + ", names);
        }

        public static PopupContent Build(ItemRecord item, bool placed, IReadOnlyList<SynergyModifier> received)
        {
            var c = new PopupContent
            {
                Title = item.nameVi,
                Subtitle = TierName(item.tier) + " · " + ShapeName(item),
            };

            int flat = ItemSynergy.Sum(received, "DamageFlat");
            int percent = ItemSynergy.Sum(received, "DamagePercent");
            int cooldownCut = ItemSynergy.Sum(received, "CooldownReduction");

            // trigger
            switch (item.trigger.kind)
            {
                case "Cooldown":
                    int n = ItemSynergy.EffectiveCooldown(item.trigger.n, cooldownCut);
                    c.Lines.Add(new PopupLine(GlyphKind.Speed, "Mỗi " + n + " hành động của chủ" + (cooldownCut != 0 ? " (gốc " + item.trigger.n + ")" : string.Empty)));
                    break;
                case "Reaction":
                    int nr = ItemSynergy.EffectiveCooldown(item.trigger.n, cooldownCut);
                    string reactDesc = item.trigger.eventKind == "Damage" &&
                                       (item.trigger.eventSide == "self" || item.trigger.eventSide == "own_hero")
                        ? "Phản ứng khi chủ nhận sát thương, hồi "
                        : item.trigger.eventKind == "StatusConsumed"
                            ? "Phản ứng khi debuff bị kích nổ, hồi "
                            : "Phản ứng khi kẻ địch dính debuff, hồi ";
                    c.Lines.Add(new PopupLine(GlyphKind.Speed, reactDesc + nr + (cooldownCut != 0 ? " (gốc " + item.trigger.n + ")" : string.Empty)));
                    break;
                case "CombatStart":
                    c.Lines.Add(new PopupLine(GlyphKind.Speed, "Đầu trận, một lần"));
                    break;
                default:
                    c.Lines.Add(new PopupLine(GlyphKind.Speed, "Bị động"));
                    break;
            }

            // effects
            foreach (EffectRecord e in item.effects)
            {
                switch (e.kind)
                {
                    case "Damage":
                        int dmg = ItemSynergy.EffectiveDamage(e.base_, flat, percent);
                        c.Lines.Add(new PopupLine(GlyphKind.Attack, "Gây " + dmg + " sát thương lên " + Target(e.target) + (dmg != e.base_ ? " (gốc " + e.base_ + ")" : string.Empty)));
                        break;
                    case "ApplyStatus":
                        int amount = e.amount + ItemSynergy.Sum(received, "StatusAmount", e.status);
                        c.Lines.Add(new PopupLine(StatusGlyph(e.status), "Gây " + amount + " " + StatusName(e.status) + " lên " + Target(e.target) + (amount != e.amount ? " (gốc " + e.amount + ")" : string.Empty)));
                        break;
                    case "DetonateAll":
                        string boost = percent != 0 ? ", " + Signed(percent / 10) + "% sát thương" : string.Empty;
                        int extra = ItemSynergy.Sum(received, "DetonateValue");
                        c.Lines.Add(new PopupLine(GlyphKind.Detonate, "Kích nổ mọi debuff trên mọi kẻ địch: 3-4 sát thương mỗi chồng" +
                            (extra != 0 ? " (" + Signed(extra) + " từ đồ kề bên)" : string.Empty) + ", +10% mỗi loại thêm" + boost));
                        break;
                    case "ConsumeDetonate":
                        string units = e.units >= 99 ? "mọi" : "tối đa " + e.units;
                        string pct = percent != 0 ? ", " + Signed(percent / 10) + "% sát thương" : string.Empty;
                        c.Lines.Add(new PopupLine(GlyphKind.Damage, "Nổ " + units + " Ấn, " + e.perUnit + " sát thương mỗi Ấn, lên " + Target(e.target) + pct));
                        break;
                    case "ShieldGain":
                        c.Lines.Add(new PopupLine(GlyphKind.Shield, "Tạo " + e.amount + " Giáp cho " + Target(e.target)));
                        break;
                    case "Heal":
                        c.Lines.Add(new PopupLine(GlyphKind.Heart, "Hồi " + e.amount + " HP cho " + Target(e.target)));
                        break;
                }
            }

            // stats
            if (item.hpBonus != 0) c.Lines.Add(new PopupLine(GlyphKind.Heart, Signed(item.hpBonus) + " HP tối đa"));
            if (item.speedBonus != 0) c.Lines.Add(new PopupLine(GlyphKind.Speed, Signed(item.speedBonus) + " tốc độ"));

            // synergy it gives
            foreach (SynergyRuleRecord r in item.synergy)
            {
                string who = r.targetTags.Length == 0 ? "mọi ô kề" : "ô kề " + TagList(r.targetTags);
                c.Lines.Add(new PopupLine(null, "Cho " + who + ": " + RuleText(r)));
            }

            // synergy it receives
            if (placed)
            {
                foreach (SynergyModifier m in received)
                {
                    c.Lines.Add(new PopupLine(null, "Nhận từ " + m.Source.nameVi + ": " + RuleText(m.Kind, m.Amount, m.StatusId)));
                }
            }
            else
            {
                c.Lines.Add(new PopupLine(null, "Đang trong kho: chưa ra đòn, chưa cộng hưởng.", true));
            }

            if (item.tags.Length > 0) c.Lines.Add(new PopupLine(null, "Thẻ: " + TagList(item.tags), true));
            if (!string.IsNullOrEmpty(item.flavorVi)) c.Lines.Add(new PopupLine(null, item.flavorVi, true));
            return c;
        }

        private static string TagList(string[] tags)
        {
            var names = new string[tags.Length];
            for (int i = 0; i < tags.Length; i++) names[i] = TagName(tags[i]);
            return string.Join(", ", names);
        }

        private static string RuleText(SynergyRuleRecord r) => RuleText(r.kind, r.amount, r.statusId);

        private static string RuleText(string kind, int amount, string statusId)
        {
            switch (kind)
            {
                case "DamageFlat": return Signed(amount) + " sát thương";
                case "DamagePercent": return Signed(amount / 10) + "% sát thương";
                case "CooldownReduction": return "hồi chiêu " + Signed(-amount);
                case "StatusAmount": return Signed(amount) + " " + StatusName(statusId) + " mỗi lần gây";
                case "DetonateValue": return Signed(amount) + " sát thương nổ mỗi chồng " + StatusName(statusId);
                default: return kind;
            }
        }
    }
}
