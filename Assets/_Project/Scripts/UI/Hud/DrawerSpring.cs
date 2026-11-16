using System;

namespace Project05.UI.Hud
{
    /// <summary>
    /// A damped spring that moves a drawer toward its target, so it springs out with a small overshoot instead of appearing.
    /// Engine-free so it can be tested. Positions are native px; the caller rounds them when drawing.
    /// </summary>
    public sealed class DrawerSpring
    {
        private const float MaxStep = 1f / 240f;

        /// <summary>Current position.</summary>
        public float Position { get; private set; }

        /// <summary>Current velocity, px per second.</summary>
        public float Velocity { get; private set; }

        /// <summary>True when the drawer is at rest on its target.</summary>
        public bool Settled { get; private set; } = true;

        /// <summary>Creates a spring resting at a position.</summary>
        public DrawerSpring(float position) => Position = position;

        /// <summary>Jumps to a position with no motion (reduced motion, or the initial layout).</summary>
        public void Snap(float position)
        {
            Position = position;
            Velocity = 0f;
            Settled = true;
        }

        /// <summary>Advances the spring.</summary>
        /// <param name="dt">Seconds since the last step.</param>
        /// <param name="target">Where the drawer should come to rest.</param>
        /// <param name="frequency">Natural frequency in Hz (how fast it moves).</param>
        /// <param name="dampingRatio">Below 1 overshoots and settles (a bounce); 1 or more never overshoots.</param>
        public void Step(float dt, float target, float frequency, float dampingRatio)
        {
            if (dt <= 0f) return;
            float omega = 2f * (float)Math.PI * frequency;
            float k = omega * omega;
            float c = 2f * dampingRatio * omega;
            while (dt > 0f)
            {
                float h = Math.Min(dt, MaxStep);
                Velocity += (-k * (Position - target) - c * Velocity) * h;   // semi-implicit Euler: stable for these rates
                Position += Velocity * h;
                dt -= h;
            }

            Settled = Math.Abs(Position - target) < 0.25f && Math.Abs(Velocity) < 4f;
            if (Settled) Snap(target);
        }
    }
}
