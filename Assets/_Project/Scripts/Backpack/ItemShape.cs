using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project05.Backpack
{
    /// <summary>
    /// Represents a multi-cell item shape (1x1, 1x2, 2x2, L-shape, T-shape, Cross, etc.)
    /// along with its directional Synergy Star offsets, supporting 90-degree clockwise rotation (R key).
    /// </summary>
    [Serializable]
    public class ItemShape
    {
        [SerializeField] private List<GridCoord> _occupiedOffsets = new List<GridCoord>();
        [SerializeField] private List<GridCoord> _synergyStarOffsets = new List<GridCoord>();

        // Pre-computed rotated states (0, 1, 2, 3 clockwise 90-degree steps) for zero-GC runtime queries
        private List<GridCoord>[] _rotatedOccupied;
        private List<GridCoord>[] _rotatedStars;
        private GridCoord[] _rotatedSizes;

        public IReadOnlyList<GridCoord> BaseOccupiedOffsets => _occupiedOffsets;
        public IReadOnlyList<GridCoord> BaseSynergyStarOffsets => _synergyStarOffsets;

        public ItemShape()
        {
            _occupiedOffsets.Add(new GridCoord(0, 0));
            RebuildRotationCache();
        }

        public ItemShape(IEnumerable<GridCoord> occupiedOffsets, IEnumerable<GridCoord> synergyStarOffsets = null)
        {
            _occupiedOffsets = new List<GridCoord>();
            if (occupiedOffsets != null)
            {
                foreach (GridCoord c in occupiedOffsets)
                {
                    if (!_occupiedOffsets.Contains(c))
                    {
                        _occupiedOffsets.Add(c);
                    }
                }
            }

            if (_occupiedOffsets.Count == 0)
            {
                _occupiedOffsets.Add(new GridCoord(0, 0));
            }

            _synergyStarOffsets = new List<GridCoord>();
            if (synergyStarOffsets != null)
            {
                foreach (GridCoord s in synergyStarOffsets)
                {
                    if (!_synergyStarOffsets.Contains(s) && !_occupiedOffsets.Contains(s))
                    {
                        _synergyStarOffsets.Add(s);
                    }
                }
            }

            RebuildRotationCache();
        }

        public void RebuildRotationCache()
        {
            if (_occupiedOffsets == null || _occupiedOffsets.Count == 0)
            {
                _occupiedOffsets = new List<GridCoord> { new GridCoord(0, 0) };
            }
            if (_synergyStarOffsets == null)
            {
                _synergyStarOffsets = new List<GridCoord>();
            }

            _rotatedOccupied = new List<GridCoord>[4];
            _rotatedStars = new List<GridCoord>[4];
            _rotatedSizes = new GridCoord[4];

            List<GridCoord> currOcc = new List<GridCoord>(_occupiedOffsets);
            List<GridCoord> currStars = new List<GridCoord>(_synergyStarOffsets);

            for (int step = 0; step < 4; step++)
            {
                NormalizeAndStore(step, currOcc, currStars);

                // Prepare next 90-degree clockwise step
                List<GridCoord> nextOcc = new List<GridCoord>(currOcc.Count);
                for (int i = 0; i < currOcc.Count; i++)
                {
                    nextOcc.Add(currOcc[i].Rotate90ClockwiseRaw());
                }

                List<GridCoord> nextStars = new List<GridCoord>(currStars.Count);
                for (int i = 0; i < currStars.Count; i++)
                {
                    nextStars.Add(currStars[i].Rotate90ClockwiseRaw());
                }

                currOcc = nextOcc;
                currStars = nextStars;
            }
        }

        private void NormalizeAndStore(int step, List<GridCoord> occ, List<GridCoord> stars)
        {
            int minX = int.MaxValue;
            int minY = int.MaxValue;
            int maxX = int.MinValue;
            int maxY = int.MinValue;

            for (int i = 0; i < occ.Count; i++)
            {
                GridCoord c = occ[i];
                if (c.X < minX) minX = c.X;
                if (c.Y < minY) minY = c.Y;
                if (c.X > maxX) maxX = c.X;
                if (c.Y > maxY) maxY = c.Y;
            }

            GridCoord shift = new GridCoord(-minX, -minY);
            List<GridCoord> normOcc = new List<GridCoord>(occ.Count);
            for (int i = 0; i < occ.Count; i++)
            {
                normOcc.Add(occ[i] + shift);
            }

            List<GridCoord> normStars = new List<GridCoord>(stars.Count);
            for (int i = 0; i < stars.Count; i++)
            {
                normStars.Add(stars[i] + shift);
            }

            _rotatedOccupied[step] = normOcc;
            _rotatedStars[step] = normStars;
            _rotatedSizes[step] = new GridCoord(maxX - minX + 1, maxY - minY + 1);
        }

        private void EnsureCache()
        {
            if (_rotatedOccupied == null || _rotatedOccupied.Length != 4 || _rotatedOccupied[0] == null)
            {
                RebuildRotationCache();
            }
        }

        public static int NormalizeRotation(int rotationSteps)
        {
            int r = rotationSteps % 4;
            return r < 0 ? r + 4 : r;
        }

        public IReadOnlyList<GridCoord> GetOccupiedOffsets(int rotationSteps = 0)
        {
            EnsureCache();
            return _rotatedOccupied[NormalizeRotation(rotationSteps)];
        }

        public IReadOnlyList<GridCoord> GetSynergyStarOffsets(int rotationSteps = 0)
        {
            EnsureCache();
            return _rotatedStars[NormalizeRotation(rotationSteps)];
        }

        public GridCoord GetBoundingSize(int rotationSteps = 0)
        {
            EnsureCache();
            return _rotatedSizes[NormalizeRotation(rotationSteps)];
        }

        public int GetWidth(int rotationSteps = 0) => GetBoundingSize(rotationSteps).X;
        public int GetHeight(int rotationSteps = 0) => GetBoundingSize(rotationSteps).Y;

        // Preset Shape Builders
        public static ItemShape Create1x1(params GridCoord[] stars)
        {
            return new ItemShape(new[] { new GridCoord(0, 0) }, stars);
        }

        public static ItemShape Create1x2Vertical(params GridCoord[] stars)
        {
            return new ItemShape(new[] { new GridCoord(0, 0), new GridCoord(0, 1) }, stars);
        }

        public static ItemShape Create2x1Horizontal(params GridCoord[] stars)
        {
            return new ItemShape(new[] { new GridCoord(0, 0), new GridCoord(1, 0) }, stars);
        }

        public static ItemShape Create1x3Vertical(params GridCoord[] stars)
        {
            return new ItemShape(new[] { new GridCoord(0, 0), new GridCoord(0, 1), new GridCoord(0, 2) }, stars);
        }

        public static ItemShape Create2x2Square(params GridCoord[] stars)
        {
            return new ItemShape(new[]
            {
                new GridCoord(0, 0), new GridCoord(1, 0),
                new GridCoord(0, 1), new GridCoord(1, 1)
            }, stars);
        }

        public static ItemShape CreateLShape(params GridCoord[] stars)
        {
            return new ItemShape(new[]
            {
                new GridCoord(0, 0),
                new GridCoord(0, 1),
                new GridCoord(1, 1)
            }, stars);
        }

        public static ItemShape CreateTShape(params GridCoord[] stars)
        {
            return new ItemShape(new[]
            {
                new GridCoord(0, 0), new GridCoord(1, 0), new GridCoord(2, 0),
                new GridCoord(1, 1)
            }, stars);
        }
    }
}
