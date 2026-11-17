using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Project05.Items;
using Project05.UI.Hud;
using UnityEngine;

namespace Project05.Tests.EditMode
{
    /// <summary>The MVP item catalog, the synergy rules, the popup text and the popup placement.</summary>
    public class ItemSystemTests
    {
        private static ItemCatalog LoadCatalog()
        {
            return ItemCatalog.Load(File.ReadAllText("Assets/_Project/Data/items_mvp.json"));
        }

        private static PlacedItem At(ItemCatalog c, string id, int x, int y) => new PlacedItem(c.Find(id), x, y);

        [Test]
        public void Catalog_Loads91ItemsWithUniqueIds()
        {
            ItemCatalog c = LoadCatalog();

            Assert.AreEqual(91, c.All.Count);   // 46 items + 45 upgraded (+) versions (item-upgrades.md)
            var seen = new HashSet<string>();
            foreach (ItemRecord i in c.All) Assert.IsTrue(seen.Add(i.id), i.id);
        }

        [Test]
        public void Catalog_EveryItemObeysTheDocumentedLimits()
        {
            foreach (ItemRecord i in LoadCatalog().All)
            {
                Assert.LessOrEqual(i.effects.Length, 4, i.id);
                Assert.LessOrEqual(i.synergy.Length, 3, i.id);
                Assert.IsTrue(i.cells.Length >= 1 && i.cells.Length <= 9, i.id);
                Assert.IsTrue(i.hpBonus >= -20 && i.hpBonus <= 40, i.id);
                Assert.AreEqual(i.trigger.kind == "Passive", i.effects.Length == 0, i.id);
                foreach (EffectRecord e in i.effects) CollectionAssert.Contains(new[] { "Damage", "ApplyStatus", "ConsumeDetonate", "DetonateAll", "ShieldGain", "Heal" }, e.kind, i.id);
            }
        }

        [Test]
        public void Adjacent_SharedEdge_IsTrueAndDiagonalIsFalse()
        {
            ItemCatalog c = LoadCatalog();

            Assert.IsTrue(ItemSynergy.Adjacent(At(c, "item_iron_sword", 0, 0), At(c, "item_whetstone", 1, 1)));
            Assert.IsFalse(ItemSynergy.Adjacent(At(c, "item_dagger", 0, 0), At(c, "item_dagger", 1, 1)));
        }

        [Test]
        public void Evaluate_WhetstoneNextToBlades_GivesFlatDamageOnlyToBlades()
        {
            ItemCatalog c = LoadCatalog();
            var placed = new List<PlacedItem>
            {
                At(c, "item_whetstone", 1, 0), At(c, "item_iron_sword", 0, 0), At(c, "item_oil_flask", 2, 0),
            };

            List<SynergyModifier>[] result = ItemSynergy.Evaluate(placed);

            Assert.AreEqual(3, ItemSynergy.Sum(result[1], "DamageFlat"));
            Assert.AreEqual(0, result[2].Count);
        }

        [Test]
        public void Evaluate_RuleThatCannotChangeTheTarget_MakesNoLink()
        {
            ItemCatalog c = LoadCatalog();
            // the drum boosts weapons, but a debuff item has no Damage effect
            var placed = new List<PlacedItem> { At(c, "item_war_drum", 0, 0), At(c, "item_venom_vial", 1, 0) };

            List<SynergyModifier>[] result = ItemSynergy.Evaluate(placed);

            Assert.AreEqual(0, result[1].Count);
        }

        [Test]
        public void Evaluate_StashItemsAreNotPartOfAnyGrid()
        {
            ItemCatalog c = LoadCatalog();

            List<SynergyModifier>[] result = ItemSynergy.Evaluate(new List<PlacedItem> { At(c, "item_iron_sword", 0, 0) });

            Assert.AreEqual(0, result[0].Count);
        }

        [TestCase(6, 2, 250, 10)]    // documented example: B = 6, F = +2, P = +250 permille gives 10
        [TestCase(3, -5, 0, 0)]      // negative raw damage clamps to zero
        [TestCase(9, 3, 0, 12)]
        public void EffectiveDamage_UsesTheDocumentedFormula(int baseDamage, int flat, int percent, int expected)
        {
            Assert.AreEqual(expected, ItemSynergy.EffectiveDamage(baseDamage, flat, percent));
        }

        [TestCase(2, 1, 1)]
        [TestCase(1, 1, 1)]
        [TestCase(1, 5, 1)]
        [TestCase(3, -2, 5)]
        public void EffectiveCooldown_IsClampedToAtLeastOne(int n, int cut, int expected)
        {
            Assert.AreEqual(expected, ItemSynergy.EffectiveCooldown(n, cut));
        }

        [Test]
        public void Build_PlacedSword_ShowsBaseAndBoostedDamageAndTheSource()
        {
            ItemCatalog c = LoadCatalog();
            var placed = new List<PlacedItem> { At(c, "item_iron_sword", 0, 0), At(c, "item_whetstone", 1, 0) };
            List<SynergyModifier>[] mods = ItemSynergy.Evaluate(placed);

            PopupContent content = ItemTooltipText.Build(placed[0].Item, true, mods[0]);

            StringAssert.Contains("Gây 12 sát thương", content.Lines[1].Text);
            StringAssert.Contains("gốc 9", content.Lines[1].Text);
            Assert.IsTrue(content.Lines.Exists(l => l.Text.Contains("Đá Mài")));
        }

        [Test]
        public void Build_StashItem_SaysItDoesNotCombine()
        {
            ItemCatalog c = LoadCatalog();

            PopupContent content = ItemTooltipText.Build(c.Find("item_crossbow"), false, new List<SynergyModifier>());

            Assert.IsTrue(content.Lines.Exists(l => l.Text.Contains("trong kho")));
        }

        [Test]
        public void Build_Subtitle_NamesTierAndShapeInWords()
        {
            ItemCatalog c = LoadCatalog();

            PopupContent content = ItemTooltipText.Build(c.Find("item_war_hammer"), false, new List<SynergyModifier>());

            StringAssert.Contains("Lam", content.Subtitle);
            StringAssert.Contains("chữ T", content.Subtitle);
        }

        [Test]
        public void Choose_NearTheBottom_OpensAboveAndNeverTouchesTheSource()
        {
            var bounds = new Rect(0, 16, 174, 344);
            var source = new Rect(7, 300, 20, 20);

            Rect popup = ItemPopupPlacement.Choose(bounds, source, new List<Rect>(), new Vector2(152, 100));

            Assert.IsFalse(popup.Overlaps(new Rect(source.x - 3, source.y - 3, 26, 26)));
            Assert.IsTrue(popup.yMax <= bounds.yMax && popup.x >= bounds.x);
        }

        [Test]
        public void Choose_PrefersTheCandidateThatHidesFewerItems()
        {
            var bounds = new Rect(0, 16, 174, 344);
            var source = new Rect(7, 120, 20, 20);
            var blocker = new List<Rect> { new Rect(7, 144, 160, 60) };   // below is crowded

            Rect popup = ItemPopupPlacement.Choose(bounds, source, blocker, new Vector2(152, 60));

            Assert.Less(popup.y, source.y);
        }
    }
}
