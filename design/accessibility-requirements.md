# Accessibility Requirements

> **Status**: Draft stub (created 2026-11-13 by the Input and Focus design session; to be completed by `/ux-design` accessibility mode and System 34)
> **Target**: WCAG 2.1 AA as the working target (to be confirmed by the project)
> **Source of the art-bible settings**: `design/art/art-bible.md` section 7.13 (text scale, tier badge, reduced flashes, reduced motion, remapping and prompts, input rules)

## Input (System 6, Input and Focus)

The MVP commits to the **Standard** tier for mouse and keyboard.

| Requirement | MVP commitment |
|---|---|
| Remapping | Every action has a primary and a secondary binding; no hard-coded keys except the reserved ones (Esc, the arrows, Tab, Enter, the left click). The rebind screen is built last and is the first thing cut; if it is cut, a waiver for the single-key shortcuts (WCAG 2.1.4 Character Key Shortcuts) is recorded here. Mouse buttons are fixed in the MVP. |
| Timing | No timed input anywhere (WCAG 2.2.1). The 250 ms input lock is a minimum delay, not a deadline. A rebind capture has no timeout. |
| Holds and chords | Nothing is hold-only; every action has a chord-free path (the default chords are shortcuts with plain alternatives). |
| Pointer gestures | The held state is reachable by click and click, by drag and by the keyboard cell cursor (WCAG 2.5.1); a drop on an invalid target is refused in place; Esc and a drag released off every grid return the item (2.5.2). Rotate has the wheel and an on-screen control as well as the right button. |
| One-handed play | The whole game is playable with the mouse alone and with the keyboard alone. |
| Key repeat | Cursor and focus movement auto-repeat with a configurable delay and interval, and a single-step option. |
| Focus | A visible focus ring at all times in Nav mode; disabled controls stay focusable with a reason; focus order is deterministic and never traps (2.1.2); a popup has a keyboard trigger and is dismissible (1.4.13). |
| Non-colour cues | Ring width, corner square and cross on blocked cells carry every state (art bible 7.11). |
| Screen reader | Not assumed; a text status line is the data source. The Unity 6.3 desktop screen-reader feature is unverified for the chosen UI toolkit. |

### Gamepad waiver

- **Scope:** gamepad and adaptive controllers are not shipped in the MVP.
- **Basis:** the user's decision of 2026-11-13.
- **Contract kept:** the action map and the focus graph are scheme-agnostic; the cell cursor is specified for keyboard and pad alike; `PromptDevice` can be Pad.
- **Exception recorded:** the project rule that every interactive element supports keyboard, mouse and gamepad.
- **Expiry:** the Vertical Slice exit gate; owner: System 34 (Accessibility).
- **Supersedes:** the earlier MVP input stub ("mouse plus the R key") recorded in `design/gdd/run-state-and-flow.md`.

## Reward-phase panels (System 28, Door and Room Panels UI)

Standard tier. Screens: BuffClaim, Reward shelf, DoorChoice.

| Screen | Non-colour carriers | Reading order (equals focus order) | Focus anchor |
|---|---|---|---|
| BuffClaim | Granted HP and cell as numerals and glyphs; shortfall and lost cell as text plus the Fog cross | Header (tier word, chips), pedestals left to right, Confirm | Confirm, with the suggested pedestal pre-armed |
| Reward shelf | Direction glyph plus tick count plus the "Matches the build / other" word; tag glyphs | Emblem, cards left to right, Skip, Confirm | First card, never Skip |
| DoorChoice | Tier word plus rank pips plus plate form; danger numeral plus word | Per card: tier, promise, danger, counts, name; cards left to right; Confirm | First card, when the reveal unlocks input |

