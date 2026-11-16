# Post-combat Breakdown

> **Status**: Designed (pending independent design review)
> **Author**: User and design team
> **Last Updated**: 2026-11-13
> **Last Verified**: 2026-11-13
> **Implements Pillars**: 2 — The Payoff Is the Product; 4 — Arrangement Is the Thinking
> **Creative Director Review (CD-GDD-ALIGN)**: APPROVE (2026-11-13); alignment only, not verification of the unresolved integration or UX evidence.

## Summary

After a fight, an in-context ranked panel explains each item's actual contribution to enemy HP and shield loss and traces recorded detonation setup sources. It shows the fight's biggest hit and longest effective chain, with a personal best that the user chose to retain across game launches. Historical inspection, metric identity and the small persistent-record contract remain subject to the owner interfaces and independent review listed below.

> **Quick reference** — Layer: `Presentation` · Priority: `MVP` · Key deps: `Event Log (4), Combat Resolution (12), Encounter Generation (16), Grid Backpack (8), Backpack UI (25)`

## Overview

Post-combat Breakdown helps the player understand what happened in the most recent fight and connect the result back to the items and heroes they arranged. The user chose a ranked item contribution panel with bars for the MVP on 2026-11-13; this supersedes the systems index's earlier text-only MVP stub and follows the Art Bible's result-panel direction. For a detonation, the item that performs the detonation receives the damage credit and the contributing setup items are listed separately, also by user decision. The explanation is derived from the recorded fight rather than rerunning it or inventing a counterfactual.

The contribution bar measures enemy HP and shield actually lost, as the user chose on 2026-11-13. The panel needs the sealed event trace, its frozen encounter arrangement and causal sources for as long as review is permitted. The exact last-fight retention and interaction with the reward flow remain design questions. Systems 4, 12 and 8 need independent review; System 29's playback GDD is a draft awaiting its gate/review, and its retained-arrangement interface still needs integration. The Unity UI reference in this project covers 6.3 and has not been reverified for the pinned Unity 6.6; the UI stack remains an architecture decision.

## Player Fantasy

After the fight the player looks back at the three-hero machine they built: which item actually removed enemy HP or shield, which items supplied the status consumed by a detonation, and where those items were when the fight began. A victory is more satisfying when the player can recognize their own arrangement in it; a loss becomes a useful clue for the next arrangement.

The bar is clearly labelled as actual enemy HP and shield lost, not the item's total value or importance. Setup sources are shown as recorded contributors, without awarding them a second share of the same damage or claiming an untested counterfactual. This supports Pillar 2's clear cause and Pillar 4's informed next decision. Creative-director consultation recommended this framing; no playtest evidence is claimed.

## Detailed Design

### Core Rules

