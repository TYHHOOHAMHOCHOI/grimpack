# Planning Screen and Run Steps: UX spec

Design note: this layout is original to Grimpack; no reference video or other game informed it (user instruction, 2026-11-17).

> **Status:** Draft 2026-11-17, for user sign-off. Author: ux-designer.
> **Redesigns:** the arrangement/planning screen (`CombatSceneBuilder`, `WorkbenchLayout`) and the post-victory run steps of `RunPanelView` (claim, reward shelf, door choice). The hero pick moves to `hero-select.md`.
> **Keeps:** every behaviour in `combat-screen-buttons.md` (buttons, Esc priority, modal rules) and in System 28 (one card mould, arm then Confirm, 250 ms commit lock).
> **Touches:** Systems 5, 6, 25, 28 and 31; art-director (kit K1 to K13 and the amendment of art bible 3.6 to "real materials, light ornament"); technical-artist (the motion hooks in section 8).
> **Strings** are Vietnamese and TEMPORARY (no System 7 yet); the English gloss is in brackets. Strings use ">" and ":" because the font has no "→" or "—".
> **Units:** native px, 640 x 360, top-left origin, integer zoom. Cell C = 20.

## 1. What changes and what each removed piece did

| Function | Today | Redesign | Note |
|---|---|---|---|
| Hero pick | `HeroPick` step in the right panel | `hero-select.md` | Moved, not dropped |
| Backpack grid 4 x 4, grows to 7 x 7 | left column | P13 | K2 tray |
| Stash 8 x 8 | left column | P17, **8 x 4** | Section 3 |
| "Chọn đồ" debug item picker | workbench tab | P19, **dev builds only** | It was never a player feature, so the player loses nothing |
| Gold, corridor map (8 rooms + boss), "Lượt tới (dự kiến)" | top strip | P2 to P5 | Positions kept |
| Esc chip and menu (cog) | top strip, right | P6 and P7 | **One combined 42 x 16 hit target** (Fitts) |
| Stage: hero column, enemies in combat | middle and right | P21 to P24 | **The next room's enemies now stand in planning** (section 4) |
| "Bắt đầu trận" (Start fight), Ctrl+Enter | action strip | P27 to P29 | K4 |
| Sell / forge drawer, "Bán" / "Rèn" tabs | right edge of the stage | D1 to D6 | K1 frame and vertical K6 tabs |
| Item popup | anywhere | K9 | Reskin only. Placement rules are unchanged (art bible 7.9). |
| Big right panel frame (322 x 314) for the run steps | x 315, y 19 | **Removed** | Each job moves elsewhere. **Host the steps:** an overlay on the enemy zone (R). **Detail box:** R6. **Title and step tracker:** R3. **Refusal message:** R7. **Confirm and Skip:** the action strip (R8 to R11, art bible 7.10). |
| New: innate ability plate | none | P18 | Uses space freed by the smaller stash |
| New: room plate | none | P25 | Room number and danger, for planning at "Vừa" |
| New: empty-stash hint | none | inside P17 | For the empty start |

## 2. Planning state layout

### 2.1 Wireframe (1 character is about 8 px, 1 row about 12 px; diacritics dropped)

```
+--------------------------------------------------------------------------------+
|(o)5  [][][][][][][][]B   Luot toi (du kien): [1][xA][1][xA][1]        [Esc][*] |  P1-P7
+---------------------++------------------+---------------------------------+----+
|[1]v Ky Si  hp110 s22||                  | [ Phong 3/8 . Nguy hiem 2: Vua ]|Ban |
|+---------+          ||                  |                                 |Ren |
|| backpack|          ||      (hero)      |      [!3]        [!5]           +----+
|| 4x4  K2 |          ||   ===pedestal=== |     (enemy)     (enemy)  <- next room,
|+---------+          ||    [HP block]    |      [hp]        [hp]      planning dim
|------ divider ------||    [stat row]    |                                      |
|[Kho do]             ||                  |                                      |
|+-------------------+||                  |                                      |
|| stash 8x4      K2 |||                  |                                      |
|| "Do thuong se vao ||| hero column P22  | enemy zone P24                       |
||  day"             |||                  |                                      |
|+-------------------+||                  |                                      |
|+-------------------+||                  |                                      |
|| Noi tai: ...  P18 ||+------------------+--------------------------------------+
|| (3 lines)         ||| reason text            [Ctrl+Enter][   Bat dau tran   ] |
+---------------------++---------------------------------------------------------+
```

