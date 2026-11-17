# Item Fusion (Fusion Door)

> **Status**: Designed (user decisions 2026-11-17; independent /design-review not yet run)
> **Author**: user + Claude design session (systems-designer, ux-designer roles taken inline)
> **Last Updated**: 2026-11-17
> **Last Verified**: 2026-11-17
> **Implements Pillar**: Arrangement Is the Thinking (which pieces to give up for a denser, stronger one); Earned Rarity (fused items exist only through a recipe, a door and gold)

## Summary

System 40 adds a fourth kind of door to the corridor: the **Fusion Door ("Lò Dung Hợp")**. Sometimes one of the three doors after a won room is a fusion room instead of a fight. The fusion room has no monsters, pays no gold, no door buff and no item offer, and still counts as one of the floor's rooms. Inside, the player may fuse once: two or three owned items that match a recipe become one new **Gold-tier** item that is stronger than the ingredients together and takes fewer cells. A 2-item fusion costs 6 gold, a 3-item fusion 10 gold. The recipe book is open: every recipe is visible in the fusion room, and an ingredient's popup names what it can become.

> **Quick reference** — Layer: `Feature` · Priority: `MVP` · Key deps: `Game Data Definitions (2), Run State and Flow (5), Grid Backpack (8), Adjacency and Synergy (10), Door Generation and Tiers (17), Item Upgrades (39)`

## Overview

The user asked on 2026-11-17 for "a door that lets two items be fused, with recipes; fusion creates a new item stronger than the old ones; 2 or 3 items depending on the recipe". User decisions, same day: the fusion door **replaces a fight** (no monsters, gold, buff or item offer; it counts as a room); **one fusion per visit, paid in gold** (6 for two items, 10 for three); an **open recipe book**; **12 recipes** (8 two-item, 4 three-item), each producing a new Gold-tier item with its own generated icon. This system owns the recipes, the fused items' rules, the fusion door's appearance rule, the fusion room's phase and the fusion operation. Gold itself belongs to System 39.

## Player Fantasy

The Crusader has carried a sword with a whetstone beside it since room 1. In the corridor before room 4 a door glows with a crucible instead of a monster count. The player weighs it: no fight means no gold, no buff, no new item, but the sword and the stone could become one blade that hits harder and frees a cell. They step in, pay six coins, watch the two plates fly into the crucible and a gold-framed blade drop into the stash. **I don't just find my build, I forge its pieces into something that did not drop.**

## Detailed Design

### Core Rules

1. **Recipes.** A recipe is authored data: an ID, 2 or 3 ingredient item IDs (base items, never `+`), and one result item ID. Two recipes never share the same ingredient multiset. A recipe may name the same item twice (none of the 12 MVP recipes does).
2. **Fused items.** Every recipe result is its own item definition: tier **Gold** ("Huyền thoại"), no upgrade (`upgradeId` empty, System 39 Rule 7 has no Gold cost), never offered as a reward (System 18 draws White to Purple), never an ingredient. It sells for the Gold price, 8 (System 39 Rule 10). Its tags are chosen so the synergies that fed the ingredients still find it (a fused blade keeps `blade`).
3. **Power and size (lint).** For every recipe, `Power(result) ≥ M_k × Σ Power(ingredient +)` (Formula 1, `M_2 = 1.60`, `M_3 = 1.75`), where each ingredient is valued as its upgraded `+` version, and `cells(result) < Σ cells(ingredient)`. A fused item is always stronger than what it consumed even after forging, and always frees space. (User decision 2026-11-17 after simulation: the first tuning, ×1.25 / ×1.40 of the plain ingredients, made the door a trap; see Balance.)
4. **The fusion door appears** on rows leading to rooms `FusionMinRoom` (3) to `Rooms` (8). For each such row, after System 17's six draws, the row is **eligible** when fewer than `FusionMaxPerRun` (2) fusion doors have been offered and the previous row had none. An eligible row makes exactly one bounded draw (0..999) on the separate RNG stream `fusion`; the row gets a fusion door when the draw is below `FusionChance` (350), or **always** when no fusion door has been offered yet and the target room is at least `FusionGuaranteeRoom` (6). Ineligible rows make no draw. The `doors` stream is never touched, so System 17's 43 calls per run are unchanged.
5. **Which card.** The fusion door replaces the card with the **lowest tier** (the safest), the leftmost on a tie. The replaced card's tier, buff, encounter and curse are discarded; its draws have already been made. A row never has two fusion doors.
6. **Choosing it.** `ChooseDoor` on the fusion card moves to the next room and to the **Fusion** phase. There is no fight, no Victory transition, no gold, no buff claim and no item offer. The room appears in the corridor history as a fusion room, with the recipe made (if any).
7. **In the Fusion phase** the player may arrange, sell and forge as in Planning (the grid is editable), and may **fuse at most once**. Leaving the room builds the next row (rooms 2 to 7 lead to a row; after room 8 the boss follows) and moves to DoorChoice, exactly as after a reward step.
8. **Fuse(recipe).** Checked in this order: phase is Fusion (`NotFusionRoom`); no fusion yet this visit (`AlreadyFused`); recipe exists (`UnknownRecipe`); every ingredient is owned in the hero grid or the stash, a `+` item counting as its base (`MissingIngredient(names)`); `gold ≥ cost` (`NotEnoughGold(shortfall)`); the result fits in the stash after the ingredients are removed, else in the hero grid (`NoSpace`). On success, in one step: the ingredients are removed, gold falls by the cost, the result is created in the stash (or the grid), and the visit is marked fused. No RNG is drawn. On refusal nothing changes.
9. **Which instances are consumed.** For each ingredient, among the owned instances of that base item: base before `+` (an upgrade is never lost when a plain copy exists), then stash before grid, then the lowest instance ID. A consumed `+` loses its upgrade; the fusion room warns before the press.
10. **Cost.** `FusionCost(k)`: 6 gold for a 2-item recipe, 10 for a 3-item recipe (`FusionCost2`, `FusionCost3`).
11. **Open book.** The fusion room lists every recipe. The hover popup of an ingredient adds "Dùng để luyện: <result> (cùng <other ingredients>)"; the popup of a fused item adds "Luyện từ: <ingredients>".

