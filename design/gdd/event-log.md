# Event Log

> **Status**: In Review
> **Design Review**: NEEDS REVISION. Both review rounds' findings addressed 2026-11-10, pending re-review.
> **Creative Director Review (CD-GDD-ALIGN)**: APPROVED 2026-11-10
> **Author**: Codex studio design session, with systems design, engineering and Creative Director consultation
> **Last Updated**: 2026-11-11
> **Last Verified**: 2026-11-11 (document consistency only)
> **Implements Pillar**: The Payoff Is the Product. Supports Arrangement Is the Thinking.

## Summary

The Event Log records what actually happened in combat and what caused it. When one hero sets up a status and another detonates it, the game can show both contributions and play back the fight in the order it happened. The live game, tests and headless harness read the same facts.

> **Quick reference** — Layer: `Foundation` · Priority: `MVP` · Key deps: `None`

## Overview

System 4 defines the structured, engine-free event stream for committed combat facts. It assigns each record a position, validates its envelope and causal links, and exposes immutable reads. System 36 owns mutable combat state, System 3 owns effect order and semantics, and System 12 owns encounter lifecycle. The MVP scope covers combat attribution, playback, HUD state and breakdown evidence, including the two-hero combat spike. Persistent replay, save compatibility and cross-run records are later systems.

The prototype exposes localized strings and a bounded UI log. Those strings can guide UI migration. The core produces structured records at the commit boundary, and presentation derives text from the records.

## Player Fantasy

“I built that, and I can tell why it worked.” A player watches the setup hero apply a status, sees a second hero cash it in and recognizes the item placement behind the payoff. On a loss, the same evidence helps them identify the enemy move or missed synergy and change their next arrangement. The log supports Sensation by letting feedback land in order, and Expression by making the player's pre-fight choices legible.

## Detailed Design

### Core Rules

1. **Ordered records.** One append-only stream per encounter, keyed by the stable encounter identifier assigned by its owner. An event reference is the encounter ID and a contiguous sequence number. The owner submits facts in deterministic simulation order. System 4 records that order without consulting time, rendering, subscribers or RNG. The record includes a kind, stable actor and target instance IDs where relevant, a typed payload and the cause. Include action or resolution context where the producer supplies it. Definition IDs from System 2 remain opaque values and do not replace instance IDs.
2. **State changes and evidence.** System 36 commits one event per semantic step through its mutation API. A status consumption or expiry can change several sources for one target and status in one event; shield and HP changes remain separate steps. Validate the record before publishing the change and make the state and event visible together. If an invariant fails, expose neither half of the failing commit and stop the encounter with a separate diagnostic. Meaningful facts without state changes, such as a blocked trigger or a terminal outcome, may use explicit kinds when their owning rules call for them. A no-op may never appear as a state change. Emit one fact at its owner boundary to avoid double logging.
3. **Causal identity.** Every mutation carries a typed origin. An item origin identifies its acting hero, item instance, definition and encounter placement reference, with the modifiers actually applied and their source references when relevant. Enemy moves, encounter setup and status ticks use the contracts below. Item placement is read from immutable encounter arrangement data or captured in the origin, so later rearrangement does not move old highlights.
4. **Detonation provenance.** The status owner keeps source references while contributions are active. Each consumption records the actual sources and amounts used, in deterministic producer order. Damage from a detonation links to the consumption so that earlier contributions remain traceable after removal. The minimum fields, valid links and validation ownership are specified below.
5. **Historical reads.** Capture the initial encounter view and make the exact immutable post-event view available by sequence. A change event exposes the result needed by consumers, such as HP or shield before and after. A nonmutating fact may reuse the preceding view. Consumers only see committed records and can read them by cursor independently. Filtering, coalescing, pacing and skipping operate on the consumer view. They preserve the underlying trace and do not change simulation inputs.
6. **Retention and parity.** Keep the complete encounter trace, baseline contribution records and historical views through playback and breakdown. Preserve the metrics or evidence needed for the run summary before releasing encounter data. System 5 and the consumers define handoff and lifetime together. Never silently drop rows or leave broken cause links in a retained trace. With equal initial state, catalog, seed, player decisions and rules, producers must generate the same logical encounter, actor, target, item instance, placement and baseline contribution IDs across tests, headless and Unity. The same requirement applies to action or resolution IDs if emitted. IDs may come from the initial snapshot or from deterministic producer allocation. Unity instance IDs, random IDs and wall-clock values cannot serve as logical IDs. The ID generation technology and wire encoding are architecture decisions.

