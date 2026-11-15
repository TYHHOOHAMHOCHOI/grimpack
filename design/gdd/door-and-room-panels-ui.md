# Door and Room Panels UI

> **Status**: Designed (CD-GDD-ALIGN CONCERNS applied 2026-11-13; independent `/design-review` not yet run)
> **Author**: user + Claude design session, with specialist consultation
> **Last Updated**: 2026-11-13
> **Last Verified**: 2026-11-13
> **Implements Pillar**: Arrangement Is the Thinking (every choice is made on information shown before it commits); Power Has a Price (the door's price and promise sit side by side); The Payoff Is the Product (the promise on a card is exactly what the claim gives); Luck Follows Commitment (the shelf shows the lean it is answering)

## Summary

Door and Room Panels UI is the right-column screen that follows every won room. It presents three steps of one flow in a single panel: **BuffClaim** (the player picks which hero receives the door's certain buff, on the real hero pedestals), the **Reward shelf** (take one of up to three item cards or skip, under the committed-direction emblem) and **DoorChoice** (arm one of three door cards, then Confirm). All three share one card mould, one arm-then-Confirm grammar, one disabled-with-reason pattern and one input lock, and a completed step collapses into a one-line receipt so the three read as one beat. The system is a read-only presentation: it shows what Systems 5, 15, 17, 18 and 19 provide, sends semantic commands stamped with the view serial, and decides nothing. Shop, event and recruit panels belong to System 21 (Alpha).

> **Quick reference** — Layer: `Presentation` · Priority: `MVP` · Key deps: `Run State and Flow (5), Door Generation and Tiers (17), Commitment and Drop Model (18), Hover Popup and Glossary (26), Input and Focus (6), Localization and Text (7)`

## Overview

After every victory in rooms 1 to 8 the run enters a short reward sequence (System 5 Rule 9): BuffClaim (rooms 2 to 8, skipped when no hero can receive anything), OfferPending (skipped when the offer has no cards) and, after rooms 1 to 7, DoorChoice. The grid stays editable throughout; the decisive arrangement happens after the next room's intents are bound. System 28 is the presentation of that sequence. It owns the **layout of the right column** for these three screens, the **card mould** shared by door cards and shelf cards, the **claim states of the hero pedestals**, the **step cluster and receipts** that link the three screens, the **arm-then-Confirm interaction**, the **disabled reasons**, the **reveal**, and the **view-model contract** that lets the logic be tested without the engine. It does not own what is on a card (Systems 16, 17, 18 and 19), the popup (System 26), the input mapping and focus model (System 6), the flow and its commands (System 5), the text and its budgets (System 7) or the pedestals themselves (the party column, shared with Systems 25 and 27).

> *Specialists consulted for this section: game-designer, ux-designer, ui-programmer, art-director, accessibility-specialist (2026-11-13). Their findings are applied below; where they disagreed the user decided (see Core Rules 2, 3, 8, 9).*

## Player Fantasy

After a win the dungeon lays its terms on the table: one gift that is certain, one offer shaped by what the player has been building, and three doors that each ask a price. The player reads, points, and commits once, and nothing is taken until they say so. **After every win the dungeon names its gift and its price side by side, and the player chooses which to pay, never by accident.**

*Pillar anchors.* Pillar 4, Arrangement Is the Thinking: the screens show only information that exists before the fight (the promise, the danger, the roster, the lean, each hero's gain) and never imply more (no intent sequence is known before the door is chosen). Pillar 5, Power Has a Price: the promise chip sits beside the danger numeral, and the promise is stated as paid after the room. Pillar 2, The Payoff Is the Product (its design test: the clearer cause wins): a number on a card is the number the claim gives; a cap shortfall or a lost cell is shown before Confirm. Pillar 1, Luck Follows Commitment: the shelf emblem shows the direction the offer leaned toward and, when the player is uncommitted, how close they are.

*Guardrails:* never colour alone; never odds or weights; no panel hides the workbench; an irreversible choice needs two deliberate acts; a disabled control always says why; text is never shrunk or cut to fit.

*Risks the design guards against:* an Enter-mash that carries through all three steps; the claim being a no-thought click on the suggested hero (accepted by decision, since the suggested hero is pre-armed; watched through the Open Question on BuffClaim depth); a dominant Skip or Take on the shelf; a door misclick (no reroll or retreat exists); an item card mistaken for a door card; a reveal that traps the player or flashes.

*Design test.* The design is right if playtesters never commit a door or a reward by accident, can say why they gave the buff to the hero they chose, and sometimes pick a non-Purple door for a reason they can state (the last depends on System 35's non-dominance check, which the UI cannot supply).

> *`creative-director` consulted (2026-11-13, CD-GDD-ALIGN); the spine above is revised from their note.*

## Detailed Design

### Core Rules

1. **Read-only presenter, commands out.** An engine-free `DoorPanelsPresenter` is a pure function of `(RunView, Offer, DoorRow, claim previews, TextService)` and fills a preallocated `PanelVM`; a thin Unity binder draws it and forwards pointer and focus events as semantic intents (System 6). The panel never mutates the run: it sends `ClaimBuff(slot)`, `TakeOffer(i)`, `SkipOffer` and `ChooseDoor(i)` to System 5. Text leaves the presenter as `(TextKey, args)`; the binder resolves it (System 7 Rule 18). The presenter holds no statics, no clock and no RNG; the lock timer reads an injected clock.
2. **One panel, three steps (user decision).** The right column holds one persistent panel, never a modal and never a wash over the workbench. The three steps share the card mould, the arm-then-Confirm grammar, the disabled-reason chip and the lock. A **step cluster** at the left of the action strip shows three pips (Buff, Item, Door) and lists **only the steps that are actually pending**: an auto-skipped step never appears, room 1 shows Item and Door, and after room 8 the cluster ends at Item (the boss follows). A completed step collapses into a **receipt** beside the cluster (for example "Hero +8 max HP", "Took: Ember Blade", "Skipped"), at most 28 characters (a budget for System 7), so the player keeps the context of the earlier steps without a second screen.
3. **BuffClaim: on the real pedestals (user decision).** The choosers are the three party pedestals in the middle column (art bible 3.3.2), not cards in the right column. The right column shows only a 16 px header: the tier word of the door just won and the promise chips (HP; at Purple also the cell), in the stat-chip grammar, Bone. Each pedestal shows a **claim chip** at all times during this step: the **granted** amounts for that hero (from a dry run of System 17's pure `ClaimBuff`; the panel never recomputes a cap), the HP as `+n` and the cell as a cell glyph. The popup of the focused or hovered pedestal states the full reading: `Max HP a -> b` (b is `B_perm + hpGranted`, from Systems 15 and 17) and the hero's locked-cell count. Required warnings, shown as visible text and never hover-only: **cap shortfall** (granted less than promised; the chip shows the granted number and the foregone amount in Fog with the 5x5 cross glyph) and **lost cell** (the hero has no locked cell: the cell chip takes the Fog cross and the popup says so, phrased as the fix, key `ui.claim.cell_lost`). The hero stays selectable. There is no Skip. The default is the suggested hero (System 17 Rule 7), **pre-armed, with Confirm focused**, and the only indication of it is that arming plus a neutral popup line, `ui.claim.suggested` ("most items in your lean"); there is no mark on the pedestal and the other two heroes' gains stay equally visible, so the suggestion never reads as advice. If the 98 px pedestal cannot hold the claim chip, the fallback is a three-row hero list in the right column carrying the same fields (decided now so the open pedestal-capacity question cannot block this step).
4. **BuffClaim focus and arm.** Focus on a pedestal **arms** it (a single-select group, because the choice is exclusive and there is no skip), and arming switches the workbench to that hero (System 6 Rule 9). From Confirm, Left or Up returns focus to the armed pedestal and changes nothing. Keys 1 to 3 switch the workbench view only (they never arm); the popup and the pedestal always name the **armed** hero, so a viewed hero that differs from the armed one is never ambiguous (the armed pedestal keeps the corner square). Esc closes a popup or opens the menu; it never disarms (the pre-armed state is an invariant). Hover never arms in BuffClaim (it opens the popup only) and a click arms; Tab leaving the party zone keeps the arm. Confirm reads `Give to [hero]` (key `ui.claim.confirm`). After Confirm, the receipt is written and focus goes to the next real anchor.
5. **Reward shelf (OfferPending).** The column shows a **16 px header** with the committed-direction emblem (a 12 px keyword glyph on an INK-2 chip, 1 to 3 filled ticks; when uncommitted, the reason and margin line from System 18, in Fog), then **one row of up to three item cards**. A card is the item's **plate at its true footprint**, a fixed **10 px tag strip** beneath it (0 to 2 Bone 8 px direction glyphs, so no tier shifts; a tag the player already holds gets a 1 px Candle underline inside the focus ring's gap), and a **14 px label** in Fog: "Matches the build" or "other" (keys `ui.offer.match` and `ui.offer.other`), chosen by whether the card carries the committed direction and never by slot, weights or odds. Fewer than three cards leave **void bays** so no card moves under the cursor. The emblem is read-only but a focus stop (its popup shows the stored reading). The compact popup opens beneath the row. **Skip** is a plain button at the left of Confirm in the action strip, labelled in words with "you take nothing" in its popup; it **arms** as a pseudo-option (any armed card is disarmed, and the reverse), and Confirm then reads `Skip reward`. With a card armed, Confirm reads `Take [item]`. The default focus is the first card, **never Skip**, and nothing is pre-armed. This replaces System 18's "at most three rows" with one row (user decision); System 18 is amended in the sync.
6. **DoorChoice.** Three cards of 84 by 140 px (up to 90 by 140 at the 20 px cell size), 12 px gaps, no header; the popup opens beneath the row, left-aligned to the focused card and clamped to the column (art bible 7.10). The card content and reading order are System 17's (tier form and rank pips, the promise chip, the danger numeral beside or below it with its word, the class-count row with the elite pip, the item-quality word, the name, the tier word) and are laid out by the **pixel budget in Formula 2**. The compact popup carries the roster (System 16) and, as its first line, `ui.door.buff_after` ("Buff is claimed after you win this room"), because the promise is paid a room later. There is no curse tag on the MVP card: the second chip row is shared (Purple's cell chip now, Gold's curse tag at Vertical Slice, System 19). Confirm reads `Enter [tier name]` (key `ui.door.confirm`); the focus starts on the first card when the reveal unlocks input; nothing is pre-armed.
7. **Arm, then Confirm.** Every irreversible choice takes two acts: select arms (a ring plus the 4 by 4 corner square, art bible 7.11; the armed card holds its popup), Confirm commits from the action strip. Mouse: a click arms and replaces any other arm; **clicking the armed item again does nothing** (never a toggle-off, to avoid an accidental disarm); there is no double-click shortcut in the MVP (it is **[VS]**, and System 6 already drops it for Start). Keyboard: arrows move focus, Select (Space) arms, Confirm (Enter) commits and only on an armed item or a focused enabled Confirm; Space never confirms. Esc disarms (after closing the popup; in BuffClaim it never disarms, Rule 4). Hovering never commits. Confirm is **disabled but focusable** until a choice is armed, and its popup or chip says why: `ui.reason.choose_door`, `ui.reason.choose_card_or_skip`, `ui.reason.hold_drop_first` (an item is held; it disables Confirm and Skip alike), each at most 30 characters and written as the fix.
8. **Transitions (user decision).** There is no cross-fade, no overshoot and no screen swap. When a step is confirmed, the popup closes at once, the unchosen elements fall to ink and the chosen one changes by an eased step of at least 250 ms (art bible 7.12); the old focus target is released (no stale ring); the next step's cards **refill in place** in the same column. **Reveal** of a new row of cards: each card rises 8 px with a **240 ms** settle and an **80 ms** stagger, luminance rises take at least 100 ms; all elements of a card settle together (the promise-before-danger order and the shelf's emblem-tick sequence are **[VS]**). Cards are **readable, focusable and their popups live from the first frame**; only the commit-class actions wait (Rule 10). A commit-class press (click, Select, Confirm) finishes the reveal and is swallowed; Navigate and Details act normally and do not skip it. Finishing the reveal sends `ViewChanged` at once, so the lock starts then. When the last element lands the Unity layer sends `ViewChanged(t)` and the input lock starts (Rule 10). A skipped step costs no transition and no lock. The grid stays editable and unwashed throughout, (marking the cell opened by the claim for one beat is **[VS]**).
9. **Reduced flashes and reduced motion.** Under reduced flashes the card light swell becomes a steady glow with no pulse; under reduced motion the 8 px rise becomes a fade of equal duration; neither changes any timing, focus or lock. Luminance rises are at least 100 ms under every setting (art bible 4.12). The settings are System 37's and the accessibility file's (`design/accessibility-requirements.md`).
10. **Input lock and carry-through.** Commit-class actions (Confirm, Select, Skip, SkipOffer, Start, dialog buttons) are ignored for `InputLockMs` (250 ms, System 6 Rule 7e) after `ViewChanged(t)` and judged at the press, so one held Enter or Select can never pass through BuffClaim, then the shelf, then a door: a press during the lock never fires on expiry, and its repeats and release are swallowed. Navigate and Details stay live. Key auto-repeat on Select and Confirm is ignored. Nothing in this panel is timed: there is no countdown and no auto-advance; the lock is a minimum delay only.
11. **Stale and rejected commands.** Every command carries the `viewGeneration` of the view the target was rendered from (System 6 Rule 1), not the serial current at the click. `Rejected(Stale)` means the screen was already replaced: drop it silently, re-read the view, no error cue. `Rejected(WrongState)` or `BadIndex` keeps the screen and the arm, shows the reason chip at once with a non-audio cue (text, not sound only) and never clears the arm. A technical stop (`TechnicalStop`) leaves this panel for the Stopped screen (System 31). Queued commands are dropped on a generation change and never re-queued.
12. **Disabled reasons and Start.** During the reward steps the action strip shows only this panel's controls (Confirm, and Skip on the shelf); **Start is not shown** until the last step is done, when the strip returns to Start (System 5 rejects `StartFight` with `WrongState` until then, which the UI never offers). An item held on the cursor disables Confirm and Skip with `ui.reason.hold_drop_first` (grid commands still pass; System 6 Rule 7c must list `SkipOffer` among the commands blocked while an item is held).
13. **Language change and text fit.** The panel keeps the arm and the focus, re-resolves every label and re-clamps the open popup (System 7 Rule 18). Card text never shrinks or truncates: the budgets of Formula 2 are checked per language at build time (System 7 Rule 10); a Vietnamese name takes two lines of about 12 characters (under 16), a danger word at most 10 characters, a quality word and a tier word at most 12. The **Large** text set card layout is **[VS]**; until it ships, Large falls back to Standard on these screens unless the build validator passes every slot. The design intent for it: the item-quality word and the class-count row move into the popup, and the tier word, the promise chip and the danger numeral and word stay on the card (the carriers of Rule 15); it is offered only if the validator passes.
14. **Window deactivation and capture loss.** `InputReset` clears presses and repeats; the arm and the focus persist (they are UI state), the popup closes, and nothing commits when the window returns.
15. **Accessibility carriers.** Tier is the tier word plus rank pips plus the plate form; danger is a numeral plus a word; the lean is a glyph plus the label word plus the header text; the promise is read before the danger. The reading order equals the focus order: DoorChoice reads cards left to right then Confirm; the shelf reads the emblem, the cards left to right, Skip, then Confirm; BuffClaim reads the header, the pedestals left to right, then Confirm. The presenter exposes a `statusLine` (key plus args), for example "Armed: Blue door, +12 max HP, danger 7. Press Enter to confirm.", shown as the first line of the armed item's popup (exposing it to an accessibility layer is **[VS]**, since the toolkit support is unverified).
16. **Scope boundary.** This system covers the three reward-phase screens. Shop, event and recruit panels (art bible 7.10) are added when System 21 is designed; the hero pick screen belongs to Systems 5 and 31, the end-of-run screen to System 31. The panel never shows a probability, a weight or a hidden value.

### Scope tiers (CD-GDD-ALIGN, 2026-11-13)

The MVP builds the claim chips on the pedestals, the one-row shelf, the door cards, arm-then-Confirm with its reasons, the lock, the stale rule, the step cluster and receipts (a user decision), and one uniform settle for every row. Deferred to the Vertical Slice and marked **[VS]**: the reveal choreography (promise before danger, the emblem-tick sequence, the tag strip last), the Large text card layout, a double-click shortcut, marking the cell opened by the claim, exposing the `statusLine` to an accessibility layer, and the shelf's resonance and pivot audio cues. Their acceptance criteria are not MVP gates (AC 20; the choreography part of AC 16).

### States and Transitions

| Subject | State | Trigger | Result |
|---|---|---|---|
| Step | BuffClaim | Thaw after a win in rooms 2 to 8 with a grantable hero | Pedestal claim chips shown; the suggested hero pre-armed; Confirm focused. |
| Step | Shelf | `ClaimBuff` accepted, or BuffClaim skipped; the offer has at least one card | Row refilled in place; focus on card 1. |
| Step | DoorChoice | `TakeOffer` or `SkipOffer` accepted, or the shelf skipped; rooms 1 to 7 | Door row refilled in place; focus assigned when the reveal unlocks input. |
| Step | Done | `ChooseDoor` accepted (rooms 1 to 7), or the shelf confirmed after room 8 | The panel closes; the next room enters Arrangement (boss after room 8). |
| Panel | Revealing | A new row or step appears | Commit actions are ignored; Navigate, Details and the popup are live. |
| Panel | Locked | `ViewChanged(t)` | Commit actions ignored for 250 ms; then Armed-able. |
| Choice | Unarmed | Entry (or Esc) | Confirm disabled but focusable, with its reason. |
| Choice | Armed | Select or click (focus on a pedestal in BuffClaim) | Ring, corner square, popup held, Confirm enabled and named. |
| Choice | Committing | Confirm accepted by System 5 | Fall to ink, receipt written, next step. |
| Choice | Rejected | `WrongState`, `BadIndex` | Screen and arm kept; reason chip; `Stale` is dropped silently. |

### Interactions with Other Systems

| System | Data in | Data out | Interface owner |
|---|---|---|---|
| 5 Run State and Flow | The phase, the stored row and offer, the pending buff, `viewGeneration`, `ViewChanged` timing | `ClaimBuff(slot)`, `TakeOffer(i)`, `SkipOffer`, `ChooseDoor(i)`, each stamped | System 5 owns the flow and the rejection codes. |
| 17 Door Generation and Tiers | The row, the card content, the promise, the suggested hero and per-hero counts, the pure `ClaimBuff` for a dry run | None | System 17 owns content and the cap rules. |
| 16 Encounter Generation | Danger, class counts, the roster (through the row) | None | System 16 owns the cost side. |
| 18 Commitment and Drop Model | The stored reading, the offer cards and the label basis | None | System 18 owns the offer. |
| 19 Curses and Trade-offs | Gold curse on the card (Vertical Slice) | None | Deferred. |
| 15 Party and Heroes; 8 Grid Backpack | `B_perm`, locked-cell counts, the heroes | None | Owners of the values. |
| 26 Hover Popup and Glossary | The shared popup | The source, the held-open (armed) request, the `statusLine` | System 26 owns the popup. |
| 6 Input and Focus | Semantic actions, focus, Pointer/Nav mode, `InputLockMs`, `InputReset` | Targets, zones, the default focus per state | System 6 owns mapping and focus. |
| 7 Localization and Text | `TextService`, slot budgets | Keys and args | System 7 owns text. |
| 25 Backpack UI; 27 Combat HUD | The party pedestals and the workbench | The claim states of the pedestals; the arm switching the workbench hero | Pedestals are the party column; this system adds the claim states. |
| 31 Menus and Screen Flow | The Stopped and menu screens | `TechnicalStop` exit | System 31. |
| 37 Settings; 34 Accessibility | Reduced flashes, reduced motion, text scale | None | Those systems own the settings. |
| 35 Balance Simulation | None | None | System 35 tests whether the door choice is non-dominant. |

*Provisional:* the pixel budgets (art bible 3.4.4 and 8.8 were not read, so the frame thickness is an assumption), the action strip width for the step cluster and the receipt, the tallest plate footprint, and the pedestal capacity for the claim chip.

> *Specialists consulted: game-designer, ux-designer, ui-programmer, art-director, accessibility-specialist (2026-11-13).*

## Formulas

All values are integers in native pixels or milliseconds; all are provisional until the font and the art bible amendments exist.

**Formula 1: column fit.** Width: `3 × W_card + 2 × G = 276` at `W_card = 84, G = 12`, within the usable width of about 280 at the 24 px cell size (spare 4) and `3 × 90 + 24 = 294` within about 312 at the 20 px size. Height: `H_card + G_popup + H_popup = 140 + 4 + 140 = 284` within about 290.

**Formula 2: door card pixel budget (inner 76 × 132).**

| Row | px | Width rule |
|---|---|---|
| Tier form and rank pips (pips on the form base) | 20 | pips 4 px |
| Promise chip 1 (HP) | 12 | glyph 8 + chevron 5 + `+12` in 8 px digits, about 40 wide |
| Promise chip 2 (Purple cell; Gold curse tag at VS) | 12 + 1 gap | stacked, since two chips side by side would be 80 > 76 |
| Danger numeral (10 high, 8 wide) and word | 14 | word at most 10 characters (60 px) |
| Class-count row (3 size silhouettes, 8 px counts, elite pip) | 12 | about 62 wide |
| Item-quality word, Fog | 14 | at most 12 characters (72 px) |
| Name, Body | 28 | two lines of at most 12 characters |
| Tier word, Body | 14 | at most 12 characters |
| Gaps | 2 + 2 | |
| **Total** | **131 of 132** | **1 px spare: any addition breaks the budget** |

**Formula 3: shelf height.** `H_shelf = H_header + H_plate_max + H_tags + H_label = 16 + 72 + 10 + 14 = 112` (the tallest plate footprint of 72 px is an **assumption** pending the cell size and the tallest MVP piece). The popup space is `290 − 112 − 4 = 174 ≥ 140` required, spare 34. If `H_plate_max` exceeds 106 the compact popup no longer fits and the layout fails validation.

**Formula 4: reveal time.** `T_row(n) = 80 × (n − 1) + 240` ms for `n` cards, so `T_row(3) = 400`. In the MVP the shelf uses the same settle, `T_shelf = T_row(n)`, so at most 400 ms (the emblem-tick sequence, `40 × k` with `k ≤ 3`, is **[VS]** and would take it to 520 ms). The lock window opens at `t_ViewChanged` and a commit is accepted iff `t ≥ t_ViewChanged + 250`.

**Formula 5: claim display mapping (no new arithmetic).** Per hero `h`: `(hpGranted, cellsGranted) = ClaimBuff(runSlice, h, buff)` (System 17, dry run, no application); shown HP `b = B_perm(h) + hpGranted`; shortfall `= buff.hp − hpGranted` and `buff.cells − cellsGranted`; lost cell iff `buff.cells > 0 and cellsGranted = 0`. The panel displays these and never recomputes a cap.

**Formula 6: receipt length.** `len(receipt) ≤ 28` characters in each language (a budget for System 7's slot table; measured in pixels at the build step).

> *`ui-programmer` and `art-director` consulted (2026-11-13); the budgets are the art director's.*

## Edge Cases

Format: *condition: outcome.*

**Flow and screens**
- *No hero can receive anything:* BuffClaim is skipped inside the same command, with no prompt; the step cluster never shows Buff.
- *The offer has no cards:* the shelf is skipped (System 5); the cluster never shows Item.
- *Room 1:* no BuffClaim; the shelf then DoorChoice. *Room 8:* BuffClaim and the shelf, then the boss; no DoorChoice and no Door pip.
- *Fewer than three cards:* void bays keep the positions; focus skips a void bay.
- *The offer has one card:* Skip and Confirm still behave identically; nothing is pre-armed.
- *Both reward steps and the door are skipped (a boss-bound room 8 with nothing to give):* the panel never appears; the next room's Arrangement starts.
- *A restore lands in BuffClaim, OfferPending or DoorChoice:* the panel rebuilds from the stored row, offer and pending buff; the reveal is skipped and the lock starts at the first frame.
- *An abandon (`AbandonRun`) during any step:* the menu dialog (System 31) takes the focus; the panel is untouched underneath.

**BuffClaim**
- *Every hero has the same gain:* the suggested hero is still pre-armed; the others show identical chips.
- *The chosen hero has no locked cell:* the cell part is lost, shown before Confirm (Rule 3); Confirm is still legal.
- *A cap shortfall:* the chip shows the granted number and the foregone amount in Fog with the cross glyph; Confirm is legal.
- *All three heroes would receive 0 HP and 0 cells:* System 5 consumes the step without a prompt (this panel is never shown).
- *The armed hero differs from the hero the player is viewing in the workbench (key 1 to 3):* the armed pedestal keeps the corner square and the popup names it; arming never changes silently.
- *Esc in BuffClaim:* closes a popup or opens the menu; never disarms.

**Shelf and doors**
- *Skip armed, then a card clicked:* Skip disarms and the card arms (single selection).
- *Click on the armed card:* no-op, never a toggle-off.
- *An item is held on the cursor:* Confirm is disabled with `ui.reason.hold_drop_first`; the popup and the grid keep working.
- *The tray is full (an item taken would be lost):* the take is legal at System 5; this panel shows tray state near the shelf as information only. (Whether taking should warn is an Open Question.)
- *The same tier on all three door cards (rooms 2 only):* the cards still differ by position, roster and danger; none is pre-armed.
- *A Purple door and a Large text set:* the Large card layout is **[VS]**; until then Large falls back to Standard on these screens unless every slot passes the validator.

**Input and timing**
- *A press during the reveal or the lock:* ignored and swallowed with its repeats and release; Navigate and Details work.
- *A double-click:* the second press falls inside the lock or returns `Stale`/`NoEffect`; the button shows Pressed for 50 ms and nothing else.
- *Enter held through a step change:* the press that confirmed is consumed; the repeat is ignored; the lock covers the next step.
- *The pointer leaves the card while armed:* the arm persists; the popup is held until Confirm or disarm.
- *A pointer move of more than the threshold after keyboard use:* Pointer mode; hover rings and popups live, the focus ring hidden but remembered (System 6 Rule 10).
- *The window loses focus:* `InputReset`; the arm persists; the popup closes; nothing commits on return.
- *A rejected command (`WrongState`, `BadIndex`):* the screen and the arm stay; the reason chip shows at once with a text cue.
- *`Stale`:* dropped silently; the view re-renders.
- *A technical stop during a step:* the panel yields to the Stopped screen without a gameplay result.

**Text and accessibility**
- *The language changes mid-step:* arm and focus kept; every label re-resolved; the popup re-clamped; no card text shrinks.
- *A string is missing in a language:* System 7's fallback chain applies; a marker frame never carries a number; the card still shows the numerals from the owning system.
- *Reduced flashes or motion:* Rule 9; focus, lock and timing are unchanged.
- *No pointer, no keyboard (not supported in the MVP):* the gamepad contract exists in System 6 only; this panel registers its targets and zones for it.

> *Edge cases drawn from the specialists' failure lists (game-designer, ux-designer, accessibility-specialist).*

## Dependencies

| System | Direction | Nature | Notes |
|---|---|---|---|
| 5 Run State and Flow | Depends on (hard) | The flow, commands, rejection codes, `viewGeneration` | Owns the reward order; amended to name this panel. |
| 17 Door Generation and Tiers | Depends on (hard) | The row, the promise, the suggested hero, `ClaimBuff` | Its Open Question on the card pixel budget is answered here (Formula 2). |
| 18 Commitment and Drop Model | Depends on (hard) | The offer, the reading, the label basis | "At most three rows" becomes one row. |
| 26 Hover Popup and Glossary | Depends on (hard) | The shared popup | |
| 6 Input and Focus | Depends on (hard) | Mapping, focus zones, lock, reset | BuffClaim "arrows re-arm" is made consistent with "arrows move focus" by the focus-arms rule (Core Rule 4). |
| 7 Localization and Text | Depends on (hard) | Keys, budgets | New keys listed under UI Requirements. |
| 16 Encounter Generation | Depends on | Danger, class counts, roster | |
| 19 Curses and Trade-offs | Related | The Gold curse tag (Vertical Slice) | Shares the second chip row. |
| 15 Party and Heroes; 8 Grid Backpack | Depends on | `B_perm`, locked cells | Values only. |
| 25 Backpack UI; 27 Combat HUD | Related | The party pedestals and workbench | Shared surface; this system adds the claim states. |
| 21 Room Types | Depended on by (later) | Shop, event, recruit panels | Added when designed. |
| 31 Menus and Screen Flow; 33 Tutorial; 34 Accessibility; 37 Settings; 35 Balance Simulation | Related | Stopped screen, first-time hints, reduced flashes and motion, the non-dominance test | |

There is no cycle.

## Tuning Knobs

All values are provisional, live in data, and never in code.

| Knob | Default | Safe range | Affects | Failure at the extremes |
|---|---|---|---|---|
| Card settle time | 240 ms | 160 to 400 | Reveal length | Low: abrupt; high: slow to read. |
| Card stagger | 80 ms | 40 to 120 | Reveal order | High: late last card. |
| Tick step (shelf emblem, **[VS]**) | 40 ms | 30 to 80 | Emblem build | |
| Luminance rise floor | 100 ms | fixed by the art bible | Flash safety | Below it: flash risk. |
| Input lock | 250 ms | System 6 owns | Carry-through | Low: misclick; high: sluggish. |
| Receipt length | 28 characters | 20 to 36 | Strip fit | Low: unreadable; high: overflow. |
| Shelf cards per row | 3 | 1 to 3 | Layout | Fixed by the door row (3). |
| Popup minimum height | 140 px | art bible 7.10 | Fit (Formula 3) | Low: truncation. |
| Plate height cap (shelf) | 72 px | validated | Formula 3 | High: popup does not fit. |

Interactions: the reveal knobs and the lock together set how soon a decision can be committed (about 650 ms after a step appears at the defaults); the plate cap and the popup height set how much the shelf can show.

## Visual/Audio Requirements

System 28 produces no art or sound; it fixes the forms the screens must take (assets are specified by `/asset-spec`).

- **Forms (art bible 7.11):** default, hover (1 px Candle ring), focus (2 px ring, lifted), armed (ring plus 4 by 4 corner square, holds the popup), disabled (INK-3 frame, still focusable). Candle on cards means selection only; the owned-tag underline sits inside the focus ring's 1 px gap so the two never merge. Buttons: pressed is a Candle fill with INK-0 text for 50 ms.
- **Pedestal claim:** the existing selected form (ring, corner square, halo, name plate) plus the claim chip; the avatar zone stays empty (art bible 5.10.3). A cap shortfall and a lost cell use the Fog 5x5 cross glyph (the invalid-target vocabulary); no red and no Plum (Plum belongs to curses).
- **Shelf:** one shelf line, plates at true footprint, the 10 px tag strip, the Fog label; the emblem is the committed-direction chip with ticks (Bone and Candle only, never tier hue). Resonance thread and pivot follow System 18's Visual/Audio section.
- **Receipt and step cluster:** three pips with 8 px glyph labels, a Fog receipt line in the action strip, left of Skip and Confirm.
- **Audio (final design belongs to the audio director):** at most one cue per act: row reveal, arm (short click), confirm (heavy low tone), reject (a soft non-alarm tone with a text equivalent). Hover is silent; the loudest sound stays reserved for detonations; a reduced-flash variant exists. The shelf's matching-tag resonance and pivot cues (System 18) are **[VS]** with the choreography.
- **Art bible amendments needed (as Open Questions):** 7.3 (action strip and right-column rows for the claim and the shelf, including the step cluster and the Skip button; today it lists Confirm for door, shop and recruit only), 7.9 (popup placement under the shelf; the popup width is 144 in the 7.7 test and 152 in 7.9), 7.10 (rewrite the card anatomy to Formula 2; add the Reward and BuffClaim subsections; the curse tag; the Purple cue decision), 7.11 (pedestal and plate element notes), 7.12 (rows for the shelf reveal, the claim and the pivot), 7.8 (the glyph budget with 3 to 4 direction tags), 7.13 (the Large layout). The glossary writes "Hero buff" in one place and "Buff hero" in another (System 7's glossary has "Hero buff").
- **Assets to spec:** the door card mould per tier form, the rank pip, the size silhouettes and the elite pip, the promise chips and chevron, the emblem chip and tick, the direction glyphs at 8 and 12 px, the resonance thread (drawn in code), the shelf line, the step pips, and the VI/EN slot table.

*`art-director` consulted (2026-11-13). Asset specs belong to `/asset-spec`.*

## Game Feel

A decision beat with no timer and no hurry.

- **One beat, not three interruptions.** The panel stays; steps collapse into receipts and refill in place. The workbench is never washed.
- **Light, tactile commits.** The commit is deliberately two acts (arm, then Confirm); the arm gives a ring and a click, the confirm a heavy low tone and a fall to ink.
- **Settle, not show.** A row settles in about 400 ms; the player can read and move focus from the first frame.
- **Certain payout.** The number the claim chip shows is the number granted; a shortfall or a lost cell is stated before Confirm.
- **No input latency** from the presenter: it runs once per step with tens of integer operations and allocates only when a label changes.

## UI Requirements

This system fixes what each screen shows and where; exact pixel layout is validated in the UX spec (`/ux-design`).

| Information | Where | Rule |
|---|---|---|
| Door promise, danger, counts, quality word, tier, name | Door card | Formula 2; tier never colour alone. |
| Promise chips (HP, cell) and the tier word | BuffClaim header (16 px) | Exact value from the tier. |
| Granted HP and cell per hero, shortfall, lost cell | Pedestal claim chip; popup text | Always visible for all three heroes. |
| `Max HP a -> b`, locked cells, "most items in your lean" | Pedestal popup | Visible text, not hover-only for the warnings. |
| Direction emblem, ticks, or the reason and margin | Shelf header | From System 18's stored reading. |
| Item plate, tag strip, owned underline, "Matches the build / other" | Shelf card | Never odds or weights. |
| "Buff is claimed after you win this room" | Door popup, first line | |
| Skip, Confirm (named by consequence) | Action strip | Disabled but focusable with a reason. |
| Step cluster and receipt | Action strip, left | Pending steps only. |
| `statusLine` | First line of the armed item's popup | Reading order tier, promise, danger. |

New localization keys (System 7): `ui.claim.cell_lost`, `ui.claim.suggested`, `ui.claim.confirm`, `ui.offer.match`, `ui.offer.other`, `ui.door.buff_after`, `ui.door.confirm`, `ui.reason.choose_door`, `ui.reason.choose_card_or_skip`, `ui.reason.hold_drop_first`, receipt keys, and the step pip labels.

Accessibility: tier, danger, lean and promise are never carried by colour alone; the reading order equals the focus order; nothing is timed; the reveal has reduced-flash and reduced-motion forms; every disabled control stays focusable with a reason; the irreversibility texts (lost cell, cap shortfall, "you take nothing" on Skip) are visible text; a keyboard path exists to every action; pointer targets are at least 24 px where the art allows (WCAG 2.2, advisory). A section for System 28 is added to `design/accessibility-requirements.md` in the sync (the screen table, the reveal rules, the irreversibility texts, the Large conditional and a test checklist).

## Cross-References

| This GDD says | Defined in | Notes |
|---|---|---|
| The reward order and the commands | `design/gdd/run-state-and-flow.md` Rules 9, 10, 15 | Stamped with `viewGeneration`. |
| The card content, reading order, promise, suggested hero | `design/gdd/door-generation-and-tiers.md` Rules 6, 7, 10; UI Requirements | Its card pixel budget question is answered by Formula 2. |
| The offer, the reading, labels, the skip | `design/gdd/commitment-and-drop-model.md` UI Requirements | One row of cards. |
| The roster and the danger | `design/gdd/encounter-generation.md` | |
| Focus by state, lock, reset, Pointer/Nav mode | `design/gdd/input-and-focus.md` Rules 7 to 10 | |
| The popup | `design/gdd/hover-popup-and-glossary.md` Rules 3 to 5, 9 | |
| Slots, budgets, fallback | `design/gdd/localization-and-text.md` Rules 9, 10, 18 | |
| Card, shelf, state and animation forms | `design/art/art-bible.md` 7.3, 7.9 to 7.12 | Amendments listed above. |
| Accessibility | `design/accessibility-requirements.md` | New System 28 section. |
| The pillars | `design/gdd/game-concept.md` | |

## Acceptance Criteria

Evidence: **Logic** (the presenter and view models) as automated EditMode tests in `Assets/Tests/EditMode/DoorPanels/` (class `DoorPanelsTests`), named `[Scenario]_[Expected]`, gate BLOCKING; **UI** as retained screenshots in `production/qa/evidence/` for each screen touched (BLOCKING); the binder's lifetime as Play Mode evidence. Tests use test-local fixtures: `RunViewFixtures` (one view per screen and edge case), `FakeCommandSink` (returns each result code), `FakeClock`, `FakeTextService` (counts resolves, measures width), `ClaimPreviewStub`. Specific values here are provisional with the pixel budgets.

| # | Given / When / Then |
|---|---|
| 1 | **Steps shown.** Room 1 view: the cluster lists Item and Door only; room 5 with a grantable hero lists Buff, Item, Door; room 8 lists Buff, Item and no Door; an auto-skipped step never appears. |
| 2 | **Receipts.** After each confirmed step the receipt text key and args are set; a receipt over 28 characters in either language fails the build check. |
| 3 | **BuffClaim contents.** For three heroes the view model holds, for each, granted HP, granted cells, `b = B_perm + hpGranted`, the locked-cell count, the shortfall and the lost-cell flag, all from the stub dry run; the presenter performs no cap arithmetic (a spy on the stub shows exactly one `ClaimBuff` call per hero per view). |
| 4 | **Suggested hero.** The suggested hero is armed on entry and Confirm is focused; no mark field exists on the pedestal view model; the suggestion text key is present and neutral. |
| 5 | **Focus arms.** In BuffClaim, moving focus to another pedestal arms it and requests the workbench hero switch; from Confirm, Left or Up focuses the armed pedestal and leaves the arm unchanged; keys 1 to 3 change the viewed hero and never the armed one; Esc never disarms; hover never arms and a click does; Tab out of the party zone keeps the arm. |
| 6 | **Lost cell and shortfall.** A hero with 0 locked cells and a cell buff shows the lost-cell flag; `hpGranted < promised` shows the shortfall flag; Confirm stays enabled. |
| 7 | **Shelf contents.** A committed reading shows the emblem, ticks and per-card match labels; an uncommitted reading shows the reason and margin; no weight or probability field exists in the view model; one-row layout with void bays for fewer than 3 cards; focus skips void bays. |
| 8 | **Skip grammar.** Skip arms and disarms any armed card (and the reverse); Confirm text is `Skip reward` or `Take [item]`; the default focus is card 1, never Skip; nothing is pre-armed. |
| 9 | **Door contents.** Each door card view model holds the tier, promise (including the cell chip at Purple), danger numeral and word, class counts with the elite pip, the quality word, name and tier word, in that reading order; the popup's first line is `ui.door.buff_after`. |
| 10 | **Arm then Confirm.** Click arms; clicking the armed item again changes nothing; Space arms; Enter on an unarmed card does nothing; Confirm is disabled-but-focusable with the right reason key when nothing is armed; hovering never commits. |
| 11 | **Disabled reasons.** With a held item Confirm and Skip are disabled with `ui.reason.hold_drop_first`; during the reward steps the view model shows no Start control; each reason is at most 30 characters in both languages. |
| 12 | **Lock.** With the fake clock: a Confirm at `t_ViewChanged + 249` is ignored, at `+250` accepted; a press at `+100` held to `+400` never fires, and its repeats and release are swallowed; Navigate works at `+0`. |
| 13 | **Carry-through.** A held Enter that confirms BuffClaim does not arm or confirm anything on the shelf or the door that follows. |
| 14 | **Stale and rejected.** A command is stamped with the generation of the rendered view; a fake sink returning `Stale` makes the presenter re-read the view with no error state; `WrongState` and `BadIndex` keep the screen and the arm and set the reason chip; queued commands are dropped on a generation change. |
| 15 | **Transitions.** After a confirmed step the popup state is closed, the old focus target is cleared, and exactly one new target is focused on the next real anchor (an auto-skipped next step lands on the following one). |
| 16 | **Reveal.** `T_row(3) = 400` and `T_shelf = 400` under the default knobs; commit actions are refused during the reveal while Navigate, Details and the popup work from the first frame; a commit-class press finishes the reveal, is swallowed, and `ViewChanged` is sent at once (the lock starts then), while Navigate and Details do not skip it. |
| 17 | **Reduced flashes and motion.** Under each setting the reveal uses the steady-glow or fade form with identical timing, focus and lock; no luminance rise is under 100 ms. |
| 18 | **Language change.** Switching vi to en keeps the arm and focus, re-resolves every slot (resolve count equals the slot count), and a changed string is the only allocation. |
| 19 | **Text fit (build check).** Vietnamese and English strings for every card slot pass the Formula 2 widths (name two lines of at most 12 characters, danger word at most 10, quality and tier word at most 12); an over-budget string fails the build; no ellipsis or shrink exists. |
| 20 | **[VS] Large text set.** With Large selected the quality word and class labels move to the popup and the tier word, promise chip and danger numeral stay on the card; the layout is refused if the validator fails for any slot. |
| 21 | **Layout arithmetic.** Formula 1 gives 276 ≤ 280 and 294 ≤ 312 and 284 ≤ 290; Formula 2's rows sum to 131 ≤ 132; Formula 3 gives 112 and a popup space of 174 ≥ 140, and a plate cap of 107 fails. |
| 22 | **Status line.** The view model for an armed Blue door produces `statusLine` with the tier, promise and danger in that order. |
| 23 | **Window deactivation.** After `InputReset` the arm persists, the popup is closed and no command was sent on return. |
| 24 | **Presenter purity.** The presenter assembly has `noEngineReferences`, no mutable statics and no clock read other than the injected one. |
| 25 | **Screens (UI evidence).** Retained screenshots for BuffClaim (with a cap shortfall and a lost-cell case), the shelf (committed and uncommitted, 3 cards and 1 card), DoorChoice (each tier form, Purple with the cell chip) and a Vietnamese and English pass of each; a greyscale pass shows the tier, danger and lean still distinguishable. [BLOCKED: the font, the art bible amendments and the card assets] |
| 26 | **Play Mode.** The card pool is built once; subscriptions pair across two enter and exit cycles with the domain reload off; the focus ring and Pointer/Nav mode behave; a screenshot at scales 1 to 3. [BLOCKED: the UI toolkit decision] |

**Blocked criteria.** 25 and 26 wait for the font (System 7), the art bible amendments and the UGUI or UI Toolkit decision; the pedestal claim chip capacity waits for System 25 and 27's pedestal layout; the reveal photosensitivity check waits for measured luminance (art bible 4.12).

**Watched failures.** Each mutant must fail a named criterion: a toggle-off on re-clicking the armed item (10); Space confirming (10); a Skip pre-armed or focused first (8); focus not arming in BuffClaim (5); Esc disarming BuffClaim (5); a cap recomputed in the presenter (3); a stale command surfacing an error (14); a press during the lock firing on expiry (12); a held Enter passing through steps (13); an ellipsis or shrink on overflow (19); a card text change allocating on every frame (18).

## Open Questions

| Question | Owner | Resolve by |
|---|---|---|
| **UGUI or UI Toolkit** decides whether focus, navigation, text and pixel-crisp rendering are custom or inherited, and the Vietnamese diacritic rendering; a one-day spike on this card. The two sources of focus and click truth (Unity's EventSystem or toolkit navigation against System 6's own mapping) must not both stay live. | technical-director, ui-programmer | Before the architecture ADR. |
| **Engine facts NOT SOURCEABLE for 6.6:** TMP in ugui 2.6 and Vietnamese diacritics at 640 by 360; Pixel Perfect Camera with a Canvas overlay at integer scale; point-filtered 9-slice in UI Toolkit; Input System 1.20 `InputReset` and synthetic release; allocation of text updates and canvas rebuilds; the lock timestamps against `InputAction` timestamps. | ui-programmer, engine-programmer | First spike. |
| **Is the door choice non-dominant?** Purple gives the best buff and quality and its only price is danger; the UI cannot fix a dominant tier. Should the card stay neutral or show the player's own weakness against this roster (a "your build against this roster" hint, which would reuse forecast data)? | System 35, game-designer | The System 35 check; before the UX spec. |
| **Is the BuffClaim decision deep enough** with `a -> b` and cells only? A lightweight per-hero forecast or a risk marker costs scope. | game-designer | Playtest of the vertical slice. |
| **Item card extras:** should hovering or focusing a shelf card ghost the item onto the grid (a placement preview needing System 8 support), and does a shelf card show the owned-copy marker? | System 8, game-designer | UX spec. |
| **Skip payout:** Skip pays nothing in the MVP, so Take almost always wins; should a skipped offer pay a small `B_perm` (already an Open Question in System 18)? | System 18 | Balance playtest. |
| **Tray warning on Take:** should taking an item with a full tray warn, since the loss happens later at Start? | Systems 5, 25 | UX spec. |
| **Pedestal capacity:** where the per-hero claim chip and the lost-cell cross fit in the 98 px party slot without touching the empty avatar zone. Fallback decided: a three-row hero list in the right column carries the claim if the pedestal cannot. | art-director, Systems 25 and 27 | UX spec and art bible 5.10. |
| **Reveal photosensitivity:** the light swell on three cards may approach a flash threshold; reduced flashes may need to be in the MVP; measure luminance (art bible 4.12). | art-director, accessibility-specialist | Before any build ships the reveal. |
| **Screen-reader support** is unverified for the chosen toolkit; the `statusLine` is the fallback; does it satisfy the accessibility target or is it waived? | accessibility-specialist, technical-director | Accessibility file review. |
| **Art bible amendments** listed in Visual/Audio (7.3, 7.9, 7.10, 7.11, 7.12, 7.8, 7.13), the action strip width for the cluster and receipt, the frame thickness in 3.4.4 and 8.8 (not read), and the Purple subtractive cue. | art-director | Art bible follow-up. |
| **System 6 sync:** Rule 9's "arrows re-arm another pedestal" is restated as "focus on a pedestal arms it" (hover never arms); Rule 7c must list `SkipOffer` among the commands blocked while an item is held; the Esc priority and the 1 to 3 keys are unchanged. | Systems 6 and 28 | Sync after this GDD. |
| **System 18 sync:** "at most three rows" of plates becomes one row of up to three cards; its resonance and pivot audio cues are marked **[VS]**. | Systems 18 and 28 | Sync after this GDD. |
| **System 17 sync:** the UI Requirements row "with the suggested hero marked" becomes "pre-armed, with a neutral popup line and no mark"; its card pixel budget question is answered by Formula 2. | Systems 17 and 28 | Sync after this GDD. |
| **Step cluster and receipts:** an addition to the action strip not yet in the art bible or System 31; confirm the strip width. | art-director, ux-designer | UX spec. |

> *Specialists consulted for these questions: game-designer, ux-designer, ui-programmer, art-director, accessibility-specialist (2026-11-13).*
