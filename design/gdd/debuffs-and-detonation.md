# Debuffs and Detonation

> **Status**: Designed (user decisions 2026-11-17; independent /design-review not yet run)
> **Author**: user + Claude design session (systems-designer)
> **Last Updated**: 2026-11-17
> **Last Verified**: 2026-11-17
> **Implements Pillar**: Arrangement Is the Thinking (primary); The Payoff Is the Product (the detonation)
> **Supersedes**: the Ấn (Seal) status of [Status Effects](status-effects.md) (System 13) and the `ConsumeDetonate` effect and `B = c × p` rule of [Effect Resolution Model](effect-resolution-model.md) (System 3). The general ledger rules of System 13 (lots with provenance, oldest-first order, expiry boundary, atomic batches) still apply where this document cites them.

## Summary

System 38 replaces the single Ấn (Seal) status with five enemy debuffs carried by items: Poison, Burn, Frost, Slow and Weaken. Poison and Burn tick damage at the start of the afflicted enemy's turn, Frost builds to a freeze, Slow lowers speed and Weaken lowers outgoing damage. One core item, the Plague Catalyst of the Plague Alchemist, **detonates every debuff on every enemy** at once and turns the stacks into one damage number per enemy. At run start the player picks the Crusader (tank, Weaken) or the Alchemist (Poison, detonation).

> **Quick reference** — Layer: `Feature` · Priority: `MVP` · Key deps: `Effect Resolution Model (3), Event Log (4), Game Data Definitions (2), Item Trigger and Cooldown (9), Adjacency and Synergy (10), Turn Order (11), Combat Resolution (12), Enemy and Intent (14), Combat State and Step Model (36)`

## Overview

The earlier documents read "detonation" as consuming one abstract mark (Ấn). The user corrected this on 2026-11-17: a detonation build means **items that attach real debuffs** (poison, burning, frost, slow, weaken) and **one hero whose core item detonates all of them** on every enemy. This system owns the five debuff kinds (caps, timers, ticks, freeze, speed and damage terms), the `DetonateAll` effect and its formula, the migration of the old Seal items, ten new debuff items, the two heroes' innate items and the enemy debuff row of the HUD. Lots, provenance and atomic commits stay with Systems 13 and 36; ordering and the damage formula's bounds stay with System 3.

## Player Fantasy

The player fills enemies with visible, named harm: green Poison that eats a little each turn, Burn that bites hard and fades, Frost that creeps toward a freeze, Slow and Weaken that buy safety. Each one is worth something left alone. Then the Alchemist's Catalyst goes off and all of it collapses into one burst per enemy, bigger for every different kind it found and bigger still on a frozen enemy. **I don't stack one mark; I paint the enemy with five kinds of harm, then cash all of it in at once.**

The choice is real: detonating ends Poison's long tail and frees a slowed, weakened enemy, so the player decides by arrangement (which afflictions, how many kinds, which neighbours of the Catalyst) when the burst is worth more than the slow burn. The cause stays legible: every chip on the enemy names its kind and count, and the burst takes those chips into the body (Pillar 2).

## Detailed Design

### Core Rules

