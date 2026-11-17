using System;
using System.Collections.Generic;

namespace Project05.UI.Hud
{
    /// <summary>Key poses of a hero (art bible 5.6). Ready is a state, not a pose: idle 2 with narrow eyes and a 1 px lift.</summary>
    public enum HeroPose { Idle1, Idle2, Hurt1, Hurt2, Down1, Down2 }

    /// <summary>Key poses of a monster (art bible 5.6).</summary>
    public enum MonsterPose { Idle1, Idle2, Act, Hurt, Die1, Die2 }

    /// <summary>Intent glyph kinds (art bible 7.8, enemy GDD move vocabulary).</summary>
    public enum IntentKind { None, Attack, Multi, Sweep, Windup }

    /// <summary>Status glyph kinds. Ấn has its own drawing; the droplet is poison (7.8).</summary>
    public enum StatusKind { Droplet, Seal, Poison, Burn, Frost, Slow, Weaken, Stun }

    /// <summary>Glyph drawings available as plate icons (placeholders until item art exists).</summary>
    public enum GlyphKind { Attack, Multi, Sweep, Windup, Heart, Shield, Speed, Damage, Droplet, Seal, Menu, Burn, Frost, Slow, Weaken, Detonate, Anvil }

    /// <summary>Item tiers, in rarity order.</summary>
    public enum Tier { White, Green, Blue, Purple, Gold, Red }

    /// <summary>One status chip: glyph and stack amount. Amounts are never capped by the display (combat HUD rule 10).</summary>
    [Serializable]
    public sealed class StatusChipData
    {
        public StatusKind Kind;
        public int Amount;
    }

    /// <summary>An item avatar floating above the acting hero (art bible 5.10).</summary>
    [Serializable]
    public sealed class AvatarData
    {
        public Tier Tier;
        public GlyphKind Icon;
        public bool Bright;
    }

    /// <summary>Everything one hero slot shows. Read-only view data: the HUD never owns or changes game state.</summary>
    [Serializable]
    public sealed class HeroSlotData
    {
        public int JoinNumeral = 1;
        public int Hp = 1;
        public int MaxHp = 1;
        public int Armour;
        public int SpeedGauge;
        public HeroPose Pose;
        public bool Ready;
        public bool Acting;
        public bool Fallen;
        public List<StatusChipData> Statuses = new List<StatusChipData>();
        public List<AvatarData> Avatars = new List<AvatarData>();
    }

    /// <summary>Everything one enemy slot shows.</summary>
    [Serializable]
    public sealed class EnemySlotData
    {
        public bool Present;
        public int Visual;
        public int Hp = 1;
        public int MaxHp = 1;
        public int Armour;
        public int SpeedGauge;
        public MonsterPose Pose;
        public IntentKind Intent;
        public bool IntentAway;
        public string Magnitude = string.Empty;
        public int TargetJoinNumeral;
        public bool EyesLit;
        public bool Acting;
        public List<StatusChipData> Statuses = new List<StatusChipData>();
    }

    /// <summary>One chip of the order strip: a hero join numeral or an enemy intent glyph.</summary>
    [Serializable]
    public sealed class OrderEntryData
    {
        public bool IsHero;
        public int JoinNumeral;
        public IntentKind Intent;

        /// <summary>For an enemy: its slot (0 = A, 1 = B, ...), the same letter its stage slot shows.</summary>
        public int EnemySlot;
    }

    /// <summary>A number rising from or falling on a unit (art bible 4.6, 7.5).</summary>
    [Serializable]
    public sealed class ReadoutData
    {
        public bool OnHero;
        public int Slot;
        public string Text = string.Empty;
        public bool Taken;
        public int Cut = 16;
    }

    /// <summary>The whole combat HUD for one presentation step.</summary>
    [Serializable]
    public sealed class CombatHudData
    {
        public const int HeroCount = 3;
        public const int EnemyCount = 6;

        public HeroSlotData[] Heroes = new HeroSlotData[HeroCount];
        public EnemySlotData[] Enemies = new EnemySlotData[EnemyCount];
        public List<OrderEntryData> Order = new List<OrderEntryData>();
        public List<ReadoutData> Readouts = new List<ReadoutData>();
        public int SpeedIndex;
        public int Currency;
        public int Room = 1;
        public int RoomCount = 8;
    }
}