### Minimum Causal Contract

The kind names below describe the contract. Each record uses the encounter identity and sequence from Rule 1. Origins include all fields required by their kind under Rule 3. System 3 supplies a reproducible logical ID for each detonation occurrence under Rule 6. A detonation produced by an item carries that item's origin on both consumption and damage.

| Record kind | Required payload | Allowed links for this chain |
|---|---|---|
| `StatusApplied` | Target instance, status ID, positive integer amount actually added as a contribution, typed origin, and expiration boundary for a timed contribution. | No predecessor is required for this record to serve as a contribution. |
| `StatusConsumed` | Target instance, status ID, positive integer total consumed, logical detonation occurrence ID, typed detonator origin, ordered nonempty list of distinct source references with the positive integer amount consumed from each. Amounts use the owning status rule's unit. | Each source is either an earlier `StatusApplied` record in the encounter or a contribution in the encounter baseline. Its target and status must match the consumption. The view shows all listed contributions after the step. |
| `StatusExpired` | Target instance, status ID, completed actor-action identity and ordinal at the expiry boundary, a typed `StatusExpiry` origin tied to that boundary, reason `DurationElapsed`, and an ordered nonempty list of distinct source references and their actual positive expired remainders. | Same-encounter application or baseline references with matching target/status. System 36 validates the stored due boundary and current remainder. One record and view cover all listed remainders for that target/status. |
| `DetonationDamage` | Damage target instance, the same logical detonation occurrence ID, typed detonator origin, resolved damage amount and actual HP and shield before and after for this record's change. | Exactly one earlier `StatusConsumed` record in the encounter, including a staged predecessor in the same atomic detonation batch. For this MVP chain, it has the same occurrence ID, target and detonator identity. The consumed status and quantity are obtained from that record. A later cross-target mechanic requires an explicit contract for both targets. |

- **System 4 validates the record against the trace.** Check required fields, origin shape, link cardinality, scope, existence and kind, distinct source references, the target and status match for each contribution, and the occurrence, target and detonator match for damage. Each consumption in this chain has a distinct occurrence ID within the encounter. Every source quantity must be positive and their sum must equal the stated consumed total. A source quantity cannot exceed the amount originally recorded for that source.
- **Systems 3 and 13 define consumption.** They select and order contributing applications and decide the amount taken from each under the status rules. System 3 supplies the cause of the resulting damage. **System 36 validates against current state** before committing, including that the target still holds the status and that each quantity is available from its contribution after previous consumption, expiry or other changes. It commits the state and the System 4 record together. Passing trace checks alone does not prove a contribution is still available.

### Enemy Move and Periodic Status Origins

| Origin | Minimum fields and links | Validation and ownership |
|---|---|---|
| `EnemyMove` | Logical enemy instance ID, monster definition ID, stable move ID within that definition, and reproducible action occurrence ID. The event carries the affected target instance. | System 14 supplies the selected move and actor identity. System 3 supplies the action occurrence and affected target. System 4 checks required fields and identifier scope. Systems 14 and 36 verify the move belongs to the enemy definition and is valid for the encounter before commit. An action occurrence distinguishes repeated uses of the same move. |
| `StatusTick` | Affected target instance, status ID, reproducible tick occurrence ID and an ordered nonempty list of distinct source references. Each reference identifies either an earlier same-encounter `StatusApplied` event or an immutable baseline contribution. The referenced target and status match the tick. Each source resolves to its typed origin, including an enemy move or setup origin where applicable. | System 13 determines which contributions cause the tick and their order under its status rules. System 3 supplies the tick occurrence and resolved effect. System 4 checks required fields, distinct references, scope, existence, source kind and target/status matches. System 36 verifies the relevant status and contributions are active at the tick boundary. Tick attribution and its numeric policy belong to Systems 3 and 13. |

