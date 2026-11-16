# Settings

> **Status**: Designed (pending independent design review)
> **Author**: User and design team
> **Last Updated**: 2026-11-13
> **Implements Pillars**: 2 — The Payoff Is the Product; 4 — Arrangement Is the Thinking
> **Creative Director Review (CD-GDD-ALIGN)**: APPROVED 2026-11-13 — pillar alignment only; storage, integration and UX evidence remain open.

## Summary

Settings lets the player choose playback speed, language, reduced effects, input preferences and the MVP effects volume and mute without changing combat. It coordinates validated preferences and proposed cross-launch storage; the menu, first-launch flow, safe persistence and accessible presentation still require their owner contracts and independent review.

> **Quick reference** — Layer: `Foundation` · Priority: `MVP` · Key deps: `Input and Focus (6), Localization and Text (7), Payoff Presentation (29), Menus and Screen Flow (31)`

## Overview

Settings gives the player control over how the game is presented and operated. It owns preferences and coordinates their validation, application and storage while each feature's owner retains its own rules. It does not save or resume a run and cannot alter the deterministic simulation, combat trace, random stream, outcome or reward.

The index's original speed-only MVP stub conflicts with later existing GDDs: System 29 requires playback speed plus reduced flashes and reduced motion, System 7 requires language choice, and System 6 requires configurable repeat and a binding-storage contract. These are incorporated here without silently cutting those decisions. The user chose an effects-volume slider and a mute button for MVP. System 31 has not yet been designed, so the menu's placement, opening availability, first-launch screen and focus-return interface remain provisional.

The project pins Unity 6000.6.3f1. `docs/engine-reference/unity/modules/core.md` does not exist and the UI/input module notes cover 6.3 rather than a verified 6.6 API. Storage paths, atomic write behavior and UI technology must be verified in an architecture spike; no particular engine storage API or implementation is asserted by this document.

## Player Fantasy

The player can watch their carefully arranged machine at a comfortable pace, read its causes in Vietnamese or English, and use the controls in a way that suits them. Lower motion, reduced flashes or silence never remove the information that explains the payoff. These are choices about presentation and access: the game does not become easier or harder and the player's arrangement remains the only way to change the fight.

The creative/UX/art consultation anchors this in Pillars 2 and 4: clear payoff and deliberate arrangement, with no timed input, no colour-only setting state and no audio-only game fact. A visual or photosensitivity test has not yet been performed.

## Detailed Design

### Core Rules

