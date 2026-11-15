using UnityEngine;

namespace Project05.UI.Hud
{
    /// <summary>Converts UI rects into safe-frame native pixels (x right, y down from the top-left corner).</summary>
    public static class FrameMath
    {
        private static readonly Vector3[] Corners = new Vector3[4];

        /// <summary>The axis-aligned box of a rect in the safe frame. Uses all four corners, so a turned plate is boxed right.</summary>
        public static Rect RectOf(RectTransform rt, RectTransform safeFrame)
        {
            rt.GetWorldCorners(Corners);
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);
            foreach (Vector3 corner in Corners)
            {
                Vector2 p = safeFrame.InverseTransformPoint(corner);
                min = Vector2.Min(min, p);
                max = Vector2.Max(max, p);
            }

            float halfW = safeFrame.rect.width / 2f;
            float halfH = safeFrame.rect.height / 2f;
            return new Rect(min.x + halfW, halfH - max.y, max.x - min.x, max.y - min.y);
        }
    }
}
