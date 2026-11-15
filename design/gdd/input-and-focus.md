# Input and Focus

> **Status**: Designed (CD-GDD-ALIGN CONCERNS, concerns applied; independent `/design-review` not yet run)
> **Author**: user + Claude design session, with specialist consultation
> **Last Updated**: 2026-11-13
> **Last Verified**: 2026-11-13
> **Implements Pillar**: Arrangement Is the Thinking (primary: arrangement is made by hand, so input must be exact, forgiving and readable); Earned Rarity (no input changes the fight once it starts)

## Summary

Input and Focus turns what a mouse or keyboard does into device-independent actions, decides what each action means in the current state of the run, and owns the focus (what is selected, in what order it moves, where it returns after a dialog, and how mouse hover and keyboard focus share one reading). It also owns the held item (a click-and-hold and a drag are the same state), the guards that keep a slip recoverable until Start and keep any input from changing a fight once it has started, and the binding map (defaults, conflict and reserved-key rules, a canonical text form that Settings stores). The MVP is mouse and keyboard in full, with every action remappable (once the rebind screen ships) and nothing timed; the gamepad is a designed contract only. The decision layer is engine-free and testable without a window; a thin Unity layer only translates devices.

> **Quick reference** — Layer: `Foundation` · Priority: `MVP` · Key deps: `None`

## Overview

System 6 is how the player's hands meet the game. It turns what a device does (a mouse move, a click, a key) into **semantic actions** that do not name a device (pick, place, rotate, cancel, confirm, start, next zone), decides what each action means in the current state of the run, and owns the **focus**: which thing on screen is selected for the keyboard, in what order focus moves, where it returns after a dialog, and how the mouse's hover and the keyboard's focus share one reading. It also owns the **held item** (a click-and-hold and a drag are the same held state), the guards that keep a slip recoverable until Start (and keep any input from changing a fight once it has started), and the **binding map** (the default keys, the rules for conflicts and reserved keys, and a canonical text form that System 37 stores). The MVP is mouse and keyboard in full: every action can be done with either, every action can be rebound once the rebind screen ships (the screen is built last and is the first thing cut, with a recorded waiver), and nothing is timed. The gamepad is a designed contract only: the action names and the focus graph assume no pointer, so a pad can be added in the Vertical Slice without redoing them. The whole decision layer is engine-free and testable without a window; a thin Unity layer translates devices into semantic events and nothing else. Without this system every screen would invent its own keys and its own focus; with it, the arrangement is the only thing that is a bet, never the player's dexterity (Pillar 4).

> *Specialists consulted for this section: game-designer, ux-designer, accessibility-specialist, gameplay-programmer, engine-programmer, creative-director (2026-11-13). Their findings are applied below.*

## Player Fantasy

The player moves an item across a grid, turns it, puts it down, picks it up again, and nothing in the way their hands work gets between them and the thinking. They can do it all with the mouse, or all with the keyboard, calmly; a slip costs nothing until they press Start, and then the fight is the fight. **Your hands are never the bet. A slip costs nothing until you press Start, and everything works calmly with a mouse or a keyboard.**

*Pillar anchors.* Pillar 4, Arrangement Is the Thinking: all of the player's skill happens in the arrangement, so input must be exact, forgiving and readable, and what the player needs to decide must be reachable and visible by hand. Pillar 3 and System 5: no input changes the outcome once Start is accepted; the only live inputs during playback are playback speed, Skip, Pause and abandoning the run. The anti-pillar "NOT active in-combat actions" is enforced here by construction.

*Guardrails* (binding rules): no timed input anywhere; no hold-only action (picking and placing work by click, click or by drag, and the overview is a toggle); every action works with mouse alone and with keyboard alone; every action can be rebound (mouse buttons are fixed in the MVP, and cutting the rebind screen triggers the recorded waiver for single-key shortcuts); a slip is recoverable until Start (Esc cancels a held item back to where it came from, a drag released off every grid returns the item and never destroys it, Undo covers every grid command); confirmation is asked only for choices that cannot be undone (a door, taking an item, the buff hero, the party, Start with a non-empty tray, abandoning the run), never for grid edits.

