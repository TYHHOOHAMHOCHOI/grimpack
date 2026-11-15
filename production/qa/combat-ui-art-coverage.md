# Combat UI art: coverage against the documents

Self-assessment, 2026-11-15. Sources: art bible 3.4, 5.4 to 5.11, 7.3 to 7.12, 8.3 to 8.9;
`design/gdd/combat-hud.md`. Status: **Done** (asset exists, palette and naming validated,
shown in a mock-up), **Partial**, **Missing**. "Mock-up" means a composed 640x360 frame in
`production/qa/evidence/combat_*_2026-11-15.png`, not a Unity scene.

Validation run on all 245 PNGs under `Assets/_Project/Art`: file names match the 8.3 regex,
every pixel is in the master palette, alpha is 0 or 255, nothing darker than INK-0 or lighter
than the ceiling. Not run: lit-area budget (4.10), greyscale and colour-blind checks, the
Vietnamese gate (7.7), any Unity capture of these assets.

| Requirement | Ref | Status | Where |
|---|---|---|---|
| Party slot: pedestal, Candle pool, join numeral, HP row, stat row | 7.4 | Done | `prop_pedestal_*`, `ui_hp-bar_*`, `ui_speed-gauge_*`, numerals |
| Hero frames: idle 2, hurt 2, down 2, narrow-eyes overlay (tank family) | 5.6 | Done | `char_tank_*` |
| Hero frames: warrior and support families, prop overlays and anchor table | 5.2.3, 8.6 | Missing | the tank prop is baked into the body |
| Enemy slot: pennant tab, dark glyph slot, contact shadow, Fog-hi turn chevron | 7.4a, 5.10.8 | Done | `ui_tab-intent_*`, `prop_shadow-contact_*`, `ui_chevron-turn_foe_*` |
| Monster frames: idle 2, act, hurt, die 2, lit-eyes overlays (Swarmer, Brute) | 5.6 | Done (Brute eyes: head region only, unverified by eye at 1x) | `mon_swarmer_*`, `mon_brute_*` |
| Elite, boss, recruit, NPC figures; the off-scale swarm member | 3.3.3, 5.6 | Missing | |
| Order strip, active chip, "if nothing changes" label | 7.5, combat-hud 4 | Done with a conflict: chips are 12 px, the art bible says 14 px, but the top strip is 16 px and the chevron needs 2 px more | `combat_*` mocks |
| Speed control: four segments, selected = ring + 4x4 Candle square | 7.3, 7.11 | Done | `ui_segment_*_9s` |
| Primary action button for planning | 7.3 | Done (code-drawn in the mock) | `combat_planning` |
| Item plates, six tiers, all footprints, rest and hi | 3.4.4 | Done | `plate_*` (114 files) |
| Avatar: lifted 1x1 plate with shadow, lifthi and liftdim | 5.10.2 | Done | `plate_*_lift*_1x1` |
| Avatar queue: two avatars, the finisher one width toward the enemies | 5.10.3 | Done (static) | `combat_party_acting` |
| Item icons, and the 1x1 avatar glyph of multi-cell items | 3.4.2, 8.5 | Missing: placeholders from the glyph set only | |
| Curse tag piece for Gold and Red plates | 3.4.4 | Missing | |
| Glyphs: intent 4, stat 4, status 2, UI 2, 12 px and 8 px, skins | 7.8 | Partial: MVP roster only; guard, buff, debuff and the other statuses wait for the content passes | `ui_glyph-*` |
| Numeral face, 6 cuts with outline | 7.7, 8.9 | Done | `font_grimpack-numeral_num_*` |
| Damage dealt and taken numerals, Blood minus, rising and falling | 4.6 | Done (static) | `combat_*` mocks |
| Stat readout chip: glyph, chevron, signed number | 5.11 | Done (static; the rise and sink is code) | `ui_chip_base_9s`, `ui_chevron-*` |
| HP: Blood on INK-0, lost trail, low-HP Bone outline, downed state | 4.6, 7.4 | Done as pieces; the 200 ms step and the beating heart are code | `ui_hp-bar_*` |
| Fallen hero: dimmed ramp, rim Candle-D, pool out, stat row cleared | 5.6 | Done | `char_tank_down_02`, `prop_pedestal_dim_*` |
| Intent Details in the workbench lower section, 12 entries | combat-hud 6 | Done as layout; content owner is System 26 | `combat_details` |
| Post-combat result panel | 7.5 | Done as layout | `combat_result` |
| Six enemies in two lines (acceptance criterion 16 fixture) | 7.4a | Done as layout | `combat_dense` |
| Shared effects: smear 3, spark 2, trail 2, mote 2, burst 4, foe recolours, burst calm | 8.7 | Partial: crude; calm variants only for the burst | `vfx_*` |
| Rings, threads, bloom, the Candle wash | 5.11, 8.B.5 | Missing: code-drawn in Unity, not started | |
| Victory, Defeat, Draw and technical-stop indicators | combat-hud 4, 2.3.8, 2.3.9 | Missing: needs the Display 24 face and the defeat palette change | |
| Text sizes Heading 16, Display 24, Large set; Vietnamese gate | 7.7, 8.9 | Missing: only the Body 12 cut exists, gate not passed | |
| Stage backdrop for combat (wall plane, void bays, arch) | Section 6 | Missing | the mocks use flat fields |
| Reduced-flash and reduced-motion variants of the effects | 4.12, 7.13 | Missing apart from the burst | |
| Intent glyph travel, avatar arcs, row-shear lean baked frames | 5.10.8, 5.12 | Missing | |
| Unity implementation: canvas, numeral renderer, HP bar, hero and enemy slots, order strip, speed control, HUD view and fixtures | 8.B | Partial: `Assets/_Project/Scenes/Combat.unity` shows four fixture states (captures `combat_scene_*`); no live data, no motion (HP step, avatar queue, intent glyph travel, numerals rising), no input beyond the speed control event, no reduced-motion switch | `Scripts/UI/Hud/*`, `CombatSceneBuilder.cs` |
| Reserved-shape audit (3.2) | 3.2 | Done by construction, not by an automated check | |

