using System.Collections.Generic;
using NUnit.Framework;
using Project05.Core;
using Project05.Data;
using Project05.Backpack;
using Project05.Combat;

namespace Project05.Tests.EditMode
{
    [TestFixture]
    public class CombatAndTurnSystemTests
    {
        [SetUp]
        public void SetUp()
        {
            DefaultGameCatalog.EnsureInitialized();
        }

        [Test]
        public void SpeedGaugeManager_HigherSpeedActsFirst_AndSpeedBootsShiftTurnOrder()
        {
            HeroCombatant hero = new HeroCombatant(DefaultGameCatalog.GetHeroById("hero_crusader"), PartyPosition.Frontline, populateStartingItems: false);
            hero.BaseSpeed = 20;

            MonsterCombatant fastMonster = new MonsterCombatant(DefaultGameCatalog.GetMonsterById("monster_cultist"), 1.0f);
            fastMonster.BaseSpeed = 24;

            var heroes = new List<HeroCombatant> { hero };
            var monsters = new List<MonsterCombatant> { fastMonster };
            SpeedGaugeManager gaugeMgr = new SpeedGaugeManager();

            // Without boots, monster (24 speed) acts before hero (20 speed)
            Combatant firstActor = gaugeMgr.AdvanceToNextTurnInstant(heroes, monsters);
            Assert.AreSame(fastMonster, firstActor);

            // Reset gauges and equip Swift Greaves (+6 Passive Speed -> Hero EffectiveSpeed = 26 > 24)
            hero.ActionGauge = 0f;
            fastMonster.ActionGauge = 0f;
            hero.PersonalBackpack.TryAutoPlaceItem(new ItemData(DefaultGameCatalog.GetItemById("item_haste_boots")));
            hero.RecalculateSynergies(null);

            Assert.AreEqual(26, hero.GetEffectiveSpeed());
            Combatant newFirstActor = gaugeMgr.AdvanceToNextTurnInstant(heroes, monsters);
            Assert.AreSame(hero, newFirstActor, "Hero with +6 Speed boots should now fill gauge before Monster");
        }

        [Test]
        public void BackpackTriggerEngine_Cooldown1And2And3_TriggerOnExactTurnIntervals()
        {
            HeroCombatant hero = new HeroCombatant(DefaultGameCatalog.GetHeroById("hero_crusader"), PartyPosition.Frontline, populateStartingItems: false);
            // Unlock all 5x4 slots
            hero.PersonalBackpack.UnlockNextLockedSlots(20);

            ItemData swordCd1 = new ItemData(DefaultGameCatalog.GetItemById("item_iron_sword")); // CD 1
            ItemData axeCd2 = new ItemData(DefaultGameCatalog.GetItemById("item_executioner_axe")); // CD 2
            ItemData tomeCd3 = new ItemData(DefaultGameCatalog.GetItemById("item_thunder_tome")); // CD 3

            Assert.IsTrue(hero.PersonalBackpack.TryPlaceItem(swordCd1, 0, 0, 0));
            Assert.IsTrue(hero.PersonalBackpack.TryPlaceItem(axeCd2, 1, 0, 0));
            Assert.IsTrue(hero.PersonalBackpack.TryPlaceItem(tomeCd3, 3, 0, 0));

            MonsterCombatant dummy = new MonsterCombatant(DefaultGameCatalog.GetMonsterById("monster_lich_boss"), 3.0f);
            var party = new List<HeroCombatant> { hero };
            var enemies = new List<MonsterCombatant> { dummy };

            BackpackTriggerEngine.ResetCooldownsForCombatStart(hero.PersonalBackpack);

            // Turn 1: All 3 items (CD1, CD2, CD3) trigger!
            BackpackTurnResult t1 = BackpackTriggerEngine.TriggerHeroBackpack(hero, party, enemies);
            Assert.AreEqual(3, t1.TriggeredItems.Count);
            Assert.AreEqual(1, swordCd1.TriggerCountThisCombat);
            Assert.AreEqual(1, axeCd2.TriggerCountThisCombat);
            Assert.AreEqual(1, tomeCd3.TriggerCountThisCombat);

            // Turn 2: Only CD1 triggers; CD2 and CD3 tick down
            BackpackTurnResult t2 = BackpackTriggerEngine.TriggerHeroBackpack(hero, party, enemies);
            Assert.AreEqual(1, t2.TriggeredItems.Count);
            Assert.AreEqual(2, swordCd1.TriggerCountThisCombat);
            Assert.AreEqual(1, axeCd2.TriggerCountThisCombat);
            Assert.AreEqual(1, tomeCd3.TriggerCountThisCombat);

            // Turn 3: CD1 and CD2 trigger; CD3 ticks down
            BackpackTurnResult t3 = BackpackTriggerEngine.TriggerHeroBackpack(hero, party, enemies);
            Assert.AreEqual(2, t3.TriggeredItems.Count);
            Assert.AreEqual(3, swordCd1.TriggerCountThisCombat);
            Assert.AreEqual(2, axeCd2.TriggerCountThisCombat);
            Assert.AreEqual(1, tomeCd3.TriggerCountThisCombat);

            // Turn 4: CD1 and CD3 trigger; CD2 rests
            BackpackTurnResult t4 = BackpackTriggerEngine.TriggerHeroBackpack(hero, party, enemies);
            Assert.AreEqual(2, t4.TriggeredItems.Count);
            Assert.AreEqual(4, swordCd1.TriggerCountThisCombat);
            Assert.AreEqual(2, axeCd2.TriggerCountThisCombat);
            Assert.AreEqual(2, tomeCd3.TriggerCountThisCombat);
        }

