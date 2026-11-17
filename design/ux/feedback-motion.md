# Feedback and Motion Spec: Grimpack

Design note: feedback patterns are generic; no other game's interface informed this spec (user instruction, 2026-11-17).

| Field | Value |
| ---- | ---- |
| Status | Draft for review (technical-artist, 2026-11-17) |
| Goal | Every player action and every combat beat gets a visible, satisfying response, inside the art bible's limits |
| Engine | Unity 6000.6.3f1, UGUI, native canvas 640x360, integer zoom |
| Sources | Art bible §2.3.2, §2.3.4, §2.3.5, §2.4, §2.6, §4.2, §4.4, §4.6, §4.12, §5.10.6, §5.11, §7.11, §7.12, §7.13, §8.3, §8.7, §8.10, §8.B.6; the code listed in §8 |
| Out of scope | Audio implementation (event ids only), palette or emblem design (art-director), gameplay rules (game-designer) |

## 1. How to read this

- Times are ms at 60 fps with frames in brackets: `100 (6f)`. Every duration is a multiple of 1/60 s (§8.4).
- **Easing:** `Step` = hard cuts on the frame grid; `Lin` = linear; `Out` = ease-out quad; `In` = ease-in quad; `Ball` = ballistic (velocity plus gravity). Every value is rounded to whole native px when written (G3). No back or elastic easing: §7.12 says no overshoot.
- **HL** (the reduced-motion fallback): the end state appears at once and the element shows its highlight state for 100 (6f): a 1 px Candle ring for UI, the `plate_{tier}_hi` frame for plates, the counter's numerals for counters. See OQ-4 for why it is 6 frames, not 1.
- **Fade:** a straight stepped fade of the same duration (§7.13). **Calm:** the `-calm` variant under Reduced flashes.
- **P1** = build now. **P2** = next. **done** = already meets this spec.
- "Today" was read from the code on 2026-11-17. "->" separates what exists from what is missing.

## 2. Action table

### 2.1 Planning (grid, stash, drawer)

| ID | Action | Today -> lacks | Trigger | ms (f) | Easing | Sprites | Sound id | Reduced motion | P |
| ---- | ---- | ---- | ---- | ---- | ---- | ---- | ---- | ---- | ---- |
| P-01 | Hover item | No ring. The popup opens after a 300 ms hover (at once on focus) -> needs an instant hover ring | `ItemHoverTarget.OnPointerEnter` / `OnSelect` | ring 0; popup per §7.12 | Step | `ui_ring_hover_9s` (1 px Candle, 1 px gap); `ui_ring_focus_9s` (2 px) | `ui.item.hover` | Same | P1 |
| P-02 | Pick up (click or drag) | Plate moves to the drag layer and jumps 2 px up (`LiftPx`); no shadow or held ring found in code -> needs the lift motion and held state (§7.11) | `ItemBoardController.Pickup(target, byClick)` | lift 100 (6f) | Out | `plate_{tier}_lift_{fp}`, `ui_ring_held_9s` (2 px Candle); origin cells show the empty bed | `ui.item.pickup` | HL: held state at once | P1 |
| P-03 | Drag follow | Integer position at the pointer every frame | `Refresh()` | 1 frame | none | - | - | Same | done |
| P-04 | Rotate | Instant quarter turn (R, right mouse) -> needs the 3-step redraw | `RotateHeld()` | 66 (4f) | Step | none (code redraw) | `ui.item.rotate` | Same: instant | P2 |
| P-05 | Drag over cells | Ghost outline per cell (valid Fog-hi, invalid Fog) and a cross on each blocking cell, 0 ms -> needs "fits here" dots | `Refresh()` on pickup and rotate | 0 | Step | `ui_cell_hint-dot_2x2` (INK-4) on every cell some valid placement covers | `ui.item.cell-valid`, `ui.item.cell-invalid` (on change only) | Same | P2 |
| P-06 | Drop, valid | `Place()` sets the plate at once -> needs a snap, a thunk and an edge pulse | `Drop()` with `PlaceResult.Ok` -> `End()` | slide 83 (5f), 1 px thunk 33 (2f), edge pulse 150 (9f) | Out, then Step | `plate_{tier}_hi_{fp}`; P2: `vfx_drop_dust_01-03` | `ui.item.drop` | HL | P1 |
| P-07 | Drop invalid, Cancel | Board moves the item back and the plate jumps -> needs a return path and a shudder | `Drop()` result not Ok; `Cancel()` | return 150 (9f), 2 px shudder 160 (10f, 2 cycles) | Out; Step | none | `ui.item.drop-invalid` | Plate at origin + HL | P1 |
| P-08 | Pinned (cursed) item refused | `PinnedRefused` event only -> needs feedback on the plate | `ItemBoardController.PinnedRefused` | shudder 160 (10f); tag `hi` 100 (6f) | Step | existing plum fill, pin glyph, Plum-E tag | `ui.item.pinned` | HL | P1 |
| P-09 | Merge / swap | No such rule: dropping on an occupied cell is invalid (P-07) | blocked (OQ-6) | if added: the displaced plate hops to the origin, 150 (9f), 3 hops | Step | none | `ui.item.swap` | HL | P2 |
| P-10 | Sell | `Sell()` hides the plate at once and raises `ItemSold(record, price)`; money jumps -> needs a coin burst and a counter tick | `ItemBoardController.ItemSold` | coin life 500 (30f); counter tick starts at the first landing (about 250) and runs 300 (18f) | Ball; coin spin 67 per frame | `vfx_gold_coin_01-04`; sell-zone `Over` frame (exists) | `ui.sell.burst`, `ui.gold.tick` | No coins; counter set + HL | P1 |
| P-11 | Forge, success | Anvil shows the Strike sprite for 250 ms; the + badge appears; gold jumps -> needs sparks, plate glow, badge pop and a gold tick | `ItemBoardController.ItemUpgraded`, `ForgeZoneView.Strike()` | strike 250 (15f, exists); flash 200 (12f); spark life 300 (18f); plate `hi` 100 on / 100 off / 100 on; badge drops 2 px over 33 (2f); gold tick down 300 (18f) | Step; Ball | `vfx_forge_flash_01-03` (+`-calm`), `vfx_forge_spark_01-02`, `plate_{tier}_hi`, the + badge (exists) | `ui.forge.strike`, `ui.forge.badge`, `ui.gold.tick` | Strike sprite only, badge and counter at once, HL on plate | P1 |
| P-12 | Forge, refused | `frameInvalid`, the item jumps back -> needs return motion | `Forge()` refused -> `ForgeZoneView.Show(Refused)` | as P-07, plus a 2 px zone-frame shudder 160 (10f) | Step | existing `frameInvalid` | `ui.forge.refused` | HL | P1 |
| P-13 | Drawer open / close | Spring with a small bounce on open, critically damped close (user request 2026-11-16); snaps under reduced motion | `SellDrawerView.SetOpen` | spring (exists) | spring, rounded | - | `ui.drawer.open`, `ui.drawer.close` | Snap (exists) | done (sound P2) |

