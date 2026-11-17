using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Project05.CombatCore;
using Project05.UI.Motion;
using UnityEngine;

namespace Project05.Tests.EditMode
{
    /// <summary>
    /// The three-item start (design/quick-specs/balance-three-item-start.md) and the motion helpers
    /// (design/ux/feedback-motion.md B1).
    /// </summary>
    public sealed class StartAndMotionTests
    {
        private GameCatalog _catalog;

        private static string Data(string file) => File.ReadAllText(Path.Combine("Assets/_Project/Data", file));

        [SetUp]
        public void SetUp()
        {
            _catalog = GameCatalog.LoadFromJson(Data("items_mvp.json"), Data("heroes_mvp.json"), Data("monsters_mvp.json"), out IReadOnlyList<string> errors);
            Assert.IsNotNull(_catalog, string.Join("\n", errors));
            Assert.IsEmpty(errors, string.Join("\n", errors));
        }

        private BackpackArrangement PlaceKit(HeroDef hero)
        {
            var arr = new BackpackArrangement(_catalog, 1, new[] { hero.BackpackProfile }, new SequentialItemIdSource());
            foreach (StartingPlacement at in hero.StartingLayout)
            {
                Assert.IsTrue(arr.CreateInTray(at.ItemId, out long id).IsOk, at.ItemId);
                GridOpResult placed = arr.Place(id, ContainerKind.Hero0, at.X, at.Y, at.Rotation);
                Assert.IsTrue(placed.IsOk, $"{hero.Id}: {at.ItemId} at ({at.X}, {at.Y}) rot {at.Rotation}: {placed.Code}");
            }

            return arr;
        }

        [TestCase("hero_crusader")]
        [TestCase("hero_alchemist")]
        [TestCase("hero_ranger")]
        [TestCase("hero_cleric")]
        public void StartingKit_IsThreeItemsAtFreeAuthoredCells_WithTheCoreItem(string heroId)
        {
            // Arrange
            HeroDef hero = _catalog.FindHero(heroId);

            // Act
            BackpackArrangement arr = PlaceKit(hero);

            // Assert
            Assert.AreEqual(3, hero.StartingItems.Count);
            Assert.AreEqual(3, hero.StartingLayout.Count);
            Assert.IsTrue(new List<string>(hero.StartingItems).Contains(hero.SignatureItemId), "the core item is carried from the start");
            Assert.AreEqual(3, arr.CaptureView().HeroGrids[0].Items.Count);
        }

        [Test]
        public void CrusaderKit_WhetstoneTouchesTheSword_SoTheSwordHitsHarder()
        {
            // Arrange
            HeroDef hero = _catalog.FindHero("hero_crusader");
            BackpackArrangement arr = PlaceKit(hero);
            ArrangementGridView grid = arr.CaptureView().HeroGrids[0];

            // Act
            GridSynergyView synergy = AdjacencySynergyEvaluator.Evaluate(grid, _catalog);

            // Assert: the whetstone feeds the sword
            long sword = System.Linq.Enumerable.First(grid.Items, i => i.DefinitionId == "item_iron_sword").InstanceId;
            int links = 0;
            foreach (SynergyLink link in synergy.AllLinks)
            {
                if (link.SourceDefId == "item_whetstone" && link.TargetInstanceId == sword) links++;
            }

            Assert.AreEqual(1, links);
        }

        [TestCase(Ease.Linear)]
        [TestCase(Ease.OutQuad)]
        [TestCase(Ease.InQuad)]
        [TestCase(Ease.OutCubic)]
        [TestCase(Ease.InOutQuad)]
        [TestCase(Ease.OutBack)]
        public void Evaluate_StartsAtZero_EndsAtOne(Ease ease)
        {
            Assert.AreEqual(0f, UiMotion.Evaluate(ease, 0f), 1e-4f);
            Assert.AreEqual(1f, UiMotion.Evaluate(ease, 1f), 1e-4f);
        }

        [Test]
        public void Snap_RoundsToWholeCanvasPixels()
        {
            Assert.AreEqual(new Vector2(3f, -2f), UiMotion.Snap(new Vector2(2.6f, -2.4f)));
        }
    }
}