- **Irreversibility texts** are visible text on the step, never hover-only: the lost cell, the cap shortfall, "you take nothing" on Skip, and "Buff is claimed after you win this room" on the door.
- **Reveal:** no timer; commit actions wait for the 250 ms lock only; cards are readable and focusable from the first frame. Reduced flashes: steady glow, no pulse. Reduced motion: a fade of equal duration instead of the 8 px rise. Luminance rises are at least 100 ms. Photosensitivity of the reveal is unmeasured (open).
- **Disabled controls** stay focusable and give a reason (at most 30 characters, written as the fix); a rejection has a text cue, not only a sound.
- **Status line:** the armed item's popup begins with "Armed: [tier], [promise], [danger]"; exposing it to a screen-reader layer is Vertical Slice and depends on the chosen UI toolkit.
- **Text scale:** the Large card layout is Vertical Slice; until then Large falls back to Standard on these screens unless the build validator passes every slot.
- **Pointer targets:** at least 24 px where the art allows (WCAG 2.2 AA, advisory).
- **Test checklist (qa-tester):** greyscale screenshot, deuteranopia simulation, longest Vietnamese strings, a no-mouse run, a no-keyboard run, a held Enter through all three steps.

## Payoff Presentation (System 29)

Standard tier. Playback of the recorded fight: escalating effects, speeds 1x/2x/4x/Skip, placeholder sound.

- **Photosensitivity (WCAG 2.3.1, Level A; BLOCKING for the MVP):** the bloom counts as one flash; at most 2 flash-class events (a rise of 10 percent or more of maximum relative luminance) in any rolling second and at least 500 ms apart; the Candle wash is a swell of at most about 0.08; no saturated-red flashing; rises eased over at least 6 frames; pop-ins at 2x and 4x aggregate into one swell under Reduced flashes; the capture must cover the Detonation at 1x, 2x and 4x, the Skip timelines (a cut then a Forced bloom; two Forced blooms with a cut between; a Detonation then the final sweep; a K press during an afterglow), the boss blow, a full avatar chain and the Red room, at native and scaled output, through an analyser. The area limit is expressed as a fraction of the screen as well as in pixels at 640 by 360.
- **Reduced flashes** is in the MVP, **on by default until the capture passes**, and a prompt is shown once before the first fight; switching the default off is a written condition before the go/no-go playtest. It replaces the bloom (including a Skip Forced span's bloom and the boss blow's) with a steady glow and removes particles and embers. The Settings option "Skip goes straight to the end" (default off) lets a player who cannot tolerate the forced bloom skip past Detonations; sub-bass and sudden loudness are not covered until "Reduced audio intensity" ships (open).
- **Reduced motion:** fades of equal duration replace travel, motes, shudder, numeral pop and chip rise.
- **Sound is never the only carrier:** the outcome is a banner and glyph, the actor is the turn marker or an outline, the level is the visual ladder plus the damage numeral; a muted playthrough must remain fully understandable. A Reduced audio intensity setting and an SFX slider are requested from System 37 (Vertical Slice).
- **Timing and controls:** nothing is timed; speed (1, 2, 3, which also cancel an armed Skip), Skip (K) and Pause are reachable by keyboard and mouse with a visible focus ring; the Skip state is announced as text ("Skipping", "Skipped N actions", the Detonation caption and the outcome); Pause stops the playback clock; the final damage and the outcome hold at least 1 s at any speed.
- **QA checklist:** analyser run on the Detonation at 1x/2x/4x, on the Skip timelines (cut then bloom, two blooms, a Detonation then the final sweep, K during an afterglow) and on an avatar chain, with clips retained; Reduced flashes on (no bursts, steady glow, ring steps); Reduced motion on; a muted playthrough; keyboard-only and mouse-only runs; Skip pressed during a Detonation is ignored and the final view equals full playback; the 250 ms lock at 249 and 250 ms; the longest Vietnamese result string at 4x; a greyscale capture.

## Open items

- The compliance target and the remaining sections (text scale, tier badge, reduced flashes and motion, photosensitivity verification for the MVP payoffs, colour-blind checks) belong to System 34 and `/ux-design`.
- Reduced flashes is now in the MVP (System 29, on by default until the photosensitivity capture passes); art bible 4.12 still requires verification before any build ships.
