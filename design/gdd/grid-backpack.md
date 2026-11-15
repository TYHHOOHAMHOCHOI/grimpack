# Grid Backpack

> **Status**: In Revision (independent `/design-review` verdict NEEDS REVISION on 2026-11-11; revision applied, re-review pending)
> **Specialist Review (2026-11-11)**: creative-director (CD-GDD-ALIGN: CONCERNS), systems-designer, qa-lead (CONCERNS), art-director, unity-specialist, then an independent `/design-review` (NEEDS REVISION). Findings were addressed in the 2026-11-11 revisions; CD-GDD-ALIGN must be re-run after the next independent re-review.
> **Author**: User with Claude Code studio design session
> **Last Updated**: 2026-11-11
> **Last Verified**: 2026-11-11
> **Implements Pillar**: Arrangement Is the Thinking.

## Summary

The Grid Backpack decides where every item lives: the three hero grids, the shared 5×5 stash and the reward tray where new items wait, plus the rules for placing, rotating, removing and losing them. It lets the player build their machine by arranging items before a fight, and gives combat a frozen, deterministic arrangement to read. Without it there is no arrangement to think about (Pillar 4), and no order for items to fire in.

> **Quick reference** — Layer: `Core` · Priority: `MVP` · Key deps: `Game Data Definitions (and an injected item-ID source from Run State)`

## Overview

System 8 owns *where things are*: the three hero backpack grids and the shared stash, and every rule for placing, moving, rotating and removing an item instance in them. Each hero has a grid that starts at 4×4 and can grow up to 6×6 during a run. The stash holds spare items that no hero can use in combat. New items arrive in a reward tray and must be stored before the next fight or they are lost after the player confirms. An item occupies the cells of its authored footprint (System 2) in one of its allowed orientations, and no two items share a cell. The player arranges everything before a fight; at fight start the arrangement is frozen, and System 8 exposes it as a read-only, deterministic view: which cells each item instance occupies, its anchor cell and orientation, and its reading-order position. Systems 3, 4, 9, 10 and 25 read that view. Arrangement is where the player's skill lives (Pillar 4): the same items laid out differently, or split differently across the three heroes, make a different machine.

For the player, this is the "arrange the machine" half of the game: dragging items into a tray of cells, turning them to fit, and seeing at once what fits, what is blocked and which hero carries what. The model manages all three hero grids; the screen shows one hero grid at a time with the stash and a party overview, as the art bible fixes (section 7.6). System 8 defines geometry and ownership only. What adjacency means for an effect belongs to System 10, when an item fires to System 9, how the grid looks and feels to System 25, and the static shapes to System 2. The prototype's `BackpackGrid` and `ItemShape` are migration evidence, not adopted rules.

## Player Fantasy

The grid is a circuit the player learns to read. They lift an item and, before they let go, the board answers: which neighbours it feeds, which feed it, where it will fire in the order of the turn. Dropping a hero's second sword beside the poison dagger is not filing an inventory; it is soldering a link, and the thrill is spotting a connection nobody pointed out. The player thinks in cells and neighbours, and the board never lies about what a placement does.

The circuit spans the whole party. Moving the poison dagger from the Warrior to the Support moves the setup earlier in the turn, so the same item can be a different link in a different hero's grid. The three grids are three stages of one machine, and reading the machine means reading across them.

The fantasy pays off later, in the watch beat: when a chain goes off, the player can point at the cells that made it, because they are the one who laid them down. It also holds on a loss. A broken link is visible on the grid, so "I misplaced that" is a lesson, not a mystery. This serves Pillar 4 (all skill happens before combat, and everything needed to decide is visible then) and Pillar 2's rule that the clearer cause wins over the bigger effect. The tone is calm and deliberate, the art bible's "cut stones laid on dark felt": rewarding to study for 30 to 60 minutes, never frantic.

*Note:* the live preview of links while dragging depends on System 10 providing a pure adjacency-preview query, and the "order of the turn" depends on Systems 9 and 11 providing a party-wide firing position (Dependencies). System 8 supplies the position inside one grid; the party-wide position is composed by Systems 9 and 11 and shown by System 25.

## Detailed Design

### Core Rules

