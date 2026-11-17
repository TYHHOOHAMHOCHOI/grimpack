using System.Collections.Generic;
using Project05.Items;
using UnityEngine;

namespace Project05.UI.Hud
{
    /// <summary>
    /// Opens, swaps and closes the one item popup (hover-popup-and-glossary.md rules 2 to 5): a 300 ms hover opens it,
    /// selection opens it at once, a grace keeps it while the pointer crosses a gap or an empty cell, and it is placed
    /// once, inside the workbench, never touching its source. After a popup has been seen, popups are "warm": another
    /// item hovered soon after opens at once, so moving between items never waits or blinks. The popup only reads
    /// definitions; it changes no game state.
    /// </summary>
    public sealed class ItemPopupController : MonoBehaviour
    {
        /// <summary>Delay before a hover opens the popup, in seconds.</summary>
        public const float HoverDelay = 0.3f;

        /// <summary>Default time the popup is kept after the pointer leaves, in seconds.</summary>
        public const float DefaultGrace = 0.2f;

        /// <summary>Default time after a popup closes during which a new hover opens at once, in seconds.</summary>
        public const float DefaultWarmWindow = 0.5f;

        [Tooltip("Seconds the popup stays after the pointer leaves an item (the GDD says 60 ms for a 1 px gap; 0.2 s also covers an empty cell).")]
        [SerializeField] private float grace = DefaultGrace;
        [Tooltip("Seconds after a popup closes during which hovering another item opens its popup without the hover delay.")]
        [SerializeField] private float warmWindow = DefaultWarmWindow;

        [SerializeField] private TextAsset catalogJson;
        [SerializeField] private ItemPopupView view;
        [SerializeField] private RectTransform safeFrame;
        [SerializeField] private RectTransform bounds;

        private ItemCatalog _catalog;
        private bool _suppressed;
        private ItemHoverTarget _pending;
        private ItemHoverTarget _current;
        private float _pendingSince;
        private float _closeAt = -1f;
        private float _closedAt = float.NegativeInfinity;

        /// <summary>The parsed catalog.</summary>
        public ItemCatalog Catalog => _catalog ??= ItemCatalog.Load(catalogJson.text);

        /// <summary>While an item is held no normal popup appears (hover-popup-and-glossary.md rule 4).</summary>
        public bool Suppressed
        {
            get => _suppressed;
            set
            {
                _suppressed = value;
                if (value) Close();
            }
        }

        /// <summary>The item whose popup is open, or null.</summary>
        public ItemHoverTarget Current => _current;

        /// <summary>The pointer entered an item: start the hover delay, or swap at once when a popup is already open.</summary>
        public void PointerEntered(ItemHoverTarget target)
        {
            if (_suppressed) return;
            _closeAt = -1f;
            if (_current == target) return;                     // came back during the grace: nothing to redo
            if (_current != null || HoverTiming.IsWarm(Time.unscaledTime, _closedAt, warmWindow)) ShowNow(target);
            else
            {
                _pending = target;
                _pendingSince = Time.unscaledTime;
            }
        }

        /// <summary>The pointer left an item: cancel a pending open, or start the grace before closing.</summary>
        public void PointerExited(ItemHoverTarget target)
        {
            if (_pending == target) _pending = null;
            if (_current == target) _closeAt = Time.unscaledTime + grace;
        }

        /// <summary>Opens the popup for an item now (keyboard focus, or a swap while a popup is open).</summary>
        public void ShowNow(ItemHoverTarget target)
        {
            ItemRecord record = Catalog.Find(target.ItemId);
            if (record == null || _suppressed) return;

            IReadOnlyList<SynergyModifier> received = System.Array.Empty<SynergyModifier>();
            bool placed = false;
            if (target.Board != null)
            {
                placed = target.Board.Describe(target, out List<SynergyModifier> mods);
                received = mods;
            }

            PopupContent content = ItemTooltipText.Build(record, placed, received);
            ItemTooltipText.AddRecipeLines(content, record, Catalog);
            Vector2 size = view.Build(content);

            var occupied = new List<Rect>();
            foreach (ItemHoverTarget other in FindObjectsByType<ItemHoverTarget>())
            {
                if (other != target) occupied.Add(ToFrameRect(other.Rect));
            }

            Rect chosen = ItemPopupPlacement.Choose(ToFrameRect(bounds), ToFrameRect(target.Rect), occupied, size);
            view.Place(chosen.position);
            _current = target;
            _pending = null;
            _closeAt = -1f;
        }

        /// <summary>Closes the popup.</summary>
        public void Close()
        {
            if (_current != null) _closedAt = Time.unscaledTime;
            view.Hide();
            _current = null;
            _pending = null;
            _closeAt = -1f;
        }

        private void Update()
        {
            if (_pending != null && Time.unscaledTime - _pendingSince >= HoverDelay) ShowNow(_pending);
            if (_current != null && _closeAt >= 0f && Time.unscaledTime >= _closeAt) Close();
        }

        /// <summary>A rectangle in safe-frame native pixels, x right and y down from the top-left corner.</summary>
        private Rect ToFrameRect(RectTransform rt) => FrameMath.RectOf(rt, safeFrame);
    }
}
