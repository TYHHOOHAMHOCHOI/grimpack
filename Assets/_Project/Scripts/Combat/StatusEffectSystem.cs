using UnityEngine;
using Project05.Core;

namespace Project05.Combat
{
    /// <summary>
    /// Handles RPG status effects:
    /// - Shield (Giáp): Absorbs Physical & Magic damage before HP.
    /// - Poison (Độc): Deals direct HP damage (ignores Shield) at the start of the unit's turn, then decays by 1.
    /// - Bleed (Chảy máu): Deals direct HP damage when the unit performs an offensive/active action, then decays by 1.
    /// - Stun (Choáng): Pushes back ActionGauge and skips the unit's action on their turn, consuming 1 stack.
    /// </summary>
    public static class StatusEffectSystem
    {
        public const float StunGaugePushback = 35f;

        /// <summary>
        /// Applies standard Physical or Magic damage, absorbed by Shield first.
        /// Returns actual HP lost.
        /// </summary>
        public static int ApplyStandardDamage(Combatant target, int rawDamage, out int absorbedByShield)
        {
            absorbedByShield = 0;
            if (target == null || !target.IsAlive || rawDamage <= 0)
            {
                return 0;
            }

            int remaining = rawDamage;
            if (target.Shield > 0)
            {
                absorbedByShield = Mathf.Min(target.Shield, remaining);
                target.Shield -= absorbedByShield;
                remaining -= absorbedByShield;
            }

            if (remaining > 0)
            {
                int prevHp = target.CurrentHP;
                target.CurrentHP = Mathf.Max(0, target.CurrentHP - remaining);
                return prevHp - target.CurrentHP;
            }

            return 0;
        }

        /// <summary>
        /// Applies direct HP damage that bypasses Shield completely (used by Poison & Bleed).
        /// </summary>
        public static int ApplyDirectHpDamage(Combatant target, int directDamage)
        {
            if (target == null || !target.IsAlive || directDamage <= 0)
            {
                return 0;
            }

            int prevHp = target.CurrentHP;
            target.CurrentHP = Mathf.Max(0, target.CurrentHP - directDamage);
            return prevHp - target.CurrentHP;
        }

        /// <summary>
        /// Inflicts a status effect on the target. Stun immediately pushes back the target's ActionGauge.
        /// </summary>
        public static void ApplyStatus(Combatant target, StatusType statusType, int stacks)
        {
            if (target == null || !target.IsAlive || stacks <= 0) return;

            switch (statusType)
            {
                case StatusType.Poison:
                    target.PoisonStacks += stacks;
                    break;
                case StatusType.Bleed:
                    target.BleedStacks += stacks;
                    break;
                case StatusType.Stun:
                    target.StunStacks += stacks;
                    target.ActionGauge = Mathf.Max(0f, target.ActionGauge - (StunGaugePushback * stacks));
                    break;
            }
        }

        /// <summary>
        /// Processes start-of-turn Shield partial decay (retains 50% of previous shield so shield cannot snowball infinitely).
        /// Returns the amount of shield lost to decay.
        /// </summary>
        public static int ProcessTurnStartShieldDecay(Combatant unit)
        {
            if (unit == null || !unit.IsAlive || unit.Shield <= 0)
            {
                return 0;
            }

            int before = unit.Shield;
            unit.Shield = before / 2;
            return before - unit.Shield;
        }

        /// <summary>
        /// Processes start-of-turn Poison direct HP damage (ignoring Shield), then decays Poison by 1.
        /// </summary>
        public static int ProcessTurnStartPoison(Combatant unit)
        {
            if (unit == null || !unit.IsAlive || unit.PoisonStacks <= 0)
            {
                return 0;
            }

            int dmg = ApplyDirectHpDamage(unit, unit.PoisonStacks);
            unit.PoisonStacks = Mathf.Max(0, unit.PoisonStacks - 1);
            return dmg;
        }

        /// <summary>
        /// Checks if the unit is Stunned at the start of their action. If so, consumes 1 Stun stack and returns true (skip turn).
        /// </summary>
        public static bool ConsumeStunIfPresent(Combatant unit)
        {
            if (unit == null || !unit.IsAlive) return false;
            if (unit.StunStacks > 0)
            {
                unit.StunStacks--;
                unit.ActionGauge = 0f;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Triggered when a unit executes an active offensive turn while bleeding.
        /// Deals direct HP damage ignoring Shield, then decays Bleed by 1.
        /// </summary>
        public static int ProcessBleedOnAction(Combatant unit)
        {
            if (unit == null || !unit.IsAlive || unit.BleedStacks <= 0)
            {
                return 0;
            }

            int dmg = ApplyDirectHpDamage(unit, unit.BleedStacks);
            unit.BleedStacks = Mathf.Max(0, unit.BleedStacks - 1);
            return dmg;
        }

        public static string GetStatusVietnameseName(StatusType statusType)
        {
            switch (statusType)
            {
                case StatusType.Poison: return "Độc";
                case StatusType.Bleed: return "Chảy máu";
                case StatusType.Stun: return "Choáng";
                default: return statusType.ToString();
            }
        }
    }
}
