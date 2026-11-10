using System;
using System.Collections.Generic;
using UnityEngine;
using Project05.Data;
using Project05.Backpack;

namespace Project05.Combat
{
    public enum CombatOutcome
    {
        Ongoing = 0,
        HeroVictory = 1,
        HeroDefeat = 2
    }

    /// <summary>
    /// Coordinates turn-based battles between the Hero Party (1..3 characters) and Monster formations.
    /// Integrates SpeedGaugeManager, BackpackTriggerEngine, StatusEffectSystem, and MonsterIntentController.
    /// </summary>
    public class CombatManager
    {
        private readonly SpeedGaugeManager _speedGauge = new SpeedGaugeManager();
        private readonly BackpackTurnResult _turnResultBuffer = new BackpackTurnResult();
        private readonly List<string> _combatLogs = new List<string>(32);

        public IReadOnlyList<HeroCombatant> Heroes { get; private set; }
        public IReadOnlyList<MonsterCombatant> Monsters { get; private set; }
        public IReadOnlyList<BuffDefinitionSO> ActiveRunBuffs { get; private set; }
        public IReadOnlyList<string> CombatLogs => _combatLogs;

        public CombatOutcome Outcome { get; private set; } = CombatOutcome.Ongoing;
        public int TurnCounter { get; private set; }
        public Combatant LastActingCombatant { get; private set; }

        public event Action<Combatant, string> OnTurnExecuted;
        public event Action<CombatOutcome> OnCombatEnded;

        public void StartCombat(
            IReadOnlyList<HeroCombatant> heroes,
            IReadOnlyList<MonsterCombatant> monsters,
            IReadOnlyList<BuffDefinitionSO> activeRunBuffs)
        {
            Heroes = heroes;
            Monsters = monsters;
            ActiveRunBuffs = activeRunBuffs;
            Outcome = CombatOutcome.Ongoing;
            TurnCounter = 0;
            LastActingCombatant = null;
            _combatLogs.Clear();

            int bonusStartShield = 0;
            if (activeRunBuffs != null)
            {
                for (int i = 0; i < activeRunBuffs.Count; i++)
                {
                    if (activeRunBuffs[i] != null)
                    {
                        bonusStartShield += activeRunBuffs[i].BonusStartingShield;
                    }
                }
            }

            if (Heroes != null)
            {
                for (int i = 0; i < Heroes.Count; i++)
                {
                    HeroCombatant h = Heroes[i];
                    if (h == null || !h.IsAlive) continue;
                    h.ResetCombatStatuses();
                    h.RecalculateSynergies(ActiveRunBuffs);
                    h.AddShield(h.Definition.StartingShield + bonusStartShield);
                    BackpackTriggerEngine.ResetCooldownsForCombatStart(h.PersonalBackpack);
                }
            }

            if (Monsters != null)
            {
                for (int i = 0; i < Monsters.Count; i++)
                {
                    MonsterCombatant m = Monsters[i];
                    if (m == null || !m.IsAlive) continue;
                    m.ResetCombatStatuses();
                    MonsterIntentController.RollNextIntent(m, Heroes);
                }
            }

            AppendLog("⚔ Trận chiến bắt đầu! Thanh tốc độ đang nạp...");
            EvaluateOutcome();
        }

        public bool TickRealTime(float deltaTime, float speedScale = 2.2f)
        {
            if (Outcome != CombatOutcome.Ongoing) return false;

            Combatant ready = _speedGauge.TickGauges(Heroes, Monsters, deltaTime, speedScale);
            if (ready != null)
            {
                ExecuteCombatantTurn(ready);
                return true;
            }

            return false;
        }

        public Combatant StepNextTurnInstant()
        {
            if (Outcome != CombatOutcome.Ongoing) return null;

            Combatant ready = _speedGauge.AdvanceToNextTurnInstant(Heroes, Monsters);
            if (ready != null)
            {
                ExecuteCombatantTurn(ready);
            }
            return ready;
        }