1. **Ownership and scope.** System 37 coordinates player preferences. Systems 6, 7 and 29 validate and apply their own respective values; System 31 owns menu entry/exit and the first-launch prompt. Settings affect presentation or the player's input mapping only. They never mutate an existing encounter trace, seeded RNG, run snapshot/checksum, item placement, result or reward. No run save/resume or replay is implied; the separate persistent personal best from System 30 is outside Reset Settings and its storage owner remains undecided.
2. **MVP settings, respecting later GDDs.** Include playback speed (1×, 2×, 4×), Vietnamese/English, Reduced flashes, Reduced motion, the accessibility option "Skip goes straight to the end" (default off; System 29 Rule 11), navigation key repeat, the binding-storage interface, and the user-selected effects-volume slider plus mute button. The System 6 rebind screen is planned for MVP but built last and is its first cut; cutting it requires the explicit waiver in `design/accessibility-requirements.md`, and does not remove keyboard play. The original speed-only index stub is superseded; System 31's menus are not yet designed.
3. **Defaults and memory.** On first launch propose 1× (System 29), Reduced flashes on (System 29), Standard text (System 7), default bindings and repeat delay 400 ms / interval 125 ms (System 6). Locale selection on first launch and Reduced motion's default are not settled: do not silently assert an OS locale or a value. MVP aims to remember accepted settings through reopening the game, independent of run save; this cross-launch preference lifetime is a design proposal, not a verified implementation or a user choice about System 30's personal record. When a storage write fails, the selected valid preference can apply for the current process but the UI must say it was not saved; do not report it as retained.
4. **Playback speed and Skip.** Speed is one of 1×, 2× and 4×; 0.5× is deferred. System 29 remembers the last selected speed, applies a mid-beat change at the next beat boundary, and keeps the authored Detonation peak timing. Skip is a transient action, never a saved speed; it does not erase the chosen speed. The option "Skip goes straight to the end" is a saved boolean preference (default off, validated and applied by System 29): when on, Skip jumps to the terminal view without stopping at Detonations; when off, Skip fast-forwards to each Detonation and plays it in full. The speed setting and playback cursor are separate; opening Settings during playback pauses the clock under System 29's proposed contract, and closing the menu resumes through its 250 ms fade, without rerunning or revealing future results.
5. **Languages.** System 7 owns `vi` and `en`, the text tables, validity and fallback. Selecting an available language re-resolves visible presentation in place; keys and arguments stay unchanged and run state/RNG remain untouched. During playback this occurs while the menu has paused the clock and does not restart it. A language with an invalid/unavailable table is disabled with a reason; keep the current valid language, or follow System 7's `en` then `vi` fallback if the active language itself fails. If both fail, System 7 owns its explicit boot-error/marker path. Initial locale and change-persistence behavior on failed save must remain distinguishable.
6. **Flashes and motion.** Reduced flashes defaults on, and System 29 specifies a one-time first-launch prompt before the first fight; System 31 must supply its entry, completion and retry/focus contracts. Reduced motion and reduced flashes affect rendering, not combat facts, schedule cursor, selected speed or lock; System 29 specifies the substitutions. A conflict between its equal-duration motion fades and the Art Bible's 100 ms destination cross-fade needs owner reconciliation before implementation. Do not claim visual or photosensitivity compliance from this design alone.
7. **Input and repeat.** System 6 owns semantic actions, contexts, conflict handling, reserved controls, validation and a versioned, checksummed canonical binding blob. System 37 stores the validated blob without interpreting it. An invalid binding blob causes System 6 to use defaults and is not overwritten until an explicit edit; never leave all paths to an action unbound. Repeat settings apply only to System 6's navigation/jump behavior, never to Confirm, Select, Skip or speed keys. Delay and interval may be changed within System 6's documented ranges, or single-step may disable repeat. The chosen repeat parameters persist separately from whether repeat is currently enabled.
8. **Text size.** Standard is the MVP baseline. The Art Bible defines a separately designed Large text set; System 7 defers its pixel-budget validation to Vertical Slice and System 28 requires fallback to Standard until all its slots pass. Do not expose a global Large choice that promises every screen supports it. Revisit System 7/28/31 and screen UX when font, all-screen budgets and fallback are validated.
9. **Settings storage.** Treat settings data as distinct in ownership from System 5's run snapshot, System 24's deferred run-save and System 30's personal best; the physical record partition is undecided. Proposed contract: load the last valid supported data at startup, validate values with their owning systems before applying, and commit changes so a partial write is never presented as a successful durable save. Order or revision-guard writes so a stale completion cannot overwrite a newer choice or falsely mark it saved; implementation is blocked on the owner/architecture contract. Version/schema, storage location and crash-safe write implementation need architecture and a Unity 6.6/platform spike. Malformed or out-of-range values do not become live; preserve unsupported/newer or damaged data for diagnosis instead of silently overwriting it. Per-field versus whole-record validation/recovery remains blocked, including how to preserve an invalid binding blob when other preferences change.
10. **Immediate application and feedback.** Valid choices apply without an extra Apply button; rebind conflict resolution is an explicit exception owned by System 6. Give visible selected state and show pending or failed persistence separately from the live setting. The user may close at any time without a timed input; changes that did not save are not secretly labelled saved. No settings interaction issues a combat, reward, placement or flow command; closing the menu is a presentation action under the run owner's control.
11. **Reset boundary.** A settings reset affects only explicitly labelled preferences. It must not clear a run, a fight trace or the separate personal best. System 6 owns Reset one binding and Reset all bindings and preserves reserved access. The exact reset scope and confirmation/failure UX for all settings is open with System 31; do not implement a blanket destructive reset by inference.
12. **Effects volume and mute.** The user chose an effects-volume slider and mute button for MVP. Proposed UI: volume is an integer 0–100%, first-launch default 100% with mute off; mute is a separate on/off value and leaves the chosen slider value intact, so unmuting restores it. Moving the slider while muted changes the selected value but does not unmute; the UI indicates if persistence fails. At 0% or muted, MVP effect cues including victory/defeat stingers are silent; visible information remains available. Audio owner/System 29 must define mapping from percentage to gain and interaction with its existing cue mix; 100% is a slider value, not an assumed loudness or decibel level. A separate 'Reduced audio intensity' mode is requested but not decided. This choice does not add music or full audio System 32.

### States and Transitions

