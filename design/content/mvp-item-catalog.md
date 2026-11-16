# MVP item catalog

> **Status**: Updated for 1-Hero Crusader Combat, 2026-11-16. Generated from `tools/art/item_catalog.py`; edit the script, not this file.
> **Every number is provisional.** Balance belongs to the balance harness (System 35).

Follows `item-trigger-and-cooldown.md`, `effect-resolution-model.md` (extended with `ShieldGain`, `Heal`, and `Self` for solo Crusader survival),
`adjacency-and-synergy.md`, `party-and-heroes.md` (passive `hpBonus` and `speedBonus`), and `grid-backpack.md` (footprints).

| # | Item | Tier | Shape | Trigger | Skill (effects) | Stats | Synergy (adjacent items) | Tags |
|---|------|------|-------|---------|-----------------|-------|--------------------------|------|
| 1 | Kiếm Sắt / Iron Sword (`item_iron_sword`) | White | 1x3 (3 cells) | Cooldown 2 | Damage 9 → FirstEnemy | - | - | weapon, blade |
| 2 | Dao Găm / Dagger (`item_dagger`) | White | 1x1 (1 cells) | Cooldown 1 | Damage 4 → FirstEnemy | - | - | weapon, blade |
| 3 | Rìu Tay / Hand Axe (`item_hand_axe`) | White | l (3 cells) | Cooldown 3 | Damage 12 → FirstEnemy | - | - | weapon, axe |
| 4 | Mực Ấn / Seal Ink (`item_seal_ink`) | White | 1x1 (1 cells) | Cooldown 2 | Apply 2 Ấn → FirstEnemy | - | - | seal |
| 5 | Đá Mài / Whetstone (`item_whetstone`) | White | 1x1 (1 cells) | Passive | - | - | blade: +3 damage | tool |
| 6 | Áo Da / Leather Vest (`item_leather_vest`) | White | 2x2 (4 cells) | Passive | - | +12 max HP | - | armour |
| 7 | Giày Gió / Swift Boots (`item_swift_boots`) | White | l3 (3 cells) | Passive | - | +3 speed | - | boots |
| 8 | Mũ Sắt / Iron Helm (`item_iron_helm`) | White | 1x1 (1 cells) | Passive | - | +6 max HP | - | armour |
| 9 | Khiên Gai / Spiked Buckler (`item_spiked_buckler`) | White | 1x2 (2 cells) | Cooldown 2 | Shield +6 → Self; Damage 3 → FirstEnemy | - | - | armour, shield |
| 10 | Khiên Gỗ Sồi / Oak Buckler (`item_oak_buckler`) | White | 1x1 (1 cells) | Cooldown 2 | Shield +5 → Self | - | - | armour, shield |
| 11 | Băng Cứu Thương / Field Bandage (`item_field_bandage`) | White | 1x1 (1 cells) | Cooldown 3 | Heal +6 → Self | - | - | tool, heal |
| 12 | Nỏ Gỗ / Crossbow (`item_crossbow`) | Green | 2x1 (2 cells) | Cooldown 3 | Damage 15 → FirstEnemy | - | - | weapon, ranged |
| 13 | Ấn Triện / Seal Stamp (`item_seal_stamp`) | Green | 1x2 (2 cells) | Cooldown 2 | Apply 3 Ấn → FirstEnemy | - | - | seal |
| 14 | Chuông Phá Ấn / Breaking Bell (`item_breaking_bell`) | Green | 1x2 (2 cells) | Cooldown 3 | Detonate up to 4 Ấn × 2 → FirstEnemy | - | - | detonator, bell |
| 15 | Trống Chiến / War Drum (`item_war_drum`) | Green | 1x2 (2 cells) | Passive | - | - | weapon: +25% damage | instrument |
| 16 | Lọ Mực / Ink Pot (`item_ink_pot`) | Green | 1x1 (1 cells) | Passive | - | - | seal: +1 an applied | tool |
| 17 | Bùa Tim / Heart Charm (`item_heart_charm`) | Green | 1x1 (1 cells) | Passive | - | +10 max HP | - | charm |
| 18 | Cờ Chiến / War Banner (`item_war_banner`) | Green | 1x3 (3 cells) | Combat start | Apply 2 Ấn → AllEnemies | - | - | banner |
| 19 | Kích Phá Trận / Sweeping Halberd (`item_cleaver_halberd`) | Green | 1x3 (3 cells) | Cooldown 3 | Damage 8 → AllEnemies | - | - | weapon, blade |
| 20 | Chùy Khắc Ấn / Runed Mace (`item_runed_mace`) | Green | 1x2 (2 cells) | Cooldown 2 | Damage 6 → FirstEnemy; Apply 2 Ấn → FirstEnemy | - | - | weapon, blunt, seal |
| 21 | Huyết Dược / Crimson Vial (`item_crimson_vial`) | Green | 1x1 (1 cells) | Cooldown 3 | Heal +10 → Self | - | - | potion, heal |
| 22 | Ấn Hộ Vệ / Guard Sigil (`item_guard_sigil`) | Green | 1x1 (1 cells) | Reaction (Owner takes damage), cooldown 2 | Shield +5 → Self | - | - | charm, shield |
| 23 | Búa Chiến / War Hammer (`item_war_hammer`) | Blue | t (5 cells) | Cooldown 4 | Damage 28 → FirstEnemy | - | - | weapon, blunt |
| 24 | Xích Ấn / Seal Chain (`item_seal_chain`) | Blue | s (4 cells) | Cooldown 3 | Apply 2 Ấn → AllEnemies | - | - | seal, chain |
| 25 | Chiêng Sấm / Thunder Gong (`item_thunder_gong`) | Blue | 2x2 (4 cells) | Cooldown 4 | Detonate up to 10 Ấn × 3 → FirstEnemy | - | - | detonator, bell |
| 26 | Đồng Hồ Cát / Hourglass (`item_hourglass`) | Blue | 1x2 (2 cells) | Passive | - | - | every neighbour: cooldown -1 | relic |
| 27 | Bùa Vang / Echo Charm (`item_echo_charm`) | Blue | 1x1 (1 cells) | Reaction (Ấn applied on an enemy), cooldown 2 | Damage 4 → FirstEnemy | - | - | charm |
| 28 | Đại Phủ Cuồng Phong / Whirlwind Axe (`item_whirlwind_axe`) | Blue | t (5 cells) | Cooldown 4 | Damage 14 → AllEnemies | - | - | weapon, axe |
| 29 | Thánh Kiếm Lời Thề / Oath Blade (`item_oath_blade`) | Blue | 1x3 (3 cells) | Cooldown 3 | Damage 14 → FirstEnemy; Shield +7 → Self | - | - | weapon, blade, shield |
| 30 | Thánh Tích Hồi Sinh / Sanctuary Relic (`item_sanctuary_relic`) | Blue | 1x2 (2 cells) | Cooldown 4 | Heal +9 → Self; Shield +9 → Self | - | - | relic, heal, shield |
| 31 | Lư Hương Hư Vô / Void Censer (`item_void_censer`) | Purple | 2x3 (6 cells) | Cooldown 5 | Detonate up to 99 Ấn × 4 → FirstEnemy | - | - | detonator, incense |
| 32 | Kính Học Giả / Scholar Lens (`item_scholar_lens`) | Purple | 1x1 (1 cells) | Passive | - | - | seal: +2 an applied; detonator: +50% damage | relic |
| 33 | Khiên Tháp / Tower Shield (`item_tower_shield`) | Purple | 2x3 (6 cells) | Passive | - | +30 max HP, -2 speed | - | armour |
| 34 | Chuông Cộng Hưởng / Resonant Bell (`item_resonant_bell`) | Purple | 2x2 (4 cells) | Cooldown 4 | Detonate up to 8 Ấn × 3 → AllEnemies | - | - | detonator, bell |

34 items. Per tier: {'White': 11, 'Green': 11, 'Blue': 8, 'Purple': 4}. Footprints used: 1x1, 1x2, 1x3, 2x1, 2x2, 2x3, l, l3, s, t.

## Design intent

- **Three jobs, one chain + solo Crusader sustain.** Seals (ApplyStatus Ấn) set up, detonators (ConsumeDetonate) cash in, passive relics
  tune either end, and active Shield/Heal/Cleave gear lets a solo Tank survive and clear 1..6 monsters.
- **Rhythm is the cost.** Strong pieces wait longer (cooldown 4 to 5) and take more cells (T, S, 2×3), so
  the grid pays for power in space and in time.
- **Passive stats are small and honest.** `hpBonus` stays inside the `-20..40` range, and the Tower Shield trades 2 speed for 30 HP.
- **Synergy is local.** Every rule sits on a neighbour link (System 10); none crosses hero grids.
- **Shapes match the object.** A sword/halberd is 1×3, a hammer/whirlwind axe a T, a chain an S, a bell 1×2, a gong/resonant bell 2×2. Art fills the footprint.
