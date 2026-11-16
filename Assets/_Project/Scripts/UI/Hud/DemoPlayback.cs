using System.Collections.Generic;

namespace Project05.UI.Hud
{
    /// <summary>
    /// A fixture timeline that stands in for the combat presentation (Systems 27 and 29) so the speed and Skip buttons can be
    /// seen working. Speed is a multiplier on how fast a beat progresses; a speed change keeps the progress of the current beat
    /// (payoff-presentation.md rule 8). It plays no real fight and decides no outcome.
    /// </summary>
    public sealed class DemoPlayback
    {
        /// <summary>Beat speed multipliers for 1x, 2x and 4x.</summary>
        public static readonly float[] Multipliers = { 1f, 2f, 4f };

        private readonly IReadOnlyList<float> _beatSeconds;
        private float _progress;

        /// <summary>Creates a timeline from the length of each beat at 1x, in seconds.</summary>
        public DemoPlayback(IReadOnlyList<float> beatSeconds) => _beatSeconds = beatSeconds;

        /// <summary>Index of the beat now playing.</summary>
        public int Beat { get; private set; }

        /// <summary>True once the last beat has finished.</summary>
        public bool Finished { get; private set; }

        /// <summary>Progress of the current beat, 0 to 1.</summary>
        public float Progress => _progress;

        /// <summary>Starts again from the first beat.</summary>
        public void Restart()
        {
            Beat = 0;
            _progress = 0f;
            Finished = false;
        }

        /// <summary>Advances the timeline.</summary>
        /// <param name="deltaSeconds">Real time since the last call.</param>
        /// <param name="speedIndex">0, 1 or 2 for 1x, 2x or 4x.</param>
        /// <param name="paused">While true nothing advances.</param>
        /// <returns>True when the beat changed or the timeline finished.</returns>
        public bool Advance(float deltaSeconds, int speedIndex, bool paused)
        {
            if (Finished || paused || deltaSeconds <= 0f) return false;

            float multiplier = Multipliers[speedIndex < 0 ? 0 : speedIndex > 2 ? 2 : speedIndex];
            _progress += deltaSeconds * multiplier / _beatSeconds[Beat];
            bool changed = false;
            while (_progress >= 1f && !Finished)
            {
                _progress -= 1f;
                changed = true;
                if (Beat + 1 >= _beatSeconds.Count) Finished = true;
                else Beat++;
            }

            return changed;
        }

        /// <summary>Skip: lands on the final view at once. Returns the number of beats that were jumped.</summary>
        public int SkipToEnd()
        {
            int jumped = _beatSeconds.Count - Beat - 1;
            Beat = _beatSeconds.Count - 1;
            _progress = 0f;
            Finished = true;
            return jumped < 0 ? 0 : jumped;
        }
    }
}
