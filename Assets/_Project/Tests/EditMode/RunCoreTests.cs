using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Project05.CombatCore;
using Project05.CombatCore.Run;

namespace Project05.Tests.EditMode
{
    /// <summary>
    /// Doors, buffs, curses and the run flow (Systems 5, 16, 17, 18, 19; engine-free core in CombatCore/Run).
    /// Seeded and deterministic.
    /// </summary>
    [TestFixture]
    public class RunCoreTests
    {
        private GameCatalog _catalog;
        private RunConfig _cfg;

        private static string Data(string file) => File.ReadAllText(Path.Combine("Assets/_Project/Data", file));

        [SetUp]
        public void SetUp()
        {
            _catalog = GameCatalog.LoadFromJson(Data("items_mvp.json"), Data("heroes_mvp.json"), Data("monsters_mvp.json"), out IReadOnlyList<string> catErrors);
            Assert.AreEqual(0, catErrors.Count, string.Join("\n", catErrors));
            _cfg = RunConfig.Load(Data("run_mvp.json"), _catalog, out IReadOnlyList<string> errors);
            Assert.IsNotNull(_cfg, string.Join("\n", errors));
        }

        [Test]
        public void Config_BadTierRow_FailsTheLoad()
        {
            // Arrange: lower the room-2 row's Green weight from 400 to 300
            string json = Data("run_mvp.json");
            int at = json.IndexOf("\"room\": 2");
            Assert.Greater(at, 0);
            int w = json.IndexOf("400", at);
            json = json.Substring(0, w) + "300" + json.Substring(w + 3);
            RunConfig bad = RunConfig.Load(json, _catalog, out IReadOnlyList<string> errors);
            Assert.IsNull(bad);
            StringAssert.Contains("room 2 sums to 900", string.Join("\n", errors));
        }

        [Test]
        public void Row_Masks_HoldOverManySeeds()
        {
            for (ulong seed = 1; seed <= 400; seed++)
            {
                var rng = new SeededRng(seed);
                RngStream doors = rng.GetStream("doors");
                for (int room = 2; room <= 8; room++)
                {
                    DoorCard[] row = DoorGenerator.BuildRow(_cfg, _catalog, room, 0, 2, doors);
                    Assert.AreEqual(3, row.Length);
                    foreach (DoorCard c in row)
                    {
                        Assert.GreaterOrEqual(room, _cfg.MinRoomFor(c.Tier), $"{c.Tier} before its first room");
                        Assert.AreEqual(c.Tier, c.Encounter.Tier);
                        Assert.AreEqual(c.Tier == TierKind.Gold, c.Curse != null, "only Gold carries a curse");
                    }

                    if (room >= _cfg.MinDistinctRoom)
                    {
                        Assert.IsFalse(row[0].Tier == row[1].Tier && row[1].Tier == row[2].Tier, $"three identical cards at room {room}");
                    }

                    if (room >= _cfg.SafeCardRoom)
                    {
                        bool safe = false;
                        foreach (DoorCard c in row) safe |= c.Tier <= TierKind.Green;
                        Assert.IsTrue(safe, $"no White or Green card at room {room}");
                    }
                }
            }
        }

        [Test]
        public void Row_GoldMaskedAtCurseCap()
        {
            for (ulong seed = 1; seed <= 300; seed++)
            {
                RngStream doors = new SeededRng(seed).GetStream("doors");
                foreach (DoorCard c in DoorGenerator.BuildRow(_cfg, _catalog, 8, 2, 2, doors))
                {
                    Assert.AreNotEqual(TierKind.Gold, c.Tier);
                }
            }
        }

        [Test]
        public void Row_IsDeterministicFromTheSeed()
        {
            DoorCard[] a = DoorGenerator.BuildRow(_cfg, _catalog, 7, 0, 2, new SeededRng(77UL).GetStream("doors"));
            DoorCard[] b = DoorGenerator.BuildRow(_cfg, _catalog, 7, 0, 2, new SeededRng(77UL).GetStream("doors"));
            for (int i = 0; i < 3; i++)
            {
                Assert.AreEqual(a[i].Tier, b[i].Tier);
                Assert.AreEqual(a[i].Encounter.TemplateId, b[i].Encounter.TemplateId);
            }
        }