| State | Trigger | Required behavior |
|---|---|---|
| Loading | Application opens | Validate stored settings with their owners; startup failure/fallback is shown truthfully. |
| Ready | Valid live preferences | Menu shows actual selected values; storage status is independent of live state. |
| Menu / paused playback | Player opens menu, or System 29 handles focus loss | Pause playback clock and route mouse/keyboard; the user chose opening the menu on playback focus loss. Exact focus return needs System 29/31/6. |
| Valid change | Player chooses an available value | Owner applies it at its allowed boundary; persistence is attempted without claiming completion prematurely. |
| Save failed / retry | Storage reports failure | Keep accepted live choice for this process, show unsaved and permit retry; next-launch value is not promised. |
| Binding capture | Player opens rebind control | System 6 owns capture, conflicts and Cancel; no timeout. |
| Unsupported / corrupt record | A stored version or value cannot be read | Explicit fallback/diagnostic; never silently overwrite newer data. |

### Interactions with Other Systems

| System | Interface / owner |
|---|---|
| 6 Input and Focus | Validates the binding blob and repeat options, reserved actions and focus/Esc priority; 37 stores accepted preferences. |
| 7 Localization and Text | Validates language, resolves labels and enforces fallback; Standard/Large screen support remains gated. |
| 29 Payoff Presentation | Owns speed, beat-boundary application, pause clock, Skip and reduced visual treatments; 37 supplies preferences. Its GDD is a draft pending independent review. |
| 31 Menus and Screen Flow | Owns menu/prompt surfaces and focus-return integration; not yet designed. |
| 5 Run State and Flow; 24 Run Save and Resume; 30 Post-combat Breakdown | Preferences are separate from the run and personal-best data. 5 owns run transitions, 24 is deferred, 30's durable best owner is open. |
| 32 Audio; 34 Accessibility | Full audio/accessibility systems are later; audio controls and tests must match the selected MVP scope. |

## Formulas

System 37 does not change gameplay damage or tuning formulas. It passes System 6's configured navigation repeat parameters to that owner; the owner defines the event generation formula and the fixed-per-tick cap.

`repeat(t) = 1` for `t < D`; otherwise `repeat(t) = 2 + floor((t − D) / I)` for an uninterrupted held navigation key with sufficient ticks. If single-step is selected, only the initial press is generated. This is System 6's timing definition expressed with user-selected values, not a second authority. System 6's maximum one repeat per tick means the formula is an ideal schedule, not a promise that missed ticks generate multiple repeats.

| Variable | Range / unit | Owner | Meaning |
|---|---|---|---|
| `t` | nonnegative milliseconds | System 6 | Time since the navigation press. |
| `D` | 200–800 ms, default 400 | System 6 | Delay before first repeat. |
| `I` | 50–250 ms, default 125 | System 6 | Interval between scheduled repeats. |
| `repeat(t)` | positive integer | System 6 | Ideal cumulative press/repeat count, subject to the per-tick cap. |

**Example:** with defaults and ticks at 0, 400, 525 and 650 ms, a held navigation key generates four events by 700 ms. This is not a deadline or a rule for Confirm. System 29, not Settings, owns speed scheduling, Detonation timing and the 250 ms resume fade.

## Edge Cases

- **First launch / no settings:** use each owner's known default; initial language and Reduced motion defaults remain unresolved. Present the Reduced flashes prompt before the first fight; handling a prompt that could not be durably marked as completed is a System 31/37 contract.
- **Invalid or newer settings version:** do not partly apply an unsupported record or overwrite it merely by opening Settings; show a recoverable fallback and retain evidence. Granular validation/recovery and explicit reset are open.
- **Failed write or interruption:** while the process remains alive, retain the validated live choice, show unsaved and allow retry. After a crash, recover only a complete valid stored record, not an alleged successful save. Exact crash-safe mechanism and backup recovery require an architecture spike.
- **Out-of-order writes:** if choices A then B produce delayed completions, A must not overwrite B or make B appear saved. The revision/ordering implementation is an open architecture contract.
- **Bad input bindings:** System 6 falls back to default bindings without overwriting the blob until the player edits. Changes to other preferences must not silently destroy that blob; the storage partition/recovery contract is unresolved.
- **Missing language tables:** System 7 disables the unavailable option and preserves a working choice; if no working table exists, use its defined explicit marker/boot-error path.
- **Speed changed mid-beat:** the next beat uses the chosen speed; the current beat's progress and authored Detonation peak follow System 29. A subsequent Skip is not persisted as a speed.
- **Menu opens during playback or focus is lost:** pause according to System 29 and clear pressed keys/buttons/repeats via System 6. The user chose to open the menu when the playback window loses focus. For an item held during arrangement, System 6 returns a drag-origin item on `InputReset` but retains a click-origin hold. No background catch-up, result reveal, trace mutation or unexpected reward selection may occur.
- **Muted with a slider change:** keep mute enabled and remember the new valid slider value; unmute uses that value. At 0% unmuting still produces silence.
- **Text/motion preferences changed with a popup or focused item open:** re-resolve presentation without inventing gameplay input; exact focus and held-item handling require Systems 6/25/26/31 before implementation.
- **Large text is not validated on all required screens:** use Standard; do not offer global Large as fully supported. The screen-specific fallback is transparent and belongs to the UX contract.
- **Reduced flashes prompt cannot persist its completion marker:** do not assume it was recorded. The prompt retry and suppression policy must be defined by System 31/37, with Reduced flashes default remaining on.
- **Personal best or run data exist:** resetting preferences or invalidating a settings record must not erase those separately owned values.

