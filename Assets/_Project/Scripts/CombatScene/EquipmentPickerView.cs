using System;
using System.Collections.Generic;
using Project05.CombatCore;
using Project05.UI.Hud;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project05.CombatScene
{
    /// <summary>
    /// Interactive catalog drawer ("Chọn đồ") inside the lower Workbench panel.
    /// Shows all items from <see cref="GameCatalog"/> as 20x20 compact plates with hover popups;
    /// clicking an item spawns and equips/stashes it onto the hero.
    /// </summary>
    public sealed class EquipmentPickerView : MonoBehaviour
    {
        private const int Cols = 7;
        private const int RowsPerPage = 5;
        private const int PageCapacity = Cols * RowsPerPage; // 35 items/page
        private const float CellPitch = 22f;

        private RectTransform _root;
        private RectTransform _gridRoot;
        private CombatSceneSpriteCatalog _sprites;
        private ItemPopupController _popup;
        private readonly List<ItemDef> _sortedItems = new List<ItemDef>();
        private readonly List<GameObject> _spawnedButtons = new List<GameObject>();

        private Button _prevPageBtn;
        private Button _nextPageBtn;
        private TextMeshProUGUI _pageLabel;
        private int _pageIndex;

        public event Action<string> ItemClicked;

        public void Initialize(RectTransform container, CombatSceneSpriteCatalog sprites, ItemPopupController popup)
        {
            _root = container;
            _sprites = sprites;
            _popup = popup;

            _gridRoot = CombatUiFactory.CreateRect("PickerGrid", _root, 4f, 0f, 154f, 112f);

            // Compact pagination bar (only visible if catalog exceeds 35 items)
            _prevPageBtn = CombatUiFactory.CreateSegmentButton("PrevPage", _root, _sprites, "<", 4f, 114f, 24f, 16f);
            _pageLabel = CombatUiFactory.CreateLabel("PageLabel", _root, _sprites.font, "1/1", 32f, 116f, 44f, 12f,
                CombatUiFactory.Fog, TextAlignmentOptions.Top);
            _nextPageBtn = CombatUiFactory.CreateSegmentButton("NextPage", _root, _sprites, ">", 78f, 114f, 24f, 16f);

            _prevPageBtn.onClick.AddListener(() => SetPage(_pageIndex - 1));
            _nextPageBtn.onClick.AddListener(() => SetPage(_pageIndex + 1));

            CombatUiFactory.CreateLabel(
                "PickerHint",
                _root,
                _sprites.font,
                "Bấm món để lấy. Kéo để xếp, R để xoay.",
                0f,
                134f,
                160f,
                28f,
                CombatUiFactory.Fog,
                TextAlignmentOptions.TopLeft,
                wrap: true);
        }

        public void Populate(GameCatalog catalog)
        {
            _sortedItems.Clear();
            if (catalog != null && catalog.Items != null)
            {
                _sortedItems.AddRange(catalog.Items);
                _sortedItems.Sort((a, b) =>
                {
                    int tierCmp = a.Tier.CompareTo(b.Tier);
                    return tierCmp != 0 ? tierCmp : string.CompareOrdinal(a.Id, b.Id);
                });
            }

            _pageIndex = 0;
            RebuildPage();
        }

        private void SetPage(int page)
        {
            int totalPages = Mathf.Max(1, Mathf.CeilToInt(_sortedItems.Count / (float)PageCapacity));
            _pageIndex = Mathf.Clamp(page, 0, totalPages - 1);
            RebuildPage();
        }

        private void RebuildPage()
        {
            for (int i = 0; i < _spawnedButtons.Count; i++)
            {
                CombatUiFactory.DestroySafe(_spawnedButtons[i]);
            }
            _spawnedButtons.Clear();

            int totalPages = Mathf.Max(1, Mathf.CeilToInt(_sortedItems.Count / (float)PageCapacity));
            bool multiPage = totalPages > 1;
            _prevPageBtn.gameObject.SetActive(multiPage);
            _nextPageBtn.gameObject.SetActive(multiPage);
            _pageLabel.gameObject.SetActive(multiPage);
            _pageLabel.text = (_pageIndex + 1) + "/" + totalPages;

            int start = _pageIndex * PageCapacity;
            int end = Mathf.Min(_sortedItems.Count, start + PageCapacity);

            for (int i = start; i < end; i++)
            {
                int localIdx = i - start;
                int col = localIdx % Cols;
                int row = localIdx / Cols;
                ItemDef item = _sortedItems[i];

                string tierLower = item.Tier.ToString().ToLowerInvariant();
                Sprite plateSprite = _sprites.GetAuthoredPlate(tierLower, false, "1x1");
                Sprite iconSprite = _sprites.GetItemIcon(item.Art, item.Footprint, compact: true);

                Image plate = CombatUiFactory.CreateImage(
                    "Pick_" + item.Id,
                    _gridRoot,
                    plateSprite,
                    col * CellPitch,
                    row * CellPitch,
                    20f,
                    20f,
                    sliced: false,
                    raycastTarget: true);

                if (iconSprite != null)
                {
                    CombatUiFactory.CreateImage("Icon", plate.transform, iconSprite, 2f, 2f, 16f, 16f);
                }

                var btn = plate.gameObject.AddComponent<Button>();
                btn.targetGraphic = plate;
                btn.transition = Selectable.Transition.None;
                string itemId = item.Id;
                btn.onClick.AddListener(() => ItemClicked?.Invoke(itemId));

                if (_popup != null)
                {
                    var hover = plate.gameObject.AddComponent<ItemHoverTarget>();
                    hover.Configure(item.Id, "picker", 0, 0, 0, null, _popup);
                }

                _spawnedButtons.Add(plate.gameObject);
            }
        }
    }
}