### The 12 MVP recipes

Power is per owner action against one enemy (Formula 1), the ingredients valued as their `+` versions. Numbers are provisional (System 35); `tools/art/item_catalog.py` is the source and lints them.

| # | Result (`item_*`) | Ingredients | Shape | Result effect | Σ Power (ingredients +) → Power | Cells |
|---|---|---|---|---|---|---|
| 1 | Kiếm Mài Bén / Honed Sword (`honed_sword`) | Iron Sword + Whetstone | 1x3 | Cooldown 2: 28 damage | 7.5 → 14.0 (×1.87) | 4 → 3 |
| 2 | Khiên Gai Kép / Thornwall (`thornwall`) | Spiked Buckler + Oak Buckler | 1x2 | Cooldown 2: shield 24, 10 damage | 9.5 → 17.0 (×1.79) | 3 → 2 |
| 3 | Rìu Song Nhận / Twin Axe (`twin_axe`) | Hand Axe + Dagger | L | Cooldown 2: 38 damage | 10.3 → 19.0 (×1.84) | 4 → 3 |
| 4 | Phi Tiêu Kịch Độc / Venom Dart (`venom_dart`) | Thorn Dart + Venom Vial | 1x1 | Cooldown 1: 7 damage, 7 Poison | 8.0 → 14.0 (×1.75) | 2 → 1 |
| 5 | Bom Dầu Lửa / Firebomb (`firebomb`) | Ember Charm + Oil Flask | 1x2 | Cooldown 2: 8 Burn, 4 Burn to all enemies | 3.5 → 6.0 (×1.71) | 3 → 2 |
| 6 | Nỏ Gai Độc / Venom Crossbow (`venom_crossbow`) | Crossbow + Thorn Dart | 2x1 | Cooldown 2: 32 damage, 13 Poison | 12.7 → 22.5 (×1.78) | 3 → 2 |
| 7 | Chuông Ru / Lullaby Bell (`lullaby_bell`) | Drowsy Bell + Cracked Mask | 1x2 | Cooldown 2: 5 Slow, 5 Weaken | 2.5 → 5.0 (×2.00) | 3 → 2 |
| 8 | Huyết Dược Cô Đặc / Elixir (`elixir`) | Field Bandage + Crimson Vial | 1x1 | Cooldown 3: heal 25, shield 13 | 7.0 → 12.7 (×1.81) | 2 → 1 |
| 9 | Kiếm Thánh Thệ / Oathkeeper (`oathkeeper`) | Oath Blade + Spiked Buckler + Heart Charm | 1x3 | Cooldown 2: 36 damage, shield 29 | 16.6 → 32.5 (×1.95) | 6 → 3 |
| 10 | Tim Dịch Hạch / Plague Heart (`plague_heart`) | Plague Catalyst + Mortar + Venom Vial | 1x2 | Cooldown 3: 7 Poison to all enemies, then detonate every debuff; neighbours with an affliction tag apply +1 debuff | 8.5 → 15.7 (×1.84, at 6 stacks) | 4 → 2 |
| 11 | Chùy Băng Vĩnh Cửu / Everfrost Maul (`everfrost_maul`) | Glacier Maul + Frost Shard + Runed Mace | 2x2 | Cooldown 2: 67 damage and 4 Frost to all enemies | 18.2 → 35.5 (×1.95) | 9 → 4 |
| 12 | Nỏ Cuồng Phong / Storm Crossbow (`storm_crossbow`) | Crossbow + Swift Boots + Hourglass | 2x1 | Cooldown 1: 24 damage | 11.6 → 24.0 (×2.07) | 7 → 2 |

