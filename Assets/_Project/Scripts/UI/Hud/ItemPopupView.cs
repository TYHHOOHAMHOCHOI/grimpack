using System.Collections.Generic;
using Project05.Items;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project05.UI.Hud
{
    /// <summary>
    /// Draws the item popup (art bible 7.9, 8.8): the panel frame, 152 px wide and 9-sliced to any height up to 232 px,
    /// then a title, the tier and shape in words, and one row per fact with its keyword glyph. Rows are pooled and reused
    /// on every <see cref="Build"/>, so swapping between items creates no objects; they are not saved with the scene.
    /// </summary>
    public sealed class ItemPopupView : MonoBehaviour
    {
        /// <summary>Popup width in native px.</summary>
        public const int Width = 152;

        /// <summary>Tallest popup in native px; more content is cut here and needs the Details page (not built yet).</summary>
        public const int MaxHeight = 232;

        private const int Pad = 7;       // 3 px frame, 1 px highlight, 3 px air
        private const int GlyphIndent = 12;

        [SerializeField] private RectTransform panel;
        [SerializeField] private TMP_FontAsset font;
        [SerializeField] private HudSpriteSet sprites;
        [SerializeField] private Sprite separator;

        private static readonly Color Bone = new Color32(0xE4, 0xDA, 0xC0, 0xFF);
        private static readonly Color Fog = new Color32(0x87, 0x95, 0xB3, 0xFF);

        private readonly List<TextMeshProUGUI> _texts = new List<TextMeshProUGUI>();
        private readonly List<Image> _glyphs = new List<Image>();
        private Image _separator;
        private int _textUsed;
        private int _glyphUsed;

        /// <summary>Current popup size in native px.</summary>
        public Vector2 Size { get; private set; }

        /// <summary>Lays the content out and returns the popup size. The popup stays hidden until <see cref="Place"/>.</summary>
        public Vector2 Build(PopupContent content)
        {
            panel.gameObject.SetActive(true);   // text on an inactive object is not measured
            if (_texts.Count == 0 && _glyphs.Count == 0 && _separator == null) ClearStrayChildren();
            _textUsed = 0;
            _glyphUsed = 0;

            int inner = Width - 2 * Pad;
            float y = Pad;
            y += AddText(content.Title, Pad, y, inner, Bone) + 1;
            y += AddText(content.Subtitle, Pad, y, inner, Fog) + 3;
            AddSeparator(Pad, y, inner);
            y += 4;

            foreach (PopupLine line in content.Lines)
            {
                bool glyph = line.Glyph.HasValue;
                float x = Pad + (glyph ? GlyphIndent : 0);
                if (glyph) AddGlyph(line.Glyph.Value, Pad, y + 2);
                y += AddText(line.Text, x, y, inner - (glyph ? GlyphIndent : 0), line.Dim ? Fog : Bone) + 3;
            }

            for (int i = _textUsed; i < _texts.Count; i++) _texts[i].gameObject.SetActive(false);
            for (int i = _glyphUsed; i < _glyphs.Count; i++) _glyphs[i].gameObject.SetActive(false);

            float height = Mathf.Min(MaxHeight, Mathf.Ceil(y + Pad - 3));
            panel.sizeDelta = new Vector2(Width, height);
            Size = panel.sizeDelta;
            return Size;
        }

        /// <summary>Moves the popup to a top-left position in the safe frame and shows it.</summary>
        public void Place(Vector2 topLeft)
        {
            panel.anchoredPosition = new Vector2(Mathf.Round(topLeft.x), -Mathf.Round(topLeft.y));
            panel.gameObject.SetActive(true);
        }

        /// <summary>Hides the popup.</summary>
        public void Hide() => panel.gameObject.SetActive(false);

        private float AddText(string text, float x, float y, float width, Color colour)
        {
            if (_textUsed == _texts.Count)
            {
                var created = NewRect("Text", 0, 0, width, 14).gameObject.AddComponent<TextMeshProUGUI>();
                created.font = font;
                created.fontSize = 12;
                created.alignment = TextAlignmentOptions.TopLeft;
                created.textWrappingMode = TextWrappingModes.Normal;
                created.overflowMode = TextOverflowModes.Overflow;
                created.margin = Vector4.zero;
                created.raycastTarget = false;
                _texts.Add(created);
            }

            TextMeshProUGUI tmp = _texts[_textUsed++];
            RectTransform rt = tmp.rectTransform;
            tmp.gameObject.SetActive(true);
            rt.anchoredPosition = new Vector2(x, -y);
            tmp.color = colour;
            tmp.text = text;
            rt.sizeDelta = new Vector2(width, 14f);
            tmp.ForceMeshUpdate();
            // the face has a 14 px line pitch (art bible 7.7), so the height is the number of wrapped lines times 14
            float h = Mathf.Max(1, tmp.textInfo.lineCount) * 14f;
            rt.sizeDelta = new Vector2(width, h);
            return h;
        }

        private void AddGlyph(GlyphKind kind, float x, float y)
        {
            if (_glyphUsed == _glyphs.Count)
            {
                Image created = NewRect("Glyph", 0, 0, 10, 10).gameObject.AddComponent<Image>();
                created.raycastTarget = false;
                _glyphs.Add(created);
            }

            Image image = _glyphs[_glyphUsed++];
            image.gameObject.SetActive(true);
            image.rectTransform.anchoredPosition = new Vector2(x - 1, -(y - 1));
            image.sprite = sprites.Glyph8(kind);
        }

        private void AddSeparator(float x, float y, float width)
        {
            if (_separator == null)
            {
                _separator = NewRect("Separator", x, y, width, 1).gameObject.AddComponent<Image>();
                _separator.sprite = separator;
                _separator.raycastTarget = false;
            }

            _separator.rectTransform.anchoredPosition = new Vector2(x, -y);
            _separator.rectTransform.sizeDelta = new Vector2(width, 1);
        }

        // Rows left in the panel by an earlier build (edit-mode captures) are not in the pools: remove them once.
        private void ClearStrayChildren()
        {
            for (int i = panel.childCount - 1; i >= 0; i--)
            {
                GameObject child = panel.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(child); else DestroyImmediate(child);
            }
        }

        private RectTransform NewRect(string name, float x, float y, float w, float h)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.hideFlags = HideFlags.DontSave;
            go.transform.SetParent(panel, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
            return rt;
        }
    }
}
