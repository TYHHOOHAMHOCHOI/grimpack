# Item Trigger and Cooldown

> **Status**: In Revision (independent /design-review NEEDS REVISION 2026-11-12; revision applied, re-review pending)
> **Creative Director Review (CD-GDD-ALIGN)**: NOT ASSESSED 2026-11-12 — not run by the authoring session (agents are spawned only when the user asks). The independent review's creative-director verdict was NEEDS REVISION, not MAJOR REVISION NEEDED.
> **Author**: user + Claude design session
> **Last Updated**: 2026-11-12
> **Last Verified**: 2026-11-12
> **Implements Pillar**: Arrangement Is the Thinking (primary); The Payoff Is the Product (cause must be readable)

## Summary

Item Trigger and Cooldown decides when each backpack item acts: on a cooldown counted in its owner hero's own actions, as a rate-limited reaction to an event, once at combat start, or never (passive). It selects the ready items in grid order, hands their effects to the effect-resolution queue as one action, and records every firing with its cause. It exists so that the rhythm and order of the items are the time axis the player arranges for, readable before the fight and recognisable during it (Pillars 4 and 2).

> **Quick reference** — Layer: `Feature` · Priority: `MVP` · Key deps: `Effect Resolution Model, Grid Backpack, Turn Order, Combat State and Step Model`

## Overview

System 9 decides when an item acts. Each item in a hero's grid has one trigger kind: a **cooldown** trigger, which fires the item on its owner's action once its cooldown has elapsed; a **passive** item, which never fires and only supplies stats or synergy; an **event reaction**, which fires when a named event happens in the fight, at most once per its own cooldown; or a **combat start** trigger, which fires once as the encounter begins. When a hero acts, System 9 selects that hero's ready items in grid order (anchor cell, row then column, as System 8 defines), hands each item's authored effects to System 3 as part of one root action, and records the firing and the new cooldown in the same transaction. A cooldown counts the owner's own actions, so a faster hero (System 11) fires its items more often. The cooldown an item has includes the cooldown modifiers System 10 supplies from its neighbours. Before combat, System 9 also supplies the firing order and the cooldown of every item, so that the arrangement screen can show the order the machine will run in and say plainly which parts of it are a forecast. All changes to combat state go through System 36 and are recorded by System 4, so a firing is as traceable as a damage number. Without this system the items in the backpack would be inert; with it, the rhythm of the items is the time axis the player arranges for, and the sequence of setup and payoff across three heroes (Pillar 4) is something the player can read before the fight and recognise during it (Pillar 2).

> *Specialists not consulted by the authoring session (not requested). The independent review of 2026-11-12 supplied specialist findings; they are applied below.*

## Player Fantasy

The player looks at a hero and reads its rhythm: the sword every action, the bomb every third, the whetstone silent but sharpening. Before the fight, a numeral on each plate shows its place in the grid's firing order, a cooldown number under each plate shows its rhythm, and a forecast, labelled as a forecast, shows when each item first fires. Then the hero acts and the items go off in the grid order the numerals show, with the bomb's count ticking down to its moment. **The machine runs by itself, and the player's work was the arrangement.** This serves the Core Fantasy (the architect of a perfect machine), Expression (the same items set to a different order and rhythm give a different build) and Discovery (finding the item that fires one action earlier than the player expected).

*Pillar anchors.* Pillar 4, Arrangement Is the Thinking: "Everything the player needs to decide (item effects, turn order, enemy intent) is visible before the fight starts." The firing order and cooldowns are shown before the fight, and the forecast says what it assumes. Pillar 2, The Payoff Is the Product: "Small triggers stay quiet, big chains are unmistakable, and the player can always see which arrangement caused them." Every firing is recorded with the item that caused it.

*References.* Backpack Battles makes items fire on cooldowns and lets the player read the rhythm; this game adds a party whose turn order is the sequence, so one hero's firing sets up another's.

> *`creative-director` not consulted by the authoring session (not requested); the independent review's verdict is recorded in the header.*

## Detailed Design

### Core Rules

