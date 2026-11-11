# Status Effects

> **Status**: In Review
> **Design Review**: MAJOR REVISION NEEDED. User decisions incorporated 2026-11-11. Pending independent re-review.
> **Creative Director Review (CD-GDD-ALIGN)**: APPROVED 2026-11-10 for pillar alignment. Status policies and technical contracts remain provisional.
> **Author**: Codex studio design session, with systems design, gameplay engineering and Creative Director consultation
> **Last Updated**: 2026-11-11
> **Last Verified**: 2026-11-11 (document consistency only)
> **Implements Pillar**: Arrangement Is the Thinking and The Payoff Is the Product.

## Summary

Status Effects describes temporary conditions that one hero or enemy can apply and a later effect can use. The design preserves each contribution's source so the player can follow a setup into its payoff. The two-hero spike uses a timed, uncapped debuff with oldest-first consumption.

> **Quick reference** — Layer: `Feature` · Priority: `MVP` · Key deps: `Effect Resolution Model, Game Data Definitions, Combat State and Step Model`

## Overview

System 13 owns the rules for applying, stacking, ticking, consuming and removing status contributions. System 2 supplies authored identities and parameters; System 3 orders and resolves effects; System 36 owns mutable state and commits validated changes with System 4's events. System 12 owns encounter lifecycle and outcomes. System 3 is Designed (in revision). Systems 4, 12 and 36 are In Review. Interfaces here are provisional until those designs agree.

The MVP includes only the statuses needed for its three to four synergy types. This GDD specifies Ấn for the two-hero spike; the full roster remains open. The prototype contains Poison, Bleed and Stun and directly changes Unity-coupled state. Its shield bypass, stack decay and float gauge pushback are observations for migration, not adopted MVP rules.

## Player Fantasy

The player sees hero A prepare a target and hero B later cash in that preparation. A status is a visible opportunity, with enough information to understand the contribution that produced a payoff or why a setup failed. Hero order and item arrangement drive the plan before the automatic fight. Status feedback must preserve the source of the setup as the payoff grows.

## Detailed Design

### Core Rules

1. **Track contributions.** Each contribution has an encounter, target, status ID, positive integer remainder, immutable source reference and expiration boundary. The source is its `StatusApplied` event or its encounter baseline contribution. The ledger belongs to System 36. System 13 supplies status policies and System 3 orders and resolves effects.
2. **Spike status Ấn.** Each valid application adds its full positive quantity as a new contribution. There is no gameplay stack cap. Numeric bounds remain technical invariants, and overflow fails before publication. Reapplying Ấn does not refresh or replace existing contributions. A request to refresh or replace Ấn is invalid; it emits no application event. Ấn has no periodic damage tick in the spike. Other status types must specify any refresh, replacement, periodic damage and their event contracts before use.
3. **Duration belongs to the contribution.** The spike fixture uses two completed actions of the target as the working duration. This trial value can be tuned before the roster is finalized. Do not count the action in progress when the contribution was added, even if the target is acting. Baseline contributions start counting at the target's first completed action. At the target's due action boundary, after its effect and reaction queue drains and before System 12 checks the outcome, remove the remainder of each due contribution. An action that finishes with effects skipped counts. Cleanup at encounter end discards the runtime ledger without rewriting the retained trace.
4. **Consume oldest sources first.** For one target and status, use only active contributions and take the requested positive amount, up to the available total. Take from baseline contributions first in stable contribution-ID order, then applied contributions in ascending event sequence. This fixes a stable order for sources that started in the same baseline. If available total is zero, do not consume or detonate. If the request exceeds the available total, consume all available units and base damage on that actual amount. System 36 verifies each source and its remaining balance before commit.
5. **One consumption record and one detonation transaction.** A single `StatusConsumed` event identifies all sources and quantities for one target and status, with one view after all selected contributions change. System 3 calculates damage using the actual consumption and uses the same occurrence, target and detonator as that event. System 36 stages consumption and any shield and HP changes together. If any calculation, state or log validation fails, none of those steps publish and the stack is unchanged. System 12 stops and returns to menu with a diagnostic and no reward. Earlier committed actions remain readable.
6. **Expiry and historical attribution.** Record actual expired amounts for one target and status in a `StatusExpired` event with ordered source references, reason and boundary identity. The post-event view removes the expired amounts together. Keep source records and baseline references through the retention window. Expiry is a state change, not a detonation and not a damage trigger for Ấn. For future periodic statuses, System 13 supplies actual ordered active sources and System 3 uses the `StatusTick` origin in System 4.

