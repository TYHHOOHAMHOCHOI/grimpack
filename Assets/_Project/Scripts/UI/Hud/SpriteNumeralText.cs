using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Project05.UI.Hud
{
    /// <summary>
    /// Draws a number from the pre-authored numeral atlas (art bible 7.7, 8.9): one cut per size, outline baked in,
    /// never scaled. Text meshes cannot carry the baked outline, so digits are small Image children.
    /// The children are not saved with the scene; they are rebuilt on enable.
    /// </summary>
    [ExecuteAlways]
    public sealed class SpriteNumeralText : MonoBehaviour
    {
        /// <summary>Horizontal anchor of the text relative to this object position.</summary>
        public enum Align { Left, Center, Right }

        [Serializable]
        private sealed class GlyphRect
        {
            public string ch = string.Empty;
            public int x;
            public int y;
            public int w;
            public int h;
            public int advance;
        }

        [Serializable]
        private sealed class Metrics
        {
            public int cut;
            public int outline;
            public int lineHeight;
            public GlyphRect[] glyphs = Array.Empty<GlyphRect>();
        }

        [SerializeField] private Sprite atlas;
        [SerializeField] private TextAsset metrics;
        [SerializeField] private string text = string.Empty;
        [SerializeField] private Align align = Align.Left;

        // Shared by every numeral: parsing the metrics and cutting glyph sprites happen once per atlas, not once per
        // text change. Keyed by asset, so a changed atlas gets new entries.
        private static readonly Dictionary<TextAsset, Metrics> ParsedMetrics = new Dictionary<TextAsset, Metrics>();
        private static readonly Dictionary<(Sprite atlas, char ch), Sprite> GlyphSprites = new Dictionary<(Sprite, char), Sprite>();

        // Glyph images are reused: changing "86/110" to "74/110" moves and re-skins existing children instead of
        // destroying and creating objects every frame of combat playback.
        private readonly List<Image> _pool = new List<Image>();
        private Metrics _parsed;
        private Dictionary<char, GlyphRect> _byChar;

        /// <summary>The text shown. Setting it lays the digits out again.</summary>
        public string Text
        {
            get => text;
            set
            {
                string next = value ?? string.Empty;
                if (text == next && _laidOut) return;
                text = next;
                Rebuild();
            }
        }

        private bool _laidOut;

        /// <summary>Width in native pixels of the current text.</summary>
        public int Width { get; private set; }

        /// <summary>Sets the atlas, metrics and alignment, for text created from code.</summary>
        /// <param name="atlasSprite">The numeral atlas of one cut and colourway.</param>
        /// <param name="metricsAsset">The JSON metrics of that cut.</param>
        /// <param name="alignment">Horizontal anchor relative to this object position.</param>
        public void Configure(Sprite atlasSprite, TextAsset metricsAsset, Align alignment)
        {
            atlas = atlasSprite;
            metrics = metricsAsset;
            align = alignment;
            _parsed = null;
            _byChar = null;
            _laidOut = false;
        }

        private void OnEnable() => Rebuild();

#if UNITY_EDITOR
        // Children cannot be created or destroyed inside OnValidate, so the rebuild waits for the editor loop.
        private void OnValidate()
        {
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null && isActiveAndEnabled) Rebuild();
            };
        }
#endif

        /// <summary>Lays the digits out again from the current text, atlas and metrics.</summary>
        public void Rebuild()
        {
            if (atlas == null || metrics == null) return;
            if (_parsed == null || _byChar == null)   // a domain reload keeps _parsed (serializable) but not the dictionary
            {
                if (_parsed == null && !ParsedMetrics.TryGetValue(metrics, out _parsed))
                {
                    _parsed = JsonUtility.FromJson<Metrics>(metrics.text);
                    ParsedMetrics[metrics] = _parsed;
                }

                _byChar = new Dictionary<char, GlyphRect>();
                foreach (GlyphRect g in _parsed.glyphs)
                {
                    if (!string.IsNullOrEmpty(g.ch)) _byChar[g.ch[0]] = g;
                }
            }

            // Children that are not in the pool (left by an editor reload) are removed once.
            if (_pool.Count == 0 && transform.childCount > 0)
            {
                for (int i = transform.childCount - 1; i >= 0; i--)
                {
                    GameObject child = transform.GetChild(i).gameObject;
                    if (Application.isPlaying) Destroy(child); else DestroyImmediate(child);
                }
            }

            int total = _parsed.outline * 2;
            foreach (char c in text)
            {
                if (_byChar.TryGetValue(c, out GlyphRect g)) total += g.advance;
            }

            Width = total;
            int x = align == Align.Left ? 0 : align == Align.Center ? -total / 2 : -total;
            int used = 0;
            foreach (char c in text)
            {
                if (!_byChar.TryGetValue(c, out GlyphRect g)) continue;
                Image image = used < _pool.Count ? _pool[used] : NewGlyph();
                used++;
                if (!image.gameObject.activeSelf) image.gameObject.SetActive(true);
                image.sprite = GlyphSprite(g);
                RectTransform rt = image.rectTransform;
                rt.anchoredPosition = new Vector2(x, 0f);
                rt.sizeDelta = new Vector2(g.w, g.h);
                x += g.advance;
            }

            for (int i = used; i < _pool.Count; i++)
            {
                if (_pool[i].gameObject.activeSelf) _pool[i].gameObject.SetActive(false);
            }

            _laidOut = true;
        }

        private Sprite GlyphSprite(GlyphRect g)
        {
            var key = (atlas, g.ch[0]);
            if (GlyphSprites.TryGetValue(key, out Sprite sprite) && sprite != null && sprite.texture == atlas.texture) return sprite;
            Texture2D tex = atlas.texture;
            var rect = new Rect(g.x, tex.height - g.y - g.h, g.w, g.h);
            sprite = Sprite.Create(tex, rect, new Vector2(0f, 1f), atlas.pixelsPerUnit, 0, SpriteMeshType.FullRect);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            GlyphSprites[key] = sprite;
            return sprite;
        }

        private Image NewGlyph()
        {
            var go = new GameObject("g", typeof(RectTransform), typeof(Image));
            go.hideFlags = HideFlags.DontSave;
            go.transform.SetParent(transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            Image image = go.GetComponent<Image>();
            image.raycastTarget = false;
            _pool.Add(image);
            return image;
        }
    }
}