1. **One trigger per item.** Each item definition (System 2) has exactly one trigger kind: `Passive` (no effects, only stats or synergy), `Cooldown` (cooldown `N`, start delay `S`), `Reaction` (an event pattern and a cooldown `N`) or `CombatStart`. Every non-passive item has an ordered, non-empty list of effects of the kinds System 3 allows, at most `MaxEffectsPerItem` (4, safe range 1 to 5). Provisional ranges: `N` in `1..20`, `S` in `0..20` with default `0`. The constraints System 9 needs are listed in the Dependencies section; System 2 implements the validation.
2. **Data model.** System 36 stores, for each hero, an action ordinal `a` (int32, initially `0`) and, for each `Cooldown` item instance, `nextReadyAt` (int32, initially `1 + S`) and, for each `Reaction` item instance, `nextReadyAt` (int32, initially `0`). `Passive` and `CombatStart` items have no stored cooldown. The values are initialised from the frozen arrangement at the encounter baseline, belong to System 36's combat slice, and are not kept after the encounter in the MVP. `a` is carried by the `ActionStarted` and `ActionSkipped` records, and `nextReadyAt` by `ItemFired`, so a replay or a snapshot reproduces both.
3. **Owner action ordinal.** `a` increases by one each time System 12 schedules that hero's action, **including an action skipped by stun or for lack of a ready item**. The pre-fight action (Rule 10) has `a = 0` and does not advance it. A hero that is not acting keeps the `a` of its last scheduled action (`0` before the first); a reaction owned by it uses that value.
4. **Ready.** A cooldown item is **ready** on its owner's action `a` when `a ≥ nextReadyAt`. A reaction item is ready when its owner's current `a ≥ nextReadyAt` at the moment a matching record commits.
5. **A hero action.** The order of calls is: (1) System 11 yields the actor. (2) System 12 asks System 13 whether the hero is stunned; if so it commits `ActionSkipped(stunned)` carrying the new `a`, and nothing else happens (Rule 9). (3) Otherwise System 9 lists the owner's ready cooldown items in reading order (Rule 8); if there are none, System 12 commits `ActionSkipped(no_ready_item)` carrying the new `a`. (4) Otherwise one System 36 transaction commits `ActionStarted(HeroAction)` carrying the new `a`, then one `ItemFired` per ready item in reading order, each with its new `nextReadyAt`. (5) System 9 hands the items' effects to System 3 as root effects of **one root action**: items in reading order, each item's effects in authored order. The transaction is atomic: if any part fails, nothing is published and the encounter ends in a technical stop (System 12).
6. **Fired means handed to the queue.** An item has fired when its effects are handed to System 3, which is the same transaction as its `ItemFired` and `nextReadyAt` (Rule 5). An item whose effects are all skipped later (no valid target) has still fired. Root effects cannot be cut: they enter the queue first (FIFO), and the budget rule keeps their number below the effect limit (Formula 6), so `ChainLimit(count)` can only discard reactions.
7. **Effective cooldown.** `N_eff = clamp(N − C, 1, 99)`, where `C` is the total `CooldownReduction` System 10 supplies for the item, taken from the view frozen at encounter start (so `N_eff` is constant for the encounter). A negative total lengthens the cooldown. The same formula applies to `Cooldown` and `Reaction` items. After a firing on owner ordinal `a`, `nextReadyAt = a + N_eff`; an item can never fire twice in one action.
8. **Order.** Ready items fire in the order of `(anchorRow, anchorCol, item instance ID)`, which is System 8's `readingIndex` order with the instance ID as a defensive final key (System 3's Canonical Order Contract). Comparisons are ordinal and the sort is over unique keys.
9. **Stun.** Stun is a status owned by System 13 (provisional: System 13's roster is open). System 12 reads it at the start of the hero's action. A stunned action is `ActionSkipped(stunned)`: nothing fires, but `a` advances, so waiting items come closer to ready and a ready item stays ready for the next action. **This is the intended design:** stun costs a hero the firings of the items that were ready on the stunned action, so it hurts fast items most and slow items least. It does not delay the waiting items.
10. **Combat start.** Before the first scheduled actor, System 12 runs one pre-fight root action for each hero that has `CombatStart` items, in party-slot order, with `a = 0`: one transaction commits `ActionStarted(CombatStart)` and one `ItemFired` per item in reading order, then the effects resolve as one root action. A hero with no `CombatStart` item has no pre-fight action. These actions do not count toward the 200-action limit, and System 12 checks the outcome after each of them like at any action boundary. A `CombatStart` item fires only there. `S` and `CombatStart` are both kept in the MVP by the user's decision (2026-11-12).
11. **Reactions.** A `Reaction` item reacts when a committed record matches its pattern and its owner is alive and the item is ready. Patterns: `ItemFired` (another item; own hero, another hero or any), `StatusApplied` or `StatusConsumed` (a status ID and a target side: ally, enemy or any), `Damage` (the victim: own hero, another hero or an enemy) or `UnitDowned` (an ally other than the owner, or an enemy). It never reacts to its own `ItemFired`. The unit of a reaction is the item's **whole effect batch**, enqueued in authored order. The reaction resolves under System 3's listener rules: the listener key is the item instance, at most one reaction per root action, depth `parent + 1`, and an ordering key whose `effectIndex` is `0` (the first effect), so one item makes one entry in the key. When the reaction is produced, System 9 commits the item's `ItemFired` and `nextReadyAt = a_owner + N_eff` in one transaction, then System 3 appends the effects. The cooldown therefore limits a reaction item to one reaction per `N_eff` of its owner's actions on top of the once-per-root-action rule.
12. **`ItemFired` record.** One `ItemFired` record is published for every firing, whether by cooldown, combat start or reaction, in the same transaction as the cooldown change. It carries the item origin (hero, item instance, definition, placement, modifiers applied including the cooldown total), the trigger kind, the owner's `a`, and for a cooldown or reaction item the new `nextReadyAt`. It is the only record that changes an item's cooldown; `CooldownChange` stays deferred in System 3. It carries no effect stamps (like `ActionStarted`), counts as no effect toward System 3's counter `k` (ordinal `0` for comparisons), and may be the parent of a reaction: a reaction triggered by an `ItemFired` has depth 1 and its `parentEffectSeq` is that `ItemFired` record's sequence. System 4 owns the schema and must accept this (Open Questions).
13. **Downed owner.** A downed hero is not scheduled, its items neither fire nor react, and their `nextReadyAt` stays as it was. Items already handed to the queue in the owner's own action still resolve (System 12, Rule 4).
14. **Forecast.** `Forecast(arrangementView, synergyView, hero, K, buffer)` is a pure query, with `K` in `1..20`. It writes into the caller's buffer one entry per firing: the item instance, the owner action `a ≤ K` and `orderInAction`. It returns `Ok` with a count, `BufferTooSmall` (the required size is at most `I × K ≤ 720`, with no partial output) or `ContractViolation` for `K` out of range. It lists only `Cooldown` items. It assumes no stun, no `ChainLimit`, the frozen or previewed synergy view and no change to the arrangement, and the screen must say so. It is invalid after an edit (recomputed), a stun, a `ChainLimit`, a downed owner or a change in the owner's turn order; during combat the real waits (Formula 7) replace it. It is available in the Editable and the Frozen state.
15. **Numerals.** The numeral on a plate is its place in the grid's reading order, which is also the order in which it fires within any one action. The party-wide firing position (Formula 5) is the rank of the item's **first** firing among all items and is shown as a forecast; later firings can interleave differently (Formula 3). `CombatStart` items are ranked first, then the cooldown items.
16. **Engine-free.** Integer arithmetic only, no RNG, no clock, no `UnityEngine`, no mutable static state (the same rules as System 3).

### States and Transitions

| Subject | State | Trigger | Result |
|---|---|---|---|
| Cooldown item | Waiting | `a ≥ nextReadyAt` at the owner's action | Ready. |
| Cooldown item | Ready | Owner's action; effects handed to the queue | Fired: `nextReadyAt = a + N_eff` in the same transaction, then Waiting (ready on the next action when `N_eff = 1`). |
| Cooldown item | Ready | Owner stunned | Stays Ready. |
| Combat-start item | Pending | Pre-fight action | Fired once, then never scheduled again. |
| Reaction item | Listening and ready | Matching record, owner alive | Fired: `nextReadyAt = a_owner + N_eff`, effects queued; Listening but not ready until the owner's `a` reaches `nextReadyAt`. |
| Any non-passive item | Active | Owner downed | Inactive; state unchanged. |

### Interactions with Other Systems

| System | Data in | Data out | Interface owner |
|---|---|---|---|
| 2 Game Data Definitions | Trigger kind, `N`, `S`, event pattern, effect list | None | System 2 owns fields and validation (provisional). |
| 3 Effect Resolution | Reaction rules, listener key, ordering contract, the effect limit | Root effects in order; reaction listeners (item batch, `effectIndex 0`) | System 3 owns the queue; System 9 owns who is ready. |
| 4 Event Log | None | `ItemFired` (cooldown change in the same transaction); `ActionStarted` origins `HeroAction` and `CombatStart`, carrying `a`; `ActionSkipped` carrying `a` | System 4 owns the schema (changes needed). |
| 8 Grid Backpack | Reading order, `readingIndex` | None | System 8. |
| 10 Adjacency and Synergy | Cooldown total `C` per item (frozen) | None | System 10. |
| 11 Turn Order | Global index of an owner action | `Forecast` per hero | System 11 (not designed). |
| 12 Combat Resolution | Hero action start, skip result, pre-fight call | Ready list, skip reason | System 12 owns the loop. |
| 13 Status Effects | Whether a hero is stunned | None | System 13 (provisional). |
| 36 Combat State | Stores `a` and `nextReadyAt` | Transactions with `ItemFired` | System 36. |
| 25 Backpack UI | None | Forecast data, numerals, cooldown numbers, remaining waits | System 25 draws. |

**Changes other GDDs need.** System 4: `ItemFired` as a record that is published with the cooldown change, may be a reaction's parent, and replaces the "no per-trigger fact" wording of the placement baseline; `ActionStarted` origins `HeroAction` and `CombatStart` (the action origin is no longer an item) and the ordinal `a` on `ActionStarted` and `ActionSkipped`. System 12: the call order of Rule 5, the pre-fight action and its outcome check. System 36: the data model of Rule 2. System 3: an item reaction is a batch with `effectIndex 0`, and an `ItemFired` may be a parent. These are recorded as provisional until those GDDs are revised.

> *Specialists (`systems-designer`, `game-designer`, `ai-programmer`) not consulted by the authoring session (not requested).*

## Formulas

All arithmetic is on small integers. Nothing here uses floating point, RNG or time. In this document `L` is System 3's per-action effect limit (System 3 calls it `N`; value 256), to keep it apart from the cooldown `N`.

**Formula 1: ready.** `ready(i, a) = (a ≥ nextReadyAt_i)`, with `nextReadyAt_i = 1 + S_i` for a cooldown item and `0` for a reaction item at the start of the encounter, and, after item `i` fires on the owner's ordinal `a`, `nextReadyAt_i = a + N_eff_i`.

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| `a` | int | `0..200` | System 36 (per hero); advanced by System 12 | The owner's action ordinal. `0` is the pre-fight action. |
| `S_i` | int | `0..20`, default `0` (provisional) | item definition (System 2) | Start delay: owner actions to wait before the first firing. |
| `nextReadyAt_i` | int | `0..299` | System 36 | The first owner ordinal on which the item is ready. |
| `N_eff_i` | int | `1..99` | Formula 2 | Effective cooldown, in owner actions. |

*Example:* an item with `S = 1` has `nextReadyAt = 2`, so it is not ready on `a = 1` and is ready on `a = 2`. A reaction item with `N_eff = 2` that reacts while its owner's `a = 3` has `nextReadyAt = 5`.

**Formula 2: effective cooldown.** `N_eff = clamp(N − C, 1, 99)`.

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| `N` | int | `1..20` (provisional) | item definition (System 2) | Authored cooldown. |
| `C` | int | `−600..600` | System 10 (`C_T`, frozen view) | Sum of the `CooldownReduction` amounts the item receives. A negative sum lengthens the cooldown. |

*Examples:* `N = 3`, `C = 1` gives `N_eff = 2`; `N = 2`, `C = 5` gives `N_eff = 1` (the floor); `N = 20`, `C = −600` gives `N_eff = 99` (the ceiling). The ceiling is a representation guard that keeps `nextReadyAt` small; it is not a gameplay lever, because a cooldown longer than a fight already switches the item off. Any `C ≥ N − 1` has no further effect, and the popup says so.

**Formula 3: firing schedule.** With no stun and no `ChainLimit`, a cooldown item fires on the owner's actions `f_k = (1 + S) + k × N_eff` for `k = 0, 1, 2, …`, and the number of firings within the first `A` owner actions is `0` if `A < 1 + S`, otherwise `⌊(A − 1 − S) / N_eff⌋ + 1`. If the owner is skipped on a ready action, the item fires on the first non-skipped action `a' ≥ nextReadyAt`, and the schedule continues from `a' + N_eff`.

*Example:* three items in one grid, with anchors `IT-A (row 0, col 0)`, `IT-B (row 0, col 2)` and `IT-C (row 1, col 0)`, so the reading order is IT-A, IT-B, IT-C. `IT-A`: `N = 2`, `S = 0`. `IT-B`: `N = 3`, `S = 1`. `IT-C`: `N = 1`, `S = 0`. The firings are:

| Owner action `a` | Items firing, in order | `orderInAction` |
|---|---|---|
| 1 | IT-A, IT-C | IT-A 0, IT-C 1 |
| 2 | IT-B, IT-C | IT-B 0, IT-C 1 |
| 3 | IT-A, IT-C | IT-A 0, IT-C 1 |
| 4 | IT-C | IT-C 0 |
| 5 | IT-A, IT-B, IT-C | IT-A 0, IT-B 1, IT-C 2 |

If the owner is stunned on `a = 3`, `IT-A` stays ready and fires on `a = 4` (then `nextReadyAt = 6`), so it fires on 1, 4 and 6 instead of 1, 3 and 5. If the owner is stunned on `a = 3` and `a = 4`, `IT-A` fires on 1, 5 and 7. If a neighbour gives `IT-B` a cooldown total `C = 1`, then `N_eff = 2` and it fires on 2, 4 and 6. Note that `IT-C` fires after `IT-B` on `a = 2` although `IT-B` is later in the party-wide forecast of first firings (Formula 5): the numeral on a plate orders the items within one action, not across actions.

**Formula 4: order in an action.** `orderInAction(i, a)` is the zero-based position of `i` among the items with `ready(·, a)`, sorted by `(anchorRow, anchorCol, instance ID)`.

**Formula 5: party-wide firing position (a forecast).** For an item `i`, its position key is `(phase, g, o)`: for a `CombatStart` item, `phase = 0`, `g` = the owner's party slot and `o` = its `readingIndex`; for a cooldown item, `phase = 1`, `g = G(h, f_0)` (System 11's global action index of the owner's first firing) and `o = orderInAction(i, f_0)`. The firing position is the 1-based rank of the key among all items' keys, ascending. Keys are unique: a global action belongs to one actor, and slot and reading index are unique. Reaction and passive items have no key. An item whose first firing is `Beyond` the 200-action limit (System 11) has no key either and is shown as beyond the limit.

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| `h` | hero | party slot index | System 15 / System 36 | The owner. |
| `G(h, a)` | int | `1..` the 200-action cap | System 11 (`GlobalIndex`) | Global index of the owner's action `a`. |
| `f_0` | int | `1 + S` | Formula 3 | The item's first firing. |

*Example:* two heroes, each with `IT-A`, `IT-B`, `IT-C` and one `CombatStart` item; a stub `G` that alternates (slot 0 on `a = 1, 2` gets `G = 1, 3`; slot 1 gets `G = 2, 4`). The ranks are: the `CombatStart` items 1 (slot 0) and 2 (slot 1); slot 0's `IT-A` 3 and `IT-C` 4; slot 1's `IT-A` 5 and `IT-C` 6; slot 0's `IT-B` 7 and slot 1's `IT-B` 8.

**Formula 6: effect budget.** With `I` the number of items that fire in one action (at most 36, the cells of a fully unlocked 6×6 grid) and `E` the effects per item, the root effects of one action number at most `I × E`. The catalog must satisfy `36 × MaxEffectsPerItem ≤ L − R`, with `R = 64` a reserve for reactions, so `MaxEffectsPerItem ≤ ⌊(256 − 64) / 36⌋ = 5`. With the default `MaxEffectsPerItem = 4` the roots number at most `144`. Roots enter the queue first, so they are always processed; reactions use whatever is left of `L` and may be cut by `ChainLimit(count)`. Without this rule, 36 items of 8 effects (288) would starve the last items of every action.

**Formula 7: actions until an item fires (display).** After a firing on owner ordinal `a`, the number of the owner's next actions until the item fires is `w = max(1, nextReadyAt − a)`. Right after a firing this equals `N_eff`, the number printed under the plate. The item is **ready** (displayed as ready, for example after a stun) when `nextReadyAt ≤ a`. *Example:* `N_eff = 3` and a firing at `a = 4` gives `nextReadyAt = 7`, `w = 3`; after the next owner action `w = 2`; after the one after that `w = 1`, meaning "this hero's next action".

> *`systems-designer` not consulted by the authoring session (not requested); the review's findings on the budget and the numerals are applied.*

## Edge Cases

- **If a hero has no ready cooldown item on an action**: that action is `ActionSkipped(no_ready_item)` and `a` still advances. Its combat-start and reaction items still work.
- **If a ready item has no valid target** (all its effects are skipped): it has still fired and `nextReadyAt` is set (Rule 6). The HUD shows that the firing found no target (UI Requirements), so the lost cooldown is not silent.
- **If a `ChainLimit` ends the action**: only reactions are discarded, never root effects (Rule 6, Formula 6). Every item handed to the queue has fired.
- **If a `ChainLimit` ends a pre-fight action**: likewise only reactions are discarded; the `CombatStart` items have fired.
- **If the owner is stunned on a ready action**: the item stays ready and fires on the first non-skipped action (Rule 9, Formula 3). **If the owner is stunned on consecutive actions**: `a` advances each time and the item fires once, on the first non-skipped action.
- **If a neighbour synergy would give `N_eff ≤ 0`**: the floor is 1, so an item fires at most once per action. **If `C` is negative**: the cooldown is lengthened. **If it would exceed 99**: the ceiling is 99.
- **If two reaction items match the same record**: they react in the order of System 3's key `(eventSeq, kindRank 0, side 0, slot, anchorRow, anchorCol, effectIndex 0, definitionOrdinal)`.
- **If a reaction item is not ready** (its cooldown has not elapsed): no reaction, no record. The popup shows when it is ready again.
- **If reaction items form a loop** (A reacts to B's status, B reacts to A's): each listener reacts at most once per root action; the second is a `ListenerSuppressed(once_per_root)` fact (System 3, Rule 6), and the HUD may say so (UI Requirements). `D` and `L` stay as the backstop.
- **If a reaction item fires**: it publishes its own `ItemFired`, which other reaction items may match; it never matches its own.
- **If the owner of a reaction item is not the acting hero**: the item uses its owner's current `a` (the last scheduled ordinal), and the reaction is produced and drawn for that owner (art-director, Open Questions).
- **If the owner of a reaction item is downed when the matching record commits**: no reaction is produced and no record is written, because nothing was produced.
- **If the owner is downed in the middle of its own action**: items already handed to the queue in that action still resolve (the action finishes before the outcome is checked, System 12 Rule 4); after the action nothing of that hero is scheduled and its `nextReadyAt` values stay.
- **If a combat-start effect defeats all enemies**: System 12 checks the outcome after the pre-fight actions like at any action boundary, so Victory can occur before any scheduled actor acts. Pre-fight actions never count toward the 200 limit.
- **If `a` reaches its maximum**: a hero has at most 200 scheduled actions whether or not skipped actions count toward System 12's limit (that is System 12's decision, Open Questions), so `nextReadyAt` is at most `200 + 99 = 299`, far inside the 32-bit domain.
- **If the forecast is requested**: it assumes no stun and no `ChainLimit`, lists only cooldown items and says so on screen (Rule 14).
- **If the arrangement is edited**: the forecast is recomputed from the new grid and the new System 10 view; nothing is stored between edits.
- **If the transaction of Rule 5 fails** (for example System 36 rejects `nextReadyAt`): nothing is published and the action ends in a technical stop under System 12.
- **If a catalog breaks the constraints of the Dependencies section**: System 2 rejects it. This system never repairs data.
- **If an item sits in the stash or the tray**: it has no schedule and no reaction (System 8).
- **If a hero is revived by a future mechanic**: its `a` and `nextReadyAt` continue from where they were. There is no revival in the MVP.