1. **Containers.** Every item instance is in exactly one of three places: a **hero grid** (one per hero), the **shared stash** (one per run), or the **reward tray** (items waiting to be stored). Only items in a hero grid take part in combat.
2. **Hero grid geometry.** A hero's backpack profile (System 2) declares a bound (at most 6×6), a **start block** (MVP: the top-left 4×4 block, which must lie inside the bound) and an ordered **unlock sequence**. Every cell of the sequence lies inside the bound and outside the start block, no cell repeats, and the sequence is no longer than the area of the bound minus the area of the start block. Each cell is Locked or Open. `Unlock(n)` opens the next `min(n, remaining)` cells of the sequence and returns the number actually opened (0 when the sequence is exhausted); `n < 0` is a `ContractViolation`. Opening is permanent for the run. Cells are addressed `(x, y)`, with `x` the column from the left and `y` the row from the top. *Deferred to Tier 2:* raising a bound to 7×7 and an extended sequence; the concept's "expandable" is served in the MVP by `Unlock`.
3. **Shared stash.** A 5×5 grid with all 25 cells open from the start (fixed for the MVP; the size is a tuning knob tied to the pixel budget). It follows the same placement rules as a hero grid. Items in the stash have no effect in combat and take no part in adjacency.
4. **Footprint and orientation.** An item's footprint (System 2) is a set of local cells, connected by shared edges, inside a 3×3 box, with 1 to 9 cells. Orientation `o` is a number of clockwise quarter turns, `0..3`; any other value is rejected with `InvalidOrientation`. One quarter turn maps `(x, y)` to `(−y, x)` (with `y` pointing down), then the cells are shifted so the smallest `x` and `y` are `0`. With `rotationEnabled` off in the rules data, only `o = 0` is allowed (`RotationDisabled` otherwise). The orientation the player chose is stored even when the footprint repeats under rotation.
5. **Placement.** A placement is a container, an `origin` `(ox, oy)` and an orientation. The occupied cells are the shifted, rotated footprint cells plus the origin. A placement is **valid** when the orientation is allowed and every occupied cell is inside the bound and Open. A valid placement is **free** when no occupied cell belongs to another item.
6. **Anchor cell and reading order.** The **anchor cell** of a placed item is the first occupied cell scanning row by row, left to right (smallest `y`, then smallest `x`). This is the definition System 3 uses, so a rotated item has exactly one anchor. The anchor cell is not the placement origin. Items in one grid are in reading order by `(anchorRow, anchorCol)`, and two items never share an anchor. Each placed item has a zero-based `readingIndex` in its grid.
7. **Drop.** Picking an item up does not change the model; only a committed drop does. A drop on a free placement moves the item there. A drop onto the stash is a placement in the stash under the same rules. A drop that is invalid, or a release outside every grid, changes nothing: an item from a container returns to its source, and an item from the tray stays in the tray. Moving an item to another hero's grid is done by switching hero while the item is held (the held plate stays in the hand; art bible 7.6) and dropping it there.
8. **Occupied cells.** A valid placement that overlaps any other item is rejected with `Occupied`. Nothing is displaced and nothing is swapped in the MVP. The player first moves the blocking item elsewhere, or uses `Remove` to put it in the tray. The blocking items are the **blocking set** (Formula 5), in reading order, and `Preview` returns it so that the UI can name them.
9. **The reward tray.** The tray is the set of items waiting to be stored. Each entry has a **tray-entry number**, a counter that never repeats and that fixes the order of loss. The tray has no cells and no physics; how it is presented is a UI matter (System 25). An item enters the tray when a reward creates it (Rule 10) or when the player uses `Remove` on an item in a container. An item leaves the tray by a `Place` into a container. Items in the tray take no part in combat, adjacency or the placement baseline.
10. **New items.** A reward creates a new item instance in the tray. The instance ID comes from an injected item-ID source (`IItemIdSource.Next()` returning a `long`) that System 5 implements with a run-scoped deterministic counter. `Create` validates first and calls `Next()` last, only when it will succeed, so a failed `Create` does not consume an ID. An ID is never reused.
11. **Loss.** When System 5 freezes the arrangement at the start of an encounter, every item still in the tray is destroyed, in tray-entry order, and System 8 returns the list of destroyed instance IDs. Grids and stash are not affected. The model never asks for confirmation. System 8 provides the tray list; System 5 returns `NeedsConfirm` and System 25 presents "Leave behind: X, Y?" before a confirmed Start when the tray is not empty. The UI blocks Start while an item is held (Dependencies, UI Requirements). Confirmation atomicity remains an open Systems 5/25 interface question.
12. **Freeze, Thaw and the order of a round.** The arrangement is **Editable** or **Frozen**. The order is: System 5 freezes at encounter start (which applies Rule 11) and System 12 takes the frozen view; combat runs; the encounter ends and System 12 returns the outcome; System 5 builds the reward offer and the next row from the by-value frozen view and then **thaws**; rewards are granted (create items, unlock cells); the arrangement is Editable. Rewards cannot be granted while Frozen. While Frozen every change is rejected. Freeze while Frozen and Thaw while Editable are no-ops (Freeze returns an empty list). Combat reads only the frozen hero grids.
13. **Operations and results.** All operations are atomic and validated as a whole, with no partial effect: Place (a Place of an item that is already placed is a Move), Remove, Unlock, Create, Auto-place, Undo, Freeze and Thaw. Every operation returns a result value with a reason code: `Ok`, `NoEffect`, `NothingToUndo`, `NoPlacement`, `Frozen`, `ContractViolation`, `UnknownContainer`, `UnknownItem`, `InvalidOrientation`, `RotationDisabled`, `OutOfBounds`, `LockedCell`, `Occupied` and, for `Preview`, `BufferTooSmall`. When several faults apply, the first code in the order **Frozen, ContractViolation, UnknownContainer, UnknownItem, InvalidOrientation, RotationDisabled, OutOfBounds, LockedCell, Occupied** is returned. Tests assert the code, never a message. Rotation is applied while an item is held and checked when it is dropped; the model has no rotate-in-place operation.
14. **Preview.** `Preview(item, container, origin, orientation, buffers)` is a pure query that returns a result code, the occupied cells, the blocking set and the `readingIndex` the item would take in that grid. It does not change the model and does not allocate: the caller supplies the buffers, and too small a buffer returns `BufferTooSmall`. While Frozen it returns `Frozen`. The party-wide firing position is not computed here (Dependencies).
15. **Undo.** One undo record is kept. A successful player `Place`, `Move` or `Remove` creates it (a later one replaces it). It stores the item, its old container, its old placement including orientation, and its old tray-entry number if it came from the tray. `Undo` restores exactly that, uses the record up, and for an undone `Remove` removes the item's new tray-entry number (numbers are not reused, so gaps can remain). `Create`, `Unlock`, `Auto-place` and `Freeze` clear the record. `Thaw` while Editable, a rejected operation and a `NoEffect` result leave it as it was. `Undo` while Frozen returns `Frozen`, and with no record returns `NothingToUndo`. An item destroyed by Rule 11 cannot be restored.
16. **Auto-place.** `AutoPlace(item, container)` is deterministic. The orientation is the outer loop: starting from the item's current orientation and going up cyclically through the allowed orientations, skipping an orientation whose footprint set equals one already tried. The origin is the inner loop: `oy` then `ox`, both increasing. It takes the first free placement. If the item is already in that container its own cells count as free. An item from another container or the tray moves only on success. If no placement exists it returns `NoPlacement` and the item is unchanged. It returns `CandidatesTried`, the number of (origin, orientation) candidates tested. It serves a "send to stash" control and the balance harness; it is not undoable.
17. **Arrangement view and geometry service.** The read-only view lists, for each hero in the party-slot order the caller supplies, the grid bound, the Open-cell mask and its items in reading order. Each item carries its instance ID, definition ID, orientation, origin, occupied cells in reading order, anchor cell and `readingIndex`. The stash and the tray (with tray-entry numbers) are listed separately for the UI. `Cells(item, offsets)` applies the item's orientation and origin to any authored offset set (a footprint or an adjacency pattern): each offset is turned by the same quarter turns and shifted by the **same shift as the footprint** (Formula 8), so offsets stay aligned with the footprint. Cells outside the bound are returned, not clipped. The meaning of such offsets belongs to System 10.
18. **No chance, no clock.** System 8 draws no RNG and reads no time.
19. **Snapshot and Restore.** `Snapshot()` is the canonical text of the model: UTF-8 without a byte-order mark, LF line endings, the first line `GRIDBACKPACK-SNAPSHOT v1`, fields in a fixed order, integers in invariant decimal form, containers in a fixed order and items by reading order (tray by tray-entry number). It covers every container, each item with placement and orientation, the Open cells, the tray with its numbers and counter, and the Frozen state. It excludes the undo record, which tests read through a separate `UndoDepth` accessor. Equality is equality of the text. `Restore(text)` validates before applying (a wrong version line, a duplicate ID, an overlap, a cell outside the bound or a Locked cell under an item is a `ContractViolation`) and leaves the model unchanged on failure; after a valid `Restore` the tray counter continues from its saved value. The item-ID counter belongs to System 5.
20. **Atomicity and no mutable static state.** An operation builds the new state and swaps it in, so a failure leaves the old state. A test-only injected seam (`IOperationProbe`, visible to the test assembly through `InternalsVisibleTo`, not keyed on `DEVELOPMENT_BUILD`) can throw at any step to test this. The core keeps no mutable `static` state. Fixed tables such as the shape table are immutable data. The tray-entry counter and the item-ID source are instance state. (Unity 6.6 skips the domain reload on entering Play Mode by default, so mutable static state would survive between runs.)

### States and Transitions

| Subject | State | Trigger | Result |
|---|---|---|---|
| Arrangement | Editable | System 5 freezes at encounter start | Destroy tray items (Rule 11) and clear the undo record, then Frozen. |
| Arrangement | Frozen | System 5 thaws after an eligible victory | Editable. Rewards are granted after this point. |
| Arrangement | Frozen | Freeze again | No change; an empty destroyed list. |
| Arrangement | Editable | Thaw | No change; the undo record is kept. |
| Item | Tray | `Place` on a free valid placement | In a container at that placement. |
| Item | In a container | `Place` on a free valid placement in any container | At the new placement. |
| Item | In a container | `Remove` | In the tray, with a new tray-entry number. |
| Item | Tray | Encounter starts (Freeze) | Destroyed. |
| Cell | Locked | `Unlock` | Open (permanent). |

