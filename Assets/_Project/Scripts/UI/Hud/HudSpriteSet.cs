using System;
using UnityEngine;

namespace Project05.UI.Hud
{
    /// <summary>
    /// The sprites the combat HUD draws from, gathered in one asset so views hold one reference and the
    /// art stays swappable. Filled by the Combat scene builder from Assets/_Project/Art.
    /// </summary>
    [CreateAssetMenu(menuName = "Project05/HUD Sprite Set", fileName = "HudSpriteSet")]
    public sealed class HudSpriteSet : ScriptableObject
    {
        /// <summary>Frames and metrics of one monster type.</summary>
        [Serializable]
        public sealed class MonsterVisual
        {
            public string name = string.Empty;
            [Tooltip("Body height in native px: sets the slot pitch (body + 56, art bible 7.4a).")]
            public int bodyHeight = 26;
            [Tooltip("Idle1, Idle2, Act, Hurt, Die1, Die2.")]
            public Sprite[] poses = new Sprite[6];
            [Tooltip("Lit-socket overlays for Idle1, Idle2 and Act.")]
            public Sprite[] eyes = new Sprite[3];
        }

        [Header("Heroes")]
        [Tooltip("Idle1, Idle2, Hurt1, Hurt2, Down1, Down2.")]
        public Sprite[] heroPoses = new Sprite[6];
        public Sprite heroNarrow;
        [Tooltip("Attack wind-up and strike, shown by combat playback during the lunge.")]
        public Sprite[] heroAttack = Array.Empty<Sprite>();
        public Sprite pedestalDefault;
        public Sprite pedestalDim;

        [Header("Monsters")]
        public MonsterVisual[] monsters = Array.Empty<MonsterVisual>();
        public Sprite shadowContact;

        [Header("Glyphs, indexed by GlyphKind")]
        public Sprite[] glyphBone12 = new Sprite[11];
        public Sprite[] glyphBone8 = new Sprite[11];
        public Sprite[] glyphBlood8 = new Sprite[11];
        public Sprite[] glyphFog8 = new Sprite[11];

        [Header("Intent glyphs, indexed by IntentKind (None unused)")]
        public Sprite[] intentFogHi12 = new Sprite[5];
        public Sprite[] intentFogHi8 = new Sprite[5];

        [Header("Plates, indexed by Tier")]
        public Sprite[] plateLift = new Sprite[6];
        public Sprite[] plateLiftHi = new Sprite[6];

        [Header("Combat effects (art bible 5.10), plain = hero side, foe = enemy side")]
        public Sprite[] vfxSmear = Array.Empty<Sprite>();
        public Sprite[] vfxSmearFoe = Array.Empty<Sprite>();
        public Sprite[] vfxSpark = Array.Empty<Sprite>();
        public Sprite[] vfxSparkFoe = Array.Empty<Sprite>();
        public Sprite[] vfxBurst = Array.Empty<Sprite>();
        public Sprite[] vfxBurstFoe = Array.Empty<Sprite>();
        public Sprite vfxBurstCalm;
        public Sprite vfxBurstFoeCalm;
        public Sprite[] vfxTrail = Array.Empty<Sprite>();
        public Sprite[] vfxTrailFoe = Array.Empty<Sprite>();
        public Sprite[] vfxMote = Array.Empty<Sprite>();
        public Sprite[] vfxMoteFoe = Array.Empty<Sprite>();

        [Tooltip("1 px INK-0 checker: dims a charging item plate without alpha.")]
        public Sprite dimMask;

        [Header("Markers")]
        public Sprite chevronTurnHero;
        public Sprite chevronTurnFoe;
        public Sprite segmentDefault;
        public Sprite segmentSelected;

        /// <summary>The 12 px Bone glyph for a kind.</summary>
        public Sprite Glyph12(GlyphKind kind) => glyphBone12[(int)kind];

        /// <summary>The 8 px Bone glyph for a kind.</summary>
        public Sprite Glyph8(GlyphKind kind) => glyphBone8[(int)kind];

        /// <summary>The flat Fog-hi intent glyph, or null for <see cref="IntentKind.None"/>.</summary>
        public Sprite Intent(IntentKind kind, int size)
        {
            if (kind == IntentKind.None) return null;
            return size >= 12 ? intentFogHi12[(int)kind] : intentFogHi8[(int)kind];
        }

        /// <summary>The status glyph (8 px Bone).</summary>
        public Sprite Status(StatusKind kind) => Glyph8(kind == StatusKind.Seal ? GlyphKind.Seal : GlyphKind.Droplet);
    }
}
