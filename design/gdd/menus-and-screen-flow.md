# Menus and Screen Flow

> **Status**: Designed (pending independent design review)
> **Author**: User and design team
> **Last Updated**: 2026-11-13
> **Implements Pillars**: 2 — The Payoff Is the Product; 4 — Arrangement Is the Thinking
> **Creative Director Review (CD-GDD-ALIGN)**: APPROVED 2026-11-13 — pillar alignment only; integration and UX evidence remain open.

## Summary

Menus and Screen Flow gives the player a clear way to start, pause, change settings, see a compact run result and choose to inspect the last fight. It presents System 5's actual state without changing combat or rewards, and keeps Restart quick while leaving the player free to read and decide at their own pace. Command, retention and first-launch storage interfaces still require independent review and owner agreement.

> **Quick reference** — Layer: `Presentation` · Priority: `MVP` · Key deps: `Run State and Flow (5), Input and Focus (6), Settings (37), Payoff Presentation (29), Post-combat Breakdown (30)`

## Overview

Menus and Screen Flow organizes the first launch, entry into a run, pause/settings, the end-of-run summary and access to last-fight review. System 5 owns the authoritative run state and transitions; System 31 presents them and routes explicit, validated user actions. System 6 owns input and System 37 owns preference storage. A menu or summary cannot produce rewards, alter deterministic combat, replace a trace or make up an outcome.

The old index describes an end screen with Restart, but later Systems 6, 29, 30 and 37 require pause, settings, first-launch reduced-flashes prompt and last-fight access as well. The user chose to open the pause menu when the window deactivates during playback and to show a compact end summary with a button for the last-fight breakdown. Existing MVP defers run save/resume, replay and a persistent run Continue feature. System 29's payoff design remains a draft, while Systems 5, 6, 30 and 37 await independent review.

The project pins Unity 6000.6.3f1. The local Unity UI reference is for 6.3 and has not been reverified for 6.6; UI technology, scene transitions and clipboard APIs require the architecture/engine spike. No implementation or engine behavior is claimed here.

## Player Fantasy

After a run, the player sees what happened, understands enough to make the next decision, and can start assembling another party without being punished by menus. A compact summary makes the outcome and the room reached clear; a deliberate choice opens the detailed explanation of what the last arrangement did. Loss and Draw are informative, not blame or a fabricated enemy attack; victory has room to feel earned.

The menu protects the rhythm of watching the machine: leaving the window during playback pauses the presentation, and returning shows a menu so the player decides when to resume. Menus serve the payoff and arrangement pillars without altering the fight, rewards or random draws. UX/art consultation informed these goals; no playtest or visual evidence has been collected.

## Detailed Design

### Core Rules

