Design note: this kit is original to Grimpack; no reference video or other game informed it (user instruction, 2026-11-17).

# Grimpack: Generated UI Kit (real materials, light ornament)

> **Status**: Draft, 2026-11-17, staged for user approval (target path `design/art/ui-kit-generated.md`).
> **Owner**: art-director. **Decision (user, 2026-11-17):** UI chrome is built from generated, palette-locked components instead of code-drawn rectangles. Direction: dark rotten wood, rusted iron, tanned leather, dark stone, small corner details; still dark, so items, numbers and HP stay the brightest things (art bible 1.1).
> **Governs with**: art bible 3.2, 3.6 and 4.9 (amendment text in Appendix A, pending approval), 4.3, 4.10, 4.12, 4.13, 6.2, 6.3, 6.7, 7.11, 8.3, 8.7, 8.10, 8.11. Sizes follow the ux-designer's layout spec of 2026-11-17.
> **Rule for every prompt:** never name a game, studio, artist or franchise, and never ask for "the style of" anything.

## 1. Scope

| Generated (this kit) | Stays as it is (code or existing generator) |
| ---- | ---- |
| Panels, trays, plates, buttons, tabs, card bodies, pedestals, popup frame, backdrops, banner, dividers, strip rails, key chips (K1 to K16); effect key poses (V1 to V15) | Item plates, tier edges and tier forms, rank pips, curse tags, cell beds, HP bars, intent tabs, stat chips, glyphs, text, hover/focus/selected rings, ghosts, code-drawn rings and threads |

**Generate the material once, derive every state by script** (Section 6). A state is a palette remap or a 1 px offset of the generated default, never a new generation: states stay pixel-aligned and cost nothing.

## 2. Material map and palette

### 2.1 Materials follow ownership
Warm organic materials hold the player's kit; stone is the dungeon's; iron is the hardware of both. This extends 4.6 (warm = you, cold = dungeon) instead of diluting it. Every field (the inside of a container) stays flat ink.

| Material | Shadow | Face | Key edge (top, left) | Restricted accent | Told apart by pattern (6.3) | Containers |
| ---- | ---- | ---- | ---- | ---- | ---- | ---- |
| Rotten wood | INK-0 | UMBER-1 | UMBER-2 | CANDLE-D worn flecks | long straight planks, sparse broken grain dashes, a 1-2 px rotten bite at a plank end, INK-1 damp patch | K1 workbench, menus, dialogs; K1D drawer; K3; K4/K5; K12; K13 wood; K14 rails |
| Tanned leather | INK-1 crease | UMBER-1 (lit walls UMBER-2) | UMBER-2 | CANDLE-D scuff | smooth face, one continuous seam groove on lit sides only, soft 2-3 px corner creases | K2 trays, K6 tabs, K7H hero card, K11 gold plate |
| Dark stone | INK-0 mortar | INK-1 (field) / INK-2 (chrome) | INK-3 (chrome only) | none | large blocks in running bond, irregular joint spacing, chipped corners | K1S stage frame, K7 door body (single slab, 6.2), K8, K10, K13 stone, K16 |
| Rusted iron | INK-1 / INK-0 | INK-3 | INK-4 | rust bloom UMBER-2 + at most one CANDLE-D fleck, at the lower edge | flat straight caps and straps, square rivets 1x1 or 2x2 with an INK-0 shadow pixel lower-right | all caps, clamps, rivets; K5M; K9; K15 |

### 2.2 Proposed palette additions (Q1, needed before any wood or leather piece)

| Name | Hex | Rel. luminance | Role | Nearest existing (OKLab, hand-computed, NOT VERIFIED) |
| ---- | ---- | ---- | ---- | ---- |
| UMBER-1 | `#2B201C` | 0.016 (between INK-1 and INK-2) | wood and leather face; text-safe field | Plum, about 0.05 |
| UMBER-2 | `#4F3628` | 0.044 (just under INK-3) | wood key edge, lit leather walls, rust | Blood-D, about 0.06 |
| RUST (held, not proposed) | `#6B3B24` | 0.064 | only if iron caps do not read as rusted | |

- Count 41 to 43 of the 48 cap (4.13), leaving 5 reserved slots (the second currency of 4.6 needs one or two).
- Why: the only brown is CANDLE-D (0.12, near the chrome ceiling and the falloff of the "you" light). Wood from Wine, Plum or Blood-D would put the Red-room, curse and lost-HP colours on every panel. **The 2026-11-17 probe confirms it:** given only the existing colours, `create_ui_asset` built the wood from Moss ("mossy wood"), which 4.9 point 5 bans from chrome.
- If refused: the kit falls back to stone and iron only (INK ramp); wood and leather are dropped.
- Contrast (WCAG, hand-computed, NOT VERIFIED): Bone on UMBER-1 11.4:1, Fog on UMBER-1 5.3:1 (pass). Fog on UMBER-2 3.7:1 fails, so **no text on UMBER-2**. INK-4 against UMBER-1 is 2.8:1 (under the 3:1 boundary rule, 4.3), so **controls never sit directly on wood or leather**; they sit on INK-0/INK-1 fields.