1. **Kinds.** Five enemy debuff kinds with status IDs `poison`, `burn`, `frost`, `slow`, `weaken`, always processed in that **fixed kind order**. `stun` is a separate status, not a debuff: it is never counted, consumed or shown as a debuff chip. Bleed is cut. The status ID `an` (Ấn) is removed; System 2 rejects a catalog that names it.
2. **Lots.** Each application that adds at least one stack creates one lot (a System 13 contribution): target, kind, amount, source item instance, owning hero, its `StatusApplied` event and, for a timed kind, its own expiry boundary. A new lot never refreshes or replaces older lots. Removal and consumption take lots oldest first (System 13, Rule 4).
3. **Requested amount.** `a_req = a + A`, the authored amount plus the `StatusAmount` bonuses that reach the applying item (System 10). An innate item receives no bonus (System 15, Rule 5).
4. **Cap.** Each kind has a cap per target (table below). The application adds `min(a_req, cap − held)`; the excess is discarded and `StatusApplied` records only what was added. If nothing can be added, no lot and no `StatusApplied` are published; System 3 publishes `EffectSkipped(status_capped)`, so reactions to `StatusApplied` do not fire.
5. **Timers.** A timed lot expires after `L` completed actions of the enemy carrying it, counted from the end of the action in which it was applied (from encounter start for a combat-start lot). A turn skipped by Stun counts as a completed action. Expiry runs at that enemy's action boundary, after its queue drains and before System 12's outcome check, as one `StatusExpired` per kind in kind order (System 13, Rules 3 and 6).
6. **Ticks.** At the start of the afflicted enemy's own turn, before its intent and before a Stun skip, Poison ticks, then Burn ticks. A tick is an action origin `StatusTick` (status ID, target, ordered list of active lots). Tick damage is **base only**: no hero modifier and no Weaken applies (System 3's formula with `F = 0`, `P = 0`), shield first, then HP. After Poison's tick one Poison stack is removed oldest first (`StatusExpired`, reason `poison_decay`). A tick that downs the enemy ends its turn: later ticks, its intent and any Stun skip do not happen.
7. **Freeze.** When a Frost application brings the enemy's Frost to the freeze threshold `6`, the application adds only up to 6, then all Frost lots are removed (`StatusExpired`, reason `freeze`) and the enemy gains Stun 1, linked to that application: it is **Frozen** and its next turn is skipped. An enemy therefore holds at most 5 Frost. A Frost application that reaches 6 on an enemy that is already Frozen removes the Frost but does not add a second Stun (Stun does not stack; see Open Questions).
8. **Stun.** At most 1 pending. At the start of a stunned enemy's turn the ticks run first, then the turn is skipped, Stun is cleared and the enemy's bound intent entry for that turn is lost (System 14). The enemy is **Frozen** while it holds a Stun that came from Frost.
9. **Slow.** Each Slow stack adds `−100‰` to the enemy's speed percent term `P` (System 11, Formula 1).
10. **Weaken.** Each Weaken stack adds `−100‰` to the percent bucket `P` of every Damage effect the enemy deals (System 3; the summed `P` is clamped at `−1000`).
11. **Scope.** In the MVP only enemies carry these debuffs; no item or enemy move applies them to a hero. Hero Wither stays with System 19. All debuffs and Stun are discarded at encounter end.

| Kind | Tick at turn start | Timer `L` | Cap | Detonation value `v` | Other effect |
|---|---|---|---|---|---|
| Poison | `S` damage, then −1 stack | none | 30 | 3 | — |
| Burn | `2 × S` damage | 2 | 15 | 3 | — |
| Frost | none | 3 | 5 held (6 freezes) | 3 | At 6: remove all, Stun 1 (Frozen) |
| Slow | none | 3 | 5 | 4 | `−100‰` speed per stack |
| Weaken | none | 3 | 5 | 4 | `−100‰` outgoing damage per stack |

### Detonation