1. **Boundary.** System 31 owns menu and screen presentation, navigation and focus restoration. System 5 owns run states, authoritative result, command legality and serial/run ID; 6 owns semantic input/focus and 37 owns preferences. The menu cannot rerun combat, synthesize a result, award a reward, advance the playback cursor or edit a backpack. Closing/opening screens is presentation-only unless the player explicitly confirms an owner-approved run command.
2. **MVP surfaces.** Provide a minimal title/start surface, the System 37 settings and first-launch Reduced flashes prompt, a pause menu, a compact end summary for gameplay outcomes, and a separate technical-stop diagnostic. Route from the summary to System 30's last-fight breakdown; no full history or replay is implied. The earlier index's end-screen-only stub is superseded by the interfaces these existing GDDs require. The exact title artwork and layout are UX/art decisions, not new progression features.
3. **First launch.** Reduced flashes is on by default; before the first fight, show the first-launch prompt required by Systems 29/37, with accessible reading time and a way to reach Settings. Do not assume the user accepted a different setting or suppress the prompt on a failed durable completion-marker write. Marker ownership, retry after failed save and exact first-launch language/Reduced motion default remain blocked with Systems 7/37; the flow must not claim that prompt completion was saved when it was not.
4. **Begin and Restart.** A title action requests a new run through System 5's owner boundary and proceeds to party choice. From an eligible Won/Lost summary, Restart requests a new session, obtains a fresh seed from the seed port and opens party choice with the prior three heroes preselected; the player confirms the party. `PickParty` remains authoritative and may reject invalid data. A fresh draw is not a mathematical guarantee that its value differs from the previous seed: System 5's AC 13 must reconcile its 'different' claim before the integration is considered complete. If obtaining a seed fails, display a recoverable error and preserve the existing session/summary; do not consume a gameplay RNG draw or silently create a fake seed. No user seed input, reroll, retry or run resume is added in the MVP.
5. **Pause and focus loss.** During playback opening the menu pauses System 29's presentation clock; closing it resumes through its specified 250 ms fade, with no catch-up or cursor reset. The user chose that window deactivation while watching a fight pauses and opens the menu; on return it stays paused until the player closes the menu. System 6's `InputReset` clears pressed keys/buttons/repeats; a drag-origin held item is returned and a click-origin hold remains held if focus loss occurs in arrangement. For other run phases, exact menu entry and held/popup handling still need Systems 5/6/25/26/31 agreement. No menu or pause reveals a result before the playback gate.
6. **Escape and focus.** Follow System 6's priority: cancel rebind capture, close dialog, close popup, cancel a held item, disarm an armed card, close menu, then open it. Modal confirmation traps focus, swallows opening input and defaults to the safe choice; Esc cancels. A closing surface returns focus to its opener, else same-zone fallback then state anchor, following System 6. The default System 6 end-summary focus is Restart; the player can navigate to Menu, seed copy, room rows and the breakdown button. Exact order for the new breakdown control is a screen UX contract, not a new global input binding.
7. **Abandon and exit.** Leaving an active run uses an explicit menu action and a confirmation with 'Keep playing' as the safe default; cancel returns to the same state and focus. Never substitute `QuitToMenu` to bypass the confirmation. System 5 distinguishes confirmed active-run Abandoned from defeat, but its playback edge case preserves a Won/Lost result already resolved underneath while an already-resolved ordinary victory with a pending reward becomes Abandoned. Respect the owner result without revealing it before its playback gate; exact presentation while exiting is a handoff to reconcile, not a new scoring rule. After a terminal state, returning to menu is not an extra abandon. The names/legal states of `QuitToMenu`, `ReturnToMenu`, `AbandonRun` and direct Restart from `Stopped` conflict inside System 5; implementation is **BLOCKED** until its owner resolves them.
8. **Compact gameplay summary.** After the playback gate, show the actual Won or Lost outcome, reached room, reason and recorded decisive source when available, and a read-only copyable seed; show actual per-room rows and committed direction only if System 5 provides them. Draw, ActionLimit and NoEligibleActor use their true reason; never manufacture an enemy intent. Show biggest hit, longest chain and the personal best where System 30 provides validated values; otherwise show unavailable, not zero. The user chose a compact result with a button to open the detailed last-fight breakdown; the button does not itself change the outcome or reward. Return restores the same summary and focus.
9. **Review and retention.** System 30 owns bars, attribution, historical views and the last-fight panel. System 5 owns retained trace/baseline/frozen arrangement and release. Show a disabled or unavailable reason if the matching retained record is absent; never reconstruct a fight from a seed or guess current placement. System 30 proposes keeping the latest fight through panel close and grid edits until next accepted Start or run exit, but that retention, historical-view interface and in-memory restore are **BLOCKED** owner contracts. A room victory's analysis returns to the actual System 5 state and any pending System 28 reward step; it is not an end-of-run summary. Room 1 skips BuffClaim; room 8 enters boss preparation after any eligible rewards, or directly if both are auto-skipped.
10. **Technical stop.** `Stopped` is a separate diagnostic surface, not a defeat, Draw, completed encounter, reward or personal-best update. Expose a safe return-to-menu action after the owner handoff; whether a valid partial trace prefix may be viewed and whether Restart is legal directly from Stopped are unresolved across Systems 5/12/29/30. A stopped prefix is never labelled a completed fight. No result is displayed until the agreed technical-stop/presentation boundary; do not assume System 29's draft prefix playback resolves the System 12/5 mismatch.
11. **Settings and persistence.** System 37 owns settings changes, input binding data, unsaved/retry feedback and the user-selected volume/mute. System 31 provides the surface and focus; returning from Settings must not issue a run command or dismiss another decision. A persistent personal best is an MVP requirement chosen in System 30; its metric, owner and write/recovery contract remain unresolved and cannot be cleared by Settings reset. There is no saved-run Continue in MVP; a later Continue entry belongs to System 24.
12. **Accessibility and safety.** Every menu and result interaction has a mouse-only and keyboard-only path, visible focus and a text/shape cue independent of color or sound; no timed or hold-only action. Gamepad remains the documented MVP waiver. Follow System 7's Vietnamese/English text, missing-language handling and Standard text; global Large remains gated on font and all-screen validation. Preserve System 6's 250 ms commit-input lock as a minimum safety delay, not a deadline, and do not make a fast restart target mandatory for the player.

