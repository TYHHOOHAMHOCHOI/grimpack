using System;

namespace Project05.Items
{
    /// <summary>
    /// Sell price of an item. A stand-in for Currency and Shop (System 20, not designed yet): the price comes from a table
    /// indexed by tier, which the scene supplies as data. Provisional values, not tuning.
    /// </summary>
    public static class ItemSale
    {
        /// <summary>Tier names in table order (art bible tier ladder).</summary>
        public static readonly string[] Tiers = { "White", "Green", "Blue", "Purple", "Gold", "Red" };

        /// <summary>The price of an item of the given tier, read from a table with one entry per tier.</summary>
        /// <param name="tier">Tier name as written in the item catalog, case ignored.</param>
        /// <param name="priceByTier">Prices in <see cref="Tiers"/> order.</param>
        /// <returns>The price, or 0 for an unknown tier or a missing table entry. Never negative.</returns>
        public static int Price(string tier, int[] priceByTier)
        {
            if (priceByTier == null) return 0;
            int index = Array.FindIndex(Tiers, t => string.Equals(t, tier, StringComparison.OrdinalIgnoreCase));
            if (index < 0 || index >= priceByTier.Length) return 0;
            return Math.Max(0, priceByTier[index]);
        }
    }
}
