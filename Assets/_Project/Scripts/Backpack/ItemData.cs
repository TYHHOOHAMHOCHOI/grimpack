using System;
using System.Collections.Generic;
using Project05.Data;

namespace Project05.Backpack
{
    /// <summary>
    /// Runtime instance of an item placed inside a Hero's personal backpack or the Shared Stash.
    /// </summary>
    [Serializable]
    public class ItemData
    {
        private static int _nextInstanceCounter = 1;

        public string InstanceId { get; private set; }
        public ItemDefinitionSO Definition { get; private set; }
        public int AnchorX { get; set; }
        public int AnchorY { get; set; }
        public int RotationSteps { get; private set; }
        public int CurrentCooldown { get; set; }
        public int TriggerCountThisCombat { get; set; }

        public ItemData(ItemDefinitionSO definition, int rotationSteps = 0)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            InstanceId = $"{definition.ItemId}_{_nextInstanceCounter++}";
            Definition = definition;
            RotationSteps = ItemShape.NormalizeRotation(rotationSteps);
            AnchorX = 0;
            AnchorY = 0;
            CurrentCooldown = 0;
            TriggerCountThisCombat = 0;
        }

        public void SetRotation(int rotationSteps)
        {
            RotationSteps = ItemShape.NormalizeRotation(rotationSteps);
        }

        public void RotateClockwise()
        {
            RotationSteps = ItemShape.NormalizeRotation(RotationSteps + 1);
        }

        public IReadOnlyList<GridCoord> GetOccupiedOffsets()
        {
            return Definition.Shape.GetOccupiedOffsets(RotationSteps);
        }

        public IReadOnlyList<GridCoord> GetSynergyStarOffsets()
        {
            return Definition.Shape.GetSynergyStarOffsets(RotationSteps);
        }

        public int GetWidth() => Definition.Shape.GetWidth(RotationSteps);
        public int GetHeight() => Definition.Shape.GetHeight(RotationSteps);

        public void GetOccupiedGridCells(int anchorX, int anchorY, int rotationSteps, List<GridCoord> results)
        {
            results.Clear();
            IReadOnlyList<GridCoord> offsets = Definition.Shape.GetOccupiedOffsets(rotationSteps);
            for (int i = 0; i < offsets.Count; i++)
            {
                results.Add(new GridCoord(anchorX + offsets[i].X, anchorY + offsets[i].Y));
            }
        }

        public void GetSynergyStarGridCells(int anchorX, int anchorY, int rotationSteps, List<GridCoord> results)
        {
            results.Clear();
            IReadOnlyList<GridCoord> offsets = Definition.Shape.GetSynergyStarOffsets(rotationSteps);
            for (int i = 0; i < offsets.Count; i++)
            {
                results.Add(new GridCoord(anchorX + offsets[i].X, anchorY + offsets[i].Y));
            }
        }
    }
}
