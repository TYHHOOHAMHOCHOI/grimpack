# Effect Resolution Model

> **Status**: In Revision (`/design-review` verdict NEEDS REVISION, 2026-11-11)
> **Creative Director Review (CD-GDD-ALIGN)**: REOPENED 2026-11-11 — failure handling and chain limits; re-run after revision. Earlier APPROVED 2026-11-10 covered pillar alignment only.
> **Author**: Codex studio design session, with systems design, gameplay engineering and Creative Director consultation
> **Last Updated**: 2026-11-11
> **Last Verified**: 2026-11-11 (document consistency only)
> **Implements Pillar**: Arrangement Is the Thinking and The Payoff Is the Product.

## Summary

Effect Resolution turns the items and enemy moves in a fight into an ordered sequence of consequences. One hero can prepare a status for another hero to detonate, and the event log preserves who contributed. The queue order, chain limits (`D = 8`, `N = 256`, ending the action with a visible `ChainLimit` fact), integer damage formula and the rule that System 3 holds no state across actions are settled in this document. The ordering keys still need confirmation from Systems 9, 10, 11, 13, 14 and 15 when those are designed.

> **Quick reference** — Layer: `Core` · Priority: `MVP` · Key deps: `Game Data Definitions, Event Log, Seeded RNG, Adjacency and Synergy, Combat State and Step Model`

## Overview

System 3 resolves authored effects and their reactions using a deterministic queue, integer calculations and explicit causal context. System 36 owns mutable combat state and its mutation API, System 4 owns event records, and System 12 owns encounter lifecycle and outcomes. Enemy moves, item effects, status ticks and detonations all use this resolution path. Systems 1, 2 and 4 have draft GDDs, System 36 is In Review and System 10 has no GDD yet. Their interfaces below remain provisional.

The MVP proof is an engine-free combat spike in which one hero applies a status and another detonates it. Identical inputs must yield the same event log and historical state in Unity, headless simulation and tests. Prototype code applies several effects directly, uses Unity types and has no detonation model. Its current values and order are migration evidence, not adopted rules.

## Player Fantasy

The player is the architect of a machine. An item on the first hero creates an opportunity, and an item on a later hero turns it into a payoff the player can recognize. A loss should also leave a readable chain of causes. Hero order and arrangement matter before combat, and the displayed intent remains binding. The player watches the result rather than manually choosing an action during the fight.

## Detailed Design

### Core Rules

