using System.Collections.Generic;
using NUnit.Framework;
using Project05.Core;
using Project05.Data;
using Project05.Backpack;

namespace Project05.Tests.EditMode
{
    [TestFixture]
    public class BackpackAndSynergyTests
    {
        [SetUp]
        public void SetUp()
        {
            DefaultGameCatalog.EnsureInitialized();
        }

        [Test]
        public void ItemShape_Rotation90Clockwise_TransformsDimensionsAndReturnsAfter4Steps()
        {
            ItemShape lShape = ItemShape.CreateLShape(new GridCoord(1, 0));
            Assert.AreEqual(2, lShape.GetWidth(0));
            Assert.AreEqual(2, lShape.GetHeight(0));

            ItemShape sword1x2 = ItemShape.Create1x2Vertical(new GridCoord(-1, 0), new GridCoord(1, 0));
            Assert.AreEqual(1, sword1x2.GetWidth(0));
            Assert.AreEqual(2, sword1x2.GetHeight(0));

            // Rotate 90 degrees clockwise -> becomes 2x1 horizontal
            Assert.AreEqual(2, sword1x2.GetWidth(1));
            Assert.AreEqual(1, sword1x2.GetHeight(1));

            // Rotate 180 degrees -> 1x2 vertical
            Assert.AreEqual(1, sword1x2.GetWidth(2));
            Assert.AreEqual(2, sword1x2.GetHeight(2));

            // Rotate 360 degrees (step 4) -> identical to step 0
            IReadOnlyList<GridCoord> occ0 = sword1x2.GetOccupiedOffsets(0);
            IReadOnlyList<GridCoord> occ4 = sword1x2.GetOccupiedOffsets(4);
            CollectionAssert.AreEquivalent(occ0, occ4);
        }

        [Test]
        public void BackpackGrid_PlacementAndCollisionAndLockedSlots_EnforcedCorrectly()
        {
            // 4x4 grid with only 3x3 unlocked initially
            BackpackGrid grid = new BackpackGrid("TestBag", 4, 4, initialUnlockedCols: 3, initialUnlockedRows: 3);
            Assert.AreEqual(9, grid.GetUnlockedSlotCount());
            Assert.AreEqual(7, grid.GetLockedSlotCount());

            ItemData sword = new ItemData(DefaultGameCatalog.GetItemById("item_iron_sword")); // 1x2 vertical
            ItemData shield2x2 = new ItemData(DefaultGameCatalog.GetItemById("item_tower_shield")); // 2x2 square

            // Place sword at (0, 0) -> occupies (0,0) and (0,1)
            Assert.IsTrue(grid.TryPlaceItem(sword, 0, 0, 0));
            Assert.AreSame(sword, grid.GetItemAt(0, 0));
            Assert.AreSame(sword, grid.GetItemAt(0, 1));

            // Overlapping placement at (0, 1) must fail
            Assert.IsFalse(grid.TryPlaceItem(shield2x2, 0, 1, 0));

            // Placement onto locked slot column 3 (x=2..3) must fail
            Assert.IsFalse(grid.TryPlaceItem(shield2x2, 2, 0, 0));

            // Valid non-overlapping placement at (1, 0) -> occupies (1,0),(2,0),(1,1),(2,1)
            Assert.IsTrue(grid.TryPlaceItem(shield2x2, 1, 0, 0));

            // Unlock remaining slots and place at (2, 2)
            grid.UnlockSlot(3, 2);
            grid.UnlockSlot(3, 3);
            grid.UnlockSlot(2, 3);
            Assert.IsTrue(grid.TryPlaceItem(shield2x2, 2, 2, 0));
        }

        [Test]
        public void SynergyEvaluator_WhetstoneAndPoisonFlaskAdjacentToSword_AmplifiesStats()
        {
            BackpackGrid grid = new BackpackGrid("SynergyBag", 5, 4, 5, 4);
            ItemData sword = new ItemData(DefaultGameCatalog.GetItemById("item_iron_sword")); // 12 phys dmg
            ItemData whetstone = new ItemData(DefaultGameCatalog.GetItemById("item_whetstone")); // +6 dmg, +1 bleed to adjacent weapon
            ItemData poisonFlask = new ItemData(DefaultGameCatalog.GetItemById("item_poison_flask")); // +3 poison to adjacent weapon

            // Place sword at (1, 1) -> occupies (1,1) and (1,2)
            Assert.IsTrue(grid.TryPlaceItem(sword, 1, 1, 0));

            // Evaluate without neighbors
            BackpackSynergyReport soloReport = SynergyEvaluator.Evaluate(grid);
            Assert.AreEqual(12, soloReport.ItemStats[sword].TotalPhysicalDamage);
            Assert.AreEqual(0, soloReport.ItemStats[sword].TotalPoison);
            Assert.AreEqual(0, soloReport.ItemStats[sword].TotalBleed);

            // Place whetstone at (0, 1) (immediately left of sword) and poisonFlask at (2, 1) (immediately right of sword)
            Assert.IsTrue(grid.TryPlaceItem(whetstone, 0, 1, 0));
            Assert.IsTrue(grid.TryPlaceItem(poisonFlask, 2, 1, 0));

            BackpackSynergyReport linkedReport = SynergyEvaluator.Evaluate(grid);
            EvaluatedItemStats swordStats = linkedReport.ItemStats[sword];

            Assert.AreEqual(12 + 6, swordStats.TotalPhysicalDamage, "Whetstone should grant +6 Physical Damage");
            Assert.AreEqual(1, swordStats.TotalBleed, "Whetstone should grant +1 Bleed");
            Assert.AreEqual(3, swordStats.TotalPoison, "Poison Flask should grant +3 Poison on hit");
            Assert.AreEqual(2, swordStats.SynergyLinksReceived);
        }