### Interactions with Other Systems

| System | Data in | Data out | Interface owner |
|---|---|---|---|
| 2 Game Data Definitions | Footprints, hero backpack profile, rotation flag (provisional; see Dependencies) | None | System 2 owns static data and validation. |
| 5 Run State and Flow | Item-ID source implementation | Snapshot slice (containers, items, open cells, tray and counter) | System 5 owns the run snapshot; System 8 owns the slice's meaning. |
| 10 Adjacency and Synergy | A pure adjacency-preview query for a candidate placement | Arrangement view, `Cells` service | System 8 owns geometry; System 10 owns meaning and the preview query. |
| 9 Item Trigger, 11 Turn Order, 3 Effect Resolution | The party-wide firing position (composed by Systems 9 and 11) | Reading order and `readingIndex` of items in a hero grid | System 8 owns the order within a grid (Rule 6). |
| 4 Event Log | None | Placement baseline: one row per item instance in the hero grids | System 4 owns the row schema (item instance, definition, hero, anchor cell, orientation). |
| 12 Combat Resolution, 36 | Freeze and Thaw calls | Frozen view, destroyed IDs, tray list | System 5 owns the calls and the loss confirmation (the `acceptLoss` flag on `StartFight`); the UI blocks the start while dragging. |
| 15 Party and Heroes | Hero instance and party slot | None | System 15 owns heroes. |
| 25, 26 | Operations, Preview, Undo | Arrangement view, destroyed IDs, tray list | System 25 owns drag, rotate controls, the tray presentation and the confirmation dialog. |
| 17, 18, 20, 21, 24, 29, 30, 35 (later) | Create item, Unlock | Result codes, placement baseline, capacity pressure | Those systems decide when; System 8 decides validity. |

*Provisional:* System 5 implementing the ID source, System 5 calling Freeze and Thaw in the order of Rule 12, and System 15's party-slot indexes are assumptions about undesigned systems.

## Formulas

All arithmetic is on small integers. Nothing here uses floating point, RNG or time.

**Formula 1: rotated footprint.** `F_o = shift( q^o(F) )`, where `q(x, y) = (−y, x)` (with `y` pointing down) and `shift` subtracts the smallest `x` and the smallest `y`, so the result touches `x = 0` and `y = 0`.

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| `F` | set of `(x, y)` | 1 to 9 cells, edge-connected, inside a 3×3 box | data file (System 2) | Authored footprint. |
| `o` | int | `0..3`; only `0` when `rotationEnabled` is off | player choice, rules data flag | Clockwise quarter turns. |
| `F_o` | set of `(x, y)` | same size as `F`, inside a 3×3 box | calculated | Footprint in orientation `o`. |

Output range: `|F_o| = |F|`, and the box stays at most 3×3. The number of distinct `F_o` over `o = 0..3` is 1 for a 1×1, 2×2 or plus, 2 for a 1×2, 1×3, H or S/Z, and 4 for an L, T or V. Shapes used in this document: L `{(0,0),(0,1),(1,1)}`; T `{(0,0),(1,0),(2,0),(1,1)}`; V `{(0,0),(0,1),(0,2),(1,2),(2,2)}`; H `{(0,0),(0,1),(0,2),(2,0),(2,1),(2,2),(1,1)}`; plus `{(1,0),(0,1),(1,1),(2,1),(1,2)}`; S `{(1,0),(2,0),(0,1),(1,1)}`; U (1×1) `{(0,0)}`; D1 `{(0,0),(1,0)}`; Sq (2×2) `{(0,0),(1,0),(0,1),(1,1)}`.

*Examples:* the L with `o = 1`: `q` gives `{(0,0), (−1,0), (−1,1)}`, and the shift by `+1` in `x` gives `{(1,0), (0,0), (0,1)}`. The L for `o = 2` is `{(1,1), (1,0), (0,0)}` and for `o = 3` is `{(0,1), (1,1), (1,0)}`. The H with `o = 1` is `{(2,0), (1,0), (0,0), (2,2), (1,2), (0,2), (1,1)}`.

**Formula 2: occupied cells.** `cells(i) = { (ox + x, oy + y) : (x, y) ∈ F_o }`.

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| `ox`, `oy` | int | any int; valid only inside the bound | player choice | Origin: where the top-left of the footprint's box lands. |
| `W`, `H` | int | hero `1..6`; stash `5` | data file (profile), constant (stash 5×5) | Grid bound. |

*Example:* the L above with origin `(2, 1)`, `o = 1`: cells `(3,1), (2,1), (2,2)`.

**Formula 3: valid placement.** `valid(i, G, ox, oy, o) = allowed(o) ∧ ∀ (cx, cy) ∈ cells(i): 0 ≤ cx < W ∧ 0 ≤ cy < H ∧ open_G(cx, cy)`, where `allowed(o)` means `0 ≤ o ≤ 3` and, if `rotationEnabled` is off, `o = 0`. A valid placement is free when it also has `∀ cells: owner_G = none` or the owner is `i` itself; otherwise it is rejected with `Occupied` (Rule 8).

**Formula 4: anchor and reading order.** `anchor(i) = argmin over cells(i) of (cy, cx)`, comparing `cy` first. Items in one grid are sorted by `(anchor.y, anchor.x)`. *Examples:* cells `(3,1), (2,1), (2,2)` give anchor `(2,1)`; the unrotated L at origin `(2,1)` has cells `(2,1), (2,2), (3,2)` and the same anchor. The L with `o = 3` at origin `(0,0)` has cells `(0,1), (1,1), (1,0)` and anchor `(1,0)`, although its origin is `(0,0)`.

**Formula 5: blocking set.** `B(i) = { j ∈ G, j ≠ i : cells(j) ∩ cells(i) ≠ ∅ }`, ordered by the reading order of each `j`. The restriction `j ∈ G` keeps items in other grids, which may use the same coordinates, out of the set. Output: at most `|F| ≤ 9` items; `B = ∅` means the placement is free.

**Formula 6: free capacity.** `free(G) = N_open(G) − Σ_{j in G} |cells(j)|`. This is an invariant: no public operation can make it negative. `N_open` starts at `N_start`, the area of the profile's start block (MVP: 16), grows by 1 per opened cell, and is at most `N_start + len(sequence)`, which is at most the area of the bound (36). The stash has `N_open = 25`. *Example:* a hero with open 4×4 holding an H (7 cells) has `free = 16 − 7 = 9`.

**Formula 7: auto-place search bound.** `CandidatesTried ≤ orientations × W × H`, which is at most `4 × 6 × 6 = 144` for a hero grid and `4 × 5 × 5 = 100` for the stash; each candidate checks at most 9 cells, so one call performs at most 1,296 cell checks. *Examples:* a 2×2 square on a full 4×4 grid (bound 4×4) tries one orientation × 16 origins = 16 candidates; an L on the same grid tries 4 × 16 = 64.