> *`systems-designer` not consulted by the authoring session (not requested).*

## Dependencies

| System | Direction | Hard/Soft | Contract | Back-reference status |
|---|---|---|---|---|
| 2 Game Data Definitions | Upstream | Hard | Trigger kind, `N`, `S`, reaction pattern, effect list, `MaxEffectsPerItem`. System 2 owns validation and implements the constraints below. Provisional: it has none of these fields yet. | A row is added to System 2's Open Questions. |
| 3 Effect Resolution | Upstream | Hard | The queue, the Canonical Order Contract, reaction listener rules, the root action, the effect limit `L`. | Updated, including the batch listener and the `ItemFired` parent. |
| 4 Event Log | Upstream (contract change) | Hard | `ItemFired` with the cooldown change, as a possible parent; `ActionStarted` origins `HeroAction` and `CombatStart`; `a` on `ActionStarted` and `ActionSkipped`. | Recorded in System 4's Open Questions; System 4 must be revised. |
| 8 Grid Backpack | Upstream | Hard | Reading order, `readingIndex`, the frozen arrangement. | Present (System 8, Dependencies). |
| 10 Adjacency and Synergy | Upstream | Hard | Cooldown total per item from the frozen view. | Present (System 10, Dependencies). |
| 11 Turn Order | Upstream | Hard | `G(h, a)`, the global index of an owner's action (`GlobalIndex`). System 11 is Designed ([turn-order.md](turn-order.md), pending review). | Present (System 11, Dependencies). |
| 12 Combat Resolution | Peer | Hard | The call order of Rule 5, the pre-fight action, skip results, the 200 limit. | Updated. |
| 36 Combat State and Step Model | Upstream | Hard | Stores `a` and `nextReadyAt`; the data model of Rule 2. | Updated. |
| 13 Status Effects | Peer | Hard (provisional) | The stun status; status IDs in reaction patterns. | Updated. |
| 25 Backpack UI | Downstream | Hard | Forecast, numerals, cooldown numbers, remaining waits. | Not designed. |
| 26 Hover Popup | Downstream | Soft | Trigger text for the popup. | Not designed. |
| 35 Balance Simulation Harness | Downstream | Soft | Calls `Forecast`. | Not designed. |
| 15 Party and Heroes | Peer | Soft | Party slot; stat bonuses from passive items (`hpBonus`, `speedBonus`); each hero's innate item. System 15 is Designed ([party-and-heroes.md](party-and-heroes.md), pending review). | Present (System 15, Dependencies). |
| 14 Enemy and Intent | None | n/a | Enemy actions are System 14's; this system never schedules them. | No change. |

