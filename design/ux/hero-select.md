# Hero Select ("Chọn nhân vật"): UX spec

Design note: this layout is original to Grimpack; no reference video or other game informed it (user instruction, 2026-11-17).

> **Status:** Draft 2026-11-17, for user sign-off. Author: ux-designer.
> **Replaces:** the `HeroPick` step of `RunPanelView` (the large right-hand panel of the planning screen, which the user found ugly).
> **Touches:** System 5 (run creation, `PickParty`), System 6 (input and focus), System 31 (menus and screen flow), art-director (component kit K1 to K13, and the amendment of art bible 3.6 to "real materials, light ornament"), technical-artist (the motion spec for the hooks in section 7).
> **Strings** are Vietnamese and TEMPORARY until System 7 exists. The English gloss is in brackets. The pixel font has no "→" or "—", so strings use ">" and ":".
> **Units:** native px on the 640 x 360 canvas, x to the right and y down, top-left origin. Integer zoom only.

## 1. Purpose and flow

`Title "Chơi"` > **Hero select** > `Planning screen, room 1` (see `planning-screen.md`).

- One full-screen step that answers one question: "who do I bring?" No workbench, stage, top strip or drawer is visible.
- Everything needed to choose is on screen **without hover**: name, role, HP, speed, starting shield, the innate ability, the 3 starting items as plates, and the core (signature) item. Hover and focus add only item details (the existing item popup).
- Entry points: Title "Chơi" (Play), and Restart from the end summary (menus-and-screen-flow Rule 4: the previous hero is preselected).
- Exits: **Confirm** goes to planning with the chosen hero, its 3 items in the backpack and an empty stash. **Esc / "Quay lại"** (Back) goes to the Title. Nothing is committed before Confirm.

## 2. Layout

### 2.1 Wireframe (1 character is about 8 px, 1 row about 12 px; diacritics dropped for alignment)

```
+------------------------------------------------------------------------------+
|                        [====  Chon nhan vat  ====]                     H2    |
|                                       +--------------------------------------+
|                                       | Do Don / Suy Yeu                H10  |
|                                       | Mau 110   Toc do 22   Giap 8    H11  |
|                                       |------------------------------------- |
|  [<]           .------.          [>]  | Noi tai: Loi The Thiet Giap     H13  |
|   H4           | HERO |           H5  | Dau tran: +6 Giap, gay 1 Suy Yeu     |
|                | 96px |               | len moi ke dich.                H14  |
|                '------'   H6          |------------------------------------- |
|           ====[ pedestal ]====  H7    | Do khoi dau        Do chu luc   H18  |
|                                       | +--------------+   Khien Gai    H19  |
|       [==  Ky Si Hac Quang  ==]  H8   | |[|][||][ ]    |   Giap +4,          |
|                                       | |=====         |   gai phan don H20  |
|                                       | +--------------+ H17                 |
|                                       +--------------------------------------+
|                          +-------+ +-------+                                 |
|                          |1 face | |2 face |   H22 (2 now, up to 4)          |
|                          +-------+ +-------+                                 |
|                                                                              |
| [Esc][ Quay lai ]                                  [Enter][ Vao ham nguc  ]  |
+------------------------------------------------------------------------------+
```

### 2.2 Region and control table

