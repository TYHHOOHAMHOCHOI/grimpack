using System.Collections.Generic;

namespace Project05.Items
{
    /// <summary>Outcome of checking or committing a placement.</summary>
    public enum PlaceResult
    {
        /// <summary>The placement is valid.</summary>
        Ok,

        /// <summary>The container does not exist.</summary>
        UnknownContainer,

        /// <summary>At least one cell lies outside the container.</summary>
        OutOfBounds,

        /// <summary>At least one cell is taken by another item.</summary>
        Blocked,
    }

    /// <summary>One item on the board: its definition, container, origin cell and rotation in quarter turns.</summary>
    public sealed class BoardItem
    {
        public int Id;
        public ItemRecord Record;
        public string Container = string.Empty;
        public int X;
        public int Y;
        public int Rotation;

        /// <summary>The footprint for the current rotation, relative to the origin.</summary>
        public CellRecord[] Cells => ItemShapes.Rotate(Record.cells, Rotation);
    }

    /// <summary>
    /// The arrangement of items over containers: a hero grid and a stash. Pure data and rules, no engine types:
    /// placement must lie inside the container and on free cells (grid-backpack.md rule 5), and rotating re-derives the
    /// footprint with Formula 1. Only grids take part in synergy; the stash gives and receives nothing.
    /// </summary>
    public sealed class ItemBoard
    {
        private sealed class Container
        {
            public int Width;
            public int Height;
            public bool TakesPartInSynergy;
        }

        private readonly Dictionary<string, Container> _containers = new Dictionary<string, Container>();
        private readonly List<BoardItem> _items = new List<BoardItem>();
        private int _nextId;

        /// <summary>Every item on the board, in the order they were added.</summary>
        public IReadOnlyList<BoardItem> Items => _items;

        /// <summary>Adds a container of the given size in cells.</summary>
        public void AddContainer(string id, int width, int height, bool takesPartInSynergy)
        {
            _containers[id] = new Container { Width = width, Height = height, TakesPartInSynergy = takesPartInSynergy };
        }

        /// <summary>
        /// Changes the size of a container (a hero grid growing from 4 x 4 toward 7 x 7). Refused, changing nothing, when an
        /// item would end up outside it.
        /// </summary>
        public bool Resize(string id, int width, int height)
        {
            if (!_containers.TryGetValue(id, out Container c) || width < 1 || height < 1) return false;
            foreach (BoardItem item in _items)
            {
                if (item.Container != id) continue;
                foreach (CellRecord cell in item.Cells)
                {
                    if (item.X + cell.x >= width || item.Y + cell.y >= height) return false;
                }
            }

            c.Width = width;
            c.Height = height;
            return true;
        }

        /// <summary>Width and height of a container, or (0, 0) when it does not exist.</summary>
        public (int width, int height) SizeOf(string id) => _containers.TryGetValue(id, out Container c) ? (c.Width, c.Height) : (0, 0);

        /// <summary>True when the container exists and is a hero grid.</summary>
        public bool IsGrid(string container) => _containers.TryGetValue(container, out Container c) && c.TakesPartInSynergy;

        /// <summary>Adds an item at a valid position and returns it, or returns null when the position is not valid.</summary>
        public BoardItem Add(ItemRecord record, string container, int x, int y, int rotation)
        {
            var item = new BoardItem { Id = _nextId, Record = record, Container = container, X = x, Y = y, Rotation = ((rotation % 4) + 4) % 4 };
            if (Check(item, container, x, y, item.Rotation) != PlaceResult.Ok) return null;
            _nextId++;
            _items.Add(item);
            return item;
        }

        /// <summary>Checks a placement without changing anything. The item itself never blocks its own new position.</summary>
        public PlaceResult Check(BoardItem item, string container, int x, int y, int rotation)
        {
            return Check(item, container, x, y, rotation, null);
        }

        /// <summary>Checks a placement and lists the cells that are outside the container or taken (for the invalid ghost).</summary>
        public PlaceResult Check(BoardItem item, string container, int x, int y, int rotation, List<(int x, int y)> badCells)
        {
            if (!_containers.TryGetValue(container, out Container c)) return PlaceResult.UnknownContainer;

            PlaceResult result = PlaceResult.Ok;
            foreach (CellRecord cell in ItemShapes.Rotate(item.Record.cells, rotation))
            {
                int cx = x + cell.x;
                int cy = y + cell.y;
                if (cx < 0 || cy < 0 || cx >= c.Width || cy >= c.Height)
                {
                    result = PlaceResult.OutOfBounds;
                    badCells?.Add((cx, cy));
                }
                else if (Occupant(container, cx, cy, item) != null)
                {
                    if (result == PlaceResult.Ok) result = PlaceResult.Blocked;
                    badCells?.Add((cx, cy));
                }
            }

            return result;
        }

        /// <summary>Moves an item to a new place and rotation if that is valid. On any other result nothing changes.</summary>
        public PlaceResult Move(BoardItem item, string container, int x, int y, int rotation)
        {
            int turns = ((rotation % 4) + 4) % 4;
            PlaceResult result = Check(item, container, x, y, turns);
            if (result != PlaceResult.Ok) return result;
            item.Container = container;
            item.X = x;
            item.Y = y;
            item.Rotation = turns;
            return PlaceResult.Ok;
        }

        /// <summary>Takes an item off the board (sold or discarded). Returns false when it was not on the board.</summary>
        public bool Remove(BoardItem item) => _items.Remove(item);

        /// <summary>The item covering a cell, ignoring one item, or null.</summary>
        public BoardItem Occupant(string container, int x, int y, BoardItem ignore = null)
        {
            foreach (BoardItem other in _items)
            {
                if (other == ignore || other.Container != container) continue;
                foreach (CellRecord cell in other.Cells)
                {
                    if (other.X + cell.x == x && other.Y + cell.y == y) return other;
                }
            }

            return null;
        }

        /// <summary>
        /// The modifiers each item of a hero grid receives. Items in the stash, or any container that does not take part in
        /// synergy, receive nothing (adjacency-and-synergy.md rule 1).
        /// </summary>
        public Dictionary<BoardItem, List<SynergyModifier>> Evaluate(string container)
        {
            var map = new Dictionary<BoardItem, List<SynergyModifier>>();
            if (!IsGrid(container)) return map;

            var members = new List<BoardItem>();
            var placed = new List<PlacedItem>();
            foreach (BoardItem item in _items)
            {
                if (item.Container != container) continue;
                members.Add(item);
                placed.Add(new PlacedItem(item.Record, item.X, item.Y, item.Rotation));
            }

            List<SynergyModifier>[] result = ItemSynergy.Evaluate(placed);
            for (int i = 0; i < members.Count; i++) map[members[i]] = result[i];
            return map;
        }
    }
}
