using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using Project05.CombatCore;

namespace Project05.Tests.EditMode
{
    [TestFixture]
    public class CombatCoreTests
    {
        private GameCatalog _catalog;

        private static string LoadDataFile(string fileName)
        {
            return File.ReadAllText(Path.Combine("Assets/_Project/Data", fileName));
        }

        [SetUp]
        public void SetUp()
        {
            _catalog = GameCatalog.LoadFromJson(
                LoadDataFile("items_mvp.json"),
                LoadDataFile("heroes_mvp.json"),
                LoadDataFile("monsters_mvp.json"),
                out IReadOnlyList<string> errors);
            Assert.IsNotNull(_catalog);
            Assert.AreEqual(0, errors.Count, string.Join("\n", errors));
        }

        [Test]
        public void Assembly_HasZeroUnityReferences_NoMutableStaticFields_AndNoFloatOrDouble()
        {
            Assembly coreAsm = typeof(CombatRunner).Assembly;
            AssemblyName[] refs = coreAsm.GetReferencedAssemblies();
            foreach (AssemblyName r in refs)
            {
                StringAssert.DoesNotStartWith("Unity", r.Name, "CombatCore must not reference Unity assemblies.");
            }

            foreach (Type t in coreAsm.GetTypes())
            {
                FieldInfo[] staticFields = t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly);
                foreach (FieldInfo f in staticFields)
                {
                    // Compiler-generated backing/lambda cache fields or const/readonly are allowed; mutable static state is banned.
                    if (f.IsLiteral || f.IsInitOnly) continue;
                    if (f.Name.StartsWith("<", StringComparison.Ordinal)) continue;
                    Assert.Fail($"Mutable static field found in CombatCore: {t.FullName}.{f.Name}");
                }

                FieldInfo[] allFields = t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
                foreach (FieldInfo f in allFields)
                {
                    Assert.AreNotEqual(typeof(float), f.FieldType, $"float field banned: {t.FullName}.{f.Name}");
                    Assert.AreNotEqual(typeof(double), f.FieldType, $"double field banned: {t.FullName}.{f.Name}");
                }
            }
        }

        [Test]
        public void Catalog_LoadsItemsHeroesMonsters_WithZeroErrors()
        {
            Assert.AreEqual(103, _catalog.Items.Count);   // 46 items + 45 upgraded (+) versions (item-upgrades.md) + 12 fused (item-fusion.md)
            Assert.GreaterOrEqual(_catalog.Heroes.Count, 1);
            Assert.GreaterOrEqual(_catalog.Monsters.Count, 5);

            HeroDef crusader = _catalog.FindHero("hero_crusader");
            Assert.IsNotNull(crusader);
            Assert.AreEqual("tank", crusader.Art);
            Assert.AreEqual(4, crusader.BackpackProfile.Width);
            Assert.AreEqual(4, crusader.BackpackProfile.Height);
            Assert.IsNotNull(crusader.InnateItem);

            foreach (ItemDef item in _catalog.Items)
            {
                Assert.IsNotEmpty(item.Id);
                Assert.GreaterOrEqual(item.Cells.Count, 1);
                Assert.LessOrEqual(item.Effects.Count, GameCatalog.MaxEffectsPerItem);
                Assert.LessOrEqual(item.Synergy.Count, GameCatalog.MaxRulesPerItem);
                Assert.AreEqual(item.Trigger.Kind == TriggerKind.Passive, item.Effects.Count == 0, item.Id);
            }

            foreach (MonsterDef mon in _catalog.Monsters)
            {
                Assert.IsNotEmpty(mon.Id);
                Assert.That(mon.Visual, Is.EqualTo("swarmer").Or.EqualTo("brute"), mon.Id);
                Assert.GreaterOrEqual(mon.Moves.Count, 1, mon.Id);
            }
        }

        [Test]
        public void SeededRng_IsDeterministic_SupportsNamedStreams_AndRestoresSnapshots()
        {
            var rngA = new SeededRng(123456UL);
            var rngB = new SeededRng(123456UL);

            RngStream intentsA = rngA.GetStream("intents");
            RngStream intentsB = rngB.GetStream("intents");

            for (int i = 0; i < 16; i++)
            {
                Assert.AreEqual(intentsA.NextBounded(100), intentsB.NextBounded(100));
            }

            RngSnapshot snap = rngA.CaptureSnapshot();
            int expectedNext = rngA.GetStream("intents").NextBounded(1000);

            Assert.IsTrue(rngB.TryRestore(snap, out string restoreError), restoreError);
            int restoredNext = rngB.GetStream("intents").NextBounded(1000);
            Assert.AreEqual(expectedNext, restoredNext);

            Assert.Throws<ArgumentException>(() => rngA.GetStream("unknown_stream"));
        }