Notes: the Crusader's starting kit (Iron Sword, Whetstone, Spiked Buckler) makes recipe 1 from room 1; the Alchemist's kit (Thorn Dart, Mortar, Plague Catalyst) needs one Venom Vial (a White drop) for recipe 4 or recipe 10, a choice between 6 and 10 gold. Recipe 10 consumes the Alchemist's signature item by choice; the result keeps the `detonator` tag, so Scholar's Lens, Resonant Bell and Plague Censer still feed it. An item that is not passive carries no HP or speed bonus (item catalog lint), so the Heart Charm's HP and the Swift Boots' speed are paid back as shield and as a faster cooldown.

### States and Transitions

| Subject | State | Trigger | Result |
|---|---|---|---|
| Run | DoorChoice | `ChooseDoor` on a fusion card | Fusion (room + 1), not fused |
| Run | Fusion, not fused | Valid `Fuse` | Fusion, fused; ingredients gone, result in the stash, gold − cost |
| Run | Fusion | Refused `Fuse` | Unchanged, result code |
| Run | Fusion | `LeaveFusion`, room < 8 | DoorChoice with the next row |
| Run | Fusion | `LeaveFusion`, room = 8 | Planning in the boss room |
| Row | Built | Eligible and drawn below chance, or guaranteed | Lowest-tier card becomes the fusion door |

### Fusion Door and Fusion Room UI

- **Door card.** An iron card body (not a tier frame) with a crucible emblem, the title **"Lò Dung Hợp"**, the lines **"Không có quái"** and **"Dung hợp 1 lần"**, and **"Làm được: N"** (recipes makeable now, counting gold). Its detail line: "Phòng không có quái: không vàng, không buff, không món thưởng. Dung hợp 1 lần: 2 món 6 vàng, 3 món 10 vàng. Vẫn tính là 1 phòng." The corridor preview shows no danger for it.
- **Fusion room.** The stage shows no enemies. The run overlay over the enemy zone shows the header "Lò Dung Hợp · Phòng N", a recipe list in two columns (result glyph, name, cost; makeable rows in Candle, others in Fog), and a detail plate with the armed recipe: ingredient glyphs with "có / thiếu", the result's glyph, name and effect, the cost against the gold held, and the warning "Mất + của …" when a `+` would be consumed. While a recipe is armed, the instances that would be consumed carry the hover ring on the board.
- **Actions.** "Dung hợp" (Enter) is enabled only when the armed recipe is makeable; "Rời lò" (K) always leaves. After a fusion the list locks and the message reads "Đã dung hợp. Rời lò để đi tiếp."
- **Motion** (feedback-motion.md style, skippable, reduced motion collapses to the end state): the cost's coins fly from the gold counter to the crucible; each ingredient plate flies to the crucible and vanishes; sparks and a double ring at the crucible; the result plate flies from the crucible to its stash cell and lands (thunk and ring).
- All text is keys (System 7); the strings above are the Vietnamese values.

### Interactions with Other Systems

- **System 5 Run State.** Adds the Fusion phase and a fusion room record; a fusion room is one of the 8 rooms.
- **System 17 Door Generation.** Unchanged draws; the fusion pass runs after the row is final, on its own stream.
- **System 18 Commitment and Drops.** Fused items are never offered; a fused item's tags count for commitment like any owned item.
- **System 19 Curses.** A fusion door is never Gold-tier and carries no curse; the Cursed Anchor is never an ingredient.
- **System 39 Forge.** Fused items cannot be upgraded; a `+` ingredient loses its upgrade.
- **System 10 Adjacency.** Recomputed after a fusion (the ingredients' links disappear, the result's appear when placed).

