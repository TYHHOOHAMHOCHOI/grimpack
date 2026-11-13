# Commitment and Drop Model

> **Status**: Designed (CD-GDD-ALIGN CONCERNS, five changes applied; independent `/design-review` not yet run)
> **Author**: user + Claude design session, with specialist consultation
> **Last Updated**: 2026-11-13
> **Last Verified**: 2026-11-13
> **Implements Pillar**: Luck Follows Commitment (primary); Earned Rarity (cap and 1-in-20 target); Power Has a Price (supports)

## Summary

After each won room, Commitment and Drop Model offers the player three item cards and lets them take one or skip. A pure function reads the arrangement the player just fought with and decides whether they are committed to one build direction (a catalog tag such as a status or a tempo style): at least three items in the direction and a clear lead over the next. If so, two cards lean toward that direction by a stepped weight capped at twice the base, and the third card always comes from outside it, so a pivot is always on offer. The door the player took sets the item tier odds, and commitment only chooses the direction inside a tier, so luck follows the build without raising the odds of rare pieces. The system holds no state, draws on the `drops` stream, and System 5 stores the finished offer; System 17 reads the same capped bias function for its door buffs.

> **Quick reference** — Layer: `Feature` · Priority: `MVP` · Key deps: `Grid Backpack, Adjacency and Synergy, Game Data Definitions, Seeded RNG`

## Overview