**Constraints System 9 requires System 2 to validate:** (1) each item has exactly one trigger kind; (2) a passive or combat-start item has no `N`; a cooldown item has `N` and `S`; a reaction item has `N` and a pattern; (3) a passive item has no effects and every other item has one to `MaxEffectsPerItem`; (4) `N` and `S` are in range; (5) `36 × MaxEffectsPerItem ≤ L − R` (Formula 6); (6) a pattern names a known event kind and, where used, a known status ID; (7) no deferred effect kind appears. System 9 does not duplicate these checks.

**Circular-dependency check:** System 9 reads Systems 8, 10, 11 and 13 and writes through 36. System 11 needs no data from System 9 except for hero stats, which come from System 15.

## Tuning Knobs

| Knob | Default | Safe range | Affects | If too high | If too low |
|---|---|---|---|---|---|
| Authored cooldown `N` (cooldown items) | per item, `1..20` (provisional) | 1 to 20 | How often an item fires. | An item fires so rarely that a fight of about 10 hero actions never reaches its second firing. | Everything fires every action; rhythm and sequence stop mattering. |
| Authored cooldown `N` (reaction items) | per item, `1..20` (provisional) | 1 to 20 | How often a reaction item can react. | The reaction is rarely available. | The reaction fires on nearly every matching event, which makes reaction items dominate cooldown items. |
| Start delay `S` | `0` | 0 to 20 | Whether an item opens the fight or arrives late; a price for a strong item. | The item contributes nothing in short fights. | Strong items open every fight (no price). |
| Effective cooldown floor | 1 | fixed rule (1) | An item fires at most once per action. | Not a knob. | Not a knob. |
| Effective cooldown ceiling | 99 | 20 to 99 | A representation guard for how far a penalty can stretch a cooldown. | Not a gameplay lever. | A penalty cannot punish enough. |
| `MaxEffectsPerItem` | 4 | 1 to 5 | Item complexity and the root budget (Formula 6). | Above 5, `36 × E` exceeds `L − R` and the catalog is rejected. | Items cannot express combined effects. |
| Reserve `R` | 64 | 32 to 128 | The room left for reactions after the roots. | Fewer effects are allowed per item. | Reactions are cut sooner. |

