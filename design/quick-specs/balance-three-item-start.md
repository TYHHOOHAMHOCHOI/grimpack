# Quick Spec: Balance for the Three-Item Start (1-hero runs)

> **Status**: Draft for simulation, 2026-11-17 · **Author**: systems-designer · **Applies after**: the lead's seeded simulation
> **Inputs**: user decisions 2026-11-17 (3-item kit, empty stash, difficulty "Vừa", one hero per run). Every number is provisional. The targets in §2 are the acceptance test; the values in §4 are only the starting point.

## 0. Facts this spec relies on (read from data and code)

- `run_mvp.json` has `rooms: 8`, so the **boss is fight 9** (`RunSession.BossRoom = Rooms + 1`). The boss uses multipliers 1000/1000, with no ramp and no kick. See Q1.
- Monster scaling: `HP × (1000 + hpRamp·(r−1) + hpKick)/1000` and damage the same way with the damage terms, rounded half up. Speed never scales.
- A unit acts every `⌈1000/speed⌉` ticks. A Cooldown-N item with `S = 0` first fires on owner action 1, then every N actions. Items fire in reading order (row, then column, of the anchor).
- After each win the player takes 1 of 3 offered items or skips. Gold starts at 5, plus the door's gold. Forge costs 4/6/9/14.
- **A1 (assumption):** hero HP is full at the start of every fight (the baseline is rebuilt per fight, with door HP added). If HP carries over between fights, every target in §2 must be measured again.

## 1. Starting kits (3 items, empty stash)

Grid coordinates: `x` = column 0..3 from left to right, `y` = row 0..3 from top to bottom. The anchor is the top-left cell. **The listed cells are authoritative.** `rot` means whichever rotation of the authored footprint produces those cells.

| Hero | Item | Cells (x,y) | Orientation | Why (one line) |
|---|---|---|---|---|
| Crusader | `item_iron_sword` | (0,0)(1,0)(2,0) | horizontal | The Crusader's blade and his main damage: 9 per 2 actions. |
| Crusader | `item_spiked_buckler` (sig.) | (3,0)(3,1) | vertical | Signature item: Shield 6 and 3 damage per 2 actions, so the tank fantasy shows from turn 1. |
| Crusader | `item_whetstone` | (1,1) | — | Touches the sword (+3 → 12): the adjacency lesson on turn 1. Its 3 open sides leave room for future blades. |
| Alchemist | `item_thorn_dart` | (1,0) | — | The Poison source. It reads before the Catalyst, so the Catalyst on action 1 detonates its Poison (6 dmg) and the innate re-seeds Poison 2. |
| Alchemist | `item_mortar` | (1,1) | — | Touches the dart (Poison 1 → 2 per action) and has 3 open sides for later affliction items: the build-around. |
| Alchemist | `item_plague_catalyst` (sig.) | (2,3)(3,3) | horizontal | Signature item, placed last in reading order. Its neighbours (2,2)(3,2)(1,3) stay free for a Bell, Lens or Censer. |
| Ranger | `item_crossbow` (sig.) | (0,0)(1,0) | horizontal | Signature burst: 15 per 3 actions. |
| Ranger | `item_thorn_dart` | (2,0) | — | Fires every action, so tempo shows from the start and the Ranger has a Poison hook. |
| Ranger | `item_swift_boots` | (3,0)(3,1)(2,1) | L | +3 speed (29 → 32) is the Tempo identity. This kit has no adjacency link by design. |
| Cleric | `item_drowsy_bell` (sig.) | (3,0)(3,1) | vertical | Signature item: Slow 2 per 2 actions (−20% enemy speed). |
| Cleric | `item_dagger` | (1,0) | — | The old kit (bell, bandage, buckler) had **0 damage** and could not win solo. The dagger is cheap damage that fires every action. |
| Cleric | `item_whetstone` | (1,1) | — | Touches the dagger (4 → 7 per action); the innate ward replaces the bandage and buckler. |

```
Crusader        Alchemist       Ranger          Cleric
S S S B         . D . .         X X D O         . G . L
. W . B         . M . .         . . O O         . W . L
. . . .         . . . .         . . . .         . . . .
. . . .         . . C C         . . . .         . . . .
```

