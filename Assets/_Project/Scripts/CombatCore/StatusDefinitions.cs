using System;
using System.Collections.Generic;

namespace Project05.CombatCore
{
    /// <summary>What a status does (debuffs-and-detonation.md). Stun is a control state, never a debuff.</summary>
    public enum StatusBehavior { Stun = 0, Poison = 1, Burn = 2, Frost = 3, Slow = 4, Weaken = 5 }

    /// <summary>
    /// One status kind, loaded from the "statuses" array of items_mvp.json. Every number is data (provisional): a timed
    /// kind expires after DurationActions completed actions of its carrier (0 = no timer); stacks above Cap are thrown
    /// away; TickPerStack is damage per stack at the start of the carrier's own turn, after which DecayPerTick stacks
    /// are removed oldest first; reaching FreezeAt converts every stack into a Stun of FreezeStunActions; the speed and
    /// outgoing-damage terms are permille per stack; DetonateValue is the damage per stack a detonation reads.
    /// </summary>
    public sealed class StatusDef
    {
        public string Id { get; }
        public StatusBehavior Behavior { get; }
        public int DurationActions { get; }
        public int Cap { get; }
        public int TickPerStack { get; }
        public int DecayPerTick { get; }
        public int FreezeAt { get; }
        public int FreezeStunActions { get; }
        public int SpeedPermillePerStack { get; }
        public int DamagePermillePerStack { get; }
        public int DetonateValue { get; }
        public string NameVi { get; }
        public string NameEn { get; }

        /// <summary>True for the kinds a detonation consumes (everything but Stun).</summary>
        public bool IsDebuff => Behavior != StatusBehavior.Stun;

        public StatusDef(string id, StatusBehavior behavior, int durationActions, int cap, int tickPerStack, int decayPerTick,
            int freezeAt, int freezeStunActions, int speedPermillePerStack, int damagePermillePerStack, int detonateValue,
            string nameVi, string nameEn)
        {
            Id = id ?? string.Empty;
            Behavior = behavior;
            DurationActions = durationActions;
            Cap = cap;
            TickPerStack = tickPerStack;
            DecayPerTick = decayPerTick;
            FreezeAt = freezeAt;
            FreezeStunActions = freezeStunActions;
            SpeedPermillePerStack = speedPermillePerStack;
            DamagePermillePerStack = damagePermillePerStack;
            DetonateValue = detonateValue;
            NameVi = nameVi ?? string.Empty;
            NameEn = nameEn ?? string.Empty;
        }

        /// <summary>The built-in Stun used when no catalog entry exists (one skipped action).</summary>
        public static readonly StatusDef DefaultStun = new StatusDef("stun", StatusBehavior.Stun, 1, 1, 0, 0, 0, 0, 0, 0, 0, "Choáng", "Stun");
    }

    /// <summary>Key prefix for DetonateValue synergy bonuses kept beside the status-amount bonuses of an item.</summary>
    public static class DetonateKeys
    {
        /// <summary>Bonus key for one status kind ("detonate:poison"), or for every kind with an empty id ("detonate:").</summary>
        public static string For(string statusId) => "detonate:" + (statusId ?? string.Empty);
    }
}
