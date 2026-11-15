using NUnit.Framework;
using Project05.UI.Hud;

namespace Project05.Tests.EditMode
{
    /// <summary>
    /// Regression tests for "placing an item needs one click too many": the release that places an item must not also
    /// pick it up again, whichever order the engine delivers the mouse state and the click event.
    /// </summary>
    public class HoldGestureTests
    {
        [Test]
        public void ClickPick_ThenClickPlace_ClickEventAfterMouseState_PlacesOnceAndDoesNotPickAgain()
        {
            var g = new HoldGesture();

            Assert.AreEqual(HoldAction.Pickup, g.Click(10, true));          // first click picks up
            g.Picked(10, true);
            Assert.AreEqual(HoldAction.None, g.MouseRelease(10));           // same frame as the pick: ignored

            Assert.AreEqual(HoldAction.Drop, g.MouseRelease(20));           // second click: the controller sees the release first
            g.Dropped(20);
            Assert.AreEqual(HoldAction.None, g.Click(20, true));            // the click event of that release must not pick again
            Assert.IsFalse(g.Held);
        }

        [Test]
        public void ClickPick_ThenClickPlace_ClickEventBeforeMouseState_PlacesOnce()
        {
            var g = new HoldGesture();
            g.Picked(10, true);

            Assert.AreEqual(HoldAction.None, g.Click(20, true));            // the click event arrives first and is left to MouseRelease
            Assert.AreEqual(HoldAction.Drop, g.MouseRelease(20));
            g.Dropped(20);

            Assert.IsFalse(g.Held);
        }

        [Test]
        public void AfterPlacing_TheNextClickOnAnItemPicksItUp()
        {
            var g = new HoldGesture();
            g.Picked(1, true);
            g.Dropped(5);

            Assert.AreEqual(HoldAction.Pickup, g.Click(9, true));
        }

        [Test]
        public void DragPickup_IsNotDroppedByAMouseRelease_OnlyByTheDragEnd()
        {
            var g = new HoldGesture();
            g.Picked(3, false);

            Assert.AreEqual(HoldAction.None, g.MouseRelease(8));
        }

        [Test]
        public void Keyboard_SubmitPicksUp_AndEnterPlacesWithoutPickingAgain()
        {
            var g = new HoldGesture();

            Assert.AreEqual(HoldAction.Pickup, g.Click(4, false));          // Enter on a focused plate arrives as a click
            g.Picked(4, true);
            Assert.AreEqual(HoldAction.None, g.ConfirmKey(4));              // the same press must not place it

            Assert.AreEqual(HoldAction.Drop, g.ConfirmKey(9));
            g.Dropped(9);
            Assert.AreEqual(HoldAction.None, g.Click(9, false));            // and its Submit click must not pick it up again
        }

        [Test]
        public void SubmitClick_WhileHeld_Places()
        {
            var g = new HoldGesture();
            g.Picked(4, true);

            Assert.AreEqual(HoldAction.Drop, g.Click(8, false));
        }
    }
}
