using System;
using System.Collections.Generic;

namespace Project05.CombatCore
{
    public sealed class TriggerDef
    {
        public TriggerKind Kind { get; }
        public int N { get; }
        public int S { get; }
        public ReactionEventKind EventKind { get; }
        public string EventStatus { get; }
        public ReactionSideFilter EventSide { get; }

        public TriggerDef(
            TriggerKind kind,
            int n,
            int s,
            ReactionEventKind eventKind = ReactionEventKind.None,
            string eventStatus = "",
            ReactionSideFilter eventSide = ReactionSideFilter.Any)
        {
            Kind = kind;
            N = n;
            S = s;
            EventKind = eventKind;
            EventStatus = eventStatus ?? string.Empty;
            EventSide = eventSide;
        }
    }

    public sealed class EffectDef
    {
        public EffectKind Kind { get; }
        public TargetKind Target { get; }
        public int Base { get; }
        public string Status { get; }
        public int Amount { get; }
        public int Units { get; }
        public int PerUnit { get; }

        public EffectDef(
            EffectKind kind,
            TargetKind target,
            int baseDamage,
            string status,
            int amount,
            int units,
            int perUnit)
        {
            Kind = kind;
            Target = target;
            Base = baseDamage;
            Status = status ?? string.Empty;
            Amount = amount;
            Units = units;
            PerUnit = perUnit;
        }
    }

    public sealed class SynergyRuleDef
    {
        public IReadOnlyList<string> TargetTags { get; }
        public SynergyKind Kind { get; }
        public int Amount { get; }
        public string StatusId { get; }

        public SynergyRuleDef(IReadOnlyList<string> targetTags, SynergyKind kind, int amount, string statusId)
        {
            TargetTags = targetTags ?? Array.Empty<string>();
            Kind = kind;
            Amount = amount;
            StatusId = statusId ?? string.Empty;
        }
    }

    public sealed class ItemDef
    {
        public string Id { get; }
        public string Art { get; }
        public TierKind Tier { get; }
        public string Footprint { get; }
        public IReadOnlyList<GridCell> Cells { get; }
        public IReadOnlyList<string> Tags { get; }
        public TriggerDef Trigger { get; }
        public IReadOnlyList<EffectDef> Effects { get; }
        public int HpBonus { get; }
        public int SpeedBonus { get; }
        public IReadOnlyList<SynergyRuleDef> Synergy { get; }
        public string NameVi { get; }
        public string NameEn { get; }
        public string FlavorVi { get; }
        public string FlavorEn { get; }

        /// <summary>The id of this item's upgraded version (item-upgrades.md), or empty when it has none.</summary>
        public string UpgradeId { get; }

        /// <summary>For an upgraded (+) item, the id of its base item; empty for a base item.</summary>
        public string UpgradeOf { get; }

        /// <summary>Gold paid when a room is won while the item sits in a hero grid (e.g. Coin Purse).</summary>
        public int GoldOnWin { get; }

        /// <summary>True for an upgraded (+) item.</summary>
        public bool IsUpgraded => !string.IsNullOrEmpty(UpgradeOf);

        public bool HasCooldown => Trigger.Kind == TriggerKind.Cooldown || Trigger.Kind == TriggerKind.Reaction;

        public bool DealsDamage
        {
            get
            {
                for (int i = 0; i < Effects.Count; i++)
                {
                    EffectKind k = Effects[i].Kind;
                    if (k == EffectKind.Damage || k == EffectKind.ConsumeDetonate || k == EffectKind.DetonateAll) return true;
                }
                return false;
            }
        }

        /// <summary>True when the item detonates debuffs (DetonateAll).</summary>
        public bool Detonates
        {
            get
            {
                for (int i = 0; i < Effects.Count; i++)
                {
                    if (Effects[i].Kind == EffectKind.DetonateAll) return true;
                }

                return false;
            }
        }

        /// <summary>True when the item applies this status; an empty id asks whether it applies any status.</summary>
        public bool AppliesStatus(string statusId)
        {
            for (int i = 0; i < Effects.Count; i++)
            {
                if (Effects[i].Kind == EffectKind.ApplyStatus &&
                    (string.IsNullOrEmpty(statusId) || string.Equals(Effects[i].Status, statusId, StringComparison.Ordinal)))
                {
                    return true;
                }
            }
            return false;
        }

        public bool HasTag(string tag)
        {
            for (int i = 0; i < Tags.Count; i++)
            {
                if (string.Equals(Tags[i], tag, StringComparison.Ordinal)) return true;
            }
            return false;
        }

