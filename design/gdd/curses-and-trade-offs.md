# Curses and Trade-offs

> **Status**: Designed (CD-GDD-ALIGN CONCERNS, five concerns applied; independent `/design-review` not yet run)
> **Author**: user + Claude design session, with specialist consultation
> **Last Updated**: 2026-11-13
> **Last Verified**: 2026-11-13
> **Implements Pillar**: Power Has a Price (primary); Earned Rarity (caps what a curse may give back); Arrangement Is the Thinking (a curse must change how the player builds)

## Summary

Curses and Trade-offs adds the price that the Gold door carries on top of its harder room. A Gold door is offered only with a curse, shown on its card before the choice; when the player wins the room behind it, the door's buff and its curse are claimed together on one hero of their choice. A curse is one of four deterministic kinds fixed by the door's template and never drawn: a pinned cursed item in the hero's grid that cannot be moved, Wither (damage each time the hero takes a turn), Frailty (less max HP) or Sluggish (less speed). It is permanent for the run, at most one per hero and two per party, and is held as a short list in the run snapshot, with no meter, tree or currency. Red is deferred to Tier 3.

> **Quick reference** — Layer: `Feature` · Priority: `Vertical Slice` · Key deps: `Door Generation and Tiers, Encounter Generation, Grid Backpack, Party and Heroes, Run State and Flow, Game Data Definitions`

## Overview

System 19 adds the **curse**, the price that a Gold door carries on top of its harder room. A Gold door is only offered with a curse, shown on its card before the player chooses; the player who wins the room behind it claims the door's buff and its curse together, on one hero of their choice (who carries the gift also carries the scar). A curse is one of four kinds, all deterministic, authored with the door's encounter template and never drawn: a **pinned** cursed item that is created in the chosen hero's grid and cannot be moved or removed (it takes space and cuts adjacency lines), **Wither** (the afflicted hero takes damage each time it acts), **Frailty** (less max HP) and **Sluggish** (less speed, so a later place in the turn order). A curse is permanent for the rest of the run; only an event room (System 21, Alpha) will be able to cleanse one. At most one curse per hero and two per party are active, and the Gold door is not offered while two are. The curses are plain data in a short list in System 5's snapshot, with no meter, no curse tree and no currency, so they stay inside the anti-pillar. Red, the sixth tier, is deferred to Tier 3. Without this system Gold would be a Purple door with a bigger buff; with it, the top door asks the player what they will let it change in their build (Pillar 5).

> *Specialists consulted for this section: game-designer, systems-designer, economy-designer, art-director, creative-director (2026-11-13). Their findings are applied below.*

## Player Fantasy

The player reads the Gold door: a Purple-class room, a gift of 16 max HP and a new cell, and a mark at the bottom of the card. The mark is a pinned anchor that would sit in the Ranger's grid and cut its adjacency lines, or a Wither that would burn the Cleric every time she acts. They look at their build and decide whether they can build around that mark: the same door is cheap for one arrangement and costly for another. They take it, win the room, put the gift and the scar on the hero who can carry both, and the endgame (rooms 7 and 8 and the boss) is shaped by the scar as much as by the gift. **The top door names a mark it leaves on you; you take it because you can build around it, and the endgame is shaped by the scar as much as the gift.**

*Pillar anchors.* Pillar 5, Power Has a Price: "a drawback that changes how the player builds counts; one the player can ignore does not." A curse acts on space (a pinned item), order (a slower hero) or role (a hero that wears itself down), and each curse has an answer in the arrangement. Pillar 3, Earned Rarity: the curse is paired with a bounded reward and the cap of two keeps it from compounding. Pillar 4, Arrangement Is the Thinking: the curse is shown in full before the choice, and nothing about it is rolled afterwards.

*Guardrails* (binding rules): the card states the exact curse before the choice; no curse is a flat percentage of damage dealt; every curse is expressible in the arrangement (a build that answers it does measurably better than one that ignores it, System 35); a curse never changes a later room's roster or multipliers (it can make that room harder to survive, which is its price, and System 35 measures it); Gold keeps an uncursed alternative in every row (System 17's safe-card mask); the curses are never drawn and never stack multiplicatively.

*Risks the design guards against:* a curse that is only a number, a random tax that makes the player price the colour and stop reading the card, a curse the player can ignore, a spiral in which the curse forces the next risky door, and a "second curse subsystem" (a meter or a tree).

> *`creative-director` consulted (2026-11-13); the spine of the fantasy is theirs.*

## Detailed Design

### Core Rules

1. **Scope.** The Vertical Slice has one cursed tier, **Gold** (the fifth tier). Red (the sixth) is deferred to Tier 3, so its row, buff and curses are absent and its weights are 0. Purple carries no curse. Gold appears from room 6 (`MinRoomForTier` 6) and only with a curse; a Gold template without a `curseId` is a content error. Only about 44 percent of runs see a Gold card (Formula 2), so for playtests a harness or debug policy can force a Gold card in rooms 6 to 8 (System 35); lowering Gold's `MinRoomForTier` to 5 is a tuning option.
2. **Pure data, engine-free.** A curse is a record `CurseDef {id, kind, p1, riderKind, riderAmount, nameKey, glyph}` in the catalog (System 2): `p1` is a positive magnitude; `riderKind` (Speed or MaxHp) and the signed `riderAmount` are used by Pinned only. No function reads a clock or holds a random generator; all arithmetic is 32-bit integer; there is no mutable static state (as Systems 3, 11, 14, 16, 17, 18 and 5).
3. **Four kinds, deterministic, none drawn.**

   | Kind | Acts on | Effect (provisional) | Where it lives |
   |---|---|---|---|
   | **Pinned** | space | A cursed item of `p1` cells (1 or 2; a 2-cell item is a horizontal domino) is created in the chosen hero's grid at the position that System 8's `AutoPlace` picks (orientation 0 first, then the origin in reading order) and cannot be moved, rotated, removed, stashed or auto-placed again (a `pinned` flag and a `Pinned` result code in System 8, even onto its own cell); it is not droppable, has no direction tag, and its links are excluded from System 18's `k`. Its rider (`riderKind` Speed with `riderAmount` −4, or MaxHp with −20) is a curse term read by `HeroStats` outside the item clamps, like Frailty and Sluggish, so other items cannot absorb it. Its cells are occupied, so the items on its sides are not adjacent to each other (System 10 counts only edge contact between different items). | The grid slice (System 8) and the curse list. |
   | **Wither** | role | At the start of each of the afflicted hero's own turns (also when the action is skipped, so a stun does not dodge it) it takes `p1` damage (3), resolved as an ordinary System 3 `Damage` effect with the curse as its source, so a shield absorbs it and it can down a hero at low HP. | A permanent, non-consumable baseline status with a stable contribution ID and a tick origin (a contract change in Systems 3, 4, 12 and 13). |
   | **Frailty** | role | `maxHP` is reduced by `p1` (30) through a curse term outside the item clamp. | `HeroStats` (System 15). |
   | **Sluggish** | order | The hero's base speed sum `F` is reduced by `p1` (4) before its clamp, so it acts later. | `HeroStats` / System 11. |

   *Frailty is a flat number whose only answer is to give it to the tank, so alone it fails Pillar 5's test; it is a fallback kind kept for variety, and only Pinned, Sluggish and Wither change how the player builds.*

