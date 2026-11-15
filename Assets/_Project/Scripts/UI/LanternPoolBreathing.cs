using UnityEngine;
using UnityEngine.UI;

namespace Project05.UI
{
    /// <summary>
    /// Makes the lantern's light pool breathe by cycling pre-authored overlay frames
    /// (art bible 2.2 rule 2: ambient light moves at 0.5 Hz or slower, with no alpha or blur).
    /// The frames come from tools/art/build_title_light.py and each is palette-exact.
    /// With reduced motion on, the pool holds its resting frame.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public sealed class LanternPoolBreathing : MonoBehaviour
    {
        /// <summary>Set by the Settings system (System 37) when the player asks for reduced motion.</summary>
        public static bool ReducedMotion { get; set; }

        [SerializeField] private Sprite[] frames;

        [Tooltip("Order the frames play in, as indices into Frames. Defaults to shrink, rest, grow, grow more, grow, rest.")]
        [SerializeField] private int[] sequence = { 0, 1, 2, 3, 2, 1 };

        [Tooltip("Seconds each step is shown. 0.45 s over six steps is a 2.7 s cycle, slower than the 0.5 Hz limit.")]
        [SerializeField] private float secondsPerStep = 0.45f;

        [Tooltip("Index into Frames shown when motion is reduced.")]
        [SerializeField] private int restingFrame = 1;

        private Image _image;

        /// <summary>
        /// Picks the step shown at a given time. Pure, so it is testable without a scene.
        /// </summary>
        /// <param name="time">Time in seconds since the loop started.</param>
        /// <param name="secondsPerStep">Seconds each step lasts. Must be positive.</param>
        /// <param name="sequence">Frame indices in play order. Must not be empty.</param>
        /// <returns>The frame index to show.</returns>
        public static int FrameAt(float time, float secondsPerStep, int[] sequence)
        {
            int step = Mathf.FloorToInt(Mathf.Max(0f, time) / secondsPerStep);
            return sequence[step % sequence.Length];
        }

        private void Awake()
        {
            _image = GetComponent<Image>();
        }

        private void Update()
        {
            if (frames == null || frames.Length == 0 || sequence == null || sequence.Length == 0) return;

            int index = ReducedMotion ? restingFrame : FrameAt(Time.unscaledTime, secondsPerStep, sequence);
            if (index >= 0 && index < frames.Length && _image.sprite != frames[index])
            {
                _image.sprite = frames[index];
            }
        }
    }
}