## Formulas

**Formula 1: power per action (authoring lint).** For an item with trigger cooldown `n` (Reaction items use their expected firings per owner action): `Power = (Σ damage + Σ status stacks + Σ shield + Σ heal) / n`, counted against one enemy (an all-enemies effect counts once). A passive item counts `hpBonus / 10` (a fight lasts about ten owner actions). A synergy ingredient counts the bonus it gives its recipe partner per action (Whetstone +3 damage on a cooldown-2 sword = 1.5). Speed and cooldown reduction count the extra firings they buy their partner (Hourglass on a cooldown-3 crossbow = 15/2 − 15/3 = 2.5; Swift Boots +3 speed ≈ 0.8). A detonation counts 3 per stack at a reference of 6 stacks. Each ingredient is valued as its `+` version (System 39), so a fusion has to beat forging the pieces.

| Symbol | Type | Range | Description |
|---|---|---|---|
| `k` | int | 2..3 | Ingredients in the recipe |
| `M_k` | float | 1.60 (k = 2), 1.75 (k = 3) | Minimum power multiplier of the result over the `+` ingredients |
| `Power` | float | 0..∞ | Value per owner action, Formula 1 |

*Output.* Every MVP recipe meets `Power(result) / Σ Power ≥ M_k`; see the recipe table (lowest 1.71, highest 2.07); `tools/art/item_catalog.py` computes and lints it.

**Balance (simulation, 2026-11-17).** Engine-free CombatCore, 600 seeded runs per line, the green-door bot taking the best reward and forging (scratchpad harness, not in the repo; System 35 replaces it). "Always fuse" takes a fusion door whenever a recipe is complete and gold allows, keeping the recipe's cost aside from the forge.

| Tuning | Crusader clear | Alchemist clear | Note |
|---|---|---|---|
| Never fuse (fusion doors present, avoided) | 41% | 30% | Baseline |
| Enter every fusion door, fuse nothing | 26% | 16% | One skipped room is worth about 10 points |
| First tuning (×1.25 / ×1.40 of the plain ingredients), always fuse | 31% | 25% | The door was a trap |
| ×1.2 more, always fuse | 42% | 29% | Break-even |
| **Chosen: about ×1.4 more (×1.71..2.07 of the `+` ingredients), always fuse** | **53%** | **33%** | User decision: the door is nearly always worth taking with a complete recipe |

Fusion doors seen: 1.54 per run. Crusader fusions are mostly the Honed Sword (its kit holds both ingredients); the Alchemist's are the Plague Heart, the Venom Dart and the Venom Crossbow.

**Formula 2: fusion door chance per run.** With `c = FusionChance / 1000` and rows to rooms 3..8, the chance that a run offers no fusion door before room 6 is `(1 − c)^3 ≈ 0.27` for `c = 0.35`, in which case the row to room 6 is forced. Expected fusion doors per run ≈ 1.5 (simulated value in the balance note).

**Formula 3: cost.** `FusionCost(k) = 6` for `k = 2`, `10` for `k = 3` (user decision: kept at 6 / 10 after the simulation; lowering the cost to 3 / 5 was the alternative). *Example:* Crusader after rooms 1 and 2 (White, Green): `5 + 2 + 3 = 10` gold, enough for a 2-item fusion with 4 left for a White upgrade.

## Edge Cases

| Case | Handling |
|---|---|
| Fusion room with no makeable recipe | Allowed; the player may only arrange, sell, forge, then leave. The card's "Làm được: 0" warned them. |
| Two copies of an ingredient, one `+` | The plain copy is consumed (Rule 9). |
| Only a `+` copy owned | Consumed; the detail plate shows "Mất + của <name>" before the press. |
| Ingredient sold or moved after a recipe is armed | Makeability is recomputed on every change; the press is checked again. |
| Stash full | The ingredients' cells free first; if the result still fits nowhere, `NoSpace` and nothing changes. |
| Fused item at the forge | "Không rèn được" (System 39 `NoUpgrade`). |
| Fusion door in the row to room 8 | Leaving goes straight to the boss. |
| Fusion chosen twice in a run | Allowed when offered twice (rows never consecutive). |
| Recipe uses the Plague Catalyst | Allowed; the player gives up the signature by choice; the result keeps `detonator`. |
| Pinned Cursed Anchor | Never an ingredient, never moved. |
| Row whose lowest tier appears twice | The leftmost of them is replaced. |
| Debug ForceGold | Applies before the fusion pass; the fusion door then replaces the lowest card, never the forced Gold one unless all three are Gold. |

