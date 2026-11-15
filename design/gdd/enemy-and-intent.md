# Enemy and Intent

> **Status**: Designed (pending independent /design-review)
> **Creative Director Review (CD-GDD-ALIGN)**: REVISED 2026-11-13 — the gate returned CONCERNS; the three required changes were applied (owners and tests for the answerability and ignorable-intent guards, the "before shield" label, and the wrap statement in the popup). Not assessed by the gate: the art bible sections 5.10.8, 7.4a, 7.5 and 7.8 (not among its inputs).
> **Author**: user + Claude design session, with specialist consultation
> **Last Updated**: 2026-11-13
> **Last Verified**: 2026-11-13
> **Implements Pillar**: Arrangement Is the Thinking (primary); The Payoff Is the Product (cause must be readable)

## Summary

Enemy and Intent defines the enemies and fixes what each will do before the fight: when the player enters a room, every enemy gets a bound sequence of moves and targets drawn from the seeded `intents` stream, which the intent tab, the order strip and a popup show, and which nothing in combat can change. It exists so that the enemy is a question asked in advance and the arrangement is the player's answer, and so that a loss can be traced to a named intent (Pillars 4 and 2).

> **Quick reference** — Layer: `Feature` · Priority: `MVP` · Key deps: `Game Data Definitions, Seeded RNG, Effect Resolution Model, Combat State and Step Model`

## Overview

System 14 defines what enemies are and what they will do. A monster is a named enemy with HP, a speed and an ordered set of moves; a move is an ordered list of damage hits with a target mode, or no hits at all (a windup). When the player enters a room, before the arrangement phase begins, System 14 draws for every enemy a fixed **sequence of intents**: for each of its next actions, which move it will use and whom it will hit, drawn from the seeded `intents` stream. From then on the sequence is binding. Nothing in the arrangement, in the party's HP or in the course of the fight can change it, and nothing is drawn during combat: when an enemy acts it simply takes the next entry. The enemy shows the first entry on its intent tab (a glyph, the damage number and the target), the order strip shows the glyph of each upcoming action, and a popup lists the whole sequence in words, so the player can plan the arrangement against it. Enemies never choose a new target when a hero falls: a hit on a downed hero is skipped. Difficulty is a pair of integer multipliers on HP and damage that System 16 sets for each encounter. System 14 never mutates combat state itself: System 12 commits each enemy action through System 36, and System 3 resolves its hits. Without this system the enemy would be an unreadable random force; with it, the enemy is a question asked in advance and the arrangement is the answer (Pillars 4 and 2).

> *Specialists consulted for this section: game-designer, ai-programmer, systems-designer, art-director, creative-director (2026-11-13). Their findings are applied below.*

## Player Fantasy

The player enters a room and reads what the enemies will do: the goblin's heavy swing at the Ranger on its second action, the cleaver that will sweep the whole party on its fourth. They answer with an arrangement, moving a speed item so the Crusader acts before the swing, or spreading max-HP items so the weakest hero survives the sweep. **The enemy shows its hand before the fight and keeps its word. The player answers with an arrangement, and if they lose, they can name the intent that beat them.** The threat is known, but knowing it does not mean the current items can answer it; the tension lives in whether the build covers what is coming, not in hidden information. This serves the Core Fantasy (the architect of a perfect machine), Expression and Challenge at the moment the player commits, and the concept's promise that "losing should be educational".

*Pillar anchors.* Pillar 4, Arrangement Is the Thinking: "Everything the player needs to decide (item effects, turn order, enemy intent) is visible before the fight starts." Design test: "Debating an extra in-combat action vs a deeper pre-combat arrangement, we choose pre-combat." The player answers intent only through arrangement, never through a button, which respects the anti-pillar against active in-combat actions. Pillar 2, The Payoff Is the Product: "the player can always see which arrangement caused them"; intent supplies the cause on the enemy's side and is recorded for the breakdown.

*Two promises kept apart.* **What** an enemy will do is binding and never changes after the player commits. **When** it acts follows the turn-order forecast, which is labelled "if nothing changes" and shifts when a unit falls. The design never presents the timing as a promise.

*Risks the design guards against* (what would turn the puzzle into luck): vague intent that hides the target or size; a hidden roll after commitment; an intent that changes mid-fight; an intent missing from the log and the breakdown; a threat no item in the pool can answer; and an intent the player can ignore without cost.

> *`creative-director` consulted (2026-11-13); the spine of the fantasy is theirs.*

## Detailed Design

### Core Rules