An explicit `EncounterSetup` origin identifies the encounter and a stable setup source key supplied by System 12. It is valid for a baseline status granted by setup. A tick can refer to that baseline contribution and reach its setup origin without inventing an item. System 2 and System 14 must define stable move IDs before the enemy-move integration fixture can run. System 13 must specify tick contribution selection before the multi-contributor tick fixture can run. The event schema and single-source fixtures above are required for the System 4 contract.

### Damage and Mutation Records

System 3 calculates resolved damage. System 36 stages each changed damage field in sequence, with one post-step view per event. The shield step and HP step share a damage occurrence ID and origin. Each record includes both before/after pairs with the unaffected field unchanged. The HP step's historical view derives downed state immediately at HP zero. Keep resolved damage distinct from actual shield absorbed and HP lost.

For detonation, System 36 stages the single grouped consumption step first and then all its damage steps. System 4 validates against the committed prefix plus earlier staged records. Publish the entire batch and its ordered views together only after all validation succeeds. Any failure leaves the original stack, shield, HP, sequence and trace unchanged for that batch. Preserve earlier committed events and report the diagnostic outside the trace. An identical accepted request replay creates no new records.

### Status Present at Encounter Start

Before recording begins, the status owner supplies an immutable table of starting contributions. Each row has an encounter-scoped logical contribution ID, target instance ID, status ID, positive starting amount, typed origin and, for a timed contribution, its expiration boundary. Use the real source identity and encounter placement if known and applicable. Use an explicit setup origin for status granted by encounter setup. A consumption refers to a baseline row by encounter ID and contribution ID, distinct from an event reference. System 4 validates that the ID resolves uniquely and the target, status and amount match the contract above. The baseline row remains addressable through the same read interface until the trace is released, even when no stacks remain. No synthetic application event is needed.

### States and Transitions

| State | Trigger | Result |
|---|---|---|
| Initialized | System 12 starts an encounter with an identity and baseline from the owner | Open an empty trace with an immutable baseline. |
| Recording | Owner submits a valid committed fact | Append in sequence and expose the view at that position. |
| Recording | Invalid record or causal link | Reject the failing commit and stop the encounter. Close the trace to further writes at the valid prefix and retain it for diagnosis. |
| Recording | System 12 commits the terminal outcome | Seal the trace with exactly one `CombatEnded` fact carrying the encounter ID, gameplay outcome and reason from System 12. A technical stop has a separate diagnostic and no gameplay terminal fact. Consumers may read the sealed record and snapshots. |
| Sealed | Playback and breakdown complete and run-level evidence is handed off | The owning lifecycle may release the trace when no remaining consumer requires it. |

### Interactions with Other Systems

Example for the Ấn spike, with an enemy initially at shield 5 and HP 3. Events 1–2 are prior committed applications. Events 3–5 publish together as the detonation batch. Each numbered position has its own historical view.

| Event | Fact | Evidence |
|---|---|---|
| 1 | Hero A applies 3 Ấn. | Application origin, target, amount and deadline. |
| 2 | Hero B applies 2 Ấn. | Distinct application origin and deadline. |
| 3 | Hero B detonates 4 Ấn. | One consumption records A:3 and B:1 in order. Its view has one remaining unit. |
| 4 | Detonation changes shield from 5 to 0. | Links to event 3; HP remains 3. |
| 5 | The same detonation changes HP from 3 to 0. | Links to event 3; shield remains 0 and the view reports the target downed. |

The HUD reads each matching historical view. Breakdown can trace the source from either damage step through consumption to the earlier applications. The outcome fact follows completion of the actor action, including remaining legal reactions and due expiry.

## Formulas

System 4 has no damage, timing or balance formulas. Its ordering and integrity rules use integer sequences.

| Symbol | Meaning | Rule |
|---|---|---|
| `n` | Number of already committed records in the encounter | `n ≥ 0`. The next record has sequence `n + 1`. A rejected commit leaves `n` unchanged. |
| `s`, `p` | Record sequence and sequence of a linked predecessor | `1 ≤ p < s` for a same-encounter causal link. The link must resolve to an existing record of the expected kind. |

