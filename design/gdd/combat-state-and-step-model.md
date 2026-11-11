# Combat State and Step Model

> **Status**: In Review
> **Design Review**: MAJOR REVISION NEEDED. User decisions incorporated 2026-11-11. Pending independent re-review.
> **Creative Director Review (CD-GDD-ALIGN)**: APPROVED 2026-11-10 for pillar alignment. Transaction, checkpoint and outcome contracts remain provisional.
> **Author**: Codex studio design session, with systems design, engineering and Creative Director consultation
> **Last Updated**: 2026-11-11
> **Last Verified**: 2026-11-11 (document consistency only)
> **Implements Pillar**: Arrangement Is the Thinking and The Payoff Is the Product.

## Summary

Combat State and Step Model keeps the current facts of a fight and records each committed change with a matching event and historical view. It lets the player watch a setup and payoff in the order they happened, with the displayed HP, intent and status matching that moment. State and event publication follow the combat transaction contract below. Checkpoints for persistent resume remain a separate design decision.

> **Quick reference** — Layer: `Core` · Priority: `MVP` · Key deps: `Game Data Definitions, Seeded RNG, Event Log`

## Overview

System 36 owns the encounter's mutable combat state and its mutation boundary. System 3 decides and orders effects, System 11 chooses the next actor, System 12 owns encounter lifecycle and outcomes, and System 13 defines status policies. System 4 owns the event schema and trace. System 5 owns the whole-run snapshot. This GDD defines how those owners exchange validated facts; it does not settle their gameplay rules.

System 2 has a draft GDD. Systems 1, 4, 12 and 13 are In Review. System 3 is Designed (in revision). Their contracts require joint review. The prototype exposes mutable combatant fields, uses a frame-timed float gauge and directly changes status and HP through Unity-coupled code. Those fields are migration inputs, not proof of the required engine-free model.

## Player Fantasy

The player arranges a team and sees the machine they built act in a comprehensible order. Hero A's setup remains visible as hero B detonates it, and the displayed result matches what actually happened in the fight. After a loss the same history helps the player understand which placement, intent or action led there.

## Detailed Design

### Core Rules

1. **State and identity.** Store reproducible logical encounter and combatant IDs, HP and maximum HP, shield, integer gauge, cooldowns, binding intents and active status contributions with their remaining amount, expiry and source. Reference the frozen arrangement, catalog and rules version. Life state is derived from HP in every live and historical view: an actor is alive if and only if HP is greater than zero. There is no separately mutable life flag or extra mutation step for becoming downed.
2. **Preparation and baseline.** Pre-fight edits refresh preview without advancing live combat state or RNG or changing a binding intent. On start, System 12 supplies the final valid arrangement and adjacency context. System 36 validates the baseline and System 4 opens the trace against that immutable view. Setup failure follows System 12's technical stop policy.
3. **Step and mutation boundary.** A step is one semantic state transition represented by one typed mutation event and its immutable post-step view. HP and shield changes are separate steps. A single `StatusConsumed` or `StatusExpired` step changes all listed contributions for one target and status together and has one view after the group. An unchanged value emits no mutation event. A nonmutating fact can reuse the preceding view.
4. **Atomic publication.** Validate the entire proposed batch against a private working state and the matching provisional event prefix. Shield and HP changes have ordered views even when published as one damage batch. A detonation batch includes one consumption step followed by all its damage steps. The staged damage can refer to the staged earlier consumption sequence. Expose state, events, views, cursor and the successful request receipt together only after all checks pass. Any failure leaves all of them at the prior committed boundary, including stack balances. System 12 stops and returns to menu with a separate diagnostic and no reward.
5. **Status and request identity.** For each consumption validate encounter, target, status, distinct source references, checked total and quantities against current availability and expiry. Process requests serially against current committed state. Use a reproducible request ID scoped to the encounter. Replaying an already accepted ID with the same semantic payload returns its prior result without mutating, allocating sequence numbers or emitting events. Reuse with a different payload is an invalid request. A new ID for the same amount can succeed if the current ledger permits it. Keep the receipt for the encounter lifetime and publish it atomically with the accepted operation. Record accepted no-ops as receipts without mutation events so replaying an earlier empty detonation cannot consume a later application.
6. **Historical reads.** Baseline and post-event views cannot be changed through readers and remain addressable through System 4's retention window. An observer only sees committed data. After batch publication, readers can access the intermediate views by sequence, even though they could not see any partial commit. The HUD uses the view at its playback cursor. Storage representation and budgets require architecture validation.
7. **Snapshot ownership.** System 36 supplies its combat slice, including encounter and version identities, committed state, status sources and deadlines, event cursor and active request receipts where needed for consistent continuation. System 5 owns the full snapshot and coordinates System 1 RNG; System 3 holds no state across actions, so it adds nothing to a snapshot. Persistent save and mid-action checkpointing are outside this spike. The MVP failure path retains the committed prefix for diagnostics and abandons the run; it does not attempt to restore the encounter.

