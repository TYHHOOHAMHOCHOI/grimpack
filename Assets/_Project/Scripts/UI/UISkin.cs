using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Project05.Core;

namespace Project05.UI
{
    /// <summary>
    /// PixelLab-drawn UI skin (Resources/Sprites/UI/parts). Builds 9-slice sprites with
    /// borders defined here, so the look does not depend on per-asset import settings.
    /// Every Apply call returns false when a sprite is missing, letting callers keep the flat-colour look.
    /// </summary>
    public static class UISkin
    {
        public const string PanelFrame = "panel_frame";
        public const string Button = "btn_tab";
        public const string SlotCommon = "slot_common";
        public const string SlotUncommon = "slot_uncommon";
        public const string SlotRare = "slot_rare";
        public const string SlotEpic = "slot_epic";
        public const string SlotLegendary = "slot_legendary";
        public const string SlotCursed = "slot_cursed";
        public const string AvatarRing = "avatar_ring";
        public const string AvatarBackdrop = "avatar_backdrop";

        /// <summary>Interior colour of the slot sprites, used to blend multi-cell item bridges.</summary>
        public static readonly Color SlotInterior = new Color32(44, 51, 68, 255);
        public static readonly Color SlotCursedInterior = new Color32(49, 22, 29, 255);

        private const string ResourceFolder = "Sprites/UI/parts/";
        private const float SourcePixelsPerUnit = 16f;
        private const float CanvasReferencePixelsPerUnit = 100f;

        // 9-slice borders in source pixels: left, bottom, right, top.
        private static readonly Dictionary<string, Vector4> Borders = new Dictionary<string, Vector4>
        {
            { PanelFrame, new Vector4(22f, 22f, 22f, 22f) },
            { Button, new Vector4(13f, 11f, 13f, 11f) },
            { SlotCommon, new Vector4(12f, 12f, 12f, 12f) },
            { SlotUncommon, new Vector4(12f, 12f, 12f, 12f) },
            { SlotRare, new Vector4(12f, 12f, 12f, 12f) },
            { SlotEpic, new Vector4(12f, 12f, 12f, 12f) },
            { SlotLegendary, new Vector4(12f, 12f, 12f, 12f) },
            { SlotCursed, new Vector4(12f, 12f, 12f, 12f) },
        };

        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        public static Sprite Get(string name)
        {
            if (Cache.TryGetValue(name, out Sprite cached)) return cached;

            Sprite sprite = null;
            Texture2D tex = Resources.Load<Texture2D>(ResourceFolder + name);
            if (tex != null)
            {
                tex.filterMode = FilterMode.Point;
                tex.wrapMode = TextureWrapMode.Clamp;
                Borders.TryGetValue(name, out Vector4 border);
                sprite = Sprite.Create(
                    tex,
                    new Rect(0, 0, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f),
                    SourcePixelsPerUnit,
                    0,
                    SpriteMeshType.FullRect,
                    border);
                sprite.name = "UISkin_" + name;
            }

            Cache[name] = sprite;
            return sprite;
        }

        /// <summary>
        /// Skins an Image with a 9-sliced sprite. pixelScale = canvas units per source pixel.
        /// Removes the flat Outline effect so the sprite's own frame is the border.
        /// </summary>
        public static bool Apply(Image img, string name, float pixelScale = 1f, Color? tint = null)
        {
            if (img == null) return false;
            Sprite sprite = Get(name);
            if (sprite == null) return false;

            img.sprite = sprite;
            img.type = Borders.ContainsKey(name) ? Image.Type.Sliced : Image.Type.Simple;
            img.fillCenter = true;
            img.pixelsPerUnitMultiplier = CanvasReferencePixelsPerUnit / (SourcePixelsPerUnit * Mathf.Max(0.01f, pixelScale));
            img.color = tint ?? Color.white;

            Outline outline = img.GetComponent<Outline>();
            if (outline != null) outline.enabled = false;
            return true;
        }

        public static string SlotForTier(CardTier tier)
        {
            switch (tier)
            {
                case CardTier.Green: return SlotUncommon;
                case CardTier.Blue: return SlotRare;
                case CardTier.Purple: return SlotEpic;
                case CardTier.Gold: return SlotLegendary;
                case CardTier.Red: return SlotCursed;
                default: return SlotCommon;
            }
        }

        /// <summary>Keeps a colour's hue but lifts it to full brightness, then mixes toward white, for tinting grey sprites.</summary>
        public static Color HueTint(Color baseColor, float strength = 0.45f)
        {
            float max = Mathf.Max(baseColor.r, Mathf.Max(baseColor.g, baseColor.b));
            if (max <= 0.001f) return Color.white;
            Color lifted = new Color(baseColor.r / max, baseColor.g / max, baseColor.b / max, 1f);
            return Color.Lerp(Color.white, lifted, strength);
        }

        /// <summary>
        /// Round portrait: dark backdrop, the given sprite, then the PixelLab ring on top.
        /// Returns the sprite Image, or null when the ring art is missing (caller keeps its old box).
        /// </summary>
        public static Image BuildRingPortrait(Transform parent, Sprite portrait)
        {
            Sprite ring = Get(AvatarRing);
            Sprite backdrop = Get(AvatarBackdrop);
            if (ring == null) return null;

            if (backdrop != null)
            {
                Image bd = CreateFullImage("RingBackdrop", parent, backdrop);
                bd.preserveAspect = true;
            }

            RectTransform spriteRt = UIBuilderUtility.CreateRect("PortraitSprite", parent, new Vector2(0.2f, 0.2f), new Vector2(0.8f, 0.8f), Vector2.zero, Vector2.zero);
            Image spriteImg = spriteRt.gameObject.AddComponent<Image>();
            spriteImg.sprite = portrait;
            spriteImg.preserveAspect = true;
            spriteImg.raycastTarget = false;

            Image ringImg = CreateFullImage("Ring", parent, ring);
            ringImg.preserveAspect = true;
            return spriteImg;
        }

        private static Image CreateFullImage(string name, Transform parent, Sprite sprite)
        {
            RectTransform rt = UIBuilderUtility.CreateRect(name, parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
            return img;
        }
    }
}
