# Event Log

> **Status**: In Review
> **Design Review**: NEEDS REVISION (first logged `/design-review`, 2026-11-11). Findings addressed in the 2026-11-11 revision; independent re-review pending, to be run together with System 3.
> **Creative Director Review (CD-GDD-ALIGN)**: WITHDRAWN 2026-11-11 until re-review. Earlier APPROVED 2026-11-10 predates the System 3 addendum and this revision.
> **Author**: Codex studio design session, with systems design, engineering and Creative Director consultation
> **Last Updated**: 2026-11-11
> **Last Verified**: 2026-11-11 (document consistency only)
> **Implements Pillar**: The Payoff Is the Product. Supports Arrangement Is the Thinking.

## Summary

The Event Log records what actually happened in combat and what caused it. When one hero sets up a status and another detonates it, the game can show both contributions and play back the fight in the order it happened. The live game, tests and headless harness read the same facts.

> **Quick reference** — Layer: `Foundation` · Priority: `MVP` · Key deps: `None upstream (Seeded RNG shares parity inputs; Systems 3, 8, 12 and 36 are producers)`

## Overview

System 4 defines the structured, engine-free event stream for committed combat facts. It assigns each record a position, validates its envelope and causal links, and exposes immutable reads. System 36 owns mutable combat state, System 3 owns effect order and semantics, and System 12 owns encounter lifecycle. The MVP scope covers combat attribution, playback, HUD state and breakdown evidence, including the two-hero combat spike. Persistent replay, save compatibility and cross-run records are later systems.

The prototype exposes localized strings and a bounded UI log. Those strings can guide UI migration. The core produces structured records at the commit boundary, and presentation derives text from the records.

## Player Fantasy

“I built that, and I can tell why it worked.” A player watches the setup hero apply a status, sees a second hero cash it in and recognizes the item placement behind the payoff. On a loss, the same evidence helps them identify the enemy move or missed synergy and change their next arrangement. The log supports Sensation by letting feedback land in order, and Expression by making the player's pre-fight choices legible.

## Detailed Design

### Core Rules

1. **Ordered records.** One append-only stream per encounter, keyed by the stable encounter identifier assigned by its owner. An event reference is the encounter ID and a contiguous sequence number. The owner submits facts in deterministic simulation order. System 4 records that order without consulting time, rendering, subscribers or RNG. The record includes a kind, stable actor and target instance IDs where relevant, a typed payload and the cause. Include action or resolution context where the producer supplies it. Records of the kinds listed in the stamp table below must carry the resolution stamps. A source reference is always a tagged pair `{kind, id}` (`event` with a sequence, or `baseline` with a contribution ID), so a baseline ID and an event sequence can never collide. Definition IDs from System 2 remain opaque values and do not replace instance IDs.
2. **State changes and evidence.** System 36 commits one event per semantic step through its mutation API. A status consumption or expiry can change several sources for one target and status in one event; shield and HP changes remain separate steps. Validate the record before publishing the change and make the state and event visible together. If an invariant fails, expose neither half of the failing commit and stop the encounter with a separate diagnostic. Meaningful facts without state changes, such as a blocked trigger or a terminal outcome, may use explicit kinds when their owning rules call for them. A fact occupies a sequence position like any other record and reuses the preceding historical view. A `ChainLimit` fact is a normal committed record: it does not close the trace and is not a technical stop, which is reserved for an invalid record or link. A no-op may never appear as a state change. Emit one fact at its owner boundary to avoid double logging.
3. **Causal identity.** Every mutation carries a typed origin. An item origin identifies its acting hero, item instance, definition and encounter placement reference, with the modifiers actually applied and their source references when relevant. Enemy moves, encounter setup and status ticks use the contracts below. Item placement is read from immutable encounter arrangement data or captured in the origin, so later rearrangement does not move old highlights.
4. **Detonation provenance.** The status owner keeps source references while contributions are active. Each consumption records the actual sources and amounts used, in deterministic producer order. Damage from a detonation links to the consumption so that earlier contributions remain traceable after removal. The minimum fields, valid links and validation ownership are specified below.
5. **Historical reads.** Capture the initial encounter view and make the exact immutable post-event view available by sequence. A change event exposes the result needed by consumers, such as HP or shield before and after. A nonmutating fact may reuse the preceding view. Consumers only see committed records and can read them by cursor independently. Filtering, coalescing, pacing and skipping operate on the consumer view. They preserve the underlying trace and do not change simulation inputs.
6. **Retention and parity.** Keep the complete encounter trace, baseline contribution records and historical views through playback and breakdown. Preserve the metrics or evidence needed for the run summary before releasing encounter data. System 5 owns the release of an encounter trace, including a trace kept after a loss for the breakdown. System 4 only seals it and never releases it. Never silently drop rows or leave broken cause links in a retained trace. With equal initial state, catalog, seed, player decisions and rules, producers must generate the same logical encounter, actor, target, item instance, placement and baseline contribution IDs across tests, headless and Unity. The same requirement applies to action or resolution IDs if emitted. IDs may come from the initial snapshot or from deterministic producer allocation. Unity instance IDs, random IDs and wall-clock values cannot serve as logical IDs. The ID generation technology and wire encoding are architecture decisions.
7. **Immutability, lookup and ceiling.** Records and historical views are copied on ingest: a producer's mutable buffers are never retained, and consumers receive read-only data. Lookup of a link target by sequence, of a baseline row by contribution ID, and of an occurrence by occurrence ID is O(1); validation never scans the whole trace. A rules-versioned `traceRecordCeiling` bounds the records of one encounter (provisional value `20,000`, to be derived from measured encounters in the combat spike; the worst-case product of the System 3 limits and the 200-action cap is far larger and is not a target). Reaching the ceiling is a technical stop; the log never truncates.
8. **Determinism and engine-free rules.** The log lives in an assembly with `noEngineReferences` that references none of `UnityEngine`, `Unity.Collections`, `Unity.Mathematics` or `Unity.Burst`. It has no mutable static fields (Unity 6.6 skips the domain reload by default, so static state would survive between Play Mode runs). It does not rely on `Dictionary` or `HashSet` iteration order, `string.GetHashCode`, culture-sensitive formatting or comparison, or floating point. It is single-threaded. All sequence and sum arithmetic goes through one checked helper. NOT SOURCEABLE: the C# language level and .NET profile for this assembly are not covered by `docs/engine-reference/unity/`; an ADR records them.

