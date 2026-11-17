# Fusion Door (System 40): build notes and evidence (2026-11-17)

User request: a door that lets the player fuse two items, with recipes; a fusion creates a new item stronger than the
old ones; 2 or 3 items depending on the recipe.
User decisions: the fusion door replaces a fight (no monsters, gold, buff or item offer; it counts as a room); one
fusion per visit, paid in gold (6 for 2 items, 10 for 3); an open recipe book; 12 recipes. After the simulation below,
the user kept the 6 / 10 cost and chose the stronger tuning (about ×1.85 of the forged ingredients).
Design: `design/gdd/item-fusion.md` (systems-designer and ux-designer roles taken inline; `/design-review` not run).

## What is built

| Area | Where |
|---|---|
| 12 fused items (Gold tier, no +, never offered) and 12 recipes, with the power and cell lint | `tools/art/item_catalog.py` → `Data/items_mvp.json` (`recipes`), `design/content/mvp-item-catalog.md` |
| Recipe data in the core catalog (parse, validation, `FindRecipe`, `RecipeMaking`, `RecipesUsing`) | `CombatCore/CatalogDefinitions.cs` (`RecipeDef`), `CombatCore/GameCatalog.cs` |
| Fusion rules (which instances are consumed, missing, cost, makeable count) | `CombatCore/Run/Fusion.cs` |
| Door rule (rooms 3..8, 35 %, max 2, never consecutive, forced by room 6, replaces the lowest tier) on its own RNG side stream | `CombatCore/Run/DoorGenerator.cs` (`FusionDoor`, `DoorCard.IsFusion`), `CombatCore/SeededRng.cs` (`DeriveSideStream`) |
| Fusion phase (`ChooseDoor` → Fusion, `RecordFusion`, `LeaveFusion`, history), debug `ForceFusion` | `CombatCore/Run/RunSession.cs`, knobs in `Data/run_mvp.json` → `fusion`, `RunConfig` |
| Door card face, recipe book (2 columns), detail plate, Dung hợp / Rời lò, keyboard and gamepad | `CombatScene/RunPanelView.Fusion.cs`, `RunPanelView.cs` |
| Scene flow: empty stage, ingredient rings on the board, apply the fusion, motion (coins, plates into the crucible, sparks, result flight), leave | `CombatScene/CombatSceneController.Fusion.cs`, `ItemBoardController.TakeOff/MarkItems`, `CombatHudView.ClearEnemies` |
| Popup lines "Dùng để luyện" / "Luyện từ", "Huyền thoại" subtitle | `Items/ItemTooltipText.AddRecipeLines`, `Items/ItemRecords.cs` (`RecipeRecord`) |
| Corridor: fusion node and history line | `CombatScene/CorridorView.cs`, `tools/art/build_run_ui.py` (`ui_map-node_fusion_10x11`) |
| Art: 12 item icons + 10 compact glyphs (Pixellab pixen, 2x the bed, palette and outline by `build_items.py --only`), crucible emblem (`ui_gen_manifest.json`) | `art-source/ai-raw/item_*_raw.png`, `art-source/ai-raw/ui/ui_emblem-crucible_raw.png`, `Art/Items/Icons`, `Art/UI/Gen/ui_emblem-crucible_32x32.png` |

`build_items.py` gained `--only`: re-running it on every item would re-quantise the accepted art against the palette
that gained the Umber pair today (it changed about 40 icons; they were restored).

## Balance (simulation, 600 seeded runs per line, green-door bot)

| Tuning | Crusader | Alchemist |
|---|---|---|
| Never fuse | 41% | 30% |
| Enter every fusion door, fuse nothing | 26% | 16% |
| First tuning (×1.25 / ×1.40 of the plain ingredients), always fuse | 31% | 25% |
| **Shipped (×1.71..2.07 of the + ingredients), always fuse** | **53%** | **33%** |

Fusion doors per run: 1.54. The harness lives in the session scratchpad, not in the repo (System 35 replaces it).

## Tests

229/229 EditMode pass. New `FusionTests` (14: data, rules, door rule over 300 seeds with the doors stream still at 43
calls, fusion phase, boss after room 8, popup lines) and `CombatSceneIntegrationTests.CombatScene_FusionRoom_FusesTheKitAndLeaves`
(plays rooms 1 and 2 with the Crusader, enters the fusion room, fuses the Honed Sword, checks gold, board and the next
row). Updated for the new data: item counts 91 → 103, fused items have no +, the Plague Heart detonates.

## Evidence (Play Mode, 3x; wins injected to reach room 3)

- `evidence/fusion_door-card_z3_2026-11-17.png`: the fusion door in the row to room 3, armed, with its detail
- `evidence/fusion_room_z3_2026-11-17.png`: the recipe book, the Honed Sword armed, its ingredients ringed on the board
- `evidence/fusion_recipe-missing_z3_2026-11-17.png`: a 3-item recipe with missing ingredients
- `evidence/fusion_popup-ingredient_z3_2026-11-17.png`, `fusion_popup-result_z3_2026-11-17.png`: the recipe popup lines
- `evidence/fusion_motion_z3_2026-11-17.png`, `fusion_result_z3_2026-11-17.png`: the fusion in flight and done (gold 10 → 4)
- `evidence/fusion_after-leave_z3_2026-11-17.png`: the row to room 4 after leaving, the fusion node in the corridor
- `evidence/items_fused_sheet_2026-11-17.png`: the 12 fused items on their plates

## Not done / follow-ups

- Independent `/design-review` of `item-fusion.md`; reverse references in Systems 2, 5, 8, 17, 18, 26 and 35 are pending.
- Sound ids for the fusion are raised through `FeedbackFx` (coins, sparks), no fusion-specific cue yet.
- The Twin Axe icon reads as a glaive (the L footprint is hard for the generator); the Firebomb and Plague Heart icons are dark after quantising. Candidates for a redraw.
- The Everfrost Maul (67 damage to every enemy) is the strongest item in the game by far; it is rare (needs a Purple maul) but worth a look in System 35.
- Strings are Vietnamese literals until System 7.