| ID | Region or control | x | y | w | h | Kit | Content and rules |
|---|---|---|---|---|---|---|---|
| H1 | Backdrop | 0 | 0 | 640 | 360 | K10 hero-select backdrop | Authored at 960 x 540 bleed (art bible 8.8). Dark stone hall, one warm lantern pool behind H7, everything else ink (2.3.1). |
| H2 | Title plate | 232 | 6 | 176 | 22 | K3 | "Chọn nhân vật" (Choose a hero), Heading 16, Bone, centred. |
| H3 | Showcase zone | 12 | 34 | 296 | 208 | none | Sits on the backdrop. No frame: the hero stands in the room, not in a box. |
| H4 | Previous arrow | 40 | 124 | 16 | 24 | K5 (mini) | "<" glyph. Selects the previous hero, wrapping. Hidden when only 1 hero is available. |
| H5 | Next arrow | 264 | 124 | 16 | 24 | K5 (mini) | ">" glyph. Selects the next hero, wrapping. |
| H6 | Hero showcase sprite | 112 | 88 | 96 | 96 | hero showcase art (section 9, Q3) | Selected hero in idle. Feet on y 184, bottom-centred on H7. |
| H7 | Pedestal | 100 | 178 | 120 | 24 | K8 (large) | Carries the Candle pool (the only warm light on screen). |
| H8 | Name plate | 64 | 212 | 192 | 22 | K3 | Hero name, Heading 16, Bone, centred. Writer budget: 20 characters or fewer. |
| H9 | Info panel | 316 | 34 | 312 | 216 | K1 | Inner padding 8, so content spans x 324 to 620. |
| H10 | Role line | 324 | 42 | 296 | 12 | text | `roleVi`, Secondary (Fog). Example: "Đỡ Đòn / Suy Yếu" (Tank / Weaken). |
| H11 | Stat row | 324 | 58 | 296 | 16 | 12 px glyph + Body + 12 px numeral | Three groups with 12 px gaps: "Máu 110" (HP), "Tốc độ 22" (Speed), "Giáp 8" (Shield; shown only when `initialShield` > 0). The word and the glyph are both shown, never the glyph alone. |
| H12 | Divider | 324 | 80 | 296 | 3 | K13 | |
| H13 | Innate heading | 324 | 88 | 296 | 12 | text | "Nội tại:" (Innate:) in Fog, then the innate name in Bone. Focusable: the popup shows the full text. |
| H14 | Innate text | 324 | 102 | 296 | 42 | text | `innateItem.descVi`, Body, at most 3 lines on the 14 px pitch (about 49 characters per line). |
| H15 | Divider | 324 | 148 | 296 | 3 | K13 | |
| H16 | Kit heading | 324 | 156 | 140 | 12 | text | "Đồ khởi đầu" (Starting items), Fog. |
| H17 | Kit tray | 324 | 170 | 140 | 72 | K2 | The 3 starting items as plates at **true footprint, C = 20**. Plate origin (328, 174), top-aligned, 6 px gaps, core item first. Under the core plate, a 2 px Bone **core marker bar** at y 236, as wide as the plate (K16). Inner width 132 fits any 3 of today's footprints (the widest case, L3 + 2x1 + 2x1 + 12 px of gaps, is exactly 132). |
| H18 | Core heading | 476 | 156 | 144 | 12 | text | "Đồ chủ lực" (Core item), Fog. |
| H19 | Core name | 476 | 172 | 144 | 12 | text | Name of `signatureItemId`, Bone. |
| H20 | Core summary | 476 | 188 | 144 | 42 | text | The item's one-line effect summary (the same text the reward card uses), Body, at most 3 lines of about 23 characters. |
| H21 | Roster row | 180 | 258 | 280 | 60 | none | Four slots on a 72 px pitch (card 64 + gap 8), centred on x = 320. |
| H22 | Hero card i | x0 + 72i | 258 | 64 | 60 | K7 hero card (tier-neutral) | x0 = 320 - (64n + 8(n - 1)) / 2. With n = 2 the cards are at x 252 and 324. With n = 3, at 216, 288 and 360. With n = 4, at 180, 252, 324 and 396. |
| H22a | Card numeral chip | card + 3 | card + 3 | 10 | 10 | K15 (small) | 8 px digit 1 to 4: the direct-select key. |
| H22b | Card portrait | card + 8 | card + 6 | 48 | 48 | existing portrait | |
| H23 | Esc key chip | 12 | 334 | 28 | 14 | K15 | "Esc", Fog. Names the key. Clicking it works the same as H24. |
| H24 | Back button | 44 | 330 | 88 | 22 | K5 | "Quay lại" (Back). |
| H25 | Enter key chip | 432 | 334 | 48 | 14 | K15 | "Enter", Fog. |
| H26 | Confirm button | 484 | 330 | 144 | 22 | K4 | "Vào hầm ngục" (Enter the dungeon). The label names the outcome (Q4 in section 9). |
| H27 | Item popup | see rule | | 152 | 140 or less | K9 | Opens to the **left** of the info panel: x = 160, y = top of the plate clamped to 34..(356 - h). Placed once and never moves while open (art bible 7.9). |

