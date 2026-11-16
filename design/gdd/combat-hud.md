# Combat HUD

> **Status**: Designed — pending independent design review
> **Author**: User and design team
> **Last Updated**: 2026-11-13
> **Last Verified**: 2026-11-13
> **Implements Pillars**: 4 — Arrangement Is the Thinking; 2 — The Payoff Is the Product
> **Creative Director Review (CD-GDD-ALIGN)**: REVISED 2026-11-13 — added open/closed Details causal-readability comparison after CONCERNS; usability evidence not assessed.

## Summary

Combat HUD shows the party's condition, the enemy's bound intent and the projected turn order before a fight, then follows the recorded fight at its presentation cursor. At the user's choice, the player may inspect the bound intent sequence in a read-only workbench Details view while playback continues. The HUD does not resolve combat; safe inspection layout, status overflow and several cursor/forecast interfaces still require their owning systems and UX validation.

> **Quick reference** — Layer: `Presentation` · Priority: `MVP` · Key deps: `Event Log (4), Combat State and Step Model (36), Turn Order (11), Enemy and Intent (14), Party and Heroes (15)`

## Overview

Combat HUD presents the party, enemy intent, turn order and combat state so the player can plan before a fight and follow what actually happens when it plays. It displays owner-provided facts rather than resolving combat or accepting gameplay commands during the fight. The event trace and the presentation cursor determine what the HUD may show; the simulation may already have finished and the authoritative run may have advanced while its recorded fight is still playing.

The event-log, state, turn, intent and party GDDs exist but still need independent review. Systems 25, 26, 6 (input) and 7 (localization) are Designed pending review; System 29 (payoff playback) has a draft awaiting its creative-director gate and review. The user chose to allow read-only intent Details while playback continues; it opens in the workbench rather than as a stage popup. System 6 specifies a read-only intent-tab path, and System 29 proposes a retained playback view; their integration and safe layout remain unverified. The project's Unity UI reference is for 6.3 and has not been reverified for pinned 6.6; the HUD's UI technology is an architecture decision.

## Player Fantasy

The player is the architect watching a three-hero machine come alive. Before the fight, they read the party's expected order and the enemy's bound intentions, then commit an arrangement; during playback they see whose action is occurring and how the results follow from that arrangement. A spectacular chain feels earned because its cause can still be read, and a defeat leaves a trail the player can study for the next run.

This serves Pillar 4 by making the decisions and threats readable before Start, and Pillar 2 by keeping causes, intent and health visible when the payoff grows. The order strip is a forecast with stated assumptions, not a promise of future events. Creative-director consultation recommended this direct player-facing framing; it has not been validated by playtest.

## Detailed Design

### Core Rules

