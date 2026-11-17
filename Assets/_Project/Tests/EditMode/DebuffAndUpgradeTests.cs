using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Project05.CombatCore;
using Project05.CombatCore.Run;

namespace Project05.Tests.EditMode
{
    /// <summary>
    /// Debuffs, detonation and item upgrades (design/gdd/debuffs-and-detonation.md, item-upgrades.md), through the real
    /// catalog and the engine-free combat runner. Seeded and deterministic.
    /// </summary>
    [TestFixture]
    public class DebuffAndUpgradeTests
    {
        private GameCatalog _catalog;

        private static string Data(string file) => File.ReadAllText(Path.Combine("Assets/_Project/Data", file));

        [SetUp]
        public void SetUp()
        {
            _catalog = GameCatalog.LoadFromJson(Data("items_mvp.json"), Data("heroes_mvp.json"), Data("monsters_mvp.json"), out IReadOnlyList<string> errors);
            Assert.AreEqual(0, errors.Count, string.Join("\n", errors));
        }

        // ------------------------------------------------------------------ catalog
        [Test]
        public void Catalog_HasFiveDebuffsAndStun_AndNoSeal()
        {
            foreach (string id in new[] { "poison", "burn", "frost", "slow", "weaken" })
            {
                StatusDef def = _catalog.FindStatus(id);
                Assert.IsNotNull(def, id);
                Assert.IsTrue(def.IsDebuff, id);
                Assert.Greater(def.DetonateValue, 0, id);
            }

            Assert.IsFalse(_catalog.FindStatus("stun").IsDebuff);
            Assert.IsNull(_catalog.FindStatus("an"), "the Seal status is gone");
            foreach (ItemDef item in _catalog.Items)
            {
                foreach (EffectDef e in item.Effects)
                {
                    Assert.AreNotEqual(EffectKind.ConsumeDetonate, e.Kind, item.Id + " still uses the Seal detonator");
                }
            }
        }

        [Test]
        public void Catalog_UnknownStatus_FailsTheLoad()
        {
            string items = Data("items_mvp.json").Replace("\"status\": \"burn\"", "\"status\": \"blight_x\"");
            GameCatalog bad = GameCatalog.LoadFromJson(items, Data("heroes_mvp.json"), Data("monsters_mvp.json"), out IReadOnlyList<string> errors);
            Assert.IsNull(bad);
            StringAssert.Contains("unknown status 'blight_x'", string.Join("\n", errors));
        }

        [Test]
        public void OnlyTheAlchemistCore_Detonates()
        {
            var detonators = new List<string>();
            foreach (ItemDef item in _catalog.Items)
            {
                if (item.Detonates) detonators.Add(item.Id);
            }

            // the Plague Heart detonates too: it is fused from the Catalyst itself (item-fusion.md recipe 10), never an upgrade
            CollectionAssert.AreEquivalent(new[] { "item_plague_catalyst", "item_plague_catalyst_plus", "item_plague_heart" }, detonators);
            Assert.AreEqual(_catalog.RecipeMaking("item_plague_heart").Ingredients[0], "item_plague_catalyst");
            HeroDef alchemist = _catalog.FindHero("hero_alchemist");
            Assert.AreEqual("item_plague_catalyst", alchemist.SignatureItemId);
            CollectionAssert.Contains(alchemist.StartingItems, "item_plague_catalyst");
        }

        // ------------------------------------------------------------------ upgrades
        [Test]
        public void EveryItem_HasOneUpgrade_WithTheSameShape()
        {
            int bases = 0;
            foreach (ItemDef item in _catalog.Items)
            {
                if (item.IsUpgraded)
                {
                    ItemDef baseItem = _catalog.FindItem(item.UpgradeOf);
                    Assert.IsNotNull(baseItem, item.Id);
                    Assert.AreEqual(item.Id, baseItem.UpgradeId);
                    Assert.IsTrue(string.IsNullOrEmpty(item.UpgradeId), item.Id + " upgrades once only");
                    continue;
                }

                if (item.Id == "item_cursed_anchor")
                {
                    Assert.IsTrue(string.IsNullOrEmpty(item.UpgradeId), "a curse item never upgrades");
                    continue;
                }

                if (_catalog.RecipeMaking(item.Id) != null)
                {
                    Assert.IsTrue(string.IsNullOrEmpty(item.UpgradeId), "a fused item never upgrades (item-fusion.md Rule 2)");
                    continue;
                }

                bases++;
                ItemDef plus = _catalog.FindItem(item.UpgradeId);
                Assert.IsNotNull(plus, item.Id);
                Assert.AreEqual(item.Footprint, plus.Footprint, item.Id);
                Assert.AreEqual(item.Cells.Count, plus.Cells.Count, item.Id);
                Assert.AreEqual(item.Tier, plus.Tier, item.Id);
                StringAssert.EndsWith(" +", plus.NameVi);
            }

            Assert.AreEqual(45, bases);
        }