**Vertical budget:** H2 is 6 to 28, H3 and H9 are 34 to 250, H21 is 258 to 318, the button row is 330 to 352. There is 30% spare line height only in H14 (2-line texts in a 3-line box). Large text (art bible 7.13) needs a re-layout pass and is not claimed here.

## 3. Content per hero (data mapping)

| Field on screen | Source (`heroes_mvp.json`) | Today's values |
|---|---|---|
| Name (H8) | `nameVi` | Kỵ Sĩ Hắc Quang / Dược Sư Dịch Hạch |
| Role (H10) | `roleVi` | Đỡ Đòn / Suy Yếu ; Gây Độc / Kích Nổ |
| HP, speed, shield (H11) | `baseMaxHp`, `baseSpeed`, `initialShield` | 110, 22, 8 ; 86, 26, 0 (no shield group) |
| Innate (H13, H14) | `innateItem.nameVi`, `descVi` | Lời Thề Thiết Giáp ; Mầm Dịch |
| Kit (H17) | `startingItems` | **must be 3 items** (user decision 2026-11-17). Data today lists 4 and 5, so game-designer must trim the lists (Q1). |
| Core (H18 to H20) | `signatureItemId` | Khiên Gai (Spiked Buckler) ; Xúc Tác Dịch Hạch (Plague Catalyst) |
| Card portrait (H22b) | the existing portrait sprite | |

## 4. States and interaction

**Selection model: a radio group.** Exactly one hero is selected at all times. The showcase (H6 to H8) and the info panel always show the selected hero. Keyboard focus in the roster **moves the selection with it**. **Mouse hover never changes the selection**, because the pointer path from the roster to Confirm crosses other cards and the showcase must not flicker.

| Element | Default | Hover | Focus | Selected | Pressed | Disabled or locked |
|---|---|---|---|---|---|---|
| Hero card (K7) | portrait, numeral chip | 1 px Candle ring, 1 px gap | 2 px Candle ring, 1 px gap, z-lifted | ring + 4 x 4 Candle corner square at the ring's top-left; pedestal pool lit | n/a (a click selects) | Locked variant (only if Q2 = show): INK silhouette, lock glyph 8 x 8, still focusable, popup "Chưa mở khóa" (Not unlocked) |
| Arrow (K5 mini) | INK-4 frame, Bone glyph | 1 px ring | 2 px ring | n/a | Candle fill, 50 ms | hidden when n = 1 |
| Kit plate | plate at rest | 1 px ring; popup after 300 ms | 2 px ring; popup at once | n/a | n/a | never (the plates are read-only, with no pick-up) |
| Innate heading (H13) | text | 1 px ring | 2 px ring; popup with the full text | n/a | n/a | n/a |
| Back (K5) and Confirm (K4) | K4: Candle 2 px frame, Bone text | ring | ring + prompt chip | n/a | Candle fill, INK-0 text, 50 ms | Confirm is disabled only when the selected hero is locked. Presses during the 250 ms input lock are ignored, not shown as disabled. |

**Initial state:** the selected hero is slot 1, or the hero picked last in this session (Restart). Focus is on that hero's card, so Enter confirms it. Confirm is enabled. Input is locked for 250 ms after entry (System 6), so an Enter carried over from the Title cannot skip the screen.

**Invalid:** Confirm on a locked hero is refused. The locked card shudders 2 px (hook HS-13) and the popup says why. Nothing else happens.

## 5. Input map

| Action | Mouse | Keyboard | Gamepad (pad-ready; MVP waiver per input-and-focus) |
|---|---|---|---|
| Select a hero | click its card, or click H4 / H5 | Left / Right in the roster (wraps); 1 to 4; Q / E | D-pad left / right; LB / RB from any zone |
| Confirm | click H26; double-click a card (a shortcut, never the only path) | Enter on the focused card or on Confirm | A on the focused card or on Confirm |
| Item and innate details | hover 300 ms | Tab to the kit tray, then Left / Right; F pages the popup | RB to the kit tray, D-pad; Y pages |
| Change zone | n/a | Tab / Shift+Tab: Roster > Kit tray > Innate > Buttons > Roster | LB / RB when not in the roster |
| Back | click H23 or H24 | Esc (closes the popup first if one is open) | B |