### 2.2 Run flow (reward, buff, curse, doors, corridor, hero select, gold)

| ID | Action | Today -> lacks | Trigger | ms (f) | Easing | Sprites | Sound id | Reduced motion | P |
| ---- | ---- | ---- | ---- | ---- | ---- | ---- | ---- | ---- | ---- |
| R-01 | Take reward | `RewardTaken(index)` -> `TrySpawnItem` places the plate at once -> needs the flight to the grid or stash | `RunPanelView.RewardTaken` | flight 280 (17f), 6 hops, 12 px arc; then P-06 | Step hops | `item_{id}_c_{fp}` (exists), `plate_{tier}_lift_{fp}` | `ui.reward.take`, `ui.item.drop` | Plate at once + HL | P1 |
| R-02 | Claim buff | `ClaimConfirmed` changes max HP and opens cells at once -> needs readouts | `RunPanelView.ClaimConfirmed`; `ItemBoardController.OpenHeroCells(count)` | +HP number rises 600 (36f); max-HP tick 300 (18f); bar grows in whole px over 200 (12f); each new cell 3 frames at 67, stagger 80 (5f), in reading order | Out; Step | `vfx_cell_open_01-03`; stat chip with `ui_chevron-buff_up_6x4` (exists) | `ui.buff.claim`, `ui.cell.open` | Values at once, HL on HP row and new cells | P1 |
| R-03 | Curse applied | `TrySpawnPinned` places the cursed plate at once -> needs the weight of a cost | `ItemBoardController.TrySpawnPinned` (after the claim on a Gold or Red door) | flight from the card's curse tag 280 (17f); heavy 2 px thunk 50 (3f); sink smoke 200 (12f), drifting 4 px down | Step; In on the last hop | `vfx_curse_sink_01-03`; existing plum fill and pin glyph | `ui.curse.apply` | Plate at once + HL on the Plum-E tag | P1 |
| R-04 | Door cards appear | Built at once; a 250 ms commit guard -> needs the §7.12 reveal | `RunPanelView.ShowDoors` | per §7.12: 240 per card, 80 stagger, pips 40 each, tag 120, about 640 total; any click completes it | Out | existing card frames, pips, tag | `ui.door.reveal`, `ui.door.pip` | Fade 240, all cards together | P2 |
| R-05 | Door hover / arm | Hover ring 2 px; armed ring 3 px + 4x4 corner; detail swaps; the corridor previews the tier | `RunPanelView.Hover`, `SetArmed` -> `DoorArmed` | 0; arm 100 (6f) | Step | `ui_door-card_ring-hover_9s`, `ui_door-card_ring-armed_9s`, `ui_door-card_armed-corner_4x4` (exist) | `ui.door.hover`, `ui.door.arm` | Same | done (sound P1) |
| R-06 | Door confirm + walk to the next room | `DoorChosen` -> next state at once; the corridor redraws and its marker jumps -> needs confirm and walk | `RunPanelView.DoorChosen`; new `CorridorView.Advance(from, to)` | other cards to ink 250 (15f, 2 checker steps); chosen card rises 4 px 100 (6f); marker hops 12 px in 3 steps over 240 (15f); path dots light 1 per 80 (5f); the new node stamps its tier with a 1 px thunk 33 (2f); ink curtain 250 out, 250 in | Step; Out | `ui_map-path_dot_2x2`, `ui_map-path_dot-lit_2x2`; existing `ui_map-node_{tier}_10x11`, `ui_map-marker_now_6x2`, the INK-0 checker of `BackpackFx` | `ui.door.confirm`, `ui.map.step`, `ui.room.enter` | Cut to the next room; HL on the new node | P1 |
| R-07 | Corridor hover | Popup with the history list -> needs a node outline and label | `CorridorView.OnPointerEnter` (per-node hit areas later) | 0 | Step | `ui_map-node_ring_12x13` (1 px Candle) | `ui.map.hover` | Same | P2 |
| R-08 | Hero select: focus (new full-screen screen) | `RunPanelView.ShowHeroPick` card row with a ring; the full-screen screen does not exist yet | hover or arrow focus on a hero (new view, class TBD) | ring 0; pedestal pool rises in 3 steps over 150 (9f); hero ready state (1 px lift + narrow eyes, code state exists); unfocus reverses in 100 (6f) | Step | `ui_ring_focus_9s`, `ui_pedestal-pool_step1-3`, `char_{family}_narrow_01` (exists) | `ui.hero.focus` | Ring and pool at final at once | P1 |
| R-09 | Hero select: confirm | `HeroPicked(index)` -> planning at once | `HeroPicked` | other pools out in 3 steps over 250 (15f); chosen hero steps 4 px forward in 2 steps, 100 (6f); name plate drops 8 px in 120 (7f) + 1 px thunk; ink curtain 250 to planning | Step; Out | as R-08 | `ui.hero.confirm` | Cut + HL | P1 |
| R-10 | Gold after a room | Gold changes at once; title note line -> needs a payout | the scene's currency change after a victory (`CombatSceneController.Run.cs`, method TBD) | 6 to 10 coins from the defeated column hop to the gold counter, 5 hops in 250 (15f), stagger 40 (2-3f); the counter ticks as they arrive, 500 at most | Ball, then Step hops | `vfx_gold_coin_01-04` | `ui.gold.gain`, `ui.gold.tick` | Counter set + HL | P1 |

