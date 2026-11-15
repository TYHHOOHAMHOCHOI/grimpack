using System.Collections.Generic;
using Project05.Items;
using UnityEngine;

namespace Project05.UI.Hud
{
    /// <summary>
    /// Opens, swaps and closes the one item popup (hover-popup-and-glossary.md rules 2 to 5): a 300 ms hover opens it,
    /// selection opens it at once, a 60 ms grace keeps it while the pointer crosses a gap, and it is placed once, inside
    /// the workbench, never touching its source. The popup only reads definitions; it changes no game state.
    /// </summary>
    public sealed class ItemPopupController : MonoBehaviour
    {
        /// <summary>Delay before a hover opens the popup, in seconds.</summary>
        public const float HoverDelay = 0.3f;

        /// <summary>Time the popup is kept after the pointer leaves, in seconds.</summary>
        public const float Grace = 0.06f;

        [SerializeField] private TextAsset catalogJson;
        [SerializeField] private ItemPopupView view;
        [SerializeField] private RectTransform safeFrame;
        [SerializeField] private RectTransform bounds;

        private ItemCatalog _catalog;
        private ItemHoverTarget _pending;
        private ItemHoverTarget _current;
        private float _pendingSince;
        private float _closeAt = -1f;

        /// <summary>The parsed catalog.</summary>
        public ItemCatalog Catalog => _catalog ??= ItemCatalog.Load(catalogJson.text);

        /// <summary>The item whose popup is open, or null.</summary>
        public ItemHoverTarget Current => _current;

        /// <summary>The pointer entered an item: start the hover delay, or swap at once when a popup is already open.</summary>
        public void PointerEntered(ItemHoverTarget target)
        {
            _closeAt = -1f;
            if (_current != null) ShowNow(target);
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
            if (_current == target) _closeAt = Time.unscaledTime + Grace;
        }

        /// <summary>Opens the popup for an item now (keyboard focus, or a swap while a popup is open).</summary>
        public void ShowNow(ItemHoverTarget target)
        {
            ItemRecord record = Catalog.Find(target.ItemId);
            if (record == null) return;

            IReadOnlyList<SynergyModifier> received = System.Array.Empty<SynergyModifier>();
            if (target.Grid != null && target.Grid.Evaluate(Catalog).TryGetValue(target, out List<SynergyModifier> mods)) received = mods;

            PopupContent content = ItemTooltipText.Build(record, target.Grid != null, received);
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
        private Rect ToFrameRect(RectTransform rt)
        {
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            Vector2 min = safeFrame.InverseTransformPoint(corners[0]);
            Vector2 max = safeFrame.InverseTransformPoint(corners[2]);
            float halfW = safeFrame.rect.width / 2f;
            float halfH = safeFrame.rect.height / 2f;
            return new Rect(min.x + halfW, halfH - max.y, max.x - min.x, max.y - min.y);
        }
    }
}
