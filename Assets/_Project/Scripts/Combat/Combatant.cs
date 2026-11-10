using UnityEngine;

namespace Project05.Combat
{
    /// <summary>
    /// Base combatant domain model shared by Party Heroes and Enemy Monsters.
    /// </summary>
    public abstract class Combatant
    {
        public string DisplayName { get; protected set; }
        public int MaxHP { get; set; }
        public int CurrentHP { get; set; }
        public int Shield { get; set; }
        public int BaseSpeed { get; set; }
        public int BonusCombatSpeed { get; set; }
        public float ActionGauge { get; set; }

        public int PoisonStacks { get; set; }
        public int BleedStacks { get; set; }
        public int StunStacks { get; set; }

        public abstract bool IsHero { get; }
        public bool IsAlive => CurrentHP > 0;

        public virtual int GetEffectiveSpeed()
        {
            return Mathf.Max(1, BaseSpeed + BonusCombatSpeed);
        }

        public int Heal(int amount)
        {
            if (!IsAlive || amount <= 0) return 0;
            int before = CurrentHP;
            CurrentHP = Mathf.Min(MaxHP, CurrentHP + amount);
            return CurrentHP - before;
        }

        public void AddShield(int amount)
        {
            if (!IsAlive || amount <= 0) return;
            Shield += amount;
        }

        public void ResetCombatStatuses()
        {
            Shield = 0;
            ActionGauge = 0f;
            BonusCombatSpeed = 0;
            PoisonStacks = 0;
            BleedStacks = 0;
            StunStacks = 0;
        }
    }
}
