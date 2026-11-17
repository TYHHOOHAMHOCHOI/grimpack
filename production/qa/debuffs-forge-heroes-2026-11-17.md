# Debuffs, detonation, item upgrades (Rèn), new items, hero pick — build notes and evidence (2026-11-17)

User request: "gọi chuyên gia, thêm Item, thêm phần Nâng cấp Item … kéo item vào nó dùng vàng nâng cấp … Sửa mạnh phần ấn … Kích nổ debuff … có 1 nhân vật có item core … ko phải ấn."
Specialists: systems-designer (debuffs + GDDs), economy-designer (upgrades, gold, new items), ux-designer (drawer, chips),
lead-programmer (plan), art-director (colours, badge, anvil), technical-artist (sprites).
User decisions: the Plague Alchemist carries the core and the player picks Crusader or Alchemist at run start; the core
detonates every enemy; gold starts at 5 and is paid by door tier; the old Seal items are renamed and redrawn.

## What is built

| Area | Where |
|---|---|
| Design | `design/gdd/debuffs-and-detonation.md` (System 38), `design/gdd/item-upgrades.md` (System 39), superseded notes in status-effects.md and effect-resolution-model.md, systems-index rows |
| Status catalog (Poison, Burn, Frost, Slow, Weaken, Stun) as data | `statuses` in `Data/items_mvp.json` (from `tools/art/item_catalog.py`), `CombatCore/StatusDefinitions.cs`, `GameCatalog.FindStatus` |
| Caps, timers, Poison/Burn ticks at the carrier's turn start, Poison decay, Frost → Freeze (stun 1), Slow on the gauge, Weaken on enemy damage | `CombatLedgerState` (`ApplyStatus`, `ApplyStatusTicks`, `StatusPermille`, `OutgoingDamagePermille`), `CombatRunner`, `EffectResolver` |
| Detonation (`DetonateAll`): every debuff on every enemy, B = Σ stacks × (value + bonus), +100‰ per extra kind, +300‰ if frozen | `CombatLedgerState.DetonateAllDebuffs`, synergy kind `DetonateValue` |
| Items: 6 Seal items renamed/redrawn, 10 new debuff items, the core Plague Catalyst; 45 + versions generated (×4/3, cooldown −1 from 4) | `tools/art/item_catalog.py` → 91 records |
| Heroes: Crusader innate Weaken; Alchemist (core, Plague Seed innate re-seeds Poison after a detonation) with new sprites | `Data/heroes_mvp.json`, `tools/art/build_characters.py`, `Art/Characters/char_alchemist_*` |
| Gold: start 5, room win pays by door tier (2/3/5/7/10) + Coin Purse; upgrade 4/6/9/14 by item tier; a + item sells at the next tier's price | `Data/run_mvp.json`, `RunConfig`, `CombatSceneController.Run.cs`, `ItemBoardController.PriceOf` |
| Forge ("Rèn"): second tab on the sell drawer, anvil + frame + cost, before > after preview while hovering, drop upgrades in place with a "+" badge | `UI/Hud/ForgeZoneView.cs`, `SellDrawerView` modes, `ItemBoardController.Forge`, builder `BuildForgeZone` |
| Enemy debuff chips (tinted frame + glyph + stacks, fixed order), item popup text for debuffs/detonation | `CombatHudMapper.KindOf`, `HudSpriteSet.StatusChip`, `StatusChipView`, `ItemTooltipText` |
| Hero pick at run start (Start is locked until a hero is chosen) | `RunPanelView.ShowHeroPick`, `CombatSceneController.Run.HandleHeroPicked` |
| Sprites (36, palette-checked) | `tools/art/build_status_ui.py` |
| Item art (Pixellab, ~35 generations) | `art-source/ai-raw/item_*` for 17 new/renamed arts, `tools/art/build_items.py` |

Tests: `DebuffAndUpgradeTests` (13), run-flow integration test extended (hero pick, gold, forge quotes); 202/202 EditMode pass.

## Evidence (Play Mode, 3x)

- `evidence/debuff_hero-pick_z3_2026-11-17.png` — run start: Crusader or Plague Alchemist.
- `evidence/debuff_forge-hover_z3_2026-11-17.png` — Venom Vial held over the forge: "Độc 3>4", cost 4.
- `evidence/debuff_forge-done_z3_2026-11-17.png` — after the drop: Venom Vial + with the badge, gold 5 → 1.
- `evidence/debuff_fight_00..09_z3_2026-11-17.png` — room 1 as the Alchemist: Poison chip on the enemy, ticks, a 26-damage detonation.
- `evidence/debuff_after-fight_z3_2026-11-17.png` — reward shelf, "+2 vàng".
- `evidence/status_forge_sprites_2026-11-17.png` — sprite contact sheet.

## Deviations and open issues

1. **Forge and sell are locked during the reward steps** (door/claim/shelf), as the sell drawer already was, so the drawer
   never covers the cards. item-upgrades.md allows upgrading during the reward steps; it is possible again right after the
   door is chosen.
2. **Reactions to a detonation** (Thunder Gong, Plague Seed) listen for `StatusConsumed` with the existing once-per-action
   listener rule, not a new `Detonation` event as debuffs-and-detonation.md Rule 14 describes. Expiry reasons in the
   trace are `decay` and `frozen` (the GDD says `poison_decay`, `freeze`).
3. Status ticks play back as ordinary hits; freezes and decay have no dedicated animation yet; motes are not tinted per
   debuff in the avatar flight yet (the tinted sprites exist).
4. The held plate is not drawn above the open drawer in the automated capture (selling worked this way before; not
   re-checked by hand).
5. One-hero balance is still the open risk noted on 2026-11-17 (rooms 4-5).
6. GDD follow-ups from the docs agent: registry entries for the new ids/tables (needs your OK), 18 GDDs to point back
   (`/propagate-design-change`), `/design-review` of Systems 38 and 39, art bible amendment for chip colours.
7. Fixed on the way: `CombatHudDemo` re-applied its fixture after entering Play Mode (OnValidate delayCall), hiding the
   live HUD at run start.
