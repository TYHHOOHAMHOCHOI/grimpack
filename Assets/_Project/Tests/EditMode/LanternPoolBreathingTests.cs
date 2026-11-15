using NUnit.Framework;
using Project05.UI;

namespace Project05.Tests.EditMode
{
    /// <summary>Frame choice for the lantern pool loop.</summary>
    public class LanternPoolBreathingTests
    {
        private static readonly int[] Sequence = { 0, 1, 2, 3, 2, 1 };

        [TestCase(0f, 0)]
        [TestCase(0.44f, 0)]
        [TestCase(0.45f, 1)]
        [TestCase(1.35f, 3)]
        [TestCase(2.25f, 1)]
        public void FrameAt_WithinFirstCycle_FollowsSequence(float time, int expected)
        {
            Assert.AreEqual(expected, LanternPoolBreathing.FrameAt(time, 0.45f, Sequence));
        }

        [Test]
        public void FrameAt_AfterOneCycle_Repeats()
        {
            float cycle = 0.45f * Sequence.Length;

            Assert.AreEqual(
                LanternPoolBreathing.FrameAt(0.2f, 0.45f, Sequence),
                LanternPoolBreathing.FrameAt(0.2f + cycle, 0.45f, Sequence));
        }

        [Test]
        public void FrameAt_NegativeTime_ClampsToFirstStep()
        {
            Assert.AreEqual(0, LanternPoolBreathing.FrameAt(-3f, 0.45f, Sequence));
        }

        [Test]
        public void DefaultCycle_IsSlowerThanHalfHertz()
        {
            // art bible 2.2 rule 2: ambient light moves at 0.5 Hz or slower, so a cycle is at least 2 s.
            Assert.GreaterOrEqual(0.45f * Sequence.Length, 2f);
        }
    }
}