## Dependencies

| System | Direction and contract | Status |
|---|---|---|
| 6 Input and Focus | Supplies binding serialization/validation, focus, conflict rules and repeat parameters; 37 stores. | Designed, pending independent review. |
| 7 Localization and Text | Supplies `vi`/`en`, text resolution, fallback and text-set validation. | Designed, pending independent review. |
| 29 Payoff Presentation | Consumes speed and reduced motion/flashes; owns playback timing and pause. | Draft pending gate/review; owner integration required. |
| 31 Menus and Screen Flow | Supplies settings entry, prompt, close/return and save-error surface. | Not Started; hard UX handoff. |
| 5 Run State and Flow; 24 Run Save and Resume; 30 Post-combat Breakdown | Run state and the personal best remain separate; none may be mutated by preferences. | 24 deferred; 30 personal-best storage owner unresolved. |
| 32 Audio; 34 Accessibility | Own later full audio/accessibility; MVP reduced-effects and audio controls must have actual evidence. | Later, Not Started. |
| Unity 6000.6.3f1 / architecture | Storage location, durability, UI stack, APIs and lifecycle behavior need verification. | No `modules/core.md` engine reference; 6.3 module details are not 6.6 evidence. |

## Tuning Knobs

Navigation repeat is owned by System 6: delay 400 ms, safe range 200–800; interval 125 ms, safe range 50–250, with single-step as an option. These are player preferences, not a new balance formula. System 29 owns the 1×/2×/4× set, mid-beat handoff and resume fade. Default language, Reduced motion default, save retries and UI dimensions are not fixed by this GDD; do not tune a safety or accessibility claim without evidence. System 7's Large budgets and font are pending.

## Visual/Audio Requirements

- Follow the Art Bible's calm Ink surfaces, legible text, clear focus rings and word/shape plus colour; do not add bright flashing confirmation effects.
- Reduced-flash and reduced-motion options must preserve every actor, cause and outcome through visible alternatives; System 29 owns the exact effects and their unresolved timing conflict with Art Bible 7.13.
- Settings labels and errors are localized by System 7; available states, value and unsaved status are readable without relying on colour, audio or motion.
- Font, contrast, actual captured visuals and photosensitivity checks are **NOT ASSESSED**. MVP effects volume and mute apply to the placeholder cue layer; gain mapping and real audio levels still need verification.

## Game Feel

Changing a preference feels immediate and reversible without changing the fight or forcing the player to race a timeout. A failed save is easy to notice while the chosen live value remains usable in the current process. Pausing and returning keeps the player's place in the presentation, subject to System 31/6's unimplemented focus contract.

## UI Requirements

- Show the actual speed choice, language, reduced-flash and motion state, effects-volume slider and mute state, input repeat settings and binding controls if the rebind screen is retained. Clarify the difference between a preference and the one-time Skip action.
- Provide mouse-only and keyboard-only paths, non-colour selected indicators, visible focus, no timed input, an explanation for disabled language choices and a readable save-failure/retry state. Follow System 6's Esc and rebind-capture priorities.
- The first-launch Reduced flashes prompt must precede the first fight; its exact wording, input focus, and successfully stored completion marker require System 31/7/37 UX.
- `/ux-design` and `/ux-review` must specify and validate menu entry, focus return, binding conflict, save failure, language reflow, screen coexistence and both languages before UI stories. No screenshots or interaction evidence currently exist.