### States and Transitions

| State | Trigger | Result |
|---|---|---|
| Absent or active | Valid Ấn application | Add a distinct contribution with its own expiration boundary and record the amount added. |
| Active | Valid detonation with a positive available total | Stage oldest-first consumption and linked damage. Publish the complete batch together. |
| Absent | Detonation requests a positive amount | No consumption or detonation damage event. |
| Active | One or more contributions become due | Remove each due remainder for the target and status in one expiry step. Other contributions remain active. |
| Any | Invalid input, overflow or failed batch validation | Publish none of the failed mutation or its events. System 12 handles the technical stop. |

### Example Provenance Fixture

Hero A applies 3 units of Ấn to enemy X, recorded as `A1`. Hero B applies 2 units, recorded as `B1`. While both are active, hero B requests 4 and consumes `A1: 3` and `B1: 1` in that order. One `StatusConsumed` record leaves one unit from `B1`. The spike's working damage coefficient is 2 per consumed unit, so resolved damage is 8 before shield and HP allocation. Each damage step links back to that one consumption record. If damage validation fails, both original balances remain 3 and 2 and no consumption or damage event publishes.

## Formulas

For one target and status, `rᵢ` is the positive active remainder of source `i`, `S = Σrᵢ` is its checked total, and `q` is the positive requested amount. The actual consumption is `C = min(q, S)`. Walk eligible sources oldest first. For each source, take `cᵢ = min(rᵢ, C − Σcⱼ)`, where the sum is over earlier selected sources. Emit only positive entries and stop once their checked sum equals `C`. The remainder is `S' = S − C`. Use signed 64-bit quantities and checked arithmetic per Systems 4 and 36. A nonrepresentable quantity is a technical error, not a stack cap.

For duration, `a₀` is the target's number of completed actions at the start of a contribution's counting window. The window opens after the action during which it was applied, or at encounter start for a baseline contribution. With the spike duration `L = 2`, expire the remainder after the target's completed action count reaches `a₀ + L`. Application during another actor's action has no effect on the target's count at that boundary. Apply expiry after the target's queue drains, before the outcome check.

## Edge Cases

| Case | Handling |
|---|---|
| A second Ấn application arrives while another is active | Add the full new contribution with its own expiry. Do not refresh the older source. |
| A detonation asks for more than the target holds | Consume all active sources oldest first and use actual consumption for damage. An empty ledger yields no detonation. |
| Baseline and applied sources are both active | Process baseline sources by stable ID before application events in sequence order. |
| A source is duplicate, stale, cross-encounter or belongs to another target/status | Reject the pending request and leave state, sequence and committed trace unchanged. |
| Multiple sources expire at the same boundary | Remove the actual remaining amounts in one `StatusExpired` event per target/status, with sources in the same stable order. |
| Damage calculation or validation fails after staged consumption | Publish no part of the detonation batch. Preserve stack and the valid trace prefix. |
| A future periodic status kills a target | It goes through System 3. System 12 completes the current action before evaluating outcome. The future status's own timing and origin policy must be specified. |

## Dependencies

| System | Contract and status |
|---|---|
| 2 Game Data Definitions | Designed, pending review. Supplies stable definition IDs and static authored data; the structured effect/status payload remains open. |
| 3 Effect Resolution Model | Designed (in revision). Orders ticks, effects and detonation through the FIFO queue and supplies occurrence IDs. The spike uses the documented detonation transaction and working damage formula; broader FIFO and production damage policies remain provisional. |
| 36 Combat State and Step Model | In Review. Owns status ledger, validation, mutations, atomic commit and historical views. |
| 4 Event Log | In Review. Defines application, detonation consumption, damage, tick origin and baseline provenance. Defines the spike's grouped expiry record. Cleanse and other future kinds require coordinated design. |
| 12 Combat Resolution | In Review. Owns completed-action boundaries, outcome and technical stops. Due expiry is processed after the current queue drains and before outcome. |
| 9, 11, 14, 15 | Trigger, turn-order, enemy action and hero systems supply boundary context under their pending GDDs. |
| 27, 29, 30, 35 | HUD, payoff and breakdown consume committed historical facts; the harness verifies reproducibility. |

## Tuning Knobs

The spike uses two completed target actions per Ấn contribution as a working fixture duration and 2 resolved damage per unit consumed as a working fixture coefficient. Neither value fixes the production roster or balance. Ấn has no gameplay stack cap. Future statuses specify their own timing, units and trigger behavior.

## Visual/Audio Requirements