**For the lead to apply:** set `startingItems` in `heroes_mvp.json` to the 3 ids above, make this layout the default placement, and **remove the scene's stash-spare seeding** (a code change).

## 2. Difficulty targets (the acceptance test)

### 2.1 Win rate per (room, door tier), Reasonable bot R (§5)

Each cell is the win rate of R's build at the start of that room against that cell's encounter(s), measured with probes (§6). A dash means the tier cannot appear in that room.

| Room | White | Green | Blue | Purple | Gold |
|---|---|---|---|---|---|
| 1 | 98–100 | — | — | — | — |
| 2 | 97–100 | 93–99 | — | — | — |
| 3 | 93–99 | 88–96 | 70–88 | — | — |
| 4 | 92–98 | 86–95 | 65–82 | — | — |
| 5 | 90–97 | 84–93 | 60–78 | 50–68 | — |
| 6 | 90–97 | 84–93 | 60–78 | 50–68 | 40–58 |
| 7 | 88–96 | 82–92 | 58–76 | 48–66 | 38–56 |
| 8 | 88–96 | 82–92 | 55–74 | 45–64 | 35–54 |
| Boss | 35–55 (conditional on reaching it) | | | | |

Why White and Green stay high: losses compound over 8 rooms. At these values R's main path (Green when offered) reaches the boss about 45–55% of the time, which gives "medium". Blue, Purple and Gold are the opt-in risk.

**Run level (R):** reach the boss 40–65%; clear the run 15–35%. **Hero parity:** Crusader and Alchemist within 15 points on boss win and within 10 points on reach-boss.

### 2.2 Starter-only bot S (kit only, never changes it)

One-hero fights have little randomness: only the enemies' move choice varies. A fixed kit against a fixed roster therefore wins about 0% or about 100%. For the cells that should be close, the test uses **fight margin** `FM` instead: on a win, +hero HP left %; on a loss, −enemy HP left %; range −100..+100.

| Cell | Target | Cell | Target |
|---|---|---|---|
| r1 White | win ≥ 95, FM +50..+100 | r3 Blue | win ≤ 15 |
| r2 White | win ≥ 95, FM +25..+80 | r4 White | FM −45..+10 |
| r2 Green | win 60–100, FM 0..+50 | r4 Green | win ≤ 30 |
| r3 White | FM −20..+35 (a close fight) | r4 Blue, all r5+ | win ≤ 5 (Purple, Gold and the boss ≤ 1) |
| r3 Green | FM −45..+15 | Run level (White doors, skips every reward) | reach r4 20–70%, reach r6 ≤ 5%, clear 0% |

### 2.3 Fight length (bot R)

| Metric | Rooms 1–2 | Rooms 3–8 | Boss |
|---|---|---|---|
| Hero actions p10 / p50 / p90 | ≥3 / 5–12 / ≤16 | ≥5 / 8–16 / ≤24 | ≥12 / 16–28 / ≤34 |
| All scheduled actions p90 | ≤ 30 | ≤ 55 | ≤ 60 |
| Seconds at 1× p90 | ≤ 20 | ≤ 40 | ≤ 50 (needs user OK; Q5) |
| Won in ≤ 2 hero actions | ≤ 5% (room 1 exempt) | ≤ 1% | 0 |
| Draws (200-action limit) | 0 | ≤ 0.5% | ≤ 0.5% |

Seconds: use the sum of the 1× presentation beats if the harness can count them. Otherwise use scheduled actions × `s_a`, where `s_a` is the mean seconds per action that the lead measures once from three 1× recordings.

## 3. Model used for the first pass

**F1. Encounter weight W and margin m.**
`DPS_i = E_i × v_i` (damage per 1000 ticks) · `W = (h·d / 10^6) × Σ_{i=1..n} DPS_i × Σ_{j≤i} HP_j` (enemies in slot order, because the hero focuses FirstEnemy) · `Π = EHP_h × DPS_h` · `m = Π / W` · damage taken `≈ W / DPS_h` · hero TTK `= 1000 × (h/1000) × Σ HP_j / DPS_h` ticks.

