using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Project05.CombatCore;
using Project05.CombatCore.Run;
using Project05.Items;

namespace Project05.Tests.EditMode
{
    /// <summary>
    /// The Fusion Door (design/gdd/item-fusion.md): recipe data, the fusion rules, the door's appearance rule and the
    /// fusion room's phase. Engine-free except the popup text test, which reads the UI item catalog.
    /// </summary>
    public sealed class FusionTests
    {
        private GameCatalog _catalog;
        private RunConfig _cfg;

        private static string Data(string file) => File.ReadAllText(Path.Combine("Assets/_Project/Data", file));

        [SetUp]
        public void SetUp()
        {
            _catalog = GameCatalog.LoadFromJson(Data("items_mvp.json"), Data("heroes_mvp.json"), Data("monsters_mvp.json"), out IReadOnlyList<string> errors);
            Assert.IsNotNull(_catalog, string.Join("\n", errors));
            Assert.IsEmpty(errors, string.Join("\n", errors));
            _cfg = RunConfig.Load(Data("run_mvp.json"), _catalog, out IReadOnlyList<string> cfgErrors);
            Assert.IsNotNull(_cfg, string.Join("\n", cfgErrors));
        }

        // ------------------------------------------------------------------ data (acceptance criterion 1)
        [Test]
        public void Catalog_TwelveRecipes_ResultsAreGoldTierWithoutUpgrade()
        {
            // Assert
            Assert.AreEqual(12, _catalog.Recipes.Count);
            int two = 0, three = 0;
            foreach (RecipeDef r in _catalog.Recipes)
            {
                ItemDef result = _catalog.FindItem(r.ResultId);
                Assert.IsNotNull(result, r.Id);
                Assert.AreEqual(TierKind.Gold, result.Tier, r.Id);
                Assert.IsTrue(string.IsNullOrEmpty(result.UpgradeId), r.Id + ": a fused item has no +");
                if (r.Size == 2) two++;
                if (r.Size == 3) three++;
            }

            Assert.AreEqual(8, two);
            Assert.AreEqual(4, three);
        }

        [Test]
        public void Recipes_ResultTakesFewerCellsThanItsIngredients()
        {
            foreach (RecipeDef r in _catalog.Recipes)
            {
                int cells = 0;
                foreach (string i in r.Ingredients) cells += _catalog.FindItem(i).Cells.Count;
                Assert.Less(_catalog.FindItem(r.ResultId).Cells.Count, cells, r.Id);
            }
        }

        [Test]
        public void Recipes_NeverOffered_AsRewards()
        {
            // Arrange
            var results = new HashSet<string>();
            foreach (RecipeDef r in _catalog.Recipes) results.Add(r.ResultId);
            var rng = new SeededRng(77);

            // Act / Assert: 1,000 offers over every door tier
            for (int i = 0; i < 1000; i++)
            {
                foreach (string id in RewardOffer.Build(_cfg, _catalog, (TierKind)(i % 5), rng.GetStream("drops")))
                    Assert.IsFalse(results.Contains(id), id);
            }
        }

        // ------------------------------------------------------------------ the fusion rules (Rules 8 to 10)
        [Test]
        public void Plan_PlainCopyBeforePlus_StashBeforeGrid()
        {
            // Arrange: a sword + and a plain sword in the grid; a whetstone in the grid and one in the stash
            var owned = new List<OwnedItem>
            {
                new OwnedItem(1, "item_iron_sword_plus", false),
                new OwnedItem(2, "item_iron_sword", false),
                new OwnedItem(3, "item_whetstone", false),
                new OwnedItem(4, "item_whetstone", true),
            };

            // Act
            FusionPlan plan = FusionRules.Plan(_catalog, _cfg, _catalog.FindRecipe("recipe_honed_sword"), owned, 6);

            // Assert
            Assert.AreEqual(FusionStatus.Ok, plan.Status);
            CollectionAssert.AreEqual(new long[] { 2, 4 }, plan.Consumed);
            Assert.IsEmpty(plan.LosesUpgrade);
            Assert.AreEqual(6, plan.Cost);
        }

