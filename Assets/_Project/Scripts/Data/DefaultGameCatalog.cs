using System.Collections.Generic;
using Project05.Core;
using Project05.Backpack;

namespace Project05.Data
{
    /// <summary>
    /// Built-in catalog of Dark Fantasy Heroes, Multi-shape Items, Monsters, and 6-Tier Door Buffs.
    /// Ensures deterministic availability in both EditMode unit tests and PlayMode runtime.
    /// </summary>
    public static class DefaultGameCatalog
    {
        private static bool _initialized;
        private static readonly List<HeroDefinitionSO> Heroes = new List<HeroDefinitionSO>();
        private static readonly List<ItemDefinitionSO> Items = new List<ItemDefinitionSO>();
        private static readonly List<MonsterDefinitionSO> Monsters = new List<MonsterDefinitionSO>();
        private static readonly List<BuffDefinitionSO> Buffs = new List<BuffDefinitionSO>();

        public static IReadOnlyList<HeroDefinitionSO> AllHeroes { get { EnsureInitialized(); return Heroes; } }
        public static IReadOnlyList<ItemDefinitionSO> AllItems { get { EnsureInitialized(); return Items; } }
        public static IReadOnlyList<MonsterDefinitionSO> AllMonsters { get { EnsureInitialized(); return Monsters; } }
        public static IReadOnlyList<BuffDefinitionSO> AllBuffs { get { EnsureInitialized(); return Buffs; } }

        public static void EnsureInitialized()
        {
            if (_initialized && Heroes.Count > 0 && Items.Count > 0 && Buffs.Count > 0)
            {
                return;
            }

            Heroes.Clear();
            Items.Clear();
            Monsters.Clear();
            Buffs.Clear();

            BuildItems();
            BuildHeroes();
            BuildMonsters();
            BuildBuffs();

            _initialized = true;
        }