1. **Scope and source.** System 30 consumes the encounter's immutable sealed trace, placement baseline, historical views and retained frozen arrangement. Its ranked, scrollable result panel is an in-context UI, not a combat replay or a separate run state. It never reruns a fight, changes the trace, draws RNG or issues a gameplay command. Systems 4/36 own facts and views; System 5 owns encounter retention and release.
2. **When shown.** Do not reveal results or rewards ahead of the System 5/29 playback gate. After playback/Skip reaches an eligible ordinary-room victory, open the result panel in the former enemy region; the player can dismiss it and return to the actual System 5 state and any currently pending reward step without an automatic choice or confirmation. System 5 owns the BuffClaim → OfferPending → DoorChoice order and its exceptions, including no room-1 BuffClaim and boss entry after room 8's rewards, or directly when both steps auto-skip. Reopen 'last fight' without altering that flow. For defeat, Draw or boss victory, the user chose a compact System 31 end summary with a button that opens last-fight details; do not auto-open the full breakdown at run end. Exact focus, safe layout and transition contracts with Systems 5/28/31 remain provisional. A technical stop is a distinct diagnostic, not a gameplay loss; whether its partial prefix can be player-reviewed requires the System 5/12/29 stop-path resolution.
3. **Metric — actual enemy loss.** A bar measures actual enemy shield plus HP lost in the fight, not resolved damage, potential damage, friendly fire, healing, shield gained or overall usefulness. Enemies have no shield in the MVP (System 14); shield is included for the shared damage-record contract and future content, not as a new MVP mechanic. For each validated enemy-targeted `Damage`/`DetonationDamage` record, take the nonnegative actual decrease in shield and HP for that step. Sum the steps once, never sum the `resolvedDamage` repeated on shield and HP records. Group relevant steps by encounter, damage occurrence and target; a multi-hit case whose IDs do not distinguish successive hits on one target is a blocked System 4 schema question, not permission to collapse them silently.
4. **Credit and setup.** Attribute actual enemy loss to the direct item instance named in the validated typed damage origin, using its encounter placement baseline. For detonation, the user chose the detonating item as the sole direct damage credit; follow `DetonationDamage → StatusConsumed → {event|baseline, id}` to list the consumed setup sources and their actual quantities separately. Do not allocate that same damage to those setup sources, imply a percentage of its cause, or claim what would have happened without them. If the direct origin is not one attributable item, place the actual amount in a visible **Other / unassigned** category with available provenance; do not distribute a multi-source status tick arbitrarily. Missing or invalid trace links are a data error/unavailable explanation, never a guessed item or fabricated zero.
5. **Rank and share.** Each item row represents an item *instance*, even if several have the same definition. Sort descending by credited actual enemy loss; ties use the frozen hero party slot, then stable item instance ID. Include recorded zero-damage setup sources in the explanation, not as fabricated damage shares. Show each row's exact total and a bar/share against **all enemy loss**, including Other/unassigned, so undisplayed causes cannot make item shares falsely total 100%. Show Other/unassigned when nonzero. If total loss is zero, display an explicit no-damage state and no percentage rather than divide by zero. Bars do not rank defensive, setup or other non-damage value.
6. **Traceable explanation.** Selecting an item/row can show the related logged occurrences and, for detonation, the contributing status sources/amounts in their recorded order. The event list is a filtered reading of structured facts, with player text resolved through System 7. A source that did not fire or has all effects skipped is not assumed inactive from a missing damage origin; System 4's placement wording conflicts with System 9's `ItemFired` contract. Do not label an item 'never fired' until that schema is reconciled. A skipped effect is shown as skipped, not dealt damage.
7. **Historical placement.** Use encounter ID plus stable item instance, original hero/anchor/orientation from the frozen placement baseline. An innate item is a real item instance but occupies no grid cell: credit it when its origin is valid, label it innate and offer non-grid source identification. System 15's proposed baseline row with anchor `(-1, 0)` and an innate flag remains a System 4 integration prerequisite; never highlight that sentinel as a cell. Inspect a clearly labelled read-only historical arrangement even if a placed item was moved, removed or destroyed since the fight; exit restores the live grid and focus without issuing a placement operation. System 25/29 do not yet expose the complete reviewed-layout interface, so historical highlighting and retention remain blocked for integration. Missing baseline is reported as unavailable, not replaced with a current-grid guess.
8. **Most recent fight and lifetime.** Proposed MVP lifetime: retain at most the most recently completed encounter's trace, baseline, frozen arrangement and review data while the run can reopen 'last fight'; replace/release the old record only when the next `StartFight` is successfully accepted, or on Restart/Abandon/ReturnToMenu as appropriate. Failed Start and closing the panel must not discard it. This is a required System 5/4/29 handoff and atomicity contract still to be confirmed; do not claim it is implemented. The separate persistent personal best does not require keeping a fight history or replay.
9. **Outcome explanation.** Display the owner-supplied outcome and reason after its terminal boundary. For a defeat, show a decisive enemy-intent line only if `terminalCauseSeq` and its origin really resolve to an enemy move; otherwise show the actual recorded cause/reason or explicitly say the cause is unavailable. Draw, ActionLimit and NoEligibleActor have their own truthful explanations; Draw and ActionLimit do not have a fabricated lethal cause. Technical stop has no `CombatEnded`, no victory/defeat and no reward. Preserve the valid diagnostic prefix without claiming it is a complete fight.
10. **Highlights and records.** The MVP must show biggest hit, longest chain and a personal best (previously called session best in the index). Proposed 'biggest hit' is greatest actual enemy HP+shield loss for one damage occurrence on one target, never the sum of repeated per-step `resolvedDamage` or all hits in a multi-hit move. The user chose longest chain as the greatest count of distinct effects that actually mutate state within one root action, consistent in intent with System 29's proposed `N`; one effect with multiple records counts once, and skipped/suppressed/empty facts do not count. Unstamped events such as action starts, item-fired facts or boundary expiry are not independently counted as effects under the current schema. System 4 separately calls max effect ordinal/depth combo size and ordinals may include skips/gaps; reconcile the schema and names before claiming integration. The user chose to retain the personal best across game launches, Restart and menu transitions. The exact record unit(s), storage owner, update point, missing/corrupt data behavior and interface remain blocked; label the value unavailable until supplied rather than inventing it. This is a small MVP persistence requirement, not run-save, replay or a complete records system.
11. **Access and reward coexistence.** Mouse-only and keyboard-only access follows System 6's focus contract; no hold-only or timed reading. Opening, selecting, filtering, closing or reopening the review is presentation-only. The result may not execute, skip, advance or preselect any reward or door; it cannot cover mandatory reward controls without an exit path. All/hero filtering changes visible item rows only; the denominator stays labelled as the total enemy loss for the full fight, not silently rebased per hero. No modal or focus behavior is final until System 6/28/31 and screen UX define it.

