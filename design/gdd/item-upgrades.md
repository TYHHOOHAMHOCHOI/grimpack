# Item Upgrades (Forge)

> **Status**: Designed (user decisions 2026-11-17; independent /design-review not yet run)
> **Author**: user + Claude design session (systems-designer)
> **Last Updated**: 2026-11-17
> **Last Verified**: 2026-11-17
> **Implements Pillar**: Arrangement Is the Thinking (which item to invest in); Earned Rarity (one upgrade per item, bounded gold)

## Summary

System 39 lets the player spend run gold to upgrade an item into its single upgraded version, **Item +**, at a Forge ("Rèn") tab that shares the sell drawer. An upgrade either raises the item's numbers or adds one ability; shape, tags and rotation never change, and the item is replaced in place, so the arrangement stays valid. Gold comes from winning rooms (more behind higher doors), from Coin Purse items and from sales. The target is about five upgrades per run.

> **Quick reference** — Layer: `Feature` · Priority: `MVP` · Key deps: `Game Data Definitions (2), Run State and Flow (5), Grid Backpack (8), Adjacency and Synergy (10), Commitment and Drop Model (18), Backpack UI (25), Hover Popup (26)`

## Overview

The user asked on 2026-11-17 for an upgrade step: every item has one upgraded version, and the player drags an item onto a Forge panel, beside selling, and pays gold to raise its stats or give it a new ability. This system owns the upgrade definitions' rules, the run's gold income in the MVP, the upgrade cost, the moment upgrading is allowed, the upgrade operation and the Forge tab of the drawer. System 20 (Currency and Shop, Alpha) builds on this gold later; it does not replace it.

## Player Fantasy

After a room the player counts their coins and looks at the grid: the dart that fires every turn, the Catalyst at the centre of the build, the shield that keeps them alive. They drag one onto the anvil, see "9 → 12" before letting go, drop it, and the plate flashes and gets a small "+". The bet on a harder door now also means more coins for the forge. **I don't just find my build; I temper the pieces I've committed to.**

## Detailed Design

### Core Rules

1. **One upgrade per item.** Every item definition has exactly one upgraded definition with ID `<base>_plus` and name key `<base name> +` (displayed with " +"). A `+` item is final. Exceptions: `item_cursed_anchor` never upgrades (System 19); `item_plague_catalyst` does (Cooldown 4 → 3). Innate items are not items in a grid and never upgrade.
2. **What an upgrade changes.** Either (a) **stats**: one or more of the item's beneficial numbers, each changed by Formula 1, or a cooldown of 4 or more lowered by 1 (Formula 2); or (b) **one ability**: one added effect on the existing trigger, or one added synergy rule (at most `MaxRulesPerItem` = 3 in total), or one passive stat. Drawback numbers (for example the Tower Shield's −2 speed) never change.
3. **What never changes.** Footprint, tags, rotation, tier, trigger kind and the item's instance ID. An upgrade never adds `DetonateAll` ([Debuffs and Detonation](debuffs-and-detonation.md), Rule 12).
4. **Authored data, linted.** The `+` definitions are authored in the item data (System 2). A lint checks every changed number against Formulas 1 and 2 and every rule above, and that every upgradeable item has exactly one `+`.
5. **Never offered.** `+` items never appear in reward offers in the MVP (System 18 draws base items only). A `+` item counts as its base item for commitment (same tags and tier).
6. **Gold.** The run holds one non-negative integer, `gold`, in System 5's snapshot. Start: `5`. When a room is won the run pays `GoldByDoor` by the tier of the door that led into it: White 2, Green 3, Blue 5, Purple 7, Gold 10. Room 1 counts as White; the boss pays 0. Each Coin Purse in a hero grid when the room is won adds 2 (a purse in the stash pays nothing; see Open Questions). Selling adds the sell price (Rule 10). Payment happens at the Victory transition, before BuffClaim.
7. **Cost.** `UpgradeCost` by the base item's tier: White 4, Green 6, Blue 9, Purple 14. Gold and Red items have none.
8. **When.** Planning only, the same gate as selling: in Arrangement before a fight and during the reward steps (BuffClaim, offer, door choice). Never while Fighting or Frozen. Only items in a hero grid or the stash; an item on the reward shelf, in the tray or loose is refused.
9. **Operation.** `Upgrade(itemInstanceId)` checks, in this order: phase, container, an upgrade exists, not already `+`, `gold ≥ cost`. On success, in one step: `gold −= cost`; the instance keeps its ID, container, origin cells and orientation, and its definition becomes `<base>_plus`. The adjacency view is recomputed (System 10): the neighbours are the same, but amounts change and an added synergy rule may create new links. No RNG is drawn. On refusal nothing changes and a result code is returned: `NotPlanning`, `NotInGridOrStash`, `NoUpgrade`, `AlreadyUpgraded`, `NotEnoughGold(shortfall)`.
10. **Sell price.** By tier: White 1, Green 2, Blue 3, Purple 5, Gold 8, Red 12. A `+` item sells for the **next** tier's price (White+ 2, Green+ 3, Blue+ 5, Purple+ 8).

