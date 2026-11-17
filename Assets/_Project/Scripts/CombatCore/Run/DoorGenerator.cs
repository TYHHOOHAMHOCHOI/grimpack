using System;
using System.Collections.Generic;

namespace Project05.CombatCore.Run
{
    /// <summary>
    /// One door card: its tier, the certain buff, the room behind it and the curse a Gold door carries; or a fusion door
    /// (item-fusion.md), which has no fight, no buff and no curse.
    /// </summary>
    public sealed class DoorCard
    {
        public TierKind Tier { get; }
        public DoorBuff Buff { get; }
        /// <summary>The room's fight; null for a fusion door.</summary>
        public EncounterSpec Encounter { get; }
        /// <summary>The Gold door's curse, or null.</summary>
        public CurseDef Curse { get; }
        /// <summary>True for the Fusion Door: a room with no fight where one fusion may be made (item-fusion.md Rule 6).</summary>
        public bool IsFusion { get; private set; }

        /// <summary>A fusion door card (item-fusion.md Rule 5).</summary>
        public static DoorCard Fusion() => new DoorCard(TierKind.White, default, null, null) { IsFusion = true };

        public DoorCard(TierKind tier, DoorBuff buff, EncounterSpec encounter, CurseDef curse)
        {
            Tier = tier;
            Buff = buff;
            Encounter = encounter;
            Curse = curse;
        }
    }

    /// <summary>
    /// System 17 (with System 19's Gold column) as pure functions: three tier draws with the third-card masks, then
    /// System 16's three variant draws, all on the doors stream. Holds no state and no RNG of its own.
    /// </summary>
    public static class DoorGenerator
    {
        /// <summary>
        /// The tier weights of one card (Rules 3 and 4): the row of the target room, Gold zeroed when the curse cap is
        /// reached, and for card 3 masks (a) and (b). The array length never changes.
        /// </summary>
        public static int[] CardWeights(RunConfig cfg, int targetRoom, int cardIndex, TierKind? first, TierKind? second, bool goldMasked)
        {
            int[] w = cfg.WeightsFor(targetRoom);
            int gold = cfg.IndexOf(TierKind.Gold);
            if (goldMasked && gold >= 0) w[gold] = 0;
            if (cardIndex != 2 || first == null || second == null) return w;

            bool maskB = targetRoom >= cfg.SafeCardRoom && first.Value >= TierKind.Blue && second.Value >= TierKind.Blue;
            if (maskB)
            {
                for (int i = 0; i < w.Length; i++)
                {
                    if (cfg.Tiers[i] >= TierKind.Blue) w[i] = 0;
                }
            }
            else if (targetRoom >= cfg.MinDistinctRoom && first.Value == second.Value)
            {
                int same = cfg.IndexOf(first.Value);
                if (same >= 0) w[same] = 0;
            }

            return w;
        }

        /// <summary>One draw over a weight array; a single positive weight is a forced bound-1 call (Rule 4).</summary>
        public static int DrawIndex(int[] weights, RngStream stream)
        {
            int positive = 0, last = -1;
            for (int i = 0; i < weights.Length; i++)
            {
                if (weights[i] > 0)
                {
                    positive++;
                    last = i;
                }
            }

            if (positive == 0) throw new InvalidOperationException("ContractViolation: a door card has no legal tier.");
            if (positive == 1)
            {
                stream.NextBounded(1);
                return last;
            }

            return stream.NextWeighted(weights);
        }

        /// <summary>
        /// Builds the row of three cards leading to room targetRoom (2..Rooms). cursesCounted is the active plus pending
        /// curses; at curseCap Gold cannot appear. forceGold (debug, System 19 Rule 1) turns card 3 Gold when Gold is
        /// allowed for the room and not masked, without changing the number of draws. The stream advances by six calls.
        /// </summary>
        public static DoorCard[] BuildRow(RunConfig cfg, GameCatalog catalog, int targetRoom, int cursesCounted, int curseCap, RngStream doors, bool forceGold = false)
        {
            if (targetRoom < 2 || targetRoom > cfg.Rooms) throw new ArgumentOutOfRangeException(nameof(targetRoom), "ContractViolation: no door row leads to that room.");
            bool goldMasked = cursesCounted >= curseCap;
            var tiers = new TierKind[3];
            for (int c = 0; c < 3; c++)
            {
                int[] w = CardWeights(cfg, targetRoom, c, c == 2 ? tiers[0] : (TierKind?)null, c == 2 ? tiers[1] : (TierKind?)null, goldMasked);
                tiers[c] = cfg.Tiers[DrawIndex(w, doors)];
            }

            if (forceGold && !goldMasked && targetRoom >= cfg.MinRoomFor(TierKind.Gold) && cfg.IndexOf(TierKind.Gold) >= 0)
            {
                tiers[2] = TierKind.Gold;
            }

            var row = new DoorCard[3];
            for (int c = 0; c < 3; c++)
            {
                EncounterSpec spec = EncounterBuilder.Draw(cfg, catalog, targetRoom, tiers[c], doors)
                    ?? throw new InvalidOperationException($"ContractViolation: no template for room {targetRoom}, {tiers[c]}.");
                row[c] = new DoorCard(tiers[c], cfg.BuffOf(tiers[c]), spec, cfg.FindCurse(spec.CurseId));
            }

            return row;
        }
    }

