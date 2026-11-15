using NUnit.Framework;
using Project05.Items;
using Project05.UI.Hud;

namespace Project05.Tests.EditMode
{
    /// <summary>The workbench column fits a 4 x 4 to 7 x 7 hero grid and an 8 x 8 stash; the board can grow the grid.</summary>
    public class WorkbenchLayoutTests
    {
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(7)]
        public void For_EveryGridSize_StashFitsUnderTheGridInsideTheColumn(int size)
        {
            WorkbenchRects r = WorkbenchLayout.For(size);

            float gridBottom = r.GridOrigin.y + size * WorkbenchLayout.Cell;
            float stashBottom = r.StashOrigin.y + WorkbenchLayout.Stash * WorkbenchLayout.Cell;
            Assert.Greater(r.SectionLineY, gridBottom);
            Assert.Greater(r.StashOrigin.y, r.StashLabelY + 11);
            Assert.LessOrEqual(stashBottom, WorkbenchLayout.Bottom);
        }

        [Test]
        public void For_OutOfRange_IsClampedToFourAndSeven()
        {
            Assert.AreEqual(WorkbenchLayout.For(4).StashOrigin, WorkbenchLayout.For(1).StashOrigin);
            Assert.AreEqual(WorkbenchLayout.For(7).StashOrigin, WorkbenchLayout.For(12).StashOrigin);
        }

        [Test]
        public void Resize_Grow_AllowsPlacingInTheNewCells()
        {
            var board = new ItemBoard();
            board.AddContainer("grid", 4, 4, true);
            var record = new ItemRecord { id = "item_test", tier = "White", cells = new[] { new CellRecord { x = 0, y = 0 } } };

            Assert.IsNull(board.Add(record, "grid", 6, 6, 0));
            Assert.IsTrue(board.Resize("grid", 7, 7));
            Assert.IsNotNull(board.Add(record, "grid", 6, 6, 0));
            Assert.AreEqual((7, 7), board.SizeOf("grid"));
        }

        [Test]
        public void Resize_ShrinkCuttingAnItem_IsRefusedAndChangesNothing()
        {
            var board = new ItemBoard();
            board.AddContainer("grid", 7, 7, true);
            var record = new ItemRecord { id = "item_test", tier = "White", cells = new[] { new CellRecord { x = 0, y = 0 } } };
            board.Add(record, "grid", 5, 5, 0);

            Assert.IsFalse(board.Resize("grid", 4, 4));
            Assert.AreEqual((7, 7), board.SizeOf("grid"));
        }
    }
}