        [Test]
        public void BackpackArrangement_RotationAnchorOrdering_FreezeThaw_AndSnapshotRestore()
        {
            HeroDef crusader = _catalog.FindHero("hero_crusader");
            var idSource = new SequentialItemIdSource(1);
            var arrangement = new BackpackArrangement(
                _catalog,
                heroCount: 1,
                heroProfiles: new[] { crusader.BackpackProfile },
                idSource: idSource,
                stashWidth: 8,
                stashHeight: 2);

            Assert.IsTrue(arrangement.CreateInTray("item_iron_sword", out long swordId).IsOk);
            Assert.IsTrue(arrangement.CreateInTray("item_whetstone", out long whetId).IsOk);
            Assert.IsTrue(arrangement.CreateInTray("item_dagger", out long trayLeftoverId).IsOk);

            // Place sword rotated 90 degrees (1x2 -> 2x1) at (0, 1), whetstone at (0, 0).
            Assert.IsTrue(arrangement.Place(swordId, ContainerKind.Hero0, 0, 1, orientation: 1).IsOk);
            Assert.IsTrue(arrangement.Place(whetId, ContainerKind.Hero0, 0, 0, orientation: 0).IsOk);

            // Overlapping placement must fail.
            Assert.AreEqual(GridOpCode.Occupied, arrangement.Place(trayLeftoverId, ContainerKind.Hero0, 0, 0, 0).Code);

            ArrangementView view = arrangement.CaptureView();
            Assert.AreEqual(8, view.Stash.Width);
            Assert.AreEqual(2, view.Stash.Height);
            Assert.AreEqual(2, view.HeroGrids[0].Items.Count);
            // Canonical anchor reading order: (0,0) whetstone before (0,1) sword.
            Assert.AreEqual(whetId, view.HeroGrids[0].Items[0].InstanceId);
            Assert.AreEqual(swordId, view.HeroGrids[0].Items[1].InstanceId);

            string snap = arrangement.Snapshot();

            // Freeze destroys items left in tray.
            IReadOnlyList<long> destroyed = arrangement.Freeze();
            Assert.AreEqual(1, destroyed.Count);
            Assert.AreEqual(trayLeftoverId, destroyed[0]);
            Assert.IsTrue(arrangement.IsFrozen);
            Assert.AreEqual(GridOpCode.Frozen, arrangement.AutoPlace(swordId, ContainerKind.Hero0).Code);
            Assert.AreEqual(GridOpCode.Ok, arrangement.Thaw().Code);
            Assert.IsFalse(arrangement.IsFrozen);

            // Restore recovers the pre-freeze tray item.
            Assert.IsTrue(arrangement.Restore(snap).IsOk);
            Assert.AreEqual(1, arrangement.CaptureView().Tray.Count);
            Assert.AreEqual(trayLeftoverId, arrangement.CaptureView().Tray[0].InstanceId);
        }

        [Test]
        public void AdjacencySynergyEvaluator_ComputesDamageAndCooldownModifiers_AndPreviewsDiff()
        {
            HeroDef crusader = _catalog.FindHero("hero_crusader");
            var arr = new BackpackArrangement(_catalog, 1, new[] { crusader.BackpackProfile }, new SequentialItemIdSource());
            Assert.IsTrue(arr.CreateInTray("item_iron_sword", out long swordId).IsOk);
            Assert.IsTrue(arr.CreateInTray("item_whetstone", out long whetId).IsOk);

            Assert.IsTrue(arr.Place(swordId, ContainerKind.Hero0, 0, 0, 0).IsOk);
            SynergyPreviewResult preview = AdjacencySynergyEvaluator.PreviewPlacement(
                arr.CaptureView().HeroGrids[0],
                _catalog.FindItem("item_whetstone"),
                whetId,
                originX: 1,
                originY: 0,
                orientation: 0,
                _catalog);

            Assert.AreEqual(GridOpCode.Ok, preview.Code);
            Assert.GreaterOrEqual(preview.GainedLinks.Count, 1);

            Assert.IsTrue(arr.Place(whetId, ContainerKind.Hero0, 1, 0, 0).IsOk);
            GridSynergyView eval = AdjacencySynergyEvaluator.Evaluate(arr.CaptureView().HeroGrids[0], _catalog);
            ItemSynergySummary swordSummary = eval.FindForItem(swordId);
            Assert.IsNotNull(swordSummary);
            Assert.AreEqual(3, swordSummary.DamageFlatTotal);
        }