1. **Ownership and timeline.** System 27 is a read-only presentation of System 4's encounter baseline and immutable post-event views from System 36. During playback every health, shield, status, actor and intent readout comes from the presentation cursor's coherent view, not the latest authoritative run or a future record. A nonmutating fact uses its preceding view. Systems 5/12 own lifecycle and outcome, System 29 owns playback and its controls; the HUD never resolves combat or mutates the trace.
2. **Planning.** After entering a room, show the bound next intent of each enemy and the party's forecast while the player arranges against it. Grid edits refresh the forecast without changing bound intent or drawing RNG. After a victory and before the next room is selected and bound, do not show the previous room's intents as the next room's. A boss entered directly after room 8 receives its own bound intents before Arrangement.
3. **HP, shield and status.** Show each unit's current/max HP, shield and effective speed, with active statuses and their actual source-provided amounts. The Art Bible calls the shield glyph 'armour'; it is the existing shield resource, not another mechanic. In the MVP, enemies have no shield or healing. A hero at HP 0 is downed immediately at that HP step: keep the slot and show zero/empty HP; its stat row clears visually while historical state remains available to the breakdown. Do not change the underlying status or omit its evidence from the trace.
4. **Order strip.** Use System 11's pure `Forecast` on the planning state or the cursor-aligned combat view. Show the next `K` actors (default 6, supported 1–20), hero join numerals and enemy intent glyphs, with the label 'if nothing changes'. Recompute after every downing HP step and arrangement edit, and on any future supported speed change; the exact refresh cadence through an in-progress action remains a System 11/29 interface question. System 11 returns actors, not per-occurrence intent glyphs: how 14/27 map repeated forecast enemy turns to successive bound entries and wrap, including the assumption about skips that retain `n`, is unresolved and blocked pending a testable adapter. Never manufacture the forecast by reading future actual events from the sealed trace. `BufferTooSmall` or invalid data yields no partial strip: clear stale entries and indicate forecast unavailable; exact presentation is a UX question.
5. **Enemy intent.** Show the enemy's stored next bound entry as glyph, target when applicable, and resolved damage **before shield** as `r` or `r × hits`. A windup may have no damage or target. Stun skips the action and keeps the bound entry; a hit aimed at a downed target is skipped, not retargeted. `ActionStarted` advances the stored index, so show the executing move from its action record separately from the next stored entry; never present the next tab as the move currently striking. The exact transition and record-to-view mapping with Systems 14/29 require validation.
6. **Read-only intent Details during playback.** The user chose to allow opening the selected enemy's full bound sequence while the fight continues (2026-11-13). This is presentation-only: no gameplay, flow, simulation or target command is sent, and playback does not pause merely because Details opens. The existing workbench lower section hosts the on-demand System 26 Details surface; no popup is placed on the stage, preserving the Art Bible's stage-popup prohibition and the earlier rejection of a permanent inspector. It reads that enemy's already-bound sequence and current presentation cursor, not future trace outcomes. When the workbench automatically follows an acting hero, retain the selected enemy's open Details if the source remains valid and the reading area remains safe; revalidate geometry and use an eventual safe fallback if not. Reaching the surface, source lifecycle, fallback, and fitting it without covering protected content are blocked on Systems 6/26/29 and UX validation. Before Start, System 26 also provides access to all `H` bound entries (default `H = 12`; safe tuning range 6–20), per-entry move and applicable hit/target/before-shield damage, fixed sequence and wrap.
7. **Ordered visible changes.** Shield and HP have separate committed historical views even when published in one atomic damage batch. Advance the displayed view according to the playback cursor and never count the same resolved damage twice. System 29's draft groups an effect's shield and HP records into one beat and advances its cursor at beat boundaries, so whether the intermediate shield-only view is separately shown is blocked on the 27/29 presentation contract; do not assume it is displayed. Mark a unit downed once the displayed view reaches HP zero; do not announce victory or defeat from a downed count alone. System 12 checks the completed action, including remaining legal reactions and due expiry; only a `CombatEnded` fact at the cursor establishes a gameplay outcome. Both sides downed yields Draw.
8. **Trace end, skip and stop.** Speed 1×/2×/4× and Skip are System 29 presentation controls. Its revised GDD (2026-11-14) specifies that Skip closes open Details at every fast-forward leg and advances to the next Detonation root action (played in full), then to the terminal view or the end of a technical-stop prefix, without changing events, gameplay result or reward handoff. System 29 proposes playing a valid stop prefix, while System 12's immediate technical-return wording still needs reconciliation; neither implementation nor review is complete. A technical stop is not a fabricated `CombatEnded` or a normal loss. Outcome and reward UI are gated until the corresponding playback boundary. Only the System 5 `AbandonRun` exception sends a flow command during playback; presentation-only inspection must not issue any flow command.
9. **Stable layout and visibility.** Show three heroes and the actual enemy roster in one or two vertical lines of up to three enemy slots each; no reinforcement or boss companion in the MVP. The source roster and legality belong to Systems 14/16, not HUD positioning. Enemy slots do not reflow mid-fight and line/slot never implies target priority. HP, status row, intent, order, action strip, turn marker and workbench stay above combat VFX as the protected layer. The Art Bible's reinforcement and boss-companion examples are later-scope and need synchronization.
10. **Status visibility and access.** The draft Art Bible allows up to three status chips and a `+n` overflow indicator; the actual status count and amounts come from System 13/36, not a display cap. Its Ấn amount has no gameplay stack cap. The surface for reading overflow and exact amounts during playback is still unresolved: the user's intent-Details choice does not by itself decide the status UI. Never silently equate `+n` with a gameplay cap or claim the overflow is accessible before that contract is designed.
11. **Workbench and controls.** System 25 follows the acting hero's grid at the start of its Ready phase during normal playback, does not follow during a Skip fast-forward leg (it follows the acting hero again in a Forced span), and returns to the pre-fight selected hero afterwards; manual hero switching during combat is outside MVP. System 29 owns pacing and a selected-speed indicator. System 6 specifies mouse and keyboard playback controls, Details on read-only intent tabs, and focus beginning at the current-speed control; its contract awaits integration and independent review. Exact gauge appearance is unresolved: the Art Bible says 'speed gauge' while System 11 says players read the strip, not gauges. A display must not alter or invent gauge data.

