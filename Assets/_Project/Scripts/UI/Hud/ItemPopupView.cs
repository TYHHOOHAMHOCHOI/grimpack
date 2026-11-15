using System.Collections.Generic;
using Project05.Items;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project05.UI.Hud
{
    /// <summary>
    /// Draws the item popup (art bible 7.9, 8.8): the panel frame, 152 px wide and 9-sliced to any height up to 232 px,
    /// then a title, the tier and shape in words, and one row per fact with its keyword glyph. Rows are rebuilt on
    /// every <see cref="Build"/>; they are not saved with the scene.
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

        /// <summary>Current popup size in native px.</summary>
        public Vector2 Size { get; private set; }

        /// <summary>Lays the content out and returns the popup size. The popup stays hidden until <see cref="Place"/>.</summary>
        public Vector2 Build(PopupContent content)
        {
            panel.gameObject.SetActive(true);   // text on an inactive object is not measured
            for (int i = panel.childCount - 1; i >= 0; i--)
            {
                GameObject child = panel.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(child); else DestroyImmediate(child);
            }

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
            RectTransform rt = NewRect("Text", x, y, width, 14);
            var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.font = font;
            tmp.fontSize = 12;
            tmp.color = colour;
            tmp.alignment = TextAlignmentOptions.TopLeft;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.margin = Vector4.zero;
            tmp.raycastTarget = false;
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
            RectTransform rt = NewRect("Glyph", x - 1, y - 1, 10, 10);
            var image = rt.gameObject.AddComponent<Image>();
            image.sprite = sprites.Glyph8(kind);
            image.raycastTarget = false;
        }

        private void AddSeparator(float x, float y, float width)
        {
            RectTransform rt = NewRect("Separator", x, y, width, 1);
            var image = rt.gameObject.AddComponent<Image>();
            image.sprite = separator;
            image.raycastTarget = false;
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