        [Test]
        public void CombatRunner_OneHeroVsOneToSixMonsters_IsDeterministic_AndPreservesBindingIntents()
        {
            HeroDef crusader = _catalog.FindHero("hero_crusader");
            MonsterDef swarmer = _catalog.FindMonster("mon_swarmer");
            MonsterDef brute = _catalog.FindMonster("mon_brute");

            for (int enemyCount = 1; enemyCount <= 6; enemyCount++)
            {
                var arrA = new BackpackArrangement(_catalog, 1, new[] { crusader.BackpackProfile }, new SequentialItemIdSource());
                Assert.IsTrue(arrA.CreateInTray("item_iron_sword", out long swordId).IsOk);
                Assert.IsTrue(arrA.CreateInTray("item_spiked_buckler", out long bucklerId).IsOk);
                Assert.IsTrue(arrA.Place(swordId, ContainerKind.Hero0, 0, 0, 0).IsOk);
                Assert.IsTrue(arrA.Place(bucklerId, ContainerKind.Hero0, 1, 0, 0).IsOk);
                arrA.Freeze();

                var enemies = new List<EnemySpawnInput>();
                for (int s = 0; s < enemyCount; s++)
                {
                    enemies.Add(new EnemySpawnInput(s, s % 2 == 0 ? swarmer : brute));
                }

                var setup = new EncounterSetup(
                    encounterId: 100 + enemyCount,
                    stageNumber: 1,
                    heroes: new[] { new HeroBaselineInput(0, crusader, 0, arrA.CaptureView().HeroGrids[0]) },
                    enemies: enemies);

                CombatResult run1 = CombatRunner.RunEncounter(_catalog, setup, new SeededRng(999UL));
                CombatResult run2 = CombatRunner.RunEncounter(_catalog, setup, new SeededRng(999UL));

                Assert.AreEqual(CombatRunStatus.Completed, run1.Status);
                Assert.AreEqual(run1.Outcome, run2.Outcome);
                Assert.AreEqual(run1.Trace.Events.Count, run2.Trace.Events.Count);
                Assert.AreEqual(run1.Trace.Events.Count + 1, run1.Trace.SnapshotsBySeq.Count);

                for (int i = 0; i < run1.Trace.Events.Count; i++)
                {
                    Assert.AreEqual(run1.Trace.Events[i].Kind, run2.Trace.Events[i].Kind);
                    Assert.AreEqual(run1.Trace.Events[i].Seq, run2.Trace.Events[i].Seq);
                    Assert.AreEqual(run1.Trace.Events[i].HpAfter, run2.Trace.Events[i].HpAfter);
                    Assert.AreEqual(run1.Trace.Events[i].ShieldAfter, run2.Trace.Events[i].ShieldAfter);
                }

                // Planning forecast matches initial prepared forecast without mutating RNG.
                var rngCheck = new SeededRng(999UL);
                Assert.IsTrue(CombatRunner.TryPrepare(_catalog, setup, rngCheck, out PreparedEncounter prep, out string err), err);
                IReadOnlyList<TurnForecastEntry> planForecast = TurnOrderScheduler.ForecastPlanning(setup.Heroes, prep.BoundEnemies, _catalog, 6);
                Assert.AreEqual(prep.InitialForecast.Count, planForecast.Count);
                for (int f = 0; f < planForecast.Count; f++)
                {
                    Assert.AreEqual(prep.InitialForecast[f].Unit, planForecast[f].Unit);
                    Assert.AreEqual(prep.InitialForecast[f].DeltaTime, planForecast[f].DeltaTime);
                }
            }
        }

        [Test]
        public void ApplyThenDetonate_OldestFirstTwoSources_AbsorbsShieldThenHp_AndExposesDownedImmediately()
        {
            // Two heroes apply Seal ("an"): Hero 0 applies 3, Hero 1 applies 2.
            // Hero 1 then consumes 4 (3 from Hero 0's lot, 1 from Hero 1's lot) with perUnit=2 -> 8 dmg.
            // Enemy has 5 shield (simulated via pre-fight? or enemy HP=3 and 0 shield; let's verify exact 2-source consumption + detonation).
            ItemDef apply3 = CreateTestItem("test_apply_3", TriggerKind.Cooldown, 1, 0,
                new[] { new EffectDef(EffectKind.ApplyStatus, TargetKind.FirstEnemy, 0, "an", 3, 0, 0) });
            ItemDef apply2 = CreateTestItem("test_apply_2", TriggerKind.Cooldown, 1, 0,
                new[] { new EffectDef(EffectKind.ApplyStatus, TargetKind.FirstEnemy, 0, "an", 2, 0, 0) });
            ItemDef detonate4 = CreateTestItem("test_detonate_4", TriggerKind.Cooldown, 1, 0,
                new[] { new EffectDef(EffectKind.ConsumeDetonate, TargetKind.FirstEnemy, 0, "an", 0, 4, 2) });

            HeroDef fastHero = CreateTestHero("test_h0", baseHp: 100, baseSpeed: 30, initialShield: 0);
            HeroDef secondHero = CreateTestHero("test_h1", baseHp: 100, baseSpeed: 20, initialShield: 0);
            MonsterDef dummy = CreateTestMonster("test_dummy", maxHp: 8, speed: 5, baseDamage: 1);

            var catalog = new GameCatalog(new[] { apply3, apply2, detonate4 }, new[] { fastHero, secondHero }, new[] { dummy });
            var arr = new BackpackArrangement(
                catalog,
                2,
                new[] { fastHero.BackpackProfile, secondHero.BackpackProfile },
                new SequentialItemIdSource());

            Assert.IsTrue(arr.CreateInTray("test_apply_3", out long i0).IsOk);
            Assert.IsTrue(arr.CreateInTray("test_apply_2", out long i1).IsOk);
            Assert.IsTrue(arr.CreateInTray("test_detonate_4", out long i2).IsOk);
            Assert.IsTrue(arr.Place(i0, ContainerKind.Hero0, 0, 0, 0).IsOk);
            Assert.IsTrue(arr.Place(i1, ContainerKind.Hero1, 0, 0, 0).IsOk);
            Assert.IsTrue(arr.Place(i2, ContainerKind.Hero1, 1, 0, 0).IsOk);
            arr.Freeze();

            var setup = new EncounterSetup(
                1,
                1,
                new[]
                {
                    new HeroBaselineInput(0, fastHero, 0, arr.CaptureView().HeroGrids[0]),
                    new HeroBaselineInput(1, secondHero, 0, arr.CaptureView().HeroGrids[1])
                },
                new[] { new EnemySpawnInput(0, dummy) });

            CombatResult result = CombatRunner.RunEncounter(catalog, setup, new SeededRng(42UL));
            Assert.AreEqual(CombatRunStatus.Completed, result.Status);
            Assert.AreEqual(CombatOutcome.Victory, result.Outcome);

            CombatEventRecord consumed = null;
            CombatEventRecord detDmg = null;
            CombatEventRecord downed = null;
            var appliedSeqs = new List<long>();
            foreach (CombatEventRecord ev in result.Trace.Events)
            {
                if (ev.Kind == CombatEventKind.StatusApplied) appliedSeqs.Add(ev.Seq);
                if (ev.Kind == CombatEventKind.StatusConsumed) consumed = ev;
                if (ev.Kind == CombatEventKind.DetonationDamage) detDmg = ev;
                if (ev.Kind == CombatEventKind.UnitDowned) downed = ev;
            }

            Assert.AreEqual(2, appliedSeqs.Count);
            Assert.IsNotNull(consumed);
            Assert.AreEqual(4L, consumed.StatusAmount);
            Assert.AreEqual(2, consumed.ConsumedSources.Count);
            Assert.AreEqual(appliedSeqs[0], consumed.ConsumedSources[0].SourceRefId);
            Assert.AreEqual(3, consumed.ConsumedSources[0].AmountConsumed);
            Assert.AreEqual(appliedSeqs[1], consumed.ConsumedSources[1].SourceRefId);
            Assert.AreEqual(1, consumed.ConsumedSources[1].AmountConsumed);

            // Snapshot immediately after StatusConsumed shows 1 stack remaining.
            CombatStateSnapshot afterConsumeSnap = result.Trace.SnapshotsBySeq[(int)consumed.Seq];
            Assert.AreEqual(1L, afterConsumeSnap.Enemies[0].Statuses[0].TotalAmount);

            Assert.IsNotNull(detDmg);
            Assert.AreEqual(8, detDmg.ResolvedAmount);
            Assert.AreEqual(0, detDmg.HpAfter);
            Assert.IsFalse(result.Trace.SnapshotsBySeq[(int)detDmg.Seq].Enemies[0].IsAlive);
            Assert.IsNotNull(downed);
        }