*Risks the design guards against:* drag precision (snap to the nearest legal anchor and show the footprint before release); a click that lands on a new button after the screen changed (the input lock); keyboard play slower than mouse play (a cell cursor with jumps; the target is within about 1.5 times the mouse's action count); a Start pressed by accident; a rebind that leaves an action unreachable.

> *`creative-director` consulted (2026-11-13); the spine of the fantasy is theirs.*

## Detailed Design

### Core Rules

1. **Layers.** (a) A **device layer** in the Unity assembly reads mouse and keyboard through the Input System and emits `InputEvent`s (an action, a phase of pressed or released, an integer pointer position in native pixels, the device family). (b) A **pure core** (engine-free, no devices, no clock beyond a timestamp passed in) holds the `InputAction` enum, the `FocusModel`, the held-item state, the `BindingMap` and the `ActionMapper`, which maps `(run phase, focus model, held state, event)` to an **intent**: a flow command for System 5 (stamped with the serial of the **view the target was rendered from**, a `viewGeneration`, and not the serial current at dispatch, so a second click cannot pick up the new serial and defeat System 5's stale rejection), a grid command for System 8, or a pure UI intent (move focus, open a dialog). (c) The UI toolkit (UGUI or UI Toolkit, undecided) is used only for hit-testing: a pointer entering or leaving a target becomes `SetHover(id)` and nothing more. The core never sees a device, so a pad can be added without touching it.
2. **The actions and their MVP defaults** (names are device-independent; the bindings are defaults and can be rebound; where two are given the first is the primary):

   | Context | Action | Mouse | Keyboard |
   |---|---|---|---|
   | Anywhere | Select (arm, toggle) | click | Space |
   | Anywhere | Confirm | click the button | Enter, only on an armed item or a focused button |
   | Anywhere | Cancel | a drag released off every grid, or a click on the held item's source (see Rule 4) | Esc |
   | Anywhere | NextZone, PrevZone | none | Tab, Shift+Tab; plain alternatives PageDown, PageUp |
   | Anywhere | Navigate | hover | arrows; Home and End go to the first and last target of the zone |
   | Anywhere | Details (opens and pages the popup; a second press or Esc closes it) | right click when nothing is held (a trackpad-friendly path; the middle click is a second binding) | F |
   | Planning | Pick, Place | click (press and release without moving) | Space |
   | Planning | Drag | press, move, release | none (Pick and Place reach the same held state) |
| Planning | JumpNext, JumpPrev (nearest item, or nearest legal placement while holding) | none | Shift+arrow; plain alternatives N and B |
   | Planning, holding | RotateCW | right click, or wheel up | R |
   | Planning, holding | RotateCCW | wheel down | Shift+R; plain alternative T |
   | Planning | Undo | the Undo control | Z, Ctrl+Z |
   | Planning | SendToStash, RemoveToTray | the controls | X, Delete |
   | Planning | SwitchHero | click the pedestal | 1, 2, 3 (the join numerals), Q and E for previous and next |
   | Planning | ToggleOverview, IntentTabNext | click the tab | O; comma and period |
   | Planning | **Start** | click the Start button | Ctrl+Enter, or Confirm while Start is focused (no bare key) |
   | Door choice | Arm a card | click the card | arrows, then Select |
   | Door choice, buff, shelf | Confirm | the Confirm button | Enter |
   | Reward shelf | SkipOffer (arms; Confirm commits) | the labelled Skip button | K (Esc never skips; K here is not the playback Skip, the contexts are disjoint) |
   | Hero pick | Toggle a hero, Confirm | click, the Confirm button | Select, Enter |
   | Playback | Speed 1x, 2x, 4x | the speed segments | 1, 2, 3 (scoped to playback) |
   | Playback | Skip (arms a latched Skip) | the Skip button | K (scoped to playback; Space activates the focused control and never skips); keys 1, 2, 3 and the speed segments cancel an armed Skip |
   | Menu | Pause or menu | the Settings button | Esc as the last fallback in the priority of Rule 10 |
   | Menu | AbandonRun | menu, then Abandon | no hotkey |

   Door cards are never bound to 1, 2, 3 (the workbench stays live in the door state, where those keys switch the hero). The hero keys stay live while an item is held.
3. **The held item.** A press that is released within `DragThreshold` of where it started and over the same cell is a **click-hold**: the item is picked and stays held until a second click places it. A press that moves farther is a **drag** and commits on release. Both create the same held state, with the item following the pointer or the cursor lifted by 2 px with its shadow and ring (art bible 7.11), and the source showing an empty bed. A place on an invalid target is refused in place with no penalty; a click-to-place outside every grid does nothing and the item stays held; a drag released outside every grid returns the item to where it came from (never to the tray, never destroyed). Hero switching while holding keeps the plate in hand. Undo is disabled while an item is held. A second pick while holding maps to nothing (the grid has no swap). A pick from the tray starts the ghost at the first legal placement found by a pure loop over `Preview` in `AutoPlace`'s search order (never by calling `AutoPlace`, which mutates the model and clears Undo), so the common case is two presses (Select, Select). A drop snaps to the nearest legal anchor within `SnapRadius` cells (default 1) of the pointer, keeping the grab offset the pointer had on the plate at pick; if nothing legal lies within the radius the drop is refused in place. The wheel rotates one quarter turn per 120 units, carrying the remainder, so a trackpad swipe cannot spin the item. Only an unchanged origin and orientation counts as returning the item by clicking its source; otherwise the click is a `Place`.
4. **Cancel.** While an item is held, the right click rotates (art bible and Grid Backpack), so **Esc** is the keyboard cancel and a drag released off every grid is the mouse cancel; an unchanged click on the held item's source also returns it. With nothing held, the right click is Details (Rule 2), so the mouse has a path to every action. The priority of Esc is: cancel a rebind capture, then close a dialog (a dialog traps input), then close the popup, then cancel a held item, then disarm an armed card, then close the menu, then open the menu (Pause). Rotation is clockwise quarter turns (System 8); counter-clockwise is three of them.
5. **The keyboard cell cursor.** In a grid, the arrows move a cell cursor; leaving a placed item takes one press, not one per cell it covers (an item is one stop). A jump (Shift and an arrow, or N and B) goes to the nearest item by Manhattan distance between anchor cells in that arrow's direction, ties by `(anchor.y, anchor.x)` and then the item ID; while an item is held it goes to the nearest legal placement by the Manhattan distance of the origin from the cursor, ties by `(y, x, orientation)` (a loop over System 8's `Preview`, at most 6 × 6 × 4 candidates). An arrow past a zone's edge continues into the adjacent zone only where the layout is truly adjacent (the grid to the stash and tray vertically; the workbench, the party and the right column horizontally). The cursor remembers its cell per zone; switching hero keeps the cursor's column and row or moves to the nearest open cell.
6. **Select, then confirm, only for the irreversible.** Select arms, Confirm commits, and Space never confirms. This applies to the door, taking an item, **skipping an offer** (the skip is arming, and Confirm commits it, so the K that skips playback cannot forfeit an offer), the buff hero, the party and abandoning the run. **Start is one deliberate action** (the button or Ctrl+Enter), disabled while an item is held, while a reward step is pending and during playback; if the tray is not empty the "Leave behind: X, Y?" dialog opens with "Go back" focused. Grid edits never need a confirm, because Undo and the held state cover them. The default of BuffClaim is pre-armed on the suggested hero, so the usual path is one Confirm.
7. **Guards (all in the `ActionMapper`).** (a) During playback only Speed, Skip, Pause (it opens the menu; whether the playback clock stops is Systems 29 and 31's), `AbandonRun` (after its confirmation), Navigate, and read-only Details on the intent tabs (with Cancel closing it) are live; every other action maps to nothing, and Skip changes the presentation only (it never delivers an outcome; it arms a latched Skip, and keys 1, 2, 3 or a speed segment cancel it). (b) While the grid is Frozen, grid commands are gated for feedback, and the model still returns `Frozen` as the authority. (c) While an item is held, `StartFight`, `ChooseDoor`, `ClaimBuff`, `TakeOffer`, `SkipOffer` and Undo are blocked. (d) Confirm and Select ignore key auto-repeat, so a held Enter cannot ripple through auto-skipped reward steps. (e) **Input lock:** after a run-phase change or the opening of a screen with a different anchor (not after every grid command), every commit-class action (Confirm, Select, Start, Skip, SkipOffer, Restart and the buttons of a dialog) is ignored for `InputLockMs`, judged at the press (a press during the lock never fires when it expires, and its repeats and release are ignored too; Navigate and Details stay live) (250 ms by default; a minimum delay, not a deadline on the player), events stamped at or before a dialog's opening time are swallowed, and the press that activated Start is consumed until its release, so it cannot become an instant Skip. Esc is never locked. (f) A command queued across a state change is dropped, not re-queued (it would be `Rejected(Stale)` anyway). (g) The mouse wheel rotates only while an item is held (it scrolls only a modal, a paged popup or a list). (h) A press that an action consumes (Start, an Esc that cancels mid-drag, the right button when it rotates) swallows its matching release. (i) On window deactivation, capture loss or an OS dialog the device layer sends `InputReset`: every pressed key and button and every repeat is cleared, a Drag-origin hold is treated as released off every grid (the item returns) and a Click-origin hold stays held; a button the core believes down but the device reports up gets a `synthetic` release that never commits and returns the item. A pointer that leaves the canvas while dragging has no target, the ghost is clamped to the canvas edge, and the release returns the item. (j) When the view changes without an input event (the end of the door reveal, the end of playback) the Unity layer sends `ViewChanged(t)`, and the lock starts at that time; a view replacement (Stopped, Ended, Abandoned, a restore) clears the held state with no command.
8. **Focus model.** Focus has two levels: **zones** (Tab and Shift+Tab, wrapping) and **targets** inside a zone (arrows, never wrapping, clamped at the edges). A plate's whole footprint is one target; open empty cells and locked cells are targets (a locked cell explains "opens with a door"); pedestals, intent tabs, order chips, cards, wares, buttons, tabs and segments are targets; HP numerals, bridges and decoration are not. The Planning zone order is: the grid; the lower section (the stash or overview tab, the stash, the tray chip and list, the send-to-stash, remove and undo controls; entering it with Tab lands on the first tray item when the tray is not empty, otherwise on the stash or overview tab); the party pedestals; the order strip; the right column (the intent tabs, or the room objects left to right); the action strip (Start or Confirm); the Settings button. While an item is held, Tab visits only the drop zones and the hero switch, and Start stays focusable but disabled with a reason (Esc cancels; there is no Cancel control).
9. **Focus by state.** Hero pick: the cards in a row, Confirm last and disabled until three are picked; the first card is focused, and after Restart Confirm is focused with the previous party pre-picked. BuffClaim: the suggested pedestal is pre-armed and Confirm is focused; focus on a pedestal arms it (a single-select group; hover never arms, a click does), and arming also switches the workbench to that hero so the player sees its cells; from Confirm, Left or Up returns focus to the armed pedestal and changes nothing; keys 1 to 3 switch the workbench view only; Esc never disarms; there is no skip (System 28). OfferPending: the emblem (read-only), the cards, Skip, Confirm; the first card is focused, never Skip. DoorChoice: the cards left to right, then Confirm; focus is assigned when the reveal unlocks input. Playback: only the speed segments, the read-only intent tabs and the menu are focusable, and the current speed is focused. End of run: Restart is focused, then the menu, the seed copy control and the room rows as one scrollable stop. After any state change exactly one target is focused (a pending reward overrides the grid's memory); after a dialog closes, focus returns to the opener, falling back to the nearest target in the same zone, then the zone's first target, then the state's anchor; after a place or an undo, focus lands on the placed or restored plate.
10. **Hover and focus share one reading.** Two variables decide what is shown: `Mode` (Pointer or Nav) and `PromptDevice`. A pointer move of more than `PointerMoveThreshold` native pixels sets Pointer mode: hover wins, hover rings and popups are live and the focus ring and prompt chips are hidden (the focus is remembered, and a click sets it). Any arrow, Home, End, Tab, PageUp, PageDown, a jump, Select or Enter sets Nav mode (Esc, R, Z and the other keys do not change the mode): focus wins and hover is suppressed until the pointer moves again; **the first navigation key after Pointer mode only reveals the focus ring at the remembered target** (it neither moves nor acts; the first remembered target of a screen is its anchor). A pointer press sets Pointer mode regardless of movement. Dialogs always show the focus ring. Hover and focus show identical content with only the ring differing (1 px for hover, 2 px lifted with a 1 px gap for focus); focus opens the popup at once (System 26). Arrows never scroll a Details popup (they move focus); the Details key, the wheel and the paging controls page it. Popup priority is pinned, then armed, then focus or hover; focusing a sibling card temporarily replaces the armed card's popup, which returns when focus leaves the siblings. Key repeat swaps content without re-fading, and repeated bridge and partner highlights are debounced against the flash limit (art bible 4.12).
11. **Disabled but focusable.** A disabled control keeps its ring and stays in the Tab order; its popup is a chip with the reason from the awaited step (`WrongState`'s awaited step, for example "Choose a buff hero first"); activating it shows the reason at once with no delay and a rejection cue.
12. **Modals.** The tray-loss dialog and the abandon dialog trap focus and swallow input on their opening frame; Tab cycles inside; the default focus is the non-destructive button ("Go back", "Keep playing") and Esc equals it; the destructive button needs an explicit move and Enter. A tray change while the tray dialog is open cancels and reopens it. The abandon path is the menu, then Abandon, and it works during playback.
13. **The binding map.** Each action has a primary and a secondary binding per scheme, scoped by **context** (Arrangement, HeldItem, Modal, Menu, Playback); the defaults are immutable and the player's overrides are a layer on top. A conflict is two actions in the same or an overlapping context on the same control; it is rejected with a report naming the other action, and the UI offers a swap (never silent). Reserved and non-rebindable: Esc (cancel and pause), the arrows and Tab for focus, Enter for Confirm, and the left click as select; every action keeps at least one binding on a shipped scheme (mouse and keyboard in the MVP; the pad has none), and an action that is natural to one scheme only (Drag on the mouse, the cell cursor on the keyboard) keeps a binding on the other scheme that reaches the same result (Pick and Place); Cancel always keeps Esc. The binding contexts map to the run's states as follows: Arrangement covers the hero pick, planning, door choice, buff claim and reward shelf screens; HeldItem is entered by a pick; Modal by a dialog or a rebind capture; Menu by the menu; Playback by the fight's playback. The contexts overlap as follows: HeldItem is a sub-state of Arrangement and overlaps it; Modal overlaps nothing (it swallows everything else); Menu and Playback overlap nothing. Bindings are by physical key position (so QWERTZ and AZERTY work) and the prompt chips are labelled from the live binding and the active layout, falling back to text for an unnamed key. The map has a canonical text form (a version line, an explicit order, invariant decimals, an FNV-1a 64 checksum line) that System 37 stores as an opaque blob; a bad blob falls back to the defaults and never throws. A rebind capture has no timeout and Esc cancels it. Mouse buttons are fixed in the MVP.
14. **The gamepad is a contract, not a feature.** The `InputAction` enum contains the navigation actions, the focus graph assumes no pointer, `PromptDevice` can be Pad, and the cell cursor is specified for keyboard and pad alike. No pad binding ships in the MVP. This is a recorded exception to the project rule that every interactive element supports keyboard, mouse and gamepad; it expires at the Vertical Slice exit gate and System 34 owns it. The waiver is recorded in `design/accessibility-requirements.md` (not yet created).
15. **Determinism and engine-freedom.** The core reads no clock (timestamps come in with the events), holds no mutable static state (Unity 6.6 skips the domain reload, and the project has it switched off), uses integer arithmetic only and sorts by unique ordinal keys. The Unity layer pairs every `Enable` with a `Disable` and `Dispose`, subscribes in `OnEnable` and unsubscribes in `OnDisable`, uses no anonymous lambdas for callbacks it must remove, and keeps no static input events.
16. **Accessibility commitments (MVP, mouse and keyboard, tier Standard).** Nothing is timed; nothing is hold-only, and every action has a chord-free path (the defaults Ctrl+Enter, Ctrl+Z, Shift+Tab, Shift+R and Shift+arrow are shortcuts with plain alternatives); the held state is reachable by click, click, by drag and by the keyboard cursor; every action can be done one-handed with the mouse alone or with the keyboard alone; the focus ring is visible at all times and never lost; a configurable key-repeat delay and rate (and a single-step option) apply to cursor and focus movement; rotate has a mouse alternative that does not depend on a right button (the wheel, and an on-screen control); a hover popup has a keyboard trigger and is dismissible; non-colour cues (ring width, corner square, cross on blocked cells) carry every state.

### States and Transitions

| Subject | State | Trigger | Result |
|---|---|---|---|
| Held item | None | Pick (click or drag start) | Holding(source, orientation, origin Click or Drag). |
| Held item | Holding | Place on a legal target, or Cancel, or a drag released off every grid | None; a legal place commits, the others return the item. |
| Mode | Nav | A pointer move beyond the threshold | Pointer. |
| Mode | Pointer | An arrow, Tab or Select | Nav (the first such key only reveals the ring). |
| Modal scope | Closed | `NeedsConfirm` or the menu's Abandon | Open (focus trapped, default on the safe button). |
| Modal scope | Open | Confirm, Go back or Esc | Closed (focus returns to the opener). |
| Input lock | Open | A state change that replaces focus targets | Locked for `InputLockMs`, then open. |

### Interactions with Other Systems

| System | Data in | Data out | Interface owner |
|---|---|---|---|
| 5 Run State and Flow | Run phase, awaited step, legal commands | Flow commands (stamped with the serial of the view the target was rendered from) | System 5 owns commands; System 6 never invents one. |
| 8 Grid Backpack | `Preview`, results (`Frozen`, `NoEffect`, `Pinned`), `AutoPlace` candidate | Grid commands | System 8 owns the operations. |
| 25 Backpack UI, 26 Hover Popup, 27 Combat HUD, 28 Door and Room Panels, 31 Menus | Targets, focus zones, popups | Semantic actions, focus, hover/focus mode, Details, Cancel, speed and Skip bindings | Those systems draw and register their targets. |
| 37 Settings | The stored binding blob, key-repeat settings | The canonical binding text | System 37 stores; System 6 parses and validates. |
| 7 Localization | Key labels | None | System 7. |
| 34 Accessibility | Later additions | The contract and the waiver | Vertical Slice. |
| 29 Payoff Presentation | The playback gate | Speed and Skip | System 29 owns pacing. |

*Provisional:* the default keys, the thresholds and the assembly name (NOT SOURCEABLE until the architecture ADR) are proposals; the UI toolkit choice is open.

> *Specialists consulted: game-designer, ux-designer, accessibility-specialist, gameplay-programmer, engine-programmer (2026-11-13).*

## Formulas

All arithmetic is on integers; times are integer milliseconds passed in with the events (the core reads no clock); positions are native pixels (the 640 × 360 art canvas). All thresholds are provisional.

**Formula 1: pointer to native pixel.** `scale = max(1, min(⌊W / 640⌋, ⌊H / 360⌋))`, `offset = ((W − 640 × scale) / 2, (H − 360 × scale) / 2)` (integer division), `native = ⌊(mouse − offset) / scale⌋` (with the pointer's y flipped from Unity's bottom-left origin to the top-left origin of the canvas: `mouse.y' = H − 1 − mouse.y`); the pointer is over the canvas iff `0 ≤ native.x < 640` and `0 ≤ native.y < 360`, otherwise it is outside (no hit). The division is **floor division** (toward negative infinity), so a pointer left of the canvas is negative and outside; truncating division would read `−1 / 2 = 0` and falsely report a hit. A window smaller than 640 × 360 is not supported (the minimum window size is enforced by the window; `max(1, …)` is only defensive).

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| `W`, `H` | int | `≥ 640`, `≥ 360` | window | Screen size in pixels. |
| `scale` | int | `≥ 1` | calculated | Integer scale of the canvas. |
| `mouse` | int pair | `0..W−1`, `0..H−1` | device layer | Pointer position in screen pixels, top-left origin. |

*Examples:* `W = 1920, H = 1080` gives `scale = 3`, `offset = (0, 0)`, and the mouse at `(960, 540)` is native `(320, 180)`. `W = 1366, H = 768` gives `scale = 2`, `offset = (43, 24)`, and the mouse at `(53, 30)` is native `(⌊10 / 2⌋, ⌊6 / 2⌋) = (5, 3)`; the mouse at `(10, 10)` is left of the canvas and is no hit.

**Formula 2: pointer mode.** A pointer move sets Pointer mode when `dx² + dy² > PointerMoveThreshold²` measured in native pixels from the pointer position at the last mode change (not per event), with `PointerMoveThreshold = 3`. *Examples:* a move of `(2, 2)` is 8, not above 9, and does not switch; a move of `(3, 1)` is 10 and does. A re-layout under a still pointer is not a move.

**Formula 3: click-hold or drag.** A press is classified at its release from its **largest excursion** (latched: a drag out and back to the start is still a drag): `max(dx² + dy²) ≤ DragThreshold²` (native pixels, `DragThreshold = 3`) and over the same cell (or the same slot for a tray or stash source) is a click-hold; otherwise it is a drag. A second click on the held item's source returns it and emits no `Place`. *Examples:* a release after `(1, 1)` over the same cell is a click-hold; a release after `(4, 0)` is a drag even over the same cell.

**Formula 4: the input lock.** `lockUntil = t_change + InputLockMs` with `InputLockMs = 250`; a commit-class action (Confirm, Select, Start, Skip, SkipOffer, Restart or a dialog button) with timestamp `t < lockUntil` is ignored and one with `t ≥ lockUntil` is accepted. *Example:* a state change at `t = 10,000` ignores a press at 10,200 and accepts one at 10,250.

**Formula 5: key repeat.** The device layer generates repeats from a `Tick(t)` and marks them `IsRepeat`; the core reads no timer. A held navigation key fires at the press, then at `RepeatDelayMs = 400`, then every `RepeatIntervalMs = 125` ms (a boundary time inclusive: at `t = 525` the third event fires), so the count at time `t` after the press is `n(t) = 1` for `t < 400` and `2 + ⌊(t − 400) / 125⌋` otherwise; with two keys held only the last pressed repeats; only the arrows and the jump keys repeat (Tab, Confirm, Select, R, Z, X and the speed keys do not); at most one repeat is generated per tick (`nextFire += interval`) and none survives an `InputReset`; a single-step option turns repeat off. *Example:* holding an arrow for 700 ms fires at 0, 400, 525 and 650 ms, 4 events.

**Formula 6: parity budget (acceptance check).** For a standard rearrangement, `keyboardActions ≤ ⌈1.5 × mouseActions⌉`, counting discrete presses and clicks including the navigation to the source item (a chord counts as one action) from Nav mode (after mouse use the first key only reveals the ring and adds one press). The standard rearrangements are: (a) a tray item to a grid cell, (b) a placed item rotated and moved within a grid, (c) a placed item moved to another hero's grid. *Examples:* (a) takes 2 clicks with the mouse and 3 presses with the keyboard (NextZone to land on the first tray item, Select, Select at the auto-place candidate): `3 ≤ ⌈3⌉ = 3`. (b) takes 3 with the mouse (click, right click, click) and 4 with the keyboard (a jump to the item, Select, R, Select): `4 ≤ ⌈4.5⌉ = 5`.

> *`gameplay-programmer` and `ux-designer` consulted (2026-11-13); the cursor mapping and the thresholds are theirs.*

## Edge Cases

Format: *if condition: exact outcome* (the detector: `ActionMapper`, `FocusModel`, `BindingMap`, or the named system).

**Transitions and the lock**

- *A press happens in view generation N and its release lands in generation N+1:* the release does nothing; the press stores `(target, viewGeneration)` and activation needs both to match, which closes click-through after a door confirm (ActionMapper).
- *A key or button is pressed during the lock:* it is ignored and does not fire when the lock expires; its repeats and release are ignored; the lock is judged at the press.
- *Enter is held across auto-skipped reward steps:* repeats are dropped; the next state's lock starts at the time the view visibly changes, not at the model change; after playback it starts at the playback boundary.
- *A double click lands on Start or a dialog's destructive button after a state change or a dialog opening:* the lock covers every commit-class action; Esc, Navigate and Details stay live.
- *An input arrives during the door reveal:* everything is dropped except Esc and the menu; a click that skips the reveal does not arm a card; the lock starts when the reveal ends.
- *The view changes without an input event* (the reveal ends, playback ends): the Unity layer sends `ViewChanged(t)`.
- *An action consumes a press* (Start, an Esc that cancels mid-drag, a right button that rotates): the matching release is swallowed.
- *The view is replaced* (Stopped, Ended, Abandoned, a restore, `ReturnToMenu`): the held state is cleared with no command.
- *Two events share a timestamp:* they are ordered by the device layer's sequence number; a timestamp that goes backwards is clamped (`tEff = max(t, lastTEff)`); times are integer milliseconds and a seconds value converts by floor.

**Drag, hold and focus loss**

- *The window is deactivated, the player alt-tabs, or capture is lost:* `InputReset` clears every pressed key, button and repeat; a Drag-origin hold returns the item and a Click-origin hold stays held.
- *The core believes a button is down and the device says it is up* (a release outside the window): a `synthetic` release is issued; it never commits and returns the item.
- *The pointer leaves the canvas while dragging:* the target is none (no `Preview` call), the ghost is clamped to the canvas edge, the release returns the item, and coming back resumes.
- *The first pointer sample after the window regains focus jumps:* the anchor is re-seeded and the mode does not change.
- *A held item's source no longer exists* (an anomaly, since Start is blocked while holding): the held state is dropped and a chip explains; nothing throws.
- *Esc or a release returns an item whose source hero is not the visible one:* the workbench switches to the source hero (a 100 ms cross-fade) so the return is visible.
- *Another item is clicked while holding:* it is a `Place`, and `Occupied` names the blockers; a drop where the snap radius finds nothing legal is refused in place.
- *X, Delete, Z, O, comma or period is pressed while holding:* nothing happens and a reason chip shows; a click on Start or Undo while holding shows the reason and is not a `Place`.

**Cursor and focus**

- *A pinned item is picked* (Vertical Slice, System 19; the `Pinned` result is not in the MVP): no Holding state is entered, a chip says why, the plate stays focusable and Details-able, and the jump skips it.
- *The cursor is on a locked cell:* Select with nothing held shows the "opens with a door" popup; with a held item the ghost is invalid (`LockedCell`) and the jump skips it.
- *A zone has no targets* (the tray list is empty): Tab skips it; a zone with only a disabled control is not empty.
- *A hero switch lands on a smaller bound:* clamp, then the nearest open cell by `(distance, y, x)`.
- *A jump finds no legal placement:* the cursor stays and a "no room" cue plays; the jump uses the held orientation only, so at most 36 candidates (the full 144 applies only to the tray ghost's search), ties by `(manhattan, y, x)`.
- *Memory points at a cell now covered by another item:* focus lands on that plate; after a Remove it lands on the vacated cell; after an Unlock the memory stays valid.
- *The focused target is removed* (a card after Confirm, a ware, a tray entry): focus is resolved in the same event, before the next event: the same index, then the previous, then the next, then the zone's first target, then the state's anchor.
- *Tab is pressed with a popup open:* popups are not interactive and are ignored; moving focus does not unpin; Esc closes; Details on another target re-pins to it.
- *A dialog is open:* Tab cycles inside it; a disabled Start remains focusable and shows its reason.
- *The tray changes while the tray dialog is open:* unreachable by the player (edits are blocked), so a defensive path: cancel, reopen with the new list, apply the lock, default "Go back" (System 5's atomicity is still open).
- *A re-layout puts a target under a still pointer:* `SetHover` may update hover in Pointer mode but never changes the mode; in Nav mode it is ignored.
- *A mouse button is pressed in Nav mode:* Pointer mode starts and the pointer is hit-tested afresh; the wheel does not change the mode.
- *The first key after Pointer mode is Select or Enter:* it only reveals the ring (Enter on a focused, non-armed button with a hidden ring reveals first, then acts on the next press).
- *A trackpad palm causes micro-moves while typing:* the mode could flap; a hysteresis (a larger threshold in the 100 ms after a Nav key) is an Open Question.

**Rebinding**

- *Two actions share a key in non-overlapping contexts* (Speed 1, 2, 3 and SwitchHero 1, 2, 3): allowed, and the UI shows the context; a key held across a context change never fires the new context's action.
- *A binding would remove the last one on a shipped scheme:* refused (BindingMap).
- *Esc, Enter, Tab, an arrow, the left click or a modifier alone is captured:* refused with a reason; Esc cancels capture; chords match modifiers exactly (R is not Shift+R).
- *A blob fails its checksum, version or any invariant, or a newer blob names an unknown action:* the whole map falls back to defaults and the stored blob is not overwritten until the player edits; a conflict or a missing action after load also means defaults.
- *A key does not exist on another layout:* bindings are physical codes, so they travel; labels fall back to text; "Reset to defaults" is reachable by reserved keys only.
- *A Vietnamese IME (Telex or VNI) is active:* gameplay reads key state and never text input; the defaults R, F, X, S, J, Z and the digits 1 to 6 collide with Telex tone keys and VNI digits, and the Unity 6.6 behaviour is NOT SOURCEABLE, so a Play Mode test is required.
- *Mouse buttons:* fixed in the MVP; a right click while holding rotates and its release does not open Details.

**Repeat, wheel and pad**

- *Key repeat:* only the arrows and the jump keys repeat; repeats are synthesised from `Tick(t)`; a repeat never survives `InputReset`.
- *The wheel with nothing held:* nothing, except scrolling a modal, a paged popup or a list (priority: modal, pinned popup, scroll region, held rotation).
- *A pad connects mid-run:* nothing changes; `PromptDevice` changes only on an event that resolves to a bound action, and stick drift is ignored.

> *`systems-designer` consulted (2026-11-13); its contradictions are applied (Details on the right click, the Esc order with rebind capture and dialogs first, Skip on its own key, the playback live set, the lock over all commit-class actions, the `viewGeneration`, the ghost by `Preview`, the context map).*

## Dependencies

| System | Direction | Nature | Notes |
|---|---|---|---|
| 5 Run State and Flow | Depends on (hard) | The run phase, the awaited step and the legal commands; the command interface | System 6 maps actions to its commands and never invents one. |
| 8 Grid Backpack | Depends on (hard) | Operations, result codes, `Preview`, `AutoPlace`, Freeze state | Rotation is clockwise quarter turns only. |
| 37 Settings | Related (hard for persistence) | Stores the binding blob and the key-repeat settings | The index stub (playback speed only) is outdated: key bindings are stored here. |
| 7 Localization | Related | Key labels and prompt text | |
| 25 Backpack UI, 26 Hover Popup and Glossary, 27 Combat HUD, 28 Door and Room Panels, 31 Menus | Depended on by | Register targets, receive focus, hover and semantic actions | Their provisional input assumptions are reconciled by this GDD. |
| 29 Payoff Presentation | Depended on by | The playback gate; speed and Skip bindings | |
| 33 Tutorial, 34 Accessibility | Depended on by | Prompts; the gamepad expiry | System 34 owns the waiver's expiry. |
| 35 Balance Simulation | None | The harness issues commands directly, not input events | |

The systems index lists System 6 with no dependencies; it consumes System 5's command interface and System 8's results through narrow ports. There is no cycle: System 6 reads those systems' public results and the systems above read System 6's actions.

## Tuning Knobs

All values are provisional. They live in data (System 2) or settings (System 37), never in code.

| Knob | Default | Safe range | Affects | Failure at the extremes |
|---|---|---|---|---|
| `DragThreshold` | 3 native px | 1 to 8 | Click-hold versus drag | Low: tremor starts drags; high: drags feel sticky. |
| `PointerMoveThreshold` | 3 native px | 1 to 8 | Pointer mode switching | Low: flapping on tiny jitter; high: sluggish hover. |
| `InputLockMs` | 250 | 100 to 500 | Click-through safety | Low: double-click lands on a new screen; high: feels unresponsive. |
| `RepeatDelayMs`, `RepeatIntervalMs` | 400, 125 | delay 200 to 800, interval 50 to 250; or single-step | Cursor and focus movement | Fast: overshoot; slow: tedium. |
| Parity ratio | 1.5 | 1.0 to 2.0 | Keyboard versus mouse effort | High: keyboard play is a chore. |
| Wrap on arrows | off | on or off | Navigation | On: surprising jumps across a 30-cell grid. |
| Hover popup delay | 300 ms (System 26) | 200 to 500 | Information timing | Owned by System 26. |
| Focus popup delay | 0 | fixed | Instant information | Fixed by the art bible. |

Interactions: `InputLockMs` and the door reveal (art bible 7.12) together decide when a card can first be confirmed; the repeat settings and the flash limit (art bible 4.12) interact when repeated focus triggers partner highlights (they are debounced).

## Visual/Audio Requirements

System 6 produces no visuals or sound; it fixes the states the presentation must show for input. The art bible's state table (7.11) already gives the forms.

- **Rings:** hover is a 1 px ring; focus is a 2 px Candle ring, z-lifted, with a 1 px gap (3 px in all, which fits the 4 px workbench padding); the ring traces the footprint of an L-shaped plate. Empty and locked cells need a focus ring too (the state table lists only hover, valid and invalid for empty cells). The Start button's resting Candle 2 px frame is identical to the focus ring, so its focus form needs a distinct treatment (art-director).
- **Prompt chips:** plain square-cornered keycaps (12 px) showing the live binding of the active layout; the rotate chip appears next to the held plate; the pad-button chips are drawn as squares with the button letter (reserved for the Vertical Slice).
- **Cues:** a rejection cue for a disabled control (a short non-visual cue is required by the art bible), a confirm cue and a speed cue; their owners (audio) are open. No sound or visual is timed or urgent.
- **No flicker:** key repeat across a grid must not trigger bridge and partner highlights faster than the flash limit (art bible 4.12); the content swaps without a re-fade.
- **Art bible gaps:** a focus form for the Start button and for empty and locked cells; the held-plate ring is hidden by the plate unless the z-lift of 7.11 is kept; a BuffClaim and reward-shelf panel (7.10).

*`ux-designer` and `accessibility-specialist` consulted (2026-11-13). Asset specs belong to `/asset-spec`.*

## Game Feel

Input is calm and exact; the hands never feel tested.

- **Immediate response.** The held plate follows the pointer with no perceptible lag and the footprint preview updates within one frame of the cursor crossing a cell boundary (Grid Backpack).
- **Forgiving drops.** A drop snaps to the nearest legal anchor, an invalid drop is refused in place with no penalty and returns the item; a slip is always one Esc away.
- **Keyboard parity.** Everything done with the mouse can be done with the keyboard in about the same number of actions (Formula 6); a pick from the tray lands on the auto-place candidate so the usual case is two presses.
- **No surprises.** The first arrow after using the mouse only shows where the focus is; Enter never confirms something that is not visibly armed; Start needs a deliberate action and never follows an accidental click.
- **No latency from the core.** Each event costs a handful of integer operations; the core allocates nothing per frame in the steady state.

## UI Requirements

Layout and interaction belong to Systems 25 to 31 and their UX specs (`/ux-design`); this system fixes the input contract they build on.

| Information | Where | Rule |
|---|---|---|
| The live binding of an action | Prompt chips next to controls | Read from the binding map and the active layout; text fallback. |
| The reason a control is disabled | A chip on focus or activation | From the awaited step; shown at once. |
| The focus ring | On the focused target | Visible at all times in Nav mode; hidden in Pointer mode except in dialogs. |
| The held item | Following the pointer or cursor | Lifted 2 px with shadow and ring; the source shows an empty bed; the rotate prompt chip beside it. |
| Dialogs | Tray loss ("Leave behind: X, Y?"), abandon | Default focus on the safe button; Esc equals it. |
| Rebinding | The settings screen (System 37, last to build, first to cut) | Capture mode with a Candle 2 px frame and "..."; a conflict names the other action; reset one and reset all. |

Accessibility: nothing is timed; no action needs a hold or a chord; one-handed play is possible; every state has a non-colour cue; the screen reader is not assumed (a text status line is the data source).

## Cross-References

| This GDD says | Defined in | Notes |
|---|---|---|
| Commands, serial, the playback gate, abandon, auto-skips | `design/gdd/run-state-and-flow.md` Rules 1, 5, 6, 12 | System 6 maps to these and invents none. |
| Operations, results, `Preview`, `AutoPlace`, clockwise rotation | `design/gdd/grid-backpack.md` | The held state mirrors Rule 7 and 11. |
| Held item, click and drag parity, start confirmation | `design/gdd/backpack-ui.md` | Reconciled here. |
| Popups, Details, paging and Cancel | `design/gdd/hover-popup-and-glossary.md` | Reconciled here. |
| Speed, Skip and the order strip | `design/gdd/combat-hud.md` | Bindings here. |
| The claim step and the reward shelf | `design/gdd/door-generation-and-tiers.md`, `design/gdd/commitment-and-drop-model.md` | Pre-armed suggested hero. |
| Remapping, prompts, no timed input | `design/art/art-bible.md` 7.11 to 7.13 | |
| Pillars 3 and 4 and the anti-pillar | `design/gdd/game-concept.md` | Player Fantasy anchors. |

## Acceptance Criteria

Evidence type: **Logic** for the pure core, automated EditMode tests in `Assets/_Project/Tests/EditMode/InputAndFocus/` (class `InputAndFocusTests`, assembly `Project05.EditModeTests`), named `[Scenario]_[Expected]`; gate level BLOCKING. UI criteria are **UI** evidence (retained screenshots in `production/qa/evidence/`, at the minimum and maximum resolutions); Unity-layer lifetime checks are Play Mode evidence. Each row is its own `[TestCase]` with a passing control. Tests use test-local fixtures only.

**Fixtures.** `ScriptedInputSource`: a list of `(t_ms, seq, InputEvent)` plus `Tick(t)` events, sorted by `(t, seq)`; an event carries the action, phase, pointer position, device and an `IsRepeat` flag. `FocusGraphs`: a builder per state (Planning, HeroPick, BuffClaim, OfferPending, DoorChoice, Playback, End) with fixed ordinal IDs and rectangles, a minimal 3 × 3 grid with one 2 × 1 plate for arrow tests, and a maximum Planning graph that stays at or under 199 nodes. `StubFlow`: phase, awaited step, a legal-command table, the current serial (set to 7), recording the dispatched commands. `StubGrid`: 6 × 6 with a scripted `Preview` (with a call counter), `AutoPlace` fixed at (2,3) and a Frozen flag. `DefaultBindings`: a literal copy of the Rule 2 table in the test, asserted equal to production (if the test read production the oracle would be circular). `RefPixel`: a floor-division reference for Formula 1, used for sweeps only. `RefFnv`: System 5's reference hasher and vectors (a duplicate hasher is flagged unless the implementation is shared).

**Rules**

| # | Given / When / Then |
|---|---|
| 1a | **Layers.** Planning, `StubFlow` serial 7: Ctrl+Enter gives the intent `Flow(StartFight, acceptLoss = false)` carrying the serial of the view it was rendered from (7), not the serial current at dispatch. |
| 1b | Every intent kind belongs to System 5's command enum, System 8's command enum or the UI-intent set; a hover from the toolkit adapter yields only `SetHover(id)` and no intent. |
| 2a | **Defaults.** One `[TestCase]` per row of the Rule 2 table (about 24): the production default equals the literal, including the plain alternatives PageDown/PageUp, T, N and B. |
| 2b | DoorChoice: key `1` gives `SwitchHero(0)` and arms no card. |
| 2c | A sweep over all states and keys: zero `Confirm` intents come from Space. |
| 3a | **Held item.** Press and release at `d² = 0, 2 (1,1), 8 (2,2)` or `9 (3,0)` over the same cell give a click-hold; a release at `(3,1)` (`d² = 10`), `(4,0)` (16) or across a cell boundary gives a drag; both reach the same `Holding` state. |
| 3b | An invalid place is refused and stays Holding with no change; a click-to-place outside every grid gives no intent and stays Holding. |
| 3c | A drag released off every grid: Holding becomes None, the item is back at its source, with 0 Remove or destroy intents. |
| 3d | Hero switch while holding keeps the item ID and orientation; Undo while holding gives no intent (control: Undo after the place gives one); a second pick gives no intent. |
| 3e | A tray pick shows the ghost at (2,3) found by a pure loop over `Preview` (the stub's `AutoPlace` is never called and Undo is unchanged); two Select events give `Place(2,3)`. |
| 4 | **Cancel.** The Esc priority is a 5-level matrix, one `[TestCase]` per level with the higher levels absent (dialog, popup, held item, armed card, Pause); a swapped-order mutant fails at least 4 of them. Rotation: CCW is 3 CW intents; the right click rotates while holding (control: with nothing held it gives Cancel); an unchanged click on the held item's source returns it and emits no `Place`, while a click on it after a rotation is a `Place`; the right click with nothing held opens Details (control: held, it rotates). |
| 5a | **Cell cursor.** A 2 × 1 plate: one arrow press leaves it (1 press, not 2); arrows clamp at the edges (no wrap). |
| 5b | A jump gives the nearest item by the Rule 5 metric and tie-break; held, it gives the nearest legal placement, with `Preview` called at most 144 times (6 × 6 × 4). |
| 5c | Zone-edge crossing happens only on adjacent layout edges; the per-zone cursor memory survives a hero switch. |
| 6a | **Select then confirm.** Select arms and Enter commits; Space, and Enter on an unarmed target, do not commit. |
| 6b | BuffClaim pre-armed: one Enter gives `ClaimBuff(suggested)`. |
| 6c | Start is disabled while holding, while a reward step is pending and during playback (control: enabled otherwise); a non-empty tray opens the dialog with "Go back" focused. |
| 7a | **Guards.** Playback: every action outside Speed, Skip, Pause, Abandon and read-only focus gives no intent; Skip (K) emits no outcome-bearing intent and arms a latched Skip, keys 1, 2, 3 cancel it and emit no outcome-bearing intent, Space on a focused speed segment activates it and never skips, and read-only Details on an intent tab is live. |
| 7b | Frozen grid: the gate refuses a grid command and the model still returns `Frozen`. |
| 7c | Held: `StartFight`, `ChooseDoor`, `ClaimBuff`, `TakeOffer` and Undo each give no intent. |
| 7d | `IsRepeat` Enter and Space give 0 intents (control: the first press gives 1). |
| 7e | The input lock follows Formula 4; Esc is not locked; an Enter that activated Start is consumed until its release. |
| 7f | A command queued across a phase change is dropped and never dispatched. |
| 7g | The wheel with nothing held gives no rotate (control: held gives 1). |
| 8 | **Focus model.** In a synthetic graph, Tab wraps across zones and arrows clamp; a plate is one target; HP numerals, bridges and decoration are not targets; held: Tab visits only the drop zones and the hero switch, and Start stays focusable but disabled; entering the lower section with Tab lands on the first tray item when the tray is not empty. |
| 9a | **Focus by state.** For each state and its entry event exactly 1 target is focused, equal to the Rule 9 anchor; a pending reward overrides the grid memory. |
| 9b | After a dialog closes focus is on the opener, with the fallbacks tested one by one; after a place or undo focus is on the placed or restored plate. |
| 9c | A traversal from the anchor with no pointer reaches 100 percent of the targets of every state. |
| 10a | **Hover and focus.** Pointer mode: hover ring and popup live, focus ring hidden, focus remembered; Nav mode: a pointer move at or below the Formula 2 threshold keeps hover suppressed. |
| 10b | The first arrow after Pointer mode reveals the ring only (focus unchanged, 0 intents); the second moves one step; dialogs always show the ring; Esc, R and Z do not change the mode. |
| 11 | **Disabled.** A disabled control stays in the Tab order and shows its reason; activation gives a rejection cue with 0 delay and 0 commands. |
| 12 | **Modals.** A dialog opens with the default on the safe button; Esc equals it; the destructive button needs an explicit move plus Enter; Tab cycles inside; a tray change while open cancels and reopens it; Abandon works in Playback. |
| 13a | **Bindings.** Rotate rebound to X (already SendToStash in an overlapping context) gives `Rejected(Conflict, SendToStash)` and leaves the map unchanged; control: X for Speed1 in the Playback context is accepted. |
| 13b | Reserved keys (Esc, arrows, Tab, Enter, left click) are rejected; unbinding the last binding is rejected; Cancel keeps Esc; a swap is offered, never silent. |
| 13c | The canonical text round-trips byte for byte; the checksum line is FNV-1a 64; the vector `"x1"` is `08f10707b58d70c2`. |
| 13d | A bad blob (empty, null, truncated, wrong version, a flipped byte) gives the defaults and no throw. |
| 14 | **Gamepad contract.** The defaults contain 0 Pad bindings; `PromptDevice.Pad` exists; criterion 9c passes with no pointer. |
| 15a | **Determinism.** The same script run twice, and with the focus graph built in reversed node order, gives an identical FNV hash over the intent log; no dictionary or hash ordering. |
| 16a | **Accessibility.** Every action has at least one binding or focus path with no modifier. |
| 16b | Each of the three held-state paths (click, click; drag; keyboard cursor) reaches Holding. |
| 16c | The key-repeat delay and the single-step option change the Formula 5 counts. |

**Formulas**

| # | Given / When / Then |
|---|---|
| 17 | **F1.** `1920 × 1080`: scale 3, offset (0,0), `(960,540)` → `(320,180)`, `(1919,1079)` → `(639,359)`, `(1920,1080)` outside. `1366 × 768`: scale 2, offset (43,24), `(53,30)` → `(5,3)`, `(43,24)` → `(0,0)`, `(1322,743)` → `(639,359)`, `(1323,743)` outside, `(10,10)` outside, `(42,30)` outside (floor division gives −1; a truncating mutant gives 0 and a false hit). `2560 × 1080`: scale `min(4,3) = 3`, offset (320,0), `(319,0)` outside, `(2239,1079)` → `(639,359)`. `1300 × 800`: scale 2, offset (10,40). |
| 18 | **F2.** From the anchor (0,0): moves to (1,0), (2,0), (3,0) stay Nav (`d² = 9`, not above 9); (4,0) switches to Pointer; `(2,2)` no and `(3,1)` yes; a per-event-delta mutant never switches. |
| 19 | **F3.** The Rule 3a values; a press whose largest excursion was 5 px and that is released back at its start is a drag (the excursion is latched). |
| 20 | **F4.** `t_change = 10,000`: presses at 10,000, 10,200 and 10,249 are ignored and 10,250 is accepted. |
| 21 | **F5.** `n(t)`: n(399) = 1, n(400) = 2, n(524) = 2, n(525) = 3, n(649) = 3, n(650) = 4, n(700) = 4, n(775) = 5; single-step gives 1 for any `t`; a `Tick` spanning 5,000 ms emits exactly `n(5000)` repeats. |
| 22 | **F6.** Three scripted rearrangements, each run through the real mapper and checked to reach the goal state on the stub grid; the bound is `(3m + 1) / 2` in integers (`m = 2` → 3, `m = 3` → 5, `m = 4` → 6). |

**Non-functional**

| # | Criterion |
|---|---|
| 23 | **Engine-free.** The core assembly sets `noEngineReferences`; an IL scan finds no `UnityEngine`, mutable `static` field, `float`, `double`, `DateTime`, `Random` or `GetHashCode`. The assembly name is NOT SOURCEABLE until the architecture ADR. |
| 24 | **Allocation.** Over 1,000 warmed-up events the managed allocation delta is 0, with an allocating control loop proving the probe works. |
| 25 | **Operation budget (counted by a seam, not timed).** At most 1 intent per event; `Preview` at most 144 calls per held jump; a focus move visits at most 199 nodes; the maximum Planning graph has at most 199 nodes. |
| 26 | **UI and Play Mode.** Retained screenshots: the 1 px hover ring against the 2 px lifted focus ring with its 1 px gap, the non-colour cues (corner square, cross on blocked cells), the held-item lift and shadow, the disabled-reason chip, the dialogs, and the per-state focus anchors; Play Mode: `Enable`, `Disable` and `Dispose` are paired and there are no duplicate subscriptions across two enter and exit cycles with the domain reload off. [BLOCKED: Systems 25 to 28, 31 and the UI toolkit decision] |

**Blocked criteria by system.** System 5 and System 8: the real phase provider and grid (the stubs carry the logic until then). System 7: key-label text (the text fallback for an unnamed key is testable now). System 37: storing the blob (parse and validate are testable now). Systems 25, 26, 27, 28, 31: all UI evidence. The accessibility waiver in `design/accessibility-requirements.md` cannot be verified until that file exists.

**Watched failures.** Each mutant must make a named criterion fail: hover winning in Nav mode (10a); the first navigation key after Pointer mode moving (10b); the Esc order swapped (4); Start enabled while holding (6c); auto-repeat confirming (7d); the lock `<=` instead of `<`, or 249 instead of 250 (20, 7e); a reserved key rebindable (13b); a silent conflict or a silent swap (13a); a wrap on the arrows (5a, 8); focus lost, or two targets focused, after a state change (9a); an off-grid drag sending the item to the tray or destroying it (3c); Space confirming (2c, 6a); door cards bound to 1, 2, 3 (2b); the wheel rotating with nothing held (7g); truncating division at negative coordinates (17); a per-event delta in the pointer-mode test (18); `>=` instead of `>` at the thresholds (18, 19); a stale queued command re-queued (7f); a modal default on the destructive button (12); a static shared between sessions (15a); leaving a plate costing one press per covered cell (5a); the pre-armed default missing (6b).

> *`qa-lead` consulted (2026-11-13). Its recomputation confirmed the Formula 1 to 5 examples and found the defects now fixed in Formulas 1 to 6 and Rules 2, 4, 5, 7, 8, 10, 13 and 16.*

## Open Questions

| Question | Owner | Resolve by |
|---|---|---|
| System 31 adds a last-fight details button to the compact end summary. Where does it fit in the end focus graph, and how do open/close and focus-loss during Skip/dialog/Details restore focus without duplicate menus or unintended commits? The user chose menu-on-deactivation during playback. | Systems 6/29/30/31, UX | Before menu and screen UX implementation. |
| **The UI toolkit** (UGUI or UI Toolkit) is undecided; the focus ring, drag and drop and instant popup behaviours of each are NOT SOURCEABLE in `docs/engine-reference`; the core stays toolkit-agnostic behind an input service. | technical-director, ui-programmer | Architecture ADR. |
| **The architecture ADR:** the engine-free assembly name and layout, the event and intent type names, and the persistence of the binding blob. | technical-director, lead-programmer | Before implementation. |
| **Engine facts to verify in the first spike:** the Input System 1.20 rebinding APIs on Unity 6.6, what `activeInputHandler: 1` means, the Enter Play Mode option values, Vietnamese IME behaviour, the Canvas reference resolution (the prototype uses 1600 × 900 against the 640 × 360 art canvas), and the pixel-perfect camera package. | engine-programmer | First spike. |
| **Settings (System 37):** the index stub (a playback-speed toggle only) is outdated; key bindings and key-repeat settings are stored here from the MVP. The rebind screen is built last and is the first thing cut; if cut, the waiver for the single-key shortcuts (WCAG 2.1.4) is recorded. | System 37, ux-designer | Sync after this GDD. |
| **Accessibility requirements:** `design/accessibility-requirements.md` does not exist; it must carry the input section (MVP commits to tier Standard for mouse and keyboard) and the gamepad waiver (expires at the Vertical Slice exit gate, owned by System 34), and supersedes the "mouse plus R" waiver of System 5. Reduced flashes and photosensitivity checks (art bible 4.12) may need to move to the MVP. | accessibility-specialist, System 34 | Before the UX specs. |
| **Gamepad:** a recorded exception to the project rule that every interactive element supports keyboard, mouse and gamepad; the focus graph and the action names are pad-ready. | System 34, lead-programmer | Vertical Slice. |
| **Art bible gaps:** a focus form for the Start button (its resting Candle frame equals the focus ring) and for empty and locked cells; a BuffClaim and a reward-shelf panel; a Details popup affordance on the mouse; double-click and "any click skips the reveal" in 7.10 conflict with the guards. | art-director | Art bible follow-up. |
| Whether the Skip segment becomes focusable (a keyboard and gamepad route); System 29 Rule 19 keeps it non-focusable and K the only binding, and no gamepad binding for Skip exists yet. | Systems 6 and 29, ux-designer | Before the gamepad contract is built. |
| **Sync with the parallel GDDs (25, 26, 27):** Backpack UI's open question on cancellation, focus and remapping, Hover Popup's Details, page and Cancel intents and Combat HUD's speed and Skip bindings are answered here; their provisional wording and the index lines ("mouse plus the R key", "gamepad partial") are updated. | Systems 25, 26, 27 and the index | Sync after this GDD. |
| **Pause:** the playback clock versus the menu; Systems 29 and 31 own the answer, but the action is named here. | Systems 29, 31 | Their GDDs. |
| **Palm and trackpad hysteresis** for Pointer and Nav mode flapping. | ux-designer | First playtests. |
| **Parity target (1.5 times the mouse's actions)** is a provisional bound and "standard rearrangement" is the three scripted ones; measure with real players. | ux-designer, qa-lead | First playtests. |
| **A cursor to cell function in the presence of a pixel-perfect renderer:** one shared viewport struct for hit testing and rendering, to avoid a 1 px offset. | engine-programmer | First spike. |
| **Grid Backpack:** a `Pinned` result and a locked-cell query are Vertical Slice (System 19); the tray ghost is computed by a pure loop over `Preview`, not `AutoPlace`. | System 8 | Before System 8 is re-reviewed. |
| Answered by System 29 ([payoff-presentation.md](payoff-presentation.md), 2026-11-14): Pause stops the playback clock while the menu is open and resumes after a 250 ms fade-in; a window deactivation pauses and opens the menu; Skip is one press available after the 250 ms lock that arms a latched Skip (ignored when repeated or pressed while armed, and refused while the menu is open; keys 1, 2, 3 and the speed segments cancel it; 2026-11-15 revision); the director raises `ViewChanged(t)` once at the terminal boundary. | Systems 6 and 29 | Next revision of this GDD. |

> *Specialists consulted for these questions: game-designer, ux-designer, accessibility-specialist, gameplay-programmer, engine-programmer, creative-director, qa-lead (2026-11-13).*
