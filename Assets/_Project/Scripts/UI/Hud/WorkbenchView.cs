using UnityEngine;

namespace Project05.UI.Hud
{
    /// <summary>
    /// Draws the beds of the workbench column: the hero grid bed, the section line, the stash label and the stash bed. The beds
    /// are tiled cells, so a bigger hero grid is only a new size; everything under it moves down (see <see cref="WorkbenchLayout"/>).
    /// </summary>
    public sealed class WorkbenchView : MonoBehaviour
    {
        [SerializeField] private RectTransform gridBed;
        [SerializeField] private RectTransform sectionLine;
        [SerializeField] private RectTransform stashLabel;
        [SerializeField] private RectTransform stashBed;

        /// <summary>Lays the column out for a hero grid of size x size.</summary>
        public WorkbenchRects Layout(int size)
        {
            WorkbenchRects r = WorkbenchLayout.For(size);
            int n = Mathf.Clamp(size, WorkbenchLayout.StartGrid, WorkbenchLayout.MaxGrid);
            Place(gridBed, r.GridOrigin, new Vector2(n, n) * WorkbenchLayout.Cell);
            Place(sectionLine, new Vector2(WorkbenchLayout.Left, r.SectionLineY), new Vector2(WorkbenchLayout.Stash * WorkbenchLayout.Cell, 1));
            stashLabel.anchoredPosition = new Vector2(WorkbenchLayout.Left, -r.StashLabelY);
            Place(stashBed, r.StashOrigin, new Vector2(WorkbenchLayout.Stash, WorkbenchLayout.Stash) * WorkbenchLayout.Cell);
            return r;
        }

        private static void Place(RectTransform rt, Vector2 topLeft, Vector2 size)
        {
            rt.anchoredPosition = new Vector2(topLeft.x, -topLeft.y);
            rt.sizeDelta = size;
        }
    }
}