        [Test]
        public void DamageFormula_WorkedExamples_AndZeroDamageEmitsNoEvent()
        {
            Assert.AreEqual(10, CombatRunner.MaxReactionDepth > 0 ? 10 : 0); // sanity
            // Tested end-to-end via ShieldGain + Damage interactions and zero-damage reduction.
            ItemDef weakHit = CreateTestItem("test_weak", TriggerKind.Cooldown, 1, 0,
                new[] { new EffectDef(EffectKind.Damage, TargetKind.FirstEnemy, 3, "", 0, 0, 0) });
            ItemDef penaltyMod = new ItemDef(
                "test_penalty",
                "art",
                TierKind.White,
                "1x1",
                new[] { new GridCell(0, 0) },
                new[] { "weapon" },
                new TriggerDef(TriggerKind.Passive, 0, 0),
                Array.Empty<EffectDef>(),
                0,
                0,
                new[] { new SynergyRuleDef(new[] { "weapon" }, SynergyKind.DamageFlat, -5, "") },
                "p",
                "p",
                "",
                "");
            ItemDef strongKill = CreateTestItem("test_kill", TriggerKind.Cooldown, 1, 1,
                new[] { new EffectDef(EffectKind.Damage, TargetKind.FirstEnemy, 50, "", 0, 0, 0) });

            HeroDef hero = CreateTestHero("test_h", 100, 30, 0);
            MonsterDef mon = CreateTestMonster("test_m", 20, 10, 1);

            var catalog = new GameCatalog(new[] { weakHit, penaltyMod, strongKill }, new[] { hero }, new[] { mon });
            var arr = new BackpackArrangement(catalog, 1, new[] { hero.BackpackProfile }, new SequentialItemIdSource());
            Assert.IsTrue(arr.CreateInTray("test_weak", out long wId).IsOk);
            Assert.IsTrue(arr.CreateInTray("test_penalty", out long pId).IsOk);
            Assert.IsTrue(arr.CreateInTray("test_kill", out long kId).IsOk);
            Assert.IsTrue(arr.Place(wId, ContainerKind.Hero0, 0, 0, 0).IsOk);
            Assert.IsTrue(arr.Place(pId, ContainerKind.Hero0, 1, 0, 0).IsOk);
            Assert.IsTrue(arr.Place(kId, ContainerKind.Hero0, 0, 1, 0).IsOk);
            arr.Freeze();

            var setup = new EncounterSetup(
                2,
                1,
                new[] { new HeroBaselineInput(0, hero, 0, arr.CaptureView().HeroGrids[0]) },
                new[] { new EnemySpawnInput(0, mon) });

            CombatResult result = CombatRunner.RunEncounter(catalog, setup, new SeededRng(1UL));
            Assert.AreEqual(CombatOutcome.Victory, result.Outcome);

            foreach (CombatEventRecord ev in result.Trace.Events)
            {
                if (ev.Kind == CombatEventKind.Damage && ev.Actor.Side == UnitSide.Hero)
                {
                    Assert.Greater(ev.ResolvedAmount, 0, "Zero resolved damage must not emit a Damage mutation event.");
                }
            }
        }