### States and Transitions

| Presentation state | Entry / exit | Authority and constraints |
|---|---|---|
| Title / new run | User selects start; owner accepts creation | System 5 owns seed/session, then party pick; no saved-run Continue in MVP. |
| First-launch prompt | Before first fight when prompt completion has not been durably established | Reduced flashes defaults on; marker failure/retry contract blocked. |
| Run view | Party/arrangement/rewards/playback | System 5/28/29 own state and playback; 31 does not advance it by presenting. |
| Paused menu / Settings | Menu requested or playback window loses focus | Clock paused; close resumes through System 29 handoff. InputReset preserves System 6 held-item semantics. |
| Abandon confirmation | Explicit abandon from active run | Safe default/cancel; confirmed action is validated by System 5. |
| End summary | Owner Won/Lost and playback gate released | Compact result; Restart, Menu, copy seed and last-fight details if retained. |
| Last-fight details | User selects breakdown | System 30's read-only view; close returns to exact prior summary/focus. |
| Technical stop | Owner reports stop at the agreed boundary | Diagnostic and safe return; never a fabricated loss or reward. |

### Interactions with Other Systems

| System | Inputs / boundary |
|---|---|
| 5 Run State and Flow | Run owner provides state, outcome/reason, room, seed, summary data and accepts/rejects commands. Legal-state/name conflicts remain blocked. |
| 6 Input and Focus | Provides Esc priority, `InputReset`, input lock, safe modal and focus rules. |
| 7 Localization and Text; 37 Settings | Supplies keyed Vietnamese/English text, preferences, first-launch prompt marker and save-error state. |
| 29 Payoff Presentation | Supplies playback gate and pause/resume behavior; draft stop-path and timing interfaces need reconciliation. |
| 30 Post-combat Breakdown; 28 Door and Room Panels | Provide read-only last-fight analysis and reward controls; owner flow and retained data are not changed by viewing. |
| 24 Run Save and Resume; 32 Audio; 34 Accessibility | Deferred or later full features; no unimplemented Continue, full sound system or gamepad support is claimed. |

## Formulas

There is no gameplay or scoring formula in System 31. It consumes two existing timing contracts, which are not player deadlines:

`lockUntil = t_view + InputLockMs`; a commit-class press with `t < lockUntil` is ignored, and one at or after the boundary may be considered by its owner. The lock starts on the visible state/screen change and never blocks Esc. It is owned by System 6; a press during the lock is discarded, not delayed until later.

| Variable | Type / range | Owner | Meaning |
|---|---|---|---|
| `t_view` | timestamp, milliseconds | System 6 | Time the new visible screen/state is presented. |
| `InputLockMs` | default 250 ms; design range 100–500 ms | System 6 | Minimum commit guard after the view changes. |
| `lockUntil` | timestamp, milliseconds | System 6 | Boundary for accepting a new commit press. |

