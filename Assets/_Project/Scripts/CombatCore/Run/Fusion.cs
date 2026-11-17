using System;
using System.Collections.Generic;

namespace Project05.CombatCore.Run
{
    /// <summary>Why a fusion is refused (design/gdd/item-fusion.md Rule 8), in check order.</summary>
    public enum FusionStatus
    {
        Ok = 0,
        NotFusionRoom = 1,
        AlreadyFused = 2,
        UnknownRecipe = 3,
        MissingIngredient = 4,
        NotEnoughGold = 5,
        NoSpace = 6,
    }

    /// <summary>One owned item instance as the fusion rules see it: its id, definition and where it is.</summary>
    public readonly struct OwnedItem
    {
        public readonly long InstanceId;
        public readonly string DefinitionId;
        public readonly bool InStash;

        public OwnedItem(long instanceId, string definitionId, bool inStash)
        {
            InstanceId = instanceId;
            DefinitionId = definitionId ?? string.Empty;
            InStash = inStash;
        }
    }

    /// <summary>The outcome of planning one recipe against what the player owns and holds in gold.</summary>
    public sealed class FusionPlan
    {
        public FusionStatus Status { get; }
        public RecipeDef Recipe { get; }

        /// <summary>The instances that would be consumed, one per ingredient in recipe order (empty when one is missing).</summary>
        public IReadOnlyList<long> Consumed { get; }

        /// <summary>Per ingredient in recipe order: true when an instance was found.</summary>
        public IReadOnlyList<bool> Owned { get; }

        /// <summary>The base ids of the missing ingredients, in recipe order.</summary>
        public IReadOnlyList<string> Missing { get; }

        /// <summary>Consumed instances that are + items (their upgrade is lost, Rule 9).</summary>
        public IReadOnlyList<long> LosesUpgrade { get; }

        public int Cost { get; }

        /// <summary>Gold still needed (NotEnoughGold), else 0.</summary>
        public int Shortfall { get; }

        public bool IsOk => Status == FusionStatus.Ok;

        public FusionPlan(FusionStatus status, RecipeDef recipe, IReadOnlyList<long> consumed, IReadOnlyList<bool> owned,
            IReadOnlyList<string> missing, IReadOnlyList<long> losesUpgrade, int cost, int shortfall)
        {
            Status = status;
            Recipe = recipe;
            Consumed = consumed ?? Array.Empty<long>();
            Owned = owned ?? Array.Empty<bool>();
            Missing = missing ?? Array.Empty<string>();
            LosesUpgrade = losesUpgrade ?? Array.Empty<long>();
            Cost = cost;
            Shortfall = shortfall;
        }
    }

    /// <summary>
    /// The fusion rules as pure functions (design/gdd/item-fusion.md Rules 8 to 10): which instances a recipe consumes,
    /// what is missing and what it costs. The grid and the gold are the caller's; no RNG is drawn.
    /// </summary>
    public static class FusionRules
    {
        /// <summary>The base id of an item: a + item counts as its base (Rule 8).</summary>
        public static string BaseOf(GameCatalog catalog, string definitionId)
        {
            ItemDef def = catalog?.FindItem(definitionId);
            return def != null && def.IsUpgraded ? def.UpgradeOf : definitionId;
        }

        /// <summary>The gold a recipe costs: FusionCost2 for two ingredients, FusionCost3 for three (Rule 10).</summary>
        public static int Cost(RunConfig cfg, RecipeDef recipe) => recipe == null ? 0 : (cfg != null ? cfg.FusionCost(recipe.Size) : (recipe.Size >= 3 ? 10 : 6));

        /// <summary>
        /// Plans a recipe: checks the ingredients, then the gold (Rule 8 order after the phase checks). Each ingredient
        /// takes, among the owned instances of its base item not already taken: base before +, stash before grid, then
        /// the lowest instance id (Rule 9).
        /// </summary>
        public static FusionPlan Plan(GameCatalog catalog, RunConfig cfg, RecipeDef recipe, IReadOnlyList<OwnedItem> owned, int gold)
        {
            if (recipe == null) return new FusionPlan(FusionStatus.UnknownRecipe, null, null, null, null, null, 0, 0);
            int cost = Cost(cfg, recipe);
            var taken = new HashSet<long>();
            var consumed = new List<long>();
            var have = new List<bool>();
            var missing = new List<string>();
            var losesUpgrade = new List<long>();
            foreach (string ingredient in recipe.Ingredients)
            {
                bool found = false;
                OwnedItem best = default;
                bool bestUpgraded = false;
                foreach (OwnedItem o in owned ?? Array.Empty<OwnedItem>())
                {
                    if (taken.Contains(o.InstanceId) || BaseOf(catalog, o.DefinitionId) != ingredient) continue;
                    bool upgraded = o.DefinitionId != ingredient;
                    if (!found || Better(upgraded, o, bestUpgraded, best))
                    {
                        best = o;
                        bestUpgraded = upgraded;
                        found = true;
                    }
                }

                have.Add(found);
                if (!found)
                {
                    missing.Add(ingredient);
                    continue;
                }

                taken.Add(best.InstanceId);
                consumed.Add(best.InstanceId);
                if (bestUpgraded) losesUpgrade.Add(best.InstanceId);
            }

            if (missing.Count > 0)
                return new FusionPlan(FusionStatus.MissingIngredient, recipe, Array.Empty<long>(), have, missing, Array.Empty<long>(), cost, 0);
            if (gold < cost)
                return new FusionPlan(FusionStatus.NotEnoughGold, recipe, consumed, have, missing, losesUpgrade, cost, cost - gold);
            return new FusionPlan(FusionStatus.Ok, recipe, consumed, have, missing, losesUpgrade, cost, 0);
        }

        private static bool Better(bool upgraded, OwnedItem o, bool bestUpgraded, OwnedItem best)
        {
            if (upgraded != bestUpgraded) return !upgraded;              // base before +
            if (o.InStash != best.InStash) return o.InStash;             // stash before grid
            return o.InstanceId < best.InstanceId;                       // lowest id
        }

        /// <summary>How many recipes could be fused now (ingredients owned and gold enough): the door card's "Làm được: N".</summary>
        public static int CountMakeable(GameCatalog catalog, RunConfig cfg, IReadOnlyList<OwnedItem> owned, int gold)
        {
            int n = 0;
            if (catalog == null) return 0;
            foreach (RecipeDef r in catalog.Recipes)
            {
                if (Plan(catalog, cfg, r, owned, gold).IsOk) n++;
            }

            return n;
        }
    }
}
