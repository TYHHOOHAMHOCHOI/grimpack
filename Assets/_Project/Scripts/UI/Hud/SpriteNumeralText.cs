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

        private readonly Dictionary<char, Sprite> _sprites = new Dictionary<char, Sprite>();
        private Metrics _parsed;

        /// <summary>The text shown. Setting it rebuilds the digits.</summary>
        public string Text
        {
            get => text;
            set
            {
                string next = value ?? string.Empty;
                if (text == next && transform.childCount > 0) return;
                text = next;
                Rebuild();
            }
        }

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
            _sprites.Clear();
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
            _parsed ??= JsonUtility.FromJson<Metrics>(metrics.text);

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                GameObject child = transform.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(child); else DestroyImmediate(child);
            }

            int total = _parsed.outline * 2;
            foreach (char c in text)
            {
                GlyphRect g = Find(c);
                if (g != null) total += g.advance;
            }

            Width = total;
            int x = align == Align.Left ? 0 : align == Align.Center ? -total / 2 : -total;
            foreach (char c in text)
            {
                GlyphRect g = Find(c);
                if (g == null) continue;
                NewGlyph(g, x);
                x += g.advance;
            }
        }

        private GlyphRect Find(char c)
        {
            string key = c.ToString();
            foreach (GlyphRect g in _parsed.glyphs)
            {
                if (g.ch == key) return g;
            }

            return null;
        }

        private void NewGlyph(GlyphRect g, int x)
        {
            if (!_sprites.TryGetValue(g.ch[0], out Sprite sprite))
            {
                Texture2D tex = atlas.texture;
                var rect = new Rect(g.x, tex.height - g.y - g.h, g.w, g.h);
                sprite = Sprite.Create(tex, rect, new Vector2(0f, 1f), atlas.pixelsPerUnit, 0, SpriteMeshType.FullRect);
                sprite.hideFlags = HideFlags.HideAndDontSave;
                _sprites[g.ch[0]] = sprite;
            }

            var go = new GameObject("g", typeof(RectTransform), typeof(Image));
            go.hideFlags = HideFlags.DontSave;
            go.transform.SetParent(transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, 0f);
            rt.sizeDelta = new Vector2(g.w, g.h);
            Image image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
        }
    }
}