1. **Effect input.** A root action carries encounter and action identity, the actor, an authored ordered effect list, target-selection information and a typed origin. Item origins include hero and item instances, definition ID, encounter placement and the modifiers actually applied. Enemy actions carry the enemy instance, monster definition, move ID and action occurrence. Status ticks and detonations retain their application or baseline contribution references as required by System 4. Definition IDs and immutable parameters come from System 2. Runtime identities and current state come from System 36.
2. **Ordering.** Resolve one encounter action at a time. Enqueue root effects in the order supplied by the owner of the action, then process a FIFO queue. Resolve each effect against targets in an explicit stable order supplied by its owner. Reactions discovered by an effect are appended to the tail in an explicit stable producer order. Each child has depth `parent depth + 1`, with a root at depth zero. Do not use collection iteration order, render timing, localized names or Unity instance IDs as tie-breakers. Systems 9, 10, 11, 13, 14 and 36 must supply their root, modifier, target and reaction ordering in the key form defined in the Canonical Order Contract below. FIFO is chosen over depth-first because playback then shows every effect of one wave before the consequences of that wave, which keeps the cause of each reaction adjacent to its trigger. Percent modifiers are summed before one application, so the order in which modifiers are listed affects only the recorded provenance, never the damage value.
3. **State and events.** Each attempted effect reads the current committed state, obtains the current target and produces a proposed mutation. System 36 validates and commits it and System 4's corresponding typed events together. System 3 never mutates HP, shield, status, gauge or cooldown directly. For damage that changes shield and HP, System 36 validates the complete batch before publishing either change, with one event and historical view per changed field as System 4 specifies. No mutation event is emitted for a no-op.
4. **Apply then detonate.** An apply effect requests a positive status contribution through System 36, preserving the typed origin and System 4 application reference. A later detonation requests contribution selection from System 13. It records the chosen sources and quantities through System 36 and System 4 and uses one reproducible detonation occurrence ID for the consumption and resulting damage. For the MVP chain, damage and consumed status have the same target and detonator. Cross-target detonation requires a coordinated change to System 4. System 13 defines oldest-first, timed Ấn contributions for the spike. Use actual consumption, and emit no consumption or damage for zero available. Stage the grouped consumption and all linked damage steps in one System 36 transaction; a failed calculation or validation publishes none of them.
5. **Chain limits.** Each root action has a configured maximum reaction depth `D` and maximum processed effects `N`. Both are positive, finite, versioned values (spike: `D = 8`, `N = 256`; see Tuning Knobs). Check child depth before enqueue and the processed count before processing. When either limit would be exceeded, System 3 discards the unprocessed queue and publishes one `ChainLimit` fact in the deterministic trace (reason `depth` or `count`, root action ID, `d` and `k`), then reports the action complete to System 12. Everything committed before the limit stays valid, and the encounter continues with due expiry, the outcome check and the next actor. A `ChainLimit` is never a victory, defeat or reward by itself. A technical stop is reserved for contract violations: unordered keys, invalid inputs, an arithmetic failure or a batch that fails validation. In that case System 12 abandons the run and returns to menu with a separate technical error and no reward, retaining the valid committed prefix. A depth cap alone does not bound a wide reaction tree, which is why both limits exist.
6. **Reaction listeners.** Each reaction listener, identified by its listener key, may produce at most one reaction per root action. A second trigger of the same listener within the same root action is not queued and publishes a `ListenerSuppressed` fact. This makes a loop such as A triggers B triggers A impossible by construction, so `D` and `N` are a last-resort backstop rather than the normal terminator. The trigger unit is the effect, not the mutation step: a damage effect that changes both shield and HP triggers its listeners once.
7. **Randomness boundary.** The current System 1 contract has `doors`, `drops` and `intents` streams. Effect resolution does not draw from them. A future random combat effect requires a named, versioned combat stream and a coordinated System 1 change before implementation. A preselected enemy intent remains binding under System 14.
8. **No state across actions.** The queue, depth `d`, counter `k` and the per-listener counters exist only inside one root action. They are discarded when the queue drains or a `ChainLimit` ends the action. System 3 therefore adds nothing to any snapshot or checkpoint. The checkpoint and resume contract belongs to Systems 24, 36 and 5.

### Canonical Order Contract

All ordering keys are integers or ordinally compared stable IDs. Never use collection iteration order, render timing, localized names or Unity instance IDs. The anchor cell of a placed item is the first cell it occupies when its grid is scanned row by row, left to right, so a rotated item has exactly one anchor.

| What is ordered | Key, most significant first | Owner |
|---|---|---|
| Ready items firing in one hero action | Anchor row, anchor column, then item instance ID as a defensive final key. | System 9 |
| Effects inside one item | Index in the authored effect list. | System 2 (data), System 9 |
| Enemy action effects | Index in the authored effect list of the chosen move. | System 14 |
| Targets of one effect | Side, then party slot (heroes) or encounter slot (enemies), front to back, then instance ID. | System 36 supplies slots; System 15 defines party slot indexes |
| Modifiers applied to an effect | Source item anchor row, anchor column, then modifier index in the item's authored list. | System 10 |
| Reactions produced by a committed event | One total key: `(eventSeq, kindRank, side, slot, anchorRow, anchorCol, effectIndex, definitionOrdinal)`. `eventSeq` is the sequence number of the triggering event. `kindRank`: Item `0`, EnemyMove `1`, Status `2`. `side`: Hero `0`, Enemy `1`. A component that does not apply is `0`. A status listener uses the `side` and `slot` of the target holding the status, `anchorRow` and `anchorCol` of `0`, and the policy's index in its definition as `effectIndex`, so status listeners on different targets never tie. `definitionOrdinal` is the ordinal of the stable definition ID. | Producing system |

