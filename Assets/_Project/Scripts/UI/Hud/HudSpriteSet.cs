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

        [Header("Plague Alchemist (hero art 'alchemist')")]
        public Sprite[] alchemistPoses = new Sprite[6];
        public Sprite alchemistNarrow;
        public Sprite[] alchemistAttack = Array.Empty<Sprite>();

        /// <summary>The art id of the hero on the pedestal (set at run start, never saved).</summary>
        [NonSerialized] public string ActiveHeroArt = "tank";

        private bool UseAlchemist => ActiveHeroArt == "alchemist" && alchemistPoses != null && alchemistPoses.Length >= 6 && alchemistPoses[0] != null;

        /// <summary>The poses of the hero on the pedestal (idle 1-2, hurt 1-2, down 1-2).</summary>
        public Sprite[] Poses => UseAlchemist ? alchemistPoses : heroPoses;

        /// <summary>The eyes-narrow overlay of the hero on the pedestal.</summary>
        public Sprite Narrow => UseAlchemist ? alchemistNarrow : heroNarrow;

        /// <summary>The attack frames of the hero on the pedestal.</summary>
        public Sprite[] Attack => UseAlchemist ? alchemistAttack : heroAttack;
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

        [Header("Debuffs (debuffs-and-detonation.md)")]
        [Tooltip("Status chip frames tinted per debuff, indexed by StatusKind (Droplet, Seal, Poison, Burn, Frost, Slow, Weaken, Stun).")]
        public Sprite[] statusChips = Array.Empty<Sprite>();

        [Tooltip("Status motes recoloured per debuff: poison, burn, frost, slow, weaken (2 frames each).")]
        public Sprite[] vfxMotePoison = Array.Empty<Sprite>();
        public Sprite[] vfxMoteBurn = Array.Empty<Sprite>();
        public Sprite[] vfxMoteFrost = Array.Empty<Sprite>();
        public Sprite[] vfxMoteSlow = Array.Empty<Sprite>();
        public Sprite[] vfxMoteWeaken = Array.Empty<Sprite>();

        /// <summary>The 12 px Bone glyph for a kind (null when not drawn).</summary>
        public Sprite Glyph12(GlyphKind kind) => (int)kind < glyphBone12.Length ? glyphBone12[(int)kind] : null;

        /// <summary>The 8 px Bone glyph for a kind (null when not drawn).</summary>
        public Sprite Glyph8(GlyphKind kind) => (int)kind < glyphBone8.Length ? glyphBone8[(int)kind] : null;

        /// <summary>The tinted chip frame of a status, or null for the plain chip.</summary>
        public Sprite StatusChip(StatusKind kind) => (int)kind < statusChips.Length ? statusChips[(int)kind] : null;

        /// <summary>The mote frames tinted for a status id, or the shared ones.</summary>
        public Sprite[] MoteFor(string statusId)
        {
            Sprite[] m = null;
            switch (statusId)
            {
                case "poison": m = vfxMotePoison; break;
                case "burn": m = vfxMoteBurn; break;
                case "frost": m = vfxMoteFrost; break;
                case "slow": m = vfxMoteSlow; break;
                case "weaken": m = vfxMoteWeaken; break;
            }

            return m != null && m.Length > 0 ? m : vfxMote;
        }

        /// <summary>The flat Fog-hi intent glyph, or null for <see cref="IntentKind.None"/>.</summary>
        public Sprite Intent(IntentKind kind, int size)
        {
            if (kind == IntentKind.None) return null;
            return size >= 12 ? intentFogHi12[(int)kind] : intentFogHi8[(int)kind];
        }

        /// <summary>The status glyph (8 px Bone).</summary>
        public Sprite Status(StatusKind kind)
        {
            switch (kind)
            {
                case StatusKind.Seal: return Glyph8(GlyphKind.Seal);
                case StatusKind.Burn: return Glyph8(GlyphKind.Burn) ?? Glyph8(GlyphKind.Droplet);
                case StatusKind.Frost:
                case StatusKind.Stun: return Glyph8(GlyphKind.Frost) ?? Glyph8(GlyphKind.Droplet);
                case StatusKind.Slow: return Glyph8(GlyphKind.Slow) ?? Glyph8(GlyphKind.Droplet);
                case StatusKind.Weaken: return Glyph8(GlyphKind.Weaken) ?? Glyph8(GlyphKind.Droplet);
                default: return Glyph8(GlyphKind.Droplet);
            }
        }
    }
}