Sequences and contribution amounts use signed 64-bit integers. Sequence values are positive and quantities are nonnegative, with source and consumed amounts strictly positive where required above. Before committing, System 4 checks that sequence allocation for the entire pending batch is representable. Sum contribution quantities with checked arithmetic before comparing them to the consumed total. If either operation overflows, reject the pending commit or batch, retain state and the valid prefix, stop the encounter and emit a diagnostic outside the trace. Never wrap or clamp a sequence or validation sum. Gameplay arithmetic and its overflow behavior are owned by Systems 3 and 36.

## Edge Cases

| Case | Resolution |
|---|---|
| Two copies of the same item definition | Use distinct item instance and placement references. Keep the shared definition ID for lookup. |
| A detonation consumes multiple applications or part of a stack | Keep all contributing application references and actual consumed quantities. Do not assign all credit to the most recent source by default. |
| Enemy damage, periodic status damage or encounter setup has no firing item | Use the relevant typed origin. Include real causal links when available. |
| Damage is absorbed or exceeds remaining HP | Keep the actual shield and HP changes distinct from the resolved damage amount supplied by System 3. Only emit a no-change fact when the consuming rule needs it. |
| A link is missing, forward, wrong-kind or from another encounter | Reject the failing commit and stop the encounter. Keep the last committed historical view and valid prefix readable. Emit the diagnostic separately from the deterministic trace. |
| Playback skips or subscribes after simulation is ahead | Read committed events and historical views by cursor. Skip can advance the view to the correct end state without changing event data or summary inputs. |

## Dependencies

No upstream GDD is required to construct and validate the log. The listed interfaces describe the contract for downstream GDDs.

| System | Contract |
|---|---|
| 3, 9, 10, 13, 14, 36 | Resolution supplies ordered actions, origin context, actual modifiers and status provenance. System 36 owns mutable state, atomic commit and historical state views. |
| 11, 12 | Turn Order supplies the selected action context. Its forecast is read-only. Combat Resolution owns encounter start and end and submits lifecycle facts. |
| 2, 5, 8 | System 2 supplies definition identities to producers. Run State owns the whole-run snapshot and snapshot lifecycle. Backpack supplies immutable encounter placement context to producers and readers. |
| 27, 29, 30, 32 | Read committed facts and historical views for HUD, payoff, breakdown and audio. Systems 29 and 30 own pacing and summary metric definitions, respectively. |
| 23, 24, 35 | Use a deterministic trace for validation, later replay or persistence integration. System 35 compares runs in MVP. System 23 owns persistent replay and System 24 owns save policy. |

## Tuning Knobs

System 4 has no gameplay tuning knobs. Playback speed, audiovisual caps and breakdown filters are owned by consumers. The architecture may choose a storage budget or a historical-view representation once measured against representative encounters, while preserving trace integrity and consumer requirements.

## Visual/Audio Requirements

Systems 29 and 32 consume ordered records and origin references to select effects and sounds. The event model carries no timing or asset decisions. When a payoff grows, the source references remain available for a clear highlight of the relevant placement.

## Game Feel

The setup is readable before the payoff and its author stays visible when the combo grows. HP and intent advance with playback, so the player can tell which action produced each change. A skipped animation still leaves the facts needed to understand a loss.

## UI Requirements

System 4 owns no screen. Systems 27, 29 and 30 read ordered facts, origin placement and historical state. Their UX specs own how grid, intent and HP stay readable and how the player opens or filters the breakdown.

## Cross-References

- [Game Concept](game-concept.md) defines Pillars 2 and 4 and the deterministic two-hero spike.
- [Systems Index](systems-index.md) records constraints 8 and 9 for historical state and cause tracking.
- [Game Data Definitions](game-data-definitions.md) distinguishes stable definition identity from runtime state.
- [Art Bible](../art/art-bible.md) prioritizes readable causes over larger effects.
- [Unity version reference](../../docs/engine-reference/unity/VERSION.md) pins `6000.6.3f1`. The Unity Core module reference is absent. The design uses plain C# boundaries, with live integration and storage feasibility to verify during the spike.

## Acceptance Criteria