### 2.3 General UI

| ID | Action | Today -> lacks | Trigger | ms (f) | Easing | Sprites | Sound id | Reduced motion | P |
| ---- | ---- | ---- | ---- | ---- | ---- | ---- | ---- | ---- | ---- |
| U-01 | Button hover | `SpriteSwap` in the scene builders; run-panel cards use `Transition.None` with their own rings -> check every button has all 5 states | `Selectable` pointer enter / select | 0 | Step | `ui_btn_*_hover` (1 px Candle ring), `_focus` (2 px) | `ui.button.hover` | Same | P1 |
| U-02 | Button press | Pressed sprite via `SpriteSwap` -> needs the label to drop 1 px | pointer down / `Button.onClick` | Candle fill 50 (3f) (§7.12); label 1 px down while pressed | Step | `ui_btn_*_pressed` | `ui.button.press` | Fill only | P1 |
| U-03 | Disabled button clicked | Nothing -> needs a denial cue | click on a non-interactable button (needs a raycast relay) | 2 px shudder 160 (10f) | Step | `ui_btn_*_disabled` | `ui.button.denied` | HL (focus ring) | P2 |
| U-04 | Panel open / close (`RunPanelView`, post-combat panel) | `SetActive` at once -> needs a slide | `RunPanelView.Begin` / `Hide`; post-combat show | open: from just off the nearest screen edge, 200 (12f); close 120 (7f) | Out; In | - | `ui.panel.open`, `ui.panel.close` | Fade | P1 |
| U-05 | Panel step change (Claim, Reward, Doors) | Content rebuilt at once | `RunPanelView.Begin(step)` | content steps in 8 px from the right, 120 (7f) | Out | - | `ui.panel.step` | Fade 120 | P2 |
| U-06 | Tooltip (item popup) | 300 ms delay, instant swap, placed once | `ItemPopupController` open / close | 100 in, 60 out (§7.12), 3 alpha steps | Step | panel frame (exists) | none (hover has its sound) | Appears at once | P2 |
| U-07 | Result rows (post-combat) | Shown at once | `PostCombatPanelView` show | rows step in top rank first, stagger 40 (2-3f); bars grow 200 (12f) (§7.12) | Step; Out | - | `ui.result.row` | At once | P2 |

### 2.4 Combat