**Example:** a screen shown at 10,000 ms ignores a commit at 10,200 ms; a valid press at 10,250 ms is eligible for owner validation. System 29's 250 ms playback resume fade is a separate presentation contract, not a second commit lock.

The System 5 restart-time target is **under about 5 s from end screen to the new party-pick screen**, with at most two confirmations as a usability target. It must be measured under declared conditions and never imposes a response deadline; Restart → party Confirm remains the flow. It is not evidence that the target is met.

## Edge Cases

- **While playback is still visible:** outcome/rewards stay hidden, even when the authoritative run state already reached a terminal boundary. A pause menu or an abandon dialog cannot leak the result.
- **Window deactivation during playback:** pause and open the menu as the user chose; no catch-up on return. System 6 clears pressed controls, returns a drag-held item and preserves a click-held item. Closing the menu is the deliberate resume action. Menu behavior on deactivation in other phases remains with the owners.
- **Deactivation during Skip or while a dialog/Details is already open:** the pause must not create a duplicate menu or let hidden input commit; System 6/29/31 still need to define layering and focus restoration; System 29 Rule 11 answers the Skip part: an armed Skip survives a pause (the clock stops, the arm stays) and the fast-forward leg or Detonation resumes after the 250 ms fade-in.
- **Abandon while a terminal result has already resolved:** preserve System 5's Won/Lost or Abandoned classification according to its actual outcome/reward state, without inventing defeat or showing hidden results; the cross-system command handoff is blocked.
- **Seed request fails:** leave the existing summary/session intact, show a recoverable error and do not create a replacement session. A successful fresh entropy request may happen to yield the same numerical seed; System 5's stricter AC needs reconciliation.
- **Seed copy is unavailable:** keep the read-only value and show a localized failure; do not claim the copy succeeded or alter the run. The platform clipboard adapter is not verified for Unity 6.6.
- **Double click / held confirmation:** System 6's transition lock and repeat rules consume stale presses; the owner additionally validates run ID and visible-state serial. At most one accepted run-changing action results.
- **Missing trace, historical placement or personal best:** show unavailable with the correct reason, not zero or a fabricated source; retain the end summary and its actionability.
- **Technical stop or action limit:** a technical stop is diagnostic, with no completed outcome/reward; ActionLimit is System 5's Lost reason, and must not be labelled a crash.
- **System 37 cannot store a setting or prompt marker:** retain its unsaved indication and current-session behavior; menu closure never labels a failed write successful. First-prompt retry policy is still open.
- **Settings or a dialog are opened above a held item, reward, popup or last-fight view:** preserve the owner state and focus; pointer-entry precedence and any disallowed nesting must be resolved with Systems 5/6/25/26/28/30/37, rather than changing or dropping an item by inference.
- **Changing language while paused:** System 7 re-resolves visible text with the same arguments; preserve cursor and current focus as far as the owner contract permits. Missing locale follows System 7 fallback and error behavior.
- **Game closes or crashes:** MVP cannot resume a run; settings and personal-best storage have separate owners. Never label a cross-launch personal best as a saved run.

## Dependencies

| Owner / client | Contract | Design status |
|---|---|---|
| 5 Run State and Flow | Authoritative states, command serial/run ID, outcome, seed, Restart, Abandon and end summary data. | Designed pending review; technical-stop/return names, seed uniqueness and playback handoff conflict. |
| 6 Input and Focus | Focus, Esc priority, repeat suppression, input lock, `InputReset`, accessible modal. | Designed pending review; exact new-screen focus graph requires UX. |
| 7 Localization and Text; 37 Settings | Localized menu text, preferences, unsaved feedback and Reduced flashes prompt/marker. | Designed pending review; prompt marker and defaults unresolved. |
| 29 Payoff Presentation | Playback, pause, 250 ms resume fade and presentation gate. | Draft pending gate/review; technical-stop boundary open. |
| 30 Post-combat Breakdown | Last-fight button and detail view, known causes and record metrics. | Designed pending review; retained trace/historical layout/record storage blocked. |
| 25 Backpack UI; 26 Hover Popup; 28 Door Panels | Held-item, popup and reward contexts under a menu. | Designed pending review; coexistence and focus need UX. |
| 24 Run Save and Resume; 32 Audio; 34 Accessibility | Later save/resume and full audio/accessibility. | Deferred; gamepad waiver and actual evidence remain open. |
| Unity 6000.6.3f1 / UI architecture | Scene/view lifecycle, actual clipboard, rendering and focus integration. | Existing UI reference is for 6.3, not reverified for 6.6; architecture choice and engine spike pending. |