        public ItemDef(
            string id,
            string art,
            TierKind tier,
            string footprint,
            IReadOnlyList<GridCell> cells,
            IReadOnlyList<string> tags,
            TriggerDef trigger,
            IReadOnlyList<EffectDef> effects,
            int hpBonus,
            int speedBonus,
            IReadOnlyList<SynergyRuleDef> synergy,
            string nameVi,
            string nameEn,
            string flavorVi,
            string flavorEn,
            string upgradeId = null,
            string upgradeOf = null,
            int goldOnWin = 0)
        {
            UpgradeId = upgradeId ?? string.Empty;
            UpgradeOf = upgradeOf ?? string.Empty;
            GoldOnWin = goldOnWin;
            Id = id ?? string.Empty;
            Art = art ?? string.Empty;
            Tier = tier;
            Footprint = footprint ?? string.Empty;
            Cells = cells ?? Array.Empty<GridCell>();
            Tags = tags ?? Array.Empty<string>();
            Trigger = trigger ?? new TriggerDef(TriggerKind.Passive, 0, 0);
            Effects = effects ?? Array.Empty<EffectDef>();
            HpBonus = hpBonus;
            SpeedBonus = speedBonus;
            Synergy = synergy ?? Array.Empty<SynergyRuleDef>();
            NameVi = nameVi ?? string.Empty;
            NameEn = nameEn ?? string.Empty;
            FlavorVi = flavorVi ?? string.Empty;
            FlavorEn = flavorEn ?? string.Empty;
        }
    }

    public sealed class BackpackProfileDef
    {
        public int Width { get; }
        public int Height { get; }
        public int StartWidth { get; }
        public int StartHeight { get; }
        public IReadOnlyList<GridCell> UnlockSequence { get; }

        public BackpackProfileDef(
            int width,
            int height,
            int startWidth,
            int startHeight,
            IReadOnlyList<GridCell> unlockSequence)
        {
            Width = width;
            Height = height;
            StartWidth = startWidth;
            StartHeight = startHeight;
            UnlockSequence = unlockSequence ?? Array.Empty<GridCell>();
        }
    }

    public sealed class InnateItemDef
    {
        public string Id { get; }
        public TriggerDef Trigger { get; }
        public IReadOnlyList<EffectDef> Effects { get; }
        public string NameVi { get; }
        public string NameEn { get; }
        public string DescVi { get; }
        public string DescEn { get; }

        public InnateItemDef(
            string id,
            TriggerDef trigger,
            IReadOnlyList<EffectDef> effects,
            string nameVi,
            string nameEn,
            string descVi,
            string descEn)
        {
            Id = id ?? string.Empty;
            Trigger = trigger ?? new TriggerDef(TriggerKind.CombatStart, 0, 0);
            Effects = effects ?? Array.Empty<EffectDef>();
            NameVi = nameVi ?? string.Empty;
            NameEn = nameEn ?? string.Empty;
            DescVi = descVi ?? string.Empty;
            DescEn = descEn ?? string.Empty;
        }
    }

    public sealed class HeroDef
    {
        public string Id { get; }
        public string Art { get; }
        public string NameVi { get; }
        public string NameEn { get; }
        public string RoleVi { get; }
        public string RoleEn { get; }
        public int BaseMaxHp { get; }
        public int BaseSpeed { get; }
        public int InitialShield { get; }
        public BackpackProfileDef BackpackProfile { get; }
        public IReadOnlyList<string> StartingItems { get; }
        public string SignatureItemId { get; }
        public InnateItemDef InnateItem { get; }
        /// <summary>
        /// Where each starting item sits in the starting grid (balance-three-item-start.md): one entry per starting item,
        /// X = -1 when the data gives no cell (the item is then auto-placed).
        /// </summary>
        public IReadOnlyList<StartingPlacement> StartingLayout { get; }

        public HeroDef(
            string id,
            string art,
            string nameVi,
            string nameEn,
            string roleVi,
            string roleEn,
            int baseMaxHp,
            int baseSpeed,
            int initialShield,
            BackpackProfileDef backpackProfile,
            IReadOnlyList<string> startingItems,
            string signatureItemId,
            InnateItemDef innateItem,
            IReadOnlyList<StartingPlacement> startingLayout = null)
        {
            Id = id ?? string.Empty;
            Art = art ?? string.Empty;
            NameVi = nameVi ?? string.Empty;
            NameEn = nameEn ?? string.Empty;
            RoleVi = roleVi ?? string.Empty;
            RoleEn = roleEn ?? string.Empty;
            BaseMaxHp = baseMaxHp;
            BaseSpeed = baseSpeed;
            InitialShield = initialShield;
            BackpackProfile = backpackProfile;
            StartingItems = startingItems ?? Array.Empty<string>();
            SignatureItemId = signatureItemId ?? string.Empty;
            InnateItem = innateItem;
            StartingLayout = startingLayout ?? Array.Empty<StartingPlacement>();
        }
    }

