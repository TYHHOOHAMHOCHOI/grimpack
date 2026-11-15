using UnityEngine;
using UnityEngine.EventSystems;

namespace Project05.UI.Hud
{
    /// <summary>
    /// Makes an item plate a source for the hover popup. Pointer hover starts the 300 ms delay; keyboard or gamepad
    /// selection opens the popup at once (hover-popup-and-glossary.md rule 2).
    /// </summary>
    public sealed class ItemHoverTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        [SerializeField] private string itemId = string.Empty;
        [SerializeField] private int cellX;
        [SerializeField] private int cellY;
        [SerializeField] private ItemGridContext grid;
        [SerializeField] private ItemPopupController controller;

        /// <summary>Definition id of the item.</summary>
        public string ItemId => itemId;

        /// <summary>Grid column of the item origin.</summary>
        public int CellX => cellX;

        /// <summary>Grid row of the item origin.</summary>
        public int CellY => cellY;

        /// <summary>The hero grid this item sits in, or null for the stash.</summary>
        public ItemGridContext Grid => grid;

        /// <summary>The rectangle of the plate.</summary>
        public RectTransform Rect => (RectTransform)transform;

        /// <summary>Configures the target. Called by the scene builder.</summary>
        public void Configure(string id, int x, int y, ItemGridContext gridContext, ItemPopupController popup)
        {
            itemId = id;
            cellX = x;
            cellY = y;
            grid = gridContext;
            controller = popup;
        }

        public void OnPointerEnter(PointerEventData eventData) => controller.PointerEntered(this);

        public void OnPointerExit(PointerEventData eventData) => controller.PointerExited(this);

        public void OnSelect(BaseEventData eventData) => controller.ShowNow(this);

        public void OnDeselect(BaseEventData eventData) => controller.PointerExited(this);
    }
}
