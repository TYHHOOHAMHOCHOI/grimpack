# Combat and Title screen buttons: behaviour spec

> **Status**: Implemented 2026-11-16 (Combat and Title), verified in Play Mode; still Draft for sign-off. Presentation only: no button here creates a run, resolves combat, draws RNG or changes a reward.
> It implements the subset of `design/gdd/input-and-focus.md` (Rules 2, 4, 6), `design/gdd/payoff-presentation.md` (Rules 8, 11, 12, 19),
> `design/gdd/settings.md` (Rules 2 to 6) and `design/gdd/menus-and-screen-flow.md` (Rules 5 to 7) that the current art and scenes can show.
> Playback is a **fixture timeline** until Systems 5, 12 and 29 exist; every label below that says "playback" means that fixture.

## 1. Buttons

| Button | Where | Enabled when | Mouse | Keyboard | Effect |
|---|---|---|---|---|---|
| **Bắt đầu trận** (Start) | Action strip, Planning phase only | Planning, no item held, no menu or dialog open | click | Ctrl+Enter, or Enter/Space while it has focus | Phase becomes Playback; the strip swaps to the speed control; focus moves to the current speed segment; a 250 ms input lock starts |
| **1×, 2×, 4×** | Action strip, Playback phase | Playback, menu closed | click | 1, 2, 3 (scoped to playback) | Selects the speed. Applies from the next beat, keeping the progress of the current beat. Cancels an armed Skip. The last speed is remembered across launches |
| **Bỏ qua** (Skip) | Action strip, Playback phase | Playback, menu closed, 250 ms lock expired (a press before that is refused, not buffered) | click | K | Arms Skip: fast-forwards to the end of the fixture timeline and lands on the final view. Label reads "Đang bỏ qua" while armed. Never remembered as a speed. Never disabled: refused presses do nothing visible |
| **Menu** (the three-bar glyph) | Top strip, far right | always | click | Esc as the last fallback of the priority in 2 | Opens the pause menu: stops the playback clock, dims the stage one step (checker mask), keeps the workbench readable |
| **Tiếp tục** (Resume) | Pause menu | menu open | click | Enter on focus, or Esc | Closes the menu; the clock resumes |
| **Cài đặt** (Settings) | Pause menu, Title | menu open | click | Enter on focus | Opens the settings panel; Esc returns to the pause menu (or the Title) |
| **Bỏ cuộc chạy** (Abandon run) | Pause menu | menu open | click | Enter on focus, no hotkey | Opens the confirmation dialog |
| **Tiếp tục chơi / Bỏ cuộc** | Confirmation dialog | dialog open | click | Esc = Keep playing; **Keep playing has the initial focus** | Keep playing closes the dialog. Abandon loads the Title scene; nothing is awarded or recorded |
| **Giảm nhấp nháy, Giảm chuyển động, Skip đi thẳng đến cuối** | Settings panel | panel open | click | Enter/Space on focus | Toggles a saved preference. Default: reduced flashes on, reduced motion off, Skip-to-end off. The state is written in words (Bật / Tắt), not by colour |
| **Quay lại** (Back) | Settings panel | panel open | click | Esc | Closes the panel |
| **Chơi** | Title | always | click | Enter/Space on focus (focus starts here) | Loads the Combat scene (a stand-in for party choice and run creation, which System 5 and the Menus GDD own) |
| **Cài đặt, Thoát** | Title | always | click | as above | Settings opens the same panel. Quit leaves the game (stops Play Mode in the Editor) |

## 2. Esc priority (input-and-focus Rule 4, menus-and-screen-flow Rule 6)

Cancel a rebind (not built) → close the confirmation dialog → close the settings panel (back to the pause menu) → close the popup →
cancel a held item (it returns to where it came from) → close the pause menu → open the pause menu. One press does one step.

## 3. Rules that hold everywhere

- While the menu, a dialog or the settings panel is open, the workbench and the action strip do not receive pointer input.
- Modals are centred on the screen and the whole frame is dimmed (user decision 2026-11-16; deviates from art bible 2.3.1, which dims only the stage).
- A modal traps focus: Tab and arrows stay inside it; closing it returns focus to the control that opened it.
- No timed input anywhere; the 250 ms lock is a minimum delay, not a deadline.
- Skip is never saved. The speed is saved. The settings are saved through `GameSettings`, a stand-in for System 37.
- Every state is shown by form or text as well as colour.

## 4. Deviations and open items

- The art bible puts a single cycle chip (Tab) on the strip; `payoff-presentation.md` Rule 19 amends it to one prompt chip per segment, and
  `input-and-focus.md` binds Tab to NextZone. The Tab chip is removed; the per-segment prompt chips (1, 2, 3, K) are **not drawn yet**.
- Menu button: the top strip is 16 px tall, so its focus and hover states use the frame (1 px, 2 px when focused) instead of a ring outside with a gap.
- The fixture playback has no Detonation, so Skip goes straight to the end; "stop at each Detonation" waits for System 29.
- Settings missing: language (needs System 7), effects volume and mute (no audio yet), navigation key repeat, rebinding (System 6), text size.
- The first-launch Reduced flashes prompt (System 31) is not built.
- Window deactivation opening the menu (menus-and-screen-flow Rule 5) is not built.
- Labels are Vietnamese literals until the localization system exists.