1. **Monster and move definitions.** A monster (System 2) has a stable ID, a name key, a classification (`regular`, `elite`, `boss`), an art size class (`small`, `medium`, `large`, `boss`), a base max HP `1..100,000`, a base speed `5..60`, an ordered list of one to eight moves (at least one with `n ≥ 1`), an optional opener move ID and, for a boss, a phase table: contiguous ranges of entry indices, the last open-ended, each naming the moves available in it. A move has an ID unique within its monster, a name key, a glyph (`attack` or `windup`), a hit count `n` in `0..8`, a base damage `B` in `1..100,000` (a windup, `n = 0`, has no `B`), a target mode (optional for a windup), a weight `0..100`, a `heavy` flag and an optional `linkedNext` move ID. The MVP vocabulary is damage only: a strike (`n = 1`), a multi-hit (`n ≥ 2`, one target), a sweep (`n = 1`, all heroes) and a windup (`n = 0`). Moves with shields, speed changes, statuses or detonation are not in the MVP.
2. **Encounter instance.** System 16 supplies the roster of an encounter: monster IDs, the multipliers `hpPermille` and `damagePermille` and encounter slots `0..5` in spawn order (at most six enemies, the art bible's two lines of three). Slots carry no mechanical meaning. The roster must have one to six enemies with distinct slots; an empty roster, more than six enemies or a repeated slot is a `ContractViolation` at binding, with no draw. Two enemies of the same monster are legal and get different sequences. An enemy's spawn ordinal equals its slot, and draws run in ascending slot. The scaled HP and damage (Formulas 1 and 2) are computed and baked into the encounter instance before anything is bound.
3. **When intents are bound.** When the player enters a room, before the arrangement phase and before any preview, never earlier (not when doors are generated).
4. **The sequence.** For each enemy, in ascending slot, System 14 builds `H` entries (default `H = 12`), each a move and a target, in index order. Each entry uses exactly two draw calls on the `intents` stream, a move draw then a target draw, even when the move is forced or its target needs no choice (a draw with bound 1: a forced entry, an `AllHeroes` target, a windup without a target mode, or a `SameAsMove` that resolves; `RandomHero` and a `SameAsMove` that does not resolve draw with the party size as the bound), so the number of draw calls depends only on the number of enemies and `H` (the words System 1 rejects and consumes are deterministic given the state).
5. **Move choice.** Entry 0 is the enemy's authored opener when it has one. Every other entry is a weighted choice over the move list in authored order, with weights set to zero by these rules: a move cannot be chosen a third time in a row; a `heavy` move cannot be chosen when a heavy move was chosen in either of the previous `HeavyGap` (2) entries; if the previous entry's move has a `linkedNext`, that move is chosen, overriding every other rule (a link points to an attack move, never to another windup, and a move with a link is not chosen when its chain would run past the last entry). If every weight is zero, the rules are relaxed in this order, drawing as usual as soon as the total is above zero: first drop the repeat limit, then drop `HeavyGap`, and only then take the first move whose authored weight is above zero with a bound-1 draw. The link rule is never relaxed. The constraints apply within the sequence only, not across the wrap: after entry `H − 1` the sequence repeats from entry 0, including the opener. A boss's phase table says which moves are available for each range of entry indices; phases are keyed to entry index, never to HP, because the sequence is bound before combat, and after the wrap a boss returns to its first phase.
6. **Target modes.** `RandomHero`: uniform over the heroes alive at binding, listed by party slot (all heroes in the MVP, because heroes are restored before every encounter). `AllHeroes`: every hero, no draw result used. `SameAsMove(M)`: the target of this enemy's most recent earlier entry that used move `M` (a move ID, because a move definition cannot know entry positions); `M` must have a single target and must be able to occur earlier in the sequence (System 2 validates; the opener cannot refer across the wrap). If `M` has not yet occurred in this sequence when the entry is built, the target is drawn like `RandomHero`. A windup may carry a target mode, so that a later move can reuse its target and the tab can show whom the coming heavy hit is aimed at. A multi-hit move strikes one target with every hit. The target is stored as a hero instance ID.
7. **No draws after binding.** Once bound, nothing is drawn, and the stored sequence has no operation that changes an entry. Neither the arrangement, nor the party's HP, nor the course of the fight can change an entry. Previews and forecasts read the stored sequence and take no RNG input; a preview before binding is a call-order `ContractViolation`, never a draw. Binding is atomic: every enemy of the room is bound against a clone of the `intents` state, and the sequences and the new stream state are committed together; a failure discards the clone, so the live stream is unchanged and a retry gives the same result.
8. **Storage.** Before combat, the sequences and the `intents` stream state belong to System 5's encounter-preparation record. At Start, System 12 copies the sequences into System 36's baseline and System 36 owns them from then on. For each enemy System 36 stores the enemy instance ID, the slot, the action ordinal `e` (initially 0; it advances on every scheduled action, skipped or not), the intent index `n` (the next unused entry) and the sequence. System 4 publishes one `IntentsBound` record per enemy with its whole sequence. A missing, short or inconsistent record at Start (a wrong length, an unknown move ID, a target that is not in the party) is a technical stop; the sequence is never drawn again lazily.
9. **An enemy action.** System 11 yields the actor. System 12 asks System 13 whether it is stunned. If it is, `ActionSkipped(stunned)` commits with `e + 1` and the gauge spend, and **the bound entry stays**: `n` does not advance. Otherwise one System 36 transaction commits `ActionStarted(EnemyMove{enemy instance, monster definition, move ID, occurrence})`, `e + 1`, the gauge spend and `n + 1` (wrapping at `H`), and System 14 hands the move's hits to System 3 as one root action in authored order; all the hits are root effects, so they enter the queue before any reaction, and a downed source does not cancel them (System 12, Rule 4). A hit whose target is downed is skipped (`EffectSkipped(target_downed)`); there is no retarget and no draw. A windup is a completed action with an empty effect list.
10. **What the player sees.** The intent tab shows the entry at `n`: the glyph, the resolved damage `r` (Formula 3; `r×h` for `h` hits) and the target chip. The order strip shows the glyph of each upcoming enemy action from the turn-order forecast, and a popup lists the whole sequence in words. Everything reads stored data and is labelled "if nothing changes" where it depends on the forecast.
11. **No reinforcements in the MVP** (`Summon` is deferred). **Enemies have no shield and no healing in the MVP.**
12. **Identity.** The enemy instance ID is `(encounterId, spawn ordinal)`; the action occurrence ID is `(encounterId, enemyInstanceId, e)` with `e` the value after the increment of Rule 8, so the first scheduled action has `e = 1`. The spawn ordinal equals the slot. No hashes and no Unity instance IDs.
13. **Determinism and engine-free.** Integer arithmetic only, no clock, no mutable static state (the same rules as Systems 3 and 11).

### States and Transitions

| Subject | State | Trigger | Result |
|---|---|---|---|
| Enemy | Unbound | Room entered; sequences drawn | Bound, `n = 0`. |
| Enemy | Bound | Scheduled action, not stunned | The entry at `n` is used; `n + 1` (wraps at `H`). |
| Enemy | Bound | Scheduled action, stunned | The entry stays; `e + 1`. |
| Enemy | Any | HP step to zero | Removed; its remaining sequence is unused. |
| Encounter | Prepared | Start | Sequences copied to System 36; no draws afterwards. |

### Interactions with Other Systems

| System | Data in | Data out | Interface owner |
|---|---|---|---|
| 2 Game Data Definitions | Monster and move definitions | None | System 2 owns fields and validation. |
| 1 Seeded RNG | `intents` stream, weighted and bounded draws | None | System 1 owns the stream; System 14 owns the call order. |
| 16 Encounter Generation | Roster, `hpPermille`, `damagePermille`, slots | None | System 16. |
| 5 Run State and Flow | Encounter-preparation record with the RNG state | The bound sequences | System 5 stores them before combat. |
| 36 Combat State | Receives the baseline sequences | The bound action for System 12 | System 36 owns state from Start. |
| 12 Combat Resolution | Calls `GetBoundAction` | The next action to commit | System 12 owns the loop. |
| 3 Effect Resolution | The move's hits, in authored order | None | System 3 resolves. |
| 4 Event Log | None | `IntentsBound`; the `EnemyMove` origin | System 4. |
| 13 Status Effects | Stun | None | System 13. |
| 15 Party and Heroes | Hero instance IDs and slot order for targets | None | System 15. |
| 11 Turn Order | Encounter slot, base speed | The glyph per upcoming action | System 11. |
| 27, 29, 30 | None | Intent tab, strip and breakdown data | Those systems draw. |

*Provisional:* an empty effect list for a windup, the `IntentsBound` record and the baseline copy are assumptions about Systems 2, 3, 4, 5 and 36.

> *Specialists consulted: game-designer, ai-programmer, systems-designer, art-director (2026-11-13).*

## Formulas

All arithmetic is on 32-bit integers except the design-time estimates of Formulas 7 and 8 (Formula 7 uses a 64-bit intermediate in tooling). Division is integer division with non-negative dividends. Nothing here uses floating point in the simulation.

**Formula 1: scaled HP.** `ScaledHP = clamp((HP × m_hp + 500) / 1000, 1, 1,000,000)`.

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| `HP` | int | `1..100,000` | monster definition (System 2) | Base max HP. |
| `m_hp` | int | `500..10,000` permille (`1000` = identity) | System 16 | HP multiplier of the encounter. Outside the range is a `ContractViolation`. |

**Formula 2: scaled damage.** `ScaledB = clamp((B × m_dmg + 500) / 1000, 1, 100,000)`.

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| `B` | int | `1..100,000` | move definition (System 2) | Base damage of one hit. |
| `m_dmg` | int | `500..5000` permille | System 16 | Damage multiplier of the encounter. Outside the range is a `ContractViolation`. |

The floor of 1 keeps a hit from becoming a no-op (System 3 emits no event for zero damage). Speed, hit count, status amounts and the number of enemies are never scaled by these multipliers. *Examples:* `B = 11`, `m_dmg = 1150` gives `(12,650 + 500) / 1000 = 13`; `HP = 64`, `m_hp = 1300` gives `(83,200 + 500) / 1000 = 83`; `B = 1`, `m_dmg = 500` gives `(500 + 500) / 1000 = 1`.

**Formula 3: displayed damage.** `r = clamp((max(0, ScaledB + F_e) × (1000 + P_e) + 500) / 1000, 0, Cap)`, shown as `r` for one hit and `r×h` for `h` hits. It is System 3's damage formula applied by the same pure function that resolves the hit, with `F_e` and `P_e` the enemy-side modifiers known now (both `0` in the MVP).

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| `F_e`, `P_e` | int | `F_e` ±100,000; `P_e` clamped to `−1000..1000` | none in the MVP (future enemy-side statuses) | Enemy damage modifiers. With these bounds the product is at most `200,000 × 2000 = 4 × 10^8`, so 32-bit arithmetic suffices. |
| `Cap` | int | `2,147,483,647` | System 3 (`damage_cap`) | Largest damage of one effect. |
| `r` | int | `0..Cap`; `1..100,000` in the MVP | calculated | Resolved damage of one hit, before shield and HP. |
| `h` | int | `1..8` | move definition | Number of hits. |

The number shown is the resolved damage **before** the hero's shield and HP; it is not the HP the hero will lose. Each hit is its own effect with its own rounding, so the label is `r×h`, never a rounded total. *Example (future modifier):* `ScaledB = 4`, `h = 3`, `P_e = +250` gives `r = (4 × 1250 + 500) / 1000 = 5`, shown `5×3`, a total of 15 before shield.

**Formula 4: move choice.** For entry `i`, let `w_j` be the weight of move `j` after the zeroing rules of Rule 5 and `W = Σ w_j`. If `W ≥ 1`, draw `t` in `[0, W)` with System 1's weighted choice, in the authored move order, and choose the first move whose cumulative weight exceeds `t`. System 14 supplies the ordered candidates and their weights; System 1 owns the mapping from the ticket to the candidate. If `W = 0`, relax the rules in the order of Rule 5 and, if the total is still zero, take the first move whose authored weight is above zero, with a bound-1 draw (System 1 rejects a weighted choice with a zero total). A forced entry (an opener or a linked move) also makes one draw with bound 1.

**Formula 5: draws per encounter.** `draws = 2 × H × E` calls on `intents`, with `H` entries per enemy and `E` enemies (rejected words add to the consumed words but not to the calls). *Example:* `H = 12`, `E = 3` gives 72 calls.

**Formula 6: intent index.** For an enemy whose first unused entry is `n_0`, after `k` completed non-stunned actions the first unused entry is `(n_0 + k) mod H`. A stunned action leaves it unchanged.

**Formula 7: advisory threat (design time).** `threat = ⌈HP × N_D × v / (1000 × W_m)⌉`, where `N_D = Σ_j w_j × n_j × B_j × t_j` over the monster's moves, `W_m = Σ_j w_j`, `t_j` the number of heroes struck by move `j` (the party size for an all-heroes move, else 1), all with the scaled values, and `v` the base speed. It is the monster's expected damage per action times its HP times its speed, used by System 16 to compare monsters (a windup contributes `n = 0`). *Examples:* HP 64, strike (`n = 1`, `B = 11`, weight 60) and a sweep (`n = 1`, `B = 6`, weight 40) with a party of 3 and `v = 18`: `N_D = 660 + 720 = 1380`, `W_m = 100`, `threat = ⌈64 × 1380 × 18 / 100,000⌉ = ⌈15.9⌉ = 16`; the same monster with a windup of weight 20 added and no sweep: `N_D = 660`, `W_m = 80`, `threat = ⌈64 × 660 × 18 / 80,000⌉ = ⌈9.5⌉ = 10`.

**Formula 8: expected load (design time, not simulated).** `φ = Σ_e (a_e × L × B̄_e) / Σ maxHP_heroes`, with `a_e = A × v_e / v̄` the actions of enemy `e` over a fight in which each hero acts `A` times, `v̄` the mean hero speed and `L` in `0.5..1` the share of the fight an enemy is alive. *Example:* two regular monsters (`v = 20`, `B̄ = 8`), `A = 10`, `v̄ = 25`, `L = 0.6`: `a = 8`, damage `2 × 8 × 0.6 × 8 = 77`, against a pool of 284 HP, so `φ ≈ 27%`. This is an estimate assuming `A = 10` hero actions per fight (an unverified assumption of the specialists), to be replaced by System 35's measurements. The provisional design target for a first-room fight is `φ` between 20% and 30%; the pass band belongs to System 35.

**Worked example (a sequence, `H = 12`).** A monster has the opener Strike (`B = 8`, weight 60), a Windup (`n = 0`, weight 20, `linkedNext` = Heavy) and Heavy (`B = 14`, weight 0, heavy, only reachable through the link). Entry 0 is the opener: Strike. Entry 1: `W = 80`, the draw `t = 75` falls past Strike's cumulative 60, so Windup. Entry 2 is forced by the link: Heavy. Entries 3 and 4 draw `t = 10` and `t = 5`: Strike and Strike. Entry 5: Strike would be the third in a row, so its weight is zeroed, `W = 20` and the draw `t = 7` gives Windup; entry 6 is Heavy by the link. Entry 7 (`t = 59`): Strike. Entry 8 (`t = 60`): Windup, and entry 9 is Heavy. Entry 10 (`t = 0`): Strike. Entry 11: the Windup's chain would run past the last entry, so its weight is zeroed, `W = 60` and `t = 40` gives Strike. The sequence is Strike, Windup, Heavy, Strike, Strike, Windup, Heavy, Strike, Windup, Heavy, Strike, Strike. With `H = 6` the same draws give the same first five entries, and entry 5 is Strike, because a Windup there would have no room for its Heavy.

> *`systems-designer` consulted (2026-11-13); balance numbers are provisional until System 35 measures them.*

## Edge Cases

**Targets and downed units**
- **If a single hit's target is already down when the effect is taken**: `EffectSkipped(target_downed)` (System 3). The action still counts; `e` and `n` have already advanced; there is no retarget and no draw.
- **If the first hit of a multi-hit downs the target**: every later hit publishes its own `EffectSkipped(target_downed)` and counts toward System 3's counter `k`; there is no overkill carry-over. A `ChainLimit(count)` could cut later hits, so `r×h` is not a promise under chain pressure; the log shows the `ChainLimit`.
- **If a sweep finds a hero already down**: the sweep is one effect resolved over the living heroes at the moment it is taken, so a downed hero is excluded. If no hero is living, one `EffectSkipped(target_downed)` is published.
- **If the last hero falls in the middle of a sweep or a multi-hit**: the queue drains, the remaining hits are skipped, then System 12's outcome table runs. A reaction that downs the last enemy in the same action gives a draw (System 12, outcome row 1).
- **If the enemy is downed during its own action by a reaction**: its queued hits still resolve, because the action's queue drains before the outcome is checked (System 12, Rule 4).
- **If an enemy is downed while it has a bound sequence**: it leaves the scheduler, the forecast and the order strip; its `IntentsBound` record stays in the log; nothing is drawn again and the RNG is not rewound.

**Stun and the action limit**
- **If an enemy is stunned**: `ActionSkipped(stunned)`, `e + 1`, the gauge is spent, `n` is unchanged. A stunned windup stays pending. No MVP status stuns an enemy (System 13's MVP roster is Ấn only), so this path is tested with a synthetic status.
- **If windups and stuns fill the 200-action limit**: whether a skipped action counts toward the limit is System 12's decision (Open Questions); if it does, the result is a defeat by action limit. Whether such a fight is winnable is System 16's concern, not an engine error.

**Definition validation (System 2, before any draw)**
- **If a monster has a single move**: legal; it repeats (the relaxation of Rule 5 drops the repeat limit).
- **If every weight is zero and there is no opener, or no move with `n ≥ 1` is reachable**: System 2 rejects the monster.
- **If `linkedNext` is a self-link, a loop, an unknown ID, another monster's ID or a windup**: System 2 rejects. A link overrides the repeat limit, `HeavyGap` and phase availability.
- **If a move with a link is near the end of the sequence and its chain would run past entry `H − 1`**: its weight is zero for that entry, so a chain is never cut by the wrap.
- **If `SameAsMove(M)` refers to a move that cannot occur earlier, to an `AllHeroes` move, or to a windup with no target mode**: System 2 rejects. Chains of `SameAsMove` resolve transitively; the opener cannot refer across the wrap.
- **If a boss phase table has a gap, an overlap, a range above `H − 1`, no phase for index 0, a phase naming an unknown move, or an opener outside the first phase**: System 2 rejects.

**Binding and roster**
- **If the roster has zero or more than six enemies, a repeated or out-of-range slot, or the party is empty**: `ContractViolation` at binding, with no draw.
- **If slots have gaps or the roster is listed in another order**: draws run in ascending slot and the spawn ordinal equals the slot; two enemies of one monster get different sequences.
- **If the sequence wraps**: the repeat limit, `HeavyGap` and links are not enforced across the wrap, and the sequence repeats from entry 0, including the opener; a boss returns to its first phase.
- **If a multiplier is out of range**: `ContractViolation`. Inside the range the clamps of Formulas 1 and 2 cannot be reached (`100,000 × 10,000 / 1000` is the cap), so they are defensive only. With `B = 1` and `m_dmg = 500` the result is 1.

**Missing, failed and restored data**
- **If the intents record is missing at Start, has the wrong length, names an unknown move or targets a hero not in the party**: a technical stop with no combat and no reward; the sequence is never drawn again lazily.
- **If a failure occurs between a draw and its commit**: binding is atomic (Rule 7); the clone is discarded, the live stream is unchanged and a retry reproduces the same result.
- **If a preview is requested repeatedly**: it reads stored data, with no RNG and no clone; before binding it is a call-order `ContractViolation`.
- **If a snapshot is taken at the binding boundary**: the room's binding is one operation, never half an enemy. Restoring before binding and binding again gives identical sequences; restoring after uses the stored sequences ("bound" follows from the presence of the record). Whether a retry after a defeat or a quit during arrangement exists is System 5's decision; if it does, it reuses the stored record.

> *`systems-designer` consulted (2026-11-13); the corrections it found (the zero-weight fallback, `SameAsMove`, the `P_e` bound, the draw-order key) are applied in the rules and formulas above.*

## Dependencies

| System | Direction | Hard/Soft | Contract | Back-reference status |
|---|---|---|---|---|
| 2 Game Data Definitions | Upstream | Hard | Monster and move fields, ranges and validation; a move with no hits (a windup). Provisional: System 2 has none of these fields yet. | A row is added to System 2's Open Questions. |
| 1 Seeded RNG | Upstream | Hard | The `intents` stream; bounded and weighted draws; the call order of Rule 4. | Present (System 1 names System 14); wording updated. |
| 3 Effect Resolution | Upstream | Hard | Resolves each hit as a `Damage` effect (a sweep is one effect on all heroes); the damage formula used for the displayed number. | Updated. |
| 36 Combat State and Step Model | Peer | Hard | Stores the bound sequences, `e` and `n`; the action transaction. | Updated. |
| 15 Party and Heroes | Upstream | Hard | Hero instance IDs and party-slot order for targets. | Present (System 15 lists System 14 as a data consumer); updated. |
| 12 Combat Resolution | Downstream (caller) | Hard | Calls `GetBoundAction`, commits the action, owns the loop and the stun check. | Updated. |
| 5 Run State and Flow | Downstream (caller) | Hard | At room entry the run flow reads the encounter spec it stored when the door was generated (System 16 never redraws it), passes the roster and multipliers to the binding of System 14, and stores the preparation record with the RNG state. | Designed ([run-state-and-flow.md](run-state-and-flow.md)); binding happens at party formation, at the door choice and at the boss entry. |
| 16 Encounter Generation | Downstream (producer of inputs) | Hard | Produces the roster and the multipliers `hpPermille` and `damagePermille` at door generation, and uses the monster data and the threat of Formula 7 (with scaled values, party size 3) for danger and validation. System 5 hands them to System 14; System 14 does not depend on System 16, which would create a cycle. | Designed ([encounter-generation.md](encounter-generation.md)). |
| 13 Status Effects | Peer | Soft | The stun status: a stunned enemy keeps its bound entry. | Updated. |
| 11 Turn Order | Peer | Hard | Encounter slot, base speed `5..60`, the forecast of upcoming enemy actions. There are no reinforcements in the MVP. | Updated. |
| 4 Event Log | Downstream | Soft | `IntentsBound`; the `EnemyMove` origin with the move ID and the occurrence. | Recorded in System 4's Open Questions. |
| 27 Combat HUD, 29 Payoff Presentation, 30 Post-combat Breakdown | Downstream | Hard for 27 | Intent tab, order-strip glyphs and the sequence list; the breakdown of intents and skipped hits. | 27 Designed pending review; 29 draft pending gate/review; 30 Not Started. |
| 35 Balance Simulation Harness | Downstream | Soft | Samples seeds and reports the RNG version with its results. | Not designed. |

**Circular-dependency check:** System 14 reads Systems 1, 2, 3, 15 and (through 36) 13, and is called by Systems 5, 12 and 16. The systems index lists 16 as depending on 14, which agrees; it also lists 14 as depending on 36, which holds for the stored state. The index row for 14 should add 15 and 13 (soft).

## Tuning Knobs

| Knob | Default | Safe range | Affects | If too high | If too low |
|---|---|---|---|---|---|
| Sequence length `H` | 12 | 6 to 20 | How many actions are bound and shown; the number of draws (`2 × H` per enemy). | The popup is long and the sequence is rarely used up. | The sequence wraps in a normal fight and repeats. |
| `HeavyGap` | 2 | 1 to 4 | The minimum distance between two heavy moves. | Heavy moves become rare. | Two heavy hits can come back to back. |
| Repeat limit | 2 | fixed rule | A move cannot be chosen a third time in a row. | Not a knob. | Not a knob. |
| Order-strip horizon `K` | 6 (System 11) | 1 to 20 | How many upcoming glyphs the strip shows. | The forecast is long but less reliable. | The player cannot see far enough. |
| Move weight | per move, `0..100` | 0 to 100 | How often a move is chosen. | One move dominates. | The move never appears. |
| Hits per move `n` | per move, `0..8` | 0 to 8 | The size and the spread of one action. | A single action nearly decides a fight. | Multi-hit is not worth reading. |
| Monster stats (provisional, design time) | regular HP `40..100`, speed `16..26`, `B 5..14` (widened for System 16's Swarmer, Brute and the room 1 and 2 speed band 18..26); elite `130..180`, `18..24`, `10..14` (plain heavy up to 20); boss `300..420`, `20..26`, `12..18` | owned by System 35's measurements | How hard a fight is (Formula 8). | Fights end the run too early. | Fights are trivial. |
| `m_hp`, `m_dmg` | 1000 (identity); `500..10,000` and `500..5000` | set by System 16 | Difficulty of an encounter. | Fights are unwinnable. | Fights are trivial. |
| Enemies per encounter | at most 6 (System 16 may cap lower) | 1 to 6 | Number of simultaneous threats. | The screen is crowded. | Little to read. |

**Not knobs.** Damage-only moves in the MVP, no draws after binding, no retargeting, a stunned enemy keeping its entry, the two draws per entry and the absence of reinforcements are design rules; changing one bumps the rules version and regenerates the golden fixtures. Monster speed is not scaled by the encounter multipliers because of the content lint of System 11.

> *`systems-designer` consulted (2026-11-13).*

## Visual/Audio Requirements

- **Data supplied, not drawing.** System 14 supplies the facts; the art bible (5.10.8, 7.4a, 7.5, 7.8) owns every pixel, colour and timing. Per enemy: its ID, size class, slot (spawn order only, no meaning) and the entry at `n` for its tab. Per entry: the glyph (`attack` or `windup`), the damage `r` and hit count `h`, the target scope (one hero with its join numeral, all heroes) and a state (ready, stunned, a hit skipped because its target fell). Per attacking move: the delivery archetype (Swing or Throw), supplied as move data. A forecast list of glyphs for the order strip, shared with System 11. Events for the act: begin, each hit landing, end.
- **Minimum glyph set for the MVP.** `attack` and `windup`. `guard` and `buff` are not needed (no shield or speed effect for enemies in the MVP); a generic `debuff` and a glyph for a detonating move are not needed either. Single hit, multi-hit and sweep are told apart by the numerals and the chip, not by new glyphs. A glyph for `windup` is new work (Open Questions).
- **One intent on the tab.** The tab (about 40×24) shows one glyph, one number and one chip, so the sequence beyond the next entry is shown by the order strip and the popup, not by a second tab.
- **Not by colour alone.** The intent glyph is flat Fog-hi; the meaning is carried by its shape, and every state of the tab has a text equivalent.
- **Audio.** This system makes no sound.
- **Not covered by the art bible (Open Questions):** the stunned, spent and "hit skipped" states of the tab, when the tab shows the next entry after an act, the chip when a hero has fallen and the move hits all heroes, and the numeral format for multi-hit (`r×h`).

> 📌 **Asset Spec** — Visual/Audio requirements are defined. After the art bible is approved, run `/asset-spec system:enemy-and-intent` to produce per-asset visual descriptions, dimensions and generation prompts from this section.

> *`art-director` consulted (2026-11-13).*

## Game Feel

- **A telegraph the player can act on.** The player sees what is coming and the order it comes in before the fight, so the intents are a puzzle to answer, not a surprise to endure.
- **What you saw is what happens.** The glyph read on the tab is the glyph that lifts off and acts; the number read is the damage dealt before shield. Nothing is drawn after the player commits.
- **A loss has a name.** A failed fight leaves, in the breakdown, the intent that decided it: the move, the target and any hit skipped because its target had already fallen.
- **No time of its own.** Choosing an entry takes no presentation time; the next enemy turn starts when the previous action's presentation ends (Systems 12 and 29). Speed control never changes an intent.

## UI Requirements

- **Intent tab (System 27):** the glyph of the entry at `n`, the damage `r` (`r×h` for several hits), the target chip (the hero's join numeral; three squares for all heroes; a windup shows a chip only when it has a target mode). The tab never shows a rolled or hidden value. Its text equivalent and the popup state the number as damage before shield (for example "8 damage, before shield"), because the hero's shield and the target decide the HP actually lost.
- **Order strip (System 27):** the glyph of each upcoming enemy action from the forecast, marked as a projection and labelled "if nothing changes".
- **Popup:** the whole bound sequence in words ("Action 1: Strike, 8 damage to the Ranger; Action 2: Windup; …"), with the move, the damage, the hit count and the target of each entry. It states that the sequence is fixed, that the timing follows the forecast, and that after action `H` the sequence repeats from action 1 (a boss returns to its first phase).
- **States:** a stunned enemy shows its intent as held; a hit skipped because its target fell is named in the log and in the breakdown. Every state has a text equivalent for keyboard and gamepad focus.
- **Breakdown (System 30):** the bound entry, the damage dealt, and "hits 2 and 3 skipped (target downed)", from `IntentsBound` and the `EffectSkipped` facts.

> 📌 **UX Flag — Enemy and Intent**: This system has UI requirements. In Phase 4 (Pre-Production), run `/ux-design` for the combat HUD intent tab, the order strip and the breakdown before writing epics. Stories that reference UI should cite `design/ux/[screen].md`, not the GDD directly.

## Cross-References

| Reference | What it provides here |
|---|---|
| [Game Concept](game-concept.md), Core Mechanics 3 and the MVP Definition | Intent shown before combat while the player can still rearrange; 6 regular monster types plus a boss (System 16's table; the concept's earlier 4 to 5 was too low). |
| [Systems Index](systems-index.md), row 14 | Dependencies 2, 1, 3, 36. |
| [Seeded RNG](seeded-rng.md), Rule 5 and Interactions | The `intents` stream; the binding intent drawn before combat; previews never draw; the open question on candidate order and draw count (answered here). |
| [Combat Resolution](combat-resolution.md), Core Rules 1 to 3 and the edge-case table | Prepare selects binding intents; a bound target that falls is skipped without retargeting. |
| [Effect Resolution Model](effect-resolution-model.md), Core Rules 4 and 5, spike effect kinds | Enemy move effects in authored order; only `Damage`, `ApplyStatus` and `ConsumeDetonate` in the MVP; late target binding. |
| [Event Log](event-log.md), Enemy Move origin | The `EnemyMove` origin with a stable move ID and the occurrence. |
| [Combat State](combat-state-and-step-model.md), Core Rule 1 | Binding intents are stored in the combat state. |
| [Turn Order](turn-order.md), Edge Cases and Open Questions | Encounter slots and reinforcements (none in the MVP); the forecast used by the order strip. |
| [Party and Heroes](party-and-heroes.md), Rules 2 and 3 | Hero instance IDs and slots for targets; heroes restored before each encounter. |
| [Art Bible](../art/art-bible.md), 5.10.8, 7.4a, 7.5, 7.8 | The intent tab, the lifted glyph, enemy lines and the glyph system. Its glyphs `guard` and `buff` are not needed in the MVP. |
| Prototype `MonsterIntentController.cs`, `MonsterDefinitionSO.cs`, `DefaultGameCatalog.cs` (evidence only) | Moves cycled by a cursor, direct mutation, retargeting of dead targets, shield and speed moves, float difficulty. None is adopted. |

## Acceptance Criteria

**Fixtures.**
- `PARTY-3`: hero instance IDs in party-slot order `[7, 3, 5]`, deliberately not sorted by ID, so a binder that sorts by ID fails.
- `SCRIPT`: a scripted draw source behind a test seam (`Bounded(b)` and `Weighted(weights)`; the seam's name is provisional until System 1's ADR). It asserts the bound or the weights of every call, fails on an extra or a missing call and counts calls. For a result below the bound the word is the result; for a bound-1 draw any word gives 0. For a weighted draw the script gives the ticket `t`, and the mapping from `t` to a move is one scan in the test.
- Monsters, each move written `n, B, target, weight` (★ = heavy): `BRUTE` (HP 100, speed 20; opener Strike): Strike `1, 8, Rand, 60`; Windup `0, –, none, 20, link → Heavy`; Heavy★ `1, 14, Rand, 0`. `DUO` (opener Jab): Jab `1, 6, Rand, 50`; Bash `1, 9, Rand, 50`. `HEAVY` (no opener): Jab `1, 5, Rand, 50`; Slam★ `1, 20, Rand, 50`. `SOLO`: Bash `1, 10, Rand, 100`. `ECHO` (opener Mark): Mark `1, 4, Rand, 50, link → Follow`; Follow `1, 4, SameAsMove(Mark), 0`. `SAME`: A `1, 5, Rand, 50`; B `1, 5, SameAsMove(A), 50`. `WIND` (opener Wind): Wind `0, –, Rand, 100, link → Hit`; Hit `1, 7, SameAsMove(Wind), 0`. `FLURRY`: `3, 5, Rand, 100`. `CLEAVE`: `1, 6, AllHeroes, 100`. `BOSS-T`: Strike `1, 10, Rand, 100` and Cleave `1, 6, AllHeroes, 100` with the phase table entries `0..1`: {Strike}; entries `2..`: {Cleave}.
- `REF-BINDER`: a reference binder of about 30 lines written in the test from Rules 4 to 6 and Formula 4 only (no production code). It is first checked by hand against criterion 6, then cross-checked against production on every fixture at `H = 12` with the ticket `t = (37 × callIndex + 11) mod bound` (deterministic, no RNG).
- Logic tests live under `Assets/_Project/Tests/EditMode/EnemyAndIntent/`, in the existing `Project05.EditModeTests` assembly (no nested asmdef). That assembly references only `Project05.Runtime` today, so the new engine-free core assembly (name and settings fixed by an ADR, NOT SOURCEABLE) must be added to its references. The prototype `MonsterIntentController` is not the system under test.

1. **Definition validation (Rule 1; blocked until System 2 has the fields).** **Given** `n = 9`, `B = 0` on a move with `n ≥ 1`, a weight of 101, two openers, no move, nine moves, no move with `n ≥ 1`, a windup with a `B`, a link to itself, a loop, an unknown or foreign ID or a windup, or a phase table with a gap, an overlap or no phase for index 0, **then** the catalog is rejected naming the record and field.
2. **Roster (Rule 2).** **Given** a roster of 1 and of 6 enemies, **then** it is accepted; **given** 0 or 7 enemies, a repeated slot, a slot above 5 or an empty party, **then** `ContractViolation` and no draw is made.
3. **Scaling baked first (Rules 2 and 11, Formulas 1 and 2).** **Given** `BRUTE` with `m_hp = 1300` and `m_dmg = 1150`, **then** the instance holds `ScaledHP = 130` and `ScaledB = 9` (Strike) and `16` (Heavy), built with zero draw calls, and a hook on the first `SCRIPT` call sees these values already in place.
4. **Slot independence (Rule 2).** **Given** two enemies whose slots are swapped, **then** each one's HP, damage and speed are unchanged.
5. **Binding time (Rule 3).** *[BLOCKED] until Systems 5, 12 and 16 exist.* **Given** a room entry, **then** the draws happen once, after the door choice and before the first preview.
6. **Sequence of the worked example (Rules 4 and 5, Formula 4).** **Given** `BRUTE` at `H = 12`, `PARTY-3`, the move tickets `t = 75, 10, 5, 7, 59, 60, 0, 40` for the weighted entries 1, 3, 4, 5, 7, 8, 10 and 11 (in that order) and the target results `1, 0, 2, 0, 2, 0, 1, 2, 0, 0, 1, 2` for entries 0 to 11, **then** there are 24 calls with the move and target bounds `(1,3) (80,1) (1,3) (80,3) (80,3) (20,1) (1,3) (80,3) (80,1) (1,3) (80,3) (60,3)`, the moves are Strike, Windup, Heavy, Strike, Strike, Windup, Heavy, Strike, Windup, Heavy, Strike, Strike, and the targets are IDs `3, –, 5, 7, 5, –, 3, 5, –, 7, 3, 5`.
7. **Link must fit (Rule 5).** **Given** `BRUTE` at `H = 6` and the same draws, **then** entries 0 to 4 equal criterion 6's and entry 5 is Strike with the bounds `(60,3)` (the Windup's chain would run past the last entry), 12 calls in all.
8. **Relaxation (Rule 5).** **Given** `SOLO` at `H = 4`, **then** every entry is Bash and the bounds are `(100,3)` four times (entries 2 and 3 drop the repeat limit and draw normally); **given** a monster whose every weight stays zero after relaxation, **then** the first move with an authored weight above zero is chosen with a bound-1 draw.
9. **Two enemies in slot order (Rule 4).** **Given** `DUO` in slot 0 and `HEAVY` in slot 1 at `H = 4`, **then** the bounds are `(1,3) (100,3) (50,3) (100,3)` for `DUO` followed by `(100,3) (50,3) (50,3) (50,3)` for `HEAVY`, and the sequences are Jab, Jab, Bash, Bash and Slam, Jab, Jab, Slam.
10. **Opener counts (Rule 5).** **Given** `DUO` at `H = 4`, **then** entry 0 is Jab by a forced draw `(1,3)`, entry 1 is Jab (`t = 10`), and entry 2 has the bound 50 because Jab, Jab would be a third in a row.
11. **No opener (Rule 5).** **Given** `HEAVY` entry 0, **then** it is a weighted draw with the bound 100.
12. **`HeavyGap` (Rule 5).** **Given** `HEAVY` at `H = 4` with tickets `75, 10, 49, 49`, **then** the sequence is Slam, Jab, Jab, Slam: the bound is 50 at entries 1 and 2 (Slam is zeroed within two entries of a heavy) and entry 3 has the weights `[0, 50]`.
13. **Link (Rule 5).** **Given** `ECHO` at `H = 4`, **then** the sequence is Mark, Follow, Mark, Follow with the bounds `(1,3) (1,1) (50,3) (1,1)`.
14. **Boss phases (Rule 5).** **Given** `BOSS-T` at `H = 4`, **then** the sequence is Strike, Strike, Cleave, Cleave with the bounds `(100,3) (100,3) (100,1) (100,1)`.
15. **Random target (Rule 6).** **Given** target results 0, 1 and 2 with `PARTY-3`, **then** the targets are IDs 7, 3 and 5; **given** a party of 2, the bound is 2; **given** a party of 1, the bound is 1.
16. **All heroes (Rule 6).** **Given** `CLEAVE`, **then** the target draw is a bound-1 draw and the target is `[7, 3, 5]`.
17. **Same target (Rule 6).** **Given** `ECHO`, **then** Follow's target equals Mark's; **given** `SAME` at `H = 1` with the ticket 99, **then** entry 0 is B, `A` has not occurred, so the target draw has the bound 3 and the result 2 gives ID 5.
18. **Windup with a target (Rule 6).** **Given** `WIND` at `H = 2` and the target result 0, **then** entry 0 draws `(1,3)` and targets ID 7, and entry 1 (Hit, forced by the link) draws `(1,1)` and targets the same ID 7.
19. **Multi-hit (Rule 6).** **Given** `FLURRY` with the target result 1, **then** the three hits all target ID 3 and the entry uses exactly two draw calls.
20. **No mutator (Rule 7).** **Given** a bound sequence and a source that fails on any call, **then** after 30 advances and reads the sequence is value-equal to the original and no call was made; and the public interface has no operation that changes an entry.
21. **Atomic binding (Rule 7).** **Given** a failure injected after the tenth draw of a binding, **then** the live `intents` state and the stored sequences are unchanged, and a retry gives the same sequences as an uninterrupted binding.
22. **Initial state (Rule 8).** **Given** a fresh binding, **then** `e = 0`, `n = 0`, there are `H` entries, and the slot and the enemy instance ID are set.
23. **Baseline and record (Rule 8).** *[BLOCKED] until Systems 4, 5, 12 and 36 are revised.* **Given** Start, **then** the sequences are copied to System 36 and one `IntentsBound` record per enemy lists the sequence; **given** a missing, short or inconsistent record, **then** a technical stop and no lazy draw.
24. **Stun (Rule 9).** **Given** a stunned action, **then** `e + 1` and `n` is unchanged.
25. **Wrap (Rule 9).** **Given** `DUO` at `H = 4`, `n = 3` and no stun, **then** entry 3 is used and `n` becomes 0.
26. **Hits and windup (Rule 9).** **Given** `FLURRY`, **then** three hits are handed over in authored order; **given** a windup, **then** an empty effect list is handed over and the action counts as completed.
27. **Downed target and source (Rule 9).** *[BLOCKED] until Systems 3, 12, 13 and 36.* **Given** a bound target downed before a hit, **then** `EffectSkipped(target_downed)` with no retarget and no draw; **given** the source downed mid-action, **then** its queued hits still resolve.
28. **Displayed damage (Rule 10, Formula 3).** **Given** `m_dmg = 1000`, **then** the tab shows 8 for Strike, 14 for Heavy and `5×3` for `FLURRY`; **given** `m_dmg = 1150`, **then** 9, 16 and `6×3`; and one hit is shown as `13`, never `13×1`.
29. **Upcoming glyphs (Rule 10).** **Given** `BRUTE` with the sequence of criterion 6 and `n = 10`, **then** the next four glyphs are attack, attack, attack, windup (entries 10, 11, 0 and 1).
30. **Vocabulary (Rule 11).** *[BLOCKED] until System 2.* **Given** a Summon, a shield or a heal move, **then** the catalog is rejected; there is no operation that adds an enemy during a fight.
31. **Identity (Rule 12).** **Given** three enemies, **then** their instance IDs are distinct; **given** eight scheduled actions with stuns at the second and the third, **then** the occurrence `e` runs 1 to 8.
32. **Determinism (Rule 13).** **Given** the same script, **then** two bindings give identical sequences and no call is made beyond the script.
33. **Engine-free (Rule 13).** **Given** the core assembly, **then** its definition sets `noEngineReferences`, and an IL scan finds no `UnityEngine` reference, no mutable `static` field, no `float` or `double`, no `DateTime`, `Random`, `GetHashCode` or `GetInstanceID`.
34. **Scaled HP (Formula 1).** **Given** `(HP, m_hp) = (64, 1300), (3, 1500), (3, 1499), (1, 500), (100000, 10000)`, **then** `83, 5, 4, 1, 1,000,000`; **given** `m_hp = 499` or `10,001`, **then** `ContractViolation`.
35. **Scaled damage (Formula 2).** **Given** `(B, m_dmg) = (11, 1150), (9, 1050), (3, 1500), (20000, 5000), (30000, 5000)`, **then** `13, 9, 5, 100000, 100000` (the last is clamped from 150,000); **given** `m_dmg = 499` or `5001`, **then** `ContractViolation`.
36. **Displayed damage formula (Formula 3).** **Given** `ScaledB = 13` with no modifier, **then** 13; **given** `(4, F_e = 0, P_e = +250, h = 3)`, **then** `5×3`; **given** `P_e = −1000`, **then** 0 and no label; **given** `(B = 4, F_e = −10)`, **then** 0. *Parity with System 3's damage function is [BLOCKED] until System 3 is implemented.*
37. **Move choice (Formula 4).** **Given** the weights `[60, 20, 0]`, **then** the tickets 0 and 59 give the first move and 60 and 79 the second; **given** `[0, 20, 0]`, **then** the tickets 0 and 19 give the second; **given** `[20, 0, 30]`, **then** the ticket 20 gives the third.
38. **Draw count (Formula 5).** **Given** `(E, H) = (1, 12), (3, 12), (6, 12), (1, 1)`, **then** `24, 72, 144, 2` calls. *The word-level check (a rejected word counted) is [BLOCKED] until System 1 is implemented.*
39. **Intent index (Formula 6).** **Given** `DUO` at `H = 4` and eight actions with stuns on the second and the third, **then** the entries used are 0, 1, 2, 3, 0, 1, with `n = 2` and `e = 8`; **given** `n_0 = 5`, `k = 30`, `H = 12`, **then** `n = 11`.
40. **Threat (Formula 7).** **Given** HP 64, Strike `n = 1, B = 11, w = 60`, Sweep `n = 1, B = 6, w = 40`, party 3, `v = 18`, **then** 16; **given** HP 64, Strike `w = 60` and a Windup `w = 20`, `v = 18`, **then** 10; **given** HP 100, one move `B = 10, w = 100`, `v = 10`, **then** exactly 10.
41. **Load estimate (Formula 8).** **Given** the example inputs, **then** `φ = 0.2704 ± 0.0005`, computed in tooling (the core has no floating point). *The pass band is [BLOCKED] until System 35.*
42. **Allocation.** **Given** warmed-up `GetBoundAction`, the upcoming-glyph query and the displayed-damage query, **when** each is called 1,000 times, **then** the managed allocation delta is zero, measured with a control loop that does allocate.
43. **Operation budget.** **Given** a binding with `E = 6` and `H = 12`, **then** it makes exactly `2 × H × E = 144` calls and at most `H × E × M` weight evaluations; `GetBoundAction` reads one entry; the upcoming-glyph query for 20 actions reads exactly 20 entries. The elapsed time is recorded for information only and never asserted.
44. **Restore (blocked).** *[BLOCKED] until Systems 1 and 5 and the RNG ADR.* **Given** a snapshot taken before binding, **then** restoring and binding again gives identical sequences; **given** a cloned preview followed by 30 reads, **then** the `intents` state is equal.
45. **UI evidence.** *[BLOCKED] until the UI exists and Systems 5/14/26/27/29 resolve the combat inspection surface and presentation-aligned state.* **Given** the combat HUD, **then** retained screenshots in `production/qa/evidence/` show the intent tab, the order strip with its "if nothing changes" label, the popup with the whole sequence, a windup with its chip, a skipped hit, the "before shield" label, and the popup's statement that the sequence repeats after action `H`. System 26 requires full-sequence access before combat; that evidence alone does not establish this combat-HUD criterion.

**Watched failures.** Each of these tests must be seen failing against the named mutant before it is trusted: criterion 6 against a binder that reverses slot order or sorts by ID; criterion 6 against a binder that skips the forced draw (22 calls instead of 24); criterion 7 against a binder without the link-fit rule; criterion 10 against a binder that does not count the opener; criterion 12 against `HEAVY`, not `BRUTE` (Heavy's weight 0 never exercises the gap); criterion 24 against a stun that advances `n`; criterion 25 against a missing wrap; criterion 35 against truncation instead of half-up; criterion 37 against a scan that does not skip a zero weight; criterion 42 against a control that does not allocate (a broken probe).

> *`qa-lead` consulted (2026-11-13); the corrections it found are applied in the rules above.*

## Open Questions

| Question | Owner | Resolve by |
|---|---|---|
| System 2 needs the monster and move fields (a windup with `n = 0` and no `B`, target modes, weights, `heavy`, `linkedNext`, the boss phase table, the delivery archetype Swing or Throw) and the validation list of the Edge Cases section. | Systems 2 and 14 | Next System 2 revision. |
| System 3 must confirm: a sweep is one `Damage` effect over the living heroes (one `EffectSkipped(target_downed)` if none is living), every hit of a move is a root effect, and an action with an empty effect list is legal. | Systems 3 and 14 | Before System 3 is re-reviewed. |
| System 4 must add one `IntentsBound` record per enemy with its whole sequence, and the `EnemyMove` origin fields; a windup is an `ActionStarted` with no effects. | Systems 4 and 14 | Before System 4 is re-reviewed. |
| Answered by System 5 ([run-state-and-flow.md](run-state-and-flow.md), 2026-11-13): System 5 holds the preparation record (the bound sequences; the `intents` state lives in the streams section), calls the binding at party formation, at `ChooseDoor` and at the boss entry, and there is no retry; a resume restores the stored record. | Systems 5 and 14 | Closed. |
| Systems 12 and 36 must copy the sequences at Start, store `e` and `n`, keep a stunned enemy's entry, and decide whether skipped actions count toward the 200 limit. | Systems 12 and 36 | Before those GDDs are re-reviewed. |
| System 1: the call order of Rule 4 (two calls per entry, bound-1 draws), the clone for atomic binding and the golden vectors need its ADR; adding a draw shifts every later `intents` outcome for the same seed, so a change here bumps the rules version. | Systems 1 and 14 | With the RNG ADR. |
| System 16 passes the roster and the multipliers, enforces the content lint of System 11 and authors the encounters. Turn Order's criterion on reinforcements conflicts with Rule 11 (none in the MVP) and must be marked deferred. | Systems 16 and 11 | When System 16 is designed. |
| Art: a glyph for `windup`; the tab states for stunned, spent and a skipped hit; when the tab relights after an act; the chip when a hero has fallen and a move hits all heroes; the numeral format for multi-hit; `guard` and `buff` glyphs deferred. | art-director | Art bible follow-up. |
| Content: the six regular monster types (System 16: Hunter, Striker, Swarmer, Sweeper, Brute, Elite Bruiser) and the boss still need authoring. Role sketch from the consultation: a Brute (strike, windup, heavy), a Swarmer (fast multi-hit), a Sweeper, a Hunter (strike on a random hero) and a boss with three phases and no summons. | game-designer, systems-designer | Content pass before the first catalog fixture. |
| Balance: the sequence length `H`, `HeavyGap`, the repeat limit, the load estimate `φ` and its assumption of 10 hero actions per fight, and whether pre-visible randomness keeps the 1-in-20 target; System 35 should sample intent seeds. | systems-designer, System 35 | First balance simulation. |
| `Mark` and `Execute` (apply and detonate Ấn on a hero), `guard` and `buff` moves are deferred to Tier 1.5; they need Systems 3 and 13 and new glyphs. | Producer, Systems 3 and 13 | Tier 1.5 planning. |
| The concept names the heroes Tanker, Warrior and Support while System 15 uses four prototype heroes; the concept should be updated. The systems index row for System 14 should add Systems 15 and 13 (soft). | game-designer, producer | Next concept revision. |
| **Answerability (CD-GDD-ALIGN).** Every MVP move type (strike, multi-hit, sweep, windup into heavy) has at least one answer in the MVP item pool (a speed item, a max-HP item, or a way to defeat the enemy before it acts), and every monster's sequence is answerable by an arrangement. Without this the threat is not a puzzle. | game-designer, systems-designer, Systems 16 and 35 | Content pass and first balance simulation. |
| **Ignorable intent (CD-GDD-ALIGN).** System 35 flags any monster whose intent display changes no outcome (no arrangement does better than any other against it); such a monster has no reason to exist (Pillars 4 and 5). | System 35, systems-designer | First balance simulation. |
| **Damage before shield (CD-GDD-ALIGN).** Should the tab or the popup also show the projected HP loss against the target's current shield, as a secondary readout? The number shown is damage before shield. | System 27, ux-designer | `/ux-design` for the combat HUD. |
| **Decoy hero.** Hits on a downed hero are skipped, so a low-HP hero that falls early lowers the damage still to come; System 35 should watch for this as a degenerate strategy. | System 35, systems-designer | First balance simulation. |
| **Popup discoverability.** The order strip shows `K = 6` actions while `H = 12` are bound; confirm that players find the popup that lists the whole sequence. | ux-designer | `/ux-design` and the first playtests. |