## Tuning Knobs

System 5 owns the provisional restart target of under about 5 seconds to the new party-pick screen; measure it rather than forcing a countdown. System 6 owns its 250 ms input lock, with a documented safe range of 100–500 ms, and System 29 owns the separate 250 ms resume fade. Layout density, screen transition duration, scroll and copy feedback await screen UX and real Vietnamese/English measurements; none changes simulation state or constitutes a safety claim.

## Visual/Audio Requirements

- Follow Art Bible 2.3.1: calm Ink/lantern visual tone; a pause overlay dims/desaturates one step without covering essential backpack information. Victory can feel warm; defeat and Draw are clear and non-punitive.
- Use text and shape in addition to colour; settings, focus, loss reason, technical stop and disabled breakdown must remain interpretable without sound. Music or jingles are not required by this GDD.
- Localize all player-facing labels and reasons through System 7. Verify the chosen font, Vietnamese/English line wrapping, actual screen size and focus visibility before accepting layout.
- Real screenshots, contrast, photosensitivity, motion and sound-off checks are **NOT ASSESSED**; no visual or accessibility compliance is claimed.

## Game Feel

The end summary acknowledges a run without holding the player hostage: they can read it, inspect the last fight at their own pace, or start again. A menu pause protects their place when attention leaves the game. Confirmations explain the consequence of abandoning a run and make the safe path obvious, without unnecessary timed actions.

## UI Requirements

- Provide a compact outcome/room/reason summary, a read-only seed with copy control and accessible feedback, a Restart control, return to menu and an explicit last-fight detail control. Expose per-room rows and verified metrics only when owner data is available; show unavailable honestly.
- During gameplay, provide an accessible pause menu, Settings and confirmed Abandon. The prompt before the first fight exposes Reduced flashes on by default and does not time out.
- Mouse-only and keyboard-only navigation, visible focus, safe default for destructive confirmation, non-colour feedback and System 6's Esc precedence are mandatory. Gamepad is a later contract; global Large text requires its existing validation gate.
- `/ux-design` and `/ux-review` must define title, pause, first-launch prompt, result/diagnostic screens, details handoff, held-item/menu coexistence and localization; retain actual captures and interaction evidence before UI stories.

## Cross-References

| Document | Relationship |
|---|---|
| [Game Concept](game-concept.md), [Systems Index](systems-index.md), [Art Bible](../art/art-bible.md) | Pillars, MVP scope, title/pause/outcome mood. |
| [Run State and Flow](run-state-and-flow.md) | Run states, seed, Restart, Abandon, reward and summary ownership. |
| [Input and Focus](input-and-focus.md), [Localization and Text](localization-and-text.md) | Input locks, focus/Esc, mouse/keyboard and language fallback. |
| [Settings](settings.md), [Payoff Presentation](payoff-presentation.md) | First-launch prompt, preference storage, pause, speed and playback boundary. |
| [Post-combat Breakdown](post-combat-breakdown.md), [Door and Room Panels](door-and-room-panels-ui.md) | Last-fight details, trace retention and exact reward return. |
| [Backpack UI](backpack-ui.md), [Hover Popup](hover-popup-and-glossary.md), [Accessibility Requirements](../accessibility-requirements.md) | Held-item, popup, focus and MVP accessibility obligations. |