**Not knobs.** The ordering by anchor cell, one trigger per item, `a` advancing on skipped actions, stun not delaying waiting items, and the rule that pre-fight actions do not count toward the 200 limit are design rules. The reaction depth `D` and the effect limit `L` are System 3's knobs; this system points to them. **Provisional ranges:** the ranges of `N` and `S` are to be narrowed after the combat spike measures how many hero actions a fight lasts (about 10 is assumed).

**Interactions.** The cooldown total from System 10 moves `N_eff` inside the floor and ceiling; `MaxEffectsPerItem` and the item count in a grid (System 8) together set the root budget against `L`. Hero speed (System 11) changes how many cooldown units pass per unit of time, so speed is a hidden multiplier on the value of a cooldown; the break-even is to be simulated (Open Questions). Changing any of them bumps the rules version.

> *`systems-designer` not consulted by the authoring session (not requested).*

## Visual/Audio Requirements

- **Data, not drawing.** The look of a firing is set by the art bible (5.10, 7.6). This system supplies the facts to draw: every `ItemFired` record is one plate pulse in the grid and, when the effect is visible, one avatar above the acting hero, in the order of the records. A trigger whose effect is invisible shows only the plate pulse (5.10.5).
- **Trigger kind must be readable.** The record carries the trigger kind (cooldown, combat start, reaction), so a reaction can be drawn with the art bible's chain thread from the plate that caused it, and a combat-start firing can be told apart from a turn. The glyphs and threads are the art-director's.
- **Cooldown and order numerals.** Order numerals sit on the plate's bottom edge lane at the anchor cell in the grid lens, and the cooldown number sits under each plate in the party ribbon, in the 8 px numeral face (art bible 7.6). The values come from Formulas 4, 5 and 7.
- **Audio.** This system makes no sound. It supplies the number and kind of firings per action so that Systems 29 and 32 can keep small triggers quiet and let long chains grow (Pillar 2).
- **Not covered by the art bible (Open Questions):** an avatar for a hero that is not acting (a reaction), the presentation of the pre-fight action, a reaction whose owner's grid is not the one on screen, and the photosensitivity of up to 36 plate pulses in one action.

