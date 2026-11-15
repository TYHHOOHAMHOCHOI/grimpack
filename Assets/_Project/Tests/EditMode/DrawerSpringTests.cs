using NUnit.Framework;
using Project05.UI.Hud;

namespace Project05.Tests.EditMode
{
    /// <summary>The sell drawer springs out with a bounce, tucks away without one, and always comes to rest on its target.</summary>
    public class DrawerSpringTests
    {
        private const float Closed = 460f;
        private const float Open = 292f;

        private static float Run(DrawerSpring spring, float target, float frequency, float damping, out float extreme)
        {
            extreme = spring.Position;
            for (int frame = 0; frame < 180; frame++)   // three seconds at 60 fps
            {
                spring.Step(1f / 60f, target, frequency, damping);
                extreme = target < Closed ? System.Math.Min(extreme, spring.Position) : System.Math.Max(extreme, spring.Position);
            }

            return spring.Position;
        }

        [Test]
        public void Open_Underdamped_OvershootsThenSettlesOnTarget()
        {
            var spring = new DrawerSpring(Closed);

            float end = Run(spring, Open, 3.2f, 0.55f, out float furthest);

            Assert.Less(furthest, Open - 5f, "it should pass the target a little: the bounce");
            Assert.Greater(furthest, Open - 60f, "but not by much");
            Assert.AreEqual(Open, end);
            Assert.IsTrue(spring.Settled);
        }

        [Test]
        public void Close_CriticallyDamped_NeverPassesTheTarget()
        {
            var spring = new DrawerSpring(Open);

            float end = Run(spring, Closed, 4f, 1f, out float furthest);

            Assert.LessOrEqual(furthest, Closed + 0.5f);
            Assert.AreEqual(Closed, end);
        }

        [Test]
        public void Step_LargeFrameTime_StaysStable()
        {
            var spring = new DrawerSpring(Closed);

            spring.Step(0.5f, Open, 3.2f, 0.55f);

            Assert.That(spring.Position, Is.InRange(Open - 200f, Closed + 200f));
        }

        [Test]
        public void Snap_MovesAtOnceWithNoVelocity()
        {
            var spring = new DrawerSpring(Closed);

            spring.Snap(Open);

            Assert.AreEqual(Open, spring.Position);
            Assert.AreEqual(0f, spring.Velocity);
            Assert.IsTrue(spring.Settled);
        }
    }
}