12. **Effect.** `DetonateAll` is a new effect kind (target `AllEnemies`), authored only on `item_plague_catalyst` and its upgrade. System 2 rejects it on any other item. It is the only way debuffs are consumed.
13. **Per enemy, atomic.** For each living enemy in ascending slot order: an enemy with no debuff stack is skipped. Otherwise one transaction removes **every** debuff stack it holds: one `StatusConsumed` per kind held, in kind order, listing every lot and amount, then one Damage record of `r_e` (Formula 5), shield first, linked to those consumption records. Stun is untouched; the Frozen bonus is read before the commit. If `r_e = 0` the consumption stands and no damage event is published (System 3).
14. **One fact per detonation.** After the last enemy, if at least one enemy was detonated System 3 publishes a `Detonation` fact (detonator item, hero, enemies hit, stacks consumed); if none was, `DetonationEmpty(no_available_units)`. Reactions "to a detonation" listen for the `Detonation` fact (new reaction event kind for System 9), never for the individual `StatusConsumed` records, so they fire once per detonation.
15. **Modifiers.** `P_det` is the sum of `DamagePercent` links into the detonator. `V_k` is the sum of `DetonateValue` links into it for kind `k` (new System 10 kind, amount `1..20`; `statusId` names one kind, empty means every kind). A `DetonateValue` or `DamagePercent` link to a `DetonateAll` item is applicable (System 10, Rule 5); a `DamageFlat` link is not, so the formula has no flat term.
16. **Order of reactions.** Reactions queue after the whole `DetonateAll` effect (System 3 FIFO). A reaction that applies a debuff (the Alchemist's innate) therefore seeds the next detonation and is never consumed by the one that triggered it.

### States and Transitions

| Subject | State | Trigger | Result |
|---|---|---|---|
| Kind on an enemy | Absent | Application adds ≥ 1 | Active, one lot |
| Kind on an enemy | Active | Application at cap | Unchanged; `EffectSkipped(status_capped)` |
| Poison | Active | Enemy's turn starts | Tick `S`, then one stack removed; Absent at 0 |
| Timed lot | Active | `L` completed enemy actions | Lot expires; kind Absent when no lot remains |
| Frost | Active | Total reaches 6 | All Frost removed; enemy Frozen (Stun 1) unless already Frozen |
| Enemy | Frozen | Its turn starts | Ticks, then turn skipped, Stun cleared |
| Any debuff | Active | `DetonateAll` | All stacks consumed in one batch; Absent |
| Enemy | Living | Tick downs it | Turn ends; later ticks, intent and skip do not happen |

### Interactions with Other Systems

| System | In | Out |
|---|---|---|
| 2 Game Data | Kind table, item data, `DetonateAll`, `DetonateValue`, `StatusAmount` with empty `statusId` | Validation rules above |
| 3 Effect Resolution | Queue order, damage formula bounds (`RawMax`, `Cap`, rounding, `P` clamp) | `DetonateAll` resolution, `EffectSkipped(status_capped)`, `Detonation` fact |
| 4 Event Log | — | Origin `StatusTick` with status ID; removal reasons `poison_decay`, `freeze`; `Detonation` fact |
| 9 Item Trigger | — | Reaction event kind `Detonation` (side filters as today) |
| 10 Adjacency | — | Kind `DetonateValue`; `StatusAmount` with empty `statusId` = every debuff kind the target applies |
| 11 Turn Order | — | Slow's `P` term, with durations in the enemy's own actions |
| 12, 14 | Turn start, completed-action boundary, intent binding | Tick before intent, Stun skip after ticks, a downing tick ends the turn |
| 27, 29, 30 | Committed events and views | Debuff row, detonation burst, cause per lot |

### Items

Tag `seal` becomes `affliction`, and every item that applies a debuff carries `affliction`. Only an item with `DetonateAll` carries `detonator`. Tags marked † are provisional (content pass).

| ID (old → new) | Name (vi / en) | Tier | Shape | Trigger | Effects | Synergy to adjacent | Tags |
|---|---|---|---|---|---|---|---|
| `item_plague_catalyst` (new core) | Xúc Tác Dịch Hạch / Plague Catalyst | Purple | 1x2 | Cooldown 4 | DetonateAll → AllEnemies | — | detonator, alchemy |
| `item_seal_ink` → `item_venom_vial` | Lọ Độc / Venom Vial | White | 1x1 | Cooldown 2 | Poison 3 → First | — | affliction |
| `item_seal_stamp` → `item_oil_flask` | Bình Dầu Lửa / Oil Flask | Green | 1x2 | Cooldown 2 | Burn 3 → First | — | affliction |
| `item_seal_chain` → `item_tar_chain` | Xích Nhựa Đen / Tar Chain | Blue | s | Cooldown 3 | Slow 2 → All | — | affliction, chain |
| `item_breaking_bell` → `item_drowsy_bell` | Chuông Mê / Drowsy Bell | Green | 1x2 | Cooldown 2 | Slow 2 → First | — | affliction, bell |
| `item_void_censer` → `item_plague_censer` | Lư Hương Dịch Hạch / Plague Censer | Purple | 2x3 | Passive | — | detonator: DetonateValue poison +2 | incense |
| `item_ink_pot` → `item_mortar` | Cối Tán Thuốc / Mortar | Green | 1x1 | Passive | — | affliction: StatusAmount (all) +1 | tool |
| `item_war_banner` | Cờ Chiến / War Banner | Green | 1x3 | Combat start | Weaken 2 → All | — | banner, affliction |
| `item_runed_mace` | Chùy Khắc Ấn / Runed Mace | Green | 1x2 | Cooldown 2 | Damage 6 + Frost 2 → First | — | weapon, blunt, affliction |
| `item_thunder_gong` | Chiêng Sấm / Thunder Gong | Blue | 2x2 | Reaction: a Detonation | Damage 5 → All | — | bell |
| `item_resonant_bell` | Chuông Cộng Hưởng / Resonant Bell | Purple | 2x2 | Passive | — | detonator: DetonateValue (all) +1 | bell |
| `item_scholar_lens` | Kính Học Giả / Scholar Lens | Purple | 1x1 | Passive | — | affliction: StatusAmount (all) +1; detonator: DamagePercent +500 | relic |
| `item_thorn_dart` | Phi Tiêu Gai / Thorn Dart | White | 1x1 | Cooldown 1 | Damage 3 + Poison 1 → First | — | weapon†, affliction |
| `item_ember_charm` | Bùa Than Hồng / Ember Charm | White | 1x1 | Cooldown 2 | Burn 2 → First | — | charm†, affliction |
| `item_frost_shard` | Mảnh Băng / Frost Shard | Green | 1x2 | Cooldown 3 | Damage 6 + Frost 2 → First | — | weapon†, affliction |
| `item_tar_net` | Lưới Nhựa / Tar Net | Green | 2x1 | Combat start | Slow 2 → All | — | tool†, affliction |
| `item_cracked_mask` | Mặt Nạ Nứt / Cracked Mask | Green | 1x1 | Cooldown 2 | Weaken 1 → First | — | charm†, affliction |
| `item_coin_purse` | Túi Tiền / Coin Purse | Green | 1x1 | Passive | +2 gold when a room is won ([Item Upgrades](item-upgrades.md)) | — | tool† |
| `item_blight_spear` | Giáo Dịch / Blight Spear | Blue | 1x3 | Cooldown 3 | Damage 10 + Poison 3 → First | — | weapon†, affliction |
| `item_lull_drum` | Trống Ru / Lull Drum | Blue | 1x2 | Cooldown 3 | Slow 1 + Weaken 1 → All | — | instrument†, affliction |
| `item_witch_cauldron` | Vạc Phù Thủy / Witch Cauldron | Purple | 2x2 | Passive | — | affliction: StatusAmount (all) +1 | alchemy† |
| `item_glacier_maul` | Chùy Băng Hà / Glacier Maul | Purple | t | Cooldown 4 | Damage 18 + Frost 2 → All | — | weapon†, blunt†, affliction |

Old IDs are renamed in data and redrawn (user decision); no alias is kept. The catalog grows from 35 to 46 items. `item_echo_charm` still names Ấn in its trigger and needs a migration (Open Questions). The Catalyst never appears in reward offers (System 18 excludes it).

### Heroes

At run start the player picks **one** hero: Crusader or Alchemist (user decision 2026-11-17; the MVP is 1-hero, System 15's three-of-four party rule awaits revision).

| Hero | Role (vi / en) | Innate item | Signature | Starting items (provisional) |
|---|---|---|---|---|
| `hero_crusader` | Đỡ Đòn / Suy Yếu (Tank / Weaken) | Combat start: Shield +6 Self; Weaken 1 → All | `item_spiked_buckler` | iron sword, spiked buckler, venom vial, whetstone |
| `hero_alchemist` (Dược Sư Dịch Hạch / Plague Alchemist) | Gây Độc / Kích Nổ (Poison / Detonate) | Reaction to its own hero's Detonation: Poison 2 → All | `item_plague_catalyst` | plague catalyst, dagger, venom vial, oil flask, mortar (as built 2026-11-17) |

### Enemy HUD and Presentation

- **Debuff row.** Under each enemy, 4 chip slots; each chip is a kind glyph plus its stack number, in kind order. With 5 kinds the fourth slot shows the first three and `+2`. The number is the exact owner value (System 27's no-silent-truncation rule).
- **Chip frames** use existing palette colours (art bible 4.6, 5.11, 8.7 amendment pending the art-director): Poison MOSS-HI, Burn CANDLE-D, Frost FOG, Slow INK-4, Weaken WINE-4. The glyph, not the colour, carries identity.
- **Ticks** show a small number from the chip at the enemy's turn start (System 29 Tick level).
- **Detonation.** The chips of each detonated enemy merge into its body, then one burst and **one number per enemy** (System 29 Detonation level, full timing at every speed). The popup of that number lists the kinds and stacks consumed and the source items (System 30).

## Formulas

All integer; rounding and saturation as System 3.

**Formula 1: application.** `added = min(a + A, C_k − S_k)`; for Frost `C_frost = 6`, and `added` reaching 6 triggers the freeze (Rule 7).

| Symbol | Type | Range | Description |
|---|---|---|---|
| `a` | int | 1..20 | Authored amount of the `ApplyStatus` effect |
| `A` | int | 0..60 | Sum of `StatusAmount` bonuses reaching the item (System 10) |
| `S_k` | int | 0..C_k − 1 | Stacks of kind `k` held before the application |
| `C_k` | int | 30, 15, 6, 5, 5 | Cap (Frost: freeze threshold) |
| `added` | int | 0..C_k | Stacks recorded by `StatusApplied`; 0 → `EffectSkipped(status_capped)` |

*Example:* Poison 28 held; Blight Spear (Poison 3) next to a Mortar (+1): `min(4, 2) = 2` added, 2 discarded. Frost 4 held; Glacier Maul Frost 2 + Scholar Lens 1: `min(3, 2) = 2`, total 6 → all Frost removed, Frozen.

**Formula 2: ticks.** `t_poison = S_poison` (then `S_poison − 1`); `t_burn = 2 × S_burn`. Output 1..30 and 2..30; base only, shield first. Untouched Poison of `S` deals `S(S+1)/2` over `S` turns. A Burn lot of `a` stacks deals `4a` over its 2 ticks. *Example:* Poison 5, Burn 3, enemy shield 4 and HP 40: Poison 5 → shield 0, HP 39; Poison 4 left; Burn 6 → HP 33.

**Formula 3: Slow.** `P_speed = −100 × S_slow`, range `0..−500`, into System 11's `v = clamp((max(0, s + F) × (1000 + P) + 500) / 1000, 1, 200)`. *Example:* `s = 20`, Slow 2: `(20 × 800 + 500) / 1000 = 16`; Slow 5: `10`.

**Formula 4: Weaken.** `P_out = P_enemy − 100 × S_weaken`, range `0..−500` from Weaken, into System 3's percent bucket of each enemy Damage effect. *Example:* a 12-damage move with Weaken 3: `(12 × 700 + 500) / 1000 = 8`.

**Formula 5: detonation damage per enemy.**
`B_e = min(Σ_k S_ek × (v_k + V_k), RawMax)`;
`P_e = max(−1000, P_det + 100 × (K_e − 1) + Z_e)`;
`r_e = clamp((B_e × (1000 + P_e) + 500) / 1000, 0, Cap)`.

| Symbol | Type | Range | Description |
|---|---|---|---|
| `S_ek` | int | 0..cap of `k` | Stacks of kind `k` on enemy `e` when consumed |
| `v_k` | int | 3 (Poison, Burn, Frost), 4 (Slow, Weaken) | Detonation value per stack |
| `V_k` | int | 0..60 | `DetonateValue` bonuses into the detonator for kind `k` |
| `K_e` | int | 1..5 | Distinct kinds consumed on `e` (an enemy with 0 is skipped) |
| `Z_e` | int | 0 or 300 | `300` if `e` is Frozen at detonation |
| `P_det` | int | −1000..100,000 | `DamagePercent` links into the detonator |
| `RawMax`, `Cap` | int | 10^12, 2,147,483,647 | System 3 bounds |
| `r_e` | int | 0..Cap | Damage to `e`; 0 → no damage event |

*Output range.* Smallest positive result with no modifier: one Poison, Burn or Frost stack, `r = 3`. Largest with no adjacency bonus: Poison 30, Burn 15, Frost 5, Slow 5, Weaken 5, Frozen: `B = 190`, `P = 700`, `r = 323`. With Plague Censer, Resonant Bell and Scholar Lens on the Catalyst (`V_poison = 3`, others 1, `P_det = 500`): `B = 310`, `P = 1200`, `r = 682`. `r = 0` is reachable only with a negative `P_det`, which no MVP item has.

*Worked example.* Poison 8, Burn 4, Frost 3: `B = 24 + 12 + 9 = 45`, `K = 3`, `P = 200`, `r = (45 × 1200 + 500) / 1000 = 54`. With Scholar Lens (`P_det = 500`): `P = 700`, `r = (45 × 1700 + 500) / 1000 = 77`. A Frozen enemy with Poison 8 and Slow 2: `B = 32`, `P = 100 + 300 = 400`, `r = (32 × 1400 + 500) / 1000 = 45`.

*Feedback loops.* (1) Alchemist innate: each detonation applies Poison 2 to all, seeding the next one; positive, bounded by the Catalyst's cooldown and the caps (intentional). (2) Detonation removes Slow and Weaken, so the enemy speeds up and hits harder at once; negative, self-dampening (intentional: the price of cashing in). (3) Frost into freeze: repeated freezing could lock an enemy; dampened by Stun not stacking and Frost being cleared at each freeze (watch in System 35).

## Edge Cases

| Case | Handling |
|---|---|
| Application on a kind at its cap | No lot, no `StatusApplied`; `EffectSkipped(status_capped)`; `StatusApplied` reactions do not fire. |
| Frost 4 held, application of 5 | Adds 2 (to 6), 3 discarded; all Frost removed; Stun 1. |
| Frost reaches 6 on an already Frozen enemy | All Frost removed; Stun stays 1; no second skip. |
| Poison tick downs the enemy | Burn does not tick; no intent; no Stun skip; System 12 checks the outcome at the boundary. |
| Poison at 1 ticks | 1 damage, then 0: Poison is Absent and its chip disappears. |
| Enemy downed before its turn by other damage | No ticks. A detonation skips it. |
| Detonation finds no debuff on any enemy | `DetonationEmpty(no_available_units)`; no reactions; the Catalyst's cooldown is spent as for any firing (System 9). |
| Enemy holds only Stun | Skipped by the detonation (Stun is not a debuff); Stun stays. |
| Enemy has shield | Ticks and detonation damage hit shield first. |
| Combat-start lot (War Banner, Tar Net, Crusader innate) | Timer counts from the enemy's first completed action, so Weaken 2 from the banner lasts 3 enemy actions. |
| Lot applied during the enemy's own action (a hero reaction) | Its window opens after that action (System 13, Rule 3). |
| Several kinds expire at one boundary | One `StatusExpired` per kind, in kind order. |
| Slow 5 on a speed 5 enemy | `v = (5 × 500 + 500) / 1000 = 3`; the floor of System 11 is 1. |
| Thunder Gong in the Crusader's grid | No detonator exists, so it never fires; no link is shown. |
| Catalyst moved to the stash or sold | No detonation; the Alchemist's innate never fires. |
| Thunder Gong's damage triggers further reactions | System 3's depth and count limits apply (`ChainLimit`). |
| Detonation batch fails validation | Nothing of that enemy's batch publishes; technical stop (System 3). Earlier enemies' committed batches remain. |

## Dependencies

| System | Direction | Contract | Reverse reference |
|---|---|---|---|
| 2 Game Data Definitions | Upstream | Status IDs, kind table, `DetonateAll` (Catalyst only), `DetonateValue`, `StatusAmount` with empty `statusId`, item and hero data above | Pending in System 2 |
| 3 Effect Resolution Model | Upstream | Queue, formula bounds; replaces `ConsumeDetonate`; new reason `status_capped`, new fact `Detonation` | Superseded note added 2026-11-17; full revision pending |
| 4 Event Log | Upstream | `StatusTick` origin with status ID; removal reasons; `Detonation` fact | Pending |
| 9 Item Trigger and Cooldown | Downstream | Reaction event kind `Detonation` | Pending |
| 10 Adjacency and Synergy | Downstream | Kind `DetonateValue`; empty `statusId` for `StatusAmount` | Pending |
| 11 Turn Order | Downstream | Slow's `P` term (closes System 11's open speed-status question) | Pending |
| 12 Combat Resolution, 14 Enemy and Intent | Downstream | Tick at turn start before intent; Stun skip; downing tick ends the turn | Pending |
| 13 Status Effects | Upstream | Lot ledger rules; Ấn superseded | Superseded note added 2026-11-17 |
| 15 Party and Heroes | Downstream | Pick 1 of 2; innate items; Alchemist signature | Pending |
| 18 Commitment and Drop Model | Downstream | Catalyst never offered; `affliction` and `detonator` tags | Pending |
| 27, 29, 30 HUD, Payoff, Breakdown | Downstream | Debuff row, tick and detonation presentation, cause per lot | Pending |
| 35 Balance Harness | Downstream | Verifies the tuning below | Pending |
| 36 Combat State | Upstream | Ledger, caps checked at commit, atomic batches | Pending |
| 39 Item Upgrades | Downstream | Catalyst+ (Cooldown 3); Coin Purse | [item-upgrades.md](item-upgrades.md) |

## Tuning Knobs

| Knob | Value | Safe range | Effect | Too high / too low |
|---|---|---|---|---|
| Caps (Poison, Burn, Slow, Weaken) | 30, 15, 5, 5 | 10..50, 5..25, 3..8, 3..8 | Ceiling of each kind and of a detonation | Bursts unreadable / no stacking play |
| Freeze threshold | 6 | 4..10 | How often an enemy loses a turn | Perma-freeze / never freezes |
| Timers `L` (Burn, Frost, Slow, Weaken) | 2, 3, 3, 3 | 1..5 | Window to cash in | Kinds pile up / vanish before the Catalyst |
| Poison decay per tick | 1 | 1..2 | Length of Poison's tail | — |
| Burn tick multiplier | 2 | 1..3 | Burn's tick pressure (`4a` per lot vs `3a` detonated) | Detonating Burn is never worth it / Burn is filler |
| Detonation values `v_k` | 3, 3, 3, 4, 4 | 1..8 | Base worth of each stack | Detonation dominates ticks / not worth the price |
| Multi-kind bonus | 100‰ per extra kind | 0..250 | Rewards variety | Variety mandatory / one-kind builds win |
| Frozen bonus `Z` | 300‰ | 0..500 | Rewards freeze timing | — |
| Slow, Weaken per stack | −100‰ | −50..−200 | Control strength (max −500‰) | Enemies harmless / control irrelevant |
| Catalyst cooldown | 4 (3 when upgraded) | 3..6 | Detonation rhythm | Constant bursts / one per fight |
| `DetonateValue` range | 1..20 | — | Passive support strength | — |

All values provisional until System 35 runs; amounts on items are content and live in the item data.

## Acceptance Criteria

1. **Given** Poison 28 on an enemy, **when** an application of 4 resolves, **then** `StatusApplied` records 2, a lot of 2 exists and the total is 30; a further application publishes `EffectSkipped(status_capped)` and no `StatusApplied`.
2. **Given** Poison 5 and Burn 3 on an enemy with shield 4 and HP 40, **when** its turn starts, **then** the order is Poison tick 5 (shield 4 → 0, HP 40 → 39), Poison decay to 4, Burn tick 6 (HP 33), then its intent; both ticks carry origin `StatusTick` with their status ID and no hero modifier.
3. **Given** an enemy with HP 3 and Poison 5 and Stun 1, **when** its turn starts, **then** the Poison tick downs it and no Burn tick, intent or skip event follows.
4. **Given** Burn applied during a hero action, **when** the enemy completes two actions, **then** `StatusExpired` for that lot is published after the second action's queue drains and before the outcome check.
5. **Given** Frost 4, **when** Frost 3 is applied, **then** `StatusApplied` records 2, then `StatusExpired(freeze)` removes 6, then Stun 1 is applied; the enemy's next turn runs its ticks and is then skipped.
6. **Given** a Frozen enemy, **when** its Frost reaches 6 again, **then** Frost is removed and the Stun count stays 1.
7. **Given** an enemy of base speed 20 with Slow 2, **when** System 11 computes its speed, **then** `v = 16`; after the lots expire, `v = 20`.
8. **Given** an enemy move of 12 damage and Weaken 3 on the enemy, **when** it resolves, **then** the damage is 8.
9. **Given** enemy A with Poison 8, Burn 4, Frost 3 and enemy B with no debuff, **when** the Catalyst fires with no adjacent items, **then** A gets three `StatusConsumed` records (Poison, Burn, Frost) then one damage record of 54, B gets no record, and one `Detonation` fact follows; with Scholar Lens adjacent the damage is 77.
10. **Given** a Frozen enemy with Poison 8 and Slow 2, **when** detonated, **then** the damage is 45, its Stun remains and its speed returns to base at once.
11. **Given** no enemy holds a debuff, **when** the Catalyst fires, **then** `DetonationEmpty(no_available_units)` is published and neither the Thunder Gong nor the Alchemist's innate fires.
12. **Given** the Alchemist detonates two enemies, **when** reactions resolve, **then** its innate fires once and applies Poison 2 to every living enemy after all consumption records.
13. **Given** a catalog where `DetonateAll` sits on any item but the Catalyst (or its upgrade), or that names status `an`, **when** System 2 validates it, **then** it is rejected.
14. **Given** an enemy with all five kinds, **when** the HUD draws its row, **then** it shows Poison, Burn, Frost and a `+2` chip with exact numbers, and a screenshot of the row and a detonation burst is retained in `production/qa/evidence/`.
15. **Given** identical inputs, **when** the fight runs in Unity, headless and tests, **then** the ordered events, lots and amounts are identical.

## Open Questions

| Question | Owner | Resolve by |
|---|---|---|
| `item_echo_charm` reacts to "Ấn applied". Proposed: react to any debuff applied to an enemy (`eventStatus` empty). | User, game-designer | Before the item data migration |
| Stun does not stack (a freeze on a Frozen enemy adds no skip): proposed default, confirm. | User, systems-designer | Before System 35 runs |
| Should reward offers filter detonator-support items (Thunder Gong, Resonant Bell, Plague Censer) for the Crusader, who has no detonator? | Systems 18, 15 | Before reward tuning |
| Which glyph or chip marks Frozen (Stun) beside the debuff row? | ux-designer, art-director | Before HUD stories |
| How is tick damage credited to source lots in the breakdown (by share of stacks)? | System 30 | Before breakdown stories |
| Art bible amendment for the five chip colours (4.6, 5.11, 8.7). | art-director | Before HUD art |