12. **[VS] Next-rung readout (near miss).** For the fight's strongest root action (the one System 29's classifier ranks highest, ties to the greatest actual enemy loss), show its class (Tick, Chain, Surge or Detonation) and, when it is not a Detonation, how far each recorded input was from the nearest unmet condition of the next class: the chain size `N` against its threshold, the reaction depth against its threshold and, in a boss fight, the Detonation's share of the boss's maximum HP against the 50 percent run-defining share. Every number is a recorded fact compared with a published threshold (System 29, `payoff_ladder_thresholds`, and `run_defining_damage_share`). It names no item as 'missing' and says nothing about what another arrangement would have done: that would be a counterfactual, which Rule 4 forbids. It reads the thresholds from the shared classifier in the engine-free core, which must return the class and the margin to the next class (an interface System 29 does not offer yet), and it keeps no copy of them. It is a Vertical Slice feature (the concept's H2 gate needs it); the MVP shows biggest hit and longest chain only. It is hidden when no root action is above Tick or when the classifier reports the margin as unavailable.

The ranked panel, direct-versus-setup credit, actual-loss metric and longest-chain meaning are user decisions (2026-11-13). Game-designer, UX designer, art-director, UI programmer, technical artist and systems-designer provided analysis; last-fight lifetime, largest-hit identity and the log-to-chain metric interface still need owner contracts reconciled.

### States and Transitions

| State | Entry | Exit / rule |
|---|---|---|
| Unavailable | No complete retained encounter, or still playing | No future outcome or result panel; show availability only after the playback gate. |
| Summary open | Playback complete for ordinary-room victory; or player selects details from the run-end summary | Ranked rows, outcome and recorded context; player can filter or inspect a row and dismiss without a run command. |
| Historical source inspection | Player selects a source | Read-only frozen arrangement, identified by encounter and instance; close restores live view/focus. Interface blocked pending 25/29. |
| Summary closed, retained | Player dismisses | Existing reward step or end summary stays authoritative; 'last fight' reopens while System 5 retains the encounter. |
| Expired | Next encounter successfully accepted or run exited under the proposed lifetime | System 5 releases/replaces encounter; old 'last fight' is unavailable, no dangling links. Ownership contract pending. |
| Technical stop | Run owner signals stop | No gameplay outcome or rewards; partial-prefix player review remains an open owner decision. |

### Interactions with Other Systems

| Owner / client | Input to System 30 | Output / ownership |
|---|---|---|
| 4 Event Log; 36 Combat State | Committed records, causal links, baseline and historical views | Breakdown aggregates facts; 4/36 own validation and state. Occurrence and ItemFired schemas need reconciliation. |
| 5 Run Flow; 12 Combat Resolution | Outcome/reason, playback gate, trace lifetime and release | 5/12 own run transition; 30 opens/closes presentation without issuing reward or outcome commands. |
| 8 Grid Backpack; 25 Backpack UI; 29 Payoff | Frozen encounter placement, item identity, retained view and playback completion | 25 owns workbench; 29 proposes frozen encounter record; historical inspection interface is not yet implemented. |
| 13 Status Effects; 3 Effect Resolution | Actual consumed sources and resolved typed origins through trace | 30 displays documented setup separately from the damage credit. |
| 16 Encounter; 28 Reward Panels; 31 Menus | Encounter/room context and currently pending reward/end-summary view | 28/31 own those screens; review cannot change their flow. |
| 6 Input; 7 Localization | Focus/commands and keyed localized text | Use mouse and keyboard, actual source parameters and text fallback; screen-specific focus remains open. |

## Formulas

These are presentation metrics, not gameplay damage formulas. Read the already validated System 4 records; do not re-run System 3. Use checked 64-bit aggregation. An overflow or invalid link makes the affected result unavailable with a diagnostic; never wrap or present a fabricated total. The existing 20,000-record trace ceiling is provisional and does not establish a memory/performance budget.

The `actual_loss` formula is defined as:

`actual_loss(r) = (shield_before(r) - shield_after(r)) + (hp_before(r) - hp_after(r))`

**Variables:**
| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| `r` | damage record | validated `Damage`/`DetonationDamage` step on an enemy | System 4 | One shield or HP mutation step, not the repeated resolved-damage field. |
| `shield_before`, `shield_after` | integer | nonnegative; after ≤ before for a damage step | System 4/36 | The shield pair on this step; unchanged field contributes zero. |
| `hp_before`, `hp_after` | integer | nonnegative; after ≤ before for a damage step | System 4/36 | The HP pair on this step; unchanged field contributes zero. |
| `actual_loss` | checked 64-bit integer | 0 or more | calculated from validated step | Actual enemy resources lost in this step. |

**Output Range:** 0 through the representable checked 64-bit range; overflow or inconsistent input is an explicit diagnostic, not a score.  
**Example (synthetic/future shield contract, not an MVP enemy):** resolved damage 8 against shield 5 and HP 3 emits steps contributing 5 and 3; the sum is 8, not 16 even though each record repeats `resolvedDamage=8`. An MVP enemy with shield 0 and HP 3 instead contributes at most 3 actual HP loss from the hit.

The `item_share` formula is defined as:

`C_i = sum(actual_loss(r) for enemy damage steps directly attributed to item instance i)`  
`T = sum(actual_loss(r) for all enemy damage steps)`  
`item_share(i) = C_i / T` when `T > 0`; otherwise no share exists.

**Variables:**
| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| `i` | stable item instance ID | one encounter baseline instance | Systems 4/8 | Identifies a specific placed item, not just its definition. |
| `C_i` | checked 64-bit integer | 0..`T` for a valid completed trace | calculated from direct typed origins | Actual enemy loss credited to this item; detonation goes to its detonator only. |
| `T` | checked 64-bit integer | 0 or more | calculated from all valid enemy damage steps | Includes direct items and visible Other/unassigned origins. |
| `item_share(i)` | rational | 0..1 when `T > 0` | calculated | The bar's share of the fight-wide enemy loss; display rounding is a later UX choice and never changes totals. |

**Output Range:** 0..1 for a valid nonzero denominator; no percentage or bar fraction at `T=0`. Sorting uses `C_i` before any display rounding.  
**Example:** an item credited 8 and another origin causing 2 give `T=10`, item share `8/10=80%`, and Other/unassigned `2/10=20%`; an item setting up 3 status units is listed as a contributor but does not receive a second portion of the 8.

For the proposed largest hit, first group records by `(encounterId, damageOccurrenceId, targetInstanceId)` and sum their actual loss once; take the maximum across enemy targets. This is **blocked** until System 4 confirms that this key distinguishes separate hits to the same target. It is a single hit's actual loss, not a whole multi-hit move's sum or pre-shield resolved damage.

The `longest_chain` formula is defined as:

`N(root) = count(distinct effectOrdinal with a committed mutating effect under root)`  
`longest_chain = max(N(root))` over completed root actions in the retained fight; an empty set has no chain.

**Variables:**
| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| `root` | root-action ID | one completed action in the encounter | Systems 3/4/12 | Scope for related effects. |
| `effectOrdinal` | integer | ≥ 1; gaps possible | System 4 stamps | Identity of an effect within its root; multiple mutation records with the same identity count once. |
| `N(root)` | nonnegative integer | 0 or more | derived from valid stamped records | Number of distinct effects that mutate state, not the greatest ordinal or depth. Excludes skipped/suppressed/empty facts and separate unstamped boundary events. |
| `longest_chain` | nonnegative integer / absent | absent if no completed root action | calculated | Maximum `N(root)`; ties identify the earliest root by its `ActionStarted` sequence. |

**Output Range:** 0 or more, or absent when there is no completed root action; do not calculate a completed-fight record from a technical-stop prefix. The user's selected meaning matches System 29's proposed `N`, but remains **BLOCKED** until Systems 3/4/12/29 confirm how stamped mutations and completed roots are identified. System 4's highest ordinal and depth are separate measures, not this count.  
**Example:** a root has mutating ordinals 1 and 5, a skipped ordinal 3, and ordinal 5 produces both shield and HP records: `N=2`, not 5 and not 3. Persistent personal-best units and storage ownership remain unresolved.

## Edge Cases

- **If damage first consumes shield then reduces HP:** add each actual field decrease once, even if both records repeat the same resolved damage; overkill that did not remove HP or shield is not credited.
- **If a detonation consumes contributions from multiple items:** give direct loss to the detonating item's recorded origin and list each valid consumed source and amount; setup is not a second damage award. A baseline contribution remains traceable even if it expired or was consumed during the fight.
- **If one instance receives or supplies several effects:** group its credited loss by item instance and show supporting events separately. Two copies of the same definition remain distinct rows and tie by slot then instance ID.
- **If an innate item deals damage:** rank its real instance and hero identity when the validated origin exists; mark it innate and never try to highlight its `(-1, 0)` sentinel as a grid cell. The baseline extension is blocked pending Systems 4/15.
- **If a damage origin is environmental, enemy-owned, a multi-source status tick or otherwise not a single attributable item:** retain its actual enemy loss in Other/unassigned; show known provenance and do not guess a split. Friendly damage is excluded from the enemy-loss metric and may be named in an outcome explanation when recorded.
- **If no enemy HP/shield is lost:** show no-damage, no percentage and no ranking by meaningless zero; retain any supported setup/skip explanations that have real events.
- **If fight-wide loss is nonzero but the selected hero has no credited damage:** show 'no credited damage for this hero', retain any real setup explanation and keep the fight-wide denominator; do not call the entire fight a zero-damage fight.
- **If an item fires but every effect is skipped:** do not mark it unfired because it has no damage or mutation origin. The `ItemFired` schema/consumer contract remains blocked until Systems 4/9 reconcile it. Passives are not classified as missed firings.
- **If a source was moved, destroyed or is not in the current arrangement:** use its frozen encounter placement and instance; if that baseline cannot be read, label its historical location unavailable rather than highlighting another item.
- **If `terminalCauseSeq` is null or is not an enemy move:** show the owner's Draw, ActionLimit, no-actor or other outcome reason, or an honest unavailable cause. Never attribute a decisive intent without a matching typed record.
- **If the trace or link is partial, invalid or missing:** do not fabricate a complete rank, source or win/loss. A technical-stop prefix is labelled partial if its owners permit player review; otherwise retain it only for diagnostics. The stop-path UX remains open.
- **If closing, filtering or reopening the panel during a reward step:** keep the same reward state, prior selection and live grid. Opening the previous fight while the player is holding an item requires a System 6/25 focus/held-state policy; it is blocked until defined.
- **If a new Start request fails or is rejected:** retain the previous fight under the proposed lifetime. For a successful next encounter, atomic replace/release is a System 5 contract, not an assumption that the UI may delete a trace.
- **If the run is restored from its in-memory snapshot:** System 5's snapshot excludes the combat log. The separate last-fight handle/retention contract must say whether it survives; if unavailable, report that review is unavailable and never synthesize a trace or claim a persistent replay.
- **If all known credited damage is from a non-item origin:** show Other/unassigned and preserve the denominator. If an item ties another exactly, compare stable hero slot then instance ID before display rounding.

## Dependencies

| Owner / client | Direction | Contract and status |
|---|---|---|
| 4 Event Log; 36 Combat State | Hard upstream | Immutable records, typed origins, baseline and views. In Review; multi-hit occurrence identity and ItemFired/unfired schema need reconciliation. |
| 5 Run Flow; 12 Combat Resolution | Hard upstream | Playback gate, outcome/reason and release ownership. 5 Designed pending review, 12 In Review; last-fight lifetime and stop-path handoff need agreement. |
| 8 Grid Backpack; 25 Backpack UI | Hard upstream / peer UI | Frozen item placement, identity and historical workbench; both await independent review. Read-only historical inspection interface not yet specified/implemented. |
| 29 Payoff Presentation | Hard peer | Playback boundary, Skip, proposed frozen encounter record. GDD draft pending director gate and independent review; it must not be treated as implemented. |
| 3 Effect Resolution; 13 Status Effects | Upstream via 4/36 | Damage origin and consumed-source records. System 3 Needs Revision; System 13 In Review. |
| 16 Encounter Generation | Upstream | Encounter, room/roster identity and context; Designed pending review. |
| 28 Door and Room Panels; 31 Menus | UI peers | 28 owns reward screens; 31 owns end summary and is Not Started. Review cannot drive those flows. |
| 6 Input and Focus; 7 Localization | Hard UI/data | Mouse/keyboard/focus, text keys, parameters and localized strings; both Designed pending review and not implemented by this GDD. |
| 35 Balance Harness; 23 Records | Downstream / later | Harness may consume agreed metrics. The user-selected small persistent personal best is MVP; the broader records system and replay remain System 23, Full Vision. Its persistence owner is not yet assigned. |
| Art Bible; UX/accessibility requirements | Visual and interaction targets | Draft references; screen UX, real font and retained visual evidence are missing. |

## Tuning Knobs

Actual loss and credit ownership are player-facing metric definitions, not gameplay balance knobs: do not change them in a tuning pass without updating the label, fixtures and cross-system contracts. Display precision, list density, bar length, scrolling and text size belong to the later UX/art specification; exact values and truncation behavior require testing, and rounding must not change rank or totals. The proposed one-fight retention is a scope/lifetime contract owned by System 5, not a configurable score multiplier. System 4's provisional trace ceiling and System 29's combo thresholds belong to their owners; no new HUD performance or memory budget is claimed here.

## Visual/Audio Requirements

- Use the Art Bible 7.5 result-panel direction: one row per credited item instance with item plate, name, exact actual-loss number and a bar; All/hero filters use join numerals. The user selected this ranked panel for MVP, replacing the prior filtered-text-only scope. Recorded setup sources and the Other/unassigned category are clearly labelled; visual styling never implies setup has direct damage credit.
- The bar is labelled 'enemy HP + shield lost' or equivalent localized wording; shape/text and numbers carry information in addition to color. A no-damage fight has an explicit empty state rather than an empty unexplained panel. No fake `100%` is shown if sources are missing.
- A selected row identifies the item in a labelled frozen battle-layout view; the live grid must not be displaced or edited. The full right-column layout, coexistence with rewards and exact historical-view presentation require `/ux-design` and evidence. The Art Bible's 7.5 measurements remain design targets, not tested fit.
- System 29 owns combat payoff/audio. The breakdown does not introduce required audio; every fact is readable without sound. Vietnamese and English, non-color cues, readable contrast and reduced-motion behavior require the approved font, accessibility and visual validation.

## Game Feel

- The player can move from the excitement of the fight to a calm explanation, read at their own pace, and return to the reward step without losing a selection or being rushed.
- Following a damaging item to its actual setup sources should make the machine understandable, not imply that a number measures an item's full strategic value. Review after Skip must expose the same recorded facts as review after normal playback.
- No automated replay, automatic scrolling deadline or interruption of the next decision is implied; all transition and focus timings require screen UX and playtest evidence.

## UI Requirements

- After an eligible ordinary-room victory, or when the player selects details on a run-end summary, present the in-context contribution-ranked item rows with exact actual HP+shield loss, labelled fight-wide shares and a visible Other/unassigned category when applicable. A player can inspect causes, filter All/by hero, dismiss and reopen the last fight within the agreed retention window.
- Show outcome and accurate reason, biggest single actual-loss hit, longest chain and persistent personal best, with 'unavailable' where the source or owner contract is unresolved. A zero-damage fight shows no fraction. The denominator is never silently changed by filtering.
- Show the recorded status contributors to a detonation separately from the detonator's damage. Inspecting a row highlights the original instance and position in a read-only historical battle layout, not an item currently under the same grid cell.
- Support mouse-only and keyboard-only focus, plain text/non-color feedback, predictable return to the underlying reward/end-screen focus and accessible scrolling. No timed input; gamepad is a later contract. Precise focus graph and screen-reader behavior require System 6, System 34 and screen UX.
- `/ux-design` must produce and review the post-combat/breakdown view before UI stories. Retained fixtures/screenshots must cover at least a multi-source detonation, skipped effects, zero damage, Other/unassigned, duplicate item definitions, a moved/destroyed source, Draw and ActionLimit, reward coexistence and normal/Skip parity; no such evidence has been run in this session.

## Cross-References

| Document | Relevance |
|---|---|
| [Game Concept](game-concept.md), [Systems Index](systems-index.md), [Art Bible](../art/art-bible.md) 7.5 | Pillars 2/4, selected ranked panel, outcome explanation and record requirements. |
| [Event Log](event-log.md), [Combat State and Step Model](combat-state-and-step-model.md) | Typed origins, shield/HP steps, occurrence and consumed-source links, baseline and historical views. |
| [Effect Resolution Model](effect-resolution-model.md), [Status Effects](status-effects.md) | Actual effect and status-consumption semantics; no counterfactual attribution. |
| [Run State and Flow](run-state-and-flow.md), [Combat Resolution](combat-resolution.md) | Playback gate, outcome, diagnostics, reward order and trace-release ownership. |
| [Grid Backpack](grid-backpack.md), [Backpack UI](backpack-ui.md), [Payoff Presentation](payoff-presentation.md) | Frozen placements, historical highlighting, retained encounter and Skip. |
| [Door and Room Panels UI](door-and-room-panels-ui.md), [Input and Focus](input-and-focus.md), [Localization and Text](localization-and-text.md) | Reward coexistence, focus and localized presentation. |

## Acceptance Criteria

These are design criteria, not tests performed in this session. **BLOCKED** marks an unresolved owner contract; **NOT ASSESSED** requires real UI, accessibility or playtest evidence.

1. **GIVEN** an MVP enemy with shield 0 and HP 8, **WHEN** a validated item damage step changes HP to 3, **THEN** the item's actual credited loss is 5. A no-op produces no invented damage record.
2. **GIVEN** an MVP enemy with shield 0 and HP 3, **WHEN** resolved damage is 20 and HP falls to 0, **THEN** actual credited loss is 3, not 20.
3. **GIVEN** a synthetic future/non-MVP enemy fixture with shield 5 and HP 3, **WHEN** resolved damage 8 produces shield and HP steps, **THEN** actual loss is 5 + 3 = 8, not 16 from the repeated resolved-damage field. This fixture does not add enemy shield to MVP gameplay.
4. **GIVEN** item A contributes 3 status units and B contributes 2, **WHEN** B consumes A:3 and B:1 to detonate for actual loss 8, **THEN** B receives direct damage credit 8 and both A:3 and B:1 are separately listed as recorded setup; no second damage share or counterfactual is assigned. Repeat with a baseline contribution.
5. **GIVEN** item-attributed enemy loss 8 and Other/unassigned enemy loss 2, **WHEN** the All and hero-filter views are read, **THEN** `T=10`, the item share is 80%, Other is 20%, and the labelled fight-wide denominator remains 10 under either filter.
6. **GIVEN** `T=0`, **WHEN** the result opens, **THEN** it says there was no enemy HP/shield loss and shows no share; documented setup and skipped events remain accessible. **GIVEN** `T>0` but the selected hero has no credited damage, **THEN** its filter says so without calling the whole fight zero-damage.
7. **GIVEN** two same-definition item instances with equal credit, **WHEN** rows are sorted, **THEN** they remain distinct and are ordered by frozen hero slot then item instance ID, not dictionary order or rounded percentages.
8. **GIVEN** an enemy-targeted status tick with several origins but no unique direct item, **WHEN** totals are computed, **THEN** its actual loss appears under Other/unassigned with known provenance. Damage to a hero is excluded from enemy-loss totals.
9. **BLOCKED — firing schema. GIVEN** an item fires and all of its effects are skipped, **WHEN** its history is shown, **THEN** it is not labelled unfired merely for lacking a damage origin; passive items are not marked as missed firings. Systems 4/9 must reconcile `ItemFired` and the baseline wording before this can pass.
10. **BLOCKED — hit identity. GIVEN** repeated multi-hits to the same target and shield/HP steps of a hit, **WHEN** biggest hit is computed, **THEN** each actual hit is counted once and separate hits are not merged. System 4 must guarantee an identifying key per hit/target before this can pass.
11. **BLOCKED — chain integration. GIVEN** a completed root action with mutating effect ordinals 1 and 5, a skipped ordinal 3, and two records for effect 5, **WHEN** the user's chosen chain count is computed, **THEN** it is 2, not 5 or 3. Systems 3/4/12/29 must confirm completed-root and effect identity and align labels before integration.
12. **GIVEN** defeat with a valid `terminalCauseSeq` whose typed origin is an enemy move, **WHEN** the explanation is shown, **THEN** it names that recorded move. **GIVEN** Draw, ActionLimit, NoEligibleActor or a status-tick origin, **THEN** it shows the actual owner-supplied reason/known source or unavailable, never an invented lethal intent.
13. **GIVEN** a fight still playing, **WHEN** the result is requested, **THEN** no terminal result or reward is exposed before the playback gate. **GIVEN** the same completed trace at normal speed or Skip after the gate, **THEN** rankings, totals, causes and outcome match; viewing emits no gameplay command or RNG draw.
14. **BLOCKED — reward/selection focus. GIVEN** a pending BuffClaim, OfferPending or DoorChoice step, **WHEN** the player opens, filters, inspects, closes and reopens the review, **THEN** the exact pending state, armed selection, live grid and RNG stay unchanged. Systems 6/25/28/31 must specify held-item, dialog and focus-return behavior.
15. **BLOCKED — historical layout. GIVEN** an item has moved hero or been destroyed after the fight, **WHEN** its historical row is inspected, **THEN** the original encounter instance/hero/anchor is identified in a labelled read-only frozen view and no current item is mistaken for it; exiting restores live view and focus. Systems 5/25/29 must supply and retain the interface.
16. **BLOCKED — innate baseline. GIVEN** an innate item is the sole direct damaging source, **WHEN** the result is calculated, **THEN** its real item instance is ranked and labelled innate without highlighting `(-1,0)` as a grid cell. Systems 4/15/25 must define its baseline and non-grid identification.
17. **BLOCKED — retention. GIVEN** the player closes the result and edits the grid, **WHEN** they reopen last fight or a new Start is rejected, **THEN** the old encounter remains available and unchanged. On an accepted next Start, System 5 atomically replaces/releases it once under the agreed lifetime; the owner contract must be confirmed.
18. **BLOCKED — restore. GIVEN** a System 5 in-memory snapshot restore, **WHEN** last-fight review is requested, **THEN** it uses a separately retained matching handle if the owner contract preserves one, otherwise truthfully reports unavailable. The snapshot does not itself contain the combat log.
19. **BLOCKED — technical stop. GIVEN** a stopped run with a valid prefix, **WHEN** its diagnostic path is shown, **THEN** it is not labelled a complete victory/defeat and no reward or `CombatEnded` is fabricated. Systems 5/12/29/31 must decide whether player-facing partial-prefix review is allowed.
20. **GIVEN** a missing/invalid causal link, missing placement baseline or checked-integer overflow, **WHEN** the affected metric or explanation is read, **THEN** it is unavailable with a diagnostic, not guessed as zero, attributed to a different item or silently declared 100% complete; the result view does not change the pending reward.
21. **BLOCKED — persistent personal best. GIVEN** a validated personal best has been successfully stored, **WHEN** the player restarts a run, returns to the menu or closes and reopens the game, **THEN** the same best remains available until surpassed under the agreed metric; a smaller valid result does not replace it. Systems 5/31 and the persistence owner must first define the unit(s), update point, storage contract and missing/corrupt-data behavior. No storage implementation is claimed.
22. **NOT ASSESSED — UI/accessibility evidence. GIVEN** retained fixtures with Vietnamese and English text, at least six rows, no damage, Other/unassigned, multi-source setup, a hero filter, a moved item and a pending reward, **WHEN** fixed-tick captures and mouse-only/keyboard-only interaction traces are reviewed, **THEN** rows and controls are legible, non-color cues work and exiting restores the correct focus. Font, contrast and real interaction evidence do not yet exist.
23. **NOT ASSESSED — learnability. GIVEN** representative win and loss playtests, **WHEN** players identify the detonator, its recorded setup sources and an honest terminal reason, **THEN** retain responses and captures and evaluate against UX's predeclared pass criterion. Do not claim counterfactual proof or a playtest pass from design text.
24. **[VS] BLOCKED — next-rung readout. GIVEN** a fight whose strongest root action is a Surge, **WHEN** the readout opens, **THEN** it shows the class, the recorded `N` and depth beside the nearest unmet Detonation condition and the gap, and no item name; **GIVEN** a boss fight whose best Detonation dealt 38 percent of the boss's maximum HP, **THEN** it shows 38 against 50; **GIVEN** a fight with only Tick actions, **THEN** no readout is shown; **GIVEN** a change to a threshold in the shared classifier, **THEN** the readout changes with it and holds no copy. System 29 must expose the margin first.

Useful negative controls include a mutant that sums `resolvedDamage` twice, gives setup damage credit a second time, drops Other from the denominator, highlights the live anchor, exposes an outcome before the gate, invents `EnemyMove` for a status tick, or releases a trace after a rejected Start.

## Open Questions

| Question | Owner | Resolve by |
|---|---|---|
| Does `(encounter, occurrence, target)` uniquely identify one hit when a multi-hit move strikes the same target more than once? | Systems 3/4/36 | Before biggest-hit or per-hit aggregation integration. |
| Does System 4 accept `ItemFired` as the authoritative firing fact, including effects all skipped, and how are passive items represented? | Systems 4/9 | Before any 'unfired' UI or schema review. |
| Does the System 4 placement baseline accept System 15's innate-item instance, hero slot, `(-1,0)` sentinel and innate flag, and what non-grid highlight identifies it? | Systems 4/15/25/30 | Before innate-item breakdown integration. |
| How do Systems 3/4/12/29 expose completed root actions and distinct mutating effects for the user's selected longest-chain count, while keeping System 4's highest ordinal/depth distinct? | Systems 3/4/12/29/30 | Before run-summary metric implementation. |
| Which metric or metrics make up the personal best, who stores them across launches, when are they updated, and how are missing, corrupt or incompatible data handled? The user chose cross-launch persistence; full records/replay remain deferred. | Systems 5/30/31 and persistence owner | Before summary and persistent-best implementation. |
| Can System 5 retain and atomically replace one complete encounter record through panel close/reopen and grid edits, releasing only at the next accepted Start or run exit? | Systems 4/5/29 | Before last-fight integration. |
| Does an in-memory System 5 restore keep its separate last-fight handle, or make review unavailable? Its snapshot does not contain the event log. | Systems 4/5/24/29/30 | Before restore/last-fight integration. |
| How does System 25 show the original frozen instance/placement and restore live view/focus without moving the current item? | Systems 8/25/30, UX designer | Before historical highlight implementation. |
| How does the result panel open/close while reward screens or end summary own the same column, including a held item or pending dialog? | Systems 5/6/28/30/31, UX designer | Before screen UX review. |
| Can a technical-stop prefix be shown to the player, and what diagnostic/partial label and release apply? | Systems 5/12/29/31 | Before technical-stop UX. |
| What should the UI display when a damage origin has multiple contributors but no single direct item, and can origin coverage be validated at catalog/trace boundaries? | Systems 3/4/13/30 | Before attribution integration. |
| Does System 29's shared classifier return the class **and the margin to the next class** (the nearest unmet condition per route), so the readout of Rule 12 needs no copy of the thresholds? Added by the concept re-review of 2026-11-14. | Systems 29/30/35 | Before the Vertical Slice readout. |
| Which Unity UI stack, layout sizes, font, number formatting and contrast work for real Vietnamese/English text and the chosen row density? | Technical director, art director, UX designer | UI architecture, font gate and screen UX. |