## Cross-References

| Document | Relationship |
|---|---|
| [Game Concept](game-concept.md), [Systems Index](systems-index.md), [Art Bible](../art/art-bible.md) | Pillars, scope, visibility, reduced effects and text set. |
| [Input and Focus](input-and-focus.md) | Bindings, reset, repeat, reserved controls, focus and menu input. |
| [Localization and Text](localization-and-text.md) | Languages, text rendering, fallback and Large validation. |
| [Payoff Presentation](payoff-presentation.md) | Playback, speed, pause, Detonation and reduced effects. |
| [Run State and Flow](run-state-and-flow.md), [Post-combat Breakdown](post-combat-breakdown.md) | Run state and separate persistent best are outside settings. |
| [Accessibility Requirements](../accessibility-requirements.md) | Input waiver and existing project accessibility commitments. |

## Acceptance Criteria

These are design requirements, not tests performed. **BLOCKED** requires an owner contract or engine spike; **NOT ASSESSED** requires actual UI, audio or accessibility evidence.

1. **GIVEN** no saved settings, **WHEN** first-launch defaults are applied, **THEN** speed is 1×, Reduced flashes is on, Standard text and default bindings are used, and repeat delay/interval are 400/125 ms. Initial language and Reduced motion default remain **BLOCKED** pending a decision.
2. **BLOCKED — prompt. GIVEN** a first launch before the first fight, **WHEN** System 31 opens the Reduced flashes prompt, **THEN** the on-by-default choice is displayed and remains usable without timed input. Once completion is durably stored the prompt is not repeated; retry after a failed marker write needs the System 31/37 contract.
3. **GIVEN** 1× playback mid-beat, **WHEN** 4× is selected from the action strip, **THEN** the current beat retains its progress, the next beat uses 4×, the Detonation peak follows System 29's authored timing, and the selected speed enters the same settings persistence path as a menu choice.
4. **GIVEN** 2× is selected, **WHEN** the player presses Skip and later starts playback again, **THEN** Skip is not saved as a speed and the most recently accepted speed remains selected; normal and Skip consume the same completed fight facts.
5. **GIVEN** playback is paused by opening Settings, **WHEN** the player changes Vietnamese to English and closes the menu, **THEN** visible labels re-resolve with the same parameters without advancing or restarting the cursor while paused; resume follows System 29's 250 ms fade. Trace, RNG, fight result, reward and run snapshot remain unchanged.
6. **GIVEN** the Vietnamese table is unavailable while English works, **WHEN** language options are shown, **THEN** Vietnamese cannot be selected and English remains active, with a visible reason. If both fail, System 7's explicit boot-error/marker behavior applies rather than an invented translation.
7. **BLOCKED — reduced-effects timing. GIVEN** any combination of Reduced flashes and Reduced motion, **WHEN** a recorded effect is presented, **THEN** actor, cause, damage and outcome remain readable and the combat result is identical. Systems 29 and art/UX must reconcile fade timing before exact playback integration is assessed.
8. **GIVEN** volume 60% and mute off, **WHEN** mute is turned on, the slider changed to 30% and mute turned off, **THEN** the live selected value is 30%, mute does not clear it, and the unmuted mix uses 30%. At 0% the mix is silent even with mute off. Actual gain mapping and measured audio are **BLOCKED/NOT ASSESSED**.
9. **GIVEN** a valid System 6 binding change, **WHEN** System 6 accepts it, **THEN** System 37 stores the opaque validated canonical blob and prompts show the live binding. A reserved key, unresolved conflict or attempt to remove the final accessible binding is rejected by System 6 without changing the valid map.
10. **GIVEN** a corrupt, invalid or newer-version binding blob, **WHEN** System 6 loads it, **THEN** defaults remain reachable and the invalid blob is not overwritten until an explicit player edit; capture has no timeout and Esc cancels it.
11. **GIVEN** repeat delay 400 ms, interval 125 ms and ticks at 0/400/525/650 ms, **WHEN** a navigation key is held through 700 ms, **THEN** exactly four events occur. Delay 200 and 800 ms and interval 50 and 250 ms are accepted boundaries; outside values are rejected by the owner, not silently used.
12. **GIVEN** single-step mode, **WHEN** a navigation key is held, **THEN** only its initial press is generated. Confirm, Select, Skip and speed keys never auto-repeat; a delayed tick emits at most one repeat and InputReset clears held repeats.
13. **BLOCKED — persistence. GIVEN** a valid settings edit and a successful durable save, **WHEN** the game exits and is reopened, **THEN** the accepted preference returns. The platform-specific save, flush/recovery and startup contract must first be verified for the pinned Unity version and target platform.
14. **GIVEN** a save fails while the process remains alive, **WHEN** a valid preference was selected, **THEN** the live value remains active, the UI states it was not saved and offers retry; success is shown only when the corresponding revision actually persists.
15. **BLOCKED — save ordering. GIVEN** edits A then B and completions arrive B then A, **WHEN** they settle, **THEN** A cannot overwrite B or falsely mark B as saved. The owner must specify revision/ordering and crash behavior.
16. **BLOCKED — crash/corruption. GIVEN** a previously complete valid record, **WHEN** a write is interrupted at each storage phase or a newer/corrupt record is loaded, **THEN** the game never presents a partial record as a successful save or silently overwrites unsupported data; only an owner-validated complete record or an explicit fallback may be used. Exact recovery and preservation require an architecture spike.
17. **GIVEN** preferences are reset or a preference record is invalid, **WHEN** the user returns to the run or summary, **THEN** separate run data, fight trace and personal best are unchanged. Binding reset obeys System 6's reserved and accessible controls; global reset scope remains **BLOCKED**.
18. **GIVEN** playback is running and the window deactivates, **WHEN** System 6 delivers InputReset, **THEN** the playback clock stops, the menu opens as the user chose, pressed keys/buttons and repeats clear and no catch-up, combat command or reward action is emitted. During arrangement a drag-origin hold returns its item, whereas a click-origin hold remains held. Exact focus restoration remains a Systems 6/29/31 UX contract.
19. **GIVEN** Large text has not passed System 7's all-required-screen font and budget checks, **WHEN** Settings is opened, **THEN** it does not promise a globally supported Large setting; System 28's Standard fallback remains in effect.
20. **NOT ASSESSED — UX evidence. GIVEN** Vietnamese and English settings, sliders, mute, a missing language, a failed save, rebind conflict and paused playback, **WHEN** actual mouse-only and keyboard-only interaction and captures are reviewed, **THEN** the controls, selected values, errors, focus return and longest text are readable without colour or sound alone. Retain the evidence; no pass is claimed yet.
21. **NOT ASSESSED — payoff/audio evidence. GIVEN** reduced effects on and off plus mute and volume extremes, **WHEN** representative fights are captured and reviewed, **THEN** visible causes and outcomes remain identifiable and measured audio follows the agreed mapping. Photosensitivity, loudness and usability are not proven by this GDD.

