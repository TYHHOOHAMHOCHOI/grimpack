# Combat Resolution

> **Status**: In Review
> **Design Review**: MAJOR REVISION NEEDED. User decisions incorporated 2026-11-11. Pending independent re-review.
> **Creative Director Review (CD-GDD-ALIGN)**: REVISED 2026-11-10, then APPROVED on re-review for pillar alignment. Technical and outcome contracts remain provisional.
> **Author**: Codex studio design session, with game design, engineering and Creative Director consultation
> **Last Updated**: 2026-11-11
> **Last Verified**: 2026-11-11 (document consistency only)
> **Implements Pillar**: Arrangement Is the Thinking and The Payoff Is the Product.

## Summary

Combat Resolution runs an automatic encounter from its prepared starting state to an outcome. The player watches the three-hero arrangement act against visible enemy intent, then can inspect why the fight was won or lost. The encounter resolves the current action before deciding the outcome, with an explicit result for a simultaneous knockout or a fight that cannot progress.

> **Quick reference** — Layer: `Core` · Priority: `MVP` · Key deps: `Effect Resolution, Item Trigger and Cooldown, Turn Order, Enemy and Intent, Party and Heroes, Combat State and Step Model, Event Log, Seeded RNG`

## Overview

System 12 coordinates encounter preparation, automatic actions, outcome checks and handoff to run flow. System 11 chooses the next actor and forecasts without mutation. Systems 9 and 14 supply hero actions and binding enemy intents, System 3 resolves all effects, System 36 owns mutable combat state and System 4 records committed events and historical views. System 5 owns the surrounding run and reward handoff.

The MVP combat spike must reproduce the same event log and state views across Unity, headless and test execution with identical inputs. The prototype's frame-timed gauge, direct enemy damage, target refresh and victory-first outcome check are observations to audit during migration, not adopted rules. Systems 3, 4 and 36 have draft GDDs, while Systems 9, 11, 14 and 15 are Designed (pending review). Actor selection, producer integration and legal setup inputs still require agreement before implementation.

## Player Fantasy

The player has arranged three heroes as a machine before the fight. They see the intent and expected action order, watch one hero create an opening and another turn it into a payoff, and understand which item placement contributed. A loss should be readable enough to change the next arrangement. The player does not choose actions during combat.

## Detailed Design

### Core Rules

1. **Prepare.** Validate the encounter identity, roster, catalog, editable arrangement and run inputs. System 14 selects binding enemy intents from `intents` before combat. System 10 refreshes the adjacency preview and System 11 forecasts from the current arrangement. Edits and previews neither reroll binding intent nor advance live RNG.
2. **Start.** Validate the last arrangement and freeze its adjacency context. System 36 validates the starting combat state, and System 4 opens the trace from the corresponding baseline. Invalid setup stops with a diagnostic and no victory or reward. Encounter generation and System 36 must specify legal setup rosters.
3. **Run one actor action.** System 11 reports an eligible actor, an explicit gameplay result that no actor can act again, or a technical error. An actor who is merely waiting to reach the gauge threshold does not count as unable to act again. System 9 supplies hero inputs and System 14 supplies the previously selected enemy action. System 3 resolves the effects and reactions. If the enemy's bound target becomes invalid, skip the effect for that target and keep resolving effects with valid targets. Use the original move and consume no RNG to pick a replacement. An intent that loses all targets still uses that actor's scheduled action.
4. **Finish the action before checking outcome.** Drain the action's effect and reaction queue, even after one side's living count becomes zero. Each effect still uses its own eligibility checks, so a living-only effect on a fallen target is skipped. After the queue drains, process the completed actor's due status expiry, including the spike debuff in System 13. Evaluate outcomes using the resulting committed state. No new action starts after a terminal result. An actor action that finishes with all effects skipped counts as completed. Technical stops during action resolution do not count as completed actions.
5. **Decide the outcome.** Apply the ordered outcome table below. The 200-action limit counts completed actor actions across both sides, not reaction events, animation frames or preview steps. A result based on living sides takes precedence at action 200. A gameplay no-actor result while both sides live is a defeat, distinct from a scheduler error. System 4 records one typed terminal fact with the outcome and reason and seals the trace.
6. **Technical stop and handoff.** Invalid setup, failed mutation or batch, arithmetic overflow, invalid scheduler state or a System 3 limit stops the encounter. Keep the valid committed prefix and a separate diagnostic, abandon the current run and return to the menu with a technical error message. Record no gameplay outcome and grant no reward. A failed detonation publishes no stack consumption or damage. A confirmed victory may trigger the System 5 reward flow once per encounter. Draw and defeat have no victory reward, and repeated completion or handoff calls cannot grant a second reward.
7. **Presentation boundary.** Systems 27, 29 and 30 consume committed records and their matching historical views. Pacing, skip and preview cannot advance or reorder simulation or consume gameplay RNG. Visual intensity preserves the player's ability to read HP, intent, the grid and the source of an effect.