        private static void BuildItems()
        {
            // 1. Iron Broadsword (1x2 vertical, Weapon, White, CD 1)
            Items.Add(ItemDefinitionSO.CreateRuntime(
                "item_iron_sword",
                "Trường Kiếm Rỉ Máu",
                "Gây 12 Sát thương Vật lý mỗi lượt. Nhận cộng hưởng mạnh từ Đá Mài và Bình Độc kề bên.",
                ItemCategory.Weapon,
                CardTier.White,
                14,
                "item_sword",
                ItemShape.Create1x2Vertical(new GridCoord(-1, 0), new GridCoord(1, 0)),
                cooldownTurns: 1,
                physDmg: 12,
                synergyTargetCat: ItemCategory.Weapon,
                starDmg: 2));

            // 2. Shadow Dagger (1x1, Weapon, White, CD 1)
            Items.Add(ItemDefinitionSO.CreateRuntime(
                "item_curved_dagger",
                "Đoản Đao Bóng Đêm",
                "Gây 7 Sát thương Vật lý + 2 Chảy máu mỗi lượt. Tăng +3 Tốc độ cho người mang.",
                ItemCategory.Weapon,
                CardTier.White,
                12,
                "item_dagger",
                ItemShape.Create1x1(new GridCoord(0, -1), new GridCoord(0, 1)),
                cooldownTurns: 1,
                physDmg: 7,
                bleed: 2,
                passiveSpeed: 3,
                synergyTargetCat: ItemCategory.Weapon,
                starDmg: 2));

            // 3. Spiked Buckler (1x2 vertical, Armor, White, CD 1)
            Items.Add(ItemDefinitionSO.CreateRuntime(
                "item_buckler",
                "Khiên Gai Hắc Thiết",
                "Tạo +10 Giáp mỗi lượt. Các ô sao 2 bên tăng +4 Giáp cho Giáp khác.",
                ItemCategory.Armor,
                CardTier.White,
                14,
                "item_shield",
                ItemShape.Create1x2Vertical(new GridCoord(-1, 0), new GridCoord(1, 0), new GridCoord(-1, 1), new GridCoord(1, 1)),
                cooldownTurns: 1,
                shield: 10,
                synergyTargetCat: ItemCategory.Armor,
                starShield: 4));

            // 4. Blood Whetstone (1x1, Accessory, Green, CD 1)
            Items.Add(ItemDefinitionSO.CreateRuntime(
                "item_whetstone",
                "Đá Mài Huyết Tinh",
                "Tạo +3 Giáp. Cường hóa Vũ khí ở 4 ô kề bên: +6 Sát thương & +1 Chảy máu!",
                ItemCategory.Accessory,
                CardTier.Green,
                18,
                "item_whetstone",
                ItemShape.Create1x1(
                    new GridCoord(-1, 0),
                    new GridCoord(1, 0),
                    new GridCoord(0, -1),
                    new GridCoord(0, 1)),
                cooldownTurns: 1,
                shield: 3,
                synergyTargetCat: ItemCategory.Weapon,
                starDmg: 6,
                starBleed: 1));

            // 5. Plague Poison Flask (1x1, Potion, Green, CD 2)
            Items.Add(ItemDefinitionSO.CreateRuntime(
                "item_poison_flask",
                "Bình Độc Dịch Hạch",
                "Hồi chiêu 2 lượt: Gây 5 Độc (trừ máu bỏ qua giáp). Tẩm độc Vũ khí ở 4 ô kề bên (+3 Độc mỗi đòn).",
                ItemCategory.Potion,
                CardTier.Green,
                20,
                "item_poison",
                ItemShape.Create1x1(
                    new GridCoord(-1, 0),
                    new GridCoord(1, 0),
                    new GridCoord(0, -1),
                    new GridCoord(0, 1)),
                cooldownTurns: 2,
                poison: 5,
                synergyTargetCat: ItemCategory.Weapon,
                starPoison: 3));

            // 6. Crimson Healing Vial (1x1, Potion, Green, CD 2)
            Items.Add(ItemDefinitionSO.CreateRuntime(
                "item_crimson_vial",
                "Huyết Dược Cổ",
                "Hồi chiêu 2 lượt: Hồi 12 HP cho đồng đội thấp máu nhất. Tăng +4 Giáp cho Giáp kề trên/dưới.",
                ItemCategory.Potion,
                CardTier.Green,
                18,
                "item_potion",
                ItemShape.Create1x1(new GridCoord(0, -1), new GridCoord(0, 1)),
                cooldownTurns: 2,
                heal: 12,
                synergyTargetCat: ItemCategory.Armor,
                starShield: 4,
                healLowestAlly: true));

            // 7. Swift Greaves (1x2, Armor, Blue, CD 2)
            Items.Add(ItemDefinitionSO.CreateRuntime(
                "item_haste_boots",
                "Chiến Hài Tốc Hành",
                "Tăng +6 Tốc độ (giúp đầy thanh hành động nhanh hơn quái). Tạo +8 Giáp mỗi 2 lượt.",
                ItemCategory.Armor,
                CardTier.Blue,
                24,
                "item_boots",
                ItemShape.Create1x2Vertical(new GridCoord(0, -1), new GridCoord(-1, 0), new GridCoord(1, 0)),
                cooldownTurns: 2,
                shield: 8,
                passiveSpeed: 6,
                synergyTargetCat: ItemCategory.Armor,
                starShield: 5));

            // 8. Executioner L-Axe (L-shape, Weapon, Blue, CD 2)
            Items.Add(ItemDefinitionSO.CreateRuntime(
                "item_executioner_axe",
                "Rìu Đồ Tể (Chữ L)",
                "Hình chữ L (Xoay bằng phím R). Mỗi 2 lượt bổ 22 Sát thương Vật lý + 4 Chảy máu.",
                ItemCategory.Weapon,
                CardTier.Blue,
                26,
                "item_axe",
                ItemShape.CreateLShape(new GridCoord(1, 0), new GridCoord(-1, 1)),
                cooldownTurns: 2,
                physDmg: 22,
                bleed: 4,
                synergyTargetCat: ItemCategory.Weapon,
                starDmg: 4));

            // 9. Bastion 2x2 Tower Shield (2x2, Armor, Purple, CD 2)
            Items.Add(ItemDefinitionSO.CreateRuntime(
                "item_tower_shield",
                "Thành Lũy Hắc Thiết (2x2)",
                "Khiên 2x2 đồ sộ: Mỗi 2 lượt tạo +24 Giáp vững chắc. Các ô sao xung quanh +5 Giáp.",
                ItemCategory.Armor,
                CardTier.Purple,
                32,
                "item_shield",
                ItemShape.Create2x2Square(
                    new GridCoord(-1, 0), new GridCoord(2, 0),
                    new GridCoord(-1, 1), new GridCoord(2, 1)),
                cooldownTurns: 2,
                shield: 24,
                synergyTargetCat: ItemCategory.Armor,
                starShield: 5));

            // 10. Grimoire of Torment (2x2, Weapon, Purple, CD 3)
            Items.Add(ItemDefinitionSO.CreateRuntime(
                "item_thunder_tome",
                "Cổ Thư Sấm Ngục (2x2)",
                "Mỗi 3 lượt giáng 18 Sát thương Phép và gây 1 CHOÁNG (lùi thanh lượt của quái vật!).",
                ItemCategory.Weapon,
                CardTier.Purple,
                36,
                "item_tome",
                ItemShape.Create2x2Square(new GridCoord(0, -1), new GridCoord(1, -1)),
                cooldownTurns: 3,
                magicDmg: 18,
                stun: 1,
                synergyAny: true,
                starDmg: 4));

            // 11. Eldritch Idol (1x1, Relic, Gold, CD 2)
            Items.Add(ItemDefinitionSO.CreateRuntime(
                "item_cursed_idol",
                "Tượng Tà Thần Cổ Quái",
                "Bắn 10 Phép mỗi 2 lượt. Tinh vân 4 hướng giảm 1 lượt Cooldown và +6 Sát thương/Giáp cho MỌI trang bị kề bên!",
                ItemCategory.Relic,
                CardTier.Gold,
                45,
                "item_idol",
                ItemShape.Create1x1(
                    new GridCoord(-1, 0),
                    new GridCoord(1, 0),
                    new GridCoord(0, -1),
                    new GridCoord(0, 1)),
                cooldownTurns: 2,
                magicDmg: 10,
                passiveSpeed: 4,
                synergyAny: true,
                starDmg: 6,
                starShield: 6,
                starHeal: 4,
                starCdReduction: 1));

            // 12. Reaper T-Scythe (T-Shape, Weapon, Red, CD 2)
            Items.Add(ItemDefinitionSO.CreateRuntime(
                "item_plague_scythe",
                "Lưỡi Hái Huyết Nguyệt (Chữ T)",
                "Vũ khí Thần Thoại chữ T: Quét toàn bộ quái 24 Sát thương Vật lý + 4 Độc + 4 Chảy máu!",
                ItemCategory.Weapon,
                CardTier.Red,
                55,
                "item_axe",
                ItemShape.CreateTShape(new GridCoord(0, 1), new GridCoord(2, 1)),
                cooldownTurns: 2,
                physDmg: 24,
                poison: 4,
                bleed: 4,
                targetAll: true,
                synergyAny: true,
                starDmg: 5));
        }

