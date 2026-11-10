using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Project05.Core;
using Project05.Data;
using Project05.Backpack;
using Project05.Progression;
using Project05.UI;

namespace Project05.Tests.EditMode
{
    [TestFixture]
    public class ProgressionAndPartyTests
    {
        [Test]
        public void RunManager_StartNewRun_StartsWithExactly1Hero_AndCanRecruitUpTo3()
        {
            RunManager run = new RunManager();
            run.StartNewRun();

            // Must start with 1 character ("khởi đầu với 1")
            Assert.AreEqual(1, run.Party.Count, "New run must start with exactly 1 Hero");
            Assert.AreEqual(PartyPosition.Frontline, run.Party[0].Position);
            Assert.IsNotNull(run.SharedStash);
            Assert.Greater(run.SharedStash.PlacedItems.Count, 0, "Shared Stash should have starter items");

            // Recruit 2nd Hero (Midline)
            HeroDefinitionSO alchemist = DefaultGameCatalog.GetHeroById("hero_alchemist");
            Assert.IsTrue(run.TryRecruitHero(alchemist, out _));
            Assert.AreEqual(2, run.Party.Count);
            Assert.AreEqual(PartyPosition.Midline, run.Party[1].Position);

            // Recruit 3rd Hero (Backline)
            HeroDefinitionSO ranger = DefaultGameCatalog.GetHeroById("hero_ranger");
            Assert.IsTrue(run.TryRecruitHero(ranger, out _));
            Assert.AreEqual(3, run.Party.Count);
            Assert.AreEqual(PartyPosition.Backline, run.Party[2].Position);

            // Recruiting when already at max 3 upgrades existing party without exceeding 3
            HeroDefinitionSO cleric = DefaultGameCatalog.GetHeroById("hero_cleric");
            int prevMaxHp = run.Party[0].MaxHP;
            Assert.IsTrue(run.TryRecruitHero(cleric, out _));
            Assert.AreEqual(3, run.Party.Count, "Party size must never exceed MaxPartySize (3)");
            Assert.Greater(run.Party[0].MaxHP, prevMaxHp, "Full party recruitment should train existing allies");
        }

        [Test]
        public void RunManager_SwitchingSelectedHero_ChangesPersonalBackpack_WhileSharedStashRemainsConstant()
        {
            RunManager run = new RunManager();
            run.StartNewRun();
            run.TryRecruitHero(DefaultGameCatalog.GetHeroById("hero_alchemist"), out _);

            BackpackGrid stashBefore = run.SharedStash;
            int stashCountBefore = stashBefore.PlacedItems.Count;

            run.SelectHero(0);
            BackpackGrid hero0Bag = run.SelectedHero.PersonalBackpack;

            run.SelectHero(1);
            BackpackGrid hero1Bag = run.SelectedHero.PersonalBackpack;

            Assert.AreNotSame(hero0Bag, hero1Bag, "Each Hero must own an independent Personal BackpackGrid");
            Assert.AreSame(stashBefore, run.SharedStash, "Shared Stash must remain the exact same grid when switching heroes");
            Assert.AreEqual(stashCountBefore, run.SharedStash.PlacedItems.Count);
        }

        [Test]
        public void DoorGenerator_Generates3Doors_WithRecruitOptionWhenPartyUnder3_AndTieredBuffs()
        {
            System.Random rng = new System.Random(42);
            List<DoorCard> doors = DoorGenerator.GenerateThreeDoors(currentFloor: 1, currentPartyCount: 1, maxPartySize: 3, rng: rng);

            Assert.AreEqual(3, doors.Count);
            // When party size is 1 (< 3), Door 0 offers Ally Recruitment
            Assert.AreEqual(RoomType.Recruit, doors[0].NextRoomType);

            // Higher door slot has higher or equal tier and higher difficulty multiplier
            Assert.LessOrEqual((int)doors[0].Tier, (int)doors[1].Tier);
            Assert.LessOrEqual((int)doors[1].Tier, (int)doors[2].Tier);
            Assert.Less(doors[0].DifficultyMultiplier, doors[2].DifficultyMultiplier);

            for (int i = 0; i < 3; i++)
            {
                Assert.IsNotNull(doors[i].BuffReward);
                Assert.AreEqual(doors[i].Tier, doors[i].BuffReward.Tier);
            }
        }

        [Test]
        public void RunManager_SelectDoor_ImmediatelyAppliesBuffAndEntersDesignatedRoom()
        {
            RunManager run = new RunManager();
            run.StartNewRun();
            run.PrepareDoorSelection();

            Assert.AreEqual(RunState.DoorSelection, run.State);
            Assert.AreEqual(3, run.CurrentDoorChoices.Count);

            DoorCard chosenDoor = run.CurrentDoorChoices[1];
            RoomType expectedRoom = chosenDoor.NextRoomType;
            int buffCountBefore = run.ActiveBuffs.Count;

            Assert.IsTrue(run.SelectDoor(1));
            Assert.AreEqual(buffCountBefore + 1, run.ActiveBuffs.Count);
            Assert.AreEqual(expectedRoom, run.CurrentRoomType);
        }

