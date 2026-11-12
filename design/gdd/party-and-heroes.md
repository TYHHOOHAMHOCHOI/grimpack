# Party and Heroes

> **Status**: Designed (pending independent /design-review)
> **Creative Director Review (CD-GDD-ALIGN)**: NOT ASSESSED 2026-11-12 — not run: specialist and director agents are spawned only when the user asks, and no request was made this session
> **Author**: user + Claude design session
> **Last Updated**: 2026-11-12
> **Last Verified**: 2026-11-12
> **Implements Pillar**: Arrangement Is the Thinking (primary); Power Has a Price (supports)

## Summary

Party and Heroes defines the heroes the player can pick, how three of the four become the party, and what each hero brings into a fight: its HP, speed, initial shield, its own backpack, a signature starting item and an innate passive ability. Every hero is restored to full strength before each encounter, so a fight is judged on the build, not on carried damage. It exists so that each hero is a distinct, nameable stage of the machine the player arranges (Pillars 4 and 5, and the concept's mechanical identity).

> **Quick reference** — Layer: `Core` · Priority: `MVP` · Key deps: `Game Data Definitions, Grid Backpack`

## Overview

System 15 defines who the player's heroes are and how they enter a run and each fight. A hero is a named character with a role, a base maximum HP, a base speed, an initial shield, a backpack profile (the shape of its grid and the order in which its cells unlock) and a short list of starting items. At the start of a run the player picks three of the four MVP heroes. The three join in a fixed order that gives each a party slot (0, 1, 2); the slot never changes during the run and is used only to break ties and to order targets (Systems 3 and 11). Each hero owns one grid, and System 15 asks System 8 to place its starting items. Before every encounter every hero is restored to full HP with its initial shield, whether or not it fell in the last fight; what carries over from fight to fight is the arrangement and any permanent growth of maximum HP. At the start of an encounter System 15 supplies each hero's combat baseline: identity, slot, maximum HP, HP, shield and the base speed that System 11 reads. Without this system there would be no one to arrange items for; with it, each hero is a distinct stage of the machine that the player chooses, names and learns (Pillar 4), and a lost fight is a clean verdict on the build, not a slow bleed.

> *Specialists not consulted by the authoring session (not requested). Review manually before production.*

## Player Fantasy

The player chooses three heroes and learns what each is for: the Crusader who takes the hit, the Alchemist who prepares, the Cleric who keeps the others standing. They give each a grid that grows its own way, and soon they are saying "my back-row detonator" about a hero, not about a stat line. **Each hero is a stage of one machine, and the player decides which three stages to build.** When the fight is won everyone stands again at full strength, so the cost of a mistake is the fight itself and the build it exposed. This serves the Core Fantasy (the architect of a perfect machine), Expression (which three heroes, which items on whom) and the relatedness the concept promises through mechanical identity.

*Pillar anchors.* Pillar 4, Arrangement Is the Thinking: "The three heroes are three stages of one machine." The party is those three stages, chosen before the run and shown before every fight. The concept's relatedness need: "Each hero has a mechanical identity (position and role in the sequence). The player attaches to 'my back-row detonator'."

*References.* Backpack Hero and Slay the Spire let the player choose a character and learn it; this game asks the player to choose three and to make them work as one machine.

> *`creative-director` not consulted (specialists not requested). Review manually before production.*

## Detailed Design

### Core Rules

1. **Roster and hero definition.** The MVP roster has four heroes: Crusader, Alchemist, Ranger and Cleric (provisional; names are text keys, System 7). A hero definition (System 2) has: a stable ID, a name key, a role key (and the art role family, the art-director's), a base max HP `h`, a base speed `s` (`5..60`), an initial shield `q0`, a backpack profile (System 8: bound at most 6×6, start block 4×4, ordered unlock sequence), a list of starting items (exactly one marked **signature**) and exactly one **innate item**. The prototype's preferred position (frontline, midline, backline) is not adopted.
2. **Party formation.** At the start of a run the player picks exactly three distinct heroes from the roster (all four are available in the MVP; meta unlocks are Alpha). The **order of picking** gives the party slots 0, 1 and 2. The choice is confirmed once; there is no reordering during the run. Slot is used only for tie-breaks and target order (Systems 3 and 11) and carries no other mechanical meaning.
3. **Hero instance.** Each picked hero becomes a hero instance whose ID equals its slot (0, 1 or 2), assigned in the same way in the headless and live runs. System 36 and System 4 use this ID. A hero instance holds one run-persistent value, `B_perm` (permanent max-HP growth, a non-negative integer, `0` at the start), and the identities of its grid, its innate item and its items.
4. **Starting items.** At run start, in slot order and then list order, System 15 takes a new item ID from the item-ID source (System 8, Rule 10) and asks System 8 to `Create` the item and `AutoPlace` it into that hero's grid. The signature item is an ordinary item, movable to another hero or the stash. A starting set that does not fit, or a placement that fails, is a content error that stops the run before it starts.
5. **Innate item.** Each hero has exactly one innate item, instantiated at run start with its own item ID. It uses the item data of Systems 2 and 9 (a trigger of `Cooldown`, `Reaction` or `CombatStart`, and effects of the kinds System 3 allows). It occupies no cell, cannot be moved, removed or lost, receives and gives no synergy (System 10 considers only placed items), and does not appear among the grid items of System 8. Its ordering key is anchor row `−1`, anchor column `0`, so it fires and reacts before the items in the grid, and it appears in the placement baseline with that anchor and the flag `innate`.
6. **Restoration.** Before every encounter every hero is restored: HP equals max HP, the shield equals `q0`, and a hero that fell in the last fight stands again. Nothing about HP, shield or downed state carries over between encounters. What persists is `B_perm`, the arrangement and the items.
7. **Max HP.** `maxHP = clamp(h + B_perm + H, 1, HP_cap)`, where `H` is the clamped sum of the `hpBonus` of the passive items in the hero's frozen grid (Formula 1). Items in the stash or the tray do not count.
8. **Combat baseline.** At encounter start System 15 supplies, for each hero in slot order: instance ID, definition ID, slot, `maxHP`, HP (equal to `maxHP`), shield (`q0`), base speed `s`, the speed sum `F` (System 11, Rule 1) and the innate item instance. `HeroStats(definition, B_perm, arrangementView)` is a pure query that returns the same values during planning, so the screen and the forecast can show them before the fight.
9. **Defeat and growth.** There is no permanent death. A hero that is downed stays down for the rest of that encounter (Systems 36 and 12); the run ends only when System 12 reports a defeat. `B_perm` grows only through rewards that other systems define (Systems 17 and 18); this system stores and applies it.
10. **Party size.** The contract allows one to three heroes (the art bible's one column); the MVP always has exactly three. Recruiting (Tier 2) would add a hero in the next free slot with the next instance ID; more than three heroes needs an art-bible change (Tier 3).
11. **Determinism.** No RNG, no clock. Instance IDs, item IDs and the order of operations depend only on the picks and the roster. Integer arithmetic only.
12. **Engine-free.** The same engine-free rules as Systems 3 and 11: no `UnityEngine`, no mutable static state.

### States and Transitions

| Subject | State | Trigger | Result |
|---|---|---|---|
| Party | Unformed | Player confirms three picks | Formed: slots 0, 1, 2; instances created; starting items placed. |
| Hero | Restored | Encounter start | HP = max HP, shield = `q0`. |
| Hero | Restored | Fight | Active; may be downed (System 36). |
| Hero | Downed | Encounter ends | Restored at the next encounter. |
| Hero | Any | Reward of permanent max HP | `B_perm` increases. |

### Interactions with Other Systems

| System | Data in | Data out | Interface owner |
|---|---|---|---|
| 2 Game Data Definitions | Hero definitions: stats, profile, starting items, innate item; `hpBonus` of passive items | None | System 2 owns fields and validation. |
| 8 Grid Backpack | The frozen arrangement; `Create`, `AutoPlace` | Hero instance IDs and slot order; starting items | System 8 owns grids; System 15 owns heroes. |
| 36 Combat State | Receives the baseline | Combat baseline per hero | System 36 validates and stores it. |
| 11 Turn Order | Base speed, slot, speed sum `F` | None | System 11. |
| 9 Item Trigger, 3 Effect Resolution | The innate item with key `(−1, 0)` | None | Systems 9 and 3 own firing and ordering. |
| 4 Event Log | Hero instance IDs; the innate rows in the placement baseline | None | System 4 owns the schema. |
| 5 Run State and Flow | Picks, `B_perm`, instance IDs | The run's hero slice | System 5 owns the snapshot. |
| 12 Combat Resolution | Baseline | Roster for the encounter | System 12. |
| 17, 18 (rewards), 31 (selection screen) | `B_perm` growth; the pick flow | Results | Those systems decide when. |

*Provisional:* the `hpBonus` field, the innate item with its ordering key, and the baseline row for it are assumptions about Systems 2, 3, 4 and 9, which have not agreed to them yet.

> *Specialists (`game-designer`, `systems-designer`, `art-director`, `narrative-director`) not consulted (not requested). Review manually before production.*

## Formulas

All arithmetic is on 32-bit integers. Nothing here uses floating point, RNG or time.

**Formula 1: maximum HP.** `maxHP = clamp(h + B_perm + H, 1, HP_cap)`, with `H = clamp(Σ hpBonus, −50, 200)` over the passive items in the hero's frozen grid.

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| `h` | int | `40..300` (provisional) | hero definition (System 2) | Base max HP. |
| `B_perm` | int | `0..1000` (provisional) | run state (rewards of Systems 17 and 18) | Permanent max-HP growth. |
| `hpBonus` | int | `−20..40` per item (provisional) | passive item definition (System 2) | An item's HP bonus. |
| `H` | int | `−50..200` | calculated | Clamped sum of the item bonuses. |
| `HP_cap` | int | `10,000` (provisional) | constant | Largest max HP. |
| `maxHP` | int | `1..10,000` | calculated | Maximum HP of the hero. |

*Examples:* the Crusader (`h = 110`) with `B_perm = 0` and passive items of `+20` and `+10` has `H = 30` and `maxHP = 140`; the same hero with those items in the stash has `maxHP = 110`; a hero with `h = 40` and passive items summing to `−60` has `H = −50` and `maxHP = 1` after the clamp (`40 − 50 = −10`, raised to 1).

**Formula 2: encounter start state.** For every hero, `HP = maxHP` and `shield = q0`, with `q0` in `0..30` (provisional). Nothing from the last fight carries over.

**Formula 3: party formation.** The picks `p_1, p_2, p_3` must be three distinct roster heroes; the slot of `p_i` is `i − 1`, and the hero instance ID equals the slot. *Example:* picking Ranger, Crusader, Cleric gives slots 0, 1, 2 and instance IDs 0, 1, 2 in that order.

**Formula 4: speed handed to System 11.** The base speed `s` is passed as authored, and the speed sum is `F = clamp(Σ speedBonus, −50, 50)` over the same passive items (System 11, Formula 1).

**Formula 5: starting loadout check.** For every hero, `Σ |cells(item)| ≤ 16` over its starting items (`hero_grid_start_cells`), and `AutoPlace` finds a free placement for each in list order. *Illustrative example:* a sword of 2 cells, a whetstone of 1 and a buckler of 4 total 7 cells, which is at most 16.

**Worked example.** The player picks Ranger, then Crusader, then Cleric. The slots are Ranger 0, Crusader 1, Cleric 2. The baselines (prototype stats, provisional): Ranger `h = 82`, `s = 29`, `q0 = 0`; Crusader `h = 110`, `s = 22`, `q0 = 6`; Cleric `h = 92`, `s = 24`, `q0 = 4`. If the Crusader's grid holds a passive item with `hpBonus = +20` and `speedBonus = +2`, its `maxHP` is 130, its `F` is 2 and System 11 derives `v = 24`. The Ranger is slot 0, so on an exact tie in gauge and speed it acts before the others.

> *`systems-designer` not consulted (not requested). Review manually before production.*

## Edge Cases

- **If the picks are fewer or more than three, a duplicate, or not in the roster**: the party is not formed and nothing is created. A hero locked by a future meta unlock counts as not in the roster.
- **If a hero's starting items do not fit or `AutoPlace` fails**: a content error stops the run before it starts (Rule 4).
- **If the item-ID source fails at run start**: a technical stop before the run starts.
- **If a hero's grid holds no items**: valid. Its innate item still works.
- **If a passive HP item moves between a grid and the stash**: `maxHP` changes the next time the baseline is computed; the preview shows it at once. There is no current HP during planning, so nothing is healed or lost.
- **If `h + B_perm + H` is below 1**: `maxHP` is 1 (Formula 1). **If it is above `HP_cap`**: `HP_cap`.
- **If a hero is downed in a won encounter**: it stands again at full HP and its initial shield in the next encounter (Rule 6).
- **If every hero is downed**: System 12 reports a defeat and the run ends; no restoration is needed.
- **If a reward of permanent max HP arrives**: `B_perm` increases and applies from the next encounter start.
- **If the player tries to reorder the party**: there is no such operation; the slot order is fixed.
- **If an innate item's trigger names an unknown status or an effect kind System 3 defers**: System 2 rejects the catalog.
- **If a `CombatStart` or `Reaction` innate item is present**: it acts before the grid items of the same hero (its anchor row is `−1`).
- **If a hero is added later (Tier 2 recruiting)**: it takes the next free slot and the next instance ID; slots of existing heroes do not change.
- **If the same picks are made twice**: the instance IDs, item IDs and starting placements are identical.

> *`systems-designer` not consulted (not requested). Review manually before production.*

## Dependencies

| System | Direction | Hard/Soft | Contract | Back-reference status |
|---|---|---|---|---|
| 2 Game Data Definitions | Upstream | Hard | Hero definitions, passive item `hpBonus` and `speedBonus`, innate items, ranges and validation. Provisional: System 2 has none of these fields yet. | A row is added to System 2's Open Questions. |
| 8 Grid Backpack | Upstream | Hard | `Create`, `AutoPlace`, the frozen arrangement view, the backpack profile. | Present (System 8, Dependencies row for System 15); wording updated. |
| 36 Combat State and Step Model | Downstream | Hard | Receives the combat baseline per hero. | Updated. |
| 11 Turn Order | Downstream | Hard | Base speed, slot, speed sum `F`. | Present (System 11, Dependencies). |
| 9 Item Trigger and Cooldown | Downstream | Hard | The innate item with ordering key `(−1, 0)`; passive bonuses. | Updated. |
| 3 Effect Resolution | Downstream | Soft | Party slot as the target order; the innate item's listener key. | Updated. |
| 4 Event Log | Downstream | Soft | Hero instance IDs; innate rows in the placement baseline. | Recorded in System 4's Open Questions. |
| 5 Run State and Flow | Peer | Hard | Stores the picks, `B_perm` and instance IDs in the run snapshot. | Not designed. |
| 12 Combat Resolution | Downstream | Hard | Takes the baseline for the roster. | Updated. |
| 14 Enemy and Intent | Downstream | Hard | Hero instance IDs and the party-slot order, used as the candidate list for enemy targets. System 14 is Designed ([enemy-and-intent.md](enemy-and-intent.md)). | Present (System 14, Dependencies). |
| 17 Door Generation, 18 Commitment and Drop Model | Peer | Soft | Rewards that grow `B_perm`. | Not designed. |
| 31 Menus and Screen Flow | Downstream | Soft | The hero-picking screen. | Not designed. |
| 1 Seeded RNG | None | n/a | System 15 draws no RNG. | No change. |

**Circular-dependency check:** System 15 reads Systems 2 and 8 and writes to Systems 36, 12 and 5; none of them reads System 15's output back except through the baseline.

## Tuning Knobs

| Knob | Default | Safe range | Affects | If too high | If too low |
|---|---|---|---|---|---|
| Base max HP `h` | per hero, `40..300` (provisional; the prototype uses 82 to 110) | 40 to 300 | How long a hero lasts. | The party outlasts every fight; the build matters less. | A single hit downs the hero. |
| Base speed `s` | per hero, `5..60` (System 11; the prototype uses 22 to 29) | 5 to 60 | How often a hero acts. | The hero dominates the action limit. | The hero rarely acts. |
| Initial shield `q0` | per hero, `0..30` (provisional) | 0 to 30 | The cushion at the start of each fight. | Early damage is absorbed and the opening is trivial. | No cushion. |
| Passive item `hpBonus` | per item, `−20..40` (provisional) | −20 to 40 | How far a build can move max HP. | One item decides survival. | HP items are not worth a cell. |
| `B_perm` cap | 1000 (provisional) | 100 to 5000 | Total growth from rewards. | Runs grow past the damage scale. | Rewards of max HP are pointless. |
| `HP_cap` | 10,000 (provisional) | 1000 to 100,000 | The largest max HP. | Large numbers hide damage. | Growth saturates. |
| Starting items per hero | 2 to 4 | 1 to 6 | How playable the first fight is. | The grid starts crowded. | The first fight has too little. |
| Party size | 3 | fixed in the MVP | The number of stages of the machine. | Needs an art-bible change. | Fewer stages. |

**Not knobs.** One signature item and one innate item per hero, the pick-order slots, the restoration before every encounter, and the absence of permanent death are design rules; changing them bumps the rules version. **Interactions:** `h`, `q0` and the damage scale of System 3 set how many hits a hero takes; `s` interacts with the content lint of System 11.

> *`systems-designer` not consulted (not requested). Review manually before production.*

## Visual/Audio Requirements

- **Identity is role family plus one prop (art bible 3.3.2, 5.2.3).** Each hero reads as a role family (Block, Round or Tall) with one identifying prop. The art bible has three families for the starting archetypes; the MVP roster has four heroes, so two of them share a family and must differ in prop zone (Crown, Hand or Back), because two heroes in the same party must not share both. Any party of three picked from the four must pass the half-size silhouette test.
- **One column, one pedestal size.** The party stands in one column in slot order with the join numeral on the pedestal; an empty slot is an empty pedestal outline, never a gap (art bible 3.3.2). Slot carries no form cue.
- **Heroes do not attack.** A hero's body shows only idle, ready, hurt and down. Everything a hero does is shown by item avatars above its head (art bible 5.10), including the innate item (see below).
- **The innate item has no plate in the grid.** Its look, its avatar and the place where the player reads it are not defined by the art bible (Open Questions).
- **Restoration is visible.** At the start of every encounter the heroes' HP rows show full HP and the initial shield, and a hero that fell is shown standing again.
- **Audio.** This system makes no sound.

> 📌 **Asset Spec** — Visual/Audio requirements are defined. After the art bible is approved, run `/asset-spec system:party-and-heroes` to produce per-asset visual descriptions, dimensions and generation prompts from this section.

> *`art-director` not consulted (not requested). Review manually before production.*

## Game Feel

- **Picking is quick and reversible until confirmed.** Choosing three of four takes a few clicks; the player can change a pick before confirming. After confirmation the party is fixed.
- **A fresh start every fight.** The restoration needs no player action and no waiting; the next encounter simply begins with the party at full strength.
- **No hidden stat.** Everything that sets a hero's HP and speed is shown with its source (UI Requirements), so the player never wonders why a number changed.

## UI Requirements

- **Hero picking screen (System 31 owns the screen):** four hero cards, each with the name, the role, base max HP, base speed, initial shield, the starting items (the signature item marked), the innate item described in words, and the shape of the grid with its first unlock cells. The join numerals 1, 2, 3 appear in the order of picking; confirming is a separate action.
- **Party column and popup:** the HP row shows current and max HP; the popup of a hero shows its max HP with sources ("140 = base 110 + 30 from items"), its speed with sources (System 11) and its innate item.
- **Arrangement screen:** changing an item that carries `hpBonus` or `speedBonus` shows the new max HP and speed at once (`HeroStats`), together with the order strip (System 11).
- **Text first:** every number has a text equivalent for keyboard and gamepad focus, and the pick order is announced as text.

> 📌 **UX Flag — Party and Heroes**: This system has UI requirements. In Phase 4 (Pre-Production), run `/ux-design` for the hero picking screen, the party column and the popup before writing epics. Stories that reference UI should cite `design/ux/[screen].md`, not the GDD directly.

## Cross-References

| Reference | What it provides here |
|---|---|
| [Game Concept](game-concept.md), Core Mechanics, MVP Definition and Scope Tiers | Three heroes in the MVP; recruiting in Tier 2; up to six heroes in Tier 3; the relatedness need. |
| [Systems Index](systems-index.md), row 15 | Dependencies 2 and 8. |
| [Game Data Definitions](game-data-definitions.md), Core Rules 2 | The hero record: base HP, speed, initial shield, backpack profile, starting item references. |
| [Grid Backpack](grid-backpack.md), Rules 2, 10 and 17 | The backpack profile, the item-ID source, `Create` and `AutoPlace`, the hero ID and party-slot order of the view. |
| [Turn Order](turn-order.md), Rule 1 and Formula 1 | Base speed, party slot, the passive speed sum `F`, the tie-break by slot. |
| [Item Trigger and Cooldown](item-trigger-and-cooldown.md), Rules 1, 8 and 10 | Trigger kinds, the ordering key, combat-start items. |
| [Effect Resolution Model](effect-resolution-model.md), Canonical Order Contract | The party slot as target order; listener keys. |
| [Combat State](combat-state-and-step-model.md), Core Rule 1 and Formulas | Hero identities, HP and shield domains. |
| [Combat Resolution](combat-resolution.md), Core Rules 1 to 3 | The roster and the baseline; defeat when every hero is downed. |
| [Event Log](event-log.md), Placement Baseline | Hero instance IDs and the innate rows. |
| [Art Bible](../art/art-bible.md), 3.3.2 and 5.2.3 | One column, role families, props, join numeral. Its sentence that the run starts with one hero and recruits more is not the MVP rule. |
| Prototype `HeroDefinitionSO.cs`, `HeroCombatant.cs`, `DefaultGameCatalog.cs` (evidence only) | Four heroes with stats and a 5×4 bag; preferred position; direct mutation. Stats are migration inputs; the rest is not adopted. |

## Acceptance Criteria

**Fixtures.**
- `ROSTER-4`: Crusader (`h = 110`, `s = 22`, `q0 = 6`), Alchemist (`86`, `26`, `0`), Ranger (`82`, `29`, `0`), Cleric (`92`, `24`, `4`), each with a test set of starting items (one marked signature) and one innate item, from a test-only catalog.
- `PARTY-A`: picks Ranger, Crusader, Cleric in that order.
- `SWORD` (2 cells), `WHETSTONE` (1), `BUCKLER` (4), `HP-RING` (passive, 1 cell, `hpBonus = +20`, `speedBonus = +2`), `HP-CHARM` (passive, 1 cell, `hpBonus = +10`) in the test catalog. Grids use System 8 `PROFILE-C` (4×4).
- Logic tests live under `Assets/_Project/Tests/EditMode/PartyAndHeroes/`, in the existing `Project05.EditModeTests` assembly. The engine-free core assembly is new; its name and settings are fixed by an ADR (NOT SOURCEABLE).

1. **Hero definition validation (Rule 1; blocked until System 2 has the fields).** **Given** a hero with no or two signature items, no or two innate items, a base speed outside `5..60`, an innate trigger of `Passive`, or a starting set outside `1..6` items, **then** the catalog is rejected naming the record and field.
2. **Party formation (Rule 2, Formula 3).** **Given** `PARTY-A`, **then** the slots are Ranger 0, Crusader 1, Cleric 2; **given** two, four, a duplicate, or an unknown hero, **then** no party is formed and nothing is created.
3. **Instance IDs (Rule 3).** **Given** `PARTY-A`, **then** the instance IDs equal the slots, and two runs with the same picks give the same IDs.
4. **Starting items (Rule 4, Formula 5).** **Given** `PARTY-A`, **then** item IDs are taken from the ID source in slot order and then list order, each item is placed in its own hero's grid, and the signature item can be placed in another hero's grid with a normal `Place`; **given** a set that does not fit, **then** a content error is reported and no run starts; **given** the ID source failing, **then** a technical stop and no party.
5. **Innate item (Rule 5).** **Given** a hero, **then** its innate item occupies no cell, is absent from the grid items of the arrangement view, cannot be moved or removed, receives no synergy modifier, and has a baseline row with anchor row `−1` and the flag `innate`.
6. **Innate ordering (Rule 5; provisional).** **Given** a hero with an innate `Cooldown` item and grid items ready on the same action, **then** the innate item fires first. *[BLOCKED] until Systems 3 and 9 accept the key.*
7. **Restoration (Rule 6, Formula 2).** **Given** a hero downed or with its shield spent in encounter 1, **then** encounter 2 starts with HP equal to `maxHP` and shield equal to `q0`.
8. **Max HP (Formula 1).** **Given** the Crusader with `HP-RING` and `HP-CHARM` in its grid, **then** `maxHP = 140`; **given** both in the stash, **then** `110`; **given** `h = 40` and items summing to `−60`, **then** `1`; **given** a sum above the cap, **then** `HP_cap`; **given** `B_perm = 25`, **then** `maxHP` is 25 higher.
9. **Combat baseline (Rule 8).** **Given** `PARTY-A` with `HP-RING` on the Crusader, **then** the baseline lists the three heroes in slot order with `maxHP`, HP, shield, base speed `s`, `F = 2` for the Crusader, and the innate item instance; **given** the same arrangement, **then** `HeroStats` equals the baseline and leaves the state value-equal.
10. **Permanent growth (Rule 9).** **Given** a reward of `+25` permanent max HP, **then** `B_perm` increases, applies from the next encounter start, persists through later encounters and never decreases.
11. **Party size (Rule 10).** **Given** the MVP flow, **then** exactly three heroes; **given** a direct call with one to three heroes, **then** accepted; **given** four, **then** rejected.
12. **Speed handoff (Formula 4).** **Given** the Crusader with `HP-RING`, **then** `F = 2`; **given** the ring in the stash, **then** `F = 0`; **given** sums beyond `±50`, **then** clamped.
13. **Determinism (Rule 11).** **Given** the same picks and catalog, **then** two runs give identical IDs, placements and baselines, and no RNG draw or clock read occurs (a counting probe stays at zero).
14. **Engine-free (Rule 12).** **Given** the core assembly, **then** its definition sets `noEngineReferences`, and a CI scan finds no `UnityEngine` reference, no mutable `static` field, no floating-point type and no `DateTime` or `Random`.
15. **Allocation.** **Given** a warmed-up `HeroStats` with caller buffers, **when** called 1,000 times, **then** the managed allocation delta is zero, measured with a control that does allocate.
16. **Integration with System 11 (provisional).** **Given** `PARTY-A` and an enemy, with equal gauge and speed, **then** the tie goes to slot 0 (Ranger). *[BLOCKED] until System 11 is implemented.*
17. **Integration with Systems 4, 5, 12 and 36 (provisional).** **Given** the spike fixture, **then** the baseline is accepted by System 36, the trace lists the hero instance IDs and innate rows, and the picks and `B_perm` survive in System 5's snapshot slice. *[BLOCKED] until those systems are revised or designed.*
18. **UI evidence.** **Given** the hero picking screen, the party column and a hero popup, **then** retained screenshots in `production/qa/evidence/` show the four cards with the signature item marked, the join numerals in pick order, and the max-HP sources. *[BLOCKED] until the UI exists.*

> *`qa-lead` not consulted (not requested). Review manually before production.*

## Open Questions

| Question | Owner | Resolve by |
|---|---|---|
| System 2 needs: hero fields (signature mark, one innate item, backpack profile, starting items), the passive item fields `hpBonus` and `speedBonus`, and the validation of Rule 1. | Systems 2 and 15 | Next System 2 revision. |
| Systems 3 and 9 must accept the innate item: ordering key anchor row `−1`, trigger kinds `Cooldown`, `Reaction`, `CombatStart`, and its place in System 9's firing rank. | Systems 3 and 9 | Before those GDDs are re-reviewed. |
| System 4 must add the hero instance IDs and the innate rows (anchor row `−1`, flag `innate`) to the placement baseline. | System 4 | Before System 4 is re-reviewed. |
| System 5 must store the picks, `B_perm` and the instance IDs in the run snapshot; System 31 owns the picking screen. | Systems 5 and 31 | When those are designed. |
| System 12 and System 36 must take the combat baseline of Rule 8. | Systems 12 and 36 | Before those GDDs are re-reviewed. |
| Art: four heroes against three role families, how an innate item is drawn and where it is read, and the art bible's sentence that a run starts with one hero (the MVP rule is: pick three). | art-director | Art bible follow-up. |
| Content: the four heroes' innate items, signature items, starting sets and unlock sequences are still to be authored; the effects System 3 allows (damage, apply status, detonate) bound what an innate item can do. | game-designer, systems-designer | Content pass before the first catalog fixture. |
| Balance: with full restoration, HP is not a resource across fights; confirm that resource pressure comes from items, doors and the grid. | systems-designer, System 35 | First balance simulation. |
| Pick order decides ties (a small lever) and the target order of area effects; confirm that this is acceptable. | systems-designer | First playtests. |
| System 16's validator reads the hero pool (base HP for the burst bound, speeds for the lint over every three-hero subset); a change to the roster or to a hero's base HP or speed must re-run it. | Systems 15 and 16 | Hero roster change. |
| Tier 2: recruiting adds a hero in the next slot; more than three heroes needs an art-bible change. | Producer, art-director | Tier 2 planning. |
