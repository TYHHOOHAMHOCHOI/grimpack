using UnityEngine;

namespace Project05.UI.Hud
{
    /// <summary>Where the parts of the workbench column sit for one hero grid size, in safe-frame px (x right, y down).</summary>
    public readonly struct WorkbenchRects
    {
        /// <summary>Top-left of the hero grid.</summary>
        public readonly Vector2 GridOrigin;

        /// <summary>Top of the line between the hero grid and the stash.</summary>
        public readonly float SectionLineY;

        /// <summary>Top of the stash label.</summary>
        public readonly float StashLabelY;

        /// <summary>Top-left of the stash.</summary>
        public readonly Vector2 StashOrigin;

        /// <summary>Creates the rects.</summary>
        public WorkbenchRects(Vector2 gridOrigin, float sectionLineY, float stashLabelY, Vector2 stashOrigin)
        {
            GridOrigin = gridOrigin;
            SectionLineY = sectionLineY;
            StashLabelY = stashLabelY;
            StashOrigin = stashOrigin;
        }
    }

    /// <summary>
    /// Layout of the workbench column (user decision 2026-11-16): the hero grid starts at 4 x 4 and can grow to 7 x 7 later;
    /// the shared stash is 8 x 8 and sits right under the hero grid, so it moves down as the grid grows. At 7 x 7 the column
    /// is exactly full (C = 20).
    /// </summary>
    public static class WorkbenchLayout
    {
        /// <summary>Cell edge in native px.</summary>
        public const int Cell = 20;

        /// <summary>Hero grid size at the start of a run.</summary>
        public const int StartGrid = 4;

        /// <summary>Largest hero grid the layout has room for.</summary>
        public const int MaxGrid = 7;

        /// <summary>Stash width and height in cells.</summary>
        public const int Stash = 8;

        /// <summary>Left edge of both containers.</summary>
        public const int Left = 7;

        /// <summary>Top of the hero grid, under the hero name row.</summary>
        public const int GridTop = 36;

        /// <summary>Lowest pixel row the column may use (inside the panel frame).</summary>
        public const int Bottom = 356;

        /// <summary>Computes the layout for a hero grid of size x size; the size is clamped to 4..7.</summary>
        public static WorkbenchRects For(int size)
        {
            int n = Mathf.Clamp(size, StartGrid, MaxGrid);
            float gridBottom = GridTop + n * Cell;
            float line = gridBottom + 3;
            float label = line + 2;
            float stashTop = label + 14;
            return new WorkbenchRects(new Vector2(Left, GridTop), line, label, new Vector2(Left, stashTop));
        }
    }
}