| ID | Action | Today -> lacks | Trigger | ms (f) | Easing | Sprites | Sound id | Reduced motion | P |
| ---- | ---- | ---- | ---- | ---- | ---- | ---- | ---- | ---- | ---- |
| C-01 | Start fight (planning to combat) | Baseline snapshot applied at once; playback starts | `CombatPlaybackController.PlayRoutine`, new pre-roll before the beat loop | planning affordances off at 0; scene light rises at least 250 (15f) (ink curtain, 4 alpha steps, §2.6); enemies step up from 6 px below in 3 steps, 100 (6f) each, stagger 60 (4f), front line first; HP bars fill from 0 over 200 (12f) with numerals ticking; turn marker last; total 600 or less; Skip completes it | Step; Out | none new | `cbt.start`, `cbt.enemy.enter` | Lights at once, HL on HP rows | P1 |
| C-02 | Fight title plate | none | same pre-roll | drops 8 px in 4 steps over 120 (7f) + 1 px thunk; holds 400; steps out 80 | Step | default: panel frame + Heading 16 room text; any emblem is art-director's call | `cbt.title` | Fade | P2 |
| C-03 | Item fires (backpack) | `BackpackFx.Fire`: scale swell to 1.15 + 1 px shake for 300 ms; cooldown checker -> needs the §5.10.9 `hi` pulse and echo | `PresentItem` -> `BackpackFx.Fire` | `hi` for 33 (2f) at the start, then the echo held while the avatar lives | Step | `plate_{tier}_hi_{fp}` | `cbt.item.fire` (+ `.pulse` `.swing` `.throw` `.ring` `.status` `.detonate`) | `hi` only (swell already skipped) | P1 |
| C-04 | Projectile / avatar travel | `ItemAvatarFx` Throw hops with a trail; the enemy intent glyph travels | `ItemAvatarFx.HeroItem` / `EnemyGlyph` | per §5.11 | Step | existing | `cbt.throw`, `cbt.swing` | per §7.13 | done (sound P1) |
| C-05 | Hit | `CombatFx.Hit`: smear (first swing hit), spark, 3 px damped knock 220, two blinks in 150, number (cut 16, 24 on detonation) rises 14 px and blinks out at 600; hit-stop -> needs the number pop and ladder scaling | `CombatFx.Hit` + new ladder level per beat | number pop: next cut for 33 (2f), then settle (§7.12); splat 3 frames at 67 (Chain and up); pixel burst 8 to 12, life 350 (21f) (Surge and up) | Step; Ball | `vfx_impact_splat_01-03` (+`-foe`), `vfx_impact_px_01-02` (+`-foe`) | `cbt.hit`, `cbt.hit.heavy` (Chain and up) | No knock (exists); number at rest; no pixel burst | P1 |
| C-06 | HP change | Fill jumps; the Blood-D trail holds 280 then drains at 70 px/s; the numeral jumps -> needs a stepped fill and a count-down | `HpBarView.SetHp` | fill steps over 200 (12f) (§7.12); numerals tick over 200 (12f) | Lin, whole px | none | `cbt.hp.drain` (P2) | Fill and numeral at once; trail kept | P1 |
| C-07 | Shield gain | `CombatFx.Gain`: "+N" (cut 12) rises, 2 px hop -> needs an armour glint and a count | `CombatFx.Gain` (needs a shield / heal kind) | glint 3 frames, 200 (12f); armour numeral tick 200 | Step | `vfx_shield_glint_01-03` | `cbt.shield` | Number only | P2 |
| C-08 | Heal | Same as shield today -> needs motes and a growing fill | `CombatFx.Gain` (Healed) | 3 to 4 motes rise 12 px over 400 (24f); fill grows 200 | Lin | `vfx_heal_mote_01-02` | `cbt.heal` | Number and fill at once | P2 |
| C-09 | Status applied (chip pop) | `StatusChipView.Apply` shows the chip at once; the amount jumps -> needs a pop and a stack tick | chip goes from none to data, or its amount rises, in `UnitRowsView.Apply` (event name unverified, OQ-9) | chip enters 3 px low and steps up in 3 steps, 50 (3f); the amount steps 1 px up for 2f on each change | Step | existing chip and glyph; `vfx_status_mote` lands on it (exists) | `cbt.status.apply` | Chip at once | P1 |
| C-10 | Status tick: poison | Unverified: shown as a plain number if it arrives as a Damage event -> needs a source cue | status tick event (OQ-9) | puff 3 frames, 200 (12f), at body centre; chip amount ticks down; P2: an 8x8 droplet glyph left of the number | Step | `vfx_poison_puff_01-03` | `cbt.status.poison` | Number only | P1 |
| C-11 | Status tick: burn | As C-10 | burn tick event (OQ-9) | flame 3 frames, 200 (12f), rising 2 px | Step | `vfx_burn_flame_01-03` | `cbt.status.burn` | Number only | P1 |
| C-12 | Freeze | none | freeze applied / ended (status kind unverified) | crust grows in 3 frames over 200 (12f), last frame held while frozen; idle breathing paused; thaw: shatter 2 frames over 133 + 4 pixel chips | Step | `vfx_freeze_crust_01-03` (`-l` for large), `vfx_freeze_shatter_01-02` | `cbt.freeze`, `cbt.thaw` | Last crust frame at once; no shatter | P2 |
| C-13 | Detonation | Detonate avatar; `vfx_detonate_burst` (calm under Reduced flashes); 2 px shake for 180 ms; cut-24 number -> needs the debuffs flying out | `BeatKind.Detonation` -> new `CombatFx.Detonate(target, chips)` | stillness 100 (6f); each chip lifts 2 px, then hops 4 steps to body centre in 120 (7f), stagger 40; the burst plays on the last arrival (exists); chips cleared; number pops | Step | existing chips, `vfx_detonate_burst` (+`-calm`), `vfx_impact_px` | `cbt.detonate.gather`, `cbt.detonate.burst` | Chips vanish; calm burst; number | P1 |
| C-14 | Enemy dies | The `Down` beat has no effect; only the snapshot changes the unit -> needs a death | `BeatKind.Down` with an enemy target -> new `CombatFx.Die(slot)` | die frames 01-02 at 100 each; dissolve 3 baked frames at 67 (200); 12 pixel particles drift up 16 to 24 px over 500 (30f), then blink out; HP row and tab step out in 80 | Step; Lin rise | `mon_{id}_die_01-02` (art, §8.4), `mon_{id}_die_03-05` (baked, OQ-8), `vfx_death_px_01-02` | `cbt.enemy.die` | Die frame, then removed; no particles | P1 |
| C-15 | Hero downed | `HeroSlotView`: Down2 pose and dim pedestal at once -> needs the fall and the light going out | `BeatKind.Down` with a hero target -> new `CombatFx.Down(slot)` | down 1 then 2 at 100 each; 1 px thunk; warm pool out in 3 steps over 300 (18f) | Step | `char_{family}_down_01-02` (exist), `ui_pedestal-pool_step1-3` | `cbt.hero.down` | Down2 and dim at once | P1 |
| C-16 | Victory / defeat banner | `PlaybackCompleted` -> post-combat panel at once -> needs a banner | `CombatPlaybackController.PlaybackCompleted` | defeat: pools go out one hero at a time, 400 each (§2.3.9, the one slow change); banner grows from 16 px to full width in 8 steps over 240 (15f), centred; title (Display 24) lands with a 1 px thunk; victory: Candle wash rises over 300 or more (§4.12 rule 3) | Step; Out | `ui_banner_victory_9s`, `ui_banner_defeat_9s` | `cbt.victory`, `cbt.defeat` | Banner at full width at once | P1 |