### Upgrade Examples

The full list of `+` definitions is content, authored with the item catalog. These follow the rules; the ability row is illustrative only.

| Item + | Change |
|---|---|
| `item_iron_sword_plus` | Damage 9 → 12 (cooldown 2 is below 4, unchanged) |
| `item_venom_vial_plus` | Poison 3 → 4 |
| `item_thorn_dart_plus` | Damage 3 → 4, Poison 1 → 2 |
| `item_leather_vest_plus` | +12 → +16 max HP |
| `item_scholar_lens_plus` | DamagePercent +500 → +667 to detonators (StatusAmount +1 → +2) |
| `item_war_hammer_plus` | Cooldown 4 → 3 |
| `item_plague_catalyst_plus` | Cooldown 4 → 3 (user decision) |
| `item_mortar_plus` (illustrative ability) | Adds a rule: detonator: DetonateValue (all) +1 |

### States and Transitions

| Subject | State | Trigger | Result |
|---|---|---|---|
| Item | Base | Valid `Upgrade` | `+` (final), gold reduced |
| Item | Base or `+` | Refused `Upgrade` | Unchanged, result code |
| Item | `+` | Sold | Removed; gold += next tier's price |
| Run gold | `g` | Room won | `g + GoldByDoor + 2 × purses` |
| Forge | Unavailable | Fighting / Frozen | Drops refused with `NotPlanning`; the drawer is closed as for selling |

### Forge UI ("Rèn")