### States and Transitions

| State | Trigger | Result |
|---|---|---|
| Preparing | Run supplies encounter inputs | Validate setup and select binding intents. |
| Ready | Valid setup, binding intent and current forecast exist | Edits refresh the preview and forecast. Start freezes the final arrangement and establishes the trace baseline. |
| Resolving | System 11 supplies an eligible actor | Resolve the actor's action, reactions and due expiry, count the completed action and evaluate the outcome table. |
| Terminal | Gameplay outcome from the table | Commit one terminal event, seal the trace and hand off once. |
| Stopped | Technical failure before terminal commit | Stop scheduling, retain the committed prefix if a trace exists, report the diagnostic and return to the menu. |

### Interactions and Example

Hero A applies Ấn through System 3. Hero B later consumes it and detonates damage. System 4 links the damage to both actors through the consumption and application records. If the damage is lethal, the historical view shows HP at zero immediately. The encounter continues the current action's queue before publishing an outcome. The HUD reads the view associated with each event, including any expiry after the queue drains.

## Formulas

Let `a` be the number of completed actor actions in the encounter, initially zero. After the current action's queue and due expiry are complete, set `a' = a + 1`. `T = 200` is the selected limit. `H` and `E` are the counts of living heroes and enemies in the committed state, derived from HP greater than zero. Check the following in order at the completed-action boundary. Before scheduling a new action, an explicit gameplay no-actor result may also end the encounter when both sides are alive. A technical error at either boundary takes the separate Stopped path.

| Order | Condition | Outcome and reason |
|---|---|---|
| 1 | `H = 0` and `E = 0` | Draw, simultaneous knockout. |
| 2 | `H > 0` and `E = 0` | Victory, enemy side defeated. |
| 3 | `H = 0` and `E > 0` | Defeat, hero side defeated. |
| 4 | `H > 0`, `E > 0`, and `a' = T` | Defeat, action limit. |
| 5 | `H > 0`, `E > 0`, and System 11 confirms no actor can act again under gameplay rules | Defeat, no eligible actor. |
| 6 | Otherwise | Continue scheduling. |

A scheduled actor whose effects all become no-ops still increments `a` after the action completes. A terminal encounter has no action 201. System 3 owns the separate reaction depth and processed-effect limits.

## Edge Cases

| Case | Handling |
|---|---|
| Invalid setup or invalid roster reference | Stop before combat. System 16 ([encounter-generation.md](encounter-generation.md)) never produces an empty enemy side (rosters have 1 to 4 enemies in the MVP, the contract cap being 6) and a roster that does not fit the layout is a technical stop. |
| Lethal effect with pending reactions | Publish the lethal state immediately, then finish the current action's remaining legal reactions. Check both sides once the queue and due expiry complete. |
| Binding enemy intent loses its target | Skip the affected effect without retargeting. Continue effects with valid targets. |
| Both sides alive but unable to progress | A confirmed gameplay no-actor result is defeat; a scheduler error stops the run with a diagnostic. |
| Victory on action 200 | Publish victory and its handoff. If both sides remain alive, publish defeat for the action limit instead. |
| Arithmetic, contract or commit failure | Preserve the valid prefix, leave the failed batch unpublished and return to menu with a technical error. |
| System 3 reports a `ChainLimit` | Not a failure. The action counts as completed; process due expiry, evaluate the outcome table and continue with the next actor. |
| Duplicate completion or playback skip | There is at most one outcome and one eligible victory reward handoff. Playback does not schedule work. |

## Dependencies

