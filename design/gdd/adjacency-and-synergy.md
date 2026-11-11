# Adjacency and Synergy

> **Status**: Designed (pending independent /design-review)
> **Creative Director Review (CD-GDD-ALIGN)**: NOT ASSESSED 2026-11-12 — not run: specialist and director agents are spawned only when the user asks, and no request was made this session
> **Author**: user + Claude design session
> **Last Updated**: 2026-11-12
> **Last Verified**: 2026-11-12
> **Implements Pillar**: Arrangement Is the Thinking (primary); The Payoff Is the Product (cause must be readable)

## Summary

Adjacency and Synergy decides which items in a hero's backpack touch, and what an item receives from the neighbours that favour it: extra flat or percent damage, shorter cooldowns or more status. It is a pure, read-only view of the arrangement, shown while the player drags and frozen when combat starts, and every bonus names its source. It exists so that where an item sits is part of what it does, and so that the payoff can be traced back to the arrangement (Pillars 4 and 2).

> **Quick reference** — Layer: `Core` · Priority: `MVP` · Key deps: `Grid Backpack, Game Data Definitions`

## Overview

System 10 decides which items in a hero's grid are adjacent, which authored synergies are therefore active, and what each item receives from them. Two placed items are adjacent when a cell of one shares an edge with a cell of the other (four directions, no diagonals), inside the same hero grid. A synergy is authored on the source item: it names which neighbours receive it, chosen by tag, and what they receive. It flows one way, from source to target. The result is a read-only synergy view: for each target item, an ordered list of modifiers, each carrying its source item, the rule that produced it and its kind (flat damage, percent damage, cooldown reduction or status amount). The view is a pure function of the arrangement: it draws no RNG and reads no clock. Before combat it is recomputed as the player drags, so the player sees the consequence of a placement before committing. When System 12 freezes the arrangement at encounter start, the view for that arrangement is frozen with it and does not change during the fight. System 3 reads the damage modifiers, System 9 the cooldown modifiers, and Systems 25 and 26 show the links and their sources. Without this system a placement would mean only "does it fit"; with it, where an item sits is part of what it does, which is the thinking the game asks of the player (Pillar 4), and every bonus names its source so the payoff can be traced to the arrangement (Pillar 2).

> *Specialists not consulted for this session (not requested). Review manually before production.*

## Player Fantasy

The player holds a sword and sees that a whetstone one cell to its left would sharpen it. Before they let go, a bridge lights across the shared seam, the number on the sword changes and the whetstone is named as the source. Each placement is a small bet that can be read at a glance, and that is the feeling to build: **I am tuning a machine whose parts reinforce each other, and I can see why.** This serves the Core Fantasy (the architect of a perfect machine), Expression (the same items laid out two ways give two different builds) and Discovery (finding that two items the player would never have paired belong side by side).

In combat the same bridges fire and the player recognises them: "that is the whetstone doing that". Nothing about adjacency surprises the player in the fight, because all of it was visible while arranging.

*Pillar anchors.* Pillar 4, Arrangement Is the Thinking: "Debating an effect shared by all heroes vs one whose value depends on hero order or position, we choose the order- or position-dependent one." Position inside the grid is exactly what this system makes matter. Pillar 2, The Payoff Is the Product: "When a bigger effect and a clearer cause conflict, the clearer cause wins." Every bonus therefore names its source, and nothing is added whose author cannot be shown.

*References.* Backpack Battles and Backpack Hero make neighbouring items matter; this game keeps that readable pleasure and adds that the bonus is recorded with its source so the post-combat breakdown can point back at the arrangement.

> *`creative-director` not consulted (specialists not requested). Review manually before production.*

## Detailed Design

### Core Rules

1. **Scope.** The view is computed for each hero grid on its own. Only items placed in a hero grid take part. Items in the stash or the reward tray neither give nor receive (System 8, Rules 3 and 9). Adjacency never crosses grids; the relation between heroes belongs to turn order (System 11).
2. **Adjacent.** Two different items `A` and `B` in one grid are adjacent when some cell of `A` and some cell of `B` are at Manhattan distance 1 (they share an edge). Diagonal contact does not count. Items never overlap (System 8), so distance 0 does not occur. The number of shared edges does not change any amount.
3. **Synergy rules.** An item definition (System 2) may carry an ordered list of synergy rules, at most `MaxRulesPerItem` (provisional 3). A rule has `targetTags` (a list of tag IDs, possibly empty), a `kind` and an `amount`:
   - `DamageFlat`: adds `amount` to the flat bucket `F` of every Damage effect of the target (detonation damage included), range `−100,000..100,000` (System 3).
   - `DamagePercent`: adds `amount` permille to the percent bucket `P` of every Damage effect of the target, range `−1000..100,000` (System 3).
   - `CooldownReduction`: reduces the target's cooldown by `amount` turns (signed; negative lengthens it), provisional range `−10..10`. System 9 owns how the total is applied and the floor.
   - `StatusAmount`: adds `amount` to the quantity applied by each `ApplyStatus` effect of the target whose status ID equals the rule's `statusId`. System 13 owns the units and range.
