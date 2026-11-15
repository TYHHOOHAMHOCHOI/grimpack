using System.Collections.Generic;
using UnityEngine;

namespace Project05.UI.Hud
{
    /// <summary>
    /// Chooses where the item popup opens (art bible 7.9, hover-popup-and-glossary.md rule 5). Coordinates are native
    /// pixels in the safe frame, x right and y down. A popup never touches the source or its focus ring; inside the
    /// workbench it may cover empty cells and, at a cost, other plates.
    /// </summary>
    public static class ItemPopupPlacement
    {
        /// <summary>Gap in native px kept between the source and the popup (ring plus its 1 px gap).</summary>
        public const int Gap = 4;

        /// <summary>
        /// Picks the legal rectangle that covers the fewest occupied pixels. Candidates, in order of preference:
        /// below, above, right, left. Each is clamped into the bounds.
        /// </summary>
        /// <param name="bounds">The region the popup must stay inside.</param>
        /// <param name="source">The hovered object.</param>
        /// <param name="occupied">Rectangles of other items that the popup would hide.</param>
        /// <param name="size">The popup size.</param>
        /// <returns>The chosen rectangle.</returns>
        public static Rect Choose(Rect bounds, Rect source, IReadOnlyList<Rect> occupied, Vector2 size)
        {
            Rect[] candidates =
            {
                new Rect(source.x, source.yMax + Gap, size.x, size.y),
                new Rect(source.x, source.y - Gap - size.y, size.x, size.y),
                new Rect(source.xMax + Gap, source.y, size.x, size.y),
                new Rect(source.x - Gap - size.x, source.y, size.x, size.y),
            };

            Rect best = default;
            float bestCost = float.MaxValue;
            bool found = false;
            foreach (Rect raw in candidates)
            {
                Rect r = Clamp(raw, bounds);
                if (Overlaps(r, Inflate(source, Gap - 1))) continue;
                float cost = 0f;
                foreach (Rect o in occupied) cost += OverlapArea(r, o);
                if (!found || cost < bestCost)
                {
                    best = r;
                    bestCost = cost;
                    found = true;
                }
            }

            return found ? best : Clamp(candidates[0], bounds);
        }

        private static Rect Clamp(Rect r, Rect bounds)
        {
            float x = Mathf.Clamp(r.x, bounds.x, Mathf.Max(bounds.x, bounds.xMax - r.width));
            float y = Mathf.Clamp(r.y, bounds.y, Mathf.Max(bounds.y, bounds.yMax - r.height));
            return new Rect(x, y, r.width, r.height);
        }

        private static Rect Inflate(Rect r, float by) => new Rect(r.x - by, r.y - by, r.width + 2 * by, r.height + 2 * by);

        private static bool Overlaps(Rect a, Rect b) => OverlapArea(a, b) > 0f;

        private static float OverlapArea(Rect a, Rect b)
        {
            float w = Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.x, b.x);
            float h = Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.y, b.y);
            return w > 0f && h > 0f ? w * h : 0f;
        }
    }
}