        [Test]
        public void Plan_OnlyAPlusCopy_ConsumesItAndWarns()
        {
            var owned = new List<OwnedItem> { new OwnedItem(1, "item_iron_sword_plus", false), new OwnedItem(2, "item_whetstone", true) };

            FusionPlan plan = FusionRules.Plan(_catalog, _cfg, _catalog.FindRecipe("recipe_honed_sword"), owned, 10);

            Assert.AreEqual(FusionStatus.Ok, plan.Status);
            CollectionAssert.AreEqual(new long[] { 1 }, plan.LosesUpgrade);
        }

        [Test]
        public void Plan_MissingIngredient_ListsIt_BeforeGold()
        {
            var owned = new List<OwnedItem> { new OwnedItem(1, "item_iron_sword", false) };

            FusionPlan plan = FusionRules.Plan(_catalog, _cfg, _catalog.FindRecipe("recipe_honed_sword"), owned, 0);

            Assert.AreEqual(FusionStatus.MissingIngredient, plan.Status);
            CollectionAssert.AreEqual(new[] { "item_whetstone" }, plan.Missing);
            CollectionAssert.AreEqual(new[] { true, false }, plan.Owned);
            Assert.IsEmpty(plan.Consumed);
        }

        [Test]
        public void Plan_GoldShort_ReportsTheShortfall()
        {
            var owned = new List<OwnedItem> { new OwnedItem(1, "item_iron_sword", false), new OwnedItem(2, "item_whetstone", false) };

            FusionPlan two = FusionRules.Plan(_catalog, _cfg, _catalog.FindRecipe("recipe_honed_sword"), owned, 5);
            FusionPlan three = FusionRules.Plan(_catalog, _cfg, _catalog.FindRecipe("recipe_oathkeeper"), owned, 99);

            Assert.AreEqual(FusionStatus.NotEnoughGold, two.Status);
            Assert.AreEqual(1, two.Shortfall);
            Assert.AreEqual(10, three.Cost, "a 3-item recipe costs 10");
        }

        [Test]
        public void CountMakeable_CrusaderKit_MakesTheHonedSword()
        {
            var owned = new List<OwnedItem>
            {
                new OwnedItem(1, "item_iron_sword", false), new OwnedItem(2, "item_spiked_buckler", false), new OwnedItem(3, "item_whetstone", false),
            };

            Assert.AreEqual(1, FusionRules.CountMakeable(_catalog, _cfg, owned, 6));
            Assert.AreEqual(0, FusionRules.CountMakeable(_catalog, _cfg, owned, 5));
        }

        // ------------------------------------------------------------------ the door (Rules 4 and 5)
        private static DoorCard Card(TierKind t) => new DoorCard(t, default, null, null);

        [Test]
        public void FusionDoor_ReplacesTheLowestTierCard_LeftmostOnATie()
        {
            var row = new[] { Card(TierKind.Green), Card(TierKind.White), Card(TierKind.Blue) };
            var tie = new[] { Card(TierKind.White), Card(TierKind.Green), Card(TierKind.White) };
            RngStream stream = SeededRng.DeriveSideStream(5, "fusion");

            Assert.IsTrue(FusionDoor.Apply(_cfg, row, 3, 0, false, stream, force: true));
            Assert.IsTrue(FusionDoor.Apply(_cfg, tie, 3, 0, false, stream, force: true));

            Assert.IsTrue(row[1].IsFusion);
            Assert.IsFalse(row[0].IsFusion || row[2].IsFusion);
            Assert.IsTrue(tie[0].IsFusion);
            Assert.IsFalse(tie[1].IsFusion || tie[2].IsFusion);
        }

