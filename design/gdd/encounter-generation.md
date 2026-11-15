# Encounter Generation

> **Status**: Designed (CD-GDD-ALIGN CONCERNS, changes A, B and C applied; D pending in the sync package; independent `/design-review` not yet run)
> **Author**: user + Claude design session, with specialist consultation
> **Last Updated**: 2026-11-13
> **Last Verified**: 2026-11-13
> **Implements Pillar**: Power Has a Price (primary, with Systems 17 and 18); Arrangement Is the Thinking (supports)

## Summary

For each door the player can choose, Encounter Generation turns the room number and the door's colour tier into an authored enemy roster, two integer multipliers for enemy HP and damage, a danger rating from 1 to 5 and the monster-class counts shown on the card. Rosters come from authored templates (at most two variants per room and tier), a tier changes what the room asks of the build as well as how big the numbers are, and one draw per card on the shared `doors` stream picks the variant. A content validator rejects illegal rosters, layouts, pacing and speeds when the catalog is built. The system holds no state and runs no fight: System 5 stores the result and System 14 binds the intents from it.

> **Quick reference** — Layer: `Feature` · Priority: `MVP` · Key deps: `Game Data Definitions, Seeded RNG, Enemy and Intent`

## Overview

System 16 decides which enemies the player will face in a room. For each door the player can choose, it turns the room's number and the door's colour tier into an **encounter**: a roster of monsters, two integer multipliers for their HP and damage, a danger rating and the monster-class counts the door card shows. The rosters are authored templates, two variants per (room, tier) cell; the only randomness is one fixed draw per card on the shared `doors` stream to pick between variants of equal danger. A higher tier changes what the room asks of the build (another body, an elite, a sweeper and a multi-hitter at once) and adds a small kicker to HP and damage, so its price is a different problem, not only bigger numbers. System 16 owns this price; the buff the player gains for taking the door belongs to System 17. It also validates all encounter content against rules for legal rosters, layout, pacing and speed, so a bad template is caught when content is authored and never reaches a fight. System 16 itself holds no state and runs no fight: System 5 stores the encounter it generated for each card, and when the player enters the room System 14 binds the enemies' intents from that roster. Without this system a door would be a colour and a number; with it, a door names what it will ask of the arrangement (Pillars 5 and 4).

> *Specialists consulted for this section: game-designer, level-designer, systems-designer, art-director, creative-director (2026-11-13). Their findings are applied below.*

## Player Fantasy

The player stands before three doors. Each one names its price: how dangerous the room behind it is and what kind of enemies wait there. They read the Purple door, look at their Warrior's detonation and decide it can answer a sweeper and a fast multi-hitter, or they read it and take the Green door instead because the build cannot. Then they win or lose, and they know why. **The door names its price, the room keeps its word, and a build that answers a harder room has earned the bigger reward.** The MVP covers four tiers (White, Green, Blue, Purple); the Gold and Red tiers and their curses come later.

*Pillar anchors.* Pillar 5, Power Has a Price: "High-tier doors and items cost the player something real… A drawback that changes how the player builds counts; one the player can ignore does not." A higher tier therefore changes the problem (what the arrangement must cover), not only the size of the numbers. Pillar 4, Arrangement Is the Thinking: "Everything the player needs to decide (item effects, turn order, enemy intent) is visible before the fight starts"; the door choice is a decision, so its price is shown on the card and in a popup before the choice. Pillar 3, Earned Rarity: "If a change makes a run-defining combo independent of player skill, we reject it"; a harder room is a known threat with a fair answer, never a coin flip.

*Who owns what.* This system owns the cost side of the bet. The reward side (buffs, rewards) is System 17's, and the two must be priced against each other.

*Risks the design guards against* (what would turn doors into noise): a danger the player cannot read from the door; tiers that differ only by a small multiplier, so the price can be ignored; a roster that never changes the arrangement; danger that rises faster than any build can answer; a random roll inside a tier that makes one colour sometimes trivial and sometimes deadly; and balance measured only with an always-Purple or always-White player, so that System 35 must use a policy that makes real door choices. Purple is the first cut if the schedule slips, so the price ladder must also make sense with three tiers.

> *`creative-director` consulted (2026-11-13); the spine of the fantasy is theirs.*

## Detailed Design

### Core Rules

