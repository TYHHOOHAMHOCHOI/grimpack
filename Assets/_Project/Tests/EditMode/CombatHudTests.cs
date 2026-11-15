using NUnit.Framework;
using Project05.UI.Hud;

namespace Project05.Tests.EditMode
{
    /// <summary>Pure rules of the combat HUD: bar widths, slot pitch and fixture shape.</summary>
    public class CombatHudTests
    {
        [TestCase(110, 110, 34)]
        [TestCase(55, 110, 17)]
        [TestCase(0, 110, 0)]
        [TestCase(-5, 110, 0)]
        [TestCase(1, 1000, 1)]
        [TestCase(999, 1000, 34)]
        public void FillWidth_ForHp_IsWholePixelsAndNeverZeroWhileAlive(int current, int max, int expected)
        {
            Assert.AreEqual(expected, HpBarView.FillWidth(current, max));
        }

        [TestCase(26, 82)]
        [TestCase(44, 100)]
        [TestCase(52, 108)]
        public void Pitch_ForBodyHeight_IsBodyPlus56(int body, int expected)
        {
            Assert.AreEqual(expected, EnemySlotView.Pitch(body));
        }

        [Test]
        public void Create_PartyActing_HasOneActingHeroWithTwoAvatars()
        {
            CombatHudData d = CombatHudFixtures.Create(HudFixture.PartyActing);

            int acting = 0;
            foreach (HeroSlotData h in d.Heroes) if (h.Acting) acting++;

            Assert.AreEqual(1, acting);
            Assert.AreEqual(2, d.Heroes[1].Avatars.Count);
        }

        [Test]
        public void Create_SixEnemies_FillsBothLinesWithoutExceedingSixSlots()
        {
            CombatHudData d = CombatHudFixtures.Create(HudFixture.SixEnemies);

            int present = 0;
            foreach (EnemySlotData e in d.Enemies) if (e.Present) present++;

            Assert.AreEqual(CombatHudData.EnemyCount, present);
        }

        [Test]
        public void Create_EnemyActing_MarksFallenHeroAtZeroHp()
        {
            CombatHudData d = CombatHudFixtures.Create(HudFixture.EnemyActing);

            Assert.IsTrue(d.Heroes[2].Fallen);
            Assert.AreEqual(0, d.Heroes[2].Hp);
        }
    }
}
