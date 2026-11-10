using System;
using System.Collections.Generic;

namespace Project05.Backpack
{
    /// <summary>
    /// Manages a 2D grid of slots for either a Hero's personal backpack or the Shared Stash.
    /// Supports locked/unlocked slots, multi-cell item placement, rotation collision checks, and slot expansion.
    /// </summary>
    public class BackpackGrid
    {
        public string GridName { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }

        private readonly bool[,] _unlockedSlots;
        private readonly ItemData[,] _occupants;
        private readonly List<ItemData> _placedItems = new List<ItemData>();
        private readonly List<GridCoord> _tempCoords = new List<GridCoord>(16);

        public IReadOnlyList<ItemData> PlacedItems => _placedItems;

        public event Action OnGridChanged;

        public BackpackGrid(string gridName, int width, int height, int initialUnlockedCols = -1, int initialUnlockedRows = -1)
        {
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));

            GridName = gridName;
            Width = width;
            Height = height;
            _unlockedSlots = new bool[width, height];
            _occupants = new ItemData[width, height];

            int uCols = initialUnlockedCols < 0 ? width : Math.Min(width, initialUnlockedCols);
            int uRows = initialUnlockedRows < 0 ? height : Math.Min(height, initialUnlockedRows);

            for (int y = 0; y < uRows; y++)
            {
                for (int x = 0; x < uCols; x++)
                {
                    _unlockedSlots[x, y] = true;
                }
            }
        }

        public bool IsInBounds(int x, int y)
        {
            return x >= 0 && x < Width && y >= 0 && y < Height;
        }

        public bool IsSlotUnlocked(int x, int y)
        {
            return IsInBounds(x, y) && _unlockedSlots[x, y];
        }

        public bool UnlockSlot(int x, int y)
        {
            if (!IsInBounds(x, y)) return false;
            if (_unlockedSlots[x, y]) return false;
            _unlockedSlots[x, y] = true;
            OnGridChanged?.Invoke();
            return true;
        }

        public int UnlockNextLockedSlots(int count)
        {
            int unlocked = 0;
            for (int y = 0; y < Height && unlocked < count; y++)
            {
                for (int x = 0; x < Width && unlocked < count; x++)
                {
                    if (!_unlockedSlots[x, y])
                    {
                        _unlockedSlots[x, y] = true;
                        unlocked++;
                    }
                }
            }

            if (unlocked > 0)
            {
                OnGridChanged?.Invoke();
            }
            return unlocked;
        }

        public int GetUnlockedSlotCount()
        {
            int count = 0;
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (_unlockedSlots[x, y]) count++;
                }
            }
            return count;
        }

        public int GetLockedSlotCount()
        {
            return (Width * Height) - GetUnlockedSlotCount();
        }

        public ItemData GetItemAt(int x, int y)
        {
            if (!IsInBounds(x, y)) return null;
            return _occupants[x, y];
        }

        public bool CanPlaceItemAt(ItemData item, int anchorX, int anchorY, int rotationSteps, ItemData ignoreItem = null)
        {
            if (item == null || item.Definition == null) return false;

            item.GetOccupiedGridCells(anchorX, anchorY, rotationSteps, _tempCoords);
            for (int i = 0; i < _tempCoords.Count; i++)
            {
                GridCoord cell = _tempCoords[i];
                if (!IsSlotUnlocked(cell.X, cell.Y))
                {
                    return false;
                }

                ItemData occupant = _occupants[cell.X, cell.Y];
                if (occupant != null && occupant != item && occupant != ignoreItem)
                {
                    return false;
                }
            }

            return true;
        }

        public bool TryPlaceItem(ItemData item, int anchorX, int anchorY, int rotationSteps)
        {
            if (item == null) return false;
            int normRot = ItemShape.NormalizeRotation(rotationSteps);

            if (!CanPlaceItemAt(item, anchorX, anchorY, normRot, item))
            {
                return false;
            }

            // If already in this grid, clear old occupancy first
            if (_placedItems.Contains(item))
            {
                ClearOccupancy(item);
            }
            else
            {
                _placedItems.Add(item);
            }

            item.AnchorX = anchorX;
            item.AnchorY = anchorY;
            item.SetRotation(normRot);

            item.GetOccupiedGridCells(anchorX, anchorY, normRot, _tempCoords);
            for (int i = 0; i < _tempCoords.Count; i++)
            {
                GridCoord cell = _tempCoords[i];
                _occupants[cell.X, cell.Y] = item;
            }

            SortPlacedItemsDeterministically();
            OnGridChanged?.Invoke();
            return true;
        }

        public bool TryAutoPlaceItem(ItemData item)
        {
            if (item == null) return false;

            // Try current rotation first, then other rotations
            int startRot = item.RotationSteps;
            for (int rOffset = 0; rOffset < 4; rOffset++)
            {
                int rot = ItemShape.NormalizeRotation(startRot + rOffset);
                for (int y = 0; y < Height; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        if (CanPlaceItemAt(item, x, y, rot, item))
                        {
                            return TryPlaceItem(item, x, y, rot);
                        }
                    }
                }
            }

            return false;
        }

        public bool RemoveItem(ItemData item)
        {
            if (item == null || !_placedItems.Contains(item))
            {
                return false;
            }

            ClearOccupancy(item);
            _placedItems.Remove(item);
            OnGridChanged?.Invoke();
            return true;
        }

        public void ClearAllItems()
        {
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    _occupants[x, y] = null;
                }
            }
            _placedItems.Clear();
            OnGridChanged?.Invoke();
        }

        private void ClearOccupancy(ItemData item)
        {
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (_occupants[x, y] == item)
                    {
                        _occupants[x, y] = null;
                    }
                }
            }
        }

        private void SortPlacedItemsDeterministically()
        {
            _placedItems.Sort((a, b) =>
            {
                int cmpY = a.AnchorY.CompareTo(b.AnchorY);
                return cmpY != 0 ? cmpY : a.AnchorX.CompareTo(b.AnchorX);
            });
        }
    }
}
