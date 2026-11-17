# Redesign after Play, balance, three-item start, feedback motion: build notes and evidence (2026-11-17)

User request: redo the screen after Play, balance the monsters, remove the extra equipment (each hero starts with 3
items), add the missing reaction animations, redesign the interface from generated components without copying the
reference video's interface.
Specialists: ux-designer (hero select, planning screen), art-director (generated kit, art bible amendment),
technical-artist (feedback motion), systems-designer (kits and balance targets).
User decisions: hero select is its own screen; real materials with light ornament; difficulty "Vừa"; stash 8 x 4;
enemy preview replaces the right panel; 96 px heroes drawn with Pixellab; locked heroes hidden; Umber-1/2 added to the
palette; primary button = dark wood face with a Candle rim; warm wood/leather for the player, cold stone for the dungeon.

## What is built

| Area | Where |
|---|---|
| Balance simulator (engine-free CombatCore under dotnet, room x tier matrix + full runs with bots) | scratchpad only (not in the repo); numbers recorded in `design/quick-specs/balance-three-item-start.md` |
| Monster stats, one-hero rosters, boss, Alchemist HP 94, Crusader shield 4 | `Data/monsters_mvp.json`, `Data/run_mvp.json`, `Data/heroes_mvp.json` |
| 3-item kits at authored cells (`startingItems` entries with x, y, rot), empty stash | `HeroDef.StartingLayout`, `GameCatalog`, `ItemBoardController.TrySpawnItem(atX, atY, atRot)`, `CombatSceneController` |
| Generated UI kit (Pixellab, palette-locked; panels, trays, boards, plates, pedestal, backdrops, 96 px heroes, rings, particles) | `tools/art/build_generated_ui.py`, `tools/art/ui_gen_manifest.json`, `art-source/ai-raw/ui/`, `Art/UI/Gen`, `Art/VFX/Gen` |
| Hero select screen (hero-select.md) | `CombatScene/HeroSelectView.cs` |
| Planning screen reskin: wood workbench, stone stage frame over a generated backdrop, leather trays, stash 8 x 4 with hint, innate plate, room plate, wood start button, F9 debug picker | `CombatSceneBuilder`, `ModalKit`, `WorkbenchLayout`/`WorkbenchView`, `CombatSceneController(.Run)` |
| Run steps as an overlay on the enemy zone, Confirm/Skip in the action strip, stone-slab cards | `RunPanelView` |
| Motion: tween runner, particles, reactions | `UI/Motion/UiMotion.cs`, `UiBurst.cs`, `FeedbackFx.cs` |

Reactions wired (feedback-motion.md ids): P-01 hover ring, P-06 landing slide + thunk + ring, P-07 return + shudder,
P-08 pinned shudder, P-10 sell coin spray + gold tick, P-11 forge sparks + double ring + badge drop + gold tick, P-12
refused shudder, R-01 reward flight, R-10 room gold coins to the counter, U-04/U-05 overlay slide, R2/C-16 victory
banner, C-05 hit pixels (heavy hits, detonations), C-14 enemy dissolve pixels, HS-01 to HS-11 on hero select.

Simulated balance (600 runs per hero, bot taking the best item, forging, preferring Green doors): Crusader clears the
run 40%, Alchemist 33% (safe policy 42/29%, random doors 28/22%). Rooms 1-2 are won by the 3-item kit alone.

Tests: 214/214 EditMode pass (new `StartAndMotionTests`; run-flow test now picks the hero on the hero select screen and
checks 3 grid items and an empty stash; the Weaken test reads the Hunter's damage from data).

## Evidence (Play Mode, 3x)

- `evidence/redesign_hero-select_z3_2026-11-17.png`, `redesign_hero-select-alchemist_z3_2026-11-17.png`
- `evidence/redesign_planning_z3_2026-11-17.png`: 3-item kit, empty 8 x 4 stash with its hint, innate plate, enemy preview
- `evidence/redesign_hover-ring_z3_2026-11-17.png`, `redesign_forge-sparks_z3_2026-11-17.png`, `redesign_sell-coins_z3_2026-11-17.png`
- `evidence/redesign_fight_00..18_z3_2026-11-17.png`, `redesign_after-fight-coins_z3_2026-11-17.png`
- `evidence/redesign_victory-banner_z3_2026-11-17.png`, `redesign_reward-overlay_z3_2026-11-17.png`, `redesign_doors_z3_2026-11-17.png`

## Not done yet (P1 rows of feedback-motion.md still open)

C-06 stepped HP drain, C-09 status chip pop, C-10/C-11 poison puff and burn flame on ticks, C-13 debuff chips gathering
before a detonation, C-15 hero downed fall, R-02 buff claim readouts, R-03 curse flight, R-06 door walk along the
corridor, U-02 label drop on press, P-02 held ring. The sell/forge drawer zones and the drawer tabs still use the older
code-drawn art (their frame is the new iron plate). Hero cards reuse the K1 wood panel instead of a leather card; name
plates are 19 px boards (spec 22). The stage backdrop keeps chains, which the kit spec asks to avoid.

## Follow-ups needing the user

- Art bible knock-on edits listed in `design/art/ui-kit-generated.md` section 10.
- Registry and GDD propagation for the new monster numbers and kits.