| Symbol | Type | Range | Description |
|---|---|---|---|
| `E_i` | float | 0..40 | Expected damage per own action. A windup and its linked heavy are averaged over both actions. |
| `v_i` | int | 1..200 | Monster speed |
| `HP_j` | int | 1..1000 | Monster base max HP |
| `h`, `d` | int ‰ | 1000..2500 | HP and damage multipliers of the cell |
| `DPS_h` | float | 0..2000 | Hero damage per 1000 ticks against one target |
| `EHP_h` | float | 1..1000 | Max HP + door HP + shield/heal gained over a typical fight |
| `m` | float | 0..∞ | > 1 means the hero outlasts the encounter |

*Output range:* `W > 0` for any legal roster. `m` is unbounded above. `DPS_h = 0` gives `m = 0`: the fight is unwinnable and ends as a Draw at 200 actions (the old Cleric kit). *Ignored by the model:* shield against multi-hit, Slow/Weaken/Frost, overkill, Poison decay, and EHP growing with fight length (the Crusader's buckler). Treat `m` as ordinal, ±30%. *Heuristic map, to be replaced by the measured curve after sweep 1:* m 2.0 → ≥95%, 1.7 → 90, 1.4 → 80, 1.2 → 68, 1.0 → 50, 0.8 → 25, ≤0.6 → ≤8.
*Worked example:* Crusader kit against r2 Green [hunter, hunter] at the §4 values. Hunter: `E = 6`, `v = 20`, DPS 120, HP 48. `Σ = 120·48 + 120·96 = 17,280`. `h·d = 1.060 × 1.040` gives `W = 19,050`. `Π = 150 × 165 = 24,750`, so `m = 1.30` (about 73%). Damage taken ≈ 115 of 150 EHP. TTK = 1000 × 102 / 165 = 618 ticks ≈ 14 Crusader actions.

**Kit power.** Crusader: 7.5 dmg/action × 22 = 165; EHP ≈ 110 + 14 + buckler ≈ 150; Π ≈ 24,750. Alchemist (hand trace against a hunter: about 10.4/action) × 26 ≈ 270 single-target plus AoE Poison; EHP 86; Π ≈ 23,200. Ranger ≈ 9.5 × 32 = 304 + 5 opener; EHP 82; Π ≈ 24,900. Cleric 7 × 24 = 168; EHP ≈ 150 (ward 4+4 per 2 actions, Slow); Π ≈ 25,200. **The four kits are within 10% of one another.**
**Assumed R growth (replace with the measured values; §6 reports them).** Π_R per room 1..8 and boss = 24.8k, 37.9k, 54.5k, 75.0k, 97.3k, 116.6k, 139.3k, 163.6k, 180k. This assumes damage ×1.0 → ×2.5 and EHP ×1.0 → ×1.95 as items fill the 16 cells by about room 6, plus about 8 door HP per room.
**Why one hero dies in rooms 4–6 today:** the old r4 Blue [elite, sweeper, hunter] has W ≈ 131,700, so m ≈ 0.57 even with R's growth. The old r5 Purple has W ≈ 229,000, so m ≈ 0.42. The rosters were authored for a 3-hero threat (`ThreatPartySize = 3`).

## 4. Tuning knobs and first-pass values

### 4.1 Knob order (turn the first that fixes the failing pattern)

| # | Knob | Current | Why this rank |
|---|---|---|---|
| 1 | Template rosters (`templates`) | 2–4 monsters from room 2 | Damage taken grows about n² with roster size and one hero cannot split targets. This is the cause of the failure. |
| 2 | Move `baseDamage` | see 4.2 | Linear in damage taken and leaves fight length unchanged. |
| 3 | Monster `baseMaxHp` | see 4.2 | Moves both difficulty and length; this is the lever for the §2.3 length targets. |
| 4 | Boss base stats | HP 320, 10/6/18 | The boss ignores ramps and kicks, so only its base stats move it. |
| 5 | `hpRampPerRoom` / `damageRampPerRoom` | 60 / 40 | Global slope; use it only when a whole row is off. |
| 6 | `tierInfo` kicks hp/dmg | Blue 100/50, Purple 200/100, Gold 300/100 | Spacing between columns. Moving the hp/dmg split is also the **length lever** (Purple 150/125, Gold 200/150 keep W within 3% and cut late fights by about 8%). |
| 7 | `startGold`, `gold` per tier, `itemRow` | 5; 2/3/5/7/10; as in data | Moves only R. Tune after the combat targets pass. |
| 8 | `baseSpeed` | 20/21/26/18/19/21/22 | Last resort: it changes rhythm, the forecast strip, and Slow/Freeze value. |

### 4.2 Monster first pass (`monsters_mvp.json`)

| Monster | HP old → new | Moves old → new | Speed | DPS/1000t new | Rationale |
|---|---|---|---|---|---|
| hunter | 52 → 48 | strike 7 → 6 | 20 | 120 | With 7, the Crusader kit sits at m 1.15 against r2 [hunter, hunter], which is below "easy". |
| striker | 64 → 60 | jab 6 → 5, bash 9 → 8 | 21 | 130 | A single striker is the r2 White fight; keeps that TTK under 10 hero actions. |
| swarmer | 38 → 34 | flurry 3×4 → 3×3 | 26 | 234 | It was the top DPS monster (312) and spiked solo fights; it stays the "kill it first" threat. |
| sweeper | 56 (=) | sweep 5 (=) | 18 | 90 | AllHeroes hits a solo hero once, so it is already the softest. |
| brute | 88 → 80 | strike 7 → 6, heavy 14 → 13 | 19 | 118 | Keeps the windup tell readable and makes it a mid-tier body. |
| elite_bruiser | 140 → 120 | 10 → 9, heavy 17 → 15 | 21 | 227 | Now fights alone at r3 Blue (TTK about 13 R actions). |
| boss | 320 → **480** | strike 10 → 18, sweep 6 → 12, crush 18 → 32 | 22 | ≈346 | Alone with no stacking, the old boss gives m ≈ 3.0 (too easy). The new one gives W ≈ 166k, m ≈ 1.08. **Most assumption-sensitive value:** after the sweep, set `HP × DPS = measured Π_R(boss) / 1.0` and keep about 25 hero actions. |

### 4.3 Rosters first pass (`run_mvp.json templates`; template ids, tiers and curses unchanged)

| Cell | Template | Old roster | New roster | W | m_R | m_S |
|---|---|---|---|---|---|---|
| r1 W | enc_r1_white | hunter | hunter | 5,760 | 4.3 | 4.3 |
| r2 W | enc_r2_white | hunter ×2 | striker | 8,600 | 4.4 | 2.9 |
| r2 G | enc_r2_green | hunter ×2, striker | hunter, hunter | 19,040 | 2.0 | 1.30 |
| r3 W | enc_r3_white | swarmer, hunter | hunter, striker | 23,960 | 2.27 | 1.03 |
| r3 G | enc_r3_green | swarmer, hunter, striker | brute, hunter | 30,010 | 1.82 | 0.82 |
| r3 B | enc_r3_blue | elite, hunter | elite | 37,560 | 1.45 | 0.66 |
| r4 W | enc_r4_white | sweeper, hunter | brute, striker | 36,540 | 2.05 | 0.68 |
| r4 G | enc_r4_green | sweeper, hunter, striker | swarmer, hunter, sweeper | 39,950 | 1.88 | 0.62 |
| r4 B | enc_r4_blue | elite, sweeper, hunter | sweeper, elite | 59,850 | 1.25 | 0.41 |
| r5 W | enc_r5_white | swarmer, hunter, swarmer | sweeper, hunter, striker | 52,400 | 1.86 | 0.47 |
| r5 G | enc_r5_green | swarmer, hunter, swarmer, striker | swarmer, brute, hunter | 58,740 | 1.66 | 0.42 |
| r5 B | enc_r5_blue | elite, swarmer, hunter | elite, brute | 82,410 | 1.18 | 0.30 |
| r5 P | enc_r5_purple | elite, sweeper, swarmer | elite, striker | 91,860 | 1.06 | 0.27 |
| r6 W | enc_r6_white | brute, hunter | swarmer, brute, hunter | 63,720 | 1.83 | 0.39 |
| r6 G | enc_r6_green | brute, hunter, striker | brute, sweeper, hunter | 68,270 | 1.71 | 0.36 |
| r6 B | enc_r6_blue | elite, brute, hunter | elite, striker | 88,620 | 1.32 | 0.28 |
| r6 P | enc_r6_purple | elite, sweeper, swarmer | elite, brute | 99,140 | 1.18 | 0.25 |
| r6–7 Gold | enc_r6-8_gold_anchor, _wither | elite, sweeper, swarmer | elite, swarmer | 131.6k (r7 140.7k) | 0.89 (r7 0.99) | 0.19 |
| r7 W | enc_r7_white | brute, swarmer, hunter | brute, sweeper, hunter | 73,780 | 1.89 | 0.34 |
| r7 G | enc_r7_green | brute, swarmer, hunter, striker | brute, swarmer, hunter | 93,670 | 1.49 | 0.26 |
| r7 B | enc_r7_blue | elite, brute, swarmer | swarmer, elite, sweeper | 116,400 | 1.20 | 0.21 |
| r7 P | enc_r7_purple | elite, sweeper, swarmer | elite, swarmer | 132,250 | 1.05 | 0.19 |
| r7 Gold | enc_r7-8_gold_sluggish | elite, sweeper, swarmer | swarmer, elite, hunter | 149,350 | 0.93 | 0.17 |
| r8 W | enc_r8_white | brute, sweeper, hunter | brute, sweeper, striker | 85,740 | 1.91 | 0.29 |
| r8 G | enc_r8_green | brute, sweeper, hunter, swarmer | brute, swarmer, hunter | 101,000 | 1.62 | 0.25 |
| r8 B | enc_r8_blue | elite, brute, sweeper | swarmer, elite, brute | 142,600 | 1.15 | 0.17 |
| r8 P | enc_r8_purple | elite, sweeper, swarmer | swarmer, elite, hunter | 150,150 | 1.09 | 0.16 |
| r8 Gold | enc_r8_gold_frailty, enc_r8_gold_anchor | elite, sweeper, swarmer | elite, brute, hunter | 191,300 | 0.86 | 0.13 |
| Boss | enc_boss | boss | boss (4.2 stats) | 166,000 | 1.08 | 0.15 |

### 4.4 Unchanged in pass 1, on purpose

Ramps 60/40, the kicks, `startGold` 5, gold 2/3/5/7/10, `itemRow` and all speeds stay as they are. Pass 1 changes only knobs 1–4, so the sweep is not confounded. With 5 starting gold, R can forge one White kit item before room 1.
**Pass-2 rules:**
- A whole R row off by more than 5 points: change the ramp. Each 10‰ of `hpRampPerRoom` moves room 8 W by about 7%.
- One tier column off: change that tier's kick.
- Length fails while win rate passes: move kick weight from hp to dmg (4.1 #6).
- Parity fails: change the leading hero's kit item, not the monsters.

## 5. Bot policies (codeable)

**Common.** One hero at slot 0. Fights use the real combat core. Kit items never move. A Draw counts as a loss.

**Bench(grid, door HP, r).** Mean of `fightScore` over benchmark seeds 1..4, where the fight is the grid against template `T(r)`:
- `T(r)` is `enc_r{r}_green` for r = 2..8, `enc_r1_white` for r = 1 and the boss for r = 9.
- `fightScore` is `1000 + 5·heroHpLeft%` on a win and `5·enemyHpRemoved%` on a loss.
- Ties go to the fewer total hero actions.
- `r` is the room of the *next* fight: the current room in Planning, `r + 1` in Reward.
- The seeds are harness constants given as the fight seed. They are not the run's streams and reveal nothing hidden.

**Place(grid, item).** Enumerate the legal `(anchorRow, anchorCol, rot 0..3)` in that order. Score each by the number of System 10 links that touch the new item (given or received). Take the maximum. On ties, take the **last** candidate for a `detonator` item and the **first** for any other item.

**Reasonable bot R.**
1. *Reward:* `B0 = Bench(grid)`. For each offered item, try `Place`. If it does not fit, try each placed non-signature item X in reading order: remove X, then `Place` the new item. Keep the best grid by Bench; X is sold. Take the offer with the largest Bench if it is greater than `B0`, otherwise skip. Ties go to the lower offer index.
2. *Forge:* runs in each Planning phase, including before room 1. Repeat: among the placed items with an upgrade whose cost ≤ gold, pick the one with the largest `ΔBench / cost` (ties: higher tier, then reading order). Apply it if `ΔBench > 0`, otherwise stop.
3. *Sell:* only the item displaced in step 1.
4. *Buff:* always claim it for hero 0.
5. *Door:* Green if any card is Green, otherwise White, otherwise the lowest tier offered (ties: lowest index). It never chooses Blue, Purple or Gold on its main path; probes cover those cells.
6. *Diagnostic arm R-Greedy* (not part of the acceptance test): the highest non-Gold tier offered.

**Starter-only bot S.** The kit at the §1 layout, with door HP 0 in probes. It never takes a reward, forges, sells or moves anything. For the run-level row it picks the lowest-tier door and claims buffs.

## 6. Simulation protocol

- **Seeds:** System 35 Formula 1. Tuning uses master 11 and validation uses master 12, which stays held out until acceptance. Benchmark seeds are 1..4 and probe seeds are 101..104.
- **S:** fight-only (Stage A driver). For each hero and each template of every cell, 200 seeds.
- **R:**
  - Tuning: 300 runs per hero per iteration. Acceptance: 1,000 runs per hero on master 12.
  - **Probes:** at each room's Planning step, after forging, run every template of every `(r, tier)` cell × 4 probe seeds with the current grid and door HP. Probes never change the run state.
  - **Ghost continue:** a lost real fight ends the run for the run-level metrics. The run is then flagged and continued as if won (gold and offers as normal), so the probes for later rooms keep their sample size.
  - Report probe win rates with the Wilson interval using `n` = runs, which is conservative because probes share a build.
- **Report per hero × bot:**
  1. Per-cell win % with n and Wilson 95%, and median FM.
  2. Per cell: hero actions p10/p50/p90, all scheduled actions p50/p90, and seconds p90.
  3. R build per room: items placed, cells used, gold held and spent, upgrades, measured `DPS_h`, `EHP_h` and **Π** (recalibrate W from these: §3).
  4. Run level: reach-room-k curve, reach boss, clear, histogram of the room of defeat.
  5. Offer take/skip rate and the top 10 items taken per hero.
  6. Outliers: wins in ≤ 2 hero actions, Draws, and the first 5 seeds of each.
- **Acceptance:** for both MVP heroes, every §2 cell and row is inside its band on the validation master. At most 3 of the 29 R cells may miss, by ≤ 3 points each. Change one knob family per iteration, in §4.1 order, and keep each patch as `path = value`.

## 7. Open questions and flags

1. **Boss numbering.** The data runs 8 rooms and then the boss as fight 9; the user said "boss (room 8)". If the boss should be the 8th fight, set `rooms: 7` and drop row 8 of §2.1.
2. `monsters_mvp.json` has its own `tierTable` (ramps 60/40, kicks with no Gold row), which duplicates `run_mvp.json`. Confirm that only `run_mvp.json` is read, then delete the duplicate or keep it in sync.
3. Danger pips (`dangerThresholds` 20/40/70/110, threat at party size 3) will re-cut with the new rosters. This only changes what the door card shows; re-check that rooms 2–8 still spread over 1..5.
4. Assumption A1 (full HP each fight) and the R growth curve in §3 carry the whole first pass. Recalibrate them from report item 3 before reading anything into a near-miss.
5. The user's 40 s cap is newer than System 29's 90 s median. This spec follows the 40 s cap and allows the boss up to 50 s p90, which needs user approval.
6. The registry (`design/registry/entities.yaml`) was **not checked** for this draft. If the monster, boss or kit values are registered, they need a registry update when the lead commits the values.
