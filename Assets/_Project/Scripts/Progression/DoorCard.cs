using System;
using Project05.Core;
using Project05.Data;

namespace Project05.Progression
{
    /// <summary>
    /// Represents one of the 3 Door Cards shown between rooms:
    /// - 6 Color Tiers: White (Trắng), Green (Lục), Blue (Lam), Purple (Tím), Gold (Vàng), Red (Đỏ).
    /// - Top Icon Badge: NextRoomType (Combat, Elite, Boss, Shop, Recruit, Event).
    /// - Immediate Buff Reward: BuffDefinitionSO matching the door's color tier.
    /// - Risk vs Reward: Higher tier = stronger buff + higher DifficultyMultiplier in the next room.
    /// </summary>
    [Serializable]
    public class DoorCard
    {
        public int DoorIndex;
        public CardTier Tier;
        public RoomType NextRoomType;
        public BuffDefinitionSO BuffReward;
        public float DifficultyMultiplier;
        public int BonusGoldReward;

        public string GetRoomBadgeText()
        {
            return Palette.GetRoomIconSymbol(NextRoomType) + " " + Palette.GetRoomVietnameseName(NextRoomType);
        }

        public string GetTierLabelText()
        {
            return Palette.GetTierVietnameseName(Tier);
        }

        public string GetDangerDescription()
        {
            if (NextRoomType == RoomType.Shop || NextRoomType == RoomType.Recruit || NextRoomType == RoomType.Event)
            {
                return $"An toàn • Thưởng thêm +{BonusGoldReward} Vàng";
            }

            int pct = UnityEngine.Mathf.RoundToInt(DifficultyMultiplier * 100f);
            return $"Độ khó Quái: {pct}% • Thưởng +{BonusGoldReward} Vàng";
        }
    }
}
