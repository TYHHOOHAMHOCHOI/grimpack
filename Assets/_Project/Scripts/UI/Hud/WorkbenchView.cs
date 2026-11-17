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
        [SerializeField] private RectTransform gridTray;
        [SerializeField] private RectTransform stashTray;
        [SerializeField] private RectTransform stashHint;

        /// <summary>Lays the column out for a hero grid of size x size.</summary>
        public WorkbenchRects Layout(int size)
        {
            WorkbenchRects r = WorkbenchLayout.For(size);
            int n = Mathf.Clamp(size, WorkbenchLayout.StartGrid, WorkbenchLayout.MaxGrid);
            Place(gridBed, r.GridOrigin, new Vector2(n, n) * WorkbenchLayout.Cell);
            Place(sectionLine, new Vector2(WorkbenchLayout.Left - 2, r.SectionLineY), new Vector2(WorkbenchLayout.Stash * WorkbenchLayout.Cell + 4, 3));
            stashLabel.anchoredPosition = new Vector2(WorkbenchLayout.Left, -r.StashLabelY);
            Vector2 stashSize = new Vector2(WorkbenchLayout.Stash, WorkbenchLayout.StashRows) * WorkbenchLayout.Cell;
            Place(stashBed, r.StashOrigin, stashSize);
            // planning-screen.md P13 / P17: leather trays 2 px around the cells
            if (gridTray != null) Place(gridTray, r.GridOrigin - new Vector2(2, 2), new Vector2(n, n) * WorkbenchLayout.Cell + new Vector2(4, 4));
            if (stashTray != null) Place(stashTray, r.StashOrigin - new Vector2(2, 2), stashSize + new Vector2(4, 4));
            if (stashHint != null) Place(stashHint, r.StashOrigin + new Vector2(0, stashSize.y / 2f - 6f), new Vector2(stashSize.x, 12));
            return r;
        }

        /// <summary>Shows the empty-stash hint ("Đồ thưởng sẽ vào đây") while the stash holds nothing.</summary>
        public void SetStashEmpty(bool empty)
        {
            if (stashHint != null && stashHint.gameObject.activeSelf != empty) stashHint.gameObject.SetActive(empty);
        }

        private static void Place(RectTransform rt, Vector2 topLeft, Vector2 size)
        {
            rt.anchoredPosition = new Vector2(topLeft.x, -topLeft.y);
            rt.sizeDelta = size;
        }
    }
}