    /// <summary>A starting item's cell and quarter turns in the hero's starting grid (X = -1: auto-place).</summary>
    public readonly struct StartingPlacement
    {
        public readonly string ItemId;
        public readonly int X;
        public readonly int Y;
        public readonly int Rotation;

        public StartingPlacement(string itemId, int x, int y, int rotation)
        {
            ItemId = itemId ?? string.Empty;
            X = x;
            Y = y;
            Rotation = rotation;
        }
    }

    /// <summary>
    /// A fusion recipe (design/gdd/item-fusion.md Rule 1): 2 or 3 base ingredient items become one Gold-tier result at
    /// the Fusion Door. An ingredient may be named twice. Immutable.
    /// </summary>
    public sealed class RecipeDef
    {
        public string Id { get; }

        /// <summary>The base item ids consumed, in authored order (never + items).</summary>
        public IReadOnlyList<string> Ingredients { get; }

        /// <summary>The fused item created.</summary>
        public string ResultId { get; }

        /// <summary>How many items the recipe consumes (2 or 3); the fusion cost depends on it.</summary>
        public int Size => Ingredients.Count;

        public RecipeDef(string id, IReadOnlyList<string> ingredients, string resultId)
        {
            Id = id ?? string.Empty;
            Ingredients = ingredients ?? Array.Empty<string>();
            ResultId = resultId ?? string.Empty;
        }
    }

    public sealed class MonsterMoveDef
    {
        public string Id { get; }
        public string NameVi { get; }
        public string NameEn { get; }
        public MoveGlyphKind Glyph { get; }
        public DeliveryKind Delivery { get; }
        public int HitCount { get; }
        public int BaseDamage { get; }
        public MoveTargetMode TargetMode { get; }
        public string SameAsMoveId { get; }
        public int Weight { get; }
        public bool Heavy { get; }
        public string LinkedNextMoveId { get; }

        public MonsterMoveDef(
            string id,
            string nameVi,
            string nameEn,
            MoveGlyphKind glyph,
            DeliveryKind delivery,
            int hitCount,
            int baseDamage,
            MoveTargetMode targetMode,
            string sameAsMoveId,
            int weight,
            bool heavy,
            string linkedNextMoveId)
        {
            Id = id ?? string.Empty;
            NameVi = nameVi ?? string.Empty;
            NameEn = nameEn ?? string.Empty;
            Glyph = glyph;
            Delivery = delivery;
            HitCount = hitCount;
            BaseDamage = baseDamage;
            TargetMode = targetMode;
            SameAsMoveId = sameAsMoveId ?? string.Empty;
            Weight = weight;
            Heavy = heavy;
            LinkedNextMoveId = linkedNextMoveId ?? string.Empty;
        }
    }

    public sealed class BossPhaseDef
    {
        public int StartIndex { get; }
        public int EndIndexInclusive { get; }
        public IReadOnlyList<string> AllowedMoveIds { get; }

        public BossPhaseDef(int startIndex, int endIndexInclusive, IReadOnlyList<string> allowedMoveIds)
        {
            StartIndex = startIndex;
            EndIndexInclusive = endIndexInclusive;
            AllowedMoveIds = allowedMoveIds ?? Array.Empty<string>();
        }
    }

    public sealed class MonsterDef
    {
        public string Id { get; }
        public string Visual { get; }
        public int BodyHeight { get; }
        public string NameVi { get; }
        public string NameEn { get; }
        public MonsterClassKind Classification { get; }
        public MonsterSizeKind SizeClass { get; }
        public int BaseMaxHp { get; }
        public int BaseSpeed { get; }
        public string OpenerMoveId { get; }
        public IReadOnlyList<MonsterMoveDef> Moves { get; }
        public IReadOnlyList<BossPhaseDef> BossPhases { get; }

        public MonsterDef(
            string id,
            string visual,
            int bodyHeight,
            string nameVi,
            string nameEn,
            MonsterClassKind classification,
            MonsterSizeKind sizeClass,
            int baseMaxHp,
            int baseSpeed,
            string openerMoveId,
            IReadOnlyList<MonsterMoveDef> moves,
            IReadOnlyList<BossPhaseDef> bossPhases)
        {
            Id = id ?? string.Empty;
            Visual = visual ?? string.Empty;
            BodyHeight = bodyHeight;
            NameVi = nameVi ?? string.Empty;
            NameEn = nameEn ?? string.Empty;
            Classification = classification;
            SizeClass = sizeClass;
            BaseMaxHp = baseMaxHp;
            BaseSpeed = baseSpeed;
            OpenerMoveId = openerMoveId ?? string.Empty;
            Moves = moves ?? Array.Empty<MonsterMoveDef>();
            BossPhases = bossPhases ?? Array.Empty<BossPhaseDef>();
        }
    }
}
