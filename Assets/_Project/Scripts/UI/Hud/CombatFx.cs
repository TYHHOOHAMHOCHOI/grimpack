using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Project05.UI.Hud
{
    /// <summary>
    /// Motion and effects of combat playback, drawn over the HUD: the attacker's wind-up and lunge, the swing smear and
    /// impact spark, the target's knock-back and hit blink, floating numbers, and a small shake on a detonation.
    /// Presentation only: it reads nothing from the simulation and changes no state. Every position is a whole native pixel.
    /// Reduced motion removes travel and shake (effects and numbers still show); reduced flashes removes the blink and uses
    /// the calm detonation frame (settings.md rules 3 and 4).
    /// </summary>
    public sealed class CombatFx : MonoBehaviour
    {
        private const float FrameTime = 0.05f;
        private const float NumberLife = 0.8f;

        private sealed class Motion
        {
            public RectTransform Rect;
            public Vector2 Base;
            public Vector2 LastSet;
            public bool HasBase;

            public float LungeAt = -1f;
            public float Windup, Strike, Hold, Recover;
            public Vector2 LungeDir;
            public float LungeDist;

            public float KnockAt = -1f;
            public Vector2 KnockDir;

            public float BlinkAt = -1f;
            public float BlinkEnded = -10f;

            public Sprite[] AttackPoses;
            public Sprite RestPose;
            public Image Body;
            public Image Extra;
        }

        private sealed class Flipbook
        {
            public Image Image;
            public Sprite[] Frames;
            public float Start;
            public float FrameTime;
        }

        private sealed class Number
        {
            public RectTransform Root;
            public SpriteNumeralText Text;
            public Vector2 Origin;
            public float Start;
        }

        private CombatHudView _hud;
        private RectTransform _layer;
        private RectTransform _shakeRoot;
        private Vector2 _shakeBase;
        private float _shakeAt = -1f;
        private readonly Dictionary<(bool hero, int slot), Motion> _motions = new Dictionary<(bool, int), Motion>();
        private readonly List<Flipbook> _flipbooks = new List<Flipbook>();
        private readonly List<Number> _numbers = new List<Number>();
        private readonly Dictionary<(bool hero, int slot), (float at, int count)> _recentNumbers = new Dictionary<(bool, int), (float, int)>();

        /// <summary>The effects layer (top-left origin, y up), for avatars and rings.</summary>
        public RectTransform Layer => _layer;

        /// <summary>The HUD this layer draws over.</summary>
        public CombatHudView Hud => _hud;

        /// <summary>Speed multiplier of the current playback (1, 2 or 4); every effect runs that much faster.</summary>
        public float Speed { get; set; } = 1f;

        private static bool ReducedMotion => GameSettings.ReducedMotion;

        private static bool ReducedFlashes => GameSettings.ReducedFlashes;

        /// <summary>Attaches to a HUD and creates the effects layer above it.</summary>
        public void Initialize(CombatHudView hud)
        {
            _hud = hud;
            _shakeRoot = (RectTransform)hud.transform;
            _shakeBase = _shakeRoot.anchoredPosition;
            if (_layer == null)
            {
                var go = new GameObject("CombatFxLayer", typeof(RectTransform));
                go.transform.SetParent(hud.transform, false);
                _layer = (RectTransform)go.transform;
                _layer.anchorMin = _layer.anchorMax = _layer.pivot = new Vector2(0f, 1f);
                _layer.anchoredPosition = Vector2.zero;
                _layer.sizeDelta = Vector2.zero;
            }

            _layer.SetAsLastSibling();
        }

        /// <summary>Stops every effect and puts every unit back in place (Skip, or playback ended).</summary>
        public void Clear()
        {
            foreach (Motion m in _motions.Values)
            {
                if (m.HasBase && m.Rect != null) m.Rect.anchoredPosition = m.Base;
                if (m.AttackPoses != null && m.Body != null && m.RestPose != null) m.Body.sprite = m.RestPose;
                if (m.Body != null) m.Body.enabled = true;
                if (m.Extra != null) m.Extra.enabled = true;
            }

            _motions.Clear();
            foreach (Flipbook f in _flipbooks) f.Image.gameObject.SetActive(false);
            foreach (Number n in _numbers) n.Root.gameObject.SetActive(false);
            if (_shakeRoot != null) _shakeRoot.anchoredPosition = _shakeBase;
            _shakeAt = -1f;
        }

        // ------------------------------------------------------------------ requests
        /// <summary>
        /// The actor winds up (pulls back 2 px), lunges toward its target and comes back. Durations are at 1x and are
        /// divided by <see cref="Speed"/>. Without a target it rises in place.
        /// </summary>
        public void Lunge(bool heroActor, int slot, bool hasTarget, float windup, float strike, float recover, float distance = 12f)
        {
            Motion m = MotionOf(heroActor, slot);
            if (m == null || ReducedMotion) return;
            m.LungeAt = Time.unscaledTime;
            m.Windup = windup / Speed;
            m.Strike = strike / Speed;
            m.Hold = 0.04f / Speed;
            m.Recover = recover / Speed;
            m.LungeDir = hasTarget ? new Vector2(heroActor ? 1f : -1f, 0f) : new Vector2(0f, 1f);
            m.LungeDist = hasTarget ? distance : 3f;
            m.AttackPoses = heroActor && hasTarget && _hud.Sprites.Attack != null && _hud.Sprites.Attack.Length >= 2 ? _hud.Sprites.Attack : null;
        }

        /// <summary>A hit lands on a unit: swing smear (on the first hit of a swing), spark, knock-back, blink and a number.</summary>
        /// <param name="targetHero">The target is a hero.</param>
        /// <param name="slot">The target's slot.</param>
        /// <param name="amount">Damage dealt.</param>
        /// <param name="byHero">The attacker is a hero (picks the effect colourway and the knock-back direction).</param>
        /// <param name="swing">Show the weapon smear (the first hit of a melee action).</param>
        /// <param name="detonation">A detonation: bigger number, burst and a small shake.</param>
        public void Hit(bool targetHero, int slot, int amount, bool byHero, bool swing, bool detonation)
        {
            if (_hud == null) return;
            HudSpriteSet s = _hud.Sprites;
            Vector2 centre = BodyCentre(targetHero, slot, 0.5f);
            if (detonation)
            {
                if (ReducedFlashes) Play(new[] { byHero ? s.vfxBurstCalm : s.vfxBurstFoeCalm }, centre, 0.2f);
                else Play(byHero ? s.vfxBurst : s.vfxBurstFoe, centre, FrameTime);
                Shake();
            }
            else
            {
                if (swing) Play(byHero ? s.vfxSmear : s.vfxSmearFoe, centre + new Vector2(byHero ? -4f : 4f, 0f), FrameTime * 0.8f);
                Play(byHero ? s.vfxSpark : s.vfxSparkFoe, centre + new Vector2(byHero ? -6f : 6f, -2f), FrameTime);
            }

            Motion m = MotionOf(targetHero, slot);
            if (m != null)
            {
                float now = Time.unscaledTime;
                if (!ReducedMotion)
                {
                    m.KnockAt = now;
                    m.KnockDir = new Vector2(byHero ? 1f : -1f, 0f);
                }

                // One blink per burst of hits: a multi-hit flickering the body on every hit reads as noise.
                if (!ReducedFlashes && m.BlinkAt < 0f && now - m.BlinkEnded > 0.25f / Speed) m.BlinkAt = now;
            }

            if (amount > 0) ShowNumber(targetHero, slot, (targetHero ? "-" : string.Empty) + amount, detonation ? 24 : 16, targetHero);
        }

        /// <summary>Shield or heal on a unit: a rising "+N" and a one-pixel hop.</summary>
        public void Gain(bool hero, int slot, int amount)
        {
            if (amount <= 0) return;
            ShowNumber(hero, slot, "+" + amount, 12, false, true);
            Motion m = MotionOf(hero, slot);
            if (m == null || ReducedMotion || m.LungeAt >= 0f) return;
            m.LungeAt = Time.unscaledTime;
            m.Windup = 0f;
            m.Strike = 0.06f / Speed;
            m.Hold = 0.02f / Speed;
            m.Recover = 0.1f / Speed;
            m.LungeDir = new Vector2(0f, 1f);
            m.LungeDist = 2f;
        }

        // ------------------------------------------------------------------ per frame
        private void LateUpdate()
        {
            float now = Time.unscaledTime;
            List<(bool, int)> done = null;
            foreach (KeyValuePair<(bool hero, int slot), Motion> pair in _motions)
            {
                Motion m = pair.Value;
                if (m.Rect == null) continue;
                Vector2 current = m.Rect.anchoredPosition;
                if (!m.HasBase || current != m.LastSet)
                {
                    m.Base = current;            // the HUD laid the slot out again: that is the new rest position
                    m.HasBase = true;
                }

                Vector2 offset = LungeOffset(m, now) + KnockOffset(m, now);
                AttackPose(m, now);
                bool blinking = Blink(m, now);
                Vector2 set = m.Base + new Vector2(Mathf.Round(offset.x), Mathf.Round(offset.y));
                m.Rect.anchoredPosition = set;
                m.LastSet = set;

                if (m.LungeAt < 0f && m.KnockAt < 0f && !blinking && m.AttackPoses == null)
                {
                    (done ??= new List<(bool, int)>()).Add(pair.Key);
                }
            }

            if (done != null)
            {
                foreach ((bool, int) key in done) _motions.Remove(key);
            }

            UpdateFlipbooks(now);
            UpdateNumbers(now);
            UpdateShake(now);
        }

        private static Vector2 LungeOffset(Motion m, float now)
        {
            if (m.LungeAt < 0f) return Vector2.zero;
            float t = now - m.LungeAt;
            float d;
            if (t < m.Windup) d = -2f * EaseOut(t / m.Windup);                                    // pull back
            else if (t < m.Windup + m.Strike) d = Mathf.Lerp(-2f, m.LungeDist, EaseIn((t - m.Windup) / m.Strike));
            else if (t < m.Windup + m.Strike + m.Hold) d = m.LungeDist;                           // contact
            else
            {
                float u = (t - m.Windup - m.Strike - m.Hold) / m.Recover;
                if (u >= 1f)
                {
                    m.LungeAt = -1f;
                    return Vector2.zero;
                }

                d = Mathf.Lerp(m.LungeDist, 0f, EaseOut(u));
            }

            return m.LungeDir * d;
        }

        /// <summary>
        /// Swaps the body to the wind-up frame, then the strike frame, for the lunge of a hero, and back to the pose the HUD
        /// last gave it. The HUD may set a new pose meanwhile; that pose is the one restored.
        /// </summary>
        private static void AttackPose(Motion m, float now)
        {
            if (m.Body == null) return;
            Sprite current = m.Body.sprite;
            bool ours = m.AttackPoses != null && (current == m.AttackPoses[0] || current == m.AttackPoses[1]);
            if (!ours) m.RestPose = current;
            if (m.AttackPoses == null) return;

            float t = m.LungeAt < 0f ? float.MaxValue : now - m.LungeAt;
            Sprite want = t < m.Windup ? m.AttackPoses[0]
                : t < m.Windup + m.Strike + m.Hold + m.Recover * 0.5f ? m.AttackPoses[1]
                : m.RestPose;
            if (want == m.RestPose && m.LungeAt < 0f) m.AttackPoses = null;
            if (want != null && m.Body.sprite != want) m.Body.sprite = want;
        }

        private float KnockDuration => 0.22f / Speed;

        private Vector2 KnockOffset(Motion m, float now)
        {
            if (m.KnockAt < 0f) return Vector2.zero;
            float u = (now - m.KnockAt) / KnockDuration;
            if (u >= 1f)
            {
                m.KnockAt = -1f;
                return Vector2.zero;
            }

            // 3 px back at once, then a damped wobble that settles on the rest position.
            float amp = 3f * (1f - u) * (1f - u);
            float wobble = Mathf.Cos(u * Mathf.PI * 3f);
            return m.KnockDir * (amp * wobble);
        }

        private bool Blink(Motion m, float now)
        {
            if (m.BlinkAt < 0f) return false;
            float t = (now - m.BlinkAt) * Speed;
            bool visible = t >= 0.15f || (t >= 0.05f && t < 0.1f);   // off, on, off, on: two quick blinks
            if (m.Body != null) m.Body.enabled = visible;
            if (m.Extra != null && !visible) m.Extra.enabled = false;
            if (t >= 0.15f)
            {
                m.BlinkAt = -1f;
                m.BlinkEnded = now;
                return false;
            }

            return true;
        }

        private void UpdateFlipbooks(float now)
        {
            foreach (Flipbook f in _flipbooks)
            {
                if (!f.Image.gameObject.activeSelf) continue;
                int frame = (int)((now - f.Start) / f.FrameTime);
                if (frame >= f.Frames.Length)
                {
                    f.Image.gameObject.SetActive(false);
                    continue;
                }

                if (f.Image.sprite != f.Frames[frame])
                {
                    f.Image.sprite = f.Frames[frame];
                    f.Image.rectTransform.sizeDelta = f.Frames[frame].rect.size;
                }
            }
        }

        private void UpdateNumbers(float now)
        {
            foreach (Number n in _numbers)
            {
                if (!n.Root.gameObject.activeSelf) continue;
                float t = (now - n.Start) * Mathf.Min(Speed, 2f);   // numbers stay readable at 4x
                if (t >= NumberLife)
                {
                    n.Root.gameObject.SetActive(false);
                    continue;
                }

                float rise = ReducedMotion ? 0f : t < 0.1f ? 8f * EaseOut(t / 0.1f) : 8f + 6f * Mathf.Min(1f, (t - 0.1f) / 0.5f);
                n.Root.anchoredPosition = n.Origin + new Vector2(0f, Mathf.Round(rise));
                bool visible = t < 0.6f || (t >= 0.65f && t < 0.72f);   // blink out, no partial alpha
                n.Text.gameObject.SetActive(visible);
            }
        }

        private void Shake()
        {
            if (ReducedMotion || _shakeRoot == null) return;
            if (_shakeAt < 0f) _shakeBase = _shakeRoot.anchoredPosition;
            _shakeAt = Time.unscaledTime;
        }

        private void UpdateShake(float now)
        {
            if (_shakeAt < 0f) return;
            float t = (now - _shakeAt) * Speed;
            if (t >= 0.18f)
            {
                _shakeRoot.anchoredPosition = _shakeBase;
                _shakeAt = -1f;
                return;
            }

            int step = (int)(t / 0.03f);
            float x = (step % 2 == 0 ? 1f : -1f) * (t < 0.09f ? 2f : 1f);
            _shakeRoot.anchoredPosition = _shakeBase + new Vector2(x, 0f);
        }

        // ------------------------------------------------------------------ helpers
        private Motion MotionOf(bool hero, int slot)
        {
            if (_hud == null) return null;
            if (_motions.TryGetValue((hero, slot), out Motion m)) return m;
            MonoBehaviour view = _hud.Slot(hero, slot);
            if (view == null || !view.gameObject.activeInHierarchy) return null;
            m = new Motion { Rect = (RectTransform)view.transform, Body = _hud.Body(hero, slot), Extra = view is EnemySlotView e ? e.Eyes : null };
            _motions[(hero, slot)] = m;
            return m;
        }

        /// <summary>A point on a unit's body in effects-layer coordinates; v is 0 at the top of the body and 1 at the bottom.</summary>
        public Vector2 BodyCentre(bool hero, int slot, float v)
        {
            Image body = _hud.Body(hero, slot);
            if (body == null || _layer == null) return Vector2.zero;
            var corners = new Vector3[4];
            body.rectTransform.GetWorldCorners(corners);   // bottom-left, top-left, top-right, bottom-right
            Vector3 top = (corners[1] + corners[2]) / 2f;
            Vector3 bottom = (corners[0] + corners[3]) / 2f;
            Vector2 local = _layer.InverseTransformPoint(Vector3.Lerp(top, bottom, v));
            return new Vector2(Mathf.Round(local.x), Mathf.Round(local.y));
        }

        /// <summary>Plays a flipbook centred on a point of the effects layer; frame time is at 1x.</summary>
        public void Play(Sprite[] frames, Vector2 centre, float frameTime)
        {
            if (frames == null || frames.Length == 0 || frames[0] == null || _layer == null) return;
            Flipbook f = _flipbooks.Find(x => !x.Image.gameObject.activeSelf);
            if (f == null)
            {
                var go = new GameObject("Fx", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(_layer, false);
                var image = go.GetComponent<Image>();
                image.raycastTarget = false;
                var rt = image.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                f = new Flipbook { Image = image };
                _flipbooks.Add(f);
            }

            f.Frames = frames;
            f.Start = Time.unscaledTime;
            f.FrameTime = frameTime / Speed;
            f.Image.sprite = frames[0];
            f.Image.rectTransform.sizeDelta = frames[0].rect.size;
            f.Image.rectTransform.anchoredPosition = centre;
            f.Image.gameObject.SetActive(true);
            f.Image.transform.SetAsLastSibling();
        }

        private void ShowNumber(bool hero, int slot, string text, int cut, bool blood, bool gain = false)
        {
            if (_hud == null || !_hud.TryGetReadoutFont(cut, out Sprite bone, out Sprite bloodAtlas, out TextAsset metrics)) return;
            Number n = _numbers.Find(x => !x.Root.gameObject.activeSelf);
            if (n == null)
            {
                var go = new GameObject("Number", typeof(RectTransform));
                go.transform.SetParent(_layer, false);
                var root = (RectTransform)go.transform;
                root.anchorMin = root.anchorMax = root.pivot = new Vector2(0f, 1f);
                var textGo = new GameObject("Text", typeof(RectTransform));
                textGo.transform.SetParent(root, false);
                var trt = (RectTransform)textGo.transform;
                trt.anchorMin = trt.anchorMax = trt.pivot = new Vector2(0f, 1f);
                n = new Number { Root = root, Text = textGo.AddComponent<SpriteNumeralText>() };
                _numbers.Add(n);
            }

            n.Text.Configure(blood ? bloodAtlas : bone, metrics, SpriteNumeralText.Align.Center);
            n.Text.Text = text;
            n.Text.gameObject.SetActive(true);

            // Numbers on the same unit in quick succession fan out (left, right, lower), so a multi-hit reads as several
            // hits and no two numbers sit on each other.
            float now = Time.unscaledTime;
            int count = 0;
            int key = gain ? slot + 100 : slot;   // gains fan out on their own, away from damage numbers
            if (_recentNumbers.TryGetValue((hero, key), out (float at, int count) recent) && now - recent.at < 0.45f) count = recent.count + 1;
            _recentNumbers[(hero, key)] = (now, count);
            Vector2 fan = FanOffsets[count % FanOffsets.Length];

            // Heroes take numbers over the head; an enemy has its intent tab there, so its numbers stand on the side the
            // blow came from, at chest height.
            // Gains (shield, heal) stand on the far side of the body from the enemy, so they never cover a damage number.
            Vector2 anchor = gain
                ? BodyCentre(hero, slot, 0.45f) + new Vector2(hero ? -30f : 30f, cut / 2f)
                : hero ? BodyCentre(true, slot, 0.35f) + new Vector2(30f, cut / 2f) : BodyCentre(false, slot, 0.5f) + new Vector2(-30f, cut / 2f);
            n.Origin = anchor + fan;
            n.Start = now;
            n.Root.anchoredPosition = n.Origin;
            n.Root.gameObject.SetActive(true);
            n.Root.SetAsLastSibling();
        }

        // Wide enough that two 16 px numbers of two digits never touch.
        private static readonly Vector2[] FanOffsets = { Vector2.zero, new Vector2(-16f, -10f), new Vector2(16f, -10f), new Vector2(0f, -20f) };

        private static float EaseOut(float u) => 1f - (1f - Mathf.Clamp01(u)) * (1f - Mathf.Clamp01(u));

        private static float EaseIn(float u) => Mathf.Clamp01(u) * Mathf.Clamp01(u);
    }
}
