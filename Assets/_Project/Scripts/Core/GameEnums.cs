namespace Project05.Core
{
    /// <summary>
    /// 6-tier rarity and door buff classification:
    /// Trắng (Common) -> Lục (Uncommon) -> Lam (Rare) -> Tím (Epic) -> Vàng (Legendary) -> Đỏ (Mythic/Hell)
    /// </summary>
    public enum CardTier
    {
        White = 0,
        Green = 1,
        Blue = 2,
        Purple = 3,
        Gold = 4,
        Red = 5
    }

    public enum ItemCategory
    {
        Weapon = 0,
        Armor = 1,
        Accessory = 2,
        Potion = 3,
        Relic = 4
    }

    public enum StatusType
    {
        Poison = 0,
        Bleed = 1,
        Stun = 2
    }

    public enum RoomType
    {
        Combat = 0,
        Elite = 1,
        Boss = 2,
        Shop = 3,
        Recruit = 4,
        Event = 5
    }

    public enum IntentType
    {
        Attack = 0,
        HeavyBleedAttack = 1,
        PoisonSpit = 2,
        StunCurse = 3,
        DefendSelf = 4,
        EnrageBuff = 5,
        CleaveAll = 6
    }

    public enum RunState
    {
        Combat = 0,
        DoorSelection = 1,
        Shop = 2,
        Recruit = 3,
        Event = 4,
        Victory = 5,
        Defeat = 6
    }

    public enum PartyPosition
    {
        Frontline = 0,
        Midline = 1,
        Backline = 2
    }
}