### 2.3 Sub-palettes (pass as `palette` to `create_ui_asset`; snap `create_image_pixen` output to the same list)
Never pass the full master palette for chrome (the probe's broad list produced Moss wood and 16 colours).

| Set | Hex list | Used by |
| ---- | ---- | ---- |
| S-WARM | `#0B0F1C #151B2E #222B44 #36415F #5A6785 #2B201C #4F3628 #7C5C3C` | K1, K1D, K2, K3, K5, K6, K7H, K11, K12 |
| S-STONE | `#0B0F1C #151B2E #222B44 #36415F` | K1S, K7, K16 |
| S-IRON | `#0B0F1C #151B2E #222B44 #36415F #5A6785 #4F3628` | K5M, K9, K15 |
| S-ENV | `#0B0F1C #151B2E #222B44` | K8, K10b |
| S-MENU | `#0B0F1C #151B2E #222B44 #36415F #7C5C3C #EFC186 #E4DAC0` | K10a |
| VFX | per row in Section 7 | V1 to V15 |

`tools/art/build_generated_ui.py` currently snaps to all 41 colours; it needs a per-entry sub-palette (technical-artist). Its docstring example name `ui-gen_panel-large_9s` breaks the 8.3 regex; use the names below.

## 3. Shared prompt style block (prefix to every UI prompt, verbatim)

> Original pixel-art interface piece for a dark dungeon-vault game, drawn at true size, one pixel per pixel. Crisp hard edges, a 1 px outline in the darkest ink colour, no anti-aliasing, no gradients, no dithering, no texture noise, no blur, no glow, no soft shadows. Flat shapes, at most three flat value steps per material. One light from the top-left: only top and left edges catch light, bottom and right edges fall into shade. Materials are dark and worn: rotten wood in long straight planks with sparse broken grain, dark tanned leather with one plain seam, cold dark stone in large blocks laid in running bond, flat rusted iron caps and straps with tiny square rivets. Everything is very dark and quiet: near-black blue ink with dull dark-brown wood and leather; nothing shines or glows, no bright metal, no white, no gold, no moss, no colour accents. Square corners and straight edges only: no bevelled or cut corners, no L-shaped brackets inside the frame, no diagonal braces, no round studs or rings, no hanging parts, no filigree, no symbols, runes, emblems or letters. Every inner area is flat and empty for text and content. Use only these colours: {sub-palette}.

**Effect swap (V rows):** replace the two material sentences and the outline clause with: "A small effect sprite on a transparent background, no outline, three flat colours stepping from a bright core to a dark edge, hard pixel clusters."

## 4. Tools, cost, pipeline

- **UA** = `create_ui_asset`: canvas 192 to 688 px, `palette` parameter, native 1:1 pixels, 10 generations per call up to 256x256. Larger canvases cost more: K10a and K10b are budgeted at about 30 and 20, **assumed**; verify with the free `search_help(tool="create_ui_asset")`.
- **PX** = `create_image_pixen`: for pieces under 192 px. Generated at the listed multiple (2x or 4x), reduced by exact k-block reduction / nearest neighbour only (8.11 step 2), then snapped. Cost **assumed 2 per call, NOT VERIFIED**: check `search_help(tool="create_image_pixen")` before the batch; only the PX lines of Section 8 change.
- **Free:** `pixelart_workbench` (clean-up, cutting, state remaps), `vfx_workbench` (in-between frames).
- **Probe (2026-11-17):** UA 256x256 gave a good dark frame with iron corner pieces, about 14 px border, 16 colours, 1:1 pixels. Keep the method; change three things: the sub-palette (no Moss, Fog, Bone, Blood, Plum, Wine), a **5 px band** (a 14 px border does not fit the 174 px workbench: 8 columns at C = 20 are 160 px), and check the "corner brackets" are solid caps lying on the band, not inner L lines (the Gold bracket, 3.2).

**Pipeline per piece:** (1) style block + piece prompt + sub-palette; raw saved to `art-source/ai-raw/ui/`. (2) PX: reduce. (3) Binary alpha, threshold 128. (4) Snap to the sub-palette in OKLab, no dithering. (5) Clean-up in indexed mode: outline, key side, orphans, flat fields. (6) Cut the 9-slice source (corners, one edge module, centre); check Tiled modules in a 3-repeat strip. (7) Derive states (Section 6). (8) Run the Section 5 checks; provenance (tool, prompt, seed, date) to the manifest (8.11.8); contact sheet to `production/qa/evidence/`. (9) UA pieces get at most one regeneration (to hold the budget), then hand-fix; PX up to three (8.11.7).
**Pilot gate:** generate K1, K1S and K5 first, composite them into the planning mock-up (`tools/art/mockup_planning.py`), run the 4.10 lit mask and the squint test, and show the user before the rest.

## 5. Components

### 5.1 Table (native px at 640x360; 9-slice order L, T, R, B)

| ID | File (8.3) | Purpose | Material | Native sizes in use | Source and 9-slice | States (derived) | Tool, canvas | Gens |
| ---- | ---- | ---- | ---- | ---- | ---- | ---- | ---- | ---- |
| K1 | `ui_panel-wood_default_9s` | Workbench, menus, pause, dialogs | Wood, iron corner caps | 174x344, 312x216 | 84x84; 10,10,10,10; Tiled, 64 px edge module; band 5 | none | UA 256x256 | 10 |
| K1S | `ui_panel-stone_default_9s` | Stage frame (window on the world) | Stone, squared corner blocks | 466x344 | 84x84; 10,10,10,10; Tiled 64; band 5 | Red room: INK-n to Wn at runtime | UA 256x256 | 10 |
| K1D | `ui_panel-drawer_default_9s` | Sell/forge drawer body | Wood, iron runners on top and bottom bands | 168x160 | 84x84; 10,10,10,10; Tiled 64 | none | UA 192x192 | 10 |
| K2 | `ui_tray-leather_{default,ready,over,invalid}_9s` | Inset tray under grid, stash, sell/forge slots | Leather | 84x84 to 144x144, 164x84, 164x66, 144x92 | 12x12; 4,4,4,4; Sliced; rim 2 | ready, over, invalid | UA 192x192 | 10 |
| K3 | `ui_plate-name_default_9s` | Hero nameplate, section and panel titles | Wood plank, iron end caps | h 16 to 22, w 152 to 296 | 32x22; 8,4,8,4; Sliced (flat centre) | none | PX 2x, 192x44 (96x22 board) | 2 |
| K4 | `ui_btn-primary_{default,hover,focus,pressed,disabled}_9s` | Start fight, Confirm | from K5; face per Q2 | 148x20, 144x22 | 24x22; 6,4,6,4; Sliced | all 5 | derived from K5 | 0 |
| K5 | `ui_btn-secondary_{default,hover,focus,pressed,disabled}_9s` | Ordinary buttons, speed segments, dialog buttons | Wood face, 1 px iron rim, iron end clamps | 88x22, 76x20 | 24x22; 6,4,6,4; Sliced | all 5 | PX 2x, 160x44 (80x22) | 2 |
| K5M | `ui_btn-mini_{default,hover,focus,pressed,disabled}_9s` | Icon buttons (menu cog, arrows) | Iron | 16x24, 16x16 | 16x16; 3,3,3,3; Sliced; face 10x10 clear | all 5 (states use the frame in the 16 px top strip) | PX 4x, 64x64 | 2 |
| K6 | `ui_tab_{selected,unselected}_9s` | Stash/Overview, Sell/Forge tabs | Leather | 54x14 | 24x14; sel 6,4,6,0 (open bottom); unsel 6,4,6,2 | unselected | PX 2x, 108x28 | 2 |
| K6V | `ui_tab-pull_{default,hover,focus,pressed}_14x28` | Drawer pull tab (vertical) | Leather strap, small iron buckle at top | 14x28 | fixed | 4 | PX 4x, 56x112 | 2 |
| K7 | `card_neutral_frame_9s` | Door-card body under the tier edge | Stone slab | 90x140 (and 84x140) | 24x24; 8,8,8,8; Sliced | 6 tier composites + disabled | UA 192x192 | 10 |
| K7H | `card_hero_frame_9s` | Hero-select card | Leather on board, plain iron rim | 64x60 | 24x24; 8,8,8,8; Sliced | selected = code ring | UA 192x192 | 10 |
| K8 | `prop_plinth_base_120x24` (+ `prop_plinth-dim`, `prop_plinth-empty`) | Hero pedestal | Stone dais | 120x24 | fixed; top ellipse 112x14 | dim, empty | PX 2x, 240x48 | 2 |
| K9 | `ui_popup-frame_default_9s` | Hover popup, chip fallback | Iron band on slate | 152 x up to 232; 18 tall | 16x16; 4,4,4,4; Sliced; band 3 | none | PX 2x, 64x64 (32x32 plate) | 2 |
| K10a | `env_heroselect_bg_640x360` | Hero-select backdrop | Stone vault, one lantern | 640x360 | fixed (bleed: `env_surround` INK-0 to 960x540) | none | UA 640x360 | ~30 |
| K10b | `env_stage_bg_460x314` | Stage backdrop (planning and combat) | Stone, D0/D1 | 460x314 | fixed | Red room swap | UA 460x314 | ~20 |
| K11 | `ui_plate-gold_default_9s`, `ui_glyph-coin_default_10x10` | Gold counter | Leather strap, iron tip; coin = V1 frame 1 | 40x14; coin 10x10 | 16x14; 4,3,6,3; Sliced | none | PX 2x, 80x28 | 2 |
| K12 | `ui_banner_{victory,defeat}_240x40` (+ `_9s`) | Result banner | Wood signboard, mounted flat, iron end straps | 240x40 | fixed; sliceable 48x40, 16,8,16,8 | defeat remap | UA 256x192 | 10 |
| K13 | `ui_divider-wood_default_9s`, `ui_divider-stone_default_9s` | Section lines | Wood batten / stone joint | 3 px thick | wood 24x3, 4,0,4,0 Sliced; stone 3x48, Tiled 48 | none | cut from K1 / K1S | 0 |
| K14 | `ui_strip-rail_default_9s` | Top strip band and action strip | Flat INK-1 face + 2 px wood rail on the edge facing the panels, iron end caps | 640x16 (rail at bottom, face 14), 460x24 (rail at top, face 22) | rail 84x2; 10,0,10,0; Tiled 64; face is a code fill | none | cut from K1 batten | 0 |
| K15 | `ui_keycap-iron_default_9s` | Input prompt chips (Esc, Ctrl+Enter, 1 2 3 K) | Iron | 14 tall, width by label | 12x14; 4,0,4,0; Sliced | none | PX 4x, 48x56 | 2 |
| K16 | `ui_map-strip_default_9s` | Corridor map groove (8 rooms + boss), only if the layout uses it | Stone groove | 124x14 (channel 12) | 24x14; 8,0,8,0; Sliced | none | PX 2x, 248x28 | 2 |

Layout fit: a 5 px band plus a 2 px tray rim replaces the old 3 px frame plus 4 px padding, so the workbench formula `n_columns x C + 14` (174 at C = 20) is unchanged with the tray flush inside the band.

### 5.2 Piece prompts (append after the style block) and piece checks

- **K1** "One large rectangular panel frame filling the canvas. The frame is a 5-pixel band of dark rotten wooden planks on all four sides, grain running along each side, a few broken grain lines and one or two small rotten bites at plank ends. Each corner is wrapped by a flat rusted-iron angle plate lying on the band, the same width as the band and about 10 pixels along each arm, with two small square rivets and a little rust at its lower edge. One thin horizontal wooden batten, 3 pixels tall, crosses the inside at about 60% height. The inside is one flat dark blue-black colour." *Checks:* band 5 px; UMBER-2 key edge on top and left bands only; caps never overhang inward; 2 rivets per cap, not mirrored; field INK-1, one colour. Batten is cut for K13 and K14.
- **K1S** "One large rectangular frame filling the canvas, a 5-pixel band of dark cold stone blocks in running bond on all four sides, block lengths varying from 12 to 22 pixels, thin dark mortar joints, one or two chipped block corners. Each corner is one larger squared stone block. One vertical line of stone 3 pixels wide divides the inside at one third of the width. The inside is one flat near-black colour. No metal, no carvings." *Checks:* no 4 equal joint spacings in a row; corners square, not chamfered; no INK-4. Divider cut for K13 stone.
- **K1D** "An open rectangular wooden drawer seen from straight above, 168 by 160 pixels, centred. Walls are dark rotten planks 5 pixels thick with long grain. A flat rusted-iron runner strip along the outer half of the top wall and of the bottom wall only. Square iron corner plates with one square rivet each. The floor is one flat dark blue-black colour." *Checks:* runners on top and bottom only (never a closed inner ring).
- **K2** "A recessed rectangular tray of dark tanned leather filling the canvas, seen from straight above, with a thin 2-pixel rim: the top and left inner walls in shadow, the bottom and right inner walls catching the light. Small soft creases only in the four corners. The floor is one flat dark brown colour." *Checks:* inset lighting (shadow top-left, lit bottom-right); no iron; no stitch dots; floor UMBER-1 (swap to INK-1 by index if the squint test shows it competing with cells). States: ready = outer lip INK-0 to Fog (1 px); over = 1 px Candle ring, 1 px gap; invalid = lip Fog, walls desaturated to INK-2/INK-3, crosses on cells (7.11, no red).
- **K3** "A horizontal nameboard: one dark rotten wooden plank 22 pixels tall and 96 wide with a flat even face for a name, its left and right ends wrapped by short rusted-iron caps, one square rivet each. Top edge lightly lit, bottom edge dark." *Checks:* face flat UMBER-1, at least 12 px clear at 16 tall; caps 8 px or less.
- **K5 (and K4)** "A rectangular push button 22 pixels tall and 80 wide: a flat dark wooden face with a thin plain iron rim all round, short rusted-iron clamps over the left and right ends with one square rivet each, a lit top edge and a 1-pixel darker lip along the bottom. Plain rectangle silhouette." *Checks:* rim 1 px INK-4 (the 3:1 boundary on INK-0/INK-1 fields); face flat UMBER-1; clamps 6 px or less.
- **K5M** "A tiny square push button of dark iron, 16 by 16 pixels: a flat plain face for a small symbol, lit top and left edge, darker bottom and right edge, one tiny rust spot at the lower edge." *Checks:* clear 10x10 face for 8x8 glyphs.
- **K6** "A small leather tab 14 pixels tall and 54 wide, like the tab of a ledger, square top corners, open at the bottom where it joins a panel below, a flat dark face for a short label, lit top and left edge, one plain seam along the top." *Checks:* selected and unselected differ by form (open versus closed bottom, Section 6), not only colour.
- **K6V** "A small vertical pull strap of dark tanned leather, 14 pixels wide and 28 tall, square ends, its right side open where it joins a drawer, a small square rusted-iron buckle at its top end, a flat face in the middle for a small symbol. Lit top and left edges." *Checks:* clear 8x8 face at centre.
- **K7** "A tall rectangular door slab of dark cold stone filling the canvas, one single flat slab with no inset panels; its face is one flat near-black blue colour; a few small chips and hairline cracks only near the four corners; top and left edges faintly lit, bottom and right edges in shade. Square corners. No frame, no metal, no carvings." *Checks:* face INK-1 flat; wear marks 3 to 7 px in from corners, darker than the face; the outer 2 px are replaced by the tier frame from the plate generator and the body is masked by the tier silhouette (never shows outside a chamfer). Disabled: body one step darker (INK-1 to INK-0), tier edge unchanged, text stays readable (Fog plus cross, 7.10).
- **K7H** "A rectangular card of dark tanned leather stretched over a board, filling the canvas: flat even leather face, one plain seam groove along the bottom and right edges only, small soft creases near the corners, a thin plain iron rim with square corners." *Checks:* no corner ornament; seam never closes into a ring.
- **K8** "A low, wide round stone dais seen from the front and slightly above, 120 pixels wide and 24 tall: a flat elliptical top about 112 by 14 pixels, one short front face below it made of large dark stone blocks with a few thin joints. Very dark, flat and quiet; the top is plain for a light pool to be drawn on it. No outline." *Checks:* S-ENV only (D2: INK-0 to INK-2, no outline, no rim, 6.3); ellipse, never octagonal or chamfered.
- **K9** "A small rectangular plate of dark slate with a thin flat iron band around its edge, 3 pixels thick, one tiny square rivet at each corner, square corners, top and left iron edges faintly lit. The slate inside is one flat dark blue-black colour for text." *Checks:* interior INK-1 (the popup keeps its INK palette in every room, 7.3); 4 rivets, corners only.
- **K10a** "A wide, quiet, very dark dungeon vault hall seen straight on: large stone blocks in running bond, flat floor slabs, one large semicircular arch opening to deep black at the centre-right, square stone piers at the far left and far right edges. One small lantern standing on a low ledge at the left, a third of the way up, casting a small warm pool of light in a few flat steps on the stone around it. The middle of the picture is plain wall and void with no objects. No people, no text, no banners, no chains, no windows." *Checks:* menu budget lit 6%, APL 0.06 or less; only lit pixels are the lantern flame; round arch only (never pointed, 6.2); quiet centre x 120 to 520, y 60 to 300 is D0/D1 only; no hanging objects (6.7).
- **K10b** "A flat dark stone wall seen straight on, filling the canvas: large blocks in running bond, every joint a thin darker line, blocks at least 8 pixels wide; a heavy flat stone lintel across the top 16 pixels; on the right two thirds a tall plain round-headed opening of pure black. No objects, no props, no light, no outline. Almost one value." *Checks:* 6.6 layers (L1 behind the party, L0 niche behind the enemies), no environment pixel above INK-2, at most 3 equal-spaced verticals (6.7). Arch position to be matched to the ux layout before generation.
- **K11** "A small flat horizontal strap of dark tanned leather, 40 pixels long and 14 tall, its right end finished by a short rusted-iron tip with one square rivet, a flat even face for numbers, lit top edge." Coin: see V1. *Checks:* face clear 26x10 for coin plus 4 digits.
- **K12** "A long horizontal signboard of two dark rotten wooden planks, 240 by 40 pixels, mounted flat (not hanging), its centre a flat even face for a title, each end wrapped by a flat rusted-iron strap with two square rivets at different heights, top edge lit, bottom edge dark. Square corners, no chains, no ropes." *Checks:* clear 200x28 face for the Display 24 cut; nothing hangs (curse tag, 3.2); defeat = Section 6 remap; the victory warmth comes from the Candle wash, never from Gold.
- **K15** "A tiny keyboard key cap of dark iron seen from the front, 14 pixels tall: a flat face for a key label, lit top edge, a 2-pixel darker front lip at the bottom. Square corners."
- **K16** "A narrow horizontal groove cut into dark stone, 124 pixels long and 14 tall: a flat near-black channel 12 pixels tall along the middle, plain stone lips above and below, each end closed by a squared stone block; the top and left walls of the groove in shade, the bottom and right walls faintly lit."

### 5.3 Shared acceptance checks (every piece)

| # | Check |
| ---- | ---- |
| A1 | Exact native size; alpha 0 or 255 only; nothing touches the canvas edge except 9-slice sources |
| A2 | 0 off-palette pixels after snapping; colours a subset of the piece's sub-palette; off-palette intent at most 15% (8.11) |
| A3 | Outline: 1 px INK-0 on chrome; none on K8 and K10 (D-classes) |
| A4 | Light from the top-left: key edges top and left only; insets (K2, K16) lit on the bottom-right walls |
| A5 | **No lit chrome:** 0 pixels in the 4.10 lit mask (luminance 0.30+, or saturation 0.60+ with value 0.55+), except state pixels (K4 face or rim, coin, lantern flame). No chrome pixel above INK-4 |
| A6 | Top step (INK-4, CANDLE-D) only on key-side 1 px edges, rivet heads, cap top-left faces; under 8% of opaque pixels on pieces 32 px or larger; CANDLE-D isolated, at most 3% |
| A7 | Reserved forms absent (3.2, 6.7): chamfers, inner L brackets or a second closed ring, 4+ equal-spaced points, hanging pieces, downward points, discs or rings 3 px+, diamonds, chevrons, diagonal braces, square lattices |
| A8 | Fields flat: text and content fields one colour; any marks darker than the face, isolated, at most 3% |
| A9 | At most 3 value steps per material; no orphan pixels |
| A10 | 9-slice: Sliced edges uniform along their length; Tiled modules seamless over 3 repeats; no seam at the three test sizes in 5.1 |
| A11 | Composited planning frame: lit-mask precision 90%+ and lit area 12% or less (4.10) still hold; chrome is not among the strongest edges next to content (squint) |
| A12 | Provenance logged; prompt names no game, studio or artist; reviewer confirms the piece came from this document's prompts only |

## 6. State and variant derivation (script, no generation)

| Variant | Rule |
| ---- | ---- |
| hover | 1 px Candle ring, 1 px gap (7.11); K5M and K15 in the 16 px strip use the frame instead |
| focus | 2 px Candle ring (K4 option A keeps the 2 px Bone ring of 2026-11-16) |
| pressed | content 1 px down, bottom lip removed; face to Candle with INK-0 text (7.11) |
| disabled | UMBER-1 to INK-1, UMBER-2 to INK-2, CANDLE-D to INK-2, INK-4 to INK-3; disabled text INK-3, never for must-read text |
| K4 primary | Q2 option B: outer 2 px of the K5 face to Candle, face UMBER-1, Bone text. Option A: whole face Candle, CANDLE-D lip, INK-0 text |
| K6 unselected | bottom closed by 2 rows (INK-0 line, face), face UMBER-1, text Fog; selected face matches the field below (INK-1) |
| K8 dim, empty | dim: INK-2 to INK-1; empty: top ellipse rim only, as the existing `prop_pedestal_empty` |
| K12 defeat | UMBER-1 to INK-1, UMBER-2 to INK-2, CANDLE-D to INK-3, INK-4 to INK-3, INK-3 to INK-2; text Fog |
| K7 tiers | body composited under the plate-generator tier frame for white, green, blue, purple, gold, red |
| Red room | stage stone (K1S, K10b, K8, stone divider) INK-n to Wn by runtime index swap (6.6); the workbench never shifts |

## 7. VFX sprites (for the technical-artist motion spec)

Flipbooks at 15 fps (8.10). Key pose by PX (1 call each); in-betweens in `vfx_workbench` (free) on a 3-colour mask ramp, recoloured by script. Particles 1x1 to 4x4; no additive blending; no post bloom. Names extend the 8.3 vfx vocabulary (validator update needed).

| ID | File stem (frames 01 up) | Use | Frames | Canvas | Ramp (bright to dark) | Key-pose prompt (PX) | Notes |
| ---- | ---- | ---- | ---- | ---- | ---- | ---- | ---- |
| V1 | `vfx_coin_spin` | coin pickup, coin fly to K11 | 4 | 10x10 | BONE glint (frame 1) / CANDLE / CANDLE-D, INK-0 rim | 4x: "a small plain round coin, pale warm candle colour, dark rim, no emblem" | frame 1 = the K11 coin; never Gold-tier hue |
| V2 | `vfx_coin_burst` | sale, reward | 3 | 24x24 | BONE (frame 1 core) / CANDLE / CANDLE-D | 2x: "a small uneven scatter of tiny warm specks bursting outward" | specks, never a ring outline (rings mean area, 3.2); code adds V1 coins |
| V3 | `vfx_forge_sparks` | anvil strike | 4 | 24x16 | BONE / CANDLE / CANDLE-D / INK-4 | 2x: "a short spray of tiny sparks flying up from a struck point, cooling as they fall" | 12 particles or fewer |
| V4 | `vfx_dust_puff` | snap thunk, landing, drawer close | 4 | 16x8 | INK-4 / INK-3 / INK-2 | 4x: "a low flat puff of dust spreading sideways along the ground" | never lit |
| V5 | `vfx_pickup_glint` | item lifted | 3 | 7x7 | BONE / CANDLE / CANDLE-D | 4x: "a tiny plus-shaped glint" | plus, never a diamond (rank pips) |
| V6 | `vfx_invalid_cross` | invalid drop | 3 | 9x9 | FOG-HI / FOG / INK-4 | 4x: "a small X mark popping in and settling" | settles into the 5x5 cross; no red (7.11); one pop |
| V7 | `vfx_status_puff-poison` | poison applied or ticking | 4 | 16x16 | MOSS-HI / MOSS / INK-2, 1 px BONE core frame 1 | 4x: "a small murky puff of cloud rising and thinning" | ink-class (4.7) |
| V8 | `vfx_status_lick-burn` | burn | 4 loop | 12x16 | BONE core (2 px max, frames 1-2) / CANDLE-D / BLOOD-D | 4x: "a small low flame with two tongues, the left taller" | matches the burn glyph; never bright Candle |
| V9 | `vfx_status_crust-frost` | frost | 4, last holds | 16x16 | BONE core / FOG / INK-3 | 4x: "a thin crust of ice with straight angular cracks spreading up from the bottom" | no lattice, no chevrons |
| V10 | `vfx_status_mark-weaken` | weaken | 3 | 12x12 | BONE core / WINE-4 / WINE-2 | 4x: "a short blade snapping in two, the tip dropping one pixel" | matches the weaken glyph and chip (Q6) |
| V11 | `vfx_impact_splatter` | HP lost on a hit | 3 | 16x16 | BLOOD / BLOOD-D | 4x: "a small splash of dark droplets flying to the lower right" | only when HP is lost (Blood = HP); never flashes |
| V12 | `vfx_death_dissolve` | enemy death | 7 | 48x48 | FOG-HI rim (frames 1-2) / FOG / INK-4 / INK-3 ash | 2x: "a dark figure crumbling into ash, blocky chunks falling away from the top down, flakes drifting up" | 1-bit blocky erase mask (2x2 chunks, no noise) plus ash flakes; TA picks mask method |
| V13 | `vfx_detonate_burst` (+ `-foe`, `-calm`) | stacks consumed | 4 (+3 calm) | 48x48 | party CEILING core (peak only) / BONE / FOG / INK-4; foe FOG-HI / FOG / INK-4 | 2x: "a burst of stepped concentric shells with short straight rays" | replaces the code burst only if it wins review; rise over 2+ frames; 2x2 ordered dither on outer shell only; 4.12 limits |
| V14 | `vfx_heal_sparkle` | HP restored | 4 | 16x16 | BONE / BLOOD / BLOOD-D | 4x: "a few tiny plus-shaped motes rising and fading" | no green (tier hue) |
| V15 | `vfx_shield_shimmer` | armour gained | 4 | 16x16 | FOG-HI / FOG / INK-4 | 4x: "a narrow band of cold light sweeping diagonally across a small shield" | masked to the 12x12 Fog shield glyph; stepped band |

## 8. Generation budget (target under 250 of about 850)

| Group | Calls | Gens per call | Planned |
| ---- | ---- | ---- | ---- |
| UA up to 256x256: K1, K1S, K1D, K2, K7, K7H, K12 | 7 | 10 | 70 |
| UA large: K10a (640x360), K10b (460x314) | 2 | about 30 and 20 (assumed) | 50 |
| PX UI: K3, K5, K5M, K6, K6V, K8, K9, K11, K15, K16 | 10 | 2 (assumed) | 20 |
| PX VFX key poses: V1 to V15 | 15 | 2 (assumed) | 30 |
| **First pass** | 34 | | **170** |
| Retry reserve (half the UA pieces once, one large backdrop once, 5 PX) | | | 70 |
| **Ceiling** | | | **240** |

## 9. Open questions for the user

**Answered by the user 2026-11-17:** Q1 add Umber-1 and Umber-2 (yes); Q2 option B, dark wood face with a 2 px Candle rim, Candle fill only when pressed; Q3 confirmed (warm wood and leather for the player, stone for the dungeon). Q4 and Q5 keep the defaults (Umber-1 tray floor unless the squint test fails; Wine kept for Weaken as an exception). Q6: the ids of this document stand.


1. **Q1 Palette:** add UMBER-1 `#2B201C` and UMBER-2 `#4F3628` (41 to 43 of 48)? Without them the kit is stone and iron only. RUST `#6B3B24` held in reserve.
2. **Q2 Primary button:** (A) keep the filled Candle face from the 2026-11-16 review, or (B) dark wood face with a 2 px Candle rim and Candle fill only when pressed (4.9 point 3). Recommend B: a filled Candle plate is the largest lit block on the planning screen after the grid, against today's "items, numbers and HP brightest".
3. **Q3 Ownership map:** warm wood and leather for the player's kit, stone for the dungeon (door cards as stone slabs, hero cards leather). Confirm.
4. **Q4 Tray floor:** warm leather (UMBER-1) or cold INK-1 under the grid? Default UMBER-1, swapped by index if the squint test fails.
5. **Q5 Weaken colour:** the weaken chip (2026-11-17) and V10 use Wine, which 4.9 point 5 reserves for the Red room. Keep as an exception or recolour?
6. **Q6 Id mapping:** this kit follows the ux spec's ids (K14 = strip band, K15 = key chip); the drawer is K1D, the drawer pull is K6V, the map strip is K16, the mini icon button is K5M. The coordinator's brief had K14 = drawer and K15 = map strip.

## 10. Knock-on edits not made here (follow-up, need approval)

Art bible 7.3 (frame widths), 8.8 ("Panel frame", "Popup"), 3.7 and 4.13 rule 4 (chrome step and outline exception), 4.2 (palette count after Q1), 8.3 vocabulary (vfx names; `card` names `neutral`, `hero`; `prop_plinth` variants), `tools/art/palette.py` (add UMBER-1, UMBER-2 after Q1, before any snapping), `build_generated_ui.py` (per-entry sub-palette).

## Appendix A. Art-bible amendment text (staged; apply to `design/art/art-bible.md` on user approval)

**A.1 In §3.6**, prefix the first bullet with `**[Superseded 2026-11-17 by the amendment at the end of this section.]**`, then insert after the section's "*Serves the Section 1 rule*" line:

> **Amended 2026-11-17 by user decision: real materials, light ornament.** This block replaces the first bullet above ("Thick, plain frames ... No rivets, bolts, filigree or ornament"). The other five bullets stand. The pieces are generated with Pixellab, palette-locked, and specified in `design/art/ui-kit-generated.md` (K1 to K16).
>
> - **The rule.** Chrome is built from four dark materials: rotten wood, tanned leather, stone, and rusted iron hardware. It may carry light ornament (corner caps, end caps, square rivets, one strap). It stays ink: no chrome pixel is lit, so items, numbers and HP remain the brightest things on screen (1.1).
> - **Materials follow ownership**, extending the warm "you" and cold dungeon split of 4.6: wood and leather hold the player's kit (workbench, menus, drawer, nameboards, buttons, banner, strip rails; trays, tabs, hero cards, gold plate), stone is the dungeon's (stage frame, door-card bodies as single slabs per 6.2, pedestals, backdrops, map strip), and iron is the hardware of both (popup frame, mini buttons, key chips, every cap, clamp, runner and rivet).
> - **Where ornament may sit, and nowhere else:** (1) a corner cap lying on the frame band, no wider than the band and at most 10 px along each arm (6 px on pieces under 32 px); (2) an end cap on a plate, button, strip or banner, at most 8 px long; (3) at most one strap per edge, only on edges of 160 px or more, off-centre; (4) the drawer's runners on its top and bottom bands. At most 2 rivets per cap. Never inside a field, on cells, plates, HP, intent tabs or chips, within 2 px of content, or overhanging the band toward content. **Door and hero cards carry no ornament:** their corners belong to the tier form.
> - **Forms ornament may not take** (3.2, 6.7): a chamfered corner; an inner line or second closed ring inside a frame (the Gold bracket); a row of 4 or more equal-spaced points (rivets, nails, stitches read as serration); a hanging piece; a downward point; a disc or ring 3 px or wider; a diamond; a chevron or diagonal brace. Rivet heads are square, 1 x 1 or 2 x 2. A leather seam is one continuous groove on the lit sides only.
> - **Value limits.** No chrome pixel is brighter than INK-4 (luminance about 0.14), and none falls in the 4.10 lit mask: **0% of any frame is lit**. The top chrome step (INK-4, Candle-D) is limited to the 1 px key-side edges, rivet heads and the top-left faces of caps, and stays under **8%** of opaque pixels on pieces 32 px or larger. Candle-D appears only as isolated 1 to 2 px marks, at most **3%** of a piece.
> - **Steps, outline, detail.** At most three steps per material (shadow, face, key edge), shadows shared from INK-0 and INK-1; a 1 px INK-0 outer outline; detail only in the band, corners and caps, one repeated module per edge. For UI chrome this overrides the supporting-shape column of 3.7 and the supporting-shape step count of 4.13 rule 4 and 8.11; 4.13 rule 5 (no lit pixel on a thing that does not matter) is unchanged; Section 6 still governs backdrops and pedestals.
> - **Fields stay flat:** one colour (INK-0, INK-1, INK-2 or Umber-1); marks only darker than the face, isolated, at most 3%.
> - **Thickness and hierarchy.** Panel band 5 px, tray rim or section line at most 3 px (tray rim 2 px), cell none. The 5 px band plus 2 px tray rim replace the 3 px frame plus 4 px padding, so `n_columns x C + 14` is unchanged. Supersedes the 3 px frame above and the "Panel frame" and "Popup" lines of 8.8.
> - **Red room.** The stage's stone takes the Wine swap as before; wood, leather and the workbench never shift.
>
> *Serves the Section 1 rule:* materials give the chrome character without giving it light. *Pillar:* Pillar 4.

**A.2 In §4.9**, insert after point 5:

> **Amended 2026-11-17 by user decision: real materials, light ornament (3.6).** Points 2 and 5 now read with this material palette: wood INK-0 / Umber-1 / Umber-2 (Candle-D flecks); leather INK-1 / Umber-1 (lit walls Umber-2) / Umber-2; stone INK-0 / INK-1 or INK-2 / INK-3; iron INK-1 / INK-3 / INK-4 (rust Umber-2, one Candle-D fleck per cap at most).
>
> - **Complete chrome palette:** INK-0 to INK-4, Umber-1, Umber-2, Candle-D (restricted, 3.6); W0 to W4 replace INK-0 to INK-4 on the stage's stone in the Red room. **Never chrome:** Fog, Fog-hi, Bone, Ceiling, Candle, Blood, Blood-D, Plum, Plum-E, Moss, Moss-hi, Wine outside the Red-room swap, Red-glow, tier colours. Candle and Bone still appear on interactive states (point 3), which are state, not chrome.
> - **Proposed additions (pending user approval; cap 48):** **Umber-1 `#2B201C`** (luminance about 0.016) and **Umber-2 `#4F3628`** (about 0.044). 41 to 43 colours, 5 reserved slots left. The only existing brown is Candle-D; wood from Wine, Plum, Blood-D or Moss would put the Red-room, curse, lost-HP or poison colours on every panel. If refused, the kit is stone and iron only.
> - **Contrast (hand-computed, NOT VERIFIED):** Bone on Umber-1 about 11.4:1, Fog on Umber-1 about 5.3:1; Fog on Umber-2 about 3.7:1 fails, so no text on Umber-2. INK-4 on Umber-1 is about 2.8:1 (under 3:1), so controls never sit directly on wood or leather.
> - **Generation:** each call gets only its piece's sub-palette, and output is snapped to that subset (still the master palette, 4.13 rule 1).
> - Point 5 otherwise stands.
