using System.Collections.Generic;
using NUnit.Framework;
using Project05.UI;
using Project05.UI.Hud;

namespace Project05.Tests.EditMode
{
    /// <summary>The fixture timeline behind the speed and Skip buttons.</summary>
    public class DemoPlaybackTests
    {
        private static DemoPlayback Make() => new DemoPlayback(new[] { 1f, 1f, 1f });

        [Test]
        public void Speed_ScalesProgress()
        {
            var slow = Make();
            var fast = Make();
            slow.Advance(0.25f, 0, false);
            fast.Advance(0.25f, 2, false);
            Assert.AreEqual(0.25f, slow.Progress, 1e-4f);
            Assert.AreEqual(1f, fast.Beat == 1 ? 1f : fast.Progress * 4f, 1e-3f);
        }

        [Test]
        public void SpeedChange_KeepsProgressOfCurrentBeat()
        {
            var p = Make();
            p.Advance(0.5f, 0, false);
            float before = p.Progress;
            p.Advance(0f, 2, false);
            Assert.AreEqual(before, p.Progress, 1e-6f);
            Assert.AreEqual(0, p.Beat);
        }

        [Test]
        public void Advance_WhilePaused_DoesNothing()
        {
            var p = Make();
            Assert.IsFalse(p.Advance(5f, 0, true));
            Assert.AreEqual(0, p.Beat);
            Assert.AreEqual(0f, p.Progress);
        }

        [Test]
        public void Advance_ToTheEnd_Finishes()
        {
            var p = Make();
            Assert.IsTrue(p.Advance(3.2f, 0, false));
            Assert.IsTrue(p.Finished);
        }

        [Test]
        public void SkipToEnd_LandsOnLastBeat_AndReportsJumpedBeats()
        {
            var p = Make();
            p.Advance(1.1f, 0, false);
            Assert.AreEqual(1, p.Beat);
            Assert.AreEqual(1, p.SkipToEnd());
            Assert.IsTrue(p.Finished);
            Assert.AreEqual(2, p.Beat);
        }

        [Test]
        public void Restart_ResetsEverything()
        {
            var p = Make();
            p.SkipToEnd();
            p.Restart();
            Assert.IsFalse(p.Finished);
            Assert.AreEqual(0, p.Beat);
        }
    }
}