### States and Transitions

| State | Trigger | Result |
|---|---|---|
| Preparing | System 12 supplies a candidate setup | Validate identities, definitions and baseline inputs. |
| Active | Start with the final validated arrangement | Establish the trace baseline and accept serialized mutation requests. |
| Active | Valid mutation or batch | Publish each semantic step and its view together with the final state and receipt. |
| Stopped | Technical failure | Publish none of the failed request, preserve the valid prefix and report to System 12. No further mutation is scheduled. |
| Sealed | System 12 commits a gameplay outcome after the current action completes | Stop accepting mutations and retain the trace and views. |

### Interactions and Example

Hero A applies 3 units of Ấn and hero B applies 2. A detonation requesting 4 stages one `StatusConsumed` listing A:3 and B:1. Its view shows B:1 remaining. With the spike coefficient of 2, a target at shield 5 and HP 3 takes 8 resolved damage: the next view shows shield 0 and HP 3, and the final view shows shield 0 and HP 0 with life state downed. System 4 can validate each staged cause against the provisional prefix. All three steps publish together. If the HP step fails validation, no consumption, shield step, HP step or receipt publishes. A downed actor is immediately visible to subsequent legal reactions. System 12 evaluates the encounter outcome after the action queue and due expiry complete.

## Formulas

| Field | Domain and rule |
|---|---|
| Maximum HP | Signed 32-bit storage, range `1..2,147,483,647`. |
| HP | Signed 32-bit storage, range `0..maximum HP`. Derived alive is `HP > 0`. A lethal HP event immediately exposes downed state in its view. |
| Shield, gauge, cooldown | Signed 32-bit storage, range `0..2,147,483,647`. Gauge and cooldown units and gameplay restrictions belong to Systems 11 and 9. |
| Active status amounts and event sequence | Signed 64-bit storage. Active contribution amounts and event sequence positions are positive. The empty trace cursor is 0. Use checked addition for totals and batch sequence allocation. |

Check intermediate arithmetic before narrowing or publication. Use a wider intermediate where required or checked arithmetic at the stated width. Reject overflow and out-of-domain changes rather than wrapping or clamping them. Gameplay saturation defined by a rule, such as HP damage stopping at zero, is applied deliberately by that rule before validating the resulting field. The absence of a gameplay stack cap does not relax representation bounds.

## Edge Cases

| Case | Handling |
|---|---|
| One staged step in a detonation fails | Publish none of its consumption, shield or HP steps. Keep the prior view, cursor, trace and stack balances. |
| One consumption updates multiple sources | Publish one step and view for the target/status group. Validate all amounts against the current ledger. |
| Accepted request ID arrives again | Same payload returns the prior result without changes; a conflicting payload is invalid. A distinct ID is validated against current state. |
| HP changes to zero | The same HP step's view reports downed, with no second life-state mutation. System 12 checks outcome after legal reactions and due expiry. |
| Status or sequence arithmetic overflows | Reject the complete pending request before publication. System 12 handles the technical stop. |
| A historical view is requested after later changes | Resolve the original state, arrangement and source references for that event position. |
| An intent target becomes invalid | Skip the affected effect without retargeting or rerolling. The move's other valid effects may still commit. |

## Dependencies

| System | Contract and status |
|---|---|
| 2 Game Data Definitions | Designed, pending review. Supplies immutable catalog, stable definition IDs and authored parameters. |
| 1 Seeded RNG | In Review. Owns seed, stream state and its restore contract; System 5 coordinates the complete snapshot. |
| 4 Event Log | In Review. Owns event kinds, causes, sequence and trace retention. State/event publication and per-event views must align with it. |
| 3 Effect Resolution | Designed. Owns the queue, effect ordering and damage calculations; submits requests and origins. Damage is capped at 2,147,483,647 so it fits the 32-bit HP and shield domain. System 3 holds no state across actions; checkpoint and resume remain outside this spike. |
| 11, 12, 13, 14 | Turn Order selects actors; Combat Resolution owns lifecycle/outcome; Status Effects defines contribution policy; Enemy and Intent supplies binding intents. Systems 12 and 13 are In Review; 11 and 14 have no GDD yet. |
| 5, 8, 10 | Run State owns the whole snapshot. Grid and Adjacency provide the final frozen arrangement and modifiers at encounter start. Their GDDs are pending. |
| 27, 29, 30, 35 | HUD, payoff, breakdown and harness read committed records and the historical state matching their position. |