        private static void BuildHeroes()
        {
            // Hero 0 (Starter): Duskblade Crusader (Frontline)
            Heroes.Add(HeroDefinitionSO.CreateRuntime(
                "hero_crusader",
                "Kỵ Sĩ Hắc Quang",
                "Tiền Tuyến • Chiến Binh / Đỡ Đòn",
                "Chiến binh khoác giáp cổ xưa, khởi đầu hành trình đơn độc giữa phế tích hắc ám.",
                PartyPosition.Frontline,
                "hero_crusader",
                maxHp: 110,
                baseSpeed: 22,
                startingShield: 6,
                bagWidth: 5,
                bagHeight: 4,
                unlockedCols: 4,
                unlockedRows: 3,
                "item_iron_sword", "item_whetstone", "item_buckler"));

            // Hero 1 (Recruitable): Plague Alchemist (Midline)
            Heroes.Add(HeroDefinitionSO.CreateRuntime(
                "hero_alchemist",
                "Dược Sư Dịch Hạch",
                "Trung Tâm • Độc Dược & Cổ Ngữ",
                "Bậc thầy độc tố và giả kim thuật, bào mòn sinh lực kẻ địch xuyên qua mọi lớp giáp.",
                PartyPosition.Midline,
                "hero_alchemist",
                maxHp: 86,
                baseSpeed: 26,
                startingShield: 0,
                bagWidth: 5,
                bagHeight: 4,
                unlockedCols: 4,
                unlockedRows: 3,
                "item_curved_dagger", "item_poison_flask", "item_crimson_vial"));

            // Hero 2 (Recruitable): Bloodmoon Arbalester (Backline)
            Heroes.Add(HeroDefinitionSO.CreateRuntime(
                "hero_ranger",
                "Thợ Săn Huyết Nguyệt",
                "Hậu Tuyến • Sát Thương Nhanh / Xuất Huyết",
                "Xạ thủ tốc độ cao với lưỡi rìu đồ tể và chiến hài lướt gió.",
                PartyPosition.Backline,
                "hero_ranger",
                maxHp: 82,
                baseSpeed: 29,
                startingShield: 0,
                bagWidth: 5,
                bagHeight: 4,
                unlockedCols: 4,
                unlockedRows: 3,
                "item_executioner_axe", "item_haste_boots", "item_whetstone"));

            // Hero 3 (Recruitable): Ashen Vestal (Backline/Midline Support)
            Heroes.Add(HeroDefinitionSO.CreateRuntime(
                "hero_cleric",
                "Thánh Nữ Tro Tàn",
                "Hậu Tuyến • Hộ Vệ / Sấm Phạt",
                "Người giữ cổ thư sấm sét, vừa hồi phục đồng đội vừa làm choáng quái vật.",
                PartyPosition.Backline,
                "hero_cleric",
                maxHp: 92,
                baseSpeed: 24,
                startingShield: 4,
                bagWidth: 5,
                bagHeight: 4,
                unlockedCols: 4,
                unlockedRows: 3,
                "item_thunder_tome", "item_crimson_vial"));
        }