        public void ExecuteCombatantTurn(Combatant actor)
        {
            if (actor == null || !actor.IsAlive || Outcome != CombatOutcome.Ongoing) return;

            TurnCounter++;
            LastActingCombatant = actor;
            SpeedGaugeManager.ConsumeTurnGauge(actor);

            // 0. Start-of-turn Shield partial decay (retains 50% of previous unconsumed shield)
            StatusEffectSystem.ProcessTurnStartShieldDecay(actor);

            // 1. Start-of-turn Poison (ignores Shield)
            if (actor.PoisonStacks > 0)
            {
                int poisonDmg = StatusEffectSystem.ProcessTurnStartPoison(actor);
                AppendLog($"☣ {actor.DisplayName} mất {poisonDmg} HP vì Độc (bỏ qua Giáp)!");
                if (!actor.IsAlive)
                {
                    AppendLog($"💀 {actor.DisplayName} đã gục ngã vì Độc!");
                    RefreshLivingMonsterIntentTargets();
                    OnTurnExecuted?.Invoke(actor, $"{actor.DisplayName} gục ngã vì Độc!");
                    EvaluateOutcome();
                    return;
                }
            }

            // 2. Check Stun (skip turn if stunned)
            if (StatusEffectSystem.ConsumeStunIfPresent(actor))
            {
                string stunMsg = $"⚡ {actor.DisplayName} bị CHOÁNG, mất lượt hành động!";
                AppendLog(stunMsg);
                OnTurnExecuted?.Invoke(actor, stunMsg);
                EvaluateOutcome();
                return;
            }

            // 3. Execute Hero Backpack or Monster Intent
            if (actor is HeroCombatant hero)
            {
                BackpackTriggerEngine.TriggerHeroBackpackInto(hero, Heroes, Monsters, ActiveRunBuffs, _turnResultBuffer);
                string summary = FormatHeroTurnLog(hero, _turnResultBuffer);
                AppendLog(summary);
                RefreshLivingMonsterIntentTargets();
                OnTurnExecuted?.Invoke(hero, summary);
            }
            else if (actor is MonsterCombatant monster)
            {
                string monsterLog = MonsterIntentController.ExecuteIntent(monster, Heroes);
                AppendLog(monsterLog);
                RefreshLivingMonsterIntentTargets();
                if (monster.IsAlive)
                {
                    MonsterIntentController.RollNextIntent(monster, Heroes);
                }
                OnTurnExecuted?.Invoke(monster, monsterLog);
            }

            EvaluateOutcome();
        }

        private void RefreshLivingMonsterIntentTargets()
        {
            if (Monsters == null || Heroes == null) return;
            for (int i = 0; i < Monsters.Count; i++)
            {
                MonsterCombatant m = Monsters[i];
                if (m != null && m.IsAlive)
                {
                    MonsterIntentController.RefreshIntentTargetIfDead(m, Heroes);
                }
            }
        }

        private string FormatHeroTurnLog(HeroCombatant hero, BackpackTurnResult res)
        {
            int trigCount = res.TriggeredItems.Count;
            if (trigCount == 0)
            {
                return $"🎒 Lượt {hero.DisplayName}: Các trang bị trong Balo đang hồi chiêu ({res.CooldownTickedItems.Count} món giảm 1 lượt CD).";
            }

            string msg = $"🎒 Lượt {hero.DisplayName}: Kích hoạt {trigCount} trang bị -> Gây {res.TotalDamageDealt} ST";
            if (res.TotalShieldGained > 0) msg += $", +{res.TotalShieldGained} Giáp";
            if (res.TotalHealGained > 0) msg += $", +{res.TotalHealGained} Hồi máu";
            if (res.BleedSelfDamageTaken > 0) msg += $" (Mất {res.BleedSelfDamageTaken} HP do Chảy máu)";
            return msg;
        }

        private void EvaluateOutcome()
        {
            if (Outcome != CombatOutcome.Ongoing) return;

            bool anyMonsterAlive = false;
            if (Monsters != null)
            {
                for (int i = 0; i < Monsters.Count; i++)
                {
                    if (Monsters[i] != null && Monsters[i].IsAlive)
                    {
                        anyMonsterAlive = true;
                        break;
                    }
                }
            }

            bool anyHeroAlive = false;
            if (Heroes != null)
            {
                for (int i = 0; i < Heroes.Count; i++)
                {
                    if (Heroes[i] != null && Heroes[i].IsAlive)
                    {
                        anyHeroAlive = true;
                        break;
                    }
                }
            }

            if (!anyMonsterAlive)
            {
                Outcome = CombatOutcome.HeroVictory;
                AppendLog("🏆 CHIẾN THẮNG! Quái vật đã bị tiêu diệt. Hãy chọn 1 trong 3 Cánh Cửa tiếp theo!");
                OnCombatEnded?.Invoke(Outcome);
            }
            else if (!anyHeroAlive)
            {
                Outcome = CombatOutcome.HeroDefeat;
                AppendLog("☠ THẤT BẠI... Toàn bộ đội hình đã ngã xuống trong hầm ngục.");
                OnCombatEnded?.Invoke(Outcome);
            }
        }

        private void AppendLog(string message)
        {
            if (string.IsNullOrEmpty(message)) return;
            _combatLogs.Add(message);
            if (_combatLogs.Count > 30)
            {
                _combatLogs.RemoveAt(0);
            }
        }
    }
}