        [Test]
        public void RunManager_InvalidTransferToLockedSlot_KeepsItemInSharedStash()
        {
            RunManager run = new RunManager();
            run.StartNewRun();

            ItemData stashItem = run.SharedStash.PlacedItems[0];
            int countBefore = run.SharedStash.PlacedItems.Count;

            // Slot (4, 3) on starter hero is initially locked (only 4x3 unlocked on 5x4 grid)
            Assert.IsFalse(run.SelectedHero.PersonalBackpack.IsSlotUnlocked(4, 3));
            bool transferred = run.TryTransferOrMoveItem(
                stashItem,
                run.SharedStash,
                run.SelectedHero.PersonalBackpack,
                4,
                3,
                0);

            Assert.IsFalse(transferred);
            Assert.AreEqual(countBefore, run.SharedStash.PlacedItems.Count, "Failed transfer must preserve item in Shared Stash");
        }

        [Test]
        public void RunManager_FullCombatLoop_TransitionsToDoorSelectionOnVictory()
        {
            RunManager run = new RunManager();
            run.StartNewRun();
            Assert.AreEqual(RunState.Combat, run.State);

            int safetySteps = 0;
            while (run.State == RunState.Combat && safetySteps < 100)
            {
                run.Combat.StepNextTurnInstant();
                safetySteps++;
            }

            Assert.AreEqual(RunState.DoorSelection, run.State, "Winning Floor 1 combat must transition RunManager into 3-Door Selection");
            Assert.AreEqual(3, run.CurrentDoorChoices.Count);
        }

        [Test]
        public void ThreeColumnLayoutManager_Builds3ColumnsAndSwitchesHeroBackpackAndRightStage()
        {
            GameObject rootGo = new GameObject("TestUIRoot");
            try
            {
                ThreeColumnLayoutManager ui = rootGo.AddComponent<ThreeColumnLayoutManager>();
                ui.InitializeImmediate();

                // Verify 1 starting hero
                Assert.AreEqual(1, ui.Run.Party.Count);

                // Verify 2 BackpackGridViews exist in Column 1 (Top = Hero Backpack, Bottom = Shared Stash)
                BackpackGridView[] gridViews = rootGo.GetComponentsInChildren<BackpackGridView>(true);
                Assert.AreEqual(2, gridViews.Length);
                Assert.AreSame(ui.Run.Party[0].PersonalBackpack, gridViews[0].BoundGrid);
                Assert.AreSame(ui.Run.SharedStash, gridViews[1].BoundGrid);

                // Recruit 2nd hero and switch selection -> Top grid changes to Hero 1, Bottom grid stays SharedStash
                ui.Run.TryRecruitHero(DefaultGameCatalog.GetHeroById("hero_alchemist"), out _);
                ui.Run.SelectHero(1);
                Assert.AreSame(ui.Run.Party[1].PersonalBackpack, gridViews[0].BoundGrid);
                Assert.AreSame(ui.Run.SharedStash, gridViews[1].BoundGrid);

                // Test rotating item and transitioning right column through all room states
                ui.RotateHeldOrHoveredItem();
                ui.Run.EnterRoom(RoomType.Shop, 1.0f);
                Assert.AreEqual(RunState.Shop, ui.Run.State);

                ui.Run.EnterRoom(RoomType.Event, 1.0f);
                Assert.AreEqual(RunState.Event, ui.Run.State);

                ui.Run.PrepareDoorSelection();
                Assert.AreEqual(RunState.DoorSelection, ui.Run.State);
            }
            finally
            {
                Object.DestroyImmediate(rootGo);
            }
        }

        [Test]
        public void RunManager_NotificationsPreservedAcrossTransitions_AndFallenAllyRevivesAfterVictory()
        {
            RunManager run = new RunManager();
            run.StartNewRun();

            // Recruit 2nd hero and verify recruitment message is preserved in LatestBannerMessage
            HeroDefinitionSO alchemist = DefaultGameCatalog.GetHeroById("hero_alchemist");
            Assert.IsTrue(run.TryRecruitHero(alchemist, out _));
            StringAssert.Contains(alchemist.HeroName, run.LatestBannerMessage, "Recruitment outcome must not be overwritten by PrepareDoorSelection");

            // Select Door 0 and verify door buff reward message is preserved in LatestBannerMessage
            DoorCard door0 = run.CurrentDoorChoices[0];
            Assert.IsTrue(run.SelectDoor(0));
            StringAssert.Contains(door0.BuffReward.BuffName, run.LatestBannerMessage, "Door buff reward must not be overwritten by EnterRoom");

            // Enter combat, knock out Hero 0 (0 HP), and let Hero 1 win the battle
            run.EnterRoom(RoomType.Combat, 1.0f);
            run.Party[0].CurrentHP = 0;
            Assert.IsFalse(run.Party[0].IsAlive);

            // Defeat all monsters except 1 HP on the last monster, then step turn
            for (int i = 0; i < run.CurrentMonsters.Count; i++)
            {
                run.CurrentMonsters[i].CurrentHP = 1;
                run.CurrentMonsters[i].Shield = 0;
            }

            int safety = 0;
            while (run.State == RunState.Combat && safety < 50)
            {
                run.Combat.StepNextTurnInstant();
                safety++;
            }

            Assert.AreEqual(RunState.DoorSelection, run.State);
            Assert.IsTrue(run.Party[0].IsAlive, "Knocked-out ally must revive at 20% MaxHP after surviving party wins combat");
            Assert.Greater(run.Party[0].CurrentHP, 0);
            StringAssert.Contains("Chiến thắng!", run.LatestBannerMessage, "Victory reward message must be preserved in LatestBannerMessage");
        }