1. **A pure function with a validator.** System 16 holds no state and no RNG of its own. `GenerateSpec(catalog, r, t, variantIndex)` maps a room index `r`, a tier `t` and the index of the chosen variant to an encounter spec, and `Validate(catalog)` checks all encounter content. System 16 never calls the RNG: it declares the draw (stream `doors`, the weights of the cell's templates in order) and the **caller** (System 17 at door generation, System 5 at run start for room 1) makes it through System 1 and passes the index back. The draws are made in the order of Rule 7. The spec shares the template's immutable roster array (no per-call allocation); `Layout` writes into a caller-provided buffer.
2. **The floor.** The MVP floor has rooms `1..8` and a boss room, all combat rooms. System 16 owns the floor table (data); System 5 owns the pointer and enters the boss room after room 8. Room 1 is entered at run start with no door, as a White encounter. Doors are offered after rooms 1 to 7 and lead to rooms 2 to 8. The boss room has no door choice and no tier. Shop, event and recruit rooms are not in the MVP.
3. **Tiers.** White, Green, Blue and Purple (an enum from System 2, data-driven for six tiers). `MinRoomForTier`: White 1, Green 2, Blue 3, Purple 5. System 17 offers three cards per row, only of tiers allowed for the target room. **A row may repeat a tier** (user decision 2026-11-13): the row into room 2 can only hold White and Green cards, so it may show three White cards or three Green cards. System 17 owns the distribution of tiers in a row; from room 3 it masks the third card so a row is never three identical cards, and from room 4 it guarantees a White or Green card ([door-generation-and-tiers.md](door-generation-and-tiers.md) Rules 3 and 4). Two cards of one tier draw independently and may hold the same template.
4. **Templates.** A template (System 2) has an ID, a tier, a room range `roomMin..roomMax`, a roster (monster IDs in slot order; 1 to 4 enemies in the MVP, the contract cap being 6) and an authored weight. The templates eligible for `(r, t)` form its **cell**; a cell has one or two.
5. **Composition by tier** (rules for authored content). White: one to three regular monsters in the room's base mix. Green: the White mix of that room plus one extra regular (at most four enemies). Blue: the elite plus one or two regulars (at most three enemies). Purple: exactly three enemies, the elite, a sweeper and a multi-hitter (a fourth enemy breaks the burst bound of Formula 4 from room 5). The White roster of room 8 has three enemies so that Green can add a fourth. At most one elite per roster; at most two copies of one regular monster; windup-capable monsters at most half the roster (`2 × windupCount ≤ size`), so a lone Brute is illegal. The elite's plain heavy counts as heavy-capable in Rule 12.
6. **Pacing and unlock depth.** A monster role may appear only at or after its unlock depth: strike 1, multi-hit 3, sweep 4, windup-then-heavy 6. The elite's heavy is a plain strong strike with no windup, so the elite is available from room 3 with Blue. A tier never puts a role in a room before its depth. Rooms 1 and 2 hold strike-only monsters whose speed is in the hero band (18..26), so that a speed item can change who acts first. The MVP has two strike-only regulars (the Hunter and the Striker) so that Green room 2 (Hunter, Hunter, Striker) respects the two-copies limit.
7. **Selection and draws.** For each card, exactly one draw on `doors` at door generation: a weighted choice among the cell's templates in authored order (a bound-1 draw when there is one). Per row the order is: System 17's draws first, then System 16's three draws in card order. Room 1 makes one draw at run start. The boss makes none. A run makes 22 encounter draw calls (`1 + 3 × 7`), whatever the tier, party or earlier results; a weighted draw may consume more than one RNG word through System 1's rejection, so the count is of calls, not words. System 17 adds three tier draws per row (21 per run), so the shared `doors` stream sees 43 calls per run ([door-generation-and-tiers.md](door-generation-and-tiers.md) Rule 5). The candidates of a cell are taken in ascending template-ID ordinal, never in file or dictionary order, and every weight is at least 1 (a zero total weight would make System 1 refuse the draw). The row is checked first, then drawn on a clone of `doors` and committed atomically, as in System 14's binding; System 17's card order is final before System 16's draws and is never reordered afterwards.
8. **The spec.** `EncounterSpec { templateId, roster, hpPermille, damagePermille, danger, classCounts, eliteCount }`, a pure function of the template, `r` and `t`. System 5 stores it with the card; entering a room never redraws it; System 5 passes the roster and multipliers to System 14's binding.
9. **Multipliers.** `m_hp = 1000 + 60 × (r − 1) + kick_hp(t)` and `m_dmg = 1000 + 40 × (r − 1) + kick_dmg(t)`, with the kicks White 0/0, Green 0/0, Blue +100/+50, Purple +200/+100 (provisional). The boss has `m_hp = m_dmg = 1000`. Speed, hit count, status amounts and monster count are never scaled.
10. **Danger rating and what the player sees.** The danger is an integer `1..5` from the spec's total threat (Formula 3), never from the tier label. The door card shows it as a number with its word and a row of monster-class counts (small, medium, large, with the elite marked); the popup lists the roster by monster and count, each monster with a **role glyph** (strike, multi-hit, sweep, windup-then-heavy) so that the role, which is what changes the problem, can be read without knowing the monster's name. Neither shows scaled HP or damage nor the intent sequence (intents are bound only when the room is entered). The ladder (Formula 5) guarantees that a higher tier never shows a lower danger than a lower tier of the same room; two cards may show the same danger, and then the difference in price must still be visible from the elite pip and the role glyphs.
11. **Layout.** `Layout(roster, sizes)` returns each enemy's line and position: line 1 is filled until the 300 px pitch budget would be exceeded (which also limits a line to three enemies, since four of the smallest class already need 336), then line 2 with the same budget; there is no line 3, so a roster that needs one does not fit. The slot equals the roster order and carries no meaning. `Layout` returns line and order only; the x positions inside a line belong to System 12 and the art bible. The boss stands alone in line 1 and the validator rejects a boss template with companions in the MVP. A template that does not fit is a content error caught by the validator; at run time an unfit roster is a technical stop, never reflowed or trimmed.
12. **Validation.** For every template and cell the validator checks: roster size, one boss at depth 9 only, at most one elite, at most two copies of a regular, unlock depth, at most one heavy-capable monster at White and Green and two at Blue and Purple, the windup share, the burst bound (Formula 4), the speed lint of System 11 against the worst-case hero pool, the layout, variants of one cell within ±10% total threat **and with equal danger rating** (so a cell never shows two dangers), and the tier ladder (Formula 5). The validator reports every error with the template ID, the rule and the room, not only the first. Threat, danger and class counts are computed here at catalog build (design-time, 64-bit) and stored with each `(template, r)`, so run time only reads them. Every check runs for every `(template, r)` with `r` in `roomMin..roomMax`, and the multiplier range of Formula 1 is checked at each such `(r, t)`. Further checks: every weight is at least 1; every cell `(r, t)` of an allowed tier has one or two templates; a template with a tier not in the tier table is an error; there is exactly one boss template; the `θ` list is strictly increasing and the kicks are non-negative and non-decreasing by tier. A violation is a content error that stops the catalog build; nothing is repaired at run time.
13. **No reroll here.** There is no retry, reroll or retreat after a door is chosen (System 5); System 16 never regenerates a room.
14. **Determinism and engine-free** (as Systems 3, 11 and 14): integer arithmetic, no clock, no mutable static state.

### Base pacing (White tier, provisional authoring guidance)

| Room | Enemies | Role mix | Idea taught |
|---|---|---|---|
| 1 | 1 | Hunter (strike) | the tab: glyph, number, target |
| 2 | 2 | Hunter, Hunter | each enemy has its own intent (Green adds a Striker) |
| 3 | 2 | Swarmer, Hunter | multi-hit (`r×h`) |
| 4 | 2 | Sweeper, Hunter | the all-heroes chip |
| 5 | 3 | Swarmer, Hunter, Swarmer | consolidation of what was taught |
| 6 | 2 | Brute, Hunter | windup then heavy |
| 7 | 3 | Brute, Swarmer, Hunter | windup beside a multi-hit |
| 8 | 3 | Brute, Sweeper, Hunter | the exam before the boss (Green adds a Swarmer) |
| Boss | 1 | Boss alone, phases keyed to entry index | introduces nothing new |

### States and Transitions

| Subject | State | Trigger | Result |
|---|---|---|---|
| Spec | Generated | Door row built (System 17) | One draw per card; spec computed. |
| Spec | Stored | System 5 keeps it with the card | Never redrawn. |
| Spec | Chosen | Player picks a door | The roster and multipliers go to System 14. |
| Spec | Spent | Room entered and bound | Intents drawn by System 14. |

### Interactions with Other Systems

| System | Data in | Data out | Interface owner |
|---|---|---|---|
| 2 Game Data Definitions | Template, tier and floor records; monster classification and size | None | System 2 owns fields and validation. |
| 1 Seeded RNG | Weighted and bounded draws on `doors` | None | System 1 owns the stream; Systems 16 and 17 own the shared call order. |
| 17 Door Generation and Tiers | Room index and the offered tiers | The three specs of a row | System 17 owns the reward side and the offer. |
| 5 Run State and Flow | None | Specs to store; the floor table | System 5 stores specs and owns the pointer. |
| 14 Enemy and Intent | Monster data, threat formula | Roster and multipliers for binding | System 14 is the callee. |
| 11 Turn Order | Speed lint rule | Lint results | System 16 enforces it. |
| 12 Combat Resolution | None | Legal roster | System 12. |
| 27, 28, 30 | None | Danger, class counts, roster list | Those systems draw. |
| 35 Balance Simulation Harness | Specs and templates | None | System 35 measures win rates per cell. |

*Provisional:* the template, tier and floor records, the `MinRoomForTier` values and the multiplier curves are assumptions about Systems 2 and 17 and about balance.

> *Specialists consulted: game-designer, level-designer, systems-designer, art-director (2026-11-13).*

## Formulas

All arithmetic is on 32-bit integers (Formula 3 may use a 64-bit intermediate in tooling). Division is integer division with non-negative dividends. All numeric values here are provisional until System 35 measures them.

**Formula 1: encounter multipliers.** `m_hp = 1000 + 60 × (r − 1) + kick_hp(t)` and `m_dmg = 1000 + 40 × (r − 1) + kick_dmg(t)`, in permille.

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| `r` | int | `1..8` | floor table | Room index (the boss uses identity). |
| `t` | tier | White, Green, Blue, Purple | System 2 | Door tier. |
| `kick_hp`, `kick_dmg` | int | White `0/0`, Green `0/0`, Blue `100/50`, Purple `200/100` | tier table (data) | The tier's kicker. |
| `m_hp` | int | `1000..1620`, inside System 14's `500..10,000` | calculated | HP multiplier. |
| `m_dmg` | int | `1000..1380`, inside System 14's `500..5000` | calculated | Damage multiplier. |

*Examples:* room 5, Blue: `m_hp = 1000 + 240 + 100 = 1340`, `m_dmg = 1000 + 160 + 50 = 1210`; room 8, Purple: `1000 + 420 + 200 = 1620` and `1000 + 280 + 100 = 1380`; the boss: `1000` and `1000`.

**Formula 2: scaled stats.** System 14's Formulas 1 and 2 (`enemy_scaled_hp`, `enemy_scaled_damage`) with the multipliers above. *Example:* a Hunter (HP 65, `B = 8`) at room 5, Blue: `ScaledHP = (65 × 1340 + 500) / 1000 = 87` and `ScaledB = (8 × 1210 + 500) / 1000 = 10`.

**Formula 3: total threat and danger rating.** For an enemy `e`, `threat_e = ⌈ScaledHP_e × N_D,e × v_e / (1000 × W_m,e)⌉` (System 14's Formula 7 with scaled values and its fixed party size of 3, so the result does not depend on the run; computed at catalog build in 64-bit, because `HP × N_D × v` can exceed int32 at legal catalog limits; boss threat uses the boss's whole move list with each phase weighted equally); `T = Σ_e threat_e`; `danger = 1 + |{ k : T ≥ θ_k }|` with `θ = [20, 40, 70, 110]`.

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| `N_D,e`, `W_m,e` | int | per System 14 | monster data (scaled `B`) | The monster's weighted damage per action and its weight total. |
| `v_e` | int | `5..60` | monster data | Base speed (not scaled). |
| `T` | int | `≥ 1` | calculated | Total threat of the roster. |
| `θ` | int list | `[20, 40, 70, 110]` (provisional) | tier table (data) | Danger thresholds. |
| `danger` | int | `1..5` | calculated | Shown on the card. |

*Examples:* room 2, White, two Hunters (`ScaledHP = (65 × 1060 + 500) / 1000 = 69`, `ScaledB = (8 × 1040 + 500) / 1000 = 8`): each `threat = ⌈69 × 8 × 20 / 1000⌉ = ⌈11.04⌉ = 12`, so `T = 24` and `danger = 2`. Room 5, Blue: an Elite Bruiser (HP 155, speed 21, strike `B = 12` weight 70 and a plain heavy `B = 20` weight 30) scaled by `1340` and `1210` has `ScaledHP = 208`, strike 15, heavy 24, `N_D = 70 × 15 + 30 × 24 = 1770`, `W_m = 100`, `threat = ⌈208 × 1770 × 21 / 100,000⌉ = ⌈77.3⌉ = 78`; with a Hunter (threat `⌈87 × 10 × 20 / 1000⌉ = 18`), `T = 96` and `danger = 4`.

**Formula 4: burst bound.** `100 × Σ_e A_e < 70 × H_min`, with `A_e` the largest damage one action of enemy `e` can deal to one hero (`n × ScaledB` for a multi-hit, `ScaledB` for a sweep or a strike) and `H_min` the lowest base HP among the heroes (82 with the prototype roster). *Examples* (limit `70 × 82 = 5740`): room 8, White (`m_dmg = 1280`): Brute heavy `(14 × 1280 + 500) / 1000 = 18`, Sweeper `(6 × 1280 + 500) / 1000 = 8`, Hunter `(8 × 1280 + 500) / 1000 = 10`; the sum is 36 and `3600 < 5740`. Room 8, Green adds a Swarmer `3 × ((5 × 1280 + 500) / 1000) = 3 × 6 = 18`; the sum is 54 and `5400 < 5740`. Room 8, Purple (`m_dmg = 1380`): elite heavy `(20 × 1380 + 500) / 1000 = 28`, Sweeper `(6 × 1380 + 500) / 1000 = 8`, Swarmer `3 × ((5 × 1380 + 500) / 1000) = 21`; the sum is 57 and `5700 < 5740`, a pass with a margin of 0.4 HP. A fourth Purple enemy (a Hunter, 11) would give 68 and fail, which is why Purple is exactly three enemies. This margin is the tightest in the MVP; any change to the elite or to the ramps must re-run this check.

**Formula 5: tier ladder.** For every template `x` of tier `t` at room `r` (only tiers allowed at `r`) and every White template `w` of the same room, by cross-multiplication (never truncated division): `100 × T(x) ≥ lo_t × T(w)` with `lo` Green 115, Blue 140, Purple 175 (provisional); a Green template also satisfies `100 × T(x) ≤ 150 × T(w)` (it is White plus one regular); between tiers at the same room `T(Blue) > T(Green)` and `100 × T(Purple) ≥ 115 × T(Blue)`; `min T(White, r + 1) > max T(White, r)` at every step; `max T(top tier, 8) < T(boss)`, where the top tier is the highest tier in the tier table. Blue and Purple have no upper ratio, because the elite fixes a floor on their size; the burst bound and the boss bound cap them. The numbers depend on monster data that are still placeholders, so the lower bounds are re-measured when System 35 runs.

**Formula 6: layout budget.** With the class pitches `S = 84`, `M = 100`, `L = 116`, `B = 176` (the class maxima, art bible 7.4a), a line is legal when it holds at most three enemies and its pitch sum is at most 300. *Examples:* `L + M + S = 300` and `M + M + M = 300` are legal; `L + L + M = 332` is not; the boss alone is `176`. A boss with a companion (`176 + 84 = 260`) is possible in the layout but is rejected in the MVP.

**Formula 7: variants and draws.** The two variants of a cell satisfy `10 × |T_1 − T_2| ≤ max(T_1, T_2)`. Encounter draw calls per run: `1 + 3 × 7 = 22` (calls, not RNG words).

**Formula 8: speed lint (System 11, Rule 13).** `Σ s ≤ 40 × s_min` over the base speeds of three heroes and the enemies of the template, checked for every three-hero subset of the hero pool; `s_min` is the lowest speed in that set. *Example:* with the prototype speeds 29, 26, 24 and 22, the subset (29, 26, 22) and four enemies at speed 26 give `77 + 104 = 181 ≤ 40 × 22 = 880`.

**Worked example (a door row, placeholder monster data).** The row into room 5 holds a White, a Green and a Blue card (`m_hp = 1240`, `m_dmg = 1160` for White and Green; Blue `1340 / 1210`). Placeholder Swarmer: HP 40, speed 26, one move of 3 hits of `B = 5`; Hunter: HP 65, speed 20, `B = 8`. Swarmer at White: `ScaledHP = (40 × 1240 + 500) / 1000 = 50`, `ScaledB = (5 × 1160 + 500) / 1000 = 6`, `N_D = 3 × 6 = 18`, threat `⌈50 × 18 × 26 / 1000⌉ = ⌈23.4⌉ = 24`; Hunter: `81`, `9`, threat `⌈81 × 9 × 20 / 1000⌉ = 15`. Card 1 (White): Swarmer, Hunter, Swarmer, `T = 24 + 15 + 24 = 63`, danger 3, class counts "3 small". Card 2 (Green): the same plus a Hunter, `T = 78`, which is `100 × 78 / 63 = 123.8` percent of White (inside `115..150`), danger 4, "4 small". Card 3 (Blue): the elite (threat 78, Formula 3) and a Hunter (18), `T = 96`, which is 152.4 percent of White (at least 140), danger 4, "1 large (elite), 1 small". Green and Blue show the same danger number but different rosters; whether the thresholds should separate them is an Open Question. The player sees these ratings and rosters on the cards and in the popups and picks one; the chosen spec goes to System 14, which then draws the intents. Each card made one draw on `doors` after System 17's draws, in card order.

> *`systems-designer` consulted (2026-11-13); the cost-based budget composer it proposed is kept as a Tier 2 alternative (Open Questions).*

## Edge Cases

Format: *if condition: exact outcome* (the party that detects it). "Build" is the System 16 content validator at catalog build; "S2" is System 2's catalog validation; a technical stop is System 12's return to the menu with no gameplay loss or reward.

**Content shape (build-time)**

- *A cell `(r, t)` of an allowed tier has no template:* the build fails for that `(r, t)` (build). At run time `GenerateSpec` would return `ContractViolation` and consume no draw; System 17 or System 5 treats it as a technical stop.
- *A cell has more than two templates:* the build fails (build).
- *A weight is zero or negative:* the build fails; every weight is at least 1, because a dead variant would hide a content cost and System 1 refuses a zero total (build).
- *A cell has one template:* an explicit bound-1 draw is still made and consumes its call, so the draw count holds; its authored weight is ignored.
- *A template's tier is not in the tier table (for example Purple after a scope cut):* the build fails; removing Purple means removing its row, its band and its `MinRoomForTier` entry together. Rooms 3 to 8 then offer only White, Green and Blue, the draw count is unchanged, and the ladder check `T(top tier, 8) < T(boss)` uses Blue.
- *A roster fails at one room of its range (layout, burst, speed lint, unlock depth, multiplier range) but passes at another:* the build fails; every `(template, r)` is checked (build).
- *A roster is legal at White but a ladder band of Formula 5 is violated for any pair of variants:* the build fails and reports `100 × T(x) / T(w)` for the offending pair (build).
- *Two variants of a cell differ by more than 10% total threat:* the build fails (build).
- *The roster is the elite alone for Blue or Purple:* illegal (Rule 5); a Blue roster has one or two regulars with the elite.
- *A boss template has a companion, or there is not exactly one boss template:* the build fails.
- *Danger thresholds `θ` are not strictly increasing or the kicks decrease with tier:* the build fails. The build also logs a histogram of danger values so that an unreachable level is visible.
- *The layout of a roster does not fit:* the build fails; at run time an unfit stored roster is a technical stop and is never reflowed or trimmed.

**Draws and rows (run time)**

- *System 17 offers a tier below its `MinRoomForTier`:* `ContractViolation` before any draw; no state changes.
- *A row repeats a tier or even a template:* legal (Rule 3); each card draws independently.
- *The row into room 2:* only White and Green are allowed there; System 17 may show any mix of three cards of those tiers, including three of one tier.
- *Room 1:* one draw at run start, before System 17's first row; the multipliers are 1000 / 1000 by Formula 1; no door or danger is shown beforehand but the encounter has a danger like any other.
- *The boss:* no draw; identity multipliers; no danger is shown on its pre-room card.
- *System 17's card order changes after System 16 has drawn:* forbidden; the order is final before System 16's draws and a violation is a contract error.
- *Candidate order within a cell:* ascending template-ID ordinal. Roster order is slot order and System 14's draw order, so reordering a roster changes the intents for a given seed and is a content-version change.
- *Danger at the boundaries:* with `θ = [20, 40, 70, 110]`, `T = 19` gives 1, `20` gives 2, `109` gives 4, `110` gives 5.
- *Multiplier extremes:* the maxima `m_hp = 1620` and `m_dmg = 1380` lie inside System 14's ranges; the build checks every `(r, t)`.

**Persistence and replay**

- *Restore after a row is generated and before the room is entered:* System 5 stores each spec by value (roster, multipliers, danger, counts); it is never recomputed or redrawn. If the `doors` stream is past the row but the specs are missing, that is a System 5 technical stop, never a re-draw. Snapshots are taken only at the row boundary.
- *Retry after a defeat:* there is none (System 5); a resume (System 24) restores the stored record without drawing.
- *The catalog changes between save and restore:* System 5 rejects a restore whose content version or catalog fingerprint differs (run-state-and-flow.md), so a run never mixes catalogs. Stream records carry the RNG version and an `encounterContentVersion`; save compatibility is System 24's call.
- *A template is added or removed:* each card consumes exactly one draw call apart from rejection, so only that cell's later outcomes change for the same seed. Golden fixtures pin `(seed, catalog version)` to the expected template ID per door and are regenerated deliberately.
- *A stored spec fails System 14's re-check at entry:* System 14 reports a bad roster, multiplier or slot as a contract error; a layout failure is a System 12 technical stop.

**Balance and what this system cannot promise**

- *An encounter cannot be won:* System 16 certifies structure only (Rule 12). Win rates per cell are System 35's job, measured with a policy that makes real door choices, and playtests sign off; a defeat by System 12's 200-action rule is a normal defeat, not an engine error.
- *The intent sequence varies after the door is chosen:* System 14 draws it at room entry, so one roster can give sequences of different difficulty. The danger rating is the expected threat over the move weights; bounding the spread between sequences belongs to System 14 and System 35, and the player sees the intents before the fight (Pillar 4).
- *A build that is strong enough for Purple has no reason to take White:* intended; the reward side belongs to System 17 and the two are priced against each other in Open Questions.

> *`systems-designer` consulted (2026-11-13); its 23 cases are merged and its six contradictions resolved (rows may repeat a tier; White room 8 has three enemies and Purple exactly three; the boss is alone; ladder holds per variant pair; draw count is of calls; weights are at least 1).*

## Dependencies

| System | Direction | Nature | Notes |
|---|---|---|---|
| 1 Seeded RNG | Depends on (hard) | One weighted or bound-1 draw per card on the shared `doors` stream | System 1 owns the stream and the draw primitives; the call order with System 17 is shared. |
| 2 Game Data Definitions | Depends on (hard) | Template, tier and floor records; monster class, role and size | System 2 owns the fields; System 16 owns the rules the validator applies to them. |
| 14 Enemy and Intent | Depends on (hard) | Monster data, threat formula, scaled-stat formulas | System 14 is the callee: it receives the roster and multipliers when the room is entered. |
| 11 Turn Order | Depends on (soft) | Speed lint rule (Rule 13 of System 11) | A violation stops the content build. |
| 15 Party and Heroes | Depends on (soft) | Base HP and speed of the worst-case hero pool for the burst bound and the speed lint | The pool is data, not a run-time input. |
| 17 Door Generation and Tiers | Depended on by | The offered tiers and the call order of draws | System 17 owns the reward side and the offer of three cards (tiers may repeat only in the row into room 2). |
| 5 Run State and Flow | Depended on by | Stores the specs, owns the floor pointer, passes the roster to System 14 | |
| 12 Combat Resolution | Depended on by | Receives a legal roster | |
| 27, 28, 30 (UI) | Depended on by | Danger, class counts and roster list | Those systems draw; this system does not specify their layout beyond UI Requirements. |
| 35 Balance Simulation Harness | Depended on by | Specs and templates to measure | The harness must play a policy that makes real door choices. |

Dependencies are one-directional for the draw (16 calls 1, 14 is called by 5 with 16's output); there is no cycle. The bidirectional back-references are listed in Cross-References.

## Tuning Knobs

All values are provisional until System 35 measures them. They live in data (System 2), never in code.

| Knob | Default | Safe range | Affects | Failure at the extremes |
|---|---|---|---|---|
| `MinRoomForTier` (White, Green, Blue, Purple) | 1, 2, 3, 5 | Green `2..3`, Blue `3..5`, Purple `4..7` | When each tier first appears | Too early: a tier is offered before its roles are unlocked; too late: the player meets Purple too few times to learn it. |
| HP ramp per room | 60 permille | `30..90` | How fast every room grows | High: late rooms outgrow builds; low: rooms feel alike. |
| Damage ramp per room | 40 permille | `20..60` | Same, for damage | Same. |
| Kicks, Blue `hp/dmg` | `+100 / +50` | `0..200 / 0..100` | The extra price of Blue | Large: price is only numbers (the risk named in Player Fantasy). |
| Kicks, Purple `hp/dmg` | `+200 / +100` | `100..350 / 50..200` | The extra price of Purple | Same. |
| Danger thresholds `θ` | `[20, 40, 70, 110]` | Strictly increasing | The 1..5 rating shown on the card | Misplaced thresholds make danger 1 or 5 for every card. |
| Tier ladder lower bounds `lo` | Green 115, Blue 140, Purple 175 (percent of White) | Strictly increasing; Green cap `≤ 150` | How different the prices are | Too low: a higher tier can be nearly as easy as a lower one. |
| Burst bound | 70% of the lowest hero HP | `50..90` | How hard the worst single round can hit | High: unwinnable spikes; low: no tension. |
| Variant tolerance | 10% | `5..20` | How alike two variants of a cell must be | High: one colour is sometimes trivial. |
| Weight per template | 1 | `1..10` | How often a variant appears | Heavy skew hides a variant (and its content cost). |
| Roster cap, Line cap, pitch budget | 4 (MVP), 3, 300 | roster `2..6`, line `2..3`, pitch fixed to art bible | What fits on screen | Beyond the art bible's layout the validator rejects it. |
| Unlock depths | strike 1, multi-hit 3, sweep 4, windup 6 | each `1..8` | Role pacing | Too early overwhelms; too late wastes the monster. |

Interactions: raising the HP or damage ramp moves every threat number, so `θ` and the tier bands must be re-measured together. Purple's kicks and `MinRoomForTier` are the first things to cut if the schedule slips; the ladder then needs only three bands.

## Visual/Audio Requirements

System 16 produces no visuals or sound; it fixes what the doors show and what the room contains.

- **Door card (System 28 draws):** the tier colour (art bible palette; colour is never the only carrier of tier, so the tier name also appears), the danger numeral with its word, and the class-count row. The row uses the monster-size silhouettes of the art bible (small, medium, large), with the elite marked by the elite pip. One card must be readable at the door row's size (art bible 7.4a).
- **Popup (shared System 26, used by System 28):** the roster as monster names with small portraits, counts and a role glyph per monster (strike, multi-hit, sweep, windup-then-heavy); no scaled stats and no intents. The glyphs share the intent glyph language of System 14 so that the role learned at the door is the one seen in the fight.
- **Audio:** the tier of a door has an audio identity owned by System 17 and the audio director; System 16 requires only that the danger rating can drive a brightness or weight change in the door-select sting. No audio ships from this system.
- **Room layout:** enemies stand in the lines and pitches of Rule 11; the art bible's large and boss silhouettes (116 and 176 px pitch) are the binding sizes.

*`art-director` consulted (2026-11-13); elite pip and class silhouettes are theirs. Asset specs belong to `/asset-spec`.*

## Game Feel

A door choice is a considered, unhurried decision, not a reflex one; there is no timer. The feel targets are:

- **Reading takes seconds, not minutes.** The card must let the player rank three doors by danger at a glance and by roster in one more look (a popup is for confirmation, not for discovery).
- **The room keeps its word.** What the card said (the roster, the class counts) is exactly what appears in the room, and the danger rating already includes the scaled HP and damage, so no number in the room is harder than the card implied.
- **No input latency from this system.** It runs at door generation only, once per card, outside any fight; its cost is tens of integer operations per card, far below one frame. There is no animation owned here.

## UI Requirements

System 16 only specifies what must be available to the UI; layout, screens and interaction belong to Systems 27, 28 and 30 and their UX specs (`/ux-design`).

| Information | Where | Rule |
|---|---|---|
| Danger numeral `1..5` and its word | Door card | From Formula 3 only; shown before the choice. |
| Class-count row (small, medium, large, elite marked) | Door card | From the spec's `classCounts` and `eliteCount`. |
| Roster list with counts and role glyphs | Door popup | Monster names, counts and one role glyph per monster; no scaled stats, no intents. Optional: the card also shows the role glyphs. |
| Tier name | Door card | Always text as well as colour. |
| Room index | Door card and run HUD | Which room the door leads to. |

Accessibility: tier and danger are never carried by colour alone; the danger word is localised text and the numeral is read by screen readers before the roster. The floor map (System 30) must not imply that unseen rooms have fixed content.

## Cross-References

| This GDD says | Defined in | Notes |
|---|---|---|
| Enemy scaled HP and damage use `m_hp` and `m_dmg` | `design/gdd/enemy-and-intent.md` Formulas 1 and 2 | System 16 supplies the multipliers; System 14 computes. |
| Threat per monster | `design/gdd/enemy-and-intent.md` Formula 7 | Reused with scaled values. |
| Speed lint | `design/gdd/turn-order.md` Rule 13 | Enforced by the validator here. |
| Weighted and bounded draws on `doors` | `design/gdd/seeded-rng.md` | Call order with System 17. |
| Template, tier and floor records | `design/gdd/game-data-definitions.md` | Fields are System 2's. |
| Hero pool for burst bound and lint | `design/gdd/party-and-heroes.md` | Base HP and speed values. |
| Pillars 3, 4 and 5 | `design/gdd/game-concept.md` | Player Fantasy anchors. |
| Layout pitches S84 M100 L116 B176 | `design/art/art-bible.md` section 7.4a | Binding for Formula 6. |

## Acceptance Criteria

Evidence type: **Logic**, automated EditMode tests in `Assets/_Project/Tests/EditMode/EncounterGeneration/` (assembly `Project05.EditModeTests`), named `[Scenario]_[Expected]`; gate level BLOCKING. UI criteria are **UI** evidence (retained screenshots in `production/qa/evidence/`). Each case below is its own `[TestCase]` and each rejection case has a legal control beside it. Values come from the test-local fixtures, never from shipped balance data (θ, the ladder bounds, the curves and the pitches are read from a fixture tier table, so retuning does not break the tests).

**Fixtures (test-local, placeholder monster data chosen for hand-checking).** Moves are `n hits, B, target, weight`.

| Monster | HP | Speed | Size | Moves |
|---|---|---|---|---|
| HUNTER | 65 | 20 | S | Strike 1, 8, RandomHero, 100 |
| STRIKER | 70 | 20 | M | Strike 1, 8, RandomHero, 100 |
| SWARMER | 40 | 26 | S | Multi-hit 3, 5, RandomHero, 100 |
| SWEEPER | 55 | 18 | M | Sweep 1, 6, AllHeroes, 100 |
| BRUTE | 100 | 20 | L | System 14's fixture: Strike 8 / 60, Windup 20 into Heavy 14 / 0 |
| ELITE | 155 | 21 | L | Strike 12 / 70, plain Heavy 20 / 30 (no windup) |
| BOSS-T | 400 | 22 | B | Strike 10 / 100, Sweep 6 / 100 (equal phase weights) |
| EXACT | 50 | 20 | – | Strike 1, 8, RandomHero, 100 |

Hero pool speeds `[22, 24, 26, 29]`, `H_min = 82`. Cells: (1, W) `[HUNTER]`; (2, W) `[HUNTER, HUNTER]`; (2, G) `[HUNTER, HUNTER, STRIKER]`; (3, W) variant 1 `[SWARMER, HUNTER]` weight 3 and variant 2 `[SWARMER, STRIKER]` weight 1; (3, B) `[ELITE, HUNTER]`; (5, W) `[SWARMER, HUNTER, SWARMER]`; (5, B) `[ELITE, HUNTER]`; (8, W) `[BRUTE, SWEEPER, HUNTER]`; (8, G) the same plus SWARMER; boss `[BOSS-T]`. `SCRIPT` is the draw seam of System 14's tests (asserts the weights or bound per call, fails on extra or missing calls, counts calls, any word gives 0 on a bound-1 draw). `REF-GEN` is a reference implementation of about 40 lines written from the rule text with integer arithmetic only and no production code; it is hand-checked against criteria 8, 10 and 17 first and is used as the oracle only for the "every legal cell" sweeps, never for a criterion with explicit values.

**Rules**

| # | Rule | Given / When / Then |
|---|---|---|
| 1 | Purity | **Given** every legal `(r, t, variantIndex)`, **when** specs are generated forward and then in reverse order, **then** they are value-equal and `SCRIPT` records 0 calls (no hidden RNG, no static cache). |
| 2a | Floor | **Given** the floor table, **then** it has 9 entries, all combat, room 1 is White, doors are offered after rooms 1 to 7 only. |
| 2b | Floor | **When** `GenerateSpec(9, t, 0)` is called, **then** `ContractViolation`; `BossSpec` returns `[BOSS-T]`, 1000 / 1000 and needs 0 draws. |
| 3 | Tier minimums | **Given** `MinRoomForTier` White 1, Green 2, Blue 3, Purple 5: `(1, G)`, `(2, B)` and `(4, P)` give `ContractViolation`; `(2, G)`, `(3, B)` and `(5, P)` succeed. A row of three Green cards into room 2 is accepted and draws three times. |
| 4 | Cell | **Given** White templates with ranges `[1..2]` and `[2..4]`, **then** cell(1) is the first, cell(2) both, cell(3) the second; cell(5) is empty (validator error); a cell of three templates is a validator error; rosters of size 0 and 5 are rejected, sizes 1 and 4 accepted. |
| 5 | Composition | **Given** these rosters, **then** the validator accepts or rejects as marked: White with an elite ✗; Blue `[ELITE]` ✗, `[ELITE, HUNTER, HUNTER]` ✓; Purple `[ELITE, SWEEPER, HUNTER]` ✗ (no multi-hitter), `[ELITE, SWEEPER, SWARMER]` ✓, any four-enemy Purple ✗; two elites ✗; `[HUNTER ×3]` ✗; `[BRUTE]` ✗ and `[BRUTE, HUNTER]` ✓; Green not equal to its room's White plus one regular ✗. |
| 6a | Unlock depth | SWARMER at `r = 2` ✗ and `r = 3` ✓; SWEEPER at 3 ✗ and 4 ✓; BRUTE at 5 ✗ and 6 ✓; ELITE with Blue at 3 ✓. |
| 6b | Early speed band | In rooms 1 and 2 a monster of speed 18 and 26 is ✓, 17 and 27 are ✗. |
| 7a | Weighted draw | **Given** the cell (3, W) with weights `[3, 1]`, **when** the caller draws, **then** `SCRIPT` sees weights `[3, 1]` in ascending template-ID order; tickets 0 to 2 give variant 1 and ticket 3 variant 2 (a mutant using the index instead of the ticket fails). |
| 7b | One template | **Given** a one-template cell, **then** exactly one bound-1 draw is made and any word selects that template. |
| 7c | Draw order | **Given** a row, **then** three draws are made in card order after System 17's draws. [BLOCKED: System 17 for the order against its draws] |
| 8 | Spec | **Given** `(5, B)` variant 0, **then** `templateId`, roster `[ELITE, HUNTER]` in order, `1340 / 1210`, danger 4, class counts S1 M0 L1 and `eliteCount 1`; the type has exactly those 7 members (no scaled HP, no intents). |
| 9 | Multipliers | Covered by criterion 15. |
| 10 | Danger from threat | **Given** `(1, W)` with `T = 11`, danger 1; `(8, W)` with `T = 75` and `(3, B)` with `T = 82`, danger 4 for both (different tiers). The card word and the popup are UI criteria 26 and 27. |
| 11 | Layout | `[L, M, S]` and `[M, M, M]` give one line; `[L, L, M, S]` gives `[L, L]` and `[M, S]`; `[L, M, S, L]` gives `[L, M, S]` and `[L]`; five L gives `NotFit` and no output (never reflowed or trimmed); `[B]` stands alone; a boss with a companion is a validator error. |
| 12a | Validator, one case per check | Each of roster size, one boss at depth 9, elite count, copies, unlock depth, heavy-capable count (two BRUTE at White ✗, `[ELITE, BRUTE]` at Blue ✓), windup share (`2 × windupCount ≤ size`), burst, speed lint, layout, variant tolerance, equal danger of variants, ladder, weight at least 1, `θ` strictly increasing, one boss template, has a failing fixture that names the template, the rule and the room, and a passing control. The whole legal fixture catalog yields 0 errors; a catalog with 3 bad templates reports all 3. |
| 12b | Catalog build | **Given** one bad template, **then** no catalog is returned. |
| 12c | Run time | **Given** a 7-enemy stored roster, **then** `ContractViolation`; nothing is trimmed. |
| 13 | No reroll | [BLOCKED: System 5] |
| 14 | Determinism, engine-free | Two runs over all legal cells are identical; the core assembly sets `noEngineReferences`; an IL scan finds no `UnityEngine`, mutable `static` field, `float`, `double`, `DateTime`, `Random` or `GetHashCode`. |

**Formulas**

| # | Formula | Given / When / Then |
|---|---|---|
| 15 | F1 multipliers | `(1, W)` 1000 / 1000; `(2, G)` 1060 / 1040; `(3, B)` 1220 / 1130; `(5, B)` 1340 / 1210; `(8, W)` 1420 / 1280; `(8, P)` 1620 / 1380; boss 1000 / 1000. A mutant with `r` instead of `r − 1` fails. |
| 16 | F2 scaling | The spec holds permille only. HUNTER at `(5, B)` through System 14's function gives 87 and 10; HUNTER `B = 5` at 1100 gives 6 (half-up; a truncating mutant gives 5). [BLOCKED: System 14 for the single-source check] |
| 17 | F3a threat | HUNTER at `(1, W)`: `⌈65 × 800 × 20 / 100,000⌉ = 11` (a flooring mutant gives 10); EXACT at `(1, W)` gives exactly 8, not 9; `(2, W)` is `12 + 12 = 24`; `(5, B)` ELITE 78 and HUNTER 18. |
| 18 | F3b danger | With `θ = [20, 40, 70, 110]`: `T = 1, 19, 20, 39, 40, 69, 70, 109, 110` give danger `1, 1, 2, 2, 3, 3, 4, 4, 5`. |
| 19 | F4 burst | With `H_min = 82`: `ΣA = 54` passes; `57` passes (`5700 < 5740`); `58` fails; with `H_min = 80`, `ΣA = 56` fails (strict `<`; an `≤` mutant fails this). `(8, P)` `[ELITE, SWEEPER, SWARMER]` is 28 + 8 + 21 = 57 and passes. |
| 20 | F5 ladder, `T_W = 20` | Green: 22 ✗, 23 ✓, 30 ✓, 31 ✗. Blue: 27 ✗, 28 ✓. Purple: 34 ✗, 35 ✓. With Blue 40: Purple 45 ✗ and 46 ✓. `T(W, 6) = T(W, 5)` ✗. `T(P, 8) = 124` against BOSS-T 124 ✗. With two White variants the lower bound uses each of them. |
| 21 | F6 pitch | Pitches are read from data: a fixture with `M = 101` makes `[M, M, M]` (303) illegal. |
| 22 | F7 variants | Threat pairs `(9, 10)` ✓, `(10, 9)` ✓, `(19, 22)` ✗; `(3, W)` variants 32 and 33 ✓. Variants with `T = 19` and `T = 20` are within 10% but differ in danger ✗. A fixture run with different tickets makes exactly 22 draw calls (`1 + 3 × 7`); a mutant that omits room 1 makes 21. |
| 23 | F8 lint | Heroes `(29, 26, 24)` plus enemies `[5, 60, 56]`: `79 + 121 = 200 ≤ 40 × 5` ✓; `[5, 60, 57]` = 201 ✗. The subset `(29, 26, 22)` with four enemies at 26 gives 181 ✓. |

**Performance and non-functional**

| # | Criterion |
|---|---|
| 24 | **Allocation.** Over 1,000 warmed-up `GenerateSpec` and `Layout` calls the managed allocation delta is 0; a deliberately allocating control loop is detected (so the probe is not broken). |
| 25 | **Operation budget (counted by a test seam, not timed).** `GenerateSpec` reads the catalog data without drawing; the caller's draw is 1 call; `Layout` reads at most 4 pitches; `Validate` visits each template at most once per check on the 50-template fixture catalog. Elapsed time is logged only. |
| 26 | **UI (retained screenshots).** Door card shows the danger numeral and its word, the class-count row and the tier name in text. [BLOCKED: System 28] |
| 27 | **UI (retained screenshots).** Shared System 26 popup for the System 28 card lists the roster by monster, small portrait and count with one role glyph per monster, with no scaled stats and no intents. [BLOCKED: Systems 26 and 28 implementation/evidence] |

**Blocked criteria by system.** System 2: template, tier and floor fields and record validation (weight at least 1, `roomMin ≤ roomMax`, unknown monster ID). System 5: spec stored and never redrawn, no reroll, floor pointer. System 12: legal-roster acceptance and the run-time technical stop. System 14: criterion 16's single-source check and the binding. System 17: row composition and the order of its draws. System 1: the draw-seam name until its ADR. System 35: shipped values are provisional, so a "shipped values equal GDD" test is ADVISORY.

**Watched failures.** Each of these mutants must make a named criterion fail: a validator that accepts everything (criteria 5, 12a, via the legal control); an unlock-depth off-by-one (6a); index instead of ticket (7a); a static last-result cache (1, caught by the reversed order); floor or `+1` ceiling in threat (17); truncation instead of half-up (16); `≤` instead of `<` in the burst bound (19) and `<` instead of `≤` in the lint (23); inclusive and exclusive band edges (20); a layout that ignores the budget (11); a broken allocation probe (24).

> *`qa-lead` consulted (2026-11-13). Its recomputation confirmed Formulas 1 to 4, 6 and 8 and found the contradictions now fixed in this document (door-row example, `s_min`, boss companion, White room 8, Green room 2 copies, row distinctness, signature of `GenerateSpec`, overflow, party size, variant danger, ladder cross-multiplication, layout limits, weights).*

## Open Questions

| Question | Owner | Resolve by |
|---|---|---|
| **Tier distribution in a row.** A row may repeat a tier (user decision), so three identical cards are possible, which is not a decision. Answered by System 17 ([door-generation-and-tiers.md](door-generation-and-tiers.md), 2026-11-13): from room 3 the third card is masked so a row is never three identical cards, and from room 4 a White or Green card is guaranteed; only the row into room 2 may be monochrome (28%). | System 17, game-designer | System 17's GDD. |
| **Price against reward.** Tier prices here must be paired with System 17's buffs and with the item-tier rows of System 18 ([commitment-and-drop-model.md](commitment-and-drop-model.md), Rule 9), which are a second reward on the same door; the pairing is untested and the cap and rows multiply at the extremes; the three-channel metric is defined in [door-generation-and-tiers.md](door-generation-and-tiers.md) and handed to System 35. | Systems 17, 18, 35 | System 17's GDD and first simulation. |
| **Danger thresholds `[20, 40, 70, 110]`.** In the worked example Green and Blue both show danger 4. Should the thresholds separate tiers, or is a shared number acceptable? Constraint for System 17 and the UI: when two cards in a row show the same danger, the card must still show the difference in price (the elite pip and the role glyphs), and the reward must not be the deciding factor; the thresholds are re-tuned if playtests show players reading equal danger as equal price. | game-designer, System 35, System 17 | First simulation. |
| **Monotonic White threat** `T(White, r + 1) > T(White, r)` may fail when monster data are authored (a low-threat Sweeper in room 4). Authoring must satisfy it or the rule needs a data flag. | Systems 2 and 14 content pass | First monster data. |
| **The elite and the boss.** With placeholder stats the room 8 Purple threat can exceed the boss's, and the burst margin at `(8, P)` is 0.4 HP. The boss must be authored above it, and the elite's heavy and the ramps re-checked together. | systems-designer, System 35 | Content pass. |
| **Cost-based composer.** The systems-designer's alternative (a cost per monster and a per-room budget curve, 76 to 132, boss 180) is kept for a Tier 2 where templates are generated, not authored. | systems-designer | After the MVP. |
| **Answerability of each template.** The validator proves structure only; whether the MVP item pool answers each roster is measured by System 35 with a real door policy. | System 35, qa-lead | First simulation. |
| **Danger words.** The five words (Low, Moderate, High, Severe, Extreme) and their localisation keys. | System 27, writer, localization-lead | UI spec. |
| **Boss threat** is computed over the boss's whole move list with equal phase weights; System 14 may define a better rule. | System 14 | Boss authoring. |
| **Draw-seam name** and the `encounterContentVersion` field in stream records. | System 1, System 24 | PRNG ADR and save design. |
| **Hero-pool inputs** to the burst bound and the lint use all four prototype heroes; if recruitable heroes grow, the pool must be re-read. | System 15 | Hero roster change. |
| **Retry after defeat.** Answered by System 5 (2026-11-13): there is no retry. | System 5 | Closed. |
| System 19 adds Gold templates (Purple's composition, `kick_hp` +300 and `kick_dmg` +100, a `curseId` on every Gold template, a ladder constant of 105 percent over Purple), keeps the burst bound for uncursed heroes (cursed heroes are measured by System 35), and needs `T(Gold, 8) < T(boss)` ([curses-and-trade-offs.md](curses-and-trade-offs.md)). | Systems 16 and 19 | When the Vertical Slice starts. |

> *Specialists consulted for these questions: game-designer, level-designer, systems-designer, qa-lead, art-director, creative-director (2026-11-13).*
