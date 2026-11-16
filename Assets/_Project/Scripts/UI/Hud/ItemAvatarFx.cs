using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Project05.UI.Hud
{
    /// <summary>
    /// Item avatars (art bible 5.10, 5.11, 5.12): when a hero item fires, its 1x1 tier plate with its glyph pops in over the
    /// hero's head, holds so the player can read which item it is, then performs its archetype: Swing (stepped arc toward the
    /// target), Throw (hops to the target with a trail), Status (a mote hops to the target), Ring (a code-drawn ring grows on
    /// the target), Detonate and Pulse (the avatar pulses in place). An enemy's intent glyph does the same from its tab. Every
    /// position is a whole pixel and nothing rotates. Impacts, numbers and knock-back are played by <see cref="CombatFx"/> at
    /// contact; this class only moves avatars, motes and rings.
    /// </summary>
    public sealed class ItemAvatarFx : MonoBehaviour
    {
        private sealed class Avatar
        {
            public RectTransform Root;
            public Image Plate;
            public Image Glyph;
            public Archetype Kind;
            public Vector2 From;
            public Vector2 To;
            public float Start;
            public float PopIn, Hold, Travel, Exit;
            public bool Enemy;
            public Color32 RingColour;
            public bool RingDone;
            public float TrailNext;
        }

        private sealed class Ring
        {
            public Image Image;
            public Vector2 Centre;
            public float Start;
            public float Duration;
            public Color32 Colour;
        }

        private const int RingSteps = 6;
        private const int RingMax = 16;

        private CombatFx _fx;
        private RectTransform _layer;
        private readonly List<Avatar> _avatars = new List<Avatar>();
        private readonly List<Ring> _rings = new List<Ring>();
        private readonly Dictionary<(Color32 colour, int radius), Sprite> _ringSprites = new Dictionary<(Color32, int), Sprite>();
        private Sprite[] _trail;
        private Sprite[] _trailFoe;
        private Sprite[] _mote;
        private Sprite[] _moteFoe;

        /// <summary>Speed multiplier of the current playback.</summary>
        public float Speed { get; set; } = 1f;

        /// <summary>Attaches to the effects layer of a <see cref="CombatFx"/>.</summary>
        public void Initialize(CombatFx fx, Sprite[] trail, Sprite[] trailFoe, Sprite[] mote, Sprite[] moteFoe)
        {
            _fx = fx;
            _layer = fx.Layer;
            _trail = trail;
            _trailFoe = trailFoe;
            _mote = mote;
            _moteFoe = moteFoe;
        }

        /// <summary>Removes every avatar and ring at once (Skip, or the end of playback).</summary>
        public void Clear()
        {
            foreach (Avatar a in _avatars) a.Root.gameObject.SetActive(false);
            foreach (Ring r in _rings) r.Image.gameObject.SetActive(false);
        }

        /// <summary>
        /// A hero item fires. All durations are at 1x and divided by <see cref="Speed"/>; contact is
        /// popIn + hold + travel after this call, which is when the timeline lands the item's effects.
        /// </summary>
        public void HeroItem(int heroSlot, Archetype kind, Sprite plate, Sprite glyph, bool hasTarget, bool targetHero, int targetSlot,
            Color32 ringColour, PlaybackPacing pacing)
        {
            if (_fx == null || _layer == null) return;
            Vector2 head = _fx.BodyCentre(true, heroSlot, 0f) + new Vector2(0f, 4f + 10f);   // the avatar zone: C + 4 over the head
            Vector2 to = hasTarget ? _fx.BodyCentre(targetHero, targetSlot, 0.5f) : _fx.BodyCentre(true, heroSlot, 0.5f);
            Spawn(kind, plate, glyph, head, to, false, ringColour, pacing);
        }

        /// <summary>An enemy acts: its intent glyph lifts off the tab and flies to the target hero (5.10.8).</summary>
        public void EnemyGlyph(int enemySlot, Sprite glyph, Archetype kind, bool hasTarget, int heroSlot, PlaybackPacing pacing)
        {
            if (_fx == null || _layer == null || glyph == null) return;
            Vector2 from = _fx.BodyCentre(false, enemySlot, 0f) + new Vector2(0f, 14f);
            Vector2 to = hasTarget ? _fx.BodyCentre(true, heroSlot, 0.5f) : from;
            Spawn(kind == Archetype.Swing ? Archetype.Throw : kind, null, glyph, from, to, true, new Color32(0xAA, 0xB8, 0xD4, 0xFF), pacing);
        }

        private void Spawn(Archetype kind, Sprite plate, Sprite glyph, Vector2 from, Vector2 to, bool enemy, Color32 ring, PlaybackPacing p)
        {
            Avatar a = _avatars.Find(x => !x.Root.gameObject.activeSelf) ?? NewAvatar();
            a.Kind = kind;
            a.From = from;
            a.To = to;
            a.Start = Time.unscaledTime;
            a.PopIn = p.popIn / Speed;
            a.Hold = p.readHold / Speed;
            a.Travel = p.Travel(kind) / Speed;
            a.Exit = p.exit / Speed;
            a.Enemy = enemy;
            a.RingColour = ring;
            a.RingDone = false;
            a.TrailNext = 0f;

            a.Plate.enabled = plate != null;
            if (plate != null) a.Plate.sprite = plate;
            a.Glyph.enabled = glyph != null;
            if (glyph != null)
            {
                a.Glyph.sprite = glyph;
                a.Glyph.rectTransform.sizeDelta = glyph.rect.size;
                a.Glyph.rectTransform.anchoredPosition = plate != null ? new Vector2(2f, -2f) : new Vector2(-glyph.rect.width / 2f + 10f, -10f + glyph.rect.height / 2f);
            }

            a.Root.anchoredPosition = Snap(from);
            a.Root.gameObject.SetActive(true);
            a.Root.SetAsLastSibling();
        }

        private Avatar NewAvatar()
        {
            var go = new GameObject("Avatar", typeof(RectTransform));
            go.transform.SetParent(_layer, false);
            var root = (RectTransform)go.transform;
            root.anchorMin = root.anchorMax = new Vector2(0f, 1f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.sizeDelta = new Vector2(20f, 20f);

            Image plate = NewImage("Plate", root, new Vector2(0f, 0f), new Vector2(20f, 20f));
            Image glyph = NewImage("Glyph", root, new Vector2(2f, -2f), new Vector2(16f, 16f));
            var a = new Avatar { Root = root, Plate = plate, Glyph = glyph };
            _avatars.Add(a);
            return a;
        }

        private static Image NewImage(string name, RectTransform parent, Vector2 topLeft, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = topLeft;
            rt.sizeDelta = size;
            var image = go.GetComponent<Image>();
            image.raycastTarget = false;
            return image;
        }

        // ------------------------------------------------------------------ per frame
        private void LateUpdate()
        {
            float now = Time.unscaledTime;
            foreach (Avatar a in _avatars)
            {
                if (!a.Root.gameObject.activeSelf) continue;
                float t = now - a.Start;
                float contact = a.PopIn + a.Hold + a.Travel;
                if (t >= contact + a.Exit)
                {
                    a.Root.gameObject.SetActive(false);
                    continue;
                }

                a.Root.anchoredPosition = Snap(Position(a, t));
                // pop-in: one step up from below; exit: blink out (no partial alpha)
                bool visible = t >= contact ? ((int)((t - contact) / 0.04f) % 2 == 0) : true;
                a.Plate.gameObject.SetActive(visible && a.Plate.enabled);
                a.Glyph.gameObject.SetActive(visible && a.Glyph.enabled);

                float travelT = t - a.PopIn - a.Hold;
                if (travelT >= 0f && t < contact) Trails(a, travelT, now);
                if (a.Kind == Archetype.Ring && !a.RingDone && travelT >= 0f)
                {
                    a.RingDone = true;
                    StartRing(a.To, a.Travel, a.RingColour);
                }
            }

            UpdateRings(now);
        }

        /// <summary>Where an avatar is at time t after it spawned.</summary>
        private Vector2 Position(Avatar a, float t)
        {
            if (t < a.PopIn) return a.From + new Vector2(0f, t < a.PopIn / 2f ? -2f : -1f);   // rises into place
            float u = (t - a.PopIn - a.Hold) / Mathf.Max(0.0001f, a.Travel);
            if (u < 0f) return a.From;
            u = Mathf.Clamp01(u);
            Vector2 dir = a.To - a.From;
            switch (a.Kind)
            {
                case Archetype.Swing:
                {
                    // stepped arc of 5 positions, about 20 px toward the target and up then down
                    float step = Mathf.Floor(u * 5f) / 5f;
                    Vector2 toward = dir.sqrMagnitude > 0.01f ? dir.normalized : Vector2.right;
                    return a.From + toward * (20f * step) + new Vector2(0f, 6f * Mathf.Sin(step * Mathf.PI));
                }

                case Archetype.Throw:
                {
                    // hops along the path: one hop per 48 px, between 4 and 8, each with a 3 px bump
                    int hops = Mathf.Clamp(Mathf.RoundToInt(dir.magnitude / 48f), 4, 8);
                    float h = u * hops;
                    int hop = Mathf.Min(hops, Mathf.FloorToInt(h));
                    float within = h - hop;
                    Vector2 along = a.From + dir * (Mathf.Min(hops, hop + (within >= 0.5f ? 1 : 0)) / (float)hops);
                    return along + new Vector2(0f, within > 0.15f && within < 0.65f && hop < hops ? 3f : 0f);
                }

                case Archetype.Status:
                    return a.From + new Vector2(Mathf.Sign(dir.x) * (u < 0.3f ? 2f : 1f), 0f);   // tips toward the target
                case Archetype.Pulse:
                    return a.From + new Vector2(0f, ((int)(u * 4f) % 2 == 0) ? 2f : 0f);           // bobs 2 px
                default:
                    return a.From + new Vector2(0f, ((int)(u * 3f) % 2 == 0) ? 1f : 0f);           // Ring and Detonate pulse
            }
        }

        private void Trails(Avatar a, float travelT, float now)
        {
            if (now < a.TrailNext) return;
            a.TrailNext = now + 0.06f / Speed;
            if (a.Kind == Archetype.Throw)
            {
                Sprite[] trail = a.Enemy ? _trailFoe : _trail;
                if (trail != null && trail.Length > 0) _fx.Play(trail, a.Root.anchoredPosition, 0.05f);
            }
            else if (a.Kind == Archetype.Status)
            {
                // the mote hops from the avatar to the target, 3 to 6 hops
                Vector2 dir = a.To - a.From;
                int hops = Mathf.Clamp(Mathf.RoundToInt(dir.magnitude / 40f), 3, 6);
                float u = Mathf.Clamp01(travelT / a.Travel);
                Vector2 at = a.From + dir * (Mathf.Floor(u * hops) / hops);
                Sprite[] mote = a.Enemy ? _moteFoe : _mote;
                if (mote != null && mote.Length > 0) _fx.Play(mote, Snap(at), 0.05f);
            }
        }

        // ------------------------------------------------------------------ rings (code-drawn, 5.11)
        private void StartRing(Vector2 centre, float duration, Color32 colour)
        {
            Ring r = _rings.Find(x => !x.Image.gameObject.activeSelf);
            if (r == null)
            {
                Image image = NewImage("Ring", _layer, Vector2.zero, Vector2.one);
                image.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                r = new Ring { Image = image };
                _rings.Add(r);
            }

            r.Centre = centre;
            r.Start = Time.unscaledTime;
            r.Duration = Mathf.Max(0.1f, duration);
            r.Colour = colour;
            r.Image.gameObject.SetActive(true);
            r.Image.transform.SetAsLastSibling();
        }

        private void UpdateRings(float now)
        {
            foreach (Ring r in _rings)
            {
                if (!r.Image.gameObject.activeSelf) continue;
                float u = (now - r.Start) / r.Duration;
                if (u >= 1f)
                {
                    r.Image.gameObject.SetActive(false);
                    continue;
                }

                // stepped growth in 6 steps; the outline steps down from the colour to INK-4 as it grows
                int step = Mathf.Min(RingSteps - 1, (int)(u * RingSteps));
                int radius = 4 + step * (RingMax - 4) / (RingSteps - 1);
                Color32 colour = step < 3 ? r.Colour : step < 5 ? Dim(r.Colour) : new Color32(0x5A, 0x67, 0x85, 0xFF);
                Sprite sprite = RingSprite(colour, radius);
                r.Image.sprite = sprite;
                r.Image.rectTransform.sizeDelta = sprite.rect.size;
                r.Image.rectTransform.anchoredPosition = Snap(r.Centre);
            }
        }

        private static Color32 Dim(Color32 c) => new Color32((byte)(c.r * 3 / 4), (byte)(c.g * 3 / 4), (byte)(c.b * 3 / 4), 255);

        /// <summary>A 1 px ring of integer radius, drawn once per colour and radius.</summary>
        private Sprite RingSprite(Color32 colour, int radius)
        {
            if (_ringSprites.TryGetValue((colour, radius), out Sprite s) && s != null) return s;
            int size = radius * 2 + 1;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Sqrt((x - radius) * (x - radius) + (y - radius) * (y - radius));
                    px[y * size + x] = Mathf.Abs(d - radius) < 0.5f ? colour : new Color32(0, 0, 0, 0);
                }
            }

            tex.SetPixels32(px);
            tex.Apply(false);
            tex.hideFlags = HideFlags.HideAndDontSave;
            s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f, 0, SpriteMeshType.FullRect);
            s.hideFlags = HideFlags.HideAndDontSave;
            _ringSprites[(colour, radius)] = s;
            return s;
        }

        private static Vector2 Snap(Vector2 v) => new Vector2(Mathf.Round(v.x), Mathf.Round(v.y));
    }
}