The queue never re-sorts. Each producer supplies its entries already ordered by a strictly ascending key. For reactions, "strictly ascending" applies within the batch produced by one committed event; batches enter the queue in `eventSeq` order, so the whole queue stays FIFO. System 3 checks that each key is strictly greater than the previous one in its batch. An equal or descending key is a contract violation: a technical stop with a diagnostic outside the deterministic trace (unlike a chain limit, which is a normal in-trace outcome). System 3 never guesses an order to continue.

### Targeting, Counting and Trace Stamps

- **Target binding is late.** An effect selects its targets when it is taken from the queue, against the latest committed state and in Canonical Order Contract order. An effect requires a living target by default (`requiresLiving`). A downed target (HP 0 in System 36) is skipped with `EffectSkipped(target_downed)`; an invalid one with `EffectSkipped(target_invalid)`. An enemy move whose intent is already bound under System 14 never retargets.
- **Modifiers are not snapshotted.** System 10 supplies modifiers that are immutable for the whole encounter, so they are collected when the effect is taken from the queue. The modifiers actually applied are recorded on the origin.
- **What `k` counts.** Every effect taken from the queue, root or reaction, counts once, whatever its outcome (applied, skipped, or zero damage). It counts per effect, not per target.
- **Multi-target damage.** The damage formula is evaluated once per effect. Every target receives the same `r`, in target order. Each target is its own System 36 transaction.
- **Detonation with `c > 0` and `r = 0`.** The consumption stands and no damage event is published. A selection that no longer validates when the transaction commits is a contract violation and ends in a technical stop; it is never silently skipped.
- **Trace stamps.** Stamps belong to an effect: every record of one effect shares `rootActionId`, `depth`, `parentEffectSeq` (the final mutation record of the effect that triggered it, null for a root effect, never a fact) and `effectOrdinal` (`k`, strictly greater than the parent's). They are required on `StatusApplied`, `StatusConsumed`, `Damage`, `DetonationDamage` and the four facts, and not on `ActionStarted`, `ActionSkipped`, `StatusExpired`, `UnitDowned` or `CombatEnded`. `ChainLimit` carries the depth and ordinal of the last processed effect, plus the blocked depth and the discarded count. Consumers measure the size of a combo as the maximum depth and the effect count of one root action. System 4 owns the schema.

### Determinism and Engine-Free Rules

The simulation core lives in an assembly with `noEngineReferences` that also references none of `UnityEngine`, `Unity.Collections`, `Unity.Mathematics` or `Unity.Burst`.

- No mutable static fields. Unity 6.6 skips the domain reload when entering Play Mode by default, so static state would survive into the next run.
- No floating point, `DateTime`, `Random`, `string.GetHashCode`, culture-sensitive comparison, iteration order of `Dictionary` or `HashSet`, or `List.Sort` without a comparer over unique keys. No Unity `InstanceID` or `EntityId`.
- Damage variables are `long`. Cast to `long` before multiplying and use a `checked` context.
- NOT SOURCEABLE: the C# language level and .NET profile available to the core assembly are not covered by `docs/engine-reference/unity/`. Do not rely on `init`, records or other newer features until an ADR records the asmdef settings and language level.

### Spike Effect Kinds

The spike resolves exactly three kinds of effect. System 2 validation rejects a catalog that names any other kind.

| Kind | Meaning | Rule |
|---|---|---|
| `Damage` | Reduce a target's shield then HP | Uses the Formulas below. `B` is validated `1..100,000`, so a modifier changes a damage effect but never creates damage from a zero base. |
| `ApplyStatus` | Add a positive status contribution to a target | Requested through System 36 with the typed origin. The amount is a positive authored integer. |
| `ConsumeDetonate` | Consume status units and deal damage from them | Selection by System 13, then `B = c × p` through the damage formula, in one transaction. |

Deferred by name, each needing its own coordinated GDD before use: `Heal`, `ShieldGain`, `GaugeChange`, `CooldownChange`, `StatusRemoval` other than expiry, `Summon`, and any random effect.

### Fact Events

Besides mutation events, System 3 asks System 4 to publish these non-mutating facts, each with a reason code and the root action ID, depth and parent effect. System 4 owns the schema; its event-log GDD already allows facts without state changes.

| Fact | Reason codes |
|---|---|
| `ChainLimit` | `depth`, `count` |
| `ListenerSuppressed` | `once_per_root` |
| `EffectSkipped` | `target_downed`, `target_invalid` |
| `DetonationEmpty` | `no_available_units` |

A no-op never appears as a state change. A skipped or empty effect appears only as one of the facts above, so a failed setup stays readable.

### States and Transitions

| State | Trigger | Result |
|---|---|---|
| Idle | System 12 supplies an action with valid identity and ordered inputs | Initialize a root queue and its provenance. |
| Resolving | Queue has an effect within the limits | Validate its target and inputs, resolve it through System 36, and enqueue ordered reactions. |
| Resolving | Queue becomes empty | Report queue completion to System 12 for due status expiry and outcome evaluation. Death does not end the action early; queued effects must still meet their eligibility rules. |
| Resolving | A chain limit would be exceeded | Discard the unprocessed queue, publish `ChainLimit`, report the action complete to System 12. The encounter continues. |
| Resolving | Invalid contract, unordered keys, arithmetic failure or failed batch validation | Technical stop: preserve the committed prefix and report the diagnostic to System 12. |

### Interactions and Example Trace

The [Status Effects fixture](status-effects.md) applies 3 Ấn from hero A and 2 from hero B. Hero B later requests 4, consuming A:3 then B:1 in a single event and deals 8 resolved damage. With shield 5 and HP 3, the same atomic batch then records shield 5 → 0 and HP 3 → 0. The damage events link to consumption. System 12 checks the outcome after the actor action's queue and due expiry finish. A validation or arithmetic error publishes none of this detonation batch.

## Formulas

All arithmetic is integer-only. No floating point, no culture-dependent rounding. Reaction limits are fixed for the spike at `D = 8` and `N = 256` (rules-versioned).

| Symbol | Meaning | Rule |
|---|---|---|
| `d`, `D` | Current and maximum reaction depth | Root `0`; child `d + 1`; allow child only if its depth is at most `D = 8`. A depth-9 child is not queued and publishes `ChainLimit(depth)`. |
| `k`, `N` | Number of effects processed in the root action and its limit | Process the next effect only while `k < N = 256`. The 257th effect is not processed and publishes `ChainLimit(count)`. |
| `c` | Actual available units consumed | Use the result of System 13 selection. Zero means no detonation. |
| `p` | Damage per consumed unit | Authored by System 13 status data, validated `1..1000` by System 2. Working Ấn value `p = 2`. |
| `B` | Base damage of the effect | Authored integer in item or enemy-move data, validated `1..100,000` by System 2 for a `Damage` effect. For a detonation `B = min(c × p, RawMax)`, computed with saturation. |
| `F` | Sum of flat modifiers | Sum of System 10 flat modifiers in Canonical Order Contract order. Each validated `−100,000..100,000`. |
| `P` | Sum of percent modifiers, in permille | 1000‰ = 100%. All percent modifiers are summed first, then applied once (additive bucket). An authored percent modifier is validated `−1000..100,000`. At run time the summed `P` is clamped to at least `−1000`, so the factor `1000 + P` is never negative even when several debuffs stack. |
| `RawMax` | Saturation bound for `Raw` | `RawMax = 10^12`. With `1000 + P` at most `101,000`, the product stays below `1.01 × 10^17`, far inside signed 64-bit. |
| `Raw` | Damage before percent scaling | `Raw = clamp(B + F, 0, RawMax)`. |
| `Scaled` | Damage after percent scaling | `Scaled = (Raw × (1000 + P) + 500) / 1000` in integer arithmetic (round half up). `Raw` is non-negative, so integer division equals floor. |
| `Cap` | Largest damage one effect may deal | `Cap = 2,147,483,647`, the largest value of the signed 32-bit HP and shield domain. |
| `r` | Resolved damage | `r = clamp(Scaled, 0, Cap)`. `r = 0` is a no-op: emit no damage event. |
| `q`, `h` | Current shield and HP | Signed 32-bit state in the System 36 domain. |
| `s`, `l` | Actual shield absorbed and HP lost | `s = min(q, r)`, `l = min(h, r − s)`. Stage shield first and then HP, only for positive losses. The leftover damage is overkill. |

**Modifier order.** Flat modifiers are added to the base first, then the combined percent bucket scales the sum. Percent modifiers never compound on one another. A modifier that does not apply to the effect's tags is not collected. The modifiers actually applied are recorded on the origin, in contract order, so the payoff can show what contributed.

**Rounding.** Round half up, so a small positive modifier still changes a small hit (+10% on 5 damage gives 6). `Raw` is clamped to at least `0` before scaling, so a negative value never reaches the division and C# truncation and floor cannot diverge.

**Worked examples.**

| Case | Calculation | `r` |
|---|---|---|
| Item with `B = 6`, `F = +2`, `P = +250‰` | `Raw = 8`; `(8 × 1250 + 500) / 1000 = 10` | `10` |
| Rounding up: `B = 5`, `P = +150‰` | `(5 × 1150 + 500) / 1000 = 6` (5.75) | `6` |
| Small buff: `B = 5`, `P = +100‰` | `(5 × 1100 + 500) / 1000 = 6` (5.5) | `6` |
| Ấn detonation: `c = 4`, `p = 2`, `F = +1`, `P = +500‰` | `B = 8`; `Raw = 9`; `(9 × 1500 + 500) / 1000 = 14` (13.5) | `14` |
| Additive bucket: three `+500‰` sources on `B = 10` | `P = 1500`; `(10 × 2500 + 500) / 1000 = 25` (compounding would give 33) | `25` |
| Negative result: `B = 3`, `F = −5` | `Raw = clamp(−2, 0, RawMax) = 0` | `0` (no event) |
| Saturation: `c = 10^13`, `p = 1000` | `B` saturates at `10^12`; `Raw = 10^12`; `r = clamp(10^12, 0, Cap)` | `Cap` |
| Fixture without modifiers: `c = 4`, `p = 2`, shield 5, HP 3 | `B = 8`, `(8 × 1000 + 500) / 1000 = 8` | `s = 5`, `l = 3` |

The damage formula has no overflow path: `B` and `Raw` saturate at `RawMax`, `P` is clamped, and the product is bounded as shown above. Only the event sequence counter and the System 36 domains can still overflow, and those end in a technical stop. System 36 validates the resulting values and publishes each target's damage atomically.

## Edge Cases

| Case | Required handling or open owner decision |
|---|---|
| Target is dead or invalid when its queued effect reaches the front | Revalidate against System 36. A living-only effect on a downed target is skipped. A bound enemy effect whose target becomes invalid is skipped without retargeting; other valid effects continue. |
| Status is absent or selected contribution is no longer available | An empty active ledger yields `DetonationEmpty`. A selection that fails validation against the current ledger at commit is a contract violation and ends in a technical stop; it is never skipped. |
| Multiple status sources or partial consumption | Follow System 13's oldest-first policy, with one record listing actual sources and amounts. |
| Cyclic reactions or excessive fan-out | The once-per-root listener rule prevents loops. As a backstop, `D = 8` and `N = 256` end the action with a visible `ChainLimit` fact; the committed prefix stays valid, there is no implicit victory, and the encounter continues. |
| Damage crosses shield into HP or a staged mutation is invalid | Validate the entire batch before publication, including consumption for detonation. No part of a failed batch commits. |
| Computation or counter overflows | The damage formula cannot overflow: `B` and `Raw` saturate at `RawMax`, and a result above `Cap` is clamped to `Cap`. An overflow of the event sequence counter or a System 36 domain fails without committing the pending mutation and ends in a technical stop. |
| Detonation has `c > 0` but resolved damage is `0` | The consumption stands, no damage event is published. This cannot occur in the MVP roster (no negative modifiers exist yet); revisit when Systems 13 or 19 add debuffs. |
| A listener appears twice in one batch with equal keys | Contract violation: technical stop (see the order contract). A second trigger from a different event is a normal `ListenerSuppressed`. |
| Stacked percent modifiers sum below −1000‰ | Clamp the summed `P` to `−1000` at run time. The factor is `0`, so the damage is `0`, which is a no-op with no mutation event. Not an error. |
| Authored values fall outside their validated ranges | System 2 validation rejects the catalog before the run starts. A value outside range reaching run time is a contract failure and ends in a technical stop. |
| A detonation finds no available units | Publish `DetonationEmpty(no_available_units)`; no consumption or damage events. |
| A queued effect's target is downed or invalid | Publish `EffectSkipped` with reason `target_downed` or `target_invalid`; other effects continue. |
| Two ready sources or targets have equal priority | Equal or descending keys from a producer violate the Canonical Order Contract. This is a technical stop with a diagnostic outside the trace; preserve the committed prefix and never guess an order. FIFO cannot repair an undefined enqueue order. |

## Dependencies

| System | Contract and status |
|---|---|
| 2 Game Data Definitions | Draft GDD supplies plain C# definitions and stable identities. Structured effect payloads and units still need a shared contract. |
| 4 Event Log | In Review. Defines mutation events, origins, status links and atomic shield/HP batch, plus the per-effect resolution stamps, the `Damage` record and the `ChainLimit`, `ListenerSuppressed`, `EffectSkipped` and `DetonationEmpty` facts that System 3 requests (revised 2026-11-11). Its re-review remains pending. |
| 1 Seeded RNG | In Review. Existing streams are not a source for random combat effects. Algorithm and stream derivation require an ADR. |
| 10 Adjacency and Synergy | No GDD yet. Supplies immutable encounter modifiers and their stable ordering and origins. |
| 36 Combat State and Step Model | In Review. Owns mutable state, mutation validation, atomic commit and historical views. |
| 9, 11, 13, 14 | Define trigger order and cooldown, turn order, status lifecycle and contribution selection, and enemy intent/action inputs respectively. System 13 is In Review; the others have no GDD yet. |
| 12 Combat Resolution | In Review. Owns encounter lifecycle, victory, defeat, draw and technical stop. After the action queue drains, System 12 processes due expiry and checks outcomes. The spike's turn cap belongs to System 12. |
| 5 Run State and Flow, 24 Run Save and Resume, 36 | Own the checkpoint and resume contract. System 3 holds no state across actions, so it adds nothing to a snapshot. System 24 is Vertical Slice and has no GDD yet. |
| 27, 29, 30, 35 | Consume the event trace and historical views for HUD, payoff, breakdown and headless verification. They do not advance resolution. |

## Tuning Knobs

| Knob | Spike value | Safe range | Affects |
|---|---|---|---|
| Max reaction depth `D` | 8 | 4 to 16 | Longest vertical chain before `ChainLimit(depth)` |
| Max processed effects `N` | 256 | 64 to 1024 | Widest fan-out per root action before `ChainLimit(count)` |
| Damage cap `Cap` | 2,147,483,647 | Fixed to the 32-bit HP and shield maximum | Largest damage of one effect; keeps `r` representable in System 36 |
| Permille scale | 1000 = 100% | Fixed | Unit of every percent modifier |
| Raw saturation `RawMax` | 10^12 | Fixed; must keep `RawMax × 101,000` inside signed 64-bit | Removes every overflow path from the damage formula |
| Base damage `B` (authored) | per item or move | `1..100,000` | Raw strength of an effect, validated by System 2 |
| Flat modifier `F` (authored) | per modifier | `−100,000..100,000` | Adds to or subtracts from `B` before scaling |
| Percent modifier `P` (authored) | per modifier | sum `−1000..100,000` permille | Scales `B + F` once, additively; the run-time sum is clamped at `−1000` |
| Damage per consumed unit `p` | Ấn: 2 | `1..1000`, owned by System 13 data | How strongly a detonation converts stacks into damage |

`D` and `N` are safety guards, not balance values: a legitimate build never reaches them. All of these live in the versioned rules config. Changing any of them bumps the rules version and requires regenerating the boundary fixtures. For `D` and `N`, below the range risks cutting valid builds; above it delays loop detection and bloats the trace. The `B`, `F` and `P` ranges are the authoring limits that keep the damage formula inside its checked 64-bit intermediates.

The working Ấn coefficient is 2 damage per consumed unit for the spike. The formula, modifier order and units are settled; production effect magnitudes and balance remain open until the item roster (Systems 8, 9, 10) and the balance harness (System 35) exist.

## Visual/Audio Requirements

Systems 29 and 32 use the committed event stream for payoff, cues and pacing. A larger chain must not hide the item, hero, grid, HP or intent needed to read its cause. System 3 carries the origin and applied modifiers needed by those consumers; it does not call audiovisual APIs.

## Game Feel

The player should be able to follow setup into detonation and recognize why a chain succeeded or failed. The order must remain legible as feedback escalates. A chain limit is shown to the player as a visible `ChainLimit` fact that ends that action's chain, not as a loss or an error. A contract violation is a diagnostic condition and must not be represented as an ordinary combat result.

## UI Requirements

System 3 owns no screen. The combat HUD, payoff and breakdown need the committed event order, item placements, modifiers and historical views. Systems 27, 29 and 30 own their presentation and UX specifications.

## Cross-References

- [Game Concept](game-concept.md) defines the player pillars, the apply-then-detonate risk and combat spike.
- [Systems Index](systems-index.md) records the engine-free boundary, integer arithmetic and the ownership split among Systems 3, 12 and 36.
- [Event Log](event-log.md) specifies the same-target detonation chain and atomic shield/HP event batch.
- [Game Data Definitions](game-data-definitions.md) defines static identity and catalog boundaries.
- [Seeded RNG](seeded-rng.md) defines currently available named streams.
- [Unity version reference](../../docs/engine-reference/unity/VERSION.md) pins `6000.6.3f1`. This rule-only core has no local Unity `modules/scripting.md` reference. Runtime feasibility and Unity/headless parity remain to be verified.

## Acceptance Criteria

1. **Given** producers that follow the Canonical Order Contract, **when** the same action resolves with their source collections built in different insertion orders, **then** the ordered input lists, effect order, records and historical views are identical. **Given** a producer list with an equal or a descending key, **then** System 3 performs a technical stop with a diagnostic, keeps the committed prefix and processes no later effect. Test producers are doubles, so this test does not wait for Systems 9, 10, 11, 13 or 14.
2. **Given** a root effect that creates two ordered reactions, **when** the FIFO policy resolves them, **then** the existing queue entries are processed before the newly appended reactions and depth increases by one per parent-child link.
3. **Given** the apply-and-detonate fixture above, **when** it resolves against test doubles of Systems 4, 12, 13 and 36 with the same catalog, state and rules version in Unity EditMode, a headless run and the unit-test run, **then** the recorded status application, consumption and damage have the same order, links, actor/item identities, placement and historical views in all three. There is no RNG in this path. A second integration run against the real implementations waits for Systems 4, 12, 13 and 36.
4. **Given** staged Ấn consumption and pending shield/HP damage and a System 36 test double that rejects the HP step, **when** the detonation resolves, **then** none of the consumption or damage publishes. A successful batch exposes a grouped consumption view followed by one view per changed damage field.
5. **Given** `D = 8`, **when** a chain contains a depth-8 child and a depth-9 child, **then** the depth-8 child resolves and the depth-9 child is not queued; one `ChainLimit(depth)` is published carrying the root action ID, the depth and ordinal of the last processed effect, the blocked depth and the discarded count, the action completes and the encounter continues.
   **Given** `N = 256`, **when** a wide chain reaches 257 effects, **then** the 256th effect processes, the 257th does not, and one `ChainLimit(count)` is published. In both cases the committed prefix stays readable, no victory, defeat or reward is declared, and System 12 proceeds to due expiry, the outcome check and the next actor.
   **Given** one listener triggered twice by effects of the same root action, **then** it produces exactly one reaction and one `ListenerSuppressed(once_per_root)` fact. A damage effect that changes shield and HP triggers each listener once.
6. **Given** actual consumption 4 with `p = 2` and no modifiers, shield 5 and HP 3, **when** the damage formula runs, **then** 8 resolved damage produces shield loss 5 and HP loss 3. **Given** the worked examples (`10`, `6`, `6`, `14`, `25`, `0` and `Cap`), **when** each runs, **then** the resolved damage matches the table and the `0` case emits no event. **Given** `c = 10^13` and `p = 1000`, **then** `B` and `Raw` saturate at `10^12` and damage equals `2,147,483,647` with no technical stop. **Given** a detonation with `c > 0` and `r = 0`, **then** the consumption event is published and no damage event is.
7. **Given** an enemy action, **when** its effect resolves, **then** it passes through the same queue and commit boundary with origin `{enemyInstance, monsterDefinition, moveId, actionOccurrence}`. **Given** a status tick, **then** its origin is `{statusDefinition, contributionRef}` listing the actual contributing application or baseline references. Both are asserted field by field.
8. **Given** the simulation core assembly, **when** it is compiled and inspected, **then** it references none of `UnityEngine`, `Unity.Collections`, `Unity.Mathematics` or `Unity.Burst`, and a reflection test finds no non-readonly static field in it.
9. **Given** two consecutive root actions where the first processed many effects or ended in a `ChainLimit`, **when** the second begins, **then** its first effect has depth 0 and `k = 1`, and no per-listener state from the first applies (a listener suppressed in the first may react in the second).
10. **Given** a queued damage effect whose target is downed when the effect is taken from the queue, **when** it is processed, **then** it is skipped with `EffectSkipped(target_downed)`, other queued effects still process, and the skipped effect counts once toward `k`.
11. **Given** a detonation with no available units, **when** it resolves, **then** `DetonationEmpty(no_available_units)` is published and no consumption or damage event appears.
12. **Given** a technical stop in the middle of an action, **when** the diagnostic is reported, **then** no further queued effect is processed and the committed prefix and its latest historical view are readable.
13. **Given** any `StatusApplied`, `StatusConsumed`, `Damage`, `DetonationDamage` record or fact produced during a root action, **when** the trace is read, **then** it carries `rootActionId`, `depth`, `parentEffectSeq` and `effectOrdinal`, every record of one effect shares them, and a root effect has depth 0 and a null parent.
14. **Given** a damage effect that changes both shield and HP, **when** it triggers listeners, **then** each listener fires once for the effect, not once per mutation step.

## Open Questions

| Question | Owner | Resolve by |
|---|---|---|
| Do Systems 9, 10, 11, 13, 14 and 15 confirm the Canonical Order Contract keys (anchor-cell reading order, party and encounter slot indexes, listener keys)? | Systems 9, 10, 11, 13, 14 and 15 | When each of those GDDs is designed, before the combat spike implementation. |
| Who owns the checkpoint and resume contract (contents, format, persisted trace prefix), given that System 3 holds no state across actions? | Systems 24, 36, 5 and 4; ADR | Before System 24 is designed. |
| Which ADR records the core assembly's asmdef settings, C# language level and .NET profile, and the determinism bans? | Technical Director and lead programmer | Before the combat spike implementation. |