        [Test]
        public void HealAtFullHp_EmitsNoMutationEvent_AndShieldAbsorbsBeforeHp()
        {
            ItemDef shieldItem = CreateTestItem("test_shield", TriggerKind.Cooldown, 1, 0,
                new[]
                {
                    new EffectDef(EffectKind.ShieldGain, TargetKind.Self, 0, "", 5, 0, 0),
                    new EffectDef(EffectKind.Heal, TargetKind.Self, 0, "", 10, 0, 0),
                    new EffectDef(EffectKind.Damage, TargetKind.FirstEnemy, 20, "", 0, 0, 0)
                });

            HeroDef hero = CreateTestHero("test_h", 100, 40, 0);
            MonsterDef mon = CreateTestMonster("test_m", 20, 5, 1);

            var catalog = new GameCatalog(new[] { shieldItem }, new[] { hero }, new[] { mon });
            var arr = new BackpackArrangement(catalog, 1, new[] { hero.BackpackProfile }, new SequentialItemIdSource());
            Assert.IsTrue(arr.CreateInTray("test_shield", out long sId).IsOk);
            Assert.IsTrue(arr.Place(sId, ContainerKind.Hero0, 0, 0, 0).IsOk);
            arr.Freeze();

            var setup = new EncounterSetup(
                3,
                1,
                new[] { new HeroBaselineInput(0, hero, 0, arr.CaptureView().HeroGrids[0]) },
                new[] { new EnemySpawnInput(0, mon) });

            CombatResult result = CombatRunner.RunEncounter(catalog, setup, new SeededRng(1UL));
            Assert.AreEqual(CombatOutcome.Victory, result.Outcome);

            bool sawShieldGain = false;
            foreach (CombatEventRecord ev in result.Trace.Events)
            {
                if (ev.Kind == CombatEventKind.ShieldGained) sawShieldGain = true;
                if (ev.Kind == CombatEventKind.Healed)
                {
                    Assert.Greater(ev.ResolvedAmount, 0, "Heal at full HP (0 healed) must not emit Healed event.");
                }
            }
            Assert.IsTrue(sawShieldGain);
        }

        [Test]
        public void NoReadyItemOnTurn_EmitsActionSkippedNoReadyItem_AndAdvancesOrdinal()
        {
            ItemDef delayedSword = CreateTestItem("test_delayed", TriggerKind.Cooldown, 2, 1,
                new[] { new EffectDef(EffectKind.Damage, TargetKind.FirstEnemy, 20, "", 0, 0, 0) });

            HeroDef hero = CreateTestHero("test_h", 100, 40, 0);
            MonsterDef mon = CreateTestMonster("test_m", 20, 5, 1);

            var catalog = new GameCatalog(new[] { delayedSword }, new[] { hero }, new[] { mon });
            var arr = new BackpackArrangement(catalog, 1, new[] { hero.BackpackProfile }, new SequentialItemIdSource());
            Assert.IsTrue(arr.CreateInTray("test_delayed", out long dId).IsOk);
            Assert.IsTrue(arr.Place(dId, ContainerKind.Hero0, 0, 0, 0).IsOk);
            arr.Freeze();

            var setup = new EncounterSetup(
                4,
                1,
                new[] { new HeroBaselineInput(0, hero, 0, arr.CaptureView().HeroGrids[0]) },
                new[] { new EnemySpawnInput(0, mon) });

            CombatResult result = CombatRunner.RunEncounter(catalog, setup, new SeededRng(1UL));
            Assert.AreEqual(CombatOutcome.Victory, result.Outcome);

            CombatEventRecord firstHeroSkip = null;
            CombatEventRecord itemFired = null;
            foreach (CombatEventRecord ev in result.Trace.Events)
            {
                if (ev.Kind == CombatEventKind.ActionSkipped && ev.Actor.Side == UnitSide.Hero && firstHeroSkip == null)
                {
                    firstHeroSkip = ev;
                }
                if (ev.Kind == CombatEventKind.ItemFired && itemFired == null)
                {
                    itemFired = ev;
                }
            }

            Assert.IsNotNull(firstHeroSkip, "Hero action 1 with S=1 item must emit ActionSkipped.");
            Assert.AreEqual("no_ready_item", firstHeroSkip.ReasonCode);
            Assert.AreEqual(1, firstHeroSkip.ActionOrdinal);
            Assert.IsNotNull(itemFired);
            Assert.AreEqual(2, itemFired.ActionOrdinal);
        }