> 📌 **Asset Spec** — Visual/Audio requirements are defined. After the art bible is approved, run `/asset-spec system:item-trigger-and-cooldown` to produce per-asset visual descriptions, dimensions and generation prompts from this section.

> *`art-director` not consulted by the authoring session (not requested).*

## Game Feel

- **The sweep is the progress.** Plates pulse in firing order and avatars follow in the same order; the cadence and the 4 s cap per action come from the art bible (5.10.6), where the cap uses the number of fired items. Presentation only consumes the records and never changes the simulation (System 12, Rule 7).
- **Waves in order.** Reactions follow the items that caused them in FIFO order (System 3), so all effects of one wave play before the consequences of that wave.
- **A turn that does nothing explains itself.** A stunned hero or a hero with no ready item produces an `ActionSkipped` record with its reason, and the HUD shows that reason, so the player never thinks the game froze.
- **Immediate planning feedback.** `Forecast` is recomputed in the same frame a drop or rotation commits, without allocation, so the numerals change as the plate lands (consistent with Systems 8 and 10).

## UI Requirements

- **Per item, on the arrangement screen:** the order numeral (Rule 15) in the grid lens and on focus; the effective cooldown under the plate in the ribbon; a start-delay marker when `S > 0`; a glyph for combat-start and for the reaction event; nothing for passives. The party-wide first-firing rank is labelled "forecast".
- **Forecast honesty:** wherever the forecast is shown, the screen says it assumes no stun and no chain limit, and that reactions are not included; it disappears or is replaced by real waits once combat starts.
- **Popup (System 26):** the trigger in words ("Fires every 3 actions, first on this hero's 2nd action"; "Reacts when Ấn is applied to an enemy, at most once per 2 actions"; "Fires at the start of combat"), the cooldown breakdown (authored value, each synergy source with its amount from System 10, and the effective value), and a note when a synergy has no further effect at the floor.
- **During combat (System 27):** the wait of an item is `w = max(1, nextReadyAt − a)` (Formula 7), phrased "this hero's Nth action", with a "ready" state when `nextReadyAt ≤ a`. A firing that found no valid target and a reaction that did not happen because of `ListenerSuppressed` are named.
- **Downed owner:** the plates of a downed hero show an inactive state.
- **Lens and strip:** the lens modes and a party-order strip for the first firings are the UX designer's to specify; a rhythm strip for the selected hero can be built from `Forecast`.
- **Text first.** Every number above has a text equivalent for keyboard and gamepad focus. The accessibility points (8 px numerals, photosensitivity of many pulses, popup focus order) go to the art-director and the UX follow-up.

> 📌 **UX Flag — Item Trigger and Cooldown**: This system has UI requirements. In Phase 4 (Pre-Production), run `/ux-design` for the arrangement screen (together with Systems 8 and 10) and the combat HUD before writing epics. Stories that reference UI should cite `design/ux/[screen].md`, not the GDD directly.

## Cross-References

| Reference | What it provides here |
|---|---|
| [Game Concept](game-concept.md), Core Mechanics 1 and MVP Definition | "Items whose turn-cooldown has elapsed fire automatically"; about 20 items with cooldown triggers. |
| [Systems Index](systems-index.md), row 9 | Dependencies 3, 8, 11, 36. |
| [Effect Resolution Model](effect-resolution-model.md), Core Rules 2, 5, 6; Canonical Order Contract; spike effect kinds; Trace Stamps | Ready-item order; the effect limit and reaction rules; `CooldownChange` deferred; the parent rule that `ItemFired` extends. |
| [Event Log](event-log.md), Combat Record Kinds and Placement Baseline | `ActionStarted`, `ActionSkipped`; the records and wording this system asks to change. |
| [Combat State](combat-state-and-step-model.md), Core Rule 1 and Formulas | Cooldown is a 32-bit field; the data model of Rule 2. |
| [Combat Resolution](combat-resolution.md), Core Rules 3 to 6 | The action loop, skip handling, the 200 limit. |
| [Grid Backpack](grid-backpack.md), Rules 6 and 17, Dependencies row 9 | Reading order, `readingIndex`, the firing position owed to the arrangement screen. |
| [Adjacency and Synergy](adjacency-and-synergy.md), Rule 3 and Formula 3 | `CooldownReduction` and the total `C`. |
| [Art Bible](../art/art-bible.md), 5.10, 7.6 | Avatars, queue timing, order numerals, cooldown numbers. |
| Prototype `Assets/_Project/Scripts/Backpack/BackpackTriggerEngine.cs` (evidence only) | Cooldown counted in the owner's own actions, passive items, all ready items in one turn. Its direct mutation, fixed effect order and `UnityEngine` coupling are not adopted. |

## Acceptance Criteria

**Fixtures.**
- `HERO-H`: a hero on a 4×4 grid (System 8 `PROFILE-C`) holding `IT-A` (anchor row 0, col 0; `N = 2`, `S = 0`), `IT-B` (row 0, col 2; `N = 3`, `S = 1`) and `IT-C` (row 1, col 0; `N = 1`, `S = 0`), each a `Cooldown` item with two effects (`Damage B = 1`, then `Damage B = 2`) on the first living enemy. `HERO-P`: a hero holding only `IT-B2` (`N = 3`, `S = 2`) and `IT-P`. `DUMMY`: an enemy with a very large HP.
- `IT-R1`: a `Reaction` item for `StatusApplied(an)` on an enemy, `N = 2`, one `Damage B = 2`. `IT-R2`: reacts to `ItemFired` of another item of its own hero, `N = 1`, one `Damage B = 1`. `IT-S`: a `CombatStart` item with one `Damage B = 1`. `IT-P`: a `Passive` item with no effects.
- `FX-LIMIT`: a test-only rules version with the effect limit `L = 8` and reserve `R = 2`, to reach a `ChainLimit` that the real limit makes unreachable.
- Logic tests live under `Assets/_Project/Tests/EditMode/ItemTriggerAndCooldown/`, in the existing `Project05.EditModeTests` assembly. The engine-free core assembly is new and its name and settings are fixed by an ADR (NOT SOURCEABLE: the asmdef settings are not covered by `docs/engine-reference/unity/`).
- "Headless runner" means the engine-free harness that drives System 12's loop over the core assembly without a scene, as the combat spike defines it.