## 3. Global rules

| ID | Rule |
| ---- | ---- |
| G1 | **Frame grid.** Author every duration at 60 fps (100 ms = 6f). Flipbooks run at 15 fps (67 ms per frame, §8.10). |
| G2 | **Clock.** All motion uses `Time.unscaledTime` / `unscaledDeltaTime`, as `CombatFx`, `BackpackFx` and `SellDrawerView` already do. Combat effects scale holds and cadence by the playback speed (1x, 2x, 4x); luminance rises keep their 100 ms floor (§5.10.7). Planning and UI motion are never speed-scaled. |
| G3 | **Pixel snap.** Every animated `anchoredPosition` and `sizeDelta` is a whole native px on the 640x360 canvas. Compute `start + offset(t)` and round once per frame; never accumulate rounded values (no drift). `localScale` stays 1 and rotation only turns in 90-degree steps. Size changes go through 9-slice `sizeDelta` or sprite swaps. The one exception is the existing `BackpackFx` 1.15 swell (OQ-1). |
| G4 | **No overshoot in new motion** (§7.12). A "pop" is a position step plus a 1 px thunk, or one numeral cut up for 2 frames. A "bounce" is a 1 px or 2 px thunk. The `SellDrawerView` spring keeps its bounce (user request 2026-11-16). |
| G5 | **Alpha.** No partial alpha on stage sprites: flipbooks and numbers end by blinking out. Whole-group fades (`CanvasGroup`) are allowed for popups, panels and the ink curtain, in 3 or 4 steps. |
| G6 | **Particles.** Particles are 1x1 to 4x4 px. Per-effect caps are in §4. Screen budget from this spec: 40 live in planning and 100 live in combat, inside the art target of **200 live VFX sprites** (§8.10, which also counts avatars, chips, rings and threads). Hard cap **500** at the Detonation peak (§8.10, §8.B.6). |
| G7 | **Input never waits more than 150 ms on motion.** Logic changes state at the trigger, as it does today; motion only presents the new state. Any input during a sequence first completes it (`UiTween.CompleteAll(scope)`) and is then handled. A press that would commit (Confirm, Take) only completes the sequence, to stop misclicks. Picking up a plate in the middle of a snap completes the snap first. |
| G8 | **Skippable.** Every sequence has `Complete()`. Click, Confirm, Esc and the combat Skip control jump to the end state. |
| G9 | **Reduced motion** (`GameSettings.ReducedMotion`, exists): motion becomes HL (§1). There is no travel, shake, knock, shudder or particles in flight. Snaps still snap, and numbers and chips appear at their rest positions. The table's "Reduced motion" column gives the fallback for each row. |
| G10 | **Reduced flashes** (`GameSettings.ReducedFlashes`, exists): `-calm` variants, no particle bursts, no hit blink (exists), rings at final radius in 3 steps (§4.12 item 6). Recommended on by default until the first playtest (§4.12). |
| G11 | **Flash limits** (§4.12, a proposal, NOT VERIFIED; `design/accessibility-requirements.md` does not exist yet): at most 2 flashes per second for the Detonation and the Red room, and 3 per second anywhere else. A flashing region is at most 160x160 native px (0.111 FSE), summed over everything that flashes in the same frame, so at most 3 bodies blink in one frame (keep the existing one-blink-per-250-ms guard per unit). No alternation to or from saturated red. Every luminance rise eases over 6 frames or more. Candle washes ramp over 300 ms or more and change luminance by 0.08 or less. Plate `hi` pulses: at most 2 per plate per second. Nothing new uses the light ceiling `#F2E8CE`, which stays reserved for the bloom core. The Detonation and the avatar chain at 4x are captured and run through an analyser before any build ships. |
| G12 | **Escalation ladder** (§2.3.5, §5.10.9): Tick stays quiet (shared spark, no particles). Splats start at Chain. Pixel bursts and washes start at Surge. Planning effects are energy 2: small and dim. |
| G13 | **Colour.** Party effects use the Bone set and enemy effects the Fog-hi set (§8.7). Candle means currency and "you". Blood is for HP only, so splats are not red. Plum is for the curse only. Moss is ink-class and never lit. Tier hue appears only on plate edges and rings, never on sparks or splats. |
| G14 | **Sound hooks.** Every id in §2 is raised through `FeedbackHub.Sound(id)`. UI never calls audio directly (`.claude/rules/ui-code.md`). Audio subscribes later. |
| G15 | **Display only.** Feedback reads events and snapshots and never changes game state, as `CombatFx` already does. |