        [Test]
        public void Multipliers_FollowRoomAndDoorTier()
        {
            Assert.AreEqual((1000, 1000), EncounterBuilder.Multipliers(_cfg, 1, TierKind.White, false));
            Assert.AreEqual((1240, 1160), EncounterBuilder.Multipliers(_cfg, 5, TierKind.White, false));
            Assert.AreEqual((1340, 1210), EncounterBuilder.Multipliers(_cfg, 5, TierKind.Blue, false));
            Assert.AreEqual((1720, 1380), EncounterBuilder.Multipliers(_cfg, 8, TierKind.Gold, false));
            Assert.AreEqual((1000, 1000), EncounterBuilder.Multipliers(_cfg, 9, TierKind.Purple, true));
        }

        [Test]
        public void Danger_CountsThresholds()
        {
            Assert.AreEqual(1, EncounterBuilder.DangerOf(_cfg, 19));
            Assert.AreEqual(2, EncounterBuilder.DangerOf(_cfg, 20));
            Assert.AreEqual(4, EncounterBuilder.DangerOf(_cfg, 70));
            Assert.AreEqual(5, EncounterBuilder.DangerOf(_cfg, 500));
        }

        [Test]
        public void FullRun_MakesFortyThreeDoorCalls_AndReachesTheBoss()
        {
            var run = new RunSession(_cfg, _catalog, 2026UL, 1);
            Assert.AreEqual(1, run.Room);
            Assert.IsNull(run.CurrentDoor);
            for (int room = 1; room <= 8; room++)
            {
                Assert.AreEqual(RunPhase.Planning, run.Phase);
                run.OnVictory();
                if (run.Phase == RunPhase.BuffClaim) Assert.AreEqual(ClaimStatus.Ok, run.ClaimBuff(0, 10, true).Status);
                Assert.AreEqual(RunPhase.Reward, run.Phase);
                Assert.IsTrue(run.SkipReward());
                if (room < 8)
                {
                    Assert.AreEqual(RunPhase.DoorChoice, run.Phase);
                    Assert.IsTrue(run.ChooseDoor(0));
                    Assert.AreEqual(room + 1, run.Room);
                }
            }

            Assert.IsTrue(run.IsBossRoom);
            Assert.IsTrue(run.CurrentEncounter.IsBoss);
            Assert.AreEqual(43, run.DoorDrawCalls);
            run.OnVictory();
            Assert.AreEqual(RunPhase.Won, run.Phase);
        }

        [Test]
        public void Defeat_EndsTheRun()
        {
            var run = new RunSession(_cfg, _catalog, 5UL, 1);
            run.OnDefeat();
            Assert.AreEqual(RunPhase.Lost, run.Phase);
            run.OnVictory();
            Assert.AreEqual(RunPhase.Lost, run.Phase);
        }

        [Test]
        public void Room1_HasNoBuffClaim()
        {
            var run = new RunSession(_cfg, _catalog, 9UL, 1);
            run.OnVictory();
            Assert.AreEqual(RunPhase.Reward, run.Phase);
            Assert.AreEqual(3, run.Offer.Count);
            Assert.AreEqual(3, run.Row.Count);
        }

        [Test]
        public void Buff_GrantsHpAndCells_WithinCaps()
        {
            var run = new RunSession(_cfg, _catalog, 11UL, 1) { ForceGold = true };
            int expectedHp = 0, expectedCells = 0;
            for (int room = 1; room <= 8; room++)
            {
                run.OnVictory();
                if (run.Phase == RunPhase.BuffClaim)
                {
                    DoorBuff buff = run.PendingBuff;
                    ClaimResult r = run.ClaimBuff(0, 10, true);
                    Assert.AreEqual(ClaimStatus.Ok, r.Status);
                    Assert.AreEqual(buff.Hp, r.HpGranted);
                    expectedHp += r.HpGranted;
                    expectedCells += r.CellsGranted;
                }

                run.SkipReward();
                if (room < 8)
                {
                    // take the highest tier offered so cells are reached
                    int best = 0;
                    for (int i = 1; i < run.Row.Count; i++)
                    {
                        if (run.Row[i].Tier > run.Row[best].Tier && (run.Row[i].Curse == null || run.Curses.Count == 0)) best = i;
                    }

                    run.ChooseDoor(best);
                }
            }

            Assert.AreEqual(expectedHp, run.DoorHp(0));
            Assert.AreEqual(expectedCells, run.DoorCells);
            Assert.LessOrEqual(run.DoorCells, _cfg.DoorCellCap);
        }