1. **Trigger and effect-list validation (Rule 1; blocked until System 2 has the fields).** **Given** a non-passive item with no effects, a passive item with effects, or an item with two trigger kinds, **then** the catalog is rejected naming the record and field.
2. **Range validation (blocked until System 2).** **Given** `N` or `S` out of range, or a reaction item with no `N`, **then** the catalog is rejected.
3. **Budget validation (Formula 6; blocked until System 2).** **Given** `MaxEffectsPerItem = 5`, **then** the catalog is accepted; **given** `6`, **then** it is rejected because `36 × 6 = 216 > 192`.
4. **Pattern validation (blocked until System 2).** **Given** a reaction pattern with an unknown event kind or status ID, or a deferred effect kind, **then** the catalog is rejected.
5. **Data model (Rule 2).** **Given** the encounter baseline of `HERO-H`, **then** `a = 0`, `nextReadyAt` is `1, 2, 1` for `IT-A, IT-B, IT-C`, a reaction item has `0`, and passive and combat-start items have none.
6. **Ordinal (Rule 3).** **Given** `HERO-H`, **when** the hero has a fired action, a stunned action and an action with no ready item, **then** `a` is `1, 2, 3` after each, and the pre-fight action has `a = 0` and leaves `a` unchanged.
7. **Ready (Formula 1).** **Given** `IT-B`, **then** `nextReadyAt = 2`, `IT-B` is not ready on `a = 1` and is ready on `a = 2`.
8. **One root action, order (Rules 5 and 8).** **Given** `HERO-H` at `a = 5`, **then** exactly one root action starts, `IT-A`, `IT-B`, `IT-C` fire in that order, and each item's two effects resolve in authored order, all before any reaction.
9. **Fired at queue, atomic (Rules 5 and 6).** **Given** `HERO-H` at `a = 1`, **then** `ActionStarted`, the `ItemFired` records and the new `nextReadyAt` values are committed in one transaction before any root effect is processed; **given** a failure injected anywhere in the transaction, **then** nothing is published and the action ends in a technical stop.
10. **Schedule (Formula 3).** **Given** `HERO-H` for five actions without stun, **then** the items firing on `a = 1..5` are exactly `A,C | B,C | A,C | C | A,B,C` (IT- prefix omitted), and after each firing `nextReadyAt = a + N_eff`.
11. **All skipped still fired (Rule 6).** **Given** an item whose effects are all skipped for lack of a valid target, **then** it publishes `ItemFired` and `nextReadyAt` advances.
12. **Roots never cut (Rule 6, Formula 6).** **Given** `FX-LIMIT` and a reaction chain that would exceed `L = 8`, **then** `ChainLimit(count)` discards only reactions, every root effect is processed and every item keeps the cooldown it was given.
13. **Effective cooldown (Formula 2).** **Given** `(N, C) = (3, 1), (2, 5), (20, −600)`, **then** `N_eff = 2, 1, 99`; **given** a frozen System 10 view, **then** `N_eff` does not change during the encounter.
14. **Negative `C`.** **Given** `N = 3` and `C = −2`, **then** `N_eff = 5`.
15. **Stun (Rule 9).** **Given** `HERO-H` stunned on `a = 3`, **then** `ActionSkipped(stunned)` is recorded, `IT-A` fires on `a = 1, 4, 6`, and `a` advances on the skipped action; **given** the waiting item `IT-B` at the same time, **then** its `nextReadyAt` is not delayed.
16. **Consecutive stuns.** **Given** `HERO-H` stunned on `a = 3` and `a = 4`, **then** `IT-A` fires on `a = 1, 5, 7`.
17. **No ready item.** **Given** `HERO-P`, **then** `a = 1` and `a = 2` are `ActionSkipped(no_ready_item)` and `IT-B2` fires on `a = 3`.
18. **Combat start (Rule 10).** **Given** two heroes with `IT-S` and a third with none, **then** before the first scheduled actor two pre-fight root actions run in party-slot order with `a = 0`, none for the third hero, none counts toward the 200 limit, and the items do not fire again.
19. **Pre-fight outcome.** **Given** a pre-fight action that defeats every enemy, **then** Victory is recorded before any scheduled actor acts.
20. **Pre-fight chain limit.** **Given** `FX-LIMIT` and a reaction chain during a pre-fight action, **then** only reactions are discarded and the `CombatStart` items have fired.
21. **Reaction to `ItemFired` (Rule 11).** **Given** `IT-R2` and `IT-A` firing, **then** `IT-R2` reacts at depth 1 with `parentEffectSeq` equal to the sequence of `IT-A`'s `ItemFired`, and never to its own `ItemFired`.
22. **Reaction to `StatusApplied` and `StatusConsumed`.** **Given** `IT-R1`, **when** `an` is applied to an enemy, **then** one reaction resolves; **given** a pattern for `StatusConsumed(an)`, **then** it reacts to a detonation and not to an application.
23. **Reaction to `Damage` and `UnitDowned`.** **Given** patterns for damage to an enemy and for an ally downed, **then** each reacts to a matching record once per effect (not once per shield and HP step) and not to a non-matching one.
24. **Reaction cooldown (Rule 11).** **Given** `IT-R1` (`N = 2`) and two matching events within one owner window, **then** only the first reacts; **given** the owner's `a` advancing by 2, **then** it can react again.
25. **Reaction of a non-acting owner.** **Given** `IT-R1` owned by a hero whose last scheduled action is `a = 3`, **when** an enemy action triggers it, **then** `nextReadyAt = 3 + N_eff`.
26. **Reaction guards.** **Given** the owner downed when the record commits, **then** no reaction and no record; **given** two listeners looping, **then** the second is a `ListenerSuppressed(once_per_root)` fact; **given** two reaction items matching one record, **then** they react in System 3's key order with `effectIndex 0`.
27. **`ItemFired` record (Rule 12).** **Given** a cooldown, a combat-start and a reaction firing, **then** each publishes one `ItemFired` with the item origin, trigger kind and the owner's `a`; the cooldown and reaction ones carry the new `nextReadyAt`; no other record changes an item's cooldown; no `ItemFired` carries effect stamps or advances `k`.
28. **Downed owner (Rule 13).** **Given** a hero downed mid-action by an enemy-side reaction, **then** items already handed to the queue still resolve, nothing of that hero is scheduled afterwards and its `nextReadyAt` values are unchanged.
29. **Forecast against an independent oracle (Rule 14).** **Given** `HERO-H` and `K = 12`, **then** `Forecast` equals the list of `(item, a, orderInAction)` produced by a minimal reference scheduler written in the test from Formulas 1 to 4 only (no `Forecast` code); and once the headless runner exists, also the firings of a headless run without stun. This holds for arrangements reached by System 8's state walker (`WALK`).
30. **Forecast interface.** **Given** `K = 0` or `K = 21`, **then** `ContractViolation`; **given** a buffer one entry too small, **then** `BufferTooSmall` with no partial output; **given** an Editable or a Frozen arrangement, **then** `Ok`.
31. **Firing position (Formula 5).** **Given** the two-hero fixture of Formula 5 and the stub `G`, **then** the ranks are `START 1, 2; A 3, C 4 (slot 0); A 5, C 6 (slot 1); B 7 (slot 0), B 8 (slot 1)`; reaction and passive items have none. *Provisional until System 11 exists; it is not a blocking criterion before then.*
32. **Remaining wait (Formula 7).** **Given** `N_eff = 3` and a firing at `a = 4`, **then** the displayed waits after the next actions are `3, 2, 1`; **given** a ready item skipped by a stun, **then** the state is "ready".
33. **Determinism (Rule 16).** **Given** the same catalog, arrangement and decisions, **then** two runs produce identical `ItemFired` sequences, and no RNG draw or clock read occurs (a counting probe stays at zero).
34. **Engine-free (Rule 16).** **Given** the core assembly, **then** its definition sets `noEngineReferences`, and a banned-symbol analyzer or IL scan finds no `UnityEngine` reference, no mutable `static` field, no floating-point type and no `DateTime` or `Random`.
35. **Integration with System 10 (provisional).** **Given** a neighbour giving `IT-B` a cooldown total of 1, **then** `IT-B` fires on `a = 2, 4, 6`. *Blocked until System 10 is implemented.*
36. **Integration with Systems 4, 11, 12, 13 and 36 (provisional).** **Given** the spike fixture, **then** the trace shows `ActionStarted` with origin `HeroAction` or `CombatStart` and the ordinal, an `ItemFired` per firing, and System 36's historical view shows `nextReadyAt` and `a` after each. *Blocked until those systems are revised or designed.*
37. **Allocation.** **Given** a warmed-up `Forecast` with caller buffers, **when** called 1,000 times, **then** the managed allocation delta is zero, measured with a control that does allocate.
38. **UI evidence.** **Given** the arrangement screen and the combat HUD, **then** retained screenshots in `production/qa/evidence/` show the order numerals, the cooldown numbers, the forecast label, the popup trigger text with the cooldown breakdown, the "ready" state, and the skip reason of a stunned hero.
39. **Performance.** **Given** a full 6×6 grid of 36 items, **then** the time of `Forecast(hero, 20)` is measured and recorded as a number (not asserted); the provisional target is under 0.2 ms, to be confirmed in the combat spike.