| System | Contract and status |
|---|---|
| 3 Effect Resolution | Designed. Owns effect order, reaction queue and damage formulas. FIFO queue, reaction limits `D = 8` and `N = 256` (reaching one ends the action with a `ChainLimit` fact and the encounter continues) and the rule that System 3 holds no state across actions are settled there. A technical failure follows the stop policy here. |
| 9 Item Trigger and Cooldown | Designed ([item-trigger-and-cooldown.md](item-trigger-and-cooldown.md), pending review). Supplies the ready items of a hero action in the call order of its Rule 5 (actor, stun check against System 13, ready list, one transaction with `ActionStarted` and `ItemFired`, root effects). Asks System 12 to run one pre-fight action per hero that has combat-start items (not counted in the 200 limit, with an outcome check after each) and to advance each hero's action ordinal on every scheduled action, skipped or not. |
| 11 Turn Order | Designed ([turn-order.md](turn-order.md), pending review). Pure `Next` (actor, time advanced `Δ`) and `Forecast`; System 12 commits the result in one System 36 transaction. A living unit always becomes ready in finite time, so "no actor can act again" cannot occur in the MVP. |
| 15 Party and Heroes | Designed ([party-and-heroes.md](party-and-heroes.md), pending review). Supplies the combat baseline per hero in slot order (max HP, HP, shield, base speed, speed sum, innate item). Every hero is restored to full HP and its initial shield before each encounter, so nothing about HP, shield or downed state carries over. |
| 14 Enemy and Intent | Designed ([enemy-and-intent.md](enemy-and-intent.md), pending review). Draws a fixed sequence of intents per enemy when the room is entered (before the arrangement phase) from the `intents` stream and stores it; draws nothing in combat; hands System 12 the bound action; a stunned enemy keeps its entry; a hit on a downed hero is skipped with no retarget; no reinforcements in the MVP. |
| 13 Status Effects | In Review. Defines the spike debuff lifetime and expiry at the target's action boundary. Status effects resolve via System 3; System 36 commits the changes. |
| 36 Combat State and Step Model | In Review. Owns mutable state, validation, atomic mutation and historical views, including life state used by outcome checks. |
| 4 Event Log | In Review. Owns structured committed events, causal links and trace retention. |
| 1 Seeded RNG | In Review. System 14 owns the binding intent draw from `intents`; playback and preview do not draw. |
| 8 Grid Backpack | Designed ([grid-backpack.md](grid-backpack.md)). System 12 freezes the arrangement when an encounter starts, which destroys any items still in the reward tray, and thaws it after the encounter; rewards (create items, unlock cells) are granted after Thaw. Before calling Freeze with a non-empty tray System 12 and System 25 ask the player to confirm ("Leave behind: X, Y?"), and no encounter starts while an item is being dragged. Combat reads only the frozen hero grids. |
| 10 Adjacency and Synergy | Designed (pending review). Supplies pure arrangement previews (`PreviewPlacement`) and the matching immutable adjacency view that System 12 takes at Freeze and releases at Thaw. |
| 5, 16–18 | Run flow owns outcome handoff. Encounter and reward systems define setup and rewards; their GDDs are pending. |
| 27, 29, 30, 35 | HUD, payoff and breakdown consume committed facts and historical views. Harness supplies deterministic scenarios. |

## Tuning Knobs

The encounter cap is 200 completed actor actions. Changing it changes the result of long encounters and must update the rules version and boundary fixtures. Reaction limits remain owned by System 3.

## Visual/Audio Requirements

The combat display receives committed events through Systems 27, 29 and 32. It should show which hero and item caused setup and payoff while keeping HP, intent, the turn marker and the grid readable. The combat-to-payoff presentation remains continuous and effects stay below the protected information layer. Playback speed and skip belong to System 29. System 12 provides lifecycle and outcome facts and does not invoke presentation APIs. Art Bible numeric budgets remain unverified and are not treated as approved thresholds here.

## Game Feel

Before combat, visible intent and turn order give the player confidence to commit an arrangement. During combat, payoff and its causes remain legible. After a loss, the event trail can explain what happened. A technical stop must be distinguishable from a legitimate game result.

## UI Requirements

System 12 provides readiness, encounter status and outcome facts to the combat HUD and end-of-run flow. Systems 27, 29, 30 and 31 own the screens, playback controls and breakdown. Their UX specs must define how readiness, a draw, defeat due to action limit or no eligible actor, and a technical stop are communicated without hiding the fight's causes.

## Cross-References

- [Game Concept](game-concept.md) defines automatic combat, visible intent, the core fantasy and the two-hero spike.
- [Systems Index](systems-index.md) defines the System 3/12/36 boundary and calls for a turn cap.
- [Effect Resolution Model](effect-resolution-model.md) contains the queue order, reaction limits (`D = 8`, `N = 256`) and the apply-then-detonate fixture.
- [Event Log](event-log.md) defines causal records and historical views. It is In Review.
- [Seeded RNG](seeded-rng.md) defines the intent stream and reproducibility prerequisites. It is In Review.
- [Game Data Definitions](game-data-definitions.md) defines static catalog identity and the separation from runtime state.
- [Unity version reference](../../docs/engine-reference/unity/VERSION.md) pins `6000.6.3f1`. The engine-free rule domain has no local `modules/scripting.md` reference; runtime integration and parity remain unverified.

