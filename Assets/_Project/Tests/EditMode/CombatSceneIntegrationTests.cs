using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Project05.CombatCore;
using Project05.CombatScene;
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
            Assert.IsNotNull(controller, "Combat.unity must contain CombatSceneController.");

            controller.InitializeRuntime();
            Assert.IsNotNull(controller.Catalog);
            Assert.AreEqual("hero_crusader", controller.HeroDef.Id);
            Assert.AreEqual(CombatScenePhase.Planning, controller.Phase);
            Assert.IsNotNull(controller.Workbench);
            Assert.IsNotNull(controller.NextStagePanel);
            Assert.IsNotNull(controller.Playback);
            Assert.IsNotNull(controller.PostCombatPanel);

            // 1-hero HUD check: HeroSlot1 active, HeroSlot2 & HeroSlot3 hidden.
            CombatHudView hud = Object.FindFirstObjectByType<CombatHudView>();
            Assert.IsNotNull(hud);
            Assert.IsTrue(hud.transform.Find("HeroSlot1").gameObject.activeSelf);
            Assert.IsFalse(hud.transform.Find("HeroSlot2").gameObject.activeSelf);
            Assert.IsFalse(hud.transform.Find("HeroSlot3").gameObject.activeSelf);

            // Picker: switch to Chọn đồ tab and pick an item.
            controller.Workbench.SwitchLowerTab(true);
            Assert.IsTrue(controller.Workbench.IsPickerOpen);

            int beforeCount = controller.Arrangement.CaptureView().HeroGrids[0].Items.Count
                              + controller.Arrangement.CaptureView().Stash.Items.Count;
            Assert.IsTrue(controller.Workbench.TryPickCatalogItem("item_crimson_vial"));
            int afterPickCount = controller.Arrangement.CaptureView().HeroGrids[0].Items.Count
                                 + controller.Arrangement.CaptureView().Stash.Items.Count;
            Assert.AreEqual(beforeCount + 1, afterPickCount);

            // Find an item in Stash and test move/rotate + remove.
            IReadOnlyList<PlacedItemView> stashItems = controller.Arrangement.CaptureView().Stash.Items;
            Assert.GreaterOrEqual(stashItems.Count, 1);
            long stashItemId = stashItems[0].InstanceId;
            Assert.IsTrue(controller.Workbench.TryMoveOrRotateItem(stashItemId, ContainerKind.Stash, 6, 0, orientation: 1));
            Assert.IsTrue(controller.Workbench.TryRemoveItem(stashItemId));

            // Next-stage monster count selector: test immediate apply for 1..6.
            for (int c = 1; c <= 6; c++)
            {
                controller.ApplyMonsterCountImmediately(c);
                Assert.AreEqual(c, controller.Prepared.BoundEnemies.Count);
            }

            // Skip playback so StartFight completes synchronously in EditMode, then proceed to Next Stage with 5 monsters.
            int heroItemsBeforeFight = controller.Arrangement.CaptureView().HeroGrids[0].Items.Count;
            controller.Playback.HandleSpeedRequested(3); // Skip
            controller.StartFight();

            Assert.AreEqual(CombatScenePhase.PostCombat, controller.Phase);
            Assert.IsNotNull(controller.LastResult);
            Assert.AreEqual(CombatRunStatus.Completed, controller.LastResult.Status);

            controller.SetNextStageMonsterCount(5);
            controller.ProceedToNextStage();

            Assert.AreEqual(2, controller.CurrentStage);
            Assert.AreEqual(CombatScenePhase.Planning, controller.Phase);
            Assert.AreEqual(5, controller.Prepared.BoundEnemies.Count);
            Assert.AreEqual(heroItemsBeforeFight, controller.Arrangement.CaptureView().HeroGrids[0].Items.Count);
        }
    }
}