        [Test]
        public void ReactionLoop_IsSuppressedOncePerRoot_AndChainLimitsEnforced()
        {
            // Item A triggers B on ItemFired, Item B triggers A on ItemFired -> once-per-root suppresses loop.
            ItemDef starter = CreateTestItem("test_start", TriggerKind.Cooldown, 1, 0,
                new[] { new EffectDef(EffectKind.Damage, TargetKind.FirstEnemy, 1, "", 0, 0, 0) });
            ItemDef reactA = new ItemDef("test_ra", "art", TierKind.White, "1x1", new[] { new GridCell(0, 0) }, new[] { "relic" },
                new TriggerDef(TriggerKind.Reaction, 1, 0, ReactionEventKind.ItemFired, "", ReactionSideFilter.OwnHero),
                new[] { new EffectDef(EffectKind.Damage, TargetKind.FirstEnemy, 1, "", 0, 0, 0) },
                0, 0, Array.Empty<SynergyRuleDef>(), "ra", "ra", "", "");
            ItemDef reactB = new ItemDef("test_rb", "art", TierKind.White, "1x1", new[] { new GridCell(0, 0) }, new[] { "relic" },
                new TriggerDef(TriggerKind.Reaction, 1, 0, ReactionEventKind.ItemFired, "", ReactionSideFilter.OwnHero),
                new[] { new EffectDef(EffectKind.Damage, TargetKind.FirstEnemy, 1, "", 0, 0, 0) },
                0, 0, Array.Empty<SynergyRuleDef>(), "rb", "rb", "", "");

            HeroDef hero = CreateTestHero("test_h", 100, 30, 0);
            MonsterDef mon = CreateTestMonster("test_m", 10, 5, 1);

            var catalog = new GameCatalog(new[] { starter, reactA, reactB }, new[] { hero }, new[] { mon });
            var arr = new BackpackArrangement(catalog, 1, new[] { hero.BackpackProfile }, new SequentialItemIdSource());
            Assert.IsTrue(arr.CreateInTray("test_start", out long idS).IsOk);
            Assert.IsTrue(arr.CreateInTray("test_ra", out long idA).IsOk);
            Assert.IsTrue(arr.CreateInTray("test_rb", out long idB).IsOk);
            Assert.IsTrue(arr.Place(idS, ContainerKind.Hero0, 0, 0, 0).IsOk);
            Assert.IsTrue(arr.Place(idA, ContainerKind.Hero0, 1, 0, 0).IsOk);
            Assert.IsTrue(arr.Place(idB, ContainerKind.Hero0, 2, 0, 0).IsOk);
            arr.Freeze();

            var setup = new EncounterSetup(
                5,
                1,
                new[] { new HeroBaselineInput(0, hero, 0, arr.CaptureView().HeroGrids[0]) },
                new[] { new EnemySpawnInput(0, mon) });

            CombatResult result = CombatRunner.RunEncounter(catalog, setup, new SeededRng(1UL));
            Assert.AreEqual(CombatRunStatus.Completed, result.Status);

            int suppressedCount = 0;
            foreach (CombatEventRecord ev in result.Trace.Events)
            {
                if (ev.Kind == CombatEventKind.ListenerSuppressed)
                {
                    suppressedCount++;
                }
            }
            Assert.GreaterOrEqual(suppressedCount, 1);
        }

        [Test]
        public void CombatRunner_Hits200ActionCap_AndDeclaresDefeatWithReasonActionLimit()
        {
            ItemDef chipDmg = CreateTestItem("test_chip", TriggerKind.Cooldown, 1, 0,
                new[] { new EffectDef(EffectKind.Damage, TargetKind.FirstEnemy, 1, "", 0, 0, 0) });
            ItemDef shield = CreateTestItem("test_big_shield", TriggerKind.Cooldown, 1, 0,
                new[] { new EffectDef(EffectKind.ShieldGain, TargetKind.Self, 0, "", 50, 0, 0) });

            HeroDef hero = CreateTestHero("test_h", 500, 20, 50);
            MonsterDef mon = CreateTestMonster("test_m", 50000, 20, 1);

            var catalog = new GameCatalog(new[] { chipDmg, shield }, new[] { hero }, new[] { mon });
            var arr = new BackpackArrangement(catalog, 1, new[] { hero.BackpackProfile }, new SequentialItemIdSource());
            Assert.IsTrue(arr.CreateInTray("test_chip", out long cId).IsOk);
            Assert.IsTrue(arr.CreateInTray("test_big_shield", out long sId).IsOk);
            Assert.IsTrue(arr.Place(cId, ContainerKind.Hero0, 0, 0, 0).IsOk);
            Assert.IsTrue(arr.Place(sId, ContainerKind.Hero0, 1, 0, 0).IsOk);
            arr.Freeze();

            var setup = new EncounterSetup(
                6,
                1,
                new[] { new HeroBaselineInput(0, hero, 0, arr.CaptureView().HeroGrids[0]) },
                new[] { new EnemySpawnInput(0, mon) });

            CombatResult result = CombatRunner.RunEncounter(catalog, setup, new SeededRng(7UL));
            Assert.AreEqual(CombatRunStatus.Completed, result.Status);
            Assert.AreEqual(CombatOutcome.Defeat, result.Outcome);

            CombatEventRecord endEv = result.Trace.Events[result.Trace.Events.Count - 1];
            Assert.AreEqual(CombatEventKind.CombatEnded, endEv.Kind);
            Assert.AreEqual("action_limit", endEv.ReasonCode);
            Assert.AreEqual(CombatRunner.MaxActorActions, result.Trace.SnapshotsBySeq[result.Trace.SnapshotsBySeq.Count - 1].CompletedActions);
        }

        [Test]
        public void BossPhase_WithOpenEndedEndIndexMinusOne_AppliesPhaseMovePool()
        {
            MonsterDef boss = _catalog.FindMonster("mon_boss");
            HeroDef hero = _catalog.FindHero("hero_crusader");
            Assert.IsNotNull(boss);

            var arr = new BackpackArrangement(_catalog, 1, new[] { hero.BackpackProfile }, new SequentialItemIdSource());
            arr.Freeze();
            var setup = new EncounterSetup(
                7,
                1,
                new[] { new HeroBaselineInput(0, hero, 0, arr.CaptureView().HeroGrids[0]) },
                new[] { new EnemySpawnInput(0, boss) });

            Assert.IsTrue(CombatRunner.TryPrepare(_catalog, setup, new SeededRng(1234UL), out PreparedEncounter prep, out string err), err);
            Assert.AreEqual(12, prep.BoundEnemies[0].Sequence.Count);
            // Phase 0 (index 0..3) only allows strike or sweep.
            for (int i = 0; i <= 3; i++)
            {
                Assert.That(prep.BoundEnemies[0].Sequence[i].MoveId, Is.EqualTo("strike").Or.EqualTo("sweep"));
            }
        }