## Open Questions

| Question | Owner | Resolve by |
|---|---|---|
| What are first-launch language and Reduced motion defaults, and how are they communicated? | Systems 7/29/31/37, UX | Before first-launch implementation. |
| What storage API, location, versioning, validation, revision ordering, crash recovery and migration work on the pinned Unity and Windows target? | Technical director, System 37 owner | Architecture and verified storage spike before implementation. |
| On corrupt or newer settings, is recovery whole-record or per field, and how does an explicit reset preserve evidence and unrelated data? | Systems 6/31/37, technical director | Before persistence and reset implementation. |
| Who supplies the first-launch prompt, completion marker, retry path and focus, especially when persistence fails? | Systems 29/31/37, UX | Before first-fight flow implementation. |
| The user chose that playback window deactivation opens the pause menu; how is focus restored on return, without catch-up or unintended input? | Systems 6/29/31, UX | Before pause UI implementation. |
| How are menu access, held item/popup handling, settings closure and focus return defined at each run phase? | Systems 5/6/25/26/31, UX | Before screen UX approval. |
| Which fade timing reconciles Art Bible 7.13's 100 ms travel replacement with System 29's equal-duration reduced-motion fade? | System 29, art director, UX | Before reduced-motion implementation. |
| What mapping turns the selected 0–100% effects volume into gain for System 29's cues, and should a separate Reduced audio intensity mode ship? | Audio director, Systems 29/32/37 | Before audio implementation and mix validation. |
| When do all required screens and the actual font pass Large text budgets so a global option can be offered? | Systems 7/28/31/34, UX/art | Vertical Slice font and screen validation. |
| Who stores the personal best selected for System 30? A settings reset must not clear it regardless of storage owner. | Systems 30/31/37, architecture | Before personal-best persistence implementation. |