        private static void BuildMonsters()
        {
            // Normal 1: Rotting Ghoul
            Monsters.Add(MonsterDefinitionSO.CreateRuntime(
                "monster_ghoul",
                "Xác Sống Đói Khát",
                "Quái Đột Biến",
                isElite: false,
                isBoss: false,
                spriteName: "monster_ghoul",
                maxHp: 64,
                baseSpeed: 18,
                goldReward: 16,
                new MonsterMoveDefinition("Cào Xé Thịt Tươi", IntentType.Attack, baseDamage: 11),
                new MonsterMoveDefinition("Nanh Vuốt Nhiễm Khuẩn", IntentType.HeavyBleedAttack, baseDamage: 9, statusType: StatusType.Bleed, statusStacks: 3),
                new MonsterMoveDefinition("Gầm Rú Khát Máu", IntentType.EnrageBuff, shieldGain: 6, speedBonus: 3)));

            // Normal 2: Blood Cultist
            Monsters.Add(MonsterDefinitionSO.CreateRuntime(
                "monster_cultist",
                "Tín Đồ Huyết Giáo",
                "Kẻ Thờ Tà Thần",
                isElite: false,
                isBoss: false,
                spriteName: "monster_cultist",
                maxHp: 56,
                baseSpeed: 21,
                goldReward: 18,
                new MonsterMoveDefinition("Phun Sương Độc", IntentType.PoisonSpit, baseDamage: 6, statusType: StatusType.Poison, statusStacks: 4, targetsBackline: true),
                new MonsterMoveDefinition("Hiến Tế Huyết Đao", IntentType.Attack, baseDamage: 12),
                new MonsterMoveDefinition("Khiên Hắc Ám", IntentType.DefendSelf, shieldGain: 12)));

            // Elite 1: Obsidian Gargoyle
            Monsters.Add(MonsterDefinitionSO.CreateRuntime(
                "monster_gargoyle",
                "Tượng Đá Hắc Diện",
                "Tinh Anh Cổ Đại (Elite)",
                isElite: true,
                isBoss: false,
                spriteName: "monster_gargoyle",
                maxHp: 115,
                baseSpeed: 20,
                goldReward: 34,
                new MonsterMoveDefinition("Cánh Đá Nghiền Nát", IntentType.Attack, baseDamage: 16),
                new MonsterMoveDefinition("Hóa Thạch Chấn Động", IntentType.StunCurse, baseDamage: 10, statusType: StatusType.Stun, statusStacks: 1),
                new MonsterMoveDefinition("Giáp Đá Vĩnh Cửu", IntentType.DefendSelf, shieldGain: 18),
                new MonsterMoveDefinition("Bão Đá Quét Ngang", IntentType.CleaveAll, baseDamage: 11)));

            // Boss: Crimson Lich Lord
            Monsters.Add(MonsterDefinitionSO.CreateRuntime(
                "monster_lich_boss",
                "Chúa Tể Huyết Nguyệt",
                "Đại Boss Phế Tích",
                isElite: false,
                isBoss: true,
                spriteName: "monster_boss",
                maxHp: 185,
                baseSpeed: 23,
                goldReward: 65,
                new MonsterMoveDefinition("Trượng Huyết Nguyệt", IntentType.HeavyBleedAttack, baseDamage: 18, statusType: StatusType.Bleed, statusStacks: 4),
                new MonsterMoveDefinition("Đại Dịch Tử Vong", IntentType.CleaveAll, baseDamage: 12, statusType: StatusType.Poison, statusStacks: 3),
                new MonsterMoveDefinition("Ác Mộng Giam Cầm", IntentType.StunCurse, baseDamage: 12, statusType: StatusType.Stun, statusStacks: 1, targetsBackline: true),
                new MonsterMoveDefinition("Hào Quang Bất Tử", IntentType.EnrageBuff, shieldGain: 20, speedBonus: 4)));
        }