        [Test]
        public void StatusEffectSystem_PoisonBypassesShield_BleedTriggersOnAction_StunSkipsTurn()
        {
            HeroCombatant hero = new HeroCombatant(DefaultGameCatalog.GetHeroById("hero_crusader"), PartyPosition.Frontline, populateStartingItems: false);
            hero.MaxHP = 100;
            hero.CurrentHP = 100;
            hero.Shield = 30;

            // 1. Standard damage is absorbed by Shield
            int hpLostFromHit = StatusEffectSystem.ApplyStandardDamage(hero, 20, out int absorbed);
            Assert.AreEqual(0, hpLostFromHit);
            Assert.AreEqual(20, absorbed);
            Assert.AreEqual(10, hero.Shield);
            Assert.AreEqual(100, hero.CurrentHP);

            // 2. Poison bypasses remaining 10 Shield completely
            StatusEffectSystem.ApplyStatus(hero, StatusType.Poison, 6);
            int poisonDmg = StatusEffectSystem.ProcessTurnStartPoison(hero);
            Assert.AreEqual(6, poisonDmg);
            Assert.AreEqual(94, hero.CurrentHP, "Poison must bypass Shield and reduce HP directly");
            Assert.AreEqual(10, hero.Shield, "Shield must remain untouched by Poison");
            Assert.AreEqual(5, hero.PoisonStacks, "Poison should decay by 1 after triggering");

            // 3. Bleed triggers when performing an offensive action
            StatusEffectSystem.ApplyStatus(hero, StatusType.Bleed, 4);
            int bleedDmg = StatusEffectSystem.ProcessBleedOnAction(hero);
            Assert.AreEqual(4, bleedDmg);
            Assert.AreEqual(90, hero.CurrentHP);
            Assert.AreEqual(3, hero.BleedStacks);

            // 4. Stun pushes back ActionGauge and skips turn
            hero.ActionGauge = 80f;
            StatusEffectSystem.ApplyStatus(hero, StatusType.Stun, 1);
            Assert.Less(hero.ActionGauge, 80f, "Stun must push back ActionGauge immediately");
            Assert.IsTrue(StatusEffectSystem.ConsumeStunIfPresent(hero));
            Assert.AreEqual(0, hero.StunStacks);
        }

        [Test]
        public void StatusEffectSystem_TurnStartShieldDecay_HalvesUnconsumedShieldAndPreventsInfiniteStacking()
        {
            HeroCombatant hero = new HeroCombatant(DefaultGameCatalog.GetHeroById("hero_crusader"), PartyPosition.Frontline, populateStartingItems: false);
            hero.Shield = 20;

            int decayed = StatusEffectSystem.ProcessTurnStartShieldDecay(hero);
            Assert.AreEqual(10, decayed);
            Assert.AreEqual(10, hero.Shield, "Unconsumed Shield should decay by 50% at the start of the unit's turn");

            // Also verify CombatManager applies turn-start shield decay before adding new backpack shield
            hero.PersonalBackpack.TryAutoPlaceItem(new ItemData(DefaultGameCatalog.GetItemById("item_buckler"))); // +10 Shield
            MonsterCombatant m = new MonsterCombatant(DefaultGameCatalog.GetMonsterById("monster_ghoul"), 1.0f);
            CombatManager cm = new CombatManager();
            cm.StartCombat(new List<HeroCombatant> { hero }, new List<MonsterCombatant> { m }, null);
            hero.Shield = 20;

            cm.ExecuteCombatantTurn(hero);
            // 20 decays to 10 at turn start, then buckler adds +10 -> 20 (instead of snowballing to 30)
            Assert.AreEqual(20, hero.Shield);
        }