## Acceptance Criteria

These are design criteria, not tests performed. **BLOCKED** means an owner contract or engine capability remains unresolved; **NOT ASSESSED** needs actual UX, performance or accessibility evidence.

1. **BLOCKED — first-launch marker. GIVEN** a first launch with no completed prompt, **WHEN** the player approaches the first fight, **THEN** Reduced flashes is on and an accessible prompt appears before that fight. A marker is considered durable only on storage success; Systems 31/37 must define retry after failure.
2. **GIVEN** boss victory, defeat, Draw, ActionLimit or NoEligibleActor, **WHEN** playback has not reached its gate, **THEN** no final outcome, reward or result is revealed. Once the owner gate permits, show the real result, room and reason without inventing an enemy move.
3. **GIVEN** an eligible Won/Lost end result after the gate, **WHEN** the end screen opens, **THEN** the player sees the compact summary and an explicit last-fight details control; the full System 30 breakdown does not open until selected. Closing details returns to the summary and its invoking focus.
4. **BLOCKED — retained source. GIVEN** a matching trace and frozen layout, **WHEN** last-fight details are requested after the grid changed, **THEN** System 30 shows the historical source, never a current-grid guess. If the owner says the trace is unavailable, show that reason instead of fabricating data; Systems 5/25/29/30 must agree retention and view contracts.
5. **GIVEN** ordinary victory in room 1 or room 8, **WHEN** the result panel closes, **THEN** the actual System 5 state and any pending reward step remain unchanged; room 1 has no BuffClaim, and room 8 enters boss preparation after eligible rewards, or directly if both steps auto-skip, not the end summary.
6. **GIVEN** a Stopped technical diagnostic, **WHEN** its agreed presentation boundary is reached, **THEN** it is not labelled defeat or Draw, awards no reward and records no invented personal best. Direct Restart and player review of a partial trace remain **BLOCKED** by owner disagreement.
7. **GIVEN** playback at a mid-beat cursor and a window deactivation, **WHEN** the window loses focus, **THEN** the clock stops and the pause menu opens. On return it stays paused; closing the menu resumes through System 29's 250 ms fade without catch-up, cursor reset or premature outcome.
8. **GIVEN** pressed keys and repeats plus an item held in arrangement, **WHEN** InputReset occurs, **THEN** the controls clear; a drag-origin held item returns, while a click-origin held item remains held. Whether and how a menu opens in that arrangement context remains an owner/UX contract.
9. **BLOCKED — layered pause. GIVEN** a Skip, dialog or Details is active when playback loses focus, **WHEN** pause is requested, **THEN** the UI does not stack duplicate menus or execute a hidden commit. Systems 6/29/31 must define overlay order and return focus before this can pass (Skip completion is answered by System 29 Rule 11: an armed Skip survives a pause and resumes after the fade-in).
10. **GIVEN** active rebind capture, dialog, popup, held item, armed card or menu, **WHEN** Esc is pressed, **THEN** exactly the highest-priority applicable System 6 action occurs. In an abandon confirmation, 'Keep playing' is initially focused and Esc cancels without changing run state.
11. **BLOCKED — abandon handoff. GIVEN** a player confirms Abandon during playback, **WHEN** System 5 has already resolved Won/Lost versus an ordinary victory pending rewards, **THEN** the owner preserves the proper existing result or records Abandoned as specified, without leaking the hidden outcome. Resolve conflicting command names and allowed states before integration.
12. **GIVEN** a valid Won/Lost summary and available seed source, **WHEN** Restart is accepted, **THEN** one new seed request produces a new session and the previous three heroes are preselected for an explicit party confirmation. A fresh draw may equal the old numeric value; System 5's contrary uniqueness criterion remains **BLOCKED** pending owner ruling.
13. **GIVEN** seed acquisition fails, **WHEN** Restart is requested, **THEN** the old summary/session remains intact, no fake seed or partial new run appears, and a recoverable error is shown.
14. **GIVEN** a view transition at 10,000 ms and the default 250 ms commit lock, **WHEN** a commit is pressed at 10,200 ms, **THEN** it is discarded; a valid new press at 10,250 ms can be passed to the owner. Esc is not locked. This is a minimum delay, not a deadline.
15. **GIVEN** a double click, held Enter or queued command from a previous session, **WHEN** Restart or another run-changing control is processed, **THEN** repeats, view-generation/serial checks and run ID prevent a second accepted command or a command into the new run.
16. **GIVEN** the end screen exposes a read-only seed, **WHEN** copy succeeds, **THEN** the UI gives non-colour success feedback; if the platform clipboard is unavailable it shows failure and preserves the seed and game state. Clipboard implementation is **BLOCKED** pending the pinned-engine/platform spike.
17. **GIVEN** a failed Settings write, **WHEN** the player returns to the menu or summary, **THEN** the unsaved state remains truthful and retry is accessible; neither run snapshot nor the separately owned personal best is reset.
18. **GIVEN** Settings is opened during a paused fight and the language is changed, **WHEN** it closes, **THEN** visible text re-resolves with original values, playback does not restart and no combat, reward, item or RNG command is issued. Screen/focus coexistence remains **BLOCKED** on UX and owner integration.
19. **GIVEN** an absent or invalid metric, source or personal-best record, **WHEN** the summary is shown, **THEN** it identifies the value as unavailable rather than presenting zero or a fabricated cause; no run history or replay is inferred from a seed.
20. **NOT ASSESSED — screen evidence. GIVEN** Vietnamese and English title, first-launch, pause, abandon, summary, details and technical-stop fixtures, **WHEN** actual mouse-only and keyboard-only interaction and screenshots are reviewed, **THEN** text, focus, safe choices, overflow and state feedback are legible without colour or sound alone. No current visual or accessibility pass is claimed.
21. **NOT ASSESSED — pacing. GIVEN** the Restart → party-pick path, **WHEN** measured under a declared fixture and device, **THEN** report whether the System 5 target of under about 5 s is met; it never becomes a deadline imposed on the player.