**Formula 8: geometry service.** For an authored offset set `P` of an item with orientation `o` and origin `(ox, oy)`: `Cells(i, P) = { (ox + x', oy + y') : (x', y') = q^o(p) − s_o, p ∈ P }`, where `s_o = (min x, min y)` of `q^o(F)` is the same shift used in Formula 1. *Example:* the L with `o = 1` has `q(F) = {(0,0), (−1,0), (−1,1)}`, so `s_1 = (−1, 0)`. The offset `(1,0)` turns into `(0,1)`, shifts by `+1` in `x` to `(1,1)`, and at origin `(2,1)` is the cell `(3,2)`.

## Edge Cases

- **If a drop touches a cell outside the bound, a Locked cell, or uses a disallowed orientation**: the drop is invalid, nothing changes, and the result code names the fault by the precedence of Rule 13. An item from a container returns to its source; an item from the tray stays in the tray.
- **If a drop overlaps another item**: it is rejected with `Occupied` and the blocking set is returned. Nothing is displaced.
- **If an item is dropped so that it overlaps its own old cells**: its own cells count as free and the move succeeds.
- **If an item is released outside every grid**: nothing changes (same as an invalid drop). Only the explicit `Remove` puts a container item in the tray.
- **If the player cancels a drag**: the item returns to its source and nothing changes.
- **If a reward arrives while the stash and grids have no room**: the new item stays in the tray. To keep it the player frees space by moving or removing something, and a removed item also goes to the tray and is lost at encounter start unless stored. The model has no overflow protection on purpose; the choice is the cost, and the confirmation makes it visible.
- **If an encounter starts while the tray holds items**: System 5 returns `NeedsConfirm` and System 25 presents the confirmation first (Rule 11). On Freeze they are destroyed in tray-entry order and their IDs are returned. If the tray is empty, an empty list is returned.
- **If a start of an encounter is requested while a drag is in progress**: System 25 blocks it. If Freeze is called anyway, the held item was never dropped, so the model is unchanged; an item held from the tray is destroyed with the tray.
- **If an operation is attempted while Frozen**: it is rejected with `Frozen`; no state changes. `Undo` returns `Frozen` and `Preview` returns `Frozen`. Freeze while Frozen and Thaw while Editable are no-ops.
- **If a reward (`Create`, `Unlock`) is attempted before Thaw**: rejected with `Frozen`. The order of Rule 12 grants rewards after Thaw.
- **If Undo is requested with no record, or after an operation that cleared it**: nothing changes and the result is `NothingToUndo`.
- **If Undo reverts a `Remove`**: the item returns to its old container, placement and orientation, and its new tray-entry number is removed (never reused). If it reverts a `Place` from the tray, the item returns to the tray with its old number.
- **If an item is destroyed (Rule 11) and then named in an operation**: the operation is rejected with `UnknownItem`, and Undo cannot restore it.
- **If an item is created after Thaw and a non-combat room follows**: it stays in the tray until the next Freeze, since Rule 11 is tied to encounter start.
- **If `Remove` names an item already in the tray**: `NoEffect`.
- **If a `Place` names an item that is already placed**: it is a Move.
- **If `rotationEnabled` is off and `o ≠ 0` is requested**: rejected with `RotationDisabled`. An `o` outside `0..3` is rejected with `InvalidOrientation` (checked first).
- **If an item's footprint repeats under rotation (a 2×2, say)**: the orientation the player chose is still stored. System 10 may use direction, so orientation is not normalised away.
- **If `Unlock(n)` finds the sequence exhausted**: nothing changes and it reports zero cells opened. If fewer than `n` cells remain, it reports the number actually opened. A negative `n` is a `ContractViolation`.
- **If a hero grid holds no items**: it is valid. Whether such a hero can act is a rule of Systems 9 and 12.
- **If two items of the same definition exist**: they are separate instances with separate IDs and placements.
- **If a footprint arrives that is disconnected, larger than 3×3, or empty; a profile has a sequence cell inside the start block, outside the bound, repeated or too many; an item definition is unknown; a container is unknown; or an instance ID repeats**: this is a contract violation. The operation is rejected with `ContractViolation` (or `UnknownItem` / `UnknownContainer`) and nothing changes.
- **If a `Restore` text is corrupt**: it is rejected with `ContractViolation` and the model is unchanged.
- **If two items in one grid report the same anchor cell**: this cannot happen after a valid placement; if it is ever detected it is a contract violation and the run reports a technical error (System 12's technical-stop policy).
- **If the player keeps spare items in the stash and swaps the loadout between fights**: this is allowed and free. Whether free re-loadout undermines the pillars is an Open Question for the game designer.

## Dependencies

| System | Direction | Kind | Contract |
|---|---|---|---|
| 2 Game Data Definitions | Upstream | Hard | Footprints (connected, at most 3×3, 1 to 9 cells), hero backpack profile (bound, start block, unlock sequence), `rotationEnabled` flag. **Provisional:** System 2's GDD does not yet carry these fields or their validation (follow-up in its Open Questions). System 8 treats a malformed input as a contract violation. |
| 5 Run State and Flow | Injected | Soft | System 8 defines the item-ID source interface that System 5 implements with its run-scoped deterministic counter. System 8 holds no reference to System 5. System 5 stores System 8's snapshot slice. |
| 15 Party and Heroes | Downstream | Soft | Supplies hero instance IDs and the party-slot order of the grids in the view; System 8 treats hero IDs as opaque. System 15 uses System 8 to give a hero its starting items. System 15 is Designed ([party-and-heroes.md](party-and-heroes.md)): the slot order is the order in which the player picked the heroes, and each hero's innate item lives outside the grid and is not in the arrangement view's grid items. |
| 10 Adjacency and Synergy | Downstream | Hard | Reads the arrangement view. **System 10 provides the pure adjacency-preview query** (`PreviewPlacement`, Designed, pending review) for a candidate placement, which System 25 shows while the player drags; the Player Fantasy depends on it. In the MVP System 10 uses only the occupied cells (edge contact), not authored offsets, so the `Cells(item, offsets)` service stays available for later use. |
| 9 Item Trigger and Cooldown, 11 Turn Order | Downstream | Hard | Use the reading order and `readingIndex` within a grid. **Systems 9 and 11 provide the party-wide firing position** (System 9 is Designed: Formula 5 of [item-trigger-and-cooldown.md](item-trigger-and-cooldown.md), built on System 11's `GlobalIndex`, [turn-order.md](turn-order.md)) of a placed item (composed from the party turn order) so that System 25 can show it on the plate. |
| 3 Effect Resolution | Downstream | Hard | Takes root-effect order from the reading order (Canonical Order Contract) and the anchor-cell definition. |
| 4 Event Log | Downstream | Hard | Receives the placement baseline: one row per item instance in the hero grids. |
| 12 Combat Resolution, 36 Combat State | Downstream | Hard | System 5 calls Freeze and Thaw in the order of Rule 12 and owns the loss confirmation; Systems 12 and 10 read the frozen view; the UI blocks starting while dragging. |
| 25 Backpack UI, 26 Hover Popup | Downstream | Hard | Operations, `Preview`, Undo, the arrangement view, the tray list and destroyed IDs. |
| 17, 18, 20, 21, 24, 29, 30, 35 | Downstream | Soft | Later systems: create items and unlock cells after Thaw (17, 18, 20, 21), persist the slice (24), read placements and the baseline (29, 30), drive operations and record capacity pressure (35). They must list System 8 when designed. |

**Bidirectional status:** Systems 2, 3, 4, 12 and 36 mention System 8. System 2 needs a follow-up addition for the fields above (recorded in its Open Questions). Systems 9, 10, 11 and 15 mention System 8 (all Designed). Systems 25, 26 and the later systems above have no GDD yet and must list System 8 when designed.

## Tuning Knobs

| Knob | Spike value | Safe range | Too low | Too high |
|---|---|---|---|---|
| Starting open block per hero | 4×4 (16 cells) | 3×3 to 5×5 | Below 3×3 a 9-cell shape cannot fit, and a 7-cell H fills most of the grid. | Above 5×5 the scarcity that makes arrangement a decision (Pillar 4) fades. |
| Hero grid bound | 6×6 | 5×5 to 6×6 | The unlock sequence has no room to grow. | Open space dilutes the choice of what to carry; 7×7 is deferred to Tier 2. |
| Unlock sequence per hero profile | Authored in the profile | Inside the bound, outside the start block, no repeats, length within Rule 2 | A short sequence gives little growth. | A long one makes expansion rewards weak per unlock. |
| Stash size | 5×5 (25 cells) | 4×4 to 6×6, confirmed against the pixel budget | Rewards are lost more often. | Hoarding removes the cost of keeping spare items; 7×7 was judged too large for the layout. |
| `rotationEnabled` | on | on or off | Off: shapes need more care and fewer layouts exist. | n/a |
| Undo depth | 1 operation | Fixed at 1 for the MVP | A misdrop would be permanent. | More than 1 needs a stack rule and weakens the cost of mistakes. |
| Loss confirmation | on | on | n/a | n/a (turning it off lets items be lost by oversight) |
| Footprint box | 3×3 | Fixed | n/a | A 4×4 box adds 1×4 lines that fill a whole row; changing it needs System 2. |
| Tray loss boundary | Encounter start | Fixed in MVP | n/a | A later boundary needs an owner decision. |

**Capacity pressure.** `P = Σ|footprint| of owned items ÷ (Σ N_open over heroes + stash cells)` is a measurement, not a knob; it is not the same as `free`, since a 5×5 stash holds only two 3×3 items. System 35 records it at the end of rooms so that the stash size and the reward rate can be tuned from data. The target value is set after the first playtest.

**Interactions.** The starting block, the stash size and the largest footprint set how often a reward must be left behind: a bigger shape or a smaller stash makes loss more likely. Each of these is part of the rules version; changing one bumps the version and regenerates fixtures. Systems 9 and 10 may add knobs that depend on grid size (for instance adjacency range); they point here for the grid size and do not duplicate it.

## Visual/Audio Requirements

Events System 8 raises, with the feedback each needs. Forms come from the art bible's planning state (section 2.3.2), state table (7.11) and UI animation rules (7.12).

| Event | Visual | Audio |
|---|---|---|
| Item picked up | **Held**: plate lifted 2 px with a 2 px INK-0 shadow lower right and a 2 px Candle ring; the origin cells show an empty bed (7.11). | Soft lift tick. |
| Placement preview, valid | Ghost footprint with a solid 1 px Fog-hi outline; covered cell beds one step lighter (7.11). A text status line says where the item would fire ("Fits, fires 3rd in this hero"). | None while hovering. |
| Placement preview, invalid or `Occupied` | Ghost footprint with a 1 px Fog outline, desaturated, and a 5×5 cross on each blocking cell; no red (7.11). The text status line names the cause ("Blocked by Sword", "Locked cell", "Outside the grid"). | None while hovering. |
| Item placed | Snap: 83 ms slide, 33 ms thunk, 150 ms tier-edge pulse (7.12). | A short, dry placement click. |
| Invalid release | The item returns to its source in 150 ms with a 2 px shudder (two cycles, 160 ms). With **reduced motion** (7.13) a straight fade of the same duration replaces the shudder. | A muted thud. |
| Item enters the tray | The plate appears in the tray with a short settle. | None. |
| Cell unlocked | A flash-free eased luminance step of at least 100 ms (4.12). A **Locked cell** has a non-colour state: an INK-1 bed with a hatch and a 3×3 lock pip, distinct from an empty cell, never changing a plate edge, and checked against 3:1 contrast. | None. |
| Tray items destroyed at encounter start | A stepped dither or ink-step fade of at least 250 ms, never an alpha fade (2.2), and the number of items lost shown for at least 250 ms at the transition. Quiet in effect, but not in information (Pillars 4 and 5). | A single low tone. |

Escalating effects belong to the scene column and the payoff (Pillar 2), never on top of the grid. The tray is a panel in the workbench, below the protected layer (HP, intent tabs, turn marker, the left grid column; section 5.10.4); only the held item is above it.

**Assets needed (names per art bible 8.3):** empty cell bed; hover, valid and invalid ghost footprints; locked cell with lock pip; locked-cell tray frame (9-slice); loose-count chip with a warning icon; tray-loss fade frames (stepped, one image per frame); held shadow. Item plates and icons already exist (8.5).

**Art bible follow-ups (owned by the art director):** list the reward tray in sections 2.3.2 and 5.10.4; reword 7.12 to "an item is never destroyed except by tray loss after confirmation"; update the 8-wide stash assumption in 8.1 (the table around the 160 px stash row) and close the open "stash size" item in 8.B.16 with 5×5; re-run `tools/art/mockup_planning.py` with a full 5×5 stash and eight tray plates against the 12% lit and 0.08 APL budget. The art bible gives three different left-column widths (7.3, 8.1 and 8.B.14); that is routed to the art director and is not settled here.

> 📌 **Asset Spec** — Visual/Audio requirements are defined. After the art bible is approved, run `/asset-spec system:grid-backpack` to produce per-asset visual descriptions, dimensions, and generation prompts from this section.

## Game Feel

Items should feel like cut stones set on dark felt: calm, deliberate, weighty, never frantic.

- **Input response:** picking up and the preview must follow the cursor with no perceptible lag; target: the preview updates within one frame of the cursor crossing a cell boundary, and it is recomputed only when the hovered cell or the orientation changes.
- **Commitment:** the model changes only on release. A release outside every grid, or a cancelled drag, leaves the model unchanged (Rule 7).
- **Rotation:** the rotate control acts while an item is held and updates the preview immediately.
- **Undo:** one press restores the last operation at once, so a misdrop costs little; it is available only until the next state-changing operation.

These are design targets to verify in playtest.

## UI Requirements

System 8 owns no screen. The arrangement screen (Systems 25 and 26) must show:

- **One hero grid at a time**, with a switch between heroes (also while an item is held) and the party overview, as the art bible fixes in 7.6. The model still manages all three.
- Open and Locked cells that are visibly distinct without relying on colour (a hatch and a lock pip, checked for 3:1 contrast).
- The 5×5 stash, and the **reward tray** as a list inside the workbench (not an overlay), with a persistent **chip** showing how many items are in it and a warning that they are lost when the fight starts. Its exact place in the 174 to 206 px column is for `/ux-design` to settle; the lower section already holds the Stash and Overview tabs.
- A held item that follows the cursor, with the preview cues and the text status line of the Visual/Audio table.
- **Input parity (art bible 7.13):** pick and place by click or by drag; right-click rotates while an item is held, with a gamepad rotate button and a counter-rotation; cancel returns the item to its source; every action is remappable through System 6; prompt chips show the current binding. A "Send to stash" control (Auto-place), a "Remove to tray" control, and an Undo control, each labelled. Partial gamepad support is a cell cursor with pick, rotate, cancel and drop buttons, which needs no change to the model because `Preview` takes an origin.
- A **confirmation** "Leave behind: X, Y?" naming the items when an encounter is about to start with a non-empty tray, and the block on starting an encounter while an item is being dragged.
- Screen-reader and text-size tiers are System 34's; the text status line is the source they read.
- The choice between UGUI and UI Toolkit belongs to the System 25 ADR (the prototype uses UGUI; the engine reference recommends UI Toolkit).

> **📌 UX Flag — Grid Backpack**: This system has UI requirements. In Phase 4 (Pre-Production), run `/ux-design` to create a UX spec for the arrangement screen **before** writing epics. Stories that reference UI should cite `design/ux/[screen].md`, not the GDD directly.

## Cross-References

- [Game Concept](game-concept.md): Pillar 4 (Arrangement Is the Thinking), the MVP definition (4×4, rotation as the first cut).
- [Systems Index](systems-index.md): System 8 row and dependency map.
- [Game Data Definitions](game-data-definitions.md): footprint and hero backpack profile.
- [Effect Resolution Model](effect-resolution-model.md): the Canonical Order Contract uses this document's anchor cell and reading order.
- [Event Log](event-log.md): the placement baseline table this system supplies.
- [Combat State and Step Model](combat-state-and-step-model.md) and [Combat Resolution](combat-resolution.md): the frozen arrangement and the Freeze and Thaw calls.
- [Art Bible](../art/art-bible.md): 2.2 (eyes-first rules), 2.3.2 (planning state, lit budget), 4.12 (photosensitivity), 5.10.4 (protected layer), 7.6 (switch-only planning UI), 7.11 (state table), 7.12 (UI animation feel), 7.13 (accessibility and input), 8.1 (runtime rules), 8.B.16 (open items).
- [Unity version reference](../../docs/engine-reference/unity/VERSION.md) and `current-best-practices.md`: domain reload is skipped by default in 6.6. `modules/scripting.md` is absent, so the C# language level, asmdef settings and .NET profile are NOT SOURCEABLE and need an ADR. The core uses only C# 9-safe value types until that ADR; the plain .NET test project lives outside `Assets/`, and the Unity tests live in `Assets/_Project/Tests/EditMode/Backpack/`.

**Prototype migration (do not migrate as is).** `Assets/_Project/Scripts/Backpack/` is evidence only. The core must be rebuilt: split the authored footprint data from a pure footprint value type; own placement and identity in one arrangement aggregate; return result codes instead of a bool plus an event; rename the prototype's `AnchorX/AnchorY` (the placement origin) so it cannot be confused with the anchor cell; replace the `static` item counter with an injected ID source; and replace the full-grid scans and shared temp buffer in `TryPlaceItem`, `ClearOccupancy` and `CanPlaceItemAt`. The prototype's sort by origin gives a different order from the anchor-cell order for rotated shapes (for example the L with `o = 3`).

## Acceptance Criteria

**Fixtures.** `PROFILE-A`: bound 6×6, start block 4×4 at the top left, unlock sequence = the 20 cells outside the start block in row-major order. `PROFILE-B` (test only): bound 2×2, start block 2×2. `PROFILE-C` (test only): bound 4×4, start block 4×4, empty sequence. `STASH`: 5×5, all open. Shapes are those of Formula 1. A fake item-ID source returns 1, 2, 3, and so on. `WALK`: `PROFILE-C` hero grid, `STASH`, items U, D1 and L, and a fixed list of at most 25 operations (placements at fixed origins and orientations, Remove, Create, Undo, AutoPlace, Freeze, Thaw, Unlock) kept with the fixtures. "Snapshot" is the text of Rule 19. Unity tests live in `Assets/_Project/Tests/EditMode/Backpack/`; a plain .NET test project outside `Assets/` runs the same sources.

**Logic (unit tests, BLOCKING):**

1. **State walker.** **Given** `WALK`, **when** every sequence of up to 3 operations from the fixed list is run (at most 25 + 625 + 15,625 = 16,275 sequences, deterministic, no random seed), **then** after each step every item is in exactly one of {hero grid, stash, tray} or in the destroyed-IDs list, `free(G) ≥ 0` for every grid, no two items overlap or share an anchor, tray-entry numbers are unique and increasing, and `Restore(Snapshot(s))` equals the Snapshot. This walker is shared by criteria 21, 22 and 25.
2. **Hero geometry and unlock.** **Given** `PROFILE-A`, **when** the arrangement is created, **then** 16 cells are Open and 20 are Locked; a `U` at `(4,0)` returns `LockedCell` and at `(6,0)` returns `OutOfBounds`; `Unlock(1)` opens `(4,0)` and returns 1; `Unlock(25)` opens the remaining 19 and returns 19; `Unlock(1)` again returns 0; `Unlock(−1)` returns `ContractViolation`.
3. **Stash.** **Given** `STASH`, **when** it is created, **then** all 25 cells are Open; a `U` at `(4,4)` is valid and at `(5,0)` returns `OutOfBounds`; placement, `Occupied` and undo follow the same rules as in a hero grid; and a stash item is absent from the hero-grid section of the view and from the baseline.
4. **Rotation table.** **Given** each of L, T, V, H, plus, S, Sq and D1, **when** all four orientations are computed, **then** the sets match the stored table, including L `o = 0..3` as `{(0,0),(0,1),(1,1)}`, `{(1,0),(0,0),(0,1)}`, `{(1,1),(1,0),(0,0)}`, `{(0,1),(1,1),(1,0)}` and H `o = 1` as `{(2,0),(1,0),(0,0),(2,2),(1,2),(0,2),(1,1)}`; the distinct-orientation counts are 1 for Sq, plus and U, 2 for D1, H and S, 4 for L, T and V. `o = 4` and `o = −1` return `InvalidOrientation`; with `rotationEnabled` off `o = 1` returns `RotationDisabled`.
5. **Placement and bounds.** **Given** `PROFILE-C`, **when** the L with `o = 1` is placed at origin `(2,1)`, **then** its cells are `(3,1),(2,1),(2,2)`. Boundary table on `PROFILE-C`, `PROFILE-A` and `STASH`: `U` at `(0,0)` and `(W−1,H−1)` is valid; `U` at `(W,0)` and `(−1,0)` returns `OutOfBounds`; H at `(1,1)` in `PROFILE-C` is valid and at `(2,2)` returns `OutOfBounds`; in `PROFILE-A` an L with a cell on a Locked cell returns `LockedCell`.
6. **Anchor and reading order.** **Given** the L with `o = 1` at origin `(2,1)`, **then** its anchor is `(2,1)`; the unrotated L at `(2,1)` has the same anchor; the L with `o = 3` at origin `(0,0)` has anchor `(1,0)`. **Given** that L and a `U` at `(0,0)` in one grid, **then** the reading order is `U` first (anchor `(0,0)`, `readingIndex` 0), then the L (anchor `(1,0)`, `readingIndex` 1).
7. **Drop and release.** **Given** a valid free placement, **when** the item is dropped there, **then** it occupies those cells and its source is empty. **Given** an invalid drop, a release outside every grid or a cancelled drag, **then** the Snapshot is byte-identical to the one before, and a tray item stays in the tray.
8. **Occupied.** **Given** `PROFILE-C` with `P = D1` at origin `(0,0)` and `Q = U` at `(3,3)`, **when** `R = U` is dropped at `(1,0)`, **then** the result is `Occupied`, the blocking set is `[P]` and the Snapshot is unchanged. **Given** `P = D1` at `(0,0)` and `S2 = U` at `(2,0)`, **when** `R = D1` is dropped at `(1,0)`, **then** the blocking set is `[P, S2]` in reading order. **Given** the same coordinates in another hero's grid, **then** those items are not in the set.
9. **Self overlap.** **Given** the L at origin `(0,0)` moved to origin `(1,0)` (overlapping its own cell `(1,1)`), **then** the result is `Ok` and no other item moves.
10. **Tray.** **Given** a `Create`, **then** the item is in the tray with the next tray-entry number and no grid changes; **given** `Remove` of a grid item, **then** it enters the tray with a new number; **given** a `Place` from the tray, **then** it leaves the tray; `Remove` of a tray item returns `NoEffect`. The tray is listed with its numbers in the view and is absent from the hero-grid section and the baseline.
11. **Create and ID source.** **Given** the fake source (1, 2, 3), **when** two items are created, **then** they get IDs 1 and 2; **given** a `Create` that fails validation, **then** the next ID is still 3 (`Next()` was not called); **given** the same source and creation order again, **then** the IDs are the same.
12. **Loss at Freeze.** **Given** tray items with entry numbers 5, 6 and 7 and one item in the stash, **when** the arrangement is frozen, **then** the tray items are destroyed in order 5, 6, 7 and their IDs are returned in that order; grids and stash are unchanged; Freeze again returns an empty list; a destroyed ID given to Place returns `UnknownItem`; an item created after Thaw survives until the next Freeze; a pick-up without a drop leaves the model unchanged through Freeze.
13. **Frozen rejections.** **Given** the Frozen state, **when** Place, Remove, Unlock, Create, AutoPlace or Undo is called, **then** each returns `Frozen` and the Snapshot is unchanged; `Preview` returns `Frozen`; after Thaw the state is Editable.
14. **Order of a round.** **Given** the sequence Freeze, combat, Thaw, **when** `Create` and `Unlock` are called before Thaw, **then** each returns `Frozen`; after Thaw each succeeds.
15. **Atomicity.** **Given** the `IOperationProbe` seam throwing at step `k` (for `k` = 0 up to the last step) inside each operation, **then** the Snapshot and `UndoDepth` after are equal to those before, and the test asserts that the exception was actually thrown at each `k`.
16. **Preview.** **Given** a candidate drop, **when** `Preview` is called, **then** it returns the code, the occupied cells, the blocking set and the `readingIndex`, these match the result of the real drop, and the Snapshot is unchanged. **Given** too small a buffer, **then** `BufferTooSmall`. **Given** 1,000 calls after a warm-up call, **then** `GC.GetAllocatedBytesForCurrentThread()` shows no allocation, and a deliberately allocating helper in the same test shows a positive delta (positive control).
17. **Undo.** **Given** each of a `Place` from the tray, a cross-container `Move` and a `Remove`, **when** the player undoes it, **then** the item is back in its old container, placement and orientation (a tray item with its old number; an undone `Remove` loses its new number), and a second `Undo` returns `NothingToUndo`. **Given** a `Create`, an `Unlock`, an `AutoPlace` or a `Freeze` between the operation and the `Undo`, **then** `Undo` returns `NothingToUndo`. **Given** `Thaw` while Editable or a rejected drop between them, **then** the record is intact. `Undo` while Frozen returns `Frozen`.
18. **Auto-place.** **Given** an empty `PROFILE-C` and an L at `o = 0`, **then** `AutoPlace` chooses origin `(0,0)` with `o = 0`; an L whose current orientation is 2 chooses `o = 2` at `(0,0)`. **Given** `PROFILE-B` with a `U` at `(1,1)`, **then** the L is placed at origin `(0,0)` with `o = 1`. **Given** a full `PROFILE-C`, **then** the result is `NoPlacement`, the item is unchanged and `CandidatesTried` is 16 for Sq and 64 for the L. An item in the tray moves only on success. `CandidatesTried` never exceeds 144 on a hero grid and 100 on the stash, and repeated calls give the same result.
19. **View and Snapshot text.** **Given** a fixed set of heroes and items with pre-assigned IDs and tray numbers, **when** the view is built with the items inserted in different orders, **then** the Snapshots are byte-identical and equal the stored golden text file (LF line endings, no byte-order mark, enforced by `.gitattributes`), and the view lists the party slots in the order supplied, items in reading order with every field of Rule 17, and the tray numbers.
20. **Geometry service.** **Given** the L with `o = 1` at origin `(2,1)` and the offset set `{(1,0)}`, **then** `Cells` returns `{(3,2)}`; an offset that falls outside the bound is returned unclipped.
21. **Capacity.** **Given** a hero with an open 4×4 holding an H, **then** `free = 9`; after one `Unlock` it is 10; the empty stash has `free = 25`; the invariant `free ≥ 0` holds throughout criterion 1's walker.
22. **Result precedence.** **Given** combined faults, **then** the code follows Rule 13: Frozen and UnknownItem return `Frozen`; UnknownItem and InvalidOrientation return `UnknownItem`; `o = 4` with `rotationEnabled` off returns `InvalidOrientation`; OutOfBounds and LockedCell return `OutOfBounds`; LockedCell and Occupied return `LockedCell`.
23. **Place equals Move.** **Given** a `Place` of an item that is already placed, **then** the Snapshot equals the one produced by the corresponding Move.
24. **Symmetric orientation.** **Given** an Sq placed with `o = 2`, **then** the Snapshot and the view record `o = 2`.
25. **Restore.** **Given** any state in criterion 1's walker, **then** `Restore(Snapshot(s))` equals the Snapshot and the tray counter continues; **given** a text with a wrong version line, a duplicate ID, an overlap, a cell outside the bound or an item on a Locked cell, **then** `Restore` returns `ContractViolation` and the model is unchanged.
26. **Contract violations.** **Given** a disconnected footprint, a 4×4 box, an empty footprint, an unknown definition, an unknown container, a fake ID source returning a repeated ID, or a profile whose sequence has a cell inside the start block, outside the bound, repeated, or longer than allowed, **when** it is submitted, **then** it is rejected with `ContractViolation`, `UnknownItem` or `UnknownContainer` and the Snapshot is unchanged.
27. **Core assembly.** (a) **Given** the core assembly, **when** it is built, **then** its asmdef sets `noEngineReferences: true` and its reference list is the named allowlist, which excludes `UnityEngine`, `Unity.Collections`, `Unity.Mathematics` and `Unity.Burst`. (b) A reflection test finds no mutable static state, skipping compiler-generated types. (c) **Given** one shared operation script, **then** the plain .NET run, the headless run and the Unity EditMode run produce a Snapshot equal to the stored golden text, and a source scan finds no use of `System.Random`, `UnityEngine.Random`, `DateTime.Now`, `DateTime.UtcNow` or `Stopwatch` in the core.

**Integration (deferred until the named systems have implementations):**

28. **System 4.** **Given** a frozen arrangement, **then** the placement baseline has one row per hero-grid item with instance ID, definition ID, hero, anchor cell and orientation, and none for the stash or tray.
29. **Systems 3 and 9.** **Given** a frozen arrangement, **then** their root order for one hero equals the reading order of this system, including a `U` at `(0,0)` and the L with `o = 3`.
30. **Systems 9, 10 and 11.** **Given** a candidate placement, **then** the adjacency-preview query answers without changing the model, and the party-wide firing position is provided and shown with the `Preview` data.
31. **Systems 5 and 25.** **Given** a non-empty tray at the start of an encounter, **then** the confirmation names the items before Freeze is called, declining keeps the arrangement Editable, and starting is blocked while an item is held. Atomic confirmation against changes to the list remains blocked pending the Systems 5/25 interface.
32. **System 25 cross-hero move.** **Given** an item held on one hero's grid, **when** the player switches hero and drops it, **then** the item is placed in the other hero's grid by `Place`.

**UI and input (retained evidence):**

33. **Given** the arrangement screen, **then** screenshots stored under `production/qa/evidence/grid-backpack-*.png` show: (a) one hero grid with the switch and overview; (b) Open and Locked cells distinct without colour; (c) the tray with the loose-item chip, count and warning; (d) an `Occupied` drop with the cross cue and the text status line naming the blocker; (e) the confirmation dialog naming the items. Timed captures use a fixed-tick stepper, and the lead signs them off.
34. **Given** the controls, **then** pick and place work by click and by drag, right-click rotates while an item is held, a gamepad rotate and counter-rotate exist, cancel returns the item to its source, and every action is remappable (checked by an input test and a retained capture of the rebind screen).

*Coverage:* every Core Rule (1 to 20) and every Formula (1 to 8) has at least one criterion: Rules 1 to 6 by criteria 1 to 6, Rules 7 and 8 by 7 to 9, Rules 9 to 12 by 10 to 14, Rule 13 by 13, 22 and 23, Rule 14 by 16, Rule 15 by 17, Rule 16 by 18, Rule 17 by 19 and 20, Rule 18 by 27c, Rule 19 by 19 and 25, Rule 20 by 15 and 27b, and Formulas 6 to 8 by 21, 18 and 20.

## Open Questions

| Question | Owner | Resolve by |
|---|---|---|
| Does free re-loadout between fights (swapping spare items from the stash at no cost) undermine the pillars? | Game designer | First playtests. |
| What is the target capacity pressure `P`, and is the 5×5 stash right (the earlier design was 7×7, which the layout could not hold)? | Game designer, System 35 | First playtest data. |
| Should MVP content use only the concept's four shapes (1×1, 1×2, 2×2, L)? Larger shapes (T, V, H) cost art and balance work, and a 7-to-9-cell item changes the 4×4 puzzle. | Game designer, System 2 | When the item roster is chosen. |
| Freeze/Thaw owner resolved: System 5 calls them and returns `NeedsConfirm`; System 25 presents the list and explicit acceptance. How is the accepted list protected against changes before `StartFight`? | Systems 5 and 25 | Before tray-loss confirmation implementation. |
| How is the party-wide firing position provided to the arrangement screen? (The adjacency preview is answered: System 10's `PreviewPlacement`. The position is defined by System 9, Formula 5, on System 11's `GlobalIndex`; both are Designed.) | Systems 11 and 25 | When those are designed. |
| Is the destruction of tray items recorded for the run summary and breakdown? | Systems 5 and 30 | Before run summaries. |
| ~~Does System 10 use direction (rotating authored offsets)?~~ Answered by System 10 (2026-11-12): no, the MVP uses only occupied cells, so the stored orientation of a symmetric item is not used there. Orientation stays stored in case Tier 2 adds directional patterns. | System 10 | Closed. |
| Where do reward items first appear (before the tray), and is a reward panel the same as the tray? | Systems 17, 18, 28 | When those are designed. |
| Where does the tray sit in the 174 to 206 px workbench column, and is there a cap on how many tray plates are shown? | System 25, art director | UX spec. |
| Is an undo depth of 1 enough? | Game designer | First playtests. |
| Which ADR covers the item-ID source interface, shared with System 4's ID scheme, and the Snapshot format? | Technical Director | Before the combat spike. |
| Which ADR records the core assembly's asmdef settings and C# language level (no `init`-only setters without a shim at C# 9)? | Technical Director | Before the combat spike. |
| Which UI stack (UGUI or UI Toolkit) does the arrangement screen use? | System 25 ADR | Before building System 25. |
| Do any door cards raise a hero bound to 7×7 (deferred to Tier 2)? | System 17 | When Tier 2 is planned. |
| System 2 follow-up: add the 3×3 box, edge connectivity, hero backpack profile (start block, unlock sequence) and `rotationEnabled` with their validation to its GDD. | System 2 | Next System 2 revision. |
| System 18 ([commitment-and-drop-model.md](commitment-and-drop-model.md)) takes its snapshot by value at victory, before Thaw, builds the offer in the same moment and stores it; only the creation of the chosen item happens after Thaw (Rule 12). Confirm this ordering and that creation never fails for capacity; the free stash re-loadout also weakens the pivot cost that System 18 relies on. | Systems 8, 18 and 5 | Before System 8 is re-reviewed. |
| System 17's Purple buff opens one locked cell with `Unlock(1)` after Thaw and before the item is created ([door-generation-and-tiers.md](door-generation-and-tiers.md)); it needs a locked-cell query for a hero (to warn before the choice), `Frozen` returned without effect when the grid is frozen, and at least three locked cells per hero at the start. | Systems 8 and 17 | Before System 8 is re-reviewed. |
| System 19's Pinned curse needs a `pinned` item flag, a `Pinned` result code for Place, Move, rotate, Remove, stash and AutoPlace (even onto its own cell), an atomic `CreatePinned`, and a version bump of the grid slice ([curses-and-trade-offs.md](curses-and-trade-offs.md)). | Systems 8 and 19 | Before the Vertical Slice. |
| System 6 ([input-and-focus.md](input-and-focus.md)) computes the tray ghost by a pure loop over `Preview` in `AutoPlace`'s search order (never by calling `AutoPlace`, which mutates the model and clears Undo), uses clockwise quarter turns (counter-clockwise is three), and needs the `Pinned` result and a locked-cell query only in the Vertical Slice. | Systems 6 and 8 | Before System 8 is re-reviewed. |
