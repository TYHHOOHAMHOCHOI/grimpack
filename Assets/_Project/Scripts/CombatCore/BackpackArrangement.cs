using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Project05.CombatCore
{
    public interface IItemIdSource
    {
        long Next();
    }

    public sealed class SequentialItemIdSource : IItemIdSource
    {
        private long _next;

        public SequentialItemIdSource(long startAt = 1)
        {
            if (startAt <= 0) throw new ArgumentOutOfRangeException(nameof(startAt), "Item IDs must be positive.");
            _next = startAt;
        }

        public long Current => _next;

        public long Next()
        {
            long id = _next;
            _next = checked(_next + 1);
            return id;
        }
    }

    public sealed class PlacedItemView
    {
        public long InstanceId { get; }
        public string DefinitionId { get; }
        public ContainerKind Container { get; }
        public int OriginX { get; }
        public int OriginY { get; }
        public int Orientation { get; }
        public GridCell AnchorCell { get; }
        public int ReadingIndex { get; }
        public IReadOnlyList<GridCell> OccupiedCells { get; }

        public PlacedItemView(
            long instanceId,
            string definitionId,
            ContainerKind container,
            int originX,
            int originY,
            int orientation,
            GridCell anchorCell,
            int readingIndex,
            IReadOnlyList<GridCell> occupiedCells)
        {
            InstanceId = instanceId;
            DefinitionId = definitionId;
            Container = container;
            OriginX = originX;
            OriginY = originY;
            Orientation = orientation;
            AnchorCell = anchorCell;
            ReadingIndex = readingIndex;
            OccupiedCells = occupiedCells ?? Array.Empty<GridCell>();
        }
    }

    public sealed class TrayItemView
    {
        public long InstanceId { get; }
        public string DefinitionId { get; }
        public int TrayEntryNumber { get; }

        public TrayItemView(long instanceId, string definitionId, int trayEntryNumber)
        {
            InstanceId = instanceId;
            DefinitionId = definitionId;
            TrayEntryNumber = trayEntryNumber;
        }
    }

    public sealed class ArrangementGridView
    {
        private readonly bool[,] _openMask;

        public ContainerKind Container { get; }
        public int Width { get; }
        public int Height { get; }
        public IReadOnlyList<PlacedItemView> Items { get; }

        public bool IsOpen(int x, int y) =>
            x >= 0 && x < Width && y >= 0 && y < Height && _openMask[x, y];

        internal ArrangementGridView(
            ContainerKind container,
            int width,
            int height,
            bool[,] openMask,
            IReadOnlyList<PlacedItemView> items)
        {
            Container = container;
            Width = width;
            Height = height;
            _openMask = openMask;
            Items = items ?? Array.Empty<PlacedItemView>();
        }
    }

    public sealed class ArrangementView
    {
        public bool IsFrozen { get; }
        public IReadOnlyList<ArrangementGridView> HeroGrids { get; }
        public ArrangementGridView Stash { get; }
        public IReadOnlyList<TrayItemView> Tray { get; }

        public ArrangementView(
            bool isFrozen,
            IReadOnlyList<ArrangementGridView> heroGrids,
            ArrangementGridView stash,
            IReadOnlyList<TrayItemView> tray)
        {
            IsFrozen = isFrozen;
            HeroGrids = heroGrids ?? Array.Empty<ArrangementGridView>();
            Stash = stash;
            Tray = tray ?? Array.Empty<TrayItemView>();
        }
    }

    public sealed class GridPreviewResult
    {
        public GridOpCode Code { get; }
        public IReadOnlyList<GridCell> OccupiedCells { get; }
        public IReadOnlyList<long> BlockingItemIds { get; }
        public int ProjectedReadingIndex { get; }

        public GridPreviewResult(
            GridOpCode code,
            IReadOnlyList<GridCell> occupiedCells,
            IReadOnlyList<long> blockingItemIds,
            int projectedReadingIndex)
        {
            Code = code;
            OccupiedCells = occupiedCells ?? Array.Empty<GridCell>();
            BlockingItemIds = blockingItemIds ?? Array.Empty<long>();
            ProjectedReadingIndex = projectedReadingIndex;
        }
    }

    public sealed class GridOpResult
    {
        public GridOpCode Code { get; }
        public bool IsOk => Code == GridOpCode.Ok;
        public int CandidatesTried { get; }
        public int CellsUnlocked { get; }

        public GridOpResult(GridOpCode code, int candidatesTried = 0, int cellsUnlocked = 0)
        {
            Code = code;
            CandidatesTried = candidatesTried;
            CellsUnlocked = cellsUnlocked;
        }
    }

    /// <summary>
    /// Engine-free Grid Backpack model (System 8): hero grids, shared stash, reward tray,
    /// deterministic rotation, canonical anchor-cell reading order, single-step undo, freeze/thaw, and text snapshot.
    /// </summary>
    public sealed class BackpackArrangement
    {
        private sealed class GridState
        {
            public readonly ContainerKind Kind;
            public readonly int Width;
            public readonly int Height;
            public readonly bool[,] Open;
            public readonly long[,] Occupants;
            public readonly List<GridCell> UnlockSequence;
            public int UnlockedSeqCount;

            public GridState(ContainerKind kind, int width, int height, int startW, int startH, IReadOnlyList<GridCell> unlockSequence)
            {
                Kind = kind;
                Width = width;
                Height = height;
                Open = new bool[width, height];
                Occupants = new long[width, height];
                UnlockSequence = new List<GridCell>(unlockSequence ?? Array.Empty<GridCell>());
                UnlockedSeqCount = 0;

                for (int y = 0; y < startH && y < height; y++)
                {
                    for (int x = 0; x < startW && x < width; x++)
                    {
                        Open[x, y] = true;
                    }
                }
            }
        }

        private sealed class ItemInstanceState
        {
            public long InstanceId;
            public string DefinitionId;
            public ContainerKind Container;
            public int OriginX;
            public int OriginY;
            public int Orientation;
            public int TrayEntryNumber;
        }

        private sealed class UndoRecord
        {
            public long InstanceId;
            public ContainerKind OldContainer;
            public int OldOriginX;
            public int OldOriginY;
            public int OldOrientation;
            public int OldTrayEntryNumber;
            public int CreatedTrayEntryNumber;
        }

        private readonly GameCatalog _catalog;
        private readonly IItemIdSource _idSource;
        private readonly bool _rotationEnabled;
        private readonly List<GridState> _heroGrids;
        private readonly GridState _stash;
        private readonly Dictionary<long, ItemInstanceState> _itemsById = new Dictionary<long, ItemInstanceState>();
        private readonly List<long> _itemOrder = new List<long>();

        private int _nextTrayEntryNumber = 1;
        private UndoRecord _undo;

        public bool IsFrozen { get; private set; }
        public int UndoDepth => _undo != null ? 1 : 0;

        public BackpackArrangement(
            GameCatalog catalog,
            int heroCount,
            IReadOnlyList<BackpackProfileDef> heroProfiles,
            IItemIdSource idSource,
            int stashWidth = 8,
            int stashHeight = 2,
            bool rotationEnabled = true)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            if (idSource == null) throw new ArgumentNullException(nameof(idSource));
            if (heroCount < 1 || heroCount > 3) throw new ArgumentOutOfRangeException(nameof(heroCount));
            if (heroProfiles == null || heroProfiles.Count < heroCount) throw new ArgumentException("Insufficient hero profiles.", nameof(heroProfiles));
            if (stashWidth < 1 || stashHeight < 1) throw new ArgumentOutOfRangeException(nameof(stashWidth));

            _catalog = catalog;
            _idSource = idSource;
            _rotationEnabled = rotationEnabled;

            _heroGrids = new List<GridState>(heroCount);
            for (int i = 0; i < heroCount; i++)
            {
                BackpackProfileDef p = heroProfiles[i] ?? new BackpackProfileDef(4, 4, 4, 4, Array.Empty<GridCell>());
                _heroGrids.Add(new GridState((ContainerKind)i, p.Width, p.Height, p.StartWidth, p.StartHeight, p.UnlockSequence));
            }

            _stash = new GridState(ContainerKind.Stash, stashWidth, stashHeight, stashWidth, stashHeight, Array.Empty<GridCell>());
        }

        public GridOpResult CreateInTray(string definitionId, out long instanceId)
        {
            instanceId = 0;
            if (IsFrozen) return new GridOpResult(GridOpCode.Frozen);
            if (string.IsNullOrEmpty(definitionId) || _catalog.FindItem(definitionId) == null)
            {
                return new GridOpResult(GridOpCode.ContractViolation);
            }

            long nextId = _idSource.Next();
            if (nextId <= 0 || _itemsById.ContainsKey(nextId))
            {
                return new GridOpResult(GridOpCode.ContractViolation);
            }

            var item = new ItemInstanceState
            {
                InstanceId = nextId,
                DefinitionId = definitionId,
                Container = ContainerKind.Tray,
                OriginX = 0,
                OriginY = 0,
                Orientation = 0,
                TrayEntryNumber = _nextTrayEntryNumber++
            };
            _itemsById[nextId] = item;
            _itemOrder.Add(nextId);
            _undo = null;
            instanceId = nextId;
            return new GridOpResult(GridOpCode.Ok);
        }

        public GridOpResult Place(long instanceId, ContainerKind container, int originX, int originY, int orientation)
        {
            if (IsFrozen) return new GridOpResult(GridOpCode.Frozen);

            if (!TryGetContainerGrid(container, out GridState targetGrid))
            {
                return new GridOpResult(GridOpCode.UnknownContainer);
            }

            if (!_itemsById.TryGetValue(instanceId, out ItemInstanceState item))
            {
                return new GridOpResult(GridOpCode.UnknownItem);
            }

            if (orientation < 0 || orientation > 3)
            {
                return new GridOpResult(GridOpCode.InvalidOrientation);
            }

            if (!_rotationEnabled && orientation != 0)
            {
                return new GridOpResult(GridOpCode.RotationDisabled);
            }

            ItemDef def = _catalog.FindItem(item.DefinitionId);
            if (def == null) return new GridOpResult(GridOpCode.ContractViolation);

            List<GridCell> occupied = ComputeOccupiedCells(def.Cells, originX, originY, orientation);
            GridOpCode check = ValidateCellsInGrid(targetGrid, occupied, instanceId, out _);
            if (check != GridOpCode.Ok)
            {
                return new GridOpResult(check);
            }

            if (item.Container == container &&
                item.OriginX == originX &&
                item.OriginY == originY &&
                item.Orientation == orientation)
            {
                return new GridOpResult(GridOpCode.NoEffect);
            }

            _undo = new UndoRecord
            {
                InstanceId = item.InstanceId,
                OldContainer = item.Container,
                OldOriginX = item.OriginX,
                OldOriginY = item.OriginY,
                OldOrientation = item.Orientation,
                OldTrayEntryNumber = item.TrayEntryNumber,
                CreatedTrayEntryNumber = 0
            };

            ClearItemFromGrid(item);
            item.Container = container;
            item.OriginX = originX;
            item.OriginY = originY;
            item.Orientation = orientation;
            item.TrayEntryNumber = 0;
            WriteItemToGrid(targetGrid, occupied, item.InstanceId);

            return new GridOpResult(GridOpCode.Ok);
        }

        public GridOpResult AutoPlace(long instanceId, ContainerKind container)
        {
            if (IsFrozen) return new GridOpResult(GridOpCode.Frozen);

            if (!TryGetContainerGrid(container, out GridState targetGrid))
            {
                return new GridOpResult(GridOpCode.UnknownContainer);
            }

            if (!_itemsById.TryGetValue(instanceId, out ItemInstanceState item))
            {
                return new GridOpResult(GridOpCode.UnknownItem);
            }

            ItemDef def = _catalog.FindItem(item.DefinitionId);
            if (def == null) return new GridOpResult(GridOpCode.ContractViolation);

            int startOrientation = item.Container == ContainerKind.Tray ? 0 : item.Orientation;
            int maxOrientations = _rotationEnabled ? 4 : 1;
            var triedShapes = new List<List<GridCell>>(4);
            int candidatesTried = 0;

            for (int offset = 0; offset < maxOrientations; offset++)
            {
                int o = (startOrientation + offset) % 4;
                IReadOnlyList<GridCell> rotated = RotateFootprint(def.Cells, o);
                if (ContainsSameShape(triedShapes, rotated)) continue;
                triedShapes.Add(new List<GridCell>(rotated));

                for (int oy = 0; oy < targetGrid.Height; oy++)
                {
                    for (int ox = 0; ox < targetGrid.Width; ox++)
                    {
                        candidatesTried++;
                        List<GridCell> occupied = OffsetCells(rotated, ox, oy);
                        if (ValidateCellsInGrid(targetGrid, occupied, instanceId, out _) == GridOpCode.Ok)
                        {
                            ClearItemFromGrid(item);
                            item.Container = container;
                            item.OriginX = ox;
                            item.OriginY = oy;
                            item.Orientation = o;
                            item.TrayEntryNumber = 0;
                            WriteItemToGrid(targetGrid, occupied, item.InstanceId);
                            _undo = null;
                            return new GridOpResult(GridOpCode.Ok, candidatesTried: candidatesTried);
                        }
                    }
                }
            }

            return new GridOpResult(GridOpCode.NoPlacement, candidatesTried: candidatesTried);
        }

        public GridOpResult RemoveToTray(long instanceId)
        {
            if (IsFrozen) return new GridOpResult(GridOpCode.Frozen);
            if (!_itemsById.TryGetValue(instanceId, out ItemInstanceState item))
            {
                return new GridOpResult(GridOpCode.UnknownItem);
            }

            if (item.Container == ContainerKind.Tray)
            {
                return new GridOpResult(GridOpCode.NoEffect);
            }

            int newTrayNum = _nextTrayEntryNumber++;
            _undo = new UndoRecord
            {
                InstanceId = item.InstanceId,
                OldContainer = item.Container,
                OldOriginX = item.OriginX,
                OldOriginY = item.OriginY,
                OldOrientation = item.Orientation,
                OldTrayEntryNumber = 0,
                CreatedTrayEntryNumber = newTrayNum
            };

            ClearItemFromGrid(item);
            item.Container = ContainerKind.Tray;
            item.OriginX = 0;
            item.OriginY = 0;
            item.TrayEntryNumber = newTrayNum;
            return new GridOpResult(GridOpCode.Ok);
        }

        public GridOpResult DestroyTrayItem(long instanceId)
        {
            if (IsFrozen) return new GridOpResult(GridOpCode.Frozen);
            if (!_itemsById.TryGetValue(instanceId, out ItemInstanceState item))
            {
                return new GridOpResult(GridOpCode.UnknownItem);
            }

            if (item.Container != ContainerKind.Tray)
            {
                ClearItemFromGrid(item);
            }

            _itemsById.Remove(instanceId);
            _itemOrder.Remove(instanceId);
            _undo = null;
            return new GridOpResult(GridOpCode.Ok);
        }

        public GridOpResult UnlockHeroCells(int heroSlot, int count)
        {
            if (IsFrozen) return new GridOpResult(GridOpCode.Frozen);
            if (count < 0) return new GridOpResult(GridOpCode.ContractViolation);
            if (heroSlot < 0 || heroSlot >= _heroGrids.Count)
            {
                return new GridOpResult(GridOpCode.UnknownContainer);
            }

            GridState grid = _heroGrids[heroSlot];
            int opened = 0;
            while (opened < count && grid.UnlockedSeqCount < grid.UnlockSequence.Count)
            {
                GridCell cell = grid.UnlockSequence[grid.UnlockedSeqCount++];
                if (cell.X >= 0 && cell.X < grid.Width && cell.Y >= 0 && cell.Y < grid.Height && !grid.Open[cell.X, cell.Y])
                {
                    grid.Open[cell.X, cell.Y] = true;
                    opened++;
                }
            }

            _undo = null;
            return new GridOpResult(GridOpCode.Ok, cellsUnlocked: opened);
        }

        public GridOpResult Undo()
        {
            if (IsFrozen) return new GridOpResult(GridOpCode.Frozen);
            if (_undo == null) return new GridOpResult(GridOpCode.NothingToUndo);

            if (!_itemsById.TryGetValue(_undo.InstanceId, out ItemInstanceState item))
            {
                _undo = null;
                return new GridOpResult(GridOpCode.NothingToUndo);
            }

            UndoRecord rec = _undo;
            _undo = null;

            ClearItemFromGrid(item);
            item.Container = rec.OldContainer;
            item.OriginX = rec.OldOriginX;
            item.OriginY = rec.OldOriginY;
            item.Orientation = rec.OldOrientation;
            item.TrayEntryNumber = rec.OldTrayEntryNumber;

            if (rec.OldContainer != ContainerKind.Tray && TryGetContainerGrid(rec.OldContainer, out GridState targetGrid))
            {
                ItemDef def = _catalog.FindItem(item.DefinitionId);
                List<GridCell> occupied = ComputeOccupiedCells(def.Cells, item.OriginX, item.OriginY, item.Orientation);
                WriteItemToGrid(targetGrid, occupied, item.InstanceId);
            }

            return new GridOpResult(GridOpCode.Ok);
        }

        public GridPreviewResult PreviewPlacement(
            string definitionId,
            long existingInstanceId,
            ContainerKind container,
            int originX,
            int originY,
            int orientation)
        {
            if (IsFrozen)
            {
                return new GridPreviewResult(GridOpCode.Frozen, Array.Empty<GridCell>(), Array.Empty<long>(), -1);
            }

            if (!TryGetContainerGrid(container, out GridState targetGrid))
            {
                return new GridPreviewResult(GridOpCode.UnknownContainer, Array.Empty<GridCell>(), Array.Empty<long>(), -1);
            }

            ItemDef def = !string.IsNullOrEmpty(definitionId) ? _catalog.FindItem(definitionId) : null;
            if (def == null && existingInstanceId > 0 && _itemsById.TryGetValue(existingInstanceId, out ItemInstanceState existing))
            {
                def = _catalog.FindItem(existing.DefinitionId);
            }

            if (def == null)
            {
                return new GridPreviewResult(GridOpCode.ContractViolation, Array.Empty<GridCell>(), Array.Empty<long>(), -1);
            }

            if (orientation < 0 || orientation > 3)
            {
                return new GridPreviewResult(GridOpCode.InvalidOrientation, Array.Empty<GridCell>(), Array.Empty<long>(), -1);
            }

            if (!_rotationEnabled && orientation != 0)
            {
                return new GridPreviewResult(GridOpCode.RotationDisabled, Array.Empty<GridCell>(), Array.Empty<long>(), -1);
            }

            List<GridCell> occupied = ComputeOccupiedCells(def.Cells, originX, originY, orientation);
            occupied.Sort(CompareCellsReadingOrder);

            GridOpCode code = ValidateCellsInGrid(targetGrid, occupied, existingInstanceId, out List<long> blockingIds);
            int projectedReadingIndex = -1;
            if (code == GridOpCode.Ok)
            {
                GridCell anchor = ComputeAnchor(occupied);
                projectedReadingIndex = ComputeProjectedReadingIndex(targetGrid, anchor, existingInstanceId);
            }

            return new GridPreviewResult(code, occupied, blockingIds, projectedReadingIndex);
        }

        public IReadOnlyList<long> Freeze()
        {
            if (IsFrozen) return Array.Empty<long>();

            var trayItems = new List<ItemInstanceState>();
            for (int i = 0; i < _itemOrder.Count; i++)
            {
                ItemInstanceState item = _itemsById[_itemOrder[i]];
                if (item.Container == ContainerKind.Tray)
                {
                    trayItems.Add(item);
                }
            }

            trayItems.Sort((a, b) => a.TrayEntryNumber.CompareTo(b.TrayEntryNumber));
            var destroyed = new List<long>(trayItems.Count);
            for (int i = 0; i < trayItems.Count; i++)
            {
                long id = trayItems[i].InstanceId;
                destroyed.Add(id);
                _itemsById.Remove(id);
                _itemOrder.Remove(id);
            }

            _undo = null;
            IsFrozen = true;
            return destroyed;
        }

        public GridOpResult Thaw()
        {
            if (!IsFrozen) return new GridOpResult(GridOpCode.NoEffect);
            IsFrozen = false;
            return new GridOpResult(GridOpCode.Ok);
        }

        public ArrangementView CaptureView()
        {
            var heroViews = new List<ArrangementGridView>(_heroGrids.Count);
            for (int i = 0; i < _heroGrids.Count; i++)
            {
                heroViews.Add(BuildGridView(_heroGrids[i]));
            }

            ArrangementGridView stashView = BuildGridView(_stash);

            var trayList = new List<TrayItemView>();
            for (int i = 0; i < _itemOrder.Count; i++)
            {
                ItemInstanceState item = _itemsById[_itemOrder[i]];
                if (item.Container == ContainerKind.Tray)
                {
                    trayList.Add(new TrayItemView(item.InstanceId, item.DefinitionId, item.TrayEntryNumber));
                }
            }
            trayList.Sort((a, b) => a.TrayEntryNumber.CompareTo(b.TrayEntryNumber));

            return new ArrangementView(IsFrozen, heroViews, stashView, trayList);
        }

        public string Snapshot()
        {
            ArrangementView view = CaptureView();
            var sb = new StringBuilder();
            sb.Append("GRIDBACKPACK-SNAPSHOT v1\n");
            sb.Append("frozen=").Append(view.IsFrozen ? "1" : "0").Append('\n');
            sb.Append("nextTray=").Append(_nextTrayEntryNumber.ToString(CultureInfo.InvariantCulture)).Append('\n');

            for (int i = 0; i < _heroGrids.Count; i++)
            {
                AppendGridSnapshot(sb, _heroGrids[i], view.HeroGrids[i]);
            }
            AppendGridSnapshot(sb, _stash, view.Stash);

            sb.Append("tray=").Append(view.Tray.Count.ToString(CultureInfo.InvariantCulture)).Append('\n');
            for (int i = 0; i < view.Tray.Count; i++)
            {
                TrayItemView t = view.Tray[i];
                sb.Append("trayItem=")
                  .Append(t.InstanceId.ToString(CultureInfo.InvariantCulture)).Append(',')
                  .Append(t.DefinitionId).Append(',')
                  .Append(t.TrayEntryNumber.ToString(CultureInfo.InvariantCulture)).Append('\n');
            }

            return sb.ToString();
        }

        public GridOpResult Restore(string snapshotText)
        {
            if (string.IsNullOrEmpty(snapshotText)) return new GridOpResult(GridOpCode.ContractViolation);
            string[] lines = snapshotText.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length < 3 || !string.Equals(lines[0], "GRIDBACKPACK-SNAPSHOT v1", StringComparison.Ordinal))
            {
                return new GridOpResult(GridOpCode.ContractViolation);
            }

            try
            {
                int idx = 1;
                if (!TryParseKeyVal(lines[idx++], "frozen", out string frozenStr)) return new GridOpResult(GridOpCode.ContractViolation);
                bool newFrozen = frozenStr == "1";

                if (!TryParseKeyVal(lines[idx++], "nextTray", out string nextTrayStr) ||
                    !int.TryParse(nextTrayStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out int newNextTray) ||
                    newNextTray < 1)
                {
                    return new GridOpResult(GridOpCode.ContractViolation);
                }

                var newGrids = new List<GridState>(_heroGrids.Count);
                for (int h = 0; h < _heroGrids.Count; h++)
                {
                    GridState clone = new GridState(
                        _heroGrids[h].Kind, _heroGrids[h].Width, _heroGrids[h].Height,
                        0, 0, _heroGrids[h].UnlockSequence);
                    newGrids.Add(clone);
                }
                var newStash = new GridState(ContainerKind.Stash, _stash.Width, _stash.Height, 0, 0, Array.Empty<GridCell>());
                var newItems = new Dictionary<long, ItemInstanceState>();
                var newOrder = new List<long>();

                int totalGrids = _heroGrids.Count + 1;
                for (int g = 0; g < totalGrids; g++)
                {
                    if (idx >= lines.Length || !TryParseKeyVal(lines[idx++], "grid", out string gridHeader))
                        return new GridOpResult(GridOpCode.ContractViolation);

                    string[] hp = gridHeader.Split(',');
                    if (hp.Length != 5 ||
                        !int.TryParse(hp[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int kindInt) ||
                        !int.TryParse(hp[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int w) ||
                        !int.TryParse(hp[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int h) ||
                        !int.TryParse(hp[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int unlockedSeq) ||
                        !int.TryParse(hp[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out int itemCount))
                    {
                        return new GridOpResult(GridOpCode.ContractViolation);
                    }

                    GridState target = g < _heroGrids.Count ? newGrids[g] : newStash;
                    if ((int)target.Kind != kindInt || target.Width != w || target.Height != h || unlockedSeq < 0)
                    {
                        return new GridOpResult(GridOpCode.ContractViolation);
                    }
                    target.UnlockedSeqCount = unlockedSeq;

                    if (idx >= lines.Length || !TryParseKeyVal(lines[idx++], "open", out string openMask) || openMask.Length != w * h)
                    {
                        return new GridOpResult(GridOpCode.ContractViolation);
                    }
                    for (int y = 0; y < h; y++)
                    {
                        for (int x = 0; x < w; x++)
                        {
                            target.Open[x, y] = openMask[y * w + x] == '1';
                        }
                    }

                    for (int it = 0; it < itemCount; it++)
                    {
                        if (idx >= lines.Length || !TryParseKeyVal(lines[idx++], "item", out string itemStr))
                            return new GridOpResult(GridOpCode.ContractViolation);

                        string[] ip = itemStr.Split(',');
                        if (ip.Length != 5 ||
                            !long.TryParse(ip[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out long instId) ||
                            !int.TryParse(ip[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int ox) ||
                            !int.TryParse(ip[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int oy) ||
                            !int.TryParse(ip[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out int orient))
                        {
                            return new GridOpResult(GridOpCode.ContractViolation);
                        }

                        string defId = ip[1];
                        ItemDef def = _catalog.FindItem(defId);
                        if (def == null || instId <= 0 || newItems.ContainsKey(instId) || orient < 0 || orient > 3)
                        {
                            return new GridOpResult(GridOpCode.ContractViolation);
                        }

                        List<GridCell> occupied = ComputeOccupiedCells(def.Cells, ox, oy, orient);
                        if (ValidateCellsInGrid(target, occupied, 0, out _) != GridOpCode.Ok)
                        {
                            return new GridOpResult(GridOpCode.ContractViolation);
                        }

                        WriteItemToGrid(target, occupied, instId);
                        newItems[instId] = new ItemInstanceState
                        {
                            InstanceId = instId,
                            DefinitionId = defId,
                            Container = target.Kind,
                            OriginX = ox,
                            OriginY = oy,
                            Orientation = orient,
                            TrayEntryNumber = 0
                        };
                        newOrder.Add(instId);
                    }
                }

                if (idx >= lines.Length || !TryParseKeyVal(lines[idx++], "tray", out string trayCountStr) ||
                    !int.TryParse(trayCountStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out int trayCount))
                {
                    return new GridOpResult(GridOpCode.ContractViolation);
                }

                var seenTrayNumbers = new HashSet<int>();
                for (int t = 0; t < trayCount; t++)
                {
                    if (idx >= lines.Length || !TryParseKeyVal(lines[idx++], "trayItem", out string tStr))
                        return new GridOpResult(GridOpCode.ContractViolation);

                    string[] tp = tStr.Split(',');
                    if (tp.Length != 3 ||
                        !long.TryParse(tp[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out long instId) ||
                        !int.TryParse(tp[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int trayNum))
                    {
                        return new GridOpResult(GridOpCode.ContractViolation);
                    }

                    string defId = tp[1];
                    if (_catalog.FindItem(defId) == null || instId <= 0 || newItems.ContainsKey(instId) || !seenTrayNumbers.Add(trayNum))
                    {
                        return new GridOpResult(GridOpCode.ContractViolation);
                    }

                    newItems[instId] = new ItemInstanceState
                    {
                        InstanceId = instId,
                        DefinitionId = defId,
                        Container = ContainerKind.Tray,
                        TrayEntryNumber = trayNum
                    };
                    newOrder.Add(instId);
                }

                if (idx != lines.Length) return new GridOpResult(GridOpCode.ContractViolation);

                // Commit restored state atomically
                for (int i = 0; i < _heroGrids.Count; i++) _heroGrids[i] = newGrids[i];
                CopyGridState(newStash, _stash);
                _itemsById.Clear();
                _itemOrder.Clear();
                for (int i = 0; i < newOrder.Count; i++)
                {
                    long id = newOrder[i];
                    _itemsById[id] = newItems[id];
                    _itemOrder.Add(id);
                }
                _nextTrayEntryNumber = newNextTray;
                IsFrozen = newFrozen;
                _undo = null;
                return new GridOpResult(GridOpCode.Ok);
            }
            catch
            {
                return new GridOpResult(GridOpCode.ContractViolation);
            }
        }

        public static IReadOnlyList<GridCell> RotateFootprint(IReadOnlyList<GridCell> baseCells, int orientation)
        {
            if (baseCells == null || baseCells.Count == 0) return Array.Empty<GridCell>();
            int o = ((orientation % 4) + 4) % 4;
            var rotated = new List<GridCell>(baseCells.Count);
            int minX = int.MaxValue;
            int minY = int.MaxValue;

            for (int i = 0; i < baseCells.Count; i++)
            {
                int x = baseCells[i].X;
                int y = baseCells[i].Y;
                for (int r = 0; r < o; r++)
                {
                    int nx = -y;
                    int ny = x;
                    x = nx;
                    y = ny;
                }
                if (x < minX) minX = x;
                if (y < minY) minY = y;
                rotated.Add(new GridCell(x, y));
            }

            for (int i = 0; i < rotated.Count; i++)
            {
                rotated[i] = new GridCell(rotated[i].X - minX, rotated[i].Y - minY);
            }

            rotated.Sort(CompareCellsReadingOrder);
            return rotated;
        }

        public static GridCell ComputeAnchor(IReadOnlyList<GridCell> occupiedCells)
        {
            if (occupiedCells == null || occupiedCells.Count == 0) return new GridCell(0, 0);
            GridCell best = occupiedCells[0];
            for (int i = 1; i < occupiedCells.Count; i++)
            {
                GridCell c = occupiedCells[i];
                if (c.Y < best.Y || (c.Y == best.Y && c.X < best.X))
                {
                    best = c;
                }
            }
            return best;
        }

        private ArrangementGridView BuildGridView(GridState grid)
        {
            var openCopy = new bool[grid.Width, grid.Height];
            Array.Copy(grid.Open, openCopy, grid.Open.Length);

            var rawPlaced = new List<PlacedItemView>();
            for (int i = 0; i < _itemOrder.Count; i++)
            {
                ItemInstanceState item = _itemsById[_itemOrder[i]];
                if (item.Container != grid.Kind) continue;

                ItemDef def = _catalog.FindItem(item.DefinitionId);
                List<GridCell> occupied = ComputeOccupiedCells(def.Cells, item.OriginX, item.OriginY, item.Orientation);
                occupied.Sort(CompareCellsReadingOrder);
                GridCell anchor = ComputeAnchor(occupied);
                rawPlaced.Add(new PlacedItemView(
                    item.InstanceId,
                    item.DefinitionId,
                    item.Container,
                    item.OriginX,
                    item.OriginY,
                    item.Orientation,
                    anchor,
                    0,
                    occupied));
            }

            rawPlaced.Sort((a, b) => ComparePlacedReadingOrder(a.AnchorCell, a.InstanceId, b.AnchorCell, b.InstanceId));

            var ordered = new List<PlacedItemView>(rawPlaced.Count);
            for (int i = 0; i < rawPlaced.Count; i++)
            {
                PlacedItemView p = rawPlaced[i];
                ordered.Add(new PlacedItemView(
                    p.InstanceId, p.DefinitionId, p.Container,
                    p.OriginX, p.OriginY, p.Orientation,
                    p.AnchorCell, i, p.OccupiedCells));
            }

            return new ArrangementGridView(grid.Kind, grid.Width, grid.Height, openCopy, ordered);
        }

        private int ComputeProjectedReadingIndex(GridState grid, GridCell newAnchor, long ignoredInstanceId)
        {
            int index = 0;
            for (int i = 0; i < _itemOrder.Count; i++)
            {
                ItemInstanceState other = _itemsById[_itemOrder[i]];
                if (other.Container != grid.Kind || other.InstanceId == ignoredInstanceId) continue;

                ItemDef def = _catalog.FindItem(other.DefinitionId);
                List<GridCell> occ = ComputeOccupiedCells(def.Cells, other.OriginX, other.OriginY, other.Orientation);
                GridCell otherAnchor = ComputeAnchor(occ);
                if (ComparePlacedReadingOrder(otherAnchor, other.InstanceId, newAnchor, ignoredInstanceId > 0 ? ignoredInstanceId : long.MaxValue) < 0)
                {
                    index++;
                }
            }
            return index;
        }

        private bool TryGetContainerGrid(ContainerKind container, out GridState grid)
        {
            if (container == ContainerKind.Stash)
            {
                grid = _stash;
                return true;
            }
            int idx = (int)container;
            if (idx >= 0 && idx < _heroGrids.Count)
            {
                grid = _heroGrids[idx];
                return true;
            }
            grid = null;
            return false;
        }

        private void ClearItemFromGrid(ItemInstanceState item)
        {
            if (item.Container == ContainerKind.Tray) return;
            if (!TryGetContainerGrid(item.Container, out GridState grid)) return;
            for (int y = 0; y < grid.Height; y++)
            {
                for (int x = 0; x < grid.Width; x++)
                {
                    if (grid.Occupants[x, y] == item.InstanceId) grid.Occupants[x, y] = 0;
                }
            }
        }

        private static void WriteItemToGrid(GridState grid, List<GridCell> occupied, long instanceId)
        {
            for (int i = 0; i < occupied.Count; i++)
            {
                GridCell c = occupied[i];
                grid.Occupants[c.X, c.Y] = instanceId;
            }
        }

        private GridOpCode ValidateCellsInGrid(
            GridState grid,
            List<GridCell> occupied,
            long ignoredInstanceId,
            out List<long> blockingIds)
        {
            blockingIds = new List<long>();
            bool anyLocked = false;
            bool anyOccupied = false;

            for (int i = 0; i < occupied.Count; i++)
            {
                GridCell c = occupied[i];
                if (c.X < 0 || c.X >= grid.Width || c.Y < 0 || c.Y >= grid.Height)
                {
                    return GridOpCode.OutOfBounds;
                }
                if (!grid.Open[c.X, c.Y])
                {
                    anyLocked = true;
                }
                long occ = grid.Occupants[c.X, c.Y];
                if (occ != 0 && occ != ignoredInstanceId)
                {
                    anyOccupied = true;
                    if (!blockingIds.Contains(occ)) blockingIds.Add(occ);
                }
            }

            if (anyLocked) return GridOpCode.LockedCell;
            if (anyOccupied)
            {
                // Order blocking items by reading order
                blockingIds.Sort((a, b) =>
                {
                    ItemInstanceState ia = _itemsById[a];
                    ItemInstanceState ib = _itemsById[b];
                    GridCell aa = ComputeAnchor(ComputeOccupiedCells(_catalog.FindItem(ia.DefinitionId).Cells, ia.OriginX, ia.OriginY, ia.Orientation));
                    GridCell ab = ComputeAnchor(ComputeOccupiedCells(_catalog.FindItem(ib.DefinitionId).Cells, ib.OriginX, ib.OriginY, ib.Orientation));
                    return ComparePlacedReadingOrder(aa, a, ab, b);
                });
                return GridOpCode.Occupied;
            }

            return GridOpCode.Ok;
        }

        private static List<GridCell> ComputeOccupiedCells(IReadOnlyList<GridCell> baseCells, int ox, int oy, int orientation)
        {
            IReadOnlyList<GridCell> rotated = RotateFootprint(baseCells, orientation);
            return OffsetCells(rotated, ox, oy);
        }

        private static List<GridCell> OffsetCells(IReadOnlyList<GridCell> localCells, int ox, int oy)
        {
            var result = new List<GridCell>(localCells.Count);
            for (int i = 0; i < localCells.Count; i++)
            {
                result.Add(new GridCell(ox + localCells[i].X, oy + localCells[i].Y));
            }
            return result;
        }

        private static bool ContainsSameShape(List<List<GridCell>> shapes, IReadOnlyList<GridCell> candidate)
        {
            for (int s = 0; s < shapes.Count; s++)
            {
                List<GridCell> existing = shapes[s];
                if (existing.Count != candidate.Count) continue;
                bool same = true;
                for (int i = 0; i < existing.Count; i++)
                {
                    if (existing[i] != candidate[i]) { same = false; break; }
                }
                if (same) return true;
            }
            return false;
        }

        private static int CompareCellsReadingOrder(GridCell a, GridCell b)
        {
            int cmpY = a.Y.CompareTo(b.Y);
            return cmpY != 0 ? cmpY : a.X.CompareTo(b.X);
        }

        private static int ComparePlacedReadingOrder(GridCell anchorA, long idA, GridCell anchorB, long idB)
        {
            int cmpY = anchorA.Y.CompareTo(anchorB.Y);
            if (cmpY != 0) return cmpY;
            int cmpX = anchorA.X.CompareTo(anchorB.X);
            return cmpX != 0 ? cmpX : idA.CompareTo(idB);
        }

        private static void AppendGridSnapshot(StringBuilder sb, GridState raw, ArrangementGridView view)
        {
            sb.Append("grid=")
              .Append(((int)raw.Kind).ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(raw.Width.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(raw.Height.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(raw.UnlockedSeqCount.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(view.Items.Count.ToString(CultureInfo.InvariantCulture)).Append('\n');

            sb.Append("open=");
            for (int y = 0; y < raw.Height; y++)
            {
                for (int x = 0; x < raw.Width; x++)
                {
                    sb.Append(raw.Open[x, y] ? '1' : '0');
                }
            }
            sb.Append('\n');

            for (int i = 0; i < view.Items.Count; i++)
            {
                PlacedItemView p = view.Items[i];
                sb.Append("item=")
                  .Append(p.InstanceId.ToString(CultureInfo.InvariantCulture)).Append(',')
                  .Append(p.DefinitionId).Append(',')
                  .Append(p.OriginX.ToString(CultureInfo.InvariantCulture)).Append(',')
                  .Append(p.OriginY.ToString(CultureInfo.InvariantCulture)).Append(',')
                  .Append(p.Orientation.ToString(CultureInfo.InvariantCulture)).Append('\n');
            }
        }

        private static bool TryParseKeyVal(string line, string expectedKey, out string value)
        {
            int eq = line.IndexOf('=');
            if (eq <= 0 || !string.Equals(line.Substring(0, eq), expectedKey, StringComparison.Ordinal))
            {
                value = null;
                return false;
            }
            value = line.Substring(eq + 1);
            return true;
        }

        private static void CopyGridState(GridState src, GridState dst)
        {
            Array.Copy(src.Open, dst.Open, src.Open.Length);
            Array.Copy(src.Occupants, dst.Occupants, src.Occupants.Length);
            dst.UnlockedSeqCount = src.UnlockedSeqCount;
        }
    }
}