        private static void BuildBuffs()
        {
            // WHITE (Trắng - Common)
            Buffs.Add(BuffDefinitionSO.CreateRuntime(
                "buff_white_whet",
                "Đá Mài Thô Sơ",
                "+2 Sát thương Vũ khí cho toàn đội & hồi ngay 15% HP.",
                CardTier.White, weaponDmg: 2, healPct: 15));
            Buffs.Add(BuffDefinitionSO.CreateRuntime(
                "buff_white_armor",
                "Tấm Chắn Sắt Vụn",
                "+5 Giáp khởi đầu mỗi trận đấu cho mọi nhân vật.",
                CardTier.White, startShield: 5));
            Buffs.Add(BuffDefinitionSO.CreateRuntime(
                "buff_white_pouch",
                "Túi Tiền Kẻ Hành Hương",
                "Nhận ngay +18 Vàng và +6 Máu tối đa.",
                CardTier.White, maxHp: 6, goldGain: 18));

            // GREEN (Lục - Uncommon)
            Buffs.Add(BuffDefinitionSO.CreateRuntime(
                "buff_green_speed",
                "Dầu Bôi Trơn Cơ Khí",
                "+4 Tốc độ cho toàn đội & hồi 20% HP.",
                CardTier.Green, speed: 4, healPct: 20));
            Buffs.Add(BuffDefinitionSO.CreateRuntime(
                "buff_green_venom",
                "Nọc Độc Rừng Thẳm",
                "Mọi trang bị gây Độc cộng thêm +2 Độc mỗi lần kích hoạt.",
                CardTier.Green, poison: 2, maxHp: 8));
            Buffs.Add(BuffDefinitionSO.CreateRuntime(
                "buff_green_slot",
                "Ngăn Túi Da Thuộc",
                "Mở khóa thêm +1 Ô Balo cho toàn bộ đồng đội & +4 Sát thương Vũ khí.",
                CardTier.Green, weaponDmg: 4, unlockSlots: 1));

            // BLUE (Lam - Rare)
            Buffs.Add(BuffDefinitionSO.CreateRuntime(
                "buff_blue_bastion",
                "Phù Hiệu Thành Lũy",
                "+12 Giáp khởi đầu trận đấu & +3 Sát thương Vũ khí/Phép.",
                CardTier.Blue, weaponDmg: 3, magicDmg: 3, startShield: 12));
            Buffs.Add(BuffDefinitionSO.CreateRuntime(
                "buff_blue_razor",
                "Lưỡi Dao Răng Cưa",
                "+5 Sát thương Vũ khí & đòn đánh gây thêm +2 Chảy máu.",
                CardTier.Blue, weaponDmg: 5, bleed: 2));
            Buffs.Add(BuffDefinitionSO.CreateRuntime(
                "buff_blue_expander",
                "Rương Chiến Thuật Lam Bảo",
                "Mở khóa ngay +2 Ô Balo cho mọi nhân vật & +5 Tốc độ.",
                CardTier.Blue, speed: 5, unlockSlots: 2));

            // PURPLE (Tím - Epic)
            Buffs.Add(BuffDefinitionSO.CreateRuntime(
                "buff_purple_chrono",
                "Đồng Hồ Cát Hư Không",
                "Giảm 1 lượt Hồi chiêu (Cooldown) cho mọi trang bị trong Balo & +6 Tốc độ!",
                CardTier.Purple, speed: 6, cdReduction: 1));
            Buffs.Add(BuffDefinitionSO.CreateRuntime(
                "buff_purple_plague",
                "Lời Nguyền Đại Dịch",
                "+7 Sát thương Vũ khí/Phép, +3 Độc mỗi lần kích hoạt & mở +2 Ô Balo.",
                CardTier.Purple, weaponDmg: 7, magicDmg: 7, poison: 3, unlockSlots: 2));

            // GOLD (Vàng - Legendary)
            Buffs.Add(BuffDefinitionSO.CreateRuntime(
                "buff_gold_aegis",
                "Thánh Tích Hoàng Kim",
                "Mở +3 Ô Balo, +20 Giáp đầu trận, +9 Sát thương & hồi 35% HP toàn đội!",
                CardTier.Gold, maxHp: 18, weaponDmg: 9, magicDmg: 9, startShield: 20, unlockSlots: 3, healPct: 35));
            Buffs.Add(BuffDefinitionSO.CreateRuntime(
                "buff_gold_overdrive",
                "Nhịp Tim Thần Tốc",
                "+10 Tốc độ, giảm 1 lượt Cooldown toàn bộ Balo & +4 Độc/Chảy máu!",
                CardTier.Gold, speed: 10, poison: 4, bleed: 4, cdReduction: 1));

            // RED (Đỏ - Mythic / Blood Moon)
            Buffs.Add(BuffDefinitionSO.CreateRuntime(
                "buff_red_bloodmoon",
                "Khế Ước Huyết Nguyệt",
                "Cấp Đỏ Tối Thượng: +14 Sát thương, +12 Tốc độ, giảm 1 lượt Cooldown, mở +4 Ô Balo & +25 Giáp đầu trận!",
                CardTier.Red, maxHp: 25, speed: 12, weaponDmg: 14, magicDmg: 14, startShield: 25, bleed: 4, cdReduction: 1, unlockSlots: 4, healPct: 50));
        }

