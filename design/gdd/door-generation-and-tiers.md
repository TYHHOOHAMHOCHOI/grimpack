# Door Generation and Tiers

> **Status**: Designed (CD-GDD-ALIGN CONCERNS, four changes applied; independent `/design-review` not yet run)
> **Author**: user + Claude design session, with specialist consultation
> **Last Updated**: 2026-11-13
> **Last Verified**: 2026-11-13
> **Implements Pillar**: Power Has a Price (primary, with System 16); Luck Follows Commitment (supports); Earned Rarity (caps the buff); Arrangement Is the Thinking (supports)

## Summary

After each won room, Door Generation and Tiers builds the three door cards the player chooses from. It draws each card's tier (White, Green, Blue or Purple) from a table that depends on the room it leads to, with two deterministic limits on the third card so a row is never three identical doors from room 3 and always holds a White or Green card from room 4. Each tier carries a fixed, certain buff granted when the room behind it is won: permanent max HP for a hero of the player's choice, and at Purple one extra backpack cell. The card shows this promise next to System 16's price (danger, roster, elite mark) and a plain word for the quality of the items the room will offer, so the player reads one bet. The system holds no state and has no random reward; System 5 stores the row and the chosen card.

> **Quick reference** — Layer: `Feature` · Priority: `MVP` · Key deps: `Game Data Definitions, Seeded RNG, Encounter Generation, Commitment and Drop Model`

## Overview

