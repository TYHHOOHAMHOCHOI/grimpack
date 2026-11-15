using System.Collections.Generic;
using Project05.Items;
using UnityEngine;

namespace Project05.UI.Hud
{
    /// <summary>
    /// One hero grid: the items placed in it. Adjacency never crosses grids and items in the stash neither give nor
    /// receive (adjacency-and-synergy.md rule 1), so only items under a grid context take part.
    /// </summary>
    public sealed class ItemGridContext : MonoBehaviour
    {
        [SerializeField] private ItemHoverTarget[] items = System.Array.Empty<ItemHoverTarget>();

        /// <summary>Sets the items of this grid. Called by the scene builder.</summary>
        public void SetItems(ItemHoverTarget[] targets) => items = targets;

        /// <summary>The modifiers each item of the grid receives, keyed by the item object.</summary>
        public Dictionary<ItemHoverTarget, List<SynergyModifier>> Evaluate(ItemCatalog catalog)
        {
            var placed = new List<PlacedItem>();
            var owners = new List<ItemHoverTarget>();
            foreach (ItemHoverTarget t in items)
            {
                ItemRecord record = catalog.Find(t.ItemId);
                if (record == null) continue;
                placed.Add(new PlacedItem(record, t.CellX, t.CellY));
                owners.Add(t);
            }

            List<SynergyModifier>[] result = ItemSynergy.Evaluate(placed);
            var map = new Dictionary<ItemHoverTarget, List<SynergyModifier>>();
            for (int i = 0; i < owners.Count; i++) map[owners[i]] = result[i];
            return map;
        }

        /// <summary>The members of this grid.</summary>
        public IReadOnlyList<ItemHoverTarget> Items => items;
    }
}