### 2.2 Table. Rows with n depend on the hero grid size (n = 4 to 7).

| ID | Region or control | x | y | w | h | Kit | Content and rules |
|---|---|---|---|---|---|---|---|
| P1 | Top strip band | 0 | 0 | 640 | 16 | **K14 strip band (new)** | Tiled wood and iron band, ink-dark |
| P2 | Gold counter | 2 | 1 | 40 | 14 | K11 plate + coin | Coin 10 x 10 at (6, 3), 8 px numeral at (19, 4) |
| P3 | Corridor map | 48 | 0 | 112 | 16 | existing nodes | 8 nodes (10 x 11, 12 px pitch) and the boss (12 x 12) at +100. Hover or focus opens the history popup (K9, 190 x 120, at (48, 18)). |
| P4 | Turn-order label | 181 | 2 | 106 | 12 | text | "Lượt tới (dự kiến):" (Next turns, projected), Fog |
| P5 | Turn-order chips | 291 | 1 | up to 296 | 14 | existing chips | End at x 586 or earlier |
| P6 | Esc chip | 594 | 1 | 24 | 14 | **K15 key chip (new)** | "Esc". **Together with P7 it forms one 42 x 16 hit target** that opens the pause menu. |
| P7 | Menu button | 620 | 0 | 16 | 16 | K5 (mini) | Cog glyph 8 x 8 |
| P8 | Workbench panel | 0 | 16 | 174 | 344 | K1 | |
| P9 | Join chip | 7 | 21 | 14 | 12 | K15 (small) | Join numeral "1" and chevron |
| P10 | Hero name | 34 | 21 | 80 | 12 | text | Bone |
| P11 | HP mini | 118 | 23 | 26 | 8 | 8 px heart + 8 px numeral | Current max HP |
| P12 | Speed mini | 148 | 23 | 20 | 8 | 8 px speed glyph + numeral | |
| P13 | Backpack tray | 5 | 34 | 4 + 20n | 4 + 20n | K2 | Cells at (7, 36). n = 4 gives 84 x 84. Locked cells use the K2 locked cell bed. |
| P14 | Divider | 5 | 39 + 20n | 164 | 3 | K13 | y 119 when n = 4 |
| P15 | Lower tab "Kho đồ" (Stash) | 7 | 44 + 20n | 54 | 14 | K6 (selected) | Slot reserved at x 63, w 70 for "Tổng quan" (Overview, art bible 7.6, not built) |
| P16 | (reserved) | | | | | | |
| P17 | Stash tray | 5 | 60 + 20n | 164 | 84 | K2 | 8 x 4 cells at (7, 62 + 20n). The tray spans y 140 to 224 when n = 4, and y 200 to 284 when n = 7. While the stash is empty, a centred Fog line reads "Đồ thưởng sẽ vào đây" (Rewards go here). |
| P18 | Innate plate | 5 | 288 | 164 | 66 | K2 | **Fixed position.** "Nội tại:" in Fog, then the name in Bone (one line), then at most 3 Body lines of about 26 characters. Focusable; the popup shows the full text. |
| P19 | "Chọn đồ" debug tab | 63 | 44 + 20n | 58 | 14 | K6 | **Absent from release builds.** In development builds (`Debug.isDebugBuild`; the `DEVELOPMENT_BUILD` define is deprecated in 6.6) it is hidden until **F9** toggles it. Hidden whenever a run is active, unless F9 is on. |
| P20 | Stage panel | 174 | 16 | 466 | 344 | K1 | Frames the stage and the action strip |
| P21 | Stage backdrop | 177 | 19 | 460 | 314 | K10 planning stage backdrop | Held dim in planning (2.3.2) |
| P22 | Hero column | 177 | 19 | 136 | 314 | none | Existing HeroSlot positions: pedestal K8 (small, 52 x 12), HP block, stat row. Unchanged. |
| P23 | Column divider | 313 | 19 | 3 | 314 | K13 (vertical) | |
| P24 | Enemy zone | 316 | 19 | 321 | 314 | none | **The next room's roster stands in its combat slots** (EnemySlotView 1 to 6, unchanged positions) at planning dim. HP numbers and intent tabs stay at full Bone and are focusable. |
| P25 | Room plate | 324 | 24 | 200 | 18 | K3 | Example: "Phòng 3/8 · Nguy hiểm 2: Vừa" (Room 3/8 · Danger 2: Medium). Shown in planning only. In the boss room: "Phòng boss". |
| P26 | Action strip band | 177 | 333 | 460 | 24 | K14 (24 px variant) | |
| P27 | Reason text | 185 | 339 | 220 | 12 | text | Fog. Why Start is off, for example while an item is held (unchanged). |
| P28 | Start key chip | 415 | 338 | 66 | 14 | K15 | "Ctrl+Enter" |
| P29 | Start button | 485 | 335 | 148 | 20 | K4 | "Bắt đầu trận" (Start fight). Enabled rules are unchanged (combat-screen-buttons 1). |