## Dependencies

| System | Direction | Contract | Reverse reference |
|---|---|---|---|
| 2 Game Data Definitions | Upstream | Recipe and fused item data, lint | Pending |
| 5 Run State and Flow | Upstream | Fusion phase, room count, history | Pending |
| 8 Grid Backpack | Upstream | Remove ingredients, place the result | Pending |
| 10 Adjacency and Synergy | Downstream | Recompute after fusion | Pending |
| 17 Door Generation | Upstream | Row final before the fusion pass; separate stream | Pending |
| 18 Commitment and Drops | Downstream | Fused items never offered | Pending |
| 26 Hover Popup | Downstream | Recipe lines on ingredients and results | Pending |
| 35 Balance Harness | Downstream | Fusion policy, door frequency, clear rates | Pending |
| 39 Item Upgrades | Peer | Gold, sell price, no `+` on fused items | [item-upgrades.md](item-upgrades.md) |

## Tuning Knobs

All in `run_mvp.json` → `fusion`, except the recipes and fused items (`tools/art/item_catalog.py`).

| Knob | Value | Safe range | Effect | Too high / too low |
|---|---|---|---|---|
| `minRoom` | 3 | 2..6 | First row that may hold the door | Fusion before any ingredient drops / too late to matter |
| `chance` | 350 ‰ | 150..600 | Door frequency | Fights skipped too often / door rarely seen |
| `guaranteeRoom` | 6 | 4..8 | Every run sees at least one | — |
| `maxPerRun` | 2 | 1..3 | Fusion rooms per run | Run becomes short / one chance only |
| `cost2`, `cost3` | 6, 10 | 3..12, 6..18 | Competes with the forge | Fusion never bought / always bought |
| `M_2`, `M_3` | 1.60, 1.75 (against `+` ingredients) | 1.3..2.0 | Fusion payoff | Fusion mandatory / not worth a room (below about 1.4 the door is a trap) |

## Acceptance Criteria

1. **Given** the item data, **when** the lint runs, **then** every recipe has 2 or 3 existing base ingredients, a Gold-tier result that is no ingredient and has no upgrade, a unique ingredient multiset, `cells(result) < Σ cells`, and meets Formula 1's multiplier.
2. **Given** seeds over 2,000 runs with a bot that never chooses fusion, **then** the `doors` stream sees exactly 43 calls per completed run, every run offers 1 or 2 fusion doors, no two consecutive rows hold one, and none appears before the row to room 3.
3. **Given** a row `[White, Green, Blue]` and a fusion draw, **then** the row becomes `[Fusion, Green, Blue]`.
4. **Given** DoorChoice with a fusion card, **when** it is chosen, **then** the room number rises by 1, the phase is Fusion, the stage has no enemies and no gold is paid.
5. **Given** the Fusion phase, gold 9, an Iron Sword in the grid and a Whetstone in the stash, **when** recipe 1 is fused, **then** gold is 3, both items are gone, a Honed Sword is in the stash, and a second `Fuse` returns `AlreadyFused`.
6. **Given** gold 5, **when** recipe 1 is fused, **then** `NotEnoughGold(1)` and nothing changes.
7. **Given** an Iron Sword and an Iron Sword `+`, **when** recipe 1 is fused, **then** the plain sword is consumed and the `+` stays.
8. **Given** the Fusion phase, **when** the player leaves, **then** the next row is built (or the boss room entered after room 8) and the history records the fusion room and its recipe.
9. **Given** a fused item, **then** the forge refuses it, its sell price is 8, and it never appears in 1,000 seeded reward offers.
10. **Given** the fusion room in Play Mode, **then** retained screenshots in `production/qa/evidence/` show the fusion door card, the recipe list with the detail plate, the marked ingredients, and the result in the stash.
11. **Given** the same seed and commands, **when** a run with a fusion room plays headless and in Unity, **then** the run snapshots are equal.

## Open Questions

| Question | Owner | Resolve by |
|---|---|---|
| Should recipes unlock over runs (meta progression) instead of all being known? | User, System 20 | Alpha |
| Should a fused item be upgradable (`+`) at a higher cost? | User, System 39 | After playtests |
| Should the result go to the cells of the main ingredient when it fits, instead of the stash? | ux-designer | After playtests |
| Should the fusion door's chance rise when the player holds a complete recipe? | systems-designer | After System 35 numbers |