## 6. Eyes-first order and light

1. The hero on the pedestal: the only warm light and the only large figure.
2. The name plate H8.
3. The stat numbers H11 and the role H10.
4. The kit plates H17 (their tier edges are the only lit data in the panel).
5. The innate text H14 and the core block H18 to H20.
6. The roster H22.
7. Confirm H26.

The light budget follows art bible 2.3.1 Menus: lit area 6% or less, APL 0.06 or less. The chrome (K1 to K15) stays ink-dark, capped at INK-4 (4.9), with ornament only at the corners. Ambient motion is limited to **two elements**: the hero idle and the lantern breathing (0.5 Hz or slower, 6% luminance amplitude or less). Nothing else loops.

## 7. Motion hooks (intent only; the technical-artist owns the timings)

| Hook | Moment | Intent | Reduced-motion fallback |
|---|---|---|---|
| HS-01 screen-enter | From the Title "Chơi" | Threshold: the hall rises out of ink, H2 settles, then the hero steps onto the pedestal. At least 250 ms in total (2.2 rule 5). | Straight fade of the same length |
| HS-02 hero-idle | Loop while shown | The hero is alive and waiting. Breathing only, no tilt. | Static idle frame |
| HS-03 card-hover | Pointer enters a card | Acknowledge without committing | Ring appears |
| HS-04 card-select | Card selected (click, key, arrow) | "This one": ring, corner square and pool shift together | Ring and square appear |
| HS-05 showcase-swap | The selection changes | The outgoing hero leaves and the incoming one takes the pedestal. **Never blocks input**: a fast second press jumps to the newest hero. | Cross-fade, 100 ms |
| HS-06 info-swap | The selection changes | Text and numbers change in place. No counting up, so the numbers are readable at once. | Instant |
| HS-07 kit-seat | The selection changes, or screen entry | The 3 plates set down into the tray in order, core first. The core bar draws last. | Plates appear |
| HS-08 arrow-press | H4 / H5 pressed | Tactile click | Pressed fill only |
| HS-09 confirm-press | H26 pressed | Weighty commit (K4 pressed state) | Same |
| HS-10 confirm-commit | Confirm accepted | The hero takes its ready pose, the other cards fall to ink, the lantern pool swells slightly | Fade of the other cards |
| HS-11 exit-to-planning | After HS-10 | Hand-off to planning PL-01. **The hero is the continuity object**: the same hero arrives on the planning pedestal. | Cross-fade of at least 250 ms |
| HS-12 back-to-title | Esc / Back | Retreat: the hall sinks to ink | Fade |
| HS-13 refused | Confirm on a locked hero | 2 px shudder of the locked card, two cycles | No shudder; the popup still explains |

## 8. Growing from 2 heroes to 4 without a redesign

- Only H21 and H22 depend on n. The card x positions come from the formula in H22. Every other rect is fixed.
- With n = 1, the card centres at x 288 and H4 / H5 are hidden.
- The digit keys 1 to n follow the card order. The card order is the data order of the unlocked heroes.
- The info panel's content is per hero with fixed slots, so new heroes need no layout work if they respect the writer budgets: name 20 characters or fewer, role 30 or fewer, innate 3 lines or fewer, core name 23 or fewer, and 3 kit plates that fit a 132 px row.

## 9. Edge cases

| Case | Behaviour |
|---|---|
| The innate text is longer than 3 lines | H14 shows 3 lines ending in "...". The full text is in the H13 popup, so nothing is hover-only: H13 is focusable. |
| The kit plates do not fit 132 px (a future 3-wide footprint) | All 3 plates render as **1 x 1 avatar plates** (the compact form of art bible 7.6). The popup still shows the true footprint. |
| A hero lists more or fewer than 3 starting items | The tray lays out what exists, up to 4 compact plates. Dev builds log a data warning. |
| The core item is not among the starting three | H18 to H20 still show it, with "(không mang sẵn)" (not carried at start) appended to the name. No marker bar appears. |
| The showcase sprite is missing | The pedestal shows an INK silhouette placeholder. The hero can still be selected. |
| A Restart preselects a hero that is now locked or missing | The selection falls back to slot 1. |
| The first-launch Reduced flashes prompt is pending (System 31 Rule 3) | It opens over the **planning** screen on entry, never over this screen, so this screen stays a single decision. |
| The window zoom changes (2x, 3x, 4x) | The native layout is unchanged. K components are 9-sliced, never scaled. |