### 2.3 Sell / forge drawer (clipped by the stage at x 637 or less)

| ID | Element | x | y | w | h | Kit | Rules |
|---|---|---|---|---|---|---|---|
| D1 | Drawer panel | open 469 / closed 637 | 23 | 168 | 160 | K1 | Spring open and close as today. With Reduced motion it snaps. |
| D2 | "Bán" (Sell) tab | panel x - 14 | 29 | 14 | 28 | **K6, vertical variant** | Arrow 5 x 7 and scale glyph 8 x 8. Hover or focus shows a chip popup "Bán đồ lấy vàng" (Sell items for gold). |
| D3 | "Rèn" (Forge) tab | panel x - 14 | 61 | 14 | 28 | K6, vertical | Arrow and anvil glyph. Chip popup "Rèn đồ (tốn vàng)" (Upgrade items, costs gold). |
| D4 | Drawer header | +8 | +10 | 152 | 16 | K3 | "Bán đồ" / "Rèn đồ" |
| D5 | Drop zone | +12 | +40 | 144 | 92 | K2 + state edge | States: Idle, Ready, Over and Invalid (ForgeZoneState / SellZone). The Invalid state is a Fog outline plus the cross glyph, never red. |
| D6 | Hint and price row | +12 | +136 | 144 | 14 | text, coin, numeral | Hint on the left ("Kéo đồ vào đây", Drag an item here). Price or cost on the right, as coin + 8 px numeral. |

Selecting the open mode's tab closes the drawer. Bringing a held item over a tab opens that mode (unchanged). The tabs keep their position in every state, run steps included.

## 3. Stash size (proposal: 8 x 4)