### Minimum Causal Contract

The kind names below describe the contract. Each record uses the encounter identity and sequence from Rule 1. Origins include all fields required by their kind under Rule 3. System 3 supplies a reproducible logical ID for each detonation occurrence under Rule 6. A detonation produced by an item carries that item's origin on both consumption and damage.

| Record kind | Required payload | Allowed links for this chain |
|---|---|---|
| `StatusApplied` | Target instance, status ID, positive integer amount actually added as a contribution, typed origin, and expiration boundary for a timed contribution. | No predecessor is required for this record to serve as a contribution. |
| `StatusConsumed` | Target instance, status ID, positive integer total consumed, logical detonation occurrence ID, typed detonator origin, ordered nonempty list of distinct source references with the positive integer amount consumed from each. Amounts use the owning status rule's unit. | Each source is either an earlier `StatusApplied` record in the encounter or a contribution in the encounter baseline. Its target and status must match the consumption. The view shows all listed contributions after the step. |
| `StatusExpired` | Target instance, status ID, completed actor-action identity and its `expiryActionOrdinal` at the expiry boundary, a typed `StatusExpiry` origin tied to that boundary, reason `DurationElapsed`, and an ordered nonempty list of distinct source references and their actual positive expired remainders. | Same-encounter application or baseline references with matching target/status. System 36 validates the stored due boundary and current remainder. One record and view cover all listed remainders for that target/status. |
| `DetonationDamage` | Damage target instance, the same logical detonation occurrence ID, typed detonator origin, resolved damage amount and actual HP and shield before and after for this record's change. | Exactly one earlier `StatusConsumed` record in the encounter, including a staged predecessor in the same atomic detonation batch. For this MVP chain, it has the same occurrence ID, target and detonator identity. The consumed status and quantity are obtained from that record. A later cross-target mechanic requires an explicit contract for both targets. |