        [Test]
        public void Buff_CellPartIsLost_WhenNoLockedCellIsLeft()
        {
            RunSession run = RunToGoldDoor(13UL, 1);
            Assert.AreEqual(1, run.PendingBuff.Cells);
            Assert.AreEqual(0, run.CellsFor(0));
            ClaimResult r = run.ClaimBuff(0, 0, true);
            Assert.AreEqual(ClaimStatus.Ok, r.Status);
            Assert.AreEqual(0, r.CellsGranted);
            Assert.AreEqual(16, r.HpGranted);
        }

        [Test]
        public void GoldClaim_AddsTheCurse_AndMasksGoldForOneHero()
        {
            RunSession run = RunToGoldDoor(21UL, 1);
            CurseDef curse = run.PendingCurse;
            Assert.IsNotNull(curse);
            ClaimResult r = run.ClaimBuff(0, 10, true);
            Assert.AreEqual(ClaimStatus.Ok, r.Status);
            Assert.AreSame(curse, r.Curse);
            Assert.AreEqual(1, run.Curses.Count);
            Assert.AreEqual(0, run.CurseOf(0).HeroSlot);
            Assert.AreEqual(6, run.CurseOf(0).AcquiredRoom);

            // one hero: the party cap is 1, so no Gold card can follow
            run.SkipReward();
            foreach (DoorCard c in run.Row) Assert.AreNotEqual(TierKind.Gold, c.Tier);
        }

        [Test]
        public void GoldClaim_RefusesACursedHero_AndAPinnedCurseWithoutSpace()
        {
            RunSession run = RunToGoldDoor(31UL, 2);
            Assert.IsNotNull(run.PendingCurse);
            if (run.PendingCurse.Kind == CurseKind.Pinned)
            {
                Assert.AreEqual(ClaimStatus.NeedsSpace, run.ClaimBuff(0, 10, false).Status);
                Assert.AreEqual(RunPhase.BuffClaim, run.Phase, "a refusal keeps the pending curse");
            }

            Assert.AreEqual(ClaimStatus.Ok, run.ClaimBuff(0, 10, true).Status);
            run.SkipReward();
            run.ChooseDoor(2);   // ForceGold: card 3 is Gold again while the cap (2 heroes) allows it
            Assert.AreEqual(TierKind.Gold, run.CurrentDoor.Tier);
            run.OnVictory();
            Assert.AreEqual(ClaimStatus.HeroHasCurse, run.ClaimBuff(0, 10, true).Status);
            Assert.AreEqual(ClaimStatus.Ok, run.ClaimBuff(1, 10, true).Status);
            Assert.AreEqual(2, run.Curses.Count);
        }

        [Test]
        public void CurseTerms_ChangeMaxHpAndSpeed()
        {
            HeroDef hero = _catalog.FindHero("hero_crusader");
            var plain = new HeroBaselineInput(0, hero, 16, null);
            var frail = new HeroBaselineInput(0, hero, 16, null, null, curseHpDelta: -30);
            var slow = new HeroBaselineInput(0, hero, 0, null, null, curseSpeedDelta: -4);
            Assert.AreEqual(hero.BaseMaxHp + 16, HeroStatMath.MaxHp(plain, _catalog));
            Assert.AreEqual(hero.BaseMaxHp + 16 - 30, HeroStatMath.MaxHp(frail, _catalog));
            Assert.AreEqual(hero.BaseSpeed - 4, HeroStatMath.Speed(slow, _catalog));
            Assert.AreEqual(1, HeroStatMath.MaxHp(hero, 0, 0, -100000), "max HP never drops below 1");

            CurseDef pinned = _cfg.FindCurse("curse_anchor");
            Assert.AreEqual(-4, pinned.SpeedDelta);
            Assert.AreEqual(0, pinned.HpDelta);
            Assert.AreEqual(-30, _cfg.FindCurse("curse_frailty").HpDelta);
            Assert.AreEqual(3, _cfg.FindCurse("curse_wither").WitherPerTurn);
        }

