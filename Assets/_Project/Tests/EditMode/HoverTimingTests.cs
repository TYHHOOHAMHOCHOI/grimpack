using NUnit.Framework;
using Project05.UI.Hud;

namespace Project05.Tests.EditMode
{
    /// <summary>The warm window that lets popups swap between items without the hover delay.</summary>
    public class HoverTimingTests
    {
        [Test]
        public void IsWarm_NoPopupYet_IsFalse()
        {
            Assert.IsFalse(HoverTiming.IsWarm(10f, float.NegativeInfinity, 0.5f));
        }

        [Test]
        public void IsWarm_InsideTheWindow_IsTrue()
        {
            Assert.IsTrue(HoverTiming.IsWarm(10.4f, 10f, 0.5f));
        }

        [Test]
        public void IsWarm_AfterTheWindow_IsFalse()
        {
            Assert.IsFalse(HoverTiming.IsWarm(10.6f, 10f, 0.5f));
        }
    }
}
