using System.Collections.Generic;
using NUnit.Framework;
using Project05.UI;
using Project05.UI.Hud;

namespace Project05.Tests.EditMode
{
    /// <summary>In-memory settings store so tests never touch PlayerPrefs.</summary>
    internal sealed class MemoryStore : ISettingsStore
    {
        private readonly Dictionary<string, int> _values = new Dictionary<string, int>();

        public int GetInt(string key, int defaultValue) => _values.TryGetValue(key, out int v) ? v : defaultValue;

        public void SetInt(string key, int value) => _values[key] = value;

        public void Save() { }
    }

    /// <summary>Preferences are stored, clamped, and never hold Skip.</summary>
    public class GameSettingsTests
    {
        [SetUp]
        public void SetUp() => GameSettings.UseStore(new MemoryStore());

        [TearDown]
        public void TearDown() => GameSettings.UseStore(new PlayerPrefsStore());

        [Test]
        public void Defaults_FlashesOn_MotionOff_SkipEndOff_Speed1x()
        {
            Assert.IsTrue(GameSettings.ReducedFlashes);
            Assert.IsFalse(GameSettings.ReducedMotion);
            Assert.IsFalse(GameSettings.SkipGoesToEnd);
            Assert.AreEqual(0, GameSettings.SpeedIndex);
        }

        [Test]
        public void Toggles_AreRemembered()
        {
            GameSettings.ReducedFlashes = false;
            GameSettings.SkipGoesToEnd = true;
            Assert.IsFalse(GameSettings.ReducedFlashes);
            Assert.IsTrue(GameSettings.SkipGoesToEnd);
            GameSettings.ReducedMotion = false;
        }

        [Test]
        public void SpeedIndex_IsClamped_SoSkipIsNeverStored()
        {
            GameSettings.SpeedIndex = 3;
            Assert.AreEqual(2, GameSettings.SpeedIndex);
            GameSettings.SpeedIndex = -1;
            Assert.AreEqual(0, GameSettings.SpeedIndex);
        }

        [Test]
        public void ReducedMotion_DrivesTheLanternBreathing()
        {
            GameSettings.ReducedMotion = true;
            Assert.IsTrue(LanternPoolBreathing.ReducedMotion);
            GameSettings.ReducedMotion = false;
            Assert.IsFalse(LanternPoolBreathing.ReducedMotion);
        }
    }
}