        [Test]
        public void Upgrade_RaisesNumbers_ByAboutAThird()
        {
            ItemDef sword = _catalog.FindItem("item_iron_sword");
            ItemDef plus = _catalog.FindItem(sword.UpgradeId);
            Assert.AreEqual(9, sword.Effects[0].Base);
            Assert.AreEqual(12, plus.Effects[0].Base);
            Assert.AreEqual(3, _catalog.FindItem("item_plague_catalyst_plus").Trigger.N, "cooldown 4 drops to 3");
        }

        [Test]
        public void RewardOffers_NeverHoldPlusItemsOrTheCore()
        {
            RunConfig cfg = RunConfig.Load(Data("run_mvp.json"), _catalog, out IReadOnlyList<string> errors);
            Assert.IsNotNull(cfg, string.Join("\n", errors));
            for (ulong seed = 1; seed <= 150; seed++)
            {
                foreach (string id in RewardOffer.Build(cfg, _catalog, TierKind.Gold, new SeededRng(seed).GetStream("drops")))
                {
                    Assert.IsFalse(_catalog.FindItem(id).IsUpgraded, id);
                    Assert.AreNotEqual("item_plague_catalyst", id);
                }
            }
        }

        [Test]
        public void Gold_StartsAt5_PaysByDoor_AndPricesUpgradesByTier()
        {
            RunConfig cfg = RunConfig.Load(Data("run_mvp.json"), _catalog, out _);
            Assert.AreEqual(5, cfg.StartGold);
            Assert.AreEqual(2, cfg.GoldFor(TierKind.White));
            Assert.AreEqual(10, cfg.GoldFor(TierKind.Gold));
            Assert.AreEqual(4, cfg.UpgradeCostFor(TierKind.White));
            Assert.AreEqual(14, cfg.UpgradeCostFor(TierKind.Purple));
        }

        // ------------------------------------------------------------------ debuffs in a fight
        [Test]
        public void Poison_TicksAtTheEnemyTurnStart_ThenDecays()
        {
            CombatResult r = Fight("hero_crusader", new[] { ("item_venom_vial", 0, 0) }, "mon_brute");
            CombatEventRecord tick = First(r, e => e.Kind == CombatEventKind.Damage && e.Origin != null && e.Origin.Kind == OriginKind.StatusTick);
            Assert.IsNotNull(tick, "a poison tick");
            Assert.AreEqual("poison", tick.Origin.ItemDefId);
            Assert.AreEqual(UnitSide.Enemy, tick.Target.Side);
            Assert.GreaterOrEqual(tick.ResolvedAmount, 3);
            CombatEventRecord decay = First(r, e => e.Kind == CombatEventKind.StatusExpired && e.ReasonCode == "decay" && e.Seq > tick.Seq);
            Assert.IsNotNull(decay, "one stack leaves after the tick");
            Assert.AreEqual(1, decay.StatusAmount);
        }

        [Test]
        public void Frost_AtSix_FreezesTheEnemy_AndItsTurnIsSkipped()
        {
            CombatResult r = Fight("hero_crusader", new[] { ("item_frost_shard", 0, 0), ("item_frost_shard", 1, 0), ("item_frost_shard", 2, 0) }, "mon_brute");
            CombatEventRecord freeze = First(r, e => e.Kind == CombatEventKind.StatusApplied && e.StatusId == "stun" && e.ReasonCode == "frozen");
            Assert.IsNotNull(freeze, "six Frost freeze the enemy");
            Assert.IsNotNull(First(r, e => e.Kind == CombatEventKind.StatusExpired && e.StatusId == "frost" && e.ReasonCode == "frozen"), "the Frost is used up");
            Assert.IsNotNull(First(r, e => e.Kind == CombatEventKind.ActionSkipped && e.Actor.Side == UnitSide.Enemy && e.ReasonCode == "stunned" && e.Seq > freeze.Seq));
        }

        [Test]
        public void Weaken_LowersWhatTheEnemyDeals()
        {
            // Crusader innate: Weaken 1 to all at combat start; War Banner: Weaken 2 to all at combat start.
            CombatResult r = Fight("hero_crusader", new[] { ("item_war_banner", 0, 0) }, "mon_hunter");
            CombatEventRecord hit = First(r, e => e.Kind == CombatEventKind.Damage && e.Origin != null && e.Origin.Kind == OriginKind.EnemyMove);
            Assert.IsNotNull(hit);
            long weaken = 0;
            foreach (CombatEventRecord e in r.Trace.Events)
            {
                if (e.Seq >= hit.Seq) break;
                if (e.Kind == CombatEventKind.StatusApplied && e.StatusId == "weaken") weaken += e.StatusAmount;
            }

            Assert.AreEqual(3, weaken);
            int strike = _catalog.FindMonster("mon_hunter").Moves[0].BaseDamage;   // data-driven (balance-three-item-start.md)
            Assert.AreEqual((strike * (1000 - 300) + 500) / 1000, hit.ResolvedAmount, "the Hunter's strike is cut by 30%");
        }