4. **Who receives.** A neighbour `T` of the source `S` is a target of rule `r` when `T` has at least one tag in `r.targetTags`, or `r.targetTags` is empty (every neighbour). `S` never targets itself.
5. **Applicable, or no link.** A rule makes a link only when it can change something on the target: `DamageFlat` and `DamagePercent` need at least one Damage effect on the target; `CooldownReduction` needs a cooldown trigger; `StatusAmount` needs an `ApplyStatus` effect with the rule's status. Otherwise no link, no modifier and no bridge exist, so the player is never shown a cause that does nothing.
6. **Passive and unconditional.** A rule is active whenever the link exists. It does not depend on whether the source fires, on cooldowns or on combat state. Modifiers do not chain: the view is computed from the arrangement and static data only, and no modifier changes another rule.
7. **Once per source, rule and target.** The triple (source, rule index, target) produces at most one modifier, however many edges the two items share.
8. **Stacking.** All active modifiers apply. Each consumer sums them by kind (System 3 sums `F` and `P`, and clamps `P`).
9. **Order.** Targets are listed by their `readingIndex` (System 8). The modifiers of one target are listed by `(source anchorRow, source anchorCol, rule index)`, the key System 3 expects.
10. **Seam.** Each link carries one **seam**, the pair of cells `(s, t)`, `s` in the source and `t` in the target at distance 1, that is smallest by `(s.y, s.x, t.y, t.x)`. System 25 draws the bridge there.
11. **Operations.** `Evaluate(grid view)` returns the full synergy view of one hero grid. `PreviewPlacement(item, container, origin, orientation, buffers)` returns the view as it would be with that placement committed (an item already placed counts as moved), plus the links gained and lost. Both are pure, draw no RNG, read no clock and do not mutate System 8; the preview allocates nothing (the caller supplies buffers; too small returns `BufferTooSmall`). While System 8 is Frozen the preview returns `Frozen`. Result codes mirror System 8 and are asserted by code, not by text.
12. **Freeze.** When System 12 freezes the arrangement, it takes the view of each hero grid as an immutable value that stays unchanged until Thaw. Evaluating the same frozen arrangement again must give an equal view.
13. **Direction unused.** Only occupied cells matter, so the orientation of a symmetric item is stored by System 8 but not used here (answers System 8's open question).
14. **Engine-free.** The same engine-free rules as System 3 apply: integer arithmetic, no `UnityEngine`, no mutable static state.

### States and Transitions

| Subject | State | Trigger | Result |
|---|---|---|---|
| Synergy view of a hero grid | Live | Any System 8 change (place, move or remove) | Recomputed by the next `Evaluate`; nothing is kept between calls. |
| Synergy view | Live | System 12 freezes | A value copy is held as the frozen view. |
| Synergy view | Frozen | Arrangement Frozen | Unchanged; preview returns `Frozen`. |
| Synergy view | Frozen | System 12 thaws | Frozen copy is discarded; Live again. |

### Interactions with Other Systems

| System | Data in | Data out | Interface owner |
|---|---|---|---|
| 2 Game Data Definitions | Item tags, synergy rules, effect kinds present on each item | None | System 2 owns the fields and their validation (provisional: see Dependencies). |
| 8 Grid Backpack | Arrangement view: hero grids, items in reading order, occupied cells, anchor, `readingIndex`; Frozen state | None | System 8 owns geometry; System 10 owns meaning. |
| 3 Effect Resolution | None | For each target item, ordered `DamageFlat` and `DamagePercent` modifiers with source item, rule index and origin | System 3 owns the sums and the damage formula. |
| 9 Item Trigger and Cooldown | None | Cooldown modifiers and status-amount modifiers per target | System 9 owns firing and the cooldown floor. |
| 12, 36 | Freeze and Thaw | Frozen view per hero grid | System 12 owns the calls. |
| 25, 26 | Preview request | Links with seams and sources, preview delta, per-target totals | System 25 owns the bridge drawing; System 26 the explanation text. |
| 4 Event Log | None | Modifiers actually applied are recorded by System 3 on the origin | System 4 owns the schema. |
| 18, 35 | Evaluate calls | Frozen or live view | Those systems decide when. |

*Provisional:* the cooldown and status-amount kinds depend on Systems 9 and 3/13, which have no GDD or no matching contract yet.

> *Specialists (`systems-designer`, `game-designer`, `ux-designer`) not consulted (not requested). Review manually before production.*

## Formulas

All arithmetic is on small integers. Nothing here uses floating point, RNG or time. Damage itself is computed by System 3's `effect_damage_formula`; this system only supplies the flat bucket `F` and the percent bucket `P` that formula reads.

**Formula 1: adjacency.** `adj(A, B) = A ≠ B ∧ ∃ a ∈ cells(A), b ∈ cells(B): |a.x − b.x| + |a.y − b.y| = 1`, both items in the same hero grid.

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| `cells(i)` | set of `(x, y)` | 1 to 9 cells, inside the grid bound | System 8 arrangement view | Occupied cells of item `i` (System 8, Formula 2). |
| `a`, `b` | `(x, y)` | `0 ≤ x, y < 6` (hero grid) | calculated | A cell of `A` and a cell of `B`. |

Output: a boolean. *Example:* an item on `(2,1), (3,1)` and an item on `(1,1)` are adjacent, because `(1,1)` and `(2,1)` differ by 1 in `x` only. An item on `(1,2)` is not adjacent to the first one: `(1,2)` and `(2,1)` differ by 1 in both axes (distance 2).

**Formula 2: link.** `link(S, r, T) = adj(S, T) ∧ tagmatch(r, T) ∧ applicable(r, T)`, where `tagmatch(r, T) = (r.targetTags = ∅) ∨ (tags(T) ∩ r.targetTags ≠ ∅)` and `applicable` is Rule 5. The link set `L` of a grid is `{ (S, i, T) : link(S, rule i of S, T) }`.

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| `S`, `T` | item instance | placed in the grid | System 8 | Source and target. `S ≠ T`. |
| `r`, `i` | rule, index | `0 ≤ i < MaxRulesPerItem` | item definition (System 2) | A synergy rule of `S` and its index in the authored list. |
| `r.targetTags` | set of tag IDs | 0 to a data-defined number of tags | item definition (System 2) | Empty means every neighbour. |
| `tags(T)` | set of tag IDs | data-defined | item definition (System 2) | Tags of the target. |

Output: `L` has at most `E_o = 2 × 60 = 120` ordered (source, target) pairs in a 6×6 grid (a 6×6 grid has `2 × 6 × 5 = 60` internal edges, so at most 60 adjacent item pairs), and at most `120 × MaxRulesPerItem = 360` links with the provisional `MaxRulesPerItem = 3`. That is the buffer size a caller needs for one hero grid.

**Formula 3: per-target totals.** For a target `T`, `F_T = Σ amount over active DamageFlat links into T`, `P_T = Σ amount over active DamagePercent links into T`, `C_T = Σ amount over active CooldownReduction links into T`, and for each status `s`, `A_T(s) = Σ amount over active StatusAmount links into T with statusId s`. The sums are not clamped here.

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| `amount` | int | by kind: flat `−100,000..100,000`, percent `−1000..100,000`, cooldown `−10..10` (provisional), status per System 13 | item definition (System 2), validated, never `0` | Authored amount of a rule. |
| `F_T`, `P_T` | int | at most `20 × 3 = 60` links per target, so `|F_T|` and `|P_T|` are at most `6,000,000` | calculated | Inputs to System 3 (`F` and `P`). System 3 clamps the summed `P` at run time (`−1000` at least). |
| `C_T` | int | `|C_T| ≤ 600` | calculated | Input to System 9, which applies its own floor. |

The bound `20` is the most items that can touch one item: a footprint inside a 3×3 box has at most 12 edge-neighbour cells outside the box and at most `9 − |F|` inside it. The sums stay far inside signed 32-bit, so no overflow path exists. *Example:* a sword receives flat `+3` from a whetstone and `+250‰` from a rune: `F_T = 3`, `P_T = 250`. With a base damage `B = 10` System 3 gives `Raw = 13`, `Scaled = (13 × 1250 + 500) / 1000 = 16`, so `r = 16`.

**Formula 4: seam.** `seam(S, T) = argmin over { (s, t) : s ∈ cells(S), t ∈ cells(T), dist(s, t) = 1 } of (s.y, s.x, t.y, t.x)`, compared in that order. *Example:* a whetstone on `(1,1)` and a sword on `(2,1), (3,1)` have the one pair at distance 1, so the seam is `((1,1), (2,1))`.

**Formula 5: preview delta.** For a candidate placement, `L'` is the link set of the grid with the placement applied (the item moved if already placed). `gained = L' \ L` and `lost = L \ L'`, comparing links by (source instance, rule index, target instance). *Example:* moving the whetstone from `(1,1)` to `(1,3)` removes its contact with the sword, so `lost = { (whetstone, 0, sword) }` and `gained = ∅`; moving it to `(3,0)` keeps contact (it sits above `(3,1)`), so nothing is lost or gained but the seam becomes `((3,0), (3,1))`. Both target cells lie in the 4×4 start block, so System 8 accepts them (the worked example below uses the same arrangement).

**Worked example (whole grid).** Sword on `(2,1), (3,1)` (tag `weapon`, has a Damage effect). Whetstone `W` on `(1,1)`, rule 0: `DamageFlat +3`, `targetTags = {weapon}`. Rune `R` on `(2,2)`, rule 0: `DamagePercent +250`, `targetTags = ∅`. Shield item `Sh` on `(1,2)`, tag `armor`, no Damage effect. `W` touches the sword and `Sh`; `R` touches the sword (below `(2,1)`) and `Sh`. `W` to `Sh`: tag `armor` is not `weapon`, so no link. `R` to `Sh`: `DamagePercent` needs a Damage effect on the target, so no link. The sword receives two modifiers, in order `(1,1,0)` from `W` (anchor row 1, column 1) then `(2,2,0)` from `R`. Totals `F_T = 3`, `P_T = 250`.

> *`systems-designer` not consulted (not requested). Review manually before production.*

## Edge Cases

- **If an item has no neighbours**: it is listed in the view with no modifiers. Nothing is shown for it.
- **If a neighbour has a matching tag but nothing the rule can change** (a shield item under `DamageFlat`): no link, no modifier and no bridge (Rule 5).
- **If two items give each other bonuses**: both links exist and apply independently. There is no loop, because modifiers never chain (Rule 6).
- **If the source and target share several edges**: one modifier per rule (Rule 7); the seam is chosen by Formula 4.
- **If one source has two rules that both reach the same target**: two modifiers, ordered by rule index.
- **If contact is diagonal only**: not adjacent.
- **If the cell between two items is empty or Locked**: not adjacent. Adjacency exists only between items that touch.
- **If two items in different grids sit at the same coordinates**: never adjacent (Rule 1).
- **If an item is moved to the stash or the tray**: it leaves the view, and every link it gave or received disappears. The preview reports them in `lost`.
- **If the summed percent bucket is below `−1000`** (for example `−1500`): the view reports the raw sum. System 3 clamps at run time, so the view is valid and the damage factor is never negative.
- **If total cooldown reduction exceeds the target's cooldown**: the view reports the sum; System 9 applies its floor.
- **If a status-amount total makes a quantity zero or negative**: the view reports the sum; Systems 13 and 3 treat a non-positive contribution as no application.
- **If a hero is downed during combat**: the frozen view does not change; items of a downed hero stop firing under Systems 9 and 36.
- **If a change is attempted while the arrangement is Frozen**: System 8 rejects it (`Frozen`); the preview returns `Frozen` and the frozen view is untouched.
- **If a preview placement is not valid** (`Occupied`, `LockedCell`, `OutOfBounds` and so on): the preview returns System 8's code with no view or delta; the invalid cue belongs to Systems 8 and 25.
- **If the preview buffers are too small**: `BufferTooSmall`, with no partial output.
- **If the same arrangement is reached by different histories**: the view is identical, because it depends on the arrangement only.
- **If a catalog has a rule with `amount = 0`, more than `MaxRulesPerItem` rules, an unknown tag or status ID, or a `StatusAmount` rule without a `statusId`**: System 2 rejects the catalog. This system never repairs data.
- **If a rule has an empty `targetTags`**: every neighbour is a candidate. This is deliberate and permitted; Rule 5 still limits it to targets the rule can change.
- **If `rotationEnabled` is off**: orientation is always 0 and adjacency is computed from those cells; nothing else changes.
- **If a cell is unlocked or a reward creates an item in the tray**: the view is unchanged until an item is placed.
- **If a hero's grid is empty at encounter start**: the frozen view for that hero is empty and valid.

> *`systems-designer` not consulted (not requested). Review manually before production.*

## Dependencies

| System | Direction | Hard/Soft | Contract | Back-reference status |
|---|---|---|---|---|
| 2 Game Data Definitions | Upstream | Hard | Item tags, effect kinds per item, synergy rules (`targetTags`, `kind`, `amount`, `statusId`), `MaxRulesPerItem` and the amount ranges. Provisional: System 2 has no synergy fields yet. | A row is added to System 2's Open Questions at sync. |
| 8 Grid Backpack | Upstream | Hard | Arrangement view (grids, items in reading order, occupied cells, anchor, `readingIndex`) and the Frozen state. System 8 already lists this system as downstream and requires the preview query. | Present (System 8, Dependencies). |
| 3 Effect Resolution | Downstream | Hard | Ordered `DamageFlat` and `DamagePercent` modifiers with source, rule index and origin. | System 3 said "No GDD yet"; updated at sync. |
| 9 Item Trigger and Cooldown | Downstream | Hard (provisional) | Cooldown and status-amount modifiers per target. System 9 has no GDD yet. | None yet; System 9 must cite this when designed. |
| 12 Combat Resolution | Downstream | Hard | Takes the frozen view at Freeze and releases it at Thaw. | System 12 said "No GDD yet"; updated at sync. |
| 36 Combat State and Step Model | Downstream (via 12) | Soft | Receives the frozen arrangement and adjacency context at encounter start. | Wording updated at sync. |
| 25 Backpack UI, 26 Hover Popup | Downstream | Hard for 25, soft for 26 | Links with seams and sources, preview delta, per-target totals. | Not designed. |
| 13 Status Effects | Peer | Soft | Status IDs for `StatusAmount`; units and range owned by System 13. | System 13 does not yet mention synergy amounts. |
| 18 Commitment and Drop Model | Downstream | Soft (provisional) | May read the arrangement view as a signal of commitment. | Not designed. |
| 35 Balance Simulation Harness | Downstream | Soft | Calls `Evaluate`. | Not designed. |
| 4 Event Log | Indirect | n/a | Modifiers actually applied are recorded by System 3 on the origin; System 10 writes no events. | No change. |
| 11 Turn Order | None | n/a | Explicitly unrelated: adjacency is inside one grid. | No change. |

**Circular-dependency check:** System 10 never calls System 3 (the systems index's rule). Data flows only from System 10 to Systems 3, 9, 12, 25 and 26.

## Tuning Knobs

| Knob | Default | Safe range | Affects | If too high | If too low |
|---|---|---|---|---|---|
| `MaxRulesPerItem` | 3 (provisional) | 1 to 4 | How many distinct synergies one item may carry; the buffer size (Formula 2); tooltip length. | Items become hard to read and a single placement shifts many numbers; the link buffer grows (`120 × n` on a 6×6 grid). | Items can express only one idea; adjacency loses depth (Expression). |
| Amount range of `DamageFlat` | `−100,000..100,000` | Owned by System 3 (`F`) | How large one flat bonus or penalty may be. | Not this system's knob: change it in System 3. | Not this system's knob. |
| Amount range of `DamagePercent` | `−1000..100,000` permille | Owned by System 3 (`P`) | How large one percent bonus may be. | Not this system's knob. | Not this system's knob. |
| Amount range of `CooldownReduction` | `−10..10` turns (provisional) | Owned by System 9 | How much one rule can speed up or slow a target. | Cooldowns collapse to the floor and the item fires every turn. | Cooldown synergies are too weak to notice. |
| Amount range of `StatusAmount` | Owned by System 13 | Owned by System 13 | How much one rule can add to an applied status. | Not this system's knob. | Not this system's knob. |
| Tag vocabulary | Owned by System 2 | Owned by System 2 | Which neighbours a rule can single out. | A large vocabulary makes rules unreadable. | A small one makes most rules "any neighbour". |

**Not knobs.** The adjacency rule itself (edge contact, distance 1, no diagonals, one hero grid) and the one-way direction are design rules; changing them is a change to the model, not a tuning pass. There is deliberately **no cap per target**: stacking is bounded by System 3's clamp of `P` at `−1000`, by `RawMax` and by `Cap`. If simulation (System 35) shows runaway stacks, the cap is added here as a new knob (Open Questions).

**Interactions.** The grid bound (System 8, at most 6×6 in the MVP) and `MaxRulesPerItem` together set the link buffer: `2 × (2 × W × (H − 1)) × MaxRulesPerItem` for a square bound `W = H`, which is 360 for 6×6 and 3 rules, and would be 504 for the deferred 7×7. Changing `MaxRulesPerItem` or the adjacency model bumps the rules version and regenerates fixtures, as System 8 does for its own rules.

> *`systems-designer` not consulted (not requested). Review manually before production.*

## Visual/Audio Requirements

- **Bridge at the seam.** A link is drawn as one short bridge across its seam (Formula 4), never covering a plate. This answers the art bible's open line ("whether adjacency synergies are shown as a bridge across the shared seam"): yes, because edge contact makes the seam natural.
- **No phantom bridges.** A bridge exists only for a link (Rule 5). Two items that touch without a link get none.
- **Three states, as the art bible defines them (3.7, 7.6).** Resting: Fog, 1 px, static, always in planning. Preview: Fog-hi, 2 px, no pulse, 100 ms in and 80 ms out, while a held item hovers a position and on focus of a plate. Trigger: tier highlight with a 1 to 2 frame pulse in combat. The states differ by width and brightness, not by hue alone.
- **Trigger is driven by the trace, not by adjacency.** A bridge pulses only when the target fires with the source's modifier recorded on that effect's origin (System 3 records the modifiers actually applied). Adjacency alone never pulses.
- **Reduced motion.** Preview and trigger follow the art bible's reduced-motion rule: no pulse, a static highlight instead.
- **Lit budget.** Resting bridges are not counted as lit (Fog luminance 0.2990 against a threshold of 0.30, art bible). Preview and trigger bridges are lit and fall under the 12% budget; how many may pulse at once is the art-director's decision (Open Questions).
- **Audio.** This system makes no sound. It supplies the count of links gained by a drop so that Systems 29 and 32 may choose a quiet cue (none while only previewing).

> 📌 **Asset Spec** — Visual/Audio requirements are defined. After the art bible is approved, run `/asset-spec system:adjacency-and-synergy` to produce per-asset visual descriptions, dimensions and generation prompts from this section.

> *`art-director` not consulted (not requested). Review manually before production.*

## Game Feel

- **Immediate.** The view is recomputed whenever the held item's snapped origin or orientation changes, in the same frame, never every frame. It must not allocate (caller buffers).
- **Presentation timing from the art bible.** Bridge preview 100 ms in, 80 ms out; on drop the preview holds 400 ms then fades over 200 ms.
- **Loss is visible.** A link lost by moving an item fades over 80 ms, so the player sees what the move gave up as well as what it gained.
- **Quiet when small, clear when big (Pillar 2).** A single `+3` is a thin bridge and a small number change; nothing is added to make a minor link look important.
- **Same recognition in combat.** The bridge the player saw while arranging is the bridge that pulses, at the step the target fires.

## UI Requirements

- **While dragging:** the target's stat chips show the new totals, the links gained and lost are visible, and a text line names the cause and amount, for example "+3 damage from Whetstone" (not colour alone; consistent with System 8's status line).
- **Popup (System 26):** a "receives from" and a "gives to" row listing the item, the amount and the rule, in the order of Rule 9, and the rule's `targetTags` as the "Works with" row of keyword glyph chips (art bible 7.9).
- **Focus:** focusing a plate lifts its partners' bridges to the preview state. Everything about links is reachable by keyboard and gamepad focus, not only by hover.
- **Post-combat breakdown (System 30):** a row can name the source of a modifier because System 3 records it.
- **Overview toggle (art bible 7.6):** the grid lens raises all bridges to the preview state.

> 📌 **UX Flag — Adjacency and Synergy**: This system has UI requirements. In Phase 4 (Pre-Production), run `/ux-design` for the arrangement screen (together with System 8) before writing epics. Stories that reference UI should cite `design/ux/[screen].md`, not the GDD directly.

## Cross-References

| Reference | What it provides here |
|---|---|
| [Game Concept](game-concept.md), Core Mechanics 1 and MVP Definition | "Adjacent items modify each other" and "3-4 synergy types". |
| [Systems Index](systems-index.md), row 10 and Circular Dependencies | Dependencies 8 and 2; "10 never calls 3". |
| [Grid Backpack](grid-backpack.md), Rules 6 and 17, Dependencies row 10 | Anchor, `readingIndex`, arrangement view, the required preview query. This GDD answers its open question: orientation is not used. |
| [Effect Resolution Model](effect-resolution-model.md), Canonical Order Contract and damage formula | Modifier order key; `F` and `P` ranges; modifiers immutable for the encounter. |
| [Combat Resolution](combat-resolution.md), Prepare and Start; [Combat State](combat-state-and-step-model.md); [Event Log](event-log.md) | The adjacency preview before combat, the frozen context at start, and where applied modifiers are recorded. |
| [Art Bible](../art/art-bible.md), 3.7, 7.6, 7.9 and the open line about adjacency | Bridge form and states; the popup "Works with" row. Art bible 7.6 measure 4 (the "affects" row for cross-hero effects) concerns item effects on another hero, not adjacency. |
| Prototype `Assets/_Project/Scripts/Backpack/SynergyEvaluator.cs` (evidence only) | Used directional star cells, category match and static state; none of it is adopted. |

## Acceptance Criteria

**Fixtures.**
- `GRID-4`: hero grid with a 4×4 open block, rotation off (System 8 `PROFILE-C`). `GRID-6`: a 6×6 hero grid with all 36 cells open (System 8 `PROFILE-A` fully unlocked).
- `SWORD`: footprint `{(0,0),(1,0)}`, tag `weapon`, cooldown trigger 3, one Damage effect `B = 10`. `WHETSTONE`: 1×1, rule 0 `DamageFlat +3`, `targetTags {weapon}`. `RUNE`: 1×1, rule 0 `DamagePercent +250`, `targetTags ∅`. `SHIELD`: 1×1, tag `armor`, a Shield effect only. `HASTE`: 1×1, rule 0 `CooldownReduction 1`, `{weapon}`. `STING`: 1×1, rule 0 `StatusAmount +1`, status `an`, `{weapon}`. `ANBLADE`: 1×1, tag `weapon`, cooldown trigger 2, one `ApplyStatus(an)` effect.
- `ARR-W` on `GRID-4`: `SWORD` at `(2,1)`, `WHETSTONE` at `(1,1)`, `RUNE` at `(2,2)`, `SHIELD` at `(1,2)`.

Logic tests live under `Assets/Tests/EditMode/AdjacencyAndSynergy/`.

1. **Scope (Rule 1).** **Given** `WHETSTONE` in the stash at `(1,1)` and `SWORD` in a hero grid at `(2,1)`, and a second hero grid with a `SWORD` at the same cells as the first, **when** the views are evaluated, **then** no link exists between grids or from the stash or tray, and each grid's view contains only its own items.
2. **Adjacency (Rule 2, Formula 1).** **Given** `ARR-W`, **when** adjacency is evaluated, **then** `SWORD`–`WHETSTONE` and `SWORD`–`RUNE` are adjacent, `WHETSTONE`–`RUNE` (diagonal) is not, and `WHETSTONE`–`SHIELD` is adjacent.
3. **Kinds (Rule 3).** **Given** one source per kind adjacent to a target that can use it, **when** evaluated, **then** each produces a modifier with the correct kind, amount, source instance and rule index.
4. **Targets (Rule 4).** **Given** a rule with `targetTags {weapon}`, **when** the neighbours are `SWORD` and `SHIELD`, **then** only `SWORD` receives it. **Given** an empty `targetTags`, **then** every applicable neighbour receives it, and a source never targets itself.
5. **Applicable (Rule 5).** **Given** `RUNE` next to `SHIELD`, **when** evaluated, **then** no link, modifier or seam exists for that pair. The same holds for `HASTE` next to an item without a cooldown trigger and for `STING` next to an item without `ApplyStatus(an)`.
6. **Passive and no chain (Rule 6).** **Given** `ARR-W` where `SWORD` never fires, **then** its modifiers are unchanged. **Given** A gives to B and B gives to C, **then** C's modifiers do not depend on A.
7. **Once per triple (Rule 7).** **Given** a source whose footprint shares three edges with a target, **then** exactly one modifier per rule is produced.
8. **Stacking and totals (Rule 8, Formula 3).** **Given** `ARR-W`, **then** `SWORD` has `F = 3` and `P = 250`, and System 3's formula with `B = 10` gives `r = 16`.
9. **Order (Rule 9).** **Given** `ARR-W`, **then** `SWORD`'s modifiers are listed `(1,1,0)` from `WHETSTONE` then `(2,2,0)` from `RUNE`, and targets are listed by `readingIndex`. Changing the order in which the items were placed does not change the lists.
10. **Seam (Rule 10, Formula 4).** **Given** `ARR-W`, **then** the `WHETSTONE`→`SWORD` seam is `((1,1),(2,1))` and the `RUNE`→`SWORD` seam is `((2,2),(2,1))`. **Given** two items sharing several edges, **then** the seam is the pair smallest by `(s.y, s.x, t.y, t.x)`.
11. **Purity (Rule 11).** **Given** any arrangement, **when** `Evaluate` and `PreviewPlacement` run, **then** System 8's `Snapshot()` text is identical before and after, and no RNG draw and no clock read occurs (a counting probe stays at zero).
12. **Preview codes (Rule 11).** **Given** a Frozen arrangement, **then** the preview returns `Frozen`. **Given** an `Occupied` or `LockedCell` candidate, **then** it returns System 8's code with no view. **Given** buffers one entry too small, **then** it returns `BufferTooSmall` with no partial output.
13. **Allocation (Rule 11).** **Given** a warmed-up preview, **when** called 1,000 times with moving origins, **then** managed allocation is zero, measured with System 8's method, including a control that does allocate.
14. **Freeze (Rule 12).** **Given** a frozen view, **when** the same frozen arrangement is evaluated again, **then** the views are equal. **When** System 12 thaws and an item is moved, **then** the live view changes and the old frozen copy is discarded.
15. **Direction unused (Rule 13).** **Given** a 2×2 item rotated through all four orientations, **then** the view is identical in each.
16. **Engine-free (Rule 14).** **Given** the assembly definition and a source scan, **then** the code references no `UnityEngine` type, has no mutable `static` field, and calls no RNG or clock.
17. **Buffer bound (Formula 2).** **Given** `GRID-6` fully packed with 1×1 items each carrying three applicable empty-tag rules, **then** the link count is exactly 360, a 360-entry buffer suffices and a 359-entry buffer returns `BufferTooSmall`.
18. **Extremes (Formula 3).** **Given** 60 modifiers of `+100,000`, **then** the sum is `6,000,000` with no overflow. **Given** a summed `P = −1500`, **then** the view reports `−1500` and System 3 gives a factor of `0` (`r = 0`).
19. **Delta (Formula 5).** **Given** `ARR-W`, **when** `WHETSTONE` is previewed at `(1,3)`, **then** `lost = {(WHETSTONE, 0, SWORD)}` and `gained = ∅`. **At** `(3,0)`, nothing is lost or gained and the seam is `((3,0),(3,1))`.
20. **Determinism.** **Given** the same arrangement reached by two different move histories, **then** the views are equal. **Given** System 8's state walker (`WALK`, at most 25 operations), **then** for every reached state `Evaluate(state)` equals `Evaluate(Restore(Snapshot(state)))`.
21. **Leaving a grid (edge case).** **Given** `ARR-W`, **when** `WHETSTONE` moves to the stash, **then** the preview reports its link as lost and, after the move, the view has no modifier from it.
22. **Empty cases.** **Given** an empty grid or an item with no neighbours, **then** the view is valid and has no modifiers.
23. **Data validation (System 2).** **Given** a rule with `amount = 0`, more than `MaxRulesPerItem` rules, an unknown tag or status ID, or `StatusAmount` without `statusId`, **then** the catalog is rejected naming the record and field.
24. **Integration with System 3 (provisional).** **Given** `ARR-W` frozen and `SWORD` firing, **then** the origin of its damage effect records the two modifiers in this order with source and rule index. *Blocked until Systems 3 and 9 are implemented.*
25. **Integration with Systems 9 and 13 (provisional).** **Given** `HASTE` and `STING` adjacent to `ANBLADE`, **then** System 9 applies the cooldown total with its floor and System 13 receives the status-amount total. *Blocked until those systems exist.*
26. **Integration with System 12 (provisional).** **Given** an encounter start, **then** the frozen view equals the view of the frozen arrangement, and no edit during the fight can change it.
27. **UI evidence.** **Given** the arrangement screen, **then** retained screenshots in `production/qa/evidence/` show the three bridge states, a link lost on a move, and the text line "+3 damage from Whetstone" with the target's changed chip.
28. **Performance.** **Given** a worst-case `GRID-6` (Formula 2), **then** the times of `Evaluate` and of one `PreviewPlacement` are measured on the development machine and recorded. The provisional target is under 0.2 ms each, to be confirmed in the combat spike.

> *`qa-lead` not consulted (not requested). Review manually before production.*

## Open Questions

| Question | Owner | Resolve by |
|---|---|---|
| Where do synergy rules, the tag vocabulary and `MaxRulesPerItem` live in System 2's records, and who validates "applicable" (it needs each item's effect kinds)? | System 2, System 10 | Next System 2 revision. |
| How does System 9 apply the cooldown total (floor, rounding, from the first firing or later) and does it accept a negative reduction (lengthening)? | System 9 | When System 9 is designed. |
| Does `StatusAmount` become a modifier kind in System 3's contract, who owns its units and range, and how is a non-positive result handled? System 3 today carries only `F` and `P`. | Systems 3 and 13 | Before System 3 is re-reviewed. |
| Is the bridge directional (a chevron toward the target) and how are two mutual links at one seam drawn? How many bridges may pulse at once under the lit budget? | art-director | Art bible follow-up (the open line about adjacency, 3.7 and 7.6). |
| Does an empty `targetTags` need an explicit "any neighbour" flag to avoid accidental global rules? | System 2 | Next System 2 revision. |
| Is a per-target cap needed? Add it as a knob if simulation shows runaway stacking. | System 35, systems-designer | First balance simulation. |
| Does System 18 read the synergy view as a signal of commitment? | System 18 | When System 18 is designed. |
| Is 0.2 ms for `Evaluate` and `PreviewPlacement` realistic on the target machine? | Technical director | Combat spike. |
| Tier 2: a 7×7 grid raises the buffer to 504, and synergy between neighbouring heroes in party order is deferred; revisit both together. | Systems designer | Tier 2 design. |