        [Test]
        public void SynergyEvaluator_EldritchIdolAdjacentTo3TurnTome_ReducesCooldownBy1Turn()
        {
            BackpackGrid grid = new BackpackGrid("IdolBag", 5, 4, 5, 4);
            ItemData tome = new ItemData(DefaultGameCatalog.GetItemById("item_thunder_tome")); // 2x2, Base CD = 3
            ItemData idol = new ItemData(DefaultGameCatalog.GetItemById("item_cursed_idol")); // 1x1, StarCooldownReduction = 1

            Assert.IsTrue(grid.TryPlaceItem(tome, 1, 1, 0));
            Assert.IsTrue(grid.TryPlaceItem(idol, 0, 1, 0)); // Left of tome's (1,1) cell

            BackpackSynergyReport report = SynergyEvaluator.Evaluate(grid);
            Assert.AreEqual(2, report.ItemStats[tome].EffectiveCooldownTurns, "Eldritch Idol should reduce Tome cooldown from 3 to 2 turns");
        }

        [Test]
        public void BackpackGrid_EdgeCases_NegativeCoordsAndNullItem_HandledSafely()
        {
            BackpackGrid grid = new BackpackGrid("EdgeBag", 4, 4, 4, 4);
            ItemData dagger = new ItemData(DefaultGameCatalog.GetItemById("item_curved_dagger"));

            Assert.IsFalse(grid.CanPlaceItemAt(null, 0, 0, 0));
            Assert.IsFalse(grid.CanPlaceItemAt(dagger, -1, 0, 0));
            Assert.IsFalse(grid.CanPlaceItemAt(dagger, 0, -1, 0));
            Assert.IsFalse(grid.CanPlaceItemAt(dagger, 4, 0, 0));
            Assert.IsFalse(grid.CanPlaceItemAt(dagger, 0, 4, 0));
            Assert.IsNull(grid.GetItemAt(-5, 99));
        }

        [Test]
        public void SynergyEvaluator_EldritchIdolAdjacentToShieldAndPotion_DoesNotGrantOffensiveDamageToNonOffensiveItems()
        {
            BackpackGrid grid = new BackpackGrid("PureSupportBag", 5, 4, 5, 4);
            ItemData idol = new ItemData(DefaultGameCatalog.GetItemById("item_cursed_idol")); // synergyAny: true, +6 Dmg, +6 Shield, +4 Heal, -1 CD
            ItemData buckler = new ItemData(DefaultGameCatalog.GetItemById("item_buckler")); // Armor, 10 Shield, 0 Damage
            ItemData vial = new ItemData(DefaultGameCatalog.GetItemById("item_crimson_vial")); // Potion, 12 Heal, 0 Damage

            Assert.IsTrue(grid.TryPlaceItem(idol, 1, 1, 0));
            Assert.IsTrue(grid.TryPlaceItem(buckler, 0, 1, 0)); // Left of idol
            Assert.IsTrue(grid.TryPlaceItem(vial, 2, 1, 0));    // Right of idol

            BackpackSynergyReport report = SynergyEvaluator.Evaluate(grid);
            Assert.AreEqual(10 + 6, report.ItemStats[buckler].TotalShield);
            Assert.AreEqual(0, report.ItemStats[buckler].TotalPhysicalDamage, "Pure Armor shield must not gain Physical Damage from Idol");
            Assert.AreEqual(12 + 4, report.ItemStats[vial].TotalHeal);
            Assert.AreEqual(0, report.ItemStats[vial].TotalPhysicalDamage, "Pure Healing Potion must not gain Physical Damage from Idol");
            Assert.AreEqual(1, report.ItemStats[vial].EffectiveCooldownTurns, "Idol should still reduce Vial cooldown from 2 to 1");
        }

        [Test]
        public void SynergyEvaluator_RunBuffBleedAndPoison_AppliesToStandardWeaponsAndPoisonedWeapons()
        {
            BackpackGrid grid = new BackpackGrid("BuffWeaponBag", 5, 4, 5, 4);
            ItemData sword = new ItemData(DefaultGameCatalog.GetItemById("item_iron_sword")); // BasePhys = 12, BaseBleed = 0, BasePoison = 0
            ItemData poisonFlask = new ItemData(DefaultGameCatalog.GetItemById("item_poison_flask")); // +3 StarBonusPoison to adjacent weapon

            Assert.IsTrue(grid.TryPlaceItem(sword, 1, 1, 0));
            Assert.IsTrue(grid.TryPlaceItem(poisonFlask, 2, 1, 0));

            BuffDefinitionSO razorBuff = BuffDefinitionSO.CreateRuntime("test_razor", "Razor", "+2 Bleed", CardTier.Blue, weaponDmg: 5, bleed: 2);
            BuffDefinitionSO venomBuff = BuffDefinitionSO.CreateRuntime("test_venom", "Venom", "+2 Poison", CardTier.Green, poison: 2);

            BackpackSynergyReport report = SynergyEvaluator.Evaluate(grid, new List<BuffDefinitionSO> { razorBuff, venomBuff });
            EvaluatedItemStats swordStats = report.ItemStats[sword];

            Assert.AreEqual(12 + 5, swordStats.TotalPhysicalDamage);
            Assert.AreEqual(2, swordStats.TotalBleed, "Weapon with 0 base bleed must still gain +2 Bleed from run buff");
            Assert.AreEqual(3 + 2, swordStats.TotalPoison, "Weapon poisoned by adjacent flask must also gain +2 Poison from run buff");
        }
    }
}