> *`qa-lead` not consulted by the authoring session (not requested); the review's QA findings are applied.*

## Open Questions

| Question | Owner | Resolve by |
|---|---|---|
| System 4 must publish `ItemFired` in the same transaction as the cooldown change, allow it as a reaction's parent, carry `a` on `ActionStarted` and `ActionSkipped`, use the origins `HeroAction` and `CombatStart` (today an item), and drop the wording that unfired items need no per-trigger fact. | System 4, System 9 | Before System 4 is re-reviewed. |
| System 12 must adopt the call order of Rule 5, the pre-fight action and its outcome check, and decide whether skipped actions count toward the 200 limit (the `a` bound of 299 holds either way). | System 12, System 9 | Before System 12 is re-reviewed. |
| System 36 must store `a` and `nextReadyAt` as Rule 2 says and include them in its combat slice. | System 36 | Before System 36 is re-reviewed. |
| ~~System 11 must provide `G(h, a)`.~~ Answered by System 11 (`GlobalIndex`; a skipped turn keeps its slot, so a stun never changes it). System 13 must still confirm that it owns the stun status that System 12 reads. | Systems 11 and 13 | Before System 13 is re-reviewed. |
| System 3 must accept an item reaction as a batch with `effectIndex 0`, an `ItemFired` record as a parent, and the root-first guarantee of Formula 6. | System 3 | Before System 3 is re-reviewed. |
| System 2 needs the fields and the validation list in Dependencies. Stat bonuses of passive items: the speed bonus is a flat `speedBonus` (provisional `−10..10`) summed per hero by System 11; HP and other stat bonuses belong to Systems 2 and 15. | Systems 2, 11 and 15 | Next System 2 revision. |
| Presentation not covered by the art bible: an avatar for a hero that is not acting (a reaction), the pre-fight action, a reaction whose owner's grid is not on screen, the photosensitivity of up to 36 plate pulses in one action, and 8 px numerals. | art-director | Art bible follow-up. |
| Balance: reaction `N` values, the break-even of hero speed against cooldown value, the dead zones of `N` and `S` against fights of about 10 actions, and the wasted synergy at the floor. | systems-designer, System 35 | First balance simulation and the combat spike. |
| Lens modes, a party-order strip and a "because of…" readout on focus are not specified. | UX designer | `/ux-design` for the arrangement screen. |
| Passive and combat-start items: should a hero that can never act (no cooldown and no combat-start item) get a pre-fight warning? | UX designer | `/ux-design`. |
| System 15 gives each hero one innate item that occupies no cell. It must be accepted with the ordering key anchor row `−1`, anchor column `0` (so it fires and reacts before the grid items), with a trigger of `Cooldown`, `Reaction` or `CombatStart`, and a place in the firing rank of Formula 5. | Systems 9 and 15 | Before System 9 is re-reviewed. |