## Acceptance Criteria

1. **Given** a prepared binding intent, **when** the player rearranges the party, **then** adjacency preview and forecast reflect the new arrangement without changing the intent or live RNG. Starting combat freezes the last validated arrangement and matching baseline.
2. **Given** a bound enemy move with one invalid target and a separate effect with a valid target, **when** the enemy acts, **then** the invalid effect is skipped, the valid one resolves through System 3 and no replacement target is drawn. A move with no remaining valid effects still counts as a completed actor action.
3. **Given** the two-hero Ấn fixture and identical catalog, seed, rule versions, baseline and decisions, **when** run in Unity, headless and tests with the agreed dependencies, **then** compare ordered events, causal links and historical views under System 4's parity contract.
4. **Given** the final enemy falls and a legal queued reaction then defeats the final hero, **when** the action finishes, **then** the log orders the enemy lethal HP change, the hero lethal HP change and one Draw terminal record. Without the hero lethal reaction the same enemy lethal change yields one Victory after the queue drains. In the mirrored single-side case, a final hero lethal change with enemies remaining yields Defeat. No further actor is scheduled. Repeating a completion signal does not add a terminal fact or duplicate a reward.
5. **Given** both sides are alive after action 199, **when** action 200 ends with both sides alive, **then** emit one Defeat with reason ActionLimit and grant no reward. If action 200 defeats the final enemy and a hero survives, emit Victory instead. The prior action alone does not trigger the cap.
6. **Given** both sides are alive and System 11 confirms that no actor can act again under the rules, **when** System 12 processes that result, **then** emit one Defeat with reason NoEligibleActor and grant no reward. A fixture where an actor is only waiting for its gauge continues.
7. **Given** a valid prefix and a technical failure in a pending detonation, **when** combat stops, **then** the log ends at the prior valid position, the failed detonation changes neither stack nor damage state, and the menu reports a technical error without a terminal gameplay fact or reward.
8. **Integration, when Systems 27 and 29 exist. Given** a sealed encounter trace, **when** playback runs at 1×, 2×, 4× or skip, **then** the events, views and outcome remain identical and the HUD uses the view at its playback position. Evaluate the largest payoff against the applicable UX/art readability criteria and retain the screenshot. Verify the core assembly has no UnityEngine references separately.

## Open Questions

| Question | Owner | Resolve by |
|---|---|---|
| What legal roster and baseline inputs can encounter generation supply? (System 14 requires one to six enemies with distinct slots; an empty roster or an empty party is a `ContractViolation` at binding.) | Systems 12, 16 and 36 | Before implementing encounter preparation. |
| ~~How does System 11 distinguish a temporarily unready actor from a permanent gameplay inability to act and from a technical error?~~ Answered by System 11 (2026-11-12): a waiting unit is never unable to act because the effective speed is at least 1; `NoLivingUnit` and `ContractViolation` are separate codes. Outcome row 5 stays as a guard that cannot occur in the MVP. | Systems 11, 12 and 36 | Closed. |
| ~~Does System 9 confirm the root order in the System 3 Canonical Order Contract (anchor-cell reading order)?~~ Answered by System 9 (2026-11-12): yes, Rule 7. Reaction limits are settled (`D = 8`, `N = 256`). | Systems 3, 9 and 12 | Closed. |
| Does System 12 accept the call order of System 9 Rule 5, the pre-fight action (one root action per hero with combat-start items, in party-slot order, outside the 200 limit, with an outcome check afterwards) and the increment of each hero's action ordinal on every scheduled action? Does a skipped action (`stunned`, `no_ready_item`) count toward the 200 limit? | Systems 9 and 12 | Before System 12 is re-reviewed. |
| How does System 5 wire the single terminal handoff and technical return to menu? | Systems 5, 12 and 31 | Before live run integration. |
| System 18 needs the frozen arrangement (placed items and System 10 views) delivered by value at victory, before Thaw, so that it can build the reward offer in the same transition; defeat and the boss victory produce no offer. | Systems 12, 18 and 5 | Before System 12 is re-reviewed. |
| System 17 builds the next door row in the same victory transition as System 18's offer (before Thaw), and the buff of the room just won is claimed after Thaw; a defeat produces neither a row nor a buff. | Systems 12, 17 and 5 | Before System 12 is re-reviewed. |
