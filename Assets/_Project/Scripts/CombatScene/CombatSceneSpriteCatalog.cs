using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Project05.CombatScene
{
    /// <summary>
    /// Runtime-accessible sprite and font catalog populated by <c>CombatSceneBuilder</c> so the
    /// Combat scene never depends on <c>UnityEditor.AssetDatabase</c> in Play Mode or builds.
    /// </summary>
    [CreateAssetMenu(menuName = "Project05/Combat Scene Sprite Catalog", fileName = "CombatSceneSpriteCatalog")]
    public sealed class CombatSceneSpriteCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class SpriteEntry
        {
            public string key = string.Empty;
            public Sprite sprite;
        }

        [Header("Typography")]
        public TMP_FontAsset font;
        public Sprite num8BoneAtlas;
        public Sprite num8FogHiAtlas;
        public TextAsset num8Metrics;

        [Header("UI Chrome & Fills")]
        public Sprite panelDefault;
        public Sprite panelVoid;
        public Sprite chipBase;
        public Sprite segmentDefault;
        public Sprite segmentSelected;
        public Sprite btnDefault;
        public Sprite btnHover;
        public Sprite btnPressed;
        public Sprite btnFocus;
        public Sprite btnDisabled;

        public Sprite fillInk0;
        public Sprite fillInk1;
        public Sprite fillInk2;
        public Sprite fillInk3;
        public Sprite fillInk4;
        public Sprite fillCandle;
        public Sprite fillCandleD;
        public Sprite fillFog;
        public Sprite fillBone;
        public Sprite fillBlood;

        [Header("Glyphs")]
        public Sprite glyphHeartBlood8;
        public Sprite glyphHeartBone8;
        public Sprite glyphShieldFog8;
        public Sprite glyphSpeedBone8;
        public Sprite glyphAttackBone8;
        public Sprite glyphSealBone8;
        public Sprite glyphDamageBone8;
        public Sprite glyphDropletBone8;

        [Header("Item Sprites")]
        public List<SpriteEntry> itemIcons = new List<SpriteEntry>();
        public List<SpriteEntry> itemPlates = new List<SpriteEntry>();

        private Dictionary<string, Sprite> _iconLookup;
        private Dictionary<string, Sprite> _plateLookup;

        private void OnEnable()
        {
            _iconLookup = null;
            _plateLookup = null;
        }

        public void InvalidateCache()
        {
            _iconLookup = null;
            _plateLookup = null;
        }

        public Sprite GetItemIcon(string art, string footprint, bool compact)
        {
            EnsureLookups();
            if (string.IsNullOrEmpty(art)) return null;

            if (compact)
            {
                string cKey = art + "_c_" + footprint;
                if (_iconLookup.TryGetValue(cKey, out Sprite cs) && cs != null) return cs;
            }

            string dKey = art + "_d_" + footprint;
            if (_iconLookup.TryGetValue(dKey, out Sprite ds) && ds != null) return ds;

            string fallback1x1 = art + "_d_1x1";
            return _iconLookup.TryGetValue(fallback1x1, out Sprite f) ? f : null;
        }

        public Sprite GetAuthoredPlate(string tierLower, bool highlight, string shapeKey)
        {
            EnsureLookups();
            string state = highlight ? "hi" : "rest";
            string key = tierLower + "_" + state + "_" + shapeKey;
            return _plateLookup.TryGetValue(key, out Sprite s) ? s : null;
        }

        private void EnsureLookups()
        {
            if (_iconLookup == null)
            {
                _iconLookup = new Dictionary<string, Sprite>(StringComparer.Ordinal);
                foreach (SpriteEntry e in itemIcons)
                {
                    if (e != null && !string.IsNullOrEmpty(e.key) && e.sprite != null)
                        _iconLookup[e.key] = e.sprite;
                }
            }

            if (_plateLookup == null)
            {
                _plateLookup = new Dictionary<string, Sprite>(StringComparer.Ordinal);
                foreach (SpriteEntry e in itemPlates)
                {
                    if (e != null && !string.IsNullOrEmpty(e.key) && e.sprite != null)
                        _plateLookup[e.key] = e.sprite;
                }
            }
        }
    }
}
