using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Project05.Core;
using Project05.Backpack;

namespace Project05.UI
{
    /// <summary>
    /// Renders a 2D BackpackGrid (Personal Hero Backpack or Shared Stash),
    /// displaying unlocked/locked cells, multi-cell item shapes, synergy star markers (★),
    /// turn cooldown badges, and placement preview highlights.
    /// </summary>
    public class BackpackGridView : MonoBehaviour
    {
        private BackpackGrid _boundGrid;
        private BackpackSynergyReport _synergyReport;
        private RectTransform _slotsContainer;
        private RectTransform _itemsContainer;
        private RectTransform _starsContainer;

        private Image[,] _slotImages;
        private Text[,] _slotLabels;
        private readonly List<GameObject> _spawnedItemObjects = new List<GameObject>();
        private readonly List<GridCoord> _tempCells = new List<GridCoord>(16);

        private float _cellSize = 52f;
        private float _effectiveCellSize = 52f;
        private float _cellSpacing = 4f;

        public BackpackGrid BoundGrid => _boundGrid;

        public event Action<BackpackGridView, int, int, PointerEventData.InputButton> OnCellClicked;
        public event Action<BackpackGridView, int, int> OnCellHovered;
        public event Action<BackpackGridView> OnGridPointerExited;
        public event Action<BackpackGridView, int, int, PointerEventData> OnCellBeginDrag;
        public event Action<BackpackGridView, PointerEventData> OnCellDragged;
        public event Action<BackpackGridView, int, int, PointerEventData> OnCellDropped;
        public event Action<BackpackGridView, PointerEventData> OnCellEndDrag;