Rules reflect consultations with game-designer, UX designer, art-director, UI programmer and technical artist. The user explicitly chose read-only intent Details during playback; safe placement and input details have not yet been settled.

### States and Transitions

| State | Entry | HUD contract / exit |
|---|---|---|
| Pre-room / rewards | No next-room preparation bound | Do not label a previous or invented intent as the next enemy's. |
| Arrangement | Room entered, intents bound | Show bound intent and forecast; edits update preview; Start remains subject to System 5/25 guards. |
| Playback | Start accepted and trace/baseline available | Read view at cursor, gate flow commands, maintain protected information; optionally open read-only intent Details in the workbench. |
| Playback with Details | User opens intent Details | Keep playback running; show only bound sequence and cursor-aligned current state; close or update per the unresolved Systems 6/26/29 source-lifecycle contract. |
| Terminal playback | Cursor reaches `CombatEnded` | Show the supplied victory, defeat or draw; System 5 releases the next allowed flow after the playback gate. |
| Technical stop | Owner signals failure with committed prefix | Show no invented gameplay outcome, preserve diagnostic prefix and route according to System 5/12; playback of the prefix and exact messaging remain to be specified. |
| Post-combat / breakdown | Playback gate released | System 30 owns breakdown; retain trace and required historical views until System 5 releases them. |

### Interactions with Other Systems

| System | Input to HUD | Output / owner |
|---|---|---|
| 4 Event Log and 36 Combat State | Baseline, ordered event/fact and immutable post-event view, causal identity | HUD renders at cursor; 4/36 own trace/state. Event payload and view-storage details remain pending review. |
| 11 Turn Order | Pure forecast and effective speed from a specified state | HUD labels and renders the result; 11 owns gauge and forecast. |
| 14 Enemy and Intent; 16 Encounters | Bound sequences, target and scaled before-shield damage, roster and slots | 14/16 own intent and roster; 27 renders without draws or retarget. |
| 15 Party and Heroes; 13 Status Effects | HP/shield baseline, hero/slot identity, statuses and amounts | 15/13/36 own values; 27 draws, System 30 preserves causal explanation. |
| 5 Run Flow; 12 Combat Resolution | Bound-room readiness, accepted Start, playback gate, terminal and stop facts | 5/12 own lifecycle and flow; no gameplay command from read-only inspection. |
| 25 Backpack UI; 26 Hover Popup | Active-hero workbench and intent Details reading surface | 25 owns grid display; 26 owns on-demand Details, including the user's combat inspection choice; integration and layout remain open. |
| 29 Payoff; 30 Breakdown; 37 Settings | Cursor progression, speed/Skip, causal playback and later breakdown | 29 has a draft pending its gate/review; 30 and 37 remain Not Started. |
| 6 Input and Focus; 7 Localization | Presentation input and keyed text | Both Designed pending independent review; integration and supporting visual evidence are not yet verified. |

## Formulas

System 27 defines no gameplay or balance formula. It displays the owners' values and does not rederive damage, HP, shields, status stacks, turn order, cooldowns or outcomes from visual effects. System 11 owns effective speed, its `Forecast` and its gauge (`T = 1000`, legal gauge `0..1199`); System 14 owns resolved intent damage and multi-hit data; Systems 12/36 own combat state and terminal checks. If a future UX spec chooses to visualize a gauge that can exceed threshold, it must specify presentation saturation without modifying the stored value and supply a testable mapping; no such gauge mapping is decided here. System 11's forecast horizon defaults to 6 and is valid from 1 to 20; the HUD does not create another horizon or change its rules.