    /// <summary>The fusion pass over a finished row (item-fusion.md Rules 4 and 5). Pure; draws only on its own stream.</summary>
    public static class FusionDoor
    {
        /// <summary>
        /// Whether the row to targetRoom may hold a fusion door: rooms FusionMinRoom..Rooms, fewer than FusionMaxPerRun
        /// offered so far, and the previous row had none.
        /// </summary>
        public static bool Eligible(RunConfig cfg, int targetRoom, int offeredSoFar, bool previousRowHadFusion) =>
            cfg != null && cfg.FusionMaxPerRun > 0 && targetRoom >= cfg.FusionMinRoom && targetRoom <= cfg.Rooms &&
            offeredSoFar < cfg.FusionMaxPerRun && !previousRowHadFusion;

        /// <summary>
        /// Applies the pass (force: the debug policy, every eligible row gets the door; the draw is still made): an eligible row makes one bounded draw (0..999) on the fusion stream and gets the door when
        /// the draw is below FusionChance, or always when none has been offered and the room is at least
        /// FusionGuaranteeRoom. The door replaces the lowest-tier card, the leftmost on a tie. Returns true when placed.
        /// </summary>
        public static bool Apply(RunConfig cfg, DoorCard[] row, int targetRoom, int offeredSoFar, bool previousRowHadFusion, RngStream fusion, bool force = false)
        {
            if (row == null || row.Length == 0 || fusion == null || !Eligible(cfg, targetRoom, offeredSoFar, previousRowHadFusion)) return false;
            int draw = fusion.NextBounded(1000);
            bool forced = force || (offeredSoFar == 0 && targetRoom >= cfg.FusionGuaranteeRoom);
            if (!forced && draw >= cfg.FusionChance) return false;
            int lowest = 0;
            for (int i = 1; i < row.Length; i++)
            {
                if (row[i].Tier < row[lowest].Tier) lowest = i;
            }

            row[lowest] = DoorCard.Fusion();
            return true;
        }
    }

    /// <summary>
    /// The item reward after a win (System 18, simplified: no commitment lean yet). Each slot draws an item tier from the
    /// door tier's row on the drops stream, then one item of that tier not already offered.
    /// </summary>
    public static class RewardOffer
    {
        public static List<string> Build(RunConfig cfg, GameCatalog catalog, TierKind doorTier, RngStream drops)
        {
            var offer = new List<string>(cfg.OfferSize);
            if (catalog == null) return offer;
            int[] row = cfg.ItemRowFor(doorTier);
            for (int slot = 0; slot < cfg.OfferSize; slot++)
            {
                int tierIndex = DoorGenerator.DrawIndex(row, drops);
                List<string> pool = Pool(cfg, catalog, (TierKind)tierIndex, offer);
                for (int down = tierIndex - 1; pool.Count == 0 && down >= 0; down--) pool = Pool(cfg, catalog, (TierKind)down, offer);
                if (pool.Count == 0) break;
                offer.Add(pool[drops.NextBounded(pool.Count)]);
            }

            return offer;
        }

        private static List<string> Pool(RunConfig cfg, GameCatalog catalog, TierKind tier, List<string> taken)
        {
            var pool = new List<string>();
            foreach (ItemDef item in catalog.Items)
            {
                if (item.Tier != tier || item.IsUpgraded || cfg.IsNonDroppable(item.Id) || taken.Contains(item.Id)) continue;   // + items only come from the forge
                pool.Add(item.Id);
            }

            pool.Sort(StringComparer.Ordinal);
            return pool;
        }
    }
}