- Intake over a run is at most 1 reward per room, so 8 rooms give 8 items (boss loot not counted). Today's largest footprints cover 3 cells (1 x 3, L3), so the worst case is about 24 cells. 8 x 4 = 32 cells holds that, plus items the player moves out of the backpack.
- It frees 80 px of column height for P18, so the innate ability, which drives item choices (for example the Alchemist's detonation reaction), stays visible during planning.
- At n = 7 the column still fits: the stash ends at y 284 and P18 starts at y 288.
- **Tuning knob:** stash rows 3 to 6 (`WorkbenchLayout.Stash` becomes width 8 and a separate row count). Rows 6 and up push P18 off the column at n = 7, so moving P18 must then be revisited.

## 4. The right side in planning: enemy preview, not a panel

- At difficulty "Vừa", planning needs the opponent visible (Pillar 4: everything the player decides on is visible before the fight). The chosen door already fixes the roster, so the enemies stand where they will fight, with HP and intent tabs. The turn-order chips in P5 already reference them.
- **Continuity:** Start causes no jump, because the slots are the combat slots. Only the light changes, rising from planning dim to the combat key (art bible 2.6).
- Room 1 (the entrance) shows its encounter the same way.

## 5. Run-step overlay (after a victory: Claim > Reward > Doors)

### 5.1 Wireframe of the door step

```
|      (hero column, dimmed one step)  |[ Chon cua vao phong 4    Buff > Do > Cua ]| R3
|                                      | +--------+  +--------+  +--------+        |
|                                      | | .. pips|  | ...    |  | ....   |        | R4
|                                      | | Xanh la|  | Xanh d.|  | Vang   |        |
|                                      | | +12 hp |  | +16 hp |  | +20 +1 |        |
|                                      | | Nguy 2 |  | Nguy 3 |  | Nguy 4 |        |
|                                      | +--------+  +--------+  +--[tag]-+        |
|                                      | +--------------------------------------+  |
|                                      | | detail of the hovered or armed card  |  | R6
|                                      | +--------------------------------------+  |
|                                      |  x  message line                          | R7
| reason            [K][Bo qua]     [Enter][        Vao cua         ]              | R8-R11
```

### 5.2 Table

| ID | Element | x | y | w | h | Kit | Rules |
|---|---|---|---|---|---|---|---|
| R1 | Stage dim | 177 | 19 | 460 | 314 | existing dim mask | One INK step. The hero column stays readable, and the workbench is **not** dimmed (it stays usable, art bible 7.10). |
| R2 | Result banner | 287 | 110 | 240 | 40 | K12 (victory) | "Thắng phòng 3" (Room 3 won), Display 24. Held about 900 ms, or until any input. It comes before R3. The defeat variant leads to the end summary (out of scope). |
| R3 | Step header | 323 | 25 | 296 | 20 | K3 | Title on the left in Body ("Phần thưởng cửa", "Chọn 1 món", "Chọn cửa vào phòng 4"). Tracker on the right: "Buff > Đồ > Cửa", with **the current step in Bone with a 1 px Bone underline** and the others in Fog. Room 1 has no Claim step, so its tracker reads "Đồ > Cửa". |
| R4 | Card i (doors, reward items) | 325 / 427 / 529 | 51 | 90 | 140 | K7, tier variant (6 tiers + disabled) | 12 px gaps. The card anatomy is unchanged from `RunPanelView` (pips, tier word, buff rows, danger and word, size counts, item quality, curse tag). Reward cards show the icon at true footprint, scaled to fit 76 x 80 or less, and the name. |
| R5 | Claim card | 325 | 51 | 294 | 140 | K7, tier variant (9-sliced) | Pips, "Phần thưởng cửa <tier>", glyph rows "+N Máu tối đa" / "+1 ô balo", curse tag and line |
| R6 | Detail plate | 325 | 199 | 294 | 98 | K9 | The hovered card's prose, else the armed card's, else the step hint. At most 6 Body lines of about 46 characters. Fixed position, so it never moves. |
| R7 | Message line | 325 | 303 | 294 | 12 | 5 x 5 cross + text | Refusals in Bone (not red text; art bible 4.9). Examples: "Kho đồ đầy: bán bớt hoặc xếp vào balo" (Stash full: sell or move items to the backpack), "Neo cần 2 ô trống liền nhau trong balo". |
| R8 | Skip key chip | 337 | 338 | 12 | 14 | K15 | "K". Reward step only. |
| R9 | Skip button | 353 | 335 | 76 | 20 | K5 | "Bỏ qua" (Skip). Reward step only. |
| R10 | Confirm key chip | 435 | 338 | 46 | 14 | K15 | "Enter" |
| R11 | Confirm button | 485 | 335 | 148 | 20 | K4 | Label per step: "Nhận" (Claim), "Lấy" (Take), "Vào cửa" (Enter door). Disabled until a card is armed (the Claim step is always armed). |

While R is shown: P25, P27, P28 and P29 are hidden. The drawer (D) and the workbench (P8 to P19) stay operable, and a held item stays held. Arming a door previews its tier on the next corridor node (unchanged). After the door Confirm, R exits, the corridor node fills, and the next room's enemies enter the enemy zone, which returns the screen to the planning state.

**Overlap:** the open drawer (x 469 to 637, y 23 to 183) covers cards 2 and 3. That is accepted: the drawer is a deliberate, temporary action, an armed card stays armed under it, and Confirm stays visible in the action strip.

## 6. States and interaction

| Element | Hover | Focus | Selected / armed | Pressed | Disabled | Invalid |
|---|---|---|---|---|---|---|
| Plates, cells | 1 px Candle ring, 1 px gap | 2 px ring, z-lifted | n/a; Held per art bible 7.11 (lift 2 px, shadow, 2 px ring) | n/a | never (the tier edge is never changed) | ghost: Fog outline, desaturated, 5 x 5 cross per blocking cell |
| Cards (R4, R5) | 1 px ring; R6 shows its prose | 2 px ring; R6 at once | ring + 4 x 4 corner square; R11 enabled | n/a | K7 disabled variant | n/a |
| K4, K5 buttons | 1 px ring | 2 px ring + prompt chip | n/a | Candle fill, INK-0 text, 50 ms | INK-3 frame, still focusable; P27 states why | n/a |
| K6 tabs | 1 px ring | 2 px ring | selected art + corner square | n/a | n/a | n/a |
| Drawer drop zone | n/a | n/a | Over: lit state edge | n/a | "Chỉ rèn khi xếp đồ" (Forge only while arranging) | Fog outline + cross; the hint states the reason ("Thiếu 4 vàng", 4 gold short) |

**Focus zones (Tab / Shift+Tab, PageDown / PageUp).** In planning: Backpack > Stash > Innate plate > Enemy zone (intent tabs, HP blocks) > Drawer tabs > Action strip > Top strip (map, order chips, menu). During R, the cards are the first zone and initial focus goes to card 1, unarmed. A closed popup or panel returns focus to its opener (System 6).

**Esc priority** is unchanged from `combat-screen-buttons.md` section 2, with "disarm an armed card" inserted before "close the pause menu" (menus-and-screen-flow Rule 6). Esc never skips a reward.

## 7. Input map additions (everything else is in input-and-focus Rule 2)

| Action | Mouse | Keyboard | Gamepad (pad-ready; MVP waiver) |
|---|---|---|---|
| Arm a card | click | Left / Right, then Space | D-pad, then A |
| Confirm a step | R11 | Enter | A on R11, or A on the armed card |
| Skip the reward | R9 | K | X on the shelf |
| Open sell / forge | D2 / D3, or a held item over a tab | Focus the tab, then Enter | A on the tab |
| Start the fight | P29 | Ctrl+Enter | A on the focused P29 (no pad shortcut until System 6 assigns one) |
| Debug picker | n/a | F9 (dev builds only) | none |

## 8. Motion hooks (intent only; the technical-artist owns the timings)

| Hook | Moment | Intent | Reduced-motion fallback |
|---|---|---|---|
| PL-01 screen-enter | From hero-select HS-11 | The workbench rises into place, the stage fades up from ink, the hero arrives on the pedestal | Cross-fade, at least 250 ms |
| PL-02 starting-items-land | After PL-01 | The 3 starting plates set into the grid one by one (art bible "snap"), so the player sees what they own | Plates appear |
| PL-03 enemies-enter | Planning entry, and after the door Confirm | The next room's enemies step into their slots, at planning dim | Fade |
| PL-04 item-lift | Pick up (click or drag) | Art bible 7.12 drag lift | Unchanged (not motion-heavy) |
| PL-05 item-lands-grid | A legal place | Snap and thunk; the tier edge pulses | Snap, no thunk |
| PL-06 item-invalid | An illegal release | Returns to its origin and shudders | Return only |
| PL-07 grid-grows | +1 cell or a larger grid | New cells open; the stash slides down to its new y | Instant re-layout |
| PL-08 gold-change | Sell, reward or forge | The coin acknowledges; the number changes | Number changes |
| PL-09 drawer-open / close | D2 / D3 | Spring with a small bounce (today's behaviour) | Snap |
| PL-10 sell-commit | An item dropped on Sell | The item is consumed and the value travels to P2 | Item vanishes; the number changes |
| PL-11 forge-strike | An upgrade | The anvil strikes (today's behaviour); the upgraded plate pulses its new tier edge | Static sprite swap |
| PL-12 tab-switch | Bán / Rèn, or Kho đồ / Overview | Content swaps under a fixed frame | Instant |
| PL-13 start-fight | Start accepted | Planning affordances fade, P25 leaves, enemies and the hero lift to the combat key (2.6) | Fade, at least 250 ms |
| PL-14 victory-banner | A room is won | K12 settles over the stage | Fade |
| PL-15 steps-enter | After PL-14 | The stage dims, R3 drops in, the cards rise one at a time (art bible 7.12 door-card reveal) | Fades, same total |
| PL-16 card-hover / arm | R4 | Ring, then ring with corner square | Unchanged |
| PL-17 claim-commit | "Nhận" | The HP block grows; a new backpack cell opens (PL-07) | Numbers change |
| PL-18 reward-to-stash | "Lấy" | The item travels from its card into its stash slot ("item lands in stash") | Item appears in the stash |
| PL-19 reward-skip | "Bỏ qua" | The cards fall to ink | Fade |
| PL-20 door-commit | "Vào cửa" | The other two cards fall to ink (2.2), the chosen door's corridor node fills, R exits, then PL-03 | Fades |
| PL-21 refused | A refused Confirm or Take | R7 appears and R11 shudders once | R7 appears |

Ambient motion stays within two elements per screen (2.2 rule 3): the hero and enemy idles count as "things that matter", and the door cards breathe at 0.5 Hz or slower with 6% amplitude or less.

## 9. Eyes-first order

- **Planning:** (1) the backpack grid, the brightest region (2.3.2); (2) the enemy intent tabs and HP numbers; (3) the Start button; (4) gold, the corridor and the turn chips; (5) the innate plate and the stash; (6) the drawer tabs.
- **Run steps:** (1) the cards, the lit doorways (2.3.3); (2) R6, the detail plate; (3) R11, Confirm; (4) the corridor preview node.
- **Budgets:** planning has 12% or less lit area and an APL of 0.08 or less. The door step has 12% or less and 0.07 or less. Chrome is capped at INK-4, and ornament stays in the corners.

## 10. Edge cases

| Case | Behaviour |
|---|---|
| The stash is full and the player takes a reward | Refused: R7 says why (PL-21), the offer stays, and Skip, the drawer and the workbench stay usable. |
| An item is held when R appears | It stays held, and Esc returns it to its origin. Cards are not drop targets, so releasing over one returns the item. |
| The drawer is open at Start | It closes as Start is accepted (PL-09), and no input is lost. |
| The grid grows during the Claim step | PL-07 runs at once, and the stash moves down. P18 never moves. |
| The innate text is longer than 3 lines | It ends in "...", and the full text is in the P18 popup. |
| The boss room is next | R3 reads "Phòng boss", the corridor boss node takes the marker, and P25 reads "Phòng boss". |
| 1 to 6 enemies | The existing slot rules apply. P25 never overlaps a slot, because the slots start below y 42. If a slot does, P25 drops to the action-strip reason line. |
| The first-launch Reduced flashes prompt is pending | It opens as a centred modal on PL-01 completion, before any input reaches the workbench. |
| A release build | P19 does not exist, and F9 does nothing. |

## 11. Accessibility checklist

- [x] Keyboard only: full loop with zones, arrows, Space, Enter, Ctrl+Enter, K and Esc. Nothing is hover-only (R6 also follows focus; P18 and the enemy tabs are focusable).
- [x] Gamepad: pad-ready mapping (section 7); delivery under the MVP waiver.
- [x] Text is at least 12 px line box (digits at least 8 px). No "→" or "—".
- [x] Not colour alone: the step tracker uses an underline, armed uses the corner square, invalid uses the cross, tiers use form + pips + words, and refusals are text.
- [x] No flashing: door breathing is 0.5 Hz or slower, and rises take at least 100 ms (4.12).
- [x] Subtitles: no dialogue.
- [x] Scales: integer zoom 2x, 3x, 4x and 6x; K components are 9-sliced with the corners unstretched.

## 12. Acceptance criteria

1. Captures at 1x of the planning state (n = 4 and n = 7), the door step, the reward step, the claim step and the open drawer match every rect in sections 2.2, 2.3 and 5.2 to the pixel.
2. A new run starts with exactly the hero's 3 starting items in the backpack and 0 in the stash. The stash is 8 x 4 and shows "Đồ thưởng sẽ vào đây" until it holds an item.
3. The planning screen has no hero-pick step, and no right-hand panel frame exists outside the run steps.
4. In a release build "Chọn đồ" is absent. In a development build it is hidden until F9 is pressed, and F9 toggles it.
5. In planning, the next room's enemies, their HP and their intent tabs are visible. After Start, no enemy slot changes position (diff of the slot rects = 0).
6. Each run step shows R3, its cards or claim card, R6, and R11 with the correct label. Start, its key chip and P25 are hidden.
7. While a run step is shown, the player can pick up and place items in the backpack and stash and can sell or forge through the drawer.
8. Taking a reward with the stash full is refused with the R7 message. The offer remains, and Skip still works.
9. The full loop (arrange, Start, win, claim, take or skip, choose a door, the next room's planning) can be completed with the keyboard only, and again with the mouse only.
10. A greyscale capture shows the current step (underline), the armed card (corner square) and the selected drawer tab without colour.
11. No string contains "→" or "—". No text uses a cut below 12 px (digits excepted). No Vietnamese glyph renders as tofu.
12. With Reduced motion on, every hook in section 8 uses its fallback. The drawer snaps, and no shudder or rise plays.
13. Esc follows the priority in section 6. It never skips a reward, and it disarms an armed card before the menu opens.
14. Clicking anywhere in the Esc chip or the cog (594 to 636, 0 to 16) opens the pause menu.
15. At 2x, 3x and 4x, no K component shows seams or stretched corner ornaments. The lit-area and APL budgets in section 9 pass the 4.10 check, or a squint-review sign-off by the art-director until that tool exists.

## 13. Open questions

| # | Question | Options | Recommendation |
|---|---|---|---|
| Q1 | Stash size | 8 x 4 / 8 x 3 / 8 x 6 / keep 8 x 8 | **8 x 4** (section 3) |
| Q2 | Right side in planning | Enemy preview + room plate / a slim hero info panel / an empty stage | **Enemy preview**: it serves planning at "Vừa" and removes the empty panel. |
| Q3 | Where the innate ability lives | Plate at the bottom of the workbench (P18) / a relic chip next to the hero on stage | **P18**: it is readable without hover and stays near the items it combines with. |
| Q4 | Run steps over the stage | Overlay on the enemy zone, with the workbench live / a centred modal with the whole frame dimmed | **Overlay**: art bible 7.10 keeps the workbench usable during door choice. The centred modal stays reserved for pause and confirm dialogs. |