## 4. Burst caps and performance

| Effect (row) | Max live | Life | Notes |
| ---- | ---- | ---- | ---- |
| Sell coins (P-10) | 12 | 500 | count = clamp(ceil(price / 2), 3, 12); up 140 to 180 px/s, sideways ±50 px/s, gravity 700 px/s², lands 10 px below the zone mouth |
| Gold after a room (R-10) | 10 | 750 | Ball for 250, then hops |
| Forge sparks (P-11) | 10 | 300 | up and out from the anvil top |
| Hit pixel burst (C-05) | 12 per hit, 3 hits at once = 36 | 350 | Surge and up only |
| Detonation pixels (C-13) | 24 | 400 | plus the existing burst flipbook |
| Death pixels (C-14) | 12 per death, 2 at once = 24 | 500 | |
| Heal motes (C-08) | 4 | 400 | P2 |
| Flipbooks (splat, puff, flame, crust, flash) | 1 per unit per effect | 200 | from `FlipbookPool` |

- **Budgets used.** These are the art bible's proposals (§8.10, §8.B.6), not ratified numbers, and are unmeasured. The `project.yaml` performance block was not re-read for this draft. Confirm in the Frame Debugger and the Profiler.
- **Canvas rebuilds (frame time).** Put the combat FX layer and the planning drag and FX layer on their own nested `Canvas`, so moving images rebuild only that sub-canvas. Cost: about one extra batch per sub-canvas.
- **Draw calls.** Pack every VFX frame in one `vfx` Sprite Atlas (Point filter, no mips). Keep sprite numerals in a sibling group after the FX, so atlas switches do not interleave and break batches.
- **CPU and GC.** Prewarm pools to the caps above at scene build, with no `Instantiate` in play. One runner `Update` drives all tweens, using struct arrays with no per-frame allocation and no coroutine per particle. Every FX image has `raycastTarget = false`. Toggle `Image.enabled` for particles rather than `SetActive`.
- **Fill.** Fill does not bind: 1 to 4 px particles cost 0.01 to 0.04 FSE (§8.B.6). Count and CPU bind.
- **Engine.** No post-cutoff API is used: nested `Canvas`, `CanvasGroup`, `Image` and `RectTransform` are long-standing UGUI. That UGUI batching is unaffected by 6.6's removal of dynamic batching is my understanding, NOT verified against `docs/engine-reference/unity/`. Confirm it in the Frame Debugger.

## 5. Implementation plan: building blocks

Plain C#, coroutine-free, `Update`-driven, no third-party packages (no DOTween). Proposed folder: `Assets/_Project/Scripts/UI/Motion/` (needs approval). Owner: ui-programmer, with technical-artist review. No gameplay code is touched.

| # | Block | New or extracted | Does | Feeds rows | EditMode tests (`Assets/Tests/EditMode/UI/Motion/`) |
| ---- | ---- | ---- | ---- | ---- | ---- |
| B1 | `PixelEase` (static, pure) | new | `Lin`, `Out`, `In`, `Step(n)`, `Ball(v, g, t)`, `Snap(Vector2)` to whole px | all | exact endpoints, monotonic, integer output |
| B2 | `UiTween` + `UiTweenRunner` | new | Struct tweens on `anchoredPosition`, `sizeDelta` (9-slice), stepped `CanvasGroup` alpha and sprite sequences; unscaled clock; speed source (1 for UI, playback speed for combat holds); `Complete(owner)` and `CompleteAll(scope)`; reduced-motion policy per tween (HL, Fade, Same); a debug assert if any tween blocks input for more than 150 ms | P-02 to P-12, R-*, U-*, C-01, C-15, C-16 | Complete jumps to the end; no drift; the 150 ms assert |
| B3 | `HighlightRing` (+ `PlatePulse`) | new | 9-slice Candle ring outside a `RectTransform` with a 1 px gap, 1 or 2 px wide; `Pulse(ms)` is the HL fallback; `PlatePulse` swaps a plate to `hi` for N frames | P-01, P-06, P-08, U-01, R-08, C-03, every HL | ring rect math in whole px |
| B4 | `NumberTicker` (+ pure `TickerMath`) | new | Counts an int from one value to another over a duration with at most 10 visible updates (the last is exact); drives `SpriteNumeralText` or TMP; raises the tick sound per update | P-10, P-11, R-02, R-10, C-01, C-06, C-07 | value at t, exact final, update cap |
| B5 | `PixelBurst` + `BurstProfile` (ScriptableObject in `Assets/_Project/Data/Fx/`) | new | Pooled images; integer ballistic motion; per-particle frames; blink-out end; seeded `System.Random` per burst; per-profile and global caps; none under Reduced flashes | P-10, P-11, R-10, C-05, C-08, C-13, C-14 | same seed gives the same path; integer positions; caps hold |
| B6 | `FlipbookPool` | extracted from `CombatFx.Play` | 15 fps flipbooks centred on a point, pooled, usable in planning too | P-11, R-02, R-03, C-05, C-10 to C-12, C-14 | frame index at t |
| B7 | `HopPath` | extracted from the `ItemAvatarFx` Throw hops | N whole-pixel hops from A to B with a stepped arc | R-01, R-03, R-10, C-13, P-09 | hop count, integer points, ends exactly at B |
| B8 | `FeedbackHub` + `FeedbackIds` | new | `SoundRequested(string id)` event; one constant per id in §2 | all | ids unique |