        [Test]
        public void FusionDoor_IneligibleRow_MakesNoDraw()
        {
            RngStream a = SeededRng.DeriveSideStream(9, "fusion");
            RngStream b = SeededRng.DeriveSideStream(9, "fusion");
            var row = new[] { Card(TierKind.White), Card(TierKind.Green), Card(TierKind.Blue) };

            Assert.IsFalse(FusionDoor.Apply(_cfg, row, 4, 1, previousRowHadFusion: true, a, force: true), "never two rows in a row");
            Assert.IsFalse(FusionDoor.Apply(_cfg, row, 2, 0, false, a, force: true), "not before room 3");
            Assert.IsFalse(FusionDoor.Apply(_cfg, row, 5, 2, false, a, force: true), "at most 2 per run");

            Assert.AreEqual(b.NextBounded(1000), a.NextBounded(1000), "an ineligible row leaves the fusion stream untouched");
        }

        /// <summary>Wins a room and steps through the claim and the shelf to the doors.</summary>
        private static void WinToDoors(RunSession run)
        {
            run.OnVictory();
            if (run.Phase == RunPhase.BuffClaim) Assert.AreEqual(ClaimStatus.Ok, run.ClaimBuff(0, 0, true).Status);
            if (run.Phase == RunPhase.Reward) run.SkipReward();
        }

        /// <summary>The first card that is neither a fusion door nor a Gold (cursed) door, else the first fight card.</summary>
        private static int FightCard(IReadOnlyList<DoorCard> row)
        {
            int any = -1;
            for (int i = 0; i < row.Count; i++)
            {
                if (row[i].IsFusion) continue;
                if (row[i].Tier != TierKind.Gold) return i;
                if (any < 0) any = i;
            }

            return any;
        }

        [Test]
        public void Runs_OfferOneOrTwoFusionDoors_FromRoom3_NeverInConsecutiveRows_DoorsStreamUnchanged()
        {
            for (ulong seed = 1; seed <= 300; seed++)
            {
                // Arrange
                var run = new RunSession(_cfg, _catalog, seed, 1);
                var fusionRows = new List<int>();

                // Act: never take a fusion door
                while (run.Phase != RunPhase.Won)
                {
                    WinToDoors(run);
                    if (run.Phase != RunPhase.DoorChoice) continue;
                    int fusionCards = 0;
                    foreach (DoorCard c in run.Row) if (c.IsFusion) fusionCards++;
                    Assert.LessOrEqual(fusionCards, 1, "a row never has two fusion doors");
                    if (fusionCards == 1) fusionRows.Add(run.Room + 1);
                    Assert.IsTrue(run.ChooseDoor(FightCard(run.Row)));
                }

                // Assert
                Assert.That(fusionRows.Count, Is.InRange(1, 2), "seed " + seed);
                Assert.GreaterOrEqual(fusionRows[0], 3, "seed " + seed);
                if (fusionRows.Count == 2) Assert.Greater(fusionRows[1] - fusionRows[0], 1, "seed " + seed);
                Assert.AreEqual(43, run.DoorDrawCalls, "System 17: 43 calls on doors per run, fusion or not");
                Assert.AreEqual(fusionRows.Count, run.FusionDoorsOffered);
            }
        }