## Tuning Knobs

System 36 enforces the stated storage and arithmetic domains. Gameplay owners supply their values and units, including damage, gauge, cooldown and status duration. Snapshot storage budgets require architecture and performance evidence.

## Visual/Audio Requirements

System 36 has no direct VFX or audio calls. Systems 27, 29 and 32 use committed events and views to present feedback. The snapshot for an event provides the HP, intent, grid and status state that should be visible at that point; the visual layer must preserve source readability as payoff grows.

## Game Feel

Actions should read as a continuous chain of cause and consequence. The player's setup stays recognizable through the payoff and a loss remains explainable. A playback pause or skip changes presentation timing, not the state history or outcome.

## UI Requirements

System 36 supplies immutable state views for the combat HUD and breakdown. Systems 27, 29 and 30 own the UX and must specify how HP, intent, status sources and event progress are shown. Before combat, Systems 10 and 11 recalculate adjacency preview and forecast for the current arrangement without altering binding intent or live RNG.

## Cross-References

- [Game Concept](game-concept.md) defines the machine-building fantasy and the two-hero checkpoint.
- [Systems Index](systems-index.md) assigns mutable combat state to System 36 and requires integer-only engine-free simulation.
- [Event Log](event-log.md) defines causal validation, atomic shield/HP batch and per-event historical views.
- [Effect Resolution Model](effect-resolution-model.md) defines action ordering, reaction limits (`D = 8`, `N = 256`), the damage formula and the shared detonation example.
- [Combat Resolution](combat-resolution.md) defines the completed-action outcome check, technical stop and preparation/preview boundary.
- [Status Effects](status-effects.md) defines the spike contribution ledger and all-or-nothing detonation.
- [Seeded RNG](seeded-rng.md) defines stream state and snapshot/restore requirements.
- [Unity version reference](../../docs/engine-reference/unity/VERSION.md) pins `6000.6.3f1`. The local `modules/core.md` reference is absent; engine integration and cross-runtime parity remain unverified.

## Acceptance Criteria

1. **Given** valid encounter setup and a typed HP change, **when** the change commits, **then** the state, one matching event, event sequence and immutable post-event view publish together. An unchanged value emits no mutation event.
2. **Given** the Ấn fixture, shield 5 and HP 3, **when** the detonation commits, **then** observers see one atomic batch ordered `StatusConsumed`, shield change, HP change. The consumption view shows one remaining unit; the damage views show shield/HP `(0, 3)` and `(0, 0)`. The last view reports downed immediately. System 12 publishes the outcome only after the action completes. Injecting a failure in the HP step leaves the original stacks, HP, shield, trace cursor and views intact.
3. **Given** an accepted request ID that consumes 3 from a 10-unit source, **when** the same ID and payload are submitted again, **then** the source stays at 7 and no event is added. A new ID for 3 may consume again to 4. A conflicting payload with the old ID, or a new request whose selected amount exceeds current availability, fails without mutation. Replaying an accepted empty detonation after a new application consumes nothing.
4. **Given** a stored historical view and baseline, **when** later live mutations alter HP and status, **then** the original cursor returns its original values and retained source references.
5. **Given** a valid prefix, **when** HP, shield, gauge or cooldown would be outside the 32-bit domain, or a checked status total or sequence exceeds the 64-bit domain, **then** the failing request publishes nothing and System 12 receives a diagnostic without a victory handoff. Include the maximum valid value for each field and the first invalid value above it.
6. **Given** the two-hero spike and identical seed, catalog, rule versions, baseline and decisions, **when** Unity, headless and tests run after the required implementations and RNG ADR, **then** ordered events and historical views match semantically. Include HP, shield, life state, gauge, cooldown, intent and status sources in the comparison.
7. **Given** a status contribution is removed by consumption or expiry, **when** a reader follows an earlier event, **then** its application or baseline origin remains addressable. An invalid source or duplicate source within one request is rejected before publication.
8. **Given** a prepared binding intent, **when** the player edits the arrangement and reads preview, **then** preview reflects the current arrangement without changing live RNG or intent. Start freezes the final validated arrangement. Verify the core assembly has no UnityEngine references separately.

## Open Questions

| Question | Owner | Resolve by |
|---|---|---|
| Which exact gauge and cooldown units and transition constraints apply? | Systems 9, 11 and 36 | Before integrating those producers. |
| Which consistent checkpoint boundaries and validation rules are needed for persistent resume? | Systems 1, 3, 4, 5, 12, 24 and 36 | Before post-MVP save integration. |
| How are views and request receipts stored within measured memory and latency budgets? | Technical Director, Systems 4, 27, 29 and 36 | Architecture review and combat core spike. |