        public static ItemDefinitionSO GetItemById(string itemId)
        {
            EnsureInitialized();
            for (int i = 0; i < Items.Count; i++)
            {
                if (Items[i].ItemId == itemId) return Items[i];
            }
            return Items[0];
        }

        public static HeroDefinitionSO GetHeroById(string heroId)
        {
            EnsureInitialized();
            for (int i = 0; i < Heroes.Count; i++)
            {
                if (Heroes[i].HeroId == heroId) return Heroes[i];
            }
            return Heroes[0];
        }

        public static MonsterDefinitionSO GetMonsterById(string monsterId)
        {
            EnsureInitialized();
            for (int i = 0; i < Monsters.Count; i++)
            {
                if (Monsters[i].MonsterId == monsterId) return Monsters[i];
            }
            return Monsters[0];
        }

        public static List<BuffDefinitionSO> GetBuffsByTier(CardTier tier)
        {
            EnsureInitialized();
            List<BuffDefinitionSO> list = new List<BuffDefinitionSO>();
            for (int i = 0; i < Buffs.Count; i++)
            {
                if (Buffs[i].Tier == tier)
                {
                    list.Add(Buffs[i]);
                }
            }
            if (list.Count == 0 && Buffs.Count > 0)
            {
                list.Add(Buffs[0]);
            }
            return list;
        }
    }
}
