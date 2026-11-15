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
        [Tooltip("Drop area that sells the held item, in safe-frame px (x right, y down).")]
        [SerializeField] private Rect sellRect;
        [SerializeField] private SellZoneView sellZone;
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
        private bool _sellEnabled = true;

        /// <summary>Raised when an item is sold, with its record and price. The listener owns the money.</summary>
        public event Action<ItemRecord, int> ItemSold;

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
        public int HeldPrice => _heldItem == null ? 0 : ItemSale.Price(_heldItem.Record.tier, sellPriceByTier);

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

        // ------------------------------------------------------------------ input from the plates
        /// <summary>The pointer started dragging a plate: pick it up.</summary>
        public void BeginDrag(ItemHoverTarget target)
        {
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
            bool mouseRelease = Application.isPlaying && Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame;
            HoldAction action = _gesture.Click(Time.frameCount, mouseRelease);
            if (action == HoldAction.Pickup) Pickup(target, true);
            else if (action == HoldAction.Drop) Drop();
        }

        /// <summary>Picks an item up (used by the plates and by tests).</summary>
        public void Pickup(ItemHoverTarget target, bool byClick)
        {
            EnsureBoard();
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

            PlaceResult result = _targetContainer != null
                ? _board.Move(_heldItem, _targetContainer, _targetX, _targetY, _heldRotation)
                : PlaceResult.OutOfBounds;
            if (result != PlaceResult.Ok)
            {
                _board.Move(_heldItem, _origin.container, _origin.x, _origin.y, _origin.rot);
            }

            End();
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
            Place(_held, _heldItem);
            Release();
        }

        /// <summary>Takes the held item off the board and hides its plate; the price goes to whoever listens.</summary>
        private void Sell()
        {
            int price = HeldPrice;
            ItemRecord record = _heldItem.Record;
            _gesture.Dropped(Time.frameCount);
            _board.Remove(_heldItem);
            _map.Remove(_held);
            _held.gameObject.SetActive(false);
            Release();
            ItemSold?.Invoke(record, price);
        }

        private void Release()
        {
            _held = null;
            _heldItem = null;
            _overSell = false;
            popup.Suppressed = false;
            HideGhost();
            ShowSellZone();
        }

        private void ShowSellZone()
        {
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
            int stops = containers.Length + (_sellEnabled && sellZone != null ? 1 : 0);
            int index = _overSell ? containers.Length : Array.FindIndex(containers, c => c.id == _targetContainer);
            int next = (index + 1) % stops;
            _overSell = next == containers.Length;
            _targetContainer = _overSell ? null : containers[next].id;
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
                _overSell = _sellEnabled && sellZone != null && sellRect.Contains(_pointer);
            }

            if (_overSell)
            {
                _targetContainer = null;
                if (!_pointerMode) centre = sellRect.center;
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

        private void Place(ItemHoverTarget target, BoardItem item) => SetRect(target, Center(item, item.Rotation), item.Rotation);

        /// <summary>Puts a plate with its box centre at a point in the safe frame, turned by quarter turns clockwise.</summary>
        private static void SetRect(ItemHoverTarget target, Vector2 centre, int rotation)
        {
            RectTransform rt = target.Rect;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(centre.x, -centre.y);
            rt.localEulerAngles = new Vector3(0f, 0f, -90f * rotation);
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