        [Test]
        public void Detonation_ConsumesEveryDebuff_AndFollowsTheFormula()
        {
            // venom (Poison 3) and oil flask (Burn 3) fire before the catalyst in reading order
            CombatResult r = Fight("hero_alchemist", new[] { ("item_venom_vial", 0, 0), ("item_oil_flask", 1, 0), ("item_plague_catalyst", 3, 0) }, "mon_brute");
            CombatEventRecord blast = First(r, e => e.Kind == CombatEventKind.DetonationDamage);
            Assert.IsNotNull(blast, "the core detonates");
            long b = 0;
            int kinds = 0;
            foreach (CombatEventRecord e in r.Trace.Events)
            {
                if (e.Kind != CombatEventKind.StatusConsumed || e.RootActionId != blast.RootActionId || e.Seq > blast.Seq || e.Target.Slot != blast.Target.Slot) continue;
                b += e.StatusAmount * _catalog.FindStatus(e.StatusId).DetonateValue;
                kinds++;
            }

            Assert.AreEqual(2, kinds, "poison and burn");
            Assert.AreEqual((b * (1000 + 100 * (kinds - 1)) + 500) / 1000, blast.ResolvedAmount);
            Assert.AreEqual(20, blast.ResolvedAmount, "(3x3 + 3x3) x 1.1");
        }

        [Test]
        public void PlagueCenser_NextToTheCore_AddsDetonationValuePerPoison()
        {
            CombatResult plain = Fight("hero_alchemist", new[] { ("item_venom_vial", 0, 0), ("item_plague_catalyst", 3, 0) }, "mon_brute");
            CombatResult boosted = Fight("hero_alchemist", new[] { ("item_venom_vial", 0, 0), ("item_plague_censer", 1, 0), ("item_plague_catalyst", 3, 0) }, "mon_brute");
            CombatEventRecord a = First(plain, e => e.Kind == CombatEventKind.DetonationDamage);
            CombatEventRecord b = First(boosted, e => e.Kind == CombatEventKind.DetonationDamage);
            Assert.AreEqual(9, a.ResolvedAmount, "Poison 3 x 3");
            Assert.AreEqual(15, b.ResolvedAmount, "Poison 3 x (3 + 2)");
        }

        [Test]
        public void AlchemistInnate_ReseedsPoison_AfterADetonation()
        {
            CombatResult r = Fight("hero_alchemist", new[] { ("item_venom_vial", 0, 0), ("item_plague_catalyst", 3, 0) }, "mon_brute");
            CombatEventRecord blast = First(r, e => e.Kind == CombatEventKind.DetonationDamage);
            Assert.IsNotNull(First(r, e => e.Kind == CombatEventKind.StatusApplied && e.StatusId == "poison" && e.Seq > blast.Seq && e.RootActionId == blast.RootActionId),
                "Plague Seed applies Poison in the same action");
        }

        // ------------------------------------------------------------------ helpers
        private CombatResult Fight(string heroId, (string id, int x, int y)[] items, string monster)
        {
            HeroDef hero = _catalog.FindHero(heroId);
            var arr = new BackpackArrangement(_catalog, 1, new[] { hero.BackpackProfile }, new SequentialItemIdSource());
            foreach ((string id, int x, int y) in items)
            {
                Assert.IsTrue(arr.CreateInTray(id, out long iid).IsOk, id);
                Assert.IsTrue(arr.Place(iid, ContainerKind.Hero0, x, y, 0).IsOk, id + " at " + x + "," + y);
            }

            arr.Freeze();
            ArrangementGridView grid = arr.CaptureView().HeroGrids[0];
            var setup = new EncounterSetup(1, 1, new[] { new HeroBaselineInput(0, hero, 0, grid) },
                new List<EnemySpawnInput> { new EnemySpawnInput(0, _catalog.FindMonster(monster)) });
            CombatResult r = CombatRunner.RunEncounter(_catalog, setup, new SeededRng(11UL));
            Assert.AreEqual(CombatRunStatus.Completed, r.Status, r.StopDiagnostic);
            return r;
        }

        private static CombatEventRecord First(CombatResult r, System.Predicate<CombatEventRecord> match)
        {
            foreach (CombatEventRecord e in r.Trace.Events)
            {
                if (match(e)) return e;
            }

            return null;
        }
    }
}
