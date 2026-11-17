using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Project05.UI.Motion
{
    /// <summary>
    /// Pooled sprite particles for UI feedback (design/ux/feedback-motion.md): bursts (coins out of the sell zone, anvil
    /// sparks, landing dust, hit splatter, death dissolve) and fliers (a coin or an item arcing from one point to another).
    /// Each particle is an Image on this layer, moved in whole canvas pixels on unscaled time and animated by its frames.
    /// The randomness is display-only (System.Random seeded per burst) and never touches gameplay.
    /// </summary>
    public sealed class UiBurst : MonoBehaviour
    {
        private sealed class Particle
        {
            public Image Image;
            public RectTransform Rect;
            public Sprite[] Frames;
            public float Born;
            public float Life;
            public Vector2 Pos;
            public Vector2 Vel;
            public float Gravity;
            public float Drag;
            // flier
            public bool Flier;
            public Vector2 From;
            public Vector2 To;
            public float Arc;
            public Action Arrive;
            public bool Loop;
        }

        [Tooltip("Most particles alive at once; older ones are recycled first.")]
        [SerializeField] private int maxParticles = 96;

        private readonly List<Particle> _live = new List<Particle>();
        private readonly Stack<Particle> _pool = new Stack<Particle>();
        private int _seed = 1;

        /// <summary>Particles alive now (tests).</summary>
        public int Alive => _live.Count;

        /// <summary>Speed multiplier (combat playback speed).</summary>
        public float Speed { get; set; } = 1f;

        /// <summary>
        /// Sprays <paramref name="count"/> particles from a point (canvas pixels in this layer's space). Speeds are pixels per
        /// second, the spread is an angle range in degrees (90 = straight up), gravity pulls down in pixels per second squared.
        /// </summary>
        public void Burst(Vector2 at, Sprite[] frames, int count, float minSpeed, float maxSpeed, float minAngle = 30f, float maxAngle = 150f,
            float gravity = 320f, float life = 0.55f, float drag = 0f)
        {
            if (frames == null || frames.Length == 0 || count <= 0) return;
            if (UiMotion.Reduced) count = Mathf.Min(count, 2);
            var rng = new System.Random(_seed++);
            for (int i = 0; i < count; i++)
            {
                float a = Mathf.Lerp(minAngle, maxAngle, (float)rng.NextDouble()) * Mathf.Deg2Rad;
                float s = Mathf.Lerp(minSpeed, maxSpeed, (float)rng.NextDouble());
                Particle p = Spawn(frames);
                p.Pos = at;
                p.Vel = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * s;
                p.Gravity = gravity;
                p.Drag = drag;
                p.Life = life * Mathf.Lerp(0.75f, 1.15f, (float)rng.NextDouble());
                if (UiMotion.Reduced) p.Life = Mathf.Min(p.Life, 0.12f);
                Place(p, 0f);
            }
        }

        /// <summary>Plays a flipbook once at a point (puff, spark, burst).</summary>
        public void Play(Vector2 at, Sprite[] frames, float life = 0.3f)
        {
            if (frames == null || frames.Length == 0) return;
            Particle p = Spawn(frames);
            p.Pos = at;
            p.Life = UiMotion.Reduced ? Mathf.Min(life, 0.1f) : life;
            Place(p, 0f);
        }

        /// <summary>Flies one sprite from a point to another along an arc, then calls <paramref name="arrive"/>.</summary>
        public void Fly(Vector2 from, Vector2 to, Sprite[] frames, float seconds, float arcPixels = 24f, Action arrive = null)
        {
            if (frames == null || frames.Length == 0)
            {
                arrive?.Invoke();
                return;
            }

            if (UiMotion.Reduced)
            {
                arrive?.Invoke();
                return;
            }

            Particle p = Spawn(frames);
            p.Flier = true;
            p.From = from;
            p.To = to;
            p.Arc = arcPixels;
            p.Life = Mathf.Max(0.05f, seconds);
            p.Arrive = arrive;
            p.Loop = true;
            Place(p, 0f);
        }

        /// <summary>Removes every particle at once (skip, scene change).</summary>
        public void Clear()
        {
            foreach (Particle p in _live)
            {
                Action a = p.Arrive;
                Recycle(p);
                a?.Invoke();
            }

            _live.Clear();
        }

        private Particle Spawn(Sprite[] frames)
        {
            if (_live.Count >= maxParticles)
            {
                Particle old = _live[0];
                _live.RemoveAt(0);
                Action a = old.Arrive;
                Recycle(old);
                a?.Invoke();
            }

            Particle p = _pool.Count > 0 ? _pool.Pop() : New();
            p.Frames = frames;
            p.Born = Time.unscaledTime;
            p.Flier = false;
            p.Arrive = null;
            p.Loop = false;
            p.Vel = Vector2.zero;
            p.Gravity = 0f;
            p.Drag = 0f;
            p.Image.sprite = frames[0];
            p.Image.SetNativeSize();
            p.Image.gameObject.SetActive(true);
            p.Rect.SetAsLastSibling();
            _live.Add(p);
            return p;
        }

        private Particle New()
        {
            var go = new GameObject("Particle", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            var img = go.GetComponent<Image>();
            img.raycastTarget = false;
            return new Particle { Image = img, Rect = rt };
        }

        private void Recycle(Particle p)
        {
            if (p.Image != null) p.Image.gameObject.SetActive(false);
            p.Arrive = null;
            _pool.Push(p);
        }

        private void Update()
        {
            if (_live.Count == 0) return;
            float now = Time.unscaledTime;
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f) * Mathf.Max(0.01f, Speed);
            for (int i = _live.Count - 1; i >= 0; i--)
            {
                Particle p = _live[i];
                float age = (now - p.Born) * Mathf.Max(0.01f, Speed);
                float u = age / p.Life;
                if (u >= 1f)
                {
                    _live.RemoveAt(i);
                    Action a = p.Arrive;
                    Recycle(p);
                    a?.Invoke();
                    continue;
                }

                if (!p.Flier)
                {
                    p.Vel.y -= p.Gravity * dt;
                    if (p.Drag > 0f) p.Vel *= Mathf.Max(0f, 1f - p.Drag * dt);
                    p.Pos += p.Vel * dt;
                }

                Place(p, u);
            }
        }

        private static void Place(Particle p, float u)
        {
            Vector2 pos = p.Pos;
            if (p.Flier)
            {
                float e = UiMotion.Evaluate(Ease.InOutQuad, u);
                pos = Vector2.Lerp(p.From, p.To, e) + new Vector2(0f, p.Arc * 4f * u * (1f - u));
            }

            p.Rect.anchoredPosition = UiMotion.Snap(pos);
            int n = p.Frames.Length;
            int f = p.Loop ? (int)(u * 8f) % n : Mathf.Min(n - 1, (int)(u * n));
            if (p.Image.sprite != p.Frames[f]) p.Image.sprite = p.Frames[f];
        }
    }
}
