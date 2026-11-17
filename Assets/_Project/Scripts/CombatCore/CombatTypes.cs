using System;

namespace Project05.CombatCore
{
    public enum TierKind { White = 0, Green = 1, Blue = 2, Purple = 3, Gold = 4, Red = 5 }
    public enum TriggerKind { Passive = 0, Cooldown = 1, Reaction = 2, CombatStart = 3 }
    public enum EffectKind { Damage = 0, ApplyStatus = 1, ConsumeDetonate = 2, ShieldGain = 3, Heal = 4, DetonateAll = 5 }
    public enum TargetKind { FirstEnemy = 0, AllEnemies = 1, Self = 2, AllHeroes = 3, LowestHpHero = 4, BoundHero = 5 }
    public enum SynergyKind { DamageFlat = 0, DamagePercent = 1, CooldownReduction = 2, StatusAmount = 3, DetonateValue = 4 }
    public enum ReactionEventKind { None = 0, ItemFired = 1, StatusApplied = 2, StatusConsumed = 3, Damage = 4, UnitDowned = 5 }
    public enum ReactionSideFilter { Any = 0, Hero = 1, Enemy = 2, OwnHero = 3, OtherHero = 4 }

    public enum MonsterClassKind { Regular = 0, Elite = 1, Boss = 2 }
    public enum MonsterSizeKind { Small = 0, Medium = 1, Large = 2, Boss = 3 }
    public enum MoveGlyphKind { Attack = 0, Windup = 1 }
    public enum MoveTargetMode { None = 0, RandomHero = 1, AllHeroes = 2, SameAsMove = 3 }
    public enum DeliveryKind { Swing = 0, Throw = 1 }

    public enum ContainerKind { Hero0 = 0, Hero1 = 1, Hero2 = 2, Stash = 100, Tray = 200 }

    public enum GridOpCode
    {
        Ok = 0,
        NoEffect = 1,
        NothingToUndo = 2,
        NoPlacement = 3,
        Frozen = 4,
        ContractViolation = 5,
        UnknownContainer = 6,
        UnknownItem = 7,
        InvalidOrientation = 8,
        RotationDisabled = 9,
        OutOfBounds = 10,
        LockedCell = 11,
        Occupied = 12,
        BufferTooSmall = 13
    }

    public enum UnitSide { Hero = 0, Enemy = 1 }
    public enum CombatOutcome { Victory = 0, Defeat = 1, Draw = 2 }
    public enum CombatRunStatus { Completed = 0, TechnicalStop = 1 }

    public enum CombatEventKind
    {
        IntentsBound = 0,
        ActionStarted = 1,
        ActionSkipped = 2,
        ItemFired = 3,
        StatusApplied = 4,
        StatusConsumed = 5,
        StatusExpired = 6,
        Damage = 7,
        DetonationDamage = 8,
        ShieldGained = 9,
        Healed = 10,
        UnitDowned = 11,
        ChainLimit = 12,
        ListenerSuppressed = 13,
        EffectSkipped = 14,
        DetonationEmpty = 15,
        CombatEnded = 16
    }

    public enum OriginKind { None = 0, Item = 1, EnemyMove = 2, StatusExpiry = 3, EncounterSetup = 4, Curse = 5, StatusTick = 6 }

    public readonly struct GridCell : IEquatable<GridCell>
    {
        public readonly int X;
        public readonly int Y;

        public GridCell(int x, int y)
        {
            X = x;
            Y = y;
        }

        public bool Equals(GridCell other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is GridCell other && Equals(other);
        public override int GetHashCode() => (Y * 397) ^ X;
        public static bool operator ==(GridCell left, GridCell right) => left.Equals(right);
        public static bool operator !=(GridCell left, GridCell right) => !left.Equals(right);
        public override string ToString() => "(" + X + "," + Y + ")";
    }

    public readonly struct UnitRef : IEquatable<UnitRef>
    {
        public readonly UnitSide Side;
        public readonly int Slot;
        public readonly int InstanceId;

        public UnitRef(UnitSide side, int slot, int instanceId)
        {
            Side = side;
            Slot = slot;
            InstanceId = instanceId;
        }

        public static UnitRef None => new UnitRef(UnitSide.Hero, -1, -1);
        public bool IsValid => Slot >= 0 && InstanceId >= 0;

        public bool Equals(UnitRef other) =>
            Side == other.Side && Slot == other.Slot && InstanceId == other.InstanceId;

        public override bool Equals(object obj) => obj is UnitRef other && Equals(other);

        public override int GetHashCode()
        {
            int hash = (int)Side;
            hash = (hash * 397) ^ Slot;
            hash = (hash * 397) ^ InstanceId;
            return hash;
        }

        public static bool operator ==(UnitRef left, UnitRef right) => left.Equals(right);
        public static bool operator !=(UnitRef left, UnitRef right) => !left.Equals(right);
        public override string ToString() => Side + "[" + Slot + "]#" + InstanceId;
    }
}
