using Project05.UI.Hud;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project05.CombatScene
{
    /// <summary>
    /// Shared UI construction helpers for runtime CombatScene views, matching the exact
    /// 640x360 pixel-art conventions and Grimpack master palette used in <c>CombatSceneBuilder</c>.
    /// </summary>
    public static class CombatUiFactory
    {
        public static readonly Vector2 TopLeft = new Vector2(0f, 1f);
        public static readonly Vector2 TopCentre = new Vector2(0.5f, 1f);

        public static readonly Color32 Ink0 = new Color32(0x0B, 0x0F, 0x1C, 0xFF);
        public static readonly Color32 Ink1 = new Color32(0x15, 0x1B, 0x2E, 0xFF);
        public static readonly Color32 Ink2 = new Color32(0x22, 0x2B, 0x44, 0xFF);
        public static readonly Color32 Ink3 = new Color32(0x36, 0x41, 0x5F, 0xFF);
        public static readonly Color32 Bone = new Color32(0xE4, 0xDA, 0xC0, 0xFF);
        public static readonly Color32 Fog = new Color32(0x87, 0x95, 0xB3, 0xFF);
        public static readonly Color32 FogHi = new Color32(0xAA, 0xB8, 0xD4, 0xFF);
        public static readonly Color32 Candle = new Color32(0xEF, 0xC1, 0x86, 0xFF);
        public static readonly Color32 Blood = new Color32(0xB0, 0x50, 0x5A, 0xFF);
        public static readonly Color32 GreenValid = new Color32(0x3C, 0xB8, 0x78, 0xFF);

        public static RectTransform CreateRect(string name, Transform parent, float x, float y, float w, float h)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = TopLeft;
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
            return rt;
        }

        public static Image CreateImage(
            string name,
            Transform parent,
            Sprite sprite,
            float x,
            float y,
            float w = 0f,
            float h = 0f,
            bool sliced = false,
            bool raycastTarget = false,
            Color? tint = null)
        {
            float width = w > 0f ? w : (sprite != null ? sprite.rect.width : 4f);
            float height = h > 0f ? h : (sprite != null ? sprite.rect.height : 4f);
            RectTransform rt = CreateRect(name, parent, x, y, width, height);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            img.raycastTarget = raycastTarget;
            if (tint.HasValue) img.color = tint.Value;
            return img;
        }

        public static TextMeshProUGUI CreateLabel(
            string name,
            Transform parent,
            TMP_FontAsset font,
            string text,
            float x,
            float y,
            float w,
            float h,
            Color color,
            TextAlignmentOptions alignment = TextAlignmentOptions.TopLeft,
            bool wrap = false)
        {
            RectTransform rt = CreateRect(name, parent, x, y, w, h);
            var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.font = font;
            tmp.fontSize = 12;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            tmp.overflowMode = wrap ? TextOverflowModes.Ellipsis : TextOverflowModes.Overflow;
            tmp.margin = Vector4.zero;
            tmp.raycastTarget = false;
            tmp.text = text;
            return tmp;
        }

        public static SpriteNumeralText CreateNumeral(
            string name,
            Transform parent,
            Sprite atlas,
            TextAsset metrics,
            float x,
            float y,
            string text)
        {
            RectTransform rt = CreateRect(name, parent, x, y, 0f, 0f);
            var num = rt.gameObject.AddComponent<SpriteNumeralText>();
            num.Configure(atlas, metrics, SpriteNumeralText.Align.Left);
            num.Text = text;
            return num;
        }

        public static Button CreateSegmentButton(
            string name,
            Transform parent,
            CombatSceneSpriteCatalog catalog,
            string label,
            float x,
            float y,
            float w,
            float h,
            bool selected = false)
        {
            Sprite bgSprite = selected ? catalog.segmentSelected : catalog.segmentDefault;
            Image img = CreateImage(name, parent, bgSprite, x, y, w, h, sliced: true, raycastTarget: true);
            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.transition = Selectable.Transition.None;

            CreateLabel("Label", img.transform, catalog.font, label, 2f, Mathf.Floor((h - 12f) * 0.5f), w - 4f, 12f,
                selected ? Candle : Bone, TextAlignmentOptions.Top);
            return btn;
        }

        public static void SetSegmentVisual(Button button, CombatSceneSpriteCatalog catalog, bool selected)
        {
            if (button == null || catalog == null) return;
            var img = button.GetComponent<Image>();
            if (img != null) img.sprite = selected ? catalog.segmentSelected : catalog.segmentDefault;
            var lbl = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (lbl != null) lbl.color = selected ? Candle : Bone;
        }

        public static void DestroySafe(UnityEngine.Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(obj);
            else UnityEngine.Object.DestroyImmediate(obj);
        }
    }
}
