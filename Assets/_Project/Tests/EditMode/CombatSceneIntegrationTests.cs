using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Project05.CombatCore;
using Project05.CombatScene;
using Project05.Items;
using Project05.UI.Hud;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project05.Tests.EditMode
{
    [TestFixture]
    public class CombatSceneIntegrationTests
    {
        private GameCatalog _catalog;
        private string _monstersJson;

        private static string ReadData(string name) => File.ReadAllText(Path.Combine("Assets/_Project/Data", name));

        [SetUp]
        public void SetUp()
        {
            _monstersJson = ReadData("monsters_mvp.json");
            _catalog = GameCatalog.LoadFromJson(
                ReadData("items_mvp.json"),
                ReadData("heroes_mvp.json"),
                _monstersJson,
                out IReadOnlyList<string> errors);
            Assert.AreEqual(0, errors.Count, string.Join("\n", errors));
        }

        [TearDown]
        public void TearDown()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        [Test]
        public void CombatHudMapper_OneHeroMode_PopulatesSlot0_HidesSlots1And2_AndMapsOneToSixEnemies()
        {
            HeroDef crusader = _catalog.FindHero("hero_crusader");
            var arr = new BackpackArrangement(_catalog, 1, new[] { crusader.BackpackProfile }, new SequentialItemIdSource());
            Assert.IsTrue(arr.CreateInTray("item_iron_sword", out long swordId).IsOk);
            Assert.IsTrue(arr.Place(swordId, ContainerKind.Hero0, 0, 0, 0).IsOk);

            var presetCatalog = new EncounterPresetCatalog(_monstersJson);
            for (int enemyCount = 1; enemyCount <= 6; enemyCount++)
            {
                List<EnemySpawnInput> spawns = presetCatalog.BuildSpawns(_catalog, enemyCount, stageNumber: 1);
                Assert.AreEqual(enemyCount, spawns.Count);

                ArrangementGridView grid = arr.CaptureView().HeroGrids[0];
                GridSynergyView synergy = AdjacencySynergyEvaluator.Evaluate(grid, _catalog);
                var setup = new EncounterSetup(
                    enemyCount,
                    1,
                    new[] { new HeroBaselineInput(0, crusader, 0, grid, synergy) },
                    spawns);

                Assert.IsTrue(CombatRunner.TryPrepare(_catalog, setup, new SeededRng(42UL), out PreparedEncounter prep, out string err), err);

                CombatHudData planning = CombatHudMapper.MapPlanning(_catalog, crusader, grid, synergy, prep, 1, 8, 120, 0);
                Assert.IsNotNull(planning.Heroes[0]);
                Assert.AreEqual(1, planning.Heroes[0].JoinNumeral);
                Assert.IsNull(planning.Heroes[1]);
                Assert.IsNull(planning.Heroes[2]);
                Assert.LessOrEqual(planning.Order.Count, 6);

                for (int s = 0; s < CombatHudData.EnemyCount; s++)
                {
                    Assert.AreEqual(s < enemyCount, planning.Enemies[s].Present, $"count={enemyCount} slot={s}");
                }
            }
        }

        [Test]
        public void EncounterPresetCatalog_BuildsValidSpawns_ForOneToSixMonstersAcrossStages()
        {
            var presetCatalog = new EncounterPresetCatalog(_monstersJson);
            for (int count = 1; count <= 6; count++)
            {
                List<EnemySpawnInput> stage1 = presetCatalog.BuildSpawns(_catalog, count, stageNumber: 1);
                List<EnemySpawnInput> stage3 = presetCatalog.BuildSpawns(_catalog, count, stageNumber: 3);

                Assert.AreEqual(count, stage1.Count);
                Assert.AreEqual(count, stage3.Count);
                for (int i = 0; i < count; i++)
                {
                    Assert.AreEqual(i, stage1[i].Slot);
                    Assert.IsNotNull(stage1[i].Definition);
                    Assert.Greater(stage3[i].HpPermille, stage1[i].HpPermille);
                    Assert.Greater(stage3[i].DamagePermille, stage1[i].DamagePermille);
                }
            }
        }

        [Test]
        public void CombatScene_InitializeAndFullLoop_SupportsPickerEquipRotateRemove_AndNextStageMonsterCount()
        {
            const string scenePath = "Assets/_Project/Scenes/Combat.unity";
            Assert.IsTrue(File.Exists(scenePath), "Combat.unity must exist; build via CombatSceneBuilder.");

            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid());

            CombatSceneController controller = Object.FindFirstObjectByType<CombatSceneController>();
            CombatScreenController screen = Object.FindFirstObjectByType<CombatScreenController>();
            ItemBoardController board = Object.FindFirstObjectByType<ItemBoardController>();
            ActionStripView strip = Object.FindFirstObjectByType<ActionStripView>();
            ItemPopupController popup = Object.FindFirstObjectByType<ItemPopupController>();
            CombatHudView hud = Object.FindFirstObjectByType<CombatHudView>();
            Assert.IsNotNull(controller, "Combat.unity must contain CombatSceneController.");
            Assert.IsNotNull(screen, "Combat.unity must contain CombatScreenController.");
            Assert.IsNotNull(board, "Combat.unity must contain ItemBoardController.");
            Assert.IsNotNull(strip, "Combat.unity must contain ActionStripView.");
            Assert.IsNotNull(hud, "Combat.unity must contain CombatHudView.");

            RectTransform safeFrame = GameObject.Find("SafeFrame").GetComponent<RectTransform>();
            var so = new UnityEditor.SerializedObject(controller);
            so.FindProperty("monsterTestSelector").boolValue = true;   // the old stage loop with the 1..6 selector
            so.ApplyModifiedPropertiesWithoutUndo();
            controller.InitializeIntegrated(board, hud, strip, popup, safeFrame, initialSpeedIndex: 0);

            Assert.IsNotNull(controller.Catalog);
            Assert.IsNull(controller.Run, "the test selector replaces the run");
            Assert.AreEqual("hero_crusader", controller.HeroDef.Id);
            Assert.AreEqual(CombatScenePhase.Planning, controller.Phase);
            Assert.IsNotNull(controller.NextStagePanel);
            Assert.IsNotNull(controller.Playback);
            Assert.IsNotNull(controller.PostCombatPanel);

            // 1-hero HUD check: HeroSlot1 active, HeroSlot2 & HeroSlot3 hidden.
            Assert.IsTrue(hud.transform.Find("HeroSlot1").gameObject.activeSelf);
            Assert.IsFalse(hud.transform.Find("HeroSlot2").gameObject.activeSelf);
            Assert.IsFalse(hud.transform.Find("HeroSlot3").gameObject.activeSelf);

            // Picker spawn via ItemBoardController: add item_crimson_vial to stash/grid.
            int beforeStash = board.ExportContainerItems("stash").Count;
            ItemRecord vial = popup.Catalog.Find("item_crimson_vial");
            Assert.IsNotNull(vial);
            Sprite vialPlate = hud.Sprites.plateLift[0];
            Assert.IsTrue(board.TrySpawnItem(vial, vialPlate, null, "stash", out ItemHoverTarget spawned));
            Assert.IsNotNull(spawned);
            Assert.AreEqual(beforeStash + 1, board.ExportContainerItems("stash").Count);

            // Next-stage monster count selector: test immediate apply for 1..6.
            for (int c = 1; c <= 6; c++)
            {
                controller.ApplyMonsterCountImmediately(c);
                Assert.AreEqual(c, controller.Prepared.BoundEnemies.Count);
            }

            // Skip playback so StartIntegratedFight completes synchronously in EditMode, then proceed to Next Stage with 5 monsters.
            int heroItemsBeforeFight = board.ExportContainerItems("grid").Count;
            controller.Playback.HandleSpeedRequested(3); // Skip
            Assert.IsTrue(controller.StartIntegratedFight(0));

            Assert.AreEqual(CombatScenePhase.PostCombat, controller.Phase);
            Assert.IsNotNull(controller.LastResult);
            Assert.AreEqual(CombatRunStatus.Completed, controller.LastResult.Status);
            Assert.Greater(controller.LastResult.Trace.Events.Count, 4, "Real combat trace must have full event steps, not 3 fixture images.");

            controller.SetNextStageMonsterCount(5);
            controller.ProceedToNextStage();

            Assert.AreEqual(2, controller.CurrentStage);
            Assert.AreEqual(CombatScenePhase.Planning, controller.Phase);
            Assert.AreEqual(5, controller.Prepared.BoundEnemies.Count);
            Assert.AreEqual(heroItemsBeforeFight, board.ExportContainerItems("grid").Count);
        }

        [Test]
        public void CombatScene_RunFlow_WinRewardDoor_EntersRoom2()
        {
            // Arrange
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/Combat.unity", OpenSceneMode.Single);
            CombatSceneController controller = Object.FindFirstObjectByType<CombatSceneController>();
            ItemBoardController board = Object.FindFirstObjectByType<ItemBoardController>();
            ActionStripView strip = Object.FindFirstObjectByType<ActionStripView>();
            ItemPopupController popup = Object.FindFirstObjectByType<ItemPopupController>();
            CombatHudView hud = Object.FindFirstObjectByType<CombatHudView>();
            RectTransform safeFrame = GameObject.Find("SafeFrame").GetComponent<RectTransform>();
            controller.InitializeIntegrated(board, hud, strip, popup, safeFrame, initialSpeedIndex: 0);
            Assert.IsNotNull(controller.Run, "run_mvp.json must load");
            Assert.IsNull(controller.NextStagePanel, "the run replaces the monster test selector");
            Assert.AreEqual(1, controller.Run.Room);
            Assert.IsNotNull(hud.transform.Find("Corridor"));
            Assert.AreEqual(controller.Run.CurrentEncounter.Roster.Count, controller.Prepared.BoundEnemies.Count);
            Assert.AreEqual(5, controller.Currency, "a run starts with 5 gold (item-upgrades.md)");

            // the run starts with the full-screen hero pick (hero-select.md); nothing can start before it
            Assert.IsTrue(controller.HeroSelect.IsVisible);
            Assert.AreEqual(RunPanelView.Step.Hidden, controller.RunPanel.Current);
            Assert.IsFalse(controller.CanStartFight);
            Assert.IsFalse(controller.StartIntegratedFight(0));
            controller.HeroSelect.Select(1);   // the Plague Alchemist
            controller.HeroSelect.Confirm();
            Assert.IsFalse(controller.HeroSelect.IsVisible);
            Assert.AreEqual("hero_alchemist", controller.HeroDef.Id);
            Assert.IsTrue(board.ExportContainerItems("grid").Exists(i => i.Record.id == "item_plague_catalyst"), "the core is in the grid");
            Assert.AreEqual(3, board.ExportContainerItems("grid").Count, "a hero starts with exactly 3 items (balance-three-item-start.md)");
            Assert.AreEqual(0, board.ExportContainerItems("stash").Count, "the stash starts empty");
            Assert.IsTrue(controller.CanStartFight);

            // the forge quotes from gold and the catalog (item-upgrades.md)
            ForgeQuote vial = board.QuoteForge(popup.Catalog.Find("item_venom_vial"));
            Assert.IsTrue(vial.Ok, vial.Reason);
            Assert.AreEqual(4, vial.Cost);
            Assert.AreEqual("item_venom_vial_plus", vial.Upgrade.id);
            StringAssert.Contains("Độc 3>4", vial.Preview);
            ForgeQuote core = board.QuoteForge(popup.Catalog.Find("item_plague_catalyst"));
            Assert.IsFalse(core.Ok);
            Assert.AreEqual("Thiếu 9 vàng", core.Reason);
            Assert.AreEqual("Đã rèn +", board.QuoteForge(popup.Catalog.Find("item_venom_vial_plus")).Reason);

            // Act: fight room 1 with playback skipped
            controller.Playback.HandleSpeedRequested(3);
            Assert.IsTrue(controller.StartIntegratedFight(0));
            if (controller.LastResult.Outcome != CombatOutcome.Victory)
            {
                Assert.AreEqual(Project05.CombatCore.Run.RunPhase.Lost, controller.Run.Phase);
                Assert.Inconclusive("room 1 was lost with this seed");
            }

            Assert.AreEqual(5 + 2, controller.Currency, "room 1 pays the White door's 2 gold");

            // Assert: room 1 has no buff, so the item shelf comes first
            Assert.AreEqual(CombatScenePhase.RunReward, controller.Phase);
            Assert.IsFalse(controller.CanStartFight, "no Start during the reward steps");
            Assert.AreEqual(RunPanelView.Step.Reward, controller.RunPanel.Current);
            int stash = board.ExportContainerItems("stash").Count;
            controller.RunPanel.Command(0);
            Assert.AreEqual(stash + 1, board.ExportContainerItems("stash").Count, "the taken item goes to the stash");
            Assert.AreEqual(RunPanelView.Step.Doors, controller.RunPanel.Current);

            controller.RunPanel.Command(1);
            Assert.AreEqual(CombatScenePhase.Planning, controller.Phase);
            Assert.AreEqual(2, controller.Run.Room);
            Assert.AreEqual(controller.Run.CurrentEncounter.Roster.Count, controller.Prepared.BoundEnemies.Count);
            Assert.IsFalse(controller.RunPanel.IsVisible);
            Assert.IsTrue(controller.CanStartFight);
        }

        [Test]
        public void CombatScene_FusionRoom_FusesTheKitAndLeaves()
        {
            // Arrange: a Crusader run with the fusion debug policy (item-fusion.md), so the row to room 3 holds the door
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/Combat.unity", OpenSceneMode.Single);
            CombatSceneController controller = Object.FindFirstObjectByType<CombatSceneController>();
            ItemBoardController board = Object.FindFirstObjectByType<ItemBoardController>();
            ActionStripView strip = Object.FindFirstObjectByType<ActionStripView>();
            ItemPopupController popup = Object.FindFirstObjectByType<ItemPopupController>();
            CombatHudView hud = Object.FindFirstObjectByType<CombatHudView>();
            RectTransform safeFrame = GameObject.Find("SafeFrame").GetComponent<RectTransform>();
            controller.InitializeIntegrated(board, hud, strip, popup, safeFrame, initialSpeedIndex: 0);
            controller.Run.ForceFusion = true;
            controller.HeroSelect.Select(0);
            controller.HeroSelect.Confirm();
            Assert.AreEqual("hero_crusader", controller.HeroDef.Id);
            for (int room = 1; room <= 2; room++)
            {
                controller.Playback.HandleSpeedRequested(3);   // room entry resets the speed: skip again
                Assert.IsTrue(controller.StartIntegratedFight(0));
                if (controller.LastResult.Outcome != CombatOutcome.Victory) Assert.Inconclusive("room " + room + " was lost with this seed");
                if (controller.RunPanel.Current == RunPanelView.Step.Claim) controller.RunPanel.Command(0);
                Assert.AreEqual(RunPanelView.Step.Reward, controller.RunPanel.Current);
                controller.RunPanel.CommandSkip();
                Assert.AreEqual(RunPanelView.Step.Doors, controller.RunPanel.Current);
                int pick = -1, fusionCard = -1;
                for (int i = 0; i < controller.Run.Row.Count; i++)
                {
                    if (controller.Run.Row[i].IsFusion) fusionCard = i;
                    else if (pick < 0 && controller.Run.Row[i].Tier != Project05.CombatCore.TierKind.Gold) pick = i;
                }

                if (room == 1)
                {
                    Assert.AreEqual(-1, fusionCard, "no fusion door before room 3");
                    controller.RunPanel.Command(pick);
                    continue;
                }

                Assert.GreaterOrEqual(fusionCard, 0, "the debug policy puts a fusion door in the row to room 3");
                int gold = controller.Currency;
                Assert.GreaterOrEqual(gold, 6);

                // Act: enter the fusion room
                controller.RunPanel.Command(fusionCard);
                Assert.AreEqual(Project05.CombatCore.Run.RunPhase.Fusion, controller.Run.Phase);
                Assert.AreEqual(3, controller.Run.Room);
                Assert.AreEqual(RunPanelView.Step.Fusion, controller.RunPanel.Current);
                Assert.AreEqual(CombatScenePhase.RunReward, controller.Phase);
                Assert.IsFalse(controller.CanStartFight, "a fusion room has no fight");
                Assert.AreEqual(gold, controller.Currency, "a fusion room pays nothing");
                Assert.AreEqual(12, controller.RunPanel.ShownRows.Count);
                int honed = controller.RunPanel.ShownRows.ToList().FindIndex(r => r.Recipe.Id == "recipe_honed_sword");
                Assert.IsTrue(controller.RunPanel.ShownRows[honed].Makeable, "the Crusader's kit holds both ingredients");

                // fuse the Honed Sword
                controller.RunPanel.Command(honed);

                // Assert
                Assert.AreEqual(gold - 6, controller.Currency);
                List<BoardItem> all = board.ExportContainerItems("grid");
                all.AddRange(board.ExportContainerItems("stash"));
                Assert.IsFalse(all.Exists(i => i.Record.id == "item_iron_sword" || i.Record.id == "item_whetstone"), "the ingredients are consumed");
                Assert.IsTrue(board.ExportContainerItems("stash").Exists(i => i.Record.id == "item_honed_sword"), "the result goes to the stash");
                Assert.IsTrue(controller.Run.FusedThisRoom);
                Assert.IsTrue(controller.RunPanel.FusionLocked);
                controller.RunPanel.Command(honed);
                Assert.AreEqual(gold - 6, controller.Currency, "one fusion per visit");

                // leave: the row to room 4 (never a fusion door right after one)
                controller.RunPanel.CommandLeave();
                Assert.AreEqual(RunPanelView.Step.Doors, controller.RunPanel.Current);
                Assert.AreEqual(Project05.CombatCore.Run.RunPhase.DoorChoice, controller.Run.Phase);
                foreach (Project05.CombatCore.Run.DoorCard c in controller.Run.Row) Assert.IsFalse(c.IsFusion);
            }
        }

        [Test]
        public void ItemBoard_OpenCellsAndPinnedItem_FollowTheDoorAndCurseRules()
        {
            // Arrange
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/Combat.unity", OpenSceneMode.Single);
            ItemBoardController board = Object.FindFirstObjectByType<ItemBoardController>();
            ItemPopupController popup = Object.FindFirstObjectByType<ItemPopupController>();
            CombatHudView hud = Object.FindFirstObjectByType<CombatHudView>();
            Assert.AreEqual(16, board.HeroCellsOpen);

            // Act / Assert: one cell opens by growing the square to 5 x 5 with the rest of its ring locked
            Assert.AreEqual(1, board.OpenHeroCells(1));
            Assert.AreEqual(17, board.HeroCellsOpen);
            Assert.AreEqual(49 - 17, board.LockedHeroCellsLeft);

            // a pinned item cannot be picked up
            ItemRecord anchor = popup.Catalog.Find("item_cursed_anchor");
            Assert.IsNotNull(anchor);
            bool refused = false;
            board.PinnedRefused += () => refused = true;
            if (!board.CanPlaceInHeroGrid(anchor)) Assert.Inconclusive("the start grid has no 2 free cells");
            Assert.IsTrue(board.TrySpawnPinned(anchor, hud.Sprites.plateLift[0], null, null, null, out ItemHoverTarget pinned));
            board.Pickup(pinned, false);
            Assert.IsTrue(refused);
            Assert.IsNull(board.Held);
        }
    }
}