## What blocks "complete"
1. Unity: the scene is a static preview of fixtures. The combat presentation (Systems 27 and 29) must
   feed `CombatHudData`; every animation in art bible 5.10 to 5.12 and 7.12 is still to build.
2. Item art: the plate system is finished, but every icon is a placeholder.
3. Fonts beyond Body 12, and the Vietnamese render gate.
4. The remaining character families, elite and boss.
5. Environment art for the stage.

## Items and the item popup (added 2026-11-15)

| Requirement | Ref | Status | Where |
|---|---|---|---|
| 23 MVP items with stats and skills (trigger, effects, passive stats, synergy rules, tags, VI/EN names) | item-trigger-and-cooldown, effect-resolution-model, adjacency-and-synergy, party-and-heroes | Done as data, all numbers provisional | `design/content/mvp-item-catalog.md`, `Assets/_Project/Data/items_mvp.json`, `tools/art/item_catalog.py` |
| Item art at the true size and shape of its footprint (1x1, 1x2, 2x1, 1x3, 2x2, 2x3, L, T, S) | user decision 2026-11-15 | Done: 23 art files plus 15 compact avatar glyphs | `Assets/_Project/Art/Items/Icons/item_*` |
| Plates for the new footprints | 3.4.4 | Done: 174 plate files | `Assets/_Project/Art/Items/Plates/` |
| Hover popup: 300 ms hover, focus opens at once, 60 ms grace, placed once inside the workbench, never touching the source | 7.9, hover-popup-and-glossary 2 to 5 | Done in the scene, captured, not driven by real pointer input yet | `Scripts/UI/Hud/ItemPopup*`, `ItemHoverTarget` |
| Popup content: name, tier in words, shape, trigger, effects with base and boosted numbers, stats, synergy given and received, tags, flavour | hover-popup 6 and 7 | Done (Vietnamese literals) | `Scripts/Items/ItemTooltipText.cs` |
| Synergy evaluation for one hero grid | adjacency-and-synergy | Done as pure code with tests | `Scripts/Items/ItemSynergy.cs` |
| Details page, keyword explanations, first-use hint, held-item name plate | hover-popup 3, 6 | Missing | |
| Heading 16 text cut for the popup title | 7.7 | Missing: the title uses the 12 px face | |
| Item rotation: art rotates with the plate, which flips the top-left light | 3.4.3 | Not decided | |
| Plate hover and focus rings around L, T and S shapes | 7.11 | Missing | |

### Decisions that need sign-off
1. **Art bible 8.5.2 (option A, one compact glyph on every plate) is replaced for items** by art that fills the footprint, at the request of the user on 2026-11-15. The art bible, 3.4.2 and 8.5 need the matching amendment.
2. **Footprints beyond the four of 3.4.2** (1x3, 2x3, T, S) come from `grid-backpack.md`, which allows up to nine cells in a 3 x 3 box. The art bible lists only four.
3. **Target names** `FirstEnemy` and `AllEnemies` are a proposal; Systems 3 and 9 own target selection.
4. The catalog treats the working Ấn numbers (2 damage per unit) as fixture values, as `status-effects.md` does.
