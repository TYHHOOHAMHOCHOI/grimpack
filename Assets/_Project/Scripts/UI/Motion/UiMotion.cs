using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Project05.UI.Motion
{
    /// <summary>Easing curves used by <see cref="UiMotion"/> (feedback-motion.md).</summary>
    public enum Ease { Linear, OutQuad, InQuad, OutBack, OutCubic, InOutQuad }

    /// <summary>
    /// Small tween runner for UGUI feedback (design/ux/feedback-motion.md): moves, scales, punches, shakes and flashes
    /// RectTransforms on unscaled time, with positions snapped to whole canvas pixels so pixel art never smears.
    /// One instance lives on the safe frame; views receive it by reference. Display only: it never touches game state.
    /// With reduced motion on, every tween jumps to its end on the first frame (the caller's end state still shows).
    /// </summary>
    public sealed class UiMotion : MonoBehaviour
    {
        private sealed class Tween
        {
            public RectTransform Rect;
            public int Channel;          // 0 position, 1 scale, 2 shake, 3 custom
            public float Start;
            public float Duration;
            public Ease Ease;
            public Vector2 From;
            public Vector2 To;
            public float Amplitude;
            public Action<float> Custom;
            public Action Done;
            public bool Snap = true;
        }

        private readonly List<Tween> _tweens = new List<Tween>();
        private readonly List<Tween> _finished = new List<Tween>();

        /// <summary>True when reduced motion is requested; tweens then complete at once.</summary>
        public static bool Reduced => GameSettings.ReducedMotion;

        /// <summary>Global speed multiplier (1 = authored timing).</summary>
        public float Speed { get; set; } = 1f;

        /// <summary>Number of running tweens (tests).</summary>
        public int Running => _tweens.Count;

        /// <summary>Moves a rect's anchored position to <paramref name="to"/>.</summary>
        public void Move(RectTransform rt, Vector2 to, float seconds, Ease ease = Ease.OutCubic, Action done = null) =>
            Add(rt, 0, rt != null ? rt.anchoredPosition : Vector2.zero, to, seconds, ease, 0f, done);

        /// <summary>Moves from a start position to an end position.</summary>
        public void MoveFrom(RectTransform rt, Vector2 from, Vector2 to, float seconds, Ease ease = Ease.OutCubic, Action done = null)
        {
            if (rt == null) return;
            rt.anchoredPosition = Snap(from);
            Add(rt, 0, from, to, seconds, ease, 0f, done);
        }

        /// <summary>Scales a rect from one uniform scale to another.</summary>
        public void Scale(RectTransform rt, float from, float to, float seconds, Ease ease = Ease.OutBack, Action done = null) =>
            Add(rt, 1, new Vector2(from, from), new Vector2(to, to), seconds, ease, 0f, done);

        /// <summary>A quick swell and settle (pick up, land, badge pop): 1 > peak > 1.</summary>
        public void Punch(RectTransform rt, float peak = 1.12f, float seconds = 0.18f)
        {
            if (rt == null) return;
            Cancel(rt, 1);
            Add(rt, 3, Vector2.zero, Vector2.zero, seconds, Ease.Linear, peak, () => rt.localScale = Vector3.one,
                u => { float s = 1f + (peak - 1f) * Mathf.Sin(u * Mathf.PI); rt.localScale = new Vector3(s, s, 1f); });
        }

        /// <summary>A squash on landing: wide and short, then back.</summary>
        public void Squash(RectTransform rt, float amount = 0.12f, float seconds = 0.16f)
        {
            if (rt == null) return;
            Add(rt, 3, Vector2.zero, Vector2.zero, seconds, Ease.Linear, amount, () => rt.localScale = Vector3.one,
                u => { float k = amount * Mathf.Sin(u * Mathf.PI); rt.localScale = new Vector3(1f + k, 1f - k, 1f); });
        }

        /// <summary>Horizontal shake in whole pixels around the current position (invalid drop, refused action).</summary>
        public void Shake(RectTransform rt, float pixels = 3f, float seconds = 0.24f)
        {
            if (rt == null) return;
            Cancel(rt, 2);
            Vector2 rest = rt.anchoredPosition;
            Add(rt, 2, rest, rest, seconds, Ease.Linear, pixels, () => rt.anchoredPosition = rest,
                u =>
                {
                    float decay = 1f - u;
                    float x = Mathf.Round(Mathf.Sin(u * Mathf.PI * 6f) * pixels * decay);
                    rt.anchoredPosition = rest + new Vector2(x, 0f);
                });
        }

        /// <summary>A small vertical bob (hover lift / idle breathe).</summary>
        public void Lift(RectTransform rt, Vector2 rest, float pixels, float seconds = 0.08f) =>
            Add(rt, 0, rt != null ? rt.anchoredPosition : rest, rest + new Vector2(0f, pixels), seconds, Ease.OutQuad, 0f, null);

        /// <summary>Runs a custom 0..1 update for a duration (fills, counters, colour steps).</summary>
        public void Run(float seconds, Action<float> update, Action done = null, RectTransform owner = null) =>
            Add(owner, 3, Vector2.zero, Vector2.zero, seconds, Ease.Linear, 0f, done, update);

        /// <summary>Counts a number from one value to another on a TMP text or a sprite numeral.</summary>
        public void Count(Action<string> setText, int from, int to, float seconds = 0.4f, string prefix = "", string suffix = "")
        {
            if (setText == null) return;
            if (from == to || Reduced) { setText(prefix + to + suffix); return; }
            int last = int.MinValue;
            Run(seconds, u =>
            {
                int v = Mathf.RoundToInt(Mathf.Lerp(from, to, EaseOut(u)));
                if (v == last) return;
                last = v;
                setText(prefix + v + suffix);
            }, () => setText(prefix + to + suffix));
        }

        /// <summary>Swaps an image's sprite for a short flash, then back.</summary>
        public void Flash(Image img, Sprite flash, float seconds = 0.1f)
        {
            if (img == null || flash == null) return;
            Sprite rest = img.sprite;
            img.sprite = flash;
            Run(seconds, null, () => { if (img != null) img.sprite = rest; }, img.rectTransform);
        }

        /// <summary>Stops every tween on a rect (its current pose stays).</summary>
        public void Cancel(RectTransform rt, int channel = -1)
        {
            for (int i = _tweens.Count - 1; i >= 0; i--)
            {
                Tween t = _tweens[i];
                if (t.Rect != rt || (channel >= 0 && t.Channel != channel)) continue;
                _tweens.RemoveAt(i);
                if (t.Channel == 2 && rt != null) rt.anchoredPosition = t.From;
                if (t.Channel == 3 && t.Custom != null && t.Done != null) t.Done();
            }
        }

        /// <summary>Completes every running tween at once (skip).</summary>
        public void CompleteAll()
        {
            var all = new List<Tween>(_tweens);
            _tweens.Clear();
            foreach (Tween t in all) Finish(t);
        }

        private void Add(RectTransform rt, int channel, Vector2 from, Vector2 to, float seconds, Ease ease, float amp, Action done,
            Action<float> custom = null)
        {
            if (channel <= 1 && rt == null) return;
            if (channel <= 1) Cancel(rt, channel);
            var t = new Tween
            {
                Rect = rt, Channel = channel, Start = Time.unscaledTime, Duration = Mathf.Max(0.0001f, seconds),
                Ease = ease, From = from, To = to, Amplitude = amp, Custom = custom, Done = done,
            };
            if (Reduced || seconds <= 0f || !isActiveAndEnabled || !Application.isPlaying)   // edit mode (tests): end state at once
            {
                Finish(t);
                return;
            }

            _tweens.Add(t);
            Apply(t, 0f);
        }

        private void Update()
        {
            if (_tweens.Count == 0) return;
            float now = Time.unscaledTime;
            _finished.Clear();
            for (int i = 0; i < _tweens.Count; i++)
            {
                Tween t = _tweens[i];
                if (t.Channel != 3 && t.Rect == null) { _finished.Add(t); continue; }
                float u = Mathf.Clamp01((now - t.Start) * Mathf.Max(0.01f, Speed) / t.Duration);
                Apply(t, u);
                if (u >= 1f) _finished.Add(t);
            }

            foreach (Tween t in _finished)
            {
                _tweens.Remove(t);
                Finish(t);
            }
        }

        private void OnDisable() => CompleteAll();

        private static void Finish(Tween t)
        {
            try
            {
                if (t.Channel == 3 && t.Custom != null) t.Custom(1f);
                else if (t.Rect != null) Apply(t, 1f);
                if (t.Channel == 2 && t.Rect != null) t.Rect.anchoredPosition = t.From;
                t.Done?.Invoke();
            }
            catch (MissingReferenceException) { }
        }

        private static void Apply(Tween t, float u)
        {
            float e = Evaluate(t.Ease, u);
            switch (t.Channel)
            {
                case 0:
                    Vector2 p = Vector2.LerpUnclamped(t.From, t.To, e);
                    t.Rect.anchoredPosition = t.Snap ? Snap(p) : p;
                    break;
                case 1:
                    float s = Mathf.LerpUnclamped(t.From.x, t.To.x, e);
                    t.Rect.localScale = new Vector3(s, s, 1f);
                    break;
                default:
                    t.Custom?.Invoke(u);
                    break;
            }
        }

        /// <summary>Rounds a position to whole canvas pixels.</summary>
        public static Vector2 Snap(Vector2 p) => new Vector2(Mathf.Round(p.x), Mathf.Round(p.y));

        /// <summary>Evaluates an easing curve at u in 0..1.</summary>
        public static float Evaluate(Ease ease, float u)
        {
            u = Mathf.Clamp01(u);
            switch (ease)
            {
                case Ease.OutQuad: return EaseOut(u);
                case Ease.InQuad: return u * u;
                case Ease.OutCubic: { float k = 1f - u; return 1f - k * k * k; }
                case Ease.InOutQuad: return u < 0.5f ? 2f * u * u : 1f - Mathf.Pow(-2f * u + 2f, 2f) / 2f;
                case Ease.OutBack:
                {
                    const float c1 = 1.70158f, c3 = c1 + 1f;
                    float k = u - 1f;
                    return 1f + c3 * k * k * k + c1 * k * k;
                }
                default: return u;
            }
        }

        private static float EaseOut(float u) => 1f - (1f - u) * (1f - u);
    }
}
