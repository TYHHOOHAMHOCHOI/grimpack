using UnityEngine;

namespace Project05.Core
{
    /// <summary>
    /// Dark Fantasy Art Bible & Single-Source Color Palette:
    /// - Style: "Gothic 16-bit Dark Fantasy (Darkest Dungeon mood): obsidian iron frames, parchment bone text, blood crimson accents, and luminous 6-tier door sigils."
    /// - Mood: "Flickering torchlight against oppressive abyssal slate shadows."
    /// </summary>
    public static class Palette
    {
        // Environment & Column Backgrounds
        public static readonly Color AbyssBg = new Color(0.06f, 0.05f, 0.08f, 1f);          // #0F0D14
        public static readonly Color ColumnLeftBg = new Color(0.09f, 0.08f, 0.12f, 0.98f);  // #17141F
        public static readonly Color ColumnMidBg = new Color(0.11f, 0.09f, 0.14f, 0.96f);   // #1C1724
        public static readonly Color ColumnRightBg = new Color(0.10f, 0.07f, 0.11f, 0.98f); // #1A121C

        // Panels & Frames
        public static readonly Color PanelSurface = new Color(0.14f, 0.12f, 0.18f, 0.96f);
        public static readonly Color PanelHeader = new Color(0.20f, 0.15f, 0.24f, 1f);
        public static readonly Color BorderIron = new Color(0.34f, 0.30f, 0.40f, 1f);
        public static readonly Color BorderGold = new Color(0.84f, 0.66f, 0.26f, 1f);

        // Grid Slots
        public static readonly Color SlotUnlocked = new Color(0.17f, 0.15f, 0.22f, 1f);
        public static readonly Color SlotLocked = new Color(0.08f, 0.07f, 0.10f, 0.85f);
        public static readonly Color SlotValidHover = new Color(0.22f, 0.68f, 0.36f, 0.85f);
        public static readonly Color SlotInvalidHover = new Color(0.78f, 0.20f, 0.24f, 0.85f);
        public static readonly Color SlotSynergyGlow = new Color(0.95f, 0.78f, 0.24f, 0.90f);

        // Text & Accents
        public static readonly Color BoneText = new Color(0.92f, 0.89f, 0.82f, 1f);
        public static readonly Color AshMutedText = new Color(0.64f, 0.60f, 0.68f, 1f);
        public static readonly Color TorchGold = new Color(0.94f, 0.75f, 0.28f, 1f);
        public static readonly Color CrimsonAccent = new Color(0.78f, 0.18f, 0.24f, 1f);

        // Combat Bars & Statuses
        public static readonly Color HpBarFill = new Color(0.76f, 0.18f, 0.22f, 1f);
        public static readonly Color ShieldBarFill = new Color(0.28f, 0.58f, 0.86f, 1f);
        public static readonly Color SpeedBarFill = new Color(0.92f, 0.76f, 0.22f, 1f);
        public static readonly Color PoisonColor = new Color(0.32f, 0.84f, 0.38f, 1f);
        public static readonly Color BleedColor = new Color(0.86f, 0.16f, 0.24f, 1f);
        public static readonly Color StunColor = new Color(0.95f, 0.82f, 0.25f, 1f);

        // 6-Tier Door & Item Rarity Colors (Trắng, Lục, Lam, Tím, Vàng, Đỏ)
        public static readonly Color TierWhite = new Color(0.85f, 0.84f, 0.80f, 1f);
        public static readonly Color TierGreen = new Color(0.30f, 0.74f, 0.38f, 1f);
        public static readonly Color TierBlue = new Color(0.26f, 0.56f, 0.90f, 1f);
        public static readonly Color TierPurple = new Color(0.68f, 0.34f, 0.88f, 1f);
        public static readonly Color TierGold = new Color(0.95f, 0.74f, 0.22f, 1f);
        public static readonly Color TierRed = new Color(0.88f, 0.18f, 0.24f, 1f);

        public static Color GetTierColor(CardTier tier)
        {
            switch (tier)
            {
                case CardTier.White: return TierWhite;
                case CardTier.Green: return TierGreen;
                case CardTier.Blue: return TierBlue;
                case CardTier.Purple: return TierPurple;
                case CardTier.Gold: return TierGold;
                case CardTier.Red: return TierRed;
                default: return TierWhite;
            }
        }

        public static string GetTierVietnameseName(CardTier tier)
        {
            switch (tier)
            {
                case CardTier.White: return "Trắng (Phàm Phẩm)";
                case CardTier.Green: return "Lục (Tinh Nhuệ)";
                case CardTier.Blue: return "Lam (Hiếm Có)";
                case CardTier.Purple: return "Tím (Sử Thi)";
                case CardTier.Gold: return "Vàng (Huyền Thoại)";
                case CardTier.Red: return "Đỏ (Huyết Nguyệt)";
                default: return "Trắng";
            }
        }

        public static string GetRoomVietnameseName(RoomType roomType)
        {
            switch (roomType)
            {
                case RoomType.Combat: return "Ải Quái Thường";
                case RoomType.Elite: return "Ải Tinh Anh (Elite)";
                case RoomType.Boss: return "Phòng Đại Boss";
                case RoomType.Shop: return "Thương Nhân Hắc Ám";
                case RoomType.Recruit: return "Trại Chiêu Mộ Đồng Đội";
                case RoomType.Event: return "Sự Kiện Cổ Quái";
                default: return "Ải Bí Ẩn";
            }
        }

        public static string GetRoomIconSymbol(RoomType roomType)
        {
            switch (roomType)
            {
                case RoomType.Combat: return "[⚔ QUÁI]";
                case RoomType.Elite: return "[☠ TINH ANH]";
                case RoomType.Boss: return "[👑 ĐẠI BOSS]";
                case RoomType.Shop: return "[💰 CỬA HÀNG]";
                case RoomType.Recruit: return "[🛡 CHIÊU MỘ]";
                case RoomType.Event: return "[🕯 SỰ KIỆN]";
                default: return "[?]";
            }
        }
    }
}
