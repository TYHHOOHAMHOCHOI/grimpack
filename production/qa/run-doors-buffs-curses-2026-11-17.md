# Run flow: corridor map, doors, buffs, curses — build notes and evidence (2026-11-17)

User request (2026-11-17): "gọi chuyên gia. Làm map + cửa, làm các thứ liên quan đến cửa, Buff, nguyền".
Specialists consulted: game-designer, ux-designer, lead-programmer, art-director (read-only), technical-artist
(sprites). User decisions: map = the corridor strip in the top strip; all four curse kinds; the card is the door
(code-drawn, no Pixellab); a simple item reward step.

## What is built

| Area | Where | Source rule |
|---|---|---|
| Run config (tier tables, buffs, kicks, templates, curses, item rows) | `Assets/_Project/Data/run_mvp.json`, `CombatCore/Run/RunConfig.cs` (validator) | Systems 16, 17, 18, 19 (all values provisional) |
| Door row: 3 tier draws with the card-3 masks, then 3 variant draws on `doors` | `CombatCore/Run/DoorGenerator.cs` | System 17 Rules 3-5, System 19 Rule 7 |
| Encounter spec: multipliers by room and door tier, threat, danger 1-5, class counts | `CombatCore/Run/EncounterBuilder.cs` | System 16 Rules 8-10, Formulas 1 and 3 |
| Item reward offer (3 distinct, by door tier row, never the curse item) | `RewardOffer` in `DoorGenerator.cs` | System 18 (no commitment lean yet) |
| Run session: rooms 1-8 then boss, win → buff claim → item → door, defeat ends the run, 43 calls on `doors` | `CombatCore/Run/RunSession.cs` | System 5 (MVP slice) |
| Curse terms in hero stats; Wither tick at the start of each own turn | `HeroStatMath` (CombatModels.cs), `CombatLedgerState.ApplyCurseTick`, `CombatRunner` | System 19 Formulas 3 and 4 |
| Locked cells and pinned items on the board; a door's +1 cell grows the square grid toward 7 x 7 | `Items/ItemBoard.cs`, `UI/Hud/ItemBoardController.cs` | System 17 Rule 7, System 19 Rule 3 |
| Right-column panel: Buff claim, Reward shelf, Door choice (arm then confirm, arrows + Enter) | `CombatScene/RunPanelView.cs` | System 28 |
| Corridor map (9 door nodes, current bar, armed preview, history popup) | `CombatScene/CorridorView.cs` | run-state-and-flow UI Requirements |
| Wiring in the live scene | `CombatScene/CombatSceneController.Run.cs` | — |
| Sprites (45, palette-checked) | `tools/art/build_run_ui.py` → `Assets/_Project/Art/UI/` | art bible 3.2, 3.4.4, 8.8 |
| Cursed Anchor item art (Pixellab) | `art-source/ai-raw/item_cursed-anchor_*`, `tools/art/item_catalog.py` | — |

Tests: `RunCoreTests` (17), two new `CombatSceneIntegrationTests`; 189/189 EditMode pass.

## Evidence (Play Mode, 3x)

- `evidence/run_corridor-room1_z3_2026-11-17.png` — corridor at room 1.
- `evidence/run_reward_z3_2026-11-17.png` — item shelf after room 1.
- `evidence/run_doors_z3_2026-11-17.png` — door choice, White armed, corridor preview.
- `evidence/run_claim_z3_2026-11-17.png` — buff claim (+4 max HP).
- `evidence/run_end_z3_2026-11-17.png` — defeat ends the run (room 4), one "new run" button.
- `evidence/run_doors-gold_z3_2026-11-17.png` — forced Gold card with the Cursed Anchor tag.
- `evidence/run_claim-curse_z3_2026-11-17.png` — Gold claim: +16 HP, +1 cell, curse line.
- `evidence/run_cursed-hero_z3_2026-11-17.png` — after the claim: anchor pinned in the grid (Plum marks), 5 x 5 grid
  with locked cells as checker, curse tag by the hero.
- `evidence/run_doors-after-curse_z3_2026-11-17.png` — next row has no Gold (curse cap reached).
- `evidence/run_ui_sprites_2026-11-17.png` — sprite contact sheet (colour and greyscale).

The Gold captures used injected wins (`run_probe3.cs`, scratchpad) and a debug-forced Gold card; the real fights
were lost at rooms 4-5 (see below).

## Deviations and open issues

1. **One hero.** The live scene has one hero, so the claim goes to it automatically and the party curse cap is
   `min(2, heroes)` = 1. The GDDs assume a party of three.
2. **Balance with one hero.** With the starting items and no rearranging, automated runs lost at room 4 (Green, 3
   enemies) and room 5 (Green, 4 enemies). The rosters are authored for three heroes (System 16 threat uses party 3).
3. **Wither** is a direct damage tick with `OriginKind.Curse`, not the "baseline status with a stable contribution
   ID" of System 19 (an Open Question there). Playback shows it as an ordinary hit.
4. **Pinned NeedsSpace** checks the grid before the claim's new cell opens (conservative).
5. **HUD max HP** updates at room entry, not right after a claim (the claim text says "from the next fight").
6. **Not built:** commitment lean in the item offer, snapshot/restore and stale-serial checks (System 5 Rules 16-17),
   the reveal choreography, gamepad grammar, the hero popup HP breakdown, curse chips in the HUD stat row.
7. **Registry ranges** contradicted by System 19 values (game-designer): `door_buff` [0, 12] vs Gold 16;
   `encounter_hp_multiplier` [1000, 1620] vs Gold room 8 = 1720.
8. Door cards are 90 px wide (UX) where the art director sized 84 px; the frames are 9-sliced, so both work.
9. The old 1..6 monster selector remains behind the `monsterTestSelector` flag on `CombatSceneController`; the legacy
   `Scripts/Progression/` code is untouched.
10. A debug flag `forceGoldDoor` on `CombatSceneController` makes card 3 Gold from room 6 (System 19 Rule 1).