System 17 builds the three door cards the player chooses from after each won room, and decides what each door promises. For every card it makes one weighted draw of the card's **tier** (White, Green, Blue or Purple) from a table that depends on the room it leads to, with two deterministic constraints on the third card so that a row is never three identical doors and always contains a safer choice. Each tier carries a fixed **buff**, a certain reward granted when the player wins the room behind the door: a permanent max HP step for one hero of the player's choice (and, at Purple, one extra open backpack cell). The buff has no roll, so the card can state its exact value. The card pairs this promise with System 16's price (the room's danger, class counts and roster) and a plain word for the better items the room will offer (System 18's door-tier rows), so the player reads one bet: is this reward worth this room for my build. The tier is the only thing System 17 draws; everything else follows from the tier, and System 16 then draws the roster variant of each card. The system holds no state: System 5 stores the finished row and, later, the chosen card. Without it, a door would be a colour and a number; with it, a door states what it asks and what it pays (Pillars 4 and 5).

> *Specialists consulted for this section: game-designer, economy-designer, systems-designer, level-designer, art-director, creative-director (2026-11-13). Their findings are applied below.*

## Player Fantasy

The player stands in a dark corridor before three lit doors. Each one shows its price (a danger, a roster with the roles that will test their build, and an elite mark if there is one) beside its promise (a certain +12 max HP, or at the top tier a bigger body and a new cell to build in). They decide that their Warrior's detonation can answer the Purple door's sweeper and multi-hitter, or they take the Green door because their build cannot yet, and the sure +8 HP is the price of caution. Either way the decision is theirs and the numbers on the cards are the numbers they get. **The door shows what it gives next to what it asks; you bet your build on it, and the payout is yours only if the arrangement answers.**

*Pillar anchors.* Pillar 5, Power Has a Price: the harder room is the price, and a higher tier changes the problem and not only the numbers (System 16); the reward side is modest and certain, so it never outshines the price. Pillar 3, Earned Rarity: the tier alone sets magnitude, and both the buff and System 18's item rows are capped; there is no random reward on the door. Pillar 4, Arrangement Is the Thinking: the choice is made on information shown before the fight, and the buff itself (a cell, HP for the hero that needs it) is something the arrangement uses. Pillar 1, Luck Follows Commitment: the door gives no luck of its own; the lean toward the build lives in System 18's item offer, and System 17 only suggests the hero that carries the most items of the build's direction.

*Who owns what.* System 16 owns the cost side of the bet; System 17 owns the reward side and the shape of the row; System 18 owns the item offer. The three are priced against each other and System 35 must test them together.

*Risks the design guards against* (what would turn doors into noise): three identical cards, a free upgrade that is better for every build, an invisible promise, a random reward that makes a colour sometimes worth it and sometimes not, always-Purple dominance, and a buff that grows faster than the room it pays for. Purple is the first cut if the schedule slips, so the ladder must also read with three tiers.

> *`creative-director` consulted (2026-11-13); the spine of the fantasy is theirs.*

## Detailed Design

### Core Rules

1. **Pure functions, engine-free.** `BuildRow(snapshot, w, draws)` returns a door row, where `draws` is the injected draw source for the `doors` stream (a clone supplied by System 5, so the function holds no RNG of its own); the variant draws of System 16 are made by `BuildRow` as the caller. `BuffOf(tier)` returns the buff, and `ClaimBuff(runSlice, heroSlot, buff)` returns the amounts actually granted `(hpGranted, cellsGranted)`; System 5 applies them (`Unlock` first, then `B_perm` through System 15 and the counters). No function reads a clock or holds state; all arithmetic is 32-bit integer, with no mutable static state (as Systems 3, 11, 14, 16 and 18).
2. **When a row is built.** A row is built when room `w` (1 to 7) is won, and leads to room `r = w + 1` (2 to 8). It is built in the same transition as System 18's offer, before Thaw, and System 5 stores it by value. There is no row after room 8 (the boss follows) and none after a defeat. A second victory hand-off for the same room makes no draw (System 5 returns the stored row). `BuildRow` with `w` outside 1 to 7 is a `ContractViolation` with no draw. The row (on `doors`) and System 18's offer (on `drops`) are two streams in one transition: both must succeed and be stored before either commits, and a failure of one discards the clone of the other.
3. **Three cards, three tier draws.** Each card makes one weighted draw of its tier on `doors`, in card order, over a fixed-length array `[White, Green, Blue, Purple]` (the length of the tier table: 4 in the MVP, 3 if Purple is cut) with zeros kept, never compacted, taken from the row of the table below for the target room `r` (permille; each row sums to 1000; provisional). A tier with `r < MinRoomForTier` (White 1, Green 2, Blue 3, Purple 5) has weight 0.

   | Target room `r` | White | Green | Blue | Purple |
   |---|---|---|---|---|
   | 2 | 600 | 400 | 0 | 0 |
   | 3 | 500 | 350 | 150 | 0 |
   | 4 | 400 | 350 | 250 | 0 |
   | 5 | 300 | 300 | 250 | 150 |
   | 6 | 250 | 300 | 250 | 200 |
   | 7 | 200 | 300 | 300 | 200 |
   | 8 | 150 | 300 | 300 | 250 |

4. **Masks on the third card.** The two constraints change only the weights of the third draw, so the number of draw calls stays three: (a) from `MinDistinctRoom` (default 3), if cards 1 and 2 have the same tier, that tier's weight is 0 for card 3; (b) from `SafeCardRoom` (default 4), if cards 1 and 2 are both Blue or Purple, the Blue and Purple weights are 0 for card 3. So a row from room 3 on is never three identical cards, and from room 4 on it always contains a White or Green card. When both masks apply (the first two cards are the same Blue or Purple tier) the result is mask (b)'s, which contains (a)'s; when a masked array has exactly one positive weight the third draw is a forced bound-1 call that ignores weights (the rule of Systems 14, 16 and 18), and otherwise it is a weighted call over the whole array. With the shipped table `SafeCardRoom = 4` and `5` give the same rows (Purple cannot appear before room 5 and mask (a) already covers two Blue cards at room 4); the value is kept at 4 so the guarantee survives if `MinRoomForTier` changes. The row into room 2 may be three White or three Green cards (user decision, System 16 Rule 3). The order of the cards is the order of the draws and is never sorted; a card's position carries no meaning.
5. **Then System 16.** After the three tier draws, System 16's three draws (one variant per card, in card order) are made, the caller being System 17. The row is checked first, drawn on a clone of `doors` and committed atomically (as in System 14's binding); the order of the cards is final before System 16's draws and is never changed afterwards. A run makes `1 + 7 × 6 = 43` draw calls on `doors`: 1 for room 1 at run start, and per row 3 tier draws and 3 variant draws (21 calls are System 17's and 22 are System 16's).
6. **The buff is a certainty.** The buff of a door is a fixed function of its tier, with no roll and no draw (provisional): White +4 max HP; Green +8; Blue +12; Purple +12 and one extra open backpack cell. It is granted when the player **wins the room behind that door** (never on entry, and never after a defeat), so the price is not prepaid. Room 1 has no door and gives no buff.
7. **Claiming the buff.** The player chooses one hero in the party to receive the whole buff. The HP part raises that hero's `B_perm` (System 15); the cell part opens one locked cell in that hero's grid with `Unlock(1)` (System 8). If the chosen hero has no locked cell left, the cell part is lost and the player is told before choosing (choose another hero to keep it). Every hero of the party is eligible (there is no permanent death), and the raise does not heal anyone because every hero is restored before each fight (System 15 Rule 6); it applies from the next encounter, and the claim after room 8 applies to the boss. The claim cannot be skipped (the buff is certain and costless, so a skip would be dominated); if every hero would receive 0 HP and 0 cells it is consumed without a prompt. The default choice offered is the hero with the most placed items carrying the committed direction (ties by slot, and slot 0 when uncommitted); System 17 computes these per-hero counts itself from the by-value victory snapshot when the row is built and stores them with the pending record, for display only and with no draw. This is the only place commitment touches the door; it is a suggestion and not a weight (a weak proxy for the hero that needs the HP, so playtests check that it does not train players into a worse choice), and buffs never count toward commitment.
8. **Order of the reward steps.** After a win: System 18's offer and this system's next row are built (by value, before Thaw), then after Thaw the player claims the door's buff (Rule 7), then takes or skips an item (System 18), then rearranges, then chooses the next door. The buff is claimed before the item so that a newly opened cell can receive it. System 5 owns the exact flow.
9. **Caps.** `B_perm` from doors is at most `DoorHpCap` (default 100) per hero and cells from doors at most `DoorCellCap` (default 3) per run; a claim that would exceed a cap grants only what fits. System 15's own `B_perm` ceiling (1000) is therefore not reachable by doors (the best legal schedule gives 80 HP in total, so with the default values `DoorHpCap` is a safety bound that never binds). The counters `doorHp[hero]` and `doorCells` are part of System 5's run slice. The validator requires every hero's unlock sequence to hold at least `DoorCellCap` locked cells at the start, so doors alone cannot exhaust a hero's cells.
10. **What the card shows.** The tier (form, rank pips, name), the danger numeral with its word and the class-count row with the elite mark (System 16), the buff promise with its exact value ("+12 max HP", "+12 max HP and +1 cell"), and a plain word for the quality of the items the room will offer, keyed by the door's tier through a localisation key that states a rank of the offer and never an item rarity (standard, better, strong, strongest for White, Green, Blue, Purple; the writer finalises the wording), so that a Purple door does not promise a rare item that System 18's row (mostly Blue, 15% Purple) does not guarantee; it is never computed from or showing a probability. The reading order is tier, promise beside danger, class counts, name. The popup lists the roster with role glyphs (System 16). No scaled stats and no intent sequence are shown (intents are bound at room entry).
11. **The Purple price is the room.** Purple carries no extra cost in the MVP beyond System 16's danger, composition and multipliers (a different problem: an elite, a sweeper and a multi-hitter at once). System 35 checks (ADVISORY) that a build ignoring the Purple roster loses at least 10 percentage points more often than one that answers it, over at least 1,000 seeded runs. A shorter item offer or a conditional buff are recorded as fallbacks (Open Questions); curses are deferred to Gold and Red. If System 35 deletes the buff (it moves the boss win rate by less than 2 percentage points), the certain half of the card disappears and Purple's reward must be rethought, so the deletion reopens this Rule and the Player Fantasy and is not only a data change.
12. **Cut tier.** If Purple is cut, its weight, its `MinRoomForTier` entry, its template cells and its buff row are removed together and each table row keeps summing to 1000 (its weight moves to Blue).
13. **Validator.** For every table row: the weights sum to 1000; a tier below its `MinRoomForTier` has weight 0; at least two tiers are positive; for `r ≥ SafeCardRoom` White and Green together have positive weight (so mask (b) leaves a legal draw); every positive `(r, tier)` has a cell in System 16; buff amounts are non-decreasing with tier; the knobs are in range (`MinDistinctRoom` and `SafeCardRoom` within `2..8`, the caps at least 1, every hero's starting locked cells at least `DoorCellCap`). A violation stops the catalog build; nothing is repaired at run time, and a positive weight on a disallowed tier at run time is a `ContractViolation` before any draw.

### States and Transitions

| Subject | State | Trigger | Result |
|---|---|---|---|
| Row | Built | A room `w` is won (1 to 7), before Thaw | Three tier draws, three variant draws on a clone of `doors`; stored by value by System 5. |
| Row | Chosen | The player picks a card | System 5 stores the chosen card by value (tier, buff, spec) and discards the row; System 16's spec goes to System 14. |
| Buff | Pending | The room behind the chosen door is won | The buff of the chosen tier (stored by value, so the delivery equals the promise) waits to be claimed with the per-hero counts. |
| Buff | Claimed | The player picks a hero | `Unlock` first, then `B_perm` and the counters (within caps); if `Unlock` returns `Frozen` nothing changes and the buff stays pending. |
| Chosen card | Spent | The buff is claimed | The record is discarded. |

### Interactions with Other Systems

| System | Data in | Data out | Interface owner |
|---|---|---|---|
| 1 Seeded RNG | Weighted draws on `doors` | None | System 1 owns the stream; Systems 16 and 17 own the shared call order. |
| 2 Game Data Definitions | The per-room tier-weight rows, the buff table, `MinDistinctRoom`, `SafeCardRoom`, caps | None | System 2 owns the fields; System 17 owns the rules. |
| 16 Encounter Generation | `GenerateSpec` (variant per card), danger, class counts | The tier of each card | System 16 is a callee of the draw; System 17 is the caller. |
| 18 Commitment and Drop Model | The stored reading (committed direction) for the default hero | The door tier of the room just won (for the item rows) | System 18 owns the offer; System 17 computes the per-hero counts itself. |
| 15 Party and Heroes | Heroes and slots | `B_perm` increase | System 15 owns `B_perm`. |
| 8 Grid Backpack | Locked cells of a hero | `Unlock(1)` | System 8 owns cells. |
| 5 Run State and Flow | The snapshot (room, party, reading) | The row, the chosen card, the pending buff | System 5 stores and orders the flow. |
| 19 Curses and Trade-offs | None | The door price (Gold and Red later) | Deferred. |
| 27, 28 (door UI) | None | The row for display | Those systems draw. |
| 35 Balance Simulation | Rows and policies | None | System 35 tests price against reward. |

*Provisional:* the weight rows, the buff amounts, the masks' room thresholds and the caps are assumptions about balance; the `Unlock` interface and the per-hero counts in System 18's stored reading are assumptions about Systems 8 and 18.

> *Specialists consulted: game-designer, economy-designer, systems-designer, level-designer, art-director (2026-11-13).*

## Formulas

All arithmetic is on 32-bit integers; division is integer division with non-negative dividends. All numeric values are provisional until System 35 measures them. A weighted draw maps a ticket `t` in `[0, W)` to the first candidate whose running total exceeds `t`, as in System 1.

**Formula 1: tier weights of a card.** `weights_c = row[r]` for cards 1 and 2; for card 3, `weights_3 = mask(row[r], tier_1, tier_2)`.

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| `r` | int | `2..8` | row table | Target room (`w + 1`). |
| `row[r]` | int[4] | each `0..1000`, sum 1000 | tier table (data) | Weights of `[White, Green, Blue, Purple]`; zeros kept. |
| `tier_1`, `tier_2` | tier | White..Purple | the first two draws | Tiers already drawn in this row. |

**Formula 2: the mask on card 3.** Start from `row[r]`. (a) If `r ≥ MinDistinctRoom` and `tier_1 = tier_2`, set that tier's weight to 0. (b) If `r ≥ SafeCardRoom` and both `tier_1` and `tier_2` are Blue or Purple, set the Blue and Purple weights to 0. The result is still a length-4 array with zeros kept; its total is at least 1 (Rule 13). *Examples:* room 4, cards `W, W`: card 3 uses `[0, 350, 250, 0]`, total 600; room 7, cards `P, B`: card 3 uses `[200, 300, 0, 0]`, total 500; room 7, cards `B, B`: both masks apply and card 3 uses `[200, 300, 0, 0]`; room 2, cards `G, G`: no mask, card 3 uses `[600, 400, 0, 0]`.

**Formula 3: the buff.** `Buff(tier) = (hp, cells)` with `hp` White 4, Green 8, Blue 12, Purple 12 and `cells` 0, 0, 0, 1.

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| `hp` | int | `0..100` | buff table (data) | Permanent max HP added to `B_perm` of the chosen hero. |
| `cells` | int | `0..1` | buff table (data) | Locked cells opened in the chosen hero's grid. |

**Formula 4: caps on a claim.** `hpGranted = min(hp, DoorHpCap − doorHp_h)` and `cellsGranted = min(cells, DoorCellCap − doorCells, lockedCells_h)`, both at least 0, where `doorHp_h` is the HP already granted to hero `h` by doors, `doorCells` the cells granted by doors this run and `lockedCells_h` the locked cells hero `h` still has. *Examples:* with the default `DoorHpCap = 100` the cap never binds (the best legal total is 80), so the cap is tested with a lowered fixture: `DoorHpCap = 40`, a hero with `doorHp = 36` claims a Blue buff: `hpGranted = min(12, 40 − 36) = 4`. A Purple claim by a hero with no locked cell: `hpGranted = 12`, `cellsGranted = 0`.

**Formula 5: draw calls.** Per row: 3 tier draws and 3 variant draws, 6 calls, always (a forced choice is a bound-1 call). Per run on `doors`: `1 + 7 × 6 = 43`, of which `7 × 3 = 21` are System 17's and `1 + 7 × 3 = 22` are System 16's. Calls are counted, not RNG words: rejection in a weighted draw may consume more than one word.

**Formula 6: how often a row would be monochrome (design check).** Before the masks, the chance that all three cards share a tier is `Σ_t (row[r][t] / 1000)^3`. *Examples:* room 2: `0.6³ + 0.4³ = 0.28`; room 3: `0.5³ + 0.35³ + 0.15³ = 0.17`; room 8: `0.15³ + 0.3³ + 0.3³ + 0.25³ ≈ 0.07`. With the mask in force from room 3 the chance is exactly 0, and only the row into room 2 can be monochrome.

**Formula 7: best legal schedule (design check on the buff).** Green into room 2 (8), Blue into rooms 3 and 4 (24), Purple into rooms 5 to 8 (48): 80 HP in total and 4 cells, of which `DoorCellCap` allows 3. All 80 HP on one hero stays under `DoorHpCap` (100). A player who takes a random card in every row gains about 56 HP in expectation (the sum over rooms of the expected buff of the row's weights); a cautious player gains less. System 35 measures this.

**Worked example (three rows, illustrative tickets; the third draw of each row is System 16's variant draw and is not shown).**

*Room 2.* Weights `[600, 400, 0, 0]` for all three cards. Tickets 640, 412, 905 give Green (600 to 999), White (0 to 599) and Green: the row is `G, W, G`; no mask applies at `r = 2`.

*Room 4.* Weights `[400, 350, 250, 0]`. Tickets 120 and 130 give White and White. Mask (a) removes White from card 3: `[0, 350, 250, 0]`, total 600; ticket 410 is at least 350 and below 600, so card 3 is Blue. The row is `W, W, B`.

*Room 7.* Weights `[200, 300, 300, 200]`. Tickets 950, 560 give Purple (800 to 999) and Blue (500 to 799). Mask (b) applies: card 3 uses `[200, 300, 0, 0]`, total 500; ticket 130 gives White. The row is `P, B, W`, with a White card as the safe choice.

*A claim.* The player wins the room behind the Purple door of the room 7 row with hero slot 1, who has two locked cells and `doorHp = 44` (at most 56 is possible before this claim): `hpGranted = min(12, 100 − 44) = 12`, `cellsGranted = min(1, 3 − 0, 2) = 1`. The default hero offered was slot 1 because the stored reading showed Fire committed and slot 1 carried the most Fire items.

> *`systems-designer`, `economy-designer` and `level-designer` consulted (2026-11-13); the tier table and the card-3 mask are theirs.*

## Edge Cases

Format: *if condition: exact outcome* (the party that detects it). "Validator" is the System 17 content validator at catalog build; "S2" is System 2's record validation; a technical stop is System 12's return to the menu with no gameplay loss or reward.

**Masks and draws**

- *Both masks apply (the first two cards are the same Blue or Purple tier):* the result is mask (b)'s, which contains (a)'s. For Blue and Purple (different tiers) only (b) applies. Room 5 with Purple twice uses `[300, 300, 0, 0]` (validator fixture).
- *A mask leaves exactly one positive weight:* the draw is a forced bound-1 call that ignores weights, and the row still makes six calls. This is a determinism rule: a weighted call and a bound-1 call consume different numbers of RNG words.
- *The masked total is 0:* `ContractViolation` before the draw; the clone is discarded, the live stream is unchanged, no row is stored and it is a technical stop (System 5). The validator makes this unreachable on validated data (at least two positive tiers per row; White and Green positive from `SafeCardRoom`).
- *The row into room 2:* may be three White or three Green cards (probability `0.6³ + 0.4³ = 0.28`); it is legal.
- *A row of the tier table does not sum to 1000, has a negative weight or is missing for a room, or a positive weight lies below `MinRoomForTier`:* the build fails (validator). At run time a positive weight on a disallowed tier is a `ContractViolation` before any draw, and the sum check is build-only (run time draws with the actual total and repairs nothing).
- *A tier is drawn that has no cell in System 16:* the build fails (validator, System 16's validator). At run time `GenerateSpec` returns `ContractViolation` after the tier draws; the whole row is discarded and nothing is committed on either stream; System 5 must not retry, because the failure is deterministic.
- *Purple is cut:* its weight, `MinRoomForTier` entry, cells and buff row go together (S2, validator); rows 2 to 4 are unchanged (zero entries do not change the total) and rows 5 to 8 change; System 18's Purple door row becomes unreachable and System 2 deletes it; `DoorCellCap` has no source and is dead.
- *Card order:* it is the order of the draws and is never sorted; sorting before the variant draws would break the pairing of card and draw.

**Row lifecycle**

- *The victory hand-off arrives twice, or the run is lost:* System 5 returns the stored row for the same `w` and draws nothing; after a defeat `BuildRow` is never called. `w` below 1 or above 7 is a `ContractViolation` with no draw (there is no row into room 9).
- *The row succeeds but System 18's offer fails (or the reverse):* both clones are discarded and neither stream advances; the checkpoint is taken only after both are stored (System 5, System 1 Rule 6).
- *Restore between building the row and choosing:* the stored row is used and never rebuilt.
- *The catalog changes between save and restore:* the stream record carries a `doorContentVersion`; a mismatch fails the restore, and so does a stored tier missing from the table (Systems 5 and 24).
- *The chosen card:* at pick time System 5 stores `{tier, buff by value, spec}` and discards the row, because the next row is already built at victory and the chosen card is still needed for the claim and for System 18's door tier. A run lost in room `k` has made `1 + 6(k − 1)` calls on `doors`.
- *Adding or removing a row of the tier table, or changing a weight, `MinDistinctRoom` or `SafeCardRoom`:* masked totals change, so word consumption and later stream positions change; this is a content-version change. Golden fixtures pin `(seed, rngVersion, doorContentVersion, encounterContentVersion)`.

**The claim**

- *The hero has no locked cell and the buff has a cell:* the cell is lost and the player is told before choosing; another hero keeps it. Grid Backpack exposes `Unlock` returning a count but no locked-cell query, so a query is a System 8 request. Because the validator requires at least `DoorCellCap` locked cells per hero at the start, doors alone never exhaust a hero.
- *No hero has a locked cell, or `doorCells` has reached `DoorCellCap`:* the buff gives HP only and the card shows "+12 max HP" (the card is built from the run state at choice time).
- *A hero at the HP cap:* unreachable (`HP_cap` 10,000 against a door total of at most 80); `B_perm` is still raised and System 15 clamps `maxHP`.
- *`DoorHpCap` reached:* `hpGranted` is what fits, possibly 0; the buff is still consumed. With the default values the cap never binds.
- *The grid is frozen when the claim starts:* `Unlock` returns `Frozen`; nothing changes (no HP, no counters) and the buff stays pending; the claim runs after Thaw (a flow error is a System 5 technical stop).
- *A claim that would grant 0 HP and 0 cells for every hero:* consumed without a prompt.
- *The buff is pending when the player quits or the run ends:* a defeat grants none; the room 8 buff is claimed before the boss; save and resume belongs to System 24 (the pending record is part of System 5's slice).
- *The party has fewer than three heroes:* the choice is among the heroes present; zero heroes is a `ContractViolation` (System 15).
- *The default hero:* ties go to the lowest slot, and an uncommitted reading gives slot 0. A slot outside the party is a `ContractViolation`.
- *Room 1:* no door, no pending buff; a row into room 2 is still built.

**Balance and what this system cannot promise**

- *A build strong enough for Purple has no reason to take a lower tier:* intended; the price is the room, and System 35 tests price against reward with policies that make real door choices.
- *The certain buff is small next to the room's ramp:* if it moves the boss win rate by less than 2 percentage points, System 35 recommends deleting it (ablation).

> *`systems-designer` consulted (2026-11-13); its cases and contradictions are applied (the bound-1 rule for a single positive weight, the chosen-card record, the per-hero counts computed here, `doorContentVersion`, the unreachable cap examples).*

## Dependencies

| System | Direction | Nature | Notes |
|---|---|---|---|
| 1 Seeded RNG | Depends on (hard) | Three weighted tier draws per row on `doors` | System 1 owns the stream; the call order with System 16 is shared (43 calls per run). |
| 2 Game Data Definitions | Depends on (hard) | The tier table, buff table, thresholds and caps | System 2 owns the fields; System 17 owns the rules. |
| 16 Encounter Generation | Depends on (hard) | The variant draw, danger and class counts of each card; `MinRoomForTier`; the cells | System 17 is the caller of System 16's draws. |
| 18 Commitment and Drop Model | Depends on (soft) | The committed direction in the stored reading for the default hero; the door tier for the item rows | Per-hero counts are computed by System 17 from the victory snapshot. |
| 15 Party and Heroes | Depends on (hard) | `B_perm` growth; hero slots | The `B_perm` ceiling in System 15 (1000) is far above the door share. |
| 8 Grid Backpack | Depends on (hard) | `Unlock(1)` and locked cells | The claim happens after Thaw. |
| 5 Run State and Flow | Depended on by | Stores the row and the chosen card; orders the reward steps | |
| 19 Curses and Trade-offs | Depended on by | Gold and Red prices later | Deferred. |
| 21 Room Types | Depended on by | Room kinds beyond combat | Alpha. |
| 27, 28 (door UI) | Depended on by | The row for display | |
| 35 Balance Simulation Harness | Depended on by | Tests price against reward | Needs policies that make real door choices. |

There is no cycle: System 17 calls System 16 and reads System 18's stored data; neither reads System 17. The bidirectional back-references are in Cross-References.

## Tuning Knobs

All values are provisional until System 35 measures them. They live in data (System 2), never in code.

| Knob | Default | Safe range | Affects | Failure at the extremes |
|---|---|---|---|---|
| Tier-weight table rows | see Rule 3 | each row sums to 1000; zeros below `MinRoomForTier` | How often each tier appears per room | Skewed up: all runs reach Purple early; skewed down: Purple is rare and the ladder is unseen. |
| `MinDistinctRoom` | 3 | `2..4` | From which room a row cannot repeat a tier | Low: the room 2 row loses its freedom; high: monochrome rows reappear. |
| `SafeCardRoom` | 4 | `3..6` | From which room a safe card is guaranteed | High: a Blue and Purple row can force a hard choice; low: no real bet. |
| Buff HP by tier | 4, 8, 12, 12 | each `0..20`, non-decreasing | Size of the certain reward | Large: reward outshines the price (Pillar 5); zero: White is a free no-reward door. |
| Purple extra cell | 1 | `0..1` | The Purple promise | 0: Purple's reward is only HP. |
| `DoorHpCap` | 100 | `50..200` | Most door HP per hero | High: doors alone make a hero tanky. |
| `DoorCellCap` | 3 | `1..4` | Most cells from doors | Large: Purple doors replace the unlock progression. |
| Default-hero rule | most committed-direction items | on or off | Only a suggestion | Off: the player always chooses without a hint. |

Interactions: the table rows, the buff amounts and System 18's item rows multiply at the extremes (all three reward channels follow the tier), so System 35 must test them together with System 16's multipliers. If Purple is cut, its weight moves to Blue and the buff row shrinks to three tiers.

## Visual/Audio Requirements

System 17 produces no visuals or sound; it fixes what each door card must be able to show. The art bible's door card (7.10, 84 × 140 px) has no zone yet for System 16's danger numeral, class-count row and elite pip, or for this system's buff promise; a pixel budget table and a row assignment are needed (it already uses about 130 px of 140).

- **Promise chip:** the buff is shown with the art bible's stat chip grammar (INK-1 bed, 1 px INK-4 frame, 12 px tall, all Bone: an 8×8 stat glyph, the up chevron, a signed number), in the benefit-rows zone, at most two chips (HP, and the cell at Purple). The heart glyph is Bone, not Blood (colour conflict 4.7.1). No hero marker is shown (the hero is chosen at claim time).
- **Reading order, top to bottom:** tier form and rank pips, the promise chip, the danger numeral beside or below it (one size larger than the chip number) with its word, the class-count row with the elite pip, the name and tier word (confirmation only). All Bone and Fog; tier hue only on the frame; danger never red.
- **Purple price:** no curse form (no hanging tag, no Plum outline, reserved for Gold and Red). The price is read from the danger and the roster; a dimming of the Purple card is not used until the creative director decides whether Purple needs a subtractive cue.
- **Item-quality word:** a plain label from System 18's door row, in Fog, never a probability.
- **States:** default, hover (1 px Candle ring), focus (2 px ring, lifted), armed (ring and corner square), disabled (INK-3 frame). Candle on cards means selection only; no Candle underline on door cards (it belongs to the item shelf).
- **Row appearing:** 7.12 reveal, 240 ms per card with an 80 ms stagger; the promise chip lands with the pips, before the danger numeral.
- **Audio:** at most three cues: row reveal (soft ascending tone, brightness driven by danger), arm (short click), confirm (heavy low tone). Hover is silent. A reduced-flash variant is required; the loudest sound stays for detonations (Pillar 2). Final design belongs to the audio director.
- **Gaps to close in the art bible:** the card pixel budget, the row assignment for the benefit rows, a card buff-chip spacing rule, and whether Purple needs a subtractive cue.

*`art-director` consulted (2026-11-13). Asset specs belong to `/asset-spec`.*

## Game Feel

Choosing a door is a considered, unhurried decision with no timer. The feel targets are:

- **One ratio at a glance.** The player can rank three doors by promise and danger in one look and confirm with the roster in a second look; the popup confirms, it does not discover.
- **The payout is certain.** What the card says (+12 max HP) is what the claim gives, never a roll; the weight of the choice comes from the room, not from the reward.
- **The claim is a small ceremony.** After a win the buff appears as a clear choice of hero before the item offer; opening a cell is felt as space to build in.
- **No input latency from this system.** The row is built once after a win with tens of integer operations; it owns no animation.

## UI Requirements

Layout and interaction belong to Systems 27 and 28 and their UX specs (`/ux-design`); this system fixes what must be available.

| Information | Where | Rule |
|---|---|---|
| Tier name and rank pips | Door card | Text as well as form; never colour alone. |
| Promise chip (+HP, +cell) | Door card | Exact value, from the tier. |
| Danger numeral and word, class counts, elite pip | Door card | From System 16. |
| Item-quality word | Door card | Plain label, no odds. |
| Roster with role glyphs | Door popup | From System 16. |
| Hero choice for the claim, with the suggested hero marked | Claim step | Shows which heroes have a locked cell before the choice; a hero without one warns that the cell would be lost. |
| Caps reached | Claim step | Shows a granted amount smaller than the promise before confirming. |

Accessibility: tier, danger and the promise are never carried by colour alone; the promise is read before the danger by screen readers; the reading order is tier, promise, danger, counts, name.

## Cross-References

| This GDD says | Defined in | Notes |
|---|---|---|
| Three cards per row, tiers may repeat, `MinRoomForTier`, the call order | `design/gdd/encounter-generation.md` Rules 3 and 7 | System 16's draw count of 22 counts only its own calls. |
| Danger, class counts and roster on the card | `design/gdd/encounter-generation.md` Rule 10 | System 16 computes. |
| Door-tier item rows and the stored reading | `design/gdd/commitment-and-drop-model.md` Rules 8, 9 and 13 | The reading needs per-hero counts. |
| `B_perm` growth | `design/gdd/party-and-heroes.md` Rules 3 and 9 | The ceiling of 1000 is not reachable by doors. |
| `Unlock`, locked cells, Thaw | `design/gdd/grid-backpack.md` Rule 12 | Claim after Thaw. |
| `doors` stream and weighted draws | `design/gdd/seeded-rng.md` | Calls, not words. |
| Pillars 1, 3, 4, 5 and CD-SYSTEMS C3 | `design/gdd/game-concept.md`, `design/gdd/systems-index.md` | Player Fantasy anchors. |
| Door card anatomy, tier cues and states | `design/art/art-bible.md` 2.3.3, 3.4.4, 7.10, 7.11, 7.12 | Needs the budget table. |

## Acceptance Criteria

Evidence type: **Logic**, automated EditMode tests in `Assets/_Project/Tests/EditMode/DoorGeneration/` (class `DoorGenerationTests`, assembly `Project05.EditModeTests`), named `[Scenario]_[Expected]`; gate level BLOCKING. UI criteria are **UI** evidence (retained screenshots in `production/qa/evidence/`). Each row is its own `[TestCase]`; each rejection case has a passing control. Tests use test-local fixtures only.

**Fixtures.** `TBL` is a test-local copy of the seven table rows, `MinRoomForTier` 1/2/3/5, buffs `(4,0)/(8,0)/(12,0)/(12,1)`, `MinDistinctRoom` 3, `SafeCardRoom` 4, caps 100 and 3. `TBL-ALT`: buffs `(5,0)/(9,0)/(13,0)/(13,1)`, `MinDistinctRoom` 4, `SafeCardRoom` 6, caps 20 and 3 (proves values and thresholds are data). `TBL-FORCED`: room 5 row `[500,0,500,0]` (leaves one positive weight after mask (a)). `TBL-NOP`: three tiers, Purple cut. `SCRIPT`: the draw seam (asserts the weights array or bound of each call, fails on an extra or missing call or a ticket at or above the total, counts calls per tag, resolves a ticket by one running-total scan, and supports clone and commit to check atomicity); a non-asserting `COUNT` twin is used for the allocation probe. `STUB16`: returns variant weights per `(r, tier)`, default a bound-1 call and `[3,1]` for `(3, White)`; it can be set to throw `ContractViolation`. Hero snapshots `{slot, doorHp, bPermOther, locked}`: H0 `{0,0,0,2}`, H1 `{1,44,0,2}`, H2 `{2,96,0,0}`, H3 `{3,0,90,5}`; the run-level `doorCells` is a separate field (states beyond what play can reach are legal test states). `REF-ROW` is a reference implementation of about 30 lines written from the rule text with integer arithmetic only, hand-checked against criteria 4a, 4b and 14 first and used as the oracle only for the sweeps.

**Rules**

| # | Given / When / Then |
|---|---|
| 1 | **Purity.** `TBL`, `r = 7`, tickets 950, 560, 130, built twice with another build between: the rows are value-equal, each build makes exactly 6 calls on `SCRIPT`, and `BuffOf` makes 0 calls. |
| 2 | **When built.** `w = 1` gives the room 2 row `[600,400,0,0]` for card 1; `w = 7` gives room 8; `w = 0` and `w = 8` give `ContractViolation` with 0 calls. |
| 3a | **Tier draws.** `r = 5`, tickets 600, 299, 850 give Blue, White, Purple; the three tier calls come first, each with `[300,300,250,150]` (card 3 unmasked here). |
| 3b | **Minimum rooms.** `TBL` with Purple weight 100 at `r = 4` gives `ContractViolation` with 0 calls; control: `r = 5` passes. |
| 4a | **Mask (a), room 4.** Cards White, White (tickets 120, 130): card 3 uses `[0,350,250,0]`; ticket 0 gives Green, 349 Green, 350 Blue, 599 Blue. |
| 4a′ | **Mask (a), room 3 and room 2.** `r = 3`, tickets 500, 849 give Green, Green: card 3 uses `[500,0,150,0]` (499 → White, 500 → Blue). `r = 2` with tickets 640, 700, 905 (Green ×3) is accepted with card 3 `[600,400,0,0]`. |
| 4b | **Mask (b).** `r = 7`: Purple, Purple (800, 999); Blue, Blue (500, 799); Purple, Blue — each gives card 3 `[200,300,0,0]`. |
| 4c | **Masks only on card 3.** For `r = 7` and each tier of card 1, card 2 uses the unmasked `[200,300,300,200]`. |
| 4d | **No sorting.** `r = 7`, tickets 950, 560, 130 give Purple, Blue, White, and the variant calls follow in the order `(7,P), (7,B), (7,W)`. |
| 4e | **Sweep.** For `r = 3..8`, over the lowest and highest ticket of each positive tier for cards 1 and 2 and all tickets for card 3: never three identical tiers; for `r ≥ 4`, at least one White or Green card. |
| 4f | **Forced third draw.** `TBL-FORCED`, `r = 5`, White, White: card 3 is a forced bound-1 call (not a weighted call, not skipped) giving Blue, and the row makes 6 calls. |
| 4g | **Thresholds are data.** `TBL-ALT`: Green, Green at `r = 3` is unmasked and at `r = 4` masked; Blue, Purple at `r = 5` is unmasked and at `r = 6` masked (`[250,300,0,0]`). |
| 5a | **Call order.** Tier, tier, tier, variant, variant, variant; the variant draws use the final card order. |
| 5b | **Atomic.** `STUB16` throws on card 2's variant: `ContractViolation` and the committed stream position is unchanged; control: no throw commits 6 calls. |
| 5c | **No partial row.** A row with a disallowed positive tier gives the violation before any call and leaves the stream unchanged. |
| 6 | **Buff.** `BuffOf` gives `(4,0), (8,0), (12,0), (12,1)`; `TBL-ALT` gives `(5,0), (9,0), (13,0), (13,1)`. Never on entry and none for room 1. [BLOCKED: System 5 for the timing] |
| 7a | **Claim, one hero.** H0 claims Blue: `hpGranted = 12` for slot 0 only; other heroes get 0. |
| 7b | **Claim, Purple.** H1 claims Purple: `hpGranted = 12`, `cellsGranted = 1` (the worked example). |
| 7c | **Claim, no locked cell.** H2 claims Purple with `DoorHpCap = 100`: `hpGranted = 4`, `cellsGranted = 0`, and the preview flag "cell lost" is true before the choice. |
| 7d | **Default hero.** Fire committed with per-hero counts `[1,3,3,0]` gives slot 1 (lowest slot); uncommitted gives slot 0; a slot outside the party gives `ContractViolation`. The counts are computed by System 17 from the by-value victory snapshot. |
| 7e | **No commitment feedback.** Claiming does not change the stored reading. [BLOCKED: System 18] |
| 8 | **Order of steps.** Offer and row are built, then Thaw, then the claim, then the item, then the next door. [BLOCKED: Systems 5 and 8] |
| 9 | **Caps over a run.** `TBL-ALT` on one hero (cap 20): Green 8, Blue 12, Blue 0 give `doorHp` 8, 20, 20; four Purple claims with at least four locked cells give cells 1, 1, 1, 0. |
| 10 | **Card content.** The card holds tier, `hp`, `cells`, and the danger and class counts from System 16, and no probability or float member; the quality word is a localisation key per tier. [BLOCKED: Systems 16, 27 and 28] |
| 11 | **Purple price.** The card type has exactly those members and no cost or curse field. The loss-rate gap of at least 10 points over 1,000 seeded runs is ADVISORY. [BLOCKED: System 35] |
| 12 | **Purple cut.** `TBL-NOP` validates with each row summing to 1000; leaving a positive Purple weight, its `MinRoomForTier` entry or its buff row behind is an error. |
| 13 | **Validator.** One failing fixture and one passing control each, with structured errors naming the room and the rule: (a) sums 999 and 1001 fail, 1000 passes; (b) a positive tier below its minimum room; (c) only one positive tier; (d) `r ≥ 4` with White and Green both 0 (`[0,0,600,400]` at `r = 5`); (e) a tier without a cell in `STUB16`; (f) buff hp 12 → 7 fails and equal hp (Blue = Purple = 12) passes; (g) three bad rows report all three and no table is built; (h) out-of-range knobs. [BLOCKED: System 2 for the real catalog stop] |

**Formulas**

| # | Given / When / Then |
|---|---|
| 14 | **F1.** `r = 5`: tickets 299 → White, 300 → Green, 599 → Green, 600 → Blue, 849 → Blue, 850 → Purple, 999 → Purple; `r = 2`: 599 → White, 600 → Green, 999 → Green. All seven rows are asserted as literal arrays. |
| 15 | **F2.** `(3,G,G)` `[500,0,150,0]` total 650; `(4,G,G)` `[400,0,250,0]` 650; `(4,B,B)` `[400,350,0,0]` 750; `(7,W,W)` `[0,300,300,200]` 800; `(7,G,G)` `[200,0,300,200]` 700; `(8,P,P)` `[150,300,0,0]` 450; `(3,B,B)` `[500,350,0,0]` 850; plus the four examples of Formula 2. |
| 16 | **F3.** The same values as criterion 6, read from the table and not from literals. |
| 17 | **F4.** `(doorHp 36, cap 40, Blue)` → 4; `(88, cap 100, Blue)` → 12 and `(89, cap 100, Blue)` → 11 (boundary); `(100, Green)` → 0; H3 (`bPermOther 90`) Blue → 12 (the cap is on `doorHp`, not on total `B_perm`); `doorCells = 3`, Purple → cells 0 with hp 12; `doorCells = 2`, locked 1 → cells 1. |
| 18 | **F5.** Seven rows give 21 tier calls and 21 variant calls, 6 per row for every `r`; the room 1 call belongs to System 16's criterion 22, and the run-level 43 is [BLOCKED: System 5]. |
| 19 | **F6 (ADVISORY, design check).** The exact integer sums per 10^9: room 2 280,000,000; room 3 171,250,000; room 8 73,000,000. Not a production function, so a test-side helper would be circular. |
| 20 | **F7 (ADVISORY).** The sum of the best hp per room is 80, and four Purple rooms give 4 cells against the cap of 3. |

**Non-functional**

| # | Criterion |
|---|---|
| 21 | **Determinism.** Two sweeps over `r = 2..8` with tickets `(37 × i + 11) mod total` are identical and give the same results in reverse order; `REF-ROW` equals production. |
| 22 | **Engine-free.** The core assembly sets `noEngineReferences`; an IL scan finds no `UnityEngine`, mutable `static` field, `float`, `double`, `DateTime`, `Random` or `GetHashCode`. The core assembly name is NOT SOURCEABLE until the architecture ADR. |
| 23 | **Allocation.** Over 1,000 warmed-up `BuildRow` and `ClaimBuff` calls on the `COUNT` seam the managed allocation delta is 0, with an allocating control loop proving the probe works (a struct row and stack or scratch arrays for the masked weights). |
| 24 | **Operation budget (counted by a test seam, not timed).** Exactly 6 draws per row, at most 12 table-weight reads, 0 draws in `ClaimBuff`. Elapsed time is logged only. |
| 25 | **UI (retained screenshots).** The door card shows the tier, promise chip, danger, counts and quality word; the claim step shows the hero choice with the suggested hero and the cell-lost warning. [BLOCKED: Systems 27 and 28] |

**Blocked criteria by system.** System 1: the real `doors` stream, clone and commit, the seam name. System 2: fields, knob ranges and the catalog-build stop. System 5: storage by value, idempotent hand-off, flow order, buff only after a win, the 43 total. System 8: `Unlock`, the locked-cell query, claim-then-item order. System 15: `B_perm` and the no-heal rule. System 16: the real cells and variant interface. System 18: the stored reading. Systems 27 and 28: UI. System 35: criteria 11, 19 and 20.

**Watched failures.** Each mutant must make a named criterion fail: a mask condition off by one room (4a′, 4g); mask (b) written as `else` after (a) so that Purple, Purple gives Purple, Purple, Blue (4b); the mask applied to card 2 (4c); compacted weight arrays (4a, 15); a redraw on collision instead of a mask (5a); sorted cards (4d); skipping the call when one tier remains (4f); `row[w]` instead of `row[r]` (2, 14); index instead of ticket (14); minimum rooms unenforced (3b); the buff granted on entry (6); the buff applied to all heroes (7a); no cap, `≤` for `<` in the cap, or a cap on total `B_perm` (17); the cell cap per hero instead of per run (17, 9); a non-atomic commit (5b); 42 calls with room 1 omitted (the run-level integration criterion); a silent trim in the validator (13g). `SafeCardRoom` 4 and 5 are equivalent with the shipped table and are told apart only through `TBL-ALT` (4g).

> *`qa-lead` consulted (2026-11-13). Its recomputation confirmed Formulas 1 to 7 and the worked examples; its twelve ambiguities are resolved in the Detailed Design above.*

## Open Questions

| Question | Owner | Resolve by |
|---|---|---|
| **Price against reward, all three channels.** System 16's multipliers must be tested against this system's buff and System 18's item-tier rows together; all three follow the tier and multiply at the extremes. Metrics: a tier chosen by a competent policy no more than 70% of the time per room, boss win rate by tier, and the ablation (buff off, item rows flattened). | System 35, economy-designer | First simulation. |
| **Is the buff worth keeping?** If the buff moves the boss win rate by less than 2 percentage points, delete it. | System 35 | First simulation. |
| **Purple's price beyond danger.** The MVP price is the room only. Fallbacks if Purple dominates: a won Purple door offers two item cards (conflicts with System 18 Rules 8 and 10), or a conditional Purple buff keyed to a slot or adjacency (a new mechanism outside System 3). | game-designer, creative-director | After the first simulation. |
| **Lean (Pillar 1) is only a suggestion** (the default hero). Decision (creative director, 2026-11-13): the lean of a door's reward lives in System 18's item offer and the buff is neutral, to avoid counting luck twice; the concept's wording is amended. If playtests show the suggestion is not enough, a directional buff kind needs a run-level modifier in Systems 3 and 5. | game-designer, creative-director | After the first playtests. |
| **System 18 sync:** its Rule 13, test 13 and Dependencies row still say System 17 calls `BiasFactorPermille` and draws buffs on `doors`; this system's buff is certain, draws nothing and does not call the bias function. | Systems 17 and 18 | Sync after this GDD. |
| **System 15:** the `B_perm` ceiling of 1000 is far above the door share (at most 80 from doors); consider lowering it to 100 once other sources are known. | System 15 | When System 18's payout or other sources are decided. |
| **System 8:** a locked-cell query for a hero (so the claim step can warn before the choice) and confirmation that `Unlock` works after Thaw only. | System 8 | Before System 8 is re-reviewed. |
| **System 5 slice:** `doorHp[hero]`, `doorCells`, the stored chosen card, the pending buff with per-hero counts, and the order of the reward steps. | System 5 | System 5's GDD. |
| **System 2:** the tier table, the buff table, the knobs, the unlock-sequence rule (at least `DoorCellCap` locked cells per hero), and the quality-word keys. | Systems 2 and 17 | Next System 2 revision. |
| **Art bible:** a door card pixel budget (the danger numeral, class counts, elite pip and promise chip are not in 7.10), the benefit-row assignment, a card buff-chip spacing rule, the heart glyph in Bone, and whether Purple needs a subtractive cue. | art-director | Art bible follow-up. |
| **Danger and pips.** Rank pips (1 to 6) and the danger numeral (1 to 5) can be confused; the numeral needs a distinct form. | art-director, System 16 | Art bible follow-up. |
| **Rhythm of the table.** The level designer's alternative table (a heavier pre-boss row and a breather at room 7) and the repair rules are recorded as options; the shipped masks are simpler. | level-designer, System 35 | First playtests. |
| **Content version fields:** `doorContentVersion` in the stream record, with `encounterContentVersion` and `dropContentVersion`. | Systems 1 and 24 | PRNG ADR and save design. |

> *Specialists consulted for these questions: game-designer, economy-designer, systems-designer, level-designer, art-director, creative-director, qa-lead (2026-11-13).*
