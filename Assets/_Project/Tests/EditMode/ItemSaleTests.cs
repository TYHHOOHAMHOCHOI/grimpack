using NUnit.Framework;
using Project05.Items;

namespace Project05.Tests.EditMode
{
    /// <summary>Sell price lookup and removing a sold item from the board.</summary>
    public class ItemSaleTests
    {
        private static readonly int[] Table = { 1, 2, 3, 5, 8, 12 };

        [TestCase("White", 1)]
        [TestCase("green", 2)]
        [TestCase("Purple", 5)]
        [TestCase("Red", 12)]
        public void Price_KnownTier_ReadsTheTable(string tier, int expected)
        {
            Assert.AreEqual(expected, ItemSale.Price(tier, Table));
        }

        [Test]
        public void Price_UnknownTierOrShortTable_IsZero()
        {
            Assert.AreEqual(0, ItemSale.Price("Mythic", Table));
            Assert.AreEqual(0, ItemSale.Price("Red", new[] { 1, 2 }));
            Assert.AreEqual(0, ItemSale.Price("White", null));
        }

        [Test]
        public void Price_NegativeEntry_IsClampedToZero()
        {
            Assert.AreEqual(0, ItemSale.Price("White", new[] { -4 }));
        }

        [Test]
        public void Remove_SoldItem_FreesItsCells()
        {
            var board = new ItemBoard();
            board.AddContainer("stash", 2, 1, false);
            var record = new ItemRecord { id = "item_test", tier = "White", cells = new[] { new CellRecord { x = 0, y = 0 } } };
            BoardItem sold = board.Add(record, "stash", 0, 0, 0);

            Assert.IsTrue(board.Remove(sold));

            Assert.IsNull(board.Occupant("stash", 0, 0));
            Assert.AreEqual(0, board.Items.Count);
            Assert.IsFalse(board.Remove(sold));
        }
    }
}
