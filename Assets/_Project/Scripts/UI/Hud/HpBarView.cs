using UnityEngine;
using UnityEngine.UI;

namespace Project05.UI.Hud
{
    /// <summary>
    /// HP row: bar (Blood on an INK-0 track, INK-4 border), lost-HP trail (Blood-D) and the numerals
    /// beside the bar (art bible 4.6, 7.4). Low HP swaps the frame for one with a 1 px Bone outline.
    /// </summary>
    public sealed class HpBarView : MonoBehaviour
    {
        /// <summary>Width in native pixels of the bar interior.</summary>
        public const int InnerWidth = 34;

        [SerializeField] private Image frame;
        [SerializeField] private Image fill;
        [SerializeField] private Image trail;
        [SerializeField] private Sprite frameNormal;
        [SerializeField] private Sprite frameLow;
        [SerializeField] private SpriteNumeralText value;

        [Tooltip("Share of max HP at or below which the low-HP frame is used.")]
        [SerializeField, Range(0f, 1f)] private float lowThreshold = 0.3f;

        [Tooltip("Seconds the lost-HP trail holds before it drains, so the size of a hit can be read.")]
        [SerializeField] private float trailHold = 0.28f;

        [Tooltip("Native px per second the lost-HP trail drains at.")]
        [SerializeField] private float trailDrain = 70f;

        private int _lastHp = -1;
        private int _lastMax = -1;
        private float _trailWidth;
        private float _drainFrom;

        /// <summary>Whole-pixel width of the fill for a given HP. Pure, so it can be tested without a scene.</summary>
        /// <param name="current">Current HP, zero or more.</param>
        /// <param name="max">Max HP, at least 1.</param>
        /// <returns>0 when empty, otherwise at least 1 and at most <see cref="InnerWidth"/>.</returns>
        public static int FillWidth(int current, int max)
        {
            if (current <= 0 || max <= 0) return 0;
            int w = Mathf.RoundToInt(InnerWidth * Mathf.Clamp01((float)current / max));
            return Mathf.Clamp(w, 1, InnerWidth);
        }

        /// <summary>Shows HP. A previous value larger than the current one leaves a Blood-D trail segment.</summary>
        /// <param name="current">Current HP.</param>
        /// <param name="max">Max HP.</param>
        /// <param name="previous">HP before the last change, or a negative number for none.</param>
        public void SetHp(int current, int max, int previous = -1)
        {
            if (max < 1) max = 1;
            current = Mathf.Clamp(current, 0, max);
            int fillWidth = FillWidth(current, max);
            SetWidth(fill, fillWidth);

            // In play, a drop leaves a Blood-D trail at the old width that holds, then drains down to the new fill, so
            // a hit reads as a chunk taken out of the bar rather than a jump.
            if (Application.isPlaying)
            {
                if (previous < 0 && _lastHp >= 0 && max == _lastMax && current < _lastHp) previous = _lastHp;
                if (previous > current)
                {
                    _trailWidth = Mathf.Max(_trailWidth, FillWidth(previous, max));
                    _drainFrom = Time.unscaledTime + trailHold;
                }

                if (_trailWidth < fillWidth) _trailWidth = 0f;
                SetWidth(trail, Mathf.RoundToInt(_trailWidth));
            }
            else
            {
                SetWidth(trail, previous > current ? FillWidth(previous, max) : 0);
            }

            _lastHp = current;
            _lastMax = max;
            if (frame != null)
            {
                bool low = current > 0 && (float)current / max <= lowThreshold;
                frame.sprite = low && frameLow != null ? frameLow : frameNormal;
            }

            if (value != null) value.Text = current + "/" + max;
        }

        private void Update()
        {
            if (_trailWidth <= 0f || Time.unscaledTime < _drainFrom) return;
            float target = fill != null && fill.enabled ? fill.rectTransform.sizeDelta.x : 0f;
            _trailWidth = Mathf.MoveTowards(_trailWidth, target, trailDrain * Time.unscaledDeltaTime);
            if (_trailWidth <= target + 0.01f) _trailWidth = 0f;
            SetWidth(trail, Mathf.RoundToInt(_trailWidth));
        }

        private static void SetWidth(Image image, int width)
        {
            if (image == null) return;
            image.enabled = width > 0;
            RectTransform rt = image.rectTransform;
            rt.sizeDelta = new Vector2(width, rt.sizeDelta.y);
        }
    }
}
