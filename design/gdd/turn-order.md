# Turn Order (speed gauge)

> **Status**: In Revision (independent /design-review NEEDS REVISION 2026-11-12; revision applied, re-review pending)
> **Creative Director Review (CD-GDD-ALIGN)**: NOT ASSESSED 2026-11-12 — not run by the authoring session (agents are spawned only when the user asks). The independent review's creative-director verdict was NEEDS REVISION: "the core is sound".
> **Author**: user + Claude design session
> **Last Updated**: 2026-11-12
> **Last Verified**: 2026-11-12
> **Implements Pillar**: Arrangement Is the Thinking (primary); The Payoff Is the Product (cause must be readable)

## Summary

Turn Order decides which hero or enemy acts next: every unit fills an integer gauge in proportion to its speed, and the unit that fills it first acts, with a fixed tie-break and no randomness. It is pure, so it also forecasts the order ahead for the order strip and tells Item Trigger and Cooldown where each item sits in the party's sequence. It exists so that turn order is the time axis of the build, readable before the fight and recognisable during it (Pillars 4 and 2).

> **Quick reference** — Layer: `Core` · Priority: `MVP` · Key deps: `Game Data Definitions, Event Log, Combat State and Step Model, Enemy and Intent, Party and Heroes`

## Overview

System 11 decides who acts next. Every living hero and enemy has a **speed** and a **gauge**: the gauge fills in proportion to the speed, and the unit whose gauge reaches the threshold acts. Time advances in whole integer steps, always straight to the moment the next unit becomes ready, so the order never depends on frame rate or on the presentation. When several units are ready at once, a fixed tie-break decides: more gauge first, then more speed (a bias toward fast units that is deliberate), then heroes before enemies, then party or encounter slot. A unit that acts spends one threshold of gauge and keeps the rest, so speed is exactly proportional to how often a unit acts. In the MVP a unit's speed is its authored base speed plus, for a hero, the speed bonuses of the passive items in its frozen grid, and it does not change during the fight; the contract also allows later statuses to change speed (Rule 7). System 11 never mutates anything itself: it answers "who acts next, after how much time" and System 12 commits the result through System 36. It also answers the same question for the future, as a pure forecast, so that the arrangement screen and the combat HUD can show the order the fight will take and Systems 9 and 25 can place each item in the party's sequence. Because heroes and enemies share one gauge scale, turn order is the time axis of the build: one hero sets up, another cashes in, and the player can see that order before the fight (Pillars 4 and 2).

> *Specialists not consulted by the authoring session (not requested). The independent review of 2026-11-12 supplied specialist findings; they are applied below.*

## Player Fantasy

The player looks at the order strip and sees the fight before it happens: the Tanker, then the Warrior, then the goblin, then the Support. They know why: a faster unit acts more often, and a slow enemy acts late. **The order is theirs to shape through the build:** a speed item in one hero's grid makes that hero act sooner and more often, so where the player puts their items decides who sets up and who cashes in. The strip tells the truth about itself: it is a forecast, marked "if nothing changes", and it shifts when an HP step downs a unit or, in a later release, when a status changes a speed. **The fight is a sequence the player composed, and they can read it.** This serves the Core Fantasy (the architect of a perfect machine), Expression (speed is one more axis of the build) and Challenge (the same enemy speed asks a different answer from a different party).

*Pillar anchors.* Pillar 4, Arrangement Is the Thinking: "Everything the player needs to decide (item effects, turn order, enemy intent) is visible before the fight starts." The forecast shows the order and says what it assumes. Pillar 2, The Payoff Is the Product: "the player can always see which arrangement caused them." A shift in the order always has a recorded cause.

*References.* Speed-gauge and active-time-battle systems let the player read who is next; this game adds that the gauge is exactly proportional to speed and never random, so the player can plan around it.

> *`creative-director` not consulted by the authoring session (not requested); the independent review's verdict is recorded in the header.*

## Detailed Design

### Core Rules