## Edge Cases

- **If 8 damage is applied to a unit with shield 5 and HP 3:** the historical views are shield 0 / HP 3 followed by shield 0 / HP 0, and the resolved damage is not summed twice. Whether playback exposes the intermediate view is blocked: System 29's draft groups the effect in one beat while System 27's original criterion expected separate visible states.
- **If the final enemy falls but a queued reaction also downs the final hero:** show each downed step, with no premature Victory; show Draw only when its terminal fact reaches the cursor. A final enemy downed without that reaction can instead lead to Victory after the boundary check.
- **If a unit is stunned:** present the skipped action and leave its current bound enemy intent unchanged; a stunned action still spends its gauge according to System 11. Never synthesize a new intent.
- **If an intent's target is already downed:** present the owner's skipped-hit fact; do not move the target marker to another hero or pretend another hit occurred. A windup without target/damage shows neither invented field.
- **If an enemy has begun an action:** its stored `n` may already point at the next entry. Use the `ActionStarted` event/occurrence for the executing move and the cursor-aligned stored value for the next tab; the precise handoff remains pending integration.
- **If no next room is bound:** show intent unavailable; do not leak an unchosen door's hidden intent or the finished room's sequence. Door rosters and danger are separate, owner-supplied information.
- **If forecast returns `BufferTooSmall` or `ContractViolation`:** no partial future strip is valid; remove stale predictions and show an unavailable/error state. Do not fill from the sealed future trace.
- **If one enemy appears more than once in a forecast:** do not repeat the same intent glyph or advance its index by assumption. The mapping of projected appearances to the bound sequence, wrap and skipped-turn assumptions remains blocked on Systems 11/14/27.
- **If a valid gauge is above its 1000 threshold:** retain the exact state supplied by System 11/36; a future graphic may saturate but must not clamp the model. What the Art Bible's 'speed gauge' means is unresolved.
- **If a unit has more status kinds than visible chips or an amount wider than its badge:** preserve the exact owner value and do not truncate it silently. The in-combat overflow/large-number reading route is blocked until UX and System 26 define it.
- **If an enemy or hero dies:** keep its slot stable and do not move surviving enemies between lines. A dead enemy has no next scheduled action; System 12 continues the already legally queued effects and reactions, checking each target, before the outcome boundary.
- **If the player opens intent Details in playback:** playback continues, and the reading surface must use the selected enemy's bound record and cursor-aligned state. If the source disappears, the relevant view is released, or no safe area fits, the close/fallback behavior is blocked on Systems 5/6/26/29 and UX; never read a different enemy or hidden future outcome.
- **If the trace hits a technical error or ceiling:** retain the committed prefix for diagnostics and present a distinct technical-stop path, not a victory/defeat or a fabricated terminal fact. System 29's draft proposes playing or skipping the prefix; reconcile that with System 12's technical return before integration.
- **If the trace contains `ChainLimit`:** it is a normal fact that ends the affected action's remaining reactions, not a technical stop; continue presentation according to the owner records. System 12 Rule 6 was synchronized with its Edge Cases and System 4; independent review remains due.
- **If Skip is selected before playback reaches its terminal fact:** System 29's revised rule closes Details at every fast-forward leg, advances to the next Detonation (played in full) and finally to the same terminal view, or the end of the valid technical-stop prefix, without replaying mutations or granting a reward itself; the technical-stop handoff remains to be reconciled.

## Dependencies