        [Test]
        public void FusionRoom_ChooseFuseOnceLeave_RecordsTheRoomAndBuildsTheNextRow()
        {
            // Arrange: room 2 won with the debug policy, so the row to room 3 holds a fusion door
            var run = new RunSession(_cfg, _catalog, 11, 1) { ForceFusion = true };
            WinToDoors(run);
            run.ChooseDoor(FightCard(run.Row));
            WinToDoors(run);
            int fusion = -1;
            for (int i = 0; i < run.Row.Count; i++) if (run.Row[i].IsFusion) fusion = i;
            Assert.GreaterOrEqual(fusion, 0);

            // Act
            Assert.IsTrue(run.ChooseDoor(fusion));

            // Assert: no fight in the fusion room
            Assert.AreEqual(RunPhase.Fusion, run.Phase);
            Assert.AreEqual(3, run.Room);
            Assert.IsNull(run.CurrentEncounter);
            Assert.IsTrue(run.IsFusionRoom);
            Assert.AreEqual(FusionStatus.Ok, run.CanFuse);
            Assert.AreEqual(FusionStatus.UnknownRecipe, run.RecordFusion("recipe_nope"));
            Assert.AreEqual(FusionStatus.Ok, run.RecordFusion("recipe_honed_sword"));
            Assert.AreEqual(FusionStatus.AlreadyFused, run.RecordFusion("recipe_thornwall"));
            Assert.AreEqual(FusionStatus.AlreadyFused, run.CanFuse);

            Assert.IsTrue(run.LeaveFusion());
            Assert.AreEqual(RunPhase.DoorChoice, run.Phase);
            Assert.AreEqual(3, run.Row.Count);
            foreach (DoorCard c in run.Row) Assert.IsFalse(c.IsFusion, "never two rows in a row");
            RoomRecord last = run.History[run.History.Count - 1];
            Assert.IsTrue(last.Fusion);
            Assert.AreEqual(3, last.Room);
            Assert.AreEqual("recipe_honed_sword", last.RecipeMade);
            Assert.AreEqual(FusionStatus.NotFusionRoom, run.RecordFusion("recipe_thornwall"));
        }

        [Test]
        public void FusionRoom_AfterRoom8_LeadsToTheBoss()
        {
            // Arrange: a config whose fusion door may only lead to room 8
            string json = Regex.Replace(Data("run_mvp.json"), "\"fusion\"\\s*:\\s*\\{[^}]*\\}",
                "\"fusion\": { \"minRoom\": 8, \"chance\": 1000, \"guaranteeRoom\": 8, \"maxPerRun\": 1, \"cost2\": 6, \"cost3\": 10 }");
            RunConfig cfg = RunConfig.Load(json, _catalog, out IReadOnlyList<string> errors);
            Assert.IsNotNull(cfg, string.Join("\n", errors));
            var run = new RunSession(cfg, _catalog, 3, 1);
            while (run.Room < 7)
            {
                WinToDoors(run);
                run.ChooseDoor(FightCard(run.Row));
            }

            WinToDoors(run);
            int fusion = -1;
            for (int i = 0; i < run.Row.Count; i++) if (run.Row[i].IsFusion) fusion = i;
            Assert.GreaterOrEqual(fusion, 0);

            // Act
            run.ChooseDoor(fusion);
            Assert.AreEqual(8, run.Room);
            run.LeaveFusion();

            // Assert
            Assert.IsTrue(run.IsBossRoom);
            Assert.AreEqual(RunPhase.Planning, run.Phase);
            Assert.IsNotNull(run.CurrentEncounter);
        }

        // ------------------------------------------------------------------ the open book (Rule 11)
        [Test]
        public void PopupLines_IngredientNamesItsResult_FusedItemNamesItsIngredients()
        {
            // Arrange
            ItemCatalog ui = ItemCatalog.Load(Data("items_mvp.json"));
            ItemRecord sword = ui.Find("item_iron_sword_plus");
            ItemRecord honed = ui.Find("item_honed_sword");
            PopupContent a = ItemTooltipText.Build(sword, true, new List<SynergyModifier>());
            PopupContent b = ItemTooltipText.Build(honed, true, new List<SynergyModifier>());

            // Act
            ItemTooltipText.AddRecipeLines(a, sword, ui);
            ItemTooltipText.AddRecipeLines(b, honed, ui);

            // Assert
            Assert.IsTrue(a.Lines.Exists(l => l.Text == "Dùng để luyện: Kiếm Mài Bén (cùng Đá Mài)"), "a + item counts as its base");
            Assert.IsTrue(b.Lines.Exists(l => l.Text == "Luyện từ: Kiếm Sắt + Đá Mài"));
            StringAssert.StartsWith("Huyền thoại", b.Subtitle);
            Assert.IsTrue(b.Lines[b.Lines.Count - 1].Dim, "the recipe lines sit before the tag and flavour lines");
        }
    }
}