1. **Units and speed.** Every living hero and enemy is a unit. Each unit has an authored **base speed** `s` (System 2, integer, provisional `5..60`) and an **effective speed** `v = clamp((max(0, s + F) × (1000 + P) + 500) / 1000, 1, V_max)`, where `F` is the flat modifier and `P` the percent modifier in permille. In the MVP, `F` is the sum of the `speedBonus` of the passive items in the hero's frozen grid (each provisional `−10..10`; an enemy has `F = 0`) and `P = 0`. The sums are clamped: `F` to `−50..50` and `P` to `−1000..1000`. `P` is the hook for future statuses (Rule 7), with the same additive bucket and rounding as System 3's damage. System 11 owns the pure function `EffectiveSpeed(s, F, P)`; System 36 stores the contributions and calls it to derive `v` for its views.
2. **Gauge.** Each unit has an integer gauge, initially `0`. The **threshold** is `T = 1000` and the largest effective speed is `V_max = 200`, a constant with the invariant `V_max < T`. A unit is **ready** when its gauge is at least `T`. System 36 stores the gauge (int32); it never exceeds `G_max = T + V_max − 1 = 1199`.
3. **Next actor (a pure procedure).** `Next(state)` does not change the state. (a) If some living unit is ready, no time passes (`Δ = 0`). (b) Otherwise `Δ` is the smallest, over living units, of `⌈(T − gauge) / v⌉` (at least 1), and every living unit's gauge would rise by `v × Δ`. (c) Among the ready units it chooses by the tie-break key (Rule 4). It returns the unit, `Δ` and the number of ready units.
4. **Tie-break (speed-biased by design).** The key, in order: larger gauge, larger effective speed, hero before enemy, smaller party slot (heroes) or encounter slot (enemies). Side and slot identify a unit, so the key is unique and no instance ID is needed. It uses no RNG and no clock. The preference for larger gauge and speed favours fast units on purpose; players read the strip, not the gauges.
5. **Acting.** A unit that acts, whether it fires its items, is stunned (`ActionSkipped(stunned)`) or has nothing ready (`ActionSkipped(no_ready_item)`), spends `T` of gauge and keeps the rest. A skipped turn **keeps the unit's slot but costs the action**: the order is unchanged, and the unit's items lose that firing (System 9, Rule 9).
6. **Commit.** System 12 calls `Next` and then commits one System 36 transaction: the action record (`ActionStarted` or `ActionSkipped`) carries `Δ` and the acting unit's gauge before and after; applying it raises every living gauge by `v × Δ` and then lowers the actor's by `T`. System 11 adds no record kind of its own. Pre-fight actions (System 9, Rule 10) carry `Δ = 0` and spend no gauge. The test-only `Apply(state, record)` is the specification of that application.
7. **Speed sources and changes.** In the MVP the effective speed of every unit is fixed from the baseline: base speed plus, for a hero, the speed bonuses of the frozen passive items. **Provisional contract for later releases:** a status may supply modifiers `F` and `P`; they take effect from the next `Next` call, never alter a stored gauge, and last a number of the **affected unit's own actions** (never time steps). No GDD defines such a status yet, so everything that depends on one is blocked (Acceptance Criteria). There is no gauge pushback in the MVP (the prototype's stun pushback is not adopted).
8. **Downed units.** A unit with zero HP is ignored from the moment its HP step commits, in `Next`, `Forecast`, `Schedule` and `GlobalIndex`. Its gauge stays stored and unused; there is no revival in the MVP.
9. **Result codes.** `Next` returns `Ok`, `NoLivingUnit` (no unit is alive; System 12 decides outcomes before calling, so this is a contract signal, not a gameplay result) or `ContractViolation` (a base speed or a contribution out of its range, a gauge out of `0..G_max`, a duplicate slot on one side). A unit that is only waiting for its gauge is never "unable to act": because `v ≥ 1`, any living unit becomes ready in finite time, so **System 11 never reports "no actor can act again" while a unit lives**. System 12's outcome row 5 cannot occur in the MVP.
10. **Initial state.** At the baseline every unit's gauge is `0`, so the first order is decided by speed and the tie-break. No RNG is involved.
11. **Forecast and schedule.** `Forecast(state, K, buffer)`, with `K` in `1..20`, lists the next `K` units that would act, each with the time `Δ` before it, assuming that all effective speeds stay as they are now, that nobody is downed and that every action takes its slot. `Schedule(state, completedActions, ordinals, N, buffer)`, with `N` in `1..200`, is the batch version: it lists the next `N` actions with the unit, `Δ`, the global index and the owner's ordinal. Both are pure, allocation-free (the caller supplies the buffer) and return `Ok`, `BufferTooSmall` (no partial output) or `ContractViolation`. The strip and every forecast must say on screen that they hold "if nothing changes", and they are recomputed after every HP step that downs a unit, every edit of the arrangement and, later, every speed change.
12. **Global index.** `GlobalIndex(state, completedActions, ordinals, hero, a)` is a pure function over the state System 36 supplies: the number of completed actions of the encounter and each hero's scheduled-action ordinal `a`. It returns the position, counted from the first scheduled action and over all units and all actions (skipped ones too), of that hero's `a`-th scheduled action, simulating forward from the supplied state for future actions; it reads no recorded sequence. It is `Beyond` when the position would exceed the 200-action limit. Because a skipped turn keeps its slot, a stun never changes it. It drives System 9's party-wide firing rank (Formula 5 there), so it is part of what the player is shown, not only a display value.
13. **Content lint.** An authored encounter must satisfy `Σ s_u ≤ 40 × s_min` over the base speeds of the units in it (the roster heroes and the encounter's enemies), which keeps every unit at five or more actions within the 200-action limit. System 16 and System 2 report a violation as a content error. It is not applied to a player's build: a build that makes a unit act rarely simply makes it act rarely.
14. **Engine-free.** Integer arithmetic only: the clamps of Rule 1 keep every intermediate value inside 32 bits, and an out-of-range input returns a result code instead of throwing. No RNG, no clock, no `UnityEngine`, no mutable static state (the same rules as System 3). The input is a read-only snapshot of the units supplied by the caller; its exact type is fixed by the language-level ADR (NOT SOURCEABLE from `docs/engine-reference/unity/`).

### States and Transitions

| Subject | State | Trigger | Result |
|---|---|---|---|
| Unit | Waiting | `Next` advances time and gauge < `T` | Stays waiting. |
| Unit | Ready | gauge ≥ `T` | Candidate for the tie-break. |
| Unit | Acting | Chosen by `Next` and committed | Gauge lowered by `T`; Waiting again or still Ready. |
| Unit | Any | HP step to zero | Removed from consideration. |
| Encounter | Planning | Edit of the arrangement | Forecast recomputed; nothing stored. |
| Encounter | Running | System 12 loop | `Next` then commit, repeated until an outcome. |

### Interactions with Other Systems

| System | Data in | Data out | Interface owner |
|---|---|---|---|
| 2 Game Data Definitions | Base speed `s` of heroes and monsters; `speedBonus` of passive items | None | System 2. |
| 36 Combat State | Gauges, speed contributions, living status, slots, completed actions, ordinals | Applies `Δ` and the spend through the action transaction; derives `v` with `EffectiveSpeed` | System 36 owns state. |
| 4 Event Log | None | `ActionStarted` and `ActionSkipped` carry `Δ` and the gauge before and after | System 4 owns the schema (small change). |
| 12 Combat Resolution (caller) | Calls `Next` | Actor, `Δ`, ready count | System 12 owns the loop and the commit. |
| 13 Status Effects | (later) speed modifiers with sources | None | System 13 owns which statuses change speed. |
| 9 Item Trigger | None | `GlobalIndex` and `Schedule` | System 9 uses them for its firing rank. |
| 14, 15 | Encounter and party slot indexes; base speed; passive speed bonuses | None | Systems 14 and 15 define slots. |
| 25, 27 | None | Forecast and schedule for the order strip and the arrangement screen | Those systems draw. |
| 16 Encounter Generation | None | The content lint (Rule 13) | System 16 enforces it. |

*Provisional:* speed statuses of System 13, the `speedBonus` field and the base speed range in Systems 2 and 15, and the `Δ` fields in System 4's records are assumptions about systems not yet revised or designed.

> *Specialists (`systems-designer`, `game-designer`, `ai-programmer`) not consulted by the authoring session (not requested).*

## Formulas

All arithmetic is on 32-bit integers (Rule 14). Nothing here uses floating point, RNG or time. Division is integer division and every dividend is non-negative (because `max(0, s + F) ≥ 0` and `1000 + P ≥ 0`), so it equals floor.

**Formula 1: effective speed.** `v = clamp((max(0, s + F) × (1000 + P) + 500) / 1000, 1, V_max)`.

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| `s` | int | `5..60` (provisional) | hero or monster definition (System 2) | Base speed. |
| `F` | int | sum of passive item bonuses, each `−10..10`, clamped to `−50..50` (provisional) | frozen arrangement (Systems 2, 15) | Flat speed modifier. `0` for an enemy. |
| `P` | int | sum clamped to `−1000..1000`; `0` in the MVP | statuses (System 13, later) | Percent modifier in permille, one additive bucket. |
| `V_max` | int | `200`, with `V_max < T` | constant | Largest effective speed. |
| `v` | int | `1..200` | calculated | Effective speed. |

*Examples:* `s = 10`, `F = 2`, `P = 0` gives `(12 × 1000 + 500) / 1000 = 12`; `s = 11`, `F = 0`, `P = −500` gives `(11 × 500 + 500) / 1000 = 6` (5.5 rounds half up); `s = 5`, `P = −1000` gives `(5 × 0 + 500) / 1000 = 0`, raised to the floor `1`; `s = 5`, `F = −10` gives `max(0, −5) = 0`, so `v = 1`; `s = 10`, `F = 2`, `P = 500` gives `(12 × 1500 + 500) / 1000 = 18`; `s = 60`, `F = 50`, `P = 1000` gives `(110 × 2000 + 500) / 1000 = 220`, capped at `200`.

**Formula 2: time until ready.** For a living unit with gauge `g < T` and speed `v`, `Δ_u = ⌈(T − g) / v⌉ = (T − g + v − 1) / v`. The time to advance is `Δ = 0` if some living unit has `g ≥ T`, otherwise `Δ = min Δ_u`.

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| `T` | int | `1000` | constant (`gauge_threshold`) | The threshold. |
| `g` | int | `0..G_max` | System 36 | Stored gauge. |
| `Δ` | int | `0..T` | calculated | Whole time steps advanced before the next action (at most `T`, reached only at `v = 1`). |

**Formula 3: advance and spend; the gauge bound.** After the advance, `g'_u = g_u + v_u × Δ` for every living unit, and for the acting unit `g'' = g' − T`. A unit that becomes ready in the advance has `g' ≤ T − 1 + v ≤ T + V_max − 1 = G_max`, and nothing advances while a unit is ready, so no stored gauge exceeds `G_max` (1199 with the defaults). The invariant `V_max < T` guarantees that a unit spending `T` at the bound is left with a gauge below the next threshold step, so a single time step never makes a unit ready twice.

**Formula 4: tie-break key.** The key `(−g, −v, side, slot)`, compared lexicographically and ascending, with `side = 0` for a hero and `1` for an enemy and `slot` the party slot or the encounter slot. The ready unit with the smallest key acts. The key is unique.

**Formula 5: proportionality.** With a constant speed `v`, a unit that starts at gauge `0` has acted `⌊v × t / T⌋` times after `t` time steps, and its stored gauge is `v × t − T × (actions)`. So a speed of 20 acts exactly twice as often as a speed of 10 over any long horizon, whatever the quantisation of the steps.

**Formula 6: schedule, forecast and global index.** `Schedule` repeats Formulas 2 to 4 `N` times on a private copy of the state, with the speeds fixed and no unit downed; `Forecast` is its first `K ≤ 20` entries; `GlobalIndex(hero, a)` is the number of the step at which hero `h` takes its `a`-th action, so it is at most `completedActions + N` and is `Beyond` past the 200th action. The cost is at most `200 × 9` unit updates, counted by a test seam (Acceptance Criteria).

**Formula 7: content lint.** `Σ_u s_u ≤ 40 × s_min` over the base speeds of the units of an encounter. *Examples:* `[5, 5, 5, 5, 60]` has `Σ = 80 ≤ 200`, so it passes; `[5, 60, 60, 60, 60, 60, 60, 60, 60]` has `Σ = 485 > 200`, so it fails, because the speed-5 unit would act about `200 × 5 / 485 ≈ 2` times.

**Worked example.** `T = 1000`. Tanker `s = 8`, Warrior `s = 12`, Support `s = 10` (slots 0, 1, 2), Goblin `s = 15` (enemy slot 0). No modifiers. All gauges start at `0`.

| Step | Needs `Δ_u` (Tanker, Warrior, Support, Goblin) | `Δ` | Gauges after the advance | Acts | Gauge after acting |
|---|---|---|---|---|---|
| 1 | 125, 84, 100, 67 | 67 | 536, 804, 670, 1005 | Goblin | 5 |
| 2 | 58, 17, 33, 67 | 17 | 672, 1008, 840, 260 | Warrior | 8 |
| 3 | 41, 83, 16, 50 | 16 | 800, 200, 1000, 500 | Support | 0 |
| 4 | 25, 67, 100, 34 | 25 | 1000, 500, 250, 875 | Tanker | 0 |
| 5 | 125, 42, 75, 9 | 9 | 72, 608, 340, 1010 | Goblin | 10 |

The order is Goblin, Warrior, Support, Tanker, Goblin. After 134 time steps the gauges gained `8 × 134 = 1072`, `12 × 134 = 1608`, `10 × 134 = 1340` and `15 × 134 = 2010`, so the Tanker, Warrior and Support acted once each and the Goblin twice (Formula 5), with stored gauges 72, 608, 340 and 10. The global index of the Warrior's first action is 2, the Support's 3, the Tanker's 4 and the Goblin's second action 5. **The build changes the order:** if the Support carries a passive item with `speedBonus = +6`, its speed is 16 from the start, it needs `⌈1000 / 16⌉ = 63` steps against the Goblin's 67, and it acts first (gauges after the advance: 504, 756, 1008 and 945), so the first action is no longer the Goblin's. A tie: a hero and an enemy that both have `s = 10` and gauge `0` both need 100 steps and both reach 1000; the key is equal up to `side`, so the hero acts first. *Provisional (no status defines it yet):* a Slow giving the Support `P = −500` at the start of step 3 (its gauge is 840) would make its speed 5 and its need `⌈160 / 5⌉ = 32`.

> *`systems-designer` not consulted by the authoring session (not requested); the review's findings on bounds and the lint are applied.*

## Edge Cases

- **If several units are ready at once**: `Next` returns the one with the smallest key (Formula 4). The others stay ready and act on later calls, each with `Δ = 0`, in key order recomputed from the stored gauges. **If they have unequal gauges**: the larger gauge acts first, whatever the speeds.
- **If a gauge exceeds the threshold when a unit acts**: the excess is kept (`g − T`), never lost (Rule 5).
- **If a unit reaches exactly `T`**: it is ready (`g ≥ T`).
- **If a hero and an enemy tie on gauge and speed**: the hero acts first (Rule 4); this is a small, documented advantage for the player.
- **If a unit is stunned**: its turn is `ActionSkipped(stunned)`; it spends `T`, keeps its slot, the order is unchanged, and its items lose that firing (Rule 5).
- **If a unit has nothing ready**: `ActionSkipped(no_ready_item)` likewise spends `T`.
- **If a contribution would make `v` below 1 or above 200**: the floor 1 and the cap 200 apply (Formula 1); `s + F` below zero counts as zero.
- **If speed changes while a unit waits** (later releases only): its stored gauge is kept and its remaining time is recomputed from the new speed; a gauge below `T` never becomes ready by a speed change alone.
- **If a unit is downed while ready and before it acts**: it is removed from `Next`, `Forecast`, `Schedule` and `GlobalIndex`; it does not act. The later indices of the others shift accordingly.
- **If a unit is downed during its own action**: the action completes (System 12, Rule 4); its gauge was spent when the action committed and it is not scheduled again.
- **If only one unit is alive**: `Next` returns it every time, after `⌈(T − g) / v⌉` steps; System 12 normally ends the encounter first.
- **If no unit is alive**: `Next` returns `NoLivingUnit`.
- **If a base speed or a contribution is out of range, a gauge is negative or above `G_max`, or two living units of one side share a slot**: `ContractViolation`; System 12 ends the encounter in a technical stop.
- **If an enemy joins during the fight** (a reinforcement): it enters with gauge `0` and the slot System 14 gives it (provisional). There are no reinforcements in the MVP (System 14, Rule 11), so this applies only when `Summon` is introduced.
- **If the arrangement is edited**: the passive speed bonuses are re-summed, effective speeds and the forecast are recomputed, and nothing is stored between edits. A speed item moved to the stash stops counting.
- **If a passive speed item sits in the stash or the tray**: it counts for nothing (System 8).
- **If the forecast is requested with more steps than units**: it simply continues the cycle; `K` is limited to 20 and the global index to the 200-action limit. **If `K = 1`**: one entry. **If `N` exceeds the actions left before the limit**: the list stops at the limit and the rest is `Beyond`.
- **If the 200th action is reached**: its index is 200; the 201st is `Beyond`.
- **If a pre-fight action runs**: no time passes and no gauge is spent (`Δ = 0`).
- **If the same encounter is run twice with the same inputs**: the sequence of actors, `Δ` values and gauges is identical (no RNG, no clock).

> *`systems-designer` not consulted by the authoring session (not requested).*

## Dependencies

| System | Direction | Hard/Soft | Contract | Back-reference status |
|---|---|---|---|---|
| 2 Game Data Definitions | Upstream | Hard | Base speed of heroes and monsters, range `5..60` (provisional); the `speedBonus` of passive items. | A row is added to System 2's Open Questions. |
| 36 Combat State and Step Model | Upstream | Hard | Stores the gauge (int32), speed contributions, living status, slots, completed actions and ordinals; applies `Δ` and the spend in the action transaction; derives `v` with `EffectiveSpeed`. | Updated. |
| 4 Event Log | Upstream (contract change) | Hard | `ActionStarted` and `ActionSkipped` carry `Δ` and the acting unit's gauge before and after. | Recorded in System 4's Open Questions. |
| 14 Enemy and Intent | Upstream | Hard | Encounter slot (the spawn ordinal equals the slot), monster base speed. No reinforcements in the MVP. System 14 is Designed ([enemy-and-intent.md](enemy-and-intent.md), pending review). | Present (System 14, Dependencies). |
| 15 Party and Heroes | Upstream | Hard | Party slot (the order of picking), base speed, passive speed bonuses. System 15 is Designed ([party-and-heroes.md](party-and-heroes.md), pending review). | Present (System 15, Dependencies). |
| 13 Status Effects | Upstream | Soft (provisional) | Later: speed modifiers `F` and `P` with sources and durations counted in the affected unit's own actions. | Updated. |
| 16 Encounter Generation | Downstream | Soft | Enforces the content lint (Rule 13) for every three-hero subset of the hero pool. | Designed ([encounter-generation.md](encounter-generation.md)). |
| 12 Combat Resolution | Downstream (caller) | Hard | Calls `Next`, commits the result, owns the 200-action limit and the outcome checks. | Updated. |
| 9 Item Trigger and Cooldown | Downstream | Hard | `GlobalIndex` and `Schedule` for the party-wide firing rank. | Present (System 9, Dependencies). |
| 25 Backpack UI | Downstream | Soft | Forecast, schedule and global index for the arrangement screen. | Not designed. |
| 27 Combat HUD | Downstream | Hard | Forecast for the order strip, real gauges for the speed gauge. | Not designed. |
| 35 Balance Simulation Harness | Downstream | Soft | Calls `Next` and `Schedule`. | Not designed. |
| 1 Seeded RNG | None | n/a | System 11 draws no RNG. | No change. |
| 3 Effect Resolution | None | n/a | `GaugeChange` stays deferred; no effect in the MVP changes a gauge. | No change. |

**Circular-dependency check:** System 11 reads Systems 2, 14 and 15 (and later 13, through 36) and never calls System 9, System 3 or System 12. System 12 and System 9 read System 11. The systems index row 11 lists the inputs 14 and 15 (and 13 as soft); System 12 is a caller and is not listed as a dependency of System 11, which would create a cycle.

## Tuning Knobs

| Knob | Default | Safe range | Affects | If too high | If too low |
|---|---|---|---|---|---|
| Gauge threshold `T` | 1000 | 300 to 10,000, with `V_max < T` | Resolution of the speed ratios and the size of `Δ`. | Larger `Δ` and gauges; no gameplay effect. | Coarse steps; below `V_max` a unit could become ready twice in one step. |
| Base speed `s` (heroes, monsters) | per unit, `5..60` (provisional) | 5 to 60 | How often a unit acts relative to others. | A unit acts so often that it dominates the 200-action limit. | A unit rarely acts (the content lint, Rule 13). |
| Passive item `speedBonus` | per item, `−10..10` (provisional) | −10 to 10 | How far a build can move the order. | One item reverses the order of two heroes. | Speed items are not felt. |
| Sum clamps | `F` ±50, `P` ±1000 permille | owned by Systems 2, 13, 15 | The largest total speed change. | A stack can reverse the order. | Stacks saturate early. |
| Content lint factor | 40 | 20 to 80 | How wide the speed spread of one encounter may be. | Slow units act rarely. | Encounters are forced to be uniform. |
| Forecast display horizon `K` | 6 | 1 to 20 | How far ahead the screen promises the order. | The forecast is long but more of it is wrong after any change. | The player cannot see the setup and payoff sequence. |

**Not knobs.** `V_max = 200` is a constant that the gauge bound depends on (Formula 3); changing it, or any rule below, bumps the rules version and regenerates the fixtures. The design rules are the tie-break order, the carry-over of the remainder, starting every gauge at `0`, no gauge pushback and the rule that a skipped turn keeps its slot. The 200-action limit belongs to System 12.

**Interactions.** `T`, `V_max` and the speed range together set the resolution and the gauge bound; the speed range and the item bonuses together set how far a build can move the order.

> *`systems-designer` not consulted by the authoring session (not requested).*

## Visual/Audio Requirements

- **Data, not drawing.** The look of the order is set by the art bible (7.4, 7.5). This system supplies the facts: the forecast list of units in order, the unit that acts now, and each unit's gauge and effective speed.
- **Order strip (art bible 7.5).** A row of 14 px square plain chips in the top strip: hero chips show the join numeral, enemy chips show the intent glyph, and the active chip carries the Candle chevron beneath it. It is shown in planning (the order the fight will play) and in combat, and it belongs to the protected layer. The number of chips is the forecast horizon `K` (default 6). **The projected chips must be visibly marked as a projection** (a treatment left to `/ux-design`), and the strip itself, not only its popup, says "if nothing changes".
- **Speed gauge (art bible 7.4).** The stat row shows a gauge about 20 px wide (Fog fill, Bone ticks) whose fill is the stored gauge over `T`. Between two action records the fill is drawn from the stored values; animating the fill between records is presentation and never changes the simulation.
- **A shifted order has a cause.** When a unit is downed or (later) a speed changes, the strip reorders and the chip that moved can be identified; the cause is named in the popup (UI Requirements).
- **Audio.** This system makes no sound.
- **Not covered by the art bible (Open Questions):** how the strip shows several ready units at once, a "=" marker for tied chips, what happens when the strip is shorter than `K`, the cue for a speed change and a one-time cue for a reinforcement.

> 📌 **Asset Spec** — Visual/Audio requirements are defined. After the art bible is approved, run `/asset-spec system:turn-order` to produce per-asset visual descriptions, dimensions and generation prompts from this section.

> *`art-director` not consulted by the authoring session (not requested).*

## Game Feel

- **The forecast is a promise with a label.** When its stated assumptions hold, the actors that follow are exactly the forecast, in order; there is no hidden roll and no frame-rate dependence.
- **Immediate planning feedback.** The forecast is recomputed in the same frame an edit commits, without allocation, so the strip changes as the plate lands (consistent with Systems 8, 9 and 10).
- **Ties are explained, not felt.** When two units tie, the popup can say why one acts first ("same gauge and speed: heroes act first"), so a tie never reads as luck. The speed bias of the key is likewise stated, not hidden.
- **No time of its own.** `Δ` has no presentation length; the next turn starts when the previous action's presentation ends (Systems 12 and 29). The speed control (1x, 2x, 4x, Skip) never changes the order.

## UI Requirements

- **Order strip:** the next `K` units as in Visual/Audio, marked as a projection and labelled "if nothing changes", and recomputed after every HP step that downs a unit and every edit of the arrangement.
- **Popup on a chip or an HP block (art bible 7.5):** "acts 2nd" and its causes: the effective speed with its sources ("speed 12: base 10, +2 from Ring"), and a tie-break line when it applies.
- **Arrangement screen:** the strip is visible in planning, so a change of item or hero changes the order the player sees. Each item can show "first fires at global action #N", built on `GlobalIndex` through System 9's firing rank; an item whose first firing is beyond the action limit is shown as such.
- **Text first:** the order is also available as a text list for keyboard and gamepad focus ("Order: Goblin, Warrior, Support, Tanker, Goblin").
- **Not by colour alone:** hero and enemy chips already differ by content (numeral against glyph).

> 📌 **UX Flag — Turn Order**: This system has UI requirements. In Phase 4 (Pre-Production), run `/ux-design` for the order strip, the arrangement screen and the combat HUD before writing epics. Stories that reference UI should cite `design/ux/[screen].md`, not the GDD directly.

## Cross-References

| Reference | What it provides here |
|---|---|
| [Game Concept](game-concept.md), Core Mechanics 2 and the risks | "Heroes and monsters fill speed gauges and act in order"; the float-gauge risk; the draft preview of 6 actions. |
| [Systems Index](systems-index.md), row 11 | Dependencies; "integer gauges, full tie-break, pure forecast function". |
| [Combat Resolution](combat-resolution.md), Core Rule 3 and the outcome table | What System 11 reports; "waiting for the gauge is not inability". |
| [Combat State](combat-state-and-step-model.md), Core Rule 1 and Formulas | The gauge is an int32; units belong to this system. |
| [Event Log](event-log.md), Combat Record Kinds | `ActionStarted` and `ActionSkipped`, which carry `Δ`. |
| [Item Trigger and Cooldown](item-trigger-and-cooldown.md), Rules 3 and 15 and Formula 5 | `GlobalIndex`; the action ordinal advances on skipped actions. |
| [Effect Resolution Model](effect-resolution-model.md), damage formula | The additive percent bucket and rounding reused for the future `P`. |
| [Status Effects](status-effects.md) | Future speed statuses and the stun status. |
| [Art Bible](../art/art-bible.md), 7.4, 7.5 and the open line on order | The order strip, the turn marker, the speed gauge. |
| Prototype `Assets/_Project/Scripts/Combat/SpeedGaugeManager.cs` (evidence only) | Float gauge 0..100 advanced by frame time, carry-over, tie-break by gauge, speed and side, stun pushback. A rewrite, not a port: the float arithmetic and pushback are not adopted. |

## Acceptance Criteria

**Fixtures.**
- `UNITS-4`: the worked example. `T = 1000`. Tanker `s = 8` (hero slot 0), Warrior `s = 12` (slot 1), Support `s = 10` (slot 2), Goblin `s = 15` (enemy slot 0). All gauges `0`. No modifiers.
- `UNITS-TIE`: one hero (slot 0) and one enemy (slot 0), both `s = 10`, gauges `0`. `UNITS-3TIE`: heroes at slots 1 and 0 and an enemy at slot 0, all `s = 10`, gauges `0`.
- `SPEED-TABLE`: 30 sets of base speeds, units alternating hero, enemy, hero, enemy… with slots counted per side: (1) 5,5; (2) 60,60; (3) 5,60; (4) 10,10; (5) 10,20; (6) 8,12,10,15; (7) 7,7,7; (8) 5,10,15; (9) 60,30,15; (10) 11,13,17,19; (11) 5,6,7,8,9; (12) 60,59,58; (13) 20,20,21; (14) 9×9; (15) 5,5,5,5,60; (16) 30,45,60,15,5; (17) 12,12,12,13; (18) 25,25,50; (19) 6,12,24,48; (20) 14,28,56; (21) 17,23,31,43,59; (22) 5,7,11,13,17,19,23,29,31; (23) 40×6; (24) 8,8,9,9,10,10; (25) 15,30; (26) 33,33,34; (27) 50,25,10,5; (28) 60,5,5; (29) 21,22,23,24; (30) 45,35,25,15,5.
- `STEPPER`: a test-only function `Apply(state, record)` that applies an action record exactly as System 36 does (every living gauge `+= v × Δ`, then the actor's `−= T`) and rejects a record whose stated gauges do not match. It is the specification of Rule 6.
- `REF-SCHEDULER`: a reference scheduler written in the test from the text of Rules 3 and 4 (not from `Next`): it advances every gauge by its speed one time step at a time until some gauge reaches `T`, then applies the tie-break key as written in Rule 4. It shares no code with `Next`.
- Logic tests live under `Assets/_Project/Tests/EditMode/TurnOrder/`, in the existing `Project05.EditModeTests` assembly. The engine-free core assembly is new; its name and settings are fixed by an ADR (NOT SOURCEABLE).
- Tests of the units of System 12, 4 and 36 are theirs; here System 11 is tested through `STEPPER`.

1. **Effective speed (Formula 1).** **Given** `(s, F, P) = (10, 2, 0), (11, 0, −500), (5, 0, −1000), (5, −10, 0), (10, 2, 500), (60, 50, 1000)`, **then** `v = 12, 6, 1, 1, 18, 200`.
2. **Gauge bound (Formula 3).** **Given** `UNITS-4` and every set of `SPEED-TABLE`, **then** all stored gauges stay within `0..1199 = G_max`, and `V_max < T` holds.
3. **Purity (Rule 3).** **Given** any fixture, **when** `Next`, `Forecast`, `Schedule` and `GlobalIndex` run, **then** the state is value-equal before and after (a snapshot comparison).
4. **Worked example (Rule 3, Formulas).** **Given** `UNITS-4`, **when** driven by `Next` and `STEPPER`, **then** the five steps give `Δ = 67, 17, 16, 25, 9`, the actors Goblin, Warrior, Support, Tanker, Goblin, and the gauges after acting `5, 8, 0, 0, 10`.
5. **Oracle (Rules 3 and 4).** **Given** `UNITS-4` and each set of `SPEED-TABLE`, **then** `Next` returns the same actor and the same `Δ` as `REF-SCHEDULER` for 200 consecutive actions. *Watched failure:* the test must be seen to fail against a `Next` with a reversed tie-break before it is trusted.
6. **Tie-break (Rule 4).** **Given** `UNITS-TIE`, **then** the hero acts first; **given** `UNITS-3TIE`, **then** the order is hero slot 0, hero slot 1, enemy slot 0; **given** two ready units with unequal gauges, **then** the larger gauge acts first whatever the speeds; **given** equal gauges and different speeds, **then** the faster acts first; and the key is unique in every fixture.
7. **Acting (Rule 5).** **Given** a unit that acts, **when** the record is applied with the reason `fired`, `stunned` or `no_ready_item`, **then** its gauge falls by exactly `T`, the excess is kept, and the order of the others is the same for all three reasons.
8. **Apply (Rule 6).** **Given** an action record, **then** `STEPPER` raises every living gauge by `v × Δ` and then lowers the actor's by `T`; **given** a record whose gauge-before or gauge-after does not match, **then** it is rejected with `ContractViolation`; **given** a pre-fight record (`Δ = 0`, no spend), **then** no gauge changes.
9. **Speed contract (Rule 7, provisional).** **Given** `UNITS-4` after step 2 and a `P = −500` contribution on the Support, **then** its `v` is 5, the next `Δ` is 32, no stored gauge changes at the moment of the modifier, and after the step the gauges are `928, 392, 1000, 740`. *[BLOCKED] until a status defines a speed change.*
10. **Downed units (Rule 8).** **Given** a unit downed while ready, **then** it is absent from `Next`, `Forecast`, `Schedule` and `GlobalIndex`; **given** a unit downed during its own action, **then** it is not scheduled again.
11. **Result codes (Rule 9).** **Given** no living unit, **then** `NoLivingUnit`; **given** a base speed of `4` or `61`, a gauge of `−1` or `1200`, or two living heroes in one slot, **then** `ContractViolation`; **given** any fixture with a living unit, **then** `Next` returns `Ok`.
12. **Initial state (Rule 10).** **Given** a new encounter, **then** all gauges are `0` and the first actor is decided by speed and the key only.
13. **Forecast and schedule (Rule 11).** **Given** `UNITS-4`, **then** `Forecast` with `K = 5` equals the actual sequence of a run (Goblin, Warrior, Support, Tanker, Goblin) and `K = 1` gives one entry; **given** `K = 0` or `K = 21`, **then** `ContractViolation`; **given** a buffer one entry too small, **then** `BufferTooSmall` with no partial output; **given** `Schedule` with `N = 200`, **then** it equals 200 repeated `Next` calls.
14. **Proportionality (Formula 5).** **Given** units with `s = 20` (hero) and `s = 10` (enemy), **when** driven until time step 3000, **then** at that step both are ready, two further `Next` calls return them in key order (hero first), and afterwards they have acted exactly 60 and 30 times and both stored gauges are `0`.
15. **Global index (Rule 12).** **Given** `UNITS-4` and the supplied counts, **then** `GlobalIndex` is 2 for the Warrior's first action, 3 for the Support's, 4 for the Tanker's and 5 for the Goblin's second; **given** the Warrior skipped on its first action, **then** all indices are unchanged; **given** the 200th action, **then** index 200, and the 201st is `Beyond`; **given** a downed unit, **then** the later indices shift.
16. **Content lint (Rule 13, Formula 7).** **Given** `[5, 5, 5, 5, 60]`, **then** it passes; **given** `[5, 60, 60, 60, 60, 60, 60, 60, 60]`, **then** it fails.
17. **Passive speed bonus (Rule 1).** **Given** a hero with `s = 10` carrying passive items `+2` and `+3` in its grid, **then** `F = 5` and `v = 15`; **given** one of them in the stash, **then** `F = 2`; **given** the arrangement edited, **then** the forecast changes; **given** the encounter started, **then** `v` does not change.
18. **Reinforcement (provisional).** **Given** an enemy that joins mid-fight with gauge `0` and a slot from System 14, **then** it enters the order from the next `Next` call. *[DEFERRED] There are no reinforcements in the MVP (System 14, Rule 11); revisit when `Summon` is introduced.*
19. **Determinism (Rule 14).** **Given** the same inputs, **then** two runs produce identical sequences of actors, `Δ` values and gauges.
20. **Engine-free (Rule 14).** **Given** the core assembly, **then** its definition sets `noEngineReferences`, and a CI scan (a banned-symbol analyzer or an IL scan) finds no `UnityEngine` reference, no mutable `static` field, no floating-point type and no `DateTime` or `Random`.
21. **Allocation.** **Given** warmed-up `Next`, `Forecast` and `Schedule` with caller buffers, **when** each is called 1,000 times, **then** the managed allocation delta is zero, measured with a control that does allocate.
22. **Operation budget (Formula 6).** **Given** 9 living units, **then** `Forecast(…, 20)` performs at most `20 × 9` gauge updates, and `Schedule` or `GlobalIndex` over 200 steps at most `200 × 9`, counted by a test seam; any excess fails. The elapsed time is recorded for information only and is not asserted.
23. **Integration with Systems 4, 12 and 36.** **Given** the spike fixture, **then** the trace shows the action records with `Δ` and the gauges, the atomic transaction rolls back when a failure is injected, and the order strip equals the forecast throughout a run in which nobody is downed. *[BLOCKED] until those systems are revised.*
24. **Integration with System 9.** **Given** System 9's fixture of two heroes, **then** replacing its stub `G` with the real `GlobalIndex` gives the ranks of its criterion 31. *[BLOCKED] until System 9 is implemented.*
25. **Integration with System 13.** **Given** a speed status and its expiry, **then** the effective speed returns to its base value. *[BLOCKED] until System 13 defines speed statuses.*
26. **UI evidence.** **Given** the arrangement screen and the combat HUD, **then** retained screenshots in `production/qa/evidence/` show the order strip in planning with its "if nothing changes" label, the strip after a unit is downed, a popup with "acts 2nd" and the speed sources, and the tie line. *[BLOCKED] until the UI exists.*

> *`qa-lead` not consulted by the authoring session (not requested); the review's QA findings are applied.*

## Open Questions

| Question | Owner | Resolve by |
|---|---|---|
| System 4 must let `ActionStarted` and `ActionSkipped` carry `Δ` and the actor's gauge before and after. | System 4, System 11 | Before System 4 is re-reviewed. |
| System 36 must store the gauge, the speed contributions, the completed-action count and the ordinals, derive `v` with `EffectiveSpeed`, and apply `Δ` and the spend in the action transaction. | System 36 | Before System 36 is re-reviewed. |
| System 12 must call `Next`, commit the result as one transaction, and treat its outcome row 5 ("no actor can act again") as a guard that cannot occur in the MVP. | System 12 | Before System 12 is re-reviewed. |
| System 2 must supply base speed in `5..60`, the `speedBonus` of passive items and the validation of the ranges. (System 15 has defined the party slot, the order of picking, and passes the base speed and the sum `F`.) | System 2 | Next System 2 revision. |
| System 13: no status changes speed in the MVP. If one is added, it must supply `F` and `P` with durations counted in the affected unit's own actions, and keep the stun status that skips a turn without moving the order. | System 13 | Before speed statuses are introduced. |
| ~~Systems 14 and 16 must define encounter slots and the reinforcement rule.~~ Answered by System 14 (2026-11-13): slots `0..5` in spawn order with the ordinal equal to the slot; no reinforcements in the MVP. System 16 must still enforce the content lint of Rule 13. | System 16 | When System 16 is designed. |
| System 9 must rank an item whose first firing is `Beyond` the action limit (it has no key and is shown as beyond the limit). | System 9 | Applied at sync. |
| Presentation not covered by the art bible: the strip with several ready units, a "=" marker for tied chips, a strip shorter than `K`, the cue for a speed change and for a reinforcement, and how the advance `Δ` is animated. | art-director | Art bible follow-up. |
| Balance: is `T = 1000` with speeds `5..60` fine enough; is the hero advantage on exact ties and the bias to fast units acceptable; how many hero actions does a typical fight have against the 200 limit? | systems-designer, System 35 | First balance simulation and the combat spike. |
| Is a default horizon of 6 enough for the setup and payoff sequence? (The concept's draft was 6.) | UX designer, playtests | `/ux-design` and the first playtests. |
| ADR: the core assembly settings, the language level, and the type of the read-only unit snapshot. | Technical director | Before the combat spike implementation. |