| System | Direction | Contract / readiness |
|---|---|---|
| 4 Event Log; 36 Combat State | Hard upstream | Baseline, cursor-addressed facts and immutable post-event views. Both In Review; payload, view storage and actual playback adapter remain to be verified. |
| 11 Turn Order | Hard upstream | `Forecast`, gauge/effective speed, downed filtering and order. Designed pending independent review. |
| 14 Enemy and Intent; 16 Encounter Generation | Hard upstream | Fixed per-enemy sequence, targets, damage, roster and slots. Designed pending review; no MVP reinforcements/boss companion. |
| 15 Party and Heroes; 13 Status Effects | Hard upstream | Hero HP/shield/speed, identity, statuses and exact amounts. 15 Designed pending review; 13 In Review. |
| 5 Run State; 12 Combat Resolution | Hard upstream | Bound-room state, playback gate, terminal/technical stop and retention/release. 5 Designed and 12 In Review; view handoff to presentation remains open. |
| 25 Backpack UI; 26 Hover Popup | Peer UI | Workbench following and on-demand bound-intent Details. Both Designed pending review and must be synchronized with the user's in-combat inspection decision. |
| 29 Payoff Presentation; 30 Breakdown | Hard downstream integration | System 29 has a draft, pending creative gate/review, proposing `IPlaybackView`, retained arrangement, Skip/stop and pacing. System 30 Not Started. Separate intermediate HP/shield display conflicts with its beat-boundary cursor and is blocked pending reconciliation. |
| 6 Input; 7 Localization; 37 Settings | Required input/text and speed control | 6 and 7 Designed pending review: 6 defines read-only intent focus/Details, 7 defines Vietnamese/English and a flagged missing-text fallback. System 37 remains Not Started. |
| Art Bible; UX/accessibility specs | Visual requirements and evidence | Art Bible Draft/REVISED; accessibility requirements exist as a draft, while the HUD UX spec and visual evidence are missing. |

The indexed dependencies are 4, 36, 11, 14 and 15. The added rows name owners required by the visible HUD and already referenced by the other GDDs. The weeks 5–6 engine-free combat-core parity checkpoint precedes building UI on that core.

## Tuning Knobs

System 27 owns no gameplay tuning values. System 11 owns forecast horizon `K` (default 6, 1–20), gauge threshold and speed; System 14 owns intent sequence length and damage; System 29 owns playback speed/pacing. Art Bible positions, sizes, three-chip threshold and transition durations are presentation targets, not measured performance or accessibility budgets. Changes to them require the art/UX owners and evidence; they must not alter underlying state or truncate exact values. No measured whole-HUD frame-time or memory budget is established.

## Visual/Audio Requirements

- **Protected layer.** HP, stat rows, intent tabs, order strip, turn marker, action strip and workbench remain readable over payoff VFX. An on-demand intent Details view stays within a legal workbench region, not the stage, and cannot obscure those protected elements. If no such region can be demonstrated, its safe fallback remains blocked.
- **Identity and contrast.** Hero slots retain join numerals; enemies use their encounter slots only for identity/layout. HP current/max appears beside the bar, shield uses the existing shield glyph and number, and status chips use shape/text as well as color. Readability, contrast and Vietnamese font coverage require real captures and testing.
- **Enemy and action.** One or two vertical enemy lines; no mid-combat reflow. Intent glyph, before-shield damage and target chip stay legible; acting-entity chevron and recorded effect presentation never obscure tabs or health. Do not infer targeting meaning from line placement.
- **Status and motion.** Statuses use owner-provided amounts; the three-chip-plus-overflow presentation is provisional, with overflow access unresolved. System 29's draft requires reduced flashes in MVP (on by default) and reduced motion; System 37/settings and accessibility integration and actual evidence remain pending. System 29/32 own payoff effects and sound; this HUD does not invent a separate sound vocabulary.

## Game Feel

- The pre-fight forecast and bound intentions let the player think without a timer. During playback, the player can follow the acting unit and the consequence of each displayed step without experiencing a second active combat phase.
- Spectacle can build in System 29, but it never erases the numerical and causal information needed to understand it. Read-only Details may be opened while the fight continues; readability at each speed and source disappearance needs playtesting.
- Compare the same payoff with Details open and closed, including 2×/4× and automatic hero follow, to find whether the player can still identify the source plate, item and order. Opening Details must not hide those causes; this is a playtest requirement, not a claim of success or a reason to pause the fight automatically.
- The Art Bible proposes a 100 ms workbench follow transition independent of playback speed, and no follow in a Skip fast-forward leg (the follow returns in a Forced span). It is a visual target for System 25/29, not evidence it has been implemented or measured.

## UI Requirements

