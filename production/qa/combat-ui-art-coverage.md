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

## Drag, drop and rotate (added 2026-11-15)

| Requirement | Ref | Status | Where |
|---|---|---|---|
| Pick up by drag, or click then click to place | 7.13 | Done in code; verified only through scripted captures, not with a real pointer | `ItemBoardController`, `ItemHoverTarget` |
| Rotate: R, right mouse button; keyboard path Enter, arrows, Tab, Esc | 7.13, backpack-ui | Done in code, same caveat | `ItemBoardController.Update` |
| Placement rules: inside the container, on free cells; invalid drop returns the item | grid-backpack 5, 7.12 | Done as pure code with 12 tests | `Scripts/Items/ItemBoard.cs`, `ItemShapes.cs` |
| Ghost footprint: Fog-hi outline when valid, Fog outline and a cross on blocking cells when not | 7.11 | Done, captured | `ui_ghost_*`, `combat_scene_drag_*` |
| Synergy and popup follow the arrangement, rotation included | adjacency-and-synergy | Done: popup reads the board, adjacency uses the rotated footprint | `ItemBoard.Evaluate` |
| Popup suppressed while an item is held | hover-popup 4 | Done | `ItemPopupController.Suppressed` |
| Snap, thunk, tier-edge pulse, invalid-release shudder, 100 ms lift | 7.12 | Missing: placement is instant, only the 2 px lift exists | |
| Held plate ring and 2 px shadow, rotate prompt chip, gamepad | 7.11, 7.13 | Missing | |
| Moving items between the three hero grids and a stash of the documented 5 x 5 size | grid-backpack, backpack-ui | Missing: one hero grid (4 x 4) and an 8 x 2 stash | |
| Rotating a plate rotates its art, so the top-left light flips | 3.4.3 | Accepted for now, needs art-director sign-off | |

## Buttons (added 2026-11-16)

Spec: `design/ux/combat-screen-buttons.md`. Presentation only; playback is a fixture timeline.

| Requirement | Ref | Status | Where |
|---|---|---|---|
| Start (primary button, Planning only, off while an item is held), Ctrl+Enter | 7.3, input-and-focus 6 | Done, driven in Play Mode | `ActionStripView`, `CombatScreenController.TryStart` |
| Speed 1x, 2x, 4x (keys 1, 2, 3), remembered; progress of the beat kept | payoff-presentation 8, 19 | Done, logic tested | `CombatScreenModel`, `DemoPlayback`, `GameSettings.SpeedIndex` |
| Skip (key K): refused inside 250 ms, never saved, label "Đang bỏ qua" while armed | payoff-presentation 11, 12, input-and-focus 7e | Done, driven in Play Mode (refused in lock, armed after, landed on Planning) | `SpeedControlView.SetSkipArmed` |
| Menu button and pause menu: Resume, Settings, Abandon run; stage dimmed by the checker mask, workbench stays readable | menus-and-screen-flow 5 to 7, art bible 2.3.1 | Done, captured | `PauseMenuView`, `ui_dim-mask_default_4x4`, `combat_scene_buttons_pause-menu_z2` |
| Abandon confirmation: Keep playing has initial focus; Esc keeps playing; Abandon loads Title | menus-and-screen-flow 7 | Done, driven in Play Mode | `ConfirmDialogView`, `combat_scene_buttons_confirm_z2` |
| Settings panel: reduced flashes, reduced motion, Skip to end, each written in words; Back | settings 2 to 6 | Done for three settings, captured; saved by a stand-in store | `SettingsPanelView`, `GameSettings`, `combat_scene_buttons_settings_z2` |
| Esc priority: confirm, settings, popup, held item, menu, open menu | input-and-focus 4 | Done, tested | `CombatScreenModel.Escape` |
| Title: Chơi loads Combat, Cài đặt opens the same panel, Thoát quits | menus-and-screen-flow | Done, Chơi and Cài đặt driven in Play Mode; Thoát not exercised | `TitleMenuController` |
| Modal traps pointer input; focus returns to the opener | input-and-focus | Pointer blocked by CanvasGroups; focus return done for the menu button only | `CombatScreenController.SyncModals` |
| Per-segment prompt chips (1, 2, 3, K), menu button ring outside the frame | payoff-presentation 19, 7.11 | Missing (Tab chip removed) | |
| Language, volume, key repeat, rebind, text size; first-launch Reduced flashes prompt; pause on window deactivation | settings, menus-and-screen-flow 5 | Missing (need Systems 6, 7, 31, 37) | |
| Gamepad for these buttons | ui-code rules | Not tried: buttons use the EventSystem, so navigation exists, but nothing was run with a gamepad | |

