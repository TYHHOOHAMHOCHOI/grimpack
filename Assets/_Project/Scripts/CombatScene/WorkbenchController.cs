using System;
using System.Collections.Generic;
using Project05.CombatCore;
using Project05.UI.Hud;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Project05.CombatScene
{
    /// <summary>
    /// Handles pointer events on placed item plates inside the Workbench.
    /// </summary>
    public sealed class WorkbenchItemHandle : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        public long InstanceId { get; private set; }
        public ContainerKind Container { get; private set; }
        public int Orientation { get; private set; }

        private WorkbenchController _owner;

        public void Configure(WorkbenchController owner, long instanceId, ContainerKind container, int orientation)
        {
            _owner = owner;
            InstanceId = instanceId;
            Container = container;
            Orientation = orientation;
        }

        public void OnBeginDrag(PointerEventData eventData) => _owner?.BeginItemDrag(this, eventData);
        public void OnDrag(PointerEventData eventData) => _owner?.UpdateItemDrag(eventData);
        public void OnEndDrag(PointerEventData eventData) => _owner?.EndItemDrag(eventData);
        public void OnPointerClick(PointerEventData eventData) => _owner?.HandleItemClick(this, eventData);
    }

    /// <summary>
    /// Interactive Workbench controller for Hero0 (4x4), Stash (8x2), Equipment Picker ("Chọn đồ"),
    /// drag-and-drop, R-key / right-click rotation, item removal, resting/preview synergy bridges,
    /// live hero stats, and hover popups.
    /// </summary>
    public sealed class WorkbenchController : MonoBehaviour
    {
        private const int HeroCols = 4;
        private const int HeroRows = 4;
        private const float HeroOriginX = 7f;
        private const float HeroOriginY = 40f;

        private const int StashCols = 8;
        private const int StashRows = 2;
        private const float StashOriginX = 7f;
        private const float StashOriginY = 152f;

        private const float CellPx = 20f;
        private const float TrashX = 7f;
        private const float TrashY = 326f;
        private const float TrashW = 160f;
        private const float TrashH = 24f;

        private RectTransform _safeFrame;
        private RectTransform _workbenchRoot;
        private Camera _uiCamera;
        private CombatSceneSpriteCatalog _sprites;
        private ItemPopupController _popup;
        private ItemGridContext _heroGridContext;
        private GameCatalog _catalog;
        private HeroDef _heroDef;
        private BackpackArrangement _arrangement;

        private ProceduralPlateBuilder _plateBuilder;

        // UI layers
        private RectTransform _heroItemsLayer;
        private RectTransform _stashItemsLayer;
        private RectTransform _bridgesLayer;
        private Image[,] _heroHighlights;
        private Image[,] _stashHighlights;

        private Button _stashTabBtn;
        private Button _pickerTabBtn;
        private Button _undoBtn;
        private RectTransform _stashSectionRoot;
        private RectTransform _pickerSectionRoot;
        private EquipmentPickerView _pickerView;
        private Image _trashBg;
        private TextMeshProUGUI _trashLabel;

        private TextMeshProUGUI _nameLabel;
        private SpriteNumeralText _nameJoin;
        private TextMeshProUGUI _statsLabel;
        private TextMeshProUGUI _feedbackLabel;
        private TextMeshProUGUI _innateLabel;

        // Spawned item tracking
        private readonly List<GameObject> _heroPlateObjects = new List<GameObject>();
        private readonly List<GameObject> _stashPlateObjects = new List<GameObject>();
        private readonly List<GameObject> _bridgeObjects = new List<GameObject>();
        private readonly Dictionary<long, Image> _plateImagesById = new Dictionary<long, Image>();

        // Drag state
        private bool _interactive = true;
        private bool _showingPicker;
        private bool _isDragging;
        private long _dragInstanceId;
        private ItemDef _dragDef;
        private int _dragOrientation;
        private Vector2 _lastPointerScreenPos;
        private RectTransform _dragGhostRoot;
        private Image _dragGhostPlate;
        private Image _dragGhostIcon;

        public bool IsDragging => _isDragging;
        public bool IsPickerOpen => _showingPicker;
        public GridSynergyView CurrentSynergy { get; private set; }

        public event Action ArrangementChanged;

        public void Initialize(
            RectTransform safeFrame,
            RectTransform workbenchRoot,
            Camera uiCamera,
            CombatSceneSpriteCatalog sprites,
            ItemPopupController popup,
            GameCatalog catalog,
            HeroDef heroDef,
            BackpackArrangement arrangement)
        {
            _safeFrame = safeFrame;
            _workbenchRoot = workbenchRoot;
            _uiCamera = uiCamera;
            _sprites = sprites;
            _popup = popup;
            _catalog = catalog;
            _heroDef = heroDef;
            _arrangement = arrangement;
            _plateBuilder = new ProceduralPlateBuilder();

            BuildWorkbenchUi();
            SwitchLowerTab(showPicker: false);
            RefreshAll();
        }

        private void OnDestroy()
        {
            _plateBuilder?.Dispose();
            _plateBuilder = null;
            if (_dragGhostRoot != null) CombatUiFactory.DestroySafe(_dragGhostRoot.gameObject);
        }

        private void Update()
        {
            if (!_isDragging || !_interactive) return;

            bool rotatePressed =
                (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) ||
                (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame);

            if (rotatePressed)
            {
                _dragOrientation = (_dragOrientation + 1) % 4;
                RebuildDragGhostVisual();
                UpdateDragPreview(_lastPointerScreenPos);
            }
            else if (Keyboard.current != null &&
                     (Keyboard.current.deleteKey.wasPressedThisFrame || Keyboard.current.backspaceKey.wasPressedThisFrame))
            {
                long id = _dragInstanceId;
                CancelDrag();
                TryRemoveItem(id);
            }
            else if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                CancelDrag();
            }
        }

        public void SetInteractive(bool interactive)
        {
            _interactive = interactive;
            if (!interactive && _isDragging) CancelDrag();
            if (_undoBtn != null) _undoBtn.interactable = interactive && _arrangement != null && _arrangement.UndoDepth > 0;
        }

        public void ForceClearDragAndTray()
        {
            if (_isDragging) CancelDrag();
            _popup?.Close();
        }

        public void ShowFeedback(string text, bool isError = false)
        {
            if (_feedbackLabel == null) return;
            _feedbackLabel.color = isError ? CombatUiFactory.Blood : CombatUiFactory.Fog;
            _feedbackLabel.text = text ?? string.Empty;
        }

        public void SwitchLowerTab(bool showPicker)
        {
            _showingPicker = showPicker;
            if (_stashSectionRoot != null) _stashSectionRoot.gameObject.SetActive(!showPicker);
            if (_pickerSectionRoot != null) _pickerSectionRoot.gameObject.SetActive(showPicker);
            CombatUiFactory.SetSegmentVisual(_stashTabBtn, _sprites, !showPicker);
            CombatUiFactory.SetSegmentVisual(_pickerTabBtn, _sprites, showPicker);
            _popup?.Close();
        }

        public void RefreshAll()
        {
            if (_arrangement == null || _catalog == null) return;

            ArrangementView view = _arrangement.CaptureView();
            ArrangementGridView heroGrid = view.HeroGrids.Count > 0 ? view.HeroGrids[0] : null;
            CurrentSynergy = heroGrid != null
                ? AdjacencySynergyEvaluator.Evaluate(heroGrid, _catalog)
                : new GridSynergyView(Array.Empty<ItemSynergySummary>(), Array.Empty<SynergyLink>());

            RebuildGridPlates(heroGrid, _heroItemsLayer, HeroOriginX, HeroOriginY, _heroPlateObjects, isHeroGrid: true);
            RebuildGridPlates(view.Stash, _stashItemsLayer, StashOriginX, StashOriginY, _stashPlateObjects, isHeroGrid: false);
            RebuildSynergyBridges(CurrentSynergy, highlightLinks: null);
            RefreshHeroStatsPanel(heroGrid, CurrentSynergy);

            if (_undoBtn != null)
            {
                _undoBtn.interactable = _interactive && _arrangement.UndoDepth > 0;
            }
        }

        public bool TryPickCatalogItem(string itemId)
        {
            if (!_interactive || _arrangement == null || _arrangement.IsFrozen) return false;
            if (_arrangement.CreateInTray(itemId, out long instanceId).Code != GridOpCode.Ok)
            {
                SetFeedback("Không thể tạo trang bị.");
                return false;
            }

            GridOpResult res = _arrangement.AutoPlace(instanceId, ContainerKind.Hero0);
            if (res.IsOk)
            {
                ItemDef def = _catalog.FindItem(itemId);
                SetFeedback("Đã trang bị: " + (def != null ? def.NameVi : itemId));
                RefreshAll();
                ArrangementChanged?.Invoke();
                return true;
            }

            GridOpResult stashRes = _arrangement.AutoPlace(instanceId, ContainerKind.Stash);
            if (stashRes.IsOk)
            {
                ItemDef def = _catalog.FindItem(itemId);
                SetFeedback("Balo đầy, đã cất kho: " + (def != null ? def.NameVi : itemId));
                RefreshAll();
                ArrangementChanged?.Invoke();
                return true;
            }

            _arrangement.DestroyTrayItem(instanceId);
            SetFeedback("Balo và Kho đồ đều đã đầy!");
            return false;
        }

        public bool TryMoveOrRotateItem(long instanceId, ContainerKind target, int ox, int oy, int orientation)
        {
            if (!_interactive || _arrangement == null || _arrangement.IsFrozen) return false;
            GridOpResult res = _arrangement.Place(instanceId, target, ox, oy, orientation);
            if (!res.IsOk)
            {
                SetFeedback(TranslateOpError(res.Code));
                return false;
            }

            RefreshAll();
            ArrangementChanged?.Invoke();
            return true;
        }

        public bool TryRemoveItem(long instanceId)
        {
            if (!_interactive || _arrangement == null || _arrangement.IsFrozen) return false;
            GridOpResult res = _arrangement.DestroyTrayItem(instanceId);
            if (!res.IsOk) return false;

            _popup?.Close();
            SetFeedback("Đã gỡ bỏ vật phẩm.");
            RefreshAll();
            ArrangementChanged?.Invoke();
            return true;
        }

        // ------------------------------------------------------------------ UI Construction
        private void BuildWorkbenchUi()
        {
            // Clear any static EditMode demo items placed by CombatSceneBuilder so Play Mode owns live plates.
            for (int i = _workbenchRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = _workbenchRoot.GetChild(i);
                if (child.name.StartsWith("Item_", StringComparison.Ordinal))
                {
                    CombatUiFactory.DestroySafe(child.gameObject);
                }
                else if (child.name == "HeroGrid")
                {
                    _heroGridContext = child.GetComponent<ItemGridContext>();
                }
                else if (child.name == "StashLabel")
                {
                    CombatUiFactory.DestroySafe(child.gameObject);
                }
                else if (child.name == "StashBed" || child.name.StartsWith("StashV", StringComparison.Ordinal) || child.name == "StashH")
                {
                    // Move stash bed lines into our toggleable StashSectionRoot below.
                }
            }

            if (_heroGridContext == null)
            {
                var ctxGo = new GameObject("HeroGrid", typeof(RectTransform));
                ctxGo.transform.SetParent(_workbenchRoot, false);
                _heroGridContext = ctxGo.AddComponent<ItemGridContext>();
            }

            _nameLabel = _workbenchRoot.Find("NameLabel")?.GetComponent<TextMeshProUGUI>();
            _nameJoin = _workbenchRoot.Find("NameJoin")?.GetComponent<SpriteNumeralText>();
            if (_nameJoin != null) _nameJoin.Text = "1";
            if (_nameLabel != null && _heroDef != null) _nameLabel.text = _heroDef.NameVi;

            // Hero stats summary right of 4x4 grid (x=92..166, y=40..120)
            _statsLabel = CombatUiFactory.CreateLabel(
                "HeroStatsSummary",
                _workbenchRoot,
                _sprites.font,
                string.Empty,
                92f,
                40f,
                75f,
                80f,
                CombatUiFactory.Bone,
                TextAlignmentOptions.TopLeft,
                wrap: true);

            // Hero grid highlights + plates + synergy bridges
            _heroHighlights = CreateHighlightGrid("HeroHighlights", _workbenchRoot, HeroOriginX, HeroOriginY, HeroCols, HeroRows);
            _heroItemsLayer = CombatUiFactory.CreateRect("HeroItemsLayer", _workbenchRoot, 0f, 0f, 174f, 360f);
            _bridgesLayer = CombatUiFactory.CreateRect("BridgesLayer", _workbenchRoot, 0f, 0f, 174f, 360f);

            // Lower section tabs (y=131)
            _stashTabBtn = CombatUiFactory.CreateSegmentButton("TabStash", _workbenchRoot, _sprites, "Kho đồ", 7f, 131f, 54f, 16f, true);
            _pickerTabBtn = CombatUiFactory.CreateSegmentButton("TabPicker", _workbenchRoot, _sprites, "Chọn đồ", 63f, 131f, 58f, 16f, false);
            _undoBtn = CombatUiFactory.CreateSegmentButton("UndoBtn", _workbenchRoot, _sprites, "Hoàn tác", 123f, 131f, 44f, 16f, false);

            _stashTabBtn.onClick.AddListener(() => SwitchLowerTab(false));
            _pickerTabBtn.onClick.AddListener(() => SwitchLowerTab(true));
            _undoBtn.onClick.AddListener(() =>
            {
                if (_interactive && _arrangement.Undo().IsOk)
                {
                    SetFeedback("Đã hoàn tác thao tác trước.");
                    RefreshAll();
                    ArrangementChanged?.Invoke();
                }
            });

            // Stash section container
            _stashSectionRoot = CombatUiFactory.CreateRect("StashSection", _workbenchRoot, 0f, 0f, 174f, 360f);
            ReattachStashLinesTo(_stashSectionRoot);
            _stashHighlights = CreateHighlightGrid("StashHighlights", _stashSectionRoot, StashOriginX, StashOriginY, StashCols, StashRows);
            _stashItemsLayer = CombatUiFactory.CreateRect("StashItemsLayer", _stashSectionRoot, 0f, 0f, 174f, 360f);

            _feedbackLabel = CombatUiFactory.CreateLabel(
                "WorkbenchFeedback",
                _stashSectionRoot,
                _sprites.font,
                "Kéo đồ xếp balo. Nhấn R để xoay.",
                7f,
                198f,
                160f,
                28f,
                CombatUiFactory.Candle,
                TextAlignmentOptions.TopLeft,
                wrap: true);

            string innateTxt = _heroDef != null && _heroDef.InnateItem != null
                ? "Nội tại: " + _heroDef.InnateItem.NameVi + "\n" + _heroDef.InnateItem.DescVi
                : string.Empty;
            _innateLabel = CombatUiFactory.CreateLabel(
                "InnateInfo",
                _stashSectionRoot,
                _sprites.font,
                innateTxt,
                7f,
                232f,
                160f,
                84f,
                CombatUiFactory.Fog,
                TextAlignmentOptions.TopLeft,
                wrap: true);

            _trashBg = CombatUiFactory.CreateImage(
                "TrashZone",
                _stashSectionRoot,
                _sprites.segmentDefault,
                TrashX,
                TrashY,
                TrashW,
                TrashH,
                sliced: true,
                raycastTarget: true);
            _trashLabel = CombatUiFactory.CreateLabel(
                "TrashLabel",
                _trashBg.transform,
                _sprites.font,
                "Thả vào đây để bỏ · Chuột phải: Cất",
                4f,
                6f,
                TrashW - 8f,
                12f,
                CombatUiFactory.Fog,
                TextAlignmentOptions.Top);

            // Equipment picker section
            _pickerSectionRoot = CombatUiFactory.CreateRect("PickerSection", _workbenchRoot, 7f, 152f, 160f, 198f);
            _pickerView = _pickerSectionRoot.gameObject.AddComponent<EquipmentPickerView>();
            _pickerView.Initialize(_pickerSectionRoot, _sprites, _popup);
            _pickerView.ItemClicked += id => TryPickCatalogItem(id);
            _pickerView.Populate(_catalog);
        }

        private void ReattachStashLinesTo(RectTransform target)
        {
            var toMove = new List<Transform>();
            for (int i = 0; i < _workbenchRoot.childCount; i++)
            {
                Transform c = _workbenchRoot.GetChild(i);
                if (c.name == "StashBed" || c.name.StartsWith("StashV", StringComparison.Ordinal) || c.name == "StashH")
                {
                    toMove.Add(c);
                }
            }
            for (int i = 0; i < toMove.Count; i++)
            {
                toMove[i].SetParent(target, false);
            }
        }

        private Image[,] CreateHighlightGrid(string name, Transform parent, float originX, float originY, int cols, int rows)
        {
            RectTransform root = CombatUiFactory.CreateRect(name, parent, originX, originY, cols * CellPx, rows * CellPx);
            var grid = new Image[cols, rows];
            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols; x++)
                {
                    Image cell = CombatUiFactory.CreateImage(
                        "HL_" + x + "_" + y,
                        root,
                        _sprites.fillBone,
                        x * CellPx + 1f,
                        y * CellPx + 1f,
                        CellPx - 2f,
                        CellPx - 2f);
                    cell.enabled = false;
                    grid[x, y] = cell;
                }
            }
            return grid;
        }

        private void ClearHighlights()
        {
            ClearHighlightArray(_heroHighlights);
            ClearHighlightArray(_stashHighlights);
            if (_trashBg != null) _trashBg.color = Color.white;
            if (_trashLabel != null) _trashLabel.color = CombatUiFactory.Fog;
        }

        private static void ClearHighlightArray(Image[,] arr)
        {
            if (arr == null) return;
            for (int x = 0; x < arr.GetLength(0); x++)
            {
                for (int y = 0; y < arr.GetLength(1); y++)
                {
                    if (arr[x, y] != null) arr[x, y].enabled = false;
                }
            }
        }

        private void RebuildGridPlates(
            ArrangementGridView gridView,
            RectTransform layer,
            float originX,
            float originY,
            List<GameObject> spawnedList,
            bool isHeroGrid)
        {
            for (int i = 0; i < spawnedList.Count; i++)
            {
                CombatUiFactory.DestroySafe(spawnedList[i]);
            }
            spawnedList.Clear();

            var hoverTargets = isHeroGrid ? new List<ItemHoverTarget>() : null;
            if (gridView == null || gridView.Items == null)
            {
                if (isHeroGrid && _heroGridContext != null) _heroGridContext.SetItems(Array.Empty<ItemHoverTarget>());
                return;
            }

            for (int i = 0; i < gridView.Items.Count; i++)
            {
                PlacedItemView placed = gridView.Items[i];
                ItemDef def = _catalog.FindItem(placed.DefinitionId);
                if (def == null) continue;

                IReadOnlyList<GridCell> rotCells = BackpackArrangement.RotateFootprint(def.Cells, placed.Orientation);
                var cellTuples = ToTuples(rotCells, out int gridW, out int gridH);

                Sprite plateSprite = _plateBuilder.GetOrBuildPlate(
                    _sprites,
                    def.Tier.ToString(),
                    def.Footprint,
                    placed.Orientation,
                    highlight: false,
                    cellTuples);

                float px = originX + placed.OriginX * CellPx;
                float py = originY + placed.OriginY * CellPx;
                float pw = gridW * CellPx;
                float ph = gridH * CellPx;

                Image plateImg = CombatUiFactory.CreateImage(
                    "Item_" + def.Art + "_" + placed.InstanceId,
                    layer,
                    plateSprite,
                    px,
                    py,
                    pw,
                    ph,
                    sliced: false,
                    raycastTarget: true);
                plateImg.alphaHitTestMinimumThreshold = 0.5f;
                _plateImagesById[placed.InstanceId] = plateImg;

                AttachIconToPlate(plateImg.transform, def, placed.Orientation, pw, ph);

                var btn = plateImg.gameObject.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;

                var handle = plateImg.gameObject.AddComponent<WorkbenchItemHandle>();
                handle.Configure(this, placed.InstanceId, gridView.Container, placed.Orientation);

                if (_popup != null)
                {
                    var hover = plateImg.gameObject.AddComponent<ItemHoverTarget>();
                    hover.Configure(def.Id, placed.OriginX, placed.OriginY, isHeroGrid ? _heroGridContext : null, _popup);
                    hoverTargets?.Add(hover);
                }

                spawnedList.Add(plateImg.gameObject);
            }

            if (isHeroGrid && _heroGridContext != null)
            {
                _heroGridContext.SetItems(hoverTargets.ToArray());
            }
        }

        private void AttachIconToPlate(Transform plateParent, ItemDef def, int orientation, float plateW, float plateH)
        {
            int o = ((orientation % 4) + 4) % 4;
            bool useCompact = (o % 2 == 1) && (def.Footprint == "1x3" || def.Footprint == "2x3" || def.Footprint == "s" || def.Footprint == "t" || def.Footprint == "l");
            if (o != 0 && def.Footprint == "l") useCompact = true;

            Sprite iconSprite = _sprites.GetItemIcon(def.Art, def.Footprint, compact: useCompact);
            if (iconSprite == null) return;

            float iw = iconSprite.rect.width;
            float ih = iconSprite.rect.height;
            float ix = Mathf.Round((plateW - iw) * 0.5f);
            float iy = Mathf.Round((plateH - ih) * 0.5f);

            // For 1x2 rotated to 2x1 or vice versa, check if _d_ icon still fits, otherwise center _c_ 16x16.
            if (iw > plateW - 2f || ih > plateH - 2f)
            {
                iconSprite = _sprites.GetItemIcon(def.Art, def.Footprint, compact: true);
                if (iconSprite == null) return;
                iw = iconSprite.rect.width;
                ih = iconSprite.rect.height;
                ix = Mathf.Round((plateW - iw) * 0.5f);
                iy = Mathf.Round((plateH - ih) * 0.5f);
            }

            CombatUiFactory.CreateImage("Art", plateParent, iconSprite, ix, iy, iw, ih);
        }

        private void RebuildSynergyBridges(GridSynergyView view, HashSet<(long src, int rule, long dst)> highlightLinks)
        {
            for (int i = 0; i < _bridgeObjects.Count; i++)
            {
                CombatUiFactory.DestroySafe(_bridgeObjects[i]);
            }
            _bridgeObjects.Clear();

            if (view == null || view.AllLinks == null) return;

            var drawnSeams = new HashSet<(int sx, int sy, int tx, int ty)>();
            for (int i = 0; i < view.AllLinks.Count; i++)
            {
                SynergyLink link = view.AllLinks[i];
                var seamKey = (link.SeamSourceCell.X, link.SeamSourceCell.Y, link.SeamTargetCell.X, link.SeamTargetCell.Y);
                if (!drawnSeams.Add(seamKey)) continue;

                bool isPreview = highlightLinks != null && highlightLinks.Contains((link.SourceInstanceId, link.RuleIndex, link.TargetInstanceId));
                Sprite fill = isPreview ? _sprites.fillCandle : _sprites.fillFog;
                float thickness = isPreview ? 2f : 1f;
                float length = 6f;

                float bx, by, bw, bh;
                if (link.SeamSourceCell.Y == link.SeamTargetCell.Y)
                {
                    // Horizontal neighbours -> vertical seam at maxX * CellPx
                    int seamCol = Mathf.Max(link.SeamSourceCell.X, link.SeamTargetCell.X);
                    bx = HeroOriginX + seamCol * CellPx - length * 0.5f;
                    by = HeroOriginY + link.SeamSourceCell.Y * CellPx + (CellPx - thickness) * 0.5f;
                    bw = length;
                    bh = thickness;
                }
                else
                {
                    // Vertical neighbours -> horizontal seam at maxY * CellPx
                    int seamRow = Mathf.Max(link.SeamSourceCell.Y, link.SeamTargetCell.Y);
                    bx = HeroOriginX + link.SeamSourceCell.X * CellPx + (CellPx - thickness) * 0.5f;
                    by = HeroOriginY + seamRow * CellPx - length * 0.5f;
                    bw = thickness;
                    bh = length;
                }

                Image bridge = CombatUiFactory.CreateImage("Bridge_" + i, _bridgesLayer, fill, bx, by, bw, bh);
                _bridgeObjects.Add(bridge.gameObject);
            }
        }

        private void RefreshHeroStatsPanel(ArrangementGridView heroGrid, GridSynergyView synergy)
        {
            if (_statsLabel == null || _heroDef == null) return;

            int hpBonus = 0;
            int speedBonus = 0;
            int itemCount = heroGrid != null && heroGrid.Items != null ? heroGrid.Items.Count : 0;
            if (heroGrid != null && heroGrid.Items != null)
            {
                for (int i = 0; i < heroGrid.Items.Count; i++)
                {
                    ItemDef def = _catalog.FindItem(heroGrid.Items[i].DefinitionId);
                    if (def == null) continue;
                    hpBonus += def.HpBonus;
                    speedBonus += def.SpeedBonus;
                }
            }

            int maxHp = Mathf.Max(1, _heroDef.BaseMaxHp + hpBonus);
            int effSpeed = TurnOrderScheduler.EffectiveSpeed(_heroDef.BaseSpeed, speedBonus, 0);
            int linkCount = synergy != null && synergy.AllLinks != null ? synergy.AllLinks.Count : 0;

            _statsLabel.text =
                "HP: " + maxHp + "\n" +
                "Giáp: " + _heroDef.InitialShield + "\n" +
                "Tốc: " + effSpeed + "\n" +
                "Món: " + itemCount + "\n" +
                "Kề: " + linkCount + "\n" +
                "[R] Xoay";
        }

        // ------------------------------------------------------------------ Drag / Drop / Rotate
        internal void BeginItemDrag(WorkbenchItemHandle handle, PointerEventData eventData)
        {
            if (!_interactive || _arrangement == null || _arrangement.IsFrozen) return;
            if (eventData.button != PointerEventData.InputButton.Left) return;

            ArrangementView view = _arrangement.CaptureView();
            PlacedItemView item = FindPlacedItem(view, handle.InstanceId);
            if (item == null) return;

            ItemDef def = _catalog.FindItem(item.DefinitionId);
            if (def == null) return;

            _popup?.Close();
            _isDragging = true;
            _dragInstanceId = item.InstanceId;
            _dragDef = def;
            _dragOrientation = item.Orientation;
            _lastPointerScreenPos = eventData.position;

            if (_plateImagesById.TryGetValue(_dragInstanceId, out Image srcImg) && srcImg != null)
            {
                srcImg.color = new Color(1f, 1f, 1f, 0.25f);
            }

            EnsureDragGhost();
            RebuildDragGhostVisual();
            UpdateDragPreview(eventData.position);
        }

        internal void UpdateItemDrag(PointerEventData eventData)
        {
            if (!_isDragging) return;
            _lastPointerScreenPos = eventData.position;
            UpdateDragPreview(eventData.position);
        }

        internal void EndItemDrag(PointerEventData eventData)
        {
            if (!_isDragging) return;
            _lastPointerScreenPos = eventData.position;

            if (TryMapPointerToDropTarget(eventData.position, out DropTargetKind targetKind, out int ox, out int oy))
            {
                if (targetKind == DropTargetKind.Trash)
                {
                    long id = _dragInstanceId;
                    CancelDrag();
                    TryRemoveItem(id);
                    return;
                }

                ContainerKind container = targetKind == DropTargetKind.Hero0 ? ContainerKind.Hero0 : ContainerKind.Stash;
                GridOpResult res = _arrangement.Place(_dragInstanceId, container, ox, oy, _dragOrientation);
                CancelDrag();

                if (res.IsOk)
                {
                    SetFeedback("Đã đặt " + _dragDef.NameVi + ".");
                    RefreshAll();
                    ArrangementChanged?.Invoke();
                }
                else if (res.Code != GridOpCode.NoEffect)
                {
                    SetFeedback(TranslateOpError(res.Code));
                }
                return;
            }

            CancelDrag();
            SetFeedback("Đã hủy kéo.");
        }

        internal void HandleItemClick(WorkbenchItemHandle handle, PointerEventData eventData)
        {
            if (!_interactive || _isDragging || _arrangement == null || _arrangement.IsFrozen) return;

            if (eventData.button == PointerEventData.InputButton.Right)
            {
                bool shift = Keyboard.current != null &&
                             (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);
                if (shift)
                {
                    TryRemoveItem(handle.InstanceId);
                    return;
                }

                ContainerKind dest = handle.Container == ContainerKind.Hero0 ? ContainerKind.Stash : ContainerKind.Hero0;
                GridOpResult res = _arrangement.AutoPlace(handle.InstanceId, dest);
                if (res.IsOk)
                {
                    SetFeedback(dest == ContainerKind.Stash ? "Đã cất vào Kho đồ." : "Đã đưa lên Balo.");
                    _popup?.Close();
                    RefreshAll();
                    ArrangementChanged?.Invoke();
                }
                else
                {
                    SetFeedback(dest == ContainerKind.Stash ? "Kho đồ đã đầy!" : "Balo đã đầy!");
                }
            }
        }

        private void CancelDrag()
        {
            if (_plateImagesById.TryGetValue(_dragInstanceId, out Image srcImg) && srcImg != null)
            {
                srcImg.color = Color.white;
            }

            _isDragging = false;
            _dragInstanceId = 0;
            _dragDef = null;
            if (_dragGhostRoot != null) _dragGhostRoot.gameObject.SetActive(false);
            ClearHighlights();
            RebuildSynergyBridges(CurrentSynergy, null);
        }

        private void EnsureDragGhost()
        {
            if (_dragGhostRoot != null) return;
            _dragGhostRoot = CombatUiFactory.CreateRect("DragGhost", _safeFrame, 0f, 0f, 20f, 20f);
            _dragGhostPlate = _dragGhostRoot.gameObject.AddComponent<Image>();
            _dragGhostPlate.raycastTarget = false;
            _dragGhostIcon = CombatUiFactory.CreateImage("GhostIcon", _dragGhostRoot, null, 2f, 2f, 16f, 16f);
        }

        private void RebuildDragGhostVisual()
        {
            if (_dragGhostRoot == null || _dragDef == null) return;
            _dragGhostRoot.gameObject.SetActive(true);
            _dragGhostRoot.SetAsLastSibling();

            IReadOnlyList<GridCell> rotCells = BackpackArrangement.RotateFootprint(_dragDef.Cells, _dragOrientation);
            var tuples = ToTuples(rotCells, out int gw, out int gh);
            Sprite plate = _plateBuilder.GetOrBuildPlate(
                _sprites,
                _dragDef.Tier.ToString(),
                _dragDef.Footprint,
                _dragOrientation,
                highlight: true,
                tuples);

            float pw = gw * CellPx;
            float ph = gh * CellPx;
            _dragGhostRoot.sizeDelta = new Vector2(pw, ph);
            _dragGhostPlate.sprite = plate;

            int o = ((_dragOrientation % 4) + 4) % 4;
            bool compact = (o % 2 == 1) && (_dragDef.Footprint != "1x1" && _dragDef.Footprint != "2x2");
            if (o != 0 && _dragDef.Footprint == "l") compact = true;
            Sprite icon = _sprites.GetItemIcon(_dragDef.Art, _dragDef.Footprint, compact: compact);
            if (icon != null && (icon.rect.width > pw - 2f || icon.rect.height > ph - 2f))
            {
                icon = _sprites.GetItemIcon(_dragDef.Art, _dragDef.Footprint, compact: true);
            }

            _dragGhostIcon.enabled = icon != null;
            if (icon != null)
            {
                _dragGhostIcon.sprite = icon;
                var rt = _dragGhostIcon.rectTransform;
                rt.sizeDelta = icon.rect.size;
                rt.anchoredPosition = new Vector2(Mathf.Round((pw - icon.rect.width) * 0.5f), -Mathf.Round((ph - icon.rect.height) * 0.5f));
            }
        }

        private void UpdateDragPreview(Vector2 screenPos)
        {
            if (!TryScreenToSafeFrame(screenPos, out float fx, out float fy)) return;

            if (_dragGhostRoot != null)
            {
                Vector2 sz = _dragGhostRoot.sizeDelta;
                _dragGhostRoot.anchoredPosition = new Vector2(Mathf.Round(fx - sz.x * 0.5f), -Mathf.Round(fy - sz.y * 0.5f));
            }

            ClearHighlights();
            if (!TryMapPointerToDropTarget(screenPos, out DropTargetKind targetKind, out int ox, out int oy))
            {
                RebuildSynergyBridges(CurrentSynergy, null);
                return;
            }

            if (targetKind == DropTargetKind.Trash)
            {
                if (_trashBg != null) _trashBg.color = CombatUiFactory.Blood;
                if (_trashLabel != null) _trashLabel.color = CombatUiFactory.Bone;
                SetFeedback("Thả để gỡ bỏ " + _dragDef.NameVi);
                return;
            }

            ContainerKind container = targetKind == DropTargetKind.Hero0 ? ContainerKind.Hero0 : ContainerKind.Stash;
            GridPreviewResult preview = _arrangement.PreviewPlacement(_dragDef.Id, _dragInstanceId, container, ox, oy, _dragOrientation);
            bool valid = preview.Code == GridOpCode.Ok;

            Image[,] hlGrid = targetKind == DropTargetKind.Hero0 ? _heroHighlights : _stashHighlights;
            int maxCols = targetKind == DropTargetKind.Hero0 ? HeroCols : StashCols;
            int maxRows = targetKind == DropTargetKind.Hero0 ? HeroRows : StashRows;
            Color32 hlColor = valid
                ? new Color32(0x3C, 0xB8, 0x78, 0x80)
                : new Color32(0xB0, 0x50, 0x5A, 0x90);

            for (int i = 0; i < preview.OccupiedCells.Count; i++)
            {
                GridCell c = preview.OccupiedCells[i];
                if (c.X >= 0 && c.X < maxCols && c.Y >= 0 && c.Y < maxRows)
                {
                    hlGrid[c.X, c.Y].color = hlColor;
                    hlGrid[c.X, c.Y].enabled = true;
                }
            }

            if (targetKind == DropTargetKind.Hero0 && valid)
            {
                ArrangementView view = _arrangement.CaptureView();
                SynergyPreviewResult synPreview = AdjacencySynergyEvaluator.PreviewPlacement(
                    view.HeroGrids[0], _dragDef, _dragInstanceId, ox, oy, _dragOrientation, _catalog);

                var previewHighlightSet = new HashSet<(long src, int rule, long dst)>();
                foreach (SynergyLink l in synPreview.GainedLinks)
                {
                    previewHighlightSet.Add((l.SourceInstanceId, l.RuleIndex, l.TargetInstanceId));
                }
                RebuildSynergyBridges(synPreview.ProjectedView, previewHighlightSet);
                SetFeedback(synPreview.GainedLinks.Count > 0
                    ? "Khớp! +" + synPreview.GainedLinks.Count + " liên kết cộng hưởng"
                    : "Khớp! Thả để đặt.");
            }
            else
            {
                RebuildSynergyBridges(CurrentSynergy, null);
                SetFeedback(valid ? "Thả để cất vào Kho." : TranslateOpError(preview.Code));
            }
        }

        private enum DropTargetKind { None, Hero0, Stash, Trash }

        private bool TryMapPointerToDropTarget(Vector2 screenPos, out DropTargetKind target, out int ox, out int oy)
        {
            target = DropTargetKind.None;
            ox = 0;
            oy = 0;

            if (!TryScreenToSafeFrame(screenPos, out float fx, out float fy) || _dragDef == null) return false;

            IReadOnlyList<GridCell> rotCells = BackpackArrangement.RotateFootprint(_dragDef.Cells, _dragOrientation);
            ToTuples(rotCells, out int gw, out int gh);
            float topLeftX = fx - gw * CellPx * 0.5f;
            float topLeftY = fy - gh * CellPx * 0.5f;

            if (fx >= HeroOriginX && fx <= HeroOriginX + HeroCols * CellPx &&
                fy >= HeroOriginY && fy <= HeroOriginY + HeroRows * CellPx)
            {
                target = DropTargetKind.Hero0;
                ox = Mathf.RoundToInt((topLeftX - HeroOriginX) / CellPx);
                oy = Mathf.RoundToInt((topLeftY - HeroOriginY) / CellPx);
                return true;
            }

            if (!_showingPicker)
            {
                if (fx >= StashOriginX && fx <= StashOriginX + StashCols * CellPx &&
                    fy >= StashOriginY && fy <= StashOriginY + StashRows * CellPx)
                {
                    target = DropTargetKind.Stash;
                    ox = Mathf.RoundToInt((topLeftX - StashOriginX) / CellPx);
                    oy = Mathf.RoundToInt((topLeftY - StashOriginY) / CellPx);
                    return true;
                }

                if (fx >= TrashX && fx <= TrashX + TrashW &&
                    fy >= TrashY && fy <= TrashY + TrashH)
                {
                    target = DropTargetKind.Trash;
                    return true;
                }
            }

            return false;
        }

        private bool TryScreenToSafeFrame(Vector2 screenPos, out float fx, out float fy)
        {
            fx = 0f;
            fy = 0f;
            if (_safeFrame == null) return false;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_safeFrame, screenPos, _uiCamera, out Vector2 local))
                return false;

            fx = local.x + _safeFrame.rect.width * 0.5f;
            fy = _safeFrame.rect.height * 0.5f - local.y;
            return true;
        }

        private static List<(int x, int y)> ToTuples(IReadOnlyList<GridCell> cells, out int width, out int height)
        {
            int maxX = 0;
            int maxY = 0;
            var list = new List<(int x, int y)>(cells.Count);
            for (int i = 0; i < cells.Count; i++)
            {
                GridCell c = cells[i];
                list.Add((c.X, c.Y));
                if (c.X > maxX) maxX = c.X;
                if (c.Y > maxY) maxY = c.Y;
            }
            width = maxX + 1;
            height = maxY + 1;
            return list;
        }

        private static PlacedItemView FindPlacedItem(ArrangementView view, long instanceId)
        {
            if (view == null) return null;
            for (int g = 0; g < view.HeroGrids.Count; g++)
            {
                foreach (PlacedItemView p in view.HeroGrids[g].Items)
                {
                    if (p.InstanceId == instanceId) return p;
                }
            }
            if (view.Stash != null)
            {
                foreach (PlacedItemView p in view.Stash.Items)
                {
                    if (p.InstanceId == instanceId) return p;
                }
            }
            return null;
        }

        private void SetFeedback(string text)
        {
            if (_feedbackLabel != null) _feedbackLabel.text = text;
        }

        private static string TranslateOpError(GridOpCode code)
        {
            switch (code)
            {
                case GridOpCode.Occupied: return "Ô đã có trang bị!";
                case GridOpCode.OutOfBounds: return "Vượt quá khung balo!";
                case GridOpCode.LockedCell: return "Ô chưa mở khóa!";
                case GridOpCode.Frozen: return "Đang chiến đấu, không thể sửa!";
                default: return "Không thể đặt ở đây (" + code + ")";
            }
        }
    }
}