- For each hero show join identity, current/max HP and shield, effective speed and available status information; mark downed immediately without announcing a final result. For each enemy show HP/status and its actual next bound intent (glyph, applicable before-shield damage/hits and target); clarify the currently executing move separately.
- Display the System 11 forecast and the text 'if nothing changes'; six entries are the default, and the active unit uses the turn chevron. Distinguish the forecast from the committed cursor and avoid displaying future outcomes.
- Provide a discoverable presentation-only action to open the selected enemy's bound intent Details in the workbench while combat playback continues. System 26 owns the content; no stage popup and no gameplay command. Exact focus, close behavior, geometry and localization require System 6/26 and screen UX design.
- Show current playback speed and access to 1×, 2×, 4× and Skip through System 29; the action strip changes by phase. Indicate Draw, Victory, Defeat and technical stop distinctly only at the owner-defined boundary.
- Use text/shape as well as color; exact numbers and overflow statuses need an accessible route. System 26's stage-popup ban and System 5's flow gate make combat overflow access unresolved; do not imply it works until specified and tested.
- `/ux-design` must specify the combat HUD, intent Details, overflow and keyboard/gamepad focus before UI stories. Retain screenshots and interaction/trace evidence covering dense six-enemy layouts, actual localized content, contrasting states and effects; none exists for this design yet.

## Cross-References

| Document | Contract |
|---|---|
| [Game Concept](game-concept.md); [Art Bible](../art/art-bible.md), 7.3–7.5 and 7.9 | Player pillars, protected layer, status/intent/order and popup constraints. |
| [Event Log](event-log.md); [Combat State and Step Model](combat-state-and-step-model.md) | Event order, baseline, immutable cursor views and causal identity. |
| [Turn Order](turn-order.md); [Enemy and Intent](enemy-and-intent.md); [Party and Heroes](party-and-heroes.md) | Forecast, bound intent, party identities and HP/shield/speed. |
| [Combat Resolution](combat-resolution.md); [Run State and Flow](run-state-and-flow.md) | Action boundary, outcomes, technical stop, playback gating and retention. |
| [Status Effects](status-effects.md); [Encounter Generation](encounter-generation.md) | Status amounts and actual legal encounter roster. |
| [Backpack UI](backpack-ui.md); [Hover Popup and Glossary](hover-popup-and-glossary.md) | Workbench follow, Details content, input and safe placement; user decision needs cross-document synchronization. |
| [Systems Index](systems-index.md) | HUD dependencies, MVP scope and pre-UI core checkpoint. |

## Acceptance Criteria

These are design criteria, not tests performed in this session. **BLOCKED** marks an unresolved integration or UX contract; **NOT ASSESSED** means actual presentation evidence is required.