## Sell zone, enemy centring, centred modals (added 2026-11-16, user request)

| Requirement | Status | Where |
|---|---|---|
| Sell zone under the stash: scale, framed drop area, hint, price of the held item; drop sells, Tab reaches it from the keyboard | Done, driven in Play Mode (item removed, currency +price); Planning only, refused during playback | `SellZoneView`, `ItemBoardController.Sell`, `ui_sell-*`, `combat_scene_drag_sell_z2` |
| Sell prices | Provisional table by tier (1, 2, 3, 5, 8, 12) on the board controller; Currency and Shop (System 20) is not designed, so this needs an economy decision | `ItemSale.Price` |
| No undo for a sale | Open: a drop on the zone sells at once | |
| One enemy line stands in the middle of the enemy column; two lines share it | Done, captured | `CombatHudView.LayoutEnemies` |
| Pause menu, confirm and settings centred on the screen; the whole frame is dimmed | Done, captured. Deviates from art bible 2.3.1 (only the stage dimmed); needs the art bible amended | `CombatSceneBuilder.BuildScreenController` |

## Start button, cursor, popup swap (added 2026-11-16, user request)

| Requirement | Status | Where |
|---|---|---|
| Start is the one filled button (Candle plate, dark text), at the right of the strip, Ctrl+Enter chip to its left, the reason it is off written at the left | Done, captured, driven in Play Mode (holding an item turns it off and shows the reason) | `ui_start_*`, `ActionStripView.SetStartEnabled` |
| Pixel cursor: arrow, and a closed hand while an item is held; pre-scaled for zoom 1 to 4 | Done on Combat and Title; the look of the hardware cursor was not checked on screen | `PixelCursor`, `Art/UI/Cursors/` |
| Popup swap between items without waiting or blinking: rows pooled (about 1 ms a swap), 0.2 s grace, 0.5 s warm window | Done, driven in Play Mode. Grace deviates from hover-popup-and-glossary rule 3 (60 ms); the GDD needs the matching change | `ItemPopupController`, `ItemPopupView`, `HoverTiming` |

## Top strip, backpack, sell drawer (added 2026-11-16, user request)

| Requirement | Status | Where |
|---|---|---|
| Top strip: the label "Lượt tới (dự kiến):" comes before the order chips; enemy chips carry the enemy's letter, which its stage slot also shows; cog on the menu button with an Esc chip | Done, captured | `OrderStripView`, `EnemySlot` letter chip, `ui_glyph-cog_bone_8x8` |
| Stash 8 x 8; hero grid 4 x 4 at the start, ready to grow to 7 x 7 (the stash moves down; the column is exactly full at 7 x 7) | Done; growth driven in Play Mode (`ResizeHeroGrid(7)`), refused if it would cut an item | `WorkbenchLayout`, `WorkbenchView`, `ItemBoard.Resize` |
| Sell moved into a drawer on the right edge of the stage: a tab with an arrow at the top opens it with a spring (about 20 px overshoot, then settles), the tab again tucks it away without a bounce; reduced motion moves it at once | Done, driven in Play Mode (path 460, 365, 304, 279, 272, 277 ... 292) | `SellDrawerView`, `DrawerSpring`, `ui_drawer-tab_*` |
| Carrying an item over the closed tab opens the drawer; Tab from the keyboard reaches it too; a carried plate draws above the drawer | Done, driven in Play Mode | `ItemBoardController` (drag layer) |
| The open drawer covers part of the enemy column | Open: acceptable while it is closed by default; revisit with the art director | |

## Combat playback motion, new item art, smoother playback (added 2026-11-16, user request)

