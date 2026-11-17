using System;
using System.Collections.Generic;
using Project05.Items;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Project05.UI.Hud
{
    /// <summary>
    /// Drag, drop and rotate for the items of the workbench (design/gdd/backpack-ui.md, art bible 7.11, 7.12, 7.13).
    /// Pick up by dragging, or by clicking and clicking again to place; rotate with R or the right mouse button; with
    /// the keyboard pick with Enter, move with the arrows, Tab to switch container, Enter to place, Esc to cancel.
    /// A held item is lifted 2 px; a ghost footprint shows the target (Fog-hi outline when valid, Fog outline and a
    /// cross on blocking cells when not). An invalid drop returns the item to where it came from, never destroys it.
    /// Validity and synergy come from <see cref="ItemBoard"/>; this class only reads input and moves rectangles.
    /// </summary>
    public sealed class ItemBoardController : MonoBehaviour
    {
        /// <summary>Where a container sits in the safe frame, in native px.</summary>
        [Serializable]
        private sealed class ContainerView
        {
            public string id = string.Empty;
            public Vector2 origin;
            public int cols;
            public int rows;
            public bool grid;
        }

        /// <summary>Cell edge in native px (art bible 8.0, C = 20).</summary>
        public const int Cell = 20;

        private const float LiftPx = 2f;

        [SerializeField] private RectTransform safeFrame;
        [SerializeField] private ContainerView[] containers = Array.Empty<ContainerView>();
        [SerializeField] private ItemPopupController popup;
        [SerializeField] private RectTransform ghostLayer;
        [SerializeField] private Sprite ghostValid;
        [SerializeField] private Sprite ghostInvalid;
        [SerializeField] private Sprite ghostCross;
        [SerializeField] private ItemHoverTarget[] items = Array.Empty<ItemHoverTarget>();
        [Tooltip("The sell zone's drop area; a held item released over it is sold.")]
        [SerializeField] private RectTransform sellArea;
        [SerializeField] private SellZoneView sellZone;
        [Tooltip("The forge zone's drop area inside the drawer (item-upgrades.md).")]
        [SerializeField] private RectTransform forgeArea;
        [SerializeField] private ForgeZoneView forgeZone;
        [Tooltip("Bone '+' badge drawn on an upgraded item's plate.")]
        [SerializeField] private Sprite plusBadge;
        [Tooltip("The drawer that holds the sell zone; selling needs it open.")]
        [SerializeField] private SellDrawerView drawer;
        [Tooltip("Top layer a held plate moves to, so it draws above the stage and the drawer.")]
        [SerializeField] private RectTransform dragLayer;
        [SerializeField] private WorkbenchView workbench;
        [Tooltip("Fill used for the 2 px gap drawn between two items that touch (user request 2026-11-16).")]
        [SerializeField] private Sprite separatorSprite;
        [Tooltip("1 px INK-0 checker tiled over a locked hero-grid cell (it opens with a door buff).")]
        [SerializeField] private Sprite lockedCellSprite;
        [Tooltip("Provisional sell price per tier (White, Green, Blue, Purple, Gold, Red) until Currency and Shop (System 20) exists.")]
        [SerializeField] private int[] sellPriceByTier = { 1, 2, 3, 5, 8, 12 };

        private ItemBoard _board;
        private readonly Dictionary<ItemHoverTarget, BoardItem> _map = new Dictionary<ItemHoverTarget, BoardItem>();
        private readonly List<Image> _ghost = new List<Image>();

        private ItemHoverTarget _held;
        private BoardItem _heldItem;
        private (string container, int x, int y, int rot) _origin;
        private int _heldRotation;
        private Vector2 _offset;
        private Vector2 _pointer;
        private bool _pointerMode = true;
        private readonly HoldGesture _gesture = new HoldGesture();
        private Vector2 _lastMouse;
        private string _targetContainer;
        private int _targetX;
        private int _targetY;
        private PlaceResult _targetResult = PlaceResult.OutOfBounds;
        private readonly List<(int x, int y)> _bad = new List<(int x, int y)>();
        private bool _overSell;
        private bool _overForge;
        private ForgeQuote _quote;
        private bool _separatorsDirty = true;
        private RectTransform _separatorLayer;
        private readonly List<Image> _separators = new List<Image>();
        private readonly List<Image> _locks = new List<Image>();
        private Transform _heldParent;
        private bool _sellEnabled = true;

        /// <summary>Raised when an item is sold, with its record and price. The listener owns the money.</summary>
        public event Action<ItemRecord, int> ItemSold;

        /// <summary>The scene's answer for the held item: can it be upgraded now, for how much (gold, catalog). Null = no forge.</summary>
        public Func<ItemRecord, ForgeQuote> QuoteForge;

        /// <summary>An item was upgraded in place: (from, to, gold cost). The scene pays.</summary>
        public event Action<ItemRecord, ItemRecord, int> ItemUpgraded;

        /// <summary>Raised whenever items on the board change (move, rotate, sell, or spawn).</summary>
        public event Action BoardChanged;

        /// <summary>When true (during combat playback), picking up or moving items is disabled.</summary>
        public bool InputLocked { get; set; }

        /// <summary>Selling is allowed (Planning only). While false the sell zone refuses drops.</summary>
        public bool SellEnabled
        {
            get => _sellEnabled;
            set
            {
                _sellEnabled = value;
                if (_held == null) ShowSellZone();
            }
        }

        /// <summary>The price the held item would sell for, or 0.</summary>
        public int HeldPrice => _heldItem == null ? 0 : PriceOf(_heldItem.Record);

        /// <summary>Sell price of a record; a + item sells for the next tier's price (item-upgrades.md).</summary>
        public int PriceOf(ItemRecord record)
        {
            if (record == null) return 0;
            if (!record.IsUpgraded) return ItemSale.Price(record.tier, sellPriceByTier);
            int index = Array.FindIndex(ItemSale.Tiers, t => string.Equals(t, record.tier, StringComparison.OrdinalIgnoreCase));
            int next = Mathf.Clamp(index + 1, 0, ItemSale.Tiers.Length - 1);
            return ItemSale.Price(ItemSale.Tiers[next], sellPriceByTier);
        }

        /// <summary>The item being held, or null.</summary>
        public ItemHoverTarget Held => _held;

        /// <summary>The rules object behind the workbench.</summary>
        public ItemBoard Board
        {
            get
            {
                EnsureBoard();
                return _board;
            }
        }

        /// <summary>Sets the items, containers and sprites. Called by the scene builder.</summary>
        public void SetItems(ItemHoverTarget[] targets) => items = targets;

        private void OnEnable()
        {
            EnsureBoard();
            foreach (ItemHoverTarget t in items)
            {
                var button = t.GetComponent<Button>();
                if (button != null) button.onClick.AddListener(() => ClickPick(t));
            }
        }

        private void OnDisable()
        {
            foreach (ItemHoverTarget t in items)
            {
                var button = t != null ? t.GetComponent<Button>() : null;
                if (button != null) button.onClick.RemoveAllListeners();
            }
        }

        // ------------------------------------------------------------------ board setup
        private void EnsureBoard()
        {
            if (_board != null) return;
            _board = new ItemBoard();
            foreach (ContainerView c in containers) _board.AddContainer(c.id, c.cols, c.rows, c.grid);
            foreach (ItemHoverTarget t in items)
            {
                ItemRecord record = popup.Catalog.Find(t.ItemId);
                if (record == null) continue;
                BoardItem item = _board.Add(record, t.Container, t.CellX, t.CellY, t.Rotation);
                if (item == null)
                {
                    Debug.LogWarning("[ItemBoardController] " + t.ItemId + " does not fit at its start position.");
                    continue;
                }

                _map[t] = item;
                Place(t, item);
            }
        }

        /// <summary>The modifiers an item receives from its neighbours, and whether it sits in a hero grid.</summary>
        public bool Describe(ItemHoverTarget target, out List<SynergyModifier> received)
        {
            EnsureBoard();
            received = new List<SynergyModifier>();
            if (!_map.TryGetValue(target, out BoardItem item) || !_board.IsGrid(item.Container)) return false;
            if (_board.Evaluate(item.Container).TryGetValue(item, out List<SynergyModifier> mods)) received = mods;
            return true;
        }

        /// <summary>Returns a copy of all items currently inside the named container.</summary>
        public List<BoardItem> ExportContainerItems(string containerId)
        {
            EnsureBoard();
            var list = new List<BoardItem>();
            foreach (BoardItem item in _board.Items)
            {
                if (item.Container == containerId) list.Add(item);
            }
            return list;
        }

        /// <summary>
        /// Spawns a new item plate into <paramref name="preferredContainer"/> (or fallback container if full)
        /// at the first open slot, wires hover &amp; click-pick, and raises <see cref="BoardChanged"/>.
        /// </summary>
        public bool TrySpawnItem(
            ItemRecord record,
            Sprite plateSprite,
            Sprite iconSprite,
            string preferredContainer,
            out ItemHoverTarget spawned)
        {
            spawned = null;
            if (record == null || plateSprite == null) return false;
            EnsureBoard();

            string chosenContainer = null;
            int chosenX = 0;
            int chosenY = 0;
            int chosenRot = 0;

            string fallback = preferredContainer == "stash" ? "grid" : "stash";
            string[] tryOrder = { preferredContainer, fallback };
            foreach (string cid in tryOrder)
            {
                if (string.IsNullOrEmpty(cid)) continue;
                ContainerView view = FindView(cid);
                if (view == null) continue;
                if (TryFindFreePlacement(record, cid, view.cols, view.rows, out chosenX, out chosenY, out chosenRot))
                {
                    chosenContainer = cid;
                    break;
                }
            }

            if (chosenContainer == null) return false;

            BoardItem item = _board.Add(record, chosenContainer, chosenX, chosenY, chosenRot);
            if (item == null) return false;

            Transform parent = workbench != null ? workbench.transform : transform.parent;
            var go = new GameObject("Item_" + record.art, typeof(RectTransform), typeof(Image), typeof(Button), typeof(ItemHoverTarget));
            go.transform.SetParent(parent, false);
            var plate = go.GetComponent<Image>();
            plate.sprite = plateSprite;
            plate.type = Image.Type.Simple;
            plate.SetNativeSize();
            plate.raycastTarget = true;
            if (plateSprite.texture != null && plateSprite.texture.isReadable)
            {
                plate.alphaHitTestMinimumThreshold = 0.5f;
            }

            if (iconSprite != null)
            {
                var artGo = new GameObject("Art", typeof(RectTransform), typeof(Image));
                artGo.transform.SetParent(go.transform, false);
                var artRt = (RectTransform)artGo.transform;
                artRt.anchorMin = artRt.anchorMax = artRt.pivot = new Vector2(0f, 1f);
                artRt.anchoredPosition = new Vector2(2f, -2f);
                var artImg = artGo.GetComponent<Image>();
                artImg.sprite = iconSprite;
                artImg.type = Image.Type.Simple;
                artImg.SetNativeSize();
                artImg.raycastTarget = false;
            }

            var btn = go.GetComponent<Button>();
            btn.transition = Selectable.Transition.None;

            spawned = go.GetComponent<ItemHoverTarget>();
            spawned.Configure(record.id, chosenContainer, chosenX, chosenY, chosenRot, this, popup);
            ItemHoverTarget captured = spawned;
            btn.onClick.AddListener(() => ClickPick(captured));

            _map[spawned] = item;
            Place(spawned, item);
            BoardChanged?.Invoke();
            return true;
        }

        private bool TryFindFreePlacement(ItemRecord record, string containerId, int cols, int rows, out int bestX, out int bestY, out int bestRot)
        {
            var probe = new BoardItem { Id = -1, Record = record, Container = containerId };
            for (int rot = 0; rot < 4; rot++)
            {
                for (int y = 0; y < rows; y++)
                {
                    for (int x = 0; x < cols; x++)
                    {
                        if (_board.Check(probe, containerId, x, y, rot) == PlaceResult.Ok)
                        {
                            bestX = x;
                            bestY = y;
                            bestRot = rot;
                            return true;
                        }
                    }
                }
            }

            bestX = 0;
            bestY = 0;
            bestRot = 0;
            return false;
        }

        // ------------------------------------------------------------------ input from the plates
        /// <summary>The pointer started dragging a plate: pick it up.</summary>
        public void BeginDrag(ItemHoverTarget target)
        {
            if (InputLocked) return;
            if (_held == null) Pickup(target, false);
        }

        /// <summary>The pointer released a dragged plate: place it where the ghost is, or send it back.</summary>
        public void EndDrag(ItemHoverTarget target)
        {
            if (_held == target) Drop();
        }

        /// <summary>A plate was clicked or submitted: pick it up, or place the held item.</summary>
        public void ClickPick(ItemHoverTarget target)
        {
            if (InputLocked && _held == null) return;
            bool mouseRelease = Application.isPlaying && Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame;
            HoldAction action = _gesture.Click(Time.frameCount, mouseRelease);
            if (action == HoldAction.Pickup) Pickup(target, true);
            else if (action == HoldAction.Drop) Drop();
        }

        /// <summary>Picks an item up (used by the plates and by tests).</summary>
        public void Pickup(ItemHoverTarget target, bool byClick)
        {
            if (InputLocked) return;
            EnsureBoard();
            if (_held == null && _map.TryGetValue(target, out BoardItem pinned) && pinned.Pinned)
            {
                PinnedRefused?.Invoke();   // a cursed item never leaves its cells
                return;
            }

            if (_held != null || !_map.TryGetValue(target, out _heldItem)) return;

            _held = target;
            _gesture.Picked(Time.frameCount, byClick);
            _origin = (_heldItem.Container, _heldItem.X, _heldItem.Y, _heldItem.Rotation);
            _heldRotation = _heldItem.Rotation;
            _targetContainer = _heldItem.Container;
            _targetX = _heldItem.X;
            _targetY = _heldItem.Y;
            _pointer = Application.isPlaying && Mouse.current != null ? ToFrame(Mouse.current.position.ReadValue()) : Center(_heldItem, _heldRotation);
            _offset = _pointer - Center(_heldItem, _heldRotation);
            _pointerMode = true;
            popup.Suppressed = true;
            popup.Close();
            _heldParent = target.transform.parent;
            _separatorsDirty = true;
            if (dragLayer != null) target.transform.SetParent(dragLayer, false);   // same top-left origin, so positions hold
            target.transform.SetAsLastSibling();
            Refresh();
        }

        /// <summary>Turns the held item a quarter turn clockwise.</summary>
        public void RotateHeld()
        {
            if (_held == null) return;
            _heldRotation = (_heldRotation + 1) % 4;
            Refresh();
        }

        /// <summary>Places the held item at the ghost if valid; otherwise it returns to where it came from.</summary>
        public void Drop()
        {
            if (_held == null) return;
            if (_overSell && _sellEnabled)
            {
                Sell();
                return;
            }

            if (_overForge && _sellEnabled)
            {
                Forge();
                return;
            }

            PlaceResult result = _targetContainer != null
                ? _board.Move(_heldItem, _targetContainer, _targetX, _targetY, _heldRotation)
                : PlaceResult.OutOfBounds;
            if (result != PlaceResult.Ok)
            {
                _board.Move(_heldItem, _origin.container, _origin.x, _origin.y, _origin.rot);
            }

            End();
            BoardChanged?.Invoke();
        }

        /// <summary>Puts the held item back where it came from.</summary>
        public void Cancel()
        {
            if (_held == null) return;
            _board.Move(_heldItem, _origin.container, _origin.x, _origin.y, _origin.rot);
            End();
        }

        private void End()
        {
            _gesture.Dropped(Time.frameCount);
            RestoreParent();
            Place(_held, _heldItem);
            Release();
        }

        private void RestoreParent()
        {
            if (_heldParent != null && _held.transform.parent != _heldParent) _held.transform.SetParent(_heldParent, false);
            _heldParent = null;
        }

        /// <summary>Takes the held item off the board and hides its plate; the price goes to whoever listens.</summary>
        private void Sell()
        {
            int price = HeldPrice;
            ItemRecord record = _heldItem.Record;
            _gesture.Dropped(Time.frameCount);
            _board.Remove(_heldItem);
            _separatorsDirty = true;
            _map.Remove(_held);
            RestoreParent();
            _held.gameObject.SetActive(false);
            Release();
            ItemSold?.Invoke(record, price);
            BoardChanged?.Invoke();
        }

        private void Release()
        {
            _held = null;
            _heldItem = null;
            _overSell = false;
            _overForge = false;
            popup.Suppressed = false;
            HideGhost();
            ShowSellZone();
        }

        /// <summary>
        /// Upgrades the held item in place (item-upgrades.md): same cells, same rotation, the + record and a badge; the item
        /// goes back where it was picked up. A refused upgrade (gold, already +) also sends it back.
        /// </summary>
        private void Forge()
        {
            ForgeQuote quote = QuoteForge != null ? QuoteForge(_heldItem.Record) : default;
            if (!quote.Ok || quote.Upgrade == null)
            {
                Cancel();
                forgeZone?.Show(ForgeZoneState.Refused, quote);
                return;
            }

            ItemRecord from = _heldItem.Record;
            BoardItem item = _heldItem;
            ItemHoverTarget target = _held;
            _board.Move(item, _origin.container, _origin.x, _origin.y, _origin.rot);
            item.Record = quote.Upgrade;
            End();
            target.Configure(quote.Upgrade.id, item.Container, item.X, item.Y, item.Rotation, this, popup);
            AddPlusBadge(target);
            forgeZone?.Strike();
            ItemUpgraded?.Invoke(from, quote.Upgrade, quote.Cost);
            BoardChanged?.Invoke();
        }

        /// <summary>Marks a plate as upgraded with the Bone '+' badge at the bottom-right of its icon.</summary>
        public void AddPlusBadge(ItemHoverTarget target)
        {
            if (target == null || plusBadge == null || target.transform.Find("PlusBadge") != null) return;
            var go = new GameObject("PlusBadge", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(target.transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 0f);
            rt.sizeDelta = new Vector2(7f, 7f);
            rt.anchoredPosition = new Vector2(-3f, 3f);
            var img = go.GetComponent<Image>();
            img.sprite = plusBadge;
            img.raycastTarget = false;
        }

        private void ShowForgeZone()
        {
            if (forgeZone == null) return;
            if (!_sellEnabled) forgeZone.Show(ForgeZoneState.Locked, default);
            else if (_held == null) forgeZone.Show(ForgeZoneState.Idle, default);
            else
            {
                _quote = QuoteForge != null ? QuoteForge(_heldItem.Record) : default;
                forgeZone.Show(_overForge ? ForgeZoneState.Over : ForgeZoneState.Ready, _quote);
            }
        }

        private void ShowSellZone()
        {
            ShowForgeZone();
            if (sellZone == null) return;
            if (!_sellEnabled) sellZone.Show(SellZoneState.Locked, 0);
            else if (_held == null) sellZone.Show(SellZoneState.Idle, 0);
            else sellZone.Show(_overSell ? SellZoneState.Over : SellZoneState.Ready, HeldPrice);
        }

        // ------------------------------------------------------------------ per-frame input
        private void Update()
        {
            if (_held == null) return;

            Keyboard kb = Keyboard.current;
            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                Vector2 m = mouse.position.ReadValue();
                if ((m - _lastMouse).sqrMagnitude > 4f)
                {
                    _pointerMode = true;
                    _pointer = ToFrame(m);
                }

                _lastMouse = m;
                if (mouse.rightButton.wasPressedThisFrame) RotateHeld();
                // placing by click happens on release, like picking up, so the click event of that release is not seen twice
                if (mouse.leftButton.wasReleasedThisFrame && _gesture.MouseRelease(Time.frameCount) == HoldAction.Drop) Drop();
            }

            if (_held == null || kb == null) return;
            if (kb.rKey.wasPressedThisFrame) RotateHeld();
            if ((kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame) && _gesture.ConfirmKey(Time.frameCount) == HoldAction.Drop)
            {
                Drop();
                return;
            }

            int dx = (kb.rightArrowKey.wasPressedThisFrame ? 1 : 0) - (kb.leftArrowKey.wasPressedThisFrame ? 1 : 0);
            int dy = (kb.downArrowKey.wasPressedThisFrame ? 1 : 0) - (kb.upArrowKey.wasPressedThisFrame ? 1 : 0);
            if (kb.tabKey.wasPressedThisFrame) SwitchContainer();
            if ((dx != 0 || dy != 0) && !_overSell)
            {
                _pointerMode = false;
                _targetX += dx;
                _targetY += dy;
            }

            Refresh();
        }

        private void SwitchContainer()
        {
            // Tab cycles the containers, then the sell zone when selling is allowed.
            int stops = containers.Length + (_sellEnabled && sellArea != null ? 1 : 0);
            int index = _overSell ? containers.Length : Array.FindIndex(containers, c => c.id == _targetContainer);
            int next = (index + 1) % stops;
            _overSell = next == containers.Length;
            _targetContainer = _overSell ? null : containers[next].id;
            if (_overSell && drawer != null) drawer.OpenIn(SellDrawerView.DrawerMode.Sell);
            _targetX = 0;
            _targetY = 0;
            _pointerMode = false;
        }

        // ------------------------------------------------------------------ geometry and drawing
        /// <summary>Recomputes the target from the pointer or keyboard, then moves the held plate and the ghost.</summary>
        public void Refresh()
        {
            if (_held == null) return;
            (int w, int h) = ItemShapes.Size(ItemShapes.Rotate(_heldItem.Record.cells, _heldRotation));
            Vector2 centre = _pointer - _offset;

            if (_pointerMode)
            {
                // Bringing a held item over a drawer tab opens it in that mode, so selling or forging never needs a free hand.
                if (drawer != null && _sellEnabled)
                {
                    if (!(drawer.IsOpen && drawer.Mode == SellDrawerView.DrawerMode.Sell) && FrameMath.RectOf(drawer.TabRect, safeFrame).Contains(_pointer))
                    {
                        drawer.OpenIn(SellDrawerView.DrawerMode.Sell);
                    }
                    else if (drawer.ForgeTabRect != null && !(drawer.IsOpen && drawer.Mode == SellDrawerView.DrawerMode.Forge) &&
                             FrameMath.RectOf(drawer.ForgeTabRect, safeFrame).Contains(_pointer))
                    {
                        drawer.OpenIn(SellDrawerView.DrawerMode.Forge);
                    }
                }

                _overSell = CanSell && SellRect().Contains(_pointer);
                _overForge = CanForge && FrameMath.RectOf(forgeArea, safeFrame).Contains(_pointer);
            }

            if (_overForge)
            {
                _targetContainer = null;
            }
            else if (_overSell)
            {
                _targetContainer = null;
                if (!_pointerMode) centre = SellRect().center;
            }
            else if (_pointerMode)
            {
                Vector2 topLeft = centre - new Vector2(w * Cell, h * Cell) / 2f;
                _targetContainer = ContainerAt(centre, out ContainerView view);
                if (_targetContainer != null)
                {
                    _targetX = Mathf.RoundToInt((topLeft.x - view.origin.x) / Cell);
                    _targetY = Mathf.RoundToInt((topLeft.y - view.origin.y) / Cell);
                }
            }
            else if (_targetContainer != null)
            {
                ContainerView view = FindView(_targetContainer);
                centre = view.origin + new Vector2(_targetX * Cell + w * Cell / 2f, _targetY * Cell + h * Cell / 2f);
            }

            _bad.Clear();
            _targetResult = _targetContainer == null
                ? PlaceResult.OutOfBounds
                : _board.Check(_heldItem, _targetContainer, _targetX, _targetY, _heldRotation, _bad);

            SetRect(_held, new Vector2(Mathf.Round(centre.x), Mathf.Round(centre.y - LiftPx)), _heldRotation);
            DrawGhost();
            ShowSellZone();
            _held.transform.SetAsLastSibling();
            ghostLayer.SetAsLastSibling();
        }

        /// <summary>Selling is possible now: allowed by the screen, and the drawer (if any) is open on the sell zone.</summary>
        private bool CanSell => _sellEnabled && sellArea != null && (drawer == null || (drawer.IsOpen && drawer.Mode == SellDrawerView.DrawerMode.Sell));

        /// <summary>Forging is possible now: allowed by the screen, and the drawer is open on the forge zone.</summary>
        private bool CanForge => _sellEnabled && forgeArea != null && QuoteForge != null && drawer != null && drawer.IsOpen && drawer.Mode == SellDrawerView.DrawerMode.Forge;

        private Rect SellRect() => FrameMath.RectOf(sellArea, safeFrame);

        /// <summary>
        /// Grows or shrinks the hero grid (4 x 4 at the start, up to 7 x 7 later) and moves the stash under it. Refused when an
        /// item would fall outside the grid. Returns true when the size changed.
        /// </summary>
        public bool ResizeHeroGrid(int size)
        {
            EnsureBoard();
            int n = Mathf.Clamp(size, WorkbenchLayout.StartGrid, WorkbenchLayout.MaxGrid);
            ContainerView grid = Array.Find(containers, c => c.grid);
            if (grid == null || _held != null || !_board.Resize(grid.id, n, n)) return false;

            WorkbenchRects r = workbench != null ? workbench.Layout(n) : WorkbenchLayout.For(n);
            grid.cols = n;
            grid.rows = n;
            grid.origin = r.GridOrigin;
            foreach (ContainerView c in containers)
            {
                if (!c.grid) c.origin = r.StashOrigin;
            }

            foreach (KeyValuePair<ItemHoverTarget, BoardItem> pair in _map) Place(pair.Key, pair.Value);
            return true;
        }

        private string ContainerAt(Vector2 point, out ContainerView found)
        {
            foreach (ContainerView c in containers)
            {
                var rect = new Rect(c.origin.x - 6, c.origin.y - 6, c.cols * Cell + 12, c.rows * Cell + 12);
                if (rect.Contains(point))
                {
                    found = c;
                    return c.id;
                }
            }

            found = null;
            return null;
        }

        private ContainerView FindView(string id) => Array.Find(containers, c => c.id == id);

        private Vector2 Center(BoardItem item, int rotation)
        {
            ContainerView view = FindView(item.Container);
            (int w, int h) = ItemShapes.Size(ItemShapes.Rotate(item.Record.cells, rotation));
            return view.origin + new Vector2(item.X * Cell + w * Cell / 2f, item.Y * Cell + h * Cell / 2f);
        }

        private void Place(ItemHoverTarget target, BoardItem item)
        {
            SetRect(target, Center(item, item.Rotation), item.Rotation);
            _separatorsDirty = true;
        }

        // ------------------------------------------------------------------ locked cells and pinned items (doors, curses)
        /// <summary>Raised when the player tries to pick up an item pinned by a curse.</summary>
        public event Action PinnedRefused;

        /// <summary>Open cells of the hero grid (its square minus its locked cells).</summary>
        public int HeroCellsOpen
        {
            get
            {
                EnsureBoard();
                ContainerView grid = Array.Find(containers, c => c.grid);
                return grid == null ? 0 : grid.cols * grid.rows - _board.LockedCount(grid.id);
            }
        }

        /// <summary>Locked cells the hero grid can still open up to 7 x 7 (a door's +1 cell needs one).</summary>
        public int LockedHeroCellsLeft => Mathf.Max(0, WorkbenchLayout.MaxGrid * WorkbenchLayout.MaxGrid - HeroCellsOpen);

        /// <summary>
        /// Opens cells of the hero grid one at a time (a door's +1 cell, System 17 Rule 7). The grid grows as a square:
        /// when no locked cell is left in it, the square grows by one with its new ring locked, then a cell opens. The
        /// order is the last column top to bottom, then the last row left to right. Returns how many opened.
        /// </summary>
        public int OpenHeroCells(int count)
        {
            EnsureBoard();
            int opened = 0;
            for (int k = 0; k < count; k++)
            {
                ContainerView grid = Array.Find(containers, c => c.grid);
                if (grid == null) break;
                if (!TryNextLocked(grid, out int x, out int y))
                {
                    int n = grid.cols;
                    if (n >= WorkbenchLayout.MaxGrid || !ResizeHeroGrid(n + 1)) break;
                    for (int i = 0; i <= n; i++)
                    {
                        _board.SetLocked(grid.id, n, i, true);
                        _board.SetLocked(grid.id, i, n, true);
                    }

                    if (!TryNextLocked(grid, out x, out y)) break;
                }

                _board.SetLocked(grid.id, x, y, false);
                opened++;
            }

            _separatorsDirty = true;
            if (opened > 0) BoardChanged?.Invoke();
            return opened;
        }

        private bool TryNextLocked(ContainerView grid, out int x, out int y)
        {
            int n = grid.cols;
            for (y = 0; y < n; y++)
            {
                x = n - 1;
                if (_board.IsLocked(grid.id, x, y)) return true;
            }

            y = n - 1;
            for (x = 0; x < n; x++)
            {
                if (_board.IsLocked(grid.id, x, y)) return true;
            }

            x = y = 0;
            return false;
        }

        /// <summary>True when the item fits somewhere in the hero grid now (a Pinned curse's NeedsSpace check).</summary>
        public bool CanPlaceInHeroGrid(ItemRecord record)
        {
            EnsureBoard();
            ContainerView grid = Array.Find(containers, c => c.grid);
            return grid != null && record != null && TryFindFreePlacement(record, grid.id, grid.cols, grid.rows, out _, out _, out _);
        }

        /// <summary>
        /// Creates a cursed item pinned in the hero grid at the first legal position (orientation 0 first, then reading
        /// order), with the Plum pinned marks on its plate. Never falls back to the stash.
        /// </summary>
        public bool TrySpawnPinned(ItemRecord record, Sprite plateSprite, Sprite iconSprite, Sprite plumFill, Sprite pinGlyph, out ItemHoverTarget spawned)
        {
            spawned = null;
            if (!CanPlaceInHeroGrid(record)) return false;
            ContainerView grid = Array.Find(containers, c => c.grid);
            if (!TrySpawnItem(record, plateSprite, iconSprite, grid.id, out spawned)) return false;
            if (!_map.TryGetValue(spawned, out BoardItem item) || item.Container != grid.id) return false;
            item.Pinned = true;
            RectTransform plate = spawned.Rect;
            if (plumFill != null)
            {
                Mark(plate, "PinStripB", plumFill, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 2f), Vector2.zero);
                Mark(plate, "PinStripR", plumFill, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(2f, 0f), Vector2.zero);
                Image chip = Mark(plate, "PinChip", plumFill, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(10f, 10f), new Vector2(-3f, 3f));
                if (pinGlyph != null) Mark(chip.rectTransform, "PinGlyph", pinGlyph, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(8f, 8f), Vector2.zero);
            }

            return true;
        }

        private static Image Mark(RectTransform parent, string name, Sprite sprite, Vector2 anchorMin, Vector2 anchorMax, Vector2 size, Vector2 offset)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(anchorMax.x >= 1f ? 1f : 0f, 0f);
            rt.sizeDelta = size;
            rt.anchoredPosition = offset;
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
            return img;
        }

        /// <summary>Takes every item of a container off the board and removes its plate (a new hero's kit replaces the old).</summary>
        public void RemoveAllIn(string containerId)
        {
            EnsureBoard();
            var gone = new List<ItemHoverTarget>();
            foreach (KeyValuePair<ItemHoverTarget, BoardItem> pair in _map)
            {
                if (pair.Value.Container == containerId) gone.Add(pair.Key);
            }

            foreach (ItemHoverTarget t in gone)
            {
                _board.Remove(_map[t]);
                _map.Remove(t);
                if (t != null)
                {
                    t.gameObject.SetActive(false);
                    if (Application.isPlaying) Destroy(t.gameObject);
                    else DestroyImmediate(t.gameObject);
                }
            }

            _separatorsDirty = true;
            BoardChanged?.Invoke();
        }

        /// <summary>The plate of a board item, or null.</summary>
        public RectTransform PlateOf(BoardItem item)
        {
            foreach (KeyValuePair<ItemHoverTarget, BoardItem> pair in _map)
            {
                if (pair.Value == item) return pair.Key != null ? pair.Key.Rect : null;
            }

            return null;
        }

        // ------------------------------------------------------------------ gaps between touching items
        private void LateUpdate()
        {
            if (!_separatorsDirty || _board == null) return;
            _separatorsDirty = false;
            RebuildSeparators();
        }

        /// <summary>
        /// Two items that touch would read as one block: a 2 px INK-1 line is drawn over their shared edge, which covers the
        /// dark outer ring of both plates and leaves each lit tier frame standing on its own. A held item has no lines.
        /// </summary>
        private void RebuildSeparators()
        {
            if (separatorSprite == null || ghostLayer == null) return;
            if (_separatorLayer == null)
            {
                var go = new GameObject("ItemGaps", typeof(RectTransform));
                go.hideFlags = HideFlags.DontSave;
                go.transform.SetParent(ghostLayer.parent, false);
                _separatorLayer = (RectTransform)go.transform;
                _separatorLayer.anchorMin = _separatorLayer.anchorMax = _separatorLayer.pivot = new Vector2(0f, 1f);
                _separatorLayer.anchoredPosition = ghostLayer.anchoredPosition;
            }

            int used = 0;
            foreach (ContainerView c in containers)
            {
                var owner = new BoardItem[c.cols, c.rows];
                foreach (BoardItem item in _board.Items)
                {
                    if (item.Container != c.id || item == _heldItem) continue;
                    foreach (CellRecord cell in item.Cells)
                    {
                        int x = item.X + cell.x, y = item.Y + cell.y;
                        if (x >= 0 && y >= 0 && x < c.cols && y < c.rows) owner[x, y] = item;
                    }
                }

                for (int x = 0; x < c.cols; x++)
                {
                    for (int y = 0; y < c.rows; y++)
                    {
                        BoardItem here = owner[x, y];
                        if (here == null) continue;
                        if (x + 1 < c.cols && owner[x + 1, y] != null && owner[x + 1, y] != here)
                        {
                            Line(ref used, c.origin + new Vector2((x + 1) * Cell - 1, y * Cell), new Vector2(2f, Cell));
                        }

                        if (y + 1 < c.rows && owner[x, y + 1] != null && owner[x, y + 1] != here)
                        {
                            Line(ref used, c.origin + new Vector2(x * Cell, (y + 1) * Cell - 1), new Vector2(Cell, 2f));
                        }
                    }
                }
            }

            for (int i = used; i < _separators.Count; i++) _separators[i].gameObject.SetActive(false);

            // locked cells of the hero grid: a dark checker over the whole cell, so they never read as free cells
            int locks = 0;
            foreach (ContainerView c in containers)
            {
                if (!c.grid) continue;
                for (int y = 0; y < c.rows; y++)
                {
                    for (int x = 0; x < c.cols; x++)
                    {
                        if (!_board.IsLocked(c.id, x, y)) continue;
                        Image lockImg;
                        if (locks < _locks.Count) lockImg = _locks[locks];
                        else
                        {
                            var go = new GameObject("LockedCell", typeof(RectTransform), typeof(Image));
                            go.hideFlags = HideFlags.DontSave;
                            go.transform.SetParent(_separatorLayer, false);
                            lockImg = go.GetComponent<Image>();
                            lockImg.raycastTarget = false;
                            lockImg.sprite = lockedCellSprite != null ? lockedCellSprite : separatorSprite;
                            lockImg.type = Image.Type.Tiled;
                            var lrt = lockImg.rectTransform;
                            lrt.anchorMin = lrt.anchorMax = lrt.pivot = new Vector2(0f, 1f);
                            _locks.Add(lockImg);
                        }

                        locks++;
                        lockImg.gameObject.SetActive(true);
                        Vector2 tl = c.origin + new Vector2(x * Cell, y * Cell);
                        lockImg.rectTransform.anchoredPosition = new Vector2(tl.x, -tl.y);
                        lockImg.rectTransform.sizeDelta = new Vector2(Cell, Cell);
                    }
                }
            }

            for (int i = locks; i < _locks.Count; i++) _locks[i].gameObject.SetActive(false);
            _separatorLayer.SetAsLastSibling();
            ghostLayer.SetAsLastSibling();
        }

        private void Line(ref int used, Vector2 topLeft, Vector2 size)
        {
            Image line;
            if (used < _separators.Count) line = _separators[used];
            else
            {
                var go = new GameObject("Gap", typeof(RectTransform), typeof(Image));
                go.hideFlags = HideFlags.DontSave;
                go.transform.SetParent(_separatorLayer, false);
                line = go.GetComponent<Image>();
                line.raycastTarget = false;
                line.sprite = separatorSprite;
                var rt = line.rectTransform;
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
                _separators.Add(line);
            }

            used++;
            line.gameObject.SetActive(true);
            line.rectTransform.anchoredPosition = new Vector2(topLeft.x, -topLeft.y);
            line.rectTransform.sizeDelta = size;
        }

        /// <summary>Puts a plate with its box centre at a point in the safe frame, turned by quarter turns clockwise.</summary>
        private static void SetRect(ItemHoverTarget target, Vector2 centre, int rotation)
        {
            RectTransform rt = target.Rect;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(centre.x, -centre.y);
            rt.localEulerAngles = new Vector3(0f, 0f, -90f * rotation);
        }

        /// <summary>Current hero grid size in cells.</summary>
        public int HeroGridSize
        {
            get
            {
                ContainerView grid = Array.Find(containers, c => c.grid);
                return grid != null ? grid.cols : 0;
            }
        }

        private Vector2 ToFrame(Vector2 screen)
        {
            Canvas canvas = safeFrame.GetComponentInParent<Canvas>();
            Camera cam = canvas != null ? canvas.worldCamera : null;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(safeFrame, screen, cam, out Vector2 local);
            return new Vector2(local.x + safeFrame.rect.width / 2f, safeFrame.rect.height / 2f - local.y);
        }

        /// <summary>Places the pointer in safe-frame pixels (x right, y down), for tests and captures.</summary>
        public void SimulatePointer(Vector2 frame)
        {
            _pointer = frame;
            _pointerMode = true;
            Refresh();
        }

        private void DrawGhost()
        {
            CellRecord[] cells = ItemShapes.Rotate(_heldItem.Record.cells, _heldRotation);
            ContainerView view = _targetContainer != null ? FindView(_targetContainer) : null;
            bool valid = _targetResult == PlaceResult.Ok;
            int needed = view == null ? 0 : cells.Length * 2;
            while (_ghost.Count < needed) _ghost.Add(NewGhostImage());

            for (int i = 0; i < _ghost.Count; i++) _ghost[i].gameObject.SetActive(false);
            if (view == null) return;

            for (int i = 0; i < cells.Length; i++)
            {
                int cx = _targetX + cells[i].x;
                int cy = _targetY + cells[i].y;
                Image outline = _ghost[2 * i];
                outline.sprite = valid ? ghostValid : ghostInvalid;
                Show(outline, view.origin + new Vector2(cx * Cell, cy * Cell));

                bool blocking = _bad.Contains((cx, cy));
                Image cross = _ghost[2 * i + 1];
                cross.sprite = ghostCross;
                cross.gameObject.SetActive(blocking);
                if (blocking) Show(cross, view.origin + new Vector2(cx * Cell, cy * Cell));
            }
        }

        private static void Show(Image image, Vector2 topLeft)
        {
            image.gameObject.SetActive(true);
            image.rectTransform.anchoredPosition = new Vector2(topLeft.x, -topLeft.y);
        }

        private Image NewGhostImage()
        {
            var go = new GameObject("Ghost", typeof(RectTransform), typeof(Image));
            go.hideFlags = HideFlags.DontSave;
            go.transform.SetParent(ghostLayer, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(Cell, Cell);
            var image = go.GetComponent<Image>();
            image.raycastTarget = false;
            return image;
        }

        private void HideGhost()
        {
            foreach (Image g in _ghost) g.gameObject.SetActive(false);
        }
    }
}