1. **GIVEN** encounter baseline `S0` and trace views `S1..Sn`, **WHEN** the playback cursor is at baseline, a mutation or a nonmutating fact, **THEN** the HUD uses the corresponding baseline/post-view or preceding view, never the later live state. A victory that has already thawed the live grid does not reveal reward or outcome ahead of the cursor.
2. **BLOCKED — intermediate views. GIVEN** 8 damage against shield 5 and HP 3, **WHEN** the historical views are inspected, **THEN** their ordered pairs are `(shield 0, HP 3)` and `(0, 0)` and the shared damage field is not counted twice. Whether both are separately displayed must be resolved with System 29's proposed beat-boundary cursor before an on-screen criterion is asserted.
3. **GIVEN** the last enemy drops to zero HP and a legal queued reaction then downs the last hero, **WHEN** each step is presented, **THEN** each downed state appears but no premature outcome is announced; only `CombatEnded(Draw)` at the cursor shows Draw. A control without the lethal reaction shows Victory only at its terminal fact.
4. **GIVEN** an enemy with next index `n=k`, **WHEN** it has `ActionSkipped(stunned)`, **THEN** its turn is spent and its tab still shows entry `k`. **WHEN** its `ActionStarted` uses entry `k` and advances `n`, **THEN** the executing move comes from that action record and the next-entry tab does not masquerade as the move striking.
5. **GIVEN** a target already downed, **WHEN** a hit yields `EffectSkipped(target_downed)`, **THEN** the hit is identified as skipped with no retarget or new RNG. A targetless, damage-free windup shows no invented target or number.
6. **GIVEN** already-bound sequences with `H=12` and controls with `H=6` and `H=20`, **WHEN** Details is read before combat or through the user-selected playback surface, **THEN** all `H` entries are available in order, with applicable move, hit count, target, per-hit damage before shield and wrap after `H`; the default 12 is not treated as a hard cap. Playback access remains blocked on the interfaces in criterion 7.
7. **BLOCKED — playback Details integration. GIVEN** enemy intent Details open in the workbench, **WHEN** System 29 advances the cursor and System 25 automatically follows another acting hero, **THEN** playback continues, the selected enemy's Details remains associated with its valid source when the area is safe, and no gameplay/flow command or trace/RNG mutation occurs. Systems 5/6/26/29 and UX must specify retained state, input, source-down, Skip and unsafe-layout fallback before this can pass.
8. **GIVEN** post-victory DoorChoice before selecting the next door, **WHEN** the HUD is viewed, **THEN** it does not show old or unbound next-room intent. **WHEN** a door is selected and the new sequence is bound, **THEN** planning shows that sequence and editing the arrangement draws no intent RNG.
9. **GIVEN** `K=6`, **WHEN** arrangement edits or an HP step downs a unit, **THEN** System 11's forecast is refreshed from the corresponding planning/cursor state and labelled 'if nothing changes'. `BufferTooSmall` or `ContractViolation` clears stale output and shows unavailable, without reading sealed future actions.
10. **BLOCKED — projected enemy glyphs. GIVEN** one enemy appears repeatedly in a forecast and its bound sequence wraps or it may skip while stunned, **WHEN** the strip assigns each occurrence an intent glyph, **THEN** the mapping and assumptions come from a defined Systems 11/14/27 adapter, never future trace events. The adapter, in-action cadence and fixtures remain to be defined.
11. **GIVEN** a legal gauge value 1000 or 1199, **WHEN** the HUD reads the owner state, **THEN** it neither rejects nor changes the value. Any visual gauge mapping must be tested after UX defines its meaning and saturation.
12. **GIVEN** a legal roster over two vertical lines, **WHEN** a unit is downed, **THEN** surviving enemy slots do not reflow, no MVP reinforcement appears, the MVP boss remains alone, and line placement does not choose a target.
13. **BLOCKED — status access. GIVEN** more than three status kinds or an amount longer than a badge, **WHEN** shown, **THEN** the actual values are not capped by the display and a route to all required exact values and sources exists. The combat overflow/large-number reading surface remains undefined; intent inspection does not automatically provide it.
14. **GIVEN** the same valid trace played at 1×, 2×, 4× or Skip, **WHEN** the gameplay terminal boundary is reached, **THEN** final view and outcome agree, the trace is unchanged and reward handoff is not duplicated. Details are closed at every Skip fast-forward leg (System 29 Rule 11); retaining them elsewhere still requires the System 29/26 lifecycle contract.
15. **BLOCKED — technical-stop handoff. GIVEN** a technical stop after a valid prefix, **WHEN** the owner reports the stop, **THEN** the prefix is retained and no `CombatEnded`, defeat or reward is fabricated. System 29 plays that prefix and lets Skip stop at its Detonations and then go to its end; reconcile this with System 12's return path and Systems 5/31 before asserting navigation behavior.
16. **NOT ASSESSED — visual/accessibility evidence. GIVEN** a dense six-enemy fixture, real localized strings, readable Details, status/intent/order/HP and overlapping payoff effects, **WHEN** fixed-tick screenshots and interaction traces are captured at target sizes and speeds, **THEN** protected information is legible and unoccluded and a safe Details route exists. Font, contrast, reduced motion and playtest evidence do not yet exist.
17. **GIVEN** hero A was selected before the fight, **WHEN** a normal playback Ready step belongs to B, **THEN** System 25 follows B; a Skip fast-forward leg does not follow (a Forced span does), and after combat the workbench returns to A. An open valid and safely placed enemy-intent Details is not closed merely by automatic follow.
18. **GIVEN** read-only intent inspection at a fixed trace cursor, **WHEN** opening or closing the surface, **THEN** simulation state, trace and RNG remain unchanged; the cursor advances only through System 29 playback, not the inspection action.
19. **GIVEN** a `ChainLimit` fact in an otherwise valid encounter, **WHEN** it reaches the presentation cursor, **THEN** it is not treated as a technical stop or fabricated terminal outcome; subsequent owner-recorded processing can continue. System 12's Rule 6 correction still needs independent review.
20. **NOT ASSESSED — causal readability with Details. GIVEN** the same representative payoff sequence with Details open versus closed, including 2×/4× and automatic hero follow, **WHEN** participants identify the acting item/source and event order, **THEN** retain their responses, timings and captures and evaluate against a pass criterion set by UX before the test. Confirm the Details surface does not obscure the source plate or other protected information; do not claim readability or a pass until actual evidence exists.