## Open Questions

| Question | Owner | Resolve by |
|---|---|---|
| Which command names and legal transitions cover returning to menu, confirmed active-run abandon and Restart from Stopped? System 5's rule, table and `QuitToMenu` text conflict. | Systems 5/6/31 | Before run/menu command integration. |
| How is a resolved Won/Lost versus pending-reward Abandon during playback handed off without exposing the hidden result? | Systems 5/12/29/31 | Before playback exit integration. |
| Does Restart require only a fresh entropy draw or guaranteed different numerical seed, and how is a seed-source failure reported atomically? | Systems 1/5/31 | Before Restart implementation and System 5 AC reconciliation. |
| Which phase boundary, prefix visibility and safe return apply to technical stop? | Systems 5/12/29/30/31 | Before diagnostic UX. |
| Who owns the first-launch prompt marker, failed-write retry and first-launch language/Reduced motion defaults? | Systems 7/29/31/37 | Before first-launch implementation. |
| How are click-held/drag-held items, popups, pending rewards, Settings and details preserved and focused when the menu opens or closes? | Systems 5/6/25/26/28/30/31/37, UX | Before screen UX approval. |
| How do Skip, nested Details or a modal interact with deactivation and a single pause menu, and where does focus return? | Systems 6/29/31, UX | Before playback pause implementation. |
| How do Systems 5/25/29/30 retain and identify the last complete fight's trace and original arrangement, including in-memory restore? | Systems 4/5/25/29/30 | Before end-summary breakdown integration. |
| Which personal-best metrics are stored across launches, by whom, and what are update, missing/corrupt and recovery rules? | Systems 5/30/31/37, architecture | Before record implementation. |
| Which UI technology, clipboard API, layout, font and screen sizes meet requirements on pinned Unity 6.6 and Windows? | Technical director, UX/art | Architecture/engine spike and screen UX. |
