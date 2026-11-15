# MVP item catalog

> **Status**: Draft, 2026-11-15. Generated from `tools/art/item_catalog.py`; edit the script, not this file.
> **Every number is provisional.** Balance belongs to the balance harness (System 35).

Follows `item-trigger-and-cooldown.md`, `effect-resolution-model.md` (MVP effect kinds), `adjacency-and-synergy.md`,
`party-and-heroes.md` (passive `hpBonus` and `speedBonus`) and `grid-backpack.md` (footprints). Gold and Red wait for
their curses. The target names `FirstEnemy` and `AllEnemies` are a **proposal**: the owners of target selection
(Systems 3 and 9) have not defined a vocabulary yet.

| # | Item | Tier | Shape | Trigger | Skill (effects) | Stats | Synergy (adjacent items) | Tags |
|---|------|------|-------|---------|-----------------|-------|--------------------------|------|
| 1 | Kiếm Sắt / Iron Sword (`item_iron_sword`) | White | 1x3 (3 cells) | Cooldown 2 | Damage 9 → FirstEnemy | - | - | weapon, blade |
| 2 | Dao Găm / Dagger (`item_dagger`) | White | 1x1 (1 cells) | Cooldown 1 | Damage 4 → FirstEnemy | - | - | weapon, blade |
| 3 | Rìu Tay / Hand Axe (`item_hand_axe`) | White | l (3 cells) | Cooldown 3 | Damage 12 → FirstEnemy | - | - | weapon, axe |
| 4 | Mực Ấn / Seal Ink (`item_seal_ink`) | White | 1x1 (1 cells) | Cooldown 2 | Apply 2 Ấn → FirstEnemy | - | - | seal |
| 5 | Đá Mài / Whetstone (`item_whetstone`) | White | 1x1 (1 cells) | Passive | - | - | blade: +3 damage | tool |
| 6 | Áo Da / Leather Vest (`item_leather_vest`) | White | 2x2 (4 cells) | Passive | - | +12 max HP | - | armour |
| 7 | Giày Gió / Swift Boots (`item_swift_boots`) | White | l (3 cells) | Passive | - | +3 speed | - | boots |
| 8 | Mũ Sắt / Iron Helm (`item_iron_helm`) | White | 1x1 (1 cells) | Passive | - | +6 max HP | - | armour |
| 9 | Nỏ Gỗ / Crossbow (`item_crossbow`) | Green | 2x1 (2 cells) | Cooldown 3 | Damage 15 → FirstEnemy | - | - | weapon, ranged |
| 10 | Ấn Triện / Seal Stamp (`item_seal_stamp`) | Green | 1x2 (2 cells) | Cooldown 2 | Apply 3 Ấn → FirstEnemy | - | - | seal |
| 11 | Chuông Phá Ấn / Breaking Bell (`item_breaking_bell`) | Green | 1x2 (2 cells) | Cooldown 3 | Detonate up to 4 Ấn × 2 → FirstEnemy | - | - | detonator, bell |
| 12 | Trống Chiến / War Drum (`item_war_drum`) | Green | 1x2 (2 cells) | Passive | - | - | weapon: +25% damage | instrument |
| 13 | Lọ Mực / Ink Pot (`item_ink_pot`) | Green | 1x1 (1 cells) | Passive | - | - | seal: +1 an applied | tool |
| 14 | Bùa Tim / Heart Charm (`item_heart_charm`) | Green | 1x1 (1 cells) | Passive | - | +10 max HP | - | charm |
| 15 | Cờ Chiến / War Banner (`item_war_banner`) | Green | 1x3 (3 cells) | Combat start | Apply 2 Ấn → AllEnemies | - | - | banner |
| 16 | Búa Chiến / War Hammer (`item_war_hammer`) | Blue | t (5 cells) | Cooldown 4 | Damage 28 → FirstEnemy | - | - | weapon, blunt |
| 17 | Xích Ấn / Seal Chain (`item_seal_chain`) | Blue | s (4 cells) | Cooldown 3 | Apply 2 Ấn → AllEnemies | - | - | seal, chain |
| 18 | Chiêng Sấm / Thunder Gong (`item_thunder_gong`) | Blue | 2x2 (4 cells) | Cooldown 4 | Detonate up to 10 Ấn × 3 → FirstEnemy | - | - | detonator, bell |
| 19 | Đồng Hồ Cát / Hourglass (`item_hourglass`) | Blue | 1x2 (2 cells) | Passive | - | - | every neighbour: cooldown -1 | relic |
| 20 | Bùa Vang / Echo Charm (`item_echo_charm`) | Blue | 1x1 (1 cells) | Reaction (Ấn applied on an enemy), cooldown 2 | Damage 4 → FirstEnemy | - | - | charm |
| 21 | Lư Hương Hư Vô / Void Censer (`item_void_censer`) | Purple | 2x3 (6 cells) | Cooldown 5 | Detonate up to 99 Ấn × 4 → FirstEnemy | - | - | detonator, incense |
| 22 | Kính Học Giả / Scholar Lens (`item_scholar_lens`) | Purple | 1x1 (1 cells) | Passive | - | - | seal: +2 an applied; detonator: +50% damage | relic |
| 23 | Khiên Tháp / Tower Shield (`item_tower_shield`) | Purple | 2x3 (6 cells) | Passive | - | +30 max HP, -2 speed | - | armour |

23 items. Per tier: {'White': 8, 'Green': 7, 'Blue': 5, 'Purple': 3}. Footprints used: 1x1, 1x2, 1x3, 2x1, 2x2, 2x3, l, s, t.

## Design intent

- **Three jobs, one chain.** Seals (ApplyStatus Ấn) set up, detonators (ConsumeDetonate) cash in, and passive relics
  tune either end. The same loop the Ấn spike proves, spread over enough pieces that arrangement is a decision.
- **Rhythm is the cost.** Strong pieces wait longer (cooldown 4 to 5) and take more cells (T, S, 2×3), so
  the grid pays for power in space and in time.
- **Passive stats are small and honest.** `hpBonus` stays inside the `-20..40` range, and the Tower Shield trades 2 speed for 30 HP.
- **Synergy is local.** Every rule sits on a neighbour link (System 10); none crosses hero grids.
- **Shapes match the object.** A sword is 1×3, a hammer a T, a chain an S, a bell 1×2, a gong 2×2. Art fills the footprint.