        [Test]
        public void BackpackTriggerEngine_MultiEnemyCleaveScythe_WithShieldPoisonBleedOnHighFloor()
        {
            HeroCombatant hero = new HeroCombatant(DefaultGameCatalog.GetHeroById("hero_crusader"), PartyPosition.Frontline, populateStartingItems: false);
            hero.PersonalBackpack.UnlockNextLockedSlots(20);

            ItemData scythe = new ItemData(DefaultGameCatalog.GetItemById("item_plague_scythe")); // T-Shape, targetAll: true, 24 phys, 4 poison, 4 bleed
            Assert.IsTrue(hero.PersonalBackpack.TryPlaceItem(scythe, 0, 0, 0));

            // Floor 10+ difficulty (~2.5x) with 3 monsters that already have Shield + Poison + Bleed
            MonsterCombatant m1 = new MonsterCombatant(DefaultGameCatalog.GetMonsterById("monster_ghoul"), 2.5f) { Shield = 15, PoisonStacks = 3, BleedStacks = 2 };
            MonsterCombatant m2 = new MonsterCombatant(DefaultGameCatalog.GetMonsterById("monster_cultist"), 2.5f) { Shield = 30, PoisonStacks = 4, BleedStacks = 3 };
            MonsterCombatant m3 = new MonsterCombatant(DefaultGameCatalog.GetMonsterById("monster_gargoyle"), 2.5f) { Shield = 10, PoisonStacks = 5, BleedStacks = 4 };
            int m1HpBefore = m1.CurrentHP;
            int m2HpBefore = m2.CurrentHP;
            int m3HpBefore = m3.CurrentHP;

            var party = new List<HeroCombatant> { hero };
            var enemies = new List<MonsterCombatant> { m1, m2, m3 };

            BackpackTurnResult turnRes = BackpackTriggerEngine.TriggerHeroBackpack(hero, party, enemies);
            Assert.AreEqual(1, turnRes.TriggeredItems.Count);
            Assert.AreEqual(24 * 3, turnRes.TotalDamageDealt, "Scythe should cleave all 3 living monsters for 24 damage each");

            // m1 had 15 shield -> 0 shield left, 9 HP lost, +4 poison, +4 bleed
            Assert.AreEqual(0, m1.Shield);
            Assert.AreEqual(m1HpBefore - 9, m1.CurrentHP);
            Assert.AreEqual(3 + 4, m1.PoisonStacks);
            Assert.AreEqual(2 + 4, m1.BleedStacks);

            // m2 had 30 shield -> 6 shield left, 0 HP lost, +4 poison, +4 bleed
            Assert.AreEqual(6, m2.Shield);
            Assert.AreEqual(m2HpBefore, m2.CurrentHP);
            Assert.AreEqual(4 + 4, m2.PoisonStacks);
            Assert.AreEqual(3 + 4, m2.BleedStacks);

            // m3 had 10 shield -> 0 shield left, 14 HP lost, +4 poison, +4 bleed
            Assert.AreEqual(0, m3.Shield);
            Assert.AreEqual(m3HpBefore - 14, m3.CurrentHP);
            Assert.AreEqual(5 + 4, m3.PoisonStacks);
            Assert.AreEqual(4 + 4, m3.BleedStacks);
        }

        [Test]
        public void CombatManager_WhenTargetedHeroFalls_RefreshesLivingMonsterIntentTargetAndUsesVietnameseStatusNames()
        {
            HeroCombatant h0 = new HeroCombatant(DefaultGameCatalog.GetHeroById("hero_crusader"), PartyPosition.Frontline, populateStartingItems: false);
            HeroCombatant h1 = new HeroCombatant(DefaultGameCatalog.GetHeroById("hero_alchemist"), PartyPosition.Midline, populateStartingItems: false);
            MonsterCombatant m0 = new MonsterCombatant(DefaultGameCatalog.GetMonsterById("monster_ghoul"), 1.0f);
            MonsterCombatant m1 = new MonsterCombatant(DefaultGameCatalog.GetMonsterById("monster_ghoul"), 1.0f);

            var party = new List<HeroCombatant> { h0, h1 };
            var monsters = new List<MonsterCombatant> { m0, m1 };

            CombatManager cm = new CombatManager();
            cm.StartCombat(party, monsters, null);

            // Both monsters initially target Frontline Hero 0
            Assert.AreEqual(0, m1.CurrentIntent.TargetHeroIndex);
            Assert.AreEqual(h0.DisplayName, m1.CurrentIntent.TargetHeroName);

            // Hero 0 drops to 1 HP and Monster 0 kills Hero 0 on its turn
            h0.CurrentHP = 1;
            h0.Shield = 0;
            cm.ExecuteCombatantTurn(m0);

            Assert.IsFalse(h0.IsAlive);
            // Monster 1's pre-announced intent must immediately update to living Hero 1
            Assert.AreEqual(1, m1.CurrentIntent.TargetHeroIndex);
            Assert.AreEqual(h1.DisplayName, m1.CurrentIntent.TargetHeroName);
            Assert.AreEqual("Chảy máu", StatusEffectSystem.GetStatusVietnameseName(StatusType.Bleed));
        }
    }
}
