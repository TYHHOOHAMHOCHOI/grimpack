# Planning mock-up and palette: findings (2026-11-10)

Evidence for the art bible (`design/art/art-bible.md`). Scripts: `tools/art/palette.py`, `tools/art/mockup_planning.py`.
Images in this folder: `mockup-planning-c24*.png`, `mockup-planning-c20*.png` (1x, 3x, greyscale 3x, lit-mask 2x),
`tier-strip-c24*.png`, `tier-strip-c20*.png` (six tiers x 1x1, 1x2, L, with same-hue icons; colour and greyscale).
Raw numbers: `mockup-planning-measurements.md`.

**What this is and is not.** A greybox frame in the exact master palette: placeholder silhouettes (Section 5 rules),
placeholder text blocks (no font exists yet, drawn at about 40% lit density), procedural item glyphs. It re-measures
numbers; it does not judge final art. The lit-mask "precision" is 100% by construction and proves nothing about a real
frame. Nothing here was viewed on a real monitor.

## 1. Contrast figures, now tool-computed (WCAG relative luminance)

These replace the hand-computed values; all matched the hand figures to the second decimal.

| Pair | Computed | Art bible claim | Result |
| ---- | ---- | ---- | ---- |
| INK-4 on INK-0 | 3.38:1 | 3:1 (4.3) | passes |
| INK-4 on INK-1 | 3.02:1 | 3:1 (4.3) | passes, barely |
| INK-4 on INK-2 | **2.48:1** | 3:1 (4.3) | **fails** (already limited to INK-0/INK-1 in 4.3) |
| Fog on INK-2 | 4.66:1 | about 4.7:1 | confirmed |
| Fog on INK-1 | 5.68:1 | n/a | |
| Bone on INK-2 | 10.08:1 | about 10:1 | confirmed |
| Bone on INK-1 | 12.29:1 | n/a | |
| Blood on Blood-D (old HP bar) | 2.61:1 | 2.6-2.8 | confirmed fail |
| Blood on INK-0 (new HP track) | 3.77:1 | about 3.8:1 | confirmed |
| Bone on Blood | 3.64:1 | about 3.6:1 | confirmed (numerals must stay beside the bar) |
| Plum-E on INK-1 | 3.30:1 | about 3.3:1 | confirmed |
| Fog-hi on INK-1 (intent) | 8.56:1 | n/a | |

Relative luminances: Fog 0.299, Bone 0.704, Ceiling 0.81, Candle 0.582, Blood 0.157, INK-4 0.136, INK-0 0.005,
White base 0.575, Gold 0.496, Green 0.366, Blue 0.303, Purple 0.205, Red 0.184.

## 2. Findings that need a decision

1. **INK-0 breaks the saturation rule by letter. Resolved 2026-11-10 (art bible 4.1): the cap applies at value >= 0.25.** `#0B0F1C` has HSV saturation 0.607 (> 0.60, rule 4.1) at value 0.11.
   HSV saturation is not meaningful that dark. Proposed fix: apply the 0.60 cap only at value >= 0.25. (`palette.py` prints
   this as a NOTE and does not fail.)
2. **The figure-ground contract is weak by the numbers; the enemy outline (option A) fixes the edge, not the body.** Hero body INK-3 on wall INK-1 is 1.69:1;
   enemy body INK-2 on void INK-0 is 1.36:1 (the hand figures were 1.7 and 1.4). In the mock-up the heroes read mainly
   through the Candle rim and face patch, and the enemies are barely visible. Section 5.9 predicted this and named the
   enemy-on-void pair as the first thing to check. Candidates, not applied: raise the enemy body one ramp step (INK-3 on a
   darker void), thicken the enemy rim to 2 px, or add the interior highlight row 5.9 already lists as a fallback.
   **Re-run after the user chose the 1 px INK-4 enemy outline on all sides (option A):** the edge pair is INK-4 on INK-0 =
   3.38:1 (passes 3:1) and INK-4 on INK-1 = 3.02:1 (passes by 0.02). In the 3x view the enemies now read as clear
   silhouettes on both the void bay and the wall. The interior body (INK-2 on INK-0, 1.36:1) is unchanged and carries no
   information, so the silhouette depends entirely on the outline: the outline must never be dropped or animated away,
   and any enemy sprite wider than its outline row (arms, weapons) needs the same outline. The headroom to INK-1 is 0.02.
3. **Fog sits one thousandth below the lit threshold.** Fog luminance is 0.2990 and the lit threshold is 0.30, so the
   resting synergy bridges (Fog, 7.6) are not counted as lit. Any palette tweak toward a brighter Fog flips them into
   the budget. The planning budget is far from binding (see 3), so this is a bookkeeping risk, not a visual one.
