using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project05.CombatScene
{
    /// <summary>
    /// Resolves authored item plates or synthesizes pixel-exact rotated plates (e.g. 3x1, 3x2, rotated T/S)
    /// matching the Grimpack tier plate palette and silhouette rules (Art Bible 3.4.4).
    /// </summary>
    public sealed class ProceduralPlateBuilder : IDisposable
    {
        public const int CellPx = 20;

        private static readonly Color32 Interior = new Color32(0x22, 0x2B, 0x44, 0xFF);
        private static readonly Color32 RedTooth = new Color32(0xFF, 0x8A, 0x8F, 0xFF);

        private readonly Dictionary<string, Sprite> _cache = new Dictionary<string, Sprite>(StringComparer.Ordinal);
        private readonly List<Texture2D> _textures = new List<Texture2D>();

        /// <summary>
        /// Returns an authored plate sprite if available for the exact rotated shape, or generates a crisp
        /// pixel-art plate for arbitrary rotated cell sets.
        /// </summary>
        public Sprite GetOrBuildPlate(
            CombatSceneSpriteCatalog catalog,
            string tierName,
            string baseFootprint,
            int orientation,
            bool highlight,
            IReadOnlyList<(int x, int y)> rotatedCells)
        {
            string tier = string.IsNullOrEmpty(tierName) ? "white" : tierName.ToLowerInvariant();
            string authoredShape = ResolveAuthoredShapeKey(baseFootprint, orientation);
            if (!string.IsNullOrEmpty(authoredShape) && catalog != null)
            {
                Sprite authored = catalog.GetAuthoredPlate(tier, highlight, authoredShape);
                if (authored != null) return authored;
            }

            string cacheKey = BuildCacheKey(tier, highlight, rotatedCells);
            if (_cache.TryGetValue(cacheKey, out Sprite cached)) return cached;

            Sprite created = BuildSprite(tier, highlight, rotatedCells);
            _cache[cacheKey] = created;
            return created;
        }

        public static string ResolveAuthoredShapeKey(string baseFootprint, int orientation)
        {
            int o = ((orientation % 4) + 4) % 4;
            switch (baseFootprint)
            {
                case "1x1":
                case "2x2":
                    return baseFootprint;
                case "1x2":
                    return (o % 2 == 0) ? "1x2" : "2x1";
                case "2x1":
                    return (o % 2 == 0) ? "2x1" : "1x2";
                case "1x3":
                    return (o % 2 == 0) ? "1x3" : null;
                case "2x3":
                    return (o % 2 == 0) ? "2x3" : null;
                case "l":
                    return "l" + o;
                case "s":
                    return (o % 2 == 0) ? "s" : null;
                case "t":
                    return o == 0 ? "t" : null;
                default:
                    return o == 0 ? baseFootprint : null;
            }
        }

        private static string BuildCacheKey(string tier, bool highlight, IReadOnlyList<(int x, int y)> cells)
        {
            var sb = new System.Text.StringBuilder(32);
            sb.Append(tier).Append(highlight ? "_hi_" : "_rest_");
            for (int i = 0; i < cells.Count; i++)
            {
                if (i > 0) sb.Append(';');
                sb.Append(cells[i].x).Append(',').Append(cells[i].y);
            }
            return sb.ToString();
        }

        private Sprite BuildSprite(string tier, bool highlight, IReadOnlyList<(int x, int y)> cells)
        {
            int maxX = 0;
            int maxY = 0;
            for (int i = 0; i < cells.Count; i++)
            {
                if (cells[i].x > maxX) maxX = cells[i].x;
                if (cells[i].y > maxY) maxY = cells[i].y;
            }

            int gridW = maxX + 1;
            int gridH = maxY + 1;
            int pxW = gridW * CellPx;
            int pxH = gridH * CellPx;

            bool[,] occ = new bool[gridW, gridH];
            for (int i = 0; i < cells.Count; i++)
            {
                int cx = cells[i].x;
                int cy = cells[i].y;
                if (cx >= 0 && cx < gridW && cy >= 0 && cy < gridH)
                    occ[cx, cy] = true;
            }

            bool IsPixelOccupied(int px, int py)
            {
                if (px < 0 || py < 0 || px >= pxW || py >= pxH) return false;
                return occ[px / CellPx, py / CellPx];
            }

            GetTierColors(tier, highlight, out Color32 outerCol, out Color32 ringCol);

            var tex = new Texture2D(pxW, pxH, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            _textures.Add(tex);

            Color32[] pixels = new Color32[pxW * pxH];
            for (int y = 0; y < pxH; y++) // y=0 is top in screen coords
            {
                for (int x = 0; x < pxW; x++)
                {
                    if (!IsPixelOccupied(x, y)) continue;

                    bool edge1 = !IsPixelOccupied(x - 1, y) || !IsPixelOccupied(x + 1, y)
                              || !IsPixelOccupied(x, y - 1) || !IsPixelOccupied(x, y + 1);
                    bool edge2 = !edge1 && (!IsPixelOccupied(x - 2, y) || !IsPixelOccupied(x + 2, y)
                              || !IsPixelOccupied(x, y - 2) || !IsPixelOccupied(x, y + 2)
                              || !IsPixelOccupied(x - 1, y - 1) || !IsPixelOccupied(x + 1, y - 1)
                              || !IsPixelOccupied(x - 1, y + 1) || !IsPixelOccupied(x + 1, y + 1));

                    Color32 c = edge1 ? outerCol : (edge2 ? ringCol : Interior);

                    // Red serration inward along top horizontal edges
                    if (tier == "red" && !edge1 && !edge2 && !IsPixelOccupied(x, y - 3) && (x % 4 < 2))
                    {
                        c = RedTooth;
                    }

                    // Texture2D y=0 is bottom, so flip Y
                    int texY = pxH - 1 - y;
                    pixels[texY * pxW + x] = c;
                }
            }

            // Simple 2px chamfer on outer convex corners for green+ tiers
            if (tier != "white")
            {
                ApplyChamfers(pixels, pxW, pxH, occ, gridW, gridH, tier == "green", tier == "blue", outerCol, ringCol);
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, false);

            return Sprite.Create(tex, new Rect(0, 0, pxW, pxH), new Vector2(0.5f, 0.5f), 16f);
        }

        private static void ApplyChamfers(
            Color32[] pixels,
            int pxW,
            int pxH,
            bool[,] occ,
            int gridW,
            int gridH,
            bool anchorOnly,
            bool diagonalPair,
            Color32 outerCol,
            Color32 ringCol)
        {
            bool Occ(int cx, int cy) => cx >= 0 && cy >= 0 && cx < gridW && cy < gridH && occ[cx, cy];

            int firstX = -1, firstY = -1;
            int lastX = -1, lastY = -1;
            for (int cy = 0; cy < gridH; cy++)
            {
                for (int cx = 0; cx < gridW; cx++)
                {
                    if (!occ[cx, cy]) continue;
                    if (firstX < 0) { firstX = cx; firstY = cy; }
                    lastX = cx; lastY = cy;
                }
            }

            void SetPx(int sx, int sy, Color32 col)
            {
                if (sx < 0 || sy < 0 || sx >= pxW || sy >= pxH) return;
                pixels[(pxH - 1 - sy) * pxW + sx] = col;
            }

            for (int cy = 0; cy < gridH; cy++)
            {
                for (int cx = 0; cx < gridW; cx++)
                {
                    if (!occ[cx, cy]) continue;
                    int x0 = cx * CellPx;
                    int y0 = cy * CellPx;
                    int x1 = x0 + CellPx - 1;
                    int y1 = y0 + CellPx - 1;

                    bool topLeftConvex = !Occ(cx - 1, cy) && !Occ(cx, cy - 1);
                    bool topRightConvex = !Occ(cx + 1, cy) && !Occ(cx, cy - 1);
                    bool botLeftConvex = !Occ(cx - 1, cy) && !Occ(cx, cy + 1);
                    bool botRightConvex = !Occ(cx + 1, cy) && !Occ(cx, cy + 1);

                    bool isAnchor = cx == firstX && cy == firstY;
                    bool isLast = cx == lastX && cy == lastY;

                    if (topLeftConvex && (!anchorOnly || isAnchor) && (!diagonalPair || isAnchor))
                    {
                        SetPx(x0, y0, default);
                        SetPx(x0 + 1, y0, outerCol);
                        SetPx(x0, y0 + 1, outerCol);
                        SetPx(x0 + 1, y0 + 1, ringCol);
                    }
                    if (topRightConvex && !anchorOnly && !diagonalPair)
                    {
                        SetPx(x1, y0, default);
                        SetPx(x1 - 1, y0, outerCol);
                        SetPx(x1, y0 + 1, outerCol);
                        SetPx(x1 - 1, y0 + 1, ringCol);
                    }
                    if (botLeftConvex && !anchorOnly && !diagonalPair)
                    {
                        SetPx(x0, y1, default);
                        SetPx(x0 + 1, y1, outerCol);
                        SetPx(x0, y1 - 1, outerCol);
                        SetPx(x0 + 1, y1 - 1, ringCol);
                    }
                    if (botRightConvex && !anchorOnly && (!diagonalPair || isLast))
                    {
                        SetPx(x1, y1, default);
                        SetPx(x1 - 1, y1, outerCol);
                        SetPx(x1, y1 - 1, outerCol);
                        SetPx(x1 - 1, y1 - 1, ringCol);
                    }
                }
            }
        }

        private static void GetTierColors(string tier, bool highlight, out Color32 outerCol, out Color32 ringCol)
        {
            switch (tier)
            {
                case "green":
                    outerCol = new Color32(0x14, 0x50, 0x2F, 0xFF);
                    ringCol = highlight ? new Color32(0x8C, 0xF0, 0xB4, 0xFF) : new Color32(0x3C, 0xB8, 0x78, 0xFF);
                    break;
                case "blue":
                    outerCol = new Color32(0x1A, 0x3F, 0x7A, 0xFF);
                    ringCol = highlight ? new Color32(0x9C, 0xCB, 0xFF, 0xFF) : new Color32(0x3E, 0x9B, 0xE8, 0xFF);
                    break;
                case "purple":
                    outerCol = new Color32(0x43, 0x25, 0x7A, 0xFF);
                    ringCol = highlight ? new Color32(0xCF, 0xA8, 0xFF, 0xFF) : new Color32(0x9A, 0x5C, 0xEB, 0xFF);
                    break;
                case "gold":
                    outerCol = new Color32(0x7A, 0x54, 0x10, 0xFF);
                    ringCol = highlight ? new Color32(0xFF, 0xE5, 0x8A, 0xFF) : new Color32(0xE6, 0xB4, 0x22, 0xFF);
                    break;
                case "red":
                    outerCol = new Color32(0x6E, 0x10, 0x20, 0xFF);
                    ringCol = highlight ? new Color32(0xFF, 0x8A, 0x8F, 0xFF) : new Color32(0xE5, 0x26, 0x3F, 0xFF);
                    break;
                default:
                    outerCol = new Color32(0x4A, 0x54, 0x70, 0xFF);
                    ringCol = highlight ? new Color32(0xDD, 0xE3, 0xEE, 0xFF) : new Color32(0xC0, 0xC8, 0xD8, 0xFF);
                    break;
            }
        }

        public void Dispose()
        {
            foreach (Sprite s in _cache.Values)
            {
                CombatUiFactory.DestroySafe(s);
            }
            _cache.Clear();

            foreach (Texture2D tex in _textures)
            {
                CombatUiFactory.DestroySafe(tex);
            }
            _textures.Clear();
        }
    }
}