        [Test]
        public void Wither_DamagesTheHeroAtTheStartOfEachOwnTurn()
        {
            HeroDef hero = _catalog.FindHero("hero_crusader");
            var arr = new BackpackArrangement(_catalog, 1, new[] { hero.BackpackProfile }, new SequentialItemIdSource());
            Assert.IsTrue(arr.CreateInTray("item_iron_sword", out long sword).IsOk);
            Assert.IsTrue(arr.Place(sword, ContainerKind.Hero0, 0, 0, 0).IsOk);
            arr.Freeze();
            ArrangementGridView grid = arr.CaptureView().HeroGrids[0];
            var enemies = new List<EnemySpawnInput> { new EnemySpawnInput(0, _catalog.FindMonster("mon_hunter")) };

            CombatResult cursed = CombatRunner.RunEncounter(_catalog,
                new EncounterSetup(1, 1, new[] { new HeroBaselineInput(0, hero, 0, grid, null, witherPerTurn: 3, curseId: "curse_wither") }, enemies),
                new SeededRng(4UL));
            Assert.AreEqual(CombatRunStatus.Completed, cursed.Status, cursed.StopDiagnostic);

            int ticks = 0, heroTurns = 0;
            foreach (CombatEventRecord e in cursed.Trace.Events)
            {
                if (e.Origin != null && e.Origin.Kind == OriginKind.Curse)
                {
                    ticks++;
                    Assert.AreEqual(UnitSide.Hero, e.Target.Side);
                    Assert.AreEqual("curse_wither", e.Origin.ItemDefId);
                    Assert.AreEqual(3, e.ResolvedAmount);
                }

                if ((e.Kind == CombatEventKind.ActionStarted || e.Kind == CombatEventKind.ActionSkipped) && e.Actor.Side == UnitSide.Hero && e.RootActionId > 0) heroTurns++;   // not the combat-start step
            }

            Assert.Greater(ticks, 0);
            Assert.AreEqual(heroTurns, ticks, "one tick per own turn");
        }

        [Test]
        public void Wither_CanDownTheHero()
        {
            HeroDef hero = _catalog.FindHero("hero_crusader");
            var arr = new BackpackArrangement(_catalog, 1, new[] { hero.BackpackProfile }, new SequentialItemIdSource());
            arr.Freeze();
            ArrangementGridView grid = arr.CaptureView().HeroGrids[0];
            var enemies = new List<EnemySpawnInput> { new EnemySpawnInput(0, _catalog.FindMonster("mon_brute")) };
            CombatResult r = CombatRunner.RunEncounter(_catalog,
                new EncounterSetup(2, 1, new[] { new HeroBaselineInput(0, hero, 0, grid, null, witherPerTurn: 1000, curseId: "curse_wither") }, enemies),
                new SeededRng(4UL));
            Assert.AreEqual(CombatRunStatus.Completed, r.Status, r.StopDiagnostic);
            Assert.AreEqual(CombatOutcome.Defeat, r.Outcome);
        }

        [Test]
        public void RewardOffer_IsDistinct_AndNeverTheCursedItem()
        {
            for (ulong seed = 1; seed <= 100; seed++)
            {
                RngStream drops = new SeededRng(seed).GetStream("drops");
                foreach (TierKind tier in new[] { TierKind.White, TierKind.Blue, TierKind.Gold })
                {
                    List<string> offer = RewardOffer.Build(_cfg, _catalog, tier, drops);
                    Assert.AreEqual(3, offer.Count);
                    Assert.AreEqual(3, new HashSet<string>(offer).Count);
                    CollectionAssert.DoesNotContain(offer, "item_cursed_anchor");
                }
            }
        }

        [Test]
        public void Baseline_CarriesDoorHpAndCurse()
        {
            RunSession run = RunToGoldDoor(41UL, 1);
            CurseDef curse = run.PendingCurse;
            run.ClaimBuff(0, 10, true);
            HeroBaselineInput b = run.Baseline(0, _catalog.FindHero("hero_crusader"), null);
            Assert.AreEqual(run.DoorHp(0), b.PermanentHpBonus);
            Assert.AreEqual(curse.HpDelta, b.CurseHpDelta);
            Assert.AreEqual(curse.SpeedDelta, b.CurseSpeedDelta);
            Assert.AreEqual(curse.WitherPerTurn, b.WitherPerTurn);
        }

        /// <summary>Plays rooms 1..5 choosing card 1, then the forced Gold card 3 into room 6, and wins it.</summary>
        private RunSession RunToGoldDoor(ulong seed, int heroes)
        {
            var run = new RunSession(_cfg, _catalog, seed, heroes) { ForceGold = true };
            for (int room = 1; room <= 5; room++)
            {
                run.OnVictory();
                if (run.Phase == RunPhase.BuffClaim) Assert.AreEqual(ClaimStatus.Ok, run.ClaimBuff(0, 10, true).Status);
                run.SkipReward();
                Assert.IsTrue(run.ChooseDoor(room == 5 ? 2 : 0));
            }

            Assert.AreEqual(6, run.Room);
            Assert.AreEqual(TierKind.Gold, run.CurrentDoor.Tier);
            run.OnVictory();
            Assert.AreEqual(RunPhase.BuffClaim, run.Phase);
            return run;
        }
    }
}