4. **Red and Purple tier frames are the weakest on the INK-2 bed.** Base contrast on INK-2: Red 3.13:1, Purple 3.41:1
   (Blue 4.72, Green 5.56, Gold 7.29, White 8.35). Red passes 3:1 by 0.13. The dark EDGE colours are 1.17 to 2.07:1 on
   INK-2, which is intended (they separate plates) but means the lit 1 px BASE ring does all the work.
5. **The Red serration is nearly invisible in this implementation.** The art bible does not say whether teeth point
   inward or outward or what colour they are. The mock-up draws them inward in the dark EDGE colour, and in greyscale they
   are almost lost. Decide: teeth in BASE (or HI) colour, and inward or outward. Form is the primary tier cue (Principle 2),
   so this matters. **Resolved 2026-11-10 (art bible 3.4.4):** teeth in Red-hi, 2 x 2 px on a 4 px pitch, hanging inward from the
   lit ring. Tried in BASE first: still faint in greyscale. Red-hi reads clearly. Still the weakest form cue on a 1x1 plate
   (only 3 teeth fit between the chamfers); the icon spike must confirm Red against Purple.
6. **Green versus Blue is distinguishable but subtle.** One chamfer (top-left) versus two diagonal chamfers (top-left and
   bottom-right) with 3 px cuts. Readable in the 3x views; the spike at native 1x and 2x with real icons is still needed.
   Same-hue icons on their own tier plate (a blue icon on a Blue plate) did not obviously hide the tier in the strip.

## 3. Budgets, measured on the worst-case planning frame

30 items in an 8 x 6 grid (91.7% occupied), 10 resting bridges, a focused plate, a stash, a compact hover popup open
(152 x 140, placed over the stash and lower workbench, cost 1) with name, tier word, numbers, two effect lines with
underlined keyword glyphs, an affects row and a curse tag, order strip, three heroes with HP and stat rows, and **five
enemies in two vertical lines** (3 in line 1, 2 in line 2) with intent tabs, HP and stat rows, INK-4 outlines, round-headed
niche and square-headed void bay. This replaces the earlier dock-version run.

| Measure | C = 24 | C = 20 | Rule |
| ---- | ---- | ---- | ---- |
| Lit area | 3.59% | 3.24% | planning budget 12% |
| APL | 0.038 | 0.035 | planning cap 0.08 |
| Saturated area (tier colours plus free-palette icons) | 1.00% | 0.86% | cap 12% |
| Off-palette pixels | 0 | 0 | 0 |
| Pixels at or below the ink floor / at or above the ceiling | 0 / 0 | 0 / 0 | 0 |

The hand estimate in 4.10 and 7.16 was 7 to 10% for planning. The measured figure is about a third of that. The popup's
text is a pattern drawn at about 40% density, and the enemy outlines are INK-4 (not lit), so real text could roughly
double the lit figure and still stay under 12%. The budget is not the constraint; legibility is.

## 4. Layout facts confirmed

- The 7.3 layout fits at 640 x 360: left workbench `8 x C + 14` (206 px at C = 24, 174 px at C = 20), party column 140 px,
  right column 294 px (326 at C = 20), top strip 16, action strip 24.
- Three hero slots at pitch 98 (C = 24) and 94 (C = 20) fit above the action strip with the 12 px stat row.
- No dock: the whole right column is enemies. Two lines of 140 px with a 4 px pier fit at both C values. Line 1 (medium,
  small, medium) uses 274 px of about 300; line 2 (small, medium) uses 178 px. The margin right of the two lines is 7 px at
  C = 24 and 39 px at C = 20 in the mock-up (10 and 42 in the bible, less the 3 px stage frame). Three medium enemies per
  line are about 300 px: that is the real ceiling, with almost no spare height.
- The compact popup (152 x 140) fits in the workbench lower section over the stash plates and never touches an HP row,
  intent tab, order strip or action strip. It covers part of the stash row (cost 1, as 7.9 specifies).
- Enemies in different lines are centred independently, so their tabs and HP rows do not align, as 7.4a requires.

## 5. Limits of this evidence

- No real font: text strokes are a pattern, so any text-legibility conclusion is untested.
- Glyphs are procedural stand-ins; the icon spike (art bible 8.5.6) is still needed.
- Only the planning state was built. Combat, door choice, payoff and Red-room frames are not measured; the lifted-glyph and avatar
  paths from line 2 across line 1 (7.5) are not drawn.
- Not viewed on a real monitor; the swatch check in 4.17 is still open.