- **System 4 validates the record against the trace.** Check required fields, origin shape, link cardinality, scope, existence and kind, distinct source references, the target and status match for each contribution, and the occurrence, target and detonator match for damage. Each consumption in this chain has a distinct occurrence ID within the encounter. Every source quantity must be positive and their sum must equal the stated consumed total. A source quantity cannot exceed the amount originally recorded for that source. System 4 also keeps a running per-source ledger: the total consumed plus expired from one source never exceeds its original amount, so consuming 3 of 3 twice from the same source is rejected even though each record passes the bound alone. System 36 still owns the checks against current state.
- **Systems 3 and 13 define consumption.** They select and order contributing applications and decide the amount taken from each under the status rules. System 3 supplies the cause of the resulting damage. **System 36 validates against current state** before committing, including that the target still holds the status and that each quantity is available from its contribution after previous consumption, expiry or other changes. It commits the state and the System 4 record together. Passing trace checks alone does not prove a contribution is still available.

### Combat Record Kinds

Besides the status records above, the MVP stream has these kinds. Each is committed by its owner at the boundary shown, with a typed origin.

| Record kind | Required payload | Submitted by and links |
|---|---|---|
| `ActionStarted` | Actor instance, the `rootActionId` (System 12's action identity) and the action origin: item, `EnemyMove` or `StatusTick`. | System 12. No link required. |
| `Damage` | Target instance, damage occurrence ID shared by its steps, typed origin, resolved damage, and for this step the actual shield and HP before and after. | System 36 on behalf of System 3. Same shape as `DetonationDamage` without the consumption link. |
| `ActionSkipped` | Actor instance, `rootActionId` and reason `stunned` or `no_ready_item`. | System 12. The owners of Systems 11 and 13 decide when a turn is skipped. |
| `UnitDowned` | Unit instance (hero or enemy) and the lethal record it links to. | System 36. The link must resolve to a `Damage` or `DetonationDamage` record whose HP after is zero. |
| `CombatEnded` | Encounter ID, gameplay outcome, reason and `terminalCauseSeq`. | System 12. `terminalCauseSeq` links the lethal record that decided the outcome; it is null for a draw or an action-limit result. |

`Heal` and `ShieldGained` are deferred by name, as in System 3. A no-op never appears as a state change; a turn that did nothing leaves an `ActionSkipped`, and an effect that did nothing leaves one of the facts below.

### Placement Baseline

Before recording begins, System 8 supplies an immutable table with one row per item instance in the encounter arrangement: item instance ID, definition ID, hero instance, anchor cell and orientation. It is addressable by item instance ID until the trace is released. A record's origin refers to a placement row, and the breakdown derives the items that never appeared in an origin as unfired, so no per-trigger fact is needed.

### Resolution Stamps

Stamps belong to an **effect**: every record produced by one effect of System 3 shares its stamps. Relationships between the records of one effect (damage to consumption) remain causal links, not stamps.

| Stamp | Meaning and validation |
|---|---|
| `rootActionId` | Reproducible identity of the root action, reusing the action identity System 12 supplies. All records of one chain share it. |
| `depth` | Integer `≥ 0`: the depth of the effect. A root effect has depth `0` and a null `parentEffectSeq`. Any other effect has its parent effect's depth plus `1`. |
| `parentEffectSeq` | Null for a root effect. Otherwise the sequence of the final mutation record of the effect that triggered this one, with `1 ≤ p < s`, in the same encounter and the same `rootActionId`. A fact is never a parent. |
| `effectOrdinal` | Integer `≥ 1`: the count `k` of effects taken from the queue. It is strictly greater than the parent effect's ordinal (gaps allowed) and never decreases within one `rootActionId`. |

Stamps are required by record kind: on `StatusApplied`, `StatusConsumed`, `Damage`, `DetonationDamage` and the four facts below. They are not carried by `ActionStarted`, `ActionSkipped`, `StatusExpired`, `UnitDowned` or `CombatEnded`. A missing, extra or inconsistent stamp is an invalid record: a technical stop in every build, because System 3 generates the stamps deterministically and a wrong one is a defect. Consumers measure the size of a combo as the maximum `depth` and the highest `effectOrdinal` of one `rootActionId`. Occurrence IDs for an enemy move, a status tick and a detonation are the pair `(rootActionId, effectOrdinal)` of the effect that produced them; the wire encoding is an architecture decision.

System 3 requests four non-mutating fact kinds. System 4 checks the shape and links below. The rules that produce each fact belong to System 3.

| Fact | Minimum payload | Stamps and links |
|---|---|---|
| `ChainLimit` | Reason `depth` or `count`, `blockedDepth`, effects already processed, `discardedCount`. | `depth` and `effectOrdinal` of the last processed effect, so `depth ≤ D`. Parent: the triggering record when one exists. |
| `ListenerSuppressed` | Reason `once_per_root`, the listener key (resolving to an item instance and its placement row, or to a status definition) and the sequence of the triggering record. | Stamps of the triggering effect. |
| `EffectSkipped` | Reason `target_downed` or `target_invalid`, the effect's typed origin and index, and the target instance. | Its own `effectOrdinal`, because System 3 counts a skipped effect in `k`. Depth and parent of the skipped effect. |
| `DetonationEmpty` | Reason `no_available_units`, the detonation occurrence ID, the typed detonator origin, the target instance and the status ID. | Stamps of the effect that contains the detonation. |

### Enemy Move and Periodic Status Origins

| Origin | Minimum fields and links | Validation and ownership |
|---|---|---|
| `EnemyMove` | Logical enemy instance ID, monster definition ID, stable move ID within that definition, and reproducible action occurrence ID. The event carries the affected target instance. | System 14 supplies the selected move and actor identity. System 3 supplies the action occurrence and affected target. System 4 checks required fields and identifier scope. Systems 14 and 36 verify the move belongs to the enemy definition and is valid for the encounter before commit. An action occurrence distinguishes repeated uses of the same move. |
| `StatusTick` | Affected target instance, status ID, reproducible tick occurrence ID and an ordered nonempty list of distinct source references. Each reference identifies either an earlier same-encounter `StatusApplied` event or an immutable baseline contribution. The referenced target and status match the tick. Each source resolves to its typed origin, including an enemy move or setup origin where applicable. | System 13 determines which contributions cause the tick and their order under its status rules. System 3 supplies the tick occurrence and resolved effect. System 4 checks required fields, distinct references, scope, existence, source kind and target/status matches. System 36 verifies the relevant status and contributions are active at the tick boundary. Tick attribution and its numeric policy belong to Systems 3 and 13. |

An explicit `EncounterSetup` origin identifies the encounter and a stable setup source key supplied by System 12. It is valid for a baseline status granted by setup. A tick can refer to that baseline contribution and reach its setup origin without inventing an item. System 2 and System 14 must define stable move IDs before the enemy-move integration fixture can run. System 13 must specify tick contribution selection before the multi-contributor tick fixture can run. The event schema and single-source fixtures above are required for the System 4 contract.

### Damage and Mutation Records

System 3 calculates resolved damage. System 36 stages each changed damage field in sequence, with one post-step view per event. The shield step and HP step share a damage occurrence ID and origin. Each record includes both before/after pairs with the unaffected field unchanged. The HP step's historical view derives downed state immediately at HP zero. Keep resolved damage distinct from actual shield absorbed and HP lost. The resolved damage is a property of the occurrence and is repeated on each step, so consumers must sum only the actual shield lost and HP lost, never the repeated resolved damage. System 4 validates step chaining: the shield after of the shield step equals the shield before of the HP step, and the actual losses of the steps together do not exceed the resolved damage. How the credit for damage is divided between the hero who prepared a status and the hero who detonated it is a metric owned by System 30; System 4 supplies the consumption sources and amounts it needs.

For detonation, System 36 stages the single grouped consumption step first and then all its damage steps. System 4 validates against the committed prefix plus earlier staged records. Publish the entire batch and its ordered views together only after all validation succeeds. Any failure leaves the original stack, shield, HP, sequence and trace unchanged for that batch. Preserve earlier committed events and report the diagnostic outside the trace. A replay of an already accepted System 36 request ID with the same payload creates no new records; the request ID is defined and scoped to the encounter by System 36.

### Status Present at Encounter Start

Before recording begins, the status owner supplies an immutable table of starting contributions. Each row has an encounter-scoped logical contribution ID, target instance ID, status ID, positive starting amount, typed origin and, for a timed contribution, its expiration boundary. Use the real source identity and encounter placement if known and applicable. Use an explicit setup origin for status granted by encounter setup. A consumption refers to a baseline row by encounter ID and contribution ID, distinct from an event reference. System 4 validates that the ID resolves uniquely and the target, status and amount match the contract above. The baseline row remains addressable through the same read interface until the trace is released, even when no stacks remain. No synthetic application event is needed.

### States and Transitions

| State | Trigger | Result |
|---|---|---|
| Initialized | System 12 starts an encounter with an identity and baseline from the owner | Open an empty trace with an immutable baseline. |
| Recording | Owner submits a valid committed fact | Append in sequence and expose the view at that position. |
| Recording | Invalid record or causal link, or the `traceRecordCeiling` would be exceeded | Reject the failing commit and stop the encounter. Move to `Stopped`: close the trace to further writes at the valid prefix and retain it for diagnosis. No `CombatEnded` is written. |
| Recording | System 12 commits the terminal outcome | Seal the trace with exactly one `CombatEnded` fact carrying the encounter ID, gameplay outcome and reason from System 12. A technical stop has a separate diagnostic and no gameplay terminal fact. Consumers may read the sealed record and snapshots. |
| Stopped | Diagnostic reported to System 12 | The trace stays readable as a prefix. Its release follows the same owner rule as for a sealed trace. |
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

Sequences and contribution amounts use signed 64-bit integers. Sequence values are positive and quantities are nonnegative, with source and consumed amounts strictly positive where required above. Before committing, System 4 checks that sequence allocation for the entire pending batch is representable. Sum contribution quantities with checked arithmetic before comparing them to the consumed total. If either operation overflows, reject the pending commit or batch, retain state and the valid prefix, stop the encounter and emit a diagnostic outside the trace. Stamp values use the same signed 64-bit domain, and `depth` and `effectOrdinal` additions are checked. Never wrap or clamp a sequence or validation sum. Gameplay arithmetic and its overflow behavior are owned by Systems 3 and 36.

## Edge Cases

| Case | Resolution |
|---|---|
| Two copies of the same item definition | Use distinct item instance and placement references. Keep the shared definition ID for lookup. |
| A detonation consumes multiple applications or part of a stack | Keep all contributing application references and actual consumed quantities. Do not assign all credit to the most recent source by default. |
| Enemy damage, periodic status damage or encounter setup has no firing item | Use the relevant typed origin. Include real causal links when available. |
| Damage is absorbed or exceeds remaining HP | Keep the actual shield and HP changes distinct from the resolved damage amount supplied by System 3. Only emit a no-change fact when the consuming rule needs it. |
| A link is missing, forward, wrong-kind or from another encounter | Reject the failing commit and stop the encounter. Keep the last committed historical view and valid prefix readable. Emit the diagnostic separately from the deterministic trace. |
| A `ChainLimit` is published mid-encounter | A normal committed fact. The trace stays open, later actions continue to append, and no gameplay terminal fact is produced by it. |
| A stamp is missing, extra, points to another `rootActionId`, points to a fact, has a `depth` that is not the parent effect's plus `1`, or has an `effectOrdinal` that is not greater than the parent's | Invalid record: reject the commit and stop the encounter with the valid prefix kept, in every build. |
| The same source is consumed twice and its total passes the original amount | Rejected by the per-source ledger even if each record passes alone. |
| An item never appears in any origin during a fight | The breakdown derives it as unfired from the placement baseline. No fact is written. |
| The encounter would exceed `traceRecordCeiling` | Technical stop. The trace is never truncated. |
| A unit is downed | `UnitDowned` links to the lethal damage record. `CombatEnded.terminalCauseSeq` links the record that decided the outcome. |
| Playback skips or subscribes after simulation is ahead | Read committed events and historical views by cursor. Skip fast-forwards the view to the next Detonation root action and finally to the correct end state without changing event data or summary inputs; it relies on every root action's records being contiguous in the sequence (System 29 asserts this), which this log must guarantee. |

## Dependencies

No upstream GDD is required to construct and validate the log. The listed interfaces describe the contract for downstream GDDs.

| System | Contract |
|---|---|
| 3, 9, 10, 13, 14, 36 | Resolution supplies ordered actions, origin context, actual modifiers, status provenance and the four resolution stamps. System 3 also requests the `ChainLimit`, `ListenerSuppressed`, `EffectSkipped` and `DetonationEmpty` facts. System 36 owns mutable state, atomic commit and historical state views. |
| 11, 12 | Turn Order supplies the selected action context. Its forecast is read-only. Combat Resolution owns encounter start and end and submits `ActionStarted`, `ActionSkipped` and `CombatEnded`. |
| 1 Seeded RNG | No runtime dependency. The log never consults RNG. The stream identities and positions appear only in parity comparisons. |
| 2, 5, 8 | System 2 supplies definition identities to producers. Run State owns the whole-run snapshot and the release of encounter traces. Backpack supplies the placement baseline table, one row per item instance. |
| 27, 29, 30, 32 | Read committed facts and historical views for HUD, payoff, breakdown and audio. Systems 29 and 30 own pacing and summary metric definitions, respectively. |
| 23, 24, 35 | Use a deterministic trace for validation, later replay or persistence integration. System 35 compares runs in MVP. System 23 owns persistent replay and System 24 owns save policy. |

## Tuning Knobs

System 4 has no gameplay tuning knobs. `traceRecordCeiling` is a rules-versioned safety bound (provisional `20,000`, safe range `5,000` to `100,000`, to be set from measured encounters in the combat spike); changing it bumps the rules version. Playback speed, audiovisual caps and breakdown filters are owned by consumers. The architecture may choose a storage budget or a historical-view representation once measured against representative encounters, while preserving trace integrity and consumer requirements.

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
- [Unity version reference](../../docs/engine-reference/unity/VERSION.md) pins `6000.6.3f1`. The Unity Core module reference (`modules/core.md`) is absent and the 6.6 content of `modules/` is not verified; the C# language level is NOT SOURCEABLE until an ADR records it. The design uses plain C# boundaries, with live integration and storage feasibility to verify during the spike.

## Acceptance Criteria

1. **Given** identical initial state, rules version, catalog, seed and player choices, **when** the two-hero spike runs in the plain .NET test run, the headless run and Unity EditMode, **then** the canonical dump of the baseline tables, records and per-position views is byte-identical in all three and its hash equals the golden hash stored with the fixture. The canonical dump uses a fixed field order, invariant integer formatting and ordinal IDs, and lists ordered fields in their declared order and keyed fields by key. On a mismatch the test reports the first differing position and field.
2. **Given** Hero A applies 3 Ấn and Hero B applies 2, **when** Hero B detonates 4 against shield 5 and HP 3, **then** the trace resolves damage → consumption → both applications, preserves both heroes, item instances, placements, status, consumed quantities (A:3, B:1) and applied modifiers after the status is gone, and exposes the grouped view with one unit remaining. **Given** an injected failure in the last damage step, **then** no part of the detonation batch publishes.
3. **Given** a starting status contribution in the baseline table, **when** it is consumed by a detonation and the active status is removed, **then** the damage still resolves through consumption to the original baseline ID, target, status, starting amount and typed origin, the trace has no synthetic application event, and the baseline stays readable until release.
4. **Given** the parameterized cases valid change, no-op, missing link, forward link, wrong-kind link, other-encounter link, mismatched target, mismatched status, incorrect quantity sum, and a source consumed beyond its original amount (the per-source ledger), **when** each is submitted, **then** the valid change emits one record and the no-op none, and every invalid case leaves `n` unchanged, moves the encounter to `Stopped`, keeps the prefix and the last view readable, and reports a diagnostic outside the trace. Whether a contribution is still available in current state is tested under System 36.
5. **Given** an enemy instance uses a defined move twice, **when** the damage records are read, **then** each identifies the enemy instance, monster definition, move ID and a distinct occurrence ID `(rootActionId, effectOrdinal)`. **Given** a status tick from an earlier application and one from a setup baseline contribution, **then** each identifies its target, status and tick occurrence and resolves its source to the right typed origin. Wrong links fail under criterion 4. The enemy fixture runs once Systems 2 and 14 define stable move IDs, and the multi-contributor tick fixture once System 13 defines selection.
6. **Given** the log's core assembly, **when** it is built, **then** its asmdef sets `noEngineReferences: true` and references only an allowlist that excludes `UnityEngine`, `Unity.Collections`, `Unity.Mathematics` and `Unity.Burst`; a plain .NET build of the same sources compiles; the same test suite passes under `dotnet test` and Unity EditMode; and a reflection test finds no non-readonly static field.
7. **Integration, when Systems 27 and 29 exist. Given** a sealed trace with several HP changes, **when** a reader plays, speeds up or skips, **then** the HUD uses the historical view at each reached position and ends in the same state, and the underlying trace is equal for all paths.
8. **BLOCKED — System 30 integration. Given** the breakdown after combat, **when** it follows damage and status links, **then** event, baseline and placement references still resolve after the inventory has changed. An item absent from damage origins is not necessarily unfired; Systems 4/9 must reconcile `ItemFired` and passive items before an unfired list can be verified.
9. **Given** shield 5 and HP 10, **when** a `Damage` of resolved 3 lands entirely on shield, **then** one record shows shield 5 → 2 and HP 10. **Given** the same start and a resolved 20, **then** two records share the damage occurrence and origin, show shield 5 → 0 then HP 10 → 0, the HP step's shield before equals the shield step's shield after, and the summed actual losses are 15, not 40. A rejected second step leaves neither published.
10. **Given** a test seam that sets the next sequence to `Int64.MaxValue − 1`, **when** a batch of three records, or a consumption whose source amounts overflow the checked sum, is submitted, **then** the commit is rejected, `n` is unchanged, the encounter is `Stopped`, the prefix and last view stay readable and the diagnostic is outside the trace.
11. **Given** an expiring Ấn contribution and a separate contribution not yet due, **when** the target completes the due action, **then** one `StatusExpired` records the expired remainder with its actual source and `expiryActionOrdinal`, leaves the newer contribution active, and precedes any outcome at that boundary.
12. **Given** the Ấn detonation fixture, **when** its records are read, **then** the consumption and both damage records of that effect share `rootActionId`, `depth 0`, a null `parentEffectSeq` and one `effectOrdinal`. **Given** a reaction triggered by that detonation, **then** it has depth 1, its `parentEffectSeq` is the final mutation record of the detonation, and its `effectOrdinal` is greater than the detonation's.
13. **Given** the parameterized stamp errors missing stamp, stamp on a kind that carries none, parent in another `rootActionId`, parent that is a fact, wrong depth, and an ordinal not above the parent's, **when** each is submitted, **then** it is rejected, the encounter is `Stopped` and the prefix stays readable.
14. **Given** each of `ChainLimit`, `ListenerSuppressed`, `EffectSkipped` and `DetonationEmpty` with its minimum payload, **when** submitted, **then** it appends at the next sequence and reuses the preceding view; a missing required field is rejected. **Given** a `ChainLimit` followed by further actions, **then** the trace stays open, later records append with contiguous sequences, and no `CombatEnded` is written.
15. **Given** the terminal outcome, **when** System 12 commits it, **then** exactly one `CombatEnded` is appended with a `terminalCauseSeq` that resolves to the lethal record (null for a draw or an action-limit result), and every later append is rejected. **Given** a technical stop, **then** the trace is `Stopped` with no `CombatEnded`.
16. **Given** a rejected batch, **when** a later valid record is committed, **then** its sequence is `n + 1`, with no gap.
17. **Given** a producer that mutates its buffer after submitting a record, **when** the record and its view are read, **then** they are unchanged and are exposed as read-only.
18. **Given** a duplicate baseline contribution ID, an origin that names an unknown item instance, or a source reference whose `{kind, id}` is not found, **when** submitted, **then** it is rejected under criterion 4.
19. **Given** a `UnitDowned` that links to a record whose HP after is not zero, or a `CombatEnded` whose `terminalCauseSeq` does not resolve, **when** submitted, **then** it is rejected under criterion 4.
20. **Given** a test configuration with a small `traceRecordCeiling`, **when** the next record would exceed it, **then** the encounter stops with a technical stop and the trace is not truncated.
21. **Given** the same encounter run twice in one process, **when** logical IDs are compared, **then** they are equal, and no ID is derived from a Unity instance ID, a random value or the wall clock.

## Open Questions

| Question | Owner | Resolve by |
|---|---|---|
| Which event encoding, canonical dump format, ID scheme and historical-view representation (lazy reconstruction, facts reusing the preceding view by reference) support live and headless parity within the measured budget? | Technical Director and engine programmer; ADR | Architecture review and combat core spike. |
| What are the memory and frame budgets for traces, and what real value should `traceRecordCeiling` take? | Technical Director and performance analyst | Combat core spike. |
| Which ADR records the core assembly's asmdef settings, C# language level, .NET profile and the choice between sealed classes and `readonly struct`? | Technical Director and lead programmer | Before the combat spike implementation. |
| Which additional attribution and event kinds are needed for future periodic, refresh, cleanse, heal and shield effects? | Systems 3, 4 and 13 | Before introducing those effects. |
| Which facts and derived metrics must survive after the encounter breakdown closes? The player chose actual enemy HP and shield lost for System 30's contribution metric, with detonation damage credited to the detonating item and consumed setup sources listed separately. Confirm the producer and aggregate interfaces without allocating that damage twice. The player also chose a small personal best that persists across launches as an MVP exception; its storage owner remains open, while the broader records system and replay remain deferred. This does not by itself retain a trace. | Systems 4, 5, 30, 31 and 35 | Before implementing run summaries, durable records and trace release. |
| Can a damage occurrence ID distinguish separate hits on the same target by one effect, while grouping the shield and HP records of each hit? System 30's largest-hit metric is blocked until this is unambiguous. | Systems 3, 4, 12 and 30 | Before implementing the largest-hit metric. |
| System 30's user-selected longest chain counts distinct committed effects that actually mutate state per completed root action. How are completed roots and mutating effect identities exposed, and how does this count differ from this GDD's maximum depth and highest ordinal, which can include gaps or nonmutating effects? Do not treat those diagnostics as the selected count. | Systems 3, 4, 12, 29 and 30 | Before chain metrics and payoff integration. |
| Do Systems 8 and 12 confirm the placement baseline table and the `ActionStarted`, `ActionSkipped` and `CombatEnded` submissions? | Systems 8 and 12 | When System 8 is designed. |
| System 9 ([item-trigger-and-cooldown.md](item-trigger-and-cooldown.md)) asks for: an `ItemFired` record (item origin, trigger kind, the owner's action ordinal, and for a cooldown or reaction item the new `nextReadyAt`) published in the same transaction as the cooldown change, with no effect stamps, and allowed as the parent of a reaction (the rule that a parent is a mutation record, never a fact, needs an exception or `ItemFired` must be classed as a mutation); `ActionStarted` origins `HeroAction` and `CombatStart` in place of an item origin; the owner's ordinal `a` on `ActionStarted` and `ActionSkipped`; and a change of the placement-baseline wording that unfired items need no per-trigger fact (they are now the items with no `ItemFired`). | Systems 4 and 9 | Before System 4 is re-reviewed. |
| System 11 ([turn-order.md](turn-order.md)) asks that `ActionStarted` and `ActionSkipped` carry the time advanced `Δ` and the acting unit's gauge before and after, so that no separate gauge record is needed (a pre-fight action carries `Δ = 0` and no spend). | Systems 4 and 11 | Before System 4 is re-reviewed. |
| System 15 ([party-and-heroes.md](party-and-heroes.md)) asks that the hero instance IDs (equal to the party slot) appear in the placement baseline, and that each hero's innate item has a baseline row with anchor row `−1`, anchor column `0` and a flag `innate`. | Systems 4 and 15 | Before System 4 is re-reviewed. |
| System 14 ([enemy-and-intent.md](enemy-and-intent.md)) asks for one `IntentsBound` record per enemy at the start, listing its whole bound sequence (move ID and target per entry), so that the breakdown can name the deciding intent; and that a windup is an `ActionStarted` with the `EnemyMove` origin and no effects. | Systems 4 and 14 | Before System 4 is re-reviewed. |
| System 5 keeps per-room aggregates for the run summary (tier taken, danger, actions used, peak hit, outcome and reason) because System 4 seals each encounter's trace; the fields System 4 should expose for the summary and the decisive-intent line (MVP-required) are to be decided with System 30 ([run-state-and-flow.md](run-state-and-flow.md) Rule 19). | Systems 4, 5 and 30 | Before System 4 is re-reviewed. |
| Required by System 35 ([balance-simulation-harness.md](balance-simulation-harness.md), 2026-11-14): a trace sink or an explicit release call right after the fight's summary callback, a value for `traceRecordCeiling`, a memory budget for 100,000 fights, and a counters-only mode so the harness reads root-action `(depthMax, N)` and actions per fight without retaining the trace. | Systems 4, 36 and 35 | Architecture review. |