Systems 27, 29 and 32 present status application, tick and detonation from committed events. The Art Bible proposes a shared status mote toward the target and a pulse and burst for detonation; hero items use a source plate echo and enemy actions use an intent glyph. Buff/debuff meaning should have glyphs or chevrons and signed values as well as color. The player should be able to locate the affected target and the contributing hero and item while HP, intent, turn marker and the grid remain readable. Payoff continues from the combat scene and must not cover the protected information layer. Art Bible sign-off, glyph roster, numeric contrast and flash thresholds remain unverified and are not treated as approved limits here.

## Game Feel

Preparing a target should read as a setup, and consuming it should feel like a consequence of the player's arrangement. Feedback remains intelligible as a chain grows. A technical stop is communicated separately from an ordinary combat outcome.

## UI Requirements

The combat HUD and breakdown need status identity, actual active amount, timing where applicable, target and traceable contributing sources from committed events and historical views. Systems 27, 29 and 30 own screen design, localization, playback and breakdown. Their UX specs must specify presentation without exposing a future outcome before its event.

## Cross-References

- [Game Concept](game-concept.md) defines the automatic-combat fantasy, pillars and two-hero checkpoint.
- [Systems Index](systems-index.md) defines MVP status scope and the engine-free ownership split.
- [Effect Resolution Model](effect-resolution-model.md) defines the proposed effect queue and same-target detonation example.
- [Combat Resolution](combat-resolution.md) defines the completed-action outcome boundary and technical-stop handling.
- [Event Log](event-log.md) specifies source references and baseline provenance. Its re-review is pending.
- [Game Data Definitions](game-data-definitions.md) defines static IDs and the runtime-state boundary.
- [Art Bible](../art/art-bible.md) sets visual direction; its revised sign-off is still pending.
- [Unity version reference](../../docs/engine-reference/unity/VERSION.md) pins `6000.6.3f1`. The local `modules/scripting.md` reference is absent; engine integration and cross-runtime parity remain unverified.

## Acceptance Criteria

1. **Given** a valid source applies 3 Ấn and a second source applies 2, **when** both commit, **then** the ledger contains separate contributions totaling 5 and each has a matching application record. Reapplication neither refreshes nor replaces the first contribution. An application of 21 units to that ledger succeeds with a new total of 26.
2. **Given** the two-source fixture and a request for 4, **when** the detonation commits, **then** one `StatusConsumed` lists `A1: 3`, `B1: 1`, the post-event view shows 1 remaining from `B1`, and the linked damage resolves to 8 before shield. A request for 9 with the same starting ledger instead consumes 5 and resolves 10 damage. With no active contribution, there is no consumption or damage event.
3. **Given** an earlier baseline contribution and a later application on the same target, **when** consumption can be satisfied by the baseline alone, **then** only that baseline source is consumed and its historical origin remains retrievable.
4. **Given** an out-of-range amount, duplicate source, stale selection, wrong target/status, or a checked sum overflow, **when** validation runs, **then** the failed request changes neither the ledger nor the trace.
5. **Given** Ấn is applied during target action 4 and has 3 remaining, **when** that action and action 5 complete, **then** the contribution is still active. After the queue of action 6 drains, emit `StatusExpired` for the 3 remaining units before checking outcome. Its historical origin remains available. A later application with a later deadline stays active.
6. **Given** the two-hero spike with identical inputs and implemented dependencies, **when** Unity, headless and tests execute it, **then** compare ordered events, source references, amounts and historical views per System 4.
7. **Given** the two-source fixture and an injected damage validation failure, **when** the batch fails after consumption has been staged, **then** the original balances remain 3 and 2, no detonation record or state change appears, and the prior prefix is readable when System 12 reports the technical stop.
8. **Integration, when Systems 27 and 29 exist. Given** application, expiry and detonation records, **when** played or skipped, **then** the HUD uses the corresponding historical views. Evaluate visibility of status, source, HP, intent, turn marker and grid under the applicable UX/art criteria and retain the screenshot. Verify the core assembly has no UnityEngine references separately.

## Open Questions

| Question | Owner | Resolve by |
|---|---|---|
| Which additional statuses, units and tuning values do the MVP synergy types need? | Game designer, Systems 3, 10 and 13 | Before production status content. |
| Which future statuses need refresh, replacement, cleanse or periodic ticks, and which event kinds and source policies do those require? | Systems 3, 4, 13 and 36 | Before adding those operations to the spike contract. |
| How should sources and duration be represented in a persistent save? | Systems 5, 13, 24 and 36 | Before post-MVP save integration. |