1. **Given** identical initial state, rules, catalog, seed and player choices, **when** the two-hero spike runs in tests, headless and Unity, **then** the baseline contributions match by logical ID, target, status, amount and origin. Compare events by sequence and check encounter ID, sequence, kind, actor and target IDs, action context where emitted, all typed payload and origin fields, and each cause link in recorded order, including its reference kind, identity and amount where present. Compare the initial view and each corresponding post-event view field by field, including HP, shield, status contributions and intent by logical combatant ID. Compare ordered fields in their declared order and keyed fields by key. Report the first differing position and field.
2. **Given** Hero A applies a status, **when** Hero B detonates it for damage, **then** the trace resolves damage → consumption → application and preserves both heroes, both item instances and placements, status, consumed quantity and applied modifiers after the status is gone. Run the documented Ấn fixture to verify oldest-first partial consumption and its grouped view. Inject a failure in the last damage step and verify that no part of the detonation batch publishes.
3. **Given** a starting status contribution in the baseline table, **when** it is consumed by a detonation and the active status is removed, **then** the damage still resolves through consumption to the original baseline ID, target, status, starting amount and typed origin. Check that the trace has no synthetic application event and that the baseline remains readable through the retention window.
4. **Given** separate cases for a valid change, a no-op and a bad commit after a valid prefix, **when** each is submitted through the owner boundary, **then** the valid change emits one mutation event and the no-op emits none. Exercise missing, forward, wrong-kind and out-of-encounter links, mismatched targets or statuses, incorrect quantity sums and a previously depleted contribution. Each invalid commit leaves state and sequence unchanged, stops the encounter, preserves the readable prefix and last historical view, and emits a diagnostic outside the trace.
5. **Given** an enemy instance uses a defined move twice, **when** the resulting damage records are read, **then** each identifies the enemy instance, monster definition, move and distinct action occurrence. **Given** a periodic status tick from an earlier application and one from a setup baseline contribution, **when** their damage records are read, **then** each tick identifies its target, status and tick occurrence and resolves its source to the correct typed origin. Missing, wrong-kind, forward, cross-encounter or mismatched source links fail under criterion 4. Run the enemy integration fixture once Systems 2 and 14 define stable move IDs. Run a multi-contributor tick fixture once System 13 defines selection.
6. **Given** the log's core assembly, **when** it is compiled and its assembly references inspected, **then** it compiles without Unity and contains no `UnityEngine` or `UnityEngine.*` assembly references.
7. **Integration, when Systems 27 and 29 exist. Given** a sealed trace with several HP changes, **when** a reader plays, speeds up or skips, **then** the HUD uses the historical view at each reached position and finishes at the same state. The underlying trace is equal for all paths.
8. **Integration, when System 30 exists. Given** a player opens breakdown after combat, **when** it follows damage and status links, **then** event, baseline and placement references still resolve even if the current inventory has changed.
9. **Given** shield 5 and HP 10, **when** the owning damage rules resolve 3 damage entirely against shield, **then** one shield change records 5 → 2 while HP stays 10. **Given** the same starting values and an owner-specified resolution of 20 damage against shield then HP, **when** both changes commit, **then** two events share the damage occurrence and origin, record shield 5 → 0 then HP 10 → 0, and expose the intermediate and final views. A rejected second change leaves neither change published. These fixtures assert recording of the supplied owner outcomes and do not define the combat damage formula.
10. **Given** the sequence is at its maximum representable value or positive contribution amounts would overflow their checked sum, **when** a further record or consumption is submitted, **then** the pending commit is rejected, the encounter stops, the valid prefix and last view remain readable, and the diagnostic is separate from the trace.

11. **Given** an expiring Ấn contribution and a separate contribution not yet due, **when** the target completes the due action, **then** one `StatusExpired` records the expired remainder with its actual source and leaves the newer contribution active. Its view precedes any outcome at that boundary.

## Open Questions

| Question | Owner | Resolve by |
|---|---|---|
| Which event encoding and historical-view representation support live and headless parity within the measured budget? | Technical Director and engine programmer | Architecture review and combat core spike. |
| Which additional attribution and event kinds are needed for future periodic, refresh and cleanse effects? | Systems 3, 4 and 13 | Before introducing those statuses. |
| Which facts and derived metrics must survive after encounter breakdown closes? | Systems 5, 30 and 35 | Before implementing run summaries and trace release. |