## Open Questions

| Question | Owner | Resolve by |
|---|---|---|
| How are trace baseline, cursor-aligned per-event views and the frozen pre-fight arrangement retained and supplied to Systems 25/26/27/29 after the live run has advanced? | Systems 4/5/25/27/29/36 | Before playback integration. |
| Does a shield/HP atomic effect expose both historical views as visible steps, or one beat-end view? System 29's proposed cursor moves only at beat boundaries. | Systems 4/27/29/36 | Before cursor and HUD integration. |
| How do the action record, lifted acting glyph and newly advanced intent `n` distinguish executing from next intent without misattribution? | Systems 14/27/29 | Before HUD/playback integration. |
| How does the actor-only forecast map repeated enemy appearances to the correct bound-entry glyph, including wrap and skipped-turn assumptions, without reading future trace events? When is it refreshed during an action? | Systems 11/14/27/29 | Before forecast-strip integration. |
| What input opens, focuses and closes read-only intent Details during continuing playback, and what happens if the source is downed? | Systems 6/26/27/29, UX designer | Before interaction implementation. |
| Can workbench Details stay readable at each speed without covering protected content or important backpack causes? What is the safe fallback? | Systems 25/26/27/29, art/UX | Before HUD UX sign-off. |
| How are status overflow, exact large numbers and detailed status sources accessible while stage popups are banned? | Systems 6/13/26/27, UX designer | Before combat accessibility sign-off. |
| What does the Art Bible's 'speed gauge' represent, and if displayed how is a legal gauge above the readiness threshold shown? | Systems 11/27, UX designer | HUD specification. |
| What exact screen and navigation communicate a technical stop, and how is a valid prefix played or skipped? | Systems 5/12/29/31, UX designer | Before failure-path implementation. |
| How are larger text, translations, contrast, reduced motion and intense simultaneous effects verified with real data? | Systems 7/29/37, art/UX | Accessibility and HUD UX review. |
| Which Unity UI stack and rendering strategy work with the pinned version? | Technical director | Architecture decision and engine spike. |
| How will System 4's remaining action/gauge, intent-binding and cooldown schema questions be resolved before cursor-based integration? | Systems 4/9/11/14/36 | Before combat-core re-review and integration. |
| Answered by System 6 ([input-and-focus.md](input-and-focus.md), 2026-11-13): during playback only Speed (1, 2, 3), Skip (K), Pause, Abandon (confirmed), Navigate and read-only Details on the intent tabs are live; Skip changes presentation only. | Systems 6 and 27 | Next revision of this GDD. |
| Answered by System 29 ([payoff-presentation.md](payoff-presentation.md), 2026-11-14): speeds 1x, 2x, 4x and Skip (K, one press, locked for 250 ms after Start); Skip (revised 2026-11-14) arms a latched Skip that closes an open intent Details at each fast-forward leg, jumps to just before the next Detonation root action (a plate sweep, an orient hold and a "Skipped N actions" line), plays that root action in full with the workbench following the acting hero again, and finally jumps to the terminal view and the banner (keys 1, 2, 3 cancel the arm); Pause stops the playback clock; a technical stop presents the valid prefix and ends in `Stopped` with no outcome and no Defeat; the HUD reads `IPlaybackView` and `ViewAt(cursor)`. | Systems 27 and 29 | Next revision of this GDD. |
