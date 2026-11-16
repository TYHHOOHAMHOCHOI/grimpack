using NUnit.Framework;
using Project05.UI;

namespace Project05.Tests.EditMode
{
    /// <summary>The cursor uses the texture pre-scaled for the canvas zoom.</summary>
    public class PixelCursorTests
    {
        [TestCase(1, 0)]
        [TestCase(3, 2)]
        [TestCase(4, 3)]
        public void TextureIndex_ZoomInRange_PicksThatZoom(int zoom, int expected)
        {
            Assert.AreEqual(expected, PixelCursor.TextureIndex(zoom, 4));
        }

        [TestCase(0, 0)]
        [TestCase(6, 3)]
        public void TextureIndex_ZoomOutOfRange_IsClamped(int zoom, int expected)
        {
            Assert.AreEqual(expected, PixelCursor.TextureIndex(zoom, 4));
        }
    }
}
