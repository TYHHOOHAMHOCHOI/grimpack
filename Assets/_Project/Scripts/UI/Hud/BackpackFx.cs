using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Project05.UI.Hud
{
    /// <summary>
    /// What an item plate in the backpack does during combat playback (user request 2026-11-16, art bible 5.10.9 "the grid
    /// plate pulses"): when the item fires, the plate swells slightly and shakes, then it charges: a dark checker covers the
    /// share of the plate that still has to cool down and shrinks as actions pass; a ready item shows its plate as usual.
    /// The checker is INK-0 on every other pixel, so the dimming stays in the palette (no alpha). Display only.
    /// </summary>
    public sealed class BackpackFx : MonoBehaviour
    {
        private sealed class Plate
        {
            public RectTransform Rect;
            public Image Dim;
            public float FireAt = -1f;
            public Vector2 RestPos;
            public bool Moving;
        }

        [Tooltip("Scale at the peak of the fire pulse.")]
        [SerializeField] private float pulseScale = 1.15f;

        [Tooltip("Seconds of the fire pulse at 1x.")]
        [SerializeField] private float pulseTime = 0.3f;

        private readonly Dictionary<RectTransform, Plate> _plates = new Dictionary<RectTransform, Plate>();
        private Sprite _dim;

        /// <summary>Speed multiplier of the current playback.</summary>
        public float Speed { get; set; } = 1f;

        /// <summary>Sets the checker sprite used to dim a charging plate.</summary>
        public void Initialize(Sprite dimMask) => _dim = dimMask;

        /// <summary>The item fired: swell and shake.</summary>
        public void Fire(RectTransform plate)
        {
            Plate p = Get(plate);
            if (p == null) return;
            if (!p.Moving) p.RestPos = plate.anchoredPosition;
            p.FireAt = Time.unscaledTime;
            p.Moving = true;
            plate.SetAsLastSibling();   // the swollen plate draws over its neighbours
        }

        /// <summary>Shows how much of the cooldown is left: 0 is ready (no dim), 1 is just fired (fully dimmed).</summary>
        public void SetCooldown(RectTransform plate, float remaining)
        {
            Plate p = Get(plate);
            if (p == null || _dim == null) return;
            remaining = Mathf.Clamp01(remaining);
            if (p.Dim == null) p.Dim = NewDim(plate);
            float h = plate.rect.height;
            int px = Mathf.RoundToInt(h * remaining);
            p.Dim.gameObject.SetActive(px > 0);
            RectTransform rt = p.Dim.rectTransform;
            rt.sizeDelta = new Vector2(plate.rect.width, px);   // anchored to the bottom: drains from the top as it charges
        }

        /// <summary>Removes every dim and puts every plate back (end of playback).</summary>
        public void Clear()
        {
            foreach (Plate p in _plates.Values)
            {
                if (p.Rect == null) continue;
                if (p.Dim != null) p.Dim.gameObject.SetActive(false);
                if (p.Moving)
                {
                    p.Rect.anchoredPosition = p.RestPos;
                    p.Moving = false;
                }

                p.Rect.localScale = Vector3.one;
                p.FireAt = -1f;
            }
        }

        private Plate Get(RectTransform plate)
        {
            if (plate == null) return null;
            if (!_plates.TryGetValue(plate, out Plate p))
            {
                p = new Plate { Rect = plate, RestPos = plate.anchoredPosition };
                _plates[plate] = p;
            }

            return p;
        }

        private Image NewDim(RectTransform plate)
        {
            // The plate masks its children, so the dim follows the plate's own shape (L, T and S included).
            if (plate.GetComponent<Mask>() == null)
            {
                Mask mask = plate.gameObject.AddComponent<Mask>();
                mask.showMaskGraphic = true;
            }

            var go = new GameObject("CooldownDim", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(plate, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0f, 0f);
            rt.anchoredPosition = Vector2.zero;
            var image = go.GetComponent<Image>();
            image.sprite = _dim;
            image.type = Image.Type.Tiled;
            image.raycastTarget = false;
            go.transform.SetAsLastSibling();
            return image;
        }

        private void LateUpdate()
        {
            float now = Time.unscaledTime;
            foreach (Plate p in _plates.Values)
            {
                if (!p.Moving || p.Rect == null) continue;
                float u = (now - p.FireAt) * Speed / pulseTime;
                if (u >= 1f || GameSettings.ReducedMotion)
                {
                    p.Rect.localScale = Vector3.one;
                    p.Rect.anchoredPosition = p.RestPos;
                    p.Moving = false;
                    continue;
                }

                // swell fast, settle slowly; a 1 px side-to-side shake while swollen
                float swell = u < 0.25f ? u / 0.25f : 1f - (u - 0.25f) / 0.75f;
                float s = 1f + (pulseScale - 1f) * Mathf.Clamp01(swell);
                p.Rect.localScale = new Vector3(s, s, 1f);
                int step = (int)(u * 10f);
                float shake = u < 0.7f ? (step % 2 == 0 ? 1f : -1f) : 0f;
                p.Rect.anchoredPosition = p.RestPos + new Vector2(shake, 0f);
            }
        }
    }
}
