# MVP item catalog

> **Status**: Debuffs, detonation and + upgrades, 2026-11-17 (design/gdd/debuffs-and-detonation.md, item-upgrades.md). Generated from `tools/art/item_catalog.py`; edit the script, not this file.
> **Every number is provisional.** Balance belongs to the balance harness (System 35).

Follows `item-trigger-and-cooldown.md`, `effect-resolution-model.md` (extended with `ShieldGain`, `Heal`, and `Self` for solo Crusader survival),
`adjacency-and-synergy.md`, `party-and-heroes.md` (passive `hpBonus` and `speedBonus`), and `grid-backpack.md` (footprints).

| # | Item | Tier | Shape | Trigger | Skill (effects) | Stats | Synergy (adjacent items) | Tags |
|---|------|------|-------|---------|-----------------|-------|--------------------------|------|
| 1 | Kiếm Sắt / Iron Sword (`item_iron_sword`) | White | 1x3 (3 cells) | Cooldown 2 | Damage 9 → FirstEnemy | - | - | weapon, blade |
| 2 | Dao Găm / Dagger (`item_dagger`) | White | 1x1 (1 cells) | Cooldown 1 | Damage 4 → FirstEnemy | - | - | weapon, blade |
| 3 | Rìu Tay / Hand Axe (`item_hand_axe`) | White | l (3 cells) | Cooldown 3 | Damage 12 → FirstEnemy | - | - | weapon, axe |
| 4 | Lọ Độc / Venom Vial (`item_venom_vial`) | White | 1x1 (1 cells) | Cooldown 2 | Apply 3 poison → FirstEnemy | - | - | affliction, potion |
| 5 | Phi Tiêu Gai / Thorn Dart (`item_thorn_dart`) | White | 1x1 (1 cells) | Cooldown 1 | Damage 3 → FirstEnemy; Apply 1 poison → FirstEnemy | - | - | weapon, ranged, affliction |
| 6 | Bùa Than Hồng / Ember Charm (`item_ember_charm`) | White | 1x1 (1 cells) | Cooldown 2 | Apply 2 burn → FirstEnemy | - | - | charm, affliction |
| 7 | Đá Mài / Whetstone (`item_whetstone`) | White | 1x1 (1 cells) | Passive | - | - | blade: +3 damage | tool |
| 8 | Áo Da / Leather Vest (`item_leather_vest`) | White | 2x2 (4 cells) | Passive | - | +12 max HP | - | armour |
| 9 | Giày Gió / Swift Boots (`item_swift_boots`) | White | l3 (3 cells) | Passive | - | +3 speed | - | boots |
| 10 | Mũ Sắt / Iron Helm (`item_iron_helm`) | White | 1x1 (1 cells) | Passive | - | +6 max HP | - | armour |
| 11 | Khiên Gai / Spiked Buckler (`item_spiked_buckler`) | White | 1x2 (2 cells) | Cooldown 2 | Shield +6 → Self; Damage 3 → FirstEnemy | - | - | armour, shield |
| 12 | Khiên Gỗ Sồi / Oak Buckler (`item_oak_buckler`) | White | 1x1 (1 cells) | Cooldown 2 | Shield +5 → Self | - | - | armour, shield |
| 13 | Băng Cứu Thương / Field Bandage (`item_field_bandage`) | White | 1x1 (1 cells) | Cooldown 3 | Heal +6 → Self | - | - | tool, heal |
| 14 | Nỏ Gỗ / Crossbow (`item_crossbow`) | Green | 2x1 (2 cells) | Cooldown 3 | Damage 15 → FirstEnemy | - | - | weapon, ranged |
| 15 | Bình Dầu Lửa / Oil Flask (`item_oil_flask`) | Green | 1x2 (2 cells) | Cooldown 2 | Apply 3 burn → FirstEnemy | - | - | affliction, potion |
| 16 | Chuông Mê / Drowsy Bell (`item_drowsy_bell`) | Green | 1x2 (2 cells) | Cooldown 2 | Apply 2 slow → FirstEnemy | - | - | affliction, bell |
| 17 | Mảnh Băng / Frost Shard (`item_frost_shard`) | Green | 1x2 (2 cells) | Cooldown 3 | Damage 6 → FirstEnemy; Apply 2 frost → FirstEnemy | - | - | weapon, affliction |
| 18 | Lưới Nhựa / Tar Net (`item_tar_net`) | Green | 2x1 (2 cells) | Combat start | Apply 2 slow → AllEnemies | - | - | tool, affliction |
| 19 | Mặt Nạ Nứt / Cracked Mask (`item_cracked_mask`) | Green | 1x1 (1 cells) | Cooldown 2 | Apply 1 weaken → FirstEnemy | - | - | charm, affliction |
| 20 | Túi Tiền / Coin Purse (`item_coin_purse`) | Green | 1x1 (1 cells) | Passive | - | - | - | tool |
| 21 | Trống Chiến / War Drum (`item_war_drum`) | Green | 1x2 (2 cells) | Passive | - | - | weapon: +25% damage | instrument |
| 22 | Cối Tán Thuốc / Mortar (`item_mortar`) | Green | 1x1 (1 cells) | Passive | - | - | affliction: +1 debuff applied | tool |
| 23 | Bùa Tim / Heart Charm (`item_heart_charm`) | Green | 1x1 (1 cells) | Passive | - | +10 max HP | - | charm |
| 24 | Cờ Chiến / War Banner (`item_war_banner`) | Green | 1x3 (3 cells) | Combat start | Apply 2 weaken → AllEnemies | - | - | banner, affliction |
| 25 | Kích Phá Trận / Sweeping Halberd (`item_cleaver_halberd`) | Green | 1x3 (3 cells) | Cooldown 3 | Damage 8 → AllEnemies | - | - | weapon, blade |
| 26 | Chùy Văn Băng / Runed Mace (`item_runed_mace`) | Green | 1x2 (2 cells) | Cooldown 2 | Damage 6 → FirstEnemy; Apply 2 frost → FirstEnemy | - | - | weapon, blunt, affliction |
| 27 | Huyết Dược / Crimson Vial (`item_crimson_vial`) | Green | 1x1 (1 cells) | Cooldown 3 | Heal +10 → Self | - | - | potion, heal |
| 28 | Ấn Hộ Vệ / Guard Sigil (`item_guard_sigil`) | Green | 1x1 (1 cells) | Reaction (Owner takes damage), cooldown 2 | Shield +5 → Self | - | - | charm, shield |
| 29 | Búa Chiến / War Hammer (`item_war_hammer`) | Blue | t (5 cells) | Cooldown 4 | Damage 28 → FirstEnemy | - | - | weapon, blunt |
| 30 | Xích Nhựa Đen / Tar Chain (`item_tar_chain`) | Blue | s (4 cells) | Cooldown 3 | Apply 2 slow → AllEnemies | - | - | affliction, chain |
| 31 | Chiêng Sấm / Thunder Gong (`item_thunder_gong`) | Blue | 2x2 (4 cells) | Reaction (a debuff is detonated), cooldown 1 | Damage 5 → AllEnemies | - | - | bell |
| 32 | Giáo Dịch / Blight Spear (`item_blight_spear`) | Blue | 1x3 (3 cells) | Cooldown 3 | Damage 10 → FirstEnemy; Apply 3 poison → FirstEnemy | - | - | weapon, affliction |
| 33 | Trống Ru / Lull Drum (`item_lull_drum`) | Blue | 1x2 (2 cells) | Cooldown 3 | Apply 1 slow → AllEnemies; Apply 1 weaken → AllEnemies | - | - | instrument, affliction |
| 34 | Đồng Hồ Cát / Hourglass (`item_hourglass`) | Blue | 1x2 (2 cells) | Passive | - | - | every neighbour: cooldown -1 | relic |
| 35 | Bùa Vang / Echo Charm (`item_echo_charm`) | Blue | 1x1 (1 cells) | Reaction (a debuff is applied to an enemy), cooldown 2 | Damage 4 → FirstEnemy | - | - | charm |
| 36 | Đại Phủ Cuồng Phong / Whirlwind Axe (`item_whirlwind_axe`) | Blue | t (5 cells) | Cooldown 4 | Damage 14 → AllEnemies | - | - | weapon, axe |
| 37 | Thánh Kiếm Lời Thề / Oath Blade (`item_oath_blade`) | Blue | 1x3 (3 cells) | Cooldown 3 | Damage 14 → FirstEnemy; Shield +7 → Self | - | - | weapon, blade, shield |
| 38 | Thánh Tích Hồi Sinh / Sanctuary Relic (`item_sanctuary_relic`) | Blue | 1x2 (2 cells) | Cooldown 4 | Heal +9 → Self; Shield +9 → Self | - | - | relic, heal, shield |
| 39 | Lư Hương Dịch Hạch / Plague Censer (`item_plague_censer`) | Purple | 2x3 (6 cells) | Passive | - | - | detonator: +2 detonation value per poison stack | incense |
| 40 | Kính Học Giả / Scholar Lens (`item_scholar_lens`) | Purple | 1x1 (1 cells) | Passive | - | - | affliction: +1 debuff applied; detonator: +50% damage | relic |
| 41 | Vạc Phù Thủy / Witch Cauldron (`item_witch_cauldron`) | Purple | 2x2 (4 cells) | Passive | - | - | affliction: +1 debuff applied | relic |
| 42 | Chùy Băng Hà / Glacier Maul (`item_glacier_maul`) | Purple | t (5 cells) | Cooldown 4 | Damage 18 → AllEnemies; Apply 2 frost → AllEnemies | - | - | weapon, blunt, affliction |
| 43 | Xúc Tác Dịch Hạch / Plague Catalyst (`item_plague_catalyst`) | Purple | 1x2 (2 cells) | Cooldown 4 | Detonate every debuff → all enemies | - | - | detonator, alchemy |
| 44 | Khiên Tháp / Tower Shield (`item_tower_shield`) | Purple | 2x3 (6 cells) | Passive | - | +30 max HP, -2 speed | - | armour |
| 45 | Chuông Cộng Hưởng / Resonant Bell (`item_resonant_bell`) | Purple | 2x2 (4 cells) | Passive | - | - | detonator: +1 detonation value per debuff stack | bell |
| 46 | Neo Nguyền / Cursed Anchor (`item_cursed_anchor`) | Gold | 2x1 (2 cells) | Passive | - | - | - |  |

46 items. Per tier: {'White': 13, 'Green': 15, 'Blue': 10, 'Purple': 7}. Footprints used: 1x1, 1x2, 1x3, 2x1, 2x2, 2x3, l, l3, s, t.

## Design intent

- **Three jobs, one chain + solo sustain.** Debuff items (Poison, Burn, Frost, Slow, Weaken) set up, the Alchemist's Plague Catalyst detonates them all, passive relics
  tune either end, and active Shield/Heal/Cleave gear lets a solo Tank survive and clear 1..6 monsters.
- **Rhythm is the cost.** Strong pieces wait longer (cooldown 4 to 5) and take more cells (T, S, 2×3), so
  the grid pays for power in space and in time.
- **Passive stats are small and honest.** `hpBonus` stays inside the `-20..40` range, and the Tower Shield trades 2 speed for 30 HP.
- **Synergy is local.** Every rule sits on a neighbour link (System 10); none crosses hero grids.
- **Shapes match the object.** A sword/halberd is 1×3, a hammer/whirlwind axe a T, a chain an S, a bell 1×2, a gong/resonant bell 2×2. Art fills the footprint.