| Requirement | Status | Where |
|---|---|---|
| The 11 items added with CombatCore (spiked buckler, oak buckler, field bandage, sweeping halberd, runed mace, crimson vial, guard sigil, whirlwind axe, oath blade, sanctuary relic, resonant bell) redrawn with Pixellab (pixen, 2x the plate bed, transparent), plus a compact glyph for each multi-cell one; the code-drawn placeholders are replaced | Done, through `build_items.py` (palette, outline); thin handles are now kept by the outline step | `art-source/ai-raw/item_*_raw.png`, `items_sheet_2026-11-15.png` |
| Hero attack key frames: wind-up (sword raised) and strike (sword swung forward), edited from the idle with Pixellab, processed by the hero rule | Done | `char_tank_attack_01/02` |
| Attack animation: 2 px pull-back, 12 px lunge (enemies 10 px) with the attack frames, contact hold, return | Done, traced in Play Mode (hero x 247, 244, 259, back to 247) | `CombatFx.Lunge` |
| Hit reactions: swing smear on the first hit, impact spark, 3 px knock-back with a damped wobble, two quick blinks (once per burst), floating numbers that fan out, HP trail that holds then drains, detonation burst and a 1-2 px shake | Done, captured | `CombatFx.Hit`, `HpBarView`, `combat_playback_*_z3_2026-11-16.png` |
| Less turn-based: beats come from `PlaybackTimeline` (next actor starts 0.2 s after the last beat, hits 0.11 s apart, detonations get 0.14 s more), 45 ms hit-stop at 1x/2x, speed changes keep progress | Done, 6 timeline tests | `PlaybackTimeline`, `CombatPlaybackController` |
| Smoother frames: numeral digits are pooled and share parsed metrics and glyph sprites (before, every HP change destroyed and created objects and re-parsed JSON); playback numbers are pooled instead of rebuilt on every HUD apply | Done | `SpriteNumeralText`, `CombatHudView.DrawReadouts` |
| Reduced motion removes lunge, knock-back, shake and number drift; reduced flashes removes the blink and uses the calm burst | Done in code, not captured | `CombatFx` |
| Open: at the start of a fight many reactions land together and numbers still stack; the whirlwind axe art is dark after quantising; enemy attacks have no wind-up frame of their own (they use the Act pose) | Open | |

## Item avatars, weapon archetypes, backpack pulse and cooldown (added 2026-11-16, user request)

| Requirement | Ref | Status | Where |
|---|---|---|---|
| Slower pacing with time to read each item: hero ready 200 ms, avatar pop-in 100 ms, read hold 180 ms, then the archetype; next avatar after 60% of the previous travel; 400 ms hand-off between actions; 130 ms between hits of one source | 5.10.6, user | Done, 7 timeline tests | `PlaybackPacing`, `PlaybackTimeline` |
| Each weapon type has its own motion (archetype from the item's effects and tags): Swing (melee: stepped arc, smear, hero attack frames), Throw (crossbow, charms: hops with trail), Status (seal items: mote hops), Ring (shield, heal: code-drawn ring grows in the tier colour), Detonate (bells, censer: pulse and burst), Pulse | 5.11, 5.12 | Done, captured | `ItemAvatarFx`, `CombatPlaybackController.ArchetypeOf` |
| Avatar: the item's 1x1 tier plate with its compact glyph, over the hero's head; enemy intent glyph flies from its tab to the hero | 5.10.2, 5.10.8 | Done, captured | `combat_playback_avatar_z3`, `combat_playback_enemy-glyph_z3` |
| Backpack: a 2 px gap is drawn wherever two items touch | user | Done | `ItemBoardController.RebuildSeparators` |
| Backpack: a firing item swells to 115% and shakes 1 px, then a checker dim covers the share of its cooldown still to go and drains as actions pass; ready items show normally | user, 5.10.9 | Done, captured. The swell is a non-integer scale (user request, deviates from 8.1) | `BackpackFx`, `combat_playback_cooldown_z3` |
| Hero damage numbers moved into the body band; the zone over the head is the avatar's | 5.10.3 | Done | `CombatFx.ShowNumber` |
| Not built: avatar lean by row shear, Chain/Surge ladder, 4x avatar groups, Skip stopping at each Detonation, chip aggregation within 300 ms | 5.10.7, 5.10.9, 5.12 | Open | |
