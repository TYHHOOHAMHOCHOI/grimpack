using UnityEngine;
using UnityEngine.UI;
using Project05.Core;

namespace Project05.UI
{
    /// <summary>
    /// Helper methods for constructing responsive uGUI hierarchies with zero-GC styling,
    /// full Vietnamese Unicode font support, and non-interactive raycastTarget optimization.
    /// </summary>
    public static class UIBuilderUtility
    {
        private static Font _vietnameseFont;

        public static Font GetVietnameseFont()
        {
            if (_vietnameseFont != null) return _vietnameseFont;

            // Create dynamic OS font supporting full Vietnamese diacritics
            _vietnameseFont = Font.CreateDynamicFontFromOSFont(new[] { "Segoe UI", "Arial", "Tahoma", "Verdana" }, 16);
            if (_vietnameseFont == null)
            {
                _vietnameseFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
            return _vietnameseFont;
        }

        public static void SafeDestroy(Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying)
            {
                Object.Destroy(obj);
            }
            else
            {
                Object.DestroyImmediate(obj);
            }
        }

        public static RectTransform CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            return rt;
        }

        public static Image CreatePanel(
            string name,
            Transform parent,
            Color bgColor,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax,
            bool withOutline = true,
            Color? outlineColor = null,
            bool raycastTarget = false)
        {
            RectTransform rt = CreateRect(name, parent, anchorMin, anchorMax, offsetMin, offsetMax);
            Image img = rt.gameObject.AddComponent<Image>();
            img.sprite = SpriteFactory.GetWhiteSprite();
            img.color = bgColor;
            img.raycastTarget = raycastTarget;

            if (withOutline)
            {
                Outline outline = rt.gameObject.AddComponent<Outline>();
                outline.effectColor = outlineColor ?? Palette.BorderIron;
                outline.effectDistance = new Vector2(1.5f, -1.5f);
            }

            return img;
        }

        public static Text CreateText(
            string name,
            Transform parent,
            string content,
            int fontSize,
            Color color,
            TextAnchor alignment = TextAnchor.MiddleLeft,
            FontStyle fontStyle = FontStyle.Normal,
            bool raycastTarget = false)
        {
            RectTransform rt = CreateRect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Text txt = rt.gameObject.AddComponent<Text>();
            txt.font = GetVietnameseFont();
            txt.text = content;
            txt.fontSize = fontSize;
            txt.color = color;
            txt.alignment = alignment;
            txt.fontStyle = fontStyle;
            txt.horizontalOverflow = HorizontalWrapMode.Wrap;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            txt.supportRichText = true;
            txt.raycastTarget = raycastTarget;
            return txt;
        }

        public static Button CreateButton(
            string name,
            Transform parent,
            string label,
            int fontSize,
            Color bgColor,
            Color textColor,
            Color borderColor,
            UnityEngine.Events.UnityAction onClick)
        {
            Image bg = CreatePanel(name, parent, bgColor, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, true, borderColor, raycastTarget: true);
            UISkin.Apply(bg, UISkin.Button, 1f, UISkin.HueTint(bgColor));
            Button btn = bg.gameObject.AddComponent<Button>();
            btn.targetGraphic = bg;

            ColorBlock cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.18f, 1.18f, 1.18f, 1f);
            cb.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
            cb.selectedColor = Color.white;
            btn.colors = cb;

            Text txt = CreateText("Label", bg.transform, label, fontSize, textColor, TextAnchor.MiddleCenter, FontStyle.Bold, raycastTarget: false);
            txt.rectTransform.offsetMin = new Vector2(6f, 2f);
            txt.rectTransform.offsetMax = new Vector2(-6f, -2f);

            if (onClick != null)
            {
                btn.onClick.AddListener(onClick);
            }

            return btn;
        }
    }
}
