using UnityEngine;
using UnityEngine.EventSystems;

namespace Project05.UI.Hud
{
    /// <summary>
    /// One item plate on the board. It is the source of the hover popup (pointer hover starts a 300 ms delay, selection
    /// opens it at once) and the handle for dragging, picking up by click and rotating. All decisions are made by
    /// <see cref="ItemBoardController"/> and <see cref="ItemPopupController"/>; this component only forwards events.
    /// </summary>
    public sealed class ItemHoverTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private string itemId = string.Empty;
        [SerializeField] private string container = string.Empty;
        [SerializeField] private int cellX;
        [SerializeField] private int cellY;
        [SerializeField] private int rotation;
        [SerializeField] private ItemBoardController board;
        [SerializeField] private ItemPopupController controller;

        /// <summary>Definition id of the item.</summary>
        public string ItemId => itemId;

        /// <summary>The container the item starts in (hero grid or stash).</summary>
        public string Container => container;

        /// <summary>Starting grid column of the item origin.</summary>
        public int CellX => cellX;

        /// <summary>Starting grid row of the item origin.</summary>
        public int CellY => cellY;

        /// <summary>Starting rotation in clockwise quarter turns.</summary>
        public int Rotation => rotation;

        /// <summary>The board this item belongs to.</summary>
        public ItemBoardController Board => board;

        /// <summary>The rectangle of the plate.</summary>
        public RectTransform Rect => (RectTransform)transform;

        /// <summary>Configures the target. Called by the scene builder.</summary>
        public void Configure(string id, string containerId, int x, int y, int turns, ItemBoardController itemBoard, ItemPopupController popup)
        {
            itemId = id;
            container = containerId;
            cellX = x;
            cellY = y;
            rotation = turns;
            board = itemBoard;
            controller = popup;
        }

        public void OnPointerEnter(PointerEventData eventData) => controller.PointerEntered(this);

        public void OnPointerExit(PointerEventData eventData) => controller.PointerExited(this);

        public void OnSelect(BaseEventData eventData) => controller.ShowNow(this);

        public void OnDeselect(BaseEventData eventData) => controller.PointerExited(this);

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (board != null) board.BeginDrag(this);
        }

        public void OnDrag(PointerEventData eventData)
        {
            // movement is read each frame by the board controller
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (board != null) board.EndDrag(this);
        }
    }
}