4. **Which curse a Gold card carries.** The curse is part of the Gold encounter template and its variant (System 16 Rule 4 gains a `curseId`), so it is known when the card is built, shown on the card, and costs no draw: the 43 calls on `doors` stay 43. Two variants of one Gold cell may carry different curses; the curse is not part of the threat or the danger rating.
5. **Claiming the curse.** The curse is claimed with the door's buff in the same claim step (System 17 Rule 7), after the room behind the door is won and the grid is thawed, so the price is never prepaid. The player chooses one hero, who receives both the buff and the curse. The hero must be able to receive the curse: no active curse on it, and for a Pinned curse a legal placement for the whole shape (the grid is editable at the claim, so the player can free cells first by moving items to the stash or the tray; the tray has no cell limit, so the claim can never soft-lock, and an item left in the tray is lost at the next Freeze with the usual confirmation). A hero that already has a curse is `Rejected(HeroHasCurse)`; with no legal placement the claim is `Rejected(NeedsSpace)`, which keeps the pending curse and draws nothing (check order: stale serial, wrong state, invalid argument, `HeroHasCurse`, `NeedsSpace`). The claim applies, in order: the buff's `Unlock` (so a new cell can hold the pinned item), one atomic `CreatePinned` (create and place; a failed placement never leaves an item in the tray), then the infallible additions (`B_perm`, the counters, the list entry); any failure leaves everything unchanged. A Gold claim is never auto-skipped, because a curse is pending, and the default hero falls back to the lowest eligible slot when the suggested one is cursed or has no space.
6. **Permanent.** An active curse lasts for the rest of the run. In the Vertical Slice there is no way to remove it; an event room (System 21, Alpha) will cleanse one curse. The curse is not lifted by a defeat (the run ends) or a restore.
7. **Caps.** At most 1 active curse per hero and 2 per party. The row is built at the victory, before the claim, so the cap counts the active curses **plus the pending one** (the curse of the Gold door just won): when that count reaches 2, Gold's weight is 0 in every row (a mask computed from the snapshot, with the array length unchanged, so the call count is unchanged). Whether a hero can receive the curse (space) cannot be known at the tier draw, because the curse comes from the variant drawn afterwards and the grid can change before the claim; it is checked at the claim (Rule 5).
8. **The curse list.** The active curses are a short ordered list `(curseId, heroSlot, acquiredRoom, pinnedItemId or 0)` in System 5's snapshot, where `acquiredRoom` is the room whose victory triggered the claim (the room behind the Gold door), sorted by `(acquiredRoom, heroSlot)` (the slot tie-break is unreachable by play, since there is one claim per room), at most 2 entries and at most 1 per hero. Restore also checks that every Pinned entry's item exists in the grid slice with the `pinned` flag and that every pinned item has an entry. It is read by `HeroStats` (Frailty, Sluggish), by System 12's baseline (Wither) and by the grid (the pinned item). Nothing is written into `B_perm`, the grid's other items or the baseline except through these existing fields, so a curse cannot be applied twice.
9. **Validator.** Every Gold template has a `curseId` that exists; a Frailty curse's `|p1|` exceeds the Gold buff's HP (so the buff cannot cancel it); System 16's speed lint is re-run for Gold cells and the boss with the hero's base speed reduced by a Sluggish curse (`s − p1`, at least 8), which with the roster minimum of 22 gives 18; `CurseCapParty ≤ party size`; a Pinned curse's cells fit a hero's start grid with the other starting items; each kind has a glyph; a curse record is never both a tier buff and a curse.
10. **Gold's reward and price.** Buff: +16 max HP and one cell (`DoorCellCap` still 3 per run). The room: System 16's Purple composition with `kick_hp` +300 and `kick_dmg` +100 (the damage kick equals Purple's, so the burst bound of System 16 Formula 4 holds). System 18's item rows get a Gold door row (50/250/450/250 over the four item tiers). The price of Gold is mainly the curse, not extra danger.
11. **The tier table.** Gold has weights from room 6 (provisional): room 6 `[230, 280, 240, 190, 60]`; room 7 `[180, 270, 300, 190, 60]`; room 8 `[140, 260, 300, 230, 70]` for `[White, Green, Blue, Purple, Gold]` (rooms 2 to 5 append a 0); mask (b) of System 17 becomes "cards 1 and 2 are both Blue or higher".
12. **What the card shows.** The tier (form, six rank pips, name), the buff chip, the danger numeral and class counts (System 16), and the curse: one Plum hanging tag (10 px tall, bottom-right) with a Bone glyph of the curse kind and a short numeral, and a Plum edge shadow on the card's right side; a Pinned curse also shows the free-cell requirement, and every curse states "lasts for the run"; the popup states the curse in one plain line (for example "pins a 2-cell anchor, speed −4", "Wither 3 per action", "max HP −30", "speed −4"). The curse is stated before the choice and nothing is rolled afterwards.
13. **Active curses are shown on the world.** An active curse is a Plum tag on the afflicted hero (sorted last in the stat chips and never collapsed into a "+n"), on the pinned item's plate, and in the combat HUD's stat row; there is no separate curse panel or meter.
14. **No retry, no gamble.** A cursed door is final like any door (System 5). In BuffClaim with a Gold pending, only the hero claim and grid commands are accepted; there is no skip and no reroll, and a `NeedsSpace` refusal keeps the same pending curse and makes no draw.

### States and Transitions

| Subject | State | Trigger | Result |
|---|---|---|---|
| Curse | Offered | A Gold card is built (System 17) | The curse is part of the card; nothing is applied. |
| Curse | Pending | The room behind the Gold door is won | The curse waits with the buff to be claimed. |
| Curse | Active | The player claims a hero | The entry joins the list; a pinned item is created; stats and baseline read it from the next encounter. |
| Curse | Cleansed | An event room (System 21, Alpha) | The entry leaves the list; a pinned item is removed. Not in the Vertical Slice. |

### Interactions with Other Systems

| System | Data in | Data out | Interface owner |
|---|---|---|---|
| 2 Game Data Definitions | `CurseDef` records, glyphs, the Gold rows | None | System 2 owns fields; System 19 owns rules. |
| 17 Door Generation and Tiers | The claim step; the tier table | Gold weights, the Gold buff, the curse chip on the card, the masks | System 17 owns the row; System 19 adds the Gold column and the curse mask. |
| 16 Encounter Generation | `curseId` on Gold templates | Gold kicks, composition | System 16 owns the room; the curse is not part of its danger. |
| 18 Commitment and Drop Model | The Gold door row | None | System 18 owns the item rows. |
| 5 Run State and Flow | The curse list in the snapshot; the claim command | The claim result | System 5 stores and orders. |
| 8 Grid Backpack | `pinned` items; `Create`, `AutoPlace` | Free cells for the claim check | System 8 owns the flag. |
| 15 Party and Heroes | `HeroStats` curse terms (Frailty, Sluggish) | Hero stats | System 15 owns the formula; System 19 adds the terms. |
| 12, 13, 3, 36 | Baseline status rows (Wither); a tick policy | Damage events with the curse as source | Those systems own the fight. |
| 21 Room Types | The cleanse | Removal of one curse | Alpha. |
| 27, 28 and the HUD | The curse on the card and on the hero | Display | Those systems draw. |
| 35 Balance Simulation | Curses and policies | None | System 35 tests the bet. |

*Provisional:* the curse values, Gold weights, kicks and buff are assumptions about balance; the `pinned` flag, the curse terms in `HeroStats`, and the baseline status rows (Wither) are assumptions about Systems 8, 15, 12 and 13.

> *Specialists consulted: game-designer, systems-designer, economy-designer, art-director (2026-11-13).*

## Formulas

All arithmetic is on 32-bit integers; division is integer division with non-negative dividends. All numeric values are provisional until System 35 measures them.

**Formula 1: Gold's weight in a row.** The tier array of System 17 gains a fifth entry, `[White, Green, Blue, Purple, Gold]`, with zeros kept. `weights_c = row[r]` with `row[r][Gold] = 0` when `activeCurses + pendingCurse ≥ CurseCapParty` (2), and the remaining weights are not renormalised (the total only has to be positive).

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| `row[r][Gold]` | int | 0 for rooms 2 to 5 or when masked; 60 for rooms 6 and 7; 70 for room 8 | tier table (data) | Gold's permille weight for target room `r`. |
| `activeCurses` | int | `0..2` | curse list | Entries in System 5's list; `pendingCurse` (0 or 1) is the curse of the Gold door just won, not yet claimed. |
| `CurseCapParty`, `CurseCapHero` | int | 2, 1 | tuning knobs | Active curses at most per party and per hero. |

*Examples:* room 6 with no active curse: `[230, 280, 240, 190, 60]` (sum 1000); the same row with two active curses: `[230, 280, 240, 190, 0]` (total 940, still positive); room 8: `[140, 260, 300, 230, 70]`.

**Formula 2: how often a Gold card shows (design check).** The chance that a row holds at least one Gold card is `1 − (1 − w/1000)^3`. *Examples:* room 6 and room 7 (`w = 60`): `1 − 0.94³ = 0.169`; room 8 (`w = 70`): `1 − 0.93³ = 0.196`. Over rooms 6 to 8 the chance of seeing at least one Gold card is `1 − 0.830584² × 0.804357 = 0.445` (the masks change it slightly).

**Formula 3: the curse in a hero's stats.** `maxHP = clamp(h + B_perm + H + C_hp, 1, HP_cap)` with `C_hp = −p1` of the hero's Frailty curse or the HP rider of its Pinned curse (0 if none), outside the clamp of `H`; the hero's speed sum becomes `F' = clamp(F, −50, 50) + C_speed` with `C_speed = −p1` of its Sluggish curse or the speed rider of its Pinned curse (0 if none), added after the item clamp so that the curse always bites (as Frailty does), and the effective speed is System 11's `v = clamp((max(0, s + F')(1000 + P) + 500) / 1000, 1, 200)`. *Examples:* a Crusader with `h = 110`, `B_perm = 16`, `H = 30` and a Frailty of 30: `maxHP = 156 − 30 = 126`. A Ranger with `s = 29`, `F = +2` and a Sluggish of 4: `F' = −2`, `v = (27 × 1000 + 500) / 1000 = 27` against 31 before the curse.

**Formula 4: Wither.** A Wither of `p1` deals `p1` damage to the afflicted hero at the start of each of its own actions, as a System 3 effect with the curse as source. The damage in a fight is `p1 × (the hero's turns in the fight)`. *Example:* `p1 = 3` and 6 turns give 18 damage out of a hero with 82 or more HP (the roster minimum); the hero is restored before the next fight (System 15 Rule 6).

**Formula 5: Gold's room against Purple's (the ladder).** For the same roster and room, `100 × T(Gold) ≥ 105 × T(Purple)`, with `m_hp(Gold) = 1000 + 60(r − 1) + 300` and `m_dmg(Gold) = m_dmg(Purple)`. *Example:* room 8: `m_hp` 1720 against 1620; the elite (HP 155) scales to `(155 × 1720 + 500) / 1000 = 267` against `(155 × 1620 + 500) / 1000 = 251`, a ratio of about 1.064, so the threat ratio is about 106 percent (asserted on whole rosters, for example `26,700 ≥ 26,355`, because per-unit rounding can break the inequality at base HP 1); the damage is unchanged, so the burst bound (57 against 57.4 at Purple room 8) is unchanged. The bound `T(Gold, 8) < T(boss)` is an Open Question.

**Formula 6: draw calls.** A curse makes no draw: the curse is part of the template. The count on `doors` stays `1 + 7 × 6 = 43`, and Gold's weights change word consumption only through the total of a weighted draw, so adding the Gold column is a `doorContentVersion` bump.

**Worked example (a Gold door with a Pinned curse into room 6).** Row into room 6: weights `[230, 280, 240, 190, 60]` (cumulative totals 230, 510, 750, 940, 1000); a ticket of 950 for card 1 is at least 940 and below 1000, so the card is Gold. The card shows the Gold form, six pips, the buff chip `+16 max HP, +1 cell`, the danger numeral, and the Plum tag with a notched-square glyph and the line "pins a 2-cell anchor, speed −4". The player takes the door, wins room 6, thaws the grid, and claims: they choose hero slot 1, who has 4 free cells. The buff gives `hpGranted = min(16, 100 − doorHp)`; the curse creates the anchor (2 cells) at the first legal position in reading order and adds `(curseId, 1, 6, itemId)` to the list; the active curses are now 1. In rooms 7 and 8 and the boss the anchor stays where it was put, and its rider lowers hero 1's speed sum by 4 (Formula 3).

> *`systems-designer` and `economy-designer` consulted (2026-11-13); the overlay terms and the Gold rows are theirs.*

## Edge Cases

Format: *if condition: exact outcome* (the party that detects it). "Validator" is the System 19 content validator at catalog build; "S2" is System 2's record validation.

**The claim and the caps**

- *A Gold card is built when one curse is active and one is pending (the Gold door just won):* the row is built at the victory, before the claim, so the cap counts active plus pending; Gold's weight is 0 in every row, the array keeps its length 5, the total is 940 or 1000, and the call count stays 43. The weighted draw uses the real total, so word consumption differs; this is deterministic and pinned by fixtures; the array is never renormalised or compacted (System 17).
- *Whether any hero can receive the curse:* it cannot be a tier mask, because the curse comes from the variant drawn after the tier draws and the grid can change before the claim. A party has three heroes and the cap is two, so an uncursed hero always exists; non-Pinned curses are always receivable; only a Pinned curse without space is a real case, handled at the claim (below). The validator requires `CurseCapParty ≤ party size`.
- *A Pinned curse and the chosen hero has no legal placement even after the buff's `Unlock`:* `Rejected(NeedsSpace)`, nothing changes; the player moves items to the stash or the tray (the tray has no cell limit, so it never soft-locks; `AbandonRun` is always legal); the staging clone counts the newly unlocked cell (System 5).
- *The chosen hero already has a curse:* `Rejected(HeroHasCurse)`; the default hero falls back to the lowest eligible slot.
- *The hero's HP cap is reached or no cell can be granted:* the curse is still claimed (System 17's consumed-without-prompt rule does not apply to a Gold buff, because a curse is pending).
- *Two Gold cards in one row:* legal, with the same or different curses; card 3 cannot be Gold (masks a and b), so at most two per row, and only one can be taken.
- *The claim is atomic:* `Unlock`, then `CreatePinned`, then `B_perm` and the counters and the list entry, with the item-ID `Next()` last; any callee `ContractViolation` is a `TechnicalStop` with no reward and no ID consumed (System 5).
- *Purple is cut:* Gold is cut with it, because it uses Purple's composition (validator).
- *Red:* absent in the Slice; adding the column is a `doorContentVersion` bump.

**The Pinned item (System 8, new `Pinned` result code)**

- *Place, Move, rotate, Remove, send to stash or AutoPlace of the pinned item:* `Pinned`, even onto its own cell.
- *Undo after the claim:* `NothingToUndo` (`Create` clears the undo record); a later Place can never restore onto pinned cells.
- *Freeze:* the pinned item is never in the tray, so tray destruction never reaches it; one atomic `CreatePinned` prevents a tray orphan.
- *Overlap with locked cells or starting items:* placement needs open cells; the validator checks the start block with the starting items.
- *`doorCells`:* the pinned item occupies cells but is not counted.
- *Commitment (System 18):* the pinned definition has no direction tag, is not droppable, and its links are excluded from `k`.

**Wither**

- *A hero at low HP:* the tick is an ordinary `Damage` effect, so it can down the hero; the shield absorbs first (the start shield `q0` blunts at most `q0 / p1` ticks); a hero downed by the tick skips the action, which counts toward the 200-action limit, and the outcome is evaluated at the boundary.
- *Order at a hero's turn:* the Wither tick, then the stun check, then items; a stunned or no-ready-item turn still ticks, because the clock is the turn order. This is a new row in System 3's canonical order contract.
- *Reaction items:* they never add hero turns, so there is no double tick; the tick is its own root action and does not consume a Reaction item's once-per-root listener.
- *The rail:* System 3's origins are an item, an enemy move or a status tick tied to a contribution, and System 13 defines only Ấn, so Wither needs a permanent, non-consumable baseline status with a stable contribution ID and a tick origin (Open Question).

**Stats**

- *Frailty:* `maxHP = clamp(h + B_perm + H + C_hp, 1, HP_cap)`; with the roster minimum 82 − 50 − 30 the clamp is a guard only; `doorHp ≤ B_perm` is unaffected because the curse never touches `B_perm`.
- *Sluggish:* System 16's speed lint is re-run for Gold cells and the boss with `s − p1` (the roster minimum 22 − 4 = 18, at least 8).
- *`HeroStats` and the baseline:* `HeroStats` takes the curse list as an input, so the preview before Start equals the combat baseline.
- *The boss and a defeat:* the boss is fought with the curses unchanged (a room 8 claim applies to the boss); a defeat ends the run (the summary lists the curses), and no pending curse can exist at a defeat.
- *No cleanse in the Slice:* the tag shows "lasts for the run".

**Restore (System 5 Rule 17; reject the whole restore)**

- A Pinned entry with item ID 0, a missing item, an item in the wrong grid, the stash or the tray, without the `pinned` flag, or with the wrong definition or footprint; a pinned item without an entry; more than 2 entries or 2 on one hero; entries not strictly increasing by `acquiredRoom`; `acquiredRoom` outside 6 to 8 or greater than `room`; `active + pending > 2`. A moved pinned item cannot be detected, because the grid is the only source of position.
- The run snapshot gains the curse list and the grid slice gains the `pinned` flag, so old snapshots are rejected (format bump).

**Versioning**

- *The tier array going from 4 to 5 entries:* a `doorContentVersion` bump; golden fixtures are regenerated; rooms 2 to 5 tickets are unchanged (a zero tail adds nothing to the total).

**Balance (what the burst bound does not cover)**

- *A cursed hero can fall in one round from full HP* (Frailty or a Pinned HP rider lowers the weakest hero to 52 or 62 against the burst bound's 82): the burst bound of System 16 is not extended to curses, which is accepted as part of the price and is measured by System 35 (Open Question).

> *`systems-designer` consulted (2026-11-13); its defects A to H are applied (the cap counts pending, the receiver check at the claim, the corrected ratio, `AutoPlace` placement, the curse terms outside the clamps, the schedule totals, the Wither rail).*

## Dependencies

| System | Direction | Nature | Notes |
|---|---|---|---|
| 17 Door Generation and Tiers | Depends on (hard) | The claim step, the tier table (Gold column), the masks, the card | System 19 adds the Gold column, the curse chip and the cap mask. |
| 16 Encounter Generation | Depends on (hard) | Gold templates carry a `curseId`; Gold kicks and composition | The curse is not part of the danger. |
| 2 Game Data Definitions | Depends on (hard) | `CurseDef` records, glyphs, the Gold rows | |
| 8 Grid Backpack | Depends on (hard) | The `pinned` flag, `Create`, `AutoPlace`, free cells | A new flag in System 8. |
| 15 Party and Heroes | Depends on (hard) | The curse terms in `HeroStats` (Frailty, Sluggish) | `maxHP` and `F` formulas gain terms. |
| 3 Effect Resolution, 13 Status Effects, 12 Combat Resolution, 36 | Depends on (hard) | Wither: a baseline status row and a tick at the start of the afflicted hero's own action, with the curse as source | System 3 is "Needs Revision"; the tick policy and the baseline status rows are open in Systems 12 and 13. |
| 5 Run State and Flow | Depends on (hard) | The curse list in the snapshot; the claim command | Snapshot format gains a section. |
| 18 Commitment and Drop Model | Depends on (soft) | The Gold door row for the item tiers | |
| 21 Room Types | Depended on by | The cleanse (Alpha) | |
| 27, 28 and the combat HUD | Depended on by | The curse on the card and on the hero | |
| 35 Balance Simulation Harness | Depended on by | Tests the bet, the dominance and the purge-free slice | |

The systems index lists System 19 as depending on 17, 3 and 2; it also needs 8, 5, 15, 16, 12 and 13. There is no cycle: System 19 reads Systems 17 and 16's outputs and extends their tables.

## Tuning Knobs

All values are provisional until System 35 measures them. They live in data (System 2), never in code.

| Knob | Default | Safe range | Affects | Failure at the extremes |
|---|---|---|---|---|
| Gold weights by room | 60, 60, 70 (rooms 6 to 8) | each `0..150` | How often the cursed door appears | High: runs are mostly cursed; low: the tier is rarely seen. |
| `MinRoomForTier` Gold | 6 | `5..7` | When the first curse can arrive | Early: more fights are cursed; late: the curse barely matters. |
| Gold buff (HP, cell) | 16, 1 | HP `12..30` | The gift against the scar | High: a free upgrade; low: Gold is never worth it. |
| Pinned footprint and rider | 1 or 2 cells, speed −4 or HP −20 | cells `1..3`, rider within the item ranges | How much space and what penalty | Large: the grid is unplayable; small: ignorable. |
| Wither damage per action | 3 | `1..6` | Wear on the afflicted hero | High: the hero is downed in every fight. |
| Frailty | −30 | `|p1|` above the Gold buff HP, `≤ 40` | A flat HP price | At or below the buff it cancels it. |
| Sluggish | −4 | `2..6`, hero speed stays at least 8 | The order price | Large: the hero never acts. |
| `CurseCapHero`, `CurseCapParty` | 1, 2 | 1 to 1, 1 to 3 | Compounding | High: a spiral. |
| Gold kick (hp/dmg) | +300 / +100 | hp `200..400`, dmg equal to Purple's | The room's difficulty | A larger damage kick breaks the burst bound. |
| Ladder lower bound Gold over Purple | 105 percent | `101..115` | Gold above Purple | Low: a cursed room that is easier than Purple. |

Interactions: the Gold buff, the Gold item row (50/250/450/250) and the curse size are one tuning decision (the curse should cost about as much as the extra reward), and the cap of two curses multiplies with the weights; the first kind to cut if the schedule slips is Wither (it needs contract changes in Systems 3, 4, 12 and 13), then Frailty (a flat number), keeping Pinned and Sluggish; then the Gold item row.

## Visual/Audio Requirements

System 19 produces no visuals or sound; it fixes what a curse must look like. The art bible gives the curse as the one subtractive element.

- **Door card:** one Plum hanging tag, 10 px tall, bottom-right, with an 8×8 Bone glyph and a one or two digit numeral, outlined in Plum-E sharing the card edge; a second tag would sit to its left (not used in the Slice); a 3 px Plum strip on the card's right side inside the lit frame, so the tier frame keeps its read. The card has no spare height: the pixel budget table that Systems 16 and 17 already need must include the tag (art bible 7.10).
- **Item plate:** the pinned item's footprint does not grow; an 8×8 glyph chip inside the plate's bottom-right and a 2 px Plum bottom and right edge; the numeral goes in the popup.
- **Glyphs (provisional, at most six kinds so each stays distinct in the 1:1 greyscale test):** notched square (Pinned), shard (Wither), cracked heart (Frailty, and never Blood red), snapped chain (Sluggish). The notched square is not the 5×5 cross reserved for invalid.
- **Active curse:** the same Plum tag on the afflicted hero in the stat row (sorted last, never collapsed into "+n"), on the pinned item's plate, and in the combat HUD; it differs from a buff or debuff chip by form (a tag silhouette with a string notch, Plum fill, no chevron). A cursed cell, if used later, gets a Plum cell bed and the corner glyph.
- **Moments:** the curse takes effect with a 2-frame darkening of the tag, no lit flash; if lifted (Alpha) the tag drops 2 px and fades to ink over 200 ms.
- **Audio (at most four cues):** the tag dropping in the card reveal (a dull wooden tick, cursed cards only), the cursed-door confirm (the confirm plus a low muffled weight), the curse taking effect (a short dry downward thud), and the curse lifted (one small clear tone, Alpha). None may out-loud a detonation; each needs a reduced-flash variant.
- **Risks:** Plum against the Purple tier (lightness and the tag form separate them; Plum-E never touches a tier frame); Plum against Wine (the Red room breaks the shadow; Red is deferred); the dark pulse is a luminance change (under 10 percent, eased over at least six frames, with a calm variant).
- **Art bible gaps:** the card budget, the tag on a plate in a grid, an active-curse indicator on a hero, Plum on Wine, and Plum and Plum-E in the colour-blind tables of 4.11.

*`art-director` consulted (2026-11-13). Asset specs belong to `/asset-spec`.*

## Game Feel

A cursed door is a deliberate, weighty choice with no timer; the mark must be felt without punishing.

- **Read in one look.** The player sees the gift and the mark on the same card, and the popup states the curse in one plain line.
- **The scar is visible afterwards.** The pinned anchor sits in the grid, the afflicted hero carries the tag, a Wither tick is a small dark number on its action, and a Sluggish hero visibly acts later in the order strip; the player always sees why.
- **No surprises.** Nothing about the curse is rolled after the choice; the numbers on the card are the numbers they get (as Systems 16 and 17).
- **No input latency from this system.** A claim is tens of integer operations; it owns no animation beyond the tag's two-frame darkening.

## UI Requirements

Layout and interaction belong to Systems 27, 28 and the combat HUD and their UX specs (`/ux-design`); this system fixes what must be available.

| Information | Where | Rule |
|---|---|---|
| The curse tag with glyph and numeral | Gold door card | Stated before the choice. |
| The curse in one plain line | Door popup | For example "pins a 2-cell anchor, speed −4", "Wither 3 per action", "max HP −30", "speed −4". |
| Which heroes can receive the curse, and why not | Claim step | A hero with an active curse, or without free cells for a Pinned curse, is shown disabled with the reason; the player may free cells first. |
| The suggested hero | Claim step | The same as System 17's, excluding heroes that cannot receive the curse. |
| Active curses | On the hero, the pinned plate and the combat HUD | A Plum tag; never hidden or collapsed. |

Accessibility: the curse is never carried by colour alone (tag form, glyph and text); the curse line is read before the danger numeral by screen readers; no flash.

## Cross-References

| This GDD says | Defined in | Notes |
|---|---|---|
| The tier array, the masks, the claim step, the caps | `design/gdd/door-generation-and-tiers.md` Rules 3 to 9 | Gains a fifth tier and the cap mask. |
| Gold templates, kicks, the ladder, the burst bound | `design/gdd/encounter-generation.md` Rules 4, 9 and Formulas 4, 5 | Gold's damage kick equals Purple's. |
| The Gold item row | `design/gdd/commitment-and-drop-model.md` Rule 9 | Fifth door row over four item tiers. |
| The curse list and the claim | `design/gdd/run-state-and-flow.md` Rules 9, 16 | A new snapshot section. |
| `Create`, `AutoPlace`, `Unlock`, the `pinned` flag | `design/gdd/grid-backpack.md` | The flag is new. |
| `maxHP`, `HeroStats`, restoration | `design/gdd/party-and-heroes.md` Rules 6 to 8, Formula 1 | Gains `C_hp` and `C_speed`. |
| Effective speed and the lint | `design/gdd/turn-order.md` | The curse changes `F`, not the base speed. |
| Status ticks and baseline rows | `design/gdd/status-effects.md`, `design/gdd/combat-resolution.md` | Open. |
| Pillars 3 and 5 and the anti-pillar | `design/gdd/game-concept.md` | Player Fantasy anchors. |
| The curse tag, Plum, Gold and Red forms | `design/art/art-bible.md` 3.4.4, 4.6, 4.7 | |

## Acceptance Criteria

Evidence type: **Logic**, automated EditMode tests in `Assets/_Project/Tests/EditMode/Curses/` (class `CursesTests`, assembly `Project05.EditModeTests`), named `[Scenario]_[Expected]`; gate level BLOCKING. UI criteria are **UI** evidence (retained screenshots in `production/qa/evidence/`). Each row is its own `[TestCase]`; each rejection has a passing control. Tests use test-local fixtures only; Vertical Slice stories are not scheduled before the MVP.

**Fixtures.** `CAT`: `PIN2` {Pinned, `p1` 2, rider Speed −4}, `PIN1` {Pinned, `p1` 1, rider MaxHp −20}, `WIT` {Wither 3}, `FRA` {Frailty 30}, `SLU` {Sluggish 4}; `CAT-ALT`: `WIT` 5, `FRA` 40, `SLU` 6 (the values are data); `CAT-BAD`: one failing record per validator row. `TBL19`: System 17's seven rows plus the Gold column (rooms 6, 7, 8 as in Rule 11, 0 elsewhere), `MinRoomForTier` 1/2/3/5/6, buffs `(4,0)/(8,0)/(12,0)/(12,1)/(16,1)`, caps 100 and 3 for doors, 2 and 1 for curses; `TBL19-ALT`: Gold from room 7 and curse caps 3 and 2. Hero snapshots `{slot, h, s, F-item sum, hp-item sum, P, doorHp, curse}`. `GRID19`: a 4×3 stub with `CreatePinned`, `AutoPlace` (orientation 0, then row-major origin) and Move, Remove, Stash returning `Rejected(Pinned)` for a pinned item; it logs the call order and holds `nextItemId` 13; occupancy rows `##..`, `..##`, `####` leave the free cells (0,2), (0,3), (1,0), (1,1), so row-major gives (0,2) while last-position and column-major give (1,0). `SCRIPT`: System 17's draw seam with a `NODRAW` mode that fails on any call; a `COUNT` twin for the allocation probe. `STUB16-G`: Gold cells with two variants `[1,1]` carrying curses 1 and 3, able to return a Gold cell with no `curseId`. `REF-GOLD`: about 15 lines written from the rule text, used as the oracle only for sweeps. Cap tests are isolated: the per-hero cap test has a party total of 1; the party cap test puts the two curses on different heroes.

**Rules**

| # | Given / When / Then |
|---|---|
| 1a | **Scope.** `TBL19` with Gold weight 100 at `r = 5` gives `ContractViolation` with 0 calls; control: `r = 6` passes. |
| 1b | **Curse on every Gold card.** A Gold card at `r = 6` has `CurseId ≠ 0`; over every ticket of `r = 5..8` non-Gold cards have `CurseId = 0`. |
| 1c | **No Red.** Row arrays have length 5; a Gold cell without a `curseId` is a validator error. |
| 2 | **Purity.** `ClaimCurse`, `Terms`, `Baseline` and `CanReceive` run twice on equal inputs give value-equal outputs, leave the inputs unchanged by value, and `NODRAW` sees 0 calls. |
| 3a | **Pinned.** `PIN2` for slot 1 with the occupancy above: the item at (0,2)–(0,3), flagged `pinned`, id 13, list entry `(1, 1, 6, 13)`; its speed rider is a curse term of −4 outside the item clamps. |
| 3b | **Wither.** `WIT` gives one baseline row for that slot only (amount 3, trigger own-turn-start, source the curse). [BLOCKED: Systems 12, 13] |
| 3c | **Frailty.** `FRA` gives `C_hp = −30`, `C_speed = 0` for that slot and 0/0 for the others. |
| 3d | **Sluggish.** `SLU` gives `C_speed = −4`, `C_hp = 0` for that slot and 0/0 for the others. |
| 4 | **Authored, not drawn.** One Gold cell with variants `[1,1]`: ticket 0 gives curse 1, ticket 1 gives curse 3; the row makes 6 calls on `SCRIPT`, never 7; the claim's curse equals the card's. |
| 5a | **Claim.** Slot 1 claims: hp 16, cells 1, an entry for slot 1 only; other heroes' `B_perm` and lists are unchanged. |
| 5b | **Never prepaid.** After `ChooseDoor`, during the fight and after a defeat the list is empty and the stats are unchanged; control: after the win and the claim there is 1 entry. [BLOCKED: System 5 for the flow] |
| 5c | **Needs space.** No legal placement gives `Rejected(NeedsSpace)` with `B_perm`, cells, list and `nextItemId` unchanged, the buff included; control: after freeing cells it is Accepted. |
| 6 | **Permanent.** A defeat or a restore leaves the list intact; only the removal API is tested, as DEFERRED to Alpha. |
| 7a | **Party cap counts pending.** One active curse and one pending (the Gold door just won): the row's Gold weight is 0, the array `[230,280,240,190,0]` at room 6 has total 940 (ticket 939 gives Purple); controls: 1 active and no pending gives 60, 0 active gives 60. |
| 7b | **Hero cap.** Slot 0 already cursed (party total 1) claims: `Rejected(HeroHasCurse)`; control: slot 1 is Accepted and the list has 2 entries. |
| 7c | **Third claim.** A third claim with 2 active gives `Rejected(PartyCap)`. |
| 7d | **Shape.** Masking keeps the array length 5 and 6 calls per row. |
| 8a | **Order.** Entries `(7, slot 0)` and `(6, slot 2)` are stored as `(6,2), (7,0)`; the sort is by `acquiredRoom` first (set directly in a snapshot fixture, since one claim per room makes the slot tie-break unreachable by play). |
| 8b | **Not twice.** Claiming `FRA` twice: the second is `Rejected(WrongState)`, 1 entry, `maxHP` 126 (a double application would give 96). |
| 8c | **Only the list, the pinned item and the buff change.** A claim leaves everything else byte-equal. [BLOCKED: System 5] |
| 9 | **Validator.** One failing fixture and one control each: (a) a Gold cell with a missing or unknown `curseId`; (b) `FRA` 16 against buff 16 fails and 17 passes (strict `>`); (c) the speed lint with `s − p1`: a hero with `s = 12` and `SLU` 4 gives 8 and passes, `SLU` 5 gives 7 and fails; (d) a start grid with 2 adjacent free cells passes a 2-cell Pinned and 1 free cell fails; (e) a missing glyph; (f) a record that is both buff and curse; (g) `CurseCapParty` above the party size; (h) three bad records report all three and no catalog is built. [BLOCKED: System 2 for the real catalog stop] |
| 10a | **Gold buff.** `BuffOf(Gold) = (16, 1)`; `TBL19-ALT` values are read from the table. |
| 10b | **Gold caps.** `doorHp` 84 gives 16, 85 gives 15, 100 gives 0 (a lowered-cap fixture); Purple, Purple, Gold, Gold gives cells 1, 1, 1, 0. |
| 10c | **Gold room.** Gold kicks are +300 hp and +100 dmg, and the damage kick equals Purple's; the Gold item row 50/250/450/250 is [BLOCKED: System 18]. |
| 11 | **Boundary tickets.** Room 6 (230/510/750/940): 229 White, 230 Green, 509 Green, 510 Blue, 749 Blue, 750 Purple, 939 Purple, 940 and 999 Gold. Room 7 (180/450/750/940): 179, 180, 449, 450, 749, 750, 939, 940. Room 8 (140/400/700/930): 139, 140, 399, 400, 699, 700, 929, 930. |
| 11b | **Mask (b) includes Gold.** At `r = 7`, cards (Gold, Gold), (Purple, Gold) or (Blue, Blue) give card 3 `[180,270,0,0,0]`. |
| 11c | **Mask (a).** At `r = 7`, (White, White) gives `[0,270,300,190,60]` (820); (Green, Green) gives `[180,0,300,190,60]` (730). |
| 12 | **The card.** The card exposes the curse kind, numeral and line key before the choice, and nothing is rolled afterwards; one Gold card per kind, plus the popup. [BLOCKED: Systems 27, 28] |
| 13 | **Active curses on the world.** The hero chip (the curse last, never in "+n"), the pinned plate and the combat HUD row show the Plum tag. [BLOCKED: Systems 27, 28] |
| 14 | **Claim step.** In BuffClaim with a Gold pending only the hero claim and grid pass-through are accepted; there is no skip and no reroll; a `NeedsSpace` refusal keeps the same pending curse with 0 draws. |
| 14b | **Restore.** Each invalid curse list of the Edge Cases (item missing, flag missing, three entries, two on a hero, wrong order, `acquiredRoom` out of range, `active + pending > 2`) is rejected and leaves the live session byte-unchanged. [BLOCKED: System 5] |

**Formulas**

| # | Given / When / Then |
|---|---|
| 15 | **F1.** All the literal arrays above, including capped rooms 7 `[180,270,300,190,0]` (940) and 8 `[140,260,300,230,0]` (930, where ticket 930 is a `SCRIPT` violation); rooms 2 to 5 carry Gold 0. |
| 16 | **F2 (ADVISORY).** Exact integers per 10^9: 169,416,000 (rooms 6 and 7) and 195,643,000 (room 8); the three-room figure is 0.445. Not a production function, so a test helper would be circular. |
| 17a | **F3, Frailty.** `h = 110`, `B_perm = 16`, item sum 30, `FRA`: 126. |
| 17b | Item sum 230 (the item clamp gives 200): `110 + 16 + 200 − 30 = 296` (inside the clamp it would be 326). |
| 17c | Item sum −40: `110 + 16 − 40 − 30 = 56` (inside the clamp it would be 76). |
| 17d | **F3, Sluggish.** `s = 29`, `F = +2`, `SLU`, `P = 0`: `v = 27` against 31 without the curse. |
| 17e | The same hero with `P = 250`: `(27 × 1250 + 500) / 1000 = 34` (subtracting after `P` would give 35). |
| 17f | `F`-item sum 55: `F' = 50 − 4 = 46`, `s + F' = 75` (a curse inside the clamp would give 79). |
| 17g | `s = 3`, `F = −2`, `SLU`: `max(0, −3) = 0` and `v` clamps to 1. |
| 18 | **F4, Wither.** A hero with 6 turns takes 6 ticks of 3 before each action, 18 in total, HP 126 → 108, the source being the curse. [BLOCKED: Systems 3, 12, 13] |
| 19 | **F5.** `m_hp` Gold/Purple: room 6 1600/1500, room 7 1660/1560, room 8 1720/1620; the elite 267/251 and `26,700 ≥ 26,355` on whole rosters. [BLOCKED: System 16 for `T`] |
| 20 | **F6.** A row with Gold and a row without it each make 6 calls; `ClaimCurse` makes 0; `doorContentVersion` is bumped; the run total of 43 is [BLOCKED: System 5]. |

**Non-functional**

| # | Criterion |
|---|---|
| 21 | **Determinism.** A sweep over `r = 6..8`, caps 0, 1, 2 and tickets `(37 × i + 11) mod total`, forward and reverse: production equals `REF-GOLD`. |
| 22 | **Engine-free.** The core assembly sets `noEngineReferences`; an IL scan finds no `UnityEngine`, mutable `static` field, `float`, `double`, `DateTime`, `Random` or `GetHashCode`. The assembly name is NOT SOURCEABLE until the architecture ADR. |
| 23 | **Allocation.** Over 1,000 warmed-up `ClaimCurse`, `Terms` and `BuildRow` calls on the `COUNT` twin the managed allocation delta is 0, with an allocating control loop proving the probe works. |
| 24 | **Operation budget (counted, not timed).** `Terms` reads at most 2 list entries; `ClaimCurse` makes at most 1 `CreatePinned` and 0 draws; `BuildRow` makes 6 draws and at most 12 table reads. |
| 25 | **Balance gate (BLOCKING for the Vertical Slice gate).** Over at least 1,000 seeded runs per policy, a defeat within two rooms after a curse is claimed occurs at most 1.5 times as often as the baseline defeat rate. [BLOCKED: System 35] |

**Blocked criteria by system.** System 2: the real catalog stop. System 5: the pending order, thaw-then-claim, storage by value, the restore matrix, the 43 total. System 8: the real `pinned` flag, `CreatePinned` and `AutoPlace` semantics. Systems 3, 12 and 13: Wither (3b, 18). System 15: the real `HeroStats` clamps (17). System 16: `curseId` cells and `T` (19). System 17: `BuildRow`'s Gold column must exist first. System 18: the Gold item row (10c). System 21: the cleanse (6). Systems 27 and 28: UI. System 35: the balance checks.

**Watched failures.** Each mutant must make a named criterion fail: a cap tested as `> 2` instead of `≥ 2` (7a) or on the party instead of the hero (7b); a third claim accepted (7c); the Gold weight left at 60 or the array compacted to length 4 (7a, 7d); a curse applied twice, 96 against 126 (8b), or written into `B_perm` (8c); a prepaid curse at `ChooseDoor` or room entry (5b); the buff granted though the curse is refused (5c); a curse drawn at the claim or the row (4, 2); a pinned item that can be moved, removed or stashed, last-position or column-major placement, or a flipped rider sign (3a); Frailty inside the item clamp (17b, 17c); Sluggish inside the clamp or after `P` (17e, 17f); Gold left out of mask (b) (11b); a sort by slot only (8a); a `>` flip in the validator (9b); a silent trim in the validator (9h).

> *`qa-lead` consulted (2026-11-13). Its recomputation confirmed the row sums, the cumulative totals, Formula 3 and the worked example, and found the arithmetic and wording defects now fixed in Formulas 1, 2 and 5; its ambiguities are resolved in the Detailed Design above.*

## Open Questions

| Question | Owner | Resolve by |
|---|---|---|
| **Removal.** The curse is permanent and only an event room (System 21, Alpha) will cleanse it, as the user decided. The wording of that answer ("an event room to send ...") was read as "to cleanse"; confirm. Until then the Vertical Slice has no way to lift a curse. | game-designer, System 21 | Before System 21's GDD. |
| **Wither's rail.** A permanent, non-consumable baseline status with a stable contribution ID and a tick origin, a new row in System 3's canonical order (tick, stun check, items), and the loss of a hero to the tick; this is a contract change in Systems 3, 4, 12 and 13, and System 3 is "Needs Revision". | Systems 3, 4, 12, 13 | System 3's revision. |
| **System 8:** the `pinned` flag and a `Pinned` result code, an atomic `CreatePinned`, a locked-cell query, and a grid slice version bump. | Systems 8 and 19 | Before System 8 is re-reviewed. |
| **System 15:** the curse terms in `HeroStats` (`C_hp`, `C_speed`, outside the clamps) so that the preview equals the baseline. | Systems 15 and 19 | Sync after this GDD. |
| **Cursed heroes and the burst bound.** A cursed hero (Frailty or a Pinned HP rider) can fall in one round from full HP; System 16's bound is not extended to curses. Measure how often defeats follow a curse within two rooms (target below 1.5 times the baseline); this is a blocking criterion of the Vertical Slice gate (criterion 25). | System 35, systems-designer | First simulation. |
| **Gold above the boss.** `T(Gold, 8) < T(boss)` and the ladder constant (105 percent over Purple) rely on the boss being authored above Gold; Gold's damage kick equals Purple's so the burst bound holds. | systems-designer, System 35 | Content pass. |
| **Is the curse a bet?** Targets for System 35: a build that ignores a curse loses at least 5 points more often than one that answers it; competent Gold gains 3 to 8 points of boss win rate over never taking it; no card-and-build pairing is worth more than 8 points over a Purple pick. | System 35 | First simulation. |
| **Red and the boss reveal.** Red is deferred to Tier 3; the art bible's "Red is a room" palette shift is the boss reveal and should not be diluted by a non-boss Red room. | art-director, creative-director | Tier 3 planning. |
| **Sizing.** Gold at about 14 equivalent HP of curse is borderline trivial; the curses are structural (space, order, role) for that reason, and the numbers are to be replaced by System 35's measurements. | economy-designer, System 35 | First simulation. |
| **Concept wording.** Tier 1.5 says "Gold tier (curses optional)", but Pillar 5's test needs a drawback on Gold: Gold ships only with a curse. | creative-director | Sync after this GDD. |
| **System 17 and 16 syncs:** the tier array becomes five-wide (Rules 3, 4, 13 and Formula 7: the best schedule becomes 88 HP for the party and 84 for one hero with two Golds), the claim step gains the curse, `curseId` joins the Gold templates, and the index dependencies of System 19 (17, 3, 2) become 17, 16, 8, 15, 5, 12, 13. | Systems 16, 17 and the index | Sync after this GDD. |
| **Art bible:** the door card pixel budget including the tag, the tag on a plate in a grid, an active-curse indicator on a hero, Plum on Wine, and Plum and Plum-E in the colour-blind tables. | art-director | Art bible follow-up. |
| **Reroll and gamble.** Rejected: doors hold no luck (Pillars 1 and 3). | game-designer | Closed. |

> *Specialists consulted for these questions: game-designer, systems-designer, economy-designer, art-director, creative-director, qa-lead (2026-11-13).*
