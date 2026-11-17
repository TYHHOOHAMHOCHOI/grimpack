using System;
using UnityEngine;
using UnityEngine.UI;

namespace Project05.UI.Motion
{
    /// <summary>
    /// The reactions to player actions and combat beats (design/ux/feedback-motion.md), built on <see cref="UiMotion"/>
    /// and <see cref="UiBurst"/>: coin sprays and fliers, sparks, pixel bursts, rings, thunks and shudders. Positions are
    /// safe-frame pixels (x right, y down, top-left origin). Display only: it never changes game state, and with reduced
    /// motion every reaction collapses to its end state (see <see cref="UiMotion.Reduced"/>).
    /// </summary>
    public sealed class FeedbackFx : MonoBehaviour
    {
        private UiMotion _motion;
        private UiBurst _burst;
        private RectTransform _layer;
        private Func<string, Sprite> _sprite;

        /// <summary>The tween runner (shared with the views).</summary>
        public UiMotion Motion => _motion;

        /// <summary>The particle layer (tests).</summary>
        public UiBurst Burst => _burst;

        /// <summary>Raised with a sound id for every reaction (feedback-motion.md G14); audio subscribes later.</summary>
        public event Action<string> SoundRequested;

        /// <summary>Creates the particle layer over the safe frame; sprites are looked up by name (the generated kit).</summary>
        public static FeedbackFx Create(RectTransform safeFrame, UiMotion motion, Func<string, Sprite> sprite)
        {
            var go = new GameObject("FeedbackFx", typeof(RectTransform));
            go.transform.SetParent(safeFrame, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(640f, 360f);
            var fx = go.AddComponent<FeedbackFx>();
            fx._motion = motion;
            fx._burst = go.AddComponent<UiBurst>();
            fx._layer = rt;
            fx._sprite = sprite;
            return fx;
        }

        /// <summary>Keeps the particle layer above every panel.</summary>
        public void BringToFront()
        {
            if (_layer != null) _layer.SetAsLastSibling();
        }

        private Sprite[] Frames(string key, int count)
        {
            var list = new Sprite[count];
            for (int i = 0; i < count; i++) list[i] = _sprite?.Invoke(key + "_" + (i + 1).ToString("00"));
            return list[0] != null ? list : null;
        }

        private static Vector2 At(Vector2 frame) => new Vector2(frame.x, -frame.y);

        /// <summary>A world point (any canvas child) as safe-frame pixels (x right, y down).</summary>
        public Vector2 FromWorld(Vector3 world)
        {
            Vector2 local = _layer.InverseTransformPoint(world);
            return new Vector2(Mathf.Round(local.x), Mathf.Round(-local.y));
        }

        /// <summary>P-10 sell: coins spray up out of the zone mouth, arc, fall and blink out.</summary>
        public void CoinSpray(Vector2 frame, int price)
        {
            BringToFront();
            int count = Mathf.Clamp(Mathf.CeilToInt(price / 2f), 3, 12);
            _burst.Burst(At(frame), Frames("vfx_gold-coin", 4), count, 140f, 180f, 60f, 120f, 700f, 0.5f);
            SoundRequested?.Invoke("ui.sell.burst");
        }

        /// <summary>R-10 / forge cost: coins hop from one point to another, staggered; <paramref name="arrive"/> once all landed.</summary>
        public void CoinFlight(Vector2 from, Vector2 to, int count, Action arrive)
        {
            BringToFront();
            Sprite[] coin = Frames("vfx_gold-coin", 4);
            count = Mathf.Clamp(count, 1, 10);
            if (coin == null || UiMotion.Reduced)
            {
                arrive?.Invoke();
                return;
            }

            int landed = 0;
            for (int i = 0; i < count; i++)
            {
                Vector2 jitter = new Vector2((i % 3 - 1) * 4f, (i % 2) * 3f);
                float delay = 0.04f * i;
                _motion.Run(delay, null, () => _burst.Fly(At(from + jitter), At(to), coin, 0.28f, 18f, () =>
                {
                    landed++;
                    if (landed == count) arrive?.Invoke();
                }));
            }

            SoundRequested?.Invoke("ui.gold.gain");
        }

        /// <summary>P-11 forge: sparks up and out from the anvil.</summary>
        public void Sparks(Vector2 frame)
        {
            BringToFront();
            _burst.Burst(At(frame), Frames("vfx_spark", 2), 10, 70f, 130f, 20f, 160f, 380f, 0.3f);
            SoundRequested?.Invoke("ui.forge.strike");
        }

        /// <summary>Pixel burst: hits (Bone / Fog-hi sets), death (Fog), poison (Moss).</summary>
        public void Pixels(Vector2 frame, string set, int count, float up = 1f)
        {
            BringToFront();
            _burst.Burst(At(frame), Frames("vfx_px-" + set, 2), count, 30f, 90f * up, up > 0.5f ? 30f : 0f, up > 0.5f ? 150f : 180f,
                up > 0.5f ? -10f : 260f, 0.45f, 0.8f);
        }

        /// <summary>C-14: the body dissolves into pixels that drift up while its image steps out in 3 alpha steps.</summary>
        public void Dissolve(Vector2 frame, Graphic body)
        {
            BringToFront();
            _burst.Burst(At(frame), Frames("vfx_px-fog", 2), 12, 10f, 40f, 70f, 110f, -30f, 0.5f, 0.5f);
            if (body != null)
            {
                Color c = body.color;
                _motion.Run(0.2f, u => { if (body != null) body.color = new Color(c.r, c.g, c.b, 1f - Mathf.Floor(u * 3f) / 3f); },
                    () => { if (body != null) body.color = new Color(c.r, c.g, c.b, 0f); });
            }

            SoundRequested?.Invoke("cbt.enemy.die");
        }

        /// <summary>P-06: a 1 px thunk down and back on landing.</summary>
        public void Thunk(RectTransform rt)
        {
            if (rt == null) return;
            Vector2 rest = rt.anchoredPosition;
            _motion.MoveFrom(rt, rest + new Vector2(0f, -1f), rest, 0.05f, Ease.Linear);
        }

        /// <summary>P-07 / P-08 / P-12: a 2 px shudder, two cycles.</summary>
        public void Shudder(RectTransform rt)
        {
            if (rt == null) return;
            _motion.Shake(rt, 2f, 0.16f);
            SoundRequested?.Invoke("ui.item.drop-invalid");
        }

        /// <summary>A Candle ring around a rect for a moment (the HL fallback and the landing pulse).</summary>
        public void RingPulse(RectTransform target, float seconds = 0.15f, int times = 1)
        {
            if (target == null || !Application.isPlaying) return;   // edit mode (tests): no transient objects
            Sprite ring = _sprite?.Invoke("ui_ring-gen_hover_9s");
            if (ring == null) return;
            var go = new GameObject("RingPulse", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(target, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(-2f, -2f);
            rt.offsetMax = new Vector2(2f, 2f);
            var img = go.GetComponent<Image>();
            img.sprite = ring;
            img.type = Image.Type.Sliced;
            img.raycastTarget = false;
            float total = seconds * (times * 2 - 1);
            _motion.Run(total, u =>
            {
                if (img == null) return;
                int phase = Mathf.Min(times * 2 - 2, (int)(u * (times * 2 - 1)));
                img.enabled = phase % 2 == 0;
            }, () => { if (go != null) Destroy(go); });
        }

        /// <summary>Counts a number from one value to another (gold, HP readouts).</summary>
        public void Tick(Action<string> set, int from, int to, float seconds = 0.3f)
        {
            _motion.Count(set, from, to, seconds);
            if (from != to) SoundRequested?.Invoke("ui.gold.tick");
        }

        /// <summary>U-04: a panel slides in from just off the nearest edge (200 ms).</summary>
        public void SlideIn(RectTransform rt, Vector2 offset, float seconds = 0.2f)
        {
            if (rt == null) return;
            Vector2 rest = rt.anchoredPosition;
            _motion.MoveFrom(rt, rest + offset, rest, seconds, Ease.OutCubic);
            SoundRequested?.Invoke("ui.panel.open");
        }
    }
}