- **One panel, two tabs.** The sell drawer becomes one panel with tabs **Bán** (sell) and **Rèn** (upgrade). The Bán tab is unchanged.
- **Rèn tab.** An anvil, a drop frame labelled **"Rèn +"** and, while an item is held, its cost and the player's gold.
- **Preview.** While a held item hovers the frame, a before → after comparison shows: the item's popup content with each changed value as `old → new` and an added ability marked as new.
- **Drop.** Dropping on the frame upgrades instantly (no confirm, no undo): the item returns to the cells it was lifted from as its `+` version, and one 6-step highlight pulse plays on its plate (subject to System 29's reduced-flash setting).
- **Refusal.** The item snaps back to its origin with a short message: not enough gold **"Thiếu N vàng"**; already upgraded **"Đã rèn +"**; no upgrade (proposed text) **"Không rèn được"**; an item the Bán tab would refuse gets the Bán tab's message.
- **Badge.** An upgraded item shows a Bone "+" badge, 7×7 px, at the bottom-right of its icon, everywhere the icon appears.
- **Popup.** The hover popup title gets " +"; values that differ from the base item show `old → new`.
- All text is keys (System 7); layout, art and focus order belong to System 25 and the ux-designer.

### Interactions with Other Systems

| System | In | Out |
|---|---|---|
| 2 Game Data | `+` definitions, tier, costs, gold tables | Lint (Rule 4) |
| 5 Run State | Phase, room, door tier into the room | `gold`, upgraded definition on the instance |
| 8 Grid Backpack | Container and cells of the instance | Definition swap in place (no move) |
| 10 Adjacency | — | Recompute after a swap |
| 18 Commitment and Drop | — | Base items only in offers; `+` counts as base |
| 25 Backpack UI, 26 Hover Popup | Drag state, hover | Forge tab, preview, badge, popup title |
| 38 Debuffs and Detonation | Coin Purse, Catalyst | Catalyst+ cooldown |

## Formulas

**Formula 1: upgraded number.** `x' = max(x + 1, (4x + 1) / 3)` (integer division; equals `4x/3` rounded to nearest, since `4x/3` never ends in .5).

| Symbol | Type | Range | Description |
|---|---|---|---|
| `x` | int | 1..100,000 | A beneficial number of the base item (damage, stacks, shield, heal, HP, speed, permille) |
| `x'` | int | 2..133,334 | The `+` value; always at least `x + 1` |

*Examples:* 1 → 2, 2 → 3, 3 → 4, 6 → 8, 9 → 12, 12 → 16, 28 → 37, 500 → 667.

**Formula 2: cooldown.** `n' = n − 1` if `n ≥ 4`, otherwise `n' = n`. Range: `n` 1..99; `n'` never below 3 from this rule. *Example:* Catalyst 4 → 3; Oil Flask 2 stays 2.

**Formula 3: run gold.** `G = G_0 + Σ_{rooms won} D(t_i) + 2 × P_w + Σ sales − Σ costs`.

| Symbol | Type | Range | Description |
|---|---|---|---|
| `G_0` | int | 5 | Starting gold |
| `D(t)` | int | 2, 3, 5, 7, 10 | `GoldByDoor` of the tier of the door into room `i` (room 1 = White; boss 0) |
| `P_w` | int | 0..∞ | Sum over won rooms of Coin Purses in hero grids |
| `G` | int | ≥ 0 | Gold held; an upgrade that would make it negative is refused |

*Output range.* No purse or sale, one hero: minimum 5 (room 1 lost), 7 after room 1; maximum 51 (Green into room 2, Blue 3 and 4, Purple 5 to 7, Gold into room 8; the curse cap allows one Gold door per hero). Purses are unbounded in principle (see Tuning).

*Target profiles* (about five upgrades each):

| Profile | Income | Example composition | Example spend |
|---|---|---|---|
| Cautious | 25 | 5 + 2 + doors W, W, W, G, G, G, G (18) | 4 + 4 + 4 + 6 + 6 = 24 (5 upgrades) |
| Typical | 44 | 5 + 2 + doors G, B, B, B, B, P, P (37) | 6 + 6 + 9 + 9 + 14 = 44 (5 upgrades) |
| Greedy | 63 | 5 + 2 + doors G, B, B, P, P, P, Gold (44) + a purse from room 3 (6 rooms × 2 = 12) | 14 × 4 + 6 = 62 (5 upgrades) |

**Formula 4: sell price.** `Sell(item) = SellByTier[tier + (1 if +, else 0)]`, table `[1, 2, 3, 5, 8, 12]`. *Example:* a Blue item sells for 3, its `+` for 5; net cost of upgrading a Blue item and selling it later is `9 − 2 = 7`.

## Edge Cases

| Case | Handling |
|---|---|
| Gold exactly equal to cost | Allowed; gold becomes 0. |
| Gold short by `N` | `NotEnoughGold(N)`; message "Thiếu N vàng"; item snaps back. |
| Item already `+` | `AlreadyUpgraded`; "Đã rèn +". |
| Cursed Anchor | It is pinned and cannot be lifted (System 19); if reached by any path, `NoUpgrade`. |
| Item on the reward shelf, in the tray or loose | `NotInGridOrStash`, with the Bán tab's message. |
| Drop attempted while Fighting or Frozen | Drawer closed; any call returns `NotPlanning`. |
| Item in the stash upgraded | Allowed; it stays in the stash; it gives and receives no synergy until placed (System 10). |
| An ability adds a synergy rule | New links appear in the recomputed view; the preview shows them before the drop. |
| `+` item sold | Next tier's price; the `+` is gone. |
| Plague Catalyst sold | Allowed by System 15 Rule 4; it is never offered again (Open Questions). |
| Coin Purse moved to the stash before the fight | Pays nothing for that room. |
| Boss won | Pays 0; the run ends, gold is not kept between runs. |
| Room lost | No payment. |

## Dependencies

| System | Direction | Contract | Reverse reference |
|---|---|---|---|
| 2 Game Data Definitions | Upstream | `+` definitions, gold and cost tables, lint | Pending |
| 5 Run State and Flow | Upstream | `gold` in the snapshot; payment at Victory; Planning gate | Pending |
| 8 Grid Backpack | Upstream | In-place definition swap, instance ID kept | Pending |
| 10 Adjacency and Synergy | Downstream | Recompute after a swap | Pending |
| 15 Party and Heroes | Upstream | Signature item may be sold (Rule 4) | Pending |
| 18 Commitment and Drop Model | Downstream | Base items only; `+` counts as base | Pending |
| 19 Curses and Trade-offs | Upstream | Cursed Anchor never upgrades | Pending |
| 20 Currency and Shop (Alpha) | Downstream | Builds on this gold | When designed |
| 25 Backpack UI, 26 Hover Popup, 29 Payoff | Downstream | Forge tab, preview, badge, pulse | Pending |
| 35 Balance Harness | Downstream | Verifies the target of about 5 upgrades per run | Pending |
| 38 Debuffs and Detonation | Peer | Coin Purse, Catalyst+ | [debuffs-and-detonation.md](debuffs-and-detonation.md) |
| 40 Item Fusion | Peer | Shares run gold; fused (Gold-tier) items have no `+`; a consumed `+` ingredient loses its upgrade | [item-fusion.md](item-fusion.md) |

## Tuning Knobs

| Knob | Value | Safe range | Effect | Too high / too low |
|---|---|---|---|---|
| `G_0` | 5 | 0..10 | An upgrade before room 2 | — |
| `GoldByDoor` | 2, 3, 5, 7, 10 | each 1..15, rising | Gold as a reason to bet on a door | Upgrades everywhere / door bet ignores gold |
| `UpgradeCost` | 4, 6, 9, 14 | each 2..25, rising | Upgrades per run (target about 5) | One or two per run / every item upgraded |
| Upgrade factor | ×4/3, at least +1 | ×5/4..×3/2 | Power of a `+` | `+` mandatory / not worth gold |
| Cooldown rule | −1 when ≥ 4 | — | Rhythm of slow items | — |
| Coin Purse payout | 2 per won room | 1..3 | Economy build | A purse loop crowds out combat items / purse ignored |
| Sell table | 1, 2, 3, 5, 8, 12 | — | Liquidity | Selling dominates / never sells |

*Feedback loop.* Coin Purse → gold → upgrades is positive; it is dampened by grid space (a purse is a cell not spent on combat) and by the one-upgrade-per-item limit. Watch purse counts in System 35; a per-run purse cap is the fallback lever.

## Acceptance Criteria

1. **Given** a new run, **then** `gold = 5`.
2. **Given** a run that wins room 1 and then a Blue room, **when** each Victory resolves, **then** gold becomes 7 then 12; winning the boss adds 0.
3. **Given** one Coin Purse in the hero grid and one in the stash, **when** a Green room is won, **then** gold rises by 3 + 2 = 5.
4. **Given** gold 9 and a Blue item in the grid at Arrangement, **when** it is dropped on the "Rèn +" frame, **then** gold is 0, the instance has the same ID, cells and orientation, its definition is `<base>_plus`, and the badge and pulse show.
5. **Given** gold 5 and a Blue item, **when** dropped, **then** the result is `NotEnoughGold(4)`, "Thiếu 4 vàng" shows, the item is back in its cells and nothing else changed.
6. **Given** a `+` item, **when** dropped, **then** `AlreadyUpgraded` and "Đã rèn +".
7. **Given** a fight in progress, **when** `Upgrade` is called, **then** `NotPlanning` and no change.
8. **Given** an item on the reward shelf, **when** dropped on the frame, **then** `NotInGridOrStash` and no change.
9. **Given** the item data, **when** the lint runs, **then** every upgradeable item has exactly one `+`, the Cursed Anchor has none, every changed number matches Formula 1 or 2, and footprint, tags, rotation, tier and trigger kind are equal to the base.
10. **Given** a Purple `+` item, **when** sold, **then** gold rises by 8.
11. **Given** reward offers over 1,000 seeded rooms, **then** no `+` item and no Plague Catalyst is offered.
12. **Given** a held item hovering the frame, **then** the preview shows every changed value as `old → new`; a retained screenshot of the Rèn tab, the preview and a `+` badge is stored in `production/qa/evidence/`.
13. **Given** the same snapshot and command, **when** `Upgrade` runs headless and in Unity, **then** the snapshots are equal.

## Open Questions

| Question | Owner | Resolve by |
|---|---|---|
| Does a Coin Purse in the stash pay? Proposed: no (hero grid only, like other passives). | User | Before the purse is implemented |
| Should the signature Plague Catalyst be sellable? It is never offered again. | User, System 15 | Before the Alchemist ships |
| Which tab the drawer opens on, and keyboard focus between tabs. | ux-designer, System 25 | Before UI stories |
| Should a `+` item weigh more than its base in System 18's commitment score? | System 18 | Before drop tuning |
