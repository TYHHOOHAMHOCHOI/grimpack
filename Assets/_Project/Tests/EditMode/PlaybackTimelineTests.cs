using NUnit.Framework;
using Project05.UI.Hud;

namespace Project05.Tests.EditMode
{
    /// <summary>
    /// Pacing of combat playback (art bible 5.10.6): each item avatar holds long enough to be read, its effects land at its
    /// contact, the next avatar overlaps the previous one's travel, and speed divides every time.
    /// </summary>
    public class PlaybackTimelineTests
    {
        private static PlaybackBeat Start(bool enemy = false, Archetype a = Archetype.Swing) =>
            new PlaybackBeat { Kind = BeatKind.ActionStart, Enemy = enemy, Archetype = a, Source = -1 };

        private static PlaybackBeat Fire(Archetype a) => new PlaybackBeat { Kind = BeatKind.ItemFire, Archetype = a, Source = -1 };

        private static PlaybackBeat Effect(BeatKind k, int source) => new PlaybackBeat { Kind = k, Source = source };

        // hero: sword (swing, 2 hits), crossbow (throw, 1 hit), then the enemy attacks once
        private static readonly PlaybackBeat[] Fight =
        {
            Start(), Fire(Archetype.Swing), Effect(BeatKind.Hit, 1), Effect(BeatKind.Hit, 1),
            Fire(Archetype.Throw), Effect(BeatKind.Hit, 4),
            Start(true), Effect(BeatKind.Hit, 6),
        };

        [Test]
        public void Schedule_FirstAvatarPopsInAfterTheReadyLift()
        {
            var p = new PlaybackPacing();

            float[] t = PlaybackTimeline.Schedule(Fight, p, 0);

            Assert.AreEqual(p.ready, t[1], 1e-5f);
        }

        [Test]
        public void Schedule_HitsLandAtContact_AfterTheAvatarWasReadable()
        {
            var p = new PlaybackPacing();

            float[] t = PlaybackTimeline.Schedule(Fight, p, 0);

            Assert.AreEqual(t[1] + p.Contact(Archetype.Swing), t[2], 1e-5f);
            Assert.GreaterOrEqual(t[2] - t[1], p.popIn + p.readHold, "the avatar is shown before it acts");
            Assert.AreEqual(t[2] + p.hitGap, t[3], 1e-5f);
        }

        [Test]
        public void Schedule_NextAvatarOverlapsThePreviousTravel()
        {
            var p = new PlaybackPacing();

            float[] t = PlaybackTimeline.Schedule(Fight, p, 0);

            Assert.AreEqual(t[1] + p.Cadence(Archetype.Swing), t[4], 1e-5f);
            Assert.Less(t[4], t[3], "the crossbow pops in before the sword's second hit lands");
        }

        [Test]
        public void Schedule_EnemyActionStartsAHandoffAfterTheLastLanding()
        {
            var p = new PlaybackPacing();

            float[] t = PlaybackTimeline.Schedule(Fight, p, 0);

            Assert.AreEqual(t[5] + p.handoff, t[6], 1e-5f);
            Assert.AreEqual(t[6] + p.ready + p.Contact(Archetype.Swing), t[7], 1e-5f);
        }

        [TestCase(1, 2f)]
        [TestCase(2, 4f)]
        public void Schedule_FasterSpeed_DividesEveryTime(int speedIndex, float factor)
        {
            var p = new PlaybackPacing();

            float[] slow = PlaybackTimeline.Schedule(Fight, p, 0);
            float[] fast = PlaybackTimeline.Schedule(Fight, p, speedIndex);

            for (int i = 0; i < slow.Length; i++) Assert.AreEqual(slow[i] / factor, fast[i], 1e-5f);
        }

        [Test]
        public void PlayOrder_SortsByTime_KeepingTraceOrderOnTies()
        {
            int[] order = PlaybackTimeline.PlayOrder(new[] { 0f, 0.5f, 0.3f, 0.3f });

            CollectionAssert.AreEqual(new[] { 0, 2, 3, 1 }, order);
        }
    }
}