**Build order**
1. B1, B2, B8: the foundation, with no visuals yet.
2. B3: P-01, U-01, U-02, and the HL fallback every later row uses.
3. Planning motion in `ItemBoardController`: P-02, P-06, P-07, P-08, P-12. Logic stays instant; `Place()` gets an `animate` flag and the plate's visual position tweens.
4. B4, B5, B6: P-10 (the `ItemSold` listener and the gold counter, whose class is TBD), P-11 (`ForgeZoneView.Strike` extended), C-06 (`HpBarView`).
5. B7: R-01, R-02, R-03, R-10. `TrySpawnItem`, `TrySpawnPinned` and `OpenHeroCells` take an optional "from" point in `ItemBoardController`; `RunPanelView` supplies the card rect.
6. Run flow: R-06 (`CorridorView.Advance`), R-08 and R-09 (the new hero-select screen), U-04.
7. Combat:
   - C-03 (`BackpackFx` `hi` pulse)
   - C-05 (`CombatFx` number pop, plus a ladder level on `PlaybackBeat` with thresholds as data, placeholders for game-designer)
   - C-09 (`StatusChipView` diff), C-10, C-11
   - C-13 (`CombatFx.Detonate`)
   - C-14 and C-15 (a `Down` case in `CombatPlaybackController.Present`)
   - C-01 (pre-roll), C-16 (banner)
8. All P2 rows.

**Evidence:** each P1 row closes with a retained capture in `production/qa/evidence/` (coding standards: a parse check is not a run), plus the B1 to B8 EditMode tests. The C-13 Detonation and a 4x avatar chain go through the §4.12 item 7 capture.

## 6. VFX and UI sprite list

Frames are authored on a 3-colour mask ramp and recoloured offline (§8.7), each with at least a 1 px transparent margin (§8.4). Party = Bone set, enemy = `-foe` Fog-hi set.

| Name (8.3 pattern) | Frames | Canvas (native px) | Palette ramp | Rows | P |
| ---- | ---- | ---- | ---- | ---- | ---- |
| `vfx_impact_splat_01-03` + `vfx_impact_splat-foe_01-03` | 3 + 3 | 32x32 | Bone `#E4DAC0`, Fog `#8795B3`, INK-3 `#36415F` / Fog-hi `#AAB8D4`, Fog, INK-3 | C-05 | P1 |
| `vfx_impact_px_01-02` + `-foe` | 2 + 2 | 4x4 (2x2 and 1x1 content) | Bone / Fog-hi | C-05, C-13 | P1 |
| `vfx_gold_coin_01-04` (spin) | 4 | 8x8 (6x6 coin) | Candle `#EFC186`, Candle-D `#7C5C3C`, INK-0 `#0B0F1C` edge, 1 px Bone glint | P-10, P-11, R-10 | P1 |
| `vfx_forge_flash_01-03` + `vfx_forge_flash-calm_01` | 3 + 1 | 24x24 | Bone, Candle, Candle-D | P-11 | P1 |
| `vfx_forge_spark_01-02` | 2 | 4x4 | Candle, Bone | P-11 | P1 |
| `vfx_cell_open_01-03` | 3 | 20x20 (full-bleed cell, C = 20) | INK-2 `#222B44`, INK-3, Candle-D corners, 1 px Candle corner ticks | R-02 | P1 |
| `vfx_curse_sink_01-03` | 3 | 16x16 | Plum `#2A1830`, Plum-E `#8A5C96`, INK-1 `#151B2E` | R-03 | P1 |
| `vfx_poison_puff_01-03` | 3 | 16x16 | Moss `#2E3820`, `#4A5E34`, INK-2 edge (never lit) | C-10 | P1 |
| `vfx_burn_flame_01-03` | 3 | 16x16 | Burn status ramp (OQ-3) | C-11 | P1 |
| `vfx_death_px_01-02` | 2 | 4x4 | Fog-hi, Fog, INK-4 `#5A6785` | C-14 | P1 |
| `mon_{id}_die_03-05` (baked, per monster) | 3 per monster | the monster's canvas class | the monster's own palette; generated by a pipeline script (ordered 2x2 dither erase at 25, 50 and 75%), no hand art | C-14 | P1 |
| `ui_ring_hover_9s`, `ui_ring_focus_9s`, `ui_ring_held_9s` (if not already present) | 1 each | 9-slice, 8x8 source | Candle, 1 px gap / 2 px / 2 px | P-01, P-02, U-01, R-08 | P1 |
| `ui_map-path_dot_2x2`, `ui_map-path_dot-lit_2x2` | 1 + 1 | 2x2 | INK-4 / Candle | R-06 | P1 |
| `ui_pedestal-pool_step1-3` | 3 | pedestal width x 8 (size TBD from the pedestal) | Candle-D, Candle | R-08, R-09, C-15 | P1 |
| `ui_banner_victory_9s`, `ui_banner_defeat_9s` | 1 + 1 | 9-slice, 24x24 source | victory: INK-2 bed, 1 px Candle inner line, INK-4 frame; defeat: INK-1 bed, Fog line | C-16 | P1 |
| `vfx_drop_dust_01-03` | 3 | 24x8 | INK-4, INK-3, Fog | P-06 | P2 |
| `vfx_shield_glint_01-03` | 3 | 16x16 | Fog, Fog-hi, INK-4 | C-07 | P2 |
| `vfx_heal_mote_01-02` | 2 | 4x4 | Bone, Fog | C-08 | P2 |
| `vfx_freeze_crust_01-03` + `vfx_freeze_crust-l_01-03` | 3 + 3 | 32x16 / 48x16 | Fog-hi, Fog, 1 px Bone glint, INK-3 edge | C-12 | P2 |
| `vfx_freeze_shatter_01-02` | 2 | 24x24 | Fog-hi, Fog, Bone | C-12 | P2 |
| `ui_cell_hint-dot_2x2` | 1 | 2x2 | INK-4 | P-05 | P2 |
| `ui_map-node_ring_12x13` | 1 | 12x13 | Candle 1 px | R-07 | P2 |

