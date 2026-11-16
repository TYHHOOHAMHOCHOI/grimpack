using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Project05.Items;

namespace Project05.Tests.EditMode
{
    /// <summary>Rotation, placement and collision rules of the item board.</summary>
    public class ItemBoardTests
    {
        private static ItemCatalog Catalog() => ItemCatalog.Load(File.ReadAllText("Assets/_Project/Data/items_mvp.json"));

        private static ItemBoard NewBoard()
        {
            var b = new ItemBoard();
            b.AddContainer("grid", 4, 4, true);
            b.AddContainer("stash", 8, 2, false);
            return b;
        }

        [Test]
        public void Rotate_VerticalThreeCells_BecomesHorizontal()
        {
            CellRecord[] sword = Catalog().Find("item_iron_sword").cells;

            (int w, int h) = ItemShapes.Size(ItemShapes.Rotate(sword, 1));

            Assert.AreEqual((3, 1), (w, h));
        }

        [Test]
        public void Rotate_FourQuarterTurns_ReturnsTheOriginalFootprint()
        {
            CellRecord[] hammer = Catalog().Find("item_war_hammer").cells;

            CellRecord[] turned = ItemShapes.Rotate(hammer, 4);

            CollectionAssert.AreEquivalent(
                System.Array.ConvertAll(hammer, c => (c.x, c.y)),
                System.Array.ConvertAll(turned, c => (c.x, c.y)));
        }

        [Test]
        public void Rotate_AlwaysTouchesTheOriginAxes()
        {
            foreach (ItemRecord item in Catalog().All)
            {
                for (int turns = 0; turns < 4; turns++)
                {
                    CellRecord[] cells = ItemShapes.Rotate(item.cells, turns);
                    int minX = int.MaxValue;
                    int minY = int.MaxValue;
                    foreach (CellRecord c in cells)
                    {
                        if (c.x < minX) minX = c.x;
                        if (c.y < minY) minY = c.y;
                    }

                    Assert.AreEqual((0, 0), (minX, minY), item.id + " turns " + turns);
                }
            }
        }

        [Test]
        public void Rotate_ClockwiseOnScreen_MovesTheLFootToTheRight()
        {
            // boots: shaft down the left (0,0)(0,1) and foot (1,1). A quarter turn clockwise puts the foot at the top left
            CellRecord[] boots = Catalog().Find("item_swift_boots").cells;

            CellRecord[] turned = ItemShapes.Rotate(boots, 1);

            Assert.AreEqual(3, turned.Length);
            Assert.AreEqual((2, 2), ItemShapes.Size(turned));
        }

        [Test]
        public void Add_OutsideTheContainer_IsRefused()
        {
            ItemBoard board = NewBoard();

            Assert.IsNull(board.Add(Catalog().Find("item_iron_sword"), "grid", 0, 2, 0));   // 3 tall from row 2 leaves a 4 row grid
        }

        [Test]
        public void Move_OntoAnotherItem_IsBlockedAndChangesNothing()
        {
            ItemBoard board = NewBoard();
            ItemCatalog c = Catalog();
            BoardItem sword = board.Add(c.Find("item_iron_sword"), "grid", 0, 0, 0);
            BoardItem dagger = board.Add(c.Find("item_dagger"), "grid", 3, 3, 0);

            PlaceResult result = board.Move(dagger, "grid", 0, 1, 0);

            Assert.AreEqual(PlaceResult.Blocked, result);
            Assert.AreEqual((3, 3), (dagger.X, dagger.Y));
            Assert.IsNotNull(sword);
        }

        [Test]
        public void Move_ToItsOwnCells_IsValid()
        {
            ItemBoard board = NewBoard();
            BoardItem sword = board.Add(Catalog().Find("item_iron_sword"), "grid", 0, 0, 0);

            Assert.AreEqual(PlaceResult.Ok, board.Move(sword, "grid", 0, 0, 0));
        }

        [Test]
        public void Move_RotatingIntoAWall_IsOutOfBounds()
        {
            ItemBoard board = NewBoard();
            BoardItem sword = board.Add(Catalog().Find("item_iron_sword"), "grid", 3, 0, 0);

            Assert.AreEqual(PlaceResult.OutOfBounds, board.Move(sword, "grid", 3, 0, 1));   // 3 wide from column 3
            Assert.AreEqual(0, sword.Rotation);
        }

        [Test]
        public void Check_ListsTheBlockingCellsForTheGhost()
        {
            ItemBoard board = NewBoard();
            ItemCatalog c = Catalog();
            board.Add(c.Find("item_whetstone"), "grid", 1, 1, 0);
            BoardItem drum = board.Add(c.Find("item_war_drum"), "grid", 3, 0, 0);
            var bad = new List<(int x, int y)>();

            PlaceResult result = board.Check(drum, "grid", 1, 0, 0, bad);

            Assert.AreEqual(PlaceResult.Blocked, result);
            CollectionAssert.AreEqual(new[] { (1, 1) }, bad);
        }

        [Test]
        public void Evaluate_AfterMovingNextToAWhetstone_TheBladeGainsDamage()
        {
            ItemBoard board = NewBoard();
            ItemCatalog c = Catalog();
            BoardItem stone = board.Add(c.Find("item_whetstone"), "grid", 0, 0, 0);
            BoardItem dagger = board.Add(c.Find("item_dagger"), "grid", 3, 3, 0);
            Assert.AreEqual(0, ItemSynergy.Sum(board.Evaluate("grid")[dagger], "DamageFlat"));

            board.Move(dagger, "grid", 1, 0, 0);

            Assert.AreEqual(3, ItemSynergy.Sum(board.Evaluate("grid")[dagger], "DamageFlat"));
            Assert.IsNotNull(stone);
        }

        [Test]
        public void Evaluate_RotationChangesAdjacency()
        {
            ItemBoard board = NewBoard();
            ItemCatalog c = Catalog();
            board.Add(c.Find("item_whetstone"), "grid", 1, 2, 0);
            BoardItem sword = board.Add(c.Find("item_iron_sword"), "grid", 0, 0, 0);   // column 0, rows 0 to 2: touches (1,2)
            Assert.AreEqual(3, ItemSynergy.Sum(board.Evaluate("grid")[sword], "DamageFlat"));

            board.Move(sword, "grid", 0, 0, 1);   // now row 0, columns 0 to 2: no longer touches (1,2)

            Assert.AreEqual(0, ItemSynergy.Sum(board.Evaluate("grid")[sword], "DamageFlat"));
        }

        [Test]
        public void Evaluate_StashGivesAndReceivesNothing()
        {
            ItemBoard board = NewBoard();
            ItemCatalog c = Catalog();
            board.Add(c.Find("item_whetstone"), "stash", 0, 0, 0);
            board.Add(c.Find("item_dagger"), "stash", 1, 0, 0);

            Assert.AreEqual(0, board.Evaluate("stash").Count);
        }
    }
}