        public void InitializeLayout(RectTransform container, float cellSize = 52f, float cellSpacing = 4f)
        {
            _cellSize = cellSize;
            _effectiveCellSize = cellSize;
            _cellSpacing = cellSpacing;

            _slotsContainer = UIBuilderUtility.CreateRect("SlotsLayer", container, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            _itemsContainer = UIBuilderUtility.CreateRect("ItemsLayer", container, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            _starsContainer = UIBuilderUtility.CreateRect("StarsLayer", container, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        }

        public void BindGrid(BackpackGrid grid, BackpackSynergyReport synergyReport = null)
        {
            _boundGrid = grid;
            _synergyReport = synergyReport;
            RebuildView();
        }

        public void RebuildView()
        {
            if (_slotsContainer == null || _boundGrid == null) return;

            for (int i = _slotsContainer.childCount - 1; i >= 0; i--)
            {
                UIBuilderUtility.SafeDestroy(_slotsContainer.GetChild(i).gameObject);
            }
            for (int i = 0; i < _spawnedItemObjects.Count; i++)
            {
                if (_spawnedItemObjects[i] != null) UIBuilderUtility.SafeDestroy(_spawnedItemObjects[i]);
            }
            _spawnedItemObjects.Clear();

            int w = _boundGrid.Width;
            int h = _boundGrid.Height;
            _slotImages = new Image[w, h];
            _slotLabels = new Text[w, h];

            _effectiveCellSize = ComputeResponsiveCellSize(w, h);
            float totalW = w * _effectiveCellSize + (w - 1) * _cellSpacing;
            float totalH = h * _effectiveCellSize + (h - 1) * _cellSpacing;

            _slotsContainer.sizeDelta = new Vector2(totalW, totalH);
            _itemsContainer.sizeDelta = new Vector2(totalW, totalH);
            if (_starsContainer != null)
            {
                _starsContainer.sizeDelta = new Vector2(totalW, totalH);
            }

            // 1. Build Slot Cells
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int cx = x;
                    int cy = y;
                    bool unlocked = _boundGrid.IsSlotUnlocked(cx, cy);

                    Vector2 pos = GetCellTopLeftLocalPos(cx, cy, totalW, totalH);
                    Image slotImg = UIBuilderUtility.CreatePanel(
                        $"Slot_{cx}_{cy}",
                        _slotsContainer,
                        unlocked ? Palette.SlotUnlocked : Palette.SlotLocked,
                        new Vector2(0f, 1f),
                        new Vector2(0f, 1f),
                        new Vector2(pos.x, pos.y - _effectiveCellSize),
                        new Vector2(pos.x + _effectiveCellSize, pos.y),
                        withOutline: true,
                        outlineColor: unlocked ? Palette.BorderIron : new Color(0.18f, 0.16f, 0.22f, 0.7f),
                        raycastTarget: true);

                    Text slotTxt = UIBuilderUtility.CreateText(
                        "SlotMark",
                        slotImg.transform,
                        unlocked ? string.Empty : "🔒",
                        12,
                        Palette.AshMutedText,
                        TextAnchor.MiddleCenter,
                        FontStyle.Normal,
                        raycastTarget: false);

                    SetSlotVisual(slotImg, unlocked ? SlotVisual.Unlocked : SlotVisual.Locked);
                    AttachSlotPointerTriggers(slotImg.gameObject, cx, cy);

                    _slotImages[cx, cy] = slotImg;
                    _slotLabels[cx, cy] = slotTxt;
                }
            }

            // 2. Render Placed Items & Foreground Synergy Star Indicators
            IReadOnlyList<ItemData> items = _boundGrid.PlacedItems;
            for (int i = 0; i < items.Count; i++)
            {
                RenderPlacedItem(items[i], totalW, totalH);
            }
        }

        private float ComputeResponsiveCellSize(int cols, int rows)
        {
            RectTransform holderRt = transform as RectTransform;
            if (holderRt == null) return _cellSize;

            Rect rect = holderRt.rect;
            if (rect.width <= 30f || rect.height <= 30f) return _cellSize;

            float availW = rect.width - 12f - (cols - 1) * _cellSpacing;
            float availH = rect.height - 8f - (rows - 1) * _cellSpacing;
            float fitW = availW / Mathf.Max(1, cols);
            float fitH = availH / Mathf.Max(1, rows);
            return Mathf.Clamp(Mathf.Min(_cellSize, fitW, fitH), 28f, _cellSize);
        }

        public void HighlightPreview(ItemData previewItem, int anchorX, int anchorY, int rotationSteps)
        {
            if (_boundGrid == null || _slotImages == null) return;

            // Reset base colors
            for (int y = 0; y < _boundGrid.Height; y++)
            {
                for (int x = 0; x < _boundGrid.Width; x++)
                {
                    bool unlocked = _boundGrid.IsSlotUnlocked(x, y);
                    SetSlotVisual(_slotImages[x, y], unlocked ? SlotVisual.Unlocked : SlotVisual.Locked);
                }
            }

            if (previewItem == null) return;

            bool canPlace = _boundGrid.CanPlaceItemAt(previewItem, anchorX, anchorY, rotationSteps, previewItem);
            SlotVisual occVisual = canPlace ? SlotVisual.ValidHover : SlotVisual.InvalidHover;

            previewItem.GetOccupiedGridCells(anchorX, anchorY, rotationSteps, _tempCells);
            for (int i = 0; i < _tempCells.Count; i++)
            {
                GridCoord c = _tempCells[i];
                if (_boundGrid.IsInBounds(c.X, c.Y))
                {
                    SetSlotVisual(_slotImages[c.X, c.Y], occVisual);
                }
            }

            // Highlight rotated Synergy Star cells in Gold
            previewItem.GetSynergyStarGridCells(anchorX, anchorY, rotationSteps, _tempCells);
            for (int i = 0; i < _tempCells.Count; i++)
            {
                GridCoord s = _tempCells[i];
                if (_boundGrid.IsInBounds(s.X, s.Y) && _boundGrid.IsSlotUnlocked(s.X, s.Y))
                {
                    SetSlotVisual(_slotImages[s.X, s.Y], SlotVisual.SynergyGlow);
                }
            }
        }

        private enum SlotVisual { Unlocked, Locked, ValidHover, InvalidHover, SynergyGlow }

        private static readonly Color LockedSlotTint = new Color(0.42f, 0.40f, 0.48f, 0.85f);

        /// <summary>Swaps the PixelLab slot sprite per state; falls back to the flat palette colours if the art is missing.</summary>
        private static void SetSlotVisual(Image img, SlotVisual visual)
        {
            bool skinned;
            switch (visual)
            {
                case SlotVisual.Locked:
                    skinned = UISkin.Apply(img, UISkin.SlotCommon, 1f, LockedSlotTint);
                    if (!skinned) img.color = Palette.SlotLocked;
                    break;
                case SlotVisual.ValidHover:
                    skinned = UISkin.Apply(img, UISkin.SlotUncommon);
                    if (!skinned) img.color = Palette.SlotValidHover;
                    break;
                case SlotVisual.InvalidHover:
                    skinned = UISkin.Apply(img, UISkin.SlotCursed);
                    if (!skinned) img.color = Palette.SlotInvalidHover;
                    break;
                case SlotVisual.SynergyGlow:
                    skinned = UISkin.Apply(img, UISkin.SlotLegendary);
                    if (!skinned) img.color = Palette.SlotSynergyGlow;
                    break;
                default:
                    skinned = UISkin.Apply(img, UISkin.SlotCommon);
                    if (!skinned) img.color = Palette.SlotUnlocked;
                    break;
            }
        }

        private void RenderPlacedItem(ItemData item, float totalW, float totalH)
        {
            if (item == null || item.Definition == null) return;

            Color tierCol = Palette.GetTierColor(item.Definition.RarityTier);
            Color cellFill = new Color(tierCol.r * 0.36f, tierCol.g * 0.36f, tierCol.b * 0.36f, 0.92f);

            item.GetOccupiedGridCells(item.AnchorX, item.AnchorY, item.RotationSteps, _tempCells);

            int minX = int.MaxValue, minY = int.MaxValue;
            int maxX = int.MinValue, maxY = int.MinValue;
            bool skinnedCells = false;

            // Render each occupied cell of the item shape
            for (int i = 0; i < _tempCells.Count; i++)
            {
                GridCoord c = _tempCells[i];
                if (!_boundGrid.IsInBounds(c.X, c.Y)) continue;

                if (c.X < minX) minX = c.X;
                if (c.Y < minY) minY = c.Y;
                if (c.X > maxX) maxX = c.X;
                if (c.Y > maxY) maxY = c.Y;

                Vector2 pos = GetCellTopLeftLocalPos(c.X, c.Y, totalW, totalH);
                Image cellBlock = UIBuilderUtility.CreatePanel(
                    $"ItemCell_{item.InstanceId}_{i}",
                    _itemsContainer,
                    cellFill,
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    new Vector2(pos.x + 2f, pos.y - _effectiveCellSize + 2f),
                    new Vector2(pos.x + _effectiveCellSize - 2f, pos.y - 2f),
                    withOutline: true,
                    outlineColor: tierCol,
                    raycastTarget: false);
                if (UISkin.Apply(cellBlock, UISkin.SlotForTier(item.Definition.RarityTier)))
                {
                    skinnedCells = true;
                }

                _spawnedItemObjects.Add(cellBlock.gameObject);
            }

            // Render bridge connectors between orthogonally adjacent cells of the same multi-cell item
            for (int i = 0; i < _tempCells.Count; i++)
            {
                for (int j = i + 1; j < _tempCells.Count; j++)
                {
                    GridCoord a = _tempCells[i];
                    GridCoord b = _tempCells[j];
                    if (!a.IsOrthogonallyAdjacent(b)) continue;

                    Vector2 posA = GetCellTopLeftLocalPos(a.X, a.Y, totalW, totalH);
                    Vector2 posB = GetCellTopLeftLocalPos(b.X, b.Y, totalW, totalH);
                    float left = Mathf.Min(posA.x, posB.x) + 4f;
                    float right = Mathf.Max(posA.x, posB.x) + _effectiveCellSize - 4f;
                    float top = Mathf.Max(posA.y, posB.y) - 4f;
                    float bottom = Mathf.Min(posA.y, posB.y) - _effectiveCellSize + 4f;
                    Color bridgeColor = cellFill;

                    if (skinnedCells)
                    {
                        // Only cover the seam between the two framed cells so their slot art stays visible.
                        const float rim = 12f;
                        if (a.Y == b.Y)
                        {
                            left = Mathf.Min(posA.x, posB.x) + _effectiveCellSize - rim;
                            right = Mathf.Max(posA.x, posB.x) + rim;
                            top = posA.y - rim;
                            bottom = posA.y - _effectiveCellSize + rim;
                        }
                        else
                        {
                            left = posA.x + rim;
                            right = posA.x + _effectiveCellSize - rim;
                            // Upper cell's bottom rim down to the lower cell's top rim.
                            top = Mathf.Max(posA.y, posB.y) - _effectiveCellSize + rim;
                            bottom = Mathf.Min(posA.y, posB.y) - rim;
                        }
                        bridgeColor = item.Definition.RarityTier == CardTier.Red ? UISkin.SlotCursedInterior : UISkin.SlotInterior;
                    }

                    Image bridge = UIBuilderUtility.CreatePanel(
                        $"ItemBridge_{item.InstanceId}_{i}_{j}",
                        _itemsContainer,
                        bridgeColor,
                        new Vector2(0f, 1f),
                        new Vector2(0f, 1f),
                        new Vector2(left, bottom),
                        new Vector2(right, top),
                        withOutline: false,
                        raycastTarget: false);
                    _spawnedItemObjects.Add(bridge.gameObject);
                }
            }

            // Render Icon, Name, Cooldown, and Synergy count across the item's bounding box (if rectangular) or primary cell
            if (_tempCells.Count > 0)
            {
                int spanCols = maxX - minX + 1;
                int spanRows = maxY - minY + 1;
                bool isFullRect = _tempCells.Count == (spanCols * spanRows);

                Vector2 badgeOffsetMin;
                Vector2 badgeOffsetMax;
                if (isFullRect)
                {
                    Vector2 topLeft = GetCellTopLeftLocalPos(minX, minY, totalW, totalH);
                    Vector2 bottomRight = GetCellTopLeftLocalPos(maxX, maxY, totalW, totalH);
                    badgeOffsetMin = new Vector2(topLeft.x + 3f, bottomRight.y - _effectiveCellSize + 3f);
                    badgeOffsetMax = new Vector2(bottomRight.x + _effectiveCellSize - 3f, topLeft.y - 3f);
                }
                else
                {
                    GridCoord mainCell = _tempCells[0];
                    Vector2 pos = GetCellTopLeftLocalPos(mainCell.X, mainCell.Y, totalW, totalH);
                    badgeOffsetMin = new Vector2(pos.x + 3f, pos.y - _effectiveCellSize + 3f);
                    badgeOffsetMax = new Vector2(pos.x + _effectiveCellSize - 3f, pos.y - 3f);
                }

                RectTransform badgeRoot = UIBuilderUtility.CreateRect(
                    $"ItemBadge_{item.InstanceId}",
                    _itemsContainer,
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    badgeOffsetMin,
                    badgeOffsetMax);
                _spawnedItemObjects.Add(badgeRoot.gameObject);

                // Pixel Art Sprite Icon
                RectTransform iconRt = UIBuilderUtility.CreateRect(
                    "Icon",
                    badgeRoot,
                    new Vector2(0.15f, 0.24f),
                    new Vector2(0.85f, 0.86f),
                    Vector2.zero,
                    Vector2.zero);
                Image iconImg = iconRt.gameObject.AddComponent<Image>();
                iconImg.sprite = SpriteFactory.GetSprite(item.Definition.SpriteName, tierCol);
                iconImg.preserveAspect = true;
                iconImg.raycastTarget = false;

                // Cooldown tag at top-right
                int effCd = item.Definition.CooldownTurns;
                int synCount = 0;
                if (_synergyReport != null && _synergyReport.ItemStats.TryGetValue(item, out EvaluatedItemStats evalStats))
                {
                    effCd = evalStats.EffectiveCooldownTurns;
                    synCount = evalStats.SynergyLinksReceived;
                }

                string cdStr = effCd <= 0
                    ? "Bị động"
                    : (item.CurrentCooldown <= 0 ? $"CD:{effCd}" : $"⏳{item.CurrentCooldown}");

                UIBuilderUtility.CreateText(
                    "CdText",
                    badgeRoot,
                    cdStr,
                    10,
                    item.CurrentCooldown <= 0 ? Palette.TorchGold : Palette.BoneText,
                    TextAnchor.UpperRight,
                    FontStyle.Bold,
                    raycastTarget: false);

                // Short item label at bottom
                int maxChars = isFullRect && spanCols >= 2 ? 16 : 9;
                string shortName = item.Definition.DisplayName.Length > maxChars
                    ? item.Definition.DisplayName.Substring(0, maxChars - 1) + ".."
                    : item.Definition.DisplayName;
                if (synCount > 0)
                {
                    shortName = $"★+{synCount} " + shortName;
                }

                UIBuilderUtility.CreateText(
                    "NameText",
                    badgeRoot,
                    shortName,
                    10,
                    Palette.BoneText,
                    TextAnchor.LowerCenter,
                    FontStyle.Bold,
                    raycastTarget: false);
            }

            // Render Star markers on both the background slot and the foreground _starsContainer so they remain visible over target items
            Transform starLayer = _starsContainer != null ? _starsContainer : _itemsContainer;
            item.GetSynergyStarGridCells(item.AnchorX, item.AnchorY, item.RotationSteps, _tempCells);
            for (int i = 0; i < _tempCells.Count; i++)
            {
                GridCoord s = _tempCells[i];
                if (_boundGrid.IsInBounds(s.X, s.Y) && _boundGrid.IsSlotUnlocked(s.X, s.Y))
                {
                    _slotLabels[s.X, s.Y].text = "★";
                    _slotLabels[s.X, s.Y].color = Palette.TorchGold;

                    Vector2 sPos = GetCellTopLeftLocalPos(s.X, s.Y, totalW, totalH);
                    RectTransform starOverlayRt = UIBuilderUtility.CreateRect(
                        $"StarOverlay_{item.InstanceId}_{i}",
                        starLayer,
                        new Vector2(0f, 1f),
                        new Vector2(0f, 1f),
                        new Vector2(sPos.x + 2f, sPos.y - 18f),
                        new Vector2(sPos.x + 18f, sPos.y - 2f));
                    Text starTxt = UIBuilderUtility.CreateText(
                        "StarGlyph",
                        starOverlayRt,
                        "★",
                        11,
                        Palette.TorchGold,
                        TextAnchor.UpperLeft,
                        FontStyle.Bold,
                        raycastTarget: false);
                    _spawnedItemObjects.Add(starOverlayRt.gameObject);
                }
            }
        }

        private Vector2 GetCellTopLeftLocalPos(int x, int y, float totalW, float totalH)
        {
            float px = x * (_effectiveCellSize + _cellSpacing);
            float py = -y * (_effectiveCellSize + _cellSpacing);
            return new Vector2(px, py);
        }

        private void AttachSlotPointerTriggers(GameObject slotGo, int x, int y)
        {
            EventTrigger trigger = slotGo.AddComponent<EventTrigger>();

            EventTrigger.Entry clickEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
            clickEntry.callback.AddListener(data =>
            {
                PointerEventData ped = (PointerEventData)data;
                OnCellClicked?.Invoke(this, x, y, ped.button);
            });
            trigger.triggers.Add(clickEntry);

            EventTrigger.Entry enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enterEntry.callback.AddListener(_ => OnCellHovered?.Invoke(this, x, y));
            trigger.triggers.Add(enterEntry);

            EventTrigger.Entry exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exitEntry.callback.AddListener(_ => OnGridPointerExited?.Invoke(this));
            trigger.triggers.Add(exitEntry);

            EventTrigger.Entry beginDragEntry = new EventTrigger.Entry { eventID = EventTriggerType.BeginDrag };
            beginDragEntry.callback.AddListener(data => OnCellBeginDrag?.Invoke(this, x, y, (PointerEventData)data));
            trigger.triggers.Add(beginDragEntry);

            EventTrigger.Entry dragEntry = new EventTrigger.Entry { eventID = EventTriggerType.Drag };
            dragEntry.callback.AddListener(data => OnCellDragged?.Invoke(this, (PointerEventData)data));
            trigger.triggers.Add(dragEntry);

            EventTrigger.Entry dropEntry = new EventTrigger.Entry { eventID = EventTriggerType.Drop };
            dropEntry.callback.AddListener(data => OnCellDropped?.Invoke(this, x, y, (PointerEventData)data));
            trigger.triggers.Add(dropEntry);

            EventTrigger.Entry endDragEntry = new EventTrigger.Entry { eventID = EventTriggerType.EndDrag };
            endDragEntry.callback.AddListener(data => OnCellEndDrag?.Invoke(this, (PointerEventData)data));
            trigger.triggers.Add(endDragEntry);
        }
    }
}