**Totals.** New VFX frames: 35 at P1 (impact splat 6, impact px 4, coin 4, forge flash 4, forge spark 2, cell open 3, curse sink 3, poison puff 3, burn flame 3, death px 2, plus the 1 calm frame counted in forge flash) and 19 at P2. Baked die frames are 3 per monster, generated. UI sprites: about 15.

**Reused, no new art:** `vfx_swing_smear` (3), `vfx_impact_spark` (2), `vfx_throw_trail` (2), `vfx_status_mote` (2), `vfx_detonate_burst` (4, + calm), `plate_{tier}_hi` / `lift`, the stat chip and chevrons, the + badge, the door-card rings, the map nodes and marker.

## 7. Open questions (decisions not mine)

| # | Question | Default in this draft | Owner |
| ---- | ---- | ---- | ---- |
| OQ-1 | §7.12 bans smooth scaling and overshoot. `BackpackFx` swells to 1.15 (resampled, so mixels appear) and the drawer spring bounces; both were user requests on 2026-11-16. | Keep both. New motion obeys §7.12. Alternative for the swell: `hi` plate + 2 px lift. | art-director, user |
| OQ-2 | Should a single (Tick) hit get a splat? §2.3.4 says small triggers stay quiet. | Tick = spark only; splat from Chain; particles from Surge | art-director |
| OQ-3 | Burn and freeze hues: §4.6 has a status-chip colour amendment that was not read for this draft. Burn must not use Red-tier red or Blood. | Use the status's chip ramp | art-director |
| OQ-4 | The brief's "1-frame highlight" for reduced motion: a single-frame full-intensity cut conflicts with §4.12 rule 5. | Highlight held 6 frames (100 ms) | UX, accessibility |
| OQ-5 | `RunPanelView` blocks commits for 250 ms after each step appears, which is over the 150 ms rule. | Cut it to 150 ms, or keep it as a design exception | ux-designer |
| OQ-6 | Merge and swap do not exist in the `ItemBoard` rules. | P-09 blocked | game-designer |
| OQ-7 | §8.3's `vfx` vocabulary lists only `swing throw status detonate impact` / `smear spark trail mote burst`. | Add names `gold forge drop cell curse poison burn freeze heal shield death` and variants `splat px coin dust flash open sink puff flame crust shatter glint` | art-director, validator |
| OQ-8 | Baked die frames 03-05 go beyond the locked 2 die frames (§5.6). They are generated, not drawn. | Allowed as generated frames | art-director |
| OQ-9 | Event names for status applied, status tick and freeze were not checked in `CombatEventKind`. | Wire to whatever the trace emits; if none exists, gameplay-programmer adds it | gameplay-programmer |
| OQ-10 | The flash limits are a proposal; `design/accessibility-requirements.md` does not exist. | Treat G11 as binding for this spec | ux-designer |

## 8. Code read for "Today" (2026-11-17)

`UI/Hud/CombatFx.cs`, `BackpackFx.cs`, `HpBarView.cs`, `ItemBoardController.cs`, `ItemHoverTarget.cs`, `SellDrawerView.cs`, `ForgeZoneView.cs`, `HeroSlotView.cs`, `StatusChipView.cs`; `CombatScene/CombatPlaybackController.cs`, `RunPanelView.cs`, `CorridorView.cs`. `PostCombatPanelView.cs`, `ItemAvatarFx.cs`, `ItemPopupController.cs` and `SellZoneView.cs` were read through their summaries only. All paths are under `Assets/_Project/Scripts/`.