System 18 decides which items a room offers as its reward and how strongly the offer leans toward the build the player has made. After each won room the player sees three item cards and takes one (or none). Two cards are drawn with a weight that leans toward the player's **committed direction**, the one build direction (a catalog tag such as a status or a tempo style) that leads in the arrangement the player has just fought with; the third card always comes from outside that direction, so a pivot is always on offer. The lean is a pure function of that arrangement: it needs at least three items in the direction and a lead of at least two items' worth of score (item and link points) over the next, it grows in steps and is capped at twice the base weight, and it never changes which item *tier* is offered, because that is set by the door the player chose (System 17's axis). Commitment therefore steers *what kind* of power arrives, while doors and skill decide *how much*. The system holds no state and makes its draws on the `drops` stream; System 5 stores the finished offer, and System 17 reads the same capped bias function for its door buffs without adding a second multiplier. Without this system, drops would be a lottery that ignores the player's plan; with it, the dungeon answers the build, and the cap keeps a perfect run rare (Pillars 1 and 3).

> *Specialists consulted for this section: economy-designer, systems-designer, game-designer, art-director, creative-director (2026-11-13). Their findings are applied below.*

## Player Fantasy

The player lines up fire items across their three backpacks, wins a room, and sees the reward cards lean toward fire: two of them are marked as matching the build, and one is deliberately something else. They never see the finished machine handed to them; they see the dungeon notice what they are building and offer pieces for it. If they change their mind, they pay: the lean cools while the old items leave the grid and the new direction has to earn its own lead. **The dungeon notices what you are building and offers pieces for it, but never hands you the finished machine; changing direction is a step you pay for, not a dead end.**

*Pillar anchors.* Pillar 1, Luck Follows Commitment: drops lean toward a visible commitment and pivoting costs something. Pillar 3, Earned Rarity: commitment may change which item arrives at a tier the door already set, and never the tier odds; its effect on the odds of a run-defining combo is real (the lean makes key items likelier) but capped and measured by System 35, and there is no pity timer and no guaranteed completion. Pillar 4, Arrangement Is the Thinking: commitment is read from the arrangement the player fought with, so layout and links are part of it, and the lean is shown before the choice.

*A deliberate choice.* The lean rewards one clear direction: a build split between two directions, or one built of items that carry two direction tags, gets no lean, because the lead is what commits. Hybrid commitment is an Open Question, and content authors must know that a dual-tagged item holds commitment off.

*The line between the pillars.* The player should be able to say: "My build decides what I am offered; my doors and my skill decide how good it is."

*Guardrails against a rigged machine* (binding rules, not advice): a hard cap on the lean; commitment read from what is placed, not from past picks, so moving items changes it at once; no guaranteed item, no pity; one card always off-direction; the lean shown as a plain label and a count the player can verify; no gacha wording or jackpot framing in text or sound.

> *`creative-director` consulted (2026-11-13); the spine of the fantasy is theirs.*

## Detailed Design

### Core Rules

1. **Pure functions, engine-free.** `Commitment(snapshot)` returns a reading, `BiasFactorPermille(reading, tags)` returns a weight factor, and `BuildOffer(snapshot, doorTier, catalog, draws)` returns an offer. No function reads a clock or holds state; all arithmetic is 32-bit integer (64-bit only in design-time tooling), and no mutable static state exists (as Systems 3, 11, 14 and 16).
2. **Direction tags.** Only tags that System 2 flags as `direction` count (an allow-list of at most 16; three or four in the MVP, authored with the items; names such as Bleed, Plague and Tempo are placeholders). Targeting tags such as `weapon` are never directions. An item may carry at most two direction tags (no duplicates). Content lint (a rule of the System 18 content validator; Systems 2 and 15 supply the starting items): no legal party of three heroes, with all of its starting items (the signature item is an ordinary placed item and counts), has three or more items in one direction, so the starting party reads as uncommitted. The catalog needs at least two directions with at least five droppable definitions each, and at least five droppable definitions in each of the White, Green and Blue item tiers.
3. **The snapshot.** The input is the arrangement of the fight just won, taken from the frozen view (System 12) and the System 10 views of the three hero grids. Only items placed in hero grids count. Items in the stash, the reward tray and innate items (which occupy no cell) count as 0. Also in the snapshot: the room index, the door tier of the room just won (White for room 1), the number of copies of each definition the player owns now (grids plus stash; the tray is empty at this point), and the catalog. The snapshot is taken **by value at victory, before Thaw**, because the frozen view is discarded at Thaw (System 10 Rule 12); `BuildOffer` runs in the same moment and System 5 stores the offer in the same transition. Only the creation of the chosen item happens after Thaw (System 8 Rule 12). Rearranging after seeing an offer changes nothing, because the reading already happened.
4. **Score per direction `g`.** `n_g` = placed non-innate item instances carrying `g`, summed over the three hero grids; `k_g` = distinct System 10 links `(source item, rule index, target item)` where `g` is in the tags of the source or of the target (System 10 already gives one link per triple, and a malformed snapshot that repeats a triple is deduplicated defensively; a link with both ends carrying `g` counts once; a link between `g` and another direction counts for both; two rules on one pair count twice). `S_g = 1000 × n_g + 500 × k_g`. Neither count is clamped here, so two heavy builds still compare honestly; only the bias table is indexed by `min(n, 6)`.
5. **Committed or not.** Let `g1` lead by `S` and `g2` be the runner-up. The player is **committed to `g1`** only when `n_g1 ≥ 3` and `S_g1 − S_g2 ≥ 2000`. A tie for the lead, a mixed build or fewer than three items means **uncommitted**: no direction, no lean.
6. **The lean.** For the committed direction the bias is `B` by `min(n_g1, 6)`: 3 items 250, 4 items 500, 5 items 750, 6 or more 1000 (permille; provisional table data). Every table entry is a multiple of 250 and at most the cap `BiasCap` (default 1000, validator ceiling 1500, a multiple of 250), so a weight is always an exact integer. `B` is 0 before `BiasFirstRoom` (default 3), so the offers after rooms 1 and 2 are unbiased. An item carrying the committed direction has factor `F = 1000 + B`; every other item has `F = 1000`. The lean only raises weights; no item is ever made less likely than its base.
7. **Pivoting costs, by construction.** The arrangement fought in room N decides offer N, so a pivot made before the next fight shows in the next offer and costs one fight of lag; moving the old items out lowers `n` and `k` at once and shrinks the margin, so the lean falls to 0 until the new direction leads by two; items moved to the stash stop counting and their links vanish. No curse, resource or extra subsystem is needed.
8. **The offer.** After each won room 1 to 8 (not the boss) the offer has **three cards** with three distinct item definitions, built once and stored by System 5; it is never redrawn or rebuilt (not after a restore, a catalog change or a rearrangement). The stored record carries the reading (the committed direction, `n`, the scores) and one label per card, so System 17 and the UI read the stored reading and not the live grid. Candidates are the items with the `droppable` flag (the only way to be non-droppable; the validator requires a base weight of at least 1 for them; starting items are not droppable) that are under their copy cap, taken in ascending item-ID ordinal (culture-insensitive). The player takes one card or skips; only the chosen item is created in the reward tray, after Thaw (System 8). Unchosen cards never enter the tray.
9. **Two-stage selection, so commitment never moves tier odds.** For each slot: (stage 1) draw the **item tier** (White, Green, Blue, Purple) from the row of the door tier of the room just won, over the tiers that still have a candidate (provisional rows below); (stage 2) draw the item among that tier's remaining candidates with weight `base × F` (slots 1 and 2) or `base` (slot 3). A weight is `base × 500 / 1000` for a second copy of a definition already owned. At most one Purple card per offer: once a Purple is picked, all Purple candidates leave the list for the rest of the offer (including the fallback and the off-direction slot). If every positive-weight tier is empty, only tiers at or below the row's highest positive tier may be used (uniformly over the tiers that have candidates); if none has a candidate the slot is omitted, so a White door never offers Purple by fallback.

   | Door tier | White | Green | Blue | Purple |
   |---|---|---|---|---|
   | White | 800 | 200 | 0 | 0 |
   | Green | 500 | 400 | 100 | 0 |
   | Blue | 250 | 400 | 300 | 50 |
   | Purple | 100 | 300 | 450 | 150 |

10. **Slots.** When committed with `B > 0`: slots 1 and 2 use the biased weights, slot 3 is the **off-direction slot** (candidates without the committed direction, weighted by Formula 4 with `F = 1000`; if none remain, the whole remaining list at `F = 1000`). When committed but `B = 0` (before `BiasFirstRoom`), nothing is biased and the restriction does not apply, so the offers after rooms 1 and 2 are fully unbiased. So "one card is off-direction" holds only while three or more candidates remain and at least one has no committed direction. When uncommitted, all three slots use base weights. Each picked definition is removed from the candidate list. Every slot makes exactly two draw calls (tier, item); a forced choice (exactly one non-zero weight) is a bound-1 draw that ignores weights, so the number of calls is fixed by the candidate list; the tier call is always a separate call from the item call, its weight array has fixed length 4 in tier order with zeros kept, and the item weights are in ascending item-ID order; a slot with no candidate left is omitted and the offer is shorter (0 cards when the list is empty). The calls of one offer are contiguous on `drops`.
11. **No pity, no guarantee.** There is no timer, no floor and no forced completion piece; the lean changes only which item at a tier; the only rare-frequency lever is the tier row (Pillar 3).
12. **Copy limits.** At most two copies of a definition owned at a time (grids plus stash, not a history of the run) and at most one copy of each Purple definition; items at the limit are not candidates, and a removed copy makes the definition a candidate again.
13. **System 17's use.** System 17 does not call the bias function: its door buff is a certain, neutral reward fixed by tier ([door-generation-and-tiers.md](door-generation-and-tiers.md)). It reads only the committed direction of the stored reading, for a default-hero suggestion, and computes per-hero counts itself. Door buffs never count toward commitment. System 17 draws its tier cards on `doors`; this system draws only on `drops`. `BiasFactorPermille` remains available for Systems 19 and 20.
14. **Skip.** Skipping is always allowed and pays nothing in the MVP (a `B_perm` payout is an Open Question). A skipped offer is never offered again.
15. **Display.** The reading and the offer expose the committed direction, the count `n`, and a per-card label so that a UI can show them before the choice. The reading also exposes, when the player is uncommitted, a **reason** (fewer than three items in the leading direction, or no clear lead over the runner-up, with the margin in items) so that "visibly committed" can be verified and the player can see how close they are. The label is "matches the build" only when the card carries the committed direction, whatever its slot, and "other" otherwise; the numbers `B` and `F` are never shown as probabilities.

### States and Transitions

| Subject | State | Trigger | Result |
|---|---|---|---|
| Reading | Computed | A room is won (victory), before Thaw | Pure function of the frozen arrangement, taken by value; carried in the stored offer record. |
| Offer | Built | Reading computed after a win | Up to six draw calls on `drops` (fewer if candidates run out); stored by System 5 in the same transition. A pending offer blocks the next encounter until the player takes or skips (System 5). |
| Offer | Chosen | Player picks a card | The item is created in the tray after Thaw. |
| Offer | Skipped | Player skips | Nothing is created; the offer is spent. |
| Offer | Spent | Next encounter starts | The offer is discarded; the tray is destroyed by System 8. |

### Interactions with Other Systems

| System | Data in | Data out | Interface owner |
|---|---|---|---|
| 2 Game Data Definitions | Item tier, tags, `direction` flag, `droppable` flag, base weight, per-door tier rows | None | System 2 owns the fields; System 18 owns the rules. |
| 1 Seeded RNG | Weighted draws on `drops` | None | System 1 owns the stream and the call primitives. |
| 8 Grid Backpack | Placed items per hero grid; the reward tray and stash | Item created in the tray after Thaw | System 8 owns placement. |
| 10 Adjacency and Synergy | `Evaluate` views (links) | None | System 10 owns links. |
| 12 Combat Resolution | The frozen view of the last fight | Victory event triggers the offer | System 12 owns the lifecycle. |
| 15 Party and Heroes | Signature and starting items (lint) | May grant `B_perm` (deferred) | System 15 owns heroes. |
| 5 Run State and Flow | Stores the offer; supplies the snapshot and door tier | Offer to store | System 5 owns the run state. |
| 17 Door Generation and Tiers | Door tier of the room (for the item rows) | The committed direction of the stored reading (default-hero suggestion only) | System 17 owns the door buffs; it does not call the bias function. |
| 19, 20 | None | Shared `drops` call order; shop prices later | Those systems. |
| 27, 28 (reward UI) | Reading and offer | Display data | Those systems draw. |
| 35 Balance Simulation | Snapshots and policies | None | System 35 measures the 1-in-20 target. |

*Provisional:* the item tier field, the direction and droppable flags, the tier rows, `B` steps and the caps are assumptions about System 2 and balance; `drops` stream ownership is System 1's call.

> *Specialists consulted: economy-designer, systems-designer, game-designer, art-director (2026-11-13).*

## Formulas

All arithmetic is on 32-bit integers; division is integer division with non-negative dividends and is exact where noted. Constants are provisional until System 35 measures them. A weighted draw maps a ticket `t` in `[0, W)` to the first candidate whose running total exceeds `t`, as in System 1.

**Formula 1: direction score.** `S_g = 1000 × n_g + 500 × k_g`

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| `n_g` | int | `0..48` (at most the cells of three 4×4 grids) | snapshot | Placed non-innate item instances carrying direction `g`, summed over the three hero grids (not clamped). |
| `k_g` | int | `0..1080` (System 10's link buffer of 360 per grid) | System 10 views | Distinct links `(source item, rule index, target item)` where `g` is among the tags of the source or the target. |
| `S_g` | int | `0..588,000` | calculated | The score. |

*Example:* Fire has 4 items and 2 links: `S = 4000 + 1000 = 5000`. Frost has 1 item and no links: `S = 1000`.

**Formula 2: committed test.** With `g1` the highest score and `g2` the next, the player is committed to `g1` iff `n_g1 ≥ 3` and `S_g1 − S_g2 ≥ 2000` (a tie for the lead is uncommitted; with a single direction present `S_g2 = 0`). *Examples:* Fire `5000` against Frost `1000`: margin 4000, `n = 4` → committed to Fire. Fire `4000` against Frost `3000` (`n = 3`): margin 1000 → uncommitted. Fire `n = 2`, margin 2000: `n < 3` → uncommitted.

**Formula 3: bias.** `B = min(BiasCap, Btable[n_g1])` with `Btable = [0, 0, 0, 250, 500, 750, 1000]` indexed by `min(n, 6)`, and `B = 0` when the room just won is below `BiasFirstRoom` or when uncommitted. `F(item) = 1000 + B` if the item carries `g1`, else `1000`.

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| `BiasCap` | int | default 1000, validator ceiling 1500, a multiple of 250; every `Btable` entry is a multiple of 250 and at most `BiasCap` | tuning knob | Largest lean in permille (1000 = twice the weight). With the default table the cap is reached at six items; a higher cap needs a longer or steeper table. |
| `BiasFirstRoom` | int | default 3, range `1..8` | tuning knob | First room whose offer may be biased. |
| `F` | int | `1000..2500` by data (`1000..2000` with the default table and cap) | calculated | Weight factor in permille. |

*Examples:* `n = 4` after room 5: `B = 500`, `F = 1500`; the same build after room 2: `B = 0`; `n = 6`, cap 1500: `B = min(1500, 1000) = 1000`.

**Formula 4: candidate weight.** `w = base × F × cf / 1000`, with `cf = 1000` for a first copy and `500` for a second copy of a definition already owned.

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| `base` | int | `1..100` (provisional) | item record | Authored base weight; 0 means not droppable. |
| `cf` | int | `500` or `1000` | calculated | Copy factor. |
| `w` | int | `500..250,000` | calculated | Candidate weight; exact because `F` is a multiple of 250 and `cf` of 500. |

Every listed candidate has `w ≥ 500` (base 1, `F = 1000`, `cf = 500`), so a non-empty list never has total weight 0, and with at most about 40 candidates the total stays below `2^24`, far under System 1's limit. *Example:* base 100 at `F = 1500`, first copy: `w = 100 × 1500 × 1000 / 1000 = 150,000`; a second copy: `75,000`.

**Formula 5: tier stage.** The tier weights of a slot are the row of the door tier (Rule 9), set to 0 for a tier with no remaining candidate, and for Purple after one Purple card has been picked in this offer (whose remaining Purple candidates also leave the list, so no fallback can re-add one). If every weight is 0, the weight is 1 for each tier at or below the row's highest positive tier that has a candidate (a fallback so that the draw is legal and a White door never offers Purple); if no such tier has a candidate the slot is omitted. A forced choice (one tier, or one candidate) is a bound-1 draw.

**Formula 6: draw count.** Each slot makes exactly two draw calls (tier, then item), a bound-1 draw when the choice is forced; an offer makes at most `3 × 2 = 6` calls, fewer only when candidates run out.

**Formula 7: lean as seen by the player (design check).** For a slot at one tier with on-direction base-weight share `s`, the chance the item is on-direction is `p = F·s / (F·s + 1000·(1 − s))` with `s` in `0..1` and `F` in permille. *Example:* `s = 0.5`, `F = 1500`: `p = 750 / (750 + 500) = 0.60` against 0.50 unbiased; at `F = 2000`: `p = 1000 / (1000 + 500) = 0.667`. This gives the size of the lean the player can notice, and the ceiling System 35 must respect.

**Worked example (a biased offer).** Fixture pool, base 100 each: Fire White `A1`, `A2`; Fire Green `A3`; Frost White `B1`; Frost Green `B2`; no direction White `N1`; Green `N2`; Blue `N3`. The player won room 5 through a White door with `n_Fire = 4` (`B = 500`, `F = 1500`) and no copies owned. Slot 1: tier row 800/200/0/0, tiers with candidates White and Green; ticket 700 in 1000 → White. Item stage White: `A1 150,000`, `A2 150,000`, `B1 100,000`, `N1 100,000`, total 500,000; ticket 250,000 → `A2`. Slot 2: `A2` removed; tier ticket 850 → Green (`800 ≤ 850 < 1000`); Green: `A3 150,000`, `B2 100,000`, `N2 100,000`, total 350,000; ticket 200,000 → `B2`. Slot 3 (off-direction: no Fire, base weight): remaining non-Fire candidates `B1`, `N1` (White), `N2` (Green), `N3` (Blue, row weight 0); tier ticket 100 → White; `B1 100,000`, `N1 100,000`; ticket 150,000 → `N1`. Offer: `[A2, B2, N1]`, six draw calls. The player sees: Fire 4 items; card 1 (`A2`, Fire) labelled "matches the build"; cards 2 (`B2`, Frost) and 3 (`N1`) labelled "other", because the lean raises the chance of a Fire card but never guarantees one.

**The 1-in-20 target (design sketch, not a formula the game computes).** Let `p` be the per-slot chance of a key item (Formula 7), `D` the number of biased slots in a run (two per offer, six offers from room 3 to room 8 with the default `BiasFirstRoom`, so 12), `m` the number of key pieces a run-defining combo needs and `σ` the share of runs whose arrangement and survival are good enough (skill). Then `P(run-defining) ≈ σ × P(Bin(D, p) ≥ m)`. System 35 tunes first the cap `BiasCap` and the share `s` of key items in the pool, because the result is very sensitive to both (for instance `s = 0.1`, `D = 12`, `m = 3` gives about 0.11 unbiased and about 0.38 at `F = 2000`, before `σ`), and with only eight offers the tuning must be measured, not argued.

> *`systems-designer` and `economy-designer` consulted (2026-11-13); the weight and tier-stage structure is theirs.*

## Edge Cases

Format: *if condition: exact outcome* (the party that detects it). "Validator" is the System 18 content validator at catalog build; "S2" is System 2's record validation.

**Direction data and the reading**

- *The catalog has no direction tag, or fewer than two directions with five droppable definitions each:* the build fails (validator). If it were bypassed at run time, every `S` is 0, so the player is uncommitted and the offer unbiased, with no error.
- *An item carries more than two direction tags or a duplicate tag:* the build fails (S2, validator). An item with two tags counts in both directions; a build made of such items leaves a margin of 0, so it is uncommitted.
- *Two or three directions tie for the lead:* uncommitted. The test is "a unique `g1` with `S_g1 − max(others) ≥ 2000`", never an iteration order.
- *One direction only:* the runner-up score is 0, so `n ≥ 3` is committed.
- *`n` above 6:* the bias stays at the table's last entry (index `min(n, 6)`); the scores stay unclamped, so two builds of nine items still compare by `S`.
- *A link has both ends in `g`, or links `g` to `h`:* counted once for `g` and, for `h`, once for `h`; two rules on one pair count twice.
- *Innate, stash and tray items:* count 0; a signature item is a placed item and counts.
- *The room index is 0 or above 8 (the boss or out of range):* `ContractViolation` and no draw; there is no offer after the boss.
- *A hero is downed during the fight:* the frozen view is unchanged, so its items still count.

**Timing and the offer**

- *The player rearranges after the offer is built, before choosing:* nothing changes; the stored offer and reading are used.
- *Pivot right after a biased offer:* that offer stays biased; the next offer reads the next fight.
- *Rooms 1 and 2:* `B = 0`, but each slot still makes its two draw calls. *Room 8:* an offer exists. *Boss:* no offer and no draw. *Defeat:* no offer.
- *The victory hand-off arrives twice (for example a playback skip):* one offer; the second call makes no draw (System 5).
- *A new encounter would start with an offer pending:* it blocks until the player takes or skips (System 5 decides the exact flow).
- *Skip:* nothing is created, no item ID is consumed, the offer is spent.
- *The chosen item is created while the grids are still frozen:* a technical stop (System 12); the offer stays pending and is never rebuilt.
- *The tray or stash has no room for the chosen item:* creation never fails for capacity (the tray has no cells); the item sits in the tray and is lost at the next Freeze unless placed, as System 8 defines.

**Candidates and tiers**

- *Every positive-weight tier is empty:* only tiers at or below the row's highest positive tier are used (uniformly); if none has a candidate the slot is omitted. A White door never offers Purple by fallback. The validator requires at least five droppable definitions in each of the White, Green and Blue tiers, so eight picks cannot exhaust a tier at the copy cap; a fixture still tests the branch.
- *A Purple card was already picked:* Purple candidates leave the list for the rest of the offer, including the off-direction slot and the fallback.
- *Fewer than three candidates:* slots with no candidate are omitted; one candidate still makes two forced draw calls. *An empty list:* no draw, and the stored offer has no cards.
- *Exactly one non-empty tier:* the tier draw is a bound-1 draw.
- *The off-direction pool is empty:* slot 3 uses the whole remaining list at `F = 1000` (never a second Purple).
- *Every candidate is at the copy cap:* an empty offer, as above.
- *Copies owned:* grids plus stash, counted in the by-value snapshot; the tray is empty at build time. A removed or destroyed copy makes the definition a candidate again. The second copy has `cf = 500`; two or more owned copies exclude a definition; any owned copy of a Purple definition excludes it.

**Draw stability and persistence**

- *Forced choices:* a bound-1 draw, ignoring weights, so words consumed do not depend on weights. Calls per offer are `0..6` and fixed by the candidate list; words are not fixed (System 1's rejection depends on the total weight), so editing the catalog or a weight can shift later `drops` consumers.
- *An item is added to, or removed from, the catalog:* offers from the first affected one change for the same seed; golden fixtures pin `(seed, catalog version)` and are regenerated deliberately. Stream records carry the RNG version and a `dropContentVersion`.
- *System 19 or 20 also draws on `drops`:* their calls come after an offer's contiguous calls and change later outcomes; the shared order is versioned (System 1).
- *Restore between building the offer and the choice:* the stored offer is used; it is never rebuilt. Checkpoints are taken only after the offer is built and stored.
- *The catalog changes between save and restore:* the stored offer is honoured; if a stored definition ID no longer exists, the restore fails (Systems 5 and 24) and nothing is substituted.
- *The tray holds an item at the boss Freeze:* it is lost (System 8); the run ends after the boss in the MVP.

> *`systems-designer` consulted (2026-11-13); its 14 contradictions are applied (raw scores with a clamped table index, sum then clamp, snapshot before Thaw, a stored reading, honest labels, the fallback rule, a satisfiable lint, owned-now copy limits, a single `droppable` flag).*

## Dependencies

| System | Direction | Nature | Notes |
|---|---|---|---|
| 1 Seeded RNG | Depends on (hard) | Weighted draws on `drops` (two per slot) | System 1 owns the stream; call order shared with Systems 19 and 20. |
| 2 Game Data Definitions | Depends on (hard) | Item tier, tags, `direction` and `droppable` flags, base weight; the door-tier rows | Fields are System 2's; the rules are this system's. |
| 8 Grid Backpack | Depends on (hard) | Placed items per hero grid; the stash and the reward tray | Tray items are created after Thaw. |
| 10 Adjacency and Synergy | Depends on (hard) | `Evaluate` views for the link count `k` | Links by triple. |
| 12 Combat Resolution | Depends on (soft) | The frozen view of the fight just won; victory triggers the offer | Hero downing does not change the frozen view. |
| 15 Party and Heroes | Depends on (soft) | Signature and starting items (content lint); future `B_perm` payout | |
| 5 Run State and Flow | Depended on by | Stores the offer; supplies the snapshot and the door tier | |
| 17 Door Generation and Tiers | Depended on by | Reads the committed direction (default-hero suggestion) and shares the door-tier axis | Does not call the bias function or add a multiplier; buffs do not count toward commitment. |
| 19 Curses and Trade-offs, 20 Currency and Shop | Depended on by | Share the `drops` call order | Their draws change later outcomes. |
| 27, 28 (reward UI) | Depended on by | Reading and offer display data | |
| 35 Balance Simulation Harness | Depended on by | Measures the 1-in-20 target | Needs a policy that makes real door and arrangement choices. |

There is no cycle: System 17 reads this system's stored reading, never the reverse. The bidirectional back-references are listed in Cross-References.

## Tuning Knobs

All values are provisional until System 35 measures them. They live in data (System 2), never in code.

| Knob | Default | Safe range | Affects | Failure at the extremes |
|---|---|---|---|---|
| Commitment threshold (items) | 3 | `2..4` | How soon the lean starts | Low: the start kit commits; high: the lean arrives too late. |
| Lead margin (score) | 2000 | `1000..3000` | How clear the lead must be | Low: mixed builds flip direction; high: pivoting is nearly impossible to read. |
| `Btable` steps | 0, 0, 0, 250, 500, 750, 1000 | each `0..1500`, non-decreasing | How fast the lean grows | Steep: the build feels pre-decided. |
| `BiasCap` | 1000 | `500..1500` (validator ceiling 1500) | The strongest lean | High: runs lock in; low: the lean is not perceivable (the lean at `F = 1250` is only about 5 points of chance at `s = 0.5`). |
| `BiasFirstRoom` | 3 | `2..5` | Rooms before the lean | Low: no run starts neutral; high: the lean arrives late. |
| Door-tier item rows (Rule 9) | White 800/200/0/0 … Purple 100/300/450/150 | each row sums to 1000 | Item power per door | Skewed up: all runs overpowered (Pillar 3). |
| Offer size | 3 | `3..4` | How many cards | Large: the player sees the whole pool. |
| Copy cap, Purple cap | 2, 1 | `1..3`, `1..2` | Duplicate power | High: a run repeats its best item. |
| Second-copy factor `cf` | 500 | `250..1000` | Duplicate appeal | 1000: no penalty for duplicates. |
| Item base weight | 1..100 | `1..100` | Frequency of an item | Skew hides items. |
| Direction tag count | 3 to 4 | `2..16` | Build variety | Many: the lean per direction is thin. |
| Link bonus per link | 500 | `0..1000` | How much arrangement counts | 0: tag count only. |

The cap and the pivot cost are one tuning decision: at the cap the lean moves one slot from 0.50 to 0.67 (about a third of an extra matching card per offer), skipping pays nothing and moving items between grid and stash is free (System 8's open question), so the only price of a pivot is the lost lean; a weaker lean also means a cheaper pivot. Interactions: raising the cap and the tier rows together compounds power (the two axes are capped separately but multiply at the extremes), so System 35 must test them together. The threshold, the margin and the cap are the first values to retune after the first simulation.

## Visual/Audio Requirements

System 18 produces no visuals or sound; it fixes what the reward screen must be able to show. The art bible has no reward screen yet, so a new 7.10 "Reward" subsection must be added from the door card and shop shelf analogues; the surface is called the **shelf**, not the tray, to avoid the art bible's use of "tray" for grid beds.

- **Plates and tag strip:** item plates sit on shelf lines at true footprint, at most three rows, with a fixed tag strip beneath each plate (0 to 2 Bone 8 px direction glyphs) so no tier shifts. A tag the player already holds in the backpacks gets a 1 px Candle underline.
- **Committed emblem:** one 12 px Bone keyword glyph on an INK-2 rectangular chip for the direction, 1 to 3 filled square ticks for the strength, placed on the shelf header; unlit except Bone and Candle, never tier hue, Gold or Red.
- **Resonance:** a thin Candle thread (1 px, 4 px pitch, 0 or 90 degrees) from the emblem to each matching plate, treated as a variant of the synergy bridge; no plate glow, no bloom, no additive blending.
- **Pivot:** the old emblem dims one INK step for about two beats while the new glyph steps in with a Candle selection ring and the ticks reset; no Plum or hanging forms.
- **Audio:** at most four soft cues: offer reveal, matching-tag resonance (only when tags match), pick or confirm, pivot (a muted descending tone). Each under about one second; the loudest sound stays reserved for detonations (Pillar 2). A non-visual cue for invalid and confirm exists. A reduced-flash variant is required.
- **Direction glyph set:** the art bible's keyword glyph family (7.8) must be checked to include the 3 to 4 direction tags before new glyphs are added; more than 10 to 12 glyphs fails the 1:1 greyscale test.

*`art-director` consulted (2026-11-13). Asset specs belong to `/asset-spec`.*

## Game Feel

The reward choice is a calm, considered beat (no timer). The feel targets are:

- **The lean is readable at a glance.** The player can tell, before choosing, which cards match their direction (the Candle underline and the emblem thread), and can tell that the third card is deliberately different.
- **Recognition, not delivery.** A matching card feels like the dungeon answering the build, never like an item handed over; cards of the committed direction are likelier, not certain, so a biased offer can still show an off card.
- **Pivots are felt as cooling and re-lighting.** The emblem dimming and the ticks resetting give the cost a visible shape.
- **No input latency from this system.** It runs once after a win and its cost is tens of integer operations; it owns no animation.

## UI Requirements

Layout and interaction belong to Systems 27 and 28 and their UX specs (`/ux-design`); this system fixes what must be available.

| Information | Where | Rule |
|---|---|---|
| Committed direction glyph and strength ticks | Reward shelf header | From the reading; absent when uncommitted. |
| Direction count text ("Bleed 4 · Poison 1") | Reward shelf header | Counts `n`, only when a UI option shows text. |
| Reason when uncommitted ("no clear lead", "fewer than three") with the margin in items | Reward shelf header | From the stored reading; shows how close the player is to committing, because link points make the counts alone insufficient. |
| "Matches the build" or "other" label per card | Each card | By whether the card carries the committed direction, not by slot; never shows weights or probabilities. |
| Tag glyph strip and owned-tag underline | Each card | Direction tags of the item. |
| Skip control | Reward shelf | Always present; pays nothing in the MVP. |

Accessibility: lean and direction are never carried by colour alone (glyph, text and the label word); the label is localised text; the reading order is emblem, then cards left to right.

## Cross-References

| This GDD says | Defined in | Notes |
|---|---|---|
| The `drops` stream and weighted draws | `design/gdd/seeded-rng.md` | Two draws per slot. |
| Item, tag and tier fields | `design/gdd/game-data-definitions.md` | Needs an item tier field and the `direction` and `droppable` flags. |
| Frozen view and Thaw | `design/gdd/combat-resolution.md`, `design/gdd/grid-backpack.md` | The tray is created after Thaw and destroyed at Freeze. |
| Links by triple | `design/gdd/adjacency-and-synergy.md` Rules 3 to 8 | `k_g` counts distinct triples. |
| Signature and starting items | `design/gdd/party-and-heroes.md` | Content lint for direction tags. |
| Door tier and buff reward | `design/gdd/encounter-generation.md` Player Fantasy | System 16 owns the cost, System 17 the buff. |
| Pillars 1, 3 and 5 | `design/gdd/game-concept.md` | Player Fantasy anchors. |
| Tier cues, plates and lit budget | `design/art/art-bible.md` 3.4.4, 7.8, 7.10, 8.10 | A Reward subsection is missing. |

## Acceptance Criteria

Evidence type: **Logic**, automated EditMode tests in `Assets/_Project/Tests/EditMode/CommitmentAndDrops/` (assembly `Project05.EditModeTests`), named `[Scenario]_[Expected]`; gate level BLOCKING. UI criteria are **UI** evidence (retained screenshots in `production/qa/evidence/`). Each row is its own `[TestCase]`. Tests use test-local fixtures only; the door rows, caps and tables come from a fixture table, never from shipped balance data.

**Fixtures.** `CAT-10`, written in shuffled order so that ascending-ID ordering is tested (ID, tier, direction tags, base weight): `I01 A1` White Fire 100; `I02 A2` White Fire 100; `I03 A3` Green Fire 100; `I04 B1` White Frost 100; `I05 B2` Green Frost 100; `I06 N1` White none 100; `I07 N2` Green none 100; `I08 N3` Blue none 100; `I09 P1` Purple Fire 20; `I10 P2` Purple none 50; plus `S0`, a starting item with `droppable = false`, tagged Fire and `weapon`. `CAT-8` is `I01..I08` (the Formulas worked example). `CAT-FIRE` is four White Fire items `A1, A2, A7, A8` (base 100). `CAT-BP` is `N3, P1, P2`. Snapshots are hand-built values (S score, M margin, dir direction):

| Snapshot | Content | Expected |
|---|---|---|
| F4L2 | Fire 4 items, 2 Fire-only links; Frost 1 item | S 5000 vs 1000, M 4000, n 4, committed to Fire |
| F3 | Fire 3, Frost 1 | 3000 vs 1000, M 2000, committed to Fire |
| F3-FR1L1 | F3 plus one link touching Frost | 3000 vs 1500, M 1500, uncommitted |
| F2L2 | Fire 2 items, 2 links | S 3000, n 2, uncommitted |
| TIE33 | Fire 3, Frost 3 | 3000 each, uncommitted |
| TRI | Direction order [Fire, Tempo, Frost]; Fire 4, Frost 3, Tempo 0 | 4000 vs 3000, M 1000, uncommitted |
| REVERSE | Fire 2 items with 6 links (S 5000); Frost 3 (S 3000) | M 2000 but `n = 2`, uncommitted |
| BIG | 8 Fire items over three grids (4 + 4 in two of them), 8 Fire-touching links | n 8, k 8, S 12,000, committed; bias index `min(8, 6)` |
| OFFGRID | Fire 3 placed; 2 Fire in the stash, 1 in the tray, 1 innate | n 3 |

`SCRIPT` is the draw seam of Systems 14 and 16 (asserts the weights array or bound of each call, fails on an extra or missing call, counts calls; a weighted call resolves ticket `t` by one scan; a bound-1 call returns 0 for any word). `REF-OFFER` is a reference implementation of about 50 lines written from the rule text with integer arithmetic only and no production code, hand-checked against criterion 8a first; it is the oracle only for the sweeps (criterion 23), never for a criterion with explicit values.

**Rules**

| # | Given / When / Then |
|---|---|
| 1 | **Purity.** `F4L2`, room 5, White door, `CAT-8`, built twice (once with another snapshot built in between): the offers are value-equal; `Commitment` and `BiasFactorPermille` make 0 `SCRIPT` calls. |
| 2 | **Direction tags.** Three `weapon`-only items give `n = 0` for every direction and an uncommitted reading; control: three Fire items give committed Fire. [BLOCKED: System 2 for the allow-list limit (16 accepted, 17 rejected) and the starting-party lint rule of the validator] |
| 3 | **Snapshot.** `OFFGRID` gives `n_Fire = 3`; stash, tray and innate items contribute 0. After `BuildOffer`, changing the input snapshot leaves the offer unchanged. [BLOCKED: Systems 8, 10 and 12 for a real frozen view and the before-Thaw timing] |
| 4a | **Score, no clamp.** `BIG`: `n = 8`, `S = 12,000`, and the same 8 items as 4 + 4 over two grids also give 8 (sum, not per-grid), with bias index 6 (`B = 1000`). |
| 4b | **Links by triple.** `(Fire, R, Fire)` gives `k_Fire = 1`; `(Fire, R, Frost)` gives 1 for each; two rules on one pair give 2; a link with no direction gives 0; a repeated triple in a malformed snapshot counts once. |
| 5 | **Committed or not.** Each of `F4L2`, `F3`, `F3-FR1L1`, `F2L2`, `TIE33`, `TRI` and `REVERSE` yields the table's result; a single direction with `n = 3` is committed (runner-up 0); no direction at all is uncommitted. |
| 6 | **Lean.** `F4L2` at room 5: items carrying Fire have `F = 1500`, others 1000. For `n = 3, 4, 5, 6`: `B = 250, 500, 750, 1000`, so `F = 1250, 1500, 1750, 2000`. At room 2 every `F` is 1000; an uncommitted reading gives 1000 for all; no `F` is below 1000. |
| 7 | **Pivot** (room 5). (a) Fire 5 items: Fire, `B = 750`. (b) Three Fire items moved to the stash, `n = 2`: uncommitted, `B = 0`. (c) Fire 2 and Frost 3: margin 1000, uncommitted. (d) Fire 2 and Frost 4: margin 2000, Frost, `B = 500`. |
| 8a | **Worked example.** `CAT-8`, `F4L2`, room 5, White door produces exactly six calls: `Weighted[800,200,0,0] t=700` → White; `[150000,150000,100000,100000] t=250000` → A2; `Weighted[800,200,0,0] t=850` → Green; `[150000,100000,100000] t=200000` → B2; `Weighted[800,200,0,0] t=100` → White; `[100000,100000] t=150000` → N1; the offer is `[A2, B2, N1]`. |
| 8b | **Distinct picks.** All tickets 0 give `[A1, A2, B1]` (slot 2 starts from `[A2 150000, B1, N1]`); a mutant that does not remove picked candidates offers `A1` twice. |
| 8c | **Pool edges.** `S0` and any item at its copy cap appear in no weights array. With 2 candidates: 2 cards and 4 calls; with 0: no cards and 0 calls. |
| 9a | **Tier first, bias after.** `F4L2` and an empty snapshot give an identical tier call `[800,200,0,0]` for the same candidates (commitment never moves tier odds). |
| 9b | **Purple door.** Door row `[100,300,450,150]`, `F4L2`: the Purple stage is `[P1 30000, P2 50000]`; `t = 29,999` → P1 and `t = 30,000` → P2. |
| 9c | **One Purple per offer.** `CAT-BP`, Purple door, `F4L2`: slot 1 tier `[0,0,450,150]` with a ticket under 450 gives Blue (forced item `N3`); slot 2 tier `[0,0,0,150]` is forced Purple and picks `P1` or `P2`; slot 3 has no candidate and is omitted: 2 cards, 4 calls, one Purple. |
| 10a | **Off-direction slot.** `CAT-FIRE`, `F4L2`, White door, tickets 0 in slots 1 and 2: slots 1 and 2 have forced tier calls and item weights `150000` each, giving `A1` and `A2`; slot 3 has no off-direction candidate, so it takes the whole remaining list at `F = 1000`: `[100000,100000] t=100000` → `A8`; the offer is `[A1, A2, A8]`, 6 calls. |
| 10b | **Uncommitted or `B = 0`.** All three slots use base weights (`TIE33`; `F4L2` at room 2). |
| 11 | **Statelessness.** Building the same input 100 times gives a weights array on call 100 equal to call 1; the signature has no history parameter. |
| 12 | **Copies.** A1 owned twice is absent; P1 owned once is absent; A2 owned once weighs 75,000 at `F = 1500` (50,000 uncommitted); A3 owned 0 weighs 150,000. |
| 13 | **Bias function for System 17.** `BiasFactorPermille(F4L2 room 5, tags)`: `[Fire]` 1500, `[Frost]` 1000, `[]` 1000, `[Fire, Frost]` 1500. With `BiasCap = 500` and `n = 6`, `[Fire]` is 1500. (no MVP consumer: System 17's buff is neutral and does not call the function; it is kept for Systems 19 and 20) |
| 14 | **Skip.** Skipping creates nothing and the offer is never offered again. [BLOCKED: Systems 5 and 8] |
| 15 | **Display data.** `F4L2` room 5: direction Fire, `N = 4`; card labels follow Rule 15 (matching the committed direction, not the slot). [UI part BLOCKED: Systems 27 and 28] |

**Formulas**

| # | Given / When / Then |
|---|---|
| 16 | **F1.** `(n, k) = (4, 2)` gives 5000; `(1, 0)` gives 1000; `(6, 6)` gives 9000; `(8, 8)` gives 12,000; `(0, 0)` gives 0. |
| 17 | **F2** on `(S1, S2, n)`: `(5000, 1000, 4)` committed; `(3000, 1000, 3)` committed (margin exactly 2000); `(3000, 1000, 2)` uncommitted; `(3000, 1500, 3)` uncommitted; `(3000, 3000, 3)` uncommitted. Scores move in steps of 500, so a margin of 1999 cannot occur; the edge pair is 1500 and 2000. |
| 18 | **F3.** `n = 3..6` at room 5 gives 250, 500, 750, 1000; `n = 4` at room 2 gives 0 and at room 3 gives 500; `n = 6` with `BiasCap` 1500 gives 1000 (the table entry); with `BiasCap` 500 gives 500; with cap 0 gives 0. |
| 19 | **F4** `(base, F, cf)` → `w`: `(100, 1500, 1000) = 150,000`; `(100, 1500, 500) = 75,000`; `(100, 1000, 500) = 50,000`; `(20, 1500, 1000) = 30,000`; `(50, 1250, 500) = 31,250`; `(1, 1000, 500) = 500` (the minimum); `(100, 2000, 1000) = 200,000` (the default maximum; the intermediate `2 × 10^8` fits 32 bits). |
| 20 | **F5.** A White door with no Green candidate gives `[800,0,0,0]`, a forced (bound-1) tier call; a Green door with only White and Blue candidates gives `[500,0,100,0]` (`t = 499` → White, `t = 500` → Blue); a White door with only Blue and Purple candidates has all weights 0 and no tier at or below White, so the slot is omitted. |
| 21 | **F6.** Three or more candidates give 6 calls (a three-item single-tier catalog still gives 6, forced draws counted); 2 candidates give 4; 1 gives 2; 0 gives 0. |
| 22 | **F7** by cross-multiplication, no floats. Tier White `[A1, A2, B1, N1]`: `F = 1500` gives on-direction weight 300,000 of 500,000, so `5 × on = 3 × total` (0.6); `F = 2000` gives 400,000 of 600,000, so `3 × on = 2 × total` (0.667). Only slot 1 is checked (later slots lose items). |

**Non-functional**

| # | Criterion |
|---|---|
| 23 | **Determinism.** Two sweeps over rooms 3 to 8, four door tiers and the snapshot set, with tickets `t = (37 × callIndex + 11) mod bound`, are identical; a reversed order gives the same results; `REF-OFFER` equals production on the sweep. |
| 24 | **Engine-free.** The core assembly sets `noEngineReferences`; an IL scan finds no `UnityEngine`, mutable `static` field, `float`, `double`, `DateTime`, `Random` or `GetHashCode`. The core assembly name is NOT SOURCEABLE until the architecture ADR. |
| 25 | **Allocation.** Over 1,000 warmed-up `Commitment` and `BuildOffer` calls the managed allocation delta is 0, with an allocating control loop proving the probe works (a struct offer and caller-supplied scratch are required). |
| 26 | **Operation budget (counted by a test seam, not timed).** `BuildOffer` makes at most 6 draw calls and at most `3 × C` weight evaluations; `Commitment` reads each placed item and each link once; a 40-candidate worst case gives a tier total under `2^24`. Elapsed time is logged only. |
| 27 | **UI (retained screenshots).** The reward shelf shows the emblem and ticks, the matching and other labels, the tag strip with the owned-tag underline, and the skip control, for a committed and an uncommitted state. [BLOCKED: Systems 27 and 28] |

**Blocked criteria by system.** System 2: the flag source, the allow-list limit, the field ranges (base `1..100`, row sums) and the lint. System 5: the stored offer, skip, the pending-offer block. System 8: tray creation after Thaw and copy counts. System 10: real link views. System 12: the frozen view and the victory trigger. System 19: the shared `drops` call order. System 1: the `drops` and seam names. Systems 27 and 28: UI. System 35: a "shipped values equal GDD" test is ADVISORY.

**Watched failures.** Each mutant must make a named criterion fail: margin `>` instead of `≥` (17); `n ≥ 2` (17, 5); the runner-up taken as the next in list order (5, `TRI`); a clamped `n` or `k` (4a); a double-counted link (4b); `room ≤ BiasFirstRoom` (18); `BiasCap` applied to `F` instead of `B` (6, 18); slot 3 including Fire (8a); slot 3 weighted by `F` (10a); no whole-list fallback (10a); `cf` ignored or applied to the wrong copy (12, 19); a copy cap of 3 (12); a single-stage draw across tiers (9, 21); an empty tier not zeroed (20); a second Purple through the fallback (9c); no candidate removal (8b); catalog order instead of ID order (shuffled `CAT-10`); index instead of ticket (boundary tickets 29,999 and 30,000); a skipped forced draw (21); stash or tray counted (3); pity or state (11). Tie handling alone is a weak test, since every tie has margin 0; the real mutant is the choice of runner-up.

> *`qa-lead` consulted (2026-11-13). Its recomputation confirmed the worked example and Formulas 1 to 4 and 7; the ranges, divisibility, `D`, label, fallback and clamp findings are applied above.*

## Open Questions

| Question | Owner | Resolve by |
|---|---|---|
| **Pool pressure.** About 16 droppable items and 24 offered slots mean the player sees most of the pool, which can erode surprise and make a build look pre-decided. Measure how much of the pool a run sees and consider hiding items per run. | System 35, economy-designer | First simulation. |
| **Is the lean noticed?** System 35 reports matching cards per offer for committed versus uncommitted players and how much of the pool a run sees; a playtest asks whether players notice the lean and whether pivoting feels like a cost. The cap and the pivot cost are tuned together. | System 35, game-designer | First simulation and playtest. |
| **Hybrid commitment.** A deliberate two-direction build gets no lean (intended, to keep one clear direction); if it narrows Expression too much, consider a second, weaker lean for the runner-up. | game-designer, System 35 | After the first playtests. |
| **Price against reward, with the item rows.** System 16's door price must be tested against System 17's certain buffs ([door-generation-and-tiers.md](door-generation-and-tiers.md), which defines the three-channel metric and hands it to System 35) *and* this system's item-tier rows together, since both are rewards of the same door and the cap and rows multiply at the extremes. The rows cover the four MVP tiers; Gold and Red rows, and how a Purple item carries a price (Pillar 5), are deferred. | Systems 17, 18 and 35 | System 17's GDD and first simulation. |
| **The 1-in-20 target** is unproven; the cap and the key-item share of the pool are the first values to tune, and only a policy that makes real door and arrangement choices can measure it. | System 35 | First simulation. |
| **System 2 fields:** item tier, `direction` and `droppable` flags, base weight (`1..100`), the door-tier rows, the direction allow-list and the validator rules (at least two directions with five droppable definitions, five per White, Green and Blue tier, the starting-party lint). | Systems 2 and 18 | Next System 2 revision. |
| **Skip payout.** A small `B_perm` for skipping (granted only by Systems 17 and 18) was deferred; it adds a power source and may weaken pivoting. | game-designer, System 15 | After the first simulation. |
| **Pending offer flow.** The pending offer blocks the next encounter until the player takes or skips; System 5 owns the flow and the order of reward and door buff steps (the concept says "take the loot and buff"). | System 5 | System 5's GDD. |
| **Direction content.** The three or four directions are not authored; defensive heroes have no direction because System 3 defers Heal, Shield and Cooldown-change effects; the concept's three fixed heroes differ from System 15's pick-3-of-4. Placeholder names (Bleed, Plague, Tempo) are not content. | game-designer, Systems 3 and 15 | Content pass. |
| **Free stash re-loadout** (System 8's open question) weakens the pivot cost, because moving items between the grid and the stash is free. | System 8, game-designer | System 8 revision. |
| **Art bible:** a Reward subsection (the shelf), the direction glyph set within the keyword family and the lit budget row. | art-director | Art bible follow-up. |
| **Reroll and pity** are rejected for the MVP (they need a token faucet and sink, or break Pillar 3); revisit with System 20 in Alpha. | System 20, economy-designer | Alpha planning. |
| **Shared `drops` call order and `dropContentVersion`** with Systems 19 and 20, and the seam name. | System 1, System 24 | PRNG ADR and save design. |
| **Index dependency row** for System 18 omits Systems 12 (frozen view) and 15 (starting items); and "17 calls 18 for weighted selection" differs from the design (17 reads only the bias function and draws on `doors`). | systems-index | Sync after this GDD. |
| **Whether commitment should count buffs** (door buffs from System 17) is decided as no, to avoid counting luck twice; revisit if System 17's buffs become items. | System 17 | System 17's GDD. |

> *Specialists consulted for these questions: economy-designer, systems-designer, game-designer, qa-lead, art-director, creative-director (2026-11-13).*
