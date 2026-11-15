using NUnit.Framework;
using Project05.UI;

namespace Project05.Tests.EditMode
{
    /// <summary>Integer zoom rule from art bible 8.1: z = max(1, min(floor(W/640), floor(H/360))).</summary>
    public class IntegerCanvasScaleTests
    {
        [TestCase(1280, 720, 2)]
        [TestCase(1920, 1080, 3)]
        [TestCase(2560, 1440, 4)]
        [TestCase(3840, 2160, 6)]
        public void ComputeZoom_StandardDisplays_ReturnsTableValue(int width, int height, int expected)
        {
            Assert.AreEqual(expected, IntegerCanvasScale.ComputeZoom(width, height));
        }

        [TestCase(1366, 768, 2)]
        [TestCase(1920, 1200, 3)]
        [TestCase(3440, 1440, 4)]
        public void ComputeZoom_NonMultipleDisplays_RoundsDown(int width, int height, int expected)
        {
            Assert.AreEqual(expected, IntegerCanvasScale.ComputeZoom(width, height));
        }

        [TestCase(640, 360)]
        [TestCase(320, 180)]
        [TestCase(0, 0)]
        public void ComputeZoom_SmallerThanNative_NeverBelowOne(int width, int height)
        {
            Assert.AreEqual(1, IntegerCanvasScale.ComputeZoom(width, height));
        }
    }
}