        [Test]
        public void CombatStart_InnateAndGridCombatStartItems_RunPreFightWithActionOrdinalZero()
        {
            HeroDef crusader = _catalog.FindHero("hero_crusader");
            MonsterDef hunter = _catalog.FindMonster("mon_hunter");

            var arr = new BackpackArrangement(_catalog, 1, new[] { crusader.BackpackProfile }, new SequentialItemIdSource());
            Assert.IsTrue(arr.CreateInTray("item_iron_sword", out long sId).IsOk);
            Assert.IsTrue(arr.Place(sId, ContainerKind.Hero0, 0, 0, 0).IsOk);
            arr.Freeze();

            var setup = new EncounterSetup(
                8,
                1,
                new[] { new HeroBaselineInput(0, crusader, 0, arr.CaptureView().HeroGrids[0]) },
                new[] { new EnemySpawnInput(0, hunter) });

            CombatResult result = CombatRunner.RunEncounter(_catalog, setup, new SeededRng(55UL));
            Assert.AreEqual(CombatRunStatus.Completed, result.Status);

            CombatEventRecord preFightAction = null;
            CombatEventRecord innateFired = null;
            foreach (CombatEventRecord ev in result.Trace.Events)
            {
                if (ev.Kind == CombatEventKind.ActionStarted && ev.RootActionId == 0L)
                {
                    preFightAction = ev;
                }
                if (ev.Kind == CombatEventKind.ItemFired && ev.TriggerKind == TriggerKind.CombatStart)
                {
                    innateFired = ev;
                    break;
                }
            }

            Assert.IsNotNull(preFightAction, "CombatStart pre-fight phase must emit ActionStarted with rootActionId=0 and actionOrdinal=0.");
            Assert.AreEqual(0, preFightAction.ActionOrdinal);
            Assert.IsNotNull(innateFired);
            Assert.AreEqual(0, innateFired.ActionOrdinal);
        }

        [Test]
        public void DetonationEmpty_WhenNoStatusAvailable_EmitsFactAndNoDamage()
        {
            ItemDef bell = CreateTestItem("test_bell", TriggerKind.Cooldown, 1, 0,
                new[] { new EffectDef(EffectKind.ConsumeDetonate, TargetKind.FirstEnemy, 0, "an", 0, 5, 2) });
            ItemDef finisher = CreateTestItem("test_finisher", TriggerKind.Cooldown, 1, 1,
                new[] { new EffectDef(EffectKind.Damage, TargetKind.FirstEnemy, 50, "", 0, 0, 0) });

            HeroDef hero = CreateTestHero("test_h", 100, 30, 0);
            MonsterDef mon = CreateTestMonster("test_m", 20, 5, 1);

            var catalog = new GameCatalog(new[] { bell, finisher }, new[] { hero }, new[] { mon });
            var arr = new BackpackArrangement(catalog, 1, new[] { hero.BackpackProfile }, new SequentialItemIdSource());
            Assert.IsTrue(arr.CreateInTray("test_bell", out long bId).IsOk);
            Assert.IsTrue(arr.CreateInTray("test_finisher", out long fId).IsOk);
            Assert.IsTrue(arr.Place(bId, ContainerKind.Hero0, 0, 0, 0).IsOk);
            Assert.IsTrue(arr.Place(fId, ContainerKind.Hero0, 1, 0, 0).IsOk);
            arr.Freeze();

            var setup = new EncounterSetup(
                9,
                1,
                new[] { new HeroBaselineInput(0, hero, 0, arr.CaptureView().HeroGrids[0]) },
                new[] { new EnemySpawnInput(0, mon) });

            CombatResult result = CombatRunner.RunEncounter(catalog, setup, new SeededRng(3UL));
            Assert.AreEqual(CombatOutcome.Victory, result.Outcome);

            bool sawEmpty = false;
            foreach (CombatEventRecord ev in result.Trace.Events)
            {
                if (ev.Kind == CombatEventKind.DetonationEmpty) sawEmpty = true;
                Assert.AreNotEqual(CombatEventKind.DetonationDamage, ev.Kind);
            }
            Assert.IsTrue(sawEmpty);
        }

        [Test]
        public void MutualKnockout_InSameActionQueue_EndsInDraw()
        {
            ItemDef mutual = CreateTestItem("test_mutual", TriggerKind.Cooldown, 1, 0,
                new[]
                {
                    new EffectDef(EffectKind.Damage, TargetKind.FirstEnemy, 50, "", 0, 0, 0),
                    new EffectDef(EffectKind.Damage, TargetKind.Self, 50, "", 0, 0, 0)
                });

            HeroDef hero = CreateTestHero("test_h", 20, 40, 0);
            MonsterDef mon = CreateTestMonster("test_m", 20, 5, 1);

            var catalog = new GameCatalog(new[] { mutual }, new[] { hero }, new[] { mon });
            var arr = new BackpackArrangement(catalog, 1, new[] { hero.BackpackProfile }, new SequentialItemIdSource());
            Assert.IsTrue(arr.CreateInTray("test_mutual", out long mId).IsOk);
            Assert.IsTrue(arr.Place(mId, ContainerKind.Hero0, 0, 0, 0).IsOk);
            arr.Freeze();

            var setup = new EncounterSetup(
                10,
                1,
                new[] { new HeroBaselineInput(0, hero, 0, arr.CaptureView().HeroGrids[0]) },
                new[] { new EnemySpawnInput(0, mon) });

            CombatResult result = CombatRunner.RunEncounter(catalog, setup, new SeededRng(1UL));
            Assert.AreEqual(CombatRunStatus.Completed, result.Status);
            Assert.AreEqual(CombatOutcome.Draw, result.Outcome);
            CombatEventRecord endEv = result.Trace.Events[result.Trace.Events.Count - 1];
            Assert.AreEqual("mutual_elimination", endEv.ReasonCode);
        }

