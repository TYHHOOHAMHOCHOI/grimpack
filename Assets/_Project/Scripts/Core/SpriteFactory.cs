using System.Collections.Generic;
using UnityEngine;

namespace Project05.Core
{
    /// <summary>
    /// Loads pixel-art sprites from Resources/Sprites with Point filtering and caches procedural pixel-art fallbacks.
    /// </summary>
    public static class SpriteFactory
    {
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();
        private static Sprite _whitePixelSprite;

        public static Sprite GetWhiteSprite()
        {
            if (_whitePixelSprite != null) return _whitePixelSprite;
            Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            Color[] cols = new Color[16];
            for (int i = 0; i < 16; i++) cols[i] = Color.white;
            tex.SetPixels(cols);
            tex.Apply();
            _whitePixelSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
            return _whitePixelSprite;
        }

        public static Sprite GetSprite(string spriteName, Color fallbackTint)
        {
            if (string.IsNullOrEmpty(spriteName))
            {
                return GetWhiteSprite();
            }

            if (Cache.TryGetValue(spriteName, out Sprite cached) && cached != null)
            {
                return cached;
            }

            // Try loading from Resources/Sprites/<spriteName>
            Sprite loaded = Resources.Load<Sprite>("Sprites/" + spriteName);
            if (loaded != null)
            {
                if (loaded.texture != null)
                {
                    loaded.texture.filterMode = FilterMode.Point;
                }
                Cache[spriteName] = loaded;
                return loaded;
            }

            // Try loading as Texture2D if sprite sub-asset wasn't generated yet
            Texture2D loadedTex = Resources.Load<Texture2D>("Sprites/" + spriteName);
            if (loadedTex != null)
            {
                loadedTex.filterMode = FilterMode.Point;
                Sprite fromTex = Sprite.Create(
                    loadedTex,
                    new Rect(0, 0, loadedTex.width, loadedTex.height),
                    new Vector2(0.5f, 0.5f),
                    16f);
                Cache[spriteName] = fromTex;
                return fromTex;
            }

            // Procedural pixel emblem fallback
            Sprite generated = CreatePixelEmblem(spriteName, fallbackTint);
            Cache[spriteName] = generated;
            return generated;
        }

        private static Sprite CreatePixelEmblem(string key, Color primary)
        {
            const int size = 16;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            Color dark = new Color(primary.r * 0.35f, primary.g * 0.35f, primary.b * 0.35f, 1f);
            Color highlight = Color.Lerp(primary, Color.white, 0.35f);
            Color clear = new Color(0f, 0f, 0f, 0f);
            int hash = Mathf.Abs(key.GetHashCode());

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int mirrorX = x < size / 2 ? x : size - 1 - x;
                    bool border = x == 1 || x == size - 2 || y == 1 || y == size - 2;
                    bool outer = x == 0 || x == size - 1 || y == 0 || y == size - 1;
                    if (outer)
                    {
                        tex.SetPixel(x, y, clear);
                    }
                    else if (border)
                    {
                        tex.SetPixel(x, y, dark);
                    }
                    else
                    {
                        bool fill = ((hash >> ((mirrorX + y) % 16)) & 1) == 1 || (mirrorX >= 4 && y >= 4 && y <= 11);
                        tex.SetPixel(x, y, fill ? (y > 9 ? highlight : primary) : dark);
                    }
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f);
        }
    }
}