        [Test]
        public void DoorGenerator_HighTierPurpleGoldRedDoors_AlwaysLeadToEliteOrBoss_NeverSafeRooms()
        {
            System.Random rng = new System.Random(1337);
            for (int floor = 1; floor <= 15; floor++)
            {
                for (int partyCount = 1; partyCount <= 3; partyCount++)
                {
                    List<DoorCard> doors = DoorGenerator.GenerateThreeDoors(floor, partyCount, 3, rng);
                    for (int i = 0; i < doors.Count; i++)
                    {
                        DoorCard d = doors[i];
                        if (d.Tier >= CardTier.Purple)
                        {
                            Assert.IsTrue(
                                d.NextRoomType == RoomType.Elite || d.NextRoomType == RoomType.Boss,
                                $"High-tier door ({d.Tier}) on floor {floor} must lead to Elite or Boss, but was {d.NextRoomType}");
                        }
                    }
                }
            }
        }

        [Test]
        public void ShopManager_TryReroll_AndRunManager_TrySellItem_WorkCorrectly()
        {
            RunManager run = new RunManager();
            run.StartNewRun();
            run.EnterRoom(RoomType.Shop, 1.0f);

            int goldBefore = run.Gold;
            Assert.IsTrue(run.Shop.TryReroll(run.CurrentFloor, ref goldBefore, out string rerollMsg));
            run.Gold = goldBefore;
            Assert.AreEqual(35 - ShopManager.RerollCost, run.Gold);
            Assert.AreEqual(4, run.Shop.CurrentOffers.Count);
            Assert.IsNotEmpty(rerollMsg);

            // Sell an item from SharedStash
            ItemData itemToSell = run.SharedStash.PlacedItems[0];
            int stashCountBefore = run.SharedStash.PlacedItems.Count;
            int goldBeforeSell = run.Gold;

            Assert.IsTrue(run.TrySellItem(itemToSell, run.SharedStash, out string sellMsg));
            Assert.AreEqual(stashCountBefore - 1, run.SharedStash.PlacedItems.Count);
            Assert.Greater(run.Gold, goldBeforeSell);
            StringAssert.Contains(itemToSell.Definition.DisplayName, sellMsg);
        }

        [Test]
        public void ThreeColumnLayoutManager_SmartAnchorSnapping_AndForegroundStarOverlay_WorkCorrectly()
        {
            GameObject rootGo = new GameObject("TestSnapUIRoot");
            try
            {
                ThreeColumnLayoutManager ui = rootGo.AddComponent<ThreeColumnLayoutManager>();
                ui.InitializeImmediate();

                BackpackGrid heroGrid = ui.Run.SelectedHero.PersonalBackpack; // 4x3 unlocked on 5x4 grid
                heroGrid.ClearAllItems();

                ItemData sword1x2 = new ItemData(DefaultGameCatalog.GetItemById("item_iron_sword")); // 1x2 vertical
                // Clicking bottom unlocked row y=2 at x=0 cannot anchor 1x2 at (0,2) since y=3 is locked,
                // so ResolveBestPlacementAnchor must snap anchor to (0,1) which covers (0,1) and (0,2)!
                GridCoord snapped = ThreeColumnLayoutManager.ResolveBestPlacementAnchor(heroGrid, sword1x2, 0, 2, 0);
                Assert.AreEqual(new GridCoord(0, 1), snapped);
                Assert.IsTrue(heroGrid.CanPlaceItemAt(sword1x2, snapped.X, snapped.Y, 0));

                // Locked slot (4, 3) must NOT snap
                GridCoord lockedCoord = ThreeColumnLayoutManager.ResolveBestPlacementAnchor(heroGrid, sword1x2, 4, 3, 0);
                Assert.AreEqual(new GridCoord(4, 3), lockedCoord);
                Assert.IsFalse(heroGrid.CanPlaceItemAt(sword1x2, lockedCoord.X, lockedCoord.Y, 0));
            }
            finally
            {
                Object.DestroyImmediate(rootGo);
            }
        }
    }
}