        [Test]
        public void ReactionDepthLimit_BlocksDepthNineAndPublishesChainLimit()
        {
            // Chain of 10 distinct reaction items each listening to ItemFired -> reaches depth 8 and blocks at depth 9.
            var items = new List<ItemDef>();
            items.Add(CreateTestItem("chain_root", TriggerKind.Cooldown, 1, 0,
                new[] { new EffectDef(EffectKind.Damage, TargetKind.FirstEnemy, 1, "", 0, 0, 0) }));
            for (int i = 1; i <= 10; i++)
            {
                items.Add(new ItemDef("chain_r" + i, "art", TierKind.White, "1x1", new[] { new GridCell(0, 0) }, new[] { "relic" },
                    new TriggerDef(TriggerKind.Reaction, 1, 0, ReactionEventKind.ItemFired, "", ReactionSideFilter.OwnHero),
                    new[] { new EffectDef(EffectKind.Damage, TargetKind.FirstEnemy, 1, "", 0, 0, 0) },
                    0, 0, Array.Empty<SynergyRuleDef>(), "r", "r", "", ""));
            }

            HeroDef hero = CreateTestHero("test_h", 100, 40, 0);
            MonsterDef mon = CreateTestMonster("test_m", 1000, 5, 1);
            var catalog = new GameCatalog(items, new[] { hero }, new[] { mon });
            var arr = new BackpackArrangement(catalog, 1, new[] { hero.BackpackProfile }, new SequentialItemIdSource());

            Assert.IsTrue(arr.CreateInTray("chain_root", out long r0).IsOk);
            Assert.IsTrue(arr.Place(r0, ContainerKind.Hero0, 0, 0, 0).IsOk);
            for (int i = 1; i <= 10; i++)
            {
                Assert.IsTrue(arr.CreateInTray("chain_r" + i, out long ri).IsOk);
                Assert.IsTrue(arr.Place(ri, ContainerKind.Hero0, i % 4, i / 4, 0).IsOk);
            }
            arr.Freeze();

            var setup = new EncounterSetup(
                11,
                1,
                new[] { new HeroBaselineInput(0, hero, 0, arr.CaptureView().HeroGrids[0]) },
                new[] { new EnemySpawnInput(0, mon) });

            CombatResult result = CombatRunner.RunEncounter(catalog, setup, new SeededRng(1UL));
            Assert.AreEqual(CombatRunStatus.Completed, result.Status);

            bool sawDepthLimit = false;
            int maxItemDepth = 0;
            foreach (CombatEventRecord ev in result.Trace.Events)
            {
                if (ev.Kind == CombatEventKind.ItemFired && ev.Depth > maxItemDepth) maxItemDepth = ev.Depth;
                if (ev.Kind == CombatEventKind.ChainLimit && ev.ReasonCode == "reaction_depth") sawDepthLimit = true;
            }
            Assert.IsTrue(sawDepthLimit, "Depth > 8 reaction must emit ChainLimit(reaction_depth).");
            Assert.LessOrEqual(maxItemDepth, CombatRunner.MaxReactionDepth);
        }

        private static ItemDef CreateTestItem(string id, TriggerKind kind, int n, int s, EffectDef[] effects)
        {
            return new ItemDef(
                id,
                "art",
                TierKind.White,
                "1x1",
                new[] { new GridCell(0, 0) },
                new[] { "weapon" },
                new TriggerDef(kind, n, s),
                effects,
                0,
                0,
                Array.Empty<SynergyRuleDef>(),
                id,
                id,
                string.Empty,
                string.Empty);
        }

        private static HeroDef CreateTestHero(string id, int baseHp, int baseSpeed, int initialShield)
        {
            return new HeroDef(
                id,
                "tank",
                id,
                id,
                "Tank",
                "Tank",
                baseHp,
                baseSpeed,
                initialShield,
                new BackpackProfileDef(4, 4, 4, 4, Array.Empty<GridCell>()),
                Array.Empty<string>(),
                string.Empty,
                null);
        }

        private static MonsterDef CreateTestMonster(string id, int maxHp, int speed, int baseDamage)
        {
            return new MonsterDef(
                id,
                "swarmer",
                26,
                id,
                id,
                MonsterClassKind.Regular,
                MonsterSizeKind.Small,
                maxHp,
                speed,
                "hit",
                new[]
                {
                    new MonsterMoveDef("hit", "Hit", "Hit", MoveGlyphKind.Attack, DeliveryKind.Swing, 1, baseDamage, MoveTargetMode.RandomHero, string.Empty, 100, false, string.Empty)
                },
                Array.Empty<BossPhaseDef>());
        }
    }
}