## 10. Accessibility checklist

- [x] Keyboard only: every control is reachable (section 5). No hover-only content (H13 and the kit plates are focusable).
- [x] Gamepad only: the mapping is pad-ready. Delivery falls under the documented MVP waiver (System 34).
- [x] Text is at least 12 px line box; digits at least 8 px. No all-caps. No "→" or "—".
- [x] Not colour alone: selection is a ring plus a corner square (form); tiers in the popup use form plus the tier word; the stat glyphs carry words.
- [x] No flashing: the lantern breathes at 0.5 Hz or slower; HS-10's swell rises over at least 100 ms (4.12 rule 5).
- [x] Subtitles: no dialogue on this screen.
- [x] UI scales: integer zoom only; tested at 1280 x 720, 1920 x 1080, 2560 x 1440 and 3840 x 2160.
- Reduced motion: every hook has a fallback (section 7).

## 11. Acceptance criteria

1. Pressing "Chơi" on the Title opens this screen full-screen. No workbench, stage, top strip or run panel is visible, and the planning screen no longer contains a hero-pick step.
2. A capture at 640 x 360 (1x) matches every rect in 2.2 to the pixel, with n = 2 cards at x 252 and 324. A 4-hero test fixture places the cards at 180, 252, 324 and 396 and changes no other rect.
3. On entry, slot 1 is selected and focused (or the last-picked hero after a Restart), and Confirm is enabled.
4. Left / Right, Q / E, 1 to n, H4 and H5 each change the selection. H6, H8, H10, H11, H14, H17 and H19 to H20 show the new hero's data on the same frame the input is processed.
5. Moving the pointer across every card without clicking leaves the selection unchanged.
6. For each hero the screen shows: `nameVi`, `roleVi`, `baseMaxHp`, `baseSpeed`, `initialShield` (only when > 0), the innate name and text, exactly 3 plates at true footprint, and the core item's name. Every value matches `heroes_mvp.json` and `items_mvp.json`.
7. Each kit plate and H13 opens the popup after 300 ms of hover and at once on focus. The popup never moves while open.
8. Confirm (click, Enter, double-click) opens the planning screen with that hero, exactly 3 items in the backpack, and 0 items in the stash. No second confirmation is asked.
9. A Confirm press within 250 ms of entry is ignored (input lock).
10. Esc with a popup open closes only the popup. Esc with no popup returns to the Title, and no run state remains (a new "Chơi" starts clean).
11. The full path (select the second hero, read one item popup, confirm) can be completed with the keyboard only, and again with the mouse only.
12. No on-screen string contains "→" or "—", no Vietnamese glyph renders as tofu, and no text uses a cut below 12 px (digits excepted).
13. A greyscale capture shows which card is selected (corner square and ring) without colour.
14. With Reduced motion on, HS-01 to HS-13 use their fallbacks: no shudder, no rise, and fades of the same length.
15. At 2x, 3x and 4x zoom, no K component shows seams, stretched corner ornaments or sub-pixel offsets.

## 12. Open questions

| # | Question | Options | Recommendation |
|---|---|---|---|
| Q1 | Which 3 starting items does each hero keep? (Data has 4 and 5.) | game-designer decides | Keep the core item among the three. |
| Q2 | Should future heroes (ranger, cleric) show as locked cards now? | Hide (centre the 2 available) / Show sealed cards | **Hide**: there is no unlock system yet, and a sealed card promises content. |
| Q3 | Big hero art | Dedicated 96 x 96 showcase idle, authored / 2x nearest-neighbour of the in-game sprite | **Authored**: 2x scaling makes mixels against 1x chrome. The art-director decides. |
| Q4 | Confirm label | "Vào hầm ngục" (Enter the dungeon) / "Chọn" (Choose, today's label) | "Vào hầm ngục" names the outcome. |
| Q5 | Esc to the Title without a confirm dialog | Requires System 5 to accept that a run created by "Chơi" but not yet picked can be dropped silently | Yes, because nothing has been committed. |
